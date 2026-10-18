using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.Attributes;
using CustomizeLib.MelonLoader;
using PlantsPlus.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlantsPlus.Plants
{
    public sealed class SuperaquaShroom : MonoBehaviour
    {
        public const int ID = 6038;
        private WaterSwordshroom? plant;
        private Animator? animator;
        private GameObject? frozenHead;
        private GameObject? normalHead;
        private ParticleSystem[]? attackParticles;
        private bool firstAttackGranted;
        private bool nativeParticlesReady;
        private bool openingAttackStarted;
        private float markerTimer;
        private float visualTimer;
        private float openingAttackTimer;
        private readonly Dictionary<int, float> nextFirnurseCharge =
            new Dictionary<int, float>();

        public SuperaquaShroom(IntPtr pointer) : base(pointer) { }

        public void Start()
        {
            plant = GetComponent<WaterSwordshroom>();
            animator = V11PlantsBootstrap.ApplyExactLocalAnimationController(
                gameObject, "Superaqua-shroom");
            frozenHead = FindVisual(transform, "head_1", "head 1")?.gameObject;
            normalHead = FindVisual(transform, "head_2", "head 2")?.gameObject;
            if (plant != null && plant.attributeCount < 0)
                plant.attributeCount = 0;
            RefreshChargeVisual();
            KeepPreviewIdle();
        }

        public void Update()
        {
            if (!IsLiveBoardPlant(plant))
            {
                openingAttackTimer = 0f;
                KeepPreviewIdle();
                return;
            }
            if (plant == null || !plant.Active)
                return;

            if (plant.attributeCount < 0)
                plant.attributeCount = 0;
            if (openingAttackTimer > 0f)
                openingAttackTimer -= Time.deltaTime;
            visualTimer -= Time.deltaTime;
            if (visualTimer <= 0f)
            {
                visualTimer = 0.12f;
                RefreshChargeVisual();
                PreventEmptyAttackAnimation();
                if (!nativeParticlesReady)
                    nativeParticlesReady = TryRepairNativeParticles();
                if (!openingAttackStarted)
                    BeginOpeningAttack();
            }

            markerTimer -= Time.deltaTime;
            if (markerTimer > 0f)
                return;
            markerTimer = 0.75f;

            var plants = Lawnf.GetAllPlants();
            if (plants == null)
                return;
            for (int i = 0; i < plants.Count; i++)
            {
                Plant shooter = plants[i];
                if (shooter == null || shooter.dying)
                    continue;
                if (shooter.thePlantType == PlantType.SuperFurnace)
                {
                    SuperFurnace firnurse =
                        shooter.GetComponent<SuperFurnace>();
                    if (firnurse != null)
                        TryChargeFromFirnurse(firnurse);
                }
                if (shooter.thePlantType != PlantType.SuperSpruce)
                    continue;
                if (shooter.GetComponent<SuperaquaShooterBoost>() == null)
                    shooter.gameObject.AddComponent<SuperaquaShooterBoost>();
            }
        }

        [HideFromIl2Cpp]
        internal void GrantFirstAttackCharge()
        {
            if (firstAttackGranted || !IsLiveBoardPlant(plant))
                return;
            firstAttackGranted = true;

            var plants = Lawnf.GetAllPlants();
            if (plants == null)
                return;
            int charged = 0;
            for (int i = 0; i < plants.Count; i++)
            {
                Plant target = plants[i];
                if (target == null || target.dying ||
                    target.thePlantType != PlantType.SuperSpruce)
                    continue;
                // Let the native Super Sharpshooter own its ice-charge state.
                target.Charge();
                charged++;
            }
            Plugin.Logger.LogInfo(
                "[Superaqua-shroom] First melee strike charged " +
                charged + " Super Sharpshooters.");
        }

        [HideFromIl2Cpp]
        internal void OnMeleeHit(Zombie zombie)
        {
            if (!IsLiveBoardPlant(plant))
                return;
            GrantFirstAttackCharge();
            PlayAttackParticles();
            if (plant == null || zombie == null || plant.attributeCount <= 0)
                return;

            // A charged Superaqua-shroom freezes the zombie struck by its
            // blade. Keep the duration short because every melee swing can
            // refresh it.
            try { zombie.SetFreeze(3f); }
            catch { try { zombie.SetCold(3f); } catch { } }
        }

        [HideFromIl2Cpp]
        internal bool IsOpeningStrike() => openingAttackStarted &&
            openingAttackTimer > 0f && IsLiveBoardPlant(plant);

        [HideFromIl2Cpp]
        internal void EnsureChargeAdvanced(int previousValue)
        {
            if (!IsLiveBoardPlant(plant) || plant == null)
                return;
            int previous = Mathf.Clamp(previousValue, 0, 15);
            int current = Mathf.Clamp(plant.attributeCount, 0, 15);
            if (current <= previous)
                current = Mathf.Min(15, previous + 1);
            plant.attributeCount = current;
            RefreshChargeVisual();
        }

        internal static bool IsLiveBoardPlant(Plant? candidate)
        {
            Board? board = Board.Instance;
            if (candidate == null || candidate.dying || board == null ||
                board.boardEntity == null ||
                !candidate.gameObject.activeInHierarchy)
                return false;

            // A Board singleton can remain alive while the Almanac is open.
            // Only entities registered on that board may execute combat.
            var plants = board.boardEntity.plantArray;
            if (plants == null)
                return false;
            for (int i = 0; i < plants.Count; i++)
            {
                Plant member = plants[i];
                if (member != null && member.Pointer == candidate.Pointer)
                    return true;
            }
            return false;
        }

        [HideFromIl2Cpp]
        private void KeepPreviewIdle()
        {
            if (animator != null)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (animator.IsInTransition(0) || !state.IsName("Base Layer.idle"))
                    animator.Play("Base Layer.idle", 0, 0f);
            }
            if (attackParticles == null)
                return;
            for (int i = 0; i < attackParticles.Length; i++)
                if (attackParticles[i] != null && attackParticles[i].isPlaying)
                    attackParticles[i].Stop(true,
                        ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        [HideFromIl2Cpp]
        private void TryChargeFromFirnurse(SuperFurnace firnurse)
        {
            if (plant == null || firnurse == null ||
                firnurse.thePlantType != PlantType.SuperFurnace ||
                Math.Abs(plant.thePlantColumn - firnurse.thePlantColumn) > 1 ||
                Math.Abs(plant.thePlantRow - firnurse.thePlantRow) > 1)
                return;
            int id = firnurse.GetInstanceID();
            float nextCharge;
            if (!nextFirnurseCharge.TryGetValue(id, out nextCharge))
            {
                nextFirnurseCharge[id] = Time.time + 3f;
                return;
            }
            if (Time.time < nextCharge)
                return;
            nextFirnurseCharge[id] = Time.time + 3f;
            if (plant.attributeCount >= 15)
                return;
            EnsureChargeAdvanced(plant.attributeCount);
            Plugin.Logger.LogInfo(
                "[Superaqua-shroom] Firnurse granted one ice charge.");
        }

        [HideFromIl2Cpp]
        private void RefreshChargeVisual()
        {
            bool charged = plant != null && plant.attributeCount > 0;
            if (frozenHead != null && frozenHead.activeSelf != charged)
                frozenHead.SetActive(charged);
            if (normalHead != null && normalHead.activeSelf == charged)
                normalHead.SetActive(!charged);
        }

        [HideFromIl2Cpp]
        private void PreventEmptyAttackAnimation()
        {
            if (animator == null || openingAttackTimer > 0f ||
                HasMeleeTarget())
                return;
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (animator.IsInTransition(0) ||
                !state.IsName("Base Layer.idle"))
                animator.Play("Base Layer.idle", 0, 0f);
        }

        [HideFromIl2Cpp]
        private bool HasMeleeTarget()
        {
            if (plant == null)
                return false;
            float tile = Mathf.Abs(Lawnf.GetBoxXFromColumn(1) -
                Lawnf.GetBoxXFromColumn(0));
            var zombies = Lawnf.GetAllZombies(false);
            if (zombies == null)
                return false;
            for (int i = 0; i < zombies.Count; i++)
            {
                Zombie zombie = zombies[i];
                if (zombie == null || !zombie.Alive ||
                    zombie.isMindControlled ||
                    zombie.theZombieRow != plant.thePlantRow)
                    continue;
                float distance = zombie.transform.position.x -
                    transform.position.x;
                if (distance >= -tile * 0.35f && distance <= tile * 2.6f)
                    return true;
            }
            return false;
        }

        [HideFromIl2Cpp]
        private bool TryRepairNativeParticles()
        {
            try
            {
                if (GameAPP.resourcesManager == null ||
                    GameAPP.resourcesManager.plantPrefabs == null ||
                    !GameAPP.resourcesManager.plantPrefabs.ContainsKey(
                        PlantType.WaterSwordshroom))
                    return false;
                GameObject native = GameAPP.resourcesManager.plantPrefabs[
                    PlantType.WaterSwordshroom];
                ParticleSystemRenderer[] source = native
                    .GetComponentsInChildren<ParticleSystemRenderer>(true);
                ParticleSystemRenderer[] oldRenderers = gameObject
                    .GetComponentsInChildren<ParticleSystemRenderer>(true);
                if (source.Length == 0)
                    return false;

                // The bundle particle renderers have no valid material in the
                // built game. Clone the complete native Water Sword-shroom
                // particle objects instead of trying to repair those broken
                // renderers piecemeal.
                for (int i = 0; i < oldRenderers.Length; i++)
                    if (oldRenderers[i] != null)
                        oldRenderers[i].enabled = false;

                Color darkBlue = new Color(0.04f, 0.18f, 0.72f, 1f);
                attackParticles = new ParticleSystem[source.Length];
                int materialCount = 0;
                for (int i = 0; i < source.Length; i++)
                {
                    ParticleSystemRenderer nativeRenderer = source[i];
                    if (nativeRenderer == null)
                        continue;
                    GameObject clone = UnityEngine.Object.Instantiate(
                        nativeRenderer.gameObject, transform);
                    clone.name = "Superaqua_NativeParticle_" + i;
                    clone.transform.localPosition =
                        nativeRenderer.transform.localPosition;
                    clone.transform.localRotation =
                        nativeRenderer.transform.localRotation;
                    clone.transform.localScale =
                        nativeRenderer.transform.localScale;
                    clone.SetActive(true);
                    ParticleSystem particle =
                        clone.GetComponent<ParticleSystem>();
                    if (particle == null)
                        continue;
                    ParticleSystem.MainModule main = particle.main;
                    main.startColor =
                        new ParticleSystem.MinMaxGradient(darkBlue);
                    main.loop = false;
                    // Both native Fume systems have EmissionModule disabled.
                    // Play() alone cannot make them visible; Emit() below
                    // supplies a burst while retaining the native material.
                    ParticleSystemRenderer cloneRenderer =
                        clone.GetComponent<ParticleSystemRenderer>();
                    if (cloneRenderer != null)
                    {
                        cloneRenderer.enabled = true;
                        cloneRenderer.sharedMaterial =
                            nativeRenderer.sharedMaterial;
                        cloneRenderer.sortingOrder = 20;
                        if (cloneRenderer.sharedMaterial != null)
                            materialCount++;
                    }
                    UnityEngine.Rendering.SortingGroup sortGroup =
                        clone.GetComponent<UnityEngine.Rendering.SortingGroup>();
                    if (sortGroup == null)
                        sortGroup = clone.AddComponent<UnityEngine.Rendering.SortingGroup>();
                    if (sortGroup != null)
                    {
                        sortGroup.sortingOrder = 20;
                        // Swordshroom.Shoot2 uses these native Fume references.
                        if (plant != null && i == 0)
                            plant.p1 = sortGroup;
                        if (plant != null && i == 1)
                            plant.p2 = sortGroup;
                    }
                    particle.Stop(true,
                        ParticleSystemStopBehavior.StopEmittingAndClear);
                    attackParticles[i] = particle;
                }
                Plugin.Logger.LogInfo(
                    "[Superaqua-shroom] Native Fume emitters ready: " +
                    source.Length + " | Materials=" + materialCount);
                return true;
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Superaqua-shroom] Native particle repair retry: " +
                    exception.Message);
                return false;
            }
        }

        [HideFromIl2Cpp]
        private void BeginOpeningAttack()
        {
            if (openingAttackStarted || !IsLiveBoardPlant(plant))
                return;
            if (plant != null)
                V11PlantsBootstrap.EnsureShooterRuntimeReferences(
                    plant, "Superaqua-shroom");
            openingAttackStarted = true;
            openingAttackTimer = 1.8f;
            animator?.Play("Base Layer.attack2", 0, 0f);
            PlayAttackParticles();
            GrantFirstAttackCharge();
        }

        [HideFromIl2Cpp]
        private void PlayAttackParticles()
        {
            if (attackParticles == null)
                return;
            for (int i = 0; i < attackParticles.Length; i++)
            {
                ParticleSystem particle = attackParticles[i];
                if (particle == null)
                    continue;
                particle.gameObject.SetActive(true);
                particle.Stop(true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
                particle.Play(true);
                particle.Emit(18);
            }
        }

        private static Transform? FindVisual(Transform root,
            params string[] names)
        {
            if (root == null)
                return null;
            for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
                if (string.Equals(root.name, names[nameIndex],
                    StringComparison.OrdinalIgnoreCase))
                    return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform? found = FindVisual(root.GetChild(i), names);
                if (found != null)
                    return found;
            }
            return null;
        }
    }

    public sealed class SuperaquaShooterBoost : MonoBehaviour
    {
        private Plant? plant;
        private float originalInterval;
        private float lastMultiplier = 1f;
        private float checkTimer;

        public SuperaquaShooterBoost(IntPtr pointer) : base(pointer) { }

        public void Start()
        {
            plant = GetComponent<Plant>();
            if (plant != null)
                originalInterval = plant.thePlantAttackInterval;
        }

        public void Update()
        {
            if (plant == null || !SuperaquaShroom.IsLiveBoardPlant(plant) ||
                plant.thePlantType != PlantType.SuperSpruce)
                return;
            checkTimer -= Time.deltaTime;
            if (checkTimer > 0f)
                return;
            checkTimer = 0.3f;

            int highestCharge = 0;
            var plants = Lawnf.GetAllPlants();
            if (plants != null)
            {
                for (int i = 0; i < plants.Count; i++)
                {
                    Plant source = plants[i];
                    if (source == null || source.dying ||
                        (int)source.thePlantType != SuperaquaShroom.ID)
                        continue;
                    highestCharge = Math.Max(highestCharge,
                        source.attributeCount);
                }
            }

            // Four percent more shots per ice charge, capped at 15 charges.
            float multiplier = 1f +
                Mathf.Clamp(highestCharge, 0, 15) * 0.04f;
            if (Mathf.Abs(multiplier - lastMultiplier) < 0.001f)
                return;
            if (originalInterval <= 0f)
                originalInterval = plant.thePlantAttackInterval;
            if (originalInterval <= 0f)
                return;

            plant.thePlantAttackInterval =
                originalInterval / multiplier;
            lastMultiplier = multiplier;
        }

        public void OnDestroy()
        {
            if (plant != null && !plant.dying &&
                originalInterval > 0f && lastMultiplier != 1f)
                plant.thePlantAttackInterval = originalInterval;
        }
    }
}

namespace PlantsPlus.Core
{
    [HarmonyPatch(typeof(WaterSwordshroom), "ChangeEnergy")]
    internal static class SuperaquaNativeEnergyPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(WaterSwordshroom __instance)
        {
            // The custom bundle has no native energy renderers. In 4.0 the
            // native method dereferences them during attacks and ice effects.
            // Superaqua owns its charge count and visuals separately.
            return (int)__instance.thePlantType !=
                Plants.SuperaquaShroom.ID;
        }
    }

    [HarmonyPatch(typeof(WaterSwordshroom), "AttackZombie")]
    internal static class SuperaquaMeleePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(WaterSwordshroom __instance) =>
            (int)__instance.thePlantType != Plants.SuperaquaShroom.ID ||
            Plants.SuperaquaShroom.IsLiveBoardPlant(__instance);

        [HarmonyPostfix]
        private static void Postfix(WaterSwordshroom __instance,
            Zombie zombie)
        {
            if (zombie == null ||
                (int)__instance.thePlantType !=
                    Plants.SuperaquaShroom.ID)
                return;
            __instance.GetComponent<Plants.SuperaquaShroom>()?
                .OnMeleeHit(zombie);
        }
    }

    [HarmonyPatch(typeof(WaterSwordshroom), "BigAttackZombie")]
    internal static class SuperaquaBigMeleePatch
    {
        private static bool firstNativeHitLogged;
        [HarmonyPrefix]
        private static bool Prefix(WaterSwordshroom __instance) =>
            (int)__instance.thePlantType != Plants.SuperaquaShroom.ID ||
            Plants.SuperaquaShroom.IsLiveBoardPlant(__instance);

        [HarmonyPostfix]
        private static void Postfix(WaterSwordshroom __instance,
            Zombie zombie)
        {
            if (zombie == null ||
                (int)__instance.thePlantType !=
                    Plants.SuperaquaShroom.ID)
                return;
            Plants.SuperaquaShroom? behaviour =
                __instance.GetComponent<Plants.SuperaquaShroom>();
            behaviour?.OnMeleeHit(zombie);
            if (behaviour == null || !behaviour.IsOpeningStrike() ||
                !zombie.Alive)
                return;
            zombie.Buttered(3f, false);
            zombie.TakeDamage(DmgType.Ice, 20,
                (PlantType)Plants.SuperaquaShroom.ID);
            if (!firstNativeHitLogged)
            {
                firstNativeHitLogged = true;
                Plugin.Logger.LogInfo(
                    "[Superaqua-shroom] Native Shoot2 hit confirmed | " +
                    "Opening bonus = 20 damage, 3s stun.");
            }
        }
    }

    [HarmonyPatch(typeof(Swordshroom), "Shoot2")]
    internal static class SuperaquaOpeningAnimationPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Swordshroom __instance) =>
            (int)__instance.thePlantType != Plants.SuperaquaShroom.ID ||
            Plants.SuperaquaShroom.IsLiveBoardPlant(__instance);
    }

    [HarmonyPatch(typeof(WaterSwordshroom), "Shoot1")]
    internal static class SuperaquaMeleeAnimationPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(WaterSwordshroom __instance) =>
            (int)__instance.thePlantType != Plants.SuperaquaShroom.ID ||
            Plants.SuperaquaShroom.IsLiveBoardPlant(__instance);
    }

    [HarmonyPatch(typeof(IceShroom), nameof(IceShroom.Explode))]
    internal static class SuperaquaIceChargePatch
    {
        private sealed class ChargeTarget
        {
            internal Plants.SuperaquaShroom? Behaviour;
            internal int Previous;
        }

        [HarmonyPrefix]
        private static void Prefix(out List<ChargeTarget> __state)
        {
            __state = new List<ChargeTarget>();
            var plants = Lawnf.GetAllPlants();
            if (plants == null)
                return;
            for (int i = 0; i < plants.Count; i++)
            {
                Plant target = plants[i];
                if (target == null || target.dying ||
                    (int)target.thePlantType != Plants.SuperaquaShroom.ID)
                    continue;
                __state.Add(new ChargeTarget
                {
                    Behaviour = target.GetComponent<Plants.SuperaquaShroom>(),
                    Previous = target.attributeCount
                });
            }
        }

        [HarmonyPostfix]
        private static void Postfix(List<ChargeTarget> __state)
        {
            if (__state == null)
                return;
            for (int i = 0; i < __state.Count; i++)
            {
                ChargeTarget target = __state[i];
                target.Behaviour?.EnsureChargeAdvanced(target.Previous);
            }
        }
    }

}
