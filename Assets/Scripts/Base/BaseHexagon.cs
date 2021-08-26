using DG.Tweening;
using UnityEngine;

namespace Scripts.Base
{
	public abstract class BaseHexagon : MonoBehaviour
	{
		public Color Color
		{
			get => _renderer.color;
			set => _renderer.color = value;
		}

		private SpriteRenderer _renderer;


		private void Awake()
		{
			_renderer = GetComponent<SpriteRenderer>();
		}

		private void OnEnable()
		{
			transform.localScale = Vector3.zero;
		}
		
		public void Scale(float duration,float delay)
		{
			transform.DOScale(Vector3.one, duration)
				.SetDelay(delay);
		}
	}
}