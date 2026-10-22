using System;
using System.Linq;
using Ash;

internal static class AutomaticProductionChecks
{
    public static int Run(HeroDefinition[] definitions,UnitDefinition[] units)
    {
        int passed=0;
        void Check(bool value,string label){if(!value)throw new Exception("FAIL automatic production/composition "+label);passed++;Console.WriteLine("PASS automatic production/composition "+label);}
        var wallet=new ResourceManager();var buildings=new BuildingService(wallet);
        buildings.Advance(59,true);Check(wallet.Get(ResourceType.Food)==0 && buildings.Seconds(BuildingKind.FoodGarden)==59,"partial minutes do not pay early");
        buildings.Advance(1,true);Check(wallet.Get(ResourceType.Food)==10 && wallet.Get(ResourceType.Water)==6 && buildings.Stored(BuildingKind.FoodGarden)==0,"core producers automatically deposit exact batches");
        buildings.Advance(600,true);Check(wallet.Get(ResourceType.Food)==110 && wallet.Get(ResourceType.Water)==66,"automatic output is not capped at five minutes of storage");
        buildings.Advance(60);Check(buildings.Stored(BuildingKind.FoodGarden)==10,"legacy buffered fixture prepared");
        buildings.Advance(1,true);Check(buildings.Stored(BuildingKind.FoodGarden)==0 && wallet.Get(ResourceType.Food)==120,"legacy stored production transfers on next active tick");
        var full=new ResourceManager(new[]{new ResourceAmount(ResourceType.Food,int.MaxValue-3)});var buffered=new BuildingService(full);
        buffered.Advance(60,true);Check(full.Get(ResourceType.Food)==int.MaxValue && buffered.Stored(BuildingKind.FoodGarden)==7,"full resource ledger safely buffers undeliverable output");
        buffered.Advance(300,true);buffered.Validate(buffered.Capture());Check(buffered.Stored(BuildingKind.FoodGarden)==50 && buffered.Seconds(BuildingKind.FoodGarden)==0,"full buffer pauses and remains valid for saving");
        full.TrySpend(new[]{new ResourceAmount(ResourceType.Food,55)});buffered.Advance(1,true);
        Check(buffered.Stored(BuildingKind.FoodGarden)==0 && full.Get(ResourceType.Food)==int.MaxValue-5 && buffered.Seconds(BuildingKind.FoodGarden)==1,"spending resumes automatic delivery exactly once");
        buffered.Advance(1,true);Check(full.Get(ResourceType.Food)==int.MaxValue-5,"retry never duplicates delivered stock");
        var restored=new BuildingService(full);restored.RestoreValidated(buffered.Capture());restored.Advance(58,true);
        Check(full.Get(ResourceType.Food)==int.MaxValue && restored.Stored(BuildingKind.FoodGarden)==5,"saved fractional timer resumes without losing output or overflow");
        var funds=new ResourceManager(new[]{new ResourceAmount(ResourceType.Food,10000),new ResourceAmount(ResourceType.Water,10000),new ResourceAmount(ResourceType.Scrap,10000),new ResourceAmount(ResourceType.Lumber,10000)});
        var town=new BuildingService(funds);var plots=SettlementLayout.Plots;
        town.BuildPlot(plots[0].Id,PlotKind.Farm);town.BuildPlot(plots[1].Id,PlotKind.WaterWell);town.BuildPlot(plots[2].Id,PlotKind.Scrapyard);
        int food=funds.Get(ResourceType.Food),water=funds.Get(ResourceType.Water),scrap=funds.Get(ResourceType.Scrap);
        town.AdvanceDevelopment(600,true);
        Check(funds.Get(ResourceType.Food)==food+100-20 && funds.Get(ResourceType.Water)==water+80-10 && funds.Get(ResourceType.Scrap)==scrap+60,"constructed producers automatically pay while resident growth still consumes supplies");
        Check(town.CaptureDevelopment().Plots.All(p=>p.Stored==0) && town.Population==30,"no collection needed and population cap respected");
        bool complete=false;town.Changed+=()=>{complete=funds.Get(ResourceType.Scrap)==scrap+66 && town.Plot(plots[2].Id).Stored==0;throw new Exception("broken observer");};
        town.AdvanceDevelopment(60,true);Check(complete && town.NotificationErrors.Length==1,"observers see complete stock and ledger even if one UI throws");
        var heroes=new[]{new HeroData("composition-a",definitions.Single(d=>d.Id=="mara-flint")),new HeroData("composition-b",definitions[1])};
        var fighters=units.Select(u=>new UnitData(u)).ToArray();var army=new HeroArmyService(heroes,fighters,100);var hero=heroes[0];
        string a=units[0].Id,b=units[1].Id,c=units[2].Id;int cap=hero.Army.Capacity;
        Check(army.SetFighterQuantity(hero.InstanceId,a,cap)==ArmyCommandResult.Success && hero.Army.TotalUnits==cap,"absolute slider quantity can fill the level-one capacity");
        Check(army.SetFighterQuantity(hero.InstanceId,b,1)==ArmyCommandResult.CapacityExceeded && army.Available(b)==100,"other sliders share capacity and cannot spend reserve on rejection");
        army.SetFighterQuantity(hero.InstanceId,a,cap-2);Check(army.Available(a)==100-cap+2,"reducing slider returns exact reserve");
        army.SetFighterQuantity(hero.InstanceId,b,2);Check(hero.Army.TotalUnits==cap,"freed capacity can be assigned to another type");
        army.GrantHeroXp(hero.InstanceId,hero.XpToNextLevel);Check(hero.Army.Capacity==cap+hero.Definition.CapacityPerLevel && hero.Army.TotalUnits==cap,"hero level raises capacity without spawning fighters");
        army.SetFighterQuantity(hero.InstanceId,c,hero.Definition.CapacityPerLevel);Check(hero.Army.TotalUnits==hero.Army.Capacity,"new level capacity is assignable");
        army.SetFighterQuantity(hero.InstanceId,b,0);Check(hero.Army.QuantityOf(b)==0 && army.Available(b)==100,"zero deselects and refunds all fighters of one type");
        Check(heroes[1].Army.TotalUnits==0,"another hero's independent army is unchanged");
        int events=0;army.Changed+=()=>events++;int current=hero.Army.QuantityOf(a);
        Check(army.SetFighterQuantity(hero.InstanceId,a,current)==ArmyCommandResult.Success && events==0,"no-op does not trigger save or duplicate assignment");
        Check(army.SetFighterQuantity(hero.InstanceId,a,-1)==ArmyCommandResult.InvalidQuantity && hero.Army.QuantityOf(a)==current,"negative selection rejected without mutation");
        army.DeploymentBlocked=id=>id==hero.InstanceId;
        Check(army.SetFighterQuantity(hero.InstanceId,a,0)==ArmyCommandResult.ArmyDeployed && hero.Army.QuantityOf(a)==current,"deployed army cannot be deselected");
        return passed;
    }
}
