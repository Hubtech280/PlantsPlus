using CustomizeLib.MelonLoader;
using HarmonyLib;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class ThreeBuckpeaterBootstrap
    {
        private static bool registered;
        private static bool nativeDataLogged;
        private static readonly HashSet<IntPtr> LiveThreeBuckpeaterPointers =
            new HashSet<IntPtr>();

        internal static void TrackLiveShooter(ThreePeater? shooter)
        {
            if (ReferenceEquals(shooter, null))
                return;

            IntPtr pointer = shooter.Pointer;
            if (pointer != IntPtr.Zero)
                LiveThreeBuckpeaterPointers.Add(pointer);
        }

        internal static void UntrackLiveShooter(ThreePeater? shooter)
        {
            if (ReferenceEquals(shooter, null))
                return;

            UntrackLiveShooter(shooter.Pointer);
        }

        internal static void UntrackLiveShooter(IntPtr pointer)
        {
            if (pointer != IntPtr.Zero)
                LiveThreeBuckpeaterPointers.Remove(pointer);
        }

        private static bool IsLiveThreeBuckpeater(ThreePeater? shooter)
        {
            if (ReferenceEquals(shooter, null))
                return false;

            IntPtr pointer = shooter.Pointer;
            return pointer != IntPtr.Zero &&
                LiveThreeBuckpeaterPointers.Contains(pointer);
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
                    "PlantsPlus.Resources.AssetBundles.three_buckpeater"
                );
                GameObject? prefab =
                    bundle?.GetAsset<GameObject>("ThreePeaterPrefab");
                GameObject? preview =
                    bundle?.GetAsset<GameObject>("ThreePeaterPreview");

                if (bundle == null || prefab == null || preview == null)
                {
                    throw new InvalidOperationException(
                        "Three-Buckpeater bundle, prefab or preview is missing."
                    );
                }

                CustomCore.RegisterCustomPlant<
                    ThreePeater,
                    Plants.ThreeBuckpeater
                >(
                    Plants.ThreeBuckpeater.ThreeBuckpeaterID,
                    prefab,
                    preview,
                    new List<(int, int)>(),
                    Plants.ThreeBuckpeater.AttackInterval,
                    0f,
                    Plants.ThreeBuckpeater.Damage,
                    Plants.ThreeBuckpeater.Toughness,
                    Plants.ThreeBuckpeater.CardRecharge,
                    Plants.ThreeBuckpeater.CardCost
                );

                CustomCore.RegisterCustomUseItemOnPlantEvent(
                    PlantType.ThreePeater,
                    BucketType.Bucket,
                    (PlantType)Plants.ThreeBuckpeater.ThreeBuckpeaterID
                );

                AlmanacEntry almanac = AlmanacContent.ThreeBuckpeater;
                CustomCore.AddPlantAlmanacStrings(
                    (PlantType)Plants.ThreeBuckpeater.ThreeBuckpeaterID,
                    almanac.Name,
                    almanac.Info,
                    almanac.Introduce,
                    Plants.ThreeBuckpeater.CardCost
                );

                Plugin.Logger.LogInfo(
                    "[Three-Buckpeater] Registered" +
                    " | Plant ID = " +
                    Plants.ThreeBuckpeater.ThreeBuckpeaterID +
                    " | Fusion = Threepeater + Bucket" +
                    " | Projectile = native iron pea"
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Three-Buckpeater] Registration failed safely: " +
                    exception
                );
            }
        }

        internal static void OnGameInit()
        {
            RefreshNativePlantData();
            ConfigureRegisteredPrefab();
            AlmanacCompatibility.RefreshLoadedData();
        }

        private static void ConfigureRegisteredPrefab()
        {
            PlantType type =
                (PlantType)Plants.ThreeBuckpeater.ThreeBuckpeaterID;

            if (GameAPP.resourcesManager == null ||
                GameAPP.resourcesManager.plantPrefabs == null ||
                !GameAPP.resourcesManager.plantPrefabs.ContainsKey(type))
            {
                return;
            }

            GameObject? prefab = GameAPP.resourcesManager.plantPrefabs[type];
            ThreePeater? shooter = prefab?.GetComponent<ThreePeater>();
            if (prefab == null || shooter == null)
                return;

            string bridge = V11PlantsBootstrap.EnsureShooterRuntimeReferences(
                shooter,
                "Three-Buckpeater"
            );

            Plugin.Logger.LogInfo(
                "[Three-Buckpeater] Registered prefab runtime bridge = " +
                bridge
            );
        }

        internal static void RefreshNativePlantData()
        {
            try
            {
                PlantType customType =
                    (PlantType)Plants.ThreeBuckpeater.ThreeBuckpeaterID;

                if (!CustomCore.CustomPlants.TryGetValue(
                    customType,
                    out var customData
                ))
                    return;
                PlantDataManager.PlantData nativeData =
                    PlantDataManager.GetPlantData(PlantType.IronPea);

                if (nativeData == null || customData.PlantData == null)
                    return;

                PlantDataManager.PlantData target = customData.PlantData;
                target.thePlantType = customType;
                target.attackInterval = nativeData.attackInterval;
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
                        "[Three-Buckpeater] Buckshooter PlantData mirrored" +
                        " | Damage = " + nativeData.attackDamage +
                        " | Interval = " + nativeData.attackInterval +
                        "s | HP = " + nativeData.maxHealth
                    );
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Three-Buckpeater] Native PlantData was not ready: " +
                    exception.Message
                );
            }
        }

        [HarmonyPatch(typeof(GameAPP), nameof(GameAPP.LoadResources))]
        private static class GameAPP_LoadResources_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix()
            {
                RefreshNativePlantData();
            }
        }

        [HarmonyPatch(typeof(ThreePeater), "GetBulletType")]
        private static class ThreePeater_GetBulletType_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(
                ThreePeater __instance,
                ref BulletType __result
            )
            {
                // GetBulletType is also called by systems that can hand Harmony
                // a stale IL2CPP wrapper. Never dereference native Plant fields
                // here: only compare the managed wrapper's stored pointer with
                // instances registered by the live custom behaviour.
                if (IsLiveThreeBuckpeater(__instance))
                {
                    __result = BulletType.Bullet_ironPea;
                }
            }
        }
    }
}

namespace PlantsPlus.Plants
{
    using PlantsPlus.Core;

    public sealed class ThreeBuckpeater : MonoBehaviour
    {
        public const int ThreeBuckpeaterID = 6022;
        public const int Damage = 80;
        public const int Toughness = 2000;
        public const int CardCost = 325;
        public const float CardRecharge = 15f;
        public const float AttackInterval = 1.5f;

        private IntPtr trackedShooterPointer;

        public ThreeBuckpeater(IntPtr pointer) : base(pointer) { }

        public void Awake()
        {
            ThreePeater? shooter = gameObject.GetComponent<ThreePeater>();
            if (!ReferenceEquals(shooter, null))
            {
                trackedShooterPointer = shooter.Pointer;
                ThreeBuckpeaterBootstrap.TrackLiveShooter(shooter);
            }
        }

        public void Start()
        {
            ThreePeater? shooter = gameObject.GetComponent<ThreePeater>();
            if (shooter == null)
            {
                Plugin.Logger.LogError(
                    "[Three-Buckpeater] Start failed: no ThreePeater component."
                );
                return;
            }

            ThreeBuckpeaterBootstrap.TrackLiveShooter(shooter);
            trackedShooterPointer = shooter.Pointer;

            string bridge = V11PlantsBootstrap.EnsureShooterRuntimeReferences(
                shooter,
                "Three-Buckpeater"
            );

            Plugin.Logger.LogInfo(
                "[Three-Buckpeater] Ready" +
                " | Projectile = native Bullet_ironPea" +
                " | Rows = 3" +
                " | Runtime bridge = " + bridge
            );
        }

        public void OnDestroy()
        {
            ThreeBuckpeaterBootstrap.UntrackLiveShooter(trackedShooterPointer);
            trackedShooterPointer = IntPtr.Zero;
        }
    }
}
