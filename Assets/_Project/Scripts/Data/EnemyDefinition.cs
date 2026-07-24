using UnityEngine;
using UnityEngine.Serialization;
using Project.Data;

namespace Project.AI
{
    public enum EnemyMovementMode
    {
        Stationary,
        Idle,
        Wander,
        Patrol
    }

    public enum EnemyPatrolMode
    {
        Loop,
        PingPong
    }

    public enum EnemyBehaviorPreset
    {
        Custom,
        AggressiveHunter,
        Guard,
        PatrolInvestigator,
        Ambush
    }

    public enum EnemyArchetype
    {
        LegacyCreature,
        HumanoidInvector
    }

    public enum SurfaceThreatKind
    {
        Any,
        Alien,
        Lifeform,
        Android
    }

    [CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Dark Matter Genesis/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string enemyId = "new_enemy";
        public string displayName = "New Enemy";
        public string prefabFileName = "NewEnemy";
        public EnemyArchetype archetype = EnemyArchetype.LegacyCreature;
        [Tooltip("Surface threat category for expedition/map encounter tables.")]
        public SurfaceThreatKind surfaceThreatKind = SurfaceThreatKind.Lifeform;

        [Header("Humanoid Invector")]
        public ItemData meleeWeaponItem;
        public ItemData rangedWeaponItem;
        public bool preferRangedWeapon;

        [Header("Health")]
        public float maxHealth = 60f;
        public bool destroyOnDeath = true;
        public float destroyDelay = 3f;
        public float respawnTime;

        [Header("Progression")]
        public int xpReward = 25;

        [Header("Health Bar")]
        public bool showFloatingHealthBar = true;
        public bool hideHealthBarUntilDamaged = true;
        public Vector3 healthBarOffset = new Vector3(0f, 2f, 0f);

        [Header("Senses")]
        public float visionRange = 16f;
        public float visionFov = 110f;
        public float eyeHeight = 1.4f;
        public float hearingRange = 18f;
        public float proximityRange = 2.5f;

        [Header("Melee Combat")]
        public float attackRange = 1.8f;
        public float attackDamage = 12f;
        public float attackCooldown = 1.4f;
        public float attackWindup = 0.35f;
        [Tooltip("Duration the melee attack animation occupies before the next action is allowed.")]
        public float meleeDuration = 0.85f;
        [Tooltip("Duration of the unarmed (no weapon) melee attack.")]
        public float unarmedDuration = 0.55f;

        [Header("Ranged Combat")]
        [Tooltip("Distance at which the enemy stops chasing and starts shooting.")]
        public float rangedEngageRange = 12f;
        [Tooltip("Cooldown between ranged shots.")]
        public float rangedAttackCooldown = 1.2f;
        [Tooltip("Duration the shoot animation occupies before the next action is allowed.")]
        public float rangedDuration = 0.35f;
        [Tooltip("Seconds the aim pose is held after leaving a ranged engagement before returning to idle.")]
        public float aimHoldDuration = 1.5f;
        [Tooltip("0 = perfect aim, 1 = always misses. Adds a random world-space offset to each shot.")]
        [Range(0f, 1f)]
        public float missRate = 0.25f;

        [Header("Movement Mode")]
        public EnemyMovementMode movementMode = EnemyMovementMode.Wander;
        public EnemyPatrolMode patrolMode = EnemyPatrolMode.Loop;
        public bool investigateNoise = true;
        public bool chasePlayer = true;
        public bool returnToHomeAfterSearch = true;
        [FormerlySerializedAs("homeLeashRadius")]
        [Tooltip("Max horizontal distance from spawn/home to pursue the player. 0 = unlimited.")]
        public float chaseRadius = 0f;

        [Header("Wander")]
        public float wanderRadius = 8f;
        public float wanderPauseMin = 2f;
        public float wanderPauseMax = 5f;

        [Header("Patrol")]
        public int patrolPointCount = 4;
        public float patrolRadius = 6f;
        public float patrolWaitDuration = 2f;

        [Header("AI")]
        public EnemyBehaviorPreset behaviorPreset = EnemyBehaviorPreset.AggressiveHunter;
        public float walkSpeed = 2.4f;
        public float runSpeed = 4.8f;
        [Tooltip("Chase speed when pursuing player/companion. 0 = use runSpeed * chaseSpeedMultiplier.")]
        public float chaseSpeed;
        [Tooltip("Applied when chaseSpeed is 0.")]
        [Range(0.5f, 1.25f)]
        public float chaseSpeedMultiplier = 0.88f;
        public float turnSpeed = 8f;
        public float loseTargetDelay = 4f;
        public float searchDuration = 6f;
        public float searchRadius = 4f;
        public float idleDuration = 3f;

        [Header("Collider")]
        public float colliderRadius = 0.45f;
        public float colliderHeight = 2f;
        public Vector3 colliderCenter = new Vector3(0f, 1f, 0f);
        public bool fitColliderToRenderers = true;

        [Header("Animation Clips")]
        public AnimationClip[] idleClips = System.Array.Empty<AnimationClip>();
        public AnimationClip[] walkClips = System.Array.Empty<AnimationClip>();
        public AnimationClip[] runClips = System.Array.Empty<AnimationClip>();
        public AnimationClip[] attackClips = System.Array.Empty<AnimationClip>();
        public AnimationClip[] hitClips = System.Array.Empty<AnimationClip>();
        public AnimationClip[] deathClips = System.Array.Empty<AnimationClip>();

        [Header("Animation Assets")]
        public RuntimeAnimatorController animatorController;
        public string animatorControllerFileName = "";
        public bool buildAnimatorFromClips = true;
        public bool addEnemyAnimationController = true;
        public bool lockVisualRootPosition;
        public string visualChildName = "scene";

        [Header("Loot")]
        public bool enableLoot = true;

        [Header("Loot AC")]
        [Tooltip("Aether Credits (AC) range dropped by this enemy.")]
        [FormerlySerializedAs("piCoinsMin")]
        public int acDropMin = 1;
        [FormerlySerializedAs("piCoinsMax")]
        public int acDropMax = 5;
        public int randomLootCountMin = 0;
        public int randomLootCountMax = 2;
        public ItemData[] lootItemPool = System.Array.Empty<ItemData>();
        public float lootRespawnDelay = 20f;
        public float lootInteractRange = 2.75f;

        public void ApplyBehaviorPreset(EnemyBehaviorPreset preset)
        {
            behaviorPreset = preset;
            if (preset == EnemyBehaviorPreset.Custom)
                return;

            switch (preset)
            {
                case EnemyBehaviorPreset.AggressiveHunter:
                    movementMode = EnemyMovementMode.Wander;
                    investigateNoise = true;
                    chasePlayer = true;
                    returnToHomeAfterSearch = false;
                    chaseRadius = 0f;
                    wanderRadius = 10f;
                    visionRange = 18f;
                    visionFov = 120f;
                    hearingRange = 20f;
                    proximityRange = 2.5f;
                    walkSpeed = 2.4f;
                    runSpeed = 5.2f;
                    loseTargetDelay = 5f;
                    attackDamage = 12f;
                    attackCooldown = 1.2f;
                    meleeDuration = 0.85f;
                    rangedEngageRange = 12f;
                    rangedAttackCooldown = 1.2f;
                    rangedDuration = 0.35f;
                    aimHoldDuration = 1.5f;
                    break;

                case EnemyBehaviorPreset.Guard:
                    movementMode = EnemyMovementMode.Stationary;
                    investigateNoise = true;
                    chasePlayer = true;
                    returnToHomeAfterSearch = true;
                    chaseRadius = 6f;
                    visionRange = 12f;
                    visionFov = 90f;
                    hearingRange = 14f;
                    proximityRange = 3.5f;
                    walkSpeed = 2f;
                    runSpeed = 4.5f;
                    loseTargetDelay = 2.5f;
                    attackDamage = 10f;
                    attackCooldown = 1.5f;
                    meleeDuration = 0.9f;
                    rangedEngageRange = 14f;
                    rangedAttackCooldown = 1.4f;
                    rangedDuration = 0.4f;
                    aimHoldDuration = 2f;
                    break;

                case EnemyBehaviorPreset.PatrolInvestigator:
                    movementMode = EnemyMovementMode.Patrol;
                    patrolPointCount = 4;
                    patrolRadius = 8f;
                    investigateNoise = true;
                    chasePlayer = true;
                    returnToHomeAfterSearch = true;
                    chaseRadius = 14f;
                    visionRange = 14f;
                    visionFov = 100f;
                    hearingRange = 22f;
                    proximityRange = 2f;
                    walkSpeed = 2.2f;
                    runSpeed = 3.8f;
                    loseTargetDelay = 3f;
                    searchDuration = 8f;
                    attackDamage = 8f;
                    attackCooldown = 1.6f;
                    meleeDuration = 1f;
                    rangedEngageRange = 16f;
                    rangedAttackCooldown = 1.6f;
                    rangedDuration = 0.4f;
                    aimHoldDuration = 2f;
                    break;

                case EnemyBehaviorPreset.Ambush:
                    movementMode = EnemyMovementMode.Stationary;
                    investigateNoise = false;
                    chasePlayer = true;
                    returnToHomeAfterSearch = true;
                    chaseRadius = 4f;
                    visionRange = 8f;
                    visionFov = 70f;
                    hearingRange = 10f;
                    proximityRange = 4.5f;
                    walkSpeed = 1.8f;
                    runSpeed = 5.5f;
                    loseTargetDelay = 2f;
                    attackDamage = 18f;
                    attackCooldown = 2f;
                    attackWindup = 0.5f;
                    meleeDuration = 1.1f;
                    rangedEngageRange = 10f;
                    rangedAttackCooldown = 1.8f;
                    rangedDuration = 0.5f;
                    aimHoldDuration = 1f;
                    break;
            }
        }
    }
}
