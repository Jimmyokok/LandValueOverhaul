using System;
using Game;
using Game.Simulation;
using HarmonyLib;

namespace LandValueOverhaul.Systems
{
    [HarmonyPatch(typeof(PropertyRenterSystem), "OnCreate")]
    public class PropertyRenterSystem_OnCreatePatch
    {
        private static bool Prefix(PropertyRenterSystem __instance)
        {
            __instance.World.GetOrCreateSystemManaged<CustomPropertyRenterSystem>();
            __instance.World.GetOrCreateSystemManaged<UpdateSystem>().UpdateAt<CustomPropertyRenterSystem>(SystemUpdatePhase.GameSimulation);
            return true;
        }
    }

    [HarmonyPatch(typeof(PropertyRenterSystem), "OnCreateForCompiler")]
    public class PropertyRenterSystem_OnCreateForCompilerPatch
    {
        private static bool Prefix(PropertyRenterSystem __instance)
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(PropertyRenterSystem), "OnUpdate")]
    public class PropertyRenterSystem_OnUpdatePatch
    {
        private static bool Prefix(PropertyRenterSystem __instance)
        {
            __instance.World.GetOrCreateSystemManaged<CustomPropertyRenterSystem>().Update();
            return false;
        }
    }


}