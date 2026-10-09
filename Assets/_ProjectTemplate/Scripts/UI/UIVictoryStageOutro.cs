using TMPro;
using UnityEngine;

namespace Funlab.MergeMaster
{
	public class UIVictoryStageOutro : UIStageOutro
	{
		[SerializeField]
		private JewelsHoard jewelsHoard;

		[SerializeField]
		private RocksHoard rocksHoard;

		[SerializeField]
		private TextMeshProUGUI stageFinishBoonText;

		[SerializeField]
		private TextMeshProUGUI hoardFoundText;

		private static readonly Vector3[] _cornerBuffer = new Vector3[4];

		private int _jewels;

		private int _rocks;

		private bool _rouletteStandardCached;

		private float _rouletteStandardY;

		protected override void ArrangeOutcomeSpecifics()
		{
			if (stageFinishBoonText != null)
			{
				// c_010 / e_004 / f_030: yellow "Level Completed Bonus x10" above the wheel (already included in YOU EARNED).
				int boon = PlayMechanics.stageFinishIncomeCoefficient;
				stageFinishBoonText.gameObject.SetActive(PlayMechanics.stageFinishBoonApplies);
				stageFinishBoonText.text = "Level Completed Bonus x" + boon;
			}
			SeatRoulette(stageFinishBoonText == null || PlayMechanics.stageFinishBoonApplies);
			// Gems: 1..3 on a win from level 2 on, never on level 1 (b_017 none, c_010 +2, d_020 +1).
			_jewels = Mathf.Max(0, PlayMechanics.DrawHoardJewels(true));
			// Stones (diorama, f_030 "+119" next to the gems): dior_stone_eol 80..120 on a win once the diorama is unlocked.
			_rocks = 0;
			if (GameTuning.MiniatureAllowed && PlayMechanics.latestPerformedStage >= GameTuning.MiniatureFloorStage)
			{
				_rocks = DecodeRockRange(GameTuning.MiniatureRockOutroOfStage);
			}
			if (jewelsHoard != null)
			{
				if (_jewels > 0)
				{
					jewelsHoard.Fill(_jewels);
				}
				jewelsHoard.Conceal();
			}
			if (rocksHoard != null)
			{
				if (_rocks > 0)
				{
					rocksHoard.Stock(_rocks);
				}
				rocksHoard.Conceal();
			}
			if (hoardFoundText != null)
			{
				hoardFoundText.gameObject.SetActive(_jewels > 0 || _rocks > 0);
			}
		}

		protected override void PresentOutcomeSpecifics()
		{
			if (_jewels > 0 && jewelsHoard != null)
			{
				jewelsHoard.Present();
			}
			if (_rocks > 0 && rocksHoard != null)
			{
				rocksHoard.Present();
			}
		}

		protected override void ConcealOutcomeSpecifics()
		{
			if (jewelsHoard != null)
			{
				jewelsHoard.Conceal();
			}
			if (rocksHoard != null)
			{
				rocksHoard.Conceal();
			}
		}

		protected override void BestowHoards()
		{
			if (_jewels > 0)
			{
				PlayMechanics.PersistJewels(_jewels);
				UIDirector director = UIDirector.Shared;
				if (director != null)
				{
					director.PresentJewelsMotion();
				}
			}
			if (_rocks > 0)
			{
				PlayMechanics.stones += _rocks;
			}
		}

		private void SeatRoulette(bool boonPresented)
		{
			RectTransform wheel = prizeRoulette != null ? prizeRoulette.transform as RectTransform : null;
			if (wheel == null)
			{
				return;
			}
			if (!_rouletteStandardCached)
			{
				_rouletteStandardCached = true;
				_rouletteStandardY = wheel.anchoredPosition.y;
			}
			Vector2 seat = new Vector2(wheel.anchoredPosition.x, _rouletteStandardY);
			wheel.anchoredPosition = seat;
			if (boonPresented || stageIncomeText == null || redeemCtrl == null)
			{
				return;
			}
			Transform space = wheel.parent;
			Vector2 incomeSpan = GetVerticalExtent(stageIncomeText.rectTransform, space);
			Vector2 redeemSpan = GetVerticalExtent((RectTransform)redeemCtrl.transform, space);
			Vector2 wheelSpan = GetVerticalExtent(wheel, space);
			seat.y += (incomeSpan.x + redeemSpan.y) * 0.5f - (wheelSpan.x + wheelSpan.y) * 0.5f;
			wheel.anchoredPosition = seat;
		}

		private static Vector2 GetVerticalExtent(RectTransform box, Transform space)
		{
			box.GetWorldCorners(_cornerBuffer);
			float low = float.MaxValue;
			float high = float.MinValue;
			for (int idx = 0; idx < _cornerBuffer.Length; idx++)
			{
				float level = space.InverseTransformPoint(_cornerBuffer[idx]).y;
				low = Mathf.Min(low, level);
				high = Mathf.Max(high, level);
			}
			return new Vector2(low, high);
		}

		private static int DecodeRockRange(string bounds)
		{
			if (string.IsNullOrEmpty(bounds))
			{
				return 0;
			}
			string[] segments = bounds.Split(',');
			if (segments.Length >= 2 && int.TryParse(segments[0].Trim(), out int floor) && int.TryParse(segments[1].Trim(), out int peak))
			{
				return Random.Range(Mathf.Min(floor, peak), Mathf.Max(floor, peak) + 1);
			}
			return int.TryParse(segments[0].Trim(), out int solo) ? Mathf.Max(0, solo) : 0;
		}
	}
}
