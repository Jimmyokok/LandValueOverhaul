using Game;
using Game.Simulation;
using HarmonyLib;

namespace LandValueOverhaul.Systems
{
    [HarmonyPatch(typeof(RentAdjustSystem), "OnCreate")]
    public class RentAdjustSystem_OnCreatePatch
    {
        private static bool Prefix(RentAdjustSystem __instance)
        {
            __instance.World.GetOrCreateSystemManaged<CustomRentAdjustSystem>();
            __instance.World.GetOrCreateSystemManaged<UpdateSystem>().UpdateAt<CustomRentAdjustSystem>(SystemUpdatePhase.GameSimulation);
            return true;
        }
    }

    [HarmonyPatch(typeof(RentAdjustSystem), "OnCreateForCompiler")]
    public class RentAdjustSystem_OnCreateForCompilerPatch
    {
        private static bool Prefix(RentAdjustSystem __instance)
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(RentAdjustSystem), "OnUpdate")]
    public class RentAdjustSystem_OnUpdatePatch
    {
        private static bool Prefix(RentAdjustSystem __instance)
        {
            __instance.World.GetOrCreateSystemManaged<CustomRentAdjustSystem>().Update();
            return false;
        }
    }
}