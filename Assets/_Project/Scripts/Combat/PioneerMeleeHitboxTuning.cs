using Invector.vMelee;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Scales Invector melee trigger colliders for wider contact (single volume — no duplicate hitboxes).
    /// </summary>
    public static class PioneerMeleeHitboxTuning
    {
        private const string LegacyAuxPrefix = "PioneerHitForgiveness_";

        public static void ApplyToWeapon(vMeleeWeapon weapon, bool enemyWeapon = false)
        {
            if (weapon == null)
                return;

            RemoveLegacyAuxiliaryBoxes(weapon);

            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            float widthScale = profile != null ? profile.meleeHitboxWidthScale : 2.1f;
            float reachScale = profile != null ? profile.meleeHitboxReachScale : 1.75f;
            if (enemyWeapon && profile != null)
            {
                widthScale = profile.enemyMeleeHitboxWidthScale;
                reachScale = profile.enemyMeleeHitboxReachScale;
            }

            // 2026-10-20d: player hitboxes keep their hand-set size (Anthony's resizes were re-enlarged by x2.1 / x1.75 on every equip / profile refresh).
            bool keepAuthored = profile != null && (enemyWeapon ? profile.enemyMeleeHitboxUseAuthoredSize : profile.playerMeleeHitboxUseAuthoredSize);

            vHitBox[] boxes = weapon.GetComponentsInChildren<vHitBox>(true);
            for (int i = 0; i < boxes.Length; i++)
            {
                vHitBox box = boxes[i];
                if (box == null || IsLegacyAuxiliary(box))
                    continue;

                if (keepAuthored)
                    RestoreAuthored(box);
                else
                    ScalePrimaryTrigger(box, widthScale, reachScale);
            }
        }

        private static bool IsLegacyAuxiliary(vHitBox box)
        {
            return box != null && box.name.StartsWith(LegacyAuxPrefix);
        }

        private static void RemoveLegacyAuxiliaryBoxes(vMeleeWeapon weapon)
        {
            vHitBox[] boxes = weapon.GetComponentsInChildren<vHitBox>(true);
            for (int i = boxes.Length - 1; i >= 0; i--)
            {
                vHitBox box = boxes[i];
                if (box != null && IsLegacyAuxiliary(box))
                    Object.Destroy(box.gameObject);
            }
        }

        /// <summary>Undo any earlier runtime enlargement once (baseline = captured authored size), then stop managing the size.</summary>
        private static void RestoreAuthored(vHitBox box)
        {
            Collider trigger = box.trigger != null ? box.trigger : box.GetComponent<Collider>();
            if (trigger == null)
                return;
            PioneerMeleeHitboxBaseline baseline = trigger.GetComponent<PioneerMeleeHitboxBaseline>();
            if (baseline == null)
                return;
            baseline.ApplyScaled(1f, 1f);
            Object.Destroy(baseline);
        }

        private static void ScalePrimaryTrigger(vHitBox box, float widthScale, float reachScale)
        {
            Collider trigger = box.trigger;
            if (trigger == null)
                trigger = box.GetComponent<Collider>();
            if (trigger == null)
                return;

            PioneerMeleeHitboxBaseline baseline = trigger.GetComponent<PioneerMeleeHitboxBaseline>();
            if (baseline == null)
                baseline = trigger.gameObject.AddComponent<PioneerMeleeHitboxBaseline>();

            baseline.CaptureIfNeeded(trigger);
            baseline.ApplyScaled(widthScale, reachScale);
        }
    }
}
