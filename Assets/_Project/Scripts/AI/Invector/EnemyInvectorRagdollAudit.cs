using System.Collections.Generic;
using Invector.vCharacterController;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// Validates and repairs humanoid enemy ragdoll setup (vRagdoll + BodyPart bone layers).
    /// </summary>
    public static class EnemyInvectorRagdollAudit
    {
        public struct Report
        {
            public bool HasRagdoll;
            public bool HasRagdollBridge;
            public int BoneRigidbodyCount;
            public int WrongLayerBoneCount;
            public int MissingRigidbodyColliderCount;
            public int MissingJointBoneCount;
            public int ImplausiblySizedColliderCount;
            public List<string> Issues;

            public bool IsHealthy =>
                HasRagdoll &&
                HasRagdollBridge &&
                WrongLayerBoneCount == 0 &&
                MissingJointBoneCount == 0 &&
                ImplausiblySizedColliderCount == 0 &&
                (Issues == null || Issues.Count == 0);
        }

        public static Report Audit(GameObject root)
        {
            Report report = new Report
            {
                Issues = new List<string>(),
            };

            if (root == null)
            {
                report.Issues.Add("Root is null.");
                return report;
            }

            report.HasRagdoll = root.GetComponent<vRagdoll>() != null;
            report.HasRagdollBridge = root.GetComponent<EnemyInvectorRagdollBridge>() != null;

            if (!report.HasRagdoll)
                report.Issues.Add("Missing vRagdoll component.");

            if (!report.HasRagdollBridge)
                report.Issues.Add("Missing EnemyInvectorRagdollBridge component.");

            int bodyPartLayer = LayerMask.NameToLayer("BodyPart");
            if (bodyPartLayer < 0)
                report.Issues.Add("BodyPart layer is not defined in Tags & Layers.");

            // vRagdollBuilder wires every non-root bone to its parent bone with a CharacterJoint
            // (connectedBody = parent's Rigidbody). A bone missing that joint — or with a joint whose
            // connectedBody was left null by an incomplete rebuild — is a free-floating Rigidbody once
            // ActivateRagdoll makes it dynamic: nothing pulls it back toward the skeleton, so gravity
            // plus initial collider-overlap depenetration flings it off independently of the corpse.
            // That reads as body parts scattered across the map rather than a single frozen or
            // launched corpse, and the older bones/layer/collider checks below never caught it.
            Animator animator = root.GetComponentInChildren<Animator>(true);
            Transform hipsTransform = animator != null && animator.isHuman
                ? animator.GetBoneTransform(HumanBodyBones.Hips)
                : null;
            List<string> unjointedBones = new List<string>();
            List<string> oversizedColliders = new List<string>();

            Rigidbody rootBody = root.GetComponent<Rigidbody>();
            Rigidbody[] bodies = root.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++)
            {
                Rigidbody body = bodies[i];
                if (body == null || body == rootBody)
                    continue;

                report.BoneRigidbodyCount++;

                if (bodyPartLayer >= 0 && body.gameObject.layer != bodyPartLayer)
                    report.WrongLayerBoneCount++;

                Collider[] colliders = body.GetComponents<Collider>();
                bool hasSolidCollider = false;
                for (int c = 0; c < colliders.Length; c++)
                {
                    Collider collider = colliders[c];
                    if (collider == null)
                        continue;

                    if (collider.enabled && !collider.isTrigger)
                        hasSolidCollider = true;

                    // Checked regardless of enabled state: a disabled-but-oversized collider is a ticking
                    // time bomb rather than a "missing" one. EnemyInvectorHitSetup.ReleaseForRagdoll
                    // enables every bone collider the moment a corpse ragdolls, so a genuinely
                    // mis-proportioned collider would suddenly overlap the ground/other bones by a huge
                    // margin — PhysX's depenetration response would then fling that bone (and anything
                    // jointed to it) off the map instead of a normal collapse. IsImplausiblyOversized
                    // compares the collider's real WORLD-space size (via Transform.lossyScale), not its
                    // raw local fields, so a bone correctly sitting under a small-scaled "Armature"
                    // wrapper (a common cm-to-meter conversion pattern) won't false-positive here even
                    // though its local radius/height/size numbers look large in isolation.
                    if (EnemyInvectorPhysicsCache.IsImplausiblyOversized(collider, out string sizeDescription))
                    {
                        report.ImplausiblySizedColliderCount++;
                        oversizedColliders.Add($"{body.gameObject.name} ({sizeDescription})");
                    }
                }

                if (!hasSolidCollider)
                    report.MissingRigidbodyColliderCount++;

                bool isHips = hipsTransform != null && body.transform == hipsTransform;
                if (!isHips)
                {
                    CharacterJoint joint = body.GetComponent<CharacterJoint>();
                    if (joint == null || joint.connectedBody == null)
                    {
                        report.MissingJointBoneCount++;
                        unjointedBones.Add(body.gameObject.name);
                    }
                }
            }

            if (report.WrongLayerBoneCount > 0)
            {
                report.Issues.Add(
                    $"{report.WrongLayerBoneCount} bone rigidbody layer(s) are not BodyPart.");
            }

            if (report.MissingRigidbodyColliderCount > 0)
            {
                report.Issues.Add(
                    $"{report.MissingRigidbodyColliderCount} bone rigidbody collider(s) are disabled or missing.");
            }

            if (report.MissingJointBoneCount > 0)
            {
                report.Issues.Add(
                    $"{report.MissingJointBoneCount} bone(s) missing a CharacterJoint to their parent " +
                    $"(will fly off independently on ragdoll): {string.Join(", ", unjointedBones)}.");
            }

            if (report.ImplausiblySizedColliderCount > 0)
            {
                report.Issues.Add(
                    $"{report.ImplausiblySizedColliderCount} bone collider(s) are implausibly sized for a " +
                    $"ragdoll bone (will explode on ragdoll activation): {string.Join(", ", oversizedColliders)}.");
            }

            return report;
        }

        /// <summary>
        /// Repairs the fixable issues: missing vRagdoll/bridge/cache components, stale bone layers,
        /// stale bodyParts cache. Does NOT fix <see cref="Report.MissingJointBoneCount"/> or
        /// <see cref="Report.ImplausiblySizedColliderCount"/> — both require rebuilding the ragdoll rig
        /// itself (correct joint axes/limits and correctly proportioned colliders need the character in
        /// T-pose with bones assigned by hand), which only Invector's ragdoll wizard
        /// (Invector > Basic Locomotion > Components > Ragdoll) can do safely. Re-run that wizard on any
        /// prefab the audit flags with either issue. At runtime, EnemyInvectorPhysicsCache already
        /// refuses to enable an implausibly sized collider on its own as a safety net, so a flagged
        /// prefab won't explode in the meantime — it'll just ragdoll without solid collision on that bone
        /// until the rig is rebuilt.
        /// </summary>
        public static void Repair(GameObject root)
        {
            if (root == null)
                return;

            EnemyInvectorRagdollSetup.EnsurePresent(root);
            if (root.GetComponent<EnemyInvectorRagdollBridge>() == null)
                root.AddComponent<EnemyInvectorRagdollBridge>();
            if (root.GetComponent<EnemyInvectorPhysicsCache>() == null)
                root.AddComponent<EnemyInvectorPhysicsCache>();

            vRagdoll ragdoll = root.GetComponent<vRagdoll>();
            if (ragdoll != null)
                ragdoll.LoadBodyPart();

            EnemyInvectorHitSetup.RestoreRagdollPhysicsLayers(root);
            EnemyInvectorHitSetup.StabilizeRigidbodies(root);
        }
    }
}
