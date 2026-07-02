using System.Collections.Generic;
using Project.Pioneers;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public static class PioneerCatalogCreator
    {
        private const string OutputFolder = "Assets/_Project/Resources/Pioneers";

        [MenuItem("Tools/Survival Pioneer/Content/Create Named Pioneer Catalog")]
        public static void CreateNamedPioneerCatalog()
        {
            EnsureFolder(OutputFolder);

            CatalogSeed[] seeds = BuildSeeds();
            int created = 0;
            for (int i = 0; i < seeds.Length; i++)
            {
                CatalogSeed seed = seeds[i];
                string assetPath = $"{OutputFolder}/{seed.pioneerId}.asset";
                NamedPioneerDefinition existing = AssetDatabase.LoadAssetAtPath<NamedPioneerDefinition>(assetPath);
                NamedPioneerDefinition definition = existing != null ? existing : ScriptableObject.CreateInstance<NamedPioneerDefinition>();
                ApplySeed(definition, seed);
                if (existing == null)
                {
                    AssetDatabase.CreateAsset(definition, assetPath);
                    created++;
                }
                else
                {
                    EditorUtility.SetDirty(definition);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            NamedPioneerCatalog.ReloadCache();
            Debug.Log($"Named pioneer catalog updated. Created {created} new assets in {OutputFolder}.");
        }

        private static void ApplySeed(NamedPioneerDefinition definition, CatalogSeed seed)
        {
            definition.pioneerId = seed.pioneerId;
            definition.displayName = seed.displayName;
            definition.pioneerClass = seed.pioneerClass;
            definition.startLevel = seed.startLevel;
            definition.radiationResistance = seed.radiationResistance;
            definition.expeditionEfficiency = seed.expeditionEfficiency;
            definition.combatSynergy = seed.combatSynergy;
            definition.saturation = seed.saturation;
            definition.backstory = seed.backstory;
            definition.traitIds = seed.traitIds;
            definition.passiveAbilityIds = seed.passiveAbilityIds;
            definition.learnedSkills = seed.learnedSkills;
        }

        private static CatalogSeed[] BuildSeeds()
        {
            return new[]
            {
                Seed("named_kael_9", "Sulfur-Blooded Kael-9", SkilledPioneerClass.CombatTactician, 0.62f, 0.48f, 0.74f, "Failed caldera survey imprint.", "aggro_pulse", "tremor_sense", "haul"),
                Seed("named_lira_prime", "Rift-Touched Lira Prime", SkilledPioneerClass.InfiltratorScout, 0.55f, 0.71f, 0.52f, "Collapsed vent route signal trace.", "vent_burst", "echo_reverb", "forage"),
                Seed("named_calder_sol", "Neural-Scarred Calder Sol", SkilledPioneerClass.ScienceSpecialist, 0.68f, 0.64f, 0.46f, "Early isotope research archive fragment.", "purification_field", "rad_hardening", "sanitize"),
                Seed("named_ryn_vale", "Pyroclast Ryn Vale", SkilledPioneerClass.ArchitectEngineer, 0.5f, 0.58f, 0.6f, "Ruined hab shell near lava tubes.", "shield_drone", "harvest_boost", "patchwork"),
                Seed("named_thorne_7x", "Lava-Phased Thorne-7X", SkilledPioneerClass.CombatTactician, 0.58f, 0.53f, 0.69f, "Basalt trench assault imprint.", "crust_fracture", "synergy_link", "salvage"),
                Seed("named_voss_nyx", "Echo-Bound Voss Nyx", SkilledPioneerClass.InfiltratorScout, 0.57f, 0.67f, 0.55f, "Ghost signal from a lost relay team.", "phase_step", "echo_reverb", "forage"),
                Seed("named_solara_core", "Magma-Forged Solara Core", SkilledPioneerClass.ScienceSpecialist, 0.7f, 0.61f, 0.44f, "Thermal lab survivor echo.", "purification_field", "rad_hardening", "sanitize"),
                Seed("named_draven_4", "Void-Glitched Draven-4", SkilledPioneerClass.CombatTactician, 0.6f, 0.5f, 0.72f, "Hostile imprint stabilized after sync.", "decoy_beacon", "tremor_sense", "haul"),
                Seed("named_quill_prime", "Tectonic Quill Prime", SkilledPioneerClass.ArchitectEngineer, 0.52f, 0.56f, 0.63f, "Seismic stabilizer crew memory.", "shield_drone", "harvest_boost", "patchwork"),
                Seed("named_mira_storm", "Crystalized Mira Storm", SkilledPioneerClass.InfiltratorScout, 0.54f, 0.73f, 0.51f, "Crystal vent scout imprint.", "vent_burst", "synergy_link", "forage"),
                Seed("named_vesper_9", "Crystalized Vesper-9", SkilledPioneerClass.ScienceSpecialist, 0.66f, 0.62f, 0.47f, "Probe archive science specialist.", "crust_fracture", "rad_hardening", "sanitize"),
                Seed("named_io_hybrid", "Rift-Touched I/O Hybrid Prime", SkilledPioneerClass.IoHybrid, 0.64f, 0.66f, 0.66f, "Rare symbiotic human-AI imprint.", "phase_step", "synergy_link", "salvage")
            };
        }

        private static CatalogSeed Seed(
            string id,
            string displayName,
            SkilledPioneerClass pioneerClass,
            float rad,
            float expedition,
            float combat,
            string backstory,
            string traitId,
            string passiveId,
            string menialSkill)
        {
            return new CatalogSeed
            {
                pioneerId = id,
                displayName = displayName,
                pioneerClass = pioneerClass,
                startLevel = 1,
                radiationResistance = rad,
                expeditionEfficiency = expedition,
                combatSynergy = combat,
                saturation = 0.22f,
                backstory = backstory,
                traitIds = new[] { traitId },
                passiveAbilityIds = new[] { passiveId },
                learnedSkills = new[] { menialSkill }
            };
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private struct CatalogSeed
        {
            public string pioneerId;
            public string displayName;
            public SkilledPioneerClass pioneerClass;
            public int startLevel;
            public float radiationResistance;
            public float expeditionEfficiency;
            public float combatSynergy;
            public float saturation;
            public string backstory;
            public string[] traitIds;
            public string[] passiveAbilityIds;
            public string[] learnedSkills;
        }
    }
}
