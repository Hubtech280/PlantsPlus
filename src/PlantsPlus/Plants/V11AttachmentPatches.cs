using HarmonyLib;
using Il2Cpp;
using PlantsPlus.Core;
using UnityEngine;

namespace PlantsPlus.Plants
{
    /// <summary>
    /// Collision hook for Not-a-pea saw projectiles.
    ///
    /// IMPORTANT (beta.12.2): attached-saw ticking no longer runs from
    /// Board.Update. The previous global manager could trigger a fatal CLR
    /// failure inside MonoMod/Il2CppInterop while JIT-compiling the large
    /// TickAttachedSaws method. Each projectile and attached visual now owns
    /// its own tiny Update loop instead.
    /// </summary>
    [HarmonyPatch]
    internal static class V11AttachmentPatches
    {
        [HarmonyPatch(typeof(Bullet), nameof(Bullet.OnTriggerEnter2D))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool BulletCollisionPrefix(
            Bullet __instance,
            Collider2D collision
        )
        {
            if (__instance == null || __instance.gameObject == null)
                return true;

            NotAPeaProjectile? controller =
                __instance.gameObject.GetComponent<NotAPeaProjectile>();

            if (controller == null)
                return true;

            controller.HandleCollision(collision);

            // Suppress native one-hit collision only for the saw projectile.
            // The companion controller defers damage by one frame, outside
            // the native physics callback, and keeps traversal bookkeeping.
            return false;
        }
    }
}
