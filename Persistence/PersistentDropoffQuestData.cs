using S1API.Internal.Abstraction;
using S1API.Saveables;

namespace DeliveryProject.Persistence;

public class PersistentDropoffQuestData: Saveable
{
    [SaveableField("MessageData")] public bool HasMessaged;
    
    public static PersistentDropoffQuestData Instance { get; private set; } = new();

    public PersistentDropoffQuestData()
    {
        Instance = this;
    }
}