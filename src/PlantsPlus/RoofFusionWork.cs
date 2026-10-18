using System;
using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace PlantsPlus.Core
{
    public sealed class RoofFusionRunner : MonoBehaviour
    {
        public RoofFusionRunner(IntPtr pointer) : base(pointer) { }
        public void Update() => RoofFusionWork.Tick(gameObject);
    }

    internal static class RoofFusionWork
    {
        private sealed class Job
        {
            internal Board Board = null!;
            internal Plant Parent = null!;
            internal Plant? Second;
            internal int Result, Column, Row, Frames = 1;
        }
        private static readonly List<Job> pending = new();
        internal static void Observe(Plant source)
        {
            Board? board = Board.Instance;
            if (!NightRoofSupportedFusion.IsRoof(board) || source == null ||
                (int)source.thePlantType != Plants.BounceShroom.ID ||
                !Plants.SuperaquaShroom.IsLiveBoardPlant(source)) return;
            int column = source.thePlantColumn, row = source.thePlantRow;
            if (BumpingShroomBootstrap.CountLobShrooms(column, row) >= 3) return;
            var plants = Lawnf.Get1x1Plants(column, row);
            for (int i = 0; plants != null && i < plants.Count; i++)
            {
                Plant other = plants[i];
                if (other == null || other == source || other.dying || other.thePlantHealth <= 0) continue;
                int result = NightRoofSupportedFusion.Result((int)source.thePlantType, (int)other.thePlantType);
                if (result < 0) continue;
                Queue(board!, source, result, column, row, other);
                return;
            }
        }

        internal static void Queue(Board board, Plant parent, int result, int column, int row, Plant? second = null)
        {
            foreach (var job in pending)
                if (job.Board == board && job.Column == column && job.Row == row)
                {
                    if (second != null && job.Second == null)
                        job.Second = job.Parent == parent ? second : parent;
                    return;
                }
            pending.Add(new Job { Board = board, Parent = parent, Second = second,
                Result = result, Column = column, Row = row });
            Plugin.Logger.LogInfo("[Roof Fusion] Queued result=" + result + " | Actual ingredients=" +
                (second == null ? "1 + incoming card" : "2 placed plants") + " | Tile=" + column + "," + row);
            if (board.GetComponent<RoofFusionRunner>() == null)
                board.gameObject.AddComponent<RoofFusionRunner>();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static void Tick(GameObject owner)
        {
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                Job job = pending[i];
                if (job.Board == null || job.Board != Board.Instance) { pending.RemoveAt(i); continue; }
                if (job.Board.gameObject != owner || job.Frames-- > 0) continue;
                pending.RemoveAt(i);
                try
                {
                    if (job.Parent == null || job.Parent.dying || job.Parent.thePlantColumn != job.Column ||
                        job.Parent.thePlantRow != job.Row || CreatePlant.Instance == null) continue;
                    if (job.Second != null && (job.Second.dying || job.Second.thePlantColumn != job.Column ||
                        job.Second.thePlantRow != job.Row)) continue;
                    Plants.BumpingIngredientRemoval.Validate();
                    Plants.BumpingIngredientRemoval.ByMix(job.Parent);
                    if (job.Second != null) Plants.BumpingIngredientRemoval.ByMix(job.Second);
                    Plant created = CreatePlant.Instance.SetPlant(job.Column, job.Row,
                        (PlantType)job.Result, null, default, true, false, null);
                    Plugin.Logger.LogInfo("[Roof Fusion] Expected=" + job.Result + " | Actual=" +
                        (created == null ? "null" : ((int)created.thePlantType).ToString()));
                }
                catch (Exception error) { Plugin.Logger.LogError("[Roof Fusion] " + error); }
            }
        }
    }
}
