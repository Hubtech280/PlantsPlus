using CustomizeLib.BepInEx;
using Il2Cpp;
using Il2CppInterop.Runtime;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using PlantsPlus.Core;
using PlantsPlus.Plants;

namespace PlantsPlus.Core
{
    /// <summary>
    /// Registration and native-data bridges for the first Plants+ V1.1 set.
    /// Kept separate from the V1 bootstrap so beta.32 remains an identifiable
    /// and easy-to-compare base while the new plants are tested.
    /// </summary>
    public static class V11PlantsBootstrap
    {
        private static bool registered;
        private static bool nativeDataLogged;
        private static bool notAPeaDirectShotLogged;
        private static bool notAStormDirectShotLogged;
        private static bool missingBulletCreatorLogged;
        private static readonly Dictionary<string, RuntimeAnimatorController>
            localAnimationControllers =
                new Dictionary<string, RuntimeAnimatorController>(
                    StringComparer.OrdinalIgnoreCase
                );
        private static readonly Dictionary<
            string,
            Dictionary<string, AnimationClip>
        > localAnimationClips =
            new Dictionary<
                string,
                Dictionary<string, AnimationClip>
            >(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<int> repairedAnimationInstances =
            new HashSet<int>();

        // Never damage or return a pooled native Bullet from inside
        // OnTriggerEnter2D. Either operation can re-enter native IL2CPP systems
        // while Unity is still dispatching the collision. Hits are queued and
        // fully resolved from Board.Update on the following frame.
        private sealed class PendingAttachmentState
        {
            public NotAPeaProjectile? Controller;
            public bool ShouldAttach;
            public int ZombieInstanceID;
            public Plant? SourcePlant;
            public int SourcePlantInstanceID;
            public PlantType ReportType;
            public int Damage;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
            public int Stage;
        }

        private readonly struct ZombieHealthSnapshot
        {
            public ZombieHealthSnapshot(
                int body,
                int firstArmor,
                int secondArmor
            )
            {
                Body = Mathf.Max(0, body);
                FirstArmor = Mathf.Max(0, firstArmor);
                SecondArmor = Mathf.Max(0, secondArmor);

                long total = (long)Body + FirstArmor + SecondArmor;
                Total = total > int.MaxValue
                    ? int.MaxValue
                    : (int)total;
            }

            public int Body { get; }
            public int FirstArmor { get; }
            public int SecondArmor { get; }
            public int Total { get; }
        }

        private sealed class AttachedSawState
        {
            public GameObject? Visual;
            public Vector3 VisualOffset;
            public int ZombieInstanceID;
            public Plant? SourcePlant;
            public int SourcePlantInstanceID;
            public PlantType ReportType;
            public int Damage;
            public float Elapsed;
            public float NextTick;
            public bool FirstTickLogged;
            public int VerificationStage;
            public ZombieHealthSnapshot VerificationBefore;
            public string VerificationPath = string.Empty;
        }

        private sealed class AttachedSawRendererTemplate
        {
            public string Name = string.Empty;
            public Sprite? Sprite;
            public Color Color;
            public bool FlipX;
            public bool FlipY;
            public int SortingLayerID;
            public int SortingOrder;
            public int Layer;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public Vector3 LocalScale;
        }

        private static readonly List<PendingAttachmentState>
            pendingAttachments = new List<PendingAttachmentState>();
        private static readonly List<AttachedSawState> attachedSaws =
            new List<AttachedSawState>();
        private static readonly List<AttachedSawRendererTemplate>
            attachedSawRendererTemplates =
                new List<AttachedSawRendererTemplate>();
        private static int attachedSawRootLayer;
        private static bool firstAttachmentQueuedLogged;
        private static bool firstDeferredImpactLogged;
        private static bool firstAttachmentStageLogged;
        private static bool firstAttachedSawLogged;
        private static bool firstAttachedTickLogged;

        public static void OnStart()
        {
            if (registered)
                return;

            registered = true;

            bool projectileReady = RegisterNotAPeaProjectile();

            if (projectileReady)
            {
                RegisterNotAPea();
                RegisterNotAStormCommando();
                InstallProjectileSkins();
            }
            else
            {
                Plugin.Logger.LogError(
                    "[Plants+ V1.1] Not-a-pea and Not-a-storm Commando " +
                    "were skipped because their projectile was unavailable."
                );
            }

            RegisterFrostFurflower();
            InstallTypeFlags();

            Plugin.Logger.LogInfo(
                "[Plants+ V1.1] Registration finished" +
                " | Not-a-pea = " + NotAPea.NotAPeaID +
                " | Not-a-storm Commando = " +
                NotAStormCommando.NotAStormCommandoID +
                " | Frost Furflower = " + FrostFurflower.FrostFurflowerID +
                " | Saw projectile = " +
                NotAPeaProjectile.NotAPeaBulletID
            );
        }

        public static void OnGameInit()
        {
            InstallTypeFlags();
            InstallProjectileSkins();
            RegisterNotAStormAsOdyssey(
                (PlantType)NotAStormCommando.NotAStormCommandoID
            );
            RefreshNativePlantData();
            ConfigureRegisteredShooterPrefab(
                (PlantType)NotAPea.NotAPeaID,
                "Not-a-pea"
            );
            ConfigureRegisteredShooterPrefab(
                (PlantType)NotAStormCommando.NotAStormCommandoID,
                "Not-a-storm Commando"
            );
        }

        private static bool RegisterNotAPeaProjectile()
        {
            BulletType bulletType =
                (BulletType)NotAPeaProjectile.NotAPeaBulletID;

            try
            {
                if (Enum.IsDefined(typeof(BulletType), (int)bulletType))
                {
                    Plugin.Logger.LogError(
                        "[Not-a-pea] Bullet ID collision | ID = " +
                        NotAPeaProjectile.NotAPeaBulletID +
                        " | Native bullet = " + bulletType
                    );
                    return false;
                }

                if (CustomCore.CustomBullets.ContainsKey(bulletType))
                {
                    Plugin.Logger.LogError(
                        "[Not-a-pea] Bullet ID " +
                        NotAPeaProjectile.NotAPeaBulletID +
                        " is already registered by another mod."
                    );
                    return false;
                }

                var bundle = CustomCore.GetAssetBundle(
                    Assembly.GetExecutingAssembly(),
                    "PlantsPlus.Resources.AssetBundles.bullet_notapea"
                );

                if (bundle == null)
                {
                    Plugin.Logger.LogError(
                        "[Not-a-pea] Projectile AssetBundle is null."
                    );
                    return false;
                }

                GameObject prefab = bundle.GetAsset<GameObject>("Bullet_pea");

                if (prefab == null)
                {
                    Plugin.Logger.LogError(
                        "[Not-a-pea] Projectile prefab Bullet_pea is null."
                    );
                    return false;
                }

                if (prefab.GetComponent<Collider2D>() == null)
                {
                    Plugin.Logger.LogError(
                        "[Not-a-pea] Projectile prefab has no Collider2D; " +
                        "registration was cancelled safely."
                    );
                    return false;
                }

                // The corrected bundle may still carry the old native
                // Bullet_pierce component. Remove every native bullet runtime
                // from the prefab before CustomizeLib ever clones it.
                Bullet_pierce? stalePierce =
                    prefab.GetComponent<Bullet_pierce>();

                if (stalePierce != null)
                    UnityEngine.Object.DestroyImmediate(stalePierce);

                Bullet? staleBullet = prefab.GetComponent<Bullet>();

                if (staleBullet != null)
                    UnityEngine.Object.DestroyImmediate(staleBullet);

                CacheAttachedSawVisual(prefab);

                // Keep the pooled runtime entirely native. Managed Bullet
                // subclasses become unsafe when ObjectPool reuses them from
                // SuperGatling.Shoot1. Bullet_pea owns movement and pooling;
                // NotAPeaProjectile is only a companion controller.
                CustomCore.RegisterCustomBullet<
                    Bullet_pea,
                    NotAPeaProjectile
                >(
                    bulletType,
                    prefab
                );

                bool ready = CustomCore.CustomBullets.ContainsKey(bulletType);

                if (ready)
                {
                    Plugin.Logger.LogInfo(
                        "[Not-a-pea] Saw projectile registered" +
                        " | Prefab = Bullet_pea" +
                        " | Runtime = native Bullet_pea + NotAPeaProjectile controller" +
                        " | Attach chance = " +
                        Mathf.RoundToInt(
                            NotAPeaProjectile.AttachmentChance * 100f
                        ) + "%" +
                        " | Duration = " +
                        NotAPeaProjectile.AttachmentDuration + "s"
                    );
                }

                return ready;
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Not-a-pea] Projectile registration failed safely: " +
                    exception
                );
                return false;
            }
        }

        private static void RegisterNotAPea()
        {
            if (!ValidatePlantID("Not-a-pea", NotAPea.NotAPeaID))
                return;

            RegisterPlant<PeaShooter, NotAPea>(
                NotAPea.NotAPeaID,
                "notapea",
                "PeashooterPrefab",
                "PeaShooterPreview",
                new List<(int, int)>
                {
                    (
                        (int)PlantType.Shulkflower,
                        (int)PlantType.Peashooter
                    ),
                    (
                        (int)PlantType.Peashooter,
                        (int)PlantType.Shulkflower
                    )
                },
                NotAPea.AttackInterval,
                0f,
                NotAPea.Damage,
                NotAPea.FallbackToughness,
                NotAPea.FallbackCardRecharge,
                NotAPea.FallbackCardCost,
                AlmanacContent.NotAPea
            );
        }

        private static void RegisterNotAStormCommando()
        {
            if (!ValidatePlantID(
                "Not-a-storm Commando",
                NotAStormCommando.NotAStormCommandoID
            ))
            {
                return;
            }

            bool ready = RegisterPlant<SuperGatling, NotAStormCommando>(
                NotAStormCommando.NotAStormCommandoID,
                "notastormcommando",
                "SuperGatlingPrefab",
                "SuperGatlingPreview",
                new List<(int, int)>
                {
                    (
                        (int)PlantType.Shulkflower,
                        (int)PlantType.SuperGatling
                    ),
                    (
                        (int)PlantType.SuperGatling,
                        (int)PlantType.Shulkflower
                    )
                },
                NotAStormCommando.FallbackAttackInterval,
                0f,
                NotAStormCommando.FallbackDamage,
                NotAStormCommando.FallbackToughness,
                NotAStormCommando.FallbackCardRecharge,
                NotAStormCommando.FallbackCardCost,
                AlmanacContent.NotAStormCommando
            );

            if (ready)
            {
                RegisterNotAStormAsOdyssey(
                    (PlantType)NotAStormCommando.NotAStormCommandoID
                );
            }
        }

        private static void RegisterNotAStormAsOdyssey(
            PlantType commandoType
        )
        {
            try
            {
                if (!CustomCore.CustomUltimatePlants.Contains(commandoType))
                    CustomCore.AddUltimatePlant(commandoType);

                // Register it as a full/strong Odyssey plant so it appears in
                // the native Odyssey filters and follows Odyssey restrictions.
                if (!TravelDictionary.PlantInfo.ContainsKey(commandoType))
                {
                    TravelDictionary.PlantInfo.Add(
                        commandoType,
                        new Il2CppSystem.ValueTuple<
                            Il2CppSystem.Nullable<PlantType>,
                            Il2CppSystem.Object,
                            Il2CppSystem.Object,
                            bool
                        >(
                            new Il2CppSystem.Nullable<PlantType>(commandoType),
                            null!,
                            null!,
                            true
                        )
                    );
                }
                else
                {
                    TravelDictionary.PlantInfo[commandoType].Item4 = true;
                }

                if (!TravelDictionary.allStrongUltimtePlant.Contains(
                        commandoType
                    ))
                {
                    TravelDictionary.allStrongUltimtePlant.Add(commandoType);
                }

                Plugin.Logger.LogInfo(
                    "[Not-a-storm Commando] Odyssey registration active" +
                    " | CustomUltimate = " +
                    CustomCore.CustomUltimatePlants.Contains(commandoType) +
                    " | Strong = true"
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Not-a-storm Commando] Odyssey metadata failed " +
                    "safely: " + exception.Message
                );
            }
        }

        private static void RegisterFrostFurflower()
        {
            if (!ValidatePlantID(
                "Frost Furflower",
                FrostFurflower.FrostFurflowerID
            ))
            {
                return;
            }

            RegisterPlant<SunFlower, FrostFurflower>(
                FrostFurflower.FrostFurflowerID,
                "frostfurflower",
                "SunflowerPrefab",
                "SunflowerPreview",
                new List<(int, int)>
                {
                    (
                        (int)PlantType.Thorns,
                        (int)PlantType.SunFlower
                    ),
                    (
                        (int)PlantType.SunFlower,
                        (int)PlantType.Thorns
                    )
                },
                0f,
                FrostFurflower.FallbackProduceInterval,
                0,
                FrostFurflower.FallbackToughness,
                FrostFurflower.FallbackCardRecharge,
                FrostFurflower.FallbackCardCost,
                AlmanacContent.FrostFurflower
            );
        }

        private static bool ValidatePlantID(string name, int id)
        {
            if (Enum.IsDefined(typeof(PlantType), id))
            {
                Plugin.Logger.LogError(
                    "[" + name + "] Plant ID collision | ID = " + id +
                    " | Native plant = " + (PlantType)id
                );
                return false;
            }

            PlantType type = (PlantType)id;

            if (CustomCore.CustomPlants.ContainsKey(type))
            {
                Plugin.Logger.LogError(
                    "[" + name + "] Plant ID " + id +
                    " is already registered by another mod."
                );
                return false;
            }

            return true;
        }

        private static bool RegisterPlant<TBase, TBehaviour>(
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
            AlmanacEntry almanac
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
                    Plugin.Logger.LogError(
                        "[" + almanac.Name + "] AssetBundle " +
                        bundleName + " is null."
                    );
                    return false;
                }

                GameObject prefab = bundle.GetAsset<GameObject>(prefabName);
                GameObject preview = bundle.GetAsset<GameObject>(previewName);

                if (prefab == null || preview == null)
                {
                    Plugin.Logger.LogError(
                        "[" + almanac.Name + "] Prefab or preview is null."
                    );
                    return false;
                }

                // The Not-a-pea and Frost Furflower controllers were exported
                // with external clip references. Unity therefore resolves them
                // to Magnet-o-pea and Inferno Torchflower when those bundles are
                // already loaded. Keep the controller/state machine, but replace
                // only those external motions with the clips embedded beside the
                // current prefab.
                if (id == NotAPea.NotAPeaID)
                {
                    IsolateAnimationClips(
                        bundle,
                        prefab,
                        almanac.Name,
                        "idle",
                        "shoot"
                    );
                }
                else if (id == FrostFurflower.FrostFurflowerID)
                {
                    IsolateAnimationClips(
                        bundle,
                        prefab,
                        almanac.Name,
                        "idle"
                    );
                }

                if (id == NotAStormCommando.NotAStormCommandoID)
                {
                    LowerNotAStormCommandoVisual(prefab);
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

                PlantType type = (PlantType)id;

                CustomCore.AddPlantAlmanacStrings(
                    type,
                    almanac.Name,
                    almanac.Info,
                    almanac.Introduce,
                    cost
                );

                bool ready = CustomCore.CustomPlants.ContainsKey(type);
                Plugin.Logger.LogInfo(
                    "[" + almanac.Name + "] Registered" +
                    " | ID = " + id +
                    " | Native behaviour = " + typeof(TBase).Name +
                    " | Ready = " + ready
                );
                return ready;
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[" + almanac.Name + "] Registration failed safely: " +
                    exception
                );
                return false;
            }
        }

        private static void LowerNotAStormCommandoVisual(
            GameObject prefab
        )
        {
            // The corrected Commando artwork is visually a few pixels above
            // its shadow in-game. Move only the direct visual children down;
            // keep the prefab root, board position, collider and Shadow fixed.
            // The -0.50 correction was still slightly too high in-game.
            // Use -0.60 local units in total (about -0.138 world units at
            // the prefab's 0.23 scale) while keeping Shadow/root/collider fixed.
            const float visualLocalYOffset = -0.60f;

            try
            {
                Transform root = prefab.transform;
                int shiftedChildren = 0;

                for (int index = 0; index < root.childCount; index++)
                {
                    Transform child = root.GetChild(index);
                    if (child == null)
                        continue;

                    string childName = child.gameObject != null
                        ? child.gameObject.name
                        : string.Empty;

                    if (string.Equals(
                        childName,
                        "Shadow",
                        StringComparison.OrdinalIgnoreCase
                    ))
                    {
                        continue;
                    }

                    Vector3 local = child.localPosition;
                    child.localPosition = new Vector3(
                        local.x,
                        local.y + visualLocalYOffset,
                        local.z
                    );
                    shiftedChildren++;
                }

                Plugin.Logger.LogInfo(
                    "[Not-a-storm Commando] Visual grounding corrected" +
                    " | Local Y offset = " + visualLocalYOffset +
                    " | Shifted root children = " + shiftedChildren +
                    " | Shadow/root/collider unchanged"
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Not-a-storm Commando] Visual grounding correction " +
                    "failed safely: " + exception.Message
                );
            }
        }

        internal static void IsolateAnimationClips(
            AssetBundle bundle,
            GameObject prefab,
            string plantName,
            params string[] clipNames
        )
        {
            try
            {
                // The corrected bundles keep their local clips in the prefab's
                // controller. Cache those native references before CustomizeLib
                // clones the prefab; after cloning, Unity may resolve identically
                // named external clips from another already-loaded bundle.
                Animator? animator = prefab.GetComponentInChildren<Animator>(true);
                RuntimeAnimatorController? controller = animator != null
                    ? animator.runtimeAnimatorController
                    : null;

                if (animator == null || controller == null)
                {
                    Plugin.Logger.LogWarning(
                        "[" + plantName + "] Animation isolation deferred: " +
                        "Animator or controller is missing."
                    );
                    return;
                }

                var controllerClips = controller.animationClips;
                var cached = new Dictionary<string, AnimationClip>(
                    StringComparer.OrdinalIgnoreCase
                );

                for (int requestedIndex = 0;
                     requestedIndex < clipNames.Length;
                     requestedIndex++)
                {
                    string requested = NormalizeAnimationName(
                        clipNames[requestedIndex]
                    );
                    AnimationClip? match = null;
                    int bestScore = -1;

                    if (controllerClips != null)
                    {
                        for (int clipIndex = 0;
                             clipIndex < controllerClips.Length;
                             clipIndex++)
                        {
                            AnimationClip? candidate = controllerClips[clipIndex];

                            if (candidate == null)
                                continue;

                            int score = GetAnimationClipMatchScore(
                                candidate.name,
                                requested
                            );

                            if (score > bestScore)
                            {
                                match = candidate;
                                bestScore = score;
                            }
                        }
                    }

                    if (match != null)
                        cached[requested] = match;
                }

                if (cached.Count != clipNames.Length)
                {
                    Plugin.Logger.LogWarning(
                        "[" + plantName + "] Local animation cache failed: " +
                        "one or more requested clips are missing."
                    );
                    return;
                }

                localAnimationControllers[plantName] = controller;
                localAnimationClips[plantName] = cached;

                Plugin.Logger.LogInfo(
                    "[" + plantName + "] Local animation controller cached" +
                    " | Controller = " + controller.name + "#" +
                    controller.GetInstanceID() +
                    " | Local clips = " + DescribeLocalClips(cached)
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[" + plantName + "] Animation isolation failed safely: " +
                    exception.Message
                );
            }
        }

        internal static void ApplyLocalAnimationController(
            GameObject instance,
            string plantName
        )
        {
            if (instance == null)
                return;

            int instanceID = instance.GetInstanceID();

            if (repairedAnimationInstances.Contains(instanceID))
                return;

            try
            {
                if (!localAnimationControllers.TryGetValue(
                        plantName,
                        out RuntimeAnimatorController? cachedController
                    ) ||
                    !localAnimationClips.TryGetValue(
                        plantName,
                        out Dictionary<string, AnimationClip>? cachedClips
                    ))
                {
                    return;
                }

                Animator? animator =
                    instance.GetComponentInChildren<Animator>(true);

                if (animator == null)
                    return;

                // Always rebuild from the controller embedded in this exact
                // plant bundle. The controller currently found on the cloned
                // instance may already have been resolved to another plant's
                // controller by Unity's cross-bundle dependency cache.
                RuntimeAnimatorController baseController = cachedController;
                AnimatorOverrideController replacement =
                    CreateNativeOverrideController(baseController);
                var baseClips = baseController.animationClips;
                var remaps = new List<string>();
                var mappedNames = new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

                if (baseClips != null)
                {
                    for (int index = 0; index < baseClips.Length; index++)
                    {
                        AnimationClip? externalClip = baseClips[index];

                        if (externalClip == null)
                            continue;

                        string normalized = NormalizeAnimationName(
                            externalClip.name
                        );

                        string? requestedKey = FindRequestedClipKey(
                            cachedClips,
                            normalized
                        );

                        if (requestedKey == null ||
                            !cachedClips.TryGetValue(
                                requestedKey,
                                out AnimationClip? localClip
                            ) || localClip == null)
                        {
                            continue;
                        }

                        replacement[externalClip.name] = localClip;
                        mappedNames.Add(requestedKey);
                        remaps.Add(
                            externalClip.name + " external#" +
                            externalClip.GetInstanceID() + " -> " +
                            localClip.name + " local#" +
                            localClip.GetInstanceID() + " [OK]"
                        );
                    }
                }

                replacement.name =
                    cachedController.name + " [Plants+ local clips]";
                animator.runtimeAnimatorController = replacement;
                repairedAnimationInstances.Add(instanceID);

                bool success = mappedNames.Count == cachedClips.Count;
                Plugin.Logger.LogInfo(
                    "[" + plantName + "] Runtime animation " +
                    (success ? "verified" : "FAILED") +
                    " | Plant instance = " + instanceID +
                    " | Base controller = " + baseController.name + "#" +
                    baseController.GetInstanceID() +
                    " | Assigned override = " + replacement.name + "#" +
                    replacement.GetInstanceID() +
                    " | Clip remap = " +
                    (remaps.Count > 0 ? string.Join(", ", remaps) : "none")
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[" + plantName +
                    "] Runtime animation repair failed safely: " +
                    exception.Message
                );
            }
        }

        internal static void ApplyNativeShooterControllerWithLocalClips(
            GameObject instance,
            string plantName,
            PlantType nativePlantType
        )
        {
            if (instance == null ||
                GameAPP.resourcesManager == null ||
                GameAPP.resourcesManager.plantPrefabs == null ||
                !GameAPP.resourcesManager.plantPrefabs.ContainsKey(
                    nativePlantType
                ) ||
                !localAnimationClips.TryGetValue(
                    plantName,
                    out Dictionary<string, AnimationClip>? localClips
                ))
            {
                return;
            }

            GameObject? nativePrefab =
                GameAPP.resourcesManager.plantPrefabs[nativePlantType];
            Animator? nativeAnimator =
                nativePrefab?.GetComponentInChildren<Animator>(true);
            Animator? targetAnimator =
                instance.GetComponentInChildren<Animator>(true);
            RuntimeAnimatorController? nativeController =
                nativeAnimator?.runtimeAnimatorController;

            if (targetAnimator == null || nativeController == null)
                return;

            AnimationClip? localIdle = null;
            AnimationClip? localShoot = null;
            AnimationClip? localElectric = null;

            foreach (KeyValuePair<string, AnimationClip> pair in localClips)
            {
                string key = NormalizeAnimationName(pair.Key);
                if (key.Contains("idle"))
                    localIdle = pair.Value;
                else if (key.Contains("shoot"))
                    localShoot = pair.Value;
                else if (key.Contains("electric"))
                    localElectric = pair.Value;
            }

            AnimatorOverrideController replacement =
                CreateNativeOverrideController(nativeController);
            AnimationClip[] nativeClips = nativeController.animationClips;

            for (int index = 0; index < nativeClips.Length; index++)
            {
                AnimationClip clip = nativeClips[index];
                if (clip == null)
                    continue;

                string name = NormalizeAnimationName(clip.name);
                if (name.Contains("idle") && localIdle != null)
                    replacement[clip.name] = localIdle;
                else if (name.Contains("shoot") && localShoot != null)
                    replacement[clip.name] = localShoot;
                else if (name.Contains("electric") &&
                         localElectric != null)
                    replacement[clip.name] = localElectric;
            }

            replacement.name =
                plantName + " [native " + nativePlantType + " states]";
            targetAnimator.runtimeAnimatorController = replacement;

            Plugin.Logger.LogInfo(
                "[" + plantName + "] Native targeting controller assigned" +
                " | Base = " + nativePlantType +
                " | Idle = " +
                (localIdle != null ? localIdle.name : "missing") +
                " | Shoot = " +
                (localShoot != null ? localShoot.name : "missing") +
                " | Electric = " +
                (localElectric != null ? localElectric.name : "unused")
            );
        }

        private static AnimatorOverrideController
            CreateNativeOverrideController(
                RuntimeAnimatorController controller
            )
        {
            RuntimeHelpers.RunClassConstructor(
                typeof(AnimatorOverrideController).TypeHandle
            );
            IntPtr nativeController = IL2CPP.il2cpp_object_new(
                Il2CppClassPointerStore<AnimatorOverrideController>
                    .NativeClassPtr
            );
            AnimatorOverrideController replacement =
                new AnimatorOverrideController(nativeController);
            AnimatorOverrideController.Internal_Create(
                replacement,
                controller
            );
            return replacement;
        }

        private static bool AnimationClipNameMatches(
            string candidate,
            string requested
        )
        {
            return GetAnimationClipMatchScore(candidate, requested) >= 0;
        }

        private static int GetAnimationClipMatchScore(
            string candidate,
            string requested
        )
        {
            if (string.IsNullOrEmpty(candidate) ||
                string.IsNullOrEmpty(requested))
            {
                return -1;
            }

            string normalizedCandidate = NormalizeAnimationAlias(candidate);
            string normalizedRequested = NormalizeAnimationAlias(requested);

            bool exact = string.Equals(
                normalizedCandidate,
                normalizedRequested,
                StringComparison.OrdinalIgnoreCase
            );
            bool suffix = normalizedCandidate.EndsWith(
                normalizedRequested,
                StringComparison.OrdinalIgnoreCase
            );

            if (!exact && !suffix)
                return -1;

            // Unity duplicates are deliberately used in the latest corrected
            // controllers (for example fff_idle - Copie and nap_shoot - Copie).
            // Prefer them over an older clip when both variants are present.
            string raw = NormalizeAnimationName(candidate);
            int score = exact ? 20 : 10;

            if (raw.EndsWith("copie", StringComparison.OrdinalIgnoreCase) ||
                raw.EndsWith("copy", StringComparison.OrdinalIgnoreCase))
            {
                score += 100;
            }

            return score;
        }

        private static string NormalizeAnimationAlias(string value)
        {
            string normalized = NormalizeAnimationName(value);
            bool changed;

            do
            {
                changed = false;

                if (normalized.EndsWith(
                        "copie",
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    normalized = normalized.Substring(
                        0,
                        normalized.Length - "copie".Length
                    );
                    changed = true;
                }
                else if (normalized.EndsWith(
                        "copy",
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    normalized = normalized.Substring(
                        0,
                        normalized.Length - "copy".Length
                    );
                    changed = true;
                }
            }
            while (changed && normalized.Length > 0);

            return normalized;
        }

        private static string? FindRequestedClipKey(
            Dictionary<string, AnimationClip> clips,
            string candidate
        )
        {
            foreach (string requested in clips.Keys)
            {
                if (AnimationClipNameMatches(candidate, requested))
                    return requested;
            }

            return null;
        }

        private static string DescribeLocalClips(
            Dictionary<string, AnimationClip> clips
        )
        {
            var descriptions = new List<string>();

            foreach (KeyValuePair<string, AnimationClip> pair in clips)
            {
                AnimationClip clip = pair.Value;
                descriptions.Add(
                    clip.name + "#" + clip.GetInstanceID()
                );
            }

            return descriptions.Count > 0
                ? string.Join(", ", descriptions)
                : "none";
        }

        private static string NormalizeAnimationName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace(" ", string.Empty)
                .Replace("(Clone)", string.Empty)
                .ToLowerInvariant();
        }

        internal static bool IsNotAPea(Plant? plant)
        {
            return plant != null &&
                (int)plant.thePlantType == NotAPea.NotAPeaID;
        }

        internal static bool IsNotAStormCommando(Plant? plant)
        {
            return plant != null &&
                (int)plant.thePlantType ==
                    NotAStormCommando.NotAStormCommandoID;
        }

        /// <summary>
        /// CustomizeLib attaches the native shooter component after Unity has
        /// deserialized the custom prefab. Native serialized fields such as
        /// Plant.shoot therefore remain null unless Plants+ reconstructs them.
        /// </summary>
        internal static string EnsureShooterRuntimeReferences(
            Shooter source,
            string plantName
        )
        {
            if (source == null || source.transform == null)
                return "unavailable";

            Transform root = source.transform;
            Transform? preferredOrigin = FindShotOrigin(root);

            if (preferredOrigin != null)
            {
                int currentScore = GetShotOriginScore(source.shoot);
                int preferredScore = GetShotOriginScore(preferredOrigin);

                if (source.shoot == null || preferredScore > currentScore)
                    source.shoot = preferredOrigin;
            }

            if (source.shoot == null)
                source.shoot = root;

            if (source.shoot2 == null || GetShotOriginScore(source.shoot2) <= 0)
                source.shoot2 = source.shoot;

            Transform? explicitAxis = FindDescendant(root, "axis");
            string currentAxisName = source.axis != null
                ? source.axis.name.ToLowerInvariant()
                : string.Empty;

            if (source.axis == null ||
                currentAxisName.Contains("shadow") ||
                currentAxisName.Contains("leaf") ||
                currentAxisName.Contains("tip"))
            {
                source.axis = explicitAxis ?? root;
            }

            if (source.anim == null)
                source.anim = root.GetComponentInChildren<Animator>(true);

            if (source.rb == null)
                source.rb = root.GetComponent<Rigidbody2D>();

            if (source.board == null && Board.Instance != null)
                source.board = Board.Instance;

            Transform origin = source.shoot;
            Vector3 local = origin.localPosition;
            Vector3 world = origin.position;

            return origin.name +
                " | local=(" + local.x.ToString("0.00") + "," +
                local.y.ToString("0.00") + ")" +
                " | world=(" + world.x.ToString("0.00") + "," +
                world.y.ToString("0.00") + ")" +
                " | shoot2=" +
                (source.shoot2 != null ? source.shoot2.name : "missing") +
                " | axis=" +
                (source.axis != null ? source.axis.name : "missing") +
                " | board=" +
                (source.board != null ? "ready" : "deferred") +
                " | plant=" + plantName;
        }

        internal static Bullet? CreateDirectSawShot(
            Shooter source,
            PlantType sourceType,
            string plantName,
            int fallbackDamage
        )
        {
            if (source == null)
                return null;

            EnsureShooterRuntimeReferences(source, plantName);

            Transform origin = source.shoot != null
                ? source.shoot
                : source.transform;
            CreateBullet? creator = CreateBullet.Instance;

            if (creator == null && Board.Instance != null)
                creator = Board.Instance.GetComponent<CreateBullet>();

            if (creator == null)
            {
                if (!missingBulletCreatorLogged)
                {
                    missingBulletCreatorLogged = true;
                    Plugin.Logger.LogError(
                        "[Plants+ V1.1] Direct saw shot failed: " +
                        "CreateBullet is null."
                    );
                }

                return null;
            }

            Vector3 position = origin.position;
            BulletType customType =
                (BulletType)NotAPeaProjectile.NotAPeaBulletID;
            Bullet bullet = creator.SetBullet(
                position.x + 0.1f,
                position.y,
                source.thePlantRow,
                customType,
                BulletMoveWay.MoveRight,
                false
            );

            if (bullet == null)
            {
                Plugin.Logger.LogError(
                    "[" + plantName + "] Direct saw shot failed: " +
                    "SetBullet returned null."
                );
                return null;
            }

            bullet.theBulletType = customType;
            bullet.Damage = source.attackDamage > 0
                ? source.attackDamage
                : fallbackDamage;
            bullet.shootingLevel = source.shootingLevel;
            bullet.from = source;
            bullet.fromType = sourceType;
            bullet.hitTimes = 0;
            bullet.penetrationTimes =
                NotAPeaProjectile.TraversalBudget;

            NotAPeaProjectile? behaviour =
                bullet.gameObject.GetComponent<NotAPeaProjectile>();

            if (behaviour != null)
                behaviour.ConfigureForFlight();

            bool alreadyLogged = sourceType == (PlantType)NotAPea.NotAPeaID
                ? notAPeaDirectShotLogged
                : notAStormDirectShotLogged;

            if (!alreadyLogged)
            {
                if (sourceType == (PlantType)NotAPea.NotAPeaID)
                    notAPeaDirectShotLogged = true;
                else
                    notAStormDirectShotLogged = true;

                Plugin.Logger.LogInfo(
                    "[" + plantName + "] Direct saw shot created" +
                    " | Bullet ID = " + NotAPeaProjectile.NotAPeaBulletID +
                    " | Damage = " + bullet.Damage +
                    " | Penetrations = " + bullet.penetrationTimes +
                    " | Row = " + source.thePlantRow
                );
            }

            return bullet;
        }

        internal static void BeginSawImpact(
            NotAPeaProjectile controller,
            Zombie target,
            bool shouldAttach
        )
        {
            if (controller == null || target == null)
                return;

            Bullet? bullet = controller.NativeBullet;

            if (bullet == null)
                return;

            try
            {
                Plant? sourcePlant = null;
                int sourcePlantID = 0;
                PlantType reportType = bullet.fromType;

                try
                {
                    if (bullet.from != null)
                    {
                        sourcePlant = bullet.from;
                        sourcePlantID = sourcePlant.GetInstanceID();
                        reportType = sourcePlant.thePlantType;
                    }
                }
                catch
                {
                    // fromType was copied when the projectile was created.
                }

                int baseDamage = bullet.Damage > 0
                    ? bullet.Damage
                    : NotAPeaProjectile.DamagePerTick;
                int attachedDamage = Mathf.Max(
                    1,
                    baseDamage /
                    NotAPeaProjectile.AttachedDamageDivisor
                );

                pendingAttachments.Add(new PendingAttachmentState
                {
                    Controller = controller,
                    ShouldAttach = shouldAttach,
                    ZombieInstanceID = target.GetInstanceID(),
                    SourcePlant = sourcePlant,
                    SourcePlantInstanceID = sourcePlantID,
                    ReportType = reportType,
                    Damage = attachedDamage,
                    Position = controller.transform.position,
                    Rotation = controller.transform.rotation,
                    Scale = controller.transform.lossyScale,
                    Stage = 0
                });

                if (!firstAttachmentQueuedLogged)
                {
                    firstAttachmentQueuedLogged = true;
                    Plugin.Logger.LogInfo(
                        "[Not-a-pea] Saw impact queued safely" +
                        " | Native Bullet_pea pool runtime" +
                        " | Attach roll = " +
                        (shouldAttach ? "accepted" : "rejected") +
                        " | Attached damage = " + attachedDamage +
                        " every 1.5s for 10s"
                    );
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Not-a-pea] Impact queue failed safely: " +
                    exception.Message
                );

                controller.ResumeAfterCancelledAttachment();
            }
        }

        private static void ProcessPendingAttachments()
        {
            if (pendingAttachments.Count == 0)
                return;

            var zombies = Lawnf.GetAllZombies(false);

            for (int index = pendingAttachments.Count - 1;
                 index >= 0;
                 index--)
            {
                PendingAttachmentState pending = pendingAttachments[index];
                NotAPeaProjectile? controller = pending.Controller;
                Zombie? target = FindLiveZombieByInstanceID(
                    zombies,
                    pending.ZombieInstanceID
                );

                if (controller == null)
                {
                    pendingAttachments.RemoveAt(index);
                    continue;
                }

                if (target == null)
                {
                    controller.ResumeAfterCancelledAttachment();
                    pendingAttachments.RemoveAt(index);
                    continue;
                }

                if (pending.Stage == 0)
                {
                    try
                    {
                        controller.ApplyDeferredImpactDamage(target);

                        if (!firstDeferredImpactLogged)
                        {
                            firstDeferredImpactLogged = true;
                            Plugin.Logger.LogInfo(
                                "[Not-a-pea] Deferred impact damage confirmed" +
                                " | Damage = " +
                                controller.GetImpactDamage()
                            );
                        }
                    }
                    catch (Exception exception)
                    {
                        Plugin.Logger.LogWarning(
                            "[Not-a-pea] Deferred impact failed safely: " +
                            exception.Message
                        );
                        controller.ResumeAfterCancelledAttachment();
                        pendingAttachments.RemoveAt(index);
                        continue;
                    }

                    if (!pending.ShouldAttach)
                    {
                        controller.CompleteTraversalImpact();
                        pendingAttachments.RemoveAt(index);
                        continue;
                    }

                    pending.Stage = 1;

                    if (!firstAttachmentStageLogged)
                    {
                        firstAttachmentStageLogged = true;
                        Plugin.Logger.LogInfo(
                            "[Not-a-pea] Attachment roll accepted" +
                            " | Visual creation deferred" +
                            " | Checked-out native bullet will NOT re-enter pool"
                        );
                    }

                    continue;
                }

                if (pending.Stage == 1)
                {
                    GameObject? visual = null;
                    Vector3 offset = Vector3.zero;

                    try
                    {
                        visual = CreateAttachedSawVisualFromCache(
                            pending.Position,
                            pending.Rotation,
                            pending.Scale
                        );
                        offset = pending.Position - target.transform.position;
                    }
                    catch (Exception exception)
                    {
                        if (visual != null)
                            UnityEngine.Object.Destroy(visual);

                        visual = null;
                        Plugin.Logger.LogWarning(
                            "[Not-a-pea] Deferred visual creation failed safely: " +
                            exception.Message
                        );
                    }

                    attachedSaws.Add(new AttachedSawState
                    {
                        Visual = visual,
                        VisualOffset = offset,
                        ZombieInstanceID = pending.ZombieInstanceID,
                        SourcePlant = pending.SourcePlant,
                        SourcePlantInstanceID =
                            pending.SourcePlantInstanceID,
                        ReportType = pending.ReportType,
                        Damage = pending.Damage,
                        Elapsed = 0f,
                        NextTick =
                            NotAPeaProjectile.DamageTickInterval,
                        FirstTickLogged = false,
                        VerificationStage = 0,
                        VerificationBefore = default,
                        VerificationPath = string.Empty
                    });

                    controller.RetireNativeProjectileSafely();

                    if (!firstAttachedSawLogged)
                    {
                        firstAttachedSawLogged = true;
                        Plugin.Logger.LogInfo(
                            "[Not-a-pea] Saw attachment finalized with native cleanup" +
                            " | Runtime = Bullet_pea" +
                            " | Managed Bullet subclass = none" +
                            " | Attached damage = " + pending.Damage +
                            " every 1.5s for 10s"
                        );
                    }

                    pendingAttachments.RemoveAt(index);
                    continue;
                }
            }
        }

        internal static void TickAttachedSaws()
        {
            ProcessPendingAttachments();

            if (attachedSaws.Count == 0)
                return;

            float delta = Time.deltaTime;

            if (delta <= 0f)
                return;

            var zombies = Lawnf.GetAllZombies(false);
            var plants = Lawnf.GetAllPlants();

            for (int index = attachedSaws.Count - 1;
                 index >= 0;
                 index--)
            {
                AttachedSawState state = attachedSaws[index];
                Zombie? target = FindLiveZombieByInstanceID(
                    zombies,
                    state.ZombieInstanceID
                );

                if (target == null)
                {
                    RemoveAttachedSawAt(index);
                    continue;
                }

                state.Elapsed += delta;

                GameObject? attachedVisual = state.Visual;

                if (attachedVisual != null)
                {
                    attachedVisual.transform.position =
                        target.transform.position + state.VisualOffset;
                    attachedVisual.transform.Rotate(
                        0f,
                        0f,
                        540f * delta,
                        Space.Self
                    );
                }

                if (ResolveAttachedDamageVerification(
                        state,
                        target,
                        plants
                    ))
                {
                    continue;
                }

                while (state.Elapsed >= state.NextTick &&
                       state.NextTick <=
                       NotAPeaProjectile.AttachmentDuration)
                {
                    try
                    {
                        state.VerificationBefore =
                            GetZombieHealthSnapshot(target);

                        Plant? source = state.SourcePlant;

                        if (!IsLiveSourcePlant(
                                source,
                                state.SourcePlantInstanceID
                            ))
                        {
                            source = FindLivePlantByInstanceID(
                                plants,
                                state.SourcePlantInstanceID,
                                state.ReportType
                            );
                            state.SourcePlant = source;
                        }

                        if (source != null)
                        {
                            state.VerificationPath =
                                "Entity.TakeDamage source-aware primary";

                            // This runs from Board.Update, never from the native
                            // Bullet collision callback. It therefore keeps the
                            // beta.15 crash-safe Bullet_pea architecture while
                            // restoring native hit feedback and damage numbers.
                            ((Entity)target).TakeDamage(
                                state.Damage,
                                source.ToIDamageMaker(),
                                DamageType.Normal,
                                state.ReportType,
                                false
                            );

                            state.VerificationStage = 1;
                        }
                        else
                        {
                            state.VerificationPath =
                                "Zombie.ApplyDamage source-less primary";
                            target.ApplyDamage(
                                DamageType.Normal,
                                state.Damage
                            );
                            state.VerificationStage = 2;
                        }
                    }
                    catch (Exception exception)
                    {
                        Plugin.Logger.LogWarning(
                            "[Not-a-pea] Attached source-aware damage failed safely: " +
                            exception.Message
                        );

                        try
                        {
                            state.VerificationPath +=
                                " -> Zombie.ApplyDamage emergency fallback";
                            target.ApplyDamage(
                                DamageType.Normal,
                                state.Damage
                            );
                            state.VerificationStage = 2;
                        }
                        catch (Exception fallbackException)
                        {
                            Plugin.Logger.LogWarning(
                                "[Not-a-pea] Attached emergency fallback failed safely: " +
                                fallbackException.Message
                            );
                            state.VerificationStage = 0;
                            state.VerificationPath = string.Empty;
                        }
                    }

                    state.NextTick +=
                        NotAPeaProjectile.DamageTickInterval;

                    if (state.VerificationStage > 0)
                        break;
                }

                if (state.Elapsed >=
                    NotAPeaProjectile.AttachmentDuration)
                {
                    RemoveAttachedSawAt(index);
                }
            }
        }

        private static bool ResolveAttachedDamageVerification(
            AttachedSawState state,
            Zombie target,
            Il2CppSystem.Collections.Generic.List<Plant>? plants
        )
        {
            if (state.VerificationStage <= 0 || target == null)
                return false;

            ZombieHealthSnapshot after =
                GetZombieHealthSnapshot(target);

            if (after.Total < state.VerificationBefore.Total)
            {
                LogAttachedSawVerification(
                    state,
                    state.VerificationBefore,
                    after,
                    true
                );
                state.VerificationStage = 0;
                state.VerificationPath = string.Empty;
                return false;
            }

            if (state.VerificationStage == 1)
            {
                try
                {
                    state.VerificationPath +=
                        " -> Zombie.ApplyDamage fallback";
                    target.ApplyDamage(
                        DamageType.Normal,
                        state.Damage
                    );
                    state.VerificationStage = 2;

                    // Verify the fallback on the next Board.Update frame.
                    return true;
                }
                catch (Exception exception)
                {
                    state.VerificationPath +=
                        " -> Zombie.ApplyDamage threw";
                    Plugin.Logger.LogWarning(
                        "[Not-a-pea] Attached fallback damage failed safely: " +
                        exception.Message
                    );
                }
            }

            LogAttachedSawVerification(
                state,
                state.VerificationBefore,
                after,
                false
            );
            state.VerificationStage = 0;
            state.VerificationPath = string.Empty;
            return false;
        }

        private static void LogAttachedSawVerification(
            AttachedSawState state,
            ZombieHealthSnapshot before,
            ZombieHealthSnapshot after,
            bool changed
        )
        {
            if (state.FirstTickLogged)
                return;

            state.FirstTickLogged = true;
            firstAttachedTickLogged = true;

            string message =
                "[Not-a-pea] Attached saw damage tick " +
                (changed ? "verified" : "still reports no health delta") +
                " | Requested damage = " + state.Damage +
                " | Before = " + FormatHealthSnapshot(before) +
                " | After = " + FormatHealthSnapshot(after) +
                " | Path = " + state.VerificationPath +
                " | Interval = " +
                NotAPeaProjectile.DamageTickInterval + "s";

            if (changed)
                Plugin.Logger.LogInfo(message);
            else
                Plugin.Logger.LogWarning(message);
        }

        internal static void ClearAttachedSaws()
        {
            for (int index = pendingAttachments.Count - 1;
                 index >= 0;
                 index--)
            {
                try
                {
                    PendingAttachmentState pending =
                        pendingAttachments[index];
                    NotAPeaProjectile? controller = pending.Controller;

                    if (controller != null)
                        controller.RetireNativeProjectileSafely();
                }
                catch
                {
                    // The board may already be tearing down native objects.
                }
            }

            pendingAttachments.Clear();

            for (int index = attachedSaws.Count - 1;
                 index >= 0;
                 index--)
            {
                AttachedSawState state = attachedSaws[index];

                if (state.Visual != null)
                    UnityEngine.Object.Destroy(state.Visual);
            }

            attachedSaws.Clear();
        }

        private static void CacheAttachedSawVisual(GameObject source)
        {
            attachedSawRendererTemplates.Clear();
            attachedSawRootLayer = source.layer;

            SpriteRenderer[] renderers =
                source.GetComponentsInChildren<SpriteRenderer>(true);

            for (int index = 0; index < renderers.Length; index++)
            {
                SpriteRenderer original = renderers[index];

                if (original == null || original.sprite == null)
                    continue;

                string rendererName = original.gameObject.name;

                if (rendererName.IndexOf(
                        "shadow",
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0)
                {
                    continue;
                }

                attachedSawRendererTemplates.Add(
                    new AttachedSawRendererTemplate
                    {
                        Name = rendererName,
                        Sprite = original.sprite,
                        Color = original.color,
                        FlipX = original.flipX,
                        FlipY = original.flipY,
                        SortingLayerID = original.sortingLayerID,
                        SortingOrder = original.sortingOrder,
                        Layer = original.gameObject.layer,
                        LocalPosition =
                            source.transform.InverseTransformPoint(
                                original.transform.position
                            ),
                        LocalRotation =
                            Quaternion.Inverse(source.transform.rotation) *
                            original.transform.rotation,
                        LocalScale = DivideScale(
                            original.transform.lossyScale,
                            source.transform.lossyScale
                        )
                    }
                );
            }

            Plugin.Logger.LogInfo(
                "[Not-a-pea] Attached visual cached safely" +
                " | Renderers = " + attachedSawRendererTemplates.Count +
                " | Runtime prefab traversal = disabled"
            );
        }

        private static GameObject? CreateAttachedSawVisualFromCache(
            Vector3 position,
            Quaternion rotation,
            Vector3 scale
        )
        {
            if (attachedSawRendererTemplates.Count == 0)
                return null;

            GameObject root = new GameObject("PlantsPlus_AttachedSawVisual");
            root.layer = attachedSawRootLayer;
            root.transform.position = position;
            root.transform.rotation = rotation;
            root.transform.localScale = scale;

            for (int index = 0;
                 index < attachedSawRendererTemplates.Count;
                 index++)
            {
                AttachedSawRendererTemplate template =
                    attachedSawRendererTemplates[index];

                Sprite? sprite = template.Sprite;

                if (sprite == null)
                    continue;

                GameObject child = new GameObject(
                    template.Name + " [attached visual]"
                );
                child.layer = template.Layer;
                child.transform.SetParent(root.transform, false);
                child.transform.localPosition = template.LocalPosition;
                child.transform.localRotation = template.LocalRotation;
                child.transform.localScale = template.LocalScale;

                SpriteRenderer copy = child.AddComponent<SpriteRenderer>();
                copy.sprite = sprite;
                copy.color = template.Color;
                copy.flipX = template.FlipX;
                copy.flipY = template.FlipY;
                copy.sortingLayerID = template.SortingLayerID;
                copy.sortingOrder = template.SortingOrder;
            }

            return root;
        }

        private static Vector3 DivideScale(Vector3 value, Vector3 divisor)
        {
            return new Vector3(
                Math.Abs(divisor.x) > 0.0001f
                    ? value.x / divisor.x
                    : value.x,
                Math.Abs(divisor.y) > 0.0001f
                    ? value.y / divisor.y
                    : value.y,
                Math.Abs(divisor.z) > 0.0001f
                    ? value.z / divisor.z
                    : value.z
            );
        }

        private static ZombieHealthSnapshot GetZombieHealthSnapshot(
            Zombie zombie
        )
        {
            if (zombie == null)
                return default;

            try
            {
                return new ZombieHealthSnapshot(
                    zombie.theHealth,
                    zombie.theFirstArmorHealth,
                    zombie.theSecondArmorHealth
                );
            }
            catch
            {
                return default;
            }
        }

        private static string FormatHealthSnapshot(
            ZombieHealthSnapshot snapshot
        )
        {
            return "body=" + snapshot.Body +
                ", armor1=" + snapshot.FirstArmor +
                ", armor2=" + snapshot.SecondArmor +
                ", total=" + snapshot.Total;
        }

        private static Zombie? FindLiveZombieByInstanceID(
            Il2CppSystem.Collections.Generic.List<Zombie>? zombies,
            int instanceID
        )
        {
            if (zombies == null)
                return null;

            for (int index = 0; index < zombies.Count; index++)
            {
                Zombie zombie = zombies[index];

                if (zombie == null)
                    continue;

                try
                {
                    if (zombie.GetInstanceID() == instanceID &&
                        zombie.Alive &&
                        !zombie.isMindControlled)
                    {
                        return zombie;
                    }
                }
                catch
                {
                    // The native object disappeared between snapshot and read.
                }
            }

            return null;
        }

        private static bool IsLiveSourcePlant(
            Plant? plant,
            int expectedInstanceID
        )
        {
            if (plant == null)
                return false;

            try
            {
                if (plant.thePlantHealth <= 0 ||
                    plant.dying ||
                    plant.waitingDestory)
                {
                    return false;
                }

                return expectedInstanceID == 0 ||
                    plant.GetInstanceID() == expectedInstanceID;
            }
            catch
            {
                return false;
            }
        }

        private static Plant? FindLivePlantByInstanceID(
            Il2CppSystem.Collections.Generic.List<Plant>? plants,
            int instanceID,
            PlantType fallbackType
        )
        {
            if (plants == null)
                return null;

            Plant? fallback = null;

            for (int index = 0; index < plants.Count; index++)
            {
                Plant plant = plants[index];

                if (plant == null)
                    continue;

                try
                {
                    if (plant.thePlantHealth <= 0 ||
                        plant.dying ||
                        plant.waitingDestory)
                    {
                        continue;
                    }

                    if (plant.GetInstanceID() == instanceID)
                        return plant;

                    if (fallback == null &&
                        plant.thePlantType == fallbackType)
                    {
                        fallback = plant;
                    }
                }
                catch
                {
                    // Ignore a plant removed during this frame.
                }
            }

            return fallback;
        }

        private static void RemoveAttachedSawAt(int index)
        {
            AttachedSawState state = attachedSaws[index];

            if (state.Visual != null)
                UnityEngine.Object.Destroy(state.Visual);

            attachedSaws.RemoveAt(index);
        }

        internal static void ConfigureRegisteredShooterPrefab(
            PlantType customType,
            string plantName
        )
        {
            try
            {
                if (
                    GameAPP.resourcesManager == null ||
                    GameAPP.resourcesManager.plantPrefabs == null ||
                    !GameAPP.resourcesManager.plantPrefabs.ContainsKey(customType)
                )
                {
                    return;
                }

                GameObject? prefab =
                    GameAPP.resourcesManager.plantPrefabs[customType];

                if (prefab == null)
                    return;

                Shooter? shooter = prefab.GetComponent<Shooter>();

                if (shooter == null)
                    return;

                string bridge = EnsureShooterRuntimeReferences(
                    shooter,
                    plantName
                );
                ApplyLocalAnimationController(prefab, plantName);
                Plugin.Logger.LogInfo(
                    "[" + plantName + "] Prefab shooter bridge | " + bridge
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[" + plantName + "] Prefab shooter bridge deferred: " +
                    exception.Message
                );
            }
        }

        private static Transform? FindShotOrigin(Transform root)
        {
            if (root == null)
                return null;

            Transform? best = null;
            int bestScore = 0;
            FindBestShotOrigin(root, ref best, ref bestScore);
            return best;
        }

        private static void FindBestShotOrigin(
            Transform current,
            ref Transform? best,
            ref int bestScore
        )
        {
            if (current == null)
                return;

            int score = GetShotOriginScore(current);

            if (score > bestScore)
            {
                best = current;
                bestScore = score;
            }

            for (int index = 0; index < current.childCount; index++)
            {
                FindBestShotOrigin(
                    current.GetChild(index),
                    ref best,
                    ref bestScore
                );
            }
        }

        private static int GetShotOriginScore(Transform? candidate)
        {
            if (candidate == null)
                return int.MinValue;

            string name = candidate.name
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace(" ", string.Empty)
                .ToLowerInvariant();

            if (name.Contains("shadow") ||
                name.Contains("leaf") ||
                name.Contains("tip") ||
                name.Contains("controller") ||
                name.Contains("preview") ||
                name.Contains("card"))
            {
                return -1000;
            }

            if (name == "shoot")
                return 1000;
            if (name == "muzzle")
                return 950;
            if (name == "firepoint")
                return 900;
            if (name == "bulletspawn")
                return 850;
            if (name == "shoot1" || name == "shoot2")
                return 800;
            if (name.StartsWith("shoot"))
                return 600;
            if (name.Contains("muzzle") || name.Contains("firepoint"))
                return 500;
            if (name.Contains("bulletspawn"))
                return 450;

            return 0;
        }

        private static Transform? FindDescendant(
            Transform root,
            string childName
        )
        {
            if (root == null)
                return null;

            if (string.Equals(
                root.name,
                childName,
                StringComparison.OrdinalIgnoreCase
            ))
            {
                return root;
            }

            for (int index = 0; index < root.childCount; index++)
            {
                Transform? result = FindDescendant(
                    root.GetChild(index),
                    childName
                );

                if (result != null)
                    return result;
            }

            return null;
        }

        private static void InstallProjectileSkins()
        {
            BulletType customBullet =
                (BulletType)NotAPeaProjectile.NotAPeaBulletID;
            PlantType notAPeaType = (PlantType)NotAPea.NotAPeaID;
            PlantType commandoType =
                (PlantType)NotAStormCommando.NotAStormCommandoID;

            if (!CustomCore.CustomBullets.ContainsKey(customBullet))
                return;

            if (CustomCore.CustomPlants.ContainsKey(notAPeaType))
            {
                SetProjectileSkin(
                    notAPeaType,
                    BulletType.Bullet_pea,
                    customBullet
                );
            }

            if (CustomCore.CustomPlants.ContainsKey(commandoType))
            {
                Array nativeBullets = Enum.GetValues(typeof(BulletType));

                for (int index = 0; index < nativeBullets.Length; index++)
                {
                    BulletType nativeBullet =
                        (BulletType)nativeBullets.GetValue(index)!;
                    SetProjectileSkin(
                        commandoType,
                        nativeBullet,
                        customBullet
                    );
                }
            }
        }

        private static void SetProjectileSkin(
            PlantType plant,
            BulletType nativeBullet,
            BulletType customBullet
        )
        {
            var key = new ValueTuple<PlantType, BulletType>(
                plant,
                nativeBullet
            );

            CustomCore.CustomBulletsSkinID[key] = new List<BulletType>
            {
                customBullet
            };
        }

        private static void InstallTypeFlags()
        {
            PlantType notAPea = (PlantType)NotAPea.NotAPeaID;
            PlantType commando =
                (PlantType)NotAStormCommando.NotAStormCommandoID;
            PlantType frost = (PlantType)FrostFurflower.FrostFurflowerID;

            AddUnique(CustomCore.TypeMgrExtra.UncrashablePlants, notAPea);
            AddUnique(CustomCore.TypeMgrExtra.UncrashablePlants, commando);
            AddUnique(CustomCore.TypeMgrExtra.UncrashablePlants, frost);
            AddUnique(CustomCore.TypeMgrExtra.IsIcePlant, frost);
        }

        private static void AddUnique(List<PlantType> list, PlantType type)
        {
            if (list != null && !list.Contains(type))
                list.Add(type);
        }

        private static void RefreshNativePlantData()
        {
            bool first = MirrorNativeData(
                (PlantType)NotAPea.NotAPeaID,
                PlantType.Peashooter,
                false,
                false
            );
            bool second = MirrorNativeData(
                (PlantType)NotAStormCommando.NotAStormCommandoID,
                PlantType.SuperGatling,
                true,
                false
            );
            bool third = MirrorNativeData(
                (PlantType)FrostFurflower.FrostFurflowerID,
                PlantType.SunFlower,
                false,
                true
            );

            if (!nativeDataLogged && first && second && third)
            {
                nativeDataLogged = true;
                Plugin.Logger.LogInfo(
                    "[Plants+ V1.1] Native card data mirrored" +
                    " | Peashooter + SuperGatling + Sunflower"
                );
            }
        }

        private static bool MirrorNativeData(
            PlantType customType,
            PlantType nativeType,
            bool mirrorAttack,
            bool mirrorProduction
        )
        {
            try
            {
                if (!CustomCore.CustomPlants.TryGetValue(
                    customType,
                    out CustomPlantData customData
                ))
                {
                    return false;
                }

                PlantDataManager.PlantData nativeData =
                    PlantDataManager.GetPlantData(nativeType);
                PlantDataManager.PlantData target = customData.PlantData;

                if (nativeData == null || target == null)
                    return false;

                int customCost = target.cost;
                target.thePlantType = customType;
                target.maxHealth = nativeData.maxHealth;
                target.cd = nativeData.cd;

                if (mirrorAttack)
                {
                    target.attackInterval = nativeData.attackInterval;
                    target.attackDamage = nativeData.attackDamage;
                }

                if (mirrorProduction)
                    target.produceInterval = nativeData.produceInterval;

                // These are fusion cards, not aliases of the base cards.
                // Preserve their own Sun costs while mirroring runtime stats.
                target.cost = customCost;
                customData.PlantData = target;
                CustomCore.CustomPlants[customType] = customData;
                return true;
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Plants+ V1.1] Native PlantData mirror deferred for " +
                    customType + ": " + exception.Message
                );
                return false;
            }
        }
    }
}

namespace PlantsPlus.Plants
{
    public sealed class NotAPea : MonoBehaviour
    {
        public const int NotAPeaID = 6010;
        public const int Damage = 20;
        public const float AttackInterval = 1.5f;
        public const int FallbackToughness = 300;
        public const float FallbackCardRecharge = 7.5f;
        public const int FallbackCardCost = 125;

        // The bundle's Shoot layer contains nap_shoot - Copie, but its state
        // is intentionally disconnected from Nothing. Triggering the bool/trigger
        // named "shoot" can therefore never enter it. Play the state directly,
        // keep the event-based shot, and retain the one-shot fallback.
        private const int ShootLayerIndex = 1;
        private const float ShootAnimationDuration = 1.0f;
        private const float AnimationEventFallbackDelay = 0.9f;
        private const float ShootLayerResetDelay = 1.05f;

        private static readonly string[] ShootStateCandidates =
        {
            "Shoot.nap_shoot - Copie",
            "nap_shoot - Copie",
            "Shoot.nap_shoot",
            "nap_shoot"
        };

        private static readonly string[] NothingStateCandidates =
        {
            "Shoot.Nothing",
            "Nothing"
        };

        private PeaShooter? shooter;
        private Animator? animator;
        private float nextAttackTime;
        private float fallbackShotTime;
        private float shootLayerResetTime;
        private bool animationShotArmed;
        private bool shootLayerResetPending;
        private bool fallbackLogged;
        private bool directAnimationLogged;

        public NotAPea(IntPtr pointer) : base(pointer) { }

        public void Start()
        {
            V11PlantsBootstrap.ApplyLocalAnimationController(
                gameObject,
                "Not-a-pea"
            );
            shooter = gameObject.GetComponent<PeaShooter>();
            animator = gameObject.GetComponentInChildren<Animator>(true);
            string bridge = shooter != null
                ? V11PlantsBootstrap.EnsureShooterRuntimeReferences(
                    shooter,
                    "Not-a-pea"
                )
                : "missing PeaShooter";
            nextAttackTime = Time.time + 0.25f;

            Plugin.Logger.LogInfo(
                "[Not-a-pea] Ready" +
                " | Damage = " + Damage + "/" + AttackInterval + "s" +
                " | Local nap_shoot event + direct fallback active" +
                " | Runtime bridge = " + bridge
            );
        }

        public void Update()
        {
            PeaShooter? current = shooter;

            if (current == null)
            {
                current = gameObject.GetComponent<PeaShooter>();
                shooter = current;
            }

            if (current == null)
                return;

            if (shootLayerResetPending && Time.time >= shootLayerResetTime)
            {
                ResetShootLayer();
            }

            if (animationShotArmed && Time.time >= fallbackShotTime)
            {
                FireFallback(current);
                return;
            }

            if (animationShotArmed || Time.time < nextAttackTime)
                return;

            if (!HasEnemyInRow(current))
            {
                nextAttackTime = Time.time + 0.15f;
                return;
            }

            animationShotArmed = true;
            fallbackShotTime = Time.time + AnimationEventFallbackDelay;
            nextAttackTime = Time.time + AttackInterval;

            Animator? currentAnimator = animator;

            if (currentAnimator == null)
            {
                currentAnimator = gameObject.GetComponentInChildren<Animator>(
                    true
                );
                animator = currentAnimator;
            }

            if (currentAnimator == null)
            {
                FireFallback(current);
                return;
            }

            try
            {
                string? playedState = PlayShootStateDirectly(currentAnimator);

                if (playedState == null)
                {
                    Plugin.Logger.LogWarning(
                        "[Not-a-pea] Shoot state was not found; " +
                        "using the direct projectile fallback."
                    );
                    FireFallback(current);
                    return;
                }

                shootLayerResetPending = true;
                shootLayerResetTime = Time.time + ShootLayerResetDelay;

                if (!directAnimationLogged)
                {
                    directAnimationLogged = true;
                    Plugin.Logger.LogInfo(
                        "[Not-a-pea] Direct shoot animation active" +
                        " | Layer = " + ShootLayerIndex +
                        " | State = " + playedState +
                        " | Duration = " + ShootAnimationDuration + "s"
                    );
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Not-a-pea] Direct shoot animation failed; " +
                    "using direct shot: " + exception.Message
                );
                FireFallback(current);
            }
        }

        private static string? PlayShootStateDirectly(Animator targetAnimator)
        {
            if (targetAnimator == null ||
                targetAnimator.layerCount <= ShootLayerIndex)
            {
                return null;
            }

            targetAnimator.SetLayerWeight(ShootLayerIndex, 1f);

            for (int index = 0; index < ShootStateCandidates.Length; index++)
            {
                string candidate = ShootStateCandidates[index];
                int stateHash = Animator.StringToHash(candidate);

                if (!targetAnimator.HasState(ShootLayerIndex, stateHash))
                    continue;

                targetAnimator.Play(
                    stateHash,
                    ShootLayerIndex,
                    0f
                );
                targetAnimator.Update(0f);
                return candidate;
            }

            return null;
        }

        private void ResetShootLayer()
        {
            shootLayerResetPending = false;
            Animator? currentAnimator = animator;

            if (currentAnimator == null ||
                currentAnimator.layerCount <= ShootLayerIndex)
            {
                return;
            }

            try
            {
                for (int index = 0;
                     index < NothingStateCandidates.Length;
                     index++)
                {
                    string candidate = NothingStateCandidates[index];
                    int stateHash = Animator.StringToHash(candidate);

                    if (!currentAnimator.HasState(
                            ShootLayerIndex,
                            stateHash
                        ))
                    {
                        continue;
                    }

                    currentAnimator.Play(
                        stateHash,
                        ShootLayerIndex,
                        0f
                    );
                    currentAnimator.Update(0f);
                    return;
                }

                // A malformed controller should not leave the additive shoot
                // pose frozen forever.
                currentAnimator.SetLayerWeight(ShootLayerIndex, 0f);
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Not-a-pea] Shoot layer reset failed safely: " +
                    exception.Message
                );
            }
        }

        internal bool TryConsumeAnimationShot()
        {
            if (!animationShotArmed)
                return false;

            animationShotArmed = false;
            return true;
        }

        private void FireFallback(PeaShooter current)
        {
            animationShotArmed = false;
            V11PlantsBootstrap.CreateDirectSawShot(
                current,
                (PlantType)NotAPeaID,
                "Not-a-pea",
                Damage
            );

            if (!fallbackLogged)
            {
                fallbackLogged = true;
                Plugin.Logger.LogInfo(
                    "[Not-a-pea] Animation event fallback created the saw " +
                    "safely."
                );
            }
        }

        private static bool HasEnemyInRow(PeaShooter current)
        {
            try
            {
                var zombies = Lawnf.GetZombiesByRow(
                    current.thePlantRow,
                    false
                );

                if (zombies == null)
                    return false;

                float plantX = current.transform.position.x;

                for (int index = 0; index < zombies.Count; index++)
                {
                    Zombie zombie = zombies[index];

                    if (zombie == null ||
                        !zombie.Alive ||
                        zombie.isMindControlled ||
                        zombie.theHealth <= 0)
                    {
                        continue;
                    }

                    // Ignore enemies that have already passed behind the plant.
                    if (zombie.transform.position.x >= plantX - 0.25f)
                        return true;
                }
            }
            catch
            {
                // Let the native target code retry on the following frame.
            }

            return false;
        }
    }

    public sealed class NotAStormCommando : MonoBehaviour
    {
        public const int NotAStormCommandoID = 6011;
        public const int FallbackDamage = 20;
        public const float FallbackAttackInterval = 1.5f;
        public const int FallbackToughness = 300;
        public const float FallbackCardRecharge = 30f;
        public const int FallbackCardCost = 650;

        public NotAStormCommando(IntPtr pointer) : base(pointer) { }

        public void Start()
        {
            SuperGatling? plant = gameObject.GetComponent<SuperGatling>();
            string bridge = plant != null
                ? V11PlantsBootstrap.EnsureShooterRuntimeReferences(
                    plant,
                    "Not-a-storm Commando"
                )
                : "missing SuperGatling";

            Plugin.Logger.LogInfo(
                "[Not-a-storm Commando] Ready" +
                " | Class = Strong Odyssey" +
                " | Native behaviour = SuperGatling" +
                " | Projectile damage = " + FallbackDamage +
                " | Attached damage = " +
                (FallbackDamage / NotAPeaProjectile.AttachedDamageDivisor) +
                "/" + NotAPeaProjectile.DamageTickInterval + "s" +
                " | All fired projectiles use the saw mechanic" +
                " | Runtime bridge = " + bridge
            );
        }
    }

    /// <summary>
    /// Companion controller for the Not-a-pea saw. Movement and pooled
    /// lifetime stay on the native Bullet_pea component; this class handles
    /// traversal bookkeeping, the 25% attach roll and fixed timed damage.
    /// No Bullet_pierce or managed Bullet subclass is used.
    /// </summary>
    public sealed class NotAPeaProjectile : MonoBehaviour
    {
        public const int NotAPeaBulletID = 6006;
        public const int TraversalBudget = 1000000;
        public const float AttachmentChance = 0.25f;
        public const float AttachmentDuration = 10f;
        public const float DamageTickInterval = 1.5f;
        public const int DamagePerTick = 20;
        public const int AttachedDamageDivisor = 2;

        private readonly HashSet<int> hitZombieIDs = new HashSet<int>();
        private Bullet? nativeBullet;
        private Rigidbody2D? body;
        private Collider2D? projectileCollider;
        private Transform? shadow;
        private bool attachmentLocked;
        private bool motionStored;
        private float storedVx;
        private float storedVy;
        private float storedDetaVx;
        private float storedDetaVy;
        private float storedNormalSpeed;
        private float storedTrackSpeed;
        private Vector2 storedVelocity;

        public NotAPeaProjectile(IntPtr pointer) : base(pointer) { }

        internal Bullet? NativeBullet
        {
            get
            {
                CacheNativeComponents();
                return nativeBullet;
            }
        }

        public void OnEnable()
        {
            RestoreReusableState();
            ConfigureForFlight();
        }

        public void Start()
        {
            ConfigureForFlight();
        }

        public void OnDisable()
        {
            attachmentLocked = false;
            motionStored = false;
            hitZombieIDs.Clear();
        }

        internal void ConfigureForFlight()
        {
            CacheNativeComponents();

            Bullet? bullet = nativeBullet;

            if (bullet == null || attachmentLocked)
                return;

            if (bullet.board == null && Board.Instance != null)
                bullet.board = Board.Instance;

            bullet.dying = false;
            bullet.hit = false;
            bullet.hitTimes = 0;
            bullet.penetrationTimes = TraversalBudget;

            if (body != null)
                body.simulated = true;

            if (projectileCollider != null)
                projectileCollider.enabled = true;

            if (shadow != null)
                shadow.gameObject.SetActive(true);
        }

        internal void HandleCollision(Collider2D collision)
        {
            if (attachmentLocked || collision == null)
                return;

            Zombie? hitZombie = FindZombie(collision.transform);

            if (hitZombie == null ||
                !hitZombie.Alive ||
                hitZombie.isMindControlled)
            {
                return;
            }

            int targetID = hitZombie.GetInstanceID();

            if (!hitZombieIDs.Add(targetID))
                return;

            bool shouldAttach =
                UnityEngine.Random.value < AttachmentChance;

            if (shouldAttach)
                PauseForAttachment();

            V11PlantsBootstrap.BeginSawImpact(
                this,
                hitZombie,
                shouldAttach
            );
        }

        internal int GetImpactDamage()
        {
            Bullet? bullet = NativeBullet;
            return bullet != null && bullet.Damage > 0
                ? bullet.Damage
                : DamagePerTick;
        }

        internal void ApplyDeferredImpactDamage(Zombie hitZombie)
        {
            Bullet? bullet = NativeBullet;

            if (bullet == null)
                return;

            int impactDamage = GetImpactDamage();
            PlantType reportType = ResolveReportType(bullet);

            ((Entity)hitZombie).TakeDamage(
                impactDamage,
                bullet.ToIDamageMaker(),
                DamageType.Normal,
                reportType,
                false
            );

            bullet.hitTimes++;
            bullet.penetrationTimes = TraversalBudget;
        }

        internal void CompleteTraversalImpact()
        {
            Bullet? bullet = NativeBullet;

            if (bullet != null)
            {
                bullet.hit = false;
                bullet.penetrationTimes = TraversalBudget;
            }
        }

        internal void ResumeAfterCancelledAttachment()
        {
            attachmentLocked = false;
            CacheNativeComponents();

            Bullet? bullet = nativeBullet;

            if (bullet == null)
                return;

            bullet.hit = false;
            bullet.penetrationTimes = TraversalBudget;

            if (motionStored)
            {
                bullet.Vx = storedVx;
                bullet.Vy = storedVy;
                bullet.detaVx = storedDetaVx;
                bullet.detaVy = storedDetaVy;
                bullet.normalSpeed = storedNormalSpeed;
                bullet.trackSpeed = storedTrackSpeed;

                if (body != null)
                {
                    body.velocity = storedVelocity;
                    body.simulated = true;
                }

                motionStored = false;
            }

            if (projectileCollider != null)
                projectileCollider.enabled = true;

            if (shadow != null)
                shadow.gameObject.SetActive(true);
        }

        internal void RetireNativeProjectileSafely()
        {
            attachmentLocked = true;
            CacheNativeComponents();

            Bullet? bullet = nativeBullet;

            if (body != null)
            {
                body.velocity = Vector2.zero;
                body.simulated = false;
            }

            if (projectileCollider != null)
                projectileCollider.enabled = false;

            if (shadow != null)
                shadow.gameObject.SetActive(false);

            if (bullet != null)
            {
                try
                {
                    // This is now the game's own Bullet_pea component. Its
                    // non-virtual Die path removes the projectile from
                    // CreateBullet's active registry before destroying it.
                    bullet.Die();
                    return;
                }
                catch (Exception exception)
                {
                    Plugin.Logger.LogWarning(
                        "[Not-a-pea] Native Bullet_pea.Die failed; " +
                        "using registry cleanup fallback: " +
                        exception.Message
                    );

                    try
                    {
                        CreateBullet? creator = CreateBullet.Instance;
                        if (creator != null)
                            creator.RemoveFromList(bullet);
                    }
                    catch
                    {
                        // Final destruction below still prevents reuse.
                    }
                }
            }

            UnityEngine.Object.Destroy(gameObject);
        }

        private void PauseForAttachment()
        {
            attachmentLocked = true;
            CacheNativeComponents();

            Bullet? bullet = nativeBullet;

            if (bullet == null)
                return;

            if (!motionStored)
            {
                motionStored = true;
                storedVx = bullet.Vx;
                storedVy = bullet.Vy;
                storedDetaVx = bullet.detaVx;
                storedDetaVy = bullet.detaVy;
                storedNormalSpeed = bullet.normalSpeed;
                storedTrackSpeed = bullet.trackSpeed;
                storedVelocity = body != null
                    ? body.velocity
                    : Vector2.zero;
            }

            bullet.Vx = 0f;
            bullet.Vy = 0f;
            bullet.detaVx = 0f;
            bullet.detaVy = 0f;
            bullet.normalSpeed = 0f;
            bullet.trackSpeed = 0f;
            bullet.hit = false;
            bullet.penetrationTimes = TraversalBudget;

            if (body != null)
            {
                body.velocity = Vector2.zero;
                body.simulated = false;
            }

            if (projectileCollider != null)
                projectileCollider.enabled = false;
        }

        private void CacheNativeComponents()
        {
            if (nativeBullet == null)
                nativeBullet = gameObject.GetComponent<Bullet>();

            if (body == null)
                body = gameObject.GetComponent<Rigidbody2D>();

            if (projectileCollider == null)
                projectileCollider =
                    gameObject.GetComponent<Collider2D>();

            if (shadow == null)
                shadow = FindChild(transform, "shadow");
        }

        private PlantType ResolveReportType(Bullet bullet)
        {
            try
            {
                if (bullet.from != null)
                    return bullet.from.thePlantType;
            }
            catch
            {
                // fromType remains stable after the source is removed.
            }

            return bullet.fromType;
        }

        private void RestoreReusableState()
        {
            attachmentLocked = false;
            motionStored = false;
            hitZombieIDs.Clear();
            CacheNativeComponents();
        }

        internal static Zombie? FindZombie(Transform? start)
        {
            Transform? current = start;

            for (int depth = 0; current != null && depth < 8; depth++)
            {
                Zombie? zombie = current.gameObject.GetComponent<Zombie>();

                if (zombie != null)
                    return zombie;

                current = current.parent;
            }

            return null;
        }

        private static Transform? FindChild(
            Transform root,
            string childName
        )
        {
            Transform[] children =
                root.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in children)
            {
                if (child != null &&
                    string.Equals(
                        child.name,
                        childName,
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    return child;
                }
            }

            return null;
        }
    }

    public sealed class FrostFurflower : MonoBehaviour
    {
        public const int FrostFurflowerID = 6012;
        public const int SnowballSunReward = 100;
        public const float FallbackProduceInterval = 25f;
        public const int FallbackToughness = 300;
        public const float FallbackCardRecharge = 7.5f;
        public const int FallbackCardCost = 100;

        private int lastSnowballInstanceID = int.MinValue;
        private float lastSnowballTime = -10f;
        private static bool firstTriggerLogged;

        public FrostFurflower(IntPtr pointer) : base(pointer) { }

        public void Start()
        {
            V11PlantsBootstrap.ApplyLocalAnimationController(
                gameObject,
                "Frost Furflower"
            );
            Plugin.Logger.LogInfo(
                "[Frost Furflower] Ready" +
                " | Native Sunflower production" +
                " | Snowball reward = " + SnowballSunReward +
                " Sun | Freeze area = 3x3"
            );
        }

        public void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision == null)
                return;

            Bullet_snowBall? snowball = FindSnowball(collision.transform);

            if (snowball == null)
                return;

            Plant? plant = gameObject.GetComponent<Plant>();

            if (plant != null)
                HandleSnowball(snowball, plant);
        }

        private void HandleSnowball(Bullet_snowBall snowball, Plant plant)
        {
            if (snowball == null || plant == null)
                return;

            int snowballID = snowball.GetInstanceID();

            if (snowballID == lastSnowballInstanceID &&
                Time.time - lastSnowballTime < 0.25f)
            {
                return;
            }

            lastSnowballInstanceID = snowballID;
            lastSnowballTime = Time.time;

            int sunCreated = CreateSnowballSun(plant);
            int plantsFrozen = FreezeSurroundingPlants(plant);

            if (!firstTriggerLogged)
            {
                firstTriggerLogged = true;
                Plugin.Logger.LogInfo(
                    "[Frost Furflower] Snowball mechanic triggered" +
                    " | Sun = " + sunCreated +
                    " | Nearby plants frozen = " + plantsFrozen
                );
            }
        }

        private static int CreateSnowballSun(Plant plant)
        {
            CreateItem? creator = CreateItem.Instance;

            if (creator == null)
                return 0;

            int created = 0;

            // Big Sun is worth 50, so two native drops produce exactly 100.
            for (int index = 0; index < 2; index++)
            {
                GameObject item = creator.SetCoin(
                    plant.thePlantColumn,
                    plant.thePlantRow,
                    (int)ItemType.BigSun,
                    0
                );

                if (item != null)
                    created += 50;
            }

            return created;
        }

        private static int FreezeSurroundingPlants(Plant center)
        {
            int frozen = 0;

            for (int column = center.thePlantColumn - 1;
                 column <= center.thePlantColumn + 1;
                 column++)
            {
                for (int row = center.thePlantRow - 1;
                     row <= center.thePlantRow + 1;
                     row++)
                {
                    if (column < 0 || row < 0)
                        continue;

                    try
                    {
                        var plants = Lawnf.Get1x1Plants(column, row);

                        if (plants == null)
                            continue;

                        for (int index = 0; index < plants.Count; index++)
                        {
                            Plant candidate = plants[index];

                            if (candidate == null ||
                                candidate.gameObject == center.gameObject ||
                                !FreezedPlant.CanFreeze(candidate))
                            {
                                continue;
                            }

                            if (FreezedPlant.FreezePlant(candidate, false) != null)
                                frozen++;
                        }
                    }
                    catch
                    {
                        // The edge of unusual boards may reject an unused tile.
                    }
                }
            }

            return frozen;
        }

        private static Bullet_snowBall? FindSnowball(Transform? start)
        {
            Transform? current = start;

            for (int depth = 0; current != null && depth < 4; depth++)
            {
                Bullet_snowBall? snowball =
                    current.gameObject.GetComponent<Bullet_snowBall>();

                if (snowball != null)
                    return snowball;

                current = current.parent;
            }

            return null;
        }
    }
}
