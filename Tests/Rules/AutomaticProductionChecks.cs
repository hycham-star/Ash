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
        buildings.Advance(35,true);Check(wallet.Get(ResourceType.Food)==0 && buildings.Capture().Single(x=>x.Kind==BuildingKind.FoodGarden).ProductionWork==3500,"hourly production retains fractions without paying early");
        buildings.Advance(1,true);Check(wallet.Get(ResourceType.Food)==1 && wallet.Get(ResourceType.Water)==1 && buildings.Stored(BuildingKind.FoodGarden)==0,"all level-one core producers deliver one resource every 36 seconds");
        buildings.Advance(3564,true);Check(wallet.Get(ResourceType.Food)==100 && wallet.Get(ResourceType.Water)==100,"one hour yields exactly 100 from each core producer");
        var full=new ResourceManager(new[]{new ResourceAmount(ResourceType.Food,int.MaxValue-3)});var buffered=new BuildingService(full);
        buffered.Advance(360,true);Check(full.Get(ResourceType.Food)==int.MaxValue && buffered.Stored(BuildingKind.FoodGarden)==7,"full wallet safely buffers hourly output");
        buffered.Advance(3600,true);buffered.Validate(buffered.Capture());Check(buffered.Stored(BuildingKind.FoodGarden)==50,"full buffer pauses without overflow");
        full.TrySpend(new[]{new ResourceAmount(ResourceType.Food,55)});buffered.Advance(1,true);
        Check(buffered.Stored(BuildingKind.FoodGarden)==0 && full.Get(ResourceType.Food)==int.MaxValue-5,"spending resumes automatic delivery exactly once");
        buffered.Advance(1,true);Check(full.Get(ResourceType.Food)==int.MaxValue-5,"retry never duplicates buffered income");
        var restored=new BuildingService(full);restored.RestoreValidated(buffered.Capture());restored.Advance(34,true);
        Check(full.Get(ResourceType.Food)==int.MaxValue-4,"fractional hourly timer survives capture and restore");
        var legacy=new BuildingService(new ResourceManager());legacy.Advance(90);var legacyMoney=new ResourceManager();var migrated=new BuildingService(legacyMoney);migrated.RestoreValidated(legacy.Capture());migrated.Advance(1,true);
        Check(legacyMoney.Get(ResourceType.Food)==10 && migrated.Capture().Single(x=>x.Kind==BuildingKind.FoodGarden).ProductionWork==1900,"old stored income and half-cycle timer migrate once");
        var funds=new ResourceManager(new[]{new ResourceAmount(ResourceType.Food,10000),new ResourceAmount(ResourceType.Water,10000),new ResourceAmount(ResourceType.Scrap,10000),new ResourceAmount(ResourceType.Lumber,10000)});
        var town=new BuildingService(funds);var plots=SettlementLayout.Plots;
        town.BuildPlot(plots[0].Id,PlotKind.Farm);town.BuildPlot(plots[1].Id,PlotKind.WaterWell);town.BuildPlot(plots[2].Id,PlotKind.Scrapyard);town.BuildPlot(plots[3].Id,PlotKind.LumberMill);
        int food=funds.Get(ResourceType.Food),water=funds.Get(ResourceType.Water),scrap=funds.Get(ResourceType.Scrap),lumber=funds.Get(ResourceType.Lumber);
        town.AdvanceDevelopment(3600,true);
        Check(funds.Get(ResourceType.Food)==food+80 && funds.Get(ResourceType.Water)==water+90 && funds.Get(ResourceType.Scrap)==scrap+100 && funds.Get(ResourceType.Lumber)==lumber+100,"farm, well, scrapyard and mill each auto-produce 100 per hour; population still consumes supplies");
        Check(town.CaptureDevelopment().Plots.All(x=>x.Stored==0) && town.Population==30,"automatic output does not fill buffers when wallet has space");
        bool complete=false;town.Changed+=()=>{complete=funds.Get(ResourceType.Scrap)==scrap+101 && town.Plot(plots[2].Id).Stored==0;throw new Exception("broken observer");};
        town.AdvanceDevelopment(36,true);Check(complete && town.NotificationErrors.Length==1,"observers see complete output and ledger when one UI throws");
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
