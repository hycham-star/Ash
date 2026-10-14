using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Ash
{
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private ResourceAmount[] initialResources = Array.Empty<ResourceAmount>();
        [SerializeField] private HeroConfig[] heroDefinitions = Array.Empty<HeroConfig>();
        [SerializeField] private UnitConfig[] unitDefinitions = Array.Empty<UnitConfig>();
        public ResourceManager Resources { get; private set; }
        public ReadOnlyCollection<HeroData> Heroes { get; private set; }
        public ReadOnlyCollection<UnitData> Units { get; private set; }
        public ArmyData Army { get; private set; }
        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            try { Initialize(); }
            catch (Exception error) { Debug.LogError("Phase 1 initialization failed: " + error, this); enabled = false; }
        }

        // Scene ownership and explicit references instead of a global singleton or automatic spawning.
        public void Initialize()
        {
            if (IsInitialized) return;
            var resources = new ResourceManager(initialResources);
            var heroes = new List<HeroData>();
            var units = new List<UnitData>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var config in heroDefinitions)
            {
                if (config == null) throw new InvalidOperationException("Missing hero asset.");
                var definition = config.CreateDefinition();
                if (!ids.Add(definition.Id)) throw new InvalidOperationException("Duplicate hero ID: " + definition.Id);
                heroes.Add(new HeroData("owned-" + definition.Id, definition));
            }
            ids.Clear();
            foreach (var config in unitDefinitions)
            {
                if (config == null) throw new InvalidOperationException("Missing unit asset.");
                var definition = config.CreateDefinition();
                if (!ids.Add(definition.Id)) throw new InvalidOperationException("Duplicate unit ID: " + definition.Id);
                units.Add(new UnitData(definition));
            }
            Resources = resources; Heroes = heroes.AsReadOnly(); Units = units.AsReadOnly();
            Army = new ArmyData("test-army");
            IsInitialized = true;
        }
    }
}
