using System;
using System.Collections.Generic;

namespace Ash
{
    /// <summary>Complete validation before in-place restoration; UI/service references stay valid.</summary>
    public sealed class ProgressPersistence
    {
        public const int Version = 10;
        public const string Format = "ASHLANDS_PROGRESS";
        private readonly HeroArmyService armies;
        private readonly ResourceManager resources;
        private readonly HeroFragmentWallet fragments;
        private readonly BuildingService buildings;
        private readonly ScavengingService scavenging;
        private readonly OfflineScavenging offline;
        private readonly HospitalService hospital;
        private readonly WorldMapService worldMap;

        public ProgressPersistence(HeroArmyService armies, ResourceManager resources, HeroFragmentWallet fragments, BuildingService buildings = null, ScavengingService scavenging = null, OfflineScavenging offline = null)
        {
            this.armies = armies ?? throw new ArgumentNullException(nameof(armies));
            this.resources = resources ?? throw new ArgumentNullException(nameof(resources));
            this.fragments = fragments ?? throw new ArgumentNullException(nameof(fragments));
            this.buildings = buildings ?? new BuildingService(resources);
            this.scavenging = scavenging ?? new ScavengingService(resources);
            this.offline = offline ?? new OfflineScavenging(this.scavenging);
            this.hospital=armies.GetHospital(resources);
            worldMap=this.scavenging.WorldMap??new WorldMapService(armies,this.scavenging,resources,this.buildings,new BattleManager(armies,resources,fragments));
        }
        public ProgressState Capture()
        {
            var resourceTypes = (ResourceType[])Enum.GetValues(typeof(ResourceType));
            var balances = new ResourceAmount[resourceTypes.Length];
            for (int i=0;i<balances.Length;i++) balances[i]=new ResourceAmount(resourceTypes[i],resources.Get(resourceTypes[i]));
            var ids = new SortedSet<string>(StringComparer.Ordinal);
            foreach(var id in fragments.DefinitionIds) ids.Add(id);
            var stock = new List<FragmentState>();
            foreach(var id in ids) stock.Add(new FragmentState { DefinitionId=id, Quantity=fragments.Get(id) });
            return new ProgressState { Placement=buildings.CapturePlacement(), WorldMap=worldMap.Capture(), Development=buildings.CaptureDevelopment(), Hospital=hospital.Capture(), Version=Version, Format=Format, Armies=armies.ExportState(), Resources=balances, Fragments=stock.ToArray(), Buildings=buildings.Capture(), Scavenging=scavenging.Capture(), ScavengingUtcSeconds=offline.LastUtcSeconds };
        }
        public void Validate(ProgressState state) { Prepare(state); }
        public void Restore(ProgressState state)
        {
            var candidate = Prepare(state);
            // Prepare has validated every domain. Nothing is published or notified during these commits.
            armies.RestoreValidatedFrom(candidate.Armies);
            resources.RestoreValidatedFrom(candidate.Resources);
            fragments.RestoreValidatedFrom(candidate.Fragments);
            buildings.RestoreValidated(candidate.Buildings);
            buildings.RestoreDevelopmentValidated(candidate.Development);
            buildings.RestorePlacementValidated(candidate.Placement);
            scavenging.RestoreValidated(candidate.Scavenging);
            offline.RestoreValidated(candidate.ScavengingUtcSeconds);
            hospital.RestoreValidated(candidate.Hospital);
            worldMap.RestoreValidated(candidate.WorldMap);
        }
        public string[] NotifyRestored()
        {
            var errors=new List<string>();
            foreach(var action in new Action[] { armies.NotifyChanged, resources.NotifyChanged, fragments.NotifyChanged, buildings.NotifyChanged, scavenging.NotifyChanged, hospital.NotifyChanged })
                try { action(); } catch(Exception error) { errors.Add(error.Message); }
            return errors.ToArray();
        }
        private sealed class Candidate
        {
            public HeroArmyService Armies;
            public ResourceManager Resources;
            public HeroFragmentWallet Fragments;
            public BuildingState[] Buildings;
            public SettlementDevelopmentState Development;
            public HospitalState Hospital;
            public ScavengingState Scavenging;
            public long ScavengingUtcSeconds;
            public WorldMapState WorldMap;
            public SettlementPlacementState Placement;
        }
        private static SettlementDevelopmentState ExpandLegacyDevelopment(SettlementDevelopmentState saved)
        {
            if(saved?.Plots==null || (saved.Plots.Length!=12 && saved.Plots.Length!=SettlementLayout.Plots.Count))throw new ArgumentException("Incomplete legacy construction state.");
            var expanded=SettlementDevelopmentState.Fresh();expanded.Population=saved.Population;expanded.GrowthSeconds=saved.GrowthSeconds;
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var source in saved.Plots)
            {
                if(source==null || !seen.Add(source.Id))throw new ArgumentException("Duplicate legacy plot.");
                var target=Array.Find(expanded.Plots,p=>p.Id==source.Id);if(target==null)throw new ArgumentException("Unknown legacy plot.");
                int index=Array.IndexOf(expanded.Plots,target);
                if(index>=12 && (source.Kind!=PlotKind.Empty || source.Level!=0 || source.Stored!=0 || source.Seconds!=0))throw new ArgumentException("Legacy expansion plot must be empty.");
                if((int)source.Kind>(int)PlotKind.Scrapyard)throw new ArgumentException("Unknown legacy building type.");
                target.Kind=source.Kind;target.Level=source.Level;target.Stored=source.Stored;target.Seconds=source.Seconds;
            }
            for(int i=0;i<12;i++)if(!seen.Contains(expanded.Plots[i].Id))throw new ArgumentException("Missing legacy plot.");
            return expanded;
        }
        private static SettlementPlacementState ExpandLegacyPlacement(SettlementPlacementState saved)
        {
            if(saved?.Positions==null || (saved.Positions.Length!=18 && saved.Positions.Length!=SettlementPlacementCatalog.Buildings.Count))throw new ArgumentException("Incomplete legacy placement state.");
            var expanded=SettlementPlacementState.Fresh();var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var source in saved.Positions)
            {
                if(source==null || !seen.Add(source.Id))throw new ArgumentException("Duplicate legacy position.");
                var target=Array.Find(expanded.Positions,p=>p.Id==source.Id);if(target==null)throw new ArgumentException("Unknown legacy position.");
                if(Array.IndexOf(expanded.Positions,target)>=18 && (target.X!=source.X || target.Z!=source.Z))throw new ArgumentException("Invalid legacy expansion position.");
                target.X=source.X;target.Z=source.Z;
            }
            for(int i=0;i<18;i++)if(!seen.Contains(expanded.Positions[i].Id))throw new ArgumentException("Missing legacy position.");
            return expanded;
        }
        private Candidate Prepare(ProgressState state)
        {
            if(state==null) throw new ArgumentException("Missing progress document.");
            if(state.Version>Version) throw new UnsupportedSaveVersionException(state.Version);
            if(state.Version<1 || state.Version>Version) throw new ArgumentException("Missing or invalid progress version.");
            if(state.Format!=Format || state.Armies==null || state.Armies.SchemaVersion!=1)
                throw new ArgumentException("Invalid progress format or army schema.");
            if(state.Armies.Heroes==null || state.Armies.Reserve==null || state.Resources==null || state.Fragments==null)
                throw new ArgumentException("Incomplete progress document.");
            BuildingState[] savedBuildings;
            if(state.Version==1) {
                if(state.Buildings!=null)throw new ArgumentException("Unexpected buildings in legacy save.");
                savedBuildings=new BuildingService(new ResourceManager()).Capture();
            } else savedBuildings=state.Buildings;
            buildings.Validate(savedBuildings);
            var savedDevelopment=state.Version<7?SettlementDevelopmentState.Fresh():state.Development;
            int headquarters=0;foreach(var b in savedBuildings)if(b.Kind==BuildingKind.Headquarters)headquarters=b.Level;
            if(state.Version>=7 && state.Version<10)savedDevelopment=ExpandLegacyDevelopment(savedDevelopment);
            buildings.ValidateDevelopment(savedDevelopment,headquarters);
            var savedPlacement=state.Version<9?SettlementPlacementState.Fresh():state.Placement;
            if(state.Version==9)savedPlacement=ExpandLegacyPlacement(savedPlacement);
            buildings.ValidatePlacement(savedPlacement,savedDevelopment);
            ScavengingState savedScavenging;
            if(state.Version<3) {
                if(state.Scavenging!=null)throw new ArgumentException("Unexpected scavenging in legacy save.");
                savedScavenging=ScavengingService.Generate(scavenging.Seed);
            } else savedScavenging=state.Scavenging;
            scavenging.Validate(savedScavenging);
            // Civilian teams now share residents with plot jobs. Retain legacy cargo and
            // return only excess old dedicated workers if their population cannot staff them.
            int plotJobs=0;foreach(var p in savedDevelopment.Plots)plotJobs+=BuildingService.WorkersRequired(p.Kind);
            int civilianSlots=savedDevelopment.Population-plotJobs;
            if(state.Version<8)
            {
                var copy=new ScavengingService(new ResourceManager(),savedScavenging.Seed);
                copy.RestoreValidated(savedScavenging);savedScavenging=copy.Capture();
                foreach(var site in savedScavenging.Sites)
                {
                    int keep=Math.Min(civilianSlots,site.Workers);civilianSlots-=keep;
                    if(keep!=site.Workers){site.Workers=0;site.Work=0;civilianSlots+=keep;}
                }
            }
            else
            {
                foreach(var site in savedScavenging.Sites)civilianSlots-=site.Workers;
                if(civilianSlots<0)throw new ArgumentException("Civilian residents allocated more than once.");
            }
            // Historical versions stored at most 100 at a site, regardless of workforce.
            if(state.Version>=3 && state.Version<5)
                foreach(var site in savedScavenging.Sites)
                    if(site.Stored>100 || (site.Stored==100 && site.Work!=0))throw new ArgumentException("Invalid legacy scavenging cargo.");
            long timestamp=state.Version<4 ? 0 : state.ScavengingUtcSeconds;
            OfflineScavenging.ValidateTimestamp(timestamp);
            var savedHospital=state.Version<6?HospitalState.Fresh():state.Hospital;
            hospital.Validate(savedHospital);
            var expectedHeroes=new Dictionary<string,HeroData>(StringComparer.Ordinal);
            var expectedFighters=new Dictionary<string,UnitData>(StringComparer.Ordinal);
            var fragmentIds=new HashSet<string>(fragments.DefinitionIds,StringComparer.Ordinal);
            foreach(var hero in armies.Heroes) { expectedHeroes.Add(hero.InstanceId,hero); }
            foreach(var fighter in armies.Fighters) expectedFighters.Add(fighter.Definition.Id,fighter);
            if(state.Armies.Heroes.Length!=expectedHeroes.Count || state.Armies.Reserve.Length!=expectedFighters.Count || state.Fragments.Length!=fragmentIds.Count)
                throw new ArgumentException("Saved roster/catalogue does not match this game version.");
            var owned=new Dictionary<string,HeroData>(StringComparer.Ordinal);
            foreach(var saved in state.Armies.Heroes)
            {
                if(saved==null || string.IsNullOrWhiteSpace(saved.InstanceId) || !expectedHeroes.TryGetValue(saved.InstanceId,out var original)
                    || saved.DefinitionId!=original.Definition.Id || saved.ArmyId!=original.Army.Id || saved.Fighters==null || owned.ContainsKey(saved.InstanceId))
                    throw new ArgumentException("Missing, duplicate, or unknown saved hero/army.");
                owned.Add(saved.InstanceId,new HeroData(saved.InstanceId,original.Definition,saved.Level,saved.Xp));
            }
            // Preserve authored roster order rather than trusting array order in the file.
            var roster=new List<HeroData>(); foreach(var hero in armies.Heroes) roster.Add(owned[hero.InstanceId]);
            var candidateArmies=new HeroArmyService(roster,armies.Fighters,0);
            var totals=new Dictionary<string,long>(StringComparer.Ordinal);
            foreach(var id in expectedFighters.Keys) totals[id]=0;
            var seenReserve=new HashSet<string>(StringComparer.Ordinal);
            foreach(var stock in state.Armies.Reserve)
            {
                if(stock==null || stock.DefinitionId==null || !expectedFighters.TryGetValue(stock.DefinitionId,out var fighter)
                    || stock.Level!=fighter.Level || stock.Quantity<0 || !seenReserve.Add(stock.DefinitionId))
                    throw new ArgumentException("Invalid reserve entry.");
                candidateArmies.RestoreReserveValidated(stock.DefinitionId,stock.Quantity); totals[stock.DefinitionId]=stock.Quantity;
            }
            foreach(var saved in state.Armies.Heroes)
            {
                var hero=owned[saved.InstanceId]; var seen=new HashSet<string>(StringComparer.Ordinal);
                foreach(var stack in saved.Fighters)
                {
                    if(stack==null || stack.DefinitionId==null || !expectedFighters.TryGetValue(stack.DefinitionId,out var fighter)
                        || stack.Level!=fighter.Level || stack.Quantity<=0 || !seen.Add(stack.DefinitionId))
                        throw new ArgumentException("Invalid or duplicate army fighter stack.");
                    if(!hero.Army.TryAddUnits(fighter,stack.Quantity)) throw new ArgumentException("Saved army exceeds hero capacity.");
                    totals[stack.DefinitionId]+=stack.Quantity;
                }
            }
            foreach(var patient in savedHospital.Patients)totals[patient.DefinitionId]+=patient.Quantity;
            foreach(var total in totals.Values) if(total>int.MaxValue) throw new ArgumentException("Saved fighter total overflows the reserve.");
            var savedWorld=state.Version<8?WorldMapState.Fresh():state.WorldMap;
            WorldMapService.Validate(savedWorld,candidateArmies,savedScavenging);
            int resourceCount=state.Version<3 ? 5 : Enum.GetValues(typeof(ResourceType)).Length;
            if(state.Resources.Length!=resourceCount) throw new ArgumentException("Incomplete resource ledger.");
            var seenResources=new HashSet<ResourceType>();
            foreach(var resource in state.Resources)
                if(!Enum.IsDefined(typeof(ResourceType),resource.Type) || (state.Version<3 && resource.Type==ResourceType.Lumber) || resource.Amount<0 || !seenResources.Add(resource.Type))
                    throw new ArgumentException("Invalid or duplicate resource balance.");
            var fragmentBalances=new Dictionary<string,int>(StringComparer.Ordinal);
            foreach(var stock in state.Fragments)
            {
                if(stock==null || stock.DefinitionId==null || !fragmentIds.Contains(stock.DefinitionId) || stock.Quantity<0 || fragmentBalances.ContainsKey(stock.DefinitionId))
                    throw new ArgumentException("Invalid or duplicate fragment balance.");
                fragmentBalances.Add(stock.DefinitionId,stock.Quantity);
            }
            return new Candidate { Placement=savedPlacement, WorldMap=savedWorld, Development=savedDevelopment, Hospital=savedHospital, Armies=candidateArmies, Resources=new ResourceManager(state.Resources), Fragments=new HeroFragmentWallet(fragmentIds,fragmentBalances), Buildings=savedBuildings, Scavenging=savedScavenging, ScavengingUtcSeconds=timestamp };
        }
    }
}
