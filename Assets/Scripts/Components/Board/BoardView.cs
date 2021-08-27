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
		[SerializeField] private GridCell _gridCellPrefab;
		[SerializeField] private ColorContainerSo _colorContainer;
		[SerializeField] private GameObject _dot;
		[SerializeField] private Outline _outline;

		private ObjectPool<Hexagon> _hexagonPool;

		private List<GridCell> _selectedCells;
		private List<BaseHexagon> _selectedHexagons;
		private GridCell _selectedCell;
		private int _selectionState;

		private BoardController _controller;


		private void Awake()
		{
			YellManager.Instance.Listen(YellType.OnTouch, new YellAction(this, OnTouch));
			YellManager.Instance.Listen(YellType.OnSwipe, new YellAction(this, OnSwipe));
			_controller = new BoardController(this);

			_selectedCells = new List<GridCell>();
			_selectedHexagons = new List<BaseHexagon>();
			_hexagonPool = new ObjectPool<Hexagon>(_hexagonPrefab, _controller.Width * _controller.Height,
				new GameObject("HexagonPool").transform);


			CreateHexagons();
		}

		protected internal GridCell CreateCell(Vector2 pos)
		{
			return Instantiate(_gridCellPrefab, pos, Quaternion.identity, transform);
		}

		private void CreateHexagons()
		{
			for (int y = 0; y < _controller.Height; y++)
			{
				for (int x = 0; x < _controller.Width; x++)
				{
					GridCell cell = _controller.Grid[x, y];
					Hexagon hexagon = _hexagonPool.GetObject();
					hexagon.transform.SetParent(cell.transform);
					hexagon.transform.localPosition = Vector3.zero;
					cell.Child = hexagon;
					hexagon.Color = GetRandomColor(cell);
					hexagon.Scale(.5f, x * y * 0.01f);
				}
			}
		}

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

			_selectedCells = _controller.PickNeighbor(cell, _selectedCell, ref _selectionState);


			_dot.transform.localPosition = (_selectedCells[0].transform.localPosition +
				_selectedCells[1].transform.localPosition +
				_selectedCells[2].transform.localPosition) / 3f;
			_dot.SetActive(true);

			_outline.SetData(_selectedCells[0].transform.localPosition, _selectedCells[1].transform.localPosition,
				_selectedCells[2].transform.localPosition);
			_outline.SetStatus(true);

			foreach (var c in _selectedCells)
			{
				_selectedHexagons.Add(c.Child);
			}

			_outline.transform.SetParent(_dot.transform);
		}

		private void OnSwipe(YellData data)
		{
			Vector2 dir = (Vector2) data.data;
			YellManager.Instance.Yell(YellType.OnRotate);
			bool isClockwise = (dir.x == 1 || dir.y == 1);
			StartCoroutine(_controller.CheckMatch(_selectedCells, isClockwise, Rotate));
		}

		private void Rotate(HashSet<GridCell> cellsToExplode, int turnCount)
		{
			if (cellsToExplode.Count >= 3)
			{
				_selectedCells.Clear();
				_selectedHexagons.Clear();
				_dot.SetActive(false);
			}

			Pop(cellsToExplode);
			YellManager.Instance.Yell(YellType.OnRotationEnd);
		}

		public void Pop(HashSet<GridCell> cellsToExplode)
		{
			Sequence seq = DOTween.Sequence();

			foreach (var cell in cellsToExplode)
			{
				var item = cell;
				seq.Insert(0,
					cell.Child.transform.DOScale(Vector3.zero, 1f).OnComplete(() =>
					{
						YellManager.Instance.Yell(YellType.OnPop);
						item.Child.gameObject.SetActive(false);
					}));
			}

			seq.OnComplete(FallTiles);
		}

		private List<GridCell> _tilesToFall;

		private void FallTiles()
		{
			_tilesToFall = new List<GridCell>();
			Sequence seq = DOTween.Sequence();
			int fallCount = 0;
			for (int x = _controller.Width - 1; x >= 0; x--)
			{
				for (int y = _controller.Height - 1; y >= 0; y--)
				{
					GridCell cell = _controller.Grid[x, y];
					if (cell.Child.gameObject.activeSelf == false)
					{
						fallCount++;
					}
					else if (fallCount > 0)
					{
						BaseHexagon hexagon = _controller.Grid[x, y + fallCount].Child;
						_controller.Grid[x, y + fallCount].Child = cell.Child;
						cell.Child = hexagon;
						cell.Child.transform.localPosition = Vector3.zero;
						seq.Insert(0,
							_controller.Grid[x, y + fallCount].Child.transform.DOLocalMove(Vector3.zero, .3f)
								.SetDelay((_controller.Height - y) * .1f)
						);
					}
				}

				fallCount = 0;
			}

			seq.OnComplete(() =>
			{
				for (int y = 0; y < _controller.Height; y++)
				{
					for (int x = 0; x < _controller.Width; x++)
					{
						GridCell cell = _controller.Grid[x, y];
						if(cell.Child.gameObject.activeSelf) continue;
						BaseHexagon hexagon = cell.Child;
						hexagon.Color = GetRandomColor(cell);
						hexagon.gameObject.SetActive(true);
						hexagon.Scale(.5f, x * y * 0.01f);
					}
				}
			});
		}

		public void MoveToCentre(BaseHexagon hexagon)
		{
			hexagon.transform.DOLocalMove(Vector3.zero, .33f);
		}
	}
}