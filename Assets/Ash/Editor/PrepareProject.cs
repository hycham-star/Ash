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
        public const string HeroArmyScene = "Assets/Ash/Scenes/HeroArmyTest.unity";
        public const string BattleScene = "Assets/Ash/Scenes/BattleScene.unity";

        [MenuItem("Ashlands/Open PvE Battle Scene")]
        public static void OpenBattleScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Prepare();
            EditorSceneManager.OpenScene(BattleScene);
        }

        [MenuItem("Ashlands/Open Hero Army Test Scene")]
        public static void OpenHeroArmyScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Prepare();
            EditorSceneManager.OpenScene(HeroArmyScene);
        }

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
            if (!File.Exists(TestScene) || !File.Exists(HeroArmyScene) || !File.Exists(BattleScene)) throw new InvalidOperationException("Missing tracked test scene.");
            if (!EditorBuildSettings.scenes.Any(scene => scene.path == HeroArmyScene))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(HeroArmyScene, true) }).ToArray();
            if (!EditorBuildSettings.scenes.Any(scene => scene.path == BattleScene))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(BattleScene, true) }).ToArray();
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.companyName = "Ashlands";
            PlayerSettings.productName = "Ashlands: Rise of the Remnants";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.ash.afterthefall");
        }

        public static void BuildAndroid()
        {
            Prepare();
            Directory.CreateDirectory("Builds");
            var result = BuildPipeline.BuildPlayer(new[] { BattleScene }, "Builds/Ashlands.apk", BuildTarget.Android, BuildOptions.None);
            if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException("Android build failed: " + result.summary.result);
        }
    }
}
