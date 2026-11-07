# Controls — Genesis Studio

**Status:** Stub (generated tables; notes pending)  
**Open:** Tools → Dark Matter Genesis → Genesis Studio → **Controls**  
RETIRED binding editor — use InputSystem_Actions.inputactions (Project Settings > Input System Package).

## Control scheme auto-switch (2026-11-05b)

`DMInputSchemeRouter` on the live player (`PlayerInput` + Invector `vThirdPersonInput`) keeps **Keyboard&Mouse** vs **Gamepad** in sync for Editor and standalone builds.

- **Gamepad promotion:** deliberate face/shoulder/trigger press, D-pad, or stick deflection above deadzone **0.28** (ignores drift). **KBM promotion:** key press or mouse button only (not mouse delta — avoids fighting the pad).
- **Hot-connect / wake:** `InputSystem.onDeviceChange` (Added/Reconnected/Enabled) switches to Gamepad when a pad appears.
- **Spawn / reload:** one-frame deferred evaluate prefers Gamepad when a pad is connected and KBM is idle; `DMSceneStack` calls `RefreshLiveInstance()` after Mars/IO switch and single-scene reload.
- **Invector:** mutes `GenericInput` on Gamepad scheme; logs `scheme -> … (GenericInput muted/restored)` only when the scheme **changes** (stamp `controller-scheme-router 1105a`).
- **Compile note (2026-11-05f):** `global::Invector.vCharacterController` and `UnityEngine.InputSystem` both define `InputDevice`; router code uses `UnityInputDevice` alias for Input System devices.
- **UI locks:** unchanged — modal gates still disable gameplay maps; journal gamepad nav uses the same router flags.

Runtime verification in standalone builds is still required after merge.

## Movement controls (2026-10-25e)

| Move | Keyboard / mouse | Gamepad |
|---|---|---|
| Sidestep | Double-tap A / D | Left stick left / right + hold A |
| Backstep | Double-tap S | Left stick back + hold A |
| Jump | Space | Tap A |
| Jetpack | Space double-press + hold | A double-press + hold |
| Dash | Hold WASD (diagonals ok) + Left Alt (no direction = forward) | Hold B with left stick deflected |
| Dodge / roll | Q | Tap B |

Tuning: Studio Combat Core > Player quick step and Directional dash input (hold time, stick deadzone, Subtle / Default / Extreme presets).

## Tabs

- [Bindings](#bindings)

## Bindings

**What it does:** Opens a pointer to InputSystem_Actions only — Studio does not write binds.

**Where:**
- Studio: `Genesis Studio / Controls / Bindings` (panel mode `ControlsInputEditor`)
- Asset: `Assets/_Project/Settings/Input/InputSystem_Actions.inputactions`


## Journal controller schema (2026-10-25i)

Profile: `Resources/UI/Controls/DMJournalPadProfile.asset` (Studio > Controls > Journal Controller; presets Subtle/Default/Extreme).

| Input | Result |
|---|---|
| D-pad / left stick | Focus tabs and content (tabs no longer auto-open on focus when `tabsRequireA`) |
| A (tab) | Open focused tab |
| A tap (item) | Old behaviour (select/use) |
| A hold (`holdMoveSeconds`, 0.3 s) | Pick up item: semi-transparent ghost follows focus across inventory + hotbar slots; target slot highlighted |
| Release A (or X) | Drop: slot under ghost (empty = place, occupied = swap/merge, existing CanAcceptItemAt rules such as level-locked hotbar). If not placeable, nearest empty slot within `snapRadiusSlots` (default 1, incl. hotbar). Otherwise returns to its original slot |
| B while moving | Cancel, item returns |
| X tap (item) | Use if usable, else click feedback |
| X hold (`holdContextSeconds`, 0.35 s) | Item context menu (pad nav confined to menu, A confirm, B close, focus returns to the slot) |
| Context "Move" | Ghost move mode. Pad: navigate, A/X drops. Mouse: ghost follows cursor, left-click drops, right-click/Esc cancels |

KBM drag/drop unchanged. Keyboard Enter/E keeps the legacy hold-for-context path. Code: `DMUiJournalGamepadNav.TickInventoryPad`, `DMUiToolkitMenus.PadMove.cs`.

### Journal pad fixes (2026-10-25k)
- A on a tab or sub-tab opens it directly through `DMUiToolkitMenus.PadActivateTabOrSubtab`. Face buttons are read from whichever pad is active, not only `Gamepad.current`.
- D-pad/stick: DOWN from the tab row goes into the content (spatial nearest); UP from the top of the content returns to the tabs. The Inventory grid continues down into the hotbar row. A scroll view scrolls to keep the focused element in view.
- B: in the content, focus returns to the open tab (gamepad only). On the tabs, B closes the Journal as before.
- Focus highlight: a shared `dmg-pad-focus` class plus an inline border and pop scale. Colour, width and scale are in Studio Controls > Journal Controller (`focusColor`, `focusBorderWidth`, `focusScale`), separate from the gold selected-hotbar border.
