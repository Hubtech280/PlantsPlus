using System;
using System.Collections.Generic;
using System.Reflection;
using CustomizeLib.MelonLoader;
using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class BounceShroomBootstrap
    {
        private static bool registered;
        internal static void OnStart()
        {
            if (registered) return;
            try
            {
            var bundle = CustomCore.GetAssetBundle(Assembly.GetExecutingAssembly(), "PlantsPlus.Resources.AssetBundles.bounce_shroom");
            var prefab = bundle?.GetAsset<GameObject>("CaltropPrefab");
            var preview = bundle?.GetAsset<GameObject>("CaltropPreview");
            if (bundle == null || prefab == null || preview == null)
                throw new InvalidOperationException("Bounce-shroom prefab or preview missing");
            prefab.transform.localPosition = Vector3.zero;
            V11PlantsBootstrap.IsolateAnimationClips(bundle, prefab, "Bounce-shroom", "idle", "attack", "rest", "rest-idle", "wake-up");
            CustomCore.RegisterCustomPlant<Caltrop, Plants.BounceShroom>(Plants.BounceShroom.ID, prefab, preview, new List<(int,int)>(), 0f, 0f, 0, 300, 15f, 75);
            PlantType type = (PlantType)Plants.BounceShroom.ID;
            InstallTypeFlags();
            AlmanacEntry almanac = AlmanacContent.BounceShroom;
            CustomCore.AddPlantAlmanacStrings(type,
                almanac.Name, almanac.Info, almanac.Introduce, 75);
            Plugin.Logger.LogInfo("[Bounce-shroom] Registered | ID=6035 | Cost=75 | Recharge=15s | Rest=5s");
            registered = true;
            }
            catch (Exception error)
            {
                Plugin.Logger.LogError("[Bounce-shroom] Registration failed: " + error);
            }
        }
        internal static void OnGameInit() => InstallTypeFlags();

        private static void InstallTypeFlags()
        {
            PlantType type = (PlantType)Plants.BounceShroom.ID;
            if (CustomCore.TypeMgrExtra.IsCaltrop != null &&
                !CustomCore.TypeMgrExtra.IsCaltrop.Contains(type))
                CustomCore.TypeMgrExtra.IsCaltrop.Add(type);
            if (CoreEnums.baiscPlants != null &&
                !CoreEnums.baiscPlants.Contains(type))
                CoreEnums.baiscPlants.Add(type);
            TypeData.SpecialCardPlants?.Remove(type);
        }
    }

    [HarmonyPatch(typeof(Caltrop), nameof(Caltrop.AnimAttack))]
    internal static class BounceShroomAttackPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Caltrop __instance)
        {
            if ((int)__instance.thePlantType != Plants.BounceShroom.ID &&
                (int)__instance.thePlantType != Plants.PulseShroom.ID &&
                (int)__instance.thePlantType != Plants.SolsticeShroom.ID)
                return true;
            __instance.GetComponent<Plants.BounceShroom>()?.AnimAttack();
            return false;
        }
    }
    [HarmonyPatch(typeof(Caltrop), nameof(Caltrop.Update))]
    internal static class BounceShroomUpdatePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Caltrop __instance) =>
            (int)__instance.thePlantType != Plants.BounceShroom.ID &&
            (int)__instance.thePlantType != Plants.PulseShroom.ID &&
            (int)__instance.thePlantType != Plants.SolsticeShroom.ID;
    }
    [HarmonyPatch(typeof(BungiZombie), nameof(BungiZombie.Stealable))]
    internal static class BounceShroomBungeePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Plant plant, ref bool __result)
        {
            if (plant == null ||
                ((int)plant.thePlantType != Plants.BounceShroom.ID &&
                 (int)plant.thePlantType != Plants.PulseShroom.ID &&
                 (int)plant.thePlantType != Plants.SolsticeShroom.ID))
                return true;
            __result = false; return false;
        }
    }
    [HarmonyPatch(typeof(Caltrop), nameof(Caltrop.KillCar))]
    internal static class BounceShroomCarPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Caltrop __instance) =>
            (int)__instance.thePlantType != Plants.BounceShroom.ID &&
            (int)__instance.thePlantType != Plants.PulseShroom.ID &&
            (int)__instance.thePlantType != Plants.SolsticeShroom.ID;
    }
}

namespace PlantsPlus.Plants
{
    public sealed class BounceShroom : MonoBehaviour
    {
        public const int ID = 6035;
        private const float BounceDistanceInTiles = 1.5f;
        private const float BounceFlightSeconds = 0.55f;
        private const float RestAnimationSeconds = 0.8333333f;
        private const float RestSeconds = 5f;
        private const float WakeAnimationSeconds = 0.8333333f;
        private Caltrop? plant;
        private Animator? animator;
        private float timer;
        private int phase;
        private readonly List<BounceArc> activeArcs = new();

        private sealed class BounceArc
        {
            internal readonly Zombie Zombie;
            internal readonly Vector3 Start;
            internal readonly float TargetX;
            internal readonly float Height;
            internal float Elapsed;

            internal BounceArc(Zombie zombie, float targetX, float height)
            {
                Zombie = zombie;
                Start = zombie.transform.position;
                TargetX = targetX;
                Height = height;
            }
        }
        public BounceShroom(IntPtr pointer) : base(pointer) { }
        public void Start()
        {
            plant = GetComponent<Caltrop>();
            animator = Core.V11PlantsBootstrap.ApplyExactLocalAnimationController(
                gameObject,
                plant != null &&
                (int)plant.thePlantType == PulseShroom.ID
                    ? "Pulse-shroom" : plant != null && (int)plant.thePlantType == SolsticeShroom.ID
                        ? "Solstice-shroom" : "Bounce-shroom");
            Play("idle");
            CheckFusion();
        }
        private void Play(string state) { if (animator != null) animator.Play("Base Layer." + state, 0, 0); }
        public void Update()
        {
            if (plant == null || plant.dying || !plant.Active || Board.Instance == null) return;
            if ((int)plant.thePlantType == SolsticeShroom.ID && !SuperaquaShroom.IsLiveBoardPlant(plant)) return;
            CheckFusion();
            timer -= Time.deltaTime;
            if (phase == 1 && timer <= 0) { AnimAttack(); return; }
            if (phase == 2 && timer <= 0)
            {
                phase = 3;
                timer = RestSeconds - RestAnimationSeconds;
                Play("rest-idle");
                return;
            }
            if (phase == 3 && timer <= 0)
            {
                phase = 4;
                timer = WakeAnimationSeconds;
                Play("wake-up");
                return;
            }
            if (phase == 4 && timer <= 0) { phase = 0; Play("idle"); }
            if (phase != 0) return;
            var zombies = Lawnf.GetAllZombies(false);
            foreach (Zombie zombie in zombies)
                if (InRange(zombie)) { phase = 1; timer = 0.75f; Play("attack"); break; }
            if (phase == 0 && animator != null)
            {
                AnimatorStateInfo state =
                    animator.GetCurrentAnimatorStateInfo(0);
                if (animator.IsInTransition(0) ||
                    !state.IsName("Base Layer.idle"))
                    Play("idle");
            }
        }

        private void CheckFusion()
        {
            if (plant == null || plant.dying || plant.thePlantHealth <= 0)
                return;
            if ((int)plant.thePlantType != ID)
                return;

            BumpingFusionRunner.TryQueue(
                plant.thePlantColumn,
                plant.thePlantRow
            );
        }
        private bool InRange(Zombie zombie) => plant != null && zombie != null && zombie.Alive && !zombie.isMindControlled && zombie.theZombieRow == plant.thePlantRow && Mathf.Abs(zombie.transform.position.x - transform.position.x) < 0.55f && zombie.theZombieType != ZombieType.BalloonZombie && zombie.theZombieType != ZombieType.BungiZombie;

        public void LateUpdate()
        {
            if (plant != null) Core.RoofFusionWork.Observe(plant);
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
                        arc.Elapsed / BounceFlightSeconds);
                    float horizontal = Mathf.SmoothStep(0f, 1f, progress);
                    Vector3 position = arc.Start;
                    position.x = Mathf.Lerp(arc.Start.x,
                        arc.TargetX, horizontal);
                    position.y += Mathf.Sin(Mathf.PI * progress) * arc.Height;
                    arc.Zombie.transform.position = position;
                    if (progress >= 1f)
                        activeArcs.RemoveAt(index);
                }
                catch (Exception)
                {
                    // A zombie can despawn while it is airborne.
                    activeArcs.RemoveAt(index);
                }
            }
        }

        public void OnDestroy()
        {
            foreach (BounceArc arc in activeArcs)
            {
                try
                {
                    if (arc.Zombie == null || !arc.Zombie.Alive)
                        continue;
                    Vector3 position = arc.Start;
                    position.x = arc.TargetX;
                    arc.Zombie.transform.position = position;
                }
                catch (Exception) { }
            }
            activeArcs.Clear();
        }

        private bool LaunchArc(Zombie zombie)
        {
            float tileWidth = Mathf.Abs(
                Lawnf.GetBoxXFromColumn(1) -
                Lawnf.GetBoxXFromColumn(0));
            float rightEdge = Lawnf.GetBoxXFromColumn(
                Mathf.Max(1, Board.Instance.columnNum - 1)) + tileWidth;
            float targetX = Mathf.Min(
                zombie.transform.position.x +
                    tileWidth * BounceDistanceInTiles,
                rightEdge);
            if (targetX <= zombie.transform.position.x + 0.01f) return false;
            activeArcs.Add(new BounceArc(zombie, targetX,
                tileWidth * 0.55f));
            return true;
        }
        public void AnimAttack()
        {
            if (phase != 1 || plant == null) return;
            phase = 2; timer = RestAnimationSeconds;
            int launched = 0;
            foreach (Zombie zombie in Lawnf.GetAllZombies(false))
                if (InRange(zombie))
                {
                    if (LaunchArc(zombie)) launched++;
                }
            if (launched > 0 && (int)plant.thePlantType == PulseShroom.ID)
                GetComponent<PulseShroom>()?.OnBounce();
            if (launched > 0 && (int)plant.thePlantType == SolsticeShroom.ID)
                GetComponent<SolsticeShroom>()?.OnBounce(launched);
            Play("rest");
        }
    }
}
