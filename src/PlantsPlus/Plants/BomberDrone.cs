using CustomizeLib.MelonLoader;
using HarmonyLib;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class BomberDroneBootstrap
    {
        private static bool registered;
        private static bool nativeDataLogged;
        private static readonly HashSet<IntPtr> LiveBomberDronePointers =
            new HashSet<IntPtr>();

        [ThreadStatic]
        private static IntPtr activeBomberDroneShot;

        internal static void TrackLiveShooter(PeaBlover? shooter)
        {
            if (ReferenceEquals(shooter, null))
                return;

            IntPtr pointer = shooter.Pointer;
            if (pointer != IntPtr.Zero)
                LiveBomberDronePointers.Add(pointer);
        }

        internal static void UntrackLiveShooter(IntPtr pointer)
        {
            if (pointer != IntPtr.Zero)
                LiveBomberDronePointers.Remove(pointer);
        }

        private static bool IsLiveBomberDrone(PeaBlover? shooter)
        {
            if (ReferenceEquals(shooter, null))
                return false;

            IntPtr pointer = shooter.Pointer;
            return pointer != IntPtr.Zero &&
                LiveBomberDronePointers.Contains(pointer);
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
                    "PlantsPlus.Resources.AssetBundles.bomberdrone"
                );
                GameObject? prefab =
                    bundle?.GetAsset<GameObject>("PeaBloverPrefab");
                GameObject? preview =
                    bundle?.GetAsset<GameObject>("PeaBloverPreview");

                if (bundle == null || prefab == null || preview == null)
                {
                    throw new InvalidOperationException(
                        "Bomber Drone bundle, prefab or preview is missing."
                    );
                }

                CustomCore.RegisterCustomPlant<PeaBlover, Plants.BomberDrone>(
                    Plants.BomberDrone.BomberDroneID,
                    prefab,
                    preview,
                    new List<(int, int)>
                    {
                        (
                            (int)PlantType.Cherryshooter,
                            (int)PlantType.Blover
                        ),
                        (
                            (int)PlantType.Blover,
                            (int)PlantType.Cherryshooter
                        )
                    },
                    Plants.BomberDrone.FallbackAttackInterval,
                    0f,
                    Plants.BomberDrone.FallbackDamage,
                    Plants.BomberDrone.FallbackToughness,
                    Plants.BomberDrone.FallbackCardRecharge,
                    Plants.BomberDrone.FallbackCardCost
                );

                AlmanacEntry almanac = AlmanacContent.BomberDrone;
                CustomCore.AddPlantAlmanacStrings(
                    (PlantType)Plants.BomberDrone.BomberDroneID,
                    almanac.Name,
                    almanac.Info,
                    almanac.Introduce,
                    Plants.BomberDrone.FallbackCardCost
                );

                EnsureFlyingClassification();

                Plugin.Logger.LogInfo(
                    "[Bomber Drone] Registered" +
                    " | Plant ID = " + Plants.BomberDrone.BomberDroneID +
                    " | Fusion = Cherryshooter + Blover" +
                    " | Native behaviour = PeaBlover" +
                    " | Projectile = Bullet_cherry"
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Bomber Drone] Registration failed safely: " +
                    exception
                );
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
                PlantType customType =
                    (PlantType)Plants.BomberDrone.BomberDroneID;
                var flyingPlants = TypeData.FlyingPlants;

                if (flyingPlants != null &&
                    !flyingPlants.Contains(customType))
                {
                    flyingPlants.Add(customType);
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Bomber Drone] Flying classification was not ready: " +
                    exception.Message
                );
            }
        }

        private static void ConfigureRegisteredPrefab()
        {
            PlantType type = (PlantType)Plants.BomberDrone.BomberDroneID;

            if (GameAPP.resourcesManager == null ||
                GameAPP.resourcesManager.plantPrefabs == null ||
                !GameAPP.resourcesManager.plantPrefabs.ContainsKey(type))
            {
                return;
            }

            GameObject? prefab = GameAPP.resourcesManager.plantPrefabs[type];
            PeaBlover? shooter = prefab?.GetComponent<PeaBlover>();
            if (prefab == null || shooter == null)
                return;

            string bridge = V11PlantsBootstrap.EnsureShooterRuntimeReferences(
                shooter,
                "Bomber Drone"
            );

            Plugin.Logger.LogInfo(
                "[Bomber Drone] Registered prefab runtime bridge = " + bridge
            );
        }

        internal static void RefreshNativePlantData()
        {
            try
            {
                PlantType customType =
                    (PlantType)Plants.BomberDrone.BomberDroneID;

                if (!CustomCore.CustomPlants.TryGetValue(
                    customType,
                    out var customData
                ))
                    return;

                PlantDataManager.PlantData nativeData =
                    PlantDataManager.GetPlantData(PlantType.PeaBlover);
                PlantDataManager.PlantData cherryData =
                    PlantDataManager.GetPlantData(PlantType.Cherryshooter);

                if (nativeData == null ||
                    cherryData == null ||
                    customData.PlantData == null)
                    return;

                PlantDataManager.PlantData target = customData.PlantData;
                target.thePlantType = customType;
                target.attackInterval = nativeData.attackInterval;
                target.produceInterval = nativeData.produceInterval;
                target.attackDamage = cherryData.attackDamage;
                target.maxHealth = nativeData.maxHealth;
                target.cd = nativeData.cd;
                target.cost = nativeData.cost;
                customData.PlantData = target;
                CustomCore.CustomPlants[customType] = customData;

                if (!nativeDataLogged)
                {
                    nativeDataLogged = true;
                    Plugin.Logger.LogInfo(
                        "[Bomber Drone] Native PlantData combined" +
                        " | Damage = Cherryshooter " +
                        cherryData.attackDamage +
                        " | Interval = " + nativeData.attackInterval +
                        "s | HP = " + nativeData.maxHealth +
                        " | Cost = " + nativeData.cost
                    );
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Bomber Drone] Native PlantData was not ready: " +
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
                EnsureFlyingClassification();
                RefreshNativePlantData();
            }
        }

        [HarmonyPatch(typeof(PeaBlover), "Shoot1")]
        private static class PeaBlover_Shoot1_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(PeaBlover __instance)
            {
                if (IsLiveBomberDrone(__instance))
                    activeBomberDroneShot = __instance.Pointer;
            }

            [HarmonyPostfix]
            private static void Postfix()
            {
                activeBomberDroneShot = IntPtr.Zero;
            }

            [HarmonyFinalizer]
            private static Exception? Finalizer(Exception? __exception)
            {
                activeBomberDroneShot = IntPtr.Zero;
                return __exception;
            }
        }

        [HarmonyPatch(
            typeof(CreateBullet),
            nameof(CreateBullet.SetBullet),
            new Type[]
            {
                typeof(float),
                typeof(float),
                typeof(int),
                typeof(BulletType),
                typeof(BulletMoveWay),
                typeof(bool)
            }
        )]
        private static class CreateBullet_SetBullet_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(ref BulletType theBulletType)
            {
                if (activeBomberDroneShot != IntPtr.Zero &&
                    LiveBomberDronePointers.Contains(activeBomberDroneShot))
                {
                    theBulletType = BulletType.Bullet_cherry;
                }
            }
        }

        [HarmonyPatch(typeof(TypeMgr), nameof(TypeMgr.FlyingPlants))]
        private static class TypeMgr_FlyingPlants_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(
                PlantType thePlantType,
                ref bool __result
            )
            {
                if ((int)thePlantType == Plants.BomberDrone.BomberDroneID)
                    __result = true;
            }
        }
    }
}

namespace PlantsPlus.Plants
{
    using PlantsPlus.Core;

    public sealed class BomberDrone : MonoBehaviour
    {
        public const int BomberDroneID = 6025;

        // These values exist only until the game's PeaBlover data is loaded;
        // RefreshNativePlantData then mirrors the exact native values.
        public const int FallbackDamage = 40;
        public const int FallbackToughness = 300;
        public const int FallbackCardCost = 225;
        public const float FallbackCardRecharge = 15f;
        public const float FallbackAttackInterval = 1.5f;

        private IntPtr trackedShooterPointer;

        public BomberDrone(IntPtr pointer) : base(pointer) { }

        public void Awake()
        {
            PeaBlover? shooter = gameObject.GetComponent<PeaBlover>();
            if (!ReferenceEquals(shooter, null))
            {
                trackedShooterPointer = shooter.Pointer;
                BomberDroneBootstrap.TrackLiveShooter(shooter);
            }
        }

        public void Start()
        {
            PeaBlover? shooter = gameObject.GetComponent<PeaBlover>();
            if (shooter == null)
            {
                Plugin.Logger.LogError(
                    "[Bomber Drone] Start failed: no PeaBlover component."
                );
                return;
            }

            BomberDroneBootstrap.TrackLiveShooter(shooter);
            trackedShooterPointer = shooter.Pointer;

            string bridge = V11PlantsBootstrap.EnsureShooterRuntimeReferences(
                shooter,
                "Bomber Drone"
            );

            Plugin.Logger.LogInfo(
                "[Bomber Drone] Ready" +
                " | Flight and targeting = native PeaBlover" +
                " | Projectile = native Bullet_cherry" +
                " | Runtime bridge = " + bridge
            );
        }

        public void OnDestroy()
        {
            BomberDroneBootstrap.UntrackLiveShooter(trackedShooterPointer);
            trackedShooterPointer = IntPtr.Zero;
        }
    }
}
