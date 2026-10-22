using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ash.Editor
{
    public static class ArmyReinforcementValidation
    {
        [MenuItem("Ashlands/Validate Production and Army Travel")]
        public static void ValidateProductionAndTravel(){Run();DevelopmentValidation.Run();ScavengingValidation.Run();RaidPreparationValidation.Run();}
        [MenuItem("Ashlands/Validate Hero Illustrations")]
        public static void ValidateIllustrations()=>Run();
        [MenuItem("Ashlands/Validate Army Reinforcement Controls")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Validate outside Play mode.");
            Scene scene=default;GameObject presentation=null;SettlementView view=null;Action refresh=null;GameManager manager=null;
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            object Field(string name)=>typeof(SettlementView).GetField(name,flags).GetValue(view);
            void Set(string name,object value)=>typeof(SettlementView).GetField(name,flags).SetValue(view,value);
            void Call(string name,params object[] args)=>typeof(SettlementView).GetMethod(name,flags).Invoke(view,args);
            try
            {
                scene=EditorSceneManager.OpenPreviewScene(PrepareProject.SettlementScene);
                manager=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameManager>(true)).Single();manager.Initialize();
                Require(!manager.PersistenceActive,"Validation must not open player saves.");
                view=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SettlementView>(true)).Single();
                presentation=new GameObject("Disposable reinforcement controls");SceneManager.MoveGameObjectToScene(presentation,scene);
                Set("presentation",presentation);Set("font",Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));Call("BuildUI");
                var illustration=(RawImage)Field("heroIllustration");
                for(int i=0;i<manager.Heroes.Count;i++)
                {
                    Set("selectedHero",i);Call("Refresh");
                    Require(illustration.texture is Texture2D atlas && atlas.width==HeroPortraitCatalog.AtlasWidth && atlas.height==HeroPortraitCatalog.AtlasHeight,"Hero preview must use the 2D illustration atlas, not a render texture.");
                    Require(illustration.uvRect==HeroPortraitCatalog.RegionFor(manager.Heroes[i].Definition.Id),"Illustration does not match selected hero identity.");
                }
                int index=manager.Heroes.Select((hero,i)=>new{hero,i}).Single(x=>x.hero.Definition.Id=="mara-flint").i;Set("selectedHero",index);
                var hero=manager.Heroes[index];while(hero.Army.Capacity<50)manager.HeroArmies.GrantHeroXp(hero.InstanceId,hero.XpToNextLevel);
                Require(hero.Army.Capacity==50,"Expected Mara's level-five capacity.");
                var unit=manager.Units[0];manager.HeroArmies.AssignFighters(hero.InstanceId,unit.Definition.Id,48);
                refresh=()=>Call("Refresh");manager.HeroArmies.Changed+=refresh;Call("Refresh");
                var sliders=(System.Collections.Generic.List<Slider>)Field("fighterSliders");
                var minus=(System.Collections.Generic.List<Button>)Field("fighterMinus");
                var plus=(System.Collections.Generic.List<Button>)Field("fighterPlus");
                Require(sliders.Count==3 && sliders.All(control=>control.wholeNumbers),"Each type must have its own integer slider.");
                int fighterIndex=manager.Units.ToList().IndexOf(unit);var slider=sliders[fighterIndex];
                Require(slider.value==48 && slider.maxValue==50 && slider.interactable,"Assigned slider must allow 48 to 50.");
                plus[fighterIndex].onClick.Invoke();Require(hero.Army.TotalUnits==49,"+1 did not immediately assign one fighter.");
                slider.value=50;Require(hero.Army.TotalUnits==50 && !plus[fighterIndex].interactable && minus[fighterIndex].interactable,"Full armies must still allow removal.");
                minus[fighterIndex].onClick.Invoke();Require(hero.Army.TotalUnits==49 && manager.HeroArmies.Available(unit.Definition.Id)==51,"−1 must return one fighter to reserve.");
                slider.value=0;Require(hero.Army.TotalUnits==0 && manager.HeroArmies.Available(unit.Definition.Id)==100,"Zero must deselect this fighter type without losing reserve.");
                sliders[0].value=20;sliders[1].value=20;sliders[2].value=10;
                Require(hero.Army.TotalUnits==50 && sliders.Select(control=>(int)control.value).SequenceEqual(new[]{20,20,10}),"Three sliders must share the total capacity.");
                Require(sliders.Select((control,i)=>control.maxValue==control.value).All(value=>value),"Other rows must not exceed shared capacity.");
                Set("selectedHero",(index+1)%manager.Heroes.Count);Call("Refresh");
                Require(manager.Heroes[(index+1)%manager.Heroes.Count].Army.TotalUnits==0 && hero.Army.TotalUnits==50,"Changing hero must not trigger slider assignment callbacks.");
                Set("selectedHero",index);Call("Refresh");
                manager.HeroArmies.GrantHeroXp(hero.InstanceId,hero.XpToNextLevel);
                Require(sliders.All(control=>control.maxValue==control.value+hero.Definition.CapacityPerLevel),"Level-up must increase the shared available capacity.");
                Require(hero.Army.TotalUnits==50,"Level-up must not create fighters.");
                // Test the exact production entry point used by the foreground GameManager tick.
                int food=manager.Resources.Get(ResourceType.Food),water=manager.Resources.Get(ResourceType.Water);
                typeof(GameManager).GetMethod("AdvanceSettlementProduction",flags).Invoke(manager,new object[]{60});
                Require(manager.Buildings.Stored(BuildingKind.FoodGarden)==0 && manager.Resources.Get(ResourceType.Food)==food+8 && manager.Resources.Get(ResourceType.Water)==water+5,"Foreground production must credit resources automatically, with resident food/water consumption.");
                Call("ShowBuilding",BuildingKind.WaterStation);
                Require(!((Text)Field("viewHeadquarters")).transform.parent.gameObject.activeSelf,"Producer panel must hide collection button.");
                Debug.Log("PASS Unity army composition: three live sliders, exact −1/+1, deselection/refund, shared level-based capacity, stable hero switching, automatic foreground settlement income and no producer collect button. Preview only; player saves untouched.");
            }
            catch(Exception error){Debug.LogException(error);if(Application.isBatchMode)EditorApplication.Exit(1);throw;}
            finally
            {
                if(manager!=null && refresh!=null)manager.HeroArmies.Changed-=refresh;
                if(view!=null)Set("presentation",null);
                if(presentation!=null)UnityEngine.Object.DestroyImmediate(presentation);
                if(scene.IsValid() && scene.isLoaded)EditorSceneManager.ClosePreviewScene(scene);
            }
        }
        private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    }
}
