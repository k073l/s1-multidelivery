using System.Collections;
using MelonLoader;
using MultiDelivery.Network;
using MultiDelivery.Persistence;
using S1API.Entities;
using S1API.Entities.NPCs.Suburbia;
using S1API.Leveling;
using UnityEngine;

namespace MultiDelivery.Quest;

public class QuestSetupManager
{
    internal const string RequestedVehicleCode = "veeper";
    internal const string QuestStartMessage =
        "Your properties are getting busy. Want to handle more than one delivery at a time? I've got an idea. Stop by the dealership.";
    private static FullRank RequiredRank = new(Rank.Enforcer, 1);
    private static readonly Logger Logger = new(nameof(QuestSetupManager));
    
    public static void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        switch (sceneName)
        {
            case "Main":
                Player.LocalPlayerSpawned += WirePlayerEvent;
                return;
            case "Menu":
                Player.LocalPlayerSpawned -= WirePlayerEvent;
                LevelManager.OnXPChanged -= SendMessageIfRequiredRank;
                break;
        }
    }
    
    private static void WirePlayerEvent(Player _)
    {
        Logger.Debug("Player loaded event called");
        MelonCoroutines.Start(WireOnXpChangedDelayed());
    }

    private static IEnumerator WireOnXpChangedDelayed()
    {
        yield return new WaitUntil((Func<bool>)(() => LevelManager.Exists));
        // Checking after LevelManager exist to delay it enough so saveable exists
        if (PersistentDropoffQuestData.Instance.HasMessaged)
        {
            DropoffQuestDialogue.Register();
            yield break;
        }
        Logger.Debug("Wiring on xp changed");
        LevelManager.OnXPChanged += SendMessageIfRequiredRank;
    }

    private static void SendMessageIfRequiredRank(FullRank _, FullRank current)
    {
        Logger.Debug($"Current rank {current}, required: {RequiredRank}");

        if (PersistentDropoffQuestData.Instance.HasMessaged)
        {
            MelonDebug.Msg("Xp changed wired, but already messaged - registering dialogue now.");
            DropoffQuestDialogue.Register();
            return;
        }
        if (current < RequiredRank) return;

        var jeremy = NPC.Get<JeremyWilkinson>();
        if (jeremy == null) return;

        if (NetworkConvenienceMethods.HostOrSingleplayer)
            jeremy.SendTextMessage(QuestStartMessage);
        PersistentDropoffQuestData.Instance.HasMessaged = true;

        DropoffQuestDialogue.Register();
        LevelManager.OnXPChanged -= SendMessageIfRequiredRank;
    }

}