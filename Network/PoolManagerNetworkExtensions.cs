using DeliveryProject.Pool;
using ScheduleOne.Delivery;

namespace DeliveryProject.Network;

/// <summary>
/// Extensions for PoolManager
/// </summary>
public static class PoolManagerNetworkExtensions
{
    private static DeliveryNetworkManager _networkManager;

    public static void InitializeNetworking(this PoolManager _, DeliveryNetworkManager networkManager)
    {
        _networkManager = networkManager;
    }

    public static void NotifyVehicleAdded(this PoolManager _, DeliveryVehicle vehicle)
    {
        _networkManager?.BroadcastVehicleAdded(vehicle);
    }

    public static void NotifyAllocation(this PoolManager _, string deliveryId, DeliveryVehicle vehicle,
        bool isAllocated)
    {
        _networkManager?.BroadcastVehicleAllocation(deliveryId, vehicle, isAllocated);
    }

    public static void NotifyBaseAllocation(this PoolManager _, string shopName, bool isAllocated)
    {
        _networkManager?.BroadcastBaseVehicleAllocation(shopName, isAllocated);
    }
}