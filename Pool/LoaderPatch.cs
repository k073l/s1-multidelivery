using HarmonyLib;
using MelonLoader;
using UnityEngine;
#if MONO
using ScheduleOne.Delivery;
using ScheduleOne.DevUtilities;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.Persistence.Loaders;
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
    private static readonly Logger Logger = new(nameof(LoaderPatch));

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
            }

            if (deliveriesData != null && deliveriesData.ActiveDeliveries != null)
            {
                DeliveryInstance[] activeDeliveries = deliveriesData.ActiveDeliveries;
                foreach (var delivery in activeDeliveries)
                {
                    NetworkSingleton<DeliveryManager>.Instance.SendDelivery(delivery);
                }

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

            if (deliveriesData != null && deliveriesData.DeliveryHistory != null)
            {
                DeliveryReceipt[] deliveryHistory = deliveriesData.DeliveryHistory;
                foreach (var receipt in deliveryHistory)
                {
                    NetworkSingleton<DeliveryManager>.Instance.RecordDeliveryReceipt_Server(receipt);
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
        var veh = PoolManager.Instance.Pool.FirstOrDefault(dv => dv.Vehicle.GUID.ToString() == data.GUID)?.Vehicle;
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
}