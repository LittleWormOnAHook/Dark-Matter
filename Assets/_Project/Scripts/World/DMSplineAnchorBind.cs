using UnityEngine;

namespace Project.World
{
    /// <summary>
    /// Marks a placed anchor object and links it to a <see cref="DMSplineCreator"/> anchor index.
    /// </summary>
    [DisallowMultipleComponent]
    public class DMSplineAnchorBind : MonoBehaviour
    {
        [SerializeField] private DMSplineCreator creator;
        [SerializeField] private int anchorIndex;

        public DMSplineCreator Creator => creator;
        public int AnchorIndex => anchorIndex;

        public void Bind(DMSplineCreator owner, int index)
        {
            creator = owner;
            anchorIndex = index;
        }
    }
}
