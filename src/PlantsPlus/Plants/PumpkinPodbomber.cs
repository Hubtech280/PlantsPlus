using CustomizeLib.BepInEx;
using HarmonyLib;
using Il2Cpp;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlantsPlus.Plants
{
    /// <summary>
    /// Advanced alternate form of Explode-o-shooter. PeaPumpkin keeps the
    /// native Pumpkin Pod targeting, copied volley size and copied cadence;
    /// this bridge replaces the fourth copied volley with Explode-o-peas.
    /// Cherry Shooter instead receives Explode-o-peas on every volley.
    /// </summary>
    public class PumpkinPodbomber : MonoBehaviour
    {
        public const int PumpkinPodbomberID = 6007;

        public const int FallbackToughness = 4000;
        public const int FallbackCardCost = 350;
        public const float FallbackCardRecharge = 30f;
        public const float FallbackAttackInterval = 3f;
        public const int FallbackExplodeDamage = 300;
        public const int ExplosiveVolleyFrequency = 4;

        [ThreadStatic]
        private static PumpkinPodbomber? activeVolley;

        [ThreadStatic]
        private static bool shovelUseInProgress;

        private static bool prefabBridgeLogged;
        private static bool plantDataMirrorLogged;
        private static bool shovelConversionLogged;

        private readonly List<Bullet> explosiveBullets = new List<Bullet>();
        private Plant? trackedInnerPlant;
        private PlantType trackedInnerType = (PlantType)(-1);
        private int volleyCount;
        private bool explosiveVolley;

        public PumpkinPodbomber(IntPtr ptr) : base(ptr) { }

        public PeaPumpkin? NativePlant =>
            gameObject.GetComponent<PeaPumpkin>();

        public void Start()
        {
            PeaPumpkin? plant = NativePlant;

            if (plant == null)
            {
                Plugin.Logger.LogError(
                    "[Pumpkin Podbomber] Start failed: no PeaPumpkin " +
                    "component."
                );
                return;
            }

            EnsureRuntimeReferences(plant);

            Plugin.Logger.LogInfo(
                "[Pumpkin Podbomber] Ready" +
                " | Native behaviour = PeaPumpkin" +
                " | Explosive volley = every " +
                ExplosiveVolleyFrequency + " shots" +
                " | Cherry Shooter = every shot explosive" +
                " | Cherry cadence = native Pumpkin Pod 50% rate" +
                " | Shovel = Explode-o-shooter card"
            );
        }

        private static bool IsPumpkinPodbomber(Plant? plant)
        {
            return
                plant != null &&
                (int)plant.thePlantType == PumpkinPodbomberID;
        }

        private static bool SamePlant(Plant? first, Plant? second)
        {
            return
                first != null &&
                second != null &&
                first.gameObject == second.gameObject;
        }

        private static Plant? FindProtectedPeaPlant(PeaPumpkin shell)
        {
            if (shell == null)
                return null;

            try
            {
                var plants = Lawnf.Get1x1Plants(
                    shell.thePlantColumn,
                    shell.thePlantRow
                );

                if (plants == null)
                    return null;

                // Prefer the exact Pumpkin relationship maintained by PVZ
                // Fusion. It remains correct when several support plants
                // share the tile.
                for (int index = 0; index < plants.Count; index++)
                {
                    Plant candidate = plants[index];

                    if (!IsPossibleInnerPlant(candidate, shell))
                        continue;

                    try
                    {
                        if (SamePlant(candidate.Pumpkin, shell))
                            return candidate;
                    }
                    catch
                    {
                        // The exact-tile fallback below still applies.
                    }
                }

                for (int index = 0; index < plants.Count; index++)
                {
                    Plant candidate = plants[index];

                    if (!IsPossibleInnerPlant(candidate, shell))
                        continue;

                    try
                    {
                        if (TypeMgr.IsPumpkin(candidate.thePlantType))
                            continue;

                        if (TypeMgr.IsPot(candidate.thePlantType))
                            continue;

                        if (TypeMgr.IsLily(candidate.thePlantType))
                            continue;
                    }
                    catch
                    {
                        // PeaPumpkin's own native table remains the final
                        // authority when an unusual plant reaches AnimShoot.
                    }

                    return candidate;
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Pumpkin Podbomber] Protected plant lookup failed " +
                    "safely: " + exception.Message
                );
            }

            return null;
        }

        private static bool IsPossibleInnerPlant(
            Plant? candidate,
            PeaPumpkin shell
        )
        {
            if (candidate == null || shell == null)
                return false;

            if (candidate.gameObject == shell.gameObject)
                return false;

            if (
                candidate.thePlantColumn != shell.thePlantColumn ||
                candidate.thePlantRow != shell.thePlantRow ||
                candidate.dying ||
                candidate.thePlantHealth <= 0
            )
            {
                return false;
            }

            return true;
        }

        private void BeginVolley(PeaPumpkin shell)
        {
            explosiveBullets.Clear();
            explosiveVolley = false;

            Plant? inner = FindProtectedPeaPlant(shell);
            PlantType innerType = inner != null
                ? inner.thePlantType
                : (PlantType)(-1);

            if (
                inner == null ||
                trackedInnerPlant == null ||
                !SamePlant(inner, trackedInnerPlant)
            )
            {
                trackedInnerPlant = inner;
                trackedInnerType = innerType;
                volleyCount = 0;
            }

            if (inner == null)
                return;

            volleyCount++;
            explosiveVolley =
                innerType == PlantType.Cherryshooter ||
                volleyCount % ExplosiveVolleyFrequency == 0;

            activeVolley = this;
        }

        private void CaptureExplosiveBullet(Bullet? bullet)
        {
            if (!explosiveVolley || bullet == null)
                return;

            explosiveBullets.Add(bullet);
        }

        private void FinishVolley()
        {
            if (activeVolley == this)
                activeVolley = null;

            if (!explosiveVolley || explosiveBullets.Count == 0)
                return;

            int damage = GetExplodeOPeaDamage();

            for (int index = 0; index < explosiveBullets.Count; index++)
            {
                Bullet bullet = explosiveBullets[index];

                if (bullet == null || bullet.dying)
                    continue;

                bullet.theBulletType = BulletType.Bullet_superCherry;
                bullet.Damage = damage;
            }

            explosiveBullets.Clear();
        }

        private static int GetExplodeOPeaDamage()
        {
            try
            {
                PlantDataManager.PlantData data =
                    PlantDataManager.GetPlantData(
                        PlantType.SuperCherryShooter
                    );

                if (data != null && data.attackDamage > 0)
                    return data.attackDamage;
            }
            catch
            {
                // Registration fallback below matches the native Almanac.
            }

            return FallbackExplodeDamage;
        }

        private static void EnsureRuntimeReferences(PeaPumpkin plant)
        {
            if (!IsPumpkinPodbomber(plant))
                return;

            GameObject? nativePrefab =
                TryGetPlantPrefab(PlantType.PeaPumpkin);
            PeaPumpkin? nativePlant = nativePrefab != null
                ? nativePrefab.GetComponent<PeaPumpkin>()
                : null;
            GameObject customRoot = plant.gameObject;

            if (plant.anim == null)
            {
                Transform? mappedAnimator = MapNativeTransform(
                    customRoot,
                    nativePrefab,
                    nativePlant != null && nativePlant.anim != null
                        ? nativePlant.anim.transform
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
                ) ?? FindDescendant(customRoot.transform, "Shoot")
                    ?? customRoot.transform;
            }

            if (
                plant.shoot2 == null &&
                nativePlant != null &&
                nativePlant.shoot2 != null
            )
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
                    : LayerMask.GetMask("Bullet");
            }

            if (plant.plantLayer.value == 0)
            {
                LayerMask nativeMask = nativePlant != null
                    ? nativePlant.plantLayer
                    : default;
                plant.plantLayer = nativeMask.value != 0
                    ? nativeMask
                    : LayerMask.GetMask("Plant");
            }

            if (plant.zombieLayer.value == 0)
            {
                LayerMask nativeMask = nativePlant != null
                    ? nativePlant.zombieLayer
                    : default;
                plant.zombieLayer = nativeMask.value != 0
                    ? nativeMask
                    : LayerMask.GetMask("Zombie");
            }
        }

        private static void ConfigureRegisteredPrefab()
        {
            GameObject? customPrefab = TryGetPlantPrefab(
                (PlantType)PumpkinPodbomberID
            );

            if (customPrefab == null)
                return;

            PeaPumpkin? plant = customPrefab.GetComponent<PeaPumpkin>();

            if (plant == null)
            {
                Plugin.Logger.LogError(
                    "[Pumpkin Podbomber] Registered prefab has no " +
                    "PeaPumpkin component."
                );
                return;
            }

            EnsureRuntimeReferences(plant);

            if (!prefabBridgeLogged)
            {
                prefabBridgeLogged = true;
                Plugin.Logger.LogInfo(
                    "[Pumpkin Podbomber] Prefab bridge" +
                    " | anim = " + Describe(plant.anim) +
                    " | axis = " + Describe(plant.axis) +
                    " | shoot = " + Describe(plant.shoot) +
                    " | native template = " +
                    (TryGetPlantPrefab(PlantType.PeaPumpkin) != null
                        ? "available"
                        : "unavailable")
                );
            }
        }

        private static GameObject? TryGetPlantPrefab(PlantType type)
        {
            try
            {
                if (
                    GameAPP.resourcesManager == null ||
                    GameAPP.resourcesManager.plantPrefabs == null ||
                    !GameAPP.resourcesManager.plantPrefabs.ContainsKey(type)
                )
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

                string? path = GetRelativePath(
                    nativeReference,
                    nativeRoot.transform
                );

                if (!string.IsNullOrEmpty(path))
                {
                    Transform exact = customRoot.transform.Find(path);

                    if (exact != null)
                        return exact;
                }
            }

            return FindDescendant(
                customRoot.transform,
                nativeReference.name
            );
        }

        private static Transform? FindDescendant(
            Transform root,
            string wantedName
        )
        {
            if (root == null || string.IsNullOrEmpty(wantedName))
                return null;

            if (root.name.Equals(
                wantedName,
                StringComparison.OrdinalIgnoreCase
            ))
            {
                return root;
            }

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

        internal static void RefreshNativePlantData()
        {
            PlantType customType = (PlantType)PumpkinPodbomberID;

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
                    PlantDataManager.GetPlantData(PlantType.PeaPumpkin);

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

                if (CustomCore.PlantsAlmanac.TryGetValue(
                    customType,
                    out PlantAlmanac almanac
                ))
                {
                    almanac.cost = nativeData.cost.ToString();
                    CustomCore.PlantsAlmanac[customType] = almanac;
                }

                if (!plantDataMirrorLogged)
                {
                    plantDataMirrorLogged = true;
                    Plugin.Logger.LogInfo(
                        "[Pumpkin Podbomber] Native Pumpkin Pod PlantData " +
                        "mirrored" +
                        " | Attack interval = " +
                        nativeData.attackInterval +
                        "s | HP = " + nativeData.maxHealth +
                        " | Recharge = " + nativeData.cd +
                        "s | Cost = " + nativeData.cost
                    );
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Pumpkin Podbomber] Native PlantData was not ready; " +
                    "fallback values remain active: " + exception.Message
                );
            }
        }

        private static void DropExplodeOShooterCard(Vector2 position)
        {
            try
            {
                DroppedCard? result = Lawnf.SetDroppedCard(
                    position,
                    PlantType.SuperCherryShooter,
                    0
                );

                if (result == null)
                {
                    Plugin.Logger.LogWarning(
                        "[Pumpkin Podbomber] Shovel conversion returned " +
                        "no Explode-o-shooter seed packet."
                    );
                    return;
                }

                if (!shovelConversionLogged)
                {
                    shovelConversionLogged = true;
                    Plugin.Logger.LogInfo(
                        "[Pumpkin Podbomber] Shovel conversion active" +
                        " | Pumpkin Podbomber -> Explode-o-shooter card"
                    );
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Pumpkin Podbomber] Shovel conversion failed safely: " +
                    exception.Message
                );
            }
        }

        [HarmonyPatch(typeof(PeaPumpkin), nameof(PeaPumpkin.Awake))]
        private static class PeaPumpkin_Awake_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(PeaPumpkin __instance)
            {
                EnsureRuntimeReferences(__instance);
            }
        }

        [HarmonyPatch(typeof(PeaPumpkin), nameof(PeaPumpkin.AnimShoot))]
        private static class PeaPumpkin_AnimShoot_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(
                PeaPumpkin __instance,
                out PumpkinPodbomber? __state
            )
            {
                __state = null;

                if (!IsPumpkinPodbomber(__instance))
                    return;

                PumpkinPodbomber? behaviour =
                    __instance.gameObject.GetComponent<PumpkinPodbomber>();

                if (behaviour == null)
                    return;

                behaviour.BeginVolley(__instance);
                __state = behaviour;
            }

            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(PumpkinPodbomber? __state)
            {
                __state?.FinishVolley();
            }

            [HarmonyFinalizer]
            [HarmonyPriority(Priority.Last)]
            private static Exception? Finalizer(
                Exception? __exception,
                PumpkinPodbomber? __state
            )
            {
                if (activeVolley == __state)
                    activeVolley = null;

                return __exception;
            }
        }

        [HarmonyPatch(typeof(CreateBullet), nameof(CreateBullet.SetBullet))]
        private static class CreateBullet_SetBullet_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(
                ref BulletType theBulletType,
                bool fromEnermy,
                out bool __state
            )
            {
                __state =
                    activeVolley != null &&
                    activeVolley.explosiveVolley &&
                    !fromEnermy;

                if (!__state)
                {
                    return;
                }

                theBulletType = BulletType.Bullet_superCherry;
            }

            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(Bullet __result, bool __state)
            {
                if (__state)
                    activeVolley?.CaptureExplosiveBullet(__result);
            }
        }

        [HarmonyPatch(typeof(Shovel), nameof(Shovel.Use))]
        private static class Shovel_Use_Patch
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
            private static Exception? Finalizer(Exception? __exception)
            {
                shovelUseInProgress = false;
                return __exception;
            }
        }

        [HarmonyPatch(typeof(Plant), nameof(Plant.Die))]
        private static class Plant_Die_ShovelConversion_Patch
        {
            private readonly struct ConversionState
            {
                public ConversionState(bool convert, Vector2 position)
                {
                    Convert = convert;
                    Position = position;
                }

                public bool Convert { get; }
                public Vector2 Position { get; }
            }

            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(
                Plant __instance,
                Plant.DieReason reason,
                out ConversionState __state
            )
            {
                bool convert =
                    shovelUseInProgress &&
                    reason == Plant.DieReason.ByShovel &&
                    IsPumpkinPodbomber(__instance) &&
                    !__instance.dying;

                Vector3 worldPosition =
                    __instance.axis != null
                        ? __instance.axis.transform.position
                        : __instance.transform.position;

                __state = new ConversionState(
                    convert,
                    new Vector2(worldPosition.x, worldPosition.y)
                );
            }

            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(ConversionState __state)
            {
                if (__state.Convert)
                    DropExplodeOShooterCard(__state.Position);
            }
        }

        [HarmonyPatch(typeof(MixData), nameof(MixData.TryGetMix))]
        private static class MixData_TryGetMix_InfernoConversion_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static bool Prefix(
                PlantType a,
                PlantType b,
                ref PlantType c,
                ref bool __result
            )
            {
                PlantType alternate =
                    (PlantType)InfernoTorchflower.InfernoTorchflowerID;

                bool isReverseConversion =
                    (a == alternate && b == PlantType.TorchWood) ||
                    (a == PlantType.TorchWood && b == alternate);

                if (!isReverseConversion)
                    return true;

                // Return Infernowood without registering a parent node for it.
                // Its native Recycling parents therefore stay untouched:
                // Scorchwood + Jalapeno.
                c = PlantType.SuperTorch;
                __result = true;

                return false;
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
                if ((int)thePlantType == PumpkinPodbomberID)
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
                if ((int)thePlantType == PumpkinPodbomberID)
                {
                    __result = Lawnf.CheckPlantClass(
                        PlantType.SuperCherryShooter
                    );
                }
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
                if ((int)p == PumpkinPodbomberID)
                    __result = true;
            }
        }
    }
}
