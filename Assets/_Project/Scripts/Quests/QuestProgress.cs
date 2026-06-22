using System;
using UnityEngine;

namespace Project.Quests
{
    [Serializable]
    public class QuestProgress
    {
        public string questId;
        public QuestStatus status = QuestStatus.Locked;
        public int[] objectiveProgress;

        public QuestProgress()
        {
            objectiveProgress = Array.Empty<int>();
        }

        public QuestProgress(string id, QuestStatus initialStatus, int objectiveCount)
        {
            questId = id;
            status = initialStatus;
            objectiveProgress = new int[Mathf.Max(1, objectiveCount)];
        }

        public int GetObjectiveProgress(int index)
        {
            if (objectiveProgress == null || index < 0 || index >= objectiveProgress.Length)
                return 0;

            return objectiveProgress[index];
        }

        public void SetObjectiveProgress(int index, int value)
        {
            if (objectiveProgress == null || index < 0 || index >= objectiveProgress.Length)
                return;

            objectiveProgress[index] = Mathf.Max(0, value);
        }
    }
}
