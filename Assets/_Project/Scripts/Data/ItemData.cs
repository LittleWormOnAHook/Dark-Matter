using Project.Combat;
using Project.Progression;
using UnityEngine;

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

        [Header("Melee")]
        public float meleeDamage = 10f;
        [Tooltip("Extra random damage rolled on top of meleeDamage. Final hit = Random between meleeDamage and meleeDamage + this value.")]
        public float meleeDamageRandomRange = 3f;
        [Tooltip("Damage multiplier applied to power / critical hits.")]
        public float criticalDamageMultiplier = 2f;
        public float meleeRange = 2.2f;
        public float meleeCooldown = 0.65f;
        [Tooltip("Animator playback multiplier for melee attacks. 0 uses grip + held scale.")]
        public float attackAnimationSpeed;
        public int gatherPower = 1;
        public string attackTrigger = "Attack";

        [Header("Ranged")]
        public float rangedDamage = 14f;
        public float rangedDamageRandomRange = 4f;
        public float rangedRange = 45f;
        public float projectileSpeed = 85f;
        public float projectileSpreadDegrees = 1.5f;
        public float fireRate = 4f;
        public int magazineSize = 30;
        public AmmoType defaultAmmoType = AmmoType.Gunpowder;
        public AmmoType[] compatibleAmmoTypes = { AmmoType.Gunpowder };
        [Tooltip("Ammo ItemData this weapon starts loaded with before the player ever explicitly equips or picks up ammo. Keeps 'default ammo' fire going through the exact same ammoItem-driven projectile/VFX/audio path as any other ammo, instead of falling back to this weapon's own (easy to forget) VFX fields.")]
        public ItemData defaultAmmoItem;
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
        [Tooltip("Optional prefab with a TrailRenderer/LineRenderer/particle system attached to the flying projectile for its travel trail. Leave empty for no trail.")]
        public GameObject tracerPrefab;
        [Tooltip("Spawned at the impact point on hit (sparks, splatter, elemental burst). Auto-destroyed shortly after.")]
        public GameObject impactVfxPrefab;
        [Tooltip("Optional prefab used to render an instant hitscan beam between muzzle and hit point when isHitscanBeam is set. Needs a LineRenderer.")]
        public GameObject beamVfxPrefab;

        [Header("Projectile Audio")]
        [Tooltip("Played once at the muzzle the instant this ammo is fired.")]
        public AudioClip fireSound;
        [Tooltip("Looping sound that travels with the physical projectile in flight and stops the instant it hits (or expires). Not used by hitscan beam ammo.")]
        public AudioClip projectileTravelSound;

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
        public bool isPiInfused = false;
        public int piValue = 0;

        [Header("Progression")]
        [Tooltip("When true, collecting this item grants XP (shards, recipe scrolls, etc.). Normal pickups stay false.")]
        public bool grantsXp;
        public int xpAmount = 10;
        public XpSource xpSource = XpSource.SpecialItem;

        [Header("Level Gates")]
        [Tooltip("Minimum player level required to equip or use this item.")]
        public int requiredLevelToEquip = 1;
        [Tooltip("Minimum player level required to craft recipes that output this item.")]
        public int requiredLevelToCraft = 1;

        [Header("Tooltip")]
        [TextArea(2, 5)]
        public string tooltipDescription;

        public bool IsConsumable =>
            itemType == ItemType.Consumable &&
            (healthRestore > 0 || energyRestore > 0 || staminaRestore > 0 || oxygenRestore > 0);

        public bool IsEquippable =>
            itemType == ItemType.MeleeWeapon || itemType == ItemType.RangedWeapon || itemType == ItemType.Tool;

        public bool IsRangedWeapon => itemType == ItemType.RangedWeapon;

        public bool IsAmmo => itemType == ItemType.Ammo;

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
            float bonus = PlayerSkillAllocator.GetMeleeDamageFlatBonus();
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
            float bonus = PlayerSkillAllocator.GetMeleeDamageFlatBonus();
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