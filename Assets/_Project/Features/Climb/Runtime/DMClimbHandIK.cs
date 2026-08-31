using UnityEngine;

namespace Project.Features.Climb
{
    /// <summary>
    /// Lives on the Animator object so OnAnimatorIK actually fires.
    /// </summary>
    [DefaultExecutionOrder(2000)]
    public sealed class DMClimbHandIK : MonoBehaviour
    {
        [HideInInspector] public DMClimbController owner;

        private void OnAnimatorIK(int layerIndex)
        {
            if (owner != null)
                owner.ApplyHandIK(layerIndex);
        }
    }
}
