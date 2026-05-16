using MultiDelivery.GameConsole.Core;

namespace MultiDelivery.GameConsole.Commands.Pool;

public class PoolSetCommand : ICommandNode
{
    public string Name => "set";

    public string Description =>
        "Sets the pool value.";

    public void Execute(CommandContext context)
    {
        throw new NotImplementedException();
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

        context.Reply($"Pool set to {amount}.");
    }
}