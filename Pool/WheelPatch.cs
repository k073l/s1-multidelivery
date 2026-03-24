using HarmonyLib;
#if MONO
using Guid = System.Guid;
using ScheduleOne.Delivery;
using ScheduleOne.Vehicles;
using ScheduleOne.Weather;
#else
using Guid = Il2CppSystem.Guid;
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.Vehicles;
using Il2CppScheduleOne.Weather;
#endif

namespace MultiDelivery.Pool;

[HarmonyPatch(typeof(Wheel))]
internal class WheelPatch
{
    [HarmonyPatch(nameof(Wheel.OnWeatherChange))]
    [HarmonyPrefix]
    private static bool ExitIfNull(Wheel __instance, WeatherConditions newConditions)
    {
        if (__instance?.vehicle == null) return false;
        if (newConditions?.Rainy == null) return false;
        return true;
    }
}

[HarmonyPatch(typeof(DeliveryVehicle))]
internal static class DeliveryVehicleAwakePatch
{
    [HarmonyPatch(nameof(DeliveryVehicle.Awake))]
    [HarmonyPrefix]
    private static bool ExitIfNull(DeliveryVehicle __instance)
    {
        // skip guid setting if invalid
        if (Guid.TryParse(__instance.GUID, out var _)) return true;
        if (__instance.GetComponent<LandVehicle>() == null) return false;
        __instance.Vehicle = __instance.GetComponent<LandVehicle>();
        __instance.Deactivate();
        return false;
    }
}
