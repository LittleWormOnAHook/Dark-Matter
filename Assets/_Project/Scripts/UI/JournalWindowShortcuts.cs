namespace Project.UI
{
    /// <summary>
    /// Journal keyboard shortcuts: only J (journal hub) and I (inventory) are direct hotkeys.
    /// Other tabs are opened from the journal UI (or gamepad nav) — no letter hints on tab labels.
    /// </summary>
    public static class JournalWindowShortcuts
    {
        public static char? GetShortcutKey(JournalWindowId windowId)
        {
            switch (windowId)
            {
                case JournalWindowId.JournalQuest: return 'J';
                case JournalWindowId.Inventory: return 'I';
                default: return null;
            }
        }

        public static string FormatTabLabel(string label, JournalWindowId windowId) => label;
    }
}
