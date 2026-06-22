using System.Collections.Generic;
using Project.Data;
using UnityEngine;

namespace Project.Interaction
{
    [DisallowMultipleComponent]
    public class WeaponHitbox : MonoBehaviour
    {
        [SerializeField] private BoxCollider hitCollider;
        [SerializeField] private float defaultHalfExtent = 0.35f;
        [SerializeField] private Vector3 hitboxLocalOffset = new Vector3(0f, 0f, 0.25f);

        private readonly HashSet<Transform> hitRoots = new HashSet<Transform>();

        private MeleeCombatController owner;
        private ItemData swingItem;
        private bool swingCritical;
        private LayerMask swingLayers = ~0;
        private Transform ownerRoot;
        private float swingEndTime;
        private bool swingActive;

        public bool IsSwingActive => swingActive;

        public void Configure(Transform ownerTransform)
        {
            ownerRoot = ownerTransform != null ? ownerTransform : transform.root;
            EnsureHitCollider();
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
            swingEndTime = Time.time + Mathf.Max(0.1f, duration);
            swingActive = true;
            hitRoots.Clear();
            EnsureHitCollider();

            if (hitCollider != null)
                hitCollider.enabled = true;
        }

        public void EndSwing()
        {
            swingActive = false;
            owner = null;
            swingItem = null;
            hitRoots.Clear();

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

            Vector3 center = hitCollider.transform.TransformPoint(hitCollider.center);
            Vector3 halfExtents = Vector3.Scale(hitCollider.size * 0.5f, hitCollider.transform.lossyScale);
            Collider[] overlaps = Physics.OverlapBox(
                center,
                halfExtents,
                hitCollider.transform.rotation,
                swingLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < overlaps.Length; i++)
                TryApplyHit(overlaps[i]);
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

            Transform root = ownerRoot != null ? ownerRoot : transform.root;
            if (collider.transform.IsChildOf(root) || collider.gameObject == root.gameObject)
                return true;

            return collider.CompareTag("Player");
        }

        private void EnsureHitCollider()
        {
            if (hitCollider != null)
                return;

            Transform hitboxTransform = transform.Find("WeaponHitbox");
            GameObject hitboxObject;
            if (hitboxTransform == null)
            {
                hitboxObject = new GameObject("WeaponHitbox");
                hitboxObject.transform.SetParent(transform, false);
                hitboxTransform = hitboxObject.transform;
            }
            else
            {
                hitboxObject = hitboxTransform.gameObject;
            }

            hitCollider = hitboxObject.GetComponent<BoxCollider>();
            if (hitCollider == null)
                hitCollider = hitboxObject.AddComponent<BoxCollider>();

            FitColliderToVisuals(hitboxTransform, hitCollider);

            Rigidbody body = hitboxObject.GetComponent<Rigidbody>();
            if (body == null)
                body = hitboxObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            hitCollider.isTrigger = true;
            hitCollider.enabled = false;
        }

        private void FitColliderToVisuals(Transform hitboxTransform, BoxCollider box)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                hitboxTransform.localPosition = hitboxLocalOffset;
                box.center = Vector3.zero;
                box.size = Vector3.one * defaultHalfExtent * 2f;
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] == null || renderers[i].transform == hitboxTransform)
                    continue;

                bounds.Encapsulate(renderers[i].bounds);
            }

            Vector3 localCenter = transform.InverseTransformPoint(bounds.center);
            Vector3 localSize = transform.InverseTransformVector(bounds.size);
            localSize.x = Mathf.Max(Mathf.Abs(localSize.x), defaultHalfExtent);
            localSize.y = Mathf.Max(Mathf.Abs(localSize.y), defaultHalfExtent * 0.5f);
            localSize.z = Mathf.Max(Mathf.Abs(localSize.z), defaultHalfExtent);

            hitboxTransform.localPosition = localCenter + hitboxLocalOffset;
            hitboxTransform.localRotation = Quaternion.identity;
            box.center = Vector3.zero;
            box.size = localSize;
        }
    }
}
