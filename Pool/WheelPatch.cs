using HarmonyLib;
#if MONO
using ScheduleOne.Vehicles;
using ScheduleOne.Weather;
#else
using Il2CppScheduleOne.Vehicles;
using Il2CppScheduleOne.Weather;
#endif

namespace DeliveryProject.Pool;

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