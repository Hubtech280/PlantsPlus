using System;
using System.Collections.Generic;
using System.Reflection;
using CustomizeLib.MelonLoader;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace PlantsPlus.Core
{
    internal static class BumpingShroomBootstrap
    {
        private static bool registered;
        private static bool nativeMixTreeLogged;
        private static bool almanacIngredientLinksLogged;
        private static AlmanacSlot? lobAlmanacSlot;
        private static AlmanacSlot? bounceAlmanacSlot;

        private sealed class AlmanacSlot
        {
            internal Transform Parent = null!;
            internal Vector2 AnchorMin;
            internal Vector2 AnchorMax;
            internal Vector2 Pivot;
            internal Vector2 SizeDelta;
            internal Vector2 AnchoredPosition;
            internal Vector3 LocalScale;
            internal Quaternion LocalRotation;
            internal int SiblingIndex;
        }

        internal static void OnStart()
        {
            if (registered)
                return;

            try
            {
                AssetBundle? bundle = CustomCore.GetAssetBundle(
                    Assembly.GetExecutingAssembly(),
                    "PlantsPlus.Resources.AssetBundles.bumping_shroom"
                );
                GameObject? prefab = bundle?.GetAsset<GameObject>("CaltropPrefab");
                GameObject? preview = bundle?.GetAsset<GameObject>("CaltropPreview");

                if (bundle == null || prefab == null || preview == null)
                    throw new InvalidOperationException(
                        "Bumping-shroom prefab or preview missing."
                    );

                prefab.transform.localPosition = Vector3.zero;
                V11PlantsBootstrap.IsolateAnimationClips(
                    bundle,
                    prefab,
                    "Bumping-shroom",
                    "idle",
                    "attack",
                    "rest",
                    "rest-idle",
                    "wake-up"
                );

                CustomCore.RegisterCustomPlant<Caltrop, Plants.BumpingShroom>(
                    Plants.BumpingShroom.ID,
                    prefab,
                    preview,
                    new List<(int, int)>
                    {
                        (
                            Plants.LobShroom.ID,
                            Plants.BounceShroom.ID
                        ),
                        (
                            Plants.BounceShroom.ID,
                            Plants.LobShroom.ID
                        )
                    },
                    0f,
                    0f,
                    20,
                    600,
                    15f,
                    75
                );

                InstallTypeFlags();
                AlmanacEntry entry = AlmanacContent.BumpingShroom;
                CustomCore.AddPlantAlmanacStrings(
                    (PlantType)Plants.BumpingShroom.ID,
                    entry.Name,
                    entry.Info,
                    entry.Introduce,
                    75
                );

                registered = true;
                Plugin.Logger.LogInfo(
                    "[Bumping-shroom] Registered | ID=6036 | " +
                    "Recipe=3 Lob-shrooms + Bounce-shroom | " +
                    "Bounce then 3 spores"
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Bumping-shroom] Registration failed: " + exception
                );
            }
        }

        internal static void OnGameInit()
        {
            InstallTypeFlags();
            AlmanacCompatibility.RefreshLoadedData();
        }

        internal static void EnsureNativeFusionRecipe()
        {
            try
            {
                PlantType lob = (PlantType)Plants.LobShroom.ID;
                PlantType bounce = (PlantType)Plants.BounceShroom.ID;
                PlantType bumping = (PlantType)Plants.BumpingShroom.ID;
                PlantType existing;

                if (!MixData.TryGetMix(
                        lob,
                        bounce,
                        out existing,
                        true
                    ) || existing != bumping)
                {
                    MixData.AddOrderedRecipe(lob, bounce, bumping);
                }

                Plugin.Logger.LogInfo(
                    "[Bumping-shroom] Native fusion ancestry installed " +
                    "| Lob-shroom + Bounce-shroom -> Bumping-shroom"
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Bumping-shroom] Native fusion ancestry repair " +
                    "failed safely: " + exception.Message
                );
            }
        }

        internal static void EnsureNativeMixTreeRecipe()
        {
            try
            {
                if (!PlantMixTreeManager.IsInitialized)
                    return;

                PlantType lob = (PlantType)Plants.LobShroom.ID;
                PlantType bounce = (PlantType)Plants.BounceShroom.ID;
                PlantType bumping = (PlantType)Plants.BumpingShroom.ID;
                var trees = PlantMixTreeManager.PlantMixTrees;
                var parentsByChild = PlantMixTreeManager.ChildToParents;

                if (trees == null || parentsByChild == null)
                    return;

                PlantMixTreeNode lobNode;
                if (!trees.TryGetValue(lob, out lobNode))
                {
                    lobNode = new PlantMixTreeNode(lob);
                    trees.Add(lob, lobNode);
                }

                PlantMixTreeNode bounceNode;
                if (!trees.TryGetValue(bounce, out bounceNode))
                {
                    bounceNode = new PlantMixTreeNode(bounce);
                    trees.Add(bounce, bounceNode);
                }

                PlantMixTreeNode bumpingNode;
                if (!trees.TryGetValue(bumping, out bumpingNode))
                {
                    bumpingNode = new PlantMixTreeNode(bumping);
                    trees.Add(bumping, bumpingNode);
                }

                lobNode.Recipes[bounce] = bumping;
                bounceNode.Recipes[lob] = bumping;
                AddNodeOnce(lobNode.DirectChildren, bumpingNode);
                AddNodeOnce(bounceNode.DirectChildren, bumpingNode);
                AddNodeOnce(lobNode.AllDescendants, bumpingNode);
                AddNodeOnce(bounceNode.AllDescendants, bumpingNode);

                Il2CppSystem.Collections.Generic.List<
                    PlantMixTreeManager.MixParentInfo> parents;
                if (!parentsByChild.TryGetValue(bumping, out parents))
                {
                    parents = new Il2CppSystem.Collections.Generic.List<
                        PlantMixTreeManager.MixParentInfo>();
                    parentsByChild.Add(bumping, parents);
                }

                bool found = false;
                for (int index = 0; index < parents.Count; index++)
                {
                    PlantMixTreeManager.MixParentInfo parent = parents[index];
                    bool sameOrder =
                        parent.ParentA == lob && parent.ParentB == bounce;
                    bool reverseOrder =
                        parent.ParentA == bounce && parent.ParentB == lob;
                    if (parent.Result == bumping &&
                        (sameOrder || reverseOrder))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    parents.Add(new PlantMixTreeManager.MixParentInfo(
                        lob,
                        bounce,
                        bumping
                    ));
                }

                if (!nativeMixTreeLogged)
                {
                    nativeMixTreeLogged = true;
                    Plugin.Logger.LogInfo(
                        "[Bumping-shroom] Native fusion tree installed " +
                        "| Lob-shroom x3 + Bounce-shroom -> Bumping-shroom"
                    );
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Bumping-shroom] Native fusion tree repair " +
                    "failed safely: " + exception.Message
                );
            }
        }

        private static void AddNodeOnce(
            Il2CppSystem.Collections.Generic.List<PlantMixTreeNode> nodes,
            PlantMixTreeNode node)
        {
            if (nodes == null || node == null)
                return;

            for (int index = 0; index < nodes.Count; index++)
            {
                if (nodes[index] != null &&
                    nodes[index].PlantType == node.PlantType)
                {
                    return;
                }
            }

            nodes.Add(node);
        }

        internal static void CaptureAlmanacIngredientSlots(AlmanacCardUI card)
        {
            if (card == null)
                return;

            try
            {
                AlmanacPlantMenu? menu =
                    card.menu?.TryCast<AlmanacPlantMenu>();
                if (menu == null)
                    return;

                ResetAlmanacCardPosition(FindAlmanacCard(
                    menu,
                    (PlantType)Plants.BumpingShroom.ID,
                    false
                ));
                lobAlmanacSlot = CaptureAlmanacSlot(
                    FindAlmanacCard(
                        menu,
                        (PlantType)Plants.LobShroom.ID,
                        true
                    )
                );
                bounceAlmanacSlot = CaptureAlmanacSlot(
                    FindAlmanacCard(
                        menu,
                        (PlantType)Plants.BounceShroom.ID,
                        true
                    )
                );
            }
            catch { }
        }

        internal static void HandleAlmanacIngredientClick(
            AlmanacCardUI card)
        {
            if (card == null)
                return;

            try
            {
                AlmanacPlantMenu? menu =
                    card.menu?.TryCast<AlmanacPlantMenu>();
                if (menu == null)
                    return;

                int clicked = (int)card.PlantType;
                bool isLob = clicked == Plants.LobShroom.ID;
                bool isBounce = clicked == Plants.BounceShroom.ID;
                if (!isLob && !isBounce)
                    return;

                Canvas.ForceUpdateCanvases();
                AlmanacSlot? targetSlot =
                    isLob ? bounceAlmanacSlot : lobAlmanacSlot;
                AlmanacCardUI? bumpingCard = FindAlmanacCard(
                    menu,
                    (PlantType)Plants.BumpingShroom.ID,
                    true
                );
                PlaceAlmanacResult(bumpingCard, targetSlot);

                Canvas.ForceUpdateCanvases();

                if (!almanacIngredientLinksLogged)
                {
                    almanacIngredientLinksLogged = true;
                    Plugin.Logger.LogInfo(
                        "[Bumping-shroom] Card moved onto the opposite " +
                        "Lob/Bounce ingredient slot only | Visible = " +
                        (bumpingCard != null &&
                         bumpingCard.gameObject.activeInHierarchy)
                    );
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Bumping-shroom] Almanac result display failed " +
                    "safely: " + exception.Message
                );
            }
        }

        private static AlmanacCardUI? FindAlmanacCard(
            AlmanacPlantMenu menu,
            PlantType type,
            bool preferActive = false)
        {
            if (menu.cards == null)
                return null;

            AlmanacCardUI? fallback = null;

            for (int index = 0; index < menu.cards.Count; index++)
            {
                AlmanacCardUI candidate = menu.cards[index];
                if (candidate == null || candidate.PlantType != type)
                    continue;
                fallback ??= candidate;
                if (!preferActive || candidate.gameObject.activeInHierarchy)
                    return candidate;
            }

            // Adventure filters can remove the opposite ingredient from the
            // menu list before the pointer-down postfix runs. It still exists
            // as an inactive Almanac card, so recover it from loaded objects.
            AlmanacCardUI[] loadedCards =
                Resources.FindObjectsOfTypeAll<AlmanacCardUI>();
            for (int index = 0; index < loadedCards.Length; index++)
            {
                AlmanacCardUI candidate = loadedCards[index];
                if (candidate == null || candidate.PlantType != type)
                    continue;

                AlmanacPlantMenu? candidateMenu =
                    candidate.menu?.TryCast<AlmanacPlantMenu>();
                if (candidateMenu != menu)
                    continue;

                fallback ??= candidate;
                if (!preferActive || candidate.gameObject.activeInHierarchy)
                    return candidate;
            }

            return fallback;
        }

        private static AlmanacSlot? CaptureAlmanacSlot(
            AlmanacCardUI? card)
        {
            if (card?.transform is not RectTransform rect)
                return null;

            return new AlmanacSlot
            {
                Parent = rect.parent,
                AnchorMin = rect.anchorMin,
                AnchorMax = rect.anchorMax,
                Pivot = rect.pivot,
                SizeDelta = rect.sizeDelta,
                AnchoredPosition = rect.anchoredPosition,
                LocalScale = rect.localScale,
                LocalRotation = rect.localRotation,
                SiblingIndex = rect.GetSiblingIndex()
            };
        }

        private static void PlaceAlmanacResult(
            AlmanacCardUI? card,
            AlmanacSlot? slot)
        {
            if (card == null || slot == null ||
                card.transform is not RectTransform rect)
            {
                return;
            }

            if (rect.parent != slot.Parent)
                rect.SetParent(slot.Parent, false);

            LayoutElement? layout = card.GetComponent<LayoutElement>();
            if (layout == null)
                layout = card.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;

            rect.anchorMin = slot.AnchorMin;
            rect.anchorMax = slot.AnchorMax;
            rect.pivot = slot.Pivot;
            rect.sizeDelta = slot.SizeDelta;
            rect.localScale = slot.LocalScale;
            rect.localRotation = slot.LocalRotation;
            rect.anchoredPosition = slot.AnchoredPosition;
            rect.SetSiblingIndex(Math.Min(
                slot.SiblingIndex,
                Math.Max(0, slot.Parent.childCount - 1)
            ));

            card.gameObject.SetActive(true);
            card.UnHide();
            if (card.image != null)
                card.image.enabled = true;
            if (card.background != null)
                card.background.enabled = true;
            if (card.cost != null)
                card.cost.enabled = true;
            if (card.shadowMask != null)
                card.shadowMask.enabled = false;

            // UnHide/layout changes can touch the RectTransform. Reapply the
            // captured native ingredient slot last so the result cannot be
            // pushed outside the filtered fusion view.
            rect.anchoredPosition = slot.AnchoredPosition;
        }

        private static void ResetAlmanacCardPosition(AlmanacCardUI? card)
        {
            if (card == null)
                return;

            LayoutElement? layout = card.GetComponent<LayoutElement>();
            if (layout == null || !layout.ignoreLayout)
                return;

            layout.ignoreLayout = false;
            if (card.transform.parent is RectTransform parent)
                LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
        }

        private static void InstallTypeFlags()
        {
            PlantType type = (PlantType)Plants.BumpingShroom.ID;
            if (CustomCore.TypeMgrExtra.IsCaltrop != null &&
                !CustomCore.TypeMgrExtra.IsCaltrop.Contains(type))
            {
                CustomCore.TypeMgrExtra.IsCaltrop.Add(type);
            }

            // This is a fusion result, never a directly selectable basic card.
            CoreEnums.baiscPlants?.Remove(type);
            TypeData.SpecialCardPlants?.Remove(type);
        }

        internal static int CountLobShrooms(int column, int row)
        {
            int count = 0;
            try
            {
                var plants = Lawnf.Get1x1Plants(column, row);
                if (plants == null)
                    return 0;

                for (int index = 0; index < plants.Count; index++)
                {
                    Plant plant = plants[index];
                    if (plant != null && !plant.dying &&
                        plant.thePlantHealth > 0 &&
                        (int)plant.thePlantType == Plants.LobShroom.ID)
                    {
                        count++;
                    }
                }
            }
            catch { }

            return count;
        }

        internal static bool HasBounceShroom(int column, int row)
        {
            try
            {
                var plants = Lawnf.Get1x1Plants(column, row);
                if (plants == null)
                    return false;

                for (int index = 0; index < plants.Count; index++)
                {
                    Plant plant = plants[index];
                    if (plant != null && !plant.dying &&
                        plant.thePlantHealth > 0 &&
                        (int)plant.thePlantType == Plants.BounceShroom.ID)
                    {
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        internal static void ConsumeRemainingLobIngredients(Caltrop bumping)
        {
            if (bumping == null)
                return;

            try
            {
                var plants = Lawnf.Get1x1Plants(
                    bumping.thePlantColumn,
                    bumping.thePlantRow
                );
                if (plants == null)
                    return;

                int remaining = 2;
                for (int index = plants.Count - 1;
                    index >= 0 && remaining > 0;
                    index--)
                {
                    Plant candidate = plants[index];
                    if (candidate == null || candidate.dying ||
                        candidate.thePlantHealth <= 0 ||
                        (int)candidate.thePlantType != Plants.LobShroom.ID)
                    {
                        continue;
                    }

                    Plants.BumpingIngredientRemoval.ByMix(candidate);
                    remaining--;
                }

                Plugin.Logger.LogInfo(
                    "[Bumping-shroom] Native fusion completed at " +
                    bumping.thePlantColumn + "," + bumping.thePlantRow +
                    " | extra Lob-shrooms consumed=" + (2 - remaining)
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Bumping-shroom] Could not consume remaining " +
                    "Lob-shrooms: " + exception.Message
                );
            }
        }
    }

    // Keep the recipe native so the game itself creates Bumping-shroom. The
    // guard only lets that recipe resolve after all three Lob-shrooms exist.
    [HarmonyPatch(typeof(CreatePlant), "CheckMix")]
    internal static class BumpingShroomNativeMixGuardPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(
            int theColumn,
            int theRow,
            PlantType theUsedType,
            ref Plant __result
        )
        {
            int lobCount = BumpingShroomBootstrap.CountLobShrooms(
                theColumn,
                theRow
            );
            bool bounceOntoLobs =
                (int)theUsedType == Plants.BounceShroom.ID && lobCount > 0;
            bool lobOntoBounce =
                (int)theUsedType == Plants.LobShroom.ID &&
                BumpingShroomBootstrap.HasBounceShroom(
                    theColumn,
                    theRow
                );

            if (!bounceOntoLobs && !lobOntoBounce)
                return true;

            // When Bounce is being placed, all three Lobs are already on the
            // tile. When Lob is being placed, two existing Lobs plus the card
            // currently being used make the required three.
            bool hasFullRecipe = bounceOntoLobs
                ? lobCount >= 3
                : lobCount >= 2;
            if (hasFullRecipe)
            {
                // PvZ Fusion 3.9 only understands two-parent mixes here. With
                // three stacked Lob-shrooms it can repaint the used plant with
                // Bumping-shroom's prefab without actually creating plant
                // 6036. Finish the four-ingredient recipe just after the
                // native click has consumed the card, outside CheckMix's
                // re-entrant SetPlant call stack.
                Plants.BumpingFusionRunner.Queue(theColumn, theRow);
                return true;
            }

            __result = null!;
            return false;
        }
    }

    [HarmonyPatch(typeof(CreatePlant), nameof(CreatePlant.CheckBox))]
    internal static class BumpingShroomCheckBoxPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(
            int theBoxColumn,
            int theBoxRow,
            PlantType theSeedType,
            ref bool __result
        )
        {
            if (__result || (int)theSeedType != Plants.BounceShroom.ID ||
                BumpingShroomBootstrap.CountLobShrooms(
                    theBoxColumn,
                    theBoxRow
                ) < 3)
            {
                return;
            }

            __result = true;
        }
    }

    [HarmonyPatch(typeof(Caltrop), nameof(Caltrop.AnimAttack))]
    internal static class BumpingShroomAttackPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Caltrop __instance)
        {
            if ((int)__instance.thePlantType != Plants.BumpingShroom.ID)
                return true;

            __instance.GetComponent<Plants.BumpingShroom>()?.AnimAttack();
            return false;
        }
    }

    [HarmonyPatch(typeof(Caltrop), nameof(Caltrop.Update))]
    internal static class BumpingShroomUpdatePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Caltrop __instance)
        {
            return (int)__instance.thePlantType != Plants.BumpingShroom.ID;
        }
    }

    [HarmonyPatch(typeof(Caltrop), nameof(Caltrop.KillCar))]
    internal static class BumpingShroomCarPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Caltrop __instance)
        {
            return (int)__instance.thePlantType != Plants.BumpingShroom.ID;
        }
    }

    [HarmonyPatch(typeof(BungiZombie), nameof(BungiZombie.Stealable))]
    internal static class BumpingShroomBungeePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Plant plant, ref bool __result)
        {
            if (plant == null ||
                (int)plant.thePlantType != Plants.BumpingShroom.ID)
            {
                return true;
            }

            __result = false;
            return false;
        }
    }
}

namespace PlantsPlus.Plants
{
    public sealed class BumpingFusionRunner : MonoBehaviour
    {
        public BumpingFusionRunner(IntPtr pointer) : base(pointer) { }

        [HideFromIl2Cpp]
        public static void TryQueue(int column, int row) => BumpingFusionWork.TryQueue(column, row);

        [HideFromIl2Cpp]
        public static void Queue(int column, int row) => BumpingFusionWork.Queue(column, row);

        // The injected type is only a lifecycle bridge. No pending managed
        // types or ingredient-removal bodies belong to the injected class.
        public void Update() => BumpingFusionWork.Tick(gameObject);
    }

    internal static class BumpingFusionWork
    {
        private sealed class PendingFusion
        {
            public int Column;
            public int Row;
            public int Frames;
            public bool IngredientsRemoved;
        }

        private static readonly List<PendingFusion> Pending =
            new List<PendingFusion>();
        private static Board? pendingBoard;

        // Puff-family stacking and Caltrop placement can coexist without
        // passing through CreatePlant.CheckMix. Observe the completed native
        // tile from the two ingredient components instead of relying on that
        // optional two-parent mix path.
        [HideFromIl2Cpp]
        public static void TryQueue(int column, int row)
        {
            if (Board.Instance == null ||
                !Core.BumpingShroomBootstrap.HasBounceShroom(column, row) ||
                Core.BumpingShroomBootstrap.CountLobShrooms(column, row) < 3)
            {
                return;
            }

            Queue(column, row);
        }

        [HideFromIl2Cpp]
        public static void Queue(int column, int row)
        {
            Board? board = Board.Instance;
            if (board == null)
                return;

            if (pendingBoard != board)
            {
                Pending.Clear();
                pendingBoard = board;
            }

            for (int index = 0; index < Pending.Count; index++)
            {
                PendingFusion existing = Pending[index];
                if (existing.Column == column && existing.Row == row)
                    return;
            }

            PendingFusion queued = new PendingFusion
            {
                Column = column,
                Row = row,
                Frames = 1,
                IngredientsRemoved = false
            };
            Pending.Add(queued);

            global::PlantsPlus.Plugin.Logger.LogInfo(
                "[Bumping-shroom] Full recipe detected at " +
                column + "," + row + " | 3 Lob-shrooms + Bounce-shroom"
            );

            try
            {
                if (board.gameObject.GetComponent<BumpingFusionRunner>() == null)
                {
                    board.gameObject.AddComponent<BumpingFusionRunner>();
                }
            }
            catch (Exception exception)
            {
                Pending.Remove(queued);
                global::PlantsPlus.Plugin.Logger.LogWarning(
                    "[Bumping-shroom] Could not start deferred native fusion: " +
                    exception.Message
                );
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static void Tick(GameObject owner)
        {
            // Pending is managed state shared by recipe hooks. Only the runner
            // attached to its owning live board may advance it.
            if (pendingBoard == null || Board.Instance != pendingBoard ||
                pendingBoard.gameObject != owner)
                return;

            for (int index = Pending.Count - 1; index >= 0; index--)
            {
                PendingFusion fusion = Pending[index];
                if (fusion.Frames-- > 0)
                    continue;

                if (!fusion.IngredientsRemoved)
                {
                    if (!RemoveIngredients(fusion.Column, fusion.Row))
                    {
                        Pending.RemoveAt(index);
                        continue;
                    }

                    fusion.IngredientsRemoved = true;
                    fusion.Frames = 1;
                    continue;
                }

                Pending.RemoveAt(index);
                CreateResult(fusion.Column, fusion.Row);
            }
        }

        // This type never enters ClassInjector. Keep the queue object out of
        // these call signatures as well, rather than relying on attributes.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool RemoveIngredients(int column, int row)
        {
            try
            {
                Plugin.Logger.LogInfo("[Bumping Fusion] Capture ingredients at " + column + "," + row);
                var plants = Lawnf.Get1x1Plants(column, row);
                if (plants == null)
                    return false;

                int lobCount = 0;
                bool hasBounce = false;
                bool hasFalseResult = false;
                var ingredients = new List<Plant>();
                for (int index = 0; index < plants.Count; index++)
                {
                    Plant candidate = plants[index];
                    if (candidate == null || candidate.dying ||
                        candidate.thePlantHealth <= 0)
                    {
                        continue;
                    }

                    int type = (int)candidate.thePlantType;
                    if (type == LobShroom.ID)
                        lobCount++;
                    else if (type == BounceShroom.ID)
                        hasBounce = true;
                    else if (type == BumpingShroom.ID)
                        hasFalseResult = true;
                    if (type == LobShroom.ID || type == BounceShroom.ID || type == BumpingShroom.ID)
                        ingredients.Add(candidate);
                }

                // Accept both native outcomes: all three Lobs can remain, or
                // the engine can consume one while producing its false result.
                if (!(lobCount >= 3 && hasBounce) &&
                    !(lobCount >= 2 && hasFalseResult))
                    return false;

                Plugin.Logger.LogInfo("[Bumping Fusion] Snapshot ready | Ingredients=" + ingredients.Count);
                BumpingIngredientRemoval.Validate();
                int removed = 0;
                for (int index = ingredients.Count - 1; index >= 0; index--)
                {
                    Plant candidate = ingredients[index];
                    if (candidate == null || candidate.dying ||
                        candidate.thePlantHealth <= 0)
                    {
                        continue;
                    }

                    int type = (int)candidate.thePlantType;
                    if (type != LobShroom.ID &&
                        type != BounceShroom.ID &&
                        type != BumpingShroom.ID)
                    {
                        continue;
                    }

                    Plugin.Logger.LogInfo("[Bumping Fusion] Native ByMix begin | Type=" + type);
                    BumpingIngredientRemoval.ByMix(candidate);
                    Plugin.Logger.LogInfo("[Bumping Fusion] Native ByMix complete | Type=" + type);
                    removed++;
                }

                global::PlantsPlus.Plugin.Logger.LogInfo(
                    "[Bumping-shroom] Four-ingredient fusion accepted at " +
                    column + "," + row +
                    " | old objects removed=" + removed
                );
                return removed > 0;
            }
            catch (Exception exception)
            {
                global::PlantsPlus.Plugin.Logger.LogWarning(
                    "[Bumping-shroom] Ingredient replacement failed safely: " +
                    exception.Message
                );
                return false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void CreateResult(int column, int row)
        {
            try
            {
                Plugin.Logger.LogInfo("[Bumping Fusion] Creating result at " + column + "," + row);
                CreatePlant? creator = CreatePlant.Instance;
                if (creator == null && Board.Instance != null)
                    creator = Board.Instance.GetComponent<CreatePlant>();
                if (creator == null)
                    return;

                Plant created = creator.SetPlant(
                    column,
                    row,
                    (PlantType)BumpingShroom.ID,
                    null,
                    default,
                    true,
                    false,
                    null
                );

                global::PlantsPlus.Plugin.Logger.LogInfo(
                    "[Bumping-shroom] Real result created at " +
                    column + "," + row +
                    " | type=" +
                    (created != null ? ((int)created.thePlantType).ToString() : "null") +
                    " | expected=" + BumpingShroom.ID
                );
            }
            catch (Exception exception)
            {
                global::PlantsPlus.Plugin.Logger.LogWarning(
                    "[Bumping-shroom] Deferred result creation failed safely: " +
                    exception.Message
                );
            }
        }
    }

    public sealed class BumpingShroom : MonoBehaviour
    {
        public const int ID = 6036;
        public const int Toughness = 600;
        private const float BounceDistanceInTiles = 1.5f;
        private const float BounceFlightSeconds = 0.55f;
        private const float SporeDelayAfterBounce = 0.58f;
        private const float RestAnimationSeconds = 0.8333333f;
        private const float RestSeconds = 5f;
        private const float WakeAnimationSeconds = 0.8333333f;
        private const float SporeFlightSeconds = 0.8f;

        private Caltrop? plant;
        private Animator? animator;
        private float timer;
        private float sporeTimer = -1f;
        private int phase;
        private bool pendingIngredientCleanup = true;
        private readonly List<BounceArc> activeArcs = new();
        private readonly List<Zombie> volleyTargets = new();

        private sealed class BounceArc
        {
            internal readonly Zombie Zombie;
            internal readonly Vector3 Start;
            internal readonly float TargetX;
            internal readonly float Height;
            internal float Elapsed;

            internal BounceArc(Zombie zombie, float targetX, float height)
            {
                Zombie = zombie;
                Start = zombie.transform.position;
                TargetX = targetX;
                Height = height;
            }
        }

        public BumpingShroom(IntPtr pointer) : base(pointer) { }

        public void Start()
        {
            plant = GetComponent<Caltrop>();
            animator = Core.V11PlantsBootstrap.ApplyExactLocalAnimationController(
                gameObject,
                "Bumping-shroom"
            );
            Play("idle");
        }

        private void Play(string state)
        {
            if (animator != null)
                animator.Play("Base Layer." + state, 0, 0);
        }

        public void Update()
        {
            if (plant == null || plant.dying || !plant.Active ||
                !SuperaquaShroom.IsLiveBoardPlant(plant))
            {
                return;
            }

            if (pendingIngredientCleanup)
            {
                pendingIngredientCleanup = false;
                Core.BumpingShroomBootstrap
                    .ConsumeRemainingLobIngredients(plant);
            }

            if (sporeTimer >= 0f)
            {
                sporeTimer -= Time.deltaTime;
                if (sporeTimer <= 0f)
                {
                    sporeTimer = -1f;
                    FireThreeSpores();
                }
            }

            timer -= Time.deltaTime;
            if (phase == 1 && timer <= 0f)
            {
                AnimAttack();
                return;
            }
            if (phase == 2 && timer <= 0f)
            {
                phase = 3;
                timer = RestSeconds - RestAnimationSeconds;
                Play("rest-idle");
                return;
            }
            if (phase == 3 && timer <= 0f)
            {
                phase = 4;
                timer = WakeAnimationSeconds;
                Play("wake-up");
                return;
            }
            if (phase == 4 && timer <= 0f)
            {
                phase = 0;
                Play("idle");
            }

            if (phase != 0)
                return;

            foreach (Zombie zombie in Lawnf.GetAllZombies(false))
            {
                if (!InRange(zombie))
                    continue;

                phase = 1;
                timer = 0.75f;
                Play("attack");
                break;
            }
        }

        private bool InRange(Zombie zombie)
        {
            return plant != null && zombie != null && zombie.Alive &&
                !zombie.isMindControlled &&
                zombie.theZombieRow == plant.thePlantRow &&
                Mathf.Abs(zombie.transform.position.x -
                    transform.position.x) < 0.55f &&
                zombie.theZombieType != ZombieType.BalloonZombie &&
                zombie.theZombieType != ZombieType.BungiZombie;
        }

        public void LateUpdate()
        {
            for (int index = activeArcs.Count - 1; index >= 0; index--)
            {
                BounceArc arc = activeArcs[index];
                try
                {
                    if (arc.Zombie == null || !arc.Zombie.Alive)
                    {
                        activeArcs.RemoveAt(index);
                        continue;
                    }

                    arc.Elapsed += Time.deltaTime;
                    float progress = Mathf.Clamp01(
                        arc.Elapsed / BounceFlightSeconds
                    );
                    Vector3 position = arc.Start;
                    position.x = Mathf.Lerp(
                        arc.Start.x,
                        arc.TargetX,
                        Mathf.SmoothStep(0f, 1f, progress)
                    );
                    position.y += Mathf.Sin(Mathf.PI * progress) * arc.Height;
                    arc.Zombie.transform.position = position;

                    if (progress >= 1f)
                        activeArcs.RemoveAt(index);
                }
                catch
                {
                    activeArcs.RemoveAt(index);
                }
            }
        }

        public void OnDestroy()
        {
            for (int index = 0; index < activeArcs.Count; index++)
            {
                BounceArc arc = activeArcs[index];
                try
                {
                    if (arc.Zombie == null || !arc.Zombie.Alive)
                        continue;

                    Vector3 position = arc.Start;
                    position.x = arc.TargetX;
                    arc.Zombie.transform.position = position;
                }
                catch { }
            }

            activeArcs.Clear();
            volleyTargets.Clear();
        }

        private void LaunchArc(Zombie zombie)
        {
            float tileWidth = Mathf.Abs(
                Lawnf.GetBoxXFromColumn(1) - Lawnf.GetBoxXFromColumn(0)
            );
            float rightEdge = Lawnf.GetBoxXFromColumn(
                Mathf.Max(1, Board.Instance.columnNum - 1)
            ) + tileWidth;
            float targetX = Mathf.Min(
                zombie.transform.position.x +
                    tileWidth * BounceDistanceInTiles,
                rightEdge
            );

            activeArcs.Add(new BounceArc(
                zombie,
                targetX,
                tileWidth * 0.55f
            ));
            volleyTargets.Add(zombie);
        }

        public void AnimAttack()
        {
            if (phase != 1 || plant == null)
                return;

            phase = 2;
            timer = RestAnimationSeconds;
            volleyTargets.Clear();

            foreach (Zombie zombie in Lawnf.GetAllZombies(false))
            {
                if (InRange(zombie))
                    LaunchArc(zombie);
            }

            if (volleyTargets.Count > 0)
                sporeTimer = SporeDelayAfterBounce;

            Play("rest");
        }

        private void FireThreeSpores()
        {
            if (plant == null || plant.dying)
                return;

            // Each post-bounce spore selects one of the three closest living
            // zombies in Bumping-shroom's lane, rather than blindly following
            // whichever zombie triggered the spring.
            var validTargets = new List<Zombie>();
            foreach (Zombie zombie in Lawnf.GetZombiesByRow(
                plant.thePlantRow,
                false
            ))
            {
                if (zombie != null && zombie.Alive &&
                    !zombie.isMindControlled &&
                    zombie.theZombieType != ZombieType.BungiZombie)
                {
                    validTargets.Add(zombie);
                }
            }

            if (validTargets.Count == 0)
                return;

            float sourceX = plant.transform.position.x;
            validTargets.Sort((left, right) =>
                Mathf.Abs(left.transform.position.x - sourceX).CompareTo(
                    Mathf.Abs(right.transform.position.x - sourceX)
                )
            );

            if (validTargets.Count > 3)
                validTargets.RemoveRange(3, validTargets.Count - 3);

            CreateBullet? creator = CreateBullet.Instance;
            if (creator == null && Board.Instance != null)
                creator = Board.Instance.GetComponent<CreateBullet>();
            if (creator == null)
                return;

            Transform? shoot = transform.Find("shoot");
            Vector3 origin = shoot != null
                ? shoot.position
                : transform.position + new Vector3(0f, 0.35f, 0f);

            for (int shot = 0; shot < 3; shot++)
            {
                Zombie target = validTargets[shot % validTargets.Count];
                TryFireSpore(creator, origin, target, shot);
            }
        }

        private void TryFireSpore(
            CreateBullet creator,
            Vector3 origin,
            Zombie target,
            int shot
        )
        {
            try
            {
                Vector3 shotOrigin = origin;
                shotOrigin.x += (shot - 1) * 0.08f;
                Bullet bullet = creator.SetBullet(
                    shotOrigin.x,
                    shotOrigin.y,
                    plant!.thePlantRow,
                    BulletType.Bullet_puff,
                    BulletMoveWay.Throw,
                    false
                );

                if (bullet == null)
                    return;

                bullet.from = plant;
                bullet.fromType = (PlantType)ID;
                bullet.theBulletRow = plant.thePlantRow;
                bullet.Damage = 20;

                var projectileOrigin = new Il2CppSystem.Nullable<Vector2>(
                    new Vector2(shotOrigin.x, shotOrigin.y)
                );
                var flightTime = new Il2CppSystem.Nullable<float>(
                    SporeFlightSeconds + shot * 0.04f
                );
                bullet.ThrowTo(target, projectileOrigin, flightTime);
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Bumping-shroom] Spore shot failed safely: " +
                    exception.Message
                );
            }
        }
    }
}
