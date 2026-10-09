using System.Linq;
using GameTool.ObjectPool.Scripts;
using UnityEditor;
using UnityEngine;
using GameTool.Assistants;
using GameToolSample.ObjectPool;

#if UNITY_2021_1_OR_NEWER
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#endif

namespace GameTool.ObjectPool.Editor
{
    public class PoolingSetupWindow : EditorWindow
    {
        private PoolingTable _table;
#if UNITY_2021_1_OR_NEWER
        private SerializedObject table;
        private SerializedProperty _propertyList;
#else
        private Vector2 _scroll;
#endif

        [MenuItem("GameTool/Pooling")]
        private static void ShowWindow()
        {
            var window = GetWindow<PoolingSetupWindow>();
            window.titleContent = new GUIContent("Setup Pooling");
            window.Show();
        }

        private void OnEnable()
        {
            _table = Resources.Load<PoolingTable>("PoolingTable");


            #if UNITY_2021_1_OR_NEWER
            table = new SerializedObject(_table);
            _propertyList = table.FindProperty("Serializers");
            #endif
        }

#if UNITY_2021_1_OR_NEWER

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            minSize = new Vector2(800, 400);

            Button saveButton = new Button()
            {
                text = "SAVE",
            };
            saveButton.clicked += () =>
            {
                table.ApplyModifiedProperties();

                ValidateValues(_table);
                UpdateScript(_table);
                UpdatePrefab(_table);

                EditorUtility.SetDirty(_table);
                AssetDatabase.SaveAssetIfDirty(_table);
            };

            root.Add(saveButton);

            var scrollView = new ScrollView();
            var list = new PropertyField();
            list.BindProperty(_propertyList);
            scrollView.Add(list);
            root.Add(scrollView);
        }
        
#else

        private void OnGUI()
        {
            minSize = new Vector2(800, 400);
            SerializedObject table = new SerializedObject(_table);

            if (GUILayout.Button("SAVE"))
            {
                ValidateValues(_table);
                UpdateScript(_table);
                UpdatePrefab(_table);

                EditorUtility.SetDirty(_table);
                AssetDatabase.SaveAssetIfDirty(_table);
            }

            _scroll = GUILayout.BeginScrollView(_scroll);
            SerializedProperty property = table.FindProperty("Serializers");
            EditorGUILayout.PropertyField(property);
            GUILayout.EndScrollView();

            table.ApplyModifiedProperties();
        }
        
#endif
        public static void ValidateValues(PoolingTable _ins)
        {
            _ins.OnValidate();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void UpdatePrefab(PoolingTable _ins)
        {
            _ins.UpdatePrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void UpdateScript(PoolingTable _ins)
        {
            string unityPath = EditorUtils.FindFilePath("ePrefabPool", "cs");
            string sysPath = EditorUtils.GetSystemFilePath(unityPath);
            EditorUtils.UpdateEnumInFile(sysPath, "ePrefabPool", _ins.Serializers.Select(serializer => serializer.key).ToList(), typeof(ePrefabPool));
            EditorUtils.ReImportAsset(unityPath);
        }
    }
}