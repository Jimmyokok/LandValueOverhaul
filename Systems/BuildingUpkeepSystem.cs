using System;
using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Common;
using Game.Economy;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Game.Zones;
using Game;
using Game.Simulation;
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
    // Token: 0x02001237 RID: 4663
    [CompilerGenerated]
    public class CustomBuildingUpkeepSystem : GameSystemBase
    {
        // Token: 0x06005117 RID: 20759 RVA: 0x0030E7AF File Offset: 0x0030C9AF
        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 262144 / (CustomBuildingUpkeepSystem.kUpdatesPerDay * 16);
        }

        // Token: 0x06005118 RID: 20760 RVA: 0x0030E7BF File Offset: 0x0030C9BF
        public static float GetHeatingMultiplier(float temperature)
        {
            return math.max(0f, 15f - temperature);
        }

        // Token: 0x06005119 RID: 20761 RVA: 0x0030E7D4 File Offset: 0x0030C9D4
        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            logger.LogInfo("Building material upkeep mechanism altered!");
            this.m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
            this.m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
            this.m_PropertyRenterSystem = base.World.GetOrCreateSystemManaged<PropertyRenterSystem>();
            this.m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
            this.m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
            this.m_ExpenseQueue = new NativeQueue<int>(Allocator.Persistent);
            this.m_BuildingGroup = base.GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<BuildingCondition>(),
                        ComponentType.ReadOnly<PrefabRef>(),
                        ComponentType.ReadOnly<UpdateFrame>()
                    },
                    Any = new ComponentType[0],
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Abandoned>(),
                        ComponentType.ReadOnly<Destroyed>(),
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>()
                    }
                }
            });
        }

        // Token: 0x0600511A RID: 20762 RVA: 0x0030E8E1 File Offset: 0x0030CAE1
        [Preserve]
        protected override void OnDestroy()
        {
            base.OnDestroy();
            this.m_ExpenseQueue.Dispose();
        }

        // Token: 0x0600511B RID: 20763 RVA: 0x0030E8F4 File Offset: 0x0030CAF4
        [Preserve]
        protected override void OnUpdate()
        {
            uint updateFrame = SimulationUtils.GetUpdateFrame(this.m_SimulationSystem.frameIndex, CustomBuildingUpkeepSystem.kUpdatesPerDay, 16);
            this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Net_ResourceAvailability_RO_BufferLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ConsumptionData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Building_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_BuildingCondition_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            CustomBuildingUpkeepSystem.BuildingUpkeepJob buildingUpkeepJob = default(CustomBuildingUpkeepSystem.BuildingUpkeepJob);
            buildingUpkeepJob.m_ConditionType = this.__TypeHandle.__Game_Buildings_BuildingCondition_RW_ComponentTypeHandle;
            buildingUpkeepJob.m_PrefabType = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
            buildingUpkeepJob.m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle;
            buildingUpkeepJob.m_BuildingType = this.__TypeHandle.__Game_Buildings_Building_RO_ComponentTypeHandle;
            buildingUpkeepJob.m_UpdateFrameType = this.__TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle;
            buildingUpkeepJob.m_ConsumptionDatas = this.__TypeHandle.__Game_Prefabs_ConsumptionData_RO_ComponentLookup;
            buildingUpkeepJob.m_Availabilities = this.__TypeHandle.__Game_Net_ResourceAvailability_RO_BufferLookup;
            buildingUpkeepJob.m_BuildingDatas = this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup;
            buildingUpkeepJob.m_BuildingPropertyDatas = this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;
            buildingUpkeepJob.m_SpawnableBuildingData = this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;
            buildingUpkeepJob.m_ZoneData = this.__TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup;
            buildingUpkeepJob.m_ResourcePrefabs = this.m_ResourceSystem.GetPrefabs();
            buildingUpkeepJob.m_ResourceDatas = this.__TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup;
            buildingUpkeepJob.m_UpdateFrameIndex = updateFrame;
            buildingUpkeepJob.m_SimulationFrame = this.m_SimulationSystem.frameIndex;
            buildingUpkeepJob.m_CommandBuffer = this.m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter();
            buildingUpkeepJob.m_LandlordExpenseQueue = this.m_ExpenseQueue.AsParallelWriter();
            buildingUpkeepJob.m_TemperatureUpkeep = CustomBuildingUpkeepSystem.GetHeatingMultiplier(this.m_ClimateSystem.temperature);
            CustomBuildingUpkeepSystem.BuildingUpkeepJob jobData = buildingUpkeepJob;
            base.Dependency = jobData.ScheduleParallel(this.m_BuildingGroup, base.Dependency);
            this.m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
            this.m_ResourceSystem.AddPrefabsReader(base.Dependency);
            this.__TypeHandle.__Game_Economy_Resources_RW_BufferLookup.Update(ref base.CheckedStateRef);
            CustomBuildingUpkeepSystem.LandlordUpkeepJob landlordUpkeepJob = default(CustomBuildingUpkeepSystem.LandlordUpkeepJob);
            landlordUpkeepJob.m_Resources = this.__TypeHandle.__Game_Economy_Resources_RW_BufferLookup;
            landlordUpkeepJob.m_Landlords = this.m_PropertyRenterSystem.Landlords;
            landlordUpkeepJob.m_Queue = this.m_ExpenseQueue;
            CustomBuildingUpkeepSystem.LandlordUpkeepJob jobData2 = landlordUpkeepJob;
            base.Dependency = jobData2.Schedule(base.Dependency);
        }

        // Token: 0x0600511C RID: 20764 RVA: 0x00002E1D File Offset: 0x0000101D
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
        }

        // Token: 0x0600511D RID: 20765 RVA: 0x0030EC16 File Offset: 0x0030CE16
        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            this.__AssignQueries(ref base.CheckedStateRef);
            this.__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
        }

        // Token: 0x0600511E RID: 20766 RVA: 0x00006953 File Offset: 0x00004B53
        [Preserve]
        public CustomBuildingUpkeepSystem()
        {
        }

        // Token: 0x04008570 RID: 34160
        public static readonly int kUpdatesPerDay = 16;

        // Token: 0x04008571 RID: 34161
        public static readonly int kMaterialUpkeep = 4;

        // Token: 0x04008572 RID: 34162
        private SimulationSystem m_SimulationSystem;

        // Token: 0x04008573 RID: 34163
        private EndFrameBarrier m_EndFrameBarrier;

        // Token: 0x04008574 RID: 34164
        private PropertyRenterSystem m_PropertyRenterSystem;

        // Token: 0x04008575 RID: 34165
        private ResourceSystem m_ResourceSystem;

        // Token: 0x04008576 RID: 34166
        private ClimateSystem m_ClimateSystem;

        // Token: 0x04008577 RID: 34167
        private NativeQueue<int> m_ExpenseQueue;

        // Token: 0x04008578 RID: 34168
        private EntityQuery m_BuildingGroup;

        // Token: 0x04008579 RID: 34169
        private CustomBuildingUpkeepSystem.TypeHandle __TypeHandle;

        private static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(MyPluginInfo.PLUGIN_GUID);


        // Token: 0x02001238 RID: 4664
        [BurstCompile]
        private struct LandlordUpkeepJob : IJob
        {
            // Token: 0x06005120 RID: 20768 RVA: 0x0030EC4C File Offset: 0x0030CE4C
            public void Execute()
            {
                if (this.m_Resources.HasBuffer(this.m_Landlords))
                {
                    int num = 0;
                    int num2;
                    while (this.m_Queue.TryDequeue(out num2))
                    {
                        num += num2;
                    }
                    EconomyUtils.AddResources(Resource.Money, num, this.m_Resources[this.m_Landlords]);
                }
            }

            // Token: 0x0400857A RID: 34170
            public BufferLookup<Game.Economy.Resources> m_Resources;

            // Token: 0x0400857B RID: 34171
            public Entity m_Landlords;

            // Token: 0x0400857C RID: 34172
            public NativeQueue<int> m_Queue;
        }

        // Token: 0x02001239 RID: 4665
        [BurstCompile]
        private struct BuildingUpkeepJob : IJobChunk
        {
            // Token: 0x06005121 RID: 20769 RVA: 0x0030ECA0 File Offset: 0x0030CEA0
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (chunk.GetSharedComponent<UpdateFrame>(this.m_UpdateFrameType).m_Index != this.m_UpdateFrameIndex)
                {
                    return;
                }
                NativeArray<Entity> nativeArray = chunk.GetNativeArray(this.m_EntityType);
                NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray<PrefabRef>(ref this.m_PrefabType);
                NativeArray<BuildingCondition> nativeArray3 = chunk.GetNativeArray<BuildingCondition>(ref this.m_ConditionType);
                NativeArray<Building> nativeArray4 = chunk.GetNativeArray<Building>(ref this.m_BuildingType);
                int num = 0;
                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity entity = nativeArray[i];
                    Entity prefab = nativeArray2[i].m_Prefab;
                    BuildingData buildingData = this.m_BuildingDatas[prefab];
                    BuildingPropertyData buildingPropertyData = this.m_BuildingPropertyDatas[prefab];
                    BuildingCondition buildingCondition = nativeArray3[i];
                    int num2 = this.m_ConsumptionDatas[prefab].m_Upkeep / CustomBuildingUpkeepSystem.kUpdatesPerDay;
                    if (buildingCondition.m_Condition < 0)
                    {
                        AreaType type = AreaType.None;
                        if (this.m_SpawnableBuildingData.HasComponent(prefab))
                        {
                            SpawnableBuildingData spawnableBuildingData = this.m_SpawnableBuildingData[prefab];
                            type = this.m_ZoneData[spawnableBuildingData.m_ZonePrefab].m_AreaType;
                        }
                        num2 = Mathf.RoundToInt((float)num2 / PropertyRenterSystem.GetUpkeepExponent(type));
                    }
                    buildingCondition.m_Condition -= num2;
                    nativeArray3[i] = buildingCondition;
                    int num3 = num2 / CustomBuildingUpkeepSystem.kMaterialUpkeep;
                    num += num2 - num3;
                    Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)(1L + (long)entity.Index * (long)((ulong)this.m_SimulationFrame)));
                    Resource resource = random.NextBool() ? Resource.Timber : Resource.Concrete;
                    float price = this.m_ResourceDatas[this.m_ResourcePrefabs[resource]].m_Price;
                    float num4 = math.sqrt((float)(buildingData.m_LotSize.x * buildingData.m_LotSize.y * buildingPropertyData.CountProperties())) * this.m_TemperatureUpkeep / (float)CustomBuildingUpkeepSystem.kUpdatesPerDay;
                    if (random.NextInt(Mathf.RoundToInt(4000f * price)) < num3)
                    {
                        Entity e = this.m_CommandBuffer.CreateEntity(unfilteredChunkIndex);
                        this.m_CommandBuffer.AddComponent<GoodsDeliveryRequest>(unfilteredChunkIndex, e, new GoodsDeliveryRequest
                        {
                            m_Amount = Math.Max(num2, 4000),
                            m_Flags = (GoodsDeliveryFlags.BuildingUpkeep | GoodsDeliveryFlags.CommercialAllowed | GoodsDeliveryFlags.IndustrialAllowed | GoodsDeliveryFlags.ImportAllowed),
                            m_Resource = resource,
                            m_Target = entity
                        });
                    }
                    Building building = nativeArray4[i];
                    if (this.m_Availabilities.HasBuffer(building.m_RoadEdge))
                    {
                        float availability = NetUtils.GetAvailability(this.m_Availabilities[building.m_RoadEdge], AvailableResource.WoodSupply, building.m_CurvePosition);
                        float availability2 = NetUtils.GetAvailability(this.m_Availabilities[building.m_RoadEdge], AvailableResource.PetrochemicalsSupply, building.m_CurvePosition);
                        float num5 = availability + availability2;
                        if (num5 < 0.001f)
                        {
                            resource = (random.NextBool() ? Resource.Wood : Resource.Petrochemicals);
                        }
                        else
                        {
                            resource = ((random.NextFloat(num5) <= availability) ? Resource.Wood : Resource.Petrochemicals);
                        }
                        num2 = ((resource == Resource.Wood) ? 4000 : 800);
                        price = this.m_ResourceDatas[this.m_ResourcePrefabs[resource]].m_Price;
                        if (random.NextFloat((float)num2 * price) < num4)
                        {
                            Entity e2 = this.m_CommandBuffer.CreateEntity(unfilteredChunkIndex);
                            int num6 = Mathf.RoundToInt((float)num2 * price);
                            this.m_CommandBuffer.AddComponent<GoodsDeliveryRequest>(unfilteredChunkIndex, e2, new GoodsDeliveryRequest
                            {
                                m_Amount = num2,
                                m_Flags = (GoodsDeliveryFlags.BuildingUpkeep | GoodsDeliveryFlags.CommercialAllowed | GoodsDeliveryFlags.IndustrialAllowed | GoodsDeliveryFlags.ImportAllowed),
                                m_Resource = resource,
                                m_Target = entity
                            });
                            num += num6;
                        }
                    }
                }
                this.m_LandlordExpenseQueue.Enqueue(-num);
            }

            // Token: 0x06005122 RID: 20770 RVA: 0x0030F047 File Offset: 0x0030D247
            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                this.Execute(chunk, unfilteredChunkIndex, useEnabledMask, chunkEnabledMask);
            }

            // Token: 0x0400857D RID: 34173
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            // Token: 0x0400857E RID: 34174
            public ComponentTypeHandle<BuildingCondition> m_ConditionType;

            // Token: 0x0400857F RID: 34175
            [ReadOnly]
            public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

            // Token: 0x04008580 RID: 34176
            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> m_PrefabType;

            // Token: 0x04008581 RID: 34177
            [ReadOnly]
            public ComponentTypeHandle<Building> m_BuildingType;

            // Token: 0x04008582 RID: 34178
            [ReadOnly]
            public ResourcePrefabs m_ResourcePrefabs;

            // Token: 0x04008583 RID: 34179
            [ReadOnly]
            public ComponentLookup<ResourceData> m_ResourceDatas;

            // Token: 0x04008584 RID: 34180
            [ReadOnly]
            public ComponentLookup<BuildingData> m_BuildingDatas;

            // Token: 0x04008585 RID: 34181
            [ReadOnly]
            public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;

            // Token: 0x04008586 RID: 34182
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingData;

            // Token: 0x04008587 RID: 34183
            [ReadOnly]
            public ComponentLookup<ZoneData> m_ZoneData;

            // Token: 0x04008588 RID: 34184
            [ReadOnly]
            public ComponentLookup<ConsumptionData> m_ConsumptionDatas;

            // Token: 0x04008589 RID: 34185
            [ReadOnly]
            public BufferLookup<ResourceAvailability> m_Availabilities;

            // Token: 0x0400858A RID: 34186
            public uint m_UpdateFrameIndex;

            // Token: 0x0400858B RID: 34187
            public uint m_SimulationFrame;

            // Token: 0x0400858C RID: 34188
            public float m_TemperatureUpkeep;

            // Token: 0x0400858D RID: 34189
            public NativeQueue<int>.ParallelWriter m_LandlordExpenseQueue;

            // Token: 0x0400858E RID: 34190
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
        }

        // Token: 0x0200123A RID: 4666
        private struct TypeHandle
        {
            // Token: 0x06005123 RID: 20771 RVA: 0x0030F054 File Offset: 0x0030D254
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Game_Buildings_BuildingCondition_RW_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingCondition>(false);
                this.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(true);
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                this.__Game_Buildings_Building_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Building>(true);
                this.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
                this.__Game_Prefabs_ConsumptionData_RO_ComponentLookup = state.GetComponentLookup<ConsumptionData>(true);
                this.__Game_Net_ResourceAvailability_RO_BufferLookup = state.GetBufferLookup<ResourceAvailability>(true);
                this.__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(true);
                this.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(true);
                this.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(true);
                this.__Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(true);
                this.__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(true);
                this.__Game_Economy_Resources_RW_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>(false);
            }

            // Token: 0x0400858F RID: 34191
            public ComponentTypeHandle<BuildingCondition> __Game_Buildings_BuildingCondition_RW_ComponentTypeHandle;

            // Token: 0x04008590 RID: 34192
            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

            // Token: 0x04008591 RID: 34193
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

            // Token: 0x04008592 RID: 34194
            [ReadOnly]
            public ComponentTypeHandle<Building> __Game_Buildings_Building_RO_ComponentTypeHandle;

            // Token: 0x04008593 RID: 34195
            public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

            // Token: 0x04008594 RID: 34196
            [ReadOnly]
            public ComponentLookup<ConsumptionData> __Game_Prefabs_ConsumptionData_RO_ComponentLookup;

            // Token: 0x04008595 RID: 34197
            [ReadOnly]
            public BufferLookup<ResourceAvailability> __Game_Net_ResourceAvailability_RO_BufferLookup;

            // Token: 0x04008596 RID: 34198
            [ReadOnly]
            public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

            // Token: 0x04008597 RID: 34199
            [ReadOnly]
            public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

            // Token: 0x04008598 RID: 34200
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

            // Token: 0x04008599 RID: 34201
            [ReadOnly]
            public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;

            // Token: 0x0400859A RID: 34202
            [ReadOnly]
            public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

            // Token: 0x0400859B RID: 34203
            public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RW_BufferLookup;
        }
    }
}
