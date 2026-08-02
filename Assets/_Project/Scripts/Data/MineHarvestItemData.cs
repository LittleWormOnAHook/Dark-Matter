using Project.Combat;
using UnityEngine;

namespace Project.Data
{
    public enum MineHarvestGatherKind
    {
        Mining = 0,
        Harvest = 1
    }

    /// <summary>
    /// Lean inventory ItemData for laser-mined ores and Hold-E plant yields.
    /// Inspector is pruned via <c>MineHarvestItemDataEditor</c> — only gather-relevant fields are shown.
    /// Still subclasses <see cref="ItemData"/> so inventory / ResourceNode / loot paths keep working.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Project/Survival/Mine-Harvest Resource Item",
        fileName = "Resource_")]
    public class MineHarvestItemData : ItemData
    {
        [Header("Gather Kind")]
        [Tooltip("Mining = laser ore. Harvest = Hold-E plant yield.")]
        public MineHarvestGatherKind gatherKind = MineHarvestGatherKind.Mining;

        [Header("Loot Attract / Harvest Audio")]
        [Tooltip("Played at the node when a yield wave / harvest completes and loot starts flying.")]
        public AudioClip lootYieldClip;
        [Range(0f, 1f)]
        public float lootYieldVolume = 0.9f;
        [Tooltip("Played when loot reaches the player and inventory is granted. Empty = GameAudioManager item pickup.")]
        public AudioClip lootGrantClip;
        [Range(0f, 1f)]
        public float lootGrantVolume = 0.95f;

        [Header("Harvest Complete VFX")]
        [Tooltip("One-shot VFX spawned at the player when loot arrives and inventory is granted.")]
        public GameObject lootCompleteVfxPrefab;

        private void OnValidate()
        {
            itemType = ItemType.Resource;
            PruneNonGatherFields();
        }

        private void OnEnable()
        {
            itemType = ItemType.Resource;
        }

        /// <summary>
        /// Clears combat / tool / consumable authoring so these assets stay gather-only at runtime.
        /// Does not clear loot attract / harvest audio.
        /// </summary>
        public void PruneNonGatherFields()
        {
            itemType = ItemType.Resource;

            deployedPrefab = null;
            heldPrefab = null;
            invectorWeaponPrefab = null;
            invectorWeaponId = string.Empty;
            projectilePrefab = null;
            defaultAmmoItem = null;
            muzzleFlashPrefab = null;
            tracerPrefab = null;
            impactVfxPrefab = null;
            beamVfxPrefab = null;
            miningChunkVfxPrefab = null;
            statusEffectVfxPrefab = null;
            fireSound = null;
            projectileTravelSound = null;
            continuousLoopSound = null;
            continuousStartSound = null;
            continuousStopSound = null;

            isMiningTool = false;
            isContinuousLaser = false;
            isHitscanBeam = false;
            isAcInfused = false;
            unlocksInventoryStorageRow = false;
            grantRandomStartingAmmo = false;
            useHeldLocalRotation = false;
            useSheathedLocalRotation = false;
            useAimHeldGrip = false;
            useAimHeldLocalRotation = false;

            componentCategory = ComponentCategory.None;
            toolType = ToolType.None;
            statusEffectOverride = StatusEffectType.None;
            ammoType = AmmoType.Gunpowder;
            defaultAmmoType = AmmoType.Gunpowder;
            compatibleAmmoTypes = System.Array.Empty<AmmoType>();

            meleeDamage = 0f;
            meleeDamageRandomRange = 0f;
            criticalChance = 0f;
            criticalDamageMultiplier = 0f;
            meleeRange = 0f;
            meleeCooldown = 0f;
            attackAnimationSpeed = 0f;
            meleeStaminaCost = 0f;
            meleeKnockback = 0f;
            gatherPower = 0;
            rangedDamage = 0f;
            rangedDamageRandomRange = 0f;
            rangedRange = 0f;
            projectileSpeed = 0f;
            projectileSpreadDegrees = 0f;
            weaponAccuracy = 0f;
            closeRangeFullAccuracyDistance = 0f;
            closeRangeSpreadScale = 0f;
            recoilVertical = 0f;
            recoilHorizontal = 0f;
            recoilFireRateScale = 0f;
            fireRate = 0f;
            magazineSize = 0;
            reloadTimeSeconds = 0f;
            startingAmmoMin = 0;
            startingAmmoMax = 0;
            aimFovMultiplier = 0f;
            hipFireMaxDeviationDegrees = 0f;
            hipFireSpreadMultiplier = 0f;
            ammoPerPickup = 0;
            ammoPickupGrant = 0f;
            projectileGravityScale = 0f;
            splashRadius = 0f;
            splashDamageFalloff = 0f;
            miningPassesRequired = 0;
            miningDropMin = 0;
            miningDropMax = 0;
            miningLockBreakDegrees = 0f;
            miningPassDuration = 0f;
            miningChargeDrainPerSecond = 0f;
            miningChargePerPlasmaFuel = 0f;
            statusEffectDamagePerTick = 0f;
            statusEffectTickInterval = 0f;
            statusEffectDuration = 0f;
            toolRange = 0f;
            scanRange = 0f;
            opticsZoomFov = 0f;
            opticsMinZoomFov = 0f;
            opticsMaxZoomFov = 0f;
            healthRestore = 0f;
            energyRestore = 0f;
            staminaRestore = 0f;
            oxygenRestore = 0f;
            acValue = 0;
            requiredLevelToEquip = 1;
            requiredLevelToCraft = 1;

            if (maxStack < 1)
                maxStack = 1;

            lootYieldVolume = Mathf.Clamp01(lootYieldVolume);
            lootGrantVolume = Mathf.Clamp01(lootGrantVolume);
        }
    }
}
