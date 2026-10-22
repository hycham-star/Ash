using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ash;

internal static class PersistenceChecks
{
    private sealed class Codec : IProgressCodec
    {
        private static readonly JsonSerializerOptions Options=new JsonSerializerOptions { IncludeFields=true };
        public string Encode(ProgressState state)=>JsonSerializer.Serialize(state,Options);
        public ProgressState Decode(string json)
        {
            try { return JsonSerializer.Deserialize<ProgressState>(json,Options); }
            catch(JsonException error) { throw new ArgumentException("Invalid progress JSON.",error); }
        }
    }
    private sealed class Context
    {
        public HeroArmyService Armies;
        public ResourceManager Resources;
        public HeroFragmentWallet Fragments;
        public BattleManager Battles;
        public ProgressPersistence Progress;
        public HeroData Mara=>Armies.Heroes.Single(h=>h.Definition.Id=="mara-flint");
        public Context(HeroDefinition[] heroes,UnitDefinition[] fighters)
        {
            Armies=new HeroArmyService(heroes.Select(h=>new HeroData("owned-"+h.Id,h)),fighters.Select(f=>new UnitData(f)),100);
            Resources=new ResourceManager(new[] { new ResourceAmount(ResourceType.Food,100),new ResourceAmount(ResourceType.Scrap,100),new ResourceAmount(ResourceType.Water,50),new ResourceAmount(ResourceType.Fuel,20),new ResourceAmount(ResourceType.Survivors,5) });
            Fragments=new HeroFragmentWallet(heroes.Select(h=>h.Id)); Battles=new BattleManager(Armies,Resources,Fragments);
            Progress=new ProgressPersistence(Armies,Resources,Fragments);
        }
    }
    internal static int Run(HeroDefinition[] heroes,UnitDefinition[] fighters)
    {
        int passed=0; var codec=new Codec();
        void C(bool condition,string name) { if(!condition)throw new Exception("FAIL "+name); passed++; Console.WriteLine("PASS save "+name); }
        void Reject<T>(Action action,string name) where T:Exception
        { try { action(); }catch(T) { C(true,name); return; } throw new Exception("FAIL expected "+typeof(T).Name+": "+name); }
        ProgressState Copy(ProgressState state)=>codec.Decode(codec.Encode(state));
        string Snapshot(Context context)=>codec.Encode(context.Progress.Capture());
        void Raw(string path,ProgressState state,string header="ASHLANDS_LOCAL_SAVE_1")
        {
            string json=codec.Encode(state), hash;
            using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(json))).Replace("-","");
            File.WriteAllText(path,header+"\n"+hash+"\n"+json,new UTF8Encoding(false));
        }
        var source=new Context(heroes,fighters); var fresh=source.Progress.Capture();
        foreach(var fighter in source.Armies.Fighters)source.Armies.AssignFighters(source.Mara.InstanceId,fighter.Definition.Id,10);
        var other=source.Armies.Heroes.First(h=>h.InstanceId!=source.Mara.InstanceId);
        source.Armies.AssignFighters(other.InstanceId,"infantry",7);
        var patrol=AssetFixtures.Encounter(Path.Combine(AssetFixtures.Root,"Assets/Ash/Data/Encounters/RaiderPatrol.asset"));
        source.Battles.RunBattle(source.Mara.InstanceId,patrol);
        var captured=source.Progress.Capture();
        C(captured.Version==ProgressPersistence.Version && captured.Resources.Length==7 && captured.Armies.Heroes.Length==8 && captured.Fragments.Length==8,"capture includes all domains and schema");
        var target=new Context(heroes,fighters); var originalService=target.Armies; var originalHero=target.Mara; var originalArmy=target.Mara.Army;
        target.Progress.Restore(codec.Decode(codec.Encode(captured)));
        C(Snapshot(target)==codec.Encode(captured),"complete save/restore JSON round trip");
        C(ReferenceEquals(target.Armies,originalService) && ReferenceEquals(target.Mara,originalHero) && ReferenceEquals(target.Mara.Army,originalArmy),"in-place restore preserves service, hero, and army references");
        C(target.Mara.Level==2 && target.Mara.Xp==25 && target.Fragments.Get("mara-flint")==2 && target.Resources.Get(ResourceType.Scrap)==140,"raid XP, resources, and fragments restored");
        C(target.Armies.FindHero(other.InstanceId).Army.TotalUnits==7 && target.Mara.Army.TotalUnits==source.Mara.Army.TotalUnits,"independent armies and casualties restored");
        C(target.Armies.Available("infantry")==source.Armies.Available("infantry"),"unassigned fighter stock restored");
        C(target.Armies.RemoveFighters(target.Mara.InstanceId,"infantry",1)==ArmyCommandResult.Success,"restored army still supports normal commands");
        C(target.Battles.PreviewBattle(target.Mara.InstanceId,patrol)!=null,"restored game can preview another battle");
        var detached=source.Progress.Capture(); detached.Fragments[0].Quantity=999; detached.Resources[0].Amount=999; detached.Armies.Heroes[0].Xp=999;
        C(Snapshot(source)==codec.Encode(captured),"captured snapshot is detached from live state");
        var shuffled=Copy(captured); Array.Reverse(shuffled.Armies.Heroes); Array.Reverse(shuffled.Armies.Reserve); Array.Reverse(shuffled.Resources); Array.Reverse(shuffled.Fragments);
        target.Progress.Restore(shuffled);
        C(Snapshot(target)==codec.Encode(captured),"file array order cannot reorder the live roster");
        var invalids=new List<(string,Action<ProgressState>)> {
            ("missing format",s=>s.Format=null), ("missing version",s=>s.Version=0),
            ("missing heroes",s=>s.Armies.Heroes=null), ("incomplete roster",s=>s.Armies.Heroes=s.Armies.Heroes.Skip(1).ToArray()),
            ("unknown hero",s=>s.Armies.Heroes[0].InstanceId="unknown"), ("wrong definition",s=>s.Armies.Heroes[0].DefinitionId="unknown"),
            ("wrong army ownership",s=>s.Armies.Heroes[0].ArmyId="another-army"), ("duplicate hero",s=>s.Armies.Heroes[1]=s.Armies.Heroes[0]),
            ("invalid level",s=>s.Armies.Heroes[0].Level=9999), ("negative XP",s=>s.Armies.Heroes[0].Xp=-1),
            ("unnormalized XP",s=>s.Armies.Heroes[0].Xp=int.MaxValue), ("missing reserve",s=>s.Armies.Reserve=null),
            ("negative reserve",s=>s.Armies.Reserve[0].Quantity=-1), ("unknown reserve",s=>s.Armies.Reserve[0].DefinitionId="unknown"),
            ("duplicate reserve",s=>s.Armies.Reserve[1]=s.Armies.Reserve[0]), ("unsupported fighter level",s=>s.Armies.Reserve[0].Level=2),
            ("negative resource",s=>s.Resources[0].Amount=-1), ("duplicate resource",s=>s.Resources[1]=s.Resources[0]),
            ("unknown resource",s=>s.Resources[0].Type=(ResourceType)999), ("missing resource",s=>s.Resources=s.Resources.Skip(1).ToArray()),
            ("negative fragments",s=>s.Fragments[0].Quantity=-1), ("unknown fragments",s=>s.Fragments[0].DefinitionId="unknown"),
            ("duplicate fragments",s=>s.Fragments[1]=s.Fragments[0]), ("missing fragment ledger",s=>s.Fragments=null),
            ("over-capacity army",s=>s.Armies.Heroes.Single(h=>h.DefinitionId=="mara-flint").Fighters[0].Quantity=999),
            ("negative army quantity",s=>s.Armies.Heroes.Single(h=>h.DefinitionId=="mara-flint").Fighters[0].Quantity=-1),
            ("unknown army fighter",s=>s.Armies.Heroes.Single(h=>h.DefinitionId=="mara-flint").Fighters[0].DefinitionId="unknown"),
            ("duplicate stack",s=> { var h=s.Armies.Heroes.Single(h=>h.DefinitionId=="mara-flint"); h.Fighters=h.Fighters.Concat(new[] { h.Fighters[0] }).ToArray(); }),
            ("reserve plus army overflow",s=>s.Armies.Reserve.Single(f=>f.DefinitionId=="infantry").Quantity=int.MaxValue)
        };
        foreach(var invalid in invalids)
        {
            var state=Copy(captured); invalid.Item2(state); string before=Snapshot(target);
            Reject<ArgumentException>(()=>target.Progress.Restore(state),"reject "+invalid.Item1);
            C(Snapshot(target)==before,"no partial live changes for "+invalid.Item1);
        }
        var future=Copy(captured); future.Version=ProgressPersistence.Version+1;
        Reject<UnsupportedSaveVersionException>(()=>target.Progress.Restore(future),"future schema protected");
        target.Progress.Restore(captured);
        var observed=false; target.Armies.Changed+=()=>observed=target.Resources.Get(ResourceType.Scrap)==140 && target.Fragments.Get("mara-flint")==2;
        C(target.Progress.NotifyRestored().Length==0 && observed,"observers see fully restored ledgers");

        string directory=Path.Combine(Path.GetTempPath(),"ash-save-tests-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var store=new LocalSaveStore(directory,codec,source.Progress.Validate);
            C(store.Load().Status==ProgressLoadStatus.Missing,"new install has no save");
            store.Save(fresh); C(store.Load().Status==ProgressLoadStatus.Primary,"first save writes a readable primary");
            store.Save(captured); C(File.Exists(store.BackupPath),"second save keeps previous-good backup");
            C(codec.Encode(store.Load().State)==codec.Encode(captured),"primary contains latest complete progress");
            string primary=File.ReadAllText(store.SavePath), backup=File.ReadAllText(store.BackupPath);
            var bad=Copy(captured); bad.Resources[0].Amount=-5;
            Reject<ArgumentException>(()=>store.Save(bad),"invalid new snapshot cannot be saved");
            C(File.ReadAllText(store.SavePath)==primary && File.ReadAllText(store.BackupPath)==backup,"rejected save leaves both files intact");
            Directory.CreateDirectory(store.SavePath+".tmp");
            Reject<UnauthorizedAccessException>(()=>store.Save(captured),"temp write failure surfaced");
            C(File.ReadAllText(store.SavePath)==primary,"failed write preserves last good primary"); Directory.Delete(store.SavePath+".tmp");
            File.WriteAllText(store.SavePath,"truncated"); var recovered=store.Load();
            C(recovered.Status==ProgressLoadStatus.Backup && codec.Encode(recovered.State)==codec.Encode(fresh),"damaged primary falls back to last good backup");
            store.Save(captured);
            C(File.ReadAllText(store.BackupPath)==backup,"saving after recovery never backs up corrupt primary over good backup");
            Raw(store.SavePath,bad);
            C(store.Load().Status==ProgressLoadStatus.Backup,"checksum-valid but invalid state also falls back");
            Raw(store.SavePath,future); string futureBytes=File.ReadAllText(store.SavePath);
            C(store.Load().Status==ProgressLoadStatus.Unsupported,"future schema does not silently load an older backup");
            Reject<UnsupportedSaveVersionException>(()=>store.Save(captured),"future primary cannot be overwritten by autosave");
            C(File.ReadAllText(store.SavePath)==futureBytes,"future save bytes preserved");
            store.Reset(fresh);
            C(store.Load().Status==ProgressLoadStatus.Primary && !File.Exists(store.BackupPath),"explicit reset writes fresh progress and removes old backup");
            File.WriteAllText(store.SavePath,"bad reset primary");
            C(store.Load().Status==ProgressLoadStatus.Invalid,"reset cannot resurrect pre-reset progress from backup");
            C(File.ReadAllText(store.SavePath)=="bad reset primary","loading invalid files never overwrites them");
            store.Reset(fresh); Raw(store.SavePath,future,"ASHLANDS_LOCAL_SAVE_2");
            C(store.Load().Status==ProgressLoadStatus.Unsupported,"newer envelope format protected");
            store.Reset(fresh); File.WriteAllText(store.SavePath,"ASHLANDS_LOCAL_SAVE_1\nBADHASH\n{}");
            C(store.Load().Status==ProgressLoadStatus.Invalid,"checksum mismatch rejected");
            store.Reset(fresh); File.WriteAllBytes(store.SavePath,new byte[] { 0xff,0xfe,0xff });
            C(store.Load().Status==ProgressLoadStatus.Invalid,"invalid UTF-8 rejected");
            store.Reset(fresh); store.Save(captured); File.Delete(store.SavePath);
            C(store.Load().Status==ProgressLoadStatus.Backup,"missing primary recovers backup");
            File.WriteAllText(store.SavePath+".tmp","interrupted write");
            C(store.Load().Status==ProgressLoadStatus.Backup,"uncommitted temporary file is ignored");
            File.WriteAllText(store.BackupPath,"corrupt backup");
            C(store.Load().Status==ProgressLoadStatus.Invalid,"two unusable slots report corruption without inventing progress");
            store.Reset(captured); var restarted=new Context(heroes,fighters); var newStore=new LocalSaveStore(directory,codec,restarted.Progress.Validate);
            restarted.Progress.Restore(newStore.Load().State);
            C(Snapshot(restarted)==codec.Encode(captured),"new session restores battle rewards, all armies, reserves, and resources from disk");
            restarted.Battles.RunBattle(restarted.Mara.InstanceId,AssetFixtures.Encounter(Path.Combine(AssetFixtures.Root,"Assets/Ash/Data/Encounters/StrayRaiders.asset")));
            C(restarted.Fragments.Get("mara-flint")>=2,"restored session can settle another real battle");
        }
        finally { Directory.Delete(directory,true); }
        return passed;
    }
}
