using Project.Core;
using Project.Survival;
using UnityEngine;

namespace Project.UI
{
    [DisallowMultipleComponent]
    public class SurvivalStatsPanelBinder : MonoBehaviour
    {
        private void OnEnable()
        {
            UIManager ui = FindAnyObjectByType<UIManager>();
            if (ui != null)
                ui.RefreshSurvivalDisplay();
        }
    }
}
