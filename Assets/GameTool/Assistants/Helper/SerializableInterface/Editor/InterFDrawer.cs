using GameTool.Assistants.Helper.SerializableInterface.Main;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameTool.Assistants.Helper.SerializableInterface.Editor
{
    [CustomPropertyDrawer(typeof(InterF<>), true)]
    public class InterFDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty componentProperty = property.FindPropertyRelative("component");

            var interfaceType = fieldInfo.FieldType.GetGenericArguments()[0];
        
            Object draggedObject =
                EditorGUI.ObjectField(
                    new Rect(position.position, new Vector2(position.width, EditorGUIUtility.singleLineHeight + 2)), label,
                    componentProperty.objectReferenceValue, typeof(Object), true);
        
            if (!interfaceType.IsInstanceOfType(componentProperty.objectReferenceValue))
            {
                EditorGUI.HelpBox(
                    new Rect(
                        position.position + new Vector2(position.x,
                            position.height - EditorGUIUtility.singleLineHeight * 1.5f),
                        new Vector2(position.width, EditorGUIUtility.singleLineHeight * 1.5f)),
                    "Need a " + interfaceType.Name, MessageType.Error);
            }

            if (draggedObject != null)
            {
                if (draggedObject is GameObject)
                {
                    var component = (draggedObject as GameObject).GetComponent(fieldInfo.FieldType.GetGenericArguments()[0]);
                    if (interfaceType.IsInstanceOfType(component))
                    {
                        componentProperty.objectReferenceValue = component;
                    }
                }
                else
                {
                    Object newComponent = draggedObject;

                    if (interfaceType.IsInstanceOfType(newComponent))
                    {
                        componentProperty.objectReferenceValue = newComponent;
                        // componentProperty
                    }
                }
            }
            else
            {
                componentProperty.objectReferenceValue = null;
            }
        
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty componentProperty = property.FindPropertyRelative("component");
            var interfaceType = fieldInfo.FieldType.GetGenericArguments()[0];

            if (interfaceType.IsInstanceOfType(componentProperty.objectReferenceValue))
            {
                return base.GetPropertyHeight(property, label);
            }

            return (base.GetPropertyHeight(property, label) + EditorGUIUtility.singleLineHeight * 2);
        }
    }
}