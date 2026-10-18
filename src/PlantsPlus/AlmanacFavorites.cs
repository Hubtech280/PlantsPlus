using System;
using System.IO;
using HarmonyLib;
using Il2Cpp;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PlantsPlus.Core
{
    internal static class AlmanacFavorites
    {
        private static FavoriteStore? store;
        private static Sprite? selected;
        private static Sprite? unselected;
        internal static FavoriteStore Store => store ??= new FavoriteStore(Path.Combine(
            Path.GetDirectoryName(typeof(Plugin).Assembly.Location)!,
            "PlantsPlusData", "favorites.txt"));
        internal static string Label => LocalizationBridge.Select("收藏", "Favorites", "Favoris");

        internal static Sprite Heart(bool favorite)
        {
            Sprite? cached = favorite ? selected : unselected;
            if (cached != null) return cached;
            string name = favorite ? "favorite_selected.png" : "favorite_unselected.png";
            using Stream stream = typeof(Plugin).Assembly.GetManifestResourceStream(
                "PlantsPlus.Resources.UI." + name) ?? throw new FileNotFoundException(name);
            using var bytes = new MemoryStream();
            stream.CopyTo(bytes);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, bytes.ToArray(), true))
                throw new InvalidDataException("Could not decode " + name);
            texture.name = name;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f);
            UnityEngine.Object.DontDestroyOnLoad(texture);
            UnityEngine.Object.DontDestroyOnLoad(sprite);
            if (favorite) selected = sprite; else unselected = sprite;
            return sprite;
        }

        [HarmonyPatch(typeof(AlmanacPlantWindow), "SetPlant")]
        private static class WindowPatch
        {
            [HarmonyPostfix, HarmonyPriority(Priority.Last)]
            private static void Postfix(AlmanacPlantWindow __instance, PlantType thePlantType)
            {
                try
                {
                    var controller = __instance.GetComponent<AlmanacFavoriteHeart>() ??
                        __instance.gameObject.AddComponent<AlmanacFavoriteHeart>();
                    controller.Refresh(__instance, (int)thePlantType);
                }
                catch (Exception error) { Plugin.Logger.LogError("[Favorites] Heart: " + error); }
            }
        }

        [HarmonyPatch(typeof(AlmanacPlantMenu), "Awake")]
        private static class CategoryPatch
        {
            [HarmonyPostfix, HarmonyPriority(Priority.Last)]
            private static void Postfix(AlmanacPlantMenu __instance)
            {
                try
                {
                    Transform content = __instance.transform.Find("Scroll View/Viewport/Content");
                    if (content == null || content.Find("LookFavorites") != null) return;
                    Transform template = content.Find("LookRedCard");
                    if (template == null) return;
                    Transform category = UnityEngine.Object.Instantiate(template, content);
                    category.name = "LookFavorites";
                    category.SetSiblingIndex(0);
                    foreach (var text in category.GetComponentsInChildren<TextMeshProUGUI>(true))
                        text.text = Label;
                    var state = __instance.GetComponent<AlmanacFavoriteFilter>() ??
                        __instance.gameObject.AddComponent<AlmanacFavoriteFilter>();
                    var button = category.GetComponent<UIButton>();
                    button.clickEvent = new UnityEvent();
                    button.clickEvent.AddListener((Action)(() => state.Show(__instance)));
                    var rect = content.GetComponent<RectTransform>();
                    var layout = content.GetComponent<VerticalLayoutGroup>();
                    float height = category.GetComponent<RectTransform>().rect.height;
                    float spacing = layout != null ? layout.spacing : 5f;
                    rect.sizeDelta = new Vector2(rect.sizeDelta.x, rect.sizeDelta.y + height + spacing);
                    if (layout == null)
                    {
                        float bottom = template.localPosition.y;
                        for (int i = 0; i < content.childCount; i++)
                            if (content.GetChild(i) != category)
                                bottom = Mathf.Min(bottom, content.GetChild(i).localPosition.y);
                        category.localPosition = new Vector3(template.localPosition.x,
                            bottom - height - spacing, template.localPosition.z);
                    }
                    category.gameObject.SetActive(true);
                    LayoutRebuilder.MarkLayoutForRebuild(rect);
                }
                catch (Exception error) { Plugin.Logger.LogError("[Favorites] Category: " + error); }
            }
        }

        // Native categories and search use either overload. Reset only our
        // filter flag, without replacing the native filtering implementation.
        [HarmonyPatch(typeof(AlmanacPlantMenu), "ShowPlants", new Type[] { typeof(Il2CppSystem.Func<PlantType, bool>) })]
        private static class PredicatePatch
        {
            [HarmonyPrefix]
            private static void Prefix(AlmanacPlantMenu __instance) => ClearFilter(__instance);
        }
        [HarmonyPatch(typeof(AlmanacPlantMenu), "ShowPlants", new Type[] { typeof(Il2CppSystem.Collections.Generic.List<PlantType>) })]
        private static class ListPatch
        {
            [HarmonyPrefix]
            private static void Prefix(AlmanacPlantMenu __instance) => ClearFilter(__instance);
        }
        private static void ClearFilter(AlmanacPlantMenu menu)
        {
            var state = menu.GetComponent<AlmanacFavoriteFilter>();
            if (state != null) state.showingFavorites = false;
        }
    }

    public sealed class AlmanacFavoriteFilter : MonoBehaviour
    {
        internal bool showingFavorites;
        public AlmanacFavoriteFilter(IntPtr pointer) : base(pointer) { }
        internal void Show(AlmanacPlantMenu menu)
        {
            // Load before entering native callback so file errors never cross
            // the IL2CPP predicate trampoline.
            var favorites = AlmanacFavorites.Store;
            Func<PlantType, bool> filter = type => favorites.Contains((int)type);
            menu.ShowPlants(filter);
            showingFavorites = true;
        }
    }

    public sealed class AlmanacFavoriteHeart : MonoBehaviour
    {
        private AlmanacPlantWindow? window;
        private Image? image;
        private UIButton? button;
        private int plantId;
        public AlmanacFavoriteHeart(IntPtr pointer) : base(pointer) { }

        internal void Refresh(AlmanacPlantWindow target, int id)
        {
            window = target;
            plantId = id;
            if (id < 0)
            {
                if (image != null) image.gameObject.SetActive(false);
                return;
            }
            Sprite heart = AlmanacFavorites.Heart(AlmanacFavorites.Store.Contains(id));
            if (image == null)
            {
                var obj = new GameObject("PvZFusionPlus_FavoriteHeart");
                obj.layer = target.gameObject.layer;
                var rect = obj.AddComponent<RectTransform>();
                rect.SetParent(target.transform, false);
                // Root is 322 x 484. This is the upper-right wooden margin,
                // outside the portrait, name, scroll text and skin arrows.
                rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = new Vector2(-22f, -20f);
                rect.sizeDelta = new Vector2(32f, 32f);
                obj.AddComponent<CanvasRenderer>();
                image = obj.AddComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.sprite = heart;
                var collider = obj.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(32f, 32f);
                collider.offset = new Vector2(-16f, -16f);
                button = obj.AddComponent<UIButton>();
                button.image = image;
                button.rectTransform = rect;
                button.highLightSprite = image.sprite;
                button.useLight = false;
                button.onSelect = new UnityEvent<UIButton>();
                button.clickEvent = new UnityEvent();
                button.clickEvent.AddListener((Action)Toggle);
                button.Interactable = true;
                rect.SetAsLastSibling();
            }
            image.gameObject.SetActive(true);
            image.sprite = heart;
            if (button != null)
            {
                button.highLightSprite = heart;
                // Native hover exit restores this cached sprite. Update both
                // states so leaving the button cannot undo the visible toggle.
                button.originSprite = heart;
            }
        }

        private void Toggle()
        {
            if (window == null) return;
            try
            {
                AlmanacFavorites.Store.Toggle(plantId);
                Refresh(window, plantId);
                var menu = window.menu;
                var filter = menu != null ? menu.GetComponent<AlmanacFavoriteFilter>() : null;
                if (filter != null && filter.showingFavorites) filter.Show(menu!);
            }
            catch (Exception error) { Plugin.Logger.LogError("[Favorites] Save failed: " + error); }
        }
    }
}
