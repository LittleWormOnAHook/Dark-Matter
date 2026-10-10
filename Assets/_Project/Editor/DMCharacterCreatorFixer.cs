#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using Project.AI;
using Project.AI.Invector;
using Project.Data;
using Project.EditorTools.Invector;
using Project.Player.Invector;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>Non-destructive repairs the prefab list can apply (object ids and GUID are kept).</summary>
    [Flags]
    public enum DMCreatorFix
    {
        None = 0,
        LinkDefinition = 1,
        CapsuleFallback = 2,
        RootCapsule = 4,
        Culling = 8,
        Controller = 16,
        LoadoutCopy = 32,
        WeaponSlots = 64,
        AvatarTPose = 128
    }

    public sealed class DMCreatorFixPlan
    {
        public DMCreatorFix Fixes;
        public readonly List<string> Auto = new List<string>();
        public readonly List<string> Manual = new List<string>();
        public bool HasAuto => Fixes != DMCreatorFix.None;

        public void AddAuto(DMCreatorFix fix, string text)
        {
            Fixes |= fix;
            Auto.Add(text);
        }
    }

    /// <summary>
    /// Maps post-build check findings to safe in-place repairs. Anything that would rebuild the visual (may orphan
    /// scene children), edit a protected prefab or needs a data decision is listed as manual, never applied.
    /// </summary>
    public static class DMCharacterCreatorFixer
    {
        public const float CrossedArmHandDistance = 0.35f;

        public static DMCreatorFixPlan Analyze(
            GameObject contents,
            string prefabPath,
            EnemyDefinition linked,
            EnemyDefinition candidate,
            float handDistance,
            GameObject template,
            DMCharacterCreatorBuildReport report)
        {
            var plan = new DMCreatorFixPlan();
            if (DMCharacterCreatorProtection.IsProtectedPath(prefabPath))
            {
                if (report != null && (report.Errors.Count > 0 || report.Warnings.Count > 0))
                    plan.Manual.Add("Protected template: never modified by the creator (findings are informational).");
                return plan;
            }

            EnemyDefinition def = linked ?? candidate;
            if (linked == null)
            {
                if (candidate != null)
                    plan.AddAuto(DMCreatorFix.LinkDefinition, $"Link the saved definition '{candidate.name}' and bake its capsule fallback.");
                else
                    plan.Manual.Add("No definition link and no matching definition: create/save one in the creator form, then link it.");
            }

            if (def != null)
            {
                AnalyzeDefinitionDriven(contents, prefabPath, linked, def, handDistance, plan);
            }

            Animator anim = contents.GetComponent<Animator>();
            if (anim != null && anim.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                plan.AddAuto(DMCreatorFix.Culling, "Set Animator culling to AlwaysAnimate (creator standard; avoids chase glides).");

            if (anim != null && anim.runtimeAnimatorController == null)
            {
                RuntimeAnimatorController ctl = template != null && template.GetComponent<Animator>() != null
                    ? template.GetComponent<Animator>().runtimeAnimatorController
                    : null;
                if (ctl != null)
                    plan.AddAuto(DMCreatorFix.Controller, $"Assign the template animator controller '{ctl.name}'.");
                else
                    plan.Manual.Add("Root Animator has no controller and the template has none to copy.");
            }

            if (report != null)
            {
                for (int i = 0; i < report.Warnings.Count; i++)
                {
                    string w = report.Warnings[i];
                    if (w.StartsWith("Singular-template", StringComparison.Ordinal))
                        plan.Manual.Add("Migration needed (not auto-fixed): " + w);
                    else if (w.StartsWith("Definition:", StringComparison.Ordinal))
                        plan.Manual.Add("Edit the definition (data decision): " + w.Substring("Definition:".Length).Trim());
                    else if (w.StartsWith("Root Animator has no valid Humanoid avatar", StringComparison.Ordinal) ||
                             w.StartsWith("The humanoid avatar is not a saved asset", StringComparison.Ordinal))
                        plan.Manual.Add("Rebuild From Template (or re-import the model as Humanoid): " + w);
                }

                for (int i = 0; i < report.Errors.Count; i++)
                {
                    string e = report.Errors[i];
                    bool covered = e.StartsWith("No definition link", StringComparison.Ordinal) ||
                                   e.StartsWith("Root Animator has no controller", StringComparison.Ordinal) ||
                                   e.StartsWith("EnemyInvectorBootstrap.enemyDefinition is not linked", StringComparison.Ordinal) ||
                                   e.StartsWith("The definition's melee/ranged weapon was not copied", StringComparison.Ordinal);
                    if (!covered)
                        plan.Manual.Add("Error needs manual action: " + e);
                }
            }

            return plan;
        }

        static void AnalyzeDefinitionDriven(
            GameObject contents,
            string prefabPath,
            EnemyDefinition linked,
            EnemyDefinition def,
            float handDistance,
            DMCreatorFixPlan plan)
        {
            EnemyInvectorBootstrap boot = contents.GetComponent<EnemyInvectorBootstrap>();
            if (boot != null && linked != null)
            {
                var so = new SerializedObject(boot);
                SerializedProperty rad = so.FindProperty("hitCapsuleRadius");
                SerializedProperty hgt = so.FindProperty("hitCapsuleHeight");
                if (rad != null && hgt != null &&
                    (Mathf.Abs(rad.floatValue - def.colliderRadius) > 0.001f || Mathf.Abs(hgt.floatValue - def.colliderHeight) > 0.001f))
                    plan.AddAuto(
                        DMCreatorFix.CapsuleFallback,
                        $"Bake the definition capsule ({def.colliderRadius:0.###}/{def.colliderHeight:0.###}) into the Bootstrap fallback fields.");
            }

            CapsuleCollider cap = contents.GetComponent<CapsuleCollider>();
            if (cap != null && !def.fitColliderToRenderers &&
                (Mathf.Abs(cap.radius - def.colliderRadius) > 0.01f || Mathf.Abs(cap.height - def.colliderHeight) > 0.01f))
                plan.AddAuto(
                    DMCreatorFix.RootCapsule,
                    $"Set the root CapsuleCollider to the definition ({def.colliderRadius:0.###}/{def.colliderHeight:0.###}).");

            EnemyInvectorLoadoutBridge loadout = contents.GetComponent<EnemyInvectorLoadoutBridge>();
            if (loadout == null)
            {
                plan.Manual.Add("EnemyInvectorLoadoutBridge is missing: Rebuild From Template.");
            }
            else
            {
                var so = new SerializedObject(loadout);
                var melee = so.FindProperty("meleeWeaponItem")?.objectReferenceValue as ItemData;
                var ranged = so.FindProperty("rangedWeaponItem")?.objectReferenceValue as ItemData;
                if (melee != def.meleeWeaponItem || ranged != def.rangedWeaponItem)
                    plan.AddAuto(DMCreatorFix.LoadoutCopy, "Copy the definition's melee/ranged weapon into the LoadoutBridge.");
            }

            bool slotMissing = false;
            slotMissing |= SlotMissing(contents, def.meleeWeaponItem, "melee", plan);
            slotMissing |= SlotMissing(contents, def.rangedWeaponItem, "ranged", plan);
            ItemData start = def.preferRangedWeapon && def.rangedWeaponItem != null
                ? def.rangedWeaponItem
                : (def.meleeWeaponItem != null ? def.meleeWeaponItem : def.rangedWeaponItem);
            if (!slotMissing && start != null)
            {
                GameObject drawn = PioneerInvectorWeaponBridge.FindPreloadedDrawnSlot(contents.transform, start);
                if (drawn != null && !drawn.activeSelf)
                    plan.AddAuto(DMCreatorFix.WeaponSlots, $"Sync weapon slot visuals and arm '{start.name}' (active Drawn_ slot).");
            }

            if (handDistance >= 0f && handDistance < CrossedArmHandDistance)
            {
                Animator anim = contents.GetComponent<Animator>();
                string avatarPath = anim != null && anim.avatar != null ? AssetDatabase.GetAssetPath(anim.avatar) : null;
                string expected = DMHumanoidBoneRenameUtility.ResolveAvatarAssetPath(prefabPath);
                if (def.lastModelSource != null && !string.IsNullOrEmpty(avatarPath) &&
                    string.Equals(avatarPath, expected, StringComparison.OrdinalIgnoreCase))
                    plan.AddAuto(
                        DMCreatorFix.AvatarTPose,
                        $"Rebuild the humanoid avatar T-pose in place from '{def.lastModelSource.name}' (crossed arms, hand distance {handDistance:0.00}).");
                else
                    plan.Manual.Add(
                        $"Crossed-arm idle (hand distance {handDistance:0.00}) but the avatar is not a creator-generated asset or the definition has no Last Model Source: Rebuild From Template.");
            }
        }

        static bool SlotMissing(GameObject contents, ItemData item, string kind, DMCreatorFixPlan plan)
        {
            if (item == null)
                return false;
            if (PioneerInvectorWeaponBridge.FindPreloadedDrawnSlot(contents.transform, item) != null)
                return false;
            plan.Manual.Add(
                $"Needs manual rebuild: the {kind} weapon '{item.name}' has no Drawn_ slot (weapon holders are missing from the rig). Rebuild From Template restores them but may orphan scene children placed on the old visual.");
            return true;
        }

        /// <summary>Applies the given fixes in place. Caller backs up first. Object ids and GUID are kept.</summary>
        public static bool Apply(string prefabPath, EnemyDefinition def, DMCreatorFix fixes, out string message)
        {
            message = null;
            if (DMCharacterCreatorProtection.IsProtectedPath(prefabPath))
            {
                message = "Protected template; not modified.";
                return false;
            }

            if (def == null || !EditorUtility.IsPersistent(def))
            {
                message = "A saved EnemyDefinition asset is required.";
                return false;
            }

            var sb = new StringBuilder();
            bool ok = true;
            if ((fixes & DMCreatorFix.AvatarTPose) != 0)
            {
                bool a = DMCharacterCreatorRepair.ReapplyAvatarOnly(prefabPath, def, out string am);
                ok &= a;
                sb.AppendLine((a ? "Avatar: " : "Avatar FAILED: ") + am);
            }

            DMCreatorFix contentFixes = fixes & ~DMCreatorFix.AvatarTPose;
            if (contentFixes != DMCreatorFix.None)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                if (root == null)
                {
                    message = sb + "Could not open prefab.";
                    return false;
                }

                try
                {
                    if ((contentFixes & (DMCreatorFix.LinkDefinition | DMCreatorFix.CapsuleFallback)) != 0)
                    {
                        if (EnemyInvectorSetupUtility.TryWireBootstrapDefinition(root, def, out string err))
                            sb.AppendLine("Linked definition + baked capsule fallback.");
                        else
                        {
                            ok = false;
                            sb.AppendLine("Link FAILED: " + err);
                        }
                    }

                    if ((contentFixes & DMCreatorFix.RootCapsule) != 0)
                    {
                        CapsuleCollider cap = root.GetComponent<CapsuleCollider>();
                        if (cap != null)
                        {
                            cap.radius = def.colliderRadius;
                            cap.height = def.colliderHeight;
                            cap.center = def.colliderCenter;
                            sb.AppendLine("Root capsule set from the definition.");
                        }
                    }

                    Animator anim = root.GetComponent<Animator>();
                    if ((contentFixes & DMCreatorFix.Culling) != 0 && anim != null)
                    {
                        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                        sb.AppendLine("Animator culling set to AlwaysAnimate.");
                    }

                    if ((contentFixes & DMCreatorFix.Controller) != 0 && anim != null)
                    {
                        GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>(
                            EnemyPrefabVisualSetupUtility.ResolveTemplatePath(def.templatePrefab));
                        Animator ta = template != null ? template.GetComponent<Animator>() : null;
                        if (ta != null && ta.runtimeAnimatorController != null)
                        {
                            anim.runtimeAnimatorController = ta.runtimeAnimatorController;
                            sb.AppendLine("Animator controller restored from the template.");
                        }
                    }

                    if ((contentFixes & DMCreatorFix.LoadoutCopy) != 0)
                    {
                        EnemyInvectorLoadoutBridge loadout = root.GetComponent<EnemyInvectorLoadoutBridge>();
                        if (loadout != null)
                        {
                            var so = new SerializedObject(loadout);
                            SerializedProperty melee = so.FindProperty("meleeWeaponItem");
                            SerializedProperty ranged = so.FindProperty("rangedWeaponItem");
                            SerializedProperty prefer = so.FindProperty("preferRangedAtRange");
                            SerializedProperty startMelee = so.FindProperty("startWithMeleeWeapon");
                            if (melee != null) melee.objectReferenceValue = def.meleeWeaponItem;
                            if (ranged != null) ranged.objectReferenceValue = def.rangedWeaponItem;
                            if (prefer != null) prefer.boolValue = def.preferRangedWeapon;
                            if (startMelee != null) startMelee.boolValue = !def.preferRangedWeapon;
                            so.ApplyModifiedPropertiesWithoutUndo();
                            sb.AppendLine("Weapons copied from the definition into the LoadoutBridge.");
                        }
                    }

                    if ((contentFixes & DMCreatorFix.WeaponSlots) != 0)
                    {
                        bool armRanged = def.preferRangedWeapon && def.rangedWeaponItem != null;
                        EnemyInvectorSetupUtility.RepairWeaponSlotVisuals(
                            root,
                            armRanged ? null : def.meleeWeaponItem,
                            def.rangedWeaponItem);
                        sb.AppendLine("Weapon slot visuals synced; starting weapon armed.");
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            message = sb.ToString().TrimEnd();
            return ok;
        }
    }
}
#endif
