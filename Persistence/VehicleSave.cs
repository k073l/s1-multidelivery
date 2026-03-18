using DeliveryProject.Builders;
using DeliveryProject.Pool;
using S1API.Internal.Abstraction;
using S1API.Saveables;
using ScheduleOne.Delivery;

namespace DeliveryProject.Persistence;

public class VehicleSave : Saveable
{
    [SaveableField("VehicleSaveData")] private List<VehicleSaveDto> _dtos = [];

    public static VehicleSave Instance { get; private set; } = new();

    public VehicleSave()
    {
        Instance = this;
    }

    protected override void OnLoaded()
    {
        for (var i = 0; i < _dtos.Count; ++i)
        {
            var guid = Guid.Parse(_dtos[i].Guid);
            var vehicle = new LandVehicleBuilder()
                .WithVehicleCode(_dtos[i].VehicleType)
                .WithVehicleName($"Additional Delivery Vehicle {i + 1}")
                .WithGuid(guid)
                .WithColor(_dtos[i].Color)
                .Build();
            var deliveryVehicle = new DeliveryVehicleBuilder()
                .WithLandVehicle(vehicle)
                .WithGuid(guid)
                .Build();
            PoolManager.Instance.AddToPool(deliveryVehicle);
        }
    }
}