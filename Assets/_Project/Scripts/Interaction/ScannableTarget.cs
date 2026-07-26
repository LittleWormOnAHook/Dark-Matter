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
        [SerializeField] private bool useCategoryOverride;
        [SerializeField] private ScannerTargetCategory scanCategory = ScannerTargetCategory.Generic;
        [Tooltip("When enabled, this object is invisible to scanner sweeps and optics markers.")]
        [SerializeField] private bool hiddenFromScanner;

        public string ScanLabel => string.IsNullOrWhiteSpace(scanLabel) ? name : scanLabel;
        public Color ScanColor => scanColor;
        public bool RequiresLineOfSight => requiresLineOfSight;
        public bool HasCategoryOverride => useCategoryOverride;
        public ScannerTargetCategory ScanCategory => scanCategory;
        public Vector3 ScanPosition => transform.position;
        public bool HiddenFromScanner => hiddenFromScanner;
        public bool IsVisibleToScanner => !hiddenFromScanner && isActiveAndEnabled;

        public void Configure(
            string label,
            Color color,
            ScannerTargetCategory category = ScannerTargetCategory.Loot,
            bool categoryOverride = true,
            bool lineOfSight = true,
            bool visibleToScanner = true)
        {
            scanLabel = label;
            scanColor = color;
            scanCategory = category;
            useCategoryOverride = categoryOverride;
            requiresLineOfSight = lineOfSight;
            hiddenFromScanner = !visibleToScanner;
        }

        public void SetHiddenFromScanner(bool hidden)
        {
            hiddenFromScanner = hidden;
        }
    }
}
