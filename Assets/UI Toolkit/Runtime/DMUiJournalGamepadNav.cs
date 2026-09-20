using System.Collections.Generic;
using Project.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Gamepad / stick focus for UITK main menu, pause, journal, and modal sub-panels.
    /// stamp: controller-pad-nav 0920 — Anthony Ctrl+R verify in Play Mode.
    /// </summary>
    public static class DMUiJournalGamepadNav
    {
        public const string Stamp = "controller-pad-nav 0920";

        private const string PadFocusClass = "dmg-pad-focus";
        private const float StickDeadZone = 0.45f;
        private const float RepeatDelaySeconds = 0.14f;

        private static VisualElement lastFocused;
        private static float nextMoveTime;
        private static bool stampedLog;

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
                return;
            }

            if (!stampedLog)
            {
                stampedLog = true;
                Debug.Log($"[DMUiJournalGamepadNav] {Stamp} active (D-Pad / left stick + A select)");
            }

            EnsureFocusables(root);
            FocusController focusController = root.panel?.focusController;
            if (focusController == null)
                return;

            VisualElement focused = focusController.focusedElement as VisualElement;
            if (focused == null || !IsWithinRoot(focused, root))
                FocusFirst(root);

            SyncPadFocusClass(focusController.focusedElement as VisualElement);

            Vector2 move = ReadNavigateVector();
            if (move.sqrMagnitude >= StickDeadZone * StickDeadZone && Time.unscaledTime >= nextMoveTime)
            {
                nextMoveTime = Time.unscaledTime + RepeatDelaySeconds;
                MoveFocus(root, move);
                SyncPadFocusClass(focusController.focusedElement as VisualElement);
            }

            if (WasSubmitPressed())
                ActivateFocused(focusController.focusedElement as VisualElement);
        }

        public static void NotifyMenuOpened(VisualElement root)
        {
            if (root == null)
                return;

            root.schedule.Execute(() => FocusFirst(root)).ExecuteLater(1);
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

        private static Vector2 ReadNavigateVector()
        {
            Gamepad pad = Gamepad.current;
            if (pad == null)
                return Vector2.zero;

            Vector2 nav = pad.dpad.ReadValue();
            if (nav.sqrMagnitude < StickDeadZone * StickDeadZone)
                nav = pad.leftStick.ReadValue();

            if (nav.sqrMagnitude < StickDeadZone * StickDeadZone)
                return Vector2.zero;

            return nav.normalized;
        }

        private static bool WasSubmitPressed()
        {
            Gamepad pad = Gamepad.current;
            if (pad != null && pad.buttonSouth.wasPressedThisFrame)
                return true;

            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.enterKey.wasPressedThisFrame;
        }

        private static void FocusFirst(VisualElement root)
        {
            List<VisualElement> items = CollectFocusables(root);
            if (items.Count == 0)
                return;

            items[0].Focus();
        }

        private static void MoveFocus(VisualElement root, Vector2 direction)
        {
            List<VisualElement> items = CollectFocusables(root);
            if (items.Count == 0)
                return;

            VisualElement focused = root.panel?.focusController?.focusedElement as VisualElement;
            int index = focused != null ? items.IndexOf(focused) : -1;
            if (index < 0)
            {
                FocusFirst(root);
                return;
            }

            bool horizontal = Mathf.Abs(direction.x) > Mathf.Abs(direction.y);
            VisualElement pivot = items[index];
            Rect pivotBounds = pivot.worldBound;
            VisualElement best = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < items.Count; i++)
            {
                if (i == index)
                    continue;

                VisualElement candidate = items[i];
                Rect bounds = candidate.worldBound;
                float dx = bounds.center.x - pivotBounds.center.x;
                float dy = bounds.center.y - pivotBounds.center.y;

                if (horizontal)
                {
                    if (direction.x > 0f && dx <= 4f)
                        continue;
                    if (direction.x < 0f && dx >= -4f)
                        continue;
                    float score = Mathf.Abs(dy) * 3f + Mathf.Abs(dx);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = candidate;
                    }
                }
                else
                {
                    if (direction.y > 0f && dy <= 4f)
                        continue;
                    if (direction.y < 0f && dy >= -4f)
                        continue;
                    float score = Mathf.Abs(dx) * 3f + Mathf.Abs(dy);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = candidate;
                    }
                }
            }

            if (best != null)
                best.Focus();
        }

        private static void ActivateFocused(VisualElement focused)
        {
            if (focused == null || focused is not Button button)
                return;

            if (!button.enabledInHierarchy || button.resolvedStyle.display == DisplayStyle.None)
                return;

            using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = button;
                button.SendEvent(submit);
            }
        }

        private static void EnsureFocusables(VisualElement root)
        {
            root.Query<Button>().ForEach(button =>
            {
                if (button.resolvedStyle.display == DisplayStyle.None)
                    return;

                button.focusable = true;
            });

            root.Query<Toggle>().ForEach(toggle =>
            {
                if (toggle.resolvedStyle.display == DisplayStyle.None)
                    return;

                toggle.focusable = true;
            });
        }

        private static List<VisualElement> CollectFocusables(VisualElement root)
        {
            List<VisualElement> items = new List<VisualElement>(48);
            root.Query<VisualElement>().ForEach(element =>
            {
                if (!element.focusable || !element.enabledInHierarchy)
                    return;

                if (element.resolvedStyle.display == DisplayStyle.None)
                    return;

                if (element is not Button and not Toggle)
                    return;

                if (!IsWithinRoot(element, root))
                    return;

                items.Add(element);
            });

            items.Sort((a, b) =>
            {
                Rect ar = a.worldBound;
                Rect br = b.worldBound;
                int y = br.y.CompareTo(ar.y);
                return y != 0 ? y : ar.x.CompareTo(br.x);
            });

            return items;
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
    }
}
