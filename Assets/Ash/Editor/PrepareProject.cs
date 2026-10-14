using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ash.Editor
{
    public static class PrepareProject
    {
        public const string TestScene = "Assets/Ash/Scenes/Phase1Test.unity";

        [MenuItem("Ashlands/Open Phase 1 Test Scene")]
        public static void OpenTestScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Prepare();
            EditorSceneManager.OpenScene(TestScene);
        }

        [MenuItem("Ashlands/Prepare Project")]
        public static void Prepare()
        {
            if (!File.Exists(TestScene)) throw new InvalidOperationException("Missing tracked Phase 1 test scene.");
            if (!EditorBuildSettings.scenes.Any(scene => scene.path == TestScene))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(TestScene, true) }).ToArray();
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.companyName = "Ashlands";
            PlayerSettings.productName = "Ashlands: Rise of the Remnants";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.ash.afterthefall");
        }

        public static void BuildAndroid()
        {
            Prepare();
            Directory.CreateDirectory("Builds");
            var result = BuildPipeline.BuildPlayer(new[] { TestScene }, "Builds/Ashlands.apk", BuildTarget.Android, BuildOptions.None);
            if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException("Android build failed: " + result.summary.result);
        }
    }
}
