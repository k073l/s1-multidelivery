using System.Collections;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MelonLoader;
using MultiDelivery.Helpers;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
#if MONO
using ScheduleOne.UI.Phone.Delivery;

#else
using Il2CppScheduleOne.UI.Phone.Delivery;
#endif

namespace MultiDelivery.Pool;

[HarmonyPatch(typeof(DeliveryShop))]
internal static class CapacityText
{
    internal static readonly Logger Logger = new(nameof(CapacityText));
    private static Dictionary<string, Text> _capacityTexts = new();

    [HarmonyPatch(nameof(DeliveryShop.Initialize))]
    [HarmonyPostfix]
    private static void AddCapacityTextPostfix(DeliveryShop __instance)
    {
        Logger.Debug($"Adding capacity text for {__instance.name}");
        if (_capacityTexts.TryGetValue(__instance.MatchingShopInterfaceName, out var text) && 
            text != null) return;
        var template = __instance.DeliveryTimeLabel;
        var templateGo = template?.transform.parent.gameObject;
        Logger.Debug($"Adding capacity text: {template}, templatego: {templateGo}");
        if (templateGo is null) return;
        var go = Object.Instantiate(templateGo);
        go.transform.SetParent(templateGo.transform.parent, false);
        go.transform.SetSiblingIndex(templateGo.transform.GetSiblingIndex());
        var label = go.transform.Find("Label").GetComponent<Text>();
        label.text = "Free vehicles";

        var count = go.transform.Find("Time").GetComponent<Text>();
        count.text = "1/1";
        _capacityTexts.Remove(__instance.MatchingShopInterfaceName);
        _capacityTexts.Add(__instance.MatchingShopInterfaceName, count);
        Logger.Debug($"Added {__instance.MatchingShopInterfaceName} capacity text");

        // disable lower spacer
        var spacers = templateGo.transform.parent.FindAllTransforms("Spacer");
        if (spacers.Count <= 0) return;
        var spacer = spacers[^1];
        if (spacer != null) spacer.gameObject.SetActive(false);
    }

    [HarmonyPatch(nameof(DeliveryShop.RefreshCart))]
    [HarmonyPostfix]
    private static void UpdateCapacityTextPostfix(DeliveryShop __instance)
    {
        if (!DeliveryApp.Instance.isOpen)
        {
            Logger.Debug($"Delivery app is not open, skipping capacity text update for {__instance.name}");
            return;
        }

        MelonCoroutines.Start(DelayedTextUpdate(__instance));
    }

    private static IEnumerator DelayedTextUpdate(DeliveryShop shop)
    {
        yield return new WaitForSeconds(0.5f);
        if (!_capacityTexts.TryGetValue(shop.MatchingShopInterfaceName, out var text))
        {
            Logger.Debug($"Capacity text for {shop.name} not found!");
            yield break;
        }
        var (available, total) = GetVehiclesForShop(shop);
        text.text = $"{available}/{total}";
    }

    private static (int available, int total) GetVehiclesForShop(DeliveryShop shop)
    {
        lock (PoolManager.Lock)
        {
            var shopName = shop.MatchingShopInterfaceName;
            var pool = PoolManager.Instance;

            var total = 1 + pool.Pool.Count;

            var available = 0;

            // Base vehicle
            var baseAllocated = pool.BaseVehicleAllocationsForShop
                .TryGetValue(shopName, out var allocated) && allocated;

            if (!baseAllocated)
                available++;

            // Custom vehicles - only count Pool vehicles in Allocations (excludes base vehicles)
            var customAllocated = pool.Allocations.Values.Count(v => pool.Pool.Contains(v));
            var customAvailable = pool.Pool.Count - customAllocated;

            available += customAvailable;

            return (available, total);
        }
    }
}