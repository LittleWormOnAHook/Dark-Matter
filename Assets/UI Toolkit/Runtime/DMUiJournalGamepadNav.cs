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
            "dmg-bp-card",
            "dmg-hex-node",
            "dmg-list-row",
            "dmg-ach-slot",
            "dmg-trio-slot",
            "dmg-subtab",
            "dmg-bp-learn"
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
            if (allowFocusHijack)
                MaybeActivateNewlyFocusedTab(nowFocused);

            TickNavigate(root, focusController);
            TickSubmitHold(focusController.focusedElement as VisualElement);
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

            if (cardinal.sqrMagnitude < 0.01f)
            {
                moveLatched = false;
                lastCardinal = Vector2.zero;
                nextAnalogRepeatTime = 0f;
                return;
            }

            // Direction change while held counts as a fresh step.
            if (lastCardinal.sqrMagnitude > 0.01f &&
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
                    nextAnalogRepeatTime = Time.unscaledTime + AnalogInitialRepeat;
                }
                else
                {
                    allowStep = Time.unscaledTime >= nextAnalogRepeatTime;
                    if (allowStep)
                        nextAnalogRepeatTime = Time.unscaledTime + AnalogRepeatRate;
                }
            }

            lastCardinal = cardinal;
            if (!allowStep)
                return;

            MoveFocusNearest(root, cardinal);
            SyncPadFocusClass(focusController.focusedElement as VisualElement);
        }

        private static Vector2 ReadNavigateRaw(out bool fromDigital)
        {
            fromDigital = false;

            for (int i = 0; i < Gamepad.all.Count; i++)
            {
                Gamepad pad = Gamepad.all[i];
                if (pad == null)
                    continue;

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

                Vector2 stick = pad.leftStick.ReadValue();
                if (stick.sqrMagnitude >= StickDeadZone * StickDeadZone)
                    return stick;
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

        private static void TickSubmitHold(VisualElement focused)
        {
            Gamepad pad = Gamepad.current;
            bool southDown = pad != null && pad.buttonSouth.isPressed;
            bool southPressed = pad != null && pad.buttonSouth.wasPressedThisFrame;
            bool southReleased = pad != null && pad.buttonSouth.wasReleasedThisFrame;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.enterKey.wasPressedThisFrame)
                    southPressed = true;
                if (keyboard.enterKey.isPressed)
                    southDown = true;
                if (keyboard.enterKey.wasReleasedThisFrame)
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
                        ActivateFocused(tabs[next]);
                    }
                }
                return;
            }

            if (pivotIsTab && !horizontal && direction.y < 0f)
            {
                VisualElement content = FindFirstContentFocusable(items);
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

                using (ClickEvent click = ClickEvent.GetPooled())
                {
                    click.target = button;
                    button.SendEvent(click);
                }

                return;
            }

            if (ActivateInventorySlotIfFocused(focused))
                return;

            using (ClickEvent click = ClickEvent.GetPooled())
            {
                click.target = focused;
                focused.SendEvent(click);
            }
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
            if (focused != null)
                focused.AddToClassList(PadFocusClass);
        }

        private static void ClearPadFocusClass(VisualElement element)
        {
            element?.RemoveFromClassList(PadFocusClass);
        }

        private static bool OpenInventoryContextIfFocused(VisualElement focused)
        {
            DMUiToolkitMenus host = DMUiToolkitMenus.Instance;
            if (host == null || focused == null || !DMUiToolkitMenus.IsInventoryOpen)
                return false;
            if (focused.userData is not int slotIndex)
                return false;

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
