using System.Collections.Generic;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// Remounts orphan ragdoll rigidbodies (e.g. leftover VBOT armature) onto the active
    /// humanoid avatar bones so <see cref="Invector.vCharacterController.vRagdoll"/> can find them.
    /// Corrupt Patrol Android ships Meshy visuals under Visual/Armature while physics stayed on
    /// inactive 3D Model/VBOT_* — ActivateRagdoll then freezes the pose with empty bodyParts.
    /// </summary>
    public static class EnemyInvectorRagdollRigRepair
    {
        public const int MinUsableBoneRigidbodies = 5;

        private static readonly Dictionary<string, HumanBodyBones> SuffixToHumanBone =
            new Dictionary<string, HumanBodyBones>(System.StringComparer.OrdinalIgnoreCase)
            {
                { "Hips", HumanBodyBones.Hips },
                { "LeftUpLeg", HumanBodyBones.LeftUpperLeg },
                { "LeftLeg", HumanBodyBones.LeftLowerLeg },
                { "LeftFoot", HumanBodyBones.LeftFoot },
                { "RightUpLeg", HumanBodyBones.RightUpperLeg },
                { "RightLeg", HumanBodyBones.RightLowerLeg },
                { "RightFoot", HumanBodyBones.RightFoot },
                { "Spine", HumanBodyBones.Spine },
                { "Spine1", HumanBodyBones.Chest },
                { "Spine2", HumanBodyBones.UpperChest },
                { "Chest", HumanBodyBones.Chest },
                { "LeftArm", HumanBodyBones.LeftUpperArm },
                { "LeftForeArm", HumanBodyBones.LeftLowerArm },
                { "LeftHand", HumanBodyBones.LeftHand },
                { "RightArm", HumanBodyBones.RightUpperArm },
                { "RightForeArm", HumanBodyBones.RightLowerArm },
                { "RightHand", HumanBodyBones.RightHand },
                { "Head", HumanBodyBones.Head },
                { "Neck", HumanBodyBones.Neck }
            };

        /// <summary>
        /// Returns true when the humanoid hips hierarchy already has enough bone rigidbodies
        /// for a usable Invector ragdoll.
        /// </summary>
        public static bool HasUsableRagdollUnderAvatar(GameObject root)
        {
            return CountBoneRigidbodiesUnderAvatarHips(root) >= MinUsableBoneRigidbodies;
        }

        public static int CountBoneRigidbodiesUnderAvatarHips(GameObject root)
        {
            Transform hips = ResolveAvatarHips(root);
            if (hips == null)
                return 0;

            Rigidbody rootBody = root != null ? root.GetComponent<Rigidbody>() : null;
            int count = 0;
            Rigidbody[] bodies = hips.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++)
            {
                Rigidbody body = bodies[i];
                if (body != null && body != rootBody)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Copies orphan bone physics onto matching avatar bones. Returns how many bones received physics.
        /// </summary>
        public static int TryRemountOrphanRagdollOntoAvatar(GameObject root)
        {
            if (root == null)
                return 0;

            if (HasUsableRagdollUnderAvatar(root))
                return CountBoneRigidbodiesUnderAvatarHips(root);

            Animator animator = root.GetComponentInChildren<Animator>(true);
            Transform hips = ResolveAvatarHips(root);
            if (animator == null || !animator.isHuman || hips == null)
                return 0;

            Rigidbody rootBody = root.GetComponent<Rigidbody>();
            Rigidbody[] allBodies = root.GetComponentsInChildren<Rigidbody>(true);

            var orphanSources = new List<Rigidbody>(16);
            for (int i = 0; i < allBodies.Length; i++)
            {
                Rigidbody body = allBodies[i];
                if (body == null || body == rootBody)
                    continue;

                if (body.transform == hips || body.transform.IsChildOf(hips))
                    continue;

                orphanSources.Add(body);
            }

            if (orphanSources.Count == 0)
                return 0;

            // source bone → destination avatar bone
            var remountMap = new Dictionary<Transform, Transform>();
            for (int i = 0; i < orphanSources.Count; i++)
            {
                Rigidbody source = orphanSources[i];
                Transform destination = ResolveAvatarBoneForSource(animator, source.name);
                if (destination == null)
                    continue;

                remountMap[source.transform] = destination;
            }

            if (remountMap.Count == 0)
                return 0;

            // Pass 1: rigidbodies + colliders
            foreach (KeyValuePair<Transform, Transform> pair in remountMap)
            {
                Rigidbody sourceBody = pair.Key.GetComponent<Rigidbody>();
                if (sourceBody == null)
                    continue;

                EnsureRigidbodyCopy(pair.Value.gameObject, sourceBody);
                EnsureColliderCopies(pair.Value.gameObject, pair.Key.gameObject);
            }

            // Pass 2: joints (need destination rigidbodies + remapped connectedBody)
            foreach (KeyValuePair<Transform, Transform> pair in remountMap)
            {
                CharacterJoint sourceJoint = pair.Key.GetComponent<CharacterJoint>();
                if (sourceJoint == null)
                    continue;

                Rigidbody connectedDestination = null;
                if (sourceJoint.connectedBody != null &&
                    remountMap.TryGetValue(sourceJoint.connectedBody.transform, out Transform connectedDestTransform))
                {
                    connectedDestination = connectedDestTransform.GetComponent<Rigidbody>();
                }
                else if (sourceJoint.connectedBody != null)
                {
                    // Connected bone may already live on the avatar (rare) or be the hips destination.
                    Transform mapped = ResolveAvatarBoneForSource(animator, sourceJoint.connectedBody.name);
                    if (mapped != null)
                        connectedDestination = mapped.GetComponent<Rigidbody>();
                }

                EnsureCharacterJointCopy(
                    pair.Value.gameObject,
                    sourceJoint,
                    connectedDestination,
                    pair.Key,
                    pair.Value);
            }

            // Strip physics from orphans so they cannot fight the avatar ragdoll.
            for (int i = 0; i < orphanSources.Count; i++)
            {
                Rigidbody source = orphanSources[i];
                if (source == null)
                    continue;

                StripPhysicsComponents(source.gameObject);
            }

            EnemyInvectorPhysicsCache cache = root.GetComponent<EnemyInvectorPhysicsCache>();
            if (cache != null)
            {
                cache.Refresh();
                cache.StabilizeAllRigidbodies();
            }
            else
            {
                EnemyInvectorHitSetup.StabilizeRigidbodies(root);
            }

            EnemyInvectorHitSetup.RestoreRagdollPhysicsLayers(root);
            return CountBoneRigidbodiesUnderAvatarHips(root);
        }

        private static Transform ResolveAvatarHips(GameObject root)
        {
            if (root == null)
                return null;

            Animator animator = root.GetComponentInChildren<Animator>(true);
            if (animator == null || !animator.isHuman)
                return null;

            return animator.GetBoneTransform(HumanBodyBones.Hips);
        }

        private static Transform ResolveAvatarBoneForSource(Animator animator, string sourceName)
        {
            if (animator == null || string.IsNullOrEmpty(sourceName))
                return null;

            string suffix = NormalizeBoneSuffix(sourceName);
            if (string.IsNullOrEmpty(suffix))
                return null;

            if (SuffixToHumanBone.TryGetValue(suffix, out HumanBodyBones humanBone))
            {
                Transform mapped = animator.GetBoneTransform(humanBone);
                if (mapped != null)
                    return mapped;

                // UpperChest often unmapped on Meshy rigs — fall back to Chest.
                if (humanBone == HumanBodyBones.UpperChest)
                {
                    mapped = animator.GetBoneTransform(HumanBodyBones.Chest);
                    if (mapped != null)
                        return mapped;
                }
            }

            // Exact / contains scan under hips as last resort.
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips == null)
                return null;

            Transform[] children = hips.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (string.Equals(children[i].name, suffix, System.StringComparison.OrdinalIgnoreCase))
                    return children[i];
            }

            return null;
        }

        private static string NormalizeBoneSuffix(string sourceName)
        {
            string name = sourceName;
            const string vbotPrefix = "VBOT_:";
            if (name.StartsWith(vbotPrefix, System.StringComparison.OrdinalIgnoreCase))
                name = name.Substring(vbotPrefix.Length);
            else if (name.StartsWith("VBOT_", System.StringComparison.OrdinalIgnoreCase))
                name = name.Substring("VBOT_".Length);

            int colon = name.LastIndexOf(':');
            if (colon >= 0 && colon < name.Length - 1)
                name = name.Substring(colon + 1);

            return name.Trim();
        }

        private static void EnsureRigidbodyCopy(GameObject destination, Rigidbody source)
        {
            Rigidbody destBody = destination.GetComponent<Rigidbody>();
            if (destBody == null)
                destBody = destination.AddComponent<Rigidbody>();

            destBody.mass = source.mass;
            destBody.linearDamping = source.linearDamping;
            destBody.angularDamping = source.angularDamping;
            destBody.useGravity = source.useGravity;
            destBody.isKinematic = true;
            destBody.interpolation = source.interpolation;
            destBody.collisionDetectionMode = source.collisionDetectionMode;
            destBody.constraints = RigidbodyConstraints.FreezeRotation;
            destBody.maxDepenetrationVelocity = source.maxDepenetrationVelocity;
            destBody.sleepThreshold = source.sleepThreshold;
        }

        private static void EnsureColliderCopies(GameObject destination, GameObject source)
        {
            Collider[] sourceColliders = source.GetComponents<Collider>();
            for (int i = 0; i < sourceColliders.Length; i++)
            {
                Collider sourceCollider = sourceColliders[i];
                if (sourceCollider == null)
                    continue;

                if (HasMatchingCollider(destination, sourceCollider))
                    continue;

                CopyCollider(destination, sourceCollider, source.transform, destination.transform);
            }
        }

        private static bool HasMatchingCollider(GameObject destination, Collider sourceCollider)
        {
            Collider[] existing = destination.GetComponents<Collider>();
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i] != null && existing[i].GetType() == sourceCollider.GetType())
                    return true;
            }

            return false;
        }

        private static void CopyCollider(
            GameObject destination,
            Collider sourceCollider,
            Transform sourceTransform,
            Transform destinationTransform)
        {
            Vector3 scale = ResolveLocalScaleTransfer(sourceTransform, destinationTransform);

            switch (sourceCollider)
            {
                case CapsuleCollider sourceCapsule:
                {
                    CapsuleCollider dest = destination.AddComponent<CapsuleCollider>();
                    int heightAxis = sourceCapsule.direction;
                    float heightScale = Axis(scale, heightAxis);
                    float radiusScale = Mathf.Max(
                        Axis(scale, heightAxis == 0 ? 1 : 0),
                        Axis(scale, heightAxis == 2 ? 1 : 2));
                    dest.center = Vector3.Scale(sourceCapsule.center, scale);
                    dest.radius = sourceCapsule.radius * radiusScale;
                    dest.height = sourceCapsule.height * heightScale;
                    dest.direction = sourceCapsule.direction;
                    dest.isTrigger = false;
                    dest.enabled = false;
                    break;
                }
                case BoxCollider sourceBox:
                {
                    BoxCollider dest = destination.AddComponent<BoxCollider>();
                    dest.center = Vector3.Scale(sourceBox.center, scale);
                    dest.size = Vector3.Scale(sourceBox.size, scale);
                    dest.isTrigger = false;
                    dest.enabled = false;
                    break;
                }
                case SphereCollider sourceSphere:
                {
                    SphereCollider dest = destination.AddComponent<SphereCollider>();
                    float radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                    dest.center = Vector3.Scale(sourceSphere.center, scale);
                    dest.radius = sourceSphere.radius * radiusScale;
                    dest.isTrigger = false;
                    dest.enabled = false;
                    break;
                }
            }
        }

        /// <summary>
        /// VBOT armatures often sit under a 0.01 cm→m parent while Meshy bones are identity.
        /// Local collider sizes must be converted to destination local space.
        /// </summary>
        private static Vector3 ResolveLocalScaleTransfer(Transform source, Transform destination)
        {
            Vector3 src = source.lossyScale;
            Vector3 dst = destination.lossyScale;
            return new Vector3(
                SafeDiv(Abs(src.x), Abs(dst.x)),
                SafeDiv(Abs(src.y), Abs(dst.y)),
                SafeDiv(Abs(src.z), Abs(dst.z)));
        }

        private static float SafeDiv(float numerator, float denominator)
        {
            if (denominator < 1e-6f)
                return numerator;
            return numerator / denominator;
        }

        private static float Abs(float value) => Mathf.Abs(value);

        private static float Axis(Vector3 scale, int axis)
        {
            switch (axis)
            {
                case 0: return Mathf.Abs(scale.x);
                case 2: return Mathf.Abs(scale.z);
                default: return Mathf.Abs(scale.y);
            }
        }

        private static void EnsureCharacterJointCopy(
            GameObject destination,
            CharacterJoint sourceJoint,
            Rigidbody connectedBody,
            Transform sourceTransform,
            Transform destinationTransform)
        {
            CharacterJoint destJoint = destination.GetComponent<CharacterJoint>();
            if (destJoint == null)
                destJoint = destination.AddComponent<CharacterJoint>();

            Vector3 scale = ResolveLocalScaleTransfer(sourceTransform, destinationTransform);

            destJoint.connectedBody = connectedBody;
            destJoint.anchor = Vector3.Scale(sourceJoint.anchor, scale);
            destJoint.axis = sourceJoint.axis;
            destJoint.swingAxis = sourceJoint.swingAxis;
            destJoint.autoConfigureConnectedAnchor = sourceJoint.autoConfigureConnectedAnchor;
            destJoint.connectedAnchor = Vector3.Scale(sourceJoint.connectedAnchor, scale);
            destJoint.lowTwistLimit = sourceJoint.lowTwistLimit;
            destJoint.highTwistLimit = sourceJoint.highTwistLimit;
            destJoint.swing1Limit = sourceJoint.swing1Limit;
            destJoint.swing2Limit = sourceJoint.swing2Limit;
            destJoint.enableProjection = sourceJoint.enableProjection;
            destJoint.projectionDistance = sourceJoint.projectionDistance;
            destJoint.projectionAngle = sourceJoint.projectionAngle;
            destJoint.breakForce = sourceJoint.breakForce;
            destJoint.breakTorque = sourceJoint.breakTorque;
            destJoint.enableCollision = sourceJoint.enableCollision;
            destJoint.enablePreprocessing = sourceJoint.enablePreprocessing;
            destJoint.massScale = sourceJoint.massScale;
            destJoint.connectedMassScale = sourceJoint.connectedMassScale;
        }

        private static void StripPhysicsComponents(GameObject source)
        {
            CharacterJoint joint = source.GetComponent<CharacterJoint>();
            if (joint != null)
                DestroyComponent(joint);

            Collider[] colliders = source.GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    DestroyComponent(colliders[i]);
            }

            Rigidbody body = source.GetComponent<Rigidbody>();
            if (body != null)
                DestroyComponent(body);
        }

        private static void DestroyComponent(Object component)
        {
            if (component == null)
                return;

            // Must be immediate: Awake remount is followed by physics-cache refresh /
            // LoadBodyPart in the same frame. Deferred Destroy would leave orphan RBs
            // in GetComponentsInChildren and ReleaseForRagdoll would wake the wrong armature.
            Object.DestroyImmediate(component);
        }
    }
}
