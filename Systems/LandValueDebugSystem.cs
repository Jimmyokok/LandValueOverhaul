using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using Game.Debug;
using System.Diagnostics;

namespace LandValueOverhaul.Systems
{
    //[CompilerGenerated]
    public partial class LandValueDebugSystem : BaseDebugSystem
    {
        private struct LandValueEdgeGizmoJob : IJobChunk
        {
            [ReadOnly]
            public ComponentTypeHandle<Edge> m_EdgeType;

            [ReadOnly]
            public ComponentTypeHandle<Curve> m_CurveType;

            [ReadOnly]
            public ComponentTypeHandle<LandValue> m_LandValues;

            [ReadOnly]
            public TerrainHeightData m_TerrainHeightData;

            [ReadOnly]
            public ComponentLookup<Node> m_NodeData;

            public GizmoBatcher m_GizmoBatcher;

            public bool m_LandValueOption;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (!this.m_LandValueOption)
                {
                    return;
                }
                NativeArray<Edge> nativeArray;
                nativeArray = chunk.GetNativeArray(ref this.m_EdgeType);
                NativeArray<LandValue> nativeArray2;
                nativeArray2 = chunk.GetNativeArray(ref this.m_LandValues);
                NativeArray<Curve> nativeArray3;
                nativeArray3 = chunk.GetNativeArray(ref this.m_CurveType);
                if (nativeArray.Length == 0)
                {
                    return;
                }
                if (nativeArray3.Length != 0)
                {
                    for (int i = 0; i < nativeArray.Length; i++)
                    {
                        Curve curve;
                        curve = nativeArray3[i];
                        float landValue;
                        landValue = nativeArray2[i].m_LandValue;
                        Color color;
                        color = LandValueDebugSystem.GetColor(Color.gray, Color.blue, Color.magenta, landValue, 30f, 500f);
                        float3 @float;
                        @float = MathUtils.Position(curve.m_Bezier, 0.5f);
                        @float.y = TerrainUtils.SampleHeight(ref this.m_TerrainHeightData, @float);
                        @float.y += LandValueDebugSystem.heightScale * landValue / 2f;
                        this.m_GizmoBatcher.DrawWireCylinder(@float, 5f, LandValueDebugSystem.heightScale * landValue, color);
                    }
                    return;
                }
                for (int j = 0; j < nativeArray.Length; j++)
                {
                    Edge edge;
                    edge = nativeArray[j];
                    Node node;
                    node = this.m_NodeData[edge.m_Start];
                    Node node2;
                    node2 = this.m_NodeData[edge.m_End];
                    float landValue2;
                    landValue2 = nativeArray2[j].m_LandValue;
                    Color color2;
                    color2 = LandValueDebugSystem.GetColor(Color.gray, Color.blue, Color.magenta, landValue2, 30f, 500f);
                    float3 float2;
                    float2 = 0.5f * (node.m_Position + node2.m_Position);
                    float2.y = TerrainUtils.SampleHeight(ref this.m_TerrainHeightData, float2);
                    float2.y += LandValueDebugSystem.heightScale * landValue2 / 2f;
                    this.m_GizmoBatcher.DrawWireCylinder(float2, 5f, LandValueDebugSystem.heightScale * landValue2, color2);
                }
            }

            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                this.Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
            }
        }

        private struct LandValueGizmoJob : IJob
        {
            [ReadOnly]
            public NativeArray<LandValueCell> m_LandValueMap;

            [ReadOnly]
            public TerrainHeightData m_TerrainHeightData;

            public GizmoBatcher m_GizmoBatcher;

            public bool m_LandValueOption;

            [ReadOnly]
            public LandValueParameterData m_LandValueParameterData;

            public void Execute()
            {
                if (!this.m_LandValueOption)
                {
                    return;
                }
                for (int i = 0; i < this.m_LandValueMap.Length; i++)
                {
                    float landValue;
                    landValue = this.m_LandValueMap[i].m_LandValue;
                    Color color;
                    color = LandValueDebugSystem.GetColor(Color.red, Color.yellow, Color.green, landValue, 30f, 500f);
                    float3 cellCenter;
                    cellCenter = LandValueSystem.GetCellCenter(i);
                    cellCenter.y = TerrainUtils.SampleHeight(ref this.m_TerrainHeightData, cellCenter);
                    cellCenter.y += LandValueDebugSystem.heightScale * landValue / 2f;
                    if (landValue > this.m_LandValueParameterData.m_LandValueBaseline)
                    {
                        this.m_GizmoBatcher.DrawWireCube(cellCenter, new float3(15f, LandValueDebugSystem.heightScale * landValue, 15f), color);
                    }
                }
            }
        }

        private struct TypeHandle
        {
            [ReadOnly]
            public ComponentTypeHandle<Edge> __Game_Net_Edge_RO_ComponentTypeHandle;

            [ReadOnly]
            public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

            [ReadOnly]
            public ComponentTypeHandle<LandValue> __Game_Net_LandValue_RO_ComponentTypeHandle;

            [ReadOnly]
            public ComponentLookup<Node> __Game_Net_Node_RO_ComponentLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Game_Net_Edge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Edge>(isReadOnly: true);
                this.__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(isReadOnly: true);
                this.__Game_Net_LandValue_RO_ComponentTypeHandle = state.GetComponentTypeHandle<LandValue>(isReadOnly: true);
                this.__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Node>(isReadOnly: true);
            }
        }

        private LandValueSystem m_LandValueSystem;

        private GizmosSystem m_GizmosSystem;

        private TerrainSystem m_TerrainSystem;

        private DefaultToolSystem m_DefaultToolSystem;

        private EntityQuery m_LandValueEdgeQuery;

        private EntityQuery m_LandValueParameterQuery;

        public Option m_LandValueCellOption;

        private Option m_EdgeLandValueOption;

        private static readonly float heightScale = 1f;

        private TypeHandle __TypeHandle;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            Mod.log.Info("Modded LandValueDebugSystem created!");
            this.m_LandValueSystem = base.World.GetOrCreateSystemManaged<LandValueSystem>();
            this.m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
            this.m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
            this.m_DefaultToolSystem = base.World.GetOrCreateSystemManaged<DefaultToolSystem>();
            this.m_LandValueParameterQuery = base.GetEntityQuery(ComponentType.ReadOnly<LandValueParameterData>());
            this.m_LandValueEdgeQuery = base.GetEntityQuery(ComponentType.ReadOnly<Edge>(), ComponentType.ReadOnly<LandValue>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Hidden>());
            this.m_LandValueCellOption = base.AddOption("Land value (Cell)", defaultEnabled: true);
            this.m_EdgeLandValueOption = base.AddOption("Land value (Edge)", defaultEnabled: true);
            base.Enabled = false;
        }

        public override void OnEnabled(DebugUI.Container container)
        {
            base.OnEnabled(container);
            this.m_DefaultToolSystem.debugLandValue = true;
        }

        public override void OnDisabled(DebugUI.Container container)
        {
            base.OnDisabled(container);
            this.m_DefaultToolSystem.debugLandValue = false;
        }

        private static Color GetColor(Color a, Color b, Color c, float value, float maxValue1, float maxValue2)
        {
            if (value < maxValue1)
            {
                return Color.Lerp(a, b, value / maxValue1);
            }
            return Color.Lerp(b, c, math.saturate((value - maxValue1) / (maxValue2 - maxValue1)));
        }

        [Preserve]
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (this.m_LandValueCellOption.enabled)
            {
                LandValueGizmoJob jobData;
                jobData = default(LandValueGizmoJob);
                jobData.m_LandValueMap = this.m_LandValueSystem.GetMap(readOnly: true, out var dependencies);
                jobData.m_GizmoBatcher = this.m_GizmosSystem.GetGizmosBatcher(out var dependencies2);
                jobData.m_TerrainHeightData = this.m_TerrainSystem.GetHeightData();
                jobData.m_LandValueOption = this.m_LandValueCellOption.enabled;
                jobData.m_LandValueParameterData = this.m_LandValueParameterQuery.GetSingleton<LandValueParameterData>();
                base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(inputDeps, dependencies2, dependencies));
                this.m_GizmosSystem.AddGizmosBatcherWriter(base.Dependency);
            }
            if (this.m_EdgeLandValueOption.enabled)
            {
                this.__TypeHandle.__Game_Net_Node_RO_ComponentLookup.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Net_LandValue_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
                this.__TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
                LandValueEdgeGizmoJob jobData2;
                jobData2 = default(LandValueEdgeGizmoJob);
                jobData2.m_EdgeType = this.__TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle;
                jobData2.m_CurveType = this.__TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle;
                jobData2.m_LandValues = this.__TypeHandle.__Game_Net_LandValue_RO_ComponentTypeHandle;
                jobData2.m_NodeData = this.__TypeHandle.__Game_Net_Node_RO_ComponentLookup;
                jobData2.m_TerrainHeightData = this.m_TerrainSystem.GetHeightData();
                jobData2.m_GizmoBatcher = this.m_GizmosSystem.GetGizmosBatcher(out var dependencies3);
                jobData2.m_LandValueOption = this.m_EdgeLandValueOption.enabled;
                base.Dependency = JobChunkExtensions.ScheduleParallel(jobData2, this.m_LandValueEdgeQuery, JobHandle.CombineDependencies(inputDeps, dependencies3));
                this.m_GizmosSystem.AddGizmosBatcherWriter(base.Dependency);
            }
            this.m_TerrainSystem.AddCPUHeightReader(base.Dependency);
            return base.Dependency;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
        }

        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            this.__AssignQueries(ref base.CheckedStateRef);
            this.__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
        }

        [Preserve]
        public LandValueDebugSystem()
        {
        }
    }
}