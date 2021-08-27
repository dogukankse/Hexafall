using System;
using Scripts.Base;
using TMPro;
using UnityEngine;
using Yell;

namespace Scripts.Components
{
	/// <summary>
	/// Bomb Hexagon item
	/// </summary>
	public class Bomb : BaseHexagon
	{
		[SerializeField] private Canvas _canvas;
		[SerializeField] private TextMeshProUGUI _countText;

		private int Counter
		{
			get => _counter;
			set
			{
				_counter = value;
				_countText.text = _counter + "";
				if (_counter == 0)
					Explode();
			}
		}

		private void Explode()
		{
			YellManager.Instance.Yell(YellType.GameEnd);
		}

		private int _counter = 6;

		public override void Awake()
		{
			base.Awake();
			_canvas.worldCamera = Camera.main;
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			YellManager.Instance.Listen(YellType.OnRotate, new YellAction(this, OnMove));
			Counter = 6;
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			YellManager.Instance.Unlisten(YellType.OnRotate, this);
		}

		private void OnMove(YellData arg0)
		{
			Counter--;
		}
	}
}