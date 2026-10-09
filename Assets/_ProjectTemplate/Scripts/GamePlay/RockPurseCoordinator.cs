using System.Collections;
using TMPro;
using UnityEngine;

namespace Funlab.MergeMaster
{
	public class RockPurseCoordinator : MonoBehaviour
	{
		private const float TallySpan = 0.5f;

		[SerializeField]
		private TMP_Text rockCaption;

		private int _shownRocks;

		private void OnEnable()
		{
			PlayMechanics.StonesAltered += OnRockFundsAltered;
			OnRockFundsAltered(PlayMechanics.stones);
		}

		private void OnDisable()
		{
			PlayMechanics.StonesAltered -= OnRockFundsAltered;
		}

		private void OnRockFundsAltered(int worth)
		{
			StopAllCoroutines();
			if (!isActiveAndEnabled)
			{
				ShowRocks(worth);
				return;
			}
			StartCoroutine(TallyRoutine(worth));
		}

		private IEnumerator TallyRoutine(int worth)
		{
			int from = _shownRocks;
			float passed = 0f;
			while (passed < TallySpan)
			{
				float t = Mathf.Clamp01(passed / TallySpan);
				ShowRocks(Mathf.RoundToInt(Mathf.Lerp(from, worth, t * (2f - t))));
				yield return null;
				passed += Time.unscaledDeltaTime;
			}
			ShowRocks(worth);
		}

		private void ShowRocks(int worth)
		{
			_shownRocks = worth;
			if (rockCaption != null)
			{
				rockCaption.text = worth.ToString();
			}
		}
	}
}
