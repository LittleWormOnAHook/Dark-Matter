using UnityEngine;

namespace Project.UI
{
    /// <summary>
    /// Deprecated: progression feedback now lives in the quest tracker HUD, character tab, and XP toasts.
    /// </summary>
    public class ProgressionHudPresenter : MonoBehaviour
    {
        private void Awake()
        {
            Destroy(this);
        }
    }
}
