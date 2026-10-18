#if LEGACY_STARBOUND_NIGHTROOF
using HarmonyLib;
using Il2Cpp;
using System;
using Il2CppTMPro;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class NightRoofAdventure
    {
        internal const int LevelValue = 46;
        internal const int MainLevelCount = 1;
        internal const int ChallengeLevelValue =
            LevelValue + MainLevelCount;
        internal const int ChallengeLevelCount = 0;
        internal static readonly AdvantureLevel Level =
            (AdvantureLevel)LevelValue;

        private static bool registered;

        internal static void OnStart()
        {
            if (registered)
                return;

            registered = true;
            Plugin.Logger.LogInfo(
                "[Night Roof Adventure] Native chapter integration enabled."
            );
        }

        internal static void StartMenuWatcher()
        {
            GameObject watcher = new GameObject(
                "[Plants+] Night Roof Adventure menu watcher"
            );
            watcher.AddComponent<NightRoofAdventureMenuWatcher>();
            UnityEngine.Object.DontDestroyOnLoad(watcher);
            Plugin.Logger.LogInfo(
                "[Night Roof Adventure] Persistent menu watcher started."
            );
        }

        internal static void InstallStrategy()
        {
            try
            {
                var strategies = AdvantureConfig._levelStrategies;
                if (strategies == null)
                    return;

                int installed = 0;
                for (int index = 0;
                     index < MainLevelCount + ChallengeLevelCount;
                     index++)
                {
                    AdvantureLevel level =
                        (AdvantureLevel)(LevelValue + index);
                    bool challenge = index >= MainLevelCount;
                    int chapterIndex = challenge
                        ? index - MainLevelCount
                        : index;
                    NightRoofOneStrategy strategy =
                        new NightRoofOneStrategy(
                            level,
                            chapterIndex,
                            challenge
                        );
                    ILevelStrategy? nativeStrategy =
                        strategy.TryCast<ILevelStrategy>();
                    if (nativeStrategy == null)
                    {
                        Plugin.Logger.LogError(
                            "[Night Roof Adventure] Strategy " +
                            (LevelValue + index) +
                            " could not be exposed as ILevelStrategy."
                        );
                        continue;
                    }

                    strategies[level] = nativeStrategy;
                    installed++;
                }

                Plugin.Logger.LogInfo(
                    "[Night Roof Adventure] Native chapter strategies ready" +
                    " | Test level = 46" +
                    " | Newly installed = " + installed +
                    " | Scene = " + SceneType.NightRoof
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Night Roof Adventure] Strategy installation failed: " +
                    exception
                );
            }
        }

        internal static void ExtendMenu(AdvantureMenu menu)
        {
            if (menu == null)
                return;

            try
            {
                var links = menu.sceneNeedLevel;
                if (links == null)
                    return;

                // sceneNeedLevel is keyed by the chapter being unlocked.
                // Its tuple stores the previous chapter and the level which
                // must be completed. Snow must therefore stay untouched;
                // Night Roof is the new chapter unlocked from Snow 6.
                links[SceneType.NightRoof] =
                    new Il2CppSystem.ValueTuple<
                        SceneType,
                        string,
                        AdvantureLevel
                    >(
                        SceneType.Snow,
                        "Night Roof",
                        AdvantureLevel.Snow6
                    );

                if (menu.currentScene == SceneType.NightRoof &&
                    menu.nextSceneButton != null)
                {
                    menu.nextSceneButton.SetActive(false);
                }

                Plugin.Logger.LogInfo(
                    "[Night Roof Adventure] Native chapter dependency wired" +
                    " | Chapter = NightRoof | Previous = Snow" +
                    " | Unlock = Snow6 | Timing = before menu Start"
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Night Roof Adventure] Menu integration failed: " +
                    exception
                );
            }
        }

        internal static void InspectMenu(
            AdvantureMenu menu,
            string source
        )
        {
            try
            {
                StarAdvantureLevelButton[] buttons =
                    menu.GetComponentsInChildren<
                        StarAdvantureLevelButton
                    >(true);
                int minimum = int.MaxValue;
                int maximum = int.MinValue;
                for (int index = 0; index < buttons.Length; index++)
                {
                    if (buttons[index] == null)
                        continue;
                    minimum = Math.Min(minimum, buttons[index].level);
                    maximum = Math.Max(maximum, buttons[index].level);
                }

                string pageChildren = string.Empty;
                if (menu.page != null)
                {
                    Transform pageTransform = menu.page.transform;
                    for (int index = 0;
                         index < pageTransform.childCount;
                         index++)
                    {
                        if (index > 0)
                            pageChildren += ", ";
                        pageChildren +=
                            pageTransform.GetChild(index).gameObject.name;
                    }
                }

                Plugin.Logger.LogInfo(
                    "[Night Roof Adventure] Menu inspection" +
                    " | Source = " + source +
                    " | Current scene = " + menu.currentScene +
                    " | Level buttons = " + buttons.Length +
                    " | Level range = " +
                    (minimum == int.MaxValue ? -1 : minimum) + "-" +
                    (maximum == int.MinValue ? -1 : maximum) +
                    " | Page children = [" + pageChildren + "]" +
                    " | Has NightRoof dependency = " +
                    (menu.sceneNeedLevel != null &&
                     menu.sceneNeedLevel.ContainsKey(SceneType.NightRoof))
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Night Roof Adventure] Menu inspection failed: " +
                    exception.Message
                );
            }
        }

        internal static void ExposeLevel46(AdvantureMenu menu)
        {
            if (menu == null)
                return;

            try
            {
                StarAdvantureLevelButton[] buttons =
                    menu.GetComponentsInChildren<
                        StarAdvantureLevelButton
                    >(true);
                for (int index = 0; index < buttons.Length; index++)
                {
                    StarAdvantureLevelButton button = buttons[index];
                    if (button == null || button.level != LevelValue)
                        continue;

                    button.mainLine = true;
                    button.gameObject.SetActive(true);
                    if (button.trophy != null)
                        button.trophy.SetActive(false);

                    Plugin.Logger.LogInfo(
                        "[Night Roof Adventure] Existing native Level 46 " +
                        "card exposed after Level 45" +
                        " | Parent = " + button.transform.parent.gameObject.name
                    );
                    return;
                }

                Plugin.Logger.LogWarning(
                    "[Night Roof Adventure] Native Level 46 card not found."
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Night Roof Adventure] Could not expose Level 46: " +
                    exception
                );
            }
        }

    }

    public sealed class NightRoofOneStrategy : BaseLevelStrategy
    {
        private AdvantureLevel level = NightRoofAdventure.Level;
        private int chapterIndex;
        private bool challenge;

        public NightRoofOneStrategy(IntPtr pointer) : base(pointer) { }
        public NightRoofOneStrategy() : base() { }
        public NightRoofOneStrategy(
            AdvantureLevel level,
            int chapterIndex,
            bool challenge
        ) : base()
        {
            this.level = level;
            this.chapterIndex = chapterIndex;
            this.challenge = challenge;
        }

        public override AdvantureLevel GetLevel()
        {
            return level;
        }

        public override int GetMaxWave()
        {
            if (challenge)
                return 10 + chapterIndex * 2;

            return 5 + chapterIndex * 5;
        }

        public override Il2CppSystem.Collections.Generic.List<ZombieType>
            GetZombieTypes()
        {
            var zombies =
                new Il2CppSystem.Collections.Generic.List<ZombieType>();
            zombies.Add(ZombieType.NormalZombie);
            zombies.Add(ZombieType.ConeZombie);
            if (chapterIndex >= 1)
                zombies.Add(ZombieType.PolevaulterZombie);
            if (chapterIndex >= 2)
                zombies.Add(ZombieType.BucketZombie);
            if (chapterIndex >= 3)
                zombies.Add(ZombieType.PaperZombie);
            if (chapterIndex >= 4)
                zombies.Add(ZombieType.DoorZombie);
            if (chapterIndex >= 5)
                zombies.Add(ZombieType.FootballZombie);

            if (challenge)
            {
                switch (chapterIndex)
                {
                    case 0:
                        zombies.Add(ZombieType.BucketZombie);
                        break;
                    case 1:
                        zombies.Add(ZombieType.PogoZombie);
                        break;
                    case 2:
                        zombies.Add(ZombieType.LadderZombie);
                        break;
                    case 3:
                        zombies.Add(ZombieType.CatapultZombie);
                        break;
                    case 4:
                        zombies.Add(ZombieType.BalloonZombie);
                        break;
                    case 5:
                        zombies.Add(ZombieType.Gargantuar);
                        break;
                }
            }
            return zombies;
        }

        public override PlantType GetBasePlant()
        {
            return PlantType.Peashooter;
        }

        public override SceneType GetSceneType()
        {
            return SceneType.NightRoof;
        }

        public override string GetLevelName()
        {
            return challenge
                ? "Night Roof Challenge " + (chapterIndex + 1)
                : "Night Roof " + (chapterIndex + 1);
        }

        public override string GetLevelTip()
        {
            if (challenge)
            {
                return "A special challenge beneath the moonlit roof.";
            }

            return chapterIndex == 0
                ? "The first night on the roof."
                : "Survive the growing horde on the Night Roof.";
        }
    }

    [HarmonyPatch]
    internal static class NightRoofAdventurePatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(
            typeof(AdvantureConfig),
            nameof(AdvantureConfig.InitializeStrategies)
        )]
        private static void InitializeStrategiesPostfix()
        {
            NightRoofAdventure.InstallStrategy();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AdvantureMenu), nameof(AdvantureMenu.Start))]
        private static void AdventureMenuStartPrefix(
            AdvantureMenu __instance
        )
        {
            NightRoofAdventure.ExtendMenu(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AdvantureMenu), nameof(AdvantureMenu.Start))]
        private static void AdventureMenuStartPostfix(
            AdvantureMenu __instance
        )
        {
            NightRoofAdventure.ExposeLevel46(__instance);
            NightRoofAdventureIndexInspector.EnsureFromAdventureMenu(
                __instance
            );
            NightRoofAdventure.InspectMenu(__instance, "Start");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AdvantureMenu), nameof(AdvantureMenu.NextScene))]
        private static void AdventureMenuNextPostfix(
            AdvantureMenu __instance
        )
        {
            NightRoofAdventure.InspectMenu(__instance, "NextScene");
        }

        [HarmonyPostfix]
        [HarmonyPatch(
            typeof(StarAdvantureLevelButton),
            nameof(StarAdvantureLevelButton.Awake)
        )]
        private static void AdventureLevelButtonAwakePostfix(
            StarAdvantureLevelButton __instance
        )
        {
            NightRoofAdventureIndexInspector.ObserveButton(__instance);
            NightRoofAdventureIndexInspector.EnsureLevel46(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Page), nameof(Page.NextPage))]
        private static void AdventurePageNextPostfix(Page __instance)
        {
            NightRoofAdventureIndexInspector.ObservePage(
                __instance,
                "NextPage"
            );
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Page), nameof(Page.LastPage))]
        private static void AdventurePagePreviousPostfix(Page __instance)
        {
            NightRoofAdventureIndexInspector.ObservePage(
                __instance,
                "LastPage"
            );
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ClgLevelMgr), nameof(ClgLevelMgr.ChangePage))]
        private static void ChallengeLevelPagePostfix(
            ClgLevelMgr __instance
        )
        {
            NightRoofAdventureIndexInspector.EnsureFromChallengeMenu(
                __instance
            );
        }
    }

    internal static class NightRoofAdventureIndexInspector
    {
        private static bool buttonHierarchyLogged;
        private static bool creatingLevel46;
        private static bool level46Created;

        internal static void EnsureFromAdventureMenu(AdvantureMenu menu)
        {
            if (menu == null || level46Created)
                return;

            StarAdvantureLevelButton[] buttons =
                menu.GetComponentsInChildren<
                    StarAdvantureLevelButton
                >(true);
            for (int index = 0; index < buttons.Length; index++)
            {
                if (buttons[index] != null && buttons[index].level == 45)
                {
                    EnsureLevel46(buttons[index]);
                    return;
                }
            }

            Plugin.Logger.LogWarning(
                "[Night Roof Adventure] Level 45 template not found."
            );
        }

        internal static void EnsureFromChallengeMenu(ClgLevelMgr menu)
        {
            if (menu == null || level46Created)
                return;

            try
            {
                StarAdvantureLevelButton[] buttons =
                    menu.GetComponentsInChildren<
                        StarAdvantureLevelButton
                    >(true);
                for (int index = 0; index < buttons.Length; index++)
                {
                    if (buttons[index] != null && buttons[index].level == 45)
                    {
                        Plugin.Logger.LogInfo(
                            "[Night Roof Adventure] Challenge page detected" +
                            " | Current page = " + menu.currentPage +
                            " | Buttons = " + buttons.Length
                        );
                        EnsureLevel46(buttons[index]);
                        return;
                    }
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Night Roof Adventure] Challenge menu scan failed: " +
                    exception
                );
            }
        }

        internal static void EnsureLevel46(
            StarAdvantureLevelButton template
        )
        {
            if (template == null || template.level != 45 ||
                creatingLevel46 || level46Created)
                return;

            try
            {
                Transform parent = template.transform.parent;
                if (parent == null)
                    return;

                StarAdvantureLevelButton[] buttons =
                    parent.GetComponentsInChildren<
                        StarAdvantureLevelButton
                    >(true);
                StarAdvantureLevelButton? previous = null;
                for (int index = 0; index < buttons.Length; index++)
                {
                    if (buttons[index] != null && buttons[index].level == 44)
                        previous = buttons[index];
                }

                creatingLevel46 = true;
                GameObject clone = UnityEngine.Object.Instantiate(
                    template.gameObject,
                    parent
                );
                clone.name = "Night Roof - Level 46";
                StarAdvantureLevelButton button =
                    clone.GetComponent<StarAdvantureLevelButton>();
                button.level = 46;
                button.mainLine = true;

                if (previous != null)
                {
                    Vector3 step = template.transform.localPosition -
                        previous.transform.localPosition;
                    clone.transform.localPosition =
                        template.transform.localPosition + step;
                }

                foreach (TextMeshProUGUI text in
                    clone.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (text != null && text.text != null &&
                        text.text.Contains("45"))
                        text.text = text.text.Replace("45", "46");
                }

                clone.SetActive(true);
                level46Created = true;
                Plugin.Logger.LogInfo(
                    "[Night Roof Adventure] Level 46 card inserted " +
                    "directly after Level 45."
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Night Roof Adventure] Level 46 card failed: " +
                    exception
                );
            }
            finally
            {
                creatingLevel46 = false;
            }
        }

        internal static void ObserveButton(
            StarAdvantureLevelButton button
        )
        {
            if (button == null || buttonHierarchyLogged)
                return;

            buttonHierarchyLogged = true;
            Plugin.Logger.LogInfo(
                "[Night Roof Index] Real adventure level button detected" +
                " | Initial level = " + button.level +
                " | Hierarchy = " + DescribeHierarchy(button.transform)
            );
        }

        internal static void ObservePage(Page page, string source)
        {
            if (page == null)
                return;

            try
            {
                StarAdvantureLevelButton[] buttons =
                    page.GetComponentsInChildren<
                        StarAdvantureLevelButton
                    >(true);
                int minimum = int.MaxValue;
                int maximum = int.MinValue;
                int active = 0;
                for (int index = 0; index < buttons.Length; index++)
                {
                    StarAdvantureLevelButton button = buttons[index];
                    if (button == null)
                        continue;
                    minimum = Math.Min(minimum, button.level);
                    maximum = Math.Max(maximum, button.level);
                    if (button.gameObject.activeInHierarchy)
                        active++;
                }

                Plugin.Logger.LogInfo(
                    "[Night Roof Index] Page inspection" +
                    " | Source = " + source +
                    " | Buttons = " + buttons.Length +
                    " | Active = " + active +
                    " | Range = " +
                    (minimum == int.MaxValue ? -1 : minimum) + "-" +
                    (maximum == int.MinValue ? -1 : maximum) +
                    " | Children = " + page.transform.childCount +
                    " | Hierarchy = " + DescribeHierarchy(page.transform)
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Night Roof Index] Page inspection failed: " +
                    exception.Message
                );
            }
        }

        private static string DescribeHierarchy(Transform start)
        {
            string result = string.Empty;
            Transform? current = start;
            int depth = 0;
            while (current != null && depth < 8)
            {
                if (depth > 0)
                    result += " <- ";
                result += current.gameObject.name + "{";
                Component[] components =
                    current.gameObject.GetComponents<Component>();
                for (int index = 0; index < components.Length; index++)
                {
                    if (index > 0)
                        result += ",";
                    result += components[index] == null
                        ? "null"
                        : components[index].GetType().Name;
                }
                result += "}";
                current = current.parent;
                depth++;
            }
            return result;
        }
    }


    public sealed class NightRoofAdventureMenuWatcher : MonoBehaviour
    {
        private float nextCheck;
        private bool firstTickLogged;

        public NightRoofAdventureMenuWatcher(IntPtr pointer) : base(pointer) { }

        public void Update()
        {
            if (!firstTickLogged)
            {
                firstTickLogged = true;
                Plugin.Logger.LogInfo(
                    "[Night Roof Adventure] Menu watcher is ticking."
                );
            }
            if (Time.unscaledTime < nextCheck)
                return;
            nextCheck = Time.unscaledTime + 0.25f;

            AdvantureMenu menu =
                UnityEngine.Object.FindObjectOfType<AdvantureMenu>();
            if (menu != null)
            {
                NightRoofAdventure.ExposeLevel46(menu);
                StarAdvantureLevelButton[] buttons =
                    menu.GetComponentsInChildren<
                        StarAdvantureLevelButton
                    >(true);
                for (int index = 0; index < buttons.Length; index++)
                {
                    if (buttons[index] != null && buttons[index].level == 45)
                    {
                        NightRoofAdventureIndexInspector.EnsureLevel46(
                            buttons[index]
                        );
                        break;
                    }
                }
            }
        }
    }
}
#endif
