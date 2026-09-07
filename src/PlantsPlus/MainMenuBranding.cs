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
            "PlantsPlus_V12_Logo";
        private const string ChangelogButtonName =
            "PlantsPlusChangelogButton";
        private const string LogoResourceName =
            "PlantsPlus.Resources.Sprites.plants_v1_2_logo.png";

        private const string ChangelogText =
            "<align=center><size=135%><b>Plants+ - Update v1.2</b></size></align>\n" +
            "<align=center><color=#8BEA45>12 new plants. 32 custom plants total.</color></align>\n\n" +
            "<b>NEW PLANTS</b>\n" +
            "- Sea Sharpshooter - grows through 3 stronger stages.\n" +
            "- Cherry StarBomber - two explosive 5-star volleys.\n" +
            "- Three-Buckpeater - iron peas across 3 lanes.\n" +
            "- Sakura Sharpshooter - explosive thorns and Explode-o-shooter synergy.\n" +
            "- Bomber Drone - hovering Cherryshooter support.\n" +
            "- Frostbite Drone - hovering Snow Pea support.\n" +
            "- Ice-Lord Cactus - different attacks for ground and air targets.\n" +
            "- Scovilia Pepper - Jalapeno fire across 3 lanes.\n" +
            "- Atomray-shroom - slowing ray with Demise-shroom effects.\n" +
            "- Sauerkraut-pult - changes attacks depending on zombie distance.\n" +
            "- Cherry Cabbage - gets deadlier as zombies approach the house.\n" +
            "- Lob-shroom - arcing spore shooter that can stack on Puff-shrooms.\n\n" +
            "<b>NEW CONTENT</b>\n" +
            "- Night Roof support in Super Level Editor+.\n" +
            "- New Plants+ v1.2 main-menu logo and changelog.\n" +
            "- New Almanac entries, recipes, projectiles and effects for the v1.2 plants.\n\n" +
            "<b>POLISH</b>\n" +
            "- Three-Buckpeater now fires correctly and has its shadow.\n" +
            "- Sakura Sharpshooter projectile position corrected.\n" +
            "- General gameplay, visuals and stability polish across Plants+.\n\n" +
            "<color=#FFB74D><b>DELAYED</b> - Boreal Orchid is not included in v1.2.0 and will arrive later.</color>\n\n" +
            "<align=center><color=#64DD17><b>Quality Over Quantity.</b></color></align>";

        private static Sprite? logoSprite;
        private static bool changelogPending;
        private static RectTransform? animatedLogo;
        private static Image? animatedLogoImage;
        private static float logoAnimationStart;
        private static Vector2 logoTargetPosition;
        private static bool logoAnimationRunning;
        private static float nextBrandingRetryAt;

        internal static void OnStart()
        {
            Plugin.Logger.LogInfo(
                "[Main Menu] Plants+ v1.2 changelog enabled."
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

        internal static void EnsureBrandingOnUpdate(MainMenu menu)
        {
            if (menu == null || Time.unscaledTime < nextBrandingRetryAt)
                return;

            nextBrandingRetryAt = Time.unscaledTime + 1f;

            if (menu.transform.Find(LogoObjectName) == null)
                EnsureLogo(menu);

            Transform grave = menu.transform.Find("Grave");
            if (grave != null && grave.Find(ChangelogButtonName) == null)
                EnsureChangelogButton(menu);
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
                    "[Main Menu] Embedded Plants+ v1.2 logo was not found."
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

            texture.name = "PlantsPlus_V12_Logo_Texture";
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
            logoSprite.name = "PlantsPlus_V12_Logo_Sprite";
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
            MainMenuBranding.EnsureBrandingOnUpdate(__instance);
            MainMenuBranding.AnimateLogo(__instance);
        }
    }

}
