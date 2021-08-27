using DG.Tweening;
using UnityEngine;
using Yell;

namespace Scripts.Base
{
	public abstract class BaseHexagon : MonoBehaviour
	{
		public enum State
		{
			Hexagon,
			Bomb
		}

		public State HexagonState
		{
			get => _hexagonState;
			set { _hexagonState = value; }
		}

		private State _hexagonState = State.Hexagon;

		public Color Color
		{
			get => _renderer.color;
			set => _renderer.color = value;
		}

		private SpriteRenderer _renderer;


		public virtual void Awake()
		{
			_renderer = GetComponent<SpriteRenderer>();
		}

		protected virtual void OnEnable()
		{
			transform.localScale = Vector3.zero;
		}

		protected virtual void OnDisable()
		{
		}

		public void Scale(float duration, float delay)
		{
			transform.DOScale(Vector3.one, duration)
				.SetDelay(delay);
		}
	}
}