using System;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;
using Game;
using Game.Prefabs;
using Game.Economy;
using static Game.Simulation.ServiceCoverageSystem;
using Game.Simulation;
using Colossal.Entities;
using Game.City;
using Mono.Cecil;


namespace LandValueOverhaul.Systems
{

    public partial class LowRentUpkeepFixSystem : GameSystemBase
    {
        private PrefabSystem m_PrefabSystem;
        private CitySystem m_CitySystem;
        private EntityQuery m_ServiceFeeQuery;

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 262144 / 16;
        }
        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
            m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
            m_ServiceFeeQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[2]
                {
                ComponentType.ReadOnly<PrefabData>(),
                ComponentType.ReadWrite<ZoneServiceConsumptionData>(),
                }
            });
            RequireForUpdate(m_ServiceFeeQuery);
        }

        [Preserve]
        protected override void OnUpdate()
        {
            foreach (Entity prefabEntity in m_ServiceFeeQuery.ToEntityArray(Allocator.Temp))
            {
                PrefabData prefabData = base.EntityManager.GetComponentData<PrefabData>(prefabEntity);
                ZoneServiceConsumptionData data = base.EntityManager.GetComponentData<ZoneServiceConsumptionData>(prefabEntity);

                if (!m_PrefabSystem.TryGetPrefab(prefabData, out PrefabBase prefab))
                {
                    continue;
                }
                switch (prefab.name)
                {
                    case "Residential LowRent": // ServiceFeeParameters
                        Mod.log.Info($"Base upkeep for {prefab.name} modified!");
                        data.m_Upkeep = 250f;
                        base.EntityManager.SetComponentData(prefabEntity, data);
                        this.Enabled = false;
                        break;
                    default:
                        break;
                }

            }
        }

        [Preserve]
        public LowRentUpkeepFixSystem()
        {
        }

    } // class

} // namespace