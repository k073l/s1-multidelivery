using System.Collections;
using DeliveryProject.Builders;
using DeliveryProject.Pool;
using MelonLoader;
using S1API.Internal.Abstraction;
using S1API.Saveables;
using ScheduleOne.Delivery;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Vehicles;
using ScheduleOne.Vehicles.Modification;

namespace DeliveryProject.Persistence;

public class VehicleSave : Saveable
{
    [SaveableField("VehicleSaveData")] private List<VehicleSaveDto> _dtos = [];
    public override SaveableLoadOrder LoadOrder => SaveableLoadOrder.BeforeBaseGame;

    public static VehicleSave Instance { get; private set; } = new();

    public VehicleSave()
    {
        Instance = this;
    }

    protected override void OnLoaded()
    {
        Melon<DeliveryProject>.Logger.Msg($"VehicleSave.OnLoaded: Loading {_dtos.Count} vehicles");

        for (var i = 0; i < _dtos.Count; ++i)
        {
            MelonDebug.Msg($"Loading vehicle {i}: {_dtos[i].Guid}");

            var guid = Guid.Parse(_dtos[i].Guid);
            var vehicle = new LandVehicleBuilder()
                .WithVehicleCode(_dtos[i].VehicleType)
                .WithVehicleName($"Additional Delivery Vehicle {i + 1}")
                .WithGuid(guid)
                .WithColor(_dtos[i].Color)
                .Build();

            // Color is not properly applied if we load before base game
            MelonCoroutines.Start(DeferredSetColor(vehicle, _dtos[i].Color));

            MelonDebug.Msg($"Built LandVehicle: {vehicle.vehicleName}, ObjectId: {vehicle.ObjectId}");

            var deliveryVehicle = new DeliveryVehicleBuilder()
                .WithLandVehicle(vehicle)
                .WithGuid(guid)
                .Build();

            MelonDebug.Msg($"Built DeliveryVehicle: {deliveryVehicle.GUID}");

            PoolManager.Instance.AddToPool(deliveryVehicle);

            MelonDebug.Msg($"Added to pool. Pool count now: {PoolManager.Instance.Pool.Count}");
        }

        Melon<DeliveryProject>.Logger.Msg(
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