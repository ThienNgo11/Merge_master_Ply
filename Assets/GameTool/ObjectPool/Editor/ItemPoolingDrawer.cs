using GameTool.ObjectPool.Scripts;
using UnityEditor;
using UnityEngine;

namespace GameTool.ObjectPool.Editor
{
    [CustomPropertyDrawer(typeof(ItemPooling))]
    public class ItemPoolingDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var img = (Texture2D)AssetDatabase.LoadMainAssetAtPath(
                "Assets/GameTool/Assistants/Textures/d_iconconflictedoverlay.png");

            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty listPooling = property.FindPropertyRelative("listPooling");
            SerializedProperty resourcePaths = property.FindPropertyRelative("resourcePaths");

            if (listPooling.isExpanded)
            {
                Rect listPoolingRect = new Rect(position.x, position.y, position.width * 0.5f - 5,
                    EditorGUI.GetPropertyHeight(listPooling));
                EditorGUI.PropertyField(listPoolingRect, listPooling, true);

                for (int i = 0; i < listPooling.arraySize; i++)
                {
                    var ele = listPooling.GetArrayElementAtIndex(i);

                    try
                    {
                        var elepath = resourcePaths.GetArrayElementAtIndex(i);

                        if (elepath.stringValue == "")
                        {
                            Rect elepathRect = new Rect(position.x + 5 + position.width * 0.5f,
                                position.y + 6 + (EditorGUIUtility.singleLineHeight + 2) * (i + 1),
                                EditorGUI.GetPropertyHeight(ele), EditorGUI.GetPropertyHeight(ele));
                            EditorGUI.DrawPreviewTexture(elepathRect, img);
                            Rect strRect = new Rect(
                                position.x + 5 + position.width * 0.5f + EditorGUI.GetPropertyHeight(ele),
                                position.y + 6 + (EditorGUIUtility.singleLineHeight + 2) * (i + 1),
                                position.width / 2 - 5, EditorGUI.GetPropertyHeight(ele));
                            EditorGUI.LabelField(strRect, new GUIContent("Prefab is not in Resources"));
                        }
                        else
                        {
                            Rect elepathRect = new Rect(position.x + position.width * 0.5f,
                                position.y + 6 + (EditorGUIUtility.singleLineHeight + 2) * (i + 1),
                                position.width / 2 - 5, EditorGUI.GetPropertyHeight(ele));
                            EditorGUI.LabelField(elepathRect, new GUIContent("Res path: " + elepath.stringValue));
                        }
                    }
                    catch
                    {
                        Rect elepathRect = new Rect(position.x + 5 + position.width * 0.5f,
                            position.y + 6 + (EditorGUIUtility.singleLineHeight + 2) * (i + 1),
                            EditorGUI.GetPropertyHeight(ele), EditorGUI.GetPropertyHeight(ele));
                        EditorGUI.DrawPreviewTexture(elepathRect, img);
                        Rect strRect = new Rect(
                            position.x + 5 + position.width * 0.5f + EditorGUI.GetPropertyHeight(ele),
                            position.y + 6 + (EditorGUIUtility.singleLineHeight + 2) * (i + 1),
                            position.width / 2 - 5, EditorGUI.GetPropertyHeight(ele));
                        EditorGUI.LabelField(strRect, new GUIContent("Please Validate values"));
                    }
                }
            }
            else
            {
                Rect listPoolingRect = new Rect(position.x, position.y, position.width - 5,
                    EditorGUI.GetPropertyHeight(listPooling));
                EditorGUI.PropertyField(listPoolingRect, listPooling, true);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty listPooling = property.FindPropertyRelative("listPooling");

            // Calculate the height of the listPooling property plus the height of the label
            float totalHeight = EditorGUI.GetPropertyHeight(listPooling, true);

            return totalHeight;
        }
    }
}