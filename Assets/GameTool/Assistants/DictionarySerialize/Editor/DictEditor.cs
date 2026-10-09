using UnityEditor;
using UnityEngine;

namespace GameTool.Assistants.DictionarySerialize.Editor
{
    [CustomPropertyDrawer(typeof(Dict<,>))]
    public class DictEditor : PropertyDrawer
    {
        Texture2D img = (Texture2D) AssetDatabase.LoadMainAssetAtPath(
            "Assets/GameTool/Assistants/Textures/d_iconconflictedoverlay.png");
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty listProperty = property.FindPropertyRelative("list");
            EditorGUI.PropertyField(position, listProperty, label, true);
            if (listProperty.isExpanded)
            {
                CheckForDuplicateKeys(listProperty, position); // Check for duplicate keys
            }

            DisplayHelpBoxIfNeeded(position, property, label); // Display help box if needed

            EditorGUI.EndProperty();
        }

        private void CheckForDuplicateKeys(SerializedProperty listProperty, Rect position)
        {
            SerializedProperty arraySizeProp = listProperty.FindPropertyRelative("Array.size");
            
            var y = EditorGUIUtility.singleLineHeight + 8;
            for (int i = 0; i < arraySizeProp.intValue; i++)
            {
                SerializedProperty elementProp = listProperty.GetArrayElementAtIndex(i);

                elementProp.isExpanded = true;

                if (elementProp.FindPropertyRelative("isDuplicate").boolValue)
                {
                    EditorGUI.DrawPreviewTexture(
                        new Rect(
                            position.position + new Vector2(-16,
                                y),
                            new Vector2(16, 16)), img);
                }

                y += EditorGUI.GetPropertyHeight(elementProp) + 2;
            }
        }

        private void DisplayHelpBoxIfNeeded(Rect position, SerializedProperty property, GUIContent label)
        {
            if (HasDuplicateKeys(property))
            {
                // EditorGUI.HelpBox(
                //     new Rect(
                //         position.position + new Vector2(0,
                //             GetPropertyHeight(property, label) - EditorGUIUtility.singleLineHeight * 2f),
                //         new Vector2(position.width, EditorGUIUtility.singleLineHeight * 2f)),
                //     "Duplicate keys detected!", MessageType.Error);
                EditorGUI.DrawPreviewTexture(
                    new Rect(
                        position.position + new Vector2(position.width - EditorGUIUtility.singleLineHeight * 4,
                            1),
                        new Vector2(16, 16)), img);
            }
        }

        private bool HasDuplicateKeys(SerializedProperty dictProperty)
        {
            return dictProperty.FindPropertyRelative("isDuplicate").boolValue;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("list"), label, true);

            // if (HasDuplicateKeys(property))
            // {
            //     height += EditorGUIUtility.singleLineHeight * 2.5f;
            // }

            return height;
        }
    }
}