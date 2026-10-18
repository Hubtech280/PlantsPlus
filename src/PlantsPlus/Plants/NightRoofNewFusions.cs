using CustomizeLib.MelonLoader;
using HarmonyLib;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class NightRoofNewFusions
    {
        internal static void OnStart()
        {
            RegisterSuperaqua();
            RegisterPulse();
            RegisterSolstice();
        }

        internal static void OnGameInit()
        {
            InstallSuperaquaTypeFlags();
            InstallPulseTypeFlags();
            InstallSolsticeTypeFlags();
            EnsureRecipes();
            RefreshSuperaquaPlantData();
            AlmanacCompatibility.RefreshLoadedData();
        }

        private static void RegisterSuperaqua()
        {
            try
            {
                AssetBundle? bundle = CustomCore.GetAssetBundle(
                    Assembly.GetExecutingAssembly(),
                    "PlantsPlus.Resources.AssetBundles.superaqua_shroom");
                GameObject? prefab = bundle?.GetAsset<GameObject>(
                    "WaterSwordshroomPrefab");
                GameObject? preview = bundle?.GetAsset<GameObject>(
                    "WaterSwordshroomPreview");
                if (prefab == null || preview == null)
                    throw new InvalidOperationException(
                        "WaterSwordshroom prefab or preview is missing.");

                prefab.transform.localPosition = Vector3.zero;
                V11PlantsBootstrap.IsolateAnimationClips(bundle!, prefab,
                    "Superaqua-shroom", "idle", "attack", "attack2");

                CustomCore.RegisterCustomPlant<
                    WaterSwordshroom, Plants.SuperaquaShroom>(
                    Plants.SuperaquaShroom.ID, prefab, preview,
                    new List<(int, int)>
                    {
                        ((int)PlantType.SuperSpruce,
                            (int)PlantType.Swordshroom),
                        ((int)PlantType.Swordshroom,
                            (int)PlantType.SuperSpruce)
                    },
                    1.5f, 0f, 40, 500, 30f, 350);

                PlantType type = (PlantType)Plants.SuperaquaShroom.ID;
                InstallSuperaquaTypeFlags();
                AlmanacEntry entry = AlmanacContent.SuperaquaShroom;
                CustomCore.AddPlantAlmanacStrings(type, entry.Name,
                    entry.Info, entry.Introduce, 350);
                Plugin.Logger.LogInfo(
                    "[Superaqua-shroom] Registered | " +
                    "SuperSpruce + Swordshroom | ID=6038");
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Superaqua-shroom] Registration failed: " + exception);
            }
        }

        private static void RegisterPulse()
        {
            try
            {
                AssetBundle? bundle = CustomCore.GetAssetBundle(
                    Assembly.GetExecutingAssembly(),
                    "PlantsPlus.Resources.AssetBundles.pulse_shroom");
                GameObject? prefab = bundle?.GetAsset<GameObject>(
                    "CaltropPrefab");
                GameObject? preview = bundle?.GetAsset<GameObject>(
                    "CaltropPreview");
                if (prefab == null || preview == null)
                    throw new InvalidOperationException(
                        "Caltrop prefab or preview is missing.");

                prefab.transform.localPosition = Vector3.zero;
                V11PlantsBootstrap.IsolateAnimationClips(bundle!, prefab,
                    "Pulse-shroom", "idle", "attack", "rest",
                    "rest-idle", "wake-up");
                BorealOrchidBootstrap.AttachWaveVisual(prefab);

                CustomCore.RegisterCustomPlant<
                    Caltrop, Plants.PulseShroom>(
                    Plants.PulseShroom.ID, prefab, preview,
                    new List<(int, int)>
                    {
                        (Plants.BounceShroom.ID,
                            Plants.BorealOrchid.BorealOrchidID),
                        (Plants.BorealOrchid.BorealOrchidID,
                            Plants.BounceShroom.ID)
                    },
                    0f, 0f, 0, 450, 15f, 175);

                PlantType type = (PlantType)Plants.PulseShroom.ID;
                InstallPulseTypeFlags();
                AlmanacEntry entry = AlmanacContent.PulseShroom;
                CustomCore.AddPlantAlmanacStrings(type, entry.Name,
                    entry.Info, entry.Introduce, 175);
                Plugin.Logger.LogInfo(
                    "[Pulse-shroom] Registered | " +
                    "Bounce-shroom + Boreal Orchid | ID=6039");
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Pulse-shroom] Registration failed: " + exception);
            }
        }

        private static void RegisterSolstice()
        {
            try
            {
                var bundle = CustomCore.GetAssetBundle(Assembly.GetExecutingAssembly(),
                    "PlantsPlus.Resources.AssetBundles.solstice_shroom");
                var prefab = bundle?.GetAsset<GameObject>("CaltropPrefab");
                var preview = bundle?.GetAsset<GameObject>("CaltropPreview");
                if (bundle == null || prefab == null || preview == null)
                    throw new InvalidOperationException("Solstice-shroom prefab or preview missing.");
                prefab.transform.localPosition = Vector3.zero;
                V11PlantsBootstrap.IsolateAnimationClips(bundle, prefab, "Solstice-shroom",
                    "idle", "attack", "rest", "rest-idle", "wake-up");
                CustomCore.RegisterCustomPlant<Caltrop, Plants.SolsticeShroom>(
                    Plants.SolsticeShroom.ID, prefab, preview, new List<(int, int)> {
                        (Plants.BounceShroom.ID, (int)PlantType.SunFlower),
                        ((int)PlantType.SunFlower, Plants.BounceShroom.ID)
                    }, 0f, 0f, 0, 300, 15f, 125);
                InstallSolsticeTypeFlags();
                var entry = AlmanacContent.SolsticeShroom;
                CustomCore.AddPlantAlmanacStrings((PlantType)Plants.SolsticeShroom.ID,
                    entry.Name, entry.Info, entry.Introduce, 125);
                Plugin.Logger.LogInfo("[Solstice-shroom] Registered | ID=6041 | 25 Sun per bounced zombie.");
            }
            catch (Exception error) { Plugin.Logger.LogError("[Solstice-shroom] Registration: " + error); }
        }

        private static void InstallSolsticeTypeFlags()
        {
            PlantType type = (PlantType)Plants.SolsticeShroom.ID;
            if (CustomCore.TypeMgrExtra.IsCaltrop != null && !CustomCore.TypeMgrExtra.IsCaltrop.Contains(type))
                CustomCore.TypeMgrExtra.IsCaltrop.Add(type);
            CoreEnums.baiscPlants?.Remove(type);
            TypeData.SpecialCardPlants?.Remove(type);
        }

        private static void InstallSuperaquaTypeFlags()
        {
            PlantType type = (PlantType)Plants.SuperaquaShroom.ID;
            var icePlants = CustomCore.TypeMgrExtra.IsIcePlant;
            if (icePlants != null && !icePlants.Contains(type))
                icePlants.Add(type);
            CoreEnums.baiscPlants?.Remove(type);
            TypeData.SpecialCardPlants?.Remove(type);
        }

        private static void InstallPulseTypeFlags()
        {
            PlantType type = (PlantType)Plants.PulseShroom.ID;
            var caltrops = CustomCore.TypeMgrExtra.IsCaltrop;
            if (caltrops != null && !caltrops.Contains(type))
                caltrops.Add(type);
            CoreEnums.baiscPlants?.Remove(type);
            TypeData.SpecialCardPlants?.Remove(type);
        }

        private static void RefreshSuperaquaPlantData()
        {
            try
            {
                PlantType type = (PlantType)Plants.SuperaquaShroom.ID;
                if (!CustomCore.CustomPlants.ContainsKey(type))
                    return;
                var custom = CustomCore.CustomPlants[type];
                PlantDataManager.PlantData native =
                    PlantDataManager.GetPlantData(
                        PlantType.WaterSwordshroom);
                if (custom.PlantData == null || native == null)
                    return;
                custom.PlantData.attackInterval = native.attackInterval;
                custom.PlantData.attackDamage = native.attackDamage;
                custom.PlantData.maxHealth = native.maxHealth;
                custom.PlantData.cd = native.cd;
                custom.PlantData.cost = native.cost;
                CustomCore.CustomPlants[type] = custom;
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Superaqua-shroom] Native stat mirror skipped: " +
                    exception.Message);
            }
        }

        internal static void EnsureRecipes()
        {
            EnsureRecipe(PlantType.SuperSpruce, PlantType.Swordshroom,
                (PlantType)Plants.SuperaquaShroom.ID);
            EnsureRecipe((PlantType)Plants.BounceShroom.ID,
                (PlantType)Plants.BorealOrchid.BorealOrchidID,
                (PlantType)Plants.PulseShroom.ID);
            EnsureRecipe((PlantType)Plants.BounceShroom.ID, PlantType.SunFlower,
                (PlantType)Plants.SolsticeShroom.ID);
        }

        private static void EnsureRecipe(
            PlantType left, PlantType right, PlantType result)
        {
            try
            {
                PlantType existing;
                if (!MixData.TryGetMix(left, right, out existing, true) ||
                    existing != result)
                    MixData.AddOrderedRecipe(left, right, result);

                if (!PlantMixTreeManager.IsInitialized)
                    return;
                var trees = PlantMixTreeManager.PlantMixTrees;
                var parentsByChild = PlantMixTreeManager.ChildToParents;
                if (trees == null || parentsByChild == null)
                    return;

                PlantMixTreeNode a = GetOrAdd(trees, left);
                PlantMixTreeNode b = GetOrAdd(trees, right);
                PlantMixTreeNode child = GetOrAdd(trees, result);
                a.Recipes[right] = result;
                b.Recipes[left] = result;
                AddOnce(a.DirectChildren, child);
                AddOnce(b.DirectChildren, child);
                AddOnce(a.AllDescendants, child);
                AddOnce(b.AllDescendants, child);

                Il2CppSystem.Collections.Generic.List<
                    PlantMixTreeManager.MixParentInfo> parents;
                if (!parentsByChild.TryGetValue(result, out parents))
                {
                    parents = new Il2CppSystem.Collections.Generic.List<
                        PlantMixTreeManager.MixParentInfo>();
                    parentsByChild.Add(result, parents);
                }
                for (int i = 0; i < parents.Count; i++)
                {
                    var item = parents[i];
                    if (item.Result == result &&
                        ((item.ParentA == left && item.ParentB == right) ||
                         (item.ParentA == right && item.ParentB == left)))
                        return;
                }
                parents.Add(new PlantMixTreeManager.MixParentInfo(
                    left, right, result));
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Night Roof fusions] Recipe repair skipped: " +
                    exception.Message);
            }
        }

        private static PlantMixTreeNode GetOrAdd(
            Il2CppSystem.Collections.Generic.Dictionary<
                PlantType, PlantMixTreeNode> trees, PlantType type)
        {
            PlantMixTreeNode node;
            if (!trees.TryGetValue(type, out node))
            {
                node = new PlantMixTreeNode(type);
                trees.Add(type, node);
            }
            return node;
        }

        private static void AddOnce(
            Il2CppSystem.Collections.Generic.List<PlantMixTreeNode> list,
            PlantMixTreeNode node)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null &&
                    list[i].PlantType == node.PlantType)
                    return;
            list.Add(node);
        }
    }

    [HarmonyPatch(typeof(MixData), nameof(MixData.InitMixData))]
    internal static class NightRoofNewMixPatch
    {
        [HarmonyPostfix]
        private static void Postfix() =>
            NightRoofNewFusions.EnsureRecipes();
    }

    [HarmonyPatch(typeof(PlantMixTreeManager), "Init")]
    internal static class NightRoofNewTreePatch
    {
        [HarmonyPostfix]
        private static void Postfix() =>
            NightRoofNewFusions.EnsureRecipes();
    }

    [HarmonyPatch(typeof(MixData), nameof(MixData.TryGetMix))]
    internal static class SuperaquaConversionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(PlantType a, PlantType b,
            ref PlantType c, ref bool __result)
        {
            PlantType aqua = (PlantType)Plants.SuperaquaShroom.ID;
            if (!((a == aqua && b == PlantType.SpruceShooter) ||
                  (b == aqua && a == PlantType.SpruceShooter)))
                return true;
            c = PlantType.SuperSpruce;
            __result = true;
            return false;
        }
    }
}
