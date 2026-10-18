using CustomizeLib.MelonLoader;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class BorealOrchidBootstrap
    {
        private static bool registered;
        private static bool hasWaveVisual;
        private static Transform? waveTemplate;

        internal static void OnStart()
        {
            if (registered)
                return;

            try
            {
                AssetBundle? bundle = CustomCore.GetAssetBundle(
                    Assembly.GetExecutingAssembly(),
                    "PlantsPlus.Resources.AssetBundles.boreal_orchid"
                );
                GameObject? prefab =
                    bundle?.GetAsset<GameObject>("SunflowerPrefab");
                GameObject? preview =
                    bundle?.GetAsset<GameObject>("SunflowerPreview");
                hasWaveVisual = false;

                if (prefab != null)
                {
                    Transform? waveRoot = prefab.transform.Find("BorealWave");
                    waveTemplate = waveRoot;
                    hasWaveVisual = waveRoot != null &&
                        waveRoot.GetComponentsInChildren<SpriteRenderer>(true).Length > 0;
                }

                if (bundle == null || prefab == null || preview == null ||
                    !hasWaveVisual)
                {
                    throw new InvalidOperationException(
                        "Boreal Orchid bundle, SunflowerPrefab, " +
                        "SunflowerPreview or BorealWave is missing."
                    );
                }

                prefab.transform.localPosition = Vector3.zero;
                prefab.transform.localRotation = Quaternion.identity;

                V11PlantsBootstrap.IsolateAnimationClips(
                    bundle,
                    prefab,
                    "Boreal Orchid",
                    "bo_idle",
                    "boreal_effect"
                );

                CustomCore.RegisterCustomPlant<
                    Plant,
                    Plants.BorealOrchid
                >(
                    Plants.BorealOrchid.BorealOrchidID,
                    prefab,
                    preview,
                    new List<(int, int)>(),
                    0f,
                    Plants.BorealOrchid.WaveInterval,
                    0,
                    Plants.BorealOrchid.Toughness,
                    Plants.BorealOrchid.CardRecharge,
                    Plants.BorealOrchid.CardCost
                );

                AlmanacEntry almanac = AlmanacContent.BorealOrchid;
                CustomCore.AddPlantAlmanacStrings(
                    (PlantType)Plants.BorealOrchid.BorealOrchidID,
                    almanac.Name,
                    almanac.Info,
                    almanac.Introduce,
                    Plants.BorealOrchid.CardCost
                );

                InstallTypeFlags();
                registered = true;

                Plugin.Logger.LogInfo(
                    "[Boreal Orchid] Registered" +
                    " | Plant ID = " + Plants.BorealOrchid.BorealOrchidID +
                    " | Wave interval = " +
                    Plants.BorealOrchid.WaveInterval + "s" +
                    " | Night-plant boost = 35%"
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Boreal Orchid] Registration failed safely: " +
                    exception
                );
            }
        }

        internal static void OnGameInit()
        {
            InstallTypeFlags();
            AlmanacCompatibility.RefreshLoadedData();
        }

        private static void InstallTypeFlags()
        {
            PlantType type = (PlantType)Plants.BorealOrchid.BorealOrchidID;
            if (CoreEnums.baiscPlants != null &&
                !CoreEnums.baiscPlants.Contains(type))
                CoreEnums.baiscPlants.Add(type);
            TypeData.SpecialCardPlants?.Remove(type);
        }

        internal static void AttachWaveVisual(GameObject target)
        {
            if (target == null || waveTemplate == null ||
                target.transform.Find("BorealWave") != null)
                return;
            GameObject clone = UnityEngine.Object.Instantiate(
                waveTemplate.gameObject, target.transform);
            clone.name = "BorealWave";
            clone.SetActive(false);
        }

        internal static void SpawnWaveVisual(Plant source)
        {
            if (source == null || !hasWaveVisual)
                return;

            Transform? waveTransform = source.transform.Find("BorealWave");
            if (waveTransform == null)
                return;
            GameObject wave = waveTransform.gameObject;

            Board? board = Board.Instance;
            int columns = board != null
                ? Math.Max(1, board.columnNum)
                : 9;
            float firstColumnX = Lawnf.GetBoxXFromColumn(0);
            float lastColumnX =
                Lawnf.GetBoxXFromColumn(columns - 1);

            SpriteRenderer[] waveRenderers =
                wave.GetComponentsInChildren<SpriteRenderer>(true);
            if (waveRenderers.Length == 0 ||
                waveRenderers[0] == null ||
                waveRenderers[0].sprite == null)
            {
                return;
            }

            wave.SetActive(true);

            SpriteRenderer[] plantRenderers =
                source.GetComponentsInChildren<SpriteRenderer>(true);
            int highestPlantOrder = 0;
            int plantSortingLayer = waveRenderers[0].sortingLayerID;

            for (int index = 0; index < plantRenderers.Length; index++)
            {
                SpriteRenderer plantRenderer = plantRenderers[index];
                if (plantRenderer == null)
                    continue;

                if (index == 0 ||
                    plantRenderer.sortingOrder > highestPlantOrder)
                {
                    highestPlantOrder = plantRenderer.sortingOrder;
                    plantSortingLayer = plantRenderer.sortingLayerID;
                }
            }

            // The Boreal Wave is an overlay across the affected row. Keep it
            // on the plant layer but explicitly above every plant sprite.
            for (int index = 0; index < waveRenderers.Length; index++)
            {
                SpriteRenderer waveRenderer = waveRenderers[index];
                if (waveRenderer == null)
                    continue;

                waveRenderer.enabled = true;
                waveRenderer.sortingLayerID = plantSortingLayer;
                waveRenderer.sortingOrder = highestPlantOrder + 1000 + index;
            }

            float columnSpacing = columns > 1
                ? Mathf.Abs(lastColumnX - firstColumnX) / (columns - 1)
                : 1f;
            // The visible lane extends beyond the nine planting cells on
            // both the house and zombie-entry sides. Include those margins;
            // sizing only to the cell edges leaves the wave visibly short.
            float laneMinX = firstColumnX - columnSpacing * 2.5f;
            float laneMaxX = lastColumnX + columnSpacing * 2.5f;

            // The new animation targets the BorealWave children under the
            // plant Animator. Keep that hierarchy intact while sizing it.
            Bounds visualBounds = waveRenderers[0].bounds;
            for (int index = 1; index < waveRenderers.Length; index++)
                if (waveRenderers[index] != null)
                    visualBounds.Encapsulate(waveRenderers[index].bounds);
            if (visualBounds.size.x > 0.001f)
            {
                Vector3 scale = waveTransform.localScale;
                scale.x *= Mathf.Abs(laneMaxX - laneMinX) /
                    visualBounds.size.x;
                waveTransform.localScale = scale;
            }
            visualBounds = waveRenderers[0].bounds;
            for (int index = 1; index < waveRenderers.Length; index++)
                if (waveRenderers[index] != null)
                    visualBounds.Encapsulate(waveRenderers[index].bounds);
            waveTransform.position += new Vector3(
                (laneMinX + laneMaxX) * 0.5f - visualBounds.center.x,
                0f, 0f);

            Plants.BorealWaveRuntime? runtime =
                wave.GetComponent<Plants.BorealWaveRuntime>();
            if (runtime == null)
                runtime = wave.AddComponent<Plants.BorealWaveRuntime>();
            runtime.Initialize(waveTransform.position,
                Plants.BorealOrchid.WaveAnimationDuration,
                (int)source.thePlantType == Plants.PulseShroom.ID);
        }
    }

    /// <summary>
    /// Shared Night Roof system. A wave only touches its own row. Its visible
    /// lifetime is also the exact duration of the temporary Boreal boost.
    /// Plants added later can register their own special wave callback without
    /// changing Boreal Orchid.
    /// </summary>
    internal static class BorealSystem
    {
        private const float StatMultiplier = 1.35f;

        private static readonly HashSet<PlantType> NightRoots = new()
        {
            PlantType.SmallPuff,
            PlantType.FumeShroom,
            PlantType.HypnoShroom,
            PlantType.ScaredyShroom,
            PlantType.IceShroom,
            PlantType.DoomShroom,
            PlantType.Gravebuster,
            (PlantType)Plants.BorealOrchid.BorealOrchidID,
            (PlantType)Plants.LobShroom.ID,
            PlantType.ElectricOnion,
            (PlantType)Plants.BounceShroom.ID,
            (PlantType)Plants.PulseShroom.ID,
            (PlantType)Plants.SuperaquaShroom.ID
        };

        private static readonly Dictionary<int, Action<Plant>> SpecialEffects =
            new();
        private static Board? activeBoard;

        internal static int BorealPoints { get; private set; }

        internal static void RegisterSpecialEffect(
            PlantType type,
            Action<Plant> effect
        )
        {
            if (effect != null)
                SpecialEffects[(int)type] = effect;
        }

        internal static void TriggerWave(Plant source)
        {
            if (source == null)
                return;

            EnsureBoardState();

            var plants = Lawnf.GetAllPlants();
            if (plants == null)
                return;

            int boostedNightPlants = 0;
            int convertedPlanterns = 0;

            for (int index = 0; index < plants.Count; index++)
            {
                Plant? target = plants[index];
                if (target == null || target.dying ||
                    target.thePlantRow != source.thePlantRow)
                {
                    continue;
                }

                bool isPlantern = TypeMgr.IsPlantern(target.thePlantType);
                bool isNightPlant = IsNightPlant(target.thePlantType);

                if (!isNightPlant && !isPlantern)
                    continue;

                Plants.BorealBoostMarker? marker =
                    target.GetComponent<Plants.BorealBoostMarker>();

                if (marker == null)
                {
                    marker =
                        target.gameObject.AddComponent<
                            Plants.BorealBoostMarker
                        >();
                }

                bool newlyBoosted = marker.Apply(
                    target,
                    StatMultiplier,
                    Plants.BorealOrchid.BoostDuration
                );
                if (newlyBoosted && isNightPlant)
                    boostedNightPlants++;

                if (isPlantern)
                {
                    // Boreal Points deliberately stay inside this system for
                    // now. The future Night Roof map gimmick can consume them
                    // without replacing Boreal Orchid's wave implementation.
                    BorealPoints++;
                    convertedPlanterns++;
                }

                if (SpecialEffects.TryGetValue(
                    (int)target.thePlantType,
                    out Action<Plant>? specialEffect
                ))
                {
                    specialEffect(target);
                }
            }

            Plugin.Logger.LogInfo(
                "[Boreal System] Wave resolved" +
                " | Row = " + source.thePlantRow +
                " | Newly boosted night plants = " + boostedNightPlants +
                " | Plantern Boreal Points = " + convertedPlanterns +
                " | Board total = " + BorealPoints
            );
        }

        internal static void TriggerPulse(Plant source)
        {
            if (source == null)
                return;
            EnsureBoardState();
            var plants = Lawnf.GetAllPlants();
            if (plants == null)
                return;

            for (int index = 0; index < plants.Count; index++)
            {
                Plant target = plants[index];
                if (target == null || target.dying ||
                    Math.Abs(target.thePlantColumn -
                        source.thePlantColumn) > 1 ||
                    Math.Abs(target.thePlantRow -
                        source.thePlantRow) > 1 ||
                    !IsNightPlant(target.thePlantType))
                    continue;

                Plants.BorealBoostMarker? marker =
                    target.GetComponent<Plants.BorealBoostMarker>();
                if (marker == null)
                    marker = target.gameObject.AddComponent<
                        Plants.BorealBoostMarker>();
                marker.Apply(target, StatMultiplier,
                    Plants.BorealOrchid.BoostDuration);
                if (SpecialEffects.TryGetValue((int)target.thePlantType,
                    out Action<Plant>? special))
                    special(target);
            }
        }

        private static void EnsureBoardState()
        {
            Board? board = Board.Instance;
            if (board == activeBoard)
                return;

            activeBoard = board;
            BorealPoints = 0;
        }

        internal static bool IsNightPlant(PlantType type)
        {
            return IsNightPlant(type, new HashSet<int>(), 0);
        }

        private static bool IsNightPlant(
            PlantType type,
            HashSet<int> visited,
            int depth
        )
        {
            if (NightRoots.Contains(type))
                return true;

            int rawType = (int)type;
            if (depth >= 12 || !visited.Add(rawType))
                return false;

            try
            {
                List<(int, int, int)>? customFusions =
                    CustomCore.CustomFusions;

                if (customFusions != null)
                {
                    for (int index = 0; index < customFusions.Count; index++)
                    {
                        (int target, int left, int right) =
                            customFusions[index];

                        if (target != rawType)
                            continue;

                        return IsNightPlant(
                            (PlantType)left,
                            visited,
                            depth + 1
                        ) || IsNightPlant(
                            (PlantType)right,
                            visited,
                            depth + 1
                        );
                    }
                }

                // Native MixData returns an IL2CPP ValueTuple. Reading Item1/2
                // can box an invalid native PlantType pointer and terminate the
                // whole game with AccessViolationException (not catchable here).
                // Only traverse the managed custom-fusion registry at runtime.
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}

namespace PlantsPlus.Plants
{
    using PlantsPlus.Core;

    public sealed class BorealOrchid : MonoBehaviour
    {
        public const int BorealOrchidID = 6024;
        public const int Toughness = 300;
        public const int CardCost = 150;
        public const float CardRecharge = 15f;
        public const float WaveInterval = 35f;
        public const float BoostDuration = 10f;
        internal const float WaveAnimationDuration = 2.4167f;

        private Plant? plant;
        private Animator? animator;
        private float waveCountdown = WaveInterval;
        private float returnToIdleCountdown;
        private bool animationInitialized;

        public BorealOrchid(IntPtr pointer) : base(pointer) { }

        public void Start()
        {
            plant = GetComponent<Plant>();
            animator = null;
            animationInitialized = false;
            waveCountdown = WaveInterval;
            returnToIdleCountdown = 0f;
        }

        public void Update()
        {
            if (plant == null || plant.dying || !plant.Active)
                return;

            if (!animationInitialized)
            {
                animationInitialized = true;
                animator =
                    V11PlantsBootstrap.ApplyExactLocalAnimationController(
                        gameObject,
                        "Boreal Orchid"
                    );

                if (animator != null)
                    animator.Play("Base Layer.bo_idle", 0, 0f);
            }

            float delta = Time.deltaTime;

            if (returnToIdleCountdown > 0f)
            {
                returnToIdleCountdown -= delta;
                if (returnToIdleCountdown <= 0f && animator != null)
                    animator.Play("Base Layer.bo_idle", 0, 0f);
            }

            waveCountdown -= delta;
            if (waveCountdown > 0f)
                return;

            waveCountdown += WaveInterval;
            TriggerWave();
        }

        private void TriggerWave()
        {
            if (plant == null)
                return;

            if (animator != null)
            {
                animator.Play("Base Layer.boreal_effect", 0, 0f);
                returnToIdleCountdown = WaveAnimationDuration;
            }

            BorealOrchidBootstrap.SpawnWaveVisual(plant);
            BorealSystem.TriggerWave(plant);
        }

    }

    public sealed class BorealWaveRuntime : MonoBehaviour
    {
        private float duration;
        private float elapsed;
        private bool manualAnimation;
        private Vector3 fullScale;
        private SpriteRenderer[]? renderers;

        public BorealWaveRuntime(IntPtr pointer) : base(pointer) { }

        internal void Initialize(
            Vector3 lanePosition,
            float lifetime,
            bool animateManually = false
        )
        {
            duration = Mathf.Max(0.1f, lifetime);
            elapsed = 0f;
            transform.position = lanePosition;
            manualAnimation = animateManually;
            if (manualAnimation)
            {
                fullScale = transform.localScale;
                renderers = GetComponentsInChildren<SpriteRenderer>(true);
                transform.localScale = new Vector3(
                    fullScale.x * 0.05f, fullScale.y, fullScale.z);
            }

            SpriteRenderer? spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
                spriteRenderer.enabled = true;
        }

        public void Update()
        {
            elapsed += Time.deltaTime;
            if (manualAnimation)
            {
                float progress = Mathf.Clamp01(elapsed / duration);
                transform.localScale = new Vector3(
                    fullScale.x * Mathf.Lerp(0.05f, 1f,
                        Mathf.SmoothStep(0f, 1f,
                            Mathf.Min(1f, progress * 2f))),
                    fullScale.y, fullScale.z);
                if (renderers != null)
                {
                    float alpha = Mathf.Clamp01((1f - progress) * 3f);
                    foreach (SpriteRenderer renderer in renderers)
                        if (renderer != null)
                        {
                            Color color = renderer.color;
                            color.a = alpha;
                            renderer.color = color;
                        }
                }
            }
            if (elapsed >= duration)
                gameObject.SetActive(false);
        }
    }

    public sealed class BorealBoostMarker : MonoBehaviour
    {
        private bool applied;
        private Plant? boostedPlant;
        private float remainingDuration;
        private int originalMaxHealth;
        private int originalAttackDamage;
        private int healthIncrease;
        private float originalAttackInterval;
        private float originalProduceInterval;
        private float originalMoveSpeed;
        private float originalPlantSpeed;

        public BorealBoostMarker(IntPtr pointer) : base(pointer) { }

        internal bool Apply(Plant plant, float multiplier, float duration)
        {
            if (plant == null)
                return false;

            remainingDuration = Mathf.Max(0.1f, duration);
            if (applied)
                return false;

            applied = true;
            boostedPlant = plant;

            originalMaxHealth = Math.Max(1, plant.thePlantMaxHealth);
            originalAttackDamage = plant.attackDamage;
            originalAttackInterval = plant.thePlantAttackInterval;
            originalProduceInterval = plant.thePlantProduceInterval;
            originalMoveSpeed = plant.moveSpeed;
            originalPlantSpeed = plant.thePlantSpeed;
            healthIncrease = Mathf.RoundToInt(
                originalMaxHealth * (multiplier - 1f)
            );

            plant.thePlantMaxHealth = originalMaxHealth + healthIncrease;
            plant.thePlantHealth = Math.Min(
                plant.thePlantMaxHealth,
                plant.thePlantHealth + healthIncrease
            );

            if (plant.attackDamage > 0)
            {
                plant.attackDamage = Mathf.RoundToInt(
                    plant.attackDamage * multiplier
                );
            }

            if (plant.thePlantAttackInterval > 0f)
                plant.thePlantAttackInterval /= multiplier;

            if (plant.thePlantProduceInterval > 0f)
                plant.thePlantProduceInterval /= multiplier;

            if (plant.moveSpeed > 0f)
                plant.moveSpeed *= multiplier;

            if (plant.thePlantSpeed > 0f)
                plant.thePlantSpeed *= multiplier;

            return true;
        }

        public void Update()
        {
            if (!applied)
                return;

            remainingDuration -= Time.deltaTime;
            if (remainingDuration <= 0f)
                RemoveBoost();
        }

        private void RemoveBoost()
        {
            Plant? plant = boostedPlant;
            applied = false;
            boostedPlant = null;
            remainingDuration = 0f;

            if (plant == null || plant.dying)
                return;

            plant.thePlantMaxHealth = originalMaxHealth;
            plant.thePlantHealth = Math.Min(
                originalMaxHealth,
                Math.Max(1, plant.thePlantHealth - healthIncrease)
            );
            plant.attackDamage = originalAttackDamage;
            plant.thePlantAttackInterval = originalAttackInterval;
            plant.thePlantProduceInterval = originalProduceInterval;
            plant.moveSpeed = originalMoveSpeed;
            plant.thePlantSpeed = originalPlantSpeed;
        }
    }
}
