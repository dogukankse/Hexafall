using System.Collections.Generic;
using System.Linq;
using Scripts.Utils;
using UnityEngine;

namespace Scripts.ScriptableObjects
{
	[CreateAssetMenu(fileName = "ColorContainer", menuName = "Custom/Color Container", order = 0)]
	public class ColorContainerSo : ScriptableObject
	{
		public List<Color> Colors => _colors;

		[SerializeField] private List<Color> _colors;

		public Color GetRandomColor(params Color[] except)
		{
			return _colors.GetRandom(except.ToList());
		}
	}
}