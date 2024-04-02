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
using static UnityEngine.Rendering.DebugUI;


namespace LandValueOverhaul.Systems
{

    public partial class GarbageFeeFixSystem : GameSystemBase
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
                ComponentType.ReadWrite<ServiceFeeParameterData>(),
                }
            });
            RequireForUpdate(m_ServiceFeeQuery);
        }

        [Preserve]
        protected override void OnUpdate()
        {
            DynamicBuffer<ServiceFee> fees;
            if (base.World.EntityManager.TryGetBuffer(base.World.GetOrCreateSystemManaged<CitySystem>().City, false, out fees))
            {
                float fee;
                if(ServiceFeeSystem.TryGetFee(PlayerResource.Garbage, fees, out fee) && fee > 0)
                {
                    ServiceFeeSystem.SetFee(PlayerResource.Garbage, fees, 0f);
                    Mod.log.Info("Garbage fee modified!");
                }
            }
            foreach (Entity prefabEntity in m_ServiceFeeQuery.ToEntityArray(Allocator.Temp))
            {
                PrefabData prefabData = base.EntityManager.GetComponentData<PrefabData>(prefabEntity);
                ServiceFeeParameterData data = base.EntityManager.GetComponentData<ServiceFeeParameterData>(prefabEntity);

                if (!m_PrefabSystem.TryGetPrefab(prefabData, out PrefabBase prefab))
                {
                    continue;
                }
                switch (prefab.name)
                {
                    case "ServiceFeeParameters": // ServiceFeeParameters
                        data.m_GarbageFee = new FeeParameters { m_Adjustable = false, m_Default = 0f, m_Max = 0f };
                        base.EntityManager.SetComponentData(prefabEntity, data);
                        break;
                    default:
                        break;
                }

            }
        }

        [Preserve]
        public GarbageFeeFixSystem()
        {
        }

    } // class

} // namespace