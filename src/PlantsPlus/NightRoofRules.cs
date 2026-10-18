using HarmonyLib;
using Il2Cpp;
using Il2CppPlaceRule;
using System;
using System.Collections.Generic;

namespace PlantsPlus.Core
{
    internal static class NightRoofSupportedFusion
    {
        internal static bool IsRoofScene(int scene) => scene == (int)SceneType.Roof || scene == (int)SceneType.NightRoof;
        internal static bool IsRoof(Board? board) => board != null && IsRoofScene((int)board.sceneType);
        internal static BoardGrid? support;
        internal static int Result(int a, int b)
        {
            int other = a == Plants.BounceShroom.ID ? b : b == Plants.BounceShroom.ID ? a : -1;
            if (other == (int)PlantType.SunFlower) return Plants.SolsticeShroom.ID;
            if (other == (int)PlantType.ElectricOnion) return Plants.SparkShroom.ID;
            if (other == Plants.BorealOrchid.BorealOrchidID) return Plants.PulseShroom.ID;
            return -1;
        }

        internal static Plant? Parent(int column, int row, PlantType used, out int result)
        {
            result = -1;
            var plants = Lawnf.Get1x1Plants(column, row);
            for (int i = 0; plants != null && i < plants.Count; i++)
            {
                Plant plant = plants[i];
                if (plant == null || plant.dying || plant.thePlantHealth <= 0) continue;
                result = Result((int)plant.thePlantType, (int)used);
                if (result >= 0) return plant;
            }
            return null;
        }

        internal static bool ValidSupport(Board board, BoardGrid grid, bool replacing)
        {
            // This family is explicitly allowed on roof tiles WITH OR WITHOUT
            // a pot. Retain obstacle/ice/space rules, not ordinary-plant soil
            // support restrictions or the vanilla Caltrop roof prohibition.
            if (grid.pitNum > 0 || grid.graveNum > 0) return false;
            var rules = PlaceManager.GetRules(PlantType.Caltrop);
            var collection = rules.Cast<Il2CppSystem.Collections.Generic.IReadOnlyCollection<IPlantPlaceRule>>();
            for (int i = 0; i < collection.Count; i++)
            {
                var rule = rules[i];
                if (replacing && rule.TryCast<EmptyLayerRule>() != null) continue;
                if (rule.TryCast<RoofRule>() != null || rule.TryCast<OrdinaryTerrainRule>() != null ||
                    rule.TryCast<PotTerrainRule>() != null) continue;
                if (!rule.Check(board, grid, PlantType.Caltrop)) return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CreatePlant), nameof(CreatePlant.CheckBox))]
        private static class Placement
        {
            [HarmonyPrefix]
            private static void Prefix(int theBoxColumn, int theBoxRow, out BoardGrid? __state)
            {
                __state = support;
                Board? board = Board.Instance;
                support = IsRoof(board) && board!.gridSystem != null
                    ? board.gridSystem.GetGrid(theBoxColumn, theBoxRow) : null;
            }
            [HarmonyPostfix, HarmonyPriority(Priority.Last)]
            private static void Postfix(int theBoxColumn, int theBoxRow, PlantType theSeedType, ref bool __result)
            {
                Board? board = Board.Instance;
                if (!IsRoof(board) || support == null) return;
                int result;
                Plant? parent = Parent(theBoxColumn, theBoxRow, theSeedType, out result);
                if (parent != null) __result = ValidSupport(board!, support, true);
                else if (NightRoofBounceSupport.IsFamily((int)theSeedType))
                    __result = ValidSupport(board!, support,
                        (int)theSeedType == Plants.BounceShroom.ID &&
                        BumpingShroomBootstrap.CountLobShrooms(theBoxColumn, theBoxRow) >= 3);
            }
            [HarmonyFinalizer]
            private static void Finalizer(BoardGrid? __state) { support = __state; }
        }

        [HarmonyPatch(typeof(CreatePlant), nameof(CreatePlant.OnHardLand))]
        private static class PottedSupport
        {
            [HarmonyPostfix]
            private static void Postfix(PlantType __0, ref bool __result)
            {
                if (IsRoof(Board.Instance) && support != null &&
                    NightRoofBounceSupport.IsFamily((int)__0)) __result = false;
            }
        }

        [HarmonyPatch(typeof(CreatePlant), "CheckMix")]
        private static class Fusion
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            private static bool Prefix(CreatePlant __instance, int theColumn, int theRow,
                PlantType theUsedType, ref Plant __result)
            {
                Board? board = Board.Instance;
                if (!IsRoof(board) || board!.gridSystem == null) return true;
                int result;
                Plant? parent = Parent(theColumn, theRow, theUsedType, out result);
                if (parent == null) return true;
                var grid = board.gridSystem.GetGrid(theColumn, theRow);
                if (grid == null || !ValidSupport(board, grid, true)) { __result = null!; return false; }
                // Finish after the native input stack has unwound, just like
                // the four-ingredient fusion. Never create inside CheckMix.
                RoofFusionWork.Queue(board, parent, result, theColumn, theRow);
                __result = parent;
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(CreatePlant), nameof(CreatePlant.CheckBox))]
    internal static class NightRoofFusionPlacementTrace
    {
        private static Board? lastBoard;
        private static readonly HashSet<string> reported = new();
        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        private static void Postfix(int __0, int __1, PlantType __2, bool __result)
        {
            Board? board = Board.Instance;
            if (__result || !NightRoofMoon.IsOurLevel(board) || board!.gridSystem == null) return;
            if (lastBoard != board) { lastBoard = board; reported.Clear(); }
            if (reported.Count >= 12) return;
            if (!NightRoofBounceSupport.IsFamily((int)__2) && __2 != PlantType.SunFlower &&
                __2 != PlantType.ElectricOnion && (int)__2 != Plants.BorealOrchid.BorealOrchidID &&
                (int)__2 != Plants.LobShroom.ID) return;
            try
            {
                var grid = board.gridSystem.GetGrid(__0, __1);
                if (grid == null) return;
                var plants = Lawnf.Get1x1Plants(__0, __1);
                var contents = new List<string>();
                for (int i = 0; plants != null && i < plants.Count; i++)
                {
                    var plant = plants[i];
                    if (plant == null || plant.dying) continue;
                    PlantType result;
                    bool recipe = MixData.TryGetMix(plant.thePlantType, __2, out result, true);
                    contents.Add((int)plant.thePlantType + ":mix=" + (recipe ? ((int)result).ToString() : "none"));
                }
                string message = "Card=" + (int)__2 + " | Pot=" + grid.potNum +
                    " | Tile=" + string.Join(",", contents);
                if (reported.Add(message)) Plugin.Logger.LogWarning("[Night Roof CheckBox] " + message);
            }
            catch (Exception error)
            {
                if (reported.Add("error")) Plugin.Logger.LogWarning("[Night Roof CheckBox] " + error.Message);
            }
        }
    }

    // This family may use bare roof tiles or a pot, on both roof scenes.
    [HarmonyPatch(typeof(RoofRule), nameof(RoofRule.Check))]
    internal static class NightRoofBounceSupport
    {
        internal static bool IsFamily(int id) => id == Plants.BounceShroom.ID ||
            id == Plants.BumpingShroom.ID || id == Plants.SparkShroom.ID ||
            id == Plants.PulseShroom.ID || id == Plants.SolsticeShroom.ID;

        [HarmonyPrefix]
        private static bool Prefix(Board __0, BoardGrid __1, PlantType __2, ref bool __result)
        {
            if (!IsFamily((int)__2) || !NightRoofSupportedFusion.IsRoof(__0) || __1 == null)
                return true;
            // Only replace this terrain rule. Ice/obstacle/space checks remain.
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(PlaceManager), nameof(PlaceManager.CanPlace))]
    internal static class NightRoofBouncePlacementDiagnostic
    {
        private static Board? lastBoard;
        private static readonly HashSet<string> reported = new();
        private static bool inspecting;

        [HarmonyPostfix]
        private static void Postfix(int __0, int __1, PlantType __2, bool __result)
        {
            Board? board = Board.Instance;
            if (__result || inspecting || !NightRoofBounceSupport.IsFamily((int)__2) ||
                !NightRoofMoon.IsOurLevel(board) || board!.gridSystem == null)
                return;
            if (lastBoard != board) { lastBoard = board; reported.Clear(); }
            if (reported.Count >= 6) return;
            try
            {
                inspecting = true;
                BoardGrid grid = board.gridSystem.GetGrid(__0, __1);
                if (grid == null) return;
                var rules = PlaceManager.GetRules(__2);
                var collection = rules.Cast<Il2CppSystem.Collections.Generic.IReadOnlyCollection<IPlantPlaceRule>>();
                var rejected = new List<string>();
                for (int i = 0; i < collection.Count; i++)
                {
                    var rule = rules[i];
                    if (!rule.Check(board, grid, __2)) rejected.Add(rule.Cast<Il2CppSystem.Object>().GetIl2CppType().Name);
                }
                string reason = "Plant=" + (int)__2 + " | Pot=" + grid.potNum + " | Layer=" + PlaceManager.GetLayer(__2) +
                    " | Rejected=" + string.Join(",", rejected);
                if (reported.Add(reason)) Plugin.Logger.LogWarning("[Night Roof Bounce] " + reason);
            }
            catch (Exception error)
            {
                if (reported.Add("diagnostic-error"))
                    Plugin.Logger.LogWarning("[Night Roof Bounce] Placement diagnostic: " + error.Message);
            }
            finally { inspecting = false; }
        }
    }

    // The custom NightRoof scene has roof visuals and BoardTag.isRoof, but
    // the native planting check can still see its cells as ordinary lawn.
    [HarmonyPatch(typeof(CreatePlant), nameof(CreatePlant.CheckBox))]
    internal static class NightRoofPotRule
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            Board? board = Board.Instance;
            if (board != null) NightRoofMoon.EnsureIceRoads(board);
        }

        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        private static void Postfix(
            int theBoxColumn,
            int theBoxRow,
            PlantType theSeedType,
            ref bool __result
        )
        {
            if (!__result)
                return;

            Board? board = Board.Instance;
            if (!NightRoofMoon.IsOurLevel(board) ||
                board!.gridSystem == null ||
                NightRoofBounceSupport.IsFamily((int)theSeedType) ||
                TypeMgr.IsPot(theSeedType) ||
                theSeedType == (PlantType)Plants.SawMyPot.ID ||
                theSeedType == (PlantType)Plants.VortexPot.ID)
                return;

            int fusionResult;
            if (NightRoofSupportedFusion.Parent(theBoxColumn, theBoxRow, theSeedType, out fusionResult) != null)
                return;

            var grid = board.gridSystem.GetGrid(theBoxColumn, theBoxRow);
            if (grid == null || grid.potNum <= 0)
                __result = false;
        }
    }

}
