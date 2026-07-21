using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Spirit Echo stabilization target for Resonance Stabilizer ammo.
    /// </summary>
    public class EchoStabilizeReceiver : MonoBehaviour
    {
        [SerializeField] [Range(0f, 1f)] private float stabilization;
        [SerializeField] private bool isStabilized;

        public float Stabilization => stabilization;
        public bool IsStabilized => isStabilized;

        public bool TryApplyStabilization(GameObject source, float amount)
        {
            if (amount <= 0f)
                return false;

            stabilization = Mathf.Clamp01(stabilization + amount);
            if (stabilization >= 0.85f)
                isStabilized = true;

            return true;
        }

        public void ResetStabilization()
        {
            stabilization = 0f;
            isStabilized = false;
        }
    }
}
