using MelonLoader;
using MultiDelivery.Network;
using MultiDelivery.Persistence;
using UnityEngine;
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
    internal static readonly Logger Logger = new(nameof(PoolManager));

    public HashSet<DeliveryVehicle> Pool { get; } = [];
    internal Dictionary<string, DeliveryVehicle> Allocations { get; } = new();
    internal Dictionary<string, bool> BaseVehicleAllocationsForShop { get; } = new();

    internal static readonly object Lock = new();

    public void AddToPool(DeliveryVehicle deliveryVehicle, bool notify = true)
    {
        lock (Lock)
        {
            Pool.Add(deliveryVehicle);

            // Notify network (if available)
            if (notify)
                NetworkConvenienceMethods.NotifyVehicleAdded(deliveryVehicle);
        }
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
        lock (Lock)
        {
            var free = Pool.FirstOrDefault(dv => dv.ActiveDelivery == null && !Allocations.ContainsValue(dv));

            if (Time.frameCount % 30 == 0)
                Logger.Debug($"Pool lookup: {(free == null ? "null" : "not null")}");

            return free;
        }
    }

    public DeliveryVehicle GetOrAllocateFirstFree(string deliveryId)
    {
        lock (Lock)
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

            VehicleSave.Instance.AddAllocation(deliveryId, free);

            // Notify network of allocation
            NetworkConvenienceMethods.NotifyAllocation(deliveryId, free, isAllocated: true);

            return free;
        }
    }

    internal void AllocateDirectly(string deliveryId, DeliveryVehicle deliveryVehicle)
    {
        lock (Lock)
        {
            Logger.Debug($"AllocateDirectly called for: {deliveryId}");
            if (Allocations.TryGetValue(deliveryId, out _))
            {
                Logger.Debug($"Already allocated, returning");
                return;
            }
            Allocations.Add(deliveryId, deliveryVehicle);
            Logger.Debug($"Directly allocated vehicle for {deliveryId}, vehicle: {deliveryVehicle.GUID}");

            VehicleSave.Instance.AddAllocation(deliveryId, deliveryVehicle);

            NetworkConvenienceMethods.NotifyAllocation(deliveryId, deliveryVehicle, isAllocated: true);
        }
    }

    internal void SetBaseAllocation(string shopName, bool allocated)
    {
        lock (Lock)
        {
            BaseVehicleAllocationsForShop[shopName] = allocated;
        }
        VehicleSave.Instance.SyncBaseAllocation(shopName, allocated);
        NetworkConvenienceMethods.NotifyBaseAllocation(shopName, allocated);
    }

    public void FreeAllocation(string deliveryId)
    {
        lock (Lock)
        {
            Logger.Debug($"Free custom allocation: {deliveryId}");

            if (Allocations.TryGetValue(deliveryId, out var deliveryVehicle))
            {
                Allocations.Remove(deliveryId);
                VehicleSave.Instance.RemoveAllocation(deliveryId);

                // Notify network of deallocation
                NetworkConvenienceMethods.NotifyAllocation(deliveryId, deliveryVehicle, isAllocated: false);
            }
        }
    }

    internal int PoolCount
    {
        get { lock (Lock) return Pool.Count; }
    }

    internal int AllocationCount
    {
        get { lock (Lock) return Allocations.Count; }
    }

    internal void ClearAll()
    {
        lock (Lock)
        {
            Pool.Clear();
            Allocations.Clear();
            BaseVehicleAllocationsForShop.Clear();
        }
        var save = VehicleSave.Instance;
        save.ClearAllocations();
        save.ClearBaseAllocations();
    }

    internal bool RemoveFromPool(DeliveryVehicle vehicle)
    {
        lock (Lock)
        {
            var keys = Allocations.Where(x => x.Value == vehicle).Select(x => x.Key).ToList();
            foreach (var key in keys)
            {
                Allocations.Remove(key);
                VehicleSave.Instance.RemoveAllocation(key);
            }
            VehicleSave.Instance.RemoveAllocationsForVehicle(vehicle);
            return Pool.Remove(vehicle);
        }
    }

    internal DeliveryVehicle? LastPoolVehicle()
    {
        lock (Lock)
        {
            return Pool.LastOrDefault();
        }
    }
}