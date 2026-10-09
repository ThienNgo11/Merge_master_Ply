using System;
using System.Collections.Generic;
using UnityEngine;

namespace Funlab.MergeMaster
{
	// Economy (coins, gems, unit prices, earnings), board formation, unlocks and win/loss statistics.
	public static class PlayMechanics
	{
		// 15 codes (index = ranger grid row*5+col): (int)type * 10000 + value, 0 = empty.
		public static int[] lineup = new int[15];

		public static readonly int[] beginLineup = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (int)ActorKind.Saurian * 10000 + 1, 0, 0 };

		// Highest owned unit VALUE per type (0 = no unit of that type owned yet; fresh save: Dino 1, Human 0).
		public static Dictionary<ActorKind, int> peakUnsealedActors = new Dictionary<ActorKind, int>
		{
			{ ActorKind.Person, 0 },
			{ ActorKind.Saurian, 1 }
		};

		public static Action onConjureMarketNoticePendingAltered;

		private static int _terminatedFoesThisStage;

		private static Action<long> m_MoneyAltered;

		private static Action<long> m_JewelsAltered;

		private static Action<int> m_StonesAltered;

		// ---- Measured economy constants of the original (save-file measurements, PL = AdvancementStage) ----

		// Win earning ("YOU EARNED") = StageIncomeBasis * PL^4 (40, 640, 3240, 10240 …).
		public const long StageIncomeBasis = 40L;

		// "Level Completed Bonus xN": a win that advances the AdvancementStage (level prefab bypassStageAdvancement = 0,
		// e.g. L2, L5, L7 …) pays peakStageIncome * N (L2: 40 x10 = 400, L5: 640 x10 = 6400).
		public const int StageFinishBoonCoefficient = 10;

		// Unit price per type = (TroopFeeBasis + TroopFeeIncrement * n) * PL^4, n = paid purchases of that type so far.
		public const long TroopFeeBasis = 2L;

		public const long TroopFeeIncrement = 8L;

		// Unit stats: stat(t) = round(base * 2^(growth * (t - 1))), tier t = 1..11, value = 2^(t - 1).
		public const double SaurianVitalityBasis = 20.0;

		public const double SaurianAssaultBasis = 3.0;

		public const double SaurianMetricGrowth = 1.24;

		public const double PersonVitalityBasis = 8.0;

		public const double PersonAssaultBasis = 4.0;

		public const double PersonMetricGrowth = 1.25;

		// Treasure gems ("TREASURE FOUND") drop only on wins from this level on (none on L1, none on a fail).
		public const int HoardJewelsBeginStage = 2;

		public const long BeginningMoney = 0L;

		// Running earnings of the current fight (always round(peakStageIncome * dealtEnemyDamageFraction)).
		// At level end: win = peakStageIncome (x bonus), fail = round(peakStageIncome * totalEnemyDamageFraction).
		public static long stageProceeds;

		public static float foeBeginVitality;

		public static float guardianBeginVitality;

		// peakStageIncome at PL 1 = 40.
		public static readonly long peakStageIncomeBegin = StageIncomeBasis;

		private static Dictionary<int, long> peakStageProceeds = new Dictionary<int, long>();

		// Coins per point of damage dealt by a player unit (set by GameplayCore at fight start; fallback only,
		// GetImpactIncome distributes peakStageIncome exactly over the enemy start health).
		public static float saurianIncomePerImpact;

		// Price of the very first unit of a type (n = 0, PL = 1) = 2.
		public static readonly long actorChargeBaselineWorth = TroopFeeBasis;

		// Tier-1 stats (Vraptormon 20 HP / 3 ATK, Blackhawk 8 HP / 4 ATK). Use GetTroopVitality / GetTroopAssault for any tier.
		public static readonly int saurianBaselineVitality = 20;

		public static readonly int personBaselineVitality = 8;

		public static readonly int saurianHarmPerImpact = 3;

		public static readonly int personHarmPerImpact = 4;

		public static readonly float guardianAssaultBeginStandardPause = 0f; // measured: units start running/shooting on the START FIGHT frame

		public static float guardianAssaultBeginPause = 0f;

		public static ActorKind latestFusedActor;

		private static int tallyOfTroopAcquisitions;

		private static List<string> impactTrace = new List<string>();

		// [NEW] private state.
		// PL is clamped here so PL^4 * price factors can never overflow a long.
		private const int PeakAdvancementForMath = 20000;

		private static bool _isBooted;

		// Paid purchases per type (tutorial purchases included; RV / summon / free units excluded).
		private static int _personAcquisitionTally;

		private static int _saurianAcquisitionTally;

		// Level-end state of the current level (reset by BeginStage()).
		private static double _foeHarmDealt;

		private static int _performedStage = 1;

		private static StageOutcome _stageOutroOutcome = StageOutcome.Unset;

		private static int _stageOutroBoonCoefficient = 1;

		private static bool _stageOutroCredited;

		private static bool _creditOutstanding;

		private static bool _creditResolved;

		private static long _creditedProceeds;

		public static ActorKind latestUnsealedActorKind
		{
			get => GameData.Instance.LatestUnsealedActorKind;
			set => GameData.Instance.LatestUnsealedActorKind = value;
		}

		public static bool conjureMarketNoticePending
		{
			get => GameData.Instance.ConjureMarketNoticePending;
			set
			{
				GameData gameData = GameData.Instance;
				bool altered = gameData.ConjureMarketNoticePending != value;
				gameData.ConjureMarketNoticePending = value;
				if (altered)
				{
					onConjureMarketNoticePendingAltered?.Invoke();
				}
			}
		}

		public static int terminatedFoesThisStage
		{
			get
			{
				return _terminatedFoesThisStage;
			}
			set
			{
				_terminatedFoesThisStage = Mathf.Max(0, value);
				// GameplayCore zeroes the kill counter in FetchStage and at fight start: a new level/fight begins.
				if (_terminatedFoesThisStage == 0)
				{
					BeginStage();
				}
			}
		}

		public const string CoinResource = "Coin";

		public const string JewelResource = "Diamond";

		public static void TraceResource(bool isEarned, string location, string itemType, long amount)
		{
		}

		private static string StageOutroLocation(bool viaRoulette)
		{
			if (_stageOutroOutcome == StageOutcome.Accomplishment)
			{
				return viaRoulette ? "spin_reward_victory" : "victory_popup";
			}
			return viaRoulette ? "spin_reward_lose" : "lose_popup";
		}

		public static long money
		{
			get
			{
				GuaranteeBoot();
				return GameData.Instance.Money;
			}
			set
			{
				GuaranteeBoot();
				long clamped = Math.Max(0L, value);
				GameData gameData = GameData.Instance;
				if (clamped == gameData.Money)
				{
					return;
				}
				gameData.Money = clamped;
				m_MoneyAltered?.Invoke(clamped);
			}
		}

		public static int stones
		{
			get => GameData.Instance.Stones;
			set
			{
				GameData.Instance.Stones = value;
				m_StonesAltered?.Invoke(value);
			}
		}

		public static long jewels
		{
			get
			{
				GuaranteeBoot();
				return GameData.Instance.Jewels;
			}
			set
			{
				GuaranteeBoot();
				long clamped = Math.Max(0L, value);
				GameData gameData = GameData.Instance;
				if (clamped == gameData.Jewels)
				{
					return;
				}
				gameData.Jewels = clamped;
				m_JewelsAltered?.Invoke(clamped);
			}
		}

		// Win earning multiplier of the level that just ended: StageFinishBoonCoefficient (10) when that win advanced
		// the AdvancementStage ("Level Completed Bonus x10" line on the victory screen), else 1.
		// Valid from DidFinishStage() until the next level starts; 1 otherwise.
		public static int stageFinishIncomeCoefficient => _stageOutroOutcome == StageOutcome.Accomplishment ? Math.Max(1, _stageOutroBoonCoefficient) : 1;

		// True when the victory screen must show the yellow "Level Completed Bonus x{levelCompleteEarningMultiplier}" line.
		public static bool stageFinishBoonApplies => stageFinishIncomeCoefficient > 1;

		// The level number the current fight / level end belongs to (GameTuning.level before a win advanced it).
		public static int latestPerformedStage
		{
			get
			{
				GuaranteeBoot();
				return _performedStage;
			}
		}

		// Result of the level end in progress (None while on the board or fighting).
		public static StageOutcome stageOutroOutcome => _stageOutroOutcome;

		// True once this level's YOU EARNED has been added to gold (at level end).
		public static bool stageProceedsCredited => _stageOutroCredited;

		// Header coin counter: during the fight gold + running earnings (cosmetic); after the level end the real gold.
		public static long renderedMoney => _stageOutroCredited ? money : money + Math.Max(0L, stageProceeds);

		// Fraction of the enemies' start health destroyed by player hits in the current fight (0..1).
		public static float foeHarmPortion => foeBeginVitality > 0f ? Mathf.Clamp01((float)(_foeHarmDealt / foeBeginVitality)) : 0f;

		public static long peakStageIncome => GetPeakStageIncome(GameTuning.advancementStage);

		public static float personIncomePerImpact => saurianIncomePerImpact;

		// Next price of a warrior: (2 + 8 * personAcquisitionTally) * PL^4.
		public static long personCharge
		{
			get
			{
				GuaranteeBoot();
				return GetTroopFee(_personAcquisitionTally, GameTuning.advancementStage);
			}
			set
			{
				GuaranteeBoot();
				_personAcquisitionTally = AcquisitionTallyForFee(value, GameTuning.advancementStage);
				PersistAcquisitionTallies();
			}
		}

		// Next price of a dino: (2 + 8 * saurianAcquisitionTally) * PL^4.
		public static long saurianCharge
		{
			get
			{
				GuaranteeBoot();
				return GetTroopFee(_saurianAcquisitionTally, GameTuning.advancementStage);
			}
			set
			{
				GuaranteeBoot();
				_saurianAcquisitionTally = AcquisitionTallyForFee(value, GameTuning.advancementStage);
				PersistAcquisitionTallies();
			}
		}

		// Paid purchases of warriors so far (tutorial purchase included).
		public static int personAcquisitionTally
		{
			get
			{
				GuaranteeBoot();
				return _personAcquisitionTally;
			}
		}

		// Paid purchases of dinos so far (tutorial purchase included).
		public static int saurianAcquisitionTally
		{
			get
			{
				GuaranteeBoot();
				return _saurianAcquisitionTally;
			}
		}

		public static int universalTriumphTally
		{
			get => GameData.Instance.VictoryCount;
			set => GameData.Instance.VictoryCount = value;
		}

		public static int universalDefeatTally
		{
			get => GameData.Instance.LoseCount;
			set => GameData.Instance.LoseCount = value;
		}

		public static event Action<long> MoneyAltered
		{
			add
			{
				m_MoneyAltered += value;
			}
			remove
			{
				m_MoneyAltered -= value;
			}
		}

		public static event Action<int> StonesAltered
		{
			add
			{
				m_StonesAltered += value;
			}
			remove
			{
				m_StonesAltered -= value;
			}
		}

		public static event Action<long> JewelsAltered
		{
			add
			{
				m_JewelsAltered += value;
			}
			remove
			{
				m_JewelsAltered -= value;
			}
		}

		public static void Boot()
		{
			if (_isBooted)
			{
				return;
			}
			_isBooted = true;
			GameTuning.Boot();
			GameData gameData = GameData.Instance;

			_personAcquisitionTally = Math.Max(0, gameData.PersonAcquisitionTally);
			_saurianAcquisitionTally = Math.Max(0, gameData.SaurianAcquisitionTally);
			_performedStage = GameTuning.stage;

			int[] persistedLineup = gameData.Lineup;
			if (persistedLineup == null || persistedLineup.Length != beginLineup.Length)
			{
				lineup = (int[])beginLineup.Clone();
				PersistLineup();
			}
			else
			{
				lineup = persistedLineup;
				for (int idx = 0; idx < lineup.Length; idx++)
				{
					if (lineup[idx] < 0 || lineup[idx] % 10000 == 0)
					{
						lineup[idx] = 0;
					}
				}
			}

			int peakPerson = PeakLineupWorth(ActorKind.Person);
			int peakSaurian = PeakLineupWorth(ActorKind.Saurian);
			int[] persistedUnseals = gameData.PeakUnsealedActors;
			bool hasPersistedUnseals = persistedUnseals != null && persistedUnseals.Length >= 2;
			if (hasPersistedUnseals)
			{
				peakPerson = Math.Max(peakPerson, persistedUnseals[0]);
				peakSaurian = Math.Max(peakSaurian, persistedUnseals[1]);
			}
			// Unlocked tiers are derived from the units owned (formation) plus merges already recorded; a fresh save
			// (one Vraptormon) has the Dino tier 1 unlocked and no warrior tier until the first warrior is bought.
			peakUnsealedActors = new Dictionary<ActorKind, int>
			{
				{ ActorKind.Person, Math.Max(0, peakPerson) },
				{ ActorKind.Saurian, Math.Max(0, peakSaurian) }
			};
			if (!hasPersistedUnseals)
			{
				PersistPeakUnsealedActors();
			}

			guardianAssaultBeginPause = guardianAssaultBeginStandardPause;
			impactTrace = new List<string>();
			peakStageProceeds = new Dictionary<int, long>();
		}

		// Win. Called (via UIDirector.DidFinishStage) right after GameplayCore set stageProceeds and before the level-end UI.
		// YOU EARNED = 40 * PL^4 (PL of the level just won) x10 when this win advances the AdvancementStage.
		// StageLayout +1 always; AdvancementStage +1 unless the level prefab has bypassStageAdvancement (L1, L3, L4, L6 …).
		public static void DidFinishStage(bool bypassStageAdvancement)
		{
			GuaranteeBoot();
			int performed = GameTuning.stage;
			int advancement = GameTuning.advancementStage;
			_performedStage = performed;
			_stageOutroOutcome = StageOutcome.Accomplishment;
			_stageOutroBoonCoefficient = bypassStageAdvancement ? 1 : StageFinishBoonCoefficient;
			stageProceeds = GetStageTriumphIncome(advancement, !bypassStageAdvancement);
			universalTriumphTally++;
			GameTuning.stage = performed + 1;
			if (!bypassStageAdvancement)
			{
				GameTuning.advancementStage = advancement + 1;
				// Stored next prices follow the new PL (cost scales with (PLnew / PLold)^4).
				PersistAcquisitionTallies();
			}
		}

		// StageLayout end (win or fail), called from GameTuning.ReviseInterlude at the end of every fight after stageProceeds is final.
		// Fail: YOU EARNED = round(40 * PL^4 * totalEnemyDamageFraction). Then the earnings are credited to gold at once.
		public static void OnStageConcluded(StageOutcome outcome)
		{
			GuaranteeBoot();
			if (outcome == StageOutcome.Unsuccessful)
			{
				_performedStage = GameTuning.stage;
				_stageOutroOutcome = StageOutcome.Unsuccessful;
				_stageOutroBoonCoefficient = 1;
				if (foeBeginVitality > 0f)
				{
					stageProceeds = GetDefeatIncome(foeHarmPortion);
				}
			}
			else if (outcome == StageOutcome.Accomplishment && _stageOutroOutcome != StageOutcome.Accomplishment)
			{
				// Win reported without DidFinishStage (no UIDirector): keep Engine's earning.
				_performedStage = GameTuning.stage;
				_stageOutroOutcome = StageOutcome.Accomplishment;
				_stageOutroBoonCoefficient = 1;
			}
			CreditStageProceeds();
		}

		// Adds this level's YOU EARNED to gold (once per level end). The wheel extra is added later by PersistProceeds(m).
		public static void CreditStageProceeds()
		{
			GuaranteeBoot();
			if (_stageOutroCredited)
			{
				return;
			}
			_stageOutroCredited = true;
			_creditedProceeds = Math.Max(0L, stageProceeds);
			_creditOutstanding = true;
			_creditResolved = false;
			money += _creditedProceeds;
			TraceResource(true, StageOutroLocation(false), CoinResource, _creditedProceeds);
		}

		// CLAIM (wheel multiplier m) / No Thanks (m = 1). YOU EARNED was already credited at level end, so only the
		// extra (m - 1) x YOU EARNED is added: total = YOU EARNED x m. Without a prior credit it adds YOU EARNED x m.
		public static void PersistProceeds(int coefficient = 1)
		{
			GuaranteeBoot();
			long coef = Math.Max(1, coefficient);
			if (_creditOutstanding)
			{
				_creditOutstanding = false;
				_creditResolved = true;
				if (coef > 1)
				{
					long bonus = GuardedMultiply(_creditedProceeds, coef - 1);
					money += bonus;
					TraceResource(true, StageOutroLocation(true), CoinResource, bonus);
				}
				return;
			}
			if (_creditResolved)
			{
				return;
			}
			long proceeds = GuardedMultiply(Math.Max(0L, stageProceeds), coef);
			money += proceeds;
			TraceResource(true, StageOutroLocation(coef > 1), CoinResource, proceeds);
		}

		public static void PersistJewels(int jewelsAmount)
		{
			jewels += jewelsAmount;
			TraceResource(true, "victory_popup", JewelResource, jewelsAmount);
		}

		// Treasure gems of a level end: gem_drops_min..gem_drops_max (1..3) on a win from level 2 on, else 0.
		public static int DrawHoardJewels(bool accomplished)
		{
			if (!accomplished || !GameTuning.JewelsAllowed || latestPerformedStage < HoardJewelsBeginStage)
			{
				return 0;
			}
			int floor = Math.Max(0, RemoteTuning.jewel_drops_floor);
			int peak = Math.Max(floor, RemoteTuning.jewel_drops_peak);
			return UnityEngine.Random.Range(floor, peak + 1);
		}

		// Starts a new level/fight: clears the per-level economy state (GameplayCore triggers it via terminatedFoesThisStage = 0).
		public static void BeginStage()
		{
			_foeHarmDealt = 0.0;
			_stageOutroOutcome = StageOutcome.Unset;
			_stageOutroBoonCoefficient = 1;
			_stageOutroCredited = false;
			_creditResolved = false;
			if (_isBooted)
			{
				_performedStage = GameTuning.stage;
			}
		}

		// Max HP of a fighter (player or enemy, same formula).
		public static int GetVitality(Combatant combatant)
		{
			if (combatant == null)
			{
				return 0;
			}
			return GetTroopVitality(combatant.kind, combatant.worth);
		}

		// Damage per hit of a fighter (player or enemy, same formula; no level-1 reduction).
		public static int GetHarmPerImpact(Combatant combatant)
		{
			if (combatant == null)
			{
				return 0;
			}
			return GetTroopAssault(combatant.kind, combatant.worth);
		}

		// HP of a unit VALUE (1, 2, 4 … 1024): Dino round(20 * 2^(1.24 (t-1))), Warrior round(8 * 2^(1.25 (t-1))).
		public static int GetTroopVitality(ActorKind kind, int worth)
		{
			return ComputeMetric(IsPersonMetrics(kind) ? PersonVitalityBasis : SaurianVitalityBasis, MetricGrowth(kind), worth);
		}

		// ATK of a unit VALUE: Dino round(3 * 2^(1.24 (t-1))), Warrior round(4 * 2^(1.25 (t-1))).
		public static int GetTroopAssault(ActorKind kind, int worth)
		{
			return ComputeMetric(IsPersonMetrics(kind) ? PersonAssaultBasis : SaurianAssaultBasis, MetricGrowth(kind), worth);
		}

		// Same stats by tier (1..11).
		public static int GetTroopVitalityForGrade(ActorKind kind, int grade)
		{
			return GetTroopVitality(kind, GradeToWorth(grade));
		}

		public static int GetTroopAssaultForGrade(ActorKind kind, int grade)
		{
			return GetTroopAssault(kind, GradeToWorth(grade));
		}

		// Coins earned by a player unit for dealing `damage` (effective damage, ≤ the target's remaining HP).
		// peakStageIncome is spread exactly over the enemies' start health: after any hit the level's running earnings are
		// round(peakStageIncome * dealtDamage / foeBeginVitality), so a fail pays round(40 PL^4 * damageFraction) and
		// killing every enemy pays exactly peakStageIncome. GameplayCore adds the result to stageProceeds.
		public static int GetImpactIncome(Combatant combatant, float harm)
		{
			if (combatant == null || combatant.faction != ActorFaction.Guardian || harm <= 0f)
			{
				return 0;
			}
			long peak = peakStageIncome;
			long income;
			if (foeBeginVitality > 0f)
			{
				double begin = foeBeginVitality;
				long prior = IncomeForHarm(peak, _foeHarmDealt, begin);
				_foeHarmDealt = Math.Min(begin, _foeHarmDealt + harm);
				long subsequent = IncomeForHarm(peak, _foeHarmDealt, begin);
				income = subsequent - prior;
			}
			else
			{
				float incomeRate = combatant.kind == ActorKind.Person ? personIncomePerImpact : saurianIncomePerImpact;
				income = (long)Math.Round(harm * (double)incomeRate, MidpointRounding.AwayFromZero);
			}
			long headroom = peak - stageProceeds;
			if (income > headroom)
			{
				income = headroom;
			}
			return (int)Math.Min(int.MaxValue, Math.Max(0L, income));
		}

		// YOU EARNED on a fail: round(peakStageIncome * totalEnemyDamageFraction).
		public static long GetDefeatIncome(float overallFoeHarmPortion)
		{
			double portion = Math.Max(0.0, Math.Min(1.0, overallFoeHarmPortion));
			return (long)Math.Round(peakStageIncome * portion, MidpointRounding.AwayFromZero);
		}

		// YOU EARNED on a win at the given PL: 40 * PL^4, x10 when the win advances the AdvancementStage.
		public static long GetStageTriumphIncome(int advancement, bool raisesAdvancement)
		{
			long income = GetPeakStageIncome(advancement);
			return raisesAdvancement ? GuardedMultiply(income, StageFinishBoonCoefficient) : income;
		}

		public static void ReviseLineup(int lineupOrdinal, ActorKind kind, int worth)
		{
			if (!IsValidLineupOrdinal(lineupOrdinal))
			{
				return;
			}
			lineup[lineupOrdinal] = kind == ActorKind.Unset || worth <= 0 ? 0 : (int)kind * 10000 + worth;
			PersistLineup();
		}

		public static void DiscardFromLineup(int lineupOrdinal)
		{
			if (!IsValidLineupOrdinal(lineupOrdinal))
			{
				return;
			}
			lineup[lineupOrdinal] = 0;
			PersistLineup();
		}

		private static void PersistLineup()
		{
			GameData.Instance.Lineup = lineup;
		}

		public static void ReviseUnsealActors(ActorKind kind, int worth)
		{
			if (kind != ActorKind.Person && kind != ActorKind.Saurian)
			{
				return;
			}
			GuaranteeBoot();
			peakUnsealedActors.TryGetValue(kind, out int activePeak);
			if (worth <= activePeak)
			{
				return;
			}
			peakUnsealedActors[kind] = worth;
			PersistPeakUnsealedActors();
			latestUnsealedActorKind = kind;
			conjureMarketNoticePending = true;
		}

		public static long GetCharge(ActorKind kind)
		{
			switch (kind)
			{
				case ActorKind.Person:
					return personCharge;
				case ActorKind.Saurian:
					return saurianCharge;
				default:
					return long.MaxValue;
			}
		}

		public static bool CanAffordAcquisition(ActorKind kind)
		{
			return money >= GetCharge(kind);
		}

		// Coin purchases only, tutorial purchases included (RV / IAP / summon / free awards do not pay and do not raise the price).
		public static void DidAcquireActor(ActorKind kind)
		{
			long charge = GetCharge(kind);
			if (charge == long.MaxValue)
			{
				return;
			}
			money -= charge;
			TraceResource(false, kind == ActorKind.Person ? "buy_human" : "buy_dino", CoinResource, charge);
			tallyOfTroopAcquisitions++;
			BoostTroopFee(kind);
		}

		// Unit price for the n-th paid purchase (n = purchases so far) at a AdvancementStage: (2 + 8n) * PL^4.
		// PL 1: 2, 10, 18 …; PL 2: 32, 160, 288, 416, 544 …; PL 3: 162, 810, 1458 … 2754, 3402 …
		public static long GetTroopFee(int acquisitionTally, int advancement)
		{
			long scalar = TroopFeeBasis + TroopFeeIncrement * Math.Max(0, acquisitionTally);
			return GuardedMultiply(scalar, AdvancementPow4(advancement));
		}

		public static int GetAcquisitionTally(ActorKind kind)
		{
			GuaranteeBoot();
			switch (kind)
			{
				case ActorKind.Person:
					return _personAcquisitionTally;
				case ActorKind.Saurian:
					return _saurianAcquisitionTally;
				default:
					return 0;
			}
		}

		// n++ for that type: the next price rises by 8 * PL^4.
		private static void BoostTroopFee(ActorKind kind)
		{
			GuaranteeBoot();
			if (kind == ActorKind.Person)
			{
				_personAcquisitionTally++;
			}
			else if (kind == ActorKind.Saurian)
			{
				_saurianAcquisitionTally++;
			}
			else
			{
				return;
			}
			PersistAcquisitionTallies();
		}

		// Unit value -> level (1 -> 1, 2 -> 2, 4 -> 3, ..., 1024 -> 11; 31 -> 5).
		public static int GetActorGrade(int worth)
		{
			if (worth <= 0)
			{
				return 0;
			}
			int stage = 0;
			while (worth > 0)
			{
				stage++;
				worth >>= 1;
			}
			return stage;
		}

		// Card stats by tier/level 1..11 (same values as the fighters). Format with Toolbox.FormatStatNumber.
		public static int GetActorTileVitality(ActorKind kind, int stage)
		{
			return GetTroopVitalityForGrade(kind, stage);
		}

		public static int GetActorTileAssault(ActorKind kind, int stage)
		{
			return GetTroopAssaultForGrade(kind, stage);
		}

		// Enemy list of a level, grouped by type (index order). Skips empty slots (type None or value <= 0).
		// targetValue > 0 scales every value so the total matches it (endless mode / enemy power curve).
		public static Dictionary<ActorKind, LatticePieceWorth[]> ConstructStage(LatticePiece[] pieces, float objectiveWorth)
		{
			List<LatticePieceWorth> worths = new List<LatticePieceWorth>();
			if (pieces != null)
			{
				for (int idx = 0; idx < pieces.Length; idx++)
				{
					LatticePiece piece = pieces[idx];
					if (piece == null || piece.kind == ActorKind.Unset || piece.worth <= 0)
					{
						continue;
					}
					worths.Add(new LatticePieceWorth { ordinal = idx, worth = piece.worth, kind = piece.kind });
				}
			}
			if (objectiveWorth > 0f && worths.Count > 0)
			{
				float runningTotal = 0f;
				foreach (LatticePieceWorth pieceWorth in worths)
				{
					runningTotal += pieceWorth.worth;
				}
				if (runningTotal > 0f)
				{
					float scalar = objectiveWorth / runningTotal;
					for (int idx = 0; idx < worths.Count; idx++)
					{
						LatticePieceWorth pieceWorth = worths[idx];
						pieceWorth.worth = Mathf.Max(1, Mathf.RoundToInt(pieceWorth.worth * scalar));
						worths[idx] = pieceWorth;
					}
				}
			}
			PrintWorths(worths);
			Dictionary<ActorKind, List<LatticePieceWorth>> clustered = new Dictionary<ActorKind, List<LatticePieceWorth>>();
			foreach (LatticePieceWorth pieceWorth in worths)
			{
				if (!clustered.TryGetValue(pieceWorth.kind, out List<LatticePieceWorth> series))
				{
					series = new List<LatticePieceWorth>();
					clustered.Add(pieceWorth.kind, series);
				}
				series.Add(pieceWorth);
			}
			Dictionary<ActorKind, LatticePieceWorth[]> outcome = new Dictionary<ActorKind, LatticePieceWorth[]>();
			foreach (KeyValuePair<ActorKind, List<LatticePieceWorth>> bucket in clustered)
			{
				outcome.Add(bucket.Key, bucket.Value.ToArray());
			}
			return outcome;
		}

		private static int ComputeActorGrade(int worth, int vitality)
		{
			return GetActorGrade(worth);
		}

		private static void PrintWorths(List<LatticePieceWorth> worthSeries)
		{
#if UNITY_EDITOR
			if (worthSeries == null)
			{
				return;
			}
			System.Text.StringBuilder buffer = new System.Text.StringBuilder("[Level] ");
			foreach (LatticePieceWorth pieceWorth in worthSeries)
			{
				buffer.Append(pieceWorth.kind).Append(':').Append(pieceWorth.worth).Append('@').Append(pieceWorth.ordinal).Append(' ');
			}
			Debug.Log(buffer.ToString());
#endif
		}

		// Editor-only fight trace: time since START FIGHT, attacker, victim, damage, victim health left.
		public static float clashBeginMoment;

		public static void TraceImpact(Combatant combatant, float objectiveVitality, Combatant casualty = null, float harm = 0f)
		{
#if UNITY_EDITOR
			if (combatant == null)
			{
				return;
			}
			if (impactTrace == null) impactTrace = new List<string>();
			if (impactTrace.Count >= 1000)
			{
				impactTrace.RemoveRange(0, 500);
			}
			string casualtyDesc = casualty != null ? $" -> {casualty.faction} {casualty.kind}{casualty.worth}#{casualty.GetInstanceID()}" : string.Empty;
			impactTrace.Add($"{Time.time - clashBeginMoment:F3} {combatant.faction} {combatant.kind}{combatant.worth}#{combatant.GetInstanceID()}{casualtyDesc} dmg:{harm:F1} hp:{objectiveVitality:F1}");
#endif
		}

		public static void PrintImpactTrace()
		{
#if UNITY_EDITOR
			if (impactTrace == null || impactTrace.Count == 0)
			{
				return;
			}
			Debug.Log("[HitLog]\n" + string.Join("\n", impactTrace));
			impactTrace.Clear();
#endif
		}

		// ---- [NEW] private / internal helpers ----

		// Forces the next Init() to reload everything from storage (after a save wipe).
		internal static void MarkStale()
		{
			_isBooted = false;
			RevertStageFinanceStatus();
		}

		private static void RevertStageFinanceStatus()
		{
			stageProceeds = 0L;
			_foeHarmDealt = 0.0;
			_stageOutroOutcome = StageOutcome.Unset;
			_stageOutroBoonCoefficient = 1;
			_stageOutroCredited = false;
			_creditOutstanding = false;
			_creditResolved = false;
			_creditedProceeds = 0L;
			_performedStage = 1;
		}

		private static void GuaranteeBoot()
		{
			if (!_isBooted)
			{
				Boot();
			}
		}

		// peakStageIncome(PL) = 40 * PL^4: 40, 640, 3240, 10240, 25000 …
		public static long GetPeakStageIncome(int advancement)
		{
			int bounded = Mathf.Clamp(advancement, 1, PeakAdvancementForMath);
			if (peakStageProceeds == null) peakStageProceeds = new Dictionary<int, long>();
			if (!peakStageProceeds.TryGetValue(bounded, out long income))
			{
				income = GuardedMultiply(StageIncomeBasis, AdvancementPow4(bounded));
				peakStageProceeds[bounded] = income;
			}
			return income;
		}

		private static bool IsValidLineupOrdinal(int ordinal)
		{
			GuaranteeBoot();
			return lineup != null && ordinal >= 0 && ordinal < lineup.Length;
		}

		// PL^4 (PL clamped to 1..PeakAdvancementForMath).
		private static long AdvancementPow4(int advancement)
		{
			long bounded = Mathf.Clamp(advancement, 1, PeakAdvancementForMath);
			return bounded * bounded * bounded * bounded;
		}

		private static long GuardedMultiply(long lhs, long rhs)
		{
			if (lhs <= 0L || rhs <= 0L)
			{
				return 0L;
			}
			return lhs > long.MaxValue / rhs ? long.MaxValue : lhs * rhs;
		}

		// round(max * dealt / start), never above max.
		private static long IncomeForHarm(long peak, double inflicted, double begin)
		{
			if (begin <= 0.0 || inflicted <= 0.0)
			{
				return 0L;
			}
			double portion = Math.Min(1.0, inflicted / begin);
			return Math.Min(peak, (long)Math.Round(peak * portion, MidpointRounding.AwayFromZero));
		}

		private static bool IsPersonMetrics(ActorKind kind)
		{
			return kind == ActorKind.Person;
		}

		private static double MetricGrowth(ActorKind kind)
		{
			return IsPersonMetrics(kind) ? PersonMetricGrowth : SaurianMetricGrowth;
		}

		// Tier 1..11 -> value 1..1024.
		private static int GradeToWorth(int grade)
		{
			return 1 << Mathf.Clamp(grade - 1, 0, 30);
		}

		// round(base * 2^(growth * (t - 1))) with t - 1 = log2(value) (exact integer steps for power-of-two values).
		private static int ComputeMetric(double basisMetric, double growthRate, int worth)
		{
			if (worth <= 0)
			{
				return 0;
			}
			double increments = (worth & (worth - 1)) == 0 ? GetActorGrade(worth) - 1 : Math.Log(worth, 2.0);
			double metric = basisMetric * Math.Pow(2.0, growthRate * increments);
			return (int)Math.Min(int.MaxValue, Math.Round(metric, MidpointRounding.AwayFromZero));
		}

		private static int AcquisitionTallyForFee(long fee, int advancement)
		{
			double scalar = (double)Math.Max(1L, fee) / AdvancementPow4(advancement);
			double estimated = Math.Round((scalar - TroopFeeBasis) / TroopFeeIncrement, MidpointRounding.AwayFromZero);
			return (int)Math.Max(0.0, Math.Min(int.MaxValue / 16, estimated));
		}

		// Persists the per-type purchase counters (prices are derived from them and the AdvancementStage).
		private static void PersistAcquisitionTallies()
		{
			GameData gameData = GameData.Instance;
			gameData.PersonAcquisitionTally = _personAcquisitionTally;
			gameData.SaurianAcquisitionTally = _saurianAcquisitionTally;
		}

		private static int PeakLineupWorth(ActorKind kind)
		{
			int peak = 0;
			if (lineup == null)
			{
				return peak;
			}
			foreach (int packed in lineup)
			{
				if (packed > 0 && (ActorKind)(packed / 10000) == kind)
				{
					peak = Math.Max(peak, packed % 10000);
				}
			}
			return peak;
		}

		private static void PersistPeakUnsealedActors()
		{
			peakUnsealedActors.TryGetValue(ActorKind.Person, out int person);
			peakUnsealedActors.TryGetValue(ActorKind.Saurian, out int saurian);
			GameData.Instance.PeakUnsealedActors = new[] { person, saurian };
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void RevertVisitStatus()
		{
			_isBooted = false;
			m_MoneyAltered = null;
			m_JewelsAltered = null;
			m_StonesAltered = null;
			onConjureMarketNoticePendingAltered = null;
			stageProceeds = 0L;
			foeBeginVitality = 0f;
			guardianBeginVitality = 0f;
			saurianIncomePerImpact = 0f;
			_terminatedFoesThisStage = 0;
			tallyOfTroopAcquisitions = 0;
			latestFusedActor = ActorKind.Unset;
			_personAcquisitionTally = 0;
			_saurianAcquisitionTally = 0;
			RevertStageFinanceStatus();
		}
	}
}
