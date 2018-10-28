using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using CargoInfoMod.Data;

namespace CargoInfoMod
{
    public class CargoCounter: ThreadingExtensionBase
    {
        private ModInfo mod;

        private UICargoChart vehicleCargoChart;
        private CargoUIPanel cargoPanel;
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
            OnLevelUnloaded();

            if (updateMode != SimulationManager.UpdateMode.NewGameFromMap &&
                updateMode != SimulationManager.UpdateMode.NewGameFromScenario &&
                updateMode != SimulationManager.UpdateMode.LoadGame)
                return;

            mod.data.Setup();

            SetupUIBindings();
        }

        private void OnLevelUnloaded()
        {
            Debug.Log("Cleaning up UI...");
            if (statsLabel != null)
                statsLabel.eventClicked -= showDelegate;
            if (cargoPanel != null)
                GameObject.Destroy(cargoPanel);
            if (vehicleCargoChart != null)
                GameObject.Destroy(vehicleCargoChart);
        }

        private void SetupUIBindings()
        {
            Debug.Log("Setting up UI...");

            cargoPanel = (CargoUIPanel)UIView.GetAView().AddUIComponent(typeof(CargoUIPanel));

            var servicePanel = UIHelper.GetPanel("(Library) CityServiceWorldInfoPanel");
            var statsPanel = servicePanel?.Find<UIPanel>("DescPanel");
            statsLabel = statsPanel?.Find<UILabel>("Desc");
            if (servicePanel == null)
                Debug.LogError("CityServiceWorldInfoPanel not found");
            if (statsPanel == null)
                Debug.LogError("DescPanel not found");
            if (statsLabel == null)
                Debug.LogError("DescPanel.Desc label not found");
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

            var vehicleMainPanel = UIHelper.GetPanel("(Library) CityServiceVehicleWorldInfoPanel");
            var vehiclePanel = vehicleMainPanel.Find<UIPanel>("Panel");
            if (vehiclePanel != null)
            {
                vehiclePanel.autoLayout = false;
                vehicleCargoChart = vehiclePanel.AddUIComponent<UICargoChart>();
                vehicleCargoChart.size = new Vector2(60, 60);
                vehicleCargoChart.relativePosition = new Vector3(330, 0);
                vehicleCargoChart.SetValues(CargoParcel.ResourceTypes.Select(f => 1f / CargoParcel.ResourceTypes.Length).ToArray());
            }
            else
            {
                Debug.LogError("CityServiceVehicleWorldInfoPanel not found!");
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

            if (!WorldInfoPanel.AnyWorldInfoPanelOpen())
                return;

            UpdateBuildingInfoPanel();
            UpdateVehicleInfoPanel();

            base.OnUpdate(realTimeDelta, simulationTimeDelta);
        }

        private void UpdateVehicleInfoPanel()
        {
            var vehicleID = WorldInfoPanel.GetCurrentInstanceID().Vehicle;
            if (vehicleID != 0 && vehicleCargoChart != null)
            {
                int guard = 0;

                // Find leading vehicle that actually has all the cargo
                while (VehicleManager.instance.m_vehicles.m_buffer[vehicleID].m_leadingVehicle != 0)
                {
                    guard++;
                    vehicleID = VehicleManager.instance.m_vehicles.m_buffer[vehicleID].m_leadingVehicle;
                    if (guard > ushort.MaxValue)
                    {
                        Debug.LogError("Invalid list detected!");
                        return;
                    }
                }

                var ai = VehicleManager.instance.m_vehicles.m_buffer[vehicleID].Info.m_vehicleAI;
                if (ai is CargoTrainAI || ai is CargoShipAI)
                {
                    var cargo = VehicleManager.instance.m_vehicles.m_buffer[vehicleID].m_firstCargo;
                    var result = new float[CargoParcel.ResourceTypes.Length];
                    guard = 0;
                    while (cargo != 0)
                    {
                        var parcel = new CargoParcel(0, false,
                            VehicleManager.instance.m_vehicles.m_buffer[cargo].m_transferType,
                            VehicleManager.instance.m_vehicles.m_buffer[cargo].m_transferSize,
                            VehicleManager.instance.m_vehicles.m_buffer[cargo].m_flags);
                        result[parcel.ResourceType] += parcel.transferSize;
                        cargo = VehicleManager.instance.m_vehicles.m_buffer[cargo].m_nextCargo;
                        guard++;
                        if (guard > ushort.MaxValue)
                        {
                            Debug.LogError("Invalid list detected!");
                            return;
                        }
                    }
                    var total = result.Sum();
                    Debug.Log(string.Join(", ", result.Select(v => v / total).Select(f => f.ToString()).ToArray()));
                    vehicleCargoChart.isVisible = true;
                    vehicleCargoChart.tooltip = string.Format("{0:0}{1}", total / 1000, Localization.Get("KILO_UNITS"));
                    if (Math.Abs(total) < 1f) total = 1f;
                    vehicleCargoChart.SetValues(result.Select(v => v / total).ToArray());
                }
                else
                {
                    vehicleCargoChart.isVisible = false;
                }
            }
        }

        public void UpdateBuildingInfoPanel()
        {
            InstanceID instanceID = WorldInfoPanel.GetCurrentInstanceID();

            if (statsLabel != null)
            {
                CargoStats2 stats;
                if (instanceID.Building != 0 && mod.data.TryGetEntry(instanceID.Building, out stats))
                {
                    var sb = new StringBuilder();
                    var timeScale = mod.Options.UseMonthlyValues? 1.0f: 0.25f;

                    var receivedAmount = Mathf.Ceil(Mathf.Max(stats.CarsReceived, stats.CarsReceivedLastTime) /
                                                    CargoData.TruckCapacity * timeScale);

                    var sentAmount = Mathf.Ceil(Mathf.Max(stats.CarsSent, stats.CarsSentLastTime) /
                                                CargoData.TruckCapacity * timeScale);

                    sb.AppendFormat(
                        "{0}: {1:0}",
                        Localization.Get(mod.Options.UseMonthlyValues ? "TRUCKS_RCVD_LAST_MONTH": "TRUCKS_RCVD_LAST_WEEK"),
                        receivedAmount);
                    sb.AppendLine();
                    sb.AppendFormat(
                        "{0}: {1:0}",
                        Localization.Get(mod.Options.UseMonthlyValues ? "TRUCKS_SENT_LAST_MONTH" : "TRUCKS_SENT_LAST_WEEK"),
                        sentAmount);
                    sb.AppendLine();
                    sb.Append(Localization.Get("CLICK_MORE"));
                    statsLabel.text = sb.ToString();
                }
            }
        }
    }
}
