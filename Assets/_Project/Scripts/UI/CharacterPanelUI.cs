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
        private Image xpFillImage;

        private CharacterStatBarRow healthBar;
        private CharacterStatBarRow energyBar;
        private CharacterStatBarRow staminaBar;
        private CharacterStatBarRow oxygenBar;
        private CharacterStatBarRow meleeBar;
        private CharacterStatBarRow rangedBar;
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
            environmentSection = null;
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
                $"Level {level}\nXP {xpProgress}/{xpRequired}\nSkill Points {skillPoints}\n" +
                $"Level stat bonus: +{Mathf.RoundToInt((statMult - 1f) * 100f)}% max vitals";

            if (xpFillImage != null)
                xpFillImage.fillAmount = progression != null ? progression.GetXpProgressNormalized() : 0f;

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
            }
            else
            {
                rangedBar.SetUnavailable("Ranged Damage");
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
                int loaded = ammo.GetActiveLoadedAmmo();
                ammoLine = $"\nLoaded ammo: {loaded}/{Mathf.Max(1, activeItem.magazineSize)}";
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
            RectTransform rootRect = panelRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = new Vector2(16f, 16f);
            rootRect.offsetMax = new Vector2(-16f, -16f);

            HorizontalLayoutGroup rootLayout = panelRoot.AddComponent<HorizontalLayoutGroup>();
            rootLayout.spacing = 14f;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = true;
            rootLayout.padding = new RectOffset(8, 8, 8, 8);

            GameObject infoColumn = CreateColumn(panelRoot.transform, flexibleWidth: 1f - StatsPanelWidthFraction);
            levelHeaderLabel = CreateSectionLabel(infoColumn.transform, 18);
            CreateXpBar(infoColumn.transform);
            BuildVitalsSection(infoColumn.transform);
            loadoutLabel = CreateSectionLabel(infoColumn.transform, 16);
            creditsLabel = CreateSectionLabel(infoColumn.transform, 16);
            unlocksLabel = CreateSectionLabel(infoColumn.transform, 14);

            BuildSurvivorPanel(panelRoot.transform);
        }

        private void BuildVitalsSection(Transform parent)
        {
            TextMeshProUGUI vitalsHeading = CreateSectionLabel(parent, 15);
            vitalsHeading.text = "Vitals";
            vitalsHeading.fontStyle = FontStyles.Bold;
            vitalsHeading.color = SurvivalPioneerUiPalette.RichFuchsia;
            vitalsHeading.alignment = TextAlignmentOptions.TopLeft;

            GameObject listHost = new GameObject("VitalsList", typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            listHost.transform.SetParent(parent, false);
            LayoutElement listLayout = listHost.GetComponent<LayoutElement>();
            // 6 rows now that Melee/Ranged damage live here alongside Health/Energy/Stamina/Oxygen.
            listLayout.minHeight = HudLayoutMetrics.Scaled(180f);

            VerticalLayoutGroup rowsGroup = listHost.GetComponent<VerticalLayoutGroup>();
            rowsGroup.spacing = 6f;
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
            panelLayout.minWidth = 420f;

            VerticalLayoutGroup panelGroup = panel.GetComponent<VerticalLayoutGroup>();
            panelGroup.padding = new RectOffset(16, 16, 14, 16);
            panelGroup.spacing = 10f;
            panelGroup.childControlWidth = true;
            panelGroup.childControlHeight = true;
            panelGroup.childForceExpandWidth = true;
            panelGroup.childForceExpandHeight = false;

            TextMeshProUGUI title = CreateSectionLabel(panel.transform, 22);
            title.text = "Survivor";
            title.fontStyle = FontStyles.Bold;
            title.color = SurvivalPioneerUiPalette.RichFuchsia;
            title.alignment = TextAlignmentOptions.Top;
            LayoutElement titleLayout = title.GetComponent<LayoutElement>();
            titleLayout.minHeight = 28f;
            titleLayout.preferredHeight = 28f;

            GameObject divider = new GameObject("Divider", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            divider.transform.SetParent(panel.transform, false);
            LayoutElement dividerLayout = divider.GetComponent<LayoutElement>();
            dividerLayout.preferredHeight = 1f;
            dividerLayout.minHeight = 1f;
            Image dividerImage = divider.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(dividerImage);
            dividerImage.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.RichFuchsia, 0.45f);

            GameObject environmentHost = new GameObject("CharacterEnvironmentSection", typeof(RectTransform), typeof(CharacterEnvironmentSection), typeof(LayoutElement));
            environmentHost.transform.SetParent(panel.transform, false);
            LayoutElement environmentLayout = environmentHost.GetComponent<LayoutElement>();
            environmentLayout.flexibleHeight = 1f;
            environmentLayout.minHeight = HudLayoutMetrics.Scaled(560f);
            environmentSection = environmentHost.GetComponent<CharacterEnvironmentSection>();
            environmentSection.Initialize();
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
            columnLayout.spacing = 10f;
            columnLayout.padding = new RectOffset(10, 10, 10, 10);
            columnLayout.childControlWidth = true;
            columnLayout.childControlHeight = true;
            columnLayout.childForceExpandWidth = true;
            columnLayout.childForceExpandHeight = false;

            return column;
        }

        private void CreateXpBar(Transform parent)
        {
            GameObject barRoot = new GameObject("XpBar", typeof(RectTransform), typeof(LayoutElement));
            barRoot.transform.SetParent(parent, false);
            LayoutElement layout = barRoot.GetComponent<LayoutElement>();
            layout.preferredHeight = 18f;

            Image bg = barRoot.AddComponent<Image>();
            bg.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.SlateGray, 0.95f);

            GameObject fillObject = new GameObject("Fill", typeof(RectTransform));
            fillObject.transform.SetParent(barRoot.transform, false);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            xpFillImage = fillObject.AddComponent<Image>();
            xpFillImage.color = SurvivalPioneerUiPalette.Gold;
            xpFillImage.type = Image.Type.Filled;
            xpFillImage.fillMethod = Image.FillMethod.Horizontal;
        }

        private static TextMeshProUGUI CreateSectionLabel(Transform parent, float fontSize)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelObject.transform.SetParent(parent, false);
            LayoutElement layout = labelObject.GetComponent<LayoutElement>();
            layout.minHeight = fontSize * 2.5f;
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
