using HarmonyLib;
using ScheduleOne.Delivery;
using ScheduleOne.Vehicles;

namespace DeliveryProject.Pool;

[HarmonyPatch(typeof(DeliveryVehicle))]
internal class DeliveryVehiclePatch
{
    [HarmonyPatch(nameof(DeliveryVehicle.Deactivate))]
    [HarmonyPostfix]
    private static void NullActiveDelivery(DeliveryVehicle __instance)
    {
        if (__instance.ActiveDelivery != null) __instance.ActiveDelivery = null;
    }
}