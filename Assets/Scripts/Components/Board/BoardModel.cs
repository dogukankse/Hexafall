using UnityEngine;

namespace Scripts.Components.Board
{
	public class BoardModel : ScriptableObject
	{
		public float CellWidth = 0.39f;
		public float CellHeight = 0.45f;
		
		public GridCell[,] Grid { get; set; }
		public int Width { get; set; } = 8;
		public int Height { get; set; } = 9;

		public Vector2 StartPos { get; set; }
	}
}