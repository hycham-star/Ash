using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Ash;

internal static class BuildingChecks
{
    private sealed class Codec : IProgressCodec
    {
        private static readonly JsonSerializerOptions Options=new JsonSerializerOptions { IncludeFields=true };
        public string Encode(ProgressState state)=>JsonSerializer.Serialize(state,Options);
        public ProgressState Decode(string json)=>JsonSerializer.Deserialize<ProgressState>(json,Options);
    }
    internal static int Run(HeroDefinition[] heroes,UnitDefinition[] fighters)
    {
        int passed=0;
        void C(bool value,string message) { if(!value)throw new Exception("FAIL building "+message); passed++; Console.WriteLine("PASS building "+message); }
        void Reject<T>(Action action,string name) where T:Exception {
            try { action(); } catch(T) { C(true,name); return; } throw new Exception("FAIL expected "+typeof(T).Name+": "+name);
        }
        var money=new ResourceManager(new[] { new ResourceAmount(ResourceType.Scrap,100),new ResourceAmount(ResourceType.Food,100),new ResourceAmount(ResourceType.Water,50) });
        var buildings=new BuildingService(money); int notifications=0;
        buildings.Changed+=()=>notifications++;
        C(buildings.Capture().All(b=>b.Level==1 && b.Stored==0 && b.Seconds==0),"three level-one empty buildings");
        C(buildings.Upgrade(BuildingKind.FoodGarden)==BuildingCommandResult.HeadquartersRequired && money.Get(ResourceType.Scrap)==100 && notifications==0,"HQ gate rejects upgrade without debit or event");
        C(buildings.Upgrade(BuildingKind.Headquarters)==BuildingCommandResult.Success && buildings.Level(BuildingKind.Headquarters)==2,"HQ upgrade completes");
        C(money.Get(ResourceType.Scrap)==60 && money.Get(ResourceType.Food)==90 && money.Get(ResourceType.Water)==45,"upgrade debits all exact costs once");
        C(buildings.Upgrade(BuildingKind.FoodGarden)==BuildingCommandResult.Success && buildings.Rate(BuildingKind.FoodGarden)==20 && buildings.Capacity(BuildingKind.FoodGarden)==100,"producer upgrade raises output and storage");
        C(buildings.Upgrade(BuildingKind.Headquarters)==BuildingCommandResult.InsufficientResources && buildings.Level(BuildingKind.Headquarters)==2 && money.Get(ResourceType.Scrap)==40,"unaffordable upgrade preserves level and resources");
        buildings.Advance(59);
        C(buildings.Stored(BuildingKind.FoodGarden)==0 && buildings.Seconds(BuildingKind.FoodGarden)==59,"partial minute stores timer, not rewards");
        buildings.Advance(1);
        C(buildings.Stored(BuildingKind.FoodGarden)==20 && buildings.Stored(BuildingKind.WaterStation)==6 && buildings.Seconds(BuildingKind.FoodGarden)==0,"both producers complete deterministic batch");
        C(money.Get(ResourceType.Food)==80 && money.Get(ResourceType.Water)==40,"production waits for collection");
        C(buildings.Collect(BuildingKind.FoodGarden)==BuildingCommandResult.Success && money.Get(ResourceType.Food)==100 && buildings.Stored(BuildingKind.FoodGarden)==0,"collection credits shared ledger exactly once");
        C(buildings.Collect(BuildingKind.FoodGarden)==BuildingCommandResult.NothingToCollect && money.Get(ResourceType.Food)==100,"repeated collection gives no duplicate reward");
        buildings.Advance(int.MaxValue);
        C(buildings.Stored(BuildingKind.FoodGarden)==100 && buildings.Stored(BuildingKind.WaterStation)==30 && buildings.Seconds(BuildingKind.FoodGarden)==0,"large elapsed input cannot overflow capped storage");
        int events=notifications; buildings.Advance(600);
        C(notifications==events,"full storage stops production and discards elapsed time");
        buildings.Collect(BuildingKind.WaterStation); buildings.Advance(59);
        C(buildings.Stored(BuildingKind.WaterStation)==0 && buildings.Seconds(BuildingKind.WaterStation)==59,"collecting full storage resumes fresh cycle");
        var copy=buildings.Capture(); copy[0].Level=25;
        C(buildings.Level(BuildingKind.Headquarters)==2,"export is detached");
        Reject<ArgumentOutOfRangeException>(()=>buildings.Advance(-1),"negative elapsed time rejected");
        Reject<ArgumentOutOfRangeException>(()=>buildings.Level((BuildingKind)99),"unknown building rejected");
        C(buildings.Collect(BuildingKind.Headquarters)==BuildingCommandResult.NothingToCollect,"HQ does not produce resources");
        var full=new ResourceManager(new[] { new ResourceAmount(ResourceType.Food,int.MaxValue) });
        var fullBuildings=new BuildingService(full); fullBuildings.Advance(60);
        C(fullBuildings.Collect(BuildingKind.FoodGarden)==BuildingCommandResult.ResourceLimit && fullBuildings.Stored(BuildingKind.FoodGarden)==10 && full.Get(ResourceType.Food)==int.MaxValue,"overflow collection retains stored batch and ledger");
        var rich=new ResourceManager(new[] { new ResourceAmount(ResourceType.Scrap,10000000),new ResourceAmount(ResourceType.Food,10000000),new ResourceAmount(ResourceType.Water,10000000) });
        var high=new BuildingService(rich);
        for(int level=1;level<25;level++)high.Upgrade(BuildingKind.Headquarters);
        for(int level=1;level<20;level++) { high.Upgrade(BuildingKind.FoodGarden); high.Upgrade(BuildingKind.WaterStation); }
        C(high.Level(BuildingKind.Headquarters)==25 && high.Level(BuildingKind.FoodGarden)==20 && high.Level(BuildingKind.WaterStation)==20,"GDD caps reachable with resources");
        int before=rich.Get(ResourceType.Scrap);
        C(high.Upgrade(BuildingKind.Headquarters)==BuildingCommandResult.MaximumLevel && high.Upgrade(BuildingKind.FoodGarden)==BuildingCommandResult.MaximumLevel && rich.Get(ResourceType.Scrap)==before,"max level cannot spend again");
        var growing=new BuildingService(rich); growing.Advance(65);
        growing.Upgrade(BuildingKind.Headquarters); growing.Upgrade(BuildingKind.FoodGarden);
        C(growing.Stored(BuildingKind.FoodGarden)==10 && growing.Seconds(BuildingKind.FoodGarden)==5,"upgrade retains existing stock and partial time");
        growing.Advance(55);
        C(growing.Stored(BuildingKind.FoodGarden)==30 && growing.Stored(BuildingKind.WaterStation)==12,"next batch uses upgraded rate without changing other producer");
        bool atomic=false,guarded=false,lateObserver=false;
        var observed=new BuildingService(rich);
        rich.Changed+=()=> { atomic=observed.Level(BuildingKind.Headquarters)==2; };
        observed.Changed+=()=> { try { observed.Upgrade(BuildingKind.Headquarters); } catch(InvalidOperationException) { guarded=true; } throw new Exception("observer failure"); };
        observed.Changed+=()=>lateObserver=true;
        C(observed.Upgrade(BuildingKind.Headquarters)==BuildingCommandResult.Success && atomic && guarded && lateObserver && observed.NotificationErrors.Length==1,"observers see committed state; reentry guarded; later observers still notified");

        var armies=new HeroArmyService(heroes.Select(h=>new HeroData("owned-"+h.Id,h)),fighters.Select(f=>new UnitData(f)),100);
        var fragments=new HeroFragmentWallet(heroes.Select(h=>h.Id),new Dictionary<string,int> { { armies.Heroes[0].Definition.Id,3 } });
        var progress=new ProgressPersistence(armies,money,fragments,buildings); var codec=new Codec();
        ProgressState Copy(ProgressState s)=>codec.Decode(codec.Encode(s));
        armies.AssignFighters(armies.Heroes[0].InstanceId,"infantry",5); armies.GrantHeroXp(armies.Heroes[0].InstanceId,125);
        var saved=progress.Capture(); string exact=codec.Encode(saved);
        C(saved.Version==ProgressPersistence.Version && saved.Buildings.Length==3,"current schema includes building ledger");
        buildings.Collect(BuildingKind.FoodGarden); progress.Restore(Copy(saved));
        C(codec.Encode(progress.Capture())==exact,"restore preserves all domains, stored production and partial timers");
        var legacy=Copy(saved); legacy.Version=1;AssetFixtures.AsLegacyWorld(legacy);legacy.Resources=legacy.Resources.Where(r=>r.Type!=ResourceType.Gold).ToArray(); legacy.Buildings=null; legacy.Scavenging=null; legacy.Resources=legacy.Resources.Where(r=>r.Type!=ResourceType.Lumber && r.Type!=ResourceType.Gold).ToArray();
        progress.Restore(codec.Decode(codec.Encode(legacy)));
        C(buildings.Capture().All(b=>b.Level==1 && b.Stored==0 && b.Seconds==0),"version-one migration initializes buildings");
        var migrated=progress.Capture(); var expected=Copy(saved); expected.Buildings=migrated.Buildings;
        C(codec.Encode(migrated)==codec.Encode(expected),"legacy migration retains exact heroes, armies, reserves, resources and fragments");
        progress.Restore(saved);
        var invalids=new List<Action<ProgressState>> {
            s=>s.Buildings=null, s=>s.Buildings=new BuildingState[0], s=>s.Buildings[1]=s.Buildings[0], s=>s.Buildings[0]=null,
            s=>s.Buildings[0].Level=26, s=>s.Buildings[1].Level=21, s=>s.Buildings[1].Level=3,
            s=>s.Buildings[1].Stored=-1, s=>s.Buildings[1].Stored=101, s=>s.Buildings[2].Seconds=60,
            s=>s.Buildings[0].Stored=1, s=>s.Buildings[0].Kind=(BuildingKind)99, s=>s.Buildings[2].Seconds=-1,
            s=> { s.Buildings[1].Stored=100; s.Buildings[1].Seconds=1; }, s=>s.Version=1
        };
        foreach(var corrupt in invalids) {
            var candidate=Copy(saved); corrupt(candidate);
            Reject<ArgumentException>(()=>progress.Restore(candidate),"malformed building snapshot rejected");
            C(codec.Encode(progress.Capture())==exact,"invalid building save changes no live domain");
        }
        string directory=Path.Combine(Path.GetTempPath(),"ash-buildings-"+Guid.NewGuid().ToString("N"));
        try {
            var store=new LocalSaveStore(directory,codec,progress.Validate); store.Save(legacy);
            progress.Restore(store.Load().State); store.Save(progress.Capture());
            C(store.Load().State.Version==ProgressPersistence.Version && File.Exists(store.BackupPath),"legacy disk slot upgrades to current schema with old backup");
            progress.Restore(saved); store.Save(progress.Capture());
            buildings.Collect(BuildingKind.FoodGarden); progress.Restore(store.Load().State);
            C(codec.Encode(progress.Capture())==exact,"building balances and collection survive actual disk round trip");
            var reset=Copy(saved); reset.Buildings=new BuildingService(new ResourceManager()).Capture();
            store.Reset(reset); progress.Restore(store.Load().State);
            C(buildings.Capture().All(b=>b.Level==1 && b.Stored==0 && b.Seconds==0) && !File.Exists(store.BackupPath),"explicit reset clears building levels, storage, timers and old backup");
        } finally { if(Directory.Exists(directory))Directory.Delete(directory,true); }
        return passed;
    }
}
