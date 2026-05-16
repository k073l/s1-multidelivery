using MultiDelivery.GameConsole.Core;

namespace MultiDelivery.GameConsole.Commands.Help;

public class HelpCommand : ICommandNode
{
    private readonly CompositeCommand _root;

    public HelpCommand(CompositeCommand root)
    {
        _root = root;
    }

    public string Name => "help";

    public string Description =>
        "Displays available commands.";

    public void Execute(CommandContext context)
    {
        foreach (var cmd in _root.GetChildren())
        {
            context.Reply($"{cmd.Name} - {cmd.Description}");
        }
    }
}