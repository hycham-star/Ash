using System;
using System.Linq;
using System.IO;
using System.Text.Json;
using Ash;

internal static class MarketChecks
{
    private sealed class Codec:IProgressCodec
    {
        private static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
        public string Encode(ProgressState state)=>JsonSerializer.Serialize(state,Options);
        public ProgressState Decode(string text)=>JsonSerializer.Deserialize<ProgressState>(text,Options);
    }
    internal static int Run(HeroDefinition[] heroes,UnitDefinition[] units)
    {
        int passed=0;void C(bool ok,string name){if(!ok)throw new Exception("FAIL market "+name);passed++;Console.WriteLine("PASS market "+name);}
        var ledger=new ResourceManager(new[]{new ResourceAmount(ResourceType.Scrap,1000),new ResourceAmount(ResourceType.Lumber,1000),new ResourceAmount(ResourceType.Gold,100)});
        var buildings=new BuildingService(ledger);var market=new MarketService(ledger,buildings);
        C((int)ResourceType.Gold==6 && (int)PlotKind.Market==8,"new identities append without renumbering saved resources or buildings");
        C(!market.IsBuilt && market.Buy(ResourceType.Water)==MarketResult.MarketRequired && ledger.Get(ResourceType.Gold)==100,"unbuilt market cannot charge gold");
        C(buildings.BuildPlot("plot-01",PlotKind.Market)==BuildingCommandResult.Success && market.IsBuilt,"market is constructible on an existing plot");
        int scrap=ledger.Get(ResourceType.Scrap),wood=ledger.Get(ResourceType.Lumber);buildings.AdvanceDevelopment(120);
        C(buildings.Plot("plot-01").Stored==0 && ledger.Get(ResourceType.Gold)==100 && ledger.Get(ResourceType.Scrap)==scrap && ledger.Get(ResourceType.Lumber)==wood,"market generates no free currency or resources");
        C(MarketService.Offers.Count==5 && MarketService.Offers.Select(o=>o.Resource).Distinct().Count()==5 && MarketService.Offers.All(o=>o.Resource!=ResourceType.Gold && o.Resource!=ResourceType.Survivors),"only five resource offers exist, no currency purchase or population offer");
        C(market.Buy(ResourceType.Gold)==MarketResult.UnknownOffer && market.Buy((ResourceType)999)==MarketResult.UnknownOffer,"unknown offers rejected");
        C(market.Buy(ResourceType.Food,0)==MarketResult.InvalidQuantity && market.Buy(ResourceType.Food,-1)==MarketResult.InvalidQuantity && market.Buy(ResourceType.Food,101)==MarketResult.InvalidQuantity && ledger.Get(ResourceType.Gold)==100,"invalid quantities never debit");
        bool observed=false,reentry=false;Action listener=()=>{observed=ledger.Get(ResourceType.Gold)==80 && ledger.Get(ResourceType.Food)==200;try{market.Buy(ResourceType.Food);}catch(InvalidOperationException){reentry=true;}};
        ledger.Changed+=listener;C(market.Buy(ResourceType.Food,2)==MarketResult.Success,"bulk purchase succeeds");ledger.Changed-=listener;
        C(observed && reentry,"observers see both balances committed and cannot reenter a purchase");
        C(market.Buy(ResourceType.Water,9)==MarketResult.InsufficientGold && ledger.Get(ResourceType.Gold)==80 && ledger.Get(ResourceType.Water)==0,"insufficient gold leaves both balances unchanged");
        ledger.Add(ResourceType.Water,int.MaxValue);C(market.Buy(ResourceType.Water)==MarketResult.ResourceLimit && ledger.Get(ResourceType.Gold)==80,"resource overflow is rejected before gold debit");
        ledger.TrySpend(new[]{new ResourceAmount(ResourceType.Water,int.MaxValue)});
        Action broken=()=>throw new Exception("fixture observer");bool notified=false;ledger.Changed+=broken;market.Changed+=()=>notified=true;
        C(market.Buy(ResourceType.Fuel)==MarketResult.Success && ledger.Get(ResourceType.Fuel)==20 && ledger.Get(ResourceType.Gold)==70 && notified && market.NotificationErrors.Length==1,"observer failure cannot roll back purchase or block independent save notification");ledger.Changed-=broken;
        var armies=new HeroArmyService(heroes.Select(h=>new HeroData("owned-"+h.Id,h)),units.Select(u=>new UnitData(u)),100);
        var fragments=new HeroFragmentWallet(heroes.Select(h=>h.Id));var battle=new BattleManager(armies,ledger,fragments);
        armies.AssignFighters(armies.Heroes[0].InstanceId,armies.Fighters[0].Definition.Id,30);
        var encounter=AssetFixtures.Encounter(System.IO.Path.Combine(AssetFixtures.Root,"Assets/Ash/Data/Encounters/StrayRaiders.asset"));int gold=ledger.Get(ResourceType.Gold);
        var preview=battle.PreviewBattle(armies.Heroes[0].InstanceId,encounter);
        C(preview.Outcome==BattleOutcome.Victory && preview.Rewards.Resources.Single(r=>r.Type==ResourceType.Gold).Amount==10 && ledger.Get(ResourceType.Gold)==gold,"preview shows earned gold but grants nothing");
        battle.RunBattle(armies.Heroes[0].InstanceId,encounter);C(ledger.Get(ResourceType.Gold)==gold+10,"successful PvE settlement credits declared gold reward");
        C(encounter.RewardsFor(BattleOutcome.Defeat,heroes[0].Id).Resources.Count==0 && encounter.RewardsFor(BattleOutcome.Draw,heroes[0].Id).Resources.Count==0,"defeat and draw grant no gold");
        ledger.TrySpend(new[]{new ResourceAmount(ResourceType.Water,int.MaxValue)});
        var sites=new ScavengingService(ledger,125);var progress=new ProgressPersistence(armies,ledger,fragments,buildings,sites);var codec=new Codec();string exact=codec.Encode(progress.Capture());
        market.Buy(ResourceType.Scrap);progress.Restore(codec.Decode(exact));
        C(codec.Encode(progress.Capture())==exact && market.IsBuilt && ledger.Get(ResourceType.Gold)==gold+10,"native services retain market building, gold and purchases through snapshot restore");
        string directory=Path.Combine(Path.GetTempPath(),"Ash-market-"+Guid.NewGuid().ToString("N"));
        try
        {
            var store=new LocalSaveStore(directory,codec,progress.Validate);store.Save(progress.Capture());market.Buy(ResourceType.Fuel);progress.Restore(store.Load().State);
            C(codec.Encode(progress.Capture())==exact,"market gold and purchased resources survive real disk save/load");
        }
        finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        var legacy=progress.Capture();legacy.Version=10;AssetFixtures.AsLegacyWorld(legacy);legacy.Resources=legacy.Resources.Where(r=>r.Type!=ResourceType.Gold).ToArray();legacy.Development.Plots[0].Kind=PlotKind.Empty;legacy.Development.Plots[0].Level=0;
        string old=codec.Encode(legacy);progress.Restore(legacy);
        C(ledger.Get(ResourceType.Gold)==0 && !market.IsBuilt && progress.Capture().Version==ProgressPersistence.Version,"authentic version-ten save migrates with zero gold and unchanged plot ownership");
        var corrupt=codec.Decode(old);corrupt.Resources=corrupt.Resources.Append(new ResourceAmount(ResourceType.Gold,100)).ToArray();bool rejected=false;
        try{progress.Restore(corrupt);}catch(ArgumentException){rejected=true;}C(rejected && ledger.Get(ResourceType.Gold)==0,"legacy save cannot smuggle new currency");
        corrupt=progress.Capture();corrupt.Resources=corrupt.Resources.Where(r=>r.Type!=ResourceType.Gold).ToArray();rejected=false;
        try{progress.Restore(corrupt);}catch(ArgumentException){rejected=true;}C(rejected && ledger.Get(ResourceType.Gold)==0,"current save with missing gold is rejected atomically");
        return passed;
    }
}
