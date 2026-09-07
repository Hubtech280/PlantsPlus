using CustomizeLib.MelonLoader;
using HarmonyLib;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class CherryStarBomberBootstrap
    {
        private static bool registered;

        internal static void OnStart()
        {
            if (registered)
                return;

            registered = true;

            try
            {
                AssetBundle? bundle = CustomCore.GetAssetBundle(
                    Assembly.GetExecutingAssembly(),
                    "PlantsPlus.Resources.AssetBundles.cherrystarbomber"
                );
                GameObject? prefab =
                    bundle?.GetAsset<GameObject>("StarfruitPrefab");
                GameObject? preview =
                    bundle?.GetAsset<GameObject>("StarfruitPreview");

                if (bundle == null || prefab == null || preview == null)
                {
                    throw new InvalidOperationException(
                        "Bundle, StarfruitPrefab or StarfruitPreview is missing."
                    );
                }

                prefab.transform.localPosition = Vector3.zero;
                prefab.transform.localRotation = Quaternion.identity;

                CustomCore.RegisterCustomPlant<
                    StarFruit,
                    Plants.CherryStarBomber
                >(
                    Plants.CherryStarBomber.CherryStarBomberID,
                    prefab,
                    preview,
                    new List<(int, int)>
                    {
                        (
                            (int)PlantType.UltimateGatling,
                            (int)PlantType.StarFruit
                        ),
                        (
                            (int)PlantType.StarFruit,
                            (int)PlantType.UltimateGatling
                        )
                    },
                    Plants.CherryStarBomber.FallbackAttackInterval,
                    0f,
                    Plants.CherryStarBomber.FallbackDamage,
                    Plants.CherryStarBomber.FallbackToughness,
                    Plants.CherryStarBomber.FallbackCardRecharge,
                    Plants.CherryStarBomber.FallbackCardCost
                );

                CustomCore.AddPlantAlmanacStrings(
                    (PlantType)Plants.CherryStarBomber.CherryStarBomberID,
                    AlmanacContent.CherryStarBomber.Name,
                    AlmanacContent.CherryStarBomber.Info,
                    AlmanacContent.CherryStarBomber.Introduce,
                    Plants.CherryStarBomber.FallbackCardCost
                );

                CustomCore.TypeMgrExtra.UncrashablePlants.Add(
                    (PlantType)Plants.CherryStarBomber.CherryStarBomberID
                );

                RegisterOdysseyBuffs();
                RegisterAsOdyssey();

                Plugin.Logger.LogInfo(
                    "[Cherry StarBomber] Registered" +
                    " | Plant ID = " +
                    Plants.CherryStarBomber.CherryStarBomberID +
                    " | Conversion = Gatling Cherrybomber + Starfruit" +
                    " | Reverse = Cherry StarBomber + Peashooter" +
                    " | Volley = 2 x 5 native Cherry Stars"
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Cherry StarBomber] Registration failed safely: " +
                    exception
                );
            }
        }

        internal static void OnGameInit()
        {
            Plants.CherryStarBomber.RefreshNativePlantData();
            RegisterAsOdyssey();
            ConfigureRegisteredPrefab();
            AlmanacCompatibility.RefreshLoadedData();
        }

        private static void RegisterOdysseyBuffs()
        {
            PlantType type =
                (PlantType)Plants.CherryStarBomber.CherryStarBomberID;

            int guidedConstellation = CustomCore.RegisterCustomBuff(
                "<color=red>[Guided Constellation]</color>\n" +
                "Cherry Stars home in on zombies.",
                BuffType.UltimateBuff,
                () => true,
                0,
                type,
                true,
                PlantType.UltimateGatling,
                1,
                default
            );
            int stellarMomentum = CustomCore.RegisterCustomBuff(
                "<color=red>[Stellar Momentum]</color>\n" +
                "After traveling 3 tiles, Cherry Stars grow slightly and " +
                "deal twice as much damage.",
                BuffType.UltimateBuff,
                () => true,
                0,
                type,
                true,
                PlantType.UltimateGatling,
                1,
                default
            );
            int rollingHelpers = CustomCore.RegisterCustomBuff(
                "<color=red>[Rolling Helpers]</color>\n" +
                "Each Jugger-nut creates a Rolling Wall-nut in its lane " +
                "every 60 seconds.",
                BuffType.UltimateBuff,
                () => true,
                0,
                PlantType.HugeWallNut,
                true,
                PlantType.HugeWallNut,
                1,
                default
            );

            CustomCore.SetCustomBuffAlmanacType(
                BuffType.UltimateBuff,
                guidedConstellation,
                AlmanacBuffType.StrongUltimate,
                PlantType.UltimateGatling
            );
            CustomCore.SetCustomBuffAlmanacType(
                BuffType.UltimateBuff,
                stellarMomentum,
                AlmanacBuffType.StrongUltimate,
                PlantType.UltimateGatling
            );
            CustomCore.SetCustomBuffAlmanacType(
                BuffType.UltimateBuff,
                rollingHelpers,
                AlmanacBuffType.StrongUltimate,
                PlantType.HugeWallNut
            );

            Plants.CherryStarBomber.SetOdysseyBuffIDs(
                guidedConstellation,
                stellarMomentum,
                rollingHelpers
            );

            Plugin.Logger.LogInfo(
                "[Cherry StarBomber] Odyssey modifiers registered" +
                " | Guided Constellation = " + guidedConstellation +
                " | Stellar Momentum = " + stellarMomentum +
                " | Rolling Helpers = " + rollingHelpers
            );
        }

        private static void RegisterAsOdyssey()
        {
            PlantType type =
                (PlantType)Plants.CherryStarBomber.CherryStarBomberID;

            try
            {
                if (!CustomCore.CustomUltimatePlants.Contains(type))
                    CustomCore.AddUltimatePlant(type);

                if (!CustomCore.CustomStrongUltimatePlants.ContainsKey(type))
                {
                    CustomCore.RegisterCustomStrongUltimatePlant(
                        type,
                        (int)TravelUnlocks.UltimateGatling
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
                    "[Cherry StarBomber] Odyssey registration active" +
                    " | Epic = true"
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Cherry StarBomber] Odyssey metadata failed " +
                    "safely: " + exception.Message
                );
            }
        }

        private static void ConfigureRegisteredPrefab()
        {
            PlantType type =
                (PlantType)Plants.CherryStarBomber.CherryStarBomberID;

            if (GameAPP.resourcesManager == null ||
                GameAPP.resourcesManager.plantPrefabs == null ||
                !GameAPP.resourcesManager.plantPrefabs.ContainsKey(type))
            {
                return;
            }

            GameObject? prefab = GameAPP.resourcesManager.plantPrefabs[type];
            StarFruit? shooter = prefab?.GetComponent<StarFruit>();

            if (shooter != null)
            {
                V11PlantsBootstrap.EnsureShooterRuntimeReferences(
                    shooter,
                    "Cherry StarBomber"
                );
            }
        }
    }
}

namespace PlantsPlus.Plants
{
    using PlantsPlus.Core;

    public sealed class CherryStarBomber : MonoBehaviour
    {
        public const int CherryStarBomberID = 6021;
        public const int FallbackDamage = 300;
        public const int FallbackToughness = 300;
        public const int FallbackCardCost = 500;
        public const float FallbackCardRecharge = 30f;
        public const float FallbackAttackInterval = 1.5f;
        public const float SecondVolleyDelay = 0.12f;

        public static int GuidedConstellationBuffID { get; private set; } = -1;
        public static int StellarMomentumBuffID { get; private set; } = -1;
        public static int RollingHelpersBuffID { get; private set; } = -1;

        private static bool nativeDataLogged;
        private float secondVolleyCountdown = -1f;
        private StarFruit? pendingSource;

        public CherryStarBomber(IntPtr pointer) : base(pointer) { }

        public static void SetOdysseyBuffIDs(
            int guidedConstellationBuffID,
            int stellarMomentumBuffID,
            int rollingHelpersBuffID
        )
        {
            GuidedConstellationBuffID = guidedConstellationBuffID;
            StellarMomentumBuffID = stellarMomentumBuffID;
            RollingHelpersBuffID = rollingHelpersBuffID;
        }

        internal static bool HasGuidedConstellation()
        {
            return HasEpicModifier(GuidedConstellationBuffID);
        }

        internal static bool HasStellarMomentum()
        {
            return HasEpicModifier(StellarMomentumBuffID);
        }

        internal static bool HasRollingHelpers()
        {
            return HasEpicModifier(RollingHelpersBuffID);
        }

        private static bool HasEpicModifier(int buffID)
        {
            if (buffID < 0)
                return false;

            try
            {
                return Lawnf.TravelUltimate((UltiBuff)buffID);
            }
            catch
            {
                return false;
            }
        }

        public void Start()
        {
            StarFruit? source = GetComponent<StarFruit>();

            if (source == null)
            {
                Plugin.Logger.LogError(
                    "[Cherry StarBomber] Start failed: no StarFruit component."
                );
                return;
            }

            string bridge = V11PlantsBootstrap.EnsureShooterRuntimeReferences(
                source,
                "Cherry StarBomber"
            );

            Plugin.Logger.LogInfo(
                "[Cherry StarBomber] Ready" +
                " | Projectile = native Bullet_cherryStar" +
                " | Volley = 5 + 5" +
                " | Origins = Shoot1..Shoot5" +
                " | Runtime bridge = " + bridge
            );
        }

        public void Update()
        {
            if (secondVolleyCountdown < 0f)
                return;

            secondVolleyCountdown -= Time.deltaTime;

            if (secondVolleyCountdown > 0f)
                return;

            StarFruit? source = pendingSource;
            pendingSource = null;
            secondVolleyCountdown = -1f;

            if (source != null && !source.dying)
                FireVolley(source);
        }

        internal void ScheduleSecondVolley(StarFruit source)
        {
            pendingSource = source;
            secondVolleyCountdown = SecondVolleyDelay;
        }

        internal static bool IsCherryStarBomber(Plant? plant)
        {
            return plant != null &&
                (int)plant.thePlantType == CherryStarBomberID;
        }

        internal static void FireVolley(StarFruit source)
        {
            if (source == null || source.dying)
                return;

            CreateBullet? creator = CreateBullet.Instance;

            if (creator == null && Board.Instance != null)
                creator = Board.Instance.GetComponent<CreateBullet>();

            if (creator == null)
            {
                Plugin.Logger.LogError(
                    "[Cherry StarBomber] Volley failed: CreateBullet is null."
                );
                return;
            }

            FireStar(source, creator, "Shoot1", BulletMoveWay.Free, 0);
            FireStar(source, creator, "Shoot2", BulletMoveWay.Free, 1);
            FireStar(source, creator, "Shoot3", BulletMoveWay.Left, 2);
            FireStar(source, creator, "Shoot4", BulletMoveWay.Free, 3);
            FireStar(source, creator, "Shoot5", BulletMoveWay.Free, 4);
        }

        private static void FireStar(
            StarFruit source,
            CreateBullet creator,
            string originName,
            BulletMoveWay moveWay,
            int shotIndex
        )
        {
            Transform? origin = source.transform.Find(originName);

            if (origin == null)
            {
                Plugin.Logger.LogWarning(
                    "[Cherry StarBomber] Missing shot origin " + originName
                );
                return;
            }

            Vector3 position = origin.position;
            Bullet bullet = creator.SetBullet(
                position.x,
                position.y,
                source.thePlantRow,
                BulletType.Bullet_cherryStar,
                moveWay,
                false
            );

            if (bullet == null)
                return;

            bullet.transform.position = position;
            bullet.transform.rotation = origin.rotation;
            bullet.theBulletType = BulletType.Bullet_cherryStar;
            bullet.from = source;
            bullet.fromType = (PlantType)CherryStarBomberID;
            bullet.theBulletRow = source.thePlantRow;
            bullet.shootingLevel = source.shootingLevel;
            bullet.Damage = source.attackDamage > 0
                ? source.attackDamage
                : FallbackDamage;

            CherryStarProjectileModifier? modifier =
                bullet.GetComponent<CherryStarProjectileModifier>();

            if (modifier == null)
            {
                modifier =
                    bullet.gameObject.AddComponent<CherryStarProjectileModifier>();
            }

            modifier.Initialize(bullet, shotIndex);
        }

        internal static void RefreshNativePlantData()
        {
            PlantType customType = (PlantType)CherryStarBomberID;

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
                    PlantDataManager.GetPlantData(PlantType.UltimateGatling);

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

                if (!nativeDataLogged)
                {
                    nativeDataLogged = true;
                    Plugin.Logger.LogInfo(
                        "[Cherry StarBomber] Gatling Cherrybomber PlantData " +
                        "mirrored" +
                        " | Damage = " + nativeData.attackDamage +
                        " | Interval = " + nativeData.attackInterval +
                        "s | HP = " + nativeData.maxHealth +
                        " | Recharge = " + nativeData.cd +
                        "s | Cost = " + nativeData.cost
                    );
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Cherry StarBomber] Native PlantData was not ready; " +
                    "fallback values remain active: " + exception.Message
                );
            }
        }

        [HarmonyPatch(typeof(StarFruit), "Shoot1")]
        private static class StarFruit_Shoot1_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static bool Prefix(
                StarFruit __instance,
                ref Bullet __result
            )
            {
                if (!IsCherryStarBomber(__instance))
                    return true;

                FireVolley(__instance);

                CherryStarBomber? behaviour =
                    __instance.GetComponent<CherryStarBomber>();
                behaviour?.ScheduleSecondVolley(__instance);

                __result = null!;
                return false;
            }
        }

        [HarmonyPatch(typeof(MixData), nameof(MixData.TryGetMix))]
        private static class MixData_TryGetMix_ReverseConversion_Patch
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
                PlantType alternate = (PlantType)CherryStarBomberID;
                bool reverse =
                    (a == alternate && b == PlantType.Peashooter) ||
                    (a == PlantType.Peashooter && b == alternate);

                if (!reverse)
                    return true;

                c = PlantType.UltimateGatling;
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
            }
        }

        // The Almanac frames the full gameplay prefab. Temporarily omit the
        // ground shadow while it creates the portrait so the shadow's large
        // bounds cannot push the plant downward. The gameplay prefab is
        // restored immediately afterwards.
        [HarmonyPatch(typeof(AlmanacPlantWindow), "SetPlant")]
        private static class AlmanacPlantWindow_SetPlant_Patch
        {
            private sealed class ShadowState
            {
                internal GameObject Object = null!;
                internal SpriteRenderer Renderer = null!;
                internal Sprite Sprite = null!;
                internal bool WasActive;
            }

            [HarmonyPrefix]
            private static void Prefix(
                PlantType thePlantType,
                out ShadowState? __state
            )
            {
                __state = null;
                if ((int)thePlantType != CherryStarBomberID ||
                    GameAPP.resourcesManager?.plantPrefabs == null ||
                    !GameAPP.resourcesManager.plantPrefabs.ContainsKey(thePlantType))
                {
                    return;
                }

                GameObject? prefab =
                    GameAPP.resourcesManager.plantPrefabs[thePlantType];
                if (prefab == null)
                    return;

                SpriteRenderer[] renderers =
                    prefab.GetComponentsInChildren<SpriteRenderer>(true);
                for (int index = 0; index < renderers.Length; index++)
                {
                    SpriteRenderer renderer = renderers[index];
                    if (renderer != null && renderer.gameObject.name.Equals(
                        "Shadow",
                        StringComparison.OrdinalIgnoreCase
                    ))
                    {
                        __state = new ShadowState
                        {
                            Object = renderer.gameObject,
                            Renderer = renderer,
                            Sprite = renderer.sprite,
                            WasActive = renderer.gameObject.activeSelf
                        };
                        // SetPlant also inspects inactive renderers, so remove
                        // the sprite bounds while it calculates the framing.
                        renderer.sprite = null;
                        renderer.gameObject.SetActive(false);
                        return;
                    }
                }
            }

            [HarmonyPostfix]
            private static void Postfix(
                AlmanacPlantWindow __instance,
                ShadowState? __state
            )
            {
                if (__state == null)
                    return;

                RestoreSource(__state);

                // The copy keeps the same hierarchy. Restore the shadow only
                // after the portrait position has already been calculated.
                GameObject? shown = __instance.showedPlant;
                if (shown == null)
                    return;

                SpriteRenderer[] renderers =
                    shown.GetComponentsInChildren<SpriteRenderer>(true);
                for (int index = 0; index < renderers.Length; index++)
                {
                    SpriteRenderer renderer = renderers[index];
                    if (renderer != null && renderer.gameObject.name.Equals(
                        "Shadow",
                        StringComparison.OrdinalIgnoreCase
                    ))
                    {
                        renderer.sprite = __state.Sprite;
                        renderer.gameObject.SetActive(__state.WasActive);
                        return;
                    }
                }
            }

            [HarmonyFinalizer]
            private static Exception? Finalizer(
                Exception? __exception,
                ShadowState? __state
            )
            {
                if (__state != null)
                    RestoreSource(__state);
                return __exception;
            }

            private static void RestoreSource(ShadowState state)
            {
                state.Renderer.sprite = state.Sprite;
                state.Object.SetActive(state.WasActive);
            }
        }

    }

    public sealed class CherryStarProjectileModifier : MonoBehaviour
    {
        private const float ThreeTileDistance = 4.5f;
        private const float ChargedVisualScale = 1.2f;

        private readonly List<Transform> visualTransforms = new();
        private readonly List<Vector3> visualBaseScales = new();
        private Bullet? bullet;
        private Zombie? target;
        private Vector3 launchPosition;
        private int baseDamage;
        private int preferredTargetIndex;
        private bool guided;
        private bool momentum;
        private bool charged;
        private bool visualsCaptured;

        public CherryStarProjectileModifier(IntPtr pointer) : base(pointer) { }

        public void Initialize(Bullet source, int shotIndex)
        {
            RestoreVisualScale();
            CaptureVisuals();

            bullet = source;
            launchPosition = source.transform.position;
            baseDamage = source.Damage;
            preferredTargetIndex = shotIndex;
            guided = CherryStarBomber.HasGuidedConstellation();
            momentum = CherryStarBomber.HasStellarMomentum();
            charged = false;
            target = null;

            if (guided)
                AcquireTarget();
        }

        public void Update()
        {
            if (bullet == null ||
                !bullet.gameObject.activeInHierarchy ||
                bullet.theBulletType != BulletType.Bullet_cherryStar ||
                (int)bullet.fromType != CherryStarBomber.CherryStarBomberID)
            {
                return;
            }

            if (guided)
            {
                if (!IsValidTarget(target))
                    AcquireTarget();

                if (IsValidTarget(target))
                {
                    bullet.targetZombie = target;
                    bullet.MoveWay = BulletMoveWay.Track;
                }
            }

            if (momentum && !charged &&
                Vector3.Distance(launchPosition, bullet.transform.position) >=
                    ThreeTileDistance)
            {
                charged = true;
                bullet.Damage = baseDamage * 2;
                ApplyChargedVisualScale();
            }
        }

        private void AcquireTarget()
        {
            var zombies = Lawnf.GetAllZombies(false);

            if (zombies == null)
            {
                target = null;
                return;
            }

            List<Zombie> candidates = new();

            for (int index = 0; index < zombies.Count; index++)
            {
                Zombie zombie = zombies[index];

                if (IsValidTarget(zombie))
                    candidates.Add(zombie);
            }

            candidates.Sort((left, right) =>
            {
                float leftDistance = Vector3.SqrMagnitude(
                    left.transform.position - transform.position
                );
                float rightDistance = Vector3.SqrMagnitude(
                    right.transform.position - transform.position
                );
                return leftDistance.CompareTo(rightDistance);
            });

            target = candidates.Count == 0
                ? null
                : candidates[preferredTargetIndex % candidates.Count];
        }

        private static bool IsValidTarget(Zombie? zombie)
        {
            return zombie != null &&
                zombie.Alive &&
                zombie.theHealth > 0 &&
                !zombie.isMindControlled;
        }

        private void CaptureVisuals()
        {
            if (visualsCaptured)
                return;

            visualTransforms.Clear();
            visualBaseScales.Clear();

            SpriteRenderer[] renderers =
                GetComponentsInChildren<SpriteRenderer>(true);

            for (int index = 0; index < renderers.Length; index++)
            {
                SpriteRenderer renderer = renderers[index];

                if (renderer == null ||
                    renderer.name.IndexOf(
                        "shadow",
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0 ||
                    visualTransforms.Contains(renderer.transform))
                {
                    continue;
                }

                visualTransforms.Add(renderer.transform);
                visualBaseScales.Add(renderer.transform.localScale);
            }

            visualsCaptured = true;
        }

        private void ApplyChargedVisualScale()
        {
            for (int index = 0; index < visualTransforms.Count; index++)
            {
                Transform visual = visualTransforms[index];

                if (visual != null)
                {
                    visual.localScale =
                        visualBaseScales[index] * ChargedVisualScale;
                }
            }
        }

        private void RestoreVisualScale()
        {
            for (int index = 0; index < visualTransforms.Count; index++)
            {
                Transform visual = visualTransforms[index];

                if (visual != null)
                    visual.localScale = visualBaseScales[index];
            }
        }
    }

    public sealed class RollingHelpersController : MonoBehaviour
    {
        private const float SpawnInterval = 60f;

        private HugeWallNut? plant;
        private float countdown = SpawnInterval;

        public RollingHelpersController(IntPtr pointer) : base(pointer) { }

        public void Start()
        {
            plant = GetComponent<HugeWallNut>();
            countdown = SpawnInterval;
        }

        public void Update()
        {
            if (plant == null || plant.dying)
                return;

            if (!CherryStarBomber.HasRollingHelpers())
            {
                countdown = SpawnInterval;
                return;
            }

            countdown -= Time.deltaTime;

            if (countdown > 0f)
                return;

            countdown = SpawnInterval;
            SpawnRollingWallNut();
        }

        private void SpawnRollingWallNut()
        {
            if (plant == null)
                return;

            CreatePlant? creator = CreatePlant.Instance;

            if (creator == null && Board.Instance != null)
                creator = Board.Instance.GetComponent<CreatePlant>();

            if (creator == null)
            {
                Plugin.Logger.LogWarning(
                    "[Rolling Helpers] Could not create Rolling Wall-nut: " +
                    "CreatePlant is null."
                );
                return;
            }

            Plant spawned = creator.SetPlant(
                plant.thePlantColumn,
                plant.thePlantRow,
                PlantType.BigWallNut,
                null,
                default,
                true,
                false,
                null
            );

            BigWallNut? rollingWallNut = spawned?.TryCast<BigWallNut>();

            if (rollingWallNut == null)
            {
                Plugin.Logger.LogWarning(
                    "[Rolling Helpers] Native Rolling Wall-nut " +
                    "could not be created."
                );
                return;
            }

            // BigWallNut is the real bowling object used by the game. Its
            // native Awake/Start path enters the Round state and owns all
            // movement, collision and damage behaviour.
            rollingWallNut.transform.position =
                plant.transform.position + new Vector3(0.65f, 0f, 0f);
            rollingWallNut.thePlantRow = plant.thePlantRow;

            Plugin.Logger.LogInfo(
                "[Rolling Helpers] Spawned native Rolling Wall-nut" +
                " | Row = " + plant.thePlantRow
            );
        }

        [HarmonyPatch(typeof(HugeWallNut), "Awake")]
        private static class HugeWallNut_Awake_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(HugeWallNut __instance)
            {
                if (__instance == null ||
                    __instance.GetComponent<RollingHelpersController>() != null)
                {
                    return;
                }

                __instance.gameObject.AddComponent<RollingHelpersController>();
            }
        }
    }
}
