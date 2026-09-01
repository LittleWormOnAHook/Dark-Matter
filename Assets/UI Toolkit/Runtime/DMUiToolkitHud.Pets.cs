using Project.Pet;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// In-world UITK pet toolbar slot (left of tools). Dual-run: hides uGUI PetToolbarUI.
    /// Does not spawn companions or touch animators.
    /// </summary>
    public partial class DMUiToolkitHud
    {
        private VisualElement petHost;
        private VisualElement petSlot;
        private VisualElement petIcon;
        private Label petLabel;
        private PetManager boundPetManager;
        private bool petHudBound;
        private bool petEventsHooked;

        private void BindPetToolbar(VisualElement root)
        {
            if (root == null)
                return;

            petHost = root.Q<VisualElement>("pets");
            petSlot = root.Q<VisualElement>("pet-slot");
            petIcon = root.Q<VisualElement>("pet-icon");
            petLabel = root.Q<Label>("pet-label");
            if (petSlot == null && petHost != null)
                petSlot = petHost.Q<VisualElement>("pet-slot");
            if (petIcon == null && petSlot != null)
                petIcon = petSlot.Q<VisualElement>("pet-icon");
            if (petLabel == null && petSlot != null)
                petLabel = petSlot.Q<Label>("pet-label");

            petHudBound = petSlot != null;
            if (!petHudBound)
                return;

            petSlot.pickingMode = PickingMode.Position;
            petSlot.UnregisterCallback<PointerDownEvent>(OnPetHudPointerDown);
            petSlot.UnregisterCallback<PointerEnterEvent>(OnPetHudPointerEnter);
            petSlot.UnregisterCallback<PointerLeaveEvent>(OnPetHudPointerLeave);
            petSlot.RegisterCallback<PointerDownEvent>(OnPetHudPointerDown);
            petSlot.RegisterCallback<PointerEnterEvent>(OnPetHudPointerEnter);
            petSlot.RegisterCallback<PointerLeaveEvent>(OnPetHudPointerLeave);

            HookPetManager();
            RefreshPetToolbar();
        }

        private void UnbindPetToolbarEvents()
        {
            if (petSlot != null)
            {
                petSlot.UnregisterCallback<PointerDownEvent>(OnPetHudPointerDown);
                petSlot.UnregisterCallback<PointerEnterEvent>(OnPetHudPointerEnter);
                petSlot.UnregisterCallback<PointerLeaveEvent>(OnPetHudPointerLeave);
            }

            if (petEventsHooked && boundPetManager != null)
                boundPetManager.OnPetsChanged -= RefreshPetToolbar;

            petEventsHooked = false;
            boundPetManager = null;
            petHudBound = false;
        }

        private void HookPetManager()
        {
            boundPetManager = PetManager.Instance ?? FindAnyObjectByType<PetManager>();
            if (boundPetManager == null || petEventsHooked)
                return;

            boundPetManager.OnPetsChanged -= RefreshPetToolbar;
            boundPetManager.OnPetsChanged += RefreshPetToolbar;
            petEventsHooked = true;
        }

        private void TickPetToolbar()
        {
            if (!petHudBound || !gameplayVisible)
                return;
            if (boundPetManager == null)
                HookPetManager();
        }

        private void RefreshPetToolbar()
        {
            if (!petHudBound)
                return;

            boundPetManager ??= PetManager.Instance ?? FindAnyObjectByType<PetManager>();
            PetController pet = boundPetManager != null ? boundPetManager.ToolbarPet : null;

            if (pet == null)
            {
                if (petIcon != null)
                {
                    petIcon.style.backgroundImage = StyleKeyword.None;
                    petIcon.style.opacity = 0f;
                }

                if (petLabel != null)
                    petLabel.text = "Empty";
                petSlot?.RemoveFromClassList("dmg-hud-slot-selected");
                return;
            }

            Sprite icon = pet.InventoryIcon;
            if (petIcon != null)
            {
                if (icon != null)
                {
                    petIcon.style.backgroundImage = new StyleBackground(Background.FromSprite(icon));
                    petIcon.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                    petIcon.style.opacity = 1f;
                }
                else
                {
                    petIcon.style.backgroundImage = StyleKeyword.None;
                    petIcon.style.opacity = 0f;
                }
            }

            if (petLabel != null)
                petLabel.text = icon != null ? string.Empty : pet.DisplayName;

            if (pet.CompanionActive)
                petSlot.AddToClassList("dmg-hud-slot-selected");
            else
                petSlot.RemoveFromClassList("dmg-hud-slot-selected");
        }

        private void OnPetHudPointerDown(PointerDownEvent evt)
        {
            boundPetManager ??= PetManager.Instance ?? FindAnyObjectByType<PetManager>();
            if (boundPetManager == null)
                return;

            if (evt.button == 1)
            {
                boundPetManager.ClearToolbarPet();
                RefreshPetToolbar();
                evt.StopPropagation();
                return;
            }

            if (evt.button != 0)
                return;

            PetController pet = boundPetManager.ToolbarPet;
            if (pet == null)
                return;

            pet.CompanionActive = !pet.CompanionActive;
            if (pet.CompanionActive)
                pet.SummonToOwner();
            boundPetManager.ApplyToolbarVisibility();
            RefreshPetToolbar();
            evt.StopPropagation();
        }

        private void OnPetHudPointerEnter(PointerEnterEvent evt)
        {
            boundPetManager ??= PetManager.Instance ?? FindAnyObjectByType<PetManager>();
            PetController pet = boundPetManager != null ? boundPetManager.ToolbarPet : null;
            if (pet == null)
                return;
            DMUiToolkitPetChrome.TryShowTooltip(pet, CurrentPointerScreenPosition());
        }

        private void OnPetHudPointerLeave(PointerLeaveEvent evt)
        {
            DMUiToolkitPetChrome.HideTooltip();
        }
    }
}
