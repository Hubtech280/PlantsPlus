using HarmonyLib;
using Il2Cpp;
using System;
using UnityEngine;

namespace PlantsPlus.Plants
{
    /// <summary>
    /// Spruce Sharpshooter + Wall-nut. The native SpruceShooter component
    /// keeps the normal targeting and animation cycle, while this bridge
    /// creates a generic piercing projectile so armor is damaged normally.
    /// </summary>
    public class NuttySharpshooter : MonoBehaviour
    {
        public const int NuttySharpshooterID = 6005;
        public const int NuttyBulletID = 6005;

        public const int Damage = 30;
        public const int Toughness = 4000;
        public const int CardCost = 200;
        public const float CardRecharge = 15f;
        public const float AttackInterval = 1.5f;
        public const float KnockbackDistance = 0.5f;

        // One penetration means the projectile can pass through its first
        // target before the native Bullet_pierce lifecycle removes it.
        public const int PenetrationCount = 1;

        private static bool fallbackShotOriginWarningLogged;
        private static bool missingBulletCreatorErrorLogged;
        private static bool directShotLogEmitted;
        private static bool knockbackLogEmitted;
        private static bool knockbackWarningLogged;

        public NuttySharpshooter(IntPtr ptr) : base(ptr) { }

        public SpruceShooter? SprucePlant =>
            gameObject.GetComponent<SpruceShooter>();

        public void Start()
        {
            SpruceShooter? plant = SprucePlant;

            if (plant == null)
            {
                Plugin.Logger.LogError(
                    "[Nutty Sharpshooter] Start failed: no " +
                    "SpruceShooter component."
                );
                return;
            }

            string origin = EnsureShotOrigin(plant);

            Plugin.Logger.LogInfo(
                "[Nutty Sharpshooter] Ready" +
                " | Damage = " + Damage +
                " | Toughness = " + Toughness +
                " | Attack interval = " + AttackInterval +
                " | Pierce = " + PenetrationCount +
                " | Knockback = " + KnockbackDistance +
                " | Armor = normal damage" +
                " | Origin = " + origin
            );
        }

        private static bool IsNuttySharpshooter(Plant? plant)
        {
            return
                plant != null &&
                (int)plant.thePlantType == NuttySharpshooterID;
        }

        private static bool IsNuttyBullet(Bullet? bullet)
        {
            return
                bullet != null &&
                (int)bullet.theBulletType == NuttyBulletID;
        }

        private static Transform? FindShotOrigin(Transform root)
        {
            if (root == null)
                return null;

            for (int index = 0; index < root.childCount; index++)
            {
                Transform child = root.GetChild(index);

                if (child == null)
                    continue;

                string normalizedName = child.name
                    .Replace("_", string.Empty)
                    .Replace(" ", string.Empty)
                    .ToLowerInvariant();

                if (
                    normalizedName.Contains("shoot") ||
                    normalizedName.Contains("muzzle") ||
                    normalizedName.Contains("firepoint") ||
                    normalizedName.Contains("bullet") ||
                    normalizedName == "zidan"
                )
                {
                    return child;
                }
            }

            for (int index = 0; index < root.childCount; index++)
            {
                Transform? result = FindShotOrigin(root.GetChild(index));

                if (result != null)
                    return result;
            }

            return null;
        }

        private static string EnsureShotOrigin(SpruceShooter source)
        {
            if (source.shoot != null)
                return source.shoot.name;

            Transform? recovered = FindShotOrigin(source.transform);

            if (recovered != null)
            {
                source.shoot = recovered;
                return recovered.name + " (recovered)";
            }

            // CustomizeLib adds the native component after loading the bundle,
            // so its serialized shoot reference cannot be restored by Unity.
            source.shoot = source.transform;

            if (!fallbackShotOriginWarningLogged)
            {
                fallbackShotOriginWarningLogged = true;
                Plugin.Logger.LogWarning(
                    "[Nutty Sharpshooter] SpruceShooter.shoot was null and " +
                    "no named muzzle exists. The plant root will be used."
                );
            }

            return "plant root (fallback)";
        }

        private static void ConfigurePiercing(Bullet bullet)
        {
            bullet.hitTimes = 0;
            bullet.penetrationTimes = PenetrationCount;
        }

        private static Bullet? CreateDirectShot(SpruceShooter source)
        {
            EnsureShotOrigin(source);

            Transform origin = source.shoot != null
                ? source.shoot
                : source.transform;

            CreateBullet? creator = CreateBullet.Instance;

            if (creator == null && Board.Instance != null)
                creator = Board.Instance.GetComponent<CreateBullet>();

            if (creator == null)
            {
                if (!missingBulletCreatorErrorLogged)
                {
                    missingBulletCreatorErrorLogged = true;
                    Plugin.Logger.LogError(
                        "[Nutty Sharpshooter] Direct shot failed: " +
                        "CreateBullet is null."
                    );
                }

                return null;
            }

            Vector3 position = origin.position;
            Bullet bullet = creator.SetBullet(
                position.x + 0.1f,
                position.y,
                source.thePlantRow,
                (BulletType)NuttyBulletID,
                BulletMoveWay.MoveRight,
                false
            );

            if (bullet == null)
            {
                Plugin.Logger.LogError(
                    "[Nutty Sharpshooter] Direct shot failed: " +
                    "SetBullet returned null."
                );
                return null;
            }

            bullet.theBulletType = (BulletType)NuttyBulletID;
            bullet.Damage = source.attackDamage > 0
                ? source.attackDamage
                : Damage;
            bullet.shootingLevel = source.shootingLevel;
            bullet.from = source;
            bullet.fromType = (PlantType)NuttySharpshooterID;
            ConfigurePiercing(bullet);

            if (!directShotLogEmitted)
            {
                directShotLogEmitted = true;
                Plugin.Logger.LogInfo(
                    "[Nutty Sharpshooter] Direct piercing shot created" +
                    " | Bullet ID = " + NuttyBulletID +
                    " | Runtime = Bullet_pierce" +
                    " | Damage = " + bullet.Damage +
                    " | Penetrations = " + bullet.penetrationTimes +
                    " | Row = " + source.thePlantRow
                );
            }

            return bullet;
        }

        [HarmonyPatch(typeof(SpruceShooter), nameof(SpruceShooter.Shoot1))]
        private static class SpruceShooter_Shoot1_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static bool Prefix(
                SpruceShooter __instance,
                ref Bullet __result
            )
            {
                if (!IsNuttySharpshooter(__instance))
                    return true;

                try
                {
                    __result = CreateDirectShot(__instance)!;
                }
                catch (Exception exception)
                {
                    __result = null!;
                    Plugin.Logger.LogError(
                        "[Nutty Sharpshooter] Shoot1 override failed: " +
                        exception
                    );
                }

                // The native Spruce shot always creates Bullet_spruce, whose
                // HitZombie override ignores armor. Only the custom ID uses
                // this generic Bullet_pierce replacement.
                return false;
            }
        }

        [HarmonyPatch(typeof(Bullet_pierce), nameof(Bullet_pierce.InitData))]
        private static class BulletPierce_InitData_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(Bullet_pierce __instance)
            {
                if (IsNuttyBullet(__instance))
                    ConfigurePiercing(__instance);
            }
        }

        [HarmonyPatch(typeof(Bullet), nameof(Bullet.HitZombie))]
        private static class Bullet_HitZombie_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(Bullet __instance, Zombie zombie)
            {
                if (!IsNuttyBullet(__instance) || zombie == null)
                    return;

                try
                {
                    zombie.KnockBack(
                        KnockbackDistance,
                        Zombie.KnockBackReason.Normal
                    );

                    if (!knockbackLogEmitted)
                    {
                        knockbackLogEmitted = true;
                        Plugin.Logger.LogInfo(
                            "[Nutty Sharpshooter] Projectile hit verified" +
                            " | Damage = " + __instance.Damage +
                            " | Knockback = " + KnockbackDistance +
                            " | Armor path = native Bullet.HitZombie"
                        );
                    }
                }
                catch (Exception exception)
                {
                    if (knockbackWarningLogged)
                        return;

                    knockbackWarningLogged = true;
                    Plugin.Logger.LogWarning(
                        "[Nutty Sharpshooter] Knockback failed safely: " +
                        exception.Message
                    );
                }
            }
        }
    }
}
