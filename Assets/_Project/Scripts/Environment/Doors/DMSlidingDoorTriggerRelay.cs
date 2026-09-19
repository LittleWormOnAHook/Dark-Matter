using UnityEngine;

namespace Project.Environment.Doors
{
    /// <summary>Forwards trigger events from kit <c>Door_Big_TRIGGER</c> to <see cref="DMSlidingDoorController"/>.</summary>
    [DisallowMultipleComponent]
    public sealed class DMSlidingDoorTriggerRelay : MonoBehaviour
    {
        private DMSlidingDoorController controller;

        public void Bind(DMSlidingDoorController door)
        {
            controller = door;
        }

        private void OnTriggerEnter(Collider other)
        {
            controller?.NotifyPlayerEntered(other);
        }

        private void OnTriggerStay(Collider other)
        {
            controller?.NotifyPlayerStay(other);
        }

        private void OnTriggerExit(Collider other)
        {
            controller?.NotifyPlayerExited(other);
        }
    }
}
