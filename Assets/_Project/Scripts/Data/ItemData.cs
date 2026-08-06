using Project.Combat;
using Project.Progression;
using UnityEngine;
using UnityEngine.Serialization;

namespace Project.Data
{
    public enum ItemType
    {
        Consumable,
        Resource,
        MeleeWeapon,
        RangedWeapon,
        Ammo,
        Tool,
        Quest,
        Vehicle
    }

    public enum ComponentCategory
    {
        None,
        MetalScrap,
        ElectronicScrap,
        Unique
    }

    public enum ToolType
    {
        None,
        Scanner,
        Multitool,
        Binoculars
    }

    public enum WeaponGrip
    {
        OneHanded,
        TwoHanded
    }

    [CreateAssetMenu(menuName = "Project/Survival/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Identity")]
        public string itemName = "New Item";
        public Sprite icon;
        public GameObject worldPrefab;
        public int maxStack = 64;

        [Header("Stable ID")]
        [Tooltip("Persistent GUID assigned once per asset; used as the save key for identification registries. Auto-filled in OnValidate.")]
        [SerializeField] private string stableItemId;

        /// <summary>
        /// Persistent GUID for this item asset. Used as the save key in ResourceIdentificationRegistry
        /// so renames do not break existing save files. Assigned automatically in OnValidate.
        /// </summary>
        public string StableItemId =>
            !string.IsNullOrEmpty(stableItemId) ? stableItemId : name;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(stableItemId))
                return;

            string path = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrEmpty(path))
            {
                string guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
                if (!string.IsNullOrEmpty(guid))
                {
                    stableItemId = guid;
                    UnityEditor.EditorUtility.SetDirty(this);
                    return;
                }
            }

            stableItemId = System.Guid.NewGuid().ToString("N");
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        [Header("Use Type")]
        public ItemType itemType = ItemType.Consumable;

        [Header("Deployable / Vehicle")]
        [Tooltip("Prefab spawned into the world when this item is 'Deployed' from the inventory (e.g. a stored hovercraft). Only relevant for ItemType.Vehicle.")]
        public GameObject deployedPrefab;

        public bool IsVehicle => itemType == ItemType.Vehicle;

        [Header("Equipment")]
        public WeaponGrip weaponGrip = WeaponGrip.OneHanded;
        public GameObject heldPrefab;
        public string equipSocketName = "RightHand";
        [Tooltip("Local position on the Invector handler socket (meleeHandler / defaultHandler).")]
        public Vector3 heldLocalPosition = Vector3.zero;
        [Tooltip("Local euler rotation on the Invector handler socket.")]
        public Vector3 heldLocalEuler = Vector3.zero;
        public bool useHeldLocalRotation;
        public Quaternion heldLocalRotation = Quaternion.identity;
        public Vector3 heldLocalScale = Vector3.one;
        public Vector3 swingEulerAngles = new Vector3(-120f, 0f, 0f);

        [Header("Sheathed (Back)")]
        public string sheatheSocketName = "Spine";
        public Vector3 sheathedLocalPosition = new Vector3(0.02f, 0.18f, -0.22f);
        public Vector3 sheathedLocalEuler = new Vector3(75f, 90f, 90f);
        public bool useSheathedLocalRotation;
        public Quaternion sheathedLocalRotation = Quaternion.identity;
        public Vector3 sheathedLocalScale = Vector3.one;

        [Header("Invector")]
        [Tooltip("Invector vShooterWeapon or vMeleeWeapon prefab (No Inventory variants).")]
        public GameObject invectorWeaponPrefab;
        [Tooltip("Optional stable id for save/sync when prefab references change.")]
        public string invectorWeaponId;

        [Header("Melee Base Stats")]
        public float meleeDamage = 10f;
        [Tooltip("Extra random damage rolled on top of meleeDamage. Final hit = Random between meleeDamage and meleeDamage + this value.")]
        public float meleeDamageRandomRange = 3f;
        [Tooltip("Chance (0-1) for a critical hit on melee. Used by RollCriticalHit().")]
        [Range(0f, 1f)]
        public float criticalChance = 0.1f;
        [Tooltip("Damage multiplier applied to power / critical hits.")]
        public float criticalDamageMultiplier = 2f;
        public float meleeRange = 2.2f;
        public float meleeCooldown = 0.65f;
        [Tooltip("Animator playback multiplier for melee attacks. 0 uses grip + held scale.")]
        public float attackAnimationSpeed;
        [Tooltip("Stamina spent per melee swing. 0 keeps legacy behavior (no per-swing drain from this field).")]
        public float meleeStaminaCost;
        [Tooltip("Knockback impulse on melee hit. Reserved for hit-path force; authored now for base-stat parity.")]
        public float meleeKnockback;
        public int gatherPower = 1;
        public string attackTrigger = "Attack";

        [Header("Ranged Base Stats")]
        public float rangedDamage = 14f;
        public float rangedDamageRandomRange = 4f;
        public float rangedRange = 45f;
        [Tooltip("Projectile travel speed (m/s). Ammo overrides weapon when ammo.projectileSpeed > 0. Primary velocity authoring knob.")]
        public float projectileSpeed = 85f;
        [Tooltip("Base cone spread in degrees before accuracy, hip-fire, and close-range modifiers. Ammo overrides weapon when ammo spread > 0.")]
        public float projectileSpreadDegrees = 1.5f;
        [Tooltip("0-100 base accuracy. Higher reduces cone spread. Skill bonuses add on top. Later weapon upgrades will raise this from the weapon base.")]
        [Range(0f, 100f)]
        public float weaponAccuracy = 75f;
        [Tooltip("Within this distance (m) to the reticle aim point, spread scales toward closeRangeSpreadScale.")]
        public float closeRangeFullAccuracyDistance = 12f;
        [Tooltip("Spread multiplier at point-blank (0 = perfect, 1 = full spread). Lerps to 1 beyond closeRangeFullAccuracyDistance.")]
        [Range(0f, 1f)]
        public float closeRangeSpreadScale = 0.2f;
        [Tooltip("Vertical camera recoil kick magnitude (authoring units matching PioneerInvectorRecoilUtility). 0 falls back to grip defaults.")]
        public float recoilVertical;
        [Tooltip("Horizontal camera recoil kick magnitude (half-range of ±drift). 0 falls back to grip defaults.")]
        public float recoilHorizontal;
        [Tooltip("When fireRate exceeds this value, recoil scales down. 0 uses default 4.5.")]
        public float recoilFireRateScale = 4.5f;
        public float fireRate = 4f;
        public int magazineSize = 30;
        [Tooltip("Authoritative reload duration in seconds. Pushed onto Invector weapons when > 0.")]
        public float reloadTimeSeconds = 1.8f;
        public AmmoType defaultAmmoType = AmmoType.Gunpowder;
        public AmmoType[] compatibleAmmoTypes = { AmmoType.Gunpowder };
        [Tooltip("Preferred ammo ItemData for this weapon (player weapons default to Standard). Used for VFX/projectile path when a mag is loaded.")]
        public ItemData defaultAmmoItem;
        [Header("Ranged Starting Mag")]
        [Tooltip("If true, the first time this weapon is equipped/picked up it loads a random amount of Standard ammo into the magazine. If false, it starts Empty 0/0.")]
        public bool grantRandomStartingAmmo;
        [Tooltip("Inclusive min magazine rounds granted when grantRandomStartingAmmo is enabled.")]
        public int startingAmmoMin = 1;
        [Tooltip("Inclusive max magazine rounds granted when grantRandomStartingAmmo is enabled.")]
        public int startingAmmoMax = 12;
        public GameObject projectilePrefab;
        public string muzzleSocketName = "Muzzle";
        public float aimFovMultiplier = 0.78f;
        [Tooltip("Hip fire: max angle (degrees) the shot may deviate from barrel forward toward the crosshair. 0 uses default 15.")]
        public float hipFireMaxDeviationDegrees = 15f;
        [Tooltip("Hip fire spread multiplier applied on top of projectileSpreadDegrees.")]
        public float hipFireSpreadMultiplier = 1f;
        [Tooltip("Optional ADS grip override baked from Play mode while aiming.")]
        public bool useAimHeldGrip;
        public Vector3 aimHeldLocalPosition;
        public Vector3 aimHeldLocalEuler;
        public bool useAimHeldLocalRotation;
        public Quaternion aimHeldLocalRotation = Quaternion.identity;
        public Vector3 aimHeldLocalScale = Vector3.one;

        [Header("Ammo")]
        public AmmoType ammoType = AmmoType.Gunpowder;
        public int ammoPerPickup = 20;
        public float ammoPickupGrant = 20f;

        [Header("Projectile Behavior")]
        [Tooltip("Straight-line hitscan beam resolved instantly on fire (e.g. lasers) instead of a traveling physical projectile.")]
        public bool isHitscanBeam;
        [Tooltip("Downward acceleration applied to the physical projectile in flight (0 = perfectly straight, sci-fi energy weapons typically stay at 0).")]
        public float projectileGravityScale;
        [Tooltip("Splash/AoE damage radius on impact. 0 = single-target only.")]
        public float splashRadius;
        [Tooltip("Damage multiplier applied at the edge of the splash radius; damage falls off linearly from 1x at the impact point to this value at splashRadius.")]
        [Range(0f, 1f)]
        public float splashDamageFalloff = 0.25f;

        [Header("Projectile VFX")]
        [Tooltip("Spawned at the firing socket every shot (muzzle flash particle/light burst). Auto-destroyed shortly after.")]
        public GameObject muzzleFlashPrefab;
        [Tooltip("Optional prefab with a TrailRenderer/LineRenderer/particle system. On projectile ammo it trails the bullet; on hitscan laser ammo it is stretched muzzle→impact as a pulse tracer.")]
        public GameObject tracerPrefab;
        [Tooltip("Spawned at the impact point on hit (sparks, splatter, elemental burst). Auto-destroyed shortly after.")]
        public GameObject impactVfxPrefab;
        [Tooltip("Optional LineRenderer (or similar) prefab for hitscan laser beams between muzzle and hit. Preferred over tracerPrefab for continuous/pulse lasers.")]
        public GameObject beamVfxPrefab;

        [Header("Mining Tool")]
        [Tooltip("When true, Fire hold drives the mining laser instead of combat hitscan/projectile damage.")]
        public bool isMiningTool;
        [Tooltip("Number of mining passes required to finish a ResourceNode.")]
        public int miningPassesRequired = 2;
        [Tooltip("Minimum resource amount granted on the final mining pass.")]
        public int miningDropMin = 1;
        [Tooltip("Maximum resource amount granted on the final mining pass.")]
        public int miningDropMax = 5;
        [Tooltip("Aim angle from lock direction beyond which soft-lock breaks.")]
        public float miningLockBreakDegrees = 30f;
        [Tooltip("Seconds of continuous Fire hold required to complete one mining pass.")]
        public float miningPassDuration = 1.25f;
        [Tooltip("Optional rock-chunk VFX spawned at the node and pulled toward the tool muzzle.")]
        public GameObject miningChunkVfxPrefab;
        [Tooltip("Charge % drained per second while mining Fire is held. Full tank is always 100%.")]
        public float miningChargeDrainPerSecond = 4f;
        [Tooltip("Charge % restored when one Plasma Fuel is consumed on reload (R).")]
        public float miningChargePerPlasmaFuel = 50f;

        [Header("Projectile / Mining Beam Audio")]
        [Tooltip("Pulse gunshot at muzzle. Leave empty on mining tools (they use continuousLoopSound).")]
        public AudioClip fireSound;
        [Tooltip("Looping sound that travels with a physical projectile in flight. Not used by hitscan beam ammo.")]
        public AudioClip projectileTravelSound;
        [Tooltip("When true (or isMiningTool), hold-fire uses continuousLoopSound instead of pulse fireSound.")]
        public bool isContinuousLaser;
        [Tooltip("Looping audio while a continuous laser/mining beam is firing. Edit on DM_Mining_Tool for mining.")]
        public AudioClip continuousLoopSound;
        [Tooltip("Optional one-shot when continuous laser/mining fire starts.")]
        public AudioClip continuousStartSound;
        [Tooltip("Optional one-shot when continuous laser/mining fire stops.")]
        public AudioClip continuousStopSound;

        [Header("Mining Resource Scan Audio")]
        [Tooltip("Loop while holding F to scan a ResourceNode (mining multi-tool). Drag clip here on DM_Mining_Tool.")]
        public AudioClip miningScanLoopSound;
        [Tooltip("Optional one-shot when a resource scan succeeds.")]
        public AudioClip miningScanSuccessSound;
        [Tooltip("Optional one-shot when a scan is denied (already identified / insufficient skill).")]
        public AudioClip miningScanDeniedSound;

        [Header("Elemental Effect")]
        [Tooltip("Status effect applied on hit. None uses the ammo type's sensible default (Fire->Burning, Ice->Frozen, Electricity->Shocked, Plasma->Corroded).")]
        public StatusEffectType statusEffectOverride = StatusEffectType.None;
        [Tooltip("Damage dealt per tick while the status effect is active. 0 disables damage-over-time (effect can still be used for pure crowd control later).")]
        public float statusEffectDamagePerTick = 0f;
        [Tooltip("Seconds between damage-over-time ticks.")]
        public float statusEffectTickInterval = 1f;
        [Tooltip("Total seconds the status effect lasts once applied. Re-applying refreshes the duration rather than stacking.")]
        public float statusEffectDuration = 0f;
        [Tooltip("Optional looping VFX (fire licking the model, electric arcs, frost, etc.) attached to the target for as long as the status effect is active.")]
        public GameObject statusEffectVfxPrefab;

        /// <summary>Resolves the actual status effect this ammo applies, falling back to the ammo type's default.</summary>
        public StatusEffectType ResolveStatusEffect() =>
            statusEffectOverride != StatusEffectType.None ? statusEffectOverride : ammoType.DefaultStatusEffectFor();

        public bool HasStatusEffect => statusEffectDuration > 0f && ResolveStatusEffect() != StatusEffectType.None;

        public bool HasSplashDamage => splashRadius > 0.05f;

        [Header("Craft Components")]
        public ComponentCategory componentCategory = ComponentCategory.None;

        [Header("Tools")]
        public ToolType toolType = ToolType.None;
        public float toolRange = 8f;
        public float scanRange = 24f;
        [Tooltip("Field of view while this optics tool is active.")]
        public float opticsZoomFov = 38f;
        [Tooltip("Minimum scroll-adjusted FOV while zooming.")]
        public float opticsMinZoomFov = 18f;
        [Tooltip("Maximum scroll-adjusted FOV while zooming.")]
        public float opticsMaxZoomFov = 55f;

        public bool IsOpticsTool =>
            itemType == ItemType.Tool &&
            (toolType == ToolType.Scanner || toolType == ToolType.Binoculars);

        [Header("Survival Restore")]
        public float healthRestore = 0;
        public float energyRestore = 0;
        public float staminaRestore = 0;
        public float oxygenRestore = 0;

        [Header("Aether Credits")]
        [Tooltip("World pickup grants AC when collected.")]
        [FormerlySerializedAs("isPiInfused")]
        public bool isAcInfused = false;
        [FormerlySerializedAs("piValue")]
        public int acValue = 0;

        [Header("Progression")]
        [Tooltip("When true, collecting this item grants XP (shards, recipe scrolls, etc.). Normal pickups stay false.")]
        public bool grantsXp;
        public int xpAmount = 10;
        public XpSource xpSource = XpSource.SpecialItem;

        [Header("Level Gates")]
        [Tooltip("Minimum player level required to equip or use this item.")]
        public int requiredLevelToEquip = 1;
        [Tooltip("Minimum player level required to craft blueprints that output this item.")]
        public int requiredLevelToCraft = 1;

        [Header("Tooltip")]
        [TextArea(2, 5)]
        public string tooltipDescription;

        [Header("Inventory Expansion")]
        [Tooltip("When crafted or installed, unlocks the next inventory storage row (10 slots).")]
        public bool unlocksInventoryStorageRow;

        public bool IsInventoryStorageModule => unlocksInventoryStorageRow;

        public bool IsConsumable =>
            itemType == ItemType.Consumable &&
            (healthRestore > 0 || energyRestore > 0 || staminaRestore > 0 || oxygenRestore > 0);

        public bool IsEquippable =>
            itemType == ItemType.MeleeWeapon || itemType == ItemType.RangedWeapon || itemType == ItemType.Tool;

        public bool IsRangedWeapon => itemType == ItemType.RangedWeapon;

        public bool IsAmmo => itemType == ItemType.Ammo;

        /// <summary>
        /// Ammo accuracy wins when ammo is present and authored (&gt; 0); otherwise weapon base.
        /// Skill bonuses are applied separately in <see cref="Project.Combat.RangedFireSolver"/>.
        /// </summary>
        public float ResolveBaseAccuracy(ItemData ammoOrNull)
        {
            if (ammoOrNull != null && ammoOrNull.weaponAccuracy > 0f)
                return ammoOrNull.weaponAccuracy;
            return weaponAccuracy;
        }

        /// <summary>
        /// Fallback for mis-typed ammo assets (e.g. created as Consumable via Crafting Item Creator).
        /// </summary>
        public bool CountsAsAmmo =>
            IsAmmo
            || (projectilePrefab != null
                && ammoPerPickup > 0
                && itemType != ItemType.MeleeWeapon
                && itemType != ItemType.RangedWeapon
                && itemType != ItemType.Tool);

        public bool IsWeapon =>
            itemType == ItemType.MeleeWeapon || itemType == ItemType.RangedWeapon;

        public bool IsTwoHanded =>
            itemType == ItemType.MeleeWeapon && weaponGrip == WeaponGrip.TwoHanded;

        public bool IsOneHandedAxe =>
            itemType == ItemType.MeleeWeapon && !IsTwoHanded && InfersAsOneHandAxe();

        private bool InfersAsOneHandAxe()
        {
            if (!string.IsNullOrWhiteSpace(itemName)
                && itemName.IndexOf("axe", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            string heldName = heldPrefab != null ? heldPrefab.name : string.Empty;
            return !string.IsNullOrWhiteSpace(heldName)
                && heldName.IndexOf("axe", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Lower values slow attack clips. Bigger held scale and two-handed grips play slower by default.
        /// </summary>
        public float ResolveAttackAnimationSpeed()
        {
            if (attackAnimationSpeed > 0f)
                return attackAnimationSpeed;

            float gripSpeed = IsTwoHanded ? 0.72f : 0.9f;
            if (IsOneHandedAxe)
                gripSpeed *= 1.25f;

            float scaleFactor = heldLocalScale.magnitude / 1.7320508f;
            float sizeSlowdown = 1f / Mathf.Clamp(scaleFactor, 0.75f, 1.75f);
            float cooldownSlowdown = Mathf.Clamp(meleeCooldown / 0.65f, 0.85f, 1.35f);
            return Mathf.Clamp(gripSpeed * sizeSlowdown * cooldownSlowdown, 0.5f, 1.35f);
        }

        /// <summary>Rolls a critical using this item's <see cref="criticalChance"/> (0–1).</summary>
        public bool RollCriticalHit()
        {
            if (criticalChance <= 0f)
                return false;
            if (criticalChance >= 1f)
                return true;
            return Random.value <= criticalChance;
        }

        public float GetAverageMeleeDamage()
        {
            float bonus = PlayerSkillAllocator.GetMeleeDamageFlatBonus();
            float minDamage = Mathf.Max(1f, meleeDamage + bonus);
            float average;
            if (meleeDamageRandomRange <= 0f)
            {
                average = minDamage;
            }
            else
            {
                float maxDamage = Mathf.Max(minDamage, meleeDamage + meleeDamageRandomRange + bonus);
                average = (minDamage + maxDamage) * 0.5f;
            }

            return average * PlayerSkillAllocator.GetLevelWeaponDamageMultiplier();
        }

        public float GetAverageRangedDamage()
        {
            float bonus = PlayerSkillAllocator.GetRangedDamageFlatBonus();
            float minDamage = Mathf.Max(1f, rangedDamage + bonus);
            float average;
            if (rangedDamageRandomRange <= 0f)
            {
                average = minDamage;
            }
            else
            {
                float maxDamage = Mathf.Max(minDamage, rangedDamage + rangedDamageRandomRange + bonus);
                average = (minDamage + maxDamage) * 0.5f;
            }

            return average * PlayerSkillAllocator.GetLevelWeaponDamageMultiplier();
        }

        /// <summary>Effective accuracy including category Weapon Accuracy skill (clamped 0–100).</summary>
        public float GetEffectiveAccuracy(ItemData ammoOrNull = null)
        {
            float accuracy = ResolveBaseAccuracy(ammoOrNull);
            accuracy += PlayerSkillAllocator.GetWeaponAccuracyBonusPercent();
            return Mathf.Clamp(accuracy, 0f, 100f);
        }

        public float RollMeleeDamage(bool isCritical = false)
        {
            float bonus = PlayerSkillAllocator.GetMeleeDamageFlatBonus();
            float minDamage = Mathf.Max(1f, meleeDamage + bonus);
            float rolledDamage = meleeDamageRandomRange <= 0f
                ? minDamage
                : Random.Range(minDamage, Mathf.Max(minDamage, meleeDamage + meleeDamageRandomRange + bonus));

            if (isCritical && criticalDamageMultiplier > 0f)
                rolledDamage *= criticalDamageMultiplier;

            return rolledDamage * PlayerSkillAllocator.GetLevelWeaponDamageMultiplier();
        }

        public float RollRangedDamage(bool isCritical = false)
        {
            float bonus = PlayerSkillAllocator.GetRangedDamageFlatBonus();
            float minDamage = Mathf.Max(1f, rangedDamage + bonus);
            float rolledDamage = rangedDamageRandomRange <= 0f
                ? minDamage
                : Random.Range(minDamage, Mathf.Max(minDamage, rangedDamage + rangedDamageRandomRange + bonus));

            if (isCritical && criticalDamageMultiplier > 0f)
                rolledDamage *= criticalDamageMultiplier;

            return rolledDamage * PlayerSkillAllocator.GetLevelWeaponDamageMultiplier();
        }

        public bool AcceptsAmmoType(AmmoType type)
        {
            if (itemType != ItemType.RangedWeapon)
                return false;

            // Mining tools are powered by Plasma Fuel via reload — not discrete ammo magazines.
            if (isMiningTool)
                return false;

            if (compatibleAmmoTypes == null || compatibleAmmoTypes.Length == 0)
                return type == defaultAmmoType;

            for (int i = 0; i < compatibleAmmoTypes.Length; i++)
            {
                if (compatibleAmmoTypes[i] == type)
                    return true;
            }

            return false;
        }
    }
}