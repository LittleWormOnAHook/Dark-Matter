using Project.Creatures;
using Project.EditorTools;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Creatures
{
    public static class DMICreatureBrainProfileUtility
    {
        public const string BrainProfilesFolder = ProjectAssetPaths.CreaturesData + "/BrainProfiles";

        public const string WanderProfilePath = BrainProfilesFolder + "/DMI_Brain_Wander.asset";
        public const string PatrolProfilePath = BrainProfilesFolder + "/DMI_Brain_Patrol.asset";
        public const string StationaryGuardProfilePath = BrainProfilesFolder + "/DMI_Brain_StationaryGuard.asset";
        public const string EmberSkitterProfilePath = BrainProfilesFolder + "/DMI_Brain_EmberSkitter.asset";

        public static DMICreatureBrainProfile EnsureWanderProfile()
        {
            return EnsureProfile(WanderProfilePath, "DMI_Brain_Wander", p => p.ApplyWanderDefaults());
        }

        public static DMICreatureBrainProfile EnsurePatrolProfile()
        {
            return EnsureProfile(PatrolProfilePath, "DMI_Brain_Patrol", p => p.ApplyPatrolDefaults());
        }

        public static DMICreatureBrainProfile EnsureStationaryGuardProfile()
        {
            return EnsureProfile(
                StationaryGuardProfilePath,
                "DMI_Brain_StationaryGuard",
                p => p.ApplyStationaryGuardDefaults());
        }

        /// <summary>Snappier turn + melee for Ember Skitter critter.</summary>
        public static DMICreatureBrainProfile EnsureEmberSkitterProfile()
        {
            return EnsureProfile(EmberSkitterProfilePath, "DMI_Brain_EmberSkitter", p =>
            {
                p.ApplyWanderDefaults();
                p.turnSpeed = 18f;
                p.agentAngularSpeed = 900f;
                p.meleeHitInterval = 0.55f;
                p.meleeAttackLockDuration = 0.28f;
                p.walkSpeed = 2.6f;
                p.runSpeed = 5.2f;
            });
        }

        public static DMICreatureBrainProfile EnsureDefaultForNewCreature()
        {
            return EnsureWanderProfile();
        }

        private static DMICreatureBrainProfile EnsureProfile(
            string path,
            string name,
            System.Action<DMICreatureBrainProfile> applyDefaults)
        {
            CraftingEditorUtility.EnsureFolder(BrainProfilesFolder);
            DMICreatureBrainProfile existing = AssetDatabase.LoadAssetAtPath<DMICreatureBrainProfile>(path);
            if (existing != null)
                return existing;

            DMICreatureBrainProfile profile = ScriptableObject.CreateInstance<DMICreatureBrainProfile>();
            profile.name = name;
            applyDefaults?.Invoke(profile);
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            return profile;
        }
    }
}
