using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DatdevUlts.GetPathResources
{
    /// <summary>
    /// Editor cho resources path mà vẫn giữ reference
    /// </summary>
    [Serializable]
    public class PathResources : ISerializationCallbackReceiver
    {
#if UNITY_EDITOR
        [SerializeField] private Object unityObject;
        [SerializeField] private Object _serializedTargetObject;

        public Object UnityObject
        {
            get => unityObject;
            set => unityObject = value;
        }

        public Object SerializedTargetObject
        {
            get => _serializedTargetObject;
            set => _serializedTargetObject = value;
        }

        /// <summary>
        /// Nếu true => bị thay đổi
        /// </summary>
        /// <returns></returns>
        public static bool RefreshResourcePath(ref string resourcesPath, Object unityObject)
        {
            if (resourcesPath != GetResourcePath(unityObject))
            {
                resourcesPath = GetResourcePath(unityObject);
                return true;
            }

            return false;
        }

        private string GetResourcePath()
        {
            var str = AssetDatabase.GetAssetPath(unityObject);
            var index = str.LastIndexOf("Resources", StringComparison.Ordinal);
            if (index >= 0)
            {
                str = str.Substring(index);
                str = str.Remove(0, "Resources/".Length);

                index = str.LastIndexOf(".", StringComparison.Ordinal);
                str = str.Remove(index);

                return str;
            }

            return resourcesPath;
        }

        private static string GetResourcePath(Object unityObject)
        {
            var str = AssetDatabase.GetAssetPath(unityObject);
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
#endif

        [SerializeField] private string resourcesPath;

        public string ResourcesPath
        {
            get => resourcesPath;
            set => resourcesPath = value;
        }

        public T LoadAsset<T>() where T : Object
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                try
                {
                    return (T)UnityObject;
                }
                catch
                {
                    // ignored
                }
            }
#endif
            return Resources.Load<T>(resourcesPath);
        }

        /// <summary>
        /// Nếu true => bị thay đổi
        /// </summary>
        /// <returns></returns>
        public bool RefreshResourcePath(Object serializedObject = null)
        {
#if UNITY_EDITOR

            if (resourcesPath != GetResourcePath())
            {
                resourcesPath = GetResourcePath();
                if (serializedObject != null)
                {
                    EditorUtility.SetDirty(serializedObject);
                }

                if (_serializedTargetObject != null)
                {
                    EditorUtility.SetDirty(_serializedTargetObject);
                }

                return true;
            }

#endif

            return false;
        }

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (SerializedTargetObject)
            {
                RefreshResourcePath(SerializedTargetObject); 
            }
            else
            {
                resourcesPath = GetResourcePath();
            }
#endif
        }

        public void OnAfterDeserialize()
        {
        }
    }
}