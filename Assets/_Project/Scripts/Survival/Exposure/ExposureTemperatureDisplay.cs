namespace Project.Survival.Exposure
{
    /// <summary>
    /// Maps bipolar thermal stress to EVA suit Fahrenheit readout.
    /// Nominal 70°F at zero stress; cold/heat zones push toward -190°F / 200°F.
    /// </summary>
    public static class ExposureTemperatureDisplay
    {
        public const float NominalFahrenheit = 70f;
        public const float MinFahrenheit = -190f;
        public const float MaxFahrenheit = 200f;

        private const float ColdStressFahrenheit = NominalFahrenheit - MinFahrenheit;
        private const float HeatStressFahrenheit = MaxFahrenheit - NominalFahrenheit;

        public static float StressToFahrenheit(float thermalStress, float maxThermalStress)
        {
            if (maxThermalStress <= 0f)
                return NominalFahrenheit;

            float normalized = UnityEngine.Mathf.Clamp(thermalStress / maxThermalStress, -1f, 1f);
            if (normalized <= 0f)
                return NominalFahrenheit + normalized * ColdStressFahrenheit;

            return NominalFahrenheit + normalized * HeatStressFahrenheit;
        }

        public static float FahrenheitToGaugeNormalized(float fahrenheit)
        {
            return UnityEngine.Mathf.InverseLerp(MinFahrenheit, MaxFahrenheit, fahrenheit);
        }

        public static string GetStatusLabel(float thermalStress, float maxThermalStress)
        {
            if (maxThermalStress <= 0f)
                return "EVA NOMINAL";

            float absStress = UnityEngine.Mathf.Abs(thermalStress);
            if (absStress <= maxThermalStress * 0.08f)
                return "EVA NOMINAL";

            if (thermalStress < 0f)
                return "COLD STRESS";

            return "HEAT STRESS";
        }

        public static string FormatFahrenheit(float fahrenheit)
        {
            return $"{UnityEngine.Mathf.RoundToInt(fahrenheit)}°F";
        }

        public static float FahrenheitToCelsius(float fahrenheit)
        {
            return (fahrenheit - 32f) * (5f / 9f);
        }

        public static string FormatCelsius(float fahrenheit)
        {
            return $"{UnityEngine.Mathf.RoundToInt(FahrenheitToCelsius(fahrenheit))}°C";
        }
    }
}
