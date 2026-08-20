using Project.Survival.World;
using UnityEngine;

namespace Project.PPT
{
    [System.Flags]
    public enum PptTalkOptions
    {
        None = 0,
        QuestBoard = 1 << 0,
        Shop = 1 << 1,
        Conversation = 1 << 2,
        Directions = 1 << 3
    }

    [CreateAssetMenu(
        fileName = "ppt_npc_profile",
        menuName = "Dark Matter: Genesis/PPT/NPC Profile")]
    public class PptNpcProfile : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string npcId;
        [SerializeField] private string displayName;

        [Header("Talk Hub")]
        [SerializeField] private PptTalkOptions talkOptions =
            PptTalkOptions.QuestBoard | PptTalkOptions.Directions;

        [Header("Directions")]
        [SerializeField, Range(2, 3)] private int directionChoicesPerPage = 3;
        [SerializeField] private PptKnowledgeScope knowledgeScope = PptKnowledgeScope.Regional;
        [SerializeField] private string[] knownPptIds;
        [SerializeField] private string[] excludedPptIds;
        [SerializeField] private IoSurfaceRegionId homeRegion = IoSurfaceRegionId.None;
        [SerializeField, Range(0f, 1f)] private float preciseDirectionChance = 0.25f;
        [SerializeField, Range(0f, 1f)] private float unknownChance = 0.15f;
        [SerializeField] private bool preferReferToOtherNpc = true;
        [SerializeField] private string[] referNpcIds;
        [SerializeField] private PptDirectionPhraseSet phraseSet;

        [Header("Gesture")]
        [SerializeField] private PptPointGestureMode pointGestureMode = PptPointGestureMode.FullBody;
        [SerializeField] private string idleStateName = "Idle";
        [SerializeField] private string pointStateName = "Point";
        [SerializeField] private string upperBodyPointStateName = "Point";
        [SerializeField] private string shrugStateName = "Shrug";
        [SerializeField] private string upperBodyLayerName = "Upper Body";
        [SerializeField] private bool rotateVisualTowardBearing = true;
        [SerializeField, Min(0.05f)] private float gestureCrossFadeSeconds = 0.15f;

        public string NpcId => npcId;
        public string DisplayName => displayName;
        public PptTalkOptions TalkOptions => talkOptions;
        public int DirectionChoicesPerPage => Mathf.Clamp(directionChoicesPerPage, 2, 3);
        public PptKnowledgeScope KnowledgeScope => knowledgeScope;
        public string[] KnownPptIds => knownPptIds;
        public string[] ExcludedPptIds => excludedPptIds;
        public IoSurfaceRegionId HomeRegion => homeRegion;
        public float PreciseDirectionChance => preciseDirectionChance;
        public float UnknownChance => unknownChance;
        public bool PreferReferToOtherNpc => preferReferToOtherNpc;
        public string[] ReferNpcIds => referNpcIds;
        public PptDirectionPhraseSet PhraseSet => phraseSet;
        public PptPointGestureMode PointGestureMode => pointGestureMode;
        public string IdleStateName => idleStateName;
        public string PointStateName => pointStateName;
        public string UpperBodyPointStateName => upperBodyPointStateName;
        public string ShrugStateName => shrugStateName;
        public string UpperBodyLayerName => upperBodyLayerName;
        public bool RotateVisualTowardBearing => rotateVisualTowardBearing;
        public float GestureCrossFadeSeconds => gestureCrossFadeSeconds;

        public bool HasTalkOption(PptTalkOptions option)
        {
            return (talkOptions & option) != 0;
        }
    }
}
