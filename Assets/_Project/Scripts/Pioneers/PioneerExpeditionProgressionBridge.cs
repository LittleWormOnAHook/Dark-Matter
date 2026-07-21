using Project.Progression;
using UnityEngine;

namespace Project.Pioneers
{
    /// <summary>
    /// Keeps expedition trio pioneer levels in sync with player level-ups. Benched/camp pioneers do not level.
    /// </summary>
    public class PioneerExpeditionProgressionBridge : MonoBehaviour
    {
        private PioneerRosterManager roster;
        private PlayerProgressionManager progression;

        private void OnEnable()
        {
            roster = PioneerRosterManager.EnsureExists();
            progression = PlayerProgressionManager.EnsureExists();

            if (progression != null)
                progression.OnLevelUp += HandlePlayerLevelUp;
        }

        private void OnDisable()
        {
            if (progression != null)
                progression.OnLevelUp -= HandlePlayerLevelUp;
        }

        private void HandlePlayerLevelUp(int newLevel, int levelsGained)
        {
            if (roster == null || levelsGained <= 0)
                return;

            roster.IncrementExpeditionTrioLevels(levelsGained);
        }
    }
}
