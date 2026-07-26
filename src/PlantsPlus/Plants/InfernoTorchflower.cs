using CustomizeLib.BepInEx;
using HarmonyLib;
using Il2Cpp;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlantsPlus.Plants
{
    /// <summary>
    /// Advanced alternate form of Infernowood. Before ignition it behaves
    /// exactly like TorchSunflower. Once lit, its normal production and the
    /// Sun extracted from nearby projectiles are stored until clicked, then
    /// multiplied by the current fire-energy tier.
    /// </summary>
    public class InfernoTorchflower : MonoBehaviour
    {
        public const int InfernoTorchflowerID = 6006;

        public const int NaturalSunOutput = 25;
        public const float ProduceInterval = 25f;
        public const int ProjectileSunValue = 5;
        public const float ProjectileConversionRadius = 1.5f;
        public const int FireEnergyPerIgnition = 50;
        public const int MaximumEnergy = 250;

        // Registration fallbacks are replaced with Torchflower's exact native
        // PlantData as soon as PVZ Fusion initializes that table.
        public const int FallbackToughness = 300;
        public const float FallbackCardRecharge = 30f;
        public const int FallbackCardCost = 225;

        [ThreadStatic]
        private static InfernoTorchflower? activeSunCapture;

        private static bool plantDataMirrorLogged;
        private static bool prefabBridgeLogged;
        private static bool nativeRecoveryLogged;
        private int storedSun;
        private int capturedThisCycle;
        private bool naturalSunCreatedThisCycle;
        private bool chargeTextRegistered;
        private bool chargeUiWaitLogged;

        public InfernoTorchflower(IntPtr ptr) : base(ptr) { }

        public TorchSunflower? NativePlant =>
            gameObject.GetComponent<TorchSunflower>();

        public void Start()
        {
            TorchSunflower? plant = NativePlant;

            if (plant == null)
            {
                Plugin.Logger.LogError(
                    "[Inferno Torchflower] Start failed: no " +
                    "TorchSunflower component."
                );
                return;
            }

            EnsureRuntimeReferences(plant);

            plant.attributeCount = Mathf.Clamp(
                plant.attributeCount,
                0,
                MaximumEnergy
            );
            TryRegisterChargeText(plant);
            UpdateEnergyText(plant);

            Plugin.Logger.LogInfo(
                "[Inferno Torchflower] Ready" +
                " | Native behaviour = TorchSunflower" +
                " | Normal mode = native TorchSunflower" +
                " | Lit mode = stores all produced Sun" +
                " | Fire energy = +" + FireEnergyPerIgnition +
                " | Maximum energy = " + MaximumEnergy +
                " | Maximum multiplier = x2.5"
            );
        }

        public void Update()
        {
            TorchSunflower? plant = NativePlant;

            if (plant == null || plant.dying)
                return;

            plant.attributeCount = Mathf.Clamp(
                plant.attributeCount,
                0,
                MaximumEnergy
            );

            if (!chargeTextRegistered)
                TryRegisterChargeText(plant);
        }

        private void TryRegisterChargeText(TorchSunflower plant)
        {
            if (chargeTextRegistered || plant == null)
                return;

            try
            {
                var text = plant.RegisterText(
                    new Color(1f, 0.55f, 0.1f, 1f),
                    new System.Func<string>(GetChargeText),
                    new Vector2(150f, 28f)
                );

                chargeTextRegistered = text != null;

                if (text != null)
                {
                    text.enableAutoSizing = false;
                    text.fontSize = 18f;
                    text.rectTransform.sizeDelta =
                        new Vector2(150f, 28f);
                    chargeUiWaitLogged = false;
                }
            }
            catch (Exception exception)
            {
                if (!chargeUiWaitLogged)
                {
                    chargeUiWaitLogged = true;
                    Plugin.Logger.LogWarning(
                        "[Inferno Torchflower] Charge UI is not ready yet: " +
                        exception.Message
                    );
                }
            }
        }

        private string GetChargeText()
        {
            TorchSunflower? plant = NativePlant;
            int energy = plant != null
                ? Mathf.Clamp(plant.attributeCount, 0, MaximumEnergy)
                : 0;

            float multiplier = energy / 100f;
            return "E " + energy + "/" + MaximumEnergy +
                " | x" + multiplier.ToString("0.0") +
                " | S " + Mathf.Max(0, storedSun);
        }

        private static bool IsInfernoTorchflower(Plant? plant)
        {
            return
                plant != null &&
                (int)plant.thePlantType == InfernoTorchflowerID;
        }

        private void BeginSunCapture()
        {
            capturedThisCycle = 0;
            naturalSunCreatedThisCycle = false;
        }

        private void StoreNaturalSun()
        {
            naturalSunCreatedThisCycle = true;
            StoreSun(NaturalSunOutput);
        }

        private void StoreSun(int amount)
        {
            if (amount <= 0)
                return;

            if (storedSun <= int.MaxValue - amount)
                storedSun += amount;
            else
                storedSun = int.MaxValue;

            if (capturedThisCycle <= int.MaxValue - amount)
                capturedThisCycle += amount;
            else
                capturedThisCycle = int.MaxValue;

            // Stored production shares the visible counter with energy, so
            // refresh it on every captured projectile instead of waiting for
            // the next natural production cycle or click.
            TorchSunflower? plant = NativePlant;

            if (plant != null && !plant.dying)
                UpdateEnergyText(plant);
        }

        private void FinishSunCapture()
        {
            if (capturedThisCycle <= 0)
                return;

            Plugin.Logger.LogInfo(
                "[Inferno Torchflower] Sun stored" +
                " | This production = " + capturedThisCycle +
                " | Reserve = " + storedSun
            );
        }

        private bool ProduceSafeFallback(TorchSunflower plant)
        {
            CreateItem? creator = CreateItem.Instance;

            if (creator == null)
                return false;

            if (!naturalSunCreatedThisCycle)
            {
                GameObject naturalSun = creator.SetCoin(
                    plant.thePlantColumn,
                    plant.thePlantRow,
                    (int)ItemType.NormalSun,
                    0
                );

                naturalSunCreatedThisCycle =
                    naturalSun != null || naturalSunCreatedThisCycle;
            }

            int converted = ConvertNearbyPlayerProjectiles(plant);

            Plugin.Logger.LogInfo(
                "[Inferno Torchflower] Safe production completed" +
                " | Natural Sun = " +
                (naturalSunCreatedThisCycle ? NaturalSunOutput : 0) +
                " | Converted projectiles = " + converted +
                " | Stored Sun = " + storedSun
            );

            return naturalSunCreatedThisCycle;
        }

        private int ConvertNearbyPlayerProjectiles(TorchSunflower plant)
        {
            int converted = 0;

            try
            {
                var bullets =
                    UnityEngine.Resources.FindObjectsOfTypeAll<Bullet>();

                Vector2 center = plant.transform.position;
                float maximumDistanceSquared =
                    ProjectileConversionRadius * ProjectileConversionRadius;

                for (int index = 0; index < bullets.Length; index++)
                {
                    Bullet bullet = bullets[index];

                    if (
                        bullet == null ||
                        bullet.dying ||
                        bullet.gameObject == null ||
                        !bullet.gameObject.activeInHierarchy ||
                        bullet.Team != Team.Player
                    )
                    {
                        continue;
                    }

                    Vector2 offset =
                        (Vector2)bullet.transform.position - center;

                    if (offset.sqrMagnitude > maximumDistanceSquared)
                        continue;

                    bullet.Die();
                    StoreSun(ProjectileSunValue);
                    converted++;
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Inferno Torchflower] Projectile conversion fallback " +
                    "stopped safely: " + exception.Message
                );
            }

            return converted;
        }

        private void AddFireEnergy()
        {
            TorchSunflower? plant = NativePlant;

            if (plant == null || plant.dying)
                return;

            int before = Mathf.Clamp(plant.attributeCount, 0, MaximumEnergy);
            int after = Mathf.Min(MaximumEnergy, before + FireEnergyPerIgnition);
            plant.attributeCount = after;
            UpdateEnergyText(plant);

            Plugin.Logger.LogInfo(
                "[Inferno Torchflower] Lit by native fire line" +
                " | Energy " + before + " -> " + after +
                " | Stored Sun = " + storedSun
            );
        }

        private static void UpdateEnergyText(Plant plant)
        {
            try
            {
                plant.UpdateText();
            }
            catch
            {
                // Some game modes do not create the optional attribute label.
            }
        }

        private static void EnsureRuntimeReferences(TorchSunflower plant)
        {
            if (!IsInfernoTorchflower(plant))
                return;

            GameObject? nativePrefab =
                TryGetPlantPrefab(PlantType.TorchSunflower);
            TorchSunflower? nativePlant = nativePrefab != null
                ? nativePrefab.GetComponent<TorchSunflower>()
                : null;
            GameObject customRoot = plant.gameObject;

            if (plant.anim == null)
            {
                Animator? nativeAnimator = nativePlant != null
                    ? nativePlant.anim
                    : null;
                Transform? mappedAnimator = MapNativeTransform(
                    customRoot,
                    nativePrefab,
                    nativeAnimator != null
                        ? nativeAnimator.transform
                        : null
                );

                plant.anim = mappedAnimator != null
                    ? mappedAnimator.GetComponent<Animator>()
                    : customRoot.GetComponentInChildren<Animator>();
            }

            if (plant.axis == null)
            {
                plant.axis = MapNativeTransform(
                    customRoot,
                    nativePrefab,
                    nativePlant != null ? nativePlant.axis : null
                ) ?? customRoot.transform;
            }

            if (plant.shoot == null)
            {
                plant.shoot = MapNativeTransform(
                    customRoot,
                    nativePrefab,
                    nativePlant != null ? nativePlant.shoot : null
                ) ?? FindShotOrigin(customRoot.transform)
                    ?? customRoot.transform;
            }

            if (plant.shoot2 == null &&
                nativePlant != null &&
                nativePlant.shoot2 != null)
            {
                plant.shoot2 = MapNativeTransform(
                    customRoot,
                    nativePrefab,
                    nativePlant.shoot2
                );
            }

            if (plant.rb == null)
                plant.rb = customRoot.GetComponent<Rigidbody2D>();

            if (plant.board == null)
                plant.board = Board.Instance;

            if (plant.bulletLayer.value == 0)
            {
                LayerMask nativeMask = nativePlant != null
                    ? nativePlant.bulletLayer
                    : default;

                plant.bulletLayer = nativeMask.value != 0
                    ? nativeMask
                    : (LayerMask)LayerMask.GetMask("Bullet");
            }

            if (plant.plantLayer.value == 0)
            {
                LayerMask nativeMask = nativePlant != null
                    ? nativePlant.plantLayer
                    : default;

                plant.plantLayer = nativeMask.value != 0
                    ? nativeMask
                    : (LayerMask)LayerMask.GetMask("Plant");
            }

            if (plant.zombieLayer.value == 0)
            {
                LayerMask nativeMask = nativePlant != null
                    ? nativePlant.zombieLayer
                    : default;

                plant.zombieLayer = nativeMask.value != 0
                    ? nativeMask
                    : (LayerMask)LayerMask.GetMask("Zombie");
            }
        }

        private static void ConfigureRegisteredPrefab()
        {
            GameObject? customPrefab = TryGetPlantPrefab(
                (PlantType)InfernoTorchflowerID
            );

            if (customPrefab == null)
            {
                Plugin.Logger.LogWarning(
                    "[Inferno Torchflower] Registered prefab was unavailable " +
                    "after GameAPP.LoadResources."
                );
                return;
            }

            TorchSunflower? plant =
                customPrefab.GetComponent<TorchSunflower>();

            if (plant == null)
            {
                Plugin.Logger.LogError(
                    "[Inferno Torchflower] Registered prefab has no " +
                    "TorchSunflower component."
                );
                return;
            }

            EnsureRuntimeReferences(plant);

            if (!prefabBridgeLogged)
            {
                prefabBridgeLogged = true;
                Plugin.Logger.LogInfo(
                    "[Inferno Torchflower] Native prefab bridge" +
                    " | anim = " + Describe(plant.anim) +
                    " | axis = " + Describe(plant.axis) +
                    " | shoot = " + Describe(plant.shoot) +
                    " | board = runtime" +
                    " | bullet mask = " + plant.bulletLayer.value
                );
            }
        }

        private static GameObject? TryGetPlantPrefab(PlantType plantType)
        {
            try
            {
                if (
                    GameAPP.resourcesManager == null ||
                    GameAPP.resourcesManager.plantPrefabs == null ||
                    !GameAPP.resourcesManager.plantPrefabs.ContainsKey(plantType)
                )
                {
                    return null;
                }

                return GameAPP.resourcesManager.plantPrefabs[plantType];
            }
            catch
            {
                return null;
            }
        }

        private static Transform? MapNativeTransform(
            GameObject customRoot,
            GameObject? nativeRoot,
            Transform? nativeReference
        )
        {
            if (customRoot == null || nativeReference == null)
                return null;

            if (nativeRoot != null)
            {
                if (nativeReference == nativeRoot.transform)
                    return customRoot.transform;

                string? relativePath = GetRelativePath(
                    nativeReference,
                    nativeRoot.transform
                );

                if (!string.IsNullOrEmpty(relativePath))
                {
                    Transform exact = customRoot.transform.Find(relativePath);

                    if (exact != null)
                        return exact;
                }
            }

            return FindDescendant(customRoot.transform, nativeReference.name);
        }

        private static Transform? FindShotOrigin(Transform root)
        {
            if (root == null)
                return null;

            string normalizedName = root.name
                .Replace("_", string.Empty)
                .Replace(" ", string.Empty)
                .ToLowerInvariant();

            if (
                normalizedName.Contains("shoot") ||
                normalizedName.Contains("muzzle") ||
                normalizedName.Contains("firepoint")
            )
            {
                return root;
            }

            for (int index = 0; index < root.childCount; index++)
            {
                Transform? found = FindShotOrigin(root.GetChild(index));

                if (found != null)
                    return found;
            }

            return null;
        }

        private static Transform? FindDescendant(
            Transform root,
            string wantedName
        )
        {
            if (root == null || string.IsNullOrEmpty(wantedName))
                return null;

            if (root.name == wantedName)
                return root;

            for (int index = 0; index < root.childCount; index++)
            {
                Transform? found = FindDescendant(
                    root.GetChild(index),
                    wantedName
                );

                if (found != null)
                    return found;
            }

            return null;
        }

        private static string? GetRelativePath(
            Transform possibleChild,
            Transform root
        )
        {
            if (possibleChild == null || root == null)
                return null;

            var parts = new List<string>();
            Transform? current = possibleChild;

            while (current != null && current != root)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            if (current != root)
                return null;

            parts.Reverse();
            return string.Join("/", parts);
        }

        private static string Describe(UnityEngine.Object? value)
        {
            return value != null ? value.name : "<missing>";
        }

        internal static void HandleClick(Plant plant)
        {
            if (!IsInfernoTorchflower(plant) || plant.dying)
                return;

            InfernoTorchflower? behaviour =
                plant.gameObject.GetComponent<InfernoTorchflower>();

            if (behaviour == null)
            {
                Plugin.Logger.LogWarning(
                    "[Inferno Torchflower] Click ignored: custom behaviour " +
                    "is missing."
                );
                return;
            }

            behaviour.TryReleaseStoredSun();
        }

        private void TryReleaseStoredSun()
        {
            TorchSunflower? plant = NativePlant;

            if (plant == null || plant.dying)
                return;

            int energy = Mathf.Clamp(plant.attributeCount, 0, MaximumEnergy);

            if (energy <= 0)
            {
                Plugin.Logger.LogInfo(
                    "[Inferno Torchflower] Release ignored: not lit" +
                    " | Energy = " + energy + "/" + MaximumEnergy +
                    " | Stored Sun = " + storedSun
                );
                return;
            }

            if (storedSun <= 0)
            {
                Plugin.Logger.LogInfo(
                    "[Inferno Torchflower] Release ignored: reserve empty" +
                    " | Energy = " + energy + "/" + MaximumEnergy
                );
                return;
            }

            CreateItem? creator = CreateItem.Instance;

            if (creator == null)
            {
                Plugin.Logger.LogWarning(
                    "[Inferno Torchflower] Stored Sun release failed " +
                    "safely: CreateItem.Instance is null."
                );
                return;
            }

            float multiplier = energy / 100f;
            double multipliedSun = storedSun * (double)multiplier;
            int requestedSun = multipliedSun >= int.MaxValue
                ? int.MaxValue
                : Mathf.FloorToInt((float)multipliedSun);

            // PvZ Fusion's smallest physical Sun is worth 5. Keep the
            // payout on that native grid instead of granting it invisibly.
            int payableSun =
                requestedSun - requestedSun % ProjectileSunValue;
            if (payableSun <= 0)
                return;

            int normalDrops = payableSun / NaturalSunOutput;
            int littleDrops =
                (payableSun % NaturalSunOutput) / ProjectileSunValue;
            int releasedSun = 0;

            for (int index = 0; index < normalDrops; index++)
            {
                GameObject coin = creator.SetCoin(
                    plant.thePlantColumn,
                    plant.thePlantRow,
                    (int)ItemType.NormalSun,
                    0
                );

                if (coin != null)
                    releasedSun += NaturalSunOutput;
            }

            for (int index = 0; index < littleDrops; index++)
            {
                GameObject coin = creator.SetCoin(
                    plant.thePlantColumn,
                    plant.thePlantRow,
                    (int)ItemType.LittleSun,
                    0
                );

                if (coin != null)
                    releasedSun += ProjectileSunValue;
            }

            storedSun = 0;
            plant.attributeCount = 0;
            UpdateEnergyText(plant);

            Plugin.Logger.LogInfo(
                "[Inferno Torchflower] Stored Sun released" +
                " | Multiplier = x" + multiplier.ToString("0.0") +
                " | Released = " + releasedSun +
                " | Reserve = 0 | Energy = 0"
            );
        }

        internal static void RefreshNativePlantData()
        {
            PlantType customType = (PlantType)InfernoTorchflowerID;

            try
            {
                if (!CustomCore.CustomPlants.TryGetValue(
                    customType,
                    out CustomPlantData customData
                ))
                {
                    return;
                }

                PlantDataManager.PlantData nativeData =
                    PlantDataManager.GetPlantData(PlantType.TorchSunflower);

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

                if (!plantDataMirrorLogged)
                {
                    plantDataMirrorLogged = true;
                    Plugin.Logger.LogInfo(
                        "[Inferno Torchflower] Native Torchflower PlantData " +
                        "mirrored" +
                        " | Production = " + nativeData.produceInterval +
                        "s | HP = " + nativeData.maxHealth +
                        " | Recharge = " + nativeData.cd +
                        "s | Cost = " + nativeData.cost
                    );
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Inferno Torchflower] Native PlantData was not ready; " +
                    "safe fallback values remain active: " +
                    exception.Message
                );
            }
        }

        [HarmonyPatch(typeof(TorchSunflower), nameof(TorchSunflower.ProduceSun))]
        private static class TorchSunflower_ProduceSun_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(
                TorchSunflower __instance,
                out InfernoTorchflower? __state
            )
            {
                __state = activeSunCapture;

                if (!IsInfernoTorchflower(__instance))
                    return;

                InfernoTorchflower? owner =
                    __instance.gameObject.GetComponent<InfernoTorchflower>();

                if (owner == null)
                    return;

                EnsureRuntimeReferences(__instance);

                int energy = Mathf.Clamp(
                    __instance.attributeCount,
                    0,
                    MaximumEnergy
                );
                if (energy <= 0)
                    return;

                owner.BeginSunCapture();
                activeSunCapture = owner;
            }

            [HarmonyFinalizer]
            [HarmonyPriority(Priority.Last)]
            private static Exception? Finalizer(
                Exception? __exception,
                InfernoTorchflower? __state
            )
            {
                InfernoTorchflower? current = activeSunCapture;
                Exception? result = __exception;

                if (current != null &&
                    !object.ReferenceEquals(current, __state))
                {
                    if (__exception != null)
                    {
                        TorchSunflower? plant = current.NativePlant;

                        try
                        {
                            if (plant != null &&
                                current.ProduceSafeFallback(plant))
                            {
                                result = null;

                                if (!nativeRecoveryLogged)
                                {
                                    nativeRecoveryLogged = true;
                                    Plugin.Logger.LogWarning(
                                        "[Inferno Torchflower] Native " +
                                        "TorchSunflower production had a " +
                                        "missing serialized reference; the " +
                                        "safe equivalent completed this and " +
                                        "future failed cycles."
                                    );
                                }
                            }
                        }
                        catch (Exception recoveryException)
                        {
                            Plugin.Logger.LogError(
                                "[Inferno Torchflower] Native production " +
                                "recovery failed: " + recoveryException
                            );
                        }
                    }

                    try
                    {
                        current.FinishSunCapture();
                    }
                    catch (Exception exception)
                    {
                        Plugin.Logger.LogWarning(
                            "[Inferno Torchflower] Sun capture summary " +
                            "failed safely: " + exception.Message
                        );
                    }
                }

                activeSunCapture = __state;
                return result;
            }
        }

        [HarmonyPatch(typeof(CreateItem), nameof(CreateItem.SetCoin))]
        private static class CreateItem_SetCoin_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static bool Prefix(
                int theItemType,
                ref GameObject __result
            )
            {
                InfernoTorchflower? owner = activeSunCapture;

                if (owner == null)
                {
                    return true;
                }

                if (theItemType == (int)ItemType.NormalSun)
                {
                    owner.StoreNaturalSun();
                    __result = null!;
                    return false;
                }

                if (theItemType != (int)ItemType.LittleSun)
                    return true;

                owner.StoreSun(ProjectileSunValue);
                __result = null!;
                return false;
            }
        }

        [HarmonyPatch(typeof(BoardAction), nameof(BoardAction.CreateFireLine))]
        private static class BoardAction_CreateFireLine_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(
                BoardAction __instance,
                int theFireRow,
                PlantType fromType,
                out List<InfernoTorchflower>? __state
            )
            {
                __state = null;

                if (__instance == null || __instance.board == null)
                    return;

                // Do not turn Witchfire/Pyro/Inferno-generated lines into a
                // mutual charge loop. A real Jalapeno fire line remains a
                // valid ignition and now grants 50 energy (5 to reach 250).
                if ((int)fromType == WitchfirePumpkin.WitchfirePumpkinID ||
                    (int)fromType == InfernoTorchflowerID ||
                    fromType == PlantType.JalaPumpkin)
                {
                    return;
                }

                try
                {
                    var plants = Lawnf.GetPlantsByRow(
                        __instance.board,
                        theFireRow
                    );

                    if (plants == null)
                        return;

                    var found = new List<InfernoTorchflower>();

                    for (int index = 0; index < plants.Count; index++)
                    {
                        Plant plant = plants[index];

                        if (!IsInfernoTorchflower(plant) || plant.dying)
                            continue;

                        InfernoTorchflower? behaviour =
                            plant.gameObject.GetComponent<InfernoTorchflower>();

                        if (behaviour != null)
                            found.Add(behaviour);
                    }

                    if (found.Count > 0)
                        __state = found;
                }
                catch (Exception exception)
                {
                    Plugin.Logger.LogWarning(
                        "[Inferno Torchflower] Fire-line scan failed " +
                        "safely: " + exception.Message
                    );
                }
            }

            [HarmonyPostfix]
            private static void Postfix(
                int damage,
                List<InfernoTorchflower>? __state
            )
            {
                if (__state == null || damage <= 0)
                    return;

                for (int index = 0; index < __state.Count; index++)
                    __state[index]?.AddFireEnergy();
            }
        }

        [HarmonyPatch(typeof(GameAPP), nameof(GameAPP.LoadResources))]
        private static class GameAPP_LoadResources_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix()
            {
                RefreshNativePlantData();
                ConfigureRegisteredPrefab();
            }
        }

        [HarmonyPatch(typeof(Lawnf), nameof(Lawnf.IsSuperPlant))]
        private static class Lawnf_IsSuperPlant_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(
                PlantType thePlantType,
                ref bool __result
            )
            {
                if ((int)thePlantType == InfernoTorchflowerID)
                    __result = true;
            }
        }

        [HarmonyPatch(typeof(Lawnf), nameof(Lawnf.CheckPlantClass))]
        private static class Lawnf_CheckPlantClass_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(
                PlantType thePlantType,
                ref int __result
            )
            {
                if ((int)thePlantType == InfernoTorchflowerID)
                    __result = Lawnf.CheckPlantClass(PlantType.SuperTorch);
            }
        }

        [HarmonyPatch(
            typeof(AlmanacPlantMenu.__c),
            "_LookSuper_b__19_0"
        )]
        private static class AlmanacPlantMenu_LookSuper_Filter_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(PlantType p, ref bool __result)
            {
                if ((int)p == InfernoTorchflowerID)
                    __result = true;
            }
        }
    }
}
