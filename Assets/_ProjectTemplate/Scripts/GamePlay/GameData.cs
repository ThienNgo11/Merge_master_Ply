using System;
using UnityEngine;

namespace Funlab.MergeMaster
{
	/// <summary>
	/// In-memory runtime data holder for Merge Master Playable session.
	/// Independent of heavy serialization frameworks, resetting cleanly for each playable play-through.
	/// </summary>
	public class GameData
	{
		private static GameData _instance;

		public static GameData Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new GameData();
				}
				return _instance;
			}
		}

		public static bool IsInstanceValid()
		{
			return _instance != null;
		}

		public static void ResetData()
		{
			_instance = new GameData();
		}

		// Economy & progress
		public long Money { get; set; } = 0L;
		public long Jewels { get; set; } = 0L;
		public int Stones { get; set; } = 0;
		public int CurrentLevel { get; set; } = 1;
		public int AdvancementStage { get; set; } = 1;
		public int LatestTelemetryTracedStage { get; set; } = 0;
		public string BaselineRevision { get; set; } = "1.0.0";

		// Settings
		public bool SoundFX { get; set; } = true;
		public bool Music { get; set; } = true;
		public bool Vibrate { get; set; } = true;
		public bool RemoveAds { get; set; } = false;
		public bool DidAcquireBeginnerPackage { get; set; } = false;

		// Biome & Stats
		public int LatestBiomeAlterStage { get; set; } = 1;
		public bool IsPayingGamer { get; set; } = false;
		public int AwayIncomeBeginMoment { get; set; } = 0;
		public GuideKind FinishedGuideKind { get; set; } = GuideKind.Unset;
		public bool DidEverShiftActor { get; set; } = false;

		// Miniature / Extras
		public bool IsMiniatureGuideFinished { get; set; } = false;
		public bool IsMiniatureBeginningRocksGiven { get; set; } = false;
		public int LatestMiniatureFinishStage { get; set; } = 0;
		public bool FirstSessionDone { get; set; } = true;
		public int OncePerStageTelemetry { get; set; } = 0;
		public int LatestInAppAcquisitionMoment { get; set; } = 0;

		public ActorKind LatestUnsealedActorKind { get; set; } = ActorKind.Unset;
		public bool ConjureMarketNoticePending { get; set; } = false;

		public int VictoryCount { get; set; } = 0;
		public int LoseCount { get; set; } = 0;
		public int VidTally { get; set; } = 0;

		public int PersonAcquisitionTally { get; set; } = 0;
		public int SaurianAcquisitionTally { get; set; } = 0;

		private int[] _lineup;
		public int[] Lineup
		{
			get
			{
				if (_lineup == null)
				{
					_lineup = (int[])PlayMechanics.beginLineup.Clone();
				}
				return _lineup;
			}
			set => _lineup = value;
		}

		private int[] _peakUnsealedActors = { 0, 1 };
		public int[] PeakUnsealedActors
		{
			get => _peakUnsealedActors;
			set => _peakUnsealedActors = value;
		}
	}
}
