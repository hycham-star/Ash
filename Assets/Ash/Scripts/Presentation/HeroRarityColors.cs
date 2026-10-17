using System;
using UnityEngine;

namespace Ash
{
    /// <summary>One shared rarity palette for every hero presentation.</summary>
    public static class HeroRarityColors
    {
        public static Color For(HeroRarity rarity)
        {
            switch(rarity)
            {
                case HeroRarity.Ordinary:return new Color32(190,195,201,255);
                case HeroRarity.Rare:return new Color32(87,176,255,255);
                case HeroRarity.Epic:return new Color32(195,133,255,255);
                case HeroRarity.Legendary:return new Color32(255,199,75,255);
                default:throw new ArgumentOutOfRangeException(nameof(rarity));
            }
        }
        public static string Text(string value,HeroRarity rarity)
            => "<color=#"+ColorUtility.ToHtmlStringRGB(For(rarity))+">"+value+"</color>";
    }
}
