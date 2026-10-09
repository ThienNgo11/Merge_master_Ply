using System.Collections;
using GameTool.UI.Scripts.CanvasPopup;
using GameToolSample.UIManager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Funlab.MergeMaster
{
	// Level-end panels (Resources/UI/VictoryPopup and Resources/UI/LosePopup). Measured original (b_006 fail, b_017 / c_010 / d_020 / f_030 win):
	// - reward wheel on EVERY level end (win and fail, from level 1); sectors clockwise from the top X5 X4 X2 X3 X2 X4 X2 X3;
	// - CLAIM (clapperboard = rewarded video) shows YOU EARNED x the sector under the needle; "No Thanks" pops in later;
	// - fail: red "FAILED!" ribbon, red glow, TOTAL ENEMY DAMAGE bar + %, YOU EARNED;
	// - win: green "VICTORY!" ribbon, rays, TREASURE FOUND (gems from level 2 on, stones with the diorama), YOU EARNED,
	//   yellow "Level Completed Bonus x10" on wins that advance the AdvancementStage.
	// YOU EARNED is credited to gold at level end (PlayMechanics.OnStageConcluded); CLAIM adds the extra (m - 1) x YOU EARNED
	// through PlayMechanics.PersistProceeds(m), No Thanks keeps x1.
	public abstract class UIStageOutro : BaseUI
	{
		protected const float UpcomingStagePause = 1.03f; // measured (tl/pe_next): No Thanks tap 1.81 s -> fade-out starts 2.84 s

		private const float RouletteHaltTimeout = 0.5f;

		[SerializeField]
		protected GameObject outcomeDetails;

		[SerializeField]
		protected PrizeRoulette prizeRoulette;

		[SerializeField]
		protected TextMeshProUGUI stageIncomeText;

		[SerializeField]
		private GameObject declineCtrl;

		[SerializeField]
		protected GameObject touchBarrier;

		[SerializeField]
		protected GameObject cashMotion;

		[SerializeField]
		protected Button redeemCtrl;

		[SerializeField]
		private RectTransform redeemCtrlTag;

		[SerializeField]
		private GameObject redeemCtrlVidGlyph;

		protected GameplayCore _gameplayCore;

		public StageOutcome stageOutcome;

		protected Vector2 _redeemCtrlTagStandardLoc;

		protected long _income;

		protected int _imposedCoefficient = 1;

		protected bool _prizesRedeemed;

		private bool _roulettePresented;

		private bool _rouletteGratis;

		private bool _redeemRequested;

		private bool _redeemTagLocCached;

		public int PrizeRouletteCoefficient => (_roulettePresented && prizeRoulette != null) ? Mathf.Max(1, prizeRoulette.coefficient) : 1;

		public virtual void Boot(GameplayCore gameplayCore)
		{
			_gameplayCore = gameplayCore;
			if (redeemCtrlTag != null && !_redeemTagLocCached)
			{
				_redeemCtrlTagStandardLoc = redeemCtrlTag.anchoredPosition;
				_redeemTagLocCached = true;
			}
		}

		public virtual void ArrangeStageOutro(StageOutcome concludedOutcome, long income)
		{
			ArrangeCommon(concludedOutcome, income);
			// The original shows the wheel after every fight (b_006: first fail of level 1, b_017: first win), whatever
			// reward_wheel_booster_frequency says; CLAIM is always a rewarded video (free_reward_wheel_count = 0).
			_roulettePresented = prizeRoulette != null;
			_rouletteGratis = _roulettePresented && GameTuning.GratisPrizeRouletteTally > 0 && GameTuning.stageOutroTally <= GameTuning.GratisPrizeRouletteTally;
			_redeemRequested = false;
			if (prizeRoulette != null)
			{
				prizeRoulette.gameObject.SetActive(_roulettePresented);
				if (prizeRoulette.redeemText != null)
				{
					prizeRoulette.redeemText.text = Toolbox.CompactNumeralToThousands(income);
				}
			}
			bool vidRedeem = _roulettePresented && !_rouletteGratis;
			if (redeemCtrlVidGlyph != null)
			{
				redeemCtrlVidGlyph.SetActive(vidRedeem);
			}
			if (redeemCtrlTag != null)
			{
				redeemCtrlTag.anchoredPosition = vidRedeem ? _redeemCtrlTagStandardLoc : new Vector2(0f, _redeemCtrlTagStandardLoc.y);
			}
			if (redeemCtrl != null)
			{
				redeemCtrl.interactable = true;
			}
			if (declineCtrl != null)
			{
				declineCtrl.SetActive(false);
			}
		}

		public virtual void PresentStageOutro(GameObject jewelsUI)
		{
			gameObject.SetActive(true);
			if (outcomeDetails != null)
			{
				outcomeDetails.SetActive(true);
			}
			PresentOutcomeSpecifics();
			if (_roulettePresented && prizeRoulette != null)
			{
				prizeRoulette.BeginRoll(_income, stageOutcome);
			}
			TraceGuideTelemetry();
		}

		protected virtual void ArrangeOutcomeSpecifics()
		{
		}

		protected virtual void PresentOutcomeSpecifics()
		{
		}

		protected virtual void ConcealOutcomeSpecifics()
		{
		}

		protected virtual void BestowHoards()
		{
		}

		protected void TraceGuideTelemetry()
		{
		}

		public virtual void FlipLiveDeclineControl(bool live)
		{
			if (declineCtrl != null)
			{
				declineCtrl.SetActive(live && _roulettePresented && !_rouletteGratis && !_prizesRedeemed && !_redeemRequested);
			}
		}

		public virtual void FlipTouchBarrierLive(bool live)
		{
			if (touchBarrier != null)
			{
				touchBarrier.SetActive(live);
			}
		}

		public virtual void RedeemControlResponder()
		{
			if (_prizesRedeemed || _redeemRequested)
			{
				return;
			}
			_redeemRequested = true;
			FlipTouchBarrierLive(true);
			if (declineCtrl != null)
			{
				declineCtrl.SetActive(false);
			}
			if (_roulettePresented && prizeRoulette != null)
			{
				prizeRoulette.HaltRoll();
				StartCoroutine(RedeemAfterRouletteHalts());
			}
			else
			{
				ProceedAfterRouletteRoll(false);
			}
		}

		public virtual void ProceedAfterRouletteRoll(bool rewardWatched)
		{
			if (_prizesRedeemed)
			{
				return;
			}
			int coefficient = rewardWatched ? PrizeRouletteCoefficient : 1;
			RedeemPrizes(coefficient);
		}

		public virtual IEnumerator ProceedAfterAdvertPrizes()
		{
			return PrizeChain(redeemCtrl != null ? redeemCtrl.transform : transform);
		}

		public virtual void ConcealStageOutro()
		{
			StopAllCoroutines();
			if (outcomeDetails != null)
			{
				outcomeDetails.SetActive(false);
			}
			if (cashMotion != null)
			{
				cashMotion.SetActive(false);
			}
			if (declineCtrl != null)
			{
				declineCtrl.SetActive(false);
			}
			if (touchBarrier != null)
			{
				touchBarrier.SetActive(false);
			}
			ConcealOutcomeSpecifics();
			if (prizeRoulette != null)
			{
				prizeRoulette.gameObject.SetActive(false);
			}
			if (gameObject.activeSelf)
			{
				if (uiName != eUIName.None)
				{
					Pop();
				}
				else
				{
					gameObject.SetActive(false);
				}
			}
		}

		public void ControlResponder(Button dispatcher)
		{
			UIDirector director = UIDirector.Shared;
			if (director != null)
			{
				director.ControlResponder(dispatcher);
			}
		}

		public virtual void DispatchResourceSignal()
		{
		}

		protected void ArrangeCommon(StageOutcome outcome, long income)
		{
			stageOutcome = outcome;
			_income = income;
			_imposedCoefficient = 1;
			_prizesRedeemed = false;
			if (stageIncomeText != null)
			{
				stageIncomeText.text = "+" + Toolbox.CompactNumeralToThousands(income);
			}
			if (touchBarrier != null)
			{
				touchBarrier.SetActive(false);
			}
			if (cashMotion != null)
			{
				cashMotion.SetActive(false);
			}
			if (outcomeDetails != null)
			{
				outcomeDetails.SetActive(false);
			}
			ArrangeOutcomeSpecifics();
		}

		protected void RedeemPrizes(int coefficient)
		{
			_prizesRedeemed = true;
			_imposedCoefficient = Mathf.Max(1, coefficient);
			FlipTouchBarrierLive(true);
			if (declineCtrl != null)
			{
				declineCtrl.SetActive(false);
			}
			if (redeemCtrl != null)
			{
				redeemCtrl.interactable = false;
			}
			if (prizeRoulette != null)
			{
				prizeRoulette.HaltRoll();
				if (prizeRoulette.redeemText != null)
				{
					prizeRoulette.redeemText.text = Toolbox.CompactNumeralToThousands(_income * _imposedCoefficient);
				}
			}
			PlayMechanics.PersistProceeds(_imposedCoefficient);
			if (isActiveAndEnabled)
			{
				StartCoroutine(ProceedAfterAdvertPrizes());
			}
			else
			{
				BestowHoards();
				UIDirector director = UIDirector.Shared;
				if (director != null)
				{
					director.JumpToUpcomingStage();
				}
			}
		}

		protected IEnumerator PrizeChain(Transform pennyHome)
		{
			if (cashMotion != null)
			{
				cashMotion.SetActive(false);
				cashMotion.SetActive(true);
			}
			PerformCue(AudioCue.RedeemCash);
			DispatchCash(pennyHome, _income * _imposedCoefficient);
			BestowHoards();
			yield return new WaitForSeconds(UpcomingStagePause);
			UIDirector director = UIDirector.Shared;
			if (director != null)
			{
				director.JumpToUpcomingStage();
			}
		}

		protected void DispatchCash(Transform home, long sum)
		{
			UIDirector director = UIDirector.Shared;
			if (director == null || director.moneyFundsSoarMotion == null || sum <= 0)
			{
				return;
			}
			Vector2 viewportLoc = RectTransformUtility.WorldToScreenPoint(null, home.position);
			director.moneyFundsSoarMotion.DispatchFunds(sum, viewportLoc, UIFundsSoarMotion.SmoothingOutBack, UIFundsSoarMotion.SmoothingOutQuad, UIFundsSoarMotion.SmoothingInQuad);
			director.FlipMastheadCashMotion(true);
		}

		protected void PerformCue(AudioCue cue)
		{
			GameplayCore gameplayCore = _gameplayCore;
			if (gameplayCore == null && UIDirector.Shared != null)
			{
				gameplayCore = UIDirector.Shared.gameplayCore;
			}
			if (gameplayCore != null && gameplayCore.audioDirector != null)
			{
				gameplayCore.audioDirector.PerformUICue(cue);
			}
		}

		private IEnumerator RedeemAfterRouletteHalts()
		{
			float passed = 0f;
			while (prizeRoulette != null && prizeRoulette.IsRolling && passed < RouletteHaltTimeout)
			{
				passed += Time.deltaTime;
				yield return null;
			}
			if (_prizesRedeemed)
			{
				yield break;
			}
			if (_rouletteGratis)
			{
				ProceedAfterRouletteRoll(true);
				yield break;
			}
			PrizeKind prizeKind = stageOutcome == StageOutcome.Accomplishment ? PrizeKind.RouletteRollTriumph : PrizeKind.RouletteRollDefeat;
			UIDirector director = UIDirector.Shared;
			bool presented = director != null && director.PresentIncentivizedFilm(prizeKind);
			if (!presented)
			{
				RevertRedeemRequest();
			}
		}

		public void RevertRedeemRequest()
		{
			if (_prizesRedeemed)
			{
				return;
			}
			_redeemRequested = false;
			FlipTouchBarrierLive(false);
			FlipLiveDeclineControl(true);
		}
	}
}
