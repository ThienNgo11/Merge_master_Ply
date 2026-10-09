using UnityEditor;
using UnityEngine;

namespace GameTool.Editor
{
    public class DragToPrefabs : EditorWindow
    {
        [SerializeField] private Object folder;
        private GameObject[] selectedObjects;

        [MenuItem("MyTools/DragToPrefabs")]
        private static void ShowWindow()
        {
            var window = GetWindow<DragToPrefabs>();
            window.titleContent = new GUIContent("DragToPrefabs");
            window.Show();
        }

        private void OnGUI()
        {
            folder = EditorGUILayout.ObjectField(folder, typeof(Object), true);
            var path = AssetDatabase.GetAssetPath(folder);
            selectedObjects = Selection.gameObjects;
            if (GUILayout.Button("Convert all to prefabs"))
            {
                foreach (GameObject obj in selectedObjects)
                {
                    PrefabUtility.SaveAsPrefabAssetAndConnect(obj, path + "/" + obj.name + ".prefab", InteractionMode.UserAction);
                }
            }
        }
    }
}