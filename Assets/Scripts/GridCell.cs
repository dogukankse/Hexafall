using Scripts.Base;
using UnityEngine;

namespace Scripts
{
	public class GridCell : MonoBehaviour
	{
		public BaseHexagon Child
		{
			get => _child;
			set
			{
				_child = value;
				_child.transform.SetParent(transform);

			}
		}

		public Vector2Int GridPosition { get; set; }
		private BaseHexagon _child;
	}
}