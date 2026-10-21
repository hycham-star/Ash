using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Ash;

internal static class DevelopmentChecks
{
    private sealed class Codec:IProgressCodec
    {
        private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
        public string Encode(ProgressState state)=>JsonSerializer.Serialize(state,Options);
        public ProgressState Decode(string json)=>ProgressJsonDecoder.Decode(json,(s,t)=>JsonSerializer.Deserialize(s,t,Options));
    }
    internal static int Run(HeroDefinition[] heroes,UnitDefinition[] fighters)
    {
        int passed=0;void C(bool value,string name){if(!value)throw new Exception("FAIL development "+name);passed++;Console.WriteLine("PASS development "+name);}
        void Reject<T>(Action action,string name)where T:Exception{try{action();}catch(T){C(true,name);return;}throw new Exception("FAIL expected "+typeof(T).Name+": "+name);}
        ResourceManager Money(int amount=100000)=>new ResourceManager(new[]{new ResourceAmount(ResourceType.Scrap,amount),new ResourceAmount(ResourceType.Lumber,amount),new ResourceAmount(ResourceType.Food,amount),new ResourceAmount(ResourceType.Water,amount)});
        var codec=new Codec();var money=Money();var b=new BuildingService(money);string[] ids=SettlementLayout.Plots.Select(p=>p.Id).ToArray();
        C(ids.Length==24 && ids.Distinct().Count()==24 && b.Population==20 && b.HousingCapacity==30 && b.FreeResidents==20,"twenty-four stable plots and initial resident population");
        C(SettlementLayout.Plots.All(p=>Math.Abs(p.X)+3<SettlementLayout.HalfWidth && Math.Abs(p.Z)+3<SettlementLayout.HalfDepth),"plot footprints stay within expanded walls");
        C(SettlementLayout.Plots.All(a=>SettlementLayout.Plots.All(z=>a==z || Math.Abs(a.X-z.X)>=6 || Math.Abs(a.Z-z.Z)>=6)),"plot footprints do not overlap");
        b.ValidateDevelopment(b.CaptureDevelopment(),1);int scrap=money.Get(ResourceType.Scrap),lumber=money.Get(ResourceType.Lumber);
        C(b.BuildPlot(ids[0],PlotKind.Farm)==BuildingCommandResult.Success && b.FreeResidents==17 && b.Plot(ids[0]).Level==1,"farm construction assigns three existing residents");
        C(money.Get(ResourceType.Scrap)==scrap-20 && money.Get(ResourceType.Lumber)==lumber-10,"construction uses exact shared ledger costs");
        var previous=codec.Encode(new ProgressState{Development=b.CaptureDevelopment()});scrap=money.Get(ResourceType.Scrap);
        C(b.BuildPlot(ids[0],PlotKind.Housing)==BuildingCommandResult.PlotOccupied && money.Get(ResourceType.Scrap)==scrap,"occupied plot cannot be replaced or charged again");
        C(b.BuildPlot(ids[1],PlotKind.WaterWell)==BuildingCommandResult.Success && b.BuildPlot(ids[2],PlotKind.Scrapyard)==BuildingCommandResult.Success && b.EmployedResidents==8,"well and scrapyard require resident workers");
        int food=money.Get(ResourceType.Food),water=money.Get(ResourceType.Water);b.AdvanceDevelopment(59);
        C(b.Plot(ids[0]).Stored==0 && b.Plot(ids[0]).Seconds==59 && b.Population==20,"fractional production and population timers do not grant early output");
        b.AdvanceDevelopment(1);C(b.Plot(ids[0]).Stored==10 && b.Plot(ids[1]).Stored==8 && b.Plot(ids[2]).Stored==6 && b.Population==21,"new producers and resident arrivals use exact minute ticks");
        C(money.Get(ResourceType.Food)==food-2 && money.Get(ResourceType.Water)==water-1,"growth spends food and water per new resident");
        food=money.Get(ResourceType.Food);C(b.CollectPlot(ids[0])==BuildingCommandResult.Success && money.Get(ResourceType.Food)==food+10 && b.Plot(ids[0]).Stored==0,"plot collection uses existing shared resource ledger");
        C(b.CollectPlot(ids[0])==BuildingCommandResult.NothingToCollect,"repeated collection cannot duplicate output");
        C(b.UpgradePlot(ids[0])==BuildingCommandResult.HeadquartersRequired,"plot upgrades reuse Headquarters gate");b.Upgrade(BuildingKind.Headquarters);b.AdvanceDevelopment(7);
        C(b.UpgradePlot(ids[0])==BuildingCommandResult.Success && b.Plot(ids[0]).Seconds==7 && BuildingService.PlotRate(b.Plot(ids[0]).Kind,b.Plot(ids[0]).Level)==20,"plot upgrade preserves progress and raises output");
        C(b.BuildPlot(ids[3],PlotKind.Housing)==BuildingCommandResult.Success && b.HousingCapacity==40 && b.EmployedResidents==8,"housing raises capacity without consuming worker jobs");
        b.AdvanceDevelopment(int.MaxValue);C(b.Population==40 && b.GrowthSeconds==0 && b.Plot(ids[0]).Stored==100 && b.Plot(ids[1]).Stored==40 && b.Plot(ids[2]).Stored==30,"large elapsed interval obeys housing and storage limits");
        var cap=b.CaptureDevelopment();b.AdvanceDevelopment(999);C(JsonSerializer.Serialize(cap, new JsonSerializerOptions{IncludeFields=true})==JsonSerializer.Serialize(b.CaptureDevelopment(),new JsonSerializerOptions{IncludeFields=true}),"full housing and storage do not bank extra production");
        var poorMoney=new ResourceManager();var poor=new BuildingService(poorMoney);C(poor.BuildPlot(ids[0],PlotKind.Farm)==BuildingCommandResult.InsufficientResources && poor.Plot(ids[0]).Kind==PlotKind.Empty,"unaffordable construction leaves plot empty");
        poor.AdvanceDevelopment(6000);C(poor.Population==20 && poor.GrowthSeconds==0,"lack of food/water pauses arrivals without killing residents");poorMoney.Add(ResourceType.Food,2);poorMoney.Add(ResourceType.Water,1);poor.AdvanceDevelopment(59);C(poor.Population==20,"starved time is not banked for free arrivals");poor.AdvanceDevelopment(1);C(poor.Population==21 && poorMoney.Get(ResourceType.Food)==0 && poorMoney.Get(ResourceType.Water)==0,"growth can use exactly one affordable arrival");
        var staff=new BuildingService(Money());for(int i=0;i<6;i++)C(staff.BuildPlot(ids[i],PlotKind.Farm)==BuildingCommandResult.Success,"available resident staffing admits producer");
        C(staff.FreeResidents==2 && staff.BuildPlot(ids[6],PlotKind.Scrapyard)==BuildingCommandResult.InsufficientResidents,"workforce limit prevents double employment");
        staff.AdvanceDevelopment(60);C(staff.BuildPlot(ids[6],PlotKind.Scrapyard)==BuildingCommandResult.Success && staff.FreeResidents==0,"population arrival makes another staffed producer possible");
        var splitMoney=Money();var wholeMoney=Money();var split=new BuildingService(splitMoney);var whole=new BuildingService(wholeMoney);
        foreach(var service in new[]{split,whole}){service.BuildPlot(ids[0],PlotKind.Farm);service.BuildPlot(ids[1],PlotKind.Housing);}
        foreach(int seconds in new[]{1,17,42,79,61,1000})split.AdvanceDevelopment(seconds);whole.AdvanceDevelopment(1200);
        var jsonOptions=new JsonSerializerOptions{IncludeFields=true};C(JsonSerializer.Serialize(split.CaptureDevelopment(),jsonOptions)==JsonSerializer.Serialize(whole.CaptureDevelopment(),jsonOptions) && splitMoney.Get(ResourceType.Food)==wholeMoney.Get(ResourceType.Food) && splitMoney.Get(ResourceType.Water)==wholeMoney.Get(ResourceType.Water),"population/plot ticking is deterministic across interval partitions");
        var overflowMoney=new ResourceManager(new[]{new ResourceAmount(ResourceType.Scrap,int.MaxValue),new ResourceAmount(ResourceType.Lumber,1000)});var overflow=new BuildingService(overflowMoney);overflow.BuildPlot(ids[0],PlotKind.Scrapyard);overflow.AdvanceDevelopment(60);overflowMoney.Add(ResourceType.Scrap,30);
        C(overflow.CollectPlot(ids[0])==BuildingCommandResult.ResourceLimit && overflow.Plot(ids[0]).Stored==6,"ledger overflow preserves plot stock");
        bool guarded=false,later=false;var observed=new BuildingService(Money());observed.Changed+=()=>{try{observed.BuildPlot(ids[1],PlotKind.Farm);}catch(InvalidOperationException){guarded=true;}throw new Exception("observer");};observed.Changed+=()=>later=true;observed.BuildPlot(ids[0],PlotKind.Housing);
        C(guarded && later && observed.NotificationErrors.Length==1,"construction observer errors preserve state, later notifications and reentry guard");
        Reject<ArgumentException>(()=>b.BuildPlot("missing",PlotKind.Farm),"unknown plot rejected");Reject<ArgumentOutOfRangeException>(()=>b.AdvanceDevelopment(-1),"negative settlement time rejected");C(b.BuildPlot(ids[4],(PlotKind)99)==BuildingCommandResult.InvalidPlotType,"unknown plot type rejected");
        var arms=new HeroArmyService(heroes.Select(h=>new HeroData("owned-"+h.Id,h)),fighters.Select(f=>new UnitData(f)),100);var fragments=new HeroFragmentWallet(heroes.Select(h=>h.Id));var scav=new ScavengingService(money,125);var progress=new ProgressPersistence(arms,money,fragments,b,scav);
        string exact=codec.Encode(progress.Capture());var saved=progress.Capture();b.CollectPlot(ids[0]);progress.Restore(codec.Decode(exact));C(codec.Encode(progress.Capture())==exact,"development and every previous domain round-trip together");
        var reordered=codec.Decode(exact);Array.Reverse(reordered.Development.Plots);progress.Restore(reordered);C(codec.Encode(progress.Capture())==exact,"save file order cannot move plots or reorder their identities");
        foreach(Action<ProgressState> corrupt in new Action<ProgressState>[] {
            s=>s.Development=null,s=>s.Development.Plots=null,s=>s.Development.Plots=new SettlementPlotState[0],s=>s.Development.Plots[0]=null,s=>s.Development.Plots[1]=s.Development.Plots[0],s=>s.Development.Plots[0].Id="wrong",s=>s.Development.Population=0,s=>s.Development.Population=9999,s=>s.Development.GrowthSeconds=60,s=>s.Development.Plots[0].Kind=(PlotKind)99,s=>s.Development.Plots[0].Level=11,s=>s.Development.Plots[0].Stored=-1,s=>s.Development.Plots[0].Seconds=60,s=>s.Development.Plots[0].Level=3,s=>s.Development.Plots[4].Stored=1,s=>s.Development.Plots[3].Stored=1 })
        {var bad=codec.Decode(exact);corrupt(bad);Reject<ArgumentException>(()=>progress.Restore(bad),"malformed development rejected");C(codec.Encode(progress.Capture())==exact,"rejected development cannot partially restore any domain");}
        var legacy=codec.Decode(exact);legacy.Version=6;legacy.Development=null;progress.Restore(legacy);
        C(b.Population==20 && b.CaptureDevelopment().Plots.All(p=>p.Kind==PlotKind.Empty) && b.Level(BuildingKind.Headquarters)==2 && scav.Seed==125,"version-six migration adds empty plots and residents without resetting old progression");
        string directory=Path.Combine(Path.GetTempPath(),"ash-development-"+Guid.NewGuid().ToString("N"));
        try
        {
            progress.Restore(saved);var store=new LocalSaveStore(directory,codec,progress.Validate);store.Save(progress.Capture());b.CollectPlot(ids[0]);progress.Restore(store.Load().State);C(codec.Encode(progress.Capture())==exact,"disk restart preserves construction, levels, stock, population and timers");
            var reset=progress.Capture();reset.Development=SettlementDevelopmentState.Fresh();store.Reset(reset);progress.Restore(store.Load().State);C(b.Population==20 && b.CaptureDevelopment().Plots.All(p=>p.Kind==PlotKind.Empty) && !File.Exists(store.BackupPath),"development reset uses isolated existing reset path");
        }finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        return passed;
    }
}
