using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Scripts.Utils
{
	public static class Extensions
	{
		public static T GetRandom<T>(this List<T> list, List<T> except = null)
		{
			var localList = new List<T>(list);

			if (except != null)
			{
				localList.RemoveAll(except.Contains);
			}

			int index = Random.Range(0, localList.Count);
			return localList[index];
		}

		/// <summary>
		/// Add range for HashSet
		/// </summary>
		/// <param name="set">The HashSet itself</param>
		/// <param name="items">items to be added</param>
		/// <typeparam name="T">Type of the HashSet's items</typeparam>
		public static void AddRange<T>(this HashSet<T> set, IEnumerable<T> items)
		{
			if (items == null) return;
			foreach (var item in items)
			{
				set.Add(item);
			}
		}
		
	}
}