using DG.Tweening;
using GameToolSample.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Funlab.MergeMaster
{
	public class UIControlReaction : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler
	{
		private const float PressedSize = 0.92f;

		private const float PressSpan = 0.08f;

		private const float ReleaseSpan = 0.12f;

		private static AudioDirector _audioDirector;

		[SerializeField]
		private AudioCue audioCue;

		[SerializeField]
		private eSoundName tailoredSample;

		[SerializeField]
		public bool dontTransition;

		private Button _control;

		private Tween _activeTransition;

		private Vector4 _baselinePadding;

		private DOTweenAnimation _transitionMotion;

		private Animation _motion;

		private bool _isTweening;

		private Graphic _drawable;

		private bool _transitionMotionWasPerforming;

		private bool _motionWasPerforming;

		private Vector3 _restingSize;

		private void Awake()
		{
			_restingSize = transform.localScale;
			_control = GetComponent<Button>();
			_transitionMotion = GetComponent<DOTweenAnimation>();
			_motion = GetComponent<Animation>();
			_drawable = GetComponent<Image>();
			if (_drawable == null)
			{
				_drawable = GetComponent<Graphic>();
			}
#if !UNITY_LUNA
			_baselinePadding = _drawable != null ? _drawable.raycastPadding : Vector4.zero;
#endif
		}

		public void OnPointerDown(PointerEventData signalInfo)
		{
			if (_control != null && !_control.interactable)
			{
				return;
			}
			PerformAudio();
			Toolbox.PerformTactileImpact(ImpactFeedbackStyle.Light);
			if (dontTransition || _isTweening)
			{
				return;
			}
			_transitionMotionWasPerforming = _transitionMotion != null && _transitionMotion.tween != null && _transitionMotion.tween.IsActive() && _transitionMotion.tween.IsPlaying();
			if (_transitionMotionWasPerforming)
			{
				_transitionMotion.DOPause();
			}
			_motionWasPerforming = _motion != null && _motion.isPlaying;
			if (_motionWasPerforming)
			{
				_motion.Stop();
			}
			_activeTransition?.Kill();
			_activeTransition = transform.DOScale(_restingSize * PressedSize, PressSpan).SetUpdate(true);
#if !UNITY_LUNA
			if (_drawable != null)
			{
				Vector2 extent = _drawable.rectTransform.rect.size;
				float inflate = (1f - PressedSize) * 0.5f;
				_drawable.raycastPadding = _baselinePadding - new Vector4(extent.x * inflate, extent.y * inflate, extent.x * inflate, extent.y * inflate);
			}
#endif
			_isTweening = true;
		}

		public void OnPointerUp(PointerEventData signalInfo)
		{
			if (dontTransition || !_isTweening)
			{
				return;
			}
			LetGo(true);
		}

		private void OnDisable()
		{
			if (_isTweening)
			{
				LetGo(false);
			}
		}

		private void LetGo(bool withTransition)
		{
			_isTweening = false;
			_activeTransition?.Kill();
			_activeTransition = null;
			if (withTransition && isActiveAndEnabled)
			{
				_activeTransition = transform.DOScale(_restingSize, ReleaseSpan).SetEase(Ease.OutBack).SetUpdate(true);
			}
			else
			{
				transform.localScale = _restingSize;
			}
#if !UNITY_LUNA
			if (_drawable != null)
			{
				_drawable.raycastPadding = _baselinePadding;
			}
#endif
			if (_transitionMotionWasPerforming && _transitionMotion != null)
			{
				_transitionMotion.DOPlay();
			}
			if (_motionWasPerforming && _motion != null && isActiveAndEnabled)
			{
				_motion.Play();
			}
			_transitionMotionWasPerforming = false;
			_motionWasPerforming = false;
		}

		private void PerformAudio()
		{
			if (_audioDirector == null)
			{
				UIDirector uiDirector = UIDirector.Shared;
				if (uiDirector != null && uiDirector.gameplayCore != null)
				{
					_audioDirector = uiDirector.gameplayCore.audioDirector;
				}
				if (_audioDirector == null)
				{
					_audioDirector = FindObjectOfType<AudioDirector>();
				}
			}
			if (_audioDirector == null)
			{
				return;
			}
			if (tailoredSample != eSoundName.None)
			{
				_audioDirector.PerformUISample(tailoredSample);
			}
			else if (audioCue != AudioCue.Unset)
			{
				_audioDirector.PerformUICue(audioCue);
			}
		}
	}
}
