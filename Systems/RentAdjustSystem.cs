using Game;
using Game.Simulation;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Agents;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Net;
using Game.Notifications;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using BepInEx.Logging;

namespace LandValueOverhaul.Systems
{
    // Token: 0x02001354 RID: 4948
    [CompilerGenerated]
    public class CustomRentAdjustSystem : GameSystemBase
    {
        // Token: 0x060055C6 RID: 21958 RVA: 0x0008E6C5 File Offset: 0x0008C8C5
        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return (int)((float) 262144 / ((float)CustomRentAdjustSystem.kUpdatesPerDay * (float)16));
        }

        // Token: 0x060055C7 RID: 21959 RVA: 0x003B4F84 File Offset: 0x003B3184
        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            /*
            UpdateFrequencyFactor = Plugin.LandValueUpdateFrenquencyFactor.Value;
            UpdateFrequencyFactor = UpdateFrequencyFactor > 0 ? UpdateFrequencyFactor : 0;
            UpdateFrequencyFactor = UpdateFrequencyFactor < 16 ? UpdateFrequencyFactor : 16;
            ResidentialMaxRentIncomeFactor = Plugin.ResidentialMaxRentIncomeFactor.Value > 0 ? (Plugin.ResidentialMaxRentIncomeFactor.Value < 1 ? Plugin.ResidentialMaxRentIncomeFactor.Value : 1) : 0;
            CompanyMaxRentProfitFactor = Plugin.CompanyMaxRentProfitFactor.Value > 0 ? (Plugin.CompanyMaxRentProfitFactor.Value < 1 ? Plugin.CompanyMaxRentProfitFactor.Value : 1) : 0;
            */
            this.m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
            this.m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
            this.m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
            this.m_GroundPollutionSystem = base.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
            this.m_AirPollutionSystem = base.World.GetOrCreateSystemManaged<AirPollutionSystem>();
            this.m_NoisePollutionSystem = base.World.GetOrCreateSystemManaged<NoisePollutionSystem>();
            this.m_TelecomCoverageSystem = base.World.GetOrCreateSystemManaged<TelecomCoverageSystem>();
            this.m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
            this.m_TaxSystem = base.World.GetOrCreateSystemManaged<TaxSystem>();
            this.m_CountEmploymentSystem = base.World.GetOrCreateSystemManaged<CountEmploymentSystem>();
            this.m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
            this.m_EconomyParameterQuery = base.GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<EconomyParameterData>()
            });
            this.m_DemandParameterQuery = base.GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<DemandParameterData>()
            });
            this.m_BuildingParameterQuery = base.GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<BuildingConfigurationData>()
            });
            this.m_BuildingQuery = base.GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<Building>(),
                ComponentType.ReadOnly<UpdateFrame>(),
                ComponentType.ReadWrite<Renter>(),
                ComponentType.Exclude<Temp>(),
                ComponentType.Exclude<Deleted>()
            });
            this.m_ExtractorParameterQuery = base.GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<ExtractorParameterData>()
            });
            this.m_HealthcareParameterQuery = base.GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<HealthcareParameterData>()
            });
            this.m_ParkParameterQuery = base.GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<ParkParameterData>()
            });
            this.m_EducationParameterQuery = base.GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<EducationParameterData>()
            });
            this.m_TelecomParameterQuery = base.GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<TelecomParameterData>()
            });
            this.m_GarbageParameterQuery = base.GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<GarbageParameterData>()
            });
            this.m_PoliceParameterQuery = base.GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<PoliceConfigurationData>()
            });
            this.m_CitizenHappinessParameterQuery = base.GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<CitizenHappinessParameterData>()
            });
            this.m_PollutionParameterQuery = base.GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<PollutionParameterData>()
            });
            base.RequireForUpdate(this.m_EconomyParameterQuery);
            base.RequireForUpdate(this.m_DemandParameterQuery);
            base.RequireForUpdate(this.m_HealthcareParameterQuery);
            base.RequireForUpdate(this.m_ParkParameterQuery);
            base.RequireForUpdate(this.m_EducationParameterQuery);
            base.RequireForUpdate(this.m_TelecomParameterQuery);
            base.RequireForUpdate(this.m_GarbageParameterQuery);
            base.RequireForUpdate(this.m_PoliceParameterQuery);
            base.RequireForUpdate(this.m_BuildingQuery);
        }

        // Token: 0x060055C8 RID: 21960 RVA: 0x003B5274 File Offset: 0x003B3474
        [Preserve]
        protected override void OnUpdate()
        {
            uint updateFrame = SimulationUtils.GetUpdateFrame(this.m_SimulationSystem.frameIndex, CustomRentAdjustSystem.kUpdatesPerDay, 16);
            this.__TypeHandle.__Game_Buildings_ExtractorProperty_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_MailProducer_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_GarbageProducer_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_AdjustHappinessData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_BuildingNotifications_RW_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Companies_TradeCost_RO_BufferLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Student_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ExtractorAreaData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Efficiency_RO_BufferLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Companies_Employee_RO_BufferLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Areas_DistrictModifier_RO_BufferLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_City_CityModifier_RO_BufferLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_Locked_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_CrimeProducer_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Companies_StorageCompany_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Areas_SubArea_RO_BufferLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Net_ResourceAvailability_RO_BufferLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Net_ServiceCoverage_RO_BufferLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ConsumptionData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Net_LandValue_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Areas_Extractor_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Areas_Geometry_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Areas_Lot_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Objects_Attached_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Companies_CompanyNotifications_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Companies_BuyingCompany_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Companies_ProcessingCompany_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Companies_ServiceCompanyData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Companies_ServiceAvailable_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Companies_WorkProvider_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Building_RW_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Household_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_PropertyOnMarket_RW_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_PropertyRenter_RW_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Renter_RW_BufferTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
            CustomRentAdjustSystem.AdjustRentJob jobData = default(CustomRentAdjustSystem.AdjustRentJob);
            jobData.m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle;
            jobData.m_RenterType = this.__TypeHandle.__Game_Buildings_Renter_RW_BufferTypeHandle;
            jobData.m_UpdateFrameType = base.GetSharedComponentTypeHandle<UpdateFrame>();
            jobData.m_Renters = this.__TypeHandle.__Game_Buildings_PropertyRenter_RW_ComponentLookup;
            jobData.m_OnMarkets = this.__TypeHandle.__Game_Buildings_PropertyOnMarket_RW_ComponentLookup;
            jobData.m_Households = this.__TypeHandle.__Game_Citizens_Household_RO_ComponentLookup;
            jobData.m_Workers = this.__TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup;
            jobData.m_Buildings = this.__TypeHandle.__Game_Buildings_Building_RW_ComponentLookup;
            jobData.m_Prefabs = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
            jobData.m_BuildingProperties = this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;
            jobData.m_BuildingDatas = this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup;
            jobData.m_WorkProviders = this.__TypeHandle.__Game_Companies_WorkProvider_RO_ComponentLookup;
            jobData.m_ProcessDatas = this.__TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;
            jobData.m_ServiceAvailables = this.__TypeHandle.__Game_Companies_ServiceAvailable_RO_ComponentLookup;
            jobData.m_ServiceCompanyDatas = this.__TypeHandle.__Game_Companies_ServiceCompanyData_RO_ComponentLookup;
            jobData.m_ProcessingCompanies = this.__TypeHandle.__Game_Companies_ProcessingCompany_RO_ComponentLookup;
            jobData.m_BuyingCompanies = this.__TypeHandle.__Game_Companies_BuyingCompany_RO_ComponentLookup;
            jobData.m_CompanyNotifications = this.__TypeHandle.__Game_Companies_CompanyNotifications_RO_ComponentLookup;
            jobData.m_Attached = this.__TypeHandle.__Game_Objects_Attached_RO_ComponentLookup;
            jobData.m_Lots = this.__TypeHandle.__Game_Areas_Lot_RO_ComponentLookup;
            jobData.m_Geometries = this.__TypeHandle.__Game_Areas_Geometry_RO_ComponentLookup;
            jobData.m_AreaExtractors = this.__TypeHandle.__Game_Areas_Extractor_RO_ComponentLookup;
            jobData.m_LandValues = this.__TypeHandle.__Game_Net_LandValue_RO_ComponentLookup;
            jobData.m_ConsumptionDatas = this.__TypeHandle.__Game_Prefabs_ConsumptionData_RO_ComponentLookup;
            jobData.m_Citizens = this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup;
            jobData.m_ServiceCoverages = this.__TypeHandle.__Game_Net_ServiceCoverage_RO_BufferLookup;
            jobData.m_Availabilities = this.__TypeHandle.__Game_Net_ResourceAvailability_RO_BufferLookup;
            jobData.m_SubAreas = this.__TypeHandle.__Game_Areas_SubArea_RO_BufferLookup;
            jobData.m_SpawnableBuildings = this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;
            jobData.m_ResourceDatas = this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup;
            jobData.m_StorageCompanies = this.__TypeHandle.__Game_Companies_StorageCompany_RO_ComponentLookup;
            jobData.m_WorkplaceDatas = this.__TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup;
            jobData.m_Abandoned = this.__TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup;
            jobData.m_Destroyed = this.__TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup;
            jobData.m_Crimes = this.__TypeHandle.__Game_Buildings_CrimeProducer_RO_ComponentLookup;
            jobData.m_Locked = this.__TypeHandle.__Game_Prefabs_Locked_RO_ComponentLookup;
            jobData.m_Transforms = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup;
            jobData.m_CityModifiers = this.__TypeHandle.__Game_City_CityModifier_RO_BufferLookup;
            jobData.m_Districts = this.__TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentLookup;
            jobData.m_DistrictModifiers = this.__TypeHandle.__Game_Areas_DistrictModifier_RO_BufferLookup;
            jobData.m_HealthProblems = this.__TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup;
            jobData.m_Employees = this.__TypeHandle.__Game_Companies_Employee_RO_BufferLookup;
            jobData.m_BuildingEfficiencies = this.__TypeHandle.__Game_Buildings_Efficiency_RO_BufferLookup;
            jobData.m_ExtractorDatas = this.__TypeHandle.__Game_Prefabs_ExtractorAreaData_RO_ComponentLookup;
            jobData.m_CitizenDatas = this.__TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup;
            jobData.m_Students = this.__TypeHandle.__Game_Citizens_Student_RO_ComponentLookup;
            jobData.m_SpawnableBuildingData = this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;
            jobData.m_ZoneData = this.__TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup;
            jobData.m_TradeCosts = this.__TypeHandle.__Game_Companies_TradeCost_RO_BufferLookup;
            jobData.m_BuildingNotifications = this.__TypeHandle.__Game_Buildings_BuildingNotifications_RW_ComponentLookup;
            jobData.m_ElectricityConsumers = this.__TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup;
            jobData.m_AdjustHappinessDatas = this.__TypeHandle.__Game_Prefabs_AdjustHappinessData_RO_ComponentLookup;
            jobData.m_WaterConsumers = this.__TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup;
            jobData.m_GarbageProducers = this.__TypeHandle.__Game_Buildings_GarbageProducer_RO_ComponentLookup;
            jobData.m_MailProducers = this.__TypeHandle.__Game_Buildings_MailProducer_RO_ComponentLookup;
            jobData.m_Abandoneds = this.__TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup;
            jobData.m_ExtractorProperties = this.__TypeHandle.__Game_Buildings_ExtractorProperty_RO_ComponentLookup;
            JobHandle job;
            jobData.m_PollutionMap = this.m_GroundPollutionSystem.GetMap(true, out job);
            JobHandle job2;
            jobData.m_AirPollutionMap = this.m_AirPollutionSystem.GetMap(true, out job2);
            JobHandle job3;
            jobData.m_NoiseMap = this.m_NoisePollutionSystem.GetMap(true, out job3);
            JobHandle job4;
            jobData.m_TelecomCoverages = this.m_TelecomCoverageSystem.GetData(true, out job4);
            jobData.m_ExtractorParameters = this.m_ExtractorParameterQuery.GetSingleton<ExtractorParameterData>();
            jobData.m_HealthcareParameters = this.m_HealthcareParameterQuery.GetSingleton<HealthcareParameterData>();
            jobData.m_ParkParameters = this.m_ParkParameterQuery.GetSingleton<ParkParameterData>();
            jobData.m_EducationParameters = this.m_EducationParameterQuery.GetSingleton<EducationParameterData>();
            jobData.m_TelecomParameters = this.m_TelecomParameterQuery.GetSingleton<TelecomParameterData>();
            jobData.m_GarbageParameters = this.m_GarbageParameterQuery.GetSingleton<GarbageParameterData>();
            jobData.m_PoliceParameters = this.m_PoliceParameterQuery.GetSingleton<PoliceConfigurationData>();
            jobData.m_CitizenHappinessParameterData = this.m_CitizenHappinessParameterQuery.GetSingleton<CitizenHappinessParameterData>();
            jobData.m_BuildingConfigurationData = this.m_BuildingParameterQuery.GetSingleton<BuildingConfigurationData>();
            jobData.m_PollutionParameters = this.m_PollutionParameterQuery.GetSingleton<PollutionParameterData>();
            jobData.m_ResourcePrefabs = this.m_ResourceSystem.GetPrefabs();
            jobData.m_TaxRates = this.m_TaxSystem.GetTaxRates();
            JobHandle job5;
            jobData.m_Unemployment = this.m_CountEmploymentSystem.GetUnemploymentByEducation(out job5);
            jobData.m_EconomyParameters = this.m_EconomyParameterQuery.GetSingleton<EconomyParameterData>();
            jobData.m_DemandParameters = this.m_DemandParameterQuery.GetSingleton<DemandParameterData>();
            jobData.m_BaseConsumptionSum = (float)this.m_ResourceSystem.BaseConsumptionSum;
            jobData.m_City = this.m_CitySystem.City;
            jobData.m_UpdateFrameIndex = updateFrame;
            jobData.m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter();
            jobData.m_IconCommandBuffer = this.m_IconCommandSystem.CreateCommandBuffer();
            JobHandle jobHandle = jobData.ScheduleParallel(this.m_BuildingQuery, JobUtils.CombineDependencies(job, job2, job3, job4, job5, base.Dependency));
            this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
            this.m_ResourceSystem.AddPrefabsReader(jobHandle);
            this.m_GroundPollutionSystem.AddReader(jobHandle);
            this.m_AirPollutionSystem.AddReader(jobHandle);
            this.m_NoisePollutionSystem.AddReader(jobHandle);
            this.m_TelecomCoverageSystem.AddReader(jobHandle);
            this.m_CountEmploymentSystem.AddReader(jobHandle);
            this.m_TaxSystem.AddReader(jobHandle);
            this.m_IconCommandSystem.AddCommandBufferWriter(jobHandle);
            base.Dependency = jobHandle;
        }

        // Token: 0x060055C9 RID: 21961 RVA: 0x003B5DA0 File Offset: 0x003B3FA0
        public static int2 GetRent(ConsumptionData consumptionData, BuildingPropertyData buildingProperties, float landValue, Game.Zones.AreaType areaType)
        {
            float2 @float;
            @float.x = (float)consumptionData.m_Upkeep;
            @float.y = @float.x;
            float num;
            if (buildingProperties.m_ResidentialProperties > 0 && (buildingProperties.m_AllowedSold != Resource.NoResource || buildingProperties.m_AllowedManufactured > Resource.NoResource))
            {
                num = (float)Mathf.RoundToInt((float)buildingProperties.m_ResidentialProperties / (1f - CustomRentAdjustSystem.kMixedCompanyRent));
            }
            else
            {
                num = (float)buildingProperties.CountProperties();
            }
            @float.x += math.max(0f, 1f * landValue);
            @float /= num;
            return new int2(Mathf.RoundToInt(@float.x), Mathf.RoundToInt(@float.y));
        }

        public static float GetUpkeep(ConsumptionData consumptionData, BuildingPropertyData buildingProperties)
        {
            float @float;
            @float = (float)consumptionData.m_Upkeep;
            float num;
            if (buildingProperties.m_ResidentialProperties > 0 && (buildingProperties.m_AllowedSold != Resource.NoResource || buildingProperties.m_AllowedManufactured > Resource.NoResource))
            {
                num = (float)Mathf.RoundToInt((float)buildingProperties.m_ResidentialProperties / (1f - CustomRentAdjustSystem.kMixedCompanyRent));
            }
            else
            {
                num = (float)buildingProperties.CountProperties();
            }
            @float /= num;
            return Mathf.Max(1f, @float);
        }

        // Token: 0x060055CA RID: 21962 RVA: 0x003B5E58 File Offset: 0x003B4058
        public static int4 CalculateMaximumRent(float upkeep, Entity renter, ref EconomyParameterData economyParameters, ref DemandParameterData demandParameters, float baseConsumptionSum, DynamicBuffer<CityModifier> cityModifiers, PropertyRenter propertyRenter, Entity healthcareService, Entity entertainmentService, Entity educationService, Entity telecomService, Entity garbageService, Entity policeService, ref ComponentLookup<Household> households, ref ComponentLookup<Worker> workers, ref ComponentLookup<Building> buildings, ref BufferLookup<HouseholdCitizen> householdCitizens, ref ComponentLookup<PrefabRef> prefabs, ref BufferLookup<ResourceAvailability> availabilities, ref ComponentLookup<BuildingPropertyData> buildingProperties, ref ComponentLookup<BuildingData> buildingDatas, ref ComponentLookup<SpawnableBuildingData> spawnableBuildings, ref ComponentLookup<CrimeProducer> crimes, ref BufferLookup<Game.Net.ServiceCoverage> serviceCoverages, ref ComponentLookup<Locked> locked, ref ComponentLookup<ElectricityConsumer> electricityConsumers, ref ComponentLookup<AdjustHappinessData> adjustHappinessDatas, ref ComponentLookup<WaterConsumer> waterConsumers, ref ComponentLookup<GarbageProducer> garbageProducers, ref ComponentLookup<MailProducer> mailProducers, ref ComponentLookup<Game.Objects.Transform> transforms, NativeArray<GroundPollution> pollutionMap, NativeArray<AirPollution> airPollutionMap, NativeArray<NoisePollution> noiseMap, CellMapData<TelecomCoverage> telecomCoverages, ResourcePrefabs resourcePrefabs, ref ComponentLookup<ResourceData> resourceDatas, ref ComponentLookup<IndustrialProcessData> processDatas, ref ComponentLookup<Game.Companies.StorageCompany> storageCompanies, ref ComponentLookup<ServiceAvailable> serviceAvailables, ref ComponentLookup<WorkProvider> workProviders, ref ComponentLookup<ServiceCompanyData> serviceCompanyDatas, ref ComponentLookup<WorkplaceData> workplaceDatas, ref ComponentLookup<Game.Companies.ProcessingCompany> processingCompanies, ref ComponentLookup<BuyingCompany> buyingCompanies, ref BufferLookup<Game.Areas.SubArea> subAreas, ref ComponentLookup<Attached> attached, ref ComponentLookup<Game.Areas.Lot> lots, ref ComponentLookup<Geometry> geometries, ref ComponentLookup<Extractor> areaExtractors, ref ComponentLookup<HealthProblem> healthProblems, CitizenHappinessParameterData happinessParameterData, GarbageParameterData garbageParameterData, NativeArray<int> taxRates, ref ComponentLookup<CurrentDistrict> districts, ref BufferLookup<DistrictModifier> districtModifiers, ref BufferLookup<Employee> employees, ref BufferLookup<Efficiency> buildingEfficiencies, ref ComponentLookup<ExtractorAreaData> extractorDatas, ExtractorParameterData extractorParameters, ref ComponentLookup<Citizen> citizenDatas, ref ComponentLookup<Game.Citizens.Student> students, NativeArray<int> unemployment, ref BufferLookup<TradeCost> tradeCosts, ref ComponentLookup<Abandoned> abandoneds)
        {
            upkeep = Mathf.Max(1f, upkeep);
            Entity property = propertyRenter.m_Property;
            if (households.HasComponent(renter))
            {
                float commuteTime = 0f;
                if (workers.HasComponent(renter))
                {
                    commuteTime = workers[renter].m_LastCommuteTime;
                }
                Building building = buildings[property];
                DynamicBuffer<HouseholdCitizen> dynamicBuffer = householdCitizens[renter];
                int length = dynamicBuffer.Length;
                int num = 0;
                int num2 = 0;
                int num3 = 0;
                for (int i = 0; i < dynamicBuffer.Length; i++)
                {
                    Entity citizen = dynamicBuffer[i].m_Citizen;
                    Citizen citizen2 = citizenDatas[citizen];
                    num += (citizenDatas.HasComponent(citizen) ? citizenDatas[citizen].Happiness : 50);
                    if (citizen2.GetAge() == CitizenAge.Child)
                    {
                        num3++;
                    }
                    else
                    {
                        num2 += CitizenHappinessSystem.GetTaxBonuses(citizen2.GetEducationLevel(), taxRates, happinessParameterData).y;
                    }
                }
                num /= math.max(1, dynamicBuffer.Length);
                num2 /= math.max(1, dynamicBuffer.Length - num3);
                Entity prefab = prefabs[property].m_Prefab;
                float shoppingTime = HouseholdFindPropertySystem.EstimateShoppingTime(building.m_RoadEdge, building.m_CurvePosition, true, availabilities);
                float apartmentQuality = HouseholdFindPropertySystem.GetApartmentQuality(dynamicBuffer.Length, num3, property, ref building, prefab, ref buildingProperties, ref buildingDatas, ref spawnableBuildings, ref crimes, ref serviceCoverages, ref locked, ref electricityConsumers, ref adjustHappinessDatas, ref waterConsumers, ref garbageProducers, ref mailProducers, ref prefabs, ref transforms, ref abandoneds, pollutionMap, airPollutionMap, noiseMap, telecomCoverages, cityModifiers, healthcareService, entertainmentService, educationService, telecomService, garbageService, policeService, happinessParameterData, garbageParameterData, num, false);
                int householdIncome = HouseholdBehaviorSystem.GetHouseholdIncome(dynamicBuffer, ref workers, ref citizenDatas, ref healthProblems, ref economyParameters, taxRates);
                int householdExpectedIncome = HouseholdBehaviorSystem.GetHouseholdExpectedIncome(dynamicBuffer, ref students, ref healthProblems, ref citizenDatas, ref economyParameters, taxRates, unemployment);
                int householdIncomeDefaultTax = HouseholdBehaviorSystem.GetHouseholdIncomeDefaultTax(dynamicBuffer, ref workers, ref healthProblems, ref citizenDatas, ref economyParameters);
                int householdExpectedIncomeDefault = HouseholdBehaviorSystem.GetHouseholdExpectedIncomeDefault(dynamicBuffer, ref students, ref healthProblems, ref citizenDatas, ref economyParameters);
                int highestEducation = HouseholdBehaviorSystem.GetHighestEducation(dynamicBuffer, ref citizenDatas);
                float3 @float;
                float3 float2;
                int max_rent_from_utility = HouseholdFindPropertySystem.FindRentToProvideUtility(HouseholdFindPropertySystem.EvaluateDefaultProperty(householdIncomeDefaultTax, householdExpectedIncomeDefault, length, highestEducation, ref economyParameters, ref demandParameters, resourcePrefabs, resourceDatas, baseConsumptionSum, happinessParameterData, false, false, out @float, out float2), length, householdIncome, householdExpectedIncome, commuteTime, shoppingTime, apartmentQuality + (float)num2 / 2f, ref economyParameters, resourcePrefabs, resourceDatas, baseConsumptionSum, happinessParameterData);
                float max_rent_from_income = 0.45f * (float)householdIncome;
                float max_rent_from_inner = Mathf.Max(0f, Mathf.Min(max_rent_from_income, max_rent_from_utility));
                float max_rent = max_rent_from_inner > upkeep ? Mathf.Log10(max_rent_from_inner / upkeep) * upkeep + upkeep : max_rent_from_inner;
                int num4 = Mathf.RoundToInt(max_rent);
                int num44 = math.max(0, math.min(Mathf.RoundToInt((float)0.45 * (float)householdIncome), max_rent_from_utility));
                return new int4(num4, num4, num44, num44);
            }
            Entity prefab2 = prefabs[renter].m_Prefab;
            Entity prefab3 = prefabs[property].m_Prefab;
            IndustrialProcessData industrialProcessData = processDatas[prefab2];
            float num5 = 0;
            float num6 = 0;
            float num55 = 0;
            float num66 = 0;
            float efficiency = BuildingUtils.GetEfficiency(property, ref buildingEfficiencies);
            if (storageCompanies.HasComponent(renter))
            {
                num5 = 0;
                num6 = 0;
                num55 = 0;
                num66 = 0;
            }
            else if (serviceAvailables.HasComponent(renter))
            {
                WorkProvider workProvider = workProviders[renter];
                ServiceAvailable service = serviceAvailables[renter];
                ServiceCompanyData serviceData = serviceCompanyDatas[prefab2];
                WorkplaceData workplaceData = workplaceDatas[prefab2];
                DynamicBuffer<Employee> employees2 = employees[renter];
                DynamicBuffer<TradeCost> tradeCosts2 = tradeCosts[renter];
                BuildingData buildingData = buildingDatas[prefab3];
                SpawnableBuildingData spawnableBuildingData = spawnableBuildings[prefab3];
                int fittingWorkers = CommercialAISystem.GetFittingWorkers(buildingDatas[prefab3], buildingProperties[prefab3], (int)spawnableBuildingData.m_Level, serviceData);
                num5 = ServiceCompanySystem.EstimateDailyProfit(efficiency, workProvider.m_MaxWorkers, employees2, service, serviceData, buildingData, industrialProcessData, ref economyParameters, workplaceData, spawnableBuildingData, resourcePrefabs, resourceDatas, tradeCosts2);
                num5 = num5 > 0f ? num5 : 0f;
                num55 = num5;
                num6 = math.max(num5, ServiceCompanySystem.EstimateDailyProfitFull(1f, fittingWorkers, service, serviceData, buildingData, industrialProcessData, ref economyParameters, workplaceData, spawnableBuildingData, resourcePrefabs, resourceDatas, tradeCosts2));
                num66 = num6;
                int num7;
                if (districts.HasComponent(property))
                {
                    Entity district = districts[property].m_District;
                    num7 = TaxSystem.GetModifiedCommercialTaxRate(industrialProcessData.m_Output.m_Resource, taxRates, district, districtModifiers);
                }
                else
                {
                    num7 = TaxSystem.GetCommercialTaxRate(industrialProcessData.m_Output.m_Resource, taxRates);
                }
                num5 = num5 * (1f - (float)num7 / 100f);
                num5 = num5 > upkeep ? Mathf.Log10(num5 / upkeep) * upkeep + upkeep : num5;
                num55 = num55 * (1f - (float)num7 / 100f);
                num6 = num6 * (1f - (float)num7 / 100f);
                num6 = num6 > upkeep ? Mathf.Log10(num6 / upkeep) * upkeep + upkeep : num6;
                num66 = num66 * (1f - (float)num7 / 100f);
            }
            else if (processingCompanies.HasComponent(renter))
            {
                WorkProvider workProvider2 = workProviders[renter];
                if (buyingCompanies.HasComponent(renter) && tradeCosts.HasBuffer(renter))
                {
                    SpawnableBuildingData spawnableBuildingData2 = spawnableBuildings[prefab3];
                    DynamicBuffer<Employee> employees3 = employees[renter];
                    int fittingWorkers2 = IndustrialAISystem.GetFittingWorkers(buildingDatas[prefab3], buildingProperties[prefab3], (int)spawnableBuildingData2.m_Level, industrialProcessData);
                    WorkplaceData workplaceData2 = workplaceDatas[prefab2];
                    num5 = ProcessingCompanySystem.EstimateDailyProfit(employees3, efficiency, workProvider2, industrialProcessData, ref economyParameters, tradeCosts[renter], workplaceData2, spawnableBuildingData2, resourcePrefabs, resourceDatas);
                    num5 = num5 > 0 ? num5 : 0;
                    num6 = ProcessingCompanySystem.EstimateDailyProfitFull(1f, fittingWorkers2, industrialProcessData, ref economyParameters, tradeCosts[renter], workplaceData2, spawnableBuildingData2, resourcePrefabs, resourceDatas);
                    num6 = Mathf.Max(num5, num6);
                    float reduction_rate;
                    ResourceData resourceData = resourceDatas[resourcePrefabs[industrialProcessData.m_Output.m_Resource]];
                    bool flag4 = resourceData.m_Weight == 0f;
                    if (flag4)
                    {
                        reduction_rate = 1f - (float)TaxSystem.GetOfficeTaxRate(industrialProcessData.m_Output.m_Resource, taxRates) / 100f;
                    }
                    else
                    {
                        reduction_rate = 1f - (float)TaxSystem.GetIndustrialTaxRate(industrialProcessData.m_Output.m_Resource, taxRates) / 100f;
                    }
                    num5 = num5 * reduction_rate;
                    num6 = num6 * reduction_rate;
                    num55 = num5;
                    num66 = num6;
                    num5 = num5 > upkeep ? Mathf.Log10(num5 / upkeep) * upkeep + upkeep : num5;
                    num6 = num6 > upkeep ? Mathf.Log10(num6 / upkeep) * upkeep + upkeep : num6;
                }
                else if (attached.HasComponent(property))
                {
                    DynamicBuffer<Employee> employees4 = employees[renter];
                    WorkplaceData workplaceData3 = workplaceDatas[prefab2];
                    SpawnableBuildingData spawnableBuildingData3 = spawnableBuildings[prefab3];
                    num5 = ExtractorCompanySystem.EstimateDailyProfit(ExtractorCompanySystem.EstimateDailyProduction(efficiency, workProvider2.m_MaxWorkers, (int)spawnableBuildingData3.m_Level, workplaceData3, industrialProcessData, ref economyParameters), employees4, industrialProcessData, ref economyParameters, workplaceData3, spawnableBuildingData3, resourcePrefabs, resourceDatas);
                    num6 = ExtractorCompanySystem.EstimateDailyProfitFull(ExtractorCompanySystem.EstimateDailyProduction(1f, workProvider2.m_MaxWorkers, (int)spawnableBuildingData3.m_Level, workplaceData3, industrialProcessData, ref economyParameters), industrialProcessData, ref economyParameters, workplaceData3, spawnableBuildingData3, resourcePrefabs, resourceDatas);
                    num5 = num5 > 0 ? num5 : 0;
                    num6 = num6 > 0 ? num6 : 0;
                    float reduction_rate = 1f - (float)TaxSystem.GetIndustrialTaxRate(industrialProcessData.m_Output.m_Resource, taxRates) / 100f;
                    num5 = num5 * reduction_rate;
                    num55 = num5;
                    num66 = num6;
                    num5 = num5 > upkeep ? Mathf.Log10(num5 / upkeep) * upkeep + upkeep : num5;
                }
                else
                {
                    num5 = 0;
                }
            }
            return new int4((int)num5, (int)num6, (int)num55, (int)num66);
        }

        // Token: 0x060055CB RID: 21963 RVA: 0x0005E08F File Offset: 0x0005C28F
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
        }

        // Token: 0x060055CC RID: 21964 RVA: 0x0008E6D5 File Offset: 0x0008C8D5
        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            this.__AssignQueries(ref base.CheckedStateRef);
            this.__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
        }

        // Token: 0x060055CD RID: 21965 RVA: 0x0005E948 File Offset: 0x0005CB48
        [Preserve]
        public CustomRentAdjustSystem()
        {
        }

        // Token: 0x0400910D RID: 37133
        public static readonly int kUpdatesPerDay = 16;

        // Token: 0x0400910E RID: 37134
        public static readonly float kMixedCompanyRent = 0.4f;

        // Token: 0x0400910F RID: 37135
        private EntityQuery m_EconomyParameterQuery;

        // Token: 0x04009110 RID: 37136
        private EntityQuery m_DemandParameterQuery;

        // Token: 0x04009111 RID: 37137
        private SimulationSystem m_SimulationSystem;

        // Token: 0x04009112 RID: 37138
        private EndFrameBarrier m_EndFrameBarrier;

        // Token: 0x04009113 RID: 37139
        private ResourceSystem m_ResourceSystem;

        // Token: 0x04009114 RID: 37140
        private GroundPollutionSystem m_GroundPollutionSystem;

        // Token: 0x04009115 RID: 37141
        private AirPollutionSystem m_AirPollutionSystem;

        // Token: 0x04009116 RID: 37142
        private NoisePollutionSystem m_NoisePollutionSystem;

        // Token: 0x04009117 RID: 37143
        private TelecomCoverageSystem m_TelecomCoverageSystem;

        // Token: 0x04009118 RID: 37144
        private CitySystem m_CitySystem;

        // Token: 0x04009119 RID: 37145
        private TaxSystem m_TaxSystem;

        // Token: 0x0400911A RID: 37146
        private CountEmploymentSystem m_CountEmploymentSystem;

        // Token: 0x0400911B RID: 37147
        private IconCommandSystem m_IconCommandSystem;

        // Token: 0x0400911C RID: 37148
        private EntityQuery m_HealthcareParameterQuery;

        // Token: 0x0400911D RID: 37149
        private EntityQuery m_ExtractorParameterQuery;

        // Token: 0x0400911E RID: 37150
        private EntityQuery m_ParkParameterQuery;

        // Token: 0x0400911F RID: 37151
        private EntityQuery m_EducationParameterQuery;

        // Token: 0x04009120 RID: 37152
        private EntityQuery m_TelecomParameterQuery;

        // Token: 0x04009121 RID: 37153
        private EntityQuery m_GarbageParameterQuery;

        // Token: 0x04009122 RID: 37154
        private EntityQuery m_PoliceParameterQuery;

        // Token: 0x04009123 RID: 37155
        private EntityQuery m_CitizenHappinessParameterQuery;

        // Token: 0x04009124 RID: 37156
        private EntityQuery m_BuildingParameterQuery;

        // Token: 0x04009125 RID: 37157
        private EntityQuery m_PollutionParameterQuery;

        // Token: 0x04009126 RID: 37158
        private EntityQuery m_BuildingQuery;

        // Token: 0x04009127 RID: 37159
        protected int cycles;

        private static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(MyPluginInfo.PLUGIN_NAME);
        // Token: 0x04009128 RID: 37160
        private CustomRentAdjustSystem.TypeHandle __TypeHandle;

        // Token: 0x02001355 RID: 4949
        [BurstCompile]
        private struct AdjustRentJob : IJobChunk
        {
            // Token: 0x060055CF RID: 21967 RVA: 0x003B644C File Offset: 0x003B464C
            private bool CanDisplayHighRentWarnIcon(DynamicBuffer<Renter> renters)
            {
                bool result = true;
                for (int i = 0; i < renters.Length; i++)
                {
                    Entity renter = renters[i].m_Renter;
                    if (this.m_CompanyNotifications.HasComponent(renter))
                    {
                        CompanyNotifications companyNotifications = this.m_CompanyNotifications[renter];
                        if (companyNotifications.m_NoCustomersEntity != Entity.Null || companyNotifications.m_NoInputEntity != Entity.Null)
                        {
                            result = false;
                            break;
                        }
                    }
                    if (this.m_WorkProviders.HasComponent(renter))
                    {
                        WorkProvider workProvider = this.m_WorkProviders[renter];
                        if (workProvider.m_EducatedNotificationEntity != Entity.Null || workProvider.m_UneducatedNotificationEntity != Entity.Null)
                        {
                            result = false;
                            break;
                        }
                    }
                    if (this.m_Citizens.HasBuffer(renter))
                    {
                        DynamicBuffer<HouseholdCitizen> dynamicBuffer = this.m_Citizens[renter];
                        result = false;
                        for (int j = 0; j < dynamicBuffer.Length; j++)
                        {
                            if (!CitizenUtils.IsDead(dynamicBuffer[j].m_Citizen, ref this.m_HealthProblems))
                            {
                                result = true;
                                break;
                            }
                        }
                    }
                }
                return result;
            }

            // Token: 0x060055D0 RID: 21968 RVA: 0x003B6564 File Offset: 0x003B4764
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (chunk.GetSharedComponent<UpdateFrame>(this.m_UpdateFrameType).m_Index != this.m_UpdateFrameIndex)
                {
                    return;
                }
                NativeArray<Entity> nativeArray = chunk.GetNativeArray(this.m_EntityType);
                BufferAccessor<Renter> bufferAccessor = chunk.GetBufferAccessor<Renter>(ref this.m_RenterType);
                DynamicBuffer<CityModifier> cityModifiers = this.m_CityModifiers[this.m_City];
                for (int i = 0; i < nativeArray.Length; i++)
                {
                    Entity entity = nativeArray[i];
                    Entity prefab = this.m_Prefabs[entity].m_Prefab;
                    if (this.m_BuildingProperties.HasComponent(prefab))
                    {
                        BuildingPropertyData buildingPropertyData = this.m_BuildingProperties[prefab];
                        bool flag = buildingPropertyData.m_ResidentialProperties > 0 && (buildingPropertyData.m_AllowedSold != Resource.NoResource || buildingPropertyData.m_AllowedManufactured > Resource.NoResource);
                        Building building = this.m_Buildings[entity];
                        DynamicBuffer<Renter> renters = bufferAccessor[i];
                        BuildingData buildingData = this.m_BuildingDatas[prefab];
                        int num = buildingData.m_LotSize.x * buildingData.m_LotSize.y;
                        if (buildingPropertyData.m_ResidentialProperties > 0)
                        {
                            num = math.min(num, buildingPropertyData.m_ResidentialProperties);
                        }
                        if (this.m_Attached.HasComponent(entity))
                        {
                            Entity parent = this.m_Attached[entity].m_Parent;
                            float area = ExtractorAISystem.GetArea(this.m_SubAreas[parent], this.m_Lots, this.m_Geometries);
                            num += Mathf.CeilToInt(area);
                        }
                        float num2 = 0f;
                        if (this.m_LandValues.HasComponent(building.m_RoadEdge))
                        {
                            num2 = this.m_LandValues[building.m_RoadEdge].m_LandValue;
                            num2 *= (float)num;
                        }
                        Game.Zones.AreaType areaType = Game.Zones.AreaType.None;
                        if (this.m_SpawnableBuildingData.HasComponent(prefab))
                        {
                            SpawnableBuildingData spawnableBuildingData = this.m_SpawnableBuildingData[prefab];
                            areaType = this.m_ZoneData[spawnableBuildingData.m_ZonePrefab].m_AreaType;
                        }
                        if (areaType == Game.Zones.AreaType.Residential)
                        {
                            int2 groundPollutionBonuses = CitizenHappinessSystem.GetGroundPollutionBonuses(entity, ref this.m_Transforms, this.m_PollutionMap, cityModifiers, this.m_CitizenHappinessParameterData);
                            int2 noiseBonuses = CitizenHappinessSystem.GetNoiseBonuses(entity, ref this.m_Transforms, this.m_NoiseMap, this.m_CitizenHappinessParameterData);
                            int2 airPollutionBonuses = CitizenHappinessSystem.GetAirPollutionBonuses(entity, ref this.m_Transforms, this.m_AirPollutionMap, cityModifiers, this.m_CitizenHappinessParameterData);
                            bool flag2 = groundPollutionBonuses.x + groundPollutionBonuses.y < 2 * this.m_PollutionParameters.m_GroundPollutionNotificationLimit;
                            bool flag3 = airPollutionBonuses.x + airPollutionBonuses.y < 2 * this.m_PollutionParameters.m_AirPollutionNotificationLimit;
                            bool flag4 = noiseBonuses.x + noiseBonuses.y < 2 * this.m_PollutionParameters.m_NoisePollutionNotificationLimit;
                            BuildingNotifications value = this.m_BuildingNotifications[entity];
                            if (flag2 && !value.HasNotification(BuildingNotification.GroundPollution))
                            {
                                this.m_IconCommandBuffer.Add(entity, this.m_PollutionParameters.m_GroundPollutionNotification, IconPriority.Problem, IconClusterLayer.Default, (IconFlags)0, default(Entity), false, false, false, 0f);
                                value.m_Notifications |= BuildingNotification.GroundPollution;
                                this.m_BuildingNotifications[entity] = value;
                            }
                            else if (!flag2 && value.HasNotification(BuildingNotification.GroundPollution))
                            {
                                this.m_IconCommandBuffer.Remove(entity, this.m_PollutionParameters.m_GroundPollutionNotification, default(Entity), (IconFlags)0);
                                value.m_Notifications &= ~BuildingNotification.GroundPollution;
                                this.m_BuildingNotifications[entity] = value;
                            }
                            if (flag3 && !value.HasNotification(BuildingNotification.AirPollution))
                            {
                                this.m_IconCommandBuffer.Add(entity, this.m_PollutionParameters.m_AirPollutionNotification, IconPriority.Problem, IconClusterLayer.Default, (IconFlags)0, default(Entity), false, false, false, 0f);
                                value.m_Notifications |= BuildingNotification.AirPollution;
                                this.m_BuildingNotifications[entity] = value;
                            }
                            else if (!flag3 && value.HasNotification(BuildingNotification.AirPollution))
                            {
                                this.m_IconCommandBuffer.Remove(entity, this.m_PollutionParameters.m_AirPollutionNotification, default(Entity), (IconFlags)0);
                                value.m_Notifications &= ~BuildingNotification.AirPollution;
                                this.m_BuildingNotifications[entity] = value;
                            }
                            if (flag4 && !value.HasNotification(BuildingNotification.NoisePollution))
                            {
                                this.m_IconCommandBuffer.Add(entity, this.m_PollutionParameters.m_NoisePollutionNotification, IconPriority.Problem, IconClusterLayer.Default, (IconFlags)0, default(Entity), false, false, false, 0f);
                                value.m_Notifications |= BuildingNotification.NoisePollution;
                                this.m_BuildingNotifications[entity] = value;
                            }
                            else if (!flag4 && value.HasNotification(BuildingNotification.NoisePollution))
                            {
                                this.m_IconCommandBuffer.Remove(entity, this.m_PollutionParameters.m_NoisePollutionNotification, default(Entity), (IconFlags)0);
                                value.m_Notifications &= ~BuildingNotification.NoisePollution;
                                this.m_BuildingNotifications[entity] = value;
                            }
                        }
                        int2 rent = CustomRentAdjustSystem.GetRent(this.m_ConsumptionDatas[prefab], buildingPropertyData, num2, areaType);
                        float upkeep = CustomRentAdjustSystem.GetUpkeep(this.m_ConsumptionDatas[prefab], buildingPropertyData);
                        if (this.m_OnMarkets.HasComponent(entity))
                        {
                            PropertyOnMarket value2 = this.m_OnMarkets[entity];
                            value2.m_AskingRent = rent.x;
                            this.m_OnMarkets[entity] = value2;
                        }
                        int num3 = buildingPropertyData.CountProperties();
                        bool flag5 = false;
                        Entity healthcareServicePrefab = this.m_HealthcareParameters.m_HealthcareServicePrefab;
                        Entity parkServicePrefab = this.m_ParkParameters.m_ParkServicePrefab;
                        Entity educationServicePrefab = this.m_EducationParameters.m_EducationServicePrefab;
                        Entity telecomServicePrefab = this.m_TelecomParameters.m_TelecomServicePrefab;
                        Entity garbageServicePrefab = this.m_GarbageParameters.m_GarbageServicePrefab;
                        Entity policeServicePrefab = this.m_PoliceParameters.m_PoliceServicePrefab;
                        int2 @int = default(int2);
                        bool flag6 = false;
                        bool flag7 = this.m_ExtractorProperties.HasComponent(entity);
                        float num4 = (num3 > 1) ? math.saturate((float)renters.Length / (float)num3) : 0f;
                        for (int j = renters.Length - 1; j >= 0; j--)
                        {
                            Entity renter = renters[j].m_Renter;
                            if (this.m_Renters.HasComponent(renter))
                            {
                                int2 int2 = rent;
                                PropertyRenter propertyRenter = this.m_Renters[renter];
                                int4 int3 = CustomRentAdjustSystem.CalculateMaximumRent(upkeep, renter, ref this.m_EconomyParameters, ref this.m_DemandParameters, this.m_BaseConsumptionSum, cityModifiers, propertyRenter, healthcareServicePrefab, parkServicePrefab, educationServicePrefab, telecomServicePrefab, garbageServicePrefab, policeServicePrefab, ref this.m_Households, ref this.m_Workers, ref this.m_Buildings, ref this.m_Citizens, ref this.m_Prefabs, ref this.m_Availabilities, ref this.m_BuildingProperties, ref this.m_BuildingDatas, ref this.m_SpawnableBuildings, ref this.m_Crimes, ref this.m_ServiceCoverages, ref this.m_Locked, ref this.m_ElectricityConsumers, ref this.m_AdjustHappinessDatas, ref this.m_WaterConsumers, ref this.m_GarbageProducers, ref this.m_MailProducers, ref this.m_Transforms, this.m_PollutionMap, this.m_AirPollutionMap, this.m_NoiseMap, this.m_TelecomCoverages, this.m_ResourcePrefabs, ref this.m_ResourceDatas, ref this.m_ProcessDatas, ref this.m_StorageCompanies, ref this.m_ServiceAvailables, ref this.m_WorkProviders, ref this.m_ServiceCompanyDatas, ref this.m_WorkplaceDatas, ref this.m_ProcessingCompanies, ref this.m_BuyingCompanies, ref this.m_SubAreas, ref this.m_Attached, ref this.m_Lots, ref this.m_Geometries, ref this.m_AreaExtractors, ref this.m_HealthProblems, this.m_CitizenHappinessParameterData, this.m_GarbageParameters, this.m_TaxRates, ref this.m_Districts, ref this.m_DistrictModifiers, ref this.m_Employees, ref this.m_BuildingEfficiencies, ref this.m_ExtractorDatas, this.m_ExtractorParameters, ref this.m_CitizenDatas, ref this.m_Students, this.m_Unemployment, ref this.m_TradeCosts, ref this.m_Abandoneds);
                                if (flag && !this.m_Households.HasComponent(renter))
                                {
                                    int2.x = Mathf.RoundToInt(CustomRentAdjustSystem.kMixedCompanyRent * ((float)(int2.x * buildingPropertyData.m_ResidentialProperties) / (1f - CustomRentAdjustSystem.kMixedCompanyRent)));
                                    int2.y = Mathf.RoundToInt(CustomRentAdjustSystem.kMixedCompanyRent * ((float)(int2.y * buildingPropertyData.m_ResidentialProperties) / (1f - CustomRentAdjustSystem.kMixedCompanyRent)));
                                }
                                if (int2.x > int3.x)
                                {
                                    float s = 0.2f + 0.3f * num4 * num4;
                                    int2.x = Mathf.RoundToInt(math.max(math.lerp((float)int2.y, (float)int2.x, s), (float)int3.x));
                                }
                                propertyRenter.m_MaxRent = int3.x;
                                propertyRenter.m_Rent = int2.x;
                                this.m_Renters[renter] = propertyRenter;
                                flag6 |= this.m_StorageCompanies.HasComponent(renter);
                                if (int2.x > int3.w && !this.m_StorageCompanies.HasComponent(renter))
                                {
                                    this.m_CommandBuffer.AddComponent<PropertySeeker>(unfilteredChunkIndex, renter, default(PropertySeeker));
                                }
                                @int.y++;
                                if (propertyRenter.m_Rent > int3.w)
                                {
                                    @int.x++;
                                }
                            }
                            else
                            {
                                renters.RemoveAt(j);
                                flag5 = true;
                            }
                        }
                        if ((float)@int.x / math.max(1f, (float)@int.y) <= 0.7f || !this.CanDisplayHighRentWarnIcon(renters))
                        {
                            this.m_IconCommandBuffer.Remove(entity, this.m_BuildingConfigurationData.m_HighRentNotification, default(Entity), (IconFlags)0);
                            building.m_Flags &= ~Game.Buildings.BuildingFlags.HighRentWarning;
                            this.m_Buildings[entity] = building;
                        }
                        else if (renters.Length > 0 && !flag7 && (!flag6 || num3 > renters.Length) && (building.m_Flags & Game.Buildings.BuildingFlags.HighRentWarning) == Game.Buildings.BuildingFlags.None)
                        {
                            this.m_IconCommandBuffer.Add(entity, this.m_BuildingConfigurationData.m_HighRentNotification, IconPriority.Problem, IconClusterLayer.Default, (IconFlags)0, default(Entity), false, false, false, 0f);
                            building.m_Flags |= Game.Buildings.BuildingFlags.HighRentWarning;
                            this.m_Buildings[entity] = building;
                        }
                        if (renters.Length > num3 && this.m_Renters.HasComponent(renters[renters.Length - 1].m_Renter))
                        {
                            this.m_CommandBuffer.RemoveComponent<PropertyRenter>(unfilteredChunkIndex, renters[renters.Length - 1].m_Renter);
                            renters.RemoveAt(renters.Length - 1);
                        }
                        if (renters.Length == 0 && (building.m_Flags & Game.Buildings.BuildingFlags.HighRentWarning) != Game.Buildings.BuildingFlags.None)
                        {
                            this.m_IconCommandBuffer.Remove(entity, this.m_BuildingConfigurationData.m_HighRentNotification, default(Entity), (IconFlags)0);
                            building.m_Flags &= ~Game.Buildings.BuildingFlags.HighRentWarning;
                            this.m_Buildings[entity] = building;
                        }
                        if (this.m_Prefabs.HasComponent(entity) && !this.m_Abandoned.HasComponent(entity) && !this.m_Destroyed.HasComponent(entity) && flag5 && num3 > renters.Length)
                        {
                            this.m_CommandBuffer.AddComponent<PropertyOnMarket>(unfilteredChunkIndex, entity, new PropertyOnMarket
                            {
                                m_AskingRent = rent.x
                            });
                        }
                    }
                }
            }

            // Token: 0x060055D1 RID: 21969 RVA: 0x0008E70D File Offset: 0x0008C90D
            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                this.Execute(chunk, unfilteredChunkIndex, useEnabledMask, chunkEnabledMask);
            }

            // Token: 0x04009129 RID: 37161
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            // Token: 0x0400912A RID: 37162
            public BufferTypeHandle<Renter> m_RenterType;

            // Token: 0x0400912B RID: 37163
            [ReadOnly]
            public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

            // Token: 0x0400912C RID: 37164
            [NativeDisableParallelForRestriction]
            public ComponentLookup<PropertyRenter> m_Renters;

            // Token: 0x0400912D RID: 37165
            [ReadOnly]
            public ComponentLookup<Household> m_Households;

            // Token: 0x0400912E RID: 37166
            [ReadOnly]
            public ComponentLookup<Worker> m_Workers;

            // Token: 0x0400912F RID: 37167
            [NativeDisableParallelForRestriction]
            public ComponentLookup<Building> m_Buildings;

            // Token: 0x04009130 RID: 37168
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_Prefabs;

            // Token: 0x04009131 RID: 37169
            [ReadOnly]
            public ComponentLookup<BuildingPropertyData> m_BuildingProperties;

            // Token: 0x04009132 RID: 37170
            [ReadOnly]
            public ComponentLookup<BuildingData> m_BuildingDatas;

            // Token: 0x04009133 RID: 37171
            [ReadOnly]
            public ComponentLookup<WorkProvider> m_WorkProviders;

            // Token: 0x04009134 RID: 37172
            [ReadOnly]
            public ComponentLookup<IndustrialProcessData> m_ProcessDatas;

            // Token: 0x04009135 RID: 37173
            [ReadOnly]
            public ComponentLookup<ServiceAvailable> m_ServiceAvailables;

            // Token: 0x04009136 RID: 37174
            [ReadOnly]
            public ComponentLookup<ServiceCompanyData> m_ServiceCompanyDatas;

            // Token: 0x04009137 RID: 37175
            [ReadOnly]
            public ComponentLookup<Game.Companies.ProcessingCompany> m_ProcessingCompanies;

            // Token: 0x04009138 RID: 37176
            [ReadOnly]
            public ComponentLookup<BuyingCompany> m_BuyingCompanies;

            // Token: 0x04009139 RID: 37177
            [ReadOnly]
            public ComponentLookup<CompanyNotifications> m_CompanyNotifications;

            // Token: 0x0400913A RID: 37178
            [ReadOnly]
            public ComponentLookup<Attached> m_Attached;

            // Token: 0x0400913B RID: 37179
            [ReadOnly]
            public ComponentLookup<Game.Areas.Lot> m_Lots;

            // Token: 0x0400913C RID: 37180
            [ReadOnly]
            public ComponentLookup<Geometry> m_Geometries;

            // Token: 0x0400913D RID: 37181
            [ReadOnly]
            public ComponentLookup<Extractor> m_AreaExtractors;

            // Token: 0x0400913E RID: 37182
            [ReadOnly]
            public ComponentLookup<LandValue> m_LandValues;

            // Token: 0x0400913F RID: 37183
            [NativeDisableParallelForRestriction]
            public ComponentLookup<PropertyOnMarket> m_OnMarkets;

            // Token: 0x04009140 RID: 37184
            [ReadOnly]
            public ComponentLookup<ConsumptionData> m_ConsumptionDatas;

            // Token: 0x04009141 RID: 37185
            [ReadOnly]
            public BufferLookup<HouseholdCitizen> m_Citizens;

            // Token: 0x04009142 RID: 37186
            [ReadOnly]
            public BufferLookup<Game.Net.ServiceCoverage> m_ServiceCoverages;

            // Token: 0x04009143 RID: 37187
            [ReadOnly]
            public BufferLookup<ResourceAvailability> m_Availabilities;

            // Token: 0x04009144 RID: 37188
            [ReadOnly]
            public BufferLookup<Game.Areas.SubArea> m_SubAreas;

            // Token: 0x04009145 RID: 37189
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildings;

            // Token: 0x04009146 RID: 37190
            [ReadOnly]
            public ComponentLookup<ResourceData> m_ResourceDatas;

            // Token: 0x04009147 RID: 37191
            [ReadOnly]
            public ComponentLookup<Game.Companies.StorageCompany> m_StorageCompanies;

            // Token: 0x04009148 RID: 37192
            [ReadOnly]
            public ComponentLookup<WorkplaceData> m_WorkplaceDatas;

            // Token: 0x04009149 RID: 37193
            [ReadOnly]
            public ComponentLookup<Abandoned> m_Abandoned;

            // Token: 0x0400914A RID: 37194
            [ReadOnly]
            public ComponentLookup<Destroyed> m_Destroyed;

            // Token: 0x0400914B RID: 37195
            [ReadOnly]
            public ComponentLookup<CrimeProducer> m_Crimes;

            // Token: 0x0400914C RID: 37196
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> m_Transforms;

            // Token: 0x0400914D RID: 37197
            [ReadOnly]
            public ComponentLookup<Locked> m_Locked;

            // Token: 0x0400914E RID: 37198
            [ReadOnly]
            public BufferLookup<CityModifier> m_CityModifiers;

            // Token: 0x0400914F RID: 37199
            [ReadOnly]
            public ComponentLookup<CurrentDistrict> m_Districts;

            // Token: 0x04009150 RID: 37200
            [ReadOnly]
            public BufferLookup<DistrictModifier> m_DistrictModifiers;

            // Token: 0x04009151 RID: 37201
            [ReadOnly]
            public ComponentLookup<HealthProblem> m_HealthProblems;

            // Token: 0x04009152 RID: 37202
            [ReadOnly]
            public BufferLookup<Employee> m_Employees;

            // Token: 0x04009153 RID: 37203
            [ReadOnly]
            public BufferLookup<Efficiency> m_BuildingEfficiencies;

            // Token: 0x04009154 RID: 37204
            [ReadOnly]
            public ComponentLookup<ExtractorAreaData> m_ExtractorDatas;

            // Token: 0x04009155 RID: 37205
            [ReadOnly]
            public ComponentLookup<Citizen> m_CitizenDatas;

            // Token: 0x04009156 RID: 37206
            [ReadOnly]
            public ComponentLookup<Game.Citizens.Student> m_Students;

            // Token: 0x04009157 RID: 37207
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingData;

            // Token: 0x04009158 RID: 37208
            [ReadOnly]
            public ComponentLookup<ZoneData> m_ZoneData;

            // Token: 0x04009159 RID: 37209
            [ReadOnly]
            public BufferLookup<TradeCost> m_TradeCosts;

            // Token: 0x0400915A RID: 37210
            [ReadOnly]
            public ComponentLookup<ElectricityConsumer> m_ElectricityConsumers;

            // Token: 0x0400915B RID: 37211
            [ReadOnly]
            public ComponentLookup<AdjustHappinessData> m_AdjustHappinessDatas;

            // Token: 0x0400915C RID: 37212
            [ReadOnly]
            public ComponentLookup<WaterConsumer> m_WaterConsumers;

            // Token: 0x0400915D RID: 37213
            [ReadOnly]
            public ComponentLookup<GarbageProducer> m_GarbageProducers;

            // Token: 0x0400915E RID: 37214
            [ReadOnly]
            public ComponentLookup<MailProducer> m_MailProducers;

            // Token: 0x0400915F RID: 37215
            [ReadOnly]
            public ComponentLookup<Abandoned> m_Abandoneds;

            // Token: 0x04009160 RID: 37216
            [ReadOnly]
            public ComponentLookup<ExtractorProperty> m_ExtractorProperties;

            // Token: 0x04009161 RID: 37217
            [NativeDisableParallelForRestriction]
            public ComponentLookup<BuildingNotifications> m_BuildingNotifications;

            // Token: 0x04009162 RID: 37218
            [ReadOnly]
            public NativeArray<AirPollution> m_AirPollutionMap;

            // Token: 0x04009163 RID: 37219
            [ReadOnly]
            public NativeArray<GroundPollution> m_PollutionMap;

            // Token: 0x04009164 RID: 37220
            [ReadOnly]
            public NativeArray<NoisePollution> m_NoiseMap;

            // Token: 0x04009165 RID: 37221
            [ReadOnly]
            public CellMapData<TelecomCoverage> m_TelecomCoverages;

            // Token: 0x04009166 RID: 37222
            [ReadOnly]
            public NativeArray<int> m_Unemployment;

            // Token: 0x04009167 RID: 37223
            public ExtractorParameterData m_ExtractorParameters;

            // Token: 0x04009168 RID: 37224
            public HealthcareParameterData m_HealthcareParameters;

            // Token: 0x04009169 RID: 37225
            public ParkParameterData m_ParkParameters;

            // Token: 0x0400916A RID: 37226
            public EducationParameterData m_EducationParameters;

            // Token: 0x0400916B RID: 37227
            public TelecomParameterData m_TelecomParameters;

            // Token: 0x0400916C RID: 37228
            public GarbageParameterData m_GarbageParameters;

            // Token: 0x0400916D RID: 37229
            public PoliceConfigurationData m_PoliceParameters;

            // Token: 0x0400916E RID: 37230
            public CitizenHappinessParameterData m_CitizenHappinessParameterData;

            // Token: 0x0400916F RID: 37231
            public BuildingConfigurationData m_BuildingConfigurationData;

            // Token: 0x04009170 RID: 37232
            public PollutionParameterData m_PollutionParameters;

            // Token: 0x04009171 RID: 37233
            public IconCommandBuffer m_IconCommandBuffer;

            // Token: 0x04009172 RID: 37234
            [ReadOnly]
            public NativeArray<int> m_TaxRates;

            // Token: 0x04009173 RID: 37235
            [ReadOnly]
            public ResourcePrefabs m_ResourcePrefabs;

            // Token: 0x04009174 RID: 37236
            public uint m_UpdateFrameIndex;

            // Token: 0x04009175 RID: 37237
            public float m_BaseConsumptionSum;

            // Token: 0x04009176 RID: 37238
            [ReadOnly]
            public Entity m_City;

            // Token: 0x04009177 RID: 37239
            public EconomyParameterData m_EconomyParameters;

            // Token: 0x04009178 RID: 37240
            public DemandParameterData m_DemandParameters;

            // Token: 0x04009179 RID: 37241
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
        }

        // Token: 0x02001356 RID: 4950
        private struct TypeHandle
        {
            // Token: 0x060055D2 RID: 21970 RVA: 0x003B7008 File Offset: 0x003B5208
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                this.__Game_Buildings_Renter_RW_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(false);
                this.__Game_Buildings_PropertyRenter_RW_ComponentLookup = state.GetComponentLookup<PropertyRenter>(false);
                this.__Game_Buildings_PropertyOnMarket_RW_ComponentLookup = state.GetComponentLookup<PropertyOnMarket>(false);
                this.__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(true);
                this.__Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(true);
                this.__Game_Buildings_Building_RW_ComponentLookup = state.GetComponentLookup<Building>(false);
                this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(true);
                this.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(true);
                this.__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(true);
                this.__Game_Companies_WorkProvider_RO_ComponentLookup = state.GetComponentLookup<WorkProvider>(true);
                this.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(true);
                this.__Game_Companies_ServiceAvailable_RO_ComponentLookup = state.GetComponentLookup<ServiceAvailable>(true);
                this.__Game_Companies_ServiceCompanyData_RO_ComponentLookup = state.GetComponentLookup<ServiceCompanyData>(true);
                this.__Game_Companies_ProcessingCompany_RO_ComponentLookup = state.GetComponentLookup<Game.Companies.ProcessingCompany>(true);
                this.__Game_Companies_BuyingCompany_RO_ComponentLookup = state.GetComponentLookup<BuyingCompany>(true);
                this.__Game_Companies_CompanyNotifications_RO_ComponentLookup = state.GetComponentLookup<CompanyNotifications>(true);
                this.__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(true);
                this.__Game_Areas_Lot_RO_ComponentLookup = state.GetComponentLookup<Game.Areas.Lot>(true);
                this.__Game_Areas_Geometry_RO_ComponentLookup = state.GetComponentLookup<Geometry>(true);
                this.__Game_Areas_Extractor_RO_ComponentLookup = state.GetComponentLookup<Extractor>(true);
                this.__Game_Net_LandValue_RO_ComponentLookup = state.GetComponentLookup<LandValue>(true);
                this.__Game_Prefabs_ConsumptionData_RO_ComponentLookup = state.GetComponentLookup<ConsumptionData>(true);
                this.__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(true);
                this.__Game_Net_ServiceCoverage_RO_BufferLookup = state.GetBufferLookup<Game.Net.ServiceCoverage>(true);
                this.__Game_Net_ResourceAvailability_RO_BufferLookup = state.GetBufferLookup<ResourceAvailability>(true);
                this.__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(true);
                this.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(true);
                this.__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(true);
                this.__Game_Companies_StorageCompany_RO_ComponentLookup = state.GetComponentLookup<Game.Companies.StorageCompany>(true);
                this.__Game_Prefabs_WorkplaceData_RO_ComponentLookup = state.GetComponentLookup<WorkplaceData>(true);
                this.__Game_Buildings_Abandoned_RO_ComponentLookup = state.GetComponentLookup<Abandoned>(true);
                this.__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(true);
                this.__Game_Buildings_CrimeProducer_RO_ComponentLookup = state.GetComponentLookup<CrimeProducer>(true);
                this.__Game_Prefabs_Locked_RO_ComponentLookup = state.GetComponentLookup<Locked>(true);
                this.__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(true);
                this.__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(true);
                this.__Game_Areas_CurrentDistrict_RO_ComponentLookup = state.GetComponentLookup<CurrentDistrict>(true);
                this.__Game_Areas_DistrictModifier_RO_BufferLookup = state.GetBufferLookup<DistrictModifier>(true);
                this.__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(true);
                this.__Game_Companies_Employee_RO_BufferLookup = state.GetBufferLookup<Employee>(true);
                this.__Game_Buildings_Efficiency_RO_BufferLookup = state.GetBufferLookup<Efficiency>(true);
                this.__Game_Prefabs_ExtractorAreaData_RO_ComponentLookup = state.GetComponentLookup<ExtractorAreaData>(true);
                this.__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(true);
                this.__Game_Citizens_Student_RO_ComponentLookup = state.GetComponentLookup<Game.Citizens.Student>(true);
                this.__Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(true);
                this.__Game_Companies_TradeCost_RO_BufferLookup = state.GetBufferLookup<TradeCost>(true);
                this.__Game_Buildings_BuildingNotifications_RW_ComponentLookup = state.GetComponentLookup<BuildingNotifications>(false);
                this.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup = state.GetComponentLookup<ElectricityConsumer>(true);
                this.__Game_Prefabs_AdjustHappinessData_RO_ComponentLookup = state.GetComponentLookup<AdjustHappinessData>(true);
                this.__Game_Buildings_WaterConsumer_RO_ComponentLookup = state.GetComponentLookup<WaterConsumer>(true);
                this.__Game_Buildings_GarbageProducer_RO_ComponentLookup = state.GetComponentLookup<GarbageProducer>(true);
                this.__Game_Buildings_MailProducer_RO_ComponentLookup = state.GetComponentLookup<MailProducer>(true);
                this.__Game_Buildings_ExtractorProperty_RO_ComponentLookup = state.GetComponentLookup<ExtractorProperty>(true);
            }

            // Token: 0x0400917A RID: 37242
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

            // Token: 0x0400917B RID: 37243
            public BufferTypeHandle<Renter> __Game_Buildings_Renter_RW_BufferTypeHandle;

            // Token: 0x0400917C RID: 37244
            public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RW_ComponentLookup;

            // Token: 0x0400917D RID: 37245
            public ComponentLookup<PropertyOnMarket> __Game_Buildings_PropertyOnMarket_RW_ComponentLookup;

            // Token: 0x0400917E RID: 37246
            [ReadOnly]
            public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

            // Token: 0x0400917F RID: 37247
            [ReadOnly]
            public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;

            // Token: 0x04009180 RID: 37248
            public ComponentLookup<Building> __Game_Buildings_Building_RW_ComponentLookup;

            // Token: 0x04009181 RID: 37249
            [ReadOnly]
            public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

            // Token: 0x04009182 RID: 37250
            [ReadOnly]
            public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

            // Token: 0x04009183 RID: 37251
            [ReadOnly]
            public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

            // Token: 0x04009184 RID: 37252
            [ReadOnly]
            public ComponentLookup<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentLookup;

            // Token: 0x04009185 RID: 37253
            [ReadOnly]
            public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

            // Token: 0x04009186 RID: 37254
            [ReadOnly]
            public ComponentLookup<ServiceAvailable> __Game_Companies_ServiceAvailable_RO_ComponentLookup;

            // Token: 0x04009187 RID: 37255
            [ReadOnly]
            public ComponentLookup<ServiceCompanyData> __Game_Companies_ServiceCompanyData_RO_ComponentLookup;

            // Token: 0x04009188 RID: 37256
            [ReadOnly]
            public ComponentLookup<Game.Companies.ProcessingCompany> __Game_Companies_ProcessingCompany_RO_ComponentLookup;

            // Token: 0x04009189 RID: 37257
            [ReadOnly]
            public ComponentLookup<BuyingCompany> __Game_Companies_BuyingCompany_RO_ComponentLookup;

            // Token: 0x0400918A RID: 37258
            [ReadOnly]
            public ComponentLookup<CompanyNotifications> __Game_Companies_CompanyNotifications_RO_ComponentLookup;

            // Token: 0x0400918B RID: 37259
            [ReadOnly]
            public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

            // Token: 0x0400918C RID: 37260
            [ReadOnly]
            public ComponentLookup<Game.Areas.Lot> __Game_Areas_Lot_RO_ComponentLookup;

            // Token: 0x0400918D RID: 37261
            [ReadOnly]
            public ComponentLookup<Geometry> __Game_Areas_Geometry_RO_ComponentLookup;

            // Token: 0x0400918E RID: 37262
            [ReadOnly]
            public ComponentLookup<Extractor> __Game_Areas_Extractor_RO_ComponentLookup;

            // Token: 0x0400918F RID: 37263
            [ReadOnly]
            public ComponentLookup<LandValue> __Game_Net_LandValue_RO_ComponentLookup;

            // Token: 0x04009190 RID: 37264
            [ReadOnly]
            public ComponentLookup<ConsumptionData> __Game_Prefabs_ConsumptionData_RO_ComponentLookup;

            // Token: 0x04009191 RID: 37265
            [ReadOnly]
            public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

            // Token: 0x04009192 RID: 37266
            [ReadOnly]
            public BufferLookup<Game.Net.ServiceCoverage> __Game_Net_ServiceCoverage_RO_BufferLookup;

            // Token: 0x04009193 RID: 37267
            [ReadOnly]
            public BufferLookup<ResourceAvailability> __Game_Net_ResourceAvailability_RO_BufferLookup;

            // Token: 0x04009194 RID: 37268
            [ReadOnly]
            public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

            // Token: 0x04009195 RID: 37269
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

            // Token: 0x04009196 RID: 37270
            [ReadOnly]
            public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

            // Token: 0x04009197 RID: 37271
            [ReadOnly]
            public ComponentLookup<Game.Companies.StorageCompany> __Game_Companies_StorageCompany_RO_ComponentLookup;

            // Token: 0x04009198 RID: 37272
            [ReadOnly]
            public ComponentLookup<WorkplaceData> __Game_Prefabs_WorkplaceData_RO_ComponentLookup;

            // Token: 0x04009199 RID: 37273
            [ReadOnly]
            public ComponentLookup<Abandoned> __Game_Buildings_Abandoned_RO_ComponentLookup;

            // Token: 0x0400919A RID: 37274
            [ReadOnly]
            public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

            // Token: 0x0400919B RID: 37275
            [ReadOnly]
            public ComponentLookup<CrimeProducer> __Game_Buildings_CrimeProducer_RO_ComponentLookup;

            // Token: 0x0400919C RID: 37276
            [ReadOnly]
            public ComponentLookup<Locked> __Game_Prefabs_Locked_RO_ComponentLookup;

            // Token: 0x0400919D RID: 37277
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

            // Token: 0x0400919E RID: 37278
            [ReadOnly]
            public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

            // Token: 0x0400919F RID: 37279
            [ReadOnly]
            public ComponentLookup<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentLookup;

            // Token: 0x040091A0 RID: 37280
            [ReadOnly]
            public BufferLookup<DistrictModifier> __Game_Areas_DistrictModifier_RO_BufferLookup;

            // Token: 0x040091A1 RID: 37281
            [ReadOnly]
            public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;

            // Token: 0x040091A2 RID: 37282
            [ReadOnly]
            public BufferLookup<Employee> __Game_Companies_Employee_RO_BufferLookup;

            // Token: 0x040091A3 RID: 37283
            [ReadOnly]
            public BufferLookup<Efficiency> __Game_Buildings_Efficiency_RO_BufferLookup;

            // Token: 0x040091A4 RID: 37284
            [ReadOnly]
            public ComponentLookup<ExtractorAreaData> __Game_Prefabs_ExtractorAreaData_RO_ComponentLookup;

            // Token: 0x040091A5 RID: 37285
            [ReadOnly]
            public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

            // Token: 0x040091A6 RID: 37286
            [ReadOnly]
            public ComponentLookup<Game.Citizens.Student> __Game_Citizens_Student_RO_ComponentLookup;

            // Token: 0x040091A7 RID: 37287
            [ReadOnly]
            public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;

            // Token: 0x040091A8 RID: 37288
            [ReadOnly]
            public BufferLookup<TradeCost> __Game_Companies_TradeCost_RO_BufferLookup;

            // Token: 0x040091A9 RID: 37289
            public ComponentLookup<BuildingNotifications> __Game_Buildings_BuildingNotifications_RW_ComponentLookup;

            // Token: 0x040091AA RID: 37290
            [ReadOnly]
            public ComponentLookup<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RO_ComponentLookup;

            // Token: 0x040091AB RID: 37291
            [ReadOnly]
            public ComponentLookup<AdjustHappinessData> __Game_Prefabs_AdjustHappinessData_RO_ComponentLookup;

            // Token: 0x040091AC RID: 37292
            [ReadOnly]
            public ComponentLookup<WaterConsumer> __Game_Buildings_WaterConsumer_RO_ComponentLookup;

            // Token: 0x040091AD RID: 37293
            [ReadOnly]
            public ComponentLookup<GarbageProducer> __Game_Buildings_GarbageProducer_RO_ComponentLookup;

            // Token: 0x040091AE RID: 37294
            [ReadOnly]
            public ComponentLookup<MailProducer> __Game_Buildings_MailProducer_RO_ComponentLookup;

            // Token: 0x040091AF RID: 37295
            [ReadOnly]
            public ComponentLookup<ExtractorProperty> __Game_Buildings_ExtractorProperty_RO_ComponentLookup;
        }
    }
}
