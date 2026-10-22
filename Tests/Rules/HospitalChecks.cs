using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Ash;

internal static class HospitalChecks
{
    private sealed class Codec:IProgressCodec
    {
        private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
        public string Encode(ProgressState state)=>JsonSerializer.Serialize(state,Options);
        public ProgressState Decode(string json)=>ProgressJsonDecoder.Decode(json,(s,t)=>JsonSerializer.Deserialize(s,t,Options));
    }
    private sealed class Context
    {
        public readonly ResourceManager Resources;
        public readonly HeroArmyService Armies;public readonly BattleManager Battles;public readonly HospitalService Hospital;
        public readonly ProgressPersistence Progress;public readonly OfflineScavenging Clock;public readonly ScavengingService Sites;
        public HeroData Hero=>Armies.Heroes.First(h=>h.Definition.Id=="mara-flint");
        public Context(HeroDefinition[] heroes,UnitDefinition[] fighters,int food=10000,int water=10000)
        {
            Resources=new ResourceManager(new[]{new ResourceAmount(ResourceType.Food,food),new ResourceAmount(ResourceType.Water,water),new ResourceAmount(ResourceType.Scrap,1000000)});
            Armies=new HeroArmyService(heroes.Select(h=>new HeroData("owned-"+h.Id,h)),fighters.Select(f=>new UnitData(f)),100);
            Battles=new BattleManager(Armies,Resources,new HeroFragmentWallet(heroes.Select(h=>h.Id)));Hospital=Battles.Hospital;
            Sites=new ScavengingService(Resources,125);Clock=new OfflineScavenging(Sites);
            Progress=new ProgressPersistence(Armies,Resources,new HeroFragmentWallet(heroes.Select(h=>h.Id)),new BuildingService(Resources),Sites,Clock);
        }
        public NpcEncounter Strong()=>new NpcEncounter("strong-test","Strong camp",new BattleArmy("strong-test",Armies.Fighters.Select(f=>new BattleFighter(f,1000))),0,Array.Empty<ResourceAmount>(),0);
        public CombatResult Wound(int count=30)
        {
            var unit=Armies.Fighters[0];if(Armies.AssignFighters(Hero.InstanceId,unit.Definition.Id,count)!=ArmyCommandResult.Success)throw new Exception("Hospital battle fixture assignment failed");
            return Battles.RunBattle(Hero.InstanceId,Strong());
        }
    }
    internal static int Run(HeroDefinition[] heroes,UnitDefinition[] fighters)
    {
        int passed=0;var codec=new Codec();
        void C(bool condition,string name){if(!condition)throw new Exception("FAIL hospital "+name);passed++;Console.WriteLine("PASS hospital "+name);}
        void Reject<T>(Action action,string name)where T:Exception{try{action();}catch(T){C(true,name);return;}throw new Exception("FAIL expected "+typeof(T).Name+": "+name);}
        string Json(Context c)=>codec.Encode(c.Progress.Capture());
        var c=new Context(heroes,fighters);string id=c.Armies.Fighters[0].Definition.Id;
        C(c.Hospital.Level==1 && c.Hospital.Beds==20 && c.Hospital.TotalWounded==0,"initial hospital has twenty beds and no invented patients");
        C(ReferenceEquals(c.Hospital,c.Armies.GetHospital(c.Resources)),"battles and persistence share one hospital per army ledger");
        Reject<InvalidOperationException>(()=>c.Armies.GetHospital(new ResourceManager()),"hospital cannot bind to a different resource ledger");
        var result=c.Wound();int wounded=result.PlayerFighters.Sum(f=>f.Casualties);
        C(result.Outcome==BattleOutcome.Defeat && wounded==30 && c.Hospital.Waiting==30 && c.Hero.Army.TotalUnits==0,"defeat transfers every casualty to wounded, never permanent loss");
        C(c.Armies.Available(id)==70 && c.Hero.Level==1,"heroes remain available and wounded do not enter reserve before healing");
        C(c.Armies.Available(id)+c.Hospital.TotalWounded+c.Hero.Army.TotalUnits==100,"fighters conserved across reserve, army and hospital");
        c.Armies.AssignFighters(c.Hero.InstanceId,id,1);var before=Json(c);c.Battles.PreviewBattle(c.Hero.InstanceId,c.Strong());C(Json(c)==before,"preview changes no patients, fighters or resources");c.Armies.RemoveFighters(c.Hero.InstanceId,id,1);
        int food=c.Resources.Get(ResourceType.Food),water=c.Resources.Get(ResourceType.Water);
        C(c.Hospital.Treat()==HospitalCommandResult.Success && c.Hospital.Waiting==0 && c.Hospital.InBeds==20 && c.Hospital.Queued==10,"paid overflow waits safely beyond bed capacity");
        C(c.Resources.Get(ResourceType.Food)==food-60 && c.Resources.Get(ResourceType.Water)==water-30,"treatment charges food and water once per wounded fighter");
        C(c.Hospital.Treat()==HospitalCommandResult.NothingToHeal && c.Resources.Get(ResourceType.Food)==food-60,"repeated treatment does not charge paid patients again");
        c.Hospital.Advance(59);C(c.Armies.Available(id)==70 && c.Hospital.InBeds==20,"partial healing does not grant early fighters");
        c.Hospital.Advance(1);C(c.Armies.Available(id)==90 && c.Hospital.InBeds==10 && c.Hospital.Queued==0,"completed bed batch returns to reserve and queued patients enter beds automatically");
        c.Hospital.Advance(60);C(c.Armies.Available(id)==100 && c.Hospital.TotalWounded==0 && c.Hero.Army.TotalUnits==0,"remaining queued fighters heal with no auto-assignment or loss");
        c.Hospital.Advance(int.MaxValue);C(c.Armies.Available(id)==100,"repeating advance cannot recreate healed fighters");
        var mixed=new Context(heroes,fighters);foreach(var f in mixed.Armies.Fighters)mixed.Armies.AssignFighters(mixed.Hero.InstanceId,f.Definition.Id,10);
        mixed.Battles.RunBattle(mixed.Hero.InstanceId,mixed.Strong());
        C(mixed.Hospital.Capture().Patients.Select(p=>p.DefinitionId).Distinct().Count()==3 && mixed.Hospital.Waiting==30,"all three fighter types enter hospital with stable identity");
        mixed.Hospital.Treat();mixed.Hospital.Advance(120);C(mixed.Armies.Fighters.All(f=>mixed.Armies.Available(f.Definition.Id)==100),"recovery returns each type to its own reserve");
        var atomic=new Context(heroes,fighters);foreach(var f in atomic.Armies.Fighters)atomic.Armies.AssignFighters(atomic.Hero.InstanceId,f.Definition.Id,10);atomic.Resources.Add(ResourceType.Scrap,int.MaxValue-atomic.Resources.Get(ResourceType.Scrap));
        // Force a victory so the overflowing reward must be prepared.
        var weak=new NpcEncounter("overflow","Overflow",Directory.GetFiles(Path.Combine(AssetFixtures.Root,"Assets/Ash/Data/Encounters"),"*.asset").Select(AssetFixtures.Encounter).Single(e=>e.Id=="raider-patrol").Enemy,0,new[]{new ResourceAmount(ResourceType.Scrap,1)},0);
        var atomicPreview=atomic.Battles.PreviewBattle(atomic.Hero.InstanceId,weak);C(atomicPreview.Outcome==BattleOutcome.Victory && atomicPreview.PlayerFighters.Sum(f=>f.Casualties)>0,"failed-battle fixture includes real projected wounded");
        before=Json(atomic);Reject<OverflowException>(()=>atomic.Battles.RunBattle(atomic.Hero.InstanceId,weak),"failed reward preparation rejects entire battle");C(Json(atomic)==before && atomic.Hospital.TotalWounded==0,"failed battle cannot partially admit wounded or change any saved domain");
        var split=new Context(heroes,fighters);var whole=new Context(heroes,fighters);split.Wound();whole.Wound();split.Hospital.Treat();whole.Hospital.Treat();
        foreach(int seconds in new[]{1,13,45,1,7,53})split.Hospital.Advance(seconds);whole.Hospital.Advance(120);
        C(Json(split)==Json(whole),"healing queue is deterministic across split and bulk time");
        var poor=new Context(heroes,fighters,5,10);poor.Wound(10);C(poor.Hospital.Treatable==2,"available food limits treatment quantity");poor.Hospital.Treat();
        C(poor.Hospital.InBeds==2 && poor.Hospital.Waiting==8 && poor.Resources.Get(ResourceType.Food)==1 && poor.Resources.Get(ResourceType.Water)==8,"partial affordable treatment preserves all unpaid wounded");
        before=Json(poor);C(poor.Hospital.Treat()==HospitalCommandResult.InsufficientResources && Json(poor)==before,"unaffordable treatment is atomic");
        var upgrade=new Context(heroes,fighters);upgrade.Wound();upgrade.Hospital.Treat();upgrade.Hospital.Advance(10);
        C(upgrade.Hospital.Upgrade()==HospitalCommandResult.Success && upgrade.Hospital.Beds==40 && upgrade.Hospital.InBeds==30,"upgrade increases beds and admits paid overflow immediately");
        C(upgrade.Hospital.SecondsPerBatch==30,"upgrade doubles healing work rate at level two");
        upgrade.Hospital.Advance(25);C(upgrade.Armies.Available(id)==90 && upgrade.Hospital.InBeds==10,"upgrade preserves prior progress and separate batch start times");
        upgrade.Hospital.Advance(5);C(upgrade.Armies.Available(id)==100 && upgrade.Hospital.TotalWounded==0,"newly admitted batch completes at faster rate");
        var noMoney=new Context(heroes,fighters,0,0);before=Json(noMoney);C(noMoney.Hospital.Upgrade()==HospitalCommandResult.InsufficientResources && Json(noMoney)==before,"unaffordable hospital upgrade cannot partially debit");
        var max=new Context(heroes,fighters,int.MaxValue,int.MaxValue);max.Resources.Add(ResourceType.Scrap,int.MaxValue-max.Resources.Get(ResourceType.Scrap));
        while(max.Hospital.Level<HospitalService.MaximumLevel)C(max.Hospital.Upgrade()==HospitalCommandResult.Success,"hospital upgrades within level cap");
        before=Json(max);C(max.Hospital.Beds==400 && max.Hospital.Upgrade()==HospitalCommandResult.MaximumLevel && Json(max)==before,"maximum hospital level rejects extra upgrade without debit");
        var guarded=new Context(heroes,fighters);guarded.Wound();bool guard=false,later=false,coherent=false;
        guarded.Hospital.Changed+=()=>{coherent=guarded.Hospital.InBeds==20 && guarded.Hospital.Queued==10;try{guarded.Hospital.Treat();}catch(InvalidOperationException){guard=true;}throw new Exception("observer");};guarded.Hospital.Changed+=()=>later=true;guarded.Hospital.Treat();
        C(guard && later && coherent && guarded.Hospital.NotificationErrors.Length==1,"hospital observers see committed state; reentry blocked and later notifications survive errors");
        var save=new Context(heroes,fighters);save.Wound();save.Hospital.Treat();save.Hospital.Advance(17);save.Clock.Checkpoint(1000);before=Json(save);
        var restore=new Context(heroes,fighters);restore.Progress.Restore(codec.Decode(before));C(Json(restore)==before,"patients, paid queue, beds, progress and old domains round-trip together");
        foreach(Action<ProgressState> corrupt in new Action<ProgressState>[] {
            s=>s.Hospital=null,s=>s.Hospital.Level=0,s=>s.Hospital.Level=21,s=>s.Hospital.Level=2,s=>s.Hospital.Patients=null,s=>s.Hospital.Patients[0]=null,
            s=>s.Hospital.Patients[0].DefinitionId="mara-flint",s=>s.Hospital.Patients[0].Level=99,s=>s.Hospital.Patients[0].Quantity=0,
            s=>s.Hospital.Patients[0].Status=(PatientStatus)99,s=>s.Hospital.Patients[0].Work=60,
            s=>s.Hospital.Patients[0]=new HospitalPatientState{DefinitionId=id,Level=1,Quantity=21,Status=PatientStatus.Healing},
            s=>s.Armies.Reserve.First(r=>r.DefinitionId==id).Quantity=int.MaxValue})
        {var bad=codec.Decode(before);corrupt(bad);Reject<ArgumentException>(()=>restore.Progress.Restore(bad),"malformed hospital or fighter overflow rejected");C(Json(restore)==before,"failed hospital restore leaves every live domain unchanged");}
        var old=codec.Decode(before);old.Version=5;old.Resources=old.Resources.Where(r=>r.Type!=ResourceType.Gold).ToArray();old.Hospital=null;restore.Progress.Restore(old);
        C(restore.Hospital.Level==1 && restore.Hospital.TotalWounded==0 && restore.Hero.Army.TotalUnits==save.Hero.Army.TotalUnits && restore.Sites.Seed==save.Sites.Seed,"version-five migration adds an empty hospital without resetting map or army");
        string directory=Path.Combine(Path.GetTempPath(),"ash-hospital-"+Guid.NewGuid().ToString("N"));
        try
        {
            var store=new LocalSaveStore(directory,codec,save.Progress.Validate);store.Save(save.Progress.Capture());var restart=new Context(heroes,fighters);restart.Progress.Restore(store.Load().State);
            var report=restart.Clock.Resume(1120);restart.Hospital.Advance(report.Seconds);store.Save(restart.Progress.Capture());
            C(restart.Hospital.TotalWounded==0 && restart.Armies.Available(id)==100,"offline elapsed heals bed and paid overflow batches after disk restart");
            var again=new Context(heroes,fighters);again.Progress.Restore(store.Load().State);report=again.Clock.Resume(1120);again.Hospital.Advance(report.Seconds);
            C(again.Armies.Available(id)==100 && again.Hospital.TotalWounded==0,"saved offline healing cannot pay recovered fighters twice");
            // A failed disk promotion must leave the old wounded snapshot replayable exactly once.
            store.Reset(save.Progress.Capture());var uncommitted=new Context(heroes,fighters);uncommitted.Progress.Restore(store.Load().State);uncommitted.Hospital.Advance(120);
            Directory.CreateDirectory(store.SavePath+".tmp");bool failed=false;
            try{store.Save(uncommitted.Progress.Capture());}catch(Exception error)when(error is IOException || error is UnauthorizedAccessException){failed=true;}
            C(failed,"failed hospital checkpoint keeps previous good save");Directory.Delete(store.SavePath+".tmp");
            var recovered=new Context(heroes,fighters);recovered.Progress.Restore(store.Load().State);recovered.Hospital.Advance(120);
            C(recovered.Armies.Available(id)==100 && recovered.Hospital.TotalWounded==0,"failed-save recovery heals old patients once without duplicating fighters");
            var reset=again.Progress.Capture();reset.Hospital=HospitalState.Fresh();store.Reset(reset);again.Progress.Restore(store.Load().State);C(again.Hospital.Level==1 && again.Hospital.TotalWounded==0 && !File.Exists(store.BackupPath),"hospital reset uses existing isolated reset path");
        }finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        return passed;
    }
}
