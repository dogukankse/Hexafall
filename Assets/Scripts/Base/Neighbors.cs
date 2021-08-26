using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Base
{
	public class Neighbors : IEnumerable
	{
		private readonly Dictionary<int, GridCell> _dict = new Dictionary<int, GridCell>();

		public void Add(int order, GridCell cell)
		{
			_dict.Add(order, cell);
		}

		public IEnumerator GetEnumerator()
		{
			return _dict.Values.ToList().GetEnumerator();
		}

		public GridCell this[int i]
		{
			get => _dict[i];
			set => _dict[i] = value;
		}
	}
}