using HarmonyLib;
using Il2Cpp;
using System;
using UnityEngine;

namespace PlantsPlus.Plants
{
    public class LotusPumpkin : MonoBehaviour
    {
        public const int LotusPumpkinID = 6000;

        public const int MaxCharges = 5;
        public const float ProtectedPlantHeal = 600f;

        // This is only a once-per-cycle guard. The real charge value remains
        // entirely owned by IceLotus.attributeCount / IceLotus.Charge().
        private bool recoveryHandled;
        private int lastObservedNativeCharge = -1;

        public LotusPumpkin(IntPtr ptr) : base(ptr) { }

        public IceLotus IceLotusPlant => gameObject.GetComponent<IceLotus>();

        public void Start()
        {
            IceLotus self = IceLotusPlant;

            if (self == null)
            {
                Plugin.Logger.LogError("[LotusPumpkin] Start failed: no IceLotus component.");
                return;
            }

            Plugin.Logger.LogInfo(
                "[LotusPumpkin] Ready | Type = " +
                (int)self.thePlantType +
                " | HP = " +
                self.thePlantHealth +
                "/" +
                self.thePlantMaxHealth +
                " | Row = " +
                self.thePlantRow +
                " | Column = " +
                self.thePlantColumn
            );

            lastObservedNativeCharge = self.attributeCount;
        }

        public void Update()
        {
            IceLotus self = IceLotusPlant;

            if (!IsLotusPumpkin(self))
                return;

            int currentCharge = self.attributeCount;

            if (lastObservedNativeCharge < 0)
            {
                lastObservedNativeCharge = currentCharge;
                return;
            }

            if (currentCharge == lastObservedNativeCharge)
                return;

            Plugin.Logger.LogInfo(
                "[LotusPumpkin] Native counter observed | " +
                lastObservedNativeCharge +
                " -> " +
                currentCharge
            );

            if (ReachedFive(lastObservedNativeCharge, currentCharge))
            {
                TriggerProtectedRecovery(
                    self,
                    "passive native counter"
                );
            }
            else if (IsFreshCycleProgress(lastObservedNativeCharge, currentCharge))
            {
                ResetRecoveryGuard();
            }

            // TriggerProtectedRecovery can reset the real counter to zero.
            lastObservedNativeCharge = self.attributeCount;
        }

        private static bool IsLotusPumpkin(Plant plant)
        {
            return plant != null && (int)plant.thePlantType == LotusPumpkinID;
        }

        private void ResetRecoveryGuard()
        {
            recoveryHandled = false;
        }

        private static bool ReachedFive(int chargeBefore, int chargeAfter)
        {
            return
                (chargeBefore < MaxCharges && chargeAfter >= MaxCharges) ||
                (chargeAfter < chargeBefore && chargeBefore >= MaxCharges - 1);
        }

        private static bool IsFreshCycleProgress(int chargeBefore, int chargeAfter)
        {
            return chargeAfter > 0 &&
                   chargeAfter < MaxCharges &&
                   chargeAfter > chargeBefore;
        }

        private void TriggerProtectedRecovery(IceLotus lotusPumpkin, string source)
        {
            if (recoveryHandled)
                return;

            recoveryHandled = true;

            Plugin.Logger.LogInfo(
                "[LotusPumpkin] Five native charges reached via " + source + "."
            );

            HealProtectedPlant(lotusPumpkin);

            // The custom Pumpkin animator does not contain Snow Lotus's
            // recovery animation event, so explicitly finish the native
            // charge cycle after applying its redirected reward.
            lotusPumpkin.attributeCount = 0;
            lastObservedNativeCharge = 0;

            try
            {
                lotusPumpkin.UpdateText();
            }
            catch
            {
                // The counter is already reset; this call only refreshes UI.
            }
        }

        private static void RestoreLotusPumpkinHealth(IceLotus lotusPumpkin, int hp)
        {
            if (lotusPumpkin == null || lotusPumpkin.thePlantHealth <= hp)
                return;

            lotusPumpkin.thePlantHealth = hp;

            try
            {
                lotusPumpkin.LimHealth();
                lotusPumpkin.UpdateText();
            }
            catch
            {
                // HP restoration is already applied.
            }
        }

        private static Plant? FindProtectedPlant(IceLotus lotusPumpkin)
        {
            if (lotusPumpkin == null)
                return null;

            try
            {
                var plants = Lawnf.Get1x1Plants(
                    lotusPumpkin.thePlantColumn,
                    lotusPumpkin.thePlantRow
                );

                if (plants == null)
                    return null;

                // Best case: let the game tell us which plant is actually
                // protected by this exact Pumpkin shell.
                for (int i = 0; i < plants.Count; i++)
                {
                    Plant plant = plants[i];

                    if (!IsPossibleProtectedPlant(plant, lotusPumpkin))
                        continue;

                    try
                    {
                        if (plant.Pumpkin == lotusPumpkin)
                            return plant;
                    }
                    catch
                    {
                        // Some unusual plant classes can reject this lookup.
                        // The exact-tile fallback below still handles them.
                    }
                }

                // Fallback restricted to the exact tile. Never heal a plant
                // from a neighbouring row or column.
                for (int i = 0; i < plants.Count; i++)
                {
                    Plant plant = plants[i];

                    if (!IsPossibleProtectedPlant(plant, lotusPumpkin))
                        continue;

                    if (TypeMgr.IsPumpkin(plant.thePlantType))
                        continue;

                    if (TypeMgr.IsPot(plant.thePlantType))
                        continue;

                    if (TypeMgr.IsLily(plant.thePlantType))
                        continue;

                    return plant;
                }
            }
            catch (Exception e)
            {
                Plugin.Logger.LogWarning(
                    "[LotusPumpkin] Failed to find protected plant: " + e.Message
                );
            }

            return null;
        }

        private static bool IsPossibleProtectedPlant(Plant plant, IceLotus lotusPumpkin)
        {
            if (plant == null || lotusPumpkin == null)
                return false;

            if (plant.gameObject == lotusPumpkin.gameObject)
                return false;

            if (plant.thePlantColumn != lotusPumpkin.thePlantColumn)
                return false;

            if (plant.thePlantRow != lotusPumpkin.thePlantRow)
                return false;

            return !IsLotusPumpkin(plant);
        }

        private static void HealProtectedPlant(IceLotus lotusPumpkin)
        {
            Plant? target = FindProtectedPlant(lotusPumpkin);

            if (target is null || target == null)
            {
                Plugin.Logger.LogInfo(
                    "[LotusPumpkin] Five charges reached, but no protected plant was found."
                );
                return;
            }

            int oldHp = target.thePlantHealth;
            int maxHp = target.thePlantMaxHealth;

            if (oldHp >= maxHp)
            {
                Plugin.Logger.LogInfo(
                    "[LotusPumpkin] Protected plant is already at full health | " +
                    target.thePlantType +
                    " | " +
                    oldHp +
                    "/" +
                    maxHp
                );
                return;
            }

            int wantedHp = Mathf.Min(
                oldHp + Mathf.RoundToInt(ProtectedPlantHeal),
                maxHp
            );

            try
            {
                target.Recover(
                    wantedHp - oldHp,
                    DamageType.Normal,
                    true,
                    false
                );
            }
            catch (Exception e)
            {
                Plugin.Logger.LogWarning(
                    "[LotusPumpkin] Protected plant Recover() failed: " + e.Message
                );
            }

            // AnimRecover has already finished at this point. If a special
            // plant rejected or redirected Recover(), apply the requested
            // heal once, without the old ten-frame forced-heal loop.
            if (target.thePlantHealth < wantedHp)
                target.thePlantHealth = wantedHp;

            try
            {
                target.LimHealth();
                target.UpdateText();
            }
            catch
            {
                // Health is already correct; these calls only refresh limits/UI.
            }

            Plugin.Logger.LogInfo(
                "[LotusPumpkin] Protected plant healed | " +
                target.thePlantType +
                " | " +
                oldHp +
                " -> " +
                target.thePlantHealth +
                "/" +
                target.thePlantMaxHealth
            );
        }

        [HarmonyPatch(typeof(Plant), nameof(Plant.TakeDamage))]
        private static class Plant_TakeDamage_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Plant __instance, out int __state)
            {
                __state = IsLotusPumpkin(__instance)
                    ? __instance.thePlantHealth
                    : -1;
            }

            [HarmonyPostfix]
            private static void Postfix(Plant __instance, int __state)
            {
                if (__state < 0 || !IsLotusPumpkin(__instance))
                    return;

                // A charge is added only if this TakeDamage call really
                // removed health. Zero damage, blocked damage and healing do
                // not count.
                if (__instance.thePlantHealth >= __state)
                    return;

                // Do not start a recovery after a lethal hit.
                if (__instance.thePlantHealth <= 0 || __instance.dying)
                    return;

                IceLotus snowLotus = __instance.gameObject.GetComponent<IceLotus>();

                if (snowLotus == null)
                {
                    Plugin.Logger.LogWarning(
                        "[LotusPumpkin] Damage charge failed: IceLotus component missing."
                    );
                    return;
                }

                Plugin.Logger.LogInfo(
                    "[LotusPumpkin] Damage received | " +
                    __state +
                    " -> " +
                    __instance.thePlantHealth +
                    " | Adding one native Snow Lotus charge."
                );

                // This is the important part: damage uses the real Snow Lotus
                // charge method, so passive charges, damage charges and the
                // visible counter all share one native state.
                snowLotus.Charge();
            }
        }

        [HarmonyPatch(typeof(IceLotus), nameof(IceLotus.Charge))]
        private static class IceLotus_Charge_Patch
        {
            private struct ChargeState
            {
                public bool Track;
                public int ChargeBefore;
                public int HealthBefore;
            }

            [HarmonyPrefix]
            private static void Prefix(IceLotus __instance, out ChargeState __state)
            {
                __state = new ChargeState
                {
                    Track = IsLotusPumpkin(__instance),
                    ChargeBefore = __instance != null ? __instance.attributeCount : -1,
                    HealthBefore = __instance != null ? __instance.thePlantHealth : -1
                };
            }

            [HarmonyPostfix]
            private static void Postfix(IceLotus __instance, ChargeState __state)
            {
                if (!__state.Track || !IsLotusPumpkin(__instance))
                    return;

                int chargeAfter = __instance.attributeCount;

                Plugin.Logger.LogInfo(
                    "[LotusPumpkin] Native Charge() | " +
                    __state.ChargeBefore +
                    " -> " +
                    chargeAfter
                );

                // Depending on the exact game build, Snow Lotus either stays
                // at 5 until its recovery animation or immediately wraps its
                // counter back to 0. Support both native behaviours.
                bool reachedFive = ReachedFive(
                    __state.ChargeBefore,
                    chargeAfter
                );

                LotusPumpkin behaviour =
                    __instance.gameObject.GetComponent<LotusPumpkin>();

                if (behaviour == null)
                {
                    Plugin.Logger.LogWarning(
                        "[LotusPumpkin] Charge patch: LotusPumpkin component missing."
                    );
                    return;
                }

                if (reachedFive)
                {
                    // Some versions apply the self-heal directly inside
                    // Charge(); others wait for AnimRecover(). Block either.
                    RestoreLotusPumpkinHealth(
                        __instance,
                        __state.HealthBefore
                    );

                    behaviour.TriggerProtectedRecovery(
                        __instance,
                        "Charge() threshold"
                    );
                    return;
                }

                // The counter advanced inside a fresh cycle, so its future
                // fifth charge may trigger one new recovery.
                if (IsFreshCycleProgress(
                    __state.ChargeBefore,
                    chargeAfter
                ))
                {
                    behaviour.ResetRecoveryGuard();
                }
            }
        }

        [HarmonyPatch(typeof(IceLotus), nameof(IceLotus.AnimRecover))]
        private static class IceLotus_AnimRecover_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(IceLotus __instance, out int __state)
            {
                __state = IsLotusPumpkin(__instance)
                    ? __instance.thePlantHealth
                    : -1;
            }

            [HarmonyPostfix]
            private static void Postfix(IceLotus __instance, int __state)
            {
                if (__state < 0 || !IsLotusPumpkin(__instance))
                    return;

                // Keep the native charge reset and recovery animation, but
                // redirect the actual reward away from the Pumpkin itself.
                RestoreLotusPumpkinHealth(__instance, __state);

                Plugin.Logger.LogInfo(
                    "[LotusPumpkin] Native five-charge recovery triggered."
                );

                LotusPumpkin behaviour =
                    __instance.gameObject.GetComponent<LotusPumpkin>();

                if (behaviour == null)
                {
                    Plugin.Logger.LogWarning(
                        "[LotusPumpkin] AnimRecover patch: LotusPumpkin component missing."
                    );
                    return;
                }

                // Fallback for the original Snow Lotus animator. If Charge()
                // already handled this cycle, the guard prevents double heal.
                behaviour.TriggerProtectedRecovery(
                    __instance,
                    "AnimRecover()"
                );
            }
        }
    }
}
