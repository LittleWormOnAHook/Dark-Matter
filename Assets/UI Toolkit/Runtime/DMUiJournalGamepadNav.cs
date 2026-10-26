using System.Collections.Generic;
using Project.Core;
using Project.Player;
using Project.Player.Invector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Gamepad / stick focus for UITK main menu, pause, journal, and modal sub-panels.
    /// Digital D-Pad is edge-triggered (one press = one step) with a neutral release buffer.
    /// stamp: controller-pad-nav 0920p
    /// </summary>
    public static class DMUiJournalGamepadNav
    {
        public const string Stamp = "journal-kbm-tabs 0920p";

        private const string PadFocusClass = "dmg-pad-focus";
        private const float StickDeadZone = 0.55f;
        private const float DigitalDeadZone = 0.5f;
        private const float DigitalReleaseDeadZone = 0.22f;
        private const float AnalogInitialRepeat = 0.38f;
        private const float AnalogRepeatRate = 0.22f;
        private const float HoldContextSeconds = 0.45f;
        private const float NeighborAxisSlack = 48f;

        private static readonly string[] ExtraFocusClasses =
        {
            "dmg-inv-slot",
            "dmg-inv-hotbar-slot",
            "dmg-bp-card",
            "dmg-hex-node",
            "dmg-list-row",
            "dmg-ach-slot",
            "dmg-trio-slot",
            "dmg-subtab",
            "dmg-bp-learn",
            "dmg-crate-slot",
            "dmg-wm-list-row",
            // 2026-10-25k: Pet / Companions content.
            "dmg-pet-slot",
            "dmg-companion-row",
            "dmg-companions-roster-row"
        };

        private static VisualElement lastFocused;
        private static bool stampedLog;
        private static float submitHeldSince = -1f;
        private static bool holdContextFired;

        // Navigation latch: digital must return to neutral before another step.
        private static bool moveLatched;
        private static float nextAnalogRepeatTime;
        private static Vector2 lastCardinal;
        private static VisualElement lastActivatedTab;
        // 2026-10-25i: X hold/tap on inventory items + context focus return.
        private static float westHeldSince = -1f;
        private static bool westHoldFired;
        private static VisualElement ctxReturnFocus;
        private static bool wasCtxOpen;

        public static void Tick()
        {
            if (!Application.isPlaying || !DMUiToolkitConfig.IsEnabled || !DMUiToolkitBootstrap.IsRootActive)
                return;

            if (DMUiToolkitLoadingOverlay.IsShowing)
                return;

            VisualElement root = ResolveNavigationRoot();
            if (root == null)
            {
                ClearPadFocusClass(lastFocused);
                lastFocused = null;
                submitHeldSince = -1f;
                holdContextFired = false;
                moveLatched = false;
                lastCardinal = Vector2.zero;
                lastActivatedTab = null;
                return;
            }

            if (!stampedLog)
            {
                stampedLog = true;
                Debug.Log($"[DMUiJournalGamepadNav] {Stamp} active (edge D-Pad + smoothed stick)");
            }

            EnsureFocusables(root);
            RegisterNativeMove(root); // 25r
            FocusController focusController = root.panel?.focusController;
            if (focusController == null)
                return;

            VisualElement focused = focusController.focusedElement as VisualElement;
            // KBM/mouse: do not yank focus every frame (tab clicks often land on Labels/children).
            // Gamepad scheme: recover onto pad candidates. Active stick/D-Pad also allowed.
            bool allowFocusHijack = DMInputSchemeRouter.IsGamepadScheme;
            if (!allowFocusHijack)
            {
                Vector2 digCheck = ReadNavigateRaw(out bool fromDigitalCheck);
                if (fromDigitalCheck || digCheck.sqrMagnitude > 0.01f)
                    allowFocusHijack = true;
            }

            if (allowFocusHijack)
            {
                if (focused == null || !IsWithinRoot(focused, root))
                {
                    FocusFirst(root);
                }
                else if (!IsUsableFocusTarget(focused) || !IsPadFocusCandidate(focused))
                {
                    VisualElement tabOwner = focused;
                    while (tabOwner != null && !IsJournalTabButton(tabOwner))
                        tabOwner = tabOwner.parent;
                    if (tabOwner != null && IsJournalTabButton(tabOwner))
                        tabOwner.Focus();
                    else
                        FocusNearestTo(root, focused);
                }
            }

            VisualElement nowFocused = focusController.focusedElement as VisualElement;
            // Yellow pad ring is gamepad-only so mouse clicks do not look like stuck D-Pad focus.
            if (DMInputSchemeRouter.IsGamepadScheme)
                SyncPadFocusClass(nowFocused);
            else
                ClearPadFocusClass(lastFocused);
            // 2026-10-25i: gamepad focuses tabs with D-pad/stick; A opens them (profile toggle).
            bool padTabsNeedA = DMInputSchemeRouter.IsGamepadScheme && DMJournalPadProfile.Active.tabsRequireA;
            if (allowFocusHijack && !padTabsNeedA && !DMUiToolkitMenus.IsPadMoveActive)
                MaybeActivateNewlyFocusedTab(nowFocused);

            bool ctxOpen = DMUiToolkitMenus.IsInventoryContextOpen;
            if (wasCtxOpen && !ctxOpen)
            {
                if (ctxReturnFocus != null && IsUsableFocusTarget(ctxReturnFocus) && IsWithinRoot(ctxReturnFocus, root))
                    ctxReturnFocus.Focus();
                ctxReturnFocus = null;
            }
            wasCtxOpen = ctxOpen;

            if (DMJournalPadProfile.Active.padDebugLog)
            {
                Gamepad dbgPad = ActivePad();
                Vector2 dbgNav = ReadNavigateRaw(out bool dbgDig);
                bool dbgA = dbgPad != null && dbgPad.buttonSouth.wasPressedThisFrame;
                bool dbgB = dbgPad != null && dbgPad.buttonEast.wasPressedThisFrame;
                bool navEdge = QuantizeCardinal(dbgNav).sqrMagnitude > 0.01f && lastCardinal.sqrMagnitude < 0.01f;
                if (dbgA || dbgB || navEdge)
                    Debug.Log("[JournalPad] input " + (dbgA ? "A " : "") + (dbgB ? "B " : "") + (navEdge ? "nav=" + QuantizeCardinal(dbgNav) + (dbgDig ? " dpad " : " stick ") : "")
                        + "pad=" + (dbgPad != null ? dbgPad.displayName : "none") + " scheme=" + (DMInputSchemeRouter.IsGamepadScheme ? "pad" : "kbm")
                        + " root=" + root.name + " focus=" + DescribeFocus(focusController.focusedElement as VisualElement));
            }
            ApplyNativeNavFeel();
            // 25n: ghost move stays on inventory/hotbar slots whatever moved focus (native navigate included).
            VisualElement fNow = focusController.focusedElement as VisualElement;
            if (DMUiToolkitMenus.IsPadMoveActive && fNow != null && !IsInventorySlotElement(fNow) && lastSlotFocus != null && IsUsableFocusTarget(lastSlotFocus))
                lastSlotFocus.Focus();
            else if (fNow != null && IsInventorySlotElement(fNow))
                lastSlotFocus = fNow;
            TickNavigate(root, focusController);
            TickSubmitHold(focusController.focusedElement as VisualElement);
            if (DMJournalPadProfile.Active.padDebugLog && focusController.focusedElement as VisualElement != dbgLastFocus)
            {
                dbgLastFocus = focusController.focusedElement as VisualElement;
                Debug.Log("[JournalPad] focus -> " + DescribeFocus(dbgLastFocus));
            }

            DMUiToolkitMenus moveHost = DMUiToolkitMenus.Instance;
            if (DMUiToolkitMenus.IsPadMoveActive && moveHost != null && !moveHost.PadMoveMouse
                && focusController.focusedElement is VisualElement moveFocus && IsInventorySlotElement(moveFocus))
                moveHost.UpdatePadMove(moveFocus.worldBound.center);
        }

        private static VisualElement dbgLastFocus;
        private static VisualElement lastSlotFocus;
        private static UnityEngine.InputSystem.UI.InputSystemUIInputModule uiModule;
        private static InputActionReference navLeftDisabledFor;
        private static bool navLeftApplied = true;

        /// <summary>25n: native Navigate repeat timing + left stick gate from the Journal Controller profile.</summary>
        private static void ApplyNativeNavFeel()
        {
            if (uiModule == null)
                uiModule = Object.FindAnyObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (uiModule == null)
                return;
            DMJournalPadProfile pp = DMJournalPadProfile.Active;
            uiModule.moveRepeatDelay = pp.navInitialDelay;
            uiModule.moveRepeatRate = pp.navRepeatRate;
            InputActionReference mv = uiModule.move;
            if (mv == null || mv.action == null)
                return;
            // Mask the left stick on the native Navigate action while the toggle is off.
            if (navLeftDisabledFor != mv || navLeftApplied != pp.navUseLeftStick)
            {
                navLeftDisabledFor = mv;
                navLeftApplied = pp.navUseLeftStick;
                for (int i = 0; i < mv.action.bindings.Count; i++)
                {
                    InputBinding b = mv.action.bindings[i];
                    if (b.isComposite || b.isPartOfComposite) continue;
                    bool left = b.path != null && b.path.Contains("leftStick");
                    if (left)
                    {
                        if (pp.navUseLeftStick) mv.action.RemoveBindingOverride(i);
                        else mv.action.ApplyBindingOverride(i, "");
                    }
                }
            }
        }

        private static string DescribeFocus(VisualElement e)
        {
            if (e == null) return "null";
            string cls = "";
            foreach (string c in e.GetClasses()) { cls += "." + c; if (cls.Length > 60) break; }
            return e.GetType().Name + "#" + e.name + cls;
        }

        public static void NotifyMenuOpened(VisualElement root)
        {
            if (root == null)
                return;

            moveLatched = false;
            lastCardinal = Vector2.zero;
            root.schedule.Execute(() => FocusFirst(root)).ExecuteLater(1);
        }

        private static void TickNavigate(VisualElement root, FocusController focusController)
        {
            Vector2 raw = ReadNavigateRaw(out bool fromDigital);
            Vector2 cardinal = QuantizeCardinal(raw);

            // 25n: stick re-arms only after returning near center (hysteresis) - one step per deliberate push.
            if (cardinal.sqrMagnitude < 0.01f && lastNavFromStick && moveLatched && !fromDigital
                && lastStickMag >= DMJournalPadProfile.Active.navStickRearm)
                return;
            if (cardinal.sqrMagnitude < 0.01f)
            {
                lastNavFromStick = false;
                moveLatched = false;
                lastCardinal = Vector2.zero;
                nextAnalogRepeatTime = 0f;
                return;
            }

            lastNavFromStick = !fromDigital;
            // Direction change while held counts as a fresh step (D-pad only; stick must re-center, 25n).
            if (fromDigital && lastCardinal.sqrMagnitude > 0.01f &&
                (Mathf.Sign(cardinal.x) != Mathf.Sign(lastCardinal.x) ||
                 Mathf.Sign(cardinal.y) != Mathf.Sign(lastCardinal.y) ||
                 (Mathf.Abs(cardinal.x) > 0.01f) != (Mathf.Abs(lastCardinal.x) > 0.01f) ||
                 (Mathf.Abs(cardinal.y) > 0.01f) != (Mathf.Abs(lastCardinal.y) > 0.01f)))
            {
                moveLatched = false;
            }

            bool allowStep;
            if (fromDigital)
            {
                // One press = one move; must release to neutral before the next.
                allowStep = !moveLatched;
                if (allowStep)
                    moveLatched = true;
            }
            else
            {
                if (!moveLatched)
                {
                    allowStep = true;
                    moveLatched = true;
                    nextAnalogRepeatTime = Time.unscaledTime + DMJournalPadProfile.Active.navInitialDelay;
                }
                else
                {
                    allowStep = Time.unscaledTime >= nextAnalogRepeatTime;
                    if (allowStep)
                        nextAnalogRepeatTime = Time.unscaledTime + DMJournalPadProfile.Active.navRepeatRate;
                }
            }

            lastCardinal = cardinal;
            if (!allowStep)
                return;

            StepFocus(root, focusController, cardinal);
        }

        private static void StepFocus(VisualElement root, FocusController focusController, Vector2 cardinal)
        {
            VisualElement beforeMove = focusController.focusedElement as VisualElement;
            MoveFocusNearest(root, cardinal);
            // 2026-10-25i: while moving an item, focus (and the ghost) only steps across inventory/hotbar slots.
            if (DMUiToolkitMenus.IsPadMoveActive && beforeMove != null
                && !(focusController.focusedElement is VisualElement afterMove && IsInventorySlotElement(afterMove)))
                beforeMove.Focus();
            SyncPadFocusClass(focusController.focusedElement as VisualElement);
        }

        // 25r: native UITK NavigationMoveEvent (right stick + D-pad, module repeat 0.5/0.25) is routed through our spatial
        // MoveFocusNearest (tab row <-> body, slots) instead of UITK's default focus-ring move, which only stepped
        // through tab order and never reached the body. Trickle-down on the nav root, then IgnoreEvent + Stop = one step.
        private static readonly HashSet<VisualElement> nativeMoveRoots = new HashSet<VisualElement>();
        private static void RegisterNativeMove(VisualElement root)
        {
            if (root == null || !nativeMoveRoots.Add(root)) return;
            root.RegisterCallback<NavigationMoveEvent>(evt => OnNativeMove(root, evt), TrickleDown.TrickleDown);
        }
        private static void OnNativeMove(VisualElement root, NavigationMoveEvent evt)
        {
            if (DMJournalPadProfile.Active.navDirectPoll || ResolveNavigationRoot() != root) return;
            Vector2 dir;
            switch (evt.direction)
            {
                case NavigationMoveEvent.Direction.Up: dir = Vector2.up; break;
                case NavigationMoveEvent.Direction.Down: dir = Vector2.down; break;
                case NavigationMoveEvent.Direction.Left: dir = Vector2.left; break;
                case NavigationMoveEvent.Direction.Right: dir = Vector2.right; break;
                default: return; // Tab / Shift-Tab keep UITK default
            }
            FocusController fc = root.panel?.focusController;
            if (fc == null) return;
            VisualElement from = fc.focusedElement as VisualElement;
            StepFocus(root, fc, dir);
            fc.IgnoreEvent(evt);
            evt.StopPropagation();
            if (DMJournalPadProfile.Active.padDebugLog)
                Debug.Log("[JournalPad] navmove " + evt.direction + " " + DescribeFocus(from) + " -> " + DescribeFocus(fc.focusedElement as VisualElement));
        }

        private static float lastStickMag;
        private static bool lastNavFromStick;

        private static Vector2 ReadNavigateRaw(out bool fromDigital)
        {
            fromDigital = false;
            lastStickMag = 0f;

            for (int i = 0; i < Gamepad.all.Count; i++)
            {
                Gamepad pad = Gamepad.all[i];
                if (pad == null)
                    continue;

                if (!DMJournalPadProfile.Active.navDirectPoll)
                    break; // 25n: native UI Navigate (right stick + D-pad) is the single nav source
                Vector2 dpad = pad.dpad.ReadValue();
                if (dpad.sqrMagnitude >= DigitalDeadZone * DigitalDeadZone)
                {
                    fromDigital = true;
                    return dpad;
                }

                // Partial press: treat as not-yet-released so latch stays until true neutral.
                if (dpad.sqrMagnitude >= DigitalReleaseDeadZone * DigitalReleaseDeadZone)
                {
                    fromDigital = true;
                    return Vector2.zero;
                }

                Vector2 stick = DMJournalPadProfile.Active.navUseLeftStick ? pad.leftStick.ReadValue() : pad.rightStick.ReadValue();
                lastStickMag = Mathf.Max(lastStickMag, stick.magnitude);
                float dz = DMJournalPadProfile.Active.navStickDeadzone;
                if (stick.sqrMagnitude >= dz * dz)
                    return stick;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && DMJournalPadProfile.Active.navDirectPoll) // 25r: native Navigate already sends arrows/WASD -> no double step
            {
                if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
                {
                    fromDigital = true;
                    return new Vector2(0f, 1f);
                }

                if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
                {
                    fromDigital = true;
                    return new Vector2(0f, -1f);
                }

                if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
                {
                    fromDigital = true;
                    return new Vector2(-1f, 0f);
                }

                if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
                {
                    fromDigital = true;
                    return new Vector2(1f, 0f);
                }
            }

            return Vector2.zero;
        }

        private static Vector2 QuantizeCardinal(Vector2 raw)
        {
            if (raw.sqrMagnitude < 0.01f)
                return Vector2.zero;

            if (Mathf.Abs(raw.x) >= Mathf.Abs(raw.y))
                return new Vector2(Mathf.Sign(raw.x), 0f);

            return new Vector2(0f, Mathf.Sign(raw.y));
        }

        private static bool IsInventorySlotElement(VisualElement element)
        {
            return element != null && element.userData is int
                && (element.ClassListContains("dmg-inv-slot") || element.ClassListContains("dmg-inv-hotbar-slot"));
        }

        /// <summary>
        /// 2026-10-25i inventory pad schema. A: tap = old select/use, hold = pick up (ghost), release = drop.
        /// X: tap = Use, hold = context menu. Context "Move": A or X drops. B cancels (back path).
        /// Returns true when the pad was consumed (keyboard Enter/E still runs the legacy path).
        /// </summary>
        private static bool TickInventoryPad(Gamepad pad, VisualElement focused)
        {
            DMUiToolkitMenus host = DMUiToolkitMenus.Instance;
            if (pad == null || host == null)
                return false;
            float now = Time.unscaledTime;

            if (DMUiToolkitMenus.IsPadMoveActive)
            {
                if (!host.PadMoveMouse)
                {
                    Vector2 c = IsInventorySlotElement(focused) ? focused.worldBound.center : new Vector2(-99999f, -99999f);
                    if (host.PadMoveFromHold)
                    {
                        if (!pad.buttonSouth.isPressed && !pad.buttonWest.isPressed)
                            host.FinishPadMove(c);
                    }
                    else if (pad.buttonSouth.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame)
                    {
                        host.FinishPadMove(c);
                    }
                }
                submitHeldSince = -1f;
                holdContextFired = true;
                westHeldSince = -1f;
                westHoldFired = true;
                return true;
            }

            if (!DMUiToolkitMenus.IsInventoryOpen || DMUiToolkitMenus.IsInventoryContextOpen || !IsInventorySlotElement(focused))
                return false;
            int slotIndex = (int)focused.userData;
            DMJournalPadProfile prof = DMJournalPadProfile.Active;

            if (pad.buttonSouth.wasPressedThisFrame)
            {
                submitHeldSince = now;
                holdContextFired = false;
            }
            if (pad.buttonSouth.isPressed && submitHeldSince >= 0f && !holdContextFired && now - submitHeldSince >= prof.holdMoveSeconds)
            {
                holdContextFired = true;
                host.BeginPadMove(slotIndex, focused.worldBound.center, true, false);
            }
            if (pad.buttonSouth.wasReleasedThisFrame)
            {
                bool tap = submitHeldSince >= 0f && !holdContextFired;
                submitHeldSince = -1f;
                holdContextFired = false;
                if (tap)
                    ActivateFocused(focused);
            }

            if (pad.buttonWest.wasPressedThisFrame)
            {
                westHeldSince = now;
                westHoldFired = false;
            }
            if (pad.buttonWest.isPressed && westHeldSince >= 0f && !westHoldFired && now - westHeldSince >= prof.holdContextSeconds)
            {
                westHoldFired = true;
                ctxReturnFocus = focused;
                OpenInventoryContextIfFocused(focused);
            }
            if (pad.buttonWest.wasReleasedThisFrame)
            {
                bool tap = westHeldSince >= 0f && !westHoldFired;
                westHeldSince = -1f;
                westHoldFired = false;
                if (tap)
                    host.PadUseSlot(slotIndex);
            }
            return true;
        }

        private static void TickSubmitHold(VisualElement focused)
        {
            Gamepad pad = ActivePad();
            if (TickInventoryPad(pad, focused))
                pad = null; // pad consumed; legacy path below only sees keyboard Enter/E
            bool southDown = pad != null && pad.buttonSouth.isPressed;
            bool southPressed = pad != null && pad.buttonSouth.wasPressedThisFrame;
            bool southReleased = pad != null && pad.buttonSouth.wasReleasedThisFrame;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame)
                    southPressed = true;
                if (keyboard.enterKey.isPressed || keyboard.eKey.isPressed)
                    southDown = true;
                if (keyboard.enterKey.wasReleasedThisFrame || keyboard.eKey.wasReleasedThisFrame)
                    southReleased = true;
            }

            if (DMUiToolkitWorldMenus.IsWeaponOpen && pad != null)
            {
                if (pad.buttonWest.wasPressedThisFrame)
                    southPressed = true;
                if (pad.buttonWest.isPressed)
                    southDown = true;
                if (pad.buttonWest.wasReleasedThisFrame)
                    southReleased = true;
            }

            if (southPressed)
            {
                submitHeldSince = Time.unscaledTime;
                holdContextFired = false;
            }

            if (southDown && submitHeldSince >= 0f && !holdContextFired)
            {
                if (Time.unscaledTime - submitHeldSince >= HoldContextSeconds)
                {
                    holdContextFired = true;
                    if (OpenInventoryContextIfFocused(focused))
                        return;
                }
            }

            if (southReleased)
            {
                bool wasTap = submitHeldSince >= 0f && !holdContextFired;
                submitHeldSince = -1f;
                holdContextFired = false;
                if (wasTap)
                    ActivateFocused(focused);
            }
        }

        private static VisualElement ResolveNavigationRoot()
        {
            if (DMUiToolkitSaveSlots.IsOpen)
                return RootFromNamedHost(DMUiToolkitSaveSlots.Name);
            if (DMUiToolkitControls.IsOpen)
                return RootFromNamedHost(DMUiToolkitControls.Name);
            if (DMUiToolkitSettings.IsOpen)
                return RootFromNamedHost(DMUiToolkitSettings.Name);
            if (DMUiToolkitDevPanel.IsOpen)
                return RootFromNamedHost(DMUiToolkitOverlayDocument.DevPanelName);

            if (DMUiToolkitMainMenu.IsVisible)
                return RootFromNamedHost(DMUiToolkitMainMenu.MainMenuName);

            // Loot window (loot plan 8.5, D15): rows reuse the focusable dmg-list-row class.
            if (DMUiToolkitLoot.IsOpen)
                return DMUiToolkitLoot.NavigationRoot;

            // Storage crate (8.5, D15): player and crate grids are focusable (dmg-crate-slot).
            if (DMUiToolkitCrate.IsOpen)
                return DMUiToolkitCrate.NavigationRoot;

            if (DMUiToolkitWorldMenus.IsWeaponOpen)
                return DMUiToolkitWorldMenus.WeaponNavigationRoot;

            // 2026-10-25i: pad nav stays inside the open inventory context menu.
            if (DMUiToolkitMenus.IsInventoryContextOpen
                && DMUiToolkitMenus.TryGetInventoryContextLayer(out VisualElement ctxRoot, out _, out _, out _)
                && ctxRoot != null)
                return ctxRoot;

            if (DMUiToolkitMenus.IsOpen)
                return DMUiToolkitMenus.NavigationRoot;

            return null;
        }

        private static VisualElement RootFromNamedHost(string objectName)
        {
            GameObject host = DMUiToolkitOverlayDocument.FindNamed(objectName);
            if (host == null)
                return null;

            UIDocument document = host.GetComponent<UIDocument>();
            return document != null ? document.rootVisualElement : null;
        }

        private static void FocusFirst(VisualElement root)
        {
            List<VisualElement> items = CollectFocusables(root);
            if (items.Count == 0)
                return;

            items[0].Focus();
        }

        /// <summary>One step to the nearest painted neighbor — tab bar stays among tabs.</summary>
        private static void MoveFocusNearest(VisualElement root, Vector2 direction)
        {
            List<VisualElement> items = CollectFocusables(root);
            if (items.Count == 0)
                return;

            VisualElement focused = root.panel?.focusController?.focusedElement as VisualElement;
            int index = focused != null ? items.IndexOf(focused) : -1;
            if (index < 0)
            {
                if (focused != null && IsWithinRoot(focused, root))
                    FocusNearestTo(root, focused);
                else
                    FocusFirst(root);
                return;
            }

            VisualElement pivot = items[index];
            bool pivotIsTab = IsJournalTabButton(pivot);
            bool horizontal = Mathf.Abs(direction.x) > Mathf.Abs(direction.y);

            if (pivotIsTab && horizontal)
            {
                List<VisualElement> tabs = new List<VisualElement>();
                for (int i = 0; i < items.Count; i++)
                {
                    if (IsJournalTabButton(items[i]))
                        tabs.Add(items[i]);
                }
                tabs.Sort((a, b) => a.worldBound.x.CompareTo(b.worldBound.x));
                int tabIndex = tabs.IndexOf(pivot);
                if (tabIndex >= 0)
                {
                    int next = Mathf.Clamp(tabIndex + (direction.x > 0f ? 1 : -1), 0, tabs.Count - 1);
                    if (next != tabIndex)
                    {
                        tabs[next].Focus();
                        // Focus alone does not switch journal body — activate like a click.
                        // 25k: with tabsRequireA (gamepad) the tab only takes focus; A opens it.
                        if (!(DMInputSchemeRouter.IsGamepadScheme && DMJournalPadProfile.Active.tabsRequireA))
                            ActivateFocused(tabs[next]);
                    }
                }
                return;
            }

            if (pivotIsTab && !horizontal && direction.y < 0f)
            {
                VisualElement content = FindFirstContentFocusable(items);
                if (DMUiToolkitMenus.IsInventoryOpen) // 25r: Inventory -> its first slot
                    for (int i = 0; i < items.Count; i++)
                        if (items[i].ClassListContains("dmg-inv-slot")) { content = items[i]; break; }
                if (content != null)
                {
                    content.Focus();
                    return;
                }
            }

            Rect pivotBounds = pivot.worldBound;
            Vector2 pivotCenter = pivotBounds.width >= 1f ? pivotBounds.center : pivotBounds.position;

            VisualElement best = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < items.Count; i++)
            {
                if (i == index)
                    continue;

                VisualElement candidate = items[i];
                if (!pivotIsTab && horizontal && IsJournalTabButton(candidate))
                    continue;
                if (!pivotIsTab && !horizontal && IsJournalTabButton(candidate) && direction.y < 0f)
                    continue;

                Rect bounds = candidate.worldBound;
                Vector2 center = bounds.width >= 1f ? bounds.center : bounds.position;
                float dx = center.x - pivotCenter.x;
                float dy = center.y - pivotCenter.y;

                if (horizontal)
                {
                    if (direction.x > 0f && dx <= 2f) continue;
                    if (direction.x < 0f && dx >= -2f) continue;
                    float score = Mathf.Abs(dx) + Mathf.Abs(dy) * 3.5f;
                    if (Mathf.Abs(dy) > NeighborAxisSlack) score += 400f;
                    if (score < bestScore) { bestScore = score; best = candidate; }
                }
                else
                {
                    if (direction.y > 0f && dy >= -2f) continue;
                    if (direction.y < 0f && dy <= 2f) continue;
                    float score = Mathf.Abs(dy) + Mathf.Abs(dx) * 3.5f;
                    if (Mathf.Abs(dx) > NeighborAxisSlack) score += 400f;
                    if (score < bestScore) { bestScore = score; best = candidate; }
                }
            }

            // 25r: Up from the body's top row (next item above is a tab, or nothing) returns to the ACTIVE tab.
            if (!pivotIsTab && !horizontal && direction.y > 0f && (best == null || IsJournalTabButton(best))
                && DMUiToolkitMenus.IsOpen && DMUiToolkitMenus.TryGetActiveTabButton(out Button activeTab) && activeTab != null && IsUsableFocusTarget(activeTab))
            {
                activeTab.Focus();
                return;
            }

            if (best != null)
            {
                best.Focus();
                return;
            }

            if (!pivotIsTab && !horizontal && direction.y > 0f)
            {
                VisualElement tab = FindNearestJournalTab(items, pivotCenter);
                if (tab != null) { tab.Focus(); return; }
            }

            int step = horizontal ? (direction.x > 0f ? 1 : -1) : (direction.y > 0f ? -1 : 1);
            int nextIndex = Mathf.Clamp(index + step, 0, items.Count - 1);
            if (nextIndex != index)
                items[nextIndex].Focus();
        }


        private static void MaybeActivateNewlyFocusedTab(VisualElement focused)
        {
            if (!IsJournalTabButton(focused))
                return;
            if (focused == lastActivatedTab)
                return;
            lastActivatedTab = focused;
            ActivateFocused(focused);
        }

        private static bool IsJournalTabButton(VisualElement element)
        {
            if (element == null) return false;
            string name = element.name ?? string.Empty;
            return name.StartsWith("tab-", System.StringComparison.Ordinal);
        }

        private static VisualElement FindFirstContentFocusable(List<VisualElement> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (IsJournalTabButton(items[i])) continue;
                if (IsPadFocusCandidate(items[i]) && IsUsableFocusTarget(items[i]))
                    return items[i];
            }
            return null;
        }

        private static VisualElement FindNearestJournalTab(List<VisualElement> items, Vector2 from)
        {
            VisualElement best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < items.Count; i++)
            {
                if (!IsJournalTabButton(items[i])) continue;
                float d = (items[i].worldBound.center - from).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = items[i]; }
            }
            return best;
        }

        private static void FocusNearestTo(VisualElement root, VisualElement around)
        {
            List<VisualElement> items = CollectFocusables(root);
            if (items.Count == 0) return;

            Vector2 from = around != null && around.worldBound.width >= 1f
                ? around.worldBound.center
                : Vector2.zero;

            VisualElement bestContent = null;
            VisualElement bestAny = null;
            float bestContentDist = float.MaxValue;
            float bestAnyDist = float.MaxValue;
            for (int i = 0; i < items.Count; i++)
            {
                VisualElement el = items[i];
                float d = (el.worldBound.center - from).sqrMagnitude;
                if (d < bestAnyDist) { bestAnyDist = d; bestAny = el; }
                if (!IsJournalTabButton(el) && d < bestContentDist)
                { bestContentDist = d; bestContent = el; }
            }
            VisualElement pick = bestContent != null ? bestContent : bestAny;
            if (pick != null)
                pick.Focus();
        }

        private static void ActivateFocused(VisualElement focused)
        {
            if (focused == null || !IsUsableFocusTarget(focused))
                return;

            // 25k: Journal tabs / sub-tabs open directly (synthetic ClickEvent never reached OnTabClicked).
            if (DMUiToolkitMenus.PadActivateTabOrSubtab(focused))
                return;

            if (focused is Toggle toggle)
            {
                toggle.value = !toggle.value;
                return;
            }

            if (focused is Button button)
            {
                using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
                {
                    submit.target = button;
                    button.SendEvent(submit);
                }

                // 25k: not disposed here - SendEvent may queue it; a using-dispose returned it to the pool before dispatch.
                ClickEvent bclick = ClickEvent.GetPooled();
                bclick.target = button;
                button.SendEvent(bclick);
                return;
            }

            if (ActivateInventorySlotIfFocused(focused))
                return;

            ClickEvent click = ClickEvent.GetPooled();
            click.target = focused;
            focused.SendEvent(click);
        }

        /// <summary>25k: pad B inside Journal content returns focus to the open tab. False = not handled (B closes as before).</summary>
        public static bool TryBackToTabs()
        {
            if (!DMInputSchemeRouter.IsGamepadScheme || !DMUiToolkitMenus.IsOpen)
                return false;
            VisualElement root = DMUiToolkitMenus.NavigationRoot;
            VisualElement focused = root?.panel?.focusController?.focusedElement as VisualElement;
            if (focused == null || !IsWithinRoot(focused, root) || IsJournalTabButton(focused))
                return false;
            if (!DMUiToolkitMenus.TryGetActiveTabButton(out Button tab) || tab == null)
                return false;
            tab.Focus();
            SyncPadFocusClass(tab);
            return true;
        }

        /// <summary>25k: the pad whose face buttons are active this frame (Gamepad.current can be a different/virtual device).</summary>
        private static Gamepad ActivePad()
        {
            for (int i = 0; i < Gamepad.all.Count; i++)
            {
                Gamepad g = Gamepad.all[i];
                if (g == null) continue;
                if (g.buttonSouth.isPressed || g.buttonSouth.wasReleasedThisFrame || g.buttonWest.isPressed || g.buttonWest.wasReleasedThisFrame)
                    return g;
            }
            return Gamepad.current;
        }

        private static void EnsureFocusables(VisualElement root)
        {
            root.Query<Button>().ForEach(MarkFocusable);
            root.Query<Toggle>().ForEach(MarkFocusable);
            root.Query<Slider>().ForEach(MarkFocusable);
            root.Query<SliderInt>().ForEach(MarkFocusable);
            root.Query<DropdownField>().ForEach(MarkFocusable);
            root.Query<TextField>().ForEach(MarkFocusable);
            root.Query<Foldout>().ForEach(MarkFocusable);

            for (int i = 0; i < ExtraFocusClasses.Length; i++)
            {
                string className = ExtraFocusClasses[i];
                root.Query(className: className).ForEach(MarkFocusable);
            }
        }

        private static void MarkFocusable(VisualElement element)
        {
            if (!IsPainted(element))
                return;

            element.focusable = true;
            if (element.tabIndex < 0)
                element.tabIndex = 0;
        }

        private static List<VisualElement> CollectFocusables(VisualElement root)
        {
            List<VisualElement> items = new List<VisualElement>(96);
            HashSet<VisualElement> seen = new HashSet<VisualElement>();

            void TryAdd(VisualElement element)
            {
                if (element == null || !seen.Add(element))
                    return;
                if (!IsUsableFocusTarget(element) || !IsWithinRoot(element, root))
                    return;
                if (!IsPadFocusCandidate(element))
                    return;
                items.Add(element);
            }

            root.Query<Button>().ForEach(TryAdd);
            root.Query<Toggle>().ForEach(TryAdd);
            root.Query<Slider>().ForEach(TryAdd);
            root.Query<SliderInt>().ForEach(TryAdd);
            root.Query<DropdownField>().ForEach(TryAdd);
            root.Query<Foldout>().ForEach(TryAdd);

            for (int i = 0; i < ExtraFocusClasses.Length; i++)
                root.Query(className: ExtraFocusClasses[i]).ForEach(TryAdd);

            items.Sort((a, b) =>
            {
                Rect ar = a.worldBound;
                Rect br = b.worldBound;
                int y = ar.y.CompareTo(br.y);
                return y != 0 ? y : ar.x.CompareTo(br.x);
            });

            return items;
        }

        private static bool IsPadFocusCandidate(VisualElement element)
        {
            if (element is Button or Toggle or Slider or SliderInt or DropdownField or Foldout)
                return true;

            for (int i = 0; i < ExtraFocusClasses.Length; i++)
            {
                if (element.ClassListContains(ExtraFocusClasses[i]))
                    return true;
            }

            return false;
        }

        private static bool IsUsableFocusTarget(VisualElement element)
        {
            if (element == null || !element.focusable || !element.enabledInHierarchy)
                return false;

            if (!IsPainted(element))
                return false;

            // Skip zero-size ghosts that are not yet laid out only when they have no useful class.
            Rect bounds = element.worldBound;
            if (bounds.width < 0.5f || bounds.height < 0.5f)
                return IsPadFocusCandidate(element);

            return true;
        }

        private static bool IsPainted(VisualElement element)
        {
            if (element == null)
                return false;

            if (element.resolvedStyle.display == DisplayStyle.None)
                return false;

            if (element.resolvedStyle.visibility == Visibility.Hidden)
                return false;

            if (element.resolvedStyle.opacity <= 0.01f)
                return false;

            VisualElement walk = element.parent;
            while (walk != null)
            {
                if (walk.resolvedStyle.display == DisplayStyle.None)
                    return false;
                if (walk.resolvedStyle.visibility == Visibility.Hidden)
                    return false;
                walk = walk.parent;
            }

            return true;
        }

        private static bool IsWithinRoot(VisualElement element, VisualElement root)
        {
            while (element != null)
            {
                if (element == root)
                    return true;
                element = element.parent;
            }

            return false;
        }

        private static void SyncPadFocusClass(VisualElement focused)
        {
            if (focused == lastFocused)
                return;

            ClearPadFocusClass(lastFocused);
            lastFocused = focused;
            if (focused == null)
                return;
            focused.AddToClassList(PadFocusClass);
            // 25k: shared pad focus highlight (inline so it wins over per-window USS; colour in Studio Journal Controller).
            DMJournalPadProfile prof = DMJournalPadProfile.Active;
            IStyle s = focused.style;
            s.borderTopColor = s.borderBottomColor = s.borderLeftColor = s.borderRightColor = prof.focusColor;
            s.borderTopWidth = s.borderBottomWidth = s.borderLeftWidth = s.borderRightWidth = prof.focusBorderWidth;
            s.scale = new Scale(new Vector3(prof.focusScale, prof.focusScale, 1f));
            // Scroll follows focus.
            for (VisualElement w = focused.parent; w != null; w = w.parent)
            {
                if (w is ScrollView sv)
                {
                    sv.ScrollTo(focused);
                    break;
                }
            }
        }

        private static void ClearPadFocusClass(VisualElement element)
        {
            if (element == null)
                return;
            element.RemoveFromClassList(PadFocusClass);
            IStyle s = element.style;
            s.borderTopColor = s.borderBottomColor = s.borderLeftColor = s.borderRightColor = StyleKeyword.Null;
            s.borderTopWidth = s.borderBottomWidth = s.borderLeftWidth = s.borderRightWidth = StyleKeyword.Null;
            s.scale = StyleKeyword.Null;
        }

        private static bool OpenInventoryContextIfFocused(VisualElement focused)
        {
            DMUiToolkitMenus host = DMUiToolkitMenus.Instance;
            if (host == null || focused == null || !DMUiToolkitMenus.IsInventoryOpen)
                return false;
            if (focused.userData is not int slotIndex)
                return false;

            ctxReturnFocus = focused;
            host.ShowInventoryContextMenu(slotIndex, focused.worldBound.center);
            return DMUiToolkitMenus.IsInventoryContextOpen;
        }

        private static bool ActivateInventorySlotIfFocused(VisualElement focused)
        {
            DMUiToolkitMenus host = DMUiToolkitMenus.Instance;
            if (host == null || focused == null || !DMUiToolkitMenus.IsInventoryOpen)
                return false;
            if (focused.userData is not int slotIndex)
                return false;

            host.HandleInvClick(slotIndex, 0, focused.worldBound.center);
            return true;
        }
    }
}
