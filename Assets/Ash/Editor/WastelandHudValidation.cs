using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ash.Editor
{
    public static class WastelandHudValidation
    {
        [MenuItem("Ashlands/Validate Wasteland HUD")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Validate outside Play mode.");
            Scene scene=default;Texture2D backup=null;GameObject presentation=null;SettlementView view=null;
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            object Field(string name)=>typeof(SettlementView).GetField(name,flags).GetValue(view);
            void Set(string name,object value)=>typeof(SettlementView).GetField(name,flags).SetValue(view,value);
            void Call(string name)=>typeof(SettlementView).GetMethod(name,flags).Invoke(view,null);
            try
            {
                var atlas=Resources.Load<Texture2D>("HUD/WastelandHUD");Require(atlas!=null && atlas.width==1280 && atlas.height==1280,"Missing full-resolution HUD atlas.");
                var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(atlas));
                Require(!importer.mipmapEnabled && importer.npotScale==TextureImporterNPOTScale.None && importer.alphaSource==TextureImporterAlphaSource.FromInput,"HUD atlas import must preserve cells and alpha.");
                var raw=Resources.Load<TextAsset>("HUD/WastelandHUDPixels");Require(raw!=null,"Missing HUD backup.");backup=WastelandHudSkin.CreateFromPixels(raw.bytes);
                Require(backup.width==1280 && backup.height==1280,"Native HUD backup decoding failed.");Require(Resources.Load<Font>("HUD/OutpostTitle")!=null,"Missing HUD title font.");
                scene=EditorSceneManager.OpenPreviewScene(PrepareProject.SettlementScene);
                var manager=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<GameManager>(true)).Single();manager.Initialize();
                Require(!manager.PersistenceActive,"HUD checks must not open player persistence.");manager.Resources.Add(ResourceType.Lumber,1400);
                view=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<SettlementView>(true)).Single();
                var refs=new SerializedObject(view).FindProperty("encounters");var templates=new List<NpcEncounter>();
                for(int i=0;i<refs.arraySize;i++)templates.Add(((EncounterConfig)refs.GetArrayElementAtIndex(i).objectReferenceValue).CreateEncounter());manager.WorldMap.ConfigureEncounters(templates);
                var progress=new ProgressPersistence(manager.HeroArmies,manager.Resources,manager.Fragments,manager.Buildings,manager.Scavenging,manager.OfflineScavenging);var codec=new UnityProgressCodec();string before=codec.Encode(progress.Capture());
                presentation=new GameObject("Disposable themed HUD");SceneManager.MoveGameObjectToScene(presentation,scene);
                var camera=new GameObject("Disposable HUD camera",typeof(Camera));camera.transform.SetParent(presentation.transform,false);var controls=camera.AddComponent<SettlementCamera>();controls.ResetView();Set("controls",controls);
                Set("presentation",presentation);Set("font",Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));Call("BuildUI");Call("Refresh");Canvas.ForceUpdateCanvases();
                Button Control(string name)=>presentation.GetComponentsInChildren<Button>(true).Single(button=>button.name==name);
                var values=(Dictionary<ResourceType,Text>)Field("hudResourceValues");Require(values.Count==5 && values[ResourceType.Lumber].text=="1.4K","Resource cards must display live ledger values.");
                Require(((RawImage)Field("hudMiniTerrain")).texture==((ScavengingMapUI)Field("scavengingMap")).OverviewTexture,"Thumbnail must share the real map texture.");
                Require(presentation.GetComponentsInChildren<WastelandMetalFrame>(true).All(frame=>frame.mainTexture!=null && !frame.raycastTarget),"Decorative frames must have artwork and must not intercept input.");
                Control("WORLD MAP").onClick.Invoke();Require(((ScavengingMapUI)Field("scavengingMap")).gameObject.activeSelf,"World map card is not wired.");Control("HOME").onClick.Invoke();
                Control("BARRACKS").onClick.Invoke();Require(((GameObject)Field("barracksPanel")).activeSelf,"Barracks navigation is not wired.");Control("HOME").onClick.Invoke();
                Control("CONSTRUCTION").onClick.Invoke();Require(((DevelopmentUI)Field("developmentUI")).gameObject.activeSelf,"Construction navigation is not wired.");Control("HOME").onClick.Invoke();
                Control("HOSPITAL").onClick.Invoke();Require(((HospitalUI)Field("hospitalUI")).gameObject.activeSelf,"Hospital navigation is not wired.");Control("HOME").onClick.Invoke();
                Control("HEROES").onClick.Invoke();Require(((GameObject)Field("heroPanel")).activeSelf,"Heroes navigation is not wired.");Control("HOME").onClick.Invoke();
                Control("WASTELAND RAID").onClick.Invoke();Require(((ScavengingMapUI)Field("scavengingMap")).gameObject.activeSelf,"Raid must open the real map.");Control("HOME").onClick.Invoke();
                Control("SETTINGS").onClick.Invoke();var utility=(GameObject)Field("utilityPanel");Require(utility.activeSelf,"Settings failed to open.");
                Require(utility.GetComponentsInChildren<Button>(true).Select(button=>button.GetComponentInChildren<Text>()?.text).Contains("RESET"),"Reset must remain available inside Settings.");
                utility.GetComponentsInChildren<Button>(true).Single(button=>button.GetComponentInChildren<Text>()?.text=="X").onClick.Invoke();Require(!utility.activeSelf,"Settings close failed.");
                Require(codec.Encode(progress.Capture())==before && !manager.PersistenceActive,"HUD navigation mutated progression or player persistence.");
                Debug.Log("PASS Unity wasteland HUD: atlas alpha/size/import, native raw fallback, font, live resource cards, real map thumbnail, navigation and Settings isolation. Disposable preview only. Verify portrait layout, touch and appearance on Android.");
            }
            catch(Exception error){Debug.LogException(error);if(Application.isBatchMode)EditorApplication.Exit(1);throw;}
            finally
            {
                if(view!=null){typeof(SettlementView).GetMethod("ReleaseWastelandHUD",flags).Invoke(view,null);Set("presentation",null);}
                if(presentation!=null)UnityEngine.Object.DestroyImmediate(presentation);
                if(backup!=null)UnityEngine.Object.DestroyImmediate(backup);
                if(scene.IsValid() && scene.isLoaded)EditorSceneManager.ClosePreviewScene(scene);
            }
        }
        private static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
    }
}
