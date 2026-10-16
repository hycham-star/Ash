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
        [MenuItem("Ashlands/Validate Army Reinforcement Controls")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Validate outside Play mode.");
            Scene scene=default;GameObject presentation=null;SettlementView view=null;Action refresh=null;GameManager manager=null;
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            object Field(string name)=>typeof(SettlementView).GetField(name,flags).GetValue(view);
            void Set(string name,object value)=>typeof(SettlementView).GetField(name,flags).SetValue(view,value);
            void Call(string name)=>typeof(SettlementView).GetMethod(name,flags).Invoke(view,null);
            try
            {
                scene=EditorSceneManager.OpenPreviewScene(PrepareProject.SettlementScene);
                manager=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameManager>(true)).Single();manager.Initialize();
                Require(!manager.PersistenceActive,"Validation must not open player saves.");
                view=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SettlementView>(true)).Single();
                presentation=new GameObject("Disposable reinforcement controls");SceneManager.MoveGameObjectToScene(presentation,scene);
                Set("presentation",presentation);Set("font",Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));Call("BuildUI");
                int index=manager.Heroes.Select((hero,i)=>new{hero,i}).Single(x=>x.hero.Definition.Id=="mara-flint").i;Set("selectedHero",index);
                var hero=manager.Heroes[index];while(hero.Army.Capacity<50)manager.HeroArmies.GrantHeroXp(hero.InstanceId,hero.XpToNextLevel);
                Require(hero.Army.Capacity==50,"Expected Mara's level-five capacity.");
                var unit=manager.Units[0];manager.HeroArmies.AssignFighters(hero.InstanceId,unit.Definition.Id,48);
                refresh=()=>Call("Refresh");manager.HeroArmies.Changed+=refresh;Call("Refresh");
                var slider=(Slider)Field("fighterQuantity");var add=(Text)Field("addFightersLabel");var fill=(Text)Field("fillFightersLabel");
                Require(slider.wholeNumbers && slider.maxValue==2 && slider.interactable,"Slider must allow exactly two replacements at 48/50.");
                slider.value=1;Require(add.text=="ADD 1","Quantity number did not follow slider.");
                add.transform.parent.GetComponent<Button>().onClick.Invoke();
                Require(hero.Army.TotalUnits==49 && manager.HeroArmies.Available(unit.Definition.Id)==51 && slider.maxValue==1,"Exact one-fighter reinforcement failed.");
                fill.transform.parent.GetComponent<Button>().onClick.Invoke();
                Require(hero.Army.TotalUnits==50 && manager.HeroArmies.Available(unit.Definition.Id)==50 && !slider.interactable && !fill.transform.parent.GetComponent<Button>().interactable,"Fill must complete army without overspending reserve.");
                manager.HeroArmies.RemoveFighters(hero.InstanceId,unit.Definition.Id,2);
                Require(fill.text=="FILL +2","Fill must display the exact two-fighter shortfall.");fill.transform.parent.GetComponent<Button>().onClick.Invoke();
                Require(hero.Army.TotalUnits==50 && manager.HeroArmies.Available(unit.Definition.Id)==50,"One-tap fill from 48 to 50 failed.");
                // Open space larger than the remaining reserve; Fill must stop at reserve availability.
                manager.HeroArmies.RemoveFighters(hero.InstanceId,unit.Definition.Id,50);
                var other=manager.Heroes.First(h=>h!=hero);manager.HeroArmies.AssignFighters(other.InstanceId,unit.Definition.Id,other.Army.Capacity);
                int available=manager.HeroArmies.Available(unit.Definition.Id);
                // Drain to a small remainder using other independent armies.
                foreach(var h in manager.Heroes.Where(h=>h!=hero && h!=other))
                { int amount=Math.Min(h.Army.Capacity,manager.HeroArmies.Available(unit.Definition.Id)-3);if(amount>0)manager.HeroArmies.AssignFighters(h.InstanceId,unit.Definition.Id,amount); }
                available=manager.HeroArmies.Available(unit.Definition.Id);Call("Refresh");
                Require(slider.maxValue==Math.Min(50,available),"Reserve must limit slider.");
                fill.transform.parent.GetComponent<Button>().onClick.Invoke();
                Require(hero.Army.TotalUnits==Math.Min(50,available),"Fill ignored available reserve.");
                Debug.Log("PASS Unity army reinforcement controls: live slider numbers, 48/50 replacement in single fighters, Fill, full-army disabling and shared reserve limits. Disposable preview only; player saves untouched. Check portrait layout/touch on device.");
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
