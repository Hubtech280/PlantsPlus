using HarmonyLib;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace PlantsPlus.Core
{
    internal static class SuperLevelEditorPlus
    {
        private const string TerrainLayerName =
            "PlantsPlus_MapTerrain";
        private const string PlantLayerName =
            "PlantsPlus_MapPlant";

        private sealed class SceneVisual
        {
            internal SceneVisual(
                string background,
                PlantType plant,
                string? terrain = null,
                int preferredWidth = 1400)
            {
                Background = background;
                Plant = plant;
                Terrain = terrain;
                PreferredWidth = preferredWidth;
            }

            internal string Background { get; }
            internal string? Terrain { get; }
            internal PlantType Plant { get; }
            internal int PreferredWidth { get; }
        }

        private static readonly Dictionary<SceneType, SceneVisual>
            Visuals = new Dictionary<SceneType, SceneVisual>
        {
            [SceneType.Day] = new SceneVisual(
                "Day", PlantType.PeaSunFlower, null, 310),
            [SceneType.Day_6] = new SceneVisual(
                "Day", PlantType.ThreePeater, null, 310),
            [SceneType.Night] = new SceneVisual(
                "Night", PlantType.SmallPuff, null, 310),
            [SceneType.Night_6] = new SceneVisual(
                "Night", PlantType.DoomThreePeater, null, 310),
            [SceneType.Pool] = new SceneVisual(
                "Pool", PlantType.LilyPad, null, 310),
            [SceneType.NightPool] = new SceneVisual(
                "Fog", PlantType.Plantern, null, 310),
            [SceneType.Roof] = new SceneVisual(
                "Roof", PlantType.Cabbagepult, null, 310),
            [SceneType.Travel_roof] = new SceneVisual(
                "Roof", PlantType.Melonpult, null, 310),
            [SceneType.Travel_roof_dusk] = new SceneVisual(
                "DuskRoof", PlantType.StarFruit, null, 310),
            [SceneType.Travel_roof_night] = new SceneVisual(
                "NightRoof", PlantType.ElectricOnion, null, 310),
            [SceneType.BigPool] = new SceneVisual(
                "Lake", PlantType.BigSeaShroom, null, 310),
            [SceneType.ReversalPool] = new SceneVisual(
                "InvertedPool", PlantType.Tanglekelp, null, 310),
            [SceneType.NormalBeach] = new SceneVisual(
                "VolcanicBeach", PlantType.NutPot, null, 310),
            [SceneType.Snow] = new SceneVisual(
                "Snow", PlantType.SpruceShooter, null, 310),
            [SceneType.NightSnow] = new SceneVisual(
                "SnowyNight", PlantType.Thorns, null, 310),
            [SceneType.Roof_Pool] = new SceneVisual(
                "MesaRiver", PlantType.ThreeMelon, null, 310),
            [SceneType.SnowPool] = new SceneVisual(
                "FrozenRiver", PlantType.IceCattail, null, 310),
            [SceneType.SnowPool_night] = new SceneVisual(
                "FrozenRiverNight", PlantType.IceDoom, null, 310),
            [SceneType.TreasureBeach] = new SceneVisual(
                "TreasureBeach", PlantType.TreasureMine, null, 310),
            [SceneType.RoofPool_night] = new SceneVisual(
                "MesaRiverNight",
                (PlantType)Plants.Icytronion.IcytronionID,
                null,
                310),
            [SceneType.LavaBeach] = new SceneVisual(
                "VolcanicBeach",
                PlantType.NutPot,
                null,
                310),
            [SceneType.LavaPool] = new SceneVisual(
                "Lava",
                PlantType.ObsidianJalapeno,
                null,
                310),
            [SceneType.River] = new SceneVisual(
                "Pond", PlantType.SeaShroom, null, 310),
            [SceneType.SuperDay] = new SceneVisual(
                "SuperDay", PlantType.SuperThreePeater, null, 310),
            [SceneType.SuperPool] = new SceneVisual(
                "SuperPool", PlantType.UltimateCattail, null, 310),
            [SceneType.MidDay] = new SceneVisual(
                "Dawn", PlantType.SolarSunflower, null, 310),
            [SceneType.NightWinter] = new SceneVisual(
                "ChristmasSnow",
                PlantType.ThornsSpruce,
                null,
                310)
        };

        private static readonly SceneVisual
            VolcanicBeachOverride = new SceneVisual(
                "VolcanicBeach",
                PlantType.NutPot,
                null,
                310
            );

        private static readonly SceneVisual
            LavaOverride = new SceneVisual(
                "Lava",
                PlantType.ObsidianJalapeno,
                null,
                310
            );

        private static readonly Dictionary<string, Sprite>
            BackgroundSprites = new Dictionary<string, Sprite>();

        private static bool registered;
        private static bool loggedEditor;
        private static Texture2D[]? allTextures;

        internal static void OnStart()
        {
            if (registered)
                return;

            registered = true;
            Plugin.Logger.LogInfo(
                "[Super Level Editor+] Christmas Snow and themed " +
                "scene previews enabled."
            );
        }

        internal static void EnsureChristmasSnow()
        {
            try
            {
                if (MapData_cs.SceneName == null ||
                    MapData_cs.SceneName.ContainsKey(
                        SceneType.NightWinter))
                {
                    return;
                }

                MapData_cs.SceneName.Add(
                    SceneType.NightWinter,
                    "Christmas Snow"
                );
                Plugin.Logger.LogInfo(
                    "[Super Level Editor+] Christmas Snow added to " +
                    "the scene selector."
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Super Level Editor+] Could not add Christmas " +
                    "Snow safely: " + exception
                );
            }
        }

        internal static void DecorateSceneButtons(CustomMenu menu)
        {
            try
            {
                if (menu == null || menu.ScenesContent == null)
                    return;

                CustomButton_scene[] buttons =
                    menu.ScenesContent
                        .GetComponentsInChildren<CustomButton_scene>(
                            true
                        );

                foreach (CustomButton_scene button in buttons)
                    DecorateButton(button);

                if (!loggedEditor)
                {
                    loggedEditor = true;
                    Plugin.Logger.LogInfo(
                        "[Super Level Editor+] Decorated " +
                        buttons.Length +
                        " scene buttons with map + plant previews."
                    );
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Super Level Editor+] Scene preview setup failed " +
                    "safely: " + exception
                );
            }
        }

        internal static void EnsureChristmasSnowButton(CustomMenu menu)
        {
            try
            {
                if (menu == null || menu.ScenesContent == null)
                    return;

                CustomButton_scene[] buttons =
                    menu.ScenesContent
                        .GetComponentsInChildren<CustomButton_scene>(
                            true
                        );

                foreach (CustomButton_scene button in buttons)
                {
                    if (button != null &&
                        button.sceneType == SceneType.NightWinter)
                    {
                        return;
                    }
                }

                CustomButton_scene? template =
                    buttons.Length > 0
                        ? buttons[buttons.Length - 1]
                        : menu.FirstSceneButton;
                if (template == null)
                    return;

                GameObject clone = UnityEngine.Object.Instantiate(
                    template.gameObject,
                    menu.ScenesContent
                );
                clone.name = "PlantsPlus_ChristmasSnowScene";
                clone.transform.SetAsLastSibling();
                clone.SetActive(true);

                CustomButton_scene christmasButton =
                    clone.GetComponent<CustomButton_scene>();
                if (christmasButton == null)
                {
                    UnityEngine.Object.Destroy(clone);
                    return;
                }

                christmasButton.sceneType = SceneType.NightWinter;
                if (christmasButton.sceneText != null)
                {
                    christmasButton.sceneText.text =
                        "Christmas Snow";
                }

                Plugin.Logger.LogInfo(
                    "[Super Level Editor+] Christmas Snow button " +
                    "restored at the end of the scene selector."
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Super Level Editor+] Could not restore the " +
                    "Christmas Snow button safely: " + exception
                );
            }
        }

        internal static void DecorateCurrentScene(CustomMenu menu)
        {
            try
            {
                if (menu == null ||
                    menu.CurrentSceneContainer == null)
                {
                    return;
                }

                CustomButton_scene[] buttons =
                    menu.CurrentSceneContainer
                        .GetComponentsInChildren<CustomButton_scene>(
                            true
                        );
                foreach (CustomButton_scene button in buttons)
                    DecorateButton(button);
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Super Level Editor+] Current scene preview " +
                    "could not be refreshed: " + exception.Message
                );
            }
        }

        private static void DecorateButton(
            CustomButton_scene button)
        {
            if (button == null)
                return;

            SceneVisual? visual = null;
            string label =
                button.sceneText != null
                    ? button.sceneText.text ?? string.Empty
                    : string.Empty;

            // The enum names are misleading: LavaBeach is the
            // Volcanic Beach selector, while the separate Lava scene
            // is LavaPool. Also use the translated visible label as a
            // safety net in case the game's dictionary order changes.
            if (label.IndexOf(
                "Volcanic Beach",
                StringComparison.OrdinalIgnoreCase) >= 0)
            {
                visual = VolcanicBeachOverride;
            }
            else if (string.Equals(
                label.Trim(),
                "Lava",
                StringComparison.OrdinalIgnoreCase))
            {
                visual = LavaOverride;
            }
            else
            {
                Visuals.TryGetValue(
                    button.sceneType,
                    out visual
                );
            }

            if (visual == null)
            {
                return;
            }

            Transform backgroundTransform =
                button.transform.Find("Background");
            Transform iconTransform =
                button.transform.Find("Icon");
            if (backgroundTransform == null ||
                iconTransform == null)
                return;

            Image backgroundImage =
                backgroundTransform.GetComponent<Image>();
            Image icon = iconTransform.GetComponent<Image>();
            if (backgroundImage == null || icon == null)
                return;

            Sprite? background = GetBackgroundSprite(
                visual.Background,
                visual.PreferredWidth
            );
            if (background != null)
            {
                backgroundImage.sprite = background;
                backgroundImage.color = Color.white;
                backgroundImage.preserveAspect = false;

                // The native Background object is only a 100 x 100
                // square. The visible preview opening is wider and a
                // little taller, so expand the actual RectTransform
                // instead of merely stretching a sprite inside the
                // original square.
                RectTransform backgroundRect =
                    backgroundImage.rectTransform;
                backgroundRect.sizeDelta =
                    new Vector2(114f, 90f);
            }

            RemoveLayer(backgroundTransform, TerrainLayerName);
            RemoveLayer(backgroundTransform, PlantLayerName);
            RemoveLayer(iconTransform, TerrainLayerName);
            RemoveLayer(iconTransform, PlantLayerName);

            if (!string.IsNullOrEmpty(visual.Terrain))
            {
                Sprite? terrain = GetBackgroundSprite(
                    visual.Terrain!,
                    1400
                );
                if (terrain != null)
                {
                    Image terrainImage = CreateLayer(
                        backgroundTransform,
                        TerrainLayerName,
                        terrain
                    );
                    terrainImage.preserveAspect = false;
                }
            }

            Sprite? plant = GetPlantSprite(visual.Plant);
            if (plant == null)
                return;

            // "Icon" is the native plant slot. Keeping the plant here
            // preserves the intended size and leaves "Background" free
            // to cover the complete rectangular preview window.
            icon.sprite = plant;
            icon.color = Color.white;
            icon.preserveAspect = true;
        }

        private static Image CreateLayer(
            Transform parent,
            string name,
            Sprite sprite)
        {
            GameObject layer = new GameObject(name);
            layer.transform.SetParent(parent, false);

            RectTransform rect =
                layer.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = layer.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private static void RemoveLayer(
            Transform parent,
            string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                UnityEngine.Object.Destroy(existing.gameObject);
        }

        private static Sprite? GetPlantSprite(PlantType plantType)
        {
            try
            {
                if (GameAPP.resourcesManager == null ||
                    GameAPP.resourcesManager.plantPreviews == null ||
                    !GameAPP.resourcesManager.plantPreviews.ContainsKey(
                        plantType))
                {
                    return null;
                }

                GameObject preview =
                    GameAPP.resourcesManager.plantPreviews[plantType];
                if (preview == null)
                    return null;

                SpriteRenderer renderer =
                    preview.GetComponent<SpriteRenderer>();
                if (renderer == null)
                {
                    renderer =
                        preview.GetComponentInChildren<SpriteRenderer>(
                            true
                        );
                }

                return renderer != null ? renderer.sprite : null;
            }
            catch
            {
                return null;
            }
        }

        private static Sprite? GetBackgroundSprite(
            string textureName,
            int preferredWidth)
        {
            string cacheKey = textureName + "|" + preferredWidth;
            if (BackgroundSprites.TryGetValue(
                cacheKey,
                out Sprite? cached))
            {
                return cached;
            }

            Sprite? embedded =
                GetEmbeddedBackground(textureName);
            if (embedded != null)
            {
                BackgroundSprites[cacheKey] = embedded;
                return embedded;
            }

            allTextures ??=
                Resources.FindObjectsOfTypeAll<Texture2D>();

            Texture2D? best = null;
            int bestScore = int.MaxValue;
            foreach (Texture2D texture in allTextures)
            {
                if (texture == null ||
                    !string.Equals(
                        texture.name,
                        textureName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int score =
                    Math.Abs(texture.width - preferredWidth) +
                    Math.Abs(texture.height - 600);
                if (score >= bestScore)
                    continue;

                best = texture;
                bestScore = score;
            }

            if (best == null)
                return null;

            // Scene textures are complete boards. A simple aspect-ratio
            // crop still shows almost the whole map, which makes a tiny
            // editor button look like a compressed level screenshot.
            // Zoom into a local patch of only a few lawn tiles instead.
            const float previewAspect = 1.55f;
            float cropHeight =
                best.width <= 512 && best.height <= 512
                    ? Math.Min(
                        best.height,
                        best.width / previewAspect
                    )
                    : Math.Min(
                        best.height,
                        best.height * 0.38f
                    );
            float cropWidth = Math.Min(
                best.width,
                cropHeight * previewAspect
            );
            float cropX = (best.width - cropWidth) * 0.5f;
            float cropY = (best.height - cropHeight) * 0.5f;

            Sprite sprite = Sprite.Create(
                best,
                new Rect(cropX, cropY, cropWidth, cropHeight),
                new Vector2(0.5f, 0.5f),
                100f
            );
            sprite.name =
                "PlantsPlus_Editor_" + textureName;
            BackgroundSprites[cacheKey] = sprite;
            return sprite;
        }

        private static Sprite? GetEmbeddedBackground(
            string textureName)
        {
            try
            {
                string resourceName =
                    "PlantsPlus.Resources.Sprites.EditorMaps." +
                    textureName +
                    ".png";
                Assembly assembly =
                    Assembly.GetExecutingAssembly();
                using Stream? stream =
                    assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                    return null;

                byte[] bytes = new byte[stream.Length];
                int offset = 0;
                while (offset < bytes.Length)
                {
                    int read = stream.Read(
                        bytes,
                        offset,
                        bytes.Length - offset
                    );
                    if (read <= 0)
                        break;
                    offset += read;
                }

                Texture2D texture = new Texture2D(
                    2,
                    2,
                    TextureFormat.RGBA32,
                    false
                );
                if (!ImageConversion.LoadImage(
                    texture,
                    bytes,
                    false))
                {
                    UnityEngine.Object.Destroy(texture);
                    return null;
                }

                texture.name =
                    "PlantsPlus_EditorMap_" + textureName;
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(
                        0f,
                        0f,
                        texture.width,
                        texture.height
                    ),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                sprite.name =
                    "PlantsPlus_Editor_" + textureName;
                return sprite;
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Super Level Editor+] Embedded preview " +
                    textureName +
                    " could not be loaded: " +
                    exception.Message
                );
                return null;
            }
        }
    }

    [HarmonyPatch(typeof(CustomMenu), "InitScene")]
    internal static class SuperLevelEditorInitScenePatch
    {
        private static void Prefix()
        {
            SuperLevelEditorPlus.EnsureChristmasSnow();
        }

        private static void Postfix(CustomMenu __instance)
        {
            SuperLevelEditorPlus.EnsureChristmasSnowButton(__instance);
            SuperLevelEditorPlus.DecorateSceneButtons(__instance);
            SuperLevelEditorPlus.DecorateCurrentScene(__instance);
        }
    }

    [HarmonyPatch(typeof(CustomMenu), nameof(CustomMenu.SetScene))]
    internal static class SuperLevelEditorSetScenePatch
    {
        private static void Postfix(CustomMenu __instance)
        {
            SuperLevelEditorPlus.DecorateCurrentScene(__instance);
        }
    }

    [HarmonyPatch(typeof(Lawnf), nameof(Lawnf.SetMusic))]
    internal static class ChristmasSnowMusicPatch
    {
        private static void Postfix(Board board)
        {
            if (board == null ||
                board.sceneType != SceneType.NightWinter)
            {
                return;
            }

            // The native scene reports MusicType.Day, but Snowy Night
            // better matches the custom Christmas Snow editor entry.
            // Force it here so SelectCard cannot continue playing.
            board.musicType = (int)MusicType.NightSnow;
            if (GameAPP.Instance != null)
                GameAPP.Instance.PlayMusic(MusicType.NightSnow);
        }
    }
}
