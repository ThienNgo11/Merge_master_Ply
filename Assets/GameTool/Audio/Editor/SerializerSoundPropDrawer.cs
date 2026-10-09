using GameTool.Audio.Scripts;
using UnityEditor;
using UnityEngine;

namespace GameTool.Audio.Editor
{
    [CustomPropertyDrawer(typeof(SerializerSound))]
    public class SerializerSoundPropDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property,
            GUIContent label)
        {
            var img = (Texture2D)AssetDatabase.LoadMainAssetAtPath(
                "Assets/GameTool/Assistants/Textures/d_iconconflictedoverlay.png");
    
            EditorGUI.BeginProperty(position, label, property);
    
            EditorGUI.PropertyField(position, property, true);
    
            var key = property.FindPropertyRelative("key").stringValue;
            var isDuplicated = property.FindPropertyRelative("isDuplicated").boolValue;
            var strPaths = property.FindPropertyRelative("track").FindPropertyRelative("resourcePaths");
            if (key == "")
            {
                Rect strRect = new Rect(
                    position.x + position.width - EditorGUIUtility.singleLineHeight * 7,
                    position.y,
                    position.width, EditorGUIUtility.singleLineHeight + 2);
                EditorGUI.LabelField(strRect, new GUIContent("Please set a Key"));
            
                strRect.x -= EditorGUIUtility.singleLineHeight;
                strRect.width = EditorGUIUtility.singleLineHeight;
                strRect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.DrawPreviewTexture(strRect, img);
                EditorGUI.EndProperty();
                return;
            }
            
            if (strPaths.arraySize == 0)
            {
                Rect strRect = new Rect(
                    position.x + position.width - EditorGUIUtility.singleLineHeight * 7,
                    position.y,
                    position.width, EditorGUIUtility.singleLineHeight + 2);
                EditorGUI.LabelField(strRect, new GUIContent("List is Empty!"));
            
                strRect.x -= EditorGUIUtility.singleLineHeight;
                strRect.width = EditorGUIUtility.singleLineHeight;
                strRect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.DrawPreviewTexture(strRect, img);
                EditorGUI.EndProperty();
                return;
            }
            
            for (int i = 0; i < strPaths.arraySize; i++)
            {
                var elepath = strPaths.GetArrayElementAtIndex(i);
            
                if (elepath.stringValue == "")
                {
                    Rect strRect = new Rect(
                        position.x + position.width - EditorGUIUtility.singleLineHeight * 7,
                        position.y,
                        position.width, EditorGUIUtility.singleLineHeight + 2);
                    EditorGUI.LabelField(strRect, new GUIContent("Error resources"));
            
                    strRect.x -= EditorGUIUtility.singleLineHeight;
                    strRect.width = EditorGUIUtility.singleLineHeight;
                    strRect.height = EditorGUIUtility.singleLineHeight;
                    EditorGUI.DrawPreviewTexture(strRect, img);
                    EditorGUI.EndProperty();
                    return;
                }
            }
            
            if (isDuplicated)
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
    }
}