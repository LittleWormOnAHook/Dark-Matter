using UnityEngine;

namespace Project.Player
{
    /// <summary>
    /// Legacy compatibility hook for UI systems that pause gameplay input.
    /// Third-person camera control is handled by PlayerController when using ECM2.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        public Transform playerTransform;

        public void SetInventoryOpen(bool open)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
                player.SetInventoryOpen(open);
        }

        public void SetJournalOpen(bool open)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
                player.SetJournalOpen(open);
        }
    }
}
