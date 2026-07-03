using Project.Data;
using Project.Inventory;
using Project.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Crosshair plus ammo readout while a ranged weapon is drawn.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(120)]
    public class RangedCombatHud : MonoBehaviour
    {
        [SerializeField] private bool showCrosshair = true;
        [SerializeField] private bool showAmmoCounter = true;
        [SerializeField] private Color crosshairColor = new Color(1f, 1f, 1f, 0.82f);
        [SerializeField] private float crosshairSize = 14f;
        [SerializeField] private float crosshairGap = 5f;
        [SerializeField] private float crosshairThickness = 2f;
        [SerializeField] private float hotbarVerticalPadding = 6f;

        private EquipmentController equipment;
        private WeaponAmmoState ammoState;
        private InventorySystem inventory;
        private RangedCombatController rangedCombat;
        private TextMeshProUGUI ammoLabel;
        private RectTransform ammoRect;

        private void Awake()
        {
            equipment = GetComponent<EquipmentController>();
            ammoState = GetComponent<WeaponAmmoState>();
            inventory = GetComponent<InventorySystem>();
            rangedCombat = GetComponent<RangedCombatController>();
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

            RefreshAmmoLabel();
        }

        private void HandleSelectionChanged(int _)
        {
            RefreshAmmoLabel();
        }

        private void RefreshAmmoLabel()
        {
            if (!showAmmoCounter || ammoLabel == null)
                return;

            if (!ShouldShowHud(out ItemData weapon))
            {
                ammoLabel.gameObject.SetActive(false);
                return;
            }

            ammoLabel.gameObject.SetActive(true);
            int loaded = ammoState != null ? ammoState.GetActiveLoadedAmmo() : 0;
            int reserve = ammoState != null ? ammoState.GetReserveAmmoCount(weapon) : 0;
            int magazineSize = Mathf.Max(1, weapon.magazineSize);
            ammoLabel.text = reserve > 0
                ? $"AMMO {loaded}/{magazineSize}  (+{reserve})"
                : $"AMMO {loaded}/{magazineSize}";
            LayoutAboveHotbar();
        }

        private void LateUpdate()
        {
            if (showAmmoCounter && ammoLabel != null && ammoLabel.gameObject.activeSelf)
                LayoutAboveHotbar();
        }

        private void OnGUI()
        {
            if (!showCrosshair || !ShouldShowHud(out _))
                return;

            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;
            float spreadScale = rangedCombat != null && rangedCombat.IsAiming ? 0.75f : 1f;
            float size = crosshairSize * spreadScale;
            float gap = crosshairGap * spreadScale;

            DrawCrosshairLine(new Rect(centerX - gap - size, centerY - crosshairThickness * 0.5f, size, crosshairThickness));
            DrawCrosshairLine(new Rect(centerX + gap, centerY - crosshairThickness * 0.5f, size, crosshairThickness));
            DrawCrosshairLine(new Rect(centerX - crosshairThickness * 0.5f, centerY - gap - size, crosshairThickness, size));
            DrawCrosshairLine(new Rect(centerX - crosshairThickness * 0.5f, centerY + gap, crosshairThickness, size));
        }

        private bool ShouldShowHud(out ItemData weapon)
        {
            weapon = null;
            if (equipment == null || !equipment.HasActiveRangedWeapon())
                return false;

            weapon = equipment.DrawnWeaponItem;
            return weapon != null && weapon.IsRangedWeapon;
        }

        private void DrawCrosshairLine(Rect rect)
        {
            Color previous = GUI.color;
            GUI.color = crosshairColor;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void EnsureAmmoLabel()
        {
            if (ammoLabel != null)
                return;

            UIManager uiManager = FindAnyObjectByType<UIManager>();
            Transform parent = uiManager != null ? uiManager.transform : transform;

            GameObject labelObject = new GameObject("RangedAmmoLabel");
            labelObject.transform.SetParent(parent, false);

            ammoRect = labelObject.AddComponent<RectTransform>();
            ammoRect.anchorMin = new Vector2(0.5f, 0f);
            ammoRect.anchorMax = new Vector2(0.5f, 0f);
            ammoRect.pivot = new Vector2(0.5f, 0f);
            ammoRect.sizeDelta = new Vector2(180f, 28f);

            ammoLabel = labelObject.AddComponent<TextMeshProUGUI>();
            ammoLabel.alignment = TextAlignmentOptions.Center;
            ammoLabel.fontSize = 17f * HudLayoutMetrics.HudScale;
            ammoLabel.color = new Color(0.95f, 0.95f, 0.95f, 0.95f);
            ammoLabel.text = string.Empty;

            Outline outline = labelObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(1f, -1f);

            LayoutAboveHotbar();
        }

        private void LayoutAboveHotbar()
        {
            if (ammoRect == null)
                return;

            RectTransform hotbarRect = ResolveHotbarRect();
            if (hotbarRect == null)
            {
                ammoRect.anchoredPosition = new Vector2(0f, 118f * HudLayoutMetrics.HudScale);
                return;
            }

            ammoRect.anchorMin = hotbarRect.anchorMin;
            ammoRect.anchorMax = hotbarRect.anchorMax;
            ammoRect.pivot = hotbarRect.pivot;
            ammoRect.anchoredPosition = new Vector2(
                hotbarRect.anchoredPosition.x,
                hotbarRect.anchoredPosition.y + hotbarRect.sizeDelta.y + hotbarVerticalPadding * HudLayoutMetrics.HudScale);
        }

        private static RectTransform ResolveHotbarRect()
        {
            InventoryUI inventoryUi = FindAnyObjectByType<InventoryUI>();
            if (inventoryUi != null && inventoryUi.hotbarParent is RectTransform hotbarFromInventory)
                return hotbarFromInventory;

            return null;
        }
    }
}
