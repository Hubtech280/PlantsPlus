using System;
using System.Collections.Generic;
using System.Reflection;
using CustomizeLib.MelonLoader;
using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class SparkShroomBootstrap
    {
        private static bool registered;
        private static bool treeLogged;

        internal static void OnStart()
        {
            if (registered)
                return;

            try
            {
                AssetBundle? bundle = CustomCore.GetAssetBundle(
                    Assembly.GetExecutingAssembly(),
                    "PlantsPlus.Resources.AssetBundles.spark_shroom"
                );
                GameObject? prefab =
                    bundle?.GetAsset<GameObject>("CaltropPrefab");
                GameObject? preview =
                    bundle?.GetAsset<GameObject>("CaltropPreview");

                if (bundle == null || prefab == null || preview == null)
                {
                    throw new InvalidOperationException(
                        "Spark-shroom prefab or preview missing."
                    );
                }

                prefab.transform.localPosition = Vector3.zero;
                V11PlantsBootstrap.IsolateAnimationClips(
                    bundle,
                    prefab,
                    "Spark-shroom",
                    "idle",
                    "attack",
                    "rest",
                    "rest-idle",
                    "wake-up",
                    "electric"
                );

                CustomCore.RegisterCustomPlant<Caltrop, Plants.SparkShroom>(
                    Plants.SparkShroom.ID,
                    prefab,
                    preview,
                    new List<(int, int)>
                    {
                        (
                            (int)PlantType.ElectricOnion,
                            Plants.BounceShroom.ID
                        ),
                        (
                            Plants.BounceShroom.ID,
                            (int)PlantType.ElectricOnion
                        )
                    },
                    0f,
                    0f,
                    Plants.SparkShroom.LightningDamage,
                    Plants.SparkShroom.Toughness,
                    15f,
                    75
                );

                InstallTypeFlags();
                AlmanacEntry entry = AlmanacContent.SparkShroom;
                CustomCore.AddPlantAlmanacStrings(
                    (PlantType)Plants.SparkShroom.ID,
                    entry.Name,
                    entry.Info,
                    entry.Introduce,
                    75
                );

                registered = true;
                Plugin.Logger.LogInfo(
                    "[Spark-shroom] Registered | ID=6037 | " +
                    "Recipe=Electronion + Bounce-shroom | " +
                    "Bounce lightning excludes its source zombie"
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Spark-shroom] Registration failed: " + exception
                );
            }
        }

        internal static void OnGameInit()
        {
            InstallTypeFlags();
            EnsureNativeFusionRecipe();
            AlmanacCompatibility.RefreshLoadedData();
        }

        internal static void EnsureNativeFusionRecipe()
        {
            try
            {
                PlantType electronion = PlantType.ElectricOnion;
                PlantType bounce = (PlantType)Plants.BounceShroom.ID;
                PlantType spark = (PlantType)Plants.SparkShroom.ID;
                PlantType existing;

                if (!MixData.TryGetMix(
                        electronion,
                        bounce,
                        out existing,
                        true
                    ) || existing != spark)
                {
                    MixData.AddOrderedRecipe(electronion, bounce, spark);
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Spark-shroom] Native fusion ancestry repair " +
                    "failed safely: " + exception.Message
                );
            }
        }

        internal static void EnsureNativeMixTreeRecipe()
        {
            try
            {
                if (!PlantMixTreeManager.IsInitialized)
                    return;

                PlantType electronion = PlantType.ElectricOnion;
                PlantType bounce = (PlantType)Plants.BounceShroom.ID;
                PlantType spark = (PlantType)Plants.SparkShroom.ID;
                var trees = PlantMixTreeManager.PlantMixTrees;
                var parentsByChild = PlantMixTreeManager.ChildToParents;
                if (trees == null || parentsByChild == null)
                    return;

                PlantMixTreeNode electronionNode = GetOrAddNode(
                    trees,
                    electronion
                );
                PlantMixTreeNode bounceNode = GetOrAddNode(trees, bounce);
                PlantMixTreeNode sparkNode = GetOrAddNode(trees, spark);

                electronionNode.Recipes[bounce] = spark;
                bounceNode.Recipes[electronion] = spark;
                AddNodeOnce(electronionNode.DirectChildren, sparkNode);
                AddNodeOnce(bounceNode.DirectChildren, sparkNode);
                AddNodeOnce(electronionNode.AllDescendants, sparkNode);
                AddNodeOnce(bounceNode.AllDescendants, sparkNode);

                Il2CppSystem.Collections.Generic.List<
                    PlantMixTreeManager.MixParentInfo> parents;
                if (!parentsByChild.TryGetValue(spark, out parents))
                {
                    parents = new Il2CppSystem.Collections.Generic.List<
                        PlantMixTreeManager.MixParentInfo>();
                    parentsByChild.Add(spark, parents);
                }

                bool found = false;
                for (int index = 0; index < parents.Count; index++)
                {
                    PlantMixTreeManager.MixParentInfo parent = parents[index];
                    bool normal = parent.ParentA == electronion &&
                        parent.ParentB == bounce;
                    bool reverse = parent.ParentA == bounce &&
                        parent.ParentB == electronion;
                    if (parent.Result == spark && (normal || reverse))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    parents.Add(new PlantMixTreeManager.MixParentInfo(
                        electronion,
                        bounce,
                        spark
                    ));
                }

                if (!treeLogged)
                {
                    treeLogged = true;
                    Plugin.Logger.LogInfo(
                        "[Spark-shroom] Native fusion tree installed | " +
                        "Electronion + Bounce-shroom -> Spark-shroom"
                    );
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Spark-shroom] Native fusion tree repair failed " +
                    "safely: " + exception.Message
                );
            }
        }

        private static PlantMixTreeNode GetOrAddNode(
            Il2CppSystem.Collections.Generic.Dictionary<
                PlantType,
                PlantMixTreeNode> trees,
            PlantType type)
        {
            PlantMixTreeNode node;
            if (!trees.TryGetValue(type, out node))
            {
                node = new PlantMixTreeNode(type);
                trees.Add(type, node);
            }
            return node;
        }

        private static void AddNodeOnce(
            Il2CppSystem.Collections.Generic.List<PlantMixTreeNode> nodes,
            PlantMixTreeNode node)
        {
            for (int index = 0; index < nodes.Count; index++)
            {
                if (nodes[index] != null &&
                    nodes[index].PlantType == node.PlantType)
                {
                    return;
                }
            }
            nodes.Add(node);
        }

        private static void InstallTypeFlags()
        {
            PlantType type = (PlantType)Plants.SparkShroom.ID;
            if (CustomCore.TypeMgrExtra.IsCaltrop != null &&
                !CustomCore.TypeMgrExtra.IsCaltrop.Contains(type))
            {
                CustomCore.TypeMgrExtra.IsCaltrop.Add(type);
            }
            CoreEnums.baiscPlants?.Remove(type);
            TypeData.SpecialCardPlants?.Remove(type);
        }
    }

    [HarmonyPatch(typeof(MixData), nameof(MixData.InitMixData))]
    internal static class SparkShroomMixDataPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix()
        {
            SparkShroomBootstrap.EnsureNativeFusionRecipe();
        }
    }

    [HarmonyPatch(typeof(PlantMixTreeManager), "Init")]
    internal static class SparkShroomMixTreePatch
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            SparkShroomBootstrap.EnsureNativeFusionRecipe();
        }

    }

    [HarmonyPatch(typeof(Caltrop), nameof(Caltrop.AnimAttack))]
    internal static class SparkShroomAttackPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Caltrop __instance)
        {
            if ((int)__instance.thePlantType != Plants.SparkShroom.ID)
                return true;
            __instance.GetComponent<Plants.SparkShroom>()?.AnimAttack();
            return false;
        }
    }

    [HarmonyPatch(typeof(Caltrop), nameof(Caltrop.Update))]
    internal static class SparkShroomUpdatePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Caltrop __instance)
        {
            return (int)__instance.thePlantType != Plants.SparkShroom.ID;
        }
    }

    [HarmonyPatch(typeof(Caltrop), nameof(Caltrop.KillCar))]
    internal static class SparkShroomCarPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Caltrop __instance)
        {
            return (int)__instance.thePlantType != Plants.SparkShroom.ID;
        }
    }

    [HarmonyPatch(typeof(BungiZombie), nameof(BungiZombie.Stealable))]
    internal static class SparkShroomBungeePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Plant plant, ref bool __result)
        {
            if (plant == null ||
                (int)plant.thePlantType != Plants.SparkShroom.ID)
            {
                return true;
            }
            __result = false;
            return false;
        }
    }
}

namespace PlantsPlus.Plants
{
    public sealed class SparkShroom : MonoBehaviour
    {
        public const int ID = 6037;
        public const int LightningDamage = 100;
        public const int Toughness = 300;
        private const float BounceDistanceInTiles = 1.5f;
        private const float BounceFlightSeconds = 0.55f;
        private const float RestAnimationSeconds = 0.8333333f;
        private const float RestSeconds = 5f;
        private const float WakeAnimationSeconds = 0.8333333f;
        private const int ElectronionRangeInColumns = 2;
        private const int ElectronionRangeInRows = 2;

        private Caltrop? plant;
        private Animator? animator;
        private float timer;
        private float idleLoopTimer;
        private int phase;
        private readonly List<BounceArc> activeArcs = new();

        private sealed class BounceArc
        {
            internal readonly Zombie Zombie;
            internal readonly Vector3 Start;
            internal readonly float TargetX;
            internal readonly float Height;
            internal float Elapsed;
            internal bool Discharged;

            internal BounceArc(Zombie zombie, float targetX, float height)
            {
                Zombie = zombie;
                Start = zombie.transform.position;
                TargetX = targetX;
                Height = height;
            }
        }

        public SparkShroom(IntPtr pointer) : base(pointer) { }

        public void Start()
        {
            plant = GetComponent<Caltrop>();
            animator = Core.V11PlantsBootstrap
                .ApplyExactLocalAnimationController(
                    gameObject,
                    "Spark-shroom"
                );
            Play("idle");
            idleLoopTimer = 2.5f;
        }

        private void Play(string state)
        {
            if (animator != null)
                animator.Play("Base Layer." + state, 0, 0f);
        }

        public void Update()
        {
            if (plant == null || plant.dying || !plant.Active ||
                Board.Instance == null)
            {
                return;
            }

            timer -= Time.deltaTime;
            if (phase == 1 && timer <= 0f)
            {
                AnimAttack();
                return;
            }
            if (phase == 2 && timer <= 0f)
            {
                phase = 3;
                timer = RestSeconds - RestAnimationSeconds;
                Play("rest-idle");
                return;
            }
            if (phase == 3 && timer <= 0f)
            {
                phase = 4;
                timer = WakeAnimationSeconds;
                Play("wake-up");
                return;
            }
            if (phase == 4 && timer <= 0f)
            {
                phase = 0;
                Play("idle");
                idleLoopTimer = 2.5f;
            }
            if (phase != 0)
                return;

            // The supplied controller has an unconditional idle -> attack
            // transition. Restart idle just before its exit time so the
            // visual attack can only be entered by the gameplay trigger.
            idleLoopTimer -= Time.deltaTime;
            if (idleLoopTimer <= 0f)
            {
                Play("idle");
                idleLoopTimer = 2.5f;
            }

            var zombies = Lawnf.GetAllZombies(false);
            for (int index = 0; index < zombies.Count; index++)
            {
                if (!InRange(zombies[index]))
                    continue;
                phase = 1;
                timer = 0.75f;
                Play("attack");
                break;
            }
        }

        private bool InRange(Zombie zombie)
        {
            return plant != null && zombie != null && zombie.Alive &&
                zombie.theHealth > 0 && !zombie.isMindControlled &&
                zombie.theZombieRow == plant.thePlantRow &&
                Mathf.Abs(zombie.transform.position.x -
                    transform.position.x) < 0.55f &&
                zombie.theZombieType != ZombieType.BalloonZombie &&
                zombie.theZombieType != ZombieType.BungiZombie;
        }

        public void LateUpdate()
        {
            for (int index = activeArcs.Count - 1; index >= 0; index--)
            {
                BounceArc arc = activeArcs[index];
                try
                {
                    if (arc.Zombie == null || !arc.Zombie.Alive)
                    {
                        activeArcs.RemoveAt(index);
                        continue;
                    }

                    arc.Elapsed += Time.deltaTime;
                    float progress = Mathf.Clamp01(
                        arc.Elapsed / BounceFlightSeconds
                    );
                    float horizontal = Mathf.SmoothStep(0f, 1f, progress);
                    Vector3 position = arc.Start;
                    position.x = Mathf.Lerp(
                        arc.Start.x,
                        arc.TargetX,
                        horizontal
                    );
                    position.y += Mathf.Sin(Mathf.PI * progress) *
                        arc.Height;
                    arc.Zombie.transform.position = position;

                    if (!arc.Discharged && progress >= 0.5f)
                    {
                        arc.Discharged = true;
                        DischargeFrom(arc.Zombie);
                    }

                    if (progress >= 1f)
                        activeArcs.RemoveAt(index);
                }
                catch (Exception)
                {
                    activeArcs.RemoveAt(index);
                }
            }
        }

        public void OnDestroy()
        {
            for (int index = 0; index < activeArcs.Count; index++)
            {
                BounceArc arc = activeArcs[index];
                try
                {
                    if (arc.Zombie == null || !arc.Zombie.Alive)
                        continue;
                    Vector3 position = arc.Start;
                    position.x = arc.TargetX;
                    arc.Zombie.transform.position = position;
                }
                catch { }
            }
            activeArcs.Clear();
        }

        private void LaunchArc(Zombie zombie)
        {
            float tileWidth = Mathf.Abs(
                Lawnf.GetBoxXFromColumn(1) -
                Lawnf.GetBoxXFromColumn(0)
            );
            float rightEdge = Lawnf.GetBoxXFromColumn(
                Mathf.Max(1, Board.Instance.columnNum - 1)
            ) + tileWidth;
            float targetX = Mathf.Min(
                zombie.transform.position.x +
                    tileWidth * BounceDistanceInTiles,
                rightEdge
            );
            activeArcs.Add(new BounceArc(
                zombie,
                targetX,
                tileWidth * 0.55f
            ));
        }

        private void DischargeFrom(Zombie source)
        {
            if (plant == null || source == null || !source.Alive)
                return;

            Zombie? target = FindNearestOtherZombie(source);
            if (target == null)
                return;

            try
            {
                ((Entity)target).TakeDamage(
                    LightningDamage,
                    plant.ToIDamageMaker(),
                    DamageType.Normal,
                    (PlantType)ID,
                    false
                );
            }
            catch
            {
                try
                {
                    target.ApplyDamage(
                        DamageType.Normal,
                        LightningDamage
                    );
                }
                catch
                {
                    return;
                }
            }

            Core.IcytronionBootstrap.SpawnZombieLightning(source, target);
        }

        private Zombie? FindNearestOtherZombie(Zombie source)
        {
            int sourceColumn;
            try
            {
                sourceColumn = Lawnf.GetColumnFromX(
                    source.transform.position.x
                );
            }
            catch
            {
                return null;
            }

            Zombie? nearest = null;
            float nearestDistance = float.MaxValue;
            var zombies = Lawnf.GetAllZombies(false);
            for (int index = 0; index < zombies.Count; index++)
            {
                Zombie candidate = zombies[index];
                if (candidate == null || candidate == source ||
                    !candidate.Alive || candidate.theHealth <= 0 ||
                    candidate.isMindControlled ||
                    Math.Abs(candidate.theZombieRow -
                        source.theZombieRow) > ElectronionRangeInRows)
                {
                    continue;
                }

                int candidateColumn;
                try
                {
                    candidateColumn = Lawnf.GetColumnFromX(
                        candidate.transform.position.x
                    );
                }
                catch
                {
                    continue;
                }

                if (Math.Abs(candidateColumn - sourceColumn) >
                    ElectronionRangeInColumns)
                {
                    continue;
                }

                float distance = (candidate.transform.position -
                    source.transform.position).sqrMagnitude;
                if (distance >= nearestDistance)
                    continue;
                nearest = candidate;
                nearestDistance = distance;
            }
            return nearest;
        }

        public void AnimAttack()
        {
            if (phase != 1 || plant == null)
                return;

            phase = 2;
            timer = RestAnimationSeconds;

            var zombies = Lawnf.GetAllZombies(false);
            for (int index = 0; index < zombies.Count; index++)
            {
                Zombie zombie = zombies[index];
                if (InRange(zombie))
                    LaunchArc(zombie);
            }
            Play("rest");
        }
    }
}
