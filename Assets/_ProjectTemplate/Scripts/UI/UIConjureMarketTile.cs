using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Funlab.MergeMaster
{
	// One unit card of the summon store (prefab CharacterSummonCard). Measured original (c_017 / c_018): the unit name in
	// its authored casing ("Triceramon", "Blackhawk" - not upper case), heart HP / swords ATK in the card number format,
	// gem price on the orange strip. Tapping it spends the gems, closes the store and places the unit through the engine
	// (placement order of every new unit, showcase of the summoned unit).
	public class UIConjureMarketTile : MonoBehaviour
	{
		private const string NoRoomLeftNotice = "NO SPACE LEFT!";

		[SerializeField]
		private Image _actorGlyph;

		[SerializeField]
		private TMP_Text _actorTitle;

		[SerializeField]
		private TMP_Text _feeCaption;

		[SerializeField]
		private Button _procureControl;

		[SerializeField]
		private GameObject _freshIndicator;

		[SerializeField]
		private TMP_Text _assaultCaption;

		[SerializeField]
		private TMP_Text _vitalityCaption;

		private ActorKind kind;

		private int worth;

		private int fee;

		private UIConjureMarket conjureMarket;

		private void Start()
		{
			if (_procureControl == null)
			{
				_procureControl = GetComponent<Button>();
			}
			if (_procureControl != null)
			{
				_procureControl.onClick.AddListener(OnTap);
			}
			ResolveMarket();
		}

		private UIConjureMarket ResolveMarket()
		{
			if (conjureMarket == null)
			{
#if UNITY_LUNA
				conjureMarket = GetComponentInParent<UIConjureMarket>();
#else
				conjureMarket = GetComponentInParent<UIConjureMarket>(true);
#endif
			}
			return conjureMarket;
		}

		public void SetTroop(ActorKind actorKind, int troopWorth, bool isFresh, int troopFee)
		{
			kind = actorKind;
			this.worth = Mathf.Max(1, troopWorth);
			this.fee = troopFee;
			int stage = Mathf.Clamp(PlayMechanics.GetActorGrade(this.worth), 1, 11);
			if (_actorGlyph != null && MatchAssets.Shared != null)
			{
				_actorGlyph.sprite = MatchAssets.Shared.GetActorPicture(kind, this.worth);
			}
			if (_actorTitle != null)
			{
				_actorTitle.text = GetTroopTitle(stage);
			}
			if (_feeCaption != null)
			{
				_feeCaption.text = troopFee.ToString();
			}
			if (_freshIndicator != null)
			{
				_freshIndicator.SetActive(isFresh);
			}
			if (_assaultCaption != null)
			{
				_assaultCaption.text = Toolbox.FormatMetricInteger(PlayMechanics.GetActorTileAssault(kind, stage));
			}
			if (_vitalityCaption != null)
			{
				_vitalityCaption.text = Toolbox.FormatMetricInteger(PlayMechanics.GetActorTileVitality(kind, stage));
			}
		}

		private string GetTroopTitle(int stage)
		{
			GameplayCore gameplayCore = GameplayCore.Instance;
			string[] titles = gameplayCore == null ? null : (kind == ActorKind.Person ? gameplayCore.personTitles : gameplayCore.saurianTitles);
			if (titles == null || titles.Length == 0)
			{
				return string.Empty;
			}
			string troopTitle = titles[Mathf.Clamp(stage - 1, 0, titles.Length - 1)];
			return string.IsNullOrEmpty(troopTitle) ? string.Empty : troopTitle;
		}

		public void OnTap()
		{
			UIConjureMarket market = ResolveMarket();
			GameplayCore gameplayCore = GameplayCore.Instance;
			if (gameplayCore == null || gameplayCore.activity > ActivityStatus.Unfusing)
			{
				return;
			}
			if (!gameplayCore.emptyLatticeSlotExists)
			{
				if (UIDirector.Shared != null)
				{
					UIDirector.Shared.PresentCaution(NoRoomLeftNotice);
				}
				return;
			}
			if (PlayMechanics.jewels < fee)
			{
				if (market != null)
				{
					market.RevealJewelsDialog();
				}
				return;
			}
			PlayMechanics.jewels -= fee;
			PlayMechanics.TraceResource(false, "summon_popup", PlayMechanics.JewelResource, fee);
			// c_019/c_020: the store closes, then the summoned unit appears on the board with its showcase.
			if (market != null)
			{
				market.DismissWindow();
			}
			if (gameplayCore.audioDirector != null)
			{
				gameplayCore.audioDirector.PerformUICue(AudioCue.CombatantAcquisition);
			}
			gameplayCore.SlotCombatant(kind, true, worth, GameplayCore.IsFreshGrade(kind, worth));
		}
	}
}
