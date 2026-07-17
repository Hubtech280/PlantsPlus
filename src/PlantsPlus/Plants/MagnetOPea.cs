using CustomizeLib.BepInEx;
using HarmonyLib;
using Il2Cpp;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlantsPlus.Plants
{
    public class MagnetOPea : MonoBehaviour
    {
        public const int MagnetOPeaID = 6002;
        public const int BaseDamage = 20;
        public const float AttackInterval = 1.5f;
        public const float NormalPeaVisualScale = 1.15f;

        // Enemy projectiles cannot be passed to CreateBullet. A normal pea is
        // therefore used only as the invisible, plant-safe collision host.
        private static readonly BulletType NativeVisualHostBulletType =
            BulletType.Bullet_pea;

        public static bool CustomNormalPeaVisualReady { get; private set; }

        private static SpriteRenderer? normalPeaVisualSource;
        private static SpriteRenderer? normalPeaShadowSource;
        private static GameObject? zomppelinProjectileVisualPrefab;
        private static GameObject? flagshipProjectileVisualPrefab;
        private static GameObject? nativeVisualTemplateRoot;
        private static bool zomppelinProbeAttempted;
        private static bool zomppelinVisualResolvedLogged;
        private static bool flagshipVisualResolvedLogged;
        private static bool zomppelinVisualWarningLogged;
        private static bool flagshipVisualWarningLogged;
        private static bool jackboxExplosionLogged;
        private static bool gigaExplosionLogged;
        private static bool explosionWarningLogged;
        private static bool visualTargetWarningLogged;
        private static bool missingBehaviourWarningLogged;
        private static bool metalDamageLogEmitted;
        private static bool directShotLogEmitted;
        private static bool fallbackShotOriginWarningLogged;
        private static bool missingBulletCreatorErrorLogged;

        private enum AbsorbedItem
        {
            Normal,
            Bucket,
            Football,
            Jackbox,
            Chrono,
            GigaMecha
        }

        private sealed class RendererSnapshot
        {
            public readonly SpriteRenderer Renderer;
            public readonly Sprite? Sprite;
            public readonly Color Color;
            public readonly bool FlipX;
            public readonly bool FlipY;
            public readonly bool Enabled;
            public readonly Vector3 LocalPosition;
            public readonly Quaternion LocalRotation;
            public readonly Vector3 LocalScale;

            public RendererSnapshot(SpriteRenderer renderer)
            {
                Renderer = renderer;
                Sprite = renderer.sprite;
                Color = renderer.color;
                FlipX = renderer.flipX;
                FlipY = renderer.flipY;
                Enabled = renderer.enabled;
                LocalPosition = renderer.transform.localPosition;
                LocalRotation = renderer.transform.localRotation;
                LocalScale = renderer.transform.localScale;
            }

            public void Restore()
            {
                if (Renderer == null || Renderer.transform == null)
                    return;

                Renderer.sprite = Sprite;
                Renderer.color = Color;
                Renderer.flipX = FlipX;
                Renderer.flipY = FlipY;
                Renderer.enabled = Enabled;
                Renderer.transform.localPosition = LocalPosition;
                Renderer.transform.localRotation = LocalRotation;
                Renderer.transform.localScale = LocalScale;
            }
        }

        // The component owns the state. The dictionary is only a safe fallback
        // for IL2CPP wrappers that temporarily fail a generic GetComponent call.
        private static readonly Dictionary<int, MagnetOPea> ActiveBehaviours =
            new Dictionary<int, MagnetOPea>();

        // Bucket.Use normally stops after the first Plant.UseItem that returns
        // true. This guard additionally prevents the same live item from being
        // accepted twice if another mod enumerates several plants itself.
        private static readonly HashSet<int> ConsumedBucketIDs =
            new HashSet<int>();

        // Bullets are pooled by the game. Every renderer changed by Plants+
        // is restored before Die and again when SetBullet reuses the object.
        private static readonly Dictionary<int, List<RendererSnapshot>>
            ProjectileVisualSnapshots =
                new Dictionary<int, List<RendererSnapshot>>();

        private static readonly Dictionary<int, List<GameObject>>
            ProjectileVisualClones =
                new Dictionary<int, List<GameObject>>();

        // Jackbox and Giga Mecha shots keep a plant-safe Bullet_pea for
        // movement/collision. This table adds the native area explosion only
        // to those exact pooled bullet instances.
        private static readonly Dictionary<int, AbsorbedItem>
            ExplosiveProjectileModes =
                new Dictionary<int, AbsorbedItem>();

        private AbsorbedItem absorbedItem = AbsorbedItem.Normal;
        private AbsorbedItem lastVerifiedShot = (AbsorbedItem)(-1);
        private int registeredPlantID = int.MinValue;

        public MagnetOPea(IntPtr ptr) : base(ptr) { }

        public PeaShooter? PeaPlant => gameObject.GetComponent<PeaShooter>();

        public void Start()
        {
            PeaShooter? plant = PeaPlant;

            if (plant == null)
            {
                Plugin.Logger.LogError(
                    "[MagnetOPea] Start failed: no PeaShooter component."
                );
                return;
            }

            registeredPlantID = plant.GetInstanceID();
            absorbedItem = AbsorbedItem.Normal;
            lastVerifiedShot = (AbsorbedItem)(-1);
            ActiveBehaviours[registeredPlantID] = this;

            string shotOrigin = EnsureShotOrigin(plant);

            Plugin.Logger.LogInfo(
                "[MagnetOPea] Ready | Shot path = direct Shoot1 override" +
                " | Origin = " + shotOrigin +
                " | No attraction ability" +
                " | Current shot = " + GetCurrentBulletType(this) +
                " | Custom normal visual = " + CustomNormalPeaVisualReady +
                " | Normal pea scale = " + NormalPeaVisualScale + "x"
            );
        }

        public static void ConfigureNormalPeaVisual(
            SpriteRenderer visualSource,
            SpriteRenderer? shadowSource
        )
        {
            normalPeaVisualSource = visualSource;
            normalPeaShadowSource = shadowSource;
            CustomNormalPeaVisualReady = visualSource != null;
        }

        public void OnDestroy()
        {
            if (registeredPlantID == int.MinValue)
                return;

            ActiveBehaviours.Remove(registeredPlantID);
        }

        private static MagnetOPea? GetBehaviour(Plant? plant)
        {
            if (plant == null)
                return null;

            try
            {
                MagnetOPea? behaviour =
                    plant.gameObject.GetComponent<MagnetOPea>();

                if (behaviour != null)
                    return behaviour;
            }
            catch
            {
                // Fall through to the instance-ID lookup below.
            }

            ActiveBehaviours.TryGetValue(
                plant.GetInstanceID(),
                out MagnetOPea? fallback
            );

            return fallback;
        }

        private static bool TryGetAbsorbedItem(
            BucketType type,
            out AbsorbedItem item
        )
        {
            switch (type)
            {
                case BucketType.Bucket:
                    item = AbsorbedItem.Bucket;
                    return true;

                case BucketType.Helmet:
                    item = AbsorbedItem.Football;
                    return true;

                case BucketType.PortalHeart:
                    item = AbsorbedItem.Chrono;
                    return true;

                case BucketType.Jackbox:
                    item = AbsorbedItem.Jackbox;
                    return true;

                case BucketType.SuperMachine:
                    item = AbsorbedItem.GigaMecha;
                    return true;
            }

            item = AbsorbedItem.Normal;
            return false;
        }

        private static bool TryAbsorbItem(
            Plant plant,
            BucketType type,
            Bucket? bucket
        )
        {
            MagnetOPea? behaviour = GetBehaviour(plant);

            if (behaviour == null || !TryGetAbsorbedItem(type, out var nextItem))
                return false;

            if (bucket == null || bucket.gameObject == null)
            {
                Plugin.Logger.LogWarning(
                    "[MagnetOPea] Item refused: the Bucket object is missing."
                );
                return false;
            }

            int bucketID;

            try
            {
                bucketID = bucket.GetInstanceID();
            }
            catch
            {
                return false;
            }

            if (!ConsumedBucketIDs.Add(bucketID))
            {
                Plugin.Logger.LogWarning(
                    "[MagnetOPea] Duplicate absorption prevented | Item = " + type
                );
                return false;
            }

            behaviour.absorbedItem = nextItem;
            behaviour.lastVerifiedShot = (AbsorbedItem)(-1);

            // Destruction is scheduled by Unity, so Bucket.Use can finish its
            // current call safely while the real object is consumed exactly once.
            UnityEngine.Object.Destroy(bucket.gameObject);

            Plugin.Logger.LogInfo(
                "[MagnetOPea] Item absorbed | Item = " + type +
                " | Mode = " + nextItem +
                " | Next bullet = " + GetCurrentBulletType(behaviour)
            );

            return true;
        }

        private static BulletType GetCurrentBulletType(MagnetOPea behaviour)
        {
            switch (behaviour.absorbedItem)
            {
                case AbsorbedItem.Bucket:
                    return BulletType.Bullet_ironPea;

                case AbsorbedItem.Football:
                    return BulletType.Bullet_helmetPea;

                case AbsorbedItem.Chrono:
                    return BulletType.Bullet_portalPea;

                case AbsorbedItem.Jackbox:
                case AbsorbedItem.GigaMecha:
                    return NativeVisualHostBulletType;
            }

            return BulletType.Bullet_pea;
        }

        private static Transform? FindDescendant(
            Transform root,
            string childName
        )
        {
            if (root == null)
                return null;

            if (string.Equals(root.name, childName, StringComparison.Ordinal))
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform? found = FindDescendant(root.GetChild(i), childName);

                if (found != null)
                    return found;
            }

            return null;
        }

        private static SpriteRenderer? FindNamedRenderer(
            Transform root,
            string childName
        )
        {
            Transform? target = FindDescendant(root, childName);
            return target != null
                ? target.GetComponent<SpriteRenderer>()
                : null;
        }

        private static SpriteRenderer? FindMainRenderer(Transform root)
        {
            SpriteRenderer? named = FindNamedRenderer(root, "Sprite");

            if (named != null)
                return named;

            SpriteRenderer? fallback = null;

            foreach (
                SpriteRenderer renderer in
                root.GetComponentsInChildren<SpriteRenderer>(true)
            )
            {
                if (renderer == null || renderer.sprite == null)
                    continue;

                fallback ??= renderer;
                string name = renderer.name.ToLowerInvariant();

                if (
                    !name.Contains("shadow") &&
                    !name.Contains("tail") &&
                    !name.Contains("trail")
                )
                {
                    return renderer;
                }
            }

            return fallback;
        }

        private static void CaptureRenderer(
            Bullet bullet,
            SpriteRenderer renderer
        )
        {
            int bulletID = bullet.GetInstanceID();

            if (
                !ProjectileVisualSnapshots.TryGetValue(
                    bulletID,
                    out List<RendererSnapshot>? snapshots
                )
            )
            {
                snapshots = new List<RendererSnapshot>();
                ProjectileVisualSnapshots.Add(bulletID, snapshots);
            }

            foreach (RendererSnapshot snapshot in snapshots)
            {
                if (snapshot.Renderer == renderer)
                    return;
            }

            snapshots.Add(new RendererSnapshot(renderer));
        }

        private static void RestoreProjectileVisual(Bullet? bullet)
        {
            if (bullet == null)
                return;

            int bulletID;

            try
            {
                bulletID = bullet.GetInstanceID();
            }
            catch
            {
                return;
            }

            // A pooled bullet must never keep the explosion mode of its
            // previous owner. On a normal hit the mode is already consumed by
            // TriggerNativeExplosion before Bullet.Die reaches this cleanup.
            ExplosiveProjectileModes.Remove(bulletID);

            if (
                ProjectileVisualSnapshots.TryGetValue(
                    bulletID,
                    out List<RendererSnapshot>? snapshots
                )
            )
            {
                // Remove first so a Unity callback triggered by a restore
                // cannot re-enter with a half-restored snapshot list.
                ProjectileVisualSnapshots.Remove(bulletID);

                foreach (RendererSnapshot snapshot in snapshots)
                {
                    try
                    {
                        snapshot.Restore();
                    }
                    catch
                    {
                        // Scene unload can destroy a renderer before Die.
                    }
                }
            }

            if (
                ProjectileVisualClones.TryGetValue(
                    bulletID,
                    out List<GameObject>? clones
                )
            )
            {
                ProjectileVisualClones.Remove(bulletID);

                foreach (GameObject clone in clones)
                {
                    if (clone == null)
                        continue;

                    try
                    {
                        clone.SetActive(false);
                        UnityEngine.Object.Destroy(clone);
                    }
                    catch
                    {
                        // The scene may already have destroyed the clone.
                    }
                }
            }
        }

        private static bool CopyProjectileRenderer(
            Bullet bullet,
            SpriteRenderer? target,
            SpriteRenderer? source
        )
        {
            if (source == null)
                return true;

            if (bullet == null || target == null || target.transform == null)
                return false;

            CaptureRenderer(bullet, target);

            target.sprite = source.sprite;
            target.color = source.color;
            target.flipX = source.flipX;
            target.flipY = source.flipY;
            target.transform.localPosition = source.transform.localPosition;
            target.transform.localRotation = source.transform.localRotation;
            // Only Magnet-o-pea's base custom pea is enlarged. Native Bucket,
            // Football, Chrono, Zomppelin and Flagship projectiles never use
            // this renderer-copy path and therefore keep their current size.
            target.transform.localScale =
                source.transform.localScale * NormalPeaVisualScale;

            return true;
        }

        private static void ApplyNormalPeaVisual(Bullet bullet)
        {
            if (
                !CustomNormalPeaVisualReady ||
                bullet == null ||
                bullet.transform == null
            )
            {
                return;
            }

            bool visualApplied = CopyProjectileRenderer(
                bullet,
                FindNamedRenderer(bullet.transform, "Sprite"),
                normalPeaVisualSource
            );

            bool shadowApplied = CopyProjectileRenderer(
                bullet,
                FindNamedRenderer(bullet.transform, "Shadow"),
                normalPeaShadowSource
            );

            if (
                (!visualApplied || !shadowApplied) &&
                !visualTargetWarningLogged
            )
            {
                visualTargetWarningLogged = true;
                Plugin.Logger.LogWarning(
                    "[MagnetOPea] The vanilla pea visual hierarchy was not " +
                    "found completely. Its original visual will be kept."
                );
            }
        }

        private static GameObject GetNativeVisualTemplateRoot()
        {
            if (nativeVisualTemplateRoot != null)
                return nativeVisualTemplateRoot;

            nativeVisualTemplateRoot = new GameObject(
                "[Plants+] Native projectile visual templates"
            );
            nativeVisualTemplateRoot.hideFlags = HideFlags.HideAndDontSave;
            nativeVisualTemplateRoot.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(nativeVisualTemplateRoot);
            return nativeVisualTemplateRoot;
        }

        private static bool CaptureZomppelinBombTemplate(
            GameObject source,
            string origin
        )
        {
            if (source == null)
                return false;

            try
            {
                GameObject template = UnityEngine.Object.Instantiate(
                    source,
                    GetNativeVisualTemplateRoot().transform
                );
                template.name = "[Plants+] Zomppelin KirovBomb template";
                template.transform.localPosition = Vector3.zero;
                template.transform.localRotation = Quaternion.identity;
                template.SetActive(false);
                zomppelinProjectileVisualPrefab = template;

                if (!zomppelinVisualResolvedLogged)
                {
                    zomppelinVisualResolvedLogged = true;
                    Plugin.Logger.LogInfo(
                        "[MagnetOPea] Native Zomppelin projectile resolved" +
                        " | Zombie = " + ZombieType.KirovZombie +
                        " (26) | Component = KirovBomb" +
                        " | Source = " + origin +
                        " | Object = " + source.name
                    );
                }

                return true;
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[MagnetOPea] KirovBomb template capture failed: " +
                    exception.Message
                );
                return false;
            }
        }

        private static GameObject? FindBombVisualInZomppelin(
            GameObject zomppelin
        )
        {
            if (zomppelin == null)
                return null;

            Transform? best = null;
            int bestScore = 0;

            foreach (
                Transform candidate in
                zomppelin.GetComponentsInChildren<Transform>(true)
            )
            {
                if (
                    candidate == null ||
                    candidate == zomppelin.transform ||
                    candidate.gameObject == null
                )
                {
                    continue;
                }

                string name = candidate.name
                    .Replace("_", string.Empty)
                    .Replace("-", string.Empty)
                    .Replace(" ", string.Empty)
                    .ToLowerInvariant();

                int score = 0;

                if (name.Contains("kirovbomb"))
                    score = 5;
                else if (name.Contains("zomppelinbomb"))
                    score = 4;
                else if (name.Contains("bomb"))
                    score = 3;
                else if (name.Contains("zhadan") || name.Contains("炸弹"))
                    score = 2;

                if (
                    score <= bestScore ||
                    candidate.GetComponentsInChildren<Renderer>(true).Length == 0
                )
                {
                    continue;
                }

                best = candidate;
                bestScore = score;
            }

            return best != null ? best.gameObject : null;
        }

        private static bool TryProbeZomppelinBomb(
            ResourcesManager resources
        )
        {
            if (
                zomppelinProbeAttempted ||
                Board.Instance == null ||
                resources == null ||
                resources.zombiePrefabs == null ||
                !resources.zombiePrefabs.ContainsKey(ZombieType.KirovZombie)
            )
            {
                return false;
            }

            zomppelinProbeAttempted = true;
            GameObject? probeRoot = null;

            try
            {
                HashSet<int> existingBombIDs = new HashSet<int>();

                foreach (
                    KirovBomb existing in
                    UnityEngine.Resources.FindObjectsOfTypeAll<KirovBomb>()
                )
                {
                    if (existing != null)
                        existingBombIDs.Add(existing.GetInstanceID());
                }

                probeRoot = new GameObject(
                    "[Plants+] Temporary Zomppelin bomb probe"
                );
                probeRoot.hideFlags = HideFlags.HideAndDontSave;
                probeRoot.transform.position = new Vector3(-100f, -100f, 0f);
                probeRoot.SetActive(false);

                GameObject probeZombie = UnityEngine.Object.Instantiate(
                    resources.zombiePrefabs[ZombieType.KirovZombie],
                    probeRoot.transform
                );
                probeZombie.transform.localPosition = Vector3.zero;

                KirovAirship? airship =
                    probeZombie.GetComponent<KirovAirship>();

                if (airship == null)
                    throw new InvalidOperationException(
                        "ZombieType.KirovZombie has no KirovAirship component"
                    );

                airship.theZombieRow = 0;
                airship.board = Board.Instance;

                // Invoke CreateBomb while the cloned zombie remains inactive.
                // This supplies its only required runtime references manually
                // and prevents the probe from entering Board.zombieArray.
                airship.CreateBomb();

                List<GameObject> spawnedBombs = new List<GameObject>();
                GameObject? capturedBomb = null;

                foreach (
                    KirovBomb bomb in
                    UnityEngine.Resources.FindObjectsOfTypeAll<KirovBomb>()
                )
                {
                    if (
                        bomb == null ||
                        bomb.gameObject == null ||
                        existingBombIDs.Contains(bomb.GetInstanceID())
                    )
                    {
                        continue;
                    }

                    spawnedBombs.Add(bomb.gameObject);
                    capturedBomb ??= bomb.gameObject;
                }

                bool captured = capturedBomb != null &&
                    CaptureZomppelinBombTemplate(
                        capturedBomb,
                        "KirovAirship.CreateBomb probe"
                    );

                foreach (GameObject spawnedBomb in spawnedBombs)
                {
                    if (spawnedBomb != null)
                    {
                        spawnedBomb.SetActive(false);
                        UnityEngine.Object.Destroy(spawnedBomb);
                    }
                }

                if (!captured)
                {
                    Plugin.Logger.LogWarning(
                        "[MagnetOPea] KirovAirship.CreateBomb did not expose " +
                        "a KirovBomb object. The safe fallback will be used."
                    );
                }

                return captured;
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[MagnetOPea] Zomppelin bomb probe failed: " +
                    exception.Message
                );
                return false;
            }
            finally
            {
                if (probeRoot != null)
                {
                    probeRoot.SetActive(false);
                    UnityEngine.Object.Destroy(probeRoot);
                }
            }
        }

        private static void TryResolveNativeProjectileVisuals(
            bool allowZomppelinProbe = false
        )
        {
            ResourcesManager? resources = GameAPP.resourcesManager;

            if (resources == null || resources.zombiePrefabs == null)
                return;

            if (zomppelinProjectileVisualPrefab == null)
            {
                try
                {
                    // The normal Zomppelin is ZombieType.KirovZombie (26),
                    // not SuperKirov (205). CreateBomb constructs KirovBomb at
                    // runtime, so it is not guaranteed to exist as a resource.
                    if (
                        resources.zombiePrefabs.ContainsKey(
                            ZombieType.KirovZombie
                        )
                    )
                    {
                        GameObject zomppelin =
                            resources.zombiePrefabs[ZombieType.KirovZombie];

                        if (zomppelin != null)
                        {
                            foreach (
                                KirovBomb bomb in
                                zomppelin.GetComponentsInChildren<KirovBomb>(
                                    true
                                )
                            )
                            {
                                if (bomb != null && bomb.gameObject != null)
                                {
                                    CaptureZomppelinBombTemplate(
                                        bomb.gameObject,
                                        "Zomppelin prefab component"
                                    );
                                    break;
                                }
                            }

                            if (zomppelinProjectileVisualPrefab == null)
                            {
                                GameObject? carriedBomb =
                                    FindBombVisualInZomppelin(zomppelin);

                                if (carriedBomb != null)
                                {
                                    CaptureZomppelinBombTemplate(
                                        carriedBomb,
                                        "Zomppelin carried-bomb hierarchy"
                                    );
                                }
                            }
                        }
                    }

                    // A real Zomppelin may already have dropped a bomb. Clone
                    // it into a persistent inactive template; never keep a
                    // reference to the live object that is about to explode.
                    if (zomppelinProjectileVisualPrefab == null)
                    {
                        KirovBomb? fallback = null;

                        foreach (
                            KirovBomb bomb in
                            UnityEngine.Resources
                                .FindObjectsOfTypeAll<KirovBomb>()
                        )
                        {
                            if (bomb == null || bomb.gameObject == null)
                                continue;

                            fallback ??= bomb;
                            string name = bomb.gameObject.name.ToLowerInvariant();

                            if (
                                name.Contains("kirov") ||
                                name.Contains("zomppelin") ||
                                name.Contains("bomb")
                            )
                            {
                                CaptureZomppelinBombTemplate(
                                    bomb.gameObject,
                                    "loaded KirovBomb"
                                );
                                break;
                            }
                        }

                        if (
                            zomppelinProjectileVisualPrefab == null &&
                            fallback != null
                        )
                        {
                            CaptureZomppelinBombTemplate(
                                fallback.gameObject,
                                "loaded KirovBomb fallback"
                            );
                        }
                    }

                    if (
                        zomppelinProjectileVisualPrefab == null &&
                        allowZomppelinProbe
                    )
                    {
                        TryProbeZomppelinBomb(resources);
                    }
                }
                catch (Exception exception)
                {
                    Plugin.Logger.LogWarning(
                        "[MagnetOPea] Zomppelin projectile lookup failed: " +
                        exception.Message
                    );
                }
            }

            if (flagshipProjectileVisualPrefab == null)
            {
                try
                {
                    if (
                        resources.zombiePrefabs.ContainsKey(
                            ZombieType.Kirov_c
                        )
                    )
                    {
                        GameObject flagshipPrefab =
                            resources.zombiePrefabs[ZombieType.Kirov_c];
                        Kirov_c? flagship = flagshipPrefab != null
                            ? flagshipPrefab.GetComponent<Kirov_c>()
                            : null;

                        flagshipProjectileVisualPrefab = flagship != null
                            ? flagship.bombPrefab
                            : null;
                    }

                    if (
                        flagshipProjectileVisualPrefab != null &&
                        !flagshipVisualResolvedLogged
                    )
                    {
                        flagshipVisualResolvedLogged = true;
                        Plugin.Logger.LogInfo(
                            "[MagnetOPea] Native Flagship projectile resolved" +
                            " | Zombie = " + ZombieType.Kirov_c +
                            " (329) | Field = Kirov_c.bombPrefab" +
                            " | Prefab = " +
                            flagshipProjectileVisualPrefab.name
                        );
                    }
                }
                catch (Exception exception)
                {
                    Plugin.Logger.LogWarning(
                        "[MagnetOPea] Flagship projectile lookup failed: " +
                        exception.Message
                    );
                }
            }
        }

        private static bool TryGetVisualBounds(
            GameObject visual,
            out Bounds bounds
        )
        {
            bounds = default;
            bool found = false;

            // Prefer actual 2D artwork. Particle/trail bounds are often much
            // larger than the visible projectile and caused tiny or displaced
            // results in the previous implementation.
            foreach (
                SpriteRenderer renderer in
                visual.GetComponentsInChildren<SpriteRenderer>(true)
            )
            {
                if (
                    renderer == null ||
                    !renderer.enabled ||
                    renderer.sprite == null ||
                    renderer.name.ToLowerInvariant().Contains("shadow")
                )
                {
                    continue;
                }

                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (found)
                return true;

            // Spine/mesh projectiles do not expose SpriteRenderers. Use their
            // mesh renderer while still excluding effects and trails.
            foreach (
                Renderer renderer in
                visual.GetComponentsInChildren<Renderer>(true)
            )
            {
                if (renderer == null || !renderer.enabled)
                    continue;

                string name = renderer.name.ToLowerInvariant();
                string rendererType = renderer.GetType().Name.ToLowerInvariant();

                // A projectile shadow should remain below the projectile, but
                // must not move the projectile body away from the shoot point.
                if (
                    name.Contains("shadow") ||
                    name.Contains("trail") ||
                    rendererType.Contains("particle") ||
                    rendererType.Contains("trail") ||
                    rendererType.Contains("line")
                )
                {
                    continue;
                }

                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return found;
        }

        private static void StoreVisualClone(Bullet bullet, GameObject clone)
        {
            int bulletID = bullet.GetInstanceID();

            if (
                !ProjectileVisualClones.TryGetValue(
                    bulletID,
                    out List<GameObject>? clones
                )
            )
            {
                clones = new List<GameObject>();
                ProjectileVisualClones.Add(bulletID, clones);
            }

            clones.Add(clone);
        }

        private static bool ApplyNativeProjectileVisual(
            Bullet bullet,
            GameObject? visualPrefab,
            string modeName,
            float maxProjectileWidth,
            float maxProjectileHeight,
            ref bool warningLogged
        )
        {
            if (
                bullet == null ||
                bullet.transform == null ||
                visualPrefab == null
            )
            {
                if (!warningLogged)
                {
                    warningLogged = true;
                    Plugin.Logger.LogWarning(
                        "[MagnetOPea] " + modeName +
                        " projectile prefab is unavailable. " +
                        "The plant-safe pea host will remain visible."
                    );
                }

                return false;
            }

            GameObject? clone = null;

            try
            {
                clone = UnityEngine.Object.Instantiate(
                    visualPrefab,
                    bullet.transform
                );
                clone.name = "[Plants+] " + modeName + " projectile visual";
                clone.SetActive(false);

                // Strip every gameplay path before activating the clone. The
                // real Bullet_pea remains responsible for movement and damage.
                foreach (
                    MonoBehaviour behaviour in
                    clone.GetComponentsInChildren<MonoBehaviour>(true)
                )
                {
                    if (behaviour != null)
                        behaviour.enabled = false;
                }

                foreach (
                    Collider2D collider in
                    clone.GetComponentsInChildren<Collider2D>(true)
                )
                {
                    if (collider != null)
                        collider.enabled = false;
                }

                foreach (
                    Rigidbody2D body in
                    clone.GetComponentsInChildren<Rigidbody2D>(true)
                )
                {
                    if (body != null)
                        body.simulated = false;
                }

                foreach (
                    Transform part in
                    clone.GetComponentsInChildren<Transform>(true)
                )
                {
                    if (part == null || part.gameObject == null)
                        continue;

                    part.gameObject.tag = "Untagged";
                    part.gameObject.layer = bullet.gameObject.layer;
                }

                clone.transform.localPosition = Vector3.zero;
                clone.transform.localRotation = Quaternion.identity;

                // Native zombie projectiles point toward the plants. Flip the
                // clone so it points toward zombies when fired by a plant.
                Vector3 sourceScale = clone.transform.localScale;
                clone.transform.localScale = new Vector3(
                    -sourceScale.x,
                    sourceScale.y,
                    sourceScale.z
                );
                clone.SetActive(true);

                if (!TryGetVisualBounds(clone, out Bounds bounds))
                    throw new InvalidOperationException(
                        "the prefab has no enabled Renderer"
                    );

                float width = Mathf.Max(bounds.size.x, 0.001f);
                float height = Mathf.Max(bounds.size.y, 0.001f);
                float scaleFactor = Mathf.Min(
                    maxProjectileWidth / width,
                    maxProjectileHeight / height
                );
                scaleFactor = Mathf.Clamp(scaleFactor, 0.05f, 5f);
                clone.transform.localScale *= scaleFactor;

                if (!TryGetVisualBounds(clone, out bounds))
                    throw new InvalidOperationException(
                        "the prefab bounds disappeared after scaling"
                    );

                // Center the visible body on the real bullet. This deliberately
                // ignores the enemy prefab's original altitude and shoot point.
                clone.transform.position +=
                    bullet.transform.position - bounds.center;

                // Hide every SpriteRenderer belonging to the pea host, but do
                // not touch renderers inside the newly-created visual clone.
                foreach (
                    SpriteRenderer renderer in
                    bullet.transform.GetComponentsInChildren<SpriteRenderer>(
                        true
                    )
                )
                {
                    if (
                        renderer == null ||
                        renderer.transform == null ||
                        renderer.transform.IsChildOf(clone.transform)
                    )
                    {
                        continue;
                    }

                    CaptureRenderer(bullet, renderer);
                    renderer.enabled = false;
                }

                StoreVisualClone(bullet, clone);
                return true;
            }
            catch (Exception exception)
            {
                if (clone != null)
                {
                    clone.SetActive(false);
                    UnityEngine.Object.Destroy(clone);
                }

                if (!warningLogged)
                {
                    warningLogged = true;
                    Plugin.Logger.LogWarning(
                        "[MagnetOPea] " + modeName +
                        " visual clone failed: " + exception.Message
                    );
                }

                return false;
            }
        }

        private static void ApplyZomppelinProjectileVisual(Bullet bullet)
        {
            TryResolveNativeProjectileVisuals(true);
            ApplyNativeProjectileVisual(
                bullet,
                zomppelinProjectileVisualPrefab,
                "Zomppelin (26)",
                0.72f,
                0.46f,
                ref zomppelinVisualWarningLogged
            );
        }

        private static void ApplyFlagshipProjectileVisual(Bullet bullet)
        {
            TryResolveNativeProjectileVisuals();
            ApplyNativeProjectileVisual(
                bullet,
                flagshipProjectileVisualPrefab,
                "Kirov Flagship (329)",
                0.82f,
                0.53f,
                ref flagshipVisualWarningLogged
            );
        }

        private static void RegisterExplosiveProjectile(
            Bullet bullet,
            AbsorbedItem mode
        )
        {
            if (
                bullet == null ||
                (mode != AbsorbedItem.Jackbox &&
                 mode != AbsorbedItem.GigaMecha)
            )
            {
                return;
            }

            ExplosiveProjectileModes[bullet.GetInstanceID()] = mode;
        }

        private static void TriggerNativeExplosion(Bullet bullet)
        {
            if (bullet == null)
                return;

            int bulletID;

            try
            {
                bulletID = bullet.GetInstanceID();
            }
            catch
            {
                return;
            }

            if (
                !ExplosiveProjectileModes.TryGetValue(
                    bulletID,
                    out AbsorbedItem mode
                )
            )
            {
                return;
            }

            // Consume first: Lawnf.ZombieExplode calls TakeDamage on every
            // zombie in range and must not recursively trigger another blast.
            ExplosiveProjectileModes.Remove(bulletID);

            try
            {
                Board? board = bullet.board != null
                    ? bullet.board
                    : Board.Instance;

                if (board == null)
                    throw new InvalidOperationException("Board.Instance is null");

                Vector3 worldPosition = bullet.transform.position;
                Vector2 explosionPosition = new Vector2(
                    worldPosition.x,
                    worldPosition.y
                );
                int damage = Mathf.Max(bullet.Damage, BaseDamage);
                DamageType damageType = mode == AbsorbedItem.Jackbox
                    ? DamageType.JackboxExplode
                    : DamageType.Explode;
                ParticleType particleType = mode == AbsorbedItem.Jackbox
                    ? ParticleType.BombKirov
                    : ParticleType.MachineExplodeRed;

                // true means the explosion is allied with the plants, like a
                // mind-controlled zombie explosion. dmgToPlant is explicitly
                // zero so the copied enemy projectile cannot hurt the lawn.
                Lawnf.ZombieExplode(
                    explosionPosition,
                    board,
                    true,
                    bullet.theBulletRow,
                    damageType,
                    damage,
                    0
                );
                CreateParticle.SetParticle(
                    (int)particleType,
                    worldPosition,
                    bullet.theBulletRow,
                    true
                );

                bool shouldLog = mode == AbsorbedItem.Jackbox
                    ? !jackboxExplosionLogged
                    : !gigaExplosionLogged;

                if (shouldLog)
                {
                    if (mode == AbsorbedItem.Jackbox)
                        jackboxExplosionLogged = true;
                    else
                        gigaExplosionLogged = true;

                    Plugin.Logger.LogInfo(
                        "[MagnetOPea] Native area explosion triggered" +
                        " | Mode = " + mode +
                        " | Damage = " + damage +
                        " | DamageType = " + damageType +
                        " | Particle = " + particleType
                    );
                }
            }
            catch (Exception exception)
            {
                if (!explosionWarningLogged)
                {
                    explosionWarningLogged = true;
                    Plugin.Logger.LogWarning(
                        "[MagnetOPea] Native area explosion failed: " +
                        exception
                    );
                }
            }
        }

        private static bool IsMagnetOPea(Plant? plant)
        {
            return
                plant != null &&
                (int)plant.thePlantType == MagnetOPeaID;
        }

        private static Transform? FindShotOrigin(Transform root)
        {
            if (root == null)
                return null;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);

                if (child == null)
                    continue;

                string normalizedName = child.name
                    .Replace("_", string.Empty)
                    .Replace(" ", string.Empty)
                    .ToLowerInvariant();

                if (
                    normalizedName.Contains("shoot") ||
                    normalizedName.Contains("muzzle") ||
                    normalizedName.Contains("firepoint")
                )
                {
                    return child;
                }
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform? result = FindShotOrigin(root.GetChild(i));

                if (result != null)
                    return result;
            }

            return null;
        }

        private static string EnsureShotOrigin(PeaShooter source)
        {
            if (source.shoot != null)
                return source.shoot.name;

            Transform? recovered = FindShotOrigin(source.transform);

            if (recovered != null)
            {
                source.shoot = recovered;
                return recovered.name + " (recovered)";
            }

            // CustomizeLib adds PeaShooter after loading the custom prefab, so
            // Unity cannot deserialize PeaShooter.shoot. The plant root is a
            // safe source for a horizontal pea when the asset has no named
            // muzzle transform.
            source.shoot = source.transform;

            if (!fallbackShotOriginWarningLogged)
            {
                fallbackShotOriginWarningLogged = true;
                Plugin.Logger.LogWarning(
                    "[MagnetOPea] PeaShooter.shoot was null and no named " +
                    "shoot point exists. The plant root will be used."
                );
            }

            return "plant root (fallback)";
        }

        private static Bullet? CreateDirectShot(PeaShooter source)
        {
            MagnetOPea? behaviour = GetBehaviour(source);

            if (behaviour == null && !missingBehaviourWarningLogged)
            {
                missingBehaviourWarningLogged = true;
                Plugin.Logger.LogWarning(
                    "[MagnetOPea] A plant with ID " + MagnetOPeaID +
                    " fired without its MagnetOPea behaviour. " +
                    "A normal pea will be used."
                );
            }

            BulletType bulletType = behaviour != null
                ? GetCurrentBulletType(behaviour)
                : BulletType.Bullet_pea;

            Transform origin = source.shoot != null
                ? source.shoot
                : source.transform;

            CreateBullet? creator = CreateBullet.Instance;

            if (creator == null && Board.Instance != null)
                creator = Board.Instance.GetComponent<CreateBullet>();

            if (creator == null)
            {
                if (!missingBulletCreatorErrorLogged)
                {
                    missingBulletCreatorErrorLogged = true;
                    Plugin.Logger.LogError(
                        "[MagnetOPea] Direct shot failed: CreateBullet is null."
                    );
                }

                return null;
            }

            Vector3 position = origin.position;
            Bullet bullet = creator.SetBullet(
                position.x + 0.1f,
                position.y,
                source.thePlantRow,
                bulletType,
                BulletMoveWay.MoveRight,
                false
            );

            if (bullet == null)
            {
                Plugin.Logger.LogError(
                    "[MagnetOPea] Direct shot failed: SetBullet returned null."
                );
                return null;
            }

            bullet.Damage = source.attackDamage > 0
                ? source.attackDamage
                : BaseDamage;
            bullet.shootingLevel = source.shootingLevel;

            FinalizeShot(source, bullet);

            if (!directShotLogEmitted)
            {
                directShotLogEmitted = true;
                Plugin.Logger.LogInfo(
                    "[MagnetOPea] Direct shot created | Bullet = " +
                    bullet.theBulletType +
                    " | Move = " + BulletMoveWay.MoveRight +
                    " | Row = " + source.thePlantRow +
                    " | Origin = (" + position.x + ", " + position.y + ")"
                );
            }

            return bullet;
        }

        private static void FinalizeShot(PeaShooter source, Bullet? bullet)
        {
            if (bullet == null)
                return;

            // The native PeaShooter method normally fills these fields. The
            // direct path must do it before the projectile can deal damage.
            bullet.from = source;
            bullet.fromType = (PlantType)MagnetOPeaID;

            MagnetOPea? behaviour = GetBehaviour(source);

            if (behaviour == null)
            {
                if (!missingBehaviourWarningLogged)
                {
                    missingBehaviourWarningLogged = true;
                    Plugin.Logger.LogWarning(
                        "[MagnetOPea] A plant with ID " + MagnetOPeaID +
                        " fired without its " +
                        "MagnetOPea behaviour. The vanilla shot was kept."
                    );
                }
                return;
            }

            switch (behaviour.absorbedItem)
            {
                case AbsorbedItem.Normal:
                    ApplyNormalPeaVisual(bullet);
                    break;

                case AbsorbedItem.Jackbox:
                    RegisterExplosiveProjectile(
                        bullet,
                        AbsorbedItem.Jackbox
                    );
                    ApplyZomppelinProjectileVisual(bullet);
                    break;

                case AbsorbedItem.GigaMecha:
                    RegisterExplosiveProjectile(
                        bullet,
                        AbsorbedItem.GigaMecha
                    );
                    ApplyFlagshipProjectileVisual(bullet);
                    break;
            }

            if (behaviour.lastVerifiedShot != behaviour.absorbedItem)
            {
                behaviour.lastVerifiedShot = behaviour.absorbedItem;
                Plugin.Logger.LogInfo(
                    "[MagnetOPea] Shot verified | Mode = " +
                    behaviour.absorbedItem +
                    " | Bullet = " + bullet.theBulletType +
                    " | Damage = " + bullet.Damage
                );
            }
        }

        private static bool HasMetalEquipment(Zombie zombie)
        {
            if (zombie == null)
                return false;

            if (zombie.theFirstArmorHealth > 0)
            {
                switch (zombie.theFirstArmorType)
                {
                    case Zombie.FirstArmorType.Bucket:
                    case Zombie.FirstArmorType.FootballHelmet:
                    case Zombie.FirstArmorType.TallNutFootball:
                    case Zombie.FirstArmorType.BucketNut:
                    case Zombie.FirstArmorType.IronBalloon:
                        return true;
                }
            }

            if (zombie.theSecondArmorHealth > 0)
            {
                switch (zombie.theSecondArmorType)
                {
                    case Zombie.SecondArmorType.Door:
                    case Zombie.SecondArmorType.Ladder:
                    case Zombie.SecondArmorType.Protal:
                    case Zombie.SecondArmorType.RedLadder:
                        return true;
                }
            }

            // Jackboxes, pogo sticks, pickaxes and iron heads are all metal
            // equipment in the game's UniqueItemType category.
            return zombie.theUniqueItemType != Zombie.UniqueItemType.Nothing;
        }

        [HarmonyPatch(typeof(Plant), nameof(Plant.UseItem))]
        private static class Plant_UseItem_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static bool Prefix(
                Plant __instance,
                BucketType type,
                Bucket bucket,
                ref bool __result
            )
            {
                if (!IsMagnetOPea(__instance))
                    return true;

                // This is the only absorption entry point. There is no scan for
                // nearby zombies, dropped equipment, or loose Bucket objects.
                __result = TryAbsorbItem(__instance, type, bucket);
                return false;
            }
        }

        [HarmonyPatch(typeof(PeaShooter), nameof(PeaShooter.Shoot1))]
        private static class PeaShooter_Shoot1_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static bool Prefix(
                PeaShooter __instance,
                ref Bullet __result
            )
            {
                if (!IsMagnetOPea(__instance))
                    return true;

                try
                {
                    __result = CreateDirectShot(__instance)!;
                }
                catch (Exception exception)
                {
                    __result = null!;
                    Plugin.Logger.LogError(
                        "[MagnetOPea] Direct Shoot1 override failed: " +
                        exception
                    );
                }

                // The custom prefab's native PeaShooter.Shoot1 dereferences a
                // serialized field that CustomizeLib cannot populate when it
                // adds the component at runtime. Skip it only for Magnet-o-pea.
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
                TryResolveNativeProjectileVisuals();
            }
        }

        [HarmonyPatch(typeof(KirovBomb), nameof(KirovBomb.Update))]
        private static class KirovBomb_VisualDiscovery_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(KirovBomb __instance)
            {
                if (
                    zomppelinProjectileVisualPrefab != null ||
                    __instance == null ||
                    __instance.gameObject == null
                )
                {
                    return;
                }

                // Clone the live bomb into a persistent inactive template.
                // Keeping the live object itself was unsafe because its own
                // KirovBomb.Update destroys it after the native explosion.
                CaptureZomppelinBombTemplate(
                    __instance.gameObject,
                    "live KirovBomb.Update discovery"
                );
            }
        }

        [HarmonyPatch(typeof(CreateBullet), nameof(CreateBullet.SetBullet))]
        private static class CreateBullet_SetBullet_VisualReset_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.First)]
            private static void Postfix(Bullet __result)
            {
                // SetBullet can return an object from the pool. Reset any
                // Plants+ renderer before the new owner receives it.
                RestoreProjectileVisual(__result);
            }
        }

        [HarmonyPatch(typeof(Bullet), nameof(Bullet.Die))]
        private static class Bullet_Die_VisualReset_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(Bullet __instance)
            {
                RestoreProjectileVisual(__instance);
            }
        }

        [HarmonyPatch(typeof(Zombie), nameof(Zombie.TakeDamage))]
        private static class Zombie_TakeDamage_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(
                Zombie __instance,
                ref int theDamage,
                IDamageMaker damageFrom
            )
            {
                if (
                    __instance == null ||
                    damageFrom == null ||
                    theDamage <= 0 ||
                    !HasMetalEquipment(__instance)
                )
                {
                    return;
                }

                Bullet bullet;

                if (!damageFrom.IsBullet(out bullet) || bullet == null)
                    return;

                bool fromMagnetOPea =
                    (int)bullet.fromType == MagnetOPeaID ||
                    IsMagnetOPea(bullet.from);

                if (!fromMagnetOPea)
                    return;

                // The official iron/helmet/portal peas may already supply their
                // own metal multiplier. Double only an unchanged base hit so a
                // built-in 2x result never becomes 4x.
                if (bullet.Damage > 0 && theDamage == bullet.Damage)
                {
                    theDamage *= 2;

                    if (!metalDamageLogEmitted)
                    {
                        metalDamageLogEmitted = true;
                        Plugin.Logger.LogInfo(
                            "[MagnetOPea] Metal bonus verified | Damage = " +
                            theDamage
                        );
                    }
                }
            }

            [HarmonyPostfix]
            private static void Postfix(IDamageMaker damageFrom)
            {
                if (damageFrom == null)
                    return;

                Bullet bullet;

                if (!damageFrom.IsBullet(out bullet) || bullet == null)
                    return;

                TriggerNativeExplosion(bullet);
            }
        }
    }
}
