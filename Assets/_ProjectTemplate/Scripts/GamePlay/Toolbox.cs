using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Funlab.MergeMaster
{
	public static class Toolbox
	{
		private static bool isBooted;

		public static bool isNotchedPhone;

		public static bool isTablet => Screen.height > 0 && (float)Screen.width / Screen.height >= 0.7f;

		public static void Boot()
		{
			if (isBooted)
			{
				return;
			}
			isBooted = true;
			isNotchedPhone = Screen.width > 0 && (float)Screen.height / Screen.width >= 2f;
		}

		// Integer K below 10 M, integer M below 10 G, then B ("204K", "1000K", "12M").
		public static string CompactNumeralToThousands(long numeral)
		{
			if (numeral < 0)
			{
				return "-" + CompactNumeralToThousands(numeral == long.MinValue ? long.MaxValue : -numeral);
			}
			if (numeral < 10000L)
			{
				return numeral.ToString();
			}
			if (numeral < 10000000L)
			{
				return (numeral / 1000L).ToString() + "K";
			}
			if (numeral < 10000000000L)
			{
				return (numeral / 1000000L).ToString() + "M";
			}
			return (numeral / 1000000000L).ToString() + "B";
		}

		public static long GetCompactNumeralBucket(long numeral, out int tier)
		{
			if (numeral < 10000L)
			{
				tier = 0;
				return numeral;
			}
			if (numeral < 10000000L)
			{
				tier = 1;
				return numeral / 1000L;
			}
			if (numeral < 10000000000L)
			{
				tier = 2;
				return numeral / 1000000L;
			}
			tier = 3;
			return numeral / 1000000000L;
		}

		public static string CompactIntegerToThousands(int numeral)
		{
			return CompactNumeralToThousands((long)numeral);
		}

		private const int ExactCaptionIntegerCeiling = 10000000;

		public static void ImposeIntegerCaption(TMP_Text caption, int worth)
		{
			if (worth >= 0 && worth < ExactCaptionIntegerCeiling)
			{
				caption.SetText("{0}", worth);
				return;
			}
			caption.text = worth.ToString();
		}

		// Unit stat numbers (cards, summon cards, new-unit popup, stat increase tabs):
		// < 1000 exact integer, >= 1000 -> floor(x / 1000) + "K" (1470 -> "1K", 3444 -> "3K", 108094 -> "108K").
		public static string FormatMetricNumeral(long numeral)
		{
			if (numeral < 0)
			{
				return "-" + FormatMetricNumeral(numeral == long.MinValue ? long.MaxValue : -numeral);
			}
			if (numeral < 1000L)
			{
				return numeral.ToString();
			}
			return (numeral / 1000L).ToString() + "K";
		}

		public static string FormatMetricInteger(int numeral)
		{
			return FormatMetricNumeral((long)numeral);
		}

		public static bool IsIntegralNumeral(float worth)
		{
			return Mathf.Approximately(worth, Mathf.Round(worth));
		}

		public static void FitCanvasScalers()
		{
			float match = (float)Screen.width / Mathf.Max(1, Screen.height) > GameTuning.standardViewportProportion + 0.001f ? 1f : 0f;
#if UNITY_LUNA
			foreach (CanvasScaler scaler in UnityEngine.Object.FindObjectsOfType<CanvasScaler>())
#else
			foreach (CanvasScaler scaler in UnityEngine.Object.FindObjectsOfType<CanvasScaler>(true))
#endif
			{
				if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
				{
					scaler.matchWidthOrHeight = match;
				}
			}
		}

		public static IEnumerator HoldScrollWhileGrowing(ScrollRect scrollBox, Transform growingFrame, ScrollRect.MovementType settledMovement, Action settle)
		{
			if (scrollBox == null)
			{
				yield break;
			}
			scrollBox.movementType = ScrollRect.MovementType.Unrestricted;
			scrollBox.StopMovement();
			float waited = 0f;
			while (growingFrame != null && growingFrame.localScale.x < 0.999f && waited < 1.5f)
			{
				waited += Time.unscaledDeltaTime;
				yield return null;
			}
			if (scrollBox == null)
			{
				yield break;
			}
			scrollBox.movementType = settledMovement;
			scrollBox.StopMovement();
			settle?.Invoke();
		}

		public static void ScrollToTop(ScrollRect scrollBox)
		{
			if (scrollBox == null || scrollBox.content == null)
			{
				return;
			}
			scrollBox.StopMovement();
			RectTransform content = scrollBox.content;
			if (Mathf.Approximately(content.anchorMin.y, 1f) && Mathf.Approximately(content.anchorMax.y, 1f))
			{
				content.anchoredPosition = new Vector2(content.anchoredPosition.x, -(1f - content.pivot.y) * content.rect.height);
			}
			else
			{
				scrollBox.verticalNormalizedPosition = 1f;
			}
		}

		// 10-level segment index (environment group): (level - 1) / 10. Environment index = GameTuning.GetBiomeOrdinal(level).
		public static int GetStageCluster(int stage)
		{
			return (Mathf.Max(1, stage) - 1) / GameTuning.StagesPerBiome;
		}

		public static int MomentInSecs()
		{
			return (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		}

		public static void PerformTactileImpact(ImpactFeedbackStyle reaction)
		{
		}

		public static void PerformTactileNotice(NotificationFeedbackStyle reaction)
		{
		}
	}

	public enum ImpactFeedbackStyle { Light, Medium, Heavy }
	public enum NotificationFeedbackStyle { Success, Warning, Error }
}
