using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Ash;

internal static class OfflineChecks
{
    private sealed class Codec : IProgressCodec
    {
        private static readonly JsonSerializerOptions Options=new JsonSerializerOptions { IncludeFields=true };
        public string Encode(ProgressState state)=>JsonSerializer.Serialize(state,Options);
        public ProgressState Decode(string json)=>ProgressJsonDecoder.Decode(json,(s,t)=>JsonSerializer.Deserialize(s,t,Options));
    }
    private sealed class Context
    {
        public readonly ResourceManager Resources=new ResourceManager();
        public readonly ScavengingService Sites;
        public readonly OfflineScavenging Clock;
        public readonly ProgressPersistence Progress;
        public Context(HeroDefinition[] heroes,UnitDefinition[] units)
        {
            Sites=new ScavengingService(Resources,973); Clock=new OfflineScavenging(Sites);
            var armies=new HeroArmyService(heroes.Select(h=>new HeroData("owned-"+h.Id,h)),units.Select(u=>new UnitData(u)),100);
            Progress=new ProgressPersistence(armies,Resources,new HeroFragmentWallet(heroes.Select(h=>h.Id)),new BuildingService(Resources),Sites,Clock);
        }
    }
    internal static int Run(HeroDefinition[] heroes,UnitDefinition[] units)
    {
        int passed=0; var codec=new Codec();
        void C(bool value,string name) { if(!value)throw new Exception("FAIL offline "+name); passed++; Console.WriteLine("PASS offline "+name); }
        void Reject<T>(Action action,string name) where T:Exception {
            try { action(); }catch(T) { C(true,name); return; } throw new Exception("FAIL expected "+typeof(T).Name+": "+name);
        }
        var c=new Context(heroes,units); string lumber=c.Sites.Capture().Sites.First(n=>n.Resource==ResourceType.Lumber).Id;
        c.Sites.Assign(lumber);
        C(c.Clock.Resume(1000).Seconds==0 && c.Sites.Site(lumber).Stored==0,"first timestamp grants no pre-install catch-up");
        var report=c.Clock.Resume(1060);
        C(report.Seconds==60 && report.Gathered.Single().Amount==8 && report.Summary.Contains("Lumber"),"one worker gathers exact minute and gets summary");
        C(c.Resources.Get(ResourceType.Lumber)==0 && c.Sites.Site(lumber).Stored==8,"partial offline cargo stays with the team until return");
        C(c.Clock.Resume(1060).Seconds==0 && c.Sites.Site(lumber).Stored==8,"same timestamp cannot pay twice");
        c.Sites.Advance(60); c.Clock.Checkpoint(1120); c.Clock.Resume(1180);
        C(c.Sites.Site(lumber).Stored==24,"foreground checkpoint prevents active seconds being credited offline again");
        c.Clock.Resume(1100);
        C(c.Clock.LastUtcSeconds==1180 && c.Sites.Site(lumber).Stored==24,"clock rollback never lowers watermark or gives resources");
        c.Clock.Resume(1190);
        C(c.Sites.Site(lumber).Stored==25 && c.Sites.Site(lumber).Work==20,"partial offline work preserved with integer accumulation");
        c.Sites.Recall(lumber);c.Clock.Resume(1250);
        C(c.Sites.Site(lumber).Stored==0 && c.Resources.Get(ResourceType.Lumber)==25 && c.Clock.LastUtcSeconds==1250,"recalled workers deliver partial load and wait idle offline");
        c.Sites.Assign(lumber);c.Clock.Resume(1256);
        C(c.Sites.Site(lumber).Stored==0 && c.Sites.Site(lumber).Work==48,"resend cannot claim previous unassigned time");
        var returned=c.Clock.Resume(1000000);
        C(c.Sites.Site(lumber).Stored==0 && c.Sites.FreeWorkers==10 && c.Resources.Get(ResourceType.Lumber)==125 && c.Clock.LastUtcSeconds==1000000 && returned.Delivered.Single().Amount==100 && returned.ReturnedWorkers==1,"long absence auto-delivers one full load and reports returned worker");
        C(c.Clock.Resume(1000000).Seconds==0,"same offline interval cannot pay again");
        c.Clock.Resume(1000006);
        C(c.Sites.Site(lumber).Stored==0 && c.Sites.Site(lumber).Work==0 && c.Resources.Get(ResourceType.Lumber)==125,"returned workers do not gather again on another resume");
        var multi=new Context(heroes,units); string water=multi.Sites.Capture().Sites.First(n=>n.Resource==ResourceType.Water).Id;
        multi.Sites.Assign(lumber,5);multi.Sites.Assign(water,2);
        multi.Clock.Checkpoint(1000); var result=multi.Clock.Resume(1060);
        C(multi.Sites.Site(lumber).Stored==40 && multi.Sites.Site(water).Stored==16 && result.Gathered.Sum(a=>a.Amount)==56,"all assigned sites catch up at composition-specific worker rates");
        C(multi.Clock.Resume(1000000000).Seconds==OfflineScavenging.MaximumSeconds,"offline interval bounded to eight hours");
        C(multi.Sites.Site(water).Stored==0 && multi.Sites.Site(lumber).Stored==0 && multi.Resources.Get(ResourceType.Water)==200 && multi.Resources.Get(ResourceType.Lumber)==500 && multi.Sites.FreeWorkers==10,"offline teams return once at combined carrying capacity");
        var finite=new Context(heroes,units); var map=finite.Sites.Capture(); map.Sites.Single(n=>n.Id==lumber).Remaining=3;
        finite.Sites.Validate(map); finite.Sites.RestoreValidated(map); finite.Sites.Assign(lumber); finite.Clock.Checkpoint(1000); finite.Clock.Resume(1060);
        C(finite.Sites.Site(lumber).Stored==0 && finite.Sites.Site(lumber).Remaining==0 && finite.Resources.Get(ResourceType.Lumber)==3 && finite.Sites.FreeWorkers==10,"offline gathering respects deposit remaining");
        finite.Clock.Resume(1120);
        C(finite.Resources.Get(ResourceType.Lumber)==3,"exhausted deposit cannot produce again");
        Reject<ArgumentOutOfRangeException>(()=>c.Clock.Resume(-1),"negative time rejected");
        Reject<ArgumentOutOfRangeException>(()=>c.Clock.Resume(0),"invalid epoch-zero clock rejected");
        Reject<ArgumentOutOfRangeException>(()=>c.Clock.Checkpoint(OfflineScavenging.MaximumUtcSeconds+1),"impossible timestamp rejected");
        var observed=new Context(heroes,units); observed.Sites.Assign(lumber); observed.Clock.Checkpoint(1000); bool atomic=false,guarded=false;
        observed.Sites.Changed+=()=> { atomic=observed.Clock.LastUtcSeconds==1060 && observed.Sites.Site(lumber).Stored==8; try { observed.Clock.Resume(1060); }catch(InvalidOperationException) { guarded=true; } };
        observed.Clock.Resume(1060);
        C(atomic && guarded,"observers see completed clock/site state; catch-up reentry rejected");
        var saved=c.Progress.Capture(); string exact=codec.Encode(saved); var next=new Context(heroes,units); next.Progress.Restore(codec.Decode(exact));
        C(codec.Encode(next.Progress.Capture())==exact,"timestamp and every existing domain restore together");
        C(next.Clock.Resume(c.Clock.LastUtcSeconds).Seconds==0,"fresh model restart cannot replay committed interval");
        next.Clock.Resume(c.Clock.LastUtcSeconds+5000);
        C(next.Sites.AssignedWorkers==0 && next.Resources.Get(ResourceType.Lumber)==125,"completed trip remains idle after save/load and later offline time");
        next.Progress.Restore(codec.Decode(exact));
        foreach(long invalid in new[] { -1L,OfflineScavenging.MaximumUtcSeconds+1 }) {
            var bad=codec.Decode(exact); bad.ScavengingUtcSeconds=invalid;
            Reject<ArgumentOutOfRangeException>(()=>next.Progress.Restore(bad),"bad saved timestamp rejected before restore");
            C(codec.Encode(next.Progress.Capture())==exact,"bad timestamp leaves all live domains unchanged");
        }
        var old=codec.Decode(exact); old.Version=3; old.ScavengingUtcSeconds=0;old.Scavenging.Sites.Single(n=>n.Id==lumber).Workers=1;
        next.Progress.Restore(old); int stock=next.Sites.Site(lumber).Stored;
        C(next.Clock.Resume(2000000).Seconds==0 && next.Sites.Site(lumber).Stored==stock && next.Sites.Site(lumber).Workers==1,"v3 migration retains world/workers without retroactive offline credit");
        string directory=Path.Combine(Path.GetTempPath(),"ash-offline-"+Guid.NewGuid().ToString("N"));
        try {
            var source=new Context(heroes,units); source.Sites.Assign(lumber); source.Clock.Checkpoint(1000);
            var store=new LocalSaveStore(directory,codec,source.Progress.Validate); store.Save(source.Progress.Capture());
            var restart=new Context(heroes,units); restart.Progress.Restore(store.Load().State); restart.Clock.Resume(1060); store.Save(restart.Progress.Capture());
            var again=new Context(heroes,units); again.Progress.Restore(store.Load().State); again.Clock.Resume(1060);
            C(again.Sites.Site(lumber).Stored==8 && again.Clock.LastUtcSeconds==1060,"disk restart commits one offline payout and watermark");
            again.Sites.Recall(lumber);again.Sites.Assign(lumber); store.Save(again.Progress.Capture());
            var afterCollect=new Context(heroes,units); afterCollect.Progress.Restore(store.Load().State); afterCollect.Clock.Resume(1060);
            C(afterCollect.Resources.Get(ResourceType.Lumber)==8 && afterCollect.Sites.Site(lumber).Stored==0,"collect/save/restart does not duplicate offline output");
            byte[] prior=File.ReadAllBytes(store.SavePath); afterCollect.Clock.Resume(1120); Directory.CreateDirectory(store.SavePath+".tmp");
            bool failed=false;
            try { store.Save(afterCollect.Progress.Capture()); } catch(Exception error) when(error is IOException || error is UnauthorizedAccessException) { failed=true; }
            C(failed,"failed checkpoint preserves previous disk state");
            Directory.Delete(store.SavePath+".tmp");
            C(File.ReadAllBytes(store.SavePath).SequenceEqual(prior),"failed offline save leaves old payload intact");
            var recovered=new Context(heroes,units); recovered.Progress.Restore(store.Load().State); recovered.Clock.Resume(1120);
            C(recovered.Resources.Get(ResourceType.Lumber)==8 && recovered.Sites.Site(lumber).Stored==8,"recovery recalculates from old snapshot without double credit");
        } finally { if(Directory.Exists(directory))Directory.Delete(directory,true); }
        return passed;
    }
}
