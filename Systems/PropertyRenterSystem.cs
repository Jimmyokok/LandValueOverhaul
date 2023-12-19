using Game;
using Game.Simulation;
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
using Game.Areas;
using Game.Rendering;
using AreaType = Game.Zones.AreaType;

namespace LandValueOverhaul.Systems
{
    // Token: 0x0200134B RID: 4939
    [CompilerGenerated]
    public class CustomPropertyRenterSystem : GameSystemBase, IDefaultSerializable, ISerializable, IPostDeserialize
    {
        // Token: 0x060055A6 RID: 21926 RVA: 0x0008E5B8 File Offset: 0x0008C7B8
        public static float GetUpkeepExponent(AreaType type)
        {
            if (type == AreaType.Residential)
            {
                return CustomPropertyRenterSystem.kResidentialUpkeepExponent;
            }
            if (type == AreaType.Industrial)
            {
                return CustomPropertyRenterSystem.kIndustrialUpkeepExponent;
            }
            if (type == AreaType.None)
            {
                return 1f;
            }
            return CustomPropertyRenterSystem.kCOUpkeepExponent;
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
                num2 = (float)((currentlevel <= 4) ? (num * Mathf.RoundToInt(math.pow(2f, (float)(currentlevel)) * 80f)) : 1073741823);
            }
            CityUtils.ApplyModifier(ref num2, cityEffects, CityModifierType.BuildingLevelingCost);
            return Mathf.RoundToInt(num2);
        }

        // Token: 0x060055A7 RID: 21927 RVA: 0x0008E5DC File Offset: 0x0008C7DC
        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 262144 / (CustomPropertyRenterSystem.kUpdatesPerDay * 16);
        }

        // Token: 0x170008D2 RID: 2258
        // (get) Token: 0x060055A9 RID: 21929 RVA: 0x0008E5EC File Offset: 0x0008C7EC
        public Entity Landlords
        {
            get
            {
                return this.m_LandlordEntity;
            }
        }

        // Token: 0x060055AA RID: 21930 RVA: 0x0008E5F4 File Offset: 0x0008C7F4
        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(this.m_LandlordEntity);
        }

        // Token: 0x060055AB RID: 21931 RVA: 0x003B2B38 File Offset: 0x003B0D38
        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            if (reader.context.version >= Version.taxRateArrayLength)
            {
                reader.Read(out this.m_LandlordEntity);
            }
        }

        // Token: 0x060055AC RID: 21932 RVA: 0x0005E08F File Offset: 0x0005C28F
        public void SetDefaults(Context context)
        {
        }

        // Token: 0x060055AD RID: 21933 RVA: 0x0008E609 File Offset: 0x0008C809
        public void PostDeserialize(Context context)
        {
            if (context.purpose == Colossal.Serialization.Entities.Purpose.NewGame)
            {
                this.CreateLandlordEntity();
            }
        }

        // Token: 0x060055AE RID: 21934 RVA: 0x003B2B7C File Offset: 0x003B0D7C
        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
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

        // Token: 0x060055AF RID: 21935 RVA: 0x0008E61B File Offset: 0x0008C81B
        [Preserve]
        protected override void OnDestroy()
        {
            this.m_PaymentQueue.Dispose();
            this.m_LevelupQueue.Dispose();
            this.m_LeveldownQueue.Dispose();
            base.OnDestroy();
        }

        // Token: 0x060055B0 RID: 21936 RVA: 0x003B2DFC File Offset: 0x003B0FFC
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

        // Token: 0x060055B1 RID: 21937 RVA: 0x003B2EB0 File Offset: 0x003B10B0
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
            uint updateFrame = SimulationUtils.GetUpdateFrame(this.m_SimulationSystem.frameIndex, CustomPropertyRenterSystem.kUpdatesPerDay, 16);
            uint updateFrame2 = SimulationUtils.GetUpdateFrame(this.m_SimulationSystem.frameIndex, CustomPropertyRenterSystem.kUpdatesPerDay, 16);
            BuildingConfigurationData singleton = this.m_BuildingSettingsQuery.GetSingleton<BuildingConfigurationData>();
            this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
            CustomPropertyRenterSystem.RenterMovingAwayJob jobData = default(CustomPropertyRenterSystem.RenterMovingAwayJob);
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
            this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_BuildingCondition_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Renter_RW_BufferTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Objects_Attached_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            CustomPropertyRenterSystem.PayRentJob jobData2 = default(CustomPropertyRenterSystem.PayRentJob);
            jobData2.m_Attached = this.__TypeHandle.__Game_Objects_Attached_RO_ComponentLookup;
            jobData2.m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle;
            jobData2.m_RenterType = this.__TypeHandle.__Game_Buildings_Renter_RW_BufferTypeHandle;
            jobData2.m_ConditionType = this.__TypeHandle.__Game_Buildings_BuildingCondition_RW_ComponentTypeHandle;
            jobData2.m_PrefabType = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
            jobData2.m_UpdateFrameType = base.GetSharedComponentTypeHandle<UpdateFrame>();
            jobData2.m_SpawnableBuildingData = this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;
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
            CustomPropertyRenterSystem.ReturnRentJob jobData3 = default(CustomPropertyRenterSystem.ReturnRentJob);
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
            CustomPropertyRenterSystem.LandlordMoneyJob jobData4 = default(CustomPropertyRenterSystem.LandlordMoneyJob);
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
            CustomPropertyRenterSystem.LevelupJob jobData5 = default(CustomPropertyRenterSystem.LevelupJob);
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
            CustomPropertyRenterSystem.LeveldownJob jobData6 = default(CustomPropertyRenterSystem.LeveldownJob);
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

        // Token: 0x060055B2 RID: 21938 RVA: 0x003B3A9C File Offset: 0x003B1C9C
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

        // Token: 0x060055B3 RID: 21939 RVA: 0x003B3B1C File Offset: 0x003B1D1C
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

        // Token: 0x060055B4 RID: 21940 RVA: 0x0005E08F File Offset: 0x0005C28F
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
        }

        // Token: 0x060055B5 RID: 21941 RVA: 0x0008E644 File Offset: 0x0008C844
        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            this.__AssignQueries(ref base.CheckedStateRef);
            this.__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
        }

        // Token: 0x060055B6 RID: 21942 RVA: 0x0005E948 File Offset: 0x0005CB48
        [Preserve]
        public CustomPropertyRenterSystem()
        {
        }

        // Token: 0x0400906C RID: 36972
        public static readonly int kUpdatesPerDay = 16;

        // Token: 0x0400906D RID: 36973
        private static readonly float kResidentialUpkeepExponent = 1.3f;

        // Token: 0x0400906E RID: 36974
        private static readonly float kCOUpkeepExponent = 2.1f;

        // Token: 0x0400906F RID: 36975
        private static readonly float kIndustrialUpkeepExponent = 2f;

        // Token: 0x04009070 RID: 36976
        private SimulationSystem m_SimulationSystem;

        // Token: 0x04009071 RID: 36977
        private EndFrameBarrier m_EndFrameBarrier;

        // Token: 0x04009072 RID: 36978
        private CityStatisticsSystem m_CityStatisticsSystem;

        // Token: 0x04009073 RID: 36979
        private CitySystem m_CitySystem;

        // Token: 0x04009074 RID: 36980
        private IconCommandSystem m_IconCommandSystem;

        // Token: 0x04009075 RID: 36981
        private TriggerSystem m_TriggerSystem;

        // Token: 0x04009076 RID: 36982
        private ZoneBuiltRequirementSystem m_ZoneBuiltRequirementSystemSystem;

        // Token: 0x04009077 RID: 36983
        private Game.Zones.SearchSystem m_ZoneSearchSystem;

        // Token: 0x04009078 RID: 36984
        private ElectricityRoadConnectionGraphSystem m_ElectricityRoadConnectionGraphSystem;

        // Token: 0x04009079 RID: 36985
        private WaterPipeRoadConnectionGraphSystem m_WaterPipeRoadConnectionGraphSystem;

        // Token: 0x0400907A RID: 36986
        private EntityQuery m_EconomyParameterQuery;

        // Token: 0x0400907B RID: 36987
        private EntityQuery m_BuildingSettingsQuery;

        // Token: 0x0400907C RID: 36988
        private EntityQuery m_BuildingGroup;

        // Token: 0x0400907D RID: 36989
        private EntityQuery m_BuildingPrefabGroup;

        // Token: 0x0400907E RID: 36990
        private EntityQuery m_HouseholdGroup;

        // Token: 0x0400907F RID: 36991
        private EntityQuery m_MovingAwayHouseholdGroup;

        // Token: 0x04009080 RID: 36992
        private EntityQuery m_LandlordQuery;

        // Token: 0x04009081 RID: 36993
        private NativeQueue<Entity> m_LevelupQueue;

        // Token: 0x04009082 RID: 36994
        private NativeQueue<Entity> m_LeveldownQueue;

        // Token: 0x04009083 RID: 36995
        private NativeQueue<int> m_PaymentQueue;

        // Token: 0x04009084 RID: 36996
        private Entity m_LandlordEntity;

        // Token: 0x04009085 RID: 36997
        public bool debugFastLeveling;

        // Token: 0x04009086 RID: 36998
        private CustomPropertyRenterSystem.TypeHandle __TypeHandle;

        // Token: 0x0200134C RID: 4940
        [BurstCompile]
        private struct LeveldownJob : IJob
        {
            // Token: 0x060055B8 RID: 21944 RVA: 0x003B3BEC File Offset: 0x003B1DEC
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

            // Token: 0x04009087 RID: 36999
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_Prefabs;

            // Token: 0x04009088 RID: 37000
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildings;

            // Token: 0x04009089 RID: 37001
            [ReadOnly]
            public ComponentLookup<BuildingData> m_BuildingDatas;

            // Token: 0x0400908A RID: 37002
            public ComponentLookup<Building> m_Buildings;

            // Token: 0x0400908B RID: 37003
            [ReadOnly]
            public ComponentLookup<ElectricityConsumer> m_ElectricityConsumers;

            // Token: 0x0400908C RID: 37004
            [ReadOnly]
            public ComponentLookup<WaterConsumer> m_WaterConsumers;

            // Token: 0x0400908D RID: 37005
            [ReadOnly]
            public ComponentLookup<GarbageProducer> m_GarbageProducers;

            // Token: 0x0400908E RID: 37006
            [ReadOnly]
            public ComponentLookup<GroundPolluter> m_GroundPolluters;

            // Token: 0x0400908F RID: 37007
            [ReadOnly]
            public ComponentLookup<MailProducer> m_MailProducers;

            // Token: 0x04009090 RID: 37008
            [ReadOnly]
            public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;

            // Token: 0x04009091 RID: 37009
            [ReadOnly]
            public ComponentLookup<OfficeBuilding> m_OfficeBuilding;

            // Token: 0x04009092 RID: 37010
            public NativeQueue<TriggerAction> m_TriggerBuffer;

            // Token: 0x04009093 RID: 37011
            public ComponentLookup<CrimeProducer> m_CrimeProducers;

            // Token: 0x04009094 RID: 37012
            public BufferLookup<Renter> m_Renters;

            // Token: 0x04009095 RID: 37013
            [ReadOnly]
            public BuildingConfigurationData m_BuildingConfigurationData;

            // Token: 0x04009096 RID: 37014
            public NativeQueue<Entity> m_LeveldownQueue;

            // Token: 0x04009097 RID: 37015
            public EntityCommandBuffer m_CommandBuffer;

            // Token: 0x04009098 RID: 37016
            public NativeQueue<Entity> m_UpdatedElectricityRoadEdges;

            // Token: 0x04009099 RID: 37017
            public NativeQueue<Entity> m_UpdatedWaterPipeRoadEdges;

            // Token: 0x0400909A RID: 37018
            public IconCommandBuffer m_IconCommandBuffer;

            // Token: 0x0400909B RID: 37019
            public uint m_SimulationFrame;
        }

        // Token: 0x0200134D RID: 4941
        [BurstCompile]
        private struct LevelupJob : IJob
        {
            // Token: 0x060055B9 RID: 21945 RVA: 0x003B3F5C File Offset: 0x003B215C
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

            // Token: 0x060055BA RID: 21946 RVA: 0x003B4190 File Offset: 0x003B2390
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

            // Token: 0x060055BB RID: 21947 RVA: 0x003B4320 File Offset: 0x003B2520
            private float GetMaxHeight(Entity building, BuildingData prefabBuildingData)
            {
                Game.Objects.Transform transform = this.m_TransformData[building];
                float2 xz = math.rotate(transform.m_Rotation, new float3(8f, 0f, 0f)).xz;
                float2 xz2 = math.rotate(transform.m_Rotation, new float3(0f, 0f, 8f)).xz;
                float2 @float = xz * ((float)prefabBuildingData.m_LotSize.x * 0.5f - 0.5f);
                float2 float2 = xz2 * ((float)prefabBuildingData.m_LotSize.y * 0.5f - 0.5f);
                float2 rhs = math.abs(float2) + math.abs(@float);
                CustomPropertyRenterSystem.LevelupJob.Iterator iterator = default(CustomPropertyRenterSystem.LevelupJob.Iterator);
                iterator.m_Bounds = new Bounds2(transform.m_Position.xz - rhs, transform.m_Position.xz + rhs);
                iterator.m_LotSize = prefabBuildingData.m_LotSize;
                iterator.m_StartPosition = transform.m_Position.xz + float2 + @float;
                iterator.m_Right = xz;
                iterator.m_Forward = xz2;
                iterator.m_MaxHeight = int.MaxValue;
                iterator.m_BlockData = this.m_BlockData;
                iterator.m_ValidAreaData = this.m_ValidAreaData;
                iterator.m_Cells = this.m_Cells;
                CustomPropertyRenterSystem.LevelupJob.Iterator iterator2 = iterator;
                this.m_ZoneSearchTree.Iterate<CustomPropertyRenterSystem.LevelupJob.Iterator>(ref iterator2, 0);
                return (float)iterator2.m_MaxHeight - transform.m_Position.y;
            }

            // Token: 0x0400909C RID: 37020
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            // Token: 0x0400909D RID: 37021
            [ReadOnly]
            public ComponentTypeHandle<SpawnableBuildingData> m_SpawnableBuildingType;

            // Token: 0x0400909E RID: 37022
            [ReadOnly]
            public ComponentTypeHandle<BuildingData> m_BuildingType;

            // Token: 0x0400909F RID: 37023
            [ReadOnly]
            public ComponentTypeHandle<BuildingPropertyData> m_BuildingPropertyType;

            // Token: 0x040090A0 RID: 37024
            [ReadOnly]
            public ComponentTypeHandle<ObjectGeometryData> m_ObjectGeometryType;

            // Token: 0x040090A1 RID: 37025
            [ReadOnly]
            public SharedComponentTypeHandle<BuildingSpawnGroupData> m_BuildingSpawnGroupType;

            // Token: 0x040090A2 RID: 37026
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> m_TransformData;

            // Token: 0x040090A3 RID: 37027
            [ReadOnly]
            public ComponentLookup<Block> m_BlockData;

            // Token: 0x040090A4 RID: 37028
            [ReadOnly]
            public ComponentLookup<ValidArea> m_ValidAreaData;

            // Token: 0x040090A5 RID: 37029
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_Prefabs;

            // Token: 0x040090A6 RID: 37030
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildings;

            // Token: 0x040090A7 RID: 37031
            [ReadOnly]
            public ComponentLookup<BuildingData> m_Buildings;

            // Token: 0x040090A8 RID: 37032
            [ReadOnly]
            public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;

            // Token: 0x040090A9 RID: 37033
            [ReadOnly]
            public ComponentLookup<OfficeBuilding> m_OfficeBuilding;

            // Token: 0x040090AA RID: 37034
            [ReadOnly]
            public ComponentLookup<ZoneData> m_ZoneData;

            // Token: 0x040090AB RID: 37035
            [ReadOnly]
            public BufferLookup<Cell> m_Cells;

            // Token: 0x040090AC RID: 37036
            [ReadOnly]
            public BuildingConfigurationData m_BuildingConfigurationData;

            // Token: 0x040090AD RID: 37037
            [ReadOnly]
            public NativeList<ArchetypeChunk> m_SpawnableBuildingChunks;

            // Token: 0x040090AE RID: 37038
            [ReadOnly]
            public NativeQuadTree<Entity, Bounds2> m_ZoneSearchTree;

            // Token: 0x040090AF RID: 37039
            [ReadOnly]
            public RandomSeed m_RandomSeed;

            // Token: 0x040090B0 RID: 37040
            public IconCommandBuffer m_IconCommandBuffer;

            // Token: 0x040090B1 RID: 37041
            public NativeQueue<Entity> m_LevelupQueue;

            // Token: 0x040090B2 RID: 37042
            public EntityCommandBuffer m_CommandBuffer;

            // Token: 0x040090B3 RID: 37043
            public NativeQueue<TriggerAction> m_TriggerBuffer;

            // Token: 0x040090B4 RID: 37044
            public NativeQueue<ZoneBuiltLevelUpdate> m_ZoneBuiltLevelQueue;

            // Token: 0x0200134E RID: 4942
            private struct Iterator : INativeQuadTreeIterator<Entity, Bounds2>, IUnsafeQuadTreeIterator<Entity, Bounds2>
            {
                // Token: 0x060055BC RID: 21948 RVA: 0x0008E690 File Offset: 0x0008C890
                public bool Intersect(Bounds2 bounds)
                {
                    return MathUtils.Intersect(bounds, this.m_Bounds);
                }

                // Token: 0x060055BD RID: 21949 RVA: 0x003B44B0 File Offset: 0x003B26B0
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

                // Token: 0x040090B5 RID: 37045
                public Bounds2 m_Bounds;

                // Token: 0x040090B6 RID: 37046
                public int2 m_LotSize;

                // Token: 0x040090B7 RID: 37047
                public float2 m_StartPosition;

                // Token: 0x040090B8 RID: 37048
                public float2 m_Right;

                // Token: 0x040090B9 RID: 37049
                public float2 m_Forward;

                // Token: 0x040090BA RID: 37050
                public int m_MaxHeight;

                // Token: 0x040090BB RID: 37051
                public ComponentLookup<Block> m_BlockData;

                // Token: 0x040090BC RID: 37052
                public ComponentLookup<ValidArea> m_ValidAreaData;

                // Token: 0x040090BD RID: 37053
                public BufferLookup<Cell> m_Cells;
            }
        }

        // Token: 0x0200134F RID: 4943
        [BurstCompile]
        private struct LandlordMoneyJob : IJob
        {
            // Token: 0x060055BE RID: 21950 RVA: 0x003B461C File Offset: 0x003B281C
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

            // Token: 0x040090BE RID: 37054
            public BufferLookup<Game.Economy.Resources> m_Resources;

            // Token: 0x040090BF RID: 37055
            public Entity m_LandlordEntity;

            // Token: 0x040090C0 RID: 37056
            public NativeQueue<int> m_PaymentQueue;
        }

        // Token: 0x02001350 RID: 4944
        [BurstCompile]
        private struct ReturnRentJob : IJobChunk
        {
            // Token: 0x060055BF RID: 21951 RVA: 0x003B466C File Offset: 0x003B286C
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

            // Token: 0x060055C0 RID: 21952 RVA: 0x0008E69E File Offset: 0x0008C89E
            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                this.Execute(chunk, unfilteredChunkIndex, useEnabledMask, chunkEnabledMask);
            }

            // Token: 0x040090C1 RID: 37057
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            // Token: 0x040090C2 RID: 37058
            [ReadOnly]
            public BufferTypeHandle<HouseholdCitizen> m_HouseholdCitizenType;

            // Token: 0x040090C3 RID: 37059
            [ReadOnly]
            public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

            // Token: 0x040090C4 RID: 37060
            [NativeDisableParallelForRestriction]
            public BufferLookup<Game.Economy.Resources> m_Resources;

            // Token: 0x040090C5 RID: 37061
            [ReadOnly]
            public EconomyParameterData m_EconomyParameters;

            // Token: 0x040090C6 RID: 37062
            [ReadOnly]
            public ComponentLookup<Citizen> m_Citizens;

            // Token: 0x040090C7 RID: 37063
            [ReadOnly]
            public BufferLookup<CityStatistic> m_Statistics;

            // Token: 0x040090C8 RID: 37064
            [ReadOnly]
            public NativeParallelHashMap<CityStatisticsSystem.StatisticsKey, Entity> m_StatisticsLookup;

            // Token: 0x040090C9 RID: 37065
            public uint m_UpdateFrameIndex;

            // Token: 0x040090CA RID: 37066
            public Entity m_LandlordEntity;

            // Token: 0x040090CB RID: 37067
            public NativeQueue<int>.ParallelWriter m_PaymentQueue;
        }

        // Token: 0x02001351 RID: 4945
        [BurstCompile]
        private struct PayRentJob : IJobChunk
        {
            // Token: 0x060055C1 RID: 21953 RVA: 0x003B491C File Offset: 0x003B2B1C
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
                                Entity property = propertyRenter.m_Property;
                                int num3;
                                num3 = MathUtils.RoundToIntRandom(ref random, (float)propertyRenter.m_Rent * 1f / (float)CustomPropertyRenterSystem.kUpdatesPerDay);
                                num2 += num3;
                                EconomyUtils.AddResources(Resource.Money, -num3, this.m_Resources[renter]);
                                if (this.m_Attached.HasComponent(property))
                                {
                                    num3 = num3 * 10 * (int)spawnableBuildingData.m_Level;
                                }
                                if (nativeArray3.Length > 0)
                                {
                                    BuildingCondition buildingCondition = nativeArray3[i];
                                    if (this.m_DebugFastLeveling)
                                    {
                                        buildingCondition.m_Condition = levelingCost;
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

            // Token: 0x060055C2 RID: 21954 RVA: 0x0008E6AB File Offset: 0x0008C8AB
            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                this.Execute(chunk, unfilteredChunkIndex, useEnabledMask, chunkEnabledMask);
            }

            // Token: 0x040090CC RID: 37068
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            [ReadOnly]
            public ComponentLookup<Attached> m_Attached;

            // Token: 0x040090CD RID: 37069
            public ComponentTypeHandle<BuildingCondition> m_ConditionType;

            // Token: 0x040090CE RID: 37070
            public BufferTypeHandle<Renter> m_RenterType;

            // Token: 0x040090CF RID: 37071
            [ReadOnly]
            public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

            // Token: 0x040090D0 RID: 37072
            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> m_PrefabType;

            // Token: 0x040090D1 RID: 37073
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingData;

            // Token: 0x040090D2 RID: 37074
            [ReadOnly]
            public ComponentLookup<ZoneData> m_ZoneData;

            // Token: 0x040090D3 RID: 37075
            [ReadOnly]
            public ComponentLookup<PropertyRenter> m_PropertyRenters;

            // Token: 0x040090D4 RID: 37076
            [NativeDisableParallelForRestriction]
            public BufferLookup<Game.Economy.Resources> m_Resources;

            // Token: 0x040090D5 RID: 37077
            [ReadOnly]
            public ComponentLookup<BuildingPropertyData> m_BuildingProperties;

            // Token: 0x040090D6 RID: 37078
            [ReadOnly]
            public ComponentLookup<PropertyOnMarket> m_PropertiesOnMarket;

            // Token: 0x040090D7 RID: 37079
            [ReadOnly]
            public ComponentLookup<Abandoned> m_Abandoned;

            // Token: 0x040090D8 RID: 37080
            [ReadOnly]
            public ComponentLookup<Destroyed> m_Destroyed;

            // Token: 0x040090D9 RID: 37081
            [ReadOnly]
            public ComponentLookup<Game.Companies.StorageCompany> m_Storages;

            // Token: 0x040090DA RID: 37082
            [ReadOnly]
            public BufferLookup<CityModifier> m_CityEffects;

            // Token: 0x040090DB RID: 37083
            [ReadOnly]
            public ComponentLookup<SignatureBuildingData> m_SignatureDatas;

            // Token: 0x040090DC RID: 37084
            public RandomSeed m_RandomSeed;

            // Token: 0x040090DD RID: 37085
            public Entity m_City;

            // Token: 0x040090DE RID: 37086
            public NativeQueue<Entity>.ParallelWriter m_LevelupQueue;

            // Token: 0x040090DF RID: 37087
            public NativeQueue<Entity>.ParallelWriter m_LevelDownQueue;

            // Token: 0x040090E0 RID: 37088
            public NativeQueue<int>.ParallelWriter m_LandlordQueue;

            // Token: 0x040090E1 RID: 37089
            public uint m_UpdateFrameIndex;

            // Token: 0x040090E2 RID: 37090
            public bool m_DebugFastLeveling;

            // Token: 0x040090E3 RID: 37091
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
        }

        // Token: 0x02001352 RID: 4946
        [BurstCompile]
        private struct RenterMovingAwayJob : IJobChunk
        {
            // Token: 0x060055C3 RID: 21955 RVA: 0x003B4D2C File Offset: 0x003B2F2C
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

            // Token: 0x060055C4 RID: 21956 RVA: 0x0008E6B8 File Offset: 0x0008C8B8
            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                this.Execute(chunk, unfilteredChunkIndex, useEnabledMask, chunkEnabledMask);
            }

            // Token: 0x040090E4 RID: 37092
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            // Token: 0x040090E5 RID: 37093
            [ReadOnly]
            public ComponentLookup<PropertyRenter> m_PropertyRenters;

            // Token: 0x040090E6 RID: 37094
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
        }

        // Token: 0x02001353 RID: 4947
        private struct TypeHandle
        {
            // Token: 0x060055C5 RID: 21957 RVA: 0x003B4DAC File Offset: 0x003B2FAC
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                this.__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(true);
                this.__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(true);
                this.__Game_Buildings_Renter_RW_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(false);
                this.__Game_Buildings_BuildingCondition_RW_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingCondition>(false);
                this.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(true);
                this.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(true);
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

            // Token: 0x040090E7 RID: 37095
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

            [ReadOnly]
            public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

            // Token: 0x040090E8 RID: 37096
            [ReadOnly]
            public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

            // Token: 0x040090E9 RID: 37097
            public BufferTypeHandle<Renter> __Game_Buildings_Renter_RW_BufferTypeHandle;

            // Token: 0x040090EA RID: 37098
            public ComponentTypeHandle<BuildingCondition> __Game_Buildings_BuildingCondition_RW_ComponentTypeHandle;

            // Token: 0x040090EB RID: 37099
            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

            // Token: 0x040090EC RID: 37100
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

            // Token: 0x040090ED RID: 37101
            [ReadOnly]
            public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;

            // Token: 0x040090EE RID: 37102
            public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RW_BufferLookup;

            // Token: 0x040090EF RID: 37103
            [ReadOnly]
            public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

            // Token: 0x040090F0 RID: 37104
            [ReadOnly]
            public ComponentLookup<PropertyOnMarket> __Game_Buildings_PropertyOnMarket_RO_ComponentLookup;

            // Token: 0x040090F1 RID: 37105
            [ReadOnly]
            public ComponentLookup<Abandoned> __Game_Buildings_Abandoned_RO_ComponentLookup;

            // Token: 0x040090F2 RID: 37106
            [ReadOnly]
            public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

            // Token: 0x040090F3 RID: 37107
            [ReadOnly]
            public ComponentLookup<Game.Companies.StorageCompany> __Game_Companies_StorageCompany_RO_ComponentLookup;

            // Token: 0x040090F4 RID: 37108
            [ReadOnly]
            public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

            // Token: 0x040090F5 RID: 37109
            [ReadOnly]
            public ComponentLookup<SignatureBuildingData> __Game_Prefabs_SignatureBuildingData_RO_ComponentLookup;

            // Token: 0x040090F6 RID: 37110
            [ReadOnly]
            public BufferTypeHandle<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle;

            // Token: 0x040090F7 RID: 37111
            [ReadOnly]
            public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

            // Token: 0x040090F8 RID: 37112
            [ReadOnly]
            public BufferLookup<CityStatistic> __Game_City_CityStatistic_RO_BufferLookup;

            // Token: 0x040090F9 RID: 37113
            [ReadOnly]
            public ComponentTypeHandle<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle;

            // Token: 0x040090FA RID: 37114
            [ReadOnly]
            public ComponentTypeHandle<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentTypeHandle;

            // Token: 0x040090FB RID: 37115
            [ReadOnly]
            public ComponentTypeHandle<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle;

            // Token: 0x040090FC RID: 37116
            [ReadOnly]
            public ComponentTypeHandle<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentTypeHandle;

            // Token: 0x040090FD RID: 37117
            public SharedComponentTypeHandle<BuildingSpawnGroupData> __Game_Prefabs_BuildingSpawnGroupData_SharedComponentTypeHandle;

            // Token: 0x040090FE RID: 37118
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

            // Token: 0x040090FF RID: 37119
            [ReadOnly]
            public ComponentLookup<Block> __Game_Zones_Block_RO_ComponentLookup;

            // Token: 0x04009100 RID: 37120
            [ReadOnly]
            public ComponentLookup<ValidArea> __Game_Zones_ValidArea_RO_ComponentLookup;

            // Token: 0x04009101 RID: 37121
            [ReadOnly]
            public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

            // Token: 0x04009102 RID: 37122
            [ReadOnly]
            public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

            // Token: 0x04009103 RID: 37123
            [ReadOnly]
            public ComponentLookup<OfficeBuilding> __Game_Prefabs_OfficeBuilding_RO_ComponentLookup;

            // Token: 0x04009104 RID: 37124
            [ReadOnly]
            public BufferLookup<Cell> __Game_Zones_Cell_RO_BufferLookup;

            // Token: 0x04009105 RID: 37125
            public ComponentLookup<Building> __Game_Buildings_Building_RW_ComponentLookup;

            // Token: 0x04009106 RID: 37126
            [ReadOnly]
            public ComponentLookup<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RO_ComponentLookup;

            // Token: 0x04009107 RID: 37127
            [ReadOnly]
            public ComponentLookup<GarbageProducer> __Game_Buildings_GarbageProducer_RO_ComponentLookup;

            // Token: 0x04009108 RID: 37128
            [ReadOnly]
            public ComponentLookup<GroundPolluter> __Game_Buildings_GroundPolluter_RO_ComponentLookup;

            // Token: 0x04009109 RID: 37129
            [ReadOnly]
            public ComponentLookup<MailProducer> __Game_Buildings_MailProducer_RO_ComponentLookup;

            // Token: 0x0400910A RID: 37130
            [ReadOnly]
            public ComponentLookup<WaterConsumer> __Game_Buildings_WaterConsumer_RO_ComponentLookup;

            // Token: 0x0400910B RID: 37131
            public ComponentLookup<CrimeProducer> __Game_Buildings_CrimeProducer_RW_ComponentLookup;

            // Token: 0x0400910C RID: 37132
            public BufferLookup<Renter> __Game_Buildings_Renter_RW_BufferLookup;
        }
    }
}
