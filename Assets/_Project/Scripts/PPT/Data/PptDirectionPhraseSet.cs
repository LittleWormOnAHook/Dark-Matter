using UnityEngine;

namespace Project.PPT
{
    [CreateAssetMenu(
        fileName = "ppt_direction_phrases",
        menuName = "Dark Matter: Genesis/PPT/Direction Phrases")]
    public class PptDirectionPhraseSet : ScriptableObject
    {
        [TextArea(1, 2)]
        [SerializeField] private string[] northPhrases = { "north of here", "to the north" };
        [TextArea(1, 2)]
        [SerializeField] private string[] southPhrases = { "south of here", "to the south" };
        [TextArea(1, 2)]
        [SerializeField] private string[] eastPhrases = { "east of here", "to the east" };
        [TextArea(1, 2)]
        [SerializeField] private string[] westPhrases = { "west of here", "to the west" };
        [TextArea(1, 2)]
        [SerializeField] private string[] northeastPhrases = { "northeast of here" };
        [TextArea(1, 2)]
        [SerializeField] private string[] northwestPhrases = { "northwest of here" };
        [TextArea(1, 2)]
        [SerializeField] private string[] southeastPhrases = { "southeast of here" };
        [TextArea(1, 2)]
        [SerializeField] private string[] southwestPhrases = { "southwest of here" };
        [TextArea(1, 2)]
        [SerializeField] private string[] generalAreaPhrases =
        {
            "Try looking around {area}.",
            "Somewhere in {area} — that's the best I can do.",
            "Head toward {area} and keep your eyes open."
        };
        [TextArea(1, 2)]
        [SerializeField] private string[] referNpcPhrases =
        {
            "Ask {npc} — they know that area better.",
            "You want {npc} for that kind of direction.",
            "{npc} might point you the right way."
        };
        [TextArea(1, 2)]
        [SerializeField] private string[] unknownBarks =
        {
            "Never heard of that.",
            "Sorry, can't help you there.",
            "No idea where that is.",
            "Try someone closer to town."
        };

        public string PickCardinalPhrase(float bearingDegrees)
        {
            string[] bucket = PickBucket(bearingDegrees);
            if (bucket == null || bucket.Length == 0)
                return "that way";

            return bucket[Random.Range(0, bucket.Length)];
        }

        public string PickGeneralAreaPhrase(string areaName)
        {
            string template = PickRandom(generalAreaPhrases, "Try looking around {area}.");
            return template.Replace("{area}", areaName);
        }

        public string PickReferNpcPhrase(string npcName)
        {
            string template = PickRandom(referNpcPhrases, "Ask {npc}.");
            return template.Replace("{npc}", npcName);
        }

        public string PickUnknownBark()
        {
            return PickRandom(unknownBarks, "I don't know.");
        }

        private string[] PickBucket(float bearingDegrees)
        {
            float normalized = Mathf.Repeat(bearingDegrees, 360f);
            if (normalized >= 337.5f || normalized < 22.5f)
                return northPhrases;
            if (normalized < 67.5f)
                return northeastPhrases;
            if (normalized < 112.5f)
                return eastPhrases;
            if (normalized < 157.5f)
                return southeastPhrases;
            if (normalized < 202.5f)
                return southPhrases;
            if (normalized < 247.5f)
                return southwestPhrases;
            if (normalized < 292.5f)
                return westPhrases;
            return northwestPhrases;
        }

        private static string PickRandom(string[] options, string fallback)
        {
            if (options == null || options.Length == 0)
                return fallback;

            return options[Random.Range(0, options.Length)];
        }
    }
}
