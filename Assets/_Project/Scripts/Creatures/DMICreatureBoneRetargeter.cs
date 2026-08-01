using System;
using UnityEngine;

namespace Project.Creatures
{
    /// <summary>
    /// Drives a foreign-skinned visual (Sulfur Hound armature) from Malbers AC bones
    /// without rebinding mesh weights. Preserves visual bindposes so the mesh stays intact;
    /// copies driver rotations (and hips position) with rest-pose offsets.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(50)]
    public class DMICreatureBoneRetargeter : MonoBehaviour
    {
        [Serializable]
        public struct BoneLink
        {
            public Transform driver;
            public Transform follower;
            public Quaternion rotationOffset;
            public Vector3 positionOffsetLocal;
            public bool copyPosition;
        }

        [SerializeField] private Transform acBoneRoot;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private BoneLink[] links = Array.Empty<BoneLink>();
        [SerializeField] private bool retargetInEditMode;

        public int LinkCount => links != null ? links.Length : 0;

        private void LateUpdate()
        {
            ApplyRetarget();
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (!Application.isPlaying && retargetInEditMode)
                ApplyRetarget();
        }
#endif

        public void ApplyRetarget()
        {
            if (links == null || links.Length == 0)
                return;

            for (int i = 0; i < links.Length; i++)
            {
                BoneLink link = links[i];
                if (link.driver == null || link.follower == null)
                    continue;

                link.follower.rotation = link.driver.rotation * link.rotationOffset;

                if (link.copyPosition)
                    link.follower.position = link.driver.TransformPoint(link.positionOffsetLocal);
            }
        }

        /// <summary>
        /// Builds links from Sulfur→AC name map using current rest poses as offsets.
        /// Call once in the editor after aligning the visual under the AC root.
        /// </summary>
        public int RebuildLinks(Transform animalRoot, Transform creatureVisualRoot)
        {
            acBoneRoot = animalRoot;
            visualRoot = creatureVisualRoot;

            if (animalRoot == null || creatureVisualRoot == null)
            {
                links = Array.Empty<BoneLink>();
                return 0;
            }

            var acBones = new System.Collections.Generic.Dictionary<string, Transform>();
            IndexByName(animalRoot, acBones);

            var built = new System.Collections.Generic.List<BoneLink>(32);
            var claimedDrivers = new System.Collections.Generic.HashSet<Transform>();
            Transform[] visualBones = creatureVisualRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < visualBones.Length; i++)
            {
                Transform follower = visualBones[i];
                if (follower == null)
                    continue;

                if (!DMICreatureBoneMap.SulfurToAc.TryGetValue(follower.name, out string acName))
                    continue;

                if (!acBones.TryGetValue(acName, out Transform driver) || driver == null)
                    continue;

                // Never drive two followers from the same AC bone (collapses limb chains).
                if (!claimedDrivers.Add(driver))
                    continue;

                bool copyPosition = DMICreatureBoneMap.ShouldCopyPosition(follower.name);
                BoneLink link = new BoneLink
                {
                    driver = driver,
                    follower = follower,
                    rotationOffset = Quaternion.Inverse(driver.rotation) * follower.rotation,
                    positionOffsetLocal = copyPosition
                        ? driver.InverseTransformPoint(follower.position)
                        : Vector3.zero,
                    copyPosition = copyPosition
                };
                built.Add(link);
            }

            links = built.ToArray();
            return links.Length;
        }

        private static void IndexByName(Transform root, System.Collections.Generic.Dictionary<string, Transform> map)
        {
            if (root == null || map == null)
                return;

            if (!map.ContainsKey(root.name))
                map.Add(root.name, root);

            for (int i = 0; i < root.childCount; i++)
                IndexByName(root.GetChild(i), map);
        }
    }
}
