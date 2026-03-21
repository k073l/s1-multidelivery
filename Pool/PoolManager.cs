using MelonLoader;
using MultiDelivery.Network;
using MultiDelivery.Persistence;
#if MONO
using ScheduleOne.Delivery;
using ScheduleOne.UI.Shop;
#else
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.UI.Shop;
#endif

namespace MultiDelivery.Pool;

public class PoolManager
{
    public static PoolManager Instance { get; } = new();
    private static readonly Logger Logger = new(nameof(PoolManager));

    public HashSet<DeliveryVehicle> Pool { get; } = [];
    internal Dictionary<string, DeliveryVehicle> Allocations { get; } = new();
    internal Dictionary<string, bool> BaseVehicleAllocationsForShop { get; } = new();

    public void AddToPool(DeliveryVehicle deliveryVehicle, bool notify = true)
    {
        Pool.Add(deliveryVehicle);

        // Notify network (if available)
        if (notify)
            NetworkConvenienceMethods.NotifyVehicleAdded(deliveryVehicle);
    }

    public void AddToSaveData(DeliveryVehicle deliveryVehicle)
    {
        if (deliveryVehicle == null) throw new ArgumentNullException(nameof(deliveryVehicle));
        VehicleSave.Instance.AddVehicle(deliveryVehicle.Vehicle);
        
        // Notify network (if available)
        NetworkConvenienceMethods.NotifyVehicleCreated(deliveryVehicle);
    }

    public DeliveryVehicle? GetFirstFree()
    {
        var free = Pool.FirstOrDefault(dv => dv.ActiveDelivery == null && !Allocations.ContainsValue(dv));

        if (UnityEngine.Time.frameCount % 30 == 0)
            Logger.Debug($"Pool lookup: {(free == null ? "null" : "not null")}");

        return free;
    }

    public DeliveryVehicle GetOrAllocateFirstFree(string deliveryId)
    {
        Logger.Debug($"GetOrAllocateFirstFree called for: {deliveryId}");

        if (Allocations.TryGetValue(deliveryId, out var allocated))
        {
            Logger.Debug($"Already allocated, returning existing");
            return allocated;
        }

        var free = GetFirstFree();
        if (free == null)
        {
            Logger.Debug($"No free vehicle found! Pool count: {Pool.Count}, Allocations count: {Allocations.Count}");
            foreach (var alloc in Allocations)
            {
                Logger.Debug(
                    $"Allocation: {alloc.Key} -> Vehicle has ActiveDelivery: {alloc.Value.ActiveDelivery?.DeliveryID ?? "null"}");
            }

            throw new ArgumentException("Tried to allocate a non-free vehicle");
        }

        Allocations.Add(deliveryId, free);
        Logger.Debug($"Allocated new vehicle for {deliveryId}, vehicle: {free.GUID}");

        // Notify network of allocation
        NetworkConvenienceMethods.NotifyAllocation(deliveryId, free, isAllocated: true);

        return free;
    }

    public void FreeAllocation(string deliveryId)
    {
        Logger.Debug($"Free custom allocation: {deliveryId}");

        if (Allocations.TryGetValue(deliveryId, out var deliveryVehicle))
        {
            Allocations.Remove(deliveryId);

            // Notify network of deallocation
            NetworkConvenienceMethods.NotifyAllocation(deliveryId, deliveryVehicle, isAllocated: false);
        }
    }
}