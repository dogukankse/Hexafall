using UnityEngine;

namespace Scripts.Components
{
	public class Outline : MonoBehaviour
	{
		[SerializeField] private Transform _cell1;
		[SerializeField] private Transform _cell2;
		[SerializeField] private Transform _cell3;

		public void SetStatus(bool status)
		{
			gameObject.SetActive(status);
		}

		public void SetData(Vector3 pos1, Vector3 pos2, Vector3 pos3)
		{
			_cell1.position = pos1;
			_cell2.position = pos2;
			_cell3.position = pos3;
		}
	}
}