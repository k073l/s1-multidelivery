using System.Collections;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
#if MONO
using ScheduleOne.Delivery;
using ScheduleOne.Vehicles;
#else
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.Vehicles;
#endif

namespace MultiDelivery.Pool;

[HarmonyPatch(typeof(DeliveryVehicle))]
internal class DeliveryVehiclePatch
{
    [HarmonyPatch(nameof(DeliveryVehicle.Deactivate))]
    [HarmonyPostfix]
    private static void NullActiveDelivery(DeliveryVehicle __instance)
    {
        // nulling dynamic should be fine, as dynamic==player vehicle, and they can't get on the dock while delivery (static occ) is on it
        if (__instance.ActiveDelivery?.LoadingDock?.VehicleDetector != null)
        {
            __instance.ActiveDelivery.LoadingDock.SetOccupant(null);
            __instance.ActiveDelivery.LoadingDock.VehicleDetector.Clear(); // stale vehicles
        }
        if (__instance.ActiveDelivery != null) __instance.ActiveDelivery = null;
    }
}