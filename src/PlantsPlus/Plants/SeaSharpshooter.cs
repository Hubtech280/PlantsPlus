using CustomizeLib.MelonLoader;
using HarmonyLib;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class SeaSharpshooterBootstrap
    {
        private static bool registered;
        private static Sprite? projectileSprite;
        private static Vector3 projectileLocalPosition;
        private static Vector3 projectileLocalScale = Vector3.one;
        private static Quaternion projectileLocalRotation = Quaternion.identity;

        public static void OnStart()
        {
            if (registered)
                return;

            registered = true;

            try
            {
                LoadProjectileSkin();
                RegisterPlant();
                InstallTypeFlags();
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Sea Sharpshooter] Registration failed safely: " +
                    exception
                );
            }
        }

        public static void OnGameInit()
        {
            InstallTypeFlags();

            PlantType type =
                (PlantType)Plants.SeaSharpshooter.SeaSharpshooterID;
            if (!CustomCore.CustomPlants.ContainsKey(type))
                return;

            AlmanacCompatibility.RefreshLoadedData();
            ConfigureRegisteredPrefab();
        }

        private static void LoadProjectileSkin()
        {
            AssetBundle? bundle = CustomCore.GetAssetBundle(
                Assembly.GetExecutingAssembly(),
                "PlantsPlus.Resources.AssetBundles.bullet_seasharpshooter"
            );
            GameObject? prefab =
                bundle?.GetAsset<GameObject>("Bullet_spruce");

            if (bundle == null || prefab == null)
            {
                throw new InvalidOperationException(
                    "Projectile bundle or Bullet_spruce is missing."
                );
            }

            SpriteRenderer[] renderers =
                prefab.GetComponentsInChildren<SpriteRenderer>(true);

            for (int index = 0; index < renderers.Length; index++)
            {
                SpriteRenderer renderer = renderers[index];
                if (renderer == null || renderer.sprite == null ||
                    renderer.gameObject.name.Equals(
                        "Shadow",
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    continue;
                }

                projectileSprite = renderer.sprite;
                projectileLocalPosition = renderer.transform.localPosition;
                projectileLocalScale = renderer.transform.localScale;
                projectileLocalRotation = renderer.transform.localRotation;
                break;
            }

            if (projectileSprite == null)
            {
                throw new InvalidOperationException(
                    "Sea Sharpshooter projectile sprite is missing."
                );
            }
        }

        internal static void ApplyProjectileSkin(Bullet bullet)
        {
            if (bullet == null || projectileSprite == null)
                return;

            SpriteRenderer[] renderers =
                bullet.GetComponentsInChildren<SpriteRenderer>(true);

            for (int index = 0; index < renderers.Length; index++)
            {
                SpriteRenderer renderer = renderers[index];
                if (renderer == null ||
                    renderer.gameObject.name.Equals(
                        "Shadow",
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    continue;
                }

                renderer.sprite = projectileSprite;
                // Bullet_spruce is spawned at lawn/root height. Keep the
                // bundle's horizontal/depth offsets, but lift its visible
                // bolt to the shooter's muzzle like Solar Sharpshooter.
                renderer.transform.localPosition = new Vector3(
                    projectileLocalPosition.x,
                    0.70f,
                    projectileLocalPosition.z
                );
                renderer.transform.localScale = projectileLocalScale;
                renderer.transform.localRotation = projectileLocalRotation;
                return;
            }
        }

        private static void RegisterPlant()
        {
            AssetBundle? bundle = CustomCore.GetAssetBundle(
                Assembly.GetExecutingAssembly(),
                "PlantsPlus.Resources.AssetBundles.seasharpshooter"
            );
            GameObject? prefab =
                bundle?.GetAsset<GameObject>("SpruceShooterPrefab");
            GameObject? preview =
                bundle?.GetAsset<GameObject>("SpruceShooterPreview");

            if (bundle == null || prefab == null || preview == null)
            {
                throw new InvalidOperationException(
                    "Plant bundle, SpruceShooterPrefab or preview is missing."
                );
            }

            prefab.transform.localPosition = Vector3.zero;
            prefab.transform.localRotation = Quaternion.identity;
            V11PlantsBootstrap.IsolateAnimationClips(
                bundle, prefab, "Sea Sharpshooter",
                "sea_sharp_idle", "sea_sharp_shoot"
            );

            CustomCore.RegisterCustomPlant<
                SpruceShooter,
                Plants.SeaSharpshooter
            >(
                Plants.SeaSharpshooter.SeaSharpshooterID,
                prefab,
                preview,
                new List<(int, int)>
                {
                    (
                        (int)PlantType.SpruceShooter,
                        (int)PlantType.SeaShroom
                    ),
                    (
                        (int)PlantType.SeaShroom,
                        (int)PlantType.SpruceShooter
                    )
                },
                Plants.SeaSharpshooter.AttackInterval,
                0f,
                Plants.SeaSharpshooter.StageOneDamage,
                Plants.SeaSharpshooter.Toughness,
                Plants.SeaSharpshooter.CardRecharge,
                Plants.SeaSharpshooter.CardCost
            );

            AlmanacEntry almanac = AlmanacContent.SeaSharpshooter;
            CustomCore.AddPlantAlmanacStrings(
                (PlantType)Plants.SeaSharpshooter.SeaSharpshooterID,
                almanac.Name,
                almanac.Info,
                almanac.Introduce,
                Plants.SeaSharpshooter.CardCost
            );

            Plugin.Logger.LogInfo(
                "[Sea Sharpshooter] Registered" +
                " | Plant ID = " +
                Plants.SeaSharpshooter.SeaSharpshooterID +
                " | Growth = 65% > 82% > 100%" +
                " | Damage = 20 > 40 > 60" +
                " | Stage duration = 30s"
            );
        }

        private static void InstallTypeFlags()
        {
            PlantType type =
                (PlantType)Plants.SeaSharpshooter.SeaSharpshooterID;

            if (!CustomCore.TypeMgrExtra.IsWaterPlant.Contains(type))
                CustomCore.TypeMgrExtra.IsWaterPlant.Add(type);
        }

        private static void ConfigureRegisteredPrefab()
        {
            PlantType type =
                (PlantType)Plants.SeaSharpshooter.SeaSharpshooterID;

            if (GameAPP.resourcesManager == null ||
                GameAPP.resourcesManager.plantPrefabs == null ||
                !GameAPP.resourcesManager.plantPrefabs.ContainsKey(type))
            {
                return;
            }

            GameObject? prefab = GameAPP.resourcesManager.plantPrefabs[type];
            SpruceShooter? shooter = prefab?.GetComponent<SpruceShooter>();
            if (prefab == null || shooter == null)
                return;

            Plants.SeaSharpshooter.ConfigurePlant(shooter);
            V11PlantsBootstrap.EnsureShooterRuntimeReferences(
                shooter,
                "Sea Sharpshooter"
            );
            V11PlantsBootstrap.ApplyNativeShooterControllerWithLocalClips(
                prefab,
                "Sea Sharpshooter",
                PlantType.SpruceShooter
            );
        }
    }
}

namespace PlantsPlus.Plants
{
    using PlantsPlus.Core;

    public sealed class SeaSharpshooter : MonoBehaviour
    {
        public const int SeaSharpshooterID = 6020;
        public const int StageOneDamage = 20;
        public const int StageTwoDamage = 40;
        public const int StageThreeDamage = 60;
        public const int Toughness = 300;
        public const int CardCost = 300;
        public const float CardRecharge = 15f;
        public const float AttackInterval = 1.5f;
        public const float GrowthStageDuration = 30f;
        public const float StageOneScale = 0.65f;
        public const float StageTwoScale = 0.82f;
        public const float StageThreeScale = 1.00f;
        private const float ScaleTransitionSpeed = 0.75f;

        private float growthElapsed;
        private int growthStage = 1;
        private Transform? visualRoot;
        private Vector3 normalVisualScale = Vector3.one;
        private float displayedScale = StageOneScale;
        private bool initialized;

        public SeaSharpshooter(IntPtr pointer) : base(pointer) { }

        public int CurrentDamage
        {
            get
            {
                if (growthStage >= 3)
                    return StageThreeDamage;
                if (growthStage == 2)
                    return StageTwoDamage;
                return StageOneDamage;
            }
        }

        public void Start()
        {
            SpruceShooter? shooter = gameObject.GetComponent<SpruceShooter>();
            if (shooter != null)
            {
                ConfigurePlant(shooter);
                V11PlantsBootstrap.EnsureShooterRuntimeReferences(
                    shooter,
                    "Sea Sharpshooter"
                );
                V11PlantsBootstrap.ApplyNativeShooterControllerWithLocalClips(
                    gameObject,
                    "Sea Sharpshooter",
                    PlantType.SpruceShooter
                );
            }

            visualRoot = FindChild(transform, "body");
            if (visualRoot == null)
            {
                Plugin.Logger.LogWarning(
                    "[Sea Sharpshooter] Visual child 'body' was not found; " +
                    "growth damage remains active without size changes."
                );
                initialized = true;
                return;
            }

            normalVisualScale = visualRoot.localScale;
            displayedScale = StageOneScale;
            visualRoot.localScale =
                normalVisualScale * StageOneScale;
            initialized = true;

            Plugin.Logger.LogInfo(
                "[Sea Sharpshooter] Stage 1 active" +
                " | Scale = 65%" +
                " | Damage = " + StageOneDamage
            );
        }

        public void Update()
        {
            if (!initialized)
                return;

            growthElapsed += Time.deltaTime;

            int nextStage = growthElapsed >= GrowthStageDuration * 2f
                ? 3
                : growthElapsed >= GrowthStageDuration
                    ? 2
                    : 1;

            if (nextStage != growthStage)
            {
                growthStage = nextStage;
                Plugin.Logger.LogInfo(
                    "[Sea Sharpshooter] Stage " + growthStage +
                    " reached" +
                    " | Damage = " + CurrentDamage
                );
            }

        }

        public void LateUpdate()
        {
            if (!initialized || visualRoot == null)
                return;

            // Animator evaluates after Update and writes the animated body's
            // transform back to its clip value. Apply growth in LateUpdate so
            // the three visual stages remain visible without custom clips.
            float targetScale = growthStage >= 3
                ? StageThreeScale
                : growthStage == 2
                    ? StageTwoScale
                    : StageOneScale;
            displayedScale = Mathf.MoveTowards(
                displayedScale,
                targetScale,
                Time.deltaTime * ScaleTransitionSpeed
            );
            visualRoot.localScale = normalVisualScale * displayedScale;
        }

        private static Transform? FindChild(Transform root, string name)
        {
            if (root.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return root;

            for (int index = 0; index < root.childCount; index++)
            {
                Transform child = root.GetChild(index);
                Transform? match = FindChild(child, name);
                if (match != null)
                    return match;
            }

            return null;
        }

        internal static bool IsSeaSharpshooter(Plant? plant)
        {
            return plant != null &&
                (int)plant.thePlantType == SeaSharpshooterID;
        }

        internal static void ConfigurePlant(SpruceShooter plant)
        {
            if (plant == null)
                return;

            Plant.PlantTag tags = plant.plantTag;
            tags.waterPlant = true;
            plant.plantTag = tags;
        }
    }

    internal static class SeaSharpshooterPatches
    {
        private static bool firstShotLogged;

        [HarmonyPatch(typeof(SpruceShooter), nameof(SpruceShooter.Shoot1))]
        private static class SpruceShooter_Shoot1_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(
                SpruceShooter __instance,
                Bullet __result
            )
            {
                if (!SeaSharpshooter.IsSeaSharpshooter(__instance) ||
                    __result == null)
                {
                    return;
                }

                SeaSharpshooter? behaviour =
                    __instance.GetComponent<SeaSharpshooter>();
                int damage = behaviour != null
                    ? behaviour.CurrentDamage
                    : SeaSharpshooter.StageOneDamage;

                __result.from = __instance;
                __result.fromType =
                    (PlantType)SeaSharpshooter.SeaSharpshooterID;
                __result.Damage = damage;
                SeaSharpshooterBootstrap.ApplyProjectileSkin(__result);

                if (!firstShotLogged)
                {
                    firstShotLogged = true;
                    Plugin.Logger.LogInfo(
                        "[Sea Sharpshooter] First shot verified" +
                        " | Damage = " + __result.Damage +
                        " | Projectile = native Bullet_spruce + custom sprite"
                    );
                }
            }
        }
    }
}
