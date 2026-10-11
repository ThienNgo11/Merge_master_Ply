using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using DG.Tweening;
using GameTool.Assistants.DesignPattern;
using GameTool.ObjectPool.Scripts;
using GameToolSample.Audio;
using GameToolSample.UIManager;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

namespace Funlab.MergeMaster
{
	public class GameplayCore : SingletonMonoBehaviour<GameplayCore>
	{
		private const int LatticeLanes = 5;

		private const int PeakTroopWorth = 1024;

		private const string NoSpaceCaution = "NO SPACE LEFT!";

		private const string LensRestingStatus = "Idle";

		private const string LensShiftTrigger = "Move";

		// Camera.controller state whose end pose (0, 4.14, -3.06), pitch 53.2 deg, is the fight view of the original
		// (b_002/b_015: the board is smaller and lower than in the "Idle" pose (0, 3, -3.4), pitch 51 deg).
		// The "Idle --Move--> Camera" transition leads to "camera-original", a push-in that makes the board bigger,
		// which does not match the measured screenshots, so the fight view cross-fades into "Camera New" instead
		// (same 0.25 s as the authored Move transition). "Move" is only the fallback for a controller without it.
		private const string LensClashStatus = "Camera New";

		private const float LensShiftChangeoverSpan = 0.25f;

		private const string BiomeIntroStatus = "Environment_Camera_Intro";

		private const string BiomeRestingStatus = "New State";

		private const string OverlordIndicatorTitle = "LevelBarSkull";

		private const float DragHoist = 0.15f;

		private const float DragTrailPace = 25f;

		private const float SnapRange = 0.45f;

		private const float ArenaRimHalfBreadth = 1.525f;

		private const float ArenaRimElevation = 0.19f;

		private const float ArenaRimNearDepth = -2.25f;

		private const float ArenaRimScreenMargin = 0.03f;

		private const float ClashControlCrestLevel = 0.3375f;

		private const float RaycastReach = 50f;

		private const float ArenaSlotStagger = 0.03f;

		private const float BestowSpotStagger = 0.15f;

		private const float FuseReminderPause = 5f;

		private const float FuseIndicatorElevation = 0.006f;

		private const string FuseIndicatorPoolKey = "FuseIndicator";

		private const float SoaringSaurianRestingPause = 5f;

		private const float ClashWatchdogSpan = 60f;

		private static readonly int[] VolleyTallies = { 1, 1, 5, 2, 5, 1, 1, 4, 3, 5, 3 };

		private static readonly bool[] VolleyTwinned = { false, false, false, false, false, false, true, true, false, true, false };

		private const float VolleyTwinSpread = 0.07f;

		private const float ImpactTagSafetySpan = 2f;

		private const int ImpactTagCaptionCapacity = 256;

		private const float MeleeSparkSize = 1.5f;

		private struct VolleyRound
		{
			public Combatant marksman;

			public float due;
		}

		private const float SpotlightBlendSpan = 1f;

		private const float SpotlightCelebrateSpan = 1.5f;

		// Merge showcase measured on the original (tl/pe_merge, 30 fps): the merged unit celebrates ~2.8 s in the
		// close-up, then the camera goes back to the board in ~0.65 s (fast start), and the unit bar is hidden meanwhile.
		private const float FuseSpotlightCelebrateSpan = 2.8f;

		private const float FuseSpotlightBlendBackSpan = 0.65f;

		// ShowUnitCamera pose fitted to the original merge close-up (tl/pe_merge frame 165: unit ~43% of the screen height,
		// board rim ~60% down, camera in front of the unit looking back over the board): follow-target yaw -30 deg and
		// transposer offset (0.676, 0.357, 0.676) -> camera ~0.36 u high, ~0.96 u from the aim point, pitch ~1 deg.
		// The pitch comes from the sky: fitting the original's sky gradient row by row gives FOV 72 and pitch 0-1.5 deg
		// (mean error ~2/255 per channel); the older 16 deg pitch showed the lower, orange part of the dome.
		private const float SpotlightObjectiveYaw = -30f;

		private static readonly Vector3 SpotlightLensOffset = new Vector3(0.676f, 0.357f, 0.676f);

		private const float BiomeIntroHold = 5.6f;

		private const float FuseGuidePause = 0.6f;

		// L2 tutorial: the bought dino is placed in the front row next to the front dino (row 0, col 3).
		private const int GuideSaurianCellOrdinal = 3;

		// A9 (measured): cell for every newly added unit (buy, RV, summon, free) = first empty cell in this order
		// (cell = row * 5 + col, row 0 = front): back row centre-out, then middle row, then front row.
		private static readonly int[] SpotOrderOrdinals = { 12, 11, 13, 10, 14, 7, 6, 8, 5, 9, 2, 1, 3, 0, 4 };

		// "howtoplay lefttoright" hand: start position relative to the HowtoPlay/Horizontal root
		// (child at local (0,0,1.4), scale 0.6, first key (-0.108, 1.1, -1.4)).
		private static readonly Vector3 FuseFingerBeginOffset = new Vector3(-0.0648f, 0.66f, 0.56f);

		[Header("External")]
		public UIDirector uiDirector;

		public GameObject lensRig;

		public StageSessionDirector stageSessionDirector;

		public AudioDirector audioDirector;

		public GameObject soaringSaurian;

		public SphereCollider soaringSaurianCollider;

		public Animator lensMotion;

		public CinemachineBlendingLensDirector blendLens;

		public Animator biomeLensMotionRig;

		public Animator troopLensMotionRig;

		public CinemachineVirtualCameraBase troopLens;

		public GameObject freshTroopParticle;

		[Header("Internal")]
		public StageAdvancement[] stageAdvancement;

		public StageLayout[] exclusiveGuideStages;

		public ActorTemplate[] foeTemplates;

		public ActorTemplate[] guardianTemplates;

		public Transform[] foeLattice;

		public Transform[] guardianLattice;

		public Transform[] spotOrder;

		public Transform foeWrapper;

		public Transform guardianWrapper;

		public Transform projectileWrapper;

		public GameObject demiseLight;

		public GameObject confettiEffect;

		public GameObject lattice;

		public Animator lensMotionRig;

		public string[] saurianTitles;

		public string[] personTitles;

		public Transform biomeWrapper;

		public GameObject biome;

		[SerializeField]
		private eSoundName[] fuseCues;

		[SerializeField]
		private eSoundName maturationAudio;

		[SerializeField]
		private GameObject matchTouchBarrier;

		[Header("Prefabs")]
		public Combatant combatantTemplate;

		public Projectile projectileTemplate;

		public GameObject meleeSparkTemplate;

		public ImpactTag impactTagTemplate;

		[SerializeField]
		private GameObject[] demiseBurstTemplates;

		private int _demiseBurstTurn;

		[SerializeField]
		private GameObject[] biomes;

		public StageLayout activeStage;

		public CombatantRegistry registry;

		public ActivityStatus activity;

		private readonly float awayIncomeOpeningPresentPause = 1.5f;

		private readonly int awayIncomePeakRetry = 5;

		private readonly float awayIncomeRetryGap = 1f;

		private readonly List<Projectile> projectiles = new List<Projectile>();

		private readonly List<VolleyRound> volleyRounds = new List<VolleyRound>();

		private readonly Dictionary<int, Projectile> _pooledProjectiles = new Dictionary<int, Projectile>();

		private readonly Dictionary<int, ImpactTag> _pooledImpactTags = new Dictionary<int, ImpactTag>();

		private readonly Dictionary<int, string> _impactTagCaptions = new Dictionary<int, string>();

		private readonly Dictionary<ActorKind, Dictionary<int, ActorTemplate>> foeTemplateCatalog = new Dictionary<ActorKind, Dictionary<int, ActorTemplate>>();

		private Transform focusedLatticeSlot;

		private Plane terrainPlane;

		public StageOutcome latestStageOutcome;

		private int awayIncomeRetry;

		private readonly Dictionary<ActorKind, Dictionary<int, GameObject>> guardianTemplateCatalog = new Dictionary<ActorKind, Dictionary<int, GameObject>>();

		private Combatant chosenCombatant;

		private bool fuseIndicatorsLive;

		private bool tweeningLens;

		[SerializeField]
		private GameObject fuseIndicatorEffect;

		private GameObject[] fuseIndicators;

		private Combatant[] fuseObjectives;

		private Coroutine fuseReminderRoutine;

		// ---- runtime state (not serialized)

		private Camera _primaryLens;

		private bool _bypassRequested;

		private bool _spotlightRunning;

		private bool _fetchingStage;

		private bool _awayIncomeRunning;

		private bool _soaringSaurianPresentedThisStage;

		private int _biomeOrdinal = -1;

		private Vector3 _dragBeginLoc;

		private Tween _fuseTransition;

		private Coroutine _soaringSaurianRoutine;

		private AnimationState _soaringSaurianFlight;

		private Coroutine _clashBeginRoutine;

		private Coroutine _clashWatchdogRoutine;

		private Coroutine _arenaSlotRoutine;

		private Coroutine _spotlightRoutine;

		private Coroutine _fuseGuideRoutine;

		private Combatant _fuseGuideLeader;

		private Combatant _fuseGuidePartner;

		private List<Combatant> _spotlightConcealedCombatants;

		private readonly List<Coroutine> _spotRoutines = new List<Coroutine>();

		private readonly List<Combatant> _arenaSlotSchedule = new List<Combatant>();

		private readonly HashSet<int> _batchedFreshGrades = new HashSet<int>();

		private readonly List<RaycastResult> _uiRaycastOutcomes = new List<RaycastResult>();

		private PointerEventData _cursorSignalInfo;

		private EventSystem _cursorSignalFramework;

		private bool _fuseFingerCached;

		private bool _arenaLensStandardCached;

		private Vector3 _arenaLensStandardLoc;

		private Vector3 _fuseFingerAnchorStandardLoc;

		private Quaternion _fuseFingerLocalRotation;

		public GameObject[] Biomes => biomes;

		public bool emptyLatticeSlotExists => EmptyLatticeSlotTally() > 0;

		// ------------------------------------------------------------------ tutorial (FTUE) steps
		// Board tutorial as measured on the original (DISCREPANCIES B1-B4). GameplayCore is the only writer of
		// GameTuning.finishedGuideKind; the step is derived from it (+ level, + whether a fight was ever lost) and is what
		// UIDirector maps to button visibility:
		//   InitialClash  - fresh L1: START FIGHT only (no buy buttons, no hand, no dim). The fight is lost by design.
		//   ProcureSoldier  - L1 after the first lost fight: dim overlay + only the Human buy button (bright, pulsing, no hand
		//                 sprite on it); no START FIGHT, no Dino button.
		//   ShiftForward - warrior bought: 3D ShiftForward hand on the dino (GameplayCore drives it); no buttons at all until a dino
		//                 stands in the front row.
		//   ClashSolely   - rest of L1 after the move, and L2 after the tutorial merge: START FIGHT only (no buy buttons).
		//   ProcureSaurian     - L2: dim overlay + only the Dino buy button (bright, pulsing, no hand sprite); no START FIGHT.
		//   Merge       - L2 dino bought (placed at cell 3): 3D merge hand from the new dino to its twin (GameplayCore drives it);
		//                 no buttons at all. The merge shows the new-tier showcase + NEW DINOSAUR card, then ClashSolely.
		//   None        - tutorial over (L3+): both buy buttons (FREE RV when unaffordable) + START FIGHT.
		public enum GuidePhase
		{
			Unset = 0,
			InitialClash = 1,
			ProcureSoldier = 2,
			ShiftForward = 3,
			ClashSolely = 4,
			ProcureSaurian = 5,
			Fuse = 6
		}

		public GuidePhase ActiveGuidePhase => GetGuidePhase();

		public static GuidePhase GetGuidePhase()
		{
			int stage = GameTuning.stage;
			switch (GameTuning.finishedGuideKind)
			{
				case GuideKind.Unset:
					if (stage > 1)
					{
						// Only reachable with an inconsistent save; FetchStage normalizes it.
						return stage == 2 ? GuidePhase.ProcureSaurian : GuidePhase.Unset;
					}
					// Bỏ qua bước đánh thua đầu tiên: Vào Level 1 là có hướng dẫn mua và sắp xếp luôn
					return GuidePhase.ProcureSoldier;
				case GuideKind.PersonAcquisition:
					return stage <= 1 ? GuidePhase.ShiftForward : (stage == 2 ? GuidePhase.ProcureSaurian : GuidePhase.Unset);
				case GuideKind.ShiftSaurianForward:
					return stage <= 1 ? GuidePhase.ClashSolely : (stage == 2 ? GuidePhase.ProcureSaurian : GuidePhase.Unset);
				case GuideKind.SaurianAcquisition:
					return stage <= 2 ? GuidePhase.Fuse : GuidePhase.Unset;
				default:
					return stage <= 2 ? GuidePhase.ClashSolely : GuidePhase.Unset;
			}
		}

		// START FIGHT is available (visible) only in these steps.
		public static bool IsClashAllowedInGuide(GuidePhase phase)
		{
			return phase == GuidePhase.Unset || phase == GuidePhase.InitialClash || phase == GuidePhase.ClashSolely;
		}

		// Buy button of `type` is visible in this step (FREE/RV variants only in GuidePhase.None).
		public static bool IsAcquisitionAllowedInGuide(GuidePhase phase, ActorKind kind)
		{
			switch (phase)
			{
				case GuidePhase.Unset:
					return kind == ActorKind.Person || kind == ActorKind.Saurian;
				case GuidePhase.ProcureSoldier:
					return kind == ActorKind.Person;
				case GuidePhase.ProcureSaurian:
					return kind == ActorKind.Saurian;
				default:
					return false;
			}
		}

		// ------------------------------------------------------------------ lifecycle

		protected override void Awake()
		{
			base.Awake();
			if (Instance != this)
			{
				return;
			}
			GameTuning.Boot();
			PlayMechanics.Boot();
			Application.targetFrameRate = 60;
			Input.multiTouchEnabled = false;

			AssembleTemplateCatalogs();
			terrainPlane = new Plane(Vector3.up, Vector3.zero);
			fuseIndicators = new GameObject[2];
			fuseObjectives = new Combatant[2];

			if (registry == null)
			{
				registry = new CombatantRegistry();
			}
			registry.Boot();
			registry.onCombatantAppended += OnCombatantAppended;
			registry.onCombatantDiscarded += OnCombatantDiscarded;

#if !UNITY_LUNA
			Application.quitting += IsExiting;
#endif

			if (blendLens == null)
			{
				blendLens = CinemachineBlendingLensDirector.Instance ?? FindObjectOfType<CinemachineBlendingLensDirector>();
			}
			if (troopLens == null && blendLens != null)
			{
				troopLens = blendLens.GetLens(2);
			}
			if (troopLens != null)
			{
				if (troopLens.LookAt == null && blendLens != null)
				{
					troopLens.LookAt = blendLens.transform.Find("FollowTarget");
				}
				if (troopLens.Follow == null)
				{
					troopLens.Follow = troopLens.LookAt;
				}
			}
			// The scene ships with the ev1 instance (environments[0]) already under the container.
			_biomeOrdinal = biome != null ? 0 : -1;
			FlipLiveMatchTouchBarrier(true);
			FindConfettiEffect();
		}

		private void OnDestroy()
		{
			if (registry != null)
			{
				registry.onCombatantAppended -= OnCombatantAppended;
				registry.onCombatantDiscarded -= OnCombatantDiscarded;
			}
#if !UNITY_LUNA
			Application.quitting -= IsExiting;
#endif
			TerminateFuseTransition();
		}

		public void FlipLiveLensRig(bool live)
		{
			if (lensRig != null)
			{
				lensRig.SetActive(live);
			}
		}

		private void Reset()
		{
		}

		private void Start()
		{
			GameTuning.MatchStatus = MatchStatus.ClashArena;
			_primaryLens = ResolvePrimaryLens();
			ImposeLensFieldOfView();
			FetchStage();
			AttemptBeginAwayIncome();
			OnBooted();
		}

		private void Update()
		{
			if (volleyRounds.Count > 0)
			{
				AdvanceVolleys();
			}
			if (tweeningLens)
			{
				if (activity == ActivityStatus.Hauling)
				{
					AbortDrag();
				}
				if (GameTuning.MotionsAllowBypass && Input.GetMouseButtonDown(0))
				{
					_bypassRequested = true;
				}
				return;
			}
			if (activity == ActivityStatus.Hauling)
			{
				if (!IsArenaInteractive())
				{
					AbortDrag();
					return;
				}
				if (Input.GetMouseButtonUp(0) || !Input.GetMouseButton(0))
				{
					OnCursorRelease();
					return;
				}
				OnDragMove();
				return;
			}
			if (!IsArenaInteractive())
			{
				return;
			}
			if (Input.GetMouseButtonDown(0) && !IsCursorOverUIPart(Input.mousePosition))
			{
				OnCursorDown();
			}
		}

		private void OnDisable()
		{
			PersistAwayIncomeMoment();
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			if (!hasFocus)
			{
				PersistAwayIncomeMoment();
				PlayerPrefs.Save();
				return;
			}
			AttemptBeginAwayIncome();
		}

		private void VerifyRetention()
		{
		}

		private void OnCombatantDiscarded(Combatant entity)
		{
			RenewArenaUI();
		}

		private void OnCombatantAppended(Combatant entity)
		{
			RenewArenaUI();
		}

		private void RenewArenaUI()
		{
			if (_fetchingStage || activity != ActivityStatus.Resting || uiDirector == null)
			{
				return;
			}
			uiDirector.ReviseGamerDirector(false);
			uiDirector.ReviseAcquisitionControls();
		}

		// ------------------------------------------------------------------ merge reminder

		private void VerifyFuseReminder()
		{
			HaltRoutine(ref fuseReminderRoutine);
			SwitchOffFuseIndicators();
			if (activity == ActivityStatus.Resting && !tweeningLens && GameTuning.stage >= GameTuning.FuseReminderBeginStage && isActiveAndEnabled)
			{
				fuseReminderRoutine = StartCoroutine(HoldForFuseReminder(FuseReminderPause));
			}
		}

		private IEnumerator HoldForFuseReminder(float countdown)
		{
			yield return new WaitForSeconds(countdown);
			fuseReminderRoutine = null;
			if (activity != ActivityStatus.Resting || tweeningLens || fuseIndicatorEffect == null || !IsFusePossible())
			{
				yield break;
			}
			for (int idx = 0; idx < fuseObjectives.Length; idx++)
			{
				Combatant objective = fuseObjectives[idx];
				if (objective == null)
				{
					continue;
				}
				Transform pivot = objective.transform;
				fuseIndicators[idx] = PoolingManager.Instance.Spawn(FuseIndicatorPoolKey, fuseIndicatorEffect, pivot.position + Vector3.up * FuseIndicatorElevation, Quaternion.identity, pivot);
			}
			fuseIndicatorsLive = true;
		}

		private void SwitchOffFuseIndicators()
		{
			if (fuseIndicators != null)
			{
				for (int idx = 0; idx < fuseIndicators.Length; idx++)
				{
					GameObject beacon = fuseIndicators[idx];
					fuseIndicators[idx] = null;
					if (beacon == null)
					{
						continue;
					}
					PoolingManager.Instance.Despawn(beacon);
				}
			}
			fuseIndicatorsLive = false;
		}

		private bool IsFusePossible()
		{
			fuseObjectives[0] = null;
			fuseObjectives[1] = null;
			return SeekFusePair(out fuseObjectives[0], out fuseObjectives[1]);
		}

		private bool SeekFusePair(out Combatant initial, out Combatant latter)
		{
			initial = null;
			latter = null;
			if (guardianLattice == null)
			{
				return false;
			}
			int tally = guardianLattice.Length;
			for (int outer = 0; outer < tally; outer++)
			{
				Combatant alpha = GetGuardianAtCell(GetLineForLatticeSlotOrdinal(outer), GetLaneForLatticeSlotOrdinal(outer));
				if (alpha == null)
				{
					continue;
				}
				for (int inner = outer + 1; inner < tally; inner++)
				{
					Combatant beta = GetGuardianAtCell(GetLineForLatticeSlotOrdinal(inner), GetLaneForLatticeSlotOrdinal(inner));
					if (beta != null && registry.VerifyFuse(alpha, beta))
					{
						initial = alpha;
						latter = beta;
						return true;
					}
				}
			}
			return false;
		}

		private void VerifyVisitMoment()
		{
		}

		// ------------------------------------------------------------------ board / level

		private void ConstructArena()
		{
			int[] lineup = PlayMechanics.lineup;
			if (lineup == null || guardianLattice == null)
			{
				return;
			}
			int tally = Mathf.Min(lineup.Length, guardianLattice.Length);
			for (int idx = 0; idx < tally; idx++)
			{
				int packed = lineup[idx];
				if (packed == 0)
				{
					continue;
				}
				ActorKind kind = (ActorKind)(packed / 10000);
				int worth = packed % 10000;
				bool isValid = worth > 0 && guardianLattice[idx] != null &&
					(kind == ActorKind.Person || kind == ActorKind.Saurian) && worth <= PeakTroopWorth;
				if (!isValid)
				{
					PlayMechanics.DiscardFromLineup(idx);
					continue;
				}
				ConstructCombatantAndAppendToRegistry(ActorFaction.Guardian, kind, worth, GetLineForLatticeSlotOrdinal(idx), GetLaneForLatticeSlotOrdinal(idx), guardianLattice[idx].position);
			}
		}

		private void ReviseBiome()
		{
			GameObject[] biomeSet = Biomes;
			if (biomeSet == null || biomeSet.Length == 0 || biomeWrapper == null)
			{
				return;
			}
			int ordinal = GameTuning.areBiomesAllowed ? GameTuning.GetBiomeOrdinal(GameTuning.stage) : 0;
			ordinal = Mathf.Clamp(ordinal, 0, biomeSet.Length - 1);
			if (biome != null && ordinal == _biomeOrdinal)
			{
				return;
			}
			GameObject template = biomeSet[ordinal];
			if (template == null)
			{
				return;
			}
			if (biome != null)
			{
				biome.SetActive(false);
				Destroy(biome);
			}
			biome = Instantiate(template, biomeWrapper);
			biome.transform.localPosition = Vector3.zero;
			biome.transform.localRotation = Quaternion.identity;
			_biomeOrdinal = ordinal;
		}

		public void FlipLiveMatchTouchBarrier(bool live)
		{
			if (matchTouchBarrier != null)
			{
				matchTouchBarrier.SetActive(live);
			}
		}

		public void FetchStage()
		{
			_fetchingStage = true;
			try
			{
				ClearArena();

				activity = ActivityStatus.Resting;
				latestStageOutcome = StageOutcome.Unset;
				GameTuning.arenaInteractionAllowed = true;
				if (lattice != null)
				{
					lattice.SetActive(true);
				}
				if (lensMotion != null && lensMotion.isActiveAndEnabled)
				{
					lensMotion.ResetTrigger(LensShiftTrigger);
					lensMotion.Play(LensRestingStatus, 0, 0f);
					lensMotion.Update(0f);
				}
				if (blendLens != null)
				{
					blendLens.AssignLens(0, 0f);
				}

				GameTuning.RevertOncePerStageSignals();
				PlayMechanics.stageProceeds = 0L;
				PlayMechanics.terminatedFoesThisStage = 0;
				_soaringSaurianPresentedThisStage = false;
				PlayMechanics.ResetGoldForStage(GameTuning.stage);

				ProduceStageFoes();
				ConstructArena();
				SyncUnsealedGradesWithArena();
				NormalizeGuideStatus();
				BeginArenaSlotMotion();

				ReviseBiome();
			}
			finally
			{
				_fetchingStage = false;
			}

			if (uiDirector != null)
			{
				uiDirector.Reset();
			}
			ImposeGuideFingers();
			VerifySoaringSaurian();
			VerifyFuseReminder();
			FlipLiveMatchTouchBarrier(false);
			AttemptPresentFuseGuideHint();
			TraceStageAdvancementInfo();
		}

		private void ClearArena()
		{
			HaltRoutine(ref fuseReminderRoutine);
			HaltRoutine(ref _soaringSaurianRoutine);
			HaltRoutine(ref _clashBeginRoutine);
			HaltRoutine(ref _clashWatchdogRoutine);
			HaltRoutine(ref _arenaSlotRoutine);
			HaltRoutine(ref _fuseGuideRoutine);
			foreach (Coroutine job in _spotRoutines)
			{
				if (job != null)
				{
					StopCoroutine(job);
				}
			}
			_spotRoutines.Clear();
			_batchedFreshGrades.Clear();
			_arenaSlotSchedule.Clear();
			HaltRoutine(ref _spotlightRoutine);
			if (_spotlightRunning)
			{
				AbortSpotlight();
			}

			TerminateFuseTransition();
			TerminateCombatantTransitions(registry.combatants);
			TerminateCombatantTransitions(registry.scrap);
			DiscardProjectiles();
			SwitchOffFuseIndicators();
			if (focusedLatticeSlot != null)
			{
				LatticeSlotFocusStatus(focusedLatticeSlot, false);
				focusedLatticeSlot = null;
			}
			registry.Wipe();
			if (confettiEffect != null)
			{
				confettiEffect.SetActive(false);
			}
			ConcealSoaringSaurian();
			chosenCombatant = null;
			ConcealFuseFingers();
		}

		private void ProduceStageFoes()
		{
			StageLayout[] stages = ActiveStages();
			if (stages == null || stages.Length == 0)
			{
				activeStage = null;
				return;
			}
			GameTuning.VerifyRepeatStageOrdinal(stages.Length);
			activeStage = stages[Mathf.Clamp(GameTuning.stageOrdinal, 0, stages.Length - 1)];
			if (activeStage == null)
			{
				return;
			}
			LatticePiece[] pieces = activeStage.pieces;
			StageLayout liveStageInstance = null;
			if (pieces == null || pieces.Length == 0)
			{
				liveStageInstance = Instantiate(activeStage);
				pieces = liveStageInstance.pieces;
				if (pieces == null || pieces.Length == 0)
				{
					pieces = liveStageInstance.GetComponentsInChildren<LatticePiece>(true);
				}
			}

			// Past the last level the original replays the LAST level layout (Level-00300) unchanged for L301, L302 …
			// (DISCREPANCIES A13: no random repeat, no enemy scaling); GameTuning.stageOrdinal already points at it.
			ProduceFoes(PlayMechanics.ConstructStage(pieces, 0f));
			AdjustFoeVitalityForFinanceSpikesReduction();

			if (liveStageInstance != null)
			{
				Destroy(liveStageInstance.gameObject);
			}
		}

		private void ProduceFoes(Dictionary<ActorKind, LatticePieceWorth[]> byKind)
		{
			if (byKind == null || foeLattice == null)
			{
				return;
			}
			foreach (KeyValuePair<ActorKind, LatticePieceWorth[]> couple in byKind)
			{
				if (couple.Value == null)
				{
					continue;
				}
				foreach (LatticePieceWorth piece in couple.Value)
				{
					if (piece.ordinal < 0 || piece.ordinal >= foeLattice.Length || foeLattice[piece.ordinal] == null || piece.worth <= 0)
					{
						continue;
					}
					ActorKind kind = piece.kind == ActorKind.Overlord ? ActorKind.Saurian : piece.kind;
					if (kind != ActorKind.Person && kind != ActorKind.Saurian)
					{
						continue;
					}
					// Enemies use the same HP / ATK formulas as the player's units (A7); no tutorial damage reduction.
					AttemptGetFoeTemplate(kind, piece.worth, out ActorTemplate info);
					ConstructCombatantAndAppendToRegistry(ActorFaction.Foe, kind, piece.worth, GetLineForLatticeSlotOrdinal(piece.ordinal), GetLaneForLatticeSlotOrdinal(piece.ordinal), foeLattice[piece.ordinal].position, info.vitality, info.tailoredHarmRate);
				}
			}
		}

		private void AdjustFoeVitalityForFinanceSpikesReduction()
		{
			if (!GameTuning.FoeMightProfileAllowed)
			{
				return;
			}
			int stage = GameTuning.stage;
			if (stage < GameTuning.FoeMightProfileBeginStage || stage > GameTuning.FoeMightProfileFinalStage)
			{
				return;
			}
			float gamerWorth = 0f;
			foreach (int packed in PlayMechanics.lineup)
			{
				if (packed > 0)
				{
					gamerWorth += packed % 10000;
				}
			}
			float foeWorth = 0f;
			foreach (Combatant foe in registry.foes)
			{
				if (foe != null)
				{
					foeWorth += foe.worth;
				}
			}
			if (gamerWorth <= 0f || foeWorth <= 0f)
			{
				return;
			}
			float percent = Mathf.Min(150f, (float)(GameTuning.FoeMightProfileBasis + GameTuning.FoeMightProfileRate * (stage - GameTuning.FoeMightProfileBeginStage)));
			float objective = gamerWorth * percent / 100f;
			registry.AdjustVitalityByScalar(ActorFaction.Foe, objective / foeWorth);
		}

		public void VerifySoaringSaurian()
		{
			HaltRoutine(ref _soaringSaurianRoutine);
			if (_soaringSaurianPresentedThisStage || GameTuning.stage < GameTuning.SoaringSaurianBeginStage || soaringSaurian == null || !isActiveAndEnabled)
			{
				return;
			}
			_soaringSaurianRoutine = StartCoroutine(SoaringSaurianRoutine());
		}

		private IEnumerator SoaringSaurianRoutine()
		{
			float restingMoment = 0f;
			while (restingMoment < SoaringSaurianRestingPause)
			{
				if (activity == ActivityStatus.Sprinting || activity == ActivityStatus.Assaulting || activity == ActivityStatus.Finished)
				{
					_soaringSaurianRoutine = null;
					yield break;
				}
				restingMoment = IsArenaRestingForSoaringSaurian() ? restingMoment + Time.deltaTime : 0f;
				yield return null;
			}
			_soaringSaurianRoutine = null;
			_soaringSaurianPresentedThisStage = true;
			soaringSaurian.SetActive(false);
			soaringSaurian.SetActive(true);
			if (soaringSaurianCollider != null)
			{
				soaringSaurianCollider.enabled = true;
			}
		}

		private bool IsArenaRestingForSoaringSaurian()
		{
			if (activity != ActivityStatus.Resting || tweeningLens || !GameTuning.arenaInteractionAllowed || uiDirector == null)
			{
				return false;
			}
			if (uiDirector.liveDialog != null)
			{
				return false;
			}
			GameObject director = uiDirector.gamerDirector;
			return director == null || director.activeInHierarchy;
		}

		public void ConcealSoaringSaurian()
		{
			HaltRoutine(ref _soaringSaurianRoutine);
			if (_soaringSaurianFlight != null)
			{
				_soaringSaurianFlight.speed = 1f;
			}
			if (soaringSaurianCollider != null)
			{
				soaringSaurianCollider.enabled = false;
			}
			if (soaringSaurian != null)
			{
				soaringSaurian.SetActive(false);
			}
		}

		public IEnumerator PresentBiome(int biomeOrdinal, Action onMotionFinishHook = null)
		{
			tweeningLens = true;
			_bypassRequested = false;
			FlipLiveMatchTouchBarrier(true);
			SwitchOffFuseIndicators();
			ConcealSoaringSaurian();
			if (biomeLensMotionRig != null)
			{
				biomeLensMotionRig.Play(BiomeIntroStatus, 0, 0f);
				// Apply the first intro frame now so the cut to ShowEnvCamera never shows its previous pose.
				biomeLensMotionRig.Update(0f);
			}
			if (blendLens != null)
			{
				blendLens.AssignLens(1, 0f);
			}
			float holdMoment = BiomeIntroHold;
			while (holdMoment > 0f && !_bypassRequested)
			{
				holdMoment -= Time.deltaTime;
				yield return null;
			}
			if (blendLens != null)
			{
				blendLens.AssignLens(0, SpotlightBlendSpan, Ease.InOutSine);
			}
			yield return new WaitForSeconds(SpotlightBlendSpan);
			if (biomeLensMotionRig != null)
			{
				biomeLensMotionRig.Play(BiomeRestingStatus, 0, 0f);
			}
			tweeningLens = false;
			_bypassRequested = false;
			FlipLiveMatchTouchBarrier(false);
			onMotionFinishHook?.Invoke();
			if (activity == ActivityStatus.Resting)
			{
				VerifyFuseReminder();
				VerifySoaringSaurian();
			}
		}

		// ------------------------------------------------------------------ fighters

		public Combatant ConstructCombatantAndAppendToRegistry(ActorFaction faction, ActorKind kind, int worth, int line, int lane, Vector3 loc, float vitality = 0f, int tailoredHarmRate = 0)
		{
			Combatant combatant = ConstructCombatant(faction, kind, worth, line, lane, loc, vitality, tailoredHarmRate);
			if (combatant != null)
			{
				registry.Add(combatant);
			}
			return combatant;
		}

		private Combatant ConstructCombatant(ActorFaction faction, ActorKind kind, int worth, int line, int lane, Vector3 loc, float vitality = 0f, int tailoredHarmRate = 0)
		{
			Combatant emitter = combatantTemplate;
			if (emitter == null)
			{
				return null;
			}
			bool isFoe = faction == ActorFaction.Foe;
			Combatant combatant = Instantiate(emitter, loc, isFoe ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity, isFoe ? foeWrapper : guardianWrapper);
			combatant.gameplayCore = this;
			combatant.faction = faction;
			combatant.kind = kind;
			combatant.line = line;
			combatant.lane = lane;
			combatant.name = faction.ToLabel() + "-" + kind.ToLabel() + "-" + worth;
			combatant.worth = worth;
			combatant.ReviseTailoredHarmRate(tailoredHarmRate);
			combatant.ReviseVitality(vitality > 0f ? vitality : combatant.GetFullVitality());
			return combatant;
		}

		private bool IsCursorOverUIPart(Vector2 location)
		{
			EventSystem signalFramework = EventSystem.current;
			if (signalFramework == null)
			{
				return false;
			}
			if (_cursorSignalInfo == null || _cursorSignalFramework != signalFramework)
			{
				_cursorSignalFramework = signalFramework;
				_cursorSignalInfo = new PointerEventData(signalFramework);
			}
			_cursorSignalInfo.position = location;
			_uiRaycastOutcomes.Clear();
			signalFramework.RaycastAll(_cursorSignalInfo, _uiRaycastOutcomes);
			bool hovering = _uiRaycastOutcomes.Count > 0;
			_uiRaycastOutcomes.Clear();
			return hovering;
		}

		public void SlotCombatant(ActorKind kind, bool forGratis)
		{
			SlotCombatant(kind, forGratis, 1, false);
		}

		public void SlotCombatant(ActorKind kind, bool forGratis, int actorWorth, bool presentMotion = false)
		{
			if (!emptyLatticeSlotExists)
			{
				if (uiDirector != null)
				{
					uiDirector.PresentCaution(NoSpaceCaution);
				}
				return;
			}
			if (actorWorth <= 0)
			{
				actorWorth = 1;
			}
			// The tutorial dino bought on L2 goes to the front row next to the front dino (cell 3), not to the back row.
			bool isGuideSaurian = !forGratis && kind == ActorKind.Saurian && GetGuidePhase() == GuidePhase.ProcureSaurian;
			if (!forGratis)
			{
				if (!PlayMechanics.CanAffordAcquisition(kind))
				{
					return;
				}
				PlayMechanics.DidAcquireActor(kind);
			}
			Transform slot = isGuideSaurian ? SeekGuideSaurianSpot() : null;
			if (slot == null)
			{
				slot = SeekInitialEmptySpot();
			}
			if (slot == null)
			{
				return;
			}
			SlotCombatantAt(kind, actorWorth, GetLatticeSlotOrdinal(slot), presentMotion);
		}

		// showAnimation (summon / offers): the unit always gets the showcase close-up; the NEW card only for a new tier.
		// Otherwise (buy, RV, awards) a new tier is just unlocked (no showcase, no card).
		private Combatant SlotCombatantAt(ActorKind kind, int actorWorth, int ordinal, bool presentMotion)
		{
			Transform slot = guardianLattice[ordinal];
			Combatant combatant = ConstructCombatantAndAppendToRegistry(ActorFaction.Guardian, kind, actorWorth, GetLineForLatticeSlotOrdinal(ordinal), GetLaneForLatticeSlotOrdinal(ordinal), slot.position);
			if (combatant == null)
			{
				return null;
			}
			combatant.ArenaMotion(ActorArenaActivity.Slot);
			PerformAudioCue(kind, AudioCue.Slot, actorWorth);
			Toolbox.PerformTactileImpact(ImpactFeedbackStyle.Light);
			PlayMechanics.ReviseLineup(ordinal, kind, actorWorth);

			bool isBattleTroop = kind == ActorKind.Person || kind == ActorKind.Saurian;
			if (presentMotion && isBattleTroop && !_spotlightRunning && !tweeningLens && isActiveAndEnabled && IsArenaPeriod)
			{
				_spotlightRoutine = StartCoroutine(PresentTroop(combatant));
			}
			else if (IsFreshGrade(kind, actorWorth) && !_batchedFreshGrades.Contains(EncodingOf(kind, actorWorth)))
			{
				UnsealGrade(kind, actorWorth, false);
			}

			if (activity == ActivityStatus.Assaulting)
			{
				// A delayed reward landed after the fight started: let the unit join in.
				combatant.Clash(registry.GetNearestOpposition(combatant));
				return combatant;
			}

			GuideKind guide = GameTuning.finishedGuideKind;
			if (guide == GuideKind.Unset && kind == ActorKind.Person)
			{
				// Buy-warrior step done -> ShiftForward step (skipped when a dino already stands in the front row).
				GameTuning.DidFinishGuide(GuideKind.PersonAcquisition);
				if (AnyGuardianOfKindInLine(ActorKind.Saurian, 0))
				{
					FinishShiftSaurianForwardGuide();
				}
				else if (uiDirector != null)
				{
					uiDirector.FlipShiftForwardGuide(ShouldPresentShiftForwardFinger());
				}
			}
			else if (guide == GuideKind.ShiftSaurianForward && kind == ActorKind.Saurian)
			{
				// Buy-dino step done -> Merge step.
				GameTuning.DidFinishGuide(GuideKind.SaurianAcquisition);
			}
			if (GameTuning.finishedGuideKind == GuideKind.SaurianAcquisition && !_spotlightRunning)
			{
				Combatant counterpart = SeekFuseCounterpart(combatant);
				if (counterpart != null)
				{
					BeginFuseGuide(counterpart, combatant);
				}
			}

			if (uiDirector != null)
			{
				uiDirector.ReviseAcquisitionControls();
				uiDirector.ReviseGamerDirector();
			}
			VerifyFuseReminder();
			return combatant;
		}

		private IEnumerator PresentFuseGuide(int line, int lane, Vector3 slotLoc)
		{
			yield return new WaitForSeconds(FuseGuidePause);
			_fuseGuideRoutine = null;
			if (activity != ActivityStatus.Resting || tweeningLens || GameTuning.finishedGuideKind != GuideKind.SaurianAcquisition)
			{
				yield break;
			}
			Combatant troop = GetGuardianAtCell(line, lane);
			GameObject anchor = GetSceneHowToPlay(0);
			if (troop == null || anchor == null)
			{
				yield break;
			}
			Transform anchorPivot = anchor.transform;
			Transform tweened = anchorPivot.childCount > 0 ? anchorPivot.GetChild(0) : null;
			Transform finger = tweened != null ? tweened.Find("hand") : null;
			if (!_fuseFingerCached)
			{
				_fuseFingerCached = true;
				_fuseFingerAnchorStandardLoc = anchorPivot.position;
				_fuseFingerLocalRotation = finger != null ? finger.localRotation : Quaternion.identity;
			}
			Vector3 begin = troop.transform.position;
			Vector3 bearing = slotLoc - begin;
			bearing.y = 0f;
			if (bearing.sqrMagnitude < 0.0001f)
			{
				bearing = Vector3.right;
			}
			// The authored hand drags along +x; turn the root toward the twin but keep the hand sprite facing the camera.
			Quaternion swivel = Quaternion.Euler(0f, -Mathf.Atan2(bearing.z, bearing.x) * Mathf.Rad2Deg, 0f);
			Vector3 displacement = swivel * FuseFingerBeginOffset;
			float fingerElevation = _fuseFingerAnchorStandardLoc.y + FuseFingerBeginOffset.y;
			Vector3 fingerBegin = ProjectTerrainMarkToElevation(new Vector3(begin.x, 0f, begin.z), fingerElevation);
			anchorPivot.SetPositionAndRotation(fingerBegin - displacement, swivel);
			if (finger != null)
			{
				finger.localRotation = Quaternion.Inverse(swivel) * _fuseFingerLocalRotation;
			}
			anchor.SetActive(false);
			anchor.SetActive(true);
		}

		// Point at `height` that the board camera sees at the same screen position as `groundPoint`.
		private Vector3 ProjectTerrainMarkToElevation(Vector3 terrainMark, float elevation)
		{
			Camera lens = _primaryLens != null ? _primaryLens : ResolvePrimaryLens();
			if (lens == null)
			{
				return new Vector3(terrainMark.x, elevation, terrainMark.z);
			}
			Vector3 viewpoint = lens.transform.position;
			float divisor = viewpoint.y - terrainMark.y;
			if (Mathf.Abs(divisor) < 0.001f)
			{
				return new Vector3(terrainMark.x, elevation, terrainMark.z);
			}
			float scalar = (viewpoint.y - elevation) / divisor;
			return viewpoint + (terrainMark - viewpoint) * scalar;
		}

		public IEnumerator SlotCombatant(ActorKind kind, float pause, bool isLatest, int actorWorth)
		{
			if (pause > 0f)
			{
				yield return new WaitForSeconds(pause);
			}
			int packed = EncodingOf(kind, actorWorth);
			if (isLatest)
			{
				_batchedFreshGrades.Remove(packed);
			}
			// Awarded units (RV "FREE +2", HOT +4, free character...) only get the showcase when they bring a new tier.
			SlotCombatant(kind, true, actorWorth, isLatest && IsFreshGrade(kind, actorWorth));
			if (isLatest && IsFreshGrade(kind, actorWorth) && !_spotlightRunning && AnyGuardian(kind, actorWorth))
			{
				UnsealGrade(kind, actorWorth, false);
			}
		}

		public void BestowCombatants(ActorKind kind, int amount, int worth = 1)
		{
			if (worth <= 0)
			{
				worth = 1;
			}
			int tally = Mathf.Min(amount, EmptyLatticeSlotTally());
			if (tally <= 0)
			{
				if (amount > 0 && uiDirector != null)
				{
					uiDirector.PresentCaution(NoSpaceCaution);
				}
				return;
			}
			if (tally > 1 && IsFreshGrade(kind, worth))
			{
				_batchedFreshGrades.Add(EncodingOf(kind, worth));
			}
			for (int idx = 0; idx < tally; idx++)
			{
				_spotRoutines.Add(StartCoroutine(SlotCombatant(kind, idx * BestowSpotStagger, idx == tally - 1, worth)));
			}
		}

		public void RelocateCombatant(Combatant combatant, int objectiveLine, int objectiveLane)
		{
			if (combatant == null)
			{
				return;
			}
			int formerOrdinal = GetLatticeSlotOrdinal(combatant.line, combatant.lane);
			int freshOrdinal = GetLatticeSlotOrdinal(objectiveLine, objectiveLane);
			combatant.line = objectiveLine;
			combatant.lane = objectiveLane;
			Transform slot = GetLatticeSlot(objectiveLine, objectiveLane);
			combatant.transform.DOKill();
			combatant.transform.rotation = combatant.faction == ActorFaction.Foe ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
			if (slot != null)
			{
				combatant.transform.DOMove(slot.position, GameTuning.combatantRunSpan).SetEase(Ease.OutQuad);
			}
			if (combatant.faction == ActorFaction.Guardian)
			{
				SyncLineup(formerOrdinal);
				SyncLineup(freshOrdinal);
				GameTuning.didEverShiftActor = true;
				if (GameTuning.finishedGuideKind == GuideKind.PersonAcquisition && combatant.kind == ActorKind.Saurian && objectiveLine == 0)
				{
					// ShiftForward step done -> START FIGHT appears (ClashSolely for the rest of L1).
					FinishShiftSaurianForwardGuide();
				}
			}
			Toolbox.PerformTactileImpact(ImpactFeedbackStyle.Light);
		}

		private void SyncLineup(int ordinal)
		{
			if (guardianLattice == null || ordinal < 0 || ordinal >= guardianLattice.Length)
			{
				return;
			}
			Combatant combatant = GetGuardianAtCell(GetLineForLatticeSlotOrdinal(ordinal), GetLaneForLatticeSlotOrdinal(ordinal));
			if (combatant != null)
			{
				PlayMechanics.ReviseLineup(ordinal, combatant.kind, combatant.worth);
			}
			else
			{
				PlayMechanics.DiscardFromLineup(ordinal);
			}
		}

		private void FinishShiftSaurianForwardGuide()
		{
			GameTuning.DidFinishGuide(GuideKind.ShiftSaurianForward);
			if (uiDirector != null)
			{
				uiDirector.FlipShiftForwardGuide(false);
				if (!_fetchingStage && activity == ActivityStatus.Resting)
				{
					// START FIGHT appears as soon as the dino stands in the front row.
					uiDirector.ReviseGamerDirector(false);
				}
			}
		}

		// ------------------------------------------------------------------ fight

		public void CombatantDidPerish(Combatant combatant)
		{
			if (combatant == null)
			{
				return;
			}
			if (combatant.faction == ActorFaction.Foe)
			{
				PlayMechanics.terminatedFoesThisStage++;
			}
			PresentDemiseBurst(combatant);
			ActorFaction victor = registry.ReportFatality(combatant);
			if (uiDirector != null)
			{
				uiDirector.ReviseCombatantVitalityMeter(combatant.faction);
			}
			if (victor != ActorFaction.Unset && (activity == ActivityStatus.Sprinting || activity == ActivityStatus.Assaulting))
			{
				DidFinishClash(victor);
			}
		}

		public void BeginClash()
		{
			if (activity != ActivityStatus.Resting || registry.guardians == null || registry.guardians.Count == 0 || tweeningLens)
			{
				return;
			}
			if (!IsClashAllowedInGuide(GetGuidePhase()))
			{
				// Buy-warrior / move-forward / buy-dino / merge tutorial steps: START FIGHT is not available.
				return;
			}
			SwitchOffFuseIndicators();
			HaltRoutine(ref fuseReminderRoutine);
			HaltRoutine(ref _fuseGuideRoutine);
			ConcealSoaringSaurian();
			ConcealFuseFingers();
			ConcludeArenaSlotMotion();
			if (uiDirector != null)
			{
				uiDirector.FlipShiftForwardGuide(false);
			}
			GameTuning.arenaInteractionAllowed = false;

			activity = ActivityStatus.Sprinting;
			FlipLiveMatchTouchBarrier(true);
			if (lattice != null)
			{
				lattice.SetActive(false);
			}

			float foeVitality = registry.GetLeftoverVitality(ActorFaction.Foe);
			PlayMechanics.foeBeginVitality = foeVitality;
			PlayMechanics.guardianBeginVitality = registry.GetLeftoverVitality(ActorFaction.Guardian);
			PlayMechanics.stageProceeds = 0L;
			PlayMechanics.terminatedFoesThisStage = 0;
			PlayMechanics.saurianIncomePerImpact = PlayMechanics.peakStageIncome / Mathf.Max(1f, foeVitality);

			if (uiDirector != null)
			{
				uiDirector.ConcealGamerInteractionControls();
				if (GameTuning.presentCombatantVitalityMeters)
				{
					uiDirector.PresentCombatantVitalityMeters();
				}
			}
			PerformClashLens();
			Toolbox.PerformTactileImpact(ImpactFeedbackStyle.Medium);
			_clashBeginRoutine = StartCoroutine(ClashBeginRoutine());
		}

		// START FIGHT: the board camera pulls back/up into the fight view and stays there until FetchStage
		// plays "Idle" again (victory/defeat are seen from the fight view, b_004/b_016).
		private void PerformClashLens()
		{
			if (blendLens != null && blendLens.activeLens != 0)
			{
				blendLens.AssignLens(0, 0f);
			}
			if (lensMotion == null || !lensMotion.isActiveAndEnabled || lensMotion.runtimeAnimatorController == null)
			{
				return;
			}
#if !UNITY_LUNA
			int clashStatus = Animator.StringToHash(LensClashStatus);
			if (lensMotion.HasState(0, clashStatus))
			{
				lensMotion.ResetTrigger(LensShiftTrigger);
				lensMotion.CrossFadeInFixedTime(clashStatus, LensShiftChangeoverSpan, 0, 0f);
			}
			else
			{
				lensMotion.SetTrigger(LensShiftTrigger);
			}
#else
			lensMotion.SetTrigger(LensShiftTrigger);
#endif
		}

		private IEnumerator ClashBeginRoutine()
		{
			yield return new WaitForSeconds(PlayMechanics.guardianAssaultBeginPause);
			_clashBeginRoutine = null;
			if (activity != ActivityStatus.Sprinting)
			{
				yield break;
			}
			activity = ActivityStatus.Assaulting;
			if (registry.foes.Count == 0 || registry.guardians.Count == 0)
			{
				DidFinishClash(registry.foes.Count == 0 ? ActorFaction.Guardian : ActorFaction.Foe);
				yield break;
			}
			PlayMechanics.clashBeginMoment = Time.time;
			ImposeMeasuredClashBeginRules();
			foreach (Combatant combatant in registry.combatants.ToArray())
			{
				if (combatant == null || !IsLiving(combatant))
				{
					continue;
				}
				combatant.Clash(registry.GetNearestOpposition(combatant));
			}
			_clashWatchdogRoutine = StartCoroutine(ClashWatchdog());
		}

		private const float LoneTroopInitialAssaultPause = 0.2f;

		// Equal-strength fight (see ImposeMeasuredClashBeginRules): hidden player health multiplier.
		private const float EqualClashVitalityCoefficientStage1 = 1.2f;

		private const float EqualClashVitalityCoefficient = 0.8f;

		// Two rules measured frame by frame on the original's team/unit bars:
		// - a player formation of ONE value-1 unit attacks for the first time 0.2 s later than usual (5/5 runs);
		// - a formation of 2+ units whose value sum EQUALS the enemy's fights with hidden health: x1.2 on level 1,
		//   x0.8 from level 2 on (7/7 runs: L1 dino+warrior 1.2; L3 Tri+raptor+2 Blackhawks 0.8 twice, 3 raptors+2 Blackhawks
		//   0.8; L3 sums 2, 3 and 7 vs 5 and every lone unit 1.0). Unit bars stay normalised to the base health and only
		//   refresh on a hit, so a 0.8 unit shows full until its first hit, which then drops the bar by that hit + 20%.
		private void ImposeMeasuredClashBeginRules()
		{
			var guardians = new List<Combatant>();
			foreach (Combatant combatant in registry.guardians)
			{
				if (combatant != null && IsLiving(combatant))
				{
					guardians.Add(combatant);
				}
			}
			// Lone player unit facing an enemy of the same or a higher value: first attack +0.2 s (measured: lone dino v1 and
			// lone warrior v1 vs L1, lone Triceramon v2 vs the L3 Triceramon; none for lone Triceramon / Hunter v2 vs L1).
			if (guardians.Count == 1)
			{
				int toughestFoe = 0;
				foreach (Combatant foe in registry.foes)
				{
					if (foe != null && IsLiving(foe))
					{
						toughestFoe = Mathf.Max(toughestFoe, foe.worth);
					}
				}
				if (toughestFoe >= guardians[0].worth)
				{
					guardians[0].PauseInitialAssault(LoneTroopInitialAssaultPause);
				}
			}
			int gamerWorth = 0;
			foreach (Combatant combatant in guardians)
			{
				gamerWorth += combatant.worth;
			}
			int foeWorth = 0;
			foreach (Combatant foe in registry.foes)
			{
				if (foe != null && IsLiving(foe))
				{
					foeWorth += foe.worth;
				}
			}
			if (guardians.Count >= 2 && gamerWorth == foeWorth)
			{
				float coefficient = GameTuning.stage == 1 ? EqualClashVitalityCoefficientStage1 : EqualClashVitalityCoefficient;
				foreach (Combatant combatant in guardians)
				{
					combatant.vitality *= coefficient;
				}
				// The team bar pool uses the real health (measured pools 33.6 = 1.2 x 28 on L1, 60.8 = 0.8 x 76 on L3).
				if (uiDirector != null)
				{
					uiDirector.PresentCombatantVitalityMeters();
				}
			}
		}

		private IEnumerator ClashWatchdog()
		{
			yield return new WaitForSeconds(ClashWatchdogSpan);
			_clashWatchdogRoutine = null;
			if (activity != ActivityStatus.Assaulting)
			{
				yield break;
			}
			float guardianProportion = PlayMechanics.guardianBeginVitality > 0f ? registry.GetLeftoverVitality(ActorFaction.Guardian) / PlayMechanics.guardianBeginVitality : 0f;
			float foeProportion = PlayMechanics.foeBeginVitality > 0f ? registry.GetLeftoverVitality(ActorFaction.Foe) / PlayMechanics.foeBeginVitality : 0f;
			HaltEveryCombatant();
			DidFinishClash(guardianProportion > foeProportion ? ActorFaction.Guardian : ActorFaction.Foe);
		}

		private void HaltEveryCombatant()
		{
			foreach (Combatant combatant in registry.combatants.ToArray())
			{
				if (combatant != null)
				{
					combatant.Halt();
				}
			}
		}

		public void FireProjectile(Combatant marksman)
		{
			if (marksman == null || projectileTemplate == null)
			{
				return;
			}
			Vector3 home = marksman.projectileBeginMark != null ? marksman.projectileBeginMark.position : marksman.transform.position + Vector3.up * 0.3f;
			Projectile projectile = SpawnProjectile(home);
			projectile.gameplayCore = this;
			projectile.firer = marksman;
			projectile.mimic = false;
			projectile.Arm();
			projectiles.Add(projectile);
			GameObject muzzle = Projectile.PickGradeVariant(projectileTemplate.muzzles, marksman);
			if (muzzle != null)
			{
				PoolingManager.Instance.SpawnParticle(muzzle, home, marksman.transform.rotation);
			}
			int grade = Projectile.GradeSlot(marksman);
			int tally = VolleyTallies[grade];
			bool volleyScheduled = tally > 1 && marksman.motionRig != null;
			float harmShare = 1f / ((volleyScheduled ? tally : 1) * (VolleyTwinned[grade] ? 2 : 1));
			projectile.harmShare = harmShare;
			if (VolleyTwinned[grade])
			{
				LaunchMimic(marksman, home + marksman.transform.right * VolleyTwinSpread, muzzle, harmShare);
			}
			if (volleyScheduled)
			{
				float cycle = marksman.motionRig.GetCurrentAnimatorStateInfo(0).length / Mathf.Max(marksman.motionRig.speed, 0.01f);
				float gap = cycle / tally;
				for (int round = 1; round < tally; round++)
				{
					volleyRounds.Add(new VolleyRound { marksman = marksman, due = Time.time + gap * round });
				}
			}
		}

		public void PresentMeleeSpark(Vector3 location, Quaternion rotation)
		{
			if (meleeSparkTemplate != null)
			{
				PoolingManager.Instance.SpawnParticle(meleeSparkTemplate, location, rotation, null, MeleeSparkSize);
			}
		}

		private void AdvanceVolleys()
		{
			for (int idx = volleyRounds.Count - 1; idx >= 0; idx--)
			{
				VolleyRound round = volleyRounds[idx];
				if (Time.time < round.due)
				{
					continue;
				}
				volleyRounds.RemoveAt(idx);
				Combatant marksman = round.marksman;
				if (marksman == null || !marksman.IsLiving || activity != ActivityStatus.Assaulting || !Combatant.IsViableObjective(marksman.objective))
				{
					continue;
				}
				Vector3 home = marksman.projectileBeginMark != null ? marksman.projectileBeginMark.position : marksman.transform.position + Vector3.up * 0.3f;
				GameObject muzzle = Projectile.PickGradeVariant(projectileTemplate.muzzles, marksman);
				int grade = Projectile.GradeSlot(marksman);
				bool twinned = VolleyTwinned[grade];
				float harmShare = 1f / (VolleyTallies[grade] * (twinned ? 2 : 1));
				LaunchMimic(marksman, home, muzzle, harmShare);
				if (twinned)
				{
					LaunchMimic(marksman, home + marksman.transform.right * VolleyTwinSpread, muzzle, harmShare);
				}
				GameTuning.audioSignal?.Invoke(marksman.kind, AudioCue.ReleaseBowstring, marksman.worth);
			}
		}

		private void LaunchMimic(Combatant marksman, Vector3 home, GameObject muzzle, float harmShare)
		{
			Projectile mimic = SpawnProjectile(home);
			mimic.gameplayCore = this;
			mimic.firer = marksman;
			mimic.mimic = true;
			mimic.harmShare = harmShare;
			mimic.Arm();
			projectiles.Add(mimic);
			if (muzzle != null)
			{
				PoolingManager.Instance.SpawnParticle(muzzle, home, marksman.transform.rotation);
			}
		}

		private void PresentDemiseBurst(Combatant fallen)
		{
			if (demiseBurstTemplates == null || demiseBurstTemplates.Length == 0)
			{
				return;
			}
			GameObject burst = demiseBurstTemplates[_demiseBurstTurn % demiseBurstTemplates.Length];
			_demiseBurstTurn++;
			if (burst != null)
			{
				PoolingManager.Instance.SpawnParticle(burst, fallen.transform.position, Quaternion.identity);
			}
			fallen.Shatter();
		}

		public void PresentImpactTag(int income, Vector3 loc)
		{
			if (impactTagTemplate == null)
			{
				return;
			}
			GameObject spawned = PoolingManager.Instance.Spawn(impactTagTemplate.gameObject, loc, Quaternion.identity, projectileWrapper);
			PoolingManager.Instance.Despawn(spawned, ImpactTagSafetySpan);
			int key = spawned.GetInstanceID();
			if (!_pooledImpactTags.TryGetValue(key, out ImpactTag impactTag))
			{
				impactTag = spawned.GetComponent<ImpactTag>();
				_pooledImpactTags.Add(key, impactTag);
			}
			if (impactTag != null && impactTag.tagText != null)
			{
				// The original's floating hit label reads "$3", "$124" ...
				impactTag.tagText.text = GetImpactTagCaption(income);
			}
		}

		private string GetImpactTagCaption(int income)
		{
			if (!_impactTagCaptions.TryGetValue(income, out string caption))
			{
				if (_impactTagCaptions.Count >= ImpactTagCaptionCapacity)
				{
					_impactTagCaptions.Clear();
				}
				caption = "$" + Toolbox.CompactIntegerToThousands(income);
				_impactTagCaptions.Add(income, caption);
			}
			return caption;
		}

		private Projectile SpawnProjectile(Vector3 home)
		{
			GameObject spawned = PoolingManager.Instance.Spawn(projectileTemplate.gameObject, home, Quaternion.identity, projectileWrapper);
			int key = spawned.GetInstanceID();
			if (!_pooledProjectiles.TryGetValue(key, out Projectile projectile))
			{
				projectile = spawned.GetComponent<Projectile>();
				_pooledProjectiles.Add(key, projectile);
			}
			return projectile;
		}

		public void DiscardProjectile(Projectile projectile)
		{
			projectiles.Remove(projectile);
			if (projectile != null)
			{
				ReleaseProjectile(projectile);
			}
		}

		public void DiscardProjectiles()
		{
			foreach (Projectile projectile in projectiles)
			{
				if (projectile != null)
				{
					ReleaseProjectile(projectile);
				}
			}
			projectiles.Clear();
			volleyRounds.Clear();
		}

		private static void ReleaseProjectile(Projectile projectile)
		{
			PoolingManager.Instance.Despawn(projectile.gameObject);
		}

		public void IncomeResponder(long income)
		{
			PlayMechanics.stageProceeds += income;
			if (uiDirector != null)
			{
				uiDirector.RenewCashTag();
			}
		}

		// ------------------------------------------------------------------ input

		private bool IsArenaPeriod => activity == ActivityStatus.Resting || activity == ActivityStatus.Hauling || activity == ActivityStatus.Fusing;

		private bool IsArenaInteractive()
		{
			return GameTuning.MatchStatus == MatchStatus.ClashArena && GameTuning.arenaInteractionAllowed && !tweeningLens &&
				(uiDirector == null || uiDirector.liveDialog == null) &&
				(activity == ActivityStatus.Resting || activity == ActivityStatus.Hauling);
		}

		private void OnCursorDown()
		{
			Camera lens = _primaryLens != null ? _primaryLens : ResolvePrimaryLens();
			if (lens == null)
			{
				return;
			}
			Ray beam = lens.ScreenPointToRay(Input.mousePosition);
			RaycastHit[] impacts = Physics.RaycastAll(beam, RaycastReach, ~0, QueryTriggerInteraction.Collide);
			Array.Sort(impacts, (lhs, rhs) => lhs.distance.CompareTo(rhs.distance));
			foreach (RaycastHit impact in impacts)
			{
				Collider impactCollider = impact.collider;
				if (impactCollider == null)
				{
					continue;
				}
				GameObject impactEntity = impactCollider.gameObject;
				if (matchTouchBarrier != null && impactEntity == matchTouchBarrier)
				{
					return;
				}
				Combatant combatant = impactCollider.GetComponentInParent<Combatant>();
				if (combatant == null)
				{
					continue;
				}
				if (combatant.faction == ActorFaction.Guardian && IsLiving(combatant) && MayHaulInGuide(combatant))
				{
					CommenceDrag(combatant);
				}
				return;
			}
		}

		private bool MayHaulInGuide(Combatant combatant)
		{
			switch (GetGuidePhase())
			{
				case GuidePhase.Unset:
					return true;
				case GuidePhase.ShiftForward:
					return combatant.kind == ActorKind.Saurian && combatant.line > 0;
				case GuidePhase.Fuse:
					return _fuseGuideLeader != null ? combatant == _fuseGuideLeader : SeekFuseCounterpart(combatant) != null;
				default:
					return false;
			}
		}

		private bool IsGuideDropAllowed(Combatant carried, Transform latticeSlot)
		{
			switch (GetGuidePhase())
			{
				case GuidePhase.Unset:
					return true;
				case GuidePhase.ShiftForward:
					return latticeSlot == GetLatticeSlot(0, carried.lane) && IsLatticeSlotEmpty(latticeSlot);
				case GuidePhase.Fuse:
					Combatant occupant = GetGuardianAtLatticeSlot(latticeSlot);
					return occupant != null && (_fuseGuidePartner != null ? occupant == _fuseGuidePartner : registry.VerifyFuse(carried, occupant));
				default:
					return false;
			}
		}

		private void OnSoaringSaurianTouched()
		{
			if (uiDirector == null)
			{
				return;
			}
			uiDirector.PresentDialog(eUIName.FlyingDinoPopup);
			Dialog liveDialog = uiDirector.liveDialog;
			if (liveDialog != null && liveDialog.uiName == eUIName.FlyingDinoPopup)
			{
				FlipSoaringSaurianFlight(false);
			}
		}

		public void ResumeSoaringSaurian()
		{
			FlipSoaringSaurianFlight(true);
		}

		private void FlipSoaringSaurianFlight(bool live)
		{
			if (_soaringSaurianFlight == null)
			{
				if (live || soaringSaurianCollider == null)
				{
					return;
				}
				Animation flight = soaringSaurianCollider.GetComponent<Animation>();
				if (flight == null || flight.clip == null)
				{
					return;
				}
				_soaringSaurianFlight = flight[flight.clip.name];
				if (_soaringSaurianFlight == null)
				{
					return;
				}
			}
			_soaringSaurianFlight.speed = live ? 1f : 0f;
		}

		private void CommenceDrag(Combatant combatant)
		{
			combatant.transform.DOKill();
			chosenCombatant = combatant;
			_dragBeginLoc = combatant.transform.position;
			combatant.chosen = true;
			activity = ActivityStatus.Hauling;
			PerformUISample(fuseCues, 0);
			Toolbox.PerformTactileImpact(ImpactFeedbackStyle.Light);
			SwitchOffFuseIndicators();
			HaltRoutine(ref fuseReminderRoutine);
			HaltRoutine(ref _fuseGuideRoutine);
			if (uiDirector != null)
			{
				uiDirector.FlipShiftForwardGuide(false);
			}
			ConcealFuseFingers();
		}

		private void OnDragMove()
		{
			Combatant combatant = chosenCombatant;
			Camera lens = _primaryLens != null ? _primaryLens : ResolvePrimaryLens();
			if (combatant == null || lens == null)
			{
				return;
			}
			Ray beam = lens.ScreenPointToRay(Input.mousePosition);
			if (!terrainPlane.Raycast(beam, out float reach))
			{
				return;
			}
			Vector3 mark = beam.GetPoint(reach);
			Vector3 objective = mark + Vector3.up * DragHoist;
			Transform pivot = combatant.transform;
			pivot.position = Vector3.Lerp(pivot.position, objective, Mathf.Clamp01(DragTrailPace * Time.deltaTime));

			Transform focal = SeekNearestLatticeSlot(mark);
			if (focal == focusedLatticeSlot)
			{
				return;
			}
			if (focusedLatticeSlot != null)
			{
				LatticeSlotFocusStatus(focusedLatticeSlot, false);
			}
			focusedLatticeSlot = focal;
			Combatant hoverObjective = null;
			if (focal != null)
			{
				LatticeSlotFocusStatus(focal, true);
				Combatant occupant = GetGuardianAtLatticeSlot(focal);
				if (occupant != null && occupant != combatant)
				{
					hoverObjective = occupant;
				}
			}
			combatant.SetFuseObjective(hoverObjective);
		}

		private Transform SeekNearestLatticeSlot(Vector3 mark)
		{
			if (guardianLattice == null)
			{
				return null;
			}
			Transform finest = null;
			float finestReach = SnapRange * SnapRange;
			foreach (Transform slot in guardianLattice)
			{
				if (slot == null)
				{
					continue;
				}
				Vector3 difference = slot.position - mark;
				difference.y = 0f;
				float squared = difference.sqrMagnitude;
				if (squared <= finestReach)
				{
					finestReach = squared;
					finest = slot;
				}
			}
			return finest;
		}

		private void AbortDrag()
		{
			if (focusedLatticeSlot != null)
			{
				LatticeSlotFocusStatus(focusedLatticeSlot, false);
				focusedLatticeSlot = null;
			}
			Combatant combatant = chosenCombatant;
			chosenCombatant = null;
			if (activity == ActivityStatus.Hauling)
			{
				activity = ActivityStatus.Resting;
			}
			if (combatant == null)
			{
				return;
			}
			combatant.chosen = false;
			ReturnToOwnSlot(combatant);
		}

		private void ReturnToOwnSlot(Combatant combatant)
		{
			Transform ownSlot = GetLatticeSlot(combatant.line, combatant.lane);
			combatant.transform.DOKill();
			combatant.transform.rotation = Quaternion.identity;
			Vector3 endpoint = ownSlot != null ? ownSlot.position : new Vector3(_dragBeginLoc.x, 0f, _dragBeginLoc.z);
			combatant.transform.DOMove(endpoint, GameTuning.combatantRunSpan).SetEase(Ease.OutQuad);
		}

		private void LatticeSlotFocusStatus(Transform latticeSlot, bool status)
		{
			if (latticeSlot != null && latticeSlot.childCount > 0)
			{
				latticeSlot.GetChild(0).gameObject.SetActive(status);
			}
		}

		public void OnCursorRelease()
		{
			if (activity != ActivityStatus.Hauling && chosenCombatant == null)
			{
				return;
			}
			Transform focusedSlot = focusedLatticeSlot;
			focusedLatticeSlot = null;
			if (focusedSlot != null)
			{
				LatticeSlotFocusStatus(focusedSlot, false);
			}
			Combatant chosen = chosenCombatant;
			chosenCombatant = null;
			if (activity == ActivityStatus.Hauling)
			{
				activity = ActivityStatus.Resting;
			}
			if (chosen == null)
			{
				return;
			}
			chosen.chosen = false;
			PerformUISample(fuseCues, 1);

			Transform ownSlot = GetLatticeSlot(chosen.line, chosen.lane);
			if (focusedSlot == null || focusedSlot == ownSlot || !IsGuideDropAllowed(chosen, focusedSlot))
			{
				ReturnToOwnSlot(chosen);
			}
			else if (IsLatticeSlotEmpty(focusedSlot))
			{
				RelocateCombatant(chosen, GetLineForLatticeSlot(focusedSlot), GetLaneForLatticeSlot(focusedSlot));
			}
			else
			{
				Combatant occupant = GetGuardianAtLatticeSlot(focusedSlot);
				if (occupant == null || occupant == chosen)
				{
					ReturnToOwnSlot(chosen);
				}
				else if (registry.VerifyFuse(chosen, occupant))
				{
					BeginFuse(chosen, occupant);
				}
				else
				{
					int line = GetLineForLatticeSlot(focusedSlot);
					int lane = GetLaneForLatticeSlot(focusedSlot);
					RelocateCombatant(occupant, chosen.line, chosen.lane);
					RelocateCombatant(chosen, line, lane);
				}
			}

			if (activity == ActivityStatus.Resting)
			{
				VerifyFuseReminder();
				if (GameTuning.finishedGuideKind == GuideKind.PersonAcquisition && uiDirector != null)
				{
					uiDirector.FlipShiftForwardGuide(ShouldPresentShiftForwardFinger());
				}
				AttemptPresentFuseGuideHint();
			}
			if (uiDirector != null)
			{
				uiDirector.ReviseGamerDirector();
			}
		}

		private void BeginFuse(Combatant carried, Combatant objective)
		{
			activity = ActivityStatus.Fusing;
			ConcealFuseFingers();
			if (IsFreshGrade(objective.kind, objective.worth * 2) && GameTuning.MotionsFreshTroop && isActiveAndEnabled)
			{
				_spotlightRoutine = StartCoroutine(PresentTroopFuse(objective, carried));
				return;
			}
			TerminateFuseTransition();
			_fuseTransition = ConstructFuseShiftTransition(carried, objective).OnComplete(() =>
			{
				_fuseTransition = null;
				if (objective != null && carried != null)
				{
					objective.FuseWith(carried);
				}
				else if (activity == ActivityStatus.Fusing)
				{
					activity = ActivityStatus.Resting;
				}
			});
		}

		private Sequence ConstructFuseShiftTransition(Combatant carried, Combatant objective)
		{
			carried.transform.DOKill();
			Sequence chain = DOTween.Sequence();
			chain.Append(carried.transform.DOMove(objective.transform.position, GameTuning.combatantFuseSpan));
			if (carried.core != null)
			{
				carried.core.DOKill();
				float sizeSpan = Mathf.Min(GameTuning.combatantFuseSizeSpan, GameTuning.combatantFuseSpan);
				chain.Insert(GameTuning.combatantFuseSpan - sizeSpan, carried.core.DOScale(Vector3.zero, sizeSpan));
			}
			// The Body Animator (CharacterEffect Controller, Idle state has no motion + write defaults) rewrites the body
			// scale every frame after Update; run the merge tween in LateUpdate so the shrink is actually visible.
			chain.SetUpdate(UpdateType.Late);
			chain.SetLink(carried.gameObject);
			return chain;
		}

		public void DidFinishFuse(Combatant actor)
		{
			if (actor == null)
			{
				return;
			}
			PlayMechanics.ReviseLineup(GetLatticeSlotOrdinal(actor.line, actor.lane), actor.kind, actor.worth);
			PlayMechanics.latestFusedActor = actor.kind;
			Toolbox.PerformTactileImpact(ImpactFeedbackStyle.Medium);
			if (IsFreshGrade(actor.kind, actor.worth))
			{
				UnsealGrade(actor.kind, actor.worth, true);
			}
			if (GameTuning.finishedGuideKind == GuideKind.SaurianAcquisition)
			{
				GameTuning.DidFinishGuide(GuideKind.FuseCombatants);
				TraceGuideCompletion();
			}
			ConcealFuseFingers();
			if (_spotlightRunning)
			{
				return;
			}
			if (activity == ActivityStatus.Fusing)
			{
				activity = ActivityStatus.Resting;
			}
			VerifyFuseReminder();
			if (uiDirector != null)
			{
				uiDirector.ReviseGamerDirector();
			}
		}

		// ------------------------------------------------------------------ fight end

		private void DidFinishClash(ActorFaction victorFaction)
		{
			if (activity == ActivityStatus.Finished)
			{
				return;
			}
			HaltRoutine(ref _clashBeginRoutine);
			HaltRoutine(ref _clashWatchdogRoutine);
			activity = ActivityStatus.Finished;
			DiscardProjectiles();
			registry.PresentConquest(victorFaction);
			latestStageOutcome = victorFaction == ActorFaction.Guardian ? StageOutcome.Accomplishment : StageOutcome.Unsuccessful;

			if (latestStageOutcome == StageOutcome.Accomplishment)
			{
				if (confettiEffect == null)
				{
					FindConfettiEffect();
				}
				if (confettiEffect != null)
				{
					// The confetti bursts from (0, 0.79, -0.34), inside the fight's GameTapBlocker box (3 x 1 x 5); its world
					// collisions (lifetime loss 0.5) killed 93 of the 99 pieces within 0.5 s. The board is locked after the
					// fight anyway, so the blocker goes: 98 pieces then fly 3-5 u (up to the camera) like the original's.
					FlipLiveMatchTouchBarrier(false);
					confettiEffect.SetActive(false);
					confettiEffect.SetActive(true);
					ParticleSystem[] systems = confettiEffect.GetComponentsInChildren<ParticleSystem>(true);
					for (int i = 0; i < systems.Length; i++)
					{
						systems[i].Play();
					}
				}
				Toolbox.PerformTactileNotice(NotificationFeedbackStyle.Success);
				// YOU EARNED = 40 * PL^4, x10 ("Level Completed Bonus x10") when this level advances the PL (skip = 0).
				// GameTuning.level / advancementStage are still those of the finished level here; DidFinishStage re-derives
				// the same value while advancing them.
				bool liftsAdvancement = activeStage != null && !activeStage.bypassStageAdvancement;
				PlayMechanics.stageProceeds = PlayMechanics.GetStageTriumphIncome(GameTuning.advancementStage, liftsAdvancement);
				if (uiDirector != null)
				{
					uiDirector.DidFinishStage();
				}
			}
			else
			{
				Toolbox.PerformTactileNotice(NotificationFeedbackStyle.Error);
				// Fail earning = round(peakStageIncome * TOTAL ENEMY DAMAGE fraction) (L1: 34% of 40 -> 14); the live
				// per-hit counter during the fight is cosmetic.
				PlayMechanics.stageProceeds = GetDefeatIncome();
				// The first lost fight also moves the L1 tutorial from InitialClash to ProcureSoldier (GetGuidePhase).
				PlayMechanics.universalDefeatTally++;
			}
			GameTuning.ReviseInterlude(latestStageOutcome);
			PersistAwayIncomeMoment();
			TraceStageOutcome();
			if (uiDirector != null)
			{
				uiDirector.StartCoroutine(uiDirector.ArrangeStageOutro(latestStageOutcome, PlayMechanics.stageProceeds));
			}
		}

		// ------------------------------------------------------------------ new-unit showcase

		private IEnumerator PresentTroopFuse(Combatant fuseObjective, Combatant pickedCombatant)
		{
			CommenceSpotlight();
			TerminateFuseTransition();
			_fuseTransition = ConstructFuseShiftTransition(pickedCombatant, fuseObjective);
			yield return new WaitForSeconds(GameTuning.combatantFuseSpan);
			_fuseTransition = null;
			if (fuseObjective == null || pickedCombatant == null)
			{
				yield return ConcludeSpotlight(ActorKind.Unset, 0);
				yield break;
			}
			List<Combatant> switchedOffCombatants = ConcealOtherCombatants(fuseObjective, pickedCombatant);
			fuseObjective.SetVitalityMeterConcealed(true);
			pickedCombatant.SetVitalityMeterConcealed(true);
			Transform trailObjective = FocusSpotlightObjective(fuseObjective);
			PerformSpotlightVisual(trailObjective, false);
			if (blendLens != null)
			{
				blendLens.AssignLens(2, SpotlightBlendSpan, Ease.InOutSine);
			}
			yield return new WaitForSeconds(SpotlightBlendSpan);
			if (fuseObjective != null && pickedCombatant != null)
			{
				fuseObjective.FuseWith(pickedCombatant, false);
			}
			// The original's unlock burst (rings, glow, light rays under FollowTarget) starts with the merge flash, when
			// the camera has arrived (tl/pe_merge 3.5-4.6 s); started with the blend it would be over before it is seen.
			PerformSpotlightVisual(trailObjective, true);
			if (fuseObjective != null)
			{
				fuseObjective.actorStatus = ActorStatus.Conquest;
			}
			float holdMoment = FuseSpotlightCelebrateSpan;
			while (holdMoment > 0f && !_bypassRequested)
			{
				holdMoment -= Time.deltaTime;
				yield return null;
			}
			if (blendLens != null)
			{
				blendLens.AssignLens(0, FuseSpotlightBlendBackSpan, Ease.OutSine);
			}
			yield return new WaitForSeconds(FuseSpotlightBlendBackSpan);
			if (fuseObjective != null)
			{
				fuseObjective.SetVitalityMeterConcealed(false);
			}
			RestoreSpotlightScene(trailObjective, switchedOffCombatants, fuseObjective);
			ActorKind kind = fuseObjective != null ? fuseObjective.kind : ActorKind.Unset;
			int worth = fuseObjective != null ? fuseObjective.worth : 0;
			yield return ConcludeSpotlight(kind, worth);
		}

		public IEnumerator PresentTroop(Combatant pickedCombatant)
		{
			if (pickedCombatant == null)
			{
				yield break;
			}
			ActorKind kind = pickedCombatant.kind;
			int worth = pickedCombatant.worth;
			// Summoned units always get the close-up; the NEW DINOSAUR / NEW WARRIOR card only for a tier not owned yet.
			bool bringsFreshGrade = IsFreshGrade(kind, worth);
			UnsealGradeInfo(kind, worth);
			if (!GameTuning.MotionsFreshTroop)
			{
				if (bringsFreshGrade)
				{
					if (uiDirector != null)
					{
						uiDirector.PresentActorTile(kind, worth);
					}
				}
				_spotlightRoutine = null;
				yield break;
			}
			CommenceSpotlight();
			List<Combatant> switchedOffCombatants = ConcealOtherCombatants(pickedCombatant, null);
			Transform trailObjective = FocusSpotlightObjective(pickedCombatant);
			if (blendLens != null)
			{
				blendLens.AssignLens(2, SpotlightBlendSpan, Ease.InOutSine);
			}
			yield return new WaitForSeconds(SpotlightBlendSpan);
			PerformUISample(maturationAudio);
			if (pickedCombatant != null)
			{
				pickedCombatant.actorStatus = ActorStatus.Conquest;
			}
			float holdMoment = SpotlightCelebrateSpan;
			while (holdMoment > 0f && !_bypassRequested)
			{
				holdMoment -= Time.deltaTime;
				yield return null;
			}
			if (blendLens != null)
			{
				blendLens.AssignLens(0, SpotlightBlendSpan, Ease.InOutSine);
			}
			yield return new WaitForSeconds(SpotlightBlendSpan);
			RestoreSpotlightScene(trailObjective, switchedOffCombatants, pickedCombatant);
			if (bringsFreshGrade)
			{
				yield return ConcludeSpotlight(kind, worth);
			}
			else
			{
				yield return ConcludeSpotlight(ActorKind.Unset, 0);
			}
		}

		private void CommenceSpotlight()
		{
			if (chosenCombatant != null)
			{
				AbortDrag();
			}
			_spotlightRunning = true;
			activity = ActivityStatus.Fusing;
			tweeningLens = true;
			_bypassRequested = false;
			FlipLiveMatchTouchBarrier(true);
			SwitchOffFuseIndicators();
			HaltRoutine(ref fuseReminderRoutine);
			HaltRoutine(ref _fuseGuideRoutine);
			ConcealFuseFingers();
			if (soaringSaurian != null && soaringSaurian.activeSelf)
			{
				ConcealSoaringSaurian();
			}
			if (uiDirector != null)
			{
				uiDirector.ConcealGamerInteractionControls();
				// The close-up is shown without the game-view HUD (b_024 merge showcase, c_019 summon showcase).
				uiDirector.FlipMatchFrameUILive(false);
			}
		}

		private List<Combatant> ConcealOtherCombatants(Combatant retainA, Combatant retainB)
		{
			List<Combatant> concealed = new List<Combatant>();
			foreach (Combatant combatant in registry.combatants)
			{
				if (combatant == null || combatant == retainA || combatant == retainB || !combatant.gameObject.activeSelf)
				{
					continue;
				}
				combatant.gameObject.SetActive(false);
				concealed.Add(combatant);
			}
			_spotlightConcealedCombatants = concealed;
			return concealed;
		}

		private Transform FocusSpotlightObjective(Combatant troop)
		{
			if (troopLens == null && blendLens != null)
			{
				troopLens = blendLens.GetLens(2);
			}
			Transform trailObjective = troopLens != null ? troopLens.LookAt : null;
			if (trailObjective == null && blendLens != null)
			{
				trailObjective = blendLens.transform.Find("FollowTarget");
				if (troopLens != null)
				{
					troopLens.LookAt = trailObjective;
				}
			}
			if (trailObjective != null && troop != null)
			{
				trailObjective.position = troop.transform.position;
				trailObjective.rotation = Quaternion.Euler(0f, SpotlightObjectiveYaw, 0f);
				var spotlightVirtualLens = troopLens as Cinemachine.CinemachineVirtualCamera;
				var transposerComp = spotlightVirtualLens != null ? spotlightVirtualLens.GetCinemachineComponent<Cinemachine.CinemachineTransposer>() : null;
				if (transposerComp != null)
				{
					transposerComp.m_FollowOffset = SpotlightLensOffset;
				}
				trailObjective.gameObject.SetActive(false);
				trailObjective.gameObject.SetActive(true);
				// ShowUnitCamera (Transposer offset (1,1,1), damping 1, Composer offset y 0.34) must start the 1 s blend
				// from its pose around the new target (b_024: low close-up on the unit), not damp over from the last one.
				if (troopLens != null)
				{
					troopLens.PreviousStateIsValid = false;
				}
				if (spotlightVirtualLens != null)
				{
					spotlightVirtualLens.UpdatePose();
				}
			}
			return trailObjective;
		}

		private static void PerformSpotlightVisual(Transform trailObjective, bool perform)
		{
			if (trailObjective == null)
			{
				return;
			}
			foreach (ParticleSystem particleEmitter in trailObjective.GetComponentsInChildren<ParticleSystem>(true))
			{
				if (perform)
				{
					particleEmitter.Clear();
					particleEmitter.Play(false);
				}
				else
				{
					particleEmitter.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
				}
			}
		}

		private void RestoreSpotlightScene(Transform trailObjective, List<Combatant> switchedOffCombatants, Combatant troop)
		{
			if (trailObjective != null)
			{
				trailObjective.gameObject.SetActive(false);
			}
			if (switchedOffCombatants != null)
			{
				foreach (Combatant combatant in switchedOffCombatants)
				{
					if (combatant != null)
					{
						combatant.gameObject.SetActive(true);
					}
				}
			}
			_spotlightConcealedCombatants = null;
			if (troop != null && IsLiving(troop))
			{
				troop.actorStatus = ActorStatus.Resting;
			}
		}

		private IEnumerator ConcludeSpotlight(ActorKind kind, int worth)
		{
			tweeningLens = false;
			_bypassRequested = false;
			_spotlightRunning = false;
			_spotlightRoutine = null;
			activity = ActivityStatus.Resting;
			FlipLiveMatchTouchBarrier(false);
			if (uiDirector != null)
			{
				uiDirector.FlipMatchFrameUILive(true);
				uiDirector.PresentGamerInteractionControls();
				if (kind != ActorKind.Unset && worth > 0)
				{
					uiDirector.PresentActorTile(kind, worth);
				}
			}
			VerifyFuseReminder();
			yield break;
		}

		// Showcase interrupted by FetchStage: put the cameras and the scene back.
		private void AbortSpotlight()
		{
			TerminateFuseTransition();
			Transform trailObjective = troopLens != null ? troopLens.LookAt : null;
			RestoreSpotlightScene(trailObjective, _spotlightConcealedCombatants, null);
			tweeningLens = false;
			_bypassRequested = false;
			_spotlightRunning = false;
			if (blendLens != null)
			{
				blendLens.AssignLens(0, 0f);
			}
			if (uiDirector != null)
			{
				uiDirector.FlipMatchFrameUILive(true);
			}
		}

		// ------------------------------------------------------------------ lookup helpers

		public GameObject GetActorForWorth(ActorFaction faction, ActorKind kind, int worth)
		{
			if (kind == ActorKind.Overlord)
			{
				kind = ActorKind.Saurian;
			}
			if (foeTemplateCatalog.Count == 0 && guardianTemplateCatalog.Count == 0)
			{
				AssembleTemplateCatalogs();
			}
			GameObject template = faction == ActorFaction.Foe ? SeekFoeFigure(kind, worth) : SeekGuardianFigure(kind, worth);
			if (template == null)
			{
				template = faction == ActorFaction.Foe ? SeekGuardianFigure(kind, worth) : SeekFoeFigure(kind, worth);
			}
			return template;
		}

		private GameObject SeekFoeFigure(ActorKind kind, int worth)
		{
			if (!foeTemplateCatalog.TryGetValue(kind, out Dictionary<int, ActorTemplate> catalog) || catalog.Count == 0)
			{
				return null;
			}
			int entry = ResolveCatalogIdentifier(catalog.Keys, worth);
			return catalog.TryGetValue(entry, out ActorTemplate info) ? info.template : null;
		}

		private GameObject SeekGuardianFigure(ActorKind kind, int worth)
		{
			if (!guardianTemplateCatalog.TryGetValue(kind, out Dictionary<int, GameObject> catalog) || catalog.Count == 0)
			{
				return null;
			}
			int entry = ResolveCatalogIdentifier(catalog.Keys, worth);
			return catalog.TryGetValue(entry, out GameObject template) ? template : null;
		}

		// Exact value, else the largest registered value below it, else the smallest one.
		private static int ResolveCatalogIdentifier(IEnumerable<int> entries, int worth)
		{
			int beneath = int.MinValue;
			int lowest = int.MaxValue;
			foreach (int entry in entries)
			{
				if (entry == worth)
				{
					return entry;
				}
				if (entry < worth && entry > beneath)
				{
					beneath = entry;
				}
				if (entry < lowest)
				{
					lowest = entry;
				}
			}
			return beneath != int.MinValue ? beneath : lowest;
		}

		private bool AttemptGetFoeTemplate(ActorKind kind, int worth, out ActorTemplate info)
		{
			info = default;
			return foeTemplateCatalog.TryGetValue(kind, out Dictionary<int, ActorTemplate> catalog) && catalog.TryGetValue(worth, out info);
		}

		private void AssembleTemplateCatalogs()
		{
			foeTemplateCatalog.Clear();
			guardianTemplateCatalog.Clear();
			if (foeTemplates != null)
			{
				foreach (ActorTemplate info in foeTemplates)
				{
					if (info.template == null)
					{
						continue;
					}
					ActorKind kind = info.kind == ActorKind.Overlord ? ActorKind.Saurian : info.kind;
					if (!foeTemplateCatalog.TryGetValue(kind, out Dictionary<int, ActorTemplate> catalog))
					{
						catalog = new Dictionary<int, ActorTemplate>();
						foeTemplateCatalog.Add(kind, catalog);
					}
					catalog[info.worth] = info;
				}
			}
			if (guardianTemplates != null)
			{
				foreach (ActorTemplate info in guardianTemplates)
				{
					if (info.template == null)
					{
						continue;
					}
					ActorKind kind = info.kind == ActorKind.Overlord ? ActorKind.Saurian : info.kind;
					if (!guardianTemplateCatalog.TryGetValue(kind, out Dictionary<int, GameObject> catalog))
					{
						catalog = new Dictionary<int, GameObject>();
						guardianTemplateCatalog.Add(kind, catalog);
					}
					catalog[info.worth] = info.template;
				}
			}
		}

		private Combatant GetGuardianAtCell(int line, int lane)
		{
			if (registry == null || registry.guardians == null)
			{
				return null;
			}
			foreach (Combatant combatant in registry.guardians)
			{
				if (combatant != null && combatant.line == line && combatant.lane == lane && combatant.vitality > 0f)
				{
					return combatant;
				}
			}
			return null;
		}

		private Combatant GetGuardianAtLatticeSlot(Transform latticeSlot)
		{
			int ordinal = GetLatticeSlotOrdinal(latticeSlot);
			return ordinal < 0 ? null : GetGuardianAtCell(GetLineForLatticeSlotOrdinal(ordinal), GetLaneForLatticeSlotOrdinal(ordinal));
		}

		private int EmptyLatticeSlotTally()
		{
			if (guardianLattice == null)
			{
				return 0;
			}
			int tally = 0;
			foreach (Transform slot in guardianLattice)
			{
				if (slot != null && IsLatticeSlotEmpty(slot))
				{
					tally++;
				}
			}
			return tally;
		}

		public bool IsLatticeSlotEmpty(Transform latticeSlot)
		{
			return latticeSlot != null && GetGuardianAtLatticeSlot(latticeSlot) == null;
		}

		private int GetLatticeSlotOrdinal(Transform latticeSlot)
		{
			return latticeSlot != null && guardianLattice != null ? Array.IndexOf(guardianLattice, latticeSlot) : -1;
		}

		public int GetLatticeSlotOrdinal(int line, int lane)
		{
			return line * LatticeLanes + lane;
		}

		public Transform GetLatticeSlot(int line, int lane)
		{
			int ordinal = GetLatticeSlotOrdinal(line, lane);
			return guardianLattice != null && ordinal >= 0 && ordinal < guardianLattice.Length ? guardianLattice[ordinal] : null;
		}

		private int GetLineForLatticeSlot(Transform latticeSlot)
		{
			return GetLineForLatticeSlotOrdinal(GetLatticeSlotOrdinal(latticeSlot));
		}

		private int GetLineForLatticeSlotOrdinal(int ordinal)
		{
			return ordinal / LatticeLanes;
		}

		private int GetLaneForLatticeSlot(Transform latticeSlot)
		{
			return GetLaneForLatticeSlotOrdinal(GetLatticeSlotOrdinal(latticeSlot));
		}

		private int GetLaneForLatticeSlotOrdinal(int ordinal)
		{
			return ordinal % LatticeLanes;
		}

		private bool IsCombatantFusable(Combatant combatant)
		{
			return SeekFuseCounterpart(combatant) != null;
		}

		private Combatant SeekFuseCounterpart(Combatant combatant)
		{
			if (combatant == null || registry.guardians == null)
			{
				return null;
			}
			foreach (Combatant candidate in registry.guardians)
			{
				if (candidate != null && candidate != combatant && registry.VerifyFuse(combatant, candidate))
				{
					return candidate;
				}
			}
			return null;
		}

		public bool IsActiveStageOverlord()
		{
			return activeStage != null && activeStage.indicator != null && activeStage.indicator.name == OverlordIndicatorTitle;
		}

		public void AttemptPresentForcefulInterlude()
		{
		}

		public void PresentInterlude()
		{
		}

		// ------------------------------------------------------------------ offline earning

		private bool VerifyAwayIncome()
		{
			return GameTuning.isAwayIncomeAccessible;
		}

		private void AttemptBeginAwayIncome()
		{
			if (_awayIncomeRunning || !isActiveAndEnabled || !VerifyAwayIncome() || IsAwayIncomeDialogRevealed())
			{
				return;
			}
			_awayIncomeRunning = true;
			StartCoroutine(PresentAwayIncome());
		}

		private IEnumerator PresentAwayIncome()
		{
			_awayIncomeRunning = true;
			awayIncomeRetry = 0;
			yield return new WaitForSeconds(awayIncomeOpeningPresentPause);
			while (IsAwayIncomeBlocked() && awayIncomeRetry < awayIncomePeakRetry)
			{
				awayIncomeRetry++;
				yield return new WaitForSeconds(awayIncomeRetryGap);
			}
			_awayIncomeRunning = false;
			if (uiDirector == null || IsAwayIncomeBlocked() || !VerifyAwayIncome())
			{
				yield break;
			}
			uiDirector.PresentDialog(eUIName.OfflineEarningPopup);
		}

		private bool IsAwayIncomeBlocked()
		{
			return uiDirector == null || uiDirector.liveDialog != null || activity != ActivityStatus.Resting || tweeningLens;
		}

		private bool IsAwayIncomeDialogRevealed()
		{
			return uiDirector != null && uiDirector.liveDialog != null && uiDirector.liveDialog.uiName == eUIName.OfflineEarningPopup;
		}

		private void IsExiting()
		{
			PersistAwayIncomeMoment();
		}

		private void OnBooted()
		{
			if (audioDirector != null)
			{
				audioDirector.PlayClashMusic();
			}
		}

		private void PersistAwayIncomeMoment()
		{
			// While the offline popup is open its amount is still pending; the popup saves the time when collected.
			if (IsAwayIncomeDialogRevealed())
			{
				return;
			}
			GameTuning.PersistAwayIncomeMoment();
		}

		private static void TraceGuideCompletion()
		{
		}

		private void TraceStageAdvancementInfo()
		{
			if (stageSessionDirector != null)
			{
				stageSessionDirector.BeginStage(GameTuning.stage);
			}
		}

		private void TraceStageOutcome()
		{
			if (stageSessionDirector == null)
			{
				return;
			}
			if (latestStageOutcome == StageOutcome.Accomplishment)
			{
				stageSessionDirector.Victory();
			}
			else
			{
				stageSessionDirector.Lose();
			}
		}

		// ------------------------------------------------------------------ private helpers

		private StageLayout[] ActiveStages()
		{
			if (stageAdvancement == null || stageAdvancement.Length == 0)
			{
				return null;
			}
			StageAdvancement cluster = stageAdvancement[Mathf.Clamp(GameTuning.stageCluster, 0, stageAdvancement.Length - 1)];
			return cluster != null ? cluster.stages : null;
		}

		// L2 tutorial dino: cell 3 (front row, right of the front-centre dino); if the player moved things around, the
		// first free front-row cell next to a front-row dino; null -> normal placement order.
		private Transform SeekGuideSaurianSpot()
		{
			if (guardianLattice == null)
			{
				return null;
			}
			if (GuideSaurianCellOrdinal < guardianLattice.Length && IsLatticeSlotEmpty(guardianLattice[GuideSaurianCellOrdinal]))
			{
				return guardianLattice[GuideSaurianCellOrdinal];
			}
			foreach (Combatant combatant in registry.guardians)
			{
				if (combatant == null || combatant.kind != ActorKind.Saurian || combatant.line != 0 || combatant.vitality <= 0f)
				{
					continue;
				}
				for (int flank = 1; flank >= -1; flank -= 2)
				{
					int lane = combatant.lane + flank;
					if (lane < 0 || lane >= LatticeLanes)
					{
						continue;
					}
					Transform slot = GetLatticeSlot(0, lane);
					if (IsLatticeSlotEmpty(slot))
					{
						return slot;
					}
				}
			}
			return null;
		}

		private Transform SeekInitialEmptySpot()
		{
			if (guardianLattice != null)
			{
				foreach (int ordinal in SpotOrderOrdinals)
				{
					if (ordinal < guardianLattice.Length && guardianLattice[ordinal] != null && IsLatticeSlotEmpty(guardianLattice[ordinal]))
					{
						return guardianLattice[ordinal];
					}
				}
			}
			// Fallbacks (same order as authored in the scene).
			if (spotOrder != null)
			{
				foreach (Transform slot in spotOrder)
				{
					if (slot != null && GetLatticeSlotOrdinal(slot) >= 0 && IsLatticeSlotEmpty(slot))
					{
						return slot;
					}
				}
			}
			if (guardianLattice != null)
			{
				foreach (Transform slot in guardianLattice)
				{
					if (IsLatticeSlotEmpty(slot))
					{
						return slot;
					}
				}
			}
			return null;
		}

		private void BeginArenaSlotMotion()
		{
			_arenaSlotSchedule.Clear();
			foreach (Combatant combatant in registry.combatants)
			{
				if (combatant != null)
				{
					combatant.gameObject.SetActive(false);
					_arenaSlotSchedule.Add(combatant);
				}
			}
			if (_arenaSlotSchedule.Count == 0)
			{
				return;
			}
			Combatant speaker = registry.guardians.Count > 0 ? registry.guardians[0] : _arenaSlotSchedule[0];
			PerformAudioCue(speaker.kind, AudioCue.Slot, speaker.worth);
			_arenaSlotRoutine = StartCoroutine(ArenaSlotRoutine());
		}

		private IEnumerator ArenaSlotRoutine()
		{
			WaitForSeconds hold = new WaitForSeconds(ArenaSlotStagger);
			while (_arenaSlotSchedule.Count > 0)
			{
				Combatant combatant = _arenaSlotSchedule[0];
				_arenaSlotSchedule.RemoveAt(0);
				BurstCombatantIn(combatant);
				if (_arenaSlotSchedule.Count > 0)
				{
					yield return hold;
				}
			}
			_arenaSlotRoutine = null;
		}

		private void ConcludeArenaSlotMotion()
		{
			HaltRoutine(ref _arenaSlotRoutine);
			foreach (Combatant combatant in _arenaSlotSchedule)
			{
				BurstCombatantIn(combatant);
			}
			_arenaSlotSchedule.Clear();
		}

		private static void BurstCombatantIn(Combatant combatant)
		{
			if (combatant == null)
			{
				return;
			}
			combatant.gameObject.SetActive(true);
			combatant.ArenaMotion(ActorArenaActivity.Slot);
		}

		private void ImposeGuideFingers()
		{
			ConcealFuseFingers();
			if (uiDirector == null)
			{
				return;
			}
			if (GameTuning.finishedGuideKind == GuideKind.PersonAcquisition && AnyGuardianOfKindInLine(ActorKind.Saurian, 0))
			{
				FinishShiftSaurianForwardGuide();
			}
			uiDirector.FlipShiftForwardGuide(ShouldPresentShiftForwardFinger());
		}

		// ShiftForward step: the 3D hand (+ green arrow) drags the dino forward; shown while no dino stands in the front row.
		private bool ShouldPresentShiftForwardFinger()
		{
			if (GetGuidePhase() != GuidePhase.ShiftForward || AnyGuardianOfKindInLine(ActorKind.Saurian, 0))
			{
				return false;
			}
			return AnyGuardianOfKindInLine(ActorKind.Saurian, 2) || AnyGuardianOfKindInLine(ActorKind.Saurian, 1);
		}

		// Keeps saved tutorial progress consistent with the level (called by FetchStage after the board is built):
		// L1 = InitialClash / ProcureSoldier / ShiftForward / ClashSolely, L2 = ProcureSaurian / Merge / ClashSolely, L3+ = tutorial over.
		private void NormalizeGuideStatus()
		{
			int stage = GameTuning.stage;
			GuideKind guide = GameTuning.finishedGuideKind;
			if (stage >= 3)
			{
				if (guide < GuideKind.FuseCombatants)
				{
					GameTuning.DidFinishGuide(GuideKind.FuseCombatants);
				}
				return;
			}
			if (stage == 2)
			{
				if (guide < GuideKind.ShiftSaurianForward)
				{
					GameTuning.DidFinishGuide(GuideKind.ShiftSaurianForward);
				}
				else if (guide == GuideKind.SaurianAcquisition && !SeekFusePair(out _, out _))
				{
					// Merge step without anything to merge (inconsistent save): continue with START FIGHT.
					GameTuning.DidFinishGuide(GuideKind.FuseCombatants);
				}
				return;
			}
			if (guide == GuideKind.PersonAcquisition && AnyGuardianOfKindInLine(ActorKind.Saurian, 0))
			{
				GameTuning.DidFinishGuide(GuideKind.ShiftSaurianForward);
			}
		}

		// B9: unlocked tiers follow the units on the board (a tier is unlocked once a unit of it exists).
		private void SyncUnsealedGradesWithArena()
		{
			SyncUnsealedGrade(ActorKind.Person);
			SyncUnsealedGrade(ActorKind.Saurian);
		}

		private void SyncUnsealedGrade(ActorKind kind)
		{
			int peak = 0;
			foreach (Combatant combatant in registry.guardians)
			{
				if (combatant != null && combatant.kind == kind)
				{
					peak = Mathf.Max(peak, combatant.worth);
				}
			}
			if (peak > 0 && IsFreshGrade(kind, peak))
			{
				UnsealGradeInfo(kind, peak);
			}
		}

		// round(peakStageIncome * damage fraction), fraction = 1 - remaining enemy HP / enemy HP at fight start
		// (the same fraction the fail screen shows as TOTAL ENEMY DAMAGE).
		private long GetDefeatIncome()
		{
			float begin = PlayMechanics.foeBeginVitality;
			if (begin <= 0f)
			{
				return 0L;
			}
			double portion = Mathf.Clamp01(1f - registry.GetLeftoverVitality(ActorFaction.Foe) / begin);
			return Math.Max(0L, (long)Math.Round(PlayMechanics.peakStageIncome * portion, MidpointRounding.AwayFromZero));
		}

		private void AttemptPresentFuseGuideHint()
		{
			if (GameTuning.finishedGuideKind != GuideKind.SaurianAcquisition || activity != ActivityStatus.Resting || _fuseGuideRoutine != null)
			{
				return;
			}
			if (SeekFusePair(out Combatant initial, out Combatant latter))
			{
				BeginFuseGuide(initial, latter);
			}
		}

		// b_021/b_022: the hand starts on the LEFT unit of the pair (L2: the front dino at c2, green arrow tail on its left)
		// and drags right onto the other one (the new dino at c3) - the authored clip is "howtoplay lefttoright", played
		// unrotated (a yawed rig also hides the arrow: its SpriteMask is back-face culled).
		private void BeginFuseGuide(Combatant alpha, Combatant beta)
		{
			if (!isActiveAndEnabled || alpha == null || beta == null)
			{
				return;
			}
			Transform slotA = GetLatticeSlot(alpha.line, alpha.lane);
			Transform slotB = GetLatticeSlot(beta.line, beta.lane);
			Vector3 locA = slotA != null ? slotA.position : alpha.transform.position;
			Vector3 locB = slotB != null ? slotB.position : beta.transform.position;
			bool reversed = locB.x < locA.x - 0.01f;
			Combatant leader = reversed ? beta : alpha;
			_fuseGuideLeader = leader;
			_fuseGuidePartner = reversed ? alpha : beta;
			HaltRoutine(ref _fuseGuideRoutine);
			_fuseGuideRoutine = StartCoroutine(PresentFuseGuide(leader.line, leader.lane, reversed ? locA : locB));
		}

		private void ConcealFuseFingers()
		{
			GameObject lateral = GetSceneHowToPlay(0);
			if (lateral != null)
			{
				lateral.SetActive(false);
			}
		}

		private GameObject GetSceneHowToPlay(int ordinal)
		{
			if (uiDirector == null || uiDirector.sceneHowTo == null || ordinal < 0 || ordinal >= uiDirector.sceneHowTo.Length)
			{
				return null;
			}
			return uiDirector.sceneHowTo[ordinal];
		}

		private bool AnyGuardianOfKindInLine(ActorKind kind, int line)
		{
			foreach (Combatant combatant in registry.guardians)
			{
				if (combatant != null && combatant.kind == kind && combatant.line == line && combatant.vitality > 0f)
				{
					return true;
				}
			}
			return false;
		}

		private bool AnyGuardian(ActorKind kind, int worth)
		{
			foreach (Combatant combatant in registry.guardians)
			{
				if (combatant != null && combatant.kind == kind && combatant.worth == worth)
				{
					return true;
				}
			}
			return false;
		}

		public static bool IsFreshGrade(ActorKind kind, int worth)
		{
			if (kind != ActorKind.Person && kind != ActorKind.Saurian)
			{
				return false;
			}
			PlayMechanics.peakUnsealedActors.TryGetValue(kind, out int peak);
			return worth > peak;
		}

		private void UnsealGrade(ActorKind kind, int worth, bool performMaturationAudio)
		{
			UnsealGradeInfo(kind, worth);
			if (performMaturationAudio)
			{
				PerformUISample(maturationAudio);
			}
		}

		private static void UnsealGradeInfo(ActorKind kind, int worth)
		{
			if (IsFreshGrade(kind, worth))
			{
				PlayMechanics.ReviseUnsealActors(kind, worth);
			}
		}

		private static int EncodingOf(ActorKind kind, int worth)
		{
			return (int)kind * 10000 + worth;
		}

		private static bool IsLiving(Combatant combatant)
		{
			return combatant != null && combatant.vitality > 0f && combatant.actorStatus != ActorStatus.Perish;
		}

		private static void PerformAudioCue(ActorKind kind, AudioCue cue, int worth)
		{
			GameTuning.audioSignal?.Invoke(kind, cue, worth);
		}

		private void PerformUISample(eSoundName[] samples, int ordinal)
		{
			if (samples != null && ordinal >= 0 && ordinal < samples.Length)
			{
				PerformUISample(samples[ordinal]);
			}
		}

		private void PerformUISample(eSoundName sample)
		{
			if (sample != eSoundName.None && audioDirector != null)
			{
				audioDirector.PerformUISample(sample);
			}
		}

		private void HaltRoutine(ref Coroutine job)
		{
			if (job != null)
			{
				StopCoroutine(job);
				job = null;
			}
		}

		private void TerminateFuseTransition()
		{
			if (_fuseTransition != null)
			{
				_fuseTransition.Kill();
				_fuseTransition = null;
			}
		}

		private static void TerminateCombatantTransitions(List<Combatant> combatants)
		{
			if (combatants == null)
			{
				return;
			}
			foreach (Combatant combatant in combatants)
			{
				if (combatant == null)
				{
					continue;
				}
				combatant.transform.DOKill();
				if (combatant.core != null)
				{
					combatant.core.DOKill();
				}
			}
		}

		private Camera ResolvePrimaryLens()
		{
			Camera lens = Camera.main;
			if (lens == null && lensRig != null)
			{
				lens = lensRig.GetComponentInChildren<Camera>();
			}
			return lens;
		}

		private void ImposeLensFieldOfView()
		{
			if (blendLens == null)
			{
				return;
			}
			GameTuning.handsetArenaLensExtent = 0f;
			CinemachineVirtualCameraBase arenaLens = blendLens.GetLens(0);
			if (arenaLens != null)
			{
				arenaLens.transform.position = SeatHandsetArenaLens(arenaLens.transform);
			}
			foreach (CinemachineVirtualCamera virtualLens in blendLens.GetComponentsInChildren<CinemachineVirtualCamera>(true))
			{
				virtualLens.m_Lens.FieldOfView = virtualLens == troopLens ? GameTuning.lensExtent : GameTuning.arenaLensExtent;
			}
		}

		private Vector3 SeatHandsetArenaLens(Transform lensPivot)
		{
			if (!_arenaLensStandardCached)
			{
				_arenaLensStandardCached = true;
				_arenaLensStandardLoc = lensPivot.position;
			}
			Transform midSlot = GetLatticeSlot(1, 2);
			if (Toolbox.isTablet || Screen.height <= 0 || midSlot == null)
			{
				return _arenaLensStandardLoc;
			}
			float proportion = (float)Screen.width / Screen.height;
			Quaternion pose = lensPivot.rotation;
			Vector3 forward = pose * Vector3.forward;
			Vector3 upward = pose * Vector3.up;
			Vector3 rim = new Vector3(ArenaRimHalfBreadth, ArenaRimElevation, midSlot.position.z) - _arenaLensStandardLoc;
			float rimReach = Vector3.Dot(rim, forward);
			Vector3 brink = new Vector3(0f, ArenaRimElevation, ArenaRimNearDepth) - _arenaLensStandardLoc;
			float brinkReach = Vector3.Dot(brink, forward);
			if (rimReach <= 0.01f || brinkReach <= 0.01f)
			{
				return _arenaLensStandardLoc;
			}
			float halfTan = Vector3.Dot(rim, pose * Vector3.right) / rimReach / ((1f - 2f * ArenaRimScreenMargin) * proportion);
			float level = ClashControlCrestLevel * Mathf.Min(1f, proportion / GameTuning.standardViewportProportion);
			float lift = Vector3.Dot(brink, upward) - (level - 0.5f) * 2f * halfTan * brinkReach;
			GameTuning.handsetArenaLensExtent = 2f * Mathf.Atan(halfTan) * Mathf.Rad2Deg;
			return _arenaLensStandardLoc + upward * lift;
		}

		private void FindConfettiEffect()
		{
			if (confettiEffect != null) return;
			GameObject[] roots = gameObject.scene.GetRootGameObjects();
			for (int i = 0; i < roots.Length; i++)
			{
				if (roots[i] != null && roots[i].name.Contains("ConfettiExplosionMix"))
				{
					confettiEffect = roots[i];
					break;
				}
			}
		}
	}
}
