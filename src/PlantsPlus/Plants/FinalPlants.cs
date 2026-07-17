using System;
using System.Collections.Generic;
using System.Reflection;
using CustomizeLib.BepInEx;
using HarmonyLib;
using Il2Cpp;
using Il2CppAlmanacData;
using System.Text.RegularExpressions;
using UnityEngine;

namespace PlantsPlus.Core
{
    public static class FinalPlantsBootstrap
    {
        private static bool registered;
        private static bool runtimeReady;

        public static void OnStart()
        {
            if (registered)
                return;

            registered = true;
            RegisterCeasarweed();
            RegisterSolarFirnace();
            InstallTypeFlags();
            InstallClickHandler();
            InstallFurnaceReturn();

            PlantsPlus.Plugin.Logger.LogInfo(
                "[Plants+] Final V1 plants registered | Ceasarweed = 6008 | Solar Firnace = 6009"
            );
        }

        public static void OnGameInit()
        {
            InstallTypeFlags();
            InstallClickHandler();
            InstallFurnaceReturn();
            FinalPlantsAlmanac.RefreshLoadedData();

            if (!runtimeReady)
            {
                runtimeReady = true;
                PlantsPlus.Plugin.Logger.LogInfo(
                    "[Plants+] Ceasarweed and Solar Firnace runtime bridges are ready."
                );
            }
        }

        private static void RegisterCeasarweed()
        {
            RegisterPlant<MelonCaltrop, PlantsPlus.Plants.Ceasarweed>(
                PlantsPlus.Plants.Ceasarweed.CeasarweedID,
                "ceasarweed",
                "MelonCaltropPrefab",
                "MelonCaltropPreview",
                new List<(int, int)>
                {
                    ((int)PlantType.SuperMelon, (int)PlantType.Caltrop),
                    ((int)PlantType.Caltrop, (int)PlantType.SuperMelon)
                },
                3.0f,
                0f,
                80,
                4000,
                30f,
                350,
                "Ceasarweed",
                Brown(
                    "Ceasarweed turns every missed lobbed shot into a larger " +
                    "salad barrage on its next attack."
                ) + "\n\n" +
                Brown("Usage Conditions: ") + Red("Advanced Alt") + "\n" +
                Stat("Toughness", "4000") + "\n" +
                Stat("Damage", "80 per salad / 3s") + "\n" +
                Brown("Special:") + "\n" +
                Bullet("Launches 1 + X salads, up to 10 per volley.") + "\n" +
                Bullet(
                    "X becomes the number of its salads that missed during " +
                    "the previous volley."
                ) + "\n" +
                Bullet(
                    "Any other lobbed projectile that reaches the ground " +
                    "without hitting a zombie also adds 1 to X. Released " +
                    "salads never count twice."
                ) + "\n" +
                Bullet("Every salad butters the zombie it hits for 4 seconds.") + "\n" +
                Bullet(
                    "Normal zombies and Zombonis ignore it; Gargantuars can " +
                    "still attack it."
                ),
                LoreWithConversionRecipe(
                    "\"Nobody likes me...\" complains Ceasarweed. They " +
                    "used to be a rising star in the salad industry; now, " +
                    "after five days in the fridge, all they get are " +
                    "disgusted looks. Ceasarweed insists the smell is part " +
                    "of the strategy.",
                    "Spikeweed <-> Melon-pult"
                )
            );
        }

        private static void RegisterSolarFirnace()
        {
            RegisterPlant<PineFurnace, PlantsPlus.Plants.SolarFirnace>(
                PlantsPlus.Plants.SolarFirnace.SolarFirnaceID,
                "solarfirnace",
                "PineFurnacePrefab",
                "PineFurnacePreview",
                // No normal MixData recipe: Firnace is flying and absorbs
                // the Sunflower underneath through PineFurnace.mixDic.
                new List<(int, int)>(),
                0f,
                25f,
                0,
                4000,
                30f,
                200,
                "Solar Firnace",
                Brown(
                    "Solar Firnace burns on borrowed time, producing Sun " +
                    "while allowing the player to purchase extensions."
                ) + "\n\n" +
                Stat("Toughness", "4000") + "\n" +
                Stat("Lifetime", "90 seconds") + "\n" +
                Stat("Sun Output", "25 / 25s") + "\n" +
                Brown("Special:") + "\n" +
                Bullet(
                    "Click it to spend 100 Sun and add 45 seconds to its " +
                    "remaining lifetime."
                ) + "\n" +
                Bullet(
                    "Every later extension costs 50 more Sun than the " +
                    "previous one."
                ) + "\n" +
                Bullet("When the timer reaches zero, it returns to Firnace."),
                LoreWithRecipe(
                    "Solar Firnace calls every extension a sound investment. " +
                    "Sunflower calls it a subscription. Neither of them has " +
                    "explained why the price keeps increasing, but the lawn " +
                    "keeps paying anyway.",
                    "Sunflower + Firnace"
                )
            );
        }

        private const string BrownOpen = "<color=#3D1400>";
        private const string RedOpen = "<color=#8B0000>";
        private const string Close = "</color>";

        private static string Brown(string text)
        {
            return BrownOpen + text + Close;
        }

        private static string Red(string text)
        {
            return RedOpen + text + Close;
        }

        private static string Stat(string label, string value)
        {
            return Brown(label + ": ") + Red(value);
        }

        private static string Bullet(string text)
        {
            return Red("• " + text);
        }

        private static string LoreWithRecipe(string lore, string recipe)
        {
            return Brown(lore) + "\n\n" +
                Brown("Fusion Recipe: ") + Red(recipe);
        }

        private static string LoreWithConversionRecipe(
            string lore,
            string recipe
        )
        {
            return Brown(lore) + "\n\n" +
                Brown("Conversion Recipe: ") + Red(recipe);
        }

        private static void RegisterPlant<TBase, TBehaviour>(
            int id,
            string bundleName,
            string prefabName,
            string previewName,
            List<(int, int)> recipes,
            float attackInterval,
            float produceInterval,
            int damage,
            int health,
            float cooldown,
            int cost,
            string name,
            string info,
            string introduce
        )
            where TBase : Plant
            where TBehaviour : MonoBehaviour
        {
            try
            {
                var bundle = CustomCore.GetAssetBundle(
                    Assembly.GetExecutingAssembly(),
                    "PlantsPlus.Resources.AssetBundles." + bundleName
                );

                if (bundle == null)
                {
                    PlantsPlus.Plugin.Logger.LogError(
                        "[" + name + "] AssetBundle could not be loaded: " + bundleName
                    );
                    return;
                }

                GameObject prefab = bundle.GetAsset<GameObject>(prefabName);
                GameObject preview = bundle.GetAsset<GameObject>(previewName);

                if (prefab == null || preview == null)
                {
                    PlantsPlus.Plugin.Logger.LogError(
                        "[" + name + "] Missing prefab or preview in " + bundleName
                    );
                    return;
                }

                CustomCore.RegisterCustomPlant<TBase, TBehaviour>(
                    id,
                    prefab,
                    preview,
                    recipes,
                    attackInterval,
                    produceInterval,
                    damage,
                    health,
                    cooldown,
                    cost
                );

                CustomCore.AddPlantAlmanacStrings(
                    (PlantType)id,
                    name,
                    info,
                    introduce,
                    cost
                );

                PlantsPlus.Plugin.Logger.LogInfo("[" + name + "] Registered successfully.");
            }
            catch (Exception exception)
            {
                PlantsPlus.Plugin.Logger.LogError(
                    "[" + name + "] Registration failed safely: " + exception
                );
            }
        }

        private static void InstallTypeFlags()
        {
            AddUnique(CustomCore.TypeMgrExtra.IsCaltrop, (PlantType)PlantsPlus.Plants.Ceasarweed.CeasarweedID);
            AddUnique(CustomCore.TypeMgrExtra.UncrashablePlants, (PlantType)PlantsPlus.Plants.Ceasarweed.CeasarweedID);

            AddUnique(CustomCore.TypeMgrExtra.FlyingPlants, (PlantType)PlantsPlus.Plants.SolarFirnace.SolarFirnaceID);
            AddUnique(CustomCore.TypeMgrExtra.IsFirePlant, (PlantType)PlantsPlus.Plants.SolarFirnace.SolarFirnaceID);
            AddUnique(CustomCore.TypeMgrExtra.IsNut, (PlantType)PlantsPlus.Plants.SolarFirnace.SolarFirnaceID);
            AddUnique(CustomCore.TypeMgrExtra.UncrashablePlants, (PlantType)PlantsPlus.Plants.SolarFirnace.SolarFirnaceID);
        }

        private static void AddUnique(List<PlantType> list, PlantType type)
        {
            if (list != null && !list.Contains(type))
                list.Add(type);
        }

        private static void InstallClickHandler()
        {
            PlantType type = (PlantType)PlantsPlus.Plants.SolarFirnace.SolarFirnaceID;
            CustomCore.CustomPlantClicks[type] = PlantsPlus.Plants.SolarFirnace.OnClicked;
        }

        private static void InstallFurnaceReturn()
        {
            try
            {
                if (PineFurnace.mixDic != null)
                {
                    // Firnace is a flying absorber: the plant underneath is the key.
                    PineFurnace.mixDic[PlantType.SunFlower] =
                        (PlantType)PlantsPlus.Plants.SolarFirnace.SolarFirnaceID;

                    // When Solar Firnace's purchased lifetime ends, it becomes Firnace again.
                    PineFurnace.mixDic[(PlantType)PlantsPlus.Plants.SolarFirnace.SolarFirnaceID] =
                        PlantType.PineFurnace;
                }
            }
            catch (Exception exception)
            {
                PlantsPlus.Plugin.Logger.LogWarning(
                    "[Solar Firnace] Return mapping will be retried: " + exception.Message
                );
            }
        }
    }
    internal static class FinalPlantsAlmanac
    {
        private static readonly PlantType[] Types =
        {
            (PlantType)PlantsPlus.Plants.Ceasarweed.CeasarweedID,
            (PlantType)PlantsPlus.Plants.SolarFirnace.SolarFirnaceID
        };

        internal static void RefreshLoadedData()
        {
            try
            {
                if (AlmanacDataLoader.plantDatas == null)
                    return;

                for (int index = 0; index < Types.Length; index++)
                {
                    PlantType type = Types[index];
                    if (!CustomCore.PlantsAlmanac.ContainsKey(type) ||
                        !AlmanacDataLoader.plantDatas.ContainsKey(type))
                    {
                        continue;
                    }

                    PlantAlmanac source = CustomCore.PlantsAlmanac[type];
                    PlantInfo target = AlmanacDataLoader.plantDatas[type];
                    if (target == null)
                        continue;

                    string cleanName = Regex.Replace(
                        source.name ?? string.Empty,
                        "\\([^()]*\\)",
                        string.Empty
                    ).TrimEnd();

                    target.name = cleanName + " ";
                    target.info = source.info ?? string.Empty;
                    target.introduce = source.introduce ?? string.Empty;
                    target.cost = string.Empty;
                    target.seedType = (int)type;
                }
            }
            catch (Exception exception)
            {
                PlantsPlus.Plugin.Logger.LogWarning(
                    "[Plants+] Final plant Almanac refresh failed safely: " +
                    exception.Message
                );
            }
        }

        [HarmonyPatch(
            typeof(AlmanacDataLoader),
            nameof(AlmanacDataLoader.LoadPlantData)
        )]
        private static class AlmanacDataLoader_LoadPlantData_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix()
            {
                RefreshLoadedData();
            }
        }
    }
}

namespace PlantsPlus.Plants
{
    public sealed class Ceasarweed : MonoBehaviour
    {
        public const int CeasarweedID = 6008;
        public const int MaxSalads = 10;
        public const float ButterSeconds = 4f;

        [ThreadStatic]
        private static Ceasarweed activeSpawner;

        [ThreadStatic]
        private static bool nativeAttackPass;

        private static readonly List<Ceasarweed> activePlants = new List<Ceasarweed>();
        private static readonly Dictionary<int, ProjectileRecord> projectiles =
            new Dictionary<int, ProjectileRecord>();
        private static readonly HashSet<int> externalMissesReported =
            new HashSet<int>();

        private int nextExtraShots;
        private int currentVolley;
        private int currentRemaining;
        private int currentMisses;
        private int currentExternalMisses;
        private int capturedThisVolley;
        private Sprite saladSprite;

        private sealed class ProjectileRecord
        {
            public Ceasarweed Owner;
            public int Volley;
            public bool Resolved;
        }

        public Ceasarweed(IntPtr pointer) : base(pointer) { }

        public MelonCaltrop NativePlant
        {
            get { return gameObject.GetComponent<MelonCaltrop>(); }
        }

        public void Start()
        {
            if (!activePlants.Contains(this))
                activePlants.Add(this);

            try
            {
                Transform salad = transform.Find("Salad");
                if (salad != null)
                {
                    SpriteRenderer renderer = salad.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                        saladSprite = renderer.sprite;
                }
            }
            catch (Exception exception)
            {
                PlantsPlus.Plugin.Logger.LogWarning(
                    "[Ceasarweed] Salad sprite lookup failed safely: " + exception.Message
                );
            }

            PlantsPlus.Plugin.Logger.LogInfo(
                "[Ceasarweed] Ready | first volley = 1 | maximum = 10 | butter = 4s"
            );
        }

        public void OnDestroy()
        {
            activePlants.Remove(this);
            RemoveOwnedProjectiles(this);
        }

        private static bool IsCeasarweed(Plant plant)
        {
            return plant != null && (int)plant.thePlantType == CeasarweedID;
        }

        private int BeginVolley()
        {
            currentVolley++;
            currentRemaining = 0;
            currentMisses = 0;
            currentExternalMisses = 0;
            capturedThisVolley = 0;
            return Mathf.Clamp(1 + nextExtraShots, 1, MaxSalads);
        }

        private void Capture(Bullet bullet)
        {
            if (bullet == null)
                return;

            int id = bullet.GetInstanceID();
            projectiles[id] = new ProjectileRecord
            {
                Owner = this,
                Volley = currentVolley,
                Resolved = false
            };
            currentRemaining++;
            capturedThisVolley++;
            ApplySaladVisual(bullet);
        }

        private void EndSpawning()
        {
            if (capturedThisVolley == 0)
            {
                currentRemaining = 0;
                nextExtraShots = Mathf.Clamp(currentExternalMisses, 0, MaxSalads - 1);
            }
        }

        private static void SpawnSalad(MelonCaltrop source, Zombie zombie)
        {
            CreateBullet creator = CreateBullet.Instance;
            if (creator == null || source == null || zombie == null)
                return;

            Vector3 sourcePosition = source.transform.position;
            Bullet bullet = creator.SetBullet(
                sourcePosition.x,
                sourcePosition.y + 0.35f,
                source.thePlantRow,
                BulletType.Bullet_superMelon,
                BulletMoveWay.Throw,
                false
            );

            if (bullet == null)
                return;

            bullet.from = source;
            bullet.fromType = (PlantType)CeasarweedID;
            bullet.theBulletRow = source.thePlantRow;
            bullet.Damage = 80;

            Vector3 targetPosition = zombie.transform.position;
            var position = new Il2CppSystem.Nullable<Vector2>(
                new Vector2(targetPosition.x, targetPosition.y)
            );
            var flightTime = new Il2CppSystem.Nullable<float>(0.65f);
            bullet.ThrowTo(zombie, position, flightTime);
        }

        private void ApplySaladVisual(Bullet bullet)
        {
            if (saladSprite == null || bullet == null || bullet.gameObject == null)
                return;

            try
            {
                Bullet_cabbage cabbage = bullet.gameObject.GetComponent<Bullet_cabbage>();
                if (cabbage != null && cabbage.spriteObject != null)
                {
                    SpriteRenderer renderer = cabbage.spriteObject.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                        renderer.sprite = saladSprite;
                }
            }
            catch (Exception exception)
            {
                PlantsPlus.Plugin.Logger.LogWarning(
                    "[Ceasarweed] Projectile visual fallback used: " + exception.Message
                );
            }
        }

        private static bool Resolve(Bullet bullet, bool hit)
        {
            if (bullet == null)
                return false;

            int id = bullet.GetInstanceID();
            ProjectileRecord record;
            if (!projectiles.TryGetValue(id, out record) || record == null || record.Resolved)
                return false;

            record.Resolved = true;
            projectiles.Remove(id);

            Ceasarweed owner = record.Owner;
            if (owner == null || record.Volley != owner.currentVolley)
                return true;

            if (!hit)
                owner.currentMisses++;

            owner.currentRemaining = Math.Max(0, owner.currentRemaining - 1);
            if (owner.currentRemaining == 0)
            {
                owner.nextExtraShots = Mathf.Clamp(
                    owner.currentMisses + owner.currentExternalMisses,
                    0,
                    MaxSalads - 1
                );
                owner.currentMisses = 0;
                owner.currentExternalMisses = 0;
            }

            return true;
        }

        private static void NotifyExternalMiss(Bullet bullet)
        {
            if (bullet == null || bullet._moveWay != BulletMoveWay.Throw)
                return;

            // CabbageCaltrop (Bounceweed) is the native reference for this
            // "lob reached the ground" mechanic. Salad-pult is SuperMelon,
            // not CabbageCaltrop: both Ceasarweed's own salads and native
            // Salad-pult projectiles must be excluded from the external count.
            if ((int)bullet.fromType == CeasarweedID ||
                bullet.fromType == PlantType.SuperMelon)
            {
                return;
            }

            int bulletID = bullet.GetInstanceID();
            if (!externalMissesReported.Add(bulletID))
                return;

            Ceasarweed[] snapshot = activePlants.ToArray();
            for (int index = 0; index < snapshot.Length; index++)
            {
                Ceasarweed plant = snapshot[index];
                if (plant == null)
                    continue;

                if (plant.currentRemaining > 0)
                    plant.currentExternalMisses++;
                else
                    plant.nextExtraShots = Mathf.Clamp(
                        plant.nextExtraShots + 1,
                        0,
                        MaxSalads - 1
                    );
            }
        }

        private static void RemoveOwnedProjectiles(Ceasarweed owner)
        {
            var ids = new List<int>();
            foreach (var pair in projectiles)
            {
                if (pair.Value != null && pair.Value.Owner == owner)
                    ids.Add(pair.Key);
            }
            for (int index = 0; index < ids.Count; index++)
                projectiles.Remove(ids[index]);
        }

        [HarmonyPatch(typeof(MelonCaltrop), nameof(MelonCaltrop.OnAttack))]
        private static class MelonCaltrop_OnAttack_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static bool Prefix(MelonCaltrop __instance, Zombie zombie)
            {
                if (nativeAttackPass || !IsCeasarweed(__instance))
                    return true;

                Ceasarweed behaviour = __instance.gameObject.GetComponent<Ceasarweed>();
                if (behaviour == null)
                    return true;

                int shotCount = behaviour.BeginVolley();
                nativeAttackPass = true;
                try
                {
                    // Preserve Melonweed's native launch/butter behaviour once.
                    // Repeating OnAttack only repeated that effect; it never
                    // created the Salad-pult projectile barrage.
                    __instance.OnAttack(zombie);

                    activeSpawner = behaviour;
                    for (int index = 0; index < shotCount; index++)
                        SpawnSalad(__instance, zombie);
                }
                finally
                {
                    nativeAttackPass = false;
                    activeSpawner = null;
                    behaviour.EndSpawning();
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(CreateBullet), nameof(CreateBullet.SetBullet))]
        private static class CreateBullet_SetBullet_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(Bullet __result, bool fromEnermy)
            {
                if (__result != null)
                    externalMissesReported.Remove(__result.GetInstanceID());

                if (!fromEnermy && activeSpawner != null)
                    activeSpawner.Capture(__result);
            }
        }

        [HarmonyPatch(typeof(Zombie), nameof(Zombie.TakeDamage))]
        private static class Zombie_TakeDamage_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(Zombie __instance, IDamageMaker damageFrom)
            {
                if (__instance == null || damageFrom == null)
                    return;

                Bullet bullet;
                if (!damageFrom.IsBullet(out bullet) || bullet == null)
                    return;

                if (Resolve(bullet, true))
                    __instance.Buttered(ButterSeconds, true);
            }
        }

        [HarmonyPatch(typeof(Bullet), nameof(Bullet.Die))]
        private static class Bullet_Die_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(Bullet __instance)
            {
                // Own salads are resolved through the volley tracker. For every
                // other lobbed projectile, Die() is the generic fallback that
                // catches misses from kernels, butter, cabbages, melons, etc.
                // The HitLand patches below remain as an early signal for the
                // common cabbage/melon classes; the instance-ID set prevents
                // the same projectile from counting twice.
                if (!Resolve(__instance, false) &&
                    __instance != null &&
                    __instance._moveWay == BulletMoveWay.Throw &&
                    !__instance.hit)
                {
                    NotifyExternalMiss(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(Bullet_cabbage), nameof(Bullet_cabbage.HitLand))]
        private static class BulletCabbage_HitLand_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Bullet_cabbage __instance)
            {
                if (!Resolve(__instance, false))
                    NotifyExternalMiss(__instance);
            }
        }

        [HarmonyPatch(typeof(Bullet_melon), nameof(Bullet_melon.HitLand))]
        private static class BulletMelon_HitLand_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Bullet_melon __instance)
            {
                if (!Resolve(__instance, false))
                    NotifyExternalMiss(__instance);
            }
        }

        [HarmonyPatch(typeof(Caltrop), nameof(Caltrop.KillCar))]
        private static class Caltrop_KillCar_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(Caltrop __instance)
            {
                return !IsCeasarweed(__instance);
            }
        }

        [HarmonyPatch(typeof(Lawnf), nameof(Lawnf.IsSuperPlant))]
        private static class Lawnf_IsSuperPlant_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(PlantType thePlantType, ref bool __result)
            {
                if ((int)thePlantType == CeasarweedID)
                    __result = true;
            }
        }

        [HarmonyPatch(typeof(Lawnf), nameof(Lawnf.CheckPlantClass))]
        private static class Lawnf_CheckPlantClass_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(PlantType thePlantType, ref int __result)
            {
                if ((int)thePlantType == CeasarweedID)
                    __result = Lawnf.CheckPlantClass(PlantType.MelonCaltrop);
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
                if ((int)p == CeasarweedID)
                    __result = true;
            }
        }

        [HarmonyPatch(typeof(MixData), nameof(MixData.TryGetMix))]
        private static class MixData_TryGetMix_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(
                PlantType a,
                PlantType b,
                ref PlantType c,
                ref bool __result
            )
            {
                bool swapBack =
                    ((int)a == CeasarweedID && b == PlantType.Melonpult) ||
                    ((int)b == CeasarweedID && a == PlantType.Melonpult);

                if (!swapBack)
                    return;

                c = PlantType.SuperMelon;
                __result = true;
            }
        }
    }

    public sealed class SolarFirnace : MonoBehaviour
    {
        public const int SolarFirnaceID = 6009;
        public const int InitialDuration = 90;
        public const int AddedDuration = 45;
        public const int InitialClickCost = 100;
        public const int ClickCostIncrease = 50;
        public const int SunAmount = 25;
        public const float SunInterval = 25f;

        private int clickCost = InitialClickCost;
        private float sunTimer;
        private float remainingSeconds;
        private int displayedSeconds;

        public SolarFirnace(IntPtr pointer) : base(pointer) { }

        public PineFurnace NativePlant
        {
            get { return gameObject.GetComponent<PineFurnace>(); }
        }

        public void Start()
        {
            PineFurnace plant = NativePlant;
            if (plant == null)
            {
                PlantsPlus.Plugin.Logger.LogError(
                    "[Solar Firnace] Missing PineFurnace component."
                );
                return;
            }

            remainingSeconds = InitialDuration;
            displayedSeconds = InitialDuration;

            // PineFurnace's native countdown is tick-based, not seconds. Disable it
            // and drive the 90-second lifetime ourselves with Time.deltaTime.
            plant.attributeCount = displayedSeconds;
            plant.attributeCountdown = float.MaxValue;
            plant.UpdateText();
            sunTimer = SunInterval;

            PlantsPlus.Plugin.Logger.LogInfo(
                "[Solar Firnace] Ready | duration = 90s | sun = 25/25s | first extension = 100 sun"
            );
        }

        public void Update()
        {
            PineFurnace plant = NativePlant;
            if (plant == null || plant.dying)
                return;

            float delta = Time.deltaTime;
            remainingSeconds -= delta;

            if (remainingSeconds <= 0f)
            {
                remainingSeconds = 0f;
                displayedSeconds = 0;

                // Let PineFurnace perform its own native return conversion once.
                // The mix dictionary maps Solar Firnace back to Firnace.
                plant.attributeCount = 1;
                plant.attributeCountdown = 0f;
                plant.AttributeEvent();
                return;
            }

            int seconds = (int)(remainingSeconds + 0.999f);
            if (seconds != displayedSeconds)
            {
                displayedSeconds = seconds;
                plant.attributeCount = seconds;
                plant.UpdateText();
            }

            sunTimer -= delta;
            if (sunTimer <= 0f)
            {
                sunTimer += SunInterval;
                Board board = Board.Instance;
                if (board != null)
                    board.GetSun(SunAmount, true);
            }
        }

        public static void OnClicked(Plant plant)
        {
            if (plant == null || (int)plant.thePlantType != SolarFirnaceID)
                return;

            SolarFirnace behaviour = plant.gameObject.GetComponent<SolarFirnace>();
            if (behaviour == null)
                return;

            behaviour.BuyTime(plant);
        }

        private void BuyTime(Plant plant)
        {
            Board board = Board.Instance;
            if (board == null || board.theSun < clickCost)
                return;

            board.UseSun(clickCost);
            remainingSeconds += AddedDuration;
            displayedSeconds = (int)(remainingSeconds + 0.999f);
            plant.attributeCount = displayedSeconds;
            plant.UpdateText();
            clickCost += ClickCostIncrease;

            PlantsPlus.Plugin.Logger.LogInfo(
                "[Solar Firnace] +45s bought | next cost = " + clickCost
            );
        }
    }
}
