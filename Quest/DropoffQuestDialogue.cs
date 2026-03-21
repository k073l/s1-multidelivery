using MultiDelivery.Helpers;
using MultiDelivery.Pool;
using S1API.Dialogues;
using S1API.Entities.NPCs.Suburbia;
using S1API.Money;
using S1API.Quests;
using S1API.Quests.Constants;
using UnityEngine;
using NPC = S1API.Entities.NPC;
#if MONO
using ScheduleOne.Map;
using ScheduleOne.Money;
using ScheduleOne.Vehicles;
using ScheduleOne.NPCs.CharacterClasses;
#else
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.Vehicles;
using Il2CppScheduleOne.NPCs.CharacterClasses;

#endif

namespace MultiDelivery.Quest;

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
                $"Yeah! I can help with that. If you bring a {MultiDelivery.RequestedVehicleCode.Capitalize()} to the dropoff zone, I'll add it to the fleet. This will let you handle multiple deliveries at once. Want to give it a shot?",
                ch =>
                {
                    ch.Add("BUY_NOW", "Sure! Can I buy one from you right now?", "CHECK_FUNDS");
                    ch.Add("ACCEPT", "I already have one, let's do it!", "ACCEPTED");
                    ch.Add("DECLINE", "Maybe later.", "DECLINED");
                });

            // Repeat offer (pool has vehicles)
            c.AddNode("OFFER_REPEAT",
                "Want to expand your fleet even more? Just bring another vehicle to the dropoff zone and I'll add it for you.",
                ch =>
                {
                    ch.Add("BUY_NOW", "Yeah, can I buy another one from you?", "CHECK_FUNDS");
                    ch.Add("ACCEPT", "I've got one ready!", "ACCEPTED");
                    ch.Add("DECLINE", "Not right now.", "DECLINED");
                });

            c.AddNode("CHECK_FUNDS",
                $"A {MultiDelivery.RequestedVehicleCode.Capitalize()} runs <color=#19BEF0>({MoneyManager.FormatAmount(GetVehiclePrice())})</color>. Want to pick one up?",
                ch =>
                {
                    ch.Add("PURCHASE",
                        $"I'll take it. <color=#19BEF0>({MoneyManager.FormatAmount(GetVehiclePrice())})</color>",
                        "PURCHASE_COMPLETE");
                    ch.Add("NEVERMIND", "Let me think about it.", "DECLINED");
                });

            c.AddNode("PURCHASE_COMPLETE",
                "All yours. You can customize it if you want. Now just drive it to the dropoff zone - I've marked it on your map.");
            c.AddNode("NOT_ENOUGH",
                "You don't have enough money to buy it. Come back later.");

            c.AddNode("IN_PROGRESS",
                "You already have an active delivery expansion going! Just bring the vehicle to the dropoff zone. Top floor of the parking garage, next to the storage units - check your map if you forgot where it is.");

            c.AddNode("ACCEPTED",
                "Great! I've marked the dropoff zone on your map - top floor of the parking garage, next to the storage units. Just drive the vehicle in there when you're ready.");

            c.AddNode("DECLINED",
                "No worries, just let me know if you change your mind!");
        });

        jeremy.Dialogue.OnChoiceSelected("ACCEPT", OnAcceptQuest);
        jeremy.Dialogue.OnChoiceSelected("BUY_NOW", OnAcceptQuest);
        jeremy.Dialogue.OnChoiceSelected("DECLINE", () => Logger.Debug("Player declined quest"));
        jeremy.Dialogue.OnChoiceSelected("NEVERMIND", () => Logger.Debug("Player cancelled purchase"));

        jeremy.Dialogue.OnChoiceSelected("PURCHASE", () =>
        {
            var vehiclePrice = GetVehiclePrice();
            var playerCash = Money.GetOnlineBalance();

            if (playerCash >= vehiclePrice)
            {
                Money.CreateOnlineTransaction($"{MultiDelivery.RequestedVehicleCode.Capitalize()} purchase",
                    -vehiclePrice, 1f, "Bought as a part of delivery expansion");
                Logger.Msg($"Player purchased {MultiDelivery.RequestedVehicleCode} for ${vehiclePrice:F2}");
                var s1Jeremy = jeremy.gameObject.GetComponent<Jeremy>();
                if (s1Jeremy != null)
                {
                    var dealership = s1Jeremy.Dealership;
                    if (dealership != null)
                    {
                        dealership.SpawnVehicle(MultiDelivery.RequestedVehicleCode);
                        return;
                    }
                }

                VehicleManager.Instance.SpawnVehicle(MultiDelivery.RequestedVehicleCode,
                    new Vector3(9.92f, 0.54f, -33.55f), Quaternion.identity, playerOwned: true);
            }
            else
            {
                jeremy.Dialogue.JumpTo(ContainerName, "NOT_ENOUGH");
            }
        });

        // Main injection
        DialogueInjector.Register(new DialogueInjection(
            npc: jeremy.ID,
            container: "Dealership_Salesman_Sell",
            from: "cc0d838e-2824-4fd5-907d-798dc0195c16",
            to: "OFFER_FIRST",
            label: "ASK_EXPANSION",
            text: "Can I expand my delivery capacity?",
            onConfirmed: () =>
            {
                var quest = QuestManager.GetQuestByName(DropoffQuest.Name) as DropoffQuest;
                var hasExpandedBefore = PoolManager.Instance.Pool.Count > 0;
                var questActive = quest is { State: QuestState.Active };

                Logger.Debug($"Quest state - Active: {questActive}, Has expanded: {hasExpandedBefore}");

                var targetNode = questActive ? "IN_PROGRESS" :
                    hasExpandedBefore ? "OFFER_REPEAT" : "OFFER_FIRST";

                Logger.Debug($"Jumping to '{ContainerName}', node '{targetNode}'");
                jeremy.Dialogue.JumpTo(ContainerName, targetNode);
            }
        ));

        Logger.Msg("Registered delivery expansion dialogue");
    }

    private static void OnAcceptQuest()
    {
        var quest = QuestManager.GetQuestByName(DropoffQuest.Name) as DropoffQuest;

        if (quest == null)
        {
            quest = QuestManager.CreateQuest<DropoffQuest>() as DropoffQuest;
            quest?.Begin();
            Logger.Msg("Started delivery expansion quest");
        }
        else if (quest?.State == QuestState.Completed)
        {
            quest.Begin();
            Logger.Msg("Restarted delivery expansion quest");
        }
    }

    private static float GetVehiclePrice()
    {
        var vehicleType = VehicleManager.Instance.GetVehiclePrefab(MultiDelivery.RequestedVehicleCode);
        return vehicleType?.VehiclePrice ?? 5000f;
    }
}