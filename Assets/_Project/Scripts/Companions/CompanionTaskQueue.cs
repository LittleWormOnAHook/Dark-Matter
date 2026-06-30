using UnityEngine;

namespace Project.Companions
{
    public enum CompanionCommand
    {
        Follow,
        Hold,
        Gather
    }

    /// <summary>
    /// Basic command state for expedition trio companions.
    /// </summary>
    public class CompanionTaskQueue
    {
        public CompanionCommand ActiveCommand { get; private set; } = CompanionCommand.Follow;
        public bool IsBusy { get; private set; }
        public Vector3 HoldPosition { get; private set; }
        public float HoldFacingYaw { get; private set; }
        public bool HasHoldPoint { get; private set; }

        public void SetCommand(CompanionCommand command)
        {
            ActiveCommand = command;
            IsBusy = command == CompanionCommand.Gather;

            if (command == CompanionCommand.Follow)
                HasHoldPoint = false;
        }

        public void SetFollow()
        {
            SetCommand(CompanionCommand.Follow);
        }

        public void SetHold(Vector3 worldPosition, float facingYaw)
        {
            ActiveCommand = CompanionCommand.Hold;
            IsBusy = false;
            HoldPosition = worldPosition;
            HoldFacingYaw = facingYaw;
            HasHoldPoint = true;
        }

        public void MarkIdle()
        {
            IsBusy = false;
        }

        public bool ShouldFollow => ActiveCommand == CompanionCommand.Follow && !IsBusy;
        public bool ShouldHold => ActiveCommand == CompanionCommand.Hold;
    }
}
