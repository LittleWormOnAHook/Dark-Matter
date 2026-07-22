namespace Project.Features.WorldState
{
    public sealed class ExperienceSnapshot
    {
        public static readonly ExperienceSnapshot Empty = new ExperienceSnapshot();

        public float RadioDensity01 { get; }
        public float Tension01 { get; }
        public bool PreferSilence { get; }

        public ExperienceSnapshot(float radioDensity01 = 0.35f, float tension01 = 0f, bool preferSilence = false)
        {
            RadioDensity01 = radioDensity01;
            Tension01 = tension01;
            PreferSilence = preferSilence;
        }
    }
}
