namespace Project.Features.GameState
{
    public sealed class PlayerSnapshot
    {
        public static readonly PlayerSnapshot Empty = new PlayerSnapshot();

        public float Health { get; }
        public float MaxHealth { get; }
        public float Energy { get; }
        public float MaxEnergy { get; }
        public float Stamina { get; }
        public float MaxStamina { get; }
        public float Oxygen { get; }
        public float MaxOxygen { get; }
        public float ThermalStress { get; }
        public float Radiation { get; }
        public float Sulfur { get; }
        public float Volcano { get; }
        public bool IsDead { get; }
        public float PosX { get; }
        public float PosY { get; }
        public float PosZ { get; }

        public PlayerSnapshot(
            float health = 0f, float maxHealth = 0f,
            float energy = 0f, float maxEnergy = 0f,
            float stamina = 0f, float maxStamina = 0f,
            float oxygen = 0f, float maxOxygen = 0f,
            float thermalStress = 0f, float radiation = 0f,
            float sulfur = 0f, float volcano = 0f,
            bool isDead = false,
            float posX = 0f, float posY = 0f, float posZ = 0f)
        {
            Health = health; MaxHealth = maxHealth;
            Energy = energy; MaxEnergy = maxEnergy;
            Stamina = stamina; MaxStamina = maxStamina;
            Oxygen = oxygen; MaxOxygen = maxOxygen;
            ThermalStress = thermalStress; Radiation = radiation;
            Sulfur = sulfur; Volcano = volcano;
            IsDead = isDead;
            PosX = posX; PosY = posY; PosZ = posZ;
        }
    }
}
