using System;
using CargoInfoMod.Data;
using ColossalFramework.Plugins;
using ICities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

using TransferType = TransferManager.TransferReason;

namespace CargoInfoMod
{
    public struct CargoParcel
    {
        public ushort building;
        public ushort transferSize;
        public CarFlags flags;
        public int ResourceType => ((flags & CarFlags.Resource) - CarFlags.Oil) / 0x10;

        public static readonly CarFlags[] ResourceTypes =
        {
            CarFlags.Oil,
            CarFlags.Petrol,
            CarFlags.Ore,
            CarFlags.Coal,
            CarFlags.Logs,
            CarFlags.Lumber,
            CarFlags.Grain,
            CarFlags.Food,
            CarFlags.Goods
        };

        public CargoParcel(ushort buildingID, bool incoming, byte transferType, ushort transferSize, Vehicle.Flags flags)
        {
            this.transferSize = transferSize;
            this.building = buildingID;
            this.flags = incoming ? CarFlags.None: CarFlags.Sent;

            if ((flags & Vehicle.Flags.Exporting) != 0)
                this.flags |= CarFlags.Exported;
            else if ((flags & Vehicle.Flags.Importing) != 0)
                this.flags |= CarFlags.Imported;

            switch ((TransferType)transferType)
            {
                case TransferType.Oil:
                    this.flags |= CarFlags.Oil;
                    break;
                case TransferType.Ore:
                    this.flags |= CarFlags.Ore;
                    break;
                case TransferType.Logs:
                    this.flags |= CarFlags.Logs;
                    break;
                case TransferType.Grain:
                    this.flags |= CarFlags.Grain;
                    break;
                case TransferType.Petrol:
                    this.flags |= CarFlags.Petrol;
                    break;
                case TransferType.Coal:
                    this.flags |= CarFlags.Coal;
                    break;
                case TransferType.Lumber:
                    this.flags |= CarFlags.Lumber;
                    break;
                case TransferType.Food:
                    this.flags |= CarFlags.Food;
                    break;
                case TransferType.Goods:
                    this.flags |= CarFlags.Goods;
                    break;
                default:
                    Debug.LogErrorFormat("Unexpected transfer type: {0}", Enum.GetName(typeof(TransferType), transferType));
                    break;
            }
        }
    }

    public class CargoData : SerializableDataExtensionBase
    {
        public static CargoData Instance;

        public const float TruckCapacity = 8000f;

        private ModInfo mod;

        private Dictionary<ushort, CargoStats2> cargoStatIndex;
        private HashSet<int> cargoStations;

        public CargoData()
        {
            cargoStations = new HashSet<int>();
            cargoStatIndex = new Dictionary<ushort, CargoStats2>();
            Instance = this;
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

        public override void OnLoadData()
        {
            Debug.Log("Restoring previous data...");
            var data = serializableDataManager.LoadData(ModInfo.Namespace);
            if (data == null)
            {
                Debug.Log("No previous data found");
                return;
            }
            var ms = new MemoryStream(data);
            try
            {
                // Try deserialize older data format first
                var binaryFormatter = new BinaryFormatter();
                var indexData = binaryFormatter.Deserialize(ms);
                if (indexData is Dictionary<ushort, CargoStats>)
                {
                    Debug.LogWarning("Loaded v1 data, upgrading...");
                    cargoStatIndex = ((Dictionary<ushort, CargoStats>) indexData).ToDictionary(kv => kv.Key, kv => new CargoStats2
                    {
                        CarsCounted =
                        {
                            [(int) (CarFlags.Previous | CarFlags.Goods | CarFlags.Sent)] = kv.Value.carsSentLastTime * (int)TruckCapacity,
                            [(int) (CarFlags.Previous | CarFlags.Goods)] = kv.Value.carsReceivedLastTime * (int)TruckCapacity
                        }
                    });
                }
                else if (indexData is Dictionary<ushort, CargoStats2>)
                {
                    Debug.Log("Loaded v2 data");
                    cargoStatIndex = (Dictionary<ushort, CargoStats2>) indexData;
                }
            }
            catch (SerializationException e)
            {
                Debug.LogErrorFormat("While trying to load data: {0}", e.ToString());
            }
            Debug.LogFormat("Loaded stats for {0} stations", cargoStatIndex.Count);
        }

        public void Setup()
        {
            Debug.Log("Looking up cargo station prefabs...");
            cargoStations.Clear();
            for (uint i = 0; i < PrefabCollection<BuildingInfo>.LoadedCount(); i++)
            {
                var prefab = PrefabCollection<BuildingInfo>.GetLoaded(i);
                if (prefab == null)
                {
                    Debug.LogWarningFormat("Uninitialized building prefab #{0}! (this should not happen)", i);
                    continue;
                }
                if (prefab.m_buildingAI is CargoStationAI)
                {
                    Debug.LogFormat("Cargo station prefab found: {0}", prefab.name);
                    cargoStations.Add(prefab.m_prefabDataIndex);
                }
            }
            Debug.LogFormat("Found {0} cargo station prefabs", cargoStations.Count);

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
                binaryFormatter.Serialize(ms, cargoStatIndex);
                serializableDataManager.SaveData(ModInfo.Namespace, ms.ToArray());
                Debug.LogFormat("Saved stats for {0} stations", cargoStatIndex.Count);
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
                cargoStatIndex.Add(buildingID, new CargoStats2());
                Debug.LogFormat("Cargo station added to index: {0}", buildingName);
            }
        }

        public void RemoveBuilding(ushort buildingID)
        {
            if (cargoStatIndex.ContainsKey(buildingID))
            {
                var buildingName = BuildingManager.instance.GetBuildingName(buildingID, InstanceID.Empty);
                cargoStatIndex.Remove(buildingID);
                Debug.LogFormat("Cargo station removed from index: {0}", buildingName);
            }
        }

        public void UpdateCounters()
        {
            foreach (var pair in cargoStatIndex)
            {
                for (var i = 0; i < pair.Value.CarsCounted.Length; i++)
                {
                    var previ = (int) ((CarFlags) i | CarFlags.Previous);
                    if (previ == i) continue;
                    pair.Value.CarsCounted[previ] = pair.Value.CarsCounted[i];
                    pair.Value.CarsCounted[i] = 0;
                }
            }
        }

        public bool TryGetEntry(ushort building, out CargoStats2 stats)
        {
            return cargoStatIndex.TryGetValue(building, out stats);
        }

        public void Count(CargoParcel cargo)
        {
            if (cargo.building == 0 ||
                !(BuildingManager.instance.m_buildings.m_buffer[cargo.building].Info.m_buildingAI is CargoStationAI))
                return;

            if (cargoStatIndex.TryGetValue(cargo.building, out CargoStats2 stats))
                stats.CarsCounted[(int)cargo.flags] += cargo.transferSize;
        }
    }
}
