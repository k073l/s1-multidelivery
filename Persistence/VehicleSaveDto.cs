using ScheduleOne.Vehicles.Modification;
using UnityEngine;

namespace DeliveryProject.Persistence;

public record VehicleSaveDto
{
    public string Guid { get; set; }
    public string VehicleType { get; set; }
    public EVehicleColor Color { get; set; }
}