using System.Linq;
using DatdevUlts.Ults;
using GameTool.Assistants.DesignPattern;
using GameTool.UI.Scripts.CanvasPopup;
using GameToolSample.Scripts.Enum;
using GameToolSample.UIManager;
using Luna.Unity;
using LunaTool;
using UnityEngine;

namespace GameToolSample.GamePlay.Manager
{
    public class GameManager : LunaAPIBase
    {
        public new static GameManager Instance => (GameManager)LunaAPIBase.Instance;
        [Header("COMPONENT")] [SerializeField] private GameModeName _gameMode = GameModeName.none;
        [SerializeField] private GamePlayState _gameplayStatus = GamePlayState.none;

        public GameModeName GameMode
        {
            get => _gameMode;
            set => _gameMode = value;
        }

        public GamePlayState GameplayStatus
        {
            get => _gameplayStatus;
            set => _gameplayStatus = value;
        }


        [Header("START")] [Tooltip("-1 is disable")] [SerializeField]
        private int _autoStartDelay = -1;

        public int AutoStartDelay
        {
            get => _autoStartDelay;
            set => _autoStartDelay = value;
        }

        [Header("VICTORY")] [Tooltip("-1 is disable")] [SerializeField]
        private float _delayShowPopupVictory;

        public float DelayShowPopupVictory
        {
            get => _delayShowPopupVictory;
            set => _delayShowPopupVictory = value;
        }


        [Header("LOSE")] [Tooltip("-1 is disable")] [SerializeField]
        private float _delayShowPopupLose;
        private float _delayShowPopupLoseAfterDenyRevive;

        public float DelayShowPopupLose
        {
            get => _delayShowPopupLose;
            set => _delayShowPopupLose = value;
        }

        public float DelayShowPopupLoseAfterDenyRevive
        {
            get => _delayShowPopupLoseAfterDenyRevive;
            set => _delayShowPopupLoseAfterDenyRevive = value;
        }


        [Header("REVIVE")] [SerializeField] private bool _canRevive = true;

        [Tooltip("-1 is infinity")] [SerializeField]
        private int _amountRevive = -1;

        [Tooltip("-1 is disable")] [SerializeField]
        private float _delayShowPopupRevive;

        private bool _shownEC;


        public bool CanRevive
        {
            get => _canRevive && _amountRevive != 0;
            set => _canRevive = value;
        }

        public int AmountRevive
        {
            get => _amountRevive;
            set => _amountRevive = value;
        }

        public float DelayShowPopupRevive
        {
            get => _delayShowPopupRevive;
            set => _delayShowPopupRevive = value;
        }

        [SerializeField] private int _currentLevel;

        public int CurrentLevel
        {
            get => _currentLevel;
            set => _currentLevel = value;
        }

        protected virtual void Start()
        {
            if (_autoStartDelay >= 0)
            {
                this.DelayedCall(_autoStartDelay, PlayGame);
            }
        }

        public bool IsGamePlayStatus(params GamePlayState[] gamePlayState)
        {
            return gamePlayState.Contains(_gameplayStatus);
        }

        #region STATUS INGAME

        public virtual void PlayGame()
        {
            if (IsGamePlayStatus(GamePlayState.playing))
            {
                return;
            }
            _gameplayStatus = GamePlayState.playing;
            TrackingPlaygame();
        }

        public virtual void Victory()
        {
            if (IsGamePlayStatus(GamePlayState.victory))
            {
                return;
            }
            _gameplayStatus = GamePlayState.victory;
            ShowVictoryPopup();
            TrackingVictory();
        }

        protected virtual void ShowVictoryPopup()
        {
            if (DelayShowPopupVictory >= 0)
            {
                this.DelayedCall(DelayShowPopupVictory, () => { CanvasManager.Instance.Push(eUIName.VictoryPopup); });
            }
        }

        public virtual void Lose()
        {
            if (CanRevive)
            {
                CheckRevive();
            }
            else
            {
                _gameplayStatus = GamePlayState.lose;
                ShowLosePopup();
                TrackingLoseEnd();
            }
        }

        public virtual void ShowEndcard()
        {
            if (_shownEC)
            {
                return;
            }

            _shownEC = true;
            CanvasManager.Instance.Push(eUIName.EndCard);
            TrackingEndCard();
        }

        protected virtual void ShowLosePopup()
        {
            if (DelayShowPopupLose >= 0)
            {
                this.DelayedCall(DelayShowPopupLose, () => { CanvasManager.Instance.Push(eUIName.LosePopup); });
            }
        }

        public virtual void CheckRevive()
        {
            if (_gameplayStatus == GamePlayState.revive) return;
            _gameplayStatus = GamePlayState.revive;
            ShowRevivePopup();
            TrackingLose();
        }

        public virtual void Revive()
        {
            _gameplayStatus = GamePlayState.playing;
            _amountRevive--;
            this.PostEvent(EventID.PlayerRevive);
        }

        protected virtual void ShowRevivePopup()
        {
            if (DelayShowPopupRevive >= 0)
            {
                this.DelayedCall(DelayShowPopupRevive, () => { CanvasManager.Instance.Push(eUIName.RevivePopup); });
            }
        }

        public virtual void SkipLevel()
        {
            TrackingSkip();
        }

        public virtual void ReplayLevel()
        {
            TrackingReplay();
        }

        public void DenyRevive()
        {
            CanRevive = false;
            DelayShowPopupLose = DelayShowPopupLoseAfterDenyRevive;
            Lose();
        }

        #endregion

        #region TRACKING

        protected virtual void TrackingPlaygame()
        {
            TrackingMngr.Instance.TrackEvent(Analytics.EventType.LevelStart, (int)Analytics.EventType.LevelStart);
        }

        protected virtual void TrackingVictory()
        {
            TrackingMngr.Instance.TrackEvent(Analytics.EventType.LevelWon, (int)Analytics.EventType.LevelWon);
        }

        protected virtual void TrackingLoseEnd()
        {
            TrackingMngr.Instance.TrackEvent(Analytics.EventType.LevelFailed, (int)Analytics.EventType.LevelFailed);
        }

        protected virtual void TrackingEndCard()
        {
            TrackingMngr.Instance.TrackEvent(Analytics.EventType.EndCardShown, (int)Analytics.EventType.EndCardShown);
        }

        protected virtual void TrackingLose()
        {
            TrackingMngr.Instance.TrackEvent(Analytics.EventType.LevelFailed, (int)Analytics.EventType.LevelFailed);
        }

        protected virtual void TrackingSkip()
        {
            TrackingMngr.Instance.TrackEvent(Analytics.EventType.LevelFailed, (int)Analytics.EventType.LevelFailed);
        }

        protected virtual void TrackingReplay()
        {
            TrackingMngr.Instance.TrackEvent(Analytics.EventType.LevelRetry, (int)Analytics.EventType.LevelRetry);
        }

        #endregion
    }
}