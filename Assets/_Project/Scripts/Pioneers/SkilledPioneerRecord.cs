using System;
using UnityEngine;

namespace Project.Pioneers
{
    [Serializable]
    public class SkilledPioneerRecord
    {
        public string id;
        public string displayName;
        public SkilledPioneerClass pioneerClass;
        public int level = 1;
        public float radiationResistance = 0.5f;
        public float expeditionEfficiency = 0.5f;
        public float combatSynergy = 0.5f;
        public string backstory;
        public bool isStarterPick;

        public static SkilledPioneerRecord CreateFromStarter(StarterPioneerOffer offer)
        {
            if (offer == null)
                return null;

            return new SkilledPioneerRecord
            {
                id = Guid.NewGuid().ToString("N"),
                displayName = offer.displayName,
                pioneerClass = offer.pioneerClass,
                level = 1,
                radiationResistance = offer.radiationResistance,
                expeditionEfficiency = offer.expeditionEfficiency,
                combatSynergy = offer.combatSynergy,
                backstory = offer.backstory,
                isStarterPick = true
            };
        }
    }

    [Serializable]
    public class SkilledPioneerSaveRecord
    {
        public string id;
        public string displayName;
        public int pioneerClass;
        public int level;
        public float radiationResistance;
        public float expeditionEfficiency;
        public float combatSynergy;
        public string backstory;
        public bool isStarterPick;

        public static SkilledPioneerSaveRecord FromRuntime(SkilledPioneerRecord record)
        {
            if (record == null)
                return null;

            return new SkilledPioneerSaveRecord
            {
                id = record.id,
                displayName = record.displayName,
                pioneerClass = (int)record.pioneerClass,
                level = record.level,
                radiationResistance = record.radiationResistance,
                expeditionEfficiency = record.expeditionEfficiency,
                combatSynergy = record.combatSynergy,
                backstory = record.backstory,
                isStarterPick = record.isStarterPick
            };
        }

        public SkilledPioneerRecord ToRuntime()
        {
            return new SkilledPioneerRecord
            {
                id = id,
                displayName = displayName,
                pioneerClass = (SkilledPioneerClass)Mathf.Clamp(pioneerClass, 0, 3),
                level = level,
                radiationResistance = radiationResistance,
                expeditionEfficiency = expeditionEfficiency,
                combatSynergy = combatSynergy,
                backstory = backstory,
                isStarterPick = isStarterPick
            };
        }
    }
}
