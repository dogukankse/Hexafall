using System;
using System.Collections;
using System.Collections.Generic;
using Scripts.Base;
using Scripts.ScriptableObjects;
using Scripts.Utils;
using UnityEngine;
using Yell;

namespace Scripts.Components.Board
{
	public class BoardController
	{
		public int Width => _model.Width;
		public int Height => _model.Height;

		public GridCell[,] Grid => _model.Grid;

		public bool SpawnBomb { get; set; }

		private readonly BoardView _view;
		private readonly BoardModel _model;

		public BoardController(BoardView view)
		{
			YellManager.Instance.Listen(YellType.FallCompleted, new YellAction(this, OnFallCompleted));
			YellManager.Instance.Listen(YellType.SpawnBomb, new YellAction(this, OnSpawnBomb));

			BoardSettings boardSettings = Resources.Load<BoardSettings>("Settings/BoardSettings");

			_model = ScriptableObject.CreateInstance<BoardModel>();
			_view = view;

			_model.Width = boardSettings.Width;
			_model.Height = boardSettings.Height;

			CalcStartPos();
			CreateGrid();
		}


		//Calculate first item of board for placement
		private void CalcStartPos()
		{
			float offset = 0;
			if (_model.Height % 2 != 0)
				offset = _model.CellHeight / 2f;


			float x = (_model.Width * _model.CellWidth / -2f) + _model.CellWidth / 2f;
			float y = (_model.Height * _model.CellHeight / 2f) - offset;

			_model.StartPos = new Vector2(x, y);
		}

		//Create the hexagonal grid
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

		//Calculate grid cell position
		private Vector2 CalcWorldPos(int x, int y)
		{
			float offset = 0;
			if (x % 2 == 0)
				offset = _model.CellHeight / 2f;

			float xCoord = _model.StartPos.x + x * _model.CellWidth;
			float yCoord = _model.StartPos.y - y * _model.CellHeight + offset;

			return new Vector2(xCoord, yCoord);
		}

		/// <summary>
		/// Searches the neighbors fot the given cell.
		/// </summary>
		/// <param name="cell">Search the neighbors for</param>
		/// <returns></returns>
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

		/// <summary>
		/// Gets 3 item by <paramref name="selectionState"/> from neighbors. One of them must be <paramref name="selectedCell"/>
		/// </summary>
		/// <returns>Returns selected 3 cell</returns>
		internal List<GridCell> PickNeighbor(GridCell selectedCell, ref int selectionState)
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

			cells.Add(selectedCell);
			cells.Add(n1);
			cells.Add(n2);

			return cells;
		}

		//Try get all neighbors
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

		/// <summary>
		/// Checks if <paramref name="selectedCells"/> items have same color.
		/// </summary>
		/// <param name="selectedCells">Selected items</param>
		/// <param name="isClockwise">For control direction</param>
		/// <param name="onComplete">action to trigger after the checking</param>
		/// <returns></returns>
		internal IEnumerator CheckMatch(List<GridCell> selectedCells, bool isClockwise,
			Action<HashSet<GridCell>> onComplete)
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

					cellsToExplode.AddRange(FindMatch(n0));
					cellsToExplode.AddRange(FindMatch(n1));
					cellsToExplode.AddRange(FindMatch(n2));
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

					cellsToExplode.AddRange(FindMatch(n0));
					cellsToExplode.AddRange(FindMatch(n1));
					cellsToExplode.AddRange(FindMatch(n2));
				}


				turnCount++;
				if (cellsToExplode.Count >= 3 || turnCount >= 3) stop = true;
			}

			onComplete(cellsToExplode);
		}

		/// <summary>
		/// Counts items by colors
		/// </summary>
		/// <param name="cell">Centre cell</param>
		/// <returns></returns>
		private List<GridCell> FindMatch(GridCell cell)
		{
			var cellsToExplode = new List<GridCell>();
			var neighbors = FindNeighbors(cell);
			for (int i = 0; i < 6; i++)
			{
				if (neighbors[i] == null || neighbors[(i + 1) % 6] == null) continue;
				if (neighbors[i].Child.Color == neighbors[(i + 1) % 6].Child.Color &&
					neighbors[i].Child.Color == cell.Child.Color)
				{
					cellsToExplode.Add(neighbors[i]);
					cellsToExplode.Add(neighbors[(i + 1) % 6]);
					cellsToExplode.Add(cell);
				}
			}

			return cellsToExplode;
		}


		//On fall anims completed
		private void OnFallCompleted(YellData data)
		{
			var cellsToExplode = new HashSet<GridCell>();
			for (int y = 0; y < _model.Height; y++)
			{
				for (int x = 0; x < _model.Width; x++)
				{
					GridCell cell = _model.Grid[x, y];
					cellsToExplode.AddRange(FindMatch(cell));
				}
			}

			if (cellsToExplode.Count > 0)
				_view.Pop(cellsToExplode);
			else
				YellManager.Instance.Yell(YellType.AllowSwipe);
		}


		//On bomb spawn tiggered
		private void OnSpawnBomb(YellData arg0)
		{
			SpawnBomb = true;
		}
	}
}