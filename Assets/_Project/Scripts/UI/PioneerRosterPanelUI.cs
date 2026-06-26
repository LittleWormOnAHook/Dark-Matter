using Project.Pioneers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Journal Pioneers tab mockup: skilled roster summary and worker counts.
    /// </summary>
    public class PioneerRosterPanelUI : MonoBehaviour
    {
        private Transform embeddedParent;
        private GameObject panelRoot;
        private TextMeshProUGUI summaryLabel;
        private Transform listParent;
        private PioneerRosterManager roster;

        public void EmbedIn(Transform parent)
        {
            if (parent == null)
                return;

            embeddedParent = parent;
            roster = PioneerRosterManager.EnsureExists();
            EnsureBuilt(parent);

            if (roster != null)
                roster.OnRosterChanged += Refresh;

            Refresh();
        }

        public void Unembed()
        {
            if (roster != null)
                roster.OnRosterChanged -= Refresh;

            if (panelRoot != null)
                Destroy(panelRoot);

            panelRoot = null;
            summaryLabel = null;
            listParent = null;
            embeddedParent = null;
        }

        public void Refresh()
        {
            if (panelRoot == null || roster == null)
                return;

            int total = roster.GetTotalPioneerCount();
            summaryLabel.text =
                $"Total {total}/{PioneerRosterManager.MaxTotalPioneers}  |  " +
                $"Skilled {roster.SkilledPioneers.Count}/{PioneerRosterManager.MaxSkilledPioneers}  |  " +
                $"Workers {roster.WorkerCount}/{PioneerRosterManager.MaxWorkerPioneers}  |  " +
                $"Expedition trio size {PioneerRosterManager.ExpeditionTrioSize}";

            for (int i = listParent.childCount - 1; i >= 0; i--)
                Destroy(listParent.GetChild(i).gameObject);

            if (roster.SkilledPioneers.Count == 0)
            {
                CreateEntryLabel("No skilled pioneers recruited yet.");
                return;
            }

            for (int i = 0; i < roster.SkilledPioneers.Count; i++)
            {
                SkilledPioneerRecord record = roster.SkilledPioneers[i];
                string starterTag = record.isStarterPick ? " [Starter]" : string.Empty;
                CreateEntryLabel(
                    $"{record.displayName}{starterTag}\n" +
                    $"{SkilledPioneerClassUtility.ToDisplayName(record.pioneerClass)}  ·  Lv {record.level}\n" +
                    $"{record.backstory}");
            }

            CreateEntryLabel(
                "Prototype note: worker assignments, trio picker, and Pi NFT listing will attach here later.");
        }

        private void EnsureBuilt(Transform parent)
        {
            if (panelRoot != null)
                return;

            panelRoot = new GameObject("PioneerRosterPanel", typeof(RectTransform), typeof(VerticalLayoutGroup));
            panelRoot.transform.SetParent(parent, false);
            RectTransform rect = panelRoot.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8f, 8f);
            rect.offsetMax = new Vector2(-8f, -8f);

            VerticalLayoutGroup layout = panelRoot.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            summaryLabel = CreateText(panelRoot.transform, "Roster", 16f, FontStyles.Bold);

            GameObject scrollHost = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollHost.transform.SetParent(panelRoot.transform, false);
            LayoutElement scrollLayout = scrollHost.AddComponent<LayoutElement>();
            scrollLayout.flexibleHeight = 1f;
            scrollLayout.minHeight = 280f;
            scrollHost.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 0.92f);

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewport.transform.SetParent(scrollHost.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            Stretch(viewportRect);
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 8f;
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scrollRect = scrollHost.GetComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            listParent = content.transform;
        }

        private void CreateEntryLabel(string text)
        {
            GameObject entry = new GameObject("Entry", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            entry.transform.SetParent(listParent, false);
            Image bg = entry.GetComponent<Image>();
            bg.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);
            LayoutElement layout = entry.GetComponent<LayoutElement>();
            layout.minHeight = 88f;

            TextMeshProUGUI label = CreateText(entry.transform, text, 13f, FontStyles.Normal);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 8f);
            labelRect.offsetMax = new Vector2(-10f, -8f);
        }

        private static TextMeshProUGUI CreateText(Transform parent, string text, float size, FontStyles style)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.color = Color.white;
            return label;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
