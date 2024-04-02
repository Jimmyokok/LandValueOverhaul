using System;
using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Common;
using Game.Economy;
using Game.Net;
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
    // Token: 0x0200131E RID: 4894
    public partial class BuildingUpkeepSystem : GameSystemBase
    {
        // Token: 0x0600569E RID: 22174 RVA: 0x0032901F File Offset: 0x0032721F
        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 262144 / (BuildingUpkeepSystem.kUpdatesPerDay * 16);
        }

        // Token: 0x0600569F RID: 22175 RVA: 0x0032902F File Offset: 0x0032722F
        public static float GetHeatingMultiplier(float temperature)
        {
            return math.max(0f, 15f - temperature);
        }

        // Token: 0x060056A0 RID: 22176 RVA: 0x00329044 File Offset: 0x00327244
        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
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
            Mod.log.Info($"Modded BuildingUpkeepSystem created!");
        }

        // Token: 0x060056A1 RID: 22177 RVA: 0x00329151 File Offset: 0x00327351
        [Preserve]
        protected override void OnDestroy()
        {
            base.OnDestroy();
            this.m_ExpenseQueue.Dispose();
        }

        // Token: 0x060056A2 RID: 22178 RVA: 0x00329164 File Offset: 0x00327364
        [Preserve]
        protected override void OnUpdate()
        {
            uint updateFrame = SimulationUtils.GetUpdateFrame(this.m_SimulationSystem.frameIndex, BuildingUpkeepSystem.kUpdatesPerDay, 16);
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
            BuildingUpkeepSystem.BuildingUpkeepJob buildingUpkeepJob = default(BuildingUpkeepSystem.BuildingUpkeepJob);
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
            buildingUpkeepJob.m_TemperatureUpkeep = BuildingUpkeepSystem.GetHeatingMultiplier(this.m_ClimateSystem.temperature);
            BuildingUpkeepSystem.BuildingUpkeepJob jobData = buildingUpkeepJob;
            base.Dependency = jobData.ScheduleParallel(this.m_BuildingGroup, base.Dependency);
            this.m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
            this.m_ResourceSystem.AddPrefabsReader(base.Dependency);
            this.__TypeHandle.__Game_Economy_Resources_RW_BufferLookup.Update(ref base.CheckedStateRef);
            BuildingUpkeepSystem.LandlordUpkeepJob landlordUpkeepJob = default(BuildingUpkeepSystem.LandlordUpkeepJob);
            landlordUpkeepJob.m_Resources = this.__TypeHandle.__Game_Economy_Resources_RW_BufferLookup;
            landlordUpkeepJob.m_Landlords = this.m_PropertyRenterSystem.Landlords;
            landlordUpkeepJob.m_Queue = this.m_ExpenseQueue;
            BuildingUpkeepSystem.LandlordUpkeepJob jobData2 = landlordUpkeepJob;
            base.Dependency = jobData2.Schedule(base.Dependency);
        }

        // Token: 0x060056A3 RID: 22179 RVA: 0x00003211 File Offset: 0x00001411
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
        }

        // Token: 0x060056A4 RID: 22180 RVA: 0x00329486 File Offset: 0x00327686
        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            this.__AssignQueries(ref base.CheckedStateRef);
            this.__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
        }

        // Token: 0x060056A5 RID: 22181 RVA: 0x00006D67 File Offset: 0x00004F67
        [Preserve]
        public BuildingUpkeepSystem()
        {
        }

        // Token: 0x040089CA RID: 35274
        public static readonly int kUpdatesPerDay = 16;

        // Token: 0x040089CB RID: 35275
        public static readonly int kMaterialUpkeep = 4;

        // Token: 0x040089CC RID: 35276
        private SimulationSystem m_SimulationSystem;

        // Token: 0x040089CD RID: 35277
        private EndFrameBarrier m_EndFrameBarrier;

        // Token: 0x040089CE RID: 35278
        private PropertyRenterSystem m_PropertyRenterSystem;

        // Token: 0x040089CF RID: 35279
        private ResourceSystem m_ResourceSystem;

        // Token: 0x040089D0 RID: 35280
        private ClimateSystem m_ClimateSystem;

        // Token: 0x040089D1 RID: 35281
        private NativeQueue<int> m_ExpenseQueue;

        // Token: 0x040089D2 RID: 35282
        private EntityQuery m_BuildingGroup;

        // Token: 0x040089D3 RID: 35283
        private BuildingUpkeepSystem.TypeHandle __TypeHandle;

        // Token: 0x0200131F RID: 4895
        private struct LandlordUpkeepJob : IJob
        {
            // Token: 0x060056A7 RID: 22183 RVA: 0x003294BC File Offset: 0x003276BC
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

            // Token: 0x040089D4 RID: 35284
            public BufferLookup<Game.Economy.Resources> m_Resources;

            // Token: 0x040089D5 RID: 35285
            public Entity m_Landlords;

            // Token: 0x040089D6 RID: 35286
            public NativeQueue<int> m_Queue;
        }

        // Token: 0x02001320 RID: 4896
        private struct BuildingUpkeepJob : IJobChunk
        {
            // Token: 0x060056A8 RID: 22184 RVA: 0x00329510 File Offset: 0x00327710
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
                    int num2 = this.m_ConsumptionDatas[prefab].m_Upkeep / BuildingUpkeepSystem.kUpdatesPerDay;
                    if (buildingCondition.m_Condition < 0)
                    {
                        AreaType type = AreaType.None;
                        if (this.m_SpawnableBuildingData.HasComponent(prefab))
                        {
                            SpawnableBuildingData spawnableBuildingData = this.m_SpawnableBuildingData[prefab];
                            type = this.m_ZoneData[spawnableBuildingData.m_ZonePrefab].m_AreaType;
                        }
                    }
                    buildingCondition.m_Condition -= num2;
                    nativeArray3[i] = buildingCondition;
                    int num3 = num2 / BuildingUpkeepSystem.kMaterialUpkeep;
                    num += num2 - num3;
                    Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)(1L + (long)entity.Index * (long)((ulong)this.m_SimulationFrame)));
                    Resource resource = random.NextBool() ? Resource.Timber : Resource.Concrete;
                    float price = this.m_ResourceDatas[this.m_ResourcePrefabs[resource]].m_Price;
                    float num4 = math.sqrt((float)(buildingData.m_LotSize.x * buildingData.m_LotSize.y * buildingPropertyData.CountProperties())) * this.m_TemperatureUpkeep / (float)BuildingUpkeepSystem.kUpdatesPerDay;
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

            // Token: 0x060056A9 RID: 22185 RVA: 0x003298B7 File Offset: 0x00327AB7
            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                this.Execute(chunk, unfilteredChunkIndex, useEnabledMask, chunkEnabledMask);
            }

            // Token: 0x040089D7 RID: 35287
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            // Token: 0x040089D8 RID: 35288
            public ComponentTypeHandle<BuildingCondition> m_ConditionType;

            // Token: 0x040089D9 RID: 35289
            [ReadOnly]
            public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

            // Token: 0x040089DA RID: 35290
            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> m_PrefabType;

            // Token: 0x040089DB RID: 35291
            [ReadOnly]
            public ComponentTypeHandle<Building> m_BuildingType;

            // Token: 0x040089DC RID: 35292
            [ReadOnly]
            public ResourcePrefabs m_ResourcePrefabs;

            // Token: 0x040089DD RID: 35293
            [ReadOnly]
            public ComponentLookup<ResourceData> m_ResourceDatas;

            // Token: 0x040089DE RID: 35294
            [ReadOnly]
            public ComponentLookup<BuildingData> m_BuildingDatas;

            // Token: 0x040089DF RID: 35295
            [ReadOnly]
            public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;

            // Token: 0x040089E0 RID: 35296
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingData;

            // Token: 0x040089E1 RID: 35297
            [ReadOnly]
            public ComponentLookup<ZoneData> m_ZoneData;

            // Token: 0x040089E2 RID: 35298
            [ReadOnly]
            public ComponentLookup<ConsumptionData> m_ConsumptionDatas;

            // Token: 0x040089E3 RID: 35299
            [ReadOnly]
            public BufferLookup<ResourceAvailability> m_Availabilities;

            // Token: 0x040089E4 RID: 35300
            public uint m_UpdateFrameIndex;

            // Token: 0x040089E5 RID: 35301
            public uint m_SimulationFrame;

            // Token: 0x040089E6 RID: 35302
            public float m_TemperatureUpkeep;

            // Token: 0x040089E7 RID: 35303
            public NativeQueue<int>.ParallelWriter m_LandlordExpenseQueue;

            // Token: 0x040089E8 RID: 35304
            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
        }

        // Token: 0x02001321 RID: 4897
        private struct TypeHandle
        {
            // Token: 0x060056AA RID: 22186 RVA: 0x003298C4 File Offset: 0x00327AC4
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

            // Token: 0x040089E9 RID: 35305
            public ComponentTypeHandle<BuildingCondition> __Game_Buildings_BuildingCondition_RW_ComponentTypeHandle;

            // Token: 0x040089EA RID: 35306
            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

            // Token: 0x040089EB RID: 35307
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

            // Token: 0x040089EC RID: 35308
            [ReadOnly]
            public ComponentTypeHandle<Building> __Game_Buildings_Building_RO_ComponentTypeHandle;

            // Token: 0x040089ED RID: 35309
            public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

            // Token: 0x040089EE RID: 35310
            [ReadOnly]
            public ComponentLookup<ConsumptionData> __Game_Prefabs_ConsumptionData_RO_ComponentLookup;

            // Token: 0x040089EF RID: 35311
            [ReadOnly]
            public BufferLookup<ResourceAvailability> __Game_Net_ResourceAvailability_RO_BufferLookup;

            // Token: 0x040089F0 RID: 35312
            [ReadOnly]
            public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

            // Token: 0x040089F1 RID: 35313
            [ReadOnly]
            public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

            // Token: 0x040089F2 RID: 35314
            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

            // Token: 0x040089F3 RID: 35315
            [ReadOnly]
            public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;

            // Token: 0x040089F4 RID: 35316
            [ReadOnly]
            public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

            // Token: 0x040089F5 RID: 35317
            public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RW_BufferLookup;
        }
    }
}
