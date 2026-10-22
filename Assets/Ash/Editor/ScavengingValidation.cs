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
        [MenuItem("Ashlands/Validate Wasteland World Presentation")]
        public static void ValidateWorldPresentation(){Run();RaidPreparationValidation.Run();}
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
            Scene scene=default;Texture2D rawAtlas=null;
            string directory=Path.Combine(Path.GetTempPath(),"Ashlands-map-validation-"+Guid.NewGuid().ToString("N"));
            try {
                var pixels=Resources.Load<TextAsset>("Map/IsometricOverworldAtlasPixels");
                Require(pixels!=null,"Missing bundled raw atlas backup.");rawAtlas=MapLandmarkArt.CreateAtlasFromPixels(pixels.bytes);
                Require(rawAtlas.width==MapLandmarkArt.AtlasWidth && rawAtlas.height==MapLandmarkArt.AtlasHeight && rawAtlas.filterMode==FilterMode.Bilinear,"Raw atlas backup failed native texture decoding.");
                scene=EditorSceneManager.OpenPreviewScene(PrepareProject.SettlementScene);
                var manager=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameManager>(true)).Single();
                manager.Initialize(); Require(!manager.PersistenceActive,"Validation must not touch the real save slot.");
                var service=manager.Scavenging;
                var nodes=service.Capture().Sites;
                Require(nodes.Length==ScavengingService.SiteCount && nodes.Select(n=>n.Resource).Distinct().Count()==4,"Map must have all four resources.");
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
                float subsecond=0;int chosen=0;map.ConfigureWorld(manager.WorldMap,()=>manager.Heroes[chosen],delta=>chosen=(chosen+delta+manager.Heroes.Count)%manager.Heroes.Count,_=>{},subsecond:()=>subsecond);
                map.Refresh(); // Actual refresh of the inactive panel, including real button references.
                Require(!map.gameObject.activeSelf && map.GetComponentsInChildren<Button>(true).Length>=24,"Map UI controls incomplete.");
                map.Show();
                var siteActions=map.GetComponentsInChildren<RectTransform>(true).Single(rect=>rect.name=="Resource site actions");
                Require(!siteActions.gameObject.activeSelf && !map.GetComponentsInChildren<Button>(true).Any(button=>button.name=="FIT" || button.name=="LOCATE" || button.name=="CONTROLS"),"World must not show permanent map action buttons.");
                var landmarks=map.GetComponentsInChildren<RawImage>(true).Where(image=>image.name=="Miniature landmark").ToArray();
                Require(landmarks.Length==ScavengingService.SiteCount && landmarks.All(image=>image.texture!=null && image.texture.width==MapLandmarkArt.AtlasWidth && !image.raycastTarget),"Miniature landmarks are missing or blocking site buttons.");
                Require(map.GetComponentsInChildren<RawImage>(true).Count(image=>image.name=="Rusty enemy base" && image.texture!=null)==WorldRegions.CampCount,"Clickable world camps missing.");
                var terrain=map.GetComponentsInChildren<RawImage>(true).Single(image=>image.name=="Textured wasteland");
                var groundPixels=Resources.Load<TextAsset>("Map/WastelandTerrainPixels");
                Require(groundPixels!=null && groundPixels.bytes.Length==1024*1024*4,"Painted wasteland ground backup is missing.");
                var overview=map.GetComponentsInChildren<RawImage>(true).Single(image=>image.name=="Live region overview");
                Require(overview.texture==terrain.texture && !overview.raycastTarget,"Overview must share actual terrain without intercepting map gestures.");
                Require(map.GetComponentsInChildren<WastelandMetalFrame>(true).Length>10 && map.GetComponentsInChildren<WastelandMetalFrame>(true).All(frame=>!frame.raycastTarget),"World interface skin is missing or blocks input.");
                Require(landmarks.All(image=>image.rectTransform.parent.GetComponentInChildren<WastelandMetalFrame>(true)==null),"Resource hit areas must not receive opaque interface frames.");
                Require(CampIconsSafe(map),"Camp hit areas must stay transparent and clickable.");
                Require(terrain.texture.width==MapLandmarkArt.TerrainSize && landmarks.Select(image=>image.texture).Distinct().Count()==1,"Overworld must use tiled biome terrain and one shared landmark atlas.");
                var campIcons=map.GetComponentsInChildren<RawImage>(true).Where(image=>image.name=="Rusty enemy base").ToArray();
                Require(map.MapSize>=32+WorldRegions.TileCount*8f,"World must open at a local scale rather than shrink the whole world to the screen.");
                Require(campIcons.Any(image=>image.rectTransform.parent.gameObject.activeSelf),"No camps visible at world-map opening zoom.");
                map.ZoomBy(100);Require(map.MapSize==24000 && landmarks.All(image=>image.rectTransform.parent.GetComponent<RectTransform>().rect.width>150),"Close zoom does not enlarge resource landmarks.");
                var viewfinder=map.GetComponentsInChildren<RectTransform>(true).Single(rect=>rect.name=="Visible map bounds");
                Require(viewfinder.anchorMax.x>viewfinder.anchorMin.x && viewfinder.anchorMax.x-viewfinder.anchorMin.x<.5f,"Overview viewfinder must contract with zoom.");
                Require(campIcons.Count(image=>image.rectTransform.parent.gameObject.activeSelf)<WorldRegions.CampCount,"Offscreen camp markers were not culled at close zoom.");
                // Move the actual map transform into the new outer region; no selected-home shortcut.
                var outerMarker=campIcons.Single(image=>image.transform.parent.name=="Enemy base • camp-512").rectTransform.parent.GetComponent<RectTransform>();
                var mapContent=(RectTransform)typeof(ScavengingMapUI).GetField("map",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(map);
                mapContent.anchoredPosition=-outerMarker.anchoredPosition;map.Refresh();
                Require(outerMarker.gameObject.activeSelf,"Panning into the outer region hid its enemy camp.");
                map.ZoomBy(.001f);Require(map.MapSize>=200 && map.MapSize<24000,"Map cannot zoom out again.");
                Require(campIcons.Any(image=>image.rectTransform.parent.gameObject.activeSelf && int.Parse(image.transform.parent.name.Substring(image.transform.parent.name.LastIndexOf('-')+1))>WorldRegions.OriginalCampCount),"Outer-region camps must remain visible at far zoom.");
                string campId=manager.WorldMap.Camps[0].Id;
                map.GetComponentsInChildren<Button>(true).Single(button=>button.name=="Enemy base • "+campId).onClick.Invoke();
                map.GetComponentsInChildren<Button>(true).Single(button=>button.name=="SCOUT").onClick.Invoke();
                Require(manager.WorldMap.IsScouted(campId),"Camp scout button is not connected to world reports.");
                var siteButton=map.GetComponentsInChildren<Button>(true).Single(button=>button.name.EndsWith(" • "+lumber.Id,StringComparison.Ordinal));
                siteButton.onClick.Invoke();
                Require(siteActions.gameObject.activeSelf,"Resource selection must open context actions.");
                map.DismissSelection();Require(!siteActions.gameObject.activeSelf,"Blank terrain must close context actions.");siteButton.onClick.Invoke();
                Require(map.SelectedSiteId==lumber.Id && service.Capture().Sites.All(site=>nodes.Any(before=>before.Id==site.Id && before.X==site.X && before.Z==site.Z && site.Workers==0)),"Landmark selection changed saved positions or worker allocation.");
                Require(service.Assign(lumber.Id)==ScavengingResult.Success && service.FreeWorkers==9,"Worker allocation failed.");
                service.Advance(37);
                Require(service.Site(lumber.Id).Stored==4 && service.Site(lumber.Id).Work==56,"Lumber production incorrect.");
                var snapshot=progress.Capture(); store.Save(snapshot); string exact=codec.Encode(snapshot);
                service.Recall(lumber.Id);
                progress.Restore(store.Load().State); progress.NotifyRestored();
                Require(codec.Encode(progress.Capture())==exact,"Native map disk round trip changed coordinates, workers or stock.");
                var legacyWorld=codec.Decode(exact);legacyWorld.Version=11;
                legacyWorld.Scavenging.Sites=legacyWorld.Scavenging.Sites.Take(ScavengingService.LegacySiteCount).ToArray();
                store.Save(legacyWorld);progress.Restore(store.Load().State);progress.NotifyRestored();
                Require(codec.Encode(progress.Capture())==exact,"Native version-eleven disk migration moved sites or lost assigned workers/work/cargo.");
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
                string oldJson=JsonUtility.ToJson(new LegacyV2 { Version=2,Format=old.Format,Armies=old.Armies,Resources=old.Resources.Where(r=>r.Type!=ResourceType.Lumber && r.Type!=ResourceType.Gold).ToArray(),Fragments=old.Fragments,Buildings=old.Buildings });
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
                manager.WorldMap.Advance(100);
                manager.WorldMap.Dispatch(hero.InstanceId,manager.WorldMap.Camps[0].Id,MapMissionKind.Raid);map.Refresh();
                var convoy=map.GetComponentsInChildren<RectTransform>(true).Single(rect=>rect.name=="Army convoy • "+hero.InstanceId);
                var route=map.GetComponentsInChildren<MapRaidRoute>(true).Single();
                Require(!route.raycastTarget && route.rectTransform.rect.width>0,"Raid route must have visible clipped geometry and never intercept input.");
                var start=convoy.anchoredPosition;subsecond=.25f;map.Refresh();var quarter=convoy.anchoredPosition;
                subsecond=.5f;map.Refresh();var half=convoy.anchoredPosition;
                Require((quarter-start).magnitude>0 && Vector2.Distance(quarter-start,half-quarter)<.001f,"Convoy must move uniformly between integer ticks.");
                subsecond=.999f;map.Refresh();var beforeTick=convoy.anchoredPosition;
                subsecond=0;manager.WorldMap.Advance(1);map.Refresh();
                Require(Vector2.Distance(beforeTick,convoy.anchoredPosition)<.1f,"Convoy must cross simulation ticks without easing pauses or jumps.");
                manager.WorldMap.Advance(manager.WorldMap.TravelSeconds(manager.WorldMap.Mission(hero.InstanceId))-1);map.Refresh();
                Require(manager.WorldMap.Mission(hero.InstanceId).Phase==MapMissionPhase.Dust && !map.GetComponentsInChildren<MapRaidRoute>(true).Any(),"Route must disappear when raid reaches its camp.");
                Debug.Log("PASS Unity world map: 48 resource landmarks, 512 grid camp buttons/scouting, close zoom sizing, continuous bus/bike travel, outbound raid arrows, gathering site colours and hidden parked convoys, native JsonUtility mission disk round trip, civilian gathering and legacy migration. Temporary saves only; test pinch/drag/convoy motion and raid dust/results on phone.");
            }
            catch(Exception error) { Debug.LogException(error); if(Application.isBatchMode)EditorApplication.Exit(1); throw; }
            finally {
                if(rawAtlas!=null)UnityEngine.Object.DestroyImmediate(rawAtlas);
                if(scene.IsValid() && scene.isLoaded)EditorSceneManager.ClosePreviewScene(scene);
                if(Directory.Exists(directory))Directory.Delete(directory,true);
            }
        }
        private static bool CampIconsSafe(ScavengingMapUI map)
        {
            return map.GetComponentsInChildren<RawImage>(true).Where(image=>image.name=="Rusty enemy base").All(image=>image.rectTransform.parent.GetComponentInChildren<WastelandMetalFrame>(true)==null && image.rectTransform.parent.GetComponent<Button>()!=null);
        }
        private static void Require(bool condition,string message) { if(!condition)throw new InvalidOperationException(message); }
    }
}
