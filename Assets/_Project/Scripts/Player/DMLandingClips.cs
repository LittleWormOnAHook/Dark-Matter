using UnityEngine;

namespace Project.Player
{
    [CreateAssetMenu(menuName = "Dark Matter/Player/Landing Clips", fileName = "DMLandingClips")]
    public sealed class DMLandingClips : ScriptableObject
    {
        public AnimationClip fall;
        public AnimationClip getUpFromBelly;
        public AnimationClip getUpFromBack;
    }
}