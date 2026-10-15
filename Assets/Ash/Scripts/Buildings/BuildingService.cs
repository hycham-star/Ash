using System;
using System.Collections.Generic;

namespace Ash
{
    public enum BuildingKind { Headquarters, FoodGarden, WaterStation }
    public enum BuildingCommandResult { Success, MaximumLevel, HeadquartersRequired, InsufficientResources, NothingToCollect, ResourceLimit }
    [Serializable]
    public sealed class BuildingState
    {
        public BuildingKind Kind;
        public int Level;
        public int Stored;
        public int Seconds;
    }

    /// <summary>Local prototype rules. Caller supplies active-play seconds; no Unity or wall clock dependency.</summary>
    public sealed class BuildingService
    {
        private readonly ResourceManager resources;
        private readonly Dictionary<BuildingKind, BuildingState> buildings = new Dictionary<BuildingKind, BuildingState>();
        private bool commanding;
        public event Action Changed;
        public string[] NotificationErrors { get; private set; } = Array.Empty<string>();
        public BuildingService(ResourceManager resources)
        {
            this.resources = resources ?? throw new ArgumentNullException(nameof(resources));
            foreach (BuildingKind kind in Enum.GetValues(typeof(BuildingKind)))
                buildings.Add(kind, new BuildingState { Kind=kind, Level=1 });
        }
        public static string Name(BuildingKind kind)
        {
            switch(kind) {
                case BuildingKind.Headquarters: return "Headquarters";
                case BuildingKind.FoodGarden: return "Food Garden";
                case BuildingKind.WaterStation: return "Water Station";
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
        public static int MaximumLevel(BuildingKind kind) { Name(kind); return kind==BuildingKind.Headquarters ? 25 : 20; }
        public static ResourceType Product(BuildingKind kind)
        {
            if(kind==BuildingKind.FoodGarden)return ResourceType.Food;
            if(kind==BuildingKind.WaterStation)return ResourceType.Water;
            throw new ArgumentException("Headquarters does not produce resources.");
        }
        public int Level(BuildingKind kind) => Find(kind).Level;
        public int Stored(BuildingKind kind) => Find(kind).Stored;
        public int Seconds(BuildingKind kind) => Find(kind).Seconds;
        public int Rate(BuildingKind kind) => kind==BuildingKind.Headquarters ? 0 : BaseRate(kind)*Level(kind);
        public int Capacity(BuildingKind kind) => Rate(kind)*5;
        private static int BaseRate(BuildingKind kind) { Product(kind); return kind==BuildingKind.FoodGarden ? 10 : 6; }
        private BuildingState Find(BuildingKind kind)
        { if(!buildings.TryGetValue(kind,out var state))throw new ArgumentOutOfRangeException(nameof(kind)); return state; }
        public ResourceAmount[] UpgradeCost(BuildingKind kind)
        {
            int level=Level(kind); if(level==MaximumLevel(kind))return Array.Empty<ResourceAmount>();
            int scale=level*level;
            return new[] { new ResourceAmount(ResourceType.Scrap,(kind==BuildingKind.Headquarters ? 40 : 20)*scale),
                new ResourceAmount(ResourceType.Food,10*scale), new ResourceAmount(ResourceType.Water,5*scale) };
        }
        public BuildingCommandResult CanUpgrade(BuildingKind kind)
        {
            if(Level(kind)==MaximumLevel(kind))return BuildingCommandResult.MaximumLevel;
            if(kind!=BuildingKind.Headquarters && Level(kind)>=Level(BuildingKind.Headquarters))return BuildingCommandResult.HeadquartersRequired;
            foreach(var cost in UpgradeCost(kind))if(resources.Get(cost.Type)<cost.Amount)return BuildingCommandResult.InsufficientResources;
            return BuildingCommandResult.Success;
        }
        public BuildingCommandResult Upgrade(BuildingKind kind)
        {
            if(commanding)throw new InvalidOperationException("Building command already running.");
            var result=CanUpgrade(kind); if(result!=BuildingCommandResult.Success)return result;
            commanding=true;
            try {
                // Prepare the complete debit and new level before publishing either domain.
                var balances=new Dictionary<ResourceType,int>();
                foreach(var cost in UpgradeCost(kind))balances.Add(cost.Type, resources.Get(cost.Type)-cost.Amount);
                resources.CommitAdditionSilently(balances); Find(kind).Level++;
                Notify(true); return BuildingCommandResult.Success;
            } finally { commanding=false; }
        }
        public BuildingCommandResult Collect(BuildingKind kind)
        {
            if(commanding)throw new InvalidOperationException("Building command already running.");
            var state=Find(kind);
            if(kind==BuildingKind.Headquarters || state.Stored==0)return BuildingCommandResult.NothingToCollect;
            var type=Product(kind);
            if(resources.Get(type)>int.MaxValue-state.Stored)return BuildingCommandResult.ResourceLimit;
            var plan=resources.PrepareAddition(new[] { new ResourceAmount(type,state.Stored) });
            commanding=true;
            try {
                resources.CommitAdditionSilently(plan); state.Stored=0;
                Notify(true); return BuildingCommandResult.Success;
            } finally { commanding=false; }
        }
        public void Advance(int activeSeconds)
        {
            if(activeSeconds<0)throw new ArgumentOutOfRangeException(nameof(activeSeconds));
            if(commanding)throw new InvalidOperationException("Building command already running.");
            if(activeSeconds==0)return;
            bool changed=false;
            foreach(var state in buildings.Values)
            {
                if(state.Kind==BuildingKind.Headquarters || state.Stored==Capacity(state.Kind))continue;
                long elapsed=(long)state.Seconds+activeSeconds;
                state.Stored=(int)Math.Min(Capacity(state.Kind), state.Stored+(elapsed/60)*Rate(state.Kind));
                state.Seconds=state.Stored==Capacity(state.Kind) ? 0 : (int)(elapsed%60); changed=true;
            }
            if(changed) {
                commanding=true;
                try { Notify(false); } finally { commanding=false; }
            }
        }
        public BuildingState[] Capture()
        {
            var copy=new List<BuildingState>();
            foreach(BuildingKind kind in Enum.GetValues(typeof(BuildingKind))) {
                var state=Find(kind); copy.Add(new BuildingState { Kind=kind,Level=state.Level,Stored=state.Stored,Seconds=state.Seconds });
            }
            return copy.ToArray();
        }
        public void Validate(BuildingState[] saved)
        {
            if(saved==null || saved.Length!=buildings.Count)throw new ArgumentException("Incomplete building ledger.");
            var seen=new HashSet<BuildingKind>(); int hq=0;
            foreach(var state in saved) {
                if(state==null || !Enum.IsDefined(typeof(BuildingKind),state.Kind) || !seen.Add(state.Kind)
                    || state.Level<1 || state.Level>MaximumLevel(state.Kind) || state.Stored<0 || state.Seconds<0 || state.Seconds>=60)
                    throw new ArgumentException("Invalid building state.");
                if(state.Kind==BuildingKind.Headquarters) {
                    hq=state.Level;
                    if(state.Stored!=0 || state.Seconds!=0)throw new ArgumentException("Headquarters cannot store production.");
                } else {
                    int cap=BaseRate(state.Kind)*state.Level*5;
                    if(state.Stored>cap || (state.Stored==cap && state.Seconds!=0))throw new ArgumentException("Invalid building storage.");
                }
            }
            foreach(var state in saved)if(state.Kind!=BuildingKind.Headquarters && state.Level>hq)
                throw new ArgumentException("Building exceeds Headquarters level.");
        }
        internal void RestoreValidated(BuildingState[] saved)
        {
            foreach(var state in saved) {
                var target=Find(state.Kind); target.Level=state.Level; target.Stored=state.Stored; target.Seconds=state.Seconds;
            }
        }
        internal void NotifyChanged() => Changed?.Invoke();
        private void Notify(bool ledgerChanged)
        {
            var errors=new List<string>();
            if(ledgerChanged)try { resources.NotifyChanged(); } catch(Exception e) { errors.Add(e.Message); }
            // Snapshot all subscribers, so one broken UI observer cannot prevent autosave notification.
            if(Changed!=null)foreach(Action observer in Changed.GetInvocationList())
                try { observer(); } catch(Exception e) { errors.Add(e.Message); }
            NotificationErrors=errors.ToArray();
        }
    }
}
