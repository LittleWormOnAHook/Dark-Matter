using UnityEngine;

namespace Project.Audio
{
    public class FootstepSurface : MonoBehaviour
    {
        [SerializeField] private string surfaceTag = "Default";

        public string SurfaceTag => surfaceTag;
    }
}
