using ScheduleOne.Delivery;
using ScheduleOne.Vehicles;
using UnityEngine;

namespace DeliveryProject.Builders;

public class DeliveryVehicleBuilder
{
    private Guid _guid;
    private LandVehicle _landVehicle;

    public DeliveryVehicleBuilder WithGuid(Guid guid)
    {
        _guid = guid;
        return this;
    }

    public DeliveryVehicleBuilder WithGuid(string guid)
    {
        _guid = Guid.Parse(guid);
        return this;
    }

    public DeliveryVehicleBuilder WithLandVehicle(LandVehicle landVehicle)
    {
        _landVehicle = landVehicle;
        return this;
    }

    public DeliveryVehicleBuilder WithLandVehicle(GameObject landVehicle)
    {
        _landVehicle = landVehicle.GetComponent<LandVehicle>();
        return this;
    }

    public DeliveryVehicle Build()
    {
        var deliveryVehicle = _landVehicle.gameObject.AddComponent<DeliveryVehicle>();
        deliveryVehicle.GUID = _guid.ToString();
        _landVehicle.SetGUID(_guid); // in case Awake runs quickly and resets the GUID
        return deliveryVehicle;
    }
}