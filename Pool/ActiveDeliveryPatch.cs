using HarmonyLib;
using MelonLoader;
using MultiDelivery.Network;
using MultiDelivery.Persistence;
#if MONO
using ScheduleOne.Delivery;
using ScheduleOne.UI.Phone.Delivery;
#else
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.UI.Phone.Delivery;
#endif

namespace MultiDelivery.Pool;

[HarmonyPatch(typeof(DeliveryManager))]
internal static class ActiveDeliveryPatch
{
    internal static readonly Logger Logger = new(nameof(ActiveDeliveryPatch));
    
    [HarmonyPatch(nameof(DeliveryManager.GetActiveShopDelivery))]
    [HarmonyPostfix]
    private static void AllowIfPoolAssigmentAvailable(DeliveryManager __instance, DeliveryShop shop,
        ref DeliveryInstance __result)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (__result == null) return;

        lock (PoolManager.Lock)
        {
            if (PoolManager.Instance.BaseVehicleAllocationsForShop.TryGetValue(shop.MatchingShopInterfaceName,
                    out var allocated))
            {
                if (UnityEngine.Time.frameCount % 30 == 0) Logger.Debug($"Base: {allocated}");
                if (!allocated)
                {
                    __result = null!;
                }
                else
                {
                    var freeVehicle = PoolManager.Instance.GetFirstFree();
                    if (freeVehicle == null) return;
                    Logger.Debug($"Free pool vehicle found");
                    __result = null!;
                }
            }
            else
            {
                var freeVehicle = PoolManager.Instance.GetFirstFree();
                if (freeVehicle == null) return;
                Logger.Debug($"Free pool vehicle found");
                __result = null!;
            }
        }
    }

    [HarmonyPatch(nameof(DeliveryManager.SendDelivery))]
    [HarmonyPostfix]
    private static void AllocateVehicle(DeliveryManager __instance, DeliveryInstance delivery)
    {
        lock (PoolManager.Lock)
        {
            // Already allocated (e.g. from save data restoration) - skip
            if (PoolManager.Instance.Allocations.ContainsKey(delivery.DeliveryID))
            {
                Logger.Debug($"Already allocated, skipping");
                return;
            }

            PoolManager.Instance.BaseVehicleAllocationsForShop.TryAdd(delivery.StoreName, false);
            Logger.Debug($"Base allocation: {PoolManager.Instance.BaseVehicleAllocationsForShop[delivery.StoreName]}");

            if (!PoolManager.Instance.BaseVehicleAllocationsForShop[delivery.StoreName])
            {
                var baseVehicle = DeliveryManager.Instance.GetShopInterface(delivery.StoreName).DeliveryVehicle;
                Logger.Debug($"Base vehicle allocating shop={delivery.StoreName} target={baseVehicle.GUID}");
                PoolManager.Instance.SetBaseAllocation(delivery.StoreName, true);
                PoolManager.Instance.Allocations.Add(delivery.DeliveryID, baseVehicle);
                VehicleSave.Instance.AddAllocation(delivery.DeliveryID, baseVehicle);
                NetworkConvenienceMethods.NotifyAllocation(delivery.DeliveryID, baseVehicle, isAllocated: true);
                return;
            }

            PoolManager.Instance.GetOrAllocateFirstFree(delivery.DeliveryID);
            Logger.Debug($"Custom allocated shop={delivery.StoreName} {delivery.DeliveryID}");
        }
    }
}