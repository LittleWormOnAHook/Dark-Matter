using System;
using System.Collections.Generic;
using Project.Achievements;
using Project.Audio;
using Project.Companions;
using Project.Crafting;
using Project.Data;
using Project.Echoes;
using Project.Features.Jetpack;
using Project.Inventory;
using Project.Pet;
using Project.Pioneers;
using Project.Progression;
using Project.Survival;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    public partial class DMUiToolkitMenus
    {
        private VisualElement petBody;
        private VisualElement companionsBody;
        private VisualElement characterBody;
        private VisualElement blueprintsBody;
        private VisualElement skillsBody;
        private VisualElement echoesBody;
        private VisualElement achievementsBody;
        private VisualElement petGrid;
        private Label petSummary;
        private Label companionsSummary;
        private ScrollView companionsList;
        private Label companionsDetail;
        private readonly Button[] trioButtons = new Button[PioneerRosterManager.ExpeditionTrioSize];
        private Label characterLevel;
        private VisualElement characterXpFill;
        private Label characterXpCount;
        private VisualElement characterVitals;
        private Label characterLoadout;
        private Label characterCredits;
        private Label characterUnlocks;
        private readonly List<VitalRow> vitalRows = new List<VitalRow>();
        private Label blueprintsSummary;
        private ScrollView blueprintsPending;
        private ScrollView blueprintsLearned;
        private Label learnedHeading;
        private VisualElement blueprintsLibTabs;
        private ScrollView blueprintsLibrary;
        private Label blueprintsDetail;
        private Label blueprintCraftMeta;
        private VisualElement blueprintCraftIngs;
        private Label blueprintCraftOutput;
        private SliderInt blueprintCraftAmount;
        private Label blueprintCraftAmountValue;
        private Button blueprintCraftConfirm;
        private readonly Dictionary<BlueprintLibraryGroup, Button> blueprintTabs =
            new Dictionary<BlueprintLibraryGroup, Button>();
        private BlueprintLibraryGroup blueprintLibraryGroup = BlueprintLibraryGroup.All;
        private bool blueprintCraftWired;

        private enum BlueprintLibraryGroup
        {
            All,
            Cooking,
            Workbench,
            Consumables,
            Weapons,
            Ammo,
            Resources,
            Other
        }
        private Label skillsSummary;
        private readonly Dictionary<SkillTreeCategory, Button> skillCats = new Dictionary<SkillTreeCategory, Button>();
        private ScrollView skillsList;
        private Label skillsDetailTitle;
        private Label skillsDetailBody;
        private Button skillsAllocate;
        private VisualElement skillsCard;
        private VisualElement skillsCardArt;
        private Label skillsCardCaption;
        private ScrollView echoesChronicle;
        private ScrollView echoesBuffs;
        private ScrollView echoesSignals;
        private ScrollView echoesDispositions;
        private VisualElement achievementsCats;
        private Label achievementsSummary;
        private VisualElement achievementsGrid;

        private PetManager boundPets;
        private CraftingManager boundCrafting;
        private PlayerProgressionManager boundProgression;
        private AchievementManager boundAchievements;
        private SurvivalStats boundSurvival;
        private EquipmentController boundEquipment;
        private WeaponAmmoState boundAmmo;
        private DMJetpackController boundJetpack;
        private bool extraEventsHooked;

        private string selectedPioneerId;
        private string selectedRecipeId;
        private bool selectedRecipePending;
        private SkillTreeCategory skillsCategory = SkillTreeCategory.Player;
        private string selectedSkillId;
        private AchievementCategory? selectedAchievementCategory;

        private sealed class VitalRow
        {
            public string Id;
            public Label Label;
            public VisualElement Fill;
            public Label Value;
        }

        private void BindExtraPanels(VisualElement tree)
        {
            if (tree == null)
                return;

            petBody = tree.Q<VisualElement>("pet-body");
            companionsBody = tree.Q<VisualElement>("companions-body");
            characterBody = tree.Q<VisualElement>("character-body");
            blueprintsBody = tree.Q<VisualElement>("blueprints-body");
            skillsBody = tree.Q<VisualElement>("skills-body");
            echoesBody = tree.Q<VisualElement>("echoes-body");
            achievementsBody = tree.Q<VisualElement>("achievements-body");
            petGrid = tree.Q<VisualElement>("pet-grid");
            petSummary = tree.Q<Label>("pet-summary");
            companionsSummary = tree.Q<Label>("companions-summary");
            companionsList = tree.Q<ScrollView>("companions-list");
            companionsDetail = tree.Q<Label>("companions-detail");
            characterLevel = tree.Q<Label>("character-level");
            characterXpFill = tree.Q<VisualElement>("character-xp-fill");
            characterXpCount = tree.Q<Label>("character-xp-count");
            characterVitals = tree.Q<VisualElement>("character-vitals");
            characterLoadout = tree.Q<Label>("character-loadout");
            characterCredits = tree.Q<Label>("character-credits");
            characterUnlocks = tree.Q<Label>("character-unlocks");
            blueprintsSummary = tree.Q<Label>("blueprints-summary");
            blueprintsPending = tree.Q<ScrollView>("blueprints-pending");
            blueprintsLearned = tree.Q<ScrollView>("blueprints-learned");
            learnedHeading = tree.Q<Label>("learned-heading");
            blueprintsLibTabs = tree.Q<VisualElement>("blueprints-lib-tabs");
            blueprintsLibrary = tree.Q<ScrollView>("blueprints-library");
            blueprintsDetail = tree.Q<Label>("blueprints-detail");
            blueprintCraftMeta = tree.Q<Label>("bp-craft-meta");
            blueprintCraftIngs = tree.Q<VisualElement>("bp-craft-ings");
            blueprintCraftOutput = tree.Q<Label>("bp-craft-output");
            blueprintCraftAmount = tree.Q<SliderInt>("bp-craft-amount");
            blueprintCraftAmountValue = tree.Q<Label>("bp-craft-amount-value");
            blueprintCraftConfirm = tree.Q<Button>("bp-craft-confirm");
            BindBlueprintLibraryTabs(tree);
            WireBlueprintCraft();
            DMUiToolkitOverlayDocument.SetShown(learnedHeading, false);
            DMUiToolkitOverlayDocument.SetShown(blueprintsLearned, false);
            skillsSummary = tree.Q<Label>("skills-summary");
            skillsList = tree.Q<ScrollView>("skills-list");
            skillsDetailTitle = tree.Q<Label>("skills-detail-title");
            skillsDetailBody = tree.Q<Label>("skills-detail-body");
            skillsAllocate = tree.Q<Button>("skills-allocate");
            skillsCard = tree.Q<VisualElement>("skills-card");
            skillsCardArt = tree.Q<VisualElement>("skills-card-art");
            skillsCardCaption = tree.Q<Label>("skills-card-caption");
            echoesChronicle = tree.Q<ScrollView>("echoes-chronicle");
            echoesBuffs = tree.Q<ScrollView>("echoes-buffs");
            echoesSignals = tree.Q<ScrollView>("echoes-signals");
            echoesDispositions = tree.Q<ScrollView>("echoes-dispositions");
            achievementsCats = tree.Q<VisualElement>("achievements-cats");
            achievementsSummary = tree.Q<Label>("achievements-summary");
            achievementsGrid = tree.Q<VisualElement>("achievements-grid");

            for (int i = 0; i < trioButtons.Length; i++)
            {
                Button slot = tree.Q<Button>("trio-slot-" + i);
                trioButtons[i] = slot;
                if (slot == null)
                    continue;
                slot.userData = i;
                slot.UnregisterCallback<ClickEvent>(OnTrioSlotClicked);
                slot.RegisterCallback<ClickEvent>(OnTrioSlotClicked);
            }

            BindSkillCat("skill-cat-player", SkillTreeCategory.Player, tree);
            BindSkillCat("skill-cat-melee", SkillTreeCategory.Melee, tree);
            BindSkillCat("skill-cat-pistols", SkillTreeCategory.Pistols, tree);
            BindSkillCat("skill-cat-rifles", SkillTreeCategory.Rifles, tree);
            BindSkillCat("skill-cat-survival", SkillTreeCategory.Survival, tree);

            if (skillsAllocate != null)
            {
                skillsAllocate.clicked -= OnAllocateSkillClicked;
                skillsAllocate.clicked += OnAllocateSkillClicked;
            }

            EnsureVitalRows();
            EnsureAchievementCategoryTabs();
            BindSkillsHex(tree);
            BindCompanionsExtras(tree);
            BindPetBoard(tree);
            BindInventoryStorage(tree);
        }

        private void BindSkillCat(string name, SkillTreeCategory category, VisualElement tree)
        {
            Button button = tree.Q<Button>(name);
            if (button == null)
                return;

            skillCats[category] = button;
            button.userData = category;
            button.UnregisterCallback<ClickEvent>(OnSkillCatClicked);
            button.RegisterCallback<ClickEvent>(OnSkillCatClicked);
        }

        private void ShowExtraPanel(JournalWindowId window)
        {
            DMUiToolkitOverlayDocument.SetShown(petBody, window == JournalWindowId.Pet);
            DMUiToolkitOverlayDocument.SetShown(companionsBody, window == JournalWindowId.Pioneers);
            DMUiToolkitOverlayDocument.SetShown(characterBody, window == JournalWindowId.Character);
            DMUiToolkitOverlayDocument.SetShown(blueprintsBody, window == JournalWindowId.Recipes);
            DMUiToolkitOverlayDocument.SetShown(skillsBody, window == JournalWindowId.Skills);
            DMUiToolkitOverlayDocument.SetShown(echoesBody, window == JournalWindowId.Echoes);
            DMUiToolkitOverlayDocument.SetShown(achievementsBody, window == JournalWindowId.Achievements);
        }

        private void RefreshExtraPanel(JournalWindowId window)
        {
            switch (window)
            {
                case JournalWindowId.Pet:
                    RefreshPets();
                    break;
                case JournalWindowId.Pioneers:
                    RefreshCompanions();
                    break;
                case JournalWindowId.Character:
                    RefreshCharacter();
                    break;
                case JournalWindowId.Recipes:
                    RefreshBlueprints();
                    break;
                case JournalWindowId.Skills:
                    RefreshSkills();
                    break;
                case JournalWindowId.Echoes:
                    RefreshEchoes();
                    break;
                case JournalWindowId.Achievements:
                    RefreshAchievements();
                    break;
            }
        }

        private void BindExtraGameplay()
        {
            if (boundPets == null)
            {
                PetManager pets = PetManager.Instance ?? FindAnyObjectByType<PetManager>();
                if (pets != null)
                {
                    boundPets = pets;
                    boundPets.OnPetsChanged -= RefreshPets;
                    boundPets.OnPetsChanged += RefreshPets;
                }
            }

            if (boundCrafting == null)
            {
                CraftingManager crafting = CraftingManager.Instance ?? FindAnyObjectByType<CraftingManager>();
                if (crafting != null)
                {
                    boundCrafting = crafting;
                    boundCrafting.OnBlueprintsChanged -= RefreshBlueprints;
                    boundCrafting.OnPendingScrollsChanged -= RefreshBlueprints;
                    boundCrafting.OnBlueprintsChanged += RefreshBlueprints;
                    boundCrafting.OnPendingScrollsChanged += RefreshBlueprints;
                }
            }

            if (boundProgression == null)
            {
                PlayerProgressionManager progression = PlayerProgressionManager.EnsureExists();
                if (progression != null)
                {
                    boundProgression = progression;
                    boundProgression.OnXpChanged -= HandleProgressionChanged;
                    boundProgression.OnXpChanged += HandleProgressionChanged;
                }
            }

            if (boundAchievements == null)
            {
                AchievementManager achievements = AchievementManager.EnsureExists();
                if (achievements != null)
                {
                    boundAchievements = achievements;
                    boundAchievements.OnProgressUpdated -= HandleAchievementChanged;
                    boundAchievements.OnAchievementUnlocked -= HandleAchievementChanged;
                    boundAchievements.OnProgressUpdated += HandleAchievementChanged;
                    boundAchievements.OnAchievementUnlocked += HandleAchievementChanged;
                }
            }

            if (boundSurvival == null)
            {
                SurvivalStats survival = FindAnyObjectByType<SurvivalStats>();
                if (survival != null)
                {
                    boundSurvival = survival;
                    boundSurvival.OnStatsChanged -= RefreshCharacter;
                    boundSurvival.OnStatsChanged += RefreshCharacter;
                }
            }

            if (boundEquipment == null)
            {
                EquipmentController equipment = FindAnyObjectByType<EquipmentController>();
                if (equipment != null)
                {
                    boundEquipment = equipment;
                    boundEquipment.OnSelectedHotbarChanged -= HandleEquipmentChanged;
                    boundEquipment.OnSelectedHotbarChanged += HandleEquipmentChanged;
                }
            }

            if (boundEquipment != null)
                boundAmmo = boundEquipment.GetComponent<WeaponAmmoState>();

            if (boundJetpack == null)
                boundJetpack = FindAnyObjectByType<DMJetpackController>();

            if (!extraEventsHooked)
            {
                EchoSignalRegistry.OnSignalsChanged -= RefreshEchoes;
                EchoSignalRegistry.OnSignalsChanged += RefreshEchoes;
                extraEventsHooked = true;
            }
        }

        private void UnhookExtraGameplay()
        {
            if (boundPets != null)
            {
                boundPets.OnPetsChanged -= RefreshPets;
                boundPets = null;
            }

            if (boundCrafting != null)
            {
                boundCrafting.OnBlueprintsChanged -= RefreshBlueprints;
                boundCrafting.OnPendingScrollsChanged -= RefreshBlueprints;
                boundCrafting = null;
            }

            if (boundProgression != null)
            {
                boundProgression.OnXpChanged -= HandleProgressionChanged;
                boundProgression = null;
            }

            if (boundAchievements != null)
            {
                boundAchievements.OnProgressUpdated -= HandleAchievementChanged;
                boundAchievements.OnAchievementUnlocked -= HandleAchievementChanged;
                boundAchievements = null;
            }

            if (boundSurvival != null)
            {
                boundSurvival.OnStatsChanged -= RefreshCharacter;
                boundSurvival = null;
            }

            if (boundEquipment != null)
            {
                boundEquipment.OnSelectedHotbarChanged -= HandleEquipmentChanged;
                boundEquipment = null;
            }

            boundAmmo = null;
            boundJetpack = null;
            EchoSignalRegistry.OnSignalsChanged -= RefreshEchoes;
            extraEventsHooked = false;
        }

        private void HandleProgressionChanged()
        {
            if (!menusVisible)
                return;
            RefreshCharacter();
            RefreshSkills();
        }

        private void HandleEquipmentChanged(int _)
        {
            if (menusVisible)
                RefreshCharacter();
        }

        private void HandleAchievementChanged(AchievementProgress progress, AchievementDefinition definition)
        {
            if (menusVisible)
                RefreshAchievements();
        }

        private void RefreshPets()
        {
            RebuildPetBoard();
        }

        private void RefreshCompanions()
        {
            if (companionsList == null)
                return;

            companionsList.Clear();
            if (companionsList.contentContainer != null)
            {
                companionsList.contentContainer.style.flexDirection = FlexDirection.Row;
                companionsList.contentContainer.style.flexWrap = Wrap.Wrap;
                companionsList.contentContainer.style.alignItems = Align.FlexStart;
            }
            boundRoster ??= PioneerRosterManager.EnsureExists();
            if (boundRoster == null)
            {
                companionsList.Add(MakeEmpty("No roster manager."));
                return;
            }

            ColonistAggregateState colonists = boundRoster.GetColonistState();
            if (companionsSummary != null)
            {
                companionsSummary.text =
                    "Total " + boundRoster.GetTotalPioneerCount() + "/" + PioneerRosterManager.MaxTotalPioneers +
                    "  ·  Skilled " + boundRoster.SkilledPioneers.Count + "/" + PioneerRosterManager.MaxSkilledPioneers +
                    "  ·  Workers " + colonists.workerCount +
                    "  ·  Available " + colonists.AvailableWorkers +
                    "  ·  Injured " + colonists.injuredCount;
            }

            IReadOnlyList<SkilledPioneerRecord> skilled = boundRoster.SkilledPioneers;
            if (skilled == null || skilled.Count == 0)
            {
                companionsList.Add(MakeEmpty("No skilled pioneers recruited yet."));
                selectedPioneerId = null;
            }
            else
            {
                if (string.IsNullOrEmpty(selectedPioneerId))
                    selectedPioneerId = skilled[0].id;

                for (int i = 0; i < skilled.Count; i++)
                {
                    SkilledPioneerRecord record = skilled[i];
                    if (record == null)
                        continue;
                    companionsList.Add(MakeCompanionRowWithPortrait(record));
                }
            }

            ApplyCompanionDetail();
            ApplyTrioSlots();
            RefreshCompanionsExtras();
        }

        private void ApplyCompanionDetail()
        {
            if (companionsDetail == null || boundRoster == null)
                return;

            SkilledPioneerRecord record = boundRoster.FindSkilledById(selectedPioneerId);
            if (record == null)
            {
                companionsDetail.text = "Select a skilled companion from the roster.";
                return;
            }

            string traits = PioneerTraitUtility.FormatTraitList(record.traitIds);
            string passives = PioneerTraitUtility.FormatTraitList(record.passiveAbilityIds);
            string skills = record.learnedSkills == null || record.learnedSkills.Length == 0
                ? "None"
                : PioneerTraitUtility.FormatTraitList(record.learnedSkills);
            string disposition = record.Kind == PioneerKind.RescuedEcho
                ? PioneerTraitUtility.GetDispositionLabel(record.Disposition)
                : "N/A";
            string status = record.isInExpeditionTrio
                ? "Expedition Trio"
                : record.WorkState == PioneerWorkState.Injured ? "Injured" : "In Roster";

            companionsDetail.text =
                PioneerUiLabels.GetDisplayName(record) + "\n" +
                SkilledPioneerClassUtility.ToDisplayName(record.pioneerClass) + "  ·  Lv " + record.level + "\n" +
                CompanionHealthLookup.FormatHealthLine(record.id) + "\n" +
                "Rad " + record.radiationResistance.ToString("P0") +
                "  ·  Exp " + record.expeditionEfficiency.ToString("P0") +
                "  ·  Syn " + record.combatSynergy.ToString("P0") + "\n" +
                "Disposition " + disposition + "  ·  " + status + "\n\n" +
                "Traits\n" + traits + "\n\nPassives\n" + passives + "\n\nLearned skills\n" + skills +
                (string.IsNullOrEmpty(record.backstory) ? string.Empty : "\n\nProfile\n" + record.backstory);
        }

        private void ApplyTrioSlots()
        {
            if (boundRoster == null)
                return;

            for (int i = 0; i < trioButtons.Length; i++)
            {
                Button button = trioButtons[i];
                if (button == null)
                    continue;

                SkilledPioneerRecord record = boundRoster.GetExpeditionTrioRecordAtSlot(i);
                bool filled = record != null;
                button.EnableInClassList("dmg-trio-slot--filled", filled);
                ApplyPioneerSprite(button, record);
                Label initials = button.Q<Label>("trio-initials");
                if (initials == null)
                {
                    initials = new Label();
                    initials.name = "trio-initials";
                    initials.AddToClassList("dmg-companion-row-initials");
                    initials.pickingMode = PickingMode.Ignore;
                    button.Add(initials);
                }
                bool hasSprite = record != null && PioneerPortraitResolver.Resolve(record) != null;
                if (filled && !hasSprite)
                    initials.text = PioneerPortraitUi.BuildInitials(PioneerUiLabels.GetDisplayName(record));
                else
                    initials.text = filled ? string.Empty : (i + 1).ToString();
                button.text = string.Empty;
            }
        }

        private void OnTrioSlotClicked(ClickEvent evt)
        {
            if (companionIgnoreClick)
            {
                companionIgnoreClick = false;
                return;
            }

            if (evt.currentTarget is not Button button || button.userData is not int slot)
                return;

            evt.StopPropagation();
            GameAudioManager.Instance?.PlayUiHoverTick();
            boundRoster ??= PioneerRosterManager.EnsureExists();
            if (boundRoster == null)
                return;

            SkilledPioneerRecord existing = boundRoster.GetExpeditionTrioRecordAtSlot(slot);
            string nextId = string.Empty;
            if (existing == null)
                nextId = selectedPioneerId ?? string.Empty;
            else if (!string.IsNullOrEmpty(selectedPioneerId) && existing.id != selectedPioneerId)
                nextId = selectedPioneerId;

            if (!boundRoster.TryAssignTrioSlot(slot, nextId, out string error) && !string.IsNullOrEmpty(error))
                PickupToastUI.Show(error);

            RefreshCompanions();
        }

        private void EnsureVitalRows()
        {
            if (characterVitals == null || vitalRows.Count > 0)
                return;

            AddVital("health", "Health");
            AddVital("energy", "Energy");
            AddVital("stamina", "Stamina");
            AddVital("oxygen", "Oxygen");
            AddVital("jet", "Jet Fuel");
            AddVital("melee", "Melee");
            AddVital("ranged", "Ranged");
            AddVital("accuracy", "Accuracy");
        }

        private void AddVital(string id, string label)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("dmg-vital-row");
            Label name = new Label(label);
            name.AddToClassList("dmg-vital-label");
            VisualElement track = new VisualElement();
            track.AddToClassList("dmg-vital-track");
            VisualElement fill = new VisualElement();
            fill.AddToClassList("dmg-vital-fill");
            track.Add(fill);
            Label value = new Label("--");
            value.AddToClassList("dmg-vital-value");
            row.Add(name);
            row.Add(track);
            row.Add(value);
            characterVitals.Add(row);
            vitalRows.Add(new VitalRow { Id = id, Label = name, Fill = fill, Value = value });
        }

        private static void SetVital(VitalRow row, float current, float max, string text)
        {
            if (row == null)
                return;
            float n = max > 0.001f ? Mathf.Clamp01(current / max) : 0f;
            row.Fill.style.width = Length.Percent(n * 100f);
            row.Value.text = text;
        }

        private static void SetVitalUnavailable(VitalRow row, string label)
        {
            if (row == null)
                return;
            row.Label.text = label;
            row.Fill.style.width = Length.Percent(0f);
            row.Value.text = "--";
        }

        private VitalRow FindVital(string id)
        {
            for (int i = 0; i < vitalRows.Count; i++)
            {
                if (vitalRows[i].Id == id)
                    return vitalRows[i];
            }

            return null;
        }

        private void RefreshCharacter()
        {
            if (characterLevel == null)
                return;

            boundProgression ??= PlayerProgressionManager.EnsureExists();
            boundSurvival ??= FindAnyObjectByType<SurvivalStats>();
            boundEquipment ??= FindAnyObjectByType<EquipmentController>();
            if (boundEquipment != null && (boundAmmo == null || boundAmmo.gameObject != boundEquipment.gameObject))
                boundAmmo = boundEquipment.GetComponent<WeaponAmmoState>();
            boundRoster ??= PioneerRosterManager.EnsureExists();
            boundJetpack ??= FindAnyObjectByType<DMJetpackController>();

            int level = boundProgression != null ? boundProgression.Level : 1;
            int xpProgress = boundProgression != null ? boundProgression.GetXpProgressInCurrentLevel() : 0;
            int xpRequired = boundProgression != null ? boundProgression.GetXpRequiredForNextLevel() : 100;
            int skillPoints = boundProgression != null ? boundProgression.UnspentSkillPoints : 0;
            float statMult = boundProgression != null ? boundProgression.GetLevelStatMultiplier() : 1f;
            characterLevel.text =
                "Level " + level + "\nSkill Points " + skillPoints + "\nLevel stat bonus: +" +
                Mathf.RoundToInt((statMult - 1f) * 100f) + "% max vitals";

            if (characterXpCount != null)
                characterXpCount.text = xpRequired > 0 ? xpProgress + " / " + xpRequired + " XP" : "MAX";
            float xpFill = boundProgression != null ? boundProgression.GetXpProgressNormalized() : 0f;
            if (characterXpFill != null)
                characterXpFill.style.width = Length.Percent(Mathf.Clamp01(xpFill) * 100f);

            SurvivalStats stats = boundSurvival;
            if (stats != null)
            {
                SetVital(FindVital("health"), stats.CurrentHealth, stats.maxHealth, FormatStat(stats.CurrentHealth) + "/" + FormatStat(stats.maxHealth));
                SetVital(FindVital("energy"), stats.CurrentEnergy, stats.maxEnergy, FormatStat(stats.CurrentEnergy) + "/" + FormatStat(stats.maxEnergy));
                SetVital(FindVital("stamina"), stats.CurrentStamina, stats.maxStamina, FormatStat(stats.CurrentStamina) + "/" + FormatStat(stats.maxStamina));
                SetVital(FindVital("oxygen"), stats.CurrentOxygen, stats.maxOxygen, FormatStat(stats.CurrentOxygen) + "/" + FormatStat(stats.maxOxygen));
            }
            else
            {
                SetVitalUnavailable(FindVital("health"), "Health");
                SetVitalUnavailable(FindVital("energy"), "Energy");
                SetVitalUnavailable(FindVital("stamina"), "Stamina");
                SetVitalUnavailable(FindVital("oxygen"), "Oxygen");
            }

            if (boundJetpack != null && boundJetpack.MaxBoostSeconds > 0f)
                SetVital(FindVital("jet"), boundJetpack.FuelRemaining, boundJetpack.MaxBoostSeconds,
                    FormatStat(boundJetpack.FuelRemaining) + "/" + FormatStat(boundJetpack.MaxBoostSeconds));
            else
                SetVitalUnavailable(FindVital("jet"), "Jet Fuel");

            ItemData weapon = boundEquipment != null ? boundEquipment.EquippedItem : null;
            if (weapon != null && weapon.itemType == ItemType.MeleeWeapon)
            {
                float damage = weapon.GetAverageMeleeDamage();
                SetVital(FindVital("melee"), damage, 100f, FormatStat(damage));
            }
            else
            {
                SetVitalUnavailable(FindVital("melee"), "Melee");
            }

            if (weapon != null && weapon.IsRangedWeapon)
            {
                float damage = weapon.GetAverageRangedDamage();
                float accuracy = weapon.GetEffectiveAccuracy();
                SetVital(FindVital("ranged"), damage, 100f, FormatStat(damage));
                SetVital(FindVital("accuracy"), accuracy, 100f, FormatStat(accuracy));
            }
            else
            {
                SetVitalUnavailable(FindVital("ranged"), "Ranged");
                SetVitalUnavailable(FindVital("accuracy"), "Accuracy");
            }

            if (characterLoadout != null)
                characterLoadout.text = BuildLoadoutText(boundEquipment, boundAmmo);

            float ac = boundRoster != null ? boundRoster.AetherCredits : 0f;
            if (characterCredits != null)
                characterCredits.text = "Aether Credits: " + Mathf.RoundToInt(ac);
            if (characterUnlocks != null)
                characterUnlocks.text = LevelUnlockRegistry.BuildUnlockSummary(level);
        }

        private static string FormatStat(float value)
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
            string ammoLine = string.Empty;
            if (activeItem != null && activeItem.IsRangedWeapon && ammo != null)
            {
                if (activeItem.isMiningTool)
                    ammoLine = "\nMining charge: " + ammo.GetMiningChargePercent(equip.ActiveWeaponHotbarSlot) + "%";
                else
                    ammoLine = "\nLoaded ammo: " + ammo.GetActiveLoadedAmmo() + "/" + WeaponAmmoState.GetMagazineCapacity(activeItem);
            }

            return
                "Active weapon: " + FormatItem(activeItem) + "\n" +
                "Secondary weapon: " + FormatItem(equip.SecondaryWeaponItem) + "\n" +
                "Active tool: " + FormatItem(equip.ActiveToolItem) + ammoLine + "\n" +
                "Suit: None equipped (upgrades coming soon)";
        }

        private static string FormatItem(ItemData item) => item != null ? item.itemName : "Empty";

        private void RefreshBlueprints()
        {
            if (blueprintsPending == null)
                return;

            boundCrafting ??= CraftingManager.Instance ?? FindAnyObjectByType<CraftingManager>();
            blueprintsPending.Clear();
            if (blueprintsLibrary != null)
                blueprintsLibrary.Clear();
            if (boundCrafting == null)
            {
                blueprintsPending.Add(MakeEmpty("No crafting manager."));
                return;
            }

            IReadOnlyList<string> pending = boundCrafting.GetPendingBlueprintScrolls();
            IReadOnlyList<RecipeDefinition> learned = boundCrafting.GetDiscoveredRecipes();
            if (blueprintsSummary != null)
            {
                blueprintsSummary.text = pending.Count + " pending  ·  " + learned.Count +
                    " learned  ·  Craft from the library on the right.";
            }

            if (pending.Count == 0)
                blueprintsPending.Add(MakeEmpty("No pending blueprint scrolls."));
            else
            {
                for (int i = 0; i < pending.Count; i++)
                    blueprintsPending.Add(MakePendingRow(pending[i]));
            }

            int shown = 0;
            if (blueprintsLibrary != null)
            {
                for (int i = 0; i < learned.Count; i++)
                {
                    RecipeDefinition recipe = learned[i];
                    if (!MatchesLibraryGroup(recipe, blueprintLibraryGroup))
                        continue;
                    blueprintsLibrary.Add(MakeLearnedRow(recipe));
                    shown++;
                }

                if (shown == 0)
                    blueprintsLibrary.Add(MakeEmpty("No learned blueprints in this group yet."));
            }

            foreach (KeyValuePair<BlueprintLibraryGroup, Button> pair in blueprintTabs)
                pair.Value.EnableInClassList("dmg-subtab--active", pair.Key == blueprintLibraryGroup);

            ApplyBlueprintCraft();
        }

        private VisualElement MakePendingRow(string recipeId)
        {
            RecipeDefinition recipe = RecipeRegistry.Resolve(recipeId);
            string title = recipe != null && !string.IsNullOrEmpty(recipe.displayName) ? recipe.displayName : recipeId;
            VisualElement card = MakeBlueprintCard(recipe, title, "Pending scroll");
            Button learn = new Button { text = "Learn" };
            learn.AddToClassList("dmg-bp-learn");
            string capturedId = recipe != null ? recipe.ResolvedId : recipeId;
            learn.clicked += () => LearnPendingScroll(capturedId);
            learn.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
            card.Add(learn);

            card.RegisterCallback<ClickEvent>(_ =>
            {
                selectedRecipeId = capturedId;
                selectedRecipePending = true;
                ApplyBlueprintDetail();
            });
            AttachBlueprintHover(card, recipe, pendingScroll: true);
            return card;
        }

        private VisualElement MakeLearnedRow(RecipeDefinition recipe)
        {
            string title = recipe != null && !string.IsNullOrEmpty(recipe.displayName)
                ? recipe.displayName
                : "Blueprint";
            string sub = recipe != null
                ? recipe.stationType + "  ·  Tier " + recipe.recipeTier
                : string.Empty;
            VisualElement card = MakeBlueprintCard(recipe, title, sub);
            bool selected = recipe != null && recipe.ResolvedId == selectedRecipeId && !selectedRecipePending;
            card.EnableInClassList("dmg-bp-card--selected", selected);
            string captured = recipe != null ? recipe.ResolvedId : string.Empty;
            card.RegisterCallback<ClickEvent>(_ =>
            {
                selectedRecipeId = captured;
                selectedRecipePending = false;
                RefreshBlueprints();
            });
            AttachBlueprintHover(card, recipe, pendingScroll: false);
            return card;
        }

        private static VisualElement MakeBlueprintCard(
            RecipeDefinition recipe,
            string title,
            string subtitle)
        {
            VisualElement card = new VisualElement();
            card.AddToClassList("dmg-bp-card");

            VisualElement icon = new VisualElement();
            icon.AddToClassList("dmg-bp-card-icon");
            icon.pickingMode = PickingMode.Ignore;
            Sprite sprite = recipe != null ? recipe.DisplayIcon : null;
            if (sprite != null)
                DMUiToolkitStyle.TrySetSpriteBackground(icon, sprite, ScaleMode.ScaleToFit);
            card.Add(icon);

            VisualElement text = new VisualElement();
            text.AddToClassList("dmg-bp-card-text");
            text.pickingMode = PickingMode.Ignore;
            Label name = new Label(title);
            name.AddToClassList("dmg-bp-card-name");
            name.pickingMode = PickingMode.Ignore;
            text.Add(name);
            if (!string.IsNullOrEmpty(subtitle))
            {
                Label sub = new Label(subtitle);
                sub.AddToClassList("dmg-bp-card-sub");
                sub.pickingMode = PickingMode.Ignore;
                text.Add(sub);
            }

            card.Add(text);
            return card;
        }

        private void AttachBlueprintHover(VisualElement target, RecipeDefinition recipe, bool pendingScroll)
        {
            if (target == null || recipe == null)
                return;

            target.RegisterCallback<PointerEnterEvent>(_ =>
                DMUiToolkitWorldMenus.TryShowRecipeTooltip(
                    recipe,
                    CurrentPointerScreenPosition(),
                    pendingScroll,
                    boundInventory,
                    blueprintsBody));
            target.RegisterCallback<PointerLeaveEvent>(_ => DMUiToolkitWorldMenus.HideRecipeTooltip());
        }

        private void LearnPendingScroll(string recipeId)
        {
            boundCrafting ??= CraftingManager.Instance ?? FindAnyObjectByType<CraftingManager>();
            if (boundCrafting == null)
                return;

            RecipeDefinition recipe = RecipeRegistry.Resolve(recipeId);
            if (!boundCrafting.TryLearnPendingScroll(recipeId))
                return;

            string recipeName = recipe != null && !string.IsNullOrEmpty(recipe.displayName)
                ? recipe.displayName
                : recipeId;
            PickupToastUI.Show("Learned blueprint: " + recipeName);
            selectedRecipeId = recipeId;
            selectedRecipePending = false;
            RefreshBlueprints();
        }

        private void BindBlueprintLibraryTabs(VisualElement tree)
        {
            BindBlueprintTab("bp-tab-all", BlueprintLibraryGroup.All, tree);
            BindBlueprintTab("bp-tab-cooking", BlueprintLibraryGroup.Cooking, tree);
            BindBlueprintTab("bp-tab-workbench", BlueprintLibraryGroup.Workbench, tree);
            BindBlueprintTab("bp-tab-consumables", BlueprintLibraryGroup.Consumables, tree);
            BindBlueprintTab("bp-tab-weapons", BlueprintLibraryGroup.Weapons, tree);
            BindBlueprintTab("bp-tab-ammo", BlueprintLibraryGroup.Ammo, tree);
            BindBlueprintTab("bp-tab-resources", BlueprintLibraryGroup.Resources, tree);
            BindBlueprintTab("bp-tab-other", BlueprintLibraryGroup.Other, tree);
        }

        private void BindBlueprintTab(string name, BlueprintLibraryGroup group, VisualElement tree)
        {
            Button button = tree.Q<Button>(name);
            if (button == null)
                return;

            blueprintTabs[group] = button;
            button.userData = group;
            button.UnregisterCallback<ClickEvent>(OnBlueprintTabClicked);
            button.RegisterCallback<ClickEvent>(OnBlueprintTabClicked);
        }

        private void OnBlueprintTabClicked(ClickEvent evt)
        {
            if (evt.currentTarget is not Button button || button.userData is not BlueprintLibraryGroup group)
                return;

            evt.StopPropagation();
            GameAudioManager.Instance?.PlayUiHoverTick();
            blueprintLibraryGroup = group;
            RefreshBlueprints();
        }

        private void WireBlueprintCraft()
        {
            if (blueprintCraftWired)
                return;

            if (blueprintCraftConfirm != null)
                blueprintCraftConfirm.clicked += OnBlueprintCraftClicked;
            if (blueprintCraftAmount != null)
                blueprintCraftAmount.RegisterValueChangedCallback(OnBlueprintCraftAmountChanged);
            blueprintCraftWired = true;
        }

        private void OnBlueprintCraftAmountChanged(ChangeEvent<int> evt)
        {
            if (blueprintCraftAmountValue != null)
                blueprintCraftAmountValue.text = evt.newValue.ToString();
            ApplyBlueprintCraft();
        }

        private void OnBlueprintCraftClicked()
        {
            if (selectedRecipePending)
                return;

            boundCrafting ??= CraftingManager.Instance ?? FindAnyObjectByType<CraftingManager>();
            RecipeDefinition recipe = RecipeRegistry.Resolve(selectedRecipeId);
            if (boundCrafting == null || recipe == null || boundInventory == null)
                return;

            int amount = blueprintCraftAmount != null ? Mathf.Max(1, blueprintCraftAmount.value) : 1;
            if (!boundCrafting.TryCraft(recipe, boundInventory, amount, requireStation: false))
                return;

            PickupToastUI.Show("Crafted: " + ResolveBlueprintName(recipe));
            RefreshBlueprints();
        }

        private static bool MatchesLibraryGroup(RecipeDefinition recipe, BlueprintLibraryGroup group)
        {
            if (recipe == null)
                return false;

            ItemType type = recipe.outputItem != null ? recipe.outputItem.itemType : ItemType.Quest;
            return group switch
            {
                BlueprintLibraryGroup.All => true,
                BlueprintLibraryGroup.Cooking => recipe.stationType == CraftingStationType.Cooking,
                BlueprintLibraryGroup.Workbench => recipe.stationType == CraftingStationType.Workbench,
                BlueprintLibraryGroup.Consumables => type == ItemType.Consumable,
                BlueprintLibraryGroup.Weapons => type == ItemType.MeleeWeapon || type == ItemType.RangedWeapon,
                BlueprintLibraryGroup.Ammo => type == ItemType.Ammo,
                BlueprintLibraryGroup.Resources => type == ItemType.Resource,
                BlueprintLibraryGroup.Other => type != ItemType.Consumable
                    && type != ItemType.MeleeWeapon
                    && type != ItemType.RangedWeapon
                    && type != ItemType.Ammo
                    && type != ItemType.Resource,
                _ => true
            };
        }

        private static string ResolveBlueprintName(RecipeDefinition recipe)
        {
            if (recipe == null)
                return "Blueprint";
            if (!string.IsNullOrWhiteSpace(recipe.displayName))
                return recipe.displayName;
            if (recipe.outputItem != null && !string.IsNullOrEmpty(recipe.outputItem.itemName))
                return recipe.outputItem.itemName;
            return recipe.ResolvedId;
        }

        private void ApplyBlueprintDetail()
        {
            ApplyBlueprintCraft();
        }

        private void ApplyBlueprintCraft()
        {
            RecipeDefinition recipe = RecipeRegistry.Resolve(selectedRecipeId);
            if (recipe == null)
            {
                if (blueprintsDetail != null)
                    blueprintsDetail.text = "Select a learned blueprint to inspect and craft.";
                if (blueprintCraftMeta != null)
                    blueprintCraftMeta.text = string.Empty;
                if (blueprintCraftIngs != null)
                    blueprintCraftIngs.Clear();
                if (blueprintCraftOutput != null)
                    blueprintCraftOutput.text = string.Empty;
                if (blueprintCraftAmount != null)
                    blueprintCraftAmount.SetEnabled(false);
                if (blueprintCraftConfirm != null)
                    blueprintCraftConfirm.SetEnabled(false);
                return;
            }

            if (blueprintsDetail != null)
                blueprintsDetail.text = ResolveBlueprintName(recipe);

            int craftLevel = LevelUnlockUtility.GetEffectiveCraftRequiredLevel(
                recipe.requiredPlayerLevel,
                recipe.outputItem);
            float duration = CraftingManager.GetCraftDurationSeconds(recipe);
            string station = recipe.stationType == CraftingStationType.Cooking ? "Cooking" : "Workbench";
            if (blueprintCraftMeta != null)
            {
                blueprintCraftMeta.text = selectedRecipePending
                    ? "Pending scroll  ·  Learn it on the left, then craft here."
                    : station + "  ·  Tier " + Mathf.Max(1, recipe.recipeTier) +
                      "  ·  Level " + craftLevel +
                      "  ·  " + duration.ToString("0.#") + "s each";
            }

            int amount = blueprintCraftAmount != null ? Mathf.Max(1, blueprintCraftAmount.value) : 1;
            RebuildBlueprintIngredients(recipe, amount);

            if (blueprintCraftOutput != null)
            {
                if (recipe.outputItem != null)
                    blueprintCraftOutput.text = "Output  " + (recipe.outputAmount * amount) + "x " + recipe.outputItem.itemName;
                else
                    blueprintCraftOutput.text = "Output  Unknown";
            }

            int max = 1;
            if (!selectedRecipePending && boundCrafting != null && boundInventory != null)
                max = Mathf.Max(1, boundCrafting.GetMaxCraftCount(recipe, boundInventory, requireStation: false));

            if (blueprintCraftAmount != null)
            {
                blueprintCraftAmount.lowValue = 1;
                blueprintCraftAmount.highValue = max;
                if (blueprintCraftAmount.value < 1 || blueprintCraftAmount.value > max)
                    blueprintCraftAmount.SetValueWithoutNotify(1);
                blueprintCraftAmount.SetEnabled(!selectedRecipePending && max > 1);
            }

            if (blueprintCraftAmountValue != null)
                blueprintCraftAmountValue.text = (blueprintCraftAmount != null ? blueprintCraftAmount.value : 1).ToString();

            bool can = !selectedRecipePending
                && boundCrafting != null
                && boundInventory != null
                && boundCrafting.CanCraft(recipe, boundInventory, amount, requireStation: false);
            if (blueprintCraftConfirm != null)
            {
                blueprintCraftConfirm.SetEnabled(can);
                blueprintCraftConfirm.text = selectedRecipePending ? "Learn first" : "Craft";
            }
        }

        private void RebuildBlueprintIngredients(RecipeDefinition recipe, int amount)
        {
            if (blueprintCraftIngs == null)
                return;

            blueprintCraftIngs.Clear();
            if (recipe?.ingredients == null || recipe.ingredients.Count == 0)
            {
                Label empty = new Label("No ingredients");
                empty.AddToClassList("dmg-bp-ing-name");
                empty.pickingMode = PickingMode.Ignore;
                blueprintCraftIngs.Add(empty);
                return;
            }

            amount = Mathf.Max(1, amount);
            for (int i = 0; i < recipe.ingredients.Count; i++)
            {
                RecipeIngredient ingredient = recipe.ingredients[i];
                if (ingredient == null || ingredient.item == null || ingredient.amount <= 0)
                    continue;

                int need = ingredient.amount * amount;
                int have = boundCrafting != null
                    ? boundCrafting.CountItem(ingredient.item, boundInventory)
                    : 0;

                VisualElement row = new VisualElement();
                row.AddToClassList("dmg-bp-ing-row");
                row.pickingMode = PickingMode.Ignore;

                VisualElement icon = new VisualElement();
                icon.AddToClassList("dmg-bp-ing-icon");
                icon.pickingMode = PickingMode.Ignore;
                Sprite sprite = DMGameIconRegistry.FindIcon(ingredient.item);
                if (sprite != null)
                    DMUiToolkitStyle.TrySetSpriteBackground(icon, sprite, ScaleMode.ScaleToFit);
                row.Add(icon);

                Label name = new Label(ingredient.item.itemName);
                name.AddToClassList("dmg-bp-ing-name");
                name.pickingMode = PickingMode.Ignore;
                row.Add(name);

                Label count = new Label(have + " / " + need);
                count.AddToClassList("dmg-bp-ing-count");
                if (have >= need)
                    count.AddToClassList("dmg-bp-ing-count--ready");
                count.pickingMode = PickingMode.Ignore;
                row.Add(count);

                blueprintCraftIngs.Add(row);
            }
        }

        private void OnSkillCatClicked(ClickEvent evt)
        {
            if (evt.currentTarget is not Button button || button.userData is not SkillTreeCategory category)
                return;

            evt.StopPropagation();
            GameAudioManager.Instance?.PlayUiHoverTick();
            skillsCategory = category;
            selectedSkillId = null;
            RefreshSkills();
        }

        private void RefreshSkills()
        {
            if (skillsHexHost == null && skillsList == null && skillsSummary == null)
                return;

            boundProgression ??= PlayerProgressionManager.EnsureExists();
            int points = boundProgression != null ? boundProgression.UnspentSkillPoints : 0;
            int level = boundProgression != null ? boundProgression.Level : 1;
            if (skillsSummary != null)
            {
                skillsSummary.text = "Level " + level + "  ·  Skill Points " + points;
                skillsSummary.style.color = points > 0
                    ? DarkMatterGenesisUiPalette.HighlightText
                    : DarkMatterGenesisUiPalette.BodyText;
            }

            foreach (KeyValuePair<SkillTreeCategory, Button> pair in skillCats)
                pair.Value.EnableInClassList("dmg-subtab--active", pair.Key == skillsCategory);

            RebuildSkillsHex();
            ApplySkillDetail();
            ApplyHexPopupOverride();
        }

        private VisualElement MakeSkillRow(SkillDefinition skill)
        {
            int rank = boundProgression != null ? boundProgression.GetSkillRank(skill.ResolvedId) : 0;
            rank = skill.GetDisplayRank(rank);
            Button row = new Button();
            row.AddToClassList("dmg-list-row");
            row.EnableInClassList("dmg-list-row--selected", skill.ResolvedId == selectedSkillId);
            Label title = new Label(skill.displayName);
            title.AddToClassList("dmg-list-row-title");
            title.pickingMode = PickingMode.Ignore;
            row.Add(title);
            Label sub = new Label("Rank " + rank + "/" + skill.ClampedMaxRank + "  ·  Next cost " + skill.GetCostForNextRank(rank));
            sub.AddToClassList("dmg-list-row-sub");
            sub.pickingMode = PickingMode.Ignore;
            row.Add(sub);
            string captured = skill.ResolvedId;
            row.clicked += () =>
            {
                selectedSkillId = captured;
                RefreshSkills();
            };
            return row;
        }

        private void ApplySkillDetail()
        {
            SkillDefinition skill = SkillRegistry.Resolve(selectedSkillId);
            if (skillsDetailTitle == null)
                return;

            if (skill == null)
            {
                skillsDetailTitle.text = "Select a skill";
                if (skillsDetailBody != null)
                    skillsDetailBody.text = "Category hex trees from the Player skill allocator.";
                if (skillsAllocate != null)
                    skillsAllocate.SetEnabled(false);
                ApplySkillCardArt(null);
                return;
            }

            int rank = boundProgression != null ? boundProgression.GetSkillRank(skill.ResolvedId) : 0;
            rank = skill.GetDisplayRank(rank);
            skillsDetailTitle.text = skill.displayName;
            string error;
            bool can = PlayerSkillAllocator.CanAllocate(skill, boundProgression, out error);
            if (skillsDetailBody != null)
            {
                skillsDetailBody.text =
                    (skill.description ?? string.Empty) + "\n\n" +
                    "Category: " + SkillDefinition.GetCategoryDisplayName(skill.treeCategory) + "\n" +
                    "Rank " + rank + "/" + skill.ClampedMaxRank + "  ·  Next cost " + skill.GetCostForNextRank(rank) + "\n" +
                    "Requires player level " + skill.GetRequiredPlayerLevelForNextRank(rank) +
                    (can ? string.Empty : "\n" + error);
            }

            if (skillsAllocate != null)
            {
                skillsAllocate.SetEnabled(can);
                if (can)
                    skillsAllocate.text = "Allocate Point";
                else if (rank >= skill.ClampedMaxRank)
                    skillsAllocate.text = "Max Rank";
                else if (!string.IsNullOrEmpty(error) && error.StartsWith("Requires ", System.StringComparison.Ordinal))
                    skillsAllocate.text = "Locked - Prior Skill";
                else
                    skillsAllocate.text = "Cannot Allocate";
            }

            ApplySkillCardArt(skill);
        }

        private static readonly HashSet<string> PrototypeSkillCardIds = new HashSet<string>
        {
            "skill_lung_capacity",
            "skill_breath_efficiency",
            "skill_o2_scrubber",
            "skill_marksman_training",
            "skill_steady_breath",
            "skill_long_range_cadence",
            "skill_deadeye",
        };

        

        private void ApplySkillCardArt(SkillDefinition skill)
        {
            if (skillsCard == null)
                return;

            bool show = skill != null && PrototypeSkillCardIds.Contains(skill.ResolvedId);
            DMUiToolkitOverlayDocument.SetShown(skillsCard, show);
            if (!show)
            {
                if (skillsCardArt != null)
                    skillsCardArt.style.backgroundImage = StyleKeyword.None;
                if (skillsCardCaption != null)
                    skillsCardCaption.text = string.Empty;
                return;
            }

            Texture2D tex = Resources.Load<Texture2D>("UI/Skills/" + skill.ResolvedId);
            if (skillsCardArt != null)
            {
                if (tex != null)
                {
                    skillsCardArt.style.backgroundImage = new StyleBackground(Background.FromTexture2D(tex));
                    DMUiToolkitStyle.ApplyBackgroundScale(skillsCardArt, ScaleMode.ScaleAndCrop);
                }
                else
                    skillsCardArt.style.backgroundImage = StyleKeyword.None;
            }

            if (skillsCardCaption != null)
                skillsCardCaption.text = skill.displayName;
        }

        private void OnAllocateSkillClicked()
        {
            SkillDefinition skill = SkillRegistry.Resolve(selectedSkillId);
            if (skill == null)
                return;

            if (!PlayerSkillAllocator.TryAllocate(skill, out string error))
            {
                if (!string.IsNullOrEmpty(error))
                    PickupToastUI.Show(error);
                return;
            }

            GameAudioManager.Instance?.PlayButtonClick();
            RefreshSkills();
            RefreshCharacter();
        }

        private void RefreshEchoes()
        {
            if (echoesChronicle == null)
                return;

            boundRoster ??= PioneerRosterManager.EnsureExists();
            echoesChronicle.Clear();
            echoesBuffs?.Clear();
            echoesSignals?.Clear();
            echoesDispositions?.Clear();

            int added = 0;
            if (boundRoster != null)
            {
                for (int i = 0; i < boundRoster.EchoChronicle.Count; i++)
                {
                    EchoChronicleEntry entry = boundRoster.EchoChronicle[i];
                    if (entry == null || entry.simulationIncident)
                        continue;

                    string heading = entry.rescueFailed ? "Rescue Failed" : "Rescue Success";
                    Color color = entry.rescueFailed
                        ? DarkMatterGenesisUiPalette.DangerRed
                        : DarkMatterGenesisUiPalette.PositiveGreen;
                    string dateLabel = entry.rescuedAtUtcTicks > 0
                        ? new DateTime(entry.rescuedAtUtcTicks, DateTimeKind.Utc).ToLocalTime().ToString("MMM d · HH:mm")
                        : "Unknown time";
                    string disposition = PioneerTraitUtility.GetDispositionLabel(entry.DispositionAtRescue);
                    echoesChronicle.Add(MakeLogCard(
                        heading,
                        entry.echoName,
                        dateLabel + "  ·  " + entry.classSummary + "  ·  " + disposition + "  ·  " + entry.abilitySummary,
                        color));
                    added++;
                }
            }

            if (added == 0)
                echoesChronicle.Add(MakeEmpty("No rescues logged. Neural Echo rescues and failures appear here as your chronicle grows."));

            IReadOnlyList<string> buffs = CompanionBuffRegistry.GetActiveBuffSummaries(boundRoster);
            if (echoesBuffs != null)
            {
                if (buffs == null || buffs.Count == 0)
                    echoesBuffs.Add(MakeEmpty("No companion buffs active."));
                else
                {
                    for (int i = 0; i < buffs.Count; i++)
                        echoesBuffs.Add(MakeLogCard("Buff", buffs[i], string.Empty, DarkMatterGenesisUiPalette.Gold));
                }
            }

            EchoSignalRegistry.EnsureDefaultPlaceholder();
            IReadOnlyList<string> signals = EchoSignalRegistry.GetActiveSignalSummaries();
            if (echoesSignals != null)
            {
                if (signals == null || signals.Count == 0)
                    echoesSignals.Add(MakeEmpty("No active echo signals."));
                else
                {
                    for (int i = 0; i < signals.Count; i++)
                        echoesSignals.Add(MakeLogCard("Signal", signals[i], string.Empty, DarkMatterGenesisUiPalette.RichFuchsia));
                }
            }

            if (echoesDispositions != null)
            {
                int disp = 0;
                if (boundRoster != null)
                {
                    for (int i = 0; i < boundRoster.SkilledPioneers.Count; i++)
                    {
                        SkilledPioneerRecord record = boundRoster.SkilledPioneers[i];
                        if (record == null || record.Kind != PioneerKind.RescuedEcho)
                            continue;

                        echoesDispositions.Add(MakeLogCard(
                            PioneerTraitUtility.GetDispositionLabel(record.Disposition),
                            PioneerUiLabels.GetDisplayName(record),
                            SkilledPioneerClassUtility.ToDisplayName(record.pioneerClass),
                            GetDispositionColor(record.Disposition)));
                        disp++;
                    }
                }

                if (disp == 0)
                    echoesDispositions.Add(MakeEmpty("No rescued echoes on roster yet."));
            }
        }

        private static Color GetDispositionColor(EchoDisposition disposition)
        {
            switch (disposition)
            {
                case EchoDisposition.Friendly: return DarkMatterGenesisUiPalette.PositiveGreen;
                case EchoDisposition.Synced: return DarkMatterGenesisUiPalette.Gold;
                case EchoDisposition.HostileUntilSynced: return DarkMatterGenesisUiPalette.DangerRed;
                default: return DarkMatterGenesisUiPalette.SoftBeigeGray;
            }
        }

        private void EnsureAchievementCategoryTabs()
        {
            if (achievementsCats == null || achievementsCats.childCount > 0)
                return;

            AddAchievementCat("All", null);
            foreach (AchievementCategory category in Enum.GetValues(typeof(AchievementCategory)))
                AddAchievementCat(category.ToString(), category);
        }

        private void AddAchievementCat(string label, AchievementCategory? category)
        {
            Button button = new Button();
            button.text = label;
            button.AddToClassList("dmg-subtab");
            button.userData = category;
            button.RegisterCallback<ClickEvent>(OnAchievementCatClicked);
            achievementsCats.Add(button);
        }

        private void OnAchievementCatClicked(ClickEvent evt)
        {
            if (evt.currentTarget is not Button button)
                return;

            evt.StopPropagation();
            GameAudioManager.Instance?.PlayUiHoverTick();
            selectedAchievementCategory = button.userData is AchievementCategory boxed
                ? boxed
                : (AchievementCategory?)null;
            RefreshAchievements();
        }

        private void RefreshAchievements()
        {
            if (achievementsGrid == null)
                return;

            boundAchievements ??= AchievementManager.EnsureExists();
            EnsureAchievementCategoryTabs();

            foreach (VisualElement child in achievementsCats.Children())
            {
                Button button = child as Button;
                if (button == null)
                    continue;
                AchievementCategory? cat = button.userData is AchievementCategory boxed
                    ? boxed
                    : (AchievementCategory?)null;
                bool on = cat.HasValue == selectedAchievementCategory.HasValue
                    && (!cat.HasValue || cat.Value == selectedAchievementCategory.Value);
                button.EnableInClassList("dmg-subtab--active", on);
            }

            achievementsGrid.Clear();
            int total = 0;
            int unlocked = 0;
            List<AchievementDefinition> defs = new List<AchievementDefinition>(AchievementRegistry.GetAllAchievements());
            defs.Sort((a, b) =>
            {
                int order = a.sortOrder.CompareTo(b.sortOrder);
                return order != 0 ? order : string.CompareOrdinal(a.title, b.title);
            });

            for (int i = 0; i < defs.Count; i++)
            {
                AchievementDefinition definition = defs[i];
                if (definition == null)
                    continue;
                if (selectedAchievementCategory.HasValue && definition.category != selectedAchievementCategory.Value)
                    continue;

                total++;
                AchievementProgress progress = boundAchievements != null
                    ? boundAchievements.GetProgress(definition.ResolvedId)
                    : null;
                bool isUnlocked = progress != null && progress.unlocked;
                if (isUnlocked)
                    unlocked++;
                achievementsGrid.Add(MakeAchievementSlot(definition, progress ?? new AchievementProgress(definition.ResolvedId)));
            }

            if (achievementsSummary != null)
            {
                string filter = selectedAchievementCategory.HasValue
                    ? selectedAchievementCategory.Value.ToString()
                    : "All";
                achievementsSummary.text = filter + "  ·  " + unlocked + "/" + total + " unlocked";
            }

            if (total == 0)
                achievementsGrid.Add(MakeEmpty("No achievements configured."));
        }

        private static VisualElement MakeAchievementSlot(AchievementDefinition definition, AchievementProgress progress)
        {
            bool unlocked = progress.unlocked;
            bool hiddenLocked = definition.hidden && !unlocked;
            VisualElement slot = new VisualElement();
            slot.AddToClassList("dmg-ach-slot");
            slot.EnableInClassList("dmg-ach-slot--unlocked", unlocked);
            slot.pickingMode = PickingMode.Ignore;

            VisualElement icon = new VisualElement();
            icon.AddToClassList("dmg-ach-icon");
            if (!hiddenLocked)
                DMUiToolkitStyle.TrySetSpriteBackground(icon, definition.icon, ScaleMode.ScaleToFit);
            else
                DMUiToolkitStyle.ClearBackgroundImage(icon);

            icon.style.backgroundColor = hiddenLocked || definition.icon == null
                ? DarkMatterGenesisUiPalette.SlateGray
                : GetAchievementCategoryColor(definition.category);

            slot.Add(icon);
            string title = hiddenLocked ? "???" : definition.title;
            string body = hiddenLocked ? "Hidden achievement" : definition.description;
            Label titleLabel = new Label(title);
            titleLabel.AddToClassList("dmg-ach-title");
            slot.Add(titleLabel);
            Label bodyLabel = new Label(body);
            bodyLabel.AddToClassList("dmg-ach-body");
            slot.Add(bodyLabel);

            string status = unlocked ? "Unlocked" : (definition.targetCount > 1 ? progress.currentCount + " / " + definition.targetCount : string.Empty);
            int xpPreview = definition.xpReward;
            if (definition.hidden)
                xpPreview = Mathf.RoundToInt(xpPreview * 1.5f);
            if (xpPreview > 0)
                status = string.IsNullOrEmpty(status) ? "+" + xpPreview + " XP" : status + "  ·  +" + xpPreview + " XP";
            if (!string.IsNullOrEmpty(status))
            {
                Label statusLabel = new Label(status);
                statusLabel.AddToClassList("dmg-ach-body");
                statusLabel.style.color = status.StartsWith("Unlocked", StringComparison.Ordinal)
                    ? DarkMatterGenesisUiPalette.PositiveGreen
                    : DarkMatterGenesisUiPalette.Gold;
                slot.Add(statusLabel);
            }

            return slot;
        }

        private static Color GetAchievementCategoryColor(AchievementCategory category)
        {
            switch (category)
            {
                case AchievementCategory.Combat: return DarkMatterGenesisUiPalette.DangerRed;
                case AchievementCategory.Crafting: return DarkMatterGenesisUiPalette.Gold;
                case AchievementCategory.Pets: return DarkMatterGenesisUiPalette.PositiveGreen;
                case AchievementCategory.Pioneers: return DarkMatterGenesisUiPalette.RichFuchsia;
                case AchievementCategory.Exploration: return DarkMatterGenesisUiPalette.SoftBeigeGray;
                default: return DarkMatterGenesisUiPalette.SlateGray;
            }
        }
    }
}
