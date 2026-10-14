using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ash
{
    public sealed class UnitStack
    {
        public UnitData Unit { get; }
        public int Count { get; private set; }
        internal UnitStack(UnitData unit, int count) { Unit = unit; Count = count; }
        internal void Add(int count) { Count = checked(Count + count); }
        internal void Remove(int count) { Count -= count; }
    }

    /// <summary>Composition only: no battles, marching, rewards, or training.</summary>
    public sealed class ArmyData
    {
        private readonly List<UnitStack> stacks = new List<UnitStack>();
        public string Id { get; }
        public HeroData Commander { get; private set; }
        public ReadOnlyCollection<UnitStack> Units { get; }
        public int TotalUnits
        {
            get { int total = 0; foreach (var stack in stacks) total = checked(total + stack.Count); return total; }
        }

        public ArmyData(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Army ID is required.", nameof(id));
            Id = id; Units = stacks.AsReadOnly();
        }

        public void AssignCommander(HeroData hero) { Commander = hero ?? throw new ArgumentNullException(nameof(hero)); }
        public void ClearCommander() { Commander = null; }

        public void AddUnits(UnitData unit, int count)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            // Validate army-wide overflow before mutating a stack.
            checked { _ = TotalUnits + count; }
            foreach (var stack in stacks)
                if (stack.Unit.Definition.Id == unit.Definition.Id && stack.Unit.Level == unit.Level)
                { stack.Add(count); return; }
            stacks.Add(new UnitStack(unit, count));
        }

        public bool TryRemoveUnits(string unitId, int level, int count)
        {
            if (string.IsNullOrWhiteSpace(unitId)) throw new ArgumentException("Unit ID is required.", nameof(unitId));
            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            for (int i = 0; i < stacks.Count; i++)
            {
                var stack = stacks[i];
                if (stack.Unit.Definition.Id != unitId || stack.Unit.Level != level) continue;
                if (stack.Count < count) return false;
                stack.Remove(count);
                if (stack.Count == 0) stacks.RemoveAt(i);
                return true;
            }
            return false;
        }
    }
}
