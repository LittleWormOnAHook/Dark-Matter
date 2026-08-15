using Project.Data;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Category-gated Inspector for <see cref="ItemData"/>. Hides unused sections; does not delete fields.
    /// <see cref="MineHarvestItemData"/> keeps <see cref="MineHarvestItemDataEditor"/>.
    /// </summary>
    [CustomEditor(typeof(ItemData))]
    public class ItemDataEditor : Editor
    {
        private SerializedProperty itemName;
        private SerializedProperty icon;
        private SerializedProperty worldPrefab;
        private SerializedProperty maxStack;
        private SerializedProperty stableItemId;
        private SerializedProperty itemType;
        private SerializedProperty deployedPrefab;
        private SerializedProperty weaponGrip;
        private SerializedProperty heldPrefab;
        private SerializedProperty equipSocketName;
        private SerializedProperty heldLocalPosition;
        private SerializedProperty heldLocalEuler;
        private SerializedProperty useHeldLocalRotation;
        private SerializedProperty heldLocalRotation;
        private SerializedProperty heldLocalScale;
        private SerializedProperty swingEulerAngles;
        private SerializedProperty sheatheSocketName;
        private SerializedProperty sheathedLocalPosition;
        private SerializedProperty sheathedLocalEuler;
        private SerializedProperty useSheathedLocalRotation;
        private SerializedProperty sheathedLocalRotation;
        private SerializedProperty sheathedLocalScale;
        private SerializedProperty invectorWeaponPrefab;
        private SerializedProperty invectorWeaponId;
        private SerializedProperty meleeDamage;
        private SerializedProperty meleeDamageRandomRange;
        private SerializedProperty criticalChance;
        private SerializedProperty criticalDamageMultiplier;
        private SerializedProperty meleeRange;
        private SerializedProperty meleeCooldown;
        private SerializedProperty attackAnimationSpeed;
        private SerializedProperty meleeStaminaCost;
        private SerializedProperty meleeKnockback;
        private SerializedProperty gatherPower;
        private SerializedProperty attackTrigger;
        private SerializedProperty rangedDamage;
        private SerializedProperty rangedDamageRandomRange;
        private SerializedProperty rangedRange;
        private SerializedProperty projectileSpeed;
        private SerializedProperty projectileSpreadDegrees;
        private SerializedProperty weaponAccuracy;
        private SerializedProperty closeRangeFullAccuracyDistance;
        private SerializedProperty closeRangeSpreadScale;
        private SerializedProperty recoilVertical;
        private SerializedProperty recoilHorizontal;
        private SerializedProperty recoilFireRateScale;
        private SerializedProperty fireRate;
        private SerializedProperty magazineSize;
        private SerializedProperty reloadTimeSeconds;
        private SerializedProperty defaultAmmoType;
        private SerializedProperty compatibleAmmoTypes;
        private SerializedProperty defaultAmmoItem;
        private SerializedProperty grantRandomStartingAmmo;
        private SerializedProperty startingAmmoMin;
        private SerializedProperty startingAmmoMax;
        private SerializedProperty projectilePrefab;
        private SerializedProperty muzzleSocketName;
        private SerializedProperty aimFovMultiplier;
        private SerializedProperty hipFireMaxDeviationDegrees;
        private SerializedProperty hipFireSpreadMultiplier;
        private SerializedProperty useAimHeldGrip;
        private SerializedProperty aimHeldLocalPosition;
        private SerializedProperty aimHeldLocalEuler;
        private SerializedProperty useAimHeldLocalRotation;
        private SerializedProperty aimHeldLocalRotation;
        private SerializedProperty aimHeldLocalScale;
        private SerializedProperty ammoType;
        private SerializedProperty ammoPerPickup;
        private SerializedProperty ammoPickupGrant;
        private SerializedProperty isHitscanBeam;
        private SerializedProperty projectileGravityScale;
        private SerializedProperty splashRadius;
        private SerializedProperty splashDamageFalloff;
        private SerializedProperty muzzleFlashPrefab;
        private SerializedProperty tracerPrefab;
        private SerializedProperty impactVfxPrefab;
        private SerializedProperty beamVfxPrefab;
        private SerializedProperty isMiningTool;
        private SerializedProperty miningPassesRequired;
        private SerializedProperty miningDropMin;
        private SerializedProperty miningDropMax;
        private SerializedProperty miningLockBreakDegrees;
        private SerializedProperty miningPassDuration;
        private SerializedProperty miningChunkVfxPrefab;
        private SerializedProperty miningChargeDrainPerSecond;
        private SerializedProperty miningChargePerPlasmaFuel;
        private SerializedProperty fireSound;
        private SerializedProperty projectileTravelSound;
        private SerializedProperty isContinuousLaser;
        private SerializedProperty continuousLoopSound;
        private SerializedProperty continuousStartSound;
        private SerializedProperty continuousStopSound;
        private SerializedProperty miningScanLoopSound;
        private SerializedProperty miningScanSuccessSound;
        private SerializedProperty miningScanDeniedSound;
        private SerializedProperty statusEffectOverride;
        private SerializedProperty statusEffectDamagePerTick;
        private SerializedProperty statusEffectTickInterval;
        private SerializedProperty statusEffectDuration;
        private SerializedProperty statusEffectVfxPrefab;
        private SerializedProperty componentCategory;
        private SerializedProperty toolType;
        private SerializedProperty toolRange;
        private SerializedProperty scanRange;
        private SerializedProperty opticsZoomFov;
        private SerializedProperty opticsMinZoomFov;
        private SerializedProperty opticsMaxZoomFov;
        private SerializedProperty healthRestore;
        private SerializedProperty energyRestore;
        private SerializedProperty staminaRestore;
        private SerializedProperty oxygenRestore;
        private SerializedProperty isAcInfused;
        private SerializedProperty acValue;
        private SerializedProperty grantsXp;
        private SerializedProperty xpAmount;
        private SerializedProperty xpSource;
        private SerializedProperty grantXpEveryPickupOrUse;
        private SerializedProperty requiredLevelToEquip;
        private SerializedProperty requiredLevelToCraft;
        private SerializedProperty requiredLevelToUse;
        private SerializedProperty requiredLevelToPickup;
        private SerializedProperty tooltipDescription;
        private SerializedProperty unlocksInventoryStorageRow;

        private void OnEnable()
        {
            itemName = serializedObject.FindProperty("itemName");
            icon = serializedObject.FindProperty("icon");
            worldPrefab = serializedObject.FindProperty("worldPrefab");
            maxStack = serializedObject.FindProperty("maxStack");
            stableItemId = serializedObject.FindProperty("stableItemId");
            itemType = serializedObject.FindProperty("itemType");
            deployedPrefab = serializedObject.FindProperty("deployedPrefab");
            weaponGrip = serializedObject.FindProperty("weaponGrip");
            heldPrefab = serializedObject.FindProperty("heldPrefab");
            equipSocketName = serializedObject.FindProperty("equipSocketName");
            heldLocalPosition = serializedObject.FindProperty("heldLocalPosition");
            heldLocalEuler = serializedObject.FindProperty("heldLocalEuler");
            useHeldLocalRotation = serializedObject.FindProperty("useHeldLocalRotation");
            heldLocalRotation = serializedObject.FindProperty("heldLocalRotation");
            heldLocalScale = serializedObject.FindProperty("heldLocalScale");
            swingEulerAngles = serializedObject.FindProperty("swingEulerAngles");
            sheatheSocketName = serializedObject.FindProperty("sheatheSocketName");
            sheathedLocalPosition = serializedObject.FindProperty("sheathedLocalPosition");
            sheathedLocalEuler = serializedObject.FindProperty("sheathedLocalEuler");
            useSheathedLocalRotation = serializedObject.FindProperty("useSheathedLocalRotation");
            sheathedLocalRotation = serializedObject.FindProperty("sheathedLocalRotation");
            sheathedLocalScale = serializedObject.FindProperty("sheathedLocalScale");
            invectorWeaponPrefab = serializedObject.FindProperty("invectorWeaponPrefab");
            invectorWeaponId = serializedObject.FindProperty("invectorWeaponId");
            meleeDamage = serializedObject.FindProperty("meleeDamage");
            meleeDamageRandomRange = serializedObject.FindProperty("meleeDamageRandomRange");
            criticalChance = serializedObject.FindProperty("criticalChance");
            criticalDamageMultiplier = serializedObject.FindProperty("criticalDamageMultiplier");
            meleeRange = serializedObject.FindProperty("meleeRange");
            meleeCooldown = serializedObject.FindProperty("meleeCooldown");
            attackAnimationSpeed = serializedObject.FindProperty("attackAnimationSpeed");
            meleeStaminaCost = serializedObject.FindProperty("meleeStaminaCost");
            meleeKnockback = serializedObject.FindProperty("meleeKnockback");
            gatherPower = serializedObject.FindProperty("gatherPower");
            attackTrigger = serializedObject.FindProperty("attackTrigger");
            rangedDamage = serializedObject.FindProperty("rangedDamage");
            rangedDamageRandomRange = serializedObject.FindProperty("rangedDamageRandomRange");
            rangedRange = serializedObject.FindProperty("rangedRange");
            projectileSpeed = serializedObject.FindProperty("projectileSpeed");
            projectileSpreadDegrees = serializedObject.FindProperty("projectileSpreadDegrees");
            weaponAccuracy = serializedObject.FindProperty("weaponAccuracy");
            closeRangeFullAccuracyDistance = serializedObject.FindProperty("closeRangeFullAccuracyDistance");
            closeRangeSpreadScale = serializedObject.FindProperty("closeRangeSpreadScale");
            recoilVertical = serializedObject.FindProperty("recoilVertical");
            recoilHorizontal = serializedObject.FindProperty("recoilHorizontal");
            recoilFireRateScale = serializedObject.FindProperty("recoilFireRateScale");
            fireRate = serializedObject.FindProperty("fireRate");
            magazineSize = serializedObject.FindProperty("magazineSize");
            reloadTimeSeconds = serializedObject.FindProperty("reloadTimeSeconds");
            defaultAmmoType = serializedObject.FindProperty("defaultAmmoType");
            compatibleAmmoTypes = serializedObject.FindProperty("compatibleAmmoTypes");
            defaultAmmoItem = serializedObject.FindProperty("defaultAmmoItem");
            grantRandomStartingAmmo = serializedObject.FindProperty("grantRandomStartingAmmo");
            startingAmmoMin = serializedObject.FindProperty("startingAmmoMin");
            startingAmmoMax = serializedObject.FindProperty("startingAmmoMax");
            projectilePrefab = serializedObject.FindProperty("projectilePrefab");
            muzzleSocketName = serializedObject.FindProperty("muzzleSocketName");
            aimFovMultiplier = serializedObject.FindProperty("aimFovMultiplier");
            hipFireMaxDeviationDegrees = serializedObject.FindProperty("hipFireMaxDeviationDegrees");
            hipFireSpreadMultiplier = serializedObject.FindProperty("hipFireSpreadMultiplier");
            useAimHeldGrip = serializedObject.FindProperty("useAimHeldGrip");
            aimHeldLocalPosition = serializedObject.FindProperty("aimHeldLocalPosition");
            aimHeldLocalEuler = serializedObject.FindProperty("aimHeldLocalEuler");
            useAimHeldLocalRotation = serializedObject.FindProperty("useAimHeldLocalRotation");
            aimHeldLocalRotation = serializedObject.FindProperty("aimHeldLocalRotation");
            aimHeldLocalScale = serializedObject.FindProperty("aimHeldLocalScale");
            ammoType = serializedObject.FindProperty("ammoType");
            ammoPerPickup = serializedObject.FindProperty("ammoPerPickup");
            ammoPickupGrant = serializedObject.FindProperty("ammoPickupGrant");
            isHitscanBeam = serializedObject.FindProperty("isHitscanBeam");
            projectileGravityScale = serializedObject.FindProperty("projectileGravityScale");
            splashRadius = serializedObject.FindProperty("splashRadius");
            splashDamageFalloff = serializedObject.FindProperty("splashDamageFalloff");
            muzzleFlashPrefab = serializedObject.FindProperty("muzzleFlashPrefab");
            tracerPrefab = serializedObject.FindProperty("tracerPrefab");
            impactVfxPrefab = serializedObject.FindProperty("impactVfxPrefab");
            beamVfxPrefab = serializedObject.FindProperty("beamVfxPrefab");
            isMiningTool = serializedObject.FindProperty("isMiningTool");
            miningPassesRequired = serializedObject.FindProperty("miningPassesRequired");
            miningDropMin = serializedObject.FindProperty("miningDropMin");
            miningDropMax = serializedObject.FindProperty("miningDropMax");
            miningLockBreakDegrees = serializedObject.FindProperty("miningLockBreakDegrees");
            miningPassDuration = serializedObject.FindProperty("miningPassDuration");
            miningChunkVfxPrefab = serializedObject.FindProperty("miningChunkVfxPrefab");
            miningChargeDrainPerSecond = serializedObject.FindProperty("miningChargeDrainPerSecond");
            miningChargePerPlasmaFuel = serializedObject.FindProperty("miningChargePerPlasmaFuel");
            fireSound = serializedObject.FindProperty("fireSound");
            projectileTravelSound = serializedObject.FindProperty("projectileTravelSound");
            isContinuousLaser = serializedObject.FindProperty("isContinuousLaser");
            continuousLoopSound = serializedObject.FindProperty("continuousLoopSound");
            continuousStartSound = serializedObject.FindProperty("continuousStartSound");
            continuousStopSound = serializedObject.FindProperty("continuousStopSound");
            miningScanLoopSound = serializedObject.FindProperty("miningScanLoopSound");
            miningScanSuccessSound = serializedObject.FindProperty("miningScanSuccessSound");
            miningScanDeniedSound = serializedObject.FindProperty("miningScanDeniedSound");
            statusEffectOverride = serializedObject.FindProperty("statusEffectOverride");
            statusEffectDamagePerTick = serializedObject.FindProperty("statusEffectDamagePerTick");
            statusEffectTickInterval = serializedObject.FindProperty("statusEffectTickInterval");
            statusEffectDuration = serializedObject.FindProperty("statusEffectDuration");
            statusEffectVfxPrefab = serializedObject.FindProperty("statusEffectVfxPrefab");
            componentCategory = serializedObject.FindProperty("componentCategory");
            toolType = serializedObject.FindProperty("toolType");
            toolRange = serializedObject.FindProperty("toolRange");
            scanRange = serializedObject.FindProperty("scanRange");
            opticsZoomFov = serializedObject.FindProperty("opticsZoomFov");
            opticsMinZoomFov = serializedObject.FindProperty("opticsMinZoomFov");
            opticsMaxZoomFov = serializedObject.FindProperty("opticsMaxZoomFov");
            healthRestore = serializedObject.FindProperty("healthRestore");
            energyRestore = serializedObject.FindProperty("energyRestore");
            staminaRestore = serializedObject.FindProperty("staminaRestore");
            oxygenRestore = serializedObject.FindProperty("oxygenRestore");
            isAcInfused = serializedObject.FindProperty("isAcInfused");
            acValue = serializedObject.FindProperty("acValue");
            grantsXp = serializedObject.FindProperty("grantsXp");
            xpAmount = serializedObject.FindProperty("xpAmount");
            xpSource = serializedObject.FindProperty("xpSource");
            grantXpEveryPickupOrUse = serializedObject.FindProperty("grantXpEveryPickupOrUse");
            requiredLevelToEquip = serializedObject.FindProperty("requiredLevelToEquip");
            requiredLevelToCraft = serializedObject.FindProperty("requiredLevelToCraft");
            requiredLevelToUse = serializedObject.FindProperty("requiredLevelToUse");
            requiredLevelToPickup = serializedObject.FindProperty("requiredLevelToPickup");
            tooltipDescription = serializedObject.FindProperty("tooltipDescription");
            unlocksInventoryStorageRow = serializedObject.FindProperty("unlocksInventoryStorageRow");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ItemData item = (ItemData)target;
            ItemDataInspectorCategory category = ItemDataInspectorCategoryResolver.Resolve(item);

            EditorGUILayout.HelpBox(BuildHelp(category), MessageType.Info);

            DrawIdentity();
            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(itemType);
            EditorGUILayout.LabelField("Inspector Category", category.ToString());

            EditorGUI.BeginChangeCheck();
            DrawCategorySections(category);
            bool categoryFieldsChanged = EditorGUI.EndChangeCheck();

            EditorGUILayout.Space(8f);
            DrawTooltip();
            DrawProgression();

            if (serializedObject.ApplyModifiedProperties() || categoryFieldsChanged)
            {
                ItemData refreshed = (ItemData)target;
                ItemDataInspectorCategory newCategory = ItemDataInspectorCategoryResolver.Resolve(refreshed);
                // Do not auto-prune on every keystroke; only when type/flags change via button or explicit prune.
                EditorUtility.SetDirty(refreshed);
                if (newCategory != category)
                    Repaint();
            }

            EditorGUILayout.Space(10f);
            if (GUILayout.Button("Prune Unused Fields For Category"))
            {
                foreach (Object obj in targets)
                {
                    if (obj is ItemData pruneTarget && !(pruneTarget is MineHarvestItemData))
                    {
                        Undo.RecordObject(pruneTarget, "Prune ItemData");
                        ItemDataPruneUtility.Prune(pruneTarget);
                        EditorUtility.SetDirty(pruneTarget);
                    }
                }

                serializedObject.Update();
            }
        }

        private static string BuildHelp(ItemDataInspectorCategory category)
        {
            switch (category)
            {
                case ItemDataInspectorCategory.ThrowableConsumable:
                    return "Throwable consumable (grenade) — identity only. Throw/cook/explosion live on combat prefabs + DMI grenade scripts. Keep itemType = Consumable.";
                case ItemDataInspectorCategory.HealConsumable:
                    return "Heal / survival consumable — identity + restore values.";
                case ItemDataInspectorCategory.GenericConsumable:
                    return "Consumable with no restores — identity only (not treated as grenade unless under Throwables / named Grenade).";
                case ItemDataInspectorCategory.Ammo:
                    return "Ammo — pickup grant, projectile overrides, VFX, and elemental.";
                case ItemDataInspectorCategory.RangedWeapon:
                    return "Ranged weapon — equipment, Invector, fire stats, ammo compatibility.";
                case ItemDataInspectorCategory.MiningTool:
                    return "Mining tool (ranged + isMiningTool) — equipment, mining passes/charge, beam audio.";
                case ItemDataInspectorCategory.MeleeWeapon:
                    return "Melee weapon — equipment, Invector, melee base stats.";
                case ItemDataInspectorCategory.OpticsTool:
                    return "Optics tool — equipment grip + FOV / scan ranges.";
                case ItemDataInspectorCategory.GenericTool:
                    return "Tool — equipment grip + tool ranges.";
                case ItemDataInspectorCategory.Module:
                    return "Inventory / building module — identity + storage unlock flag.";
                case ItemDataInspectorCategory.Operations:
                    return "Operations resource (e.g. Plasma Fuel) — identity only.";
                case ItemDataInspectorCategory.Component:
                    return "Craft component / scrap — identity + component category.";
                case ItemDataInspectorCategory.Resource:
                    return "Generic resource ItemData. Mine/harvest ores use MineHarvestItemData editor.";
                case ItemDataInspectorCategory.Vehicle:
                    return "Vehicle item — identity + deployed prefab.";
                case ItemDataInspectorCategory.Quest:
                    return "Quest item — identity only.";
                default:
                    return "ItemData";
            }
        }

        private void DrawIdentity()
        {
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(itemName);
            EditorGUILayout.PropertyField(icon);
            EditorGUILayout.PropertyField(worldPrefab);
            EditorGUILayout.PropertyField(maxStack);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(stableItemId, new GUIContent("Stable Item Id"));
        }

        private void DrawTooltip()
        {
            EditorGUILayout.LabelField("Tooltip", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(tooltipDescription, GUIContent.none);
        }

        private void DrawProgression()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Progression", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(isAcInfused);
            using (new EditorGUI.DisabledScope(!isAcInfused.boolValue))
                EditorGUILayout.PropertyField(acValue);
            EditorGUILayout.PropertyField(grantsXp);
            using (new EditorGUI.DisabledScope(!grantsXp.boolValue))
            {
                EditorGUILayout.PropertyField(xpAmount);
                EditorGUILayout.PropertyField(xpSource);
                EditorGUILayout.PropertyField(
                    grantXpEveryPickupOrUse,
                    new GUIContent(
                        "XP Every Pickup / Use",
                        "On: grant XP each pickup, use/consume, or gather. Off: one-time XP for this item asset."));
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Level Gates", EditorStyles.boldLabel);
            DrawSuggestedXpFromGates();
        }

        /// <summary>
        /// Level gates auto-write suggested xpAmount when edited.
        /// Apply Suggested XP remains as a manual override / resync.
        /// </summary>
        private void DrawSuggestedXpFromGates()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(
                requiredLevelToEquip,
                new GUIContent("Required Level To Equip", "Equip/select weapon or tool. 0 or 1 = no gate."));
            EditorGUILayout.PropertyField(
                requiredLevelToCraft,
                new GUIContent("Required Level To Craft", "Craft blueprints that output this item. 0 or 1 = no gate."));
            EditorGUILayout.PropertyField(
                requiredLevelToUse,
                new GUIContent("Required Level To Use", "Use/consume/throw from inventory. 0 or 1 = no gate."));
            EditorGUILayout.PropertyField(
                requiredLevelToPickup,
                new GUIContent("Required Level To Pickup", "World pickup and loot claim. 0 or 1 = no gate."));
            bool gatesChanged = EditorGUI.EndChangeCheck();

            if (gatesChanged)
            {
                ItemDataXpAuthoringHints.ApplySuggestedXpFromGates(
                    xpAmount,
                    requiredLevelToEquip.intValue,
                    requiredLevelToCraft.intValue,
                    requiredLevelToUse.intValue,
                    requiredLevelToPickup.intValue,
                    grantXpEveryPickupOrUse.boolValue);
            }

            int gate = ItemDataXpAuthoringHints.GetAuthoringGateLevel(
                requiredLevelToEquip.intValue,
                requiredLevelToCraft.intValue,
                requiredLevelToUse.intValue,
                requiredLevelToPickup.intValue);
            bool continuous = grantXpEveryPickupOrUse.boolValue;
            int suggested = ItemDataXpAuthoringHints.GetSuggestedXpAmount(gate, continuous);
            int current = xpAmount.intValue;

            EditorGUILayout.Space(2f);
            EditorGUILayout.HelpBox(
                ItemDataXpAuthoringHints.FormatPreviewLabel(gate, continuous, suggested, current)
                + "\nGate edits auto-write xpAmount. Use Apply to resync after manual XP edits.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(suggested == current))
                {
                    if (GUILayout.Button("Apply Suggested XP", GUILayout.Width(150f)))
                    {
                        xpAmount.intValue = suggested;
                        if (!grantsXp.boolValue)
                            grantsXp.boolValue = true;
                    }
                }
            }
        }

        private void DrawCategorySections(ItemDataInspectorCategory category)
        {
            switch (category)
            {
                case ItemDataInspectorCategory.ThrowableConsumable:
                    break;

                case ItemDataInspectorCategory.HealConsumable:
                    DrawRestores();
                    break;

                case ItemDataInspectorCategory.GenericConsumable:
                    DrawRestores();
                    break;

                case ItemDataInspectorCategory.Ammo:
                    DrawAmmoSection();
                    DrawProjectileBehavior();
                    DrawProjectileVfx();
                    DrawProjectileAudio(includeMiningScan: false);
                    DrawElemental();
                    break;

                case ItemDataInspectorCategory.RangedWeapon:
                    DrawEquipment(includeSheathe: true, includeSwing: false);
                    DrawInvector();
                    DrawRangedStats();
                    DrawStartingMag();
                    DrawProjectileBehaviorLight();
                    DrawProjectileVfx();
                    DrawProjectileAudio(includeMiningScan: false);
                    EditorGUILayout.Space(6f);
                    EditorGUILayout.PropertyField(isMiningTool);
                    break;

                case ItemDataInspectorCategory.MiningTool:
                    DrawEquipment(includeSheathe: true, includeSwing: false);
                    DrawInvector();
                    DrawMining();
                    DrawProjectileAudio(includeMiningScan: true);
                    break;

                case ItemDataInspectorCategory.MeleeWeapon:
                    DrawEquipment(includeSheathe: true, includeSwing: true);
                    DrawInvector();
                    DrawMeleeStats();
                    break;

                case ItemDataInspectorCategory.OpticsTool:
                    DrawEquipment(includeSheathe: false, includeSwing: false);
                    DrawToolOptics();
                    break;

                case ItemDataInspectorCategory.GenericTool:
                    DrawEquipment(includeSheathe: false, includeSwing: false);
                    DrawToolGeneric();
                    break;

                case ItemDataInspectorCategory.Module:
                    EditorGUILayout.Space(6f);
                    EditorGUILayout.LabelField("Inventory Expansion", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(unlocksInventoryStorageRow);
                    break;

                case ItemDataInspectorCategory.Component:
                    EditorGUILayout.Space(6f);
                    EditorGUILayout.LabelField("Craft Components", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(componentCategory);
                    break;

                case ItemDataInspectorCategory.Vehicle:
                    EditorGUILayout.Space(6f);
                    EditorGUILayout.LabelField("Deployable / Vehicle", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(deployedPrefab);
                    break;

                case ItemDataInspectorCategory.Resource:
                case ItemDataInspectorCategory.Operations:
                case ItemDataInspectorCategory.Quest:
                    break;
            }
        }

        private void DrawRestores()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Survival Restore", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(healthRestore);
            EditorGUILayout.PropertyField(energyRestore);
            EditorGUILayout.PropertyField(staminaRestore);
            EditorGUILayout.PropertyField(oxygenRestore);
        }

        private void DrawEquipment(bool includeSheathe, bool includeSwing)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Equipment", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(weaponGrip);
            EditorGUILayout.PropertyField(heldPrefab);
            EditorGUILayout.PropertyField(equipSocketName);
            EditorGUILayout.PropertyField(heldLocalPosition);
            EditorGUILayout.PropertyField(heldLocalEuler);
            EditorGUILayout.PropertyField(useHeldLocalRotation);
            EditorGUILayout.PropertyField(heldLocalRotation);
            EditorGUILayout.PropertyField(heldLocalScale);
            if (includeSwing)
                EditorGUILayout.PropertyField(swingEulerAngles);

            if (!includeSheathe)
                return;

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Sheathed (Back)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(sheatheSocketName);
            EditorGUILayout.PropertyField(sheathedLocalPosition);
            EditorGUILayout.PropertyField(sheathedLocalEuler);
            EditorGUILayout.PropertyField(useSheathedLocalRotation);
            EditorGUILayout.PropertyField(sheathedLocalRotation);
            EditorGUILayout.PropertyField(sheathedLocalScale);
        }

        private void DrawInvector()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Invector", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(invectorWeaponPrefab);
            EditorGUILayout.PropertyField(invectorWeaponId);
        }

        private void DrawMeleeStats()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Melee Base Stats", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(meleeDamage);
            EditorGUILayout.PropertyField(meleeDamageRandomRange);
            EditorGUILayout.PropertyField(criticalChance);
            EditorGUILayout.PropertyField(criticalDamageMultiplier);
            EditorGUILayout.PropertyField(meleeRange);
            EditorGUILayout.PropertyField(meleeCooldown);
            EditorGUILayout.PropertyField(attackAnimationSpeed);
            EditorGUILayout.PropertyField(meleeStaminaCost);
            EditorGUILayout.PropertyField(meleeKnockback);
            EditorGUILayout.PropertyField(gatherPower);
            EditorGUILayout.PropertyField(attackTrigger);
        }

        private void DrawRangedStats()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Ranged Base Stats", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(rangedDamage);
            EditorGUILayout.PropertyField(rangedDamageRandomRange);
            EditorGUILayout.PropertyField(rangedRange);
            EditorGUILayout.PropertyField(projectileSpeed);
            EditorGUILayout.PropertyField(projectileSpreadDegrees);
            EditorGUILayout.PropertyField(weaponAccuracy);
            EditorGUILayout.PropertyField(closeRangeFullAccuracyDistance);
            EditorGUILayout.PropertyField(closeRangeSpreadScale);
            EditorGUILayout.PropertyField(recoilVertical);
            EditorGUILayout.PropertyField(recoilHorizontal);
            EditorGUILayout.PropertyField(recoilFireRateScale);
            EditorGUILayout.PropertyField(fireRate);
            EditorGUILayout.PropertyField(magazineSize);
            EditorGUILayout.PropertyField(reloadTimeSeconds);
            EditorGUILayout.PropertyField(defaultAmmoType);
            EditorGUILayout.PropertyField(compatibleAmmoTypes, true);
            EditorGUILayout.PropertyField(defaultAmmoItem);
            EditorGUILayout.PropertyField(projectilePrefab);
            EditorGUILayout.PropertyField(muzzleSocketName);
            EditorGUILayout.PropertyField(aimFovMultiplier);
            EditorGUILayout.PropertyField(hipFireMaxDeviationDegrees);
            EditorGUILayout.PropertyField(hipFireSpreadMultiplier);
            EditorGUILayout.PropertyField(useAimHeldGrip);
            if (useAimHeldGrip.boolValue)
            {
                EditorGUILayout.PropertyField(aimHeldLocalPosition);
                EditorGUILayout.PropertyField(aimHeldLocalEuler);
                EditorGUILayout.PropertyField(useAimHeldLocalRotation);
                EditorGUILayout.PropertyField(aimHeldLocalRotation);
                EditorGUILayout.PropertyField(aimHeldLocalScale);
            }
        }

        private void DrawStartingMag()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Ranged Starting Mag", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(grantRandomStartingAmmo);
            using (new EditorGUI.DisabledScope(!grantRandomStartingAmmo.boolValue))
            {
                EditorGUILayout.PropertyField(startingAmmoMin);
                EditorGUILayout.PropertyField(startingAmmoMax);
            }
        }

        private void DrawAmmoSection()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Ammo", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(ammoType);
            EditorGUILayout.PropertyField(ammoPerPickup);
            EditorGUILayout.PropertyField(ammoPickupGrant);
            EditorGUILayout.PropertyField(rangedDamage);
            EditorGUILayout.PropertyField(rangedDamageRandomRange);
            EditorGUILayout.PropertyField(rangedRange);
            EditorGUILayout.PropertyField(projectileSpeed);
            EditorGUILayout.PropertyField(projectileSpreadDegrees);
            EditorGUILayout.PropertyField(weaponAccuracy);
            EditorGUILayout.PropertyField(closeRangeFullAccuracyDistance);
            EditorGUILayout.PropertyField(closeRangeSpreadScale);
            EditorGUILayout.LabelField("Shot Recoil (per weapon grip)", EditorStyles.miniBoldLabel);
            SerializedProperty ammoRecoilProfile = serializedObject.FindProperty("ammoRecoilProfile");
            if (ammoRecoilProfile != null)
                EditorGUILayout.PropertyField(ammoRecoilProfile, true);
            EditorGUILayout.PropertyField(projectilePrefab);
        }

        private void DrawProjectileBehavior()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Projectile Behavior", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(isHitscanBeam);
            EditorGUILayout.PropertyField(projectileGravityScale);
            EditorGUILayout.PropertyField(splashRadius);
            EditorGUILayout.PropertyField(splashDamageFalloff);
        }

        private void DrawProjectileBehaviorLight()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Projectile Behavior", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(isHitscanBeam);
            EditorGUILayout.PropertyField(projectileGravityScale);
        }

        private void DrawProjectileVfx()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Projectile VFX", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(muzzleFlashPrefab);
            EditorGUILayout.PropertyField(tracerPrefab);
            EditorGUILayout.PropertyField(impactVfxPrefab);
            EditorGUILayout.PropertyField(beamVfxPrefab);
        }

        private void DrawProjectileAudio(bool includeMiningScan)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Projectile / Mining Beam Audio", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(fireSound);
            EditorGUILayout.PropertyField(projectileTravelSound);
            EditorGUILayout.PropertyField(isContinuousLaser);
            EditorGUILayout.PropertyField(continuousLoopSound);
            EditorGUILayout.PropertyField(continuousStartSound);
            EditorGUILayout.PropertyField(continuousStopSound);
            if (!includeMiningScan)
                return;

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Mining Resource Scan Audio", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(miningScanLoopSound);
            EditorGUILayout.PropertyField(miningScanSuccessSound);
            EditorGUILayout.PropertyField(miningScanDeniedSound);
        }

        private void DrawMining()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Mining Tool", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(isMiningTool);
            EditorGUILayout.PropertyField(miningPassesRequired);
            EditorGUILayout.PropertyField(miningDropMin);
            EditorGUILayout.PropertyField(miningDropMax);
            EditorGUILayout.PropertyField(miningLockBreakDegrees);
            EditorGUILayout.PropertyField(miningPassDuration);
            EditorGUILayout.PropertyField(miningChunkVfxPrefab);
            EditorGUILayout.PropertyField(miningChargeDrainPerSecond);
            EditorGUILayout.PropertyField(miningChargePerPlasmaFuel);
            EditorGUILayout.PropertyField(rangedRange);
            EditorGUILayout.PropertyField(muzzleSocketName);
            EditorGUILayout.PropertyField(isHitscanBeam);
            EditorGUILayout.PropertyField(isContinuousLaser);
        }

        private void DrawElemental()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Elemental Effect", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(statusEffectOverride);
            EditorGUILayout.PropertyField(statusEffectDamagePerTick);
            EditorGUILayout.PropertyField(statusEffectTickInterval);
            EditorGUILayout.PropertyField(statusEffectDuration);
            EditorGUILayout.PropertyField(statusEffectVfxPrefab);
        }

        private void DrawToolOptics()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Tools / Optics", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(toolType);
            EditorGUILayout.PropertyField(toolRange);
            EditorGUILayout.PropertyField(scanRange);
            EditorGUILayout.PropertyField(opticsZoomFov);
            EditorGUILayout.PropertyField(opticsMinZoomFov);
            EditorGUILayout.PropertyField(opticsMaxZoomFov);
        }

        private void DrawToolGeneric()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(toolType);
            EditorGUILayout.PropertyField(toolRange);
            EditorGUILayout.PropertyField(scanRange);
        }
    }
}
