using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DatdevUlts.GetPathResources.Editor
{
    [CustomPropertyDrawer(typeof(PathResources), true)]
    public class PathResourcesDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var unityObjectProp = property.FindPropertyRelative("unityObject");
            var resourcesPathProp = property.FindPropertyRelative("resourcesPath");
            
            var serializedTargetObjectProp = property.FindPropertyRelative("_serializedTargetObject");
            if (serializedTargetObjectProp.objectReferenceValue != property.serializedObject.targetObject)
            {
                serializedTargetObjectProp.objectReferenceValue = property.serializedObject.targetObject;
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }

            string resourcesPathValue = resourcesPathProp.stringValue;
            if (PathResources.RefreshResourcePath(ref resourcesPathValue, unityObjectProp.objectReferenceValue))
            {
                resourcesPathProp.stringValue = resourcesPathValue;
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }

            Rect posFix = EditorGUI.PrefixLabel(position, label);
            Rect pos = posFix;
            pos.width = position.width - posFix.width;

            EditorGUI.ObjectField(new Rect(pos.x, pos.y, pos.width / 3 * 2, pos.height),
                unityObjectProp, typeof(Object), GUIContent.none);

            EditorGUI.TextField(
                new Rect(pos.x + pos.width / 3 * 2, pos.y, position.x + position.width - pos.x, pos.height),
                string.IsNullOrEmpty(resourcesPathProp.stringValue)
                    ? "Not in resources"
                    : resourcesPathProp.stringValue);

            EditorGUI.EndProperty();
        }
    }
}