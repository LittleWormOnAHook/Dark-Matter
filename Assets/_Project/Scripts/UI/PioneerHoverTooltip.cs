using System.Collections;
using System.Text;
using Project.Companions;
using Project.Data;
using Project.Pioneers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Hover card for pioneer roster rows and expedition trio slots: large circular portrait
    /// (4× roster size) + quick-glance info, fading in quickly beside the pointer.
    /// </summary>
    public class PioneerHoverTooltip : MonoBehaviour
    {
        private const float FadeInDuration = 0.12f;
        private static float PortraitSize => PioneerRosterPanelUI.RosterPortraitSize * 4f;

        private static PioneerHoverTooltip instance;

        private RectTransform tooltipRect;
        private CanvasGroup canvasGroup;
        private RawImage portraitPhoto;
        private Image portraitMask;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI bodyText;
        private Vector2 screenOffset = new Vector2(18f, -18f);
        private Coroutine fadeRoutine;

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

            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
            panel.transform.SetParent(transform, false);

            Image panelImage = panel.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(panelImage);
            panelImage.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.PanelBackground, 0.96f);
            panelImage.raycastTarget = false;
            DarkMatterGenesisUiPalette.ApplyFuchsiaTrim(panel);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);

            HorizontalLayoutGroup layout = panel.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 14, 12, 12);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = panel.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            float portraitSize = PortraitSize;
            LayoutElement panelLayout = panel.GetComponent<LayoutElement>();
            panelLayout.minWidth = portraitSize + 240f;
            panelLayout.preferredWidth = portraitSize + 320f;

            portraitPhoto = PioneerPortraitUi.CreateCircularPortrait(panel.transform, portraitSize);
            portraitMask = PioneerPortraitUi.GetMaskImage(portraitPhoto);

            GameObject textColumn = new GameObject("TextColumn", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            textColumn.transform.SetParent(panel.transform, false);
            LayoutElement textColumnLayout = textColumn.GetComponent<LayoutElement>();
            textColumnLayout.minWidth = 220f;
            textColumnLayout.preferredWidth = 300f;
            textColumnLayout.flexibleWidth = 1f;

            VerticalLayoutGroup textLayout = textColumn.GetComponent<VerticalLayoutGroup>();
            textLayout.spacing = 4f;
            textLayout.childAlignment = TextAnchor.UpperLeft;
            textLayout.childControlWidth = true;
            textLayout.childForceExpandWidth = true;
            textLayout.childControlHeight = true;
            textLayout.childForceExpandHeight = false;

            GameObject titleObj = new GameObject("Title", typeof(RectTransform));
            titleObj.transform.SetParent(textColumn.transform, false);
            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(titleText);
            titleText.fontSize = 18f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = DarkMatterGenesisUiPalette.WarmOffWhite;
            titleText.raycastTarget = false;

            GameObject bodyObj = new GameObject("Body", typeof(RectTransform));
            bodyObj.transform.SetParent(textColumn.transform, false);
            bodyText = bodyObj.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(bodyText);
            bodyText.fontSize = 13f;
            bodyText.color = DarkMatterGenesisUiPalette.Gold;
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            bodyText.raycastTarget = false;

            Hide();
        }

        public void Show(SkilledPioneerRecord record, Vector2 screenPosition)
        {
            if (record == null || titleText == null || bodyText == null)
                return;

            titleText.text = PioneerUiLabels.GetDisplayName(record);
            bodyText.text = BuildBody(record);
            PioneerPortraitUi.ApplyPortrait(portraitMask, portraitPhoto, null, record);

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                UiFrontLayer.ReparentToFront(transform, canvas.transform);

            tooltipRect.position = screenPosition + screenOffset;
            ClampToScreen();

            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeIn());
        }

        public void Hide()
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            gameObject.SetActive(false);
        }

        private IEnumerator FadeIn()
        {
            if (canvasGroup == null)
                yield break;

            canvasGroup.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < FadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / FadeInDuration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            fadeRoutine = null;
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
            sb.Append(CompanionHealthLookup.FormatHealthLine(record.id)).Append('\n');
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
