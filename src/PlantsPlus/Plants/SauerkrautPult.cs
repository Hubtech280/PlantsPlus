using CustomizeLib.MelonLoader;
using HarmonyLib;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class SauerkrautPultBootstrap
    {
        private static bool registered;
        private static bool firstShotLogged;
        private static GameObject? focusVisualPrefab;
        private static GameObject? splashVisualPrefab;
        internal static AnimationClip? FlameClip { get; private set; }

        internal static void OnStart()
        {
            if (registered)
                return;
            registered = true;

            try
            {
                LoadProjectileVisuals();
                RegisterPlant();
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Sauerkraut-pult] Registration failed safely: " +
                    exception
                );
            }
        }

        internal static void OnGameInit()
        {
            OdysseyRegistration.RegisterWeak(
                (PlantType)Plants.SauerkrautPult.SauerkrautPultID,
                "Sauerkraut-pult"
            );
            ConfigureRegisteredPrefab();
            AlmanacCompatibility.RefreshLoadedData();
        }

        private static void LoadProjectileVisuals()
        {
            AssetBundle? bundle = CustomCore.GetAssetBundle(
                Assembly.GetExecutingAssembly(),
                "PlantsPlus.Resources.AssetBundles.sauerkraut_pult_bullet"
            );
            GameObject? focus = bundle?.GetAsset<GameObject>("Bullet_SP_focus");
            GameObject? splash = bundle?.GetAsset<GameObject>("Bullet_SP_splash");

            if (bundle == null || focus == null || splash == null)
                throw new InvalidOperationException(
                    "Projectile bundle, Bullet_SP_focus or Bullet_SP_splash is missing."
                );

            focusVisualPrefab = focus;
            splashVisualPrefab = splash;
        }

        private static void RegisterPlant()
        {
            AssetBundle? bundle = CustomCore.GetAssetBundle(
                Assembly.GetExecutingAssembly(),
                "PlantsPlus.Resources.AssetBundles.sauerkraut_pult"
            );
            GameObject? prefab =
                bundle?.GetAsset<GameObject>("GarlicCabbagePrefab");
            GameObject? preview =
                bundle?.GetAsset<GameObject>("GarlicCabbagePreview");

            if (bundle == null || prefab == null || preview == null)
                throw new InvalidOperationException(
                    "Plant bundle, GarlicCabbagePrefab or preview is missing."
                );

            V11PlantsBootstrap.IsolateAnimationClips(
                bundle,
                prefab,
                "Sauerkraut-pult",
                "idle",
                "shoot",
                "Flame"
            );
            FlameClip = bundle.GetAsset<AnimationClip>("Flame");
            if (FlameClip == null)
            {
                throw new InvalidOperationException(
                    "The Flame animation clip is missing from the plant bundle."
                );
            }

            CustomCore.RegisterCustomPlant<
                GarlicCabbage,
                Plants.SauerkrautPult
            >(
                Plants.SauerkrautPult.SauerkrautPultID,
                prefab,
                preview,
                new List<(int, int)>
                {
                    ((int)PlantType.GarlicCabbage, (int)PlantType.CherryJalapeno),
                    ((int)PlantType.CherryJalapeno, (int)PlantType.GarlicCabbage)
                },
                Plants.SauerkrautPult.AttackInterval,
                0f,
                Plants.SauerkrautPult.SplashDamage,
                Plants.SauerkrautPult.Toughness,
                Plants.SauerkrautPult.CardRecharge,
                Plants.SauerkrautPult.CardCost
            );

            AlmanacEntry almanac = AlmanacContent.SauerkrautPult;
            CustomCore.AddPlantAlmanacStrings(
                (PlantType)Plants.SauerkrautPult.SauerkrautPultID,
                almanac.Name,
                almanac.Info,
                almanac.Introduce,
                Plants.SauerkrautPult.CardCost
            );
            OdysseyRegistration.RegisterWeak(
                (PlantType)Plants.SauerkrautPult.SauerkrautPultID,
                "Sauerkraut-pult"
            );

            Plugin.Logger.LogInfo(
                "[Sauerkraut-pult] Registered" +
                " | Plant ID = " + Plants.SauerkrautPult.SauerkrautPultID +
                " | Fusion = Garbage-pult + Pepper Popper" +
                " | Focus columns = 1-5 | Splash columns = 6-10"
            );
        }

        private static void ConfigureRegisteredPrefab()
        {
            PlantType type =
                (PlantType)Plants.SauerkrautPult.SauerkrautPultID;
            if (GameAPP.resourcesManager == null ||
                GameAPP.resourcesManager.plantPrefabs == null ||
                !GameAPP.resourcesManager.plantPrefabs.ContainsKey(type))
            {
                return;
            }

            GameObject? prefab = GameAPP.resourcesManager.plantPrefabs[type];
            GarlicCabbage? thrower = prefab?.GetComponent<GarlicCabbage>();
            if (prefab == null || thrower == null)
                return;

            V11PlantsBootstrap.EnsureShooterRuntimeReferences(
                thrower,
                "Sauerkraut-pult"
            );
            V11PlantsBootstrap.ApplyNativeShooterControllerWithLocalClips(
                prefab,
                "Sauerkraut-pult",
                PlantType.GarlicCabbage
            );
        }

        internal static void ConfigureShot(
            GarlicCabbage source,
            Zombie target,
            Bullet bullet
        )
        {
            if (source == null || target == null || bullet == null)
                return;

            int column = Lawnf.GetColumnFromX(target.transform.position.x) + 1;
            bool focus = column <= Plants.SauerkrautPult.FocusLastColumn;

            bullet.from = source;
            bullet.fromType =
                (PlantType)Plants.SauerkrautPult.SauerkrautPultID;
            bullet.Damage = focus
                ? Plants.SauerkrautPult.FocusDamage
                : Plants.SauerkrautPult.SplashDamage;

            Plants.SauerkrautProjectileMarker? marker =
                bullet.gameObject.GetComponent<Plants.SauerkrautProjectileMarker>();
            if (marker == null)
            {
                marker = bullet.gameObject.AddComponent<
                    Plants.SauerkrautProjectileMarker
                >();
            }

            marker.FocusMode = focus;
            marker.Consumed = false;
            marker.TargetInstanceID = target.GetInstanceID();
            marker.PreviousHorizontalDelta = float.PositiveInfinity;
            ApplyProjectileSkin(bullet, focus);

            if (!firstShotLogged)
            {
                firstShotLogged = true;
                Plugin.Logger.LogInfo(
                    "[Sauerkraut-pult] First shot verified" +
                    " | Target column = " + column +
                    " | Mode = " + (focus ? "Focus" : "Splash") +
                    " | Damage = " + bullet.Damage
                );
            }
        }

        private static void ApplyProjectileSkin(Bullet bullet, bool focus)
        {
            GameObject? visualPrefab = focus
                ? focusVisualPrefab
                : splashVisualPrefab;
            if (visualPrefab == null)
                return;

            SpriteRenderer[] renderers =
                bullet.GetComponentsInChildren<SpriteRenderer>(true);
            Transform? visualAnchor = null;
            SpriteRenderer? nativeRenderer = null;
            for (int index = 0; index < renderers.Length; index++)
            {
                SpriteRenderer renderer = renderers[index];
                if (renderer == null ||
                    IsSauerkrautVisual(renderer.transform) ||
                    renderer.gameObject.name.Equals(
                    "Shadow",
                    StringComparison.OrdinalIgnoreCase
                ))
                {
                    continue;
                }

                if (visualAnchor == null)
                {
                    visualAnchor = renderer.transform;
                    nativeRenderer = renderer;
                }
                renderer.enabled = false;
            }

            if (visualAnchor == null)
                visualAnchor = bullet.transform;

            GameObject visual = UnityEngine.Object.Instantiate(visualPrefab);
            visual.name = focus
                ? "SauerkrautFocusVisual"
                : "SauerkrautSplashVisual";
            visual.transform.SetParent(visualAnchor, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            Vector3 desiredWorldScale = visualPrefab.transform.localScale;
            Vector3 parentScale = visualAnchor.lossyScale;
            visual.transform.localScale = new Vector3(
                SafeScale(desiredWorldScale.x, parentScale.x),
                SafeScale(desiredWorldScale.y, parentScale.y),
                SafeScale(desiredWorldScale.z, parentScale.z)
            );

            Collider2D[] colliders =
                visual.GetComponentsInChildren<Collider2D>(true);
            for (int index = 0; index < colliders.Length; index++)
                colliders[index].enabled = false;

            Rigidbody2D[] bodies =
                visual.GetComponentsInChildren<Rigidbody2D>(true);
            for (int index = 0; index < bodies.Length; index++)
                bodies[index].simulated = false;

            SpriteRenderer[] customRenderers =
                visual.GetComponentsInChildren<SpriteRenderer>(true);
            for (int index = 0; index < customRenderers.Length; index++)
            {
                SpriteRenderer renderer = customRenderers[index];
                if (renderer.gameObject.name.Equals(
                    "Shadow",
                    StringComparison.OrdinalIgnoreCase
                ))
                {
                    renderer.enabled = false;
                }
                else
                {
                    renderer.enabled = true;
                    if (nativeRenderer != null)
                    {
                        renderer.sortingLayerID =
                            nativeRenderer.sortingLayerID;
                        renderer.sortingOrder =
                            nativeRenderer.sortingOrder + 1;
                    }
                }
            }
        }

        internal static void ResetProjectileSkin(Bullet bullet)
        {
            if (bullet == null)
                return;

            SpriteRenderer[] renderers =
                bullet.GetComponentsInChildren<SpriteRenderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                SpriteRenderer renderer = renderers[index];
                if (renderer == null)
                    continue;

                if (IsSauerkrautVisual(renderer.transform))
                    continue;

                if (!renderer.gameObject.name.Equals(
                    "Shadow",
                    StringComparison.OrdinalIgnoreCase))
                {
                    renderer.enabled = true;
                }
            }

            for (int index = bullet.transform.childCount - 1;
                 index >= 0;
                 index--)
            {
                Transform child = bullet.transform.GetChild(index);
                RemoveSauerkrautVisuals(child);
            }

            Plants.SauerkrautProjectileMarker? marker =
                bullet.GetComponent<Plants.SauerkrautProjectileMarker>();
            if (marker != null)
            {
                marker.Consumed = true;
                marker.TargetInstanceID = 0;
            }
        }

        private static void RemoveSauerkrautVisuals(Transform root)
        {
            if (root == null)
                return;

            if (root.name.StartsWith(
                "Sauerkraut",
                StringComparison.Ordinal))
            {
                root.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(root.gameObject);
                return;
            }

            for (int index = root.childCount - 1; index >= 0; index--)
                RemoveSauerkrautVisuals(root.GetChild(index));
        }

        private static bool IsSauerkrautVisual(Transform candidate)
        {
            Transform? current = candidate;
            while (current != null)
            {
                if (current.name.StartsWith(
                    "Sauerkraut",
                    StringComparison.Ordinal))
                {
                    return true;
                }
                current = current.parent;
            }
            return false;
        }

        private static float SafeScale(float desired, float parent)
        {
            return Mathf.Abs(parent) > 0.0001f
                ? desired / parent
                : desired;
        }
    }
}

namespace PlantsPlus.Plants
{
    using PlantsPlus.Core;

    public sealed class SauerkrautProjectileMarker : MonoBehaviour
    {
        public bool FocusMode;
        public bool Consumed;
        public int TargetInstanceID;
        public float PreviousHorizontalDelta;

        public SauerkrautProjectileMarker(IntPtr pointer) : base(pointer) { }

        public void Update()
        {
            // The native Garlic Cabbage collision owns impact and damage.
            // Manual proximity damage here bypassed its setup and could
            // corrupt a dying zombie's removable-animation bookkeeping.
        }

        private Zombie? FindTarget()
        {
            var zombies = Lawnf.GetAllZombies(false);
            if (zombies == null)
                return null;

            for (int index = 0; index < zombies.Count; index++)
            {
                Zombie? zombie = zombies[index];
                if (zombie != null &&
                    zombie.GetInstanceID() == TargetInstanceID &&
                    zombie.Alive && zombie.theHealth > 0)
                {
                    return zombie;
                }
            }
            return null;
        }
    }

    public sealed class SauerkrautPult : MonoBehaviour
    {
        public const int SauerkrautPultID = 6029;
        public const int FocusDamage = 300;
        public const int SplashDamage = 100;
        public const int Toughness = 300;
        public const int CardCost = 400;
        public const float CardRecharge = 15f;
        public const float AttackInterval = 3f;
        public const int FocusLastColumn = 5;
        public const float DebuffDuration = 10f;

        private static bool focusImpactLogged;
        private static bool splashImpactLogged;

        private bool animationInitialized;
        private float flameTime;

        public SauerkrautPult(IntPtr pointer) : base(pointer) { }

        public void Start()
        {
            animationInitialized = false;
            flameTime = 0f;

            GarlicCabbage? plant = gameObject.GetComponent<GarlicCabbage>();
            if (plant != null)
            {
                V11PlantsBootstrap.EnsureShooterRuntimeReferences(
                    plant,
                    "Sauerkraut-pult"
                );
                plant.thePlantAttackInterval = AttackInterval;
                plant.attackDamage = SplashDamage;
            }
        }

        public void Update()
        {
            if (!animationInitialized)
            {
                animationInitialized = true;
                V11PlantsBootstrap.ApplyNativeShooterControllerWithLocalClips(
                    gameObject,
                    "Sauerkraut-pult",
                    PlantType.GarlicCabbage
                );
            }

            AnimationClip? flame = SauerkrautPultBootstrap.FlameClip;
            if (flame != null)
            {
                flameTime = Mathf.Repeat(
                    flameTime + Time.deltaTime,
                    Mathf.Max(0.01f, flame.length)
                );
                flame.SampleAnimation(gameObject, flameTime);
            }
        }

        internal static bool IsSauerkraut(Plant? plant)
        {
            return plant != null && (int)plant.thePlantType == SauerkrautPultID;
        }

        internal static void ApplyFocusEffects(Zombie zombie)
        {
            if (zombie == null)
                return;

            try
            {
                EffectManager.SetEffect(
                    zombie,
                    EffectType.Jala,
                    DebuffDuration,
                    1f
                );
            }
            catch { }

            try
            {
                CreateParticle.SetParticle(
                    (int)ParticleType.JalaedCloudSmall,
                    zombie.ColliderCenter,
                    zombie.theZombieRow,
                    true
                );
            }
            catch { }

            if (!focusImpactLogged)
            {
                focusImpactLogged = true;
                Plugin.Logger.LogInfo(
                    "[Sauerkraut-pult] Focus impact verified" +
                    " | Damage = " + FocusDamage +
                    " | Enflamed = " + DebuffDuration + "s"
                );
            }
        }

        internal static void SplashOtherZombies(Bullet bullet, Zombie center)
        {
            if (bullet == null || center == null)
                return;

            int centerColumn = Lawnf.GetColumnFromX(center.transform.position.x);
            var zombies = Lawnf.GetAllZombies(false);
            if (zombies == null)
                return;

            int centerID = center.GetInstanceID();
            int splashed = 0;
            for (int index = 0; index < zombies.Count; index++)
            {
                Zombie? zombie = zombies[index];
                if (zombie == null || zombie.GetInstanceID() == centerID ||
                    !zombie.Alive || zombie.theHealth <= 0 ||
                    zombie.isMindControlled ||
                    Math.Abs(zombie.theZombieRow - center.theZombieRow) > 1)
                {
                    continue;
                }

                int column = Lawnf.GetColumnFromX(zombie.transform.position.x);
                if (Math.Abs(column - centerColumn) > 1)
                    continue;

                DamageZombie(bullet, zombie, SplashDamage);
                splashed++;
                try
                {
                    zombie.SetPoison(DebuffDuration);
                    zombie.AddPoisonLevel();
                }
                catch { }
            }

            try
            {
                center.SetPoison(DebuffDuration);
            }
            catch { }

            if (!splashImpactLogged)
            {
                splashImpactLogged = true;
                Plugin.Logger.LogInfo(
                    "[Sauerkraut-pult] Splash impact verified" +
                    " | Primary damage = " + SplashDamage +
                    " | Additional zombies = " + splashed +
                    " | Poison = " + DebuffDuration + "s"
                );
            }
        }

        private static void DamageZombie(
            Bullet bullet,
            Zombie zombie,
            int damage
        )
        {
            try
            {
                IDamageMaker? maker = bullet.from != null
                    ? bullet.from.ToIDamageMaker()
                    : null;
                ((Entity)zombie).TakeDamage(
                    damage,
                    maker,
                    DamageType.Normal,
                    (PlantType)SauerkrautPultID,
                    false
                );
            }
            catch
            {
                // Loader-safe release fix: never fall back to direct
                // Zombie.ApplyDamage here. The native model-loss path can
                // receive an invalid part index after overlapping custom hits.
            }
        }
    }

    internal static class SauerkrautPultPatches
    {
        [HarmonyPatch(typeof(CreateBullet), nameof(CreateBullet.SetBullet))]
        private static class CreateBullet_SetBullet_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(
                float x,
                float y,
                int theRow,
                BulletType theBulletType,
                Bullet __result
            )
            {
                if (__result == null ||
                    theBulletType != BulletType.Bullet_garlicCabbage)
                    return;

                // Bullet_garlicCabbage is pooled. Always erase the custom
                // skin first so a later native Garlic Cabbage shot cannot
                // inherit Sauerkraut-pult's projectile.
                SauerkrautPultBootstrap.ResetProjectileSkin(__result);

                GarlicCabbage? garlic = FindSource(theRow, x, y);
                Zombie? target = garlic?.targetZombie;
                if (garlic == null || target == null)
                    return;

                SauerkrautPultBootstrap.ConfigureShot(
                    garlic,
                    target,
                    __result
                );
            }

            private static GarlicCabbage? FindSource(
                int row,
                float shotX,
                float shotY
            )
            {
                var plants = Lawnf.GetAllPlants();
                if (plants == null)
                    return null;

                GarlicCabbage? nearest = null;
                float nearestDistance = 3f;
                for (int index = 0; index < plants.Count; index++)
                {
                    Plant? plant = plants[index];
                    if (!SauerkrautPult.IsSauerkraut(plant) ||
                        plant!.thePlantRow != row || plant.targetZombie == null)
                    {
                        continue;
                    }

                    float dx = plant.transform.position.x - shotX;
                    float dy = plant.transform.position.y - shotY;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    if (distance >= nearestDistance)
                        continue;

                    GarlicCabbage? candidate = plant.TryCast<GarlicCabbage>();
                    if (candidate != null)
                    {
                        nearest = candidate;
                        nearestDistance = distance;
                    }
                }
                return nearest;
            }
        }

        [HarmonyPatch(typeof(Bullet), "CheckZombie")]
        private static class Bullet_CheckZombie_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static bool Prefix(
                Bullet __instance,
                Zombie zombie
            )
            {
                SauerkrautProjectileMarker? marker =
                    __instance.gameObject.GetComponent<
                        SauerkrautProjectileMarker
                    >();
                if (marker == null || marker.Consumed)
                    return true;

                if (marker.FocusMode)
                {
                    marker.Consumed = true;
                    SauerkrautPult.ApplyFocusEffects(zombie);
                    return true;
                }

                SauerkrautPult.SplashOtherZombies(__instance, zombie);
                return true;
            }
        }
    }
}
