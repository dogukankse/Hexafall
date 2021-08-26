using System.Collections.Generic;
using System.Linq;

namespace Yell
{
	public class YellManager
	{
		public static YellManager Instance => _instance ?? (_instance = new YellManager());
		private static YellManager _instance;

		private readonly Dictionary<YellType, IList<YellAction>> _yells =
			new Dictionary<YellType, IList<YellAction>>();

		public void Yell(YellType yellType, YellData yellData = null)
		{
			//if (!_yells.ContainsKey(yellType)) return;

			foreach (var yell in _yells[yellType])
			{
				yell.action?.Invoke(yellData);
			}
		}

		/// <summary>
		/// Starts the listening process and register an action
		/// </summary>
		/// <param name="yellType">Type of the action</param>
		/// <param name="yellAction">An action to run when the yell heard</param>
		public void Listen(YellType yellType, YellAction yellAction)
		{
			if (_yells.ContainsKey(yellType))
			{
				_yells[yellType].Add(yellAction);
			}
			else
			{
				_yells.Add(yellType, new List<YellAction> {yellAction});
			}
		}

		/// <summary>
		/// Stops the listening process
		/// </summary>
		/// <param name="yellType">Type of the yell</param>
		/// <param name="sender">Sender of the yell</param>
		public void Unlisten(YellType yellType, object sender)
		{
			YellAction yellActionToRemove = _yells[yellType].FirstOrDefault(x => x.listener == sender);
			if (yellActionToRemove == null) return;
			_yells[yellType].Remove(yellActionToRemove);
		}
	}
}