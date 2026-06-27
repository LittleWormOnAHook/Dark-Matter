using System.Collections.Generic;
using UnityEngine;

namespace Project.UI
{
    [CreateAssetMenu(menuName = "Project/UI/Main Menu Zone Profile", fileName = "MainMenuZoneProfile")]
    public class MainMenuZoneProfile : ScriptableObject
    {
        public string zoneId;
        public float temperatureC;
        public string surfaceCondition;
        [TextArea(1, 3)]
        public string hazardsText;

        public static IReadOnlyList<MainMenuZoneProfile> GetDefaultZones()
        {
            return new[]
            {
                CreateRuntime("12-B", -190f, "EXTREME", "Sulfur storms, radiation spikes"),
                CreateRuntime("07-A", 42f, "SAFE", "Stable thermal envelope"),
                CreateRuntime("03-C", 428f, "EXTREME", "Lava fissures, heat cascade"),
                CreateRuntime("01-START", 18f, "SAFE", "Command perimeter secured")
            };
        }

        private static MainMenuZoneProfile CreateRuntime(string id, float temp, string condition, string hazards)
        {
            MainMenuZoneProfile profile = CreateInstance<MainMenuZoneProfile>();
            profile.zoneId = id;
            profile.temperatureC = temp;
            profile.surfaceCondition = condition;
            profile.hazardsText = hazards;
            return profile;
        }
    }
}
