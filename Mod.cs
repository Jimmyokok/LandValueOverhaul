using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Unity.Entities;
using HarmonyLib;
using System.Linq;
using LandValueOverhaul.Patches;
using LandValueOverhaul.Systems;
using System.IO;
using System.Reflection;
using Game.Simulation;
using Game.Prefabs;

namespace LandValueOverhaul
{
    public class Mod : IMod
    {
        public static readonly string harmonyID = "Jimmyok." + nameof(LandValueOverhaul);
        public static Mod instance { get; private set; }
        public static ILog log = LogManager.GetLogger($"{nameof(LandValueOverhaul)}").SetShowsErrorsInUI(false);
        public static Setting setting { get; private set; }

        private World _world;

        private void SafelyRemove<T>()
            where T : GameSystemBase
        {
            var system = _world.GetExistingSystemManaged<T>();

            if (system != null)
                _world?.DestroySystemManaged(system);
        }

        public void OnLoad(UpdateSystem updateSystem)
        {
            instance = this;
            log.Info(nameof(OnLoad));

            setting = new Setting(this);
            setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(setting));
            GameManager.instance.localizationManager.AddSource("zh-HANS", new LocaleCN(setting));
            setting._Hidden = false;
            AssetDatabase.global.LoadSettings(nameof(LandValueOverhaul), setting, new Setting(this));

            new Patcher(harmonyID, log);
            if (Patcher.Instance is null || !Patcher.Instance.PatchesApplied)
            {
                log.Critical("Harmony patches not applied; aborting system activation");
                return;
            }

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.LandValueSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.BuildingUpkeepSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.PropertyRenterSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Simulation.RentAdjustSystem>().Enabled = false;

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Debug.LandValueDebugSystem>().Enabled = false;
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.UI.Tooltip.LandValueTooltipSystem>().Enabled = false;
            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Rendering.OverlayInfomodeSystem>().Enabled = false;

            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<LandValueOverhaul.Systems.LandValueSystem>();
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<LandValueOverhaul.Systems.PropertyRenterSystem>();
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<LandValueOverhaul.Systems.RentAdjustSystem>();
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<LandValueOverhaul.Systems.BuildingUpkeepSystem>();

            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<LandValueOverhaul.Systems.LandValueDebugSystem>();
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<LandValueOverhaul.Systems.LandValueTooltipSystem>();
            //World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<LandValueOverhaul.Systems.OverlayInfomodeSystem>();

            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<LandValueOverhaul.Systems.GarbageFeeFixSystem>();
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<LandValueOverhaul.Systems.BuildingReinitializeSystem>();

            updateSystem?.UpdateAt<LandValueOverhaul.Systems.LandValueSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem?.UpdateAt<LandValueOverhaul.Systems.PropertyRenterSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem?.UpdateAt<LandValueOverhaul.Systems.RentAdjustSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem?.UpdateAt<LandValueOverhaul.Systems.BuildingUpkeepSystem>(SystemUpdatePhase.GameSimulation);

            //updateSystem?.UpdateAt<LandValueOverhaul.Systems.LandValueDebugSystem>(SystemUpdatePhase.DebugGizmos);
            updateSystem?.UpdateAt<LandValueOverhaul.Systems.LandValueTooltipSystem>(SystemUpdatePhase.UITooltip);
            //updateSystem?.UpdateAt<LandValueOverhaul.Systems.OverlayInfomodeSystem>(SystemUpdatePhase.PreCulling);

            updateSystem?.UpdateAfter<GarbageFeeFixSystem>(SystemUpdatePhase.PrefabUpdate);
            updateSystem?.UpdateAt<GarbageFeeFixSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem?.UpdateAfter<GarbageFeeFixSystem, CitySystem>(SystemUpdatePhase.GameSimulation);
            updateSystem?.UpdateAt<BuildingReinitializeSystem>(SystemUpdatePhase.PrefabUpdate);
            updateSystem?.UpdateBefore<BuildingReinitializeSystem>(SystemUpdatePhase.PrefabReferences);
            updateSystem?.UpdateAfter<BuildingReinitializeSystem, BuildingInitializeSystem>(SystemUpdatePhase.PrefabUpdate);
            _world = updateSystem.World;
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
            if (setting != null)
            {
                setting.UnregisterInOptionsUI();
                setting = null;
            }
            instance = null;
            Patcher.Instance?.UnPatchAll();
            SafelyRemove<LandValueOverhaul.Systems.BuildingReinitializeSystem>();
            SafelyRemove<LandValueOverhaul.Systems.BuildingUpkeepSystem>();
            SafelyRemove<LandValueOverhaul.Systems.LandValueSystem>();

            SafelyRemove<LandValueOverhaul.Systems.LandValueTooltipSystem>();
            //SafelyRemove<LandValueOverhaul.Systems.LandValueDebugSystem>();
            //SafelyRemove<LandValueOverhaul.Systems.OverlayInfomodeSystem>();

            SafelyRemove<LandValueOverhaul.Systems.PropertyRenterSystem>();
            SafelyRemove<LandValueOverhaul.Systems.RentAdjustSystem>();
            SafelyRemove<LandValueOverhaul.Systems.GarbageFeeFixSystem>();
        }
    }
}
