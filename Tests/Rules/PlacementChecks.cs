using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Ash;

internal static class PlacementChecks
{
    private sealed class Codec:IProgressCodec
    {
        private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
        public string Encode(ProgressState s)=>JsonSerializer.Serialize(s,Options);
        public ProgressState Decode(string text)=>ProgressJsonDecoder.Decode(text,(json,type)=>JsonSerializer.Deserialize(json,type,Options));
    }
    public static int Run(HeroDefinition[] definitions,UnitDefinition[] units)
    {
        int count=0;void Check(bool ok,string text){if(!ok)throw new Exception("FAIL placement "+text);count++;Console.WriteLine("PASS placement "+text);}
        void Reject(Action action,string text){try{action();}catch(ArgumentException){Check(true,text);return;}throw new Exception("FAIL placement expected rejection: "+text);}
        var resources=new ResourceManager(new[]{new ResourceAmount(ResourceType.Food,1000),new ResourceAmount(ResourceType.Water,1000),new ResourceAmount(ResourceType.Scrap,1000),new ResourceAmount(ResourceType.Lumber,1000)});
        var buildings=new BuildingService(resources);
        var armies=new HeroArmyService(definitions.Select(d=>new HeroData("owned-"+d.Id,d)),units.Select(u=>new UnitData(u)),100);
        var fragments=new HeroFragmentWallet(definitions.Select(d=>d.Id));var map=new ScavengingService(resources,915);
        var progress=new ProgressPersistence(armies,resources,fragments,buildings,map);var codec=new Codec();
        buildings.ValidatePlacement(buildings.CapturePlacement());
        Check(buildings.CapturePlacement().Positions.Length==18,"six core buildings and twelve plots have stable movable IDs");
        Check(SettlementPlacementCatalog.Snap(1.24f)==1 && SettlementPlacementCatalog.Snap(-1.25f)==-1.5f,"half-unit snapping is symmetric");
        var detached=buildings.CapturePlacement();detached.Positions[0].X=99;
        Check(buildings.Position("core-headquarters").X==-3,"position snapshots are detached");
        string before=codec.Encode(progress.Capture());
        Check(buildings.CanMoveBuilding("core-workshop",-3,3)==PlacementResult.Overlap,"other building footprint blocks move");
        Check(buildings.MoveBuilding("core-workshop",-3,3)==PlacementResult.Overlap && codec.Encode(progress.Capture())==before,"invalid overlap leaves every domain unchanged");
        Check(buildings.MoveBuilding("core-workshop",100,0)==PlacementResult.OutsideWalls,"outside settlement walls rejected");
        Check(buildings.MoveBuilding("core-foodgarden",0,-17.5f)==PlacementResult.GateBlocked,"gate approach stays clear");
        Check(buildings.MoveBuilding("unknown",0,0)==PlacementResult.UnknownBuilding,"unknown placement ID rejected");
        Check(buildings.MoveBuilding("core-workshop",float.NaN,0)==PlacementResult.OutsideWalls && buildings.MoveBuilding("core-workshop",0,float.PositiveInfinity)==PlacementResult.OutsideWalls,"non-finite move coordinates rejected");
        buildings.BuildPlot("plot-01",PlotKind.Farm);buildings.AdvanceDevelopment(63);
        var development=codec.Encode(progress.Capture());var totals=resources.Get(ResourceType.Food);
        int moved=0;buildings.Changed+=()=>moved++;
        Check(buildings.MoveBuilding("core-headquarters",-5.8f,3.2f)==PlacementResult.Success,"headquarters can relocate to clear ground");
        Check(buildings.Position("core-headquarters").X==-6 && buildings.Position("core-headquarters").Z==3 && moved==1,"committed move snaps and publishes once");
        Check(buildings.MoveBuilding("plot-01",-12,-16)==PlacementResult.Success,"development building and pad can move together");
        Check(buildings.Plot("plot-01").Kind==PlotKind.Farm && buildings.Plot("plot-01").Stored==10 && buildings.Plot("plot-01").Seconds==3 && resources.Get(ResourceType.Food)==totals,"moving preserves production, timers and resources");
        buildings.ValidatePlacement(buildings.CapturePlacement());
        string exact=codec.Encode(progress.Capture());progress.Restore(codec.Decode(exact));
        Check(codec.Encode(progress.Capture())==exact,"layout JSON round trip restores exact positions with progression");
        var ordered=progress.Capture();Array.Reverse(ordered.Placement.Positions);progress.Restore(ordered);
        Check(codec.Encode(progress.Capture())==exact,"restore uses stable IDs instead of array order");
        foreach(var mutate in new Action<SettlementPlacementState>[]
        {
            s=>s.Positions=null,s=>s.Positions=s.Positions.Skip(1).ToArray(),s=>s.Positions[0]=null,
            s=>s.Positions[0].Id="unknown",s=>s.Positions[0].Id=s.Positions[1].Id,
            s=>s.Positions[0].X=float.NaN,s=>s.Positions[0].Z=float.NegativeInfinity,
            s=>s.Positions[0].X=40,s=>{s.Positions[0].X=s.Positions[1].X;s.Positions[0].Z=s.Positions[1].Z;},
            s=>{var p=s.Positions.Single(p=>p.Id=="core-foodgarden");p.X=0;p.Z=-17.5f;}
        })
        {
            var broken=progress.Capture();mutate(broken.Placement);Reject(()=>progress.Restore(broken),"malformed layout rejected before restore");
            Check(codec.Encode(progress.Capture())==exact,"malformed restore keeps complete live progression intact");
        }
        var missing=progress.Capture();missing.Placement=null;Reject(()=>progress.Restore(missing),"current save requires explicit placement state");
        var legacy=progress.Capture();legacy.Version=8;legacy.Placement=null;int stock=buildings.Plot("plot-01").Stored;progress.Restore(legacy);
        Check(buildings.Position("core-headquarters").X==-3 && buildings.Position("plot-01").Z==-14 && buildings.Plot("plot-01").Stored==stock,"schema eight migration adds default positions without resetting production");
        // Every core building and every empty/built plot can find a legal new location.
        foreach(var definition in SettlementPlacementCatalog.Buildings)
        {
            buildings.RestorePlacementValidated(SettlementPlacementState.Fresh());bool found=false;
            for(int z=-16;z<=16 && !found;z++)for(int x=-14;x<=14 && !found;x++)
            {
                if(x==definition.X && z==definition.Z)continue;
                if(buildings.CanMoveBuilding(definition.Id,x,z)!=PlacementResult.Success)continue;
                found=buildings.MoveBuilding(definition.Id,x,z)==PlacementResult.Success;
            }
            Check(found,"relocation works for "+definition.Id);
        }
        string directory=Path.Combine(Path.GetTempPath(),"ash-placement-"+Guid.NewGuid().ToString("N"));
        try
        {
            var store=new LocalSaveStore(directory,codec,progress.Validate);var saved=progress.Capture();store.Save(saved);
            buildings.RestorePlacementValidated(SettlementPlacementState.Fresh());progress.Restore(store.Load().State);
            Check(codec.Encode(progress.Capture())==codec.Encode(saved),"disk restart preserves moved layout and existing domains");
        }
        finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        return count;
    }
}
