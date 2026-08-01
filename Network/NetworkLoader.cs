using System.Runtime.CompilerServices;

namespace MultiDelivery.Network;

internal static class NetworkLoader
{
    private static readonly Logger Logger = new("NetworkLoader");

    internal static IDeliveryNetworkManager? Create()
    {
        if (AppDomain.CurrentDomain.GetAssemblies()
            .All(a => a.GetName().Name != "SteamNetworkLib"))
            return null;

        return CreateInternal();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IDeliveryNetworkManager? CreateInternal()
    {
        try
        {
            var nm = new DeliveryNetworkManager();
            nm.Initialize();
            return nm;
        }
        catch (Exception e)
        {
            Logger.Warning(
                $"Failed to initialize network manager: {e}. If you're playing singleplayer, you may disregard this message.");
            return null;
        }
    }
}