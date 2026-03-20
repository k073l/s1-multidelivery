#if MONO
using ScheduleOne.Delivery;
#else
using Il2CppScheduleOne.Delivery;
#endif

namespace DeliveryProject.Network;

/// <summary>
/// Convenience methods for <see cref="DeliveryNetworkManager"/>.
/// Provides a static API to broadcast actions.
/// </summary>
public static class NetworkConvenienceMethods
{
    private static DeliveryNetworkManager? _networkManager;

    public static void InitializeNetworking(DeliveryNetworkManager networkManager)
    {
        _networkManager = networkManager;
    }

    public static void NotifyVehicleAdded(DeliveryVehicle vehicle)
    {
        _networkManager?.BroadcastVehicleAdded(vehicle);
    }

    public static void NotifyVehicleCreated(DeliveryVehicle vehicle)
    {
        _networkManager?.BroadcastVehicleCreated(vehicle);
    }

    public static void NotifyAllocation(string deliveryId, DeliveryVehicle vehicle,
        bool isAllocated)
    {
        _networkManager?.BroadcastVehicleAllocation(deliveryId, vehicle, isAllocated);
    }

    public static void NotifyBaseAllocation(string shopName, bool isAllocated)
    {
        _networkManager?.BroadcastBaseVehicleAllocation(shopName, isAllocated);
    }
}