using CustomizeLib.MelonLoader;
using HarmonyLib;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class PineshooterBootstrap
    {
        private const float PreviewScaleMultiplier = 1.65f;
        private const float NativeShadowLocalY = 0.8f;
        private const float NativeShadowScale = 7.1613f;
        private const string RuntimeShadowName =
            "PlantsPlus_PineshooterShadow";
        private static bool registered;
        private static bool shadowSetupLogged;
        private static bool runtimeShadowLogged;
        private static Sprite? projectileSprite;
        private static Vector3 projectileLocalPosition;
        private static Vector3 projectileLocalScale = Vector3.one;
        private static Quaternion projectileLocalRotation =
            Quaternion.identity;
        private static readonly Dictionary<int, ProjectileVisualState>
            PineProjectileVisuals =
                new Dictionary<int, ProjectileVisualState>();

        private sealed class ProjectileVisualState
        {
            internal readonly SpriteRenderer Renderer;
            internal readonly Sprite? Sprite;
            internal readonly Vector3 LocalPosition;
            internal readonly Vector3 LocalScale;
            internal readonly Quaternion LocalRotation;

            internal ProjectileVisualState(SpriteRenderer renderer)
            {
                Renderer = renderer;
                Sprite = renderer.sprite;
                LocalPosition = renderer.transform.localPosition;
                LocalScale = renderer.transform.localScale;
                LocalRotation = renderer.transform.localRotation;
            }

            internal void Restore()
            {
                if (Renderer == null || Renderer.transform == null)
                    return;

                Renderer.sprite = Sprite;
                Renderer.transform.localPosition = LocalPosition;
                Renderer.transform.localScale = LocalScale;
                Renderer.transform.localRotation = LocalRotation;
            }
        }

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
                    "[Pineshooter] Registration failed safely: " +
                    exception
                );
            }
        }

        public static void OnGameInit()
        {
            InstallTypeFlags();
            RestoreAllProjectileSkins();
            Plants.Pineshooter.ResetRuntimeState();

            PlantType type =
                (PlantType)Plants.Pineshooter.PineshooterID;

            if (!CustomCore.CustomPlants.ContainsKey(type))
                return;

            AlmanacCompatibility.RefreshLoadedData();
            ConfigureRegisteredPrefab();
        }

        private static void LoadProjectileSkin()
        {
            AssetBundle? bundle = CustomCore.GetAssetBundle(
                Assembly.GetExecutingAssembly(),
                "PlantsPlus.Resources.AssetBundles.bullet_pineshooter"
            );
            GameObject? prefab =
                bundle?.GetAsset<GameObject>("Bullet_pea");

            if (bundle == null || prefab == null)
            {
                throw new InvalidOperationException(
                    "Projectile bundle or Bullet_pea is missing."
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
                projectileLocalPosition =
                    renderer.transform.localPosition;
                projectileLocalScale = renderer.transform.localScale;
                projectileLocalRotation =
                    renderer.transform.localRotation;
                break;
            }

            if (projectileSprite == null)
            {
                throw new InvalidOperationException(
                    "Pineshooter projectile sprite is missing."
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

                int projectileID = bullet.GetInstanceID();
                if (!PineProjectileVisuals.ContainsKey(projectileID))
                {
                    PineProjectileVisuals[projectileID] =
                        new ProjectileVisualState(renderer);
                }

                renderer.sprite = projectileSprite;
                renderer.transform.localPosition =
                    projectileLocalPosition;
                renderer.transform.localScale = projectileLocalScale;
                renderer.transform.localRotation =
                    projectileLocalRotation;
                return;
            }
        }

        internal static void RestoreProjectileSkin(Bullet bullet)
        {
            if (bullet == null)
                return;

            int projectileID = bullet.GetInstanceID();
            if (!PineProjectileVisuals.TryGetValue(
                    projectileID,
                    out ProjectileVisualState? state
                ))
            {
                return;
            }

            try
            {
                state.Restore();
            }
            finally
            {
                PineProjectileVisuals.Remove(projectileID);
            }
        }

        private static void RestoreAllProjectileSkins()
        {
            foreach (
                KeyValuePair<int, ProjectileVisualState> pair
                in PineProjectileVisuals
            )
            {
                try
                {
                    pair.Value.Restore();
                }
                catch
                {
                    // The previous board may already have destroyed it.
                }
            }

            PineProjectileVisuals.Clear();
        }

        private static void RegisterPlant()
        {
            AssetBundle? bundle = CustomCore.GetAssetBundle(
                Assembly.GetExecutingAssembly(),
                "PlantsPlus.Resources.AssetBundles.pineshooter"
            );
            GameObject? prefab =
                bundle?.GetAsset<GameObject>("PeashooterPrefab");
            GameObject? preview =
                bundle?.GetAsset<GameObject>("PeaShooterPreview");

            if (bundle == null || prefab == null || preview == null)
            {
                throw new InvalidOperationException(
                    "Plant bundle, PeashooterPrefab or PeaShooterPreview " +
                    "is missing."
                );
            }

            prefab.transform.localPosition = Vector3.zero;
            prefab.transform.localRotation = Quaternion.identity;
            EnsureShadowVisible(prefab);

            Vector3 previewScale = preview.transform.localScale;
            preview.transform.localScale = new Vector3(
                previewScale.x * PreviewScaleMultiplier,
                previewScale.y * PreviewScaleMultiplier,
                previewScale.z
            );

            V11PlantsBootstrap.IsolateAnimationClips(
                bundle,
                prefab,
                "Pineshooter",
                "ps_idle",
                "ps_shoot"
            );

            CustomCore.RegisterCustomPlant<
                PeaShooter,
                Plants.Pineshooter
            >(
                Plants.Pineshooter.PineshooterID,
                prefab,
                preview,
                new List<(int, int)>
                {
                    (
                        (int)PlantType.Peashooter,
                        (int)PlantType.SpruceShooter
                    ),
                    (
                        (int)PlantType.SpruceShooter,
                        (int)PlantType.Peashooter
                    )
                },
                Plants.Pineshooter.AttackInterval,
                0f,
                Plants.Pineshooter.Damage,
                Plants.Pineshooter.Toughness,
                Plants.Pineshooter.CardRecharge,
                Plants.Pineshooter.CardCost
            );

            AlmanacEntry almanac = AlmanacContent.Pineshooter;
            CustomCore.AddPlantAlmanacStrings(
                (PlantType)Plants.Pineshooter.PineshooterID,
                almanac.Name,
                almanac.Info,
                almanac.Introduce,
                Plants.Pineshooter.CardCost
            );

            Plugin.Logger.LogInfo(
                "[Pineshooter] Registered" +
                " | Plant ID = " +
                Plants.Pineshooter.PineshooterID +
                " | Damage = " + Plants.Pineshooter.Damage +
                " | Knockback = " +
                Plants.Pineshooter.KnockbackDistance +
                " | Collision damage = " +
                Plants.Pineshooter.CollisionDamage +
                " | Collision stun = " +
                Plants.Pineshooter.CollisionStunDuration + "s"
            );
        }

        private static void InstallTypeFlags()
        {
            PlantType type =
                (PlantType)Plants.Pineshooter.PineshooterID;

            if (!CustomCore.TypeMgrExtra.IsIcePlant.Contains(type))
                CustomCore.TypeMgrExtra.IsIcePlant.Add(type);
        }

        private static void ConfigureRegisteredPrefab()
        {
            PlantType type =
                (PlantType)Plants.Pineshooter.PineshooterID;

            if (GameAPP.resourcesManager == null ||
                GameAPP.resourcesManager.plantPrefabs == null ||
                !GameAPP.resourcesManager.plantPrefabs.ContainsKey(type))
            {
                return;
            }

            GameObject? prefab =
                GameAPP.resourcesManager.plantPrefabs[type];
            PeaShooter? shooter =
                prefab?.GetComponent<PeaShooter>();

            if (prefab == null || shooter == null)
                return;

            V11PlantsBootstrap.EnsureShooterRuntimeReferences(
                shooter,
                "Pineshooter"
            );
            V11PlantsBootstrap.ApplyNativeShooterControllerWithLocalClips(
                prefab,
                "Pineshooter",
                PlantType.Peashooter
            );
            ApplyNativeShadow(prefab, false);
        }

        internal static void EnsureShadowVisible(GameObject root)
        {
            if (root == null)
                return;

            Transform shadow = root.transform.Find("Shadow");
            if (shadow == null)
                return;

            shadow.gameObject.SetActive(true);
            SpriteRenderer renderer =
                shadow.GetComponent<SpriteRenderer>();
            if (renderer == null)
                return;

            renderer.enabled = true;
            renderer.color = Color.white;
        }

        internal static void ApplyNativeShadow(
            GameObject root,
            bool compensateVisualScale
        )
        {
            if (root == null)
                return;

            EnsureShadowVisible(root);
            Transform shadow = root.transform.Find("Shadow");
            if (shadow == null)
                return;

            SpriteRenderer renderer =
                shadow.GetComponent<SpriteRenderer>();
            if (renderer == null)
                return;

            float compensation =
                compensateVisualScale
                    ? 1f / Plants.Pineshooter.VisualScale
                    : 1f;

            // Apply the native Peashooter transform first. This fallback is
            // independent from ResourcesManager initialization order.
            shadow.localPosition = new Vector3(
                0f,
                NativeShadowLocalY * compensation,
                0f
            );
            shadow.localScale = new Vector3(
                NativeShadowScale * compensation,
                NativeShadowScale * compensation,
                NativeShadowScale
            );
            shadow.localRotation = Quaternion.identity;
            shadow.gameObject.layer = 9;
            shadow.gameObject.SetActive(true);
            renderer.enabled = true;
            renderer.color = Color.black;
            renderer.sortingLayerID = 0;
            renderer.sortingOrder = 0;

            bool copiedNativeRenderer = false;
            if (GameAPP.resourcesManager != null &&
                GameAPP.resourcesManager.plantPrefabs != null &&
                GameAPP.resourcesManager.plantPrefabs.ContainsKey(
                    PlantType.Peashooter
                ))
            {
                GameObject nativeRoot =
                    GameAPP.resourcesManager.plantPrefabs[
                        PlantType.Peashooter
                    ];
                Transform? nativeShadow =
                    nativeRoot != null
                        ? nativeRoot.transform.Find("Shadow")
                        : null;
                SpriteRenderer? nativeRenderer =
                    nativeShadow != null
                        ? nativeShadow.GetComponent<SpriteRenderer>()
                        : null;

                if (nativeRenderer != null)
                {
                    renderer.sprite = nativeRenderer.sprite;
                    renderer.sharedMaterial =
                        nativeRenderer.sharedMaterial;
                    renderer.color = nativeRenderer.color;
                    renderer.sortingLayerID =
                        nativeRenderer.sortingLayerID;
                    renderer.sortingOrder =
                        nativeRenderer.sortingOrder;
                    copiedNativeRenderer = true;
                }
            }

            if (!shadowSetupLogged)
            {
                shadowSetupLogged = true;
                Plugin.Logger.LogInfo(
                    "[Pineshooter] Shadow configured" +
                    " | Native renderer = " + copiedNativeRenderer +
                    " | Scale = " + shadow.localScale +
                    " | Position = " + shadow.localPosition +
                    " | Sprite = " +
                    (renderer.sprite != null
                        ? renderer.sprite.name
                        : "<missing>")
                );
            }
        }

        internal static void CreateRuntimeShadow(GameObject root)
        {
            if (root == null)
                return;

            Transform existing =
                root.transform.Find(RuntimeShadowName);
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                SpriteRenderer existingRenderer =
                    existing.GetComponent<SpriteRenderer>();
                if (existingRenderer != null)
                    existingRenderer.enabled = true;
                return;
            }

            SpriteRenderer? source = null;
            if (GameAPP.resourcesManager != null &&
                GameAPP.resourcesManager.plantPrefabs != null &&
                GameAPP.resourcesManager.plantPrefabs.ContainsKey(
                    PlantType.Peashooter
                ))
            {
                GameObject nativeRoot =
                    GameAPP.resourcesManager.plantPrefabs[
                        PlantType.Peashooter
                    ];
                Transform? nativeShadow =
                    nativeRoot != null
                        ? nativeRoot.transform.Find("Shadow")
                        : null;
                source = nativeShadow != null
                    ? nativeShadow.GetComponent<SpriteRenderer>()
                    : null;
            }

            if (source == null || source.sprite == null)
                return;

            GameObject runtimeShadow =
                new GameObject(RuntimeShadowName);
            runtimeShadow.layer = 9;
            runtimeShadow.transform.SetParent(
                root.transform,
                false
            );
            runtimeShadow.transform.localPosition =
                new Vector3(
                    0f,
                    -0.45f / Plants.Pineshooter.VisualScale,
                    0f
                );
            runtimeShadow.transform.localScale =
                new Vector3(
                    NativeShadowScale /
                        Plants.Pineshooter.VisualScale,
                    NativeShadowScale /
                        Plants.Pineshooter.VisualScale,
                    NativeShadowScale
                );

            SpriteRenderer renderer =
                runtimeShadow.AddComponent<SpriteRenderer>();
            renderer.sprite = source.sprite;
            renderer.sharedMaterial = source.sharedMaterial;
            renderer.color = new Color(0f, 0f, 0f, 0.65f);
            renderer.sortingLayerID = source.sortingLayerID;
            renderer.sortingOrder = 100;
            renderer.enabled = true;
            runtimeShadow.SetActive(true);

            Transform bundledShadow = root.transform.Find("Shadow");
            if (bundledShadow != null)
                bundledShadow.gameObject.SetActive(false);

            if (!runtimeShadowLogged)
            {
                runtimeShadowLogged = true;
                Plugin.Logger.LogInfo(
                    "[Pineshooter] Independent runtime shadow created" +
                    " | Scale = " +
                    runtimeShadow.transform.localScale +
                    " | Position = " +
                    runtimeShadow.transform.localPosition +
                    " | Order = " + renderer.sortingOrder
                );
            }
        }
    }
}

namespace PlantsPlus.Plants
{
    using PlantsPlus.Core;

    public sealed class Pineshooter : MonoBehaviour
    {
        public const int PineshooterID = 6018;
        public const int Damage = 40;
        public const int Toughness = 300;
        public const int CardCost = 275;
        public const float CardRecharge = 15f;
        public const float AttackInterval = 1.5f;
        public const float KnockbackDistance = 0.65f;
        public const float CollisionReach = 0.85f;
        public const int CollisionDamage = 40;
        public const float CollisionStunDuration = 0.5f;
        public const float VisualScale = 0.65f;
        public const float ProjectileVerticalOffset = 0.12f;

        private static readonly HashSet<int> ProcessedProjectiles =
            new HashSet<int>();
        private static bool shotLogged;
        private static bool knockbackLogged;
        private static bool collisionLogged;
        private static bool warningLogged;
        private static bool creatorWarningLogged;
        private bool visualScaleReady;
        private Vector3 lastAppliedRootScale;

        public Pineshooter(IntPtr pointer) : base(pointer) { }

        public void Start()
        {
            ApplyVisualScale();

            PeaShooter? plant =
                gameObject.GetComponent<PeaShooter>();

            if (plant == null)
                return;

            V11PlantsBootstrap.EnsureShooterRuntimeReferences(
                plant,
                "Pineshooter"
            );
            V11PlantsBootstrap.ApplyNativeShooterControllerWithLocalClips(
                gameObject,
                "Pineshooter",
                PlantType.Peashooter
            );
        }

        public void LateUpdate()
        {
            if (!visualScaleReady)
                return;

            Vector3 current = transform.localScale;

            // If the Animator authored a new root scale this frame, apply the
            // same global size factor to that animated value. If nothing
            // changed, do nothing so the factor cannot accumulate every frame.
            if (Approximately(current, lastAppliedRootScale))
                return;

            lastAppliedRootScale = ScaleXY(current, VisualScale);
            transform.localScale = lastAppliedRootScale;
        }

        private void ApplyVisualScale()
        {
            if (visualScaleReady)
                return;

            PineshooterBootstrap.ApplyNativeShadow(
                gameObject,
                true
            );
            PineshooterBootstrap.CreateRuntimeShadow(gameObject);

            BoxCollider2D[] colliders =
                gameObject.GetComponents<BoxCollider2D>();

            for (int index = 0; index < colliders.Length; index++)
            {
                BoxCollider2D collider = colliders[index];

                if (collider == null)
                    continue;

                // Root scaling must remain purely visual. Compensate the local
                // collider dimensions so their world-space gameplay size does
                // not shrink with the artwork.
                collider.size = new Vector2(
                    collider.size.x / VisualScale,
                    collider.size.y / VisualScale
                );
                collider.offset = new Vector2(
                    collider.offset.x / VisualScale,
                    collider.offset.y / VisualScale
                );
            }

            lastAppliedRootScale =
                ScaleXY(transform.localScale, VisualScale);
            transform.localScale = lastAppliedRootScale;
            visualScaleReady = true;

            Plugin.Logger.LogInfo(
                "[Pineshooter] Runtime visual scale applied" +
                " | Factor = " + VisualScale +
                " | Colliders compensated = " + colliders.Length
            );
        }

        private static Vector3 ScaleXY(Vector3 value, float factor)
        {
            return new Vector3(
                value.x * factor,
                value.y * factor,
                value.z
            );
        }

        private static bool Approximately(Vector3 left, Vector3 right)
        {
            return Mathf.Approximately(left.x, right.x) &&
                Mathf.Approximately(left.y, right.y) &&
                Mathf.Approximately(left.z, right.z);
        }

        internal static bool IsPineshooter(Plant? plant)
        {
            return plant != null &&
                (int)plant.thePlantType == PineshooterID;
        }

        internal static bool IsPineshooterBullet(Bullet? bullet)
        {
            if (bullet == null)
                return false;

            return (int)bullet.fromType == PineshooterID ||
                IsPineshooter(bullet.from);
        }

        internal static void ConfigureNativeShot(
            PeaShooter source,
            Bullet bullet
        )
        {
            if (source == null || bullet == null)
                return;

            int projectileID = bullet.GetInstanceID();
            ProcessedProjectiles.Remove(projectileID);

            bullet.from = source;
            bullet.fromType = (PlantType)PineshooterID;
            PineshooterBootstrap.ApplyProjectileSkin(bullet);

            if (!shotLogged)
            {
                shotLogged = true;
                Plugin.Logger.LogInfo(
                    "[Pineshooter] Native pine shot verified" +
                    " | Bullet = " + bullet.theBulletType +
                    " | Damage = " + bullet.Damage
                );
            }
        }

        internal static Bullet? CreateDirectShot(PeaShooter source)
        {
            if (source == null)
                return null;

            V11PlantsBootstrap.EnsureShooterRuntimeReferences(
                source,
                "Pineshooter"
            );

            CreateBullet? creator = CreateBullet.Instance;

            if (creator == null && Board.Instance != null)
                creator = Board.Instance.GetComponent<CreateBullet>();

            if (creator == null)
            {
                if (!creatorWarningLogged)
                {
                    creatorWarningLogged = true;
                    Plugin.Logger.LogError(
                        "[Pineshooter] Direct shot failed: " +
                        "CreateBullet is null."
                    );
                }

                return null;
            }

            Transform origin = source.shoot != null
                ? source.shoot
                : source.transform;
            Vector3 position = origin.position;

            Bullet bullet = creator.SetBullet(
                position.x + 0.1f,
                position.y + ProjectileVerticalOffset,
                source.thePlantRow,
                BulletType.Bullet_pea,
                BulletMoveWay.MoveRight,
                false
            );

            if (bullet == null)
            {
                Plugin.Logger.LogError(
                    "[Pineshooter] Direct shot failed: " +
                    "SetBullet returned null."
                );
                return null;
            }

            bullet.Damage = source.attackDamage > 0
                ? source.attackDamage
                : Damage;
            bullet.shootingLevel = source.shootingLevel;
            ConfigureNativeShot(source, bullet);

            return bullet;
        }

        internal static void HandleFirstHit(
            Bullet bullet,
            Zombie primary
        )
        {
            if (!IsPineshooterBullet(bullet) || primary == null ||
                !primary.Alive || primary.theHealth <= 0)
            {
                return;
            }

            int projectileID = bullet.GetInstanceID();

            if (!ProcessedProjectiles.Add(projectileID))
                return;

            try
            {
                Zombie? collisionTarget =
                    FindCollisionTarget(primary);

                primary.KnockBack(
                    KnockbackDistance,
                    Zombie.KnockBackReason.Normal
                );

                if (!knockbackLogged)
                {
                    knockbackLogged = true;
                    Plugin.Logger.LogInfo(
                        "[Pineshooter] Pine knockback verified" +
                        " | Distance = " + KnockbackDistance
                    );
                }

                if (collisionTarget == null)
                    return;

                ApplyCollisionDamage(bullet, primary);
                ApplyCollisionDamage(bullet, collisionTarget);

                primary.Buttered(CollisionStunDuration, false);
                collisionTarget.Buttered(
                    CollisionStunDuration,
                    false
                );

                if (!collisionLogged)
                {
                    collisionLogged = true;
                    Plugin.Logger.LogInfo(
                        "[Pineshooter] Zombie collision verified" +
                        " | Bonus damage to both = " +
                        CollisionDamage +
                        " | Stun = " +
                        CollisionStunDuration + "s"
                    );
                }
            }
            catch (Exception exception)
            {
                if (warningLogged)
                    return;

                warningLogged = true;
                Plugin.Logger.LogWarning(
                    "[Pineshooter] Impact mechanic failed safely: " +
                    exception.Message
                );
            }
        }

        private static Zombie? FindCollisionTarget(Zombie primary)
        {
            var zombies = Lawnf.GetZombiesByRow(
                primary.theZombieRow,
                false
            );

            if (zombies == null)
                return null;

            float primaryX = primary.transform.position.x;
            float maximumX = primaryX + CollisionReach;
            Zombie? nearest = null;
            float nearestX = float.MaxValue;

            for (int index = 0; index < zombies.Count; index++)
            {
                Zombie zombie = zombies[index];

                if (zombie == null ||
                    zombie.gameObject == primary.gameObject ||
                    !zombie.Alive ||
                    zombie.theHealth <= 0 ||
                    zombie.isMindControlled)
                {
                    continue;
                }

                float zombieX = zombie.transform.position.x;

                // Zombies walk from right to left. A normal knockback sends
                // the struck zombie back toward the right.
                if (zombieX <= primaryX + 0.05f ||
                    zombieX > maximumX ||
                    zombieX >= nearestX)
                {
                    continue;
                }

                nearest = zombie;
                nearestX = zombieX;
            }

            return nearest;
        }

        private static void ApplyCollisionDamage(
            Bullet source,
            Zombie target
        )
        {
            if (target == null || !target.Alive ||
                target.theHealth <= 0)
            {
                return;
            }

            ((Entity)target).TakeDamage(
                CollisionDamage,
                source.ToIDamageMaker(),
                DamageType.Normal,
                (PlantType)PineshooterID,
                false
            );
        }

        internal static void ResetRuntimeState()
        {
            ProcessedProjectiles.Clear();
        }
    }

    internal static class PineshooterPatches
    {
        [HarmonyPatch(
            typeof(CreateBullet),
            nameof(CreateBullet.SetBullet)
        )]
        private static class CreateBullet_SetBullet_PineVisualReset_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.First)]
            private static void Postfix(Bullet __result)
            {
                // Bullet_pea objects are pooled. Remove a previous pine skin
                // before the pooled object is handed to Peashooter, Gatling
                // or any other new owner.
                PineshooterBootstrap.RestoreProjectileSkin(__result);
            }
        }

        [HarmonyPatch(typeof(Bullet), nameof(Bullet.Die))]
        private static class Bullet_Die_PineVisualReset_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(Bullet __instance)
            {
                // Return the native visual before the bullet enters the pool.
                PineshooterBootstrap.RestoreProjectileSkin(__instance);
            }
        }

        [HarmonyPatch(typeof(PeaShooter), nameof(PeaShooter.Shoot1))]
        private static class PeaShooter_Shoot1_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static bool Prefix(
                PeaShooter __instance,
                ref Bullet __result
            )
            {
                if (!Pineshooter.IsPineshooter(__instance))
                    return true;

                try
                {
                    __result =
                        Pineshooter.CreateDirectShot(__instance)!;
                }
                catch (Exception exception)
                {
                    __result = null!;
                    Plugin.Logger.LogError(
                        "[Pineshooter] Direct Shoot1 failed: " +
                        exception
                    );
                }

                // The custom Peashooter prefab cannot deserialize the
                // native shoot reference. Its animation event still invokes
                // Shoot1, while this direct path creates a fully initialized
                // native Bullet_pea at the recovered Shoot transform.
                return false;
            }
        }

        [HarmonyPatch(
            typeof(Bullet_pea),
            nameof(Bullet_pea.HitZombie)
        )]
        private static class BulletPea_HitZombie_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(
                Bullet_pea __instance,
                Zombie zombie
            )
            {
                Pineshooter.HandleFirstHit(__instance, zombie);
            }
        }
    }
}
