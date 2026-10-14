using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
    internal static NpcEncounter Encounter(string path)
    {
        var text = File.ReadAllText(path);
        string Field(string name)
        {
            var match = Regex.Match(text, @"^  " + name + @": (.+)$", RegexOptions.Multiline);
            if (!match.Success) throw new InvalidOperationException("Missing encounter field: " + name);
            return match.Groups[1].Value.Trim();
        }
        int I(string name) => int.Parse(Field(name), CultureInfo.InvariantCulture);
        var units = Directory.GetFiles(Path.Combine(Root, "Assets/Ash/Data/Units"), "*.asset")
            .ToDictionary(p => Regex.Match(File.ReadAllText(p + ".meta"), @"guid: (\w+)").Groups[1].Value, p => p);
        var stacks = new List<BattleFighter>();
        foreach (Match match in Regex.Matches(text, @"- fighter: \{fileID: 11400000, guid: (\w+), type: 2\}\s+quantity: (\d+)"))
            stacks.Add(new BattleFighter(new UnitData(Fighter(units[match.Groups[1].Value])), int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture)));
        var resources = new List<ResourceAmount>();
        foreach (Match match in Regex.Matches(text, @"- Type: (\d+)\s+Amount: (\d+)"))
            resources.Add(new ResourceAmount((ResourceType)int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture)));
        return new NpcEncounter(Field("id"), Field("displayName"), new BattleArmy(Field("id"), stacks),
            I("victoryXp"), resources, I("victoryFragments"), I("defeatXp"));
    }

}
