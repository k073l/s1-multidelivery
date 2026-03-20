using DeliveryProject.Helpers;
using DeliveryProject.Pool;
using S1API.Dialogues;
using S1API.Entities.NPCs.Suburbia;
using S1API.Quests;
using S1API.Quests.Constants;
using NPC = S1API.Entities.NPC;

namespace DeliveryProject.Quest;

public class DropoffQuestDialogue
{
    private static readonly Logger Logger = new(nameof(DropoffQuestDialogue));
    private const string ContainerName = "DeliveryExpansion";

    public static void Register()
    {
        var jeremy = NPC.Get<JeremyWilkinson>();
        if (jeremy == null)
        {
            Logger.Warning("Jeremy Wilkinson not found");
            return;
        }

        jeremy.Dialogue.BuildAndRegisterContainer(ContainerName, c =>
        {
            // First time offer (pool empty)
            c.AddNode("OFFER_FIRST",
                $"Yeah! I can help with that. If you bring a {DeliveryProject.RequestedVehicleCode.Capitalize()} to the dropoff zone, I'll add it to the fleet. This will let you handle multiple deliveries at once. Want to give it a shot?",
                ch =>
                {
                    ch.Add("ACCEPT", "Sure, let's do it!", "ACCEPTED");
                    ch.Add("DECLINE", "Maybe later.", "DECLINED");
                });

            // Repeat offer (pool has vehicles)
            c.AddNode("OFFER_REPEAT",
                "Want to expand your fleet even more? Just bring another vehicle to the dropoff zone and I'll add it for you.",
                ch =>
                {
                    ch.Add("ACCEPT", "Yeah, let's add another!", "ACCEPTED");
                    ch.Add("DECLINE", "Not right now.", "DECLINED");
                });

            c.AddNode("IN_PROGRESS",
                "You already have an active delivery expansion going! Just bring the vehicle to the dropoff zone - check your map if you forgot where it is.");

            c.AddNode("ACCEPTED",
                "Great! I've marked the dropoff zone on your map - top floor of the parking garage, next to the storage units. Just drive the vehicle in there when you're ready.");

            c.AddNode("DECLINED",
                "No worries, just let me know if you change your mind!");
        });

        jeremy.Dialogue.OnChoiceSelected("ACCEPT", () =>
        {
            var quest = QuestManager.GetQuestByName(DropoffQuest.Name) as DropoffQuest;

            if (quest == null)
            {
                quest = QuestManager.CreateQuest<DropoffQuest>() as DropoffQuest;
                quest.Begin();
                Logger.Msg("Started delivery expansion quest");
            }
            else if (quest.State == QuestState.Completed)
            {
                quest.Begin();
                Logger.Msg("Restarted delivery expansion quest");
            }
        });

        jeremy.Dialogue.OnChoiceSelected("DECLINE", () => { Logger.Debug("Player declined quest"); });

        DialogueInjector.Register(new DialogueInjection(
            npc: jeremy.ID,
            container: "Dealership_Salesman_Sell",
            from: "cc0d838e-2824-4fd5-907d-798dc0195c16",
            to: "OFFER_FIRST", // Default target, will be overridden
            label: "ASK_EXPANSION",
            text: "Can I expand my delivery capacity?",
            onConfirmed: () =>
            {
                var quest = QuestManager.GetQuestByName(DropoffQuest.Name) as DropoffQuest;
                var hasExpandedBefore = PoolManager.Instance.Pool.Count > 0;
                var questActive = quest is { State: QuestState.Active };

                Logger.Debug($"Quest state - Active: {questActive}, Has expanded: {hasExpandedBefore}");

                string targetNode;
                if (questActive)
                {
                    targetNode = "IN_PROGRESS";
                }
                else if (hasExpandedBefore)
                {
                    targetNode = "OFFER_REPEAT";
                }
                else
                {
                    targetNode = "OFFER_FIRST";
                }

                Logger.Debug($"Attempting to jump to container '{ContainerName}', node '{targetNode}'");

                try
                {
                    jeremy.Dialogue.JumpTo(ContainerName, targetNode);
                    Logger.Debug("Jump successful");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Jump failed: {ex}");
                }
            }
        ));

        Logger.Msg("Registered delivery expansion dialogue");
    }
}