using CustomizeLib.MelonLoader;
using HarmonyLib;
using Il2Cpp;
using Il2CppTMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace PlantsPlus.Core
{
    [HarmonyPatch(typeof(Advanture_Btn), nameof(Advanture_Btn.Start))]
    internal static class NightRoofClassic
    {
        private static GameObject? cachedNightCard;

        private static string CardName(int level) =>
            "PlantsPlus_Classic" + level;

        [HarmonyPostfix]
        private static void Postfix(Advanture_Btn __instance)
        {
            if (__instance == null)
                return;

            if (__instance.buttonNumber == 10)
            {
                CacheNativeNightCard(__instance);
                return;
            }
            if (__instance.buttonNumber != 45)
                return;

            try
            {
                Transform card = __instance.transform.parent;
                Transform page = card.parent;
                if (page == null || !IsClassicAdventurePage(page))
                    return;

                NightRoofTestLevel.OnStart();
                Transform? level44 = FindLevelCard(page, 44);
                Transform? level37 = FindLevelCard(page, 37);
                Transform? level43 = FindLevelCard(page, 43);
                if (level44 == null)
                    throw new InvalidOperationException(
                        "Classic Level 44 position missing"
                    );

                Vector3 columnStep =
                    card.localPosition - level44.localPosition;
                Vector3 rowStep = level37 != null && level43 != null
                    ? level43.localPosition - level37.localPosition
                    : Vector3.zero;
                Transform? nightCard = FindNativeNightCard();
                if (nightCard == null)
                    Plugin.Logger.LogWarning(
                        "[Night Roof Classic] Native Night card not loaded; " +
                        "using the current page template."
                    );

                for (int level = NightRoofTestLevel.FirstLevel;
                     level < NightRoofTestLevel.FirstLevel +
                         NightRoofTestLevel.LevelCount;
                     level++)
                {
                    if (page.Find(CardName(level)) != null)
                        continue;

                    int customId =
                        NightRoofTestLevel.GetRegisteredId(level);
                    if (customId < 0)
                        continue;

                    if (level == 49 && rowStep == Vector3.zero)
                    {
                        Plugin.Logger.LogWarning(
                            "[Night Roof Classic] Level 49 row position " +
                            "could not be measured; card not inserted."
                        );
                        continue;
                    }

                    GameObject clone = UnityEngine.Object.Instantiate(
                        nightCard != null ? nightCard.gameObject :
                            card.gameObject,
                        page
                    );
                    clone.name = CardName(level);
                    clone.transform.localScale = card.localScale;
                    clone.transform.localPosition = level <= 48
                        ? card.localPosition +
                            columnStep * (level - 45)
                        : level43!.localPosition + rowStep;

                    Advanture_Btn button =
                        clone.GetComponentInChildren<Advanture_Btn>(true);
                    if (button == null)
                    {
                        UnityEngine.Object.Destroy(clone);
                        continue;
                    }
                    button.levelType = (LevelType)66;
                    button.buttonNumber = customId;
                    button.randomLevel = false;
                    button.thisMenu = __instance.thisMenu;
                    button.levelCtrl = __instance.levelCtrl;

                    foreach (TextMeshProUGUI label in
                        clone.GetComponentsInChildren<TextMeshProUGUI>(true))
                    {
                        label.text = LocalizationBridge.Select(
                            "第 " + level + " 关", "Level " + level, "Niveau " + level);
                    }

                    DecorateCard(
                        clone,
                        level,
                        NightRoofTestLevel.GetFeaturedPlant(level)
                    );
                    clone.SetActive(true);
                    Plugin.Logger.LogInfo(
                        "[Night Roof Classic] Level " + level +
                        " inserted | Custom ID = " + customId
                    );
                }
            }
            catch (Exception error)
            {
                Plugin.Logger.LogError(
                    "[Night Roof Classic] Insertion failed: " + error
                );
            }
        }

        private static bool IsClassicAdventurePage(Transform page)
        {
            Transform? ancestor = page;
            while (ancestor != null)
            {
                if (ancestor.name == "PageAdvantureLevel")
                    return true;
                ancestor = ancestor.parent;
            }
            return false;
        }

        private static Transform? FindLevelCard(
            Transform page,
            int level
        )
        {
            foreach (Advanture_Btn button in
                page.GetComponentsInChildren<Advanture_Btn>(true))
            {
                if (button != null && button.buttonNumber == level)
                    return button.transform.parent;
            }
            return null;
        }

        private static Transform? FindNativeNightCard()
        {
            if (cachedNightCard != null)
                return cachedNightCard.transform;

            foreach (Advanture_Btn button in
                Resources.FindObjectsOfTypeAll<Advanture_Btn>())
            {
                if (button != null && button.buttonNumber == 10 &&
                    button.transform.parent != null &&
                    IsClassicAdventurePage(button.transform.parent))
                    return button.transform.parent;
            }
            return null;
        }

        private static void CacheNativeNightCard(Advanture_Btn button)
        {
            if (cachedNightCard != null ||
                button.transform.parent == null ||
                !IsClassicAdventurePage(button.transform.parent))
                return;

            cachedNightCard = UnityEngine.Object.Instantiate(
                button.transform.parent.gameObject
            );
            cachedNightCard.name = "PlantsPlus_NativeNightCardTemplate";
            cachedNightCard.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(cachedNightCard);
        }

        private static void DecorateCard(
            GameObject clone,
            int level,
            PlantType featured
        )
        {
            if (clone.transform.childCount == 0)
                return;

            Image? plantImage =
                clone.transform.GetChild(0).GetComponent<Image>();
            if (plantImage == null)
                return;

            Sprite? featuredSprite = GetPlantSprite(featured);
            if (featuredSprite == null)
            {
                Plugin.Logger.LogWarning(
                    "[Night Roof Classic] Preview missing for " + featured
                );
                return;
            }

            // The first child is the plant portrait, not the terrain. The
            // terrain comes from the native Night card behind this image.
            plantImage.sprite = featuredSprite;
            plantImage.color = Color.white;
            plantImage.preserveAspect = true;
            plantImage.raycastTarget = false;
            plantImage.rectTransform.localScale =
                plantImage.rectTransform.localScale * 1.8f;
        }

        internal static Sprite? GetPlantSprite(PlantType type)
        {
            GameObject? preview = null;
            if (GameAPP.resourcesManager != null &&
                GameAPP.resourcesManager.plantPreviews != null &&
                GameAPP.resourcesManager.plantPreviews.ContainsKey(type))
            {
                preview = GameAPP.resourcesManager.plantPreviews[type];
            }
            if (preview == null &&
                CustomCore.CustomPlants.TryGetValue(type, out var custom))
            {
                preview = custom.Preview;
            }
            return preview?.GetComponentInChildren<SpriteRenderer>(true)?.sprite;
        }
    }
}
