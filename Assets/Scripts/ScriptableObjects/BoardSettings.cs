using UnityEngine;

namespace Scripts.ScriptableObjects
{
	[CreateAssetMenu(fileName = "BoardSettings", menuName = "Custom/BoardSettings", order = 0)]
	public class BoardSettings : ScriptableObject
	{
		public int Width;
		public int Height;
	}
}