using System.Collections.Generic;
using GameTool.Audio.Scripts;
using UnityEditor;
using UnityEngine;

namespace GameTool.Audio.Editor
{
    public class ImproveAudio : EditorWindow
    {
        [MenuItem("GameTool/Audio/AudioImporter")]
        private static void ShowWindow()
        {
            var window = GetWindow<ImproveAudio>();
            window.titleContent = new GUIContent("Audio Importer");
            window.Show();
        }

        List<string> filePaths = new List<string>();
        List<AudioClip> listAudioClip = new List<AudioClip>();

        AudioTable audioTable;

        void OnGUI()
        {
            FindAudios();

            GUILayout.Label("Total File: " + listAudioClip.Count);

            if (GUILayout.Button("APPLY"))
            {
                Improve();
            }
        }

        private void FindAudios()
        {
            filePaths.Clear();
            listAudioClip.Clear();
            Object[] selectedObjects = Selection.GetFiltered(typeof(AudioClip), SelectionMode.DeepAssets);

            for (int i = 0; i < selectedObjects.Length; i++)
            {
                filePaths.Add(AssetDatabase.GetAssetPath(selectedObjects[i]));
                listAudioClip.Add(selectedObjects[i] as AudioClip);
            }
        }

        private void Improve()
        {
            audioTable = Resources.Load<AudioTable>("AudioTable");
            audioTable.Improve();

            for (int i = 0; i < listAudioClip.Count; i++)
            {
                AudioClip audio = listAudioClip[i];

                string filePath = filePaths[i];
                
                AudioTable.ImproveSetting(filePath, audio, false, false);
            }

            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }
    }
}