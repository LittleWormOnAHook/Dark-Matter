#if UNITY_EDITOR
using Project.UI;
using UnityEngine;

namespace Project.EditorTools
{
    internal static class DMToolsSectionVisual
    {
        public static string GetIcon(string title)
        {
            return title switch
            {
                "Profiles" => "♡",
                "Authoring" => "⚙",
                "Player" => "◆",
                "Companions & Pets" => "◇",
                "Combat" => "✦",
                "World & Scene" => "◎",
                "HDRP" => "◈",
                "UI" => "▣",
                "Audio & Optics" => "♫",
                "Content seeds" => "★",
                "Debug (Play Mode)" => "⚡",
                "Play Mode Saver" => "◉",
                "Maintenance" => "⚒",
                "Legacy" => "⧖",
                _ => "•"
            };
        }

        public static Color GetAccent(string title)
        {
            return title switch
            {
                "Profiles" => FromHex("#D4A017"),
                "Authoring" => FromHex("#C02E7A"),
                "Player" => FromHex("#C02E7A"),
                "Companions & Pets" => FromHex("#4A4A5A"),
                "Combat" => FromHex("#8F1E5E"),
                "World & Scene" => FromHex("#D4A017"),
                "HDRP" => FromHex("#4A4A5A"),
                "UI" => FromHex("#EDE9E4"),
                "Audio & Optics" => FromHex("#8C7F75"),
                "Content seeds" => FromHex("#4A4A5A"),
                "Debug (Play Mode)" => FromHex("#8F1E5E"),
                "Play Mode Saver" => DarkMatterGenesisUiPalette.PositiveGreen,
                "Maintenance" => FromHex("#4A4A5A"),
                "Legacy" => FromHex("#2F2F2F"),
                _ => FromHex("#C02E7A")
            };
        }

        private static Color FromHex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.white;
        }
    }
}
#endif
