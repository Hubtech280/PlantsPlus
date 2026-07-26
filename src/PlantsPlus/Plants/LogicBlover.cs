using CustomizeLib.BepInEx;
using HarmonyLib;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class LogicBloverBootstrap
    {
        private static bool registered;

        public static void OnStart()
        {
            if (registered)
                return;

            registered = true;

            try
            {
                AssetBundle? bundle = CustomCore.GetAssetBundle(
                    Assembly.GetExecutingAssembly(),
                    "PlantsPlus.Resources.AssetBundles.logicblover"
                );
                GameObject? prefab =
                    bundle?.GetAsset<GameObject>("LuckyBloverPrefab");
                GameObject? preview =
                    bundle?.GetAsset<GameObject>("LuckyBloverPreview");

                if (bundle == null || prefab == null || preview == null)
                    throw new InvalidOperationException(
                        "logicblover bundle, prefab or preview is missing."
                    );

                prefab.transform.localPosition = Vector3.zero;
                prefab.transform.localRotation = Quaternion.identity;

                V11PlantsBootstrap.IsolateAnimationClips(
                    bundle,
                    prefab,
                    "Logic Blover",
                    "idle",
                    "blow"
                );

                CustomCore.RegisterCustomPlant<
                    HurricaneBlover,
                    Plants.LogicBlover
                >(
                    Plants.LogicBlover.LogicBloverID,
                    prefab,
                    preview,
                    new List<(int, int)>(),
                    0f,
                    0f,
                    0,
                    Plants.LogicBlover.FallbackToughness,
                    Plants.LogicBlover.FallbackCardRecharge,
                    Plants.LogicBlover.FallbackCardCost
                );

                AlmanacEntry almanac = AlmanacContent.LogicBlover;
                CustomCore.AddPlantAlmanacStrings(
                    (PlantType)Plants.LogicBlover.LogicBloverID,
                    almanac.Name,
                    almanac.Info,
                    almanac.Introduce,
                    Plants.LogicBlover.FallbackCardCost
                );

                TypeMgr.RedPlant.Add(
                    (PlantType)Plants.LogicBlover.LogicBloverID
                );

                Plugin.Logger.LogInfo(
                    "[Logic Blover] Registered | ID = " +
                    Plants.LogicBlover.LogicBloverID +
                    " | Native behaviour = HurricaneBlover" +
                    " | Red Card = true" +
                    " | AnimStartBlow = bundle event at 0.500s"
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Logic Blover] Registration failed safely: " + exception
                );
            }
        }

        public static void OnGameInit()
        {
            if (CustomCore.CustomPlants.ContainsKey(
                (PlantType)Plants.LogicBlover.LogicBloverID
            ))
            {
                AlmanacCompatibility.RefreshLoadedData();
            }
        }

        public static void ApplyRandomEffects(Plant source)
        {
            if (source == null)
                return;

            var zombies = Lawnf.GetAllZombies(false);
            if (zombies == null)
                return;

            int affected = 0;
            for (int index = 0; index < zombies.Count; index++)
            {
                Zombie? zombie = zombies[index];
                if (zombie == null || zombie.gameObject == null)
                    continue;

                switch (UnityEngine.Random.Range(0, 4))
                {
                    case 0:
                        zombie.SetEmbered(false);
                        break;
                    case 1:
                        zombie.SetCold(10f, 0, false);
                        break;
                    case 2:
                        zombie.Buttered(4f, true);
                        break;
                    default:
                        zombie.SetPoison(10f);
                        break;
                }

                affected++;
            }

            Plugin.Logger.LogInfo(
                "[Logic Blover] Random effects resolved | Zombies = " +
                affected
            );
        }

        public static void AddGiftBoxLuck(PlantType usedType)
        {
            float increase;

            switch (usedType)
            {
                case PlantType.Blover:
                    increase = 5f;
                    break;
                case PlantType.LuckyBlover:
                    increase = 15f;
                    break;
                case PlantType.ImitateWheat:
                    increase = 16f;
                    break;
                default:
                    return;
            }

            var plants = Lawnf.GetAllPlants();
            if (plants == null)
                return;

            int boosted = 0;

            for (int index = 0; index < plants.Count; index++)
            {
                Plant? plant = plants[index];
                if (plant == null ||
                    (int)plant.thePlantType !=
                    Plants.LogicBlover.LogicBloverID)
                {
                    continue;
                }

                Plants.LogicBlover? logic =
                    plant.gameObject.GetComponent<Plants.LogicBlover>();

                if (logic == null)
                    continue;

                logic.AddGiftBoxLuck(increase);
                boosted++;
            }

            if (boosted > 0)
            {
                Plugin.Logger.LogInfo(
                    "[Logic Blover] Gift Box luck increased" +
                    " | Source = " + usedType +
                    " | Increase = " + increase + "%" +
                    " | Logic Blovers = " + boosted
                );
            }
        }

        public static float GetActiveGiftBoxLuck()
        {
            var plants = Lawnf.GetAllPlants();
            if (plants == null)
                return 0f;

            float highest = 0f;

            for (int index = 0; index < plants.Count; index++)
            {
                Plant? plant = plants[index];
                if (plant == null ||
                    (int)plant.thePlantType !=
                    Plants.LogicBlover.LogicBloverID)
                {
                    continue;
                }

                Plants.LogicBlover? logic =
                    plant.gameObject.GetComponent<Plants.LogicBlover>();

                if (logic != null)
                    highest = Mathf.Max(highest, logic.GiftBoxLuck);
            }

            return Mathf.Clamp(highest, 0f, 100f);
        }

        public static bool TryUseGoldBean(Mouse mouse)
        {
            if (mouse == null ||
                mouse.mouseItemType != MouseItemType.Bean)
            {
                return false;
            }

            Board board = Board.Instance;
            if (board == null)
                return false;

            var plants = Lawnf.GetAllPlants();
            if (plants == null)
                return false;

            Plant? target = null;

            for (int index = 0; index < plants.Count; index++)
            {
                Plant? plant = plants[index];
                if (plant == null ||
                    (int)plant.thePlantType !=
                    Plants.LogicBlover.LogicBloverID ||
                    plant.thePlantColumn != mouse.theMouseColumn ||
                    plant.thePlantRow != mouse.theMouseRow)
                {
                    continue;
                }

                target = plant;
                break;
            }

            if (target == null)
                return false;

            if (board.theMoney < Plants.LogicBlover.GoldBeanCost)
            {
                Plugin.Logger.LogInfo(
                    "[Logic Blover] Gold Bean rejected" +
                    " | Money = " + board.theMoney +
                    " | Required = " +
                    Plants.LogicBlover.GoldBeanCost
                );
                return true;
            }

            board.UseMoney(Plants.LogicBlover.GoldBeanCost);

            if (target.anim != null)
                target.anim.Play("blow", 0, 0f);
            else
                ApplyRandomEffects(target);

            mouse.ClearItemOnMouse(true);

            Plugin.Logger.LogInfo(
                "[Logic Blover] Gold Bean retriggered blow" +
                " | Cost = " + Plants.LogicBlover.GoldBeanCost +
                " | Remaining money = " + board.theMoney
            );
            return true;
        }
    }
}

namespace PlantsPlus.Plants
{
    using PlantsPlus.Core;

    public sealed class LogicBlover : MonoBehaviour
    {
        public const int LogicBloverID = 6015;
        public const int FallbackToughness = 300;
        public const float FallbackCardRecharge = 15f;
        public const int FallbackCardCost = 250;
        public const int GoldBeanCost = 10000;

        private float giftBoxLuck;

        public float GiftBoxLuck => giftBoxLuck;

        public LogicBlover(IntPtr pointer) : base(pointer) { }

        public void Start()
        {
            V11PlantsBootstrap.ApplyLocalAnimationController(
                gameObject,
                "Logic Blover"
            );
        }

        public void ResolveRandomEffects()
        {
            Plant? plant = gameObject.GetComponent<Plant>();
            if (plant != null)
                LogicBloverBootstrap.ApplyRandomEffects(plant);
        }

        public void AddGiftBoxLuck(float amount)
        {
            giftBoxLuck = Mathf.Clamp(
                giftBoxLuck + amount,
                0f,
                100f
            );
        }
    }

    [HarmonyPatch]
    internal static class LogicBloverPatches
    {
        [HarmonyPatch(typeof(HurricaneBlover), "DieEventMustExecute")]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool HurricaneBloverDieEventMustExecutePrefix(
            HurricaneBlover __instance
        )
        {
            if (__instance == null ||
                (int)__instance.thePlantType != LogicBlover.LogicBloverID)
            {
                return true;
            }

            // HurricaneBlover's native death hook dereferences its optional
            // particle SortingGroup. Logic Blover's supplied prefab has no
            // such particle, which throws and aborts Plant.Die before the
            // board/grid cleanup can finish. Skipping this cosmetic hook lets
            // the native Plant.Die routine complete normally.
            return false;
        }

        [HarmonyPatch(typeof(HurricaneBlover), "AnimStartBlow")]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void HurricaneBloverAnimStartBlowPostfix(
            HurricaneBlover __instance
        )
        {
            if (__instance == null ||
                (int)__instance.thePlantType != LogicBlover.LogicBloverID)
            {
                return;
            }

            LogicBlover? behaviour =
                __instance.gameObject.GetComponent<LogicBlover>();
            behaviour?.ResolveRandomEffects();
        }

        [HarmonyPatch(typeof(Lawnf), nameof(Lawnf.CheckPlantClass))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void CheckPlantClassPostfix(
            PlantType thePlantType,
            ref int __result
        )
        {
            if ((int)thePlantType == LogicBlover.LogicBloverID)
                __result = Lawnf.CheckPlantClass(PlantType.HurricaneBlover);
            else if (
                (int)thePlantType ==
                SolarSharpshooter.SolarSharpshooterID
            )
            {
                __result = Lawnf.CheckPlantClass(PlantType.SpruceShooter);
            }
        }

        [HarmonyPatch(typeof(CreatePlant), nameof(CreatePlant.SetPlant))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void CreatePlantSetPlantPostfix(
            PlantType theSeedType,
            Plant __result
        )
        {
            if (__result != null)
                LogicBloverBootstrap.AddGiftBoxLuck(theSeedType);
        }

        [HarmonyPatch(typeof(Present), "RandomPlant")]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void PresentRandomPlantPrefix()
        {
            float chance =
                LogicBloverBootstrap.GetActiveGiftBoxLuck();

            if (chance <= 0f ||
                UnityEngine.Random.Range(0f, 100f) >= chance)
            {
                return;
            }

            LuckyBlover.lucky = true;

            Plugin.Logger.LogInfo(
                "[Logic Blover] Gift Box luck roll succeeded" +
                " | Chance = " + chance + "%"
            );
        }

        [HarmonyPatch(typeof(Mouse), "LeftClickWithSomeThing")]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool MouseLeftClickWithSomeThingPrefix(
            Mouse __instance
        )
        {
            return !LogicBloverBootstrap.TryUseGoldBean(__instance);
        }
    }
}
