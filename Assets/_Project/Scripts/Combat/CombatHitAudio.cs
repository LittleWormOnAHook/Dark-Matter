using Project.Audio;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Shared weapon-hit SFX with a short per-collider debounce so legacy WeaponHitbox and
    /// Invector <see cref="PioneerInvectorDamageReceiver"/> paths do not double-play the same impact.
    /// </summary>
    public static class CombatHitAudio
    {
        private const float DebounceSeconds = 0.06f;

        private static float _lastPlayTime = float.NegativeInfinity;
        private static EntityId _lastColliderId;

        public static void PlayWeaponHit(Vector3 position, bool isCritical, Collider hitCollider = null)
        {
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
                audio.PlayWeaponHit(position, isCritical);
        }
    }
}
