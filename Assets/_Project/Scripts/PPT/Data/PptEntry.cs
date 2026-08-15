using Project.Survival.World;
using UnityEngine;

namespace Project.PPT
{
    [CreateAssetMenu(
        fileName = "ppt_entry",
        menuName = "Dark Matter: Genesis/PPT/Entry")]
    public class PptEntry : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string pptId;
        [SerializeField] private PptType type = PptType.Place;
        [SerializeField] private string displayName = "Unknown";
        [SerializeField] private string[] aliases;
        [SerializeField] private string[] keywordTags;

        [Header("World Links")]
        [SerializeField] private string mapMarkerDiscoveryId;
        [SerializeField] private string questLocationId;
        [SerializeField] private string npcId;
        [SerializeField] private Vector3 authoredWorldPosition;
        [SerializeField] private bool hasAuthoredWorldPosition;
        [SerializeField] private IoSurfaceRegionId surfaceRegion = IoSurfaceRegionId.None;
        [SerializeField] private string exposureZoneTag;

        [Header("Thing")]
        [SerializeField] private string questObjectiveTargetId;

        [Header("Discovery")]
        [SerializeField] private bool requiresDiscovery = true;

        public string PptId => string.IsNullOrWhiteSpace(pptId) ? name : pptId;
        public PptType Type => type;
        public string DisplayName => displayName;
        public string[] Aliases => aliases;
        public string[] KeywordTags => keywordTags;
        public string MapMarkerDiscoveryId => mapMarkerDiscoveryId;
        public string QuestLocationId => questLocationId;
        public string NpcId => npcId;
        public bool HasAuthoredWorldPosition => hasAuthoredWorldPosition;
        public Vector3 AuthoredWorldPosition => authoredWorldPosition;
        public IoSurfaceRegionId SurfaceRegion => surfaceRegion;
        public string ExposureZoneTag => exposureZoneTag;
        public string QuestObjectiveTargetId => questObjectiveTargetId;
        public bool RequiresDiscovery => requiresDiscovery;

        public bool MatchesAlias(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return false;

            if (string.Equals(displayName, query, System.StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(PptId, query, System.StringComparison.OrdinalIgnoreCase))
                return true;

            if (aliases == null)
                return false;

            for (int i = 0; i < aliases.Length; i++)
            {
                if (string.Equals(aliases[i], query, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static PptEntry CreateRuntime(
            string id,
            string label,
            PptType entryType,
            string markerDiscoveryId,
            Vector3 position,
            string linkedNpcId = "")
        {
            PptEntry entry = CreateInstance<PptEntry>();
            entry.pptId = id;
            entry.displayName = label;
            entry.type = entryType;
            entry.mapMarkerDiscoveryId = markerDiscoveryId;
            entry.authoredWorldPosition = position;
            entry.hasAuthoredWorldPosition = true;
            entry.npcId = linkedNpcId;
            entry.requiresDiscovery = !string.IsNullOrWhiteSpace(markerDiscoveryId);
            entry.hideFlags = HideFlags.DontSave;
            return entry;
        }
    }
}
