using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Unity.Assertions;
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
using Game.Buildings;
using Game.Areas;
using Game.Citizens;
using Game.Objects;
using Game.Economy;
using SearchSystem = Game.Net.SearchSystem;


namespace LandValueOverhaul.Systems
{
    public partial class LandValueSystem : CellMapSystem<LandValueCell>, IJobSerializable
    {
        // Token: 0x06005838 RID: 22584 RVA: 0x00336289 File Offset: 0x00334489
        public new CellMapData<LandValueCell> GetData(bool readOnly, out JobHandle dependencies)
        {
            dependencies = (readOnly ? this.m_WriteDependencies : JobHandle.CombineDependencies(this.m_ReadDependencies, this.m_WriteDependencies));
            
            CellMapData<LandValueCell> data = new CellMapData<LandValueCell>
            {
                m_Buffer = this.m_Map,
                m_CellSize = CellMapSystem<LandValueCell>.kMapSize / this.m_TextureSize,
                m_TextureSize = this.m_TextureSize
            };
            return data;
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 128;
        }

        // Token: 0x170009CC RID: 2508
        // (get) Token: 0x06005839 RID: 22585 RVA: 0x00336296 File Offset: 0x00334496
        public int2 TextureSize
        {
            get
            {
                return new int2(LandValueSystem.kTextureSize, LandValueSystem.kTextureSize);
            }
        }

        // Token: 0x0600583A RID: 22586 RVA: 0x003362A7 File Offset: 0x003344A7
        public static float3 GetCellCenter(int index)
        {
            return CellMapSystem<LandValueCell>.GetCellCenter(index, LandValueSystem.kTextureSize);
        }

        // Token: 0x0600583B RID: 22587 RVA: 0x003362B4 File Offset: 0x003344B4
        public static int GetCellIndex(float3 pos)
        {
            int num = CellMapSystem<LandValueCell>.kMapSize / LandValueSystem.kTextureSize;
            return Mathf.FloorToInt(((float)(CellMapSystem<LandValueCell>.kMapSize / 2) + pos.x) / (float)num) + Mathf.FloorToInt(((float)(CellMapSystem<LandValueCell>.kMapSize / 2) + pos.z) / (float)num) * LandValueSystem.kTextureSize;
        }

        // Token: 0x0600583C RID: 22588 RVA: 0x00336304 File Offset: 0x00334504
        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            Mod.log.Info("Modded LandValueSystem created!");
            Assert.IsTrue(LandValueSystem.kTextureSize == TerrainAttractivenessSystem.kTextureSize);
            base.CreateTextures(LandValueSystem.kTextureSize);
            this.m_NetSearchSystem = base.World.GetOrCreateSystemManaged<SearchSystem>();
            this.m_GroundPollutionSystem = base.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
            this.m_AirPollutionSystem = base.World.GetOrCreateSystemManaged<AirPollutionSystem>();
            this.m_NoisePollutionSystem = base.World.GetOrCreateSystemManaged<NoisePollutionSystem>();
            this.m_TerrainAttractivenessSystem = base.World.GetOrCreateSystemManaged<TerrainAttractivenessSystem>();
            this.m_AvailabilityInfoToGridSystem = base.World.GetOrCreateSystemManaged<AvailabilityInfoToGridSystem>();
            this.m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
            this.m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
            this.m_TelecomCoverageSystem = base.World.GetOrCreateSystemManaged<TelecomCoverageSystem>();
            this.m_AttractivenessParameterQuery = base.GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<AttractivenessParameterData>()
            });
            this.m_LandValueParameterQuery = base.GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<LandValueParameterData>()
            });
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

        // Token: 0x0600583D RID: 22589 RVA: 0x003364A0 File Offset: 0x003346A0
        [Preserve]
        protected override void OnUpdate()
        {
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
                this.__TypeHandle.__Game_Net_ResourceAvailability_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Net_ServiceCoverage_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
                LandValueSystem.EdgeUpdateJob jobData = default(LandValueSystem.EdgeUpdateJob);
                jobData.m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle;
                jobData.m_EdgeType = this.__TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle;
                jobData.m_ServiceCoverageType = this.__TypeHandle.__Game_Net_ServiceCoverage_RO_BufferTypeHandle;
                jobData.m_AvailabilityType = this.__TypeHandle.__Game_Net_ResourceAvailability_RO_BufferTypeHandle;
                jobData.m_LandValueParameterData = this.m_LandValueParameterQuery.GetSingleton<LandValueParameterData>();
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
                JobHandle job0;
                jobData.m_PollutionMap = this.m_GroundPollutionSystem.GetMap(true, out job0);
                jobData.m_PollutionParameters = this.m_PollutionParameterQuery.GetSingleton<PollutionParameterData>();
                base.Dependency = jobData.ScheduleParallel(this.m_EdgeGroup, JobHandle.CombineDependencies(base.Dependency, job0));
                this.m_GroundPollutionSystem.AddReader(base.Dependency);
            }
            if (!this.m_NodeGroup.IsEmptyIgnoreFilter)
            {
                this.__TypeHandle.__Game_Net_LandValue_RW_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Net_Curve_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Net_ConnectedEdge_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Net_Node_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
                LandValueSystem.NodeUpdateJob jobData2 = default(LandValueSystem.NodeUpdateJob);
                jobData2.m_EntityType = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle;
                jobData2.m_NodeType = this.__TypeHandle.__Game_Net_Node_RO_ComponentTypeHandle;
                jobData2.m_ConnectedEdgeType = this.__TypeHandle.__Game_Net_ConnectedEdge_RO_BufferTypeHandle;
                jobData2.m_Curves = this.__TypeHandle.__Game_Net_Curve_RO_ComponentLookup;
                jobData2.m_LandValues = this.__TypeHandle.__Game_Net_LandValue_RW_ComponentLookup;
                base.Dependency = jobData2.ScheduleParallel(this.m_NodeGroup, base.Dependency);
            }
            this.__TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Net_LandValue_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            LandValueSystem.LandValueMapUpdateJob landValueMapUpdateJob = default(LandValueSystem.LandValueMapUpdateJob);
            JobHandle job;
            landValueMapUpdateJob.m_NetSearchTree = this.m_NetSearchSystem.GetNetSearchTree(true, out job);
            JobHandle job2;
            landValueMapUpdateJob.m_AttractiveMap = this.m_TerrainAttractivenessSystem.GetMap(true, out job2);
            JobHandle job3;
            landValueMapUpdateJob.m_GroundPollutionMap = this.m_GroundPollutionSystem.GetMap(true, out job3);
            JobHandle job4;
            landValueMapUpdateJob.m_AirPollutionMap = this.m_AirPollutionSystem.GetMap(true, out job4);
            JobHandle job5;
            landValueMapUpdateJob.m_NoisePollutionMap = this.m_NoisePollutionSystem.GetMap(true, out job5);
            JobHandle job6;
            landValueMapUpdateJob.m_AvailabilityInfoMap = this.m_AvailabilityInfoToGridSystem.GetMap(true, out job6);
            JobHandle job7;
            landValueMapUpdateJob.m_TelecomCoverageMap = this.m_TelecomCoverageSystem.GetData(true, out job7);
            landValueMapUpdateJob.m_LandValueMap = this.m_Map;
            landValueMapUpdateJob.m_LandValueData = this.__TypeHandle.__Game_Net_LandValue_RO_ComponentLookup;
            landValueMapUpdateJob.m_TerrainHeightData = this.m_TerrainSystem.GetHeightData(false);
            JobHandle job8;
            landValueMapUpdateJob.m_WaterSurfaceData = this.m_WaterSystem.GetSurfaceData(out job8);
            landValueMapUpdateJob.m_EdgeGeometryData = this.__TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup;
            landValueMapUpdateJob.m_AttractivenessParameterData = this.m_AttractivenessParameterQuery.GetSingleton<AttractivenessParameterData>();
            landValueMapUpdateJob.m_LandValueParameterData = this.m_LandValueParameterQuery.GetSingleton<LandValueParameterData>();
            landValueMapUpdateJob.m_CellSize = (float)CellMapSystem<LandValueCell>.kMapSize / (float)LandValueSystem.kTextureSize;
            LandValueSystem.LandValueMapUpdateJob jobData3 = landValueMapUpdateJob;
            base.Dependency = jobData3.Schedule(LandValueSystem.kTextureSize * LandValueSystem.kTextureSize, LandValueSystem.kTextureSize, JobHandle.CombineDependencies(job, job2, JobHandle.CombineDependencies(this.m_WriteDependencies, this.m_ReadDependencies, JobHandle.CombineDependencies(base.Dependency, job8, JobHandle.CombineDependencies(job3, job5, JobHandle.CombineDependencies(job6, job4, job7))))));
            base.AddWriter(base.Dependency);
            this.m_NetSearchSystem.AddNetSearchTreeReader(base.Dependency);
            this.m_WaterSystem.AddSurfaceReader(base.Dependency);
            this.m_TerrainAttractivenessSystem.AddReader(base.Dependency);
            this.m_GroundPollutionSystem.AddReader(base.Dependency);
            this.m_AirPollutionSystem.AddReader(base.Dependency);
            this.m_NoisePollutionSystem.AddReader(base.Dependency);
            this.m_AvailabilityInfoToGridSystem.AddReader(base.Dependency);
            this.m_TelecomCoverageSystem.AddReader(base.Dependency);
            base.Dependency = JobHandle.CombineDependencies(this.m_ReadDependencies, this.m_WriteDependencies, base.Dependency);
        }
        private static float GetDistanceFade(float distance)
        {
            return math.saturate(1f - distance / Mod.setting.FadeDistance);//2000 -> 200
        }

        // Token: 0x0600583E RID: 22590 RVA: 0x00003211 File Offset: 0x00001411
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
        }

        // Token: 0x0600583F RID: 22591 RVA: 0x0033681B File Offset: 0x00334A1B
        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            this.__AssignQueries(ref base.CheckedStateRef);
            this.__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
        }

        // Token: 0x06005840 RID: 22592 RVA: 0x00336840 File Offset: 0x00334A40
        [Preserve]
        public LandValueSystem()
        {
        }

        // Token: 0x04008C76 RID: 35958
        public static readonly int kTextureSize = 128;

        // Token: 0x04008C77 RID: 35959
        public static readonly int kUpdatesPerDay = 32;

        // Token: 0x04008C78 RID: 35960
        private EntityQuery m_EdgeGroup;

        // Token: 0x04008C79 RID: 35961
        private EntityQuery m_NodeGroup;

        // Token: 0x04008C7A RID: 35962
        private EntityQuery m_AttractivenessParameterQuery;

        // Token: 0x04008C7B RID: 35963
        private EntityQuery m_LandValueParameterQuery;

        private EntityQuery m_PollutionParameterQuery;

        // Token: 0x04008C7C RID: 35964
        private GroundPollutionSystem m_GroundPollutionSystem;

        // Token: 0x04008C7D RID: 35965
        private AirPollutionSystem m_AirPollutionSystem;

        // Token: 0x04008C7E RID: 35966
        private NoisePollutionSystem m_NoisePollutionSystem;

        // Token: 0x04008C7F RID: 35967
        private AvailabilityInfoToGridSystem m_AvailabilityInfoToGridSystem;

        // Token: 0x04008C80 RID: 35968
        private SearchSystem m_NetSearchSystem;

        // Token: 0x04008C81 RID: 35969
        private TerrainAttractivenessSystem m_TerrainAttractivenessSystem;

        // Token: 0x04008C82 RID: 35970
        private TerrainSystem m_TerrainSystem;

        // Token: 0x04008C83 RID: 35971
        private WaterSystem m_WaterSystem;

        // Token: 0x04008C84 RID: 35972
        private TelecomCoverageSystem m_TelecomCoverageSystem;

        // Token: 0x04008C85 RID: 35973
        private LandValueSystem.TypeHandle __TypeHandle;

        // Token: 0x02001383 RID: 4995
        private struct NetIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
        {
            // Token: 0x06005842 RID: 22594 RVA: 0x0033685B File Offset: 0x00334A5B
            public bool Intersect(QuadTreeBoundsXZ bounds)
            {
                return MathUtils.Intersect(bounds.m_Bounds, this.m_Bounds);
            }

            // Token: 0x06005843 RID: 22595 RVA: 0x00336870 File Offset: 0x00334A70
            public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
            {
                if (!MathUtils.Intersect(bounds.m_Bounds, this.m_Bounds))
                {
                    return;
                }
                if (this.m_LandValueData.HasComponent(entity) && this.m_EdgeGeometryData.HasComponent(entity))
                {
                    LandValue landValue = this.m_LandValueData[entity];
                    if (landValue.m_LandValue > 0f)
                    {
                        this.m_TotalLandValueBonus += landValue.m_LandValue;
                        this.m_TotalCount++;
                    }
                }
            }

            // Token: 0x04008C86 RID: 35974
            public int m_TotalCount;

            // Token: 0x04008C87 RID: 35975
            public float m_TotalLandValueBonus;

            // Token: 0x04008C88 RID: 35976
            public Bounds3 m_Bounds;

            // Token: 0x04008C89 RID: 35977
            public ComponentLookup<LandValue> m_LandValueData;

            // Token: 0x04008C8A RID: 35978
            public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;
        }

        private struct LandValueMapUpdateJob : IJobParallelFor
        {
            // Token: 0x06005844 RID: 22596 RVA: 0x003368E8 File Offset: 0x00334AE8
            public void Execute(int index)
            {
                float3 cellCenter = CellMapSystem<LandValueCell>.GetCellCenter(index, LandValueSystem.kTextureSize);
                if (WaterUtils.SampleDepth(ref this.m_WaterSurfaceData, cellCenter) > 1f)
                {
                    this.m_LandValueMap[index] = new LandValueCell
                    {
                        m_LandValue = this.m_LandValueParameterData.m_LandValueBaseline
                    };
                    return;
                }
                LandValueSystem.NetIterator netIterator = new LandValueSystem.NetIterator
                {
                    m_TotalCount = 0,
                    m_TotalLandValueBonus = 0f,
                    m_Bounds = new Bounds3(cellCenter - new float3(1.5f * this.m_CellSize, 10000f, 1.5f * this.m_CellSize), cellCenter + new float3(1.5f * this.m_CellSize, 10000f, 1.5f * this.m_CellSize)),
                    m_EdgeGeometryData = this.m_EdgeGeometryData,
                    m_LandValueData = this.m_LandValueData
                };
                this.m_NetSearchTree.Iterate<LandValueSystem.NetIterator>(ref netIterator, 0);
                LandValueCell landValueCell = this.m_LandValueMap[index];
                float num5 = ((float)netIterator.m_TotalCount > 0f) ? (netIterator.m_TotalLandValueBonus / (float)netIterator.m_TotalCount) : 0f;
                landValueCell.m_LandValue = num5;
                this.m_LandValueMap[index] = landValueCell;
            }

            // Token: 0x04008C8B RID: 35979
            public NativeArray<LandValueCell> m_LandValueMap;

            // Token: 0x04008C8C RID: 35980
            [ReadOnly]
            public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

            // Token: 0x04008C8D RID: 35981
            [ReadOnly]
            public NativeArray<TerrainAttractiveness> m_AttractiveMap;

            // Token: 0x04008C8E RID: 35982
            [ReadOnly]
            public NativeArray<GroundPollution> m_GroundPollutionMap;

            // Token: 0x04008C8F RID: 35983
            [ReadOnly]
            public NativeArray<AirPollution> m_AirPollutionMap;

            // Token: 0x04008C90 RID: 35984
            [ReadOnly]
            public NativeArray<NoisePollution> m_NoisePollutionMap;

            // Token: 0x04008C91 RID: 35985
            [ReadOnly]
            public NativeArray<AvailabilityInfoCell> m_AvailabilityInfoMap;

            // Token: 0x04008C92 RID: 35986
            [ReadOnly]
            public CellMapData<TelecomCoverage> m_TelecomCoverageMap;

            // Token: 0x04008C93 RID: 35987
            [ReadOnly]
            public WaterSurfaceData m_WaterSurfaceData;

            // Token: 0x04008C94 RID: 35988
            [ReadOnly]
            public TerrainHeightData m_TerrainHeightData;

            // Token: 0x04008C95 RID: 35989
            [ReadOnly]
            public ComponentLookup<LandValue> m_LandValueData;

            // Token: 0x04008C96 RID: 35990
            [ReadOnly]
            public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

            // Token: 0x04008C97 RID: 35991
            [ReadOnly]
            public AttractivenessParameterData m_AttractivenessParameterData;

            // Token: 0x04008C98 RID: 35992
            [ReadOnly]
            public LandValueParameterData m_LandValueParameterData;

            // Token: 0x04008C99 RID: 35993
            public float m_CellSize;
        }

        private struct EdgeUpdateJob : IJobChunk
        {
            // Token: 0x06005845 RID: 22597 RVA: 0x00336BC8 File Offset: 0x00334DC8
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> nativeArray = chunk.GetNativeArray(this.m_EntityType);
                NativeArray<Edge> nativeArray2 = chunk.GetNativeArray<Edge>(ref this.m_EdgeType);
                NativeArray<Curve> nativeArray3 = chunk.GetNativeArray<Curve>(ref this.m_CurveType);
                BufferAccessor<Game.Net.ServiceCoverage> bufferAccessor = chunk.GetBufferAccessor<Game.Net.ServiceCoverage>(ref this.m_ServiceCoverageType);
                BufferAccessor<ResourceAvailability> bufferAccessor2 = chunk.GetBufferAccessor<ResourceAvailability>(ref this.m_AvailabilityType);
                BufferAccessor<ConnectedBuilding> bufferAccessor3 = chunk.GetBufferAccessor<ConnectedBuilding>(ref this.m_ConnectedBuildingType);
                for (int i = 0; i < nativeArray2.Length; i++)
                {
                    Entity entity = nativeArray[i];
                    float num = 0f;
                    float num2 = 0f;
                    float num3 = 0f;

                    if (bufferAccessor.Length > 0)
                    {
                        DynamicBuffer<Game.Net.ServiceCoverage> dynamicBuffer = bufferAccessor[i];
                        Game.Net.ServiceCoverage serviceCoverage = dynamicBuffer[0];
                        num = math.lerp(serviceCoverage.m_Coverage.x, serviceCoverage.m_Coverage.y, 0.5f) * this.m_LandValueParameterData.m_HealthCoverageBonusMultiplier;
                        Game.Net.ServiceCoverage serviceCoverage2 = dynamicBuffer[5];
                        num2 = math.lerp(serviceCoverage2.m_Coverage.x, serviceCoverage2.m_Coverage.y, 0.5f) * this.m_LandValueParameterData.m_EducationCoverageBonusMultiplier;
                        Game.Net.ServiceCoverage serviceCoverage3 = dynamicBuffer[2];
                        num3 = math.lerp(serviceCoverage3.m_Coverage.x, serviceCoverage3.m_Coverage.y, 0.5f) * this.m_LandValueParameterData.m_PoliceCoverageBonusMultiplier;
                    }
                    float num4 = 0f;
                    float num5 = 0f;
                    float num6 = 0f;
                    if (bufferAccessor2.Length > 0)
                    {
                        DynamicBuffer<ResourceAvailability> dynamicBuffer2 = bufferAccessor2[i];
                        ResourceAvailability resourceAvailability = dynamicBuffer2[1];
                        num4 = math.lerp(resourceAvailability.m_Availability.x, resourceAvailability.m_Availability.y, 0.5f) * this.m_LandValueParameterData.m_CommercialServiceBonusMultiplier;
                        ResourceAvailability resourceAvailability2 = dynamicBuffer2[31];
                        num5 = math.lerp(resourceAvailability2.m_Availability.x, resourceAvailability2.m_Availability.y, 0.5f) * this.m_LandValueParameterData.m_BusBonusMultiplier;
                        ResourceAvailability resourceAvailability3 = dynamicBuffer2[32];
                        num6 = math.lerp(resourceAvailability3.m_Availability.x, resourceAvailability3.m_Availability.y, 0.5f) * this.m_LandValueParameterData.m_TramSubwayBonusMultiplier;
                    }
                    LandValue landValue = this.m_LandValues[entity];
                    float num7 = math.max(num + num2 + num3 + num4 + num5 + num6, 0f);

                    Entity start = nativeArray2[i].m_Start;
                    Entity end = nativeArray2[i].m_End;
                    int num8 = 0;
                    float num9 = 0f;
                    float num10 = 0f;
                    float pollution_factor = 0f;
                    DynamicBuffer<ConnectedBuilding> dynamicBuffer3 = bufferAccessor3[i];
                    for (int j = 0; j < dynamicBuffer3.Length; j++)
                    {
                        Entity building = dynamicBuffer3[j].m_Building;
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
                                    int num11 = buildingPropertyData.CountProperties();
                                    bool flag = buildingPropertyData.m_ResidentialProperties > 0 && (buildingPropertyData.m_AllowedSold != Resource.NoResource || buildingPropertyData.m_AllowedManufactured > Resource.NoResource);
                                    int num12 = buildingData.m_LotSize.x * buildingData.m_LotSize.y;
                                    if (buildingPropertyData.m_ResidentialProperties > 0)
                                    {
                                        num12 = math.min(num12, Mathf.CeilToInt(math.sqrt((float)num12 * (float)buildingPropertyData.m_ResidentialProperties)));
                                    }
                                    if (this.m_Attached.HasComponent(building))
                                    {
                                        Entity parent = this.m_Attached[building].m_Parent;
                                        if (this.m_SubAreas.HasBuffer(parent))
                                        {
                                            DynamicBuffer<Game.Areas.SubArea> subAreas = this.m_SubAreas[parent];
                                            num12 += Mathf.CeilToInt(ExtractorAISystem.GetArea(subAreas, this.m_Lots, this.m_Geometries));
                                        }
                                    }
                                    float num13 = landValue.m_LandValue * (float)num12 / (float)math.max(1, num11);
                                    float num14 = (float)consumptionData.m_Upkeep / (float)math.max(1, num11);
                                    if (this.m_RenterBuffers.HasBuffer(building))
                                    {
                                        DynamicBuffer<Renter> dynamicBuffer4 = this.m_RenterBuffers[building];
                                        for (int k = 0; k < dynamicBuffer4.Length; k++)
                                        {
                                            Entity renter = dynamicBuffer4[k].m_Renter;
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
                                                    current_rent_scaled = num13 + num14;

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
                                                num10 += 1f;
                                                num9 += score;
                                            }
                                        }
                                        num8 += buildingData.m_LotSize.x * buildingData.m_LotSize.y;
                                        int num15 = num11 - dynamicBuffer4.Length;
                                        num10 += num15;
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
                            num8++;
                        }
                    }
                    float length = nativeArray3[i].m_Length;
                    float distanceFade = LandValueSystem.GetDistanceFade(length);
                    num8 = math.max(num8, Mathf.CeilToInt(length / 4f));
                    float2 @float = new float2(math.max(1f, this.m_LandValues[start].m_Weight), math.max(1f, this.m_LandValues[end].m_Weight));
                    float num17 = @float.x + @float.y;
                    float2 float2 = new float2(this.m_LandValues[start].m_LandValue, this.m_LandValues[end].m_LandValue);
                    @float *= distanceFade;
                    float y = 0f;
                    if (float2.y >= float2.x)
                    {
                        y = math.lerp(float2.x, float2.y, @float.y / num17);
                    }
                    if (float2.y < float2.x)
                    {
                        y = math.lerp(float2.y, float2.x, @float.x / num17);
                    }
                    float y2 = 0f;
                    if (pollution_factor > 0f)
                    {
                        pollution_factor = math.lerp(0f, 3f, pollution_factor / 50f);
                    }
                    num7 = num7 / (1f + pollution_factor);
                    float city_service_score = num7 / math.max(landValue.m_LandValue, 1f);
                    
                    num9 += 1f / (1f + city_service_score) - 0.5f;

                    if (num10 > 0)
                    {
                        y2 = Mathf.Min(1f, Mathf.Max(-1f, -2 * num9 / num10));
                    }
                    landValue.m_Weight = math.max(1f, math.lerp(landValue.m_Weight, (float)num8, 0.1f));
                    float s = num10 / ((float)(100 * Mod.setting.FadeSpeed - 1) * landValue.m_Weight + num17);
                    if (landValue.m_LandValue > 0)
                    {
                        landValue.m_LandValue = math.lerp(landValue.m_LandValue, y, s);
                    }
                    landValue.m_LandValue += math.min(1f, math.max(-1f, y2));
                    landValue.m_LandValue = math.max(landValue.m_LandValue, 0f);
                    landValue.m_Weight = math.lerp(landValue.m_Weight, math.max(1f, 0.5f * num17), s);
                    this.m_LandValues[entity] = landValue;
                }
            }

            // Token: 0x06005846 RID: 22598 RVA: 0x00336E61 File Offset: 0x00335061
            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                this.Execute(chunk, unfilteredChunkIndex, useEnabledMask, chunkEnabledMask);
            }

            // Token: 0x04008C9A RID: 35994
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            // Token: 0x04008C9B RID: 35995
            [ReadOnly]
            public BufferTypeHandle<ConnectedBuilding> m_ConnectedBuildingType;

            [ReadOnly]
            public ComponentTypeHandle<Edge> m_EdgeType;

            [ReadOnly]
            public ComponentTypeHandle<Curve> m_CurveType;

            // Token: 0x04008C9C RID: 35996
            [ReadOnly]
            public BufferTypeHandle<Game.Net.ServiceCoverage> m_ServiceCoverageType;

            // Token: 0x04008C9D RID: 35997
            [ReadOnly]
            public BufferTypeHandle<ResourceAvailability> m_AvailabilityType;

            // Token: 0x04008C9E RID: 35998
            [NativeDisableParallelForRestriction]
            public ComponentLookup<LandValue> m_LandValues;

            // Token: 0x04008C9F RID: 35999
            [ReadOnly]
            public LandValueParameterData m_LandValueParameterData;

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
                                float distanceFade = LandValueSystem.GetDistanceFade(this.m_Curves[edge].m_Length);
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


        // Token: 0x02001386 RID: 4998
        private struct TypeHandle
        {
            // Token: 0x06005847 RID: 22599 RVA: 0x00336E70 File Offset: 0x00335070
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                this.__Game_Net_Edge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Edge>(true);
                this.__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(true);
                this.__Game_Buildings_ConnectedBuilding_RO_BufferTypeHandle = state.GetBufferTypeHandle<ConnectedBuilding>(true);
                this.__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(true);
                this.__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(true);
                this.__Game_Net_ServiceCoverage_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Net.ServiceCoverage>(true);
                this.__Game_Net_ResourceAvailability_RO_BufferTypeHandle = state.GetBufferTypeHandle<ResourceAvailability>(true);
                this.__Game_Net_LandValue_RW_ComponentLookup = state.GetComponentLookup<LandValue>(false);
                this.__Game_Net_LandValue_RO_ComponentLookup = state.GetComponentLookup<LandValue>(true);
                this.__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(true);
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

            // Token: 0x04008CA0 RID: 36000
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

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

            // Token: 0x04008CA1 RID: 36001
            [ReadOnly]
            public ComponentTypeHandle<Edge> __Game_Net_Edge_RO_ComponentTypeHandle;

            // Token: 0x04008CA2 RID: 36002
            [ReadOnly]
            public BufferTypeHandle<Game.Net.ServiceCoverage> __Game_Net_ServiceCoverage_RO_BufferTypeHandle;

            // Token: 0x04008CA3 RID: 36003
            [ReadOnly]
            public BufferTypeHandle<ResourceAvailability> __Game_Net_ResourceAvailability_RO_BufferTypeHandle;

            // Token: 0x04008CA4 RID: 36004
            public ComponentLookup<LandValue> __Game_Net_LandValue_RW_ComponentLookup;

            // Token: 0x04008CA5 RID: 36005
            [ReadOnly]
            public ComponentLookup<LandValue> __Game_Net_LandValue_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

            // Token: 0x04008CA6 RID: 36006
            [ReadOnly]
            public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

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
