using Project.Survival.Exposure;
using Project.Data;
using Project.Inventory;
using Project.Pioneers;
using Project.Progression;
using Project.Survival;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public class CharacterPanelUI : MonoBehaviour
    {
        private const float StatsPanelWidthFraction = 0.44f;
        private const float CombatDamageReference = 100f;

        private Transform embeddedParent;
        private GameObject panelRoot;
        private TextMeshProUGUI levelHeaderLabel;
        private TextMeshProUGUI loadoutLabel;
        private TextMeshProUGUI creditsLabel;
        private TextMeshProUGUI unlocksLabel;
        private TextMeshProUGUI xpCountLabel;
        private RectTransform xpFillRect;
        private Image xpFillImage;
        private float displayedXpFill;
        private float targetXpFill;
        private const float XpFillLerpSpeed = 10f;

        private CharacterStatBarRow healthBar;
        private CharacterStatBarRow energyBar;
        private CharacterStatBarRow staminaBar;
        private CharacterStatBarRow oxygenBar;
        private CharacterStatBarRow meleeBar;
        private CharacterStatBarRow rangedBar;
        private CharacterStatBarRow accuracyBar;
        private CharacterEnvironmentSection environmentSection;

        private PlayerProgressionManager progression;
        private SurvivalStats survivalStats;
        private EquipmentController equipment;
        private WeaponAmmoState ammoState;
        private PioneerRosterManager roster;

        public void EmbedIn(Transform parent)
        {
            if (parent == null)
                return;

            embeddedParent = parent;
            progression = PlayerProgressionManager.EnsureExists();
            survivalStats = FindAnyObjectByType<SurvivalStats>();
            equipment = FindAnyObjectByType<EquipmentController>();
            ammoState = equipment != null ? equipment.GetComponent<WeaponAmmoState>() : null;
            roster = PioneerRosterManager.EnsureExists();
            EnsureBuilt(parent);
            SubscribeRefreshEvents();
            Refresh();
        }

        private void SubscribeRefreshEvents()
        {
            if (progression != null)
                progression.OnXpChanged += Refresh;

            if (roster != null)
                roster.OnRosterChanged += Refresh;

            if (equipment != null)
                equipment.OnSelectedHotbarChanged += HandleEquipmentChanged;

            if (ammoState != null)
                ammoState.OnAmmoChanged += Refresh;

            EnsureSurvivalStatsSubscription();
            EnsureExposureStatusSubscription();
        }

        private void EnsureExposureStatusSubscription()
        {
            ExposureStatusService service = ExposureStatusService.Instance;
            if (service == null)
                return;

            service.OnSnapshotChanged -= HandleExposureSnapshotChanged;
            service.OnSnapshotChanged += HandleExposureSnapshotChanged;
        }

        private void HandleExposureSnapshotChanged(ExposureStatusSnapshot snapshot)
        {
            Refresh();
        }

        private void EnsureSurvivalStatsSubscription()
        {
            SurvivalStats found = FindAnyObjectByType<SurvivalStats>();
            if (found == survivalStats)
                return;

            if (survivalStats != null)
                survivalStats.OnStatsChanged -= Refresh;

            survivalStats = found;
            if (survivalStats != null)
                survivalStats.OnStatsChanged += Refresh;
        }

        private void HandleEquipmentChanged(int _)
        {
            Refresh();
        }

        public void Unembed()
        {
            if (progression != null)
                progression.OnXpChanged -= Refresh;

            if (roster != null)
                roster.OnRosterChanged -= Refresh;

            if (equipment != null)
                equipment.OnSelectedHotbarChanged -= HandleEquipmentChanged;

            if (ammoState != null)
                ammoState.OnAmmoChanged -= Refresh;

            if (survivalStats != null)
                survivalStats.OnStatsChanged -= Refresh;

            ExposureStatusService service = ExposureStatusService.Instance;
            if (service != null)
                service.OnSnapshotChanged -= HandleExposureSnapshotChanged;

            if (environmentSection != null)
                environmentSection.Unembed();

            if (panelRoot != null)
                Destroy(panelRoot);

            panelRoot = null;
            embeddedParent = null;
            healthBar = null;
            energyBar = null;
            staminaBar = null;
            oxygenBar = null;
            meleeBar = null;
            rangedBar = null;
            accuracyBar = null;
            environmentSection = null;
            xpFillImage = null;
            xpFillRect = null;
            xpCountLabel = null;
            levelHeaderLabel = null;
            loadoutLabel = null;
            creditsLabel = null;
            unlocksLabel = null;
        }

        public void Refresh()
        {
            if (panelRoot == null)
                return;

            progression ??= PlayerProgressionManager.EnsureExists();
            survivalStats ??= FindAnyObjectByType<SurvivalStats>();
            equipment ??= FindAnyObjectByType<EquipmentController>();
            if (equipment != null && (ammoState == null || ammoState.gameObject != equipment.gameObject))
                ammoState = equipment.GetComponent<WeaponAmmoState>();
            roster ??= PioneerRosterManager.EnsureExists();
            EnsureSurvivalStatsSubscription();

            int level = progression != null ? progression.Level : 1;
            int xpProgress = progression != null ? progression.GetXpProgressInCurrentLevel() : 0;
            int xpRequired = progression != null ? progression.GetXpRequiredForNextLevel() : 100;
            int skillPoints = progression != null ? progression.UnspentSkillPoints : 0;
            float statMult = progression != null ? progression.GetLevelStatMultiplier() : 1f;

            levelHeaderLabel.text =
                $"Level {level}\nSkill Points {skillPoints}\n" +
                $"Level stat bonus: +{Mathf.RoundToInt((statMult - 1f) * 100f)}% max vitals";

            if (xpCountLabel != null)
            {
                xpCountLabel.text = xpRequired > 0
                    ? $"{xpProgress} / {xpRequired} XP"
                    : "MAX";
            }

            targetXpFill = progression != null ? progression.GetXpProgressNormalized() : 0f;
            // Snap on large jumps (panel open / level-up); small XP gains lerp in Update.
            if (Mathf.Abs(targetXpFill - displayedXpFill) > 0.35f)
            {
                displayedXpFill = targetXpFill;
                ApplyXpFillVisual(displayedXpFill);
            }
            else if (Mathf.Approximately(displayedXpFill, targetXpFill))
            {
                ApplyXpFillVisual(displayedXpFill);
            }

            RefreshStatBars(survivalStats, equipment);
            environmentSection?.RefreshFromStats(survivalStats);

            loadoutLabel.text = BuildLoadoutText(equipment, ammoState);

            float ac = roster != null ? roster.AetherCredits : 0f;
            creditsLabel.text = $"Aether Credits: {Mathf.RoundToInt(ac)}";

            unlocksLabel.text = LevelUnlockRegistry.BuildUnlockSummary(level);
        }

        private void RefreshStatBars(SurvivalStats stats, EquipmentController equip)
        {
            if (healthBar == null)
                return;

            if (stats != null)
            {
                healthBar.SetValues(stats.CurrentHealth, stats.maxHealth);
                energyBar.SetValues(stats.CurrentEnergy, stats.maxEnergy);
                staminaBar.SetValues(stats.CurrentStamina, stats.maxStamina);
                oxygenBar.SetValues(stats.CurrentOxygen, stats.maxOxygen);
            }
            else
            {
                healthBar.SetUnavailable("Health");
                energyBar.SetUnavailable("Energy");
                staminaBar.SetUnavailable("Stamina");
                oxygenBar.SetUnavailable("Oxygen");
            }

            ItemData weapon = equip != null ? equip.EquippedItem : null;
            bool hasMelee = weapon != null && weapon.itemType == ItemType.MeleeWeapon;
            if (hasMelee)
            {
                float damage = weapon.GetAverageMeleeDamage();
                meleeBar.SetValues(damage, CombatDamageReference, FormatStatValue(damage));
            }
            else
            {
                meleeBar.SetUnavailable("Melee Damage");
            }

            bool hasRanged = weapon != null && weapon.IsRangedWeapon;
            if (hasRanged)
            {
                float damage = weapon.GetAverageRangedDamage();
                rangedBar.SetValues(damage, CombatDamageReference, FormatStatValue(damage));
                if (accuracyBar != null)
                {
                    float accuracy = weapon.GetEffectiveAccuracy();
                    accuracyBar.SetValues(accuracy, 100f, FormatStatValue(accuracy));
                }
            }
            else
            {
                rangedBar.SetUnavailable("Ranged Damage");
                accuracyBar?.SetUnavailable("Accuracy");
            }
        }

        private static string FormatStatValue(float value)
        {
            return Mathf.Approximately(value, Mathf.Round(value))
                ? Mathf.RoundToInt(value).ToString()
                : value.ToString("0.#");
        }

        private static string BuildLoadoutText(EquipmentController equip, WeaponAmmoState ammo)
        {
            if (equip == null)
                return "Loadout unavailable.";

            ItemData activeItem = equip.EquippedItem;
            string activeWeapon = FormatItem(activeItem);
            string secondary = FormatItem(equip.SecondaryWeaponItem);
            string tool = FormatItem(equip.ActiveToolItem);
            string ammoLine = string.Empty;
            if (activeItem != null && activeItem.IsRangedWeapon && ammo != null)
            {
                if (activeItem.isMiningTool)
                {
                    int charge = ammo.GetMiningChargePercent(equip.ActiveWeaponHotbarSlot);
                    ammoLine = $"\nMining charge: {charge}%";
                }
                else
                {
                    int loaded = ammo.GetActiveLoadedAmmo();
                    ammoLine = $"\nLoaded ammo: {loaded}/{WeaponAmmoState.GetMagazineCapacity(activeItem)}";
                }
            }

            return
                $"Active weapon: {activeWeapon}\n" +
                $"Secondary weapon: {secondary}\n" +
                $"Active tool: {tool}{ammoLine}\n" +
                $"Suit: None equipped (upgrades coming soon)";
        }

        private static string FormatItem(ItemData item) => item != null ? item.itemName : "Empty";

        private void EnsureBuilt(Transform parent)
        {
            if (panelRoot != null)
                return;

            panelRoot = new GameObject("CharacterPanel", typeof(RectTransform));
            panelRoot.transform.SetParent(parent, false);
            JournalPanelLayout.StretchFill(panelRoot.GetComponent<RectTransform>());

            HorizontalLayoutGroup rootLayout = panelRoot.AddComponent<HorizontalLayoutGroup>();
            JournalPanelLayout.ApplyRootHorizontalLayout(rootLayout);

            GameObject infoColumn = CreateColumn(panelRoot.transform, flexibleWidth: 1f - StatsPanelWidthFraction);
            // Level header — slightly condensed vs previous oversized block.
            levelHeaderLabel = CreateSectionLabel(infoColumn.transform, 22);
            LayoutElement levelLayout = levelHeaderLabel.GetComponent<LayoutElement>();
            if (levelLayout != null)
                levelLayout.minHeight = 56f;
            CreateXpBar(infoColumn.transform);
            BuildVitalsSection(infoColumn.transform);
            loadoutLabel = CreateSectionLabel(infoColumn.transform, JournalPanelLayout.BodyFontSize + 1f);
            creditsLabel = CreateSectionLabel(infoColumn.transform, JournalPanelLayout.BodyFontSize + 1f);
            unlocksLabel = CreateSectionLabel(infoColumn.transform, JournalPanelLayout.SecondaryFontSize);

            BuildSurvivorPanel(panelRoot.transform);
        }

        private void BuildVitalsSection(Transform parent)
        {
            TextMeshProUGUI vitalsHeading = CreateSectionLabel(parent, JournalPanelLayout.HeaderFontSize);
            vitalsHeading.text = "Vitals";
            JournalPanelLayout.ApplyHeaderStyle(vitalsHeading);
            vitalsHeading.alignment = TextAlignmentOptions.TopLeft;

            GameObject listHost = new GameObject("VitalsList", typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            listHost.transform.SetParent(parent, false);
            LayoutElement listLayout = listHost.GetComponent<LayoutElement>();
            // 7 rows: Health/Energy/Stamina/Oxygen + Melee/Ranged/Accuracy.
            listLayout.minHeight = HudLayoutMetrics.Scaled(190f);

            VerticalLayoutGroup rowsGroup = listHost.GetComponent<VerticalLayoutGroup>();
            rowsGroup.spacing = 4f;
            rowsGroup.childControlWidth = true;
            rowsGroup.childControlHeight = true;
            rowsGroup.childForceExpandWidth = true;
            rowsGroup.childForceExpandHeight = false;

            healthBar = new CharacterStatBarRow(listHost.transform, "+", "Health", SurvivalPioneerUiPalette.RichFuchsia);
            energyBar = new CharacterStatBarRow(listHost.transform, "E", "Energy", SurvivalPioneerUiPalette.RichFuchsia);
            staminaBar = new CharacterStatBarRow(listHost.transform, "S", "Stamina", SurvivalPioneerUiPalette.RichFuchsia);
            oxygenBar = new CharacterStatBarRow(listHost.transform, "O", "Oxygen", SurvivalPioneerUiPalette.RichFuchsia);
            meleeBar = new CharacterStatBarRow(listHost.transform, "M", "Melee Damage", SurvivalPioneerUiPalette.RichFuchsia);
            rangedBar = new CharacterStatBarRow(listHost.transform, "R", "Ranged Damage", SurvivalPioneerUiPalette.RichFuchsia);
            accuracyBar = new CharacterStatBarRow(listHost.transform, "A", "Accuracy", SurvivalPioneerUiPalette.Gold);
        }

        private void BuildSurvivorPanel(Transform parent)
        {
            GameObject panel = new GameObject("CharacterStatsPanel", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(parent, false);

            Image panelBg = panel.GetComponent<Image>();
            SurvivalPioneerUiPalette.ApplyPanelShellBackground(panelBg, 0.98f);
            SurvivalPioneerUiPalette.ApplyFuchsiaTrim(panel, new Vector2(1.5f, -1.5f));

            LayoutElement panelLayout = panel.GetComponent<LayoutElement>();
            panelLayout.flexibleWidth = StatsPanelWidthFraction;
            panelLayout.flexibleHeight = 1f;
            // Wide enough for the now full-size (non-compact) temp + hazard gauges reused from the
            // player's own hotbar HUD, plus section/panel padding.
            panelLayout.minWidth = 400f;

            VerticalLayoutGroup panelGroup = panel.GetComponent<VerticalLayoutGroup>();
            panelGroup.padding = new RectOffset(12, 12, 10, 12);
            panelGroup.spacing = JournalPanelLayout.SectionSpacing;
            panelGroup.childControlWidth = true;
            panelGroup.childControlHeight = true;
            panelGroup.childForceExpandWidth = true;
            panelGroup.childForceExpandHeight = false;

            // No "Survivor" section title — Character tab on the journal rail identifies this panel.

            GameObject environmentHost = new GameObject("CharacterEnvironmentSection", typeof(RectTransform), typeof(CharacterEnvironmentSection), typeof(LayoutElement));
            environmentHost.transform.SetParent(panel.transform, false);
            LayoutElement environmentLayout = environmentHost.GetComponent<LayoutElement>();
            environmentLayout.flexibleHeight = 1f;
            environmentLayout.minHeight = HudLayoutMetrics.Scaled(720f);
            environmentSection = environmentHost.GetComponent<CharacterEnvironmentSection>();
            environmentSection.Initialize();

            if (panel.GetComponent<RectMask2D>() == null)
                panel.AddComponent<RectMask2D>();
        }

        private static GameObject CreateColumn(Transform parent, float flexibleWidth)
        {
            GameObject column = new GameObject("Column", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            column.transform.SetParent(parent, false);

            Image bg = column.GetComponent<Image>();
            bg.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.CharcoalGray, 0.55f);

            LayoutElement layout = column.GetComponent<LayoutElement>();
            layout.flexibleWidth = flexibleWidth;
            layout.flexibleHeight = 1f;
            layout.minWidth = 180f;

            VerticalLayoutGroup columnLayout = column.AddComponent<VerticalLayoutGroup>();
            columnLayout.spacing = JournalPanelLayout.SectionSpacing;
            columnLayout.padding = JournalPanelLayout.PanelPaddingRect;
            columnLayout.childControlWidth = true;
            columnLayout.childControlHeight = true;
            columnLayout.childForceExpandWidth = true;
            columnLayout.childForceExpandHeight = false;

            return column;
        }

        private void Update()
        {
            if (panelRoot == null || xpFillRect == null)
                return;

            if (Mathf.Approximately(displayedXpFill, targetXpFill))
                return;

            displayedXpFill = Mathf.MoveTowards(
                displayedXpFill,
                targetXpFill,
                XpFillLerpSpeed * Time.unscaledDeltaTime);
            ApplyXpFillVisual(displayedXpFill);
        }

        private void ApplyXpFillVisual(float normalized)
        {
            if (xpFillRect == null)
                return;

            normalized = Mathf.Clamp01(normalized);
            xpFillRect.anchorMin = Vector2.zero;
            xpFillRect.anchorMax = new Vector2(normalized, 1f);
            xpFillRect.pivot = new Vector2(0f, 0.5f);
            xpFillRect.anchoredPosition = Vector2.zero;
            xpFillRect.offsetMin = new Vector2(2f, 2f);
            xpFillRect.offsetMax = new Vector2(-2f, -2f);
        }

        private void CreateXpBar(Transform parent)
        {
            GameObject barRoot = new GameObject("XpBar", typeof(RectTransform), typeof(LayoutElement));
            barRoot.transform.SetParent(parent, false);
            LayoutElement layout = barRoot.GetComponent<LayoutElement>();
            layout.preferredHeight = 18f;
            layout.minHeight = 18f;

            Image bg = barRoot.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(bg);
            bg.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.SlateGray, 0.95f);
            bg.raycastTarget = false;

            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(barRoot.transform, false);
            xpFillRect = fillObject.GetComponent<RectTransform>();
            xpFillRect.anchorMin = Vector2.zero;
            xpFillRect.anchorMax = Vector2.one;
            xpFillRect.pivot = new Vector2(0f, 0.5f);
            xpFillRect.offsetMin = new Vector2(2f, 2f);
            xpFillRect.offsetMax = new Vector2(-2f, -2f);
            xpFillImage = fillObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(xpFillImage);
            xpFillImage.color = SurvivalPioneerUiPalette.Gold;
            xpFillImage.raycastTarget = false;
            xpFillImage.preserveAspect = false;

            GameObject countObject = new GameObject("XpCount", typeof(RectTransform));
            countObject.transform.SetParent(barRoot.transform, false);
            RectTransform countRect = countObject.GetComponent<RectTransform>();
            countRect.anchorMin = Vector2.zero;
            countRect.anchorMax = Vector2.one;
            countRect.offsetMin = Vector2.zero;
            countRect.offsetMax = Vector2.zero;
            xpCountLabel = countObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(xpCountLabel);
            xpCountLabel.fontSize = 12f;
            xpCountLabel.fontStyle = FontStyles.Bold;
            xpCountLabel.alignment = TextAlignmentOptions.Center;
            xpCountLabel.color = SurvivalPioneerUiPalette.WarmOffWhite;
            xpCountLabel.raycastTarget = false;
            xpCountLabel.overflowMode = TextOverflowModes.Ellipsis;
            xpCountLabel.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private static TextMeshProUGUI CreateSectionLabel(Transform parent, float fontSize)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelObject.transform.SetParent(parent, false);
            LayoutElement layout = labelObject.GetComponent<LayoutElement>();
            layout.minHeight = Mathf.Max(fontSize * 1.6f, 22f);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            label.fontSize = fontSize;
            label.color = SurvivalPioneerUiPalette.BodyText;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }
    }
}
