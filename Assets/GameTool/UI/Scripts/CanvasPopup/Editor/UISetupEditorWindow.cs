using System.Linq;
using GameTool.Assistants;
using GameToolSample.UIManager;
using UnityEditor;
using UnityEngine;

#if UNITY_2021_1_OR_NEWER
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#else
using System.Collections.Generic;
using System.IO;
#endif

namespace GameTool.UI.Scripts.CanvasPopup.Editor
{
    public class UISetupEditorWindow : EditorWindow
    {
        private CanvasPrefTable _table;
        #if UNITY_2021_1_OR_NEWER
        private SerializedObject _tableSerializedObject;
        #else
        private Vector2 _scroll;
        #endif

        [MenuItem("GameTool/UI/CanvasManager")]
        private static void ShowWindow()
        {
            var window = GetWindow<UISetupEditorWindow>();
            window.titleContent = new GUIContent("UI Setup");
            window.Show();
        }

        private void OnEnable()
        {
            _table = Resources.Load<CanvasPrefTable>("CanvasPrefTable");
            Undo.RecordObject(_table, "CanvasPrefTable");
        }

#if UNITY_2021_1_OR_NEWER

        private void CreateGUI()
        {
            minSize = new Vector2(800, 400);
            VisualElement root = rootVisualElement;

            _tableSerializedObject = new SerializedObject(_table);
            root.Add(new Button(SaveClick) { text = "SAVE" });

            SerializedProperty property = _tableSerializedObject.FindProperty("Serializers");
            SerializedProperty ListLayerproperty = _tableSerializedObject.FindProperty("ListLayer");

            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            root.Add(scrollView);
            var ListLayerpropertypropertyField = new PropertyField();
            ListLayerpropertypropertyField.BindProperty(ListLayerproperty);
            var propertyField = new PropertyField();
            propertyField.BindProperty(property);
            scrollView.Add(ListLayerpropertypropertyField);
            scrollView.Add(propertyField);
        }

#else
        private void OnGUI()
        {
            minSize = new Vector2(800, 400);
            SerializedObject table = new SerializedObject(_table);

            if (GUILayout.Button("SAVE"))
            {
                SaveClick();
            }

            _scroll = GUILayout.BeginScrollView(_scroll);
            SerializedProperty ListLayerproperty = table.FindProperty("ListLayer");
            SerializedProperty property = table.FindProperty("Serializers");
            EditorGUILayout.PropertyField(ListLayerproperty);
            EditorGUILayout.PropertyField(property);
            GUILayout.EndScrollView();

            table.ApplyModifiedProperties();
        }

#endif

        private void SaveClick()
        {
            _table.OnValidate();

            var uniPath = EditorUtils.FindFilePath("eUIName", "cs");
            var sysPath = EditorUtils.GetSystemFilePath(uniPath);
            EditorUtils.UpdateEnumInFile(sysPath, "eUIName", _table.Serializers.Select(serializer => serializer.key).ToList(), typeof(eUIName));
            EditorUtils.ReImportAsset(uniPath);
            
#if UNITY_2021_1_OR_NEWER
            _tableSerializedObject.ApplyModifiedProperties();
#endif
        }
    }
}