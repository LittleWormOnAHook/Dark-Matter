namespace Project.Combat
{
    public static class CombatElementMapping
    {
        public static CombatElement ToCombatElement(this AmmoType ammoType)
        {
            switch (ammoType)
            {
                case AmmoType.Plasma:
                    return CombatElement.Plasma;
                case AmmoType.Ice:
                    return CombatElement.Cryo;
                case AmmoType.Electricity:
                    return CombatElement.Energy;
                case AmmoType.Laser:
                    return CombatElement.Laser;
                case AmmoType.Ion:
                    return CombatElement.Ion;
                default:
                    return CombatElement.None;
            }
        }

        public static CombatDamageType ToDamageType(this AmmoType ammoType)
        {
            switch (ammoType)
            {
                case AmmoType.Gunpowder:
                    return CombatDamageType.Gunpowder;
                case AmmoType.Fire:
                    return CombatDamageType.Fire;
                case AmmoType.Explosive:
                    return CombatDamageType.Explosive;
                case AmmoType.ResonanceStabilizer:
                    return CombatDamageType.ResonanceStabilizer;
                default:
                    return CombatDamageType.Generic;
            }
        }

        public static string GetDisplayName(this AmmoType ammoType)
        {
            switch (ammoType)
            {
                case AmmoType.Ice:
                    return "Cryo";
                case AmmoType.Electricity:
                    return "Energy";
                default:
                    return ammoType.ToString();
            }
        }

        public static string GetDisplayName(this CombatElement element)
        {
            return element == CombatElement.None ? "None" : element.ToString();
        }
    }
}
