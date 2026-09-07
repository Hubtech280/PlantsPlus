using CustomizeLib.MelonLoader;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class LobShroomBootstrap
    {
        private static bool registered;

        internal static void OnStart()
        {
            if (registered)
                return;

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var bundle = CustomCore.GetAssetBundle(
                    assembly,
                    "PlantsPlus.Resources.AssetBundles.lob_shroom"
                );
                var prefab = bundle?.GetAsset<GameObject>("SmallPuffPrefab");
                var preview = bundle?.GetAsset<GameObject>("SmallPuffPreview");

                if (bundle == null || prefab == null || preview == null)
                {
                    throw new InvalidOperationException(
                        "Lob-shroom plant or preview prefab missing."
                    );
                }

                V11PlantsBootstrap.IsolateAnimationClips(
                    bundle,
                    prefab,
                    "Lob-shroom",
                    "idle",
                    "shoot"
                );

                PlantType type = (PlantType)Plants.LobShroom.ID;

                if (!CustomCore.TypeMgrExtra.IsPuff.Contains(type))
                    CustomCore.TypeMgrExtra.IsPuff.Add(type);

                CustomCore.RegisterCustomPlant<Cabbage, Plants.LobShroom>(
                    Plants.LobShroom.ID,
                    prefab,
                    preview,
                    new List<(int, int)>(),
                    1.5f,
                    0f,
                    20,
                    300,
                    7.5f,
                    0
                );

                var entry = AlmanacContent.LobShroom;
                CustomCore.AddPlantAlmanacStrings(
                    type,
                    entry.Name,
                    entry.Info,
                    entry.Introduce,
                    0
                );

                registered = true;
                Plugin.Logger.LogInfo(
                    "[Lob-shroom] Registered as a stackable spore lobber (6032)."
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Lob-shroom] Registration failed: " + exception
                );
            }
        }

        internal static void Configure(GameObject prefab)
        {
            if (prefab == null)
                return;

            Cabbage? plant = prefab.GetComponent<Cabbage>();
            if (plant == null)
                return;

            V11PlantsBootstrap.EnsureShooterRuntimeReferences(
                plant,
                "Lob-shroom"
            );
            V11PlantsBootstrap.ApplyNativeShooterControllerWithLocalClips(
                prefab,
                "Lob-shroom",
                PlantType.Cabbagepult
            );

            Plant.PlantTag tag = plant.plantTag;
            tag.puffPlant = true;
            plant.plantTag = tag;
        }

        internal static void OnGameInit()
        {
            var prefabs = GameAPP.resourcesManager?.plantPrefabs;
            PlantType type = (PlantType)Plants.LobShroom.ID;

            if (prefabs != null && prefabs.ContainsKey(type))
                Configure(prefabs[type]);

            AlmanacCompatibility.RefreshLoadedData();
        }
    }
}

namespace PlantsPlus.Plants
{
    public sealed class LobShroom : MonoBehaviour
    {
        public const int ID = 6032;

        public LobShroom(IntPtr pointer) : base(pointer) { }

        public void Start()
        {
            Core.LobShroomBootstrap.Configure(gameObject);
        }
    }

}
