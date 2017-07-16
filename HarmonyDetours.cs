using Harmony;
using System;
using System.Reflection;
using UnityEngine;

namespace CargoInfoMod
{
    static class HarmonyDetours
    {
        public const string patchNamespace = "com.github.rumkex.cargomod";

        public static void Apply()
        {
            var harmony = HarmonyInstance.Create(patchNamespace);
            Version currentVersion;
            if (harmony.VersionInfo(out currentVersion).ContainsKey(patchNamespace))
            {
                Debug.LogWarning("Harmony patches already present");
                return;
            }
            Debug.Log("Harmony v" + currentVersion);

            var truckSetSource = typeof(CargoTruckAI).GetMethod("SetSource");
            var truckSetSourcePostfix = typeof(HarmonyDetours).GetMethod("CargoTruckAI_SetSource");
            var truckChangeVehicleType = typeof(CargoTruckAI).GetMethod("ChangeVehicleType", BindingFlags.Instance | BindingFlags.NonPublic);
            var truckChangeVehicleTypePrefix = typeof(HarmonyDetours).GetMethod("CargoTruckAI_PreChangeVehicleType");
            var truckChangeVehicleTypePostfix = typeof(HarmonyDetours).GetMethod("CargoTruckAI_PostChangeVehicleType");

            Debug.Log("Patching CargoTruckAI...");
            harmony.Patch(
                truckSetSource,
                null,
                new HarmonyMethod(truckSetSourcePostfix)
                );
            harmony.Patch(
                truckChangeVehicleType,
                new HarmonyMethod(truckChangeVehicleTypePrefix),
                new HarmonyMethod(truckChangeVehicleTypePostfix)
                );

            Debug.Log("Harmony patches applied");
        }

        public struct VehicleLoad
        {
            public ushort buildingID;
            public ushort transferSize;
        }

        public static void CargoTruckAI_PreChangeVehicleType(out VehicleLoad __state, ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position pathPos, uint laneID)
        {
            Vector3 vector = NetManager.instance.m_lanes.m_buffer[laneID].CalculatePosition(0.5f);
            NetInfo info = NetManager.instance.m_segments.m_buffer[pathPos.m_segment].Info;
            ushort buildingID = BuildingManager.instance.FindBuilding(vector, 100f, info.m_class.m_service, ItemClass.SubService.None, Building.Flags.None, Building.Flags.None);

            __state.buildingID = buildingID;
            __state.transferSize = vehicleData.m_transferSize;
        }

        public static void CargoTruckAI_PostChangeVehicleType(bool __result, ref VehicleLoad __state, ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position pathPos, uint laneID)
        {
            if (__result && __state.buildingID != 0 && BuildingManager.instance.m_buildings.m_buffer[__state.buildingID].Info.m_buildingAI is CargoStationAI)
            {
                if (!CargoCounter.instance.cargoStatIndex.ContainsKey(__state.buildingID))
                {
                    CargoCounter.instance.cargoStatIndex.Add(__state.buildingID, new CargoStats());
                }
                CargoCounter.instance.cargoStatIndex[__state.buildingID].carsReceived++;
            }
        }

        public static void CargoTruckAI_SetSource(ushort vehicleID, ref Vehicle data, ushort sourceBuilding)
        {
            if (sourceBuilding != 0 && BuildingManager.instance.m_buildings.m_buffer[sourceBuilding].Info.m_buildingAI is CargoStationAI)
            {
                if (!CargoCounter.instance.cargoStatIndex.ContainsKey(sourceBuilding))
                {
                    CargoCounter.instance.cargoStatIndex.Add(sourceBuilding, new CargoStats());
                }
                CargoCounter.instance.cargoStatIndex[sourceBuilding].carsSent++;
            }
        }
    }
}
