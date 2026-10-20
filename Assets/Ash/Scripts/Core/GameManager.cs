using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using UnityEngine;

namespace Ash
{
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private ResourceAmount[] initialResources = Array.Empty<ResourceAmount>();
        [SerializeField] private HeroConfig[] heroDefinitions = Array.Empty<HeroConfig>();
        [SerializeField] private UnitConfig[] unitDefinitions = Array.Empty<UnitConfig>();
        public ResourceManager Resources { get; private set; }
        public ReadOnlyCollection<HeroData> Heroes { get; private set; }
        public ReadOnlyCollection<UnitData> Units { get; private set; }
        [SerializeField, Min(0)] private int initialReservePerType = 100;
        public HeroArmyService HeroArmies { get; private set; }
        public HeroFragmentWallet Fragments { get; private set; }
        public BattleManager Battles { get; private set; }
        public HospitalService Hospital {get;private set;}
        public BuildingService Buildings { get; private set; }
        public ScavengingService Scavenging { get; private set; }
        public WorldMapService WorldMap {get;private set;}
        public OfflineScavenging OfflineScavenging { get; private set; }
        public string OfflineSummary { get; private set; } = "";
        public event Action OfflineProgressChanged;
        private Func<long> utcNow = () => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        private bool applicationPaused, applicationFocused=true, skipProductionFrame;
        private float productionSeconds, productionCheckpoint;
        private bool productionPaused;
        [SerializeField] private bool enableLocalPersistence;
        private ProgressPersistence progress;
        private LocalSaveStore saveStore;
        private bool dirty, unsaved, restoring, saveBlocked;
        private string saveBlockReason;
        public bool PersistenceActive => saveStore!=null;
        public string SaveStatus { get; private set; } = "Debug session • not saved";
        public string SavePath => saveStore?.SavePath;
        public event Action SaveStatusChanged;
        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            try { Initialize(); if(Application.isPlaying && enableLocalPersistence)BeginLocalPersistence(); }
            catch (Exception error) { Debug.LogError("Game initialization failed: " + error, this); enabled = false; }
        }

        private void BeginLocalPersistence(string directory = null)
        {
            progress=new ProgressPersistence(HeroArmies,Resources,Fragments,Buildings,Scavenging,OfflineScavenging);
            saveStore=new LocalSaveStore(directory ?? Path.Combine(Application.persistentDataPath,"Saves"),new UnityProgressCodec(),progress.Validate);
            TryLoadProgress();
            HeroArmies.Changed+=MarkDirty; Resources.Changed+=MarkDirty; Fragments.Changed+=MarkDirty;
            Buildings.Changed+=MarkProductionDirty; Hospital.Changed+=MarkProductionDirty; Scavenging.Changed+=MarkScavengingDirty;WorldMap.Changed+=MarkScavengingDirty;
            Debug.Log("Local progress file: "+saveStore.SavePath,this);
        }
        private void Update()
        {
            if(!IsInitialized || productionPaused)return;
            // Foreground production is separate from UTC catch-up on resume.
            if(skipProductionFrame) { skipProductionFrame=false; return; }
            float elapsed=Mathf.Min(Time.unscaledDeltaTime,1f);
            productionSeconds+=elapsed; productionCheckpoint+=elapsed;
            if(productionSeconds>=1) {
                int seconds=(int)productionSeconds; productionSeconds-=seconds; Buildings.Advance(seconds); Buildings.AdvanceDevelopment(seconds); Hospital.Advance(seconds);
                advancingScavenging=true;
                try { Scavenging.Advance(seconds); WorldMap.Advance(seconds); } finally { advancingScavenging=false; }
            }
            if(productionCheckpoint>=10) { productionCheckpoint=0; if(unsaved && !saveBlocked)dirty=true; }
        }
        private bool advancingScavenging;
        private void MarkScavengingDirty() { if(restoring)return; if(advancingScavenging)unsaved=true; else MarkDirty(); }
        private void MarkProductionDirty() { if(!restoring)unsaved=true; }
        private void MarkDirty() { if(!restoring) { dirty=true; unsaved=true; } }
        private void LateUpdate() { if(dirty) { dirty=false; if(!saveBlocked)SaveProgress(); } }
        private void OnApplicationPause(bool paused) { applicationPaused=paused; UpdateSuspension(); }
        private void OnApplicationFocus(bool focused) { applicationFocused=focused; UpdateSuspension(); }
        private void UpdateSuspension()
        {
            bool suspended=applicationPaused || !applicationFocused;
            if(suspended==productionPaused)return;
            productionPaused=suspended; productionSeconds=0; skipProductionFrame=true;
            if(!PersistenceActive || saveBlocked)return;
            if(!suspended)CatchUpOffline();
            // Always checkpoint departure, even when no resources changed this frame.
            SaveProgress();
        }
        private void CatchUpOffline()
        {
            advancingScavenging=true;
            try
            {
                var report=OfflineScavenging.Resume(utcNow());int before=Hospital.TotalWounded;
                Hospital.Advance(report.Seconds);OfflineSummary=report.Summary;
                WorldMap.Advance(report.Seconds,false);
                if(report.Seconds>0 && WorldMap.Report.Length>0)OfflineSummary+="\n"+WorldMap.Report;
                int recovered=before-Hospital.TotalWounded;
                if(recovered>0)OfflineSummary+="\nHospital: "+recovered+" fighters recovered to reserve.";
            }
            finally { advancingScavenging=false; }
            if(OfflineSummary.Length>0 && OfflineProgressChanged!=null)
                foreach(Action callback in OfflineProgressChanged.GetInvocationList())
                    try { callback(); } catch(Exception error) { Debug.LogWarning("Offline observer: "+error.Message,this); }
        }
        private void OnApplicationQuit() { if(PersistenceActive && !saveBlocked)SaveProgress(); }
        private void OnDisable() { if(PersistenceActive && !saveBlocked)SaveProgress(); }
        private void OnDestroy()
        {
            if(IsInitialized) { HeroArmies.Changed-=MarkDirty; Resources.Changed-=MarkDirty; Fragments.Changed-=MarkDirty; Buildings.Changed-=MarkProductionDirty; Scavenging.Changed-=MarkScavengingDirty; Hospital.Changed-=MarkProductionDirty;WorldMap.Changed-=MarkScavengingDirty; }
        }
        public bool SaveProgress()
        {
            if(!PersistenceActive) { Status("Saving disabled in this test scene"); return false; }
            if(saveBlocked) { Status("Save protected — existing files kept"); Debug.LogWarning("Save blocked: "+saveBlockReason,this); return false; }
            if(Battles.IsRunning) { Status("Wait for the battle to finish"); return false; }
            try
            {
                OfflineScavenging.Checkpoint(utcNow());
                saveStore.Save(progress.Capture()); dirty=unsaved=false; Status("Saved locally • autosave on"); return true;
            }
            catch(Exception error)
            {
                if(error is UnsupportedSaveVersionException) { saveBlocked=true; saveBlockReason=error.Message; Status("Newer save version — file protected"); }
                else Status("Save failed — progress is not on disk");
                Debug.LogWarning(error.Message,this); return false;
            }
        }
        public bool TryLoadProgress()
        {
            if(!PersistenceActive) { Status("Loading disabled in this test scene"); return false; }
            if(Battles.IsRunning) { Status("Wait for the battle to finish"); return false; }
            try
            {
                var loaded=saveStore.Load();
                if(!loaded.Loaded)
                {
                    if(loaded.Status==ProgressLoadStatus.Missing) { saveBlocked=false; dirty=unsaved=true; Status("New world • autosave on"); return false; }
                    saveBlocked=true; saveBlockReason=loaded.Detail;
                    Status(loaded.Status==ProgressLoadStatus.Unsupported ? "Newer save version — file protected" : "Save unreadable — file protected");
                    Debug.LogWarning(loaded.Detail,this); return false;
                }
                Restore(loaded.State); CatchUpOffline(); dirty=unsaved=true; saveBlocked=false;
                Status(loaded.Status==ProgressLoadStatus.Backup ? "Recovered backup • autosave on" : "Loaded local progress • autosave on");
                if(loaded.Status==ProgressLoadStatus.Backup)Debug.LogWarning("Recovered local progress from backup. "+loaded.Detail,this);
                return true;
            }
            catch(Exception error) { saveBlocked=true; saveBlockReason=error.Message; Status("Load failed — save files protected"); Debug.LogWarning(error.Message,this); return false; }
        }
        public bool ResetProgress()
        {
            if(!PersistenceActive || Battles.IsRunning) { Status("Reset unavailable right now"); return false; }
            try
            {
                var fresh=progress.Capture();
                foreach(var hero in fresh.Armies.Heroes) { hero.Level=1; hero.Xp=0; hero.Fighters=Array.Empty<FighterState>(); }
                foreach(var fighter in fresh.Armies.Reserve)fighter.Quantity=initialReservePerType;
                var starting=new ResourceManager(initialResources);
                for(int i=0;i<fresh.Resources.Length;i++)fresh.Resources[i]=new ResourceAmount(fresh.Resources[i].Type,starting.Get(fresh.Resources[i].Type));
                foreach(var fragment in fresh.Fragments)fragment.Quantity=0;
                fresh.Development=SettlementDevelopmentState.Fresh();
                fresh.WorldMap=WorldMapState.Fresh();
                fresh.Hospital=HospitalState.Fresh();
                fresh.Buildings=new BuildingService(new ResourceManager()).Capture();
                fresh.Scavenging=ScavengingService.Generate(NewMapSeed());
                fresh.ScavengingUtcSeconds=utcNow(); OfflineSummary="";
                saveStore.Reset(fresh); Restore(fresh); dirty=unsaved=saveBlocked=false;
                Status("Progress reset • autosave on"); return true;
            }
            catch(Exception error) { Status("Reset failed — current progress retained"); Debug.LogWarning(error.Message,this); return false; }
        }
        private void Restore(ProgressState state)
        {
            restoring=true;
            try
            {
                progress.Restore(state); productionSeconds=productionCheckpoint=0; Battles.ClearResultAfterLoad();
                foreach(var error in progress.NotifyRestored())Debug.LogWarning("Progress observer: "+error,this);
            }
            finally { restoring=false; }
        }
        private void Status(string message)
        {
            SaveStatus=message;
            if(SaveStatusChanged!=null)
                foreach(Action callback in SaveStatusChanged.GetInvocationList())
                    try { callback(); } catch(Exception error) { Debug.LogWarning("Save-status observer: "+error.Message,this); }
        }

        private static int NewMapSeed() => (int)((uint)Guid.NewGuid().GetHashCode() % int.MaxValue)+1;

        // Scene ownership and explicit references instead of a global singleton or automatic spawning.
        public void Initialize()
        {
            if (IsInitialized) return;
            var resources = new ResourceManager(initialResources);
            var heroes = new List<HeroData>();
            var units = new List<UnitData>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var config in heroDefinitions)
            {
                if (config == null) throw new InvalidOperationException("Missing hero asset.");
                var definition = config.CreateDefinition();
                if (!ids.Add(definition.Id)) throw new InvalidOperationException("Duplicate hero ID: " + definition.Id);
                heroes.Add(new HeroData("owned-" + definition.Id, definition));
            }
            ids.Clear();
            foreach (var config in unitDefinitions)
            {
                if (config == null) throw new InvalidOperationException("Missing unit asset.");
                var definition = config.CreateDefinition();
                if (!ids.Add(definition.Id)) throw new InvalidOperationException("Duplicate unit ID: " + definition.Id);
                units.Add(new UnitData(definition));
            }
            var service = new HeroArmyService(heroes, units, initialReservePerType);
            var fragmentIds = new List<string>();
            foreach (var hero in heroes) fragmentIds.Add(hero.Definition.Id);
            var fragments = new HeroFragmentWallet(fragmentIds);
            Hospital=service.GetHospital(resources);
            var battles = new BattleManager(service, resources, fragments);
            Buildings = new BuildingService(resources);
            Scavenging = new ScavengingService(resources,NewMapSeed());
            WorldMap=new WorldMapService(service,Scavenging,resources,Buildings,battles);
            OfflineScavenging = new OfflineScavenging(Scavenging);
            Resources = resources; HeroArmies = service; Fragments = fragments; Battles = battles;
            Heroes = service.Heroes; Units = service.Fighters;
            IsInitialized = true;
        }
    }
}
