using MultiDelivery.GameConsole.Core;
using MultiDelivery.Network;
using MultiDelivery.Persistence;
using MultiDelivery.Quest;
using S1API.Entities;
using S1API.Entities.NPCs.Suburbia;

namespace MultiDelivery.GameConsole.Commands.Quest;

public class QuestForceCommand : ICommandNode
{
    public string Name => "force";

    public string Description =>
        "Forces the quest to register regardless of Rank";

    public void Execute(CommandContext context)
    {
        var jeremy = NPC.Get<JeremyWilkinson>();
        if (jeremy == null) return;

        if (NetworkConvenienceMethods.HostOrSingleplayer)
            jeremy.SendTextMessage(QuestSetupManager.QuestStartMessage);
        PersistentDropoffQuestData.Instance.HasMessaged = true;

        DropoffQuestDialogue.Register();
        context.Reply("Quest force executed.");
    }
}