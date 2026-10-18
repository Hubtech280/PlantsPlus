using System;
using System.Reflection;
using System.Text.RegularExpressions;
using PlantsPlus.Core;

namespace PlantsPlus.Core
{
    internal enum PlantsPlusLanguage { Chinese, English, French }
    internal static class LocalizationBridge
    {
        internal static PlantsPlusLanguage Current { get; set; }
    }
}

internal static class Program
{
    static int Main()
    {
        int count = 0;
        var approvedNames = new System.Collections.Generic.Dictionary<string, string>
        {
            ["SolarSharpshooter"] = "Epicéa Tireur Solaire",
            ["SakuraSharpshooter"] = "Sakura Tireur d'Explosif",
            ["SeaSharpshooter"] = "Epicéa Tireur des Mers",
            ["Ceasarweed"] = "Ortie de César",
            ["IceLordCactus"] = "Cactus Rois du Givre",
            ["BomberDrone"] = "Drone Bombardeur",
            ["FrostbiteDrone"] = "Drone Givré",
            ["PumpkinPodbomber"] = "Citrouille Grenadière",
            ["BorealOrchid"] = "Orchidée Boréale",
            ["SauerkrautPult"] = "Lance-choucroute",
            ["BumpingShroom"] = "Champi-secousse",
            ["IcebergShroom"] = "Champi-iceberg",
            ["SparkShroom"] = "Champi-éclaire"
        };
        foreach (var field in typeof(AlmanacContent).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType != typeof(AlmanacEntry)) continue;
            count++;
            var source = (AlmanacEntry)field.GetValue(null)!;
            foreach (var language in Enum.GetValues<PlantsPlusLanguage>())
            {
                LocalizationBridge.Current = language;
                var entry = AlmanacContent.ForCurrentLanguage(source);
                string flavor = Regex.Replace(entry.Introduce, "<[^>]+>", "");
                // Safety ceiling only, not an editorial target. Stories need
                // room for setup, voice and atmosphere.
                if (flavor.Length > 700)
                    throw new Exception(field.Name + ": flavor text too long");
                if (language == PlantsPlusLanguage.French &&
                    approvedNames.TryGetValue(field.Name, out string? approved) && entry.Name != approved)
                    throw new Exception(field.Name + ": expected name " + approved + ", got " + entry.Name);
                if (language == PlantsPlusLanguage.French && Regex.IsMatch(entry.Name, @"-\p{Lu}"))
                    throw new Exception(field.Name + ": uppercase letter after hyphen: " + entry.Name);
                string plain = Regex.Replace(entry.Info + entry.Introduce + entry.Cost, "<[^>]+>", "");
                if (string.IsNullOrWhiteSpace(entry.Name) || string.IsNullOrWhiteSpace(entry.Info) || string.IsNullOrWhiteSpace(entry.Introduce))
                    throw new Exception(field.Name + ": empty field");
                if (language == PlantsPlusLanguage.French &&
                    Regex.IsMatch(plain, @"\b(Special|Toughness|Damage|Fusion Recipe|Conversion Recipe|Usage Condition|native|shroom|Sharpshooter|Every|Medium)\b", RegexOptions.IgnoreCase))
                    throw new Exception(field.Name + ": English in French: " + plain);
                if (language == PlantsPlusLanguage.Chinese && Regex.IsMatch(plain, @"\b[A-Za-z]{3,}\b"))
                    throw new Exception(field.Name + ": untranslated Chinese field: " + plain);
                int opens = Regex.Matches(entry.Info + entry.Introduce + entry.Cost, "<color[=>]").Count;
                int closes = Regex.Matches(entry.Info + entry.Introduce + entry.Cost, "</color>").Count;
                if (opens != closes) throw new Exception(field.Name + ": unbalanced colours");
            }
        }
        if (count != 42) throw new Exception("Expected 42 entries, got " + count);
        foreach (string fieldName in approvedNames.Keys)
            if (typeof(AlmanacContent).GetField(fieldName) == null)
                throw new Exception("Unknown approved-name field: " + fieldName);
        Console.WriteLine("PASS: 42 entries x 3 languages; checked fields, language leaks, colour tags, flavor text safety ceiling and " + approvedNames.Count + " expected names.");
        return 0;
    }
}
