using HarmonyLib;
using Il2Cpp;
using System;

namespace PlantsPlus.Core
{
    internal static class NightRoofAdventure
    {
        internal const int LevelValue = 73;
        internal static readonly AdvantureLevel Level =
            (AdvantureLevel)LevelValue;

        private static bool registered;

        internal static void OnStart()
        {
            if (registered)
                return;

            registered = true;
            Plugin.Logger.LogInfo(
                "[Night Roof Adventure] Native chapter integration enabled."
            );
        }

        internal static void InstallStrategy()
        {
            try
            {
                var strategies = AdvantureConfig._levelStrategies;
                if (strategies == null || strategies.ContainsKey(Level))
                    return;

                NightRoofOneStrategy strategy =
                    new NightRoofOneStrategy();
                ILevelStrategy? nativeStrategy =
                    strategy.TryCast<ILevelStrategy>();
                if (nativeStrategy == null)
                {
                    Plugin.Logger.LogError(
                        "[Night Roof Adventure] Injected strategy could " +
                        "not be exposed as ILevelStrategy."
                    );
                    return;
                }

                strategies.Add(Level, nativeStrategy);
                Plugin.Logger.LogInfo(
                    "[Night Roof Adventure] Level 1 strategy installed " +
                    "with native Adventure defeat and wave logic" +
                    " | Scene = " + nativeStrategy.GetSceneType() +
                    " | Waves = " + nativeStrategy.GetMaxWave() +
                    " | Zombies = " +
                    nativeStrategy.GetZombieTypes().Count
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Night Roof Adventure] Strategy installation failed: " +
                    exception
                );
            }
        }

        internal static void ExtendMenu(AdvantureMenu menu)
        {
            if (menu == null)
                return;

            try
            {
                var links = menu.sceneNeedLevel;
                if (links == null)
                    return;

                // Keep the exact beta.21 Harmony/type surface, but correct
                // the native chapter direction: Snow -> Night Roof.
                if (links.ContainsKey(SceneType.Snow))
                {
                    var snowLink = links[SceneType.Snow];
                    links[SceneType.Snow] =
                        new Il2CppSystem.ValueTuple<
                            SceneType,
                            string,
                            AdvantureLevel
                        >(
                            SceneType.NightRoof,
                            snowLink.Item2,
                            AdvantureLevel.Snow6
                        );
                }

                // Give Night Roof a valid native menu record without
                // manually pushing it into sceneTypes (the old bug).
                links[SceneType.NightRoof] =
                    new Il2CppSystem.ValueTuple<
                        SceneType,
                        string,
                        AdvantureLevel
                    >(
                        SceneType.NightRoof,
                        "Night Roof",
                        Level
                    );

                if (menu.currentScene == SceneType.NightRoof &&
                    menu.nextSceneButton != null)
                {
                    menu.nextSceneButton.SetActive(false);
                }

                Plugin.Logger.LogInfo(
                    "[Night Roof Adventure] Native navigation wired " +
                    "Snow -> Night Roof | Unlock = Snow6"
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Night Roof Adventure] Menu integration failed: " +
                    exception
                );
            }
        }

    }

    public sealed class NightRoofOneStrategy : BaseLevelStrategy
    {
        public NightRoofOneStrategy(IntPtr pointer) : base(pointer) { }
        public NightRoofOneStrategy() : base() { }

        public override AdvantureLevel GetLevel()
        {
            return NightRoofAdventure.Level;
        }

        public override int GetMaxWave()
        {
            return 5;
        }

        public override Il2CppSystem.Collections.Generic.List<ZombieType>
            GetZombieTypes()
        {
            var zombies =
                new Il2CppSystem.Collections.Generic.List<ZombieType>();
            zombies.Add(ZombieType.NormalZombie);
            zombies.Add(ZombieType.ConeZombie);
            zombies.Add(ZombieType.BucketZombie);
            return zombies;
        }

        public override PlantType GetBasePlant()
        {
            return PlantType.Peashooter;
        }

        public override SceneType GetSceneType()
        {
            return SceneType.NightRoof;
        }

        public override string GetLevelName()
        {
            return "Night Roof 1";
        }

        public override string GetLevelTip()
        {
            return "The first night on the roof.";
        }
    }

    [HarmonyPatch]
    internal static class NightRoofAdventurePatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(
            typeof(AdvantureConfig),
            nameof(AdvantureConfig.InitializeStrategies)
        )]
        private static void InitializeStrategiesPostfix()
        {
            NightRoofAdventure.InstallStrategy();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AdvantureMenu), nameof(AdvantureMenu.Start))]
        private static void AdventureMenuStartPostfix(
            AdvantureMenu __instance
        )
        {
            NightRoofAdventure.ExtendMenu(__instance);
        }
    }
}
