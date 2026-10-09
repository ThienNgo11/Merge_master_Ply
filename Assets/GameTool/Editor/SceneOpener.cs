using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace GameTool.Editor
{
    public class SceneOpener : EditorWindow
    {
        private EditorBuildSettingsScene[] scenes;

        [MenuItem("MyTools/Scene Opener")]
        public static void ShowWindow()
        {
            GetWindow<SceneOpener>("Scene Opener");
        }

        private Vector2 scrollPosition;

        private void OnEnable()
        {
            scenes = EditorBuildSettings.scenes;
        }

        void OnGUI()
        {
            if (EditorBuildSettings.scenes.Length != scenes.Length)
            {
                scenes = EditorBuildSettings.scenes;
            }

            // Hiển thị danh sách các scene trong build settings
            GUILayout.Label("Scenes in Build Settings:", EditorStyles.boldLabel);

            // Tạo một biến Vector2 để lưu vị trí của scroll view
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            int sceneCount = scenes.Length;
            for (int i = 0; i < sceneCount; i++)
            {
                EditorBuildSettingsScene scene = scenes[i];
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scene.path);
                if (GUILayout.Button(sceneName))
                {
                    // Mở scene khi nhấn nút
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(scene.path);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }
}