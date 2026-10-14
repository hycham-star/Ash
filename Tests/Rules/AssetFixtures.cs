using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Ash;

// Reads the flat authored fields of our assets, not a general-purpose Unity YAML importer.
internal static class AssetFixtures
{
    internal static string Root
    {
        get
        {
            var path = new DirectoryInfo(AppContext.BaseDirectory);
            while (path != null && !Directory.Exists(Path.Combine(path.FullName, "Assets", "Ash"))) path = path.Parent;
            return path?.FullName ?? throw new InvalidOperationException("Cannot locate the project fixtures.");
        }
    }

    private static Dictionary<string, string> Fields(string path)
    {
        var fields = new Dictionary<string, string>();
        bool data = false;
        foreach (var line in File.ReadLines(path))
        {
            if (line.TrimStart().StartsWith("m_EditorClassIdentifier:")) { data = true; continue; }
            if (!data || string.IsNullOrWhiteSpace(line)) continue;
            int separator = line.IndexOf(':');
            if (separator < 0) throw new InvalidOperationException("Unsupported fixture field: " + line);
            fields.Add(line.Substring(0, separator).Trim(), line.Substring(separator + 1).Trim());
        }
        return fields;
    }

    internal static HeroDefinition Hero(string path)
    {
        var f = Fields(path);
        int I(string key) => int.Parse(f[key], CultureInfo.InvariantCulture);
        float F(string key) => float.Parse(f[key], CultureInfo.InvariantCulture);
        return new HeroDefinition(f["id"], f["displayName"], f["biography"], (HeroRarity)I("rarity"), (FighterType)I("specialization"),
            new HeroAbilityDefinition(f["abilityId"], f["abilityName"], f["abilityDescription"], (FighterType)I("abilityTargetType"),
                I("abilityPowerBonusBasisPoints"), F("cooldownSeconds"), F("durationSeconds")),
            I("baseCapacity"), I("capacityPerLevel"), I("baseBonusBasisPoints"), I("bonusPerLevelBasisPoints"),
            I("maxLevel"), I("baseXpRequired"), I("xpPerLevel"));
    }

    internal static UnitDefinition Fighter(string path)
    {
        var f = Fields(path);
        int I(string key) => int.Parse(f[key], CultureInfo.InvariantCulture);
        return new UnitDefinition(f["id"], f["displayName"], (FighterType)I("type"), I("health"), I("damage"),
            float.Parse(f["speed"], CultureInfo.InvariantCulture), I("basePower"));
    }
}
