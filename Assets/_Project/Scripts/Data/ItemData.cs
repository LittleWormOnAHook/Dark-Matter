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

        public float RollMeleeDamage(bool isCritical = false)
        {
            float rolledDamage = meleeDamageRandomRange <= 0f
                ? Mathf.Max(1f, meleeDamage)
                : Random.Range(Mathf.Max(1f, meleeDamage), Mathf.Max(1f, meleeDamage) + meleeDamageRandomRange);

            if (isCritical && criticalDamageMultiplier > 0f)
                rolledDamage *= criticalDamageMultiplier;

            return rolledDamage;
        }
    }
}