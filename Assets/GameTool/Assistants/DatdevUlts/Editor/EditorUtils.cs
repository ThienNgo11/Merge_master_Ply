using System;
using System.Collections.Generic;
using GameTool.Editor;
using UnityEditor;
using UnityEngine;
using Range = DatdevUlts.Ults.Range;

namespace DatdevUlts.Editor
{
    public static class EditorUtils
    {
        public static void DrawDropDownButton(SerializedProperty property, Rect position, ref int idHash, GUIContent label, Type typeDrawer, List<string> options, Action<int> action = null)
        {
            if (property.propertyType == SerializedPropertyType.String)
            {
                int selectedIndex = options.IndexOf(property.stringValue);
                if (idHash == 0) idHash = typeDrawer.GetHashCode();
                int id = GUIUtility.GetControlID(idHash, FocusType.Keyboard, position);

                label = EditorGUI.BeginProperty(position, label, property);
                position = EditorGUI.PrefixLabel(position, id, label);

                var buttonText = new GUIContent();

                if (DropdownButton(id, position, buttonText))
                {
                    void OnSelect(int i)
                    {
                        property.stringValue = options[i];
                        property.serializedObject.ApplyModifiedProperties();

                        action?.Invoke(i);
                    }

                    SearchablePopup.Show(position, options.ToArray(), selectedIndex < 0 ? 0 : selectedIndex, OnSelect,
                        true);
                }

                GUI.Label(new Rect(position.position + new Vector2(0, 1), position.size),
                    " <color=cyan>\"</color><color=white>" + property.stringValue + "</color><color=cyan>\"</color>",
                    new GUIStyle { richText = true });

                EditorGUI.EndProperty();
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Use with string fields.");
            }
        }

        private static bool DropdownButton(int id, Rect position, GUIContent content)
        {
            Event current = Event.current;
            switch(current.type)
            {
                case EventType.MouseDown:
                    if (position.Contains(current.mousePosition) && current.button == 0)
                    {
                        Event.current.Use();
                        return true;
                    }

                    break;
                case EventType.KeyDown:
                    if (GUIUtility.keyboardControl == id &&
                        (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter))
                    {
                        Event.current.Use();
                        return true;
                    }

                    break;
                case EventType.Repaint:
                    EditorStyles.popup.Draw(position, content, id, false);
                    break;
            }

            return false;
        }
    }
    
    [CustomPropertyDrawer(typeof(Range))]
    public class RangeDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var minProp = property.FindPropertyRelative("min");
            var maxProp = property.FindPropertyRelative("max");

            position = EditorGUI.PrefixLabel(position, label);
            var minRect = new Rect(position.x, position.y, position.width / 2 - 2, position.height);
            var maxRect = new Rect(position.x + position.width / 2 + 2, position.y, position.width / 2 - 2, position.height);

            EditorGUI.PropertyField(minRect, minProp, GUIContent.none);
            EditorGUI.PropertyField(maxRect, maxProp, GUIContent.none);
        }
    }
}