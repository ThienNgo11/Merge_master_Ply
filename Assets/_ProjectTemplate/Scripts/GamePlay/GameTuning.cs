using System;
using System.Collections.Generic;
using System.Globalization;
using GameTool.Audio.Scripts;
using UnityEngine;

namespace Funlab.MergeMaster
{
	// Static game configuration: live remote-config values (RemoteTuning constants), persisted settings/progress
	// (through GameData) and the global event buses.
	public static class GameTuning
	{
		public static readonly int[] LatticeWorths = { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024 };

		public static bool isBooted;

		private static Action<int> m_StageAltered;

		private static readonly int floorAwayIncomeSpan = 600;

		public static readonly int awayIncomeVidCoefficient = 2;

		public static bool isIncentivizedFilmAccessible = true;

		public static bool isHVIncentivizedFilmAccessible = true;

		public static readonly float combatantFuseSpan = 0.3f;

		public static readonly float combatantFuseSizeSpan = 0.12f;

		public static readonly float combatantRunSpan = 0.35f;

		public static readonly float afterStagePause = 1.95f; // measured (tl/pe_win): final kill ~5.0 s -> jaws start closing 6.98 s

		public static readonly float floorLensExtent = 72f;

		public static readonly float peakLensExtent = 84f;

		public static readonly float floorArenaLensExtent = 45.5f;

		public static readonly float troopStature = 1.308f;

		private static readonly float[] personGradeStatures = { 1f, 0.876f, 0.987f, 0.876f, 0.947f, 0.876f, 0.966f, 0.984f, 0.946f, 0.923f, 0.986f };

		private static readonly float[] saurianGradeStatures = { 1f, 1.103f, 0.914f, 0.976f, 1.093f, 1.273f, 1.14f, 0.94f, 1.347f, 1.108f, 1.149f };

		public static readonly float standardViewportProportion = 0.5625f;

		public static readonly float floorViewportProportion = 0.4615f;

		public static int repeatStageOrdinal = -1;

		public static bool arenaInteractionAllowed = true;

		public static AudioSignal audioSignal = new AudioSignal();

		public static MatchStatus MatchStatus = MatchStatus.ClashArena;

		public static HashSet<string> motionSamplesWithSignal = new HashSet<string>();

		public static HashSet<string> actorsWithMotionSignalsAppended = new HashSet<string>();

		public static StageOutcome awaitingInterlude = StageOutcome.Unset;

		public static int accomplishmentTally;

		public static int defeatTally;

		public static int stageOutroTally;

		public static int prizeRouletteDefeatTally;

		public static bool didWatchVid;

		// Session only; starts at 1 so the "+4 HOT!" offer can appear on the first eligible level of a session.
		private static int _stagesSinceInMatchVid = 1;

		private static bool _isInitialVisit;

		private static int[] _conjureCharges;

		private static readonly char[] SeriesSeparator = { ',' };

		// [NEW] live remote values used by other units.
		public const int SoaringSaurianBeginStage = 4;

		public const int FuseReminderBeginStage = 3;

		public const int ExclusiveVidControlBeginStage = 5;

		public static int GratisPrizeRouletteTally => RemoteTuning.gratis_prize_roulette_tally;

		public static bool MotionsFreshTroop => RemoteTuning.motions_freshtroop;

		public static bool MotionsFreshBiome => RemoteTuning.motions_freshbiome;

		public static bool MotionsAllowBypass => RemoteTuning.motions_allowbypass;

		// Summon store gem prices per tier (index = tier - 1).
		public static int[] ConjureCharges
		{
			get
			{
				if (_conjureCharges == null)
				{
					_conjureCharges = DecodeIntegerSeries(RemoteTuning.conjure_charges);
				}
				return _conjureCharges;
			}
		}

		// Levels per environment and number of distinct environments (L1-10 env 0 desert, L11-20 env 1, L21-30 env 2 …).
		public const int StagesPerBiome = 10;

		public const int BiomeTally = 10;

		// Environment index = (level - 1) / 10 (wrapping after the 10 environments).
		public static int GetBiomeOrdinal(int stageNumber)
		{
			return (Mathf.Max(1, stageNumber) - 1) / StagesPerBiome % BiomeTally;
		}

		public static bool IsBiomeAlterPending => areBiomesAllowed && stage > 1 &&
			(stage - 1) / StagesPerBiome != (Mathf.Max(1, latestBiomeAlterStage) - 1) / StagesPerBiome;

		public static int latestTelemetryTracedStage
		{
			get { return GameData.Instance.LatestTelemetryTracedStage; }
			set { GameData.Instance.LatestTelemetryTracedStage = value; }
		}

		public static int stage
		{
			get => Mathf.Max(1, GameData.Instance.CurrentLevel);
			set
			{
				int clamped = Mathf.Max(1, value);
				GameData.Instance.CurrentLevel = clamped;
				m_StageAltered?.Invoke(clamped);
			}
		}

		public static int advancementStage
		{
			get => Mathf.Max(1, GameData.Instance.AdvancementStage);
			set => GameData.Instance.AdvancementStage = Mathf.Max(1, value);
		}

		public static string baselineRevision
		{
			get => GameData.Instance.BaselineRevision;
			set => GameData.Instance.BaselineRevision = value ?? string.Empty;
		}

		public static bool isAudioOn
		{
			get => GameData.Instance.SoundFX;
			set
			{
				if (AudioManager.IsInstanceValid())
				{
					AudioManager.Instance.SetSFX(value);
				}
				else
				{
					GameData.Instance.SoundFX = value;
				}
			}
		}

		public static bool isSoundtrackOn
		{
			get => GameData.Instance.Music;
			set
			{
				bool altered = GameData.Instance.Music != value;
				if (AudioManager.IsInstanceValid())
				{
					AudioManager.Instance.SetMusic(value);
				}
				else
				{
					GameData.Instance.Music = value;
				}
				if (!altered)
				{
					return;
				}
				AudioDirector audioDirector = GameplayCore.IsInstanceValid() ? GameplayCore.Instance.audioDirector : null;
				if (audioDirector == null)
				{
					return;
				}
				if (value)
				{
					audioDirector.ResumeMusicAfterToggle();
				}
				else
				{
					audioDirector.HaltMusic();
				}
			}
		}

		public static bool isTactileReactionAllowed
		{
			get => GameData.Instance.Vibrate;
			set
			{
				GameData.Instance.Vibrate = value;
			}
		}

		public static bool didDiscardAdverts
		{
			get => GameData.Instance.RemoveAds;
			set => GameData.Instance.RemoveAds = value;
		}

		public static bool DidAcquireBeginnerPackage
		{
			get => GameData.Instance.DidAcquireBeginnerPackage;
			set => GameData.Instance.DidAcquireBeginnerPackage = value;
		}

		// "a.b.c" -> a*10000 + b*100 + c (3.50.2 -> 35002); 0 when unparsable.
		public static int baselineRevisionNum
		{
			get
			{
				string revision = baselineRevision;
				if (string.IsNullOrEmpty(revision))
				{
					return 0;
				}
				string[] segments = revision.Split('.');
				int outcome = 0;
				int[] magnitudes = { 10000, 100, 1 };
				for (int idx = 0; idx < magnitudes.Length; idx++)
				{
					if (idx >= segments.Length)
					{
						break;
					}
					if (!int.TryParse(segments[idx].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int num))
					{
						return 0;
					}
					outcome += num * magnitudes[idx];
				}
				return outcome;
			}
		}

		public static int latestBiomeAlterStage
		{
			get => Mathf.Max(1, GameData.Instance.LatestBiomeAlterStage);
			set => GameData.Instance.LatestBiomeAlterStage = Mathf.Max(1, value);
		}

		public static bool IsPayingGamer
		{
			get => GameData.Instance.IsPayingGamer;
			set => GameData.Instance.IsPayingGamer = value;
		}

		public static float actorMotionPace => RemoteTuning.actor_motion_pace;

		public static int awayIncomeBeginMoment => GameData.Instance.AwayIncomeBeginMoment;

		public static bool isAwayIncomeAccessible
		{
			get
			{
				int beginStamp = awayIncomeBeginMoment;
				return beginStamp > 0 && finishedGuideKind >= GuideKind.FuseCombatants && stage >= 3 &&
					Toolbox.MomentInSecs() - beginStamp >= floorAwayIncomeSpan;
			}
		}

		// Measured on the original (h3_060): after ~74 min away at L95 (PL 89) the popup showed "2509M" = 40 * 89^4 exactly,
		// i.e. one max level earning, not a time-scaled amount.
		public static long awayIncomeSum => PlayMechanics.peakStageIncome;

		public static GuideKind finishedGuideKind
		{
			get => GameData.Instance.FinishedGuideKind;
			set => GameData.Instance.FinishedGuideKind = value;
		}

		public static bool didEverShiftActor
		{
			get => GameData.Instance.DidEverShiftActor;
			set
			{
				GameData gameData = GameData.Instance;
				if (gameData.DidEverShiftActor != value)
				{
					gameData.DidEverShiftActor = value;
				}
			}
		}

		public static int stageOrdinal => repeatStageOrdinal >= 0 ? repeatStageOrdinal : Mathf.Max(0, stage - 1);

		private static float viewportProportion => Screen.height > 0 ? (float)Screen.width / Screen.height : standardViewportProportion;

		private static float viewportProportionRate => Mathf.InverseLerp(standardViewportProportion, floorViewportProportion, viewportProportion);

		// Vertical FOV of the unit close-up camera: 72 at 16:9 up to 84 at 19.5:9.
		public static float lensExtent => Mathf.Lerp(floorLensExtent, peakLensExtent, viewportProportionRate);

		public static float handsetArenaLensExtent;

		public static float arenaLensExtent
		{
			get
			{
				if (handsetArenaLensExtent > 0f)
				{
					return handsetArenaLensExtent;
				}
				float breadthTan = Mathf.Tan(floorArenaLensExtent * 0.5f * Mathf.Deg2Rad) * standardViewportProportion;
				float fitted = 2f * Mathf.Atan(breadthTan / Mathf.Max(0.1f, viewportProportion)) * Mathf.Rad2Deg;
				return Mathf.Max(floorArenaLensExtent, fitted);
			}
		}

		public static float arenaContourScale => Mathf.Tan(arenaLensExtent * 0.5f * Mathf.Deg2Rad) / Mathf.Tan(lensExtent * 0.5f * Mathf.Deg2Rad);

		public static int stageCluster => Mathf.Max(0, (int)financeScheme);

		public static long financeScheme => RemoteTuning.finance_scheme;

		public static bool areBiomesAllowed => RemoteTuning.biomes_allowed;

		public static bool JewelsAllowed => RemoteTuning.jewels_allowed;

		public static int StagesSinceInMatchVid
		{
			get
			{
				return _stagesSinceInMatchVid;
			}
			set
			{
				_stagesSinceInMatchVid = Mathf.Max(0, value);
			}
		}

		// Measured on the live game (L5-L8 runs): the HOT +4 offer needs 2 levels WON since the last in-game rewarded video
		// or since app launch (L7 = 1 win after an RV: none; L8 = 2 wins: HOT; 3 losses in a row: none), so the live value
		// is 2 even though the bundled default is 1.
		public static int ExclusiveTroopControlGap => Mathf.Max(2, RemoteTuning.exclusive_troop_control_gap);

		public static bool ExclusiveTroopControlPlusFourTactic => RemoteTuning.exclusive_troop_control_plus_four_tactic;

		public static bool ExclusiveTroopControlPlusFourDoubleTactic => RemoteTuning.exclusive_troop_control_plus_four_double_tactic;

		public static bool FoeMightProfileAllowed => RemoteTuning.foe_might_profile_allowed;

		public static int FoeMightProfileBeginStage => RemoteTuning.foe_might_profile_begin_stage;

		public static int FoeMightProfileFinalStage => RemoteTuning.foe_might_profile_final_stage;

		public static int FoeMightProfileBasis => RemoteTuning.foe_might_profile_basis;

		public static double FoeMightProfileRate => RemoteTuning.foe_might_profile_rate;

		public static bool MiniatureAllowed => RemoteTuning.mini_allowed;

		public static int MiniatureFloorStage => RemoteTuning.mini_floor_stage;

		public static bool CanPresentMiniatureUIControl => MiniatureAllowed && stage >= MiniatureFloorStage;

		public static string MiniatureRockOutroOfStage => RemoteTuning.mini_rock_eos;

		public static string MiniatureRequirements => RemoteTuning.mini_requirements;

		public static int MiniatureBeginningRocks => RemoteTuning.mini_begin_rock;

		public static bool IsMiniatureGuideFinished
		{
			get => GameData.Instance.IsMiniatureGuideFinished;
			set => GameData.Instance.IsMiniatureGuideFinished = value;
		}

		public static bool IsMiniatureBeginningRocksGiven
		{
			get => GameData.Instance.IsMiniatureBeginningRocksGiven;
			set => GameData.Instance.IsMiniatureBeginningRocksGiven = value;
		}

		public static int LatestMiniatureFinishStage
		{
			get => GameData.Instance.LatestMiniatureFinishStage;
			set => GameData.Instance.LatestMiniatureFinishStage = value;
		}

		public static bool presentTileBoostBadges => RemoteTuning.present_actor_might_boost;

		public static bool presentStageFoeHarmMeter => RemoteTuning.present_stage_outro_metrics;

		public static bool presentCombatantVitalityMeters => RemoteTuning.present_cumulative_vitality_meters;

		public static TileStyle tileStyle => RemoteTuning.present_detailed_tile ? TileStyle.Elaborate : TileStyle.Standard;

		public static bool isInitialVisit
		{
			get
			{
				GuaranteeBoot();
				return _isInitialVisit;
			}
		}

		public static event Action<int> StageAltered
		{
			add
			{
				m_StageAltered += value;
			}
			remove
			{
				m_StageAltered -= value;
			}
		}

		public static void Boot()
		{
			if (isBooted)
			{
				return;
			}
			isBooted = true;
			if (audioSignal == null) audioSignal = new AudioSignal();
			if (motionSamplesWithSignal == null) motionSamplesWithSignal = new HashSet<string>();
			if (actorsWithMotionSignalsAppended == null) actorsWithMotionSignalsAppended = new HashSet<string>();
			Toolbox.Boot();

			GameData gameData = GameData.Instance;
			_isInitialVisit = !gameData.FirstSessionDone;
			if (_isInitialVisit)
			{
				gameData.FirstSessionDone = true;
			}
			if (string.IsNullOrEmpty(gameData.BaselineRevision))
			{
				gameData.BaselineRevision = Application.version;
			}

			_stagesSinceInMatchVid = 1;
		}

		// Called by GameplayCore once at the end of every fight (after the win/fail bookkeeping).
		// No interstitials exist in the remake; the session counters are kept and the level's YOU EARNED is credited to gold.
		public static void ReviseInterlude(StageOutcome stageOutcome)
		{
			PlayMechanics.OnStageConcluded(stageOutcome);
			stageOutroTally++;
			if (stageOutcome == StageOutcome.Accomplishment)
			{
				accomplishmentTally++;
			}
			else
			{
				defeatTally++;
			}
			awaitingInterlude = StageOutcome.Unset;
		}

		public static float GetTroopStature(ActorKind kind, int worth)
		{
			if (worth <= 0 || (worth & (worth - 1)) != 0 || (kind != ActorKind.Person && kind != ActorKind.Saurian))
			{
				return troopStature;
			}
			float[] gradeStatures = kind == ActorKind.Person ? personGradeStatures : saurianGradeStatures;
			int slot = Mathf.Clamp(PlayMechanics.GetActorGrade(worth) - 1, 0, gradeStatures.Length - 1);
			return troopStature * gradeStatures[slot];
		}

		public static void PersistAwayIncomeMoment()
		{
			if (!GameData.IsInstanceValid())
			{
				return;
			}
			GameData.Instance.AwayIncomeBeginMoment = Toolbox.MomentInSecs();
		}

		public static void VerifyRepeatStageOrdinal(int stageTally)
		{
			if (stageTally <= 0 || stage <= stageTally)
			{
				repeatStageOrdinal = -1;
				return;
			}
			SetRepeatStageOrdinal(stageTally);
		}

		public static void DidFinishGuide(GuideKind kind)
		{
			if (kind > finishedGuideKind)
			{
				finishedGuideKind = kind;
			}
		}

		// After the last level (L300) the original keeps replaying the LAST level layout (Level-00300) for L301, L302 …
		// (measured, DISCREPANCIES A13); PL keeps growing since that prefab has bypassStageAdvancement = 0.
		public static void SetRepeatStageOrdinal(int stageTally)
		{
			if (stageTally <= 0)
			{
				repeatStageOrdinal = -1;
				return;
			}
			repeatStageOrdinal = stageTally - 1;
		}

		public static void DispatchActivitySignal(string activityKind, string activity)
		{
		}

		public static void RevertOncePerStageSignals()
		{
			if (stage == latestTelemetryTracedStage)
			{
				return;
			}
			GameData.Instance.OncePerStageTelemetry = 0;
			latestTelemetryTracedStage = stage;
		}

#if USE_IAP
		public static int GetJewelsForInAppItem(IAPManager.IAPItemName itemName)
		{
			switch (itemName)
			{
				case IAPManager.IAPItemName.Gems1:
					return RemoteTuning.jewel_kit_grade_one;
				case IAPManager.IAPItemName.Gems2:
					return RemoteTuning.jewel_kit_grade_two;
				case IAPManager.IAPItemName.Gems3:
					return RemoteTuning.jewel_kit_grade_three;
				case IAPManager.IAPItemName.Gems4:
					return RemoteTuning.jewel_kit_grade_four;
				case IAPManager.IAPItemName.Gems5:
					return RemoteTuning.jewel_kit_grade_five;
				case IAPManager.IAPItemName.Gems6:
					return RemoteTuning.jewel_kit_grade_six;
				case IAPManager.IAPItemName.BeginnerPackage:
					return 100;
				case IAPManager.IAPItemName.ExclusivePackage:
					return 250;
				default:
					return 0;
			}
		}
#endif

		public static void PersistInAppAcquisitionMomentNow()
		{
			GameData.Instance.LatestInAppAcquisitionMoment = Toolbox.MomentInSecs();
			IsPayingGamer = true;
		}

		public static bool AreInterludesSilencedAfterInAppAcquisition()
		{
			int latestInApp = GameData.Instance.LatestInAppAcquisitionMoment;
			return latestInApp > 0 && Toolbox.MomentInSecs() - latestInApp < RemoteTuning.in_app_interludes_silence_period * 86400;
		}

		// ---- [NEW] private helpers ----

		private static void GuaranteeBoot()
		{
			if (!isBooted)
			{
				Boot();
			}
		}

		// Keeps play sessions independent when domain reload is disabled in the editor.
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void RevertVisitStatus()
		{
			isBooted = false;
			m_StageAltered = null;
			audioSignal = new AudioSignal();
			motionSamplesWithSignal = new HashSet<string>();
			actorsWithMotionSignalsAppended = new HashSet<string>();
			repeatStageOrdinal = -1;
			arenaInteractionAllowed = true;
			isIncentivizedFilmAccessible = true;
			isHVIncentivizedFilmAccessible = true;
			MatchStatus = MatchStatus.ClashArena;
			awaitingInterlude = StageOutcome.Unset;
			accomplishmentTally = 0;
			defeatTally = 0;
			stageOutroTally = 0;
			prizeRouletteDefeatTally = 0;
			didWatchVid = false;
			_stagesSinceInMatchVid = 1;
		}

		private static bool IsBlank(string text)
		{
			return string.IsNullOrWhiteSpace(text) || string.Equals(text.Trim(), "null", StringComparison.OrdinalIgnoreCase);
		}

		private static int[] DecodeIntegerSeries(string text)
		{
			if (IsBlank(text))
			{
				return new int[0];
			}
			string[] segments = text.Split(SeriesSeparator, StringSplitOptions.RemoveEmptyEntries);
			List<int> outcome = new List<int>(segments.Length);
			foreach (string segment in segments)
			{
				if (int.TryParse(segment.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int num))
				{
					outcome.Add(num);
				}
			}
			return outcome.ToArray();
		}

	}
}
