using CustomizeLib.MelonLoader;
using HarmonyLib;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class SakuraSharpshooterBootstrap
    {
        private const float ProjectileVisualLocalY = 0.70f;
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
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Sakura Sharpshooter] Registration failed safely: " +
                    exception
                );
            }
        }

        public static void OnGameInit()
        {
            PlantType type =
                (PlantType)Plants.SakuraSharpshooter.SakuraSharpshooterID;

            if (!CustomCore.CustomPlants.ContainsKey(type))
                return;

            AlmanacCompatibility.RefreshLoadedData();
            ConfigureRegisteredPrefab();
        }

        private static void LoadProjectileSkin()
        {
            AssetBundle? bundle = CustomCore.GetAssetBundle(
                Assembly.GetExecutingAssembly(),
                "PlantsPlus.Resources.AssetBundles.bullet_sakurasharpshooter"
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
                    "Sakura Sharpshooter projectile sprite is missing."
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
                // Bullet_spruce itself is valid; only the Sakura bundle's
                // renderer Y is authored at lawn height. Keep its custom X/Z
                // offsets while lifting the visible thorn to the same muzzle
                // height already used by Sea/Solar Sharpshooter.
                renderer.transform.localPosition = new Vector3(
                    projectileLocalPosition.x,
                    ProjectileVisualLocalY,
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
                "PlantsPlus.Resources.AssetBundles.sakurasharpshooter"
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

            // The Sakura bundle ships its own two clips. Cache them before
            // CustomizeLib clones the prefab so Unity cannot resolve similarly
            // named clips from another already-loaded Sharpshooter bundle.
            V11PlantsBootstrap.IsolateAnimationClips(
                bundle,
                prefab,
                "Sakura Sharpshooter",
                "sakura_idle",
                "sakura_shoot"
            );

            CustomCore.RegisterCustomPlant<
                SpruceShooter,
                Plants.SakuraSharpshooter
            >(
                Plants.SakuraSharpshooter.SakuraSharpshooterID,
                prefab,
                preview,
                new List<(int, int)>
                {
                    (
                        (int)PlantType.CherryBomb,
                        (int)PlantType.SpruceShooter
                    ),
                    (
                        (int)PlantType.SpruceShooter,
                        (int)PlantType.CherryBomb
                    )
                },
                Plants.SakuraSharpshooter.AttackInterval,
                0f,
                Plants.SakuraSharpshooter.Damage,
                Plants.SakuraSharpshooter.Toughness,
                Plants.SakuraSharpshooter.CardRecharge,
                Plants.SakuraSharpshooter.CardCost
            );

            AlmanacEntry almanac = AlmanacContent.SakuraSharpshooter;
            CustomCore.AddPlantAlmanacStrings(
                (PlantType)Plants.SakuraSharpshooter.SakuraSharpshooterID,
                almanac.Name,
                almanac.Info,
                almanac.Introduce,
                Plants.SakuraSharpshooter.CardCost
            );

            Plugin.Logger.LogInfo(
                "[Sakura Sharpshooter] Registered" +
                " | Plant ID = " +
                Plants.SakuraSharpshooter.SakuraSharpshooterID +
                " | Recipe = Cherry Bomb + Spruce Sharpshooter" +
                " | Projectile explosion chance = 15%" +
                " | Explode-o splash synergy = 50%" +
                " | Secondary chain damage = 50%"
            );
        }

        private static void ConfigureRegisteredPrefab()
        {
            PlantType type =
                (PlantType)Plants.SakuraSharpshooter.SakuraSharpshooterID;

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

            V11PlantsBootstrap.EnsureShooterRuntimeReferences(
                shooter,
                "Sakura Sharpshooter"
            );
            V11PlantsBootstrap.ApplyNativeShooterControllerWithLocalClips(
                prefab,
                "Sakura Sharpshooter",
                PlantType.SpruceShooter
            );
        }
    }
}

namespace PlantsPlus.Plants
{
    using PlantsPlus.Core;

    public sealed class SakuraProjectileMarker : MonoBehaviour
    {
        public bool ExplodeOnImpact;
        public bool Consumed;

        public SakuraProjectileMarker(IntPtr pointer) : base(pointer) { }
    }

    public sealed class SakuraSharpshooter : MonoBehaviour
    {
        public const int SakuraSharpshooterID = 6023;

        // Keep the ordinary Spruce-style shot intentionally modest; the plant's
        // power budget lives in the two explosion mechanics below.
        public const int Damage = 30;
        public const int Toughness = 300;
        public const int CardCost = 350;
        public const float CardRecharge = 15f;
        public const float AttackInterval = 1.5f;

        public const float ProjectileExplosionChance = 0.15f;
        public const float ExplodeOSplashChainChance = 0.50f;
        public const int FallbackExplodeODamage = 300;
        public const float SecondaryExplosionMultiplier = 0.50f;

        private static bool firstShotLogged;
        private static bool firstProjectileExplosionLogged;
        private static bool firstSynergyLogged;
        private static bool explosionWarningLogged;

        public SakuraSharpshooter(IntPtr pointer) : base(pointer) { }

        public void Start()
        {
            V11PlantsBootstrap.ApplyNativeShooterControllerWithLocalClips(
                gameObject,
                "Sakura Sharpshooter",
                PlantType.SpruceShooter
            );

            SpruceShooter? shooter = gameObject.GetComponent<SpruceShooter>();

            if (shooter != null)
            {
                V11PlantsBootstrap.EnsureShooterRuntimeReferences(
                    shooter,
                    "Sakura Sharpshooter"
                );
            }

            Plugin.Logger.LogInfo(
                "[Sakura Sharpshooter] Ready" +
                " | Thorn damage = " + Damage +
                " | Cherry proc = 15%" +
                " | Explode-o adjacent proc = 50%" +
                " | Random 3x3 explosion = half damage"
            );
        }

        internal static bool IsSakuraPlant(Plant? plant)
        {
            return plant != null &&
                (int)plant.thePlantType == SakuraSharpshooterID;
        }

        internal static void ConfigureNativeShot(
            SpruceShooter source,
            Bullet bullet
        )
        {
            if (source == null || bullet == null)
                return;

            bullet.from = source;
            bullet.fromType = (PlantType)SakuraSharpshooterID;
            bullet.Damage = source.attackDamage > 0
                ? source.attackDamage
                : Damage;

            SakuraSharpshooterBootstrap.ApplyProjectileSkin(bullet);

            SakuraProjectileMarker? marker =
                bullet.gameObject.GetComponent<SakuraProjectileMarker>();

            if (marker == null)
                marker = bullet.gameObject.AddComponent<SakuraProjectileMarker>();

            marker.ExplodeOnImpact =
                UnityEngine.Random.value < ProjectileExplosionChance;
            marker.Consumed = false;

            if (!firstShotLogged)
            {
                firstShotLogged = true;
                Plugin.Logger.LogInfo(
                    "[Sakura Sharpshooter] First shot verified" +
                    " | Projectile = native Bullet_spruce + Sakura sprite" +
                    " | Damage = " + bullet.Damage +
                    " | 15% roll is per projectile" +
                    " | Visual Y = 0.70"
                );
            }
        }

        internal static int GetExplodeODamage()
        {
            try
            {
                PlantDataManager.PlantData data =
                    PlantDataManager.GetPlantData(
                        PlantType.SuperCherryShooter
                    );

                if (data != null && data.attackDamage > 0)
                    return data.attackDamage;
            }
            catch
            {
                // Fallback below matches the current native Explode-o-shooter
                // damage used elsewhere in Plants+.
            }

            return FallbackExplodeODamage;
        }

        internal static void TriggerProjectileExplosion(Zombie? zombie)
        {
            if (zombie == null)
                return;

            int damage = GetExplodeODamage();
            Vector3 world = zombie.transform.position;

            CreateCherryExplosion(
                new Vector2(world.x, world.y),
                zombie.theZombieRow,
                damage,
                CherryBombType.Normal
            );

            if (!firstProjectileExplosionLogged)
            {
                firstProjectileExplosionLogged = true;
                Plugin.Logger.LogInfo(
                    "[Sakura Sharpshooter] 15% Cherry proc verified" +
                    " | Damage = " + damage +
                    " | Visual = Cherry Bomb"
                );
            }
        }

        internal readonly struct ChainExplosionPlan
        {
            public ChainExplosionPlan(
                Vector2 primaryPosition,
                int primaryRow,
                int primaryDamage,
                bool hasSecondary,
                Vector2 secondaryPosition,
                int secondaryRow,
                int secondaryDamage
            )
            {
                PrimaryPosition = primaryPosition;
                PrimaryRow = primaryRow;
                PrimaryDamage = primaryDamage;
                HasSecondary = hasSecondary;
                SecondaryPosition = secondaryPosition;
                SecondaryRow = secondaryRow;
                SecondaryDamage = secondaryDamage;
            }

            public Vector2 PrimaryPosition { get; }
            public int PrimaryRow { get; }
            public int PrimaryDamage { get; }
            public bool HasSecondary { get; }
            public Vector2 SecondaryPosition { get; }
            public int SecondaryRow { get; }
            public int SecondaryDamage { get; }
        }

        internal static bool TryBuildExplodeOShooterChain(
            Zombie? hitZombie,
            out ChainExplosionPlan plan
        )
        {
            plan = default;

            if (hitZombie == null || hitZombie.isMindControlled)
                return false;

            if (!HasAdjacentSakura(hitZombie))
                return false;

            if (UnityEngine.Random.value >= ExplodeOSplashChainChance)
                return false;

            int fullDamage = GetExplodeODamage();
            int halfDamage = Mathf.Max(
                1,
                Mathf.RoundToInt(
                    fullDamage * SecondaryExplosionMultiplier
                )
            );

            // Snapshot the secondary target before any chain explosion can
            // change the local zombie cluster.
            Zombie? secondary = PickRandomZombieIn3x3(hitZombie);
            Vector2 secondaryPosition = default;
            int secondaryRow = 0;
            bool hasSecondary = secondary != null;

            if (secondary != null)
            {
                Vector3 secondaryWorld = secondary.transform.position;
                secondaryPosition = new Vector2(
                    secondaryWorld.x,
                    secondaryWorld.y
                );
                secondaryRow = secondary.theZombieRow;
            }

            Vector3 primaryWorld = hitZombie.transform.position;
            plan = new ChainExplosionPlan(
                new Vector2(primaryWorld.x, primaryWorld.y),
                hitZombie.theZombieRow,
                fullDamage,
                hasSecondary,
                secondaryPosition,
                secondaryRow,
                halfDamage
            );
            return true;
        }

        internal static void ExecuteExplodeOShooterChain(
            ChainExplosionPlan plan
        )
        {
            CreateCherryExplosion(
                plan.PrimaryPosition,
                plan.PrimaryRow,
                plan.PrimaryDamage,
                CherryBombType.Bullet
            );

            if (plan.HasSecondary)
            {
                CreateCherryExplosion(
                    plan.SecondaryPosition,
                    plan.SecondaryRow,
                    plan.SecondaryDamage,
                    CherryBombType.Bullet
                );
            }

            if (!firstSynergyLogged)
            {
                firstSynergyLogged = true;
                Plugin.Logger.LogInfo(
                    "[Sakura Sharpshooter] Explode-o synergy verified" +
                    " | Adjacent splash proc = 50%" +
                    " | Primary chain damage = " + plan.PrimaryDamage +
                    " | Random 3x3 damage = " + plan.SecondaryDamage +
                    " | Secondary target = " +
                    (plan.HasSecondary ? "found" : "none")
                );
            }
        }

        private static bool HasAdjacentSakura(Zombie zombie)
        {
            int zombieColumn;

            try
            {
                zombieColumn = Lawnf.GetColumnFromX(
                    zombie.transform.position.x
                );
            }
            catch
            {
                return false;
            }

            var plants = Lawnf.GetAllPlants();

            if (plants == null)
                return false;

            for (int index = 0; index < plants.Count; index++)
            {
                Plant? plant = plants[index];

                if (!IsSakuraPlant(plant) || plant!.dying ||
                    plant.waitingDestory || plant.thePlantHealth <= 0)
                {
                    continue;
                }

                if (Math.Abs(plant.thePlantRow - zombie.theZombieRow) <= 1 &&
                    Math.Abs(plant.thePlantColumn - zombieColumn) <= 1)
                {
                    return true;
                }
            }

            return false;
        }

        private static Zombie? PickRandomZombieIn3x3(Zombie center)
        {
            int centerColumn;

            try
            {
                centerColumn = Lawnf.GetColumnFromX(
                    center.transform.position.x
                );
            }
            catch
            {
                return null;
            }

            var zombies = Lawnf.GetAllZombies(false);

            if (zombies == null)
                return null;

            List<Zombie> candidates = new List<Zombie>();
            int centerID = center.GetInstanceID();

            for (int index = 0; index < zombies.Count; index++)
            {
                Zombie? zombie = zombies[index];

                if (zombie == null ||
                    zombie.GetInstanceID() == centerID ||
                    !zombie.Alive ||
                    zombie.theHealth <= 0 ||
                    zombie.isMindControlled ||
                    Math.Abs(zombie.theZombieRow - center.theZombieRow) > 1)
                {
                    continue;
                }

                int zombieColumn;

                try
                {
                    zombieColumn = Lawnf.GetColumnFromX(
                        zombie.transform.position.x
                    );
                }
                catch
                {
                    continue;
                }

                if (Math.Abs(zombieColumn - centerColumn) <= 1)
                    candidates.Add(zombie);
            }

            if (candidates.Count == 0)
                return null;

            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        private static void CreateCherryExplosion(
            Vector2 position,
            int row,
            int damage,
            CherryBombType bombType
        )
        {
            try
            {
                Board? board = Board.Instance;
                BoardAction? action = board != null ? board.boardAction : null;

                if (action == null)
                    return;

                action.CreateCherryExplode(
                    position,
                    row,
                    bombType,
                    damage,
                    (PlantType)SakuraSharpshooterID,
                    null,
                    true
                );
            }
            catch (Exception exception)
            {
                if (explosionWarningLogged)
                    return;

                explosionWarningLogged = true;
                Plugin.Logger.LogWarning(
                    "[Sakura Sharpshooter] Cherry explosion failed safely: " +
                    exception.Message
                );
            }
        }
    }

    internal static class SakuraSharpshooterPatches
    {
        [ThreadStatic]
        private static int activeBombSourceID;

        [ThreadStatic]
        private static HashSet<int>? activeProcessedZombies;

        [ThreadStatic]
        private static List<SakuraSharpshooter.ChainExplosionPlan>?
            activePendingChains;

        private readonly struct BombContextState
        {
            public BombContextState(
                int previousSourceID,
                HashSet<int>? previousProcessedZombies,
                List<SakuraSharpshooter.ChainExplosionPlan>?
                    previousPendingChains
            )
            {
                PreviousSourceID = previousSourceID;
                PreviousProcessedZombies = previousProcessedZombies;
                PreviousPendingChains = previousPendingChains;
            }

            public int PreviousSourceID { get; }
            public HashSet<int>? PreviousProcessedZombies { get; }
            public List<SakuraSharpshooter.ChainExplosionPlan>?
                PreviousPendingChains { get; }
        }

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
                if (!SakuraSharpshooter.IsSakuraPlant(__instance) ||
                    __result == null)
                {
                    return;
                }

                SakuraSharpshooter.ConfigureNativeShot(
                    __instance,
                    __result
                );
            }
        }

        [HarmonyPatch(typeof(Bullet_spruce), nameof(Bullet_spruce.HitZombie))]
        private static class BulletSpruce_HitZombie_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(
                Bullet_spruce __instance,
                out bool __state
            )
            {
                __state = false;

                if (__instance == null)
                    return;

                SakuraProjectileMarker? marker =
                    __instance.gameObject.GetComponent<SakuraProjectileMarker>();

                if (marker == null ||
                    !marker.ExplodeOnImpact || marker.Consumed)
                {
                    return;
                }

                marker.Consumed = true;
                __state = true;
            }

            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(Zombie zombie, bool __state)
            {
                if (__state)
                    SakuraSharpshooter.TriggerProjectileExplosion(zombie);
            }
        }

        // Track the source type for every native cherry explosion. A nested
        // Sakura-created explosion temporarily replaces this context, then the
        // finalizer restores the outer Explode-o-shooter context correctly.
        [HarmonyPatch(
            typeof(BombCherry),
            nameof(BombCherry.Explode),
            new Type[] { typeof(IDamageMaker) }
        )]
        private static class BombCherry_Explode_Source_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(
                BombCherry __instance,
                out BombContextState __state
            )
            {
                __state = new BombContextState(
                    activeBombSourceID,
                    activeProcessedZombies,
                    activePendingChains
                );

                activeBombSourceID = __instance != null
                    ? (int)__instance.fromType
                    : -1;

                bool explodeOShooter =
                    activeBombSourceID == (int)PlantType.SuperCherryShooter;

                activeProcessedZombies = explodeOShooter
                    ? new HashSet<int>()
                    : null;
                activePendingChains = explodeOShooter
                    ? new List<SakuraSharpshooter.ChainExplosionPlan>()
                    : null;
            }

            [HarmonyFinalizer]
            [HarmonyPriority(Priority.Last)]
            private static Exception? Finalizer(
                Exception? __exception,
                BombContextState __state
            )
            {
                bool completedExplodeO =
                    activeBombSourceID ==
                    (int)PlantType.SuperCherryShooter;
                List<SakuraSharpshooter.ChainExplosionPlan>? completed =
                    completedExplodeO ? activePendingChains : null;

                activeBombSourceID = __state.PreviousSourceID;
                activeProcessedZombies =
                    __state.PreviousProcessedZombies;
                activePendingChains = __state.PreviousPendingChains;

                // Execute only after the native explosion has finished
                // iterating its victims. This avoids re-entering BombCherry's
                // damage loop from inside Zombie.TakeDamage.
                if (__exception == null && completed != null)
                {
                    for (int index = 0; index < completed.Count; index++)
                    {
                        SakuraSharpshooter.ExecuteExplodeOShooterChain(
                            completed[index]
                        );
                    }
                }

                return __exception;
            }
        }

        [HarmonyPatch(typeof(Zombie), nameof(Zombie.TakeDamage))]
        private static class Zombie_TakeDamage_ExplodeOSynergy_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(Zombie __instance)
            {
                if (activeBombSourceID !=
                    (int)PlantType.SuperCherryShooter ||
                    __instance == null)
                {
                    return;
                }

                int instanceID = __instance.GetInstanceID();

                if (activeProcessedZombies != null &&
                    !activeProcessedZombies.Add(instanceID))
                {
                    return;
                }

                if (SakuraSharpshooter.TryBuildExplodeOShooterChain(
                        __instance,
                        out SakuraSharpshooter.ChainExplosionPlan plan
                    ))
                {
                    activePendingChains?.Add(plan);
                }
            }
        }
    }
}
