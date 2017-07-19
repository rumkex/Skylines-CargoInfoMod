using ColossalFramework.Plugins;
using ICities;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using CargoInfoMod.Data;

namespace CargoInfoMod
{

    public class CargoData : SerializableDataExtensionBase
    {
        private ModInfo mod;

        private Dictionary<ushort, CargoStats> cargoStatIndex;
        private HashSet<int> cargoStations;

        public CargoData()
        {
            cargoStations = new HashSet<int>();
            cargoStatIndex = new Dictionary<ushort, CargoStats>();
        }

        public override void OnCreated(ISerializableData serializedData)
        {
            base.OnCreated(serializedData);
            mod = PluginManager.instance.FindPluginInfo(Assembly.GetExecutingAssembly()).userModInstance as ModInfo;
            if (LoadingManager.instance.m_loadingComplete)
            {
                OnLoadData();
                Setup();
            }
            if (mod != null)
                mod.data = this;
            else
                Debug.LogError("Could not find parent IUserMod!");
        }

        public override void OnReleased()
        {
            OnSaveData();
            base.OnReleased();
        }

        public override void OnLoadData()
        {
            Debug.Log("Restoring previous data...");
            try
            {
                var data = serializableDataManager.LoadData(ModInfo.Namespace);
                if (data == null)
                {
                    Debug.Log("No previous data found");
                    return;
                }
                var ms = new MemoryStream(data);
                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.AssemblyFormat = FormatterAssemblyStyle.Simple;
                cargoStatIndex = binaryFormatter.Deserialize(ms) as Dictionary<ushort, CargoStats> ?? cargoStatIndex;
                Debug.Log(string.Format("Loaded stats for {0} stations", cargoStatIndex.Count));
            }
            catch (SerializationException e)
            {
                Debug.LogError("While deserializing data: " + e.Message);
            }
        }

        public void Setup()
        {
            Debug.Log("Looking up cargo station prefabs...");
            cargoStations.Clear();
            for (uint i = 0; i < PrefabCollection<BuildingInfo>.LoadedCount(); i++)
            {
                var prefab = PrefabCollection<BuildingInfo>.GetLoaded(i);
                if (prefab.m_buildingAI is CargoStationAI)
                {
                    Debug.Log(string.Format("Cargo station prefab found: {0}", prefab.name));
                    cargoStations.Add(prefab.m_prefabDataIndex);
                }
            }
            Debug.Log(string.Format("Found {0} cargo station prefabs", cargoStations.Count));

            for (ushort i = 0; i < BuildingManager.instance.m_buildings.m_size; i++)
            {
                AddBuilding(i);
            }

            BuildingManager.instance.EventBuildingCreated += AddBuilding;
            BuildingManager.instance.EventBuildingReleased += RemoveBuilding;
        }

        public override void OnSaveData()
        {
            Debug.Log("Saving data...");
            try
            {
                var ms = new MemoryStream();
                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.AssemblyFormat = FormatterAssemblyStyle.Simple;
                binaryFormatter.Serialize(ms, cargoStatIndex);
                serializableDataManager.SaveData(ModInfo.Namespace, ms.ToArray());
                Debug.Log(string.Format("Saved stats for {0} stations", cargoStatIndex.Count));
            }
            catch (SerializationException e)
            {
                Debug.LogError("While serializing data: " + e.Message);
            }
        }

        public void AddBuilding(ushort buildingID)
        {
            var building = BuildingManager.instance.m_buildings.m_buffer[buildingID];
            if (cargoStations.Contains(building.m_infoIndex) && !cargoStatIndex.ContainsKey(buildingID))
            {
                var buildingName = BuildingManager.instance.GetBuildingName(buildingID, InstanceID.Empty);
                // Restoring previous values of truck statistics
                cargoStatIndex.Add(buildingID, new CargoStats());
                Debug.Log(string.Format("Cargo station added to index: {0}", buildingName));
            }
        }

        public void RemoveBuilding(ushort buildingID)
        {
            if (cargoStatIndex.ContainsKey(buildingID))
            {
                var buildingName = BuildingManager.instance.GetBuildingName(buildingID, InstanceID.Empty);
                cargoStatIndex.Remove(buildingID);
                Debug.Log(string.Format("Cargo station removed from index: {0}", buildingName));
            }
        }

        public void UpdateCounters()
        {
            foreach (var pair in cargoStatIndex)
            {
                pair.Value.carsReceivedLastTime = BuildingManager.instance.m_buildings.m_buffer[pair.Key].m_customBuffer1;
                pair.Value.carsSentLastTime = BuildingManager.instance.m_buildings.m_buffer[pair.Key].m_customBuffer2;
                BuildingManager.instance.m_buildings.m_buffer[pair.Key].m_customBuffer1 = 0;
                BuildingManager.instance.m_buildings.m_buffer[pair.Key].m_customBuffer2 = 0;
            }
        }

        public bool TryGetEntry(ushort building, out CargoStats stats)
        {
            return cargoStatIndex.TryGetValue(building, out stats);
        }
    }
}
