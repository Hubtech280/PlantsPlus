using BepInEx.Logging;
using CustomizeLib.BepInEx;
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

            Logger.LogInfo("Plants+ 1.0.0-release-ml.3 loaded!");

            RegisterIl2CppTypes();
            bool registered = PlantRegister.RegisterPlants();

            if (registered)
            {
#if ENABLE_MAGNETOPEA
                Logger.LogInfo(
                    "Plants+ V1.0 registration finished " +
                    "(safe IDs; Pumpkin Podbomber, Inferno Torchflower, " +
                    "Nutty Sharpshooter, " +
                    "Witchfire Pumpkin, Iceberg-shroom and Magnet-o-pea " +
                    "enabled)."
                );
#else
                Logger.LogInfo(
                    "Plants+ V1.0 registration finished " +
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
                    "Plants+ V1.0 registration failed safely; " +
                    "no custom plant was added."
                );
            }

            FinalPlantsBootstrap.OnStart();
        }

        public override void OnGameInit()
        {
            // The game rebuilds part of TravelDictionary during startup.
            // Reassert the weak-Odyssey metadata after that native reset.
            PlantRegister.RefreshWitchfireWeakOdysseyRegistration();

            // Refresh the Advanced Alt with Torchflower's exact native card
            // statistics after PVZ Fusion has initialized PlantDataManager.
            InfernoTorchflower.RefreshNativePlantData();
            PumpkinPodbomber.RefreshNativePlantData();

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

#if ENABLE_MAGNETOPEA
            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<MagnetOPea>())
                ClassInjector.RegisterTypeInIl2Cpp<MagnetOPea>();
#endif
        }
    }
}
