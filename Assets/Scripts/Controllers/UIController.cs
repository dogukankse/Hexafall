using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Yell;

namespace Scripts.Controllers
{
	public class UIController : MonoBehaviour
	{
		[SerializeField] private Canvas _canvas;
		[SerializeField] private Button _refreshButton;
		[SerializeField] private Button _refreshButtonFromGameEnd;
		[SerializeField] private RectTransform _endGamePanel;
		[SerializeField] private Button _pauseButton;
		[SerializeField] private RectTransform _pausePanel;
		[SerializeField] private TextMeshProUGUI _scoreText;
		[SerializeField] private TextMeshProUGUI _moveText;


		private RectTransform _safeAreaTransform;

		private int _targetScore = 1000;

		private void Awake()
		{
			YellManager.Instance.Listen(YellType.OnPop, new YellAction(this, UpdateScore));
			YellManager.Instance.Listen(YellType.OnRotate, new YellAction(this, UpdateMoveCount));
			YellManager.Instance.Listen(YellType.GameEnd, new YellAction(this, OnGameEnd));
			_safeAreaTransform = GetComponent<RectTransform>();
			_pauseButton.onClick.AddListener(OnPauseButtonClicked);
			_refreshButtonFromGameEnd.onClick.AddListener(OnRefreshButton);

		}


		private void UpdateMoveCount(YellData arg0)
		{
			_moveText.text = (int.Parse(_moveText.text) + 1) + "";
		}

		private void UpdateScore(YellData data)
		{
			_scoreText.text = int.Parse(_scoreText.text) + (int) data.data * 5 + "";
			if (int.Parse(_scoreText.text) > _targetScore)
			{
				YellManager.Instance.Yell(YellType.SpawnBomb);
				_targetScore += 1000;
			}
		}


		private void Start()
		{
			//_layoutGroup.enabled = false;
			ApplySafeArea();
		}

		//for "notched" phones
		private void ApplySafeArea()
		{
			var safeArea = Screen.safeArea;

			var anchorMin = safeArea.position;
			var anchorMax = safeArea.position + safeArea.size;
			anchorMin.x /= _canvas.pixelRect.width;
			anchorMin.y /= _canvas.pixelRect.height;
			anchorMax.x /= _canvas.pixelRect.width;
			anchorMax.y /= _canvas.pixelRect.height;

			_safeAreaTransform.anchorMin = anchorMin;
			_safeAreaTransform.anchorMax = anchorMax;
		}

		private void OnPauseButtonClicked()
		{
			if (!_pausePanel.gameObject.activeSelf)
			{
				YellManager.Instance.Yell(YellType.BlockTouch);
				_pausePanel.gameObject.SetActive(true);
				_refreshButton.onClick.AddListener(OnRefreshButton);
			}
			else
			{
				_refreshButton.onClick.RemoveListener(OnRefreshButton);
				_pausePanel.gameObject.SetActive(false);
				YellManager.Instance.Yell(YellType.UnblockTouch);
			}
		}

		private void OnRefreshButton()
		{
			SceneManager.LoadScene(0);
		}

		private void OnGameEnd(YellData arg0)
		{
			YellManager.Instance.Yell(YellType.BlockTouch);
			_endGamePanel.DOScale(Vector3.one, .3f);
		}
	}
}