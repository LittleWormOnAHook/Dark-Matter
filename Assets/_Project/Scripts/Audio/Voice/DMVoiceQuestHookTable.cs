using System.Collections.Generic;
using UnityEngine;

namespace Project.Audio.Voice
{
    /// <summary>2026-11-03o. Reusable quest-event -> voice mapping, assigned to one or more DMVoiceQuestHook components.</summary>
    [CreateAssetMenu(menuName = "Dark Matter/Voice/Quest Hook Table", fileName = "DMVoiceQuestHookTable")]
    public sealed class DMVoiceQuestHookTable : ScriptableObject
    {
        public List<DMVoiceQuestHookEntry> entries = new List<DMVoiceQuestHookEntry>();
    }
}
