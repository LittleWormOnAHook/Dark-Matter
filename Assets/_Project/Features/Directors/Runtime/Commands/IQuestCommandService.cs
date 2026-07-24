namespace Project.Features.Directors
{
    public interface IQuestCommandService
    {
        bool TryActivateQuest(string questId);
        bool TryCompleteObjective(string questId, int objectiveIndex);
    }
}
