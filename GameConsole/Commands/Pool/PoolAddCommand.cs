using MultiDelivery.GameConsole.Core;

namespace MultiDelivery.GameConsole.Commands.Pool;

public class PoolAddCommand : ICommandNode
{
    public string Name => "add";

    public string Description =>
        "Adds to the pool.";

    public void Execute(CommandContext context)
    {
        throw new NotImplementedException();
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

        context.Reply($"Added {amount} to pool.");
    }
}