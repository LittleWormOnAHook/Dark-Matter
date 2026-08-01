using System;
using System.Collections.Generic;
using MalbersAnimations.Controller.AI;
using Project.AI;
using Project.Data;
using UnityEngine;

namespace Project.Creatures
{
    [Serializable]
    public class DMICreatureAnimEntry
    {
        [Tooltip("Animator state name (Idle, Walk, Run, Attack, Death, or custom).")]
        public string stateName = "Idle";
        public AnimationClip clip;
    }

    public enum DMIAnimalControllerTemplate
    {
        WolfLiteAiEnemy,
        EmptyController
    }

    public enum DMICreatureVisualSourceMode
    {
        DefinitionMesh,
        SelectedHierarchyObject,
        ExistingPrefab
    }

    /// <summary>
    /// RiggedNative = default: any rigged mesh + project AI + anim slots.
    /// MalbersAcV1 = Legacy Wolf Lite AC + OnWolf / AutoReskin (Sulfur Hound).
    /// MeshyNativeV2A = obsolete alias for RiggedNative (serialized value 1 still routes to rigged path).
    /// </summary>
    public enum DMICreatureBuildTrack
    {
        MalbersAcV1 = 0,
        MeshyNativeV2A = 1,
        RiggedNative = 2
    }

    public enum DMICreatureRigArchetype
    {
        BipedHumanoid,
        QuadrupedGeneric,
        CustomGeneric
    }

    /// <summary>
    /// Authoring data for DMI creatures built via Creatures Manager.
    /// Separate from <see cref="EnemyDefinition"/> (Invector humanoids).
    /// </summary>
    [CreateAssetMenu(fileName = "CreatureDefinition", menuName = "Dark Matter Genesis/Creature Definition")]
    public class DMICreatureDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string creatureId = "new_creature";
        public string displayName = "New Creature";
        public string prefabFileName = "New_Creature";
        public SurfaceThreatKind surfaceThreatKind = SurfaceThreatKind.Lifeform;

        [Header("Rig")]
        public DMICreatureRigArchetype rigArchetype = DMICreatureRigArchetype.CustomGeneric;

        [Tooltip(
            "Meters applied to CreatureVisual.localPosition.y after AlignVisualFeetToGround. " +
            "0 = auto feet-align only. Negative lowers when auto-align overshoots (mesh AABB below foot bones).")]
        public float heightOffset = 0f;

        [Tooltip(
            "Uniform scale applied to the creature root at build time (capsule + NavMeshAgent refit after). " +
            "1 = source size. Prefer root scale so collision matches visual size.")]
        [Min(0.01f)]
        public float prefabScale = 1f;

        [Header("Melee Hit Reception")]
        [Tooltip(
            "Optional capsule height override (meters) for player melee reception. " +
            "0 = auto from visual bounds. Raises/enlarges hit volume for short creatures so player " +
            "chest-height melee connects without changing player swings.")]
        [Min(0f)]
        public float hitCapsuleHeight = 0f;

        [Tooltip("Optional capsule radius override (meters). 0 = auto from visual bounds.")]
        [Min(0f)]
        public float hitCapsuleRadius = 0f;

        [Tooltip(
            "Added to auto/override capsule center Y (local meters). " +
            "Positive raises the hit volume toward mid/upper body for low-to-ground creatures.")]
        public float hitCapsuleCenterYOffset = 0f;

        [Tooltip("Multiplies auto (or override) capsule height. 1 = unchanged.")]
        [Min(0.01f)]
        public float meleeHitHeightMultiplier = 1f;

        [Tooltip("Multiplies auto (or override) capsule radius. 1 = unchanged.")]
        [Min(0.01f)]
        public float meleeHitRadiusMultiplier = 1f;

        [Header("Brain (RiggedNative)")]
        [Tooltip("Generic project-AI brain: Stationary / Wander / Patrol + combat feature toggles. Not Malbers.")]
        public DMICreatureBrainProfile brainProfile;

        [Header("Wander / Idle Timing")]
        [Tooltip("Base seconds spent idle before the next wander/patrol leg. Creature Manager authority.")]
        [Min(0f)]
        public float idleDuration = 1.5f;
        [Range(0f, 10f)]
        [Tooltip("Extra random idle time: wait = Idle Duration + Random(0, this). 0 = deterministic.")]
        public float idleDurationVariation = 0f;
        [Tooltip(
            "Base max seconds for a wander walk before returning to idle. " +
            "0 = no timeout (walk until arrival). Creature Manager authority.")]
        [Min(0f)]
        public float wanderDuration = 0f;
        [Range(0f, 10f)]
        [Tooltip("Extra random wander timeout: timeout = Wander Duration + Random(0, this). Ignored when Wander Duration is 0 and this is 0.")]
        public float wanderDurationVariation = 0f;

        [Header("Build Track")]
        [Tooltip("RiggedNative is the default. MalbersAcV1 is Legacy only.")]
        public DMICreatureBuildTrack buildTrack = DMICreatureBuildTrack.RiggedNative;

        [Header("Legacy Malbers AC")]
        public DMIAnimalControllerTemplate acTemplate = DMIAnimalControllerTemplate.WolfLiteAiEnemy;
        public MAIState startBrainState;

        [Header("Animator")]
        [Tooltip("Generated or assigned AnimatorController for RiggedNative builds.")]
        public RuntimeAnimatorController v2AnimatorController;
        [Tooltip("When true, build/rebuild regenerates the AnimatorController from clip slots.")]
        public bool generateAnimatorFromSlots = true;

        [Header("Animations")]
        [Tooltip("Variable animation list. State names Idle/Walk/Run/Attack/Death drive locomotion AI; extras are optional custom states.")]
        public DMICreatureAnimEntry[] animationEntries = Array.Empty<DMICreatureAnimEntry>();

        // Legacy fixed slots (migrated into animationEntries on load). Kept for asset upgrade.
        [HideInInspector] public AnimationClip idleClip;
        [HideInInspector] public AnimationClip walkClip;
        [HideInInspector] public AnimationClip runClip;
        [HideInInspector] public AnimationClip attackClip;
        [HideInInspector] public AnimationClip deathClip;
        [HideInInspector] public AnimationClip hitClip;

        [Header("Visual Source")]
        public DMICreatureVisualSourceMode visualSourceMode = DMICreatureVisualSourceMode.DefinitionMesh;
        [Tooltip("Default mesh prefab/FBX when visual source mode is Definition Mesh.")]
        public GameObject visualMeshSource;
        [Tooltip(
            "Body material assigned to CreatureVisual on RiggedNative builds. " +
            "Also filters which renderer materials DMICreatureEmissionDriver pulses.")]
        public Material visualMaterialSource;
        [Tooltip(
            "Legacy OnWolf / Blender: mesh already uses Malbers Wolf bone names. " +
            "Bind sharedMesh + remap bones onto AC Mesh — do NOT run AutoReskin.")]
        public bool skipAutoReskin = true;

        [Header("Material Source Emission")]
        [Tooltip(
            "When true, raise _EmissionColor while melee or ranged/spit Attack plays. " +
            "Intensity mapping: authored material emission = look at Emission Idle. " +
            "Applied = authored * (currentIntensity / Emission Idle). Idle 5 / Attack 10 ⇒ 2× glow.")]
        public bool boostEmissionWhileAttacking = false;
        [Tooltip("Pulse emission between idle and attack intensity for the attack-lock window.")]
        public bool flashEmissionWhileAttacking = false;
        [Min(0.01f)]
        [Tooltip("Idle / normal emission intensity unit. Authored _EmissionColor is the look at this value.")]
        public float emissionIdleIntensity = 5f;
        [Min(0.01f)]
        [Tooltip("Emission intensity while attacking (melee or ranged). Relative to Emission Idle.")]
        public float emissionAttackIntensity = 10f;
        [Min(0.1f)]
        [Tooltip("Flash oscillation rate in Hz when Flash While Attacking is on.")]
        public float emissionFlashRateHz = 8f;
        [Tooltip("Optional HDR tint multiplied into emission at flash peaks (white = hue unchanged).")]
        public Color emissionFlashTint = Color.white;

        [Header("Health")]
        public float maxHealth = 80f;
        public bool destroyOnDeath = true;
        public float destroyDelay = 3f;

        [Header("Death Dissolve")]
        [Tooltip("Reuse EnemyDisintegrationEffect + EnemyDeathSequence (Project/EnemyDisintegrate). Default ON.")]
        public bool dissolveOnDeath = true;
        [Tooltip("Seconds of death pose / linger before dissolve starts. Critters ~1–1.5s; humanoids may use longer.")]
        [Min(0f)]
        public float preDisintegrationDelay = 1.25f;

        [Header("Threat / Melee")]
        [Tooltip("Max distance to newly acquire a threat (player, pets, companions, non-ally creatures). Vision/engage range.")]
        public float threatSenseRange = 9f;
        [Tooltip("After engaging, keep chase until threat exceeds sense × this multiplier (leash).")]
        public float threatLeashMultiplier = 1.4f;
        [Tooltip("Seconds out of leash before dropping target and returning to patrol.")]
        public float loseTargetDelay = 2.5f;
        [Tooltip("Enter melee brain state when threat is within this distance.")]
        public float meleeEngageRange = 2.75f;
        [Tooltip("Damage applied to IDamageable targets on melee hit.")]
        public float meleeDamage = 12f;
        [Min(0.05f)]
        [Tooltip("Base seconds between melee attacks (melee interval / delay). Creature Manager authority — overrides brain profile meleeHitInterval on build/runtime.")]
        public float meleeAttackCooldown = 1.1f;
        [Range(0f, 10f)]
        [Tooltip("Extra random delay added after each melee hit: wait = Melee Interval + Random(0, this). 0 = deterministic.")]
        public float meleeIntervalVariation = 0f;

        [Header("AI Senses")]
        [Tooltip("When true, this creature listens to EnemyNoiseEvents (ranged impacts, etc.).")]
        public bool senseHearingEnabled = true;
        [Tooltip("Audio sense radius. Hears combat impacts within hearingRange + impact noise radius.")]
        public float hearingRange = 14f;
        [Tooltip("Chance (0–1) to Chase the shooter when a ranged impact is heard nearby.")]
        [Range(0f, 1f)]
        public float hearingAggroChance = 0.55f;
        [Tooltip("Seconds between hearing-aggro rolls so burst fire does not spam.")]
        [Min(0f)]
        public float hearingCooldown = 0.8f;
        [Tooltip("Direct damage (melee or ranged) pulls this creature into Chase.")]
        public bool aggroOnDamaged = true;
        [Tooltip("Hearing a nearby ranged impact may pull this creature into Chase.")]
        public bool aggroOnHeardHit = true;

        [Header("Progression")]
        public int xpReward = 30;

        [Header("Health Bar")]
        public bool showFloatingHealthBar = true;
        public bool hideHealthBarUntilDamaged = true;
        public Vector3 healthBarOffset = new Vector3(0f, 1.6f, 0f);

        [Header("Ranged Particle Attack")]
        [Tooltip("Enable spit / breath / ball style ranged special for this creature.")]
        public bool enableRangedParticleAttack = true;
        [Range(0f, 1f)]
        [Tooltip("Malbers brain / optional prefer-spit weight. RiggedNative fire rate uses Ranged Attack Cooldown, not this chance.")]
        public float spitBaseChance = 0.12f;
        [Range(0f, 1f)]
        [Tooltip("Malbers / view-boosted prefer-spit weight. Does not replace Ranged Attack Cooldown for RiggedNative.")]
        public float spitViewBoostedChance = 0.45f;
        public float spitRange = 14f;
        [Min(0.05f)]
        [Tooltip("Base seconds between ranged / spit shots. Creature Manager authority applied to DMISulfurSpitAttack on build and runtime.")]
        public float spitCooldown = 6f;
        [Range(0f, 10f)]
        [Tooltip("Extra random delay added after each ranged shot: wait = Ranged Cooldown + Random(0, this). 0 = deterministic.")]
        public float spitCooldownVariation = 0f;
        public float spitDamage = 10f;
        [Tooltip("Particle VFX from Assets/_Project/Prefabs/Particles (Poison Spit, FireBreath, Plasma Ball, etc.).")]
        public GameObject spitVfxPrefab;

        [Header("Audio")]
        [Tooltip("Primary footstep / move SFX played on an interval while the creature is moving (not idle).")]
        public AudioClip walkFootstepClip;
        [Tooltip("Optional extra walk variants; a random pick from primary + variants each step.")]
        public AudioClip[] walkFootstepVariants = System.Array.Empty<AudioClip>();
        [Range(0f, 1f)] public float walkVolume = 0.55f;
        [Min(0.05f)]
        [Tooltip("Seconds between footstep one-shots while moving.")]
        public float footstepInterval = 0.4f;
        [Tooltip("One-shot when ranged / spit fires (same moment as Attack anim).")]
        public AudioClip rangedAttackClip;
        [Range(0f, 1f)] public float rangedAttackVolume = 0.85f;
        [Tooltip("Optional one-shot when melee Attack fires.")]
        public AudioClip meleeAttackClip;
        [Range(0f, 1f)] public float meleeAttackVolume = 0.85f;
        [Tooltip("One-shot when death starts (same moment as Death anim / before dissolve).")]
        public AudioClip deathAudioClip;
        [Range(0f, 1f)] public float deathVolume = 0.9f;
        [Tooltip("3D spatial min distance for creature AudioSource.")]
        public float audioMinDistance = 2f;
        [Tooltip("3D spatial max distance for creature AudioSource.")]
        public float audioMaxDistance = 28f;

        [Header("Loot")]
        public bool enableLoot = true;
        public int acDropMin = 2;
        public int acDropMax = 8;
        public int randomLootCountMin = 0;
        public int randomLootCountMax = 2;
        public ItemData[] lootItemPool = System.Array.Empty<ItemData>();
        public float lootRespawnDelay = 20f;
        public float lootInteractRange = 2.75f;

        public bool UsesLegacyMalbers => buildTrack == DMICreatureBuildTrack.MalbersAcV1;

        public bool UsesRiggedNative =>
            buildTrack == DMICreatureBuildTrack.RiggedNative
            || buildTrack == DMICreatureBuildTrack.MeshyNativeV2A;

        public AnimationClip GetAnimClip(string stateName)
        {
            if (string.IsNullOrWhiteSpace(stateName) || animationEntries == null)
                return null;

            for (int i = 0; i < animationEntries.Length; i++)
            {
                DMICreatureAnimEntry entry = animationEntries[i];
                if (entry == null || entry.clip == null || string.IsNullOrWhiteSpace(entry.stateName))
                    continue;
                if (string.Equals(entry.stateName.Trim(), stateName.Trim(), StringComparison.OrdinalIgnoreCase))
                    return entry.clip;
            }

            return null;
        }

        public bool HasAnyAnimationClip()
        {
            if (animationEntries != null)
            {
                for (int i = 0; i < animationEntries.Length; i++)
                {
                    if (animationEntries[i] != null && animationEntries[i].clip != null)
                        return true;
                }
            }

            return idleClip != null || walkClip != null || runClip != null
                   || attackClip != null || deathClip != null || hitClip != null;
        }

        /// <summary>
        /// Ensures the variable list exists and migrates legacy fixed clip fields once.
        /// </summary>
        public void EnsureAnimationEntriesMigrated()
        {
            if (animationEntries != null && animationEntries.Length > 0)
                return;

            var list = new List<DMICreatureAnimEntry>();
            void Add(string name, AnimationClip clip)
            {
                if (clip == null)
                    return;
                list.Add(new DMICreatureAnimEntry { stateName = name, clip = clip });
            }

            Add("Idle", idleClip);
            Add("Walk", walkClip);
            Add("Run", runClip);
            Add("Attack", attackClip);
            Add("Death", deathClip);
            Add("Hit", hitClip);

            if (list.Count == 0)
            {
                animationEntries = CreateDefaultAnimationEntries();
                return;
            }

            animationEntries = list.ToArray();
        }

        public static DMICreatureAnimEntry[] CreateDefaultAnimationEntries()
        {
            return new[]
            {
                new DMICreatureAnimEntry { stateName = "Idle" },
                new DMICreatureAnimEntry { stateName = "Walk" },
                new DMICreatureAnimEntry { stateName = "Run" },
                new DMICreatureAnimEntry { stateName = "Attack" },
                new DMICreatureAnimEntry { stateName = "Death" }
            };
        }

        public void ApplyNewCreatureDefaults()
        {
            creatureId = "new_creature";
            displayName = "New Creature";
            prefabFileName = "New_Creature";
            surfaceThreatKind = SurfaceThreatKind.Lifeform;
            buildTrack = DMICreatureBuildTrack.RiggedNative;
            rigArchetype = DMICreatureRigArchetype.CustomGeneric;
            heightOffset = 0f;
            prefabScale = 1f;
            hitCapsuleHeight = 0f;
            hitCapsuleRadius = 0f;
            hitCapsuleCenterYOffset = 0f;
            meleeHitHeightMultiplier = 1f;
            meleeHitRadiusMultiplier = 1f;
            generateAnimatorFromSlots = true;
            animationEntries = CreateDefaultAnimationEntries();
            brainProfile = null;
            idleDuration = 1.5f;
            idleDurationVariation = 0f;
            wanderDuration = 0f;
            wanderDurationVariation = 0f;
            skipAutoReskin = true;
            boostEmissionWhileAttacking = false;
            flashEmissionWhileAttacking = false;
            emissionIdleIntensity = 5f;
            emissionAttackIntensity = 10f;
            emissionFlashRateHz = 8f;
            emissionFlashTint = Color.white;
            maxHealth = 80f;
            destroyOnDeath = true;
            destroyDelay = 3f;
            dissolveOnDeath = true;
            preDisintegrationDelay = 1.25f;
            threatSenseRange = 9f;
            threatLeashMultiplier = 1.4f;
            loseTargetDelay = 2.5f;
            meleeEngageRange = 2.75f;
            meleeDamage = 12f;
            meleeAttackCooldown = 1.1f;
            meleeIntervalVariation = 0f;
            senseHearingEnabled = true;
            hearingRange = 14f;
            hearingAggroChance = 0.55f;
            hearingCooldown = 0.8f;
            aggroOnDamaged = true;
            aggroOnHeardHit = true;
            xpReward = 30;
            enableRangedParticleAttack = false;
            spitBaseChance = 0.12f;
            spitViewBoostedChance = 0.45f;
            spitRange = 14f;
            spitCooldown = 6f;
            spitCooldownVariation = 0f;
            spitDamage = 10f;
            walkFootstepClip = null;
            walkFootstepVariants = System.Array.Empty<AudioClip>();
            walkVolume = 0.55f;
            footstepInterval = 0.4f;
            rangedAttackClip = null;
            rangedAttackVolume = 0.85f;
            meleeAttackClip = null;
            meleeAttackVolume = 0.85f;
            deathAudioClip = null;
            deathVolume = 0.9f;
            audioMinDistance = 2f;
            audioMaxDistance = 28f;
            acDropMin = 2;
            acDropMax = 8;
        }

        public void ApplySulfurHoundDefaults()
        {
            creatureId = "sulfur_hound";
            displayName = "Sulfur Hound";
            prefabFileName = "Sulfur_Hound";
            surfaceThreatKind = SurfaceThreatKind.Lifeform;
            buildTrack = DMICreatureBuildTrack.MalbersAcV1;
            rigArchetype = DMICreatureRigArchetype.QuadrupedGeneric;
            acTemplate = DMIAnimalControllerTemplate.WolfLiteAiEnemy;
            skipAutoReskin = true;
            generateAnimatorFromSlots = false;
            maxHealth = 80f;
            destroyOnDeath = true;
            destroyDelay = 3f;
            dissolveOnDeath = true;
            preDisintegrationDelay = 1.5f;
            threatSenseRange = 9f;
            threatLeashMultiplier = 1.4f;
            loseTargetDelay = 2.5f;
            meleeEngageRange = 2.75f;
            meleeDamage = 12f;
            meleeAttackCooldown = 1.1f;
            meleeIntervalVariation = 0f;
            senseHearingEnabled = true;
            hearingRange = 16f;
            hearingAggroChance = 0.5f;
            hearingCooldown = 0.75f;
            aggroOnDamaged = true;
            aggroOnHeardHit = true;
            idleDuration = 1.5f;
            idleDurationVariation = 0f;
            wanderDuration = 0f;
            wanderDurationVariation = 0f;
            xpReward = 30;
            spitBaseChance = 0.12f;
            spitViewBoostedChance = 0.45f;
            spitRange = 14f;
            spitCooldown = 6f;
            spitCooldownVariation = 0f;
            spitDamage = 10f;
            enableRangedParticleAttack = true;
            acDropMin = 2;
            acDropMax = 8;
        }

        /// <summary>
        /// Randomized melee wait: base interval + Random(0, variation). Never below base floor.
        /// </summary>
        public float SampleMeleeInterval()
        {
            float interval = Mathf.Max(0.05f, meleeAttackCooldown);
            float variation = Mathf.Clamp(meleeIntervalVariation, 0f, 10f);
            return variation > 0f ? interval + UnityEngine.Random.Range(0f, variation) : interval;
        }

        /// <summary>
        /// Randomized ranged wait: base cooldown + Random(0, variation). Never below base floor.
        /// </summary>
        public float SampleRangedInterval()
        {
            float interval = Mathf.Max(0.05f, spitCooldown);
            float variation = Mathf.Clamp(spitCooldownVariation, 0f, 10f);
            return variation > 0f ? interval + UnityEngine.Random.Range(0f, variation) : interval;
        }

        /// <summary>
        /// Randomized idle wait: base + Random(0, variation).
        /// </summary>
        public float SampleIdleDuration()
        {
            float duration = Mathf.Max(0f, idleDuration);
            float variation = Mathf.Clamp(idleDurationVariation, 0f, 10f);
            return variation > 0f ? duration + UnityEngine.Random.Range(0f, variation) : duration;
        }

        /// <summary>
        /// Randomized wander timeout: base + Random(0, variation). 0 = walk until arrival (no timeout).
        /// </summary>
        public float SampleWanderDuration()
        {
            float duration = Mathf.Max(0f, wanderDuration);
            float variation = Mathf.Clamp(wanderDurationVariation, 0f, 10f);
            return variation > 0f ? duration + UnityEngine.Random.Range(0f, variation) : duration;
        }
    }
}
