using HarmonyLib;
using MelonLoader;
using MultiDelivery.Helpers;
using MultiDelivery.Persistence;
using UnityEngine;
#if MONO
using ScheduleOne.Delivery;
using ScheduleOne.DevUtilities;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.Persistence.Loaders;
using ScheduleOne.UI.Shop;
using ScheduleOne.Vehicles;
using ScheduleOne.Vehicles.Modification;
using Console = ScheduleOne.Console;
using Guid = System.Guid;
#else
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Persistence.Datas;
using Il2CppScheduleOne.Persistence.Loaders;
using Il2CppScheduleOne.UI.Shop;
using Il2CppScheduleOne.Vehicles;
using Il2CppScheduleOne.Vehicles.Modification;
using Il2CppScheduleOne.Graffiti;
using Console = Il2CppScheduleOne.Console;
using Il2Cpp;
using Guid = Il2CppSystem.Guid;
#endif

namespace MultiDelivery.Pool;

/// <summary>
/// Replace the whole saved deliveries loader - was causing issues, so we have to remove a few offending lines
/// </summary>
[HarmonyPatch(typeof(DeliveriesLoader))]
internal static class LoaderPatch
{
    internal static readonly Logger Logger = new(nameof(LoaderPatch));

    [HarmonyPatch(nameof(DeliveriesLoader.Load))]
    [HarmonyPrefix]
    private static bool Load(DeliveriesLoader __instance, string mainPath)
    {
        var flag = false;
        if (__instance.TryLoadFile(Path.Combine(mainPath, "Deliveries"), out var contents) ||
            __instance.TryLoadFile(mainPath, out contents))
        {
            DeliveriesData deliveriesData = null!;
            try
            {
                deliveriesData = JsonUtility.FromJson<DeliveriesData>(contents);
            }
            catch (Exception ex)
            {
                Debug.LogError("Error loading data: " + ex.Message);
                Logger.Debug("Error loading data: " + ex.Message);
            }

            if (deliveriesData != null && deliveriesData.ActiveDeliveries != null)
            {
                LoadDelivery(deliveriesData);

                if (deliveriesData.DeliveryVehicles != null)
                {
                    flag = true;
                    VehicleData[] deliveryVehicles = deliveriesData.DeliveryVehicles;
                    foreach (var data in deliveryVehicles)
                    {
                        LoadVehicle(data, mainPath);
                    }
                }
            }
        }

        if (!flag && Directory.Exists(mainPath))
        {
            Console.Log("Loading legacy delivery vehicles at: " + mainPath);
            var parentPath = Path.Combine(mainPath, "DeliveryVehicles");
            var directories = __instance.GetDirectories(parentPath);
            for (var j = 0; j < directories.Count; j++)
            {
                __instance.LoadVehicle(directories[j].FullName);
            }
        }

        return false;
    }

    private static void LoadVehicle(VehicleData data, string path)
    {
        Logger.Debug($"Processing GUID {data.GUID}");
        LandVehicle? veh;
        lock (PoolManager.Lock)
        {
            veh = PoolManager.Instance.Pool.FirstOrDefault(dv => dv.Vehicle.GUID.ToString() == data.GUID)?.Vehicle;
        }
        if (veh == null)
        {
            Logger.Debug($"GUID {data.GUID} not found in Pool, lookup via GUIDManager");
            veh = GUIDManager.GetObject<LandVehicle>(new Guid(data.GUID));
        }

        if (veh == null)
        {
            Logger.Error("Vehicle not found with GUID", data.GUID);
            Console.LogError("LoadVehicle: Vehicle not found with GUID " + data.GUID);
        }
        else
        {
            LoadVehicle(veh, data, path);
        }
    }

    private static void LoadVehicle(LandVehicle vehicle, VehicleData data, string containerPath)
    {
        Logger.Debug($"Processing vehicle {vehicle.name} with GUID {vehicle.GUID}");
        if (vehicle.Storage != null)
        {
            if (data.VehicleContents != null && data.VehicleContents.Items != null)
            {
                if (ItemSet.TryDeserialize(data.VehicleContents, out var itemSet))
                {
                    itemSet.LoadTo(vehicle.Storage.ItemSlots);
                }
            }
            else if (File.Exists(Path.Combine(containerPath, "Contents.json")))
            {
                Console.LogWarning("Loading legacy vehicle contents.");
                if (vehicle.Loader.TryLoadFile(containerPath, "Contents", out var contents) &&
                    ItemSet.TryDeserialize(contents, out var itemSet2))
                {
                    itemSet2.LoadTo(vehicle.Storage.ItemSlots);
                }
            }
        }

        if (data.SpraySurfaces == null) return;
        try
        {
            for (var i = 0; i < data.SpraySurfaces.Count; i++)
            {
                if (vehicle._spraySurfaces.Length > i)
                {
#if MONO
                    vehicle._spraySurfaces[i]
                        ?.Set(null, data.SpraySurfaces[i].Strokes.ToArray(),
                            data.SpraySurfaces[i].ContainsCartelGraffiti);
#else
                    vehicle._spraySurfaces[i]
                        ?.Set(null, data.SpraySurfaces[i].Strokes.ToArray().Cast<Il2CppReferenceArray<SprayStroke>>(),
                            data.SpraySurfaces[i].ContainsCartelGraffiti);
#endif
                }
            }
        }
        catch (Exception e)
        {
            Logger.Error("Exception thrown while loading saved graffities", e);
            // ignore
        }
    }
    
    private static DeliveryVehicle? FindVehicleByGuid(string guid)
    {
        lock (PoolManager.Lock)
        {
            var poolVehicle = PoolManager.Instance.Pool
                .FirstOrDefault(dv => dv.GUID.ToString() == guid);
            if (poolVehicle != null) return poolVehicle;
        }

        // Check shop base vehicles
        foreach (var shop in ShopInterface.AllShops)
        {
            if (shop.DeliveryVehicle != null && shop.DeliveryVehicle.GUID.ToString() == guid)
                return shop.DeliveryVehicle;
        }

        return null;
    }

    private static void LoadDelivery(DeliveriesData data)
    {
        if (data == null) return;

        if (data.ActiveDeliveries != null)
        {
            // Restore allocation map from save data
            foreach (var entry in VehicleSave.Instance.AllocationMap.ToList())
            {
                var vehicle = FindVehicleByGuid(entry.VehicleGUID);
                if (vehicle != null)
                {
                    PoolManager.Instance.AllocateDirectly(entry.DeliveryID, vehicle);
                    Logger.Debug($"Restored allocation: {entry.DeliveryID} -> {entry.VehicleGUID}");
                }
                else
                {
                    Logger.Warning($"Could not find vehicle for allocation: {entry.VehicleGUID}");
                }
            }

            // Restore base allocation map from save data
            foreach (var entry in VehicleSave.Instance.BaseAllocationMap.ToList())
            {
                PoolManager.Instance.SetBaseAllocation(entry.ShopName, entry.IsAllocated);
                Logger.Debug($"Restored base allocation: {entry.ShopName} = {entry.IsAllocated}");
            }

            foreach (var delivery in data.ActiveDeliveries)
            {
                DeliveryManager.Instance.SendDelivery(delivery);
            }
        }

        if (data.DeliveryHistory != null)
        {
            foreach (var delivery in data.DeliveryHistory)
                DeliveryManager.Instance._deliveryHistory.Add(delivery);
        }
        if (data.DisplayedDeliveryHistory != null)
        {
            foreach (var delivery in data.DisplayedDeliveryHistory)
                DeliveryManager.Instance._displayedDeliveryHistory.Add(delivery);
        }
    }
}