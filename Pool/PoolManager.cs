using DeliveryProject.Network;
using MelonLoader;
using ScheduleOne.Delivery;
using ScheduleOne.UI.Shop;

namespace DeliveryProject.Pool;

public class PoolManager
{
    public static PoolManager Instance { get; } = new();

    public HashSet<DeliveryVehicle> Pool { get; } = [];
    internal Dictionary<string, DeliveryVehicle> Allocations { get; } = new();
    internal Dictionary<string, bool> BaseVehicleAllocationsForShop { get; } = new();

    public void AddToPool(DeliveryVehicle deliveryVehicle)
    {
        Pool.Add(deliveryVehicle);

        // Notify network (if available)
        this.NotifyVehicleAdded(deliveryVehicle);
    }

    public DeliveryVehicle? GetFirstFree()
    {
        var free = Pool.FirstOrDefault(dv => dv.ActiveDelivery == null && !Allocations.ContainsValue(dv));

        if (UnityEngine.Time.frameCount % 30 == 0)
            MelonDebug.Msg($"Pool lookup: {(free == null ? "null" : "not null")}");

        return free;
    }

    public DeliveryVehicle GetOrAllocateFirstFree(string deliveryId)
    {
        MelonDebug.Msg($"GetOrAllocateFirstFree called for: {deliveryId}");

        if (Allocations.TryGetValue(deliveryId, out var allocated))
        {
            MelonDebug.Msg($"Already allocated, returning existing");
            return allocated;
        }

        var free = GetFirstFree();
        if (free == null)
        {
            MelonDebug.Msg($"No free vehicle found! Pool count: {Pool.Count}, Allocations count: {Allocations.Count}");
            foreach (var alloc in Allocations)
            {
                MelonDebug.Msg(
                    $"Allocation: {alloc.Key} -> Vehicle has ActiveDelivery: {alloc.Value.ActiveDelivery?.DeliveryID ?? "null"}");
            }

            throw new ArgumentException("Tried to allocate a non-free vehicle");
        }

        Allocations.Add(deliveryId, free);
        MelonDebug.Msg($"Allocated new vehicle for {deliveryId}");

        // Notify network of allocation
        this.NotifyAllocation(deliveryId, free, isAllocated: true);

        return free;
    }

    public void FreeAllocation(string deliveryId)
    {
        MelonDebug.Msg($"Free custom allocation: {deliveryId}");

        if (Allocations.TryGetValue(deliveryId, out var deliveryVehicle))
        {
            Allocations.Remove(deliveryId);

            // Notify network of deallocation
            this.NotifyAllocation(deliveryId, deliveryVehicle, isAllocated: false);
        }
    }
}