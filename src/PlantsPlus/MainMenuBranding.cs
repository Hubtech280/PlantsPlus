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
            "PvZFusionPlus_Logo";
        private const string ChangelogButtonName =
            "PvZFusionPlusChangelogButton";

        private static bool changelogPending;
        private static BaseMenu? customChangelogMenu;
        private static float changelogRequestDeadline;
        private static RectTransform? animatedLogo;
        private static Graphic? animatedLogoGraphic;
        private static float logoAnimationStart;
        private static Vector2 logoTargetPosition;
        private static bool logoAnimationRunning;
        private static float nextBrandingRetryAt;
        private static Texture2D? principalLogoTexture;
        private static Texture2D? chineseLogoTexture;
        private static bool logoLoadFailed;

        internal static void OnStart()
        {
            Plugin.Logger.LogInfo(
                "[Main Menu] PvZ Fusion+ changelog enabled."
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

            // Awake may still be followed by the translator's own postfix.
            // Keep the request pending until the open callback has completed.
            TryReplaceChangelog(menu);
        }

        private static void EnsureLogo(BaseMenu menu)
        {
            Transform existing = menu.transform.Find(LogoObjectName);
            if (existing != null)
            {
                UpdateLogoImage(existing.GetComponent<RawImage>());
                BeginLogoAnimation(existing as RectTransform);
                return;
            }

            GameObject logo = new GameObject(LogoObjectName);
            RectTransform rect =
                logo.AddComponent<RectTransform>();
            logo.AddComponent<CanvasRenderer>();
            RawImage image = logo.AddComponent<RawImage>();
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
            rect.sizeDelta = new Vector2(180f, 142f);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;

            image.color = Color.white;
            image.raycastTarget = false;
            UpdateLogoImage(image);

            // Render above the menu background while keeping the existing
            // interactive grave and buttons untouched.
            rect.SetAsLastSibling();
            BeginLogoAnimation(rect);
        }

        private static void UpdateLogoImage(RawImage? image)
        {
            if (image == null || logoLoadFailed)
                return;
            try
            {
                bool chinese = LocalizationBridge.Current == PlantsPlusLanguage.Chinese;
                Texture2D? texture = chinese ? chineseLogoTexture : principalLogoTexture;
                if (texture == null)
                {
                    string resource = "PlantsPlus.Resources.Branding." +
                        (chinese ? "logo_chinese.png" : "logo.png");
                    using Stream stream = Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream(resource) ??
                        throw new InvalidOperationException("Missing logo: " + resource);
                    using var bytes = new MemoryStream();
                    stream.CopyTo(bytes);
                    texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!ImageConversion.LoadImage(texture, bytes.ToArray(), true))
                    {
                        UnityEngine.Object.Destroy(texture);
                        throw new InvalidOperationException("Could not decode logo: " + resource);
                    }
                    texture.name = chinese ? "PvZFusionPlus_LogoChinese" : "PvZFusionPlus_LogoPrincipal";
                    texture.filterMode = FilterMode.Bilinear;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    UnityEngine.Object.DontDestroyOnLoad(texture);
                    if (chinese) chineseLogoTexture = texture;
                    else principalLogoTexture = texture;
                }
                if (image.texture == texture)
                    return;
                image.texture = texture;
                // Preserve the supplied artwork's aspect ratio and transparency.
                image.rectTransform.sizeDelta = new Vector2(180f,
                    180f * texture.height / texture.width);
            }
            catch (Exception exception)
            {
                logoLoadFailed = true;
                image.enabled = false;
                Plugin.Logger.LogError("[Main Menu] Logo could not load: " + exception);
            }
        }

        private static void BeginLogoAnimation(
            RectTransform? rect
        )
        {
            if (rect == null)
                return;

            Graphic graphic = rect.GetComponent<Graphic>();
            if (graphic == null)
                return;

            logoTargetPosition = new Vector2(-205f, -100f);
            rect.anchoredPosition =
                logoTargetPosition + new Vector2(0f, -18f);
            rect.localScale = Vector3.one * 0.82f;

            Color color = graphic.color;
            color.a = 0f;
            graphic.color = color;

            animatedLogo = rect;
            animatedLogoGraphic = graphic;
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
            else
                UpdateLogoImage(menu.transform.Find(LogoObjectName).GetComponent<RawImage>());

            EnsureChangelogButton(menu);
        }

        internal static void AnimateLogo(MainMenu menu)
        {
            if (!logoAnimationRunning ||
                menu == null ||
                animatedLogo == null ||
                animatedLogoGraphic == null)
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

            Color color = animatedLogoGraphic.color;
            color.a = eased;
            animatedLogoGraphic.color = color;

            if (t >= 1f)
            {
                animatedLogo.anchoredPosition = logoTargetPosition;
                animatedLogo.localScale = Vector3.one;
                color.a = 1f;
                animatedLogoGraphic.color = color;
                logoAnimationRunning = false;
            }
        }

        private static void EnsureChangelogButton(BaseMenu menu)
        {
            Transform grave = menu.transform.Find("Grave");
            if (grave == null)
                return;

            Transform existing = grave.Find(ChangelogButtonName);
            if (existing != null)
            {
                UpdateChangelogLabel(existing);
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

            UpdateChangelogLabel(clone);

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

        private static void UpdateChangelogLabel(Transform button)
        {
            string label = ChangelogContent.ButtonLabel;
            foreach (TextMeshProUGUI text in
                button.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text.text != label)
                    text.text = label;
            }
        }

        private static void OpenPlantsPlusChangelog(
            UIButton originalButton)
        {
            try
            {
                changelogPending = true;
                changelogRequestDeadline = Time.unscaledTime + 5f;
                originalButton.clickEvent.Invoke();

                BaseMenu? notice = FindNoticeMenu();
                if (notice != null &&
                    TryReplaceChangelog(notice))
                {
                    customChangelogMenu = notice;
                    ResetChangelogScroll(notice);
                    changelogPending = false;
                    Plugin.Logger.LogInfo("[Main Menu] Custom PvZ Fusion+ changelog displayed.");
                }
            }
            catch (Exception exception)
            {
                changelogPending = false;
                Plugin.Logger.LogError(
                    "[Main Menu] PvZ Fusion+ changelog could not open: " +
                    exception
                );
            }
        }

        internal static void RefreshCustomChangelog()
        {
            if (customChangelogMenu != null)
            {
                if (!customChangelogMenu.gameObject.activeInHierarchy)
                    customChangelogMenu = null;
                else
                    TryReplaceChangelog(customChangelogMenu);
            }
            if (!changelogPending)
                return;

            BaseMenu? notice = FindNoticeMenu();
            if (notice != null && TryReplaceChangelog(notice))
            {
                customChangelogMenu = notice;
                ResetChangelogScroll(notice);
                changelogPending = false;
                Plugin.Logger.LogInfo("[Main Menu] Custom PvZ Fusion+ changelog displayed (deferred).");
            }
            else if (Time.unscaledTime >= changelogRequestDeadline)
            {
                changelogPending = false;
                Plugin.Logger.LogWarning("[Main Menu] Custom changelog panel/text was not found.");
            }
        }

        private static void ResetChangelogScroll(BaseMenu menu)
        {
            ScrollRect scroll = menu.GetComponentInChildren<ScrollRect>(true);
            if (scroll != null)
            {
                scroll.StopMovement();
                scroll.verticalNormalizedPosition = 1f;
            }
        }

        private static BaseMenu? FindNoticeMenu()
        {
            if (GameAPP.canvasUp == null)
                return null;

            foreach (BaseMenu menu in
                GameAPP.canvasUp.GetComponentsInChildren<BaseMenu>(true))
            {
                if (menu != null && menu.gameObject.activeInHierarchy &&
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
                content.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text == null)
                return false;

            string replacement = ChangelogContent.Text;
            if (text.text == replacement)
                return true;
            text.text = replacement;
            text.margin = new Vector4(6f, 2f, 12f, 0f);
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            Canvas.ForceUpdateCanvases();
            text.ForceMeshUpdate(false, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            RectTransform textRect = text.GetComponent<RectTransform>();
            if (textRect != null)
                textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                    text.preferredHeight + 16f);
            if (contentRect != null)
            {
                contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                    text.preferredHeight + 16f);
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            }
            return true;
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
            MainMenuBranding.RefreshCustomChangelog();
        }
    }

}
