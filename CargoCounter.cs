using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using System;
using System.Reflection;
using System.Text;
using UnityEngine;
using CargoInfoMod.Data;

namespace CargoInfoMod
{
    public class CargoCounter: ThreadingExtensionBase
    {
        private ModInfo mod;

        private UIPanel servicePanel;
        private CityServiceWorldInfoPanel serviceInfoPanel;
        private UILabel statsLabel;

        public override void OnCreated(IThreading threading)
        {
            base.OnCreated(threading);
            mod = PluginManager.instance.FindPluginInfo(Assembly.GetExecutingAssembly()).userModInstance as ModInfo;

            if (LoadingManager.instance.m_loadingComplete)
            {
                // Mod reloaded while running the game, should set up again
                Debug.Log("ThreadingExtension created while running");
                OnLevelLoaded(SimulationManager.UpdateMode.LoadGame);
            }
            LoadingManager.instance.m_levelLoaded += OnLevelLoaded;

            HarmonyDetours.Apply();
        }

        public override void OnReleased()
        {
            base.OnReleased();
        }

        private void OnLevelLoaded(SimulationManager.UpdateMode updateMode)
        {
            mod.data.Setup();

            SetupUIBindings();
        }

        private void SetupUIBindings()
        {
            Debug.Log("Setting up UI...");
            servicePanel = UIHelper.GetPanel("(Library) CityServiceWorldInfoPanel");
            serviceInfoPanel = servicePanel?.GetComponent<CityServiceWorldInfoPanel>();
            var statsPanel = servicePanel?.Find<UIPanel>("StatsPanel");
            statsLabel = statsPanel?.Find<UILabel>("Info");
            if (servicePanel == null)
                Debug.LogError("Service info panel not found");
            if (statsPanel == null)
                Debug.LogError("Service stats panel not found");
            if (statsLabel == null)
                Debug.LogError("Service stats label not found");
            else
                Debug.Log("Service stats label found!");
        }

        private DateTime lastReset = DateTime.MinValue;
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            if (lastReset < SimulationManager.instance.m_currentGameTime.Date && SimulationManager.instance.m_currentGameTime.Day == 1)
            {
                Debug.Log("Resetting all counter on the first day of the month");
                mod.data.UpdateCounters();
                lastReset = SimulationManager.instance.m_currentGameTime.Date;
            }

            if (statsLabel != null && serviceInfoPanel.isActiveAndEnabled)
            {
                InstanceID instanceID = WorldInfoPanel.GetCurrentInstanceID();
                CargoStats stats;
                if (instanceID.Building != 0 && mod.data.TryGetEntry(instanceID.Building, out stats))
                {
                    var building = BuildingManager.instance.m_buildings.m_buffer[instanceID.Building];
                    var sb = new StringBuilder();
                    sb.AppendFormat("Trucks received last month: {0}", Mathf.Max(building.m_customBuffer1, stats.carsReceivedLastTime));
                    sb.AppendLine();
                    sb.AppendFormat("Trucks sent last month: {0}", Mathf.Max(building.m_customBuffer2, stats.carsSentLastTime));
                    statsLabel.text = sb.ToString();
                }
            }
            base.OnUpdate(realTimeDelta, simulationTimeDelta);
        }
    }
}
