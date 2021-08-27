using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Scripts.Base;
using Scripts.Utils;
using UnityEngine;
using Yell;

namespace Scripts.Components.Board
{
	public class BoardController
	{
		public int Width => _model.Width;
		public int Height => _model.Height;

		public float CellWidth => _model.CellWidth;
		public float CellHeight => _model.CellHeight;
		public Vector2 StartPos => _model.StartPos;

		public GridCell[,] Grid => _model.Grid;

		private BoardView _view;
		private BoardModel _model;

		public BoardController(BoardView view)
		{
			YellManager.Instance.Listen(YellType.FallCompleted, new YellAction(this, OnFallCompleted));
			_model = ScriptableObject.CreateInstance<BoardModel>();
			_view = view;

			CalcStartPos();
			CreateGrid();
		}


		private void CalcStartPos()
		{
			float offset = 0;
			if (_model.Height / 2 % 2 != 0)
				offset = _model.CellWidth / 2f;


			float x = -_model.CellWidth * (_model.Width / 2f) - offset;
			float y = _model.Height / 4f;

			_model.StartPos = new Vector2(x, y);
		}

		private void CreateGrid()
		{
			_model.Grid = new GridCell[_model.Width, _model.Height];

			for (int y = 0; y < _model.Height; y++)
			{
				for (int x = 0; x < _model.Width; x++)
				{
					Vector2 pos = CalcWorldPos(x, y);
					GridCell cell = _view.CreateCell(pos);
					cell.GridPosition = new Vector2Int(x, y);
					cell.name = $"Cell {x}-{y}";
					_model.Grid[x, y] = cell;
				}
			}
		}

		private Vector2 CalcWorldPos(int x, int y)
		{
			float offset = 0;
			if (x % 2 == 0)
				offset = _model.CellHeight / 2f;

			float xCoord = _model.StartPos.x + x * _model.CellWidth;
			float yCoord = _model.StartPos.y - y * _model.CellHeight + offset;

			return new Vector2(xCoord, yCoord);
		}

		internal List<GridCell> GetNeighbors(GridCell cell)
		{
			var list = new List<GridCell>();
			for (int y = -1; y <= 1; y++)
			{
				for (int x = -1; x <= 1; x++)
				{
					if (x == 0 && y == 0) continue;
					if (cell.GridPosition.x % 2 == 1 && ((x == -1 && y == -1) || (y == 1 && x == -1))) continue;
					if (cell.GridPosition.x % 2 == 0 && ((y == 1 && x == 1) || (y == -1 && x == 1))) continue;
					int xCord = cell.GridPosition.x + x;
					int yCord = cell.GridPosition.y + y;
					try
					{
						if (_model.Grid[xCord, yCord].Child != null)
							list.Add(_model.Grid[xCord, yCord]);
					}
					catch (Exception)
					{
						//ignored
					}
				}
			}


			return list;
		}

		internal List<GridCell> PickNeighbor(GridCell cell, GridCell selectedCell, ref int selectionState)
		{
			List<GridCell> cells = new List<GridCell>();
			Neighbors neighbors = FindNeighbors(selectedCell);

			GridCell n1 = neighbors[selectionState];
			GridCell n2 = neighbors[(selectionState + 1) % 6];

			while (n1 == null || n2 == null)
			{
				selectionState++;
				selectionState %= 6;

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
			if (pos.x % 2 == 1)
			{
				return new Neighbors()
				{
					{0, TryGetCellAtIndex(pos.x, pos.y - 1)},
					{1, TryGetCellAtIndex(pos.x + 1, pos.y)},
					{2, TryGetCellAtIndex(pos.x + 1, pos.y + 1)},
					{3, TryGetCellAtIndex(pos.x, pos.y + 1)},
					{4, TryGetCellAtIndex(pos.x - 1, pos.y + 1)},
					{5, TryGetCellAtIndex(pos.x - 1, pos.y)},
				};
			}

			return new Neighbors()
			{
				{0, TryGetCellAtIndex(pos.x, pos.y - 1)},
				{1, TryGetCellAtIndex(pos.x + 1, pos.y - 1)},
				{2, TryGetCellAtIndex(pos.x + 1, pos.y)},
				{3, TryGetCellAtIndex(pos.x, pos.y + 1)},
				{4, TryGetCellAtIndex(pos.x - 1, pos.y)},
				{5, TryGetCellAtIndex(pos.x - 1, pos.y - 1)},
			};
		}

		private GridCell TryGetCellAtIndex(int x, int y)
		{
			GridCell cell;
			try
			{
				cell = _model.Grid[x, y].Child != null ? _model.Grid[x, y] : null;
			}
			catch (Exception)
			{
				cell = null;
			}

			return cell;
		}

		internal IEnumerator CheckMatch(List<GridCell> selectedCells, bool isClockwise,
			Action<HashSet<GridCell>, int> rotate)
		{
			var n0 = selectedCells[0];
			var n1 = selectedCells[1];
			var n2 = selectedCells[2];
			int turnCount = 0;
			bool stop = false;
			var cellsToExplode = new HashSet<GridCell>();
			while (!stop)
			{
				cellsToExplode.Clear();

				var n0GridItem = n0.Child;
				var n1GridItem = n1.Child;
				var n2GridItem = n2.Child;

				if (isClockwise)
				{
					n0.Child = n2GridItem;
					n1.Child = n0GridItem;
					n2.Child = n1GridItem;

					_view.MoveToCentre(n0GridItem);
					_view.MoveToCentre(n1GridItem);
					_view.MoveToCentre(n2GridItem);

					yield return new WaitForSeconds(.33f);

					var n = FindNeighbors(n0);
					
					var dict = new Dictionary<Color, List<GridCell>>();
					int selectionState = 0;
					foreach (var cell in n)
					{
						if (cell == null) continue;
						var neighbors = PickNeighbor(n0, n0, ref selectionState);
						foreach (var nn in neighbors)
						{
							if (dict.ContainsKey(nn.Child.Color)) dict[nn.Child.Color].Add(nn);
							else dict[nn.Child.Color] = new List<GridCell> {nn};
						}
						
					}

					if (dict.ContainsKey(n0.Child.Color)) dict[n0.Child.Color].Add(n0);

					foreach (var pair in dict)
					{
						if (pair.Value.Count >= 3) cellsToExplode.AddRange(pair.Value);
					}


					// List<GridCell> match = FindMatch(n0);
					// if (match.Contains(n0))
					// 	cellsToExplode.AddRange(match);
					// match = FindMatch(n1);
					// if (match.Contains(n1))
					// 	cellsToExplode.AddRange(match);
					// match = FindMatch(n2);
					// if (match.Contains(n2))
					// 	cellsToExplode.AddRange(match);
				}
				else
				{
					n0.Child = n1GridItem;
					n1.Child = n2GridItem;
					n2.Child = n0GridItem;

					_view.MoveToCentre(n0GridItem);
					_view.MoveToCentre(n1GridItem);
					_view.MoveToCentre(n2GridItem);

					yield return new WaitForSeconds(.33f);

					// List<GridCell> match = FindMatch(n0);
					// if (match.Contains(n0))
					// 	cellsToExplode.AddRange(match);
					// match = FindMatch(n1);
					// if (match.Contains(n1))
					// 	cellsToExplode.AddRange(match);
					// match = FindMatch(n2);
					// if (match.Contains(n2))
					// 	cellsToExplode.AddRange(match);
				}


				turnCount++;
				if (cellsToExplode.Count >= 3 || turnCount >= 3) stop = true;
			}

			rotate(cellsToExplode, turnCount);
		}

		private List<GridCell> FindMatch(GridCell cell)
		{
			Neighbors neighbors = FindNeighbors(cell);
			Dictionary<Color, List<GridCell>> gridCells = CountItems(neighbors);
			List<GridCell> matches = new List<GridCell>();
			if (gridCells.ContainsKey(cell.Child.Color))
				gridCells[cell.Child.Color].Add(cell);
			foreach (var pair in gridCells)
			{
				if (pair.Value.Count >= 3)
				{
					var xList = pair.Value.Select(c => c.GridPosition.x).ToList();
					var yList = pair.Value.Select(c => c.GridPosition.y).ToList();
					//if (xList.Count <= 3 && yList.Count <= 3) 
					matches.AddRange(pair.Value);
				}
			}

			return matches;
		}

		private Dictionary<Color, List<GridCell>> CountItems(Neighbors neighbors)
		{
			Dictionary<Color, List<GridCell>> counts = new Dictionary<Color, List<GridCell>>();
			foreach (GridCell cell in neighbors)
			{
				if (cell == null || !cell.Child.gameObject.activeSelf) continue;
				if (counts.ContainsKey(cell.Child.Color)) counts[cell.Child.Color].Add(cell);
				else counts[cell.Child.Color] = new List<GridCell> {cell};
			}

			return counts;
		}

		private void OnFallCompleted(YellData data)
		{
			// var cellsToExplode = new List<GridCell>();
			// for (int y = 0; y < _model.Height; y++)
			// {
			// 	for (int x = 0; x < _model.Width; x++)
			// 	{
			// 		GridCell cell = _model.Grid[x, y];
			// 		List<GridCell> match = FindMatch(cell);
			// 		if (match.Contains(cell))
			// 			cellsToExplode.AddRange(match);
			// 	}
			// }
			//
			// _view.Pop(cellsToExplode);
		}
	}
}