using Project.Combat;
using Project.Data;
using Project.Inventory;
using Project.Interaction;
using Project.Player;
using Project.Player.Invector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Crosshair plus ammo / mining charge readout while a ranged weapon is drawn.
    /// Mining tools reuse the same centered white text style as ammo counts (e.g. "CHARGE 50%").
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(120)]
    public class RangedCombatHud : MonoBehaviour
    {
        [SerializeField] private bool showAmmoCounter = true;
        // Kept in canvas pixels so the visible gap remains the requested seven pixels.
        private const float SurvivalLabelGapPixels = 7f;

        private EquipmentController equipment;
        private WeaponAmmoState ammoState;
        private InventorySystem inventory;
        private PlayerController playerController;
        private TextMeshProUGUI ammoLabel;
        private RectTransform ammoRect;
        private RectTransform petToolbarRect;
        private RectTransform expeditionPioneerRect;
        private readonly Vector3[] rectCorners = new Vector3[4];
        private int lastMiningChargePercent = -1;
        private bool uitkAmmoHidden;

        private void Awake()
        {
            equipment = GetComponent<EquipmentController>();
            ammoState = GetComponent<WeaponAmmoState>();
            inventory = GetComponent<InventorySystem>();
            playerController = GetComponent<PlayerController>();
        }

        private void OnEnable()
        {
            if (ammoState != null)
                ammoState.OnAmmoChanged += RefreshAmmoLabel;
            if (equipment != null)
                equipment.OnSelectedHotbarChanged += HandleSelectionChanged;
            if (inventory != null)
                inventory.OnInventoryChanged += RefreshAmmoLabel;
        }

        private void OnDisable()
        {
            if (ammoState != null)
                ammoState.OnAmmoChanged -= RefreshAmmoLabel;
            if (equipment != null)
                equipment.OnSelectedHotbarChanged -= HandleSelectionChanged;
            if (inventory != null)
                inventory.OnInventoryChanged -= RefreshAmmoLabel;
        }

        private void Start()
        {
            if (showAmmoCounter)
                EnsureAmmoLabel();

            // Drop any leftover magenta mining slider HUD from earlier builds.
            DestroyLegacyMiningChargeHud();
            RefreshAmmoLabel();
        }

        private void HandleSelectionChanged(int _)
        {
            lastMiningChargePercent = -1;
            RefreshAmmoLabel();
        }

        private void RefreshAmmoLabel()
        {
            if (!showAmmoCounter)
                return;

            if (!ShouldShowHud(out ItemData weapon))
            {
                SetAmmoLabelVisible(false);
                return;
            }

            EnsureAmmoLabel();
            if (ammoLabel == null)
                return;

            SetAmmoLabelVisible(true);

            if (weapon.isMiningTool)
            {
                int percent = ammoState != null
                    ? ammoState.GetMiningChargePercent(equipment.ActiveWeaponHotbarSlot)
                    : 0;
                bool needsLayout = percent != lastMiningChargePercent;
                if (needsLayout)
                {
                    lastMiningChargePercent = percent;
                    ammoLabel.text = $"CHARGE {percent}%";
                }

                LayoutAboveSurvivalStats(ammoRect);
                return;
            }

            int weaponHotbarSlot = equipment.ActiveWeaponHotbarSlot;
            int loaded = ammoState != null ? ammoState.GetActiveLoadedAmmo() : 0;
            int magazineSize = WeaponAmmoState.GetMagazineCapacity(weapon);
            bool infiniteReserve = ammoState != null && ammoState.IsInfiniteAmmoForSlot(weaponHotbarSlot);
            int reserve = !infiniteReserve && ammoState != null
                ? ammoState.GetReserveAmmoCount(weaponHotbarSlot)
                : 0;

            if (!infiniteReserve && loaded <= 0 && reserve <= 0)
            {
                ammoLabel.text = "Empty 0/0";
                LayoutAboveSurvivalStats(ammoRect);
                return;
            }

            string ammoLabelName = ResolveAmmoLabelName(weaponHotbarSlot);
            if (infiniteReserve)
            {
                ammoLabel.text = $"{ammoLabelName} {loaded}/{magazineSize}  (∞)";
            }
            else
            {
                ammoLabel.text = reserve > 0
                    ? $"{ammoLabelName} {loaded}/{magazineSize}  (+{reserve})"
                    : $"{ammoLabelName} {loaded}/{magazineSize}";
            }

            LayoutAboveSurvivalStats(ammoRect);
        }

        private string ResolveAmmoLabelName(int weaponHotbarSlot)
        {
            if (ammoState == null)
                return "STANDARD";

            ItemData loadedAmmoItem = ammoState.GetLoadedAmmoItem(weaponHotbarSlot);
            if (loadedAmmoItem != null && !string.IsNullOrWhiteSpace(loadedAmmoItem.itemName))
                return loadedAmmoItem.itemName.ToUpperInvariant();

            AmmoType loadedType = ammoState.GetLoadedAmmoType(weaponHotbarSlot);
            return loadedType == AmmoType.Gunpowder ? "STANDARD" : loadedType.ToString().ToUpperInvariant();
        }

        private void LateUpdate()
        {
            if (!showAmmoCounter)
                return;

            if (DMUiToolkitHud.IsDriving)
            {
                if (!uitkAmmoHidden)
                {
                    SetAmmoLabelVisible(false);
                    uitkAmmoHidden = true;
                }
                return;
            }

            uitkAmmoHidden = false;

            if (!ShouldShowHud(out ItemData weapon))
            {
                SetAmmoLabelVisible(false);
                return;
            }

            if (ammoLabel == null || !ammoLabel.gameObject.activeSelf)
            {
                RefreshAmmoLabel();
                return;
            }

            if (weapon != null && weapon.isMiningTool)
            {
                if (ammoState == null || equipment == null)
                    return;

                int percent = ammoState.GetMiningChargePercent(equipment.ActiveWeaponHotbarSlot);
                if (percent == lastMiningChargePercent && ammoLabel != null && ammoLabel.gameObject.activeSelf)
                    return;

                RefreshAmmoLabel();
                return;
            }

            if (ammoLabel != null && ammoLabel.gameObject.activeSelf)
                LayoutAboveSurvivalStats(ammoRect);
        }

        private bool ShouldShowHud(out ItemData weapon)
        {
            weapon = null;
            if (playerController == null)
                playerController = GetComponent<PlayerController>();
            if (playerController != null && playerController.BlocksCombatInput)
                return false;

            if (equipment == null || !equipment.HasActiveRangedWeapon())
                return false;

            weapon = equipment.DrawnWeaponItem;
            return weapon != null && weapon.IsRangedWeapon;
        }

        private void EnsureAmmoLabel()
        {
            if (ammoLabel != null)
                return;

            Transform parent = ResolveHudParent();

            GameObject labelObject = new GameObject("RangedAmmoLabel");
            labelObject.transform.SetParent(parent, false);

            ammoRect = labelObject.AddComponent<RectTransform>();
            ammoRect.anchorMin = new Vector2(0.5f, 0f);
            ammoRect.anchorMax = new Vector2(0.5f, 0f);
            ammoRect.pivot = new Vector2(0.5f, 0f);
            ammoRect.sizeDelta = new Vector2(220f, 28f);

            ammoLabel = labelObject.AddComponent<TextMeshProUGUI>();
            ammoLabel.alignment = TextAlignmentOptions.Center;
            ammoLabel.fontSize = 15f;
            ammoLabel.color = DarkMatterGenesisUiPalette.WarmOffWhite;
            ammoLabel.text = string.Empty;
            ammoLabel.raycastTarget = false;

            Outline outline = labelObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(1f, -1f);

            LayoutAboveSurvivalStats(ammoRect);
        }

        private void SetAmmoLabelVisible(bool visible)
        {
            if (visible && DMUiToolkitHud.IsDriving)
                visible = false;

            if (ammoLabel != null)
                ammoLabel.gameObject.SetActive(visible);
        }

        private Transform ResolveHudParent()
        {
            UIManager uiManager = FindAnyObjectByType<UIManager>();
            return uiManager != null ? uiManager.transform : transform;
        }

        private void LayoutAboveSurvivalStats(RectTransform target)
        {
            if (target == null)
                return;

            if (target.parent == null)
            {
                Transform parent = ResolveHudParent();
                if (parent != null)
                    target.SetParent(parent, false);
                else
                    target.SetParent(transform, false);
            }

            // Draw above survival bars (sibling order), not behind them.
            target.SetAsLastSibling();

            RectTransform layoutParent = target.parent as RectTransform;
            RectTransform survivalRect = null;

            if (layoutParent != null && survivalRect != null && survivalRect.gameObject.activeInHierarchy)
            {
                Vector2 survivalTop = GetTopCenterInParent(survivalRect, layoutParent);
                target.pivot = new Vector2(0.5f, 0f);
                target.anchorMin = new Vector2(0.5f, 0f);
                target.anchorMax = new Vector2(0.5f, 0f);
                target.anchoredPosition = ParentLocalToAnchoredPosition(
                    layoutParent,
                    target,
                    new Vector2(ResolveLowerHudCenterX(layoutParent, survivalTop.x),
                        survivalTop.y + SurvivalLabelGapPixels));
                return;
            }

            RectTransform hotbarRect = ResolveHotbarRect();
            if (layoutParent == null || hotbarRect == null)
            {
                target.anchoredPosition = new Vector2(0f, 118f * HudLayoutMetrics.HudScale);
                return;
            }

            Vector2 hotbarTop = GetTopCenterInParent(hotbarRect, layoutParent);
            target.pivot = new Vector2(0.5f, 0f);
            target.anchorMin = new Vector2(0.5f, 0f);
            target.anchorMax = new Vector2(0.5f, 0f);
            target.anchoredPosition = ParentLocalToAnchoredPosition(
                layoutParent,
                target,
                new Vector2(ResolveLowerHudCenterX(layoutParent, hotbarTop.x),
                    hotbarTop.y + SurvivalLabelGapPixels));
        }

        /// <summary>
        /// Centers across the complete lower strip, from PET's left edge to the expedition
        /// PIONEERS cluster's right edge. The runtime roots are created by PetToolbarUI and
        /// ExpeditionPioneerHudUI under MainCanvas.
        /// </summary>
        private float ResolveLowerHudCenterX(RectTransform layoutParent, float fallbackX)
        {
            petToolbarRect = ResolveHudPanelRect(layoutParent, petToolbarRect, "PetToolbar");
            expeditionPioneerRect = ResolveHudPanelRect(layoutParent, expeditionPioneerRect, "ExpeditionPioneerHud");

            if (petToolbarRect == null || expeditionPioneerRect == null ||
                !petToolbarRect.gameObject.activeInHierarchy || !expeditionPioneerRect.gameObject.activeInHierarchy)
            {
                return fallbackX;
            }

            float petLeft = GetHorizontalBoundsInParent(petToolbarRect, layoutParent).x;
            float pioneerRight = GetHorizontalBoundsInParent(expeditionPioneerRect, layoutParent).y;
            return (petLeft + pioneerRight) * 0.5f;
        }

        private static RectTransform ResolveHudPanelRect(RectTransform layoutParent, RectTransform cached, string panelName)
        {
            if (cached != null)
                return cached;

            Transform panel = layoutParent.Find(panelName);
            return panel as RectTransform;
        }

        private Vector2 GetHorizontalBoundsInParent(RectTransform rect, RectTransform layoutParent)
        {
            rect.GetWorldCorners(rectCorners);

            float left = float.PositiveInfinity;
            float right = float.NegativeInfinity;
            for (int i = 0; i < rectCorners.Length; i++)
            {
                float x = layoutParent.InverseTransformPoint(rectCorners[i]).x;
                left = Mathf.Min(left, x);
                right = Mathf.Max(right, x);
            }

            return new Vector2(left, right);
        }

        private Vector2 GetTopCenterInParent(RectTransform rect, RectTransform layoutParent)
        {
            rect.GetWorldCorners(rectCorners);

            Vector2 topLeft = layoutParent.InverseTransformPoint(rectCorners[1]);
            Vector2 topRight = layoutParent.InverseTransformPoint(rectCorners[2]);
            return new Vector2((topLeft.x + topRight.x) * 0.5f, Mathf.Max(topLeft.y, topRight.y));
        }

        private static Vector2 ParentLocalToAnchoredPosition(
            RectTransform layoutParent,
            RectTransform target,
            Vector2 parentLocalPosition)
        {
            Vector2 anchorPosition = new Vector2(
                (target.anchorMin.x - layoutParent.pivot.x) * layoutParent.rect.width,
                (target.anchorMin.y - layoutParent.pivot.y) * layoutParent.rect.height);
            return parentLocalPosition - anchorPosition;
        }

        private static RectTransform ResolveHotbarRect()
        {
            InventoryUI inventoryUi = FindAnyObjectByType<InventoryUI>();
            if (inventoryUi != null && inventoryUi.hotbarParent is RectTransform hotbarFromInventory)
                return hotbarFromInventory;

            return null;
        }

        private static void DestroyLegacyMiningChargeHud()
        {
            UIManager uiManager = FindAnyObjectByType<UIManager>();
            Transform parent = uiManager != null ? uiManager.transform : null;
            if (parent == null)
                return;

            Transform legacy = parent.Find("MiningChargeHud");
            if (legacy != null)
                Object.Destroy(legacy.gameObject);
        }
    }
}
