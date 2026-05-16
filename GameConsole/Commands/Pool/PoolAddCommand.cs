using MultiDelivery.Builders;
using MultiDelivery.GameConsole.Core;
using MultiDelivery.Helpers;
using MultiDelivery.Pool;
using MultiDelivery.Quest;
using UnityEngine;
#if MONO
using ScheduleOne.Delivery;
using ScheduleOne.Vehicles;
#else
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.Vehicles;
#endif

namespace MultiDelivery.GameConsole.Commands.Pool;

public class PoolAddCommand : ICommandNode
{
    public string Name => "add";

    public string Description =>
        "Adds to the pool.";

    public void Execute(CommandContext context)
    {
        if (context.Args.Count == 0)
        {
            context.Error("Missing number.");
            return;
        }

        if (!int.TryParse(context.Args[0], out var amount) || amount < 1)
        {
            context.Error("Invalid number.");
            return;
        }
        for (var i = 0; i < amount; i++)
            CreatePoolVehicle();

        context.Reply($"Added {amount} to pool.");
    }

    internal static void CreatePoolVehicle()
    {
        var newLandVehicle = VehicleManager.Instance.SpawnAndReturnVehicle(QuestSetupManager.RequestedVehicleCode, Vector3.zero, Quaternion.identity, false);
        var deliveryVehicle = newLandVehicle.GetComponent<DeliveryVehicle>();
        if (deliveryVehicle == null)
        {
            var guid = newLandVehicle.GUID;
            deliveryVehicle = new DeliveryVehicleBuilder()
                .WithLandVehicle(newLandVehicle)
                .WithGuid(guid)
                .Build();
        }

        newLandVehicle.IsPlayerOwned = false;
        newLandVehicle.SetIsPlayerOwned(null, false);
        newLandVehicle.SetVisible(false);
        newLandVehicle.IsPhysicallySimulated = false;
        // try remove from player owned list
        var newVehicles = VehicleManager.Instance.PlayerOwnedVehicles.AsEnumerable()
            .Where(lv => lv.GUID.ToString() != newLandVehicle.GUID.ToString());
        VehicleManager.Instance.PlayerOwnedVehicles = newVehicles
#if MONO
            .ToList();
#else
            .ToIl2CppList();
#endif

        newLandVehicle.transform.position = new Vector3(0f, -100f, 0f);

        PoolManager.Instance.AddToSaveData(deliveryVehicle);
        PoolManager.Instance.AddToPool(deliveryVehicle);
    }
}