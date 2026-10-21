using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ash
{
    [Serializable] public sealed class BuildingPosition
    {
        public string Id; public float X,Z;
    }
    [Serializable] public sealed class SettlementPlacementState
    {
        public BuildingPosition[] Positions;
        public static SettlementPlacementState Fresh()
        {
            var list=new List<BuildingPosition>();
            foreach(var d in SettlementPlacementCatalog.Buildings)list.Add(new BuildingPosition{Id=d.Id,X=d.X,Z=d.Z});
            return new SettlementPlacementState{Positions=list.ToArray()};
        }
    }
    public sealed class BuildingFootprint
    {
        public string Id {get;} public float X {get;} public float Z {get;} public float Width {get;} public float Depth {get;}
        internal BuildingFootprint(string id,float x,float z,float width,float depth){Id=id;X=x;Z=z;Width=width;Depth=depth;}
    }
    public enum PlacementResult { Success, UnknownBuilding, OutsideWalls, Overlap, GateBlocked, EmptyPlot }
    public static class SettlementPlacementCatalog
    {
        public static ReadOnlyCollection<BuildingFootprint> Buildings {get;}=Create();
        private static ReadOnlyCollection<BuildingFootprint> Create()
        {
            var list=new List<BuildingFootprint>
            {
                new BuildingFootprint("core-headquarters",-3,3,4.6f,4.2f),
                new BuildingFootprint("core-workshop",3,2.5f,3.4f,3.2f),
                new BuildingFootprint("core-barracks",-3.3f,-4,3,2.6f),
                new BuildingFootprint("core-waterstation",3.4f,-4,3,2.6f),
                new BuildingFootprint("core-foodgarden",3.5f,6,3,2.5f),
                new BuildingFootprint("core-hospital",-3,6.5f,3,2.5f)
            };
            foreach(var plot in SettlementLayout.Plots)list.Add(new BuildingFootprint(plot.Id,plot.X,plot.Z,6,6));
            return list.AsReadOnly();
        }
        public static BuildingFootprint Find(string id)
        {
            foreach(var item in Buildings)if(item.Id==id)return item;
            throw new ArgumentException("Unknown settlement building: "+id);
        }
        public static float Snap(float value)=>(float)(Math.Round(value*2,MidpointRounding.AwayFromZero)/2);
        internal static bool Intersects(float x,float z,float width,float depth,float otherX,float otherZ,float otherWidth,float otherDepth)
            =>Math.Abs(x-otherX)<(width+otherWidth)/2-.001f && Math.Abs(z-otherZ)<(depth+otherDepth)/2-.001f;
    }
    public sealed partial class BuildingService
    {
        private SettlementPlacementState placement=SettlementPlacementState.Fresh();
        public BuildingPosition Position(string id)
        {
            foreach(var p in placement.Positions)if(p.Id==id)return CopyPosition(p);
            throw new ArgumentException("Unknown settlement position: "+id);
        }
        public SettlementPlacementState CapturePlacement()
        {
            var list=new List<BuildingPosition>();foreach(var p in placement.Positions)list.Add(CopyPosition(p));
            return new SettlementPlacementState{Positions=list.ToArray()};
        }
        public PlacementResult CanMoveBuilding(string id,float x,float z)
        {
            if(float.IsNaN(x)||float.IsNaN(z)||float.IsInfinity(x)||float.IsInfinity(z))return PlacementResult.OutsideWalls;
            BuildingFootprint moving=null;foreach(var d in SettlementPlacementCatalog.Buildings)if(d.Id==id)moving=d;
            if(moving==null)return PlacementResult.UnknownBuilding;
            if(!HasBuilding(id,development))return PlacementResult.EmptyPlot;
            return CheckPlacement(moving,SettlementPlacementCatalog.Snap(x),SettlementPlacementCatalog.Snap(z),placement,development);
        }
        public PlacementResult MoveBuilding(string id,float x,float z)
        {
            if(commanding)throw new InvalidOperationException("Building command already running.");
            var result=CanMoveBuilding(id,x,z);if(result!=PlacementResult.Success)return result;
            x=SettlementPlacementCatalog.Snap(x);z=SettlementPlacementCatalog.Snap(z);
            foreach(var p in placement.Positions)if(p.Id==id)
            {
                if(p.X==x && p.Z==z)return PlacementResult.Success;
                commanding=true;
                try{p.X=x;p.Z=z;Notify(false);}finally{commanding=false;}
                return PlacementResult.Success;
            }
            return PlacementResult.UnknownBuilding;
        }
        private static PlacementResult CheckPlacement(BuildingFootprint moving,float x,float z,SettlementPlacementState state,SettlementDevelopmentState development)
        {
            if(Math.Abs(x)+moving.Width/2>SettlementLayout.HalfWidth-1 || Math.Abs(z)+moving.Depth/2>SettlementLayout.HalfDepth-1)return PlacementResult.OutsideWalls;
            if(SettlementPlacementCatalog.Intersects(x,z,moving.Width,moving.Depth,0,-18.5f,6,3))return PlacementResult.GateBlocked;
            foreach(var p in state.Positions)
            {
                if(p.Id==moving.Id || !HasBuilding(p.Id,development))continue;var other=SettlementPlacementCatalog.Find(p.Id);
                if(SettlementPlacementCatalog.Intersects(x,z,moving.Width,moving.Depth,p.X,p.Z,other.Width,other.Depth))return PlacementResult.Overlap;
            }
            return PlacementResult.Success;
        }
        private static bool HasBuilding(string id,SettlementDevelopmentState development)
        {
            foreach(var plot in development.Plots)if(plot.Id==id)return plot.Kind!=PlotKind.Empty;
            return true;
        }
        public string BuildingAt(float x,float z)
        {
            foreach(var p in placement.Positions)
            {
                if(!HasBuilding(p.Id,development))continue;var footprint=SettlementPlacementCatalog.Find(p.Id);
                if(Math.Abs(p.X-x)<footprint.Width/2 && Math.Abs(p.Z-z)<footprint.Depth/2)return p.Id;
            }
            return null;
        }
        public void ValidatePlacement(SettlementPlacementState state,SettlementDevelopmentState savedDevelopment=null)
        {
            if(state?.Positions==null || state.Positions.Length!=SettlementPlacementCatalog.Buildings.Count)throw new ArgumentException("Incomplete settlement positions.");
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var p in state.Positions)
            {
                if(p==null || p.Id==null || !seen.Add(p.Id) || float.IsNaN(p.X)||float.IsNaN(p.Z)||float.IsInfinity(p.X)||float.IsInfinity(p.Z))throw new ArgumentException("Invalid/duplicate settlement position.");
                var footprint=SettlementPlacementCatalog.Find(p.Id);
                if(Math.Abs(p.X)+footprint.Width/2>SettlementLayout.HalfWidth-1 || Math.Abs(p.Z)+footprint.Depth/2>SettlementLayout.HalfDepth-1)throw new ArgumentException("Settlement position leaves walls: "+p.Id);
            }
            foreach(var p in state.Positions)if(HasBuilding(p.Id,savedDevelopment??development) && CheckPlacement(SettlementPlacementCatalog.Find(p.Id),p.X,p.Z,state,savedDevelopment??development)!=PlacementResult.Success)throw new ArgumentException("Settlement position overlaps, blocks gate or leaves walls: "+p.Id);
        }
        internal void RestorePlacementValidated(SettlementPlacementState saved)
        {
            var list=new List<BuildingPosition>();
            foreach(var def in SettlementPlacementCatalog.Buildings)foreach(var p in saved.Positions)if(p.Id==def.Id)list.Add(CopyPosition(p));
            placement=new SettlementPlacementState{Positions=list.ToArray()};
        }
        private static BuildingPosition CopyPosition(BuildingPosition p)=>new BuildingPosition{Id=p.Id,X=p.X,Z=p.Z};
    }
}
