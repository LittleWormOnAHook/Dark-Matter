using System;

namespace Project.Storage
{
    [Serializable]
    public class StorageCrateSlotSave
    {
        public string itemId;
        public int amount;
    }

    [Serializable]
    public class StorageCrateSave
    {
        public string crateId;
        public StorageCrateSlotSave[] slots;
    }
}
