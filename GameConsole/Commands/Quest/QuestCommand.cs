using MultiDelivery.GameConsole.Core;

namespace MultiDelivery.GameConsole.Commands.Quest;

public class QuestCommand : CompositeCommand
{
    public override string Name => "quest";

    public override string Description =>
        "Quest-related commands.";

    public QuestCommand()
    {
        Register(new QuestForceCommand());
        Register(new QuestStartCommand());
    }
}