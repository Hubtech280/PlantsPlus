using CustomizeLib.MelonLoader;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace PlantsPlus.Core
{
    // Attached only by the four Night Roof custom levels. No global plant or
    // zombie stat changes escape into Adventure, Sandbox or other modes.
    public sealed class NightRoofMoon : MonoBehaviour
    {
        private const float MoonArrival = 600f;
        private const float MoonLifetime = 420f;
        private const float DayPlantMultiplier = 0.75f;
        private const float BloodMoonMultiplier = 1.5f;

        private static readonly string[] TrackNames =
        {
            "night_roof_theme.mp3",
            "night_roof_horde.mp3",
            "moon_broom.mp3"
        };
        private static readonly AudioClip?[] TrackClips =
            new AudioClip?[3];

        private readonly HashSet<int> penalizedPlants = new();
        private readonly HashSet<int> enragedZombies = new();
        private Board? board;
        private AudioSource? musicSource;
        private AudioSource? moonSource;
        private SpriteRenderer? moonVisual;
        private UnityWebRequest? audioRequest;
        private int loadingTrack = -1;
        private float elapsed;
        private float nextStatScan;
        private bool moonArrived;
        private bool bloodMoon;
        private bool moonVisible;
        private bool nativeMusicPaused;
        private bool musicSuspended;
        private bool moonSuspended;

        public NightRoofMoon(IntPtr pointer) : base(pointer) { }

        internal static void Attach(Board target)
        {
            if (target == null ||
                target.GetComponent<NightRoofMoon>() != null)
                return;

            NightRoofMoon moon =
                target.gameObject.AddComponent<NightRoofMoon>();
            moon.board = target;
            EnsureRoofPlanes(target, target.rowNum - 1);
            Plugin.Logger.LogInfo(
                "[Night Roof Moon] Runtime attached to custom level."
            );
        }

        public void Start()
        {
            board ??= GetComponent<Board>();
            if (board != null)
            {
                EnsureRoofPlanes(board, board.rowNum - 1);
                EnsureIceRoads(board);
            }
            musicSource = gameObject.AddComponent<AudioSource>();
            moonSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            moonSource.playOnAwake = false;
            LoadNextTrack();
        }

        internal static bool IsOurLevel(Board? target)
        {
            return target != null &&
                target.sceneType == SceneType.NightRoof &&
                target.GetComponent<NightRoofMoon>() != null;
        }

        internal static void EnsureRoofPlanes(Board target, int highestRow)
        {
            if (target == null || highestRow < 0 ||
                target.sceneType != SceneType.NightRoof)
                return;

            Board.BoardTag tag = target.boardTag;
            if (!tag.isRoof)
            {
                tag.isRoof = true;
                target.boardTag = tag;
            }
            target.planeRoof ??=
                new Il2CppSystem.Collections.Generic.List<Transform>();

            int before = target.planeRoof.Count;
            int end = Math.Min(highestRow, 8);
            for (int row = 0; row <= end; row++)
            {
                Transform fallback = target.transform;
                if (target.plane != null && row < target.plane.Count &&
                    target.plane[row] != null)
                {
                    Transform plane = target.plane[row];
                    fallback = plane.childCount > 0
                        ? plane.GetChild(0) : plane;
                }
                else if (target.background != null)
                {
                    fallback = target.background.transform;
                }

                if (row < target.planeRoof.Count)
                {
                    if (target.planeRoof[row] == null)
                        target.planeRoof[row] = fallback;
                }
                else
                {
                    target.planeRoof.Add(fallback);
                }
            }
            if (target.planeRoof.Count != before)
            {
                Plugin.Logger.LogWarning(
                    "[Night Roof] Repaired roof planes | Before = " +
                    before + " | After = " + target.planeRoof.Count +
                    " | Rows = " + target.rowNum
                );
            }
        }

        public void Update()
        {
            PollAudioRequest();

            bool running = board != null && Board.Instance == board &&
                GameAPP.theGameStatus == GameStatus.InGame && Time.timeScale > 0f;
            SyncAudioPause(musicSource, !running, ref musicSuspended);
            SyncAudioPause(moonSource, !running, ref moonSuspended);
            if (!running || board == null)
                return;

            elapsed += Time.deltaTime;
            if (elapsed >= nextStatScan)
            {
                nextStatScan = elapsed + 0.25f;
                EnsureRoofPlanes(board, board.rowNum - 1);
                ApplyDayPlantPenalty();
                if (bloodMoon)
                    EnrageZombies();
            }

            if (!moonArrived && elapsed >= MoonArrival)
            {
                moonArrived = true;
                HealAllPlants();
                ShowMoon();
                if (moonSource != null && TrackClips[2] != null)
                {
                    moonSource.clip = TrackClips[2];
                    moonSource.volume = GetMusicVolume();
                    moonSource.Play();
                }
                Plugin.Logger.LogInfo(
                    "[Night Roof Moon] Moon appeared at 10 minutes; " +
                    "healed plants and will stay for 7 minutes."
                );
            }

            if (moonArrived && elapsed >= MoonArrival + MoonLifetime &&
                !bloodMoon && moonVisible)
            {
                HideMoon();
                Plugin.Logger.LogInfo(
                    "[Night Roof Moon] Seven-minute Moon period ended."
                );
            }

            if (!bloodMoon && board.theMaxWave > 0 &&
                board.theWave >= board.theMaxWave)
            {
                bloodMoon = true;
                ShowMoon();
                TintMoon(Color.red);
                Plugin.Logger.LogInfo(
                    "[Night Roof Moon] Blood Moon began at the final flag."
                );
            }

            UpdateMusic();
        }

        private static void SyncAudioPause(AudioSource? source, bool suspend,
            ref bool suspended)
        {
            if (source == null) return;
            if (suspend && source.isPlaying)
            {
                source.Pause();
                suspended = true;
            }
            else if (!suspend && suspended)
            {
                source.UnPause();
                suspended = false;
            }
        }

        // CheckBox's native IceRoadRule indexes this list by row even when
        // there is no visible ice. Rebuild missing slots with native, inactive
        // roads, not an override of the placement rule. Keep existing ice.
        internal static void EnsureIceRoads(Board target)
        {
            if (!IsOurLevel(target) || target.rowNum <= 0) return;
            target.iceRoads ??= new Il2CppSystem.Collections.Generic.List<IceRoad>();
            int repaired = 0;
            for (int row = 0; row < target.rowNum; row++)
            {
                if (row < target.iceRoads.Count && target.iceRoads[row] != null)
                    continue;
                Transform parent = target.plane != null && row < target.plane.Count
                    && target.plane[row] != null ? target.plane[row] : target.transform;
                var obj = UnityEngine.Object.Instantiate((GameObject)GamePrefabs.IceRoad,
                    new Vector3(19.7f, 0.8f, 0f), Quaternion.identity, parent);
                IceRoad road = obj.GetComponent<IceRoad>();
                road.theRow = row;
                road.x = road.roadStartX = 19.7f;
                road.active = false;
                road.transform.localPosition = new Vector3(19.7f, 0.8f, 0f);
                if (row < target.iceRoads.Count) target.iceRoads[row] = road;
                else target.iceRoads.Add(road);
                repaired++;
            }
            if (repaired > 0)
                Plugin.Logger.LogInfo("[Night Roof] Restored native ice-road slots: " +
                    repaired + " | Rows = " + target.rowNum);
        }

        private void ApplyDayPlantPenalty()
        {
            var plants = Lawnf.GetAllPlants();
            if (plants == null)
                return;

            for (int index = 0; index < plants.Count; index++)
            {
                Plant? plant = plants[index];
                if (plant == null || plant.dying)
                    continue;

                int id = plant.GetInstanceID();
                if (penalizedPlants.Contains(id) ||
                    !IsDayPlant(plant.thePlantType))
                    continue;
                if (plant.attackDamage <= 0 &&
                    plant.thePlantAttackInterval <= 0f)
                    continue;
                penalizedPlants.Add(id);

                if (plant.attackDamage > 0)
                {
                    plant.attackDamage = Math.Max(1,
                        Mathf.RoundToInt(
                            plant.attackDamage * DayPlantMultiplier
                        )
                    );
                }
                if (plant.thePlantAttackInterval > 0f)
                {
                    plant.thePlantAttackInterval /=
                        DayPlantMultiplier;
                }
            }
        }

        private static bool IsDayPlant(PlantType type)
        {
            if (type == (PlantType)Plants.BorealOrchid.BorealOrchidID ||
                type == (PlantType)Plants.LobShroom.ID ||
                type == PlantType.ElectricOnion ||
                type == (PlantType)Plants.BounceShroom.ID)
                return false;

            return !BorealSystem.IsNightPlant(type);
        }

        private static void HealAllPlants()
        {
            var plants = Lawnf.GetAllPlants();
            if (plants == null)
                return;

            int healed = 0;
            for (int index = 0; index < plants.Count; index++)
            {
                Plant? plant = plants[index];
                if (plant == null || plant.dying)
                    continue;

                int missing =
                    plant.thePlantMaxHealth - plant.thePlantHealth;
                if (missing <= 0)
                    continue;

                plant.Recover(
                    missing,
                    DamageType.Normal,
                    true,
                    false
                );
                healed++;
            }
            Plugin.Logger.LogInfo(
                "[Night Roof Moon] Fully healed " + healed + " plants."
            );
        }

        private void EnrageZombies()
        {
            var zombies = Lawnf.GetAllZombies();
            if (zombies == null)
                return;

            for (int index = 0; index < zombies.Count; index++)
            {
                Zombie? zombie = zombies[index];
                if (zombie == null || zombie.isMindControlled ||
                    (zombie.theOriginSpeed <= 0f &&
                     zombie.theAttackDamage <= 0))
                    continue;

                if (!enragedZombies.Add(zombie.GetInstanceID()))
                    continue;

                if (zombie.theOriginSpeed > 0f)
                {
                    zombie.theOriginSpeed *= BloodMoonMultiplier;
                    zombie.theSpeed *= BloodMoonMultiplier;
                }
                if (zombie.theAttackDamage > 0)
                {
                    zombie.theAttackDamage = Math.Max(1,
                        Mathf.RoundToInt(
                            zombie.theAttackDamage * BloodMoonMultiplier
                        )
                    );
                }
            }
        }

        private void ShowMoon()
        {
            moonVisible = true;
            if (moonVisual == null)
                CreateMoonVisual();
            if (moonVisual != null)
            {
                moonVisual.gameObject.SetActive(true);
                TintMoon(bloodMoon ? Color.red : Color.white);
            }
        }

        private void HideMoon()
        {
            moonVisible = false;
            if (moonVisual != null)
                moonVisual.gameObject.SetActive(false);
        }

        private void TintMoon(Color color)
        {
            if (moonVisual != null)
                moonVisual.color = color;
        }

        private void CreateMoonVisual()
        {
            Sprite? sprite = FindNativeMoonSprite();
            Camera? camera = Camera.main;
            if (sprite == null || camera == null)
            {
                Plugin.Logger.LogWarning(
                    "[Night Roof Moon] Native Moon sprite or camera " +
                    "unavailable; gameplay effects remain active."
                );
                return;
            }

            GameObject icon = new GameObject("PlantsPlus_NightRoof_Moon");
            icon.transform.SetParent(transform, false);
            Vector3 point = camera.ViewportToWorldPoint(new Vector3(
                0.88f,
                0.84f,
                -camera.transform.position.z
            ));
            point.z = 0f;
            icon.transform.position = point;
            moonVisual = icon.AddComponent<SpriteRenderer>();
            moonVisual.sprite = sprite;
            moonVisual.sortingOrder = 2500;
            Plugin.Logger.LogInfo(
                "[Night Roof Moon] Native sprite = " + sprite.name
            );
            float largest = Mathf.Max(
                sprite.bounds.size.x,
                sprite.bounds.size.y
            );
            if (largest > 0f)
                icon.transform.localScale = Vector3.one * (1.5f / largest);
        }

        private static Sprite? FindNativeMoonSprite()
        {
            Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
            for (int index = 0; index < sprites.Length; index++)
            {
                Sprite sprite = sprites[index];
                if (sprite != null && sprite.name.Equals(
                    "Moon",
                    StringComparison.OrdinalIgnoreCase
                ))
                    return sprite;
            }

            for (int index = 0; index < sprites.Length; index++)
            {
                Sprite sprite = sprites[index];
                if (sprite == null)
                    continue;
                string name = sprite.name;
                if (name.IndexOf("moon", StringComparison.OrdinalIgnoreCase)
                        >= 0 ||
                    name.IndexOf("luna", StringComparison.OrdinalIgnoreCase)
                        >= 0 ||
                    name.Contains("月亮"))
                    return sprite;
            }
            return null;
        }

        private void UpdateMusic()
        {
            AudioClip? selected = bloodMoon && TrackClips[1] != null
                ? TrackClips[1]
                : TrackClips[0];
            if (selected == null || musicSource == null)
                return;

            if (GameAPP.music != null && GameAPP.music.isPlaying)
            {
                GameAPP.music.Pause();
                nativeMusicPaused = true;
            }
            if (GameAPP.musicDrum != null &&
                GameAPP.musicDrum.isPlaying)
            {
                GameAPP.musicDrum.Pause();
                nativeMusicPaused = true;
            }

            if (musicSource.clip != selected)
            {
                musicSource.Stop();
                musicSource.clip = selected;
            }
            musicSource.volume = GetMusicVolume();
            if (!musicSource.isPlaying)
                musicSource.Play();
        }

        private static float GetMusicVolume()
        {
            return GameAPP.music != null
                ? GameAPP.music.volume
                : 0.8f;
        }

        private void LoadNextTrack()
        {
            try
            {
                loadingTrack++;
                if (loadingTrack >= TrackNames.Length)
                    return;
                if (TrackClips[loadingTrack] != null)
                {
                    LoadNextTrack();
                    return;
                }

                string file = EnsureAudioFile(TrackNames[loadingTrack]);
                audioRequest = UnityWebRequestMultimedia.GetAudioClip(
                    new Uri(file).AbsoluteUri,
                    AudioType.MPEG
                );
                audioRequest.SendWebRequest();
            }
            catch (Exception error)
            {
                Plugin.Logger.LogWarning(
                    "[Night Roof Audio] " +
                    TrackNames[loadingTrack] + " failed to load: " +
                    error.Message
                );
                audioRequest?.Dispose();
                audioRequest = null;
                if (loadingTrack < TrackNames.Length - 1)
                    LoadNextTrack();
            }
        }

        private static string EnsureAudioFile(string name)
        {
            string directory = Path.Combine(
                Path.GetDirectoryName(typeof(NightRoofMoon).Assembly.Location)!,
                "PlantsPlusAudio"
            );
            Directory.CreateDirectory(directory);
            string file = Path.Combine(directory, name);
            string resource = "PlantsPlus.Resources.Audio." + name;
            using Stream stream =
                typeof(NightRoofMoon).Assembly
                    .GetManifestResourceStream(resource) ??
                throw new FileNotFoundException(resource);

            if (!File.Exists(file) ||
                new FileInfo(file).Length != stream.Length)
            {
                using FileStream target = File.Create(file);
                stream.CopyTo(target);
            }
            return file;
        }

        private void PollAudioRequest()
        {
            if (audioRequest == null || !audioRequest.isDone)
                return;

            try
            {
                if (!string.IsNullOrEmpty(audioRequest.error))
                    throw new IOException(audioRequest.error);

                AudioClip clip =
                    DownloadHandlerAudioClip.GetContent(audioRequest);
                if (clip == null)
                    throw new IOException("Unity returned no audio clip.");
                clip.name = "PlantsPlus_" + TrackNames[loadingTrack];
                TrackClips[loadingTrack] = clip;
                Plugin.Logger.LogInfo(
                    "[Night Roof Audio] Loaded " + TrackNames[loadingTrack]
                );
            }
            catch (Exception error)
            {
                Plugin.Logger.LogWarning(
                    "[Night Roof Audio] " +
                    TrackNames[loadingTrack] + " failed safely: " +
                    error.Message
                );
            }
            finally
            {
                audioRequest.Dispose();
                audioRequest = null;
                LoadNextTrack();
            }
        }

        public void OnDestroy()
        {
            audioRequest?.Dispose();
            audioRequest = null;
            musicSource?.Stop();
            moonSource?.Stop();
            if (nativeMusicPaused && GameAPP.theGameStatus == GameStatus.InGame)
            {
                GameAPP.music?.UnPause();
                GameAPP.musicDrum?.UnPause();
            }
        }
    }
}
