using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Funlab.MergeMaster
{
	// "Environment Presentation Banner" (e_004..e_006): the Animator's only state ("Environment Presentation Banner Anim",
	// 0.75 s) fades in the "Environment Unlocked" message; the environment panel "Level <env+1>" (icon + name, e.g. env 1 =
	// "Level 2" = "The Wastelands") fades in through its own DOTweenAnimations (delay 1.25 s, 1 s).
	public class BiomeTitleUI : MonoBehaviour
	{
		private const string ReadoutTitlePrefix = "Level ";

		[SerializeField]
		private GameObject[] biomeReadouts;

		private Animator motionRig;

		private List<GameObject> _readouts;

		private readonly List<GameObject> _concealedSolely = new List<GameObject>();

		private void Awake()
		{
			motionRig = GetComponent<Animator>();
		}

		public void Present(int biomeOrdinal)
		{
			gameObject.SetActive(true);
			if (motionRig == null)
			{
				motionRig = GetComponent<Animator>();
			}
			List<GameObject> readouts = GetReadouts();
			GameObject presented = null;
			if (readouts.Count > 0)
			{
				int ordinal = Mathf.Abs(biomeOrdinal);
				presented = ordinal < readouts.Count ? readouts[ordinal] : null;
				for (int offset = 0; presented == null && offset < readouts.Count; offset++)
				{
					presented = readouts[(ordinal + offset) % readouts.Count];
				}
			}
			for (int idx = 0; idx < readouts.Count; idx++)
			{
				if (readouts[idx] != null && readouts[idx] != presented)
				{
					readouts[idx].SetActive(false);
				}
			}
			for (int hiddenIdx = 0; hiddenIdx < _concealedSolely.Count; hiddenIdx++)
			{
				if (_concealedSolely[hiddenIdx] != null && _concealedSolely[hiddenIdx] != presented)
				{
					_concealedSolely[hiddenIdx].SetActive(false);
				}
			}
			if (presented != null)
			{
				presented.SetActive(true);
				// The first enable creates and autoplays the tweens (Awake); on a later show replay the fade-in from the start.
				foreach (DOTweenAnimation transitionMotion in presented.GetComponentsInChildren<DOTweenAnimation>(true))
				{
					if (transitionMotion != null && transitionMotion.tween != null && transitionMotion.tween.IsActive())
					{
						transitionMotion.tween.Restart();
					}
				}
			}
			if (motionRig != null && motionRig.runtimeAnimatorController != null)
			{
				motionRig.Rebind();
				motionRig.Update(0f);
			}
		}

		public void Conceal()
		{
			gameObject.SetActive(false);
		}

		// biomeReadouts holds "Level 1".."Level 9"; the scene also has "Level 10" ("The Lair", env 9) that the list
		// misses, so the direct children named "Level <n>" fill the slots the list does not cover.
		private List<GameObject> GetReadouts()
		{
			if (_readouts != null)
			{
				return _readouts;
			}
			_readouts = new List<GameObject>();
			if (biomeReadouts != null)
			{
				_readouts.AddRange(biomeReadouts);
			}
			for (int idx = 0; idx < transform.childCount; idx++)
			{
				Transform descendant = transform.GetChild(idx);
				string descendantTitle = descendant.name;
				if (!descendantTitle.StartsWith(ReadoutTitlePrefix) || !int.TryParse(descendantTitle.Substring(ReadoutTitlePrefix.Length), out int numeral) || numeral < 1 || numeral > 64)
				{
					continue;
				}
				int cell = numeral - 1;
				while (_readouts.Count <= cell)
				{
					_readouts.Add(null);
				}
				if (_readouts[cell] == null)
				{
					_readouts[cell] = descendant.gameObject;
				}
				else if (!_readouts.Contains(descendant.gameObject))
				{
					// A panel the list does not reference: never shown, but kept hidden with the others.
					_concealedSolely.Add(descendant.gameObject);
				}
			}
			return _readouts;
		}
	}
}
