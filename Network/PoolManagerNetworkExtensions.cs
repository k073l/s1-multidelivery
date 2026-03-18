using DeliveryProject.Pool;
using ScheduleOne.Delivery;

namespace DeliveryProject.Network;


/// <summary>
/// Extensions for PoolManager
/// </summary>
public static class PoolManagerNetworkExtensions
{
    private static DeliveryNetworkManager _networkManager;
 
    public static void InitializeNetworking(this PoolManager poolManager, DeliveryNetworkManager networkManager)
    {
        _networkManager = networkManager;
    }
 
    public static void NotifyVehicleAdded(this PoolManager poolManager, DeliveryVehicle vehicle)
    {
        _networkManager?.BroadcastVehicleAdded(vehicle);
    }
 
    public static void NotifyVehicleRemoved(this PoolManager poolManager, DeliveryVehicle vehicle)
    {
        _networkManager?.BroadcastVehicleRemoved(vehicle);
    }
 
    public static void NotifyAllocation(this PoolManager poolManager, string deliveryId, DeliveryVehicle vehicle, bool isAllocated)
    {
        _networkManager?.BroadcastVehicleAllocation(deliveryId, vehicle, isAllocated);
    }
 
    public static void NotifyBaseAllocation(this PoolManager poolManager, string shopName, bool isAllocated)
    {
        _networkManager?.BroadcastBaseVehicleAllocation(shopName, isAllocated);
    }
}