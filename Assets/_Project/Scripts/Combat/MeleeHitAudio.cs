using Project.Audio;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Melee weapon-hit SFX with a short per-collider debounce so legacy WeaponHitbox and
    /// Invector <see cref="PioneerInvectorDamageReceiver"/> paths do not double-play the same impact.
    /// </summary>
    public static class MeleeHitAudio
    {
        private const float DebounceSeconds = 0.06f;

        private static float _lastPlayTime = float.NegativeInfinity;
        private static EntityId _lastColliderId;

        public static void PlayMeleeHit(Vector3 position, bool isCritical, Collider hitCollider = null)
        {
            // Player damage has dedicated local playback, issued only after health loss.
            if (hitCollider != null && hitCollider.GetComponentInParent<Project.Survival.SurvivalStats>() != null)
                return;
            EntityId colliderId = hitCollider != null ? hitCollider.GetEntityId() : EntityId.None;
            if (colliderId != EntityId.None &&
                colliderId == _lastColliderId &&
                Time.unscaledTime - _lastPlayTime < DebounceSeconds)
            {
                return;
            }

            _lastColliderId = colliderId;
            _lastPlayTime = Time.unscaledTime;

            GameAudioManager audio = GameAudioManager.Instance;
            if (audio != null)
                audio.PlayMeleeHit(position, isCritical);
        }
    }
}
