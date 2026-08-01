#if MONO
using Steamworks;
using ScheduleOne.Delivery;
using ScheduleOne.UI.Shop;
using ScheduleOne.Vehicles;
using ScheduleOne.Vehicles.Modification;

#else
using Il2CppSteamworks;
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.UI.Shop;
using Il2CppScheduleOne.Vehicles;
using Il2CppScheduleOne.Vehicles.Modification;
using Guid = Il2CppSystem.Guid;
#endif

namespace MultiDelivery.Network;

public interface IDeliveryNetworkManager : IDisposable
{
    bool IsInLobby { get; }
    bool IsHost { get; }
    bool IsSingleplayer { get; }
    bool HostOrSingleplayer { get; }
    
    bool Initialize();
    void Update();
    void BroadcastVehicleAdded(DeliveryVehicle vehicle);
    void BroadcastVehicleCreated(DeliveryVehicle vehicle);
    void BroadcastVehicleAllocation(string deliveryId, DeliveryVehicle vehicle, bool isAllocated);
    void BroadcastBaseVehicleAllocation(string shopName, bool isAllocated);
}