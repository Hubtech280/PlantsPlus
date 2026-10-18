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
        private static bool borealMechanicLogged;
        private static PlantsPlusLanguage? lastWindowLanguage;
        private const string BorealMechanicTitleChinese = "植物机制-极地系统";
        private const string BorealMechanicTitleEnglish = "Plant System - Boreal System";
        private const string BorealMechanicTitleFrench = "Mécanique végétale - Système boréal";
        private const string BorealMechanicTextChinese =
            "<color=#002461>极光兰</color>及其融合植物会释放极地之波。\n\n" +
            "波经过的行内，夜间植物获得持续10秒的强化：" +
            "伤害、韧性、攻击速度、生产速度和移动速度提高35%。\n\n" +
            "<color=#002461>路灯花</color>也能受到强化，" +
            "此时会获得极地点数。";
        private const string BorealMechanicTextEnglish =
            "<color=#002461>Boreal Orchid</color> and its fusions release Boreal Waves.\n\n" +
            "Night plants in the affected lane gain 35% damage, Toughness, attack speed, " +
            "production speed and movement speed for 10 seconds.\n\n" +
            "<color=#002461>Plantern</color> also receives the boost and gains Boreal Points.";
        private const string BorealMechanicTextFrench =
            "<color=#002461>L'Orchidée Boréale</color> et ses fusions émettent des vagues boréales.\n\n" +
            "Pendant 10 s, les plantes nocturnes de la ligne gagnent 35 % de dégâts, de robustesse, " +
            "de cadence de tir, de production et de vitesse de déplacement.\n\n" +
            "<color=#002461>Planterne</color> reçoit aussi le bonus et gagne des points boréaux.";

        private static readonly PlantType[] PlantsPlusTypes =
        {
            (PlantType)PlantsPlus.Plants.Ceasarweed.CeasarweedID,
            (PlantType)PlantsPlus.Plants.SolarFirnace.SolarFirnaceID,
            (PlantType)PlantsPlus.Plants.LotusPumpkin.LotusPumpkinID,
            (PlantType)PlantRegister.BambnutID,
            (PlantType)PlantsPlus.Plants.Bamboom.ID,
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
            (PlantType)PlantsPlus.Plants.BorealOrchid.BorealOrchidID,
            (PlantType)PlantsPlus.Plants.BounceShroom.ID,
            (PlantType)PlantsPlus.Plants.BumpingShroom.ID,
            (PlantType)PlantsPlus.Plants.SparkShroom.ID,
            (PlantType)PlantsPlus.Plants.SuperaquaShroom.ID,
            (PlantType)PlantsPlus.Plants.PulseShroom.ID,
            (PlantType)PlantsPlus.Plants.SolsticeShroom.ID,
            (PlantType)PlantsPlus.Plants.SawMyPot.ID,
            (PlantType)PlantsPlus.Plants.VortexPot.ID,
#if ENABLE_MAGNETOPEA
            (PlantType)PlantsPlus.Plants.MagnetOPea.MagnetOPeaID,
#endif
        };

        private static readonly AlmanacEntry[] PlantsPlusEntries =
        {
            AlmanacContent.Ceasarweed,
            AlmanacContent.SolarFirnace,
            AlmanacContent.LotusPumpkin,
            AlmanacContent.Bambnut,
            AlmanacContent.Bamboom,
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
            AlmanacContent.BorealOrchid,
            AlmanacContent.BounceShroom,
            AlmanacContent.BumpingShroom,
            AlmanacContent.SparkShroom,
            AlmanacContent.SuperaquaShroom,
            AlmanacContent.PulseShroom,
            AlmanacContent.SolsticeShroom,
            AlmanacContent.SawMyPot,
            AlmanacContent.VortexPot,
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
                    AlmanacEntry entry = AlmanacContent.ForCurrentLanguage(
                        PlantsPlusEntries[index]);

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

        internal static void RefreshMechanicsData()
        {
            try
            {
                PlantsPlusLanguage language = LocalizationBridge.Current;
                string title = language switch
                {
                    PlantsPlusLanguage.English => BorealMechanicTitleEnglish,
                    PlantsPlusLanguage.French => BorealMechanicTitleFrench,
                    _ => BorealMechanicTitleChinese
                };
                string text = language switch
                {
                    PlantsPlusLanguage.English => BorealMechanicTextEnglish,
                    PlantsPlusLanguage.French => BorealMechanicTextFrench,
                    _ => BorealMechanicTextChinese
                };
                var details = AlmanacDataLoader.almanacData?.details;
                if (details == null)
                    return;

                int insertionIndex = -1;
                Details? template = null;
                for (int index = 0; index < details.Count; index++)
                {
                    Details detail = details[index];
                    if (detail == null)
                        continue;

                    if (detail.title == BorealMechanicTitleChinese ||
                        detail.title == BorealMechanicTitleEnglish ||
                        detail.title == BorealMechanicTitleFrench)
                    {
                        detail.title = title;
                        detail.text = text;
                        return;
                    }

                    if (detail.title == "植物机制-传送状态" ||
                        detail.title == "Plant System - Chronoshift")
                    {
                        insertionIndex = index + 1;
                        template = detail;
                    }
                    else if (template == null &&
                        detail.title != null &&
                        (detail.title.StartsWith("植物机制-") ||
                         detail.title.StartsWith("Plant System - ")))
                    {
                        insertionIndex = index + 1;
                        template = detail;
                    }
                }

                if (template == null)
                    return;

                Details boreal = new Details
                {
                    type = template.type,
                    title = title,
                    text = text,
                    link = new Il2CppSystem.Collections.Generic.List<Link>()
                };
                details.Insert(insertionIndex, boreal);

                if (!borealMechanicLogged)
                {
                    borealMechanicLogged = true;
                    Plugin.Logger.LogInfo(
                        "[Boreal System] Added to native Mechanics Almanac"
                    );
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Boreal System] Mechanics Almanac insertion " +
                    "failed safely: " + exception.Message
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

        [HarmonyPatch(typeof(AlmanacPlantWindow), "SetPlant")]
        private static class AlmanacPlantWindow_Language_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix()
            {
                PlantsPlusLanguage current = LocalizationBridge.Current;
                if (lastWindowLanguage == current)
                    return;

                lastWindowLanguage = current;
                RefreshLoadedData();
                RefreshMechanicsData();
                Plugin.Logger.LogInfo(
                    "[Localization] PvZ Fusion+ Almanac language = " +
                    current + ".");
            }
        }

        [HarmonyPatch(
            typeof(AlmanacDataLoader),
            nameof(AlmanacDataLoader.LoadDetailsData)
        )]
        private static class AlmanacDataLoader_LoadDetailsData_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix()
            {
                RefreshMechanicsData();
            }
        }

        [HarmonyPatch(
            typeof(AlmanacSelectMenu),
            nameof(AlmanacSelectMenu.InitMenu)
        )]
        private static class AlmanacSelectMenu_InitMenu_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix()
            {
                RefreshMechanicsData();
            }
        }
    }
}
