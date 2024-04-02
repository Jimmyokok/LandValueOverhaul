using System;
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
using Game.Zones;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using Game;
using Game.Simulation;


namespace LandValueOverhaul.Systems
{
    // Token: 0x02001469 RID: 5225
    public partial class RentAdjustSystem : GameSystemBase
    {
        // Token: 0x06005C45 RID: 23621 RVA: 0x0036919E File Offset: 0x0036739E
        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 262144 / (RentAdjustSystem.kUpdatesPerDay * 16);
        }

        // Token: 0x06005C46 RID: 23622 RVA: 0x003691B0 File Offset: 0x003673B0
        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            Mod.log.Info("Modded RentAdjustSystem created!");
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

        // Token: 0x06005C47 RID: 23623 RVA: 0x003694A0 File Offset: 0x003676A0
        [Preserve]
        protected override void OnUpdate()
        {
            uint updateFrame = SimulationUtils.GetUpdateFrame(this.m_SimulationSystem.frameIndex, RentAdjustSystem.kUpdatesPerDay, 16);
            this.__TypeHandle.__Game_Buildings_ExtractorProperty_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_MailProducer_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_GarbageProducer_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup.Update(ref base.CheckedStateRef);
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
            RentAdjustSystem.AdjustRentJob jobData = default(RentAdjustSystem.AdjustRentJob);
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

        // Token: 0x06005C48 RID: 23624 RVA: 0x00369FA4 File Offset: 0x003681A4
        public static int2 GetRent(ConsumptionData consumptionData, BuildingPropertyData buildingProperties, float landValue, Game.Zones.AreaType areaType)
        {
            float2 @float;
            @float.x = (float)consumptionData.m_Upkeep;
            @float.y = @float.x;
            float num;
            if (buildingProperties.m_ResidentialProperties > 0 && (buildingProperties.m_AllowedSold != Resource.NoResource || buildingProperties.m_AllowedManufactured > Resource.NoResource))
            {
                num = (float)Mathf.RoundToInt((float)buildingProperties.m_ResidentialProperties / (1f - RentAdjustSystem.kMixedCompanyRent));
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
                num = (float)Mathf.RoundToInt((float)buildingProperties.m_ResidentialProperties / (1f - RentAdjustSystem.kMixedCompanyRent));
            }
            else
            {
                num = (float)buildingProperties.CountProperties();
            }
            @float /= num;
            return Mathf.Max(1f, @float);
        }

        // Token: 0x06005C49 RID: 23625 RVA: 0x0036A05C File Offset: 0x0036825C
        public static int4 CalculateMaximumRent(float upkeep, Entity renter, ref EconomyParameterData economyParameters, ref DemandParameterData demandParameters, float baseConsumptionSum, DynamicBuffer<CityModifier> cityModifiers, PropertyRenter propertyRenter, Entity healthcareService, Entity entertainmentService, Entity educationService, Entity telecomService, Entity garbageService, Entity policeService, ref ComponentLookup<Household> households, ref ComponentLookup<Worker> workers, ref ComponentLookup<Building> buildings, ref BufferLookup<HouseholdCitizen> householdCitizens, ref ComponentLookup<PrefabRef> prefabs, ref BufferLookup<ResourceAvailability> availabilities, ref ComponentLookup<BuildingPropertyData> buildingProperties, ref ComponentLookup<BuildingData> buildingDatas, ref ComponentLookup<SpawnableBuildingData> spawnableBuildings, ref ComponentLookup<CrimeProducer> crimes, ref BufferLookup<Game.Net.ServiceCoverage> serviceCoverages, ref ComponentLookup<Locked> locked, ref ComponentLookup<ElectricityConsumer> electricityConsumers, ref ComponentLookup<WaterConsumer> waterConsumers, ref ComponentLookup<GarbageProducer> garbageProducers, ref ComponentLookup<MailProducer> mailProducers, ref ComponentLookup<Game.Objects.Transform> transforms, NativeArray<GroundPollution> pollutionMap, NativeArray<AirPollution> airPollutionMap, NativeArray<NoisePollution> noiseMap, CellMapData<TelecomCoverage> telecomCoverages, ResourcePrefabs resourcePrefabs, ref ComponentLookup<ResourceData> resourceDatas, ref ComponentLookup<IndustrialProcessData> processDatas, ref ComponentLookup<Game.Companies.StorageCompany> storageCompanies, ref ComponentLookup<ServiceAvailable> serviceAvailables, ref ComponentLookup<WorkProvider> workProviders, ref ComponentLookup<ServiceCompanyData> serviceCompanyDatas, ref ComponentLookup<WorkplaceData> workplaceDatas, ref ComponentLookup<Game.Companies.ProcessingCompany> processingCompanies, ref ComponentLookup<BuyingCompany> buyingCompanies, ref BufferLookup<Game.Areas.SubArea> subAreas, ref ComponentLookup<Attached> attached, ref ComponentLookup<Game.Areas.Lot> lots, ref ComponentLookup<Geometry> geometries, ref ComponentLookup<Extractor> areaExtractors, ref ComponentLookup<HealthProblem> healthProblems, CitizenHappinessParameterData happinessParameterData, GarbageParameterData garbageParameterData, NativeArray<int> taxRates, ref ComponentLookup<CurrentDistrict> districts, ref BufferLookup<DistrictModifier> districtModifiers, ref BufferLookup<Employee> employees, ref BufferLookup<Efficiency> buildingEfficiencies, ref ComponentLookup<ExtractorAreaData> extractorDatas, ExtractorParameterData extractorParameters, ref ComponentLookup<Citizen> citizenDatas, ref ComponentLookup<Game.Citizens.Student> students, NativeArray<int> unemployment, ref BufferLookup<TradeCost> tradeCosts, ref ComponentLookup<Abandoned> abandoneds)
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
                float apartmentQuality = HouseholdFindPropertySystem.GetApartmentQuality(dynamicBuffer.Length, num3, property, ref building, prefab, ref buildingProperties, ref buildingDatas, ref spawnableBuildings, ref crimes, ref serviceCoverages, ref locked, ref electricityConsumers, ref waterConsumers, ref garbageProducers, ref mailProducers, ref prefabs, ref transforms, ref abandoneds, pollutionMap, airPollutionMap, noiseMap, telecomCoverages, cityModifiers, healthcareService, entertainmentService, educationService, telecomService, garbageService, policeService, happinessParameterData, garbageParameterData, num, false);
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

        // Token: 0x06005C4A RID: 23626 RVA: 0x00003211 File Offset: 0x00001411
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
        }

        // Token: 0x06005C4B RID: 23627 RVA: 0x0036A64D File Offset: 0x0036884D
        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            this.__AssignQueries(ref base.CheckedStateRef);
            this.__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
        }

        // Token: 0x06005C4C RID: 23628 RVA: 0x00006D67 File Offset: 0x00004F67
        [Preserve]
        public RentAdjustSystem()
        {
        }

        // Token: 0x0400961E RID: 38430
        public static readonly int kUpdatesPerDay = 16;

        // Token: 0x0400961F RID: 38431
        public static readonly float kMixedCompanyRent = 0.4f;

        // Token: 0x04009620 RID: 38432
        private EntityQuery m_EconomyParameterQuery;

        // Token: 0x04009621 RID: 38433
        private EntityQuery m_DemandParameterQuery;

        // Token: 0x04009622 RID: 38434
        private SimulationSystem m_SimulationSystem;

        // Token: 0x04009623 RID: 38435
        private EndFrameBarrier m_EndFrameBarrier;

        // Token: 0x04009624 RID: 38436
        private ResourceSystem m_ResourceSystem;

        // Token: 0x04009625 RID: 38437
        private GroundPollutionSystem m_GroundPollutionSystem;

        // Token: 0x04009626 RID: 38438
        private AirPollutionSystem m_AirPollutionSystem;

        // Token: 0x04009627 RID: 38439
        private NoisePollutionSystem m_NoisePollutionSystem;

        // Token: 0x04009628 RID: 38440
        private TelecomCoverageSystem m_TelecomCoverageSystem;

        // Token: 0x04009629 RID: 38441
        private CitySystem m_CitySystem;

        // Token: 0x0400962A RID: 38442
        private TaxSystem m_TaxSystem;

        // Token: 0x0400962B RID: 38443
        private CountEmploymentSystem m_CountEmploymentSystem;

        // Token: 0x0400962C RID: 38444
        private IconCommandSystem m_IconCommandSystem;

        // Token: 0x0400962D RID: 38445
        private EntityQuery m_HealthcareParameterQuery;

        // Token: 0x0400962E RID: 38446
        private EntityQuery m_ExtractorParameterQuery;

        // Token: 0x0400962F RID: 38447
        private EntityQuery m_ParkParameterQuery;

        // Token: 0x04009630 RID: 38448
        private EntityQuery m_EducationParameterQuery;

        // Token: 0x04009631 RID: 38449
        private EntityQuery m_TelecomParameterQuery;

        // Token: 0x04009632 RID: 38450
        private EntityQuery m_GarbageParameterQuery;

        // Token: 0x04009633 RID: 38451
        private EntityQuery m_PoliceParameterQuery;

        // Token: 0x04009634 RID: 38452
        private EntityQuery m_CitizenHappinessParameterQuery;

        // Token: 0x04009635 RID: 38453
        private EntityQuery m_BuildingParameterQuery;

        // Token: 0x04009636 RID: 38454
        private EntityQuery m_PollutionParameterQuery;

        // Token: 0x04009637 RID: 38455
        private EntityQuery m_BuildingQuery;

        // Token: 0x04009638 RID: 38456
        protected int cycles;

        // Token: 0x04009639 RID: 38457
        private RentAdjustSystem.TypeHandle __TypeHandle;

        // Token: 0x0200146A RID: 5226
        
        private struct AdjustRentJob : IJobChunk
        {
            // Token: 0x06005C4E RID: 23630 RVA: 0x0036A688 File Offset: 0x00368888
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

            // Token: 0x06005C4F RID: 23631 RVA: 0x0036A7A0 File Offset: 0x003689A0
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
                            num = math.min(num, Mathf.CeilToInt(math.sqrt((float)num * (float)buildingPropertyData.m_ResidentialProperties)));
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
                        int2 rent = RentAdjustSystem.GetRent(this.m_ConsumptionDatas[prefab], buildingPropertyData, num2, areaType);
                        float upkeep = RentAdjustSystem.GetUpkeep(this.m_ConsumptionDatas[prefab], buildingPropertyData);
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
                                int4 int3 = RentAdjustSystem.CalculateMaximumRent(upkeep, renter, ref this.m_EconomyParameters, ref this.m_DemandParameters, this.m_BaseConsumptionSum, cityModifiers, propertyRenter, healthcareServicePrefab, parkServicePrefab, educationServicePrefab, telecomServicePrefab, garbageServicePrefab, policeServicePrefab, ref this.m_Households, ref this.m_Workers, ref this.m_Buildings, ref this.m_Citizens, ref this.m_Prefabs, ref this.m_Availabilities, ref this.m_BuildingProperties, ref this.m_BuildingDatas, ref this.m_SpawnableBuildings, ref this.m_Crimes, ref this.m_ServiceCoverages, ref this.m_Locked, ref this.m_ElectricityConsumers, ref this.m_WaterConsumers, ref this.m_GarbageProducers, ref this.m_MailProducers, ref this.m_Transforms, this.m_PollutionMap, this.m_AirPollutionMap, this.m_NoiseMap, this.m_TelecomCoverages, this.m_ResourcePrefabs, ref this.m_ResourceDatas, ref this.m_ProcessDatas, ref this.m_StorageCompanies, ref this.m_ServiceAvailables, ref this.m_WorkProviders, ref this.m_ServiceCompanyDatas, ref this.m_WorkplaceDatas, ref this.m_ProcessingCompanies, ref this.m_BuyingCompanies, ref this.m_SubAreas, ref this.m_Attached, ref this.m_Lots, ref this.m_Geometries, ref this.m_AreaExtractors, ref this.m_HealthProblems, this.m_CitizenHappinessParameterData, this.m_GarbageParameters, this.m_TaxRates, ref this.m_Districts, ref this.m_DistrictModifiers, ref this.m_Employees, ref this.m_BuildingEfficiencies, ref this.m_ExtractorDatas, this.m_ExtractorParameters, ref this.m_CitizenDatas, ref this.m_Students, this.m_Unemployment, ref this.m_TradeCosts, ref this.m_Abandoneds);
                                if (flag && !this.m_Households.HasComponent(renter))
                                {
                                    int2.x = Mathf.RoundToInt(RentAdjustSystem.kMixedCompanyRent * ((float)(int2.x * buildingPropertyData.m_ResidentialProperties) / (1f - RentAdjustSystem.kMixedCompanyRent)));
                                    int2.y = Mathf.RoundToInt(RentAdjustSystem.kMixedCompanyRent * ((float)(int2.y * buildingPropertyData.m_ResidentialProperties) / (1f - RentAdjustSystem.kMixedCompanyRent)));
                                }
                                if (int2.x > int3.x)
                                {
                                    float s = 0.2f + 0.3f * num4 * num4;
                                    int2.x = Mathf.RoundToInt(math.max(math.lerp((float)int2.y, (float)int2.x, s), (float)int3.x));
                                }
                                propertyRenter.m_Rent = int2.x;
                                propertyRenter.m_MaxRent = int3.x;
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

            // Token: 0x06005C50 RID: 23632 RVA: 0x0036B23E File Offset: 0x0036943E
            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                this.Execute(chunk, unfilteredChunkIndex, useEnabledMask, chunkEnabledMask);
            }

            // Token: 0x0400963A RID: 38458
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            // Token: 0x0400963B RID: 38459
            public BufferTypeHandle<Renter> m_RenterType;

            // Token: 0x0400963C RID: 38460
            [ReadOnly]
            public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

            // Token: 0x0400963D RID: 38461
            [NativeDisableParallelForRestriction]
            public ComponentLookup<PropertyRenter> m_Renters;

            // Token: 0x0400963E RID: 38462
            [ReadOnly]
            public ComponentLookup<Household> m_Households;

            // Token: 0x0400963F RID: 38463
            [ReadOnly]
            public ComponentLookup<Worker> m_Workers;

            // Token: 0x04009640 RID: 38464
            [NativeDisableParallelForRestriction]
            public ComponentLookup<Building> m_Buildings;

            // Token: 0x04009641 RID: 38465
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_Prefabs;

            // Token: 0x04009642 RID: 38466
            [ReadOnly]
            public ComponentLookup<BuildingPropertyData> m_BuildingProperties;

            // Token: 0x04009643 RID: 38467
            [ReadOnly]
            public ComponentLookup<BuildingData> m_BuildingDatas;

            // Token: 0x04009644 RID: 38468
            [ReadOnly]
            public ComponentLookup<WorkProvider> m_WorkProviders;

            // Token: 0x04009645 RID: 38469
            [ReadOnly]
            public ComponentLookup<IndustrialProcessData> m_ProcessDatas;

            // Token: 0x04009646 RID: 38470
            [ReadOnly]
            public ComponentLookup<ServiceAvailable> m_ServiceAvailables;

            // Token: 0x04009647 RID: 38471
            [ReadOnly]
            public ComponentLookup<ServiceCompanyData> m_ServiceCompanyDatas;

            // Token: 0x04009648 RID: 38472
            [ReadOnly]
            public ComponentLookup<Game.Companies.ProcessingCompany> m_ProcessingCompanies;

            // Token: 0x04009649 RID: 38473
            [ReadOnly]
            public ComponentLookup<BuyingCompany> m_BuyingCompanies;

            // Token: 0x0400964A RID: 38474
            [ReadOnly]
            public ComponentLookup<CompanyNotifications> m_CompanyNotifications;

            // Token: 0x0400964B RID: 38475
            [ReadOnly]
            public ComponentLookup<Attached> m_Attached;

            // Token: 0x0400964C RID: 38476
            [ReadOnly]
            public ComponentLookup<Game.Areas.Lot> m_Lots;

            // Token: 0x0400964D RID: 38477
            [ReadOnly]
            public ComponentLookup<Geometry> m_Geometries;

            // Token: 0x0400964E RID: 38478
            [ReadOnly]
            public ComponentLookup<Extractor> m_AreaExtractors;

            // Token: 0x0400964F RID: 38479
            [ReadOnly]
            public ComponentLookup<LandValue> m_LandValues;

            // Token: 0x04009650 RID: 38480
            [NativeDisableParallelForRestriction]
            public ComponentLookup<PropertyOnMarket> m_OnMarkets;

            // Token: 0x04009651 RID: 38481
            [ReadOnly]
            public ComponentLookup<ConsumptionData> m_ConsumptionDatas;

            // Token: 0x04009652 RID: 38482
            [ReadOnly]
            public BufferLookup<HouseholdCitizen> m_Citizens;

            // Token: 0x04009653 RID: 38483
            [ReadOnly]
            public BufferLookup<Game.Net.ServiceCoverage> m_ServiceCoverages;

            // Token: 0x04009654 RID: 38484
            [ReadOnly]
            public BufferLookup<ResourceAvailability> m_Availabilities;

            // Token: 0x04009655 RID: 38485
            [ReadOnly]
            public BufferLookup<Game.Areas.SubArea> m_SubAreas;

            // Token: 0x04009656 RID: 38486
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildings;

            // Token: 0x04009657 RID: 38487
            [ReadOnly]
            public ComponentLookup<ResourceData> m_ResourceDatas;

            // Token: 0x04009658 RID: 38488
            [ReadOnly]
            public ComponentLookup<Game.Companies.StorageCompany> m_StorageCompanies;

            // Token: 0x04009659 RID: 38489
            [ReadOnly]
            public ComponentLookup<WorkplaceData> m_WorkplaceDatas;

            // Token: 0x0400965A RID: 38490
            [ReadOnly]
            public ComponentLookup<Abandoned> m_Abandoned;

            // Token: 0x0400965B RID: 38491
            [ReadOnly]
            public ComponentLookup<Destroyed> m_Destroyed;

            // Token: 0x0400965C RID: 38492
            [ReadOnly]
            public ComponentLookup<CrimeProducer> m_Crimes;

            // Token: 0x0400965D RID: 38493
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> m_Transforms;

            // Token: 0x0400965E RID: 38494
            [ReadOnly]
            public ComponentLookup<Locked> m_Locked;

            // Token: 0x0400965F RID: 38495
            [ReadOnly]
            public BufferLookup<CityModifier> m_CityModifiers;

            // Token: 0x04009660 RID: 38496
            [ReadOnly]
            public ComponentLookup<CurrentDistrict> m_Districts;

            // Token: 0x04009661 RID: 38497
            [ReadOnly]
            public BufferLookup<DistrictModifier> m_DistrictModifiers;

            // Token: 0x04009662 RID: 38498
            [ReadOnly]
            public ComponentLookup<HealthProblem> m_HealthProblems;

            // Token: 0x04009663 RID: 38499
            [ReadOnly]
            public BufferLookup<Employee> m_Employees;

            // Token: 0x04009664 RID: 38500
            [ReadOnly]
            public BufferLookup<Efficiency> m_BuildingEfficiencies;

            // Token: 0x04009665 RID: 38501
            [ReadOnly]
            public ComponentLookup<ExtractorAreaData> m_ExtractorDatas;

            // Token: 0x04009666 RID: 38502
            [ReadOnly]
            public ComponentLookup<Citizen> m_CitizenDatas;

            // Token: 0x04009667 RID: 38503
            [ReadOnly]
            public ComponentLookup<Game.Citizens.Student> m_Students;

            // Token: 0x04009668 RID: 38504
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingData;

            // Token: 0x04009669 RID: 38505
            [ReadOnly]
            public ComponentLookup<ZoneData> m_ZoneData;

            // Token: 0x0400966A RID: 38506
            [ReadOnly]
            public BufferLookup<TradeCost> m_TradeCosts;

            // Token: 0x0400966B RID: 38507
            [ReadOnly]
            public ComponentLookup<ElectricityConsumer> m_ElectricityConsumers;

            // Token: 0x0400966C RID: 38508
            [ReadOnly]
            public ComponentLookup<WaterConsumer> m_WaterConsumers;

            // Token: 0x0400966D RID: 38509
            [ReadOnly]
            public ComponentLookup<GarbageProducer> m_GarbageProducers;

            // Token: 0x0400966E RID: 38510
            [ReadOnly]
            public ComponentLookup<MailProducer> m_MailProducers;

            // Token: 0x0400966F RID: 38511
            [ReadOnly]
            public ComponentLookup<Abandoned> m_Abandoneds;

            // Token: 0x04009670 RID: 38512
            [ReadOnly]
            public ComponentLookup<ExtractorProperty> m_ExtractorProperties;

            // Token: 0x04009671 RID: 38513
            [NativeDisableParallelForRestriction]
            public ComponentLookup<BuildingNotifications> m_BuildingNotifications;

            // Token: 0x04009672 RID: 38514
            [ReadOnly]
            public NativeArray<AirPollution> m_AirPollutionMap;

            // Token: 0x04009673 RID: 38515
            [ReadOnly]
            public NativeArray<GroundPollution> m_PollutionMap;

            // Token: 0x04009674 RID: 38516
            [ReadOnly]
            public NativeArray<NoisePollution> m_NoiseMap;

            // Token: 0x04009675 RID: 38517
            [ReadOnly]
            public CellMapData<TelecomCoverage> m_TelecomCoverages;

            // Token: 0x04009676 RID: 38518
            [ReadOnly]
            public NativeArray<int> m_Unemployment;

            // Token: 0x04009677 RID: 38519
            public ExtractorParameterData m_ExtractorParameters;

            // Token: 0x04009678 RID: 38520
            public HealthcareParameterData m_HealthcareParameters;

            // Token: 0x04009679 RID: 38521
            public ParkParameterData m_ParkParameters;

            // Token: 0x0400967A RID: 38522
            public EducationParameterData m_EducationParameters;

            // Token: 0x0400967B RID: 38523
            public TelecomParameterData m_TelecomParameters;

            // Token: 0x0400967C RID: 38524
            public GarbageParameterData m_GarbageParameters;

            // Token: 0x0400967D RID: 38525
            public PoliceConfigurationData m_PoliceParameters;

            // Token: 0x0400967E RID: 38526
            public CitizenHappinessParameterData m_CitizenHappinessParameterData;

            // Token: 0x0400967F RID: 38527
            public BuildingConfigurationData m_BuildingConfigurationData;

            // Token: 0x04009680 RID: 38528
            public PollutionParameterData m_PollutionParameters;

            // Token: 0x04009681 RID: 38529
            public IconCommandBuffer m_IconCommandBuffer;

            // Token: 0x04009682 RID: 38530
            [ReadOnly]
            public NativeArray<int> m_TaxRates;

            // Token: 0x04009683 RID: 38531
            [ReadOnly]
            public ResourcePrefabs m_ResourcePrefabs;

            // Token: 0x04009684 RID: 38532
            public uint m_UpdateFrameIndex;

            // Token: 0x04009685 RID: 38533
            public float m_BaseConsumptionSum;

            // Token: 0x04009686 RID: 38534
            [ReadOnly]
            public Entity m_City;

            // Token: 0x04009687 RID: 38535
            public EconomyParameterData m_EconomyParameters;

            // Token: 0x04009688 RID: 38536
            public DemandParameterData m_DemandParameters;

            // Token: 0x04009689 RID: 38537
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
        }

        // Token: 0x0200146B RID: 5227
        private struct TypeHandle
        {
            // Token: 0x06005C51 RID: 23633 RVA: 0x0036B24C File Offset: 0x0036944C
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
                this.__Game_Buildings_WaterConsumer_RO_ComponentLookup = state.GetComponentLookup<WaterConsumer>(true);
                this.__Game_Buildings_GarbageProducer_RO_ComponentLookup = state.GetComponentLookup<GarbageProducer>(true);
                this.__Game_Buildings_MailProducer_RO_ComponentLookup = state.GetComponentLookup<MailProducer>(true);
                this.__Game_Buildings_ExtractorProperty_RO_ComponentLookup = state.GetComponentLookup<ExtractorProperty>(true);
            }

            // Token: 0x0400968A RID: 38538
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

            // Token: 0x0400968B RID: 38539
            public BufferTypeHandle<Renter> __Game_Buildings_Renter_RW_BufferTypeHandle;

            // Token: 0x0400968C RID: 38540
            public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RW_ComponentLookup;

            // Token: 0x0400968D RID: 38541
            public ComponentLookup<PropertyOnMarket> __Game_Buildings_PropertyOnMarket_RW_ComponentLookup;

            // Token: 0x0400968E RID: 38542
            [ReadOnly]
            public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

            // Token: 0x0400968F RID: 38543
            [ReadOnly]
            public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;

            // Token: 0x04009690 RID: 38544
            public ComponentLookup<Building> __Game_Buildings_Building_RW_ComponentLookup;

            // Token: 0x04009691 RID: 38545
            [ReadOnly]
            public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

            // Token: 0x04009692 RID: 38546
            [ReadOnly]
            public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

            // Token: 0x04009693 RID: 38547
            [ReadOnly]
            public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

            // Token: 0x04009694 RID: 38548
            [ReadOnly]
            public ComponentLookup<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentLookup;

            // Token: 0x04009695 RID: 38549
            [ReadOnly]
            public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

            // Token: 0x04009696 RID: 38550
            [ReadOnly]
            public ComponentLookup<ServiceAvailable> __Game_Companies_ServiceAvailable_RO_ComponentLookup;

            // Token: 0x04009697 RID: 38551
            [ReadOnly]
            public ComponentLookup<ServiceCompanyData> __Game_Companies_ServiceCompanyData_RO_ComponentLookup;

            // Token: 0x04009698 RID: 38552
            [ReadOnly]
            public ComponentLookup<Game.Companies.ProcessingCompany> __Game_Companies_ProcessingCompany_RO_ComponentLookup;

            // Token: 0x04009699 RID: 38553
            [ReadOnly]
            public ComponentLookup<BuyingCompany> __Game_Companies_BuyingCompany_RO_ComponentLookup;

            // Token: 0x0400969A RID: 38554
            [ReadOnly]
            public ComponentLookup<CompanyNotifications> __Game_Companies_CompanyNotifications_RO_ComponentLookup;

            // Token: 0x0400969B RID: 38555
            [ReadOnly]
            public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

            // Token: 0x0400969C RID: 38556
            [ReadOnly]
            public ComponentLookup<Game.Areas.Lot> __Game_Areas_Lot_RO_ComponentLookup;

            // Token: 0x0400969D RID: 38557
            [ReadOnly]
            public ComponentLookup<Geometry> __Game_Areas_Geometry_RO_ComponentLookup;

            // Token: 0x0400969E RID: 38558
            [ReadOnly]
            public ComponentLookup<Extractor> __Game_Areas_Extractor_RO_ComponentLookup;

            // Token: 0x0400969F RID: 38559
            [ReadOnly]
            public ComponentLookup<LandValue> __Game_Net_LandValue_RO_ComponentLookup;

            // Token: 0x040096A0 RID: 38560
            [ReadOnly]
            public ComponentLookup<ConsumptionData> __Game_Prefabs_ConsumptionData_RO_ComponentLookup;

            // Token: 0x040096A1 RID: 38561
            [ReadOnly]
            public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

            // Token: 0x040096A2 RID: 38562
            [ReadOnly]
            public BufferLookup<Game.Net.ServiceCoverage> __Game_Net_ServiceCoverage_RO_BufferLookup;

            // Token: 0x040096A3 RID: 38563
            [ReadOnly]
            public BufferLookup<ResourceAvailability> __Game_Net_ResourceAvailability_RO_BufferLookup;

            // Token: 0x040096A4 RID: 38564
            [ReadOnly]
            public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

            // Token: 0x040096A5 RID: 38565
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

            // Token: 0x040096A6 RID: 38566
            [ReadOnly]
            public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

            // Token: 0x040096A7 RID: 38567
            [ReadOnly]
            public ComponentLookup<Game.Companies.StorageCompany> __Game_Companies_StorageCompany_RO_ComponentLookup;

            // Token: 0x040096A8 RID: 38568
            [ReadOnly]
            public ComponentLookup<WorkplaceData> __Game_Prefabs_WorkplaceData_RO_ComponentLookup;

            // Token: 0x040096A9 RID: 38569
            [ReadOnly]
            public ComponentLookup<Abandoned> __Game_Buildings_Abandoned_RO_ComponentLookup;

            // Token: 0x040096AA RID: 38570
            [ReadOnly]
            public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

            // Token: 0x040096AB RID: 38571
            [ReadOnly]
            public ComponentLookup<CrimeProducer> __Game_Buildings_CrimeProducer_RO_ComponentLookup;

            // Token: 0x040096AC RID: 38572
            [ReadOnly]
            public ComponentLookup<Locked> __Game_Prefabs_Locked_RO_ComponentLookup;

            // Token: 0x040096AD RID: 38573
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

            // Token: 0x040096AE RID: 38574
            [ReadOnly]
            public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

            // Token: 0x040096AF RID: 38575
            [ReadOnly]
            public ComponentLookup<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentLookup;

            // Token: 0x040096B0 RID: 38576
            [ReadOnly]
            public BufferLookup<DistrictModifier> __Game_Areas_DistrictModifier_RO_BufferLookup;

            // Token: 0x040096B1 RID: 38577
            [ReadOnly]
            public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;

            // Token: 0x040096B2 RID: 38578
            [ReadOnly]
            public BufferLookup<Employee> __Game_Companies_Employee_RO_BufferLookup;

            // Token: 0x040096B3 RID: 38579
            [ReadOnly]
            public BufferLookup<Efficiency> __Game_Buildings_Efficiency_RO_BufferLookup;

            // Token: 0x040096B4 RID: 38580
            [ReadOnly]
            public ComponentLookup<ExtractorAreaData> __Game_Prefabs_ExtractorAreaData_RO_ComponentLookup;

            // Token: 0x040096B5 RID: 38581
            [ReadOnly]
            public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

            // Token: 0x040096B6 RID: 38582
            [ReadOnly]
            public ComponentLookup<Game.Citizens.Student> __Game_Citizens_Student_RO_ComponentLookup;

            // Token: 0x040096B7 RID: 38583
            [ReadOnly]
            public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;

            // Token: 0x040096B8 RID: 38584
            [ReadOnly]
            public BufferLookup<TradeCost> __Game_Companies_TradeCost_RO_BufferLookup;

            // Token: 0x040096B9 RID: 38585
            public ComponentLookup<BuildingNotifications> __Game_Buildings_BuildingNotifications_RW_ComponentLookup;

            // Token: 0x040096BA RID: 38586
            [ReadOnly]
            public ComponentLookup<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RO_ComponentLookup;

            // Token: 0x040096BB RID: 38587
            [ReadOnly]
            public ComponentLookup<WaterConsumer> __Game_Buildings_WaterConsumer_RO_ComponentLookup;

            // Token: 0x040096BC RID: 38588
            [ReadOnly]
            public ComponentLookup<GarbageProducer> __Game_Buildings_GarbageProducer_RO_ComponentLookup;

            // Token: 0x040096BD RID: 38589
            [ReadOnly]
            public ComponentLookup<MailProducer> __Game_Buildings_MailProducer_RO_ComponentLookup;

            // Token: 0x040096BE RID: 38590
            [ReadOnly]
            public ComponentLookup<ExtractorProperty> __Game_Buildings_ExtractorProperty_RO_ComponentLookup;
        }
    }
}
