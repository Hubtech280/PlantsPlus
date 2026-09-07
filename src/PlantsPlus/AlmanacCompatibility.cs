using CustomizeLib.MelonLoader;
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
            (PlantType)PlantsPlus.Plants.NotAPea.NotAPeaID,
            (PlantType)PlantsPlus.Plants.NotAStormCommando.NotAStormCommandoID,
            (PlantType)PlantsPlus.Plants.FrostFurflower.FrostFurflowerID,
            (PlantType)PlantsPlus.Plants.Doomtronion.DoomtronionID,
            (PlantType)PlantsPlus.Plants.LichenPea.LichenPeaID,
            (PlantType)PlantsPlus.Plants.LogicBlover.LogicBloverID,
            (PlantType)PlantsPlus.Plants.SolarSharpshooter.SolarSharpshooterID,
            (PlantType)PlantsPlus.Plants.SeaBallista.SeaBallistaID,
            (PlantType)PlantsPlus.Plants.Pineshooter.PineshooterID,
            (PlantType)PlantsPlus.Plants.Icytronion.IcytronionID,
            (PlantType)PlantsPlus.Plants.SeaSharpshooter.SeaSharpshooterID,
            (PlantType)PlantsPlus.Plants.CherryStarBomber.CherryStarBomberID,
            (PlantType)PlantsPlus.Plants.ThreeBuckpeater.ThreeBuckpeaterID,
            (PlantType)PlantsPlus.Plants.SakuraSharpshooter.SakuraSharpshooterID,
            (PlantType)PlantsPlus.Plants.BomberDrone.BomberDroneID,
            (PlantType)PlantsPlus.Plants.FrostbiteDrone.ID,
            (PlantType)PlantsPlus.Plants.IceLordCactus.IceLordCactusID,
            (PlantType)PlantsPlus.Plants.ScoviliaPepper.ScoviliaPepperID,
            (PlantType)PlantsPlus.Plants.AtomrayShroom.AtomrayShroomID,
            (PlantType)PlantsPlus.Plants.SauerkrautPult.SauerkrautPultID,
            (PlantType)PlantsPlus.Plants.CherryCabbage.ID,
            (PlantType)PlantsPlus.Plants.LobShroom.ID,
#if ENABLE_MAGNETOPEA
            (PlantType)PlantsPlus.Plants.MagnetOPea.MagnetOPeaID,
#endif
        };

        private static readonly AlmanacEntry[] PlantsPlusEntries =
        {
            AlmanacContent.LotusPumpkin,
            AlmanacContent.Bambnut,
            AlmanacContent.IcebergShroom,
            AlmanacContent.WitchfirePumpkin,
            AlmanacContent.NuttySharpshooter,
            AlmanacContent.InfernoTorchflower,
            AlmanacContent.PumpkinPodbomber,
            AlmanacContent.NotAPea,
            AlmanacContent.NotAStormCommando,
            AlmanacContent.FrostFurflower,
            AlmanacContent.Doomtronion,
            AlmanacContent.LichenPea,
            AlmanacContent.LogicBlover,
            AlmanacContent.SolarSharpshooter,
            AlmanacContent.SeaBallista,
            AlmanacContent.Pineshooter,
            AlmanacContent.Icytronion,
            AlmanacContent.SeaSharpshooter,
            AlmanacContent.CherryStarBomber,
            AlmanacContent.ThreeBuckpeater,
            AlmanacContent.SakuraSharpshooter,
            AlmanacContent.BomberDrone,
            AlmanacContent.FrostbiteDrone,
            AlmanacContent.IceLordCactus,
            AlmanacContent.ScoviliaPepper,
            AlmanacContent.AtomrayShroom,
            AlmanacContent.SauerkrautPult,
            AlmanacContent.CherryCabbage,
            AlmanacContent.LobShroom,
#if ENABLE_MAGNETOPEA
            AlmanacContent.MagnetOPea,
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
                    AlmanacEntry entry = PlantsPlusEntries[index];

                    if (!CustomCore.PlantsAlmanac.ContainsKey(type) ||
                        !AlmanacDataLoader.plantDatas.ContainsKey(type))
                    {
                        continue;
                    }

                    PlantAlmanac source = CustomCore.PlantsAlmanac[type];
                    PlantInfo target = AlmanacDataLoader.plantDatas[type];

                    if (target == null)
                        continue;

                    // The native title concatenates name + "(ID)" directly.
                    // One trailing space reproduces vanilla's "Name (ID)".
                    source.name = entry.Name;
                    source.info = entry.Info;
                    source.introduce = entry.Introduce;
                    source.cost = entry.Cost;
                    target.name = entry.Name + " ";
                    target.info = entry.Info;
                    target.introduce = entry.Introduce;
                    target.cost = entry.Cost;
                    CustomCore.PlantsAlmanac[type] = source;
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
