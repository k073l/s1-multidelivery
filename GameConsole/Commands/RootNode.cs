using MultiDelivery.GameConsole.Commands.Pool;
using MultiDelivery.GameConsole.Commands.Help;
using MultiDelivery.GameConsole.Commands.Quest;
using MultiDelivery.GameConsole.Core;

namespace MultiDelivery.GameConsole.Commands;

public class RootNode : CompositeCommand
{
    public override string Name => "root";

    public override string Description =>
        "Root command node";

    public RootNode()
    {
        Register(new HelpCommand(this));
        Register(new QuestCommand());
        Register(new PoolCommand());
    }
}