using System;

namespace Ash
{
    public static class DamageCalculator
    {
        public static int CounterBasisPoints(FighterType attacker, FighterType defender)
        {
            if (!Enum.IsDefined(typeof(FighterType), attacker) || !Enum.IsDefined(typeof(FighterType), defender))
                throw new ArgumentOutOfRangeException(nameof(attacker));
            if (attacker == defender) return 10000;
            bool advantage = (attacker == FighterType.Melee && defender == FighterType.Biker)
                || (attacker == FighterType.Biker && defender == FighterType.Sharpshooter)
                || (attacker == FighterType.Sharpshooter && defender == FighterType.Melee);
            return advantage ? 12500 : 8000;
        }

        public static decimal Calculate(UnitData attacker, int quantity, FighterType defender,
            HeroDefinition hero = null, int heroLevel = 1, bool abilityActive = false)
        {
            if (attacker == null) throw new ArgumentNullException(nameof(attacker));
            if (quantity < 0 || quantity > 1000000) throw new ArgumentOutOfRangeException(nameof(quantity));
            int passive = 0, ability = 0;
            if (hero != null)
            {
                int bonus = hero.BonusAt(heroLevel);
                if (attacker.Definition.Type == hero.Specialization) passive = bonus;
                if (abilityActive && attacker.Definition.Type == hero.Ability.TargetType) ability = hero.Ability.PowerBonusBasisPoints;
            }
            decimal damage = (decimal)attacker.Definition.Damage * attacker.Level * quantity;
            damage = damage * (10000 + passive) / 10000;
            damage = damage * (10000 + ability) / 10000;
            damage = damage * CounterBasisPoints(attacker.Definition.Type, defender) / 10000;
            return decimal.Round(damage, 2, MidpointRounding.AwayFromZero);
        }
    }
}
