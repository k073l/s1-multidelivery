using MultiDelivery.GameConsole.Core;
using MultiDelivery.Pool;

namespace MultiDelivery.GameConsole.Commands.Pool;

public class PoolGetCommand: ICommandNode
{
    public string Name => "get";
    public string Description => "Gets the number of vehicles in the pool.";
    public void Execute(CommandContext context)
    {
        var poolCount = PoolManager.Instance.PoolCount;
        var allocations = PoolManager.Instance.AllocationCount;
        context.Reply($"Pool has {poolCount} vehicles in it, {allocations} of which are allocated.");
    }
}