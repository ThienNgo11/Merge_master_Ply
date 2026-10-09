using System.Collections.Generic;
using DatdevUlts.AnimationUtils;
using GameTool.UI.Scripts.CanvasPopup;
using GameToolSample.UIManager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Funlab.MergeMaster
{
	// Summon store (Resources/UI/SummonPopup, header button from level 3): buy any already unlocked tier with gems.
	// Measured original (c_017 / c_018): orange "SUMMON" ribbon, tabs Dinosaurs / Warriors, one pink card per unlocked
	// tier from the highest tier down (Triceramon before Vraptormon), red "NEW" ribbon on the newest (highest) tier of
	// each tab, price = summon_costs[tier - 1] gems (10, 15, 20, 30, 60, 100, 200, 500, 1250, 3500, 10000),
	// green "GET MORE GEMS!" button.
	public class UIConjureMarket : BaseUI
	{
		private const string PopupOpenMotion = "open";

		private const int PeakGrade = 11;

		[SerializeField]
		private AnimatorController popupMotion;

		private UIDirector _uiDirector;

		private ScrollRect _scrollBox;

		private ScrollRect.MovementType _scrollMovement;

		private Transform _popupFrame;

		private Coroutine _scrollHold;

		private System.Action _settleScroll;

		private int[] _fees;

		[SerializeField]
		private GameObject _sauriansTab;

		[SerializeField]
		private GameObject _personsTab;

		[SerializeField]
		private Transform _sauriansHolder;

		[SerializeField]
		private Transform _personsHolder;

		[SerializeField]
		private UIConjureMarketTile _conjureTileTemplate;

		[SerializeField]
		private Animation _getJewelsControlMotion;

		[SerializeField]
		public UITabStrip tabStrip;

		private bool _controlsWired;

		private bool _tileHoldersWiped;

		private readonly List<UIConjureMarketTile> _saurianTiles = new List<UIConjureMarketTile>();

		private readonly List<UIConjureMarketTile> _personTiles = new List<UIConjureMarketTile>();

		public UIDirector uiDirector
		{
			get
			{
				if (_uiDirector == null)
				{
					_uiDirector = UIDirector.Shared;
				}
				return _uiDirector;
			}
		}

		private void Awake()
		{
			GuaranteeFees();
			WireControls();
		}

		private void OnEnable()
		{
			if (popupMotion != null)
			{
				popupMotion.SetAnimation(PopupOpenMotion, false, forceReplay: true);
				if (popupMotion.Awaked)
				{
					popupMotion.Update();
				}
			}
		}

		private void GuaranteeFees()
		{
			if (_fees == null || _fees.Length == 0)
			{
				_fees = GameTuning.ConjureCharges;
			}
		}

		// DismissCtrl and RevealJewelsDialog have no persistent targets in the scene.
		private void WireControls()
		{
			if (_controlsWired)
			{
				return;
			}
			_controlsWired = true;
			Transform dismissPivot = transform.Find("Content/CloseBtn");
			Button dismissControl = dismissPivot != null ? dismissPivot.GetComponent<Button>() : null;
			if (dismissControl != null)
			{
				dismissControl.onClick.AddListener(DismissWindow);
			}
			Button jewelsControl = _getJewelsControlMotion != null ? _getJewelsControlMotion.GetComponent<Button>() : null;
			if (jewelsControl == null)
			{
				Transform jewelsPivot = transform.Find("Content/OpenGemsPopup");
				jewelsControl = jewelsPivot != null ? jewelsPivot.GetComponent<Button>() : null;
			}
			if (jewelsControl != null)
			{
				jewelsControl.onClick.AddListener(RevealJewelsDialog);
			}
		}

		public void Present()
		{
			GuaranteeFees();
			WireControls();
			BootstrapTroopsSeries();
			ChooseTab(PlayMechanics.latestUnsealedActorKind);
			if (!gameObject.activeSelf)
			{
				gameObject.SetActive(true);
			}
			if (_getJewelsControlMotion != null)
			{
				_getJewelsControlMotion.Play();
			}
			PlayMechanics.conjureMarketNoticePending = false;
			HoldScrollWhileOpening();
		}

		private void HoldScrollWhileOpening()
		{
			if (_scrollBox == null)
			{
				_scrollBox = GetComponentInChildren<ScrollRect>(true);
				if (_scrollBox == null)
				{
					return;
				}
				_scrollMovement = _scrollBox.movementType;
			}
			if (!isActiveAndEnabled)
			{
				return;
			}
			if (_popupFrame == null)
			{
				_popupFrame = transform.Find("Content");
			}
			if (_settleScroll == null)
			{
				_settleScroll = SettleScroll;
			}
			if (_scrollHold != null)
			{
				StopCoroutine(_scrollHold);
			}
			_scrollHold = StartCoroutine(Toolbox.HoldScrollWhileGrowing(_scrollBox, _popupFrame, _scrollMovement, _settleScroll));
		}

		private void SettleScroll()
		{
			_scrollHold = null;
			Toolbox.ScrollToTop(_scrollBox);
		}

		public void DismissWindow()
		{
			if (!gameObject.activeSelf)
			{
				return;
			}
			if (uiName != eUIName.None)
			{
				base.Pop();
			}
			else
			{
				gameObject.SetActive(false);
			}
		}

		private void BootstrapTroopsSeries()
		{
			if (!_tileHoldersWiped)
			{
				_tileHoldersWiped = true;
				WipeDescendants(_sauriansHolder);
				WipeDescendants(_personsHolder);
			}
			ReleaseTileChoice();
			if (_conjureTileTemplate == null)
			{
				ConcealTiles(_saurianTiles, 0);
				ConcealTiles(_personTiles, 0);
				return;
			}
			PopulateTroops(ActorKind.Saurian, _sauriansHolder, _saurianTiles);
			PopulateTroops(ActorKind.Person, _personsHolder, _personTiles);
		}

		private void ReleaseTileChoice()
		{
			EventSystem framework = EventSystem.current;
			GameObject chosen = framework != null ? framework.currentSelectedGameObject : null;
			if (chosen == null)
			{
				return;
			}
			Transform chosenPivot = chosen.transform;
			if ((_sauriansHolder != null && chosenPivot.IsChildOf(_sauriansHolder)) || (_personsHolder != null && chosenPivot.IsChildOf(_personsHolder)))
			{
				framework.SetSelectedGameObject(null);
			}
		}

		private static void ConcealTiles(List<UIConjureMarketTile> tiles, int firstSlot)
		{
			for (int idx = firstSlot; idx < tiles.Count; idx++)
			{
				if (tiles[idx] != null && tiles[idx].gameObject.activeSelf)
				{
					tiles[idx].gameObject.SetActive(false);
				}
			}
		}

		private static void WipeDescendants(Transform holder)
		{
			if (holder == null)
			{
				return;
			}
			for (int idx = holder.childCount - 1; idx >= 0; idx--)
			{
				GameObject descendant = holder.GetChild(idx).gameObject;
				descendant.SetActive(false);
				Destroy(descendant);
			}
		}

		private void PopulateTroops(ActorKind kind, Transform holder, List<UIConjureMarketTile> tiles)
		{
			if (holder == null)
			{
				return;
			}
			int peakWorth = 1;
			if (PlayMechanics.peakUnsealedActors != null && PlayMechanics.peakUnsealedActors.TryGetValue(kind, out int unsealed) && unsealed > 0)
			{
				peakWorth = unsealed;
			}
			int peakGrade = Mathf.Clamp(PlayMechanics.GetActorGrade(peakWorth), 1, PeakGrade);
			// Highest tier first (c_017: Triceramon | Vraptormon); the NEW ribbon marks the newest tier of every tab
			// (c_017 Triceramon, c_018 Blackhawk - the only warrior tier - both NEW on the first opening at level 3).
			int slot = 0;
			for (int grade = peakGrade; grade >= 1; grade--)
			{
				int worth = 1 << (grade - 1);
				bool freshIndicator = grade == peakGrade;
				UIConjureMarketTile tile = slot < tiles.Count ? tiles[slot] : null;
				if (tile == null)
				{
					tile = Instantiate(_conjureTileTemplate, holder);
					if (slot < tiles.Count)
					{
						tiles[slot] = tile;
					}
					else
					{
						tiles.Add(tile);
					}
				}
				else if (!tile.gameObject.activeSelf)
				{
					tile.gameObject.SetActive(true);
				}
				tile.transform.SetSiblingIndex(slot);
				tile.SetTroop(kind, worth, freshIndicator, GetFee(grade));
				slot++;
			}
			ConcealTiles(tiles, slot);
		}

		private int GetFee(int grade)
		{
			GuaranteeFees();
			if (_fees == null || _fees.Length == 0)
			{
				return 0;
			}
			return _fees[Mathf.Clamp(grade - 1, 0, _fees.Length - 1)];
		}

		public void ChooseTab(ActorKind actorKind)
		{
			if (tabStrip != null)
			{
				tabStrip.SetLiveControl(actorKind == ActorKind.Person ? 1 : 0, 1f, true);
			}
		}

		public void RevealJewelsDialog()
		{
			if (uiDirector != null)
			{
				uiDirector.PresentInAppStorefrontDialog();
			}
		}
	}
}
