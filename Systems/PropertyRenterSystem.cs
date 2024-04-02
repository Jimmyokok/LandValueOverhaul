using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Notifications;
using Game.Objects;
using Game.Prefabs;
using Game.Serialization;
using Game.Tools;
using Game.Triggers;
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
using Version = Game.Version;

namespace LandValueOverhaul.Systems
{
    // Token: 0x02001460 RID: 5216
    public partial class PropertyRenterSystem : GameSystemBase, IDefaultSerializable, ISerializable, IPostDeserialize
    {
        // Token: 0x06005C25 RID: 23589 RVA: 0x00366B3E File Offset: 0x00364D3E
        public static float GetUpkeepExponent(AreaType type)
        {
            if (type == AreaType.Residential)
            {
                return PropertyRenterSystem.kResidentialUpkeepExponent;
            }
            if (type == AreaType.Industrial)
            {
                return PropertyRenterSystem.kIndustrialUpkeepExponent;
            }
            if (type == AreaType.None)
            {
                return 1f;
            }
            return PropertyRenterSystem.kCOUpkeepExponent;
        }


        private static int GetLevelingCost(AreaType areaType, BuildingPropertyData propertyData, int currentlevel, DynamicBuffer<CityModifier> cityEffects)
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
                num2 = (float)((currentlevel <= 4) ? (num * Mathf.RoundToInt(math.pow(2f, 1f + ((float)(currentlevel) - 1) * upgrade_cost_factor) * 80f)) : 1073741823);
            }
            CityUtils.ApplyModifier(ref num2, cityEffects, CityModifierType.BuildingLevelingCost);
            return Mathf.RoundToInt(num2);
        }

        // Token: 0x06005C26 RID: 23590 RVA: 0x00366B62 File Offset: 0x00364D62
        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 262144 / (PropertyRenterSystem.kUpdatesPerDay * 16);
        }

        public static int GetUpkeep(int level, int residential_properties, float baseUpkeep, int lotSize, AreaType areaType, bool isStorage = false)
        {
            int upkeepThreshold = math.max(lotSize, residential_properties) * BuildingUpkeepSystem.kUpdatesPerDay;
            if (areaType == AreaType.Residential)
            {
                return math.max(upkeepThreshold, Mathf.RoundToInt(math.sqrt((float)level + 3f) * baseUpkeep * (float)lotSize * 0.5f));
            }
            return math.max(upkeepThreshold, Mathf.RoundToInt((float)level * baseUpkeep * (float)lotSize * (isStorage ? 0.5f : 1f)));
        }

        // Token: 0x17000A0F RID: 2575
        // (get) Token: 0x06005C28 RID: 23592 RVA: 0x00366BC4 File Offset: 0x00364DC4
        public Entity Landlords
        {
            get
            {
                return this.m_LandlordEntity;
            }
        }

        // Token: 0x06005C29 RID: 23593 RVA: 0x00366BCC File Offset: 0x00364DCC
        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(this.m_LandlordEntity);
        }

        // Token: 0x06005C2A RID: 23594 RVA: 0x00366BE4 File Offset: 0x00364DE4
        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            if (reader.context.version >= Version.taxRateArrayLength)
            {
                reader.Read(out this.m_LandlordEntity);
            }
        }

        // Token: 0x06005C2B RID: 23595 RVA: 0x00003211 File Offset: 0x00001411
        public void SetDefaults(Context context)
        {
        }

        // Token: 0x06005C2C RID: 23596 RVA: 0x00366C25 File Offset: 0x00364E25
        public void PostDeserialize(Context context)
        {
            if (context.purpose == Colossal.Serialization.Entities.Purpose.NewGame)
            {
                this.CreateLandlordEntity();
            }
        }

        // Token: 0x06005C2D RID: 23597 RVA: 0x00366C38 File Offset: 0x00364E38
        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            Mod.log.Info("Modded PropertyRenterSystem created!");
            this.m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
            this.m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
            this.m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
            this.m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
            this.m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
            this.m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
            this.m_ZoneBuiltRequirementSystemSystem = base.World.GetOrCreateSystemManaged<ZoneBuiltRequirementSystem>();
            this.m_ZoneSearchSystem = base.World.GetOrCreateSystemManaged<Game.Zones.SearchSystem>();
            this.m_ElectricityRoadConnectionGraphSystem = base.World.GetOrCreateSystemManaged<ElectricityRoadConnectionGraphSystem>();
            this.m_WaterPipeRoadConnectionGraphSystem = base.World.GetOrCreateSystemManaged<WaterPipeRoadConnectionGraphSystem>();
            this.m_LevelupQueue = new NativeQueue<Entity>(Allocator.Persistent);
            this.m_LeveldownQueue = new NativeQueue<Entity>(Allocator.Persistent);
            this.m_PaymentQueue = new NativeQueue<int>(Allocator.Persistent);
            this.m_EconomyParameterQuery = base.GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<EconomyParameterData>()
            });
            this.m_BuildingSettingsQuery = base.GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<BuildingConfigurationData>()
            });
            this.m_BuildingGroup = base.GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Building>(),
                        ComponentType.ReadOnly<Renter>(),
                        ComponentType.ReadOnly<UpdateFrame>()
                    },
                    Any = new ComponentType[]
                    {
                        ComponentType.ReadWrite<BuildingCondition>()
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>()
                    }
                }
            });
            this.m_HouseholdGroup = base.GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<Household>(),
                ComponentType.ReadOnly<UpdateFrame>()
            });
            this.m_MovingAwayHouseholdGroup = base.GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<Household>(),
                ComponentType.ReadOnly<MovingAway>(),
                ComponentType.ReadOnly<PropertyRenter>()
            });
            this.m_BuildingPrefabGroup = base.GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<BuildingData>(),
                ComponentType.ReadOnly<BuildingSpawnGroupData>(),
                ComponentType.ReadOnly<PrefabData>()
            });
            this.m_LandlordQuery = base.GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadWrite<Landlord>()
            });
            base.RequireForUpdate(this.m_BuildingSettingsQuery);
            base.RequireForUpdate(this.m_BuildingGroup);
        }

        // Token: 0x06005C2E RID: 23598 RVA: 0x00366EB8 File Offset: 0x003650B8
        [Preserve]
        protected override void OnDestroy()
        {
            this.m_PaymentQueue.Dispose();
            this.m_LevelupQueue.Dispose();
            this.m_LeveldownQueue.Dispose();
            base.OnDestroy();
        }

        // Token: 0x06005C2F RID: 23599 RVA: 0x00366EE4 File Offset: 0x003650E4
        private void CreateLandlordEntity()
        {
            int num = this.m_LandlordQuery.CalculateEntityCount();
            if (num > 1)
            {
                base.EntityManager.DestroyEntity(this.m_LandlordQuery);
            }
            if (num > 1 || num == 0)
            {
                this.m_LandlordEntity = base.World.EntityManager.CreateEntity(base.World.EntityManager.CreateArchetype(new ComponentType[]
                {
                    ComponentType.ReadOnly<Landlord>(),
                    ComponentType.ReadWrite<Game.Economy.Resources>()
                }));
                return;
            }
            this.m_LandlordEntity = this.m_LandlordQuery.GetSingletonEntity();
            base.EntityManager.GetBuffer<Game.Economy.Resources>(this.m_LandlordEntity, false).Clear();
        }

        // Token: 0x06005C30 RID: 23600 RVA: 0x00366F98 File Offset: 0x00365198
        [Preserve]
        protected override void OnUpdate()
        {
            if (this.m_LandlordQuery.IsEmptyIgnoreFilter)
            {
                this.CreateLandlordEntity();
            }
            else
            {
                this.m_LandlordEntity = this.m_LandlordQuery.GetSingletonEntity();
            }
            uint updateFrame = SimulationUtils.GetUpdateFrame(this.m_SimulationSystem.frameIndex, PropertyRenterSystem.kUpdatesPerDay, 16);
            uint updateFrame2 = SimulationUtils.GetUpdateFrame(this.m_SimulationSystem.frameIndex, PropertyRenterSystem.kUpdatesPerDay, 16);
            BuildingConfigurationData singleton = this.m_BuildingSettingsQuery.GetSingleton<BuildingConfigurationData>();
            this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
            PropertyRenterSystem.RenterMovingAwayJob jobData = default(PropertyRenterSystem.RenterMovingAwayJob);
            jobData.m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle;
            jobData.m_PropertyRenters = this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup;
            jobData.m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter();
            JobHandle jobHandle = jobData.Schedule(this.m_MovingAwayHouseholdGroup, base.Dependency);
            this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
            this.__TypeHandle.__Game_Prefabs_SignatureBuildingData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_City_CityModifier_RO_BufferLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Companies_StorageCompany_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_PropertyOnMarket_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Economy_Resources_RW_BufferLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Companies_Profitability_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_BuildingCondition_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Renter_RW_BufferTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
            PropertyRenterSystem.PayRentJob jobData2 = default(PropertyRenterSystem.PayRentJob);
            jobData2.m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle;
            jobData2.m_RenterType = this.__TypeHandle.__Game_Buildings_Renter_RW_BufferTypeHandle;
            jobData2.m_ConditionType = this.__TypeHandle.__Game_Buildings_BuildingCondition_RW_ComponentTypeHandle;
            jobData2.m_PrefabType = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
            jobData2.m_UpdateFrameType = base.GetSharedComponentTypeHandle<UpdateFrame>();
            jobData2.m_SpawnableBuildingData = this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;
            jobData2.m_Profitabilities = this.__TypeHandle.__Game_Companies_Profitability_RO_ComponentLookup;
            jobData2.m_ZoneData = this.__TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup;
            jobData2.m_PropertyRenters = this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup;
            jobData2.m_Resources = this.__TypeHandle.__Game_Economy_Resources_RW_BufferLookup;
            jobData2.m_BuildingProperties = this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;
            jobData2.m_PropertiesOnMarket = this.__TypeHandle.__Game_Buildings_PropertyOnMarket_RO_ComponentLookup;
            jobData2.m_Abandoned = this.__TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup;
            jobData2.m_Destroyed = this.__TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup;
            jobData2.m_Storages = this.__TypeHandle.__Game_Companies_StorageCompany_RO_ComponentLookup;
            jobData2.m_CityEffects = this.__TypeHandle.__Game_City_CityModifier_RO_BufferLookup;
            jobData2.m_SignatureDatas = this.__TypeHandle.__Game_Prefabs_SignatureBuildingData_RO_ComponentLookup;
            jobData2.m_City = this.m_CitySystem.City;
            jobData2.m_RandomSeed = RandomSeed.Next();
            jobData2.m_UpdateFrameIndex = updateFrame;
            jobData2.m_LevelupQueue = this.m_LevelupQueue.AsParallelWriter();
            jobData2.m_LevelDownQueue = this.m_LeveldownQueue.AsParallelWriter();
            jobData2.m_DebugFastLeveling = this.debugFastLeveling;
            jobData2.m_LandlordQueue = this.m_PaymentQueue.AsParallelWriter();
            jobData2.m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter();
            jobHandle = jobData2.ScheduleParallel(this.m_BuildingGroup, jobHandle);
            this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
            this.__TypeHandle.__Game_City_CityStatistic_RO_BufferLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Economy_Resources_RW_BufferLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
            PropertyRenterSystem.ReturnRentJob jobData3 = default(PropertyRenterSystem.ReturnRentJob);
            jobData3.m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle;
            jobData3.m_UpdateFrameType = base.GetSharedComponentTypeHandle<UpdateFrame>();
            jobData3.m_HouseholdCitizenType = this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle;
            jobData3.m_Resources = this.__TypeHandle.__Game_Economy_Resources_RW_BufferLookup;
            jobData3.m_EconomyParameters = this.m_EconomyParameterQuery.GetSingleton<EconomyParameterData>();
            jobData3.m_Citizens = this.__TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup;
            jobData3.m_Statistics = this.__TypeHandle.__Game_City_CityStatistic_RO_BufferLookup;
            jobData3.m_StatisticsLookup = this.m_CityStatisticsSystem.GetLookup();
            jobData3.m_LandlordEntity = this.m_LandlordEntity;
            jobData3.m_UpdateFrameIndex = updateFrame2;
            jobData3.m_PaymentQueue = this.m_PaymentQueue.AsParallelWriter();
            jobHandle = jobData3.ScheduleParallel(this.m_HouseholdGroup, jobHandle);
            this.__TypeHandle.__Game_Economy_Resources_RW_BufferLookup.Update(ref base.CheckedStateRef);
            PropertyRenterSystem.LandlordMoneyJob jobData4 = default(PropertyRenterSystem.LandlordMoneyJob);
            jobData4.m_Resources = this.__TypeHandle.__Game_Economy_Resources_RW_BufferLookup;
            jobData4.m_LandlordEntity = this.m_LandlordEntity;
            jobData4.m_PaymentQueue = this.m_PaymentQueue;
            jobHandle = jobData4.Schedule(jobHandle);
            this.__TypeHandle.__Game_Zones_Cell_RO_BufferLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_OfficeBuilding_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Zones_ValidArea_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Zones_Block_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingSpawnGroupData_SharedComponentTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
            PropertyRenterSystem.LevelupJob jobData5 = default(PropertyRenterSystem.LevelupJob);
            jobData5.m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle;
            jobData5.m_SpawnableBuildingType = this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle;
            jobData5.m_BuildingType = this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentTypeHandle;
            jobData5.m_BuildingPropertyType = this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle;
            jobData5.m_ObjectGeometryType = this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentTypeHandle;
            jobData5.m_BuildingSpawnGroupType = this.__TypeHandle.__Game_Prefabs_BuildingSpawnGroupData_SharedComponentTypeHandle;
            jobData5.m_TransformData = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup;
            jobData5.m_BlockData = this.__TypeHandle.__Game_Zones_Block_RO_ComponentLookup;
            jobData5.m_ValidAreaData = this.__TypeHandle.__Game_Zones_ValidArea_RO_ComponentLookup;
            jobData5.m_Prefabs = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
            jobData5.m_SpawnableBuildings = this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;
            jobData5.m_Buildings = this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup;
            jobData5.m_BuildingPropertyDatas = this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;
            jobData5.m_OfficeBuilding = this.__TypeHandle.__Game_Prefabs_OfficeBuilding_RO_ComponentLookup;
            jobData5.m_ZoneData = this.__TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup;
            jobData5.m_Cells = this.__TypeHandle.__Game_Zones_Cell_RO_BufferLookup;
            jobData5.m_BuildingConfigurationData = singleton;
            JobHandle job;
            jobData5.m_SpawnableBuildingChunks = this.m_BuildingPrefabGroup.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out job);
            JobHandle job2;
            jobData5.m_ZoneSearchTree = this.m_ZoneSearchSystem.GetSearchTree(true, out job2);
            jobData5.m_RandomSeed = RandomSeed.Next();
            jobData5.m_IconCommandBuffer = this.m_IconCommandSystem.CreateCommandBuffer();
            jobData5.m_LevelupQueue = this.m_LevelupQueue;
            jobData5.m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer();
            jobData5.m_TriggerBuffer = this.m_TriggerSystem.CreateActionBuffer();
            JobHandle job3;
            jobData5.m_ZoneBuiltLevelQueue = this.m_ZoneBuiltRequirementSystemSystem.GetZoneBuiltLevelQueue(out job3);
            jobHandle = jobData5.Schedule(JobUtils.CombineDependencies(jobHandle, job, job2, job3));
            this.m_ZoneSearchSystem.AddSearchTreeReader(jobHandle);
            this.m_ZoneBuiltRequirementSystemSystem.AddWriter(jobHandle);
            this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
            this.m_TriggerSystem.AddActionBufferWriter(jobHandle);
            this.__TypeHandle.__Game_Buildings_Renter_RW_BufferLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_CrimeProducer_RW_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_OfficeBuilding_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_MailProducer_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_GroundPolluter_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_GarbageProducer_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Building_RW_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            PropertyRenterSystem.LeveldownJob jobData6 = default(PropertyRenterSystem.LeveldownJob);
            jobData6.m_BuildingDatas = this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup;
            jobData6.m_Prefabs = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
            jobData6.m_SpawnableBuildings = this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;
            jobData6.m_Buildings = this.__TypeHandle.__Game_Buildings_Building_RW_ComponentLookup;
            jobData6.m_ElectricityConsumers = this.__TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup;
            jobData6.m_GarbageProducers = this.__TypeHandle.__Game_Buildings_GarbageProducer_RO_ComponentLookup;
            jobData6.m_GroundPolluters = this.__TypeHandle.__Game_Buildings_GroundPolluter_RO_ComponentLookup;
            jobData6.m_MailProducers = this.__TypeHandle.__Game_Buildings_MailProducer_RO_ComponentLookup;
            jobData6.m_WaterConsumers = this.__TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup;
            jobData6.m_BuildingPropertyDatas = this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;
            jobData6.m_OfficeBuilding = this.__TypeHandle.__Game_Prefabs_OfficeBuilding_RO_ComponentLookup;
            jobData6.m_TriggerBuffer = this.m_TriggerSystem.CreateActionBuffer();
            jobData6.m_CrimeProducers = this.__TypeHandle.__Game_Buildings_CrimeProducer_RW_ComponentLookup;
            jobData6.m_Renters = this.__TypeHandle.__Game_Buildings_Renter_RW_BufferLookup;
            jobData6.m_BuildingConfigurationData = singleton;
            jobData6.m_LeveldownQueue = this.m_LeveldownQueue;
            jobData6.m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer();
            JobHandle job4;
            jobData6.m_UpdatedElectricityRoadEdges = this.m_ElectricityRoadConnectionGraphSystem.GetEdgeUpdateQueue(out job4);
            JobHandle job5;
            jobData6.m_UpdatedWaterPipeRoadEdges = this.m_WaterPipeRoadConnectionGraphSystem.GetEdgeUpdateQueue(out job5);
            jobData6.m_IconCommandBuffer = this.m_IconCommandSystem.CreateCommandBuffer();
            jobData6.m_SimulationFrame = this.m_SimulationSystem.frameIndex;
            jobHandle = jobData6.Schedule(JobHandle.CombineDependencies(jobHandle, job4, job5));
            this.m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
            this.m_ElectricityRoadConnectionGraphSystem.AddQueueWriter(jobHandle);
            this.m_IconCommandSystem.AddCommandBufferWriter(jobHandle);
            this.m_TriggerSystem.AddActionBufferWriter(jobHandle);
            base.Dependency = jobHandle;
        }

        // Token: 0x06005C31 RID: 23601 RVA: 0x00367BAC File Offset: 0x00365DAC
        public void DebugLevelUp(Entity building, ComponentLookup<BuildingCondition> conditions, ComponentLookup<SpawnableBuildingData> spawnables, ComponentLookup<PrefabRef> prefabRefs, ComponentLookup<ZoneData> zoneDatas, ComponentLookup<BuildingPropertyData> propertyDatas)
        {
            if (conditions.HasComponent(building) && prefabRefs.HasComponent(building))
            {
                BuildingCondition buildingCondition = conditions[building];
                Entity prefab = prefabRefs[building].m_Prefab;
                if (spawnables.HasComponent(prefab) && propertyDatas.HasComponent(prefab))
                {
                    SpawnableBuildingData spawnableBuildingData = spawnables[prefab];
                    if (zoneDatas.HasComponent(spawnableBuildingData.m_ZonePrefab))
                    {
                        ZoneData zoneData = zoneDatas[spawnableBuildingData.m_ZonePrefab];
                        this.m_LevelupQueue.Enqueue(building);
                    }
                }
            }
        }

        // Token: 0x06005C32 RID: 23602 RVA: 0x00367C2C File Offset: 0x00365E2C
        public void DebugLevelDown(Entity building, ComponentLookup<BuildingCondition> conditions, ComponentLookup<SpawnableBuildingData> spawnables, ComponentLookup<PrefabRef> prefabRefs, ComponentLookup<ZoneData> zoneDatas, ComponentLookup<BuildingPropertyData> propertyDatas)
        {
            if (conditions.HasComponent(building) && prefabRefs.HasComponent(building))
            {
                BuildingCondition value = conditions[building];
                Entity prefab = prefabRefs[building].m_Prefab;
                if (spawnables.HasComponent(prefab) && propertyDatas.HasComponent(prefab))
                {
                    SpawnableBuildingData spawnableBuildingData = spawnables[prefab];
                    if (zoneDatas.HasComponent(spawnableBuildingData.m_ZonePrefab))
                    {
                        int levelingCost = GetLevelingCost(zoneDatas[spawnableBuildingData.m_ZonePrefab].m_AreaType, propertyDatas[prefab], (int)spawnableBuildingData.m_Level, base.EntityManager.GetBuffer<CityModifier>(this.m_CitySystem.City, true));
                        value.m_Condition = -3 * levelingCost / 2;
                        conditions[building] = value;
                        this.m_LeveldownQueue.Enqueue(building);
                    }
                }
            }
        }

        // Token: 0x06005C33 RID: 23603 RVA: 0x00003211 File Offset: 0x00001411
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
        }

        // Token: 0x06005C34 RID: 23604 RVA: 0x00367CFC File Offset: 0x00365EFC
        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            this.__AssignQueries(ref base.CheckedStateRef);
            this.__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
        }

        // Token: 0x06005C35 RID: 23605 RVA: 0x00006D67 File Offset: 0x00004F67
        [Preserve]
        public PropertyRenterSystem()
        {
        }

        // Token: 0x0400957B RID: 38267
        public static readonly int kUpdatesPerDay = 16;

        public static readonly float upgrade_cost_factor = math.log2(20f) / 3;

        // Token: 0x0400957C RID: 38268
        private static readonly float kResidentialUpkeepExponent = 1.3f;

        // Token: 0x0400957D RID: 38269
        private static readonly float kCOUpkeepExponent = 2.1f;

        // Token: 0x0400957E RID: 38270
        private static readonly float kIndustrialUpkeepExponent = 2f;

        // Token: 0x0400957F RID: 38271
        private SimulationSystem m_SimulationSystem;

        // Token: 0x04009580 RID: 38272
        private EndFrameBarrier m_EndFrameBarrier;

        // Token: 0x04009581 RID: 38273
        private CityStatisticsSystem m_CityStatisticsSystem;

        // Token: 0x04009582 RID: 38274
        private CitySystem m_CitySystem;

        // Token: 0x04009583 RID: 38275
        private IconCommandSystem m_IconCommandSystem;

        // Token: 0x04009584 RID: 38276
        private TriggerSystem m_TriggerSystem;

        // Token: 0x04009585 RID: 38277
        private ZoneBuiltRequirementSystem m_ZoneBuiltRequirementSystemSystem;

        // Token: 0x04009586 RID: 38278
        private Game.Zones.SearchSystem m_ZoneSearchSystem;

        // Token: 0x04009587 RID: 38279
        private ElectricityRoadConnectionGraphSystem m_ElectricityRoadConnectionGraphSystem;

        // Token: 0x04009588 RID: 38280
        private WaterPipeRoadConnectionGraphSystem m_WaterPipeRoadConnectionGraphSystem;

        // Token: 0x04009589 RID: 38281
        private EntityQuery m_EconomyParameterQuery;

        // Token: 0x0400958A RID: 38282
        private EntityQuery m_BuildingSettingsQuery;

        // Token: 0x0400958B RID: 38283
        private EntityQuery m_BuildingGroup;

        // Token: 0x0400958C RID: 38284
        private EntityQuery m_BuildingPrefabGroup;

        // Token: 0x0400958D RID: 38285
        private EntityQuery m_HouseholdGroup;

        // Token: 0x0400958E RID: 38286
        private EntityQuery m_MovingAwayHouseholdGroup;

        // Token: 0x0400958F RID: 38287
        private EntityQuery m_LandlordQuery;

        // Token: 0x04009590 RID: 38288
        private NativeQueue<Entity> m_LevelupQueue;

        // Token: 0x04009591 RID: 38289
        private NativeQueue<Entity> m_LeveldownQueue;

        // Token: 0x04009592 RID: 38290
        private NativeQueue<int> m_PaymentQueue;

        // Token: 0x04009593 RID: 38291
        private Entity m_LandlordEntity;

        // Token: 0x04009594 RID: 38292
        public bool debugFastLeveling;

        // Token: 0x04009595 RID: 38293
        private PropertyRenterSystem.TypeHandle __TypeHandle;

        // Token: 0x02001461 RID: 5217
        private struct LeveldownJob : IJob
        {
            // Token: 0x06005C37 RID: 23607 RVA: 0x00367D48 File Offset: 0x00365F48
            public void Execute()
            {
                Entity entity;
                while (this.m_LeveldownQueue.TryDequeue(out entity))
                {
                    if (this.m_Prefabs.HasComponent(entity))
                    {
                        Entity prefab = this.m_Prefabs[entity].m_Prefab;
                        if (this.m_SpawnableBuildings.HasComponent(prefab))
                        {
                            SpawnableBuildingData spawnableBuildingData = this.m_SpawnableBuildings[prefab];
                            BuildingData buildingData = this.m_BuildingDatas[prefab];
                            BuildingPropertyData buildingPropertyData = this.m_BuildingPropertyDatas[prefab];
                            this.m_CommandBuffer.AddComponent<Abandoned>(entity, new Abandoned
                            {
                                m_AbandonmentTime = this.m_SimulationFrame
                            });
                            this.m_CommandBuffer.AddComponent<Updated>(entity, default(Updated));
                            if (this.m_ElectricityConsumers.HasComponent(entity))
                            {
                                this.m_CommandBuffer.RemoveComponent<ElectricityConsumer>(entity);
                                Entity roadEdge = this.m_Buildings[entity].m_RoadEdge;
                                if (roadEdge != Entity.Null)
                                {
                                    this.m_UpdatedElectricityRoadEdges.Enqueue(roadEdge);
                                }
                            }
                            if (this.m_WaterConsumers.HasComponent(entity))
                            {
                                this.m_CommandBuffer.RemoveComponent<WaterConsumer>(entity);
                                Entity roadEdge2 = this.m_Buildings[entity].m_RoadEdge;
                                if (roadEdge2 != Entity.Null)
                                {
                                    this.m_UpdatedWaterPipeRoadEdges.Enqueue(roadEdge2);
                                }
                            }
                            if (this.m_GarbageProducers.HasComponent(entity))
                            {
                                this.m_CommandBuffer.RemoveComponent<GarbageProducer>(entity);
                            }
                            if (this.m_GroundPolluters.HasComponent(entity))
                            {
                                this.m_CommandBuffer.RemoveComponent<GroundPolluter>(entity);
                            }
                            if (this.m_MailProducers.HasComponent(entity))
                            {
                                this.m_CommandBuffer.RemoveComponent<MailProducer>(entity);
                            }
                            if (this.m_CrimeProducers.HasComponent(entity))
                            {
                                CrimeProducer crimeProducer = this.m_CrimeProducers[entity];
                                this.m_CommandBuffer.SetComponent<CrimeProducer>(entity, new CrimeProducer
                                {
                                    m_Crime = crimeProducer.m_Crime * 2f,
                                    m_PatrolRequest = crimeProducer.m_PatrolRequest
                                });
                            }
                            if (this.m_Renters.HasBuffer(entity))
                            {
                                DynamicBuffer<Renter> dynamicBuffer = this.m_Renters[entity];
                                for (int i = dynamicBuffer.Length - 1; i >= 0; i--)
                                {
                                    this.m_CommandBuffer.RemoveComponent<PropertyRenter>(dynamicBuffer[i].m_Renter);
                                    dynamicBuffer.RemoveAt(i);
                                }
                            }
                            if ((this.m_Buildings[entity].m_Flags & Game.Buildings.BuildingFlags.HighRentWarning) != Game.Buildings.BuildingFlags.None)
                            {
                                Building value = this.m_Buildings[entity];
                                this.m_IconCommandBuffer.Remove(entity, this.m_BuildingConfigurationData.m_HighRentNotification, default(Entity), (IconFlags)0);
                                value.m_Flags &= ~Game.Buildings.BuildingFlags.HighRentWarning;
                                this.m_Buildings[entity] = value;
                            }
                            this.m_IconCommandBuffer.Remove(entity, IconPriority.Problem);
                            this.m_IconCommandBuffer.Remove(entity, IconPriority.FatalProblem);
                            this.m_IconCommandBuffer.Add(entity, this.m_BuildingConfigurationData.m_AbandonedNotification, IconPriority.FatalProblem, IconClusterLayer.Default, (IconFlags)0, default(Entity), false, false, false, 0f);
                            if (buildingPropertyData.CountProperties(AreaType.Commercial) > 0)
                            {
                                this.m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.LevelDownCommercialBuilding, Entity.Null, entity, entity, 0f));
                            }
                            if (buildingPropertyData.CountProperties(AreaType.Industrial) > 0)
                            {
                                if (this.m_OfficeBuilding.HasComponent(prefab))
                                {
                                    this.m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.LevelDownOfficeBuilding, Entity.Null, entity, entity, 0f));
                                }
                                else
                                {
                                    this.m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.LevelDownIndustrialBuilding, Entity.Null, entity, entity, 0f));
                                }
                            }
                        }
                    }
                }
            }

            // Token: 0x04009596 RID: 38294
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_Prefabs;

            // Token: 0x04009597 RID: 38295
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildings;

            // Token: 0x04009598 RID: 38296
            [ReadOnly]
            public ComponentLookup<BuildingData> m_BuildingDatas;

            // Token: 0x04009599 RID: 38297
            public ComponentLookup<Building> m_Buildings;

            // Token: 0x0400959A RID: 38298
            [ReadOnly]
            public ComponentLookup<ElectricityConsumer> m_ElectricityConsumers;

            // Token: 0x0400959B RID: 38299
            [ReadOnly]
            public ComponentLookup<WaterConsumer> m_WaterConsumers;

            // Token: 0x0400959C RID: 38300
            [ReadOnly]
            public ComponentLookup<GarbageProducer> m_GarbageProducers;

            // Token: 0x0400959D RID: 38301
            [ReadOnly]
            public ComponentLookup<GroundPolluter> m_GroundPolluters;

            // Token: 0x0400959E RID: 38302
            [ReadOnly]
            public ComponentLookup<MailProducer> m_MailProducers;

            // Token: 0x0400959F RID: 38303
            [ReadOnly]
            public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;

            // Token: 0x040095A0 RID: 38304
            [ReadOnly]
            public ComponentLookup<OfficeBuilding> m_OfficeBuilding;

            // Token: 0x040095A1 RID: 38305
            public NativeQueue<TriggerAction> m_TriggerBuffer;

            // Token: 0x040095A2 RID: 38306
            public ComponentLookup<CrimeProducer> m_CrimeProducers;

            // Token: 0x040095A3 RID: 38307
            public BufferLookup<Renter> m_Renters;

            // Token: 0x040095A4 RID: 38308
            [ReadOnly]
            public BuildingConfigurationData m_BuildingConfigurationData;

            // Token: 0x040095A5 RID: 38309
            public NativeQueue<Entity> m_LeveldownQueue;

            // Token: 0x040095A6 RID: 38310
            public EntityCommandBuffer m_CommandBuffer;

            // Token: 0x040095A7 RID: 38311
            public NativeQueue<Entity> m_UpdatedElectricityRoadEdges;

            // Token: 0x040095A8 RID: 38312
            public NativeQueue<Entity> m_UpdatedWaterPipeRoadEdges;

            // Token: 0x040095A9 RID: 38313
            public IconCommandBuffer m_IconCommandBuffer;

            // Token: 0x040095AA RID: 38314
            public uint m_SimulationFrame;
        }

        // Token: 0x02001462 RID: 5218
        private struct LevelupJob : IJob
        {
            // Token: 0x06005C38 RID: 23608 RVA: 0x003680B8 File Offset: 0x003662B8
            public void Execute()
            {
                Unity.Mathematics.Random random = this.m_RandomSeed.GetRandom(0);
                Entity entity;
                while (this.m_LevelupQueue.TryDequeue(out entity))
                {
                    Entity prefab = this.m_Prefabs[entity].m_Prefab;
                    if (this.m_SpawnableBuildings.HasComponent(prefab))
                    {
                        SpawnableBuildingData spawnableBuildingData = this.m_SpawnableBuildings[prefab];
                        BuildingData buildingData = this.m_Buildings[prefab];
                        BuildingPropertyData buildingPropertyData = this.m_BuildingPropertyDatas[prefab];
                        ZoneData zoneData = this.m_ZoneData[spawnableBuildingData.m_ZonePrefab];
                        float maxHeight = this.GetMaxHeight(entity, buildingData);
                        Entity entity2 = this.SelectSpawnableBuilding(zoneData.m_ZoneType, (int)(spawnableBuildingData.m_Level + 1), buildingData.m_LotSize, maxHeight, buildingData.m_Flags & (Game.Prefabs.BuildingFlags.LeftAccess | Game.Prefabs.BuildingFlags.RightAccess), buildingPropertyData, ref random);
                        if (entity2 != Entity.Null)
                        {
                            this.m_CommandBuffer.AddComponent<UnderConstruction>(entity, new UnderConstruction
                            {
                                m_NewPrefab = entity2,
                                m_Progress = byte.MaxValue
                            });
                            if (buildingPropertyData.CountProperties(AreaType.Residential) > 0)
                            {
                                this.m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.LevelUpResidentialBuilding, Entity.Null, entity, entity, 0f));
                            }
                            if (buildingPropertyData.CountProperties(AreaType.Commercial) > 0)
                            {
                                this.m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.LevelUpCommercialBuilding, Entity.Null, entity, entity, 0f));
                            }
                            if (buildingPropertyData.CountProperties(AreaType.Industrial) > 0)
                            {
                                if (this.m_OfficeBuilding.HasComponent(prefab))
                                {
                                    this.m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.LevelUpOfficeBuilding, Entity.Null, entity, entity, 0f));
                                }
                                else
                                {
                                    this.m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.LevelUpIndustrialBuilding, Entity.Null, entity, entity, 0f));
                                }
                            }
                            this.m_ZoneBuiltLevelQueue.Enqueue(new ZoneBuiltLevelUpdate
                            {
                                m_Zone = spawnableBuildingData.m_ZonePrefab,
                                m_FromLevel = (int)spawnableBuildingData.m_Level,
                                m_ToLevel = (int)(spawnableBuildingData.m_Level + 1),
                                m_Squares = buildingData.m_LotSize.x * buildingData.m_LotSize.y
                            });
                            this.m_IconCommandBuffer.Add(entity, this.m_BuildingConfigurationData.m_LevelUpNotification, IconPriority.Info, IconClusterLayer.Transaction, (IconFlags)0, default(Entity), false, false, false, 0f);
                        }
                    }
                }
            }

            // Token: 0x06005C39 RID: 23609 RVA: 0x003682EC File Offset: 0x003664EC
            private Entity SelectSpawnableBuilding(ZoneType zoneType, int level, int2 lotSize, float maxHeight, Game.Prefabs.BuildingFlags accessFlags, BuildingPropertyData buildingPropertyData, ref Unity.Mathematics.Random random)
            {
                int num = 0;
                Entity result = Entity.Null;
                for (int i = 0; i < this.m_SpawnableBuildingChunks.Length; i++)
                {
                    ArchetypeChunk archetypeChunk = this.m_SpawnableBuildingChunks[i];
                    if (archetypeChunk.GetSharedComponent<BuildingSpawnGroupData>(this.m_BuildingSpawnGroupType).m_ZoneType.Equals(zoneType))
                    {
                        NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(this.m_EntityType);
                        NativeArray<SpawnableBuildingData> nativeArray2 = archetypeChunk.GetNativeArray<SpawnableBuildingData>(ref this.m_SpawnableBuildingType);
                        NativeArray<BuildingData> nativeArray3 = archetypeChunk.GetNativeArray<BuildingData>(ref this.m_BuildingType);
                        NativeArray<BuildingPropertyData> nativeArray4 = archetypeChunk.GetNativeArray<BuildingPropertyData>(ref this.m_BuildingPropertyType);
                        NativeArray<ObjectGeometryData> nativeArray5 = archetypeChunk.GetNativeArray<ObjectGeometryData>(ref this.m_ObjectGeometryType);
                        for (int j = 0; j < archetypeChunk.Count; j++)
                        {
                            SpawnableBuildingData spawnableBuildingData = nativeArray2[j];
                            BuildingData buildingData = nativeArray3[j];
                            BuildingPropertyData buildingPropertyData2 = nativeArray4[j];
                            ObjectGeometryData objectGeometryData = nativeArray5[j];
                            if (level == (int)spawnableBuildingData.m_Level && lotSize.Equals(buildingData.m_LotSize) && objectGeometryData.m_Size.y <= maxHeight && (buildingData.m_Flags & (Game.Prefabs.BuildingFlags.LeftAccess | Game.Prefabs.BuildingFlags.RightAccess)) == accessFlags && buildingPropertyData.m_ResidentialProperties <= buildingPropertyData2.m_ResidentialProperties && buildingPropertyData.m_AllowedManufactured == buildingPropertyData2.m_AllowedManufactured && buildingPropertyData.m_AllowedSold == buildingPropertyData2.m_AllowedSold && buildingPropertyData.m_AllowedStored == buildingPropertyData2.m_AllowedStored)
                            {
                                int num2 = 100;
                                num += num2;
                                if (random.NextInt(num) < num2)
                                {
                                    result = nativeArray[j];
                                }
                            }
                        }
                    }
                }
                return result;
            }

            // Token: 0x06005C3A RID: 23610 RVA: 0x0036847C File Offset: 0x0036667C
            private float GetMaxHeight(Entity building, BuildingData prefabBuildingData)
            {
                Game.Objects.Transform transform = this.m_TransformData[building];
                float2 xz = math.rotate(transform.m_Rotation, new float3(8f, 0f, 0f)).xz;
                float2 xz2 = math.rotate(transform.m_Rotation, new float3(0f, 0f, 8f)).xz;
                float2 @float = xz * ((float)prefabBuildingData.m_LotSize.x * 0.5f - 0.5f);
                float2 float2 = xz2 * ((float)prefabBuildingData.m_LotSize.y * 0.5f - 0.5f);
                float2 rhs = math.abs(float2) + math.abs(@float);
                PropertyRenterSystem.LevelupJob.Iterator iterator = default(PropertyRenterSystem.LevelupJob.Iterator);
                iterator.m_Bounds = new Bounds2(transform.m_Position.xz - rhs, transform.m_Position.xz + rhs);
                iterator.m_LotSize = prefabBuildingData.m_LotSize;
                iterator.m_StartPosition = transform.m_Position.xz + float2 + @float;
                iterator.m_Right = xz;
                iterator.m_Forward = xz2;
                iterator.m_MaxHeight = int.MaxValue;
                iterator.m_BlockData = this.m_BlockData;
                iterator.m_ValidAreaData = this.m_ValidAreaData;
                iterator.m_Cells = this.m_Cells;
                PropertyRenterSystem.LevelupJob.Iterator iterator2 = iterator;
                this.m_ZoneSearchTree.Iterate<PropertyRenterSystem.LevelupJob.Iterator>(ref iterator2, 0);
                return (float)iterator2.m_MaxHeight - transform.m_Position.y;
            }

            // Token: 0x040095AB RID: 38315
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            // Token: 0x040095AC RID: 38316
            [ReadOnly]
            public ComponentTypeHandle<SpawnableBuildingData> m_SpawnableBuildingType;

            // Token: 0x040095AD RID: 38317
            [ReadOnly]
            public ComponentTypeHandle<BuildingData> m_BuildingType;

            // Token: 0x040095AE RID: 38318
            [ReadOnly]
            public ComponentTypeHandle<BuildingPropertyData> m_BuildingPropertyType;

            // Token: 0x040095AF RID: 38319
            [ReadOnly]
            public ComponentTypeHandle<ObjectGeometryData> m_ObjectGeometryType;

            // Token: 0x040095B0 RID: 38320
            [ReadOnly]
            public SharedComponentTypeHandle<BuildingSpawnGroupData> m_BuildingSpawnGroupType;

            // Token: 0x040095B1 RID: 38321
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> m_TransformData;

            // Token: 0x040095B2 RID: 38322
            [ReadOnly]
            public ComponentLookup<Block> m_BlockData;

            // Token: 0x040095B3 RID: 38323
            [ReadOnly]
            public ComponentLookup<ValidArea> m_ValidAreaData;

            // Token: 0x040095B4 RID: 38324
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_Prefabs;

            // Token: 0x040095B5 RID: 38325
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildings;

            // Token: 0x040095B6 RID: 38326
            [ReadOnly]
            public ComponentLookup<BuildingData> m_Buildings;

            // Token: 0x040095B7 RID: 38327
            [ReadOnly]
            public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;

            // Token: 0x040095B8 RID: 38328
            [ReadOnly]
            public ComponentLookup<OfficeBuilding> m_OfficeBuilding;

            // Token: 0x040095B9 RID: 38329
            [ReadOnly]
            public ComponentLookup<ZoneData> m_ZoneData;

            // Token: 0x040095BA RID: 38330
            [ReadOnly]
            public BufferLookup<Cell> m_Cells;

            // Token: 0x040095BB RID: 38331
            [ReadOnly]
            public BuildingConfigurationData m_BuildingConfigurationData;

            // Token: 0x040095BC RID: 38332
            [ReadOnly]
            public NativeList<ArchetypeChunk> m_SpawnableBuildingChunks;

            // Token: 0x040095BD RID: 38333
            [ReadOnly]
            public NativeQuadTree<Entity, Bounds2> m_ZoneSearchTree;

            // Token: 0x040095BE RID: 38334
            [ReadOnly]
            public RandomSeed m_RandomSeed;

            // Token: 0x040095BF RID: 38335
            public IconCommandBuffer m_IconCommandBuffer;

            // Token: 0x040095C0 RID: 38336
            public NativeQueue<Entity> m_LevelupQueue;

            // Token: 0x040095C1 RID: 38337
            public EntityCommandBuffer m_CommandBuffer;

            // Token: 0x040095C2 RID: 38338
            public NativeQueue<TriggerAction> m_TriggerBuffer;

            // Token: 0x040095C3 RID: 38339
            public NativeQueue<ZoneBuiltLevelUpdate> m_ZoneBuiltLevelQueue;

            // Token: 0x02001463 RID: 5219
            private struct Iterator : INativeQuadTreeIterator<Entity, Bounds2>, IUnsafeQuadTreeIterator<Entity, Bounds2>
            {
                // Token: 0x06005C3B RID: 23611 RVA: 0x0036860B File Offset: 0x0036680B
                public bool Intersect(Bounds2 bounds)
                {
                    return MathUtils.Intersect(bounds, this.m_Bounds);
                }

                // Token: 0x06005C3C RID: 23612 RVA: 0x0036861C File Offset: 0x0036681C
                public void Iterate(Bounds2 bounds, Entity blockEntity)
                {
                    if (!MathUtils.Intersect(bounds, this.m_Bounds))
                    {
                        return;
                    }
                    ValidArea validArea = this.m_ValidAreaData[blockEntity];
                    if (validArea.m_Area.y <= validArea.m_Area.x)
                    {
                        return;
                    }
                    Block block = this.m_BlockData[blockEntity];
                    DynamicBuffer<Cell> dynamicBuffer = this.m_Cells[blockEntity];
                    float2 @float = this.m_StartPosition;
                    int2 @int;
                    @int.y = 0;
                    while (@int.y < this.m_LotSize.y)
                    {
                        float2 float2 = @float;
                        @int.x = 0;
                        while (@int.x < this.m_LotSize.x)
                        {
                            int2 cellIndex = ZoneUtils.GetCellIndex(block, float2);
                            if (math.all(cellIndex >= validArea.m_Area.xz & cellIndex < validArea.m_Area.yw))
                            {
                                int index = cellIndex.y * block.m_Size.x + cellIndex.x;
                                Cell cell = dynamicBuffer[index];
                                if ((cell.m_State & CellFlags.Visible) != CellFlags.None)
                                {
                                    this.m_MaxHeight = math.min(this.m_MaxHeight, (int)cell.m_Height);
                                }
                            }
                            float2 -= this.m_Right;
                            @int.x++;
                        }
                        @float -= this.m_Forward;
                        @int.y++;
                    }
                }

                // Token: 0x040095C4 RID: 38340
                public Bounds2 m_Bounds;

                // Token: 0x040095C5 RID: 38341
                public int2 m_LotSize;

                // Token: 0x040095C6 RID: 38342
                public float2 m_StartPosition;

                // Token: 0x040095C7 RID: 38343
                public float2 m_Right;

                // Token: 0x040095C8 RID: 38344
                public float2 m_Forward;

                // Token: 0x040095C9 RID: 38345
                public int m_MaxHeight;

                // Token: 0x040095CA RID: 38346
                public ComponentLookup<Block> m_BlockData;

                // Token: 0x040095CB RID: 38347
                public ComponentLookup<ValidArea> m_ValidAreaData;

                // Token: 0x040095CC RID: 38348
                public BufferLookup<Cell> m_Cells;
            }
        }

        // Token: 0x02001464 RID: 5220
        private struct LandlordMoneyJob : IJob
        {
            // Token: 0x06005C3D RID: 23613 RVA: 0x00368788 File Offset: 0x00366988
            public void Execute()
            {
                DynamicBuffer<Game.Economy.Resources> resources = this.m_Resources[this.m_LandlordEntity];
                int amount;
                while (this.m_PaymentQueue.TryDequeue(out amount))
                {
                    EconomyUtils.AddResources(Resource.Money, amount, resources);
                }
                if (EconomyUtils.GetResources(Resource.Money, resources) < 0)
                {
                    EconomyUtils.SetResources(Resource.Money, resources, 0);
                }
            }

            // Token: 0x040095CD RID: 38349
            public BufferLookup<Game.Economy.Resources> m_Resources;

            // Token: 0x040095CE RID: 38350
            public Entity m_LandlordEntity;

            // Token: 0x040095CF RID: 38351
            public NativeQueue<int> m_PaymentQueue;
        }

        private struct ReturnRentJob : IJobChunk
        {
            // Token: 0x06005C3E RID: 23614 RVA: 0x003687D8 File Offset: 0x003669D8
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (chunk.GetSharedComponent<UpdateFrame>(this.m_UpdateFrameType).m_Index != this.m_UpdateFrameIndex)
                {
                    return;
                }
                BufferAccessor<HouseholdCitizen> bufferAccessor = chunk.GetBufferAccessor<HouseholdCitizen>(ref this.m_HouseholdCitizenType);
                DynamicBuffer<Game.Economy.Resources> resources = this.m_Resources[this.m_LandlordEntity];
                int num = EconomyUtils.GetResources(Resource.Money, resources);
                if (num < 0)
                {
                    return;
                }
                num /= 32;
                int num2 = CityStatisticsSystem.GetStatisticValue(this.m_StatisticsLookup, this.m_Statistics, StatisticType.EducationCount, 0);
                int num3 = CityStatisticsSystem.GetStatisticValue(this.m_StatisticsLookup, this.m_Statistics, StatisticType.EducationCount, 1);
                int num4 = CityStatisticsSystem.GetStatisticValue(this.m_StatisticsLookup, this.m_Statistics, StatisticType.EducationCount, 2);
                int num5 = CityStatisticsSystem.GetStatisticValue(this.m_StatisticsLookup, this.m_Statistics, StatisticType.EducationCount, 3);
                int num6 = CityStatisticsSystem.GetStatisticValue(this.m_StatisticsLookup, this.m_Statistics, StatisticType.EducationCount, 4);
                int num7 = num2 * this.m_EconomyParameters.m_RentReturnUneducated + num3 * this.m_EconomyParameters.m_RentReturnPoorlyEducated + num4 * this.m_EconomyParameters.m_RentReturnEducated + num5 * this.m_EconomyParameters.m_RentReturnWellEducated + num6 * this.m_EconomyParameters.m_RentReturnHighlyEducated;
                if (num7 == 0)
                {
                    return;
                }
                num2 = num * this.m_EconomyParameters.m_RentReturnUneducated / num7;
                num3 = num * this.m_EconomyParameters.m_RentReturnPoorlyEducated / num7;
                num4 = num * this.m_EconomyParameters.m_RentReturnEducated / num7;
                num5 = num * this.m_EconomyParameters.m_RentReturnWellEducated / num7;
                num6 = num * this.m_EconomyParameters.m_RentReturnHighlyEducated / num7;
                int num8 = 0;
                NativeArray<Entity> nativeArray = chunk.GetNativeArray(this.m_EntityType);
                for (int i = 0; i < nativeArray.Length; i++)
                {
                    num = 0;
                    DynamicBuffer<HouseholdCitizen> dynamicBuffer = bufferAccessor[i];
                    for (int j = 0; j < dynamicBuffer.Length; j++)
                    {
                        Entity citizen = dynamicBuffer[j].m_Citizen;
                        if (this.m_Citizens.HasComponent(citizen))
                        {
                            CitizenAge age = this.m_Citizens[citizen].GetAge();
                            if (age == CitizenAge.Adult || age == CitizenAge.Elderly)
                            {
                                switch (this.m_Citizens[citizen].GetEducationLevel())
                                {
                                    case 0:
                                        num += num2;
                                        num8 += num2;
                                        break;
                                    case 1:
                                        num += num3;
                                        num8 += num3;
                                        break;
                                    case 2:
                                        num += num4;
                                        num8 += num4;
                                        break;
                                    case 3:
                                        num += num5;
                                        num8 += num5;
                                        break;
                                    case 4:
                                        num += num6;
                                        num8 += num6;
                                        break;
                                }
                            }
                        }
                    }
                    EconomyUtils.AddResources(Resource.Money, num, this.m_Resources[nativeArray[i]]);
                }
                this.m_PaymentQueue.Enqueue(-2 * num8);
            }

            // Token: 0x06005C3F RID: 23615 RVA: 0x00368A85 File Offset: 0x00366C85
            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                this.Execute(chunk, unfilteredChunkIndex, useEnabledMask, chunkEnabledMask);
            }

            // Token: 0x040095D0 RID: 38352
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            // Token: 0x040095D1 RID: 38353
            [ReadOnly]
            public BufferTypeHandle<HouseholdCitizen> m_HouseholdCitizenType;

            // Token: 0x040095D2 RID: 38354
            [ReadOnly]
            public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

            // Token: 0x040095D3 RID: 38355
            [NativeDisableParallelForRestriction]
            public BufferLookup<Game.Economy.Resources> m_Resources;

            // Token: 0x040095D4 RID: 38356
            [ReadOnly]
            public EconomyParameterData m_EconomyParameters;

            // Token: 0x040095D5 RID: 38357
            [ReadOnly]
            public ComponentLookup<Citizen> m_Citizens;

            // Token: 0x040095D6 RID: 38358
            [ReadOnly]
            public BufferLookup<CityStatistic> m_Statistics;

            // Token: 0x040095D7 RID: 38359
            [ReadOnly]
            public NativeParallelHashMap<CityStatisticsSystem.StatisticsKey, Entity> m_StatisticsLookup;

            // Token: 0x040095D8 RID: 38360
            public uint m_UpdateFrameIndex;

            // Token: 0x040095D9 RID: 38361
            public Entity m_LandlordEntity;

            // Token: 0x040095DA RID: 38362
            public NativeQueue<int>.ParallelWriter m_PaymentQueue;
        }

        private struct PayRentJob : IJobChunk
        {
            // Token: 0x06005C40 RID: 23616 RVA: 0x00368A94 File Offset: 0x00366C94
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (chunk.GetSharedComponent<UpdateFrame>(this.m_UpdateFrameType).m_Index != this.m_UpdateFrameIndex)
                {
                    return;
                }
                Unity.Mathematics.Random random = this.m_RandomSeed.GetRandom(1 + unfilteredChunkIndex);
                NativeArray<Entity> nativeArray = chunk.GetNativeArray(this.m_EntityType);
                NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray<PrefabRef>(ref this.m_PrefabType);
                BufferAccessor<Renter> bufferAccessor = chunk.GetBufferAccessor<Renter>(ref this.m_RenterType);
                NativeArray<BuildingCondition> nativeArray3 = chunk.GetNativeArray<BuildingCondition>(ref this.m_ConditionType);
                DynamicBuffer<CityModifier> cityEffects = this.m_CityEffects[this.m_City];
                for (int i = 0; i < chunk.Count; i++)
                {
                    DynamicBuffer<Renter> dynamicBuffer = bufferAccessor[i];
                    Entity prefab = nativeArray2[i].m_Prefab;
                    if (this.m_SpawnableBuildingData.HasComponent(prefab))
                    {
                        SpawnableBuildingData spawnableBuildingData = this.m_SpawnableBuildingData[prefab];
                        AreaType areaType = this.m_ZoneData[spawnableBuildingData.m_ZonePrefab].m_AreaType;
                        BuildingPropertyData buildingPropertyData = this.m_BuildingProperties[prefab];
                        int levelingCost = GetLevelingCost(areaType, buildingPropertyData, (int)spawnableBuildingData.m_Level, cityEffects);
                        int num = (spawnableBuildingData.m_Level == 5) ? GetLevelingCost(areaType, buildingPropertyData, 4, cityEffects) : levelingCost;
                        if (areaType == AreaType.Residential && buildingPropertyData.m_ResidentialProperties > 1)
                        {
                            num = Mathf.RoundToInt((float)(num * (int)(6 - spawnableBuildingData.m_Level)) / math.sqrt((float)buildingPropertyData.m_ResidentialProperties));
                        }
                        int num2 = 0;
                        for (int j = 0; j < dynamicBuffer.Length; j++)
                        {
                            Entity renter = dynamicBuffer[j].m_Renter;
                            if (this.m_PropertyRenters.HasComponent(renter))
                            {
                                PropertyRenter propertyRenter = this.m_PropertyRenters[renter];
                                int num3;
                                if (this.m_Storages.HasComponent(renter))
                                {
                                    num3 = EconomyUtils.GetResources(Resource.Money, this.m_Resources[renter]);
                                }
                                else
                                {
                                    num3 = MathUtils.RoundToIntRandom(ref random, (float)propertyRenter.m_Rent * 1f / (float)PropertyRenterSystem.kUpdatesPerDay);
                                    num2 += num3;
                                }
                                EconomyUtils.AddResources(Resource.Money, -num3, this.m_Resources[renter]);
                                if (nativeArray3.Length > 0)
                                {
                                    BuildingCondition buildingCondition = nativeArray3[i];
                                    if (this.m_DebugFastLeveling)
                                    {
                                        buildingCondition.m_Condition = levelingCost;
                                    }
                                    else if (areaType == AreaType.Industrial && this.m_Profitabilities.HasComponent(renter))
                                    {
                                        buildingCondition.m_Condition += (int)((float)num3 * (1f + (float)(math.max((int)(spawnableBuildingData.m_Level - 1), 0) * (int)this.m_Profitabilities[renter].m_Profitability) / 255f));
                                    }
                                    else
                                    {
                                        buildingCondition.m_Condition += num3;
                                    }
                                    if (buildingCondition.m_Condition >= levelingCost)
                                    {
                                        this.m_LevelupQueue.Enqueue(nativeArray[i]);
                                        buildingCondition.m_Condition -= levelingCost;
                                    }
                                    nativeArray3[i] = buildingCondition;
                                }
                            }
                        }
                        this.m_LandlordQueue.Enqueue(num2);
                        bool flag = !this.m_Abandoned.HasComponent(nativeArray[i]) && !this.m_Destroyed.HasComponent(nativeArray[i]);
                        if (flag && nativeArray3[i].m_Condition <= -num && !this.m_SignatureDatas.HasComponent(prefab))
                        {
                            this.m_LevelDownQueue.Enqueue(nativeArray[i]);
                            BuildingCondition value = nativeArray3[i];
                            value.m_Condition += levelingCost;
                            nativeArray3[i] = value;
                        }
                        for (int k = dynamicBuffer.Length - 1; k >= 0; k--)
                        {
                            Entity renter2 = dynamicBuffer[k].m_Renter;
                            if (!this.m_PropertyRenters.HasComponent(renter2))
                            {
                                dynamicBuffer.RemoveAt(k);
                            }
                        }
                        if (dynamicBuffer.Length < buildingPropertyData.CountProperties() && !this.m_PropertiesOnMarket.HasComponent(nativeArray[i]) && flag)
                        {
                            this.m_CommandBuffer.AddComponent<PropertyToBeOnMarket>(unfilteredChunkIndex, nativeArray[i], default(PropertyToBeOnMarket));
                        }
                        int num4 = buildingPropertyData.CountProperties();
                        while ((dynamicBuffer.Length > 0 && !flag) || dynamicBuffer.Length > num4)
                        {
                            Entity renter3 = dynamicBuffer[dynamicBuffer.Length - 1].m_Renter;
                            if (this.m_PropertyRenters.HasComponent(renter3))
                            {
                                this.m_CommandBuffer.RemoveComponent<PropertyRenter>(unfilteredChunkIndex, renter3);
                            }
                            dynamicBuffer.RemoveAt(dynamicBuffer.Length - 1);
                        }
                    }
                }
            }

            // Token: 0x06005C41 RID: 23617 RVA: 0x00368EFC File Offset: 0x003670FC
            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                this.Execute(chunk, unfilteredChunkIndex, useEnabledMask, chunkEnabledMask);
            }

            // Token: 0x040095DB RID: 38363
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            // Token: 0x040095DC RID: 38364
            public ComponentTypeHandle<BuildingCondition> m_ConditionType;

            // Token: 0x040095DD RID: 38365
            public BufferTypeHandle<Renter> m_RenterType;

            // Token: 0x040095DE RID: 38366
            [ReadOnly]
            public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

            // Token: 0x040095DF RID: 38367
            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> m_PrefabType;

            // Token: 0x040095E0 RID: 38368
            [ReadOnly]
            public ComponentLookup<Profitability> m_Profitabilities;

            // Token: 0x040095E1 RID: 38369
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingData;

            // Token: 0x040095E2 RID: 38370
            [ReadOnly]
            public ComponentLookup<ZoneData> m_ZoneData;

            // Token: 0x040095E3 RID: 38371
            [ReadOnly]
            public ComponentLookup<PropertyRenter> m_PropertyRenters;

            // Token: 0x040095E4 RID: 38372
            [NativeDisableParallelForRestriction]
            public BufferLookup<Game.Economy.Resources> m_Resources;

            // Token: 0x040095E5 RID: 38373
            [ReadOnly]
            public ComponentLookup<BuildingPropertyData> m_BuildingProperties;

            // Token: 0x040095E6 RID: 38374
            [ReadOnly]
            public ComponentLookup<PropertyOnMarket> m_PropertiesOnMarket;

            // Token: 0x040095E7 RID: 38375
            [ReadOnly]
            public ComponentLookup<Abandoned> m_Abandoned;

            // Token: 0x040095E8 RID: 38376
            [ReadOnly]
            public ComponentLookup<Destroyed> m_Destroyed;

            // Token: 0x040095E9 RID: 38377
            [ReadOnly]
            public ComponentLookup<Game.Companies.StorageCompany> m_Storages;

            // Token: 0x040095EA RID: 38378
            [ReadOnly]
            public BufferLookup<CityModifier> m_CityEffects;

            // Token: 0x040095EB RID: 38379
            [ReadOnly]
            public ComponentLookup<SignatureBuildingData> m_SignatureDatas;

            // Token: 0x040095EC RID: 38380
            public RandomSeed m_RandomSeed;

            // Token: 0x040095ED RID: 38381
            public Entity m_City;

            // Token: 0x040095EE RID: 38382
            public NativeQueue<Entity>.ParallelWriter m_LevelupQueue;

            // Token: 0x040095EF RID: 38383
            public NativeQueue<Entity>.ParallelWriter m_LevelDownQueue;

            // Token: 0x040095F0 RID: 38384
            public NativeQueue<int>.ParallelWriter m_LandlordQueue;

            // Token: 0x040095F1 RID: 38385
            public uint m_UpdateFrameIndex;

            // Token: 0x040095F2 RID: 38386
            public bool m_DebugFastLeveling;

            // Token: 0x040095F3 RID: 38387
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
        }

        private struct RenterMovingAwayJob : IJobChunk
        {
            // Token: 0x06005C42 RID: 23618 RVA: 0x00368F0C File Offset: 0x0036710C
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> nativeArray = chunk.GetNativeArray(this.m_EntityType);
                for (int i = 0; i < nativeArray.Length; i++)
                {
                    Entity entity = nativeArray[i];
                    PropertyRenter propertyRenter = this.m_PropertyRenters[entity];
                    this.m_CommandBuffer.RemoveComponent<PropertyRenter>(unfilteredChunkIndex, entity);
                    if (propertyRenter.m_Property != Entity.Null)
                    {
                        this.m_CommandBuffer.AddComponent<Updated>(unfilteredChunkIndex, propertyRenter.m_Property, default(Updated));
                    }
                }
            }

            // Token: 0x06005C43 RID: 23619 RVA: 0x00368F89 File Offset: 0x00367189
            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                this.Execute(chunk, unfilteredChunkIndex, useEnabledMask, chunkEnabledMask);
            }

            // Token: 0x040095F4 RID: 38388
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            // Token: 0x040095F5 RID: 38389
            [ReadOnly]
            public ComponentLookup<PropertyRenter> m_PropertyRenters;

            // Token: 0x040095F6 RID: 38390
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
        }

        // Token: 0x02001468 RID: 5224
        private struct TypeHandle
        {
            // Token: 0x06005C44 RID: 23620 RVA: 0x00368F98 File Offset: 0x00367198
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                this.__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(true);
                this.__Game_Buildings_Renter_RW_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(false);
                this.__Game_Buildings_BuildingCondition_RW_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingCondition>(false);
                this.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(true);
                this.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(true);
                this.__Game_Companies_Profitability_RO_ComponentLookup = state.GetComponentLookup<Profitability>(true);
                this.__Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(true);
                this.__Game_Economy_Resources_RW_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>(false);
                this.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(true);
                this.__Game_Buildings_PropertyOnMarket_RO_ComponentLookup = state.GetComponentLookup<PropertyOnMarket>(true);
                this.__Game_Buildings_Abandoned_RO_ComponentLookup = state.GetComponentLookup<Abandoned>(true);
                this.__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(true);
                this.__Game_Companies_StorageCompany_RO_ComponentLookup = state.GetComponentLookup<Game.Companies.StorageCompany>(true);
                this.__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(true);
                this.__Game_Prefabs_SignatureBuildingData_RO_ComponentLookup = state.GetComponentLookup<SignatureBuildingData>(true);
                this.__Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle = state.GetBufferTypeHandle<HouseholdCitizen>(true);
                this.__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(true);
                this.__Game_City_CityStatistic_RO_BufferLookup = state.GetBufferLookup<CityStatistic>(true);
                this.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SpawnableBuildingData>(true);
                this.__Game_Prefabs_BuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingData>(true);
                this.__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingPropertyData>(true);
                this.__Game_Prefabs_ObjectGeometryData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ObjectGeometryData>(true);
                this.__Game_Prefabs_BuildingSpawnGroupData_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<BuildingSpawnGroupData>();
                this.__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(true);
                this.__Game_Zones_Block_RO_ComponentLookup = state.GetComponentLookup<Block>(true);
                this.__Game_Zones_ValidArea_RO_ComponentLookup = state.GetComponentLookup<ValidArea>(true);
                this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(true);
                this.__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(true);
                this.__Game_Prefabs_OfficeBuilding_RO_ComponentLookup = state.GetComponentLookup<OfficeBuilding>(true);
                this.__Game_Zones_Cell_RO_BufferLookup = state.GetBufferLookup<Cell>(true);
                this.__Game_Buildings_Building_RW_ComponentLookup = state.GetComponentLookup<Building>(false);
                this.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup = state.GetComponentLookup<ElectricityConsumer>(true);
                this.__Game_Buildings_GarbageProducer_RO_ComponentLookup = state.GetComponentLookup<GarbageProducer>(true);
                this.__Game_Buildings_GroundPolluter_RO_ComponentLookup = state.GetComponentLookup<GroundPolluter>(true);
                this.__Game_Buildings_MailProducer_RO_ComponentLookup = state.GetComponentLookup<MailProducer>(true);
                this.__Game_Buildings_WaterConsumer_RO_ComponentLookup = state.GetComponentLookup<WaterConsumer>(true);
                this.__Game_Buildings_CrimeProducer_RW_ComponentLookup = state.GetComponentLookup<CrimeProducer>(false);
                this.__Game_Buildings_Renter_RW_BufferLookup = state.GetBufferLookup<Renter>(false);
            }

            // Token: 0x040095F7 RID: 38391
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

            // Token: 0x040095F8 RID: 38392
            [ReadOnly]
            public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

            // Token: 0x040095F9 RID: 38393
            public BufferTypeHandle<Renter> __Game_Buildings_Renter_RW_BufferTypeHandle;

            // Token: 0x040095FA RID: 38394
            public ComponentTypeHandle<BuildingCondition> __Game_Buildings_BuildingCondition_RW_ComponentTypeHandle;

            // Token: 0x040095FB RID: 38395
            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

            // Token: 0x040095FC RID: 38396
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

            // Token: 0x040095FD RID: 38397
            [ReadOnly]
            public ComponentLookup<Profitability> __Game_Companies_Profitability_RO_ComponentLookup;

            // Token: 0x040095FE RID: 38398
            [ReadOnly]
            public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;

            // Token: 0x040095FF RID: 38399
            public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RW_BufferLookup;

            // Token: 0x04009600 RID: 38400
            [ReadOnly]
            public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

            // Token: 0x04009601 RID: 38401
            [ReadOnly]
            public ComponentLookup<PropertyOnMarket> __Game_Buildings_PropertyOnMarket_RO_ComponentLookup;

            // Token: 0x04009602 RID: 38402
            [ReadOnly]
            public ComponentLookup<Abandoned> __Game_Buildings_Abandoned_RO_ComponentLookup;

            // Token: 0x04009603 RID: 38403
            [ReadOnly]
            public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

            // Token: 0x04009604 RID: 38404
            [ReadOnly]
            public ComponentLookup<Game.Companies.StorageCompany> __Game_Companies_StorageCompany_RO_ComponentLookup;

            // Token: 0x04009605 RID: 38405
            [ReadOnly]
            public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

            // Token: 0x04009606 RID: 38406
            [ReadOnly]
            public ComponentLookup<SignatureBuildingData> __Game_Prefabs_SignatureBuildingData_RO_ComponentLookup;

            // Token: 0x04009607 RID: 38407
            [ReadOnly]
            public BufferTypeHandle<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle;

            // Token: 0x04009608 RID: 38408
            [ReadOnly]
            public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

            // Token: 0x04009609 RID: 38409
            [ReadOnly]
            public BufferLookup<CityStatistic> __Game_City_CityStatistic_RO_BufferLookup;

            // Token: 0x0400960A RID: 38410
            [ReadOnly]
            public ComponentTypeHandle<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle;

            // Token: 0x0400960B RID: 38411
            [ReadOnly]
            public ComponentTypeHandle<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentTypeHandle;

            // Token: 0x0400960C RID: 38412
            [ReadOnly]
            public ComponentTypeHandle<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle;

            // Token: 0x0400960D RID: 38413
            [ReadOnly]
            public ComponentTypeHandle<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentTypeHandle;

            // Token: 0x0400960E RID: 38414
            public SharedComponentTypeHandle<BuildingSpawnGroupData> __Game_Prefabs_BuildingSpawnGroupData_SharedComponentTypeHandle;

            // Token: 0x0400960F RID: 38415
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

            // Token: 0x04009610 RID: 38416
            [ReadOnly]
            public ComponentLookup<Block> __Game_Zones_Block_RO_ComponentLookup;

            // Token: 0x04009611 RID: 38417
            [ReadOnly]
            public ComponentLookup<ValidArea> __Game_Zones_ValidArea_RO_ComponentLookup;

            // Token: 0x04009612 RID: 38418
            [ReadOnly]
            public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

            // Token: 0x04009613 RID: 38419
            [ReadOnly]
            public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

            // Token: 0x04009614 RID: 38420
            [ReadOnly]
            public ComponentLookup<OfficeBuilding> __Game_Prefabs_OfficeBuilding_RO_ComponentLookup;

            // Token: 0x04009615 RID: 38421
            [ReadOnly]
            public BufferLookup<Cell> __Game_Zones_Cell_RO_BufferLookup;

            // Token: 0x04009616 RID: 38422
            public ComponentLookup<Building> __Game_Buildings_Building_RW_ComponentLookup;

            // Token: 0x04009617 RID: 38423
            [ReadOnly]
            public ComponentLookup<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RO_ComponentLookup;

            // Token: 0x04009618 RID: 38424
            [ReadOnly]
            public ComponentLookup<GarbageProducer> __Game_Buildings_GarbageProducer_RO_ComponentLookup;

            // Token: 0x04009619 RID: 38425
            [ReadOnly]
            public ComponentLookup<GroundPolluter> __Game_Buildings_GroundPolluter_RO_ComponentLookup;

            // Token: 0x0400961A RID: 38426
            [ReadOnly]
            public ComponentLookup<MailProducer> __Game_Buildings_MailProducer_RO_ComponentLookup;

            // Token: 0x0400961B RID: 38427
            [ReadOnly]
            public ComponentLookup<WaterConsumer> __Game_Buildings_WaterConsumer_RO_ComponentLookup;

            // Token: 0x0400961C RID: 38428
            public ComponentLookup<CrimeProducer> __Game_Buildings_CrimeProducer_RW_ComponentLookup;

            // Token: 0x0400961D RID: 38429
            public BufferLookup<Renter> __Game_Buildings_Renter_RW_BufferLookup;
        }
    }
}
