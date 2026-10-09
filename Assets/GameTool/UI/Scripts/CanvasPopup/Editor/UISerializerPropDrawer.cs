using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;


namespace GameTool.UI.Scripts.CanvasPopup.Editor
{
    [CustomPropertyDrawer(typeof(UISerializer))]
    public class UISerializerPropDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property,
            GUIContent label)
        {
            var img = (Texture2D)AssetDatabase.LoadMainAssetAtPath(
                "Assets/GameTool/Assistants/Textures/d_iconconflictedoverlay.png");
    
            EditorGUI.BeginProperty(position, label, property);
    
            EditorGUI.PropertyField(position, property, true);
    
            var isDuplicatedKey = property.FindPropertyRelative("isDuplicatedKey").boolValue;
            var res = property.FindPropertyRelative("settingUI").FindPropertyRelative("baseUI").objectReferenceValue;
            if (!res)
            {
                Rect strRect = new Rect(
                    position.x + position.width - EditorGUIUtility.singleLineHeight * 7,
                    position.y,
                    position.width, EditorGUIUtility.singleLineHeight + 2);
                EditorGUI.LabelField(strRect, new GUIContent("Prefab is Empty"));
    
                strRect.x -= EditorGUIUtility.singleLineHeight;
                strRect.width = EditorGUIUtility.singleLineHeight;
                strRect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.DrawPreviewTexture(strRect, img);
                EditorGUI.EndProperty();
                return;
            }
            
            if (GetResPath(res) == "")
            {
                Rect strRect = new Rect(
                    position.x + position.width - EditorGUIUtility.singleLineHeight * 10,
                    position.y,
                    position.width, EditorGUIUtility.singleLineHeight + 2);
                EditorGUI.LabelField(strRect, new GUIContent("Prefab is not in Resources"));
    
                strRect.x -= EditorGUIUtility.singleLineHeight;
                strRect.width = EditorGUIUtility.singleLineHeight;
                strRect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.DrawPreviewTexture(strRect, img);
                EditorGUI.EndProperty();
                return;
            }

            if (isDuplicatedKey)
            {
                Rect strRect = new Rect(
                    position.x + position.width - EditorGUIUtility.singleLineHeight * 7,
                    position.y,
                    position.width, EditorGUIUtility.singleLineHeight + 2);
                EditorGUI.LabelField(strRect, new GUIContent("Key duplicated"));
                
                strRect.x -= EditorGUIUtility.singleLineHeight;
                strRect.width = EditorGUIUtility.singleLineHeight;
                strRect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.DrawPreviewTexture(strRect, img);
                EditorGUI.EndProperty();
                return;
            }
    
            EditorGUI.EndProperty();
        }
    
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property);
        }
        
        public string GetResPath(Object baseUI)
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
    }
}