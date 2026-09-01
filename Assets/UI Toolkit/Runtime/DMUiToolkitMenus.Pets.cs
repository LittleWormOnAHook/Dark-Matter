using System;
using System.Collections.Generic;
using Project.Pet;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Toolkit pet board: owned slots + active toolbar slot with inventory-style drag.
    /// Stamp: DMUiToolkit 0901-finish
    /// </summary>
    public partial class DMUiToolkitMenus
    {
        private VisualElement petToolbarSlot;
        private VisualElement petToolbarIcon;
        private Label petToolbarName;
        private readonly List<VisualElement> petSlotRoots = new List<VisualElement>();
        private readonly List<VisualElement> petSlotIcons = new List<VisualElement>();
        private PetController petDragPet;
        private VisualElement petDragSource;
        private bool petDragActive;
        private Vector2 petPointerDown;
        private Vector2 petLastPos;
        private int petPointerId = -1;
        private VisualElement petDragGhost;
        private bool petDragFromToolbar;

        private void BindPetBoard(VisualElement tree)
        {
            if (tree == null)
                return;

            petToolbarSlot = tree.Q<VisualElement>("pet-toolbar-slot");
            petToolbarIcon = tree.Q<VisualElement>("pet-toolbar-icon");
            petToolbarName = tree.Q<Label>("pet-toolbar-name");
            if (petToolbarSlot != null)
            {
                petToolbarSlot.pickingMode = PickingMode.Position;
                petToolbarSlot.UnregisterCallback<PointerDownEvent>(OnPetToolbarPointerDown);
                petToolbarSlot.UnregisterCallback<PointerMoveEvent>(OnPetPointerMove);
                petToolbarSlot.UnregisterCallback<PointerUpEvent>(OnPetPointerUp);
                petToolbarSlot.RegisterCallback<PointerDownEvent>(OnPetToolbarPointerDown);
                petToolbarSlot.RegisterCallback<PointerMoveEvent>(OnPetPointerMove);
                petToolbarSlot.RegisterCallback<PointerUpEvent>(OnPetPointerUp);
            }
        }

        private void RebuildPetBoard()
        {
            if (petGrid == null)
                return;

            petGrid.Clear();
            petSlotRoots.Clear();
            petSlotIcons.Clear();
            boundPets ??= PetManager.Instance ?? FindAnyObjectByType<PetManager>();
            IReadOnlyList<PetController> pets = boundPets != null
                ? boundPets.GetOwnedPets()
                : Array.Empty<PetController>();

            int slots = PetManager.MaxOwnedPets;
            PetController toolbar = boundPets != null ? boundPets.ToolbarPet : null;
            for (int i = 0; i < slots; i++)
            {
                PetController pet = i < pets.Count ? pets[i] : null;
                VisualElement slot = new VisualElement();
                slot.AddToClassList("dmg-pet-slot");
                slot.pickingMode = PickingMode.Position;
                slot.userData = pet;
                bool assigned = pet != null && pet == toolbar;
                slot.EnableInClassList("dmg-inv-slot--active", assigned);

                VisualElement icon = new VisualElement();
                icon.AddToClassList("dmg-inv-icon");
                icon.pickingMode = PickingMode.Ignore;
                if (pet != null)
                    DMUiToolkitStyle.TrySetSpriteBackground(icon, pet.InventoryIcon, ScaleMode.ScaleToFit);
                slot.Add(icon);

                Label name = new Label(pet != null ? pet.DisplayName : string.Empty);
                name.AddToClassList("dmg-pet-name");
                name.pickingMode = PickingMode.Ignore;
                slot.Add(name);

                PetController captured = pet;
                slot.RegisterCallback<PointerDownEvent>(OnPetListPointerDown);
                slot.RegisterCallback<PointerMoveEvent>(OnPetPointerMove);
                slot.RegisterCallback<PointerUpEvent>(OnPetPointerUp);
                slot.RegisterCallback<PointerEnterEvent>(evt =>
                {
                    if (captured == null)
                        return;
                    DMUiToolkitPetChrome.TryShowTooltip(captured, CurrentPointerScreenPosition());
                });
                slot.RegisterCallback<PointerLeaveEvent>(_ => DMUiToolkitPetChrome.HideTooltip());
                petGrid.Add(slot);
                petSlotRoots.Add(slot);
                petSlotIcons.Add(icon);
            }

            if (petToolbarSlot != null)
            {
                petToolbarSlot.EnableInClassList("dmg-inv-slot--active", toolbar != null);
                if (petToolbarIcon != null)
                {
                    if (toolbar != null)
                        DMUiToolkitStyle.TrySetSpriteBackground(petToolbarIcon, toolbar.InventoryIcon, ScaleMode.ScaleToFit);
                    else
                        DMUiToolkitStyle.ClearBackgroundImage(petToolbarIcon);
                    DMUiToolkitOverlayDocument.SetShown(petToolbarIcon, toolbar != null && toolbar.InventoryIcon != null);
                }

                if (petToolbarName != null)
                    petToolbarName.text = toolbar != null ? toolbar.DisplayName : "Empty";
            }

            if (petSummary != null)
            {
                string activeName = toolbar != null ? toolbar.DisplayName : "none";
                petSummary.text = pets.Count == 0
                    ? "No pets befriended yet. Press E to Adopt near a wild pet."
                    : pets.Count + "/" + slots + " owned  ·  Active: " + activeName + "  ·  Drag onto the active slot";
            }
        }

        private void OnPetListPointerDown(PointerDownEvent evt)
        {
            if (evt.currentTarget is not VisualElement slot)
                return;
            BeginPetPointer(slot, slot.userData as PetController, false, evt);
        }

        private void OnPetToolbarPointerDown(PointerDownEvent evt)
        {
            boundPets ??= PetManager.Instance ?? FindAnyObjectByType<PetManager>();
            BeginPetPointer(petToolbarSlot, boundPets != null ? boundPets.ToolbarPet : null, true, evt);
        }

        private void BeginPetPointer(VisualElement host, PetController pet, bool fromToolbar, PointerDownEvent evt)
        {
            if (host == null)
                return;

            petDragPet = pet;
            petDragSource = host;
            petDragFromToolbar = fromToolbar;
            petDragActive = false;
            petPointerDown = (Vector2)evt.position;
            petLastPos = petPointerDown;
            petPointerId = evt.pointerId;
            host.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnPetPointerMove(PointerMoveEvent evt)
        {
            if (petDragSource == null)
                return;

            petLastPos = (Vector2)evt.position;
            if (petDragActive)
            {
                PositionPetGhost(petLastPos);
                return;
            }

            if ((evt.pressedButtons & 1) == 0 || petDragPet == null)
                return;
            Vector2 delta = petLastPos - petPointerDown;
            if (delta.sqrMagnitude < InvDragThresholdPx * InvDragThresholdPx)
                return;

            petDragActive = true;
            ClearPetGhost();
            petDragGhost = new VisualElement { pickingMode = PickingMode.Ignore };
            petDragGhost.style.position = Position.Absolute;
            petDragGhost.style.width = 48f;
            petDragGhost.style.height = 48f;
            petDragGhost.style.opacity = 0.75f;
            if (petDragPet.InventoryIcon != null)
                DMUiToolkitStyle.TrySetSpriteBackground(petDragGhost, petDragPet.InventoryIcon, ScaleMode.ScaleToFit);

            (root ?? petBody)?.Add(petDragGhost);
            PositionPetGhost(petLastPos);
        }

        private void OnPetPointerUp(PointerUpEvent evt)
        {
            bool dragging = petDragActive;
            PetController pet = petDragPet;
            bool fromToolbar = petDragFromToolbar;
            Vector2 panelPos = (Vector2)evt.position;
            int button = evt.button;
            ReleasePetPointer();

            if (dragging)
            {
                CompletePetDrag(pet, fromToolbar, panelPos);
                return;
            }

            if (pet == null || boundPets == null)
                return;

            if (button == 1)
            {
                DMUiToolkitPetChrome.HideTooltip();
                DMUiToolkitPetChrome.TryShowMenuAtPanel(pet, panelPos);
                return;
            }

            if (button != 0)
                return;
            if (boundPets.ToolbarPet == pet)
                boundPets.ClearToolbarPet();
            else
                boundPets.TryAssignToolbarPet(pet);
        }

        private void CompletePetDrag(PetController pet, bool fromToolbar, Vector2 panelPos)
        {
            boundPets ??= PetManager.Instance ?? FindAnyObjectByType<PetManager>();
            if (boundPets == null)
                return;

            if (petToolbarSlot != null && petToolbarSlot.worldBound.Contains(panelPos))
            {
                if (pet != null)
                    boundPets.TryAssignToolbarPet(pet);
                RebuildPetBoard();
                return;
            }

            bool overList = false;
            for (int i = 0; i < petSlotRoots.Count; i++)
            {
                if (petSlotRoots[i] != null && petSlotRoots[i].worldBound.Contains(panelPos))
                {
                    overList = true;
                    break;
                }
            }

            if (fromToolbar && !overList && (petBody == null || !petBody.worldBound.Contains(panelPos)))
                boundPets.ClearToolbarPet();

            RebuildPetBoard();
        }

        private void ReleasePetPointer()
        {
            if (petDragSource != null && petPointerId >= 0 && petDragSource.HasPointerCapture(petPointerId))
                petDragSource.ReleasePointer(petPointerId);

            petDragSource = null;
            petPointerId = -1;
            petDragPet = null;
            petDragActive = false;
            petDragFromToolbar = false;
            ClearPetGhost();
        }

        private void PositionPetGhost(Vector2 panelPos)
        {
            if (petDragGhost == null)
                return;
            VisualElement parent = petDragGhost.parent != null ? petDragGhost.parent : root;
            Vector2 local = parent != null ? parent.WorldToLocal(panelPos) : panelPos;
            petDragGhost.style.left = local.x - 24f;
            petDragGhost.style.top = local.y - 24f;
        }

        private void ClearPetGhost()
        {
            if (petDragGhost == null)
                return;
            petDragGhost.RemoveFromHierarchy();
            petDragGhost = null;
        }
    }
}
