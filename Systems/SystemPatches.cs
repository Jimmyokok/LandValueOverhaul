using Colossal.Collections;
using Game;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using Game.UI;
using Game.UI.Tooltip;
using Game.Zones;
using HarmonyLib;
using LandValueOverhaul.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Game.Rendering.Debug.RenderPrefabRenderer;


namespace LandValueOverhaul.Patches
{
    [HarmonyPatch]
    class LandValueOverhaulPatches
    {

        /*
        [HarmonyPatch(typeof(Game.Prefabs.ServiceFeeParameterPrefab), "LateInitialize")]
        [HarmonyPrefix]
        static bool ServiceFeeParameterPrefab_LateInitialize(ServiceFeeParameterPrefab __instance, EntityManager entityManager, Entity entity)
        {
            Mod.log.Info($"Garbage fee set to zero!");
            ServiceFeeParameterData componentData = new ServiceFeeParameterData
            {
                m_ElectricityFee = __instance.m_ElectricityFee,
                m_ElectricityFeeConsumptionMultiplier = new AnimationCurve1(__instance.m_ElectricityFeeConsumptionMultiplier),
                m_HealthcareFee = __instance.m_HealthcareFee,
                m_BasicEducationFee = __instance.m_BasicEducationFee,
                m_HigherEducationFee = __instance.m_HigherEducationFee,
                m_SecondaryEducationFee = __instance.m_SecondaryEducationFee,
                m_GarbageFee = new FeeParameters { m_Adjustable = false, m_Default = 0f, m_Max = 0f },
                m_WaterFee = __instance.m_WaterFee,
                m_WaterFeeConsumptionMultiplier = new AnimationCurve1(__instance.m_WaterFeeConsumptionMultiplier),
                m_FireResponseFee = __instance.m_FireResponseFee,
                m_PoliceFee = __instance.m_PoliceFee
            };
            entityManager.SetComponentData<ServiceFeeParameterData>(entity, componentData);
            return false;
        }
        */

        [HarmonyPatch(typeof(BuildingUtils), "GetLevelingCost")]
        [HarmonyPrefix]
        static bool BuildingUtils_GetLevelingCost(ref int __result, AreaType areaType, BuildingPropertyData propertyData, int currentlevel, DynamicBuffer<CityModifier> cityEffects)
        {
            int num = propertyData.CountProperties();
            float num2 = 0f;
            if (areaType != AreaType.Residential)
            {
                if (areaType - AreaType.Commercial > 1)
                {
                    num2 = 1.0737418E+09f;
                }
                else
                {
                    num2 = (float)((currentlevel <= 4) ? (num * Mathf.RoundToInt(math.pow(2f, (float)(2 * currentlevel)) * 160f)) : 1073741823);
                    if (propertyData.m_AllowedStored != Resource.NoResource)
                    {
                        num2 *= 4f;
                    }
                }
            }
            else
            {
                num2 = (float)((currentlevel <= 4) ? (num * Mathf.RoundToInt(math.pow(2f, 1f + ((float)(currentlevel) - 1) * LandValueOverhaul.Systems.PropertyRenterSystem.upgrade_cost_factor) * 80f)) : 1073741823);
            }
            CityUtils.ApplyModifier(ref num2, cityEffects, CityModifierType.BuildingLevelingCost);
            __result = Mathf.RoundToInt(num2);
            return false;
        }

        [HarmonyPatch(typeof(Game.Prefabs.BuildingInitializeSystem), "OnCreate")]
        [HarmonyPrefix]
        public static bool BuildingInitializeSystem_OnCreate(Game.Prefabs.BuildingInitializeSystem __instance)
        {
            __instance.World.GetOrCreateSystemManaged<Systems.BuildingInitializeSystem>();

            __instance.World.GetOrCreateSystemManaged<UpdateSystem>().UpdateAt<LandValueOverhaul.Systems.BuildingInitializeSystem>(SystemUpdatePhase.PrefabUpdate);
            return true;
        }

        [HarmonyPatch(typeof(Game.Prefabs.BuildingInitializeSystem), "OnCreateForCompiler")]
        [HarmonyPrefix]
        public static bool BuildingInitializeSystem_OnCreateForCompiler(Game.Prefabs.BuildingInitializeSystem __instance)
        {
            return false;
        }

        // Skip system
        [HarmonyPatch(typeof(Game.Prefabs.BuildingInitializeSystem), "OnUpdate")]
        [HarmonyPrefix]
        public static bool BuildingInitializeSystem_OnUpdate(Game.Prefabs.BuildingInitializeSystem __instance)
        {
            return false;
        }

        [HarmonyPatch(typeof(CellMapSystem<LandValueCell>), "AddReader")]
        [HarmonyPrefix]
        public static bool LandValueSystem_AddReader(CellMapSystem<LandValueCell> __instance, JobHandle jobHandle)
        {
            string name = __instance.GetType().FullName;
            if (name == "Game.Simulation.LandValueSystem")
            { 
                __instance.World.GetExistingSystemManaged<LandValueOverhaul.Systems.LandValueSystem>().AddReader(jobHandle);
                return false;
            }
            return true;
        }
        
        [HarmonyPatch(typeof(CellMapSystem<LandValueCell>), "GetData")]
        [HarmonyPrefix]
        public static bool LandValueSystem_GetData(CellMapSystem<LandValueCell> __instance, ref CellMapData<LandValueCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            string name = __instance.GetType().FullName;
            if (name == "Game.Simulation.LandValueSystem")
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.LandValueSystem>().GetData(readOnly, out dependencies);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CellMapSystem<LandValueCell>), "GetMap")]
        [HarmonyPrefix]
        public static bool LandValueSystem_GetMap(CellMapSystem<LandValueCell> __instance, ref NativeArray<LandValueCell> __result, bool readOnly, ref JobHandle dependencies)
        {
            string name = __instance.GetType().FullName;
            if (name == "Game.Simulation.LandValueSystem")
            {
                __result = __instance.World.GetExistingSystemManaged<Systems.LandValueSystem>().GetMap(readOnly, out dependencies);
                return false;
            }
            return true;
        }
    }
}
