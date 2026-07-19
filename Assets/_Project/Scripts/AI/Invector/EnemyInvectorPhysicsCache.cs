using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// Caches humanoid rigidbody references so bone stabilization does not scan the hierarchy every frame.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyInvectorPhysicsCache : MonoBehaviour
    {
        private Rigidbody _rootBody;
        private Rigidbody[] _boneBodies;
        private bool _bonesStable;

        public bool BonesStable => _bonesStable;

        private void Awake()
        {
            Refresh();
        }

        public void Refresh()
        {
            _rootBody = GetComponent<Rigidbody>();
            Rigidbody[] allBodies = GetComponentsInChildren<Rigidbody>(true);
            if (allBodies == null || allBodies.Length == 0)
            {
                _boneBodies = System.Array.Empty<Rigidbody>();
                _bonesStable = false;
                return;
            }

            int boneCount = 0;
            for (int i = 0; i < allBodies.Length; i++)
            {
                Rigidbody body = allBodies[i];
                if (body != null && body != _rootBody)
                    boneCount++;
            }

            if (boneCount == 0)
            {
                _boneBodies = System.Array.Empty<Rigidbody>();
            }
            else
            {
                _boneBodies = new Rigidbody[boneCount];
                int write = 0;
                for (int i = 0; i < allBodies.Length; i++)
                {
                    Rigidbody body = allBodies[i];
                    if (body == null || body == _rootBody)
                        continue;

                    _boneBodies[write++] = body;
                }
            }

            _bonesStable = false;
        }

        public void StabilizeAllRigidbodies()
        {
            if (_boneBodies == null)
                Refresh();

            StabilizeBody(_rootBody);
            StabilizeBoneArray(_boneBodies);
            _bonesStable = _boneBodies.Length == 0 || AllBonesStable();
        }

        public void StabilizeBonesIfNeeded()
        {
            if (_boneBodies == null)
                Refresh();

            if (_bonesStable)
                return;

            StabilizeBody(_rootBody);
            StabilizeBoneArray(_boneBodies);
            _bonesStable = _boneBodies.Length == 0 || AllBonesStable();
        }

        public void MarkBonesUnstable()
        {
            _bonesStable = false;
        }

        public void ReleaseBonesForRagdoll()
        {
            if (_boneBodies == null)
                Refresh();

            MarkBonesUnstable();
            RestoreBonePhysicsLayers();

            for (int i = 0; i < _boneBodies.Length; i++)
            {
                Rigidbody body = _boneBodies[i];
                if (body == null)
                    continue;

                EnableBoneColliders(body);
                body.isKinematic = false;
                body.useGravity = true;
                body.constraints = RigidbodyConstraints.None;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        public void RestoreBonePhysicsLayers()
        {
            if (_boneBodies == null)
                Refresh();

            int physicsLayer = LayerMask.NameToLayer("BodyPart");
            if (physicsLayer < 0)
                physicsLayer = 0;

            for (int i = 0; i < _boneBodies.Length; i++)
            {
                Rigidbody body = _boneBodies[i];
                if (body != null)
                    body.gameObject.layer = physicsLayer;
            }
        }

        private bool AllBonesStable()
        {
            for (int i = 0; i < _boneBodies.Length; i++)
            {
                Rigidbody body = _boneBodies[i];
                if (body == null)
                    continue;

                if (!body.isKinematic || body.useGravity)
                    return false;
            }

            return true;
        }

        private static void StabilizeBody(Rigidbody body)
        {
            if (body == null)
                return;

            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            body.isKinematic = true;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
        }

        private static void StabilizeBoneArray(Rigidbody[] bodies)
        {
            if (bodies == null || bodies.Length == 0)
                return;

            for (int i = 0; i < bodies.Length; i++)
                StabilizeBody(bodies[i]);
        }

        // Generous upper bounds for a single ragdoll bone's collider. A capsule/box/sphere on one bone
        // (forearm, shin, etc.) should never legitimately need dimensions anywhere near these — a whole
        // human is ~1.8-2m tall. Anything past this is almost certainly a broken/mis-scaled collider
        // (e.g. an FBX import-scale mismatch baked into the bone transform data) rather than a real
        // design choice, even for oversized enemies.
        public const float MaxPlausibleBoneColliderRadius = 1f;
        public const float MaxPlausibleBoneColliderLength = 3f;

        private static void EnableBoneColliders(Rigidbody body)
        {
            Collider[] colliders = body.GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null)
                    continue;

                if (IsImplausiblyOversized(collider, out string sizeDescription))
                {
                    // Enabling this collider would make the bone massively overlap the ground and
                    // neighboring bones the instant ragdoll physics takes over; PhysX's depenetration
                    // response would then fling it (and anything jointed to it) off the map instead of
                    // a normal collapse. Leave it disabled and warn — the bone still ragdolls via its
                    // CharacterJoint, just without solid collision, which is far less jarring than an
                    // explosion.
                    Debug.LogWarning(
                        $"{body.name}: collider size implausible for a ragdoll bone ({sizeDescription}); " +
                        "left disabled to avoid a physics explosion. This prefab's ragdoll collider needs to be rebuilt.",
                        collider);
                    continue;
                }

                collider.enabled = true;
                collider.isTrigger = false;
            }
        }

        /// <summary>
        /// Checks a collider's actual real-world size, not its raw local dimensions. Rigs commonly wrap
        /// their bone hierarchy in an "Armature"-style parent with a small localScale (e.g. 0.01, a
        /// standard centimeters-to-meters conversion) — under that parent, a bone's local radius/height/
        /// size fields will legitimately be large numbers even though the resulting world-space collider
        /// is perfectly normal. Comparing those raw local numbers directly against a meters threshold, as
        /// this method previously did, produces false positives on any such rig. Multiplying by the
        /// collider's own Transform.lossyScale first gives the true effective size.
        /// </summary>
        public static bool IsImplausiblyOversized(Collider collider, out string sizeDescription)
        {
            Vector3 lossyScale = collider.transform.lossyScale;

            switch (collider)
            {
                case CapsuleCollider capsule:
                {
                    int heightAxis = capsule.direction;
                    int radiusAxisA = heightAxis == 0 ? 1 : 0;
                    int radiusAxisB = heightAxis == 2 ? 1 : 2;
                    float heightScale = AxisScale(lossyScale, heightAxis);
                    float radiusScale = Mathf.Max(AxisScale(lossyScale, radiusAxisA), AxisScale(lossyScale, radiusAxisB));

                    float effectiveRadius = capsule.radius * radiusScale;
                    float effectiveHeight = capsule.height * heightScale;
                    sizeDescription =
                        $"radius={capsule.radius:0.##} (world {effectiveRadius:0.##}), " +
                        $"height={capsule.height:0.##} (world {effectiveHeight:0.##})";
                    return effectiveRadius > MaxPlausibleBoneColliderRadius ||
                           effectiveHeight > MaxPlausibleBoneColliderLength;
                }

                case BoxCollider box:
                {
                    Vector3 effectiveSize = Vector3.Scale(box.size, lossyScale);
                    sizeDescription = $"size={box.size} (world {effectiveSize})";
                    return effectiveSize.x > MaxPlausibleBoneColliderLength ||
                           effectiveSize.y > MaxPlausibleBoneColliderLength ||
                           effectiveSize.z > MaxPlausibleBoneColliderLength;
                }

                case SphereCollider sphere:
                {
                    // Unity scales a sphere collider by the largest absolute axis of lossyScale.
                    float scale = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y), Mathf.Abs(lossyScale.z));
                    float effectiveRadius = sphere.radius * scale;
                    sizeDescription = $"radius={sphere.radius:0.##} (world {effectiveRadius:0.##})";
                    return effectiveRadius > MaxPlausibleBoneColliderRadius;
                }

                default:
                    sizeDescription = string.Empty;
                    return false;
            }
        }

        private static float AxisScale(Vector3 lossyScale, int axis)
        {
            switch (axis)
            {
                case 0: return Mathf.Abs(lossyScale.x);
                case 2: return Mathf.Abs(lossyScale.z);
                default: return Mathf.Abs(lossyScale.y);
            }
        }
    }
}
