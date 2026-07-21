using Project.Companions.Invector;
using UnityEngine;

namespace Project.Companions
{
    public static class PioneerCompanionDefaults
    {
        public const string InvectorPrefabAssetPath = "Assets/_Project/Prefabs/Companions/PioneerCompanion_Invector.prefab";
        public const string InvectorPrefabResourcesPath = "Companions/PioneerCompanion_Invector";
        public const string DefaultAttackStateName = "AttackCombo1";

        public static PioneerCompanionAgent LoadDefaultAgentPrefab()
        {
            return Resources.Load<PioneerCompanionAgent>(InvectorPrefabResourcesPath);
        }

        public static bool IsInvectorPrefab(PioneerCompanionAgent prefab)
        {
            return prefab != null &&
                   prefab.GetComponentInChildren<CompanionInvectorBootstrap>(true) != null;
        }
    }
}
