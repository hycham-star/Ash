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
    public static class DevelopmentValidation
    {
        [Serializable] private sealed class LegacyV6
        {
            public int Version;public string Format;public HeroArmyState Armies;public ResourceAmount[] Resources;public FragmentState[] Fragments;public BuildingState[] Buildings;public HospitalState Hospital;public ScavengingState Scavenging;public long ScavengingUtcSeconds;
        }
        [MenuItem("Ashlands/Validate Settlement Development")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Validate outside Play mode.");
            Scene scene=default;string directory=Path.Combine(Path.GetTempPath(),"Ashlands-development-"+Guid.NewGuid().ToString("N"));
            try
            {
                scene=EditorSceneManager.OpenPreviewScene(PrepareProject.SettlementScene);
                var manager=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameManager>(true)).Single();manager.Initialize();Require(!manager.PersistenceActive,"Validator must not open player saves.");
                manager.Resources.Add(ResourceType.Scrap,1000);manager.Resources.Add(ResourceType.Lumber,1000);
                var service=manager.Buildings;var canvas=new GameObject("Disposable development canvas",typeof(RectTransform),typeof(Canvas));SceneManager.MoveGameObjectToScene(canvas,scene);canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
                var ui=DevelopmentUI.Create(canvas.transform,manager,Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));ui.Show();
                Button FindButton(string prefix)=>ui.GetComponentsInChildren<Button>(true).Single(b=>b.GetComponentInChildren<Text>().text.StartsWith(prefix,StringComparison.Ordinal));
                FindButton("BUILD ").onClick.Invoke();
                Require(service.Plot(SettlementLayout.Plots[0].Id).Kind==PlotKind.Farm && service.EmployedResidents==3,"Actual build button did not construct/staff farm.");
                service.AdvanceDevelopment(60);Require(service.Population==21 && service.Plot(SettlementLayout.Plots[0].Id).Stored==10 && manager.Resources.Get(ResourceType.Food)==98,"Population growth or farm production incorrect.");
                ui.Show(SettlementLayout.Plots[1].Id);FindButton("HOUSING").onClick.Invoke();FindButton("BUILD ").onClick.Invoke();
                Require(service.HousingCapacity==40,"Actual UI did not build housing.");
                service.Upgrade(BuildingKind.Headquarters);FindButton("UPGRADE").onClick.Invoke();Require(service.Plot(SettlementLayout.Plots[1].Id).Level==2 && service.HousingCapacity==50,"Plot upgrade did not preserve/grow housing.");
                ui.Show(SettlementLayout.Plots[2].Id);FindButton("WELL").onClick.Invoke();FindButton("BUILD ").onClick.Invoke();
                ui.Show(SettlementLayout.Plots[3].Id);FindButton("SCRAPYARD").onClick.Invoke();FindButton("BUILD ").onClick.Invoke();
                Require(service.EmployedResidents==8 && service.FreeResidents==13,"Well/scrapyard resident staffing incorrect.");
                manager.Resources.Add(ResourceType.Food,200);manager.Resources.Add(ResourceType.Water,200);
                var types=new[]{PlotKind.InfantryCenter,PlotKind.BikerCenter,PlotKind.ArmyCenter};var labels=new[]{"INFANTRY","BIKER","ARMY"};
                for(int i=0;i<types.Length;i++)
                {
                    string id=SettlementLayout.Plots[4+i].Id;ui.Show(id);FindButton(labels[i]).onClick.Invoke();FindButton("BUILD ").onClick.Invoke();
                    Require(service.Plot(id).Kind==types[i],"Training center construction failed.");
                    var fighter=manager.Training.Fighter(types[i]);int before=manager.HeroArmies.Available(fighter.Definition.Id);
                    ui.GetComponentsInChildren<Slider>(true).Single().value=2;FindButton("TRAIN ").onClick.Invoke();
                    Require(manager.HeroArmies.Available(fighter.Definition.Id)==before+2,"Training button must add chosen fighter quantity to reserve.");
                }
                Require(service.HousingCapacity==50 && service.EmployedResidents==14,"Training centers must employ residents without adding housing.");
                var progress=new ProgressPersistence(manager.HeroArmies,manager.Resources,manager.Fragments,service,manager.Scavenging,manager.OfflineScavenging);var codec=new UnityProgressCodec();var store=new LocalSaveStore(directory,codec,progress.Validate);var saved=progress.Capture();string exact=codec.Encode(saved);store.Save(saved);
                ui.Show(SettlementLayout.Plots[0].Id);FindButton("COLLECT ").onClick.Invoke();Require(service.Plot(SettlementLayout.Plots[0].Id).Stored==0,"Actual collect button failed.");
                progress.Restore(store.Load().State);progress.NotifyRestored();Require(codec.Encode(progress.Capture())==exact,"Native development disk round trip changed progression.");
                var old=new LegacyV6{Version=6,Format=saved.Format,Armies=saved.Armies,Resources=saved.Resources.Where(r=>r.Type!=ResourceType.Gold).ToArray(),Fragments=saved.Fragments,Buildings=saved.Buildings,Hospital=saved.Hospital,Scavenging=new ScavengingState{Seed=saved.Scavenging.Seed,Sites=saved.Scavenging.Sites.Take(ScavengingService.LegacySiteCount).ToArray()},ScavengingUtcSeconds=saved.ScavengingUtcSeconds};
                progress.Restore(codec.Decode(JsonUtility.ToJson(old)));Require(service.Population==20 && service.CaptureDevelopment().Plots.All(p=>p.Kind==PlotKind.Empty) && service.Level(BuildingKind.Headquarters)==2,"Native version-six migration lost core progression or failed plot initialization.");
                Debug.Log("PASS Unity settlement development: actual construction/upgrade/collect buttons, resident growth and jobs, housing, farm/well/scrapyard, native JSON/disk and version-six migration. Unique temporary saves only. Run Validate Settlement Presentation and check expanded plots/portrait touch on device.");
            }
            catch(Exception error){Debug.LogException(error);if(Application.isBatchMode)EditorApplication.Exit(1);throw;}
            finally{if(scene.IsValid() && scene.isLoaded)EditorSceneManager.ClosePreviewScene(scene);if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
        private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    }
}
