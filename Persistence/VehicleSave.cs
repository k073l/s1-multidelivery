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
using ScheduleOne.Vehicles;
using ScheduleOne.Vehicles.Modification;
using Guid = System.Guid;
#else
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Vehicles;
using Il2CppScheduleOne.Vehicles.Modification;
using Guid = Il2CppSystem.Guid;
#endif

namespace MultiDelivery.Persistence;

public class VehicleSave : Saveable
{
    [SaveableField("VehicleSaveData")] private List<VehicleSaveDto> _dtos = [];
    public override SaveableLoadOrder LoadOrder => SaveableLoadOrder.BeforeBaseGame;

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

    protected override void OnLoaded()
    {
        Melon<MultiDelivery>.Logger.Msg($"VehicleSave.OnLoaded: Loading {_dtos.Count} vehicles");
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

            Logger.Debug($"Added to pool. Pool count now: {PoolManager.Instance.Pool.Count}");
        }

        Logger.Msg(
            $"VehicleSave.OnLoaded complete. Final pool count: {PoolManager.Instance.Pool.Count}");
    }

    private IEnumerator DeferredSetColor(LandVehicle vehicle, EVehicleColor color)
    {
        while (Player.Local == null || Player.Local.gameObject == null)
            yield return null;
        yield return null;
        vehicle.ApplyColor(color);
    }
}