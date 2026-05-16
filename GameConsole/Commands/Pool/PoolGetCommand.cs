using MultiDelivery.GameConsole.Core;
using MultiDelivery.Pool;

namespace MultiDelivery.GameConsole.Commands.Pool;

public class PoolGetCommand: ICommandNode
{
    public string Name => "get";
    public string Description => "Gets the number of vehicles in the pool.";
    public void Execute(CommandContext context)
    {
        var poolCount = PoolManager.Instance.Pool.Count;
        var allocations = PoolManager.Instance.Allocations.Count;
        context.Reply($"Pool has {poolCount} vehicles in it, {allocations} of which are allocated.");
    }
}