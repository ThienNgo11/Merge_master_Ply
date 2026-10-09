using System;
using GameTool.Assistants.DesignPattern;
using GameToolSample.Scripts.UI.ResourcesItems;
using UnityEngine;

namespace GameToolSample.GameConfigScripts
{
    public class GameConfig : SingletonMonoBehaviour<GameConfig>
    {
        [SerializeField] private ItemResourceData _itemResourceData;
        [SerializeField] private IAPConfig _iapConfig;

        [Header("CONFIG")]
        [SerializeField] private int _totalLevel = 100;

        public ItemResourceData ItemResourceData => _itemResourceData;
        public IAPConfig IAPConfig => _iapConfig;
        public int TotalLevel => _totalLevel;
    }

    [Serializable]
    public class IAPConfig { }
}