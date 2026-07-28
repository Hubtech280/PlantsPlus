using CustomizeLib.MelonLoader;
using HarmonyLib;
using Il2Cpp;
using System;
using System.IO;
using System.Reflection;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlantsPlus.Core
{
    internal static class NightRoofCards
    {
        private const string CardResourceName =
            "PlantsPlus.Resources.Sprites.night_roof_card.png";

        private static bool registered;
        private static Sprite? cardSprite;
        private static Texture2D? cardTexture;
        private static Transform? normalCardContainer;
        private static CardUI? normalCardInstance;
        private static CardUI? carbonCopyInstance;
        private static bool? lastSelectedBase;
        private static bool? lastSelectedCarbon;
        private static SeedLibrary? configuredSandboxLibrary;
        private static Transform? sandboxElectronionContainer;
        private static IZBottomMenu? configuredIZMenu;
        private static bool sandboxMenuMissingLogged;
        private static bool sandboxPageMissingLogged;
        private static int lastLimitedLevelLogged = int.MinValue;
        private static SeedLibrary? limitedLevelConfiguredLibrary;
        private static SeedLibrary? customizeLibFinishedLibrary;
        private static SeedLibrary? repairedNormalLibrary;
        private static Transform? evolutionWarElectronionContainer;
        private static float nextNormalRepairAttempt;

        internal static void OnStart()
        {
            if (registered)
                return;

            registered = true;

            try
            {
                EnsureCardSprite();
                InstallNativeClassification();

                // CustomizeLib appends normal cards immediately after the
                // native AdventureCardLayout. Frozen Giftbox is the last
                // native entry, so Electronion becomes the first card on
                // the following line/page instead of a Unique Plant.
                // CustomizeLib adds one to repeatTime on normal boards.
                // Passing its default value (1) therefore creates two
                // complete pairs: two regular cards and two Carbon Copies.
                // Zero is the value that creates exactly the native pair.
                CustomCore.RegisterCustomNormalCard(
                    PlantType.ElectricOnion,
                    0
                );

                Plugin.Logger.LogInfo(
                    "[Night Roof] Electronion registered as a normal " +
                    "Adventure plant after Frozen Giftbox."
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Night Roof] Adventure-card registration failed " +
                    "safely: " + exception
                );
            }
        }

        private static void InstallNativeClassification()
        {
            // These are the two native classifiers used throughout the
            // game, including Almanac filters. Electronion starts in the
            // special/colourful set; registering a CustomizeLib card alone
            // does not change that native classification.
            if (CoreEnums.baiscPlants != null &&
                !CoreEnums.baiscPlants.Contains(
                    PlantType.ElectricOnion
                ))
            {
                CoreEnums.baiscPlants.Add(PlantType.ElectricOnion);
            }

            if (TypeData.SpecialCardPlants != null)
            {
                TypeData.SpecialCardPlants.Remove(
                    PlantType.ElectricOnion
                );
            }

            Plugin.Logger.LogInfo(
                "[Night Roof] Native classification repaired" +
                " | Basic = " +
                Lawnf.IsBasicPlant(PlantType.ElectricOnion) +
                " | Unique = " +
                (
                    TypeData.SpecialCardPlants != null &&
                    TypeData.SpecialCardPlants.Contains(
                        PlantType.ElectricOnion
                    )
                )
            );
        }

        internal static void ApplyCardSkin(CardUI card)
        {
            // Every playable Electronion card uses the Night Roof
            // background. The sandbox is the sole exception: both its
            // zero-cost LibraryCard and the selected card-bank copy keep the
            // native sandbox style.
            if (card == null ||
                card.thePlantType != PlantType.ElectricOnion ||
                IsSandboxContext())
            {
                return;
            }

            SeedLibrary library = SeedLibrary.Instance;
            if (library != null &&
                IsLimitedChallengeSelection(library))
            {
                // Restricted levels only need a normal native seed packet
                // that is present but unavailable. The Night Roof Adventure
                // artwork is intentionally not used here.
                return;
            }

            try
            {
                EnsureCardSprite();

                Image? background = card.GetComponent<Image>();
                if (background == null || cardSprite == null)
                    return;

                // Only replace the sprite. In particular, keep Image.color:
                // the native Carbon Copy tint must remain visible.
                background.sprite = cardSprite;
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Night Roof] Electronion seed background could not " +
                    "be applied: " + exception.Message
                );
            }
        }

        internal static void RefreshSelectionCards(SeedLibrary library)
        {
            if (library == null)
                return;

            CardUI[] cards =
                library.GetComponentsInChildren<CardUI>(true);

            for (int index = 0; index < cards.Length; index++)
            {
                ApplyCardSkin(cards[index]);
            }

            RefreshLimitedLevelAvailability(library);
        }

        internal static void RefreshLimitedLevelAvailability(
            SeedLibrary? library = null
        )
        {
            library ??= SeedLibrary.Instance;

            if (library == null ||
                IsSandboxPlantLibrary(library) ||
                limitedLevelConfiguredLibrary == library ||
                !IsLimitedChallengeSelection(library))
            {
                return;
            }

            CardUI[] cards =
                library.GetComponentsInChildren<CardUI>(true);
            int blocked = 0;

            for (int index = 0; index < cards.Length; index++)
            {
                CardUI card = cards[index];
                if (card == null ||
                    card.thePlantType != PlantType.ElectricOnion)
                {
                    continue;
                }

                BlockLimitedLevelCard(card);
                blocked++;
            }

            if (blocked > 0)
                limitedLevelConfiguredLibrary = library;

            if (blocked > 0 &&
                lastLimitedLevelLogged != GameAPP.theBoardLevel)
            {
                lastLimitedLevelLogged = GameAPP.theBoardLevel;
                Plugin.Logger.LogInfo(
                    "[Night Roof] Electronion kept visible but locked in " +
                    "limited challenge level " + GameAPP.theBoardLevel +
                    " | Cards blocked = " + blocked
                );
            }
        }

        internal static bool ShouldBlockLimitedLevelClick(CardUI card)
        {
            if (card == null ||
                card.thePlantType != PlantType.ElectricOnion ||
                IsSandboxCard(card))
            {
                return false;
            }

            SeedLibrary library = SeedLibrary.Instance;
            return library != null &&
                (
                    limitedLevelConfiguredLibrary == library ||
                    IsLimitedChallengeSelection(library)
                );
        }

        internal static void EnforceLimitedLevelAvailability(CardUI card)
        {
            if (card == null ||
                card.thePlantType != PlantType.ElectricOnion ||
                IsSandboxContext())
            {
                return;
            }

            SeedLibrary library = SeedLibrary.Instance;
            if (library != null &&
                IsLimitedChallengeSelection(library))
            {
                BlockLimitedLevelCard(card);
            }
        }

        private static void BlockLimitedLevelCard(CardUI card)
        {
            card.isAvailable = false;
            card.disabled = true;

            // Native CardUI uses child 3 as the unavailable overlay.
            // Activating the same child preserves the level's normal visual
            // language instead of hiding Electronion from the grid.
            if (card.transform != null &&
                card.transform.childCount > 3)
            {
                Transform overlay = card.transform.GetChild(3);
                if (overlay != null)
                    overlay.gameObject.SetActive(true);
            }
        }

        private static bool IsLimitedChallengeSelection(
            SeedLibrary library
        )
        {
            if (library == null || IsSandboxContext())
            {
                return false;
            }

            // Evolution War rebuilds its selection library before the board
            // tag is finalized. The level ID is already available here and
            // is therefore the reliable signal on the seed-selection screen.
            if (GameAPP.theBoardLevel == (int)ChallengeLevel.EvolutionWar)
                return true;

            Board board = Board.Instance;

            // The Gods: Evolved uses the native evolution-war tag instead of
            // the generic Challenge board type. Keep Electronion visible in
            // these restricted selections, but do not make it selectable.
            if (board != null && board.boardTag.evolutionWar)
                return true;

            // No Plants+ challenge explicitly authorizes Electronion yet.
            // Individual level IDs can be whitelisted here later.
            return GameAPP.theBoardType == LevelType.Challenge;
        }

        internal static bool EnsureSandboxElectronion(
            SeedLibrary library
        )
        {
            if (library == null ||
                !IsSandboxPlantLibrary(library))
            {
                return false;
            }

            if (configuredSandboxLibrary == library &&
                sandboxElectronionContainer != null)
            {
                return true;
            }

            try
            {
                Transform? normalCards =
                    FindCardsContainer(library, "NormalCards");

                if (normalCards == null)
                    return false;

                // Frozen Giftbox (SnowPresent) is the 54th and final native
                // Adventure card. Clone its already configured LibraryCard
                // grid item so Electronion inherits the sandbox's native
                // click handling and zero-cost presentation.
                Transform? frozenGiftbox =
                    FindDirectCardContainer(
                        normalCards,
                        PlantType.SnowPresent
                    );

                if (frozenGiftbox == null ||
                    frozenGiftbox.parent == null)
                {
                    Plugin.Logger.LogWarning(
                        "[Sandbox] Frozen Giftbox card was not found; " +
                        "Electronion was not inserted."
                    );
                    return false;
                }

                // CustomizeLib may already have appended its normal
                // Electronion pair to another page. Remove every sandbox
                // copy first; this menu needs one zero-cost LibraryCard,
                // not the Adventure/Carbon-Copy pair.
                int removedContainers = 0;

                for (int pageIndex = 0;
                     pageIndex < normalCards.childCount;
                     pageIndex++)
                {
                    Transform page = normalCards.GetChild(pageIndex);
                    if (page == null)
                        continue;

                    for (int cardIndex = page.childCount - 1;
                         cardIndex >= 0;
                         cardIndex--)
                    {
                        Transform candidate = page.GetChild(cardIndex);
                        if (candidate == null ||
                            !ContainsPlant(
                                candidate,
                                PlantType.ElectricOnion
                            ))
                        {
                            continue;
                        }

                        candidate.gameObject.SetActive(false);
                        UnityEngine.Object.Destroy(candidate.gameObject);
                        removedContainers++;
                    }
                }

                GameObject clone = UnityEngine.Object.Instantiate(
                    frozenGiftbox.gameObject,
                    frozenGiftbox.parent
                );
                clone.name = "PlantsPlus_Electronion_SandboxCard";
                clone.transform.SetSiblingIndex(
                    frozenGiftbox.GetSiblingIndex() + 1
                );

                CardUI? card =
                    clone.GetComponentInChildren<CardUI>(true);

                if (card == null)
                {
                    UnityEngine.Object.Destroy(clone);
                    Plugin.Logger.LogWarning(
                        "[Sandbox] Cloned LibraryCard had no CardUI; " +
                        "Electronion was not inserted."
                    );
                    return false;
                }

                card.thePlantType = PlantType.ElectricOnion;
                card.theSeedType = (int)PlantType.ElectricOnion;
                card.theSeedCost = 0;
                card.fullCD =
                    PlantDataManager.PlantData_Default[
                        PlantType.ElectricOnion
                    ].cd;
                card.CD = card.fullCD;
                card.parent = clone;
                card.isExtra = false;
                card.ChangeCardSprite();

                RectTransform? pageRect =
                    frozenGiftbox.parent as RectTransform;
                if (pageRect != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(pageRect);
                }

                configuredSandboxLibrary = library;
                sandboxElectronionContainer = clone.transform;

                Plugin.Logger.LogInfo(
                    "[Sandbox] Electronion inserted after Frozen Giftbox " +
                    "as Adventure card 55 | Cost = 0" +
                    " | Previous custom containers removed = " +
                    removedContainers
                );
                return true;
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Sandbox] Electronion insertion failed safely: " +
                    exception
                );
                return false;
            }
        }

        internal static bool EnsureSandboxElectronion(
            IZBottomMenu menu
        )
        {
            if (menu == null || menu.plantLibrary == null)
                return false;

            if (configuredIZMenu == menu &&
                sandboxElectronionContainer != null)
            {
                return true;
            }

            try
            {
                Transform? main =
                    menu.plantLibrary.transform.FindChild("Grid/Main");
                Transform? firstPage =
                    main != null
                        ? main.FindChild("Page1")
                        : null;

                Transform? frozenGiftbox = null;

                if (firstPage != null)
                {
                    frozenGiftbox = FindDirectCardContainer(
                        firstPage,
                        PlantType.SnowPresent
                    );
                }

                // The page itself is named PlantCardPage_1 in this build,
                // not Page1. Search inside Grid/Main first so that the
                // similarly shaped CustomizeLib and All Plants pages cannot
                // be mistaken for the native Adventure page.
                if (firstPage == null || frozenGiftbox == null)
                {
                    Transform searchRoot =
                        main != null
                            ? main
                            : menu.plantLibrary.transform;

                    CardUI[] allCards =
                        searchRoot
                            .GetComponentsInChildren<CardUI>(true);

                    int bestDistance = int.MaxValue;

                    for (int index = 0;
                         index < allCards.Length;
                         index++)
                    {
                        CardUI candidateCard = allCards[index];
                        if (candidateCard == null ||
                            candidateCard.thePlantType !=
                            PlantType.SnowPresent)
                        {
                            continue;
                        }

                        Transform current = candidateCard.transform;
                        Transform libraryRoot = searchRoot;

                        while (current != null &&
                               current.parent != null &&
                               current != libraryRoot)
                        {
                            Transform candidatePage = current.parent;
                            int directCardCount =
                                CountDirectCardContainers(candidatePage);

                            // The native Adventure grid has 54 cards and
                            // contains the basic starters. Prefer the closest
                            // matching grid if a translated build adds one or
                            // two native entries.
                            if (directCardCount >= 50 &&
                                directCardCount <= 56 &&
                                FindDirectCardContainer(
                                    candidatePage,
                                    PlantType.Peashooter
                                ) != null &&
                                FindDirectCardContainer(
                                    candidatePage,
                                    PlantType.SunFlower
                                ) != null)
                            {
                                int distance =
                                    Math.Abs(directCardCount - 54);

                                if (distance < bestDistance)
                                {
                                    bestDistance = distance;
                                    firstPage = candidatePage;
                                    frozenGiftbox = current;

                                    if (main == null)
                                        main = candidatePage.parent;
                                }
                            }

                            current = candidatePage;
                        }
                    }
                }

                if (firstPage == null || frozenGiftbox == null)
                {
                    if (!sandboxPageMissingLogged)
                    {
                        sandboxPageMissingLogged = true;
                        Plugin.Logger.LogWarning(
                            "[Sandbox] Adventure page not ready yet; " +
                            "waiting for the Frozen Giftbox grid."
                        );
                    }
                    return false;
                }

                int removedCards = 0;

                Transform pagesRoot =
                    main != null ? main : firstPage;

                int pageCount =
                    pagesRoot == firstPage
                        ? 1
                        : pagesRoot.childCount;

                for (int pageIndex = 0;
                     pageIndex < pageCount;
                     pageIndex++)
                {
                    Transform page =
                        pagesRoot == firstPage
                            ? firstPage
                            : pagesRoot.GetChild(pageIndex);
                    if (page == null)
                        continue;

                    for (int cardIndex = page.childCount - 1;
                         cardIndex >= 0;
                         cardIndex--)
                    {
                        Transform candidate = page.GetChild(cardIndex);
                        if (candidate == null)
                            continue;

                        CardUI? candidateCard =
                            candidate.GetComponentInChildren<CardUI>(true);

                        if (candidateCard == null ||
                            candidateCard.thePlantType !=
                            PlantType.ElectricOnion)
                        {
                            continue;
                        }

                        candidate.gameObject.SetActive(false);
                        UnityEngine.Object.Destroy(candidate.gameObject);
                        removedCards++;
                    }
                }

                GameObject clone = UnityEngine.Object.Instantiate(
                    frozenGiftbox.gameObject,
                    firstPage
                );
                clone.name = PlantType.ElectricOnion.ToString();
                clone.transform.SetSiblingIndex(
                    frozenGiftbox.GetSiblingIndex() + 1
                );
                clone.SetActive(true);

                CardUI? card =
                    clone.GetComponentInChildren<CardUI>(true);
                if (card == null)
                {
                    UnityEngine.Object.Destroy(clone);
                    return false;
                }

                SpriteRenderer? previewRenderer =
                    GameAPP.resourcesManager
                        .plantPreviews[PlantType.ElectricOnion]
                        .GetComponent<SpriteRenderer>();
                Image? previewImage =
                    card.transform.childCount > 0
                        ? card.transform
                            .GetChild(0)
                            .GetComponent<Image>()
                        : null;

                if (previewRenderer != null &&
                    previewImage != null)
                {
                    previewImage.sprite = previewRenderer.sprite;
                    previewImage.SetNativeSize();
                }

                Mouse.Instance.ChangeCardSprite(
                    PlantType.ElectricOnion,
                    card
                );

                BoxCollider2D? collider =
                    card.GetComponent<BoxCollider2D>();
                if (collider != null)
                    collider.enabled = true;

                card.gameObject.SetActive(true);
                card.thePlantType = PlantType.ElectricOnion;
                card.theSeedType = (int)PlantType.ElectricOnion;
                card.theSeedCost = 0;
                card.fullCD = 0f;
                card.CD = 0f;
                card.parent = clone;
                card.isExtra = false;

                if (card.transform.childCount > 1)
                {
                    TextMeshProUGUI? costText =
                        card.transform
                            .GetChild(1)
                            .GetComponent<TextMeshProUGUI>();

                    if (costText != null)
                        costText.text = "0";
                }

                RectTransform? pageRect =
                    firstPage as RectTransform;
                if (pageRect != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(pageRect);
                }

                configuredIZMenu = menu;
                sandboxElectronionContainer = clone.transform;
                sandboxPageMissingLogged = false;

                Plugin.Logger.LogInfo(
                    "[Sandbox] Electronion inserted in " +
                    firstPage.name +
                    " immediately after SnowPresent" +
                    " | Sibling index = " +
                    clone.transform.GetSiblingIndex() +
                    " | Cards on page = " + firstPage.childCount +
                    " | Adventure starters verified = true" +
                    " | Previous copies removed = " + removedCards +
                    " | Cost = 0"
                );
                return true;
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Sandbox] Direct IZ Electronion insertion failed: " +
                    exception
                );
                return false;
            }
        }

        private static int CountDirectCardContainers(Transform page)
        {
            int count = 0;

            for (int index = 0; index < page.childCount; index++)
            {
                Transform candidate = page.GetChild(index);
                if (candidate != null &&
                    candidate.GetComponentInChildren<CardUI>(true) != null)
                {
                    count++;
                }
            }

            return count;
        }

        internal static void TryEnsureSandboxElectronion()
        {
            Board board = Board.Instance;
            if (board == null || !board.boardTag.isIZ)
                return;

            IZBottomMenu menu = IZBottomMenu.Instance;
            if (menu == null || menu.plantLibrary == null)
            {
                if (!sandboxMenuMissingLogged)
                {
                    sandboxMenuMissingLogged = true;
                    Plugin.Logger.LogWarning(
                        "[Sandbox] IZ plant library not ready yet; " +
                        "Electronion insertion will retry."
                    );
                }

                return;
            }

            sandboxMenuMissingLogged = false;
            EnsureSandboxElectronion(menu);
        }

        internal static void RepairCardsAfterCustomizeLibCreation()
        {
            SeedLibrary library = SeedLibrary.Instance;

            if (library == null ||
                library.cardPagesContainer == null ||
                IsSandboxPlantLibrary(library))
            {
                return;
            }

            if (Time.unscaledTime < nextNormalRepairAttempt)
                return;

            nextNormalRepairAttempt = Time.unscaledTime + 0.25f;

            try
            {
                if (repairedNormalLibrary != library)
                {
                    normalCardContainer = null;
                    normalCardInstance = null;
                    carbonCopyInstance = null;
                    evolutionWarElectronionContainer = null;
                    limitedLevelConfiguredLibrary = null;
                }

                Transform? normalCards =
                    FindNormalCardsRoot(library);

                if (normalCards == null)
                {
                    Plugin.Logger.LogWarning(
                        "[Night Roof] NormalCards container was not found."
                    );
                    return;
                }

                // Restricted challenge libraries use a different card
                // hierarchy from Adventure. Always route them through the
                // dedicated single-card path before the generic normal-pair
                // repair can see the newly created card and run again.
                if (IsLimitedChallengeSelection(library))
                {
                    // PatchMgr.ShowCards rebuilds this hierarchy after a
                    // delay. Creating our replacement before that point
                    // leaves it on a page which CustomizeLib then destroys.
                    if (customizeLibFinishedLibrary != library)
                        return;

                    EnsureEvolutionWarElectronion(
                        library,
                        normalCards
                    );
                    return;
                }

                Transform? electronionContainer =
                    FindDirectCardContainer(
                        normalCards,
                        PlantType.ElectricOnion
                    );

                // Some special levels rebuild the card pages while retaining
                // the same SeedLibrary instance. Only trust the cache when
                // the current live container is still the complete native
                // pair and is still located on NormalCards page 2.
                if (repairedNormalLibrary == library &&
                    electronionContainer == normalCardContainer &&
                    IsHealthyNormalPair(
                        electronionContainer,
                        normalCards
                    ))
                {
                    return;
                }

                // The same SeedLibrary can survive while Evolution War
                // rebuilds all of its page objects. Its previous availability
                // cache no longer describes the newly created cards.
                limitedLevelConfiguredLibrary = null;

                if (electronionContainer == null)
                {
                    Plugin.Logger.LogWarning(
                        "[Night Roof] CustomizeLib finished, but the " +
                        "Electronion normal-card container was not found."
                    );
                    return;
                }

                int removedTemplateCards =
                    NormalizeNormalCardContainer(
                        electronionContainer
                    );

                Transform secondPage =
                    library.LateCreateCardPage("NormalCards");

                if (secondPage == null)
                {
                    Plugin.Logger.LogWarning(
                        "[Night Roof] Native NormalCards page 2 could not " +
                        "be created."
                    );
                    return;
                }

                bool moved = electronionContainer.parent != secondPage;
                if (moved)
                {
                    // Move the whole grid item. Moving its internal CardUI
                    // children separately destroys the normal-card layout.
                    electronionContainer.SetParent(secondPage, false);
                    electronionContainer.SetAsLastSibling();
                }

                CardUI[] cards =
                    electronionContainer.GetComponentsInChildren<CardUI>(
                        true
                    );
                int electronionCards = 0;

                for (int index = 0; index < cards.Length; index++)
                {
                    ApplyCardSkin(cards[index]);
                    if (cards[index] != null &&
                        cards[index].thePlantType ==
                        PlantType.ElectricOnion)
                    {
                        electronionCards++;
                    }
                }

                RectTransform? secondPageRect =
                    secondPage as RectTransform;
                if (secondPageRect != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(
                        secondPageRect
                    );
                }

                Plugin.Logger.LogInfo(
                    "[Night Roof] Electronion normal-card pair repaired" +
                    " | Electronion cards = " + electronionCards +
                    " | Peashooter/template cards removed = " +
                    removedTemplateCards +
                    " | Whole container moved to page 2 = " + moved
                );

                int hiddenUniqueCards =
                    HideElectronionFromUniqueSelection(library);

                Plugin.Logger.LogInfo(
                    "[Night Roof] Native Unique selection cleaned" +
                    " | Hidden Electronion containers = " +
                    hiddenUniqueCards
                );

                repairedNormalLibrary = library;
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Night Roof] NormalCards pagination repair failed " +
                    "safely: " + exception
                );
            }
        }

        internal static bool SuppressLateCustomizeLibElectronionCard()
        {
            SeedLibrary library = SeedLibrary.Instance;
            if (library == null ||
                !IsLimitedChallengeSelection(library))
            {
                return false;
            }

            bool removed =
                CustomCore.CustomNormalCards.Remove(
                    PlantType.ElectricOnion
                );

            if (removed)
            {
                Plugin.Logger.LogInfo(
                    "[Night Roof] Suppressed CustomizeLib's incompatible " +
                    "Electronion normal-card clone for this restricted " +
                    "selection."
                );
            }

            return removed;
        }

        internal static void FinishCustomizeLibCardCreation(
            bool electronionWasSuppressed
        )
        {
            if (electronionWasSuppressed)
            {
                // Restore the global registration for Adventure and every
                // ordinary level. Only this one ShowCards invocation was
                // filtered.
                CustomCore.RegisterCustomNormalCard(
                    PlantType.ElectricOnion,
                    0
                );
            }

            SeedLibrary library = SeedLibrary.Instance;
            if (library != null &&
                IsLimitedChallengeSelection(library))
            {
                customizeLibFinishedLibrary = library;
                nextNormalRepairAttempt = 0f;
            }

            RepairCardsAfterCustomizeLibCreation();
        }

        private static bool EnsureEvolutionWarElectronion(
            SeedLibrary library,
            Transform normalCards
        )
        {
            if (library == null || normalCards == null)
                return false;

            if (evolutionWarElectronionContainer != null &&
                evolutionWarElectronionContainer.parent != null)
            {
                CardUI[] existingCards =
                    evolutionWarElectronionContainer
                        .GetComponentsInChildren<CardUI>(true);

                for (int index = 0;
                     index < existingCards.Length;
                     index++)
                {
                    CardUI existing = existingCards[index];
                    if (existing != null &&
                        existing.thePlantType ==
                        PlantType.ElectricOnion)
                    {
                        BlockLimitedLevelCard(existing);
                    }
                }

                repairedNormalLibrary = library;
                return true;
            }

            Transform? template =
                FindDirectCardContainer(
                    normalCards,
                    PlantType.SnowPresent
                );

            if (template == null)
            {
                template = FindDirectCardContainer(
                    normalCards,
                    PlantType.Peashooter
                );
            }

            if (template == null)
            {
                Plugin.Logger.LogWarning(
                    "[Night Roof] Limited-level native card template " +
                    "was not ready."
                );
                return false;
            }

            Transform firstPage = template.parent;
            if (firstPage == null)
            {
                Plugin.Logger.LogWarning(
                    "[Night Roof] Limited-level live page containing the " +
                    "native template was not found."
                );
                return false;
            }

            // Remove leftovers made by older Plants+ hotfixes. They have a
            // unique name, so native and CustomizeLib cards are untouched.
            int removedPlantsPlusClones = 0;
            for (int pageIndex = 0;
                 pageIndex < normalCards.childCount;
                 pageIndex++)
            {
                Transform page = normalCards.GetChild(pageIndex);
                if (page == null)
                    continue;

                for (int childIndex = page.childCount - 1;
                     childIndex >= 0;
                     childIndex--)
                {
                    Transform candidate = page.GetChild(childIndex);
                    if (candidate == null ||
                        !candidate.name.StartsWith(
                            "PlantsPlus_Electronion_",
                            StringComparison.Ordinal
                        ))
                    {
                        continue;
                    }

                    candidate.gameObject.SetActive(false);
                    UnityEngine.Object.Destroy(candidate.gameObject);
                    removedPlantsPlusClones++;
                }
            }

            // CustomizeLib's normal-card clone is not parented like the
            // native cards in Evolution War. Hide every copy it produced
            // before creating one clean native grid item below.
            CardUI[] previousCards =
                library.cardPagesContainer
                    .GetComponentsInChildren<CardUI>(true);
            int hiddenBrokenCards = 0;

            for (int index = 0;
                 index < previousCards.Length;
                 index++)
            {
                CardUI previous = previousCards[index];
                if (previous == null ||
                    previous.thePlantType !=
                    PlantType.ElectricOnion)
                {
                    continue;
                }

                previous.gameObject.SetActive(false);
                hiddenBrokenCards++;
            }

            CardUI[] templateCards =
                template.GetComponentsInChildren<CardUI>(true);
            CardUI? templateCard = null;

            for (int index = 0;
                 index < templateCards.Length;
                 index++)
            {
                CardUI candidate = templateCards[index];
                if (candidate != null &&
                    candidate.gameObject.activeSelf)
                {
                    templateCard = candidate;
                    break;
                }
            }

            if (templateCard == null && templateCards.Length > 0)
                templateCard = templateCards[0];

            if (templateCard == null)
            {
                Plugin.Logger.LogWarning(
                    "[Night Roof] Limited-level native template had no " +
                    "usable CardUI."
                );
                return false;
            }

            // LateCreateCardPage rebuilds the grid it paginates. Create the
            // second page first; a custom card instantiated before this call
            // is destroyed with the old grid and gets recreated every frame.
            Transform? secondPage =
                library.LateCreateCardPage("NormalCards");

            if (secondPage == null)
            {
                Plugin.Logger.LogWarning(
                    "[Night Roof] Limited-level second page could not be " +
                    "created."
                );
                return false;
            }

            // Clone the complete native grid item. The restricted challenge
            // menu requires the layout components and anchors carried by
            // this container; a hand-made empty RectTransform exists in the
            // hierarchy but is never rendered by its page.
            GameObject cardContainer =
                UnityEngine.Object.Instantiate(
                    template.gameObject,
                    secondPage
                );
            cardContainer.name =
                "PlantsPlus_Electronion_EvolutionWarContainer";
            cardContainer.SetActive(true);
            cardContainer.transform.SetAsLastSibling();

            CardUI[] clonedCards =
                cardContainer.GetComponentsInChildren<CardUI>(true);
            CardUI? card = null;

            for (int index = 0;
                 index < clonedCards.Length;
                 index++)
            {
                CardUI candidate = clonedCards[index];
                if (candidate == null)
                    continue;

                if (card == null && candidate.gameObject.activeSelf)
                {
                    card = candidate;
                    continue;
                }

                // This level needs one locked seed packet, not a Carbon Copy
                // or another template packet.
                candidate.gameObject.SetActive(false);
            }

            if (card == null && clonedCards.Length > 0)
            {
                card = clonedCards[0];
                card.gameObject.SetActive(true);
            }

            if (card == null)
            {
                UnityEngine.Object.Destroy(cardContainer);
                Plugin.Logger.LogWarning(
                    "[Night Roof] Limited-level native grid clone had no " +
                    "CardUI."
                );
                return false;
            }

            card.gameObject.name =
                "PlantsPlus_Electronion_EvolutionWarCardUI";

            var electronionData =
                PlantDataManager.PlantData_Default[
                    PlantType.ElectricOnion
                ];

            card.gameObject.SetActive(true);
            card.thePlantType = PlantType.ElectricOnion;
            card.theSeedType = (int)PlantType.ElectricOnion;
            card.theSeedCost = electronionData.cost;
            card.fullCD = electronionData.cd;
            card.CD = card.fullCD;
            card.parent = cardContainer;
            card.isExtra = false;

            // Refresh only after the type fields are correct. Doing this
            // while the clone still identifies as Peashooter restores the
            // Peashooter packet above the Electronion preview.
            Mouse.Instance.ChangeCardSprite(
                PlantType.ElectricOnion,
                card
            );
            card.ChangeCardSprite();

            SpriteRenderer? previewRenderer =
                GameAPP.resourcesManager
                    .plantPreviews[PlantType.ElectricOnion]
                    .GetComponent<SpriteRenderer>();
            Image? cardPreviewImage =
                card.transform.childCount > 0
                    ? card.transform
                        .GetChild(0)
                        .GetComponent<Image>()
                    : null;

            if (previewRenderer != null &&
                cardPreviewImage != null)
            {
                cardPreviewImage.sprite = previewRenderer.sprite;
            }

            if (card.transform.childCount > 1)
            {
                TextMeshProUGUI? costText =
                    card.transform
                        .GetChild(1)
                        .GetComponent<TextMeshProUGUI>();

                if (costText != null)
                    costText.text = electronionData.cost.ToString();
            }

            // Normal-card grid items display a separate shared preview and
            // cost in container child 0. Updating only CardUI changes the
            // logical plant while leaving the visible Peashooter artwork and
            // its 100-Sun label untouched.
            if (cardContainer.transform.childCount > 0)
            {
                Transform visibleHeader =
                    cardContainer.transform.GetChild(0);

                if (visibleHeader != null &&
                    visibleHeader.childCount > 0 &&
                    previewRenderer != null)
                {
                    Image? visiblePreview =
                        visibleHeader
                            .GetChild(0)
                            .GetComponent<Image>();

                    if (visiblePreview != null)
                    {
                        visiblePreview.sprite = previewRenderer.sprite;
                        visiblePreview.SetNativeSize();

                        RectTransform? visibleRect =
                            visiblePreview.rectTransform;
                        RectTransform? cardPreviewRect =
                            cardPreviewImage != null
                                ? cardPreviewImage.rectTransform
                                : null;

                        if (visibleRect != null &&
                            cardPreviewRect != null)
                        {
                            visibleRect.localScale =
                                cardPreviewRect.localScale;
                            visibleRect.sizeDelta =
                                cardPreviewRect.sizeDelta;
                        }
                    }
                }

                if (visibleHeader != null &&
                    visibleHeader.childCount > 1)
                {
                    TextMeshProUGUI? visibleCost =
                        visibleHeader
                            .GetChild(1)
                            .GetComponent<TextMeshProUGUI>();

                    if (visibleCost != null)
                    {
                        visibleCost.text =
                            electronionData.cost.ToString();
                    }
                }
            }

            ApplyCardSkin(card);
            BlockLimitedLevelCard(card);

            RectTransform? secondPageRect =
                secondPage as RectTransform;
            if (secondPageRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(
                    secondPageRect
                );
            }

            evolutionWarElectronionContainer =
                cardContainer.transform;
            normalCardContainer = cardContainer.transform;
            normalCardInstance = card;
            carbonCopyInstance = null;
            limitedLevelConfiguredLibrary = library;
            repairedNormalLibrary = library;

            Plugin.Logger.LogInfo(
                "[Night Roof] Limited-level Electronion rebuilt as " +
                "one locked native card on NormalCards page 2" +
                " | Board level = " + GameAPP.theBoardLevel +
                " | Board type = " + GameAPP.theBoardType +
                " | Broken cards hidden = " + hiddenBrokenCards +
                " | Old Plants+ clones removed = " +
                removedPlantsPlusClones +
                " | Cloned object = complete native grid item" +
                " | Source page = " + firstPage.name +
                "#" + firstPage.GetInstanceID() +
                " | Result page = " + secondPage.name +
                "#" + secondPage.GetInstanceID() +
                " | Same page = " + (firstPage == secondPage) +
                " | Result sibling = " +
                cardContainer.transform.GetSiblingIndex() +
                " | Separate parent = " +
                (card.parent == cardContainer) +
                " | Visible header refreshed = true" +
                " | Cached after pagination = true"
            );
            return true;
        }

        private static Transform? FindNormalCardsRoot(
            SeedLibrary library
        )
        {
            if (library == null || library.cardPagesContainer == null)
                return null;

            for (int index = 0;
                 index < library.cardPagesContainer.childCount;
                 index++)
            {
                Transform child =
                    library.cardPagesContainer.GetChild(index);

                if (child != null &&
                    child.name.Equals(
                        "NormalCards",
                        StringComparison.Ordinal
                    ))
                {
                    return child;
                }
            }

            return null;
        }

        private static bool IsHealthyNormalPair(
            Transform? cardContainer,
            Transform normalCards
        )
        {
            if (cardContainer == null ||
                cardContainer.parent == null ||
                cardContainer.parent.parent != normalCards ||
                cardContainer.parent.GetSiblingIndex() != 1)
            {
                return false;
            }

            CardUI[] cards =
                cardContainer.GetComponentsInChildren<CardUI>(true);
            int electronionCards = 0;

            for (int index = 0; index < cards.Length; index++)
            {
                CardUI card = cards[index];
                if (card == null)
                    continue;

                if (card.thePlantType != PlantType.ElectricOnion)
                    return false;

                electronionCards++;
            }

            return electronionCards == 2;
        }

        private static int NormalizeNormalCardContainer(
            Transform cardContainer
        )
        {
            CardUI[] allCards =
                cardContainer.GetComponentsInChildren<CardUI>(true);
            CardUI? normalCard = null;
            CardUI? carbonCopy = null;
            int removed = 0;

            for (int index = 0; index < allCards.Length; index++)
            {
                CardUI card = allCards[index];
                if (card == null)
                    continue;

                if (card.thePlantType != PlantType.ElectricOnion)
                {
                    // CustomizeLib clones the complete Peashooter grid item
                    // and destroys only one of its template cards. Disable
                    // every remaining template immediately, then destroy it.
                    card.gameObject.SetActive(false);
                    UnityEngine.Object.Destroy(card.gameObject);
                    removed++;
                    continue;
                }

                if (normalCard == null ||
                    card.theSeedCost < normalCard.theSeedCost)
                {
                    if (normalCard != null)
                    {
                        if (carbonCopy == null ||
                            normalCard.theSeedCost >
                            carbonCopy.theSeedCost)
                        {
                            carbonCopy = normalCard;
                        }
                    }

                    normalCard = card;
                }
                else if (carbonCopy == null ||
                         card.theSeedCost > carbonCopy.theSeedCost)
                {
                    carbonCopy = card;
                }
            }

            if (normalCard == null || carbonCopy == null)
            {
                Plugin.Logger.LogWarning(
                    "[Night Roof] Electronion base/Carbon Copy pair " +
                    "could not be identified completely."
                );
                return removed;
            }

            int baseCost =
                PlantDataManager.PlantData_Default[
                    PlantType.ElectricOnion
                ].cost;

            ConfigureNormalCard(
                normalCard,
                cardContainer.gameObject,
                baseCost,
                false
            );
            ConfigureNormalCard(
                carbonCopy,
                cardContainer.gameObject,
                baseCost * 2,
                true
            );

            // CustomizeLib keeps child 0 as a visible source/template packet
            // after cloning the real base and Carbon Copy CardUIs. Once the
            // container is moved into a paginated native grid, that source is
            // laid out beside the selectable base card and looks like a
            // duplicate Electronion. It has no CardUI and is safe to hide.
            if (cardContainer.childCount > 0)
            {
                Transform visualTemplate =
                    cardContainer.GetChild(0);

                if (visualTemplate != null &&
                    visualTemplate
                        .GetComponentsInChildren<CardUI>(true)
                        .Length == 0)
                {
                    visualTemplate.gameObject.SetActive(false);
                }
            }

            normalCardContainer = cardContainer;
            normalCardInstance = normalCard;
            carbonCopyInstance = carbonCopy;

            // CustomizeLib.CheckCardState expects this exact hierarchy:
            // child 1 = Carbon Copy, child 2 = regular card.
            carbonCopy.transform.SetSiblingIndex(1);
            normalCard.transform.SetSiblingIndex(2);

            RefreshNormalPairVisibility();

            return removed;
        }

        internal static void RefreshNormalPairVisibility()
        {
            if (normalCardContainer == null ||
                normalCardInstance == null ||
                carbonCopyInstance == null)
            {
                return;
            }

            bool selectedBase = false;
            bool selectedCarbon = false;
            InGameUI ui = InGameUI.Instance;

            if (ui != null && ui.CardSlotManager != null)
            {
                // CardSlotManager finishes selection asynchronously. Query
                // the actual slot array instead of the temporary hierarchy
                // seen by InGameUI.MoveCardToTarget's immediate postfix.
                selectedBase =
                    ui.CardSlotManager.ContainsCard(normalCardInstance);
                selectedCarbon =
                    ui.CardSlotManager.ContainsCard(carbonCopyInstance);
            }

            // A selected CardUI is reparented into the seed bank. Never
            // disable that selected object: only update whichever member
            // of the pair is still inside the selection-grid container.
            if (IsInsideNormalContainer(normalCardInstance.transform))
            {
                normalCardInstance.gameObject.SetActive(
                    !selectedBase && !selectedCarbon
                );
            }

            if (IsInsideNormalContainer(carbonCopyInstance.transform))
            {
                carbonCopyInstance.gameObject.SetActive(
                    selectedBase && !selectedCarbon
                );
            }

            if (lastSelectedBase != selectedBase ||
                lastSelectedCarbon != selectedCarbon)
            {
                lastSelectedBase = selectedBase;
                lastSelectedCarbon = selectedCarbon;

                Plugin.Logger.LogInfo(
                    "[Night Roof] Electronion card state" +
                    " | Base selected = " + selectedBase +
                    " | Carbon selected = " + selectedCarbon +
                    " | Base visible = " +
                    normalCardInstance.gameObject.activeSelf +
                    " | Carbon visible = " +
                    carbonCopyInstance.gameObject.activeSelf
                );
            }
        }

        private static bool IsInsideNormalContainer(Transform candidate)
        {
            if (candidate == null || normalCardContainer == null)
                return false;

            Transform? current = candidate;
            while (current != null)
            {
                if (current == normalCardContainer)
                    return true;

                current = current.parent;
            }

            return false;
        }

        private static void ConfigureNormalCard(
            CardUI card,
            GameObject parent,
            int cost,
            bool isExtra
        )
        {
            card.thePlantType = PlantType.ElectricOnion;
            card.theSeedType = (int)PlantType.ElectricOnion;
            card.theSeedCost = cost;
            card.fullCD =
                PlantDataManager.PlantData_Default[
                    PlantType.ElectricOnion
                ].cd;
            card.CD = card.fullCD;
            card.parent = parent;
            card.isExtra = isExtra;

            ApplyCardSkin(card);
        }

        private static int HideElectronionFromUniqueSelection(
            SeedLibrary library
        )
        {
            Transform? uniqueCards = FindCardsContainer(
                library,
                "ColorCards"
            );

            if (uniqueCards == null)
                return 0;

            int hidden = 0;

            for (int pageIndex = 0;
                 pageIndex < uniqueCards.childCount;
                 pageIndex++)
            {
                Transform page = uniqueCards.GetChild(pageIndex);
                if (page == null)
                    continue;

                for (int cardIndex = 0;
                     cardIndex < page.childCount;
                     cardIndex++)
                {
                    Transform cardContainer = page.GetChild(cardIndex);
                    if (cardContainer == null ||
                        !ContainsPlant(
                            cardContainer,
                            PlantType.ElectricOnion
                        ))
                    {
                        continue;
                    }

                    cardContainer.gameObject.SetActive(false);
                    hidden++;
                }
            }

            return hidden;
        }

        internal static void RepairAlmanac(AlmanacPlantMenu menu)
        {
            if (menu == null || menu.cards == null)
                return;

            AlmanacCardUI? electronion = null;
            AlmanacCardUI? frozenGiftbox = null;
            AlmanacCardUI? anyElectronion = null;
            AlmanacCardUI? anyFrozenGiftbox = null;
            int electronionCandidates = 0;
            int frozenGiftboxCandidates = 0;

            for (int index = 0; index < menu.cards.Count; index++)
            {
                AlmanacCardUI card = menu.cards[index];
                if (card == null)
                    continue;

                if (card.PlantType == PlantType.ElectricOnion)
                {
                    electronionCandidates++;
                    anyElectronion ??= card;

                    if (menu.basicCardHead != null &&
                        IsDescendantOf(
                            card.transform,
                            menu.basicCardHead
                        ))
                    {
                        electronion = card;
                    }
                }
                else if (card.PlantType == PlantType.SnowPresent)
                {
                    frozenGiftboxCandidates++;
                    anyFrozenGiftbox ??= card;

                    if (menu.basicCardHead != null &&
                        IsDescendantOf(
                            card.transform,
                            menu.basicCardHead
                        ))
                    {
                        frozenGiftbox = card;
                    }
                }
            }

            electronion ??= anyElectronion;
            frozenGiftbox ??= anyFrozenGiftbox;

            if (electronion == null || frozenGiftbox == null)
            {
                Plugin.Logger.LogWarning(
                    "[Night Roof] Almanac placement could not be " +
                    "repaired | Electronion found = " +
                    (electronion != null) +
                    " | Frozen Giftbox found = " +
                    (frozenGiftbox != null)
                );
                return;
            }

            // Grid2 is inactive in the Unlocked/basic view. Both Frozen
            // Giftbox and Electronion can have Almanac instances in more
            // than one grid, so choosing the last matching card silently
            // sent Electronion into that disabled hierarchy. Always target
            // basicCardHead (the visible Grid) explicitly.
            Transform targetParent =
                menu.basicCardHead != null
                    ? menu.basicCardHead
                    : frozenGiftbox.transform.parent;
            if (targetParent == null)
                return;

            electronion.transform.SetParent(targetParent, false);
            int targetSiblingIndex =
                frozenGiftbox.transform.parent == targetParent
                    ? frozenGiftbox.transform.GetSiblingIndex() + 1
                    : targetParent.childCount - 1;
            electronion.transform.SetSiblingIndex(targetSiblingIndex);

            // The card was originally created under Grid2. Normalize all
            // RectTransform data to a native unlocked card after moving it
            // into Grid; root activity alone does not guarantee that its
            // internal visuals survive the category/layout transition.
            RectTransform? electronionRect =
                electronion.transform as RectTransform;
            RectTransform? frozenRect =
                frozenGiftbox.transform as RectTransform;

            if (electronionRect != null && frozenRect != null)
            {
                electronionRect.anchorMin = frozenRect.anchorMin;
                electronionRect.anchorMax = frozenRect.anchorMax;
                electronionRect.pivot = frozenRect.pivot;
                electronionRect.sizeDelta = frozenRect.sizeDelta;
                electronionRect.localScale = frozenRect.localScale;
                electronionRect.localRotation = frozenRect.localRotation;
            }

            electronion.gameObject.SetActive(true);
            if (electronion.image != null)
                electronion.image.enabled = true;
            if (electronion.background != null)
                electronion.background.enabled = true;
            if (electronion.cost != null)
                electronion.cost.enabled = true;
            if (electronion.shadowMask != null)
                electronion.shadowMask.enabled = false;

            LayoutElement? layoutElement =
                electronion.GetComponent<LayoutElement>();
            if (layoutElement != null)
                layoutElement.ignoreLayout = false;

            RectTransform? targetRect =
                targetParent as RectTransform;
            if (targetRect != null)
            {
                EnsureAlmanacScrollableArea(
                    menu,
                    targetRect
                );
                LayoutRebuilder.ForceRebuildLayoutImmediate(targetRect);
            }

            if (menu.basicCardContent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(
                    menu.basicCardContent
                );
            }

            Plugin.Logger.LogInfo(
                "[Night Roof] Almanac placement repaired" +
                " | Electronion is immediately after Frozen Giftbox" +
                " | Unlock = " +
                Lawnf.CheckIfPlantUnlock(PlantType.ElectricOnion) +
                " | Active = " +
                electronion.gameObject.activeSelf +
                " | In hierarchy = " +
                electronion.gameObject.activeInHierarchy +
                " | Parent = " + targetParent.name +
                " | Electronion candidates = " +
                electronionCandidates +
                " | Frozen candidates = " +
                frozenGiftboxCandidates +
                " | Local position = " +
                electronion.transform.localPosition +
                " | Shadow mask = " +
                (
                    electronion.shadowMask != null &&
                    electronion.shadowMask.enabled
                ) +
                " | Unique classification removed"
            );
        }

        private static bool IsDescendantOf(
            Transform candidate,
            Transform expectedAncestor
        )
        {
            if (candidate == null || expectedAncestor == null)
                return false;

            Transform? current = candidate;
            while (current != null)
            {
                if (current == expectedAncestor)
                    return true;

                current = current.parent;
            }

            return false;
        }

        private static void EnsureAlmanacScrollableArea(
            AlmanacPlantMenu menu,
            RectTransform gridRect
        )
        {
            GridLayoutGroup? grid =
                gridRect.GetComponent<GridLayoutGroup>();
            if (grid == null)
                return;

            int columns = grid.constraintCount;
            if (grid.constraint !=
                GridLayoutGroup.Constraint.FixedColumnCount ||
                columns <= 0)
            {
                float usableWidth =
                    gridRect.rect.width -
                    grid.padding.left -
                    grid.padding.right;
                float step = grid.cellSize.x + grid.spacing.x;

                columns = step > 0f
                    ? Math.Max(
                        1,
                        Mathf.FloorToInt(
                            (usableWidth + grid.spacing.x) / step
                        )
                    )
                    : 1;
            }

            int rows = Mathf.CeilToInt(
                (float)gridRect.childCount / columns
            );
            float requiredHeight =
                grid.padding.top +
                grid.padding.bottom +
                rows * grid.cellSize.y +
                Math.Max(0, rows - 1) * grid.spacing.y;
            float currentHeight = gridRect.rect.height;
            float growth = requiredHeight - currentHeight;

            if (growth > 0.5f)
            {
                gridRect.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    requiredHeight
                );
            }

            if (menu.basicCardContent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);

                // Grid's ContentSizeFitter may already have expanded Grid
                // before this method runs. Content has no fitter, so it must
                // always be checked independently instead of only when Grid
                // itself grew.
                float gridBottom =
                    Math.Abs(gridRect.anchoredPosition.y) +
                    gridRect.rect.height;
                float requiredContentHeight = gridBottom + 8f;

                if (menu.basicCardContent.rect.height <
                    requiredContentHeight)
                {
                    menu.basicCardContent.SetSizeWithCurrentAnchors(
                        RectTransform.Axis.Vertical,
                        requiredContentHeight
                    );
                }
            }

            Plugin.Logger.LogInfo(
                "[Night Roof] Almanac layout measured" +
                " | Columns = " + columns +
                " | Rows = " + rows +
                " | Grid height = " + gridRect.rect.height +
                " | Required grid height = " + requiredHeight +
                " | Content height = " +
                (
                    menu.basicCardContent != null
                        ? menu.basicCardContent.rect.height
                        : -1f
                )
            );
        }

        private static Transform? FindCardsContainer(
            SeedLibrary library,
            string name
        )
        {
            for (int index = 0;
                 index < library.cardPagesContainer.childCount;
                 index++)
            {
                Transform child =
                    library.cardPagesContainer.GetChild(index);

                if (child != null &&
                    child.name.Equals(name, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static bool ContainsPlant(
            Transform root,
            PlantType plantType
        )
        {
            CardUI[] cards =
                root.GetComponentsInChildren<CardUI>(true);

            for (int index = 0; index < cards.Length; index++)
            {
                if (cards[index] != null &&
                    cards[index].thePlantType == plantType)
                {
                    return true;
                }
            }

            return false;
        }

        private static Transform? FindDirectCardContainer(
            Transform cardsContainer,
            PlantType plantType
        )
        {
            for (int pageIndex = 0;
                 pageIndex < cardsContainer.childCount;
                 pageIndex++)
            {
                Transform page = cardsContainer.GetChild(pageIndex);
                if (page == null)
                    continue;

                for (int cardIndex = 0;
                     cardIndex < page.childCount;
                     cardIndex++)
                {
                    Transform cardContainer = page.GetChild(cardIndex);
                    if (cardContainer == null)
                        continue;

                    CardUI[] cards =
                        cardContainer.GetComponentsInChildren<CardUI>(
                            true
                        );

                    for (int index = 0; index < cards.Length; index++)
                    {
                        CardUI card = cards[index];
                        if (card != null &&
                            card.thePlantType == plantType)
                        {
                            return cardContainer;
                        }
                    }
                }
            }

            return null;
        }

        private static bool IsSandboxCard(CardUI card)
        {
            if (IsSandboxContext())
                return true;

            if (card == null || card.transform == null)
                return false;

            IZBottomMenu menu = IZBottomMenu.Instance;
            if (menu == null || menu.plantLibrary == null)
                return false;

            Transform? current = card.transform;
            Transform sandboxRoot = menu.plantLibrary.transform;

            while (current != null)
            {
                if (current == sandboxRoot)
                    return true;

                current = current.parent;
            }

            return false;
        }

        private static bool IsSandboxPlantLibrary(SeedLibrary library)
        {
            IZBottomMenu menu = IZBottomMenu.Instance;
            if (menu == null || menu.plantLibrary == null)
                return false;

            Transform current = library.transform;
            Transform sandboxRoot = menu.plantLibrary.transform;

            while (current != null)
            {
                if (current == sandboxRoot)
                    return true;

                current = current.parent;
            }

            return false;
        }

        private static bool IsSandboxContext()
        {
            Board board = Board.Instance;
            return board != null && board.boardTag.isIZ;
        }

        private static void EnsureCardSprite()
        {
            if (cardSprite != null)
                return;

            Assembly assembly = Assembly.GetExecutingAssembly();
            using Stream? stream =
                assembly.GetManifestResourceStream(CardResourceName);

            if (stream == null)
            {
                throw new InvalidOperationException(
                    "Embedded Night Roof card PNG is missing."
                );
            }

            byte[] png = new byte[stream.Length];
            int offset = 0;

            while (offset < png.Length)
            {
                int read = stream.Read(png, offset, png.Length - offset);
                if (read <= 0)
                    break;

                offset += read;
            }

            if (offset != png.Length)
            {
                throw new EndOfStreamException(
                    "Night Roof card PNG could not be read completely."
                );
            }

            cardTexture = new Texture2D(
                2,
                2,
                TextureFormat.RGBA32,
                false
            );
            cardTexture.name = "PlantsPlus_NightRoofCard_Texture";
            cardTexture.filterMode = FilterMode.Bilinear;
            cardTexture.wrapMode = TextureWrapMode.Clamp;

            if (!ImageConversion.LoadImage(cardTexture, png, false))
            {
                UnityEngine.Object.Destroy(cardTexture);
                cardTexture = null;

                throw new InvalidOperationException(
                    "Unity could not decode the Night Roof card PNG."
                );
            }

            cardSprite = Sprite.Create(
                cardTexture,
                new Rect(
                    0f,
                    0f,
                    cardTexture.width,
                    cardTexture.height
                ),
                new Vector2(0.5f, 0.5f),
                100f
            );
            cardSprite.name = "PlantsPlus_NightRoofCard";
        }
    }

    [HarmonyPatch]
    internal static class NightRoofCardPatches
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(Lawnf), nameof(Lawnf.IsBasicPlant))]
        private static void IsBasicPlantPostfix(
            PlantType thePlantType,
            ref bool __result
        )
        {
            if (thePlantType == PlantType.ElectricOnion)
                __result = true;
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(Lawnf), nameof(Lawnf.CheckIfPlantUnlock))]
        private static void CheckIfPlantUnlockPostfix(
            PlantType thePlantType,
            ref UnlockType __result
        )
        {
            if (thePlantType == PlantType.ElectricOnion)
                __result = UnlockType.Unlocked;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CardUI), "Start")]
        private static void CardStartPostfix(CardUI __instance)
        {
            NightRoofCards.ApplyCardSkin(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CardUI), nameof(CardUI.ChangeCardSprite))]
        private static void ChangeCardSpritePostfix(CardUI __instance)
        {
            NightRoofCards.ApplyCardSkin(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(
            typeof(CardUI),
            nameof(CardUI.SetImage),
            new Type[] { typeof(int) }
        )]
        private static void SetImageByIndexPostfix(CardUI __instance)
        {
            NightRoofCards.ApplyCardSkin(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(
            typeof(CardUI),
            nameof(CardUI.SetImage),
            new Type[] { typeof(CardBgType) }
        )]
        private static void SetImageByTypePostfix(CardUI __instance)
        {
            NightRoofCards.ApplyCardSkin(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(SeedLibrary), nameof(SeedLibrary.ShowCards))]
        private static void ShowCardsPostfix(
            SeedLibrary __instance,
            string name
        )
        {
            NightRoofCards.EnsureSandboxElectronion(__instance);
            NightRoofCards.RefreshSelectionCards(__instance);
        }

        // The IZ sandbox uses a direct Grid/Main/Page1 hierarchy rather than
        // SeedLibrary. Retry until that hierarchy has finished loading.
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(IZBottomMenu), "Update")]
        private static void IZBottomMenuUpdatePostfix(
            IZBottomMenu __instance
        )
        {
            if (__instance == null ||
                __instance.plantLibrary == null)
            {
                return;
            }

            NightRoofCards.EnsureSandboxElectronion(__instance);
        }

        // CustomizeLib creates custom cards from a coroutine 1.5 seconds
        // after SeedLibrary.Awake. Its generic clone is incompatible with
        // restricted challenge layouts: it produces an Electronion preview
        // under a Peashooter seed packet. Filter only Electronion out of that
        // invocation, let CustomizeLib finish rebuilding the pages, then add
        // the single locked native card ourselves.
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(PatchMgr), nameof(PatchMgr.ShowCards))]
        private static void CustomizeLibShowCardsPrefix(
            out bool __state
        )
        {
            __state =
                NightRoofCards.SuppressLateCustomizeLibElectronionCard();
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(PatchMgr), nameof(PatchMgr.ShowCards))]
        private static void CustomizeLibShowCardsPostfix(
            bool __state
        )
        {
            NightRoofCards.FinishCustomizeLibCardCreation(__state);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(
            typeof(InGameUI),
            nameof(InGameUI.MoveCardToTarget),
            new Type[] { typeof(CardUI), typeof(bool) }
        )]
        private static void MoveCardToTargetPostfix()
        {
            NightRoofCards.RefreshNormalPairVisibility();
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(
            typeof(InGameUI),
            nameof(InGameUI.RemoveCardFromBank),
            new Type[] { typeof(CardUI), typeof(bool) }
        )]
        private static void RemoveCardFromBankPostfix()
        {
            NightRoofCards.RefreshNormalPairVisibility();
        }

        // CardSlotManager.MoveCardToTarget completes on an async continuation,
        // after InGameUI.MoveCardToTarget has already returned. This final
        // lightweight check observes the real slot state on the following
        // frames, which is when the Carbon Copy must become visible.
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(InGameUI), "Update")]
        private static void InGameUIUpdatePostfix()
        {
            NightRoofCards.RepairCardsAfterCustomizeLibCreation();
            NightRoofCards.RefreshNormalPairVisibility();
            NightRoofCards.RefreshLimitedLevelAvailability();
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(CardUI), "Update")]
        private static void CardUpdatePostfix(CardUI __instance)
        {
            // Some level-specific card logic refreshes the native background
            // after Start/SetImage. Reapply the Night Roof background at the
            // final CardUI stage so every non-sandbox level stays consistent.
            NightRoofCards.ApplyCardSkin(__instance);
            NightRoofCards.EnforceLimitedLevelAvailability(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(CardUI), "OnMouseDown")]
        private static bool CardOnMouseDownPrefix(
            CardUI __instance
        )
        {
            if (!NightRoofCards.ShouldBlockLimitedLevelClick(__instance))
                return true;

            NightRoofCards.RefreshLimitedLevelAvailability();
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(AlmanacPlantMenu), "InitCards")]
        private static void AlmanacInitCardsPostfix(
            AlmanacPlantMenu __instance
        )
        {
            NightRoofCards.RepairAlmanac(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(
            typeof(AlmanacPlantMenu),
            nameof(AlmanacPlantMenu.LookUnlocked)
        )]
        private static void AlmanacLookUnlockedPostfix(
            AlmanacPlantMenu __instance
        )
        {
            NightRoofCards.RepairAlmanac(__instance);
        }
    }
}
