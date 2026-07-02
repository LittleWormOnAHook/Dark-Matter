using ECM2;
using Project.Interaction;
using Project.Inventory;
using Project.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Companions
{
    /// <summary>
    /// Removes player-only components from shared character models used by companions.
    /// </summary>
    public static class CompanionModelSanitizer
    {
        public static void StripPlayerComponents(GameObject companionRoot)
        {
            if (companionRoot == null)
                return;

            Transform model = companionRoot.transform.Find("ProjectUnityCharacter");
            if (model == null)
                model = companionRoot.transform;

            DestroyIfPresent<PlayerController>(model.gameObject);
            DestroyIfPresent<Character>(model.gameObject);
            DestroyIfPresent<CharacterMovement>(model.gameObject);
            DestroyIfPresent<MeleeCombatController>(model.gameObject);
            DestroyIfPresent<EquipmentController>(model.gameObject);
            DestroyIfPresent<EquippedItemVisual>(model.gameObject);
            DestroyIfPresent<PlayerInput>(model.gameObject);
            DestroyIfPresent<CombatFocusController>(model.gameObject);
            DestroyIfPresent<PlayerLootAnimationController>(model.gameObject);

            Animator animator = model.GetComponent<Animator>();
            if (animator == null)
                animator = model.GetComponentInChildren<Animator>(true);

            if (animator != null)
                animator.applyRootMotion = false;
        }

        private static void DestroyIfPresent<T>(GameObject target) where T : Component
        {
            T[] components = target.GetComponentsInChildren<T>(true);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null)
                    Object.Destroy(components[i]);
            }
        }
    }
}
