using Invector;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// Keeps Invector BodySnaps / vSnapToBody on humanoid enemies so holstered and drawn weapons follow bones.
    /// </summary>
    public static class EnemyInvectorBodySnapSetup
    {
        private const string BodySnapsName = "BodySnaps";
        private const string InvectorComponentsName = "InvectorComponents";

        public static void ApplyRuntime(GameObject root)
        {
            if (root == null)
                return;

            vBodySnappingControl bodySnap = root.GetComponentInChildren<vBodySnappingControl>(true);
            if (bodySnap == null)
                return;

            bodySnap.LoadBones();
            WireSnapComponents(root, bodySnap);
            SnapWeaponContainersToLocalBones(root, bodySnap);
        }

        /// <summary>
        /// Reparent weapon slot containers onto this enemy's bones (same fix as companion pioneers).
        /// vSnapToBody.Start uses transform.root and breaks when hierarchies shift after ragdoll.
        /// </summary>
        public static void SnapWeaponContainersToLocalBones(GameObject root, vBodySnappingControl bodySnap = null)
        {
            if (root == null)
                return;

            if (bodySnap == null)
                bodySnap = root.GetComponentInChildren<vBodySnappingControl>(true);
            if (bodySnap == null)
                return;

            vSnapToBody[] snaps = root.GetComponentsInChildren<vSnapToBody>(true);
            for (int i = 0; i < snaps.Length; i++)
            {
                vSnapToBody snap = snaps[i];
                if (snap == null)
                    continue;

                Transform bone = snap.boneToSnap;
                if (bone == null && snap.boneName != vSnapToBody.manuallyAssignBone)
                    bone = bodySnap.GetBone(snap.boneName);

                if (bone != null)
                    snap.transform.SetParent(bone, true);

                Object.Destroy(snap);
            }
        }

        /// <summary>
        /// Reparent BodySnaps onto the character root before InvectorComponents is removed.
        /// </summary>
        public static void PreserveBeforeStrip(GameObject root)
        {
            if (root == null)
                return;

            Transform bodySnaps = FindBodySnapsTransform(root.transform);
            if (bodySnaps == null)
                return;

            if (bodySnaps.parent != root.transform)
                bodySnaps.SetParent(root.transform, true);
        }

        public static void WireSnapComponents(GameObject root, vBodySnappingControl bodySnap)
        {
            if (root == null || bodySnap == null)
                return;

            vSnapToBody[] snaps = root.GetComponentsInChildren<vSnapToBody>(true);
            for (int i = 0; i < snaps.Length; i++)
            {
                vSnapToBody snap = snaps[i];
                if (snap == null)
                    continue;

                snap.bodySnap = bodySnap;

                if (snap.boneToSnap == null &&
                    snap.boneName != vSnapToBody.manuallyAssignBone)
                {
                    Transform bone = bodySnap.GetBone(snap.boneName);
                    if (bone != null)
                        snap.boneToSnap = bone;
                }
            }
        }

        private static Transform FindBodySnapsTransform(Transform root)
        {
            Transform direct = root.Find(BodySnapsName);
            if (direct != null)
                return direct;

            Transform invectorComponents = root.Find(InvectorComponentsName);
            if (invectorComponents != null)
            {
                Transform nested = invectorComponents.Find(BodySnapsName);
                if (nested != null)
                    return nested;
            }

            return null;
        }
    }
}
