using Project.Survival.Exposure;
using UnityEngine;

namespace Project.Survival
{
    /// <summary>
    /// Ensures player exposure components exist at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerExposureBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            if (GetComponent<SurvivalStats>() == null)
                return;

            ExposureController controller = GetComponent<ExposureController>();
            if (controller == null)
                controller = gameObject.AddComponent<ExposureController>();

            // Only one receiver — zones must register on ExposureController, not a stray sibling.
            ExposureReceiver[] receivers = GetComponents<ExposureReceiver>();
            for (int i = 0; i < receivers.Length; i++)
            {
                if (receivers[i] != null && receivers[i] != controller)
                    Destroy(receivers[i]);
            }

            if (GetComponent<ExposureStatusService>() == null)
                gameObject.AddComponent<ExposureStatusService>();
        }
    }
}
