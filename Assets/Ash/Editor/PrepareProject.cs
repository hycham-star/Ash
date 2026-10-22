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
        public const string SettlementScene = "Assets/Ash/Scenes/SettlementScene.unity";

        [MenuItem("Ashlands/Open Settlement Scene", false, 0)]
        public static void OpenSettlementScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Prepare();
            EditorSceneManager.OpenScene(SettlementScene);
        }

        [MenuItem("Ashlands/Test Scenes/PvE Battle Scene")]
        public static void OpenBattleScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Prepare();
            EditorSceneManager.OpenScene(BattleScene);
        }

        [MenuItem("Ashlands/Test Scenes/Hero Army Test Scene")]
        public static void OpenHeroArmyScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Prepare();
            EditorSceneManager.OpenScene(HeroArmyScene);
        }

        [MenuItem("Ashlands/Test Scenes/Phase 1 Test Scene")]
        public static void OpenTestScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Prepare();
            EditorSceneManager.OpenScene(TestScene);
        }

        [MenuItem("Ashlands/Prepare Project", false, 10)]
        public static void Prepare()
        {
            if (!File.Exists(TestScene) || !File.Exists(HeroArmyScene) || !File.Exists(BattleScene) || !File.Exists(SettlementScene)) throw new InvalidOperationException("Missing tracked test scene.");
            if (!EditorBuildSettings.scenes.Any(scene => scene.path == HeroArmyScene))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(HeroArmyScene, true) }).ToArray();
            if (!EditorBuildSettings.scenes.Any(scene => scene.path == BattleScene))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(BattleScene, true) }).ToArray();
            if (!EditorBuildSettings.scenes.Any(scene => scene.path == SettlementScene))
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(SettlementScene, true) }.Concat(EditorBuildSettings.scenes).ToArray();
            // Runtime lights need the built-in screen-space shadow shader in Android players too.
            UnityEngine.Rendering.GraphicsSettings.SetShaderMode(UnityEngine.Rendering.BuiltinShaderType.ScreenSpaceShadows,UnityEngine.Rendering.BuiltinShaderMode.UseBuiltin);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.companyName = "Ashlands";
            PlayerSettings.productName = "Ashlands: Rise of the Remnants";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.ash.afterthefall");
        }

        public static void BuildAndroid()
        {
            Prepare();
            Directory.CreateDirectory("Builds");
            var result = BuildPipeline.BuildPlayer(new[] { SettlementScene, BattleScene, HeroArmyScene }, "Builds/Ashlands.apk", BuildTarget.Android, BuildOptions.None);
            if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException("Android build failed: " + result.summary.result);
        }
    }
}
