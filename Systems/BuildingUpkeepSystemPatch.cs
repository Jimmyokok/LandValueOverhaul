using Game;
using Game.Simulation;
using Game.UI;
using HarmonyLib;

namespace LandValueOverhaul.Systems
{
    [HarmonyPatch(typeof(BuildingUpkeepSystem), "OnCreate")]
    public class BuildingUpkeepSystem_OnCreatePatch
    {
        private static bool Prefix(BuildingUpkeepSystem __instance)
        {
            __instance.World.GetOrCreateSystemManaged<CustomBuildingUpkeepSystem>();
            __instance.World.GetOrCreateSystemManaged<UpdateSystem>().UpdateAt<CustomBuildingUpkeepSystem>(SystemUpdatePhase.GameSimulation);
            return true;
        }
    }

    [HarmonyPatch(typeof(BuildingUpkeepSystem), "OnCreateForCompiler")]
    public class BuildingUpkeepSystem_OnCreateForCompilerPatch
    {
        private static bool Prefix(BuildingUpkeepSystem __instance)
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(BuildingUpkeepSystem), "OnUpdate")]
    public class BuildingUpkeepSystem_OnUpdatePatch
    {
        private static bool Prefix(BuildingUpkeepSystem __instance)
        {
            __instance.World.GetOrCreateSystemManaged<CustomBuildingUpkeepSystem>().Update();
            return false;
        }
    }
}