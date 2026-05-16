namespace MultiDelivery.GameConsole.Core;

public interface ICommandNode
{
    string Name { get; }
    string Description { get; }

    void Execute(CommandContext context);
}