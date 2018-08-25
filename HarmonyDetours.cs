using Harmony;
using System;
using System.Reflection;
using UnityEngine;

namespace CargoInfoMod
{
    static class HarmonyDetours
    {
        private static void ConditionalPatch(this HarmonyInstance harmony, MethodBase method, HarmonyMethod prefix, HarmonyMethod postfix)
        {
            var fullMethodName = string.Format("{0}.{1}", method.ReflectedType?.Name ?? "(null)", method.Name);
            if (harmony.GetPatchInfo(method)?.Owners?.Contains(harmony.Id) == true)
            {
                Debug.LogWarningFormat("Harmony patches already present for {0}", fullMethodName);
            }
            else
            {
                Debug.LogFormat("Patching {0}...", fullMethodName);
                harmony.Patch(method, prefix, postfix);
            }
        }

        public static void Apply()
        {
            var harmony = HarmonyInstance.Create(ModInfo.Namespace);

            var truckSetSource = typeof(CargoTruckAI).GetMethod("SetSource");
            var truckSetSourcePostfix = typeof(HarmonyDetours).GetMethod("CargoTruckAI_SetSource");
            var truckChangeVehicleType = typeof(CargoTruckAI).GetMethod("ChangeVehicleType", BindingFlags.Instance | BindingFlags.NonPublic);
            var truckChangeVehicleTypePrefix = typeof(HarmonyDetours).GetMethod("CargoTruckAI_PreChangeVehicleType");
            var truckChangeVehicleTypePostfix = typeof(HarmonyDetours).GetMethod("CargoTruckAI_PostChangeVehicleType");

            harmony.ConditionalPatch(truckSetSource,
                null,
                new HarmonyMethod(truckSetSourcePostfix));

            harmony.ConditionalPatch(truckChangeVehicleType,
                new HarmonyMethod(truckChangeVehicleTypePrefix),
                new HarmonyMethod(truckChangeVehicleTypePostfix));

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
