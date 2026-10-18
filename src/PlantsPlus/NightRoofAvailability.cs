using HarmonyLib;
using Il2Cpp;

namespace PlantsPlus.Core
{
    internal static class NightRoofAvailability
    {
        internal static bool IsStarter(PlantType type) => type == PlantType.ElectricOnion ||
            (int)type == Plants.BorealOrchid.BorealOrchidID ||
            (int)type == Plants.LobShroom.ID || (int)type == Plants.BounceShroom.ID;

        // Adventure's mission/plant progression gate is separate from the
        // generic PlantDataManager unlock flag already repaired by this mod.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AdvantureConfig), nameof(AdvantureConfig.CheckPlantUnlock))]
        private static void Unlock(PlantType plantType, ref bool __result)
        {
            if (IsStarter(plantType)) __result = true;
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(CardUI), "LevelLim")]
        private static void LevelLimit(CardUI __instance)
        {
            if (!IsStarter(__instance.thePlantType) || __instance.onCardBank ||
                SeedLibrary.Instance == null || Board.Instance == null ||
                Board.Instance.boardTag.evolutionWar ||
                (GameAPP.theBoardType != LevelType.Advanture &&
                 GameAPP.theBoardType != LevelType.NewAdvanture &&
                 GameAPP.theBoardType != LevelType.Survival)) return;
            __instance.isAvailable = true;
            __instance.disabled = false;
        }
    }
}
