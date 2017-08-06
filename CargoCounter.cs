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

        private CargoUIPanel cargoPanel;
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
            LoadingManager.instance.m_levelPreUnloaded += OnLevelUnloaded;

            HarmonyDetours.Apply();
        }

        private MouseEventHandler showDelegate;

        public override void OnReleased()
        {
            OnLevelUnloaded();
            // TODO: Unapply Harmony patches once the feature is available
            base.OnReleased();
        }

        private void OnLevelLoaded(SimulationManager.UpdateMode updateMode)
        {
            mod.data.Setup();

            SetupUIBindings();
        }

        private void OnLevelUnloaded()
        {
            Debug.Log("Cleaning up UI...");
            statsLabel.eventClicked -= showDelegate;
            GameObject.Destroy(cargoPanel);
        }

        private void SetupUIBindings()
        {
            if (cargoPanel != null)
                OnLevelUnloaded();

            Debug.Log("Setting up UI...");

            cargoPanel = (CargoUIPanel)UIView.GetAView().AddUIComponent(typeof(CargoUIPanel));

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
            {
                Debug.Log("Service stats label found!");

                showDelegate = (sender, e) =>
                {
                    if (mod.data.TryGetEntry(WorldInfoPanel.GetCurrentInstanceID().Building, out _))
                    {
                        cargoPanel.Show();
                    }
                };

                statsLabel.eventClicked += showDelegate;
            }
        }

        private DateTime lastReset = DateTime.MinValue;

        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            if (lastReset < SimulationManager.instance.m_currentGameTime.Date && SimulationManager.instance.m_currentGameTime.Day == 1)
            {
                lastReset = SimulationManager.instance.m_currentGameTime.Date;
                mod.data.UpdateCounters();
                Debug.Log("Monthly counter values updated");
            }

            if (statsLabel != null && serviceInfoPanel.isActiveAndEnabled)
            {
                InstanceID instanceID = WorldInfoPanel.GetCurrentInstanceID();
                CargoStats2 stats;
                if (instanceID.Building != 0 && mod.data.TryGetEntry(instanceID.Building, out stats))
                {
                    var sb = new StringBuilder();
                    sb.AppendFormat("Trucks received last month: {0:0}", Mathf.Max(stats.CarsReceived, stats.CarsReceivedLastTime) / CargoData.TruckCapacity);
                    sb.AppendLine();
                    sb.AppendFormat("Trucks sent last month: {0:0}", Mathf.Max(stats.CarsSent, stats.CarsSentLastTime) / CargoData.TruckCapacity);
                    sb.AppendLine();
                    sb.Append("Click for more!");
                    statsLabel.text = sb.ToString();
                }
            }
            base.OnUpdate(realTimeDelta, simulationTimeDelta);
        }
    }
}
