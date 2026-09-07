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
        private static GameObject? wavePrefab;
        private static Vector3 waveWorldScale = Vector3.one;
        private static float waveVerticalOffset;

        internal static void OnStart()
        {
            if (registered)
                return;

            registered = true;

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
                wavePrefab = null;

                if (prefab != null)
                {
                    SpriteRenderer[] renderers =
                        prefab.GetComponentsInChildren<SpriteRenderer>(true);

                    for (int index = 0; index < renderers.Length; index++)
                    {
                        SpriteRenderer renderer = renderers[index];
                        if (renderer != null &&
                            renderer.gameObject.name.Equals(
                                "BorealWave",
                                StringComparison.OrdinalIgnoreCase
                            ))
                        {
                            // BorealWave is embedded as a disabled child of
                            // SunflowerPrefab, not as a top-level bundle asset.
                            // Keep that authored object as the visual template.
                            wavePrefab = renderer.gameObject;
                            waveWorldScale = renderer.transform.lossyScale;
                            waveVerticalOffset =
                                renderer.transform.localPosition.y *
                                prefab.transform.localScale.y;
                            break;
                        }
                    }
                }

                if (bundle == null || prefab == null || preview == null ||
                    wavePrefab == null)
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
            AlmanacCompatibility.RefreshLoadedData();
        }

        internal static void SpawnWaveVisual(Plant source)
        {
            if (source == null || wavePrefab == null)
                return;

            Board? board = Board.Instance;
            int columns = board != null
                ? Math.Max(1, board.columnNum)
                : 9;
            int rows = board != null
                ? Math.Max(1, board.rowNum)
                : 5;

            float firstColumnX = Lawnf.GetBoxXFromColumn(0);
            float lastColumnX =
                Lawnf.GetBoxXFromColumn(columns - 1);
            float rowY = Lawnf.GetBoxYFromRow(
                source.thePlantRow,
                rows
            );

            GameObject wave = UnityEngine.Object.Instantiate(wavePrefab);
            if (wave == null)
                return;

            SpriteRenderer[] waveRenderers =
                wave.GetComponentsInChildren<SpriteRenderer>(true);
            if (waveRenderers.Length == 0 ||
                waveRenderers[0] == null ||
                waveRenderers[0].sprite == null)
            {
                UnityEngine.Object.Destroy(wave);
                return;
            }

            SpriteRenderer renderer = waveRenderers[0];

            SpriteRenderer[] plantRenderers =
                source.GetComponentsInChildren<SpriteRenderer>(true);
            int highestPlantOrder = 0;
            int plantSortingLayer = renderer.sortingLayerID;

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

            float targetLaneWidth = Mathf.Abs(laneMaxX - laneMinX);
            float authoredWorldWidth =
                renderer.sprite.bounds.size.x * Mathf.Abs(waveWorldScale.x);
            float horizontalScaleMultiplier = authoredWorldWidth > 0.001f
                ? targetLaneWidth / authoredWorldWidth
                : 1f;

            wave.transform.localScale = new Vector3(
                waveWorldScale.x * horizontalScaleMultiplier,
                waveWorldScale.y,
                waveWorldScale.z
            );

            // BorealWave is the complete lane-wide visual authored in the
            // bundle. It appears centered on the row, remains stationary for
            // the effect animation, then disappears; it is not a projectile.
            Vector3 laneCenter = new Vector3(
                (laneMinX + laneMaxX) * 0.5f,
                rowY + waveVerticalOffset,
                source.transform.position.z
            );

            wave.transform.position = laneCenter;
            wave.SetActive(true);

            Plants.BorealWaveRuntime runtime =
                wave.AddComponent<Plants.BorealWaveRuntime>();
            runtime.Initialize(
                laneCenter,
                Plants.BorealOrchid.BoostDuration
            );
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
            PlantType.Gravebuster
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

        private static void EnsureBoardState()
        {
            Board? board = Board.Instance;
            if (board == activeBoard)
                return;

            activeBoard = board;
            BorealPoints = 0;
        }

        private static bool IsNightPlant(PlantType type)
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

                if (!MixData.TryGetDisMix(type, out var parents))
                    return false;

                return IsNightPlant(parents.Item1, visited, depth + 1) ||
                    IsNightPlant(parents.Item2, visited, depth + 1);
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

        public BorealWaveRuntime(IntPtr pointer) : base(pointer) { }

        internal void Initialize(
            Vector3 lanePosition,
            float lifetime
        )
        {
            duration = Mathf.Max(0.1f, lifetime);
            elapsed = 0f;
            transform.position = lanePosition;

            SpriteRenderer? spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
                spriteRenderer.enabled = true;
        }

        public void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= duration)
                UnityEngine.Object.Destroy(gameObject);
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
