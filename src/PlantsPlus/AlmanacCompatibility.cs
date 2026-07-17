using CustomizeLib.BepInEx;
using HarmonyLib;
using Il2Cpp;
using Il2CppAlmanacData;
using System;
using System.Text.RegularExpressions;

namespace PlantsPlus.Core
{
    /// <summary>
    /// CustomizeLib 3.8 stores the separate introduce field, but its current
    /// LoadPlantData postfix does not copy it into the game's PlantInfo.
    /// Restore that field for Plants+ entries after every Almanac reload.
    /// </summary>
    internal static class AlmanacCompatibility
    {
        private static bool refreshLogged;

        private static readonly PlantType[] PlantsPlusTypes =
        {
            (PlantType)PlantsPlus.Plants.LotusPumpkin.LotusPumpkinID,
            (PlantType)PlantRegister.BambnutID,
            (PlantType)PlantsPlus.Plants.IcebergShroom.IcebergShroomID,
            (PlantType)PlantsPlus.Plants.WitchfirePumpkin.WitchfirePumpkinID,
            (PlantType)PlantsPlus.Plants.NuttySharpshooter.NuttySharpshooterID,
            (PlantType)PlantsPlus.Plants.InfernoTorchflower.InfernoTorchflowerID,
            (PlantType)PlantsPlus.Plants.PumpkinPodbomber.PumpkinPodbomberID,
#if ENABLE_MAGNETOPEA
            (PlantType)PlantsPlus.Plants.MagnetOPea.MagnetOPeaID,
#endif
        };

        internal static void RefreshLoadedData()
        {
            try
            {
                if (AlmanacDataLoader.plantDatas == null)
                    return;

                int refreshed = 0;

                for (int index = 0; index < PlantsPlusTypes.Length; index++)
                {
                    PlantType type = PlantsPlusTypes[index];

                    if (!CustomCore.PlantsAlmanac.ContainsKey(type) ||
                        !AlmanacDataLoader.plantDatas.ContainsKey(type))
                    {
                        continue;
                    }

                    PlantAlmanac source = CustomCore.PlantsAlmanac[type];
                    PlantInfo target = AlmanacDataLoader.plantDatas[type];

                    if (target == null)
                        continue;

                    string cleanName = Regex.Replace(
                        source.name ?? string.Empty,
                        "\\([^()]*\\)",
                        string.Empty
                    ).TrimEnd();

                    // The native title concatenates name + "(ID)" directly.
                    // One trailing space reproduces vanilla's "Name (ID)".
                    target.name = cleanName + " ";
                    target.info = source.info ?? string.Empty;
                    target.introduce = source.introduce ?? string.Empty;
                    target.cost = source.cost ?? string.Empty;
                    target.seedType = (int)type;
                    refreshed++;
                }

                if (!refreshLogged && refreshed > 0)
                {
                    refreshLogged = true;
                    Plugin.Logger.LogInfo(
                        "[Plants+] Almanac data refreshed" +
                        " | Entries = " + refreshed +
                        " | Separate mechanics + lore active"
                    );
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Plants+] Almanac refresh failed safely: " +
                    exception.Message
                );
            }
        }

        [HarmonyPatch(
            typeof(AlmanacDataLoader),
            nameof(AlmanacDataLoader.LoadPlantData)
        )]
        private static class AlmanacDataLoader_LoadPlantData_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix()
            {
                RefreshLoadedData();
            }
        }
    }
}
