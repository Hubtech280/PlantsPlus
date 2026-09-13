using CustomizeLib.MelonLoader;
using Il2Cpp;
using System;
using System.Reflection;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class ExcavatorObsidianSpikeSkinBootstrap
    {
        private const string BundleResource =
            "PlantsPlus.Resources.AssetBundles.excavatorobsidianspikeskin";
        private const string PrefabName = "ObsidianWallNutPrefab";
        private const string PreviewName = "ObsidianWallNutPreview";
        private static bool registered;
        private static GameObject? skinPrefab;
        private static GameObject? skinPreview;

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
                if (bundle == null ||
                    skinPrefab == null ||
                    skinPreview == null)
                {
                    throw new InvalidOperationException(
                        "Excavator Obsidian Spike prefab or preview is missing."
                    );
                }

                SpriteRenderer previewRenderer =
                    skinPreview.GetComponent<SpriteRenderer>();
                if (previewRenderer == null || previewRenderer.sprite == null)
                {
                    throw new InvalidOperationException(
                        "Excavator Obsidian Spike preview sprite is missing."
                    );
                }

                CustomCore.RegisterCustomPlantSkin<
                    ObsidianWallNut,
                    Plants.ExcavatorObsidianSpikeSkinRuntime
                >(
                    (int)PlantType.ObsidianWallNut,
                    skinPrefab,
                    skinPreview,
                    plant => plant.thePlantType = PlantType.ObsidianWallNut,
                    null
                );

                registered = true;
                Plugin.Logger.LogInfo(
                    "[Skin] Excavator Obsidian Spike queued for native " +
                    "Obsidian Spike-nut skin selection."
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Skin] Excavator Obsidian Spike registration failed: " +
                    exception
                );
            }
        }

        internal static void OnGameInit()
        {
            try
            {
                var resources = GameAPP.resourcesManager;
                PlantType target = PlantType.ObsidianWallNut;
                if (resources == null ||
                    !resources._plantPrefabs.ContainsKey(target) ||
                    !resources._plantPreviews.ContainsKey(target))
                {
                    Plugin.Logger.LogWarning(
                        "[Skin] Excavator Obsidian Spike is waiting for " +
                        "the native skin lists."
                    );
                    return;
                }

                int prefabIndex = skinPrefab == null
                    ? -1
                    : resources._plantPrefabs[target].IndexOf(skinPrefab);
                var previews = resources._plantPreviews[target];
                int previewIndex = FindPreviewIndex(previews);

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
                        CustomCore.CustomPlantSkinIndex[target]
                            .Add(prefabIndex);
                    }
                }

                Plugin.Logger.LogInfo(
                    "[Skin] Excavator Obsidian Spike native registration " +
                    "verified | Prefab index = " + prefabIndex +
                    " | Preview index = " + previewIndex +
                    " | Skin count = " +
                    resources._plantPrefabs[target].Count +
                    " | Preview count = " + previews.Count
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Skin] Excavator Obsidian Spike verification failed: " +
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
}

namespace PlantsPlus.Plants
{
    public sealed class ExcavatorObsidianSpikeSkinRuntime : MonoBehaviour
    {
        private ObsidianWallNut? plant;
        private GameObject? healthy;
        private GameObject? cracked;
        private GameObject? critical;
        private int visibleState = -1;

        public ExcavatorObsidianSpikeSkinRuntime(IntPtr pointer)
            : base(pointer)
        {
        }

        public void Start()
        {
            plant = GetComponent<ObsidianWallNut>();
            healthy = transform.Find("Wallnut_body")?.gameObject;
            cracked = transform.Find("Wallnut_cracked1")?.gameObject;
            critical = transform.Find("Wallnut_cracked2")?.gameObject;
            RefreshDamageVisual(true);
        }

        public void Update()
        {
            RefreshDamageVisual(false);
        }

        private void RefreshDamageVisual(bool force)
        {
            if (plant == null || plant.thePlantMaxHealth <= 0)
                return;

            float healthRatio = Mathf.Clamp01(
                (float)plant.thePlantHealth / plant.thePlantMaxHealth
            );
            int nextState = healthRatio > 0.66f
                ? 0
                : healthRatio > 0.33f
                    ? 1
                    : 2;
            if (!force && nextState == visibleState)
                return;

            visibleState = nextState;
            healthy?.SetActive(nextState == 0);
            cracked?.SetActive(nextState == 1);
            critical?.SetActive(nextState == 2);
        }
    }
}
