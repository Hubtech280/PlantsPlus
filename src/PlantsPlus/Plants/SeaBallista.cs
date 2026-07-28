using CustomizeLib.MelonLoader;
using HarmonyLib;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class SeaBallistaBootstrap
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
                    "[Sea Ballista] Registration failed safely: " +
                    exception
                );
            }
        }

        public static void OnGameInit()
        {
            InstallTypeFlags();

            PlantType type = (PlantType)Plants.SeaBallista.SeaBallistaID;
            if (!CustomCore.CustomPlants.ContainsKey(type))
                return;

            AlmanacCompatibility.RefreshLoadedData();
            ConfigureRegisteredPrefab();
        }

        private static void LoadProjectileSkin()
        {
            AssetBundle? bundle = CustomCore.GetAssetBundle(
                Assembly.GetExecutingAssembly(),
                "PlantsPlus.Resources.AssetBundles.bullet_seaballista"
            );
            GameObject? prefab =
                bundle?.GetAsset<GameObject>("Bullet_spruceBallista");

            if (bundle == null || prefab == null)
            {
                throw new InvalidOperationException(
                    "Projectile bundle or Bullet_spruceBallista is missing."
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
                    "Sea Ballista projectile sprite is missing."
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
                renderer.transform.localPosition =
                    projectileLocalPosition +
                    Vector3.up * Plants.SeaBallista.ProjectileVisualYOffset;
                renderer.transform.localScale = projectileLocalScale;
                renderer.transform.localRotation = projectileLocalRotation;
                return;
            }
        }

        private static void RegisterPlant()
        {
            AssetBundle? bundle = CustomCore.GetAssetBundle(
                Assembly.GetExecutingAssembly(),
                "PlantsPlus.Resources.AssetBundles.seaballista"
            );
            GameObject? prefab =
                bundle?.GetAsset<GameObject>("SpruceBallistaPrefab");
            GameObject? preview =
                bundle?.GetAsset<GameObject>("SpruceBallistaPreview");

            if (bundle == null || prefab == null || preview == null)
            {
                throw new InvalidOperationException(
                    "Plant bundle, prefab or preview is missing."
                );
            }

            prefab.transform.localPosition = Vector3.zero;
            prefab.transform.localRotation = Quaternion.identity;

            V11PlantsBootstrap.IsolateAnimationClips(
                bundle,
                prefab,
                "Sea Ballista",
                "idle",
                "shoot"
            );

            CustomCore.RegisterCustomPlant<
                SpruceBallista,
                Plants.SeaBallista
            >(
                Plants.SeaBallista.SeaBallistaID,
                prefab,
                preview,
                new List<(int, int)>
                {
                    (
                        (int)PlantType.SpruceBallista,
                        (int)PlantType.SeaShroom
                    ),
                    (
                        (int)PlantType.SeaShroom,
                        (int)PlantType.SpruceBallista
                    )
                },
                Plants.SeaBallista.AttackInterval,
                0f,
                Plants.SeaBallista.Damage,
                Plants.SeaBallista.Toughness,
                Plants.SeaBallista.CardRecharge,
                Plants.SeaBallista.CardCost
            );

            AlmanacEntry almanac = AlmanacContent.SeaBallista;
            CustomCore.AddPlantAlmanacStrings(
                (PlantType)Plants.SeaBallista.SeaBallistaID,
                almanac.Name,
                almanac.Info,
                almanac.Introduce,
                Plants.SeaBallista.CardCost
            );

            Plugin.Logger.LogInfo(
                "[Sea Ballista] Registered" +
                " | Plant ID = " + Plants.SeaBallista.SeaBallistaID +
                " | Base = native SpruceBallista" +
                " | Water plant = true" +
                " | Double box = true" +
                " | Range = 3.5 tiles" +
                " | Knockback = " + Plants.SeaBallista.KnockbackDistance
            );
        }

        private static void InstallTypeFlags()
        {
            PlantType type = (PlantType)Plants.SeaBallista.SeaBallistaID;

            if (!CustomCore.TypeMgrExtra.IsWaterPlant.Contains(type))
                CustomCore.TypeMgrExtra.IsWaterPlant.Add(type);

            if (!CustomCore.TypeMgrExtra.DoubleBoxPlants.Contains(type))
                CustomCore.TypeMgrExtra.DoubleBoxPlants.Add(type);
        }

        private static void ConfigureRegisteredPrefab()
        {
            PlantType type = (PlantType)Plants.SeaBallista.SeaBallistaID;

            if (GameAPP.resourcesManager == null ||
                GameAPP.resourcesManager.plantPrefabs == null ||
                !GameAPP.resourcesManager.plantPrefabs.ContainsKey(type))
            {
                return;
            }

            GameObject? prefab = GameAPP.resourcesManager.plantPrefabs[type];
            SpruceBallista? ballista =
                prefab?.GetComponent<SpruceBallista>();

            if (prefab == null || ballista == null)
                return;

            Plants.SeaBallista.ConfigurePlant(ballista);
            V11PlantsBootstrap.EnsureShooterRuntimeReferences(
                ballista,
                "Sea Ballista"
            );
            V11PlantsBootstrap.ApplyNativeShooterControllerWithLocalClips(
                prefab,
                "Sea Ballista",
                PlantType.SpruceBallista
            );
        }
    }
}

namespace PlantsPlus.Plants
{
    using PlantsPlus.Core;

    public sealed class SeaBallista : MonoBehaviour
    {
        public const int SeaBallistaID = 6017;
        public const int Damage = 80;
        public const int Toughness = 300;
        public const int CardCost = 300;
        public const float CardRecharge = 50f;
        public const float AttackInterval = 3f;
        public const float RangeInTiles = 3.5f;
        public const float KnockbackDistance = 0.5f;
        public const float ProjectileVisualYOffset = 0.2f;

        public SeaBallista(IntPtr pointer) : base(pointer) { }

        public void Start()
        {
            SpruceBallista? plant =
                gameObject.GetComponent<SpruceBallista>();
            if (plant == null)
                return;

            ConfigurePlant(plant);
            V11PlantsBootstrap.EnsureShooterRuntimeReferences(
                plant,
                "Sea Ballista"
            );
            V11PlantsBootstrap.ApplyNativeShooterControllerWithLocalClips(
                gameObject,
                "Sea Ballista",
                PlantType.SpruceBallista
            );
        }

        internal static bool IsSeaBallista(Plant? plant)
        {
            return plant != null &&
                (int)plant.thePlantType == SeaBallistaID;
        }

        internal static bool IsSeaBallistaBullet(Bullet? bullet)
        {
            if (bullet == null)
                return false;

            return (int)bullet.fromType == SeaBallistaID ||
                IsSeaBallista(bullet.from);
        }

        internal static void ConfigurePlant(SpruceBallista plant)
        {
            if (plant == null)
                return;

            Plant.PlantTag tags = plant.plantTag;
            tags.waterPlant = true;
            tags.doubleBoxPlant = true;
            plant.plantTag = tags;
            plant.size = new Vector2Int(2, 1);
        }

        internal static float GetMaximumWorldRange()
        {
            float tileWidth = Mathf.Abs(
                Lawnf.GetBoxXFromColumn(1) -
                Lawnf.GetBoxXFromColumn(0)
            );

            return tileWidth > 0.01f
                ? tileWidth * RangeInTiles
                : 3.5f;
        }
    }

    internal static class SeaBallistaPatches
    {
        private static bool shotLogged;
        private static bool knockbackLogged;

        [HarmonyPatch(typeof(SpruceBallista), "Shoot1")]
        private static class SpruceBallista_Shoot1_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(
                SpruceBallista __instance,
                Bullet __result
            )
            {
                if (!SeaBallista.IsSeaBallista(__instance) ||
                    __result == null)
                {
                    return;
                }

                __result.from = __instance;
                __result.fromType = (PlantType)SeaBallista.SeaBallistaID;
                SeaBallistaBootstrap.ApplyProjectileSkin(__result);

                if (!shotLogged)
                {
                    shotLogged = true;
                    Plugin.Logger.LogInfo(
                        "[Sea Ballista] Native bolt verified" +
                        " | Bullet = " + __result.theBulletType +
                        " | Penetration = " + __result.penetrationTimes
                    );
                }
            }
        }

        [HarmonyPatch(typeof(Plant), "SearchZombie")]
        private static class Plant_SearchZombie_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(
                Plant __instance,
                ref GameObject __result
            )
            {
                if (!SeaBallista.IsSeaBallista(__instance) ||
                    __result == null)
                {
                    return;
                }

                float distance =
                    __result.transform.position.x -
                    __instance.transform.position.x;

                if (distance > SeaBallista.GetMaximumWorldRange())
                    __result = null!;
            }
        }

        [HarmonyPatch(typeof(Bullet_spruceBallista), "HitZombie")]
        private static class BulletSpruceBallista_HitZombie_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(
                Bullet_spruceBallista __instance,
                Zombie zombie
            )
            {
                if (!SeaBallista.IsSeaBallistaBullet(__instance) ||
                    zombie == null)
                {
                    return;
                }

                BallistaBomb? bomb =
                    zombie.GetComponent<BallistaBomb>();
                if (bomb != null)
                    bomb.fromType = (PlantType)SeaBallista.SeaBallistaID;
            }
        }

        [HarmonyPatch(typeof(BallistaBomb), "ExplodeAction")]
        private static class BallistaBomb_ExplodeAction_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(
                BallistaBomb __instance,
                Zombie zombie
            )
            {
                if (__instance == null || zombie == null ||
                    (int)__instance.fromType != SeaBallista.SeaBallistaID)
                {
                    return;
                }

                zombie.KnockBack(
                    SeaBallista.KnockbackDistance,
                    Zombie.KnockBackReason.Normal
                );

                if (!knockbackLogged)
                {
                    knockbackLogged = true;
                    Plugin.Logger.LogInfo(
                        "[Sea Ballista] Delayed explosion knockback verified" +
                        " | Distance = " +
                        SeaBallista.KnockbackDistance
                    );
                }
            }
        }
    }
}
