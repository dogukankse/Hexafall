using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Yell;

public class UIController : MonoBehaviour
{
	[SerializeField] private Canvas _canvas;
	[SerializeField] private Button _refreshButton;
	[SerializeField] private Button _pauseButton;
	[SerializeField] private RectTransform _pausePanel;


	private VerticalLayoutGroup _layoutGroup;
	private RectTransform _safeAreaTransform;

	private void Awake()
	{
		_safeAreaTransform = GetComponent<RectTransform>();
		_layoutGroup = GetComponent<VerticalLayoutGroup>();
		_pauseButton.onClick.AddListener(OnPauseButtonClicked);
	}

	private void Start()
	{
		//_layoutGroup.enabled = false;
		ApplySafeArea();
	}

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
		if (_pausePanel.sizeDelta.x == 0)
		{
			YellManager.Instance.Yell(YellType.BlockTouch);
			_pausePanel.DOSizeDelta(new Vector2(Screen.width, _pausePanel.sizeDelta.y), .2f).OnComplete(() =>
			{
				_refreshButton.onClick.AddListener(OnRefreshButton);
			});
		}
		else
		{
			_refreshButton.onClick.RemoveListener(OnRefreshButton);
			_pausePanel.DOSizeDelta(new Vector2(0, _pausePanel.sizeDelta.y), .2f).OnComplete(() =>
			{
				YellManager.Instance.Yell(YellType.UnblockTouch);

			});
		}
	}

	private void OnRefreshButton()
	{
		SceneManager.LoadScene(0);
	}
}