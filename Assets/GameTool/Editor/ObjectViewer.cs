using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;

using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GameTool.Editor
{
    public class ObjectViewer : EditorWindow
    {
        public bool viewMore;
        public bool viewTextureSize;
        public bool viewParent = true;
        public GameObject currentObject;
        public List<ComponentInfo> allList = new List<ComponentInfo>();
        public List<ComponentInfo> dislayList = new List<ComponentInfo>();
        public string strPathToMove = string.Empty;

        private Vector2 scroll;
        private string[] tabs;
        private int tabIndex;
        private string[] search;
        private bool initDone;
        private ObjectViewerStyle _style;
        private List<string> _sortingLayerNames;

        private class ObjectViewerStyle
        {
            public GUILayoutOption iconW;
            public GUILayoutOption iconH;
            public GUILayoutOption expandWidthFalse = GUILayout.ExpandWidth(false);
            public GUILayoutOption color = GUILayout.Width(60);
            public GUIStyle objectPathStyle;
            public GUIStyle parent;
            public GUIStyle parentGroup;
            public GUIStyle toogle;

            public void InitStyle()
            {
                iconW = GUILayout.Width(60);
                iconH = GUILayout.Height(40);
                objectPathStyle = new GUIStyle()
                {
                    normal =
                    {
                        textColor = new Color(.6f, .6f, .6f, .8f)
                    },
                    fontSize = 10,
                    padding = new RectOffset(5, 0, 0, 0)
                };
                parent = new GUIStyle(GUI.skin.box)
                {
                    normal =
                    {
                        textColor = new Color(.6f, .6f, .6f, .8f)
                    },
                    fontSize = 10,
                    padding = new RectOffset(5, 5, 5, 5)
                };
                parentGroup = new GUIStyle()
                {
                    margin = new RectOffset(5, 5, 5, 5)
                };
                toogle = new GUIStyle(GUI.skin.toggle)
                {
                    normal =
                    {
                        textColor = new Color(.6f, .6f, .6f, .8f)
                    },
                    hover =
                    {
                        textColor = new Color(.6f, .6f, .6f, .8f)
                    },
                };
            }
        }

        [MenuItem("Tools/Object Viewer")]
        public static void Init()
        {
            var w = GetWindow<ObjectViewer>("Object Viewer");
            w.Show();
        }

        private void OnEnable()
        {
            tabs = new[] { "Image", "Text", "Button", "Mask", "SpriteRenderer" };
            search = new[] { "", "", "", "", "" };
            if (currentObject != null)
            {
                Find(currentObject);
            }

            _sortingLayerNames = GetSortingLayerNames().ToList();
        }

        private void OnGUI()
        {
            if (!initDone || _style == null)
            {
                _style = new ObjectViewerStyle();
                _style.InitStyle();
                initDone = true;
            }

            GUILayout.BeginHorizontal();
            var obj = EditorGUILayout.ObjectField(currentObject, typeof(GameObject), true) as GameObject;
            viewParent = GUILayout.Toggle(viewParent, "Parent", _style.expandWidthFalse);
            viewTextureSize = GUILayout.Toggle(viewTextureSize, "Texture Size", _style.expandWidthFalse);
            viewMore = GUILayout.Toggle(viewMore, "Show More", _style.expandWidthFalse);
            if (GUILayout.Button("Refresh", _style.expandWidthFalse))
            {
                Find(currentObject);
                _sortingLayerNames = GetSortingLayerNames().ToList();
            }

            GUILayout.EndHorizontal();
            if (obj != currentObject)
            {
                currentObject = obj;
                Find(currentObject);
            }

            UpdateDragAndDrop();
            var tab = GUILayout.Toolbar(tabIndex, tabs);
            if (tab != tabIndex)
            {
                tabIndex = tab;
                Find(currentObject);
                EditorGUI.FocusTextInControl("");
            }

            if (currentObject != null)
            {
                GUILayout.BeginHorizontal();
                var strSearch = EditorGUILayout.TextField(search[tabIndex]);
                if (strSearch != search[tabIndex])
                {
                    search[tabIndex] = strSearch;
                    Filter();
                }

                GUILayout.Label($"{dislayList.Count}/{allList.Count}", GUILayout.Width(60));
                GUILayout.EndHorizontal();

                scroll = GUILayout.BeginScrollView(scroll);
                if (tabs[tabIndex] == tabs[0])
                {
                    ViewImages();
                }
                else if (tabs[tabIndex] == tabs[1])
                {
                    ViewTexts();
                }
                else if (tabs[tabIndex] == tabs[2])
                {
                    ViewButtons();
                }
                else if (tabs[tabIndex] == tabs[3])
                {
                    ViewMasks();
                }
                else if (tabs[tabIndex] == tabs[4])
                {
                    ViewSpriteRenderer();
                }

                GUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label("Please select object", EditorStyles.helpBox);
            }
        }

        private void ViewTexts()
        {
            Event e = Event.current;
            foreach (ComponentInfo cpInfo in dislayList)
            {
                if (cpInfo == null) return;
                GUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUILayout.Label(cpInfo.component.name, GUILayout.Width(100));
                var rectName = GUILayoutUtility.GetLastRect();
                if ((e.type == EventType.MouseUp || e.type == EventType.DragPerform) && rectName.Contains(e.mousePosition))
                {
                    if (e.type == EventType.DragPerform)
                    {
                        if (DragAndDrop.objectReferences.Length > 0)
                        {
                            foreach (Object obj in DragAndDrop.objectReferences)
                            {
                                if (cpInfo.component is TextMeshProUGUI textMeshProUGui)
                                {
                                    if (obj is Material m)
                                    {
                                        textMeshProUGui.fontMaterial = m;
                                    }
                                    else if (obj is TMP_FontAsset font)
                                    {
                                        textMeshProUGui.font = font;
                                    }
                                }
                                else if (cpInfo.component is TextMeshPro textMeshPro)
                                {
                                    if (obj is Material m)
                                    {
                                        textMeshPro.fontMaterial = m;
                                    }
                                    else if (obj is TMP_FontAsset font)
                                    {
                                        textMeshPro.font = font;
                                    }
                                }
                                else if (cpInfo.component is Text text)
                                {
                                    if (obj is Material m)
                                    {
                                        text.material = m;
                                    }
                                    else if (obj is Font font)
                                    {
                                        text.font = font;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Ping(cpInfo.component);
                    }
                }

                GUILayout.BeginVertical();
                ViewParents(cpInfo);
                if (cpInfo.component is TextMeshProUGUI textMeshProUGUI)
                {
                    var text = EditorGUILayout.TextField(textMeshProUGUI.text);
                    if (text != textMeshProUGUI.text)
                    {
                        textMeshProUGUI.text = text;
                        EditorUtility.SetDirty(textMeshProUGUI);
                    }

                    if (viewMore)
                    {
                        GUILayout.BeginHorizontal();
                        var font = (TMP_FontAsset)EditorGUILayout.ObjectField(textMeshProUGUI.font, typeof(TMP_FontAsset),
                            true, _style.expandWidthFalse);
                        if (font != textMeshProUGUI.font)
                        {
                            textMeshProUGUI.font = font;
                            EditorUtility.SetDirty(textMeshProUGUI);
                        }

                        var material =
                            (Material)EditorGUILayout.ObjectField(textMeshProUGUI.material, typeof(Material), true,
                                _style.expandWidthFalse);
                        if (material != textMeshProUGUI.material)
                        {
                            textMeshProUGUI.material = material;
                            EditorUtility.SetDirty(textMeshProUGUI);
                        }

                        var enable = GUILayout.Toggle(textMeshProUGUI.enabled, "Enable", _style.expandWidthFalse);
                        if (enable != textMeshProUGUI.enabled)
                        {
                            textMeshProUGUI.enabled = enable;
                            EditorUtility.SetDirty(textMeshProUGUI);
                        }

                        var raycastTarget = GUILayout.Toggle(textMeshProUGUI.raycastTarget, "Raycast Target",
                            _style.expandWidthFalse);
                        if (raycastTarget != textMeshProUGUI.raycastTarget)
                        {
                            textMeshProUGUI.raycastTarget = raycastTarget;
                            EditorUtility.SetDirty(textMeshProUGUI);
                        }

                        GUILayout.EndHorizontal();
                    }
                }
                else if (cpInfo.component is TextMeshPro textMeshPro)
                {
                    var text = EditorGUILayout.TextField(textMeshPro.text);
                    if (text != textMeshPro.text)
                    {
                        textMeshPro.text = text;
                        EditorUtility.SetDirty(textMeshPro);
                    }

                    if (viewMore)
                    {
                        GUILayout.BeginHorizontal();
                        var font = (TMP_FontAsset)EditorGUILayout.ObjectField(textMeshPro.font, typeof(TMP_FontAsset),
                            true, _style.expandWidthFalse);
                        if (font != textMeshPro.font)
                        {
                            textMeshPro.font = font;
                            EditorUtility.SetDirty(textMeshPro);
                        }

                        var material =
                            (Material)EditorGUILayout.ObjectField(textMeshPro.material, typeof(Material), true,
                                _style.expandWidthFalse);
                        if (material != textMeshPro.material)
                        {
                            textMeshPro.material = material;
                            EditorUtility.SetDirty(textMeshPro);
                        }

                        var enable = GUILayout.Toggle(textMeshPro.enabled, "Enable", _style.expandWidthFalse);
                        if (enable != textMeshPro.enabled)
                        {
                            textMeshPro.enabled = enable;
                            EditorUtility.SetDirty(textMeshPro);
                        }

                        var raycastTarget = GUILayout.Toggle(textMeshPro.raycastTarget, "Raycast Target",
                            _style.expandWidthFalse);
                        if (raycastTarget != textMeshPro.raycastTarget)
                        {
                            textMeshPro.raycastTarget = raycastTarget;
                            EditorUtility.SetDirty(textMeshPro);
                        }

                        GUILayout.EndHorizontal();
                    }
                }
                else if (cpInfo.component is Text legacyText)
                {
                    var text = EditorGUILayout.TextField(legacyText.text);
                    if (text != legacyText.text)
                    {
                        legacyText.text = text;
                        EditorUtility.SetDirty(legacyText);
                    }

                    if (viewMore)
                    {
                        GUILayout.BeginHorizontal();

                        var font = (Font)EditorGUILayout.ObjectField(legacyText.font, typeof(Font),
                            true, _style.expandWidthFalse);
                        if (font != legacyText.font)
                        {
                            legacyText.font = font;
                            EditorUtility.SetDirty(legacyText);
                        }

                        var material =
                            (Material)EditorGUILayout.ObjectField(legacyText.material, typeof(Material), true,
                                _style.expandWidthFalse);
                        if (material != legacyText.material)
                        {
                            legacyText.material = material;
                            EditorUtility.SetDirty(legacyText);
                        }

                        var enable = GUILayout.Toggle(legacyText.enabled, "Enable", _style.expandWidthFalse);
                        if (enable != legacyText.enabled)
                        {
                            legacyText.enabled = enable;
                            EditorUtility.SetDirty(legacyText);
                        }

                        var raycastTarget = GUILayout.Toggle(legacyText.raycastTarget, "Raycast Target",
                            _style.expandWidthFalse);
                        if (raycastTarget != legacyText.raycastTarget)
                        {
                            legacyText.raycastTarget = raycastTarget;
                            EditorUtility.SetDirty(legacyText);
                        }

                        GUILayout.EndHorizontal();
                    }
                }


                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
        }

        private void Ping(Component component)
        {
            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(component)))
            {
#if UNITY_2021_1_OR_NEWER
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.OpenPrefab(AssetDatabase.GetAssetPath(component));
            currentObject = stage.prefabContentsRoot;
#else
                AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(component)));
                currentObject = PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot;
#endif

                Find(currentObject);
            }

            SceneView.lastActiveSceneView.LookAt(component.gameObject.transform.position);
            Selection.activeObject = component;
            EditorGUIUtility.PingObject(component);
        }

        private void ViewImages()
        {
            GUILayout.BeginHorizontal();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.EndHorizontal();
            Event e = Event.current;
            foreach (ComponentInfo cpInfo in dislayList)
            {
                if (cpInfo.component == null) return;
                GUILayout.BeginHorizontal(EditorStyles.helpBox);
                if (cpInfo.component is Image image)
                {
                    if (image.sprite != null)
                    {
                        GUILayout.Label(image.sprite.texture, _style.iconW, _style.iconH);
                    }
                    else
                    {
                        GUILayout.Label("Empty", _style.iconW, _style.iconH);
                    }

                    var rectIcon = GUILayoutUtility.GetLastRect();
                    if ((e.type == EventType.MouseUp || e.type == EventType.DragPerform) &&
                        rectIcon.Contains(e.mousePosition))
                    {
                        if (e.type == EventType.DragPerform)
                        {
                            if (DragAndDrop.objectReferences.Length > 0)
                            {
                                foreach (Object obj in DragAndDrop.objectReferences)
                                {
                                    if (obj is Sprite s)
                                    {
                                        image.sprite = s;
                                        EditorUtility.SetDirty(currentObject);
                                    }
                                    else if (obj is Texture t)
                                    {
                                        image.sprite =
                                            AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(obj));
                                        EditorUtility.SetDirty(currentObject);
                                    }
                                }
                            }
                        }
                        else
                        {
                            Selection.activeObject = image.sprite.texture;
                            EditorGUIUtility.PingObject(image.sprite.texture);
                        }
                    }

                    GUILayout.BeginVertical();
                    GUILayout.Label(image.name);
                    ViewParents(cpInfo);
                    if (viewTextureSize)
                    {
                        GUILayout.Label(cpInfo.textureSize, _style.objectPathStyle);
                    }

                    if (viewMore)
                    {
                        GUILayout.BeginHorizontal();
                        if (cpInfo.component is Image)
                        {
                            cpInfo.Image.enabled = GUILayout.Toggle(cpInfo.Image.enabled, "Enable", _style.toogle,
                                _style.expandWidthFalse);
                            cpInfo.Image.raycastTarget = GUILayout.Toggle(cpInfo.Image.raycastTarget, "Raycast Target",
                                _style.toogle,
                                _style.expandWidthFalse);
                            var color = EditorGUILayout.ColorField(cpInfo.Image.color, _style.color);
                            if (color != cpInfo.Image.color)
                            {
                                cpInfo.Image.color = color;
                                EditorUtility.SetDirty(currentObject);
                            }
                        }
                        else if (cpInfo.component is RawImage)
                        {
                            cpInfo.RawImage.enabled =
                                GUILayout.Toggle(cpInfo.RawImage.enabled, "Enable", _style.toogle, _style.expandWidthFalse);
                            cpInfo.RawImage.raycastTarget = GUILayout.Toggle(cpInfo.RawImage.raycastTarget,
                                "Raycast Target",
                                _style.toogle,
                                _style.expandWidthFalse);
                            var color = EditorGUILayout.ColorField(cpInfo.RawImage.color, _style.color);
                            if (color != cpInfo.RawImage.color)
                            {
                                cpInfo.RawImage.color = color;
                                EditorUtility.SetDirty(currentObject);
                            }
                        }

                        GUILayout.EndHorizontal();
                    }

                    GUILayout.EndVertical();
                    var rectName = GUILayoutUtility.GetLastRect();
                    if (e.type == EventType.MouseUp && rectName.Contains(e.mousePosition))
                    {
                        Ping(cpInfo.component);
                    }

                    if (!cpInfo.assetPath.StartsWith(strPathToMove))
                    {
                        if (GUILayout.Button("MoveToSelectedPath", GUILayout.ExpandWidth(false)))
                        {
                            MoveTexture(image.sprite);
                            UpdateAssetPath();
                        }
                    }
                }

                GUILayout.EndHorizontal();
            }
        }

        private void ViewParents(ComponentInfo cpInfo)
        {
            if (!viewParent) return;
            GUILayout.BeginHorizontal();
            foreach (var parent in cpInfo.parents)
            {
                if (GUILayout.Button(parent.name, _style.parent, _style.expandWidthFalse))
                {
                    Ping(parent.transform);
                }
            }

            GUILayout.EndHorizontal();
        }

        private void ViewSpriteRenderer()
        {
            GUILayout.BeginHorizontal();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.EndHorizontal();
            Event e = Event.current;
            foreach (ComponentInfo cpInfo in dislayList)
            {
                if (cpInfo.component == null) return;
                GUILayout.BeginHorizontal(EditorStyles.helpBox);
                if (cpInfo.component is SpriteRenderer spriteRenderer)
                {
                    if (spriteRenderer.sprite != null)
                    {
                        GUILayout.Label(spriteRenderer.sprite.texture, _style.iconW, _style.iconH);
                    }
                    else
                    {
                        GUILayout.Label("Empty", _style.iconW, _style.iconH);
                    }

                    var rectIcon = GUILayoutUtility.GetLastRect();
                    if ((e.type == EventType.MouseUp || e.type == EventType.DragPerform) &&
                        rectIcon.Contains(e.mousePosition))
                    {
                        if (e.type == EventType.DragPerform)
                        {
                            if (DragAndDrop.objectReferences.Length > 0)
                            {
                                foreach (Object obj in DragAndDrop.objectReferences)
                                {
                                    if (obj is Sprite s)
                                    {
                                        spriteRenderer.sprite = s;
                                        EditorUtility.SetDirty(currentObject);
                                    }
                                    else if (obj is Texture t)
                                    {
                                        spriteRenderer.sprite =
                                            AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(obj));
                                        EditorUtility.SetDirty(currentObject);
                                    }
                                }
                            }
                        }
                        else
                        {
                            Selection.activeObject = spriteRenderer.sprite.texture;
                            EditorGUIUtility.PingObject(spriteRenderer.sprite.texture);
                        }
                    }

                    GUILayout.BeginVertical();
                    {
                        GUILayout.Label(spriteRenderer.name);
                        ViewParents(cpInfo);
                        if (viewTextureSize)
                        {
                            GUILayout.Label(cpInfo.textureSize, _style.objectPathStyle);
                        }

                        if (viewMore)
                        {
                            GUILayout.BeginHorizontal();
                            {
                                cpInfo.SpriteRenderer.enabled = GUILayout.Toggle(cpInfo.SpriteRenderer.enabled, "Enable",
                                    _style.toogle, _style.expandWidthFalse);
                                var color = EditorGUILayout.ColorField(cpInfo.SpriteRenderer.color, _style.color);
                                if (color != cpInfo.SpriteRenderer.color)
                                {
                                    cpInfo.SpriteRenderer.color = color;
                                    EditorUtility.SetDirty(currentObject);
                                }

                                int sortingOrder =
                                    EditorGUILayout.IntField("Sorting Order", cpInfo.SpriteRenderer.sortingOrder);
                                if (sortingOrder != cpInfo.SpriteRenderer.sortingOrder)
                                {
                                    cpInfo.SpriteRenderer.sortingOrder = sortingOrder;
                                    EditorUtility.SetDirty(currentObject);
                                }

                                int index = _sortingLayerNames.IndexOf(cpInfo.SpriteRenderer.sortingLayerName);
                                int sortingLayerName = EditorGUILayout.Popup(index, _sortingLayerNames.ToArray());
                                if (sortingLayerName != index)
                                {
                                    cpInfo.SpriteRenderer.sortingLayerName = _sortingLayerNames[sortingLayerName];
                                }

                                if (sortingOrder != cpInfo.SpriteRenderer.sortingOrder)
                                {
                                    cpInfo.SpriteRenderer.sortingOrder = sortingOrder;
                                    EditorUtility.SetDirty(currentObject);
                                }
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                    GUILayout.EndVertical();
                    var rectName = GUILayoutUtility.GetLastRect();
                    if (e.type == EventType.MouseUp && rectName.Contains(e.mousePosition))
                    {
                        Ping(cpInfo.component);
                    }
                }

                GUILayout.EndHorizontal();
            }
        }

        private string MoveTexture(Sprite sprite)
        {
            var assetPath = AssetDatabase.GetAssetPath(sprite);
            int indexOfTextures = assetPath.LastIndexOf("Textures/UI/", StringComparison.Ordinal);
            var subPath = assetPath.Substring(indexOfTextures + 12);
            var destinationPath = strPathToMove + "/" + subPath;
            var destinationFolder = Path.GetDirectoryName(destinationPath);
            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
                AssetDatabase.Refresh();
            }

            AssetDatabase.MoveAsset(assetPath, destinationPath);
            AssetDatabase.Refresh();
            return destinationPath;
        }


        void UpdateDragAndDrop()
        {
            if (Event.current.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                Event.current.Use();
            }
            else if (Event.current.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                if (DragAndDrop.objectReferences.Length > 0)
                {
                    foreach (Object obj in DragAndDrop.objectReferences)
                    {
                        if (obj is GameObject)
                        {
                            currentObject = obj as GameObject;
                            Find(currentObject);
                        }
                    }
                }
            }
        }

        private void Find(GameObject o)
        {
            if (o == null)
            {
                return;
            }

            allList.Clear();
            if (tabIndex == 0)
            {
                FindImageComponents(o);
                UpdateAssetPath();
            }
            else if (tabIndex == 1)
            {
                FindTextComponents(o);
            }
            else if (tabIndex == 2)
            {
                FindButtonComponents(o);
            }
            else if (tabIndex == 3)
            {
                FindComponentsMasks(o);
            }
            else if (tabIndex == 4)
            {
                FindSpriteRendererComponents(o);
            }

            Filter();
        }

        private void Filter()
        {
            var searchKey = search[tabIndex].ToLower();
            dislayList = allList.FindAll(s => s.component.name.ToLower().Contains(searchKey));
        }

        private void FindComponentsMasks(GameObject obj)
        {
            Mask component = obj.GetComponent<Mask>();
            if (component != null)
            {
                List<GameObject> parents = new List<GameObject>();
                GetScenePath(component.transform, ref parents);
                allList.Add(new ComponentInfo(component, parents));
            }

            foreach (Transform child in obj.transform)
            {
                FindComponentsMasks(child.gameObject);
            }
        }

        private void FindButtonComponents(GameObject obj)
        {
            Button component = obj.GetComponent<Button>();
            if (component != null)
            {
                List<GameObject> path = new List<GameObject>();
                GetScenePath(component.transform, ref path);
                allList.Add(new ComponentInfo(component, path));
            }

            foreach (Transform child in obj.transform)
            {
                FindButtonComponents(child.gameObject);
            }
        }


        private void ViewButtons()
        {
            Event e = Event.current;
            foreach (var cpInfo in dislayList)
            {
                if (cpInfo == null) return;
                GUILayout.BeginHorizontal(EditorStyles.helpBox);
                {
                    GUILayout.BeginVertical();
                    {
                        GUILayout.Label(cpInfo.component.name);
                        var rectIcon = GUILayoutUtility.GetLastRect();
                        if ((e.type == EventType.MouseUp || e.type == EventType.DragPerform) &&
                            rectIcon.Contains(e.mousePosition))
                        {
                            Ping(cpInfo.component);
                        }

                        List<GameObject> path = new List<GameObject>();
                        GetScenePath(cpInfo.component.transform, ref path);
                        ViewParents(cpInfo);
                        // Button controls
                        if (viewMore)
                        {
                            GUILayout.BeginHorizontal();
                            {
                                var enable = GUILayout.Toggle(cpInfo.Button.enabled, "Enable", _style.toogle,
                                    _style.expandWidthFalse);
                                if (enable != cpInfo.Button.enabled)
                                {
                                    cpInfo.Button.enabled = enable;
                                    EditorUtility.SetDirty(cpInfo.Button);
                                }

                                var interactable = GUILayout.Toggle(cpInfo.Button.interactable, "Interactable",
                                    _style.toogle,
                                    _style.expandWidthFalse);
                                if (interactable != cpInfo.Button.interactable)
                                {
                                    cpInfo.Button.interactable = interactable;
                                    EditorUtility.SetDirty(cpInfo.Button);
                                }
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }
        }

        private void ViewMasks()
        {
            Event e = Event.current;
            foreach (var cpInfo in dislayList)
            {
                if (cpInfo == null) return;
                GUILayout.BeginHorizontal(EditorStyles.helpBox);
                {
                    GUILayout.BeginVertical();
                    {
                        GUILayout.Label(cpInfo.component.name);
                        List<GameObject> path = new List<GameObject>();
                        GetScenePath(cpInfo.component.transform, ref path);
                        ViewParents(cpInfo);

                        if (viewMore)
                        {
                            GUILayout.BeginHorizontal();
                            {
                                var enable = GUILayout.Toggle(cpInfo.Mask.enabled, "Enable", _style.toogle,
                                    _style.expandWidthFalse);
                                if (enable != cpInfo.Mask.enabled)
                                {
                                    cpInfo.Mask.enabled = enable;
                                    EditorUtility.SetDirty(cpInfo.Mask);
                                }

                                var interactable = GUILayout.Toggle(cpInfo.Mask.showMaskGraphic, "Interactable",
                                    _style.toogle,
                                    _style.expandWidthFalse);
                                if (interactable != cpInfo.Mask.showMaskGraphic)
                                {
                                    cpInfo.Mask.showMaskGraphic = interactable;
                                    EditorUtility.SetDirty(cpInfo.Mask);
                                }
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                    GUILayout.EndVertical();
                    var rectIcon = GUILayoutUtility.GetLastRect();
                    if ((e.type == EventType.MouseUp || e.type == EventType.DragPerform) &&
                        rectIcon.Contains(e.mousePosition))
                    {
                        Ping(cpInfo.component);
                    }
                }
                GUILayout.EndHorizontal();
            }
        }


        void FindTextComponents(GameObject obj)
        {
            Text text = obj.GetComponent<Text>();
            if (text != null)
            {
                List<GameObject> path = new List<GameObject>();
                GetScenePath(text.transform, ref path);
                allList.Add(new ComponentInfo()
                {
                    component = text,
                    parents = path,
                });
            }

            TextMeshProUGUI textMeshProUGUI = obj.GetComponent<TextMeshProUGUI>();
            if (textMeshProUGUI != null)
            {
                List<GameObject> path = new List<GameObject>();
                GetScenePath(textMeshProUGUI.transform, ref path);
                allList.Add(new ComponentInfo()
                {
                    component = textMeshProUGUI,
                    parents = path,
                });
            }

            TextMeshPro textMeshPro = obj.GetComponent<TextMeshPro>();
            if (textMeshPro != null)
            {
                List<GameObject> path = new List<GameObject>();
                GetScenePath(textMeshPro.transform, ref path);
                allList.Add(new ComponentInfo()
                {
                    component = textMeshPro,
                    parents = path,
                });
            }

            foreach (Transform child in obj.transform)
            {
                FindTextComponents(child.gameObject);
            }
        }

        private void FindImageComponents(GameObject obj)
        {
            Image image = obj.GetComponent<Image>();
            if (image != null)
            {
                List<GameObject> path = new List<GameObject>();
                GetScenePath(image.transform, ref path);
                allList.Add(new ComponentInfo()
                {
                    component = image,
                    parents = path,
                    textureSize = image.sprite == null
                        ? string.Empty
                        : $"({image.sprite.texture.width}x{image.sprite.texture.height}) {EditorUtility.FormatBytes(Profiler.GetRuntimeMemorySizeLong(image.sprite.texture))}"
                });
            }

            RawImage rawImage = obj.GetComponent<RawImage>();
            if (rawImage != null)
            {
                List<GameObject> parents = new List<GameObject>();
                GetScenePath(rawImage.transform, ref parents);
                allList.Add(new ComponentInfo()
                {
                    component = rawImage,
                    parents = parents,
                    textureSize = rawImage.texture == null
                        ? string.Empty
                        : $"({rawImage.texture.width}x{rawImage.texture.height}) {EditorUtility.FormatBytes(Profiler.GetRuntimeMemorySizeLong(rawImage.texture))}"
                });
            }

            foreach (Transform child in obj.transform)
            {
                FindImageComponents(child.gameObject);
            }
        }

        private void FindSpriteRendererComponents(GameObject obj)
        {
            SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                List<GameObject> parents = new List<GameObject>();
                GetScenePath(spriteRenderer.transform, ref parents);
                allList.Add(new ComponentInfo
                {
                    component = spriteRenderer,
                    parents = parents,
                    textureSize = spriteRenderer.sprite == null
                        ? string.Empty
                        : $"({spriteRenderer.sprite.texture.width}x{spriteRenderer.sprite.texture.height}) {EditorUtility.FormatBytes(Profiler.GetRuntimeMemorySizeLong(spriteRenderer.sprite.texture))}"
                });
            }

            foreach (Transform child in obj.transform)
            {
                FindSpriteRendererComponents(child.gameObject);
            }
        }

        private void GetScenePath(Transform obj, ref List<GameObject> path)
        {
            var parent = obj.parent;
            if (parent != null)
            {
                path.Insert(0, obj.gameObject);
                GetScenePath(parent, ref path);
            }
        }

        private void UpdateAssetPath()
        {
            foreach (ComponentInfo info in allList)
            {
                if (info.component is Image image)
                {
                    info.assetPath = AssetDatabase.GetAssetPath(image.sprite);
                }
            }
        }

        public string[] GetSortingLayerNames()
        {
            Type internalEditorUtilityType = typeof(InternalEditorUtility);
            PropertyInfo sortingLayersProperty =
                internalEditorUtilityType.GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);
            return (string[])sortingLayersProperty.GetValue(null, new object[0]);
        }

        public class ComponentInfo
        {
            public Component component;
            public string textureSize;
            public string assetPath;
            public List<GameObject> parents = new List<GameObject>();

            public ComponentInfo()
            {
            }

            public ComponentInfo(Component component, List<GameObject> parents)
            {
                this.component = component;
                this.parents = parents;
            }

            private TextMeshProUGUI _textMeshProUGUI;

            public TextMeshProUGUI TextMeshProUGUI
            {
                get
                {
                    if (_textMeshProUGUI == null)
                    {
                        _textMeshProUGUI = component as TextMeshProUGUI;
                    }

                    return _textMeshProUGUI;
                }
            }

            private TextMeshPro _textMeshPro;

            public TextMeshPro TextMeshPro
            {
                get
                {
                    if (_textMeshPro == null)
                    {
                        _textMeshPro = component as TextMeshPro;
                    }

                    return _textMeshPro;
                }
            }

            private Text _text;

            public Text Text
            {
                get
                {
                    if (_text == null)
                    {
                        _text = component as Text;
                    }

                    return _text;
                }
            }

            private Image _image;

            public Image Image
            {
                get
                {
                    if (_image == null)
                    {
                        _image = component as Image;
                    }

                    return _image;
                }
            }

            private RawImage _rawImage;

            public RawImage RawImage
            {
                get
                {
                    if (_rawImage == null)
                    {
                        _rawImage = component as RawImage;
                    }

                    return _rawImage;
                }
            }

            private Button _button;

            public Button Button
            {
                get
                {
                    if (_button == null)
                    {
                        _button = component as Button;
                    }

                    return _button;
                }
            }

            private Mask _mask;

            public Mask Mask
            {
                get
                {
                    if (_mask == null)
                    {
                        _mask = component as Mask;
                    }

                    return _mask;
                }
            }

            private SpriteRenderer _spriteRenderer;

            public SpriteRenderer SpriteRenderer
            {
                get
                {
                    if (_spriteRenderer == null)
                    {
                        _spriteRenderer = component as SpriteRenderer;
                    }

                    return _spriteRenderer;
                }
            }


            public static class ObjectViewerHelper
            {
                [MenuItem("GameObject/Open on Object Viewer", false, 0)]
                [MenuItem("Assets/Open on Object Viewer", false, 0)]
                public static void OpenOnObjectViewer()
                {
                    var w = GetWindow<ObjectViewer>();
                    w.currentObject = Selection.activeGameObject;
                    w.Find(w.currentObject);
                }

                [MenuItem("Assets/Open on Object Viewer", true)]
                static bool ValidateLogSelectedTransformName()
                {
                    // Return false if no transform is selected.
                    return Selection.activeObject is GameObject;
                }
            }
        }
    }
}