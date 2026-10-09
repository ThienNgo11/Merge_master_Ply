using TMPro;
using UnityEngine;

namespace Funlab.MergeMaster
{
	public class PrizeRoulette : MonoBehaviour
	{
		private const int SliceTally = 8;

		private const float SliceAngle = 360f / SliceTally;

		private const float NeedlePace = 270f;

		public RectTransform needle;

		public TextMeshProUGUI redeemText;

		public TextMeshProUGUI[] coefficientPrizes;

		public int coefficient;

		private bool isRolling;

		private bool shallHalt;

		private float rollDegrees;

		private long income;

		private int prizeCategory;

		// Sectors clockwise from the top (row 0 = fail, row 1 = win). Measured on the original (b_006 fail, b_017 /
		// c_010 / d_020 win): X5 (with the coin icon), X4, X2, X3, X2, X4, X2, X3 - identical for a win and a fail.
		private readonly int[,] coefficients = new int[2, SliceTally]
		{
			{ 5, 4, 2, 3, 2, 4, 2, 3 },
			{ 5, 4, 2, 3, 2, 4, 2, 3 }
		};

		private int _slice = -1;

		internal bool IsRolling => isRolling;

		public void BeginRoll(long withIncome, StageOutcome stageOutcome)
		{
			income = withIncome;
			prizeCategory = stageOutcome == StageOutcome.Accomplishment ? 1 : 0;
			PopulateCoefficients();
			rollDegrees = 0f;
			_slice = -1;
			shallHalt = false;
			isRolling = true;
			ImposeAngle();
			ReviseSlice();
			AudioDirector audioDirector = ResolveAudioDirector();
			if (audioDirector != null)
			{
				audioDirector.PerformMiniatureCue(AudioCue.RouletteSpin, true);
			}
		}

		public void HaltRoll()
		{
			if (isRolling)
			{
				shallHalt = true;
			}
		}

		private void Update()
		{
			if (!isRolling)
			{
				return;
			}
			float stride = NeedlePace * Time.deltaTime;
			if (shallHalt)
			{
				float objectiveAngle = Mathf.Max(0, _slice) * SliceAngle;
				float difference = Mathf.DeltaAngle(rollDegrees, objectiveAngle);
				if (Mathf.Abs(difference) <= stride)
				{
					rollDegrees = Mathf.Repeat(objectiveAngle, 360f);
					isRolling = false;
					shallHalt = false;
					SilenceRoll();
				}
				else
				{
					rollDegrees = Mathf.Repeat(rollDegrees + Mathf.Sign(difference) * stride, 360f);
				}
				ImposeAngle();
				return;
			}
			rollDegrees = Mathf.Repeat(rollDegrees + stride, 360f);
			ImposeAngle();
			ReviseSlice();
		}

		private void ImposeAngle()
		{
			if (needle != null)
			{
				needle.localEulerAngles = new Vector3(0f, 0f, -rollDegrees);
			}
		}

		private void ReviseSlice()
		{
			int landedSlice = Mathf.FloorToInt(Mathf.Repeat(rollDegrees + SliceAngle * 0.5f, 360f) / SliceAngle) % SliceTally;
			if (landedSlice == _slice)
			{
				return;
			}
			_slice = landedSlice;
			coefficient = coefficients[prizeCategory, landedSlice];
			if (redeemText != null)
			{
				redeemText.text = Toolbox.CompactNumeralToThousands(income * coefficient);
			}
		}

		private void PopulateCoefficients()
		{
			if (coefficientPrizes == null)
			{
				return;
			}
			for (int idx = 0; idx < coefficientPrizes.Length && idx < SliceTally; idx++)
			{
				if (coefficientPrizes[idx] != null)
				{
					coefficientPrizes[idx].text = "X" + coefficients[prizeCategory, idx];
				}
			}
		}

		private void OnDisable()
		{
			bool wasRolling = isRolling;
			isRolling = false;
			shallHalt = false;
			if (wasRolling)
			{
				SilenceRoll();
			}
		}

		private static void SilenceRoll()
		{
			AudioDirector audioDirector = ResolveAudioDirector();
			if (audioDirector != null)
			{
				audioDirector.HaltMiniatureCue(AudioCue.RouletteSpin);
			}
		}

		private static AudioDirector ResolveAudioDirector()
		{
			UIDirector director = UIDirector.Shared;
			return director != null && director.gameplayCore != null ? director.gameplayCore.audioDirector : null;
		}
	}
}
