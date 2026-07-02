using Project.Data;
using Project.Progression;
using UnityEngine;

namespace Project.Data
{
    public enum ItemType
    {
        Consumable,
        Resource,
        MeleeWeapon,
        Tool,
        Quest
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

    /// <summary>
    /// GKC animator Weapon ID blend-tree selector. Infer resolves from grip/name.
    /// </summary>
    public enum GkcWeaponKind
    {
        Infer = -1,
        Unarmed = 0,
        OneHandSword = 1,
        TwoHand = 2,
        OneHandAxe = 3
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

        [Header("Equipment")]
        public WeaponGrip weaponGrip = WeaponGrip.OneHanded;
        [Tooltip("GKC Weapon ID for locomotion/attacks. Infer: two-hand=2, axe name=3, else 1H sword.")]
        public GkcWeaponKind gkcWeaponKind = GkcWeaponKind.Infer;
        [Tooltip("Optional GKC Right Arm ID while drawn. -1 keeps bridge defaults.")]
        public int gkcRightArmId = -1;
        [Tooltip("Optional GKC Left Arm ID while drawn. -1 keeps bridge defaults.")]
        public int gkcLeftArmId = -1;
        public GameObject heldPrefab;
        public string equipSocketName = "RightHand";
        public Vector3 heldLocalPosition = Vector3.zero;
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
            itemType == ItemType.MeleeWeapon || itemType == ItemType.Tool;

        public bool IsTwoHanded =>
            itemType == ItemType.MeleeWeapon && weaponGrip == WeaponGrip.TwoHanded;

        public bool IsOneHandedAxe =>
            itemType == ItemType.MeleeWeapon && !IsTwoHanded && ResolveGkcWeaponKind() == GkcWeaponKind.OneHandAxe;

        public GkcWeaponKind ResolveGkcWeaponKind()
        {
            if (itemType != ItemType.MeleeWeapon)
                return GkcWeaponKind.Unarmed;

            if (gkcWeaponKind != GkcWeaponKind.Infer)
                return gkcWeaponKind;

            if (IsTwoHanded)
                return GkcWeaponKind.TwoHand;

            if (InfersAsOneHandAxe())
                return GkcWeaponKind.OneHandAxe;

            return GkcWeaponKind.OneHandSword;
        }

        public float ResolveGkcWeaponId() =>
            (float)ResolveGkcWeaponKind();

        public int ResolveGkcRightArmId()
        {
            if (gkcRightArmId >= 0)
                return gkcRightArmId;

            if (itemType != ItemType.MeleeWeapon)
                return 0;

            return 0;
        }

        public int ResolveGkcLeftArmId()
        {
            if (gkcLeftArmId >= 0)
                return gkcLeftArmId;

            return 0;
        }

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
            if (ResolveGkcWeaponKind() == GkcWeaponKind.OneHandAxe)
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
            if (meleeDamageRandomRange <= 0f)
                return minDamage;

            float maxDamage = Mathf.Max(minDamage, meleeDamage + meleeDamageRandomRange + bonus);
            return (minDamage + maxDamage) * 0.5f;
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

            return rolledDamage;
        }
    }
}