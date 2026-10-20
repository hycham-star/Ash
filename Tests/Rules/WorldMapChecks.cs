using System;
using System.Linq;
using System.IO;
using Ash;

internal static class WorldMapChecks
{
    public static int Run(HeroDefinition[] definitions,UnitDefinition[] units)
    {
        int count=0;
        void Check(bool ok,string label){if(!ok)throw new Exception("FAIL world "+label);count++;Console.WriteLine("PASS world "+label);}
        void Reject(Action action,string label){try{action();}catch(ArgumentException){Check(true,label);return;}catch(InvalidOperationException){Check(true,label);return;}throw new Exception("FAIL world expected rejection: "+label);}
        var templates=Directory.GetFiles(Path.Combine(AssetFixtures.Root,"Assets/Ash/Data/Encounters"),"*.asset").OrderBy(p=>p).Select(AssetFixtures.Encounter).ToArray();
        var resources=new ResourceManager(new[]{new ResourceAmount(ResourceType.Food,1000),new ResourceAmount(ResourceType.Water,1000),new ResourceAmount(ResourceType.Scrap,1000),new ResourceAmount(ResourceType.Lumber,1000)});
        var roster=definitions.Select(d=>new HeroData("owned-"+d.Id,d)).ToArray();
        var armies=new HeroArmyService(roster,units.Select(u=>new UnitData(u)),1000);
        var fragments=new HeroFragmentWallet(definitions.Select(d=>d.Id));var battles=new BattleManager(armies,resources,fragments);
        var buildings=new BuildingService(resources);var scavenging=new ScavengingService(resources,4821);
        var world=new WorldMapService(armies,scavenging,resources,buildings,battles);world.ConfigureEncounters(templates);
        var progress=new ProgressPersistence(armies,resources,fragments,buildings,scavenging);
        var hero=roster[0];string fighter=units[0].Id;var site=scavenging.Capture().Sites[0];
        var camps=world.Camps;
        Check(camps.Length==9 && camps.Select(c=>c.Level).Distinct().Count()==3,"nine camps in three difficulty tiers");
        Check(camps.Select(c=>c.Id+":"+c.X+":"+c.Z).SequenceEqual(world.Camps.Select(c=>c.Id+":"+c.X+":"+c.Z)),"stable camp IDs and coordinates");
        Check(camps.All(c=>scavenging.Capture().Sites.All(s=>(s.X-c.X)*(s.X-c.X)+(s.Z-c.Z)*(s.Z-c.Z)>=225)),"camps do not overlap resource nodes");
        for(int seed=1;seed<=100;seed++)WorldMapService.GenerateCamps(ScavengingService.Generate(seed));
        Check(true,"camp placement works across 100 seeds");
        Reject(()=>world.Dispatch(hero.InstanceId,site.Id,MapMissionKind.Gather),"empty armies cannot gather");
        Reject(()=>world.Dispatch(hero.InstanceId,"missing",MapMissionKind.Raid),"unknown targets rejected");
        world.Scout(camps[0].Id);Check(world.IsScouted(camps[0].Id) && !world.IsDeployed(hero.InstanceId),"scouting reveals report without dispatching hero");
        armies.AssignFighters(hero.InstanceId,fighter,10);
        world.Dispatch(hero.InstanceId,site.Id,MapMissionKind.Gather);world.Advance(2);
        int earlyTravel=world.TravelSeconds(world.Mission(hero.InstanceId));world.Recall(hero.InstanceId);
        Check(world.Mission(hero.InstanceId).Elapsed==earlyTravel-2,"early recall preserves convoy map position");
        world.Advance(2);Check(!world.IsDeployed(hero.InstanceId),"early recall returns from actual travelled distance");
        world.Dispatch(hero.InstanceId,site.Id,MapMissionKind.Gather);
        Check(world.IsDeployed(hero.InstanceId) && world.Mission(hero.InstanceId).Phase==MapMissionPhase.Outbound,"gather starts outbound");
        Reject(()=>world.Dispatch(hero.InstanceId,camps[0].Id,MapMissionKind.Raid),"same hero cannot dispatch twice");
        Check(armies.AssignFighters(hero.InstanceId,fighter,1)==ArmyCommandResult.ArmyDeployed && armies.RemoveFighters(hero.InstanceId,fighter,1)==ArmyCommandResult.ArmyDeployed,"deployed fighters cannot be transferred");
        Reject(()=>battles.RunBattle(hero.InstanceId,templates[0]),"deployed hero blocked from direct raid");
        int travel=world.TravelSeconds(world.Mission(hero.InstanceId));world.Advance(travel);
        Check(world.Mission(hero.InstanceId).Phase==MapMissionPhase.Gathering,"arrival starts gathering");
        world.Advance(12);int cargo=world.Mission(hero.InstanceId).Cargo;
        Check(cargo>0 && scavenging.Site(site.Id).Remaining==1000-cargo,"cargo comes from finite deposit");
        int balance=resources.Get(site.Resource);world.Recall(hero.InstanceId);world.Advance(travel);
        Check(!world.IsDeployed(hero.InstanceId) && resources.Get(site.Resource)==balance+cargo,"recall delivers partial cargo on return");
        Check(hero.Army.TotalUnits==10 && hero.Xp==0,"peaceful gathering preserves fighters and XP");
        world.Dispatch(hero.InstanceId,site.Id,MapMissionKind.Gather);world.Advance(1000,false);
        Check(!world.IsDeployed(hero.InstanceId) && resources.Get(site.Resource)==balance+cargo+100,"full army load auto-returns offline without repeating");
        var before=progress.Capture();world.Advance(1000,false);
        Check(resources.Get(site.Resource)==balance+cargo+100,"completed gathering does not auto-resend");
        Check(scavenging.Assign(site.Id,5)==ScavengingResult.Success && buildings.EmployedResidents==5 && buildings.FreeResidents==15,"civilian collection reserves development population");
        scavenging.Recall(site.Id);Check(buildings.EmployedResidents==0 && buildings.FreeResidents==20,"civilian return releases jobs");
        world.Dispatch(hero.InstanceId,site.Id,MapMissionKind.Gather);world.Advance(travel+5);
        var saved=progress.Capture();world.Recall(hero.InstanceId);world.Advance(100);
        progress.Restore(saved);Check(world.Mission(hero.InstanceId).Phase==MapMissionPhase.Gathering && world.Mission(hero.InstanceId).Cargo==saved.WorldMap.Missions[0].Cargo,"save restores gathering phase and cargo");
        var clone=world.Capture();clone.Missions[0].Cargo=5001;Reject(()=>WorldMapService.Validate(clone,armies,scavenging.Capture()),"invalid cargo rejected");
        clone=world.Capture();clone.Missions=new[]{clone.Missions[0],clone.Missions[0]};Reject(()=>WorldMapService.Validate(clone,armies,scavenging.Capture()),"duplicate deployment rejected");
        clone=world.Capture();clone.Missions[0].TargetId="camp-01";Reject(()=>WorldMapService.Validate(clone,armies,scavenging.Capture()),"resource mission cannot target camp");
        clone=world.Capture();clone.Scouted=new[]{"camp-99"};Reject(()=>WorldMapService.Validate(clone,armies,scavenging.Capture()),"unknown scouting report rejected");
        clone=world.Capture();clone.Missions[0].Cargo=100;var tamperedMap=scavenging.Capture();tamperedMap.Sites.First(s=>s.Id==site.Id).Remaining=1000;
        Reject(()=>WorldMapService.Validate(clone,armies,tamperedMap),"carried resources must have left finite deposit");
        Reject(()=>world.Advance(-1),"negative mission time rejected");
        world.Recall(hero.InstanceId);world.Advance(100);
        world.Dispatch(hero.InstanceId,camps[0].Id,MapMissionKind.Raid);var raidSaved=progress.Capture();world.Advance(1000,false);
        Check(world.Mission(hero.InstanceId).Elapsed==0 && world.LastRaid==null,"raids pause offline and never silently settle");
        int settled=0;ProgressState settlementSnapshot=null;
        battles.BattleCompleted+=_=>{settled++;settlementSnapshot=progress.Capture();progress.Validate(settlementSnapshot);};
        travel=world.TravelSeconds(world.Mission(hero.InstanceId));world.Advance(travel);
        Check(world.Mission(hero.InstanceId).Phase==MapMissionPhase.Dust && settled==0,"arrival shows dust before resolving");
        world.Advance(2);Check(settled==0,"dust lasts three seconds");
        world.Advance(1);Check(settled==1 && world.Mission(hero.InstanceId).Phase==MapMissionPhase.Returning,"raid resolves once through existing battle engine");
        Check(settlementSnapshot!=null && settlementSnapshot.WorldMap.Missions[0].Phase==MapMissionPhase.Returning,"save taken by battle observer already records settled return phase");
        Check(battles.Hospital.TotalWounded==world.LastRaid.PlayerFighters.Sum(f=>f.Casualties),"all raid casualties enter hospital");
        int xp=hero.Xp;progress.Restore(settlementSnapshot);world.Advance(100,false);
        Check(settled==1 && hero.Xp==xp && !world.IsDeployed(hero.InstanceId),"saved returning raid cannot grant rewards twice");
        var legacy=progress.Capture();legacy.Version=7;legacy.WorldMap=null;progress.Restore(legacy);
        Check(world.Capture().Missions.Length==0 && world.Camps[0].X==camps[0].X,"version seven migration preserves camp seed without phantom missions");
        var broken=progress.Capture();broken.WorldMap=null;Reject(()=>progress.Restore(broken),"current save missing world state rejected");
        var oldBusy=progress.Capture();oldBusy.Version=7;oldBusy.WorldMap=null;
        for(int i=0;i<6;i++){oldBusy.Development.Plots[i].Kind=PlotKind.Farm;oldBusy.Development.Plots[i].Level=1;}
        oldBusy.Scavenging.Sites[0].Workers=5;oldBusy.Scavenging.Sites[0].Stored=10;oldBusy.Scavenging.Sites[0].Remaining=990;
        oldBusy.Scavenging.Sites[1].Workers=5;progress.Restore(oldBusy);
        Check(buildings.FreeResidents>=0 && scavenging.AssignedWorkers==0 && scavenging.Site(oldBusy.Scavenging.Sites[0].Id).Stored==10,"legacy excess civilian teams return without losing protected cargo");
        var overstaff=progress.Capture();overstaff.Scavenging.Sites[1].Workers=5;Reject(()=>progress.Restore(overstaff),"current saves cannot allocate residents twice");
        progress.Restore(legacy);
        // A depleted deposit may release a civilian team while an army harvests the last resources.
        var shortSite=scavenging.Capture();var target=shortSite.Sites[0];target.Remaining=1;target.Workers=1;target.Work=7;target.Stored=4;
        scavenging.Validate(shortSite);scavenging.RestoreValidated(shortSite);
        if(hero.Army.TotalUnits==0)armies.AssignFighters(hero.InstanceId,fighter,1);
        world.Dispatch(hero.InstanceId,target.Id,MapMissionKind.Gather);world.Advance(1000,false);
        scavenging.Validate(scavenging.Capture());Check(scavenging.Site(target.Id).Workers==0 && scavenging.Site(target.Id).Remaining==0,"last army harvest returns civilian team and preserves valid site timers");
        // Full resource ledgers retain army cargo safely until delivery can be retried.
        var second=scavenging.Capture().Sites[1];resources.Add(second.Resource,int.MaxValue-resources.Get(second.Resource));
        world.Dispatch(hero.InstanceId,second.Id,MapMissionKind.Gather);world.Advance(1000,false);
        Check(world.IsDeployed(hero.InstanceId) && world.Mission(hero.InstanceId).Cargo>0,"resource overflow protects returning cargo");
        int pending=world.Mission(hero.InstanceId).Cargo;resources.TrySpend(new[]{new ResourceAmount(second.Resource,pending)});world.Advance(1,false);
        Check(!world.IsDeployed(hero.InstanceId) && resources.Get(second.Resource)==int.MaxValue,"protected cargo delivers once balance has room");
        progress.Validate(progress.Capture());Check(true,"completed mission snapshot validates");
        return count;
    }
}
