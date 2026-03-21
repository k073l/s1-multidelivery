using System.Collections;
using System.Reflection;
using MelonLoader;
using MultiDelivery.Helpers;
using MultiDelivery.Network;
using MultiDelivery.Persistence;
using MultiDelivery.Pool;
using MultiDelivery.Quest;
using S1API.Entities;
using S1API.Entities.NPCs.Suburbia;
using S1API.Leveling;
using S1API.Quests;
using S1API.Utils;
using UnityEngine;
using static MultiDelivery.Quest.DropoffQuestDialogue;
#if MONO
using Steamworks;

#else
using Il2CppSteamworks;
#endif

[assembly: MelonInfo(
    typeof(MultiDelivery.MultiDelivery),
    MultiDelivery.BuildInfo.Name,
    MultiDelivery.BuildInfo.Version,
    MultiDelivery.BuildInfo.Author
)]
[assembly: MelonColor(1, 255, 0, 0)]
[assembly: MelonGame("TVGS", "Schedule I")]

// Specify platform domain based on build target (remove this if your mod supports both via S1API)
#if MONO
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
#else
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
#endif

namespace MultiDelivery;

public static class BuildInfo
{
    public const string Name = "MultiDelivery";
    public const string Description = "Add vehicles, order multiple deliveries from the same place!";
    public const string Author = "k073l";
    public const string Version = "1.0.0";
}

public class MultiDelivery : MelonMod
{
    internal const string RequestedVehicleCode = "veeper";

    private static FullRank RequiredRank = new(Rank.Enforcer, 1);
    public static Sprite QuestIconSprite => GetIcon(ref _questIconSprite, $"{nameof(MultiDelivery)}.assets.quest_icon.png");

    private static Sprite _questIconSprite;
    private static readonly Logger Logger = new("");
    private DeliveryNetworkManager? _networkManager;
    private bool _networkManagerFailed;

    internal static MelonPreferences_Category Category =
        MelonPreferences.CreateCategory($"{nameof(MultiDelivery)}Settings", $"{nameof(MultiDelivery)}'s Settings");

    internal static MelonPreferences_Entry<bool> NetworkLogging =
        Category.CreateEntry("NetworkDebugLogs", false, "Enable Network Logs",
            "Display networking-related debug logs in MelonLoader console/log file (may be verbose)"
        );

    public override void OnInitializeMelon()
    {
        Logger.Msg("MultiDelivery initialized");
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
                $"Network manager update failed: {ex.Message}\n\n" +
                $"You can ignore this error if you plan on playing singleplayer only and don't want to install SteamNetworkLib");
            _networkManagerFailed = true; // give up
        }
    }

    public override void OnDeinitializeMelon()
    {
        _networkManager?.Dispose();
        Logger.Msg("MultiDelivery deinitialized");
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

        if (NetworkConvenienceMethods.HostOrSingleplayer)
            jeremy.SendTextMessage(
                "Your properties are getting busy. Want to handle more than one delivery at a time? I've got an idea. Stop by the dealership."
            );
        PersistentDropoffQuestData.Instance.HasMessaged = true;

        DropoffQuestDialogue.Register();
        LevelManager.OnXPChanged -= SendMessageIfRequiredRank;
    }
    
    private static Sprite LoadEmbeddedPNG(string resourceName)
    {
        var asm = Assembly.GetExecutingAssembly();

        using var stream = asm.GetManifestResourceStream(resourceName);
        if (stream == null) return null;

        var data = new byte[stream.Length];
        stream.Read(data, 0, data.Length);
        var sprite = ImageUtils.LoadImageRaw(data);
        if (sprite != null) sprite.name = resourceName;
        return sprite;
    }

    private static Sprite GetIcon(ref Sprite spriteField, string resourceName)
    {
        if (spriteField == null)
        {
            spriteField = LoadEmbeddedPNG(resourceName);
        }

        return spriteField;
    }
}