using HarmonyLib;
using Il2Cpp;
using System;
using UnityEngine;

namespace PlantsPlus.Plants
{
    public class IcebergShroom : MonoBehaviour
    {
        public const int IcebergShroomID = 6003;
        public const int Damage = 40;
        public const float FreezeDuration = 8f;
        public const float ImmuneSlowDuration = 8f;

        private bool enhancedEffectApplied;

        public IcebergShroom(IntPtr ptr) : base(ptr) { }

        public IceShroom? IceShroomPlant =>
            gameObject.GetComponent<IceShroom>();

        public void Start()
        {
            IceShroom? plant = IceShroomPlant;

            if (plant == null)
            {
                Plugin.Logger.LogError(
                    "[IcebergShroom] Start failed: no IceShroom component."
                );
                return;
            }

            Plugin.Logger.LogInfo(
                "[IcebergShroom] Ready | Damage = " + Damage +
                " | Freeze = " + FreezeDuration + " seconds" +
                " | Immune slow = " + ImmuneSlowDuration + " seconds"
            );
        }

        private static bool IsIcebergShroom(IceShroom? plant)
        {
            return
                plant != null &&
                (int)plant.thePlantType == IcebergShroomID;
        }

        private bool TryMarkEffectApplied()
        {
            if (enhancedEffectApplied)
                return false;

            enhancedEffectApplied = true;
            return true;
        }

        private const float SpeedTolerance = 0.001f;

        private static float ReadFreezeSpeed(Zombie zombie)
        {
            try
            {
                return zombie.freezeSpeed;
            }
            catch
            {
                return 1f;
            }
        }

        private static bool HasFreezeBuff(Zombie zombie)
        {
            try
            {
                return zombie.HasBuff(EffectType.Freeze);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryFreeze(Zombie zombie)
        {
            float before = ReadFreezeSpeed(zombie);
            bool hadFreezeBuff = HasFreezeBuff(zombie);

            try
            {
                zombie.SetFreeze(FreezeDuration);
            }
            catch
            {
                return false;
            }

            float after = ReadFreezeSpeed(zombie);

            // The vanilla Ice-shroom explosion runs before this postfix, so a
            // normal target can already be fully frozen here. The buff check
            // handles the 3.8 effect system; the before/after comparison keeps
            // compatibility with zombies that still use the legacy fields.
            return
                hadFreezeBuff ||
                HasFreezeBuff(zombie) ||
                after <= SpeedTolerance ||
                after < before - SpeedTolerance;
        }

        private static bool TryNativeSlow(Zombie zombie)
        {
            try
            {
                // PVZ Fusion 3.8 routes cold through its effect manager. An
                // ice-immune class may reject this soft-control effect; the
                // final-speed fallback below still guarantees the mechanic.
                return EffectManager.SetEffect(
                    zombie,
                    EffectType.Cold,
                    ImmuneSlowDuration,
                    IcebergForcedSlow.SpeedMultiplier
                );
            }
            catch
            {
                return false;
            }
        }

        private static bool TryForcedSlow(Zombie zombie)
        {
            try
            {
                IcebergForcedSlow? slow =
                    zombie.gameObject.GetComponent<IcebergForcedSlow>();

                if (slow == null)
                    slow = zombie.gameObject.AddComponent<IcebergForcedSlow>();

                if (slow == null)
                    return false;

                slow.Refresh(zombie, ImmuneSlowDuration);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void ApplyEnhancedFreeze(IceShroom plant)
        {
            var zombies = Lawnf.GetAllZombies(false);

            if (zombies == null)
            {
                Plugin.Logger.LogWarning(
                    "[IcebergShroom] No zombie list was available at explosion time."
                );
                return;
            }

            int frozen = 0;
            int nativeSlowed = 0;
            int forcedSlowed = 0;
            int skipped = 0;

            for (int i = 0; i < zombies.Count; i++)
            {
                Zombie zombie = zombies[i];

                if (zombie == null)
                {
                    skipped++;
                    continue;
                }

                try
                {
                    if (!zombie.Alive || zombie.isMindControlled)
                    {
                        skipped++;
                        continue;
                    }
                }
                catch
                {
                    skipped++;
                    continue;
                }

                // The vanilla Ice-shroom explosion has already run. Calling
                // SetFreeze again with 8 seconds extends normal freezes to
                // exactly twice the native four-second duration.
                if (TryFreeze(zombie))
                {
                    frozen++;
                    continue;
                }

                // Ice-resistant classes often override SetFreeze. Ask the
                // native 3.8 effect system for the standard cold visuals and
                // timers, then always enforce the movement result separately:
                // some snow zombies accept/reject the effect without changing
                // the speed value they actually use.
                bool nativeSlowApplied = TryNativeSlow(zombie);

                if (nativeSlowApplied)
                    nativeSlowed++;

                if (TryForcedSlow(zombie))
                    forcedSlowed++;
                else if (nativeSlowApplied)
                {
                    // The native effect remains a valid fallback if attaching
                    // the final-speed guard failed for this one target.
                }
                else
                    skipped++;
            }

            Plugin.Logger.LogInfo(
                "[IcebergShroom] Enhanced freeze applied" +
                " | Frozen = " + frozen +
                " | Native cold accepted = " + nativeSlowed +
                " | Final-speed immune slow = " + forcedSlowed +
                " | Skipped = " + skipped
            );
        }

        [HarmonyPatch(typeof(IceShroom), nameof(IceShroom.Explode))]
        private static class IceShroom_Explode_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(IceShroom __instance)
            {
                if (!IsIcebergShroom(__instance))
                    return;

                try
                {
                    IcebergShroom? behaviour =
                        __instance.gameObject.GetComponent<IcebergShroom>();

                    if (behaviour == null)
                    {
                        Plugin.Logger.LogError(
                            "[IcebergShroom] Enhanced freeze was skipped: " +
                            "the custom behaviour is missing."
                        );
                        return;
                    }

                    if (!behaviour.TryMarkEffectApplied())
                        return;

                    ApplyEnhancedFreeze(__instance);
                }
                catch (Exception exception)
                {
                    Plugin.Logger.LogError(
                        "[IcebergShroom] Enhanced freeze failed safely: " +
                        exception
                    );
                }
            }
        }
    }

    public class IcebergForcedSlow : MonoBehaviour
    {
        public const float SpeedMultiplier = 0.5f;
        private const float MinimumUsefulSpeed = 0.0001f;

        private Zombie? target;
        private float remaining;
        private bool active;
        private bool finishing;

        public IcebergForcedSlow(IntPtr ptr) : base(ptr) { }

        public void Refresh(Zombie zombie, float duration)
        {
            if (zombie == null)
                return;

            target = zombie;
            active = true;
            finishing = false;
            remaining = Mathf.Max(remaining, duration);
            EnforceFinalSpeed(zombie);
        }

        public void LateUpdate()
        {
            if (!active)
                return;

            if (target == null)
            {
                StopAndDestroy(false);
                return;
            }

            remaining -= Time.deltaTime;

            if (remaining > 0f)
            {
                EnforceFinalSpeed(target);
                return;
            }

            StopAndDestroy(true);
        }

        public void OnDisable()
        {
            StopWithoutDestroy();
        }

        public void OnDestroy()
        {
            StopWithoutDestroy();
        }

        private void EnforceFinalSpeed(Zombie zombie)
        {
            if (!active || zombie == null || target == null || zombie != target)
                return;

            try
            {
                float originMagnitude = Mathf.Abs(zombie.theOriginSpeed);

                if (originMagnitude <= MinimumUsefulSpeed)
                    return;

                float speed = zombie.theSpeed;
                float cap = originMagnitude * SpeedMultiplier;

                // Preserve direction and any stronger slowdown already
                // applied by the game or another mod.
                if (speed > cap)
                    zombie.theSpeed = cap;
                else if (speed < -cap)
                    zombie.theSpeed = -cap;
            }
            catch
            {
                StopAndDestroy(false);
            }
        }

        private void StopAndDestroy(bool recalculateSpeed)
        {
            if (finishing)
                return;

            finishing = true;
            Zombie? previousTarget = target;
            active = false;
            remaining = 0f;
            target = null;

            if (recalculateSpeed && previousTarget != null)
            {
                try
                {
                    // Rebuild the final movement speed from the game's own
                    // active effects instead of restoring a stale snapshot.
                    previousTarget.UpdateSpeed();
                }
                catch
                {
                    // The target may already be leaving the board.
                }
            }

            try
            {
                UnityEngine.Object.Destroy(this);
            }
            catch
            {
                // The component can already be scheduled for destruction.
            }
        }

        private void StopWithoutDestroy()
        {
            if (finishing || !active)
                return;

            Zombie? previousTarget = target;
            active = false;
            remaining = 0f;
            target = null;

            if (previousTarget == null)
                return;

            try
            {
                previousTarget.UpdateSpeed();
            }
            catch
            {
                // The zombie is being disabled or destroyed.
            }
        }

        [HarmonyPatch(typeof(Zombie), nameof(Zombie.UpdateSpeed))]
        private static class Zombie_UpdateSpeed_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Zombie __instance)
            {
                try
                {
                    IcebergForcedSlow? slow =
                        __instance.gameObject.GetComponent<IcebergForcedSlow>();

                    if (slow != null)
                        slow.EnforceFinalSpeed(__instance);
                }
                catch
                {
                    // Speed updates must never be allowed to break a zombie.
                }
            }
        }
    }
}
