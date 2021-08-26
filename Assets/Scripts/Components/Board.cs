using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Scripts.Base;
using Scripts.ScriptableObjects;
using Scripts.Utils;
using UnityEngine;
using Yell;

namespace Scripts.Components
{
	public class Board : MonoBehaviour
	{
		public float CellWidth = 0.39f;
		public float CellHeight = 0.45f;

		[SerializeField] private Hexagon _hexagonPrefab;
		[SerializeField] private GridCell _gridCellPrefab;
		[SerializeField] private ColorContainerSo _colorContainer;
		[SerializeField] private GameObject _dot;
		[SerializeField] private Outline _outline;

		private GridCell[,] _grid;
		private Hexagon[,] _hexagons;
		private Vector2 _startPos;
		private int _width = 8;
		private int _height = 9;
		private List<GridCell> _selectedCells;
		private bool _stop;


		private void Awake()
		{
			YellManager.Instance.Listen(YellType.OnTouch, new YellAction(this, OnTouch));
			YellManager.Instance.Listen(YellType.OnSwipe, new YellAction(this, OnSwipe));
			_selectedCells = new List<GridCell>();

			CalcStartPos();
			CreateGrid();
			CreateHexagons();
		}


		private void CreateGrid()
		{
			_grid = new GridCell[_width, _height];

			for (int y = 0; y < _height; y++)
			{
				for (int x = 0; x < _width; x++)
				{
					Vector2 pos = CalcWorldPos(x, y);
					GridCell cell = Instantiate(_gridCellPrefab, pos, Quaternion.identity, transform);
					cell.GridPosition = new Vector2Int(x, y);
					cell.name = $"Cell {x}-{y}";
					_grid[x, y] = cell;
				}
			}
		}

		private void CreateHexagons()
		{
			for (int y = 0; y < _height; y++)
			{
				for (int x = 0; x < _width; x++)
				{
					GridCell cell = _grid[x, y];
					Hexagon hexagon = Instantiate(_hexagonPrefab, cell.transform);
					hexagon.transform.localPosition = Vector3.zero;
					cell.Child = hexagon;
					hexagon.Color = GetColor(cell);
					hexagon.Scale(.5f, x * y * 0.01f);
				}
			}
		}

		private Color GetColor(GridCell cell)
		{
			List<GridCell> neighbors = GetNeighbors(cell);
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

		private List<GridCell> GetNeighbors(GridCell cell)
		{
			var list = new List<GridCell>();
			for (int y = -1; y <= 1; y++)
			{
				for (int x = -1; x <= 1; x++)
				{
					if (x == 0 && y == 0) continue;
					if (cell.GridPosition.y % 2 == 1 && ((y == -1 && x == -1) || (y == 1 && x == -1))) continue;
					if (cell.GridPosition.y % 2 == 0 && ((y == 1 && x == 1) || (y == -1 && x == 1))) continue;
					int xCord = cell.GridPosition.x + x;
					int yCord = cell.GridPosition.y + y;
					try
					{
						if (_grid[xCord, yCord].Child != null)
							list.Add(_grid[xCord, yCord]);
					}
					catch (Exception)
					{
						//ignored
					}
				}
			}


			return list;
		}

		private GridCell TryGetCellAtIndex(int x, int y)
		{
			GridCell cell;
			try
			{
				cell = _grid[x, y].Child != null ? _grid[x, y] : null;
			}
			catch (Exception)
			{
				cell = null;
			}

			return cell;
		}

		private void CalcStartPos()
		{
			float offset = 0;
			if (_height / 2 % 2 != 0)
				offset = CellWidth / 2f;


			float x = -CellWidth * (_width / 2f) - offset;
			float y = (_height / 4f);

			_startPos = new Vector2(x, y);
		}

		private Vector2 CalcWorldPos(int x, int y)
		{
			float offset = 0;
			if (x % 2 == 0)
				offset = CellHeight / 2f;

			float xCoord = _startPos.x + x * CellWidth;
			float yCoord = _startPos.y - y * CellHeight + offset;

			return new Vector2(xCoord, yCoord);
		}

		private GridCell _selectedCell;
		private int _selectionState;

		private List<GridCell> PickNeighbor(GridCell cell, ref int selectionState)
		{
			List<GridCell> cells = new List<GridCell>();
			Neighbors neighbors = FindNeighbors(_selectedCell);


			GridCell n1 = neighbors[selectionState];
			GridCell n2 = neighbors[(selectionState + 1) % 6];

			while (n1 == null || n2 == null)
			{
				selectionState++;
				selectionState %= 6;
				print(selectionState);

				n1 = neighbors[selectionState];
				n2 = neighbors[(selectionState + 1) % 6];
			}

			cells.Add(cell);
			cells.Add(n1);
			cells.Add(n2);

			return cells;
		}

		private Neighbors FindNeighbors(GridCell cell)
		{
			var pos = cell.GridPosition;
			if (pos.y % 2 == 1)
			{
				return new Neighbors()
				{
					{0, TryGetCellAtIndex(pos.x, pos.y - 1)},
					{1, TryGetCellAtIndex(pos.x + 1, pos.y - 1)},
					{2, TryGetCellAtIndex(pos.x + 1, pos.y)},
					{3, TryGetCellAtIndex(pos.x + 1, pos.y + 1)},
					{4, TryGetCellAtIndex(pos.x, pos.y + 1)},
					{5, TryGetCellAtIndex(pos.x - 1, pos.y)},
				};
			}

			return new Neighbors()
			{
				{0, TryGetCellAtIndex(pos.x - 1, pos.y - 1)},
				{1, TryGetCellAtIndex(pos.x, pos.y - 1)},
				{2, TryGetCellAtIndex(pos.x + 1, pos.y)},
				{3, TryGetCellAtIndex(pos.x, pos.y + 1)},
				{4, TryGetCellAtIndex(pos.x - 1, pos.y + 1)},
				{5, TryGetCellAtIndex(pos.x - 1, pos.y)},
			};
		}


		private void OnTouch(YellData data)
		{
			GridCell cell = (GridCell) data.data;
			foreach (var c in _selectedCells)
			{
				c.transform.SetParent(transform);
			}

			_selectedCells.Clear();

			if (_selectedCell == cell)
			{
				_selectionState++;
				_selectionState %= 6;
				print(_selectionState);
			}
			else
			{
				_selectedCell = cell;
				_selectionState = 0;
			}

			_selectedCells = PickNeighbor(cell, ref _selectionState);


			_dot.transform.localPosition = (_selectedCells[0].transform.localPosition +
				_selectedCells[1].transform.localPosition +
				_selectedCells[2].transform.localPosition) / 3f;
			_dot.SetActive(true);

			_outline.SetData(_selectedCells[0].transform.localPosition, _selectedCells[1].transform.localPosition,
				_selectedCells[2].transform.localPosition);
			_outline.SetStatus(true);

			foreach (var c in _selectedCells)
			{
				c.transform.SetParent(_dot.transform);
			}

			_outline.transform.SetParent(_dot.transform);
		}

		private void OnSwipe(YellData data)
		{
			Vector2 dir = (Vector2) data.data;
			bool isClockwise = !(dir.x == -1 || dir.y == -1);

			var n0 = _selectedCells[0];
			var n1 = _selectedCells[1];
			var n2 = _selectedCells[2];
			int turnCount = 0;
			List<GridCell> cellToExplode = new List<GridCell>();

			while (!_stop)
			{
				cellToExplode.Clear();

				var n0GridItem = _grid[n0.GridPosition.x, n0.GridPosition.y];
				var n1GridItem = _grid[n1.GridPosition.x, n1.GridPosition.y];
				var n2GridItem = _grid[n2.GridPosition.x, n2.GridPosition.y];

				var n0Pos = n0.GridPosition;
				var n1Pos = n1.GridPosition;
				var n2Pos = n2.GridPosition;

				if (isClockwise)
				{
					_grid[n0.GridPosition.x, n0.GridPosition.y] = n2GridItem;
					_grid[n1.GridPosition.x, n1.GridPosition.y] = n0GridItem;
					_grid[n2.GridPosition.x, n2.GridPosition.y] = n1GridItem;

					n0.GridPosition = n2Pos;
					n1.GridPosition = n0Pos;
					n2.GridPosition = n1Pos;

					n0 = _selectedCells[(2 + turnCount) % 3];
					n1 = _selectedCells[turnCount % 3];
					n2 = _selectedCells[(1 + turnCount) % 3];

					List<GridCell> match = FindMatch(n0);
					if (match.Contains(n0))
						cellToExplode.AddRange(match);
					match = FindMatch(n1);
					if (match.Contains(n1))
						cellToExplode.AddRange(match);
					match = FindMatch(n2);
					if (match.Contains(n2))
						cellToExplode.AddRange(match);
				}
				else
				{
					_grid[n0.GridPosition.x, n0.GridPosition.y] = n1GridItem;
					_grid[n1.GridPosition.x, n1.GridPosition.y] = n2GridItem;
					_grid[n2.GridPosition.x, n2.GridPosition.y] = n0GridItem;

					n0.GridPosition = n1Pos;
					n1.GridPosition = n2Pos;
					n2.GridPosition = n0Pos;

					n0 = _selectedCells[(1 + turnCount) % 3];
					n1 = _selectedCells[(2 + turnCount) % 3];
					n2 = _selectedCells[turnCount % 3];

					List<GridCell> match = FindMatch(n0);
					if (match.Contains(n0))
						cellToExplode.AddRange(match);
					match = FindMatch(n1);
					if (match.Contains(n1))
						cellToExplode.AddRange(match);
					match = FindMatch(n2);
					if (match.Contains(n2))
						cellToExplode.AddRange(match);
				}


				turnCount++;
				print($"{turnCount} : {cellToExplode.Count} : {isClockwise}");
				if (cellToExplode.Count >= 3 || turnCount >= 3) _stop = true;
			}


			_dot.transform.DOLocalRotate(Vector3.forward * ((isClockwise ? 1 : -1) * 120 * turnCount), 1f,
					RotateMode.FastBeyond360)
				.SetRelative().OnComplete(() =>
				{
					foreach (var cell in cellToExplode)
					{
						cell.transform.DOScale(Vector3.zero, 1f);
					}

					foreach (var c in _selectedCells)
					{
						c.transform.SetParent(transform);
					}

					_selectedCells.Clear();

					_dot.SetActive(false);

					YellManager.Instance.Yell(YellType.OnRotationEnd);
					_stop = false;
				});
		}

		private List<GridCell> FindMatch(GridCell cell)
		{
			Neighbors neighbors = FindNeighbors(cell);
			Dictionary<Color, List<GridCell>> gridCells = CountItems(neighbors);
			List<GridCell> matches = new List<GridCell>();
			foreach (var pair in gridCells)
			{
				if (pair.Value.Count >= 3)
				{
					var xList = pair.Value.Select(c => c.GridPosition.x).ToList();
					var yList = pair.Value.Select(c => c.GridPosition.y).ToList();
					if (xList.Count <= 3 && yList.Count <= 3) matches.AddRange(pair.Value);
					print(xList.Aggregate("", (c, i) => c += i + " "));
					print(yList.Aggregate("", (c, i) => c += i + " "));
				}
			}

			return matches;
		}

		private Dictionary<Color, List<GridCell>> CountItems(Neighbors neighbors)
		{
			Dictionary<Color, List<GridCell>> counts = new Dictionary<Color, List<GridCell>>();
			foreach (GridCell cell in neighbors)
			{
				if (cell == null) continue;
				if (counts.ContainsKey(cell.Child.Color)) counts[cell.Child.Color].Add(cell);
				else counts[cell.Child.Color] = new List<GridCell> {cell};
			}

			return counts;
		}
	}
}