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
		public UIFundsSoarMotion moneyFundsSoarMotion => null;
		public RectTransform mastheadTilesControlLoc => null;

		// Internal battle states
		private bool isClashing;
		private float[] combatantBeginVitalities = new float[2];
		private float _vitalityInfillBreadth = 246f;
		private Animation clashCtrlMotion;

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

			// Unlimited currency in Playable so buying troops is never blocked
			PlayMechanics.money = 999999;

			if (combatantVitalityMeters != null)
			{
				combatantVitalityMeters.SetActive(false);
			}
		}

		private void Start()
		{
			if (EventSystem.current != null)
			{
				EventSystem.current.sendNavigationEvents = false;
			}

			Reset();
		}

		private void OnDestroy()
		{
			CancelInvoke();
			if (_shared == this)
			{
				_shared = null;
			}
		}

		// ------------------------------------------------------------------ Public UI Controls

		public void Reset()
		{
			isClashing = false;
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
			if (gamerDirector == null)
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

			// Always keep buy buttons active and clickable in Playable
			if (feeControls != null)
			{
				for (int i = 0; i < feeControls.Length; i++)
				{
					if (feeControls[i] != null)
					{
						feeControls[i].gameObject.SetActive(true);
						feeControls[i].interactable = true;
					}
				}
			}

			// Update fight button state
			bool hasTroops = HasGuardians();
			if (clashCtrl != null)
			{
				clashCtrl.SetActive(hasTroops);
			}

			if (hasTroops)
			{
				BeginClashControlMotion();
			}
			else
			{
				HaltClashControlMotion();
			}
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

			PlayMechanics.money = 999999;
			gameplayCore.SlotCombatant(kind, false);

			if (controlHighlighter != null)
			{
				controlHighlighter.SetActive(false);
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
				// Win: Level 1 - 6 transition to next level; Level 7 opens store
				if (GameTuning.stage < 7)
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
				// Defeat: Replay current level
				JumpToUpcomingStage(false);
			}
		}

		public void JumpToUpcomingStage(bool isWin = true)
		{
			isClashing = false;
			if (isWin)
			{
				GameTuning.stage++;
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
			if (sceneHowTo != null && sceneHowTo.Length > 0 && sceneHowTo[0] != null)
			{
				sceneHowTo[0].SetActive(show);
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
		public void ReviseAcquisitionControls() { }
		public void ReviseMoney() { }
		public void RenewCashTag() { }
		public void FlipMastheadCashMotion(bool play) { }
		public void PresentJewelsMotion() { }
		public int GetActorGradeTally(ActorKind kind, int stage) => 11;
		public bool IsAnyExclusiveVidControlLive() => false;
		public void FlipLiveHotVidControlsMotion(bool live) { }
		public void DestroyTroopControlDOTweenComponentAndRevertSize(bool isHuman = false) { }
		public bool PresentIncentivizedFilm(PrizeKind prizeKind) => false;
		public void OnMiniatureControlTapResponder() { }
		public void ProcessMiniatureExit() { }
		public void TraceInAppStorefrontRevealFromMastheadTelemetrySignal() { }
		public bool DidTouchMastheadBarrier(PointerEventData eventData) => false;
		public static float GetAspectProportion() => Screen.width > 0 ? (float)Screen.height / Screen.width : 16f / 9f;
		public override void MotionSignal(string signal) { }
	}
}
