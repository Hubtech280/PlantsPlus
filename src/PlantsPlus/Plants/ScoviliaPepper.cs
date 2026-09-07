using CustomizeLib.MelonLoader;
using HarmonyLib;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class ScoviliaPepperBootstrap
    {
        private static bool registered;
        private static bool patchesInstalled;
        private static bool nativeDataLogged;

        internal static void OnStart()
        {
            if (registered)
                return;
            registered = true;
            InstallPatches();

            try
            {
                AssetBundle? bundle = CustomCore.GetAssetBundle(
                    Assembly.GetExecutingAssembly(),
                    "PlantsPlus.Resources.AssetBundles.scoviliapepper"
                );
                GameObject? prefab = bundle?.GetAsset<GameObject>("JalapenoPrefab");
                GameObject? preview = bundle?.GetAsset<GameObject>("JalapenoPreview");
                if (bundle == null || prefab == null || preview == null)
                    throw new InvalidOperationException(
                        "Scovilia Pepper bundle, prefab or preview is missing."
                    );

                CustomCore.RegisterCustomPlant<Jalapeno, Plants.ScoviliaPepper>(
                    Plants.ScoviliaPepper.ScoviliaPepperID,
                    prefab,
                    preview,
                    new List<(int, int)>
                    {
                        ((int)PlantType.Jalapeno, (int)PlantType.Jalapeno)
                    },
                    1f,
                    0f,
                    Plants.ScoviliaPepper.FallbackDamage,
                    Plants.ScoviliaPepper.FallbackToughness,
                    Plants.ScoviliaPepper.FallbackCardRecharge,
                    Plants.ScoviliaPepper.FallbackCardCost
                );

                AlmanacEntry almanac = AlmanacContent.ScoviliaPepper;
                CustomCore.AddPlantAlmanacStrings(
                    (PlantType)Plants.ScoviliaPepper.ScoviliaPepperID,
                    almanac.Name,
                    almanac.Info,
                    almanac.Introduce,
                    Plants.ScoviliaPepper.FallbackCardCost
                );
                Plugin.Logger.LogInfo(
                    "[Scovilia Pepper] Registered | Fusion = Jalapeno + Jalapeno"
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Scovilia Pepper] Registration failed safely: " + exception
                );
            }
        }

        private static void InstallPatches()
        {
            if (patchesInstalled)
                return;
            patchesInstalled = true;

            HarmonyLib.Harmony harmony =
                new HarmonyLib.Harmony("PlantsPlus.ScoviliaPepper");
            harmony.Patch(
                AccessTools.Method(typeof(Jalapeno), nameof(Jalapeno.AnimExplode)),
                prefix: new HarmonyMethod(
                    AccessTools.Method(typeof(ScoviliaPepperBootstrap),
                        nameof(JalapenoAnimExplodePrefix))
                )
            );
            harmony.Patch(
                AccessTools.Method(typeof(GameAPP), nameof(GameAPP.LoadResources)),
                postfix: new HarmonyMethod(
                    AccessTools.Method(typeof(ScoviliaPepperBootstrap),
                        nameof(GameAppLoadResourcesPostfix))
                )
            );
        }

        internal static void OnGameInit()
        {
            RefreshNativeData();
            AlmanacCompatibility.RefreshLoadedData();
        }

        private static void RefreshNativeData()
        {
            try
            {
                PlantType type = (PlantType)Plants.ScoviliaPepper.ScoviliaPepperID;
                if (!CustomCore.CustomPlants.TryGetValue(type, out var customData))
                    return;
                PlantDataManager.PlantData native =
                    PlantDataManager.GetPlantData(PlantType.Jalapeno);
                if (native == null || customData.PlantData == null)
                    return;

                PlantDataManager.PlantData target = customData.PlantData;
                target.thePlantType = type;
                target.attackDamage = native.attackDamage;
                target.maxHealth = native.maxHealth;
                target.cd = native.cd;
                target.cost = native.cost;
                customData.PlantData = target;
                CustomCore.CustomPlants[type] = customData;

                if (!nativeDataLogged)
                {
                    nativeDataLogged = true;
                    Plugin.Logger.LogInfo(
                        "[Scovilia Pepper] Native Jalapeno data mirrored" +
                        " | Damage = " + native.attackDamage +
                        " | Cost = " + native.cost
                    );
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Scovilia Pepper] Native data was not ready: " +
                    exception.Message
                );
            }
        }

        private static void JalapenoAnimExplodePrefix(Jalapeno __instance)
        {
            if (__instance == null ||
                (int)__instance.thePlantType !=
                    Plants.ScoviliaPepper.ScoviliaPepperID)
                return;

            BoardAction? action = Board.Instance?.boardAction;
            if (action == null)
                return;

            int damage = __instance.attackDamage > 0
                ? __instance.attackDamage
                : Plants.ScoviliaPepper.FallbackDamage;
            int lastRow = Board.Instance != null
                ? Board.Instance.rowNum - 1
                : 4;

            for (int offset = -1; offset <= 1; offset += 2)
            {
                int row = __instance.thePlantRow + offset;
                if (row < 0 || row > lastRow)
                    continue;
                action.CreateFireLine(
                    row,
                    damage,
                    false,
                    false,
                    true,
                    null,
                    (PlantType)Plants.ScoviliaPepper.ScoviliaPepperID
                );
            }
        }

        private static void GameAppLoadResourcesPostfix() => RefreshNativeData();
    }
}

namespace PlantsPlus.Plants
{
    public sealed class ScoviliaPepper : MonoBehaviour
    {
        public const int ScoviliaPepperID = 6027;
        public const int FallbackDamage = 1800;
        public const int FallbackToughness = 300;
        public const int FallbackCardCost = 250;
        public const float FallbackCardRecharge = 50f;

        public ScoviliaPepper(IntPtr pointer) : base(pointer) { }
    }
}
