using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Ash;

internal static class ScavengingChecks
{
    private sealed class Codec : IProgressCodec
    {
        private static readonly JsonSerializerOptions Options=new JsonSerializerOptions { IncludeFields=true };
        public string Encode(ProgressState state)=>JsonSerializer.Serialize(state,Options);
        public ProgressState Decode(string json)=>JsonSerializer.Deserialize<ProgressState>(json,Options);
        public string Map(ScavengingState state)=>JsonSerializer.Serialize(state,Options);
    }
    internal static int Run(HeroDefinition[] heroes,UnitDefinition[] fighters)
    {
        int passed=0; var codec=new Codec();
        void C(bool value,string name) { if(!value)throw new Exception("FAIL scavenging "+name); passed++; Console.WriteLine("PASS scavenging "+name); }
        void Reject<T>(Action action,string name) where T:Exception {
            try { action(); }catch(T) { C(true,name); return; } throw new Exception("FAIL expected "+typeof(T).Name+": "+name);
        }
        var ledger=new ResourceManager(); var service=new ScavengingService(ledger,125);
        var fresh=service.Capture(); var food=fresh.Sites.First(s=>s.Resource==ResourceType.Food).Id;
        var water=fresh.Sites.First(s=>s.Resource==ResourceType.Water).Id;
        var scrap=fresh.Sites.First(s=>s.Resource==ResourceType.Scrap).Id;
        var lumber=fresh.Sites.First(s=>s.Resource==ResourceType.Lumber).Id;
        C((int)ResourceType.Survivors==4 && (int)ResourceType.Lumber==5,"Lumber appended without changing old resource IDs");
        C(fresh.Sites.Length==16 && fresh.Sites.Select(s=>s.Resource).Distinct().Count()==4 && fresh.Sites.GroupBy(s=>s.Resource).All(g=>g.Count()==4),"four sites of each required resource");
        C(codec.Map(fresh)==codec.Map(ScavengingService.Generate(125)),"same seed regenerates same sites");
        C(codec.Map(fresh)!=codec.Map(ScavengingService.Generate(126)),"different seed changes map positions");
        C(fresh.Sites.All(s=>Math.Abs(s.X)<=94 && Math.Abs(s.Z)<=94 && s.X*s.X+s.Z*s.Z>400),"sites within map and outside home clearing");
        C(fresh.Sites.All(a=>fresh.Sites.All(b=>a.Id==b.Id || (a.X-b.X)*(a.X-b.X)+(a.Z-b.Z)*(a.Z-b.Z)>=144)),"random sites do not overlap");
        for(int seed=1;seed<=100;seed++)service.Validate(ScavengingService.Generate(seed));
        C(service.FreeWorkers==10 && service.AssignedWorkers==0,"separate workforce starts unallocated");
        service.Advance(3600);
        C(codec.Map(service.Capture())==codec.Map(fresh),"unassigned sites never produce");
        C(service.Assign(food)==ScavengingResult.Success && service.FreeWorkers==9,"send consumes dedicated workers only");
        service.Advance(30);
        C(service.Site(food).Stored==5 && ledger.Get(ResourceType.Food)==0,"partial load stays with gathering team");
        C(service.Assign(food)==ScavengingResult.SiteBusy && service.Collect(food)==ScavengingResult.SiteBusy,"active trip cannot be silently topped up or unloaded");
        service.Recall(food);
        C(service.Site(food).Stored==0 && service.FreeWorkers==10 && ledger.Get(ResourceType.Food)==5,"early recall returns whole team and delivers partial cargo");
        string before=codec.Map(service.Capture());service.Advance(600);
        C(codec.Map(service.Capture())==before && ledger.Get(ResourceType.Food)==5,"returned workers wait for explicit resend");
        C(service.Collect(food)==ScavengingResult.NothingToCollect,"delivered load cannot be claimed twice");
        C(service.Assign(food,2)==ScavengingResult.Success && service.FreeWorkers==8,"player sends selected team size atomically");
        service.Advance(15);C(service.Site(food).Stored==5,"team rate uses assigned workforce");service.Recall(food);
        service.Assign(scrap);service.Advance(1);service.Recall(scrap);service.Assign(scrap);service.Advance(9);
        C(service.Site(scrap).Stored==0 && service.Site(scrap).Work==54,"new trip starts fresh without unearned fractional carryover");
        var split=new ScavengingService(new ResourceManager(),125);var single=new ScavengingService(new ResourceManager(),125);
        split.Assign(lumber);single.Assign(lumber);foreach(int seconds in new[]{3,7,13,2,36})split.Advance(seconds);single.Advance(61);
        C(codec.Map(split.Capture())==codec.Map(single.Capture()),"partial trip is deterministic across tick partitions");
        var busy=new ScavengingService(new ResourceManager(),125);busy.Assign(food,5);busy.Assign(water,5);
        C(busy.Assign(food)==ScavengingResult.SiteBusy && busy.Assign(scrap)==ScavengingResult.NoFreeWorkers && busy.AssignedWorkers==10,"busy centers and global workforce reject double allocation");
        busy.Recall(water);C(busy.Assign(lumber,5)==ScavengingResult.Success && busy.FreeWorkers==0,"recalled team can be sent to another center");
        var snapshot=busy.Capture();snapshot.Sites[0].Workers=0;C(busy.Site(food).Workers==5,"snapshots detach from live assignments");
        var stock=new ResourceManager();var depleted=new ScavengingService(stock,125);
        int deposit=depleted.Site(food).Remaining;
        for(int i=0;i<deposit/500;i++){depleted.Assign(food,5);depleted.Advance(int.MaxValue);}
        C(depleted.Site(food).Remaining==0 && stock.Get(ResourceType.Food)==deposit && depleted.FreeWorkers==10,"finite regional deposit delivers complete five-worker loads without overflow");
        C(depleted.Assign(food)==ScavengingResult.Exhausted,"empty center cannot launch a new trip");
        var cappedLedger=new ResourceManager();var capped=new ScavengingService(cappedLedger,125);capped.Assign(food);capped.Advance(599);
        C(capped.Site(food).Stored==99 && capped.Site(food).Workers==1 && cappedLedger.Get(ResourceType.Food)==0,"worker stays gathering until exact carrying threshold");
        capped.Advance(1);before=codec.Map(capped.Capture());capped.Advance(int.MaxValue);
        C(capped.Site(food).Stored==0 && capped.Site(food).Work==0 && capped.FreeWorkers==10 && cappedLedger.Get(ResourceType.Food)==100 && codec.Map(capped.Capture())==before,"full worker auto-delivers once and never auto-repeats");
        capped.Assign(food);capped.Advance(6);C(capped.Site(food).Stored==1,"only an explicit resend starts a new load");
        var wholeLedger=new ResourceManager();var chunkLedger=new ResourceManager();var whole=new ScavengingService(wholeLedger,125);var chunk=new ScavengingService(chunkLedger,125);
        whole.Assign(water,3);chunk.Assign(water,3);whole.Advance(4000);for(int i=0;i<4000;i++)chunk.Advance(1);
        C(codec.Map(whole.Capture())==codec.Map(chunk.Capture()) && wholeLedger.Get(ResourceType.Water)==300 && chunkLedger.Get(ResourceType.Water)==300,"large and partitioned advances complete only one identical trip");
        var full=new ResourceManager(new[]{new ResourceAmount(ResourceType.Lumber,int.MaxValue)});var blocked=new ScavengingService(full,125);blocked.Assign(lumber);blocked.Advance(int.MaxValue);
        C(blocked.FreeWorkers==10 && blocked.Site(lumber).Stored==100 && blocked.Collect(lumber)==ScavengingResult.ResourceLimit,"ledger overflow returns workers and protects undelivered cargo");
        C(blocked.Assign(lumber)==ScavengingResult.PendingDelivery,"pending cargo cannot merge into another trip");
        full.TrySpend(new[]{new ResourceAmount(ResourceType.Lumber,100)});C(blocked.Collect(lumber)==ScavengingResult.Success && blocked.Collect(lumber)==ScavengingResult.NothingToCollect && full.Get(ResourceType.Lumber)==int.MaxValue,"blocked cargo can be delivered exactly once after making room");
        Reject<ArgumentOutOfRangeException>(()=>service.Assign(food,0),"zero-size team rejected");
        Reject<ArgumentOutOfRangeException>(()=>service.Assign(food,6),"oversized team rejected");
        Reject<ArgumentOutOfRangeException>(()=>service.Advance(-1),"negative time rejected");
        Reject<ArgumentException>(()=>service.Assign("missing"),"unknown site rejected");
        Reject<ArgumentOutOfRangeException>(()=>ScavengingService.Generate(0),"invalid map seed rejected");
        bool guarded=false,later=false;
        var observed=new ScavengingService(new ResourceManager(),125);
        observed.Changed+=()=> { try { observed.Recall(food); }catch(InvalidOperationException) { guarded=true; } throw new Exception("broken observer"); };
        observed.Changed+=()=>later=true;
        C(observed.Assign(food)==ScavengingResult.Success && observed.Site(food).Workers==1 && guarded && later && observed.NotificationErrors.Length==1,"observer failures preserve command and later notifications; reentry rejected");
        var deliveredLedger=new ResourceManager();var delivery=new ScavengingService(deliveredLedger,125);delivery.Assign(food);bool coherent=false,reentryBlocked=false,notified=false;
        deliveredLedger.Changed+=()=> { coherent=delivery.FreeWorkers==10 && delivery.Site(food).Stored==0 && deliveredLedger.Get(ResourceType.Food)==100;try{delivery.Assign(food);}catch(InvalidOperationException){reentryBlocked=true;}throw new Exception("delivery observer"); };
        delivery.Changed+=()=>notified=true;delivery.Advance(600);
        C(coherent && reentryBlocked && notified && delivery.NotificationErrors.Length==1,"auto-delivery publishes coherent workers and ledger despite observer failure and blocks reentry");
        var resources=new ResourceManager(new[] { new ResourceAmount(ResourceType.Food,100),new ResourceAmount(ResourceType.Water,50),new ResourceAmount(ResourceType.Scrap,100) });
        var armies=new HeroArmyService(heroes.Select(h=>new HeroData("owned-"+h.Id,h)),fighters.Select(f=>new UnitData(f)),100);
        var fragments=new HeroFragmentWallet(heroes.Select(h=>h.Id)); var buildings=new BuildingService(resources); var scavenging=new ScavengingService(resources,125);
        var progress=new ProgressPersistence(armies,resources,fragments,buildings,scavenging);
        armies.AssignFighters(armies.Heroes[0].InstanceId,"infantry",5); buildings.Upgrade(BuildingKind.Headquarters); buildings.Advance(17); scavenging.Assign(lumber); scavenging.Advance(7);
        var saved=progress.Capture(); string exact=codec.Encode(saved);
        var versionFour=codec.Decode(exact);versionFour.Version=4;versionFour.Resources=versionFour.Resources.Where(r=>r.Type!=ResourceType.Gold).ToArray();progress.Restore(versionFour);
        C(codec.Encode(progress.Capture())==exact,"version-four upgrade preserves exact coordinates, workers, cargo and other domains");
        var invalidLegacy=codec.Decode(exact);invalidLegacy.Version=4;invalidLegacy.Resources=invalidLegacy.Resources.Where(r=>r.Type!=ResourceType.Gold).ToArray();var oversizedOld=invalidLegacy.Scavenging.Sites.Single(n=>n.Id==lumber);oversizedOld.Stored=200;oversizedOld.Remaining=800;oversizedOld.Workers=5;oversizedOld.Work=0;
        Reject<ArgumentException>(()=>progress.Restore(invalidLegacy),"old save retains historical 100-unit storage validation");
        C(codec.Encode(progress.Capture())==exact,"rejected legacy cargo cannot partially change live progression");
        var legacyFull=codec.Decode(exact);legacyFull.Version=4;legacyFull.Resources=legacyFull.Resources.Where(r=>r.Type!=ResourceType.Gold).ToArray();var oldTeam=legacyFull.Scavenging.Sites.Single(n=>n.Id==lumber);oldTeam.Stored=100;oldTeam.Remaining=900;oldTeam.Work=0;
        progress.Restore(legacyFull);scavenging.Advance(0);
        C(resources.Get(ResourceType.Lumber)==100 && scavenging.Site(lumber).Workers==0 && scavenging.Site(lumber).Stored==0,"legacy full cargo returns once even with zero elapsed time");
        scavenging.Advance(5000);C(resources.Get(ResourceType.Lumber)==100,"upgraded completed trip cannot silently restart");progress.Restore(saved);
        C(armies.Heroes[0].Army.TotalUnits==5 && armies.Available("infantry")==95 && resources.Get(ResourceType.Survivors)==0,"dedicated workers do not consume army fighters or survivor currency");
        scavenging.Collect(lumber); progress.Restore(codec.Decode(exact));
        C(codec.Encode(progress.Capture())==exact,"complete map, partial work, allocations and old domains round-trip together");
        var reversed=codec.Decode(exact); Array.Reverse(reversed.Scavenging.Sites); progress.Restore(reversed);
        C(codec.Encode(progress.Capture())==exact,"file order cannot reorder site identities");
        var invalids=new List<Action<ProgressState>> {
            s=>s.Scavenging=null,s=>s.Scavenging.Seed=0,s=>s.Scavenging.Sites=null,s=>s.Scavenging.Sites=new ScavengingSiteState[0],
            s=>s.Scavenging.Sites[0]=null,s=>s.Scavenging.Sites[1]=s.Scavenging.Sites[0],s=>s.Scavenging.Sites[0].Id="wrong",
            s=>s.Scavenging.Sites[0].X++,s=>s.Scavenging.Sites[0].Resource=ResourceType.Fuel,s=>s.Scavenging.Sites[0].Remaining=-1,
            s=>s.Scavenging.Sites[0].Stored=-1,s=>s.Scavenging.Sites[0].Stored=501,s=>s.Scavenging.Sites[0].Stored=1,
            s=>s.Scavenging.Sites[0].Workers=6,s=>s.Scavenging.Sites[0].Work=-1,s=>s.Scavenging.Sites[0].Work=60,
            s=> { s.Scavenging.Sites[0].Remaining=900;s.Scavenging.Sites[0].Stored=100;s.Scavenging.Sites[0].Work=1; },
            s=> { foreach(var n in s.Scavenging.Sites)n.Workers=1; }
        };
        foreach(var corrupt in invalids) {
            var candidate=codec.Decode(exact); corrupt(candidate);
            Reject<ArgumentException>(()=>progress.Restore(candidate),"malformed map rejected");
            C(codec.Encode(progress.Capture())==exact,"invalid map cannot partially restore any domain");
        }
        foreach(int version in new[] { 1,2 }) {
            var legacy=codec.Decode(exact); legacy.Version=version; legacy.Scavenging=null; legacy.Resources=legacy.Resources.Where(r=>r.Type!=ResourceType.Lumber && r.Type!=ResourceType.Gold).ToArray(); if(version==1)legacy.Buildings=null;
            progress.Restore(legacy);
            C(resources.Get(ResourceType.Lumber)==0 && scavenging.FreeWorkers==10 && scavenging.Capture().Sites.All(s=>s.Remaining==ScavengingService.Generate(scavenging.Seed).Sites.Single(original=>original.Id==s.Id).Remaining),"legacy migration adds empty Lumber ledger and unassigned random map");
            C(armies.Heroes[0].Army.TotalUnits==5 && resources.Get(ResourceType.Scrap)==60 && buildings.Level(BuildingKind.Headquarters)==(version==1 ? 1 : 2),"legacy migration preserves armies and existing compatible building/resource progression");
        }
        string directory=Path.Combine(Path.GetTempPath(),"ash-scavenging-"+Guid.NewGuid().ToString("N"));
        try {
            progress.Restore(saved); var store=new LocalSaveStore(directory,codec,progress.Validate); store.Save(saved);
            scavenging.Recall(lumber); scavenging.Advance(600); progress.Restore(store.Load().State);
            C(codec.Encode(progress.Capture())==exact,"disk restart retains exact random locations, stock and assignments");
            scavenging.Advance(53); scavenging.Recall(lumber); store.Save(progress.Capture());
            C(resources.Get(ResourceType.Lumber)==8 && store.Load().State.Resources.Single(r=>r.Type==ResourceType.Lumber).Amount==8,"Lumber gathering reaches persistent shared ledger");
            var reset=progress.Capture(); reset.Scavenging=ScavengingService.Generate(991); store.Reset(reset); progress.Restore(store.Load().State);
            C(scavenging.Seed==991 && scavenging.FreeWorkers==10 && scavenging.Capture().Sites.All(s=>s.Stored==0) && !File.Exists(store.BackupPath),"explicit reset creates new unallocated map without old backup");
        } finally { if(Directory.Exists(directory))Directory.Delete(directory,true); }
        return passed;
    }
}
