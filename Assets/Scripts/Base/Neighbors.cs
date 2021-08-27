using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Base
{
	public class Neighbors : IEnumerable<GridCell>
	{
		private readonly Dictionary<int, GridCell> _dict = new Dictionary<int, GridCell>();
		public GridCell this[int i] => _dict[i];


		public void Add(int order, GridCell cell)
		{
			_dict.Add(order, cell);
		}

		public IEnumerator<GridCell> GetEnumerator()
		{
			return _dict.Values.ToList().GetEnumerator();
		}


		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}