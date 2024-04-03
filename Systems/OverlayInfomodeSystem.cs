using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.City;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using Game;
using Game.Rendering;

namespace LandValueOverhaul.Systems
{
    public partial class OverlayInfomodeSystem : GameSystemBase
    {
        
        private struct ClearJob : IJob
        {
            public NativeArray<byte> m_TextureData;

            public void Execute()
            {
                for (int i = 0; i < this.m_TextureData.Length; i++)
                {
                    this.m_TextureData[i] = 0;
                }
            }
        }

        
        private struct GroundWaterJob : IJob
        {
            [ReadOnly]
            public InfomodeActive m_ActiveData;

            [ReadOnly]
            public CellMapData<GroundWater> m_MapData;

            public NativeArray<byte> m_TextureData;

            public void Execute()
            {
                int num;
                num = this.m_ActiveData.m_Index - 1;
                for (int i = 0; i < this.m_MapData.m_TextureSize.y; i++)
                {
                    for (int j = 0; j < this.m_MapData.m_TextureSize.x; j++)
                    {
                        int num2;
                        num2 = j + i * this.m_MapData.m_TextureSize.x;
                        GroundWater groundWater;
                        groundWater = this.m_MapData.m_Buffer[num2];
                        this.m_TextureData[num2 * 4 + num] = (byte)math.clamp(groundWater.m_Amount / 32, 0, 255);
                    }
                }
            }
        }

        
        private struct GroundPollutionJob : IJob
        {
            [ReadOnly]
            public InfomodeActive m_ActiveData;

            [ReadOnly]
            public CellMapData<GroundPollution> m_MapData;

            public NativeArray<byte> m_TextureData;

            public float m_Multiplier;

            public void Execute()
            {
                int num;
                num = this.m_ActiveData.m_Index - 1;
                for (int i = 0; i < this.m_MapData.m_TextureSize.y; i++)
                {
                    for (int j = 0; j < this.m_MapData.m_TextureSize.x; j++)
                    {
                        int num2;
                        num2 = j + i * this.m_MapData.m_TextureSize.x;
                        GroundPollution groundPollution;
                        groundPollution = this.m_MapData.m_Buffer[num2];
                        this.m_TextureData[num2 * 4 + num] = (byte)math.clamp(Mathf.RoundToInt((float)groundPollution.m_Pollution * this.m_Multiplier), 0, 255);
                    }
                }
            }
        }

        
        private struct NoisePollutionJob : IJob
        {
            [ReadOnly]
            public InfomodeActive m_ActiveData;

            [ReadOnly]
            public CellMapData<NoisePollution> m_MapData;

            public NativeArray<byte> m_TextureData;

            public float m_Multiplier;

            public void Execute()
            {
                int num;
                num = this.m_ActiveData.m_Index - 1;
                for (int i = 0; i < this.m_MapData.m_TextureSize.y; i++)
                {
                    for (int j = 0; j < this.m_MapData.m_TextureSize.x; j++)
                    {
                        int num2;
                        num2 = j + i * this.m_MapData.m_TextureSize.x;
                        NoisePollution noisePollution;
                        noisePollution = this.m_MapData.m_Buffer[num2];
                        this.m_TextureData[num2 * 4 + num] = (byte)math.clamp(Mathf.RoundToInt((float)noisePollution.m_Pollution * this.m_Multiplier), 0, 255);
                    }
                }
            }
        }

        
        private struct AirPollutionJob : IJob
        {
            [ReadOnly]
            public InfomodeActive m_ActiveData;

            [ReadOnly]
            public CellMapData<AirPollution> m_MapData;

            public NativeArray<byte> m_TextureData;

            public float m_Multiplier;

            public void Execute()
            {
                int num;
                num = this.m_ActiveData.m_Index - 1;
                for (int i = 0; i < this.m_MapData.m_TextureSize.y; i++)
                {
                    for (int j = 0; j < this.m_MapData.m_TextureSize.x; j++)
                    {
                        int num2;
                        num2 = j + i * this.m_MapData.m_TextureSize.x;
                        AirPollution airPollution;
                        airPollution = this.m_MapData.m_Buffer[num2];
                        this.m_TextureData[num2 * 4 + num] = (byte)math.clamp(Mathf.RoundToInt((float)airPollution.m_Pollution * this.m_Multiplier), 0, 255);
                    }
                }
            }
        }

        
        private struct WindJob : IJob
        {
            [ReadOnly]
            public CellMapData<Wind> m_MapData;

            public NativeArray<half4> m_TextureData;

            public void Execute()
            {
                for (int i = 0; i < this.m_MapData.m_TextureSize.y; i++)
                {
                    for (int j = 0; j < this.m_MapData.m_TextureSize.x; j++)
                    {
                        int index;
                        index = j + i * this.m_MapData.m_TextureSize.x;
                        Wind wind;
                        wind = this.m_MapData.m_Buffer[index];
                        this.m_TextureData[index] = new half4((half)wind.m_Wind.x, (half)wind.m_Wind.y, (half)0f, (half)0f);
                    }
                }
            }
        }

        
        private struct TelecomCoverageJob : IJob
        {
            [ReadOnly]
            public InfomodeActive m_ActiveData;

            [ReadOnly]
            public CellMapData<TelecomCoverage> m_MapData;

            public NativeArray<byte> m_TextureData;

            public void Execute()
            {
                int num;
                num = this.m_ActiveData.m_Index - 1;
                for (int i = 0; i < this.m_MapData.m_TextureSize.y; i++)
                {
                    for (int j = 0; j < this.m_MapData.m_TextureSize.x; j++)
                    {
                        int num2;
                        num2 = j + i * this.m_MapData.m_TextureSize.x;
                        TelecomCoverage telecomCoverage;
                        telecomCoverage = this.m_MapData.m_Buffer[num2];
                        this.m_TextureData[num2 * 4 + num] = (byte)math.clamp(telecomCoverage.networkQuality, 0, 255);
                    }
                }
            }
        }

        
        private struct FertilityJob : IJob
        {
            [ReadOnly]
            public InfomodeActive m_ActiveData;

            [ReadOnly]
            public CellMapData<NaturalResourceCell> m_MapData;

            public NativeArray<byte> m_TextureData;

            public void Execute()
            {
                int num;
                num = this.m_ActiveData.m_Index - 1;
                for (int i = 0; i < this.m_MapData.m_TextureSize.y; i++)
                {
                    for (int j = 0; j < this.m_MapData.m_TextureSize.x; j++)
                    {
                        int num2;
                        num2 = j + i * this.m_MapData.m_TextureSize.x;
                        NaturalResourceCell naturalResourceCell;
                        naturalResourceCell = this.m_MapData.m_Buffer[num2];
                        float num3;
                        num3 = math.saturate(((float)(int)naturalResourceCell.m_Fertility.m_Base - (float)(int)naturalResourceCell.m_Fertility.m_Used) * 0.0001f);
                        this.m_TextureData[num2 * 4 + num] = (byte)math.clamp(Mathf.RoundToInt(num3 * 255f), 0, 255);
                    }
                }
            }
        }

        
        private struct OreJob : IJob
        {
            [ReadOnly]
            public InfomodeActive m_ActiveData;

            [ReadOnly]
            public CellMapData<NaturalResourceCell> m_MapData;

            [ReadOnly]
            public Entity m_City;

            [ReadOnly]
            public BufferLookup<CityModifier> m_CityModifiers;

            public NativeArray<byte> m_TextureData;

            public void Execute()
            {
                int num;
                num = this.m_ActiveData.m_Index - 1;
                DynamicBuffer<CityModifier> modifiers;
                modifiers = default(DynamicBuffer<CityModifier>);
                if (this.m_CityModifiers.HasBuffer(this.m_City))
                {
                    modifiers = this.m_CityModifiers[this.m_City];
                }
                for (int i = 0; i < this.m_MapData.m_TextureSize.y; i++)
                {
                    for (int j = 0; j < this.m_MapData.m_TextureSize.x; j++)
                    {
                        int num2;
                        num2 = j + i * this.m_MapData.m_TextureSize.x;
                        NaturalResourceCell naturalResourceCell;
                        naturalResourceCell = this.m_MapData.m_Buffer[num2];
                        float value;
                        value = (int)naturalResourceCell.m_Ore.m_Base;
                        if (modifiers.IsCreated)
                        {
                            CityUtils.ApplyModifier(ref value, modifiers, CityModifierType.OreResourceAmount);
                        }
                        value = math.saturate((value - (float)(int)naturalResourceCell.m_Ore.m_Used) * 0.0001f);
                        this.m_TextureData[num2 * 4 + num] = (byte)math.clamp(Mathf.RoundToInt(value * 255f), 0, 255);
                    }
                }
            }
        }

        
        private struct OilJob : IJob
        {
            [ReadOnly]
            public InfomodeActive m_ActiveData;

            [ReadOnly]
            public CellMapData<NaturalResourceCell> m_MapData;

            [ReadOnly]
            public Entity m_City;

            [ReadOnly]
            public BufferLookup<CityModifier> m_CityModifiers;

            public NativeArray<byte> m_TextureData;

            public void Execute()
            {
                int num;
                num = this.m_ActiveData.m_Index - 1;
                DynamicBuffer<CityModifier> modifiers;
                modifiers = default(DynamicBuffer<CityModifier>);
                if (this.m_CityModifiers.HasBuffer(this.m_City))
                {
                    modifiers = this.m_CityModifiers[this.m_City];
                }
                for (int i = 0; i < this.m_MapData.m_TextureSize.y; i++)
                {
                    for (int j = 0; j < this.m_MapData.m_TextureSize.x; j++)
                    {
                        int num2;
                        num2 = j + i * this.m_MapData.m_TextureSize.x;
                        NaturalResourceCell naturalResourceCell;
                        naturalResourceCell = this.m_MapData.m_Buffer[num2];
                        float value;
                        value = (int)naturalResourceCell.m_Oil.m_Base;
                        if (modifiers.IsCreated)
                        {
                            CityUtils.ApplyModifier(ref value, modifiers, CityModifierType.OilResourceAmount);
                        }
                        value = math.saturate((value - (float)(int)naturalResourceCell.m_Oil.m_Used) * 0.0001f);
                        this.m_TextureData[num2 * 4 + num] = (byte)math.clamp(Mathf.RoundToInt(value * 255f), 0, 255);
                    }
                }
            }
        }

        
        private struct LandValueJob : IJob
        {
            [ReadOnly]
            public InfomodeActive m_ActiveData;

            [ReadOnly]
            public CellMapData<LandValueCell> m_MapData;

            public NativeArray<byte> m_TextureData;

            public void Execute()
            {
                int num;
                num = this.m_ActiveData.m_Index - 1;
                Mod.log.Info($"{this.m_MapData.m_TextureSize.x}, {this.m_MapData.m_TextureSize.y}, {num}");
                for (int i = 0; i < this.m_MapData.m_TextureSize.y; i++)
                {
                    for (int j = 0; j < this.m_MapData.m_TextureSize.x; j++)
                    {
                        int num2;
                        num2 = j + i * this.m_MapData.m_TextureSize.x;
                        LandValueCell landValueCell;
                        landValueCell = this.m_MapData.m_Buffer[num2];
                        this.m_TextureData[num2 * 4 + num] = (byte)math.clamp(Mathf.RoundToInt(landValueCell.m_LandValue * 0.51f), 0, 255);
                        Mod.log.Info($"{landValueCell.m_LandValue}");
                    }
                }
            }
        }

        
        private struct PopulationJob : IJob
        {
            [ReadOnly]
            public InfomodeActive m_ActiveData;

            [ReadOnly]
            public CellMapData<PopulationCell> m_MapData;

            public NativeArray<byte> m_TextureData;

            public void Execute()
            {
                int num;
                num = this.m_ActiveData.m_Index - 1;
                for (int i = 0; i < this.m_MapData.m_TextureSize.y; i++)
                {
                    for (int j = 0; j < this.m_MapData.m_TextureSize.x; j++)
                    {
                        int num2;
                        num2 = j + i * this.m_MapData.m_TextureSize.x;
                        PopulationCell populationCell;
                        populationCell = this.m_MapData.m_Buffer[num2];
                        this.m_TextureData[num2 * 4 + num] = (byte)math.clamp(Mathf.RoundToInt(populationCell.Get() * 0.24902344f), 0, 255);
                    }
                }
            }
        }

        
        private struct AttractionJob : IJob
        {
            [ReadOnly]
            public InfomodeActive m_ActiveData;

            [ReadOnly]
            public CellMapData<AvailabilityInfoCell> m_MapData;

            public NativeArray<byte> m_TextureData;

            public void Execute()
            {
                int num;
                num = this.m_ActiveData.m_Index - 1;
                for (int i = 0; i < this.m_MapData.m_TextureSize.y; i++)
                {
                    for (int j = 0; j < this.m_MapData.m_TextureSize.x; j++)
                    {
                        int num2;
                        num2 = j + i * this.m_MapData.m_TextureSize.x;
                        AvailabilityInfoCell availabilityInfoCell;
                        availabilityInfoCell = this.m_MapData.m_Buffer[num2];
                        this.m_TextureData[num2 * 4 + num] = (byte)math.clamp(Mathf.RoundToInt(availabilityInfoCell.m_AvailabilityInfo.x * 15.9375f), 0, 255);
                    }
                }
            }
        }

        
        private struct CustomerJob : IJob
        {
            [ReadOnly]
            public InfomodeActive m_ActiveData;

            [ReadOnly]
            public CellMapData<AvailabilityInfoCell> m_MapData;

            public NativeArray<byte> m_TextureData;

            public void Execute()
            {
                int num;
                num = this.m_ActiveData.m_Index - 1;
                for (int i = 0; i < this.m_MapData.m_TextureSize.y; i++)
                {
                    for (int j = 0; j < this.m_MapData.m_TextureSize.x; j++)
                    {
                        int num2;
                        num2 = j + i * this.m_MapData.m_TextureSize.x;
                        AvailabilityInfoCell availabilityInfoCell;
                        availabilityInfoCell = this.m_MapData.m_Buffer[num2];
                        this.m_TextureData[num2 * 4 + num] = (byte)math.clamp(Mathf.RoundToInt(availabilityInfoCell.m_AvailabilityInfo.y * 15.9375f), 0, 255);
                    }
                }
            }
        }

        
        private struct WorkplaceJob : IJob
        {
            [ReadOnly]
            public InfomodeActive m_ActiveData;

            [ReadOnly]
            public CellMapData<AvailabilityInfoCell> m_MapData;

            public NativeArray<byte> m_TextureData;

            public void Execute()
            {
                int num;
                num = this.m_ActiveData.m_Index - 1;
                for (int i = 0; i < this.m_MapData.m_TextureSize.y; i++)
                {
                    for (int j = 0; j < this.m_MapData.m_TextureSize.x; j++)
                    {
                        int num2;
                        num2 = j + i * this.m_MapData.m_TextureSize.x;
                        AvailabilityInfoCell availabilityInfoCell;
                        availabilityInfoCell = this.m_MapData.m_Buffer[num2];
                        this.m_TextureData[num2 * 4 + num] = (byte)math.clamp(Mathf.RoundToInt(availabilityInfoCell.m_AvailabilityInfo.z * 15.9375f), 0, 255);
                    }
                }
            }
        }

        
        private struct ServiceJob : IJob
        {
            [ReadOnly]
            public InfomodeActive m_ActiveData;

            [ReadOnly]
            public CellMapData<AvailabilityInfoCell> m_MapData;

            public NativeArray<byte> m_TextureData;

            public void Execute()
            {
                int num;
                num = this.m_ActiveData.m_Index - 1;
                for (int i = 0; i < this.m_MapData.m_TextureSize.y; i++)
                {
                    for (int j = 0; j < this.m_MapData.m_TextureSize.x; j++)
                    {
                        int num2;
                        num2 = j + i * this.m_MapData.m_TextureSize.x;
                        AvailabilityInfoCell availabilityInfoCell;
                        availabilityInfoCell = this.m_MapData.m_Buffer[num2];
                        this.m_TextureData[num2 * 4 + num] = (byte)math.clamp(Mathf.RoundToInt(availabilityInfoCell.m_AvailabilityInfo.w * 15.9375f), 0, 255);
                    }
                }
            }
        }

        
        private struct GroundWaterPollutionJob : IJob
        {
            [ReadOnly]
            public InfomodeActive m_ActiveData;

            [ReadOnly]
            public CellMapData<GroundWater> m_MapData;

            public NativeArray<byte> m_TextureData;

            public void Execute()
            {
                int num;
                num = this.m_ActiveData.m_Index - 1;
                for (int i = 0; i < this.m_MapData.m_TextureSize.y; i++)
                {
                    for (int j = 0; j < this.m_MapData.m_TextureSize.x; j++)
                    {
                        int num2;
                        num2 = j + i * this.m_MapData.m_TextureSize.x;
                        GroundWater groundWater;
                        groundWater = this.m_MapData.m_Buffer[num2];
                        this.m_TextureData[num2 * 4 + num] = (byte)math.clamp(math.min(groundWater.m_Amount / 32, groundWater.m_Polluted * 256 / math.max(1, groundWater.m_Amount)), 0, 255);
                    }
                }
            }
        }

        private struct TypeHandle
        {
            [ReadOnly]
            public ComponentTypeHandle<InfoviewHeatmapData> __Game_Prefabs_InfoviewHeatmapData_RO_ComponentTypeHandle;

            [ReadOnly]
            public ComponentTypeHandle<InfomodeActive> __Game_Prefabs_InfomodeActive_RO_ComponentTypeHandle;

            [ReadOnly]
            public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Game_Prefabs_InfoviewHeatmapData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewHeatmapData>(isReadOnly: true);
                this.__Game_Prefabs_InfomodeActive_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfomodeActive>(isReadOnly: true);
                this.__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
            }
        }

        private TerrainRenderSystem m_TerrainRenderSystem;

        private WaterRenderSystem m_WaterRenderSystem;

        private GroundWaterSystem m_GroundWaterSystem;

        private GroundPollutionSystem m_GroundPollutionSystem;

        private NoisePollutionSystem m_NoisePollutionSystem;

        private AirPollutionSystem m_AirPollutionSystem;

        private WindSystem m_WindSystem;

        private CitySystem m_CitySystem;

        private TelecomPreviewSystem m_TelecomCoverageSystem;

        private NaturalResourceSystem m_NaturalResourceSystem;

        private Game.Simulation.LandValueSystem m_LandValueSystem;

        private PopulationToGridSystem m_PopulationToGridSystem;

        private AvailabilityInfoToGridSystem m_AvailabilityInfoToGridSystem;

        private ToolSystem m_ToolSystem;

        private EntityQuery m_InfomodeQuery;

        private EntityQuery m_HappinessParameterQuery;

        private Texture2D m_TerrainTexture;

        private Texture2D m_WaterTexture;

        private Texture2D m_WindTexture;

        private JobHandle m_Dependency;

        private TypeHandle __TypeHandle;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            Mod.log.Info("Modded OverlayInfomodeSystem created!");
            this.m_TerrainRenderSystem = base.World.GetOrCreateSystemManaged<TerrainRenderSystem>();
            this.m_WaterRenderSystem = base.World.GetOrCreateSystemManaged<WaterRenderSystem>();
            this.m_GroundWaterSystem = base.World.GetOrCreateSystemManaged<GroundWaterSystem>();
            this.m_GroundPollutionSystem = base.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
            this.m_NoisePollutionSystem = base.World.GetOrCreateSystemManaged<NoisePollutionSystem>();
            this.m_AirPollutionSystem = base.World.GetOrCreateSystemManaged<AirPollutionSystem>();
            this.m_WindSystem = base.World.GetOrCreateSystemManaged<WindSystem>();
            this.m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
            this.m_TelecomCoverageSystem = base.World.GetOrCreateSystemManaged<TelecomPreviewSystem>();
            this.m_NaturalResourceSystem = base.World.GetOrCreateSystemManaged<NaturalResourceSystem>();
            this.m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
            this.m_LandValueSystem = base.World.GetOrCreateSystemManaged<Game.Simulation.LandValueSystem>();
            this.m_PopulationToGridSystem = base.World.GetOrCreateSystemManaged<PopulationToGridSystem>();
            this.m_AvailabilityInfoToGridSystem = base.World.GetOrCreateSystemManaged<AvailabilityInfoToGridSystem>();
            this.m_TerrainTexture = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false, linear: true)
            {
                name = "TerrainInfoTexture",
                hideFlags = HideFlags.HideAndDontSave
            };
            this.m_WaterTexture = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false, linear: true)
            {
                name = "WaterInfoTexture",
                hideFlags = HideFlags.HideAndDontSave
            };
            this.m_WindTexture = new Texture2D(this.m_WindSystem.TextureSize.x, this.m_WindSystem.TextureSize.y, GraphicsFormat.R16G16B16A16_SFloat, 1, TextureCreationFlags.None)
            {
                name = "WindInfoTexture",
                hideFlags = HideFlags.HideAndDontSave
            };
            this.m_InfomodeQuery = base.GetEntityQuery(ComponentType.ReadOnly<InfomodeActive>(), ComponentType.ReadOnly<InfoviewHeatmapData>());
            this.m_HappinessParameterQuery = base.GetEntityQuery(ComponentType.ReadOnly<CitizenHappinessParameterData>());
        }

        [Preserve]
        protected override void OnDestroy()
        {
            CoreUtils.Destroy(this.m_TerrainTexture);
            CoreUtils.Destroy(this.m_WaterTexture);
            CoreUtils.Destroy(this.m_WindTexture);
            base.OnDestroy();
        }

        public void ApplyOverlay()
        {
            if (this.m_TerrainRenderSystem.overrideOverlaymap == this.m_TerrainTexture)
            {
                this.m_Dependency.Complete();
                this.m_TerrainTexture.Apply();
            }
            if (this.m_TerrainRenderSystem.overlayExtramap == this.m_WindTexture)
            {
                this.m_Dependency.Complete();
                this.m_WindTexture.Apply();
            }
            if (this.m_WaterRenderSystem.overrideOverlaymap == this.m_WaterTexture)
            {
                this.m_Dependency.Complete();
                this.m_WaterTexture.Apply();
            }
        }

        private NativeArray<byte> GetTerrainTextureData<T>(CellMapData<T> cellMapData) where T : struct, ISerializable
        {
            return this.GetTerrainTextureData(cellMapData.m_TextureSize);
        }

        private NativeArray<byte> GetTerrainTextureData(int2 size)
        {
            if (this.m_TerrainTexture.width != size.x || this.m_TerrainTexture.height != size.y)
            {
                this.m_TerrainTexture.Reinitialize(size.x, size.y);
                this.m_TerrainRenderSystem.overrideOverlaymap = null;
            }
            if (this.m_TerrainRenderSystem.overrideOverlaymap != this.m_TerrainTexture)
            {
                this.m_TerrainRenderSystem.overrideOverlaymap = this.m_TerrainTexture;
                ClearJob jobData;
                jobData = default(ClearJob);
                jobData.m_TextureData = this.m_TerrainTexture.GetRawTextureData<byte>();
                this.m_Dependency = IJobExtensions.Schedule(jobData, base.Dependency);
                base.Dependency = this.m_Dependency;
            }
            return this.m_TerrainTexture.GetRawTextureData<byte>();
        }

        private NativeArray<byte> GetWaterTextureData<T>(CellMapData<T> cellMapData) where T : struct, ISerializable
        {
            return this.GetWaterTextureData(cellMapData.m_TextureSize);
        }

        private NativeArray<byte> GetWaterTextureData(int2 size)
        {
            if (this.m_WaterTexture.width != size.x || this.m_WaterTexture.height != size.y)
            {
                this.m_WaterTexture.Reinitialize(size.x, size.y);
                this.m_WaterRenderSystem.overrideOverlaymap = null;
            }
            if (this.m_WaterRenderSystem.overrideOverlaymap != this.m_WaterTexture)
            {
                this.m_WaterRenderSystem.overrideOverlaymap = this.m_WaterTexture;
                ClearJob jobData;
                jobData = default(ClearJob);
                jobData.m_TextureData = this.m_WaterTexture.GetRawTextureData<byte>();
                this.m_Dependency = IJobExtensions.Schedule(jobData, base.Dependency);
                base.Dependency = this.m_Dependency;
            }
            return this.m_WaterTexture.GetRawTextureData<byte>();
        }

        [Preserve]
        protected override void OnUpdate()
        {
            Mod.log.Info("Overlay updated");
            this.m_TerrainRenderSystem.overrideOverlaymap = null;
            this.m_TerrainRenderSystem.overlayExtramap = null;
            this.m_TerrainRenderSystem.overlayArrowMask = default(float4);
            this.m_WaterRenderSystem.overrideOverlaymap = null;
            this.m_WaterRenderSystem.overlayExtramap = null;
            this.m_WaterRenderSystem.overlayPollutionMask = default(float4);
            this.m_WaterRenderSystem.overlayArrowMask = default(float4);
            if (!this.m_InfomodeQuery.IsEmptyIgnoreFilter)
            {
                NativeArray<ArchetypeChunk> nativeArray;
                nativeArray = this.m_InfomodeQuery.ToArchetypeChunkArray(Allocator.TempJob);
                this.__TypeHandle.__Game_Prefabs_InfoviewHeatmapData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
                ComponentTypeHandle<InfoviewHeatmapData> typeHandle;
                typeHandle = this.__TypeHandle.__Game_Prefabs_InfoviewHeatmapData_RO_ComponentTypeHandle;
                this.__TypeHandle.__Game_Prefabs_InfomodeActive_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
                ComponentTypeHandle<InfomodeActive> typeHandle2;
                typeHandle2 = this.__TypeHandle.__Game_Prefabs_InfomodeActive_RO_ComponentTypeHandle;
                for (int i = 0; i < nativeArray.Length; i++)
                {
                    ArchetypeChunk archetypeChunk;
                    archetypeChunk = nativeArray[i];
                    NativeArray<InfoviewHeatmapData> nativeArray2;
                    nativeArray2 = archetypeChunk.GetNativeArray(ref typeHandle);
                    NativeArray<InfomodeActive> nativeArray3;
                    nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle2);
                    for (int j = 0; j < nativeArray2.Length; j++)
                    {
                        InfoviewHeatmapData infoviewHeatmapData;
                        infoviewHeatmapData = nativeArray2[j];
                        InfomodeActive activeData;
                        activeData = nativeArray3[j];
                        switch (infoviewHeatmapData.m_Type)
                        {
                            case HeatmapData.GroundWater:
                                {
                                    GroundWaterJob groundWaterJob;
                                    groundWaterJob = default(GroundWaterJob);
                                    groundWaterJob.m_ActiveData = activeData;
                                    groundWaterJob.m_MapData = this.m_GroundWaterSystem.GetData(readOnly: true, out var dependencies16);
                                    GroundWaterJob jobData16;
                                    jobData16 = groundWaterJob;
                                    jobData16.m_TextureData = this.GetTerrainTextureData(jobData16.m_MapData);
                                    JobHandle jobHandle16;
                                    jobHandle16 = IJobExtensions.Schedule(jobData16, JobHandle.CombineDependencies(base.Dependency, dependencies16));
                                    this.m_GroundWaterSystem.AddReader(jobHandle16);
                                    this.m_Dependency = jobHandle16;
                                    base.Dependency = jobHandle16;
                                    break;
                                }
                            case HeatmapData.GroundPollution:
                                {
                                    CitizenHappinessParameterData singleton3;
                                    singleton3 = this.m_HappinessParameterQuery.GetSingleton<CitizenHappinessParameterData>();
                                    GroundPollutionJob groundPollutionJob;
                                    groundPollutionJob = default(GroundPollutionJob);
                                    groundPollutionJob.m_ActiveData = activeData;
                                    groundPollutionJob.m_MapData = this.m_GroundPollutionSystem.GetData(readOnly: true, out var dependencies15);
                                    groundPollutionJob.m_Multiplier = 256f / ((float)singleton3.m_MaxAirAndGroundPollutionBonus * (float)singleton3.m_PollutionBonusDivisor);
                                    GroundPollutionJob jobData15;
                                    jobData15 = groundPollutionJob;
                                    jobData15.m_TextureData = this.GetTerrainTextureData(jobData15.m_MapData);
                                    JobHandle jobHandle15;
                                    jobHandle15 = IJobExtensions.Schedule(jobData15, JobHandle.CombineDependencies(base.Dependency, dependencies15));
                                    this.m_GroundPollutionSystem.AddReader(jobHandle15);
                                    this.m_Dependency = jobHandle15;
                                    base.Dependency = jobHandle15;
                                    break;
                                }
                            case HeatmapData.AirPollution:
                                {
                                    CitizenHappinessParameterData singleton2;
                                    singleton2 = this.m_HappinessParameterQuery.GetSingleton<CitizenHappinessParameterData>();
                                    AirPollutionJob airPollutionJob;
                                    airPollutionJob = default(AirPollutionJob);
                                    airPollutionJob.m_ActiveData = activeData;
                                    airPollutionJob.m_MapData = this.m_AirPollutionSystem.GetData(readOnly: true, out var dependencies14);
                                    airPollutionJob.m_Multiplier = 256f / ((float)singleton2.m_MaxAirAndGroundPollutionBonus * (float)singleton2.m_PollutionBonusDivisor);
                                    AirPollutionJob jobData14;
                                    jobData14 = airPollutionJob;
                                    jobData14.m_TextureData = this.GetTerrainTextureData(jobData14.m_MapData);
                                    JobHandle jobHandle14;
                                    jobHandle14 = IJobExtensions.Schedule(jobData14, JobHandle.CombineDependencies(base.Dependency, dependencies14));
                                    this.m_AirPollutionSystem.AddReader(jobHandle14);
                                    this.m_Dependency = jobHandle14;
                                    base.Dependency = jobHandle14;
                                    break;
                                }
                            case HeatmapData.Noise:
                                {
                                    CitizenHappinessParameterData singleton;
                                    singleton = this.m_HappinessParameterQuery.GetSingleton<CitizenHappinessParameterData>();
                                    NoisePollutionJob noisePollutionJob;
                                    noisePollutionJob = default(NoisePollutionJob);
                                    noisePollutionJob.m_ActiveData = activeData;
                                    noisePollutionJob.m_MapData = this.m_NoisePollutionSystem.GetData(readOnly: true, out var dependencies13);
                                    noisePollutionJob.m_Multiplier = 256f / ((float)singleton.m_MaxNoisePollutionBonus * (float)singleton.m_PollutionBonusDivisor);
                                    NoisePollutionJob jobData13;
                                    jobData13 = noisePollutionJob;
                                    jobData13.m_TextureData = this.GetTerrainTextureData(jobData13.m_MapData);
                                    JobHandle jobHandle13;
                                    jobHandle13 = IJobExtensions.Schedule(jobData13, JobHandle.CombineDependencies(base.Dependency, dependencies13));
                                    this.m_NoisePollutionSystem.AddReader(jobHandle13);
                                    this.m_Dependency = jobHandle13;
                                    base.Dependency = jobHandle13;
                                    break;
                                }
                            case HeatmapData.Wind:
                                {
                                    this.m_TerrainRenderSystem.overlayExtramap = this.m_WindTexture;
                                    float4 overlayArrowMask2;
                                    overlayArrowMask2 = default(float4);
                                    overlayArrowMask2[activeData.m_Index - 1] = 1f;
                                    this.m_TerrainRenderSystem.overlayArrowMask = overlayArrowMask2;
                                    WindJob jobData12;
                                    jobData12 = default(WindJob);
                                    jobData12.m_MapData = this.m_WindSystem.GetData(readOnly: true, out var dependencies12);
                                    jobData12.m_TextureData = this.m_WindTexture.GetRawTextureData<half4>();
                                    JobHandle jobHandle12;
                                    jobHandle12 = IJobExtensions.Schedule(jobData12, JobHandle.CombineDependencies(base.Dependency, dependencies12));
                                    this.m_WindSystem.AddReader(jobHandle12);
                                    this.m_Dependency = jobHandle12;
                                    base.Dependency = jobHandle12;
                                    break;
                                }
                            case HeatmapData.WaterFlow:
                                {
                                    this.m_WaterRenderSystem.overlayExtramap = this.m_WaterRenderSystem.flowTexture;
                                    float4 overlayArrowMask;
                                    overlayArrowMask = default(float4);
                                    overlayArrowMask[activeData.m_Index - 5] = 1f;
                                    this.m_WaterRenderSystem.overlayArrowMask = overlayArrowMask;
                                    break;
                                }
                            case HeatmapData.TelecomCoverage:
                                {
                                    TelecomCoverageJob telecomCoverageJob;
                                    telecomCoverageJob = default(TelecomCoverageJob);
                                    telecomCoverageJob.m_ActiveData = activeData;
                                    telecomCoverageJob.m_MapData = this.m_TelecomCoverageSystem.GetData(readOnly: true, out var dependencies11);
                                    TelecomCoverageJob jobData11;
                                    jobData11 = telecomCoverageJob;
                                    jobData11.m_TextureData = this.GetTerrainTextureData(jobData11.m_MapData);
                                    JobHandle jobHandle11;
                                    jobHandle11 = IJobExtensions.Schedule(jobData11, JobHandle.CombineDependencies(base.Dependency, dependencies11));
                                    this.m_TelecomCoverageSystem.AddReader(jobHandle11);
                                    this.m_Dependency = jobHandle11;
                                    base.Dependency = jobHandle11;
                                    break;
                                }
                            case HeatmapData.Fertility:
                                {
                                    FertilityJob fertilityJob;
                                    fertilityJob = default(FertilityJob);
                                    fertilityJob.m_ActiveData = activeData;
                                    fertilityJob.m_MapData = this.m_NaturalResourceSystem.GetData(readOnly: true, out var dependencies10);
                                    FertilityJob jobData10;
                                    jobData10 = fertilityJob;
                                    jobData10.m_TextureData = this.GetTerrainTextureData(jobData10.m_MapData);
                                    JobHandle jobHandle10;
                                    jobHandle10 = IJobExtensions.Schedule(jobData10, JobHandle.CombineDependencies(base.Dependency, dependencies10));
                                    this.m_NaturalResourceSystem.AddReader(jobHandle10);
                                    this.m_Dependency = jobHandle10;
                                    base.Dependency = jobHandle10;
                                    break;
                                }
                            case HeatmapData.Ore:
                                {
                                    this.__TypeHandle.__Game_City_CityModifier_RO_BufferLookup.Update(ref base.CheckedStateRef);
                                    OreJob oreJob;
                                    oreJob = default(OreJob);
                                    oreJob.m_ActiveData = activeData;
                                    oreJob.m_MapData = this.m_NaturalResourceSystem.GetData(readOnly: true, out var dependencies9);
                                    oreJob.m_City = this.m_CitySystem.City;
                                    oreJob.m_CityModifiers = this.__TypeHandle.__Game_City_CityModifier_RO_BufferLookup;
                                    OreJob jobData9;
                                    jobData9 = oreJob;
                                    jobData9.m_TextureData = this.GetTerrainTextureData(jobData9.m_MapData);
                                    JobHandle jobHandle9;
                                    jobHandle9 = IJobExtensions.Schedule(jobData9, JobHandle.CombineDependencies(base.Dependency, dependencies9));
                                    this.m_NaturalResourceSystem.AddReader(jobHandle9);
                                    this.m_Dependency = jobHandle9;
                                    base.Dependency = jobHandle9;
                                    break;
                                }
                            case HeatmapData.Oil:
                                {
                                    this.__TypeHandle.__Game_City_CityModifier_RO_BufferLookup.Update(ref base.CheckedStateRef);
                                    OilJob oilJob;
                                    oilJob = default(OilJob);
                                    oilJob.m_ActiveData = activeData;
                                    oilJob.m_MapData = this.m_NaturalResourceSystem.GetData(readOnly: true, out var dependencies8);
                                    oilJob.m_CityModifiers = this.__TypeHandle.__Game_City_CityModifier_RO_BufferLookup;
                                    OilJob jobData8;
                                    jobData8 = oilJob;
                                    jobData8.m_TextureData = this.GetTerrainTextureData(jobData8.m_MapData);
                                    JobHandle jobHandle8;
                                    jobHandle8 = IJobExtensions.Schedule(jobData8, JobHandle.CombineDependencies(base.Dependency, dependencies8));
                                    this.m_NaturalResourceSystem.AddReader(jobHandle8);
                                    this.m_Dependency = jobHandle8;
                                    base.Dependency = jobHandle8;
                                    break;
                                }
                            case HeatmapData.LandValue:
                                {
                                    LandValueJob landValueJob;
                                    landValueJob = default(LandValueJob);
                                    landValueJob.m_ActiveData = activeData;
                                    landValueJob.m_MapData = this.m_LandValueSystem.GetData(readOnly: true, out var dependencies7);
                                    LandValueJob jobData7;
                                    jobData7 = landValueJob;
                                    jobData7.m_TextureData = this.GetTerrainTextureData(jobData7.m_MapData);
                                    JobHandle jobHandle7;
                                    jobHandle7 = IJobExtensions.Schedule(jobData7, JobHandle.CombineDependencies(dependencies7, base.Dependency));
                                    this.m_LandValueSystem.AddReader(jobHandle7);
                                    this.m_Dependency = jobHandle7;
                                    base.Dependency = jobHandle7;
                                    break;
                                }
                            case HeatmapData.Population:
                                {
                                    PopulationJob populationJob;
                                    populationJob = default(PopulationJob);
                                    populationJob.m_ActiveData = activeData;
                                    populationJob.m_MapData = this.m_PopulationToGridSystem.GetData(readOnly: true, out var dependencies6);
                                    PopulationJob jobData6;
                                    jobData6 = populationJob;
                                    jobData6.m_TextureData = this.GetTerrainTextureData(jobData6.m_MapData);
                                    JobHandle jobHandle6;
                                    jobHandle6 = IJobExtensions.Schedule(jobData6, JobHandle.CombineDependencies(dependencies6, base.Dependency));
                                    this.m_PopulationToGridSystem.AddReader(jobHandle6);
                                    this.m_Dependency = jobHandle6;
                                    base.Dependency = jobHandle6;
                                    break;
                                }
                            case HeatmapData.Attraction:
                                {
                                    AttractionJob attractionJob;
                                    attractionJob = default(AttractionJob);
                                    attractionJob.m_ActiveData = activeData;
                                    attractionJob.m_MapData = this.m_AvailabilityInfoToGridSystem.GetData(readOnly: true, out var dependencies5);
                                    AttractionJob jobData5;
                                    jobData5 = attractionJob;
                                    jobData5.m_TextureData = this.GetTerrainTextureData(jobData5.m_MapData);
                                    JobHandle jobHandle5;
                                    jobHandle5 = IJobExtensions.Schedule(jobData5, JobHandle.CombineDependencies(base.Dependency, dependencies5));
                                    this.m_AvailabilityInfoToGridSystem.AddReader(jobHandle5);
                                    this.m_Dependency = jobHandle5;
                                    base.Dependency = jobHandle5;
                                    break;
                                }
                            case HeatmapData.Customers:
                                {
                                    CustomerJob customerJob;
                                    customerJob = default(CustomerJob);
                                    customerJob.m_ActiveData = activeData;
                                    customerJob.m_MapData = this.m_AvailabilityInfoToGridSystem.GetData(readOnly: true, out var dependencies4);
                                    CustomerJob jobData4;
                                    jobData4 = customerJob;
                                    jobData4.m_TextureData = this.GetTerrainTextureData(jobData4.m_MapData);
                                    JobHandle jobHandle4;
                                    jobHandle4 = IJobExtensions.Schedule(jobData4, JobHandle.CombineDependencies(base.Dependency, dependencies4));
                                    this.m_AvailabilityInfoToGridSystem.AddReader(jobHandle4);
                                    this.m_Dependency = jobHandle4;
                                    base.Dependency = jobHandle4;
                                    break;
                                }
                            case HeatmapData.Workplaces:
                                {
                                    WorkplaceJob workplaceJob;
                                    workplaceJob = default(WorkplaceJob);
                                    workplaceJob.m_ActiveData = activeData;
                                    workplaceJob.m_MapData = this.m_AvailabilityInfoToGridSystem.GetData(readOnly: true, out var dependencies3);
                                    WorkplaceJob jobData3;
                                    jobData3 = workplaceJob;
                                    jobData3.m_TextureData = this.GetTerrainTextureData(jobData3.m_MapData);
                                    JobHandle jobHandle3;
                                    jobHandle3 = IJobExtensions.Schedule(jobData3, JobHandle.CombineDependencies(base.Dependency, dependencies3));
                                    this.m_AvailabilityInfoToGridSystem.AddReader(jobHandle3);
                                    this.m_Dependency = jobHandle3;
                                    base.Dependency = jobHandle3;
                                    break;
                                }
                            case HeatmapData.Services:
                                {
                                    ServiceJob serviceJob;
                                    serviceJob = default(ServiceJob);
                                    serviceJob.m_ActiveData = activeData;
                                    serviceJob.m_MapData = this.m_AvailabilityInfoToGridSystem.GetData(readOnly: true, out var dependencies2);
                                    ServiceJob jobData2;
                                    jobData2 = serviceJob;
                                    jobData2.m_TextureData = this.GetTerrainTextureData(jobData2.m_MapData);
                                    JobHandle jobHandle2;
                                    jobHandle2 = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(base.Dependency, dependencies2));
                                    this.m_AvailabilityInfoToGridSystem.AddReader(jobHandle2);
                                    this.m_Dependency = jobHandle2;
                                    base.Dependency = jobHandle2;
                                    break;
                                }
                            case HeatmapData.GroundWaterPollution:
                                {
                                    GroundWaterPollutionJob groundWaterPollutionJob;
                                    groundWaterPollutionJob = default(GroundWaterPollutionJob);
                                    groundWaterPollutionJob.m_ActiveData = activeData;
                                    groundWaterPollutionJob.m_MapData = this.m_GroundWaterSystem.GetData(readOnly: true, out var dependencies);
                                    GroundWaterPollutionJob jobData;
                                    jobData = groundWaterPollutionJob;
                                    jobData.m_TextureData = this.GetTerrainTextureData(jobData.m_MapData);
                                    JobHandle jobHandle;
                                    jobHandle = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(base.Dependency, dependencies));
                                    this.m_GroundWaterSystem.AddReader(jobHandle);
                                    this.m_Dependency = jobHandle;
                                    base.Dependency = jobHandle;
                                    break;
                                }
                            case HeatmapData.WaterPollution:
                                {
                                    float4 overlayPollutionMask;
                                    overlayPollutionMask = default(float4);
                                    overlayPollutionMask[activeData.m_Index - 5] = 1f;
                                    this.m_WaterRenderSystem.overlayPollutionMask = overlayPollutionMask;
                                    break;
                                }
                        }
                    }
                }
                nativeArray.Dispose();
            }
            if (this.m_ToolSystem.activeInfoview != null)
            {
                if (this.m_TerrainRenderSystem.overrideOverlaymap == null)
                {
                    this.GetTerrainTextureData(1);
                }
                if (this.m_WaterRenderSystem.overrideOverlaymap == null)
                {
                    this.GetWaterTextureData(1);
                }
            }
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
        public OverlayInfomodeSystem()
        {
        }
    }
}