using UnityEngine;

namespace Project.Companions
{
    public static class PioneerCompanionDefaults
    {
        public const string DefaultPrefabAssetPath = "Assets/_Project/Prefabs/Companions/PioneerCompanion.prefab";
        public const string DefaultPrefabResourcesPath = "Companions/PioneerCompanion";
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
            return Resources.Load<PioneerCompanionAgent>(DefaultPrefabResourcesPath);
        }

        public static RuntimeAnimatorController LoadPioneerAnimatorController()
        {
            return Resources.Load<RuntimeAnimatorController>(PioneerControllerResourcesPath);
        }
    }
}
