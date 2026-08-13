#if UNITY_EDITOR
using System.IO;
using Project.EditorTools;
using Project.Progression;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Progression
{
    public static class ProgressionSetupUtility
    {
        private const string ProgressionResourcesRoot = "Assets/_Project/Resources/Progression";
        private const string SkillsFolder = ProgressionResourcesRoot + "/Skills";
        private const string CurvePath = ProgressionResourcesRoot + "/ProgressionCurve.asset";
        private const string RegistryPath = ProgressionResourcesRoot + "/SkillRegistry.asset";

        [MenuItem(SurvivalPioneerEditorMenus.Content + "Create Progression Curve", false, 40)]
        public static void CreateProgressionCurve()
        {
            EnsureFolder(ProgressionResourcesRoot);
            ProgressionCurveDefinition existing = AssetDatabase.LoadAssetAtPath<ProgressionCurveDefinition>(CurvePath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                return;
            }

            ProgressionCurveDefinition curve = ScriptableObject.CreateInstance<ProgressionCurveDefinition>();
            curve.expScale = ProgressionCurveDefinition.DefaultExpScale;
            curve.expPower = ProgressionCurveDefinition.DefaultExpPower;
            curve.linearScale = ProgressionCurveDefinition.DefaultLinearScale;
            curve.xpRequiredPerLevel = System.Array.Empty<int>();
            AssetDatabase.CreateAsset(curve, CurvePath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = curve;
        }

        [MenuItem(SurvivalPioneerEditorMenus.Content + "Create Starter Skills + Registry", false, 41)]
        public static void CreateStarterSkills()
        {
            EnsureFolder(SkillsFolder);

            SkillDefinition[] skills =
            {
                // Player — core vitals
                CreateSkill("skill_vital_boost", "Vital Boost", "Increase max health.", SkillTreeCategory.Player, 0, 0, 1, SkillModifierType.MaxHealthPercent, 5f, maxRank: 5),
                CreateSkill("skill_endurance", "Endurance", "Increase max energy.", SkillTreeCategory.Player, 1, 0, 1, SkillModifierType.MaxEnergyPercent, 5f, maxRank: 5),
                CreateSkill("skill_stamina_core", "Stamina Core", "Increase max stamina.", SkillTreeCategory.Player, 2, 0, 2, SkillModifierType.MaxStaminaPercent, 5f, maxRank: 5),
                CreateSkill("skill_vital_resilience", "Vital Resilience", "Further increase max health after Vital Boost.", SkillTreeCategory.Player, 0, 1, 4, SkillModifierType.MaxHealthPercent, 4f, maxRank: 5, prerequisiteSkillIds: new[] { "skill_vital_boost" }),
                CreateSkill("skill_field_conditioning", "Field Conditioning", "Boost energy reserves for long expeditions.", SkillTreeCategory.Player, 1, 1, 4, SkillModifierType.MaxEnergyPercent, 4f, maxRank: 5, prerequisiteSkillIds: new[] { "skill_endurance" }),
                CreateSkill("skill_survivor_edge", "Survivor's Edge", "Push stamina capacity after Stamina Core.", SkillTreeCategory.Player, 2, 1, 5, SkillModifierType.MaxStaminaPercent, 4f, maxRank: 5, prerequisiteSkillIds: new[] { "skill_stamina_core" }),

                // Melee
                CreateSkill("skill_blade_training", "Blade Training", "+2 melee damage per rank for all melee weapons.", SkillTreeCategory.Melee, 0, 0, 2, SkillModifierType.MeleeDamageFlat, 2f, maxRank: 5),
                CreateSkill("skill_guard_break", "Guard Break", "+2 melee damage per rank; opens heavier follow-ups.", SkillTreeCategory.Melee, 1, 0, 3, SkillModifierType.MeleeDamageFlat, 2f, maxRank: 5),
                CreateSkill("skill_momentum_strike", "Momentum Strike", "+2 melee damage per rank when pressing the attack.", SkillTreeCategory.Melee, 2, 0, 3, SkillModifierType.MeleeDamageFlat, 2f, maxRank: 5),
                CreateSkill("skill_cleaving_edge", "Cleaving Edge", "Additional melee damage after Blade Training.", SkillTreeCategory.Melee, 0, 1, 5, SkillModifierType.MeleeDamageFlat, 2f, maxRank: 5, prerequisiteSkillIds: new[] { "skill_blade_training" }),
                CreateSkill("skill_counter_rhythm", "Counter Rhythm", "Melee damage after learning Guard Break.", SkillTreeCategory.Melee, 1, 1, 6, SkillModifierType.MeleeDamageFlat, 2f, maxRank: 5, prerequisiteSkillIds: new[] { "skill_guard_break" }),
                CreateSkill("skill_warpath", "Warpath", "Capstone melee damage requiring Blade Training and Momentum Strike.", SkillTreeCategory.Melee, 2, 1, 8, SkillModifierType.MeleeDamageFlat, 3f, maxRank: 5, prerequisiteSkillIds: new[] { "skill_blade_training", "skill_momentum_strike" }),

                // Pistols
                CreateSkill("skill_sidearm_drill", "Sidearm Drill", "+2 ranged damage per rank — sidearm fundamentals.", SkillTreeCategory.Pistols, 0, 0, 2, SkillModifierType.RangedDamageFlat, 2f, maxRank: 5),
                CreateSkill("skill_quick_draw", "Quick Draw", "+5% weapon accuracy per rank with sidearms.", SkillTreeCategory.Pistols, 1, 0, 2, SkillModifierType.WeaponAccuracyPercent, 5f, maxRank: 5),
                CreateSkill("skill_close_quarters_mark", "Close Quarters Mark", "+2 ranged damage per rank in tight engagements.", SkillTreeCategory.Pistols, 2, 0, 3, SkillModifierType.RangedDamageFlat, 2f, maxRank: 5),
                CreateSkill("skill_dual_focus", "Dual Focus", "Accuracy after Sidearm Drill.", SkillTreeCategory.Pistols, 0, 1, 5, SkillModifierType.WeaponAccuracyPercent, 4f, maxRank: 5, prerequisiteSkillIds: new[] { "skill_sidearm_drill" }),
                CreateSkill("skill_piercing_rounds", "Piercing Rounds", "Ranged damage after Quick Draw.", SkillTreeCategory.Pistols, 1, 1, 5, SkillModifierType.RangedDamageFlat, 2f, maxRank: 5, prerequisiteSkillIds: new[] { "skill_quick_draw" }),
                CreateSkill("skill_gunslinger", "Gunslinger", "Capstone sidearm damage requiring Sidearm Drill and Close Quarters Mark.", SkillTreeCategory.Pistols, 2, 1, 8, SkillModifierType.RangedDamageFlat, 3f, maxRank: 5, prerequisiteSkillIds: new[] { "skill_sidearm_drill", "skill_close_quarters_mark" }),

                // Rifles
                CreateSkill("skill_marksman_training", "Marksman Training", "+2 ranged damage per rank for rifles and long guns.", SkillTreeCategory.Rifles, 0, 0, 2, SkillModifierType.RangedDamageFlat, 2f, maxRank: 5),
                CreateSkill("skill_weapon_accuracy", "Weapon Accuracy", "+5% accuracy per rank for ranged weapons.", SkillTreeCategory.Rifles, 1, 0, 1, SkillModifierType.WeaponAccuracyPercent, 5f, maxRank: 5),
                CreateSkill("skill_steady_breath", "Steady Breath", "+2 ranged damage per rank while lined up.", SkillTreeCategory.Rifles, 2, 0, 3, SkillModifierType.RangedDamageFlat, 2f, maxRank: 5),
                CreateSkill("skill_long_range_cadence", "Long Range Cadence", "Rifle damage after Marksman Training.", SkillTreeCategory.Rifles, 0, 1, 5, SkillModifierType.RangedDamageFlat, 2f, maxRank: 5, prerequisiteSkillIds: new[] { "skill_marksman_training" }),
                CreateSkill("skill_mag_discipline", "Mag Discipline", "Accuracy after Weapon Accuracy.", SkillTreeCategory.Rifles, 1, 1, 4, SkillModifierType.WeaponAccuracyPercent, 4f, maxRank: 5, prerequisiteSkillIds: new[] { "skill_weapon_accuracy" }),
                CreateSkill("skill_deadeye", "Deadeye", "Capstone rifle damage requiring Marksman Training and Steady Breath.", SkillTreeCategory.Rifles, 2, 1, 8, SkillModifierType.RangedDamageFlat, 3f, maxRank: 5, prerequisiteSkillIds: new[] { "skill_marksman_training", "skill_steady_breath" }),

                // Survival
                CreateSkill("skill_gather_efficiency", "Gather Efficiency", "Gather resources faster.", SkillTreeCategory.Survival, 0, 0, 1, SkillModifierType.GatherSpeedPercent, 6f, maxRank: 5),
                CreateSkill(
                    "skill_mining",
                    "Mining",
                    "Unlock higher-tier ores for multi-tool resource scanning and mining.",
                    SkillTreeCategory.Survival,
                    1,
                    0,
                    5,
                    SkillModifierType.MiningTier,
                    1f,
                    maxRank: 5,
                    costPerTargetRank: new[] { 1, 2, 2, 2, 2 }),
                CreateSkill(
                    "skill_harvesting",
                    "Harvesting",
                    "Unlock higher-tier plants for multi-tool resource scanning and harvesting.",
                    SkillTreeCategory.Survival,
                    2,
                    0,
                    5,
                    SkillModifierType.HarvestingTier,
                    1f,
                    maxRank: 5,
                    costPerTargetRank: new[] { 1, 2, 2, 2, 2 }),
                CreateSkill("skill_artisan_focus", "Artisan Focus", "Earn more crafting XP.", SkillTreeCategory.Survival, 0, 1, 3, SkillModifierType.CraftXpPercent, 8f, maxRank: 5, prerequisiteSkillIds: new[] { "skill_gather_efficiency" }),
                CreateSkill("skill_recon_sweep", "Recon Sweep", "Each rank expands scanner fog reveal and sweep range by +10m (up to 5 ranks). Base scan reveal is 40m.", SkillTreeCategory.Survival, 1, 1, 1, SkillModifierType.ScanRangeFlat, 10f, maxRank: 5),
                CreateSkill("skill_field_logistics", "Field Logistics", "Gather faster after Mining fundamentals.", SkillTreeCategory.Survival, 2, 1, 6, SkillModifierType.GatherSpeedPercent, 5f, maxRank: 5, prerequisiteSkillIds: new[] { "skill_mining" })
            };

            SkillRegistry registry = AssetDatabase.LoadAssetAtPath<SkillRegistry>(RegistryPath);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<SkillRegistry>();
                AssetDatabase.CreateAsset(registry, RegistryPath);
            }

            SerializedObject serialized = new SerializedObject(registry);
            SerializedProperty array = serialized.FindProperty("skills");
            array.arraySize = skills.Length;
            for (int i = 0; i < skills.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = skills[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            Selection.activeObject = registry;
            Debug.Log($"[Progression] Seeded {skills.Length} skills across Melee / Pistols / Rifles / Survival / Player trees.");
        }

        private static SkillDefinition CreateSkill(
            string id,
            string displayName,
            string description,
            SkillTreeCategory category,
            int treeColumn,
            int treeRow,
            int requiredLevel,
            SkillModifierType modifier,
            float bonusPerRank,
            int maxRank = 5,
            int[] costPerTargetRank = null,
            string[] prerequisiteSkillIds = null)
        {
            string path = $"{SkillsFolder}/{id}.asset";
            SkillDefinition skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(path);
            if (skill == null)
            {
                skill = ScriptableObject.CreateInstance<SkillDefinition>();
                AssetDatabase.CreateAsset(skill, path);
            }

            skill.skillId = id;
            skill.displayName = displayName;
            skill.description = description;
            skill.treeCategory = category;
            skill.treeColumn = treeColumn;
            skill.treeRow = treeRow;
            skill.requiredPlayerLevel = requiredLevel;
            skill.modifierType = modifier;
            skill.bonusPercentPerRank = bonusPerRank;
            skill.costPerRank = 1;
            skill.maxRank = Mathf.Clamp(maxRank, 1, SkillDefinition.DisplayMaxRank);
            skill.costPerTargetRank = costPerTargetRank;
            skill.prerequisiteSkillIds = prerequisiteSkillIds ?? System.Array.Empty<string>();
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
