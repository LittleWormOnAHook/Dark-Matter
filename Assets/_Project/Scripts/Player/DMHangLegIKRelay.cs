using UnityEngine;

namespace Project.Player
{
    /// <summary>
    /// Lives on the Animator object so OnAnimatorIK actually fires.
    /// </summary>
    [DefaultExecutionOrder(2100)]
    public sealed class DMHangLegIKRelay : MonoBehaviour
    {
        [HideInInspector] public DMHangLegOverlay owner;

        private void OnAnimatorIK(int layerIndex)
        {
            if (owner != null)
                owner.ApplyAnimatorIK(layerIndex);
        }
    }
}
