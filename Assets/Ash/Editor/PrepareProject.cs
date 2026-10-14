using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ash.Editor
{
    [InitializeOnLoad]
    public static class PrepareProject
    {
        static PrepareProject() { EditorApplication.delayCall += Prepare; }

        [MenuItem("Ash/Prepare Project")]
        public static void Prepare()
        {
            const string scenePath = "Assets/Ash/Scenes/Settlement.unity";
            if (!File.Exists(scenePath))
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                // Preserve any unsaved scene the developer is already editing.
                for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) return;
                Directory.CreateDirectory("Assets/Ash/Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                EditorSceneManager.SaveScene(scene, scenePath);
                EditorSceneManager.CloseScene(scene, true);
                AssetDatabase.Refresh();
                if (string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path))
                    EditorSceneManager.OpenScene(scenePath);
            }
            if (EditorBuildSettings.scenes.Length == 0)
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.companyName = "Ash";
            PlayerSettings.productName = "Ash: After the Fall";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.ash.afterthefall");
        }

        public static void BuildAndroid()
        {
            Prepare();
            Directory.CreateDirectory("Builds");
            var result = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "Builds/Ash.apk", BuildTarget.Android, BuildOptions.None);
            if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception("Android build failed: " + result.summary.result);
        }
    }
}
