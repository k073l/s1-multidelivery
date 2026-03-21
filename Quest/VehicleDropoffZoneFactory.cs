using UnityEngine;

namespace MultiDelivery.Quest;

public static class VehicleDropoffZoneFactory
{
    public static VehicleDropoffZone CreateZone(
        Vector3 corner1,
        Vector3 corner2,
        float height = 5f,
        Color? visualColor = null,
        bool showVisuals = true,
        DropoffQuest? attachedQuest = null)
    {
        var zoneObject = new GameObject("VehicleDropoffZone");
        var zone = zoneObject.AddComponent<VehicleDropoffZone>();
        zone.ShowVisuals = showVisuals;
        zone.SetupZone(corner1, corner2, height, visualColor);
        if (attachedQuest != null) zone.SetQuest(attachedQuest);

        return zone;
    }

    public static VehicleDropoffZone CreateZoneNear(
        Transform reference,
        Vector3 offset,
        Vector3 size,
        Color? visualColor = null)
    {
        var center = reference.position + offset;
        var corner1 = center - size / 2f;
        var corner2 = center + size / 2f;

        return CreateZone(corner1, corner2, size.y, visualColor);
    }
}