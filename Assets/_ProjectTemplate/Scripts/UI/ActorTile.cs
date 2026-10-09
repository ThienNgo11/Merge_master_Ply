using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Funlab.MergeMaster
{
	// Unit card: MY CARDS mini cards (RankingPopup), the NEW DINOSAUR / NEW WARRIOR card popup and the showcase card.
	// Measured original (b_025, b_028, b_029):
	// - unlocked: UPPERCASE name, round portrait, heart HP / swords ATK row (numbers in the card K format, Toolbox.FormatStatNumber:
	//   < 1000 exact, else floor(x / 1000) + "K");
	// - locked (MY CARDS): title "Locked" (the Locked overlay's LockedTxt replaces the name), dark silhouette of the
	//   portrait on the blue disc of the overlay, padlock, no stats row;
	// - new-unit card: red "+HP delta" tab on the heart, green "+ATK delta" tab on the swords (delta to the previous tier).
	public class ActorTile : MonoBehaviour
	{
		public TextMeshProUGUI titleText;

		public Image tileBackdrop;

		public Image avatarArtwork;

		public GameObject avatarBackdrop;

		public GameObject avatarFrame;

		public RectTransform starStrip;

		public GameObject mightMeter;

		public RectTransform mightMeterInfill;

		public GameObject sealedCover;

		public GameObject detailMeter;

		public Text vitalityText;

		public Text assaultText;

		public GameObject vitalityBoost;

		public GameObject assaultBoost;

		public TextMeshProUGUI vitalityBoostText;

		public TextMeshProUGUI assaultBoostText;

		private readonly float beginBreadth = 75f;

		private readonly float[] mightMeterInfillLimits = new float[2] { 40f, 352f };

		// Locked portrait = the unit sprite in black at 28% over the overlay's blue disc: measured silhouette
		// (28, 97, 144) over the disc colour (39, 135, 200) on b_029 -> 1 - 97/135 = 0.28 on every channel.
		private readonly Color avatarSealedTint = new Color(0f, 0f, 0f, 0.28f);

		private bool _isElaborate;

		private bool _assembled;

		public void Assemble(Sprite picture, string actorTitle, ActorKind actorKind, int stage, int stageTally)
		{
			stage = Mathf.Max(1, stage);
			if (titleText != null)
			{
				titleText.text = actorTitle;
			}
			if (avatarArtwork != null)
			{
				if (picture != null)
				{
					avatarArtwork.sprite = picture;
				}
				avatarArtwork.color = Color.white;
			}
			if (starStrip != null)
			{
				starStrip.sizeDelta = new Vector2(beginBreadth * stage, starStrip.sizeDelta.y);
			}
			if (mightMeterInfill != null)
			{
				float proportion = (stage - 1f) / Mathf.Max(1, stageTally - 1);
				mightMeterInfill.sizeDelta = new Vector2(Mathf.Lerp(mightMeterInfillLimits[0], mightMeterInfillLimits[1], proportion), mightMeterInfill.sizeDelta.y);
			}
			_assembled = true;
			_isElaborate = GameTuning.tileStyle == TileStyle.Elaborate;
			if (mightMeter != null)
			{
				mightMeter.SetActive(!_isElaborate);
			}
			if (detailMeter != null)
			{
				detailMeter.SetActive(_isElaborate);
			}
			int vitality = PlayMechanics.GetActorTileVitality(actorKind, stage);
			int assault = PlayMechanics.GetActorTileAssault(actorKind, stage);
			if (vitalityText != null)
			{
				vitalityText.text = Toolbox.FormatMetricInteger(vitality);
			}
			if (assaultText != null)
			{
				assaultText.text = Toolbox.FormatMetricInteger(assault);
			}
			if (vitalityBoost != null || assaultBoost != null)
			{
				// b_025: TRICERAMON 47 (+27) / 7 (+4) = stats of tier t minus stats of tier t-1.
				bool presentBadges = GameTuning.presentTileBoostBadges && stage > 1;
				if (vitalityBoost != null)
				{
					vitalityBoost.SetActive(presentBadges);
				}
				if (assaultBoost != null)
				{
					assaultBoost.SetActive(presentBadges);
				}
				if (presentBadges)
				{
					if (vitalityBoostText != null)
					{
						vitalityBoostText.text = "+" + Toolbox.FormatMetricInteger(vitality - PlayMechanics.GetActorTileVitality(actorKind, stage - 1));
					}
					if (assaultBoostText != null)
					{
						assaultBoostText.text = "+" + Toolbox.FormatMetricInteger(assault - PlayMechanics.GetActorTileAssault(actorKind, stage - 1));
					}
				}
			}
		}

		public void ReviseStatus(bool isUnsealed)
		{
			if (sealedCover != null)
			{
				sealedCover.SetActive(!isUnsealed);
				// The overlay carries its own "Locked" title at the name's position: hide the unit name under it.
				if (titleText != null)
				{
					titleText.gameObject.SetActive(isUnsealed);
				}
			}
			if (avatarArtwork != null)
			{
				avatarArtwork.color = isUnsealed ? Color.white : avatarSealedTint;
			}
			if (avatarBackdrop != null)
			{
				// Locked: the silhouette sits directly on the overlay's blue disc (no light portrait background / ring).
				avatarBackdrop.SetActive(isUnsealed || sealedCover == null);
			}
			if (avatarFrame != null)
			{
				avatarFrame.SetActive(isUnsealed);
			}
			if (!isUnsealed || _assembled)
			{
				if (mightMeter != null)
				{
					mightMeter.SetActive(isUnsealed && !_isElaborate);
				}
				if (detailMeter != null)
				{
					detailMeter.SetActive(isUnsealed && _isElaborate);
				}
			}
		}
	}
}
