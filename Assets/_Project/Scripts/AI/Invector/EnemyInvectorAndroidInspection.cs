using System.Collections.Generic;
using System.Text;
using Invector.vCharacterController;
using Invector.vMelee;
using Invector.vShooter;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// Checklist audit for humanoid Android / Invector enemy prefabs (Corrupt Patrol Android, etc.).
    /// Complements <see cref="EnemyInvectorRagdollAudit"/> with definition, visual, loadout, and AI data checks.
    /// </summary>
    public static class EnemyInvectorAndroidInspection
    {
        public struct ChecklistItem
        {
            public string Id;
            public string Label;
            public bool Passed;
            public string Detail;
        }

        public struct Report
        {
            public string PrefabName;
            public EnemyDefinition Definition;
            public List<ChecklistItem> Items;
            public EnemyInvectorRagdollAudit.Report RagdollAudit;

            public bool IsHealthy
            {
                get
                {
                    if (Items == null || Items.Count == 0)
                        return false;

                    for (int i = 0; i < Items.Count; i++)
                    {
                        if (!Items[i].Passed)
                            return false;
                    }

                    return true;
                }
            }
        }

        public static Report Audit(GameObject root)
        {
            Report report = new Report
            {
                PrefabName = root != null ? root.name : "(null)",
                Items = new List<ChecklistItem>(24),
            };

            if (root == null)
            {
                AddItem(report, "root", "Prefab root exists", false, "Root is null.");
                return report;
            }

            EnemyInvectorBootstrap bootstrap = root.GetComponent<EnemyInvectorBootstrap>();
            report.Definition = bootstrap != null ? bootstrap.Definition : null;

            AddItem(report, "bootstrap", "EnemyInvectorBootstrap present", bootstrap != null,
                bootstrap != null ? "OK" : "Missing bootstrap — not a humanoid Invector enemy.");

            EnemyDefinition definition = report.Definition;
            AddItem(report, "definition", "EnemyDefinition linked on bootstrap", definition != null,
                definition != null ? definition.name : "Assign EnemyDefinition on EnemyInvectorBootstrap.");

            if (definition != null)
            {
                bool androidKind = definition.surfaceThreatKind == SurfaceThreatKind.Android;
                AddItem(report, "threat_kind", "SurfaceThreatKind = Android", androidKind,
                    androidKind
                        ? "Android category set for encounter tables."
                        : $"Expected Android, found {definition.surfaceThreatKind}.");

                bool humanoidArchetype = definition.archetype == EnemyArchetype.HumanoidInvector;
                AddItem(report, "archetype", "Archetype = HumanoidInvector", humanoidArchetype,
                    humanoidArchetype
                        ? "Humanoid Invector archetype."
                        : $"Expected HumanoidInvector, found {definition.archetype}.");
            }

            bool spawnReady = EnemyPrefabResolver.IsSpawnReady(root);
            AddItem(report, "spawn_ready", "Spawn-ready gameplay stack", spawnReady,
                spawnReady
                    ? "EnemyHealth + EnemyAiController + EnemyCombat present."
                    : "Missing EnemyHealth, EnemyAiController, or EnemyCombat.");

            AddItem(report, "health", "EnemyHealth", root.GetComponent<EnemyHealth>() != null, DetailPresent(root, typeof(EnemyHealth)));
            AddItem(report, "ai", "EnemyAiController", root.GetComponent<EnemyAiController>() != null, DetailPresent(root, typeof(EnemyAiController)));
            AddItem(report, "combat", "EnemyCombat", root.GetComponent<EnemyCombat>() != null, DetailPresent(root, typeof(EnemyCombat)));
            AddItem(report, "motor_bridge", "EnemyInvectorMotorBridge", root.GetComponent<EnemyInvectorMotorBridge>() != null,
                DetailPresent(root, typeof(EnemyInvectorMotorBridge)));
            AddItem(report, "combat_bridge", "EnemyInvectorCombatBridge", root.GetComponent<EnemyInvectorCombatBridge>() != null,
                DetailPresent(root, typeof(EnemyInvectorCombatBridge)));
            AddItem(report, "loadout_bridge", "EnemyInvectorLoadoutBridge", root.GetComponent<EnemyInvectorLoadoutBridge>() != null,
                DetailPresent(root, typeof(EnemyInvectorLoadoutBridge)));
            AddItem(report, "ragdoll_bridge", "EnemyInvectorRagdollBridge", root.GetComponent<EnemyInvectorRagdollBridge>() != null,
                DetailPresent(root, typeof(EnemyInvectorRagdollBridge)));
            AddItem(report, "death_presenter", "EnemyInvectorDeathPresenter", root.GetComponent<EnemyInvectorDeathPresenter>() != null,
                DetailPresent(root, typeof(EnemyInvectorDeathPresenter)));
            AddItem(report, "death_sequence", "EnemyDeathSequence", root.GetComponent<EnemyDeathSequence>() != null,
                DetailPresent(root, typeof(EnemyDeathSequence)));

            AuditVisualRig(root, definition, report);
            AuditLoadout(root, definition, report);
            AuditPatrolDefinition(definition, report);

            report.RagdollAudit = EnemyInvectorRagdollAudit.Audit(root);
            bool ragdollRigUsable = EnemyInvectorRagdollRigRepair.HasUsableRagdollUnderAvatar(root);
            int boneCount = EnemyInvectorRagdollRigRepair.CountBoneRigidbodiesUnderAvatarHips(root);
            AddItem(report, "ragdoll_rig", "Avatar hips ragdoll rig usable", ragdollRigUsable,
                ragdollRigUsable
                    ? $"{boneCount} bone rigidbodies under avatar hips."
                    : $"Only {boneCount} bone rigidbodies — remount VBOT physics or rebuild ragdoll.");

            AddItem(report, "ragdoll_audit", "Ragdoll audit clean", report.RagdollAudit.IsHealthy,
                report.RagdollAudit.IsHealthy
                    ? "vRagdoll, bridge, layers, joints, and collider sizes OK."
                    : SummarizeRagdollIssues(report.RagdollAudit));

            return report;
        }

        public static string FormatReport(Report report)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(report.PrefabName + (report.IsHealthy ? " — PASS" : " — NEEDS ATTENTION"));
            if (report.Definition != null)
                builder.AppendLine($"Definition: {report.Definition.displayName} ({report.Definition.enemyId})");

            for (int i = 0; i < report.Items.Count; i++)
            {
                ChecklistItem item = report.Items[i];
                builder.Append(item.Passed ? "[x] " : "[ ] ");
                builder.Append(item.Label);
                if (!string.IsNullOrEmpty(item.Detail))
                    builder.Append(" — ").Append(item.Detail);
                builder.AppendLine();
            }

            return builder.ToString().TrimEnd();
        }

        private static void AuditVisualRig(GameObject root, EnemyDefinition definition, Report report)
        {
            string visualChildName = definition != null && !string.IsNullOrWhiteSpace(definition.visualChildName)
                ? definition.visualChildName
                : "Visual";

            Transform visualChild = root.transform.Find(visualChildName);
            bool hasVisualChild = visualChild != null;
            AddItem(report, "visual_child", $"Visual child '{visualChildName}'", hasVisualChild,
                hasVisualChild ? visualChild.name : $"Missing child transform '{visualChildName}'.");

            Animator rootAnimator = root.GetComponent<Animator>();
            bool hasRootAnimator = rootAnimator != null;
            bool humanoidAvatar = hasRootAnimator && rootAnimator.avatar != null && rootAnimator.avatar.isHuman &&
                                  rootAnimator.avatar.isValid;
            AddItem(report, "root_avatar", "Root Animator humanoid avatar", humanoidAvatar,
                hasRootAnimator
                    ? (humanoidAvatar ? rootAnimator.avatar.name : "Avatar missing or not valid humanoid.")
                    : "No Animator on root.");

            int nestedAnimatorCount = 0;
            Animator[] animators = root.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                if (animators[i] != null && animators[i] != rootAnimator)
                    nestedAnimatorCount++;
            }

            AddItem(report, "nested_animators", "No nested Animators (root only)", nestedAnimatorCount == 0,
                nestedAnimatorCount == 0 ? "Single root Animator." : $"{nestedAnimatorCount} nested Animator(s) — strip in repair.");

            bool customMeshVisible = false;
            bool vbotBodyHidden = true;
            SkinnedMeshRenderer[] skinned = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skinned.Length; i++)
            {
                SkinnedMeshRenderer renderer = skinned[i];
                if (renderer == null)
                    continue;

                string path = BuildTransformPath(renderer.transform, root.transform);
                bool isVbotBody = path.IndexOf("VBOT_LOD", System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (renderer.enabled && !isVbotBody)
                    customMeshVisible = true;
                if (isVbotBody && renderer.enabled)
                    vbotBodyHidden = false;
            }

            AddItem(report, "custom_mesh", "Custom Meshy/visual mesh enabled", customMeshVisible,
                customMeshVisible ? "Enabled non-VBOT skinned mesh found." : "No enabled custom visual mesh.");

            AddItem(report, "vbot_hidden", "Stock VBOT body hidden", vbotBodyHidden,
                vbotBodyHidden ? "VBOT_LOD renderers disabled." : "VBOT_LOD still enabled — run humanoid visual setup.");
        }

        private static void AuditLoadout(GameObject root, EnemyDefinition definition, Report report)
        {
            if (definition == null)
                return;

            if (definition.meleeWeaponItem != null)
            {
                bool hasMeleeDrawn = HasDrawnWeaponSlot(root, preferMelee: true);
                AddItem(report, "melee_drawn", "Melee Drawn_* weapon slot", hasMeleeDrawn,
                    hasMeleeDrawn
                        ? $"Melee item: {definition.meleeWeaponItem.name}"
                        : "Definition has melee weapon but no Drawn_* melee slot found.");
            }

            if (definition.rangedWeaponItem != null)
            {
                bool hasRangedDrawn = HasDrawnWeaponSlot(root, preferMelee: false);
                AddItem(report, "ranged_drawn", "Ranged Drawn_* weapon slot", hasRangedDrawn,
                    hasRangedDrawn
                        ? $"Ranged item: {definition.rangedWeaponItem.name}"
                        : "Definition has ranged weapon but no Drawn_* ranged slot found.");
            }
        }

        private static void AuditPatrolDefinition(EnemyDefinition definition, Report report)
        {
            if (definition == null)
                return;

            if (definition.movementMode != EnemyMovementMode.Patrol)
                return;

            bool patrolConfigured = definition.patrolPointCount > 0 || definition.patrolRadius > 0f;
            AddItem(report, "patrol_data", "Patrol movement configured on definition", patrolConfigured,
                patrolConfigured
                    ? $"patrolPointCount={definition.patrolPointCount}, patrolRadius={definition.patrolRadius:0.#}m"
                    : "movementMode=Patrol but no patrol points/radius on EnemyDefinition.");
        }

        private static bool HasDrawnWeaponSlot(GameObject root, bool preferMelee)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == null || !child.name.StartsWith("Drawn_", System.StringComparison.Ordinal))
                    continue;

                if (preferMelee && child.GetComponentInChildren<vMeleeWeapon>(true) != null)
                    return true;
                if (!preferMelee && child.GetComponentInChildren<vShooterWeapon>(true) != null)
                    return true;
            }

            return false;
        }

        private static void AddItem(Report report, string id, string label, bool passed, string detail)
        {
            report.Items.Add(new ChecklistItem
            {
                Id = id,
                Label = label,
                Passed = passed,
                Detail = detail ?? string.Empty,
            });
        }

        private static string DetailPresent(GameObject root, System.Type componentType)
        {
            return root.GetComponent(componentType) != null ? "Present." : "Missing.";
        }

        private static string SummarizeRagdollIssues(EnemyInvectorRagdollAudit.Report ragdollReport)
        {
            if (ragdollReport.Issues == null || ragdollReport.Issues.Count == 0)
                return "Ragdoll audit failed — see console.";

            return string.Join(" | ", ragdollReport.Issues);
        }

        private static string BuildTransformPath(Transform node, Transform stopAt)
        {
            if (node == null)
                return string.Empty;

            System.Text.StringBuilder path = new System.Text.StringBuilder(node.name);
            Transform current = node.parent;
            while (current != null && current != stopAt)
            {
                path.Insert(0, current.name + "/");
                current = current.parent;
            }

            return path.ToString();
        }
    }
}
