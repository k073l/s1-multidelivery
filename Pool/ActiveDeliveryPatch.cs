using HarmonyLib;
using MelonLoader;
using ScheduleOne.Delivery;
using ScheduleOne.UI.Phone.Delivery;

namespace DeliveryProject.Pool;

[HarmonyPatch(typeof(DeliveryManager))]
internal static class ActiveDeliveryPatch
{
    [HarmonyPatch(nameof(DeliveryManager.GetActiveShopDelivery))]
    [HarmonyPostfix]
    private static void AllowIfPoolAssigmentAvailable(DeliveryManager __instance, DeliveryShop shop,
        ref DeliveryInstance __result)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (__result == null) return;
        if (PoolManager.Instance.BaseVehicleAllocationsForShop.TryGetValue(shop.MatchingShopInterfaceName,
                out var allocated))
        {
            if (UnityEngine.Time.frameCount % 30 == 0) MelonDebug.Msg($"Base: {allocated}");
            if (!allocated)
            {
                __result = null!;
            }
            else
            {
                var freeVehicle = PoolManager.Instance.GetFirstFree();
                if (freeVehicle == null) return;
                MelonDebug.Msg($"Free pool vehicle found");
                __result = null!;
            }
        }
        else
        {
            var freeVehicle = PoolManager.Instance.GetFirstFree();
            if (freeVehicle == null) return;
            MelonDebug.Msg($"Free pool vehicle found");
            __result = null!;
        }
    }

    [HarmonyPatch(nameof(DeliveryManager.SendDelivery))]
    [HarmonyPostfix]
    private static void AllocateVehicle(DeliveryManager __instance, DeliveryInstance delivery)
    {
        PoolManager.Instance.BaseVehicleAllocationsForShop.TryAdd(delivery.StoreName, false);
        MelonDebug.Msg($"Base allocation: {PoolManager.Instance.BaseVehicleAllocationsForShop[delivery.StoreName]}");
        if (!PoolManager.Instance.BaseVehicleAllocationsForShop[delivery.StoreName])
        {
            MelonDebug.Msg("Base vehicle allocating");
            PoolManager.Instance.BaseVehicleAllocationsForShop[delivery.StoreName] = true;
            return;
        }

        PoolManager.Instance.GetOrAllocateFirstFree(delivery.DeliveryID);
        MelonDebug.Msg($"Allocated for {delivery.DeliveryID}");
    }
}