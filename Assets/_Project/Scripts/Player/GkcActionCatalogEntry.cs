using System;
using Project.Data;
using UnityEngine;

namespace Project.Player
{
    [Serializable]
    public class GkcActionCatalogEntry
    {
        public GkcCombatAction combatAction;
        public int actionId;
        public string stateName;
        public string layerName;
        public bool requiresActionActive;
        public bool useActionActiveUpperBody;
        public bool requiresStrafeMode;
        public bool clearActionIdAfterTrigger = true;
        public bool useDirectCrossFade;
        public GkcWeaponKind weaponFilter = GkcWeaponKind.Infer;
        public float defaultDuration = GkcAnimatorConstants.DefaultActionDuration;
        public float crossFadeDuration = 0.1f;

        public bool MatchesWeapon(GkcWeaponKind weaponKind)
        {
            if (weaponFilter == GkcWeaponKind.Infer)
                return true;

            return weaponFilter == weaponKind;
        }
    }
}
