using CustomizeLib.BepInEx;
using HarmonyLib;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlantsPlus.Core
{
    /// <summary>
    /// Plants+ V1.1 bootstrap for Doomtronion and Lichen-pea.
    /// Effects triggered by hits are queued and resolved from Board.Update,
    /// outside of native collision and damage callbacks.
    /// </summary>
    public static class V11ExpansionBootstrap
    {
        private sealed class PendingDoomExplosion
        {
            public Vector2 Position;
            public int Column;
            public int Row;
        }

        private sealed class PendingLichenFreeze
        {
            public int Column;
            public int Row;
        }

        private sealed class PendingIrradiation
        {
            public int ZombieInstanceID;
            public Plant? SourcePlant;
            public int SourcePlantInstanceID;
        }

        private sealed class IrradiatedZombieState
        {
            public int ZombieInstanceID;
            public Plant? SourcePlant;
            public int SourcePlantInstanceID;
            public float Remaining;
            public float PulseTimer;
            public bool FirstPulseLogged;
        }

        private sealed class ActiveElectricArc
        {
            public GameObject? Visual;
            public LineRenderer? Renderer;
            public Vector3 Start;
            public Vector3 End;
            public float Remaining;
        }

        private static readonly List<PendingDoomExplosion>
            PendingDoomExplosions = new List<PendingDoomExplosion>();

        private static readonly List<PendingLichenFreeze>
            PendingLichenFreezes = new List<PendingLichenFreeze>();

        private static readonly List<PendingIrradiation>
            PendingIrradiations = new List<PendingIrradiation>();

        private static readonly List<IrradiatedZombieState>
            IrradiatedZombies = new List<IrradiatedZombieState>();

        private static readonly List<ActiveElectricArc>
            ActiveElectricArcs = new List<ActiveElectricArc>();

        private static GameObject? doomtronionElectricArcPrefab;
        private static Sprite? doomtronionBiuSprite;
        private static bool doomtronionBiuFound;

        private static bool registered;
        private static bool nativeDataLogged;
        private static bool firstIrradiationLogged;
        private static bool firstIrradiationPulseLogged;
        private static bool firstDoomQueuedLogged;
        private static bool firstDoomExplosionLogged;
        private static bool firstLichenQueuedLogged;
        private static bool firstLichenFreezeLogged;
        private static bool firstDoomDirectAttackLogged;
        private static bool firstElectricArcLogged;
        private static bool firstDoomVisualNormalizationLogged;
        private static bool resolvingDoomExplosion;
        private static bool resolvingIrradiationPulse;
        private static bool resolvingDoomDirectDamage;

        public static void OnStart()
        {
            if (registered)
                return;

            registered = true;

            bool doomReady = RegisterDoomtronion();
            bool lichenBulletReady = RegisterLichenPeaProjectile();
            bool lichenReady = lichenBulletReady && RegisterLichenPea();

            if (lichenReady)
                InstallLichenProjectileSkin();

            InstallTypeFlags();

            Plugin.Logger.LogInfo(
                "[Plants+ V1.1] Registration finished" +
                " | Doomtronion = " +
                (doomReady ? Plants.Doomtronion.DoomtronionID : -1) +
                " | Lichen-pea = " +
                (lichenReady ? Plants.LichenPea.LichenPeaID : -1) +
                " | Lichen projectile = " +
                (lichenBulletReady ? Plants.LichenPea.LichenPeaBulletID : -1)
            );
        }

        public static void OnGameInit()
        {
            InstallTypeFlags();
            InstallLichenProjectileSkin();
            RefreshNativePlantData();
            ConfigureDoomtronionPrefab();

            if (CustomCore.CustomPlants.ContainsKey(
                (PlantType)Plants.LichenPea.LichenPeaID
            ))
            {
                V11PlantsBootstrap.ConfigureRegisteredShooterPrefab(
                    (PlantType)Plants.LichenPea.LichenPeaID,
                    "Lichen-pea"
                );
            }
        }

        private static bool RegisterDoomtronion()
        {
            if (!ValidatePlantID(
                "Doomtronion",
                Plants.Doomtronion.DoomtronionID
            ))
            {
                return false;
            }

            return RegisterPlant<Prismflower, Plants.Doomtronion>(
                Plants.Doomtronion.DoomtronionID,
                "doomtronion",
                "ElectricOnionPrefab",
                "ElectricOnionPreview",
                new List<(int, int)>
                {
                    (
                        (int)PlantType.ElectricOnion,
                        (int)PlantType.DoomShroom
                    ),
                    (
                        (int)PlantType.DoomShroom,
                        (int)PlantType.ElectricOnion
                    )
                },
                Plants.Doomtronion.FallbackAttackInterval,
                0f,
                Plants.Doomtronion.FallbackDamage,
                Plants.Doomtronion.FallbackToughness,
                Plants.Doomtronion.FallbackCardRecharge,
                Plants.Doomtronion.FallbackCardCost,
                AlmanacContent.Doomtronion,
                "idle",
                "shoot",
                "electric"
            );
        }

        private static bool RegisterLichenPeaProjectile()
        {
            BulletType bulletType =
                (BulletType)Plants.LichenPea.LichenPeaBulletID;

            try
            {
                if (Enum.IsDefined(typeof(BulletType), (int)bulletType))
                {
                    Plugin.Logger.LogError(
                        "[Lichen-pea] Bullet ID collision | ID = " +
                        Plants.LichenPea.LichenPeaBulletID +
                        " | Native bullet = " + bulletType
                    );
                    return false;
                }

                if (CustomCore.CustomBullets.ContainsKey(bulletType))
                {
                    Plugin.Logger.LogError(
                        "[Lichen-pea] Bullet ID " +
                        Plants.LichenPea.LichenPeaBulletID +
                        " is already registered by another mod."
                    );
                    return false;
                }

                AssetBundle? bundle = CustomCore.GetAssetBundle(
                    Assembly.GetExecutingAssembly(),
                    "PlantsPlus.Resources.AssetBundles.bullet_lichenpea"
                );

                if (bundle == null)
                {
                    Plugin.Logger.LogError(
                        "[Lichen-pea] Projectile AssetBundle is null."
                    );
                    return false;
                }

                GameObject? prefab = bundle.GetAsset<GameObject>("Bullet_pea");

                if (prefab == null || prefab.GetComponent<Collider2D>() == null)
                {
                    Plugin.Logger.LogError(
                        "[Lichen-pea] Projectile prefab or collider is null."
                    );
                    return false;
                }

                // The supplied bundle is visual-only. Remove one accidental
                // runtime component if Unity serialized one into the prefab.
                Bullet? staleBullet = prefab.GetComponent<Bullet>();
                if (staleBullet != null)
                    UnityEngine.Object.DestroyImmediate(staleBullet);

                CustomCore.RegisterCustomBullet<Bullet_pea>(
                    bulletType,
                    prefab
                );

                bool ready = CustomCore.CustomBullets.ContainsKey(bulletType);

                Plugin.Logger.LogInfo(
                    "[Lichen-pea] Projectile registration" +
                    " | Prefab = Bullet_pea" +
                    " | Runtime = Bullet_pea + deferred Cold" +
                    " | ID = " + Plants.LichenPea.LichenPeaBulletID +
                    " | Ready = " + ready
                );

                return ready;
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Lichen-pea] Projectile registration failed safely: " +
                    exception
                );
                return false;
            }
        }

        private static bool RegisterLichenPea()
        {
            if (!ValidatePlantID(
                "Lichen-pea",
                Plants.LichenPea.LichenPeaID
            ))
            {
                return false;
            }

            return RegisterPlant<PeaShooter, Plants.LichenPea>(
                Plants.LichenPea.LichenPeaID,
                "lichenpea",
                "PeashooterPrefab",
                "PeaShooterPreview",
                new List<(int, int)>
                {
                    (
                        (int)PlantType.Thorns,
                        (int)PlantType.Peashooter
                    ),
                    (
                        (int)PlantType.Peashooter,
                        (int)PlantType.Thorns
                    )
                },
                Plants.LichenPea.FallbackAttackInterval,
                0f,
                Plants.LichenPea.FallbackDamage,
                Plants.LichenPea.FallbackToughness,
                Plants.LichenPea.FallbackCardRecharge,
                Plants.LichenPea.FallbackCardCost,
                AlmanacContent.LichenPea,
                "idle",
                "shoot"
            );
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
            AlmanacEntry almanac,
            params string[] animationClips
        )
            where TBase : Plant
            where TBehaviour : MonoBehaviour
        {
            try
            {
                AssetBundle? bundle = CustomCore.GetAssetBundle(
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

                GameObject? prefab = bundle.GetAsset<GameObject>(prefabName);
                GameObject? preview = bundle.GetAsset<GameObject>(previewName);

                if (prefab == null || preview == null)
                {
                    Plugin.Logger.LogError(
                        "[" + almanac.Name + "] Prefab or preview is null."
                    );
                    return false;
                }

                if (animationClips.Length > 0)
                {
                    V11PlantsBootstrap.IsolateAnimationClips(
                        bundle,
                        prefab,
                        almanac.Name,
                        animationClips
                    );
                }

                if (id == Plants.Doomtronion.DoomtronionID)
                    PrepareDoomtronionVisualAssets(bundle, prefab);

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

        private static void PrepareDoomtronionVisualAssets(
            AssetBundle bundle,
            GameObject prefab
        )
        {
            if (bundle == null || prefab == null)
                return;

            try
            {
                // The supplied doomtronion(2) bundle was exported with the
                // scene-placement offset still serialized on the prefab root.
                // Custom plants are positioned by the board, so keeping that
                // offset shifts the whole plant far away from its tile.
                prefab.transform.localPosition = Vector3.zero;
                prefab.transform.localRotation = Quaternion.identity;

                // Doomtronion does not use Prismflower's LineRenderer.
                // Its own electric.electric animation displays the special
                // child sprite named "biu" from the plant prefab.
                doomtronionElectricArcPrefab = null;
                doomtronionBiuSprite = LoadCorrectBiuSprite();
                doomtronionBiuFound = doomtronionBiuSprite != null;

                if (!firstDoomVisualNormalizationLogged)
                {
                    firstDoomVisualNormalizationLogged = true;
                    Plugin.Logger.LogInfo(
                        "[Doomtronion] Visual prefab normalized" +
                        " | Root local position = (0,0,0)" +
                        " | Electric visual = thin attack sprite 'biu'" +
                        " | Biu found = " + doomtronionBiuFound +
                        " | PrismLine disabled = true"
                    );
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Doomtronion] Visual asset preparation deferred safely: " +
                    exception.Message
                );
            }
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

            if (CustomCore.CustomPlants.ContainsKey((PlantType)id))
            {
                Plugin.Logger.LogError(
                    "[" + name + "] Plant ID " + id +
                    " is already registered by another mod."
                );
                return false;
            }

            return true;
        }

        private static void InstallLichenProjectileSkin()
        {
            PlantType plantType = (PlantType)Plants.LichenPea.LichenPeaID;
            BulletType customBullet =
                (BulletType)Plants.LichenPea.LichenPeaBulletID;

            if (!CustomCore.CustomPlants.ContainsKey(plantType) ||
                !CustomCore.CustomBullets.ContainsKey(customBullet))
            {
                return;
            }

            SetProjectileSkin(plantType, BulletType.Bullet_pea, customBullet);
            SetProjectileSkin(
                plantType,
                BulletType.Bullet_snowPea,
                customBullet
            );
        }

        private static void SetProjectileSkin(
            PlantType plant,
            BulletType nativeBullet,
            BulletType customBullet
        )
        {
            CustomCore.CustomBulletsSkinID[
                new ValueTuple<PlantType, BulletType>(plant, nativeBullet)
            ] = new List<BulletType> { customBullet };
        }

        private static void InstallTypeFlags()
        {
            PlantType lichen = (PlantType)Plants.LichenPea.LichenPeaID;

            if (CustomCore.CustomPlants.ContainsKey(lichen))
                AddUnique(CustomCore.TypeMgrExtra.IsIcePlant, lichen);
        }

        private static void AddUnique(List<PlantType> list, PlantType type)
        {
            if (!list.Contains(type))
                list.Add(type);
        }

        private static void RefreshNativePlantData()
        {
            bool doomReady = MirrorNativeData(
                (PlantType)Plants.Doomtronion.DoomtronionID,
                PlantType.ElectricOnion
            );

            bool lichenReady = MirrorNativeData(
                (PlantType)Plants.LichenPea.LichenPeaID,
                PlantType.Peashooter
            );

            if (!nativeDataLogged && doomReady && lichenReady)
            {
                nativeDataLogged = true;
                Plugin.Logger.LogInfo(
                    "[Plants+ V1.1] Native attack data mirrored" +
                    " | Doomtronion <- Amp-nion" +
                    " | Lichen-pea <- Peashooter"
                );
            }
        }

        private static bool MirrorNativeData(
            PlantType customType,
            PlantType nativeType
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
                float customCooldown = target.cd;

                target.thePlantType = customType;
                target.maxHealth = nativeData.maxHealth;
                target.attackInterval = nativeData.attackInterval;
                target.attackDamage = nativeData.attackDamage;
                target.cost = customCost;
                target.cd = customCooldown;

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

        internal static void ConfigureDoomtronionInstance(GameObject instance)
        {
            if (instance == null)
                return;

            try
            {
                Prismflower? plant = instance.GetComponent<Prismflower>();
                if (plant == null)
                    return;

                if (plant.anim == null)
                    plant.anim = instance.GetComponentInChildren<Animator>(true);

                if (plant.axis == null)
                    plant.axis = instance.transform;

                if (plant.board == null && Board.Instance != null)
                    plant.board = Board.Instance;

                if (plant.rb == null)
                    plant.rb = instance.GetComponent<Rigidbody2D>();

                // Never inject PrismLine: the supplied prefab uses its own
                // animated "biu" sprite for the electric attack.
                plant.prism = null;


                V11PlantsBootstrap.ApplyLocalAnimationController(
                    instance,
                    "Doomtronion"
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Doomtronion] Runtime bridge deferred safely: " +
                    exception.Message
                );
            }
        }

        private static void ConfigureDoomtronionPrefab()
        {
            try
            {
                GameObject? prefab = GetNativePlantPrefab(
                    (PlantType)Plants.Doomtronion.DoomtronionID
                );

                if (prefab == null)
                    return;

                ConfigureDoomtronionInstance(prefab);
                Prismflower? plant = prefab.GetComponent<Prismflower>();

                Plugin.Logger.LogInfo(
                    "[Doomtronion] Prefab bridge" +
                    " | Animator = " +
                    (plant != null && plant.anim != null
                        ? plant.anim.name
                        : "missing") +
                    " | Prism effect = " +
                    (plant != null && plant.prism != null
                        ? plant.prism.name
                        : "missing") +
                    " | Board = " +
                    (plant != null && plant.board != null
                        ? "ready"
                        : "deferred")
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Doomtronion] Prefab bridge deferred: " +
                    exception.Message
                );
            }
        }

        private static GameObject? GetNativePlantPrefab(PlantType type)
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

        private static Transform? FindChildNamed(
            Transform root,
            string wantedName
        )
        {
            if (root == null)
                return null;

            if (string.Equals(
                    root.name,
                    wantedName,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return root;
            }

            for (int index = 0; index < root.childCount; index++)
            {
                Transform? found = FindChildNamed(
                    root.GetChild(index),
                    wantedName
                );
                if (found != null)
                    return found;
            }

            return null;
        }

        private static Sprite? LoadCorrectBiuSprite()
        {
            try
            {
                using System.IO.Stream? stream = Assembly
                    .GetExecutingAssembly()
                    .GetManifestResourceStream(
                        "PlantsPlus.Resources.Sprites." +
                        "doomtronion_biu.png"
                    );

                if (stream == null)
                    return null;

                byte[] bytes = new byte[stream.Length];
                int total = 0;
                while (total < bytes.Length)
                {
                    int read = stream.Read(
                        bytes,
                        total,
                        bytes.Length - total
                    );
                    if (read <= 0)
                        break;
                    total += read;
                }

                Texture2D texture = new Texture2D(
                    2,
                    2,
                    TextureFormat.RGBA32,
                    false
                );
                texture.name = "Doomtronion correct biu texture";
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;

                if (!ImageConversion.LoadImage(texture, bytes, false))
                    return null;

                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                sprite.name = "biu";
                return sprite;
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Doomtronion] Correct biu sprite load failed: " +
                    exception.Message
                );
                return null;
            }
        }

        private static void SpawnCorrectBiu(
            Prismflower source,
            Zombie target
        )
        {
            if (source == null || target == null)
                return;

            Transform? sourceAnchor = FindChildNamed(
                source.transform,
                "Ball1"
            );
            Vector3 start = sourceAnchor != null
                ? sourceAnchor.position
                : source.transform.position +
                    new Vector3(0.05f, 1.05f, -0.05f);

            Vector3 end;
            try
            {
                end = target.col != null
                    ? target.col.bounds.center
                    : target.transform.position +
                        new Vector3(0f, 0.75f, 0f);
            }
            catch
            {
                end = target.transform.position +
                    new Vector3(0f, 0.75f, 0f);
            }

            SpawnCorrectBiu(start, end);
        }

        private static void SpawnCorrectBiu(
            Zombie source,
            Zombie target
        )
        {
            if (source == null || target == null)
                return;

            Vector3 start;
            Vector3 end;

            try
            {
                start = source.col != null
                    ? source.col.bounds.center
                    : source.transform.position +
                        new Vector3(0f, 0.75f, 0f);
            }
            catch
            {
                start = source.transform.position +
                    new Vector3(0f, 0.75f, 0f);
            }

            try
            {
                end = target.col != null
                    ? target.col.bounds.center
                    : target.transform.position +
                        new Vector3(0f, 0.75f, 0f);
            }
            catch
            {
                end = target.transform.position +
                    new Vector3(0f, 0.75f, 0f);
            }

            SpawnCorrectBiu(start, end);
        }

        private static void SpawnCorrectBiu(
            Plant source,
            Prismflower target
        )
        {
            if (source == null || target == null)
                return;

            Transform? sourceAnchor =
                FindChildNamed(source.transform, "Ball1");
            Transform? targetAnchor =
                FindChildNamed(target.transform, "Ball1");

            Vector3 start = sourceAnchor != null
                ? sourceAnchor.position
                : source.transform.position +
                    new Vector3(0.05f, 1.05f, -0.05f);
            Vector3 end = targetAnchor != null
                ? targetAnchor.position
                : target.transform.position +
                    new Vector3(0.05f, 1.05f, -0.05f);

            SpawnCorrectBiu(start, end);
        }

        private static void SpawnCorrectBiu(
            Vector3 start,
            Vector3 end
        )
        {
            Sprite? sprite = doomtronionBiuSprite;
            if (sprite == null)
                return;

            start.z = -0.05f;
            end.z = -0.05f;
            Vector3 delta = end - start;
            float distance = delta.magnitude;
            if (distance <= 0.01f)
                return;

            GameObject visual = new GameObject(
                "[Plants+] Doomtronion correct biu"
            );
            visual.transform.position = (start + end) * 0.5f;
            visual.transform.rotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg
            );

            float nativeLength = Mathf.Max(
                sprite.bounds.size.x,
                0.01f
            );
            visual.transform.localScale = new Vector3(
                distance / nativeLength,
                0.15f,
                1f
            );

            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = 30;

            ActiveElectricArcs.Add(new ActiveElectricArc
            {
                Visual = visual,
                Renderer = null,
                Start = start,
                End = end,
                Remaining = 0.10f
            });

            if (!firstElectricArcLogged)
            {
                firstElectricArcLogged = true;
                Plugin.Logger.LogInfo(
                    "[Doomtronion] Synchronized thin biu created" +
                    " | Source = Ball1" +
                    " | Target = collider center" +
                    " | Lifetime = 0.10s" +
                    " | Distance = " + distance
                );
            }
        }

        private static void SpawnElectricArc(
            Vector3 start,
            Vector3 end,
            float duration,
            Transform? parent = null
        )
        {
            GameObject? template = doomtronionElectricArcPrefab;

            if (template == null || duration <= 0f)
                return;

            GameObject? visual = null;

            try
            {
                visual = parent != null
                    ? UnityEngine.Object.Instantiate(template, parent)
                    : UnityEngine.Object.Instantiate(template);
                visual.name = "[Plants+] Doomtronion electric arc";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.rotation = Quaternion.identity;
                visual.SetActive(true);

                LineRenderer? line =
                    visual.GetComponentInChildren<LineRenderer>(true);

                if (line == null)
                {
                    Plugin.Logger.LogWarning(
                        "[Doomtronion] Electric arc prefab has no " +
                        "LineRenderer; visual skipped."
                    );
                    UnityEngine.Object.Destroy(visual);
                    return;
                }

                line.gameObject.SetActive(true);
                line.useWorldSpace = parent == null;
                line.positionCount = 2;
                Vector3 lineStart = parent != null
                    ? parent.InverseTransformPoint(start)
                    : start;
                Vector3 lineEnd = parent != null
                    ? parent.InverseTransformPoint(end)
                    : end;
                line.SetPosition(0, lineStart);
                line.SetPosition(1, lineEnd);
                line.sortingOrder = 30;
                line.startWidth = Mathf.Max(line.startWidth, 0.06f);
                line.endWidth = Mathf.Max(line.endWidth, 0.035f);

                Color startColor = line.startColor;
                Color endColor = line.endColor;
                startColor.a = 1f;
                endColor.a = 1f;
                line.startColor = startColor;
                line.endColor = endColor;
                line.enabled = true;

                ActiveElectricArcs.Add(new ActiveElectricArc
                {
                    Visual = visual,
                    Renderer = line,
                    Start = lineStart,
                    End = lineEnd,
                    Remaining = duration
                });

                if (!firstElectricArcLogged)
                {
                    firstElectricArcLogged = true;
                    Plugin.Logger.LogInfo(
                        "[Doomtronion] Electric target arc created" +
                        " | Prefab = " + template.name +
                        " | Renderer = " + line.name +
                        " | Duration = " + duration + "s"
                    );
                }
            }
            catch (Exception exception)
            {
                if (visual != null)
                    UnityEngine.Object.Destroy(visual);

                Plugin.Logger.LogWarning(
                    "[Doomtronion] Electric arc failed safely: " +
                    exception.Message
                );
            }
        }

        private static void SpawnAttackElectricArc(
            Prismflower source,
            Zombie target
        )
        {
            if (source == null || target == null)
                return;

            Vector3 start = source.transform.position +
                new Vector3(0f, 0.42f, -0.05f);
            Vector3 end = target.transform.position +
                new Vector3(0f, 0.58f, -0.05f);

            SpawnElectricArc(start, end, 0.24f, source.transform);
        }

        private static void SpawnIrradiationElectricPulse(Zombie target)
        {
            if (target == null)
                return;

            Vector3 center = target.transform.position +
                new Vector3(0f, 0.52f, -0.04f);
            Vector2 direction = UnityEngine.Random.insideUnitCircle;

            if (direction.sqrMagnitude < 0.01f)
                direction = Vector2.right;

            direction.Normalize();
            Vector3 offset = new Vector3(
                direction.x * 0.32f,
                direction.y * 0.32f,
                0f
            );

            SpawnElectricArc(
                center - offset,
                center + offset,
                0.12f,
                target.transform
            );
        }

        private static bool ApplyWitchfireIrradiation(Zombie target)
        {
            try
            {
                // Witchfire Pumpkin represents its Irritated/Irradiation
                // status with Ember. Poison and Radiation are separate.
                EffectManager.SetEffect(
                    target,
                    EffectType.Ember,
                    Plants.Doomtronion.IrradiationDuration,
                    1f
                );
                return true;
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Doomtronion] Witchfire Irradiation failed safely: " +
                    exception.Message
                );
                return false;
            }
        }

        private static void TickElectricArcs()
        {
            if (ActiveElectricArcs.Count == 0)
                return;

            float delta = Time.deltaTime;
            if (delta <= 0f)
                return;

            for (int index = ActiveElectricArcs.Count - 1;
                 index >= 0;
                 index--)
            {
                ActiveElectricArc state = ActiveElectricArcs[index];
                state.Remaining -= delta;

                if (state.Visual == null || state.Remaining <= 0f)
                {
                    if (state.Visual != null)
                        UnityEngine.Object.Destroy(state.Visual);

                    ActiveElectricArcs.RemoveAt(index);
                    continue;
                }

                if (state.Renderer != null)
                {
                    state.Renderer.SetPosition(0, state.Start);
                    state.Renderer.SetPosition(1, state.End);
                }
            }
        }

        private static void ClearElectricArcs()
        {
            for (int index = ActiveElectricArcs.Count - 1;
                 index >= 0;
                 index--)
            {
                GameObject? visual = ActiveElectricArcs[index].Visual;
                if (visual != null)
                    UnityEngine.Object.Destroy(visual);
            }

            ActiveElectricArcs.Clear();
        }

        private static void QueueIrradiation(
            Zombie zombie,
            Plant? sourcePlant
        )
        {
            if (zombie == null)
                return;

            int sourcePlantInstanceID = 0;

            try
            {
                if (sourcePlant != null)
                    sourcePlantInstanceID = sourcePlant.GetInstanceID();
            }
            catch
            {
                sourcePlant = null;
            }

            PendingIrradiations.Add(new PendingIrradiation
            {
                ZombieInstanceID = zombie.GetInstanceID(),
                SourcePlant = sourcePlant,
                SourcePlantInstanceID = sourcePlantInstanceID
            });
        }

        private static void ResolveIrradiations()
        {
            if (PendingIrradiations.Count > 0)
            {
                List<PendingIrradiation> work =
                    new List<PendingIrradiation>(PendingIrradiations);
                PendingIrradiations.Clear();

                var zombies = Lawnf.GetAllZombies(false);

                for (int workIndex = 0;
                     workIndex < work.Count;
                     workIndex++)
                {
                    PendingIrradiation pending = work[workIndex];
                    Zombie? target = FindLiveZombieByInstanceID(
                        zombies,
                        pending.ZombieInstanceID
                    );

                    if (target == null)
                        continue;

                    IrradiatedZombieState? existing = null;

                    for (int stateIndex = 0;
                         stateIndex < IrradiatedZombies.Count;
                         stateIndex++)
                    {
                        if (IrradiatedZombies[stateIndex].ZombieInstanceID ==
                            pending.ZombieInstanceID)
                        {
                            existing = IrradiatedZombies[stateIndex];
                            break;
                        }
                    }

                    if (existing != null)
                    {
                        existing.Remaining =
                            Plants.Doomtronion.IrradiationDuration;

                        ApplyWitchfireIrradiation(target);

                        if (pending.SourcePlant != null)
                        {
                            existing.SourcePlant = pending.SourcePlant;
                            existing.SourcePlantInstanceID =
                                pending.SourcePlantInstanceID;
                        }

                        continue;
                    }

                    bool irradiationApplied =
                        ApplyWitchfireIrradiation(target);
                    IrradiatedZombies.Add(new IrradiatedZombieState
                    {
                        ZombieInstanceID = pending.ZombieInstanceID,
                        SourcePlant = pending.SourcePlant,
                        SourcePlantInstanceID =
                            pending.SourcePlantInstanceID,
                        Remaining = Plants.Doomtronion.IrradiationDuration,
                        PulseTimer = 0f,
                        FirstPulseLogged = false
                    });

                    if (!firstIrradiationLogged)
                    {
                        firstIrradiationLogged = true;
                        Plugin.Logger.LogInfo(
                            "[Doomtronion] Irradiation applied" +
                            " | Behaviour = Witchfire Pumpkin Irradiation" +
                            " | EffectType = Ember" +
                            " | Duration = " +
                            Plants.Doomtronion.IrradiationDuration + "s" +
                            " | Applied = " + irradiationApplied +
                            " | Radiation = false"
                        );
                    }
                }
            }

            if (IrradiatedZombies.Count == 0)
                return;

            float delta = Time.deltaTime;
            if (delta <= 0f)
                return;

            var liveZombies = Lawnf.GetAllZombies(false);
            Board? board = Board.Instance;

            for (int index = IrradiatedZombies.Count - 1;
                 index >= 0;
                 index--)
            {
                IrradiatedZombieState state = IrradiatedZombies[index];
                Zombie? target = FindLiveZombieByInstanceID(
                    liveZombies,
                    state.ZombieInstanceID
                );

                state.Remaining -= delta;

                if (target == null || state.Remaining <= 0f)
                {
                    IrradiatedZombies.RemoveAt(index);
                    continue;
                }

                Plant? source = state.SourcePlant;

                if (!IsLiveDoomtronionSource(
                        source,
                        state.SourcePlantInstanceID
                    ))
                {
                    source = null;
                    state.SourcePlant = null;
                }

                // EffectManager owns the Irradiation lifetime and damage,
                // exactly as it does for Witchfire Pumpkin. Do not add the
                // separate Radiation pulse mechanic here.
            }
        }

        private static void DamageIrradiationPulse(
            IrradiatedZombieState state,
            Zombie irradiatedTarget,
            Plant? source,
            Il2CppSystem.Collections.Generic.List<Zombie>? zombies,
            Board? board
        )
        {
            if (irradiatedTarget == null || zombies == null || board == null)
                return;

            Vector3 centerPosition;
            int centerColumn;
            int centerRow;

            try
            {
                centerPosition = irradiatedTarget.transform.position;
                centerColumn = Lawnf.GetColumnFromX(centerPosition.x);
                centerRow = irradiatedTarget.theZombieRow;
            }
            catch
            {
                return;
            }

            int damage =
                Plants.WitchfirePumpkin.CalculateRadiationDamage(0);
            float radiusTiles =
                Plants.WitchfirePumpkin.CalculateRadiationRadiusTiles(0);
            int hitCount = 0;

            resolvingIrradiationPulse = true;

            try
            {
                for (int index = 0; index < zombies.Count; index++)
                {
                    Zombie zombie = zombies[index];

                    if (!Plants.WitchfirePumpkin
                            .CanReceiveRadiationPulse(zombie) ||
                        !Plants.WitchfirePumpkin.IsInsideRadiationArea(
                            centerPosition,
                            centerColumn,
                            centerRow,
                            board,
                            zombie,
                            radiusTiles
                        ))
                    {
                        continue;
                    }

                    try
                    {
                        if (source != null)
                        {
                            ((Entity)zombie).TakeDamage(
                                damage,
                                source.ToIDamageMaker(),
                                DamageType.NormalAll,
                                (PlantType)Plants.Doomtronion.DoomtronionID,
                                false
                            );
                        }
                        else
                        {
                            zombie.ApplyDamage(
                                DamageType.NormalAll,
                                damage
                            );
                        }
                    }
                    catch
                    {
                        try
                        {
                            zombie.ApplyDamage(
                                DamageType.NormalAll,
                                damage
                            );
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    hitCount++;
                }
            }
            finally
            {
                resolvingIrradiationPulse = false;
            }

            if (hitCount > 0)
                SpawnIrradiationElectricPulse(irradiatedTarget);

            if (!state.FirstPulseLogged)
            {
                state.FirstPulseLogged = true;

                if (!firstIrradiationPulseLogged)
                {
                    firstIrradiationPulseLogged = true;
                    Plugin.Logger.LogInfo(
                        "[Doomtronion] Irradiation pulse confirmed" +
                        " | Center = irradiated zombie" +
                        " | Targets = " + hitCount +
                        " | Damage each = " + damage +
                        " | Interval = " +
                        Plants.WitchfirePumpkin.RadiationInterval + "s" +
                        " | Radius = " + radiusTiles + " tiles" +
                        " | Damage type = NormalAll"
                    );
                }
            }
        }

        private static bool IsLiveDoomtronionSource(
            Plant? source,
            int expectedInstanceID
        )
        {
            if (source == null)
                return false;

            try
            {
                return !source.dying &&
                    !source.waitingDestory &&
                    source.thePlantHealth > 0 &&
                    (int)source.thePlantType ==
                        Plants.Doomtronion.DoomtronionID &&
                    (expectedInstanceID == 0 ||
                     source.GetInstanceID() == expectedInstanceID);
            }
            catch
            {
                return false;
            }
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
                    // The zombie vanished between native snapshots.
                }
            }

            return null;
        }

        internal static int ExecuteDoomtronionAttack(Prismflower source)
        {
            if (source == null ||
                source.dying ||
                source.waitingDestory ||
                source.thePlantHealth <= 0)
            {
                return 0;
            }

            int sourceRow = source.thePlantRow;
            int sourceColumn = source.thePlantColumn;
            var zombies = Lawnf.GetAllZombies(false);
            Zombie? primaryTarget = null;
            float primaryDistance = float.MaxValue;

            for (int index = 0; index < zombies.Count; index++)
            {
                Zombie? zombie = zombies[index];

                if (zombie == null ||
                    !zombie.Alive ||
                    zombie.isMindControlled ||
                    Math.Abs(zombie.theZombieRow - sourceRow) > 2)
                {
                    continue;
                }

                int zombieColumn;
                try
                {
                    zombieColumn = Lawnf.GetColumnFromX(
                        zombie.transform.position.x
                    );
                }
                catch
                {
                    continue;
                }

                if (Math.Abs(zombieColumn - sourceColumn) > 2)
                    continue;

                float distance =
                    (zombie.transform.position -
                     source.transform.position).sqrMagnitude;

                if (primaryTarget == null ||
                    distance < primaryDistance)
                {
                    primaryTarget = zombie;
                    primaryDistance = distance;
                }
            }

            if (primaryTarget == null)
                return 0;

            int supportCount = 0;
            var plants = Lawnf.GetAllPlants();

            for (int index = 0; index < plants.Count; index++)
            {
                Plant? support = plants[index];
                if (support == null ||
                    support == source ||
                    support.dying ||
                    support.waitingDestory ||
                    support.thePlantHealth <= 0 ||
                    (int)support.theStatus != 0)
                {
                    continue;
                }

                int supportType = (int)support.thePlantType;
                if (supportType != (int)PlantType.ElectricOnion &&
                    supportType !=
                    Plants.Doomtronion.DoomtronionID &&
                    supportType !=
                    Plants.Icytronion.IcytronionID)
                {
                    continue;
                }

                if (Math.Abs(
                        support.thePlantColumn - sourceColumn
                    ) > 2 ||
                    Math.Abs(
                        support.thePlantRow - sourceRow
                    ) > 2)
                {
                    continue;
                }

                supportCount++;
                SpawnCorrectBiu(support, source);
            }

            int primaryDamage =
                Plants.Doomtronion.FallbackDamage +
                supportCount *
                Plants.Doomtronion.SupportDamage;

            int hitCount = DamageDoomtronionTarget(
                source,
                primaryTarget,
                primaryDamage
            )
                ? 1
                : 0;

            if (hitCount > 0)
                SpawnCorrectBiu(source, primaryTarget);

            var chainCandidates = new List<Zombie>();
            var chainDistances = new List<float>();
            Vector3 primaryPosition =
                primaryTarget.transform.position;

            for (int index = 0; index < zombies.Count; index++)
            {
                Zombie? zombie = zombies[index];
                if (zombie == null ||
                    zombie == primaryTarget ||
                    !zombie.Alive ||
                    zombie.isMindControlled ||
                    Math.Abs(
                        zombie.theZombieRow -
                        primaryTarget.theZombieRow
                    ) > 1)
                {
                    continue;
                }

                float distance =
                    (zombie.transform.position -
                     primaryPosition).sqrMagnitude;

                if (distance >
                    Plants.Doomtronion.ChainRadius *
                    Plants.Doomtronion.ChainRadius)
                {
                    continue;
                }

                int insertAt = chainDistances.Count;
                while (insertAt > 0 &&
                       chainDistances[insertAt - 1] > distance)
                {
                    insertAt--;
                }

                chainDistances.Insert(insertAt, distance);
                chainCandidates.Insert(insertAt, zombie);
            }

            int chainDamage = Math.Max(1, primaryDamage / 2);
            int chainCount = Math.Min(
                Plants.Doomtronion.MaximumChainTargets,
                chainCandidates.Count
            );

            for (int index = 0; index < chainCount; index++)
            {
                Zombie target = chainCandidates[index];
                if (!DamageDoomtronionTarget(
                        source,
                        target,
                        chainDamage
                    ))
                {
                    continue;
                }

                SpawnCorrectBiu(primaryTarget, target);
                hitCount++;
            }

            if (hitCount > 0 && !firstDoomDirectAttackLogged)
            {
                firstDoomDirectAttackLogged = true;
                Plugin.Logger.LogInfo(
                    "[Doomtronion] Electronion-style attack confirmed" +
                    " | Primary damage = " + primaryDamage +
                    " | Support plants = " + supportCount +
                    " | Chained zombies = " + (hitCount - 1) +
                    "/" + Plants.Doomtronion.MaximumChainTargets +
                    " | Chain damage = " + chainDamage +
                    " | Interval = " +
                    Plants.Doomtronion.FallbackAttackInterval + "s"
                );
            }

            return hitCount;
        }

        private static bool DamageDoomtronionTarget(
            Prismflower source,
            Zombie zombie,
            int damage
        )
        {
            if (source == null ||
                zombie == null ||
                !zombie.Alive ||
                zombie.isMindControlled)
            {
                return false;
            }

            resolvingDoomDirectDamage = true;
            try
            {
                ((Entity)zombie).TakeDamage(
                    damage,
                    source.ToIDamageMaker(),
                    DamageType.Normal,
                    (PlantType)Plants.Doomtronion.DoomtronionID,
                    false
                );
            }
            catch
            {
                try
                {
                    zombie.ApplyDamage(
                        DamageType.Normal,
                        damage
                    );
                }
                catch
                {
                    return false;
                }
            }
            finally
            {
                resolvingDoomDirectDamage = false;
            }

            HandleDoomtronionHit(zombie, source);
            IcytronionBootstrap.ApplyFamilyColdIfConnected(
                zombie,
                source
            );
            return true;
        }

        internal static bool BeginDoomtronionAttack(Prismflower source)
        {
            if (source == null ||
                source.dying ||
                source.waitingDestory ||
                source.thePlantHealth <= 0)
            {
                return false;
            }

            int sourceRow = source.thePlantRow;
            int sourceColumn = source.thePlantColumn;
            var zombies = Lawnf.GetAllZombies(false);
            bool hasTarget = false;

            for (int index = 0; index < zombies.Count; index++)
            {
                Zombie? zombie = zombies[index];
                if (zombie == null ||
                    !zombie.Alive ||
                    zombie.isMindControlled ||
                    Math.Abs(zombie.theZombieRow - sourceRow) > 2)
                {
                    continue;
                }

                try
                {
                    int column = Lawnf.GetColumnFromX(
                        zombie.transform.position.x
                    );
                    if (Math.Abs(column - sourceColumn) <= 2)
                    {
                        hasTarget = true;
                        break;
                    }
                }
                catch
                {
                    // Continue checking the remaining native zombie snapshot.
                }
            }

            if (!hasTarget)
                return false;

            try
            {
                Animator? animator = source.anim;
                if (animator == null)
                    animator = source.GetComponentInChildren<Animator>(true);

                if (animator == null)
                    return false;

                animator.Play("Base Layer.shoot", 0, 0f);
                if (animator.layerCount > 1)
                {
                    animator.SetLayerWeight(1, 1f);
                    animator.Play("electric.electric", 1, 0f);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static void HandleZombieDamaged(
            Zombie zombie,
            IDamageMaker damageFrom
        )
        {
            if (zombie == null ||
                damageFrom == null ||
                resolvingDoomExplosion ||
                resolvingIrradiationPulse ||
                resolvingDoomDirectDamage)
            {
                return;
            }

            if (!TryGetSourcePlant(
                    damageFrom,
                    out Plant? sourcePlant,
                    out PlantType sourceType
                ))
            {
                return;
            }

            if ((int)sourceType == Plants.Doomtronion.DoomtronionID)
            {
                HandleDoomtronionHit(zombie, sourcePlant);
                return;
            }

            if ((int)sourceType == Plants.LichenPea.LichenPeaID)
                HandleLichenPeaHit(zombie);
        }

        private static bool TryGetSourcePlant(
            IDamageMaker damageFrom,
            out Plant? sourcePlant,
            out PlantType sourceType
        )
        {
            sourcePlant = null;
            sourceType = PlantType.Nothing;

            try
            {
                if (damageFrom.IsBullet(out Bullet bullet) && bullet != null)
                {
                    if (bullet.from != null)
                    {
                        sourcePlant = bullet.from;
                        sourceType = sourcePlant.thePlantType;
                        return true;
                    }

                    sourceType = bullet.fromType;
                    return sourceType != PlantType.Nothing;
                }

                if (damageFrom.IsPlant(out Plant plant) && plant != null)
                {
                    sourcePlant = plant;
                    sourceType = plant.thePlantType;
                    return true;
                }
            }
            catch
            {
                sourcePlant = null;
                sourceType = PlantType.Nothing;
                return false;
            }

            return false;
        }

        private static void HandleDoomtronionHit(
            Zombie zombie,
            Plant? sourcePlant
        )
        {
            // Apply the same Ember-backed Irradiation as Witchfire Pumpkin
            // immediately. The queued state refreshes its duration on the
            // following board frame, but must not be the first application:
            // a connected Doomtronion can kill the target with its boosted
            // hit before the queue is processed.
            bool irradiationApplied =
                ApplyWitchfireIrradiation(zombie);
            QueueIrradiation(zombie, sourcePlant);

            if (!firstIrradiationLogged)
            {
                firstIrradiationLogged = true;
                Plugin.Logger.LogInfo(
                    "[Doomtronion] Irradiation applied immediately" +
                    " | Connected damage cannot skip effect" +
                    " | EffectType = Ember" +
                    " | Duration = " +
                    Plants.Doomtronion.IrradiationDuration + "s" +
                    " | Applied = " + irradiationApplied
                );
            }

            if (UnityEngine.Random.value >=
                Plants.Doomtronion.DoomExplosionChance)
            {
                return;
            }

            Vector3 world = zombie.transform.position;
            PendingDoomExplosions.Add(new PendingDoomExplosion
            {
                // A zombie transform can contain a sprite/pivot Y offset.
                // SetDoom expects the lawn-row center for its particle.
                Position = new Vector2(
                    world.x,
                    Lawnf.GetBoxYFromRow(zombie.theZombieRow)
                ),
                Column = Lawnf.GetColumnFromX(world.x),
                Row = zombie.theZombieRow
            });

            if (!firstDoomQueuedLogged)
            {
                firstDoomQueuedLogged = true;
                Plugin.Logger.LogInfo(
                    "[Doomtronion] Doom explosion queued safely" +
                    " | Chance = 25%" +
                    " | Damage = " + Plants.Doomtronion.DoomExplosionDamage
                );
            }
        }

        private static void HandleLichenPeaHit(Zombie zombie)
        {
            try
            {
                zombie.SetCold(Plants.LichenPea.ColdDuration);
            }
            catch
            {
                // Keep the projectile damage and plant-freeze roll even when
                // a special zombie refuses the normal cold status.
            }

            if (UnityEngine.Random.value >= Plants.LichenPea.FreezeChance)
                return;

            Vector3 world = zombie.transform.position;
            PendingLichenFreezes.Add(new PendingLichenFreeze
            {
                Column = Lawnf.GetColumnFromX(world.x),
                Row = zombie.theZombieRow
            });

            if (!firstLichenQueuedLogged)
            {
                firstLichenQueuedLogged = true;
                Plugin.Logger.LogInfo(
                    "[Lichen-pea] Plant freeze queued safely" +
                    " | Chance = 25%" +
                    " | Targets = 2-4 random plants"
                );
            }
        }

        internal static void TickQueuedEffects()
        {
            TickElectricArcs();
            ResolveIrradiations();
            ResolveDoomExplosions();
            ResolveLichenFreezes();
        }

        private static void ResolveDoomExplosions()
        {
            if (PendingDoomExplosions.Count == 0)
                return;

            List<PendingDoomExplosion> work =
                new List<PendingDoomExplosion>(PendingDoomExplosions);
            PendingDoomExplosions.Clear();

            Board? board = Board.Instance;
            if (board == null || board.boardAction == null)
                return;

            for (int index = 0; index < work.Count; index++)
            {
                PendingDoomExplosion state = work[index];

                try
                {
                    resolvingDoomExplosion = true;
                    board.boardAction.SetDoom(
                        state.Column,
                        state.Row,
                        false,
                        false,
                        state.Position,
                        Plants.Doomtronion.DoomExplosionDamage,
                        0,
                        null,
                        true,
                        (PlantType)Plants.Doomtronion.DoomtronionID
                    );

                    if (!firstDoomExplosionLogged)
                    {
                        firstDoomExplosionLogged = true;
                        Plugin.Logger.LogInfo(
                            "[Doomtronion] Native Doom-shroom explosion " +
                            "confirmed" +
                            " | Damage = " +
                            Plants.Doomtronion.DoomExplosionDamage +
                            " | Crater = false"
                        );
                    }
                }
                catch (Exception exception)
                {
                    Plugin.Logger.LogWarning(
                        "[Doomtronion] Doom explosion failed safely: " +
                        exception.Message
                    );
                }
                finally
                {
                    resolvingDoomExplosion = false;
                }
            }
        }

        private static void ResolveLichenFreezes()
        {
            if (PendingLichenFreezes.Count == 0)
                return;

            List<PendingLichenFreeze> work =
                new List<PendingLichenFreeze>(PendingLichenFreezes);
            PendingLichenFreezes.Clear();

            for (int stateIndex = 0; stateIndex < work.Count; stateIndex++)
            {
                PendingLichenFreeze state = work[stateIndex];
                var nearby = Lawnf.Get3x3Plants(state.Column, state.Row);

                if (nearby == null)
                    continue;

                List<Plant> candidates = new List<Plant>();
                HashSet<int> seen = new HashSet<int>();

                for (int index = 0; index < nearby.Count; index++)
                {
                    Plant? candidate = nearby[index];

                    if (candidate == null ||
                        candidate.dying ||
                        candidate.waitingDestory ||
                        candidate.thePlantHealth <= 0)
                    {
                        continue;
                    }

                    int instanceID = candidate.GetInstanceID();
                    if (!seen.Add(instanceID))
                        continue;

                    try
                    {
                        if (FreezedPlant.CanFreeze(candidate))
                            candidates.Add(candidate);
                    }
                    catch
                    {
                        // Some special support plants reject freezing.
                    }
                }

                Shuffle(candidates);

                int wanted = UnityEngine.Random.Range(
                    Plants.LichenPea.MinimumFrozenPlants,
                    Plants.LichenPea.MaximumFrozenPlants + 1
                );
                int targetCount = Mathf.Min(wanted, candidates.Count);
                int frozen = 0;

                for (int index = 0; index < targetCount; index++)
                {
                    try
                    {
                        if (FreezedPlant.FreezePlant(
                            candidates[index],
                            false
                        ) != null)
                        {
                            frozen++;
                        }
                    }
                    catch
                    {
                        // Continue with the other random plants.
                    }
                }

                if (frozen > 0 && !firstLichenFreezeLogged)
                {
                    firstLichenFreezeLogged = true;
                    Plugin.Logger.LogInfo(
                        "[Lichen-pea] Random plant freeze confirmed" +
                        " | Frozen = " + frozen +
                        " | Requested = " + wanted +
                        " | Area = 3x3 around hit zombie"
                    );
                }
            }
        }

        private static void Shuffle(List<Plant> plants)
        {
            for (int index = plants.Count - 1; index > 0; index--)
            {
                int swapIndex = UnityEngine.Random.Range(0, index + 1);
                Plant temporary = plants[index];
                plants[index] = plants[swapIndex];
                plants[swapIndex] = temporary;
            }
        }

        internal static void ClearQueuedEffects()
        {
            PendingDoomExplosions.Clear();
            PendingLichenFreezes.Clear();
            PendingIrradiations.Clear();

            IrradiatedZombies.Clear();
            ClearElectricArcs();
            resolvingDoomExplosion = false;
            resolvingIrradiationPulse = false;
            resolvingDoomDirectDamage = false;
        }
    }
}

namespace PlantsPlus.Plants
{
    using PlantsPlus.Core;

    public sealed class Doomtronion : MonoBehaviour
    {
        public const int DoomtronionID = 6013;
        public const int FallbackDamage = 100;
        public const float FallbackAttackInterval = 1.5f;
        public const int FallbackToughness = 300;
        public const float FallbackCardRecharge = 15f;
        public const int FallbackCardCost = 300;
        public const float IrradiationDuration = 10f;
        public const float DoomExplosionChance = 0.25f;
        public const int DoomExplosionDamage = 1800;
        public const int SupportDamage = 150;
        public const int MaximumChainTargets = 3;
        public const float ChainRadius = 1.6f;

        private Prismflower? plant;
        private float attackTimer = 0.25f;
        private bool attackPending;

        public Doomtronion(IntPtr pointer) : base(pointer) { }

        public void Start()
        {
            V11ExpansionBootstrap.ConfigureDoomtronionInstance(gameObject);
            plant = gameObject.GetComponent<Prismflower>();
            attackTimer = 0.25f;
            attackPending = false;

            Plugin.Logger.LogInfo(
                "[Doomtronion] Ready" +
                " | Attack = Electronion network + capped chain" +
                " | Damage = " + FallbackDamage +
                "/" + FallbackAttackInterval + "s" +
                " | Support = +150 each" +
                " | Chain = 3 targets at 50%" +
                " | Irradiation = every damaged zombie" +
                " | Doom chance = 25%" +
                " | Doom damage = " + DoomExplosionDamage
            );
        }

        public void Update()
        {
            float delta = Time.deltaTime;
            if (delta <= 0f)
                return;

            if (plant == null)
                plant = gameObject.GetComponent<Prismflower>();

            if (plant == null ||
                plant.dying ||
                plant.waitingDestory ||
                plant.thePlantHealth <= 0)
            {
                return;
            }

            attackTimer -= delta;
            if (attackTimer > 0f || attackPending)
                return;

            attackPending =
                V11ExpansionBootstrap.BeginDoomtronionAttack(plant);
            attackTimer = attackPending
                ? FallbackAttackInterval
                : 0.20f;
        }

        public void ResolveAnimationImpact()
        {
            if (!attackPending)
                return;

            attackPending = false;
            if (plant == null)
                plant = gameObject.GetComponent<Prismflower>();

            if (plant != null)
            {
                V11ExpansionBootstrap.ExecuteDoomtronionAttack(plant);
            }
        }
    }

    public sealed class LichenPea : MonoBehaviour
    {
        public const int LichenPeaID = 6014;
        public const int LichenPeaBulletID = 6014;
        public const int FallbackDamage = 20;
        public const float FallbackAttackInterval = 1.5f;
        public const int FallbackToughness = 300;
        public const float FallbackCardRecharge = 7.5f;
        public const int FallbackCardCost = 150;
        public const float ColdDuration = 10f;
        public const float FreezeChance = 0.25f;
        public const int MinimumFrozenPlants = 2;
        public const int MaximumFrozenPlants = 4;

        public LichenPea(IntPtr pointer) : base(pointer) { }

        public void Start()
        {
            V11PlantsBootstrap.ApplyLocalAnimationController(
                gameObject,
                "Lichen-pea"
            );

            PeaShooter? plant = gameObject.GetComponent<PeaShooter>();
            string bridge = plant != null
                ? V11PlantsBootstrap.EnsureShooterRuntimeReferences(
                    plant,
                    "Lichen-pea"
                )
                : "missing PeaShooter";

            Plugin.Logger.LogInfo(
                "[Lichen-pea] Ready" +
                " | Projectile = Bullet_pea + native Cold effect" +
                " | Plant-freeze chance = 25%" +
                " | Frozen plants = 2-4" +
                " | Bridge = " + bridge
            );
        }
    }

    [HarmonyPatch]
    internal static class V11ExpansionCombatPatches
    {
        [HarmonyPatch(typeof(Zombie), nameof(Zombie.TakeDamage))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void ZombieTakeDamagePostfix(
            Zombie __instance,
            IDamageMaker damageFrom
        )
        {
            try
            {
                V11ExpansionBootstrap.HandleZombieDamaged(
                    __instance,
                    damageFrom
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Plants+ V1.1] Hit effect skipped safely: " +
                    exception.Message
                );
            }
        }

        [HarmonyPatch(typeof(Board), nameof(Board.Update))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void BoardUpdatePostfix()
        {
            try
            {
                V11ExpansionBootstrap.TickQueuedEffects();
                NightRoofCards.TryEnsureSandboxElectronion();
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Plants+ V1.1] Deferred effect frame skipped safely: " +
                    exception.Message
                );
            }
        }

        [HarmonyPatch(typeof(Board), nameof(Board.OnDestroy))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void BoardDestroyPrefix()
        {
            V11ExpansionBootstrap.ClearQueuedEffects();
        }

        [HarmonyPatch(typeof(Prismflower), nameof(Prismflower.SearchZombieUpdate))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool PrismflowerSearchZombieUpdatePrefix(
            Prismflower __instance
        )
        {
            return __instance == null ||
                (int)__instance.thePlantType != Doomtronion.DoomtronionID;
        }

        [HarmonyPatch(typeof(Prismflower), nameof(Prismflower.AnimShoot))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool PrismflowerAnimShootPrefix(
            Prismflower __instance
        )
        {
            if (__instance == null ||
                (int)__instance.thePlantType != Doomtronion.DoomtronionID)
            {
                return true;
            }

            // shoot.anim invokes AnimShoot at exactly 0.500 seconds. Consume
            // the native event and resolve the complete Plants+ impact there.
            Doomtronion? behaviour =
                __instance.gameObject.GetComponent<Doomtronion>();
            if (behaviour != null)
                behaviour.ResolveAnimationImpact();

            return false;
        }

        [HarmonyPatch(typeof(Lawnf), nameof(Lawnf.CheckPlantClass))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void CheckPlantClassPostfix(
            PlantType thePlantType,
            ref int __result
        )
        {
            if ((int)thePlantType == Doomtronion.DoomtronionID)
            {
                __result = Lawnf.CheckPlantClass(PlantType.ElectricOnion);
            }
            else if ((int)thePlantType == LichenPea.LichenPeaID)
            {
                __result = Lawnf.CheckPlantClass(PlantType.Peashooter);
            }
        }
    }
}
