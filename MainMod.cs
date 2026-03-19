using System.Collections;
using MelonLoader;
using DeliveryProject.Helpers;
using DeliveryProject.Network;
using DeliveryProject.Persistence;
using DeliveryProject.Pool;
using DeliveryProject.Quest;
using ScheduleOne.Vehicles.Modification;
using Steamworks;
using UnityEngine;
#if MONO
using FishNet;

#else
using Il2CppFishNet;
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
    private static readonly Logger Logger = new("");
    private DeliveryNetworkManager? _networkManager;

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
        if (sceneName != "Menu") return;
        // Cleanup
        PoolManager.Instance.Pool.Clear();
        PoolManager.Instance.Allocations.Clear();
        PoolManager.Instance.BaseVehicleAllocationsForShop.Clear();
    }

    private IEnumerator InitializeNetworkManager()
    {
        yield return new WaitUntil(SteamAPI.Init);
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
            Logger.Warning("Network manager initialization failed - running in offline mode");
        }
    }

    public override void OnUpdate()
    {
        _networkManager?.Update();
        if (Input.GetKeyDown(KeyCode.F6))
        {
            var zone = VehicleDropoffZoneFactory.CreateZone(
                new Vector3(5.10f, 4.2f, 82.58f),
                new Vector3(0.55f, 4.2f, 76.56f)
            );
            Logger.Msg(zone);
        }
    }

    public override void OnDeinitializeMelon()
    {
        _networkManager?.Dispose();
        Logger.Msg("DeliveryProject deinitialized");
    }
}