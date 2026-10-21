using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ash.Editor
{
    public static class ScavengingValidation
    {
        [MenuItem("Ashlands/Validate Overworld Map")]
        public static void ValidateOverworld(){Run();RaidPreparationValidation.Run();}
        [MenuItem("Ashlands/Validate World Map")]
        public static void ValidateWorldMap()=>Run();
        [Serializable]
        private sealed class LegacyV2
        {
            public int Version;
            public string Format;
            public HeroArmyState Armies;
            public ResourceAmount[] Resources;
            public FragmentState[] Fragments;
            public BuildingState[] Buildings;
        }
        [MenuItem("Ashlands/Validate Map Scavenging")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Validate outside Play mode.");
            Scene scene=default;
            string directory=Path.Combine(Path.GetTempPath(),"Ashlands-map-validation-"+Guid.NewGuid().ToString("N"));
            try {
                scene=EditorSceneManager.OpenPreviewScene(PrepareProject.SettlementScene);
                var manager=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameManager>(true)).Single();
                manager.Initialize(); Require(!manager.PersistenceActive,"Validation must not touch the real save slot.");
                var service=manager.Scavenging;
                var nodes=service.Capture().Sites;
                Require(nodes.Length==16 && nodes.Select(n=>n.Resource).Distinct().Count()==4,"Map must have all four resources.");
                var lumber=nodes.First(n=>n.Resource==ResourceType.Lumber);
                var codec=new UnityProgressCodec();
                var progress=new ProgressPersistence(manager.HeroArmies,manager.Resources,manager.Fragments,manager.Buildings,service);
                var store=new LocalSaveStore(directory,codec,progress.Validate);
                var canvas=new GameObject("Disposable scavenging UI",typeof(RectTransform),typeof(Canvas));
                SceneManager.MoveGameObjectToScene(canvas,scene); canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
                var map=ScavengingMapUI.Create(canvas.transform,service,UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
                var view=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SettlementView>(true)).Single();
                var references=new SerializedObject(view).FindProperty("encounters");
                var templates=new System.Collections.Generic.List<NpcEncounter>();
                for(int i=0;i<references.arraySize;i++)templates.Add(((EncounterConfig)references.GetArrayElementAtIndex(i).objectReferenceValue).CreateEncounter());
                manager.WorldMap.ConfigureEncounters(templates);
                int chosen=0;map.ConfigureWorld(manager.WorldMap,()=>manager.Heroes[chosen],delta=>chosen=(chosen+delta+manager.Heroes.Count)%manager.Heroes.Count,_=>{});
                map.Refresh(); // Actual refresh of the inactive panel, including real button references.
                Require(!map.gameObject.activeSelf && map.GetComponentsInChildren<Button>(true).Length>=24,"Map UI controls incomplete.");
                map.Show();
                var landmarks=map.GetComponentsInChildren<RawImage>(true).Where(image=>image.name=="Miniature landmark").ToArray();
                Require(landmarks.Length==16 && landmarks.All(image=>image.texture!=null && image.texture.width==768 && !image.raycastTarget),"Miniature landmarks are missing or blocking site buttons.");
                Require(map.GetComponentsInChildren<RawImage>(true).Count(image=>image.name=="Rusty enemy base" && image.texture!=null)==WorldRegions.CampCount,"Clickable world camps missing.");
                var terrain=map.GetComponentsInChildren<RawImage>(true).Single(image=>image.name=="Textured wasteland");
                Require(terrain.texture.width==WorldRegions.TileCount*4 && landmarks.Select(image=>image.texture).Distinct().Count()==1,"Overworld must use tiled biome terrain and one shared landmark atlas.");
                var campIcons=map.GetComponentsInChildren<RawImage>(true).Where(image=>image.name=="Rusty enemy base").ToArray();
                Require(campIcons.Count(image=>image.rectTransform.parent.gameObject.activeSelf)<=9,"Far zoom must reduce camp clutter.");
                map.ZoomBy(100);Require(map.MapSize==10000 && landmarks.All(image=>image.rectTransform.parent.GetComponent<RectTransform>().rect.width>150),"Close zoom does not enlarge resource landmarks.");
                Require(campIcons.Count(image=>image.rectTransform.parent.gameObject.activeSelf)<WorldRegions.CampCount,"Offscreen camp markers were not culled at close zoom.");
                map.ZoomBy(.001f);Require(map.MapSize>=200 && map.MapSize<10000,"Map cannot zoom out again.");
                string campId=manager.WorldMap.Camps[0].Id;
                map.GetComponentsInChildren<Button>(true).Single(button=>button.name=="Enemy base • "+campId).onClick.Invoke();
                map.GetComponentsInChildren<Button>(true).Single(button=>button.name=="SCOUT").onClick.Invoke();
                Require(manager.WorldMap.IsScouted(campId),"Camp scout button is not connected to world reports.");
                var siteButton=map.GetComponentsInChildren<Button>(true).Single(button=>button.name.EndsWith(" • "+lumber.Id,StringComparison.Ordinal));
                siteButton.onClick.Invoke();
                Require(map.SelectedSiteId==lumber.Id && service.Capture().Sites.All(site=>nodes.Any(before=>before.Id==site.Id && before.X==site.X && before.Z==site.Z && site.Workers==0)),"Landmark selection changed saved positions or worker allocation.");
                Require(service.Assign(lumber.Id)==ScavengingResult.Success && service.FreeWorkers==9,"Worker allocation failed.");
                service.Advance(37);
                Require(service.Site(lumber.Id).Stored==4 && service.Site(lumber.Id).Work==56,"Lumber production incorrect.");
                var snapshot=progress.Capture(); store.Save(snapshot); string exact=codec.Encode(snapshot);
                service.Recall(lumber.Id);
                progress.Restore(store.Load().State); progress.NotifyRestored();
                Require(codec.Encode(progress.Capture())==exact,"Native map disk round trip changed coordinates, workers or stock.");
                service.Advance(23); Require(service.Recall(lumber.Id)==ScavengingResult.Success && manager.Resources.Get(ResourceType.Lumber)==8,"Lumber did not reach resource ledger.");
                Require(service.Assign(lumber.Id,2)==ScavengingResult.Success,"Team dispatch failed.");
                service.Advance(750);
                Require(service.Site(lumber.Id).Workers==0 && service.FreeWorkers==10 && manager.Resources.Get(ResourceType.Lumber)==208,"Full team must auto-deliver and return.");
                service.Advance(750);
                Require(manager.Resources.Get(ResourceType.Lumber)==208,"Returned team must not auto-repeat.");
                store.Save(progress.Capture());progress.Restore(store.Load().State);
                Require(service.Site(lumber.Id).Workers==0 && manager.Resources.Get(ResourceType.Lumber)==208,"Completed trip failed native disk restart.");
                manager.Buildings.Upgrade(BuildingKind.Headquarters);
                var old=progress.Capture();
                string oldJson=JsonUtility.ToJson(new LegacyV2 { Version=2,Format=old.Format,Armies=old.Armies,Resources=old.Resources.Where(r=>r.Type!=ResourceType.Lumber).ToArray(),Fragments=old.Fragments,Buildings=old.Buildings });
                progress.Restore(codec.Decode(oldJson)); progress.NotifyRestored();
                Require(service.FreeWorkers==10 && manager.Resources.Get(ResourceType.Lumber)==0 && manager.Buildings.Level(BuildingKind.Headquarters)==2,"Version-two migration lost buildings or failed to initialize map.");
                var hero=manager.Heroes[0];manager.HeroArmies.AssignFighters(hero.InstanceId,manager.Units[0].Definition.Id,5);
                var gatherSite=service.Capture().Sites[0];manager.WorldMap.Dispatch(hero.InstanceId,gatherSite.Id,MapMissionKind.Gather);
                map.Refresh();
                Require(map.GetComponentsInChildren<RawImage>(true).Count(image=>(image.name=="Army bus" || image.name=="Hero bike") && image.texture!=null)==2,"Army bus/bike convoy not connected to active mission.");
                int travel=manager.WorldMap.TravelSeconds(manager.WorldMap.Mission(hero.InstanceId));
                manager.WorldMap.Advance(travel);map.Refresh();
                Require(manager.WorldMap.Mission(hero.InstanceId).Phase==MapMissionPhase.Gathering,"Army did not enter gathering phase.");
                Require(!map.GetComponentsInChildren<RawImage>(true).Any(image=>image.name=="Army bus" || image.name=="Hero bike"),"Parked convoy must disappear while gathering.");
                var occupied=map.GetComponentsInChildren<Button>(true).Single(button=>button.name.EndsWith(" • "+gatherSite.Id,StringComparison.Ordinal));
                Require(occupied.GetComponent<Image>().color==new Color32(38,140,73,145) && occupied.GetComponentInChildren<RawImage>(true).color!=Color.white,"Gathering site must change colour.");
                manager.WorldMap.Recall(hero.InstanceId);map.Refresh();
                Require(map.GetComponentsInChildren<RawImage>(true).Count(image=>image.name=="Army bus" || image.name=="Hero bike")==2,"Returning convoy must reappear.");
                Require(occupied.GetComponentInChildren<RawImage>(true).color==Color.white,"Recalled gathering site must recover its normal colour.");
                store.Save(progress.Capture());string missionJson=codec.Encode(progress.Capture());manager.WorldMap.Advance(100);
                progress.Restore(store.Load().State);progress.NotifyRestored();Require(codec.Encode(progress.Capture())==missionJson,"Native mission disk round trip changed convoy state.");
                Debug.Log("PASS Unity world map: sixteen resource landmarks, 256 grid camp buttons/scouting, close zoom sizing, bus/bike travel, gathering site colours and hidden parked convoys, native JsonUtility mission disk round trip, civilian gathering and legacy migration. Temporary saves only; test pinch/drag/convoy motion and raid dust/results on phone.");
            }
            catch(Exception error) { Debug.LogException(error); if(Application.isBatchMode)EditorApplication.Exit(1); throw; }
            finally {
                if(scene.IsValid() && scene.isLoaded)EditorSceneManager.ClosePreviewScene(scene);
                if(Directory.Exists(directory))Directory.Delete(directory,true);
            }
        }
        private static void Require(bool condition,string message) { if(!condition)throw new InvalidOperationException(message); }
    }
}
