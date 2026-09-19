using UnityEngine;

namespace Project.World
{
    /// <summary>
    /// Place on a child of the anchor prefab (or name the child "WireAttach") to define where the survey wire connects.
    /// The wire always uses this transform; mesh width/sag/wiggle never moves this point.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Dark Matter/World/Spline Wire Attach Point")]
    public class DMSplineWireAttachPoint : MonoBehaviour
    {
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.85f, 0.78f, 0.2f, 0.95f);
            Gizmos.DrawWireSphere(transform.position, 0.08f);
        }
#endif
    }
}
