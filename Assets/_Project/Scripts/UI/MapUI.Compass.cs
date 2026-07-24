using Project.Map;
using UnityEngine;

namespace Project.UI
{
    // Compass strip anchored just below the minimap. This partial owns the CompassHudUI child
    // object and feeds it heading/position/marker data that MapUI already resolves each refresh
    // (GetMapFacingYaw / GetMapWorldPosition / MapRegistry) — CompassHudUI itself is presentation
    // only and never reads the player or MapRegistry directly. Split out of MapUI.cs.
    public partial class MapUI
    {
        private CompassHudUI compassHud;

        private void EnsureCompassBuilt()
        {
            if (compassHud != null)
            {
                compassHud.RepositionBelowMinimap(minimapRootRect);
                RepositionInfoPanelBelowCompass();
                return;
            }

            GameObject compassObject = new GameObject("CompassHud", typeof(RectTransform));
            compassHud = compassObject.AddComponent<CompassHudUI>();
            compassHud.EnsureBuilt(transform, minimapRootRect);
            RepositionInfoPanelBelowCompass();
        }

        // Stacks the Range%/Scan-status info panel directly below the compass's own live rect
        // (rather than a constant offset), so it stays correct even when the compass itself had to
        // move to match a hand-authored ("preserve manual layout") minimap of a different size.
        private void RepositionInfoPanelBelowCompass()
        {
            if (compassHud == null || compassHud.Root == null)
                return;

            if (transform.Find("InfoPanel") is not RectTransform infoRect)
                return;

            RectTransform compassRect = compassHud.Root;
            infoRect.anchorMin = compassRect.anchorMin;
            infoRect.anchorMax = compassRect.anchorMax;
            infoRect.pivot = compassRect.pivot;
            infoRect.anchoredPosition = new Vector2(
                compassRect.anchoredPosition.x,
                compassRect.anchoredPosition.y - compassRect.sizeDelta.y - GameplayHudLayout.InfoPanelGapBelowCompass);
        }

        private void DestroyCompassHud()
        {
            if (compassHud == null)
                return;

            DestroyUiObject(compassHud.gameObject);
            compassHud = null;
        }

        private void SetCompassVisible(bool visible)
        {
            compassHud?.SetVisible(visible);
        }

        private void UpdateCompassHeading()
        {
            if (compassHud == null)
                return;

            compassHud.RefreshHeading(GetMapFacingYaw());
        }

        private void UpdateCompassMarkers()
        {
            if (compassHud == null || !HasMapWorldPosition())
                return;

            compassHud.RefreshMarkers(GetMapFacingYaw(), GetMapWorldPosition(), MapRegistry.ActiveMarkers);
        }
    }
}
