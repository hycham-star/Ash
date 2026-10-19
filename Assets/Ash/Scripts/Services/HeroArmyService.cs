using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ash
{
    public enum ArmyCommandResult { Success, HeroNotFound, FighterNotFound, InvalidQuantity, InsufficientReserve, CapacityExceeded, InsufficientAssigned }

    /// <summary>Local domain command boundary; no networking, clocks, or Unity dependencies.</summary>
    public sealed class HeroArmyService
    {
        private readonly Dictionary<string, HeroData> heroes = new Dictionary<string, HeroData>(StringComparer.Ordinal);
        private readonly Dictionary<string, UnitData> fighters = new Dictionary<string, UnitData>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> reserve = new Dictionary<string, int>(StringComparer.Ordinal);
        public ReadOnlyCollection<HeroData> Heroes { get; }
        public ReadOnlyCollection<UnitData> Fighters { get; }
        public event Action Changed;

        public HeroArmyService(IEnumerable<HeroData> heroRoster, IEnumerable<UnitData> fighterCatalogue, int initialReservePerType)
        {
            if (heroRoster == null || fighterCatalogue == null) throw new ArgumentNullException("Roster and catalogue are required.");
            if (initialReservePerType < 0) throw new ArgumentOutOfRangeException(nameof(initialReservePerType));
            var roster = new List<HeroData>(); var catalogue = new List<UnitData>();
            foreach (var hero in heroRoster)
            {
                if (hero == null || heroes.ContainsKey(hero.InstanceId)) throw new ArgumentException("Missing or duplicate hero instance.");
                if (hero.Army.TotalUnits != 0) throw new ArgumentException("Initial heroes must have empty armies; restore via a future explicit persistence adapter.");
                heroes.Add(hero.InstanceId, hero); roster.Add(hero);
            }
            foreach (var fighter in fighterCatalogue)
            {
                if (fighter == null || fighters.ContainsKey(fighter.Definition.Id)) throw new ArgumentException("Missing or duplicate fighter definition.");
                fighters.Add(fighter.Definition.Id, fighter); reserve.Add(fighter.Definition.Id, initialReservePerType); catalogue.Add(fighter);
            }
            Heroes = roster.AsReadOnly(); Fighters = catalogue.AsReadOnly();
        }

        private HospitalService hospital;
        internal HospitalService GetHospital(ResourceManager resources)
        {
            if(hospital==null)hospital=new HospitalService(this,resources);
            else if(!hospital.Uses(resources))throw new InvalidOperationException("Army hospital is bound to another resource ledger.");
            return hospital;
        }
        internal Dictionary<string,int> PrepareHealingReturn(IEnumerable<FighterState> healed)
        {
            var plan=new Dictionary<string,int>(StringComparer.Ordinal);
            foreach(var p in healed)
            {
                if(!fighters.TryGetValue(p.DefinitionId,out var fighter) || fighter.Level!=p.Level || p.Quantity<=0)throw new ArgumentException("Unknown recovered fighter.");
                int current=plan.TryGetValue(p.DefinitionId,out int value)?value:reserve[p.DefinitionId];plan[p.DefinitionId]=checked(current+p.Quantity);
            }
            return plan;
        }
        internal void CommitHealingReturn(Dictionary<string,int> plan){foreach(var p in plan)reserve[p.Key]=p.Value;}
        public int Available(string fighterId) => fighterId != null && reserve.TryGetValue(fighterId, out int quantity) ? quantity : 0;
        public HeroData FindHero(string heroId) => heroId != null && heroes.TryGetValue(heroId, out var hero) ? hero : null;

        public ArmyCommandResult AssignFighters(string heroId, string fighterId, int quantity)
        {
            if (quantity <= 0) return ArmyCommandResult.InvalidQuantity;
            var hero = FindHero(heroId);
            if (hero == null) return ArmyCommandResult.HeroNotFound;
            if (fighterId == null || !fighters.TryGetValue(fighterId, out var fighter)) return ArmyCommandResult.FighterNotFound;
            if (reserve[fighterId] < quantity) return ArmyCommandResult.InsufficientReserve;
            if (!hero.Army.TryAddUnits(fighter, quantity)) return ArmyCommandResult.CapacityExceeded;
            reserve[fighterId] -= quantity;
            Changed?.Invoke();
            return ArmyCommandResult.Success;
        }

        public ArmyCommandResult RemoveFighters(string heroId, string fighterId, int quantity)
        {
            if (quantity <= 0) return ArmyCommandResult.InvalidQuantity;
            var hero = FindHero(heroId);
            if (hero == null) return ArmyCommandResult.HeroNotFound;
            if (fighterId == null || !fighters.TryGetValue(fighterId, out var fighter)) return ArmyCommandResult.FighterNotFound;
            if (hero.Army.QuantityOf(fighterId, fighter.Level) < quantity) return ArmyCommandResult.InsufficientAssigned;
            // Check reserve overflow before touching the army.
            int returned = checked(reserve[fighterId] + quantity);
            if (!hero.Army.TryRemoveUnits(fighterId, fighter.Level, quantity)) return ArmyCommandResult.InsufficientAssigned;
            reserve[fighterId] = returned;
            Changed?.Invoke();
            return ArmyCommandResult.Success;
        }

        public bool GrantHeroXp(string heroId, int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            var hero = FindHero(heroId);
            if (hero == null) return false;
            if (hero.AddXp(amount)) Changed?.Invoke();
            return true;
        }

        internal void ValidateCombatState(HeroData hero, CombatResult result)
        {
            if (FindHero(hero.InstanceId) != hero || result.HeroInstanceId != hero.InstanceId || result.PlayerFighters.Count != hero.Army.Units.Count)
                throw new InvalidOperationException("Battle no longer matches the owned army.");
            foreach (var stack in result.PlayerFighters)
                if (hero.Army.QuantityOf(stack.Fighter.Definition.Id, stack.Fighter.Level) != stack.InitialQuantity)
                    throw new InvalidOperationException("Army quantities changed during battle.");
        }

        internal void CommitCombatSilently(HeroData hero, CombatResult result)
        {
            foreach (var stack in result.PlayerFighters)
                if (stack.Casualties > 0) hero.Army.TryRemoveUnits(stack.Fighter.Definition.Id, stack.Fighter.Level, stack.Casualties);
            hero.AddXp(result.Rewards.HeroXp);
        }
        internal void NotifyChanged() => Changed?.Invoke();

        internal void RestoreReserveValidated(string id,int quantity) { reserve[id]=quantity; }
        internal void RestoreValidatedFrom(HeroArmyService source)
        {
            foreach(var hero in Heroes)
            {
                var saved=source.FindHero(hero.InstanceId);
                hero.RestoreProgress(saved.Level,saved.Xp); hero.Army.RestoreValidatedFrom(saved.Army);
            }
            foreach(var fighter in Fighters)reserve[fighter.Definition.Id]=source.Available(fighter.Definition.Id);
        }

        public HeroArmyState ExportState()
        {
            var state = new HeroArmyState { Heroes = new HeroState[Heroes.Count], Reserve = new FighterState[Fighters.Count] };
            for (int i = 0; i < Heroes.Count; i++)
            {
                var hero = Heroes[i];
                var snapshot = new HeroState { InstanceId = hero.InstanceId, DefinitionId = hero.Definition.Id,
                    Level = hero.Level, Xp = hero.Xp, ArmyId = hero.Army.Id, Fighters = new FighterState[hero.Army.Units.Count] };
                for (int j = 0; j < snapshot.Fighters.Length; j++)
                {
                    var stack = hero.Army.Units[j];
                    snapshot.Fighters[j] = new FighterState { DefinitionId = stack.Unit.Definition.Id, Level = stack.Unit.Level, Quantity = stack.Count };
                }
                state.Heroes[i] = snapshot;
            }
            for (int i = 0; i < Fighters.Count; i++)
                state.Reserve[i] = new FighterState { DefinitionId = Fighters[i].Definition.Id, Level = Fighters[i].Level, Quantity = Available(Fighters[i].Definition.Id) };
            return state;
        }
    }
}
