using CustomizeLib.BepInEx;
using Il2Cpp;
using PlantsPlus.Plants;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlantsPlus.Core
{
    public static class PlantRegister
    {
        
        public const int BambnutID = 6001;

        public static bool RegisterPlants()
        {
            if (!ValidatePlantIDs())
            {
                Plugin.Logger.LogError(
                    "[Plants+] Registration aborted before touching game data."
                );
                return false;
            }

            RegisterLotusPumpkin();
            RegisterBambNut();
            RegisterIcebergShroom();
            RegisterWitchfirePumpkin();
            if (RegisterNuttySharpshooterProjectile())
            {
                RegisterNuttySharpshooter();
            }
            else
            {
                Plugin.Logger.LogError(
                    "[Nutty Sharpshooter] Plant registration skipped " +
                    "because its projectile could not be registered."
                );
            }
            RegisterInfernoTorchflower();
            RegisterPumpkinPodbomber();
#if ENABLE_MAGNETOPEA
            RegisterMagnetOPeaProjectileVisual();
            RegisterMagnetOPea();
#else
            Plugin.Logger.LogInfo("[Magnet-o-pea] Disabled in this build.");
#endif
            // Plus tard :
            // RegisterFuturePlant();
            // RegisterAnotherPlant();

            return true;
        }

        private static bool ValidatePlantIDs()
        {
            var ids = new (string Name, int ID)[]
            {
                ("Lotus Pumpkin", LotusPumpkin.LotusPumpkinID),
                ("Bambnut", BambnutID),
                ("Iceberg-shroom", IcebergShroom.IcebergShroomID),
                ("Witchfire Pumpkin", WitchfirePumpkin.WitchfirePumpkinID),
                (
                    "Nutty Sharpshooter",
                    NuttySharpshooter.NuttySharpshooterID
                ),
                (
                    "Inferno Torchflower",
                    InfernoTorchflower.InfernoTorchflowerID
                ),
                (
                    "Pumpkin Podbomber",
                    PumpkinPodbomber.PumpkinPodbomberID
                ),
#if ENABLE_MAGNETOPEA
                ("Magnet-o-pea", MagnetOPea.MagnetOPeaID),
#endif
            };

            var seen = new HashSet<int>();
            bool valid = true;

            foreach (var entry in ids)
            {
                if (!seen.Add(entry.ID))
                {
                    valid = false;
                    Plugin.Logger.LogError(
                        "[Plants+] Duplicate custom plant ID " + entry.ID +
                        " inside Plants+ (" + entry.Name + ")."
                    );
                    continue;
                }

                if (Enum.IsDefined(typeof(PlantType), entry.ID))
                {
                    valid = false;
                    Plugin.Logger.LogError(
                        "[Plants+] Plant ID collision | " + entry.Name +
                        " = " + entry.ID +
                        " | Vanilla plant = " + (PlantType)entry.ID
                    );
                }
            }

            if (valid)
            {
                Plugin.Logger.LogInfo(
                    "[Plants+] Plant IDs validated | Lotus Pumpkin = " +
                    LotusPumpkin.LotusPumpkinID +
                    " | Bambnut = " + BambnutID +
                    " | Iceberg-shroom = " + IcebergShroom.IcebergShroomID +
                    " | Witchfire Pumpkin = " +
                    WitchfirePumpkin.WitchfirePumpkinID +
                    " | Nutty Sharpshooter = " +
                    NuttySharpshooter.NuttySharpshooterID +
                    " | Inferno Torchflower = " +
                    InfernoTorchflower.InfernoTorchflowerID +
                    " | Pumpkin Podbomber = " +
                    PumpkinPodbomber.PumpkinPodbomberID
#if ENABLE_MAGNETOPEA
                    + " | Magnet-o-pea = " + MagnetOPea.MagnetOPeaID
#endif
                );
            }

            return valid;
        }

        private static void RegisterCustomPlant<TBase, TBehaviour>(
            int id,
            string bundleName,
            string prefabName,
            string previewName,
            List<(int, int)> recipes,
            float attackInterval,
            float produceInterval,
            int damage,
            int health,
            float cooldown,
            int cost,
            AlmanacEntry almanac
        )
            where TBase : Plant
            where TBehaviour : MonoBehaviour
        {
            var ab = CustomCore.GetAssetBundle(
    Assembly.GetExecutingAssembly(),
    "PlantsPlus.Resources.AssetBundles." + bundleName
);

            if (ab == null)
            {
                Plugin.Logger.LogError(
                    $"[{almanac.Name}] AssetBundle {bundleName} is null."
                );
                return;
            }

            GameObject prefab = ab.GetAsset<GameObject>(prefabName);
            GameObject preview = ab.GetAsset<GameObject>(previewName);

            if (prefab == null || preview == null)
            {
                Plugin.Logger.LogError(
                    $"[{almanac.Name}] Prefab or preview is null."
                );
                return;
            }

            CustomCore.RegisterCustomPlant<TBase, TBehaviour>(
                id,
                prefab,
                preview,
                recipes,
                attackInterval,
                produceInterval,
                damage,
                health,
                cooldown,
                cost
            );

            CustomCore.AddPlantAlmanacStrings(
                (PlantType)id,
                almanac.Name,
                almanac.Info,
                almanac.Introduce,
                cost
            );

            Plugin.Logger.LogInfo(
                $"[{almanac.Name}] Registered successfully!"
            );
        }

        private static void RegisterCustomPlant<TBase>(
    int id,
    string bundleName,
    string prefabName,
    string previewName,
    List<(int, int)> recipes,
    float attackInterval,
    float produceInterval,
    int damage,
    int health,
    float cooldown,
    int cost,
    AlmanacEntry almanac
)
    where TBase : Plant
        {
            var ab = CustomCore.GetAssetBundle(
                Assembly.GetExecutingAssembly(),
                "PlantsPlus.Resources.AssetBundles." + bundleName
            );

            if (ab == null)
            {
                Plugin.Logger.LogError(
                    $"[{almanac.Name}] AssetBundle {bundleName} is null."
                );
                return;
            }

            GameObject prefab = ab.GetAsset<GameObject>(prefabName);
            GameObject preview = ab.GetAsset<GameObject>(previewName);

            if (prefab == null)
            {
                Plugin.Logger.LogError(
                    $"[{almanac.Name}] Prefab {prefabName} is null."
                );
                return;
            }

            if (preview == null)
            {
                Plugin.Logger.LogError(
                    $"[{almanac.Name}] Preview {previewName} is null."
                );
                return;
            }

            CustomCore.RegisterCustomPlant<TBase>(
                id,
                prefab,
                preview,
                recipes,
                attackInterval,
                produceInterval,
                damage,
                health,
                cooldown,
                cost
            );

            CustomCore.AddPlantAlmanacStrings(
                (PlantType)id,
                almanac.Name,
                almanac.Info,
                almanac.Introduce,
                cost
            );

            Plugin.Logger.LogInfo(
                $"[{almanac.Name}] Registered successfully!"
            );
        }

        private static void RegisterLotusPumpkin()
        {
            RegisterCustomPlant<IceLotus, LotusPumpkin>(
                LotusPumpkin.LotusPumpkinID,
                "lotuspumpkin",
                "PumpkinPrefab",
                "PumpkinPreview",
                new List<(int, int)>
{
    ((int)PlantType.Pumpkin, (int)PlantType.IceLotus),
    ((int)PlantType.IceLotus, (int)PlantType.Pumpkin)
},
                0f,
                0f,
                0,
                4000,
                30f,
                125,
                AlmanacContent.LotusPumpkin
            );

            CustomCore.TypeMgrExtra.IsPumpkin.Add((PlantType)LotusPumpkin.LotusPumpkinID);
            CustomCore.TypeMgrExtra.UncrashablePlants.Add((PlantType)LotusPumpkin.LotusPumpkinID);
            CustomCore.TypeMgrExtra.IsIcePlant.Add((PlantType)LotusPumpkin.LotusPumpkinID);
        }

        private static void RegisterBambNut()
        {
            RegisterCustomPlant<Bamboo>(
                BambnutID,
                "bambnut",
                "BambooPrefab",
                "BambooPreview",
                new List<(int, int)>
{
    ((int)PlantType.Bamboo, (int)PlantType.WallNut),
    ((int)PlantType.WallNut, (int)PlantType.Bamboo)
},
                0f,
                0f,
                0,
                4000,
                30f,
                125,
                AlmanacContent.Bambnut
            );

            CustomCore.TypeMgrExtra.IsNut.Add((PlantType)BambnutID);
        }

        private static void RegisterIcebergShroom()
        {
            RegisterCustomPlant<IceShroom, IcebergShroom>(
                IcebergShroom.IcebergShroomID,
                "icebergshroom",
                "IceShroomPrefab",
                "IceShroomPreview",
                new List<(int, int)>
                {
                    ((int)PlantType.IceShroom, (int)PlantType.IceShroom)
                },
                0f,
                0f,
                IcebergShroom.Damage,
                300,
                50f,
                150,
                AlmanacContent.IcebergShroom
            );

            CustomCore.TypeMgrExtra.IsIcePlant.Add(
                (PlantType)IcebergShroom.IcebergShroomID
            );
        }

        private static void RegisterWitchfirePumpkin()
        {
            RegisterCustomPlant<JalaPumpkin, WitchfirePumpkin>(
                WitchfirePumpkin.WitchfirePumpkinID,
                "witchfire_pumpkin",
                "JalaPumpkinPrefab",
                "JalaPumpkinPreview",
                new List<(int, int)>
                {
                    (
                        (int)PlantType.JalaPumpkin,
                        (int)PlantType.DoomPumpkin
                    ),
                    (
                        (int)PlantType.DoomPumpkin,
                        (int)PlantType.JalaPumpkin
                    )
                },
                0f,
                0f,
                WitchfirePumpkin.BiteDamage,
                WitchfirePumpkin.Toughness,
                WitchfirePumpkin.CardRecharge,
                WitchfirePumpkin.CardCost,
                AlmanacContent.WitchfirePumpkin
            );

            PlantType customType =
                (PlantType)WitchfirePumpkin.WitchfirePumpkinID;

            CustomCore.TypeMgrExtra.IsPumpkin.Add(customType);
            CustomCore.TypeMgrExtra.IsFirePlant.Add(customType);
            CustomCore.TypeMgrExtra.UncrashablePlants.Add(customType);

            RegisterWitchfireOdysseyBuffs(customType);

            // Register this after the buffs: RegisterCustomBuff rebuilds the
            // native Travel dictionaries, so doing it before that can leave
            // the Almanac's weak-Odyssey filter without Witchfire metadata.
            RegisterWitchfireAsWeakOdyssey(customType);
        }

        private static bool RegisterNuttySharpshooterProjectile()
        {
            try
            {
                BulletType bulletType =
                    (BulletType)NuttySharpshooter.NuttyBulletID;

                if (Enum.IsDefined(typeof(BulletType), (int)bulletType))
                {
                    Plugin.Logger.LogError(
                        "[Nutty Sharpshooter] Bullet ID collision | ID = " +
                        NuttySharpshooter.NuttyBulletID +
                        " | Vanilla bullet = " + bulletType
                    );
                    return false;
                }

                if (CustomCore.CustomBullets.ContainsKey(bulletType))
                {
                    Plugin.Logger.LogError(
                        "[Nutty Sharpshooter] Bullet ID " +
                        NuttySharpshooter.NuttyBulletID +
                        " is already registered by another mod."
                    );
                    return false;
                }

                var bundle = CustomCore.GetAssetBundle(
                    Assembly.GetExecutingAssembly(),
                    "PlantsPlus.Resources.AssetBundles.nss_bullet"
                );

                if (bundle == null)
                {
                    Plugin.Logger.LogError(
                        "[Nutty Sharpshooter] Projectile AssetBundle " +
                        "nss_bullet is null."
                    );
                    return false;
                }

                GameObject prefab =
                    bundle.GetAsset<GameObject>("Bullet_spruce");

                if (prefab == null)
                {
                    Plugin.Logger.LogError(
                        "[Nutty Sharpshooter] Projectile prefab " +
                        "Bullet_spruce is null."
                    );
                    return false;
                }

                if (prefab.GetComponent<Collider2D>() == null)
                {
                    Plugin.Logger.LogError(
                        "[Nutty Sharpshooter] Projectile prefab has no " +
                        "Collider2D; registration was cancelled safely."
                    );
                    return false;
                }

                // Do not use Bullet_spruce here: its native HitZombie method
                // bypasses handheld armor. Bullet_pierce keeps the desired
                // penetration lifecycle and the game's normal armor path.
                CustomCore.RegisterCustomBullet<Bullet_pierce>(
                    bulletType,
                    prefab
                );

                bool registered =
                    CustomCore.CustomBullets.ContainsKey(bulletType);

                if (registered)
                {
                    Plugin.Logger.LogInfo(
                        "[Nutty Sharpshooter] Projectile registered" +
                        " | ID = " + NuttySharpshooter.NuttyBulletID +
                        " | Prefab = Bullet_spruce" +
                        " | Runtime = Bullet_pierce" +
                        " | Armor bypass = false"
                    );
                }

                return registered;
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Nutty Sharpshooter] Projectile registration failed: " +
                    exception
                );
                return false;
            }
        }

        private static void RegisterNuttySharpshooter()
        {
            RegisterCustomPlant<SpruceShooter, NuttySharpshooter>(
                NuttySharpshooter.NuttySharpshooterID,
                "nuttysharpshooter",
                "SpruceShooterPrefab",
                "SpruceShooterPreview",
                new List<(int, int)>
                {
                    (
                        (int)PlantType.SpruceShooter,
                        (int)PlantType.WallNut
                    ),
                    (
                        (int)PlantType.WallNut,
                        (int)PlantType.SpruceShooter
                    )
                },
                NuttySharpshooter.AttackInterval,
                0f,
                NuttySharpshooter.Damage,
                NuttySharpshooter.Toughness,
                NuttySharpshooter.CardRecharge,
                NuttySharpshooter.CardCost,
                AlmanacContent.NuttySharpshooter
            );

            PlantType customType =
                (PlantType)NuttySharpshooter.NuttySharpshooterID;

            CustomCore.TypeMgrExtra.IsNut.Add(customType);
            CustomCore.TypeMgrExtra.IsIcePlant.Add(customType);
        }

        private static void RegisterInfernoTorchflower()
        {
            RegisterCustomPlant<TorchSunflower, InfernoTorchflower>(
                InfernoTorchflower.InfernoTorchflowerID,
                "infernotorchflower",
                "TorchSunflowerPrefab",
                "TorchSunflowerPreview",
                new List<(int, int)>
                {
                    (
                        (int)PlantType.SuperTorch,
                        (int)PlantType.SunFlower
                    ),
                    (
                        (int)PlantType.SunFlower,
                        (int)PlantType.SuperTorch
                    )
                },
                0f,
                InfernoTorchflower.ProduceInterval,
                0,
                InfernoTorchflower.FallbackToughness,
                InfernoTorchflower.FallbackCardRecharge,
                InfernoTorchflower.FallbackCardCost,
                AlmanacContent.InfernoTorchflower
            );

            PlantType customType =
                (PlantType)InfernoTorchflower.InfernoTorchflowerID;

            if (!CustomCore.CustomPlants.ContainsKey(customType))
            {
                Plugin.Logger.LogError(
                    "[Inferno Torchflower] Conversion registration skipped " +
                    "because the plant prefab was not registered."
                );
                return;
            }

            // Infernowood + Sunflower -> Inferno Torchflower is registered
            // above. The reverse conversion is intercepted by the focused
            // MixData.TryGetMix patch in PumpkinPodbomber.cs instead of being
            // inserted as a normal recipe. A normal reverse recipe would make
            // CustomizeLib replace Infernowood's native disassembly parents
            // and create a Recycling duplication loop.

            CustomCore.CustomPlantClicks[customType] =
                InfernoTorchflower.HandleClick;

            CustomCore.TypeMgrExtra.IsFirePlant.Add(customType);
            CustomCore.TypeMgrExtra.UncrashablePlants.Add(customType);

            Plugin.Logger.LogInfo(
                "[Inferno Torchflower] Advanced Alt conversion active" +
                " | Infernowood + Sunflower -> Inferno Torchflower" +
                " | Inferno Torchflower + Torchwood -> Infernowood"
            );
        }

        private static void RegisterPumpkinPodbomber()
        {
            RegisterCustomPlant<PeaPumpkin, PumpkinPodbomber>(
                PumpkinPodbomber.PumpkinPodbomberID,
                "pumpkinpodbomber",
                "PeaPumpkinPrefab",
                "PeaPumpkinPreview",
                new List<(int, int)>
                {
                    (
                        (int)PlantType.SuperCherryShooter,
                        (int)PlantType.Pumpkin
                    ),
                    (
                        (int)PlantType.Pumpkin,
                        (int)PlantType.SuperCherryShooter
                    )
                },
                PumpkinPodbomber.FallbackAttackInterval,
                0f,
                0,
                PumpkinPodbomber.FallbackToughness,
                PumpkinPodbomber.FallbackCardRecharge,
                PumpkinPodbomber.FallbackCardCost,
                AlmanacContent.PumpkinPodbomber
            );

            PlantType customType =
                (PlantType)PumpkinPodbomber.PumpkinPodbomberID;

            CustomCore.TypeMgrExtra.IsPumpkin.Add(customType);
            CustomCore.TypeMgrExtra.UncrashablePlants.Add(customType);

            Plugin.Logger.LogInfo(
                "[Pumpkin Podbomber] Advanced Alt conversion active" +
                " | Explode-o-shooter + Pumpkin -> Pumpkin Podbomber" +
                " | Shovel -> Explode-o-shooter card"
            );
        }

        private static void RegisterWitchfireAsWeakOdyssey(
            PlantType witchfireType
        )
        {
            try
            {
                if (!CustomCore.CustomUltimatePlants.Contains(witchfireType))
                    CustomCore.AddUltimatePlant(witchfireType);

                // Mirror the native metadata used by Odyssey plants. Item4
                // is the strong-Odyssey flag, therefore false means weak.
                if (!TravelDictionary.PlantInfo.ContainsKey(witchfireType))
                {
                    TravelDictionary.PlantInfo.Add(
                        witchfireType,
                        new Il2CppSystem.ValueTuple<
                            Il2CppSystem.Nullable<PlantType>,
                            Il2CppSystem.Object,
                            Il2CppSystem.Object,
                            bool
                        >(
                            new Il2CppSystem.Nullable<PlantType>(
                                witchfireType
                            ),
                            null!,
                            null!,
                            false
                        )
                    );
                }
                else
                {
                    TravelDictionary.PlantInfo[witchfireType].Item4 = false;
                }

                if (TravelDictionary.allStrongUltimtePlant.Contains(
                        witchfireType
                    ))
                {
                    TravelDictionary.allStrongUltimtePlant.Remove(
                        witchfireType
                    );
                }

                Plugin.Logger.LogInfo(
                    "[Witchfire Pumpkin] Weak Odyssey registration active" +
                    " | CustomUltimate = " +
                    CustomCore.CustomUltimatePlants.Contains(witchfireType) +
                    " | Strong = false"
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Witchfire Pumpkin] Weak Odyssey native metadata " +
                    "failed safely: " + exception.Message
                );
            }
        }

        internal static void RefreshWitchfireWeakOdysseyRegistration()
        {
            RegisterWitchfireAsWeakOdyssey(
                (PlantType)WitchfirePumpkin.WitchfirePumpkinID
            );
        }

        private static void RegisterWitchfireOdysseyBuffs(
            PlantType witchfireType
        )
        {
            try
            {
                int grenadesBuff = CustomCore.RegisterCustomBuff(
                    "Grenades: replaces Witchfire Pumpkin's local Doom " +
                    "explosion with a Doom Bomb thrown into every lane.",
                    BuffType.AdvancedBuff,
                    () => true,
                    0,
                    witchfireType,
                    true,
                    witchfireType,
                    1,
                    default
                );

                int radiationBuff = CustomCore.RegisterCustomBuff(
                    "Radiation: deals 100 damage every 0.2 seconds in a " +
                    "1x1 area. Damage and radius grow whenever Witchfire " +
                    "Pumpkin or its protected plant kills a zombie.",
                    BuffType.AdvancedBuff,
                    () => true,
                    0,
                    witchfireType,
                    true,
                    witchfireType,
                    1,
                    default
                );

                CustomCore.SetCustomBuffAlmanacType(
                    BuffType.AdvancedBuff,
                    grenadesBuff,
                    AlmanacBuffType.General,
                    witchfireType
                );
                CustomCore.SetCustomBuffAlmanacType(
                    BuffType.AdvancedBuff,
                    radiationBuff,
                    AlmanacBuffType.General,
                    witchfireType
                );

                WitchfirePumpkin.SetOdysseyBuffIDs(
                    grenadesBuff,
                    radiationBuff
                );

                Plugin.Logger.LogInfo(
                    "[Witchfire Pumpkin] Odyssey buffs registered" +
                    " | Grenades (Advanced) = " + grenadesBuff +
                    " | Radiation (Advanced) = " + radiationBuff
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[Witchfire Pumpkin] Odyssey buff registration failed " +
                    "safely: " + exception.Message
                );
            }
        }

#if ENABLE_MAGNETOPEA
        private static void RegisterMagnetOPeaProjectileVisual()
        {
            const string bundleName = "bullet_magnetopea";
            const string prefabName = "Bullet_pea";

            var bundle = CustomCore.GetAssetBundle(
                Assembly.GetExecutingAssembly(),
                "PlantsPlus.Resources.AssetBundles." + bundleName
            );

            if (bundle == null)
            {
                Plugin.Logger.LogError(
                    "[Magnet-o-pea] Projectile AssetBundle is null. " +
                    "The complete vanilla pea visual will be used."
                );
                return;
            }

            GameObject prefab = bundle.GetAsset<GameObject>(prefabName);

            if (prefab == null)
            {
                Plugin.Logger.LogError(
                    "[Magnet-o-pea] Projectile prefab " + prefabName +
                    " is null. The complete vanilla pea visual will be used."
                );
                return;
            }

            Transform visualTransform = prefab.transform.FindChild("Sprite");
            Transform shadowTransform = prefab.transform.FindChild("Shadow");

            SpriteRenderer? visual = visualTransform != null
                ? visualTransform.GetComponent<SpriteRenderer>()
                : null;

            SpriteRenderer? shadow = shadowTransform != null
                ? shadowTransform.GetComponent<SpriteRenderer>()
                : null;

            if (visual == null)
            {
                Plugin.Logger.LogError(
                    "[Magnet-o-pea] The custom projectile has no Sprite " +
                    "renderer. The complete vanilla pea visual will be used."
                );
                return;
            }

            MagnetOPea.ConfigureNormalPeaVisual(visual, shadow);

            Plugin.Logger.LogInfo(
                "[Magnet-o-pea] Normal pea visual loaded successfully" +
                " | Runtime behaviour = " + BulletType.Bullet_pea +
                " | No custom Bullet component"
            );
        }

        private static void RegisterMagnetOPea()
        {
            RegisterCustomPlant<PeaShooter, MagnetOPea>(
                MagnetOPea.MagnetOPeaID,
                "magnetopea",
                "PeashooterPrefab",
                "PeaShooterPreview",
                new List<(int, int)>
                {
                    ((int)PlantType.Magnetshroom, (int)PlantType.Peashooter),
                    ((int)PlantType.Peashooter, (int)PlantType.Magnetshroom)
                },
                MagnetOPea.AttackInterval,
                0f,
                MagnetOPea.BaseDamage,
                300,
                7.5f,
                125,
                AlmanacContent.MagnetOPea
            );

        }
#endif
    }
}
