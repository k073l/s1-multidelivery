using MultiDelivery.GameConsole.Core;

namespace MultiDelivery.GameConsole.Commands.Pool;

public class PoolCommand : CompositeCommand
{
    public override string Name => "pool";

    public override string Description =>
        "Pool management.";

    public PoolCommand()
    {
        Register(new PoolAddCommand());
        Register(new PoolSetCommand());
        Register(new PoolGetCommand());
    }
}