using HarmonyLib;
using ScheduleOne;
using ScheduleOne.Delivery;
using UnityEngine;
using Console = ScheduleOne.Console;

namespace DeliveryProject.Pool;

[HarmonyPatch(typeof(DeliveryInstance))]
internal static class DeliveryInstancePatches
{
    [HarmonyPatch(nameof(DeliveryInstance.SetStatus))]
    [HarmonyPrefix]
    private static bool UsePoolSetStatus(DeliveryInstance __instance, EDeliveryStatus status)
    {
        Console.Log($"Setting delivery status to {status} for delivery {__instance.DeliveryID}");
        __instance.Status = status;

        var shopInterface = DeliveryManager.Instance.GetShopInterface(__instance.StoreName);
        var regularVehicleAvailable = shopInterface.DeliveryVehicle.ActiveDelivery == null;

        switch (status)
        {
            case EDeliveryStatus.Arrived:

                __instance.ActiveVehicle = regularVehicleAvailable
                    ? shopInterface.DeliveryVehicle
                    : PoolManager.Instance.GetOrAllocateFirstFree(__instance.DeliveryID);
                __instance.ActiveVehicle.Activate(__instance);

                break;
            case EDeliveryStatus.Completed:

                // free allocations
                if (__instance.ActiveVehicle == shopInterface.DeliveryVehicle)
                    PoolManager.Instance.BaseVehicleAllocationsForShop[__instance.StoreName] = false;
                else PoolManager.Instance.FreeAllocation(__instance.DeliveryID);

                if (__instance.ActiveVehicle != null) __instance.ActiveVehicle.Deactivate();
                __instance.onDeliveryCompleted?.Invoke();

                break;
        }

        return false;
    }

    [HarmonyPatch(nameof(DeliveryInstance.AddItemsToDeliveryVehicle))]
    [HarmonyPrefix]
    private static bool UsePoolAddItemsToDeliveryVehicle(DeliveryInstance __instance)
    {
        var shopInterface = DeliveryManager.Instance.GetShopInterface(__instance.StoreName);

        // Use the same logic as SetStatus(Arrived) to determine the vehicle
        DeliveryVehicle deliveryVehicle;
        var regularVehicleAvailable = shopInterface.DeliveryVehicle.ActiveDelivery == null;

        if (regularVehicleAvailable)
        {
            deliveryVehicle = shopInterface.DeliveryVehicle;
        }
        else
        {
            // Check if already allocated, otherwise allocate now
            if (!PoolManager.Instance.Allocations.TryGetValue(__instance.DeliveryID, out deliveryVehicle))
            {
                deliveryVehicle = PoolManager.Instance.GetOrAllocateFirstFree(__instance.DeliveryID);
            }
        }

        var items = __instance.Items;
        foreach (var pair in items)
        {
            var item = Registry.GetItem(pair.String);
            var count = pair.Int;
            while (count > 0)
            {
                var clampedCount = Mathf.Min(count, item.StackLimit);
                count -= clampedCount;
                var defaultInstance = Registry.GetItem(pair.String).GetDefaultInstance(clampedCount);
                deliveryVehicle.Vehicle.Storage.InsertItem(defaultInstance);
            }
        }

        return false;
    }
}