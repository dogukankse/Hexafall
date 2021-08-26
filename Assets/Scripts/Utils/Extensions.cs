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

		public static void AddRange<T>(this HashSet<T> set, IEnumerable<T> enumerable)
		{
			if (enumerable == null) return;
			foreach (var item in enumerable)
			{
				set.Add(item);
			}
		}

		public static Color SetAlpha(this Color c, float a)
		{
			return new Color(c.r, c.g, c.b, a);
		}
	}
}