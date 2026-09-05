using System;
using Project.Core;
using Project.Player;
using Project.Progression;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    [DisallowMultipleComponent]
    public class DMUiToolkitDevPanel : MonoBehaviour
    {
        private static DMUiToolkitDevPanel instance;

        private UIDocument document;
        private VisualElement root;
        private Label liveLabel;
        private Label slot0Label;
        private Label statusLabel;
        private Button godButton;
        private Button stamButton;
        private Button energyButton;
        private Button oxyButton;
        private Button pauseButton;
        private Button cursorButton;
        private TextField levelField;
        private TextField spField;
        private TextField acField;
        private TextField itemField;
        private TextField qtyField;
        private TextField noteField;
        private ScrollView registryView;
        private string selectedId;
        private bool open;
        private bool wired;

        public static bool IsOpen => instance != null && instance.open;

        public static DMUiToolkitDevPanel EnsureHost()
        {
            if (!DMUiToolkitConfig.IsEnabled)
                return null;

            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(
                DMUiToolkitOverlayDocument.DevPanelName,
                DMUiToolkitOverlayDocument.DevPanelUxml,
                DMUiToolkitOverlayDocument.DevPanelUss,
                DMUiToolkitOverlayDocument.DevPanelSort);
            if (doc == null)
                return null;

            DMUiToolkitDevPanel host = doc.GetComponent<DMUiToolkitDevPanel>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitDevPanel>();

            host.document = doc;
            host.BindTree();
            return host;
        }

        public static void Toggle()
        {
            DMUiToolkitDevPanel host = EnsureHost();
            if (host == null)
                return;

            if (host.open)
                host.HideInternal();
            else
                host.ShowInternal();
        }

        public static bool HandleBack()
        {
            if (!IsOpen)
                return false;

            instance.HideInternal();
            return true;
        }

        private void Awake()
        {
            instance = this;
            if (document == null)
                document = GetComponent<UIDocument>();
        }

        private void OnDestroy()
        {
            DMSaveLoadRegistry.Changed -= RefreshRegistry;
            if (instance == this)
                instance = null;
        }

        private void BindTree()
        {
            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                return;

            VisualElement tree = document.rootVisualElement;
            if (tree == null)
                return;

            root = tree.Q<VisualElement>("dev-root") ?? tree;
            liveLabel = tree.Q<Label>("dev-live");
            slot0Label = tree.Q<Label>("dev-slot0");
            statusLabel = tree.Q<Label>("dev-status");
            godButton = tree.Q<Button>("dev-god");
            stamButton = tree.Q<Button>("dev-stam");
            energyButton = tree.Q<Button>("dev-energy");
            oxyButton = tree.Q<Button>("dev-oxy");
            pauseButton = tree.Q<Button>("dev-pause");
            cursorButton = tree.Q<Button>("dev-cursor");
            levelField = tree.Q<TextField>("dev-level");
            spField = tree.Q<TextField>("dev-sp");
            acField = tree.Q<TextField>("dev-ac");
            itemField = tree.Q<TextField>("dev-item");
            qtyField = tree.Q<TextField>("dev-qty");
            noteField = tree.Q<TextField>("dev-note");
            registryView = tree.Q<ScrollView>("dev-registry");

            if (wired)
                return;

            wired = true;
            Wire(godButton, () =>
            {
                DMDevCommandState.GodMode = !DMDevCommandState.GodMode;
                SetStatus(DMDevCommandState.GodMode ? "God mode on." : "God mode off.");
                RefreshToggles();
            });
            Wire(stamButton, () =>
            {
                DMDevCommandState.InfiniteStamina = !DMDevCommandState.InfiniteStamina;
                RefreshToggles();
            });
            Wire(energyButton, () =>
            {
                DMDevCommandState.InfiniteEnergy = !DMDevCommandState.InfiniteEnergy;
                RefreshToggles();
            });
            Wire(oxyButton, () =>
            {
                DMDevCommandState.InfiniteOxygen = !DMDevCommandState.InfiniteOxygen;
                RefreshToggles();
            });
            Wire(tree.Q<Button>("dev-refill"), () => SetStatus(DMDevCommandState.RefillVitals()));
            Wire(pauseButton, TogglePause);
            Wire(cursorButton, ToggleCursor);
            Wire(tree.Q<Button>("dev-set-level"), () =>
            {
                if (int.TryParse(levelField != null ? levelField.value : "5", out int level))
                    SetStatus(DMDevCommandState.SetLevel(level));
                RefreshLive();
            });
            Wire(tree.Q<Button>("dev-add-sp"), () =>
            {
                if (int.TryParse(spField != null ? spField.value : "25", out int sp))
                    SetStatus(DMDevCommandState.AddSkillPoints(sp));
                RefreshLive();
            });
            Wire(tree.Q<Button>("dev-add-ac"), () =>
            {
                if (int.TryParse(acField != null ? acField.value : "500", out int ac))
                    SetStatus(DMDevCommandState.AddCredits(ac));
            });
            Wire(tree.Q<Button>("dev-add-item"), () =>
            {
                SetStatus(DMDevCommandState.AddItem(itemField != null ? itemField.value : string.Empty, ParseQty()));
            });
            Wire(tree.Q<Button>("dev-remove-item"), () =>
            {
                SetStatus(DMDevCommandState.RemoveItem(itemField != null ? itemField.value : string.Empty, ParseQty()));
            });
            Wire(tree.Q<Button>("dev-clear-inv"), () => SetStatus(DMDevCommandState.ClearInventory()));
            Wire(tree.Q<Button>("dev-despawn"), () => SetStatus(DMDevCommandState.DespawnNearbyPickups()));
            Wire(tree.Q<Button>("dev-refresh"), RefreshAll);
            Wire(tree.Q<Button>("dev-save0"), () =>
            {
                bool ok = GameSaveSystem.TrySave(0, out string message);
                SetStatus(message);
                RefreshAll();
                if (!ok)
                    return;
            });
            Wire(tree.Q<Button>("dev-load0"), () =>
            {
                bool ok = GameSaveSystem.TryLoad(0, out string message);
                SetStatus(message);
                RefreshAll();
                if (!ok)
                    return;
            });
            Wire(tree.Q<Button>("dev-reset-new"), () =>
            {
                PlayerProgressionManager.EnsureExists()?.ResetToNewGame();
                SetStatus("Reset live progression to level 5 / 25 SP.");
                RefreshLive();
            });
            Wire(tree.Q<Button>("dev-clear-log"), () =>
            {
                DMSaveLoadRegistry.ClearUnpinned();
                selectedId = null;
                SetStatus("Cleared unpinned registry rows.");
            });
            Wire(tree.Q<Button>("dev-note-save"), () =>
            {
                if (DMSaveLoadRegistry.TrySetNote(selectedId, noteField != null ? noteField.value : string.Empty))
                    SetStatus("Note written.");
                else
                    SetStatus("Select a registry row first.");
            });
            Wire(tree.Q<Button>("dev-pin"), () =>
            {
                SetStatus(DMSaveLoadRegistry.TrySetPinned(selectedId, true) ? "Pinned." : "Select a row.");
            });
            Wire(tree.Q<Button>("dev-unpin"), () =>
            {
                SetStatus(DMSaveLoadRegistry.TrySetPinned(selectedId, false) ? "Unpinned." : "Select a row.");
            });
            Wire(tree.Q<Button>("dev-forget"), () =>
            {
                if (DMSaveLoadRegistry.TryRemove(selectedId))
                {
                    selectedId = null;
                    SetStatus("Forgot registry row.");
                }
                else
                    SetStatus("Select a row.");
            });
            Wire(tree.Q<Button>("dev-close"), HideInternal);

            VisualElement veil = tree.Q<VisualElement>("dev-veil");
            if (veil != null)
            {
                veil.UnregisterCallback<PointerDownEvent>(OnVeil);
                veil.RegisterCallback<PointerDownEvent>(OnVeil);
            }

            DMSaveLoadRegistry.Changed -= RefreshRegistry;
            DMSaveLoadRegistry.Changed += RefreshRegistry;
            HideInternal();
        }

        private void OnVeil(PointerDownEvent evt)
        {
            evt.StopPropagation();
            HideInternal();
        }

        private void TogglePause()
        {
            DMDevCommandState.GamePaused = !GameplayMenuTime.HasPauseReason(GameplayMenuTime.ReasonDevPanel);
            GameplayMenuTime.SetPause(GameplayMenuTime.ReasonDevPanel, DMDevCommandState.GamePaused);
            SetStatus(DMDevCommandState.GamePaused ? "Game paused." : "Game unpaused.");
            RefreshToggles();
        }

        private void ToggleCursor()
        {
            DMDevCommandState.UnlockCursor = !DMDevCommandState.UnlockCursor;
            PlayerLocator.FindPlayerController()?.ApplyCursorState();
            if (DMDevCommandState.UnlockCursor)
            {
                UnityEngine.Cursor.lockState = CursorLockMode.None;
                UnityEngine.Cursor.visible = true;
            }

            SetStatus(DMDevCommandState.UnlockCursor ? "Cursor unlocked." : "Cursor lock restored.");
            RefreshToggles();
        }

        private static void Wire(Button button, Action action)
        {
            if (button == null || action == null)
                return;

            button.clicked -= action.Invoke;
            button.clicked += action.Invoke;
        }

        private int ParseQty()
        {
            return int.TryParse(qtyField != null ? qtyField.value : "1", out int qty) ? Mathf.Max(1, qty) : 1;
        }

        private void ShowInternal()
        {
            BindTree();
            open = true;
            RefreshAll();
            DMUiToolkitOverlayDocument.SetShown(root, true);
            DMUiToolkitOverlayDocument.PromoteInteractiveOverlay(document);
            PlayerLocator.FindPlayerController()?.ApplyCursorState();
        }

        private void HideInternal()
        {
            open = false;
            if (root != null)
                DMUiToolkitOverlayDocument.SetShown(root, false);
            PlayerLocator.FindPlayerController()?.ApplyCursorState();
        }

        private void RefreshAll()
        {
            RefreshLive();
            RefreshToggles();
            RefreshRegistry();
        }

        private void RefreshLive()
        {
            PlayerProgressionManager progression = PlayerProgressionManager.EnsureExists();
            if (liveLabel != null)
            {
                liveLabel.text = progression != null
                    ? $"Live: Lv {progression.Level}  XP {progression.CurrentXp}  SP {progression.UnspentSkillPoints}  God {(DMDevCommandState.GodMode ? "ON" : "off")}  Pause {(DMDevCommandState.GamePaused ? "ON" : "off")}  Cursor {(DMDevCommandState.UnlockCursor ? "FREE" : "lock")}"
                    : "Live: no progression manager";
            }

            if (slot0Label != null)
            {
                SaveSlotInfo info = GameSaveSystem.GetSlotInfo(0);
                slot0Label.text = info.HasData
                    ? $"Continue slot 1: {info.GetSummaryLine()}"
                    : "Continue slot 1: empty (New Game will not load an old file)";
            }
        }

        private void RefreshToggles()
        {
            SetToggle(godButton, DMDevCommandState.GodMode);
            SetToggle(stamButton, DMDevCommandState.InfiniteStamina);
            SetToggle(energyButton, DMDevCommandState.InfiniteEnergy);
            SetToggle(oxyButton, DMDevCommandState.InfiniteOxygen);
            SetToggle(pauseButton, DMDevCommandState.GamePaused);
            SetToggle(cursorButton, DMDevCommandState.UnlockCursor);
        }

        private static void SetToggle(Button button, bool on)
        {
            if (button == null)
                return;

            button.EnableInClassList("dmg-dev-btn--on", on);
        }

        private void RefreshRegistry()
        {
            if (registryView == null)
                return;

            registryView.Clear();
            var records = DMSaveLoadRegistry.Records;
            for (int i = 0; i < records.Count; i++)
            {
                DMSaveLoadRecord record = records[i];
                if (record == null)
                    continue;

                string id = record.id;
                VisualElement row = new VisualElement { pickingMode = PickingMode.Position };
                row.AddToClassList("dmg-dev-reg-row");
                if (id == selectedId)
                    row.AddToClassList("dmg-dev-reg-row--sel");

                Label text = new Label(DMSaveLoadRegistry.FormatLine(record))
                {
                    pickingMode = PickingMode.Ignore
                };
                text.AddToClassList("dmg-dev-reg-text");
                row.Add(text);
                row.RegisterCallback<ClickEvent>(_ =>
                {
                    selectedId = id;
                    if (noteField != null)
                        noteField.value = record.note ?? string.Empty;
                    RefreshRegistry();
                });
                registryView.Add(row);
            }
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null)
                statusLabel.text = message ?? string.Empty;
            RefreshLive();
        }
    }
}
