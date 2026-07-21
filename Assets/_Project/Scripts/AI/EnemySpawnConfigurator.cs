using System;
using Invector.vShooter;
using Project.AI.Invector;
using Project.Data;
using UnityEngine;

namespace Project.AI
{
    public enum EnemySpawnWeaponMode
    {
        UseDefinitionDefaults,
        FixedMelee,
        FixedRanged,
        RandomFromPool,
    }

    /// <summary>
    /// Applies spawn-time overrides to an enemy instance (used by EnemySpawner).
    /// </summary>
    public static class EnemySpawnConfigurator
    {
        public static void Apply(GameObject enemy, EnemySpawnSettings settings, EnemyDefinition definition = null)
        {
            if (enemy == null || settings == null)
                return;

            definition ??= settings.definition ?? EnemyPrefabResolver.GetDefinition(enemy);
            EnemyInvectorGameplaySetup.Ensure(enemy, definition);
            EnemyInvectorTargetLayers.Apply(enemy);

            if (settings.overrideHealth)
            {
                EnemyHealth health = enemy.GetComponent<EnemyHealth>();
                if (health != null)
                {
                    SetField(health, "maxHealth", settings.maxHealth);
                    if (settings.respawnTime >= 0f)
                        SetField(health, "respawnTime", settings.respawnTime);
                }
            }

            if (settings.overrideCombat)
            {
                EnemyCombat combat = enemy.GetComponent<EnemyCombat>();
                if (combat != null)
                {
                    SetField(combat, "attackDamage",   settings.attackDamage);
                    SetField(combat, "attackRange",    settings.attackRange);
                    SetField(combat, "attackCooldown", settings.attackCooldown);
                    SetField(combat, "attackWindup",   settings.attackWindup);
                }

                EnemyInvectorCombatBridge bridge = enemy.GetComponent<EnemyInvectorCombatBridge>();
                if (bridge != null)
                {
                    SetField(bridge, "defaultMeleeDuration",   settings.meleeDuration);
                    SetField(bridge, "defaultUnarmedDuration", settings.unarmedDuration);
                }
            }

            if (settings.overrideRangedCombat)
            {
                EnemyInvectorCombatBridge bridge = enemy.GetComponent<EnemyInvectorCombatBridge>();
                if (bridge != null)
                {
                    SetField(bridge, "rangedEngageRange",     settings.rangedEngageRange);
                    SetField(bridge, "defaultRangedDuration", settings.rangedDuration);
                    SetField(bridge, "aimHoldDuration",       settings.aimHoldDuration);
                    SetField(bridge, "missRate",              settings.missRate);
                }

                EnemyCombat combat = enemy.GetComponent<EnemyCombat>();
                if (combat != null)
                    SetField(combat, "attackCooldown", settings.rangedAttackCooldown);
            }

            if (settings.overrideMovement)
            {
                EnemyAiController ai = enemy.GetComponent<EnemyAiController>();
                if (ai != null)
                {
                    SetField(ai, "movementMode", (int)settings.movementMode);
                    SetField(ai, "walkSpeed", settings.walkSpeed);
                    SetField(ai, "runSpeed", settings.runSpeed);
                    SetField(ai, "wanderRadius", settings.wanderRadius);
                    SetField(ai, "chasePlayer", settings.chasePlayer);
                }
            }

            ApplyHumanoidLoadout(enemy, settings, definition);
            ApplyInfiniteAmmo(enemy, settings.infiniteAmmo);
        }

        private static void ApplyHumanoidLoadout(
            GameObject enemy,
            EnemySpawnSettings settings,
            EnemyDefinition definition)
        {
            EnemyInvectorLoadoutBridge loadout = enemy.GetComponent<EnemyInvectorLoadoutBridge>();
            if (loadout == null)
                return;

            if (!settings.overrideWeaponLoadout)
                return;

            ResolveWeaponChoice(settings, definition, out ItemData melee, out ItemData ranged, out bool startMelee);

            loadout.ConfigureSpawnLoadout(
                melee,
                ranged,
                startMelee,
                settings.preferRangedAtRange);
        }

        public static void ResolveWeaponChoice(
            EnemySpawnSettings settings,
            EnemyDefinition definition,
            out ItemData melee,
            out ItemData ranged,
            out bool startMelee)
        {
            melee = settings.startMeleeWeapon;
            ranged = settings.startRangedWeapon;
            startMelee = settings.startWithMeleeWeapon;

            if (definition != null)
            {
                if (melee == null)
                    melee = definition.meleeWeaponItem;
                if (ranged == null)
                    ranged = definition.rangedWeaponItem;
            }

            switch (settings.weaponMode)
            {
                case EnemySpawnWeaponMode.FixedMelee:
                    startMelee = true;
                    break;

                case EnemySpawnWeaponMode.FixedRanged:
                    startMelee = false;
                    break;

                case EnemySpawnWeaponMode.RandomFromPool:
                {
                    ItemData picked = PickRandomWeapon(settings);
                    if (picked == null)
                        break;

                    if (picked.itemType == ItemType.MeleeWeapon)
                    {
                        melee = picked;
                        startMelee = true;
                    }
                    else if (picked.IsRangedWeapon)
                    {
                        ranged = picked;
                        startMelee = false;
                    }

                    break;
                }
            }
        }

        private static ItemData PickRandomWeapon(EnemySpawnSettings settings)
        {
            if (settings.randomWeaponPool != null && settings.randomWeaponPool.Length > 0)
            {
                int index = UnityEngine.Random.Range(0, settings.randomWeaponPool.Length);
                return settings.randomWeaponPool[index];
            }

            bool pickMelee = UnityEngine.Random.value > 0.5f;
            if (pickMelee && settings.startMeleeWeapon != null)
                return settings.startMeleeWeapon;
            if (!pickMelee && settings.startRangedWeapon != null)
                return settings.startRangedWeapon;
            if (settings.definition?.meleeWeaponItem != null && settings.definition?.rangedWeaponItem != null)
                return UnityEngine.Random.value > 0.5f
                    ? settings.definition.meleeWeaponItem
                    : settings.definition.rangedWeaponItem;

            return settings.startMeleeWeapon != null
                ? settings.startMeleeWeapon
                : settings.startRangedWeapon;
        }

        private static void ApplyInfiniteAmmo(GameObject enemy, bool infiniteAmmo)
        {
            if (!infiniteAmmo)
                return;

            vShooterManager shooter = enemy.GetComponent<vShooterManager>();
            if (shooter != null)
                shooter.AllAmmoInfinity = true;
        }

        private static void SetField(UnityEngine.Object target, string fieldName, object value)
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

    [Serializable]
    public class EnemySpawnSettings
    {
        [Tooltip("Optional. Leave empty when the prefab already has a baked EnemyDefinition.")]
        public EnemyDefinition definition;

        [Header("Weapon")]
        public bool overrideWeaponLoadout;
        public EnemySpawnWeaponMode weaponMode = EnemySpawnWeaponMode.UseDefinitionDefaults;
        public ItemData startMeleeWeapon;
        public ItemData startRangedWeapon;
        public ItemData[] randomWeaponPool = Array.Empty<ItemData>();
        public bool startWithMeleeWeapon = true;
        public bool preferRangedAtRange = true;
        public bool infiniteAmmo = true;

        [Header("Health")]
        public bool overrideHealth;
        public float maxHealth = 60f;
        public float respawnTime = -1f;

        [Header("Melee Combat")]
        public bool overrideCombat;
        public float attackDamage = 12f;
        public float attackRange = 1.8f;
        public float attackCooldown = 1.4f;
        public float attackWindup = 0.35f;
        public float meleeDuration = 0.85f;
        public float unarmedDuration = 0.55f;

        [Header("Ranged Combat")]
        public bool overrideRangedCombat;
        [Tooltip("Distance at which enemy stops chasing and starts shooting.")]
        public float rangedEngageRange = 12f;
        [Tooltip("Cooldown between ranged shots.")]
        public float rangedAttackCooldown = 1.2f;
        [Tooltip("Duration the shoot animation occupies before the next action is allowed.")]
        public float rangedDuration = 0.35f;
        [Tooltip("Seconds the aim pose is held after leaving a ranged engagement.")]
        public float aimHoldDuration = 1.5f;
        [Tooltip("0 = perfect aim, 1 = always misses.")]
        [Range(0f, 1f)]
        public float missRate = 0.25f;

        [Header("Movement")]
        public bool overrideMovement;
        public EnemyMovementMode movementMode = EnemyMovementMode.Wander;
        public float walkSpeed = 2.4f;
        public float runSpeed = 4.8f;
        public float wanderRadius = 8f;
        public bool chasePlayer = true;
    }
}
