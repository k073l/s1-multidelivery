using System.Collections;
using MelonLoader;
using SteamNetworkLib;
using MultiDelivery.Builders;
using MultiDelivery.Persistence;
using MultiDelivery.Pool;
using UnityEngine;

#if MONO
using Steamworks;
using ScheduleOne.Delivery;
using ScheduleOne.Vehicles;
using ScheduleOne.Vehicles.Modification;

#else
using Il2CppSteamworks;
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.Vehicles;
using Il2CppScheduleOne.Vehicles.Modification;
using Guid = Il2CppSystem.Guid;
#endif

namespace MultiDelivery.Network;

/// <summary>
/// Manages network synchronization for the delivery vehicle pool system
/// Uses ObjectId (FishNet network ID) for vehicle lookups across clients
/// </summary>
public class DeliveryNetworkManager
{
    private SteamNetworkClient _client;
    private readonly Logger _logger;

    private const string ModDataKey = "MultiDelivery_Version";
    private const string ModVersion = "1.0.0";

    private bool IsInLobby => _client?.IsInLobby ?? false;
    private bool IsHost => _client?.IsHost ?? false;
    private bool IsSingleplayer => !IsInLobby;

    public DeliveryNetworkManager()
    {
        _logger = new Logger("Network", LogLevel.NetworkTrace);
    }

    public bool Initialize()
    {
        try
        {
            var rules = new SteamNetworkLib.Core.NetworkRules
            {
                EnableRelay = true,
                AcceptOnlyFriends = false
            };

            _client = new SteamNetworkClient(rules);
            if (!_client.Initialize())
            {
                _logger.Error("Failed to initialize SteamNetworkClient");
                return false;
            }

            RegisterMessageHandlers();
            SubscribeToEvents();

            _logger.Msg("Network manager initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            _client = null!;
            _logger.Error($"Failed to initialize network manager: {ex}");
            return false;
        }
    }

    private void RegisterMessageHandlers()
    {
        _client.RegisterMessageHandler<VehicleAddedMessage>(OnVehicleAdded);
        _client.RegisterMessageHandler<VehicleCreatedMessage>(OnVehicleCreated);
        _client.RegisterMessageHandler<VehiclePoolSyncRequest>(OnPoolSyncRequest);
        _client.RegisterMessageHandler<VehiclePoolSyncResponse>(OnPoolSyncResponse);
        _client.RegisterMessageHandler<VehicleAllocationMessage>(OnVehicleAllocation);
        _client.RegisterMessageHandler<BaseVehicleAllocationMessage>(OnBaseVehicleAllocation);
    }

    private void SubscribeToEvents()
    {
        _client.OnLobbyCreated += OnLobbyCreated;
        _client.OnLobbyJoined += OnLobbyJoined;
        _client.OnMemberJoined += OnMemberJoined;
        _client.OnLobbyLeft += OnLobbyLeft;
    }

    public void Update()
    {
        _client?.ProcessIncomingMessages();
    }

    public void Dispose()
    {
        _client?.Dispose();
    }

    #region Broadcasts

    public async void BroadcastVehicleAdded(DeliveryVehicle vehicle)
    {
        if (IsSingleplayer) return;

        var message = new VehicleAddedMessage
        {
            ObjectId = vehicle.Vehicle.ObjectId, // FishNet network ID
            VehicleGuid = vehicle.GUID, // Our DeliveryVehicle GUID
            VehicleName = vehicle.Vehicle.vehicleName,
            VehicleCode = vehicle.Vehicle.VehicleCode,
            VehicleColor = (int)vehicle.Vehicle.Color.displayedColor,
        };

        _logger.Msg($"Broadcasting vehicle added: ObjectId={message.ObjectId}, GUID={message.VehicleGuid}");
        await _client.BroadcastMessageAsync(message);
    }

    public async void BroadcastVehicleCreated(DeliveryVehicle vehicle)
    {
        if (IsSingleplayer || IsHost) return;

        var message = new VehicleCreatedMessage
        {
            ObjectId = vehicle.Vehicle.ObjectId,
            VehicleGuid = vehicle.GUID
        };

        _logger.Msg($"Broadcasting vehicle created: ObjectId={message.ObjectId}");
        await _client.BroadcastMessageAsync(message);
    }

    public async void BroadcastVehicleAllocation(string deliveryId, DeliveryVehicle vehicle, bool isAllocated)
    {
        if (IsSingleplayer) return;

        var message = new VehicleAllocationMessage
        {
            DeliveryId = deliveryId,
            VehicleGuid = vehicle.GUID,
            IsAllocated = isAllocated
        };

        _logger.Msg($"Broadcasting allocation: Delivery={deliveryId}, Vehicle={vehicle.GUID}, Allocated={isAllocated}");
        await _client.BroadcastMessageAsync(message);
    }

    public async void BroadcastBaseVehicleAllocation(string shopName, bool isAllocated)
    {
        if (IsSingleplayer) return;

        var message = new BaseVehicleAllocationMessage
        {
            ShopName = shopName,
            IsAllocated = isAllocated
        };

        _logger.Msg($"Broadcasting base allocation: Shop={shopName}, Allocated={isAllocated}");
        await _client.BroadcastMessageAsync(message);
    }

    #endregion

    #region Event Handlers

    private void OnVehicleAdded(VehicleAddedMessage message, CSteamID cSteamID)
    {
        // if (IsHost) return;

        _logger.Msg($"Received VehicleAdded: ObjectId={message.ObjectId}, GUID={message.VehicleGuid}");
        MelonCoroutines.Start(ProcessMessage());

        IEnumerator ProcessMessage()
        {
            yield return ExponentialBackoff(
                () => UnityEngine.SceneManagement.SceneManager.GetSceneByName("Main").isLoaded, 1f, 30f, 60f);
            yield return ExponentialBackoff(() => FindLandVehicleByObjectId(message.ObjectId) != null, 1f, 30f, 60f);

            try
            {
                // Find the existing LandVehicle by ObjectId (FishNet network ID)
                var landVehicle = FindLandVehicleByObjectId(message.ObjectId);

                if (landVehicle == null)
                {
                    _logger.Warning(
                        $"LandVehicle with ObjectId {message.ObjectId} not found - may not have spawned yet");
                    yield break;
                }

                // The LandVehicle already has its own GUID from the network
                // We use our custom GUID for the DeliveryVehicle component
                var deliveryGuid = new Guid(message.VehicleGuid);
                landVehicle.SetGUID(deliveryGuid);
                landVehicle.ApplyColor((EVehicleColor)message.VehicleColor);

                var deliveryVehicle = landVehicle.GetComponent<DeliveryVehicle>()
                                      ?? new DeliveryVehicleBuilder()
                                          .WithLandVehicle(landVehicle)
                                          .WithGuid(deliveryGuid)
                                          .Build();

                PoolManager.Instance.AddToPool(deliveryVehicle, notify: false); // don't notify back

                _logger.Msg($"Added vehicle to pool: ObjectId={message.ObjectId}, DeliveryGUID={message.VehicleGuid}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to add vehicle: {ex}");
            }
        }
    }

    private void OnVehicleCreated(VehicleCreatedMessage message, CSteamID senderId)
    {
        if (!IsHost) return;
        
        _logger.Msg($"Received vehicle created message from {senderId}");
        MelonCoroutines.Start(ProcessMessage());

        IEnumerator ProcessMessage()
        {
            yield return ExponentialBackoff(
                () => UnityEngine.SceneManagement.SceneManager.GetSceneByName("Main").isLoaded, 1f, 30f, 60f);
            yield return ExponentialBackoff(() => FindLandVehicleByObjectId(message.ObjectId) != null, 1f, 30f, 60f);
            try
            {
                // we technically could use GUID here, but we might as well
                var landVehicle = FindLandVehicleByObjectId(message.ObjectId);

                if (landVehicle == null)
                {
                    _logger.Error(
                        $"LandVehicle with ObjectId {message.ObjectId} not found");
                    yield break;
                }
                VehicleSave.Instance.AddVehicle(landVehicle);
                _logger.Msg($"Added created vehicle to save data: ObjectId={message.ObjectId}, GUID={message.VehicleGuid}");
            }
            catch (Exception e)
            {
                _logger.Error($"Failed to create vehicle: {e}");
            }
        }
    }

    private async void OnPoolSyncRequest(VehiclePoolSyncRequest message, CSteamID senderId)
    {
        if (!IsHost) return;

        _logger.Msg($"Received pool sync request from {senderId}");

        try
        {
            var response = new VehiclePoolSyncResponse();

            foreach (var vehicle in PoolManager.Instance.Pool)
            {
                response.Vehicles.Add(new VehiclePoolSyncResponse.VehicleData
                {
                    ObjectId = vehicle.Vehicle.ObjectId,
                    Guid = vehicle.GUID,
                    Name = vehicle.Vehicle.vehicleName,
                    Code = vehicle.Vehicle.VehicleCode,
                    Color = (int)vehicle.Vehicle.Color.displayedColor,
                });
            }

            _logger.Msg($"Sending pool sync with {response.Vehicles.Count} vehicles");
            await _client.SendMessageToPlayerAsync(senderId, response);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to send pool sync: {ex}");
        }
    }

    private void OnPoolSyncResponse(VehiclePoolSyncResponse message, CSteamID senderId)
    {
        if (IsHost) return;

        _logger.Msg($"Received pool sync response with {message.Vehicles.Count} vehicles");
        MelonCoroutines.Start(ProcessMessage());

        IEnumerator ProcessMessage()
        {
            yield return ExponentialBackoff(
                () => UnityEngine.SceneManagement.SceneManager.GetSceneByName("Main").isLoaded, 1f, 30f, 60f);
            yield return ExponentialBackoff(
                () => message.Vehicles.All(v => FindLandVehicleByObjectId(v.ObjectId) != null),
                1f,
                30f,
                60f
            );
            try
            {
                // Find and attach DeliveryVehicle components
                foreach (var vehicleData in message.Vehicles)
                {
                    var landVehicle = FindLandVehicleByObjectId(vehicleData.ObjectId);

                    if (landVehicle == null)
                    {
                        _logger.Warning($"LandVehicle with ObjectId {vehicleData.ObjectId} not found - skipping");
                        continue;
                    }

                    var deliveryGuid = new Guid(vehicleData.Guid);
                    landVehicle.SetGUID(deliveryGuid);
                    landVehicle.ApplyColor((EVehicleColor)vehicleData.Color);

                    var deliveryVehicle = landVehicle.GetComponent<DeliveryVehicle>()
                                          ?? new DeliveryVehicleBuilder()
                                              .WithLandVehicle(landVehicle)
                                              .WithGuid(deliveryGuid)
                                              .Build();

                    PoolManager.Instance.AddToPool(deliveryVehicle, notify: false); // don't notify back
                }

                _logger.Msg($"Pool synchronized: {PoolManager.Instance.Pool.Count} vehicles");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to sync pool: {ex}");
            }

            yield break;
        }
    }

    private void OnVehicleAllocation(VehicleAllocationMessage message, CSteamID senderId)
    {
        _logger.Msg(
            $"Received allocation: Delivery={message.DeliveryId}, Vehicle={message.VehicleGuid}, Allocated={message.IsAllocated}");

        MelonCoroutines.Start(ProcessMessage());
        IEnumerator ProcessMessage() {
            yield return ExponentialBackoff(
                () => UnityEngine.SceneManagement.SceneManager.GetSceneByName("Main").isLoaded, 1f, 30f, 60f);
            yield return ExponentialBackoff(
                () => PoolManager.Instance.Pool.FirstOrDefault(v => v.GUID == message.VehicleGuid) != null,
                1f,
                30f,
                60f
            );
            try
            {
                var vehicle = PoolManager.Instance.Pool
                    .FirstOrDefault(v => v.GUID == message.VehicleGuid);

                if (vehicle == null)
                {
                    _logger.Warning($"Vehicle not found for allocation: {message.VehicleGuid}");
                    yield break;
                }

                if (message.IsAllocated)
                {
                    _logger.Msg($"Trying to allocate {message.VehicleGuid} for delivery {message.DeliveryId}");
                    if (!PoolManager.Instance.Allocations.ContainsKey(message.DeliveryId))
                    {
                        _logger.Msg($"Allocating {message.VehicleGuid} for delivery {message.DeliveryId}");
                        PoolManager.Instance.Allocations.Add(message.DeliveryId, vehicle);
                    }
                }
                else
                {
                    _logger.Msg($"Deallocating {message.VehicleGuid} for delivery {message.DeliveryId}");
                    PoolManager.Instance.Allocations.Remove(message.DeliveryId);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to process allocation: {ex}");
            }
        }
    }

    private void OnBaseVehicleAllocation(BaseVehicleAllocationMessage message, CSteamID senderId)
    {
        _logger.Msg($"Received base allocation: Shop={message.ShopName}, Allocated={message.IsAllocated}");

        try
        {
            PoolManager.Instance.BaseVehicleAllocationsForShop[message.ShopName] = message.IsAllocated;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to process base allocation: {ex}");
        }
    }

    #endregion

    #region Callbacks

    private void OnLobbyCreated(object sender, SteamNetworkLib.Events.LobbyCreatedEventArgs e)
    {
        _logger.Msg($"Lobby created: {e.Lobby.LobbyId}");
        _client.SetMyData(ModDataKey, ModVersion);
    }

    private void OnLobbyJoined(object sender, SteamNetworkLib.Events.LobbyJoinedEventArgs e)
    {
        _logger.Msg($"Joined lobby: {e.Lobby.LobbyId}");
        _client.SetMyData(ModDataKey, ModVersion);

        if (!IsHost)
        {
            RequestPoolSync();
        }
    }

    private async void OnMemberJoined(object sender, SteamNetworkLib.Events.MemberJoinedEventArgs e)
    {
        if (!IsHost) return;

        _logger.Msg($"Member joined: {e.Member.DisplayName}");

        await Task.Delay(1000);

        var response = new VehiclePoolSyncResponse();
        foreach (var vehicle in PoolManager.Instance.Pool)
        {
            response.Vehicles.Add(new VehiclePoolSyncResponse.VehicleData
            {
                ObjectId = vehicle.Vehicle.ObjectId,
                Guid = vehicle.GUID,
                Name = vehicle.Vehicle.vehicleName,
                Code = vehicle.Vehicle.VehicleCode,
                Color = (int)vehicle.Vehicle.Color.displayedColor
            });
        }

        _logger.Msg($"Sending pool sync to new member ({response.Vehicles.Count} vehicles)");
        await _client.SendMessageToPlayerAsync(e.Member.SteamId, response);
    }

    private void OnLobbyLeft(object sender, SteamNetworkLib.Events.LobbyLeftEventArgs e)
    {
        _logger.Msg($"Left lobby: {e.Reason}");
    }

    #endregion

    private async void RequestPoolSync()
    {
        if (IsSingleplayer || IsHost) return;

        var members = _client.GetLobbyMembers();
        var host = members.FirstOrDefault(m => m.IsOwner);

        if (host == null)
        {
            _logger.Error("Cannot request pool sync - no host found");
            return;
        }

        var message = new VehiclePoolSyncRequest
        {
            RequesterId = _client.LocalPlayerId.ToString()
        };

        _logger.Msg("Requesting pool sync from host");
        await _client.SendMessageToPlayerAsync(host.SteamId, message);
    }

    /// <summary>
    /// Find a LandVehicle by its ObjectId (FishNet network ID)
    /// </summary>
    private LandVehicle? FindLandVehicleByObjectId(int objectId)
    {
        var mainScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("Main");
        if (!mainScene.isLoaded)
        {
            _logger.Error("Main scene is not loaded");
            return null;
        }

        var rootObjects = mainScene.GetRootGameObjects();
        foreach (var root in rootObjects)
        {
            var vehicles = root.GetComponentsInChildren<LandVehicle>(true);
            foreach (var vehicle in vehicles)
            {
                if (vehicle.ObjectId == objectId)
                {
                    _logger.Msg($"Found LandVehicle: ObjectId={objectId}, Name={vehicle.vehicleName}");
                    return vehicle;
                }
            }
        }

        _logger.Warning($"LandVehicle with ObjectId {objectId} not found in Main scene");
        return null;
    }

    private static IEnumerator ExponentialBackoff(Func<bool> predicate, float initialDelay, float finalDelay,
        float timeout)
    {
        var delay = initialDelay;
        var elapsed = 0f;

        while (!predicate() && elapsed < timeout)
        {
            yield return new WaitForSeconds(delay);

            elapsed += delay;
            delay = Mathf.Min(delay * 2f, finalDelay);
        }
    }
}