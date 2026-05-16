using MultiDelivery.GameConsole.Core;
using MultiDelivery.Quest;

namespace MultiDelivery.GameConsole.Commands.Quest;

public class QuestStartCommand : ICommandNode
{
    public string Name => "start";

    public string Description =>
        "Starts a quest.";

    public void Execute(CommandContext context)
    {
        DropoffQuestDialogue.OnAcceptQuest();
        context.Reply("Quest started.");
    }
}