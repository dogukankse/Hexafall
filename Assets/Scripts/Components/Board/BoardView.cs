using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Scripts.Base;
using Scripts.ScriptableObjects;
using Scripts.Utils;
using UnityEngine;
using Yell;

namespace Scripts.Components.Board
{
	public class BoardView : MonoBehaviour
	{
		[SerializeField] private Hexagon _hexagonPrefab;
		[SerializeField] private Bomb _bombPrefab;
		[SerializeField] private GridCell _gridCellPrefab;
		[SerializeField] private ColorContainerSo _colorContainer;
		[SerializeField] private GameObject _dot;
		[SerializeField] private Outline _outline;

		private ObjectPool<BaseHexagon> _hexagonPool;
		private ObjectPool<Bomb> _bombPool;
		private List<BaseHexagon> _passiveHexagons;

		private List<GridCell> _selectedCells;
		private List<BaseHexagon> _selectedHexagons;
		private GridCell _selectedCell;
		private int _selectionState;
		private Transform _parent;

		private BoardController _controller;

		/// <summary>
		/// Plays popping anims for cells
		/// </summary>
		/// <param name="cellsToExplode"></param>
		public void Pop(HashSet<GridCell> cellsToExplode)
		{
			Sequence seq = DOTween.Sequence();
			YellManager.Instance.Yell(YellType.OnPop, new YellData(cellsToExplode.Count));

			foreach (var cell in cellsToExplode)
			{
				var item = cell;

				seq.Insert(0,
					cell.Child.transform.DOScale(Vector3.zero, 1f).OnComplete(() =>
					{
						if (item.Child.HexagonState == BaseHexagon.State.Bomb)
						{
							item.Child.transform.SetParent(_parent);
							item.Child = _passiveHexagons.Last();
							item.Child.HexagonState = BaseHexagon.State.Hexagon;
							_passiveHexagons.RemoveAt(_passiveHexagons.Count - 1);
						}

						item.Child.gameObject.SetActive(false);
					}));
			}

			seq.OnComplete(FallTiles);
		}

		//move to local centre
		public void MoveToCentre(BaseHexagon hexagon)
		{
			hexagon.transform.DOLocalMove(Vector3.zero, .33f);
		}

		//Creates individual grid cell
		protected internal GridCell CreateCell(Vector2 pos)
		{
			return Instantiate(_gridCellPrefab, pos, Quaternion.identity, transform);
		}

		private void Awake()
		{
			YellManager.Instance.Listen(YellType.OnTouch, new YellAction(this, OnTouch));
			YellManager.Instance.Listen(YellType.OnSwipe, new YellAction(this, OnSwipe));

			_controller = new BoardController(this);

			_parent = new GameObject("HexagonPool").transform;
			_selectedCells = new List<GridCell>();
			_selectedHexagons = new List<BaseHexagon>();
			_hexagonPool = new ObjectPool<BaseHexagon>(_hexagonPrefab, _controller.Width * _controller.Height,
				_parent);
			_bombPool = new ObjectPool<Bomb>(_bombPrefab, 10, _parent);
			_passiveHexagons = new List<BaseHexagon>();
		}

		private void Start()
		{
			CreateHexagons();
			ScaleBoard();
		}

		//get base hexagon item from pool and place it
		private void CreateHexagons()
		{
			for (int y = 0; y < _controller.Height; y++)
			{
				for (int x = 0; x < _controller.Width; x++)
				{
					GridCell cell = _controller.Grid[x, y];
					BaseHexagon hexagon = _hexagonPool.GetObject();
					hexagon.transform.SetParent(cell.transform);
					hexagon.transform.localPosition = Vector3.zero;
					cell.Child = hexagon;
					hexagon.Color = GetRandomColor(cell);
					hexagon.Scale(.5f, x * y * 0.01f);
				}
			}

			//trigger an event for allow user input
			YellManager.Instance.Yell(YellType.CreationComplete);
		}

		/// <summary>
		/// scales the board by size
		/// </summary>
		private void ScaleBoard()
		{
			transform.localScale = _controller.Width <= 8 ? new Vector3(1.3f, 1.3f) : new Vector3(1f, 1f);
		}

		/// <summary>
		/// Get random color by neighbors' colors. Searches different colors than the other to prevent an initial match.
		/// </summary>
		/// <param name="cell">Cell to color</param>
		/// <returns>Color</returns>
		private Color GetRandomColor(GridCell cell)
		{
			List<GridCell> neighbors = _controller.GetNeighbors(cell);
			Dictionary<Color, int> neighborColors = new Dictionary<Color, int>();

			foreach (var neighbor in neighbors)
			{
				if (neighborColors.ContainsKey(neighbor.Child.Color))
					neighborColors[neighbor.Child.Color]++;
				else neighborColors.Add(neighbor.Child.Color, 1);
			}

			foreach (KeyValuePair<Color, int> item in neighborColors.Where(kvp => kvp.Value == 1).ToList())
			{
				neighborColors.Remove(item.Key);
			}

			if (neighborColors.Keys.Count == 0)
				return _colorContainer.GetRandomColor();
			else
				return _colorContainer.GetRandomColor(neighborColors.Keys.ToArray());
		}
		/// <summary>
		/// Yell Action.
		/// Selects item based on user input. If same item selected again pick different 2 neighbor 
		/// </summary>
		private void OnTouch(YellData data)
		{
			GridCell cell = (GridCell) data.data;
			for (var i = 0; i < _selectedCells.Count; i++)
			{
				var c = _selectedCells[i];
				_selectedHexagons[i].transform.SetParent(c.transform);
				_selectedHexagons[i].transform.localPosition = Vector3.zero;
			}

			_selectedCells.Clear();
			_selectedHexagons.Clear();

			if (_selectedCell == cell)
			{
				_selectionState++;
				_selectionState %= 6;
			}
			else
			{
				_selectedCell = cell;
				_selectionState = 0;
			}

			_selectedCells = _controller.PickNeighbor(_selectedCell, ref _selectionState);

			//dot and outline for selected 3 neighbor
			_dot.transform.localPosition = (_selectedCells[0].transform.localPosition +
				_selectedCells[1].transform.localPosition +
				_selectedCells[2].transform.localPosition) / 3f;
			_dot.SetActive(true);

			_outline.SetData(_selectedCells[0].transform.position, _selectedCells[1].transform.position,
				_selectedCells[2].transform.position);
			_outline.SetStatus(true);

			foreach (var c in _selectedCells)
			{
				_selectedHexagons.Add(c.Child);
			}

			_outline.transform.SetParent(_dot.transform);
		}

		/// <summary>
		/// Yell Action.
		/// Starts a coroutine and rotates items.
		/// </summary>
		/// <param name="data"></param>
		private void OnSwipe(YellData data)
		{
			Vector2 dir = (Vector2) data.data;
			YellManager.Instance.Yell(YellType.OnRotate);
			bool isClockwise = (dir.x == 1 || dir.y == 1);//read from user input swipe delta
			StartCoroutine(_controller.CheckMatch(_selectedCells, isClockwise, OnCheckComplete));
		}

		private void OnCheckComplete(HashSet<GridCell> cellsToExplode)
		{
			if (cellsToExplode.Count >= 3)
			{
				_selectedCells.Clear();
				_selectedHexagons.Clear();
				_dot.SetActive(false);
			}

			if (cellsToExplode.Count > 0)
				Pop(cellsToExplode);
			else
				YellManager.Instance.Yell(YellType.AllowSwipe);

			YellManager.Instance.Yell(YellType.OnRotationEnd);
		}


		/// <summary>
		/// Checks and fells the item if the bottom of them is empty
		/// </summary>
		private void FallTiles()
		{
			Sequence seq = DOTween.Sequence();
			seq.SetAutoKill(true);
			int fallCount = 0;
			for (int x = _controller.Width - 1; x >= 0; x--)
			{
				for (int y = _controller.Height - 1; y >= 0; y--)
				{
					GridCell cell = _controller.Grid[x, y];
					if (cell.Child.gameObject.activeSelf == false)
					{
						//if cell is empty +1 for counter
						fallCount++;
					}
					else if (fallCount > 0)
					{
						//pick non-empty first item
						BaseHexagon hexagon = _controller.Grid[x, y + fallCount].Child;
						//assign it to the cell to fall
						_controller.Grid[x, y + fallCount].Child = cell.Child;
						cell.Child = hexagon;
						cell.Child.transform.localPosition = Vector3.zero;
						int xCord = x;
						int yCord = y + fallCount;
						float delay = (_controller.Height - y) * .1f;
						seq.Insert(0,
							_controller.Grid[xCord, yCord].Child.transform.DOLocalMove(Vector3.zero, .3f)
								.SetDelay(delay)
						);
					}
				}

				fallCount = 0;
			}

			//on fall complete spawn new hexagons
			seq.OnComplete(() =>
			{
				for (int y = 0; y < _controller.Height; y++)
				{
					for (int x = 0; x < _controller.Width; x++)
					{
						GridCell cell = _controller.Grid[x, y];
						BaseHexagon hexagon = cell.Child;
						if (cell.Child.gameObject.activeSelf) continue;

						//if the bomb item must be spawned, spawn it
						if (_controller.SpawnBomb)
						{
							_passiveHexagons.Add(hexagon);
							hexagon = _bombPool.GetObject();
							hexagon.HexagonState = BaseHexagon.State.Bomb;
							cell.Child = hexagon;
							_controller.SpawnBomb = false;
						}

						hexagon.transform.localPosition = Vector3.zero;
						hexagon.Color = GetRandomColor(cell);
						hexagon.gameObject.SetActive(true);
						//spawn anim
						hexagon.Scale(.5f, x * y * 0.01f);
					}
				}

				DOVirtual.DelayedCall(_controller.Height * _controller.Width * .01f + .5f, () =>
					YellManager.Instance.Yell(YellType.FallCompleted));
			});
		}
	}
}