using System;
using System.Reflection;

namespace PlantsPlus.Core
{
    internal enum PlantsPlusLanguage
    {
        Chinese,
        English,
        French
    }

    // The translator is optional. Do not reference its DLL at build time:
    // the same mod must also load in the unmodified Chinese game.
    internal static class LocalizationBridge
    {
        internal static string Select(string chinese, string english, string french) =>
            Current switch
            {
                PlantsPlusLanguage.Chinese => chinese,
                PlantsPlusLanguage.French => french,
                _ => english
            };

        internal static PlantsPlusLanguage Current
        {
            get
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (!string.Equals(
                        assembly.GetName().Name,
                        "PvZ_Fusion_Translator",
                        StringComparison.OrdinalIgnoreCase))
                        continue;

                    try
                    {
                        Type? utils = assembly.GetType(
                            "PvZ_Fusion_Translator.Utils", false);
                        FieldInfo? language = utils?.GetField(
                            "Language", BindingFlags.Public |
                            BindingFlags.Static);
                        string? selected = language?.GetValue(null)?.ToString();
                        return selected switch
                        {
                            "French" => PlantsPlusLanguage.French,
                            "Chinese_cn" => PlantsPlusLanguage.Chinese,
                            _ => PlantsPlusLanguage.English
                        };
                    }
                    catch (Exception exception)
                    {
                        Plugin.Logger.LogWarning(
                            "[Localization] Translator language lookup failed: " +
                            exception.Message);
                        return PlantsPlusLanguage.English;
                    }
                }

                return PlantsPlusLanguage.Chinese;
            }
        }
    }
}
