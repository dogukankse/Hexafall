using UnityEngine.Events;

namespace Yell
{
	/// <summary>
	/// UnityAction wrapper.
	/// Contains UnityAction with a parameter and listener; 
	/// </summary>
	public sealed class YellAction
	{
		public readonly object listener;
		public readonly UnityAction<YellData> action;

		public YellAction(object listener, UnityAction<YellData> action)
		{
			this.listener = listener;
			this.action = action;
		}
	}
}