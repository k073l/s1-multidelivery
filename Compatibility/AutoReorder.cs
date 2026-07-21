using System.Reflection;
using HarmonyLib;
using MelonLoader;
using MultiDelivery.Pool;
#if IL2CPP
using Il2CppScheduleOne.UI.Phone.Delivery;
#else
using ScheduleOne.UI.Phone.Delivery;
#endif


namespace MultiDelivery.Compatibility;

public class AutoReorder
{
    private static readonly Logger Logger = new(nameof(AutoReorder));
    public const int MAX_VEHICLES_PER_SHOP = 2;

    public static void ApplyPatches()
    {
        var harmony = Melon<MultiDelivery>.Instance.HarmonyInstance;
        
        var targetAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "AutoReorder");

        if (targetAssembly == null)
        {
            Logger.Error("Target assembly not found!");
            return;
        }
        
        var targetType = targetAssembly.GetType("AutoReorder.Core");
        if (targetType == null)
        {
            Logger.Error("Target class not found!");
            return;
        }
        
        var method = AccessTools.Method(targetType, "ShopHasActiveDelivery");
        if (method == null)
        {
            Logger.Error("ShopHasActiveDelivery method not found!");
            return;
        }

        harmony.Patch(method, postfix: new HarmonyMethod(typeof(AutoReorder).GetMethod(nameof(ShopHasActiveDelivery_Postfix), BindingFlags.Static | BindingFlags.Public)));
    }

    public static void ShopHasActiveDelivery_Postfix(object deliveryApp, string shopName, ref bool __result)
    {
        var shop = DeliveryApp.Instance.GetShop(shopName);
        if (shop == null) return;
        var allocated = PoolManager.Instance.GetAllocationsForShop(shopName);
        if (allocated.Count >= MAX_VEHICLES_PER_SHOP) __result = true;
    }
}