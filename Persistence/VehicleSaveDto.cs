#if MONO
using ScheduleOne.Vehicles.Modification;
#else
using Il2CppScheduleOne.Vehicles.Modification;
#endif

namespace DeliveryProject.Persistence;

public record VehicleSaveDto
{
    public string Guid { get; set; }
    public string VehicleType { get; set; }
    public EVehicleColor Color { get; set; }
}