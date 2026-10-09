using System;
using DatdevUlts.AnimationUtils;
using DG.Tweening;
using GameTool.UI.Scripts.CanvasPopup;
using GameToolSample.UIManager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Funlab.MergeMaster
{
	public class Dialog : BaseUI
	{
		private const string PopupOpenMotion = "open";

		private const float TouchBarrierSpan = 0.5f;

		// Measured on the original (tl/pe_cont, 30 fps): after CONTINUE the dark background is gone at once and the card
		// shrinks to 0.15 almost linearly in ~0.14 s while flying to the bottom-left of the screen.
		private const float TileConcealSpan = 0.14f;

		private const float TileConcealSize = 0.15f;

		private const float TileBackdropAlpha = 0.9f;

		private const string PrivacyPolicyAddress = "";

		public UIDirector uiDirector;

		[SerializeField]
		private AnimatorController popupMotion;

		public TextMeshProUGUI incomeText;

		public TextMeshProUGUI headingText;

		public GameObject touchBarrier;

		public ActorTile actorTile;

		public RectTransform actorTileCore;

		public Button actorTileBackdrop;

		private Vector2 actorTileCoreStandardLoc;

		private Vector2[] actorTileCoreConcealWorths;

		[Header("SETTINGS")]
		public Image[] toggleArtworks;

		public Sprite[] togglePictures;

		public Button[] acquisitionControls;

		public GameObject acquisitionSection;

		public RectTransform[] preferencesTrailers;

		public RectTransform preferencesBackdrop;

		public float acquisitionSectionSpan = 154f;

		private Vector2[] _preferencesTrailerStandardLocs;

		private Vector2 _preferencesBackdropStandardLoc;

		private Vector2 _preferencesBackdropStandardSize;

		private bool _preferencesStandardsCached;

		private Action<eUIName> m_DialogDismissedSignal;

		private bool _tileStandardsCached;

		private Color _tileBackdropStandardTint = Color.white;

		private bool _isConcealingTile;

		private Tween _touchBarrierCall;

		private Tween _tileConcealTransition;

		private event Action<eUIName> DialogDismissedSignal
		{
			add
			{
				m_DialogDismissedSignal = (Action<eUIName>)Delegate.Combine(m_DialogDismissedSignal, value);
			}
			remove
			{
				m_DialogDismissedSignal = (Action<eUIName>)Delegate.Remove(m_DialogDismissedSignal, value);
			}
		}

		private GameplayCore gameplayCore => uiDirector != null ? uiDirector.gameplayCore : null;

		private static bool AcquisitionRestoreAllowed
		{
			get
			{
#if UNITY_IOS
				return true;
#else
				return false;
#endif
			}
		}

		private void Awake()
		{
			if (uiDirector == null)
			{
				uiDirector = UIDirector.Shared;
			}
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

		internal void SetDismissedHook(Action<eUIName> hook)
		{
			m_DialogDismissedSignal = null;
			if (hook != null)
			{
				DialogDismissedSignal += hook;
			}
		}

		public virtual void Present(Action<eUIName> dialogDismissedSignalHook = null)
		{
			SetDismissedHook(dialogDismissedSignalHook);
			if (uiDirector != null)
			{
				uiDirector.liveDialog = this;
			}
			AudioCue revealCue = AudioCue.DialogReveal;
			switch (uiName)
			{
			case eUIName.OfflineEarningPopup:
				SetCaption(incomeText, Toolbox.CompactNumeralToThousands(GameTuning.awayIncomeSum));
				revealCue = AudioCue.PrizeDialog;
				break;
			case eUIName.FlyingDinoPopup:
				SetCaption(incomeText, Toolbox.CompactNumeralToThousands(PlayMechanics.peakStageIncome));
				revealCue = AudioCue.PrizeDialog;
				break;
			case eUIName.SettingPopup:
				RevisePreferencesToggles();
				if (acquisitionControls != null)
				{
					foreach (Button acquisitionControl in acquisitionControls)
					{
						if (acquisitionControl != null)
						{
							acquisitionControl.gameObject.SetActive(AcquisitionRestoreAllowed);
						}
					}
				}
				ArrangePreferencesFooter();
				break;
			}
			EngageTouchBarrier(TouchBarrierSpan);
			gameObject.SetActive(true);
			PerformCue(revealCue);
		}

		public void PresentActor(ActorKind actorKind, int actorWorth)
		{
			if (uiDirector != null)
			{
				uiDirector.liveDialog = this;
			}
			int stage = Mathf.Max(1, PlayMechanics.GetActorGrade(actorWorth));
			if (actorTile != null)
			{
				Sprite picture = MatchAssets.Shared != null ? MatchAssets.Shared.GetActorPicture(actorKind, 1 << (stage - 1)) : null;
				int stageTally = uiDirector != null ? uiDirector.GetActorGradeTally(actorKind, stage) : 11;
				actorTile.Assemble(picture, GetActorTitle(actorKind, stage), actorKind, stage, stageTally);
			}
			// b_025: purple ribbon "NEW DINOBOT" / "NEW MECH".
			SetCaption(headingText, actorKind == ActorKind.Saurian ? "NEW DINOBOT" : "NEW MECH");
			bool motionSettled = popupMotion != null && popupMotion.Awaked;
			if (popupMotion == null || motionSettled)
			{
				RevertTileVisuals();
			}
			else
			{
				CacheTileStandards();
			}
			gameObject.SetActive(true);
			if (motionSettled)
			{
				popupMotion.Update();
			}
			PerformCue(AudioCue.PresentTile);
		}

		public virtual void ControlResponder(Button dispatcher)
		{
			if (dispatcher == null)
			{
				return;
			}
			switch (dispatcher.name)
			{
			case "CloseBtn":
			case "dim":
				OnDismissRequested();
				break;
			case "OfflineEarningCollectBtn":
				CollectIncome(false);
				break;
			case "OfflineEarningRVBtn":
				PresentIncentivizedFilm(PrizeKind.AwayIncome);
				break;
			case "FlyingDinoFreeBtn":
				PresentIncentivizedFilm(PrizeKind.HVASoaringSaurian);
				break;
			case "FlyingDinoLaterBtn":
				Conceal();
				break;
			case "SoundSwitch":
				GameTuning.isAudioOn = !GameTuning.isAudioOn;
				RevisePreferencesToggles();
				break;
			case "MusicSwitch":
				GameTuning.isSoundtrackOn = !GameTuning.isSoundtrackOn;
				RevisePreferencesToggles();
				break;
			case "HapticSwitch":
				GameTuning.isTactileReactionAllowed = !GameTuning.isTactileReactionAllowed;
				if (GameTuning.isTactileReactionAllowed)
				{
					Toolbox.PerformTactileImpact(ImpactFeedbackStyle.Light);
				}
				RevisePreferencesToggles();
				break;
			case "RestoreBtn":
				// InAppCoordinator not needed in Playable
				break;
			case "PrivacyPolicyBtn":
				Application.OpenURL(PrivacyPolicyAddress);
				break;
			}
		}

		public void CollectIncome(bool withVid)
		{
			if (!gameObject.activeSelf || uiName != eUIName.OfflineEarningPopup)
			{
				return;
			}
			long sum = GameTuning.awayIncomeSum;
			if (withVid)
			{
				sum *= GameTuning.awayIncomeVidCoefficient;
			}
			if (sum > 0)
			{
				PlayMechanics.money += sum;
				PlayMechanics.TraceResource(true, withVid ? "x2_offline_earning_popup" : "offline_earning_popup", PlayMechanics.CoinResource, sum);
				PerformCue(AudioCue.RedeemCash);
				if (uiDirector != null && uiDirector.moneyFundsSoarMotion != null)
				{
					Transform home = incomeText != null ? incomeText.transform : transform;
					Vector2 viewportLoc = RectTransformUtility.WorldToScreenPoint(CanvasLens(home), home.position);
					uiDirector.moneyFundsSoarMotion.DispatchFunds(sum, viewportLoc, UIFundsSoarMotion.SmoothingOutBack, UIFundsSoarMotion.SmoothingOutQuad, UIFundsSoarMotion.SmoothingInQuad);
				}
			}
			GameTuning.PersistAwayIncomeMoment();
			Conceal();
		}

		private void ArrangePreferencesFooter()
		{
			bool restoreShown = AcquisitionRestoreAllowed;
			if (acquisitionSection != null)
			{
				acquisitionSection.SetActive(restoreShown);
			}
			if (preferencesBackdrop == null || preferencesTrailers == null)
			{
				return;
			}
			if (!_preferencesStandardsCached)
			{
				_preferencesTrailerStandardLocs = new Vector2[preferencesTrailers.Length];
				for (int i = 0; i < preferencesTrailers.Length; i++)
				{
					if (preferencesTrailers[i] != null)
					{
						_preferencesTrailerStandardLocs[i] = preferencesTrailers[i].anchoredPosition;
					}
				}
				_preferencesBackdropStandardLoc = preferencesBackdrop.anchoredPosition;
				_preferencesBackdropStandardSize = preferencesBackdrop.sizeDelta;
				_preferencesStandardsCached = true;
			}
			float lift = restoreShown ? 0f : acquisitionSectionSpan;
			for (int i = 0; i < preferencesTrailers.Length; i++)
			{
				if (preferencesTrailers[i] != null)
				{
					preferencesTrailers[i].anchoredPosition = _preferencesTrailerStandardLocs[i] + new Vector2(0f, lift);
				}
			}
			preferencesBackdrop.anchoredPosition = _preferencesBackdropStandardLoc + new Vector2(0f, lift * 0.5f);
			preferencesBackdrop.sizeDelta = _preferencesBackdropStandardSize - new Vector2(0f, lift);
		}

		private void RevisePreferencesToggles()
		{
			if (toggleArtworks == null || togglePictures == null || togglePictures.Length < 2)
			{
				return;
			}
			SetToggle(0, GameTuning.isTactileReactionAllowed);
			SetToggle(1, GameTuning.isAudioOn);
			SetToggle(2, GameTuning.isSoundtrackOn);
		}

		public void Conceal()
		{
			if (_touchBarrierCall != null)
			{
				_touchBarrierCall.Kill();
				_touchBarrierCall = null;
			}
			if (touchBarrier != null)
			{
				touchBarrier.SetActive(false);
			}
			if (gameObject.activeSelf)
			{
				if (uiName != eUIName.None)
				{
					base.Pop();
				}
				else
				{
					gameObject.SetActive(false);
				}
			}
			if (uiDirector != null && uiDirector.liveDialog == this)
			{
				uiDirector.liveDialog = null;
			}
			Action<eUIName> hook = m_DialogDismissedSignal;
			m_DialogDismissedSignal = null;
			hook?.Invoke(uiName);
		}

		public override void Pop()
		{
			Conceal();
		}

		private void ActorDialogConcealResponder(float worth)
		{
			if (actorTileCore == null || actorTileCoreConcealWorths == null)
			{
				return;
			}
			actorTileCore.anchoredPosition = Vector2.LerpUnclamped(actorTileCoreConcealWorths[0], actorTileCoreConcealWorths[1], worth);
			Vector2 size = Vector2.LerpUnclamped(actorTileCoreConcealWorths[2], actorTileCoreConcealWorths[3], worth);
			actorTileCore.localScale = new Vector3(size.x, size.y, 1f);
		}

		private void ActorDialogDidConceal()
		{
			_tileConcealTransition = null;
			RevertTileVisuals();
			if (uiDirector != null && uiDirector.tileControlMotion != null)
			{
				uiDirector.tileControlMotion.Stop();
				uiDirector.tileControlMotion.Play("ButtonBounce");
			}
			PerformCue(AudioCue.CollectCash);
			_isConcealingTile = false;
			Conceal();
		}

		private void OnDismissRequested()
		{
			switch (uiName)
			{
			case eUIName.CardPopup:
				ConcealActorTile();
				break;
			case eUIName.OfflineEarningPopup:
				CollectIncome(false);
				break;
			default:
				Conceal();
				break;
			}
		}

		private void ConcealActorTile()
		{
			if (_isConcealingTile)
			{
				return;
			}
			if (actorTileCore == null || uiDirector == null || uiDirector.mastheadTilesControlLoc == null)
			{
				Conceal();
				return;
			}
			_isConcealingTile = true;
			Animation coreMotion = actorTileCore.GetComponent<Animation>();
			if (coreMotion != null)
			{
				coreMotion.Stop();
			}
			CacheTileStandards();
			actorTileCore.anchoredPosition = actorTileCoreStandardLoc;
			actorTileCore.localScale = Vector3.one;
			Vector2 objective = actorTileCoreStandardLoc;
			RectTransform holder = actorTileCore.parent as RectTransform;
			if (holder != null)
			{
				Transform masthead = uiDirector.mastheadTilesControlLoc;
				Vector2 viewport = RectTransformUtility.WorldToScreenPoint(CanvasLens(masthead), masthead.position);
				// The original's card lands at the header cards button mirrored vertically (bottom-left of the screen).
				viewport.y = Screen.height - viewport.y;
				if (RectTransformUtility.ScreenPointToLocalPointInRectangle(holder, viewport, CanvasLens(holder), out Vector2 localMark))
				{
					objective = actorTileCoreStandardLoc + (localMark - (Vector2)actorTileCore.localPosition);
				}
			}
			actorTileCoreConcealWorths = new Vector2[4]
			{
				actorTileCoreStandardLoc,
				objective,
				Vector2.one,
				new Vector2(TileConcealSize, TileConcealSize)
			};
			if (actorTileBackdrop != null)
			{
				actorTileBackdrop.interactable = false;
				Image backdrop = BackdropArtwork();
				if (backdrop != null)
				{
					Color tint = backdrop.color;
					tint.a = 0f;
					backdrop.color = tint;
				}
			}
			_tileConcealTransition = DOVirtual.Float(0f, 1f, TileConcealSpan, ActorDialogConcealResponder).SetEase(Ease.Linear).OnComplete(ActorDialogDidConceal);
		}

		private void PresentIncentivizedFilm(PrizeKind prizeKind)
		{
			if (uiDirector != null)
			{
				uiDirector.PresentIncentivizedFilm(prizeKind);
			}
		}

		private void CacheTileStandards()
		{
			if (_tileStandardsCached)
			{
				return;
			}
			_tileStandardsCached = true;
			if (actorTileCore != null)
			{
				actorTileCoreStandardLoc = actorTileCore.anchoredPosition;
			}
			Image backdrop = BackdropArtwork();
			if (backdrop != null)
			{
				_tileBackdropStandardTint = backdrop.color;
				_tileBackdropStandardTint.a = Mathf.Max(_tileBackdropStandardTint.a, TileBackdropAlpha);
			}
		}

		private void RevertTileVisuals()
		{
			if (_tileConcealTransition != null)
			{
				_tileConcealTransition.Kill();
				_tileConcealTransition = null;
			}
			_isConcealingTile = false;
			CacheTileStandards();
			if (actorTileCore != null)
			{
				actorTileCore.anchoredPosition = actorTileCoreStandardLoc;
				actorTileCore.localScale = Vector3.one;
			}
			if (actorTileBackdrop != null)
			{
				actorTileBackdrop.interactable = true;
				Image backdrop = BackdropArtwork();
				if (backdrop != null)
				{
					backdrop.color = _tileBackdropStandardTint;
				}
			}
		}

		private Image BackdropArtwork()
		{
			if (actorTileBackdrop == null)
			{
				return null;
			}
			Image artwork = actorTileBackdrop.targetGraphic as Image;
			return artwork != null ? artwork : actorTileBackdrop.GetComponent<Image>();
		}

		private string GetActorTitle(ActorKind actorKind, int stage)
		{
			GameplayCore core = gameplayCore;
			string[] titles = core == null ? null : (actorKind == ActorKind.Saurian ? core.saurianTitles : core.personTitles);
			if (titles == null || titles.Length == 0)
			{
				return string.Empty;
			}
			string actorTitle = titles[Mathf.Clamp(stage - 1, 0, titles.Length - 1)];
			return string.IsNullOrEmpty(actorTitle) ? string.Empty : actorTitle.ToUpper();
		}

		private void EngageTouchBarrier(float span)
		{
			if (_touchBarrierCall != null)
			{
				_touchBarrierCall.Kill();
				_touchBarrierCall = null;
			}
			if (touchBarrier == null)
			{
				return;
			}
			touchBarrier.SetActive(true);
			_touchBarrierCall = DOVirtual.DelayedCall(span, () =>
			{
				_touchBarrierCall = null;
				if (touchBarrier != null)
				{
					touchBarrier.SetActive(false);
				}
			});
		}

		private void SetToggle(int ordinal, bool isOn)
		{
			if (ordinal < toggleArtworks.Length && toggleArtworks[ordinal] != null)
			{
				toggleArtworks[ordinal].sprite = togglePictures[isOn ? 1 : 0];
			}
		}

		private void PerformCue(AudioCue cue)
		{
			GameplayCore core = gameplayCore;
			if (core != null && core.audioDirector != null)
			{
				core.audioDirector.PerformUICue(cue);
			}
		}

		private static void SetCaption(TMP_Text caption, string worth)
		{
			if (caption != null)
			{
				caption.text = worth;
			}
		}

		private static Camera CanvasLens(Transform pivot)
		{
			Canvas hostCanvas = pivot != null ? pivot.GetComponentInParent<Canvas>() : null;
			if (hostCanvas == null)
			{
				return null;
			}
			hostCanvas = hostCanvas.rootCanvas;
			return hostCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : hostCanvas.worldCamera;
		}

		private void OnDestroy()
		{
			_touchBarrierCall?.Kill();
			_tileConcealTransition?.Kill();
		}
	}
}
