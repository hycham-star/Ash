using System;
using System.Collections.Generic;

namespace Ash
{
    public enum MapMissionKind { Gather, Raid }
    public enum MapMissionPhase { Outbound, Gathering, Dust, Returning }
    [Serializable] public sealed class MapMission
    {
        public string HeroId,TargetId;
        public MapMissionKind Kind;
        public MapMissionPhase Phase;
        public int Elapsed,Cargo,Work;
    }
    [Serializable] public sealed class WorldMapState
    {
        public string[] Scouted;
        public MapMission[] Missions;
        public string Report;
        public static WorldMapState Fresh()=>new WorldMapState{Scouted=Array.Empty<string>(),Missions=Array.Empty<MapMission>(),Report=""};
    }
    public sealed class WorldCamp
    {
        public string Id {get;} public int X {get;} public int Z {get;} public int Template {get;}
        public int Level=>Template+1;
        public string Name=>"Rusthaven "+Id.Substring(5);
        internal WorldCamp(string id,int x,int z,int template){Id=id;X=x;Z=z;Template=template;}
    }
    /// <summary>Local map commands and saved army missions. No Unity, wall clock or networking.</summary>
    public sealed class WorldMapService
    {
        private readonly HeroArmyService armies;
        private readonly ScavengingService scavenging;
        private readonly ResourceManager resources;
        private readonly BuildingService buildings;
        private readonly BattleManager battles;
        private readonly List<MapMission> missions=new List<MapMission>();
        private readonly HashSet<string> scouted=new HashSet<string>(StringComparer.Ordinal);
        private NpcEncounter[] encounters=Array.Empty<NpcEncounter>();
        private bool commanding;
        public event Action Changed;
        public CombatResult LastRaid {get;private set;}
        public string[] NotificationErrors {get;private set;}=Array.Empty<string>();
        public string Report {get;private set;}="";
        private int layoutSeed;
        private WorldCamp[] layout;
        private WorldCamp[] Layout
        {
            get{if(layout==null || layoutSeed!=scavenging.Seed){layout=GenerateCamps(scavenging.Capture());layoutSeed=scavenging.Seed;}return layout;}
        }
        public IReadOnlyList<HeroData> Heroes=>armies.Heroes;
        public WorldCamp[] Camps=>(WorldCamp[])Layout.Clone();
        public bool IsDeployed(string heroId)=>missions.Exists(m=>m.HeroId==heroId);
        public MapMission Mission(string heroId){var m=missions.Find(item=>item.HeroId==heroId);return m==null?null:Copy(m);}
        public int AvailableResidents=>buildings.FreeResidents;
        public HeroData Hero(string instanceId)=>armies.FindHero(instanceId);
        public static int Capacity(HeroData hero)=>(int)Math.Min(5000,(long)hero.Army.TotalUnits*10);
        public WorldMapService(HeroArmyService armies,ScavengingService scavenging,ResourceManager resources,BuildingService buildings,BattleManager battles)
        {
            if(scavenging.WorldMap!=null)throw new InvalidOperationException("Map already bound.");
            this.armies=armies;this.scavenging=scavenging;this.resources=resources;this.buildings=buildings;this.battles=battles;
            scavenging.WorldMap=this;armies.DeploymentBlocked=IsDeployed;
            buildings.ScavengingWorkers=()=>scavenging.AssignedWorkers;
            scavenging.AvailableCivilians=()=>buildings.FreeResidents;
            scavenging.Changed+=buildings.NotifyChanged;
        }
        public void ConfigureEncounters(IEnumerable<NpcEncounter> templates)
        {
            var values=new List<NpcEncounter>(templates);
            if(values.Count!=3 || values.Exists(value=>value==null))throw new ArgumentException("Three existing encounter templates required.");
            encounters=values.ToArray();
        }
        public NpcEncounter Encounter(string campId){var camp=Camp(campId);return encounters.Length==3?encounters[camp.Template]:null;}
        public WorldCamp Camp(string id){foreach(var c in Layout)if(c.Id==id)return c;throw new ArgumentException("Unknown enemy camp.");}
        public bool IsScouted(string campId)=>scouted.Contains(campId);
        public void Scout(string campId){Guard();Camp(campId);scouted.Add(campId);Report="Scout report ready. Level, fighters and rewards revealed.";Notify();}
        public void Dispatch(string heroId,string targetId,MapMissionKind kind)
        {
            Guard();var hero=armies.FindHero(heroId)??throw new ArgumentException("Unknown hero.");
            if(IsDeployed(heroId))throw new InvalidOperationException("Hero already deployed. Recall or wait for return.");
            if(hero.Army.TotalUnits==0)throw new InvalidOperationException("Assign fighters to this hero first.");
            if(kind==MapMissionKind.Gather){if(scavenging.Site(targetId).Remaining==0)throw new InvalidOperationException("Resource site exhausted.");}
            else if(kind==MapMissionKind.Raid){Camp(targetId);if(encounters.Length!=3)throw new InvalidOperationException("Raid encounters not configured.");}
            else throw new ArgumentException("Unknown mission.");
            missions.Add(new MapMission{HeroId=heroId,TargetId=targetId,Kind=kind});
            Report=kind==MapMissionKind.Raid?"Raid convoy dispatched.":"Army gathering convoy dispatched. Returns automatically when full.";Notify();
        }
        public void Recall(string heroId)
        {
            Guard();var m=missions.Find(item=>item.HeroId==heroId)??throw new InvalidOperationException("Hero is at home.");
            if(m.Phase==MapMissionPhase.Returning)return;
            int returnElapsed=m.Phase==MapMissionPhase.Outbound?TravelSeconds(m)-m.Elapsed:0;
            m.Phase=MapMissionPhase.Returning;m.Elapsed=returnElapsed;m.Work=0;Report="Convoy recalled; returning with its current cargo.";Notify();
        }
        public int TravelSeconds(MapMission mission)
        {
            Coordinates(mission,out int x,out int z);return 6+(int)Math.Ceiling(Math.Sqrt(x*x+z*z)/15);
        }
        public void Coordinates(MapMission mission,out int x,out int z)
        {
            if(mission.Kind==MapMissionKind.Raid){var c=Camp(mission.TargetId);x=c.X;z=c.Z;}
            else {var s=scavenging.Site(mission.TargetId);x=s.X;z=s.Z;}
        }
        public void Advance(int seconds,bool allowRaids=true)
        {
            Guard();if(seconds<0 || seconds>OfflineScavenging.MaximumSeconds)throw new ArgumentOutOfRangeException(nameof(seconds));
            if(seconds==0 || missions.Count==0)return;
            var balances=new Dictionary<ResourceType,int>();foreach(ResourceType type in Enum.GetValues(typeof(ResourceType)))balances[type]=resources.Get(type);
            commanding=true;
            try
            {
                for(int tick=0;tick<seconds;tick++)foreach(var m in missions.ToArray())
                {
                    if(m.Kind==MapMissionKind.Raid && m.Phase!=MapMissionPhase.Returning && (!allowRaids || encounters.Length!=3))continue;
                    var hero=armies.FindHero(m.HeroId);int travel=TravelSeconds(m);
                    if(m.Phase==MapMissionPhase.Outbound)
                    {
                        if(++m.Elapsed>=travel){m.Phase=m.Kind==MapMissionKind.Gather?MapMissionPhase.Gathering:MapMissionPhase.Dust;m.Elapsed=0;}
                    }
                    else if(m.Phase==MapMissionPhase.Gathering)
                    {
                        var site=scavenging.Site(m.TargetId);
                        m.Work+=Math.Min(100,hero.Army.TotalUnits)*ScavengingService.Rate(site.Resource);
                        int requested=Math.Min(Capacity(hero)-m.Cargo,m.Work/60);m.Work%=60;
                        m.Cargo+=scavenging.TakeForArmy(m.TargetId,requested);
                        if(m.Cargo>=Capacity(hero) || scavenging.Site(m.TargetId).Remaining==0){m.Phase=MapMissionPhase.Returning;m.Elapsed=0;m.Work=0;}
                    }
                    else if(m.Phase==MapMissionPhase.Dust)
                    {
                        if(++m.Elapsed<3)continue;
                        // Mark return BEFORE battle observers can save, so a settled raid cannot replay.
                        m.Phase=MapMissionPhase.Returning;m.Elapsed=0;
                        try
                        {
                            LastRaid=battles.ResolveDeployment(m.HeroId,Encounter(m.TargetId));
                            int wounded=0;foreach(var f in LastRaid.PlayerFighters)wounded+=f.Casualties;
                            Report=Camp(m.TargetId).Name+": "+LastRaid.Outcome+" • "+wounded+" wounded • XP +"+LastRaid.Rewards.HeroXp+". Convoy returning.";
                        }
                        catch(Exception e){m.Phase=MapMissionPhase.Dust;m.Elapsed=2;Report="Raid waiting: "+e.Message;}
                    }
                    else
                    {
                        m.Elapsed=Math.Min(travel,m.Elapsed+1);
                        if(m.Elapsed<travel)continue;
                        if(m.Cargo>0)
                        {
                            var type=scavenging.Site(m.TargetId).Resource;
                            if(resources.Get(type)>int.MaxValue-m.Cargo){Report="Convoy home; cargo protected until resource balance has room.";continue;}
                            resources.CommitAdditionSilently(resources.PrepareAddition(new[]{new ResourceAmount(type,m.Cargo)}));
                            Report=hero.Definition.DisplayName+" returned with "+m.Cargo+" "+type+". Send again when ready.";
                        }
                        missions.Remove(m);
                    }
                }
            }
            finally
            {
                commanding=false;bool resourceChanged=false;foreach(var balance in balances)if(resources.Get(balance.Key)!=balance.Value)resourceChanged=true;
                Notify(resourceChanged);
            }
        }
        public WorldMapState Capture()
        {
            var known=new List<string>(scouted);known.Sort(StringComparer.Ordinal);
            return new WorldMapState{Scouted=known.ToArray(),Missions=missions.ConvertAll(Copy).ToArray(),Report=Report};
        }
        internal void RestoreValidated(WorldMapState saved)
        {
            missions.Clear();foreach(var m in saved.Missions)missions.Add(Copy(m));
            scouted.Clear();foreach(var id in saved.Scouted)scouted.Add(id);Report=saved.Report??"";LastRaid=null;
        }
        public static void Validate(WorldMapState saved,HeroArmyService armies,ScavengingState map)
        {
            if(armies==null || map==null || map.Sites==null)throw new ArgumentException("Missing world catalogue.");
            if(saved==null || saved.Missions==null || saved.Scouted==null || (saved.Report?.Length??0)>2048)throw new ArgumentException("Missing/invalid world missions.");
            var camps=GenerateCamps(map);var known=new HashSet<string>(StringComparer.Ordinal);
            foreach(var id in saved.Scouted)if(Array.Find(camps,c=>c.Id==id)==null || !known.Add(id))throw new ArgumentException("Unknown/duplicate scouting report.");
            known.Clear();var carried=new Dictionary<string,long>(StringComparer.Ordinal);
            foreach(var m in saved.Missions)
            {
                var hero=m==null?null:armies.FindHero(m.HeroId);
                if(hero==null || !known.Add(m.HeroId) || !Enum.IsDefined(typeof(MapMissionKind),m.Kind) || !Enum.IsDefined(typeof(MapMissionPhase),m.Phase))throw new ArgumentException("Invalid/duplicate deployed hero.");
                bool gather=m.Kind==MapMissionKind.Gather;
                var site=Array.Find(map.Sites,s=>s.Id==m.TargetId);var camp=Array.Find(camps,c=>c.Id==m.TargetId);
                if((gather && site==null) || (!gather && camp==null))throw new ArgumentException("Unknown mission target.");
                int x=gather?site.X:camp.X,z=gather?site.Z:camp.Z;
                int travel=6+(int)Math.Ceiling(Math.Sqrt(x*x+z*z)/15);
                if(m.Elapsed<0 || m.Work<0 || m.Work>=60 || m.Cargo<0 || m.Cargo>Capacity(hero)
                    || (hero.Army.TotalUnits==0 && (gather || m.Phase!=MapMissionPhase.Returning))
                    || (!gather && (m.Cargo!=0 || m.Phase==MapMissionPhase.Gathering))
                    || (gather && m.Phase==MapMissionPhase.Dust)
                    || (m.Phase==MapMissionPhase.Gathering && m.Elapsed!=0)
                    || (m.Phase!=MapMissionPhase.Gathering && m.Work!=0)
                    || (m.Phase==MapMissionPhase.Outbound && m.Cargo!=0)
                    || (m.Phase==MapMissionPhase.Dust && m.Elapsed>=3)
                    || (m.Phase==MapMissionPhase.Outbound && m.Elapsed>=travel)
                    || (m.Phase==MapMissionPhase.Returning && m.Elapsed>travel))throw new ArgumentException("Invalid mission phase/cargo/time.");
                if(gather){carried.TryGetValue(m.TargetId,out long cargo);carried[m.TargetId]=cargo+m.Cargo;}
            }
            var deposits=ScavengingService.Generate(map.Seed);
            foreach(var site in map.Sites)
            {
                if(site==null)throw new ArgumentException("Missing map site.");
                var original=Array.Find(deposits.Sites,item=>item.Id==site.Id);
                if(original==null)throw new ArgumentException("Unknown map site.");
                if(carried.TryGetValue(site.Id,out long cargo) && cargo+site.Remaining+site.Stored>original.Remaining)throw new ArgumentException("Army cargo exceeds harvested deposit.");
            }
        }
        public static WorldCamp[] GenerateCamps(ScavengingState map,bool legacyOnly=false,bool originalOnly=false)
        {
            uint random=(uint)map.Seed^0xA51C29u;
            int Next(int max){unchecked{random^=random<<13;random^=random>>17;random^=random<<5;}return (int)(random%(uint)max);}
            var cells=new List<int>();for(int i=0;i<100;i++)cells.Add(i);
            for(int i=cells.Count-1;i>0;i--){int j=Next(i+1);int tmp=cells[i];cells[i]=cells[j];cells[j]=tmp;}
            var camps=new List<WorldCamp>();
            foreach(int cell in cells)
            {
                int x=-90+cell%10*20,z=-90+cell/10*20;
                if(x*x+z*z<900)continue;
                bool clear=true;foreach(var site in map.Sites)if(Math.Abs(site.X)<=100 && Math.Abs(site.Z)<=100 && (site.X-x)*(site.X-x)+(site.Z-z)*(site.Z-z)<225)clear=false;
                if(!clear)continue;
                camps.Add(new WorldCamp("camp-"+(camps.Count+1).ToString("00",System.Globalization.CultureInfo.InvariantCulture),x,z,camps.Count%3));
                if(camps.Count==9)break;
            }
            if(camps.Count!=9)throw new InvalidOperationException("Unable to place enemy camps.");
            if(legacyOnly)return camps.ToArray();
            var anchors=camps.ToArray();var extra=new List<int>();for(int i=0;i<625;i++)extra.Add(i);
            for(int i=extra.Count-1;i>0;i--){int j=Next(i+1);int temporary=extra[i];extra[i]=extra[j];extra[j]=temporary;}
            foreach(int cell in extra)
            {
                int x=-96+cell%25*8,z=-96+cell/25*8;if(x*x+z*z<900)continue;
                bool clear=true;foreach(var site in map.Sites)if(Math.Abs(site.X)<=100 && Math.Abs(site.Z)<=100 && (site.X-x)*(site.X-x)+(site.Z-z)*(site.Z-z)<225){clear=false;break;}
                if(!clear)continue;
                foreach(var camp in camps)if((camp.X-x)*(camp.X-x)+(camp.Z-z)*(camp.Z-z)<64){clear=false;break;}
                if(!clear)continue;
                camps.Add(new WorldCamp("camp-"+(camps.Count+1).ToString("00",System.Globalization.CultureInfo.InvariantCulture),x,z,WorldRegions.ThreatAt(anchors,x,z)-1));
                if(camps.Count==WorldRegions.OriginalCampCount)break;
            }
            if(camps.Count!=WorldRegions.OriginalCampCount)throw new InvalidOperationException("Unable to place original overworld camps.");
            if(originalOnly)return camps.ToArray();
            var outerCells=new List<int>();
            for(int cell=0;cell<49*49;cell++){int x=-192+cell%49*8,z=-192+cell/49*8;if(Math.Abs(x)>104 || Math.Abs(z)>104)outerCells.Add(cell);}
            for(int i=outerCells.Count-1;i>0;i--){int j=Next(i+1),tmp=outerCells[i];outerCells[i]=outerCells[j];outerCells[j]=tmp;}
            foreach(int cell in outerCells)
            {
                int x=-192+cell%49*8,z=-192+cell/49*8;bool clear=true;
                foreach(var site in map.Sites)if((site.X-x)*(site.X-x)+(site.Z-z)*(site.Z-z)<225){clear=false;break;}
                if(!clear)continue;
                foreach(var camp in camps)if((camp.X-x)*(camp.X-x)+(camp.Z-z)*(camp.Z-z)<64){clear=false;break;}
                if(!clear)continue;
                camps.Add(new WorldCamp("camp-"+(camps.Count+1).ToString("00",System.Globalization.CultureInfo.InvariantCulture),x,z,WorldRegions.ThreatAt(anchors,x,z)-1));
                if(camps.Count==WorldRegions.CampCount)break;
            }
            if(camps.Count!=WorldRegions.CampCount)throw new InvalidOperationException("Unable to place expanded overworld camps.");
            return camps.ToArray();
        }
        private static MapMission Copy(MapMission m)=>new MapMission{HeroId=m.HeroId,TargetId=m.TargetId,Kind=m.Kind,Phase=m.Phase,Elapsed=m.Elapsed,Cargo=m.Cargo,Work=m.Work};
        private void Guard(){if(commanding || battles.IsRunning || scavenging.IsCommanding)throw new InvalidOperationException("Map command already running.");}
        private void Notify(bool resourceChanged=false)
        {
            bool before=commanding;commanding=true;var errors=new List<string>();
            try
            {
                if(resourceChanged)try{resources.NotifyChanged();}catch(Exception error){errors.Add(error.Message);}
                foreach(var action in new Action[]{scavenging.NotifyChanged,buildings.NotifyChanged})
                    try{action();}catch(Exception error){errors.Add(error.Message);}
                if(Changed!=null)foreach(Action callback in Changed.GetInvocationList())try{callback();}catch(Exception error){errors.Add(error.Message);}
            }
            finally{commanding=before;NotificationErrors=errors.ToArray();}
        }
    }
}
