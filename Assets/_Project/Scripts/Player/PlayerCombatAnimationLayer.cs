using UnityEngine;

namespace Project.Player
{
    /// <summary>
    /// Resolves the optional upper-body combat animator layer created by the editor setup utility.
    /// </summary>
    public static class PlayerCombatAnimationLayer
    {
        public const string UpperBodyCombatLayerName = "Upper Body Combat";
        public const string UpperBodyChargeLayerName = "Upper Body Charge";
        public const string GroundedStateName = "Grounded";
        public const string AirborneStateName = "Airborne";

        private const float AttackFadeOutStart = 0.82f;
        private const float AttackFadeOutEnd = 0.98f;
        private const float LayerWeightSmoothSpeed = 10f;

        public static int ResolveUpperBodyCombatLayer(Animator animator)
        {
            if (animator == null)
                return -1;

            for (int i = 0; i < animator.layerCount; i++)
            {
                if (animator.GetLayerName(i) == UpperBodyCombatLayerName)
                    return i;
            }

            return -1;
        }

        public static int ResolveUpperBodyChargeLayer(Animator animator)
        {
            if (animator == null)
                return -1;

            for (int i = 0; i < animator.layerCount; i++)
            {
                if (animator.GetLayerName(i) == UpperBodyChargeLayerName)
                    return i;
            }

            return -1;
        }

        public static bool IsUpperBodyAttackPlaying(Animator animator)
        {
            int combatLayer = ResolveUpperBodyCombatLayer(animator);
            if (IsLayerPlayingAttack(animator, combatLayer))
                return true;

            int chargeLayer = ResolveUpperBodyChargeLayer(animator);
            return IsLayerPlayingAttack(animator, chargeLayer);
        }

        public static void EnsureBaseLocomotionState(Animator animator, bool grounded)
        {
            if (animator == null || IsUpperBodyAttackPlaying(animator))
                return;

            string targetState = grounded ? GroundedStateName : AirborneStateName;
            int targetHash = Animator.StringToHash(targetState);
            if (!animator.HasState(0, targetHash))
                return;

            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
            if (current.IsName(targetState))
                return;

            if (current.IsTag("Attack") || current.IsName("Pickup"))
                animator.CrossFadeInFixedTime(targetState, 0.12f, 0, 0f);
        }

        public static void SetUpperBodyLayerWeight(Animator animator, int layerIndex, float targetWeight, float deltaTime)
        {
            if (animator == null || layerIndex < 0 || animator.layerCount <= layerIndex)
                return;

            float currentWeight = animator.GetLayerWeight(layerIndex);
            float nextWeight = Mathf.MoveTowards(
                currentWeight,
                targetWeight,
                LayerWeightSmoothSpeed * deltaTime);
            animator.SetLayerWeight(layerIndex, nextWeight);
        }

        public static void BeginUpperBodyAttack(Animator animator, int layerIndex)
        {
            if (animator == null || layerIndex < 0 || animator.layerCount <= layerIndex)
                return;

            animator.SetLayerWeight(layerIndex, 1f);
        }

        public static void UpdateUpperBodyLayerWeight(Animator animator, int layerIndex)
        {
            if (animator == null || layerIndex < 0 || animator.layerCount <= layerIndex)
                return;

            float weight = animator.GetLayerWeight(layerIndex);
            if (weight <= 0.001f)
                return;

            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layerIndex);
            if (!info.IsTag("Attack"))
            {
                SetUpperBodyLayerWeight(animator, layerIndex, 0f, Time.deltaTime);
                return;
            }

            if (info.normalizedTime >= AttackFadeOutEnd)
            {
                animator.SetLayerWeight(layerIndex, 0f);
                return;
            }

            if (info.normalizedTime >= AttackFadeOutStart)
            {
                float fadeT = Mathf.InverseLerp(AttackFadeOutStart, AttackFadeOutEnd, info.normalizedTime);
                animator.SetLayerWeight(layerIndex, Mathf.Lerp(1f, 0f, fadeT));
            }
        }

        private static bool IsLayerPlayingAttack(Animator animator, int layerIndex)
        {
            if (animator == null || layerIndex < 0 || animator.layerCount <= layerIndex)
                return false;

            if (animator.GetLayerWeight(layerIndex) <= 0.01f)
                return false;

            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layerIndex);
            return info.IsTag("Attack") && info.normalizedTime < AttackFadeOutEnd;
        }
    }
}
