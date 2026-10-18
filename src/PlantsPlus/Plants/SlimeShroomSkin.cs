using CustomizeLib.MelonLoader;
using HarmonyLib;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class SlimeShroomSkinBootstrap
    {
        private const string BundleResource =
            "PlantsPlus.Resources.AssetBundles.slime_shroom";
        private const string PrefabName = "CornFumePrefab";
        internal const string PlantedPrefabName = "SlimeShroomSkinPrefab";
        private const string PreviewName = "CornFumePreview";
        private static bool registered;
        private static GameObject? skinPrefab;
        private static GameObject? skinPreview;
        internal static GameObject? SkinPrefab => skinPrefab;

        internal static void OnStart()
        {
            if (registered)
                return;

            try
            {
                var bundle = CustomCore.GetAssetBundle(
                    Assembly.GetExecutingAssembly(),
                    BundleResource
                );
                skinPrefab = bundle?.GetAsset<GameObject>(PrefabName);
                skinPreview = bundle?.GetAsset<GameObject>(PreviewName);
                if (bundle == null || skinPrefab == null || skinPreview == null)
                {
                    throw new InvalidOperationException(
                        "Slime-shroom prefab or preview is missing."
                    );
                }

                SpriteRenderer previewRenderer =
                    skinPreview.GetComponent<SpriteRenderer>();
                if (previewRenderer == null || previewRenderer.sprite == null)
                {
                    throw new InvalidOperationException(
                        "Slime-shroom preview sprite is missing."
                    );
                }

                registered = true;
                Plugin.Logger.LogInfo(
                    "[Skin] Slime-shroom bundle loaded; native " +
                    "Bubbler-shroom registration deferred until GameInit."
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Skin] Slime-shroom registration failed: " + exception
                );
            }
        }

        internal static void OnGameInit()
        {
            try
            {
                var resources = GameAPP.resourcesManager;
                PlantType target = PlantType.CornFume;
                if (resources == null ||
                    !resources._plantPrefabs.ContainsKey(target) ||
                    !resources._plantPreviews.ContainsKey(target))
                {
                    Plugin.Logger.LogWarning(
                        "[Skin] Slime-shroom is waiting for the native skin lists."
                    );
                    return;
                }

                if (skinPrefab == null || skinPreview == null)
                    return;

                var prefabs = resources._plantPrefabs[target];
                var previews = resources._plantPreviews[target];
                int prefabIndex = prefabs.IndexOf(skinPrefab);

                if (prefabIndex < 0)
                {
                    if (prefabs.Count == 0 || prefabs[0] == null)
                    {
                        throw new InvalidOperationException(
                            "Native CornFume prefab is unavailable."
                        );
                    }

                    Component nativePlant =
                        prefabs[0].GetComponent<Plant>();
                    if (nativePlant == null)
                    {
                        throw new InvalidOperationException(
                            "Native CornFume component is unavailable."
                        );
                    }

                    // Do not call RegisterCustomPlantSkin<CornFume>: its
                    // generic AddComponent path crashes the CoreCLR hook in
                    // MelonLoader 0.7.3. Use Unity's native Type overload.
                    // Keep the resource prefab inactive while attaching the
                    // native behaviour. Otherwise CornFume.Awake runs once
                    // against no board and that broken state is cloned into
                    // every planted Slime-shroom.
                    skinPrefab.SetActive(false);
                    Component added = skinPrefab.AddComponent(
                        nativePlant.GetIl2CppType()
                    );
                    Plant skinPlant = added.TryCast<Plant>();
                    if (skinPlant == null)
                    {
                        throw new InvalidOperationException(
                            "CornFume component could not be attached."
                        );
                    }

                    skinPlant.thePlantType = target;
                    skinPlant.zombieList =
                        new Il2CppSystem.Collections.Generic.List<Zombie>();
                    // A distinct name lets the planting hook identify only
                    // clones of this inactive skin, never native CornFume.
                    skinPrefab.name = PlantedPrefabName;
                    skinPrefab.tag = "Plant";
                    skinPreview.tag = "Preview";
                    prefabs.Add(skinPrefab);
                    previews.Add(skinPreview);
                    prefabIndex = prefabs.Count - 1;
                }

                int previewIndex = FindPreviewIndex(previews);

                if (!CustomCore.CustomPlantsSkin.ContainsKey(target))
                {
                    CustomCore.CustomPlantsSkin.Add(
                        target,
                        new List<CustomPlantData>()
                    );
                }

                List<CustomPlantData> skinData =
                    CustomCore.CustomPlantsSkin[target];
                bool dataRegistered = false;
                for (int index = 0; index < skinData.Count; index++)
                {
                    if (skinData[index].Prefab == skinPrefab)
                    {
                        dataRegistered = true;
                        break;
                    }
                }
                if (!dataRegistered)
                {
                    skinData.Add(new CustomPlantData
                    {
                        ID = (int)target,
                        Prefab = skinPrefab,
                        Preview = skinPreview,
                        PlantData = null,
                        BulletList = null
                    });
                }
                CustomCore.CustomPlantsSkinActive[target] = false;

                if (prefabIndex >= 0)
                {
                    if (!CustomCore.CustomPlantSkinIndex.ContainsKey(target))
                    {
                        CustomCore.CustomPlantSkinIndex.Add(
                            target,
                            new System.Collections.Generic.List<int>()
                        );
                    }
                    if (!CustomCore.CustomPlantSkinIndex[target]
                            .Contains(prefabIndex))
                    {
                        CustomCore.CustomPlantSkinIndex[target].Add(prefabIndex);
                    }
                }

                Plugin.Logger.LogInfo(
                    "[Skin] Slime-shroom native registration verified" +
                    " | Target = Bubbler-shroom/CornFume" +
                    " | Prefab index = " + prefabIndex +
                    " | Preview index = " + previewIndex +
                    " | Skin count = " + prefabs.Count +
                    " | Preview count = " + previews.Count
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Skin] Slime-shroom verification failed: " +
                    exception.Message
                );
            }
        }

        private static int FindPreviewIndex(
            Il2CppSystem.Collections.Generic.List<GameObject> previews
        )
        {
            if (skinPreview != null)
            {
                int exactIndex = previews.IndexOf(skinPreview);
                if (exactIndex >= 0)
                    return exactIndex;
            }

            for (int index = 0; index < previews.Count; index++)
            {
                GameObject candidate = previews[index];
                if (candidate != null && candidate.name == PreviewName)
                {
                    skinPreview = candidate;
                    return index;
                }
            }
            return -1;
        }
    }

    // The resource must stay inactive so CornFume.Awake does not run before a
    // board exists. Instantiate inherits that inactive state, so wake only the
    // actual planted clone once SetPlant has put it on the board.
    [HarmonyPatch(typeof(CreatePlant), nameof(CreatePlant.SetPlant))]
    internal static class SlimeShroomPlantedSkinPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(PlantType theSeedType, Plant __result)
        {
            if (theSeedType != PlantType.CornFume || __result == null)
                return;

            GameObject planted = __result.gameObject;
            if (planted == null || planted.activeSelf ||
                !planted.name.StartsWith(
                    SlimeShroomSkinBootstrap.PlantedPrefabName,
                    StringComparison.Ordinal))
                return;

            planted.SetActive(true);
            Plugin.Logger.LogInfo(
                "[Skin] Activated planted Slime-shroom clone.");
        }
    }

    // The Slime skin prefab is deliberately inactive until it is planted:
    // activating the resource itself runs CornFume.Awake without a board.
    // The Almanac's SetPlantInAlmamac call assumes the chosen prefab was
    // initialised, however, and throws when it clones that inactive skin.
    // Show the safe native CornFume model for this one Almanac call only;
    // keep the selected Slime skin unchanged for actual gameplay.
    [HarmonyPatch(typeof(AlmanacPlantWindow), "SetPlant")]
    internal static class SlimeShroomAlmanacPreviewPatch
    {
        [HarmonyPrefix]
        private static void Prefix(
            PlantType thePlantType, out GameObject? __state)
        {
            __state = null;
            if (thePlantType != PlantType.CornFume)
                return;

            var resources = GameAPP.resourcesManager;
            PlantType target = PlantType.CornFume;
            if (resources == null ||
                !resources.plantPrefabs.ContainsKey(target) ||
                !resources._plantPrefabs.ContainsKey(target) ||
                resources._plantPrefabs[target].Count == 0)
                return;

            GameObject selected = resources.plantPrefabs[target];
            GameObject native = resources._plantPrefabs[target][0];
            if (selected == null || native == null ||
                selected == native || selected != SlimeShroomSkinBootstrap.SkinPrefab)
                return;

            __state = selected;
            resources.plantPrefabs[target] = native;
            Plugin.Logger.LogInfo(
                "[Skin] Slime-shroom Almanac preview uses the native " +
                "CornFume model to avoid an inactive-prefab error.");
        }

        [HarmonyFinalizer]
        private static Exception? Finalizer(
            Exception? __exception, GameObject? __state)
        {
            if (__state != null)
            {
                var resources = GameAPP.resourcesManager;
                if (resources != null)
                    resources.plantPrefabs[PlantType.CornFume] = __state;
            }

            return __exception;
        }
    }

    // A later Almanac skin hook can hide the selector even when the game's
    // native skin lists contain alternatives. Restore it only for plants with
    // a valid skin index and matching prefab/preview entries.
    [HarmonyPatch(typeof(AlmanacPlantWindow), "SetPlant")]
    internal static class AlmanacNativeSkinButtonPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(
            AlmanacPlantWindow __instance, PlantType thePlantType)
        {
            var resources = GameAPP.resourcesManager;
            if (__instance == null || __instance.skinButton == null ||
                resources == null ||
                !resources._plantPrefabs.ContainsKey(thePlantType) ||
                !resources._plantPreviews.ContainsKey(thePlantType) ||
                resources._plantPrefabs[thePlantType].Count < 2 ||
                resources._plantPreviews[thePlantType].Count < 2 ||
                resources._plantPrefabs[thePlantType].Count !=
                    resources._plantPreviews[thePlantType].Count)
                return;

            if (!resources.plantSkinDic.ContainsKey(thePlantType))
            {
                int selectedIndex = 0;
                if (resources.plantPrefabs.ContainsKey(thePlantType))
                {
                    int found = resources._plantPrefabs[thePlantType]
                        .IndexOf(resources.plantPrefabs[thePlantType]);
                    if (found >= 0)
                        selectedIndex = found;
                }

                resources.plantSkinDic.Add(thePlantType, selectedIndex);
                Plugin.Logger.LogInfo(
                    "[Skin] Restored missing skin index for " +
                    thePlantType + " | Selected = " + selectedIndex + ".");
            }

            if (!__instance.skinButton.activeSelf)
            {
                __instance.skinButton.SetActive(true);
                Plugin.Logger.LogInfo(
                    "[Skin] Restored native Almanac selector for " +
                    thePlantType + ".");
            }
        }
    }
}
