using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Stores authored vHitBox collider size so profile scale can be reapplied without compounding.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PioneerMeleeHitboxBaseline : MonoBehaviour
    {
        public Vector3 boxSize;
        public float capsuleRadius;
        public float capsuleHeight;
        public bool hasBox;
        public bool hasCapsule;

        public void CaptureIfNeeded(Collider trigger)
        {
            if (trigger == null)
                return;

            if (trigger is BoxCollider boxCollider)
            {
                if (hasBox)
                    return;
                boxSize = boxCollider.size;
                hasBox = true;
                return;
            }

            if (trigger is CapsuleCollider capsule)
            {
                if (hasCapsule)
                    return;
                capsuleRadius = capsule.radius;
                capsuleHeight = capsule.height;
                hasCapsule = true;
            }
        }

        public void ApplyScaled(float widthScale, float reachScale)
        {
            Collider trigger = GetComponent<Collider>();
            if (trigger is BoxCollider boxCollider && hasBox)
            {
                Vector3 size = boxSize;
                size.x *= widthScale;
                size.y *= widthScale;
                size.z *= reachScale;
                boxCollider.size = size;
                return;
            }

            if (trigger is CapsuleCollider capsule && hasCapsule)
            {
                capsule.radius = capsuleRadius * widthScale;
                capsule.height = capsuleHeight * reachScale;
            }
        }
    }
}
