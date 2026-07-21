using System;
using UnityEngine;

namespace Project.Pioneers
{
    [Serializable]
    public class ColonistAggregateState
    {
        public int workerCount;
        public int injuredCount;
        public int shelteredCount;
        public int assignedToFacilityCount;

        public int AvailableWorkers =>
            Mathf.Max(0, workerCount - injuredCount - shelteredCount - assignedToFacilityCount);
    }

    [Serializable]
    public class ColonistAggregateSaveRecord
    {
        public int workerCount;
        public int injuredCount;
        public int shelteredCount;
        public int assignedToFacilityCount;

        public static ColonistAggregateSaveRecord FromRuntime(ColonistAggregateState state)
        {
            if (state == null)
                return new ColonistAggregateSaveRecord();

            return new ColonistAggregateSaveRecord
            {
                workerCount = state.workerCount,
                injuredCount = state.injuredCount,
                shelteredCount = state.shelteredCount,
                assignedToFacilityCount = state.assignedToFacilityCount
            };
        }

        public ColonistAggregateState ToRuntime()
        {
            return new ColonistAggregateState
            {
                workerCount = workerCount,
                injuredCount = injuredCount,
                shelteredCount = shelteredCount,
                assignedToFacilityCount = assignedToFacilityCount
            };
        }
    }
}
