using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using Colossal.Collections;
using Game.Prefabs;
using Unity.Entities;
using Game.Simulation;
using Game;
using LandValueOverhaul.Systems;
using BepInEx.Logging;
using Game.City;
using Game.Buildings;
using Game.Zones;
using Unity.Mathematics;
using Game.Economy;
using Game.Areas;
using Game.Rendering;
using AreaType = Game.Zones.AreaType;
using System.Collections.Generic;
using System.Reflection.Emit;

#if BEPINEX_V6
    using BepInEx.Unity.Mono;
#endif

namespace LandValueOverhaul
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, "1.4.3")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} is loaded!");
            var harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MyPluginInfo.PLUGIN_GUID + "_Cities2Harmony");
            var patchedMethods = harmony.GetPatchedMethods().ToArray();
        }
    }
    [HarmonyPatch(typeof(ServiceFeeParameterPrefab), "LateInitialize")]
    public class ServiceFeeParameterPrefab_LateInitializePatch
    {
        private static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(MyPluginInfo.PLUGIN_NAME);

        private static bool Prefix(ServiceFeeParameterPrefab __instance, EntityManager entityManager, Entity entity)
        {
            logger.LogInfo("GaebageFee set to zero!");
            ServiceFeeParameterData componentData = new ServiceFeeParameterData
            {
                m_ElectricityFee = __instance.m_ElectricityFee,
                m_ElectricityFeeConsumptionMultiplier = new AnimationCurve1(__instance.m_ElectricityFeeConsumptionMultiplier),
                m_HealthcareFee = __instance.m_HealthcareFee,
                m_BasicEducationFee = __instance.m_BasicEducationFee,
                m_HigherEducationFee = __instance.m_HigherEducationFee,
                m_SecondaryEducationFee = __instance.m_SecondaryEducationFee,
                m_GarbageFee = new FeeParameters{m_Adjustable=false, m_Default=0f, m_Max=0f},
                m_WaterFee = __instance.m_WaterFee,
                m_WaterFeeConsumptionMultiplier = new AnimationCurve1(__instance.m_WaterFeeConsumptionMultiplier),
                m_FireResponseFee = __instance.m_FireResponseFee,
                m_PoliceFee = __instance.m_PoliceFee
            };
            entityManager.SetComponentData<ServiceFeeParameterData>(entity, componentData);
            return false;
        }
    }

    [HarmonyPatch(typeof(BuildingUtils), "GetLevelingCost")]
    public class BuildingUtils_GetLevelingCostPatch
    {
        private static bool Prefix(ref int __result, AreaType areaType, BuildingPropertyData propertyData, int currentlevel, DynamicBuffer<CityModifier> cityEffects)
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
                num2 = (float)((currentlevel <= 4) ? (num * Mathf.RoundToInt(math.pow(2f, (float)(currentlevel)) * 80f)) : 1073741823);
            }
            CityUtils.ApplyModifier(ref num2, cityEffects, CityModifierType.BuildingLevelingCost);
            __result = Mathf.RoundToInt(num2);
            return false;
        }
    }
}
