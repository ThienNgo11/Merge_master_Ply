using System;
using System.Collections.Generic;
using DG.Tweening;
using GameToolSample.UIManager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Funlab.MergeMaster
{
	public class StandingsDialog : Dialog
	{
		private const int TilesPerKind = 11;

		private const int Lanes = 2;

		private const float WrapperBottomPadding = 8f;

		private const float TabChooserSpan = 0.2f;

		[Header("PREFABS")]
		public ActorTile actorTileTemplate;

		public ScrollRect scrollBox;

		private ScrollRect.MovementType _scrollMovement;

		private bool _scrollMovementCached;

		private Transform _popupFrame;

		private Coroutine _scrollHold;

		private Action _settleScroll;

		private int _activeTab;

		public RectTransform[] actorWrappers;

		public Button[] tabControls;

		public TextMeshProUGUI[] tabControlTags;

		public Graphic[] tabControlGlyphs;

		public Color[] tabStripControlTints;

		public GameObject tabChooser;

		private float[] wrapperVerticalExtents;

		private readonly float tileBeginX = 5f;

		private readonly float tileBeginY = -305f;

		private readonly float tileBreadth = 382f;

		private readonly float tileVerticalExtent = 603f;

		private readonly float tileSpacing = 18f;

		private Dictionary<string, ActorTile> tileCatalog;

		private bool _settleTabChooser;

		private int liveTabOrdinal
		{
			set
			{
				ChooseTab(value);
			}
		}

		public override void Present(Action<eUIName> dialogDismissedSignalHook = null)
		{
			if (tileCatalog == null)
			{
				AssembleTiles();
			}
			RenewTiles();
			_settleTabChooser = true;
			liveTabOrdinal = 1;
			_settleTabChooser = false;
			base.Present(dialogDismissedSignalHook);
			HoldScrollWhileOpening();
		}

		private void HoldScrollWhileOpening()
		{
			if (scrollBox == null || !isActiveAndEnabled)
			{
				return;
			}
			if (!_scrollMovementCached)
			{
				_scrollMovement = scrollBox.movementType;
				_scrollMovementCached = true;
			}
			if (_popupFrame == null)
			{
				_popupFrame = transform.Find("Content");
			}
			if (_settleScroll == null)
			{
				_settleScroll = SettleActiveTab;
			}
			if (_scrollHold != null)
			{
				StopCoroutine(_scrollHold);
			}
			_scrollHold = StartCoroutine(Toolbox.HoldScrollWhileGrowing(scrollBox, _popupFrame, _scrollMovement, _settleScroll));
		}

		private void SettleActiveTab()
		{
			_scrollHold = null;
			Toolbox.ScrollToTop(scrollBox);
			SettleOnFreshestTile(_activeTab == 0 ? ActorKind.Person : ActorKind.Saurian);
		}

		public override void ControlResponder(Button dispatcher)
		{
			if (dispatcher == null)
			{
				return;
			}
			switch (dispatcher.name)
			{
			case "HumanTabBtn":
				liveTabOrdinal = 0;
				break;
			case "DinoTabBtn":
				liveTabOrdinal = 1;
				break;
			case "CloseBtn":
				Conceal();
				break;
			default:
				base.ControlResponder(dispatcher);
				break;
			}
		}

		private void AssembleTiles()
		{
			tileCatalog = new Dictionary<string, ActorTile>();
			int wrapperTally = actorWrappers != null ? actorWrappers.Length : 0;
			wrapperVerticalExtents = new float[Mathf.Max(wrapperTally, 3)];
			if (actorTileTemplate == null || wrapperTally < 2)
			{
				return;
			}
			GameplayCore matchCore = uiDirector != null ? uiDirector.gameplayCore : null;
			ActorKind[] kinds = { ActorKind.Person, ActorKind.Saurian };
			for (int wrapperIdx = 0; wrapperIdx < kinds.Length; wrapperIdx++)
			{
				RectTransform wrapper = actorWrappers[wrapperIdx];
				if (wrapper == null)
				{
					continue;
				}
				ActorKind actorKind = kinds[wrapperIdx];
				string[] titles = matchCore == null ? null : (actorKind == ActorKind.Saurian ? matchCore.saurianTitles : matchCore.personTitles);
				for (int stage = 1; stage <= TilesPerKind; stage++)
				{
					int idx = stage - 1;
					ActorTile tile = Instantiate(actorTileTemplate, wrapper);
					tile.name = actorTileTemplate.name + "-" + actorKind.ToLabel() + "-" + stage;
					RectTransform box = (RectTransform)tile.transform;
					box.anchoredPosition = new Vector2(tileBeginX + (idx % Lanes) * (tileBreadth + tileSpacing), tileBeginY - (idx / Lanes) * (tileVerticalExtent + tileSpacing));
					Sprite picture = MatchAssets.Shared != null ? MatchAssets.Shared.GetActorPicture(actorKind, 1 << idx) : null;
					string actorTitle = titles != null && idx < titles.Length && titles[idx] != null ? titles[idx].ToUpper() : string.Empty;
					tile.Assemble(picture, actorTitle, actorKind, stage, TilesPerKind);
					tileCatalog[TileIdentifier(actorKind, stage)] = tile;
				}
				int lines = Mathf.CeilToInt(TilesPerKind / (float)Lanes);
				wrapperVerticalExtents[wrapperIdx] = lines * (tileVerticalExtent + tileSpacing) + WrapperBottomPadding;
				wrapper.sizeDelta = new Vector2(wrapper.sizeDelta.x, wrapperVerticalExtents[wrapperIdx]);
			}
		}

		private void RenewTiles()
		{
			if (tileCatalog == null)
			{
				return;
			}
			foreach (ActorKind actorKind in new[] { ActorKind.Person, ActorKind.Saurian })
			{
				// B9/C6: a tier is unlocked only once a unit of it exists (fresh save: no warrior -> Blackhawk shows "Locked").
				int peakStage = GetPeakStage(actorKind);
				for (int stage = 1; stage <= TilesPerKind; stage++)
				{
					if (tileCatalog.TryGetValue(TileIdentifier(actorKind, stage), out ActorTile tile) && tile != null)
					{
						tile.ReviseStatus(stage <= peakStage);
					}
				}
			}
		}

		private void ChooseTab(int ordinal)
		{
			if (actorWrappers == null || actorWrappers.Length == 0)
			{
				return;
			}
			ordinal = Mathf.Clamp(ordinal, 0, actorWrappers.Length - 1);
			_activeTab = ordinal;
			for (int idx = 0; idx < actorWrappers.Length; idx++)
			{
				if (actorWrappers[idx] != null)
				{
					actorWrappers[idx].gameObject.SetActive(idx == ordinal);
				}
			}
			if (tabControlTags != null && tabStripControlTints != null && tabStripControlTints.Length >= 2)
			{
				for (int idx = 0; idx < tabControlTags.Length; idx++)
				{
					if (tabControlTags[idx] != null)
					{
						tabControlTags[idx].color = idx == ordinal ? tabStripControlTints[0] : tabStripControlTints[1];
					}
				}
				if (tabControlGlyphs != null)
				{
					for (int idx = 0; idx < tabControlGlyphs.Length; idx++)
					{
						if (tabControlGlyphs[idx] != null)
						{
							tabControlGlyphs[idx].color = idx == ordinal ? Color.white : tabStripControlTints[1];
						}
					}
				}
			}
			if (tabChooser != null && tabControls != null && ordinal < tabControls.Length && tabControls[ordinal] != null)
			{
				RectTransform chooser = (RectTransform)tabChooser.transform;
				RectTransform tabBox = (RectTransform)tabControls[ordinal].transform;
				chooser.DOKill();
				if (gameObject.activeInHierarchy && !_settleTabChooser)
				{
					chooser.DOAnchorPosX(tabBox.anchoredPosition.x, TabChooserSpan);
				}
				else
				{
					chooser.anchoredPosition = new Vector2(tabBox.anchoredPosition.x, chooser.anchoredPosition.y);
				}
			}
			if (scrollBox != null && actorWrappers[ordinal] != null)
			{
				scrollBox.content = actorWrappers[ordinal];
				Toolbox.ScrollToTop(scrollBox);
				SettleOnFreshestTile(ordinal == 0 ? ActorKind.Person : ActorKind.Saurian);
			}
		}

		private void SettleOnFreshestTile(ActorKind actorKind)
		{
			int peakStage = Mathf.Min(GetPeakStage(actorKind), TilesPerKind);
			RectTransform view = scrollBox.viewport != null ? scrollBox.viewport : (RectTransform)scrollBox.transform;
			RectTransform content = scrollBox.content;
			float range = content.rect.height - view.rect.height;
			if (peakStage <= Lanes || range <= 0f)
			{
				return;
			}
			float offset = Mathf.Min(((peakStage - 1) / Lanes) * (tileVerticalExtent + tileSpacing), range);
			scrollBox.StopMovement();
			if (Mathf.Approximately(content.anchorMin.y, 1f) && Mathf.Approximately(content.anchorMax.y, 1f))
			{
				content.anchoredPosition = new Vector2(content.anchoredPosition.x, offset - (1f - content.pivot.y) * content.rect.height);
			}
			else
			{
				scrollBox.verticalNormalizedPosition = 1f - offset / range;
			}
		}

		private static int GetPeakStage(ActorKind actorKind)
		{
			if (PlayMechanics.peakUnsealedActors != null && PlayMechanics.peakUnsealedActors.TryGetValue(actorKind, out int peakWorth))
			{
				return Mathf.Max(0, PlayMechanics.GetActorGrade(peakWorth));
			}
			return 0;
		}

		private static string TileIdentifier(ActorKind actorKind, int stage)
		{
			return actorKind + "-" + stage;
		}
	}
}
