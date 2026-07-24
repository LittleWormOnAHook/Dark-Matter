using Invector;
using Project.Data;
using Project.Player.Invector;
using UnityEngine;

namespace Project.Companions.Invector
{
    /// <summary>
    /// Rolls companion outgoing damage from roster loadout ItemData at reduced strength.
    /// </summary>
    [DisallowMultipleComponent]
    public class CompanionInvectorDamageBridge : MonoBehaviour, IInvectorOutgoingDamageSource
    {
        private const float CompanionDamageMultiplier = 0.25f;

        private CompanionInvectorLoadoutBridge _loadoutBridge;

        private void Awake()
        {
            _loadoutBridge = GetComponent<CompanionInvectorLoadoutBridge>();
        }

        public float ResolveOutgoingDamage(vDamage damage, GameObject source, out bool isCritical)
        {
            isCritical = false;
            ItemData item = _loadoutBridge != null ? _loadoutBridge.ActiveItem : null;
            if (item == null)
                return ScaleRawDamage(damage);

            float rolled;
            if (item.IsRangedWeapon)
            {
                isCritical = item.RollCriticalHit();
                rolled = item.RollRangedDamage(isCritical);
            }
            else if (item.itemType == ItemType.MeleeWeapon)
            {
                isCritical = item.RollCriticalHit();
                rolled = item.RollMeleeDamage(isCritical);
            }
            else
            {
                rolled = ScaleRawDamage(damage);
            }

            return rolled * CompanionDamageMultiplier;
        }

        private static float ScaleRawDamage(vDamage damage)
        {
            float raw = damage != null ? damage.damageValue : 0f;
            return raw * CompanionDamageMultiplier;
        }
    }
}
