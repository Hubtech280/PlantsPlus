using CustomizeLib.MelonLoader;
using Il2Cpp;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlantsPlus.Core
{
    internal static class NightRoofTestLevel
    {
        private static bool registered;

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
                    Name = () => "Night Roof - Test",
                    Logo = logo,
                    SceneType = SceneType.NightRoof,
                    BgmType = MusicType.Roof,
                    BoardTag = tag,
                    RowCount = 5,
                    NeedSelectCard = true,
                    Sun = () => 1000,
                    WaveCount = () => 5,
                    ZombieHealthRate = () => 1,
                    ZombieList = () => new List<ZombieType>
                    {
                        ZombieType.NormalZombie,
                        ZombieType.ConeZombie,
                        ZombieType.BucketZombie
                    },
                    PreSelectCards = () => new List<PlantType>
                    {
                        (PlantType)Plants.LobShroom.ID
                    }
                };

                int id = CustomCore.RegisterCustomLevel(level);
                registered = true;
                Plugin.Logger.LogInfo(
                    "[Night Roof Test] Registered simple 5-wave level" +
                    " | ID = " + id +
                    " | Zombies = Normal, Cone, Bucket"
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError(
                    "[Night Roof Test] Registration failed safely: " +
                    exception
                );
            }
        }
    }
}
