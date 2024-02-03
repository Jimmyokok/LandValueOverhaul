using Game;
using Game.Simulation;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Rendering;
using Game.Zones;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using Game.Prefabs;
using SubObject = Game.Prefabs.SubObject;
using SubNet = Game.Prefabs.SubNet;
using TransformerData = Game.Prefabs.TransformerData;
using Layer = Game.Net.Layer;
using BepInEx.Logging;
using Colossal.Logging;

namespace LandValueOverhaul.Systems
{

    // Token: 0x020017E8 RID: 6120
    [CompilerGenerated]
    public class CustomBuildingInitializeSystem : GameSystemBase
    {
        // New method
        public static int GetUpkeep(int level, int residential_properties, float baseUpkeep, int lotSize, AreaType areaType, bool isStorage = false)
        {
            if (areaType == AreaType.Residential)
            {
                if (residential_properties < lotSize)
                {
                    return math.max(residential_properties, Mathf.RoundToInt(math.sqrt((float)level + 3f) * baseUpkeep * (float)residential_properties * 0.5f));
                }
                return math.max(lotSize, Mathf.RoundToInt(math.sqrt((float)level + 3f) * baseUpkeep * (float)lotSize * 0.5f));
            }
            return math.max(lotSize, Mathf.RoundToInt((float)level * baseUpkeep * (float)lotSize * (isStorage ? 0.5f : 1f)));
        }
        // Token: 0x0600671B RID: 26395 RVA: 0x00417EF4 File Offset: 0x004160F4
        [Preserve]
        protected override void OnCreate()
        {
            CustomBuildingInitializeSystem.log = LogManager.GetLogger("Simulation", true);
            base.OnCreate();
            logger.LogInfo("Building upkeep calculation bypassed!");
            this.m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
            this.m_PrefabQuery = base.GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Created>(),
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
            this.m_ConfigurationQuery = base.GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<BuildingConfigurationData>()
            });
            base.RequireForUpdate(this.m_PrefabQuery);
        }

        // Token: 0x0600671C RID: 26396 RVA: 0x00417FD0 File Offset: 0x004161D0
        [Preserve]
        protected override void OnUpdate()
        {
            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback);
            NativeArray<ArchetypeChunk> chunks = this.m_PrefabQuery.ToArchetypeChunkArray(Allocator.TempJob);
            this.__TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
            EntityTypeHandle _Unity_Entities_Entity_TypeHandle = this.__TypeHandle.__Unity_Entities_Entity_TypeHandle;
            this.__TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            ComponentTypeHandle<PrefabData> _Game_Prefabs_PrefabData_RO_ComponentTypeHandle = this.__TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle;
            this.__TypeHandle.__Game_Prefabs_BuildingData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            ComponentTypeHandle<BuildingData> _Game_Prefabs_BuildingData_RW_ComponentTypeHandle = this.__TypeHandle.__Game_Prefabs_BuildingData_RW_ComponentTypeHandle;
            this.__TypeHandle.__Game_Prefabs_BuildingExtensionData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            ComponentTypeHandle<BuildingExtensionData> _Game_Prefabs_BuildingExtensionData_RW_ComponentTypeHandle = this.__TypeHandle.__Game_Prefabs_BuildingExtensionData_RW_ComponentTypeHandle;
            this.__TypeHandle.__Game_Prefabs_BuildingTerraformData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            ComponentTypeHandle<BuildingTerraformData> _Game_Prefabs_BuildingTerraformData_RW_ComponentTypeHandle = this.__TypeHandle.__Game_Prefabs_BuildingTerraformData_RW_ComponentTypeHandle;
            this.__TypeHandle.__Game_Prefabs_ConsumptionData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            ComponentTypeHandle<ConsumptionData> _Game_Prefabs_ConsumptionData_RW_ComponentTypeHandle = this.__TypeHandle.__Game_Prefabs_ConsumptionData_RW_ComponentTypeHandle;
            this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            ComponentTypeHandle<ObjectGeometryData> _Game_Prefabs_ObjectGeometryData_RW_ComponentTypeHandle = this.__TypeHandle.__Game_Prefabs_ObjectGeometryData_RW_ComponentTypeHandle;
            this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            ComponentTypeHandle<SpawnableBuildingData> _Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle = this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle;
            this.__TypeHandle.__Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            ComponentTypeHandle<SignatureBuildingData> _Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle = this.__TypeHandle.__Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle;
            this.__TypeHandle.__Game_Prefabs_PlaceableObjectData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            ComponentTypeHandle<PlaceableObjectData> _Game_Prefabs_PlaceableObjectData_RW_ComponentTypeHandle = this.__TypeHandle.__Game_Prefabs_PlaceableObjectData_RW_ComponentTypeHandle;
            this.__TypeHandle.__Game_Prefabs_ServiceUpgradeData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            ComponentTypeHandle<ServiceUpgradeData> _Game_Prefabs_ServiceUpgradeData_RO_ComponentTypeHandle = this.__TypeHandle.__Game_Prefabs_ServiceUpgradeData_RO_ComponentTypeHandle;
            this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            ComponentTypeHandle<BuildingPropertyData> _Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle = this.__TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle;
            this.__TypeHandle.__Game_Prefabs_WaterPoweredData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            ComponentTypeHandle<WaterPoweredData> _Game_Prefabs_WaterPoweredData_RO_ComponentTypeHandle = this.__TypeHandle.__Game_Prefabs_WaterPoweredData_RO_ComponentTypeHandle;
            this.__TypeHandle.__Game_Prefabs_SewageOutletData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            ComponentTypeHandle<SewageOutletData> _Game_Prefabs_SewageOutletData_RO_ComponentTypeHandle = this.__TypeHandle.__Game_Prefabs_SewageOutletData_RO_ComponentTypeHandle;
            this.__TypeHandle.__Game_Prefabs_ServiceUpgradeBuilding_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
            BufferTypeHandle<ServiceUpgradeBuilding> _Game_Prefabs_ServiceUpgradeBuilding_RO_BufferTypeHandle = this.__TypeHandle.__Game_Prefabs_ServiceUpgradeBuilding_RO_BufferTypeHandle;
            this.__TypeHandle.__Game_Simulation_CollectedServiceBuildingBudgetData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            ComponentTypeHandle<CollectedServiceBuildingBudgetData> _Game_Simulation_CollectedServiceBuildingBudgetData_RO_ComponentTypeHandle = this.__TypeHandle.__Game_Simulation_CollectedServiceBuildingBudgetData_RO_ComponentTypeHandle;
            this.__TypeHandle.__Game_Prefabs_ServiceUpkeepData_RW_BufferTypeHandle.Update(ref base.CheckedStateRef);
            BufferTypeHandle<ServiceUpkeepData> _Game_Prefabs_ServiceUpkeepData_RW_BufferTypeHandle = this.__TypeHandle.__Game_Prefabs_ServiceUpkeepData_RW_BufferTypeHandle;
            this.__TypeHandle.__Game_Prefabs_ZoneData_RW_ComponentLookup.Update(ref base.CheckedStateRef);
            ComponentLookup<ZoneData> _Game_Prefabs_ZoneData_RW_ComponentLookup = this.__TypeHandle.__Game_Prefabs_ZoneData_RW_ComponentLookup;
            this.__TypeHandle.__Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            ComponentLookup<ZoneServiceConsumptionData> _Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup = this.__TypeHandle.__Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup;
            base.CompleteDependency();
            for (int i = 0; i < chunks.Length; i++)
            {
                ArchetypeChunk archetypeChunk = chunks[i];
                NativeArray<PrefabData> nativeArray = archetypeChunk.GetNativeArray<PrefabData>(ref _Game_Prefabs_PrefabData_RO_ComponentTypeHandle);
                NativeArray<ObjectGeometryData> nativeArray2 = archetypeChunk.GetNativeArray<ObjectGeometryData>(ref _Game_Prefabs_ObjectGeometryData_RW_ComponentTypeHandle);
                NativeArray<BuildingData> nativeArray3 = archetypeChunk.GetNativeArray<BuildingData>(ref _Game_Prefabs_BuildingData_RW_ComponentTypeHandle);
                NativeArray<BuildingExtensionData> nativeArray4 = archetypeChunk.GetNativeArray<BuildingExtensionData>(ref _Game_Prefabs_BuildingExtensionData_RW_ComponentTypeHandle);
                NativeArray<ConsumptionData> nativeArray5 = archetypeChunk.GetNativeArray<ConsumptionData>(ref _Game_Prefabs_ConsumptionData_RW_ComponentTypeHandle);
                NativeArray<SpawnableBuildingData> nativeArray6 = archetypeChunk.GetNativeArray<SpawnableBuildingData>(ref _Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle);
                NativeArray<PlaceableObjectData> nativeArray7 = archetypeChunk.GetNativeArray<PlaceableObjectData>(ref _Game_Prefabs_PlaceableObjectData_RW_ComponentTypeHandle);
                NativeArray<ServiceUpgradeData> nativeArray8 = archetypeChunk.GetNativeArray<ServiceUpgradeData>(ref _Game_Prefabs_ServiceUpgradeData_RO_ComponentTypeHandle);
                NativeArray<BuildingPropertyData> nativeArray9 = archetypeChunk.GetNativeArray<BuildingPropertyData>(ref _Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle);
                BufferAccessor<ServiceUpgradeBuilding> bufferAccessor = archetypeChunk.GetBufferAccessor<ServiceUpgradeBuilding>(ref _Game_Prefabs_ServiceUpgradeBuilding_RO_BufferTypeHandle);
                BufferAccessor<ServiceUpkeepData> bufferAccessor2 = archetypeChunk.GetBufferAccessor<ServiceUpkeepData>(ref _Game_Prefabs_ServiceUpkeepData_RW_BufferTypeHandle);
                NativeArray<Entity> nativeArray10 = archetypeChunk.GetNativeArray(_Unity_Entities_Entity_TypeHandle);
                bool flag = archetypeChunk.Has<CollectedServiceBuildingBudgetData>(ref _Game_Simulation_CollectedServiceBuildingBudgetData_RO_ComponentTypeHandle);
                bool flag2 = archetypeChunk.Has<SignatureBuildingData>(ref _Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle);
                bool flag3 = archetypeChunk.Has<WaterPoweredData>(ref _Game_Prefabs_WaterPoweredData_RO_ComponentTypeHandle);
                bool flag4 = archetypeChunk.Has<SewageOutletData>(ref _Game_Prefabs_SewageOutletData_RO_ComponentTypeHandle);
                if (nativeArray3.Length != 0)
                {
                    NativeArray<BuildingTerraformData> nativeArray11 = archetypeChunk.GetNativeArray<BuildingTerraformData>(ref _Game_Prefabs_BuildingTerraformData_RW_ComponentTypeHandle);
                    for (int j = 0; j < nativeArray3.Length; j++)
                    {
                        BuildingPrefab prefab = this.m_PrefabSystem.GetPrefab<BuildingPrefab>(nativeArray[j]);
                        BuildingTerraformOverride component = prefab.GetComponent<BuildingTerraformOverride>();
                        ObjectGeometryData value = nativeArray2[j];
                        BuildingTerraformData value2 = nativeArray11[j];
                        BuildingData value3 = nativeArray3[j];
                        this.InitializeLotSize(prefab, component, ref value, ref value2, ref value3);
                        if (nativeArray6.Length != 0 && !flag2)
                        {
                            value.m_Flags |= Game.Objects.GeometryFlags.DeleteOverridden;
                        }
                        else
                        {
                            value.m_Flags &= ~Game.Objects.GeometryFlags.Overridable;
                            value.m_Flags |= Game.Objects.GeometryFlags.OverrideZone;
                        }
                        if (flag3)
                        {
                            value.m_Flags |= Game.Objects.GeometryFlags.CanSubmerge;
                        }
                        else if (flag4 && prefab.GetComponent<SewageOutlet>().m_AllowSubmerged)
                        {
                            value.m_Flags |= Game.Objects.GeometryFlags.CanSubmerge;
                        }
                        value.m_Flags &= ~Game.Objects.GeometryFlags.Brushable;
                        value.m_Flags |= (Game.Objects.GeometryFlags.ExclusiveGround | Game.Objects.GeometryFlags.WalkThrough | Game.Objects.GeometryFlags.OccupyZone | Game.Objects.GeometryFlags.HasLot);
                        nativeArray2[j] = value;
                        nativeArray11[j] = value2;
                        nativeArray3[j] = value3;
                    }
                }
                if (nativeArray4.Length != 0)
                {
                    NativeArray<BuildingTerraformData> nativeArray12 = archetypeChunk.GetNativeArray<BuildingTerraformData>(ref _Game_Prefabs_BuildingTerraformData_RW_ComponentTypeHandle);
                    for (int k = 0; k < nativeArray4.Length; k++)
                    {
                        BuildingExtensionPrefab prefab2 = this.m_PrefabSystem.GetPrefab<BuildingExtensionPrefab>(nativeArray[k]);
                        ObjectGeometryData objectGeometryData = nativeArray2[k];
                        Bounds2 xz2;
                        if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Standing) != Game.Objects.GeometryFlags.None)
                        {
                            float2 xz = objectGeometryData.m_Pivot.xz;
                            float2 rhs = objectGeometryData.m_LegSize.xz * 0.5f;
                            xz2 = new Bounds2(xz - rhs, xz + rhs);
                        }
                        else
                        {
                            xz2 = objectGeometryData.m_Bounds.xz;
                        }
                        objectGeometryData.m_Bounds.min = math.min(objectGeometryData.m_Bounds.min, new float3(-0.5f, 0f, -0.5f));
                        objectGeometryData.m_Bounds.max = math.max(objectGeometryData.m_Bounds.max, new float3(0.5f, 5f, 0.5f));
                        objectGeometryData.m_Flags &= ~(Game.Objects.GeometryFlags.Overridable | Game.Objects.GeometryFlags.Brushable);
                        objectGeometryData.m_Flags |= (Game.Objects.GeometryFlags.ExclusiveGround | Game.Objects.GeometryFlags.WalkThrough | Game.Objects.GeometryFlags.OccupyZone | Game.Objects.GeometryFlags.HasLot);
                        BuildingExtensionData buildingExtensionData = nativeArray4[k];
                        buildingExtensionData.m_Position = prefab2.m_Position;
                        buildingExtensionData.m_LotSize = prefab2.m_OverrideLotSize;
                        buildingExtensionData.m_External = prefab2.m_ExternalLot;
                        if (prefab2.m_OverrideHeight > 0f)
                        {
                            objectGeometryData.m_Bounds.max.y = prefab2.m_OverrideHeight;
                        }
                        Bounds2 bounds;
                        if (math.all(buildingExtensionData.m_LotSize > 0))
                        {
                            float2 lhs = buildingExtensionData.m_LotSize;
                            lhs *= 8f;
                            bounds = new Bounds2(lhs * -0.5f, lhs * 0.5f);
                            lhs -= 0.4f;
                            objectGeometryData.m_Bounds.min.xz = lhs * -0.5f;
                            objectGeometryData.m_Bounds.max.xz = lhs * 0.5f;
                            if (bufferAccessor.Length != 0)
                            {
                                objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.OverrideZone;
                            }
                        }
                        else
                        {
                            Bounds3 bounds2 = objectGeometryData.m_Bounds;
                            bounds = objectGeometryData.m_Bounds.xz;
                            if (bufferAccessor.Length != 0)
                            {
                                DynamicBuffer<ServiceUpgradeBuilding> dynamicBuffer = bufferAccessor[k];
                                for (int l = 0; l < dynamicBuffer.Length; l++)
                                {
                                    ServiceUpgradeBuilding serviceUpgradeBuilding = dynamicBuffer[l];
                                    BuildingPrefab prefab3 = this.m_PrefabSystem.GetPrefab<BuildingPrefab>(serviceUpgradeBuilding.m_Building);
                                    float2 @float = new int2(prefab3.m_LotWidth, prefab3.m_LotDepth);
                                    @float *= 8f;
                                    float2 lhs2 = @float;
                                    @float -= 0.4f;
                                    StandingObject standingObject;
                                    if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Standing) == Game.Objects.GeometryFlags.None && prefab3.TryGet<StandingObject>(out standingObject))
                                    {
                                        @float = standingObject.m_LegSize.xz;
                                        lhs2 = standingObject.m_LegSize.xz;
                                        if (standingObject.m_CircularLeg)
                                        {
                                            objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.Circular;
                                        }
                                    }
                                    if (l == 0)
                                    {
                                        bounds2.xz = new Bounds2(@float * -0.5f, @float * 0.5f) - prefab2.m_Position.xz;
                                        bounds = new Bounds2(lhs2 * -0.5f, lhs2 * 0.5f) - prefab2.m_Position.xz;
                                    }
                                    else
                                    {
                                        bounds2.xz &= new Bounds2(@float * -0.5f, @float * 0.5f) - prefab2.m_Position.xz;
                                        bounds &= new Bounds2(lhs2 * -0.5f, lhs2 * 0.5f) - prefab2.m_Position.xz;
                                    }
                                }
                                objectGeometryData.m_Bounds.xz = bounds2.xz;
                                objectGeometryData.m_Flags |= Game.Objects.GeometryFlags.OverrideZone;
                            }
                            float2 float2 = math.min(-bounds2.min.xz, bounds2.max.xz) * 0.25f - 0.01f;
                            buildingExtensionData.m_LotSize.x = math.max(1, Mathf.CeilToInt(float2.x));
                            buildingExtensionData.m_LotSize.y = math.max(1, Mathf.CeilToInt(float2.y));
                        }
                        if (buildingExtensionData.m_External)
                        {
                            float2 float3 = buildingExtensionData.m_LotSize;
                            float3 *= 8f;
                            objectGeometryData.m_Layers |= MeshLayer.Default;
                            objectGeometryData.m_MinLod = math.min(objectGeometryData.m_MinLod, RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float3(float3.x, 0f, float3.y))));
                        }
                        if (nativeArray12.Length != 0)
                        {
                            BuildingTerraformOverride component2 = prefab2.GetComponent<BuildingTerraformOverride>();
                            BuildingTerraformData value4 = nativeArray12[k];
                            CustomBuildingInitializeSystem.InitializeTerraformData(component2, ref value4, bounds, xz2);
                            nativeArray12[k] = value4;
                        }
                        objectGeometryData.m_Size = math.max(ObjectUtils.GetSize(objectGeometryData.m_Bounds), new float3(1f, 5f, 1f));
                        nativeArray2[k] = objectGeometryData;
                        nativeArray4[k] = buildingExtensionData;
                    }
                }
                if (nativeArray6.Length != 0)
                {
                    for (int m = 0; m < nativeArray6.Length; m++)
                    {
                        Entity e = nativeArray10[m];
                        BuildingPrefab prefab4 = this.m_PrefabSystem.GetPrefab<BuildingPrefab>(nativeArray[m]);
                        BuildingPropertyData buildingPropertyData = (nativeArray9.Length != 0) ? nativeArray9[m] : default(BuildingPropertyData);
                        SpawnableBuildingData spawnableBuildingData = nativeArray6[m];
                        int residentialProperties = buildingPropertyData.m_ResidentialProperties;
                        if (spawnableBuildingData.m_ZonePrefab != Entity.Null)
                        {
                            Entity zonePrefab = spawnableBuildingData.m_ZonePrefab;
                            ZoneData zoneData = _Game_Prefabs_ZoneData_RW_ComponentLookup[zonePrefab];
                            if (!flag2)
                            {
                                entityCommandBuffer.SetSharedComponent<BuildingSpawnGroupData>(e, new BuildingSpawnGroupData(zoneData.m_ZoneType));
                                ushort num = (ushort)math.clamp(Mathf.CeilToInt(nativeArray2[m].m_Size.y), 0, 65535);
                                if (spawnableBuildingData.m_Level == 1)
                                {
                                    if (prefab4.m_LotWidth == 1 && (zoneData.m_ZoneFlags & ZoneFlags.SupportNarrow) == (ZoneFlags)0)
                                    {
                                        zoneData.m_ZoneFlags |= ZoneFlags.SupportNarrow;
                                        _Game_Prefabs_ZoneData_RW_ComponentLookup[zonePrefab] = zoneData;
                                    }
                                    if (prefab4.m_AccessType == BuildingAccessType.LeftCorner && (zoneData.m_ZoneFlags & ZoneFlags.SupportLeftCorner) == (ZoneFlags)0)
                                    {
                                        zoneData.m_ZoneFlags |= ZoneFlags.SupportLeftCorner;
                                        _Game_Prefabs_ZoneData_RW_ComponentLookup[zonePrefab] = zoneData;
                                    }
                                    if (prefab4.m_AccessType == BuildingAccessType.RightCorner && (zoneData.m_ZoneFlags & ZoneFlags.SupportRightCorner) == (ZoneFlags)0)
                                    {
                                        zoneData.m_ZoneFlags |= ZoneFlags.SupportRightCorner;
                                        _Game_Prefabs_ZoneData_RW_ComponentLookup[zonePrefab] = zoneData;
                                    }
                                    if (prefab4.m_AccessType == BuildingAccessType.Front && prefab4.m_LotWidth <= 3 && prefab4.m_LotDepth <= 2)
                                    {
                                        if ((prefab4.m_LotWidth == 1 || prefab4.m_LotWidth == 3) && num < zoneData.m_MinOddHeight)
                                        {
                                            zoneData.m_MinOddHeight = num;
                                            _Game_Prefabs_ZoneData_RW_ComponentLookup[zonePrefab] = zoneData;
                                        }
                                        if ((prefab4.m_LotWidth == 1 || prefab4.m_LotWidth == 2) && num < zoneData.m_MinEvenHeight)
                                        {
                                            zoneData.m_MinEvenHeight = num;
                                            _Game_Prefabs_ZoneData_RW_ComponentLookup[zonePrefab] = zoneData;
                                        }
                                    }
                                }
                                if (num > zoneData.m_MaxHeight)
                                {
                                    zoneData.m_MaxHeight = num;
                                    _Game_Prefabs_ZoneData_RW_ComponentLookup[zonePrefab] = zoneData;
                                }
                            }
                            int level = (int)spawnableBuildingData.m_Level;
                            BuildingData buildingData = nativeArray3[m];
                            int lotSize = buildingData.m_LotSize.x * buildingData.m_LotSize.y;
                            if (nativeArray5.Length != 0 && !prefab4.Has<ServiceConsumption>() && _Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup.HasComponent(zonePrefab))
                            {
                                ZoneServiceConsumptionData zoneServiceConsumptionData = _Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup[zonePrefab];
                                ref ConsumptionData ptr = ref nativeArray5.ElementAt(m);
                                if (zoneData.m_AreaType == AreaType.Residential)
                                {
                                    if (flag2)
                                    {
                                        ptr.m_Upkeep = GetUpkeep(2, residentialProperties, zoneServiceConsumptionData.m_Upkeep, lotSize, zoneData.m_AreaType, false);
                                    }
                                    else
                                    {
                                        ptr.m_Upkeep = GetUpkeep(level, residentialProperties, zoneServiceConsumptionData.m_Upkeep, lotSize, zoneData.m_AreaType, false);
                                    }
                                }
                                else
                                {
                                    bool isStorage = buildingPropertyData.m_AllowedStored > Resource.NoResource;
                                    if (flag2)
                                    {
                                        ptr.m_Upkeep = GetUpkeep(2, residentialProperties, zoneServiceConsumptionData.m_Upkeep, lotSize, zoneData.m_AreaType, false);
                                    }
                                    else
                                    {
                                        ptr.m_Upkeep = GetUpkeep(level, residentialProperties, zoneServiceConsumptionData.m_Upkeep, lotSize, zoneData.m_AreaType, isStorage);
                                    }
                                }
                            }
                        }
                    }
                }
                if (nativeArray7.Length != 0)
                {
                    if (nativeArray8.Length != 0)
                    {
                        for (int n = 0; n < nativeArray7.Length; n++)
                        {
                            PlaceableObjectData value5 = nativeArray7[n];
                            ServiceUpgradeData serviceUpgradeData = nativeArray8[n];
                            if (nativeArray3.Length != 0)
                            {
                                value5.m_Flags |= Game.Objects.PlacementFlags.OwnerSide;
                            }
                            value5.m_ConstructionCost = serviceUpgradeData.m_UpgradeCost;
                            nativeArray7[n] = value5;
                        }
                    }
                    else
                    {
                        for (int num2 = 0; num2 < nativeArray7.Length; num2++)
                        {
                            PlaceableObjectData value6 = nativeArray7[num2];
                            value6.m_Flags |= Game.Objects.PlacementFlags.RoadSide;
                            nativeArray7[num2] = value6;
                        }
                    }
                }
                if (flag)
                {
                    for (int num3 = 0; num3 < nativeArray10.Length; num3++)
                    {
                        if (nativeArray5.Length != 0 && nativeArray5[num3].m_Upkeep > 0)
                        {
                            bool flag5 = false;
                            DynamicBuffer<ServiceUpkeepData> dynamicBuffer2 = bufferAccessor2[num3];
                            for (int num4 = 0; num4 < dynamicBuffer2.Length; num4++)
                            {
                                if (dynamicBuffer2[num4].m_Upkeep.m_Resource == Resource.Money)
                                {
                                    CustomBuildingInitializeSystem.log.WarnFormat("Warning: {0} has monetary upkeep in both ConsumptionData and CityServiceUpkeep", this.m_PrefabSystem.GetPrefab<PrefabBase>(nativeArray10[num3]).name);
                                    ServiceUpkeepData value7 = dynamicBuffer2[num4];
                                    value7.m_Upkeep.m_Amount = value7.m_Upkeep.m_Amount + nativeArray5[num3].m_Upkeep;
                                    dynamicBuffer2[num4] = value7;
                                    flag5 = true;
                                }
                            }
                            if (!flag5)
                            {
                                dynamicBuffer2.Add(new ServiceUpkeepData
                                {
                                    m_ScaleWithUsage = false,
                                    m_Upkeep = new ResourceStack
                                    {
                                        m_Amount = nativeArray5[num3].m_Upkeep,
                                        m_Resource = Resource.Money
                                    }
                                });
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
                            bufferAccessor2[num5].Add(new ServiceUpkeepData
                            {
                                m_ScaleWithUsage = false,
                                m_Upkeep = new ResourceStack
                                {
                                    m_Amount = nativeArray5[num5].m_Upkeep,
                                    m_Resource = Resource.Money
                                }
                            });
                        }
                    }
                }
            }
            this.__TypeHandle.__Game_Prefabs_SubObject_RO_BufferLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_AudioEffectData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_AudioSpotData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_AudioSourceData_RO_BufferLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_VFXData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_EffectData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_MeshData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_SpawnLocationData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_Effect_RW_BufferTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_BuildingData_RW_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_SubMesh_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_SubObject_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_SubNet_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_CargoTransportStationData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_PublicTransportStationData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ParkingFacilityData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_TransformerData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_WastewaterTreatmentPlantData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_SewageOutletData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_WaterTowerData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_WaterPumpingStationData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ConsumptionData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ExtractorFacilityData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_ServiceUpgradeData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            CustomBuildingInitializeSystem.FindConnectionRequirementsJob jobData = default(CustomBuildingInitializeSystem.FindConnectionRequirementsJob);
            jobData.m_SpawnableBuildingDataType = this.__TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle;
            jobData.m_ServiceUpgradeDataType = this.__TypeHandle.__Game_Prefabs_ServiceUpgradeData_RO_ComponentTypeHandle;
            jobData.m_ExtractorFacilityDataType = this.__TypeHandle.__Game_Prefabs_ExtractorFacilityData_RO_ComponentTypeHandle;
            jobData.m_ConsumptionDataType = this.__TypeHandle.__Game_Prefabs_ConsumptionData_RO_ComponentTypeHandle;
            jobData.m_WorkplaceDataType = this.__TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentTypeHandle;
            jobData.m_WaterPumpingStationDataType = this.__TypeHandle.__Game_Prefabs_WaterPumpingStationData_RO_ComponentTypeHandle;
            jobData.m_WaterTowerDataType = this.__TypeHandle.__Game_Prefabs_WaterTowerData_RO_ComponentTypeHandle;
            jobData.m_SewageOutletDataType = this.__TypeHandle.__Game_Prefabs_SewageOutletData_RO_ComponentTypeHandle;
            jobData.m_WastewaterTreatmentPlantDataType = this.__TypeHandle.__Game_Prefabs_WastewaterTreatmentPlantData_RO_ComponentTypeHandle;
            jobData.m_TransformerDataType = this.__TypeHandle.__Game_Prefabs_TransformerData_RO_ComponentTypeHandle;
            jobData.m_ParkingFacilityDataType = this.__TypeHandle.__Game_Prefabs_ParkingFacilityData_RO_ComponentTypeHandle;
            jobData.m_PublicTransportStationDataType = this.__TypeHandle.__Game_Prefabs_PublicTransportStationData_RO_ComponentTypeHandle;
            jobData.m_CargoTransportStationDataType = this.__TypeHandle.__Game_Prefabs_CargoTransportStationData_RO_ComponentTypeHandle;
            jobData.m_SubNetType = this.__TypeHandle.__Game_Prefabs_SubNet_RO_BufferTypeHandle;
            jobData.m_SubObjectType = this.__TypeHandle.__Game_Prefabs_SubObject_RO_BufferTypeHandle;
            jobData.m_SubMeshType = this.__TypeHandle.__Game_Prefabs_SubMesh_RO_BufferTypeHandle;
            jobData.m_BuildingDataType = this.__TypeHandle.__Game_Prefabs_BuildingData_RW_ComponentTypeHandle;
            jobData.m_EffectType = this.__TypeHandle.__Game_Prefabs_Effect_RW_BufferTypeHandle;
            jobData.m_NetData = this.__TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup;
            jobData.m_SpawnLocationData = this.__TypeHandle.__Game_Prefabs_SpawnLocationData_RO_ComponentLookup;
            jobData.m_MeshData = this.__TypeHandle.__Game_Prefabs_MeshData_RO_ComponentLookup;
            jobData.m_EffectData = this.__TypeHandle.__Game_Prefabs_EffectData_RO_ComponentLookup;
            jobData.m_VFXData = this.__TypeHandle.__Game_Prefabs_VFXData_RO_ComponentLookup;
            jobData.m_AudioSourceData = this.__TypeHandle.__Game_Prefabs_AudioSourceData_RO_BufferLookup;
            jobData.m_AudioSpotData = this.__TypeHandle.__Game_Prefabs_AudioSpotData_RO_ComponentLookup;
            jobData.m_AudioEffectData = this.__TypeHandle.__Game_Prefabs_AudioEffectData_RO_ComponentLookup;
            jobData.m_SubObjects = this.__TypeHandle.__Game_Prefabs_SubObject_RO_BufferLookup;
            jobData.m_RandomSeed = RandomSeed.Next();
            jobData.m_Chunks = chunks;
            jobData.m_BuildingConfigurationData = this.m_ConfigurationQuery.GetSingleton<BuildingConfigurationData>();
            jobData.Schedule(chunks.Length, 1, default(JobHandle)).Complete();
            chunks.Dispose();
            entityCommandBuffer.Playback(base.EntityManager);
            entityCommandBuffer.Dispose();
        }

        // Token: 0x0600671D RID: 26397 RVA: 0x004194B8 File Offset: 0x004176B8
        private void InitializeLotSize(BuildingPrefab buildingPrefab, BuildingTerraformOverride terraformOverride, ref ObjectGeometryData objectGeometryData, ref BuildingTerraformData buildingTerraformData, ref BuildingData buildingData)
        {
            float2 @float = new float2((float)buildingPrefab.m_LotWidth, (float)buildingPrefab.m_LotDepth);
            @float *= 8f;
            Bounds2 xz2;
            if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Standing) != Game.Objects.GeometryFlags.None)
            {
                buildingData.m_LotSize.x = Mathf.RoundToInt(objectGeometryData.m_LegSize.x / 8f);
                buildingData.m_LotSize.y = Mathf.RoundToInt(objectGeometryData.m_LegSize.z / 8f);
                float2 xz = objectGeometryData.m_Pivot.xz;
                float2 rhs = objectGeometryData.m_LegSize.xz * 0.5f;
                xz2 = new Bounds2(xz - rhs, xz + rhs);
            }
            else
            {
                buildingData.m_LotSize = new int2(buildingPrefab.m_LotWidth, buildingPrefab.m_LotDepth);
                xz2 = objectGeometryData.m_Bounds.xz;
            }
            Bounds2 bounds;
            bounds.max = buildingData.m_LotSize * 4;
            bounds.min = -bounds.max;
            CustomBuildingInitializeSystem.InitializeTerraformData(terraformOverride, ref buildingTerraformData, bounds, xz2);
            objectGeometryData.m_Layers |= MeshLayer.Default;
            objectGeometryData.m_MinLod = math.min(objectGeometryData.m_MinLod, RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float3(@float.x, 0f, @float.y))));
            switch (buildingPrefab.m_AccessType)
            {
                case BuildingAccessType.LeftCorner:
                    buildingData.m_Flags |= BuildingFlags.LeftAccess;
                    break;
                case BuildingAccessType.RightCorner:
                    buildingData.m_Flags |= BuildingFlags.RightAccess;
                    break;
                case BuildingAccessType.LeftAndRightCorner:
                    buildingData.m_Flags |= (BuildingFlags.LeftAccess | BuildingFlags.RightAccess);
                    break;
                case BuildingAccessType.LeftAndBackCorner:
                    buildingData.m_Flags |= (BuildingFlags.LeftAccess | BuildingFlags.BackAccess);
                    break;
                case BuildingAccessType.RightAndBackCorner:
                    buildingData.m_Flags |= (BuildingFlags.RightAccess | BuildingFlags.BackAccess);
                    break;
                case BuildingAccessType.FrontAndBack:
                    buildingData.m_Flags |= BuildingFlags.BackAccess;
                    break;
                case BuildingAccessType.All:
                    buildingData.m_Flags |= (BuildingFlags.LeftAccess | BuildingFlags.RightAccess | BuildingFlags.BackAccess);
                    break;
            }
            if (math.any(objectGeometryData.m_Size.xz > @float + 0.5f))
            {
                CustomBuildingInitializeSystem.log.WarnFormat("Building geometry doesn't fit inside the lot ({0}): {1}m x {2}m ({3}x{4})", buildingPrefab.name, objectGeometryData.m_Size.x, objectGeometryData.m_Size.z, buildingData.m_LotSize.x, buildingData.m_LotSize.y);
            }
            @float -= 0.4f;
            objectGeometryData.m_Size.xz = @float;
            objectGeometryData.m_Size.y = math.max(objectGeometryData.m_Size.y, 5f);
            objectGeometryData.m_Bounds.min.xz = @float * -0.5f;
            objectGeometryData.m_Bounds.min.y = math.min(objectGeometryData.m_Bounds.min.y, 0f);
            objectGeometryData.m_Bounds.max.xz = @float * 0.5f;
            objectGeometryData.m_Bounds.max.y = math.max(objectGeometryData.m_Bounds.max.y, 5f);
        }

        // Token: 0x0600671E RID: 26398 RVA: 0x004197E0 File Offset: 0x004179E0
        public static void InitializeTerraformData(BuildingTerraformOverride terraformOverride, ref BuildingTerraformData buildingTerraformData, Bounds2 lotBounds, Bounds2 flatBounds)
        {
            float3 rhs = new float3(1f, 0f, 1f);
            float3 rhs2 = new float3(1f, 0f, 1f);
            float3 rhs3 = new float3(1f, 0f, 1f);
            float3 rhs4 = new float3(1f, 0f, 1f);
            buildingTerraformData.m_Smooth.xy = lotBounds.min;
            buildingTerraformData.m_Smooth.zw = lotBounds.max;
            if (terraformOverride != null)
            {
                flatBounds.min += terraformOverride.m_LevelMinOffset;
                flatBounds.max += terraformOverride.m_LevelMaxOffset;
                rhs.x = terraformOverride.m_LevelBackRight.x;
                rhs.z = terraformOverride.m_LevelFrontRight.x;
                rhs2.x = terraformOverride.m_LevelBackRight.y;
                rhs2.z = terraformOverride.m_LevelBackLeft.y;
                rhs3.x = terraformOverride.m_LevelBackLeft.x;
                rhs3.z = terraformOverride.m_LevelFrontLeft.x;
                rhs4.x = terraformOverride.m_LevelFrontRight.y;
                rhs4.z = terraformOverride.m_LevelFrontLeft.y;
                buildingTerraformData.m_Smooth.xy = buildingTerraformData.m_Smooth.xy + terraformOverride.m_SmoothMinOffset;
                buildingTerraformData.m_Smooth.zw = buildingTerraformData.m_Smooth.zw + terraformOverride.m_SmoothMaxOffset;
                buildingTerraformData.m_HeightOffset = terraformOverride.m_HeightOffset;
                buildingTerraformData.m_DontRaise = terraformOverride.m_DontRaise;
                buildingTerraformData.m_DontLower = terraformOverride.m_DontLower;
            }
            float3 @float = flatBounds.min.x + rhs;
            float3 float2 = flatBounds.min.y + rhs2;
            float3 float3 = flatBounds.max.x - rhs3;
            float3 float4 = flatBounds.max.y - rhs4;
            float3 x = (@float + float3) * 0.5f;
            float3 x2 = (float2 + float4) * 0.5f;
            buildingTerraformData.m_FlatX0 = math.min(@float, math.max(x, float3));
            buildingTerraformData.m_FlatZ0 = math.min(float2, math.max(x2, float4));
            buildingTerraformData.m_FlatX1 = math.max(float3, math.min(x, @float));
            buildingTerraformData.m_FlatZ1 = math.max(float4, math.min(x2, float2));
        }

        // Token: 0x0600671F RID: 26399 RVA: 0x00002E1D File Offset: 0x0000101D
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
        }

        // Token: 0x06006720 RID: 26400 RVA: 0x00419A67 File Offset: 0x00417C67
        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            this.__AssignQueries(ref base.CheckedStateRef);
            this.__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
        }

        // Token: 0x06006721 RID: 26401 RVA: 0x00006953 File Offset: 0x00004B53
        [Preserve]
        public CustomBuildingInitializeSystem()
        {
        }

        // Token: 0x0400B863 RID: 47203
        private static ILog log;

        // Token: 0x0400B864 RID: 47204
        private EntityQuery m_PrefabQuery;

        // Token: 0x0400B865 RID: 47205
        private EntityQuery m_ConfigurationQuery;

        // Token: 0x0400B866 RID: 47206
        private PrefabSystem m_PrefabSystem;

        // Token: 0x0400B867 RID: 47207
        private CustomBuildingInitializeSystem.TypeHandle __TypeHandle;

        private static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(MyPluginInfo.PLUGIN_NAME);

        // Token: 0x020017E9 RID: 6121
        [BurstCompile]
        private struct FindConnectionRequirementsJob : IJobParallelFor
        {
            // Token: 0x06006722 RID: 26402 RVA: 0x00419A8C File Offset: 0x00417C8C
            public void Execute(int index)
            {
                ArchetypeChunk archetypeChunk = this.m_Chunks[index];
                NativeArray<BuildingData> nativeArray = archetypeChunk.GetNativeArray<BuildingData>(ref this.m_BuildingDataType);
                BufferAccessor<SubMesh> bufferAccessor = archetypeChunk.GetBufferAccessor<SubMesh>(ref this.m_SubMeshType);
                BufferAccessor<SubObject> bufferAccessor2 = archetypeChunk.GetBufferAccessor<SubObject>(ref this.m_SubObjectType);
                BufferAccessor<Effect> bufferAccessor3 = archetypeChunk.GetBufferAccessor<Effect>(ref this.m_EffectType);
                if (archetypeChunk.Has<SpawnableBuildingData>(ref this.m_SpawnableBuildingDataType))
                {
                    for (int i = 0; i < nativeArray.Length; i++)
                    {
                        BuildingData value = nativeArray[i];
                        value.m_Flags |= (BuildingFlags.RequireRoad | BuildingFlags.RestrictedPedestrian | BuildingFlags.RestrictedCar);
                        if (bufferAccessor[i].Length == 0)
                        {
                            value.m_Flags |= BuildingFlags.ColorizeLot;
                        }
                        DynamicBuffer<SubObject> subObjects;
                        if (CollectionUtils.TryGet<SubObject>(bufferAccessor2, i, out subObjects))
                        {
                            this.CheckPropFlags(ref value.m_Flags, subObjects, 10);
                        }
                        nativeArray[i] = value;
                    }
                }
                else if (archetypeChunk.Has<ServiceUpgradeData>(ref this.m_ServiceUpgradeDataType) || archetypeChunk.Has<ExtractorFacilityData>(ref this.m_ExtractorFacilityDataType))
                {
                    for (int j = 0; j < nativeArray.Length; j++)
                    {
                        BuildingData value2 = nativeArray[j];
                        value2.m_Flags |= (BuildingFlags.NoRoadConnection | BuildingFlags.RestrictedPedestrian | BuildingFlags.RestrictedCar);
                        if (bufferAccessor[j].Length == 0)
                        {
                            value2.m_Flags |= BuildingFlags.ColorizeLot;
                        }
                        DynamicBuffer<SubObject> subObjects2;
                        if (CollectionUtils.TryGet<SubObject>(bufferAccessor2, j, out subObjects2))
                        {
                            this.CheckPropFlags(ref value2.m_Flags, subObjects2, 10);
                        }
                        nativeArray[j] = value2;
                    }
                }
                else
                {
                    NativeArray<ConsumptionData> nativeArray2 = archetypeChunk.GetNativeArray<ConsumptionData>(ref this.m_ConsumptionDataType);
                    NativeArray<WorkplaceData> nativeArray3 = archetypeChunk.GetNativeArray<WorkplaceData>(ref this.m_WorkplaceDataType);
                    BufferAccessor<SubNet> bufferAccessor4 = archetypeChunk.GetBufferAccessor<SubNet>(ref this.m_SubNetType);
                    bool flag = archetypeChunk.Has<WaterPumpingStationData>(ref this.m_WaterPumpingStationDataType);
                    bool flag2 = archetypeChunk.Has<WaterTowerData>(ref this.m_WaterTowerDataType);
                    bool flag3 = archetypeChunk.Has<SewageOutletData>(ref this.m_SewageOutletDataType);
                    bool flag4 = archetypeChunk.Has<WastewaterTreatmentPlantData>(ref this.m_WastewaterTreatmentPlantDataType);
                    bool flag5 = archetypeChunk.Has<TransformerData>(ref this.m_TransformerDataType);
                    bool flag6 = archetypeChunk.Has<ParkingFacilityData>(ref this.m_ParkingFacilityDataType);
                    bool flag7 = archetypeChunk.Has<PublicTransportStationData>(ref this.m_PublicTransportStationDataType);
                    bool flag8 = archetypeChunk.Has<CargoTransportStationData>(ref this.m_CargoTransportStationDataType);
                    BuildingFlags buildingFlags = (BuildingFlags)0U;
                    if (!flag6 && !flag7)
                    {
                        buildingFlags |= BuildingFlags.RestrictedPedestrian;
                    }
                    if (!flag6 && !flag8 && !flag7)
                    {
                        buildingFlags |= BuildingFlags.RestrictedCar;
                    }
                    for (int k = 0; k < nativeArray.Length; k++)
                    {
                        Layer layer = Layer.None;
                        Layer layer2 = Layer.None;
                        Layer layer3 = Layer.None;
                        if (nativeArray2.Length != 0)
                        {
                            ConsumptionData consumptionData = nativeArray2[k];
                            if (consumptionData.m_ElectricityConsumption > 0f)
                            {
                                layer |= Layer.PowerlineLow;
                            }
                            if (consumptionData.m_GarbageAccumulation > 0f)
                            {
                                layer |= Layer.Road;
                            }
                            if (consumptionData.m_WaterConsumption > 0f)
                            {
                                layer |= (Layer.WaterPipe | Layer.SewagePipe);
                            }
                        }
                        if (nativeArray3.Length != 0 && nativeArray3[k].m_MaxWorkers > 0)
                        {
                            layer |= Layer.Road;
                        }
                        if (flag || flag2)
                        {
                            layer |= Layer.WaterPipe;
                        }
                        if (flag3 || flag4)
                        {
                            layer |= Layer.SewagePipe;
                        }
                        if (flag5)
                        {
                            layer |= Layer.PowerlineLow;
                        }
                        if (layer != Layer.None && bufferAccessor4.Length != 0)
                        {
                            DynamicBuffer<SubNet> dynamicBuffer = bufferAccessor4[k];
                            for (int l = 0; l < dynamicBuffer.Length; l++)
                            {
                                SubNet subNet = dynamicBuffer[l];
                                if (this.m_NetData.HasComponent(subNet.m_Prefab))
                                {
                                    NetData netData = this.m_NetData[subNet.m_Prefab];
                                    if ((netData.m_RequiredLayers & Layer.Road) == Layer.None)
                                    {
                                        layer2 |= (netData.m_RequiredLayers | netData.m_LocalConnectLayers);
                                        layer3 |= netData.m_RequiredLayers;
                                    }
                                }
                            }
                        }
                        BuildingData value3 = nativeArray[k];
                        value3.m_Flags |= buildingFlags;
                        if (layer != Layer.None && layer2 == Layer.None)
                        {
                            value3.m_Flags |= BuildingFlags.RequireRoad;
                        }
                        if ((layer3 & Layer.PowerlineLow) != Layer.None)
                        {
                            value3.m_Flags |= BuildingFlags.HasLowVoltageNode;
                        }
                        if ((layer3 & Layer.WaterPipe) != Layer.None)
                        {
                            value3.m_Flags |= BuildingFlags.HasWaterNode;
                        }
                        if ((layer3 & Layer.SewagePipe) != Layer.None)
                        {
                            value3.m_Flags |= BuildingFlags.HasSewageNode;
                        }
                        if (bufferAccessor[k].Length == 0)
                        {
                            value3.m_Flags |= BuildingFlags.ColorizeLot;
                        }
                        DynamicBuffer<SubObject> subObjects3;
                        if (CollectionUtils.TryGet<SubObject>(bufferAccessor2, k, out subObjects3))
                        {
                            this.CheckPropFlags(ref value3.m_Flags, subObjects3, 10);
                        }
                        nativeArray[k] = value3;
                    }
                }
                Unity.Mathematics.Random random = this.m_RandomSeed.GetRandom(index);
                int m = 0;
            IL_84B:
                while (m < bufferAccessor3.Length)
                {
                    DynamicBuffer<Effect> dynamicBuffer2 = bufferAccessor3[m];
                    DynamicBuffer<SubMesh> dynamicBuffer3 = bufferAccessor[m];
                    bool2 @bool = new bool2(false, nativeArray.Length == 0);
                    for (int n = 0; n < dynamicBuffer2.Length; n++)
                    {
                        EffectData effectData;
                        if (this.m_EffectData.TryGetComponent(dynamicBuffer2[n].m_Effect, out effectData) && (effectData.m_Flags.m_RequiredFlags & EffectConditionFlags.Collapsing) != EffectConditionFlags.None)
                        {
                            @bool.x |= this.m_VFXData.HasComponent(dynamicBuffer2[n].m_Effect);
                            @bool.y |= this.m_AudioSourceData.HasBuffer(dynamicBuffer2[n].m_Effect);
                            if (math.all(@bool))
                            {
                                m++;
                                goto IL_84B;
                            }
                        }
                    }
                    for (int num = 0; num < dynamicBuffer3.Length; num++)
                    {
                        SubMesh subMesh = dynamicBuffer3[num];
                        MeshData meshData;
                        if (this.m_MeshData.TryGetComponent(subMesh.m_SubMesh, out meshData))
                        {
                            float2 @float = MathUtils.Center(meshData.m_Bounds.xz);
                            float2 float2 = MathUtils.Size(meshData.m_Bounds.xz);
                            int2 @int = math.max(1, (int2)(math.sqrt(float2) * 0.5f));
                            float2 float3 = float2 / @int;
                            float3 lhs = math.rotate(subMesh.m_Rotation, new float3(float3.x, 0f, 0f));
                            float3 lhs2 = math.rotate(subMesh.m_Rotation, new float3(0f, 0f, float3.y));
                            float3 float4 = subMesh.m_Position + math.rotate(subMesh.m_Rotation, new float3(@float.x, 0f, @float.y));
                            if (!@bool.y)
                            {
                                dynamicBuffer2.Add(new Effect
                                {
                                    m_Effect = this.m_BuildingConfigurationData.m_CollapseSFX,
                                    m_Position = float4,
                                    m_Rotation = subMesh.m_Rotation,
                                    m_Scale = 1f,
                                    m_Intensity = 1f,
                                    m_ParentMesh = num,
                                    m_AnimationIndex = -1,
                                    m_Procedural = true
                                });
                            }
                            if (!@bool.x)
                            {
                                float3 float5 = new float3(float3.x * 0.05f, 1f, float3.y * 0.05f);
                                float4 -= lhs * ((float)@int.x * 0.5f - 0.5f) + lhs2 * ((float)@int.y * 0.5f - 0.5f);
                                float5.y = (float5.x + float5.y) * 0.5f;
                                dynamicBuffer2.Capacity = dynamicBuffer2.Length + @int.x * @int.y;
                                for (int num2 = 0; num2 < @int.y; num2++)
                                {
                                    for (int num3 = 0; num3 < @int.x; num3++)
                                    {
                                        float2 float6 = new float2((float)num3, (float)num2) + random.NextFloat2(-0.25f, 0.25f);
                                        dynamicBuffer2.Add(new Effect
                                        {
                                            m_Effect = this.m_BuildingConfigurationData.m_CollapseVFX,
                                            m_Position = float4 + lhs * float6.x + lhs2 * float6.y,
                                            m_Rotation = subMesh.m_Rotation,
                                            m_Scale = float5,
                                            m_Intensity = 1f,
                                            m_ParentMesh = num,
                                            m_AnimationIndex = -1,
                                            m_Procedural = true
                                        });
                                    }
                                }
                            }
                        }
                    }
                    m++;
                    goto IL_84B;
                }
                for (int num4 = 0; num4 < bufferAccessor3.Length; num4++)
                {
                    DynamicBuffer<Effect> dynamicBuffer4 = bufferAccessor3[num4];
                    DynamicBuffer<SubMesh> dynamicBuffer5 = bufferAccessor[num4];
                    bool2 bool2 = new bool2(false, false);
                    for (int num5 = 0; num5 < dynamicBuffer4.Length; num5++)
                    {
                        EffectData effectData2;
                        if (this.m_EffectData.TryGetComponent(dynamicBuffer4[num5].m_Effect, out effectData2) && (effectData2.m_Flags.m_RequiredFlags & EffectConditionFlags.OnFire) != EffectConditionFlags.None)
                        {
                            bool2.x |= this.m_AudioEffectData.HasComponent(dynamicBuffer4[num5].m_Effect);
                            bool2.y |= this.m_AudioSpotData.HasComponent(dynamicBuffer4[num5].m_Effect);
                        }
                    }
                    for (int num6 = 0; num6 < dynamicBuffer5.Length; num6++)
                    {
                        SubMesh subMesh2 = dynamicBuffer5[num6];
                        MeshData meshData2;
                        if (this.m_MeshData.TryGetComponent(subMesh2.m_SubMesh, out meshData2))
                        {
                            float2 float7 = MathUtils.Center(meshData2.m_Bounds.xz);
                            float3 position = subMesh2.m_Position + math.rotate(subMesh2.m_Rotation, new float3(float7.x, 0f, float7.y));
                            if (!bool2.x)
                            {
                                dynamicBuffer4.Add(new Effect
                                {
                                    m_Effect = this.m_BuildingConfigurationData.m_FireLoopSFX,
                                    m_Position = position,
                                    m_Rotation = subMesh2.m_Rotation,
                                    m_Scale = 1f,
                                    m_Intensity = 1f,
                                    m_ParentMesh = num6,
                                    m_AnimationIndex = -1,
                                    m_Procedural = true
                                });
                            }
                            if (!bool2.y)
                            {
                                dynamicBuffer4.Add(new Effect
                                {
                                    m_Effect = this.m_BuildingConfigurationData.m_FireSpotSFX,
                                    m_Position = position,
                                    m_Rotation = subMesh2.m_Rotation,
                                    m_Scale = 1f,
                                    m_Intensity = 1f,
                                    m_ParentMesh = num6,
                                    m_AnimationIndex = -1,
                                    m_Procedural = true
                                });
                            }
                        }
                    }
                }
            }

            // Token: 0x06006723 RID: 26403 RVA: 0x0041A534 File Offset: 0x00418734
            private void CheckPropFlags(ref BuildingFlags flags, DynamicBuffer<SubObject> subObjects, int maxDepth = 10)
            {
                if (--maxDepth >= 0)
                {
                    for (int i = 0; i < subObjects.Length; i++)
                    {
                        SubObject subObject = subObjects[i];
                        SpawnLocationData spawnLocationData;
                        if (this.m_SpawnLocationData.TryGetComponent(subObject.m_Prefab, out spawnLocationData) && spawnLocationData.m_ActivityMask.m_Mask == 0U && spawnLocationData.m_ConnectionType == RouteConnectionType.Pedestrian)
                        {
                            flags |= BuildingFlags.HasInsideRoom;
                        }
                        DynamicBuffer<SubObject> subObjects2;
                        if (this.m_SubObjects.TryGetBuffer(subObject.m_Prefab, out subObjects2))
                        {
                            this.CheckPropFlags(ref flags, subObjects2, maxDepth);
                        }
                    }
                }
            }

            // Token: 0x0400B868 RID: 47208
            [ReadOnly]
            public ComponentTypeHandle<SpawnableBuildingData> m_SpawnableBuildingDataType;

            // Token: 0x0400B869 RID: 47209
            [ReadOnly]
            public ComponentTypeHandle<ServiceUpgradeData> m_ServiceUpgradeDataType;

            // Token: 0x0400B86A RID: 47210
            [ReadOnly]
            public ComponentTypeHandle<ExtractorFacilityData> m_ExtractorFacilityDataType;

            // Token: 0x0400B86B RID: 47211
            [ReadOnly]
            public ComponentTypeHandle<ConsumptionData> m_ConsumptionDataType;

            // Token: 0x0400B86C RID: 47212
            [ReadOnly]
            public ComponentTypeHandle<WorkplaceData> m_WorkplaceDataType;

            // Token: 0x0400B86D RID: 47213
            [ReadOnly]
            public ComponentTypeHandle<WaterPumpingStationData> m_WaterPumpingStationDataType;

            // Token: 0x0400B86E RID: 47214
            [ReadOnly]
            public ComponentTypeHandle<WaterTowerData> m_WaterTowerDataType;

            // Token: 0x0400B86F RID: 47215
            [ReadOnly]
            public ComponentTypeHandle<SewageOutletData> m_SewageOutletDataType;

            // Token: 0x0400B870 RID: 47216
            [ReadOnly]
            public ComponentTypeHandle<WastewaterTreatmentPlantData> m_WastewaterTreatmentPlantDataType;

            // Token: 0x0400B871 RID: 47217
            [ReadOnly]
            public ComponentTypeHandle<TransformerData> m_TransformerDataType;

            // Token: 0x0400B872 RID: 47218
            [ReadOnly]
            public ComponentTypeHandle<ParkingFacilityData> m_ParkingFacilityDataType;

            // Token: 0x0400B873 RID: 47219
            [ReadOnly]
            public ComponentTypeHandle<PublicTransportStationData> m_PublicTransportStationDataType;

            // Token: 0x0400B874 RID: 47220
            [ReadOnly]
            public ComponentTypeHandle<CargoTransportStationData> m_CargoTransportStationDataType;

            // Token: 0x0400B875 RID: 47221
            [ReadOnly]
            public BufferTypeHandle<SubNet> m_SubNetType;

            // Token: 0x0400B876 RID: 47222
            [ReadOnly]
            public BufferTypeHandle<SubObject> m_SubObjectType;

            // Token: 0x0400B877 RID: 47223
            [ReadOnly]
            public BufferTypeHandle<SubMesh> m_SubMeshType;

            // Token: 0x0400B878 RID: 47224
            public ComponentTypeHandle<BuildingData> m_BuildingDataType;

            // Token: 0x0400B879 RID: 47225
            public BufferTypeHandle<Effect> m_EffectType;

            // Token: 0x0400B87A RID: 47226
            [ReadOnly]
            public ComponentLookup<NetData> m_NetData;

            // Token: 0x0400B87B RID: 47227
            [ReadOnly]
            public ComponentLookup<SpawnLocationData> m_SpawnLocationData;

            // Token: 0x0400B87C RID: 47228
            [ReadOnly]
            public ComponentLookup<MeshData> m_MeshData;

            // Token: 0x0400B87D RID: 47229
            [ReadOnly]
            public ComponentLookup<EffectData> m_EffectData;

            // Token: 0x0400B87E RID: 47230
            [ReadOnly]
            public ComponentLookup<VFXData> m_VFXData;

            // Token: 0x0400B87F RID: 47231
            [ReadOnly]
            public BufferLookup<AudioSourceData> m_AudioSourceData;

            // Token: 0x0400B880 RID: 47232
            [ReadOnly]
            public ComponentLookup<AudioSpotData> m_AudioSpotData;

            // Token: 0x0400B881 RID: 47233
            [ReadOnly]
            public ComponentLookup<AudioEffectData> m_AudioEffectData;

            // Token: 0x0400B882 RID: 47234
            [ReadOnly]
            public BufferLookup<SubObject> m_SubObjects;

            // Token: 0x0400B883 RID: 47235
            [ReadOnly]
            public RandomSeed m_RandomSeed;

            // Token: 0x0400B884 RID: 47236
            [ReadOnly]
            public NativeArray<ArchetypeChunk> m_Chunks;

            // Token: 0x0400B885 RID: 47237
            [ReadOnly]
            public BuildingConfigurationData m_BuildingConfigurationData;
        }

        // Token: 0x020017EA RID: 6122
        private struct TypeHandle
        {
            // Token: 0x06006724 RID: 26404 RVA: 0x0041A5B8 File Offset: 0x004187B8
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                this.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>(true);
                this.__Game_Prefabs_BuildingData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingData>(false);
                this.__Game_Prefabs_BuildingExtensionData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingExtensionData>(false);
                this.__Game_Prefabs_BuildingTerraformData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingTerraformData>(false);
                this.__Game_Prefabs_ConsumptionData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ConsumptionData>(false);
                this.__Game_Prefabs_ObjectGeometryData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ObjectGeometryData>(false);
                this.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SpawnableBuildingData>(true);
                this.__Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SignatureBuildingData>(true);
                this.__Game_Prefabs_PlaceableObjectData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PlaceableObjectData>(false);
                this.__Game_Prefabs_ServiceUpgradeData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ServiceUpgradeData>(true);
                this.__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingPropertyData>(true);
                this.__Game_Prefabs_WaterPoweredData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterPoweredData>(true);
                this.__Game_Prefabs_SewageOutletData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SewageOutletData>(true);
                this.__Game_Prefabs_ServiceUpgradeBuilding_RO_BufferTypeHandle = state.GetBufferTypeHandle<ServiceUpgradeBuilding>(true);
                this.__Game_Simulation_CollectedServiceBuildingBudgetData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CollectedServiceBuildingBudgetData>(true);
                this.__Game_Prefabs_ServiceUpkeepData_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceUpkeepData>(false);
                this.__Game_Prefabs_ZoneData_RW_ComponentLookup = state.GetComponentLookup<ZoneData>(false);
                this.__Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup = state.GetComponentLookup<ZoneServiceConsumptionData>(true);
                this.__Game_Prefabs_ExtractorFacilityData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ExtractorFacilityData>(true);
                this.__Game_Prefabs_ConsumptionData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ConsumptionData>(true);
                this.__Game_Prefabs_WorkplaceData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WorkplaceData>(true);
                this.__Game_Prefabs_WaterPumpingStationData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterPumpingStationData>(true);
                this.__Game_Prefabs_WaterTowerData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterTowerData>(true);
                this.__Game_Prefabs_WastewaterTreatmentPlantData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WastewaterTreatmentPlantData>(true);
                this.__Game_Prefabs_TransformerData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TransformerData>(true);
                this.__Game_Prefabs_ParkingFacilityData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ParkingFacilityData>(true);
                this.__Game_Prefabs_PublicTransportStationData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PublicTransportStationData>(true);
                this.__Game_Prefabs_CargoTransportStationData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CargoTransportStationData>(true);
                this.__Game_Prefabs_SubNet_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubNet>(true);
                this.__Game_Prefabs_SubObject_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubObject>(true);
                this.__Game_Prefabs_SubMesh_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubMesh>(true);
                this.__Game_Prefabs_Effect_RW_BufferTypeHandle = state.GetBufferTypeHandle<Effect>(false);
                this.__Game_Prefabs_NetData_RO_ComponentLookup = state.GetComponentLookup<NetData>(true);
                this.__Game_Prefabs_SpawnLocationData_RO_ComponentLookup = state.GetComponentLookup<SpawnLocationData>(true);
                this.__Game_Prefabs_MeshData_RO_ComponentLookup = state.GetComponentLookup<MeshData>(true);
                this.__Game_Prefabs_EffectData_RO_ComponentLookup = state.GetComponentLookup<EffectData>(true);
                this.__Game_Prefabs_VFXData_RO_ComponentLookup = state.GetComponentLookup<VFXData>(true);
                this.__Game_Prefabs_AudioSourceData_RO_BufferLookup = state.GetBufferLookup<AudioSourceData>(true);
                this.__Game_Prefabs_AudioSpotData_RO_ComponentLookup = state.GetComponentLookup<AudioSpotData>(true);
                this.__Game_Prefabs_AudioEffectData_RO_ComponentLookup = state.GetComponentLookup<AudioEffectData>(true);
                this.__Game_Prefabs_SubObject_RO_BufferLookup = state.GetBufferLookup<SubObject>(true);
            }

            // Token: 0x0400B886 RID: 47238
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

            // Token: 0x0400B887 RID: 47239
            [ReadOnly]
            public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentTypeHandle;

            // Token: 0x0400B888 RID: 47240
            public ComponentTypeHandle<BuildingData> __Game_Prefabs_BuildingData_RW_ComponentTypeHandle;

            // Token: 0x0400B889 RID: 47241
            public ComponentTypeHandle<BuildingExtensionData> __Game_Prefabs_BuildingExtensionData_RW_ComponentTypeHandle;

            // Token: 0x0400B88A RID: 47242
            public ComponentTypeHandle<BuildingTerraformData> __Game_Prefabs_BuildingTerraformData_RW_ComponentTypeHandle;

            // Token: 0x0400B88B RID: 47243
            public ComponentTypeHandle<ConsumptionData> __Game_Prefabs_ConsumptionData_RW_ComponentTypeHandle;

            // Token: 0x0400B88C RID: 47244
            public ComponentTypeHandle<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RW_ComponentTypeHandle;

            // Token: 0x0400B88D RID: 47245
            [ReadOnly]
            public ComponentTypeHandle<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle;

            // Token: 0x0400B88E RID: 47246
            [ReadOnly]
            public ComponentTypeHandle<SignatureBuildingData> __Game_Prefabs_SignatureBuildingData_RO_ComponentTypeHandle;

            // Token: 0x0400B88F RID: 47247
            public ComponentTypeHandle<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RW_ComponentTypeHandle;

            // Token: 0x0400B890 RID: 47248
            [ReadOnly]
            public ComponentTypeHandle<ServiceUpgradeData> __Game_Prefabs_ServiceUpgradeData_RO_ComponentTypeHandle;

            // Token: 0x0400B891 RID: 47249
            [ReadOnly]
            public ComponentTypeHandle<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle;

            // Token: 0x0400B892 RID: 47250
            [ReadOnly]
            public ComponentTypeHandle<WaterPoweredData> __Game_Prefabs_WaterPoweredData_RO_ComponentTypeHandle;

            // Token: 0x0400B893 RID: 47251
            [ReadOnly]
            public ComponentTypeHandle<SewageOutletData> __Game_Prefabs_SewageOutletData_RO_ComponentTypeHandle;

            // Token: 0x0400B894 RID: 47252
            [ReadOnly]
            public BufferTypeHandle<ServiceUpgradeBuilding> __Game_Prefabs_ServiceUpgradeBuilding_RO_BufferTypeHandle;

            // Token: 0x0400B895 RID: 47253
            [ReadOnly]
            public ComponentTypeHandle<CollectedServiceBuildingBudgetData> __Game_Simulation_CollectedServiceBuildingBudgetData_RO_ComponentTypeHandle;

            // Token: 0x0400B896 RID: 47254
            public BufferTypeHandle<ServiceUpkeepData> __Game_Prefabs_ServiceUpkeepData_RW_BufferTypeHandle;

            // Token: 0x0400B897 RID: 47255
            public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RW_ComponentLookup;

            // Token: 0x0400B898 RID: 47256
            [ReadOnly]
            public ComponentLookup<ZoneServiceConsumptionData> __Game_Prefabs_ZoneServiceConsumptionData_RO_ComponentLookup;

            // Token: 0x0400B899 RID: 47257
            [ReadOnly]
            public ComponentTypeHandle<ExtractorFacilityData> __Game_Prefabs_ExtractorFacilityData_RO_ComponentTypeHandle;

            // Token: 0x0400B89A RID: 47258
            [ReadOnly]
            public ComponentTypeHandle<ConsumptionData> __Game_Prefabs_ConsumptionData_RO_ComponentTypeHandle;

            // Token: 0x0400B89B RID: 47259
            [ReadOnly]
            public ComponentTypeHandle<WorkplaceData> __Game_Prefabs_WorkplaceData_RO_ComponentTypeHandle;

            // Token: 0x0400B89C RID: 47260
            [ReadOnly]
            public ComponentTypeHandle<WaterPumpingStationData> __Game_Prefabs_WaterPumpingStationData_RO_ComponentTypeHandle;

            // Token: 0x0400B89D RID: 47261
            [ReadOnly]
            public ComponentTypeHandle<WaterTowerData> __Game_Prefabs_WaterTowerData_RO_ComponentTypeHandle;

            // Token: 0x0400B89E RID: 47262
            [ReadOnly]
            public ComponentTypeHandle<WastewaterTreatmentPlantData> __Game_Prefabs_WastewaterTreatmentPlantData_RO_ComponentTypeHandle;

            // Token: 0x0400B89F RID: 47263
            [ReadOnly]
            public ComponentTypeHandle<TransformerData> __Game_Prefabs_TransformerData_RO_ComponentTypeHandle;

            // Token: 0x0400B8A0 RID: 47264
            [ReadOnly]
            public ComponentTypeHandle<ParkingFacilityData> __Game_Prefabs_ParkingFacilityData_RO_ComponentTypeHandle;

            // Token: 0x0400B8A1 RID: 47265
            [ReadOnly]
            public ComponentTypeHandle<PublicTransportStationData> __Game_Prefabs_PublicTransportStationData_RO_ComponentTypeHandle;

            // Token: 0x0400B8A2 RID: 47266
            [ReadOnly]
            public ComponentTypeHandle<CargoTransportStationData> __Game_Prefabs_CargoTransportStationData_RO_ComponentTypeHandle;

            // Token: 0x0400B8A3 RID: 47267
            [ReadOnly]
            public BufferTypeHandle<SubNet> __Game_Prefabs_SubNet_RO_BufferTypeHandle;

            // Token: 0x0400B8A4 RID: 47268
            [ReadOnly]
            public BufferTypeHandle<SubObject> __Game_Prefabs_SubObject_RO_BufferTypeHandle;

            // Token: 0x0400B8A5 RID: 47269
            [ReadOnly]
            public BufferTypeHandle<SubMesh> __Game_Prefabs_SubMesh_RO_BufferTypeHandle;

            // Token: 0x0400B8A6 RID: 47270
            public BufferTypeHandle<Effect> __Game_Prefabs_Effect_RW_BufferTypeHandle;

            // Token: 0x0400B8A7 RID: 47271
            [ReadOnly]
            public ComponentLookup<NetData> __Game_Prefabs_NetData_RO_ComponentLookup;

            // Token: 0x0400B8A8 RID: 47272
            [ReadOnly]
            public ComponentLookup<SpawnLocationData> __Game_Prefabs_SpawnLocationData_RO_ComponentLookup;

            // Token: 0x0400B8A9 RID: 47273
            [ReadOnly]
            public ComponentLookup<MeshData> __Game_Prefabs_MeshData_RO_ComponentLookup;

            // Token: 0x0400B8AA RID: 47274
            [ReadOnly]
            public ComponentLookup<EffectData> __Game_Prefabs_EffectData_RO_ComponentLookup;

            // Token: 0x0400B8AB RID: 47275
            [ReadOnly]
            public ComponentLookup<VFXData> __Game_Prefabs_VFXData_RO_ComponentLookup;

            // Token: 0x0400B8AC RID: 47276
            [ReadOnly]
            public BufferLookup<AudioSourceData> __Game_Prefabs_AudioSourceData_RO_BufferLookup;

            // Token: 0x0400B8AD RID: 47277
            [ReadOnly]
            public ComponentLookup<AudioSpotData> __Game_Prefabs_AudioSpotData_RO_ComponentLookup;

            // Token: 0x0400B8AE RID: 47278
            [ReadOnly]
            public ComponentLookup<AudioEffectData> __Game_Prefabs_AudioEffectData_RO_ComponentLookup;

            // Token: 0x0400B8AF RID: 47279
            [ReadOnly]
            public BufferLookup<SubObject> __Game_Prefabs_SubObject_RO_BufferLookup;
        }
    }
}