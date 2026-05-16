using MultiDelivery.GameConsole.Core;
using MultiDelivery.Persistence;
using MultiDelivery.Pool;

namespace MultiDelivery.GameConsole.Commands.Pool;

public class PoolSetCommand : ICommandNode
{
    public string Name => "set";

    public string Description =>
        "Sets the pool value. Downsizing the pool is not networked.";

    public void Execute(CommandContext context)
    {
        if (context.Args.Count == 0)
        {
            context.Error("Missing number.");
            return;
        }

        if (!int.TryParse(context.Args[0], out var amount) || amount < 0)
        {
            context.Error("Invalid number.");
            return;
        }

        var todo = amount - PoolManager.Instance.Pool.Count;
        if (todo > 0)
        {
            for (var i = 0; i < todo; i++)
                PoolAddCommand.CreatePoolVehicle();
        }
        else
        {
            for (var i = 0; i < -todo; i++)
            {
                // remove last vehicle
                var vehicle = PoolManager.Instance.Pool.LastOrDefault();
                if (vehicle == null)
                {
                    context.Error($"Last pool vehicle not found. Downsizing the pool unavailable");
                    return;
                }

                PoolManager.Instance.Pool.Remove(vehicle);
                // free alloc
                var keys = PoolManager.Instance.Allocations
                    .Where(x => x.Value == vehicle)
                    .Select(x => x.Key)
                    .ToList();
                foreach (var key in keys)
                    PoolManager.Instance.Allocations.Remove(key);
                VehicleSave.Instance.RemoveVehicle(vehicle);
            }
        }

        context.Reply($"Pool set to {amount}.");
    }
}