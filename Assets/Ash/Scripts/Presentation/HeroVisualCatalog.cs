using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ash
{
    /// <summary>Presentation bindings use permanent hero IDs, independently of roster order and save data.</summary>
    public static class HeroVisualCatalog
    {
        public static IReadOnlyDictionary<string,string> Heroes { get; } = new ReadOnlyDictionary<string,string>(
            new Dictionary<string,string>(StringComparer.Ordinal)
            {
                {"mara-flint","MaraFlint"}, {"jonas-reed","JonasReed"},
                {"sera-vale","SeraVale"}, {"bram-holt","BramHolt"},
                {"ilyra-voss","IlyraVoss"}, {"kade-rook","KadeRook"},
                {"asha-sol","AshaSol"}, {"orin-ashcroft","OrinAshcroft"}
            });
        public static string ModelFor(string heroId)
        {
            if(heroId==null || !Heroes.TryGetValue(heroId,out var model))
                throw new ArgumentException("Missing hero visual binding: "+heroId,nameof(heroId));
            return model;
        }
        public static bool IsCharacterModel(string name)
        {
            if(name=="Scavenger" || name=="Engineer" || name=="Sentry")return true;
            foreach(var model in Heroes.Values)if(model==name)return true;
            return false;
        }
    }
}
