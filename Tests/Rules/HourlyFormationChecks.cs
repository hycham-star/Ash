using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Ash;

internal static class HourlyFormationChecks
{
    private sealed class Codec:IProgressCodec
    {
        private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
        public string Encode(ProgressState state)=>JsonSerializer.Serialize(state,Options);
        public ProgressState Decode(string json)=>ProgressJsonDecoder.Decode(json,(s,t)=>JsonSerializer.Deserialize(s,t,Options));
    }
    private sealed class Context
    {
        public ResourceManager Money;public BuildingService Buildings;public HeroArmyService Armies;public BattleManager Battles;public FighterTrainingService Training;
        public ScavengingService Sites;public WorldMapService World;public ProgressPersistence Progress;
        public HeroData Hero=>Armies.Heroes[0];public UnitData Fighter=>Armies.Fighters[0];
        public Context(HeroDefinition[] definitions,UnitDefinition[] units,int reserve=30)
        {
            Money=new ResourceManager(Enum.GetValues<ResourceType>().Select(t=>new ResourceAmount(t,100000)));
            Buildings=new BuildingService(Money);Armies=new HeroArmyService(definitions.Select(d=>new HeroData("formation-"+d.Id,d)),units.Select(u=>new UnitData(u)),reserve);
            var fragments=new HeroFragmentWallet(definitions.Select(d=>d.Id));Battles=new BattleManager(Armies,Money,fragments);
            Sites=new ScavengingService(Money,42);World=new WorldMapService(Armies,Sites,Money,Buildings,Battles);World.ConfigureEncounters(new[]{Strong(),Strong(),Strong()});
            Training=new FighterTrainingService(Armies,Money,Buildings,Battles.Hospital);Progress=new ProgressPersistence(Armies,Money,fragments,Buildings,Sites);
        }
        public NpcEncounter Strong()=>new NpcEncounter("strong-formation-test","Strong camp",new BattleArmy("strong",Armies.Fighters.Select(f=>new BattleFighter(f,1000))),0,Array.Empty<ResourceAmount>(),0);
        public string Center=>SettlementLayout.Plots[0].Id;
        public void BuildCenter(){if(Buildings.BuildPlot(Center,PlotKind.InfantryCenter)!=BuildingCommandResult.Success)throw new Exception("Fixture center failed");}
    }
    public static int Run(HeroDefinition[] definitions,UnitDefinition[] units)
    {
        int count=0;void C(bool ok,string label){if(!ok)throw new Exception("FAIL hourly/formation "+label);count++;Console.WriteLine("PASS hourly/formation "+label);}
        var c=new Context(definitions,units);var b=c.Buildings;
        foreach(var kind in new[]{PlotKind.Farm,PlotKind.WaterWell,PlotKind.Scrapyard,PlotKind.LumberMill})
            for(int level=1;level<=10;level++)C(BuildingService.PlotHourlyRate(kind,level)==100+(level-1)*50,"uniform hourly rate "+kind+" level "+level);
        C(b.HourlyRate(BuildingKind.FoodGarden)==100 && b.HourlyRate(BuildingKind.WaterStation)==100,"starting producers use same hourly policy");
        b.Upgrade(BuildingKind.Headquarters);b.Upgrade(BuildingKind.FoodGarden);int food=c.Money.Get(ResourceType.Food),water=c.Money.Get(ResourceType.Water);
        b.Advance(3600,true);C(c.Money.Get(ResourceType.Food)==food+150 && c.Money.Get(ResourceType.Water)==water+100,"upgraded core pays 150/hour without changing level-one water");
        b.Upgrade(BuildingKind.Headquarters);b.Upgrade(BuildingKind.FoodGarden);b.Upgrade(BuildingKind.Headquarters);b.Upgrade(BuildingKind.FoodGarden);
        food=c.Money.Get(ResourceType.Food);b.Advance(3600,true);C(c.Money.Get(ResourceType.Food)==food+250,"non-integer per-minute rate pays exact 250/hour at level four");
        var moneyPieces=new ResourceManager();var moneyWhole=new ResourceManager();var pieces=new BuildingService(moneyPieces);var whole=new BuildingService(moneyWhole);
        for(int i=0;i<3600;i++)pieces.Advance(1,true);whole.Advance(3600,true);
        C(moneyPieces.Get(ResourceType.Food)==100 && moneyPieces.Get(ResourceType.Food)==moneyWhole.Get(ResourceType.Food) && pieces.Capture().Select(x=>x.ProductionWork).SequenceEqual(whole.Capture().Select(x=>x.ProductionWork)),"income and fractions are invariant under tick chunking");
        c=new Context(definitions,units);c.Armies.SetFormationQuantity(c.Hero.InstanceId,c.Fighter.Definition.Id,10);
        C(c.Armies.FormationQuantity(c.Hero.InstanceId,c.Fighter.Definition.Id)==10,"manual composition remembers chosen count");
        c.World.Dispatch(c.Hero.InstanceId,c.World.Camps[0].Id,MapMissionKind.Raid);int travel=c.World.TravelSeconds(c.World.Mission(c.Hero.InstanceId));
        c.World.Advance(travel+3);C(c.Hero.Army.TotalUnits==0 && c.Battles.Hospital.TotalWounded==10 && c.Armies.FormationTotal(c.Hero.InstanceId)==10,"raid wounds fighters without changing desired formation");
        c.BuildCenter();C(c.Training.ReinforceHomeArmies()==0,"returning convoy cannot draw replacement fighters");
        c.World.Advance(travel);food=c.Money.Get(ResourceType.Food);int scrap=c.Money.Get(ResourceType.Scrap);
        C(c.Training.ReinforceHomeArmies()==10 && c.Hero.Army.TotalUnits==10 && c.Armies.Available(c.Fighter.Definition.Id)==10,"home army refills exact missing count from matching trained reserve");
        C(c.Battles.Hospital.TotalWounded==10 && c.Money.Get(ResourceType.Food)==food && c.Money.Get(ResourceType.Scrap)==scrap,"replacement does not heal wounded, create fighters or spend resources");
        C(c.Training.ReinforceHomeArmies()==0,"repeated refill is idempotent");
        c=new Context(definitions,units,10);c.Armies.SetFormationQuantity(c.Hero.InstanceId,c.Fighter.Definition.Id,10);c.Battles.RunBattle(c.Hero.InstanceId,c.Strong());
        C(c.Training.ReinforceHomeArmies()==0 && c.Armies.FormationTotal(c.Hero.InstanceId)==10,"no station means no automatic transfer");
        c.BuildCenter();C(c.Training.ReinforceHomeArmies()==0 && c.Hero.Army.TotalUnits==0,"empty reserve waits while formation remains saved");
        C(c.Training.Train(c.Center,3)==TrainingResult.Success && c.Training.ReinforceHomeArmies()==3 && c.Hero.Army.TotalUnits==3 && c.Armies.FormationTotal(c.Hero.InstanceId)==10,"later paid manual training partially restores the exact formation");
        var codec=new Codec();string directory=Path.Combine(Path.GetTempPath(),"Ashlands-formation-"+Guid.NewGuid().ToString("N"));
        try {
            var saved=c.Progress.Capture();var store=new LocalSaveStore(directory,codec,c.Progress.Validate);store.Save(saved);
            var next=new Context(definitions,units);next.Progress.Restore(store.Load().State);c=next;
            C(c.Hero.Army.TotalUnits==3 && c.Armies.FormationTotal(c.Hero.InstanceId)==10 && c.Battles.Hospital.TotalWounded==10,"actual army, pending formation and wounded survive disk restart independently");
            c.Progress.Validate(c.Progress.Capture());
            var old=codec.Decode(codec.Encode(c.Progress.Capture()));old.Version=12;foreach(var h in old.Armies.Heroes)h.Formation=null;
            var legacy=new Context(definitions,units);legacy.Progress.Restore(old);
            C(legacy.Armies.FormationTotal(legacy.Hero.InstanceId)==3,"version-twelve migration adopts actual owned army without inventing missing historical troops");
            c.Armies.SetFormationQuantity(c.Hero.InstanceId,c.Fighter.Definition.Id,0);c.Training.Train(c.Center,2);c.Training.ReinforceHomeArmies();
            C(c.Hero.Army.TotalUnits==0 && c.Armies.FormationTotal(c.Hero.InstanceId)==0,"deselect cancels future refills as well as returning current fighters");
            var corrupt=codec.Decode(codec.Encode(saved));corrupt.Armies.Heroes[0].Formation=null;bool rejected=false;try{c.Progress.Restore(corrupt);}catch(ArgumentException){rejected=true;}
            C(rejected && c.Armies.FormationTotal(c.Hero.InstanceId)==0,"missing current-schema formation rejected atomically");
        } finally {if(Directory.Exists(directory))Directory.Delete(directory,true);}
        c=new Context(definitions,units);string mill=SettlementLayout.Plots[1].Id;
        C(c.Buildings.BuildPlot(mill,PlotKind.LumberMill)==BuildingCommandResult.Success && c.Buildings.Plot(mill).Level==1,"lumber mill uses normal construction, placement and workers");
        int lumber=c.Money.Get(ResourceType.Lumber);c.Buildings.AdvanceDevelopment(3600,true);C(c.Money.Get(ResourceType.Lumber)==lumber+100,"lumber mill credits actual lumber ledger automatically");
        c.Buildings.Upgrade(BuildingKind.Headquarters);c.Buildings.UpgradePlot(mill);lumber=c.Money.Get(ResourceType.Lumber);c.Buildings.AdvanceDevelopment(3600,true);
        C(c.Money.Get(ResourceType.Lumber)==lumber+150,"level-two mill pays 150/hour");
        var checkpoint=c.Progress.Capture();var loaded=new Context(definitions,units);loaded.Progress.Restore(codec.Decode(codec.Encode(checkpoint)));
        C(loaded.Buildings.Plot(mill).Kind==PlotKind.LumberMill && loaded.Buildings.Plot(mill).Level==2,"new mill and production fractions survive full progress codec");
        return count;
    }
}
