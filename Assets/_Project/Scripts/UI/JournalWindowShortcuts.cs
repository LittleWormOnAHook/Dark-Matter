namespace Project.UI
{
    /// <summary>
    /// Default keyboard shortcuts for journal fullscreen tabs (Player input actions).
    /// </summary>
    public static class JournalWindowShortcuts
    {
        public static char? GetShortcutKey(JournalWindowId windowId)
        {
            switch (windowId)
            {
                case JournalWindowId.JournalQuest: return 'J';
                case JournalWindowId.Inventory: return 'I';
                case JournalWindowId.Map: return 'M';
                case JournalWindowId.Pet: return 'F';
                case JournalWindowId.Pioneers: return 'P';
                case JournalWindowId.Character: return 'U';
                case JournalWindowId.Recipes: return 'B';
                case JournalWindowId.Skills: return 'T';
                // Craft tab removed from journal rail — C still opens Blueprints; primary shortcut is B.
                case JournalWindowId.Craft: return null;
                case JournalWindowId.Echoes: return 'L';
                case JournalWindowId.Achievements: return 'A';
                default: return null;
            }
        }

        public static string FormatTabLabel(string label, JournalWindowId windowId)
        {
            char? key = GetShortcutKey(windowId);
            return key.HasValue ? $"{label} ({key.Value})" : label;
        }
    }
}
