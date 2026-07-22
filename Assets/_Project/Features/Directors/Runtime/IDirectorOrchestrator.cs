namespace Project.Features.Directors
{
    public interface IDirectorOrchestrator
    {
        void EvaluateAll();
        void Evaluate(DirectorTrigger trigger);
    }
}
