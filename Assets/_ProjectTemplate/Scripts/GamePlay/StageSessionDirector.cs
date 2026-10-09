using GameToolSample;
using GameToolSample.GamePlay.Manager;
using UnityEngine;

namespace Funlab.MergeMaster
{
	public class StageSessionDirector : GameManager
	{
		public void BeginStage(int stage)
		{
			GameplayStatus = GamePlayState.none;
			CurrentLevel = stage;
			PlayGame();
		}

		public override void Victory()
		{
			if (!IsGamePlayStatus(GamePlayState.playing))
			{
				return;
			}
			GameplayStatus = GamePlayState.victory;
			TrackingVictory();
		}

		protected override void ShowVictoryPopup()
		{
			// Playable flow: bypass victory popup
		}

		protected override void ShowLosePopup()
		{
			// Playable flow: bypass lose popup
		}

		protected override void ShowRevivePopup()
		{
			// Playable flow: bypass revive popup
		}

		protected override void TrackingPlaygame() { }
		protected override void TrackingVictory() { }
		protected override void TrackingLoseEnd() { }
		protected override void TrackingEndCard() { }
		protected override void TrackingLose() { }
	}
}
