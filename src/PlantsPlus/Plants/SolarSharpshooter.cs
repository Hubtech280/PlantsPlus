using CustomizeLib.BepInEx;
using HarmonyLib;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class SolarSharpshooterBootstrap
    {
        private static bool registered;
        private static Sprite? projectileSprite;

        public static void OnStart()
        {
            if (registered)
                return;

            registered = true;

            try
            {
                LoadProjectileSkin();
                RegisterPlant();
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Solar Sharpshooter] Registration failed safely: " +
                    exception
                );
            }
        }

        public static void OnGameInit()
        {
            if (CustomCore.CustomPlants.ContainsKey(
                (PlantType)Plants.SolarSharpshooter.SolarSharpshooterID
            ))
            {
                AlmanacCompatibility.RefreshLoadedData();
                ConfigureRegisteredPrefab();
            }
        }

        private static void ConfigureRegisteredPrefab()
        {
            PlantType type =
                (PlantType)Plants.SolarSharpshooter.SolarSharpshooterID;

            if (GameAPP.resourcesManager == null ||
                GameAPP.resourcesManager.plantPrefabs == null ||
                !GameAPP.resourcesManager.plantPrefabs.ContainsKey(type))
            {
                return;
            }

            GameObject? prefab =
                GameAPP.resourcesManager.plantPrefabs[type];
            SpruceShooter? shooter = prefab?.GetComponent<SpruceShooter>();
            if (prefab == null || shooter == null)
                return;

            V11PlantsBootstrap.EnsureShooterRuntimeReferences(
                shooter,
                "Solar Sharpshooter"
            );
            V11PlantsBootstrap.ApplyNativeShooterControllerWithLocalClips(
                prefab,
                "Solar Sharpshooter",
                PlantType.SpruceShooter
            );
        }

        private static void LoadProjectileSkin()
        {
            AssetBundle? bundle = CustomCore.GetAssetBundle(
                Assembly.GetExecutingAssembly(),
                "PlantsPlus.Resources.AssetBundles.bullet_solarsharpshooter"
            );
            GameObject? prefab =
                bundle?.GetAsset<GameObject>("Bullet_spruce");

            if (bundle == null || prefab == null)
            {
                throw new InvalidOperationException(
                    "Projectile bundle or prefab is missing."
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
                break;
            }

            if (projectileSprite == null)
                throw new InvalidOperationException(
                    "Solar projectile sprite is missing."
                );
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
                renderer.transform.localPosition = new Vector3(
                    0f,
                    0.70f,
                    0f
                );
                return;
            }
        }

        private static void RegisterPlant()
        {
            AssetBundle? bundle = CustomCore.GetAssetBundle(
                Assembly.GetExecutingAssembly(),
                "PlantsPlus.Resources.AssetBundles.solarsharpshooter"
            );
            GameObject? prefab =
                bundle?.GetAsset<GameObject>("SpruceShooterPrefab");
            GameObject? preview =
                bundle?.GetAsset<GameObject>("SpruceShooterPreview");

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
                "Solar Sharpshooter",
                "sss_idle",
                "sss_shoot"
            );

            CustomCore.RegisterCustomPlant<
                SpruceShooter,
                Plants.SolarSharpshooter
            >(
                Plants.SolarSharpshooter.SolarSharpshooterID,
                prefab,
                preview,
                new List<(int, int)>
                {
                    (
                        (int)PlantType.SpruceShooter,
                        (int)PlantType.SunFlower
                    ),
                    (
                        (int)PlantType.SunFlower,
                        (int)PlantType.SpruceShooter
                    )
                },
                Plants.SolarSharpshooter.AttackInterval,
                0f,
                Plants.SolarSharpshooter.Damage,
                Plants.SolarSharpshooter.Toughness,
                Plants.SolarSharpshooter.CardRecharge,
                Plants.SolarSharpshooter.CardCost
            );

            AlmanacEntry almanac = AlmanacContent.SolarSharpshooter;
            CustomCore.AddPlantAlmanacStrings(
                (PlantType)Plants.SolarSharpshooter.SolarSharpshooterID,
                almanac.Name,
                almanac.Info,
                almanac.Introduce,
                Plants.SolarSharpshooter.CardCost
            );

            Plugin.Logger.LogInfo(
                "[Solar Sharpshooter] Registered" +
                " | Plant ID = " +
                Plants.SolarSharpshooter.SolarSharpshooterID +
                " | Projectile = native Bullet_spruce + Solar sprite" +
                " | Sun per pierced zombie = 25"
            );
        }
    }
}

namespace PlantsPlus.Plants
{
    using PlantsPlus.Core;

    public sealed class SolarSharpshooter : MonoBehaviour
    {
        public const int SolarSharpshooterID = 6016;
        public const int Damage = 30;
        public const int Toughness = 300;
        public const int CardCost = 350;
        public const float CardRecharge = 15f;
        public const float AttackInterval = 1.5f;
        public const int PenetrationCount = 5;
        public const int SunPerHit = 25;

        private static bool firstHitLogged;

        public SolarSharpshooter(IntPtr pointer) : base(pointer) { }

        public void Start()
        {
            V11PlantsBootstrap.ApplyNativeShooterControllerWithLocalClips(
                gameObject,
                "Solar Sharpshooter",
                PlantType.SpruceShooter
            );

            SpruceShooter? plant = gameObject.GetComponent<SpruceShooter>();
            if (plant != null)
            {
                V11PlantsBootstrap.EnsureShooterRuntimeReferences(
                    plant,
                    "Solar Sharpshooter"
                );
            }
        }

        private static Transform? FindMuzzle(Transform root)
        {
            for (int index = 0; index < root.childCount; index++)
            {
                Transform child = root.GetChild(index);
                string name = child.name.Replace("_", "").ToLowerInvariant();
                if (name == "zidan" || name.Contains("shoot") ||
                    name.Contains("muzzle"))
                {
                    return child;
                }

                Transform? nested = FindMuzzle(child);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        internal static bool IsSolarPlant(Plant? plant)
        {
            return plant != null &&
                (int)plant.thePlantType == SolarSharpshooterID;
        }

        internal static bool IsSolarBullet(Bullet? bullet)
        {
            if (bullet == null)
                return false;

            if ((int)bullet.fromType == SolarSharpshooterID)
            {
                return true;
            }

            return bullet.from != null &&
                (int)bullet.from.thePlantType == SolarSharpshooterID;
        }

        internal static void ConfigureNativeShot(
            SpruceShooter source,
            Bullet bullet
        )
        {
            if (source == null || bullet == null)
                return;

            bullet.from = source;
            bullet.fromType = (PlantType)SolarSharpshooterID;
            bullet.hitTimes = 0;
            bullet.penetrationTimes = PenetrationCount;
            SolarSharpshooterBootstrap.ApplyProjectileSkin(bullet);
        }

        internal static void DropSun(Bullet bullet, int hitCount)
        {
            if (!IsSolarBullet(bullet) || hitCount <= 0)
                return;

            CreateItem? creator = CreateItem.Instance;
            if (creator == null)
                return;

            int column = bullet.from != null
                ? bullet.from.thePlantColumn
                : 0;
            int created = 0;

            for (int index = 0; index < hitCount; index++)
            {
                GameObject sun = creator.SetCoin(
                    column,
                    bullet.theBulletRow,
                    (int)ItemType.NormalSun,
                    0,
                    bullet.transform.position,
                    true
                );

                if (sun != null)
                    created++;
            }

            if (!firstHitLogged)
            {
                firstHitLogged = true;
                Plugin.Logger.LogInfo(
                    "[Solar Sharpshooter] Piercing hit verified" +
                    " | Physical 25-sun drops = " + created
                );
            }
        }

    }

}
