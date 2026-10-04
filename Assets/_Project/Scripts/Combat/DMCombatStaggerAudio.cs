using Project.Audio;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>Plays combat feedback when poise breaks (<see cref="CombatEvents.Stagger"/>).</summary>
    public sealed class DMCombatStaggerAudio : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureListener()
        {
            if (FindAnyObjectByType<DMCombatStaggerAudio>(FindObjectsInactive.Include) != null)
                return;

            GameObject host = new GameObject(nameof(DMCombatStaggerAudio));
            host.AddComponent<DMCombatStaggerAudio>();
            DontDestroyOnLoad(host);
        }

        private void OnEnable()
        {
            CombatEvents.Stagger += HandleStagger;
        }

        private void OnDisable()
        {
            CombatEvents.Stagger -= HandleStagger;
        }

        private static void HandleStagger(DamageInfo info, GameObject target)
        {
            if (target == null)
                return;

            GameAudioManager.EnsureExists();
            GameAudioManager.Instance?.PlayPoiseStaggerTripleDing();
        }
    }
}
