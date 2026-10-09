using TMPro;
using UnityEngine;

namespace Funlab.MergeMaster
{
	public class UIDefeatStageOutro : UIStageOutro
	{
		private const float FoeHarmInfillFloorBreadth = 65f;

		private const float FoeHarmInfillPeakBreadth = 680f;

		private const float FoeHarmTextInset = 5f;

		private const float FoeHarmTextPeakEdge = 618f;

		[SerializeField]
		private GameObject foeHarmMeter;

		[SerializeField]
		private RectTransform foeHarmInfill;

		[SerializeField]
		private TextMeshProUGUI foeHarmText;

		protected override void ArrangeOutcomeSpecifics()
		{
			if (foeHarmMeter == null)
			{
				return;
			}
			bool shouldPresent = stageOutcome == StageOutcome.Unsuccessful && GameTuning.presentStageFoeHarmMeter;
			foeHarmMeter.SetActive(shouldPresent);
			if (!shouldPresent)
			{
				return;
			}
			// Same fraction as the fail earning (round(40 * PL^4 * fraction)): b_006 34% -> +14.
			float portion = PlayMechanics.foeHarmPortion;
			if (portion <= 0f && _gameplayCore != null && _gameplayCore.registry != null && PlayMechanics.foeBeginVitality > 0f)
			{
				portion = Mathf.Clamp01(1f - _gameplayCore.registry.GetLeftoverVitality(ActorFaction.Foe) / PlayMechanics.foeBeginVitality);
			}
			float infillBreadth = Mathf.Max(FoeHarmInfillFloorBreadth, FoeHarmInfillPeakBreadth * portion);
			if (foeHarmInfill != null)
			{
				foeHarmInfill.sizeDelta = new Vector2(infillBreadth, foeHarmInfill.sizeDelta.y);
			}
			if (foeHarmText != null)
			{
				foeHarmText.text = Mathf.RoundToInt(portion * 100f) + "%";
				RectTransform harmCaption = foeHarmText.rectTransform;
				harmCaption.anchoredPosition = new Vector2(Mathf.Min(-FoeHarmTextInset, FoeHarmTextPeakEdge - infillBreadth), harmCaption.anchoredPosition.y);
			}
		}
	}
}
