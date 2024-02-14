using Game;
using Game.Simulation;
using System.Runtime.CompilerServices;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Economy;
using Game.Net;
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
    // Token: 0x0200136E RID: 4974
    [CompilerGenerated]
    public class LandValueSystem_Custom : GameSystemBase
    {
        // Token: 0x0600567C RID: 22140 RVA: 0x002A4D59 File Offset: 0x002A2F59
        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 16;
        }

        // Token: 0x0600567D RID: 22141 RVA: 0x0034BA74 File Offset: 0x00349C74
        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            logger.LogInfo("Land value update manipulated!");
            this.m_GroundPollutionSystem = base.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
            this.m_PollutionParameterQuery = base.GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<PollutionParameterData>()
            });
            this.m_EdgeGroup = base.GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Edge>(),
                        ComponentType.ReadWrite<LandValue>(),
                        ComponentType.ReadOnly<Curve>(),
                        ComponentType.ReadOnly<ConnectedBuilding>()
                    },
                    Any = new ComponentType[0],
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>()
                    }
                }
            });
            this.m_NodeGroup = base.GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Game.Net.Node>(),
                        ComponentType.ReadWrite<LandValue>(),
                        ComponentType.ReadOnly<ConnectedEdge>()
                    },
                    Any = new ComponentType[0],
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>()
                    }
                }
            });
            base.RequireAnyForUpdate(new EntityQuery[]
            {
                this.m_EdgeGroup,
                this.m_NodeGroup
            });
        }

        // Token: 0x0600567E RID: 22142 RVA: 0x0034BBE0 File Offset: 0x00349DE0
        [Preserve]
        protected override void OnUpdate()
        {
            JobHandle jobHandle = base.Dependency;
            if (!this.m_EdgeGroup.IsEmptyIgnoreFilter)
            {
                this.__TypeHandle.__Game_Areas_Geometry_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Areas_Lot_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Areas_SubArea_RO_BufferLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Objects_Attached_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Objects_Placeholder_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Citizens_Household_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Prefabs_ConsumptionData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Buildings_Renter_RO_BufferLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Net_LandValue_RW_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Buildings_ConnectedBuilding_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
                LandValueSystem_Custom.EdgeUpdateJob jobData = default(LandValueSystem_Custom.EdgeUpdateJob);
                jobData.m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle;
                jobData.m_EdgeType = this.__TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle;
                jobData.m_CurveType = this.__TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle;
                jobData.m_ConnectedBuildingType = this.__TypeHandle.__Game_Buildings_ConnectedBuilding_RO_BufferTypeHandle;
                jobData.m_BuildingDatas = this.__TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup;
                jobData.m_Transforms = this.__TypeHandle.__Game_Objects_Transform_RO_ComponentLookup;
                jobData.m_LandValues = this.__TypeHandle.__Game_Net_LandValue_RW_ComponentLookup;
                jobData.m_Prefabs = this.__TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
                jobData.m_PropertyRenters = this.__TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup;
                jobData.m_RenterBuffers = this.__TypeHandle.__Game_Buildings_Renter_RO_BufferLookup;
                jobData.m_Abandoneds = this.__TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup;
                jobData.m_Destroyeds = this.__TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup;
                jobData.m_ConsumptionDatas = this.__TypeHandle.__Game_Prefabs_ConsumptionData_RO_ComponentLookup;
                jobData.m_PropertyDatas = this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;
                jobData.m_Households = this.__TypeHandle.__Game_Citizens_Household_RO_ComponentLookup;
                jobData.m_Placeholders = this.__TypeHandle.__Game_Objects_Placeholder_RO_ComponentLookup;
                jobData.m_Attached = this.__TypeHandle.__Game_Objects_Attached_RO_ComponentLookup;
                jobData.m_SubAreas = this.__TypeHandle.__Game_Areas_SubArea_RO_BufferLookup;
                jobData.m_Lots = this.__TypeHandle.__Game_Areas_Lot_RO_ComponentLookup;
                jobData.m_Geometries = this.__TypeHandle.__Game_Areas_Geometry_RO_ComponentLookup;
                JobHandle job;
                jobData.m_PollutionMap = this.m_GroundPollutionSystem.GetMap(true, out job);
                jobData.m_PollutionParameters = this.m_PollutionParameterQuery.GetSingleton<PollutionParameterData>();
                jobHandle = jobData.ScheduleParallel(this.m_EdgeGroup, JobHandle.CombineDependencies(base.Dependency, job));
                this.m_GroundPollutionSystem.AddReader(jobHandle);
            }
            if (!this.m_NodeGroup.IsEmptyIgnoreFilter)
            {
                this.__TypeHandle.__Game_Net_LandValue_RW_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Net_Curve_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Net_ConnectedEdge_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Net_Node_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
                LandValueSystem_Custom.NodeUpdateJob jobData2 = default(LandValueSystem_Custom.NodeUpdateJob);
                jobData2.m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle;
                jobData2.m_NodeType = this.__TypeHandle.__Game_Net_Node_RO_ComponentTypeHandle;
                jobData2.m_ConnectedEdgeType = this.__TypeHandle.__Game_Net_ConnectedEdge_RO_BufferTypeHandle;
                jobData2.m_Curves = this.__TypeHandle.__Game_Net_Curve_RO_ComponentLookup;
                jobData2.m_LandValues = this.__TypeHandle.__Game_Net_LandValue_RW_ComponentLookup;
                jobHandle = jobData2.ScheduleParallel(this.m_NodeGroup, jobHandle);
            }
            base.Dependency = jobHandle;
        }

        // Token: 0x0600567F RID: 22143 RVA: 0x0034C06D File Offset: 0x0034A26D
        private static float GetDistanceFade(float distance)
        {
            return math.saturate(1f - distance / 200f);//2000 -> 200
        }

        // Token: 0x06005680 RID: 22144 RVA: 0x00002E1D File Offset: 0x0000101D
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
        }

        // Token: 0x06005681 RID: 22145 RVA: 0x0034C081 File Offset: 0x0034A281
        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            this.__AssignQueries(ref base.CheckedStateRef);
            this.__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
        }

        // Token: 0x06005682 RID: 22146 RVA: 0x00006953 File Offset: 0x00004B53
        [Preserve]
        public LandValueSystem_Custom()
        {
        }

        // Token: 0x040090F3 RID: 37107
        private GroundPollutionSystem m_GroundPollutionSystem;

        // Token: 0x040090F4 RID: 37108
        private EntityQuery m_EdgeGroup;

        // Token: 0x040090F5 RID: 37109
        private EntityQuery m_NodeGroup;

        // Token: 0x040090F6 RID: 37110
        private EntityQuery m_PollutionParameterQuery;

        private static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(MyPluginInfo.PLUGIN_GUID);
        // Token: 0x040090F7 RID: 37111
        private LandValueSystem_Custom.TypeHandle __TypeHandle;

        // Token: 0x0200136F RID: 4975
        [BurstCompile]
        private struct EdgeUpdateJob : IJobChunk
        {
            // Token: 0x06005683 RID: 22147 RVA: 0x0034C0A8 File Offset: 0x0034A2A8
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> nativeArray = chunk.GetNativeArray(this.m_EntityType);
                NativeArray<Edge> nativeArray2 = chunk.GetNativeArray<Edge>(ref this.m_EdgeType);
                NativeArray<Curve> nativeArray3 = chunk.GetNativeArray<Curve>(ref this.m_CurveType);
                BufferAccessor<ConnectedBuilding> bufferAccessor = chunk.GetBufferAccessor<ConnectedBuilding>(ref this.m_ConnectedBuildingType);
                for (int i = 0; i < nativeArray2.Length; i++)
                {
                    Entity entity = nativeArray[i];
                    Entity start = nativeArray2[i].m_Start;
                    Entity end = nativeArray2[i].m_End;
                    LandValue landValue = this.m_LandValues[entity];
                    int num = 0;
                    float num2 = 0f;
                    float num3 = 0f;
                    float pollution_factor = 0f;
                    DynamicBuffer<ConnectedBuilding> dynamicBuffer = bufferAccessor[i];
                    for (int j = 0; j < dynamicBuffer.Length; j++)
                    {
                        Entity building = dynamicBuffer[j].m_Building;
                        if (this.m_Prefabs.HasComponent(building) && !this.m_Placeholders.HasComponent(building))
                        {
                            Entity prefab = this.m_Prefabs[building].m_Prefab;
                            if (this.m_PropertyDatas.HasComponent(prefab) && !this.m_Abandoneds.HasComponent(building) && !this.m_Destroyeds.HasComponent(building))
                            {
                                BuildingPropertyData buildingPropertyData = this.m_PropertyDatas[prefab];
                                if (buildingPropertyData.m_AllowedStored == Resource.NoResource)
                                {
                                    BuildingData buildingData = this.m_BuildingDatas[prefab];
                                    ConsumptionData consumptionData = this.m_ConsumptionDatas[prefab];
                                    int num4 = buildingPropertyData.CountProperties();
                                    bool flag = buildingPropertyData.m_ResidentialProperties > 0 && (buildingPropertyData.m_AllowedSold != Resource.NoResource || buildingPropertyData.m_AllowedManufactured > Resource.NoResource);
                                    int num5 = buildingData.m_LotSize.x * buildingData.m_LotSize.y;
                                    if (buildingPropertyData.m_ResidentialProperties > 0)
                                    {
                                        num5 = math.min(num5, buildingPropertyData.m_ResidentialProperties);
                                    }
                                    if (this.m_Attached.HasComponent(building))
                                    {
                                        Entity parent = this.m_Attached[building].m_Parent;
                                        if (this.m_SubAreas.HasBuffer(parent))
                                        {
                                            DynamicBuffer<Game.Areas.SubArea> subAreas = this.m_SubAreas[parent];
                                            num5 += Mathf.CeilToInt(ExtractorAISystem.GetArea(subAreas, this.m_Lots, this.m_Geometries));
                                        }
                                    }
                                    float num6 = landValue.m_LandValue * (float)num5 / (float)math.max(1, num4);
                                    float num7 = (float)consumptionData.m_Upkeep / (float)math.max(1, num4);
                                    if (this.m_RenterBuffers.HasBuffer(building))
                                    {
                                        DynamicBuffer<Renter> dynamicBuffer2 = this.m_RenterBuffers[building];
                                        for (int k = 0; k < dynamicBuffer2.Length; k++)
                                        {
                                            Entity renter = dynamicBuffer2[k].m_Renter;
                                            if (this.m_PropertyRenters.HasComponent(renter))
                                            {
                                                PropertyRenter propertyRenter = this.m_PropertyRenters[renter];
                                                float desired_rent_scaled;
                                                float current_rent_scaled;
                                                if (flag && !this.m_Households.HasComponent(renter))
                                                {
                                                    desired_rent_scaled = (float)propertyRenter.m_MaxRent;
                                                    current_rent_scaled = RentAdjustSystem.kMixedCompanyRent * landValue.m_LandValue + RentAdjustSystem.kMixedCompanyRent * (float)consumptionData.m_Upkeep;
                                                }
                                                else
                                                {
                                                    desired_rent_scaled = (float)propertyRenter.m_MaxRent;
                                                    current_rent_scaled = num6 + num7;

                                                }
                                                float p = desired_rent_scaled / current_rent_scaled;
                                                float score;
                                                if (p < 1)
                                                {
                                                    score = 1f / (1f + p) - 0.5f;
                                                }
                                                else
                                                {
                                                    score = (1 + Mathf.Log(1 / p, 10f)) * (1f / (1f + p) - 0.5f);
                                                }
                                                num3 += 1f;
                                                num2 += score;
                                            }
                                        }
                                        num += buildingData.m_LotSize.x * buildingData.m_LotSize.y; ;
                                        int num8 = num4 - dynamicBuffer2.Length;
                                        num3 += num8;
                                    }
                                    if (this.m_Transforms.HasComponent(building))
                                    {
                                        pollution_factor = (float)GroundPollutionSystem.GetPollution(this.m_Transforms[building].m_Position, this.m_PollutionMap).m_Pollution / (float)this.m_PollutionParameters.m_GroundPollutionLandValueDivisor;
                                    }
                                }
                            }
                        }
                        else
                        {
                            num++;
                        }
                    }
                    float length = nativeArray3[i].m_Length;
                    float distanceFade = LandValueSystem_Custom.GetDistanceFade(length);
                    num = math.max(num, Mathf.CeilToInt(length / 4f));
                    float2 @float = new float2(math.max(1f, this.m_LandValues[start].m_Weight), math.max(1f, this.m_LandValues[end].m_Weight));
                    float num10 = @float.x + @float.y;
                    float2 float2 = new float2(this.m_LandValues[start].m_LandValue, this.m_LandValues[end].m_LandValue);
                    @float *= distanceFade;
                    float y = math.lerp(float2.x, float2.y, @float.y / num10);
                    float y2 = 0f;
                    if (num3 > 0)
                    {
                        y2 = 0.1f * Mathf.Min(1f, Mathf.Max(-1f, -2 * num2 / num3)) - 1 / (float)2400;
                    }
                    else
                    {
                        landValue.m_LandValue = 0f;
                    }
                    if (pollution_factor > 0f)
                    {
                        pollution_factor = math.lerp(0f, 2f, pollution_factor / 50f);
                    }
                    landValue.m_Weight = math.max(1f, math.lerp(landValue.m_Weight, (float)num, 0.1f));
                    float s = num10 / (99f * landValue.m_Weight + num10);
                    if (landValue.m_LandValue > 0)
                    {
                        landValue.m_LandValue = math.lerp(landValue.m_LandValue, y, s);
                    }
                    if (landValue.m_LandValue > 30f)
                    {
                        y2 -= pollution_factor * 0.2f;
                    }
                    landValue.m_LandValue += math.min(1f, math.max(-1f, y2));
                    landValue.m_LandValue = math.max(landValue.m_LandValue, 0f);
                    landValue.m_Weight = math.lerp(landValue.m_Weight, math.max(1f, 0.5f * num10), s);
                    this.m_LandValues[entity] = landValue;
                }
            }

            // Token: 0x06005684 RID: 22148 RVA: 0x0034C672 File Offset: 0x0034A872
            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                this.Execute(chunk, unfilteredChunkIndex, useEnabledMask, chunkEnabledMask);
            }

            // Token: 0x040090F8 RID: 37112
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            // Token: 0x040090F9 RID: 37113
            [ReadOnly]
            public BufferTypeHandle<ConnectedBuilding> m_ConnectedBuildingType;

            // Token: 0x040090FA RID: 37114
            [ReadOnly]
            public ComponentTypeHandle<Edge> m_EdgeType;

            // Token: 0x040090FB RID: 37115
            [ReadOnly]
            public ComponentTypeHandle<Curve> m_CurveType;

            // Token: 0x040090FC RID: 37116
            [NativeDisableParallelForRestriction]
            public ComponentLookup<LandValue> m_LandValues;

            // Token: 0x040090FD RID: 37117
            [ReadOnly]
            public BufferLookup<Renter> m_RenterBuffers;

            // Token: 0x040090FE RID: 37118
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> m_Transforms;

            // Token: 0x040090FF RID: 37119
            [ReadOnly]
            public ComponentLookup<PropertyRenter> m_PropertyRenters;

            // Token: 0x04009100 RID: 37120
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_Prefabs;

            // Token: 0x04009101 RID: 37121
            [ReadOnly]
            public ComponentLookup<BuildingData> m_BuildingDatas;

            // Token: 0x04009102 RID: 37122
            [ReadOnly]
            public ComponentLookup<Abandoned> m_Abandoneds;

            // Token: 0x04009103 RID: 37123
            [ReadOnly]
            public ComponentLookup<Destroyed> m_Destroyeds;

            // Token: 0x04009104 RID: 37124
            [ReadOnly]
            public ComponentLookup<ConsumptionData> m_ConsumptionDatas;

            // Token: 0x04009105 RID: 37125
            [ReadOnly]
            public ComponentLookup<BuildingPropertyData> m_PropertyDatas;

            // Token: 0x04009106 RID: 37126
            [ReadOnly]
            public ComponentLookup<Household> m_Households;

            // Token: 0x04009107 RID: 37127
            [ReadOnly]
            public ComponentLookup<Placeholder> m_Placeholders;

            // Token: 0x04009108 RID: 37128
            [ReadOnly]
            public ComponentLookup<Attached> m_Attached;

            // Token: 0x04009109 RID: 37129
            [ReadOnly]
            public BufferLookup<Game.Areas.SubArea> m_SubAreas;

            // Token: 0x0400910A RID: 37130
            [ReadOnly]
            public ComponentLookup<Game.Areas.Lot> m_Lots;

            // Token: 0x0400910B RID: 37131
            [ReadOnly]
            public ComponentLookup<Geometry> m_Geometries;

            // Token: 0x0400910C RID: 37132
            [ReadOnly]
            public NativeArray<GroundPollution> m_PollutionMap;

            // Token: 0x0400910D RID: 37133
            [ReadOnly]
            public PollutionParameterData m_PollutionParameters;
        }

        // Token: 0x02001370 RID: 4976
        [BurstCompile]
        private struct NodeUpdateJob : IJobChunk
        {
            // Token: 0x06005685 RID: 22149 RVA: 0x0034C680 File Offset: 0x0034A880
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> nativeArray = chunk.GetNativeArray(this.m_EntityType);
                NativeArray<Game.Net.Node> nativeArray2 = chunk.GetNativeArray<Game.Net.Node>(ref this.m_NodeType);
                BufferAccessor<ConnectedEdge> bufferAccessor = chunk.GetBufferAccessor<ConnectedEdge>(ref this.m_ConnectedEdgeType);
                for (int i = 0; i < nativeArray2.Length; i++)
                {
                    Entity entity = nativeArray[i];
                    float num = 0f;
                    float num2 = 0f;
                    DynamicBuffer<ConnectedEdge> dynamicBuffer = bufferAccessor[i];
                    for (int j = 0; j < dynamicBuffer.Length; j++)
                    {
                        Entity edge = dynamicBuffer[j].m_Edge;
                        if (this.m_LandValues.HasComponent(edge))
                        {
                            float landValue = this.m_LandValues[edge].m_LandValue;
                            float num3 = this.m_LandValues[edge].m_Weight;
                            if (this.m_Curves.HasComponent(edge))
                            {
                                float distanceFade = LandValueSystem_Custom.GetDistanceFade(this.m_Curves[edge].m_Length);
                                num3 *= distanceFade;
                            }
                            if (landValue > 0)
                            {
                                num += landValue * num3;
                                num2 += num3;
                            }
                        }
                    }
                    if (num2 != 0f)
                    {
                        num /= num2;
                        LandValue landValue2 = this.m_LandValues[entity];
                        landValue2.m_LandValue = math.lerp(landValue2.m_LandValue, num, 0.05f);
                        landValue2.m_Weight = math.max(1f, math.lerp(landValue2.m_Weight, num2 / (float)dynamicBuffer.Length, 0.05f));
                        this.m_LandValues[entity] = landValue2;
                    }
                }
            }

            // Token: 0x06005686 RID: 22150 RVA: 0x0034C806 File Offset: 0x0034AA06
            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                this.Execute(chunk, unfilteredChunkIndex, useEnabledMask, chunkEnabledMask);
            }

            // Token: 0x0400910E RID: 37134
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            // Token: 0x0400910F RID: 37135
            [ReadOnly]
            public ComponentTypeHandle<Game.Net.Node> m_NodeType;

            // Token: 0x04009110 RID: 37136
            [ReadOnly]
            public BufferTypeHandle<ConnectedEdge> m_ConnectedEdgeType;

            // Token: 0x04009111 RID: 37137
            [NativeDisableParallelForRestriction]
            public ComponentLookup<LandValue> m_LandValues;

            // Token: 0x04009112 RID: 37138
            [ReadOnly]
            public ComponentLookup<Curve> m_Curves;
        }

        // Token: 0x02001371 RID: 4977
        private struct TypeHandle
        {
            // Token: 0x06005687 RID: 22151 RVA: 0x0034C814 File Offset: 0x0034AA14
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                this.__Game_Net_Edge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Edge>(true);
                this.__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(true);
                this.__Game_Buildings_ConnectedBuilding_RO_BufferTypeHandle = state.GetBufferTypeHandle<ConnectedBuilding>(true);
                this.__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(true);
                this.__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(true);
                this.__Game_Net_LandValue_RW_ComponentLookup = state.GetComponentLookup<LandValue>(false);
                this.__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(true);
                this.__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(true);
                this.__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(true);
                this.__Game_Buildings_Abandoned_RO_ComponentLookup = state.GetComponentLookup<Abandoned>(true);
                this.__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(true);
                this.__Game_Prefabs_ConsumptionData_RO_ComponentLookup = state.GetComponentLookup<ConsumptionData>(true);
                this.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(true);
                this.__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(true);
                this.__Game_Objects_Placeholder_RO_ComponentLookup = state.GetComponentLookup<Placeholder>(true);
                this.__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(true);
                this.__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(true);
                this.__Game_Areas_Lot_RO_ComponentLookup = state.GetComponentLookup<Game.Areas.Lot>(true);
                this.__Game_Areas_Geometry_RO_ComponentLookup = state.GetComponentLookup<Geometry>(true);
                this.__Game_Net_Node_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.Node>(true);
                this.__Game_Net_ConnectedEdge_RO_BufferTypeHandle = state.GetBufferTypeHandle<ConnectedEdge>(true);
                this.__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(true);
            }

            // Token: 0x04009113 RID: 37139
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

            // Token: 0x04009114 RID: 37140
            [ReadOnly]
            public ComponentTypeHandle<Edge> __Game_Net_Edge_RO_ComponentTypeHandle;

            // Token: 0x04009115 RID: 37141
            [ReadOnly]
            public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

            // Token: 0x04009116 RID: 37142
            [ReadOnly]
            public BufferTypeHandle<ConnectedBuilding> __Game_Buildings_ConnectedBuilding_RO_BufferTypeHandle;

            // Token: 0x04009117 RID: 37143
            [ReadOnly]
            public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

            // Token: 0x04009118 RID: 37144
            [ReadOnly]
            public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

            // Token: 0x04009119 RID: 37145
            public ComponentLookup<LandValue> __Game_Net_LandValue_RW_ComponentLookup;

            // Token: 0x0400911A RID: 37146
            [ReadOnly]
            public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

            // Token: 0x0400911B RID: 37147
            [ReadOnly]
            public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

            // Token: 0x0400911C RID: 37148
            [ReadOnly]
            public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

            // Token: 0x0400911D RID: 37149
            [ReadOnly]
            public ComponentLookup<Abandoned> __Game_Buildings_Abandoned_RO_ComponentLookup;

            // Token: 0x0400911E RID: 37150
            [ReadOnly]
            public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

            // Token: 0x0400911F RID: 37151
            [ReadOnly]
            public ComponentLookup<ConsumptionData> __Game_Prefabs_ConsumptionData_RO_ComponentLookup;

            // Token: 0x04009120 RID: 37152
            [ReadOnly]
            public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

            // Token: 0x04009121 RID: 37153
            [ReadOnly]
            public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

            // Token: 0x04009122 RID: 37154
            [ReadOnly]
            public ComponentLookup<Placeholder> __Game_Objects_Placeholder_RO_ComponentLookup;

            // Token: 0x04009123 RID: 37155
            [ReadOnly]
            public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

            // Token: 0x04009124 RID: 37156
            [ReadOnly]
            public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

            // Token: 0x04009125 RID: 37157
            [ReadOnly]
            public ComponentLookup<Game.Areas.Lot> __Game_Areas_Lot_RO_ComponentLookup;

            // Token: 0x04009126 RID: 37158
            [ReadOnly]
            public ComponentLookup<Geometry> __Game_Areas_Geometry_RO_ComponentLookup;

            // Token: 0x04009127 RID: 37159
            [ReadOnly]
            public ComponentTypeHandle<Game.Net.Node> __Game_Net_Node_RO_ComponentTypeHandle;

            // Token: 0x04009128 RID: 37160
            [ReadOnly]
            public BufferTypeHandle<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferTypeHandle;

            // Token: 0x04009129 RID: 37161
            [ReadOnly]
            public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;
        }
    }
}