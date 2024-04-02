using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Common;
using Game.Debug;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.UI.Localization;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;
using Game.UI.Tooltip;

namespace LandValueOverhaul.Systems
{
    //[CompilerGenerated]
    public partial class LandValueTooltipSystem : TooltipSystemBase
    {
        //[BurstCompile]
        private struct LandValueTooltipJob : IJob
        {
            [ReadOnly]
            public NativeArray<LandValueCell> m_LandValueMap;

            [ReadOnly]
            public NativeArray<TerrainAttractiveness> m_AttractiveMap;

            [ReadOnly]
            public NativeArray<GroundPollution> m_GroundPollutionMap;

            [ReadOnly]
            public NativeArray<AirPollution> m_AirPollutionMap;

            [ReadOnly]
            public NativeArray<NoisePollution> m_NoisePollutionMap;

            [ReadOnly]
            public AttractivenessParameterData m_AttractivenessParameterData;

            [ReadOnly]
            public float m_TerrainHeight;

            [ReadOnly]
            public float3 m_RaycastPosition;

            public NativeValue<float> m_LandValueResult;

            public NativeValue<float> m_TerrainAttractiveResult;

            public NativeValue<float> m_AirPollutionResult;

            public NativeValue<float> m_NoisePollutionResult;

            public NativeValue<float> m_GroundPollutionResult;

            public void Execute()
            {
                int cellIndex;
                cellIndex = LandValueSystem.GetCellIndex(this.m_RaycastPosition);
                this.m_LandValueResult.value = this.m_LandValueMap[cellIndex].m_LandValue * 240f;
                TerrainAttractiveness attractiveness;
                attractiveness = TerrainAttractivenessSystem.GetAttractiveness(this.m_RaycastPosition, this.m_AttractiveMap);
                this.m_TerrainAttractiveResult.value = TerrainAttractivenessSystem.EvaluateAttractiveness(this.m_TerrainHeight, attractiveness, this.m_AttractivenessParameterData);
                this.m_GroundPollutionResult.value = GroundPollutionSystem.GetPollution(this.m_RaycastPosition, this.m_GroundPollutionMap).m_Pollution;
                this.m_AirPollutionResult.value = AirPollutionSystem.GetPollution(this.m_RaycastPosition, this.m_AirPollutionMap).m_Pollution;
                this.m_NoisePollutionResult.value = NoisePollutionSystem.GetPollution(this.m_RaycastPosition, this.m_NoisePollutionMap).m_Pollution;
            }
        }

        private ToolRaycastSystem m_ToolRaycastSystem;

        private ToolSystem m_ToolSystem;

        private TerrainToolSystem m_TerrainToolSystem;

        private LandValueSystem m_LandValueSystem;

        private LandValueDebugSystem m_LandValueDebugSystem;

        private TerrainAttractivenessSystem m_TerrainAttractivenessSystem;

        private TerrainSystem m_TerrainSystem;

        private PrefabSystem m_PrefabSystem;

        private GroundPollutionSystem m_GroundPollutionSystem;

        private AirPollutionSystem m_AirPollutionSystem;

        private NoisePollutionSystem m_NoisePollutionSystem;

        private EntityQuery m_AttractivenessParameterQuery;

        private EntityQuery m_LandValueParameterQuery;

        private FloatTooltip m_LandValueTooltip;

        private FloatTooltip m_TerrainAttractiveTooltip;

        private FloatTooltip m_AirPollutionTooltip;

        private FloatTooltip m_GroundPollutionTooltip;

        private FloatTooltip m_NoisePollutionTooltip;

        private NativeValue<float> m_LandValueResult;

        private NativeValue<float> m_TerrainAttractiveResult;

        private NativeValue<float> m_AirPollutionResult;

        private NativeValue<float> m_NoisePollutionResult;

        private NativeValue<float> m_GroundPollutionResult;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            Mod.log.Info("Modded LandValueTooltipSystem created!");
            this.m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
            this.m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
            this.m_TerrainToolSystem = base.World.GetOrCreateSystemManaged<TerrainToolSystem>();
            this.m_ToolRaycastSystem = base.World.GetOrCreateSystemManaged<ToolRaycastSystem>();
            this.m_LandValueSystem = base.World.GetOrCreateSystemManaged<LandValueSystem>();
            this.m_LandValueDebugSystem = base.World.GetOrCreateSystemManaged<LandValueDebugSystem>();
            this.m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
            this.m_TerrainAttractivenessSystem = base.World.GetOrCreateSystemManaged<TerrainAttractivenessSystem>();
            this.m_GroundPollutionSystem = base.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
            this.m_AirPollutionSystem = base.World.GetOrCreateSystemManaged<AirPollutionSystem>();
            this.m_NoisePollutionSystem = base.World.GetOrCreateSystemManaged<NoisePollutionSystem>();
            this.m_AttractivenessParameterQuery = base.GetEntityQuery(ComponentType.ReadOnly<AttractivenessParameterData>());
            this.m_LandValueParameterQuery = base.GetEntityQuery(ComponentType.ReadOnly<LandValueParameterData>());
            base.RequireForUpdate(this.m_AttractivenessParameterQuery);
            base.RequireForUpdate(this.m_LandValueParameterQuery);
            this.m_LandValueTooltip = new FloatTooltip
            {
                path = "LandValue",
                icon = "Media/Game/Icons/LandValue.svg",
                label = LocalizedString.Id("Infoviews.INFOVIEW[LandValue]"),
                unit = "money"
            };
            this.m_TerrainAttractiveTooltip = new FloatTooltip
            {
                path = "TerrainAttractive",
                icon = "Media/Game/Icons/Tourism.svg",
                label = LocalizedString.Id("Properties.CITY_MODIFIER[Attractiveness]"),
                unit = "integer"
            };
            this.m_AirPollutionTooltip = new FloatTooltip
            {
                path = "AirPollution",
                icon = "Media/Game/Icons/AirPollution.svg",
                label = LocalizedString.Id("Infoviews.INFOVIEW[AirPollution]"),
                unit = "integer"
            };
            this.m_GroundPollutionTooltip = new FloatTooltip
            {
                path = "GroundPollution",
                icon = "Media/Game/Icons/GroundPollution.svg",
                label = LocalizedString.Id("Infoviews.INFOVIEW[GroundPollution]"),
                unit = "integer"
            };
            this.m_NoisePollutionTooltip = new FloatTooltip
            {
                path = "NoisePollution",
                icon = "Media/Game/Icons/NoisePollution.svg",
                label = LocalizedString.Id("Infoviews.INFOVIEW[NoisePollution]"),
                unit = "integer"
            };
            this.m_LandValueResult = new NativeValue<float>(Allocator.Persistent);
            this.m_TerrainAttractiveResult = new NativeValue<float>(Allocator.Persistent);
            this.m_NoisePollutionResult = new NativeValue<float>(Allocator.Persistent);
            this.m_AirPollutionResult = new NativeValue<float>(Allocator.Persistent);
            this.m_GroundPollutionResult = new NativeValue<float>(Allocator.Persistent);
        }

        [Preserve]
        protected override void OnDestroy()
        {
            this.m_LandValueResult.Dispose();
            this.m_TerrainAttractiveResult.Dispose();
            this.m_NoisePollutionResult.Dispose();
            this.m_AirPollutionResult.Dispose();
            this.m_GroundPollutionResult.Dispose();
            base.OnDestroy();
        }

        private bool IsInfomodeActivated()
        {
            if (this.m_PrefabSystem.TryGetPrefab<InfoviewPrefab>(this.m_LandValueParameterQuery.GetSingleton<LandValueParameterData>().m_LandValueInfoViewPrefab, out var prefab))
            {
                return this.m_ToolSystem.activeInfoview == prefab;
            }
            return false;
        }

        [Preserve]
        protected override void OnUpdate()
        {
            if (this.IsInfomodeActivated() || this.m_LandValueDebugSystem.Enabled)
            {
                base.CompleteDependency();
                this.m_LandValueTooltip.value = this.m_LandValueResult.value;
                base.AddMouseTooltip(this.m_LandValueTooltip);
                if (this.m_LandValueDebugSystem.Enabled)
                {
                    if (this.m_TerrainAttractiveResult.value > 0f)
                    {
                        this.m_TerrainAttractiveTooltip.value = this.m_TerrainAttractiveResult.value;
                        base.AddMouseTooltip(this.m_TerrainAttractiveTooltip);
                    }
                    if (this.m_AirPollutionResult.value > 0f)
                    {
                        this.m_AirPollutionTooltip.value = this.m_AirPollutionResult.value;
                        base.AddMouseTooltip(this.m_AirPollutionTooltip);
                    }
                    if (this.m_GroundPollutionResult.value > 0f)
                    {
                        this.m_GroundPollutionTooltip.value = this.m_GroundPollutionResult.value;
                        base.AddMouseTooltip(this.m_GroundPollutionTooltip);
                    }
                    if (this.m_NoisePollutionResult.value > 0f)
                    {
                        this.m_NoisePollutionTooltip.value = this.m_NoisePollutionResult.value;
                        base.AddMouseTooltip(this.m_NoisePollutionTooltip);
                    }
                }
                this.m_LandValueResult.value = 0f;
                this.m_TerrainAttractiveResult.value = 0f;
                this.m_AirPollutionResult.value = 0f;
                this.m_GroundPollutionResult.value = 0f;
                this.m_NoisePollutionResult.value = 0f;
                this.m_ToolRaycastSystem.typeMask = TypeMask.Terrain | TypeMask.Water;
                this.m_ToolRaycastSystem.GetRaycastResult(out var result);
                TerrainHeightData data;
                data = this.m_TerrainSystem.GetHeightData();
                LandValueTooltipJob jobData;
                jobData = default(LandValueTooltipJob);
                jobData.m_LandValueMap = this.m_LandValueSystem.GetMap(readOnly: true, out var dependencies);
                jobData.m_AttractiveMap = this.m_TerrainAttractivenessSystem.GetMap(readOnly: true, out var dependencies2);
                jobData.m_GroundPollutionMap = this.m_GroundPollutionSystem.GetMap(readOnly: true, out var dependencies3);
                jobData.m_AirPollutionMap = this.m_AirPollutionSystem.GetMap(readOnly: true, out var dependencies4);
                jobData.m_NoisePollutionMap = this.m_NoisePollutionSystem.GetMap(readOnly: true, out var dependencies5);
                jobData.m_TerrainHeight = TerrainUtils.SampleHeight(ref data, result.m_Hit.m_HitPosition);
                jobData.m_AttractivenessParameterData = this.m_AttractivenessParameterQuery.GetSingleton<AttractivenessParameterData>();
                jobData.m_LandValueResult = this.m_LandValueResult;
                jobData.m_NoisePollutionResult = this.m_NoisePollutionResult;
                jobData.m_AirPollutionResult = this.m_AirPollutionResult;
                jobData.m_GroundPollutionResult = this.m_GroundPollutionResult;
                jobData.m_TerrainAttractiveResult = this.m_TerrainAttractiveResult;
                jobData.m_RaycastPosition = result.m_Hit.m_HitPosition;
                base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(base.Dependency, JobHandle.CombineDependencies(dependencies2, dependencies, JobHandle.CombineDependencies(dependencies3, dependencies4, dependencies5))));
                this.m_LandValueSystem.AddReader(base.Dependency);
                this.m_TerrainAttractivenessSystem.AddReader(base.Dependency);
                this.m_GroundPollutionSystem.AddReader(base.Dependency);
                this.m_AirPollutionSystem.AddReader(base.Dependency);
                this.m_NoisePollutionSystem.AddReader(base.Dependency);
            }
            else
            {
                this.m_LandValueResult.value = 0f;
                this.m_TerrainAttractiveResult.value = 0f;
                this.m_AirPollutionResult.value = 0f;
                this.m_GroundPollutionResult.value = 0f;
                this.m_NoisePollutionResult.value = 0f;
            }
        }

        [Preserve]
        public LandValueTooltipSystem()
        {
        }
    }
}