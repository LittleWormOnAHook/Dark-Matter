using UnityEngine;

namespace Project.Interaction
{
    /// <summary>
    /// Optional world label surfaced by the scanner optics overlay.
    /// </summary>
    public class ScannableTarget : MonoBehaviour
    {
        [SerializeField] private string scanLabel = "Point of Interest";
        [SerializeField] private Color scanColor = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private bool requiresLineOfSight = true;

        public string ScanLabel => string.IsNullOrWhiteSpace(scanLabel) ? name : scanLabel;
        public Color ScanColor => scanColor;
        public bool RequiresLineOfSight => requiresLineOfSight;
        public Vector3 ScanPosition => transform.position;
    }
}
