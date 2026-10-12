#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
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
    /// <summary>Result of the creator's post-build validation (errors block the save, warnings are surfaced).</summary>
    public sealed class DMCharacterCreatorBuildReport
    {
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Warnings = new List<string>();
        public readonly List<string> Info = new List<string>();
        public bool Failed => Errors.Count > 0;
        public bool HasWeaponWarning;

        public string Format()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < Errors.Count; i++)
                sb.AppendLine("ERROR: " + Errors[i]);
            for (int i = 0; i < Warnings.Count; i++)
                sb.AppendLine("WARNING: " + Warnings[i]);
            for (int i = 0; i < Info.Count; i++)
                sb.AppendLine("info: " + Info[i]);
            return sb.ToString().TrimEnd();
        }
    }

    /// <summary>Protected template names. Creator output can never overwrite these.</summary>
    public static class DMCharacterCreatorProtection
    {
        public static bool IsProtectedPrefabName(string fileNameWithoutExtension)
        {
            if (string.IsNullOrEmpty(fileNameWithoutExtension))
                return false;

            return fileNameWithoutExtension.StartsWith("Player_v7", StringComparison.OrdinalIgnoreCase) ||
                   fileNameWithoutExtension.StartsWith("Player_Invector", StringComparison.OrdinalIgnoreCase) ||
                   fileNameWithoutExtension.Equals("HumanoidEnemy_Invector", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsProtectedPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;
            return IsProtectedPrefabName(Path.GetFileNameWithoutExtension(assetPath.Replace('\\', '/')));
        }
    }

    /// <summary>
    /// The creator must always link the SAVED EnemyDefinition asset (never the panel's in-memory working copy).
    /// </summary>
    public static class DMCharacterCreatorDefinitionLink
    {
        public static string FileNameFor(EnemyDefinition def)
        {
            if (def == null)
                return "NewEnemy";
            return DMCharacterCreatorDefinitionSidebar.SuggestEnemyDefinitionAssetFileName(
                def.prefabFileName, def.enemyId, def.displayName);
        }

        public static string AssetPathFor(string fileName) =>
            $"{ProjectAssetPaths.EnemiesData}/{fileName}.asset";

        /// <summary>Path the working definition should be saved to (keeps an existing asset's identity).</summary>
        public static string ResolveAssetPath(EnemyDefinition working)
        {
            if (working == null)
                return null;

            if (EditorUtility.IsPersistent(working))
                return AssetDatabase.GetAssetPath(working);

            // A working copy of a library asset keeps that asset (e.g. FRED_Android, prefab "FRED").
            if (!string.IsNullOrWhiteSpace(working.name))
            {
                string byName = AssetPathFor(working.name);
                EnemyDefinition named = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(byName);
                if (named != null &&
                    string.Equals(named.prefabFileName, working.prefabFileName, StringComparison.OrdinalIgnoreCase))
                    return byName;
            }

            return AssetPathFor(FileNameFor(working));
        }

        /// <summary>True when saving would silently overwrite a DIFFERENT definition or duplicate an id.</summary>
        public static bool TryFindCollision(EnemyDefinition working, out string message)
        {
            message = null;
            if (working == null || EditorUtility.IsPersistent(working))
                return false;

            string path = ResolveAssetPath(working);
            EnemyDefinition existing = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (existing != null && !string.Equals(working.name, existing.name, StringComparison.OrdinalIgnoreCase))
            {
                message =
                    $"A definition named '{fileName}' already exists ({path}). Saving would overwrite it. " +
                    "Pick a different Prefab File Name, or select that definition from the library to edit it.";
                return true;
            }

            if (!string.IsNullOrWhiteSpace(working.enemyId))
            {
                string[] guids = AssetDatabase.FindAssets("t:EnemyDefinition", new[] { ProjectAssetPaths.EnemiesData });
                for (int i = 0; i < guids.Length; i++)
                {
                    string other = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (string.Equals(other, path, StringComparison.OrdinalIgnoreCase))
                        continue;
                    EnemyDefinition def = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(other);
                    if (def != null && string.Equals(def.enemyId, working.enemyId, StringComparison.OrdinalIgnoreCase))
                    {
                        message = $"Enemy Id '{working.enemyId}' is already used by {other}. Enemy Ids must be unique.";
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Unique prefab/definition/id base name for "New Custom" (NewEnemy, NewEnemy_2, ...).</summary>
        public static string MakeUniqueName(string baseName)
        {
            string name = string.IsNullOrWhiteSpace(baseName) ? "NewEnemy" : baseName;
            for (int i = 1; i < 500; i++)
            {
                string candidate = i == 1 ? name : name + "_" + i;
                bool taken =
                    AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetPathFor(candidate)) != null ||
                    File.Exists($"{ProjectAssetPaths.PrefabsCombatEnemies}/{candidate}.prefab");
                if (!taken)
                {
                    string[] guids = AssetDatabase.FindAssets("t:EnemyDefinition", new[] { ProjectAssetPaths.EnemiesData });
                    for (int g = 0; g < guids.Length && !taken; g++)
                    {
                        EnemyDefinition other = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guids[g]));
                        taken = other != null && string.Equals(other.enemyId, candidate, StringComparison.OrdinalIgnoreCase);
                    }
                }

                if (!taken)
                    return candidate;
            }

            return name + "_" + DateTime.Now.ToString("HHmmss");
        }

        public static bool TryEnsureSaved(EnemyDefinition working, out EnemyDefinition saved, out string error)
        {
            saved = null;
            error = null;
            if (working == null)
            {
                error = "No enemy definition to link.";
                return false;
            }

            if (EditorUtility.IsPersistent(working))
            {
                EditorUtility.SetDirty(working);
                AssetDatabase.SaveAssets();
                saved = working;
                return true;
            }

            EnemyPrefabCreatorPanel.EnsureEnemyIdentityDefaults(working);
            if (TryFindCollision(working, out string collision))
            {
                error = collision;
                return false;
            }

            string path = ResolveAssetPath(working);
            string fileName = Path.GetFileNameWithoutExtension(path);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.EnemiesData);

            EnemyDefinition existing = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (existing == null)
            {
                EnemyDefinition asset = UnityEngine.Object.Instantiate(working);
                asset.name = fileName;
                AssetDatabase.CreateAsset(asset, path);
            }
            else
            {
                EditorUtility.CopySerialized(working, existing);
                existing.name = fileName;
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssets();
            saved = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (saved == null || !EditorUtility.IsPersistent(saved))
            {
                error = $"Definition asset could not be saved/loaded at {path}.";
                saved = null;
                return false;
            }

            // The working copy now belongs to this asset (so a later save is not mistaken for a name collision).
            working.name = fileName;
            return true;
        }
    }

    /// <summary>Post-build validation: link authority, weapon loadout, capsule, rig, singular-template rule.</summary>
    public static class DMCharacterCreatorPostBuildCheck
    {
        public static DMCharacterCreatorBuildReport LastReport { get; private set; }
        public static EnemyDefinition LastSavedDefinition { get; set; }

        public static DMCharacterCreatorBuildReport Run(
            GameObject root,
            EnemyDefinition def,
            string outputPath,
            GameObject templateAsset)
        {
            var r = new DMCharacterCreatorBuildReport();
            LastReport = r;
            if (root == null)
            {
                r.Errors.Add("Built prefab root is missing.");
                return r;
            }

            if (def == null)
            {
                if (templateAsset != null && root.name == templateAsset.name)
                    r.Info.Add("This is the singular template: it carries no EnemyDefinition by design (each enemy links its own).");
                else
                    r.Warnings.Add(
                        "No EnemyDefinition was linked: prefab-side values are the only source (no authority).");
            }
            else
            {
                CheckDefinitionLink(root, def, r);
                CheckWeapons(root, def, r);
                CheckCapsule(root, def, r);
                DMCharacterCreatorWeaponAudit.CheckValueDrift(root, def, r);
                AddLiveVersusBakedInfo(r);
            }

            CheckRig(root, r);
            CheckSingularTemplate(root, templateAsset, r, def);
            CheckTemplateSync(root, def, templateAsset, r);
            return r;
        }

        static void CheckDefinitionLink(GameObject root, EnemyDefinition def, DMCharacterCreatorBuildReport r)
        {
            if (!EditorUtility.IsPersistent(def))
            {
                r.Errors.Add(
                    "The definition is an unsaved in-memory copy. Save Definition first; the prefab must link the saved asset.");
                return;
            }

            EnemyInvectorBootstrap boot = root.GetComponent<EnemyInvectorBootstrap>();
            if (boot == null)
            {
                r.Errors.Add("EnemyInvectorBootstrap is missing from the output; the definition cannot be linked.");
                return;
            }

            var so = new SerializedObject(boot);
            var link = so.FindProperty("enemyDefinition")?.objectReferenceValue as EnemyDefinition;
            if (link == null || link != def)
            {
                r.Errors.Add(
                    $"EnemyInvectorBootstrap.enemyDefinition is not linked to the saved asset '{AssetDatabase.GetAssetPath(def)}'.");
            }

            // Prefab-side fields that still shadow the definition (should equal it; runtime reads the definition).
            SerializedProperty rad = so.FindProperty("hitCapsuleRadius");
            SerializedProperty hgt = so.FindProperty("hitCapsuleHeight");
            if (rad != null && hgt != null &&
                (Mathf.Abs(rad.floatValue - def.colliderRadius) > 0.001f ||
                 Mathf.Abs(hgt.floatValue - def.colliderHeight) > 0.001f))
            {
                r.Warnings.Add(
                    $"Bootstrap hit capsule fallback ({rad.floatValue:0.###}/{hgt.floatValue:0.###}) differs from the definition " +
                    $"({def.colliderRadius:0.###}/{def.colliderHeight:0.###}). Runtime uses the definition; run Re-apply Definition to refresh the fallback.");
            }
        }

        static void CheckWeapons(GameObject root, EnemyDefinition def, DMCharacterCreatorBuildReport r)
        {
            EnemyInvectorLoadoutBridge loadout = root.GetComponent<EnemyInvectorLoadoutBridge>();
            if (loadout == null)
            {
                r.Errors.Add("EnemyInvectorLoadoutBridge is missing; weapons cannot be equipped.");
                return;
            }

            // WEAPON FLOW: LoadoutBridge fields are per-instance OVERRIDES; empty = the definition's weapon (default).
            ItemData melee = loadout.ResolvedMelee;
            ItemData ranged = loadout.ResolvedRanged;
            ItemData start = loadout.ResolveStartingWeapon();
            r.Info.Add(
                $"Weapon flow (LoadoutBridge is the authority): melee = {Name(melee)} [{Source(loadout.MeleeSource)}], " +
                $"ranged = {Name(ranged)} [{Source(loadout.RangedSource)}], " +
                $"prefer ranged = {loadout.ResolvedPreferRanged} [{(loadout.PreferRangedOverride == EnemyPreferRangedOverride.UseDefinition ? "Definition default" : "Bridge override")}], " +
                $"starts with {Name(start)}.");

            // A set bridge field is an intentional override and is never a mismatch, unless it merely copies the definition.
            if (loadout.MeleeOverride != null && loadout.MeleeOverride == def.meleeWeaponItem)
                r.Warnings.Add(
                    $"Redundant weapon override: the LoadoutBridge melee field ('{loadout.MeleeOverride.name}') only copies the definition's weapon, so it will not follow a later definition edit. Fix clears it.");
            if (loadout.RangedOverride != null && loadout.RangedOverride == def.rangedWeaponItem)
                r.Warnings.Add(
                    $"Redundant weapon override: the LoadoutBridge ranged field ('{loadout.RangedOverride.name}') only copies the definition's weapon, so it will not follow a later definition edit. Fix clears it.");

            CheckWeaponSlot(root, melee, "melee", r);
            CheckWeaponSlot(root, ranged, "ranged", r);

            if (start != null)
            {
                GameObject drawn = PioneerInvectorWeaponBridge.FindPreloadedDrawnSlot(root.transform, start);
                if (drawn != null && !drawn.activeSelf)
                {
                    r.Warnings.Add(
                        $"Starting weapon '{start.name}' has a Drawn_ slot but it is not active in the prefab (it is drawn at spawn by the LoadoutBridge).");
                }
            }
            else if (melee == null && ranged == null)
            {
                r.Info.Add("No weapon resolved (override or definition): the enemy spawns unarmed by design.");
            }

            DMCharacterCreatorWeaponAudit.CheckUnlistedWeapons(root, start, r);
        }

        static string Source(EnemyWeaponSource s) =>
            s == EnemyWeaponSource.Override ? "Bridge override" : (s == EnemyWeaponSource.Definition ? "Definition default" : "none");

        static void CheckWeaponSlot(GameObject root, ItemData item, string kind, DMCharacterCreatorBuildReport r)
        {
            if (item == null)
                return;

            GameObject drawn = PioneerInvectorWeaponBridge.FindPreloadedDrawnSlot(root.transform, item);
            if (drawn == null)
            {
                r.HasWeaponWarning = true;
                r.Warnings.Add(
                    $"WEAPON NOT EQUIPPABLE: the resolved {kind} weapon '{item.name}' (override or definition) has no " +
                    $"Drawn_ slot for it (weapon holders / Drawn_* objects are missing from the rig). Rebuild From Template.");
            }
        }

        static void CheckCapsule(GameObject root, EnemyDefinition def, DMCharacterCreatorBuildReport r)
        {
            CapsuleCollider cap = root.GetComponent<CapsuleCollider>();
            if (cap == null)
            {
                r.Warnings.Add("No root CapsuleCollider (runtime will create one from the definition).");
                return;
            }

            if (!def.fitColliderToRenderers &&
                (Mathf.Abs(cap.radius - def.colliderRadius) > 0.01f || Mathf.Abs(cap.height - def.colliderHeight) > 0.01f))
            {
                r.Warnings.Add(
                    $"Root capsule ({cap.radius:0.##}/{cap.height:0.##}) differs from the definition " +
                    $"({def.colliderRadius:0.##}/{def.colliderHeight:0.##}); the definition wins at runtime.");
            }
        }

        static void CheckRig(GameObject root, DMCharacterCreatorBuildReport r)
        {
            Animator anim = root.GetComponent<Animator>();
            if (anim == null || anim.avatar == null || !anim.avatar.isValid || !anim.avatar.isHuman)
            {
                r.Warnings.Add("Root Animator has no valid Humanoid avatar.");
                return;
            }

            if (anim.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                r.Warnings.Add("Enemy Animator culling is not AlwaysAnimate (the creator standard; CullUpdateTransforms causes chase glides). Re-apply Definition / Fix restores it.");

            if (anim.runtimeAnimatorController == null)
                r.Errors.Add("Root Animator has no controller.");

            if (!EditorUtility.IsPersistent(anim.avatar))
                r.Warnings.Add("The humanoid avatar is not a saved asset (it will be lost on reload).");
        }

        static void CheckSingularTemplate(GameObject root, GameObject templateAsset, DMCharacterCreatorBuildReport r, EnemyDefinition def = null)
        {
            if (templateAsset == null)
                return;

            var templateTypes = new HashSet<Type>();
            foreach (Component c in templateAsset.GetComponents<Component>())
            {
                if (c != null)
                    templateTypes.Add(c.GetType());
            }

            var extras = new List<string>();
            foreach (Component c in root.GetComponents<Component>())
            {
                if (c == null || templateTypes.Contains(c.GetType()))
                    continue;
                if (def != null && def.canThrowGrenades && DMEnemyTemplateSync.IsGrenadeType(c.GetType().Name))
                    continue; // allowed by the definition's Can Throw Grenades
                extras.Add(c.GetType().Name);
            }

            if (extras.Count > 0)
            {
                r.Warnings.Add(
                    "Singular-template rule: root has components that are not on the template (" +
                    string.Join(", ", extras) + "). Variation must live in the EnemyDefinition / profiles.");
            }
        }

        static void CheckTemplateSync(GameObject root, EnemyDefinition def, GameObject templateAsset, DMCharacterCreatorBuildReport r)
        {
            bool isTemplate = templateAsset != null && root.name == templateAsset.name;
            foreach (DMSyncItem it in DMEnemyTemplateSync.Analyze(root, def, templateAsset, isTemplate))
                r.Warnings.Add("Template sync: " + it.Text + (it.Auto ? string.Empty : " (manual)"));
        }

        static void AddLiveVersusBakedInfo(DMCharacterCreatorBuildReport r)
        {
            r.Info.Add(
                "LIVE-LINKED (read from the definition on every spawn): stats, health, senses, melee/ranged/AI timings, movement, " +
                "loot + loot bag, health bar, body type, brain archetype/personality + profile overrides, XP, weapon items, hit capsule.");
            r.Info.Add(
                "BAKED into the prefab (refresh with Re-apply Definition): Drawn_/Holstered_ weapon slot visuals + active slot, " +
                "Bootstrap hit-capsule fallback fields, root CapsuleCollider, visual model + avatar, animator controller.");
        }

        static string Name(UnityEngine.Object o) => o != null ? o.name : "none";
    }

    /// <summary>Re-apply a definition (and the T-pose-correct avatar) to an existing creator prefab.</summary>
    public static class DMCharacterCreatorRepair
    {
        /// <summary>
        /// Rebuilds the humanoid avatar from the source model's avatar T-pose (see DMHumanoidBoneRenameUtility).
        /// Fixes the "crossed arms" idle on prefabs built before the fix. Non-destructive to the hierarchy.
        /// </summary>
        public static bool RebuildAvatarTPose(GameObject root, EnemyDefinition def, string prefabPath, out string message)
        {
            message = null;
            if (root == null || def == null || def.lastModelSource == null)
            {
                message = "No source model recorded on the definition (lastModelSource); avatar not rebuilt.";
                return false;
            }

            Animator animator = root.GetComponent<Animator>();
            string visualName = string.IsNullOrWhiteSpace(def.visualChildName) ? "Visual" : def.visualChildName;
            Transform visual = root.transform.Find(visualName);
            if (animator == null || animator.avatar == null || visual == null)
            {
                message = "Animator/avatar/Visual child not found; avatar not rebuilt.";
                return false;
            }

            string modelPath = EnemyModelAvatarUtility.ResolvePreferredModelAssetPath(def.lastModelSource);
            Avatar source = null;
            if (!string.IsNullOrEmpty(modelPath))
            {
                UnityEngine.Object[] all = AssetDatabase.LoadAllAssetsAtPath(modelPath);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] is Avatar a && a.isHuman && a.isValid)
                    {
                        source = a;
                        break;
                    }
                }
            }

            if (source == null)
            {
                message = $"Source model '{def.lastModelSource.name}' has no valid Humanoid avatar; avatar not rebuilt.";
                return false;
            }

            var names = DMHumanoidBoneRenameUtility.SnapshotNamesFromAvatar(animator.avatar);
            Avatar rebuilt = DMHumanoidBoneRenameUtility.RebuildHumanoidAvatar(visual.gameObject, source, names);
            if (rebuilt == null)
            {
                message = "AvatarBuilder failed; the existing avatar was kept.";
                return false;
            }

            string avatarPath = DMHumanoidBoneRenameUtility.ResolveAvatarAssetPath(prefabPath);
            rebuilt = DMHumanoidBoneRenameUtility.SaveAvatarAsset(rebuilt, avatarPath);
            animator.avatar = rebuilt;
            message = $"Avatar rebuilt from '{def.lastModelSource.name}' T-pose and saved to {avatarPath}.";
            return true;
        }

        /// <summary>
        /// Avatar-only re-apply: rewrites the prefab's saved avatar asset in place (GUID kept) from the source model's
        /// T-pose. The prefab, its hierarchy and every scene instance are not touched.
        /// </summary>
        public static bool ReapplyAvatarOnly(string prefabPath, EnemyDefinition def, out string message)
        {
            message = null;
            if (DMCharacterCreatorProtection.IsProtectedPath(prefabPath))
            {
                message = "Protected template; not modified.";
                return false;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                message = "Could not open prefab.";
                return false;
            }

            try
            {
                Animator animator = root.GetComponent<Animator>();
                string avatarPath = animator != null && animator.avatar != null ? AssetDatabase.GetAssetPath(animator.avatar) : null;
                string expected = DMHumanoidBoneRenameUtility.ResolveAvatarAssetPath(prefabPath);
                if (string.IsNullOrEmpty(avatarPath) || !string.Equals(avatarPath, expected, StringComparison.OrdinalIgnoreCase))
                {
                    message = $"Prefab avatar ({(string.IsNullOrEmpty(avatarPath) ? "none" : avatarPath)}) is not a creator-generated avatar asset; nothing to re-apply.";
                    return false;
                }

                return RebuildAvatarTPose(root, def, prefabPath, out message);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Link-only repair: sets the saved definition on the bootstrap and bakes the capsule fallback.</summary>
        public static bool RelinkDefinition(string prefabPath, EnemyDefinition savedDefinition, out string message)
        {
            message = null;
            if (savedDefinition == null || !EditorUtility.IsPersistent(savedDefinition))
            {
                message = "Definition must be a saved asset.";
                return false;
            }

            if (DMCharacterCreatorProtection.IsProtectedPath(prefabPath))
            {
                message = "Protected template; not modified.";
                return false;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                message = "Could not open prefab.";
                return false;
            }

            try
            {
                if (!EnemyInvectorSetupUtility.TryWireBootstrapDefinition(root, savedDefinition, out message))
                    return false;

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                message = $"Linked {AssetDatabase.GetAssetPath(savedDefinition)} on {prefabPath}.";
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    /// <summary>
    /// Weapon-object and prefab-vs-definition drift audit shared by the post-build check, the Fixer and the prefab
    /// list. The LoadoutBridge hides everything but the resolved weapon at runtime; this reports/repairs the prefab
    /// side so the asset matches what the runtime enforces.
    /// </summary>
    public static class DMCharacterCreatorWeaponAudit
    {
        /// <summary>Active Drawn_/Holstered_ slots (other than the resolved start weapon) and baked holder meshes.</summary>
        public static List<Transform> FindUnlistedActive(GameObject root, ItemData start)
        {
            var found = new List<Transform>();
            if (root == null)
                return found;

            GameObject keep = start != null
                ? PioneerInvectorWeaponBridge.FindPreloadedDrawnSlot(root.transform, start)
                : null;

            // The OTHER resolved weapon may stay on its holster (one drawn, the other holstered).
            GameObject keepHolster = null;
            var lb = root.GetComponent<EnemyInvectorLoadoutBridge>();
            if (lb != null && start != null)
            {
                ItemData other = start == lb.ResolvedMelee ? lb.ResolvedRanged : lb.ResolvedMelee;
                if (other != null && other != start)
                    keepHolster = PioneerInvectorWeaponBridge.FindPreloadedHolsteredSlot(root.transform, other);
            }

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null || t == root.transform)
                    continue;

                bool drawn = t.name.StartsWith("Drawn_", StringComparison.Ordinal);
                bool holstered = t.name.StartsWith("Holstered_", StringComparison.Ordinal);
                if (!drawn && !holstered)
                    continue;
                if (!t.gameObject.activeInHierarchy)
                    continue;
                if (drawn && keep != null && t.gameObject == keep)
                    continue;
                if (holstered && keepHolster != null && t.gameObject == keepHolster)
                    continue;

                found.Add(t);
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer rend = renderers[i];
                if (rend == null || rend is SkinnedMeshRenderer || IsUnderSlot(rend.transform))
                    continue;

                Transform holderChild = FindHolderChild(rend.transform);
                if (holderChild != null && holderChild.gameObject.activeInHierarchy && !found.Contains(holderChild))
                    found.Add(holderChild);
            }

            return found;
        }

        static bool IsUnderSlot(Transform t)
        {
            while (t != null)
            {
                if (t.name.StartsWith("Drawn_", StringComparison.Ordinal) ||
                    t.name.StartsWith("Holstered_", StringComparison.Ordinal))
                    return true;
                t = t.parent;
            }

            return false;
        }

        static Transform FindHolderChild(Transform t)
        {
            Transform c = t;
            while (c != null && c.parent != null)
            {
                string parentName = c.parent.name;
                if (parentName.EndsWith("Holder", StringComparison.Ordinal) ||
                    parentName.Equals("WeaponHolders", StringComparison.Ordinal))
                    return c;
                c = c.parent;
            }

            return null;
        }

        public static void CheckUnlistedWeapons(GameObject root, ItemData start, DMCharacterCreatorBuildReport r)
        {
            List<Transform> extra = FindUnlistedActive(root, start);
            if (extra.Count == 0)
                return;

            var names = new List<string>();
            int drawnExtra = 0;
            for (int i = 0; i < extra.Count; i++)
                if (extra[i].name.StartsWith("Drawn_", StringComparison.Ordinal))
                    drawnExtra++;
            for (int i = 0; i < extra.Count && i < 5; i++)
                names.Add(extra[i].name);

            if (drawnExtra > 0)
                r.Warnings.Add(
                    $"Both drawn at once: {drawnExtra} extra Drawn_ weapon slot(s) are active next to the resolved weapon ({names[0]}). Only one weapon may be drawn; Fix deactivates the extras.");

            r.Warnings.Add(
                $"Weapon objects: {extra.Count} weapon object(s) are active on the prefab but are not the resolved weapon ({string.Join(", ", names)}" +
                (extra.Count > 5 ? ", ..." : string.Empty) +
                "). The LoadoutBridge hides them at runtime; Fix deactivates them on the prefab.");
        }

        public static int DeactivateUnlisted(GameObject root, ItemData start)
        {
            List<Transform> extra = FindUnlistedActive(root, start);
            for (int i = 0; i < extra.Count; i++)
                extra[i].gameObject.SetActive(false);
            return extra.Count;
        }

        // ---- prefab value vs definition -----------------------------------------------------------------
        struct Field
        {
            public string Component;
            public string Name;
            public Func<EnemyDefinition, float> Value;
        }

        static readonly Field[] Fields =
        {
            new Field { Component = "EnemyHealth", Name = "maxHealth", Value = d => d.maxHealth },
            new Field { Component = "EnemyHealth", Name = "destroyDelay", Value = d => d.destroyDelay },
            new Field { Component = "EnemySenses", Name = "visionRange", Value = d => d.visionRange },
            new Field { Component = "EnemySenses", Name = "visionFov", Value = d => d.visionFov },
            new Field { Component = "EnemySenses", Name = "hearingRange", Value = d => d.hearingRange },
            new Field { Component = "EnemySenses", Name = "proximityRange", Value = d => d.proximityRange },
            new Field { Component = "EnemyCombat", Name = "attackRange", Value = d => d.attackRange },
            new Field { Component = "EnemyCombat", Name = "attackDamage", Value = d => d.attackDamage },
            new Field { Component = "EnemyCombat", Name = "attackCooldown", Value = d => d.attackCooldown },
            new Field { Component = "EnemyCombat", Name = "attackWindup", Value = d => d.attackWindup },
            new Field { Component = "EnemyAiController", Name = "walkSpeed", Value = d => d.walkSpeed },
            new Field { Component = "EnemyAiController", Name = "runSpeed", Value = d => d.runSpeed },
            new Field { Component = "EnemyAiController", Name = "chaseSpeed", Value = d => d.chaseSpeed },
            new Field { Component = "EnemyAiController", Name = "wanderRadius", Value = d => d.wanderRadius },
            new Field { Component = "EnemyAiController", Name = "chaseRadius", Value = d => d.chaseRadius },
            new Field { Component = "EnemyAiController", Name = "turnSpeed", Value = d => d.turnSpeed },
        };

        static bool TryRead(SerializedProperty p, out float value)
        {
            value = 0f;
            if (p == null)
                return false;
            if (p.propertyType == SerializedPropertyType.Float) { value = p.floatValue; return true; }
            if (p.propertyType == SerializedPropertyType.Integer) { value = p.intValue; return true; }
            return false;
        }

        /// <summary>Prefab-side values that differ from the definition (runtime uses the definition; these are fallbacks).</summary>
        public static List<string> FindValueDrift(GameObject root, EnemyDefinition def)
        {
            var diffs = new List<string>();
            if (root == null || def == null)
                return diffs;

            for (int i = 0; i < Fields.Length; i++)
            {
                Component c = root.GetComponent(Fields[i].Component);
                if (c == null)
                    continue;

                SerializedProperty p = new SerializedObject(c).FindProperty(Fields[i].Name);
                if (!TryRead(p, out float prefabValue))
                    continue;

                float defValue = Fields[i].Value(def);
                if (Mathf.Abs(prefabValue - defValue) > 0.001f)
                    diffs.Add($"{Fields[i].Component}.{Fields[i].Name} prefab={prefabValue:0.##} vs definition={defValue:0.##}");
            }

            return diffs;
        }

        public static void CheckValueDrift(GameObject root, EnemyDefinition def, DMCharacterCreatorBuildReport r)
        {
            List<string> diffs = FindValueDrift(root, def);
            if (diffs.Count == 0)
                return;

            int shown = Mathf.Min(diffs.Count, 4);
            r.Warnings.Add(
                $"Prefab value differs from definition: {diffs.Count} field(s) ({string.Join("; ", diffs.GetRange(0, shown))}" +
                (diffs.Count > shown ? "; ..." : string.Empty) +
                "). Runtime uses the definition; Fix syncs the prefab fallback values (the definition is never changed).");
        }

        /// <summary>Writes the definition's values into the prefab-side fallback fields. Returns the count changed.</summary>
        public static int SyncValues(GameObject root, EnemyDefinition def)
        {
            int changed = 0;
            if (root == null || def == null)
                return 0;

            for (int i = 0; i < Fields.Length; i++)
            {
                Component c = root.GetComponent(Fields[i].Component);
                if (c == null)
                    continue;

                var so = new SerializedObject(c);
                SerializedProperty p = so.FindProperty(Fields[i].Name);
                if (!TryRead(p, out float prefabValue))
                    continue;

                float defValue = Fields[i].Value(def);
                if (Mathf.Abs(prefabValue - defValue) <= 0.001f)
                    continue;

                if (p.propertyType == SerializedPropertyType.Float)
                    p.floatValue = defValue;
                else
                    p.intValue = Mathf.RoundToInt(defValue);
                so.ApplyModifiedPropertiesWithoutUndo();
                changed++;
            }

            return changed;
        }
    }
}
#endif
