using CustomizeLib.MelonLoader;
using HarmonyLib;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class IceLordCactusBootstrap
    {
        private static bool registered;
        private static bool nativeDataLogged;
        private static readonly HashSet<IntPtr> LivePointers = new();

        internal static void Track(Cactus? plant)
        {
            if (plant != null && plant.Pointer != IntPtr.Zero)
                LivePointers.Add(plant.Pointer);
        }

        internal static void Untrack(IntPtr pointer)
        {
            if (pointer != IntPtr.Zero)
                LivePointers.Remove(pointer);
        }

        private static bool IsIceLord(Cactus? plant)
        {
            return plant != null && plant.Pointer != IntPtr.Zero &&
                LivePointers.Contains(plant.Pointer);
        }

        internal static void OnStart()
        {
            if (registered)
                return;
            registered = true;

            try
            {
                AssetBundle? bundle = CustomCore.GetAssetBundle(
                    Assembly.GetExecutingAssembly(),
                    "PlantsPlus.Resources.AssetBundles.ice_lord_cactus"
                );
                GameObject? prefab = bundle?.GetAsset<GameObject>("IceCactusPrefab");
                GameObject? preview = bundle?.GetAsset<GameObject>("IceCactusPreview");

                if (bundle == null || prefab == null || preview == null)
                    throw new InvalidOperationException(
                        "Ice-Lord Cactus bundle, prefab or preview is missing."
                    );

                CustomCore.RegisterCustomPlant<IceCactus, Plants.IceLordCactus>(
                    Plants.IceLordCactus.IceLordCactusID,
                    prefab,
                    preview,
                    new List<(int, int)>
                    {
                        ((int)PlantType.IceCactus, Plants.IcebergShroom.IcebergShroomID),
                        (Plants.IcebergShroom.IcebergShroomID, (int)PlantType.IceCactus)
                    },
                    Plants.IceLordCactus.GroundInterval,
                    0f,
                    Plants.IceLordCactus.FallbackDamage,
                    Plants.IceLordCactus.FallbackToughness,
                    Plants.IceLordCactus.FallbackCardRecharge,
                    Plants.IceLordCactus.FallbackCardCost
                );

                AlmanacEntry almanac = AlmanacContent.IceLordCactus;
                CustomCore.AddPlantAlmanacStrings(
                    (PlantType)Plants.IceLordCactus.IceLordCactusID,
                    almanac.Name,
                    almanac.Info,
                    almanac.Introduce,
                    Plants.IceLordCactus.FallbackCardCost
                );
                OdysseyRegistration.RegisterWeak(
                    (PlantType)Plants.IceLordCactus.IceLordCactusID,
                    "Ice-Lord Cactus"
                );

                Plugin.Logger.LogInfo(
                    "[Ice-Lord Cactus] Registered | Plant ID = " +
                    Plants.IceLordCactus.IceLordCactusID +
                    " | Fusion = Ice Cactus + Iceberg-shroom"
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Ice-Lord Cactus] Registration failed safely: " + exception
                );
            }
        }

        internal static void OnGameInit()
        {
            OdysseyRegistration.RegisterWeak(
                (PlantType)Plants.IceLordCactus.IceLordCactusID,
                "Ice-Lord Cactus"
            );
            RefreshNativePlantData();
            ConfigureRegisteredPrefab();
            AlmanacCompatibility.RefreshLoadedData();
        }

        private static void RefreshNativePlantData()
        {
            try
            {
                PlantType customType = (PlantType)Plants.IceLordCactus.IceLordCactusID;
                if (!CustomCore.CustomPlants.TryGetValue(customType, out var customData))
                    return;

                PlantDataManager.PlantData nativeData =
                    PlantDataManager.GetPlantData(PlantType.IceCactus);
                if (nativeData == null || customData.PlantData == null)
                    return;

                PlantDataManager.PlantData target = customData.PlantData;
                target.thePlantType = customType;
                target.attackInterval = Plants.IceLordCactus.GroundInterval;
                target.produceInterval = nativeData.produceInterval;
                target.attackDamage = nativeData.attackDamage;
                target.maxHealth = nativeData.maxHealth;
                target.cd = nativeData.cd;
                target.cost = nativeData.cost;
                customData.PlantData = target;
                CustomCore.CustomPlants[customType] = customData;

                if (!nativeDataLogged)
                {
                    nativeDataLogged = true;
                    Plugin.Logger.LogInfo(
                        "[Ice-Lord Cactus] Native Ice Cactus data mirrored" +
                        " | Damage = " + nativeData.attackDamage +
                        " | HP = " + nativeData.maxHealth +
                        " | Cost = " + nativeData.cost
                    );
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Ice-Lord Cactus] Native data was not ready: " + exception.Message
                );
            }
        }

        private static void ConfigureRegisteredPrefab()
        {
            PlantType type = (PlantType)Plants.IceLordCactus.IceLordCactusID;
            if (GameAPP.resourcesManager == null ||
                GameAPP.resourcesManager.plantPrefabs == null ||
                !GameAPP.resourcesManager.plantPrefabs.ContainsKey(type))
                return;

            GameObject? prefab = GameAPP.resourcesManager.plantPrefabs[type];
            IceCactus? shooter = prefab?.GetComponent<IceCactus>();
            if (shooter == null)
                return;

            string bridge = V11PlantsBootstrap.EnsureShooterRuntimeReferences(
                shooter,
                "Ice-Lord Cactus"
            );
            RestoreAirShotOrigin(shooter);
            Plugin.Logger.LogInfo(
                "[Ice-Lord Cactus] Registered prefab runtime bridge = " + bridge
            );
        }

        internal static bool IsFlying(Zombie? zombie)
        {
            if (zombie == null)
                return false;
            return zombie.theStatus == ZombieStatus.Flying ||
                zombie.theStatus == ZombieStatus.Imp_fly;
        }

        internal static void RestoreAirShotOrigin(Cactus source)
        {
            Transform? airOrigin = FindDescendant(source.transform, "Shoot2");
            if (airOrigin != null)
                source.shoot2 = airOrigin;
        }

        private static Transform? FindDescendant(Transform root, string name)
        {
            if (root.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return root;

            for (int index = 0; index < root.childCount; index++)
            {
                Transform? match = FindDescendant(root.GetChild(index), name);
                if (match != null)
                    return match;
            }

            return null;
        }

        internal static Bullet? Fire(Cactus source, bool airborne)
        {
            if (airborne)
                RestoreAirShotOrigin(source);
            Transform? origin = airborne ? source.shoot2 : source.shoot;
            if (origin == null)
                origin = source.transform;

            CreateBullet? creator = CreateBullet.Instance;
            if (creator == null && Board.Instance != null)
                creator = Board.Instance.GetComponent<CreateBullet>();
            if (creator == null)
                return null;

            Vector3 position = origin.position;
            Bullet bullet = creator.SetBullet(
                position.x,
                position.y,
                source.thePlantRow,
                source.GetBulletType(),
                BulletMoveWay.MoveRight,
                false
            );
            if (bullet == null)
                return null;

            bullet.transform.position = position;
            bullet.theBulletType = source.GetBulletType();
            bullet.Damage = source.attackDamage > 0
                ? source.attackDamage
                : Plants.IceLordCactus.FallbackDamage;
            bullet.shootingLevel = source.shootingLevel;
            bullet.from = source;
            bullet.fromType = (PlantType)Plants.IceLordCactus.IceLordCactusID;
            bullet.theBulletRow = source.thePlantRow;

            Plants.IceLordProjectileMarker? marker =
                bullet.GetComponent<Plants.IceLordProjectileMarker>();
            if (marker == null)
                marker = bullet.gameObject.AddComponent<Plants.IceLordProjectileMarker>();
            marker.baseDamage = bullet.Damage;
            return bullet;
        }

        internal static void ApplyCold(Zombie zombie)
        {
            const float tolerance = 0.001f;
            float before;
            try { before = zombie.freezeSpeed; }
            catch { before = 1f; }

            bool hadFreeze;
            try { hadFreeze = zombie.HasBuff(EffectType.Freeze); }
            catch { hadFreeze = false; }

            try { zombie.SetFreeze(Plants.IceLordCactus.FreezeDuration); }
            catch { }

            float after;
            try { after = zombie.freezeSpeed; }
            catch { after = before; }

            bool hasFreeze;
            try { hasFreeze = zombie.HasBuff(EffectType.Freeze); }
            catch { hasFreeze = false; }

            if (hadFreeze || hasFreeze || after <= tolerance ||
                after < before - tolerance)
                return;

            try
            {
                EffectManager.SetEffect(
                    zombie,
                    EffectType.Cold,
                    Plants.IceLordCactus.FreezeDuration,
                    Plants.IcebergForcedSlow.SpeedMultiplier
                );
            }
            catch { }

            try
            {
                Plants.IcebergForcedSlow? slow =
                    zombie.gameObject.GetComponent<Plants.IcebergForcedSlow>();
                if (slow == null)
                    slow = zombie.gameObject.AddComponent<Plants.IcebergForcedSlow>();
                slow?.Refresh(zombie, Plants.IceLordCactus.FreezeDuration);
            }
            catch { }
        }

        [HarmonyPatch(typeof(Cactus), nameof(Cactus.Shoot1))]
        private static class Cactus_Shoot1_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static bool Prefix(Cactus __instance, ref Bullet __result)
            {
                if (!IsIceLord(__instance))
                    return true;

                __result = Fire(__instance, false)!;
                __instance.GetComponent<Plants.IceLordCactus>()?
                    .ScheduleGroundBurst(__instance);
                return false;
            }
        }

        [HarmonyPatch(typeof(Cactus), nameof(Cactus.Shoot2))]
        private static class Cactus_Shoot2_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static bool Prefix(Cactus __instance, ref Bullet __result)
            {
                if (!IsIceLord(__instance))
                    return true;
                __result = Fire(__instance, true)!;
                return false;
            }
        }

        [HarmonyPatch(typeof(Zombie), nameof(Zombie.TakeDamage))]
        private static class Zombie_TakeDamage_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(
                Zombie __instance,
                ref int theDamage,
                IDamageMaker damageFrom
            )
            {
                if (__instance == null || damageFrom == null)
                    return;
                if (!damageFrom.IsBullet(out Bullet bullet) || bullet == null)
                    return;
                Plants.IceLordProjectileMarker? marker =
                    bullet.GetComponent<Plants.IceLordProjectileMarker>();
                if (marker == null)
                    return;
                int baseDamage = marker.baseDamage > 0
                    ? marker.baseDamage
                    : Plants.IceLordCactus.FallbackDamage;
                theDamage = IsFlying(__instance) ? baseDamage * 4 : baseDamage;
            }

            [HarmonyPostfix]
            private static void Postfix(Zombie __instance, IDamageMaker damageFrom)
            {
                if (__instance == null || damageFrom == null)
                    return;
                if (!damageFrom.IsBullet(out Bullet bullet) || bullet == null)
                    return;
                if (bullet.GetComponent<Plants.IceLordProjectileMarker>() != null)
                    ApplyCold(__instance);
            }
        }

        [HarmonyPatch(typeof(GameAPP), nameof(GameAPP.LoadResources))]
        private static class GameAPP_LoadResources_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix() => RefreshNativePlantData();
        }
    }
}

namespace PlantsPlus.Plants
{
    using PlantsPlus.Core;

    public sealed class IceLordCactus : MonoBehaviour
    {
        public const int IceLordCactusID = 6026;
        public const int FallbackDamage = 20;
        public const int FallbackToughness = 300;
        public const int FallbackCardCost = 250;
        public const float FallbackCardRecharge = 15f;
        public const float GroundInterval = 2f;
        public const float AirInterval = 1f;
        public const float FreezeDuration = 8f;

        private IntPtr trackedPointer;
        private Cactus? queuedSource;
        private int remainingGroundShots;
        private float nextGroundShot;

        public IceLordCactus(IntPtr pointer) : base(pointer) { }

        public void Awake()
        {
            Cactus? plant = gameObject.GetComponent<Cactus>();
            if (plant != null)
            {
                trackedPointer = plant.Pointer;
                IceLordCactusBootstrap.Track(plant);
            }
        }

        public void Start()
        {
            Cactus? plant = gameObject.GetComponent<Cactus>();
            if (plant == null)
            {
                Plugin.Logger.LogError(
                    "[Ice-Lord Cactus] Start failed: no Cactus component."
                );
                return;
            }
            trackedPointer = plant.Pointer;
            IceLordCactusBootstrap.Track(plant);
            V11PlantsBootstrap.EnsureShooterRuntimeReferences(
                plant,
                "Ice-Lord Cactus"
            );
            IceLordCactusBootstrap.RestoreAirShotOrigin(plant);
        }

        public void Update()
        {
            Cactus? plant = gameObject.GetComponent<Cactus>();
            if (plant != null)
            {
                bool airborne = IceLordCactusBootstrap.IsFlying(plant.targetZombie);
                plant.thePlantAttackInterval = airborne ? AirInterval : GroundInterval;
            }

            if (remainingGroundShots <= 0 || queuedSource == null)
                return;
            nextGroundShot -= Time.deltaTime;
            if (nextGroundShot > 0f)
                return;

            IceLordCactusBootstrap.Fire(queuedSource, false);
            remainingGroundShots--;
            nextGroundShot = 0.12f;
        }

        internal void ScheduleGroundBurst(Cactus source)
        {
            queuedSource = source;
            remainingGroundShots = 2;
            nextGroundShot = 0.12f;
        }

        public void OnDestroy()
        {
            IceLordCactusBootstrap.Untrack(trackedPointer);
            trackedPointer = IntPtr.Zero;
        }
    }

    public sealed class IceLordProjectileMarker : MonoBehaviour
    {
        public int baseDamage;
        public IceLordProjectileMarker(IntPtr pointer) : base(pointer) { }
    }
}
