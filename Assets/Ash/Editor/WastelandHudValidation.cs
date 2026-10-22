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
        [MenuItem("Ashlands/Validation/Workflows/Shared Map and Market")]
        public static void ValidateSharedMapAndMarket(){Run();ScavengingValidation.Run();RaidPreparationValidation.Run();}
        [MenuItem("Ashlands/Validation/Graphics/HUD")]
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
                Button Control(string name)
                {
                    var matches=presentation.GetComponentsInChildren<Button>(true).Where(button=>button.name==name).ToArray();
                    return matches.SingleOrDefault(button=>button.transform.parent==(Transform)Field("ui"))??matches.Single();
                }
                var values=(Dictionary<ResourceType,Text>)Field("hudResourceValues");Require(values.Count==6 && values[ResourceType.Lumber].text=="1.4K","Resource cards must display live ledger values.");
                Require(((RawImage)Field("hudMiniTerrain")).texture==((ScavengingMapUI)Field("scavengingMap")).OverviewTexture,"Thumbnail must share the real map texture.");
                Require(presentation.GetComponentsInChildren<WastelandMetalFrame>(true).All(frame=>frame.mainTexture!=null && !frame.raycastTarget),"Decorative frames must have artwork and must not intercept input.");
                Control("WORLD MAP").onClick.Invoke();Require(((ScavengingMapUI)Field("scavengingMap")).gameObject.activeSelf,"World map card is not wired.");Control("HUD HOME").onClick.Invoke();
                Require(((Text)Field("hudMapCaption")).text=="WORLD MAP","Home must restore the world navigation caption.");
                Control("WORLD MAP").onClick.Invoke();var map=(ScavengingMapUI)Field("scavengingMap");
                Require(((Text)Field("hudMapCaption")).text=="HOME" && map.transform.GetSiblingIndex()==0,"World map must replace the playfield beneath the shared HUD.");
                var mapRect=map.GetComponent<RectTransform>();Require(mapRect.anchorMin==Vector2.zero && mapRect.anchorMax==Vector2.one,"World map must be full screen.");
                Control("HEROES").onClick.Invoke();Require(((GameObject)Field("heroPanel")).activeSelf && map.gameObject.activeSelf,"Heroes must retain world-map context.");
                Control("INVENTORY").onClick.Invoke();Require(((Text)Field("navigationDetails")).text.Contains("Lumber: 1400") && map.gameObject.activeSelf,"Inventory must show real stocks over the world.");
                Control("MARKET").onClick.Invoke();Require(((GameObject)Field("marketContent")).activeSelf && !Control("BUY Food").interactable,"Unbuilt market must prevent purchases.");
                Control("MESSAGE").onClick.Invoke();Require(((Text)Field("navigationDetails")).text.Contains("LOCAL RAID REPORT"),"Message button must open actual local reports.");
                Control("CLAN").onClick.Invoke();Require(((Text)Field("navigationDetails")).text.Contains("No clan joined"),"Clan must not invent online membership.");
                Control("HUD HOME").onClick.Invoke();
                Call("ShowBarracks");Require(((GameObject)Field("barracksPanel")).activeSelf,"Existing barracks remains available through buildings.");Control("HUD HOME").onClick.Invoke();
                Call("ShowHospital");Require(((HospitalUI)Field("hospitalUI")).gameObject.activeSelf,"Existing hospital remains available through buildings.");Control("HUD HOME").onClick.Invoke();
                Control("SETTINGS").onClick.Invoke();var utility=(GameObject)Field("utilityPanel");Require(utility.activeSelf,"Settings failed to open.");
                Require(utility.GetComponentsInChildren<Button>(true).Select(button=>button.GetComponentInChildren<Text>()?.text).Contains("RESET"),"Reset must remain available inside Settings.");
                utility.GetComponentsInChildren<Button>(true).Single(button=>button.GetComponentInChildren<Text>()?.text=="X").onClick.Invoke();Require(!utility.activeSelf,"Settings close failed.");
                Require(codec.Encode(progress.Capture())==before && !manager.PersistenceActive,"HUD navigation mutated progression or player persistence.");
                manager.Resources.Add(ResourceType.Gold,30);Require(manager.Buildings.BuildPlot("plot-01",PlotKind.Market)==BuildingCommandResult.Success,"Market construction fixture failed.");
                Control("MARKET").onClick.Invoke();Require(Control("BUY Food").interactable,"Constructed market must enable affordable offer.");
                int food=manager.Resources.Get(ResourceType.Food);Control("BUY Food").onClick.Invoke();
                Require(manager.Resources.Get(ResourceType.Gold)==20 && manager.Resources.Get(ResourceType.Food)==food+100,"Native market UI failed atomic exchange.");
                string marketSave=codec.Encode(progress.Capture());progress.Restore(codec.Decode(marketSave));Require(codec.Encode(progress.Capture())==marketSave,"Native JSON lost market or gold.");
                Debug.Log("PASS Unity shared HUD and market: full-screen world/Home switch, five shared navigation actions, live resource inventory, earned-gold purchases and market JSON restore, navigation and Settings isolation. Disposable preview only. Verify portrait layout, touch and appearance on Android.");
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
