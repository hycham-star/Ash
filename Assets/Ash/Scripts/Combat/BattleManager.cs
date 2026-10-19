using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ash
{
    /// <summary>Local PvE orchestration. Preview is pure; RunBattle settles one result once.</summary>
    public sealed class BattleManager
    {
        private readonly HeroArmyService armies;
        private readonly ResourceManager resources;
        private readonly HeroFragmentWallet fragments;
        private readonly AutoBattle simulation = new AutoBattle();
        public HospitalService Hospital {get;}
        private bool running;
        public CombatResult LastResult { get; private set; }
        public ReadOnlyCollection<string> NotificationErrors { get; private set; } = new List<string>().AsReadOnly();
        public event Action<CombatResult> BattleCompleted;

        public BattleManager(HeroArmyService armies, ResourceManager resources, HeroFragmentWallet fragments)
        {
            this.armies = armies ?? throw new ArgumentNullException(nameof(armies));
            this.resources = resources ?? throw new ArgumentNullException(nameof(resources));
            this.fragments = fragments ?? throw new ArgumentNullException(nameof(fragments));
            Hospital=armies.GetHospital(resources);
        }

        public bool IsRunning => running;
        internal void ClearResultAfterLoad()
        { LastResult=null; NotificationErrors=new List<string>().AsReadOnly(); }

        public CombatResult PreviewBattle(string heroInstanceId, NpcEncounter encounter, bool abilitiesEnabled = true, int maxRounds = 120)
        {
            var hero = armies.FindHero(heroInstanceId) ?? throw new ArgumentException("Unknown battle hero.");
            return simulation.Resolve(BattleArmy.FromHero(hero), encounter, abilitiesEnabled, maxRounds);
        }

        public CombatResult RunBattle(string heroInstanceId, NpcEncounter encounter, bool abilitiesEnabled = true, int maxRounds = 120)
        {
            if (running) throw new InvalidOperationException("A battle is already being settled.");
            running = true;
            try
            {
                var hero = armies.FindHero(heroInstanceId) ?? throw new ArgumentException("Unknown battle hero.");
                var result = simulation.Resolve(BattleArmy.FromHero(hero), encounter, abilitiesEnabled, maxRounds);
                // Validate every state update and overflow before changing casualties, XP, or rewards.
                armies.ValidateCombatState(hero, result);
                var woundedPlan=Hospital.PrepareWounded(result);
                var resourcePlan = resources.PrepareAddition(result.Rewards.Resources);
                int fragmentBalance = result.Rewards.HeroFragments > 0
                    ? fragments.PrepareAddition(result.Rewards.FragmentHeroDefinitionId, result.Rewards.HeroFragments) : 0;
                armies.CommitCombatSilently(hero, result);
                Hospital.RestoreValidated(woundedPlan);
                bool resourceChanged = resources.CommitAdditionSilently(resourcePlan);
                if (result.Rewards.HeroFragments > 0) fragments.CommitAdditionSilently(result.Rewards.FragmentHeroDefinitionId, fragmentBalance);
                LastResult = result;
                // All domains are committed before observers run. Observer errors cannot cause a partial settlement.
                var errors = new List<string>();
                Publish(armies.NotifyChanged, errors);
                Publish(Hospital.NotifyChanged, errors);
                if (resourceChanged) Publish(resources.NotifyChanged, errors);
                if (result.Rewards.HeroFragments > 0) Publish(fragments.NotifyChanged, errors);
                if (BattleCompleted != null)
                    foreach (Action<CombatResult> handler in BattleCompleted.GetInvocationList()) Publish(() => handler(result), errors);
                NotificationErrors = errors.AsReadOnly();
                return result;
            }
            finally { running = false; }
        }
        private static void Publish(Action callback, List<string> errors)
        {
            try { callback(); }
            catch (Exception error) { errors.Add(error.GetType().Name + ": " + error.Message); }
        }
    }
}
