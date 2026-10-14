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

    public sealed class ArmyData
    {
        private readonly List<UnitStack> stacks = new List<UnitStack>();
        public string Id { get; }
        public HeroData OwnerHero { get; }
        public ReadOnlyCollection<UnitStack> Units { get; }
        public int Capacity => OwnerHero.Definition.CapacityAt(OwnerHero.Level);
        public int TotalUnits
        {
            get { int total = 0; foreach (var stack in stacks) total = checked(total + stack.Count); return total; }
        }
        public decimal Power
        {
            get
            {
                decimal power = 0;
                foreach (var stack in stacks)
                {
                    int bonus = stack.Unit.Definition.Type == OwnerHero.Definition.Specialization
                        ? OwnerHero.Definition.BonusAt(OwnerHero.Level) : 0;
                    power += (decimal)stack.Unit.Power * stack.Count * (10000 + bonus) / 10000;
                }
                // Round the aggregate once, independent of stack splitting or assignment order.
                return decimal.Round(power, 2, MidpointRounding.AwayFromZero);
            }
        }

        internal ArmyData(string id, HeroData ownerHero)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Army ID is required.", nameof(id));
            OwnerHero = ownerHero ?? throw new ArgumentNullException(nameof(ownerHero));
            Id = id; Units = stacks.AsReadOnly();
        }

        public int QuantityOf(string unitId, int level = 1)
        {
            foreach (var stack in stacks)
                if (stack.Unit.Definition.Id == unitId && stack.Unit.Level == level) return stack.Count;
            return 0;
        }

        // Mutation is restricted to the domain command service, which also updates the reserve.
        internal bool TryAddUnits(UnitData unit, int count)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            if ((long)TotalUnits + count > Capacity) return false;
            foreach (var stack in stacks)
                if (stack.Unit.Definition.Id == unit.Definition.Id && stack.Unit.Level == unit.Level)
                { stack.Add(count); return true; }
            stacks.Add(new UnitStack(unit, count));
            return true;
        }

        internal bool TryRemoveUnits(string unitId, int level, int count)
        {
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
