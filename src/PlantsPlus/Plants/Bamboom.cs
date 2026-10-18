using System;
using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace PlantsPlus.Plants
{
    // Keep the native Bamboo component and its contact/HP mechanics.
    public sealed class Bamboom : MonoBehaviour
    {
        public const int ID = 6040;
        public const int Heal = 500;
        public const int ExplosionDamage = 1800;
        internal bool exploded;
        public Bamboom(IntPtr pointer) : base(pointer) { }
        public void OnEnable() { exploded = false; }

        internal static bool IsOnBoard(Plant plant)
        {
            var plants = Board.Instance?.boardEntity?.plantArray;
            if (plant == null || plants == null || !plant.gameObject.activeInHierarchy) return false;
            for (int i = 0; i < plants.Count; i++)
                if (plants[i] != null && plants[i].Pointer == plant.Pointer) return true;
            return false;
        }
    }

    [HarmonyPatch(typeof(BombCherry), nameof(BombCherry.Explode), new Type[] { typeof(float) })]
    internal static class BamboomCherryHealing
    {
        [HarmonyPostfix]
        private static void Postfix(BombCherry __instance)
        {
            if (__instance == null || __instance.bulletFromZombie ||
                __instance.fromType != PlantType.CherryBomb) return;
            var plants = Board.Instance?.boardEntity?.plantArray;
            if (plants == null) return;
            int column = Lawnf.GetColumnFromX(__instance.bombPosition.x);
            for (int i = 0; i < plants.Count; i++)
            {
                Plant plant = plants[i];
                if (plant == null || (int)plant.thePlantType != Bamboom.ID ||
                    plant.dying || plant.thePlantHealth <= 0 || !plant.gameObject.activeInHierarchy ||
                    Math.Abs(plant.thePlantRow - __instance.bombRow) > 1 ||
                    Math.Abs(plant.thePlantColumn - column) > 1) continue;
                int heal = Math.Min(Bamboom.Heal, plant.thePlantMaxHealth - plant.thePlantHealth);
                if (heal > 0) plant.Recover(heal, DamageType.Normal, true, false);
            }
        }
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.Die))]
    internal static class BamboomDeathExplosion
    {
        [HarmonyPrefix]
        private static void Prefix(Plant __instance, Plant.DieReason __0)
        {
            if (__instance == null || (int)__instance.thePlantType != Bamboom.ID ||
                !Bamboom.IsOnBoard(__instance)) return;
            // Fusion, upgrade, shovel, steal and hide are removals, not deaths.
            if (__0 != Plant.DieReason.Default && __0 != Plant.DieReason.BySelf &&
                __0 != Plant.DieReason.ByFreeze && __0 != Plant.DieReason.Crash &&
                __0 != Plant.DieReason.CrashInWater && __0 != Plant.DieReason.Wheel) return;
            var runtime = __instance.GetComponent<Bamboom>() ?? __instance.gameObject.AddComponent<Bamboom>();
            if (runtime.exploded) return;
            runtime.exploded = true;
            Board.Instance.boardAction.CreateCherryExplode(__instance.transform.position,
                __instance.thePlantRow, CherryBombType.Normal, Bamboom.ExplosionDamage,
                (PlantType)Bamboom.ID, null, true);
        }
    }
}
