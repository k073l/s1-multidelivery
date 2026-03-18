using MelonLoader;
using ScheduleOne.Delivery;
using ScheduleOne.UI.Shop;

namespace DeliveryProject.Pool;

public class PoolManager
{
    public static PoolManager Instance { get; } = new();

    private HashSet<DeliveryVehicle> Pool { get; } = [];
    internal Dictionary<string, DeliveryVehicle> Allocations { get; } = new();

    internal Dictionary<string, bool> BaseVehicleAllocationsForShop { get; } = new();

    public void AddToPool(DeliveryVehicle deliveryVehicle) => Pool.Add(deliveryVehicle);

    public DeliveryVehicle? GetFirstFree()
    {
        foreach (var dv in Pool)
        {
            MelonDebug.Msg(
                $"Vehicle in pool - ActiveDelivery: {(dv.ActiveDelivery == null ? "null" : dv.ActiveDelivery.DeliveryID + " (Status: " + dv.ActiveDelivery.Status + ")")}");
        }

        var free = Pool.FirstOrDefault(dv => dv.ActiveDelivery == null);

        MelonDebug.Msg($"Pool lookup result: {(free == null ? "null" : "not null")}");

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
        return free;
    }

    public void FreeAllocation(string deliveryId)
    {
        MelonDebug.Msg($"Free custom allocation: {deliveryId}");
        Allocations.Remove(deliveryId);
    }
}