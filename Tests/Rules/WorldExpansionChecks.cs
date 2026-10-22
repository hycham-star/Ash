using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Ash;

internal static class WorldExpansionChecks
{
    public static int Run(HeroDefinition[] heroes,UnitDefinition[] units)
    {
        int passed=0;
        void C(bool ok,string label){if(!ok)throw new Exception("FAIL expansion "+label);passed++;Console.WriteLine("PASS expansion "+label);}
        var options=new JsonSerializerOptions{IncludeFields=true};
        string Encode(object value)=>JsonSerializer.Serialize(value,options);
        ProgressState Clone(ProgressState value)=>JsonSerializer.Deserialize<ProgressState>(Encode(value),options);
        using var baseline=JsonDocument.Parse(File.ReadAllText(Path.Combine(AssetFixtures.Root,"Tests/Rules/World201Baseline.json")));
        C(WorldRegions.TileCount==401 && WorldRegions.HalfSize==200,"401 by 401 tiles expand the old area by approximately four");
        foreach(var old in baseline.RootElement.EnumerateArray())
        {
            int seed=old.GetProperty("seed").GetInt32();var map=ScavengingService.Generate(seed);var camps=WorldMapService.GenerateCamps(map);
            var oldSites=old.GetProperty("sites").EnumerateArray().ToArray();var oldCamps=old.GetProperty("camps").EnumerateArray().ToArray();
            C(map.Sites.Take(16).Select((s,i)=>s.Id==oldSites[i].GetProperty("Id").GetString() && s.X==oldSites[i].GetProperty("X").GetInt32() && s.Z==oldSites[i].GetProperty("Z").GetInt32() && (int)s.Resource==oldSites[i].GetProperty("Type").GetInt32() && s.Remaining==oldSites[i].GetProperty("Remaining").GetInt32()).All(x=>x),"seed "+seed+" all original resource coordinates, types and deposits match pre-change fixture");
            C(camps.Take(256).Select((c,i)=>c.Id==oldCamps[i].GetProperty("Id").GetString() && c.X==oldCamps[i].GetProperty("X").GetInt32() && c.Z==oldCamps[i].GetProperty("Z").GetInt32() && c.Template==oldCamps[i].GetProperty("Template").GetInt32()).All(x=>x),"seed "+seed+" all original 256 camps retain coordinates and levels");
            C(map.Sites.Length==48 && map.Sites.Skip(16).All(s=>Math.Abs(s.X)>100 || Math.Abs(s.Z)>100) && map.Sites.GroupBy(s=>s.Resource).All(g=>g.Count()==12),"seed "+seed+" 32 additional deposits are outside original map");
            C(camps.Length==512 && camps.Skip(256).All(c=>Math.Abs(c.X)>104 || Math.Abs(c.Z)>104),"seed "+seed+" 256 additional camps are outside original map");
        }
        var ledger=new ResourceManager(new[]{new ResourceAmount(ResourceType.Gold,77)});
        var roster=heroes.Select(h=>new HeroData("owned-"+h.Id,h)).ToArray();var army=new HeroArmyService(roster,units.Select(u=>new UnitData(u)),100);
        var fragments=new HeroFragmentWallet(heroes.Select(h=>h.Id));var buildings=new BuildingService(ledger);var sites=new ScavengingService(ledger,42);
        var world=new WorldMapService(army,sites,ledger,buildings,new BattleManager(army,ledger,fragments));
        var progress=new ProgressPersistence(army,ledger,fragments,buildings,sites);
        sites.Assign("site-01",2);sites.Advance(17);
        army.AssignFighters(roster[0].InstanceId,units[0].Id,10);world.Dispatch(roster[0].InstanceId,"site-02",MapMissionKind.Gather);world.Advance(world.TravelSeconds(world.Mission(roster[0].InstanceId))+7);
        world.Scout("camp-190");var oldSave=progress.Capture();oldSave.Version=11;AssetFixtures.AsLegacyWorld(oldSave);
        string oldSitesExact=Encode(oldSave.Scavenging.Sites),oldMissions=Encode(oldSave.WorldMap);
        sites.Recall("site-01");world.Recall(roster[0].InstanceId);progress.Restore(Clone(oldSave));var expanded=progress.Capture();
        C(Encode(expanded.Scavenging.Sites.Take(16).ToArray())==oldSitesExact,"version eleven upgrade preserves workers, fractional work, cargo, depletion and coordinates exactly");
        C(Encode(expanded.WorldMap)==oldMissions && world.IsDeployed(roster[0].InstanceId) && world.IsScouted("camp-190") && world.Mission(roster[0].InstanceId).Cargo>0,"active gathering cargo and army mission and scouted target survive expansion exactly");
        C(ledger.Get(ResourceType.Gold)==77 && expanded.Version==12 && expanded.Scavenging.Sites.Skip(16).All(s=>s.Workers==0 && s.Stored==0 && s.Work==0 && s.Remaining>0),"new deposits begin idle; gold and ownership are preserved");
        string exact=Encode(expanded);progress.Restore(Clone(expanded));C(Encode(progress.Capture())==exact,"current save restart does not reroll map or reapply migration");
        foreach(int kind in Enumerable.Range(0,4))
        {
            var bad=Clone(kind==0?expanded:oldSave);
            if(kind==0)bad.Scavenging.Sites=bad.Scavenging.Sites.Take(47).ToArray();
            if(kind==1)bad.Scavenging.Sites[0].X++;
            if(kind==2)bad.Scavenging.Sites[0].Id=bad.Scavenging.Sites[1].Id;
            if(kind==3)bad.Scavenging.Sites[0].Id="site-17";
            bool rejected=false;try{progress.Restore(bad);}catch(ArgumentException){rejected=true;}
            C(rejected && Encode(progress.Capture())==exact,"corrupt map variant "+kind+" rejected before changing any domain");
        }
        world.Recall(roster[0].InstanceId);world.Advance(1000);world.Dispatch(roster[0].InstanceId,"site-48",MapMissionKind.Gather);
        int travel=world.TravelSeconds(world.Mission(roster[0].InstanceId));world.Advance(travel);
        C(world.Mission(roster[0].InstanceId).Phase==MapMissionPhase.Gathering,"army can reach an outer-region deposit");
        world.Recall(roster[0].InstanceId);world.Advance(1000);
        world.ConfigureEncounters(Directory.GetFiles(Path.Combine(AssetFixtures.Root,"Assets/Ash/Data/Encounters"),"*.asset").OrderBy(p=>p).Select(AssetFixtures.Encounter));
        world.Dispatch(roster[0].InstanceId,"camp-512",MapMissionKind.Raid);world.Advance(2);
        var outbound=progress.Capture();progress.Restore(Clone(outbound));
        C(world.Mission(roster[0].InstanceId).TargetId=="camp-512" && world.Mission(roster[0].InstanceId).Elapsed==2 && world.Mission(roster[0].InstanceId).Phase==MapMissionPhase.Outbound,"outer camp raid remains stable across restart");
        return passed;
    }
}
