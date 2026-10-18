using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace PlantsPlus.Core
{
    // ClassicMix enumerates basicCardHead2 and queries recipes using realType.
    // A card in basicCardHead alone cannot provide an ingredient slot there.
    [HarmonyPatch(typeof(AlmanacPlantMenu), nameof(AlmanacPlantMenu.ClassicMix))]
    internal static class ClassicFusionSlots
    {
        internal static bool CreatingSlot;

        [HarmonyPrefix]
        private static void Prefix(AlmanacPlantMenu __instance, PlantType thePlantType)
        {
            Transform grid = __instance.basicCardHead2;
            Transform basics = __instance.basicCardHead;
            if (grid == null || basics == null)
                return;

            // Include Electronion too: the old SetCard type swap replaced its
            // second-grid slot with Lob-shroom during native initialization.
            int[] ingredients = {
                Plants.BorealOrchid.BorealOrchidID, Plants.LobShroom.ID,
                (int)PlantType.ElectricOnion, Plants.BounceShroom.ID
            };
            foreach (int id in ingredients)
            {
                AlmanacCardUI slot = Find(grid, id);
                if (slot == null)
                {
                    CreatingSlot = true;
                    try { __instance.SetCard(grid, (PlantType)id); }
                    finally { CreatingSlot = false; }
                    slot = Find(grid, id);
                    Plugin.Logger.LogInfo("[Classic fusion slots] Added ingredient=" +
                        id + " | present=" + (slot != null));
                }
                if (slot == null)
                    continue;

                // Preserve the adventure order in the native second grid.
                AlmanacCardUI source = Find(basics, id);
                if (source != null)
                    slot.transform.SetSiblingIndex(source.transform.GetSiblingIndex());
            }

            if ((int)thePlantType == Plants.LobShroom.ID ||
                (int)thePlantType == Plants.BounceShroom.ID ||
                thePlantType == PlantType.ElectricOnion)
            {
                foreach (int id in ingredients)
                {
                    PlantType result;
                    bool found = MixData.TryGetMix(thePlantType, (PlantType)id,
                        out result, false);
                    Plugin.Logger.LogInfo("[Classic fusion slots] Selected=" +
                        (int)thePlantType + " | ingredient=" + id +
                        " | slot=" + (Find(grid, id) != null) +
                        " | recipe=" + found + " | result=" + (int)result);
                }
            }
        }

        private static AlmanacCardUI Find(Transform grid, int id)
        {
            for (int i = 0; i < grid.childCount; i++)
            {
                AlmanacCardUI card = grid.GetChild(i).GetComponent<AlmanacCardUI>();
                if (card != null && (int)card.realType == id)
                    return card;
            }
            return null!;
        }
    }
}
