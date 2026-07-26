using CustomizeLib.BepInEx;
using HarmonyLib;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class IcytronionBootstrap
    {
        private static bool registered;
        private static Sprite? biuSprite;
        private static bool firstAttackLogged;
        private static bool firstColdLogged;
        private static bool firstFreezeLogged;
        private static bool firstArcLogged;

        public static void OnStart()
        {
            if (registered)
                return;

            registered = true;

            try
            {
                RegisterPlant();
                InstallTypeFlags();
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Icytronion] Registration failed safely: " +
                    exception
                );
            }
        }

        public static void OnGameInit()
        {
            InstallTypeFlags();

            PlantType type =
                (PlantType)Plants.Icytronion.IcytronionID;

            if (!CustomCore.CustomPlants.ContainsKey(type))
                return;

            AlmanacCompatibility.RefreshLoadedData();
            ConfigureRegisteredPrefab();
        }

        private static void RegisterPlant()
        {
            AssetBundle? bundle = CustomCore.GetAssetBundle(
                Assembly.GetExecutingAssembly(),
                "PlantsPlus.Resources.AssetBundles.icytronion"
            );
            GameObject? prefab =
                bundle?.GetAsset<GameObject>("ElectricOnionPrefab");
            GameObject? preview =
                bundle?.GetAsset<GameObject>("ElectricOnionPreview");
            GameObject? electricVisual =
                bundle?.GetAsset<GameObject>("ElectricOnion");

            if (bundle == null || prefab == null || preview == null ||
                electricVisual == null)
            {
                throw new InvalidOperationException(
                    "Bundle, ElectricOnionPrefab or ElectricOnionPreview " +
                    "or the complete ElectricOnion visual prefab is missing."
                );
            }

            prefab.transform.localPosition = Vector3.zero;
            prefab.transform.localRotation = Quaternion.identity;

            V11PlantsBootstrap.IsolateAnimationClips(
                bundle,
                prefab,
                "Icytronion",
                "io_idle",
                "io_shoot",
                "io_electric"
            );

            // The board plant prefab intentionally contains only the plant.
            // Its complete electric visual, including biu/biu1/C1-C3, is
            // stored in the separate ElectricOnion prefab.
            biuSprite = FindBiuSprite(electricVisual);

            if (biuSprite == null)
            {
                Plugin.Logger.LogWarning(
                    "[Icytronion] ElectricOnion/biu has no sprite; " +
                    "attacks will still work without the arc visual."
                );
            }

            CustomCore.RegisterCustomPlant<
                Prismflower,
                Plants.Icytronion
            >(
                Plants.Icytronion.IcytronionID,
                prefab,
                preview,
                new List<(int, int)>
                {
                    (
                        (int)PlantType.ElectricOnion,
                        (int)PlantType.IceShroom
                    ),
                    (
                        (int)PlantType.IceShroom,
                        (int)PlantType.ElectricOnion
                    )
                },
                Plants.Icytronion.AttackInterval,
                0f,
                Plants.Icytronion.Damage,
                Plants.Icytronion.Toughness,
                Plants.Icytronion.CardRecharge,
                Plants.Icytronion.CardCost
            );

            AlmanacEntry almanac = AlmanacContent.Icytronion;
            CustomCore.AddPlantAlmanacStrings(
                (PlantType)Plants.Icytronion.IcytronionID,
                almanac.Name,
                almanac.Info,
                almanac.Introduce,
                Plants.Icytronion.CardCost
            );

            Plugin.Logger.LogInfo(
                "[Icytronion] Registered" +
                " | Plant ID = " +
                Plants.Icytronion.IcytronionID +
                " | Biu sprite = " +
                (biuSprite != null ? biuSprite.name : "missing") +
                " | Biu source = ElectricOnion/biu" +
                " | Cold = every hit" +
                " | Freeze chance = 25%"
            );
        }

        private static Sprite? FindBiuSprite(GameObject prefab)
        {
            SpriteRenderer[] renderers =
                prefab.GetComponentsInChildren<SpriteRenderer>(true);

            for (int index = 0; index < renderers.Length; index++)
            {
                SpriteRenderer renderer = renderers[index];

                if (renderer != null && renderer.sprite != null &&
                    renderer.gameObject.name.Equals(
                        "biu",
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    return renderer.sprite;
                }
            }

            return null;
        }

        private static void InstallTypeFlags()
        {
            PlantType type =
                (PlantType)Plants.Icytronion.IcytronionID;

            if (!CustomCore.TypeMgrExtra.IsIcePlant.Contains(type))
                CustomCore.TypeMgrExtra.IsIcePlant.Add(type);
        }

        internal static void ConfigureInstance(GameObject instance)
        {
            if (instance == null)
                return;

            try
            {
                Prismflower? plant =
                    instance.GetComponent<Prismflower>();

                if (plant == null)
                    return;

                if (plant.anim == null)
                {
                    plant.anim =
                        instance.GetComponentInChildren<Animator>(true);
                }

                if (plant.axis == null)
                    plant.axis = instance.transform;

                if (plant.board == null && Board.Instance != null)
                    plant.board = Board.Instance;

                if (plant.rb == null)
                    plant.rb = instance.GetComponent<Rigidbody2D>();

                // The custom bundle owns its animated biu sprite. The native
                // PrismLine is never injected.
                plant.prism = null;

                V11PlantsBootstrap.ApplyNativeShooterControllerWithLocalClips(
                    instance,
                    "Icytronion",
                    PlantType.ElectricOnion
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Icytronion] Runtime bridge deferred safely: " +
                    exception.Message
                );
            }
        }

        private static void ConfigureRegisteredPrefab()
        {
            try
            {
                PlantType type =
                    (PlantType)Plants.Icytronion.IcytronionID;

                if (GameAPP.resourcesManager == null ||
                    GameAPP.resourcesManager.plantPrefabs == null ||
                    !GameAPP.resourcesManager.plantPrefabs.ContainsKey(type))
                {
                    return;
                }

                GameObject? prefab =
                    GameAPP.resourcesManager.plantPrefabs[type];

                if (prefab == null)
                    return;

                ConfigureInstance(prefab);
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Icytronion] Prefab bridge deferred safely: " +
                    exception.Message
                );
            }
        }

        internal static bool BeginAttack(Prismflower source)
        {
            if (!IsUsable(source))
                return false;

            if (FindPrimaryTarget(source) == null)
                return false;

            try
            {
                Animator? animator = source.anim;

                if (animator == null)
                {
                    animator =
                        source.GetComponentInChildren<Animator>(true);
                }

                if (animator == null)
                    return false;

                animator.Play("Base Layer.shoot", 0, 0f);

                if (animator.layerCount > 1)
                {
                    animator.SetLayerWeight(1, 1f);
                    animator.Play("electric.electric", 1, 0f);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static int ExecuteAttack(Prismflower source)
        {
            if (!IsUsable(source))
                return 0;

            Zombie? primary = FindPrimaryTarget(source);

            if (primary == null)
                return 0;

            int supportCount = CountSupports(source);
            int primaryDamage =
                Plants.Icytronion.Damage +
                supportCount * Plants.Icytronion.SupportDamage;
            int hitCount = DamageTarget(
                source,
                primary,
                primaryDamage
            )
                ? 1
                : 0;

            if (hitCount == 0)
                return 0;

            SpawnBiu(source, primary);

            var candidates = new List<Zombie>();
            var distances = new List<float>();
            var zombies = Lawnf.GetAllZombies(false);
            Vector3 primaryPosition = primary.transform.position;

            for (int index = 0; index < zombies.Count; index++)
            {
                Zombie? zombie = zombies[index];

                if (zombie == null ||
                    zombie == primary ||
                    !zombie.Alive ||
                    zombie.theHealth <= 0 ||
                    zombie.isMindControlled ||
                    Math.Abs(
                        zombie.theZombieRow - primary.theZombieRow
                    ) > 1)
                {
                    continue;
                }

                float distance =
                    (zombie.transform.position -
                     primaryPosition).sqrMagnitude;

                if (distance >
                    Plants.Icytronion.ChainRadius *
                    Plants.Icytronion.ChainRadius)
                {
                    continue;
                }

                int insertAt = distances.Count;

                while (insertAt > 0 &&
                       distances[insertAt - 1] > distance)
                {
                    insertAt--;
                }

                distances.Insert(insertAt, distance);
                candidates.Insert(insertAt, zombie);
            }

            int chainDamage = Math.Max(1, primaryDamage / 2);
            int chainCount = Math.Min(
                Plants.Icytronion.MaximumChainTargets,
                candidates.Count
            );

            for (int index = 0; index < chainCount; index++)
            {
                Zombie target = candidates[index];

                if (!DamageTarget(source, target, chainDamage))
                    continue;

                SpawnBiu(primary, target);
                hitCount++;
            }

            if (!firstAttackLogged)
            {
                firstAttackLogged = true;
                Plugin.Logger.LogInfo(
                    "[Icytronion] Electronion network attack confirmed" +
                    " | Primary damage = " + primaryDamage +
                    " | Support plants = " + supportCount +
                    " | Chained zombies = " + (hitCount - 1) +
                    " | Cold on every hit = true" +
                    " | Freeze chance = 25%"
                );
            }

            return hitCount;
        }

        private static bool IsUsable(Prismflower source)
        {
            return source != null &&
                !source.dying &&
                !source.waitingDestory &&
                source.thePlantHealth > 0;
        }

        private static Zombie? FindPrimaryTarget(Prismflower source)
        {
            int sourceRow = source.thePlantRow;
            int sourceColumn = source.thePlantColumn;
            var zombies = Lawnf.GetAllZombies(false);
            Zombie? primary = null;
            float nearestDistance = float.MaxValue;

            for (int index = 0; index < zombies.Count; index++)
            {
                Zombie? zombie = zombies[index];

                if (zombie == null ||
                    !zombie.Alive ||
                    zombie.theHealth <= 0 ||
                    zombie.isMindControlled ||
                    Math.Abs(zombie.theZombieRow - sourceRow) > 2)
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

                if (Math.Abs(zombieColumn - sourceColumn) > 2)
                    continue;

                float distance =
                    (zombie.transform.position -
                     source.transform.position).sqrMagnitude;

                if (primary == null || distance < nearestDistance)
                {
                    primary = zombie;
                    nearestDistance = distance;
                }
            }

            return primary;
        }

        private static int CountSupports(Prismflower source)
        {
            int count = 0;
            int sourceRow = source.thePlantRow;
            int sourceColumn = source.thePlantColumn;
            var plants = Lawnf.GetAllPlants();

            for (int index = 0; index < plants.Count; index++)
            {
                Plant? support = plants[index];

                if (support == null ||
                    support == source ||
                    support.dying ||
                    support.waitingDestory ||
                    support.thePlantHealth <= 0 ||
                    (int)support.theStatus != 0)
                {
                    continue;
                }

                int type = (int)support.thePlantType;

                if (type != (int)PlantType.ElectricOnion &&
                    type != Plants.Icytronion.IcytronionID &&
                    type != Plants.Doomtronion.DoomtronionID)
                {
                    continue;
                }

                if (Math.Abs(
                        support.thePlantColumn - sourceColumn
                    ) > 2 ||
                    Math.Abs(
                        support.thePlantRow - sourceRow
                    ) > 2)
                {
                    continue;
                }

                count++;
                SpawnBiu(support, source);
            }

            return count;
        }

        private static bool DamageTarget(
            Prismflower source,
            Zombie zombie,
            int damage
        )
        {
            if (zombie == null ||
                !zombie.Alive ||
                zombie.theHealth <= 0 ||
                zombie.isMindControlled)
            {
                return false;
            }

            try
            {
                ((Entity)zombie).TakeDamage(
                    damage,
                    source.ToIDamageMaker(),
                    DamageType.Normal,
                    (PlantType)Plants.Icytronion.IcytronionID,
                    false
                );
            }
            catch
            {
                try
                {
                    zombie.ApplyDamage(DamageType.Normal, damage);
                }
                catch
                {
                    return false;
                }
            }

            ApplyFamilyColdIfConnected(zombie, source);
            return true;
        }

        internal static void ApplyFamilyColdIfConnected(
            Zombie zombie,
            Plant? source
        )
        {
            if (zombie == null || source == null)
                return;

            if ((int)source.thePlantType !=
                    Plants.Icytronion.IcytronionID &&
                !HasConnectedIcytronion(source))
            {
                return;
            }

            ApplyColdAndFreeze(zombie);
        }

        private static bool HasConnectedIcytronion(Plant source)
        {
            if (source == null)
                return false;

            int sourceRow = source.thePlantRow;
            int sourceColumn = source.thePlantColumn;
            var plants = Lawnf.GetAllPlants();

            for (int index = 0; index < plants.Count; index++)
            {
                Plant? candidate = plants[index];

                if (candidate == null ||
                    candidate.dying ||
                    candidate.waitingDestory ||
                    candidate.thePlantHealth <= 0 ||
                    (int)candidate.thePlantType !=
                        Plants.Icytronion.IcytronionID)
                {
                    continue;
                }

                if (Math.Abs(
                        candidate.thePlantColumn - sourceColumn
                    ) <= 2 &&
                    Math.Abs(
                        candidate.thePlantRow - sourceRow
                    ) <= 2)
                {
                    return true;
                }
            }

            return false;
        }

        internal static void HandleNativeAmpnionDamage(
            Zombie zombie,
            IDamageMaker damageFrom
        )
        {
            if (zombie == null || damageFrom == null)
                return;

            Plant? source = null;

            try
            {
                if (damageFrom.IsBullet(out Bullet bullet) &&
                    bullet != null)
                {
                    source = bullet.from;
                }
                else if (damageFrom.IsPlant(out Plant plant) &&
                         plant != null)
                {
                    source = plant;
                }
            }
            catch
            {
                return;
            }

            if (source == null ||
                source.thePlantType != PlantType.ElectricOnion)
            {
                return;
            }

            ApplyFamilyColdIfConnected(zombie, source);
        }

        private static void ApplyColdAndFreeze(Zombie zombie)
        {
            if (zombie == null || !zombie.Alive ||
                zombie.theHealth <= 0)
            {
                return;
            }

            try
            {
                zombie.SetCold(Plants.Icytronion.ColdDuration);

                if (!firstColdLogged)
                {
                    firstColdLogged = true;
                    Plugin.Logger.LogInfo(
                        "[Icytronion] Cold applied" +
                        " | Duration = " +
                        Plants.Icytronion.ColdDuration + "s"
                    );
                }
            }
            catch
            {
                // Damage and the freeze roll remain valid for special zombies
                // which reject the ordinary cold status.
            }

            if (UnityEngine.Random.value >=
                Plants.Icytronion.FreezeChance)
            {
                return;
            }

            try
            {
                zombie.SetFreeze(
                    Plants.Icytronion.FreezeDuration
                );

                if (!firstFreezeLogged)
                {
                    firstFreezeLogged = true;
                    Plugin.Logger.LogInfo(
                        "[Icytronion] Freeze roll succeeded" +
                        " | Chance = 25%" +
                        " | Duration = " +
                        Plants.Icytronion.FreezeDuration + "s"
                    );
                }
            }
            catch
            {
                // Ice-resistant zombies may reject freeze.
            }
        }

        private static Transform? FindChild(
            Transform root,
            string wanted
        )
        {
            if (root == null)
                return null;

            if (root.name.Equals(
                wanted,
                StringComparison.OrdinalIgnoreCase
            ))
            {
                return root;
            }

            for (int index = 0; index < root.childCount; index++)
            {
                Transform? found = FindChild(
                    root.GetChild(index),
                    wanted
                );

                if (found != null)
                    return found;
            }

            return null;
        }

        private static Vector3 PlantAnchor(Plant plant)
        {
            Transform? anchor = FindChild(plant.transform, "Ball1");

            return anchor != null
                ? anchor.position
                : plant.transform.position +
                    new Vector3(0.05f, 1.05f, -0.05f);
        }

        private static Vector3 ZombieAnchor(Zombie zombie)
        {
            try
            {
                return zombie.col != null
                    ? zombie.col.bounds.center
                    : zombie.transform.position +
                        new Vector3(0f, 0.75f, 0f);
            }
            catch
            {
                return zombie.transform.position +
                    new Vector3(0f, 0.75f, 0f);
            }
        }

        private static void SpawnBiu(Plant source, Prismflower target)
        {
            SpawnBiu(PlantAnchor(source), PlantAnchor(target));
        }

        private static void SpawnBiu(Prismflower source, Zombie target)
        {
            SpawnBiu(PlantAnchor(source), ZombieAnchor(target));
        }

        private static void SpawnBiu(Zombie source, Zombie target)
        {
            SpawnBiu(ZombieAnchor(source), ZombieAnchor(target));
        }

        private static void SpawnBiu(Vector3 start, Vector3 end)
        {
            Sprite? sprite = biuSprite;

            if (sprite == null)
                return;

            start.z = -0.05f;
            end.z = -0.05f;
            Vector3 delta = end - start;
            float distance = delta.magnitude;

            if (distance <= 0.01f)
                return;

            GameObject visual =
                new GameObject("[Plants+] Icytronion biu");
            visual.transform.position = (start + end) * 0.5f;
            visual.transform.rotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f
            );

            float nativeLength = Mathf.Max(
                sprite.bounds.size.y,
                0.01f
            );
            visual.transform.localScale = new Vector3(
                0.15f,
                distance / nativeLength,
                1f
            );

            SpriteRenderer renderer =
                visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = 30;

            UnityEngine.Object.Destroy(visual, 0.10f);

            if (!firstArcLogged)
            {
                firstArcLogged = true;
                Plugin.Logger.LogInfo(
                    "[Icytronion] Biu arc created" +
                    " | Lifetime = 0.10s" +
                    " | Distance = " + distance
                );
            }
        }
    }
}

namespace PlantsPlus.Plants
{
    using PlantsPlus.Core;

    public sealed class Icytronion : MonoBehaviour
    {
        public const int IcytronionID = 6019;
        public const int Damage = 100;
        public const float AttackInterval = 1.5f;
        public const int Toughness = 300;
        public const float CardRecharge = 15f;
        public const int CardCost = 300;
        public const int SupportDamage = 150;
        public const int MaximumChainTargets = 3;
        public const float ChainRadius = 1.6f;
        public const float ColdDuration = 10f;
        public const float FreezeChance = 0.25f;
        public const float FreezeDuration = 3f;

        private Prismflower? plant;
        private float attackTimer = 0.25f;
        private bool attackPending;

        public Icytronion(IntPtr pointer) : base(pointer) { }

        public void Start()
        {
            IcytronionBootstrap.ConfigureInstance(gameObject);
            plant = gameObject.GetComponent<Prismflower>();
            attackTimer = 0.25f;
            attackPending = false;

            Plugin.Logger.LogInfo(
                "[Icytronion] Ready" +
                " | Attack = Electronion network + capped chain" +
                " | Damage = " + Damage + "/" + AttackInterval + "s" +
                " | Support = +" + SupportDamage + " each" +
                " | Chain = " + MaximumChainTargets + " targets at 50%" +
                " | Cold = " + ColdDuration + "s on every hit" +
                " | Freeze = 25% for " + FreezeDuration + "s"
            );
        }

        public void Update()
        {
            float delta = Time.deltaTime;

            if (delta <= 0f)
                return;

            if (plant == null)
                plant = gameObject.GetComponent<Prismflower>();

            if (plant == null ||
                plant.dying ||
                plant.waitingDestory ||
                plant.thePlantHealth <= 0)
            {
                return;
            }

            attackTimer -= delta;

            if (attackTimer > 0f || attackPending)
                return;

            attackPending =
                IcytronionBootstrap.BeginAttack(plant);
            attackTimer = attackPending
                ? AttackInterval
                : 0.20f;
        }

        public void ResolveAnimationImpact()
        {
            if (!attackPending)
                return;

            attackPending = false;

            if (plant == null)
                plant = gameObject.GetComponent<Prismflower>();

            if (plant != null)
                IcytronionBootstrap.ExecuteAttack(plant);
        }
    }

    [HarmonyPatch]
    internal static class IcytronionPatches
    {
        [HarmonyPatch(typeof(Zombie), nameof(Zombie.TakeDamage))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void ZombieTakeDamagePostfix(
            Zombie __instance,
            IDamageMaker damageFrom
        )
        {
            IcytronionBootstrap.HandleNativeAmpnionDamage(
                __instance,
                damageFrom
            );
        }

        [HarmonyPatch(
            typeof(Prismflower),
            nameof(Prismflower.SearchZombieUpdate)
        )]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool SearchZombieUpdatePrefix(
            Prismflower __instance
        )
        {
            return __instance == null ||
                (int)__instance.thePlantType !=
                    Icytronion.IcytronionID;
        }

        [HarmonyPatch(
            typeof(Prismflower),
            nameof(Prismflower.AnimShoot)
        )]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool AnimShootPrefix(Prismflower __instance)
        {
            if (__instance == null ||
                (int)__instance.thePlantType !=
                    Icytronion.IcytronionID)
            {
                return true;
            }

            Icytronion? behaviour =
                __instance.gameObject.GetComponent<Icytronion>();

            if (behaviour != null)
                behaviour.ResolveAnimationImpact();

            return false;
        }

        [HarmonyPatch(typeof(Lawnf), nameof(Lawnf.CheckPlantClass))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void CheckPlantClassPostfix(
            PlantType thePlantType,
            ref int __result
        )
        {
            if ((int)thePlantType == Icytronion.IcytronionID)
            {
                __result =
                    Lawnf.CheckPlantClass(PlantType.ElectricOnion);
            }
        }
    }
}
