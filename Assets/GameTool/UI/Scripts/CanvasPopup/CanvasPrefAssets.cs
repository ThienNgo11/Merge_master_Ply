using System;
using System.Collections.Generic;
using GameToolSample.UIManager;
using UnityEditor;
using UnityEngine;

namespace GameTool.UI.Scripts.CanvasPopup
{
    [CreateAssetMenu(fileName = "CanvasPrefAssets", menuName = "ScriptableObject/CanvasPrefAssets", order = 0)]
    public class CanvasPrefAssets : ScriptableObject
    {
        public List<CanvasPrefAssetItem> uiAsset;
        
#if UNITY_EDITOR
        [ContextMenu("Re Update")]
        public void OnValidate()
        {
            var table = Resources.Load<CanvasPrefTable>("CanvasPrefTable");
            for (int i = 0; i < uiAsset.Count; i++)
            {
                try
                {
                    uiAsset[i].ui = table.Serializers
                        .Find(ui => ui.key == uiAsset[i].key.ToString()).settingUI.baseUI;
                }
                catch
                {
                    uiAsset[i].ui = null;
                }
            }
            EditorUtility.SetDirty(this);
        }
#endif
    }
    
    [Serializable]
    public class CanvasPrefAssetItem
    {
        public eUIName key;
        public eUIType type;
        public BaseUI ui;
    }
}
