using Project.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Shared soft proximity dot visuals for pickups and Press-E interactables.
    /// </summary>
    internal static class ProximityDotStyle
    {
        public const float CoreSize = 8f;
        public const float GlowMultiplier = 2f;
        public const float GlowAlpha = 0.28f;
        public const float DefaultWorldOffset = 0.45f;

        public static RectTransform CreateDotWidget(Transform parent)
        {
            GameObject dotObject = new GameObject("ProximityDot", typeof(RectTransform));
            dotObject.transform.SetParent(parent, false);

            RectTransform dotRect = dotObject.GetComponent<RectTransform>();
            dotRect.sizeDelta = new Vector2(CoreSize * GlowMultiplier, CoreSize * GlowMultiplier);

            GameObject glowObject = new GameObject("Glow", typeof(RectTransform));
            glowObject.transform.SetParent(dotObject.transform, false);
            RectTransform glowRect = glowObject.GetComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = Vector2.zero;
            glowRect.offsetMax = Vector2.zero;

            Image glowImage = glowObject.AddComponent<Image>();
            glowImage.sprite = ShiftUiTheme.CircleGlow ?? MapUiSprites.Dot;
            glowImage.color = BuildGlowColor(SurvivalPioneerUiPalette.Gold);
            glowImage.raycastTarget = false;
            glowImage.preserveAspect = true;

            GameObject coreObject = new GameObject("Core", typeof(RectTransform));
            coreObject.transform.SetParent(dotObject.transform, false);
            RectTransform coreRect = coreObject.GetComponent<RectTransform>();
            coreRect.anchorMin = new Vector2(0.5f, 0.5f);
            coreRect.anchorMax = new Vector2(0.5f, 0.5f);
            coreRect.pivot = new Vector2(0.5f, 0.5f);
            coreRect.anchoredPosition = Vector2.zero;
            coreRect.sizeDelta = new Vector2(CoreSize, CoreSize);

            Image coreImage = coreObject.AddComponent<Image>();
            coreImage.sprite = ShiftUiTheme.CircleFilled ?? MapUiSprites.Dot;
            coreImage.raycastTarget = false;
            coreImage.preserveAspect = true;

            return dotRect;
        }

        public static void ApplyColor(RectTransform dotRect, Color coreColor)
        {
            if (dotRect == null)
                return;

            dotRect.sizeDelta = new Vector2(CoreSize * GlowMultiplier, CoreSize * GlowMultiplier);

            Transform glow = dotRect.Find("Glow");
            if (glow != null && glow.TryGetComponent<Image>(out Image glowImage))
            {
                glowImage.sprite = ShiftUiTheme.CircleGlow ?? MapUiSprites.Dot;
                glowImage.color = BuildGlowColor(coreColor);
            }

            Transform core = dotRect.Find("Core");
            if (core != null && core.TryGetComponent<Image>(out Image coreImage))
            {
                coreImage.sprite = ShiftUiTheme.CircleFilled ?? MapUiSprites.Dot;
                coreImage.color = coreColor;
            }
        }

        public static Color BuildGlowColor(Color baseColor)
        {
            baseColor.a = GlowAlpha;
            return baseColor;
        }

        public static Color PickupColor(ItemType itemType) =>
            MapUiSprites.GetResourceColor(itemType);

        public static Color QuestGiverColor => SurvivalPioneerUiPalette.WithAlpha(
            QuestUiPalette.InProgressText,
            0.92f);

        public static Color CraftingColor => SurvivalPioneerUiPalette.WithAlpha(
            SurvivalPioneerUiPalette.Gold,
            0.92f);

        public static Color BuildingColor => SurvivalPioneerUiPalette.WithAlpha(
            SurvivalPioneerUiPalette.RichFuchsia,
            0.9f);

        public static Color LootColor => SurvivalPioneerUiPalette.WithAlpha(
            SurvivalPioneerUiPalette.Gold,
            0.9f);

        public static Color RecipeColor => SurvivalPioneerUiPalette.WithAlpha(
            MapUiSprites.GetResourceColor(ItemType.Quest),
            0.92f);

        public static Color PetColor => SurvivalPioneerUiPalette.WithAlpha(
            SurvivalPioneerUiPalette.ConnectedGreen,
            0.92f);

        public static Color ScienceLabColor => SurvivalPioneerUiPalette.WithAlpha(
            SurvivalPioneerUiPalette.RichFuchsia,
            0.88f);

        public static Color EchoColor => SurvivalPioneerUiPalette.WithAlpha(
            SurvivalPioneerUiPalette.RichFuchsia,
            0.88f);
    }
}
