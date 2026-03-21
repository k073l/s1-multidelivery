#if MONO
using FishNet;
using ScheduleOne.Vehicles.Modification;
using ScheduleOne.Vehicles;
#else
using Il2Cpp;
using Il2CppFishNet;
using Il2CppScheduleOne.Vehicles.Modification;
using Il2CppScheduleOne.Vehicles;
using Guid = Il2CppSystem.Guid;
#endif
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MultiDelivery.Builders;

public class LandVehicleBuilder
{
    private string _vehicleName = "CustomVehicle";
    private string _vehicleCode = "veeper";
    private EVehicleColor _color = EVehicleColor.Custom;
    private Guid _guid = GUIDManager.GenerateUniqueGUID();
    private Transform _parent;

    public LandVehicleBuilder WithVehicleName(string vehicleName)
    {
        _vehicleName = vehicleName;
        return this;
    }

    public LandVehicleBuilder WithVehicleCode(string vehicleCode)
    {
        _vehicleCode = vehicleCode;
        return this;
    }

    public LandVehicleBuilder WithColor(EVehicleColor color)
    {
        _color = color;
        return this;
    }

    public LandVehicleBuilder WithGuid(Guid guid)
    {
        _guid = guid;
        return this;
    }

    public LandVehicleBuilder WithGuid(string guid)
    {
        _guid = new Guid(guid);
        return this;
    }

    public LandVehicle Build()
    {
        if (_parent == null)
        {
            var rootGo = new GameObject("VehiclePool");
            _parent = rootGo.transform;
        }

        var position = new Vector3(0f, -100f, 0);
        var rotation = Quaternion.identity;

        if (!InstanceFinder.IsServer)
            throw new ArgumentException("LandVehicleBuilder can only be used on the server");

        var prefab = VehicleManager.Instance.GetVehiclePrefab(_vehicleCode);
        if (prefab == null)
            throw new ArgumentException($"Vehicle prefab with code '{_vehicleCode}' not found.");

        var go = Object.Instantiate(prefab.gameObject, _parent, true);
        var component = go.GetComponent<LandVehicle>();

        component.IsPlayerOwned = false;
        component.SetVisible(false);
        component.IsPhysicallySimulated = false;

        component.transform.position = position;
        component.transform.rotation = rotation;

        component.SetGUID(_guid);
        component.name = _vehicleName;
        component.gameObject.name = _vehicleName;
        component.vehicleName = _vehicleName;
        component.SetIsPlayerOwned(null, false);
        component.Rb.isKinematic = false;
        component.Owner.ClientId = -1;

        component.ApplyColor(_color);

        VehicleManager.Instance.AllVehicles.Add(component);
        VehicleManager.Instance.NetworkObject.Spawn(component.gameObject, null, default(Scene));

        // later we need to notify network consumers

        return component;
    }
}