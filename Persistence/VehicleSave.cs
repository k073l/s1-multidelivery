using System.Collections;
using MelonLoader;
using MultiDelivery.Builders;
using MultiDelivery.Pool;
using S1API.Internal.Abstraction;
using S1API.Saveables;
using UnityEngine;
#if MONO
using ScheduleOne.Delivery;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI.Shop;
using ScheduleOne.Vehicles;
using ScheduleOne.Vehicles.Modification;
using Guid = System.Guid;
#else
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI.Shop;
using Il2CppScheduleOne.Vehicles;
using Il2CppScheduleOne.Vehicles.Modification;
using Guid = Il2CppSystem.Guid;
#endif

namespace MultiDelivery.Persistence;

[Serializable]
public class AllocationEntry
{
    public string DeliveryID;
    public string VehicleGUID;
    public string? ShopName;
    public bool IsBase;
}

[Serializable]
public class BaseAllocationEntry
{
    public string ShopName;
    public bool IsAllocated;
}

public class VehicleSave : Saveable
{
    [SaveableField("VehicleSaveData")] private List<VehicleSaveDto> _dtos = [];
    [SaveableField("AllocationMap")] private List<AllocationEntry> _allocationMap = [];
    [SaveableField("BaseAllocationMap")] private List<BaseAllocationEntry> _baseAllocationMap = [];
    public override SaveableLoadOrder LoadOrder => SaveableLoadOrder.BeforeBaseGame;

    public IReadOnlyList<AllocationEntry> AllocationMap => _allocationMap;
    public IReadOnlyList<BaseAllocationEntry> BaseAllocationMap => _baseAllocationMap;

    private static readonly Logger Logger = new(nameof(VehicleSave));
    private static Transform _parent;
    public static VehicleSave Instance { get; set; } = new();

    public VehicleSave()
    {
        Instance = this;
    }

    public void AddVehicle(LandVehicle vehicle)
    {
        var dto = new VehicleSaveDto
        {
            Guid = vehicle.GUID.ToString(),
            VehicleType = vehicle.VehicleCode,
            Color = vehicle.Color.displayedColor,
        };
        if (_dtos.Contains(dto)) return;
        _dtos.Add(dto);
    }

    internal void AddAllocation(string deliveryId, DeliveryVehicle vehicle)
    {
        var guid = GetDeliveryVehicleGuid(vehicle);
        string? shopName = null;
        var isBase = false;

        foreach (var shop in ShopInterface.AllShops)
        {
            if (shop?.DeliveryVehicle == vehicle)
            {
                shopName = shop.ShopName;
                isBase = true;
                break;
            }
        }

        _allocationMap.RemoveAll(e => e.DeliveryID == deliveryId);
        _allocationMap.Add(new AllocationEntry
        {
            DeliveryID = deliveryId, 
            VehicleGUID = guid,
            ShopName = shopName,
            IsBase = isBase,
        });
    }

    internal void RemoveAllocation(string deliveryId)
    {
        _allocationMap.RemoveAll(e => e.DeliveryID == deliveryId);
    }

    internal void RemoveAllocationsForVehicle(DeliveryVehicle vehicle)
    {
        var guid = GetDeliveryVehicleGuid(vehicle);
        _allocationMap.RemoveAll(e => e.VehicleGUID == guid);
    }

    internal void ClearAllocations()
    {
        _allocationMap.Clear();
    }

    internal void ClearBaseAllocations()
    {
        _baseAllocationMap.Clear();
    }

    internal void SyncBaseAllocation(string shopName, bool allocated)
    {
        _baseAllocationMap.RemoveAll(e => e.ShopName == shopName);
        _baseAllocationMap.Add(new BaseAllocationEntry { ShopName = shopName, IsAllocated = allocated });
    }

    internal void RemoveVehicle(DeliveryVehicle vehicle)
    {
        var guid = GetDeliveryVehicleGuid(vehicle);
        _dtos.RemoveAll(dto => dto.Guid.ToString() == guid);
    }

    protected override void OnLoaded()
    {
        Logger.Msg($"OnLoaded: Loading {_dtos.Count} vehicles");
        if (_parent == null)
        {
            // try getting it first
            var rootGo = GameObject.Find("VehiclePool");
            if (rootGo == null) rootGo = new GameObject("VehiclePool");
            _parent = rootGo.transform;
        }

        for (var i = 0; i < _dtos.Count; ++i)
        {
            Logger.Debug($"Loading vehicle {i}: {_dtos[i].Guid}");

            var guid = new Guid(_dtos[i].Guid);
            var vehicle = new LandVehicleBuilder()
                .WithVehicleCode(_dtos[i].VehicleType)
                .WithVehicleName($"Additional Delivery Vehicle {i + 1}")
                .WithGuid(guid)
                .WithColor(_dtos[i].Color)
                .WithParent(_parent)
                .Build();

            // Color is not properly applied if we load before base game
            MelonCoroutines.Start(DeferredSetColor(vehicle, _dtos[i].Color));

            Logger.Debug($"Built LandVehicle: {vehicle.vehicleName}, ObjectId: {vehicle.ObjectId}");

            var deliveryVehicle = new DeliveryVehicleBuilder()
                .WithLandVehicle(vehicle)
                .WithGuid(guid)
                .Build();

            Logger.Debug($"Built DeliveryVehicle: {deliveryVehicle.GUID}");

            PoolManager.Instance.AddToPool(deliveryVehicle);

            Logger.Debug($"Added to pool. Pool count now: {PoolManager.Instance.PoolCount}");
        }

        Logger.Msg($"OnLoaded complete. Final pool count: {PoolManager.Instance.PoolCount}");
    }

    private IEnumerator DeferredSetColor(LandVehicle vehicle, EVehicleColor color)
    {
        while (Player.Local == null || Player.Local.gameObject == null)
            yield return null;
        yield return null;
        vehicle.ApplyColor(color);
    }

    private static string GetDeliveryVehicleGuid(DeliveryVehicle vehicle)
    {
        Guid vehicleGuid;
        if (vehicle.Vehicle != null)
        {
            vehicleGuid = vehicle.Vehicle.GUID;
        }
        else if (Guid.TryParse(vehicle.GUID, out var parsedGuid))
        {
            vehicleGuid = parsedGuid;
        }
        else
        {
            vehicleGuid = Guid.Empty;
        }

        return vehicleGuid.ToString();
    }
}