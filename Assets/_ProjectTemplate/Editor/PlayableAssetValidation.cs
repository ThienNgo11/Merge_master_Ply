#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Funlab.MergeMaster;

// Batch entry point: -executeMethod PlayableAssetValidation.Run
public static class PlayableAssetValidation
{
    [Serializable] private class Entry { public string path; }
    [Serializable] private class Manifest { public Entry[] files; }

    public static void Run()
    {
        try
        {
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText("Reports/playable-cleanup-candidates.json"));
            var candidates = new HashSet<string>(manifest.files.Select(f => f.path));
            var retained = AssetDatabase.GetAllAssetPaths().Where(p =>
                p.StartsWith("Assets/") && !AssetDatabase.IsValidFolder(p) && !candidates.Contains(p)).ToArray();
            var dependencies = new HashSet<string>(AssetDatabase.GetDependencies(retained, true));
            var conflicts = candidates.Where(dependencies.Contains).ToArray();
            if (conflicts.Length > 0)
                throw new Exception("Unity found retained dependencies in cleanup manifest: " + string.Join(", ", conflicts));

            var scene = EditorSceneManager.OpenScene("Assets/_ProjectTemplate/Scenes/GamePlay.unity", OpenSceneMode.Single);
            var core = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameplayCore>(true)).Single();
            if (core.stageAdvancement.Length != 1 || core.stageAdvancement[0].stages.Length != 3 ||
                core.stageAdvancement[0].stages.Any(s => s == null))
                throw new Exception("Playable must contain exactly three valid stage layouts.");
            if (core.uiDirector == null) throw new Exception("Missing UIDirector.");
            File.WriteAllText("Reports/unity-playable-validation.txt",
                "PASS: C# compilation; Unity dependency closure excludes all cleanup candidates; three valid levels and UIDirector.\n" +
                "Candidate count: " + candidates.Count + "\n" +
                "This does not verify a Playworks export or gameplay in a browser.\n");
            Debug.Log("PLAYABLE_VALIDATION_PASS");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorApplication.Exit(1);
        }
    }
}
#endif
