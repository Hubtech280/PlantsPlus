using CustomizeLib.MelonLoader;
using Il2Cpp;
using System;
using System.Reflection;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class NightRoofMap
    {
        private static GameObject? mapPrefab;

        internal static void OnStart()
        {
            try
            {
                var bundle = CustomCore.GetAssetBundle(
                    Assembly.GetExecutingAssembly(),
                    "PlantsPlus.Resources.AssetBundles.nightroofmap"
                );
                mapPrefab = bundle?.GetAsset<GameObject>("Roof");

                if (mapPrefab == null)
                {
                    throw new InvalidOperationException(
                        "Night Roof map prefab missing."
                    );
                }

                Plugin.Logger.LogInfo(
                    "[Night Roof] Custom animated map loaded."
                );
            }
            catch (Exception exception)
            {
                mapPrefab = null;
                Plugin.Logger.LogError(
                    "[Night Roof] Map loading failed safely: " + exception
                );
            }
        }

        internal static bool TryCreate(ref GameObject result)
        {
            if (mapPrefab == null)
                return false;

            result = UnityEngine.Object.Instantiate(mapPrefab);
            result.name = "PlantsPlus_NightRoof";
            result.SetActive(true);
            return true;
        }

        internal static void OnGameInit()
        {
            try
            {
                var backgrounds =
                    GameAPP.resourcesManager?.backgroundPrefabs;

                if (backgrounds == null || mapPrefab == null)
                    return;

                backgrounds[SceneType.NightRoof] = mapPrefab;

                Plugin.Logger.LogInfo(
                    "[Night Roof] Map installed in the game's background list."
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Night Roof] Map installation failed safely: " +
                    exception
                );
            }
        }
    }
}
