using Project.Data;
using UnityEngine;

namespace Project.Combat
{
    public struct DamageInfo
    {
        public float Amount;
        public CombatDamageType DamageType;
        public CombatElement Element;
        public bool IsCritical;
        public float PoiseDamage;
        public StatusEffectType Status;
        public GameObject Source;
        public Vector3 HitPoint;
        public CombatBodyPart BodyPart;

        public static DamageInfo FromHit(
            float amount,
            bool isCritical,
            GameObject source,
            Vector3 hitPoint,
            ItemData ammoItem = null)
        {
            AmmoType ammo = ammoItem != null ? ammoItem.ammoType : AmmoType.Gunpowder;
            StatusEffectType status = StatusEffectType.None;
            if (ammoItem != null && ammoItem.HasStatusEffect)
                status = ammoItem.ResolveStatusEffect();

            float poiseScale = DM_CombatCoreProfile.Live != null
                ? DM_CombatCoreProfile.Live.poiseDamageFromHealthMultiplier
                : 0.35f;

            return new DamageInfo
            {
                Amount = amount,
                DamageType = ammo.ToDamageType(),
                Element = ammo.ToCombatElement(),
                IsCritical = isCritical,
                PoiseDamage = amount * poiseScale,
                Status = status,
                Source = source,
                HitPoint = hitPoint,
                BodyPart = CombatBodyPart.None
            };
        }
    }
}
