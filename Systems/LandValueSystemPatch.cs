using Game;
using Game.Simulation;
using HarmonyLib;

namespace LandValueOverhaul.Systems
{
    [HarmonyPatch(typeof(LandValueSystem), "OnCreate")]
    public class LandValueSystem_OnCreatePatch
    {
        private static bool Prefix(LandValueSystem __instance)
        {
            __instance.World.GetOrCreateSystemManaged<LandValueSystem_Custom>();
            __instance.World.GetOrCreateSystemManaged<UpdateSystem>().UpdateAt<LandValueSystem_Custom>(SystemUpdatePhase.GameSimulation);
            return true;
        }
    }

    [HarmonyPatch(typeof(LandValueSystem), "OnCreateForCompiler")]
    public class LandValueSystem_OnCreateForCompilerPatch
    {
        private static bool Prefix(LandValueSystem __instance)
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(LandValueSystem), "OnUpdate")]
    public class LandValueSystem_OnUpdatePatch
    {
        private static bool Prefix(LandValueSystem __instance)
        {
            __instance.World.GetOrCreateSystemManaged<LandValueSystem_Custom>().Update();
            return false;
        }
    }
}