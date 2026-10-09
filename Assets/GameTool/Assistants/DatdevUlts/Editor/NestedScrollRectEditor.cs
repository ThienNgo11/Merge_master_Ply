using DatdevUlts.UI_Utility;
using UnityEditor;
using UnityEditor.UI;

namespace DatdevUlts.Editor
{
    [CustomEditor(typeof(NestedScrollRect))]
    public class NestedScrollRectEditor : ScrollRectEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_parentScrollRect"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}