using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Sandbox preview host for the inventory panel (UI Studio / preview scene).
    /// </summary>
    public sealed class InventoryPanelPreviewHost : MonoBehaviour, IUiPreviewSurface
    {
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private int previewSlotCount = 24;

        private Transform panelRoot;

        public string PanelId => UiPanelIds.InventoryPanel;

        public Transform GetPreviewPanelRoot() => panelRoot;

        public void BuildPreview(Transform previewRoot)
        {
            TeardownPreview();
            if (previewRoot == null)
                return;

            UiPreviewContext.IsActive = true;
            UiPreviewContext.IsSandbox = true;
            try
            {
                panelRoot = CreateInventoryPanel(previewRoot);
            }
            finally
            {
                UiPreviewContext.IsActive = false;
                UiPreviewContext.IsSandbox = false;
            }
        }

        public void TeardownPreview()
        {
            if (panelRoot == null)
                return;

            if (Application.isPlaying)
                Destroy(panelRoot.gameObject);
            else
                DestroyImmediate(panelRoot.gameObject);

            panelRoot = null;
        }

        private Transform CreateInventoryPanel(Transform canvasRoot)
        {
            GameObject panelGo = new GameObject(
                "InventoryPanel",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster),
                typeof(Image));

            panelGo.transform.SetParent(canvasRoot, false);
            RectTransform panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(640f, 480f);
            panelRect.anchoredPosition = Vector2.zero;

            Canvas panelCanvas = panelGo.GetComponent<Canvas>();
            panelCanvas.overrideSorting = true;
            panelCanvas.sortingOrder = 10;

            Image panelImage = panelGo.GetComponent<Image>();
            panelImage.color = new Color(0.08f, 0.1f, 0.12f, 0.94f);

            GameObject gridGo = new GameObject("MainInventoryGrid", typeof(RectTransform));
            gridGo.transform.SetParent(panelGo.transform, false);
            RectTransform gridRect = gridGo.GetComponent<RectTransform>();
            gridRect.anchorMin = Vector2.zero;
            gridRect.anchorMax = Vector2.one;
            gridRect.offsetMin = new Vector2(12f, 12f);
            gridRect.offsetMax = new Vector2(-12f, -12f);

            GridLayoutGroup grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 8;
            grid.spacing = new Vector2(8f, 8f);
            grid.cellSize = new Vector2(64f, 64f);

            if (slotPrefab != null)
            {
                for (int i = 0; i < previewSlotCount; i++)
                    Instantiate(slotPrefab, gridGo.transform);
            }

            panelGo.SetActive(true);
            return panelGo.transform;
        }
    }
}
