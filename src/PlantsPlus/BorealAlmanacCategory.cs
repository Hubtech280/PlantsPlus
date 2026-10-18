using CustomizeLib.MelonLoader;
using HarmonyLib;
using Il2Cpp;
using Il2CppTMPro;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PlantsPlus.Core
{
    [HarmonyPatch(typeof(AlmanacPlantMenu), "Awake")]
    internal static class BorealAlmanacCategory
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(AlmanacPlantMenu __instance)
        {
            try
            {
                Transform content = __instance.transform.Find(
                    "Scroll View/Viewport/Content");
                if (content == null)
                    return;
                Transform customCategory = content.Find("LookCustom");
                if (customCategory != null)
                {
                    string customLabel = LocalizationBridge.Select(
                        "二创植物", "Custom Plants", "Plantes du mod");
                    foreach (TextMeshProUGUI text in
                        customCategory.GetComponentsInChildren<TextMeshProUGUI>(true))
                        text.text = customLabel;
                }
                if (content.Find("LookBorealPlants") != null)
                    return;

                Transform template = content.Find("LookRedCard");
                if (template == null)
                    return;

                Transform category = UnityEngine.Object.Instantiate(template, content);
                category.name = "LookBorealPlants";
                Transform custom = content.Find("LookCustom");
                if (custom != null)
                    category.SetSiblingIndex(custom.GetSiblingIndex());

                string label = LocalizationBridge.Current switch
                {
                    PlantsPlusLanguage.French => "Plantes boréales",
                    PlantsPlusLanguage.English => "Boreal Plants",
                    _ => "极光植物"
                };
                foreach (TextMeshProUGUI text in
                    category.GetComponentsInChildren<TextMeshProUGUI>(true))
                    text.text = label;

                UIButton button = category.GetComponent<UIButton>();
                button.clickEvent = new UnityEvent();
                Action show = () =>
                {
                    // Build the family from the managed recipe registry only.
                    // Native MixData tuples are unsafe to inspect in this port.
                    HashSet<int> family = BuildFamily();
                    Func<PlantType, bool> filter = type => family.Contains((int)type);
                    __instance.ShowPlants(filter);
                };
                button.clickEvent.AddListener(show);

                RectTransform rect = content.GetComponent<RectTransform>();
                VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
                RectTransform categoryRect = category.GetComponent<RectTransform>();
                float height = categoryRect.rect.height;
                float spacing = layout != null ? layout.spacing : 5f;
                rect.sizeDelta = new Vector2(rect.sizeDelta.x,
                    rect.sizeDelta.y + height + spacing);
                if (layout == null)
                {
                    // Fallback for manually positioned versions of the sidebar.
                    float bottom = template.localPosition.y;
                    for (int i = 0; i < content.childCount; i++)
                    {
                        Transform child = content.GetChild(i);
                        if (child != category)
                            bottom = Mathf.Min(bottom, child.localPosition.y);
                    }
                    category.localPosition = new Vector3(template.localPosition.x,
                        bottom - height - spacing, template.localPosition.z);
                }
                category.gameObject.SetActive(true);
                LayoutRebuilder.MarkLayoutForRebuild(rect);
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError("[Boreal Almanac] Category failed: " + exception);
            }
        }

        private static HashSet<int> BuildFamily()
        {
            var family = new HashSet<int> { Plants.BorealOrchid.BorealOrchidID };
            bool changed;
            do
            {
                changed = false;
                if (CustomCore.CustomFusions == null)
                    break;
                foreach (var (target, left, right) in CustomCore.CustomFusions)
                {
                    if (family.Contains(left) || family.Contains(right))
                        changed |= family.Add(target);
                }
            } while (changed);
            return family;
        }
    }
}
