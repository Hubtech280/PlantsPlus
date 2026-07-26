using HarmonyLib;
using Il2Cpp;
using PlantsPlus.Core;
using UnityEngine;

namespace PlantsPlus.Plants
{
    /// <summary>
    /// Drives saw impacts and attached-saw timers from Board.Update. The custom
    /// visual projectile now uses a native Bullet_pea plus a companion
    /// controller, so pooled bullets never contain a managed Bullet subclass.
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

            // Suppress Bullet's native one-hit collision only for the saw.
            // Movement and off-board cleanup remain owned by native Bullet_pea.
            return false;
        }

        [HarmonyPatch(typeof(Board), nameof(Board.Update))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void BoardUpdatePostfix()
        {
            try
            {
                V11PlantsBootstrap.TickAttachedSaws();
            }
            catch (System.Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Not-a-pea] Attachment manager frame skipped safely: " +
                    exception.Message
                );
            }
        }

        [HarmonyPatch(typeof(Board), nameof(Board.OnDestroy))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void BoardDestroyPrefix()
        {
            try
            {
                V11PlantsBootstrap.ClearAttachedSaws();
            }
            catch
            {
                // The native board is already being destroyed.
            }
        }
    }
}
