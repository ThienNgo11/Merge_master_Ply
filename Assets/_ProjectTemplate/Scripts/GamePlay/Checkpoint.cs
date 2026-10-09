using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Funlab.MergeMaster
{
	// One building step of a diorama (BuildScene data): the Target building (fill-shader silhouette that fills from the
	// bottom while stones are spent), its island area (AreaSwitchedOn = sand / AreaSwitchedOff = dark silhouette), the props that
	// rise before it is built (_beforeAssembleOrnaments) or pop in after (_afterAssembleOrnaments), its confetti (AfterAssembleFlourishes), the
	// building VFX, the camera zoom used while building and the cost bubble above it.
	[Serializable]
	public class Checkpoint
	{
		private const float ObjectiveTransitionInSpan = 0.5f;

		private const float ChargeBalloonTransitionOutSpan = 0.25f;

		private const float MagnifySpan = 1f;

		private static readonly int InfillProperty = Shader.PropertyToID("_fill");

		public GameObject Objective;

		[SerializeField]
		private GameObject AreaSwitchedOn;

		[SerializeField]
		private GameObject AreaSwitchedOff;

		[SerializeField]
		private GameObject[] _beforeAssembleOrnaments;

		[SerializeField]
		private GameObject[] _afterAssembleOrnaments;

		[SerializeField]
		private ParticleSystem[] AfterAssembleFlourishes;

		[SerializeField]
		private GameObject _assemblingVisual;

		[SerializeField]
		private LensMagnifyInfo _lensMagnifyInfo;

		[SerializeField]
		private GameObject _chargeBalloon;

		[SerializeField]
		private TextMeshPro _chargeCaption;

		private Renderer _primaryRenderer;

		private Animator _motionRig;

		private Vector3 _objectivePristineSize;

		private bool _isAssemblingMotionPerforming;

		private bool _isFinished;

		private Vector3 _chargeBalloonPristineSize;

		private List<Renderer> _infillRenderers;

		private MaterialPropertyBlock _infillBlock;

		public bool IsFinished => _isFinished;

		public GameObject[] BeforeAssembleOrnaments => _beforeAssembleOrnaments;

		public LensMagnifyInfo MagnifyInfo => _lensMagnifyInfo;

		public Renderer PrimaryRenderer
		{
			get
			{
				if (_primaryRenderer == null && Objective != null)
				{
					_primaryRenderer = Objective.GetComponent<Renderer>();
					if (_primaryRenderer == null)
					{
						List<Renderer> candidates = InfillRenderers;
						_primaryRenderer = candidates.Count > 0 ? candidates[0] : Objective.GetComponentInChildren<Renderer>(true);
					}
				}
				return _primaryRenderer;
			}
		}

		// Mesh renderers of the target that use the diorama fill shader (particles, the bubble and its text are excluded).
		public List<Renderer> InfillRenderers
		{
			get
			{
				if (_infillRenderers == null)
				{
					_infillRenderers = new List<Renderer>();
					if (Objective != null)
					{
						foreach (Renderer candidate in Objective.GetComponentsInChildren<Renderer>(true))
						{
							if (!(candidate is MeshRenderer) && !(candidate is SkinnedMeshRenderer))
							{
								continue;
							}
							if (_chargeBalloon != null && candidate.transform.IsChildOf(_chargeBalloon.transform))
							{
								continue;
							}
							Material sharedMat = candidate.sharedMaterial;
							if (sharedMat != null && sharedMat.HasProperty(InfillProperty))
							{
								_infillRenderers.Add(candidate);
							}
						}
					}
				}
				return _infillRenderers;
			}
		}

		// Point the currency stream at: the middle of the building.
		public Vector3 ObjectiveFocusLocation
		{
			get
			{
				Renderer focusRenderer = PrimaryRenderer;
				if (focusRenderer != null && focusRenderer.enabled && focusRenderer.bounds.size.sqrMagnitude > 0f)
				{
					return focusRenderer.bounds.center;
				}
				return Objective != null ? Objective.transform.position : Vector3.zero;
			}
		}

		public void MarkFinished(bool finished = true)
		{
			_isFinished = finished;
			FlipAreaStatus(finished);
		}

		public void SetChargeCaption(int charge)
		{
			if (_chargeCaption != null)
			{
				Toolbox.ImposeIntegerCaption(_chargeCaption, Mathf.Max(0, charge));
			}
		}

		public void FlipAreaStatus(bool status)
		{
			if (AreaSwitchedOn != null)
			{
				AreaSwitchedOn.SetActive(status);
			}
			if (AreaSwitchedOff != null)
			{
				AreaSwitchedOff.SetActive(!status);
			}
		}

		// Build progress of the target: 0 = dark silhouette, 1 = fully built (the fill line rises from the bottom).
		public void SetInfill(float infill)
		{
			List<Renderer> infillList = InfillRenderers;
			if (infillList.Count == 0)
			{
				return;
			}
			if (_infillBlock == null)
			{
				_infillBlock = new MaterialPropertyBlock();
			}
			float clampedInfill = Mathf.Clamp01(infill);
			foreach (Renderer rend in infillList)
			{
				if (rend == null)
				{
					continue;
				}
				rend.GetPropertyBlock(_infillBlock);
				_infillBlock.SetFloat(InfillProperty, clampedInfill);
				rend.SetPropertyBlock(_infillBlock);
			}
		}

		// The silhouette of the new current target pops out of the ground (scale 0 -> original, OutBack) while its own
		// PropTween components raise it to its final position.
		public void PerformObjectiveTransitionInMotion(float pause = 0f)
		{
			if (Objective == null)
			{
				return;
			}
			CacheObjectiveSize();
			Objective.SetActive(true);
			Transform objectivePivot = Objective.transform;
			objectivePivot.DOKill();
			objectivePivot.localScale = Vector3.zero;
			objectivePivot.DOScale(_objectivePristineSize, ObjectiveTransitionInSpan).SetDelay(pause).SetEase(Ease.OutBack);
			foreach (OrnamentTransitionBasis transition in Objective.GetComponents<OrnamentTransitionBasis>())
			{
				transition.Perform(pause);
			}
		}

		// initialState = hidden (future milestone: sunk and scaled to zero); otherwise shown at its final place.
		public void SetObjectiveTransitionStatus(bool baselineStatus)
		{
			if (Objective == null)
			{
				return;
			}
			CacheObjectiveSize();
			Objective.transform.DOKill();
			Objective.transform.localScale = baselineStatus ? Vector3.zero : _objectivePristineSize;
			foreach (OrnamentTransitionBasis transition in Objective.GetComponents<OrnamentTransitionBasis>())
			{
				if (baselineStatus)
				{
					transition.SetStatusToPristine();
				}
				else
				{
					transition.SetStatusToFinal();
				}
			}
		}

		public bool PerformBeforeAssembleOrnamentTransitions(float pause = 0f)
		{
			if (_beforeAssembleOrnaments == null || _beforeAssembleOrnaments.Length == 0)
			{
				return false;
			}
			PerformOrnamentTransitions(_beforeAssembleOrnaments, pause);
			return true;
		}

		public void SetStatusToPristine(bool includeBeforeOrnaments = true, bool includeAfterOrnaments = true)
		{
			if (includeBeforeOrnaments)
			{
				SetOrnamentTransitionsStatusToPristine(_beforeAssembleOrnaments);
			}
			if (includeAfterOrnaments)
			{
				SetOrnamentTransitionsStatusToPristine(_afterAssembleOrnaments);
			}
		}

		public void SetStatusToConcluded(bool includeBeforeOrnaments = true, bool includeAfterOrnaments = true)
		{
			if (includeBeforeOrnaments)
			{
				SetOrnamentTransitionsStatusToConcluded(_beforeAssembleOrnaments);
			}
			if (includeAfterOrnaments)
			{
				SetOrnamentTransitionsStatusToConcluded(_afterAssembleOrnaments);
			}
		}

		public void TransitionOutChargeBalloon()
		{
			if (_chargeBalloon == null || !_chargeBalloon.activeSelf)
			{
				return;
			}
			CacheChargeBalloonSize();
			TerminateChargeBalloonTransitions();
			GameObject balloon = _chargeBalloon;
			balloon.transform.DOScale(0f, ChargeBalloonTransitionOutSpan).SetEase(Ease.InBack).OnComplete(() =>
			{
				if (balloon != null)
				{
					balloon.SetActive(false);
				}
			});
		}

		public bool PerformAfterAssembleOrnamentTransitions(float pause = 0f)
		{
			if (_afterAssembleOrnaments == null || _afterAssembleOrnaments.Length == 0)
			{
				return false;
			}
			PerformOrnamentTransitions(_afterAssembleOrnaments, pause);
			return true;
		}

		// Showing the bubble pops it in (its OrnamentTransitionSize components, 0 -> 10, OutBack 0.5 s).
		public void FlipLiveChargeBalloon(bool live)
		{
			if (_chargeBalloon == null)
			{
				return;
			}
			CacheChargeBalloonSize();
			TerminateChargeBalloonTransitions();
			if (!live)
			{
				_chargeBalloon.transform.localScale = _chargeBalloonPristineSize;
				_chargeBalloon.SetActive(false);
				return;
			}
			_chargeBalloon.SetActive(true);
			OrnamentTransitionBasis[] transitions = _chargeBalloon.GetComponents<OrnamentTransitionBasis>();
			if (transitions.Length > 0)
			{
				foreach (OrnamentTransitionBasis transition in transitions)
				{
					transition.Perform();
				}
			}
			else
			{
				_chargeBalloon.transform.localScale = Vector3.zero;
				_chargeBalloon.transform.DOScale(_chargeBalloonPristineSize, ObjectiveTransitionInSpan).SetEase(Ease.OutBack);
			}
		}

		public void FlipPerformAfterAssembleFlourishes(bool perform)
		{
			if (AfterAssembleFlourishes == null)
			{
				return;
			}
			foreach (ParticleSystem flourish in AfterAssembleFlourishes)
			{
				if (flourish == null)
				{
					continue;
				}
				if (perform)
				{
					flourish.gameObject.SetActive(true);
					flourish.Clear();
					flourish.Play(true);
				}
				else
				{
					flourish.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
					flourish.gameObject.SetActive(false);
				}
			}
		}

		public void MagnifyOnObjective(Camera lens)
		{
			if (lens == null)
			{
				return;
			}
			lens.transform.DOKill();
			lens.DOKill();
			lens.transform.DOMove(_lensMagnifyInfo.Location, MagnifySpan).SetEase(Ease.InOutQuad);
			lens.DOFieldOfView(_lensMagnifyInfo.AOV, MagnifySpan).SetEase(Ease.InOutQuad);
		}

		// While stones flow in: building VFX on and the target's pulse animation ("Desert House Fill Anim": scale 1 -> 1.07).
		public void FlipPerformAssemblingMotions(bool perform)
		{
			_isAssemblingMotionPerforming = perform;
			if (_assemblingVisual != null)
			{
				_assemblingVisual.SetActive(perform);
			}
			if (_motionRig == null && Objective != null)
			{
				_motionRig = Objective.GetComponent<Animator>();
			}
			if (_motionRig != null)
			{
				_motionRig.enabled = perform;
			}
			if (!perform && Objective != null && _objectivePristineSize != Vector3.zero && Objective.transform.localScale != Vector3.zero &&
				!DOTween.IsTweening(Objective.transform))
			{
				Objective.transform.localScale = _objectivePristineSize;
			}
		}

		private void CacheObjectiveSize()
		{
			if (_objectivePristineSize == Vector3.zero && Objective != null && Objective.transform.localScale != Vector3.zero)
			{
				_objectivePristineSize = Objective.transform.localScale;
			}
			if (_objectivePristineSize == Vector3.zero)
			{
				_objectivePristineSize = Vector3.one;
			}
		}

		private void CacheChargeBalloonSize()
		{
			if (_chargeBalloonPristineSize != Vector3.zero || _chargeBalloon == null)
			{
				return;
			}
			OrnamentTransitionSize sizeTransition = _chargeBalloon.GetComponent<OrnamentTransitionSize>();
			Vector3 size = sizeTransition != null ? sizeTransition.FinalSize : _chargeBalloon.transform.localScale;
			if (size == Vector3.zero)
			{
				size = _chargeBalloon.transform.localScale;
			}
			_chargeBalloonPristineSize = size != Vector3.zero ? size : Vector3.one;
		}

		private void TerminateChargeBalloonTransitions()
		{
			if (_chargeBalloon == null)
			{
				return;
			}
			_chargeBalloon.transform.DOKill();
			foreach (OrnamentTransitionBasis transition in _chargeBalloon.GetComponents<OrnamentTransitionBasis>())
			{
				transition.TerminateTransition();
			}
		}

		private void PerformOrnamentTransitions(GameObject[] Ornaments, float pause = 0f)
		{
			if (Ornaments == null)
			{
				return;
			}
			foreach (GameObject ornament in Ornaments)
			{
				if (ornament == null)
				{
					continue;
				}
				ornament.SetActive(true);
				foreach (OrnamentTransitionBasis transition in ornament.GetComponents<OrnamentTransitionBasis>())
				{
					transition.Perform(pause);
				}
			}
		}

		private void SetOrnamentTransitionsStatusToPristine(GameObject[] Ornaments)
		{
			if (Ornaments == null)
			{
				return;
			}
			foreach (GameObject ornament in Ornaments)
			{
				if (ornament == null)
				{
					continue;
				}
				foreach (OrnamentTransitionBasis transition in ornament.GetComponents<OrnamentTransitionBasis>())
				{
					transition.SetStatusToPristine();
				}
			}
		}

		private void SetOrnamentTransitionsStatusToConcluded(GameObject[] Ornaments)
		{
			if (Ornaments == null)
			{
				return;
			}
			foreach (GameObject ornament in Ornaments)
			{
				if (ornament == null)
				{
					continue;
				}
				ornament.SetActive(true);
				foreach (OrnamentTransitionBasis transition in ornament.GetComponents<OrnamentTransitionBasis>())
				{
					transition.SetStatusToFinal();
				}
			}
		}
	}
}
