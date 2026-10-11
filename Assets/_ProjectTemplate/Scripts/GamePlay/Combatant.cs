using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Funlab.MergeMaster
{
	public class Combatant : Actor
	{
		private const int CombatantStratum = 6;

		private const int DemiseStratum = 7;

		private const int SpotlightStratum = 10;

		private const int PeakTroopWorth = 1024;

		private const float MeleeImpactRangeTolerance = 1.3f;

		private const float ImpactSparkElevation = 0.25f;

		private const float DemiseLightSpan = 0.27f;

		private const float ArenaEffectSpan = 2f;

		private const string ShardsTitle = "Shards";

		private const int DebrisStratum = 18;

		private const float ShardRestSpan = 1.8f;

		private const float ShardFadeSpan = 0.35f;

		private const float ShardDriftFloor = 0.25f;

		private const float ShardDriftPeak = 0.55f;

		private const float ShardRiseFloor = 1.1f;

		private const float ShardRisePeak = 2f;

		private const float ShardSpread = 35f;

		private const float ShardSpinFloor = 5f;

		private const float ShardSpinPeak = 9f;

		private const float ShardRollBoost = 1f;

		private const float ShardSpinCeiling = 30f;

		private const float ShardTouchdownLag = 0.03f;

		private const float ShardEaseOut = 1f;

		private const float ArenaShardHalfBreadth = 1.35f;

		private const float ArenaShardHalfDepth = 2.05f;

		private static readonly Quaternion VitalityMeterRotation = Quaternion.Euler(30f, 0f, 0f);

		private static readonly List<Renderer> ShatterVisualsBuffer = new List<Renderer>();

		private struct ShardPart
		{
			public Transform piece;

			public Transform anchor;

			public Rigidbody body;

			public Collider hull;
		}

		private Transform _shardsPivot;

		private ShardPart[] _shardParts;

		private readonly Dictionary<int, Combatant> _contactOwners = new Dictionary<int, Combatant>();

		public Combatant objective;

		[Header("HealthBar")]
		public GameObject vitalityMeter;

		public SpriteRenderer vitalityMeterInfill;

		public Color[] vitalityMeterInfillTints;

		[Header("Outline")]
		private Outline contour;

		[SerializeField]
		public Color choiceTint;

		[SerializeField]
		private Color failureTint;

		[SerializeField]
		[Range(0f, 10f)]
		public float contourBreadth;

		[Header("Fx")]
		public GameObject fuseEffect;

		public GameObject slotEffect;

		private Rigidbody physicsBody;

		private BoxCollider cubeCollider;

		private bool _chosen;

		private Vector3 latestRunningLoc;

		private Combatant fuseObjective;

		public float vitality;

		[HideInInspector]
		public float beginVitality;

		private float vitalityMeterInfillStandardBreadth;

		public float impactCoefficient;

		public readonly float standardImpactCoefficient = 1f;

		private readonly float runPace = 1.0f; // measured: 1.00 ±0.03 world u/s for every dino tier (A F2)

		public readonly float haltReach = 0.35f;

		private readonly float standardShakeSpan = 0.15f;

		private readonly float shakeMagnitude = 0.02f;

		private readonly float shakeDecay = 1f;

		private float shakeSpan;

		private int _tailoredHarmRate;

		public bool isContactingRival;

		// 0 = none, 1 = merge target (green), 2 = max-tier twin (red); written by the dragged fighter.
		internal int hoverContour;

		private bool _booted;

		private int _basisStratum = CombatantStratum;

		private int _imposedContour = -1;

		private Combatant _contactingCombatant;

		// Measured on the original (L3 4v4): a melee unit that runs into a standing ally stops there for good and bites its
		// target from where it stands (the raptor behind its own Triceramon keeps landing 2.4 bites on the enemy).
		private bool _obstructedByAlly;

		private Combatant _obstructingAlly;

		// Consecutive FixedUpdates in which a RUNNING ally stood in the way of this unit's step. A runner ahead normally
		// clears the way the next step (file-following); two runners heading for the same target can hold each other
		// forever, so after SprinterHoldTicks the unit slides along the ally's collider instead of waiting.
		private int _heldBySprinterTicks;

		private const int SprinterHoldTicks = 3;

		private const int SprinterPassTicks = 12;

		private Coroutine _huntRoutine;

		private bool _hunting;

		private Coroutine _slotEffectRoutine;

		private Coroutine _fuseEffectRoutine;

		private bool _pendingAssaultVariant;

		private float _latestAssaultPortion;

		// Extra delay before this unit's FIRST attack (measured +0.2 s when the player formation is a single value-1 unit).
		private float _initialAssaultPause;

		private float _contactMoment = -1f;

		public bool chosen
		{
			get
			{
				return _chosen;
			}
			set
			{
				_chosen = value;
				ArenaMotion(value ? ActorArenaActivity.Choose : ActorArenaActivity.Resting);
				if (hasHoldMotion)
				{
					PerformMotion(value ? "Hold" : "Idle");
				}
				if (vitalityMeter != null)
				{
					vitalityMeter.SetActive(!value && IsLiving);
				}
				if (!value)
				{
					SetFuseObjective(null);
				}
				SenseOutline();
			}
		}

		// Damage of one hit (bite / arrow / magic ball), for player and enemy units alike (see GetAssault).
		// impactCoefficient is 1 on the prefab; the original has no damage reduction on the tutorial level.
		public float harmPerImpact
		{
			get
			{
				if (_tailoredHarmRate > 0)
				{
					return _tailoredHarmRate;
				}
				float coefficient = impactCoefficient > 0f ? impactCoefficient : standardImpactCoefficient;
				return GetAssault(kind, worth) * coefficient;
			}
		}

		public int TailoredHarmRate => _tailoredHarmRate;

		internal bool IsLiving => vitality > 0f && actorStatus != ActorStatus.Perish;

		private bool IsCloseCombat => kind == ActorKind.Saurian || kind == ActorKind.Overlord;

		// ---- in-fight unit stats, measured frame by frame on the original (team/unit bar drops, tl_* runs):
		// HP and damage are LINEAR in the unit value v for player and enemy units alike:
		//   Dino/Boss HP 20·v, bite 2.4·v      Warrior HP 8·v, arrow/magic 1.6·v
		// (T1 dino 20/2.4, T2 40/4.85, T3 80/9.6; T1 warrior 8/1.6). The card formula A7 (round(20·2^(1.24(t−1))) …)
		// is only what the UI cards print (Gamification.GetCharacterCardHealth/Attack), not what the fight uses.

		private const float SaurianVitalityPerWorth = 20f;

		private const float SaurianHarmPerWorth = 2.4f;

		private const float PersonVitalityPerWorth = 8f;

		private const float PersonHarmPerWorth = 1.6f;

		public static float GetPeakVitality(ActorKind actorKind, int actorWorth)
		{
			ActorKind metricKind = MetricKindOf(actorKind);
			if (metricKind == ActorKind.Unset || actorWorth <= 0)
			{
				return 0f;
			}
			return (metricKind == ActorKind.Person ? PersonVitalityPerWorth : SaurianVitalityPerWorth) * actorWorth;
		}

		public static float GetAssault(ActorKind actorKind, int actorWorth)
		{
			ActorKind metricKind = MetricKindOf(actorKind);
			if (metricKind == ActorKind.Unset || actorWorth <= 0)
			{
				return 0f;
			}
			return (metricKind == ActorKind.Person ? PersonHarmPerWorth : SaurianHarmPerWorth) * actorWorth;
		}

		// Full health of this unit for its current type/value (used on spawn and after a merge).
		internal float GetFullVitality()
		{
			if (MetricKindOf(kind) == ActorKind.Unset)
			{
				return PlayMechanics.GetVitality(this);
			}
			return GetPeakVitality(kind, worth);
		}

		private static ActorKind MetricKindOf(ActorKind actorKind)
		{
			switch (actorKind)
			{
				case ActorKind.Person:
					return ActorKind.Person;
				case ActorKind.Saurian:
				case ActorKind.Overlord:
					return ActorKind.Saurian;
				default:
					return ActorKind.Unset;
			}
		}

		internal static bool IsViableObjective(Combatant combatant)
		{
			return combatant != null && combatant.IsLiving && combatant.gameObject.activeInHierarchy;
		}

		internal override void GuaranteeBoot()
		{
			if (_booted)
			{
				return;
			}
			_booted = true;
			physicsBody = GetComponent<Rigidbody>();
			cubeCollider = GetComponent<BoxCollider>();
			if (core != null)
			{
				coreMotionRig = core.GetComponent<Animator>();
			}
			if (vitalityMeterInfill != null)
			{
				vitalityMeterInfillStandardBreadth = vitalityMeterInfill.transform.localScale.x;
			}
		}

		internal override void OnActorAssembled()
		{
			contour = null;
			_imposedContour = -1;
			if (_basisStratum != CombatantStratum)
			{
				ShiftToStratum(_basisStratum);
			}
			SetVitalityMeterLocation();
			GatherShards();
		}

		protected virtual void Start()
		{
			GuaranteeBoot();
			SetVitalityMeterLocation();
			ReviseVitality();
		}

		// The original keeps the unit bar at the same world offset for both sides (toward the camera, not rotated with the
		// unit): enemies face 180 deg, and a rotated offset put their bars ~0.18 u further back (≈35 px higher than b_001/b_002).
		private Vector3 VitalityMeterWorldLocation
		{
			get
			{
				// Offset in the fighter's own frame, applied without its rotation.
				Vector3 displacement = Quaternion.Inverse(transform.rotation) * (vitalityMeterLocation.position - transform.position);
				// The board pop (CharacterEffect_Place / _Merge scale Body up to 1.25) leaves the original's bar where it is.
				if (coreMotionRig != null)
				{
					Vector3 burst = coreMotionRig.transform.localScale;
					if (burst.x > 0.01f && burst.y > 0.01f && burst.z > 0.01f)
					{
						displacement = new Vector3(displacement.x / burst.x, displacement.y / burst.y, displacement.z / burst.z);
					}
				}
				return transform.position + displacement;
			}
		}

		private void SetVitalityMeterLocation()
		{
			if (vitalityMeter != null && vitalityMeterLocation != null)
			{
				vitalityMeter.transform.position = VitalityMeterWorldLocation;
				vitalityMeter.transform.rotation = VitalityMeterRotation;
			}
		}

		// Merge showcase: the original shows the close-up without the unit bar.
		internal void SetVitalityMeterConcealed(bool concealed)
		{
			if (vitalityMeter != null && IsLiving)
			{
				vitalityMeter.SetActive(!concealed);
			}
		}

		public void ReviseVitality(float givenVitality = 0f)
		{
			GuaranteeBoot();
			if (givenVitality > 0f)
			{
				beginVitality = givenVitality;
				this.vitality = givenVitality;
			}
			RenewVitalityMeter();
		}

		private void RenewVitalityMeter()
		{
			if (vitalityMeterInfill == null)
			{
				return;
			}
			float proportion = beginVitality > 0f ? Mathf.Clamp01(this.vitality / beginVitality) : 0f;
			Transform infill = vitalityMeterInfill.transform;
			Vector3 size = infill.localScale;
			size.x = vitalityMeterInfillStandardBreadth * proportion;
			infill.localScale = size;
			if (vitalityMeterInfillTints != null && vitalityMeterInfillTints.Length >= 2)
			{
				vitalityMeterInfill.color = vitalityMeterInfillTints[faction == ActorFaction.Foe ? 0 : 1];
			}
		}

		public void ReviseTailoredHarmRate(int harmRate)
		{
			_tailoredHarmRate = harmRate;
		}

		public void Clash(Combatant rival)
		{
			GuaranteeBoot();
			if (!IsLiving)
			{
				return;
			}
			HaltHunt();
			if (this.objective != rival)
			{
				_obstructedByAlly = false;
				_heldBySprinterTicks = 0;
				_obstructingAlly = null;
			}
			this.objective = rival;
			if (rival == null)
			{
				BeginHunt();
				return;
			}
			if (!IsCloseCombat && _initialAssaultPause > 0f)
			{
				// Lone value-1 formation: the archer starts its attack clip 0.2 s late (measured, E1).
				float pause = _initialAssaultPause;
				_initialAssaultPause = 0f;
				actorStatus = ActorStatus.Resting;
				StartCoroutine(BeginAssaultAfter(pause));
				return;
			}
			actorStatus = IsCloseCombat ? ActorStatus.Sprint : ActorStatus.Assault;
		}

		private IEnumerator BeginAssaultAfter(float pause)
		{
			yield return new WaitForSeconds(pause);
			if (IsLiving && IsViableObjective(objective) && gameplayCore != null && gameplayCore.activity == ActivityStatus.Assaulting)
			{
				actorStatus = ActorStatus.Assault;
			}
		}

		// Measured on the original: when the player's formation is one single value-1 unit, its first attack comes
		// 0.2 s later than usual (melee: 0.2 s after contact; ranged: clip starts at +0.2 s). Set by GameplayCore at fight start.
		internal void PauseInitialAssault(float secs)
		{
			_initialAssaultPause = Mathf.Max(0f, secs);
			_contactMoment = -1f;
		}

		public void CombatantDidPerish(Combatant dispatcher)
		{
			if (dispatcher == this)
			{
				return;
			}
			if (_contactingCombatant == dispatcher)
			{
				_contactingCombatant = null;
				isContactingRival = false;
			}
			if (objective == dispatcher)
			{
				objective = null;
				if (IsLiving && gameplayCore != null && gameplayCore.activity == ActivityStatus.Assaulting)
				{
					BeginHunt();
				}
			}
		}

		private void BeginHunt()
		{
			if (_hunting || !isActiveAndEnabled)
			{
				return;
			}
			_hunting = true;
			// The first iteration runs synchronously and may already have finished the search.
			Coroutine huntTask = StartCoroutine(HuntForObjective());
			if (_hunting)
			{
				_huntRoutine = huntTask;
			}
		}

		private void HaltHunt()
		{
			if (_huntRoutine != null)
			{
				StopCoroutine(_huntRoutine);
			}
			_huntRoutine = null;
			_hunting = false;
		}

		private IEnumerator HuntForObjective()
		{
			WaitForSeconds hold = new WaitForSeconds(0.1f);
			while (gameplayCore != null && gameplayCore.activity == ActivityStatus.Assaulting && IsLiving)
			{
				Combatant upcoming = gameplayCore.registry != null ? gameplayCore.registry.GetNearestOpposition(this) : null;
				if (upcoming != null)
				{
					_huntRoutine = null;
					_hunting = false;
					Clash(upcoming);
					yield break;
				}
				if (actorStatus != ActorStatus.Resting && actorStatus != ActorStatus.Conquest)
				{
					actorStatus = ActorStatus.Resting;
				}
				yield return hold;
			}
			_huntRoutine = null;
			_hunting = false;
		}

		protected override void LateUpdate()
		{
			base.LateUpdate();
		}

		protected override void StatusRevise()
		{
			if (!IsLiving)
			{
				return;
			}
			if (isContactingRival && !IsViableObjective(_contactingCombatant))
			{
				isContactingRival = false;
				_contactingCombatant = null;
			}
			if (gameplayCore == null || gameplayCore.activity != ActivityStatus.Assaulting)
			{
				return;
			}
			if (_obstructedByAlly && !BarrierStillStanding())
			{
				// The standing ally that stopped us died, ran off or is no longer next to us: walk on to the target.
				_obstructedByAlly = false;
				_obstructingAlly = null;
			}
			switch (actorStatus)
			{
				case ActorStatus.Sprint:
					if (!IsViableObjective(objective))
					{
						DropObjective();
					}
					else if (IsTouching(objective, 1f) || isContactingRival || _obstructedByAlly)
					{
						if (_initialAssaultPause > 0f)
						{
							if (_contactMoment < 0f)
							{
								_contactMoment = Time.time;
							}
							if (Time.time - _contactMoment < _initialAssaultPause)
							{
								break;
							}
							_initialAssaultPause = 0f;
						}
						actorStatus = ActorStatus.Assault;
					}
					break;
				case ActorStatus.Assault:
					if (!IsViableObjective(objective))
					{
						if (objective != null)
						{
							DropObjective();
						}
						break;
					}
					FaceObjective();
					if (IsCloseCombat)
					{
						if (!IsTouching(objective, MeleeImpactRangeTolerance) && !isContactingRival && !_obstructedByAlly)
						{
							actorStatus = ActorStatus.Sprint;
						}
					}
					else if (_pendingAssaultVariant)
					{
						AttemptSwapAssaultVariant();
					}
					break;
			}
		}

		private void DropObjective()
		{
			objective = null;
			BeginHunt();
		}

		private void AttemptSwapAssaultVariant()
		{
			if (motionRig == null || !motionRig.isActiveAndEnabled || motionRig.IsInTransition(0))
			{
				return;
			}
			float clipAdvance = motionRig.GetCurrentAnimatorStateInfo(0).normalizedTime;
			float portion = clipAdvance - Mathf.Floor(clipAdvance);
			bool looped = portion < _latestAssaultPortion;
			_latestAssaultPortion = portion;
			// Switch only when the clip loops: the original shoots exactly once per 1.00 s clip (no early cut).
			if (!looped)
			{
				return;
			}
			_pendingAssaultVariant = false;
			PerformUpcomingAssaultVariant();
		}

		private float ContactReach(Combatant counterpart)
		{
			// Measured: melee units stop when their sphere colliders touch (centre distance 0.28 raptor-raptor, 0.31 Tri-raptor).
			return HullRadius(this) + HullRadius(counterpart);
		}

		private static float HullRadius(Combatant combatant)
		{
			SphereCollider sphereHull = combatant.sphereHitbox;
			if (sphereHull == null)
			{
				return 0.15f;
			}
			return sphereHull.radius * Mathf.Abs(sphereHull.transform.lossyScale.x);
		}

		private bool IsTouching(Combatant counterpart, float leeway)
		{
			if (isContactingRival && _contactingCombatant == counterpart)
			{
				return true;
			}
			Vector3 difference = counterpart.transform.position - transform.position;
			difference.y = 0f;
			float limit = ContactReach(counterpart) * leeway;
			return difference.sqrMagnitude <= limit * limit;
		}

		public override void MotionSignal(string signalKind)
		{
			switch (signalKind)
			{
				case "ShowCharacter":
					PresentActor();
					break;
				case "Hit":
					OnImpactSignal();
					break;
				case "Shoot":
					OnFireSignal();
					break;
				case "PullString":
					OnDrawStringSignal();
					break;
				case "DieComplete":
					OnPerishFinish();
					break;
			}
		}

		private bool CanSmite()
		{
			return IsLiving && actorStatus == ActorStatus.Assault && gameplayCore != null && gameplayCore.activity == ActivityStatus.Assaulting;
		}

		private void OnImpactSignal()
		{
			if (!CanSmite() || !IsViableObjective(objective))
			{
				return;
			}
			if (IsTouching(objective, MeleeImpactRangeTolerance) || isContactingRival || _obstructedByAlly)
			{
				gameplayCore.PresentMeleeSpark(objective.GetImpactMark(transform.position), transform.rotation);
				objective.DidTakeImpact(this);
				PerformCue(AudioCue.Impact);
			}
			else
			{
				actorStatus = ActorStatus.Sprint;
			}
		}

		private Vector3 GetImpactMark(Vector3 assailantLocation)
		{
			Vector3 heart = transform.position + Vector3.up * ImpactSparkElevation;
			Vector3 heading = assailantLocation - transform.position;
			heading.y = 0f;
			if (sphereHitbox == null || heading.sqrMagnitude < 0.0001f)
			{
				return heart;
			}
			return heart + heading.normalized * (sphereHitbox.radius * sphereHitbox.transform.lossyScale.x);
		}

		private void OnFireSignal()
		{
			if (!CanSmite() || !IsViableObjective(objective))
			{
				return;
			}
			gameplayCore.FireProjectile(this);
			PerformCue(AudioCue.ReleaseBowstring);
			if (dart != null)
			{
				dart.SetActive(false);
			}
			if (AssaultSampleTally > 1)
			{
				_pendingAssaultVariant = true;
			}
		}

		private void OnDrawStringSignal()
		{
			if (!CanSmite())
			{
				return;
			}
			PerformCue(AudioCue.DrawBowstring);
			if (dart != null)
			{
				dart.SetActive(true);
			}
		}

		// End of the Die clip. The original keeps the corpses lying on the board (last Die frame) for the rest of the
		// fight and during the victory/defeat pose (c_007, b_004, b_016); they go away with hub.Clear() on FetchStage.
		private void OnPerishFinish()
		{
			if (actorStatus != ActorStatus.Perish || core == null)
			{
				return;
			}
			if (vitalityMeter != null)
			{
				vitalityMeter.SetActive(false);
			}
		}

		public int IncomeResponder(float harm)
		{
			int income = PlayMechanics.GetImpactIncome(this, harm);
			if (income > 0 && gameplayCore != null)
			{
				gameplayCore.IncomeResponder(income);
			}
			return income;
		}

		public float DidTakeImpact(Combatant assailant, float harmScale = 1f)
		{
			if (assailant == null || !IsLiving)
			{
				return 0f;
			}
			float harm = (assailant.TailoredHarmRate > 0 ? assailant.TailoredHarmRate : assailant.harmPerImpact) * harmScale;
			if (IsCloseCombat && assailant.IsCloseCombat && objective != assailant && IsViableObjective(assailant) &&
				(_contactingCombatant == assailant || IsTouching(assailant, MeleeImpactRangeTolerance)))
			{
				objective = assailant;
			}
			float imposed = Mathf.Min(harm, vitality);
			vitality -= imposed;
			ReviseVitality();
			Jolt();
			PlayMechanics.TraceImpact(assailant, vitality, this, imposed);

			if (assailant.faction == ActorFaction.Guardian && faction == ActorFaction.Foe)
			{
				int income = assailant.IncomeResponder(imposed);
				if (income > 0 && gameplayCore != null)
				{
					Vector3 tagLoc = vitalityMeter != null ? vitalityMeter.transform.position : transform.position + Vector3.up * 0.5f;
					gameplayCore.PresentImpactTag(income, tagLoc + Vector3.up * 0.1f);
				}
			}
			if (gameplayCore != null && gameplayCore.uiDirector != null)
			{
				gameplayCore.uiDirector.ReviseCombatantVitalityMeter(faction);
			}
			if (vitality <= 0f)
			{
				Perish();
			}
			return imposed;
		}

		internal void Perish()
		{
			if (actorStatus == ActorStatus.Perish)
			{
				return;
			}
			vitality = 0f;
			HaltHunt();
			objective = null;
			_obstructedByAlly = false;
			_heldBySprinterTicks = 0;
			_obstructingAlly = null;
			isContactingRival = false;
			_contactingCombatant = null;
			_pendingAssaultVariant = false;
			actorStatus = ActorStatus.Perish;
			if (physicsBody != null)
			{
				if (!physicsBody.isKinematic)
				{
					physicsBody.velocity = Vector3.zero;
				}
				physicsBody.isKinematic = true;
			}
			// The emptied (black) unit bar stays visible during the white death flash (c_006), then hides.
			CancelInvoke(nameof(ConcealDeadVitalityMeter));
			Invoke(nameof(ConcealDeadVitalityMeter), DemiseLightSpan);
			if (cubeCollider != null)
			{
				cubeCollider.enabled = false;
			}
			if (sphereHitbox != null)
			{
				sphereHitbox.enabled = false;
			}
			PerformCue(AudioCue.Perish);
			ImpactLight();
			Toolbox.PerformTactileImpact(ImpactFeedbackStyle.Light);
			if (gameplayCore != null)
			{
				gameplayCore.CombatantDidPerish(this);
			}
		}

		public virtual void FuseWith(Combatant donorCombatant, bool announce = true)
		{
			if (donorCombatant == null || donorCombatant == this || gameplayCore == null)
			{
				return;
			}
			int consumedOrdinal = gameplayCore.GetLatticeSlotOrdinal(donorCombatant.line, donorCombatant.lane);
			if (gameplayCore.registry != null)
			{
				gameplayCore.registry.Discard(donorCombatant, false);
			}
			donorCombatant.gameObject.SetActive(false);
			Destroy(donorCombatant.gameObject);

			worth = worth * 2;
			ReviseVitality(GetFullVitality());
			ArenaMotion(ActorArenaActivity.Fuse);
			if (announce)
			{
				PerformCue(AudioCue.Fuse);
			}
			if (consumedOrdinal != gameplayCore.GetLatticeSlotOrdinal(line, lane))
			{
				PlayMechanics.DiscardFromLineup(consumedOrdinal);
			}
			gameplayCore.DidFinishFuse(this);
		}

		public void ArenaMotion(ActorArenaActivity activity)
		{
			GuaranteeBoot();
			bool concealsFigure = activity == ActorArenaActivity.Slot || activity == ActorArenaActivity.Fuse;
			bool canTween = coreMotionRig != null && coreMotionRig.isActiveAndEnabled && coreMotionRig.runtimeAnimatorController != null;
			if (concealsFigure)
			{
				if (canTween && Figure != null)
				{
					// The clip event "ShowCharacter" (0.1667 s place / 0.1167 s merge) shows it again.
					Figure.SetActive(false);
					CancelInvoke(nameof(GuaranteeFigureOnscreen));
					Invoke(nameof(GuaranteeFigureOnscreen), 0.5f);
				}
			}
			else
			{
				PresentActor();
			}
			if (canTween)
			{
				coreMotionRig.Play(activity.ToAnimatorState());
			}
			if (concealsFigure && isActiveAndEnabled)
			{
				if (activity == ActorArenaActivity.Slot)
				{
					if (_slotEffectRoutine != null)
					{
						StopCoroutine(_slotEffectRoutine);
					}
					_slotEffectRoutine = StartCoroutine(PresentArenaEffect(activity));
				}
				else
				{
					if (_fuseEffectRoutine != null)
					{
						StopCoroutine(_fuseEffectRoutine);
					}
					_fuseEffectRoutine = StartCoroutine(PresentArenaEffect(activity));
				}
			}
		}

		private void GuaranteeFigureOnscreen()
		{
			PresentActor();
		}

		internal void Shatter()
		{
			GameObject figure = Figure;
			if (figure == null)
			{
				return;
			}
			Transform shards = _shardsPivot;
			if (shards == null || _shardParts == null)
			{
				return;
			}
#if UNITY_LUNA
			Renderer[] renderers = figure.GetComponentsInChildren<Renderer>();
			for (int idx = 0; idx < renderers.Length; idx++)
			{
				renderers[idx].enabled = false;
			}
#else
			figure.GetComponentsInChildren(false, ShatterVisualsBuffer);
			for (int idx = 0; idx < ShatterVisualsBuffer.Count; idx++)
			{
				ShatterVisualsBuffer[idx].enabled = false;
			}
			ShatterVisualsBuffer.Clear();
#endif
			shards.gameObject.SetActive(false);
			Vector3 shardsScale = shards.lossyScale;
			for (int idx = 0; idx < _shardParts.Length; idx++)
			{
				Transform shard = _shardParts[idx].piece;
				if (shard == null)
				{
					continue;
				}
				Transform anchor = _shardParts[idx].anchor;
				if (anchor == null)
				{
					shard.gameObject.SetActive(false);
					continue;
				}
				Vector3 anchorScale = anchor.lossyScale;
				shard.gameObject.SetActive(true);
				shard.SetPositionAndRotation(anchor.position, anchor.rotation);
				shard.localScale = new Vector3(anchorScale.x / shardsScale.x, anchorScale.y / shardsScale.y, anchorScale.z / shardsScale.z);
			}
			shards.gameObject.SetActive(true);
			Vector3 hub = figure.transform.position;
			System.Random scatter = new System.Random(GetInstanceID());
			for (int idx = 0; idx < _shardParts.Length; idx++)
			{
				ShardPart part = _shardParts[idx];
				if (part.piece != null && part.piece.gameObject.activeSelf)
				{
					LaunchShard(part.piece, part.body, part.hull, hub, scatter);
				}
			}
		}

		private void GatherShards()
		{
			_shardsPivot = null;
			_shardParts = null;
			GameObject figure = Figure;
			if (figure == null)
			{
				return;
			}
			Transform figurePivot = figure.transform;
			Transform shards = figurePivot.Find(ShardsTitle);
			if (shards == null)
			{
				return;
			}
			ShardPart[] parts = new ShardPart[shards.childCount];
			for (int idx = 0; idx < parts.Length; idx++)
			{
				Transform shard = shards.GetChild(idx);
				parts[idx].piece = shard;
				parts[idx].anchor = figurePivot.Find(shard.name);
				parts[idx].body = shard.GetComponent<Rigidbody>();
				parts[idx].hull = shard.GetComponentInChildren<Collider>(true);
			}
			_shardsPivot = shards;
			_shardParts = parts;
		}

		private static void LaunchShard(Transform shard, Rigidbody body, Collider hull, Vector3 hub, System.Random scatter)
		{
			bool rolls = false;
			float airtime = 0f;
			if (body != null && hull != null)
			{
				Vector3 centre = hull.bounds.center;
				Vector3 outward = centre - hub;
				outward.y = 0f;
				float swing = ShardSpread;
				if (outward.sqrMagnitude < 0.0001f)
				{
					outward = Vector3.forward;
					swing = 180f;
				}
				outward = Quaternion.Euler(0f, Mathf.Lerp(-swing, swing, (float)scatter.NextDouble()), 0f) * outward.normalized;
				float drift = Mathf.Lerp(ShardDriftFloor, ShardDriftPeak, (float)scatter.NextDouble());
				float rise = Mathf.Lerp(ShardRiseFloor, ShardRisePeak, (float)scatter.NextDouble());
				float gravity = -Physics.gravity.y;
				float drop = Mathf.Max(0f, hull.bounds.min.y);
				airtime = (rise + Mathf.Sqrt(rise * rise + 2f * gravity * drop)) / gravity;
				Vector3 sweep = outward * drift;
				Vector3 touchdown = centre + sweep * airtime;
				if (Mathf.Abs(touchdown.x) > ArenaShardHalfBreadth)
				{
					sweep.x = -sweep.x;
				}
				if (Mathf.Abs(touchdown.z) > ArenaShardHalfDepth)
				{
					sweep.z = -sweep.z;
				}
				Vector3 tumble = new Vector3((float)scatter.NextDouble() - 0.5f, (float)scatter.NextDouble() - 0.5f, (float)scatter.NextDouble() - 0.5f);
				if (tumble.sqrMagnitude < 0.0001f)
				{
					tumble = Vector3.right;
				}
				tumble = tumble.normalized * Mathf.Lerp(ShardSpinFloor, ShardSpinPeak, (float)scatter.NextDouble());
				rolls = hull is CapsuleCollider || hull is SphereCollider;
				if (hull is CapsuleCollider && sweep.sqrMagnitude > 0.0001f)
				{
					Vector3 axis = hull.transform.up;
					Vector3 bed = Vector3.Cross(Vector3.up, sweep).normalized;
					if (Vector3.Dot(axis, bed) < 0f)
					{
						bed = -bed;
					}
					Vector3 hinge = Vector3.Cross(axis, bed);
					tumble = hinge.sqrMagnitude > 0.0001f ? hinge.normalized * (Vector3.Angle(axis, bed) * Mathf.Deg2Rad / Mathf.Max(airtime, 0.1f)) : Vector3.zero;
				}
				body.maxAngularVelocity = ShardSpinCeiling;
#if !UNITY_LUNA
				body.maxDepenetrationVelocity = ShardEaseOut;
#endif
				body.velocity = new Vector3(sweep.x, rise, sweep.z);
				body.angularVelocity = tumble;
			}
			Sequence chain = DOTween.Sequence();
			chain.AppendInterval(ShardRestSpan);
			if (rolls)
			{
				chain.InsertCallback(Mathf.Min(airtime + ShardTouchdownLag, ShardRestSpan), () => RollShard(body, hull));
			}
			chain.Append(shard.DOScale(Vector3.zero, ShardFadeSpan));
			chain.OnComplete(() => shard.gameObject.SetActive(false));
			chain.SetLink(shard.gameObject);
		}

		private static void RollShard(Rigidbody body, Collider hull)
		{
			if (body == null || hull == null || body.isKinematic)
			{
				return;
			}
			Vector3 heave = body.velocity;
			Vector3 glide = new Vector3(heave.x, 0f, heave.z);
			Vector3 size = hull.transform.lossyScale;
			float radius;
			CapsuleCollider shaft = hull as CapsuleCollider;
			if (shaft != null)
			{
				Vector3 axis = hull.transform.up;
				if (Mathf.Abs(axis.y) > 0.6f)
				{
					return;
				}
				glide -= axis * Vector3.Dot(glide, axis);
				radius = shaft.radius * Mathf.Max(Mathf.Abs(size.x), Mathf.Abs(size.z));
			}
			else
			{
				SphereCollider orb = hull as SphereCollider;
				if (orb == null)
				{
					return;
				}
				radius = orb.radius * Mathf.Max(Mathf.Abs(size.x), Mathf.Max(Mathf.Abs(size.y), Mathf.Abs(size.z)));
			}
			body.angularVelocity = Vector3.Cross(Vector3.up, glide) * (ShardRollBoost / Mathf.Max(radius, 0.01f));
		}

		private IEnumerator PresentArenaEffect(ActorArenaActivity activity)
		{
			GameObject effectEntity = activity == ActorArenaActivity.Slot ? slotEffect : (activity == ActorArenaActivity.Fuse ? fuseEffect : null);
			if (effectEntity == null)
			{
				yield break;
			}
			effectEntity.SetActive(false);
			effectEntity.SetActive(true);
			yield return new WaitForSeconds(ArenaEffectSpan);
			if (effectEntity != null)
			{
				effectEntity.SetActive(false);
			}
		}

		private void PerformCue(AudioCue cue)
		{
			GameTuning.audioSignal?.Invoke(kind, cue, worth);
		}

		private void SwitchOffContour()
		{
			if (contour != null)
			{
				contour.enabled = false;
			}
		}

		private void ImpactLight()
		{
			ShiftToStratum(DemiseStratum);
			if (gameplayCore != null && gameplayCore.demiseLight != null)
			{
				Animation lightMotion = gameplayCore.demiseLight.GetComponent<Animation>();
				if (lightMotion != null)
				{
					lightMotion.enabled = true;
					lightMotion.Stop();
					lightMotion.Play();
				}
			}
			CancelInvoke(nameof(RestoreStratumAfterFlash));
			Invoke(nameof(RestoreStratumAfterFlash), DemiseLightSpan);
		}

		private void ConcealDeadVitalityMeter()
		{
			if (vitalityMeter != null && !IsLiving)
			{
				vitalityMeter.SetActive(false);
			}
		}

		private void RestoreStratumAfterFlash()
		{
			ShiftToStratum(_basisStratum);
		}

		public void ShiftBackStratum()
		{
			_basisStratum = CombatantStratum;
			ShiftToStratum(CombatantStratum);
		}

		protected override void SenseOutline()
		{
			int variant = _chosen ? 1 : hoverContour;
			if (variant == _imposedContour)
			{
				return;
			}
			_imposedContour = variant;
			if (variant == 0)
			{
				SwitchOffContour();
				return;
			}
			if (contour == null)
			{
				if (meshEntity == null)
				{
					return;
				}
				contour = meshEntity.GetComponent<Outline>();
				if (contour == null)
				{
					contour = meshEntity.AddComponent<Outline>();
				}
				contour.OutlineMode = Outline.Mode.OutlineVisible;
				contour.OutlineWidth = contourBreadth * GameTuning.arenaContourScale;
			}
			contour.OutlineColor = variant == 2 ? failureTint : choiceTint;
			contour.enabled = true;
		}

		internal void SetFuseObjective(Combatant candidate)
		{
			if (fuseObjective != null && fuseObjective != candidate)
			{
				fuseObjective.hoverContour = 0;
			}
			fuseObjective = candidate;
			if (candidate == null)
			{
				return;
			}
			if (gameplayCore != null && gameplayCore.registry != null && gameplayCore.registry.VerifyFuse(this, candidate))
			{
				candidate.hoverContour = 1;
			}
			else if (candidate.faction == faction && candidate.kind == kind && candidate.worth == worth && worth >= PeakTroopWorth)
			{
				candidate.hoverContour = 2;
			}
			else
			{
				candidate.hoverContour = 0;
			}
		}

		public void Immobilize()
		{
			if (!IsLiving)
			{
				return;
			}
			HaltHunt();
			if (physicsBody != null && !physicsBody.isKinematic)
			{
				physicsBody.velocity = Vector3.zero;
			}
			actorStatus = ActorStatus.Frozen;
		}

		public void Thaw()
		{
			if (actorStatus == ActorStatus.Frozen)
			{
				actorStatus = ActorStatus.Resting;
			}
		}

		public void ShiftToSpotlight()
		{
			_basisStratum = SpotlightStratum;
			ShiftToStratum(SpotlightStratum);
		}

		private void ShiftToStratum(int stratumOrdinal)
		{
			if (core != null)
			{
				SetStratumRecursively(core, stratumOrdinal);
			}
		}

		private void SetStratumRecursively(Transform pivot, int stratumOrdinal)
		{
			if (pivot.gameObject.layer == DebrisStratum)
			{
				return;
			}
			pivot.gameObject.layer = stratumOrdinal;
			for (int idx = 0; idx < pivot.childCount; idx++)
			{
				SetStratumRecursively(pivot.GetChild(idx), stratumOrdinal);
			}
		}

		public void Jolt()
		{
			shakeSpan = standardShakeSpan;
		}

		private void OnCollisionStay(Collision crash)
		{
			if (!IsLiving || crash.collider == null)
			{
				return;
			}
			Combatant counterpart = ResolveContactOwner(crash.collider);
			if (counterpart == null || counterpart == this || counterpart.faction == faction || !counterpart.IsLiving)
			{
				return;
			}
			isContactingRival = true;
			_contactingCombatant = counterpart;
			// Measured: a melee unit blocked by an enemy that is not its target stops and keeps attacking its CURRENT
			// target (the bite lands on it even from a distance); it only switches when the blocker's bite lands on it.
		}

		private void OnCollisionExit(Collision crash)
		{
			Combatant counterpart = crash.collider != null ? ResolveContactOwner(crash.collider) : null;
			if (counterpart == null || counterpart == _contactingCombatant)
			{
				isContactingRival = false;
				_contactingCombatant = null;
			}
		}

		private Combatant ResolveContactOwner(Collider hull)
		{
			int key = hull.GetInstanceID();
			if (!_contactOwners.TryGetValue(key, out Combatant owner))
			{
				owner = hull.GetComponentInParent<Combatant>();
				_contactOwners.Add(key, owner);
			}
			return owner;
		}

		public void Halt()
		{
			HaltHunt();
			objective = null;
			_obstructedByAlly = false;
			_heldBySprinterTicks = 0;
			_obstructingAlly = null;
			isContactingRival = false;
			_contactingCombatant = null;
			_pendingAssaultVariant = false;
			if (IsLiving)
			{
				actorStatus = ActorStatus.Resting;
			}
			if (physicsBody != null && !physicsBody.isKinematic)
			{
				physicsBody.velocity = Vector3.zero;
			}
		}

		private void FaceObjective()
		{
			if (objective == null)
			{
				return;
			}
			Vector3 heading = objective.transform.position - transform.position;
			heading.y = 0f;
			if (heading.sqrMagnitude < 0.000001f)
			{
				return;
			}
			Quaternion facing = Quaternion.LookRotation(heading);
			transform.rotation = Quaternion.Slerp(transform.rotation, facing, 10f * Time.deltaTime);
		}

		private void FixedUpdate()
		{
			if (actorStatus != ActorStatus.Sprint || !IsCloseCombat || !IsLiving || !IsViableObjective(objective))
			{
				return;
			}
			if (gameplayCore == null || gameplayCore.activity != ActivityStatus.Assaulting || IsTouching(objective, 1f) || isContactingRival)
			{
				return;
			}
			Vector3 location = physicsBody != null ? physicsBody.position : transform.position;
			Vector3 heading = objective.transform.position - location;
			heading.y = 0f;
			if (heading.sqrMagnitude < 0.000001f)
			{
				return;
			}
			Vector3 upcoming = location + heading.normalized * (runPace * MotionPaceScalar * Time.fixedDeltaTime);
			Combatant comrade = ObstructingAlly(location, upcoming);
			if (comrade == null)
			{
				_heldBySprinterTicks = 0;
			}
			else
			{
				// A standing ally blocks for good (measured).
				if (comrade.actorStatus != ActorStatus.Sprint)
				{
					_obstructedByAlly = true;
					_obstructingAlly = comrade;
					_heldBySprinterTicks = 0;
					return;
				}
				// A running ally ahead holds us back for a few steps. When it still blocks after that (two units running
				// at the same back-row target hold each other forever otherwise), slide along its collider at full speed.
				if (++_heldBySprinterTicks <= SprinterHoldTicks)
				{
					return;
				}
				Vector3 glided = GlideAround(comrade, location, upcoming, objective.transform.position);
				Combatant counterpart = ObstructingAlly(location, glided);
				if (counterpart == null)
				{
					upcoming = glided;
				}
				else if (counterpart.actorStatus != ActorStatus.Sprint)
				{
					_obstructedByAlly = true;
					_obstructingAlly = counterpart;
					_heldBySprinterTicks = 0;
					return;
				}
				else if (_heldBySprinterTicks < SprinterPassTicks)
				{
					return;
				}
				// Still wedged between runners after SprinterPassTicks: take the step (allies never collide with each other).
			}
			// Fighter.prefab's Rigidbody is FreezeAll (constraints 126) and nothing unfreezes it, so MovePosition would not
			// move the unit: melee units are moved through the transform, the body just follows.
			transform.position = upcoming;
			if (physicsBody != null)
			{
				physicsBody.position = upcoming;
			}
			FaceObjective();
			latestRunningLoc = upcoming;
		}

		// The measured stop lasts as long as the blocking ally stands next to us (the raptor behind its fighting Triceramon).
		private bool BarrierStillStanding()
		{
			Combatant comrade = _obstructingAlly;
			if (comrade == null || !comrade.IsLiving || comrade.actorStatus == ActorStatus.Sprint)
			{
				return false;
			}
			Vector3 difference = comrade.transform.position - transform.position;
			difference.y = 0f;
			float limit = (HullRadius(this) + HullRadius(comrade)) * 1.1f;
			return difference.sqrMagnitude <= limit * limit;
		}

		// The step with the component pointing into the ally removed, rescaled to the full step length. A tangent step never
		// gets closer to that ally, so it passes ObstructingAlly for it. Head-on, sidestep to the side nearer the target.
		private Vector3 GlideAround(Combatant comrade, Vector3 location, Vector3 upcoming, Vector3 objectiveLocation)
		{
			Vector3 stride = upcoming - location;
			Vector3 perpendicular = comrade.transform.position - location;
			perpendicular.y = 0f;
			if (perpendicular.sqrMagnitude < 0.000001f)
			{
				return upcoming;
			}
			perpendicular.Normalize();
			Vector3 tangential = stride - perpendicular * Vector3.Dot(stride, perpendicular);
			if (tangential.sqrMagnitude < stride.sqrMagnitude * 0.0001f)
			{
				Vector3 lateral = Vector3.Cross(Vector3.up, perpendicular);
				float toObjective = Vector3.Dot(objectiveLocation - location, lateral);
				if (Mathf.Abs(toObjective) < 0.0001f)
				{
					toObjective = (GetInstanceID() & 1) == 0 ? 1f : -1f;
				}
				tangential = toObjective >= 0f ? lateral : -lateral;
			}
			return location + tangential.normalized * stride.magnitude;
		}

		private Combatant ObstructingAlly(Vector3 location, Vector3 upcoming)
		{
			if (gameplayCore == null || gameplayCore.registry == null)
			{
				return null;
			}
			List<Combatant> comrades = faction == ActorFaction.Guardian ? gameplayCore.registry.guardians : faction == ActorFaction.Foe ? gameplayCore.registry.foes : null;
			if (comrades == null)
			{
				return null;
			}
			for (int idx = 0; idx < comrades.Count; idx++)
			{
				Combatant comrade = comrades[idx];
				if (comrade == null || comrade == this || !comrade.IsLiving)
				{
					continue;
				}
				Vector3 nowGap = comrade.transform.position - location;
				Vector3 thenGap = comrade.transform.position - upcoming;
				nowGap.y = 0f;
				thenGap.y = 0f;
				float limit = HullRadius(this) + HullRadius(comrade);
				// Blocked when the step would push into the ally's collider (moving away or sideways stays free).
				if (thenGap.sqrMagnitude < limit * limit && thenGap.sqrMagnitude < nowGap.sqrMagnitude)
				{
					return comrade;
				}
			}
			return null;
		}

		protected override void Update()
		{
			base.Update();
			if (shakeSpan > 0f && core != null)
			{
				shakeSpan -= Time.deltaTime * shakeDecay;
				core.localPosition = shakeSpan > 0f ? Random.insideUnitSphere * shakeMagnitude : Vector3.zero;
			}
			if (vitalityMeter != null && vitalityMeter.activeSelf)
			{
				if (vitalityMeterLocation != null)
				{
					vitalityMeter.transform.position = VitalityMeterWorldLocation;
				}
				vitalityMeter.transform.rotation = VitalityMeterRotation;
			}
		}
	}
}
