using UnityEngine.UIElements;

namespace Project.UI
{
    public partial class DMUiToolkitWorldMenus
    {
        private void TogglePistolsFoldout()
        {
            pistolsExpanded = !pistolsExpanded;
            if (pistolsExpanded)
                riflesExpanded = false;
            ApplyWeaponFoldouts();
            weaponPistols?.Focus();
        }

        private void ToggleRiflesFoldout()
        {
            riflesExpanded = !riflesExpanded;
            if (riflesExpanded)
                pistolsExpanded = false;
            ApplyWeaponFoldouts();
            weaponRifles?.Focus();
        }

        private void ApplyWeaponFoldouts()
        {
            if (pistolsBody != null)
                DMUiToolkitOverlayDocument.SetShown(pistolsBody, weaponOpen && pistolsExpanded);
            if (riflesBody != null)
                DMUiToolkitOverlayDocument.SetShown(riflesBody, weaponOpen && riflesExpanded);

            weaponPistols?.EnableInClassList("dmg-wm-list-row-selected", weaponOpen && pistolsExpanded);
            weaponRifles?.EnableInClassList("dmg-wm-list-row-selected", weaponOpen && riflesExpanded);

            if (weaponPistols != null)
                weaponPistols.text = pistolsExpanded ? "Pistols  ▼" : "Pistols  ▶";
            if (weaponRifles != null)
                weaponRifles.text = riflesExpanded ? "Rifles  ▼" : "Rifles  ▶";

            RefreshWeapon();
        }

        /// <summary>Gamepad / keyboard submit on a category row toggles fold-out instead of firing twice.</summary>
        internal bool TryWeaponCategorySubmit(VisualElement focused)
        {
            if (!weaponOpen || focused == null)
                return false;

            if (focused == weaponPistols)
            {
                TogglePistolsFoldout();
                return true;
            }

            if (focused == weaponRifles)
            {
                ToggleRiflesFoldout();
                return true;
            }

            return false;
        }
    }
}
