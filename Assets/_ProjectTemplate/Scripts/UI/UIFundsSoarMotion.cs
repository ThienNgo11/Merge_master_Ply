using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Funlab.MergeMaster
{
	public class UIFundsSoarMotion : MonoBehaviour
	{
		private const float DispatchStagger = 0.015f;

		private const float ReboundSpan = 0.25f;

		private const float ReboundStrength = 0.15f;

		private const float PennyExtent = 90f;

		private const float PennyAudioGap = 0.1f;

		[SerializeField]
		private Sprite fundsIcon;

		[Header("UI")]
		[SerializeField]
		private RectTransform fundsHolder;

		[SerializeField]
		private RectTransform fundsGlyph;

		[SerializeField]
		private Image fundsArtwork;

		[SerializeField]
		private TMP_Text activeSumCaption;

		[Header("Animation")]
		[SerializeField]
		private float driftPixelRadius;

		[SerializeField]
		private float sizeSpan;

		[SerializeField]
		private float driftSpan;

		[SerializeField]
		private float dispatchSpan;

		[SerializeField]
		private int peakSum;

		[SerializeField]
		private AnimationCurve reboundProfile;

		private int _liveMotions;

		private long _upcomingMotionSum;

		private long _undeliveredSum;

		private Action _onDone;

		private Coroutine _reboundRoutine;

		private Vector3 _glyphBasisSize = Vector3.one;

		private bool _glyphBasisSizeCached;

		private float _latestPennyAudioMoment = -1f;

		private readonly Stack<Image> _idleTokens = new Stack<Image>();

		private readonly List<Image> _liveTokens = new List<Image>();

		public bool IsTweening => _liveMotions > 0;

		internal static float Uniform(float frac) => frac;

		internal static float SmoothingOutQuad(float frac) => frac * (2f - frac);

		internal static float SmoothingInQuad(float frac) => frac * frac;

		internal static float SmoothingOutBack(float frac)
		{
			const float overshoot = 1.70158f;
			frac -= 1f;
			return frac * frac * ((overshoot + 1f) * frac + overshoot) + 1f;
		}

		public void DispatchFunds(long sum, Vector2 viewportLocation, Func<float, float> sizeSmoothing, Func<float, float> driftSmoothing, Func<float, float> dispatchSmoothing, Action onDone = null)
		{
			if (sum <= 0 || !isActiveAndEnabled || fundsHolder == null)
			{
				RenewCaption(PlayMechanics.money);
				onDone?.Invoke();
				return;
			}
			_onDone += onDone;
			_liveMotions++;
			_undeliveredSum += sum;
			RenewCaption(PlayMechanics.money - _undeliveredSum);
			_upcomingMotionSum = sum;
			StartCoroutine(TweenFlight(viewportLocation, sizeSmoothing ?? Uniform, driftSmoothing ?? Uniform, dispatchSmoothing ?? Uniform));
		}

		private IEnumerator ReboundGlyph()
		{
			if (fundsGlyph == null)
			{
				yield break;
			}
			if (!_glyphBasisSizeCached)
			{
				_glyphBasisSize = fundsGlyph.localScale;
				_glyphBasisSizeCached = true;
			}
			float elapsed = 0f;
			while (elapsed < ReboundSpan)
			{
				elapsed += Time.unscaledDeltaTime;
				float norm = Mathf.Clamp01(elapsed / ReboundSpan);
				fundsGlyph.localScale = _glyphBasisSize * (1f + ReboundStrength * EvaluateRebound(norm));
				yield return null;
			}
			fundsGlyph.localScale = _glyphBasisSize;
			_reboundRoutine = null;
		}

		private IEnumerator TweenFlight(Vector2 viewportLoc, Func<float, float> sizeSmoothing, Func<float, float> driftSmoothing, Func<float, float> dispatchSmoothing)
		{
			long sum = _upcomingMotionSum;
			int tally = (int)Math.Max(1L, Math.Min(sum, (long)Mathf.Max(1, peakSum)));
			float spreadRadius = driftPixelRadius * CanvasSizeScalar();
			Vector3 beginWorld = PixelToWorld(viewportLoc);
			List<Image> cash = new List<Image>(tally);
			for (int idx = 0; idx < tally; idx++)
			{
				Image token = ConstructFundsToken(beginWorld);
				RectTransform penny = token.rectTransform;
				Vector3 endpoint = PixelToWorld(viewportLoc + UnityEngine.Random.insideUnitCircle * spreadRadius);
				StartCoroutine(SizeRoutine(penny, 0f, 1f, sizeSpan, sizeSmoothing));
				StartCoroutine(DriftRoutine(penny, endpoint, driftSpan, driftSmoothing));
				cash.Add(token);
			}
			float lingerSpan = Mathf.Max(sizeSpan, driftSpan);
			if (lingerSpan > 0f)
			{
				yield return new WaitForSecondsRealtime(lingerSpan);
			}
			int[] landed = { 0 };
			long handedOver = 0L;
			for (int idx = 0; idx < tally; idx++)
			{
				StartCoroutine(DispatchPenny(cash[idx], idx, dispatchSmoothing, landed));
			}
			while (landed[0] < tally)
			{
				long objective = sum * landed[0] / tally;
				if (objective != handedOver)
				{
					_undeliveredSum -= objective - handedOver;
					handedOver = objective;
					RenewCaption(PlayMechanics.money - _undeliveredSum);
				}
				yield return null;
			}
			_undeliveredSum -= sum - handedOver;
			_liveMotions = Mathf.Max(0, _liveMotions - 1);
			if (_liveMotions == 0)
			{
				_undeliveredSum = 0L;
				RenewCaption(PlayMechanics.money);
				Action hook = _onDone;
				_onDone = null;
				hook?.Invoke();
			}
			else
			{
				RenewCaption(PlayMechanics.money - _undeliveredSum);
			}
		}

		private IEnumerator DispatchPenny(Image token, int ordinal, Func<float, float> dispatchSmoothing, int[] landed)
		{
			if (token != null)
			{
				RectTransform penny = token.rectTransform;
				yield return ShiftRoutine(penny, penny.position, dispatchSpan + ordinal * DispatchStagger, dispatchSmoothing);
				ReleaseFundsToken(token);
			}
			landed[0]++;
			if (_reboundRoutine != null)
			{
				StopCoroutine(_reboundRoutine);
			}
			_reboundRoutine = StartCoroutine(ReboundGlyph());
			PerformPennyAudio();
		}

		private IEnumerator SizeRoutine(Transform focus, float fromSize, float toSize, float span, Func<float, float> smoothing)
		{
			float elapsed = 0f;
			while (elapsed < span && focus != null)
			{
				elapsed += Time.unscaledDeltaTime;
				float norm = smoothing(Mathf.Clamp01(elapsed / span));
				focus.localScale = Vector3.one * Mathf.LerpUnclamped(fromSize, toSize, norm);
				yield return null;
			}
			if (focus != null)
			{
				focus.localScale = Vector3.one * toSize;
			}
		}

		private IEnumerator DriftRoutine(Transform pennyPivot, Vector3 endpoint, float span, Func<float, float> smoothing)
		{
			if (pennyPivot == null)
			{
				yield break;
			}
			Vector3 beginLoc = pennyPivot.position;
			float elapsed = 0f;
			while (elapsed < span && pennyPivot != null)
			{
				elapsed += Time.unscaledDeltaTime;
				pennyPivot.position = Vector3.LerpUnclamped(beginLoc, endpoint, smoothing(Mathf.Clamp01(elapsed / span)));
				yield return null;
			}
			if (pennyPivot != null)
			{
				pennyPivot.position = endpoint;
			}
		}

		private IEnumerator ShiftRoutine(Transform focus, Vector3 beginLoc, float span, Func<float, float> smoothing)
		{
			float elapsed = 0f;
			while (elapsed < span && focus != null)
			{
				elapsed += Time.unscaledDeltaTime;
				focus.position = Vector3.LerpUnclamped(beginLoc, ObjectiveLocation(), smoothing(Mathf.Clamp01(elapsed / span)));
				yield return null;
			}
			if (focus != null)
			{
				focus.position = ObjectiveLocation();
			}
		}

		private Image ConstructFundsToken(Vector3 worldLocation)
		{
			Image artwork = null;
			while (artwork == null && _idleTokens.Count > 0)
			{
				artwork = _idleTokens.Pop();
			}
			bool recycled = artwork != null;
			RectTransform box;
			if (recycled)
			{
				box = artwork.rectTransform;
				if (box.parent != fundsHolder)
				{
					box.SetParent(fundsHolder, false);
				}
				box.SetAsLastSibling();
			}
			else
			{
				GameObject node = new GameObject("Currency", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
				node.layer = fundsHolder.gameObject.layer;
				box = (RectTransform)node.transform;
				box.SetParent(fundsHolder, false);
				artwork = node.GetComponent<Image>();
				artwork.raycastTarget = false;
				artwork.preserveAspect = true;
			}
			float extent = PennyExtentInHolder();
			box.sizeDelta = new Vector2(extent, extent);
			box.position = worldLocation;
			box.localScale = Vector3.zero;
			if (fundsArtwork != null && fundsArtwork.sprite != null)
			{
				artwork.sprite = fundsArtwork.sprite;
			}
			else if (fundsIcon != null)
			{
				artwork.sprite = fundsIcon;
			}
			else if (recycled)
			{
				artwork.sprite = null;
			}
			if (recycled)
			{
				artwork.gameObject.SetActive(true);
			}
			_liveTokens.Add(artwork);
			return artwork;
		}

		private void ReleaseFundsToken(Image token)
		{
			_liveTokens.Remove(token);
			if (token == null)
			{
				return;
			}
			token.gameObject.SetActive(false);
			_idleTokens.Push(token);
		}

		private Vector3 ObjectiveLocation()
		{
			if (fundsGlyph == null)
			{
				return transform.position;
			}
			return fundsGlyph.TransformPoint(fundsGlyph.rect.center);
		}

		private float PennyExtentInHolder()
		{
			if (fundsGlyph == null || fundsHolder == null || fundsHolder.lossyScale.x <= 0f)
			{
				return PennyExtent;
			}
			Rect glyphBox = fundsGlyph.rect;
			float glyphScale = (fundsGlyph.parent != null ? fundsGlyph.parent.lossyScale.x : 1f) * (_glyphBasisSizeCached ? _glyphBasisSize.x : fundsGlyph.localScale.x);
			return Mathf.Max(glyphBox.width, glyphBox.height) * glyphScale / fundsHolder.lossyScale.x;
		}

		private Vector3 PixelToWorld(Vector2 viewportLocation)
		{
			Canvas anchor = AnchorCanvas();
			Camera lens = anchor != null && anchor.renderMode != RenderMode.ScreenSpaceOverlay ? anchor.worldCamera : null;
			if (RectTransformUtility.ScreenPointToWorldPointInRectangle(fundsHolder, viewportLocation, lens, out Vector3 worldPoint))
			{
				return worldPoint;
			}
			return new Vector3(viewportLocation.x, viewportLocation.y, fundsHolder.position.z);
		}

		private float CanvasSizeScalar()
		{
			Canvas anchor = AnchorCanvas();
			return anchor != null ? anchor.scaleFactor : 1f;
		}

		private Canvas AnchorCanvas()
		{
			Canvas foundCanvas = fundsHolder != null ? fundsHolder.GetComponentInParent<Canvas>() : GetComponentInParent<Canvas>();
			return foundCanvas != null ? foundCanvas.rootCanvas : null;
		}

		private float EvaluateRebound(float norm)
		{
			if (reboundProfile != null && reboundProfile.length > 0)
			{
				return reboundProfile.Evaluate(norm);
			}
			return Mathf.Sin(norm * Mathf.PI);
		}

		public void RenewCaption(long worth)
		{
			if (activeSumCaption != null)
			{
				activeSumCaption.text = Toolbox.CompactNumeralToThousands(Math.Max(0L, worth));
			}
		}

		private void PerformPennyAudio()
		{
			if (Time.unscaledTime - _latestPennyAudioMoment < PennyAudioGap)
			{
				return;
			}
			_latestPennyAudioMoment = Time.unscaledTime;
			UIDirector hudDirector = UIDirector.Shared;
			if (hudDirector != null && hudDirector.gameplayCore != null && hudDirector.gameplayCore.audioDirector != null)
			{
				hudDirector.gameplayCore.audioDirector.PerformUICue(AudioCue.CollectCash);
			}
		}

		private void OnDisable()
		{
			if (_liveMotions > 0)
			{
				StopAllCoroutines();
				_liveMotions = 0;
				_undeliveredSum = 0L;
				_reboundRoutine = null;
				for (int idx = _liveTokens.Count - 1; idx >= 0; idx--)
				{
					if (_liveTokens[idx] != null)
					{
						Destroy(_liveTokens[idx].gameObject);
					}
				}
				_liveTokens.Clear();
				if (fundsGlyph != null && _glyphBasisSizeCached)
				{
					fundsGlyph.localScale = _glyphBasisSize;
				}
				RenewCaption(PlayMechanics.money);
				Action hook = _onDone;
				_onDone = null;
				hook?.Invoke();
			}
		}
	}
}
