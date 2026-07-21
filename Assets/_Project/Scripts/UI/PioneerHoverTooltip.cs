using System.Text;
using Project.Data;
using Project.Pioneers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Hover tooltip for pioneer roster rows and expedition trio slots. Mirrors PetHoverTooltip's
    /// build/show/clamp pattern so pioneers get the same quick-glance detail treatment as pets.
    /// </summary>
    public class PioneerHoverTooltip : MonoBehaviour
    {
        private static PioneerHoverTooltip instance;

        private RectTransform tooltipRect;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI bodyText;
        private Vector2 screenOffset = new Vector2(18f, -18f);

        public static PioneerHoverTooltip Instance => instance;

        public static void HideAny()
        {
            instance?.Hide();
        }

        public static PioneerHoverTooltip EnsureExists(Transform canvasRoot)
        {
            if (instance != null)
                return instance;

            GameObject host = new GameObject("PioneerHoverTooltip", typeof(RectTransform));
            host.transform.SetParent(canvasRoot, false);
            PioneerHoverTooltip tooltip = host.AddComponent<PioneerHoverTooltip>();
            tooltip.Build();
            instance = tooltip;
            return tooltip;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void Build()
        {
            tooltipRect = transform as RectTransform;
            tooltipRect.pivot = new Vector2(0f, 1f);

            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
            panel.transform.SetParent(transform, false);

            Image panelImage = panel.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(panelImage);
            panelImage.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.PanelBackground, 0.96f);
            panelImage.raycastTarget = false;

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);

            VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            ContentSizeFitter fitter = panel.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement panelLayout = panel.GetComponent<LayoutElement>();
            panelLayout.minWidth = 220f;
            panelLayout.preferredWidth = 300f;

            GameObject titleObj = new GameObject("Title", typeof(RectTransform));
            titleObj.transform.SetParent(panel.transform, false);
            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(titleText);
            titleText.fontSize = 18f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = SurvivalPioneerUiPalette.WarmOffWhite;
            titleText.raycastTarget = false;

            GameObject bodyObj = new GameObject("Body", typeof(RectTransform));
            bodyObj.transform.SetParent(panel.transform, false);
            bodyText = bodyObj.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(bodyText);
            bodyText.fontSize = 13f;
            bodyText.color = SurvivalPioneerUiPalette.SoftBeigeGray;
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            bodyText.raycastTarget = false;

            Hide();
        }

        public void Show(SkilledPioneerRecord record, Vector2 screenPosition)
        {
            if (record == null || titleText == null || bodyText == null)
                return;

            titleText.text = record.displayName;
            bodyText.text = BuildBody(record);

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                UiFrontLayer.ReparentToFront(transform, canvas.transform);

            tooltipRect.position = screenPosition + screenOffset;
            ClampToScreen();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private static string BuildBody(SkilledPioneerRecord record)
        {
            string traits = PioneerTraitUtility.FormatTraitList(record.traitIds);
            string passives = PioneerTraitUtility.FormatTraitList(record.passiveAbilityIds);
            string skills = record.learnedSkills == null || record.learnedSkills.Length == 0
                ? "None"
                : PioneerTraitUtility.FormatTraitList(record.learnedSkills);
            string disposition = record.Kind == PioneerKind.RescuedEcho
                ? PioneerTraitUtility.GetDispositionLabel(record.Disposition)
                : null;

            string statusTag = record.WorkState == PioneerWorkState.Injured
                ? "Injured"
                : record.isInExpeditionTrio
                    ? "In expedition trio"
                    : "Available";
            if (record.isStarterPick)
                statusTag += "  ·  Starter";

            // Trimmed to a preview length — this is a quick-glance hover card, not the full Pioneer
            // Detail panel (which already shows the untruncated backstory when the row is selected).
            const int backstoryPreviewLength = 180;
            string backstory = string.IsNullOrEmpty(record.backstory)
                ? null
                : record.backstory.Length > backstoryPreviewLength
                    ? record.backstory.Substring(0, backstoryPreviewLength - 3) + "..."
                    : record.backstory;

            StringBuilder sb = new StringBuilder();
            sb.Append(SkilledPioneerClassUtility.ToDisplayName(record.pioneerClass)).Append("  ·  Lv ").Append(record.level).Append('\n');
            sb.Append(statusTag).Append("\n\n");
            sb.Append("Rad ").Append(record.radiationResistance.ToString("P0"))
                .Append("  ·  Exp ").Append(record.expeditionEfficiency.ToString("P0"))
                .Append("  ·  Syn ").Append(record.combatSynergy.ToString("P0")).Append('\n');
            sb.Append("Saturation ").Append(record.saturation.ToString("P0"));
            if (disposition != null)
                sb.Append("  ·  Disposition ").Append(disposition);
            sb.Append("\n\n");
            sb.Append("Traits: ").Append(traits).Append('\n');
            sb.Append("Passives: ").Append(passives).Append('\n');
            sb.Append("Skills: ").Append(skills);

            if (backstory != null)
                sb.Append("\n\n").Append(backstory);

            return sb.ToString();
        }

        private void ClampToScreen()
        {
            if (tooltipRect == null)
                return;

            Vector3[] corners = new Vector3[4];
            tooltipRect.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            Vector2 offset = Vector2.zero;

            if (max.x > Screen.width)
                offset.x = Screen.width - max.x;
            if (min.y < 0f)
                offset.y = -min.y;
            if (max.y > Screen.height)
                offset.y = Screen.height - max.y;
            if (min.x < 0f)
                offset.x = -min.x;

            tooltipRect.position += (Vector3)offset;
        }
    }
}
