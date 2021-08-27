using Scripts.Components;
using UnityEngine;
using Yell;

namespace Scripts.Managers
{
	public class InputManager : MonoBehaviour
	{
		[SerializeField] private float _treshold = .3f;
		private Camera _cam;
		private Vector2 _firstTouch;
		private bool _isRotating;
		private bool _blockTouch;
		private bool _allowSwipe;

		private void Awake()
		{
			YellManager.Instance.Listen(YellType.OnRotationEnd, new YellAction(this, OnRotationEnd));
			YellManager.Instance.Listen(YellType.BlockTouch, new YellAction(this, OnBlockTouch));
			YellManager.Instance.Listen(YellType.UnblockTouch, new YellAction(this, OnUnblockTouch));
			YellManager.Instance.Listen(YellType.AllowSwipe, new YellAction(this, OnAllowSwipe));
			YellManager.Instance.Listen(YellType.CreationComplete, new YellAction(this, OnAllowSwipe));

			_cam = Camera.main;
		}


		private void Update()
		{
			if (_blockTouch || !_allowSwipe) return;
			if (Input.touchCount > 0)
			{
				Touch touch = Input.GetTouch(0);
				if (_isRotating) return;
				switch (touch.phase)
				{
					case TouchPhase.Began:
						_firstTouch = touch.position;
						break;
					case TouchPhase.Moved:
						//calculate the finger move delta and send it
						Vector2 touchPos = touch.position;
						Vector2 delta = new Vector2(touchPos.x - _firstTouch.x, touchPos.y - _firstTouch.y);
						Vector2 dir = Vector2.zero;
						if (Mathf.Abs(delta.x) > _treshold)
						{
							dir.x = Mathf.Sign(delta.x);
						}
						else if (Mathf.Abs(delta.y) > _treshold)
						{
							dir.y = Mathf.Sign(delta.y);
						}

						if (dir != Vector2.zero)
						{
							YellManager.Instance.Yell(YellType.OnSwipe, new YellData(dir));
							_isRotating = true;
							_allowSwipe = false;
						}

						break;
					case TouchPhase.Ended:
						if (touch.deltaPosition.magnitude <= _treshold)
						{
							Vector3 worldPos = _cam.ScreenToWorldPoint(touch.position);
							Collider2D item = Physics2D.OverlapPoint(new Vector2(worldPos.x, worldPos.y));
							if (item == null) return;
							item.TryGetComponent(out GridCell cell);
							if (cell)
							{
								YellManager.Instance.Yell(YellType.OnTouch, new YellData(cell));
								print(cell.name);
							}
						}

						break;
				}
			}
		}

		private void OnRotationEnd(YellData arg0)
		{
			_isRotating = false;
		}

		private void OnUnblockTouch(YellData arg0)
		{
			_blockTouch = false;
		}

		private void OnBlockTouch(YellData arg0)
		{
			_blockTouch = true;
		}

		private void OnAllowSwipe(YellData arg0)
		{
			_allowSwipe = true;
		}
	}
}