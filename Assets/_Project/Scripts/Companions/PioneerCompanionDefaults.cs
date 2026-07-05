using Project.Companions.Invector;
using UnityEngine;

namespace Project.Companions
{
    public static class PioneerCompanionDefaults
    {
        public const string DefaultPrefabAssetPath = "Assets/_Project/Prefabs/Companions/PioneerCompanion.prefab";
        public const string DefaultPrefabResourcesPath = "Companions/PioneerCompanion";
        public const string InvectorPrefabAssetPath = "Assets/_Project/Prefabs/Companions/PioneerCompanion_Invector.prefab";
        public const string InvectorPrefabResourcesPath = "Companions/PioneerCompanion_Invector";

        /// <summary>
        /// When true, expedition trio spawns use PioneerCompanion_Invector from Resources.
        /// </summary>
        public static bool UseInvectorStackPref = true;
        public const string CharacterModelPrefabPath = "Assets/_Project/Prefabs/Players/ProjectUnityCharacter.prefab";
        public const string PioneerControllerAssetPath = "Assets/_Project/Animations/PioneerController.controller";
        public const string PioneerControllerResourcesPath = "Animations/PioneerController";
        public const string GkcAnimationAssetsResourcesPath = "Companions/CompanionGkcAnimationAssets";
        public const string DefaultAttackStateName = "AttackCombo1";

        public static CompanionGkcAnimationAssets LoadGkcAnimationAssets()
        {
            return Resources.Load<CompanionGkcAnimationAssets>(GkcAnimationAssetsResourcesPath);
        }

        public static RuntimeAnimatorController LoadGkcAnimatorController()
        {
            CompanionGkcAnimationAssets assets = LoadGkcAnimationAssets();
            if (assets != null && assets.animatorController != null)
                return assets.animatorController;

            return LoadPioneerAnimatorController();
        }

        public static PioneerCompanionAgent LoadDefaultAgentPrefab()
        {
            PioneerCompanionAgent invectorPrefab =
                Resources.Load<PioneerCompanionAgent>(InvectorPrefabResourcesPath);
            PioneerCompanionAgent legacyPrefab =
                Resources.Load<PioneerCompanionAgent>(DefaultPrefabResourcesPath);

            if (UseInvectorStackPref && invectorPrefab != null)
                return invectorPrefab;

            return legacyPrefab != null ? legacyPrefab : invectorPrefab;
        }

        public static bool IsInvectorPrefab(PioneerCompanionAgent prefab)
        {
            return prefab != null &&
                   prefab.GetComponentInChildren<CompanionInvectorBootstrap>(true) != null;
        }

        public static RuntimeAnimatorController LoadPioneerAnimatorController()
        {
            return Resources.Load<RuntimeAnimatorController>(PioneerControllerResourcesPath);
        }
    }
}
