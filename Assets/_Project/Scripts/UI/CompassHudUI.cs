using System.Collections.Generic;
using Project.Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Horizontal scrolling compass strip anchored just below the minimap. Pure presentation —
    /// it never reads player transforms or MapRegistry itself; <see cref="MapUI"/> (via
    /// MapUI.Compass.cs) resolves heading/position/markers each refresh and pushes them in through
    /// <see cref="RefreshHeading"/> / <see cref="RefreshMarkers"/>. World convention: +Z = North,
    /// +X = East, heading increases clockwise (matches WorldMapProvider and PlayerController.CameraYaw).
    /// </summary>
    public class CompassHudUI : MonoBehaviour
    {
        private const float TickIntervalDegrees = 15f;
        private const float FieldOfViewDegrees = 140f;
        private const float MaxMarkerRange = 250f;
        private const int MaxVisibleMarkers = 10;
        private const float TickLabelFontSize = 12f;
        private const float CardinalLabelFontSize = 14f;
        private const float MarkerIconSize = 14f;
        private const float MarkerDistanceFontSize = 9f;
        private const float BorderThickness = 2f;

        private RectTransform root;

        /// <summary>Exposes the compass's own live rect so sibling HUD elements (the info panel)
        /// can stack directly below its actual computed position instead of duplicating the
        /// positioning math.</summary>
        public RectTransform Root => root;
        private RectTransform stripViewport;
        private RectTransform markerLayer;
        private RectTransform tickLayer;
        private RectTransform pointerRect;
        private TextMeshProUGUI headingReadout;

        private readonly List<TickEntry> ticks = new List<TickEntry>();
        private readonly Dictionary<MapMarker, MarkerEntry> markerIcons = new Dictionary<MapMarker, MarkerEntry>();
        private bool built;

        private struct TickEntry
        {
            public float AngleDegrees;
            public RectTransform Rect;
            public TextMeshProUGUI Label;
            public Image Line;
        }

        private struct MarkerEntry
        {
            public RectTransform Rect;
            public Image Icon;
            public TextMeshProUGUI DistanceLabel;
        }

        public void EnsureBuilt(Transform parent, RectTransform minimapRootRect)
        {
            if (built)
                return;

            root = BuildRoot(parent);
            PositionBelowMinimap(minimapRootRect);
            BuildStrip();
            BuildPointerAndReadout();
            BuildTicks();
            built = true;
        }

        public void RepositionBelowMinimap(RectTransform minimapRootRect)
        {
            if (built)
                PositionBelowMinimap(minimapRootRect);
        }

        public void SetVisible(bool visible)
        {
            if (root != null)
                root.gameObject.SetActive(visible);
        }

        /// <summary>Cheap per-frame refresh: repositions the fixed tick set and heading readout.</summary>
        public void RefreshHeading(float headingDegrees)
        {
            if (!built)
                return;

            float halfFov = FieldOfViewDegrees * 0.5f;
            float halfWidth = stripViewport.rect.width * 0.5f;

            for (int i = 0; i < ticks.Count; i++)
            {
                TickEntry tick = ticks[i];
                float delta = Mathf.DeltaAngle(headingDegrees, tick.AngleDegrees);
                bool visible = Mathf.Abs(delta) <= halfFov;
                tick.Rect.gameObject.SetActive(visible);
                if (visible)
                {
                    float x = (delta / halfFov) * halfWidth;
                    tick.Rect.anchoredPosition = new Vector2(x, tick.Rect.anchoredPosition.y);
                }
            }

            if (headingReadout != null)
                headingReadout.text = $"{Mathf.RoundToInt(NormalizeDegrees(headingDegrees)):000}";
        }

        /// <summary>Throttled refresh (matches the minimap's marker refresh cadence): pools and
        /// positions POI icons + distance labels along the strip.</summary>
        public void RefreshMarkers(float headingDegrees, Vector3 playerWorldPosition, IReadOnlyList<MapMarker> markers)
        {
            if (!built)
                return;

            float halfFov = FieldOfViewDegrees * 0.5f;
            float halfWidth = stripViewport.rect.width * 0.5f;
            var seen = new HashSet<MapMarker>();
            int shown = 0;

            for (int i = 0; i < markers.Count && shown < MaxVisibleMarkers; i++)
            {
                MapMarker marker = markers[i];
                if (marker == null || !marker.ShowOnMinimap || !marker.IsRevealedOnMap)
                    continue;

                Vector3 toMarker = marker.WorldPosition - playerWorldPosition;
                toMarker.y = 0f;
                float distance = toMarker.magnitude;
                if (distance > MaxMarkerRange)
                    continue;

                float bearing = Mathf.Atan2(toMarker.x, toMarker.z) * Mathf.Rad2Deg;
                if (bearing < 0f)
                    bearing += 360f;

                float delta = Mathf.DeltaAngle(headingDegrees, bearing);
                if (Mathf.Abs(delta) > halfFov)
                    continue;

                seen.Add(marker);
                shown++;

                if (!markerIcons.TryGetValue(marker, out MarkerEntry entry))
                {
                    entry = CreateMarkerEntry(marker);
                    markerIcons[marker] = entry;
                }

                entry.Icon.sprite = marker.IconSprite != null ? marker.IconSprite : MapUiSprites.Dot;
                entry.Icon.color = marker.Color;
                entry.DistanceLabel.text = $"{Mathf.RoundToInt(distance)}m";

                float x = (delta / halfFov) * halfWidth;
                entry.Rect.anchoredPosition = new Vector2(x, entry.Rect.anchoredPosition.y);
            }

            List<MapMarker> stale = null;
            foreach (KeyValuePair<MapMarker, MarkerEntry> pair in markerIcons)
            {
                if (seen.Contains(pair.Key))
                    continue;

                stale ??= new List<MapMarker>();
                stale.Add(pair.Key);
                if (pair.Value.Rect != null)
                    Destroy(pair.Value.Rect.gameObject);
            }

            if (stale == null)
                return;

            for (int i = 0; i < stale.Count; i++)
                markerIcons.Remove(stale[i]);
        }

        private static float NormalizeDegrees(float degrees)
        {
            degrees %= 360f;
            return degrees < 0f ? degrees + 360f : degrees;
        }

        private RectTransform BuildRoot(Transform parent)
        {
            gameObject.name = "CompassHud";
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            if (rect == null)
                rect = gameObject.AddComponent<RectTransform>();

            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            return rect;
        }

        private void PositionBelowMinimap(RectTransform minimapRootRect)
        {
            root.sizeDelta = new Vector2(GameplayHudLayout.CompassWidth, GameplayHudLayout.CompassTotalHeight);

            // Prefer reading the minimap panel's own live anchoredPosition/size/localScale over
            // GameplayHudLayout's build-time constants. "Preserve manual layout" scenes keep a
            // hand-authored MinimapPanel that can have a different sizeDelta and/or an extra local
            // scale baked on — trusting the constants there put the compass in the wrong place
            // (it ended up overlapping the minimap). This only applies when the minimap is anchored
            // top-right with a top-right pivot (the standard corner-minimap convention used
            // everywhere in this HUD); otherwise fall back to the constant-based position.
            if (minimapRootRect != null &&
                Mathf.Approximately(minimapRootRect.pivot.x, 1f) &&
                Mathf.Approximately(minimapRootRect.pivot.y, 1f))
            {
                float rightInset = -minimapRootRect.anchoredPosition.x;
                float minimapRenderedHeight = minimapRootRect.rect.height * minimapRootRect.localScale.y;
                float minimapBottomY = minimapRootRect.anchoredPosition.y - minimapRenderedHeight;

                root.anchoredPosition = new Vector2(
                    -rightInset,
                    minimapBottomY - GameplayHudLayout.CompassGapBelowMinimap);
            }
            else
            {
                root.anchoredPosition = GameplayHudLayout.CompassAnchoredPosition;
            }
        }

        private void BuildStrip()
        {
            GameObject viewportObject = new GameObject("StripViewport", typeof(RectTransform));
            viewportObject.transform.SetParent(root, false);
            stripViewport = viewportObject.GetComponent<RectTransform>();
            stripViewport.anchorMin = new Vector2(0f, 1f);
            stripViewport.anchorMax = new Vector2(1f, 1f);
            stripViewport.pivot = new Vector2(0.5f, 1f);
            stripViewport.sizeDelta = new Vector2(0f, GameplayHudLayout.CompassStripHeight);
            stripViewport.anchoredPosition = Vector2.zero;
            viewportObject.AddComponent<RectMask2D>();

            // Thin gold border: a full-bounds gold frame with an inset navy fill on top, leaving
            // only a BorderThickness-px ring of gold visible around the strip's edge.
            GameObject borderObject = new GameObject("Border", typeof(RectTransform));
            borderObject.transform.SetParent(stripViewport, false);
            RectTransform borderRect = borderObject.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            Image border = borderObject.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(border);
            border.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.Gold, 0.3f);
            border.raycastTarget = false;

            // Punches out a fully transparent middle so only the BorderThickness-px gold ring is
            // visible — no dark fill panel behind the ticks (user explicitly wants border-only,
            // no overlay obscuring the minimap/world behind the strip).
            GameObject backgroundObject = new GameObject("Background", typeof(RectTransform));
            backgroundObject.transform.SetParent(stripViewport, false);
            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = new Vector2(BorderThickness, BorderThickness);
            backgroundRect.offsetMax = new Vector2(-BorderThickness, -BorderThickness);
            Image background = backgroundObject.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(background);
            background.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.DarkNavy, 0f);
            background.raycastTarget = false;

            GameObject markerLayerObject = new GameObject("MarkerLayer", typeof(RectTransform));
            markerLayerObject.transform.SetParent(stripViewport, false);
            markerLayer = markerLayerObject.GetComponent<RectTransform>();
            markerLayer.anchorMin = new Vector2(0.5f, 1f);
            markerLayer.anchorMax = new Vector2(0.5f, 1f);
            markerLayer.pivot = new Vector2(0.5f, 1f);
            markerLayer.sizeDelta = Vector2.zero;
            markerLayer.anchoredPosition = new Vector2(0f, -2f);

            GameObject tickLayerObject = new GameObject("TickLayer", typeof(RectTransform));
            tickLayerObject.transform.SetParent(stripViewport, false);
            tickLayer = tickLayerObject.GetComponent<RectTransform>();
            tickLayer.anchorMin = new Vector2(0.5f, 0f);
            tickLayer.anchorMax = new Vector2(0.5f, 0f);
            tickLayer.pivot = new Vector2(0.5f, 0f);
            tickLayer.sizeDelta = Vector2.zero;
            tickLayer.anchoredPosition = new Vector2(0f, 2f);
        }

        private void BuildPointerAndReadout()
        {
            GameObject pointerObject = new GameObject("Pointer", typeof(RectTransform));
            pointerObject.transform.SetParent(root, false);
            pointerRect = pointerObject.GetComponent<RectTransform>();
            pointerRect.anchorMin = new Vector2(0.5f, 1f);
            pointerRect.anchorMax = new Vector2(0.5f, 1f);
            pointerRect.pivot = new Vector2(0.5f, 1f);
            pointerRect.sizeDelta = new Vector2(10f, GameplayHudLayout.CompassPointerHeight);
            pointerRect.anchoredPosition = new Vector2(0f, -GameplayHudLayout.CompassStripHeight);

            Image pointerImage = pointerObject.AddComponent<Image>();
            pointerImage.sprite = MapUiSprites.PlayerArrow;
            pointerImage.color = DarkMatterGenesisUiPalette.WarmOffWhite;
            pointerImage.raycastTarget = false;

            GameObject readoutObject = new GameObject("HeadingReadout", typeof(RectTransform));
            readoutObject.transform.SetParent(root, false);
            RectTransform readoutRect = readoutObject.GetComponent<RectTransform>();
            readoutRect.anchorMin = new Vector2(0.5f, 1f);
            readoutRect.anchorMax = new Vector2(0.5f, 1f);
            readoutRect.pivot = new Vector2(0.5f, 1f);
            readoutRect.sizeDelta = new Vector2(60f, GameplayHudLayout.CompassHeadingLabelHeight);
            readoutRect.anchoredPosition = new Vector2(
                0f,
                -(GameplayHudLayout.CompassStripHeight + GameplayHudLayout.CompassPointerHeight));

            headingReadout = readoutObject.AddComponent<TextMeshProUGUI>();
            ApplyLabelFont(headingReadout, semiBold: true);
            headingReadout.fontSize = CardinalLabelFontSize;
            headingReadout.alignment = TextAlignmentOptions.Top;
            headingReadout.color = DarkMatterGenesisUiPalette.Gold;
            headingReadout.text = "000";
            headingReadout.raycastTarget = false;
        }

        private void BuildTicks()
        {
            for (float angle = 0f; angle < 360f; angle += TickIntervalDegrees)
            {
                GameObject tickObject = new GameObject($"Tick_{angle:000}", typeof(RectTransform));
                tickObject.transform.SetParent(tickLayer, false);
                RectTransform tickRect = tickObject.GetComponent<RectTransform>();
                tickRect.anchorMin = new Vector2(0.5f, 0f);
                tickRect.anchorMax = new Vector2(0.5f, 0f);
                tickRect.pivot = new Vector2(0.5f, 0f);
                tickRect.sizeDelta = new Vector2(32f, GameplayHudLayout.CompassStripHeight - 4f);
                tickRect.anchoredPosition = Vector2.zero;

                GameObject lineObject = new GameObject("Line", typeof(RectTransform));
                lineObject.transform.SetParent(tickRect, false);
                RectTransform lineRect = lineObject.GetComponent<RectTransform>();
                lineRect.anchorMin = new Vector2(0.5f, 0f);
                lineRect.anchorMax = new Vector2(0.5f, 0f);
                lineRect.pivot = new Vector2(0.5f, 0f);
                bool isCardinal = Mathf.Approximately(angle % 90f, 0f);
                lineRect.sizeDelta = new Vector2(2f, isCardinal ? 10f : 6f);
                lineRect.anchoredPosition = Vector2.zero;
                Image line = lineObject.AddComponent<Image>();
                MenuUiBuilder.ApplyUiSprite(line);
                line.color = isCardinal
                    ? DarkMatterGenesisUiPalette.Gold
                    : DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.WarmOffWhite, 0.6f);
                line.raycastTarget = false;

                GameObject labelObject = new GameObject("Label", typeof(RectTransform));
                labelObject.transform.SetParent(tickRect, false);
                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0.5f, 0f);
                labelRect.anchorMax = new Vector2(0.5f, 0f);
                labelRect.pivot = new Vector2(0.5f, 0f);
                labelRect.sizeDelta = new Vector2(32f, 16f);
                labelRect.anchoredPosition = new Vector2(0f, isCardinal ? 12f : 8f);

                TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
                ApplyLabelFont(label, semiBold: isCardinal);
                label.fontSize = isCardinal ? CardinalLabelFontSize : TickLabelFontSize;
                label.alignment = TextAlignmentOptions.Center;
                label.color = isCardinal ? DarkMatterGenesisUiPalette.Gold : DarkMatterGenesisUiPalette.MutedText;
                label.text = isCardinal ? CardinalLabel(angle) : Mathf.RoundToInt(angle).ToString();
                label.raycastTarget = false;

                ticks.Add(new TickEntry
                {
                    AngleDegrees = angle,
                    Rect = tickRect,
                    Label = label,
                    Line = line
                });
            }
        }

        private static string CardinalLabel(float angle)
        {
            if (Mathf.Approximately(angle, 0f)) return "N";
            if (Mathf.Approximately(angle, 90f)) return "E";
            if (Mathf.Approximately(angle, 180f)) return "S";
            if (Mathf.Approximately(angle, 270f)) return "W";
            return Mathf.RoundToInt(angle).ToString();
        }

        private MarkerEntry CreateMarkerEntry(MapMarker marker)
        {
            GameObject markerObject = new GameObject("CompassMarker", typeof(RectTransform));
            markerObject.transform.SetParent(markerLayer, false);
            RectTransform markerRect = markerObject.GetComponent<RectTransform>();
            markerRect.anchorMin = new Vector2(0.5f, 1f);
            markerRect.anchorMax = new Vector2(0.5f, 1f);
            markerRect.pivot = new Vector2(0.5f, 1f);
            markerRect.sizeDelta = new Vector2(MarkerIconSize, MarkerIconSize + MarkerDistanceFontSize + 2f);
            markerRect.anchoredPosition = Vector2.zero;

            GameObject iconObject = new GameObject("Icon", typeof(RectTransform));
            iconObject.transform.SetParent(markerRect, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.sizeDelta = new Vector2(MarkerIconSize, MarkerIconSize);
            iconRect.anchoredPosition = Vector2.zero;
            Image icon = iconObject.AddComponent<Image>();
            icon.raycastTarget = false;

            GameObject distanceObject = new GameObject("Distance", typeof(RectTransform));
            distanceObject.transform.SetParent(markerRect, false);
            RectTransform distanceRect = distanceObject.GetComponent<RectTransform>();
            distanceRect.anchorMin = new Vector2(0.5f, 1f);
            distanceRect.anchorMax = new Vector2(0.5f, 1f);
            distanceRect.pivot = new Vector2(0.5f, 1f);
            distanceRect.sizeDelta = new Vector2(40f, MarkerDistanceFontSize + 2f);
            distanceRect.anchoredPosition = new Vector2(0f, -MarkerIconSize);
            TextMeshProUGUI distanceLabel = distanceObject.AddComponent<TextMeshProUGUI>();
            ApplyLabelFont(distanceLabel, semiBold: false);
            distanceLabel.fontSize = MarkerDistanceFontSize;
            distanceLabel.alignment = TextAlignmentOptions.Center;
            distanceLabel.color = DarkMatterGenesisUiPalette.MutedText;
            distanceLabel.raycastTarget = false;

            return new MarkerEntry
            {
                Rect = markerRect,
                Icon = icon,
                DistanceLabel = distanceLabel
            };
        }

        private static void ApplyLabelFont(TextMeshProUGUI label, bool semiBold)
        {
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.ApplyFont(label, semiBold: semiBold);
            else
                TmpUiHelper.ApplyDefaultFont(label);
        }
    }
}
