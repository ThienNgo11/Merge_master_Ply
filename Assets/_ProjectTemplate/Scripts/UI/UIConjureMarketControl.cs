using UnityEngine;
using UnityEngine.UI;

namespace Funlab.MergeMaster
{
	// Header button that opens the summon store; shows a red badge while a newly unlocked tier is waiting.
	public class UIConjureMarketControl : MonoBehaviour
	{
		[SerializeField]
		private GameObject _notice;

		private CanvasGroup _canvasCluster;

		private bool _registered;

		private void Awake()
		{
			_canvasCluster = GetComponent<CanvasGroup>();
			Button control = GetComponent<Button>();
			if (control != null)
			{
				control.onClick.AddListener(OnTap);
			}
		}

		private void Start()
		{
			if (!_registered)
			{
				PlayMechanics.onConjureMarketNoticePendingAltered += OnNoticeAltered;
				_registered = true;
			}
			VerifyControlStatus();
			OnNoticeAltered();
		}

		private void OnEnable()
		{
			VerifyControlStatus();
			OnNoticeAltered();
		}

		private void VerifyControlStatus()
		{
			bool shouldPresent = GameTuning.JewelsAllowed;
			if (_canvasCluster != null)
			{
				_canvasCluster.alpha = shouldPresent ? 1f : 0f;
				_canvasCluster.interactable = shouldPresent;
				_canvasCluster.blocksRaycasts = shouldPresent;
			}
		}

		private void OnTap()
		{
			UIDirector uiDirector = UIDirector.Shared;
			if (uiDirector == null)
			{
				return;
			}
			GameplayCore gameplayCore = GameplayCore.Instance;
			if (gameplayCore != null && gameplayCore.activity > ActivityStatus.Unfusing)
			{
				return;
			}
			uiDirector.PresentConjureMarket();
		}

		private void OnDestroy()
		{
			if (_registered)
			{
				PlayMechanics.onConjureMarketNoticePendingAltered -= OnNoticeAltered;
				_registered = false;
			}
		}

		private void OnNoticeAltered()
		{
			if (_notice != null)
			{
				_notice.SetActive(PlayMechanics.conjureMarketNoticePending);
			}
		}
	}
}
