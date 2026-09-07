using CustomizeLib.MelonLoader;
using HarmonyLib;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class AtomrayShroomBootstrap
    {
        private static bool registered;
        private static bool patchesInstalled;
        private static bool nativeDataLogged;
        private static float nativeBaseInterval = Plants.AtomrayShroom.FallbackInterval;
        private static int demiseDamage = Plants.AtomrayShroom.FallbackDemiseDamage;
        internal static Sprite? BlueRaySprite { get; private set; }

        internal static void OnStart()
        {
            if (registered)
                return;
            registered = true;
            InstallPatches();

            try
            {
                AssetBundle? bundle = CustomCore.GetAssetBundle(
                    Assembly.GetExecutingAssembly(),
                    "PlantsPlus.Resources.AssetBundles.atomray_shroom"
                );
                GameObject? prefab =
                    bundle?.GetAsset<GameObject>("LanternFumePrefab");
                GameObject? preview =
                    bundle?.GetAsset<GameObject>("LanternFumePreview");
                GameObject? visualAsset =
                    bundle?.GetAsset<GameObject>("LanternFume");
                if (bundle == null || prefab == null || preview == null)
                    throw new InvalidOperationException(
                        "Atomray-shroom bundle, prefab or preview is missing."
                    );

                Transform? c1 = visualAsset != null
                    ? FindDescendant(visualAsset.transform, "c1")
                    : null;
                BlueRaySprite = c1?.GetComponent<SpriteRenderer>()?.sprite;
                if (BlueRaySprite == null)
                {
                    Plugin.Logger.LogWarning(
                        "[Atomray-shroom] Bundle asset LanternFume/c1 " +
                        "was not found; the blue ray will be unavailable."
                    );
                }

                CustomCore.RegisterCustomPlant<LanternFume, Plants.AtomrayShroom>(
                    Plants.AtomrayShroom.AtomrayShroomID,
                    prefab,
                    preview,
                    new List<(int, int)>
                    {
                        ((int)PlantType.UltimatePlantern, (int)PlantType.FumeShroom),
                        ((int)PlantType.FumeShroom, (int)PlantType.UltimatePlantern)
                    },
                    Plants.AtomrayShroom.FallbackInterval,
                    0f,
                    Plants.AtomrayShroom.FallbackDamage,
                    Plants.AtomrayShroom.FallbackToughness,
                    Plants.AtomrayShroom.FallbackCardRecharge,
                    Plants.AtomrayShroom.FallbackCardCost
                );

                AlmanacEntry almanac = AlmanacContent.AtomrayShroom;
                CustomCore.AddPlantAlmanacStrings(
                    (PlantType)Plants.AtomrayShroom.AtomrayShroomID,
                    almanac.Name,
                    almanac.Info,
                    almanac.Introduce,
                    Plants.AtomrayShroom.FallbackCardCost
                );
                OdysseyRegistration.RegisterWeak(
                    (PlantType)Plants.AtomrayShroom.AtomrayShroomID,
                    "Atomray-shroom"
                );
                Plugin.Logger.LogInfo(
                    "[Atomray-shroom] Registered | Alt = true" +
                    " | Fusion = Atomheart Plantern + Fume-shroom" +
                    " | Return = Atomray-shroom + Plantern"
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Atomray-shroom] Registration failed safely: " + exception
                );
            }
        }

        private static Transform? FindDescendant(Transform root, string name)
        {
            if (root.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return root;
            for (int index = 0; index < root.childCount; index++)
            {
                Transform? found = FindDescendant(root.GetChild(index), name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static void InstallPatches()
        {
            if (patchesInstalled)
                return;
            patchesInstalled = true;

            HarmonyLib.Harmony harmony =
                new HarmonyLib.Harmony("PlantsPlus.AtomrayShroom");
            harmony.Patch(
                AccessTools.Method(typeof(LanternFume), nameof(LanternFume.Update)),
                postfix: new HarmonyMethod(AccessTools.Method(
                    typeof(AtomrayShroomBootstrap), nameof(LanternFumeUpdatePostfix)))
            );
            harmony.Patch(
                AccessTools.Method(typeof(LanternFume), nameof(LanternFume.Shoot1)),
                prefix: new HarmonyMethod(AccessTools.Method(
                    typeof(AtomrayShroomBootstrap), nameof(LanternFumeShoot1Prefix)))
            );
            harmony.Patch(
                AccessTools.Method(typeof(Plant), nameof(Plant.Die),
                    new Type[] { typeof(Plant.DieReason) }),
                prefix: new HarmonyMethod(AccessTools.Method(
                    typeof(AtomrayShroomBootstrap), nameof(PlantDiePrefix)))
            );
            harmony.Patch(
                AccessTools.Method(typeof(MixData), nameof(MixData.TryGetMix)),
                prefix: new HarmonyMethod(AccessTools.Method(
                    typeof(AtomrayShroomBootstrap), nameof(MixDataTryGetMixPrefix)))
            );
            harmony.Patch(
                AccessTools.Method(typeof(GameAPP), nameof(GameAPP.LoadResources)),
                postfix: new HarmonyMethod(AccessTools.Method(
                    typeof(AtomrayShroomBootstrap), nameof(GameAppLoadResourcesPostfix)))
            );
        }

        internal static void OnGameInit()
        {
            OdysseyRegistration.RegisterWeak(
                (PlantType)Plants.AtomrayShroom.AtomrayShroomID,
                "Atomray-shroom"
            );
            RefreshNativeData();
            ConfigurePrefab();
            AlmanacCompatibility.RefreshLoadedData();
        }

        private static void RefreshNativeData()
        {
            try
            {
                PlantType type = (PlantType)Plants.AtomrayShroom.AtomrayShroomID;
                if (!CustomCore.CustomPlants.TryGetValue(type, out var customData))
                    return;

                PlantDataManager.PlantData ray =
                    PlantDataManager.GetPlantData(PlantType.LanternFume);
                PlantDataManager.PlantData demise =
                    PlantDataManager.GetPlantData(PlantType.IceDoom);
                if (ray == null || customData.PlantData == null)
                    return;

                nativeBaseInterval = ray.attackInterval > 0f
                    ? ray.attackInterval
                    : Plants.AtomrayShroom.FallbackInterval;
                if (demise != null && demise.attackDamage > 0)
                    demiseDamage = demise.attackDamage;

                PlantDataManager.PlantData target = customData.PlantData;
                target.thePlantType = type;
                target.attackInterval = nativeBaseInterval;
                target.produceInterval = ray.produceInterval;
                target.attackDamage = ray.attackDamage;
                target.maxHealth = ray.maxHealth;
                target.cd = ray.cd;
                target.cost = ray.cost;
                customData.PlantData = target;
                CustomCore.CustomPlants[type] = customData;

                if (!nativeDataLogged)
                {
                    nativeDataLogged = true;
                    Plugin.Logger.LogInfo(
                        "[Atomray-shroom] Native Ray-shroom data mirrored" +
                        " | Damage = " + ray.attackDamage +
                        " | Base interval = " + nativeBaseInterval +
                        "s | Demise damage = " + demiseDamage
                    );
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Atomray-shroom] Native data was not ready: " +
                    exception.Message
                );
            }
        }

        private static void ConfigurePrefab()
        {
            PlantType type = (PlantType)Plants.AtomrayShroom.AtomrayShroomID;
            if (GameAPP.resourcesManager == null ||
                GameAPP.resourcesManager.plantPrefabs == null ||
                !GameAPP.resourcesManager.plantPrefabs.ContainsKey(type))
                return;
            LanternFume? plant = GameAPP.resourcesManager.plantPrefabs[type]?
                .GetComponent<LanternFume>();
            if (plant != null)
                V11PlantsBootstrap.EnsureShooterRuntimeReferences(
                    plant,
                    "Atomray-shroom"
                );
        }

        private static bool IsAtomray(Plant? plant)
        {
            return plant != null &&
                (int)plant.thePlantType == Plants.AtomrayShroom.AtomrayShroomID;
        }

        private static int CountNearbyAtomhearts(LanternFume source)
        {
            try
            {
                var plants = Lawnf.Get3x3Plants(
                    source.thePlantRow,
                    source.thePlantColumn
                );
                int count = 0;
                for (int index = 0; index < plants.Count; index++)
                {
                    Plant plant = plants[index];
                    if (plant != null && !plant.dying &&
                        plant.thePlantType == PlantType.UltimatePlantern)
                        count++;
                }
                return count;
            }
            catch
            {
                return 0;
            }
        }

        private static void ApplyCadence(LanternFume source)
        {
            if (!IsAtomray(source))
                return;
            int supports = CountNearbyAtomhearts(source);
            float interval = nativeBaseInterval /
                (1f + supports * Plants.AtomrayShroom.SpeedPerAtomheart);
            source.thePlantAttackInterval = Mathf.Max(
                Plants.AtomrayShroom.MinimumInterval,
                interval
            );
        }

        private static void SlowTarget(Zombie? zombie)
        {
            if (zombie == null)
                return;
            try
            {
                zombie.SetCold(Plants.AtomrayShroom.SlowDuration);
            }
            catch
            {
                try
                {
                    EffectManager.SetEffect(
                        zombie,
                        EffectType.Cold,
                        Plants.AtomrayShroom.SlowDuration,
                        0.5f
                    );
                }
                catch { }
            }
        }

        internal static void CreateDemiseExplosion(
            Plant source,
            Vector3 world,
            int row
        )
        {
            try
            {
                if (GameAPP.resourcesManager == null ||
                    GameAPP.resourcesManager.plantPrefabs == null ||
                    !GameAPP.resourcesManager.plantPrefabs.ContainsKey(
                        PlantType.IceDoom
                    ))
                {
                    throw new InvalidOperationException(
                        "The native Demise-shroom prefab is unavailable."
                    );
                }

                GameObject? template =
                    GameAPP.resourcesManager.plantPrefabs[PlantType.IceDoom];
                if (template == null)
                    throw new InvalidOperationException(
                        "The native Demise-shroom prefab is null."
                    );

                GameObject demiseObject =
                    UnityEngine.Object.Instantiate(template);
                demiseObject.name = "[Plants+] Atomray Demise-shroom effect";
                demiseObject.transform.position = new Vector3(
                    world.x,
                    Lawnf.GetBoxYFromRow(row),
                    world.z
                );

                IceDoom? demise = demiseObject.GetComponent<IceDoom>();
                if (demise == null)
                {
                    UnityEngine.Object.Destroy(demiseObject);
                    throw new InvalidOperationException(
                        "The native Demise-shroom component is missing."
                    );
                }

                demise.board = Board.Instance;
                demise.thePlantType = PlantType.IceDoom;
                demise.thePlantColumn = Lawnf.GetColumnFromX(world.x);
                demise.thePlantRow = row;
                demise.attackDamage = demiseDamage;
                demise.AnimExplode();
                UnityEngine.Object.Destroy(demiseObject);
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Atomray-shroom] Demise explosion failed safely: " +
                    exception.Message
                );
            }
        }

        private static void LanternFumeUpdatePostfix(LanternFume __instance) =>
            ApplyCadence(__instance);

        private static Zombie? FindRayTarget(LanternFume source)
        {
            try
            {
                var zombies = Lawnf.GetZombiesByRow(source.thePlantRow, false);
                Zombie? nearest = null;
                float nearestX = float.MaxValue;
                float sourceX = source.transform.position.x;

                for (int index = 0; index < zombies.Count; index++)
                {
                    Zombie zombie = zombies[index];
                    if (zombie == null || !zombie.Alive ||
                        zombie.isMindControlled)
                        continue;

                    float x = zombie.transform.position.x;
                    if (x < sourceX - 0.1f || x >= nearestX)
                        continue;
                    nearest = zombie;
                    nearestX = x;
                }

                return nearest;
            }
            catch
            {
                return source.targetZombie;
            }
        }

        private static bool LanternFumeShoot1Prefix(
            LanternFume __instance,
            ref Bullet __result
        )
        {
            if (!IsAtomray(__instance))
                return true;

            Zombie? target = FindRayTarget(__instance);
            __result = null!;
            if (target == null)
                return false;

            __instance.targetZombie = target;
            int damage = __instance.attackDamage > 0
                ? __instance.attackDamage
                : Plants.AtomrayShroom.FallbackDamage;

            try
            {
                ((Entity)target).TakeDamage(
                    damage,
                    __instance.ToIDamageMaker(),
                    DamageType.Normal,
                    (PlantType)Plants.AtomrayShroom.AtomrayShroomID,
                    false
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Atomray-shroom] Ray damage failed safely: " +
                    exception.Message
                );
            }

            SlowTarget(target);
            __instance.GetComponent<Plants.AtomrayShroom>()?
                .ShowBlueRay(__instance, target);

            if (UnityEngine.Random.value < Plants.AtomrayShroom.DemiseChance)
            {
                CreateDemiseExplosion(
                    __instance,
                    target.transform.position,
                    target.theZombieRow
                );
            }

            return false;
        }

        private static void PlantDiePrefix(Plant __instance)
        {
            if (!IsAtomray(__instance))
                return;
            Plants.AtomrayShroom? behaviour =
                __instance.GetComponent<Plants.AtomrayShroom>();
            if (behaviour != null && behaviour.TryExplodeOnDeath())
            {
                CreateDemiseExplosion(
                    __instance,
                    __instance.transform.position,
                    __instance.thePlantRow
                );
            }
        }

        private static bool MixDataTryGetMixPrefix(
            PlantType a,
            PlantType b,
            ref PlantType c,
            ref bool __result
        )
        {
            PlantType atomray = (PlantType)Plants.AtomrayShroom.AtomrayShroomID;
            bool reverse =
                (a == atomray && b == PlantType.Plantern) ||
                (a == PlantType.Plantern && b == atomray);
            if (!reverse)
                return true;
            c = PlantType.UltimatePlantern;
            __result = true;
            return false;
        }

        private static void GameAppLoadResourcesPostfix() => RefreshNativeData();
    }
}

namespace PlantsPlus.Plants
{
    using PlantsPlus.Core;

    public sealed class AtomrayShroom : MonoBehaviour
    {
        public const int AtomrayShroomID = 6028;
        public const int FallbackDamage = 40;
        public const int FallbackToughness = 300;
        public const int FallbackCardCost = 300;
        public const int FallbackDemiseDamage = 1800;
        public const float FallbackCardRecharge = 15f;
        public const float FallbackInterval = 1.5f;
        public const float DemiseChance = 0.25f;
        public const float SlowDuration = 3f;
        public const float SpeedPerAtomheart = 0.25f;
        public const float MinimumInterval = 0.5f;

        private bool deathExplosionUsed;
        private LanternFume? nativeSource;

        public AtomrayShroom(IntPtr pointer) : base(pointer) { }

        public void Start()
        {
            LanternFume? source = GetComponent<LanternFume>();
            if (source == null)
                return;
            V11PlantsBootstrap.EnsureShooterRuntimeReferences(
                source,
                "Atomray-shroom"
            );

            nativeSource = source;
            DisableNativePinkParticles();
            Plugin.Logger.LogInfo(
                "[Atomray-shroom] Ready | Beam c1 = " +
                (AtomrayShroomBootstrap.BlueRaySprite != null
                    ? "loaded from bundle"
                    : "missing") +
                " | Demise chance = 25%"
            );
        }

        public void LateUpdate()
        {
            DisableNativePinkParticles();
        }

        private void DisableNativePinkParticles()
        {
            try
            {
                if (nativeSource != null && nativeSource.lightShine != null &&
                    nativeSource.lightShine.gameObject.activeSelf)
                {
                    nativeSource.lightShine.gameObject.SetActive(false);
                }
            }
            catch { }
        }

        internal void ShowBlueRay(LanternFume source, Zombie target)
        {
            Sprite? raySprite = AtomrayShroomBootstrap.BlueRaySprite;
            if (raySprite == null || source == null || target == null)
                return;

            try
            {
                GameObject ray = new GameObject();
                ray.name = "[Plants+] Atomray blue ray";
                SpriteRenderer renderer = ray.AddComponent<SpriteRenderer>();
                renderer.sprite = raySprite;
                renderer.sortingOrder = 20;

                Vector3 start = source.shoot != null
                    ? source.shoot.position
                    : source.transform.position;
                float targetX = target.transform.position.x;
                try
                {
                    Collider2D? collider = target.GetComponent<Collider2D>();
                    if (collider != null)
                        targetX = collider.bounds.center.x;
                }
                catch { }

                float distance = Mathf.Max(0.1f, targetX - start.x);
                ray.transform.position = new Vector3(
                    start.x + distance * 0.5f,
                    start.y,
                    start.z
                );
                ray.transform.rotation = Quaternion.identity;
                float spriteWidth = Mathf.Max(0.01f, raySprite.bounds.size.x);
                ray.transform.localScale = new Vector3(
                    distance / spriteWidth,
                    0.3f,
                    1f
                );

                UnityEngine.Object.Destroy(ray, 0.2f);
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Atomray-shroom] Blue c1 ray failed safely: " +
                    exception.Message
                );
            }
        }

        internal bool TryExplodeOnDeath()
        {
            if (deathExplosionUsed)
                return false;
            deathExplosionUsed = true;
            return true;
        }

    }
}
