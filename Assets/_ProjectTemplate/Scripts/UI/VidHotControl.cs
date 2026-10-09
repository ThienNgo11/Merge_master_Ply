using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Funlab.MergeMaster
{
	public class VidHotControl : MonoBehaviour
	{
		private const float DialShrinkSpan = 0.16f;

		[SerializeField]
		private UICircle dial;

		[SerializeField]
		private float dialSecs;

		[SerializeField]
		private DOTweenAnimation dialSizeMotion;

		private TweenerCore<float, float, FloatOptions> _activeTransition;

		private Tween _timeoutCall;

		// The clock's authored scale (0.4 in GamePlay.unity, c_072: ~40 px pie); DORewind/expiry tweens scale it, Show restores it.
		private Vector3 _dialSize = Vector3.zero;

		private void Awake()
		{
			if (dial != null && _dialSize == Vector3.zero)
			{
				_dialSize = dial.transform.localScale;
			}
		}

		public void Present()
		{
			if (dial != null && _dialSize == Vector3.zero)
			{
				_dialSize = dial.transform.localScale;
			}
			gameObject.SetActive(true);
			if (dialSizeMotion != null)
			{
				dialSizeMotion.DORewind();
			}
			if (dial != null)
			{
				if (_dialSize == Vector3.zero)
				{
					_dialSize = dial.transform.localScale;
				}
				dial.transform.localScale = _dialSize;
				dial.SetProgress(1f);
			}
			BeginDial();
			UIDirector uiDirector = UIDirector.Shared;
			if (uiDirector != null && uiDirector.gameplayCore != null && uiDirector.gameplayCore.audioDirector != null)
			{
				uiDirector.gameplayCore.audioDirector.PerformUICue(AudioCue.ChampionAccessible);
			}
		}

		public void Conceal()
		{
			TerminateTransitions();
			gameObject.SetActive(false);
		}

		public void BeginDial()
		{
			TerminateTransitions();
			if (dial == null)
			{
				return;
			}
			float span = dialSecs > 0f ? dialSecs : 30f;
			_activeTransition = DOTween.To(() => dial.Progress, portion => dial.SetProgress(portion), 0f, span).SetEase(Ease.Linear);
			_activeTransition.OnComplete(OnDialLapsed);
		}

		private void OnDialLapsed()
		{
			_activeTransition = null;
			if (dialSizeMotion != null)
			{
				dialSizeMotion.DORestart();
			}
			_timeoutCall = DOVirtual.DelayedCall(DialShrinkSpan, () =>
			{
				_timeoutCall = null;
				Conceal();
				UIDirector uiDirector = UIDirector.Shared;
				if (uiDirector != null)
				{
					uiDirector.ReviseGamerDirector(false);
				}
			});
		}

		private void TerminateTransitions()
		{
			if (_activeTransition != null)
			{
				_activeTransition.Kill();
				_activeTransition = null;
			}
			if (_timeoutCall != null)
			{
				_timeoutCall.Kill();
				_timeoutCall = null;
			}
		}

		private void OnDestroy()
		{
			TerminateTransitions();
		}
	}
}
