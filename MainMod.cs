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
    public const string Version = "1.0.3";
}

public class MultiDelivery : MelonMod
{
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
        DependenciesChecker.PrintMissing();
        MelonCoroutines.Start(InitializeNetworkManager());
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        QuestSetupManager.OnSceneWasLoaded(buildIndex, sceneName);
        if (sceneName != "Menu") return;
        // Cleanup
        PoolManager.Instance.Pool.Clear();
        PoolManager.Instance.Allocations.Clear();
        PoolManager.Instance.BaseVehicleAllocationsForShop.Clear();
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
}