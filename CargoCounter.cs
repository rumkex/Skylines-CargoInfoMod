using ICities;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework.UI;
using System.Text;
using System;

namespace CargoInfoMod
{
    public class CargoStats
    {
        public int carsReceived = 0;
        public int carsSent = 0;
        public int carsReceivedLastTime = 0;
        public int carsSentLastTime = 0;
    }

    public class CargoCounter: ThreadingExtensionBase
    {
        public static CargoCounter instance;

        private HashSet<int> cargoStations;
        public Dictionary<ushort, CargoStats> cargoStatIndex;

        private UIPanel servicePanel;
        private CityServiceWorldInfoPanel serviceInfoPanel;
        private UILabel statsLabel;

        public CargoCounter()
        {
            cargoStations = new HashSet<int>();
            cargoStatIndex = new Dictionary<ushort, CargoStats>();
            instance = this;
        }

        public override void OnCreated(IThreading threading)
        {
            base.OnCreated(threading);
            if (LoadingManager.instance.m_loadingComplete)
            {
                // Mod reloaded while running the game, should set up again
                Debug.Log("ThreadingExtension created while running");
                OnLevelLoaded(SimulationManager.UpdateMode.LoadGame);
            }
            LoadingManager.instance.m_levelLoaded += OnLevelLoaded;
            BuildingManager.instance.EventBuildingCreated += AddBuilding;
            BuildingManager.instance.EventBuildingReleased += RemoveBuilding;
        }

        private void AddBuilding(ushort buildingID)
        {
            var building = BuildingManager.instance.m_buildings.m_buffer[buildingID];
            if (cargoStations.Contains(building.m_infoIndex) && !cargoStatIndex.ContainsKey(buildingID))
            {
                var buildingName = BuildingManager.instance.GetBuildingName(buildingID, InstanceID.Empty);
                cargoStatIndex.Add(buildingID, new CargoStats());
                Debug.Log(string.Format("Cargo station added to index: {0}", buildingName));
            }
        }

        private void RemoveBuilding(ushort buildingID)
        {
            if (cargoStatIndex.ContainsKey(buildingID))
            {
                var buildingName = BuildingManager.instance.GetBuildingName(buildingID, InstanceID.Empty);
                cargoStatIndex.Remove(buildingID);
                Debug.Log(string.Format("Cargo station removed from index: {0}", buildingName));
            }
        }

        public override void OnReleased()
        {
            Debug.Log("ThreadingExtension released");
            base.OnReleased();
        }

        private void OnLevelLoaded(SimulationManager.UpdateMode updateMode)
        {
            InitCargoDB();
            SetupUIBindings();
            HarmonyDetours.Apply();
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

        private void InitCargoDB()
        {
            for (uint i = 0; i < PrefabCollection<BuildingInfo>.LoadedCount(); i++)
            {
                var prefab = PrefabCollection<BuildingInfo>.GetLoaded(i);
                if (prefab.m_buildingAI is CargoStationAI)
                {
                    Debug.Log(string.Format("Cargo station prefab found: {0}", prefab.name));
                    cargoStations.Add(prefab.m_prefabDataIndex);
                }
            }

            for (ushort i = 0; i < BuildingManager.instance.m_buildings.m_size; i++)
            {
                AddBuilding(i);
            }
        }

        private DateTime lastReset = DateTime.MinValue;
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            if (lastReset < SimulationManager.instance.m_currentGameTime.Date && SimulationManager.instance.m_currentGameTime.Day == 1)
            {
                Debug.Log("Resetting all counter on the first day of the month");
                foreach (var pair in cargoStatIndex)
                {
                    pair.Value.carsReceivedLastTime = pair.Value.carsReceived;
                    pair.Value.carsSentLastTime = pair.Value.carsSent;
                    pair.Value.carsReceived = 0;
                    pair.Value.carsSent = 0;
                }
                lastReset = SimulationManager.instance.m_currentGameTime.Date;
            }

            if (statsLabel != null && serviceInfoPanel.isActiveAndEnabled)
            {
                InstanceID instanceID = WorldInfoPanel.GetCurrentInstanceID();
                if (instanceID.Building != 0 && cargoStatIndex.ContainsKey(instanceID.Building))
                {
                    var sb = new StringBuilder();
                    sb.AppendFormat("Trucks received last month: {0}", Mathf.Max(
                        cargoStatIndex[instanceID.Building].carsReceived,
                        cargoStatIndex[instanceID.Building].carsReceivedLastTime
                        )
                    );
                    sb.AppendLine();
                    sb.AppendFormat("Trucks sent last month: {0}", Mathf.Max(
                        cargoStatIndex[instanceID.Building].carsSent,
                        cargoStatIndex[instanceID.Building].carsSentLastTime
                        )
                    );
                    statsLabel.text = sb.ToString();
                }
            }
            base.OnUpdate(realTimeDelta, simulationTimeDelta);
        }

    }
}
