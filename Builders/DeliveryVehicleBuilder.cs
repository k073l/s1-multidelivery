#if MONO
using ScheduleOne.Delivery;
using ScheduleOne.Vehicles;
using Guid = System.Guid;
#else
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.Vehicles;
using Guid = Il2CppSystem.Guid;
#endif
using UnityEngine;

namespace MultiDelivery.Builders;

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
        _guid = new Guid(guid);
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