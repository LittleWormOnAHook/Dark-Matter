using UnityEngine;

namespace Project.UI
{
    /// <summary>2026-10-25i: controller Journal schema tuning (hold thresholds, pad move snap, ghost).</summary>
    [CreateAssetMenu(menuName = "Dark Matter Genesis/UI/Journal Pad Profile", fileName = "DMJournalPadProfile")]
    public class DMJournalPadProfile : ScriptableObject
    {
        public const string ResourcePath = "UI/Controls/DMJournalPadProfile";

        [Header("Tabs")]
        [Tooltip("Gamepad: D-pad/stick only focuses Journal tabs; A opens the focused tab. Off = old auto-open on focus.")]
        public bool tabsRequireA = true;
        [Tooltip("25m: console log [JournalPad] for pad input received + focus target (diagnostics).")]
        public bool padDebugLog = true;

        [Header("Navigation feel (25n)")]
        [Tooltip("Left stick deflection needed to step focus.")]
        [Range(0.2f, 0.95f)] public float navStickDeadzone = 0.6f;
        [Tooltip("Stick must fall below this before another fresh step (return-to-center re-arm).")]
        [Range(0.05f, 0.8f)] public float navStickRearm = 0.3f;
        [Tooltip("Hold stick: delay before auto-repeat starts (s).")]
        [Range(0.15f, 1.5f)] public float navInitialDelay = 0.5f;
        [Tooltip("Hold stick: seconds between repeated steps.")]
        [Range(0.05f, 1f)] public float navRepeatRate = 0.25f;
        [Tooltip("25n: left stick also navigates the journal (off = right stick + D-pad only, native UI navigate).")]
        public bool navUseLeftStick = false;
        [Tooltip("25n: our own D-pad/stick poll as well as the native UI Navigate (off avoids double steps).")]
        public bool navDirectPoll = false;

        [Header("Inventory hold thresholds (seconds)")]
        [Tooltip("Hold A on an item this long to pick it up (ghost move). Shorter tap = old A behaviour.")]
        [Range(0.1f, 1.5f)] public float holdMoveSeconds = 0.3f;
        [Tooltip("Hold X on an item this long to open its context menu. Shorter tap = Use.")]
        [Range(0.1f, 1.5f)] public float holdContextSeconds = 0.35f;

        [Header("Pad focus highlight (25k)")]
        [Tooltip("Border colour of the pad-focused element (distinct from the gold selected-hotbar border).")]
        public Color focusColor = new Color(0.35f, 0.9f, 1f, 1f);
        [Range(1f, 6f)] public float focusBorderWidth = 3f;
        [Tooltip("Pop scale of the focused element.")]
        [Range(1f, 1.2f)] public float focusScale = 1.05f;

        [Header("Move / drop")]
        [Tooltip("Ghost drops into the nearest empty slot within this many slots (0 = only the slot under the ghost). Otherwise the item returns.")]
        [Range(0, 4)] public int snapRadiusSlots = 1;
        [Range(0.15f, 1f)] public float ghostOpacity = 0.6f;
        public Color targetHighlight = new Color(1f, 0.82f, 0.25f, 1f);
        [Range(1f, 6f)] public float targetBorderWidth = 3f;

        private static DMJournalPadProfile cached;
        private static DMJournalPadProfile fallback;

        public static DMJournalPadProfile Active
        {
            get
            {
                if (cached != null) return cached;
                cached = Resources.Load<DMJournalPadProfile>(ResourcePath);
                if (cached != null) return cached;
                if (fallback == null) fallback = CreateInstance<DMJournalPadProfile>();
                return fallback;
            }
        }

        /// <summary>0 Subtle, 1 Default, 2 Extreme.</summary>
        public void ApplyPadPreset(int preset)
        {
            switch (preset)
            {
                case 0: navStickDeadzone = 0.7f; navStickRearm = 0.25f; navInitialDelay = 0.65f; navRepeatRate = 0.35f; focusBorderWidth = 2f; focusScale = 1.02f; holdMoveSeconds = 0.45f; holdContextSeconds = 0.5f; snapRadiusSlots = 0; ghostOpacity = 0.45f; targetBorderWidth = 2f; break;
                case 2: navStickDeadzone = 0.4f; navStickRearm = 0.2f; navInitialDelay = 0.3f; navRepeatRate = 0.12f; focusBorderWidth = 5f; focusScale = 1.1f; holdMoveSeconds = 0.15f; holdContextSeconds = 0.2f; snapRadiusSlots = 3; ghostOpacity = 0.85f; targetBorderWidth = 5f; break;
                default: navStickDeadzone = 0.6f; navStickRearm = 0.3f; navInitialDelay = 0.5f; navRepeatRate = 0.25f; focusBorderWidth = 3f; focusScale = 1.05f; holdMoveSeconds = 0.3f; holdContextSeconds = 0.35f; snapRadiusSlots = 1; ghostOpacity = 0.6f; targetBorderWidth = 3f; break;
            }
            tabsRequireA = true;
        }
    }
}
