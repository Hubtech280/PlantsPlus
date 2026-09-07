using BepInEx.Logging;
using CustomizeLib.MelonLoader;
using Il2CppInterop.Runtime.Injection;
using PlantsPlus.Core;
using PlantsPlus.Plants;

namespace PlantsPlus
{
    public sealed class Plugin : CorePlugin
    {
        // Keep the original static logging surface used by the plant classes.
        public static new ManualLogSource Logger { get; private set; } = null!;

        public override void OnStart()
        {
            Logger = base.Logger;

            Logger.LogInfo("Plants+ 1.2.0 loaded!");
            Logger.LogInfo(
                "Plants+ assembly path: " +
                typeof(Plugin).Assembly.Location
            );
            Logger.LogInfo(
                "Plants+ build marker: 1.2.0-release"
            );

            NightRoofCards.OnStart();
            NightRoofMap.OnStart();
            SuperLevelEditorPlus.OnStart();
            NightRoofAdventure.OnStart();
            MainMenuBranding.OnStart();
            RegisterIl2CppTypes();
            bool registered = PlantRegister.RegisterPlants();

            if (registered)
            {
#if ENABLE_MAGNETOPEA
                Logger.LogInfo(
                    "Plants+ registration finished " +
                    "(safe IDs; Pumpkin Podbomber, Inferno Torchflower, " +
                    "Nutty Sharpshooter, " +
                    "Witchfire Pumpkin, Iceberg-shroom and Magnet-o-pea " +
                    "enabled)."
                );
#else
                Logger.LogInfo(
                    "Plants+ registration finished " +
                    "(safe IDs; Pumpkin Podbomber, Inferno Torchflower, " +
                    "Nutty Sharpshooter, " +
                    "Witchfire Pumpkin and Iceberg-shroom enabled; " +
                    "Magnet-o-pea disabled in this fallback build)."
                );
#endif
            }
            else
            {
                Logger.LogError(
                    "Plants+ registration failed safely; " +
                    "no custom plant was added."
                );
            }

            FinalPlantsBootstrap.OnStart();
            V11PlantsBootstrap.OnStart();
            V11ExpansionBootstrap.OnStart();
            LogicBloverBootstrap.OnStart();
            SolarSharpshooterBootstrap.OnStart();
            SeaBallistaBootstrap.OnStart();
            PineshooterBootstrap.OnStart();
            IcytronionBootstrap.OnStart();
            SeaSharpshooterBootstrap.OnStart();
            CherryStarBomberBootstrap.OnStart();
            ThreeBuckpeaterBootstrap.OnStart();
            SakuraSharpshooterBootstrap.OnStart();
            BomberDroneBootstrap.OnStart();
            FrostbiteDroneBootstrap.OnStart();
            IceLordCactusBootstrap.OnStart();
            ScoviliaPepperBootstrap.OnStart();
            AtomrayShroomBootstrap.OnStart();
            SauerkrautPultBootstrap.OnStart();
            CherryCabbageBootstrap.OnStart();
            LobShroomBootstrap.OnStart();
        }

        public override void OnGameInit()
        {
            NightRoofMap.OnGameInit();
            // The game rebuilds part of TravelDictionary during startup.
            // Reassert the weak-Odyssey metadata after that native reset.
            PlantRegister.RefreshWitchfireWeakOdysseyRegistration();

            // Refresh the Advanced Alt with Torchflower's exact native card
            // statistics after PVZ Fusion has initialized PlantDataManager.
            InfernoTorchflower.RefreshNativePlantData();
            PumpkinPodbomber.RefreshNativePlantData();
            V11PlantsBootstrap.OnGameInit();
            V11ExpansionBootstrap.OnGameInit();
            LogicBloverBootstrap.OnGameInit();
            SolarSharpshooterBootstrap.OnGameInit();
            SeaBallistaBootstrap.OnGameInit();
            PineshooterBootstrap.OnGameInit();
            IcytronionBootstrap.OnGameInit();
            SeaSharpshooterBootstrap.OnGameInit();
            CherryStarBomberBootstrap.OnGameInit();
            ThreeBuckpeaterBootstrap.OnGameInit();
            SakuraSharpshooterBootstrap.OnGameInit();
            BomberDroneBootstrap.OnGameInit();
            FrostbiteDroneBootstrap.OnGameInit();
            IceLordCactusBootstrap.OnGameInit();
            ScoviliaPepperBootstrap.OnGameInit();
            AtomrayShroomBootstrap.OnGameInit();
            SauerkrautPultBootstrap.OnGameInit();
            CherryCabbageBootstrap.OnGameInit();
            LobShroomBootstrap.OnGameInit();

            // CustomizeLib 3.8 currently leaves the separate lore field out
            // when it copies custom entries into the native Almanac data.
            AlmanacCompatibility.RefreshLoadedData();
            FinalPlantsBootstrap.OnGameInit();
        }

        private static void RegisterIl2CppTypes()
        {
            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<LotusPumpkin>())
                ClassInjector.RegisterTypeInIl2Cpp<LotusPumpkin>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<IcebergShroom>())
                ClassInjector.RegisterTypeInIl2Cpp<IcebergShroom>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<IcebergForcedSlow>())
                ClassInjector.RegisterTypeInIl2Cpp<IcebergForcedSlow>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<WitchfirePumpkin>())
                ClassInjector.RegisterTypeInIl2Cpp<WitchfirePumpkin>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<NuttySharpshooter>())
                ClassInjector.RegisterTypeInIl2Cpp<NuttySharpshooter>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<InfernoTorchflower>())
                ClassInjector.RegisterTypeInIl2Cpp<InfernoTorchflower>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<PumpkinPodbomber>())
                ClassInjector.RegisterTypeInIl2Cpp<PumpkinPodbomber>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<Ceasarweed>())
                ClassInjector.RegisterTypeInIl2Cpp<Ceasarweed>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<SolarFirnace>())
                ClassInjector.RegisterTypeInIl2Cpp<SolarFirnace>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<NotAPea>())
                ClassInjector.RegisterTypeInIl2Cpp<NotAPea>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<NotAStormCommando>())
                ClassInjector.RegisterTypeInIl2Cpp<NotAStormCommando>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<NotAPeaProjectile>())
                ClassInjector.RegisterTypeInIl2Cpp<NotAPeaProjectile>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<AttachedSawRuntime>())
                ClassInjector.RegisterTypeInIl2Cpp<AttachedSawRuntime>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<FrostFurflower>())
                ClassInjector.RegisterTypeInIl2Cpp<FrostFurflower>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<Doomtronion>())
                ClassInjector.RegisterTypeInIl2Cpp<Doomtronion>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<LichenPea>())
                ClassInjector.RegisterTypeInIl2Cpp<LichenPea>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<LogicBlover>())
                ClassInjector.RegisterTypeInIl2Cpp<LogicBlover>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<SolarSharpshooter>())
                ClassInjector.RegisterTypeInIl2Cpp<SolarSharpshooter>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<SeaBallista>())
                ClassInjector.RegisterTypeInIl2Cpp<SeaBallista>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<Pineshooter>())
                ClassInjector.RegisterTypeInIl2Cpp<Pineshooter>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<Icytronion>())
                ClassInjector.RegisterTypeInIl2Cpp<Icytronion>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<SeaSharpshooter>())
                ClassInjector.RegisterTypeInIl2Cpp<SeaSharpshooter>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<CherryStarBomber>())
                ClassInjector.RegisterTypeInIl2Cpp<CherryStarBomber>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<CherryStarProjectileModifier>())
                ClassInjector.RegisterTypeInIl2Cpp<CherryStarProjectileModifier>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<RollingHelpersController>())
                ClassInjector.RegisterTypeInIl2Cpp<RollingHelpersController>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<ThreeBuckpeater>())
                ClassInjector.RegisterTypeInIl2Cpp<ThreeBuckpeater>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<SakuraSharpshooter>())
                ClassInjector.RegisterTypeInIl2Cpp<SakuraSharpshooter>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<SakuraProjectileMarker>())
                ClassInjector.RegisterTypeInIl2Cpp<SakuraProjectileMarker>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<BomberDrone>())
                ClassInjector.RegisterTypeInIl2Cpp<BomberDrone>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<FrostbiteDrone>())
                ClassInjector.RegisterTypeInIl2Cpp<FrostbiteDrone>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<IceLordCactus>())
                ClassInjector.RegisterTypeInIl2Cpp<IceLordCactus>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<IceLordProjectileMarker>())
                ClassInjector.RegisterTypeInIl2Cpp<IceLordProjectileMarker>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<ScoviliaPepper>())
                ClassInjector.RegisterTypeInIl2Cpp<ScoviliaPepper>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<AtomrayShroom>())
                ClassInjector.RegisterTypeInIl2Cpp<AtomrayShroom>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<SauerkrautPult>())
                ClassInjector.RegisterTypeInIl2Cpp<SauerkrautPult>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<CherryCabbage>())
                ClassInjector.RegisterTypeInIl2Cpp<CherryCabbage>();
            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<CherryCabbageProjectile>())
                ClassInjector.RegisterTypeInIl2Cpp<CherryCabbageProjectile>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<LobShroom>())
                ClassInjector.RegisterTypeInIl2Cpp<LobShroom>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<SauerkrautProjectileMarker>())
                ClassInjector.RegisterTypeInIl2Cpp<SauerkrautProjectileMarker>();

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<NightRoofOneStrategy>())
                ClassInjector.RegisterTypeInIl2Cpp<NightRoofOneStrategy>();

#if ENABLE_MAGNETOPEA
            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<MagnetOPea>())
                ClassInjector.RegisterTypeInIl2Cpp<MagnetOPea>();
#endif
        }
    }
}
