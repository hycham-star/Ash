using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ash
{
    public enum PlotKind { Empty, Housing, Farm, WaterWell, Scrapyard, InfantryCenter, BikerCenter, ArmyCenter, Market }
    public sealed class SettlementPlotDefinition
    {
        public string Id {get;}public int X {get;}public int Z {get;}
        internal SettlementPlotDefinition(string id,int x,int z){Id=id;X=x;Z=z;}
    }
    public static class SettlementLayout
    {
        public const int HalfWidth=28,HalfDepth=32;
        public static ReadOnlyCollection<SettlementPlotDefinition> Plots {get;}=Create();
        private static ReadOnlyCollection<SettlementPlotDefinition> Create()
        {
            var plots=new List<SettlementPlotDefinition>();
            void Add(int x,int z){plots.Add(new SettlementPlotDefinition("plot-"+(plots.Count+1).ToString("00",System.Globalization.CultureInfo.InvariantCulture),x,z));}
            foreach(int z in new[]{-14,14})foreach(int x in new[]{-12,0,12})Add(x,z);
            foreach(int x in new[]{-12,12})foreach(int z in new[]{-6,2,8})Add(x,z);
            foreach(int z in new[]{-25,25})foreach(int x in new[]{-21,-7,7,21})Add(x,z);
            foreach(int x in new[]{-23,23})foreach(int z in new[]{-12,12})Add(x,z);
            return plots.AsReadOnly();
        }
    }
    [Serializable] public sealed class SettlementPlotState
    {
        public string Id;public PlotKind Kind;public int Level,Stored,Seconds;
    }
    [Serializable] public sealed class SettlementDevelopmentState
    {
        public int Population,GrowthSeconds;public SettlementPlotState[] Plots;
        public static SettlementDevelopmentState Fresh()
        {
            var plots=new List<SettlementPlotState>();foreach(var p in SettlementLayout.Plots)plots.Add(new SettlementPlotState{Id=p.Id});
            return new SettlementDevelopmentState{Population=20,Plots=plots.ToArray()};
        }
    }
    /// <summary>Extension of the existing building service: construction, resident workforce and population.</summary>
    public sealed partial class BuildingService
    {
        public const int MaximumPlotLevel=10,BaseHousing=30,ResidentsPerHouseLevel=10;
        private SettlementDevelopmentState development=SettlementDevelopmentState.Fresh();
        public int Population=>development.Population;
        public int HousingCapacity{get{int total=BaseHousing;foreach(var p in development.Plots)if(p.Kind==PlotKind.Housing)total+=p.Level*ResidentsPerHouseLevel;return total;}}
        internal Func<int> ScavengingWorkers;
        public int EmployedResidents{get{int total=ScavengingWorkers?.Invoke()??0;foreach(var p in development.Plots)total+=WorkersRequired(p.Kind);return total;}}
        public int FreeResidents=>Population-EmployedResidents;
        public int GrowthSeconds=>development.GrowthSeconds;
        public static string PlotName(PlotKind kind)
        {
            switch(kind){case PlotKind.Empty:return "Empty plot";case PlotKind.Housing:return "Housing";case PlotKind.Farm:return "Farm";case PlotKind.WaterWell:return "Water Well";case PlotKind.Scrapyard:return "Scrapyard";case PlotKind.InfantryCenter:return "Infantry Center";case PlotKind.BikerCenter:return "Biker Center";case PlotKind.ArmyCenter:return "Army Center";case PlotKind.Market:return "Market";default:throw new ArgumentOutOfRangeException(nameof(kind));}
        }
        public static int WorkersRequired(PlotKind kind){PlotName(kind);return IsTraining(kind)?2:kind==PlotKind.Farm || kind==PlotKind.Scrapyard?3:kind==PlotKind.WaterWell?2:0;}
        public static bool IsTraining(PlotKind kind)=>kind==PlotKind.InfantryCenter || kind==PlotKind.BikerCenter || kind==PlotKind.ArmyCenter;
        public static FighterType TrainedType(PlotKind kind)
        {
            switch(kind){case PlotKind.InfantryCenter:return FighterType.Melee;case PlotKind.BikerCenter:return FighterType.Biker;case PlotKind.ArmyCenter:return FighterType.Sharpshooter;default:throw new ArgumentException("Not a training center.");}
        }
        public static ResourceType PlotProduct(PlotKind kind)
        {switch(kind){case PlotKind.Farm:return ResourceType.Food;case PlotKind.WaterWell:return ResourceType.Water;case PlotKind.Scrapyard:return ResourceType.Scrap;default:throw new ArgumentException("Plot does not produce resources.");}}
        public static int PlotRate(PlotKind kind,int level)=>kind==PlotKind.Farm?10*level:kind==PlotKind.WaterWell?8*level:kind==PlotKind.Scrapyard?6*level:0;
        private SettlementPlotState FindPlot(string id){foreach(var p in development.Plots)if(p.Id==id)return p;throw new ArgumentException("Unknown settlement plot.",nameof(id));}
        private static SettlementPlotState CopyPlot(SettlementPlotState p)=>new SettlementPlotState{Id=p.Id,Kind=p.Kind,Level=p.Level,Stored=p.Stored,Seconds=p.Seconds};
        public SettlementPlotState Plot(string id)=>CopyPlot(FindPlot(id));
        public SettlementDevelopmentState CaptureDevelopment(){var copy=new List<SettlementPlotState>();foreach(var p in development.Plots)copy.Add(CopyPlot(p));return new SettlementDevelopmentState{Population=Population,GrowthSeconds=GrowthSeconds,Plots=copy.ToArray()};}
        public ResourceAmount[] PlotBuildCost(PlotKind kind)
        {
            PlotName(kind);if(kind==PlotKind.Empty)throw new ArgumentException("Choose a building type.");
            if(kind==PlotKind.Market)return new[]{new ResourceAmount(ResourceType.Scrap,40),new ResourceAmount(ResourceType.Lumber,25)};
            if(IsTraining(kind))return new[]{new ResourceAmount(ResourceType.Scrap,40),new ResourceAmount(ResourceType.Lumber,25)};
            return new[]{new ResourceAmount(ResourceType.Scrap,kind==PlotKind.WaterWell?25:kind==PlotKind.Scrapyard?30:20),new ResourceAmount(ResourceType.Lumber,kind==PlotKind.Housing || kind==PlotKind.Scrapyard?15:10)};
        }
        public BuildingCommandResult CanBuildPlot(string id,PlotKind kind)
            =>CanBuildPlotAt(id,kind,Position(id).X,Position(id).Z);
        public BuildingCommandResult CanBuildPlotAt(string id,PlotKind kind,float x,float z)
        {
            var p=FindPlot(id);if(!Enum.IsDefined(typeof(PlotKind),kind) || kind==PlotKind.Empty)return BuildingCommandResult.InvalidPlotType;
            if(p.Kind!=PlotKind.Empty)return BuildingCommandResult.PlotOccupied;
            if(float.IsNaN(x)||float.IsNaN(z)||float.IsInfinity(x)||float.IsInfinity(z))return BuildingCommandResult.PlotOccupied;
            if(CheckPlacement(SettlementPlacementCatalog.Find(id),SettlementPlacementCatalog.Snap(x),SettlementPlacementCatalog.Snap(z),placement,development)!=PlacementResult.Success)return BuildingCommandResult.PlotOccupied;
            if(FreeResidents<WorkersRequired(kind))return BuildingCommandResult.InsufficientResidents;
            foreach(var cost in PlotBuildCost(kind))if(resources.Get(cost.Type)<cost.Amount)return BuildingCommandResult.InsufficientResources;
            return BuildingCommandResult.Success;
        }
        private void DebitPlot(ResourceAmount[] costs){var plan=new Dictionary<ResourceType,int>();foreach(var cost in costs)plan[cost.Type]=resources.Get(cost.Type)-cost.Amount;resources.CommitAdditionSilently(plan);}
        public BuildingCommandResult BuildPlot(string id,PlotKind kind)
            =>BuildPlotAt(id,kind,Position(id).X,Position(id).Z);
        public BuildingCommandResult BuildPlotAt(string id,PlotKind kind,float x,float z)
        {
            if(commanding)throw new InvalidOperationException("Building command already running.");var result=CanBuildPlotAt(id,kind,x,z);if(result!=BuildingCommandResult.Success)return result;commanding=true;
            try
            {
                DebitPlot(PlotBuildCost(kind));var p=FindPlot(id);p.Kind=kind;p.Level=1;
                foreach(var position in placement.Positions)if(position.Id==id){position.X=SettlementPlacementCatalog.Snap(x);position.Z=SettlementPlacementCatalog.Snap(z);}
                Notify(true);return BuildingCommandResult.Success;
            }
            finally{commanding=false;}
        }
        public ResourceAmount[] PlotUpgradeCost(string id)
        {
            var p=FindPlot(id);if(p.Kind==PlotKind.Empty || p.Level==MaximumPlotLevel)return Array.Empty<ResourceAmount>();int scale=p.Level*p.Level;
            return new[]{new ResourceAmount(ResourceType.Scrap,20*scale),new ResourceAmount(ResourceType.Lumber,10*scale)};
        }
        public BuildingCommandResult CanUpgradePlot(string id)
        {
            var p=FindPlot(id);if(p.Kind==PlotKind.Empty)return BuildingCommandResult.InvalidPlotType;if(p.Level==MaximumPlotLevel)return BuildingCommandResult.MaximumLevel;
            if(p.Level>=Level(BuildingKind.Headquarters))return BuildingCommandResult.HeadquartersRequired;
            foreach(var cost in PlotUpgradeCost(id))if(resources.Get(cost.Type)<cost.Amount)return BuildingCommandResult.InsufficientResources;return BuildingCommandResult.Success;
        }
        public BuildingCommandResult UpgradePlot(string id)
        {
            if(commanding)throw new InvalidOperationException("Building command already running.");var result=CanUpgradePlot(id);if(result!=BuildingCommandResult.Success)return result;commanding=true;
            try{DebitPlot(PlotUpgradeCost(id));FindPlot(id).Level++;Notify(true);return BuildingCommandResult.Success;}finally{commanding=false;}
        }
        public BuildingCommandResult CollectPlot(string id)
        {
            if(commanding)throw new InvalidOperationException("Building command already running.");var p=FindPlot(id);if(p.Stored==0)return BuildingCommandResult.NothingToCollect;
            var type=PlotProduct(p.Kind);if(resources.Get(type)>int.MaxValue-p.Stored)return BuildingCommandResult.ResourceLimit;var plan=resources.PrepareAddition(new[]{new ResourceAmount(type,p.Stored)});commanding=true;
            try{resources.CommitAdditionSilently(plan);p.Stored=0;Notify(true);return BuildingCommandResult.Success;}finally{commanding=false;}
        }
        private static void AdvanceStock(int stored,int timer,int seconds,int rate,int capacity,out int stock,out int nextTimer)
        {long elapsed=(long)timer+seconds;stock=(int)Math.Min(capacity,stored+(elapsed/60)*rate);nextTimer=stock==capacity?0:(int)(elapsed%60);}
        public void AdvanceDevelopment(int activeSeconds, bool automaticCollection = false)
        {
            if(commanding)throw new InvalidOperationException("Building command already running.");if(activeSeconds<0)throw new ArgumentOutOfRangeException(nameof(activeSeconds));if(activeSeconds==0)return;
            commanding=true;bool changed=false,ledger=false;
            try
            {
                foreach(var p in development.Plots) {
                    int rate=PlotRate(p.Kind,p.Level);if(rate==0)continue;
                    if(automaticCollection)changed|=AdvanceAutomatic(PlotProduct(p.Kind),rate,activeSeconds,ref p.Stored,ref p.Seconds,ref ledger);
                    else if(p.Stored<rate*5){AdvanceStock(p.Stored,p.Seconds,activeSeconds,rate,rate*5,out p.Stored,out p.Seconds);changed=true;}
                }
                int before=development.GrowthSeconds;
                if(Population>=HousingCapacity || resources.Get(ResourceType.Food)<2 || resources.Get(ResourceType.Water)<1)development.GrowthSeconds=0;
                else
                {
                    long elapsed=(long)GrowthSeconds+activeSeconds;int budget=Math.Min(resources.Get(ResourceType.Food)/2,resources.Get(ResourceType.Water));
                    int arrivals=(int)Math.Min(elapsed/60,Math.Min(HousingCapacity-Population,budget));
                    if(arrivals>0){resources.CommitAdditionSilently(new Dictionary<ResourceType,int>{{ResourceType.Food,resources.Get(ResourceType.Food)-arrivals*2},{ResourceType.Water,resources.Get(ResourceType.Water)-arrivals}});development.Population+=arrivals;ledger=changed=true;}
                    development.GrowthSeconds=Population==HousingCapacity || resources.Get(ResourceType.Food)<2 || resources.Get(ResourceType.Water)<1?0:(int)(elapsed%60);
                }
                changed|=before!=GrowthSeconds;if(changed)Notify(ledger);
            }finally{commanding=false;}
        }
        public void ValidateDevelopment(SettlementDevelopmentState state,int headquartersLevel)
        {
            if(state==null || state.Plots==null || state.Plots.Length!=SettlementLayout.Plots.Count || state.Population<20 || state.GrowthSeconds<0 || state.GrowthSeconds>=60)throw new ArgumentException("Incomplete settlement development.");
            var known=new HashSet<string>(StringComparer.Ordinal);foreach(var p in SettlementLayout.Plots)known.Add(p.Id);int capacity=BaseHousing,employed=0;
            foreach(var p in state.Plots)
            {
                if(p==null || p.Id==null || !known.Remove(p.Id) || !Enum.IsDefined(typeof(PlotKind),p.Kind))throw new ArgumentException("Invalid or duplicate settlement plot.");
                if(p.Kind==PlotKind.Empty){if(p.Level!=0 || p.Stored!=0 || p.Seconds!=0)throw new ArgumentException("Empty plot cannot contain production.");continue;}
                if(p.Level<1 || p.Level>MaximumPlotLevel || p.Level>headquartersLevel || p.Stored<0 || p.Seconds<0 || p.Seconds>=60)throw new ArgumentException("Invalid plot level or production.");
                int rate=PlotRate(p.Kind,p.Level);if(rate==0){if(p.Stored!=0 || p.Seconds!=0)throw new ArgumentException("Non-production building contains resource stock.");if(p.Kind==PlotKind.Housing)capacity+=p.Level*ResidentsPerHouseLevel;}
                else if(p.Stored>rate*5 || (p.Stored==rate*5 && p.Seconds!=0))throw new ArgumentException("Invalid plot storage.");
                employed+=WorkersRequired(p.Kind);
            }
            if(state.Population>capacity || state.Population<employed || (state.Population==capacity && state.GrowthSeconds!=0))throw new ArgumentException("Invalid population, housing or workforce.");
        }
        internal void RestoreDevelopmentValidated(SettlementDevelopmentState saved)
        {
            var ordered=SettlementDevelopmentState.Fresh();ordered.Population=saved.Population;ordered.GrowthSeconds=saved.GrowthSeconds;
            foreach(var p in ordered.Plots)foreach(var s in saved.Plots)if(p.Id==s.Id){p.Kind=s.Kind;p.Level=s.Level;p.Stored=s.Stored;p.Seconds=s.Seconds;}
            development=ordered;
        }
    }
}
