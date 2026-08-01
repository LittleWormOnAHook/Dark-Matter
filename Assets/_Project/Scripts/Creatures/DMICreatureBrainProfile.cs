using UnityEngine;

namespace Project.Creatures
{
    public enum DMICreatureMovementMode
    {
        Stationary,
        Wander,
        Patrol
    }

    public enum DMICreaturePatrolMode
    {
        Loop,
        PingPong
    }

    /// <summary>
    /// Generic project-AI brain profile for RiggedNative creatures (no Malbers).
    /// Mirrors common enemy patrol/wander/guard behaviors used by <see cref="Project.AI.EnemyAiController"/>.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CreatureBrainProfile",
        menuName = "Dark Matter Genesis/Creature Brain Profile")]
    public class DMICreatureBrainProfile : ScriptableObject
    {
        [Header("Idle Movement")]
        [Tooltip("Stationary = idle until threat. Wander = random around home. Patrol = generated route around home.")]
        public DMICreatureMovementMode movementMode = DMICreatureMovementMode.Wander;

        [Header("Speeds")]
        public float walkSpeed = 2.2f;
        public float runSpeed = 4.5f;
        [Tooltip("Slerp factor for FaceToward (melee / fallback facing). Higher = snappier turns.")]
        public float turnSpeed = 8f;
        [Tooltip("NavMeshAgent.angularSpeed (deg/sec) while pathing. 0 = leave agent default.")]
        public float agentAngularSpeed = 480f;
        public float stopDistance = 0.4f;

        [Header("Wander / Idle")]
        public float wanderRadius = 8f;
        [Tooltip("Base seconds spent idle before next wander/patrol leg.")]
        public float idleDurationMin = 1.5f;
        [Tooltip("Legacy upper idle bound. Prefer idleDurationVariation when set via Creature Manager.")]
        public float idleDurationMax = 3.5f;
        [Range(0f, 10f)]
        [Tooltip("Extra random idle time: wait = idleDurationMin + Random(0, this).")]
        public float idleDurationVariation = 0f;
        [Tooltip("Base max seconds for a wander walk before idle. 0 = until arrival.")]
        [Min(0f)]
        public float wanderDuration = 0f;
        [Range(0f, 10f)]
        [Tooltip("Extra random wander timeout: timeout = wanderDuration + Random(0, this).")]
        public float wanderDurationVariation = 0f;
        public float navMeshSampleRadius = 2.5f;

        [Header("Patrol")]
        [Tooltip("Loop / PingPong. Assign Path Creator on the creature AI for the route; point count/radius are fallbacks only.")]
        public DMICreaturePatrolMode patrolMode = DMICreaturePatrolMode.Loop;
        [Min(2)] public int patrolPointCount = 4;
        public float patrolRadius = 6f;
        public float patrolWaitDuration = 2f;

        [Header("Combat Features")]
        public bool allowChase = true;
        public bool allowMelee = true;
        [Tooltip("Also requires definition.enableRangedParticleAttack + spit component.")]
        public bool allowRangedSpit = true;
        [Tooltip("Seconds between melee swing pulses.")]
        public float meleeHitInterval = 1.1f;
        [Tooltip("How long Attack anim locks locomotion after each swing (windup feel). Lower = snappier.")]
        public float meleeAttackLockDuration = 0.55f;

        public void ApplyWanderDefaults()
        {
            movementMode = DMICreatureMovementMode.Wander;
            walkSpeed = 2.2f;
            runSpeed = 4.5f;
            turnSpeed = 8f;
            agentAngularSpeed = 480f;
            wanderRadius = 8f;
            idleDurationMin = 1.5f;
            idleDurationMax = 3.5f;
            idleDurationVariation = 0f;
            wanderDuration = 0f;
            wanderDurationVariation = 0f;
            allowChase = true;
            allowMelee = true;
            allowRangedSpit = true;
            meleeHitInterval = 1.1f;
            meleeAttackLockDuration = 0.55f;
            patrolPointCount = 4;
            patrolRadius = 6f;
        }

        public void ApplyPatrolDefaults()
        {
            ApplyWanderDefaults();
            movementMode = DMICreatureMovementMode.Patrol;
            patrolMode = DMICreaturePatrolMode.Loop;
            patrolPointCount = 4;
            patrolRadius = 8f;
            patrolWaitDuration = 2f;
        }

        public void ApplyStationaryGuardDefaults()
        {
            ApplyWanderDefaults();
            movementMode = DMICreatureMovementMode.Stationary;
            wanderRadius = 0f;
            allowChase = true;
            allowMelee = true;
        }
    }
}
