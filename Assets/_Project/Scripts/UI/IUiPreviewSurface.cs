using UnityEngine;

namespace Project.UI
{
    public interface IUiPreviewSurface
    {
        string PanelId { get; }

        /// <summary>Build or refresh panel hierarchy under previewRoot (editor sandbox).</summary>
        void BuildPreview(Transform previewRoot);

        void TeardownPreview();

        Transform GetPreviewPanelRoot();
    }
}
