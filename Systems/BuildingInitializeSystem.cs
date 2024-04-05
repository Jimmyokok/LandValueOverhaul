using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Logging;
using Colossal.Mathematics;
using Game.Common;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Rendering;
using Game.Simulation;
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
using Game.Prefabs;
using SubObject = Game.Prefabs.SubObject;
using SubNet = Game.Prefabs.SubNet;
using TransformerData = Game.Prefabs.TransformerData;

namespace LandValueOverhaul.Systems
{
    // Token: 0x020018CB RID: 6347
    public partial class BuildingReinitializeSystem : GameSystemBase
    {
        // Token: 0x06006C9B RID: 27803 RVA: 0x004320DC File Offset: 0x004302DC
        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
            this.m_PrefabQuery = base.GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<PrefabData>()
                    },
                    Any = new ComponentType[]
                    {
                        ComponentType.ReadWrite<BuildingData>(),
                        ComponentType.ReadWrite<BuildingExtensionData>(),
                        ComponentType.ReadWrite<ServiceUpgradeData>(),
                        ComponentType.ReadWrite<SpawnableBuildingData>()
                    }
                }
            });
            base.RequireForUpdate(this.m_PrefabQuery);
        }

        // Token: 0x06006C9C RID: 27804 RVA: 0x004321B8 File Offset: 0x004303B8
        protected override void OnUpdate()
        {
            Mod.log.Info("Building upkeep modified!");
            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback);
            NativeArray<ArchetypeChunk> chunks = this.m_PrefabQuery.ToArchetypeChunkArray(Allocator.TempJob);
            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
            EntityTypeHandle _Unity_Entities_Entity_TypeHandle = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle;
            this.__TypeHandle.__Game_Simulation_CollectedServiceBuildingBudgetData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            ComponentTypeHandle<CollectedServiceBuildingBudgetData> _Game_Simulation_CollectedServiceBuildingBudgetData_RO_ComponentTypeHandle = this.__TypeHandle.__Game_Simulation_CollectedServiceBuildingBudgetData_RO_ComponentTypeHandle;
            this.__TypeHandle.__Game_Prefabs_ServiceUpkeepData_RW_BufferTypeHandle.Update(ref base.CheckedStateRef);
            BufferTypeHandle<ServiceUpkeepData> _Game_Prefabs_ServiceUpkeepData_RW_BufferTypeHandle = this.__TypeHandle.__Game_Prefabs_ServiceUpkeepData_RW_BufferTypeHandle;
            this.__TypeHandle.__Game_Prefabs_ServiceUpgradeBuilding_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
            BufferTypeHandle<ServiceUpgradeBuilding> _Game_Prefabs_ServiceUpgradeBuilding_RO_BufferTypeHandle = this.__TypeHandle.__Game_Prefabs_ServiceUpgradeBuilding_RO_BufferTypeHandle;
            this.__TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            ComponentTypeHandle<PrefabData> _Game_Prefabs_PrefabData_RO_ComponentTypeHandle = this.__TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle;
            this.__TypeHandle.__Game_Prefabs_BuildingData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            ComponentTypeHandle<BuildingData> _Game_Prefabs_BuildingData_RW_ComponentTypeHandle = this.__TypeHandle.__Game_Prefabs_BuildingData_RW_ComponentTypeHandle;
            this.__TypeHandle.__Game_Prefabs_ConsumptionData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            ComponentTypeHandle<ConsumptionData> _Game_Prefabs_ConsumptionData_RW_ComponentTypeHandle = this.__TypeHandle.__Game_Prefabs_ConsumptionData_RW_ComponentTypeHandle;
            this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            ComponentTypeHandle<SpawnableBuildingData> _Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle = this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle;
            this.__TypeHandle.__Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            ComponentTypeHandle<SignatureBuildingData> _Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle = this.__TypeHandle.__Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle;
            this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef); 
            ComponentTypeHandle<BuildingPropertyData> _Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle = this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle;
            this.__TypeHandle.__Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            ComponentLookup<ZoneServiceConsumptionData> _Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup = this.__TypeHandle.__Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup;
            this.__TypeHandle.__Game_Prefabs_ZoneData_RW_ComponentLookup.Update(ref base.CheckedStateRef);
            ComponentLookup<ZoneData> _Game_Prefabs_ZoneData_RW_ComponentLookup = this.__TypeHandle.__Game_Prefabs_ZoneData_RW_ComponentLookup;
            base.CompleteDependency();
            for (int i = 0; i < chunks.Length; i++)
            {
                ArchetypeChunk archetypeChunk = chunks[i];
                NativeArray<PrefabData> nativeArray = archetypeChunk.GetNativeArray<PrefabData>(ref _Game_Prefabs_PrefabData_RO_ComponentTypeHandle);
                NativeArray<BuildingData> nativeArray3 = archetypeChunk.GetNativeArray<BuildingData>(ref _Game_Prefabs_BuildingData_RW_ComponentTypeHandle);
                NativeArray<ConsumptionData> nativeArray5 = archetypeChunk.GetNativeArray<ConsumptionData>(ref _Game_Prefabs_ConsumptionData_RW_ComponentTypeHandle);
                NativeArray<SpawnableBuildingData> nativeArray6 = archetypeChunk.GetNativeArray<SpawnableBuildingData>(ref _Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle);
                NativeArray<BuildingPropertyData> nativeArray9 = archetypeChunk.GetNativeArray<BuildingPropertyData>(ref _Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle);
                BufferAccessor<ServiceUpgradeBuilding> bufferAccessor = archetypeChunk.GetBufferAccessor<ServiceUpgradeBuilding>(ref _Game_Prefabs_ServiceUpgradeBuilding_RO_BufferTypeHandle);
                BufferAccessor<ServiceUpkeepData> bufferAccessor2 = archetypeChunk.GetBufferAccessor<ServiceUpkeepData>(ref _Game_Prefabs_ServiceUpkeepData_RW_BufferTypeHandle);
                NativeArray<Entity> nativeArray10 = archetypeChunk.GetNativeArray(_Unity_Entities_Entity_TypeHandle);
                bool flag = archetypeChunk.Has<CollectedServiceBuildingBudgetData>(ref _Game_Simulation_CollectedServiceBuildingBudgetData_RO_ComponentTypeHandle);
                bool flag2 = archetypeChunk.Has<SignatureBuildingData>(ref _Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle);
                if (nativeArray6.Length != 0)
                {
                    for (int m = 0; m < nativeArray6.Length; m++)
                    {
                        BuildingPrefab prefab4 = this.m_PrefabSystem.GetPrefab<BuildingPrefab>(nativeArray[m]);
                        BuildingPropertyData buildingPropertyData = (nativeArray9.Length != 0) ? nativeArray9[m] : default(BuildingPropertyData);
                        SpawnableBuildingData spawnableBuildingData = nativeArray6[m];
                        int residentialProperties = buildingPropertyData.m_ResidentialProperties;
                        if (spawnableBuildingData.m_ZonePrefab != Entity.Null)
                        {
                            Entity zonePrefab = spawnableBuildingData.m_ZonePrefab;
                            ZoneData zoneData = _Game_Prefabs_ZoneData_RW_ComponentLookup[zonePrefab];
                            int level = (int)spawnableBuildingData.m_Level;
                            BuildingData buildingData = nativeArray3[m];
                            int lotSize = buildingData.m_LotSize.x * buildingData.m_LotSize.y;
                            if (nativeArray5.Length != 0 && !prefab4.Has<ServiceConsumption>() && _Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup.HasComponent(zonePrefab))
                            {
                                ZoneServiceConsumptionData zoneServiceConsumptionData = _Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup[zonePrefab];
                                if(prefab4.name == "Residential LowRent")
                                {
                                    zoneServiceConsumptionData.m_Upkeep = 250f;
                                }
                                ConsumptionData ptr = nativeArray5[m];
                                //Mod.log.Info($"{residentialProperties}, {buildingPropertyData.CountProperties()}, {Game.Simulation.PropertyRenterSystem.GetUpkeepExponent(zoneData.m_AreaType)}");
                                if (zoneData.m_AreaType == AreaType.Residential)
                                {
                                    if (flag2)
                                    {
                                        ptr.m_Upkeep = PropertyRenterSystem.GetUpkeep(2, residentialProperties, zoneServiceConsumptionData.m_Upkeep, lotSize, zoneData.m_AreaType, false);
                                    }
                                    else
                                    {
                                        ptr.m_Upkeep = PropertyRenterSystem.GetUpkeep(level, residentialProperties, zoneServiceConsumptionData.m_Upkeep, lotSize, zoneData.m_AreaType, false);
                                    }
                                }
                                else
                                {
                                    bool isStorage = buildingPropertyData.m_AllowedStored > Resource.NoResource;
                                    if (flag2)
                                    {
                                        ptr.m_Upkeep = PropertyRenterSystem.GetUpkeep(2, residentialProperties, zoneServiceConsumptionData.m_Upkeep, lotSize, zoneData.m_AreaType, false);
                                    }
                                    else
                                    {
                                        ptr.m_Upkeep = PropertyRenterSystem.GetUpkeep(level, residentialProperties, zoneServiceConsumptionData.m_Upkeep, lotSize, zoneData.m_AreaType, isStorage);
                                    }
                                }
                                nativeArray5[m] = ptr;
                            }
                        }
                    }
                }
                if (flag)
                {
                    for (int num3 = 0; num3 < nativeArray10.Length; num3++)
                    {
                        if (nativeArray5.Length != 0 && nativeArray5[num3].m_Upkeep > 0)
                        {
                            DynamicBuffer<ServiceUpkeepData> dynamicBuffer2 = bufferAccessor2[num3];
                            for (int num4 = 0; num4 < dynamicBuffer2.Length; num4++)
                            {
                                ServiceUpkeepData value7 = dynamicBuffer2[num4];
                                value7.m_Upkeep.m_Amount = nativeArray5[num3].m_Upkeep;
                                dynamicBuffer2[num4] = value7;
                            }
                        }
                    }
                }
                if (bufferAccessor.Length != 0)
                {
                    for (int num5 = 0; num5 < bufferAccessor.Length; num5++)
                    {
                        Entity upgrade = nativeArray10[num5];
                        DynamicBuffer<ServiceUpgradeBuilding> dynamicBuffer3 = bufferAccessor[num5];
                        for (int num6 = 0; num6 < dynamicBuffer3.Length; num6++)
                        {
                            ServiceUpgradeBuilding serviceUpgradeBuilding2 = dynamicBuffer3[num6];
                            base.EntityManager.GetBuffer<BuildingUpgradeElement>(serviceUpgradeBuilding2.m_Building, false).Add(new BuildingUpgradeElement(upgrade));
                        }
                        if (nativeArray5.Length != 0 && nativeArray5[num5].m_Upkeep > 0)
                        {
                            DynamicBuffer<ServiceUpkeepData> dynamicBuffer2 = bufferAccessor2[num5];
                            for (int num7 = 0; num7 < dynamicBuffer2.Length; num7++)
                            {
                                ServiceUpkeepData value8 = dynamicBuffer2[num7];
                                value8.m_Upkeep.m_Amount = nativeArray5[num5].m_Upkeep;
                                dynamicBuffer2[num7] = value8;
                            }
                        }
                    }
                }
            }
            chunks.Dispose();
            entityCommandBuffer.Playback(base.EntityManager);
            entityCommandBuffer.Dispose();
            this.Enabled = false;
        }

        // Token: 0x06006C9F RID: 27807 RVA: 0x00003211 File Offset: 0x00001411
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
        }

        // Token: 0x06006CA0 RID: 27808 RVA: 0x00433C4F File Offset: 0x00431E4F
        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            this.__AssignQueries(ref base.CheckedStateRef);
            this.__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
        }

        // Token: 0x06006CA1 RID: 27809 RVA: 0x00006D67 File Offset: 0x00004F67
        public BuildingReinitializeSystem()
        {
        }

        // Token: 0x0400BC88 RID: 48264
        private EntityQuery m_PrefabQuery;

        // Token: 0x0400BC8A RID: 48266
        private PrefabSystem m_PrefabSystem;

        // Token: 0x0400BC8B RID: 48267
        private BuildingReinitializeSystem.TypeHandle __TypeHandle;

        // Token: 0x020018CD RID: 6349
        private struct TypeHandle
        {
            // Token: 0x06006CA4 RID: 27812 RVA: 0x004347A0 File Offset: 0x004329A0
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                this.__Game_Simulation_CollectedServiceBuildingBudgetData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CollectedServiceBuildingBudgetData>(true);
                this.__Game_Prefabs_ServiceUpkeepData_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceUpkeepData>(false);
                this.__Game_Prefabs_ServiceUpgradeBuilding_RO_BufferTypeHandle = state.GetBufferTypeHandle<ServiceUpgradeBuilding>(true);
                this.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>(true);
                this.__Game_Prefabs_BuildingData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingData>(false);
                this.__Game_Prefabs_ConsumptionData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ConsumptionData>(false);
                this.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SpawnableBuildingData>(true);
                this.__Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SignatureBuildingData>(true);
                this.__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingPropertyData>(true);
                this.__Game_Prefabs_ZoneData_RW_ComponentLookup = state.GetComponentLookup<ZoneData>(false);
                this.__Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup = state.GetComponentLookup<ZoneServiceConsumptionData>(true);
            }

            // Token: 0x0400BCAA RID: 48298
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

            [ReadOnly]
            public ComponentTypeHandle<CollectedServiceBuildingBudgetData> __Game_Simulation_CollectedServiceBuildingBudgetData_RO_ComponentTypeHandle;

            [ReadOnly]
            public BufferTypeHandle<ServiceUpgradeBuilding> __Game_Prefabs_ServiceUpgradeBuilding_RO_BufferTypeHandle;

            // Token: 0x0400BCAB RID: 48299
            [ReadOnly]
            public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentTypeHandle;

            // Token: 0x0400BCAC RID: 48300
            public ComponentTypeHandle<BuildingData> __Game_Prefabs_BuildingData_RW_ComponentTypeHandle;

            // Token: 0x0400BCAF RID: 48303
            public ComponentTypeHandle<ConsumptionData> __Game_Prefabs_ConsumptionData_RW_ComponentTypeHandle;

            // Token: 0x0400BCB1 RID: 48305
            [ReadOnly]
            public ComponentTypeHandle<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle;

            // Token: 0x0400BCB2 RID: 48306
            [ReadOnly]
            public ComponentTypeHandle<SignatureBuildingData> __Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle;

            [ReadOnly]
            public ComponentTypeHandle<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle;

            public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RW_ComponentLookup;

            // Token: 0x0400BCBC RID: 48316
            [ReadOnly]
            public ComponentLookup<ZoneServiceConsumptionData> __Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup;

            public BufferTypeHandle<ServiceUpkeepData> __Game_Prefabs_ServiceUpkeepData_RW_BufferTypeHandle;
        }
    }
}
