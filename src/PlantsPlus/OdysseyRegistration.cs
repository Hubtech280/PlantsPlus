using CustomizeLib.MelonLoader;
using Il2Cpp;
using System;

namespace PlantsPlus.Core
{
    internal static class OdysseyRegistration
    {
        internal static void RegisterWeak(PlantType type, string name)
        {
            try
            {
                if (!CustomCore.CustomUltimatePlants.Contains(type))
                    CustomCore.AddUltimatePlant(type);

                if (!TravelDictionary.PlantInfo.ContainsKey(type))
                {
                    TravelDictionary.PlantInfo.Add(
                        type,
                        new Il2CppSystem.ValueTuple<
                            Il2CppSystem.Nullable<PlantType>,
                            Il2CppSystem.Object,
                            Il2CppSystem.Object,
                            bool
                        >(
                            new Il2CppSystem.Nullable<PlantType>(type),
                            null!,
                            null!,
                            false
                        )
                    );
                }
                else
                {
                    TravelDictionary.PlantInfo[type].Item4 = false;
                }

                if (TravelDictionary.allStrongUltimtePlant.Contains(type))
                    TravelDictionary.allStrongUltimtePlant.Remove(type);

                Plugin.Logger.LogInfo(
                    "[" + name + "] Odyssey registration active" +
                    " | Epic = false"
                );
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogWarning(
                    "[" + name + "] Odyssey metadata failed safely: " +
                    exception.Message
                );
            }
        }
    }
}
