using System;
using System.Collections.Generic;

namespace Ash
{
    [Serializable]
    public sealed class ScavengingSiteState
    {
        public string Id;
        public ResourceType Resource;
        public int X, Z, Remaining, Stored, Workers, Work;
    }
    [Serializable]
    public sealed class ScavengingState
    {
        public int Seed;
        public ScavengingSiteState[] Sites;
    }
    public enum ScavengingResult { Success, NoFreeWorkers, SiteFull, NoWorkers, NothingToCollect, ResourceLimit, Exhausted, SiteBusy, PendingDelivery }

    /// <summary>Seeded local map and dedicated worker allocations. Time is supplied by the caller.</summary>
    public sealed class ScavengingService
    {
        public const int TotalWorkers=10, MaximumWorkersPerSite=5, CarryCapacityPerWorker=100, SiteCapacity=MaximumWorkersPerSite*CarryCapacityPerWorker, InitialDeposit=1000;
        public const int MapHalfSize=WorldRegions.HalfSize, LegacySiteCount=16, SiteCount=48;
        private readonly ResourceManager resources;
        private ScavengingState state;
        private bool commanding;
        internal bool IsCommanding => commanding;
        public event Action Changed;
        public string[] NotificationErrors { get; private set; } = Array.Empty<string>();
        public int Seed => state.Seed;
        public int AssignedWorkers { get { int total=0; foreach(var site in state.Sites)total+=site.Workers; return total; } }
        internal Func<int> AvailableCivilians;
        internal WorldMapService WorldMap;
        public int FreeWorkers => Math.Max(0,Math.Min(TotalWorkers-AssignedWorkers,AvailableCivilians?.Invoke()??TotalWorkers));
        internal int TakeForArmy(string id,int requested)
        {
            Guard();if(requested<0)throw new ArgumentOutOfRangeException(nameof(requested));
            var site=Find(id);int taken=Math.Min(requested,site.Remaining);site.Remaining-=taken;
            if(site.Remaining==0 && site.Workers>0)ReturnHome(site);
            return taken;
        }
        public ScavengingService(ResourceManager resources,int seed=1)
        {
            this.resources=resources ?? throw new ArgumentNullException(nameof(resources)); state=Generate(seed);
        }
        public static int Rate(ResourceType type)
        {
            switch(type) {
                case ResourceType.Food: return 10;
                case ResourceType.Water: return 8;
                case ResourceType.Scrap: return 6;
                case ResourceType.Lumber: return 8;
                default: throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
        public static ScavengingState Generate(int seed)
        {
            if(seed<=0)throw new ArgumentOutOfRangeException(nameof(seed));
            uint random=(uint)seed;
            int Next(int count) { unchecked { random^=random<<13; random^=random>>17; random^=random<<5; } return (int)(random%(uint)count); }
            var cells=new List<int>();
            for(int cell=0;cell<100;cell++) {
                int x=-90+(cell%10)*20,z=-90+(cell/10)*20;
                if(x*x+z*z>=900)cells.Add(cell);
            }
            for(int i=cells.Count-1;i>0;i--) { int j=Next(i+1); int t=cells[i]; cells[i]=cells[j]; cells[j]=t; }
            var types=new[] { ResourceType.Food,ResourceType.Water,ResourceType.Scrap,ResourceType.Lumber };
            var sites=new ScavengingSiteState[SiteCount];
            for(int i=0;i<LegacySiteCount;i++)sites[i]=new ScavengingSiteState {
                Id="site-"+(i+1).ToString("00",System.Globalization.CultureInfo.InvariantCulture), Resource=types[i%types.Length],
                X=-90+cells[i]%10*20+Next(9)-4, Z=-90+cells[i]/10*20+Next(9)-4, Remaining=InitialDeposit
            };
            // Append outer sites with a separate random stream. The original sixteen never move.
            uint outer=(uint)seed^0xD71F39u;
            int NextOuter(int count){unchecked{outer^=outer<<13;outer^=outer>>17;outer^=outer<<5;}return (int)(outer%(uint)count);}
            var outerCells=new List<int>();
            for(int cell=0;cell<400;cell++){int x=-190+cell%20*20,z=-190+cell/20*20;if(Math.Abs(x)>100 || Math.Abs(z)>100)outerCells.Add(cell);}
            for(int i=outerCells.Count-1;i>0;i--){int j=NextOuter(i+1),tmp=outerCells[i];outerCells[i]=outerCells[j];outerCells[j]=tmp;}
            var originalSites=new ScavengingSiteState[LegacySiteCount];Array.Copy(sites,originalSites,LegacySiteCount);
            var originalCamps=WorldMapService.GenerateCamps(new ScavengingState{Seed=seed,Sites=originalSites},false,true);
            int added=LegacySiteCount;
            foreach(int cell in outerCells)
            {
                int x=-190+cell%20*20+NextOuter(9)-4,z=-190+cell/20*20+NextOuter(9)-4;bool clear=true;
                foreach(var camp in originalCamps)if((camp.X-x)*(camp.X-x)+(camp.Z-z)*(camp.Z-z)<225){clear=false;break;}
                if(!clear)continue;
                sites[added]=new ScavengingSiteState{Id="site-"+(added+1).ToString("00",System.Globalization.CultureInfo.InvariantCulture),Resource=types[added%4],X=x,Z=z,Remaining=InitialDeposit};
                if(++added==sites.Length)break;
            }
            if(added!=SiteCount)throw new InvalidOperationException("Unable to place expanded resource sites.");
            var map=new ScavengingState { Seed=seed,Sites=sites };
            var regions=originalCamps;
            foreach(var site in sites)site.Remaining=InitialDeposit*WorldRegions.ThreatAt(regions,site.X,site.Z);
            return map;
        }
        private ScavengingSiteState Find(string id)
        {
            foreach(var site in state.Sites)if(site.Id==id)return site;
            throw new ArgumentException("Unknown scavenging site.",nameof(id));
        }
        private static ScavengingSiteState Copy(ScavengingSiteState s) => new ScavengingSiteState { Id=s.Id,Resource=s.Resource,X=s.X,Z=s.Z,Remaining=s.Remaining,Stored=s.Stored,Workers=s.Workers,Work=s.Work };
        public ScavengingSiteState Site(string id)=>Copy(Find(id));
        public ScavengingState Capture()
        {
            var sites=new ScavengingSiteState[state.Sites.Length];
            for(int i=0;i<sites.Length;i++)sites[i]=Copy(state.Sites[i]);
            return new ScavengingState { Seed=Seed,Sites=sites };
        }
        public static int CarryCapacity(int workers) => workers*CarryCapacityPerWorker;
        internal int ResourceBalance(ResourceType type) => resources.Get(type);
        public ScavengingResult Assign(string id,int workers=1)
        {
            Guard(); var site=Find(id);
            if(workers<1 || workers>MaximumWorkersPerSite)throw new ArgumentOutOfRangeException(nameof(workers));
            if(site.Remaining==0)return ScavengingResult.Exhausted;
            if(site.Workers>0)return ScavengingResult.SiteBusy;
            if(site.Stored>0)return ScavengingResult.PendingDelivery;
            if(FreeWorkers<workers)return ScavengingResult.NoFreeWorkers;
            site.Workers=workers; site.Work=0; Notify(false); return ScavengingResult.Success;
        }
        // Recall is an early return of the entire team, including a partial load.
        public ScavengingResult Recall(string id)
        {
            Guard(); var site=Find(id); if(site.Workers==0)return ScavengingResult.NoWorkers;
            bool delivered=ReturnHome(site);Notify(delivered);return site.Stored==0?ScavengingResult.Success:ScavengingResult.ResourceLimit;
        }
        // Retrying a blocked delivery never restarts a completed trip.
        public ScavengingResult Collect(string id)
        {
            Guard();var site=Find(id);
            if(site.Workers>0)return ScavengingResult.SiteBusy;
            if(site.Stored==0)return ScavengingResult.NothingToCollect;
            bool delivered=Deliver(site);
            if(!delivered)return ScavengingResult.ResourceLimit;
            Notify(true);return ScavengingResult.Success;
        }
        private bool Deliver(ScavengingSiteState site)
        {
            if(site.Stored==0 || resources.Get(site.Resource)>int.MaxValue-site.Stored)return false;
            var plan=resources.PrepareAddition(new[] { new ResourceAmount(site.Resource,site.Stored) });
            resources.CommitAdditionSilently(plan);site.Stored=0;return true;
        }
        private bool ReturnHome(ScavengingSiteState site)
        {
            site.Workers=0;site.Work=0;return Deliver(site);
        }
        public void Advance(int seconds)
        {
            Guard(); if(seconds<0)throw new ArgumentOutOfRangeException(nameof(seconds));
            bool changed=false,delivered=false;
            foreach(var site in state.Sites) {
                if(site.Workers==0)continue;
                int capacity=CarryCapacity(site.Workers);
                // Preserve legacy cargo even if it exceeds this team's new carrying capacity.
                if(site.Remaining>0 && site.Stored<capacity && seconds>0) {
                    long work=site.Work+(long)seconds*site.Workers*Rate(site.Resource);
                    int gathered=(int)Math.Min(Math.Min(site.Remaining,capacity-site.Stored),work/60);
                    site.Stored+=gathered;site.Remaining-=gathered;site.Work=(int)(work%60);changed=true;
                }
                if(site.Remaining==0 || site.Stored>=capacity) {
                    delivered|=ReturnHome(site);changed=true;
                }
            }
            if(changed)Notify(delivered);
        }
        public void Validate(ScavengingState saved)
        {
            if(saved==null || saved.Sites==null)throw new ArgumentException("Missing scavenging map.");
            var expected=Generate(saved.Seed); if(saved.Sites.Length!=expected.Sites.Length)throw new ArgumentException("Incomplete map.");
            var known=new Dictionary<string,ScavengingSiteState>(StringComparer.Ordinal);
            foreach(var site in expected.Sites)known.Add(site.Id,site);
            int workers=0;
            foreach(var site in saved.Sites) {
                if(site==null || site.Id==null || !known.TryGetValue(site.Id,out var original))throw new ArgumentException("Unknown or duplicate map site.");
                known.Remove(site.Id);
                if(site.Resource!=original.Resource || site.X!=original.X || site.Z!=original.Z || site.Remaining<0 || site.Stored<0
                    || site.Stored>SiteCapacity || site.Remaining>original.Remaining-site.Stored || site.Workers<0 || site.Workers>MaximumWorkersPerSite
                    || site.Work<0 || site.Work>=60 || ((site.Remaining==0 || site.Stored>=CarryCapacity(Math.Max(1,site.Workers))) && site.Work!=0))
                    throw new ArgumentException("Invalid scavenging site state.");
                workers+=site.Workers;
            }
            if(workers>TotalWorkers)throw new ArgumentException("Workers allocated more than once.");
        }
        internal void RestoreValidated(ScavengingState saved)
        {
            var ordered=Generate(saved.Seed);
            foreach(var target in ordered.Sites)foreach(var source in saved.Sites)if(target.Id==source.Id) {
                target.Remaining=source.Remaining; target.Stored=source.Stored; target.Workers=source.Workers; target.Work=source.Work;
            }
            state=ordered;
        }
        internal void NotifyChanged() => Changed?.Invoke();
        private void Guard() { if(commanding)throw new InvalidOperationException("Scavenging command already running."); }
        private void Notify(bool resourceChanged)
        {
            commanding=true; var errors=new List<string>();
            try {
                if(resourceChanged)try { resources.NotifyChanged(); }catch(Exception e) { errors.Add(e.Message); }
                if(Changed!=null)foreach(Action observer in Changed.GetInvocationList())
                    try { observer(); }catch(Exception e) { errors.Add(e.Message); }
            } finally { commanding=false; NotificationErrors=errors.ToArray(); }
        }
    }
}
