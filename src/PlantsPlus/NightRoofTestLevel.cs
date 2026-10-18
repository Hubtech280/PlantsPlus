using CustomizeLib.MelonLoader;
using Il2Cpp;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class NightRoofTestLevel
    {
        internal const int FirstLevel = 46;
        internal const int LevelCount = 4;
        private static readonly int[] RegisteredIds = { -1, -1, -1, -1 };
        private static bool registered;
        internal static int RegisteredId => RegisteredIds[0];

        internal static int GetRegisteredId(int levelNumber)
        {
            int index = levelNumber - FirstLevel;
            return index >= 0 && index < RegisteredIds.Length
                ? RegisteredIds[index]
                : -1;
        }

        internal static PlantType GetFeaturedPlant(int levelNumber)
        {
            return levelNumber switch
            {
                46 => (PlantType)Plants.BorealOrchid.BorealOrchidID,
                47 => (PlantType)Plants.LobShroom.ID,
                48 => PlantType.ElectricOnion,
                49 => (PlantType)Plants.BounceShroom.ID,
                _ => (PlantType)Plants.BorealOrchid.BorealOrchidID
            };
        }

        internal static void OnStart()
        {
            if (registered)
                return;

            try
            {
                Sprite? logo = SuperLevelEditorPlus.GetNightRoofPreview();
                if (logo == null)
                {
                    Plugin.Logger.LogWarning(
                        "[Night Roof Test] Preview could not be loaded; " +
                        "level registration postponed."
                    );
                    return;
                }

                Board.BoardTag tag = new Board.BoardTag
                {
                    isNight = true,
                    isRoof = true
                };

                var level = new CustomLevelData
                {
                    Name = () => "夜间屋顶 1",
                    Logo = logo,
                    SceneType = SceneType.NightRoof,
                    BgmType = MusicType.Roof,
                    BoardTag = tag,
                    RowCount = 5,
                    NeedSelectCard = true,
                    Sun = () => 1000,
                    WaveCount = () => 10,
                    ZombieHealthRate = () => 1,
                    ZombieList = () => new List<ZombieType>
                    {
                        ZombieType.NormalZombie,
                        ZombieType.ConeZombie,
                        ZombieType.BucketZombie
                    },
                    PreSelectCards = () => new List<PlantType>
                    {
                        (PlantType)Plants.BorealOrchid.BorealOrchidID
                    }
                };

                for (int index = 0; index < LevelCount; index++)
                {
                    int chapterIndex = index;
                    int levelNumber = FirstLevel + index;
                    PlantType featured = GetFeaturedPlant(levelNumber);
                    level.Name = () => "夜间屋顶 " +
                        (chapterIndex + 1);
                    // The native progress bar counts flags in groups of ten.
                    // Fewer waves showed "0 flags" and never had a real final flag.
                    level.WaveCount = () => 10;
                    level.ZombieList = () => GetZombies(chapterIndex);
                    level.PreSelectCards = () => new List<PlantType>
                    {
                        featured
                    };
                    level.PostBoard = board => NightRoofMoon.Attach(board);
                    RegisteredIds[index] =
                        CustomCore.RegisterCustomLevel(level);
                    Plugin.Logger.LogInfo(
                        "[Night Roof] Level " + levelNumber +
                        " registered | Custom ID = " +
                        RegisteredIds[index] +
                        " | Featured plant = " + featured
                    );
                }
                registered = true;
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Night Roof Test] Registration failed safely: " +
                    exception
                );
            }
        }

        private static List<ZombieType> GetZombies(int chapterIndex)
        {
            var zombies = new List<ZombieType>
            {
                ZombieType.NormalZombie,
                ZombieType.ConeZombie,
                ZombieType.BucketZombie
            };
            if (chapterIndex >= 1)
                zombies.Add(ZombieType.PolevaulterZombie);
            if (chapterIndex >= 2)
                zombies.Add(ZombieType.PaperZombie);
            if (chapterIndex >= 3)
                zombies.Add(ZombieType.CatapultZombie);
            return zombies;
        }
    }
}
