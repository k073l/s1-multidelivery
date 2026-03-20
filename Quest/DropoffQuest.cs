using System.Collections;
using DeliveryProject.Helpers;
using DeliveryProject.Pool;
using MelonLoader;
using S1API.Entities;
using S1API.Entities.NPCs.Suburbia;
using S1API.Quests;
using S1API.Quests.Constants;
using S1API.Saveables;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeliveryProject.Quest;

public class DropoffQuest : S1API.Quests.Quest
{
    internal const string Name = "Expanding the Fleet";
    protected override string Title => Name;

    protected override string Description =>
        "Bring a delivery vehicle to the dropoff zone to expand your delivery capacity";

    protected override bool AutoBegin => false;

    internal QuestState State => QuestState;

    private bool _vehicleAdded;

    private QuestEntry _addVehicleEntry;

    private int _startingCapacity;

    private static VehicleDropoffZone dropoffZone;
    private static Vector3 _dropoffZonePosition;
    private static readonly Logger Logger = new(nameof(DropoffQuest));

    protected override void OnCreated()
    {
        base.OnCreated();
        _startingCapacity = PoolManager.Instance.Pool.Count;
        Logger.Debug($"Starting Quest with {_startingCapacity} capacity");
        var vec1 = new Vector3(5.10f, 4.2f, 82.58f);
        var vec2 = new Vector3(0.55f, 4.2f, 76.56f);
        _dropoffZonePosition = (vec1 + vec2) / 2f;
        if (dropoffZone != null) Object.Destroy(dropoffZone.gameObject);
        dropoffZone = VehicleDropoffZoneFactory.CreateZone(
            vec1,
            vec2,
            visualColor: new Color(0f, 1f, 0f, 0.4f),
            attachedQuest: this
        );
        Logger.Debug($"Dropoff zone created. Spawning {dropoffZone.name}");
        OnComplete += Completed;
        Logger.Debug("Wired completion event");
        UpdateQuestEntries();
    }

    private void UpdateQuestEntries()
    {
        QuestEntries.Clear();
        if (!_vehicleAdded)
        {
            Logger.Debug("Adding add vehicle entry");
            _addVehicleEntry = AddEntry($"Purchase a " +
                                        $"{DeliveryProject.RequestedVehicleCode.Capitalize()} " +
                                        $"and drive it into the green dropoff zone (top floor of the parking garage, next to storage unit)",
                _dropoffZonePosition);
            _addVehicleEntry.Begin();
        }

        Logger.Debug("Entries added!");
    }

    public void MarkAddVehicleEntryComplete()
    {
        Logger.Debug("Marked add vehicle entry as completed");
        _addVehicleEntry.Complete();
    }

    private void Completed()
    {
        Logger.Msg("Delivery vehicle dropoff quest completed!");
        MelonCoroutines.Start(NotifyCompletion());
    }

    private IEnumerator NotifyCompletion()
    {
        for (var i = 0; i < 3; ++i)
        {
            if (PoolManager.Instance.Pool.Count > _startingCapacity) break;
            yield return new WaitForSeconds(3f);
        }

        // well now we should have bigger capacity
        var npc = NPC.Get<JeremyWilkinson>();
        if (npc != null)
        {
            if (PoolManager.Instance.Pool.Count <= _startingCapacity)
                npc.SendTextMessage("Something went wrong... Vehicle got in a car crash :(");
            else
            {
                npc.SendTextMessage(
                    $"Vehicle added, you now can order {PoolManager.Instance.Pool.Count} more " +
                    $"deliver{(PoolManager.Instance.Pool.Count > 1 ? "y" : "ies")} " +
                    $"from stores.");
                npc.SendTextMessage("If you want to add more, you know where to find me.");
            }
        }
    }
}