using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace PlantsPlus.Core
{
    public sealed class NightRoofSurvivalLayout : MonoBehaviour
    {
        internal float startX, stepX, normalY, hardY, endlessY;
        internal Vector2 anchorMin, anchorMax, pivot;
        internal Vector3 scale;
        private int settleFrames;
        private float nextCaptionRefresh;
        private Advanture_Btn[]? buttons;
        private readonly Dictionary<int, Sprite> frames = new();
        public NightRoofSurvivalLayout(IntPtr pointer) : base(pointer) { }
        public void OnEnable() { settleFrames = 0; nextCaptionRefresh = 0; }
        public void LateUpdate()
        {
            // Apply after native button Start/first layout, not halfway through
            // the grid's initialization. Never compound scale or infer columns.
            if (settleFrames++ < 4) Refresh();
            // Late initialization/localization can replace the clone's title
            // after its Start. No geometry is changed by this caption-only pass.
            if (buttons != null && Time.unscaledTime >= nextCaptionRefresh)
            {
                nextCaptionRefresh = Time.unscaledTime + 0.25f;
                foreach (var button in buttons)
                    if (button != null) NightRoofSurvival.RefreshCaption(button);
            }
        }

        [HideFromIl2Cpp]
        internal void Refresh()
        {
            if (Mathf.Abs(stepX) < 1f) return;
            buttons = GetComponentsInChildren<Advanture_Btn>(true);
            int[][] rows = {
                new[] { (int)SurvivalLevel.DayNormal, (int)SurvivalLevel.NightNormal,
                    (int)SurvivalLevel.PoolNormal, (int)SurvivalLevel.FogNormal,
                    (int)SurvivalLevel.RoofNormal, NightRoofSurvival.Normal, (int)SurvivalLevel.SnowEndless },
                new[] { (int)SurvivalLevel.DayHard, (int)SurvivalLevel.NightHard,
                    (int)SurvivalLevel.PoolHard, (int)SurvivalLevel.FogHard,
                    (int)SurvivalLevel.RoofHard, NightRoofSurvival.Hard, (int)SurvivalLevel.EndlessReversePool },
                new[] { (int)SurvivalLevel.DayEndless, (int)SurvivalLevel.NightEndless,
                    (int)SurvivalLevel.PoolEndless, (int)SurvivalLevel.FogEndless,
                    (int)SurvivalLevel.RoofEndless, NightRoofSurvival.Endless, (int)SurvivalLevel.EndlessPool_crisis }
            };
            float[] ys = { normalY, hardY, endlessY };
            for (int row = 0; row < rows.Length; row++)
                for (int column = 0; column < rows[row].Length; column++)
                {
                    var button = NightRoofSurvival.FindButton(buttons, rows[row][column]);
                    if (button == null) continue;
                    var rect = button.transform.parent.GetComponent<RectTransform>();
                    rect.anchorMin = anchorMin;
                    rect.anchorMax = anchorMax;
                    rect.pivot = pivot;
                    rect.anchoredPosition = new Vector2(startX + column * stepX, ys[row]);
                    rect.localScale = scale;
                    Image frame = button.GetComponent<Image>();
                    if (frame != null && frame.sprite != null && !frames.ContainsKey(button.GetInstanceID()))
                        frames.Add(button.GetInstanceID(), frame.sprite);
                    if (NightRoofSurvival.Native(button.buttonNumber) != 0)
                    {
                        NightRoofSurvival.RefreshCaption(button);
                        // Do not keep the template's completed trophy active.
                        var progress = GameAPP.survivalLevelCompleted;
                        bool completed = progress != null && button.buttonNumber < progress.Length && progress[button.buttonNumber];
                        foreach (Transform child in rect.GetComponentsInChildren<Transform>(true))
                            if (child.name == "Trophy") child.gameObject.SetActive(completed);
                    }
                }
        }

        [HideFromIl2Cpp]
        internal void Hover(Advanture_Btn button, bool enter)
        {
            // Native hover restores a cached origin from before our reflow.
            // Keep the native highlight sprites, but do not run its position
            // writes for this managed grid (including the vanilla cards in it).
            Refresh();
            Image image = button.GetComponent<Image>();
            if (image == null) return;
            if (enter && button.highLightSprite != null) image.sprite = button.highLightSprite;
            else if (frames.TryGetValue(button.GetInstanceID(), out Sprite? original)) image.sprite = original;
        }
    }

    internal static class NightRoofSurvivalHover
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Advanture_Btn), "OnMouseEnter")]
        private static bool Enter(Advanture_Btn __instance) => Handle(__instance, true);
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Advanture_Btn), "OnMouseExit")]
        private static bool Exit(Advanture_Btn __instance) => Handle(__instance, false);
        private static bool Handle(Advanture_Btn button, bool enter)
        {
            var layout = button.GetComponentInParent<NightRoofSurvivalLayout>();
            if (layout == null) return true;
            layout.Hover(button, enter);
            return false;
        }
    }
}
