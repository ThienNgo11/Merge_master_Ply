using System.IO;
using Unity.CodeEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameTool.Editor
{
    public class MyTools
    {
        [MenuItem("MyTools/Clear Data", priority = 0)]
        private static void ClearData()
        {
            PlayerPrefs.DeleteAll();
            ClearJsonData();
        }

        [MenuItem("MyTools/Clear Json Data", priority = 1)]
        private static void ClearJsonData()
        {
            //SaveGameData.ClearData();
        }

        [MenuItem("MyTools/Clear PlayerPrefs", priority = 2)]
        private static void ClearPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
        }

        [MenuItem("MyTools/OpenNextScene &1")]
        private static void OpenNextScene()
        {
            int index = SceneManager.GetActiveScene().buildIndex;
            if (index + 1 >= EditorBuildSettings.scenes.Length)
            {
                index = 0;
            }
            EditorSceneManager.OpenScene(EditorBuildSettings.scenes[index + 1].path);
        }

        [MenuItem("MyTools/OpenPreviousScene &`")]
        private static void OpenPreviousScene()
        {
            int index = SceneManager.GetActiveScene().buildIndex;
            if (index - 1 < 0)
            {
                index = EditorBuildSettings.scenes.Length - 1;
            }
            else
            {
                index--;
            }
            EditorSceneManager.OpenScene(EditorBuildSettings.scenes[index].path);
        }

        [MenuItem("MyTools/Screen Shot Without Canvas &z")]
        private static void GetScrShot()
        {
            Canvas canvas = GameObject.FindObjectOfType<Canvas>();
            canvas?.gameObject.SetActive(false);

            // File path
            string folderPath = "D:/Screenshots/";
            string fileName = "scr";

            // Create the folder beforehand if not exists
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            int i = 0;
            while (File.Exists(folderPath + fileName + ".png"))
            {
                fileName = "scr" + i;
                i++;
            }
            Debug.Log(folderPath + fileName + ".png");
            // Capture and store the screenshot
            ScreenCapture.CaptureScreenshot(folderPath + fileName + ".png");
        }


        [MenuItem("MyTools/Screen Shot With Canvas &a")]
        private static void GetScrShotCanvas()
        {
            // File path
            string folderPath = "D:/Screenshots/";
            string fileName = "scr";

            // Create the folder beforehand if not exists
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            int i = 0;
            while (File.Exists(folderPath + fileName + ".png"))
            {
                fileName = "scr" + i;
                i++;
            }
            Debug.Log(folderPath + fileName + ".png");
            // Capture and store the screenshot
            ScreenCapture.CaptureScreenshot(folderPath + fileName + ".png");
        }


        [MenuItem("MyTools/Re-Initialize CodeEditor")]
        private static void InitializeCodeEditor()
        {
            CodeEditor.CurrentEditor.Initialize(CodeEditor.CurrentEditorInstallation);
        }
    
        [MenuItem("MyTools/Open Internal Storage")]
        private static void OpenStorage()
        {
#if UNITY_EDITOR_WIN
            string path = Application.persistentDataPath.Replace('/', '\\'); // Đổi tất cả dấu '/' thành '\'
            System.Diagnostics.Process.Start("cmd.exe", $"/c start explorer \"{path}\"");
#endif
            // Đối với macOS
#if UNITY_EDITOR_OSX
        System.Diagnostics.Process.Start("open", Application.persistentDataPath);
#endif
        }
    }
}
