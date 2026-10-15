using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Ash;

internal static class MigrationCodecChecks
{
    private static readonly JsonSerializerOptions Options=new JsonSerializerOptions { IncludeFields=true };
    private sealed class Codec : IProgressCodec
    {
        public string Encode(ProgressState state)=>JsonSerializer.Serialize(state,Options);
        public ProgressState Decode(string json)=>ProgressJsonDecoder.Decode(json,UnityLikeRead);
    }
    // Explicitly emulate Unity's missing inline-class behavior; this is not a native Unity test.
    private static object UnityLikeRead(string json,Type type)
    {
        var value=JsonSerializer.Deserialize(json,type,Options);
        if(value is ProgressState state && state.Version<3) {
            state.Scavenging ??= new ScavengingState { Sites=Array.Empty<ScavengingSiteState>() };
            if(state.Version==1)state.Buildings ??= Array.Empty<BuildingState>();
        }
        return value;
    }
    private sealed class Context
    {
        public readonly ResourceManager Resources=new ResourceManager();
        public readonly HeroArmyService Armies;
        public readonly HeroFragmentWallet Fragments;
        public readonly BuildingService Buildings;
        public readonly ScavengingService Scavenging;
        public readonly ProgressPersistence Progress;
        public Context(HeroDefinition[] heroes,UnitDefinition[] units)
        {
            Armies=new HeroArmyService(heroes.Select(h=>new HeroData("owned-"+h.Id,h)),units.Select(u=>new UnitData(u)),0);
            Fragments=new HeroFragmentWallet(heroes.Select(h=>h.Id)); Buildings=new BuildingService(Resources);
            Scavenging=new ScavengingService(Resources,973); Progress=new ProgressPersistence(Armies,Resources,Fragments,Buildings,Scavenging);
        }
    }
    internal static int Run(HeroDefinition[] heroes,UnitDefinition[] units)
    {
        int passed=0; var codec=new Codec(); var context=new Context(heroes,units);
        void C(bool value,string name) { if(!value)throw new Exception("FAIL migration codec "+name); passed++; Console.WriteLine("PASS migration codec "+name); }
        void Reject(Action action,string name) { try { action(); }catch(ArgumentException) { C(true,name); return; } throw new Exception("FAIL "+name); }
        var current=context.Progress.Capture();
        foreach(int version in new[] { 1,2 }) {
            var root=JsonNode.Parse(codec.Encode(current)); root["Version"]=version; root.AsObject().Remove("Scavenging");
            if(version==1)root.AsObject().Remove("Buildings");
            root["Resources"]=JsonSerializer.SerializeToNode(current.Resources.Where(r=>r.Type!=ResourceType.Lumber).ToArray(),Options);
            string json=root.ToJsonString();
            var before=(ProgressState)UnityLikeRead(json,typeof(ProgressState));
            Reject(()=>context.Progress.Validate(before),"reproduce legacy rejection with emulated Unity inline defaults");
            var migrated=codec.Decode(json);
            C(migrated.Scavenging==null && (version!=1 || migrated.Buildings==null),"legacy decode excludes fields outside its declared schema");
            context.Progress.Restore(migrated);
            C(context.Progress.Capture().Version==ProgressPersistence.Version && context.Scavenging.FreeWorkers==10 && context.Resources.Get(ResourceType.Lumber)==0,"legacy upgrade creates complete valid current snapshot");
        }
        string exact=codec.Encode(current);
        C(codec.Encode(codec.Decode(exact))==exact,"current schema round trip retains every field");
        var malformed=codec.Decode(exact); malformed.Scavenging=null;
        Reject(()=>context.Progress.Validate(codec.Decode(codec.Encode(malformed))),"missing current map is not repaired or silently reset");
        malformed=codec.Decode(exact); malformed.Scavenging.Sites[0].Workers=99;
        Reject(()=>context.Progress.Validate(codec.Decode(codec.Encode(malformed))),"corrupt current workers remain invalid");
        malformed=codec.Decode(exact); malformed.Version=ProgressPersistence.Version+1; bool unsupported=false;
        try { context.Progress.Validate(codec.Decode(codec.Encode(malformed))); }catch(UnsupportedSaveVersionException) { unsupported=true; }
        C(unsupported,"future versions retain overwrite protection");
        return passed;
    }
    internal static int ValidateFile(string path,HeroDefinition[] heroes,UnitDefinition[] units)
    {
        var bytes=File.ReadAllBytes(path); var context=new Context(heroes,units); var codec=new Codec();
        var store=new LocalSaveStore(Path.GetDirectoryName(path),codec,context.Progress.Validate);
        var loaded=store.Load(); if(!loaded.Loaded)throw new Exception("Uploaded save rejected: "+loaded.Detail);
        var old=loaded.State; context.Progress.Restore(old);
        bool intact=old.Resources.All(r=>context.Resources.Get(r.Type)==r.Amount)
            && old.Armies.Heroes.All(h=> { var live=context.Armies.FindHero(h.InstanceId); return live.Level==h.Level && live.Xp==h.Xp && live.Army.TotalUnits==h.Fighters.Sum(f=>f.Quantity); })
            && old.Armies.Reserve.All(r=>context.Armies.Available(r.DefinitionId)==r.Quantity)
            && old.Fragments.All(f=>context.Fragments.Get(f.DefinitionId)==f.Quantity)
            && old.Buildings.All(b=>context.Buildings.Level(b.Kind)==b.Level && context.Buildings.Stored(b.Kind)==b.Stored && context.Buildings.Seconds(b.Kind)==b.Seconds);
        if(!intact || !File.ReadAllBytes(path).SequenceEqual(bytes))throw new Exception("Uploaded save migration lost progress or modified original.");
        string temporary=Path.Combine(Path.GetTempPath(),"ash-user-migration-"+Guid.NewGuid().ToString("N"));
        try {
            var upgraded=new LocalSaveStore(temporary,codec,context.Progress.Validate); upgraded.Save(context.Progress.Capture());
            var node=context.Scavenging.Capture().Sites.First(s=>s.Resource==ResourceType.Lumber);
            context.Scavenging.Assign(node.Id); upgraded.Save(context.Progress.Capture());
            var restart=new Context(heroes,units); restart.Progress.Restore(upgraded.Load().State);
            if(restart.Scavenging.Seed!=context.Scavenging.Seed || restart.Scavenging.Site(node.Id).Workers!=1)throw new Exception("Migrated map restart lost allocation.");
        } finally { if(Directory.Exists(temporary))Directory.Delete(temporary,true); }
        Console.WriteLine("PASS uploaded save "+Path.GetFileName(path)+": checksum/load, complete legacy progress, current-schema write and worker/map restart; original untouched (emulated Unity defaults, .NET JSON).");
        return 1;
    }
}
