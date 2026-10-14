using System;

namespace Ash
{
    // Pure strategy rules, independent of Unity and the presentation layer.
    [Serializable]
    public sealed class Settlement
    {
        public int Supplies = 60;
        public int Salvage = 50;
        public int Squad = 3;
        public int Farms = 1;
        public int Workshops = 1;
        public int Watchtowers;
        public bool OwnsDepot;
        public int Day = 1;

        public void Produce()
        {
            Supplies += Farms * 8 + (OwnsDepot ? 12 : 0);
            Salvage += Workshops * 6;
            Day++;
        }

        public string BuildFarm()
        {
            if (Salvage < 25) return "Need 25 salvage for a farm.";
            Salvage -= 25;
            Farms++;
            return "Farm built. Food production increased.";
        }

        public string BuildWorkshop()
        {
            if (Supplies < 25 || Salvage < 20) return "Need 25 supplies and 20 salvage.";
            Supplies -= 25;
            Salvage -= 20;
            Workshops++;
            return "Workshop built. Salvage production increased.";
        }

        public string Recruit()
        {
            if (Supplies < 20) return "Need 20 supplies to recruit.";
            Supplies -= 20;
            Squad++;
            return "A survivor joined your squad.";
        }

        public string BuildWatchtower()
        {
            if (Salvage < 30) return "Need 30 salvage for a watchtower.";
            Salvage -= 30;
            Watchtowers++;
            return "Watchtower built. Your squad gains +2 strength.";
        }

        public string AttackDepot()
        {
            if (OwnsDepot) return "The water depot already belongs to Ash.";
            if (Supplies < 15) return "Need 15 supplies to launch a raid.";
            Supplies -= 15;
            if (Squad + Watchtowers * 2 < 7)
            {
                Squad = Math.Max(1, Squad - 1);
                return "The Iron Pact repelled your raid. One fighter was lost; recruit and fortify.";
            }
            OwnsDepot = true;
            Salvage += 35;
            return "Victory! Water depot captured: +12 supplies per day and 35 salvage recovered.";
        }
    }
}
