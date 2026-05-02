using System.Runtime.CompilerServices;
using HarmonyLib;
using MultiDelivery.Helpers;
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
    private static readonly Logger Logger = new(nameof(CapacityText));
    private static ConditionalWeakTable<DeliveryShop, Text> _capacityTexts = new();

    [HarmonyPatch(nameof(DeliveryShop.Initialize))]
    [HarmonyPostfix]
    private static void AddCapacityTextPostfix(DeliveryShop __instance)
    {
        Logger.Debug($"Adding capacity text for {__instance.name}");
        if (_capacityTexts.TryGetValue(__instance, out _)) return;
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
        _capacityTexts.Add(__instance, count);

        // disable lower spacer
        var spacers = templateGo.transform.parent.FindAllTransforms("Spacer");
        if (spacers.Count <= 0) return;
        var spacer = spacers[^1];
        if (spacer != null) spacer.gameObject.SetActive(false);
    }

    [HarmonyPatch(nameof(DeliveryShop.FixedUpdate))]
    [HarmonyPostfix]
    private static void UpdateCapacityTextPostfix(DeliveryShop __instance)
    {
        if (!__instance.IsOpen || !DeliveryApp.Instance.isOpen) return;
        if (!_capacityTexts.TryGetValue(__instance, out var text)) return;
        var (available, total) = GetVehiclesForShop(__instance);
        text.text = $"{available}/{total}";
    }

    private static (int available, int total) GetVehiclesForShop(DeliveryShop shop)
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

        // Custom vehicles
        var customAllocated = Math.Min(pool.Allocations.Count, pool.Pool.Count);
        var customAvailable = pool.Pool.Count - customAllocated;

        available += customAvailable;

        return (available, total);
    }
}