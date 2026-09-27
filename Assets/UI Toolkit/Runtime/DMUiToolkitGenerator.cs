using Project.Building;
using Project.Core;
using Project.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// 0926-generator: Press E at a base generator. Shows tank %, units, burn rate, time left and the Plasma Fuel
    /// available in the inventory and in storage crates within range, with Add 1 / Add 10 / Fill Tank.
    /// </summary>
    [DefaultExecutionOrder(-365)]
    [DisallowMultipleComponent]
    public class DMUiToolkitGenerator : MonoBehaviour
    {
        public const string DocumentName = "UITK_Generator";
        public const string UxmlPath = "Assets/UI Toolkit/Screens/Generator.uxml";
        public const string UssPath = "Assets/UI Toolkit/Screens/Generator.uss";
        public const string PauseReason = "BaseGenerator";

        private static DMUiToolkitGenerator instance;

        private UIDocument document;
        private VisualElement root;
        private Label percentLabel;
        private VisualElement fill;
        private Label unitsLabel;
        private Label burnLabel;
        private Label leftLabel;
        private Label zoneLabel;
        private Label stateLabel;
        private Label invLabel;
        private Label cratesLabel;
        private Label roomLabel;
        private Label statusLabel;
        private Button add1;
        private Button add10;
        private Button fillButton;
        private bool bound;
        private bool open;
        private float nextRefresh;
        private DMBaseGenerator generator;

        public static bool IsOpen => instance != null && instance.open;

        public static DMUiToolkitGenerator EnsureHost()
        {
            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(
                DocumentName, UxmlPath, UssPath, DMUiToolkitOverlayDocument.CrateSort);
            if (doc == null)
                return null;

            DMUiToolkitGenerator host = doc.GetComponent<DMUiToolkitGenerator>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitGenerator>();
            host.document = doc;
            host.BindTree();
            return host;
        }

        public static bool TryShow(DMBaseGenerator target)
        {
            if (target == null)
                return false;
            DMUiToolkitGenerator host = EnsureHost();
            if (host == null)
                return false;
            host.ShowInternal(target);
            return true;
        }

        public static bool TryHide()
        {
            if (instance == null || !instance.open)
                return false;
            instance.HideInternal();
            return true;
        }

        public static bool TryHandleBack()
        {
            return TryHide();
        }

        private void OnEnable()
        {
            instance = this;
            BindTree();
            DMBaseGenerator.Changed += Refresh;
        }

        private void OnDisable()
        {
            DMBaseGenerator.Changed -= Refresh;
            if (open)
                ApplyOverlaySession(false);
            open = false;
            if (instance == this)
                instance = null;
        }

        private void BindTree()
        {
            if (bound || document == null)
                return;
            root = document.rootVisualElement;
            if (root == null)
                return;

            percentLabel = root.Q<Label>("gen-percent");
            fill = root.Q("gen-fill");
            unitsLabel = root.Q<Label>("gen-units");
            burnLabel = root.Q<Label>("gen-burn");
            leftLabel = root.Q<Label>("gen-left");
            zoneLabel = root.Q<Label>("gen-zone");
            stateLabel = root.Q<Label>("gen-state");
            invLabel = root.Q<Label>("gen-inv");
            cratesLabel = root.Q<Label>("gen-crates");
            roomLabel = root.Q<Label>("gen-room");
            statusLabel = root.Q<Label>("gen-status");
            add1 = root.Q<Button>("gen-add1");
            add10 = root.Q<Button>("gen-add10");
            fillButton = root.Q<Button>("gen-fill-btn");

            Button close = root.Q<Button>("gen-close");
            if (close != null)
                close.clicked += () => TryHide();
            if (add1 != null)
                add1.clicked += () => DoRefuel(1);
            if (add10 != null)
                add10.clicked += () => DoRefuel(10);
            if (fillButton != null)
                fillButton.clicked += () => DoRefuel(int.MaxValue);
            VisualElement veil = root.Q("gen-veil");
            if (veil != null)
                veil.RegisterCallback<PointerDownEvent>(_ => TryHide());

            bound = true;
            DMUiToolkitOverlayDocument.SetShown(root, false);
        }

        private void ShowInternal(DMBaseGenerator target)
        {
            BindTree();
            generator = target;
            open = true;
            if (statusLabel != null)
                statusLabel.text = string.Empty;
            if (root != null)
                DMUiToolkitOverlayDocument.SetShown(root, true);
            DMUiToolkitOverlayDocument.PromoteInteractiveOverlay(document);
            ApplyOverlaySession(true);
            Refresh();
            if (fillButton != null)
                fillButton.Focus();
        }

        private void HideInternal()
        {
            open = false;
            generator = null;
            if (root != null)
                DMUiToolkitOverlayDocument.SetShown(root, false);
            ApplyOverlaySession(false);
        }

        private static void ApplyOverlaySession(bool overlayOpen)
        {
            GameplayMenuTime.SetPause(PauseReason, overlayOpen);
            PlayerController player = PlayerLocator.FindPlayerController();
            if (player == null)
                return;
            player.SetGameplayPaused(overlayOpen);
            if (overlayOpen)
            {
                UnityEngine.Cursor.lockState = CursorLockMode.None;
                UnityEngine.Cursor.visible = true;
            }

            player.ApplyCursorState();
        }

        private void Update()
        {
            if (!open)
                return;
            if (generator == null)
            {
                HideInternal();
                return;
            }

            if (Time.unscaledTime >= nextRefresh)
                Refresh();
        }

        private void DoRefuel(int requested)
        {
            if (generator == null)
                return;
            if (generator.Room <= 0)
            {
                SetStatus("The tank is full.");
                return;
            }

            int loaded = generator.Refuel(requested, out int fromInventory, out int fromCrates);
            if (loaded <= 0)
            {
                SetStatus("No Plasma Fuel in your inventory or in storage within "
                    + Mathf.RoundToInt(DMBuildingGhostProfile.GeneratorFuelRangeMeters) + " m.");
            }
            else
            {
                string where = fromCrates <= 0 ? "from your inventory"
                    : fromInventory <= 0 ? "from storage"
                    : fromInventory + " from your inventory, " + fromCrates + " from storage";
                SetStatus("Loaded " + loaded + " Plasma Fuel (" + where + ").");
            }

            Refresh();
        }

        private void SetStatus(string text)
        {
            if (statusLabel != null)
                statusLabel.text = text;
        }

        private void Refresh()
        {
            if (!open || generator == null)
                return;
            nextRefresh = Time.unscaledTime + 0.5f;

            int capacity = generator.Capacity;
            int units = Mathf.CeilToInt(generator.Fuel - 0.0001f);
            if (percentLabel != null)
                percentLabel.text = generator.Percent + "%";
            if (fill != null)
                DMUiToolkitOverlayDocument.SetFillPercent(fill, generator.Fill01);
            if (unitsLabel != null)
                unitsLabel.text = "Tank: " + units + " / " + capacity + " units";
            generator.RefreshLoad();
            if (burnLabel != null)
            {
                float period = DMBuildingGhostProfile.PowerLoadPeriodMinutes;
                float perPeriod = generator.UnitsPerSecond * period * 60f;
                burnLabel.text = "Burn: 1 unit every " + FormatMinutes(DMBuildingGhostProfile.GeneratorMinutesPerUnit)
                    + " + load " + FormatUnits(generator.LoadUnitsPerPeriod) + " per " + FormatMinutes(period)
                    + " (" + generator.LoadForceFields + (generator.LoadForceFields == 1 ? " force field, " : " force fields, ")
                    + generator.LoadItems + (generator.LoadItems == 1 ? " powered item" : " powered items") + ")"
                    + "\nTotal: " + FormatUnits(perPeriod) + " units per " + FormatMinutes(period);
            }
            if (leftLabel != null)
                leftLabel.text = generator.IsRunning ? "Time left: " + FormatDuration(generator.SecondsLeft) : "Time left: empty";

            bool inBase = DMBasePower.IsInsideAnyBase(generator.transform.position);
            int square = Mathf.RoundToInt(DMBuildingGhostProfile.BasePowerSquareMeters);
            if (zoneLabel != null)
            {
                zoneLabel.text = inBase
                    ? "Powers force fields, lights and equipment in the " + square + " m base area."
                    : "Not inside a base area. Place it within " + (square / 2) + " m of the base's first foundation.";
            }

            if (stateLabel != null)
            {
                stateLabel.text = !generator.IsRunning ? "OFFLINE - NO FUEL" : inBase ? "ONLINE" : "RUNNING - NO BASE";
                stateLabel.EnableInClassList("dmg-gen-offline", !generator.IsRunning || !inBase);
            }

            int inv = generator.CountInventoryFuel();
            int crates = generator.CountCrateFuel();
            int crateCount = generator.CratesInRangeCount();
            int range = Mathf.RoundToInt(DMBuildingGhostProfile.GeneratorFuelRangeMeters);
            if (invLabel != null)
                invLabel.text = "Plasma Fuel in inventory: " + inv;
            if (cratesLabel != null)
                cratesLabel.text = "Plasma Fuel in storage (" + crateCount + (crateCount == 1 ? " crate" : " crates") + " within " + range + " m): " + crates;
            int room = generator.Room;
            if (roomLabel != null)
                roomLabel.text = room > 0 ? "Room in tank: " + room + " units" : "Tank is full";

            bool canLoad = room > 0 && inv + crates > 0;
            add1?.SetEnabled(canLoad);
            add10?.SetEnabled(canLoad);
            fillButton?.SetEnabled(canLoad);
        }

        private static string FormatUnits(float units)
        {
            return Mathf.Approximately(units, Mathf.Round(units)) ? Mathf.RoundToInt(units).ToString() : units.ToString("0.#");
        }

        private static string FormatMinutes(float minutes)
        {
            if (Mathf.Approximately(minutes, 1f))
                return "minute";
            return (Mathf.Approximately(minutes, Mathf.Round(minutes)) ? Mathf.RoundToInt(minutes).ToString() : minutes.ToString("0.#")) + " minutes";
        }

        private static string FormatDuration(float seconds)
        {
            int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
            int hours = total / 3600;
            int minutes = (total % 3600) / 60;
            if (hours > 0)
                return hours + "h " + minutes.ToString("00") + "m";
            int secs = total % 60;
            return minutes + "m " + secs.ToString("00") + "s";
        }
    }
}
