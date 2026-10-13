#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using Invector.vShooter;
using Project.AI;
using Project.AI.Invector;
using Project.Combat;
using Project.Data;
using Project.Player.Invector;
using Project.Progression;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>One finding of the template sync (one thing the Sync to Template action can add, remove or fix).</summary>
    public sealed class DMSyncItem
    {
        public string Id;
        public string Text;
        public bool Auto = true;
    }

    /// <summary>
    /// "Sync to Template": brings an enemy prefab to the singular template's script set. Only root components and
    /// weapon slot / holder children are touched. Definition-driven values, tuning values, vSnapToBody, vDamageReceiver,
    /// patrol points and hand-placed scene children are never edited. GUID and object ids are kept (edits happen in
    /// place on the loaded prefab contents).
    /// </summary>
    public static class DMEnemyTemplateSync
    {
        public const string TemplatePath = "Assets/_Project/Prefabs/Combat/Enemies/HumanoidEnemy_Invector.prefab";
        public const string HitboxLayerName = "DMHitbox";
        const string StandardAmmoPath = "Assets/_Project/Data/Items/Ammo/DMAmmoFxProfile_Standard.asset";
        const string PlasmaAmmoPath = "Assets/_Project/Data/Items/Ammo/DMAmmoFxProfile_Plasma.asset";

        /// <summary>Player-only components that must not be on an enemy.</summary>
        static readonly string[] PlayerOnlyTypes = { "PlayerProgressionManager" };

        /// <summary>Allowed only while EnemyDefinition.canThrowGrenades is on (the future brain hook).</summary>
        static readonly string[] GrenadeTypes = { "DMIGrenadeCookController", "DMIGrenadeThrowBridge" };

        static readonly string[] PlayerOnlySlotNames = { "DM_Mining_Tool" };
        static readonly string[] PlayerOnlyHolderMeshes = { "ScifiPistolFlashlightBlack" };

        /// <summary>
        /// Scripts the audit classifies UNUSED (no runtime or editor code references them; see DM_Script_Audit_And_Slimming_Plan.md).
        /// Sync never ADDS them and never copies them onto new weapon-slot clones. Existing instances are left alone until
        /// Anthony confirms the audit (nothing is removed from existing enemies in this pass).
        /// </summary>
        public static readonly string[] UnusedPendingAudit =
        {
            "vComment", "vEntityPreview", "vInstantiate", "vWeaponConstrain", "vHitEffects", "vWeaponCollectableControl"
        };

        /// <summary>Root-level template components Sync never adds even if the template carries them.</summary>
        static readonly string[] NeverAddRoot = { "Transform", "Animator", "CapsuleCollider", "Rigidbody" };

        /// <summary>Non-tuning, non-definition Invector locomotion/health fields aligned to the template value (Invector health is disabled on enemies).</summary>
        static readonly string[][] AlignFieldsList =
        {
            new[] { "vThirdPersonController", "fillHealthOnStart" },
            new[] { "vThirdPersonController", "_maxHealth" },
            new[] { "vThirdPersonController", "_currentHealth" },
            new[] { "vThirdPersonController", "selectedToolbar" },
            new[] { "vThirdPersonController", "_isDead" },
        };

        /// <summary>
        /// Tuning values that follow the template (decision 2026-10-10). Definition-driven fields (health, senses, attack values,
        /// speeds, radii, hearing aggro, loot, capsule, body type, weapons) and structural/hand data are NOT in this list.
        /// </summary>
        static readonly string[][] TuningFieldsList =
        {
            new[] { "EnemyAiController", "chaseStaminaPauseMin" },
            new[] { "EnemyAiController", "chaseStaminaPauseMax" },
            new[] { "EnemyAiController", "playerThreatRange" },
            new[] { "EnemyAiController", "minCombatSeparation" },
            new[] { "EnemyAiController", "attackStandoffFraction" },
            new[] { "EnemyAiController", "playerStandoffBonus" },
            new[] { "EnemyInvectorRagdollBridge", "defaultStaggerSeconds" },
            new[] { "EnemyInvectorRagdollBridge", "baseReactionChance" },
            new[] { "EnemyInvectorRagdollBridge", "criticalReactionChanceMultiplier" },
            new[] { "CombatPoise", "maxPoise" },
        };

        public static bool IsUnusedPendingAudit(string typeName) => Array.IndexOf(UnusedPendingAudit, typeName) >= 0;

        public static bool IsGrenadeType(string typeName) => Array.IndexOf(GrenadeTypes, typeName) >= 0;

        // ------------------------------------------------------------------ analyze

        public static List<DMSyncItem> Analyze(GameObject root, EnemyDefinition def, GameObject template, bool isTemplate = false)
        {
            var items = new List<DMSyncItem>();
            if (root == null)
                return items;

            bool grenadesOn = def != null && def.canThrowGrenades;
            foreach (string n in PlayerOnlyTypes)
            {
                int c = Count(root, n);
                if (c > 0)
                    items.Add(Item("RemovePlayerOnly", $"Remove player-only {n} ({c}). It is a player singleton and can corrupt the player's progression."));
            }

            int gren = 0;
            foreach (string n in GrenadeTypes)
                gren += Count(root, n);
            if (gren > 0 && !grenadesOn)
                items.Add(Item("RemoveGrenades", $"Remove the grenade components ({gren}); they are allowed only when the definition's Can Throw Grenades is on."));
            else if (gren < GrenadeTypes.Length && grenadesOn && !isTemplate)
                items.Add(Item("AddGrenades", "Add the grenade components (the definition's Can Throw Grenades is on; no throw behaviour yet)."));

            if (root.GetComponent<PioneerShooterManager>() != null)
                items.Add(Item("SwapShooter", "Replace PioneerShooterManager (player shooter manager) with the plain vShooterManager enemies use; references are re-pointed."));

            int slots = FindPlayerOnlyChildren(root).Count;
            if (slots > 0)
                items.Add(Item("RemoveToolChildren", $"Remove {slots} player-only child object(s) (mining-tool slots / flashlight mesh)."));

            if (!isTemplate && template != null)
            {
                int missing = MissingSlots(root, template, out int unplaceable);
                if (missing > 0)
                    items.Add(new DMSyncItem { Id = "AddSlots", Auto = false, Text = $"{missing} weapon slot(s) the template has are missing (e.g. Blade_Of_Chaos Drawn_/Holstered_): add by hand if wanted (Fix / Fix All never write Drawn_/Holstered_ slots or holder placements on existing prefabs)." });
                if (unplaceable > 0)
                    items.Add(new DMSyncItem { Id = "SlotsManual", Auto = false, Text = $"{unplaceable} template weapon slot(s) have no matching holder bone on this rig: add by hand." });
            }

            var br = root.GetComponent<EnemyInvectorLoadoutBridge>();
            if (isTemplate && br != null && (br.MeleeOverride != null || br.RangedOverride != null))
                items.Add(Item("ClearTemplateOverrides", "Clear the weapon override fields baked on the template bridge (the definition supplies weapons)."));

            Animator anim = root.GetComponent<Animator>();
            if (anim != null && anim.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                items.Add(Item("Culling", "Set Animator culling to AlwaysAnimate."));

            var cb = root.GetComponent<EnemyInvectorCombatBridge>();
            if (cb != null)
            {
                var so = new SerializedObject(cb);
                if (IsNull(so, "enemyPistolAmmo") || IsNull(so, "enemyRifleAmmo"))
                    items.Add(Item("AmmoFx", "Fill the empty enemy pistol/rifle ammo (Standard / Plasma) so shots have ammo FX."));
            }

            if (anim != null && !anim.enabled)
                items.Add(Item("AnimatorEnabled", "Enable the root Animator (template value; the Bootstrap re-enables it at runtime anyway)."));

            if (!isTemplate && template != null)
            {
                int tune = AlignDrift(root, template, null, TuningFieldsList);
                if (tune > 0)
                    items.Add(Item("Tuning", $"Apply {tune} template tuning value(s) (AI standoff/stamina, ragdoll stagger/reaction, poise). Definition-driven fields are NOT touched."));
                int drift = AlignDrift(root, template, null);
                if (drift > 0)
                    items.Add(Item("AlignFields", $"Align {drift} stale Invector health/locomotion field(s) to the template (e.g. baked _isDead). Tuning values are NOT touched."));
                foreach (Type mt in MissingRootTypes(root, template))
                    if (mt != typeof(CombatPoise) && mt != typeof(DMEnemyHitboxRig) && mt != typeof(DMEnemyHitMarks) && mt != typeof(vShooterManager))
                        items.Add(Item("AddRoot:" + mt.Name, $"Add {mt.Name} (template root component)."));
            }

            if (root.GetComponent<CombatPoise>() == null)
                items.Add(Item("AddPoise", "Add CombatPoise (template component)."));

            if (root.GetComponent<DMEnemyHitboxRig>() == null)
                items.Add(Item("AddHitboxRig", "Add DMEnemyHitboxRig (per-bone hitboxes on layer DMHitbox are built from it at spawn)."));

            DMEnemyHitMarks marks = root.GetComponent<DMEnemyHitMarks>();
            if (marks == null)
                items.Add(Item("AddHitMarks", def != null
                    ? $"Add DMEnemyHitMarks with body type {def.bodyType} from the definition."
                    : "Add DMEnemyHitMarks (no definition: Humanoid default)."));
            else if (def != null && marks.BodyType != def.bodyType)
                items.Add(Item("HitMarksBodyType", $"Set DMEnemyHitMarks body type {marks.BodyType} -> {def.bodyType} (from the definition)."));

            if (LayerMask.NameToLayer(HitboxLayerName) < 0)
                items.Add(new DMSyncItem { Id = "HitboxLayerManual", Auto = false, Text = "Physics layer 'DMHitbox' (26) is not defined: run Combat -> Hit Marks -> 1. Apply Project Settings." });

            return items;
        }

        static DMSyncItem Item(string id, string text) => new DMSyncItem { Id = id, Text = text, Auto = true };

        // ------------------------------------------------------------------ apply

        /// <summary>Applies every auto item to loaded prefab contents. Returns the number of changes.</summary>
        public static int ApplyToContents(GameObject root, EnemyDefinition def, bool isTemplate, StringBuilder log)
        {
            int changes = 0;
            GameObject templateContents = null;
            try
            {
                bool grenadesOn = def != null && def.canThrowGrenades;
                foreach (string n in PlayerOnlyTypes)
                    changes += Remove(root, n, log);
                if (!grenadesOn)
                    foreach (string n in GrenadeTypes)
                        changes += Remove(root, n, log);

                if (root.GetComponent<PioneerShooterManager>() != null)
                    changes += SwapShooter(root, log);

                foreach (Transform t in FindPlayerOnlyChildren(root))
                {
                    if (t == null) continue;
                    log.AppendLine("Removed child " + t.name);
                    UnityEngine.Object.DestroyImmediate(t.gameObject);
                    changes++;
                }

                if (!isTemplate)
                {
                    templateContents = PrefabUtility.LoadPrefabContents(TemplatePath);
                    // 2026-10-12: never add / place weapon slots on an existing prefab (hand-set); only a NEW character gets template slots.
                    log.AppendLine("Weapon slots: skipped (existing prefab; slots and placements are hand-set).");
                }

                var br = root.GetComponent<EnemyInvectorLoadoutBridge>();
                if (isTemplate && br != null)
                {
                    var so = new SerializedObject(br);
                    foreach (string f in new[] { "meleeWeaponItem", "rangedWeaponItem" })
                    {
                        SerializedProperty p = so.FindProperty(f);
                        if (p != null && p.objectReferenceValue != null)
                        {
                            log.AppendLine("Cleared bridge override " + f);
                            p.objectReferenceValue = null;
                            changes++;
                        }
                    }

                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                Animator anim0 = root.GetComponent<Animator>();
                if (anim0 != null && !anim0.enabled)
                {
                    anim0.enabled = true;
                    log.AppendLine("Animator enabled");
                    changes++;
                }

                if (!isTemplate && templateContents != null)
                {
                    changes += AlignDrift(root, templateContents, log);
                    changes += AlignDrift(root, templateContents, log, TuningFieldsList);
                    foreach (Type mt in MissingRootTypes(root, templateContents))
                    {
                        if (mt == typeof(CombatPoise) || mt == typeof(DMEnemyHitboxRig) || mt == typeof(DMEnemyHitMarks) || mt == typeof(vShooterManager))
                            continue; // handled explicitly below / above
                        root.AddComponent(mt);
                        log.AppendLine("Added template root component " + mt.Name);
                        changes++;
                    }
                }

                Animator anim = root.GetComponent<Animator>();
                if (anim != null && anim.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                {
                    anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    log.AppendLine("Culling -> AlwaysAnimate");
                    changes++;
                }

                var cb = root.GetComponent<EnemyInvectorCombatBridge>();
                if (cb != null)
                {
                    var so = new SerializedObject(cb);
                    changes += FillAmmo(so, "enemyPistolAmmo", StandardAmmoPath, log);
                    changes += FillAmmo(so, "enemyRifleAmmo", PlasmaAmmoPath, log);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                if (root.GetComponent<CombatPoise>() == null)
                {
                    root.AddComponent<CombatPoise>();
                    log.AppendLine("Added CombatPoise");
                    changes++;
                }

                if (root.GetComponent<DMEnemyHitboxRig>() == null)
                {
                    root.AddComponent<DMEnemyHitboxRig>();
                    log.AppendLine("Added DMEnemyHitboxRig");
                    changes++;
                }

                DMEnemyHitMarks marks = root.GetComponent<DMEnemyHitMarks>();
                if (marks == null)
                {
                    marks = root.AddComponent<DMEnemyHitMarks>();
                    if (def != null) marks.BodyType = def.bodyType;
                    log.AppendLine("Added DMEnemyHitMarks (" + marks.BodyType + ")");
                    changes++;
                }
                else if (def != null && marks.BodyType != def.bodyType)
                {
                    log.AppendLine($"DMEnemyHitMarks body type {marks.BodyType} -> {def.bodyType}");
                    marks.BodyType = def.bodyType;
                    changes++;
                }

                if (def != null && def.canThrowGrenades)
                    changes += AddGrenades(root, log);
            }
            finally
            {
                if (templateContents != null)
                    PrefabUtility.UnloadPrefabContents(templateContents);
            }

            return changes;
        }

        /// <summary>Opens the prefab, applies the sync and saves in place (GUID kept). Caller backs up first.</summary>
        public static bool SyncPrefab(string prefabPath, EnemyDefinition def, bool isTemplate, out string message)
        {
            var log = new StringBuilder();
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                message = "Could not open " + prefabPath;
                return false;
            }

            try
            {
                int n = ApplyToContents(root, def, isTemplate, log);
                if (n > 0)
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                message = (n > 0 ? $"Template sync: {n} change(s).\n" : "Template sync: already in sync.\n") + log.ToString().TrimEnd();
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>Root MonoBehaviour types on the template that the enemy lacks (UNUSED-class and player-only are skipped).</summary>
        static List<Type> MissingRootTypes(GameObject root, GameObject template)
        {
            var missing = new List<Type>();
            foreach (Component c in template.GetComponents<Component>())
            {
                if (c == null) continue;
                Type t = c.GetType();
                string n = t.Name;
                if (Array.IndexOf(NeverAddRoot, n) >= 0 || IsUnusedPendingAudit(n) || IsGrenadeType(n) || Array.IndexOf(PlayerOnlyTypes, n) >= 0)
                    continue;
                if (root.GetComponent(t) == null && !(t == typeof(vShooterManager) && root.GetComponent<PioneerShooterManager>() != null))
                    missing.Add(t);
            }

            return missing;
        }

        /// <summary>Counts (and when log != null, applies) template-value alignment for the whitelisted non-tuning fields.</summary>
        static int AlignDrift(GameObject root, GameObject template, StringBuilder log, string[][] fields = null)
        {
            int n = 0;
            foreach (string[] pair in fields ?? AlignFieldsList)
            {
                Component ec = FindRootByName(root, pair[0]);
                Component tc = FindRootByName(template, pair[0]);
                if (ec == null || tc == null) continue;
                var eso = new SerializedObject(ec);
                var tso = new SerializedObject(tc);
                SerializedProperty ep = eso.FindProperty(pair[1]);
                SerializedProperty tp = tso.FindProperty(pair[1]);
                if (ep == null || tp == null || ep.propertyType != tp.propertyType) continue;
                if (SerializedProperty.DataEquals(ep, tp)) continue;
                n++;
                if (log != null)
                {
                    log.AppendLine($"Aligned {pair[0]}.{pair[1]}: {Show(ep)} -> {Show(tp)} (template)");
                    eso.CopyFromSerializedProperty(tp);
                    eso.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            return n;
        }

        static string Show(SerializedProperty p)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.Float: return p.floatValue.ToString("0.###");
                case SerializedPropertyType.Integer: return p.intValue.ToString();
                case SerializedPropertyType.Boolean: return p.boolValue.ToString();
                default: return p.propertyType.ToString();
            }
        }

        static Component FindRootByName(GameObject go, string typeName)
        {
            foreach (Component c in go.GetComponents<Component>())
                if (c != null && c.GetType().Name == typeName) return c;
            return null;
        }

        static bool IsNull(SerializedObject so, string field)
        {
            SerializedProperty p = so.FindProperty(field);
            return p != null && p.objectReferenceValue == null;
        }

        static int FillAmmo(SerializedObject so, string field, string path, StringBuilder log)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p == null || p.objectReferenceValue != null)
                return 0;
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item == null)
                return 0;
            p.objectReferenceValue = item;
            log.AppendLine($"{field} = {item.name}");
            return 1;
        }

        static int Count(GameObject root, string typeName)
        {
            int n = 0;
            foreach (Component c in root.GetComponentsInChildren<Component>(true))
                if (c != null && c.GetType().Name == typeName)
                    n++;
            return n;
        }

        static int Remove(GameObject root, string typeName, StringBuilder log)
        {
            int n = 0;
            foreach (Component c in root.GetComponentsInChildren<Component>(true))
            {
                if (c != null && c.GetType().Name == typeName)
                {
                    UnityEngine.Object.DestroyImmediate(c);
                    n++;
                }
            }

            if (n > 0)
                log.AppendLine($"Removed {typeName} x{n}");
            return n;
        }

        static int AddGrenades(GameObject root, StringBuilder log)
        {
            int n = 0;
            Type cook = FindType("DMIGrenadeCookController");
            Type thr = FindType("DMIGrenadeThrowBridge");
            foreach (Type t in new[] { cook, thr })
            {
                if (t != null && root.GetComponent(t) == null)
                {
                    root.AddComponent(t);
                    log.AppendLine("Added " + t.Name);
                    n++;
                }
            }

            return n;
        }

        static Type FindType(string name)
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                foreach (Type t in a.GetTypes())
                    if (t.Name == name && typeof(Component).IsAssignableFrom(t))
                        return t;
            return null;
        }

        static List<Transform> FindPlayerOnlyChildren(GameObject root)
        {
            var found = new List<Transform>();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == root.transform) continue;
                bool hit = false;
                foreach (string s in PlayerOnlySlotNames)
                    if (t.name.StartsWith("Drawn_" + s, StringComparison.Ordinal) || t.name.StartsWith("Holstered_" + s, StringComparison.Ordinal))
                        hit = true;
                foreach (string s in PlayerOnlyHolderMeshes)
                    if (t.name == s)
                        hit = true;
                if (hit)
                    found.Add(t);
            }

            // Drop children whose ancestor is also listed (destroying the parent removes them).
            found.RemoveAll(t => found.Exists(o => o != t && t.IsChildOf(o)));
            return found;
        }

        static int SwapShooter(GameObject root, StringBuilder log)
        {
            var old = root.GetComponent<PioneerShooterManager>();
            string json = EditorJsonUtility.ToJson(old);
            var created = root.AddComponent<vShooterManager>();
            EditorJsonUtility.FromJsonOverwrite(json, created);
            int repointed = 0;
            foreach (Component c in root.GetComponentsInChildren<Component>(true))
            {
                if (c == null || c == old || c == created) continue;
                var so = new SerializedObject(c);
                SerializedProperty it = so.GetIterator();
                bool dirty = false;
                while (it.Next(true))
                {
                    if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue == old)
                    {
                        it.objectReferenceValue = created;
                        repointed++;
                        dirty = true;
                    }
                }

                if (dirty)
                    so.ApplyModifiedPropertiesWithoutUndo();
            }

            UnityEngine.Object.DestroyImmediate(old);
            log.AppendLine($"PioneerShooterManager -> vShooterManager ({repointed} reference(s) re-pointed)");
            return 1;
        }

        // ---- weapon slots

        static bool IsSlotName(string n) => n.StartsWith("Drawn_", StringComparison.Ordinal) || n.StartsWith("Holstered_", StringComparison.Ordinal);

        static bool IsPlayerOnlySlot(string n)
        {
            foreach (string s in PlayerOnlySlotNames)
                if (n.EndsWith(s, StringComparison.Ordinal)) return true;
            return false;
        }

        static Transform FindByName(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        /// <summary>Parent in the enemy that matches the template slot's parent (via a sibling slot both rigs share).</summary>
        static Transform FindParentFor(Transform enemyRoot, Transform templateSlot)
        {
            Transform tParent = templateSlot.parent;
            for (int i = 0; i < tParent.childCount; i++)
            {
                Transform sib = tParent.GetChild(i);
                if (sib == templateSlot || !IsSlotName(sib.name)) continue;
                Transform match = FindByName(enemyRoot, sib.name);
                if (match != null) return match.parent;
            }

            return null;
        }

        static int MissingSlots(GameObject root, GameObject template, out int unplaceable)
        {
            int missing = 0;
            unplaceable = 0;
            foreach (Transform t in template.GetComponentsInChildren<Transform>(true))
            {
                if (!IsSlotName(t.name) || IsPlayerOnlySlot(t.name) || t.parent == null || IsSlotName(t.parent.name)) continue;
                if (FindByName(root.transform, t.name) != null) continue;
                if (FindParentFor(root.transform, t) != null) missing++;
                else unplaceable++;
            }

            return missing;
        }

        static int AddSlots(GameObject root, GameObject templateContents, StringBuilder log)
        {
            int added = 0;
            var todo = new List<Transform>();
            foreach (Transform t in templateContents.GetComponentsInChildren<Transform>(true))
                if (IsSlotName(t.name) && !IsPlayerOnlySlot(t.name) && t.parent != null && !IsSlotName(t.parent.name) && FindByName(root.transform, t.name) == null)
                    todo.Add(t);

            foreach (Transform t in todo)
            {
                Transform parent = FindParentFor(root.transform, t);
                if (parent == null)
                {
                    log.AppendLine("Slot " + t.name + ": no matching holder bone (manual)");
                    continue;
                }

                GameObject clone = UnityEngine.Object.Instantiate(t.gameObject, parent, false);
                clone.name = t.name;
                clone.transform.localPosition = t.localPosition;
                clone.transform.localRotation = t.localRotation;
                clone.transform.localScale = t.localScale;
                clone.SetActive(false);
                int stripped = StripUnused(clone);
                int nulled = NullForeignRefs(clone, root);
                log.AppendLine($"Added slot {t.name} under {parent.name} ({nulled} foreign ref(s) cleared, {stripped} UNUSED-class component(s) left out)");
                added++;
            }

            if (added > 0)
                DMWeaponPlacementBake.ApplyFromProfile(root, log); // baked placements win over the template slot transforms

            return added;
        }

        static int StripUnused(GameObject subtree)
        {
            int n = 0;
            foreach (Component c in subtree.GetComponentsInChildren<Component>(true))
            {
                if (c != null && IsUnusedPendingAudit(c.GetType().Name))
                {
                    UnityEngine.Object.DestroyImmediate(c);
                    n++;
                }
            }

            return n;
        }

        static int NullForeignRefs(GameObject subtree, GameObject root)
        {
            int n = 0;
            foreach (Component c in subtree.GetComponentsInChildren<Component>(true))
            {
                if (c == null) continue;
                var so = new SerializedObject(c);
                SerializedProperty it = so.GetIterator();
                bool dirty = false;
                while (it.Next(true))
                {
                    if (it.propertyType != SerializedPropertyType.ObjectReference || it.objectReferenceValue == null) continue;
                    UnityEngine.Object v = it.objectReferenceValue;
                    Transform vt = v is Component vc ? vc.transform : (v is GameObject vg ? vg.transform : null);
                    if (vt != null && !vt.IsChildOf(root.transform))
                    {
                        it.objectReferenceValue = null;
                        n++;
                        dirty = true;
                    }
                }

                if (dirty)
                    so.ApplyModifiedPropertiesWithoutUndo();
            }

            return n;
        }
    }
}
#endif
