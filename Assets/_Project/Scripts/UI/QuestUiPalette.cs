using Project.Quests;
using UnityEngine;

namespace Project.UI
{
    public static class QuestUiPalette
    {
        public static readonly Color InProgressText = new Color(0.62f, 0.84f, 1f, 1f);
        public static readonly Color InProgressBackground = new Color(0.12f, 0.2f, 0.3f, 0.92f);

        public static readonly Color ReadyToTurnInText = new Color(0.95f, 0.78f, 0.28f, 1f);
        public static readonly Color ReadyToTurnInBackground = new Color(0.42f, 0.34f, 0.08f, 0.72f);

        public static readonly Color TurnedInText = new Color(0.58f, 0.9f, 0.62f, 1f);
        public static readonly Color TurnedInBackground = new Color(0.12f, 0.24f, 0.14f, 0.85f);

        public static readonly Color AvailableText = new Color(0.88f, 0.92f, 0.96f, 0.95f);
        public static readonly Color MutedText = new Color(0.62f, 0.66f, 0.72f, 0.85f);

        public static Color GetTitleColor(QuestStatus status, ShiftUiTheme theme)
        {
            return status switch
            {
                QuestStatus.Active => InProgressText,
                QuestStatus.Completed => ReadyToTurnInText,
                QuestStatus.TurnedIn => TurnedInText,
                QuestStatus.Available => theme != null ? theme.secondaryTextColor : AvailableText,
                _ => MutedText
            };
        }

        public static Color GetStatusLabelColor(QuestStatus status, ShiftUiTheme theme)
        {
            return GetTitleColor(status, theme);
        }

        public static Color GetRowBackgroundColor(QuestStatus status, bool selected, ShiftUiTheme theme)
        {
            if (selected)
            {
                return theme != null
                    ? new Color(theme.primaryColor.r, theme.primaryColor.g, theme.primaryColor.b, 0.28f)
                    : new Color(0.2f, 0.35f, 0.5f, 1f);
            }

            return status switch
            {
                QuestStatus.Active => InProgressBackground,
                QuestStatus.Completed => ReadyToTurnInBackground,
                QuestStatus.TurnedIn => TurnedInBackground,
                _ => new Color(0.16f, 0.18f, 0.22f, 0.95f)
            };
        }

        public static Color GetObjectiveTextColor(bool complete, QuestStatus questStatus, ShiftUiTheme theme)
        {
            if (questStatus == QuestStatus.Completed)
                return ReadyToTurnInText;

            if (complete)
                return TurnedInText;

            return InProgressText;
        }

        public static string GetStatusLabel(QuestStatus status)
        {
            return status switch
            {
                QuestStatus.Available => "Available",
                QuestStatus.Active => "In Progress",
                QuestStatus.Completed => "Ready to turn in",
                QuestStatus.TurnedIn => "Completed",
                _ => "Locked"
            };
        }
    }
}
