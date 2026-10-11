using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using GameToolSample.Audio;
using GameToolSample.GamePlay.Manager;
using GameToolSample.Scripts.Enum;
using GameToolSample.UIManager;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Funlab.MergeMaster
{
	public class UIDirector : MotionSignalResponder
	{
		private static UIDirector _shared;

		public static UIDirector Shared
		{
			get
			{
				if (_shared == null)
				{
#if UNITY_LUNA
					_shared = FindObjectOfType<UIDirector>();
#else
					_shared = FindObjectOfType<UIDirector>(true);
#endif
				}
				return _shared;
			}
		}

		[Header("EXTERNAL")]
		public GameplayCore gameplayCore;

		[Header("CANVAS & BASE")]
		public Canvas viewportCanvas;
		public GameObject dissolver;

		[Header("STAGE TEXT")]
		public TextMeshProUGUI stageText;
		[Header("COIN / FUNDS DISPLAY")]
		public TextMeshProUGUI cashText;
		public UIFundsSoarMotion moneyFundsSoarMotion;

		[Header("BATTLE CONTROLS")]
		public GameObject clashCtrl;
		public GameObject gamerDirector;
		[SerializeField] private Animator bladeClashMotion;

		[Header("TROOP BUY BUTTONS")]
		public Button[] feeControls;
		public TextMeshProUGUI[] feeTags;

		[Header("HEALTH BARS")]
		public GameObject combatantVitalityMeters;
		public RectTransform[] combatantVitalityInfills;

		[Header("HOW TO PLAY / TUTORIAL")]
		public GameObject controlHighlighter;
		public GameObject[] sceneHowTo;

		[Header("AUDIO SFX")]
		public eSoundName swooshSample;
		public eSoundName bladeClashSample;

		// Non-serialized properties for external compatibility (will NEVER appear on Inspector)
		public Dialog liveDialog { get => null; set { } }
		public Animation tileControlMotion => null;
		public RectTransform mastheadTilesControlLoc => null;

		// Internal battle states
		private bool isClashing;
		private float[] combatantBeginVitalities = new float[2];
		private float _vitalityInfillBreadth = 246f;
		private Animation clashCtrlMotion;

		private enum ArenaPhase
		{
			ClashSolely = 0,
			ProcureSoldier = 1,
			ShiftSaurianForward = 2,
			ProcureSaurian = 3,
			FuseSaurians = 4,
			Usual = 5
		}

		private ArenaPhase _arenaPhase = ArenaPhase.Usual;
		private const int ProcureControlsFloorStage = 3;
		private bool[] _troopControlTransitionsDestroyed = new bool[2];

		// ------------------------------------------------------------------ Lifecycle

		private void Awake()
		{
			_shared = this;

			if (clashCtrl != null)
			{
				clashCtrlMotion = clashCtrl.GetComponent<Animation>();
			}

			if (combatantVitalityInfills != null && combatantVitalityInfills.Length > 0 && combatantVitalityInfills[0] != null)
			{
				if (combatantVitalityInfills[0].sizeDelta.x > 0f)
				{
					_vitalityInfillBreadth = combatantVitalityInfills[0].sizeDelta.x;
				}
			}

			if (combatantVitalityMeters != null)
			{
				combatantVitalityMeters.SetActive(false);
			}

			PlayMechanics.MoneyAltered += OnMoneyAltered;
		}

		private bool _storeRedirectTriggered;
		private float _stage4EnteredTime = -1f;

		private void Start()
		{
			if (EventSystem.current != null)
			{
				EventSystem.current.sendNavigationEvents = false;
			}

			Reset();
			ReviseMoney();
		}

		private void Update()
		{
			if (GameTuning.stage >= 4 && !_storeRedirectTriggered)
			{
				if (_stage4EnteredTime < 0f)
				{
					_stage4EnteredTime = Time.unscaledTime;
				}

				if (Time.unscaledTime - _stage4EnteredTime >= 0.05f)
				{
					if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
					{
						_storeRedirectTriggered = true;
						GameManager.Instance.InstallFullGame();
					}
				}
			}
		}

		private void OnDestroy()
		{
			CancelInvoke();
			PlayMechanics.MoneyAltered -= OnMoneyAltered;
			if (_shared == this)
			{
				_shared = null;
			}
		}

		private void OnMoneyAltered(long amount)
		{
			ReviseMoney();
		}

		// ------------------------------------------------------------------ Public UI Controls

		public void Reset()
		{
			isClashing = false;
			AssignControlHighlighterLive(false);
			RenewStageTag();
			ReviseGamerDirector(false);
		}

		public void RenewStageTag()
		{
			if (stageText != null)
			{
				stageText.gameObject.SetActive(!isClashing);
				stageText.text = "LEVEL " + GameTuning.stage;
			}
		}

		public void ReviseGamerDirector(bool dispatchTelemetry = true)
		{
			if (gamerDirector == null || gameplayCore == null)
			{
				return;
			}

			if (isClashing)
			{
				gamerDirector.SetActive(false);
				if (clashCtrl != null) clashCtrl.SetActive(false);
				return;
			}

			gamerDirector.SetActive(true);

			GuideKind guide = GameTuning.finishedGuideKind;
			int stage = GameTuning.stage;
			if (guide >= GuideKind.PersonAcquisition)
			{
				DestroyTroopControlDOTweenComponentAndRevertSize(true);
			}
			if (guide >= GuideKind.SaurianAcquisition)
			{
				DestroyTroopControlDOTweenComponentAndRevertSize();
			}

			_arenaPhase = GetArenaPhase(guide, stage);
			bool clashOnscreen = IsClashControlOnscreen(_arenaPhase);
			if (clashCtrl != null)
			{
				clashCtrl.SetActive(clashOnscreen);
			}
			ImposeGuideGating(_arenaPhase, out bool personPulse, out bool saurianPulse);
			RenewFeeTags();

			if (personPulse)
			{
				PerformTroopControlPulse(0);
			}
			if (saurianPulse)
			{
				PerformTroopControlPulse(1);
			}

			if (clashOnscreen)
			{
				BeginClashControlMotion();
			}
			else
			{
				HaltClashControlMotion();
			}
		}

		private static ArenaPhase GetArenaPhase(GuideKind guide, int stage)
		{
			switch (GameplayCore.GetGuidePhase())
			{
			case GameplayCore.GuidePhase.ProcureSoldier:
				return ArenaPhase.ProcureSoldier;
			case GameplayCore.GuidePhase.ShiftForward:
				return ArenaPhase.ShiftSaurianForward;
			case GameplayCore.GuidePhase.ProcureSaurian:
				return ArenaPhase.ProcureSaurian;
			case GameplayCore.GuidePhase.Fuse:
				return ArenaPhase.FuseSaurians;
			case GameplayCore.GuidePhase.Unset:
				return stage >= ProcureControlsFloorStage ? ArenaPhase.Usual : ArenaPhase.ClashSolely;
			default:
				return ArenaPhase.ClashSolely;
			}
		}

		private bool IsClashControlOnscreen(ArenaPhase phase)
		{
			if (!HasGuardians())
			{
				return false;
			}
			switch (phase)
			{
			case ArenaPhase.ProcureSoldier:
			case ArenaPhase.ProcureSaurian:
				return false;
			case ArenaPhase.ShiftSaurianForward:
				return !HasGuardianOfKind(ActorKind.Saurian);
			case ArenaPhase.FuseSaurians:
				return !HasFusablePair();
			default:
				return true;
			}
		}

		private void ImposeGuideGating(ArenaPhase phase, out bool personPulse, out bool saurianPulse)
		{
			bool procureSoldier = phase == ArenaPhase.ProcureSoldier;
			bool procureSaurian = phase == ArenaPhase.ProcureSaurian;
			bool usual = phase == ArenaPhase.Usual;
			personPulse = procureSoldier;
			saurianPulse = procureSaurian;

			bool personOnscreen = usual || procureSoldier;
			bool saurianOnscreen = usual || procureSaurian;

			if (feeControls != null)
			{
				if (feeControls.Length > 0 && feeControls[0] != null)
				{
					feeControls[0].gameObject.SetActive(personOnscreen);
				}
				if (feeControls.Length > 1 && feeControls[1] != null)
				{
					feeControls[1].gameObject.SetActive(saurianOnscreen);
				}
			}

			AssignControlHighlighterLive(procureSoldier || procureSaurian);
		}

		private void AssignControlHighlighterLive(bool live)
		{
			if (controlHighlighter == null || controlHighlighter.activeSelf == live)
			{
				return;
			}
			controlHighlighter.SetActive(live);
			if (live)
			{
				DOTweenAnimation dissolve = controlHighlighter.GetComponent<DOTweenAnimation>();
				if (dissolve != null)
				{
					dissolve.DORestart();
				}
			}
		}

		private void PerformTroopControlPulse(int ordinal)
		{
			if (ordinal < 0 || ordinal >= _troopControlTransitionsDestroyed.Length || _troopControlTransitionsDestroyed[ordinal])
			{
				return;
			}
			Button control = (feeControls != null && ordinal < feeControls.Length) ? feeControls[ordinal] : null;
			if (control == null || !control.gameObject.activeInHierarchy)
			{
				return;
			}
			DOTweenAnimation[] throbs = control.GetComponentsInChildren<DOTweenAnimation>();
			bool resting = throbs.Length > 0 && (throbs[0].tween == null || !throbs[0].tween.IsPlaying());
			foreach (DOTweenAnimation throb in throbs)
			{
				if (resting)
				{
					throb.DORestart();
				}
				else
				{
					throb.DOPlay();
				}
			}
		}

		private void RenewFeeTags()
		{
			for (int slot = 0; slot < 2; slot++)
			{
				ActorKind kind = slot == 0 ? ActorKind.Person : ActorKind.Saurian;
				if (feeTags != null && slot < feeTags.Length && feeTags[slot] != null)
				{
					long charge = PlayMechanics.GetCharge(kind);
					feeTags[slot].text = charge >= 1000 ? $"{charge / 1000f:0.#}K" : charge.ToString();
				}
				if (feeControls != null && slot < feeControls.Length && feeControls[slot] != null)
				{
					bool canAfford = PlayMechanics.CanAffordAcquisition(kind);
					bool hasSpace = gameplayCore == null || gameplayCore.emptyLatticeSlotExists;
					feeControls[slot].interactable = canAfford && hasSpace;
				}
			}
		}

		private bool HasGuardianOfKind(ActorKind kind)
		{
			if (!HasGuardians())
			{
				return false;
			}
			foreach (Combatant combatant in gameplayCore.registry.guardians)
			{
				if (combatant != null && combatant.kind == kind && combatant.vitality > 0f)
				{
					return true;
				}
			}
			return false;
		}

		private bool HasFusablePair()
		{
			if (!HasGuardians())
			{
				return false;
			}
			HashSet<int> visited = new HashSet<int>();
			foreach (Combatant combatant in gameplayCore.registry.guardians)
			{
				if (combatant == null)
				{
					continue;
				}
				int entry = ((int)combatant.kind * 100000) + combatant.worth;
				if (!visited.Add(entry))
				{
					return true;
				}
			}
			return false;
		}

		public void ConcealGamerInteractionControls()
		{
			if (gamerDirector != null) gamerDirector.SetActive(false);
			if (clashCtrl != null) clashCtrl.SetActive(false);
		}

		public void PresentGamerInteractionControls()
		{
			ReviseGamerDirector(false);
		}

		public void FlipMatchFrameUILive(bool live)
		{
			if (stageText != null) stageText.gameObject.SetActive(live && !isClashing);
			if (gamerDirector != null) gamerDirector.SetActive(live && !isClashing);
			if (clashCtrl != null) clashCtrl.SetActive(live && !isClashing && HasGuardians());
		}

		// ------------------------------------------------------------------ Troop Acquisition

		private void AcquireTroop(ActorKind kind)
		{
			if (isClashing || gameplayCore == null)
			{
				return;
			}

			if (!gameplayCore.emptyLatticeSlotExists)
			{
				PresentCaution("NO SPACE LEFT!");
				return;
			}

			if (!PlayMechanics.CanAffordAcquisition(kind))
			{
				return;
			}

			GuideKind prior = GameTuning.finishedGuideKind;
			gameplayCore.SlotCombatant(kind, false);

			if (kind == ActorKind.Person && prior == GuideKind.Unset)
			{
				AssignControlHighlighterLive(false);
				DestroyTroopControlDOTweenComponentAndRevertSize(true);
			}
			else if (kind == ActorKind.Saurian && prior < GuideKind.SaurianAcquisition)
			{
				AssignControlHighlighterLive(false);
				DestroyTroopControlDOTweenComponentAndRevertSize();
			}

			ReviseGamerDirector(false);
		}

		// ------------------------------------------------------------------ Battle & Clash

		private void OnBeginClashControl()
		{
			if (gameplayCore == null || isClashing || !HasGuardians())
			{
				return;
			}

			HaltClashControlMotion();
			isClashing = true;

			if (clashCtrl != null) clashCtrl.SetActive(false);
			if (gamerDirector != null) gamerDirector.SetActive(false);
			if (controlHighlighter != null) controlHighlighter.SetActive(false);

			if (bladeClashMotion != null)
			{
				bladeClashMotion.gameObject.SetActive(true);
				bladeClashMotion.Play("StartFight", 0, 0f);
				StartCoroutine(ConcealBladeRoutine());
			}

			if (bladeClashSample != eSoundName.None && gameplayCore.audioDirector != null)
			{
				gameplayCore.audioDirector.PerformUISample(bladeClashSample);
			}

			PresentCombatantVitalityMeters();
			gameplayCore.BeginClash();
		}

		private IEnumerator ConcealBladeRoutine()
		{
			yield return new WaitForSeconds(0.9f);
			if (bladeClashMotion != null)
			{
				bladeClashMotion.gameObject.SetActive(false);
			}
		}

		public void FlipLiveClashControlMotion(bool live)
		{
			if (clashCtrlMotion == null) return;
			if (live)
			{
				clashCtrlMotion.Play();
			}
			else
			{
				clashCtrlMotion.Stop();
				if (clashCtrl != null) clashCtrl.transform.localScale = Vector3.one;
			}
		}

		public void BeginClashControlMotion()
		{
			FlipLiveClashControlMotion(true);
		}

		public void HaltClashControlMotion()
		{
			FlipLiveClashControlMotion(false);
		}

		// ------------------------------------------------------------------ Vitality / Health Bars

		public void PresentCombatantVitalityMeters()
		{
			if (combatantVitalityMeters == null || gameplayCore == null || gameplayCore.registry == null)
			{
				return;
			}

			float foe = gameplayCore.registry.GetLeftoverVitality(ActorFaction.Foe);
			float guardian = gameplayCore.registry.GetLeftoverVitality(ActorFaction.Guardian);
			combatantBeginVitalities[0] = foe > 0f ? foe : PlayMechanics.foeBeginVitality;
			combatantBeginVitalities[1] = guardian > 0f ? guardian : PlayMechanics.guardianBeginVitality;

			for (int slot = 0; slot < 2; slot++)
			{
				if (combatantVitalityInfills != null && slot < combatantVitalityInfills.Length && combatantVitalityInfills[slot] != null)
				{
					RectTransform infill = combatantVitalityInfills[slot];
					infill.DOKill();
					infill.localScale = Vector3.one;
					infill.sizeDelta = new Vector2(_vitalityInfillBreadth, infill.sizeDelta.y);
				}
			}

			combatantVitalityMeters.SetActive(true);
		}

		public void ReviseCombatantVitalityMeter(ActorFaction faction)
		{
			if (gameplayCore == null || gameplayCore.registry == null || combatantBeginVitalities == null)
			{
				return;
			}

			int ordinal = faction == ActorFaction.Foe ? 0 : 1;
			if (combatantVitalityInfills == null || ordinal >= combatantVitalityInfills.Length || combatantVitalityInfills[ordinal] == null)
			{
				return;
			}

			float begin = combatantBeginVitalities[ordinal];
			if (begin <= 0f)
			{
				begin = faction == ActorFaction.Foe ? PlayMechanics.foeBeginVitality : PlayMechanics.guardianBeginVitality;
			}

			float proportion = begin > 0f ? Mathf.Clamp01(gameplayCore.registry.GetLeftoverVitality(faction) / begin) : 0f;
			RectTransform infill = combatantVitalityInfills[ordinal];
			infill.DOKill();
			infill.DOSizeDelta(new Vector2(_vitalityInfillBreadth * proportion, infill.sizeDelta.y), 0.15f).SetRecyclable(true);
		}

		// ------------------------------------------------------------------ Stage Flow (Playable Flow)

		public void DidFinishStage()
		{
		}

		public IEnumerator ArrangeStageOutro(StageOutcome stageOutcome, long income)
		{
			isClashing = false;

			if (combatantVitalityMeters != null) combatantVitalityMeters.SetActive(false);
			if (gamerDirector != null) gamerDirector.SetActive(false);
			if (clashCtrl != null) clashCtrl.SetActive(false);

			yield return new WaitForSeconds(1.2f);

			if (stageOutcome == StageOutcome.Accomplishment)
			{
				// Win: Level 1, 2, 3 transition to next level (L1->L2, L2->L3, L3->L4)
				if (GameTuning.stage < 4)
				{
					JumpToUpcomingStage(true);
				}
				else
				{
					GameManager.Instance.InstallFullGame();
				}
			}
			else
			{
				// Defeat: at Level >= 4, open store; earlier levels replay
				if (GameTuning.stage >= 4)
				{
					GameManager.Instance.InstallFullGame();
				}
				else
				{
					JumpToUpcomingStage(false);
				}
			}
		}

		public void JumpToUpcomingStage(bool isWin = true)
		{
			isClashing = false;
			if (isWin)
			{
				GameTuning.stage++;
				if (GameTuning.stage >= 4)
				{
					_stage4EnteredTime = Time.unscaledTime;
				}
			}
			if (gameplayCore != null)
			{
				gameplayCore.FetchStage();
			}
			RenewStageTag();
			ReviseGamerDirector(false);
		}

		// ------------------------------------------------------------------ Button Click Dispatcher

		public void ControlResponder(Button dispatcher)
		{
			if (dispatcher == null) return;

			if (GameTuning.stage >= 4)
			{
				_storeRedirectTriggered = true;
				GameManager.Instance.InstallFullGame();
				return;
			}

			switch (dispatcher.name)
			{
				case "HumanBtn":
				case "HumanRVBtn":
				case "HumanRVBtn4":
					AcquireTroop(ActorKind.Person);
					break;

				case "DinoBtn":
				case "DinoRVBtn":
				case "DinoRVBtn4":
					AcquireTroop(ActorKind.Saurian);
					break;

				case "StartFightBtn":
					OnBeginClashControl();
					break;

				default:
					// Any other clicks (Settings, Cards, IAP Shop) lead directly to store in Playable
					GameManager.Instance.InstallFullGame();
					break;
			}
		}

		// ------------------------------------------------------------------ Helpers & No-op Shims

		private bool HasGuardians()
		{
			return gameplayCore != null && gameplayCore.registry != null && gameplayCore.registry.guardians != null && gameplayCore.registry.guardians.Count > 0;
		}

		public void FlipShiftForwardGuide(bool show)
		{
			if (sceneHowTo != null && sceneHowTo.Length > 2 && sceneHowTo[2] != null)
			{
				sceneHowTo[2].SetActive(show);
			}
		}

		public void PresentCaution(string caution)
		{
			Debug.LogWarning(caution);
		}

		public void PresentInAppStorefrontDialog()
		{
			GameManager.Instance.InstallFullGame();
		}

		public void PresentConjureMarket()
		{
			GameManager.Instance.InstallFullGame();
		}

		public void PresentDialog(eUIName name) { }
		public void PresentActorTile(ActorKind kind, int worth) { }
		public void ReviseTags() { }
		public void ReviseAcquisitionControls()
		{
			RenewFeeTags();
		}
		public void ReviseMoney()
		{
			RenewCashTag();
			RenewFeeTags();
		}

		public void RenewCashTag()
		{
			bool isTweening = moneyFundsSoarMotion != null && moneyFundsSoarMotion.IsTweening;
			if (isTweening)
			{
				return;
			}
			long presentedSum = PlayMechanics.money;
			if (cashText != null)
			{
				cashText.text = Toolbox.CompactNumeralToThousands(presentedSum);
			}
			else if (moneyFundsSoarMotion != null)
			{
				moneyFundsSoarMotion.RenewCaption(presentedSum);
			}
		}
		public void FlipMastheadCashMotion(bool play) { }
		public void PresentJewelsMotion() { }
		public int GetActorGradeTally(ActorKind kind, int stage) => 11;
		public bool IsAnyExclusiveVidControlLive() => false;
		public void FlipLiveHotVidControlsMotion(bool live) { }
		public void DestroyTroopControlDOTweenComponentAndRevertSize(bool isHuman = false)
		{
			DestroyTroopControlTransitions(0);
			if (!isHuman)
			{
				DestroyTroopControlTransitions(1);
			}
		}

		private void DestroyTroopControlTransitions(int ordinal)
		{
			if (ordinal < 0 || ordinal >= _troopControlTransitionsDestroyed.Length || _troopControlTransitionsDestroyed[ordinal])
			{
				return;
			}
			Button control = (feeControls != null && ordinal < feeControls.Length) ? feeControls[ordinal] : null;
			if (control == null)
			{
				return;
			}
			_troopControlTransitionsDestroyed[ordinal] = true;
			GameObject node = control.gameObject;
			foreach (DOTweenAnimation transitionMotion in node.GetComponentsInChildren<DOTweenAnimation>(true))
			{
				transitionMotion.DOKill();
				Destroy(transitionMotion);
			}
			foreach (DOTweenVisualManager visualCoordinator in node.GetComponentsInChildren<DOTweenVisualManager>(true))
			{
				Destroy(visualCoordinator);
			}
			DOTween.Kill(node);
			node.transform.DOKill();
			node.transform.localScale = Vector3.one;
		}
		public bool PresentIncentivizedFilm(PrizeKind prizeKind) => false;
		public void OnMiniatureControlTapResponder() { }
		public void ProcessMiniatureExit() { }
		public void TraceInAppStorefrontRevealFromMastheadTelemetrySignal() { }
		public bool DidTouchMastheadBarrier(PointerEventData eventData) => false;
		public static float GetAspectProportion() => Screen.width > 0 ? (float)Screen.height / Screen.width : 16f / 9f;
		public override void MotionSignal(string signal) { }
	}
}
