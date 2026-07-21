using System.Collections.Generic;
using Project.Data;
using UnityEngine;

namespace Project.Interaction
{
    [DisallowMultipleComponent]
    public class WeaponHitbox : MonoBehaviour
    {
        public const string HitColliderChildName = "WeaponHitbox";

        [SerializeField] private float defaultRadius = 0.08f;
        [SerializeField] private float defaultLength = 0.75f;
        [Tooltip("Scales the auto-fit capsule radius. Lower = tighter hit volume.")]
        [SerializeField] private float radiusScale = 0.78f;
        [Tooltip("Extra local offset applied after tip placement. Tune in Inspector if a weapon's blade tip is still off.")]
        [SerializeField] private Vector3 hitboxLocalOffset = Vector3.zero;
        [Tooltip("0 = handle end of the primary axis, 1 = tip. Default 0.78 keeps volume on the blade without hugging only the tip.")]
        [SerializeField, Range(0.5f, 1f)] private float strikeEndBias = 0.78f;
        [Tooltip("Length of the hit capsule as a fraction of the weapon's primary axis span.")]
        [SerializeField, Range(0.2f, 1f)] private float strikeLengthScale = 0.58f;
        [SerializeField] private float activeWindowStart = 0.12f;
        [SerializeField] private float activeWindowEnd = 0.88f;

        private readonly HashSet<Transform> hitRoots = new HashSet<Transform>();
        private readonly RaycastHit[] sweepHits = new RaycastHit[32];

        private CapsuleCollider hitCollider;
        private MeleeCombatController owner;
        private ItemData swingItem;
        private bool swingCritical;
        private LayerMask swingLayers = ~0;
        private Transform ownerRoot;
        private float swingEndTime;
        private float swingStartTime;
        private float swingDuration;
        private bool swingActive;

        private Vector3 previousWorldCenter;
        private Quaternion previousWorldRotation;
        private float previousRadius;
        private float previousHalfHeight;
        private int previousDirection;
        private bool hasPreviousPose;

        public bool IsSwingActive => swingActive;

        public void Configure(Transform ownerTransform, ItemData item = null)
        {
            ownerRoot = ownerTransform != null ? ownerTransform : transform.root;
            EnsureHitCollider(item);
        }

        /// <summary>
        /// Builds or refits the child capsule collider. Safe for prefab authoring in the editor.
        /// </summary>
        public void RebuildHitCollider(ItemData item = null)
        {
            ownerRoot = transform;
            EnsureHitCollider(item);
        }

        public static void SetupPrefabRoot(GameObject root, ItemData item = null)
        {
            if (root == null)
                return;

            WeaponHitbox hitbox = root.GetComponent<WeaponHitbox>();
            if (hitbox == null)
                hitbox = root.AddComponent<WeaponHitbox>();

            hitbox.RebuildHitCollider(item);
        }

        public void BeginSwing(
            MeleeCombatController combatOwner,
            ItemData item,
            bool isCritical,
            LayerMask layers,
            float duration)
        {
            owner = combatOwner;
            swingItem = item;
            swingCritical = isCritical;
            swingLayers = layers;
            swingDuration = Mathf.Max(0.1f, duration);
            swingStartTime = Time.time;
            swingEndTime = swingStartTime + swingDuration;
            swingActive = true;
            hitRoots.Clear();
            hasPreviousPose = false;
            EnsureHitCollider(item);

            if (hitCollider != null)
                hitCollider.enabled = true;
        }

        public void EndSwing()
        {
            swingActive = false;
            owner = null;
            swingItem = null;
            hitRoots.Clear();
            hasPreviousPose = false;

            if (hitCollider != null)
                hitCollider.enabled = false;
        }

        private void Update()
        {
            if (!swingActive)
                return;

            if (Time.time >= swingEndTime)
            {
                EndSwing();
                return;
            }

            float normalizedTime = (Time.time - swingStartTime) / swingDuration;
            if (normalizedTime < activeWindowStart || normalizedTime > activeWindowEnd)
            {
                CapturePoseForNextFrame();
                return;
            }

            ScanOverlaps();
        }

        private void OnDisable()
        {
            EndSwing();
        }

        private void ScanOverlaps()
        {
            if (hitCollider == null || owner == null || swingItem == null)
                return;

            GetWorldCapsule(out Vector3 center, out Quaternion rotation, out float radius, out float halfHeight, out int direction);

            if (hasPreviousPose)
                SweepCapsule(previousWorldCenter, previousWorldRotation, previousRadius, previousHalfHeight, previousDirection,
                    center, rotation, radius, halfHeight, direction);

            OverlapCapsule(center, rotation, radius, halfHeight, direction);

            previousWorldCenter = center;
            previousWorldRotation = rotation;
            previousRadius = radius;
            previousHalfHeight = halfHeight;
            previousDirection = direction;
            hasPreviousPose = true;
        }

        private void CapturePoseForNextFrame()
        {
            if (hitCollider == null)
                return;

            GetWorldCapsule(out previousWorldCenter, out previousWorldRotation, out previousRadius, out previousHalfHeight, out previousDirection);
            hasPreviousPose = true;
        }

        private void GetWorldCapsule(
            out Vector3 worldCenter,
            out Quaternion worldRotation,
            out float radius,
            out float halfHeight,
            out int direction)
        {
            Transform capsuleTransform = hitCollider.transform;
            worldRotation = capsuleTransform.rotation;
            radius = hitCollider.radius * MaxAbsScale(capsuleTransform.lossyScale, hitCollider.direction);
            halfHeight = Mathf.Max(hitCollider.height * 0.5f, radius + 0.01f);
            halfHeight *= MaxAbsScale(capsuleTransform.lossyScale, hitCollider.direction);
            direction = hitCollider.direction;
            worldCenter = capsuleTransform.TransformPoint(hitCollider.center);
        }

        private void OverlapCapsule(Vector3 center, Quaternion rotation, float radius, float halfHeight, int direction)
        {
            GetCapsulePoints(center, rotation, radius, halfHeight, direction, out Vector3 pointA, out Vector3 pointB);
            Collider[] overlaps = Physics.OverlapCapsule(
                pointA,
                pointB,
                radius,
                swingLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < overlaps.Length; i++)
                TryApplyHit(overlaps[i]);
        }

        private void SweepCapsule(
            Vector3 fromCenter,
            Quaternion fromRotation,
            float fromRadius,
            float fromHalfHeight,
            int fromDirection,
            Vector3 toCenter,
            Quaternion toRotation,
            float toRadius,
            float toHalfHeight,
            int toDirection)
        {
            GetCapsulePoints(fromCenter, fromRotation, fromRadius, fromHalfHeight, fromDirection, out Vector3 fromA, out Vector3 fromB);
            GetCapsulePoints(toCenter, toRotation, toRadius, toHalfHeight, toDirection, out Vector3 toA, out Vector3 toB);

            Vector3 move = toCenter - fromCenter;
            float distance = move.magnitude;
            if (distance < 0.0001f)
                return;

            Vector3 direction = move / distance;
            float castRadius = Mathf.Max(fromRadius, toRadius);
            int hitCount = Physics.CapsuleCastNonAlloc(
                fromA,
                fromB,
                castRadius,
                direction,
                sweepHits,
                distance,
                swingLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
                TryApplyHit(sweepHits[i].collider);
        }

        private static void GetCapsulePoints(
            Vector3 center,
            Quaternion rotation,
            float radius,
            float halfHeight,
            int direction,
            out Vector3 pointA,
            out Vector3 pointB)
        {
            Vector3 axis = direction switch
            {
                0 => rotation * Vector3.right,
                1 => rotation * Vector3.up,
                _ => rotation * Vector3.forward
            };

            float segment = Mathf.Max(0f, halfHeight - radius);
            pointA = center + axis * segment;
            pointB = center - axis * segment;
        }

        private static float MaxAbsScale(Vector3 scale, int direction)
        {
            return direction switch
            {
                0 => Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)),
                1 => Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)),
                _ => Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y))
            };
        }

        private void TryApplyHit(Collider other)
        {
            if (!swingActive || owner == null || swingItem == null || other == null)
                return;

            if (IsIgnoredCollider(other))
                return;

            Transform hitRoot = other.attachedRigidbody != null
                ? other.attachedRigidbody.transform.root
                : other.transform.root;

            if (!hitRoots.Add(hitRoot))
                return;

            owner.ProcessWeaponHit(other, swingItem, swingCritical);
        }

        private bool IsIgnoredCollider(Collider collider)
        {
            if (collider == null)
                return true;

            if (collider.GetComponentInParent<WeaponHitbox>() != null)
                return true;

            Transform root = ownerRoot != null ? ownerRoot : transform.root;
            if (collider.transform.IsChildOf(root) || collider.gameObject == root.gameObject)
                return true;

            return collider.CompareTag("Player");
        }

        private void EnsureHitCollider(ItemData item)
        {
            if (hitCollider != null)
            {
                FitColliderToWeapon(hitCollider.transform, hitCollider, item);
                return;
            }

            Transform hitboxTransform = transform.Find(HitColliderChildName);
            GameObject hitboxObject;
            if (hitboxTransform == null)
            {
                hitboxObject = new GameObject(HitColliderChildName);
                hitboxObject.transform.SetParent(transform, false);
                hitboxTransform = hitboxObject.transform;
            }
            else
            {
                hitboxObject = hitboxTransform.gameObject;
            }

            BoxCollider legacyBox = hitboxObject.GetComponent<BoxCollider>();
            if (legacyBox != null)
                DestroyUnityObject(legacyBox);

            hitCollider = hitboxObject.GetComponent<CapsuleCollider>();
            if (hitCollider == null)
                hitCollider = hitboxObject.AddComponent<CapsuleCollider>();

            Rigidbody body = hitboxObject.GetComponent<Rigidbody>();
            if (body == null)
                body = hitboxObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            hitCollider.isTrigger = true;
            hitCollider.enabled = false;
            FitColliderToWeapon(hitboxTransform, hitCollider, item);
        }

        private void FitColliderToWeapon(Transform hitboxTransform, CapsuleCollider capsule, ItemData item)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                hitboxTransform.localPosition = hitboxLocalOffset;
                hitboxTransform.localRotation = Quaternion.identity;
                capsule.direction = 2;
                float fallbackLength = defaultLength * strikeLengthScale;
                capsule.center = DirectionalCenter(2, fallbackLength);
                capsule.height = fallbackLength;
                capsule.radius = defaultRadius;
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || renderer.transform == hitboxTransform)
                    continue;

                bounds.Encapsulate(renderer.bounds);
            }

            Vector3 localMin = transform.InverseTransformPoint(bounds.min);
            Vector3 localMax = transform.InverseTransformPoint(bounds.max);
            Vector3 localSize = localMax - localMin;
            localSize.x = Mathf.Abs(localSize.x);
            localSize.y = Mathf.Abs(localSize.y);
            localSize.z = Mathf.Abs(localSize.z);

            int direction = 0;
            float axisSpan = localSize.x;
            if (localSize.y > axisSpan)
            {
                direction = 1;
                axisSpan = localSize.y;
            }

            if (localSize.z > axisSpan)
            {
                direction = 2;
                axisSpan = localSize.z;
            }

            Vector3 axisVector = AxisVector(direction);
            float minAlong = Vector3.Dot(localMin, axisVector);
            float maxAlong = Vector3.Dot(localMax, axisVector);
            if (minAlong > maxAlong)
            {
                float swap = minAlong;
                minAlong = maxAlong;
                maxAlong = swap;
            }

            axisSpan = Mathf.Max(maxAlong - minAlong, 0.01f);
            float strikeLength = Mathf.Max(axisSpan * strikeLengthScale, defaultRadius * 2.5f);
            if (item != null && item.meleeRange > 0f)
                strikeLength = Mathf.Clamp(strikeLength, defaultRadius * 2.5f, item.meleeRange * 0.55f);

            float radius = direction switch
            {
                0 => Mathf.Min(localSize.y, localSize.z) * 0.5f,
                1 => Mathf.Min(localSize.x, localSize.z) * 0.5f,
                _ => Mathf.Min(localSize.x, localSize.y) * 0.5f
            };

            radius = Mathf.Clamp(radius * radiusScale, 0.04f, 0.18f);
            strikeLength = Mathf.Max(strikeLength, radius * 2.5f);

            float tipAlong = Mathf.Lerp(minAlong, maxAlong, strikeEndBias);
            Vector3 tipLocalPosition = axisVector * tipAlong;
            hitboxTransform.localPosition = tipLocalPosition + hitboxLocalOffset;
            hitboxTransform.localRotation = Quaternion.identity;
            capsule.direction = direction;
            capsule.radius = radius;
            capsule.height = strikeLength;
            // Capsule extends backward from the tip toward the handle.
            capsule.center = -DirectionalCenter(direction, strikeLength);
        }

        private static Vector3 AxisVector(int direction) =>
            direction switch
            {
                0 => Vector3.right,
                1 => Vector3.up,
                _ => Vector3.forward
            };

        private static Vector3 DirectionalCenter(int direction, float length)
        {
            float half = length * 0.5f;
            return direction switch
            {
                0 => new Vector3(half, 0f, 0f),
                1 => new Vector3(0f, half, 0f),
                _ => new Vector3(0f, 0f, half)
            };
        }

        private static void DestroyUnityObject(UnityEngine.Object target)
        {
            if (target == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(target);
                return;
            }
#endif
            UnityEngine.Object.Destroy(target);
        }
    }
}
