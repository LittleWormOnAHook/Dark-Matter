using Project.Core;
using Project.UI;
using UnityEngine;

namespace Project.Building
{
    /// <summary>
    /// Ticks facility production once per second while the gameplay session is active.
    /// </summary>
    public class FacilityTaskRunner : MonoBehaviour
    {
        private float tickAccumulator;

        private void Update()
        {
            if (!GameSession.HasStarted)
                return;

            tickAccumulator += Time.deltaTime;
            if (tickAccumulator < 1f)
                return;

            tickAccumulator -= 1f;
            bool paused = EnvironmentalCrisisHudMode.IsCrisisActive;
            BuildingOperationRegistry.TickAllFacilities(1f, paused);
        }
    }
}
