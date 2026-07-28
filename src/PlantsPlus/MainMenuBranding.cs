using HarmonyLib;
using Il2Cpp;
using Il2CppTMPro;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PlantsPlus.Core
{
    internal static class MainMenuBranding
    {
        private const string LogoObjectName =
            "PlantsPlus_V11_Logo";
        private const string ChangelogButtonName =
            "PlantsPlusChangelogButton";
        private const string LogoResourceName =
            "PlantsPlus.Resources.Sprites.plants_v1_1_logo.png";

        private const string ChangelogText =
            "<align=center><size=130%>Plants+ - Update v1.1</size></align>\n\n" +
            "1. New Plants\n" +
            "- Not-a-pea\n" +
            "- Not-a-storm Commando\n" +
            "- Frost Furflower\n" +
            "- Doomtronion\n" +
            "- Lichen-pea\n" +
            "- Logic Blover\n" +
            "- Solar Sharpshooter\n" +
            "- Sea Ballista\n" +
            "- Pineshooter\n" +
            "- Icytronion\n\n" +
            "2. Plant Reworks\n" +
            "- Inferno Torchflower now stores sun while lit. Click it to release all stored sun; each flame increases the payout multiplier.\n" +
            "- Electronion now belongs to the regular plant pages, supports Carbon Copy and appears in the correct Almanac and Sandbox positions.\n" +
            "- Doomtronion and Icytronion now keep their special effects while connected to other Electronion-family plants.\n\n" +
            "3. Super Level Editor+\n" +
            "- Added the Christmas Snow map.\n" +
            "- Added a themed map background and a matching plant preview for every scene.\n" +
            "- Christmas Snow now uses the Snowy Night music.\n\n" +
            "4. Quality of Life\n" +
            "- Added the Plants+ v1.1 logo to the main menu.\n" +
            "- Added this dedicated Plants+ changelog.\n" +
            "- Improved custom plant cards, Almanac entries, previews and visual consistency.\n\n" +
            "5. Bug Fixes\n" +
            "- Fixed Solar Firnace cold protection.\n" +
            "- Fixed several projectile positions, pooled projectile skins, attack animations and missing shadows.\n" +
            "- Fixed Electronion card backgrounds and availability in restricted levels.\n" +
            "- Fixed multiple electric-chain effects and visual synchronization issues.\n\n" +
            "<align=center><color=#64DD17>Quality Over Quantity.</color></align>";

        private static Sprite? logoSprite;
        private static bool changelogPending;
        private static RectTransform? animatedLogo;
        private static Image? animatedLogoImage;
        private static float logoAnimationStart;
        private static Vector2 logoTargetPosition;
        private static bool logoAnimationRunning;

        internal static void OnStart()
        {
            Plugin.Logger.LogInfo(
                "[Main Menu] Plants+ v1.1 logo and changelog enabled."
            );
        }

        internal static void DecorateMainMenu(BaseMenu menu)
        {
            try
            {
                EnsureLogo(menu);
                EnsureChangelogButton(menu);
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Main Menu] Decoration failed safely: " +
                    exception
                );
            }
        }

        internal static void ApplyPendingChangelog(BaseMenu menu)
        {
            if (!changelogPending || menu == null ||
                !menu.gameObject.name.Contains("NoticePauseMenu"))
            {
                return;
            }

            if (TryReplaceChangelog(menu))
                changelogPending = false;
        }

        private static void EnsureLogo(BaseMenu menu)
        {
            Transform existing = menu.transform.Find(LogoObjectName);
            if (existing != null)
            {
                BeginLogoAnimation(existing as RectTransform);
                return;
            }

            Sprite? sprite = LoadLogoSprite();
            if (sprite == null)
                return;

            GameObject logo = new GameObject(LogoObjectName);
            RectTransform rect =
                logo.AddComponent<RectTransform>();
            logo.AddComponent<CanvasRenderer>();
            Image image = logo.AddComponent<Image>();
            rect.SetParent(menu.transform, false);

            // This is the open patch of sea between Recommended Levels
            // and the Fusion Showcase machine on the 1920x1080 menu.
            // Anchors keep the placement stable at other resolutions.
            rect.anchorMin = new Vector2(0.28f, 0.45f);
            rect.anchorMax = new Vector2(0.28f, 0.45f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            // MainMenu's own RectTransform is offset from the full-screen
            // canvas. This local correction places the logo in the sea gap
            // shown by the supplied 1920x1080 reference capture.
            rect.anchoredPosition = new Vector2(-205f, -100f);
            rect.sizeDelta = new Vector2(165f, 129f);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;

            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            // Render above the menu background while keeping the existing
            // interactive grave and buttons untouched.
            rect.SetAsLastSibling();
            BeginLogoAnimation(rect);
        }

        private static void BeginLogoAnimation(
            RectTransform? rect
        )
        {
            if (rect == null)
                return;

            Image image = rect.GetComponent<Image>();
            if (image == null)
                return;

            logoTargetPosition = new Vector2(-205f, -100f);
            rect.anchoredPosition =
                logoTargetPosition + new Vector2(0f, -18f);
            rect.localScale = Vector3.one * 0.82f;

            Color color = image.color;
            color.a = 0f;
            image.color = color;

            animatedLogo = rect;
            animatedLogoImage = image;
            logoAnimationStart = Time.unscaledTime;
            logoAnimationRunning = true;
            rect.gameObject.SetActive(true);
        }

        internal static void AnimateLogo(MainMenu menu)
        {
            if (!logoAnimationRunning ||
                menu == null ||
                animatedLogo == null ||
                animatedLogoImage == null)
            {
                return;
            }

            Transform current = animatedLogo;
            bool belongsToMenu = false;

            while (current != null)
            {
                if (current == menu.transform)
                {
                    belongsToMenu = true;
                    break;
                }

                current = current.parent;
            }

            if (!belongsToMenu)
            {
                logoAnimationRunning = false;
                return;
            }

            const float delay = 0.25f;
            const float duration = 0.55f;
            float raw =
                (Time.unscaledTime - logoAnimationStart - delay) /
                duration;

            if (raw <= 0f)
                return;

            float t = Mathf.Clamp01(raw);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            animatedLogo.anchoredPosition = Vector2.Lerp(
                logoTargetPosition + new Vector2(0f, -18f),
                logoTargetPosition,
                eased
            );
            animatedLogo.localScale = Vector3.one * Mathf.Lerp(
                0.82f,
                1f,
                eased
            );

            Color color = animatedLogoImage.color;
            color.a = eased;
            animatedLogoImage.color = color;

            if (t >= 1f)
            {
                animatedLogo.anchoredPosition = logoTargetPosition;
                animatedLogo.localScale = Vector3.one;
                color.a = 1f;
                animatedLogoImage.color = color;
                logoAnimationRunning = false;
            }
        }

        private static void EnsureChangelogButton(BaseMenu menu)
        {
            Transform grave = menu.transform.Find("Grave");
            if (grave == null ||
                grave.Find(ChangelogButtonName) != null)
            {
                return;
            }

            Transform originalTransform =
                grave.Find("UpdateInfoButton");
            if (originalTransform == null)
                return;

            UIButton original =
                originalTransform.GetComponent<UIButton>();
            if (original == null)
                return;

            Transform clone = UnityEngine.Object.Instantiate(
                originalTransform,
                originalTransform.position +
                    new Vector3(0f, 1.71f, 0f),
                originalTransform.rotation,
                grave
            );
            clone.name = ChangelogButtonName;

            foreach (TextMeshProUGUI text in
                clone.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                text.text = "Plants+ Changelog";
            }

            UIButton button = clone.GetComponent<UIButton>();
            if (button == null)
            {
                UnityEngine.Object.Destroy(clone.gameObject);
                return;
            }

            button.clickEvent = new UnityEvent();
            Action openAction =
                () => OpenPlantsPlusChangelog(original);
            UnityAction unityAction = openAction;
            button.clickEvent.AddListener(unityAction);
        }

        private static void OpenPlantsPlusChangelog(
            UIButton originalButton)
        {
            try
            {
                changelogPending = true;
                originalButton.clickEvent.Invoke();

                BaseMenu? notice = FindNoticeMenu();
                if (notice != null &&
                    TryReplaceChangelog(notice))
                {
                    changelogPending = false;
                }
            }
            catch (Exception exception)
            {
                changelogPending = false;
                Plugin.Logger.LogError(
                    "[Main Menu] Plants+ changelog could not open: " +
                    exception
                );
            }
        }

        private static BaseMenu? FindNoticeMenu()
        {
            if (GameAPP.canvasUp == null)
                return null;

            foreach (BaseMenu menu in
                GameAPP.canvasUp.GetComponentsInChildren<BaseMenu>(true))
            {
                if (menu != null &&
                    menu.gameObject.name.Contains("NoticePauseMenu"))
                {
                    return menu;
                }
            }

            return null;
        }

        private static bool TryReplaceChangelog(BaseMenu menu)
        {
            Transform content = menu.transform.Find(
                "Scroll View/Viewport/Content"
            );
            if (content == null)
                return false;

            TextMeshProUGUI text =
                content.GetComponent<TextMeshProUGUI>();
            if (text == null)
                return false;

            text.text = ChangelogText;
            text.margin = new Vector4(6f, 2f, 12f, 0f);
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            Canvas.ForceUpdateCanvases();
            text.ForceMeshUpdate(false, false);
            return true;
        }

        private static Sprite? LoadLogoSprite()
        {
            if (logoSprite != null)
                return logoSprite;

            Assembly assembly =
                Assembly.GetExecutingAssembly();
            using Stream? stream =
                assembly.GetManifestResourceStream(
                    LogoResourceName
                );
            if (stream == null)
            {
                Plugin.Logger.LogError(
                    "[Main Menu] Embedded v1.1 logo was not found."
                );
                return null;
            }

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
            if (!ImageConversion.LoadImage(texture, bytes, false))
            {
                UnityEngine.Object.Destroy(texture);
                return null;
            }

            texture.name = "PlantsPlus_V11_Logo_Texture";
            texture.filterMode = FilterMode.Bilinear;
            logoSprite = Sprite.Create(
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
            logoSprite.name = "PlantsPlus_V11_Logo_Sprite";
            return logoSprite;
        }
    }

    [HarmonyPatch(typeof(BaseMenu), "Awake")]
    internal static class PlantsPlusMainMenuAwakePatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(BaseMenu __instance)
        {
            if (__instance == null)
                return;

            string name = __instance.gameObject.name;
            if (name.StartsWith("MainMenu"))
                MainMenuBranding.DecorateMainMenu(__instance);

            MainMenuBranding.ApplyPendingChangelog(__instance);
        }
    }

    [HarmonyPatch(typeof(MainMenu), "Update")]
    internal static class PlantsPlusMainMenuUpdatePatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(MainMenu __instance)
        {
            MainMenuBranding.AnimateLogo(__instance);
        }
    }

}
