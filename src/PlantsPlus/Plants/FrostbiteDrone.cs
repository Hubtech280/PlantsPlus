using CustomizeLib.MelonLoader;
using HarmonyLib;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class FrostbiteDroneBootstrap
    {
        private static bool registered;
        private static bool nativeDataLogged;
        private static readonly HashSet<IntPtr> LivePointers = new HashSet<IntPtr>();
        [ThreadStatic] private static IntPtr activeShot;

        internal static void Track(PeaBlover? shooter)
        {
            if (ReferenceEquals(shooter, null) || shooter.Pointer == IntPtr.Zero) return;
            LivePointers.Add(shooter.Pointer);
        }

        internal static void Untrack(IntPtr pointer)
        {
            if (pointer != IntPtr.Zero) LivePointers.Remove(pointer);
        }

        private static bool IsLive(PeaBlover? shooter)
        {
            return !ReferenceEquals(shooter, null) && shooter.Pointer != IntPtr.Zero &&
                LivePointers.Contains(shooter.Pointer);
        }

        internal static void OnStart()
        {
            if (registered) return;
            registered = true;
            try
            {
                AssetBundle? bundle = CustomCore.GetAssetBundle(
                    Assembly.GetExecutingAssembly(),
                    "PlantsPlus.Resources.AssetBundles.frostbitedrone"
                );
                GameObject? prefab = bundle?.GetAsset<GameObject>("PeaBloverPrefab");
                GameObject? preview = bundle?.GetAsset<GameObject>("PeaBloverPreview");
                if (bundle == null || prefab == null || preview == null)
                    throw new InvalidOperationException("Frostbite Drone bundle, prefab or preview is missing.");

                V11PlantsBootstrap.IsolateAnimationClips(
                    bundle,
                    prefab,
                    "Frostbite Drone",
                    "idle",
                    "shoot"
                );

                CustomCore.RegisterCustomPlant<PeaBlover, Plants.FrostbiteDrone>(
                    Plants.FrostbiteDrone.ID, prefab, preview,
                    new List<(int, int)>
                    {
                        ((int)PlantType.SnowPeaShooter, (int)PlantType.Blover),
                        ((int)PlantType.Blover, (int)PlantType.SnowPeaShooter)
                    },
                    Plants.FrostbiteDrone.FallbackAttackInterval, 0f,
                    Plants.FrostbiteDrone.FallbackDamage,
                    Plants.FrostbiteDrone.FallbackToughness,
                    Plants.FrostbiteDrone.FallbackCardRecharge,
                    Plants.FrostbiteDrone.FallbackCardCost
                );

                AlmanacEntry entry = AlmanacContent.FrostbiteDrone;
                CustomCore.AddPlantAlmanacStrings(
                    (PlantType)Plants.FrostbiteDrone.ID,
                    entry.Name, entry.Info, entry.Introduce,
                    Plants.FrostbiteDrone.FallbackCardCost
                );
                EnsureFlyingClassification();
                Plugin.Logger.LogInfo(
                    "[Frostbite Drone] Registered | Plant ID = " + Plants.FrostbiteDrone.ID +
                    " | Fusion = Snow Pea + Blover | Projectile = Bullet_snowPea"
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError("[Frostbite Drone] Registration failed safely: " + exception);
            }
        }

        internal static void OnGameInit()
        {
            EnsureFlyingClassification();
            RefreshNativePlantData();
            ConfigureRegisteredPrefab();
            AlmanacCompatibility.RefreshLoadedData();
        }

        private static void EnsureFlyingClassification()
        {
            try
            {
                PlantType type = (PlantType)Plants.FrostbiteDrone.ID;
                if (TypeData.FlyingPlants != null && !TypeData.FlyingPlants.Contains(type))
                    TypeData.FlyingPlants.Add(type);
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning("[Frostbite Drone] Flying classification was not ready: " + exception.Message);
            }
        }

        private static void ConfigureRegisteredPrefab()
        {
            PlantType type = (PlantType)Plants.FrostbiteDrone.ID;
            var prefabs = GameAPP.resourcesManager?.plantPrefabs;
            if (prefabs == null || !prefabs.ContainsKey(type)) return;
            PeaBlover? shooter = prefabs[type]?.GetComponent<PeaBlover>();
            if (shooter != null)
            {
                V11PlantsBootstrap.EnsureShooterRuntimeReferences(shooter, "Frostbite Drone");
                V11PlantsBootstrap.ApplyNativeShooterControllerWithLocalClips(
                    prefabs[type],
                    "Frostbite Drone",
                    PlantType.PeaBlover
                );
            }
        }

        private static void RefreshNativePlantData()
        {
            try
            {
                PlantType type = (PlantType)Plants.FrostbiteDrone.ID;
                if (!CustomCore.CustomPlants.TryGetValue(type, out var customData)) return;
                var droneData = PlantDataManager.GetPlantData(PlantType.PeaBlover);
                var snowData = PlantDataManager.GetPlantData(PlantType.SnowPeaShooter);
                if (droneData == null || snowData == null || customData.PlantData == null) return;
                var target = customData.PlantData;
                target.thePlantType = type;
                target.attackInterval = droneData.attackInterval;
                target.produceInterval = droneData.produceInterval;
                target.attackDamage = snowData.attackDamage;
                target.maxHealth = droneData.maxHealth;
                target.cd = droneData.cd;
                target.cost = droneData.cost;
                customData.PlantData = target;
                CustomCore.CustomPlants[type] = customData;
                if (!nativeDataLogged)
                {
                    nativeDataLogged = true;
                    Plugin.Logger.LogInfo("[Frostbite Drone] Native data combined | Damage = " +
                        snowData.attackDamage + " | Interval = " + droneData.attackInterval + "s");
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning("[Frostbite Drone] Native PlantData was not ready: " + exception.Message);
            }
        }

        [HarmonyPatch(typeof(GameAPP), nameof(GameAPP.LoadResources))]
        private static class LoadResourcesPatch
        {
            [HarmonyPostfix, HarmonyPriority(Priority.Last)]
            private static void Postfix() { EnsureFlyingClassification(); RefreshNativePlantData(); }
        }

        [HarmonyPatch(typeof(PeaBlover), "Shoot1")]
        private static class ShootPatch
        {
            [HarmonyPrefix]
            private static void Prefix(PeaBlover __instance)
            {
                if (IsLive(__instance)) activeShot = __instance.Pointer;
            }
            [HarmonyPostfix] private static void Postfix() { activeShot = IntPtr.Zero; }
            [HarmonyFinalizer]
            private static Exception? Finalizer(Exception? __exception)
            {
                activeShot = IntPtr.Zero;
                return __exception;
            }
        }

        [HarmonyPatch(typeof(CreateBullet), nameof(CreateBullet.SetBullet),
            new Type[] { typeof(float), typeof(float), typeof(int), typeof(BulletType), typeof(BulletMoveWay), typeof(bool) })]
        private static class BulletPatch
        {
            [HarmonyPrefix]
            private static void Prefix(ref BulletType theBulletType)
            {
                if (activeShot != IntPtr.Zero && LivePointers.Contains(activeShot))
                    theBulletType = BulletType.Bullet_snowPea;
            }
        }

        [HarmonyPatch(typeof(TypeMgr), nameof(TypeMgr.FlyingPlants))]
        private static class FlyingPatch
        {
            [HarmonyPostfix]
            private static void Postfix(PlantType thePlantType, ref bool __result)
            {
                if ((int)thePlantType == Plants.FrostbiteDrone.ID) __result = true;
            }
        }
    }
}

namespace PlantsPlus.Plants
{
    using PlantsPlus.Core;

    public sealed class FrostbiteDrone : MonoBehaviour
    {
        public const int ID = 6031;
        public const int FallbackDamage = 20;
        public const int FallbackToughness = 300;
        public const int FallbackCardCost = 225;
        public const float FallbackCardRecharge = 15f;
        public const float FallbackAttackInterval = 1.5f;
        private IntPtr trackedPointer;

        public FrostbiteDrone(IntPtr pointer) : base(pointer) { }

        public void Awake()
        {
            PeaBlover? shooter = gameObject.GetComponent<PeaBlover>();
            if (!ReferenceEquals(shooter, null))
            {
                trackedPointer = shooter.Pointer;
                FrostbiteDroneBootstrap.Track(shooter);
            }
        }

        public void Start()
        {
            PeaBlover? shooter = gameObject.GetComponent<PeaBlover>();
            if (shooter == null)
            {
                Plugin.Logger.LogError("[Frostbite Drone] Start failed: no PeaBlover component.");
                return;
            }
            trackedPointer = shooter.Pointer;
            FrostbiteDroneBootstrap.Track(shooter);
            V11PlantsBootstrap.EnsureShooterRuntimeReferences(shooter, "Frostbite Drone");
            V11PlantsBootstrap.ApplyNativeShooterControllerWithLocalClips(
                gameObject,
                "Frostbite Drone",
                PlantType.PeaBlover
            );
        }

        public void OnDestroy()
        {
            FrostbiteDroneBootstrap.Untrack(trackedPointer);
            trackedPointer = IntPtr.Zero;
        }
    }
}
