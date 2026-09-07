using CustomizeLib.MelonLoader;
using HarmonyLib;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class CherryCabbageBootstrap
    {
        private static bool registered;
        internal static void OnStart()
        {
            if (registered) return;
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var bundle = CustomCore.GetAssetBundle(assembly, "PlantsPlus.Resources.AssetBundles.cherry_cabbage");
                var bullets = CustomCore.GetAssetBundle(assembly, "PlantsPlus.Resources.AssetBundles.bullet_cherrycabbage");
                var prefab = bundle?.GetAsset<GameObject>("CabbagepultPrefab");
                var preview = bundle?.GetAsset<GameObject>("CabbagepultPreview");
                var projectile = bullets?.GetAsset<GameObject>("Bullet_CherryCabbage");
                if (bundle == null || prefab == null || preview == null || projectile == null)
                    throw new InvalidOperationException("Cherry Cabbage plant, preview or projectile prefab missing.");
                V11PlantsBootstrap.IsolateAnimationClips(bundle, prefab, "Cherry Cabbage", "cherrycabbage_idle", "cherrycabbage_shoot");
                // Retain the authored Sprite, collider, shadow and scale without nesting a skin.
                CustomCore.RegisterCustomBullet<Bullet_cabbage, Plants.CherryCabbageProjectile>(
                    (BulletType)Plants.CherryCabbage.ID, projectile);
                CustomCore.RegisterCustomPlant<Cabbage, Plants.CherryCabbage>(
                    Plants.CherryCabbage.ID, prefab, preview,
                    new List<(int, int)> {
                        ((int)PlantType.CherryBomb, (int)PlantType.Cabbagepult),
                        ((int)PlantType.Cabbagepult, (int)PlantType.CherryBomb)
                    }, 3f, 0f, 40, 300, 15f, 250);
                var entry = AlmanacContent.CherryCabbage;
                CustomCore.AddPlantAlmanacStrings((PlantType)Plants.CherryCabbage.ID,
                    entry.Name, entry.Info, entry.Introduce, 250);
                registered = true;
                Plugin.Logger.LogInfo("[Cherry Cabbage] Registered plant and custom cabbage projectile (6030).");
            }
            catch (Exception e) { Plugin.Logger.LogError("[Cherry Cabbage] Registration failed: " + e); }
        }
        internal static void Configure(GameObject prefab)
        {
            var plant = prefab.GetComponent<Cabbage>();
            if (plant == null) return;
            V11PlantsBootstrap.EnsureShooterRuntimeReferences(plant, "Cherry Cabbage");
            V11PlantsBootstrap.ApplyNativeShooterControllerWithLocalClips(prefab, "Cherry Cabbage", PlantType.Cabbagepult);
        }
        internal static void OnGameInit()
        {
            var prefabs = GameAPP.resourcesManager?.plantPrefabs;
            if (prefabs != null && prefabs.ContainsKey((PlantType)Plants.CherryCabbage.ID))
                Configure(prefabs[(PlantType)Plants.CherryCabbage.ID]);
            AlmanacCompatibility.RefreshLoadedData();
        }
    }
}

namespace PlantsPlus.Plants
{
    public sealed class CherryCabbage : MonoBehaviour
    {
        public const int ID = 6030;
        public CherryCabbage(IntPtr pointer) : base(pointer) { }
        public void Start() { Core.CherryCabbageBootstrap.Configure(gameObject); }
        // Zero-based column: zero at the house, nine at the far edge.
        internal static float Proximity(int column) => 1f - Mathf.Clamp(column, 0, 9) / 9f;
        internal static int DamageAt(int column) => Mathf.RoundToInt(40f + 120f * Proximity(column));
        internal static float ChanceAt(int column) => 0.15f + 0.45f * Proximity(column);
    }

    public sealed class CherryCabbageProjectile : MonoBehaviour
    {
        private bool consumed;
        private static bool firstImpactLogged;
        public CherryCabbageProjectile(IntPtr pointer) : base(pointer) { }
        public void OnEnable() { consumed = false; }
        internal void Impact(Bullet bullet, Vector2 position, int row)
        {
            // HitZombie can invoke HitLand: one roll per pooled projectile.
            if (consumed) return;
            consumed = true;
            int column = Lawnf.GetColumnFromX(position.x);
            int damage = CherryCabbage.DamageAt(column);
            bullet.Damage = damage;
            bool explode = UnityEngine.Random.value < CherryCabbage.ChanceAt(column);
            if (explode && Board.Instance?.boardAction != null)
                Board.Instance.boardAction.CreateCherryExplode(position, row,
                    CherryBombType.Bullet, damage, (PlantType)CherryCabbage.ID, null, true);
            if (!firstImpactLogged)
            {
                firstImpactLogged = true;
                Plugin.Logger.LogInfo("[Cherry Cabbage] Native impact callback | column=" + (column + 1) +
                    " | damage=" + damage + " | explosion=" + explode);
            }
        }
    }

    internal static class CherryCabbagePatches
    {
        [HarmonyPatch(typeof(Cabbage), "GetBulletType")]
        private static class ProjectileType
        {
            [HarmonyPostfix]
            private static void Postfix(Cabbage __instance, ref BulletType __result)
            {
                if ((int)__instance.thePlantType == CherryCabbage.ID)
                    __result = (BulletType)CherryCabbage.ID;
                else if ((int)__instance.thePlantType == LobShroom.ID)
                    __result = BulletType.Bullet_puff;
            }
        }
        [HarmonyPatch(typeof(Bullet_cabbage), "HitZombie")]
        private static class HitZombie
        {
            [HarmonyPrefix]
            private static void Prefix(Bullet_cabbage __instance, Zombie zombie)
            {
                if (zombie == null) return;
                var marker = __instance.GetComponent<CherryCabbageProjectile>();
                if (marker != null)
                    marker.Impact(__instance, zombie.ColliderCenter, zombie.theZombieRow);
            }
        }
        [HarmonyPatch(typeof(Bullet_cabbage), nameof(Bullet_cabbage.HitLand))]
        private static class HitLand
        {
            [HarmonyPrefix]
            private static void Prefix(Bullet_cabbage __instance)
            {
                var marker = __instance.GetComponent<CherryCabbageProjectile>();
                if (marker == null) return;
                var target = __instance.targetZombie;
                // Native lobbed shots can reach HitLand before HitZombie. Use
                // the living target's body center, but never move a missed shot
                // to a distant target or a different lane.
                if (target != null && target.Alive &&
                    target.theZombieRow == __instance.theBulletRow &&
                    Mathf.Abs(target.ColliderCenter.x - __instance.transform.position.x) <= 1f)
                    marker.Impact(__instance, target.ColliderCenter, target.theZombieRow);
                else
                    marker.Impact(__instance, __instance.transform.position, __instance.theBulletRow);
            }
        }
    }
}
