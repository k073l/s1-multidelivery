using System.Collections;
using MelonLoader;
using DeliveryProject.Helpers;
using DeliveryProject.Network;
using DeliveryProject.Persistence;
using DeliveryProject.Pool;
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
    private DeliveryNetworkManager _networkManager;
 
    public override void OnInitializeMelon()
    {
        Melon<DeliveryProject>.Logger.Msg("DeliveryProject initialized");
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
            Melon<DeliveryProject>.Logger.Msg("Network manager initialized");
                
            // Wire up to the pool manager
            PoolManager.Instance.InitializeNetworking(_networkManager);
        }
        else
        {
            Melon<DeliveryProject>.Logger.Warning("Network manager initialization failed - running in offline mode");
        }
    }

    public override void OnUpdate()
    {
        _networkManager?.Update();
    }
 
    public override void OnDeinitializeMelon()
    {
        _networkManager?.Dispose();
        Melon<DeliveryProject>.Logger.Msg("DeliveryProject shut down");
    }
}