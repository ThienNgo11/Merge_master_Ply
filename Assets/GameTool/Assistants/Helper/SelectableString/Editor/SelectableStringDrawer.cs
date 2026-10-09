using System;
using System.Collections.Generic;
using System.Reflection;
using GameTool.Editor;
using UnityEditor;
using UnityEngine;

namespace GameTool.Assistants.Helper.SelectableString.Editor
{
    [CustomPropertyDrawer(typeof(Scripts.SelectableString))]
    public class SelectableStringDrawer : PropertyDrawer
    {
        private int idHash;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Scripts.SelectableString stringSelectionAttribute = (Scripts.SelectableString)attribute;
            Type[] optionTypes = stringSelectionAttribute.optionTypes;
            List<string> options = new List<string>();
            int selectedIndex = -1;

            if (optionTypes != null)
            {
                foreach (Type type in optionTypes)
                {
                    if (type.IsEnum)
                    {
                        options.AddRange(Enum.GetNames(type));
                    }
                    else if (type.IsClass || (type.IsValueType && !type.IsPrimitive))
                    {
                        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
                        foreach (FieldInfo field in fields)
                        {
                            if (field.FieldType == typeof(string))
                            {
                                options.Add((string)field.GetValue(null));
                            }
                        }
                    }
                }
            }

            if (stringSelectionAttribute.options != null)
            {
                options.AddRange(stringSelectionAttribute.options);
            }

            if (property.propertyType == SerializedPropertyType.String)
            {
                selectedIndex = options.IndexOf(property.stringValue);

                if (idHash == 0) idHash = "SelectableStringDrawer".GetHashCode();
                int id = GUIUtility.GetControlID(idHash, FocusType.Keyboard, position);

                label = EditorGUI.BeginProperty(position, label, property);
                position = EditorGUI.PrefixLabel(position, id, label);

                var buttonText = new GUIContent();

                if (DropdownButton(id, position, buttonText))
                {
                    Action<int> onSelect = i =>
                    {
                        property.stringValue = options[i];
                        property.serializedObject.ApplyModifiedProperties();
                    };

                    SearchablePopup.Show(position, options.ToArray(), selectedIndex < 0 ? 0 : selectedIndex, onSelect,
                        true);
                }

                GUI.Label(new Rect(position.position + new Vector2(0, 1), position.size),
                    " <color=cyan>\"</color><color=white>" + property.stringValue + "</color><color=cyan>\"</color>",
                    new GUIStyle { richText = true });

                EditorGUI.EndProperty();
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Use StringSelection with string fields.");
            }
        }

        private static bool DropdownButton(int id, Rect position, GUIContent content)
        {
            Event current = Event.current;
            switch (current.type)
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
}