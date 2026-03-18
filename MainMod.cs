using System.Collections;
using MelonLoader;
using DeliveryProject.Helpers;
using DeliveryProject.Persistence;
using ScheduleOne.Vehicles.Modification;
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
    private static MelonLogger.Instance Logger;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        Logger.Msg("DeliveryProject initialized");
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        Logger.Debug($"Scene loaded: {sceneName}");
        if (sceneName == "Main")
        {
            Logger.Debug("Main scene loaded, waiting for player");
            MelonCoroutines.Start(Utils.WaitForPlayer(DoStuff()));
        }
    }

    private IEnumerator DoStuff()
    {
        Logger.Msg("Player ready, doing stuff...");
        yield return new WaitForSeconds(2f);
        Logger.Msg("Did some stuff!");
    }
}