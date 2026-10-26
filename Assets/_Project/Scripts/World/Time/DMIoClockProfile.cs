using UnityEngine;

namespace Project.World.Clock
{
    [CreateAssetMenu(
        fileName = "DM_IoClockProfile",
        menuName = "Dark Matter/World/Io Clock Profile")]
    public sealed class DMIoClockProfile : ScriptableObject
    {
        public const string ResourcesPath = "World/DM_IoClockProfile";

        private static DMIoClockProfile live;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLiveCache()
        {
            live = null;
        }

        public static DMIoClockProfile Live
        {
            get
            {
                if (live == null)
                    live = Resources.Load<DMIoClockProfile>(ResourcesPath);
                return live;
            }
        }

        [Header("Io Day")]
        [Tooltip("Game hours in one Io day.")]
        [Min(1)] public int hoursPerDay = 30;
        [Tooltip("Real seconds that equal one Io hour. 120 = 1 real hour per 30-hour Io day.")]
        [Min(1f)] public float realSecondsPerGameHour = 120f;
        [Tooltip("How often clock labels repaint, in real seconds.")]
        [Min(1f)] public float uiPaintRealSeconds = 60f;
        [Tooltip("25q: game-time speed while the sun is below the horizon (1 = same as day, 2 = nights pass twice as fast).")]
        [Range(0.1f, 5f)] public float nightTimeScale = 1f;
        [Tooltip("25q: sun elevation band (deg, +/-) over which the night scale blends in.")]
        [Range(0.01f, 30f)] public float nightTransitionDeg = 4f;
        public void ApplyNightScalePreset(int preset) { nightTimeScale = preset <= 0 ? 1.5f : preset == 1 ? 1f : 3f; nightTransitionDeg = preset <= 0 ? 6f : preset == 1 ? 4f : 2f; }

        [Header("New Game")]
        [Min(1)] public int startDay = 1;
        [Range(0, 29)] public int startHour = 6;
        [Range(0, 59)] public int startMinute;
    }
}
