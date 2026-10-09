using GameTool.ObjectPool.Scripts;
using UnityEditor;
using UnityEngine;

namespace GameTool.ObjectPool.Editor
{
    [CustomPropertyDrawer(typeof(PoolSerializer))]
    public class PoolSerializerPropDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property,
            GUIContent label)
        {
            
            var img = (Texture2D)AssetDatabase.LoadMainAssetAtPath(
                "Assets/GameTool/Assistants/Textures/d_iconconflictedoverlay.png");
    
            EditorGUI.BeginProperty(position, label, property);
    
            EditorGUI.PropertyField(position, property, true);
    
            var table = (PoolingTable)property.serializedObject.targetObject;
            Undo.RecordObject(table, "PoolingTable");
            var key = property.FindPropertyRelative("key").stringValue;
            var keyDuplicated = property.FindPropertyRelative("keyDuplicated").boolValue;
            var strPaths = property.FindPropertyRelative("ItemPooling").FindPropertyRelative("resourcePaths");
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

            if (keyDuplicated)
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