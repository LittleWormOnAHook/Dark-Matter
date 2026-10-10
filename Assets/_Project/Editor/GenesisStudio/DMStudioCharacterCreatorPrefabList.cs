#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Project.AI;
using Project.AI.Invector;
using Project.Data;
using Project.EditorTools.Invector;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.GenesisStudio
{
    /// <summary>
    /// Genesis Studio Character Creator: clickable list of existing creator-compatible prefabs (enemy or player)
    /// with a stats / status panel, inline definition editing and safe Rebuild / Re-apply actions.
    /// No per-prefab logic: everything is read from the prefab's linked EnemyDefinition / PlayerVisualDefinition and
    /// runs through the existing creator utilities (backup, in-place rebuild, protection, post-build check).
    /// </summary>
    public sealed class DMStudioCharacterCreatorPrefabList
    {
        public enum ListKind { Enemy, Player }

        enum RowStatus { Ok, Warning, Error }

        sealed class Row
        {
            public string Path;
            public string Name;
            public GameObject Asset;
            public bool Protected;
            public EnemyDefinition EnemyDef;
            public PlayerVisualDefinition PlayerDef;
            public EnemyDefinition CandidateDef;
            public string KindLabel = "-";
            public string DefLabel = "none";
            public RowStatus Status;
            public string StatusText = "";
            public DMCharacterCreatorBuildReport Report;
            public float HandDistance = -1f;
            public string BakedCapsule = "-";
            public string RootCapsule = "-";
            public DMCreatorFixPlan Plan;
            public string BeforeAfter;
        }

        const string IdleFbx =
            "Assets/Invector-3rdPersonController/Basic Locomotion/3D Models/Animations/Basic_FreeMovement.fbx";
        const float CrossedArmHandDistance = 0.35f;
        const float LabelWidth = 200f;

        static AnimationClip idleClip;

        readonly ListKind kind;
        readonly List<Row> rows = new List<Row>();
        bool loaded;
        bool fold = true;
        string filter = string.Empty;
        Vector2 listScroll;
        Vector2 detailScroll;
        string selectedPath;
        bool foldEdit;
        bool foldValues;
        EnemyDefinition workingDef;
        bool workingDirty;
        string dryRunText;
        string lastMessage;
        EnemyDefinition linkCandidate;
        Action pending;

        public DMStudioCharacterCreatorPrefabList(ListKind kind)
        {
            this.kind = kind;
        }

        // ------------------------------------------------------------------ Draw

        public void Draw()
        {
            DMStudioStyles.DrawInspectorSectionHeader(kind == ListKind.Enemy ? "Enemy Prefab List" : "Player Prefab List");
            fold = DMStudioStyles.DrawInspectorFoldout(fold, kind == ListKind.Enemy
                ? "Existing enemy prefabs (EnemyInvectorBootstrap)"
                : "Existing player prefabs (Invector pipeline)");

            if (fold)
            {
                if (!loaded && Event.current.type == EventType.Layout)
                    Refresh();

                DrawToolbar();
                DrawList();
                Row sel = FindSelected();
                if (sel != null)
                    DrawDetail(sel);
                else
                    DMCharacterCreatorSharedUi.DrawWrappedHelpBox(
                        "Click a prefab to see its definition values, status and rebuild options.", MessageType.None);
            }

            EditorGUILayout.Space(10f);

            // Run queued actions only after every layout scope above has closed (balanced groups), then abort the GUI pass.
            if (pending != null && Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint)
            {
                Action run = pending;
                pending = null;
                run();
                GUIUtility.ExitGUI();
            }
        }

        void Queue(Action action)
        {
            pending = action;
            GUI.changed = true;
        }

        void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh", GUILayout.Width(70f), GUILayout.Height(20f)))
                    Queue(Refresh);
                filter = EditorGUILayout.TextField(filter, GUILayout.Height(20f));
                if (kind == ListKind.Enemy)
                {
                    int fixable = CountFixable(Visible());
                    using (new EditorGUI.DisabledScope(fixable == 0))
                    {
                        if (GUILayout.Button($"Fix All ({fixable})", GUILayout.Width(86f), GUILayout.Height(20f)))
                            Queue(DoFixAll);
                    }
                }
                GUILayout.Label(Visible().Count + "/" + rows.Count, GUILayout.Width(48f));
            }
        }

        void DrawList()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Prefab", EditorStyles.miniBoldLabel, GUILayout.ExpandWidth(true));
                GUILayout.Label(kind == ListKind.Enemy ? "Kind / body" : "Type", EditorStyles.miniBoldLabel, GUILayout.Width(86f));
                GUILayout.Label("Definition", EditorStyles.miniBoldLabel, GUILayout.Width(150f));
                GUILayout.Label("Check", EditorStyles.miniBoldLabel, GUILayout.Width(74f));
                if (kind == ListKind.Enemy)
                    GUILayout.Label("Fix", EditorStyles.miniBoldLabel, GUILayout.Width(46f));
            }

            using (var sv = new EditorGUILayout.ScrollViewScope(listScroll, GUILayout.Height(190f)))
            {
                listScroll = sv.scrollPosition;
                List<Row> visible = Visible();
                for (int i = 0; i < visible.Count; i++)
                {
                    Row row = visible[i];
                    bool isSel = row.Path == selectedPath;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button(
                                row.Name,
                                isSel ? DMStudioStyles.ListButtonSelected : DMStudioStyles.ListButton,
                                GUILayout.Height(22f),
                                GUILayout.ExpandWidth(true)))
                        {
                            Select(row);
                        }

                        GUILayout.Label(row.KindLabel, GUILayout.Width(86f));
                        GUILayout.Label(row.DefLabel, GUILayout.Width(150f));
                        DMStudioStyles.DrawBadge(row.StatusText, StatusColor(row.Status), 70f);
                        if (kind == ListKind.Enemy)
                        {
                            bool hasFix = row.Plan != null && row.Plan.HasAuto && !row.Protected;
                            using (new EditorGUI.DisabledScope(!hasFix))
                            {
                                var fixContent = new GUIContent("Fix", hasFix ? string.Join("\n", row.Plan.Auto) : "Nothing auto-fixable");
                                if (GUILayout.Button(fixContent, GUILayout.Width(46f), GUILayout.Height(22f)))
                                {
                                    Row captured = row;
                                    Select(captured);
                                    Queue(() => DoFix(captured));
                                }
                            }
                        }
                    }
                }

                if (visible.Count == 0)
                    GUILayout.Label(loaded ? "No matching prefabs." : "Loading...", EditorStyles.miniLabel);
            }
        }

        void DrawDetail(Row row)
        {
            EditorGUILayout.Space(6f);
            DMStudioStyles.DrawPingSelectHeader(row.Name, row.Path, row.Asset);

            if (row.Protected)
                DMCharacterCreatorSharedUi.DrawWrappedHelpBox(
                    "Protected template (Player_v7*, Player_Invector*, HumanoidEnemy_Invector): listed for reference; Rebuild / Re-apply are blocked.",
                    MessageType.Info);

            using (var sv = new EditorGUILayout.ScrollViewScope(detailScroll, GUILayout.MaxHeight(620f)))
            {
                detailScroll = sv.scrollPosition;
                DrawStatusBlock(row);
                DrawFixBlock(row);
                if (kind == ListKind.Enemy)
                    DrawEnemyStats(row);
                else
                    DrawPlayerStats(row);
                DrawActions(row);
            }
        }

        void DrawStatusBlock(Row row)
        {
            DMStudioStyles.DrawInspectorSectionHeader("Post-build check");
            if (row.Report == null)
            {
                DMCharacterCreatorSharedUi.DrawWrappedHelpBox("Not checked yet. Press Refresh.", MessageType.None);
                return;
            }

            for (int i = 0; i < row.Report.Errors.Count; i++)
                DMCharacterCreatorSharedUi.DrawWrappedHelpBox(row.Report.Errors[i], MessageType.Error);
            for (int i = 0; i < row.Report.Warnings.Count; i++)
                DMCharacterCreatorSharedUi.DrawWrappedHelpBox(row.Report.Warnings[i], MessageType.Warning);
            if (row.Report.Errors.Count == 0 && row.Report.Warnings.Count == 0)
                DMCharacterCreatorSharedUi.DrawWrappedHelpBox("All checks passed.", MessageType.None);

            if (row.HandDistance >= 0f)
                DMCharacterCreatorSharedUi.DrawWrappedHelpBox(
                    $"Idle hand distance {row.HandDistance:0.00} (relaxed about 0.55 to 0.60; under {CrossedArmHandDistance:0.00} means crossed arms).",
                    MessageType.None);
        }

        void DrawFixBlock(Row row)
        {
            if (kind != ListKind.Enemy || row.Plan == null)
                return;

            if (row.Plan.HasAuto && !row.Protected)
            {
                DMStudioStyles.DrawInspectorSectionHeader("Auto-fixable (non-destructive, GUID + object ids kept)");
                for (int i = 0; i < row.Plan.Auto.Count; i++)
                    DMCharacterCreatorSharedUi.DrawWrappedHelpBox(row.Plan.Auto[i], MessageType.None);
                if (GUILayout.Button("Fix this prefab", GUILayout.Height(24f)))
                {
                    Row captured = row;
                    Queue(() => DoFix(captured));
                }
            }

            if (row.Plan.Manual.Count > 0)
            {
                DMStudioStyles.DrawInspectorSectionHeader("Needs manual action");
                for (int i = 0; i < row.Plan.Manual.Count; i++)
                    DMCharacterCreatorSharedUi.DrawWrappedHelpBox(row.Plan.Manual[i], MessageType.Warning);
            }

            if (!string.IsNullOrEmpty(row.BeforeAfter))
                DMCharacterCreatorSharedUi.DrawWrappedHelpBox(row.BeforeAfter, MessageType.Info);
        }

        // ------------------------------------------------------------------ Fix

        static int CountFixable(List<Row> list)
        {
            int n = 0;
            for (int i = 0; i < list.Count; i++)
                if (list[i].Plan != null && list[i].Plan.HasAuto && !list[i].Protected)
                    n++;
            return n;
        }

        static HashSet<string> ReadObjectIds(string assetPath)
        {
            var ids = new HashSet<string>();
            try
            {
                string full = Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
                foreach (Match m in Regex.Matches(File.ReadAllText(full), @"(?m)^--- !u!\d+ &(-?\d+)"))
                    ids.Add(m.Groups[1].Value);
            }
            catch (IOException) { }
            return ids;
        }

        static string IssueSummary(Row row)
        {
            if (row.Report == null)
                return "unchecked";
            var sb = new StringBuilder(row.StatusText);
            int shown = 0;
            foreach (string e in row.Report.Errors)
                if (shown++ < 4) sb.Append(" | ").Append(Short(e));
            foreach (string w in row.Report.Warnings)
                if (shown++ < 4) sb.Append(" | ").Append(Short(w));
            return sb.ToString();
        }

        static string Short(string s) => s.Length > 90 ? s.Substring(0, 90) + "..." : s;

        /// <summary>Backup, apply the row's auto fixes in place, verify ids, re-run the check, record before/after.</summary>
        bool FixRow(Row row, out string summary)
        {
            summary = null;
            if (row.Plan == null || !row.Plan.HasAuto || row.Protected)
                return false;

            EnemyDefinition def = row.EnemyDef ?? row.CandidateDef;
            string before = IssueSummary(row);
            string backup = DMHumanoidVisualRebuildUtility.BackupExistingOutput(row.Path);
            string guidBefore = AssetDatabase.AssetPathToGUID(row.Path);
            HashSet<string> idsBefore = ReadObjectIds(row.Path);
            bool ok = DMCharacterCreatorFixer.Apply(row.Path, def, row.Plan.Fixes, out string msg);
            AssetDatabase.SaveAssets();
            HashSet<string> idsAfter = ReadObjectIds(row.Path);
            int lost = 0;
            foreach (string id in idsBefore)
                if (!idsAfter.Contains(id))
                    lost++;
            bool guidSame = guidBefore == AssetDatabase.AssetPathToGUID(row.Path);

            DMCreatorFixPlan planBefore = row.Plan;
            RefreshRow(row);
            string integrity = (guidSame ? "GUID kept" : "GUID CHANGED") + $", {lost} object id(s) lost" +
                               (lost > 0 || !guidSame ? $" - RESTORE FROM BACKUP {backup}" : string.Empty);
            row.BeforeAfter = $"{(ok ? "Fixed" : "Fix had errors")} ({integrity}). Backup: {backup}\nBEFORE: {before}\nAFTER: {IssueSummary(row)}\n{msg}";
            summary = $"{row.Name}: {(ok ? "fixed" : "ERRORS")} ({before.Split('|')[0].Trim()} -> {row.StatusText}); {integrity}";
            return ok;
        }

        static string DescribeFixes(Row row)
        {
            var sb = new StringBuilder();
            sb.AppendLine(row.Name + ":");
            for (int i = 0; i < row.Plan.Auto.Count; i++)
                sb.AppendLine("  - " + row.Plan.Auto[i]);
            return sb.ToString();
        }

        void DoFix(Row row)
        {
            if (row.Plan == null || !row.Plan.HasAuto || row.Protected)
                return;

            string message = "Apply these fixes in place to '" + row.Path + "'?\n\n" + DescribeFixes(row) +
                             "\nBack up to Library/DMCreatorBackups first; GUID and object ids are kept; the check is re-run afterwards." +
                             (row.Plan.Manual.Count > 0 ? $"\n\n{row.Plan.Manual.Count} issue(s) will remain (manual / rebuild)." : string.Empty);
            if (!EditorUtility.DisplayDialog("Fix prefab", message, "Fix", "Cancel"))
                return;

            FixRow(row, out string summary);
            lastMessage = summary;
        }

        void DoFixAll()
        {
            var targets = new List<Row>();
            foreach (Row r in Visible())
                if (r.Plan != null && r.Plan.HasAuto && !r.Protected)
                    targets.Add(r);
            if (targets.Count == 0)
                return;

            var sb = new StringBuilder();
            int manual = 0;
            foreach (Row r in targets)
            {
                sb.Append(DescribeFixes(r));
                manual += r.Plan.Manual.Count;
            }

            string message = $"Fix {targets.Count} prefab(s) in place?\n\n{sb}\nEach prefab is backed up to Library/DMCreatorBackups first; GUIDs and object ids are kept; every prefab is re-checked afterwards." +
                             (manual > 0 ? $"\n\n{manual} manual issue(s) will remain (rebuilds are never run by Fix All)." : string.Empty);
            if (!EditorUtility.DisplayDialog("Fix All", message, $"Fix {targets.Count}", "Cancel"))
                return;

            var report = new StringBuilder();
            foreach (Row r in targets)
            {
                FixRow(r, out string summary);
                report.AppendLine(summary);
            }

            lastMessage = "Fix All:\n" + report.ToString().TrimEnd();
        }

        // ------------------------------------------------------------------ Enemy stats

        void DrawEnemyStats(Row row)
        {
            EnemyDefinition def = row.EnemyDef ?? row.CandidateDef;
            DMStudioStyles.DrawInspectorSectionHeader("Definition");
            if (def == null)
            {
                DMCharacterCreatorSharedUi.DrawWrappedHelpBox(
                    "No EnemyDefinition is linked to this prefab. Pick one below and press Link Definition (in place, GUID kept).",
                    MessageType.Warning);
                using (DMStudioStyles.BeginProfileInspector(LabelWidth))
                    linkCandidate = (EnemyDefinition)EditorGUILayout.ObjectField(
                        "Definition to link", linkCandidate, typeof(EnemyDefinition), false);
                using (new EditorGUI.DisabledScope(linkCandidate == null || row.Protected))
                {
                    if (GUILayout.Button("Link Definition", GUILayout.Height(24f)))
                        Queue(() => DoLink(row));
                }

                return;
            }

            if (row.EnemyDef == null)
                DMCharacterCreatorSharedUi.DrawWrappedHelpBox(
                    $"Not linked on the prefab; showing the best match '{def.name}'. Re-apply Definition will link it.",
                    MessageType.Warning);

            DMStudioStyles.DrawPingSelectHeader(def.name, AssetDatabase.GetAssetPath(def), def);

            using (DMStudioStyles.BeginProfileInspector(LabelWidth))
            {
                Stat("Body type / category / threat", $"{def.bodyType} / {def.enemyCategory} / {def.surfaceThreatKind}");
                Stat("Health / XP", $"{def.maxHealth:0.#} HP / {def.xpReward} XP");
                Stat("Melee damage / range / cooldown", $"{def.attackDamage:0.#} / {def.attackRange:0.##} m / {def.attackCooldown:0.##} s");
                Stat("Ranged engage / cooldown", $"{def.rangedEngageRange:0.#} m / {def.rangedAttackCooldown:0.##} s (miss {def.missRate:0.##})");
                float chase = def.chaseSpeed > 0f ? def.chaseSpeed : def.runSpeed * def.chaseSpeedMultiplier;
                Stat("Walk / run / chase speed", $"{def.walkSpeed:0.##} / {def.runSpeed:0.##} / {chase:0.##}");
                Stat("Vision / hearing / proximity", $"{def.visionRange:0.#} m @ {def.visionFov:0}deg / {def.hearingRange:0.#} m / {def.proximityRange:0.#} m");
                Stat("Weapons", $"melee: {Nm(def.meleeWeaponItem)}, ranged: {Nm(def.rangedWeaponItem)}, prefer ranged: {def.preferRangedWeapon}");
                Stat("Brain", def.overrideBrain
                    ? $"override: {def.brainArchetype} / {def.primaryPersonality} / {def.secondaryPersonality}"
                    : "profile default (no override)");
                Stat("Profile overrides", $"brain: {Nm(def.brainProfileOverride)}, engagement: {Nm(def.engagementProfileOverride)}, hit mark: {Nm(def.hitMarkProfileOverride)}");
                Stat("Loot", def.enableLoot
                    ? $"AC {def.acDropMin}-{def.acDropMax}, random {def.randomLootCountMin}-{def.randomLootCountMax}, pool {(def.lootItemPool != null ? def.lootItemPool.Length : 0)}, bag {Nm(def.lootBagPrefab)}"
                    : "disabled");
                Stat("Capsule (definition)", $"r {def.colliderRadius:0.###} / h {def.colliderHeight:0.###} / center {def.colliderCenter} / fit {def.fitColliderToRenderers}");
                Stat("Capsule (baked fallback / root collider)", $"{row.BakedCapsule} / {row.RootCapsule}");
                Stat("Template / last model", $"{Nm(def.templatePrefab)} / {Nm(def.lastModelSource)}");
            }

            foldValues = DMStudioStyles.DrawInspectorFoldout(foldValues, "Live-linked vs baked values");
            if (foldValues)
            {
                DMCharacterCreatorSharedUi.DrawWrappedHelpBox(
                    "LIVE (read from the definition on every spawn; edit the definition and it applies to every enemy built from it): stats, health, senses, melee/ranged/AI timings, movement, loot + bag, health bar, body type, brain archetype/personality + profile overrides, XP, weapon items, hit capsule.",
                    MessageType.None);
                DMCharacterCreatorSharedUi.DrawWrappedHelpBox(
                    "BAKED into the prefab (press Re-apply Definition after editing): Drawn_/Holstered_ weapon slot visuals + active slot, Bootstrap capsule fallback fields, root CapsuleCollider, visual model + avatar, animator controller + culling.",
                    MessageType.None);
            }

            DrawInlineEdit(row, def);
        }

        void DrawInlineEdit(Row row, EnemyDefinition def)
        {
            if (row.EnemyDef == null)
                return;

            foldEdit = DMStudioStyles.DrawInspectorFoldout(foldEdit, "Edit definition (saved through the saved-definition helper)");
            if (!foldEdit)
                return;

            if (workingDef == null || workingDef.name != def.name)
                BeginEdit(def);

            EditorGUI.BeginChangeCheck();
            using (DMStudioStyles.BeginProfileInspector(LabelWidth))
            {
                EnemyPrefabCreatorPanel.DrawKindAndBrain(workingDef);
                EditorGUILayout.Space(4f);
                EnemyPrefabCreatorPanel.DrawHumanoidWeapons(workingDef);
                EditorGUILayout.Space(4f);
                EnemyPrefabCreatorPanel.DrawHealth(workingDef);
                EditorGUILayout.Space(4f);
                EnemyPrefabCreatorPanel.DrawHealthBar(workingDef);
                EditorGUILayout.Space(4f);
                EnemyPrefabCreatorPanel.DrawSenses(workingDef);
                EditorGUILayout.Space(4f);
                EnemyPrefabCreatorPanel.DrawCombatStats(workingDef);
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Hit capsule", EditorStyles.boldLabel);
                workingDef.colliderRadius = EditorGUILayout.FloatField("Radius", workingDef.colliderRadius);
                workingDef.colliderHeight = EditorGUILayout.FloatField("Height", workingDef.colliderHeight);
                workingDef.colliderCenter = EditorGUILayout.Vector3Field("Center", workingDef.colliderCenter);
                workingDef.fitColliderToRenderers = EditorGUILayout.Toggle("Fit to renderers", workingDef.fitColliderToRenderers);
                EditorGUILayout.Space(4f);
                var ctx = new EnemyPrefabCreatorPanelContext
                {
                    Definition = workingDef,
                    DefinitionAssetFileName = workingDef.name,
                    ShowDefinitionAssetName = false,
                    ShowArchetype = false,
                    ApplyLoot = () => EnemyPrefabCreatorPanel.ApplyLootToExistingPrefab(workingDef)
                };
                EnemyPrefabCreatorPanel.DrawLoot(ctx);
            }

            if (EditorGUI.EndChangeCheck())
                workingDirty = true;

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!workingDirty))
                {
                    if (GUILayout.Button("Save Definition", GUILayout.Height(24f)))
                        Queue(() => DoSaveEdit(row));
                    if (GUILayout.Button("Revert", GUILayout.Height(24f)))
                        Queue(() => BeginEdit(def));
                }
            }

            if (workingDirty)
                DMCharacterCreatorSharedUi.DrawWrappedHelpBox("Unsaved edits. Save, then Re-apply Definition to refresh baked values.", MessageType.Warning);
        }

        void BeginEdit(EnemyDefinition def)
        {
            if (workingDef != null)
                UnityEngine.Object.DestroyImmediate(workingDef);
            workingDef = UnityEngine.Object.Instantiate(def);
            workingDef.name = def.name;
            workingDirty = false;
        }

        void DoSaveEdit(Row row)
        {
            EnemyDefinition w = workingDef;
            if (w == null)
                return;

            if (EnemyPrefabCreatorPanel.SaveDefinitionAsset(ref w, w.name))
            {
                BeginEdit(w);
                RefreshRow(row);
                lastMessage = $"Saved definition '{w.name}'. Baked values (weapon slots, capsule fallback) need Re-apply Definition.";
            }
        }

        // ------------------------------------------------------------------ Player stats

        void DrawPlayerStats(Row row)
        {
            DMStudioStyles.DrawInspectorSectionHeader("Definition");
            PlayerVisualDefinition def = row.PlayerDef;
            if (def == null)
            {
                DMCharacterCreatorSharedUi.DrawWrappedHelpBox(
                    "No PlayerVisualDefinition points at this prefab (not creator-built, or built before the definition was saved). Rebuild is unavailable until one exists (Player Prefab form below: Save Definition).",
                    MessageType.Info);
                return;
            }

            DMStudioStyles.DrawPingSelectHeader(def.name, AssetDatabase.GetAssetPath(def), def);
            using (DMStudioStyles.BeginProfileInspector(LabelWidth))
            {
                Stat("Display name / prefab file", $"{def.displayName} / {def.prefabFileName}");
                Stat("Template", Nm(def.templatePrefab));
                Stat("Visual child / last model", $"{def.visualChildName} / {Nm(def.lastModelSource)}");
                if (!string.IsNullOrEmpty(def.notes))
                    Stat("Notes", def.notes);
                Stat("Gameplay stats", "inherited from the Player_Invector template (none stored on the definition)");
            }
        }

        // ------------------------------------------------------------------ Actions

        void DrawActions(Row row)
        {
            DMStudioStyles.DrawInspectorSectionHeader("Actions");
            bool blocked = row.Protected;
            using (new EditorGUI.DisabledScope(blocked))
            {
                DMCharacterCreatorSharedUi.DrawToolbarButtonsWrapped(
                    26f,
                    ("Dry Run (check only)", () => Queue(() => DoDryRun(row))),
                    (kind == ListKind.Enemy ? "Re-apply Definition" : "Repair Visual (re-bind holders)", () => Queue(() => DoReapply(row))),
                    ("Rebuild From Template + Apply Visual", () => Queue(() => DoRebuild(row))));
            }

            DMCharacterCreatorSharedUi.DrawWrappedHelpBox(
                "Rebuild replaces the prefab IN PLACE (GUID kept, old file backed up to Library/DMCreatorBackups). Scene instances keep working, but hand-placed children or overrides parented to bones of the OLD visual (for example a weapon placed under a hand bone in a scene) lose their parent and can be orphaned. You get the instance count before confirming.",
                MessageType.Warning);

            if (!string.IsNullOrEmpty(dryRunText))
                DMCharacterCreatorSharedUi.DrawWrappedHelpBox(dryRunText, MessageType.None);
            if (!string.IsNullOrEmpty(lastMessage))
                DMCharacterCreatorSharedUi.DrawWrappedHelpBox(lastMessage, MessageType.Info);
        }

        void DoDryRun(Row row)
        {
            RefreshRow(row);
            string pre = Preflight(row, out _);
            SceneUsage usage = CountSceneUsage(row.Path);
            dryRunText = "DRY RUN (nothing was changed)\n" + pre + "\n" + usage.Format();
            lastMessage = null;
        }

        void DoLink(Row row)
        {
            if (linkCandidate == null || row.Protected)
                return;
            if (!EditorUtility.DisplayDialog(
                    "Link Definition",
                    $"Link '{linkCandidate.name}' to '{row.Path}' in place (GUID kept)? The old file is backed up first.",
                    "Link",
                    "Cancel"))
                return;

            string backup = DMHumanoidVisualRebuildUtility.BackupExistingOutput(row.Path);
            lastMessage = DMCharacterCreatorRepair.RelinkDefinition(row.Path, linkCandidate, out string msg)
                ? msg + (backup != null ? $" Backup: {backup}" : string.Empty)
                : "Link failed: " + msg;
            AssetDatabase.SaveAssets();
            RefreshRow(row);
        }

        void DoReapply(Row row)
        {
            if (row.Protected)
                return;

            dryRunText = null;
            if (kind == ListKind.Enemy)
            {
                EnemyDefinition def = row.EnemyDef ?? row.CandidateDef;
                if (def == null)
                {
                    lastMessage = "Link a definition first.";
                    return;
                }

                var blockers = DMCharacterCreatorActionValidation.CollectEnemySaveBlockers(def, DMCharacterCreatorDefinitionLink.FileNameFor(def));
                if (blockers.Count > 0)
                {
                    EditorUtility.DisplayDialog("Re-apply Definition", "The definition has problems:\n\n- " + string.Join("\n- ", blockers), "OK");
                    return;
                }

                if (!EditorUtility.DisplayDialog(
                        "Re-apply Definition",
                        $"Re-apply '{def.name}' to '{row.Path}' in place? Bakes the weapon slots, capsule fallback, avatar fix and definition link. GUID kept; backup to Library/DMCreatorBackups first. No mesh change.",
                        "Re-apply",
                        "Cancel"))
                    return;

                string backup = DMHumanoidVisualRebuildUtility.BackupExistingOutput(row.Path);
                bool ok = EnemyPrefabVisualSetupUtility.RepairVisualAtPath(row.Path, def);
                AssetDatabase.SaveAssets();
                lastMessage = (ok ? "Re-applied the definition." : "Re-apply failed (see Console).") +
                              (backup != null ? $" Backup: {backup}" : string.Empty);
                DMCharacterCreatorActionValidation.ShowBuildReportIfNeeded(DMCharacterCreatorActionValidation.EnemyDialogTitle);
            }
            else
            {
                if (!EditorUtility.DisplayDialog(
                        "Repair Visual",
                        $"Re-bind holders / weapon visuals on '{row.Path}' in place? Backup to Library/DMCreatorBackups first. No mesh change.",
                        "Repair",
                        "Cancel"))
                    return;

                string backup = DMHumanoidVisualRebuildUtility.BackupExistingOutput(row.Path);
                bool ok = PlayerPrefabVisualSetupUtility.RepairVisualAtPath(row.Path);
                AssetDatabase.SaveAssets();
                lastMessage = (ok ? "Repaired the player visual." : "Repair failed (see Console).") +
                              (backup != null ? $" Backup: {backup}" : string.Empty);
            }

            RefreshRow(row);
        }

        void DoRebuild(Row row)
        {
            if (row.Protected)
                return;

            dryRunText = null;
            string pre = Preflight(row, out Prepared prepared);
            if (prepared == null)
            {
                EditorUtility.DisplayDialog(DMHumanoidVisualRebuildUtility.RebuildDialogTitle, "Cannot rebuild:\n\n" + pre, "OK");
                lastMessage = "Rebuild blocked: " + pre;
                return;
            }

            SceneUsage usage = CountSceneUsage(row.Path);
            string message =
                $"Rebuild '{row.Path}' IN PLACE from the template '{Nm(prepared.Template)}' with visual '{Nm(prepared.Model)}'?\n\n" +
                "GUID and scene instances are kept; manual tweaks on the prefab are reset to the template; the old file is backed up to Library/DMCreatorBackups.\n\n" +
                usage.Format() +
                (usage.HandPlacedLoaded > 0 || usage.AddedInFiles > 0
                    ? "\n\nWARNING: scene instances have hand-placed children / added objects. Those parented to bones of the OLD visual may be orphaned."
                    : string.Empty);
            if (!EditorUtility.DisplayDialog(DMHumanoidVisualRebuildUtility.RebuildDialogTitle, message, "Rebuild", "Cancel"))
                return;

            string backup = DMHumanoidVisualRebuildUtility.BackupExistingOutput(row.Path);
            GameObject made;
            if (kind == ListKind.Enemy)
            {
                made = DMHumanoidVisualRebuildUtility.RebuildEnemyFromTemplate(
                    row.Path,
                    prepared.Model,
                    prepared.VisualChild,
                    prepared.Template,
                    prepared.EnemyDef,
                    autoPrepareImport: false,
                    allowForceHumanoid: false);
            }
            else
            {
                made = DMHumanoidVisualRebuildUtility.RebuildPlayerFromTemplate(
                    row.Path,
                    prepared.Model,
                    prepared.VisualChild,
                    prepared.Template,
                    autoPrepareImport: false,
                    allowForceHumanoid: false);
            }

            AssetDatabase.SaveAssets();
            lastMessage = (made != null ? "Rebuilt in place." : "Rebuild failed (see Console).") +
                          (backup != null ? $" Backup: {backup}" : string.Empty);
            if (made != null && kind == ListKind.Enemy)
                DMCharacterCreatorActionValidation.ShowBuildReportIfNeeded(DMCharacterCreatorActionValidation.EnemyDialogTitle);
            RefreshRow(row);
        }

        sealed class Prepared
        {
            public GameObject Model;
            public GameObject Template;
            public string VisualChild;
            public EnemyDefinition EnemyDef;
        }

        /// <summary>Everything a rebuild needs, validated. prepared == null means the rebuild is blocked.</summary>
        string Preflight(Row row, out Prepared prepared)
        {
            prepared = null;
            var sb = new StringBuilder();
            if (row.Protected)
            {
                sb.AppendLine("BLOCKED: protected template.");
                return sb.ToString().TrimEnd();
            }

            GameObject model;
            GameObject template;
            string visualChild;
            EnemyDefinition edef = null;
            if (kind == ListKind.Enemy)
            {
                edef = row.EnemyDef;
                if (edef == null)
                {
                    sb.AppendLine("BLOCKED: no linked EnemyDefinition (link one first).");
                    return sb.ToString().TrimEnd();
                }

                var blockers = DMCharacterCreatorActionValidation.CollectEnemySaveBlockers(edef, DMCharacterCreatorDefinitionLink.FileNameFor(edef));
                for (int i = 0; i < blockers.Count; i++)
                    sb.AppendLine("BLOCKED (definition): " + blockers[i]);

                visualChild = string.IsNullOrEmpty(edef.visualChildName) ? "Visual" : edef.visualChildName;
                model = edef.lastModelSource != null ? edef.lastModelSource : ResolveCurrentVisual(row.Asset, visualChild);
                template = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabVisualSetupUtility.ResolveTemplatePath(edef.templatePrefab));
                if (blockers.Count > 0)
                    return sb.ToString().TrimEnd();
            }
            else
            {
                PlayerVisualDefinition pdef = row.PlayerDef;
                if (pdef == null)
                {
                    sb.AppendLine("BLOCKED: no PlayerVisualDefinition for this prefab.");
                    return sb.ToString().TrimEnd();
                }

                visualChild = string.IsNullOrEmpty(pdef.visualChildName) ? "Visual" : pdef.visualChildName;
                model = pdef.lastModelSource != null ? pdef.lastModelSource : ResolveCurrentVisual(row.Asset, visualChild);
                template = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabVisualSetupUtility.ResolveTemplatePath(pdef.templatePrefab));
            }

            if (model == null)
            {
                sb.AppendLine("BLOCKED: no visual model source (definition has no Last Model Source and the current visual is not an FBX/prefab instance).");
                return sb.ToString().TrimEnd();
            }

            if (template == null)
            {
                sb.AppendLine("BLOCKED: template prefab not found.");
                return sb.ToString().TrimEnd();
            }

            if (!EnemyModelAvatarUtility.IsReadyForHumanoidPaste(EnemyModelAvatarUtility.ResolvePreferredVisualModel(model)))
            {
                sb.AppendLine($"BLOCKED: model '{model.name}' is not Humanoid-ready. Prepare it in the creator form (Prepare Model Import) first; the list never changes importers.");
                return sb.ToString().TrimEnd();
            }

            string templatePath = AssetDatabase.GetAssetPath(template);
            if (string.Equals(templatePath, row.Path, StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine("BLOCKED: output equals the template.");
                return sb.ToString().TrimEnd();
            }

            sb.AppendLine($"Ready: model '{model.name}', template '{template.name}', visual child '{visualChild}'.");
            sb.AppendLine("In place (GUID kept); backup to Library/DMCreatorBackups first.");
            prepared = new Prepared
            {
                Model = EnemyModelAvatarUtility.ResolvePreferredVisualModel(model),
                Template = template,
                VisualChild = visualChild,
                EnemyDef = edef
            };
            return sb.ToString().TrimEnd();
        }

        static GameObject ResolveCurrentVisual(GameObject prefabAsset, string visualChild)
        {
            if (prefabAsset == null)
                return null;
            Transform child = prefabAsset.transform.Find(visualChild);
            if (child == null)
                return null;
            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject);
            return source != null && source != child.gameObject ? source : null;
        }

        // ------------------------------------------------------------------ Scene usage

        sealed class SceneUsage
        {
            public int Loaded;
            public int HandPlacedLoaded;
            public int InFiles;
            public int AddedInFiles;
            public readonly List<string> Files = new List<string>();

            public string Format()
            {
                var sb = new StringBuilder();
                sb.Append($"Scene instances: {Loaded} in loaded scene(s) ({HandPlacedLoaded} with hand-placed children/components); ");
                sb.Append($"{InFiles} in scene files under Assets/_Project/Scenes ({AddedInFiles} added objects)");
                if (Files.Count > 0)
                    sb.Append(": " + string.Join(", ", Files));
                sb.Append('.');
                return sb.ToString();
            }
        }

        static SceneUsage CountSceneUsage(string prefabPath)
        {
            var usage = new SceneUsage();
            for (int s = 0; s < UnityEngine.SceneManagement.SceneManager.sceneCount; s++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(s);
                if (!scene.isLoaded)
                    continue;
                foreach (GameObject rootGo in scene.GetRootGameObjects())
                {
                    foreach (Transform t in rootGo.GetComponentsInChildren<Transform>(true))
                    {
                        GameObject go = t.gameObject;
                        if (!PrefabUtility.IsOutermostPrefabInstanceRoot(go))
                            continue;
                        GameObject src = PrefabUtility.GetCorrespondingObjectFromSource(go);
                        if (src == null || !string.Equals(AssetDatabase.GetAssetPath(src), prefabPath, StringComparison.OrdinalIgnoreCase))
                            continue;
                        usage.Loaded++;
                        if (PrefabUtility.GetAddedGameObjects(go).Count > 0 || PrefabUtility.GetAddedComponents(go).Count > 0)
                            usage.HandPlacedLoaded++;
                    }
                }
            }

            string guid = AssetDatabase.AssetPathToGUID(prefabPath);
            string scenesDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Assets", "_Project", "Scenes");
            if (!string.IsNullOrEmpty(guid) && Directory.Exists(scenesDir))
            {
                var inst = new Regex("m_SourcePrefab: \\{fileID: 100100000, guid: " + guid);
                var added = new Regex("targetCorrespondingSourceObject: \\{fileID: -?\\d+, guid: " + guid + ", type: 3\\}\\s+insertIndex: -?\\d+\\s+addedObject:");
                foreach (string file in Directory.GetFiles(scenesDir, "*.unity", SearchOption.AllDirectories))
                {
                    string text;
                    try { text = File.ReadAllText(file); }
                    catch (IOException) { continue; }
                    if (text.IndexOf(guid, StringComparison.Ordinal) < 0)
                        continue;
                    int n = inst.Matches(text).Count;
                    if (n == 0)
                        continue;
                    int a = added.Matches(text).Count;
                    usage.InFiles += n;
                    usage.AddedInFiles += a;
                    usage.Files.Add($"{Path.GetFileNameWithoutExtension(file)} ({n})");
                }
            }

            return usage;
        }

        // ------------------------------------------------------------------ Data

        void Select(Row row)
        {
            selectedPath = row.Path;
            dryRunText = null;
            lastMessage = null;
            linkCandidate = null;
            workingDirty = false;
            if (workingDef != null)
            {
                UnityEngine.Object.DestroyImmediate(workingDef);
                workingDef = null;
            }
        }

        Row FindSelected()
        {
            if (string.IsNullOrEmpty(selectedPath))
                return null;
            for (int i = 0; i < rows.Count; i++)
                if (rows[i].Path == selectedPath)
                    return rows[i];
            return null;
        }

        List<Row> Visible()
        {
            if (string.IsNullOrWhiteSpace(filter))
                return rows;
            string f = filter.Trim();
            var list = new List<Row>();
            for (int i = 0; i < rows.Count; i++)
            {
                Row r = rows[i];
                if (Has(r.Name, f) || Has(r.KindLabel, f) || Has(r.DefLabel, f) || Has(r.StatusText, f))
                    list.Add(r);
            }

            return list;
        }

        static bool Has(string s, string f) => s != null && s.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0;

        void Refresh()
        {
            loaded = true;
            rows.Clear();
            string folder = kind == ListKind.Enemy ? ProjectAssetPaths.PrefabsCombatEnemies : ProjectAssetPaths.PrefabsPlayers;
            PlayerVisualDefinition[] playerDefs = kind == ListKind.Player
                ? PlayerPrefabVisualSetupUtility.LoadAllDefinitions()
                : Array.Empty<PlayerVisualDefinition>();

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null)
                    continue;

                bool isEnemy = asset.GetComponent<EnemyInvectorBootstrap>() != null;
                bool isPlayer = !isEnemy && asset.GetComponent("vThirdPersonController") != null;
                if (kind == ListKind.Enemy ? !isEnemy : !isPlayer)
                    continue;

                var row = new Row { Path = path, Name = Path.GetFileNameWithoutExtension(path), Asset = asset };
                row.Protected = DMCharacterCreatorProtection.IsProtectedPath(path);
                if (kind == ListKind.Player)
                    row.PlayerDef = FindPlayerDefinition(playerDefs, asset, row.Name);
                Check(row);
                rows.Add(row);
            }

            rows.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        }

        void RefreshRow(Row row)
        {
            if (kind == ListKind.Player)
                row.PlayerDef = FindPlayerDefinition(PlayerPrefabVisualSetupUtility.LoadAllDefinitions(), row.Asset, row.Name);
            Check(row);
        }

        /// <summary>Strict match only (prefab file name or display name); never falls back to a generic default definition.</summary>
        static EnemyDefinition FindMatchingDefinition(string prefabPath)
        {
            string fileName = Path.GetFileNameWithoutExtension(prefabPath);
            foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { ProjectAssetPaths.EnemiesData }))
            {
                EnemyDefinition d = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (d == null)
                    continue;
                if (string.Equals(d.prefabFileName, fileName, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(d.displayName) && string.Equals(d.displayName.Replace(' ', '_'), fileName, StringComparison.OrdinalIgnoreCase)))
                    return d;
            }

            return null;
        }

        static PlayerVisualDefinition FindPlayerDefinition(PlayerVisualDefinition[] defs, GameObject asset, string name)
        {
            for (int i = 0; i < defs.Length; i++)
                if (defs[i] != null && defs[i].playerPrefab == asset)
                    return defs[i];
            for (int i = 0; i < defs.Length; i++)
                if (defs[i] != null && string.Equals(defs[i].prefabFileName, name, StringComparison.OrdinalIgnoreCase))
                    return defs[i];
            return null;
        }

        void Check(Row row)
        {
            row.Report = null;
            row.Plan = null;
            row.HandDistance = -1f;
            GameObject contents = null;
            try
            {
                contents = PrefabUtility.LoadPrefabContents(row.Path);
                if (kind == ListKind.Enemy)
                    CheckEnemy(row, contents);
                else
                    CheckPlayer(row, contents);
            }
            catch (Exception ex)
            {
                row.Report = new DMCharacterCreatorBuildReport();
                row.Report.Errors.Add("Check failed: " + ex.Message);
            }
            finally
            {
                if (contents != null)
                    PrefabUtility.UnloadPrefabContents(contents);
            }

            DMCharacterCreatorBuildReport r = row.Report;
            if (r.Errors.Count > 0)
            {
                row.Status = RowStatus.Error;
                row.StatusText = r.Errors.Count + " error" + (r.Errors.Count > 1 ? "s" : string.Empty);
            }
            else if (r.Warnings.Count > 0)
            {
                row.Status = RowStatus.Warning;
                row.StatusText = r.Warnings.Count + " warn";
            }
            else
            {
                row.Status = RowStatus.Ok;
                row.StatusText = "OK";
            }
        }

        void CheckEnemy(Row row, GameObject contents)
        {
            EnemyInvectorBootstrap boot = contents.GetComponent<EnemyInvectorBootstrap>();
            EnemyDefinition linked = null;
            row.BakedCapsule = "-";
            if (boot != null)
            {
                var so = new SerializedObject(boot);
                linked = so.FindProperty("enemyDefinition")?.objectReferenceValue as EnemyDefinition;
                SerializedProperty rad = so.FindProperty("hitCapsuleRadius");
                SerializedProperty hgt = so.FindProperty("hitCapsuleHeight");
                if (rad != null && hgt != null)
                    row.BakedCapsule = $"{rad.floatValue:0.###}/{hgt.floatValue:0.###}";
            }

            CapsuleCollider cap = contents.GetComponent<CapsuleCollider>();
            row.RootCapsule = cap != null ? $"{cap.radius:0.###}/{cap.height:0.###}" : "none";

            row.EnemyDef = linked;
            row.CandidateDef = linked == null ? FindMatchingDefinition(row.Path) : null;
            EnemyDefinition shown = linked ?? row.CandidateDef;
            row.DefLabel = linked != null ? linked.name : (shown != null ? shown.name + " (unlinked)" : "none");
            row.KindLabel = shown != null ? shown.bodyType.ToString() : "-";

            GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>(
                EnemyPrefabVisualSetupUtility.ResolveTemplatePath(linked != null ? linked.templatePrefab : null));
            DMCharacterCreatorBuildReport report = DMCharacterCreatorPostBuildCheck.Run(contents, linked, row.Path, template);
            if (linked == null)
                report.Errors.Insert(0, "No definition link: EnemyInvectorBootstrap.enemyDefinition is empty (prefab-side values are the only source).");
            else
            {
                var blockers = DMCharacterCreatorActionValidation.CollectEnemySaveBlockers(linked, DMCharacterCreatorDefinitionLink.FileNameFor(linked));
                for (int i = 0; i < blockers.Count; i++)
                    report.Warnings.Add("Definition: " + blockers[i]);
            }

            row.HandDistance = MeasureIdleHandDistance(contents);
            if (row.HandDistance >= 0f && row.HandDistance < CrossedArmHandDistance)
                report.Warnings.Add(
                    $"Crossed-arm idle: hand distance {row.HandDistance:0.00} (relaxed is about 0.57). Re-apply Definition (avatar T-pose fix) or Rebuild.");

            row.Report = report;
            row.Plan = DMCharacterCreatorFixer.Analyze(contents, row.Path, linked, row.CandidateDef, row.HandDistance, template, report);
        }

        void CheckPlayer(Row row, GameObject contents)
        {
            var report = new DMCharacterCreatorBuildReport();
            row.Report = report;
            row.KindLabel = row.Protected ? "Player (locked)" : "Player";
            row.DefLabel = row.PlayerDef != null ? row.PlayerDef.name : "none";

            Animator anim = contents.GetComponent<Animator>();
            if (anim == null || anim.avatar == null || !anim.avatar.isValid || !anim.avatar.isHuman)
                report.Warnings.Add("Root Animator has no valid Humanoid avatar.");
            else if (anim.runtimeAnimatorController == null)
                report.Errors.Add("Root Animator has no controller.");
            else if (!EditorUtility.IsPersistent(anim.avatar))
                report.Warnings.Add("The humanoid avatar is not a saved asset.");

            if (contents.GetComponent("vThirdPersonController") == null)
                report.Errors.Add("vThirdPersonController is missing from the root.");
            if (row.PlayerDef == null && !row.Protected)
                report.Info.Add("No PlayerVisualDefinition points at this prefab (not creator-built).");
        }

        /// <summary>Distance between the hands (root space) in the Invector Idle clip; crossed arms read small.</summary>
        public static float MeasureIdleHandDistance(GameObject root)
        {
            if (root == null)
                return -1f;
            if (idleClip == null)
            {
                foreach (UnityEngine.Object o in AssetDatabase.LoadAllAssetsAtPath(IdleFbx))
                {
                    if (o is AnimationClip c && c.name == "Idle")
                    {
                        idleClip = c;
                        break;
                    }
                }
            }

            Animator an = root.GetComponent<Animator>();
            if (idleClip == null || an == null || an.avatar == null || !an.avatar.isValid || !an.avatar.isHuman)
                return -1f;

            an.Rebind();
            idleClip.SampleAnimation(root, idleClip.length * 0.37f);
            Transform l = an.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform r = an.GetBoneTransform(HumanBodyBones.RightHand);
            if (l == null || r == null)
                return -1f;
            return Vector3.Distance(root.transform.InverseTransformPoint(l.position), root.transform.InverseTransformPoint(r.position));
        }

        // ------------------------------------------------------------------ Small helpers

        static void Stat(string label, string value) => EditorGUILayout.LabelField(label, value, EditorStyles.wordWrappedLabel);

        static string Nm(UnityEngine.Object o) => o != null ? o.name : "none";

        static Color StatusColor(RowStatus s)
        {
            switch (s)
            {
                case RowStatus.Ok: return new Color(0.35f, 0.8f, 0.45f);
                case RowStatus.Warning: return new Color(0.95f, 0.7f, 0.2f);
                default: return new Color(0.9f, 0.3f, 0.3f);
            }
        }
    }
}
#endif
