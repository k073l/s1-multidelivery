using HarmonyLib;
using MultiDelivery.Network;
using UnityEngine;
#if MONO
using ScheduleOne;
using ScheduleOne.Delivery;
using ScheduleOne.UI.Shop;
using Console = ScheduleOne.Console;
#else
using Il2CppScheduleOne;
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.UI.Shop;
using Console = Il2CppScheduleOne.Console;
#endif

namespace MultiDelivery.Pool;

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
        
        switch (status)
        {
            case EDeliveryStatus.Arrived:
                if (PoolManager.Instance.Allocations.TryGetValue(__instance.DeliveryID, out var allocatedVehicle))
                {
                    // Use the pre-allocated pool vehicle
                    __instance.ActiveVehicle = allocatedVehicle;
                }
                else
                {
                    // Use the base vehicle (first delivery for this shop)
                    __instance.ActiveVehicle = shopInterface.DeliveryVehicle;
                }
                __instance.ActiveVehicle.Activate(__instance);
                break;
                
            case EDeliveryStatus.Completed:
                // Free allocations and notify network
                if (__instance.ActiveVehicle == shopInterface.DeliveryVehicle)
                {
                    PoolManager.Instance.BaseVehicleAllocationsForShop[__instance.StoreName] = false;
                    
                    // Notify network of base allocation freed
                    NetworkConvenienceMethods.NotifyBaseAllocation(__instance.StoreName, false);
                }
                else
                {
                    PoolManager.Instance.FreeAllocation(__instance.DeliveryID);
                }
                
                if (__instance.ActiveVehicle != null) 
                    __instance.ActiveVehicle.Deactivate();
                    
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
    
        // Same logic as patch above
        DeliveryVehicle deliveryVehicle;
    
        // Check if this delivery has a pool vehicle allocated
        if (PoolManager.Instance.Allocations.TryGetValue(__instance.DeliveryID, out deliveryVehicle))
        {
            // Use allocated pool vehicle
        }
        else
        {
            // Use base vehicle (first delivery)
            deliveryVehicle = shopInterface.DeliveryVehicle;
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