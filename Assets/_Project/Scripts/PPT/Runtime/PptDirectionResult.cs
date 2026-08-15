using UnityEngine;

namespace Project.PPT
{
    public readonly struct PptDirectionResult
    {
        public PptDirectionResult(
            PptDirectionKind kind,
            string phrase,
            Vector3 aimPosition,
            float bearingDegrees,
            string referNpcId,
            string referNpcDisplayName,
            bool spawnTracer)
        {
            Kind = kind;
            Phrase = phrase;
            AimPosition = aimPosition;
            BearingDegrees = bearingDegrees;
            ReferNpcId = referNpcId;
            ReferNpcDisplayName = referNpcDisplayName;
            SpawnTracer = spawnTracer;
        }

        public PptDirectionKind Kind { get; }
        public string Phrase { get; }
        public Vector3 AimPosition { get; }
        public float BearingDegrees { get; }
        public string ReferNpcId { get; }
        public string ReferNpcDisplayName { get; }
        public bool SpawnTracer { get; }

        public static PptDirectionResult Unknown(string phrase)
        {
            return new PptDirectionResult(
                PptDirectionKind.Unknown,
                phrase,
                Vector3.zero,
                0f,
                string.Empty,
                string.Empty,
                false);
        }
    }
}
