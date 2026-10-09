using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameTool.Editor
{
    public class InspectorHelperEditor
    {
        public static int commandCount = 0;
        static Dictionary<string, List<TransformCache>> saveCache = new Dictionary<string, List<TransformCache>>();
        static string[] namespaces = new string[] { "GameTool" };
        #region Auto References
        [MenuItem("GameTool/InspectorHelper/AutoReferences #r", priority = 0)]
        private static void AutoReferences()
        {
            DateTime time = DateTime.Now;
            commandCount = 0;
            GameObject[] selections = Selection.gameObjects;
            for (int i = 0; i < selections.Length; i++)
            {
                // Lấy danh sách tất cả gameObjects đang chọn ở Hierarchy
                GameObject activeGameObject = selections[i];
                Undo.RegisterCompleteObjectUndo(activeGameObject, "AutoReferences");
                CachedData(activeGameObject);
                // Ghi nhớ để ctrl + z
                foreach (var target in activeGameObject.GetComponents<Component>())
                {
                    Debug.Log($"{target.GetType().Name}");
                    System.Type targetType = target.GetType();
                    while (targetType!=null)
                    {
                        if (CheckReferences(targetType))
                        {
                            Debug.Log($"\t{targetType.Namespace}");
                            ReferencesComponent(target, targetType);
                        }
                        targetType = targetType.BaseType;
                    }
                }
                // Preview scene khi scene giả lập khi chọn xem 1 prefab ấy
                if (!EditorSceneManager.IsPreviewScene(activeGameObject.scene))
                {
                    SaveOverridePrefab(activeGameObject);
                    EditorSceneManager.SaveScene(activeGameObject.scene);
                }
                else
                {
                    EditorUtility.SetDirty(activeGameObject);
                }
            }
            RenameByProperty();
            Debug.Log(commandCount +" : " + (DateTime.Now - time).TotalMilliseconds + " ms");

        }
        static bool CheckReferences(Type targetType)
        {
            if (targetType.Namespace != null)
            {
                for (int i = 0; i < namespaces.Length; i++)
                {
                    if (targetType.Namespace.CompareTo(namespaces[i]) == 0)
                    {
                        return true;
                    }
                }
                return false;
            }
            else return true;
        }
        private static void ReferencesComponent(Component target, System.Type type)
        {
            //Debug.Log($"\t{type.Name}");
            MethodInfo methodInfo = type.GetMethod("AutoReferences");
            methodInfo?.Invoke(target, null);

            // Lấy danh sách các field của lớp này
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if(fields.Length > 0) Debug.LogWarning($"=======<color=blue>References</color>=======");
            // Duyệt qua từng field
            foreach (FieldInfo field in fields)
            {
                //Debug.Log($"\t\t{field.Name}");
                commandCount++;
                // Kiểm tra xem field có thuộc loại Component hay không, không tính GameObject vì GameObject không nằm trong Component
                if (typeof(Component).IsAssignableFrom(field.FieldType))
                {
                    FindComponent(target, field);

                }
                // Kiểm tra xem FieldType có phải là GameObject hay không
                else if (typeof(GameObject).IsAssignableFrom(field.FieldType))
                {
                    FindGameObject( target, field);
                }
                else if (field.FieldType.IsGenericType)
                {
                    if (field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        var genericType = field.FieldType.GetGenericArguments()[0];
                        if (typeof(Component).IsAssignableFrom(genericType))
                        {
                            FindComponentList(target, field, genericType);
                        }
                        else if (typeof(GameObject).IsAssignableFrom(genericType))
                        {
                            FindObjectList(target, field, genericType);
                        }
                        else
                        {
                            Debug.LogWarning(genericType);
                        }
                    }
                    else
                    {
                        Debug.LogWarning(field.FieldType.GetGenericTypeDefinition());
                    }
                }
                else if(field.FieldType.IsArray)
                {
                    var arrayType = field.FieldType.GetElementType();
                    if (typeof(Component).IsAssignableFrom(arrayType))
                    {
                        FindComponentArray(target, field, arrayType);
                    }
                    else if (typeof(GameObject).IsAssignableFrom(arrayType))
                    {
                        FindObjectArray(target, field, arrayType);
                    }
                    else
                    {
                        Debug.LogWarning(arrayType);
                    }
                }
                else
                {
                    Debug.LogWarning(field.FieldType);
                }
            }
        }
        private static void FindObjectArray(Component target, FieldInfo field, Type type)
        {
            var cleanName = CleaningName(field.Name).ToLower();
            cleanName = cleanName.Replace("list_", "");
            List<TransformCache> cached = new List<TransformCache>();
            if (saveCache.TryGetValue(cleanName, out cached))
            {
                GameObject[] childs = new GameObject[cached.Count];
                for (int i = 0; i < cached.Count; i++)
                {
                    commandCount++;
                    childs[i]= cached[i].trans.gameObject;
                }
                field.SetValue(target, childs);
            }
            else
            {
                commandCount++;
                Debug.LogWarning($"Component of type '{type}' has '{type.Name}'.");
            }
        }
        private static void FindComponentArray(Component target, FieldInfo field, Type type)
        {
            var cleanName = CleaningName(field.Name).ToLower();
            cleanName = cleanName.Replace("list_", "");
            List<TransformCache> cached = new List<TransformCache>();
            if (saveCache.TryGetValue(cleanName, out cached))
            {
                var components = Array.CreateInstance(type,cached.Count);
                for (int i = 0; i < cached.Count; i++)
                {
                    commandCount++;
                    components.SetValue(cached[i].trans.gameObject,i);
                }
                field.SetValue(target, components);
            }
            else
            {
                Debug.LogWarning($"Component of type '{type}' has '{type.Name}'.");
            }
        }
        private static void FindObjectList(Component target, FieldInfo field, Type type)
        {
            var cleanName = CleaningName(field.Name).ToLower();
            cleanName = cleanName.Replace("list_", "");
            List<TransformCache> cached = new List<TransformCache>();
            if (saveCache.TryGetValue(cleanName, out cached))
            {
                List<GameObject> components = new List<GameObject>();
                for (int i = 0; i < cached.Count; i++)
                {
                    commandCount++;
                    var cache = cached[i];
                    components.Add(cache.trans.gameObject);
                }
                // Nếu không, gán giá trị
                field.SetValue(target, components);
            }
            else
            {
                // Nếu có thì thôi
                Debug.LogWarning($"Component of type '{type}' has '{type.Name}'.");
            }
        }

        private static void FindComponentList(Component target, FieldInfo field, Type type)
        {
            var cleanName = CleaningName(field.Name).ToLower();
            cleanName = cleanName.Replace("list_", "");
            List<TransformCache> cached = new List<TransformCache>();
            if (saveCache.TryGetValue(cleanName, out cached))
            {
                var listType = typeof(List<>).MakeGenericType(type);
                var components = (IList)Activator.CreateInstance(listType);
                for (int i = 0; i < cached.Count; i++)
                {
                    commandCount++;
                    var cache = cached[i];
                    components.Add(cache.trans.GetComponent(type));
                }
                // Nếu không, gán giá trị
                field.SetValue(target, components);
            }
            else
            {
                Debug.LogWarning($"Component of type '{type}' has '{type.Name}'.");
            }
        }

        private static void FindGameObject(Component target, FieldInfo field)
        {
            var cleanName = CleaningName(field.Name).ToLower();
            List<TransformCache> cached = new List<TransformCache>();
            if(saveCache.TryGetValue(cleanName,out cached))
            {
                var cache = cached[0];
                GameObject fieldValue = field.GetValue(target) as GameObject;
                if (fieldValue == null)
                {
                    commandCount++;
                    field.SetValue(target, cache.trans.gameObject);
                }
                else
                    Debug.LogWarning($"GameObject '{field.FieldType}' has '{fieldValue.name}'.");
            }
            else
            {
                //GameObject fieldValue = field.GetValue(target) as GameObject;
                //if (fieldValue == null)
                //{
                //    commandCount++;
                //    field.SetValue(target, target.gameObject);
                //}
                //else
                //{
                //    Debug.LogWarning($"GameObject '{field.FieldType}' has '{fieldValue.name}'.");
                //}
            }
        }

        private static void FindComponent(Component target, FieldInfo field)
        {
            var cleanName = CleaningName(field.Name).ToLower();
            List<TransformCache> cached = new List<TransformCache>();
            if (saveCache.TryGetValue(cleanName, out cached))
            {
                commandCount++;
                var cache = cached[0];
                // Kiểm tra xem field hiện tại có references nào không?
                Component fieldValue = field.GetValue(target) as Component;
                if (fieldValue == null)
                {
                    // Nếu không, gán giá trị
                    field.SetValue(target, cache.trans.GetComponent(field.FieldType));
                }
                else
                {

                    // Nếu có thì thôi
                    Debug.LogWarning($"Component of type '{field.FieldType}' has '{fieldValue.name}'.");
                }
            }
            // Nếu không có thì xét với gameobject đang chọn hiện tại
            else
            {
                //commandCount++;
                //Component fieldValue = field.GetValue(target) as Component;
                //if (fieldValue == null)
                //{
                //    field.SetValue(target, target.gameObject.GetComponent(field.FieldType));
                //}
                //else
                //{
                //    Debug.LogWarning($"Component of type '{field.FieldType}' has '{fieldValue.name}'.");
                //}
            }
        }

        #endregion

        #region Rename
        [MenuItem("GameTool/InspectorHelper/RenameByProperty #&r")]
        private static void RenameByProperty()
        {
            Debug.Log("=======Rename=======");
            GameObject[] selections = Selection.gameObjects;
            for (int i = 0; i < selections.Length; i++)
            {
                GameObject activeGameObject = selections[i];
                Undo.RegisterCompleteObjectUndo(activeGameObject, "RenameByProperty");

                foreach (var target in activeGameObject.GetComponents<Component>())
                {
                    FieldInfo[] fields = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    foreach (FieldInfo field in fields)
                    {
                        var cleanName = LowerBack(field.Name.ToLower().Replace("list_", ""));
                        // Kiểm tra xem field có thuộc loại Component hay không
                        if (typeof(Component).IsAssignableFrom(field.FieldType))
                        {
                            Component child = field.GetValue(target) as Component;
                            if (child != null && child.gameObject.name != activeGameObject.name)
                            {
                                child.gameObject.name = cleanName;
                            }
                            else
                            {
                                Debug.LogWarning($"{field.FieldType} attact to '{field.Name}' not found.");
                            }
                        }
                        else if (typeof(GameObject).IsAssignableFrom(field.FieldType))
                        {
                            GameObject child = field.GetValue(target) as GameObject;
                            if (child != null && child.gameObject.name != child.name)
                            {
                                child.name = cleanName;
                            }
                            else
                            {
                                Debug.LogWarning($"Gameobject attact to '{field.Name}' not found.");
                            }
                        }
                        else if (field.FieldType.IsGenericType)
                        {
                            if (field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                            {
                                var genericType = field.FieldType.GetGenericArguments()[0];
                                if (typeof(Component).IsAssignableFrom(genericType))
                                {
                                    var listType = typeof(List<>).MakeGenericType(genericType);
                                    var components = (IList)field.GetValue(target);
                                    for (int index = 0; index < components.Count; index++)
                                    {
                                        var component = (Component)components[index];
                                        if (component)
                                        {
                                            component.name = cleanName + "_" + index;
                                        }
                                    }
                                }
                                else if (typeof(GameObject).IsAssignableFrom(genericType))
                                {
                                    List<GameObject> child = field.GetValue(target) as List<GameObject>;
                                    for (int index = 0; index < child.Count; index++)
                                    {
                                        if (child[index])
                                            child[index].name = cleanName + "_" + index;
                                    }
                                }
                                else
                                {
                                    Debug.LogWarning(genericType);
                                }
                            }
                            else
                            {
                                Debug.LogWarning(field.FieldType);
                            }
                        }
                        else if (field.FieldType.IsArray)
                        {
                            var arrayType = field.FieldType.GetElementType();
                            if (typeof(Component).IsAssignableFrom(arrayType))
                            {
                                Component[] child = field.GetValue(target) as Component[];
                                for (int index = 0; index < child.Length; index++)
                                {
                                    if (child[index])
                                    {
                                        child[index].transform.name = cleanName + "_" + index;
                                    }
                                }
                            }
                            else if (typeof(GameObject).IsAssignableFrom(arrayType))
                            {
                                GameObject[] child = field.GetValue(target) as GameObject[];
                                for (int index = 0; index < child.Length; index++)
                                {
                                    if (child[index])
                                        child[index].name = cleanName + "_" + index;
                                }
                            }
                            else
                            {
                                Debug.LogWarning(arrayType);
                            }
                        }

                    }
                }

                if (!EditorSceneManager.IsPreviewScene(activeGameObject.scene))
                {
                    SaveOverridePrefab(activeGameObject);
                    EditorSceneManager.SaveScene(activeGameObject.scene);
                }
                else
                {
                    EditorUtility.SetDirty(activeGameObject);
                }
            }

        }
        #endregion

        #region Modified Name Func
        private static string LowerBack(string str)
        {

            string result = Regex.Replace(str, "(^|_)([a-z])", match => match.Groups[0].Value.ToUpper());

            return result;
        }
        private static void CachedData(GameObject root)
        {
            Debug.LogWarning($"=======<color=red>Cached {root.transform.name}</color>=======");
            saveCache.Clear();
            var rootCache = new TransformCache() {trans=root.transform, parent = null, level = 0,baseName = CleaningName(root.name)};
            GetAllName(rootCache);
            RenameByData(false);
        }
        private static void RenameByData(bool isUniform = false)
        {
            foreach (var dict in saveCache)
            {
                commandCount++;
                var value = dict.Value;
                if (value.Count == 1)
                {
                    commandCount++;
                    value[0].trans.name = value[0].baseName;
                }
                else
                {
                    commandCount++;
                    for (int i = 0; i < value.Count; i++)
                    {
                        commandCount++;
                        if (isUniform)
                        {
                            commandCount++;
                            value[i].trans.name = value[i].baseName;
                        }
                        else
                        {
                            commandCount++;
                            if (value[i].IsLast)
                            {
                                commandCount++;
                                value[i].trans.name = value[i].baseName;
                            }
                            else
                            {
                                commandCount++;
                                value[i].trans.name = value[i].baseName + "_" + i;
                            }
                        }
                    }
                }
            }
        }
        private static void GetAllName(TransformCache activeGameObject)
        {
            string clearName;
            List<TransformCache> list = new List<TransformCache>();
            commandCount++;
            foreach (TransformCache child in activeGameObject.childs)
            {
                clearName = CleaningName(child.trans.name);
                child.baseName = clearName;
                if (!saveCache.ContainsKey(clearName.ToLower()))
                {
                    commandCount++;
                    saveCache.Add(clearName.ToLower(), new List<TransformCache>() { child });
                }
                else
                {
                    commandCount++;
                    if (child.trans.name.ToLower().Contains(clearName.ToLower()))
                    {
                        commandCount++;
                        saveCache[clearName.ToLower()].Add(child);
                    }
                }

                if (child.trans.childCount > 0)
                {
                    commandCount++;
                    list.Add(child);
                }
            }
            for (int i = 0; i < list.Count; i++)
            {
                GetAllName(list[i]);
            }
        }
        private static string CleaningName(string clearName)
        {
            // Remove non-alphanumeric characters
            string clean = Regex.Replace(clearName, "[^a-zA-Z_]", "");
            commandCount++;
            if (clean[clean.Length - 1].CompareTo('_') == 0)
            {
                clean = clean.Remove(clean.Length - 1,1);
                commandCount++;
            }
            return clean;
        }
        #endregion
        private static void SaveOverridePrefab(GameObject activeGameObject)
        {
            if (!EditorSceneManager.IsPreviewScene(activeGameObject.scene))
            {
                if (PrefabUtility.IsPartOfPrefabInstance(activeGameObject))
                {
                    GameObject prefabParent = PrefabUtility.GetCorrespondingObjectFromSource(activeGameObject);
                    if (prefabParent != null)
                    {
                        // Tạo một prefab mới
                        string prefabPath = AssetDatabase.GetAssetPath(prefabParent);
                        PrefabUtility.SaveAsPrefabAssetAndConnect(activeGameObject, prefabPath, InteractionMode.AutomatedAction);
                    }
                }
                EditorSceneManager.SaveScene(activeGameObject.scene);
            }

        }
    }
//public class ReplaceEditor : EditorWindow
//{
//    static string root;
//    string replace;
//    [MenuItem("GameTool/InspectorHelper/Rename Selected %h", priority = 1)]
//    private static void RenameSelected()
//    {
//        ReplaceEditor window = (ReplaceEditor)EditorWindow.GetWindow(typeof(ReplaceEditor));
//        root = Selection.activeGameObject.name;
//        window.position = new Rect(200, 300, 500, 150);
//        window.Show();
//    }
//    void OnGUI()
//    {
//        // Hiển thị nội dung trong Panelss
//        GUILayout.BeginVertical();
//        GUILayout.Label("Replace Name");
//        root = EditorGUILayout.TextField("Find...", root);
//        replace = EditorGUILayout.TextField("Replace...", replace);

//        if (GUILayout.Button("Rename"))
//        {
//            Debug.Log(root);
//            Debug.Log(replace);
//            Rename(root, replace);
//        }
//        GUILayout.EndVertical();
//    }

//    void Rename(string root, string replace)
//    {
//        GameObject[] selections = Selection.gameObjects;
//        foreach (GameObject activeGameObject in selections)
//        {
//            RenameAllChild(activeGameObject.transform, root, replace);

//            if (!EditorSceneManager.IsPreviewScene(activeGameObject.scene))
//            {
//                Debug.Log("Not in preview");
//                SaveOverridePrefab(activeGameObject);
//                EditorSceneManager.SaveScene(activeGameObject.scene);
//            }
//            else
//            {
//                Debug.Log("In preview");
//                EditorUtility.SetDirty(activeGameObject);
//            }
//        }
//    }

//    void SaveOverridePrefab(GameObject activeGameObject)
//    {
//        if (!EditorSceneManager.IsPreviewScene(activeGameObject.scene))
//        {
//            if (PrefabUtility.IsPartOfPrefabInstance(activeGameObject))
//            {
//                GameObject prefabParent = PrefabUtility.GetCorrespondingObjectFromSource(activeGameObject);
//                if (prefabParent != null)
//                {
//                    // Tạo một prefab mới
//                    string prefabPath = AssetDatabase.GetAssetPath(prefabParent);
//                    Debug.Log(prefabPath);
//                    PrefabUtility.SaveAsPrefabAssetAndConnect(activeGameObject, prefabPath, InteractionMode.AutomatedAction);
//                }
//            }
//            EditorSceneManager.SaveScene(activeGameObject.scene);
//        }

//    }
//    // Hàm tìm kiếm đệ quy để tìm tất cả child
//    void RenameAllChild(Transform parent, string root, string replace)
//    {
//        if (parent.name.CompareTo(root) == 0)
//        {
//            Undo.RecordObject(parent.gameObject, "lastChange");
//            parent.name = replace;
//        }
//        foreach (Transform child in parent)
//        {
//            RenameAllChild(child, root, replace);
//        }
//    }
//}
    public class CopyComponentsWindows : EditorWindow
    {
        GameObject sourceObject;
        GameObject targetObject;
        [MenuItem("GameTool/InspectorHelper/Copy Components %h")]
        public static void CopyComponent()
        {
            GetWindow<CopyComponentsWindows>("Copy Components");
        }

        void OnGUI()
        {
            sourceObject = (GameObject)EditorGUILayout.ObjectField("Source Object", sourceObject, typeof(GameObject), true);
            targetObject = (GameObject)EditorGUILayout.ObjectField("Target Object", targetObject, typeof(GameObject), true);

            if (GUILayout.Button("Copy Components"))
            {
                if (sourceObject != null && targetObject != null)
                {
                    Undo.RegisterCompleteObjectUndo(targetObject, "Copy Components");

                    targetObject.name = sourceObject.name;
                    targetObject.layer = sourceObject.layer;
                    targetObject.tag = sourceObject.tag;

                    Component[] components = sourceObject.GetComponents<Component>();
                    foreach (Component component in components)
                    {
                        UnityEditorInternal.ComponentUtility.CopyComponent(component);
                        var targetCom = targetObject.GetComponent(component.GetType());
                        if (targetCom)
                            UnityEditorInternal.ComponentUtility.PasteComponentValues(targetCom);
                        else
                            UnityEditorInternal.ComponentUtility.PasteComponentAsNew(targetObject);

                        targetCom = targetObject.GetComponent(component.GetType());
                        FieldInfo[] fields = targetCom.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        // Duyệt qua từng field
                        foreach (FieldInfo field in fields)
                        {
                            try
                            {
                                if (typeof(Component).IsAssignableFrom(field.FieldType))
                                {
                                    // Kiểm tra xem giá trị của field có phải là chính nó hay không
                                    Component fieldValue = field.GetValue(component) as Component;
                                    if (fieldValue != null && fieldValue.gameObject == sourceObject)
                                    {
                                        // Gán giá trị mới cho field
                                        field.SetValue(targetCom, targetObject.GetComponent(field.FieldType));
                                    }
                                }
                                else if (typeof(GameObject).IsAssignableFrom(field.FieldType))
                                {
                                    // Kiểm tra xem giá trị của field có phải là chính nó hay không
                                    GameObject fieldValue = field.GetValue(component) as GameObject;
                                    if (fieldValue != null && fieldValue.gameObject == sourceObject)
                                    {
                                        // Gán giá trị mới cho field
                                        field.SetValue(targetCom, targetObject);
                                    }
                                }
                            }
                            catch (System.Exception err)
                            {
                                Debug.LogWarning(err.Message);
                                continue;
                            }
                        }
                    }
                }
            }
        }
    }

    public class NameConvention
    {
        public string renderer = "renderer";
        public string Image = "img";
        public string Button = "btn";
        public string Toggle = "toggle";
        public string Text = "txt";
        public string GameObject = "obj";
    }
    public class TransformCache
    {
        public int level = 0;
        public string baseName = String.Empty;
        public Transform trans;
        public TransformCache parent;
        public List<TransformCache> childs { 
            get
            {
                if (trans.childCount == 0)
                {
                    childsCached = new List<TransformCache>();
                    InspectorHelperEditor.commandCount++;
                }
                else
                {
                    foreach (Transform child in trans)
                    {
                        childsCached.Add(new TransformCache() { level = level + 1, trans = child, parent = this });
                        InspectorHelperEditor.commandCount++;
                    }
                }
                return childsCached;
            }
        }
        List<TransformCache> childsCached = new List<TransformCache>();
        public List<Type> components = new List<Type>();
        public bool IsRoot
        {
            get
            {
                InspectorHelperEditor.commandCount++;
                return level == 0;
            }
        }
        public bool IsLast
        {
            get
            {
                InspectorHelperEditor.commandCount++;
                return trans.childCount == 0;
            }
        }
    }
}