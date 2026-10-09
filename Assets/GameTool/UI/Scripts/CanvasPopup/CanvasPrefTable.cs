using System;
using System.Collections.Generic;
using System.Linq;
using DatdevUlts.InspectorUtils;
using GameToolSample.UIManager;
using UnityEditor;
using UnityEngine;


namespace GameTool.UI.Scripts.CanvasPopup
{
    [CreateAssetMenu(fileName = "CanvasPrefTable", menuName = "ScriptableObject/CanvasPrefTable", order = 0)]
    public class CanvasPrefTable : ScriptableObject
    {
        public List<eUIType> ListLayer = new List<eUIType>();
        public List<UISerializer> Serializers = new List<UISerializer>();

#if UNITY_EDITOR
        public void OnValidate()
        {
            for (int i = 0; i < Serializers.Count; i++)
            {
                UISerializer uiSerializer = Serializers[i];
                if (uiSerializer.settingUI.baseUI)
                {
                    uiSerializer.key = uiSerializer.settingUI.baseUI.name.Trim().Replace(" ","");
                }
                else
                {
                    uiSerializer.key = "";
                }
                uiSerializer.settingUI.resourcePath = GetResPath(uiSerializer.settingUI.baseUI);

                if (Serializers.Count(serializer => serializer.key == uiSerializer.key) >= 2)
                {
                    uiSerializer.isDuplicatedKey = true;
                }
                else
                {
                    uiSerializer.isDuplicatedKey = false;
                }
            }
            EditorUtility.SetDirty(this);
        }
        
        public string GetResPath(BaseUI baseUI)
        {
            var str = AssetDatabase.GetAssetPath(baseUI);
            var index = str.LastIndexOf("Resources", StringComparison.Ordinal);
            if (index >= 0)
            {
                str = str.Substring(index);
                str = str.Remove(0, "Resources/".Length);
                
                index = str.LastIndexOf(".", StringComparison.Ordinal);
                str = str.Remove(index);

                return str;
            }

            return "";
        }
#endif
    }

    [Serializable]
    public class SettingUI
    {
        public eUIType UIType;
#if UNITY_EDITOR
        public BaseUI baseUI;
#endif
        [HideInInspector] public string resourcePath;
    }

    [Serializable]
    public class UISerializer
    {
        public string key;
        public SettingUI settingUI;
        #if UNITY_EDITOR
        [HideInNormalInspector] public bool isDuplicatedKey;
        #endif
    }
}