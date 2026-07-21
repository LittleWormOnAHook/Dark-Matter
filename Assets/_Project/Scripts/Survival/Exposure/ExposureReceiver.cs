using System.Collections.Generic;
using UnityEngine;

namespace Project.Survival.Exposure
{
    /// <summary>
    /// Tracks active exposure volumes affecting a player or companion.
    /// </summary>
    public class ExposureReceiver : MonoBehaviour
    {
        private readonly List<ExposureZoneVolume> activeZones = new List<ExposureZoneVolume>(4);

        public IReadOnlyList<ExposureZoneVolume> ActiveZones => activeZones;

        public event System.Action<ExposureZoneVolume> ZoneEntered;
        public event System.Action<ExposureZoneVolume> ZoneExited;

        public void RegisterZone(ExposureZoneVolume zone)
        {
            if (zone == null || activeZones.Contains(zone))
                return;

            activeZones.Add(zone);
            ZoneEntered?.Invoke(zone);
        }

        public void UnregisterZone(ExposureZoneVolume zone)
        {
            if (zone == null || !activeZones.Remove(zone))
                return;

            ZoneExited?.Invoke(zone);
        }

        public void ClearZones()
        {
            activeZones.Clear();
        }
    }
}
