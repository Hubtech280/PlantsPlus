using CustomizeLib.MelonLoader;
using HarmonyLib;
using Il2Cpp;
using System;
using System.Reflection;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class ReapingCollisionBootstrap
    {
        private const int TrophyHunterBuffId = 46;
        private static bool registered;
        private static readonly MethodInfo? GetEnergyMethod =
            AccessTools.Method(
                typeof(UltimateJalapeno),
                "GetEnergy",
                new[] { typeof(int) }
            );

        internal static int BuffId { get; private set; } = -1;

        internal static void OnStart()
        {
            if (registered)
                return;

            try
            {
                BuffId = CustomCore.RegisterCustomBuff(
                    "<align=center><size=24>[Reaping Collision]" +
                    "</size></align>\n" +
                    "<align=left><size=20>When Grim Arbiter consumes a " +
                    "Cryolina Reaper, the Reaper survives, gains 1 soul " +
                    "charge, and triggers its death effect twice." +
                    "</size></align>",
                    BuffType.AdvancedBuff,
                    () => true,
                    0,
                    PlantType.DeathChomper,
                    1,
                    default
                );

                CustomCore.SetCustomBuffAlmanacType(
                    BuffType.AdvancedBuff,
                    BuffId,
                    AlmanacBuffType.General,
                    PlantType.DeathChomper
                );

                registered = true;
                Plugin.Logger.LogInfo(
                    "[Reaping Collision] Synergy modifier registered" +
                    " | Advanced buff ID = " + BuffId +
                    " | Grim Arbiter = 956" +
                    " | Cryolina Reaper = 977"
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Reaping Collision] Registration failed safely: " +
                    exception
                );
            }
        }

        internal static bool IsActive()
        {
            if (BuffId < 0)
                return false;

            try
            {
                return Lawnf.TravelAdvanced((AdvBuff)BuffId);
            }
            catch
            {
                return false;
            }
        }

        private static int GetSoulChargeCap()
        {
            try
            {
                return Lawnf.TravelUltimate((UltiBuff)TrophyHunterBuffId)
                    ? 10
                    : 3;
            }
            catch
            {
                return 3;
            }
        }

        private static void AddSoulCharge(
            UltimateJalapeno reaper,
            int chargeCap
        )
        {
            int previousCharges = reaper.count;

            try
            {
                if (GetEnergyMethod == null)
                    throw new MissingMethodException(
                        typeof(UltimateJalapeno).FullName,
                        "GetEnergy"
                    );

                GetEnergyMethod.Invoke(reaper, new object[] { chargeCap });
            }
            catch (Exception exception)
            {
                // Preserve the mechanic even if a future game build renames
                // the native visual-refresh method.
                if (reaper.count < chargeCap)
                    reaper.count++;

                Plugin.Logger.LogWarning(
                    "[Reaping Collision] Native charge refresh failed; " +
                    "used field fallback: " + exception.GetBaseException().Message
                );
            }

            Plugin.Logger.LogInfo(
                "[Reaping Collision] Soul charge granted" +
                " | Charges = " + previousCharges +
                " -> " + reaper.count +
                " | Cap = " + chargeCap
            );
        }

        [HarmonyPatch(typeof(DeathChomper), nameof(DeathChomper.KillPlants))]
        private static class DeathChomper_KillPlants_Patch
        {
            private static void Prefix(
                Il2CppSystem.Collections.Generic.List<Plant> plants
            )
            {
                if (plants == null || !IsActive())
                    return;

                for (int index = plants.Count - 1; index >= 0; index--)
                {
                    Plant plant = plants[index];
                    if (plant == null ||
                        plant.thePlantType != PlantType.UltimateJalapeno)
                    {
                        continue;
                    }

                    UltimateJalapeno? reaper =
                        plant.TryCast<UltimateJalapeno>();
                    if (reaper == null)
                        continue;

                    // Keep this Reaper out of Grim Arbiter's native destroy
                    // loop, while leaving every other consumed plant alone.
                    plants.RemoveAt(index);

                    int chargeCap = GetSoulChargeCap();
                    AddSoulCharge(reaper, chargeCap);

                    // Separate the two native effects so the game cannot
                    // merge their particles or ignore the second invocation.
                    reaper.OnDeath();
                    Plants.ReapingCollisionSecondEffect delayedEffect =
                        reaper.GetComponent<
                            Plants.ReapingCollisionSecondEffect
                        >();
                    if (delayedEffect == null)
                    {
                        delayedEffect = reaper.gameObject
                            .AddComponent<
                                Plants.ReapingCollisionSecondEffect
                            >();
                    }
                    delayedEffect.Arm(reaper);

                    Plugin.Logger.LogInfo(
                        "[Reaping Collision] Triggered" +
                        " | First death effect fired" +
                        " | Second effect queued"
                    );
                }
            }
        }
    }
}

namespace PlantsPlus.Plants
{
    public sealed class ReapingCollisionSecondEffect : MonoBehaviour
    {
        private const float EffectDelay = 0.2f;
        private UltimateJalapeno? reaper;
        private float countdown = -1f;
        private int pendingEffects;

        public ReapingCollisionSecondEffect(IntPtr pointer) : base(pointer) { }

        public void Arm(UltimateJalapeno source)
        {
            reaper = source;
            pendingEffects++;
            if (countdown < 0f)
                countdown = EffectDelay;
        }

        public void Update()
        {
            if (pendingEffects <= 0 || reaper == null || countdown < 0f)
                return;

            countdown -= Time.deltaTime;
            if (countdown > 0f)
                return;

            reaper.OnDeath();
            pendingEffects--;
            countdown = pendingEffects > 0 ? EffectDelay : -1f;

            Plugin.Logger.LogInfo(
                "[Reaping Collision] Second native death effect fired"
            );
        }
    }
}
