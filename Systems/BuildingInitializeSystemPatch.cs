using System;
using Game;
using Game.Prefabs;
using Game.Simulation;
using HarmonyLib;

namespace LandValueOverhaul.Systems
{
    [HarmonyPatch(typeof(BuildingInitializeSystem), "OnCreate")]
    public class BuildingInitializeSystem_OnCreatePatch
    {
        private static bool Prefix(BuildingInitializeSystem __instance)
        {
            __instance.World.GetOrCreateSystemManaged<CustomBuildingInitializeSystem>();
            __instance.World.GetOrCreateSystemManaged<UpdateSystem>().UpdateAt<CustomBuildingInitializeSystem>(SystemUpdatePhase.GameSimulation);
            return true;
        }
    }

    [HarmonyPatch(typeof(BuildingInitializeSystem), "OnCreateForCompiler")]
    public class BuildingInitializeSystem_OnCreateForCompilerPatch
    {
        private static bool Prefix(BuildingInitializeSystem __instance)
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(BuildingInitializeSystem), "OnUpdate")]
    public class BuildingInitializeSystem_OnUpdatePatch
    {
        private static bool Prefix(BuildingInitializeSystem __instance)
        {
            __instance.World.GetOrCreateSystemManaged<CustomBuildingInitializeSystem>().Update();
            return false;
        }
    }
}