using CustomizeLib.MelonLoader;
using HarmonyLib;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class V13DoublePotsBootstrap
    {
        private const int FallbackToughness = 300;
        private const int FallbackCost = 25;
        private const float FallbackRecharge = 7.5f;
        private static bool registered;
        private static bool sawDataLogged;
        private static bool vortexDataLogged;
        private static bool savingSawDiscovery;
        [ThreadStatic] private static bool shovelUseInProgress;
        // PvZ Fusion 3.9 internal enum name for the Almanac plant "Maelonstorm" (ID 957).
        private const PlantType MaelonstormType = PlantType.UltimateWinterMelon;

        internal static void OnStart()
        {
            if (registered)
                return;

            try
            {
                RegisterSawMyPot();
                RegisterVortexPot();
                registered = true;
                Plugin.Logger.LogInfo(
                    "[v1.3 Double Pots] Registered Saw-my-pot (6033) and Vortex Pot (6034)."
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[v1.3 Double Pots] Registration failed safely: " + exception
                );
            }
        }

        internal static void OnGameInit()
        {
            RefreshNativePlantData(
                (PlantType)Plants.SawMyPot.ID,
                PlantType.Pot,
                ref sawDataLogged,
                "Saw-my-pot"
            );
            RefreshNativePlantData(
                (PlantType)Plants.VortexPot.ID,
                PlantType.IcePot,
                ref vortexDataLogged,
                "Vortex Pot"
            );

            ConfigureRegisteredPot(
                (PlantType)Plants.SawMyPot.ID,
                PlantType.Pot,
                "Saw-my-pot"
            );
            ConfigureRegisteredPot(
                (PlantType)Plants.VortexPot.ID,
                PlantType.IcePot,
                "Vortex Pot"
            );

            RestoreSawMyPotDiscovery();

            AlmanacCompatibility.RefreshLoadedData();
        }

        private static bool IsSawMyPotPersistentlyDiscovered()
        {
            PlantType type = (PlantType)Plants.SawMyPot.ID;
            GameConfig? config = GameAPP.config;
            if (config == null)
                return false;

            return (config.meetPlants != null &&
                    config.meetPlants.Contains(type)) ||
                (config.meetPlant_runTime != null &&
                 config.meetPlant_runTime.Contains(type));
        }

        private static void RestoreSawMyPotDiscovery()
        {
            PlantType type = (PlantType)Plants.SawMyPot.ID;
            GameConfig? config = GameAPP.config;
            if (config == null || config.meetPlants == null ||
                !config.meetPlants.Contains(type))
            {
                return;
            }

            if (config.meetPlant_runTime != null &&
                !config.meetPlant_runTime.Contains(type))
            {
                config.meetPlant_runTime.Add(type);
            }
        }

        private static void PersistSawMyPotDiscovery()
        {
            if (savingSawDiscovery)
                return;

            PlantType type = (PlantType)Plants.SawMyPot.ID;
            GameConfig? config = GameAPP.config;
            if (config == null)
                return;

            bool changed = false;
            if (config.meetPlants != null &&
                !config.meetPlants.Contains(type))
            {
                config.meetPlants.Add(type);
                changed = true;
            }

            if (config.meetPlant_runTime != null &&
                !config.meetPlant_runTime.Contains(type))
            {
                config.meetPlant_runTime.Add(type);
            }

            if (!changed || SaveInfo.Instance == null)
                return;

            try
            {
                savingSawDiscovery = true;
                SaveInfo.Instance.SavePlayerData();
                Plugin.Logger.LogInfo(
                    "[Saw-my-pot] Almanac discovery saved to current profile."
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Saw-my-pot] Discovery save failed safely: " +
                    exception.Message
                );
            }
            finally
            {
                savingSawDiscovery = false;
            }
        }

        private static void RegisterSawMyPot()
        {
            AssetBundle bundle = LoadBundle("saw_my_pot");
            GameObject prefab = FindPrefab(
                bundle,
                new[] { "PotPrefab", "SawMyPotPrefab", "Saw-my-potPrefab" }
            );
            GameObject preview = FindPreview(
                bundle,
                new[] { "PotPreview", "SawMyPotPreview", "Saw-my-potPreview" }
            );

            CustomCore.RegisterCustomPlant<Pot, Plants.SawMyPot>(
                Plants.SawMyPot.ID,
                prefab,
                preview,
                new List<(int, int)>
                {
                    ((int)PlantType.Shulkflower, (int)PlantType.Pot),
                    ((int)PlantType.Pot, (int)PlantType.Shulkflower)
                },
                0f,
                0f,
                0,
                FallbackToughness,
                FallbackRecharge,
                FallbackCost
            );

            PlantType type = (PlantType)Plants.SawMyPot.ID;
            if (!CustomCore.TypeMgrExtra.IsPot.Contains(type))
                CustomCore.TypeMgrExtra.IsPot.Add(type);

            AlmanacEntry entry = AlmanacContent.SawMyPot;
            CustomCore.AddPlantAlmanacStrings(
                type,
                entry.Name,
                entry.Info,
                entry.Introduce,
                FallbackCost
            );
        }

        private static void RegisterVortexPot()
        {
            AssetBundle bundle = LoadBundle("vortexpot");
            GameObject prefab = FindPrefab(
                bundle,
                new[] { "IcePotPrefab", "VortexPotPrefab", "PotPrefab" }
            );
            GameObject preview = FindPreview(
                bundle,
                new[] { "IcePotPreview", "VortexPotPreview", "PotPreview" }
            );

            // Maelonstorm is displayed as "Maelonstorm" in-game, but its actual
            // PvZ Fusion 3.9 PlantType enum is UltimateWinterMelon (957).
            List<(int, int)> hiddenRecipes = new List<(int, int)>
            {
                ((int)MaelonstormType, (int)PlantType.Pot),
                ((int)PlantType.Pot, (int)MaelonstormType)
            };
            Plugin.Logger.LogInfo(
                "[Vortex Pot] Fusion registered: Flower Pot + Maelonstorm " +
                "(UltimateWinterMelon/957) -> Vortex Pot."
            );

            CustomCore.RegisterCustomPlant<IcePot, Plants.VortexPot>(
                Plants.VortexPot.ID,
                prefab,
                preview,
                hiddenRecipes,
                0f,
                0f,
                0,
                FallbackToughness,
                FallbackRecharge,
                FallbackCost
            );

            PlantType type = (PlantType)Plants.VortexPot.ID;
            if (!CustomCore.TypeMgrExtra.IsPot.Contains(type))
                CustomCore.TypeMgrExtra.IsPot.Add(type);

            RegisterVortexPotAsStrongOdyssey(type);

            AlmanacEntry entry = AlmanacContent.VortexPot;
            CustomCore.AddPlantAlmanacStrings(
                type,
                entry.Name,
                entry.Info,
                entry.Introduce,
                FallbackCost
            );
        }

        private static void RegisterVortexPotAsStrongOdyssey(PlantType type)
        {
            try
            {
                if (!CustomCore.CustomUltimatePlants.Contains(type))
                    CustomCore.AddUltimatePlant(type);

                if (!CustomCore.CustomStrongUltimatePlants.ContainsKey(type))
                {
                    CustomCore.RegisterCustomStrongUltimatePlant(
                        type,
                        (int)TravelUnlocks.UltimateWinterMelon
                    );
                }

                if (!TravelDictionary.PlantInfo.ContainsKey(type))
                {
                    TravelDictionary.PlantInfo.Add(
                        type,
                        new Il2CppSystem.ValueTuple<
                            Il2CppSystem.Nullable<PlantType>,
                            Il2CppSystem.Object,
                            Il2CppSystem.Object,
                            bool
                        >(
                            new Il2CppSystem.Nullable<PlantType>(type),
                            null!,
                            null!,
                            true
                        )
                    );
                }
                else
                {
                    TravelDictionary.PlantInfo[type].Item4 = true;
                }

                if (!TravelDictionary.allStrongUltimtePlant.Contains(type))
                    TravelDictionary.allStrongUltimtePlant.Add(type);

                Plugin.Logger.LogInfo(
                    "[Vortex Pot] Strong Odyssey registration active" +
                    " | Unlock = Maelonstorm"
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Vortex Pot] Strong Odyssey metadata failed safely: " +
                    exception.Message
                );
            }
        }

        private static AssetBundle LoadBundle(string bundleName)
        {
            AssetBundle bundle = CustomCore.GetAssetBundle(
                Assembly.GetExecutingAssembly(),
                "PlantsPlus.Resources.AssetBundles." + bundleName
            );

            if (bundle == null)
                throw new InvalidOperationException(bundleName + " AssetBundle is null.");

            try
            {
                List<string> names = bundle.GetAssetBundleAssetNames();
                Plugin.Logger.LogInfo(
                    "[v1.3 Double Pots] " + bundleName + " assets: " +
                    string.Join(", ", names)
                );
            }
            catch
            {
                // Asset-name logging is diagnostic only.
            }

            return bundle;
        }

        private static GameObject FindPrefab(
            AssetBundle bundle,
            string[] preferredNames
        )
        {
            GameObject result = FindNamedGameObject(bundle, preferredNames);
            if (result != null)
                return result;

            List<string> names = bundle.GetAssetBundleAssetNames();
            for (int index = 0; index < names.Count; index++)
            {
                string name = names[index];
                if (name.IndexOf("preview", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                try
                {
                    GameObject candidate = bundle.GetAsset<GameObject>(name);
                    if (candidate != null)
                        return candidate;
                }
                catch { }
            }

            throw new InvalidOperationException("No plant prefab GameObject was found in " + bundle.name + ".");
        }

        private static GameObject FindPreview(
            AssetBundle bundle,
            string[] preferredNames
        )
        {
            GameObject result = FindNamedGameObject(bundle, preferredNames);
            if (result != null)
                return result;

            List<string> names = bundle.GetAssetBundleAssetNames();
            for (int index = 0; index < names.Count; index++)
            {
                string name = names[index];
                if (name.IndexOf("preview", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                try
                {
                    GameObject candidate = bundle.GetAsset<GameObject>(name);
                    if (candidate != null)
                        return candidate;
                }
                catch { }
            }

            throw new InvalidOperationException("No preview GameObject was found in " + bundle.name + ".");
        }

        private static GameObject FindNamedGameObject(
            AssetBundle bundle,
            string[] names
        )
        {
            for (int index = 0; index < names.Length; index++)
            {
                try
                {
                    GameObject candidate = bundle.GetAsset<GameObject>(names[index]);
                    if (candidate != null)
                        return candidate;
                }
                catch { }
            }
            return null;
        }

        internal static void ConfigurePotRuntime(
            Pot plant,
            PlantType templateType,
            string label
        )
        {
            if (plant == null)
                return;

            try
            {
                GameObject customRoot = plant.gameObject;
                GameObject nativePrefab = TryGetPlantPrefab(templateType);
                Pot nativePlant = nativePrefab != null
                    ? nativePrefab.GetComponent<Pot>()
                    : null;

                if (plant.anim == null)
                    plant.anim = customRoot.GetComponentInChildren<Animator>();

                if (plant.axis == null)
                    plant.axis = FindDescendant(customRoot.transform, "Axis") ?? customRoot.transform;

                if (plant.rb == null)
                    plant.rb = customRoot.GetComponent<Rigidbody2D>();

                if (plant.board == null)
                    plant.board = Board.Instance;

                if (nativePlant != null)
                {
                    plant.carry = nativePlant.carry;
                    plant.lowerPot = nativePlant.lowerPot;

                    if (plant.bulletLayer.value == 0)
                        plant.bulletLayer = nativePlant.bulletLayer;
                    if (plant.plantLayer.value == 0)
                        plant.plantLayer = nativePlant.plantLayer;
                    if (plant.zombieLayer.value == 0)
                        plant.zombieLayer = nativePlant.zombieLayer;
                    if (plant.potLayer.value == 0)
                        plant.potLayer = nativePlant.potLayer;
                }

                Plant.PlantTag tag = plant.plantTag;
                tag.potPlant = true;
                plant.plantTag = tag;
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[" + label + "] Pot runtime bridge failed safely: " + exception.Message
                );
            }
        }

        private static void ConfigureRegisteredPot(
            PlantType customType,
            PlantType templateType,
            string label
        )
        {
            GameObject prefab = TryGetPlantPrefab(customType);
            Pot pot = prefab != null ? prefab.GetComponent<Pot>() : null;
            if (pot != null)
                ConfigurePotRuntime(pot, templateType, label);
        }

        private static GameObject TryGetPlantPrefab(PlantType type)
        {
            try
            {
                if (GameAPP.resourcesManager == null ||
                    GameAPP.resourcesManager.plantPrefabs == null ||
                    !GameAPP.resourcesManager.plantPrefabs.ContainsKey(type))
                {
                    return null;
                }
                return GameAPP.resourcesManager.plantPrefabs[type];
            }
            catch
            {
                return null;
            }
        }

        private static Transform FindDescendant(Transform root, string wantedName)
        {
            if (root == null)
                return null;

            if (root.name.Equals(wantedName, StringComparison.OrdinalIgnoreCase))
                return root;

            for (int index = 0; index < root.childCount; index++)
            {
                Transform found = FindDescendant(root.GetChild(index), wantedName);
                if (found != null)
                    return found;
            }
            return null;
        }

        internal static bool TryGetMaelonstorm(out PlantType type)
        {
            // Verified against PvZ Fusion 3.9 PlantType.cs:
            // UltimateWinterMelon = 957, displayed in the Almanac as Maelonstorm.
            type = MaelonstormType;
            return true;
        }

        private static void RefreshNativePlantData(
            PlantType customType,
            PlantType templateType,
            ref bool logged,
            string label
        )
        {
            try
            {
                if (!CustomCore.CustomPlants.TryGetValue(customType, out CustomPlantData customData))
                    return;

                PlantDataManager.PlantData nativeData = PlantDataManager.GetPlantData(templateType);
                if (nativeData == null || customData.PlantData == null)
                    return;

                PlantDataManager.PlantData target = customData.PlantData;
                target.thePlantType = customType;
                target.attackInterval = nativeData.attackInterval;
                target.produceInterval = nativeData.produceInterval;
                target.attackDamage = nativeData.attackDamage;
                target.maxHealth = nativeData.maxHealth;
                target.cd = nativeData.cd;
                target.cost = nativeData.cost;
                customData.PlantData = target;
                CustomCore.CustomPlants[customType] = customData;

                if (!logged)
                {
                    logged = true;
                    Plugin.Logger.LogInfo(
                        "[" + label + "] Native pot data mirrored from " + templateType +
                        " | HP = " + nativeData.maxHealth +
                        " | Recharge = " + nativeData.cd +
                        " | Cost = " + nativeData.cost
                    );
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[" + label + "] Native PlantData mirror failed safely: " + exception.Message
                );
            }
        }

        internal static bool IsSawFamily(Plant plant)
        {
            if (plant == null || plant.dying)
                return false;

            if ((int)plant.thePlantType == Plants.SawMyPot.ID ||
                (int)plant.thePlantType == Plants.VortexPot.ID)
            {
                return false;
            }

            try
            {
                if (TypeMgr.IsPot(plant.thePlantType))
                    return false;
            }
            catch { }

            if (plant.thePlantType == PlantType.Shulkflower)
                return false;

            if (plant.firstParent == PlantType.Shulkflower ||
                plant.secondParent == PlantType.Shulkflower)
            {
                return true;
            }

            return HasAncestor(plant.thePlantType, PlantType.Shulkflower);
        }

        internal static bool HasAncestor(PlantType type, PlantType ancestor)
        {
            return HasAncestor(type, ancestor, new HashSet<int>(), 0);
        }

        private static bool HasAncestor(
            PlantType type,
            PlantType ancestor,
            HashSet<int> visited,
            int depth
        )
        {
            if (type == ancestor)
                return true;
            if (depth > 14 || !visited.Add((int)type))
                return false;

            try
            {
                List<(int, int, int)> customFusions = CustomCore.CustomFusions;
                if (customFusions != null)
                {
                    for (int index = 0; index < customFusions.Count; index++)
                    {
                        (int target, int left, int right) = customFusions[index];
                        if (target != (int)type)
                            continue;

                        if (HasAncestor(
                                (PlantType)left,
                                ancestor,
                                new HashSet<int>(visited),
                                depth + 1
                            ) ||
                            HasAncestor(
                                (PlantType)right,
                                ancestor,
                                new HashSet<int>(visited),
                                depth + 1
                            ))
                        {
                            return true;
                        }
                    }
                }

                // Do not call MixData.TryGetDisMix from Plant.GetDamage.
                // PvZ Fusion 3.9 can expose an invalid IL2CPP ValueTuple here,
                // and reading Item1/Item2 causes a fatal AccessViolation.
                // Runtime Plant.firstParent/secondParent cover native direct
                // fusions; this managed list covers every custom descendant.
            }
            catch { }

            return false;
        }

        internal static bool HasSawMyPotUnder(Plant plant)
        {
            if (plant == null)
                return false;

            try
            {
                var plants = Lawnf.Get1x1Plants(plant.thePlantColumn, plant.thePlantRow);
                if (plants == null)
                    return false;

                for (int index = 0; index < plants.Count; index++)
                {
                    Plant candidate = plants[index];
                    if (candidate != null && (int)candidate.thePlantType == Plants.SawMyPot.ID)
                        return true;
                }
            }
            catch { }

            return false;
        }

        internal static bool TryGetIceVariant(PlantType source, out PlantType result)
        {
            result = (PlantType)(-1);

            if (source == PlantType.IceShroom || HasAncestor(source, PlantType.IceShroom))
                return false;

            try
            {
                if (TypeMgr.IsIcePlant(source))
                    return false;
            }
            catch { }

            try
            {
                List<(int, int, int)> customFusions = CustomCore.CustomFusions;
                if (customFusions != null)
                {
                    for (int index = 0; index < customFusions.Count; index++)
                    {
                        (int target, int left, int right) = customFusions[index];
                        bool matches =
                            (left == (int)source && right == (int)PlantType.IceShroom) ||
                            (right == (int)source && left == (int)PlantType.IceShroom);
                        if (matches && target != (int)source && target != (int)PlantType.IceShroom)
                        {
                            result = (PlantType)target;
                            return true;
                        }
                    }
                }

                if (MixData.TryGetMix(source, PlantType.IceShroom, out result, true) &&
                    result != source && result != PlantType.IceShroom)
                {
                    return true;
                }
                if (MixData.TryGetMix(PlantType.IceShroom, source, out result, true) &&
                    result != source && result != PlantType.IceShroom)
                {
                    return true;
                }
                if (MixData.TryGetMix(source, PlantType.IceShroom, out result, false) &&
                    result != source && result != PlantType.IceShroom)
                {
                    return true;
                }
                if (MixData.TryGetMix(PlantType.IceShroom, source, out result, false) &&
                    result != source && result != PlantType.IceShroom)
                {
                    return true;
                }
            }
            catch { }

            result = (PlantType)(-1);
            return false;
        }

        internal static Plant FindMainPlantOnPot(Pot support)
        {
            if (support == null)
                return null;

            try
            {
                var plants = Lawnf.Get1x1Plants(support.thePlantColumn, support.thePlantRow);
                if (plants == null)
                    return null;

                for (int index = 0; index < plants.Count; index++)
                {
                    Plant candidate = plants[index];
                    if (candidate == null || candidate.gameObject == support.gameObject ||
                        candidate.dying || candidate.thePlantHealth <= 0)
                    {
                        continue;
                    }

                    try
                    {
                        if (TypeMgr.IsPot(candidate.thePlantType) ||
                            TypeMgr.IsLily(candidate.thePlantType) ||
                            TypeMgr.IsPumpkin(candidate.thePlantType))
                        {
                            continue;
                        }
                    }
                    catch { }

                    return candidate;
                }
            }
            catch { }

            return null;
        }

        internal static bool ConvertToIceVariant(Plant target, PlantType expectedResult)
        {
            if (target == null)
                return false;

            try
            {
                int column = target.thePlantColumn;
                int row = target.thePlantRow;
                PlantType sourceType = target.thePlantType;

                // Supplying Ice-shroom through SetPlant with isFreeSet=true does
                // not run the native fusion route: it simply creates a loose
                // Ice-shroom. Replace the plant deliberately instead and defer
                // the spawn one frame so the old plant has left the board list.
                target.Die(Plant.DieReason.ByMix);
                QueuePotSwap(column, row, expectedResult);

                Plugin.Logger.LogInfo(
                    "[Vortex Pot] Ice conversion queued | " + sourceType +
                    " -> " + expectedResult
                );
                return true;
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Vortex Pot] Ice conversion failed safely: " + exception.Message
                );
                return false;
            }
        }

        internal static void FreezeAllZombies()
        {
            try
            {
                var zombies = Lawnf.GetAllZombies(false);
                if (zombies == null)
                    return;

                int frozen = 0;
                for (int index = 0; index < zombies.Count; index++)
                {
                    Zombie zombie = zombies[index];
                    if (zombie == null)
                        continue;

                    try
                    {
                        if (!zombie.Alive || zombie.isMindControlled)
                            continue;
                        zombie.SetFreeze(4f);
                        frozen++;
                    }
                    catch { }
                }

                Plugin.Logger.LogInfo(
                    "[Vortex Pot] No ice form exists: froze " + frozen + " zombies."
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Vortex Pot] Screen freeze failed safely: " + exception.Message
                );
            }
        }

        internal static void QueuePotSwap(int column, int row, PlantType resultType)
        {
            Plants.DoublePotSwapRunner.Queue(column, row, resultType);
        }

        internal static void DropMaelonstormCard(Vector2 position)
        {
            try
            {
                DroppedCard card = Lawnf.SetDroppedCard(
                    position,
                    MaelonstormType,
                    0
                );

                Plugin.Logger.LogInfo(
                    "[Vortex Pot] Shovel return | Maelonstorm seed packet = " +
                    (card != null ? "created" : "null")
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Vortex Pot] Maelonstorm seed packet failed safely: " +
                    exception.Message
                );
            }
        }

        [HarmonyPatch(typeof(Plant), "GetDamage")]
        private static class Plant_GetDamage_SawMyPot_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(Plant __instance, ref int __result)
            {
                if (__result <= 0 || !IsSawFamily(__instance) || !HasSawMyPotUnder(__instance))
                    return;

                __result = Mathf.Max(1, Mathf.RoundToInt(__result * 1.5f));
            }
        }

        [HarmonyPatch(
            typeof(CreatePlant),
            nameof(CreatePlant.SetPlant),
            new Type[]
            {
                typeof(int),
                typeof(int),
                typeof(PlantType),
                typeof(Plant),
                typeof(Vector2),
                typeof(bool),
                typeof(bool),
                typeof(Plant)
            }
        )]
        private static class CreatePlant_SetPlant_SawDiscovery_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(
                PlantType theSeedType,
                Plant __result
            )
            {
                if ((int)theSeedType == Plants.SawMyPot.ID ||
                    (__result != null &&
                     (int)__result.thePlantType == Plants.SawMyPot.ID))
                {
                    PersistSawMyPotDiscovery();
                }
            }
        }

        [HarmonyPatch(
            typeof(PlantDataManager),
            nameof(PlantDataManager.CheckIfPlantUnlock)
        )]
        private static class Lawnf_CheckIfPlantUnlock_SawDiscovery_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(
                PlantType thePlantType,
                ref bool __result
            )
            {
                if ((int)thePlantType == Plants.SawMyPot.ID &&
                    IsSawMyPotPersistentlyDiscovered())
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(Shovel), nameof(Shovel.Use))]
        private static class Shovel_Use_DoublePots_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix()
            {
                shovelUseInProgress = true;
            }

            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix()
            {
                shovelUseInProgress = false;
            }

            [HarmonyFinalizer]
            [HarmonyPriority(Priority.Last)]
            private static Exception Finalizer(Exception __exception)
            {
                shovelUseInProgress = false;
                return __exception;
            }
        }

        [HarmonyPatch(typeof(Plant), nameof(Plant.Die))]
        private static class Plant_Die_DoublePotSwap_Patch
        {
            private readonly struct SwapState
            {
                public SwapState(bool returnCard, Vector2 position)
                {
                    ReturnCard = returnCard;
                    Position = position;
                }

                public bool ReturnCard { get; }
                public Vector2 Position { get; }
            }

            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(
                Plant __instance,
                Plant.DieReason reason,
                out SwapState __state
            )
            {
                bool byShovel = shovelUseInProgress && reason == Plant.DieReason.ByShovel;
                bool vortexPot = byShovel && __instance != null &&
                    (int)__instance.thePlantType == Plants.VortexPot.ID;

                Vector3 worldPosition = __instance != null && __instance.axis != null
                    ? __instance.axis.transform.position
                    : (__instance != null ? __instance.transform.position : Vector3.zero);

                __state = new SwapState(
                    vortexPot,
                    new Vector2(worldPosition.x, worldPosition.y)
                );
            }

            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(SwapState __state)
            {
                if (__state.ReturnCard)
                    DropMaelonstormCard(__state.Position);
            }
        }
    }
}

namespace PlantsPlus.Plants
{
    using PlantsPlus.Core;

    public sealed class SawMyPot : MonoBehaviour
    {
        public const int ID = 6033;

        public SawMyPot(IntPtr pointer) : base(pointer) { }

        public void Start()
        {
            Pot plant = GetComponent<Pot>();
            V13DoublePotsBootstrap.ConfigurePotRuntime(
                plant,
                PlantType.Pot,
                "Saw-my-pot"
            );
        }
    }

    public sealed class VortexPot : MonoBehaviour
    {
        public const int ID = 6034;
        private const float TargetSettleDelay = 0.20f;

        private IcePot plant;
        private Plant candidate;
        private IntPtr processedPointer = IntPtr.Zero;
        private PlantType processedType = (PlantType)(-1);
        private float settleTimer;

        public VortexPot(IntPtr pointer) : base(pointer) { }

        public void Start()
        {
            plant = GetComponent<IcePot>();
            V13DoublePotsBootstrap.ConfigurePotRuntime(
                plant,
                PlantType.IcePot,
                "Vortex Pot"
            );
        }

        public void Update()
        {
            if (plant == null || plant.dying)
                return;

            Plant target = V13DoublePotsBootstrap.FindMainPlantOnPot(plant);
            if (target == null)
            {
                candidate = null;
                processedPointer = IntPtr.Zero;
                processedType = (PlantType)(-1);
                settleTimer = 0f;
                return;
            }

            if (candidate == null || candidate.Pointer != target.Pointer || candidate.thePlantType != target.thePlantType)
            {
                candidate = target;
                settleTimer = TargetSettleDelay;
                return;
            }

            if (settleTimer > 0f)
            {
                settleTimer -= Time.deltaTime;
                return;
            }

            if (processedPointer == target.Pointer && processedType == target.thePlantType)
                return;

            processedPointer = target.Pointer;
            processedType = target.thePlantType;

            try
            {
                if (TypeMgr.IsIcePlant(target.thePlantType) ||
                    V13DoublePotsBootstrap.HasAncestor(target.thePlantType, PlantType.IceShroom))
                {
                    return;
                }
            }
            catch { }

            if (V13DoublePotsBootstrap.TryGetIceVariant(target.thePlantType, out PlantType result))
            {
                V13DoublePotsBootstrap.ConvertToIceVariant(target, result);
                return;
            }

            V13DoublePotsBootstrap.FreezeAllZombies();
        }
    }

    public sealed class DoublePotSwapRunner : MonoBehaviour
    {
        private sealed class PendingSwap
        {
            public int Column;
            public int Row;
            public PlantType ResultType;
            public int Frames;
        }

        private static readonly List<PendingSwap> Pending = new List<PendingSwap>();

        public DoublePotSwapRunner(IntPtr pointer) : base(pointer) { }

        public static void Queue(int column, int row, PlantType resultType)
        {
            Pending.Add(new PendingSwap
            {
                Column = column,
                Row = row,
                ResultType = resultType,
                Frames = 1
            });

            try
            {
                if (Board.Instance != null &&
                    Board.Instance.gameObject.GetComponent<DoublePotSwapRunner>() == null)
                {
                    Board.Instance.gameObject.AddComponent<DoublePotSwapRunner>();
                }
            }
            catch { }
        }

        public void Update()
        {
            if (Pending.Count == 0)
                return;

            for (int index = Pending.Count - 1; index >= 0; index--)
            {
                PendingSwap swap = Pending[index];
                if (swap.Frames-- > 0)
                    continue;

                Pending.RemoveAt(index);
                Spawn(swap);
            }
        }

        private static void Spawn(PendingSwap swap)
        {
            try
            {
                CreatePlant creator = CreatePlant.Instance;
                if (creator == null && Board.Instance != null)
                    creator = Board.Instance.GetComponent<CreatePlant>();

                if (creator == null)
                    return;

                Plant created = creator.SetPlant(
                    swap.Column,
                    swap.Row,
                    swap.ResultType,
                    null,
                    default,
                    true,
                    false,
                    null
                );

                string action = swap.ResultType == PlantType.UltimateWinterMelon
                    ? "Shovel return to Maelonstorm (957)"
                    : "Ice conversion result";
                Plugin.Logger.LogInfo(
                    "[Vortex Pot] " + action + " | Tile = " +
                    swap.Column + "," + swap.Row +
                    " | Result = " + swap.ResultType +
                    " | Created = " + (created != null ? created.thePlantType.ToString() : "null")
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Vortex Pot] Shovel return failed safely: " + exception.Message
                );
            }
        }
    }
}
