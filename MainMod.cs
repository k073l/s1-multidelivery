using System.Collections;
using MelonLoader;
using MultiDelivery.Helpers;
using MultiDelivery.Network;
using MultiDelivery.Pool;
using MultiDelivery.Quest;
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
[assembly: MelonColor(1, 0, 255, 0)]
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
    public const string Version = "1.0.7";
}

public class MultiDelivery : MelonMod
{
    private static Logger Logger;
    private IDeliveryNetworkManager? _networkManager;
    private bool _networkManagerFailed;

    internal static MelonPreferences_Category Category =
        MelonPreferences.CreateCategory($"{nameof(MultiDelivery)}Settings", $"{nameof(MultiDelivery)}'s Settings");

    internal static MelonPreferences_Entry<bool> NetworkLogging;
    internal static MelonPreferences_Entry<bool> PoolDebugLogging;
    internal static MelonPreferences_Entry<bool> QuestDebugLogging;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        Logger.Msg("MultiDelivery initialized");
        NetworkLogging = Category.CreateEntry("NetworkDebugLogs", false, "Enable Network Logs",
            "Display networking-related debug logs in MelonLoader console/log file (may be verbose)"
        );
        PoolDebugLogging = Category.CreateEntry("PoolDebugLogs", false, "Enable Pool Debug Logs",
            "Display pool-related debug logs in MelonLoader console/log file (may be verbose)"
        );
        QuestDebugLogging = Category.CreateEntry("QuestDebugLogs", false, "Enable Quest Debug Logs",
            "Display quest-related debug logs in MelonLoader console/log file (may be verbose)"
        );
        RegisterLoggerSettings();
        CheckDependencies();
        MelonCoroutines.Start(InitializeNetworkManager());
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        QuestSetupManager.OnSceneWasLoaded(buildIndex, sceneName);
        if (sceneName != "Menu") return;
        // Cleanup
        PoolManager.Instance.ClearAll();
    }

    private IEnumerator InitializeNetworkManager()
    {
        if (!SteamAPI.Init()) yield break;
        yield return null;
        
        _networkManager = NetworkLoader.Create();
        if (_networkManager == null)
        {
            Logger.Warning("NetworkManager is null, multiplayer features will be unavailable.");
            yield break;
        }
        NetworkConvenienceMethods.InitializeNetworking(_networkManager);
        _networkManager.RegisterLoggerSettings();
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

    private static void CheckDependencies()
    {
        var depChecker = new DependenciesChecker
        {
            ShowMenuBanner = true,
            UnloadIfMissingRequired = true,
        };
        depChecker.AddDependency(new DependencyInfo
        {
            Name = "S1API (Forked)",
            AssemblyName = "S1API",
            IsRequired = true,
            Version = "3.2.1",
            Urls =
            [
                new DependencyUrl
                {
                    SourceName = "Thunderstore",
                    Url = "https://thunderstore.io/c/schedule-i/p/ifBars/S1API_Forked/"
                },
                new DependencyUrl
                {
                    SourceName = "NexusMods",
                    Url = "https://www.nexusmods.com/schedule1/mods/1194"
                },
                new DependencyUrl
                {
                    SourceName = "Github",
                    Url = "https://github.com/ifBars/S1API/releases/"
                }
            ]
        });
        depChecker.AddDependency(new DependencyInfo
        {
            Name = "SteamNetworkLib",
            AssemblyName = "SteamNetworkLib",
            IsRequired = false,
            Version = "1.2.1",
            Urls =
            [
                new DependencyUrl
                {
                    SourceName = "Thunderstore Il2Cpp",
                    Url = "https://thunderstore.io/c/schedule-i/p/ifBars/SteamNetworkLib_Il2Cpp/"
                },
                new DependencyUrl
                {
                    SourceName = "Thunderstore Mono",
                    Url = "https://thunderstore.io/c/schedule-i/p/ifBars/SteamNetworkLib_Mono/"
                },
                new DependencyUrl
                {
                    SourceName = "NexusMods",
                    Url = "https://www.nexusmods.com/schedule1/mods/1396"
                },
                new DependencyUrl
                {
                    SourceName = "Github",
                    Url = "https://github.com/ifBars/SteamNetworkLib/releases"
                }
            ]
        });
        depChecker.ProcessAndAlert();
    }

    private static void RegisterLoggerSettings()
    {
        SetAndRegister(ActiveDeliveryPatch.Logger, PoolDebugLogging);
        SetAndRegister(CapacityText.Logger, PoolDebugLogging);
        SetAndRegister(DeliveryInstancePatches.Logger, PoolDebugLogging);
        SetAndRegister(LoaderPatch.Logger, PoolDebugLogging);
        SetAndRegister(PoolManager.Logger, PoolDebugLogging);
        
        SetAndRegister(DropoffQuest.Logger, QuestDebugLogging);
        SetAndRegister(DropoffQuestDialogue.Logger, QuestDebugLogging);
        SetAndRegister(QuestSetupManager.Logger, QuestDebugLogging);
        // VehicleDropoffZone logger registers itself
    }

    private static void SetAndRegister(Logger logger, MelonPreferences_Entry<bool>? entry)
    {
        logger.RaiseDebug = entry?.Value ?? false;
        entry?.OnEntryValueChanged.Subscribe((_, newValue) => logger.RaiseDebug = newValue);
    }
}