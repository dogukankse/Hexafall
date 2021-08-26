using System.Collections.Generic;

namespace Scripts.Utils
{
	public static class Helpers
	{
		public static void Swap<T>(ref T t1, ref T t2)
		{
			T temp = t1;
			t1 = t2;
			t2 = temp;
		}

		public static void Swap<T>(T t1, T t2)
		{
			T temp = t1;
			t1 = t2;
			t2 = temp;
		}
	}
}