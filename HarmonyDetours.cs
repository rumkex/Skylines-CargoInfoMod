using Harmony;
using System;
using System.Reflection;
using UnityEngine;

namespace CargoInfoMod
{
    static class HarmonyDetours
    {
        public static void Apply()
        {
            var harmony = HarmonyInstance.Create(ModInfo.Namespace);
            Version currentVersion;
            if (harmony.VersionInfo(out currentVersion).ContainsKey(ModInfo.Namespace))
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

        public static void CargoTruckAI_PreChangeVehicleType(out CargoParcel __state, ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position pathPos, uint laneID)
        {
            Vector3 vector = NetManager.instance.m_lanes.m_buffer[laneID].CalculatePosition(0.5f);
            NetInfo info = NetManager.instance.m_segments.m_buffer[pathPos.m_segment].Info;
            ushort buildingID = BuildingManager.instance.FindBuilding(vector, 100f, info.m_class.m_service, ItemClass.SubService.None, Building.Flags.None, Building.Flags.None);

            __state = new CargoParcel(buildingID, true, vehicleData.m_transferType, vehicleData.m_transferSize, vehicleData.m_flags);
        }

        public static void CargoTruckAI_PostChangeVehicleType(bool __result, ref CargoParcel __state, ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position pathPos, uint laneID)
        {
            if (__result)
            {
                CargoData.Instance.Count(__state);
            }
        }

        public static void CargoTruckAI_SetSource(ushort vehicleID, ref Vehicle data, ushort sourceBuilding)
        {
            var parcel = new CargoParcel(sourceBuilding, false, data.m_transferType, data.m_transferSize, data.m_flags);
            CargoData.Instance.Count(parcel);
        }
    }
}
