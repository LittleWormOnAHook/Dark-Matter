using Project.Quests;
using UnityEngine;

namespace Project.UI
{
    public static class QuestUiPalette
    {
        public static readonly Color InProgressText = SurvivalPioneerUiPalette.RichFuchsia;
        public static readonly Color InProgressBackground = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.SlateGray, 0.92f);

        public static readonly Color ReadyToTurnInText = SurvivalPioneerUiPalette.Gold;
        public static readonly Color ReadyToTurnInBackground = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.RichFuchsia, 0.72f);

        public static readonly Color TurnedInText = SurvivalPioneerUiPalette.PositiveGreen;
        public static readonly Color TurnedInBackground = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.CharcoalGray, 0.85f);

        public static readonly Color AvailableText = SurvivalPioneerUiPalette.BodyText;
        public static readonly Color MutedText = SurvivalPioneerUiPalette.MutedText;

        public static Color GetTitleColor(QuestStatus status, ShiftUiTheme theme)
        {
            return status switch
            {
                QuestStatus.Active => SurvivalPioneerUiPalette.WarmOffWhite,
                QuestStatus.Completed => ReadyToTurnInText,
                QuestStatus.TurnedIn => TurnedInText,
                QuestStatus.Available => theme != null ? theme.secondaryTextColor : AvailableText,
                _ => MutedText
            };
        }

        public static Color GetStatusLabelColor(QuestStatus status, ShiftUiTheme theme)
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

        public static Color GetRowBackgroundColor(QuestStatus status, bool selected, ShiftUiTheme theme)
        {
            if (selected)
            {
                return theme != null
                    ? SurvivalPioneerUiPalette.WithAlpha(theme.primaryColor, 0.28f)
                    : SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.RichFuchsia, 0.28f);
            }

            return status switch
            {
                QuestStatus.Active => InProgressBackground,
                QuestStatus.Completed => ReadyToTurnInBackground,
                QuestStatus.TurnedIn => TurnedInBackground,
                _ => SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.CharcoalGray, 0.95f)
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
