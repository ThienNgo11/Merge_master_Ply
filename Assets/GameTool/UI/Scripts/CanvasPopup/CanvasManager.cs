using System;
using System.Collections.Generic;
using System.Linq;
using GameTool.Assistants.DesignPattern;
using GameToolSample.Scripts.Enum;
using GameToolSample.UIManager;
using UnityEngine;
using UnityEngine.UI;

namespace GameTool.UI.Scripts.CanvasPopup
{
    public class CanvasManager : SingletonMonoBehaviour<CanvasManager>
    {
        public Canvas canvas;
        public CanvasScaler canvasScaler;
        public float aspect { get; private set; }

        private readonly Dictionary<eUIName, List<BaseUI>> ListUI = new Dictionary<eUIName, List<BaseUI>>();
        private readonly Dictionary<eUIName, Stack<BaseUI>> DictUIDisabled = new Dictionary<eUIName, Stack<BaseUI>>();
        private readonly Dictionary<eUIType, Transform> DictMenu = new Dictionary<eUIType, Transform>();

        public CanvasPrefAssets Serializers;

        public readonly Dictionary<eUIName, BaseUI> DictUIPref = new Dictionary<eUIName, BaseUI>();
        public readonly Dictionary<eUIName, eUIType> DictUIType = new Dictionary<eUIName, eUIType>();

        public List<string> StartMenus
        {
            get { return startMenu; }
            set { startMenu = value; }
        }

        [SerializeField] private List<string> startMenu;

        private void OnValidate()
        {
            canvas = GetComponent<Canvas>();
            canvasScaler = GetComponent<CanvasScaler>();
        }

        protected override void Awake()
        {
            base.Awake();

            UpdateKey();

            aspect = (float)Screen.width / Screen.height;

            if (Serializers)
            {
                for (int i = 0; i < Serializers.uiAsset.Count; i++)
                {
                    DictUIDisabled.Add(Serializers.uiAsset[i].key, new Stack<BaseUI>());
                    ListUI.Add(Serializers.uiAsset[i].key, new List<BaseUI>());
                }
            }

            string[] typeNames = Resources.Load<CanvasPrefTable>("CanvasPrefTable").ListLayer.Select(type => type.ToString()).ToArray();

            for (int i = 0; i < typeNames.Length; i++)
            {
                GameObject obj = new GameObject(typeNames[i], typeof(RectTransform));
                obj.transform.SetParent(transform);
                obj.transform.position = transform.position;
                obj.transform.localScale = Vector3.one;
                obj.transform.localRotation = Quaternion.identity;
                SetFullRect(obj);
                DictMenu.Add((eUIType)Enum.Parse(typeof(eUIType), typeNames[i]), obj.transform);
            }
        }

        private void Start()
        {
            foreach (var menu in startMenu)
            {
                if (Enum.TryParse(menu, out eUIName result))
                {
                    if (result != eUIName.None)
                    {
                        Push(result);
                    }
                }
                else
                {
                    Debug.LogError($"Menu {menu} is not any eUIName");
                }
            }

            this.PostEvent(EventID.UpdateCanvas);
        }

        public BaseUI Push(eUIName identifier, object[] args = null)
        {
            UpdateUIAsset(identifier);
            BaseUI uiReturn;
            if (DictUIDisabled[identifier].Count > 0)
            {
                uiReturn = DictUIDisabled[identifier].Pop();
                if (uiReturn)
                {
                    uiReturn.gameObject.SetActive(true);
                    GotoTop(uiReturn);
                    uiReturn.Init(args);
                    return uiReturn;
                }
            }

            uiReturn = Instantiate(DictUIPref[identifier], DictMenu[DictUIType[identifier]]);
            ListUI[identifier].Add(uiReturn);
            uiReturn.uiName = identifier;
            uiReturn.uiType = DictUIType[identifier];
            GotoTop(uiReturn);
            uiReturn.Init(args);
            return uiReturn;
        }

        public void Pop(BaseUI ui)
        {
            ui.gameObject.SetActive(false);
            DictUIDisabled[ui.uiName].Push(ui);
        }

        public void Pop(eUIName identifier)
        {
            UpdateUIAsset(identifier);
            var ui = ListUI[identifier].Find(_ui => _ui.uiName == identifier && _ui.gameObject.activeSelf);
            if (ui)
            {
                ui.Pop();
            }
        }

        public void Pop(eUIType identifier)
        {
            for (int i = 0; i < ListUI.Count; i++)
            {
                BaseUI popup = ListUI.ElementAt(i).Value
                    .Find(ui => ui && ui.uiType == identifier && ui.gameObject.activeSelf);
                
                if (popup)
                {
                    popup.Pop();
                }
            }
        }

        public BaseUI FindEnabling(eUIName identifier)
        {
            UpdateUIAsset(identifier);
            if (ListUI.TryGetValue(identifier, out var list))
            {
                return list.Find(ui => ui.uiName == identifier && ui.gameObject.activeSelf);
            }
            else
            {
                return null;
            }
        }

        public bool IsShowing(eUIName identifier)
        {
            UpdateUIAsset(identifier);
            BaseUI ui = FindEnabling(identifier);
            return ui != null;
        }

        public bool IsHaveUIShowing(eUIType UIType)
        {
            for (int i = 0; i < ListUI.Count; i++)
            {
                BaseUI popup = ListUI.ElementAt(i).Value.Find(ui => ui.uiType == UIType && ui.gameObject.activeSelf);

                if (popup)
                {
                    return true;
                }
            }

            return false;
        }

        private void GotoTop(BaseUI baseUi)
        {
            baseUi.transform.SetSiblingIndex(baseUi.transform.parent.childCount - 1);
        }

        private void SetFullRect(GameObject _obj)
        {
            RectTransform _rect = _obj.GetComponent<RectTransform>();

            _rect.anchorMin = Vector2.zero;
            _rect.anchorMax = Vector2.one;

            _rect.pivot = Vector2.one / 2;

            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }


        private void UpdateKey()
        {
            DictUIPref.Clear();
            if (Serializers)
            {
                foreach (var serializer in Serializers.uiAsset)
                {
                    if (!DictUIPref.ContainsKey(serializer.key))
                    {
                        DictUIPref.Add(serializer.key, serializer.ui);
                        DictUIType.Add(serializer.key, serializer.type);
                    }
                }
            }
        }

        private void UpdateUIAsset(eUIName filename)
        {
            if (!DictUIPref.ContainsKey(filename))
            {
                var table = Resources.Load<CanvasPrefTable>("CanvasPrefTable");
                var settingUI = table.Serializers.Find(serializer => serializer.key == filename.ToString()).settingUI;
                var path = settingUI.resourcePath;
                var ui = Resources.Load<BaseUI>(path);
                DictUIPref.Add(filename, ui);
                DictUIType.Add(filename, settingUI.UIType);
                DictUIDisabled.Add(filename, new Stack<BaseUI>());
                ListUI.Add(filename, new List<BaseUI>());
            }
        }
    }
}