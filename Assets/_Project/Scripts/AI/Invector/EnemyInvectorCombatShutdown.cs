using System.Collections.Generic;
using Invector;
using Invector.vCharacterController;
using Invector.vMelee;
using UnityEngine;
using UnityEngine.Events;

namespace Project.AI.Invector
{
    /// <summary>
    /// Stops Invector melee callbacks before animator events can touch destroyed vHitBox refs.
    /// Never calls SetActiveAttack / SetActiveDamage — those throw MissingReferenceException
    /// when stock-armature hitboxes were stripped.
    /// </summary>
    public static class EnemyInvectorCombatShutdown
    {
        public static void Apply(GameObject root)
        {
            if (root == null)
                return;

            DisableMeleeBeforeAnimatorEvents(root);

            vThirdPersonController controller = root.GetComponent<vThirdPersonController>();
            if (controller != null)
            {
                controller.StopCharacter();
                controller.moveDirection = Vector3.zero;
                controller.input = Vector3.zero;
                controller.isSprinting = false;
                controller.lockMovement = true;
                controller.customAction = false;
            }

            Animator animator = controller != null && controller.animator != null
                ? controller.animator
                : root.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                ResetAttackTriggers(animator);
                animator.SetFloat("InputMagnitude", 0f);
                animator.SetFloat("InputHorizontal", 0f);
                animator.SetFloat("InputVertical", 0f);
            }
        }

        public static void DisableMeleeBeforeAnimatorEvents(GameObject root)
        {
            if (root == null)
                return;

            vMeleeManager meleeManager = root.GetComponent<vMeleeManager>();
            if (meleeManager != null)
                meleeManager.enabled = false;

            Animator animator = root.GetComponentInChildren<Animator>(true);
            if (animator != null)
                ResetAttackTriggers(animator);

            SanitizeStaleHitBoxes(root);
        }

        public static void SanitizeStaleHitBoxes(GameObject root)
        {
            if (root == null)
                return;

            vMeleeManager meleeManager = root.GetComponent<vMeleeManager>();
            if (meleeManager != null && meleeManager.Members != null)
            {
                vMeleeAttackObject[] memberAttackObjects = root.GetComponentsInChildren<vMeleeAttackObject>(true);
                for (int i = meleeManager.Members.Count - 1; i >= 0; i--)
                {
                    vBodyMember member = meleeManager.Members[i];
                    if (member == null)
                    {
                        meleeManager.Members.RemoveAt(i);
                        continue;
                    }

                    if (member.attackObject == null)
                        member.attackObject = FindAttackObjectByBodyPart(memberAttackObjects, member.bodyPart);

                    if (member.attackObject == null)
                    {
                        meleeManager.Members.RemoveAt(i);
                        continue;
                    }

                    PruneDestroyedHitBoxes(member.attackObject);
                    SanitizeMeleeDamageEvents(member.attackObject);
                    if (member.attackObject.hitBoxes == null || member.attackObject.hitBoxes.Count == 0)
                    {
                        member.attackObject = null;
                        meleeManager.Members.RemoveAt(i);
                    }
                }
            }

            vMeleeAttackObject[] attackObjects = root.GetComponentsInChildren<vMeleeAttackObject>(true);
            for (int i = 0; i < attackObjects.Length; i++)
            {
                PruneDestroyedHitBoxes(attackObjects[i]);
                SanitizeMeleeDamageEvents(attackObjects[i]);
            }

            vMeleeWeapon equipped = meleeManager != null ? meleeManager.rightWeapon : null;
            vHitBox[] hitBoxes = root.GetComponentsInChildren<vHitBox>(true);
            for (int i = 0; i < hitBoxes.Length; i++)
            {
                vHitBox hitBox = hitBoxes[i];
                if (hitBox == null)
                    continue;

                if (equipped != null &&
                    (hitBox.attackObject == equipped || hitBox.transform.IsChildOf(equipped.transform)))
                    continue;

                hitBox.enabled = false;
            }
        }

        public static void EnableMeleeIfSafe(GameObject root)
        {
            if (root == null)
                return;

            vMeleeManager meleeManager = root.GetComponent<vMeleeManager>();
            if (meleeManager == null)
                return;

            // Fast path: already equipped — skip GetComponentsInChildren every swing.
            if (meleeManager.rightWeapon != null)
            {
                RestoreEquippedWeaponHitBoxes(meleeManager.rightWeapon);
                meleeManager.enabled = true;
                return;
            }

            SanitizeStaleHitBoxes(root);

            if (meleeManager.rightWeapon != null)
            {
                RestoreEquippedWeaponHitBoxes(meleeManager.rightWeapon);
                meleeManager.enabled = true;
                return;
            }

            if (HasLiveUnarmedHitBox(meleeManager))
                meleeManager.enabled = true;
        }

        private static void RestoreEquippedWeaponHitBoxes(vMeleeWeapon weapon)
        {
            if (weapon == null)
                return;

            SanitizeMeleeDamageEvents(weapon);

            if (!weapon.gameObject.activeSelf)
                weapon.gameObject.SetActive(true);

            weapon.enabled = true;
            if (weapon.hitBoxes == null)
                return;

            for (int i = 0; i < weapon.hitBoxes.Count; i++)
            {
                vHitBox box = weapon.hitBoxes[i];
                if (box == null)
                    continue;

                box.enabled = true;
                box.attackObject = weapon;
            }
        }

        private static bool HasLiveUnarmedHitBox(vMeleeManager meleeManager)
        {
            if (meleeManager.Members == null)
                return false;

            for (int i = 0; i < meleeManager.Members.Count; i++)
            {
                vBodyMember member = meleeManager.Members[i];
                if (member == null || member.attackObject == null || member.attackObject.hitBoxes == null)
                    continue;

                for (int h = 0; h < member.attackObject.hitBoxes.Count; h++)
                {
                    if (member.attackObject.hitBoxes[h] != null)
                        return true;
                }
            }

            return false;
        }

        private static vMeleeAttackObject FindAttackObjectByBodyPart(vMeleeAttackObject[] attackObjects, string bodyPart)
        {
            if (attackObjects == null || string.IsNullOrEmpty(bodyPart))
                return null;

            for (int i = 0; i < attackObjects.Length; i++)
            {
                vMeleeAttackObject attackObject = attackObjects[i];
                if (attackObject != null &&
                    !string.IsNullOrEmpty(attackObject.attackObjectName) &&
                    attackObject.attackObjectName.Equals(bodyPart))
                    return attackObject;
            }

            return null;
        }

        private static void PruneDestroyedHitBoxes(vMeleeAttackObject attackObject)
        {
            if (attackObject == null || attackObject.hitBoxes == null)
                return;

            for (int i = attackObject.hitBoxes.Count - 1; i >= 0; i--)
            {
                if (attackObject.hitBoxes[i] == null)
                    attackObject.hitBoxes.RemoveAt(i);
            }
        }

        /// <summary>
        /// Clears broken Invector UnityEvent persistent listeners and ensures damage/hitbox lists exist.
        /// </summary>
        public static void SanitizeMeleeDamageEvents(vMeleeAttackObject attackObject)
        {
            if (attackObject == null)
                return;

            if (attackObject.hitBoxes == null)
                attackObject.hitBoxes = new List<vHitBox>();

            if (attackObject.damage == null)
                attackObject.damage = new vDamage();

            if (attackObject.onPassDamage == null)
                attackObject.onPassDamage = new OnReceiveDamage();
            if (attackObject.onDamageHit == null)
                attackObject.onDamageHit = new OnHitEnter();
            if (attackObject.onRecoilHit == null)
                attackObject.onRecoilHit = new OnHitEnter();

            if (NeedsNewUnityEvent(attackObject.onEnableDamage))
                attackObject.onEnableDamage = new UnityEvent();
            if (NeedsNewUnityEvent(attackObject.onDisableDamage))
                attackObject.onDisableDamage = new UnityEvent();
        }

        private static bool NeedsNewUnityEvent(UnityEvent evt)
        {
            return evt == null || evt.GetPersistentEventCount() > 0;
        }

        private static void ResetAttackTriggers(Animator animator)
        {
            if (animator == null)
                return;

            animator.ResetTrigger("WeakAttack");
            animator.ResetTrigger("StrongAttack");
            animator.ResetTrigger("TriggerReaction");
        }
    }
}
