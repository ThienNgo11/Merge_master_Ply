using System;
using System.Collections.Generic;
using GameTool.Assistants.DesignPattern;

namespace LunaTool
{
    public class LunaAPIBase : SingletonMonoBehaviour<LunaAPIBase>
    {
        public string AndroidLinkGame;
        public string IOSLinkGame;

        private List<IAutoUpdate> _listUpdateObject = new List<IAutoUpdate>();

        public List<IAutoUpdate> ListAutoUpdateObject => _listUpdateObject;
        private bool IsGameEnded;

        protected override void Awake()
        {
            Luna.Unity.LifeCycle.OnPause += PauseGameplay;
            Luna.Unity.LifeCycle.OnResume += ResumeGameplay;
            Luna.Unity.LifeCycle.GameStarted();
        }

        public virtual void Update()
        {
            foreach (var autoUpdate in ListAutoUpdateObject)
            {
                var update = autoUpdate as IUpdate;
                if (update != null && update.CanUpdate)
                {
                    update.OnUpdate();
                }
            }
        }

        public virtual void FixedUpdate()
        {
            foreach (var autoUpdate in ListAutoUpdateObject)
            {
                var update = autoUpdate as IFixedUpdate;
                if (update != null && update.CanUpdate)
                {
                    update.OnFixedUpdate();
                }
            }
        }

        public void LateUpdate()
        {
            foreach (var autoUpdate in ListAutoUpdateObject)
            {
                var update = autoUpdate as ILateUpdate;
                if (update != null && update.CanUpdate)
                {
                    update.OnLateUpdate();
                }
            }
        }

        public void RegisUpdate(IUpdate update)
        {
#if UNITY_EDITOR
            if (ListAutoUpdateObject.Contains(update))
            {
                throw new ArgumentException("Object đã có trong List update");
            }
#endif
            ListAutoUpdateObject.Add(update);
        }

        public void RemoveUpdate(IUpdate update)
        {
            ListAutoUpdateObject.Remove(update);
        }

        protected virtual void PauseGameplay()
        {
        }

        protected virtual void ResumeGameplay()
        {
        }

        public virtual void InstallFullGame()
        {
            if (!IsGameEnded)
            {
                IsGameEnded = true;
                GameEnded();
            }
            
            Luna.Unity.Playable.InstallFullGame(IOSLinkGame, AndroidLinkGame);
        }

        public virtual void GameEnded()
        {
            Luna.Unity.LifeCycle.GameEnded();
        }
    }

    public interface IAutoUpdate
    {
        bool CanUpdate { get; }
    }

    public interface IUpdate : IAutoUpdate
    {
        void OnUpdate();
    }

    public interface IFixedUpdate : IAutoUpdate
    {
        void OnFixedUpdate();
    }

    public interface ILateUpdate : IAutoUpdate
    {
        void OnLateUpdate();
    }
}