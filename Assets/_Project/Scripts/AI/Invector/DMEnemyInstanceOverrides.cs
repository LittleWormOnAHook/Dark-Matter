using System;
using Project.AI;
using Project.Combat;
using UnityEngine;

namespace Project.AI.Invector
{
    public enum DMOverrideBool { UseDefinition, ForceOn, ForceOff }

    /// <summary>
    /// 2026-10-13: per-instance overrides of the EnemyDefinition "Type, Body &amp; Brain" fields, on EnemyInvectorLoadoutBridge.
    /// Every toggle defaults off / every profile empty = use the EnemyDefinition, so existing prefabs are unaffected.
    /// Effective value: instance override, then definition, then global profile.
    /// </summary>
    [Serializable]
    public class DMEnemyInstanceOverrides
    {
        public bool overrideSurfaceThreatKind;
        public SurfaceThreatKind surfaceThreatKind = SurfaceThreatKind.Lifeform;
        public bool overrideEnemyCategory;
        public EnemyCategory enemyCategory = EnemyCategory.Grunt;
        [Tooltip("e.g. a Humanoid definition used as a Robot: drives blood pool, sparks vs blood and reach.")]
        public bool overrideBodyType;
        public DMEnemyBodyType bodyType = DMEnemyBodyType.Humanoid;

        [Tooltip("On = this instance uses the archetype / personalities below (ignores the definition's brain settings).")]
        public bool overrideBrain;
        public DMEnemyArchetype brainArchetype = DMEnemyArchetype.Duelist;
        public DMEnemyPersonality primaryPersonality = DMEnemyPersonality.None;
        public DMEnemyPersonality secondaryPersonality = DMEnemyPersonality.None;

        [Tooltip("Empty = definition's, then global.")] public DM_EnemyBrainProfile brainProfileOverride;
        [Tooltip("Empty = definition's, then global.")] public DM_EnemyEngagementProfile engagementProfileOverride;
        [Tooltip("Empty = definition's, then global.")] public DM_EnemyHitMarkProfile hitMarkProfileOverride;

        public DMOverrideBool canThrowGrenades = DMOverrideBool.UseDefinition;

        public bool overrideWeaponDown;
        [Range(0f, 90f)] public float weaponDownAngle = 60f;
        [Range(0f, 30f)] public float weaponDownVariation = 5f;

        [Tooltip("Only when Override Brain is on (prefab). Use Grip Default = follow EnemyDefinition.rangedAmmoType.")]
        public DMEnemyRangedAmmoChoice overrideRangedAmmoType = DMEnemyRangedAmmoChoice.UseGripDefault;

        public bool Any =>
            overrideSurfaceThreatKind || overrideEnemyCategory || overrideBodyType || overrideBrain
            || brainProfileOverride != null || engagementProfileOverride != null || hitMarkProfileOverride != null
            || canThrowGrenades != DMOverrideBool.UseDefinition || overrideWeaponDown;
    }
}
