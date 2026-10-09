using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Funlab.MergeMaster
{
	// Tab strip of the summon store (Dinosaurs / Warriors): moves the selector, recolours the tabs and swaps the scroll content.
	public class UITabStrip : MonoBehaviour
	{
		[SerializeField]
		private float transitionSpan;

		[SerializeField]
		private RectTransform chosenIndicator;

		[SerializeField]
		private Gradient switchedOffToChosenGradient;

		[SerializeField]
		private RectTransform[] contentPivots;

		[SerializeField]
		private ScrollRect scrollBox;

		private GameObject[] _controlParts;

		private Sequence _activeChain;

		private int _priorChoice;

		public int activeLiveControl;

		public bool booted;

		private Graphic[][] _tabVisuals;

		private void Awake()
		{
			Bootstrap();
		}

		private void OnDestroy()
		{
			_activeChain?.Kill();
		}

		private void Bootstrap()
		{
			if (booted && _controlParts != null)
			{
				return;
			}
			List<GameObject> controls = new List<GameObject>();
			for (int idx = 0; idx < transform.childCount; idx++)
			{
				Transform descendant = transform.GetChild(idx);
				if (descendant.GetComponent<Button>() != null)
				{
					controls.Add(descendant.gameObject);
				}
			}
			_controlParts = controls.ToArray();
			_tabVisuals = new Graphic[_controlParts.Length][];
			for (int idx = 0; idx < _controlParts.Length; idx++)
			{
				int ordinal = idx;
				Button control = _controlParts[idx].GetComponent<Button>();
				control.onClick.AddListener(() => SetLiveControl(ordinal));
				List<Graphic> visuals = new List<Graphic>();
				foreach (Graphic visual in _controlParts[idx].GetComponentsInChildren<Graphic>(true))
				{
					if (visual.gameObject != _controlParts[idx] && (visual is TMP_Text || visual is Image))
					{
						visuals.Add(visual);
					}
				}
				_tabVisuals[idx] = visuals.ToArray();
			}
			booted = true;
		}

		public void SetLiveControl(int ordinal, float presetScroll = -1f, bool immediate = false)
		{
			Bootstrap();
			if (_controlParts.Length == 0)
			{
				return;
			}
			ordinal = Mathf.Clamp(ordinal, 0, _controlParts.Length - 1);
			_priorChoice = activeLiveControl;
			activeLiveControl = ordinal;
			_activeChain?.Kill();
			_activeChain = immediate ? null : DOTween.Sequence().SetUpdate(true);
			if (chosenIndicator != null)
			{
				RectTransform objective = (RectTransform)_controlParts[ordinal].transform;
				if (immediate)
				{
					chosenIndicator.DOKill();
					chosenIndicator.anchoredPosition = new Vector2(objective.anchoredPosition.x, chosenIndicator.anchoredPosition.y);
				}
				else
				{
					_activeChain.Join(chosenIndicator.DOAnchorPosX(objective.anchoredPosition.x, transitionSpan).SetEase(Ease.OutQuad));
				}
			}
			for (int idx = 0; idx < _tabVisuals.Length; idx++)
			{
				Color tint = SampleGradient(idx == ordinal ? 1f : 0f);
				foreach (Graphic visual in _tabVisuals[idx])
				{
					if (immediate)
					{
						visual.DOKill();
						visual.color = tint;
					}
					else
					{
						_activeChain.Join(visual.DOColor(tint, transitionSpan));
					}
				}
			}
			if (contentPivots != null)
			{
				for (int idx = 0; idx < contentPivots.Length; idx++)
				{
					if (contentPivots[idx] != null)
					{
						contentPivots[idx].gameObject.SetActive(idx == ordinal);
					}
				}
				if (scrollBox != null && ordinal < contentPivots.Length && contentPivots[ordinal] != null)
				{
					scrollBox.content = contentPivots[ordinal];
					if (presetScroll >= 0f && presetScroll < 1f)
					{
						scrollBox.StopMovement();
						scrollBox.verticalNormalizedPosition = presetScroll;
					}
					else
					{
						Toolbox.ScrollToTop(scrollBox);
					}
				}
			}
		}

		private void SwitchOffTab(int ordinal)
		{
			Bootstrap();
			if (ordinal < 0 || ordinal >= _controlParts.Length)
			{
				return;
			}
			_controlParts[ordinal].GetComponent<Button>().interactable = false;
			Color tint = SampleGradient(0f);
			foreach (Graphic visual in _tabVisuals[ordinal])
			{
				visual.color = tint;
			}
		}

		private Color SampleGradient(float moment)
		{
			return switchedOffToChosenGradient != null ? switchedOffToChosenGradient.Evaluate(moment) : Color.white;
		}
	}
}
