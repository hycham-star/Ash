using System;
using System.Linq;
using System.Text.Json;
using Ash;

internal static class TrainingChecks
{
    private sealed class Codec:IProgressCodec
    {
        private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
        public string Encode(ProgressState state)=>JsonSerializer.Serialize(state,Options);
        public ProgressState Decode(string json)=>ProgressJsonDecoder.Decode(json,(text,type)=>JsonSerializer.Deserialize(text,type,Options));
    }
    public static int Run(HeroDefinition[] heroes,UnitDefinition[] units)
    {
        int count=0;void Check(bool ok,string label){if(!ok)throw new Exception("FAIL training "+label);count++;Console.WriteLine("PASS training "+label);}
        var money=new ResourceManager(new[]{new ResourceAmount(ResourceType.Food,10000),new ResourceAmount(ResourceType.Water,10000),new ResourceAmount(ResourceType.Scrap,10000),new ResourceAmount(ResourceType.Lumber,10000),new ResourceAmount(ResourceType.Fuel,10000)});
        var armies=new HeroArmyService(heroes.Select(h=>new HeroData("owned-"+h.Id,h)),units.Select(u=>new UnitData(u)),0);
        var buildings=new BuildingService(money);var hospital=armies.GetHospital(money);var training=new FighterTrainingService(armies,money,buildings,hospital);
        Check(training.Train("plot-01",1)==TrainingResult.CenterRequired && training.Train("unknown",1)==TrainingResult.CenterRequired,"only constructed training centers can train");
        var kinds=new[]{PlotKind.InfantryCenter,PlotKind.BikerCenter,PlotKind.ArmyCenter};var types=new[]{FighterType.Melee,FighterType.Biker,FighterType.Sharpshooter};
        for(int i=0;i<3;i++)
        {
            string id="plot-"+(i+1).ToString("00");Check(buildings.BuildPlot(id,kinds[i])==BuildingCommandResult.Success,"construction accepts "+kinds[i]);
            Check(training.Fighter(kinds[i]).Definition.Type==types[i],"center maps to exactly "+types[i]);
            int before=money.Get(ResourceType.Food);Check(training.Train(id,0)==TrainingResult.InvalidQuantity && training.Train(id,11)==TrainingResult.InvalidQuantity && money.Get(ResourceType.Food)==before,"invalid batch does not charge resources");
            var costs=training.Cost(kinds[i],3);var balances=costs.ToDictionary(c=>c.Type,c=>money.Get(c.Type));
            Check(training.Train(id,3)==TrainingResult.Success && armies.Available(training.Fighter(kinds[i]).Definition.Id)==3 && costs.All(c=>money.Get(c.Type)==balances[c.Type]-c.Amount),"paid batch credits matching reserve and debits exact costs");
        }
        Check(buildings.HousingCapacity==30 && buildings.EmployedResidents==6,"centers employ residents without adding housing");
        buildings.AdvanceDevelopment(60);Check(armies.Fighters.All(unit=>armies.Available(unit.Definition.Id)==3) && buildings.CaptureDevelopment().Plots.Where(p=>BuildingService.IsTraining(p.Kind)).All(p=>p.Stored==0 && p.Seconds==0),"idle centers do not grant free fighters or resource output");
        var melee=training.Fighter(PlotKind.InfantryCenter);armies.AssignFighters(armies.Heroes[0].InstanceId,melee.Definition.Id,2);armies.AssignFighters(armies.Heroes[1].InstanceId,melee.Definition.Id,1);
        Check(armies.Heroes[0].Army.TotalUnits==2 && armies.Heroes[1].Army.TotalUnits==1 && armies.Available(melee.Definition.Id)==0,"trained fighters fill independently owned hero armies");
        int food=money.Get(ResourceType.Food),water=money.Get(ResourceType.Water),stock=armies.Available(melee.Definition.Id);bool resourceObserved=false,armyObserved=false,reentryRejected=false;
        Action resourceObserver=()=>{resourceObserved=money.Get(ResourceType.Food)==food-4 && money.Get(ResourceType.Water)==water-2 && armies.Available(melee.Definition.Id)==stock+2;try{training.Train("plot-01",1);}catch(InvalidOperationException){reentryRejected=true;}};
        Action armyObserver=()=>armyObserved=money.Get(ResourceType.Food)==food-4 && armies.Available(melee.Definition.Id)==stock+2;
        money.Changed+=resourceObserver;armies.Changed+=armyObserver;training.Train("plot-01",2);money.Changed-=resourceObserver;armies.Changed-=armyObserver;
        Check(resourceObserved && armyObserved && reentryRejected,"observers see both committed domains and cannot reenter training");
        Action broken=()=>throw new Exception("test observer");money.Changed+=broken;stock=armies.Available(melee.Definition.Id);var result=training.Train("plot-01",1);money.Changed-=broken;
        Check(result==TrainingResult.Success && armies.Available(melee.Definition.Id)==stock+1 && training.NotificationErrors.Length==1,"observer failure cannot duplicate or roll back paid training");
        buildings.Upgrade(BuildingKind.Headquarters);buildings.UpgradePlot("plot-01");Check(training.CanTrain("plot-01",15)==TrainingResult.Success && training.Train("plot-01",15)==TrainingResult.Success,"center upgrades increase batch capacity");
        int fuel=money.Get(ResourceType.Fuel);money.TrySpend(new[]{new ResourceAmount(ResourceType.Fuel,fuel)});food=money.Get(ResourceType.Food);stock=armies.Available(training.Fighter(PlotKind.BikerCenter).Definition.Id);
        Check(training.Train("plot-02",1)==TrainingResult.InsufficientResources && money.Get(ResourceType.Food)==food && armies.Available(training.Fighter(PlotKind.BikerCenter).Definition.Id)==stock,"fuel shortage rejects biker training atomically");
        var progress=new ProgressPersistence(armies,money,new HeroFragmentWallet(heroes.Select(h=>h.Id)),buildings,new ScavengingService(money,125));var codec=new Codec();string exact=codec.Encode(progress.Capture());progress.Restore(codec.Decode(exact));
        Check(codec.Encode(progress.Capture())==exact,"new center types, trained reserves and paid costs survive exact save/load");
        stock=armies.Available(melee.Definition.Id);armies.RestoreReserveValidated(melee.Definition.Id,int.MaxValue-3);
        Check(training.CanTrain("plot-01",1)==TrainingResult.FighterLimit,"assigned plus reserve overflow rejected before purchase");armies.RestoreReserveValidated(melee.Definition.Id,stock);
        var legacy=progress.Capture();legacy.Version=9;legacy.Resources=legacy.Resources.Where(r=>r.Type!=ResourceType.Gold).ToArray();legacy.Development.Plots=legacy.Development.Plots.Take(12).ToArray();legacy.Placement.Positions=legacy.Placement.Positions.Take(18).ToArray();
        // Legacy files have the old building kinds. Retain live production in a farm instead.
        foreach(var p in legacy.Development.Plots){p.Kind=PlotKind.Empty;p.Level=0;p.Stored=0;p.Seconds=0;}
        legacy.Development.Plots[0].Kind=PlotKind.Farm;legacy.Development.Plots[0].Level=1;legacy.Development.Plots[0].Stored=7;legacy.Development.Plots[0].Seconds=23;
        var position=legacy.Placement.Positions.Single(p=>p.Id=="core-headquarters");position.X=-6;position.Z=3;
        var savedResources=codec.Encode(legacy);progress.Restore(legacy);
        Check(buildings.CaptureDevelopment().Plots.Length==24 && buildings.CapturePlacement().Positions.Length==30 && buildings.Position("core-headquarters").X==-6,"version-nine migration expands plots while preserving moved core buildings");
        Check(buildings.Plot("plot-01").Stored==7 && buildings.Plot("plot-01").Seconds==23 && armies.Heroes[0].Army.TotalUnits==2 && armies.Heroes[1].Army.TotalUnits==1,"migration preserves production and both independent hero armies");
        Check(buildings.CaptureDevelopment().Plots.Skip(12).All(p=>p.Kind==PlotKind.Empty) && progress.Capture().Version==ProgressPersistence.Version,"migration adds only empty expansion spots and writes current version");
        string migrated=codec.Encode(progress.Capture());var brokenSave=codec.Decode(savedResources);brokenSave.Development.Plots=brokenSave.Development.Plots.Skip(1).ToArray();
        bool rejected=false;try{progress.Restore(brokenSave);}catch(ArgumentException){rejected=true;}Check(rejected && codec.Encode(progress.Capture())==migrated,"incomplete legacy expansion rejects atomically");
        Check(SettlementLayout.HalfWidth*2==56 && SettlementLayout.HalfDepth*2==64 && SettlementLayout.Plots.Select(p=>p.Id).Distinct().Count()==24,"expanded settlement measures 56 by 64 with 24 distinct spots");
        return count;
    }
}
