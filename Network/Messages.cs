using SteamNetworkLib.Models;

namespace DeliveryProject.Network;

/// <summary>
/// Message sent when a new vehicle is added to the pool
/// </summary>
public class VehicleAddedMessage : P2PMessage
{
    public override string MessageType => "VehicleAdded";

    public int ObjectId { get; set; }
    public string VehicleGuid { get; set; }
    public string VehicleName { get; set; }
    public string VehicleCode { get; set; }
    public int VehicleColor { get; set; }

    public override byte[] Serialize()
    {
        var json = CreateJsonBase(
            $"\"ObjectId\":{ObjectId},\"VehicleGuid\":\"{VehicleGuid}\",\"VehicleName\":\"{VehicleName}\",\"VehicleCode\":\"{VehicleCode}\",\"VehicleColor\":{VehicleColor}");
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    public override void Deserialize(byte[] data)
    {
        var json = System.Text.Encoding.UTF8.GetString(data);
        ParseJsonBase(json);
        ObjectId = int.Parse(ExtractJsonValue(json, "ObjectId"));
        VehicleGuid = ExtractJsonValue(json, "VehicleGuid");
        VehicleName = ExtractJsonValue(json, "VehicleName");
        VehicleCode = ExtractJsonValue(json, "VehicleCode");
        VehicleColor = int.Parse(ExtractJsonValue(json, "VehicleColor"));
    }
}

/// <summary>
/// Message sent when a vehicle is removed from the pool
/// </summary>
public class VehicleRemovedMessage : P2PMessage
{
    public override string MessageType => "VehicleRemoved";

    public int ObjectId { get; set; }
    public string VehicleGuid { get; set; }

    public override byte[] Serialize()
    {
        var json = CreateJsonBase($"\"ObjectId\":{ObjectId},\"VehicleGuid\":\"{VehicleGuid}\"");
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    public override void Deserialize(byte[] data)
    {
        var json = System.Text.Encoding.UTF8.GetString(data);
        ParseJsonBase(json);
        ObjectId = int.Parse(ExtractJsonValue(json, "ObjectId"));
        VehicleGuid = ExtractJsonValue(json, "VehicleGuid");
    }
}

/// <summary>
/// Message requesting full vehicle pool sync from host
/// </summary>
public class VehiclePoolSyncRequest : P2PMessage
{
    public override string MessageType => "VehiclePoolSyncRequest";

    public string RequesterId { get; set; }

    public override byte[] Serialize()
    {
        var json = CreateJsonBase($"\"RequesterId\":\"{RequesterId}\"");
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    public override void Deserialize(byte[] data)
    {
        var json = System.Text.Encoding.UTF8.GetString(data);
        ParseJsonBase(json);
        RequesterId = ExtractJsonValue(json, "RequesterId");
    }
}

/// <summary>
/// Message containing full pool state (sent by host)
/// </summary>
public class VehiclePoolSyncResponse : P2PMessage
{
    public override string MessageType => "VehiclePoolSyncResponse";

    public List<VehicleData> Vehicles { get; set; } = new();

    [Serializable]
    public class VehicleData
    {
        public int ObjectId { get; set; }
        public string Guid { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public int Color { get; set; }
    }

    public override byte[] Serialize()
    {
        var vehiclesJson = string.Join(",", Vehicles.Select(v =>
            $"{{\"ObjectId\":{v.ObjectId},\"Guid\":\"{v.Guid}\",\"Name\":\"{v.Name}\",\"Code\":\"{v.Code}\",\"Color\":{v.Color}}}"));
        var json = CreateJsonBase($"\"Vehicles\":[{vehiclesJson}]");
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    public override void Deserialize(byte[] data)
    {
        var json = System.Text.Encoding.UTF8.GetString(data);
        ParseJsonBase(json);

        // simple JSON array parsing
        var vehiclesStart = json.IndexOf("\"Vehicles\":[") + 12;
        var vehiclesEnd = json.IndexOf("]", vehiclesStart);
        var vehiclesSection = json.Substring(vehiclesStart, vehiclesEnd - vehiclesStart);

        Vehicles.Clear();
        if (string.IsNullOrWhiteSpace(vehiclesSection)) return;

        var vehicleObjects = vehiclesSection.Split(new[] { "}," }, StringSplitOptions.None);
        foreach (var vehicleJson in vehicleObjects)
        {
            var cleanJson = vehicleJson.TrimStart('{').TrimEnd('}') + "}";
            if (!cleanJson.Contains("ObjectId")) continue;

            Vehicles.Add(new VehicleData
            {
                ObjectId = int.Parse(ExtractJsonValue(cleanJson, "ObjectId")),
                Guid = ExtractJsonValue(cleanJson, "Guid"),
                Name = ExtractJsonValue(cleanJson, "Name"),
                Code = ExtractJsonValue(cleanJson, "Code"),
                Color = int.Parse(ExtractJsonValue(cleanJson, "Color"))
            });
        }
    }
}

/// <summary>
/// Message sent when a vehicle allocation changes
/// </summary>
public class VehicleAllocationMessage : P2PMessage
{
    public override string MessageType => "VehicleAllocation";

    public string DeliveryId { get; set; }
    public string VehicleGuid { get; set; }
    public bool IsAllocated { get; set; }

    public override byte[] Serialize()
    {
        var json = CreateJsonBase(
            $"\"DeliveryId\":\"{DeliveryId}\",\"VehicleGuid\":\"{VehicleGuid}\",\"IsAllocated\":{IsAllocated.ToString().ToLower()}");
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    public override void Deserialize(byte[] data)
    {
        var json = System.Text.Encoding.UTF8.GetString(data);
        ParseJsonBase(json);
        DeliveryId = ExtractJsonValue(json, "DeliveryId");
        VehicleGuid = ExtractJsonValue(json, "VehicleGuid");
        IsAllocated = bool.Parse(ExtractJsonValue(json, "IsAllocated"));
    }
}

/// <summary>
/// Message sent when base vehicle allocation changes for a shop
/// </summary>
public class BaseVehicleAllocationMessage : P2PMessage
{
    public override string MessageType => "BaseVehicleAllocation";

    public string ShopName { get; set; }
    public bool IsAllocated { get; set; }

    public override byte[] Serialize()
    {
        var json = CreateJsonBase($"\"ShopName\":\"{ShopName}\",\"IsAllocated\":{IsAllocated.ToString().ToLower()}");
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    public override void Deserialize(byte[] data)
    {
        var json = System.Text.Encoding.UTF8.GetString(data);
        ParseJsonBase(json);
        ShopName = ExtractJsonValue(json, "ShopName");
        IsAllocated = bool.Parse(ExtractJsonValue(json, "IsAllocated"));
    }
}