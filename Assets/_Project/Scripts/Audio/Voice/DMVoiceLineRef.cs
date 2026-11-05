using System;
using UnityEngine;

namespace Project.Audio.Voice
{
    /// <summary>
    /// Designer-friendly line reference: the line set asset plus the stable line id.
    /// The id survives reordering, re-bakes and clip regeneration; the drawer picks it from a popup.
    /// </summary>
    [Serializable]
    public struct DMVoiceLineRef
    {
        public DMVoiceLineSet set;
        public string lineId;

        public bool IsSet => set != null && !string.IsNullOrEmpty(lineId);

        /// <summary>Save / dedupe key: set guid (name fallback) + ':' + line id.</summary>
        public string Key => IsSet ? set.SetKey + ":" + lineId : string.Empty;

        public DMVoiceLine Line => ResolveLine();

        public DMVoiceLine ResolveLine()
        {
            if (set == null || set.lines == null || string.IsNullOrEmpty(lineId))
                return null;

            for (int i = 0; i < set.lines.Count; i++)
            {
                DMVoiceLine line = set.lines[i];
                if (line != null && line.id == lineId)
                    return line;
            }

            return null;
        }

        public override string ToString()
        {
            return IsSet ? set.name + "/" + lineId : "(no voice line)";
        }
    }
}
