using System.Collections;
using MelonLoader;
using DeliveryProject.Helpers;
using DeliveryProject.Network;
using DeliveryProject.Persistence;
using DeliveryProject.Pool;
using DeliveryProject.Quest;
using S1API.Entities;
using S1API.Entities.NPCs.Suburbia;
using S1API.Leveling;
using S1API.Quests;
using UnityEngine;
using static DeliveryProject.Quest.DropoffQuestDialogue;
#if MONO
using Steamworks;

#else
using Il2CppSteamworks;
#endif

[assembly: MelonInfo(
    typeof(DeliveryProject.DeliveryProject),
    DeliveryProject.BuildInfo.Name,
    DeliveryProject.BuildInfo.Version,
    DeliveryProject.BuildInfo.Author
)]
[assembly: MelonColor(1, 255, 0, 0)]
[assembly: MelonGame("TVGS", "Schedule I")]

// Specify platform domain based on build target (remove this if your mod supports both via S1API)
#if MONO
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
#else
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
#endif

namespace DeliveryProject;

public static class BuildInfo
{
    public const string Name = "DeliveryProject";
    public const string Description = "does stuff i guess";
    public const string Author = "me";
    public const string Version = "1.0.0";
}

public class DeliveryProject : MelonMod
{
    internal const string RequestedVehicleCode = "veeper";

    private static FullRank RequiredRank = new(Rank.Enforcer, 1);

    private static readonly Logger Logger = new("");
    private DeliveryNetworkManager? _networkManager;
    private bool _networkManagerFailed;

    internal static MelonPreferences_Category Category =
        MelonPreferences.CreateCategory($"{nameof(DeliveryProject)}Settings", $"{nameof(DeliveryProject)}'s Settings");

    internal static MelonPreferences_Entry<bool> NetworkLogging =
        Category.CreateEntry("NetworkDebugLogs", false, "Enable Network Logs",
            "Display networking-related debug logs in MelonLoader console/log file (may be verbose)"
        );

    public override void OnInitializeMelon()
    {
        Logger.Msg("DeliveryProject initialized");
        MelonCoroutines.Start(InitializeNetworkManager());
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName == "Main")
        {
            Player.LocalPlayerSpawned += WirePlayerEvent;
            return;
        }

        if (sceneName != "Menu") return;
        // Cleanup
        PoolManager.Instance.Pool.Clear();
        PoolManager.Instance.Allocations.Clear();
        PoolManager.Instance.BaseVehicleAllocationsForShop.Clear();
        Player.LocalPlayerSpawned -= WirePlayerEvent;
        LevelManager.OnXPChanged -= SendMessageIfRequiredRank;
    }

    private IEnumerator InitializeNetworkManager()
    {
        if (!SteamAPI.Init()) yield break;
        yield return null;

        // Initialize network manager
        _networkManager = new DeliveryNetworkManager();
        if (_networkManager.Initialize())
        {
            Logger.Msg("Network manager initialized");

            // Wire up to the pool manager
            NetworkConvenienceMethods.InitializeNetworking(_networkManager);
        }
        else
        {
            _networkManager = null;
            Logger.Warning("Network manager initialization failed - running in offline mode");
        }
    }

    public override void OnUpdate()
    {
        if (_networkManagerFailed || _networkManager == null) return;
        try
        {
            _networkManager.Update();
        }
        catch (Exception ex)
        {
            MelonLogger.Error(
                $"Network manager update failed: {ex.Message}\n" +
                $"You can ignore this error if you plan on playing singleplayer only and don't want to install SteamNetworkLib");
            _networkManagerFailed = true; // give up
        }
    }

    public override void OnDeinitializeMelon()
    {
        _networkManager?.Dispose();
        Logger.Msg("DeliveryProject deinitialized");
    }

    private void WirePlayerEvent(Player _)
    {
        Logger.Debug("Player loaded event called");
        if (PersistentDropoffQuestData.Instance.HasMessaged)
        {
            DropoffQuestDialogue.Register();
            return;
        }

        MelonCoroutines.Start(WireOnXpChangedDelayed());
    }

    private IEnumerator WireOnXpChangedDelayed()
    {
        yield return new WaitUntil((Func<bool>)(() => LevelManager.Exists));
        Logger.Debug("Wiring on xp changed");
        LevelManager.OnXPChanged += SendMessageIfRequiredRank;
    }

    private void SendMessageIfRequiredRank(FullRank _, FullRank current)
    {
        Logger.Debug($"Current rank {current}, required: {RequiredRank}");

        if (PersistentDropoffQuestData.Instance.HasMessaged) return;
        if (current < RequiredRank) return;

        var jeremy = NPC.Get<JeremyWilkinson>();
        if (jeremy == null) return;

        jeremy.SendTextMessage(
            "Your properties are getting busy. Want to handle more than one delivery at a time? I've got an idea. Stop by the dealership."
        );
        PersistentDropoffQuestData.Instance.HasMessaged = true;

        DropoffQuestDialogue.Register();
        LevelManager.OnXPChanged -= SendMessageIfRequiredRank;
    }
}