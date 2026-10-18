using System;
using HarmonyLib;
using Il2Cpp;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlantsPlus.Core
{
    internal static class NightRoofSurvival
    {
        internal const int Normal = 101, Hard = 102, Endless = 103;
        internal static int Native(int id) => id switch {
            Normal => (int)SurvivalLevel.RoofNormal,
            Hard => (int)SurvivalLevel.RoofHard,
            Endless => (int)SurvivalLevel.RoofEndless, _ => 0
        };
        internal static bool Active => GameAPP.theBoardType == LevelType.Survival && Native(GameAPP.theBoardLevel) != 0;
        internal static string Title(int id)
        {
            string suffix = id == Hard ? LocalizationBridge.Select("（困难）", " (Hard)", " (difficile)") :
                id == Endless ? LocalizationBridge.Select("（无尽）", " (Endless)", " (infini)") : "";
            return LocalizationBridge.Select("夜间屋顶", "Night Roof", "Toit nocturne") + suffix;
        }

        private static void EnsureProgressSlots()
        {
            var previous = GameAPP.survivalLevelCompleted;
            if (previous == null || previous.Length > Endless) return;
            var extended = new bool[Endless + 1];
            for (int i = 0; i < previous.Length; i++) extended[i] = previous[i];
            GameAPP.survivalLevelCompleted = extended;
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(Advanture_Btn), "Start")]
        private static void Menu(Advanture_Btn __instance)
        {
            if (__instance.levelType != LevelType.Survival ||
                __instance.buttonNumber != (int)SurvivalLevel.RoofNormal) return;
            try
            {
                EnsureProgressSlots();
                Transform card = __instance.transform.parent;
                Transform page = card.parent;
                if (page == null || page.Find("PvZFusionPlus_NightRoofSurvival_101") != null) return;
                var buttons = page.GetComponentsInChildren<Advanture_Btn>(true);
                var first = FindButton(buttons, (int)SurvivalLevel.DayNormal);
                var last = FindButton(buttons, (int)SurvivalLevel.SnowEndless);
                var hard = FindButton(buttons, (int)SurvivalLevel.DayHard);
                var endless = FindButton(buttons, (int)SurvivalLevel.DayEndless);
                if (first == null || last == null || hard == null || endless == null)
                    throw new InvalidOperationException("Native survival grid anchors missing.");
                var firstRect = first.transform.parent.GetComponent<RectTransform>();
                var lastRect = last.transform.parent.GetComponent<RectTransform>();
                var hardRect = hard.transform.parent.GetComponent<RectTransform>();
                var endlessRect = endless.transform.parent.GetComponent<RectTransform>();
                var layout = page.gameObject.AddComponent<NightRoofSurvivalLayout>();
                layout.startX = firstRect.anchoredPosition.x;
                layout.stepX = (lastRect.anchoredPosition.x - layout.startX) / 6f;
                layout.normalY = firstRect.anchoredPosition.y;
                layout.hardY = hardRect.anchoredPosition.y;
                layout.endlessY = endlessRect.anchoredPosition.y;
                layout.anchorMin = firstRect.anchorMin;
                layout.anchorMax = firstRect.anchorMax;
                layout.pivot = firstRect.pivot;
                layout.scale = firstRect.localScale * 0.82f;
                if (Mathf.Abs(layout.stepX) < 1f)
                    throw new InvalidOperationException("Native survival column spacing invalid.");
                for (int i = 0; i < 3; i++)
                {
                    var source = FindButton(buttons, Native(Normal + i));
                    if (source == null) throw new InvalidOperationException("Missing roof survival template.");
                    GameObject clone = UnityEngine.Object.Instantiate(source.transform.parent.gameObject, page);
                    clone.name = "PvZFusionPlus_NightRoofSurvival_" + (Normal + i);
                    var custom = clone.GetComponentInChildren<Advanture_Btn>(true);
                    custom.buttonNumber = Normal + i;
                    custom.thisMenu = __instance.thisMenu;
                    custom.levelCtrl = __instance.levelCtrl;
                    custom.randomLevel = false;
                    RefreshCaption(custom);
                    DecoratePortrait(custom, Normal + i);
                    clone.SetActive(true);
                }
                layout.Refresh();
                Plugin.Logger.LogInfo("[Night Roof Survival] Normal/Hard/Endless cards added after Roof (IDs 101-103).");
            }
            catch (Exception error) { Plugin.Logger.LogError("[Night Roof Survival] Menu setup: " + error); }
        }


        internal static Advanture_Btn? FindButton(Advanture_Btn[] buttons, int id)
        {
            foreach (var button in buttons)
                if (button.levelType == LevelType.Survival && button.buttonNumber == id)
                    return button;
            return null;
        }

        private static Image AddPortraitImage(string name, Transform parent, Sprite sprite)
        {
            var obj = new GameObject(name);
            obj.AddComponent<RectTransform>().SetParent(parent, false);
            var image = obj.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            image.color = Color.white;
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.offsetMin = Vector2.zero;
            image.rectTransform.offsetMax = Vector2.zero;
            return image;
        }

        internal static void RefreshCaption(Advanture_Btn button)
        {
            if (Native(button.buttonNumber) == 0) return;
            var label = button.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            if (label == null) return;
            string title = Title(button.buttonNumber).Replace(" (", "\n(").Replace("（", "\n（");
            // Native/translator Start may rewrite the cloned Roof label using
            // its unknown custom ID. Restore only our caption after that pass.
            if (label.text != title) label.text = title;
            if (!label.gameObject.activeSelf) label.gameObject.SetActive(true);
            label.enabled = true;
            label.color = new Color(0.18f, 0.18f, 0.26f, 1f);
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.SetAsLastSibling();
        }

        private static void DecoratePortrait(Advanture_Btn button, int id)
        {
            Sprite? terrain = SuperLevelEditorPlus.GetNightRoofPreview();
            PlantType type = id == Endless ? (PlantType)Plants.PulseShroom.ID :
                (PlantType)Plants.BorealOrchid.BorealOrchidID;
            Sprite? plant = NightRoofClassic.GetPlantSprite(type);
            if (terrain == null || plant == null)
                throw new InvalidOperationException("Missing survival portrait for plant " + type);
            // Root/Image was the template plant (Flower Pot), not the terrain.
            Transform previous = button.transform.parent.Find("Image");
            if (previous != null) previous.gameObject.SetActive(false);
            // The frame has a transparent picture hole. Put the portrait BEHIND
            // the frame as its sibling, rather than covering the stone border
            // with a rectangular child image drawn in front of the frame.
            var plane = new GameObject("PvZFusionPlus_Portrait");
            var planeRect = plane.AddComponent<RectTransform>();
            var frameRect = button.GetComponent<RectTransform>();
            planeRect.SetParent(button.transform.parent, false);
            planeRect.anchorMin = frameRect.anchorMin;
            planeRect.anchorMax = frameRect.anchorMax;
            planeRect.pivot = frameRect.pivot;
            planeRect.sizeDelta = frameRect.sizeDelta;
            planeRect.anchoredPosition = frameRect.anchoredPosition;
            planeRect.localScale = frameRect.localScale;
            planeRect.localRotation = frameRect.localRotation;
            planeRect.SetSiblingIndex(button.transform.GetSiblingIndex());
            var viewport = new GameObject("PictureClip");
            var rect = viewport.AddComponent<RectTransform>();
            rect.SetParent(planeRect, false);
            rect.anchorMin = new Vector2(0.08f, 0.43f);
            rect.anchorMax = new Vector2(0.92f, 0.94f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            viewport.AddComponent<RectMask2D>();
            AddPortraitImage("Terrain", rect, terrain);
            Image icon = AddPortraitImage("Plant", rect, plant);
            icon.preserveAspect = true;
            icon.rectTransform.offsetMin = new Vector2(14f, 1f);
            icon.rectTransform.offsetMax = new Vector2(-14f, -1f);
        }

        // Use the vanilla Roof setup transiently, but retain distinct public IDs
        // for saves and trophies. Finalizers restore the ID even on exceptions.
        internal static int BeginNativeSetup()
        {
            if (!Active) return -1;
            EnsureProgressSlots();
            int original = GameAPP.theBoardLevel;
            GameAPP.theBoardLevel = Native(original);
            return original;
        }
        internal static Exception? EndNativeSetup(int original, Exception? error)
        {
            if (original >= 0) GameAPP.theBoardLevel = original;
            return error;
        }
        private static void ApplyScene(Board board)
        {
            if (!Active) return;
            board.sceneType = SceneType.NightRoof;
            var tag = board.boardTag; tag.isNight = true; tag.isRoof = true;
            board.boardTag = tag;
            board.fallSun = false;
        }

        internal static void FinishBoard(Board board, int original)
        {
            if (original < 0) return;
            GameAPP.theBoardLevel = original;
            try
            {
                ApplyScene(board);
                GameObject background = null!;
                if (NightRoofMap.TryCreate(ref background)) board.ChangeMap(background);
                NightRoofMoon.Attach(board);
                Plugin.Logger.LogInfo("[Night Roof Survival] Board ready: " + Title(original) +
                    " | Native rounds=" + board.theSurvivalMaxRound);
            }
            catch (Exception error) { Plugin.Logger.LogError("[Night Roof Survival] Scene setup: " + error); }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InitZombieList), nameof(InitZombieList.InitZombie))]
        private static void Zombies(LevelType __0, ref int __1)
        { if (__0 == LevelType.Survival && Native(__1) != 0) __1 = Native(__1); }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WaveManager), nameof(WaveManager.GetMaxWave))]
        private static void Waves(LevelType __0, ref int __1)
        { if (__0 == LevelType.Survival && Native(__1) != 0) __1 = Native(__1); }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InGameUI), "GetSurvivalLevelName")]
        private static bool Name(ref string __result)
        { if (!Active) return true; __result = Title(GameAPP.theBoardLevel); return false; }
    }

    [HarmonyPatch(typeof(Board), "Awake")]
    internal static class NightRoofSurvivalAwake
    {
        [HarmonyPrefix, HarmonyPriority(Priority.First)]
        private static void Prefix(out int __state) { __state = NightRoofSurvival.BeginNativeSetup(); }
        [HarmonyFinalizer, HarmonyPriority(Priority.Last)]
        private static Exception? Finalizer(int __state, Exception? __exception) => NightRoofSurvival.EndNativeSetup(__state, __exception);
    }
    [HarmonyPatch(typeof(Board), "Start")]
    internal static class NightRoofSurvivalStart
    {
        [HarmonyPrefix, HarmonyPriority(Priority.First)]
        private static void Prefix(out int __state) { __state = NightRoofSurvival.BeginNativeSetup(); }
        [HarmonyFinalizer, HarmonyPriority(Priority.Last)]
        private static Exception? Finalizer(Board __instance, int __state, Exception? __exception)
        {
            NightRoofSurvival.EndNativeSetup(__state, __exception);
            if (__exception == null) NightRoofSurvival.FinishBoard(__instance, __state);
            return __exception;
        }
    }
}
