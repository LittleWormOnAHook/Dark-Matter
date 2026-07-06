using Project.Combat;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// Ensures the full Pioneer enemy gameplay stack exists on humanoid Invector bodies.
    /// </summary>
    public static class EnemyInvectorGameplaySetup
    {
        public static void Ensure(GameObject root, EnemyDefinition definition = null)
        {
            if (root == null)
                return;

            EnemyHealth health = GetOrAdd<EnemyHealth>(root);
            EnemySenses senses = GetOrAdd<EnemySenses>(root);
            EnemyCombat combat = GetOrAdd<EnemyCombat>(root);
            EnemyAiController ai = GetOrAdd<EnemyAiController>(root);
            GetOrAdd<EnemyLootable>(root);
            GetOrAdd<EnemyDisintegrationEffect>(root);
            GetOrAdd<EnemyDeathSequence>(root);
            GetOrAdd<EnemyHealthBarPresenter>(root);
            GetOrAdd<EnemyInvectorLoadoutBridge>(root);

            EnemyInvectorHitSetup.EnsureRootDamageReceiver(root);
            EnemyInvectorTargetLayers.Apply(root);

            if (definition != null)
                ApplyDefinition(health, senses, combat, ai, root, definition);
            else
                ApplyRuntimeDefaults(health, senses, combat, ai, root);
        }

        private static void ApplyRuntimeDefaults(
            EnemyHealth health,
            EnemySenses senses,
            EnemyCombat combat,
            EnemyAiController ai,
            GameObject root)
        {
            SetField(health, "maxHealth", 60f);
            SetField(health, "destroyOnDeath", true);
            SetField(health, "destroyDelay", 3f);
            SetField(health, "healthBarOffset", new Vector3(0f, 2f, 0f));

            SetField(ai, "movementMode", (int)EnemyMovementMode.Wander);
            SetField(ai, "walkSpeed", 2.4f);
            SetField(ai, "runSpeed", 4.8f);
            SetField(ai, "wanderRadius", 8f);
            SetField(ai, "chasePlayer", true);

            SetField(combat, "attackRange", 1.8f);
            SetField(combat, "attackDamage", 12f);
            SetField(combat, "attackCooldown", 1.4f);
            SetField(combat, "attackWindup", 0.35f);

            EnemyLootable lootable = root.GetComponent<EnemyLootable>();
            if (lootable != null)
                lootable.ConfigureFromDefinition(null);

            EnemyHealthBarPresenter bar = root.GetComponent<EnemyHealthBarPresenter>();
            if (bar != null)
            {
                SetField(bar, "showFloatingHealthBar", true);
                SetField(bar, "hideUntilDamaged", true);
                SetField(bar, "healthBarOffset", new Vector3(0f, 2f, 0f));
            }
        }

        private static void ApplyDefinition(
            EnemyHealth health,
            EnemySenses senses,
            EnemyCombat combat,
            EnemyAiController ai,
            GameObject root,
            EnemyDefinition definition)
        {
            SetField(health, "maxHealth", definition.maxHealth);
            SetField(health, "destroyOnDeath", definition.destroyOnDeath);
            SetField(health, "destroyDelay", definition.destroyDelay);
            SetField(health, "respawnTime", definition.respawnTime);
            SetField(health, "healthBarOffset", definition.healthBarOffset);

            SetField(senses, "visionRange", definition.visionRange);
            SetField(senses, "visionFov", definition.visionFov);
            SetField(senses, "eyeHeight", definition.eyeHeight);
            SetField(senses, "hearingRange", definition.hearingRange);
            SetField(senses, "proximityRange", definition.proximityRange);

            SetField(combat, "attackRange", definition.attackRange);
            SetField(combat, "attackDamage", definition.attackDamage);
            SetField(combat, "attackCooldown", definition.attackCooldown);
            SetField(combat, "attackWindup", definition.attackWindup);

            SetField(ai, "movementMode", (int)definition.movementMode);
            SetField(ai, "patrolMode", (int)definition.patrolMode);
            SetField(ai, "investigateNoise", definition.investigateNoise);
            SetField(ai, "chasePlayer", definition.chasePlayer);
            SetField(ai, "returnToHomeAfterSearch", definition.returnToHomeAfterSearch);
            SetField(ai, "chaseRadius", definition.chaseRadius);
            SetField(ai, "wanderRadius", definition.wanderRadius);
            SetField(ai, "wanderPauseMin", definition.wanderPauseMin);
            SetField(ai, "wanderPauseMax", definition.wanderPauseMax);
            SetField(ai, "walkSpeed", definition.walkSpeed);
            SetField(ai, "runSpeed", definition.runSpeed);
            SetField(ai, "turnSpeed", definition.turnSpeed);
            SetField(ai, "loseTargetDelay", definition.loseTargetDelay);
            SetField(ai, "searchDuration", definition.searchDuration);
            SetField(ai, "searchRadius", definition.searchRadius);
            SetField(ai, "idleDuration", definition.idleDuration);
            SetField(ai, "patrolWaitDuration", definition.patrolWaitDuration);

            EnemyLootable lootable = root.GetComponent<EnemyLootable>();
            if (lootable != null)
                lootable.ConfigureFromDefinition(definition);

            EnemyHealthBarPresenter bar = root.GetComponent<EnemyHealthBarPresenter>();
            if (bar != null)
            {
                SetField(bar, "showFloatingHealthBar", definition.showFloatingHealthBar);
                SetField(bar, "hideUntilDamaged", definition.hideHealthBarUntilDamaged);
                SetField(bar, "healthBarOffset", definition.healthBarOffset);
            }
        }

        public static void EnsureRootDamageReceiver(GameObject root)
        {
            EnemyInvectorHitSetup.EnsureRootDamageReceiver(root);
        }

        private static T GetOrAdd<T>(GameObject root) where T : Component
        {
            T existing = root.GetComponent<T>();
            if (existing != null)
                return existing;

            return root.AddComponent<T>();
        }

        private static void SetField(Object target, string fieldName, object value)
        {
            if (target == null)
                return;

            var field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);
            field?.SetValue(target, value);
        }
    }
}
