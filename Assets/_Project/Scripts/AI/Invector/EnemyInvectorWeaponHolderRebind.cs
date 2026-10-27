using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// After Meshy/custom humanoid avatar swap, Invector weapon holders stay under the stock VBOT
    /// armature while the Animator drives Meshy bones. Reparents holders onto the live humanoid bones
    /// so drawn weapons and melee hitboxes follow the visible attack swing.
    /// </summary>
    public static class EnemyInvectorWeaponHolderRebind
    {
        private static readonly string[] RightHandHolderNames =
        {
            "RightHandlers",
            "RightHand",
        };

        private static readonly string[] LeftHandHolderNames =
        {
            "LeftHandlers",
            "LeftHand",
        };

        private static readonly string[] HipHolderNames =
        {
            "HandgunHolder",
            "RightUpLeg",
        };

        private static readonly string[] TorsoHolderNames =
        {
            "WeaponHolders",
            "RifleHolder",
            "Spine2",
        };

        /// <summary>Editor hook (set by DMWeaponPlacementBake): applies baked weapon placements after every holder rebind, so Rebuild / Fix keep hand-tweaked placements.</summary>
        public static System.Action<GameObject> AfterRebind;

        public static void RebindToAnimatorBones(GameObject root)
        {
            RebindToAnimatorBonesCore(root);
            if (root != null && AfterRebind != null)
                AfterRebind(root);
        }

        private static void RebindToAnimatorBonesCore(GameObject root)
        {
            if (root == null)
                return;

            Animator animator = root.GetComponent<Animator>();
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
                return;

            Transform meshyRightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            Transform meshyLeftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform meshyRightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            Transform meshyChest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
            if (meshyChest == null)
                meshyChest = animator.GetBoneTransform(HumanBodyBones.Chest);
            if (meshyChest == null)
                meshyChest = animator.GetBoneTransform(HumanBodyBones.Spine);

            if (meshyRightHand == null)
                return;

            // Search stock VBOT ("3D Model") AND player BodySnaps proxies — both can host holders
            // that no longer follow the Animator after a Meshy/T-pose avatar swap.
            Transform[] searchRoots = CollectHolderSearchRoots(root.transform);
            for (int r = 0; r < searchRoots.Length; r++)
            {
                Transform searchRoot = searchRoots[r];
                if (searchRoot == null)
                    continue;

                RebindNamedHoldersUnderStock(searchRoot, RightHandHolderNames, meshyRightHand);
                if (meshyLeftHand != null)
                    RebindNamedHoldersUnderStock(searchRoot, LeftHandHolderNames, meshyLeftHand);
                if (meshyRightUpperLeg != null)
                    RebindNamedHoldersUnderStock(searchRoot, HipHolderNames, meshyRightUpperLeg);
                if (meshyChest != null)
                    RebindNamedHoldersUnderStock(searchRoot, TorsoHolderNames, meshyChest);

                ReparentOrphanWeaponSlots(searchRoot, meshyRightHand, meshyRightUpperLeg, meshyChest);
            }

            RebindOrphansUnderVisual(root.transform, animator, meshyRightHand, meshyLeftHand, meshyRightUpperLeg, meshyChest);
        }

        /// <summary>
        /// Holders left directly under the Visual (not under any live humanoid bone, e.g. after a mesh swap) are
        /// moved onto the matching bone. Holders already on a humanoid bone are never touched, so hand-tuned
        /// placements survive.
        /// </summary>
        private static void RebindOrphansUnderVisual(
            Transform root,
            Animator animator,
            Transform rightHand,
            Transform leftHand,
            Transform rightUpperLeg,
            Transform chest)
        {
            Transform visual = root.Find("Visual");
            if (visual == null)
                return;

            var bones = new System.Collections.Generic.HashSet<Transform>();
            for (int i = 0; i < (int)HumanBodyBones.LastBone; i++)
            {
                Transform b = animator.GetBoneTransform((HumanBodyBones)i);
                if (b != null)
                    bones.Add(b);
            }

            Transform[] all = visual.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform node = all[i];
                if (node == null || bones.Contains(node) || HasAncestorIn(node, bones, visual))
                    continue;

                string name = node.name;
                Transform destination = null;
                if (System.Array.IndexOf(RightHandHolderNames, name) >= 0 || name.StartsWith("Drawn_", System.StringComparison.Ordinal))
                    destination = rightHand;
                else if (System.Array.IndexOf(LeftHandHolderNames, name) >= 0)
                    destination = leftHand;
                else if (System.Array.IndexOf(HipHolderNames, name) >= 0 || name.StartsWith("Holstered_", System.StringComparison.Ordinal))
                    destination = rightUpperLeg != null ? rightUpperLeg : chest;
                else if (System.Array.IndexOf(TorsoHolderNames, name) >= 0)
                    destination = chest;

                if (destination != null && !destination.IsChildOf(node))
                    node.SetParent(destination, true);
            }
        }

        private static bool HasAncestorIn(Transform node, System.Collections.Generic.HashSet<Transform> set, Transform stop)
        {
            Transform cur = node.parent;
            while (cur != null && cur != stop)
            {
                if (set.Contains(cur))
                    return true;
                cur = cur.parent;
            }

            return false;
        }

        private static Transform[] CollectHolderSearchRoots(Transform root)
        {
            var roots = new System.Collections.Generic.List<Transform>(4);

            Transform stockModel = root.Find("3D Model");
            if (stockModel != null)
                roots.Add(stockModel);

            Transform bodySnaps = root.Find("BodySnaps");
            if (bodySnaps == null)
            {
                Transform invectorComponents = root.Find("InvectorComponents");
                if (invectorComponents != null)
                    bodySnaps = invectorComponents.Find("BodySnaps");
            }

            if (bodySnaps != null)
                roots.Add(bodySnaps);

            return roots.ToArray();
        }

        private static void RebindNamedHoldersUnderStock(
            Transform stockModel,
            string[] holderNames,
            Transform meshyBone)
        {
            if (stockModel == null || meshyBone == null || holderNames == null)
                return;

            System.Collections.Generic.HashSet<Transform> moved =
                new System.Collections.Generic.HashSet<Transform>();

            for (int i = 0; i < holderNames.Length; i++)
            {
                string name = holderNames[i];
                Transform[] matches = FindAllNamedUnder(stockModel, name);
                for (int m = 0; m < matches.Length; m++)
                {
                    Transform holder = matches[m];
                    if (holder == null || IsUnder(holder, meshyBone))
                        continue;

                    if (!IsLikelyWeaponHolder(holder, name))
                        continue;

                    // If RightHandlers sits under a local "RightHand"/"LeftHand"/"RightUpLeg"/"Spine2"
                    // BodySnaps socket, move that socket so handler local offsets stay valid.
                    Transform moveTarget = holder;
                    if (holder.parent != null &&
                        IsBodySnapSocketName(holder.parent.name) &&
                        IsUnderStockModel(holder.parent, stockModel))
                    {
                        moveTarget = holder.parent;
                    }

                    if (moveTarget.parent == meshyBone || moved.Contains(moveTarget))
                        continue;

                    // Skip if an ancestor was already moved this pass.
                    bool ancestorMoved = false;
                    Transform walk = moveTarget.parent;
                    while (walk != null)
                    {
                        if (moved.Contains(walk))
                        {
                            ancestorMoved = true;
                            break;
                        }
                        walk = walk.parent;
                    }

                    if (ancestorMoved)
                        continue;

                    // Preserve authored local offsets (manual weapon grip posing) when reparenting
                    // onto Meshy bones. Do not force hand sockets to identity — that leaked from a
                    // Player V5 arm-twist experiment and fought user / enemy holder layouts.
                    moveTarget.SetParent(meshyBone, true);
                    moved.Add(moveTarget);
                }
            }
        }

        private static void ReparentOrphanWeaponSlots(
            Transform stockModel,
            Transform rightHand,
            Transform rightUpperLeg,
            Transform chest)
        {
            if (stockModel == null)
                return;

            Transform[] all = stockModel.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform slot = all[i];
                if (slot == null)
                    continue;

                bool isDrawn = slot.name.StartsWith("Drawn_", System.StringComparison.Ordinal);
                bool isHolstered = slot.name.StartsWith("Holstered_", System.StringComparison.Ordinal);
                if (!isDrawn && !isHolstered)
                    continue;

                if (rightHand != null && IsUnder(slot, rightHand))
                    continue;
                if (rightUpperLeg != null && IsUnder(slot, rightUpperLeg))
                    continue;
                if (chest != null && IsUnder(slot, chest))
                    continue;

                Transform destination = isDrawn ? rightHand : (rightUpperLeg != null ? rightUpperLeg : chest);
                if (destination == null)
                    destination = rightHand;
                if (destination == null)
                    continue;

                // Keep parent handler if already rebound; only move true orphans.
                if (slot.parent != null &&
                    (slot.parent.name.IndexOf("Handler", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     slot.parent.name.IndexOf("Holder", System.StringComparison.OrdinalIgnoreCase) >= 0))
                    continue;

                slot.SetParent(destination, true);
            }
        }

        private static bool IsLikelyWeaponHolder(Transform holder, string expectedName)
        {
            if (holder == null)
                return false;

            // Bone itself named RightHand under Visual must not be moved.
            if (holder.name.StartsWith("VBOT_:", System.StringComparison.OrdinalIgnoreCase))
                return false;

            string path = holder.name;
            Transform p = holder.parent;
            if (p != null)
                path = p.name + "/" + holder.name;

            // Prefer containers that already host Drawn_/Holstered_ or nested Handlers/Holders.
            if (HasWeaponSlotDescendant(holder))
                return true;

            if (holder.name.Equals("RightHandlers", System.StringComparison.Ordinal) ||
                holder.name.Equals("LeftHandlers", System.StringComparison.Ordinal) ||
                holder.name.Equals("WeaponHolders", System.StringComparison.Ordinal) ||
                holder.name.Equals("HandgunHolder", System.StringComparison.Ordinal) ||
                holder.name.Equals("RifleHolder", System.StringComparison.Ordinal))
                return true;

            // Intermediate sockets created by Invector (RightHand under VBOT_:RightHand).
            if (holder.name.Equals(expectedName, System.StringComparison.Ordinal) &&
                p != null &&
                p.name.StartsWith("VBOT_:", System.StringComparison.OrdinalIgnoreCase))
                return true;

            return path.IndexOf("Handlers", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   path.IndexOf("Holder", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool HasWeaponSlotDescendant(Transform root)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == null)
                    continue;
                if (child.name.StartsWith("Drawn_", System.StringComparison.Ordinal) ||
                    child.name.StartsWith("Holstered_", System.StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static Transform[] FindAllNamedUnder(Transform root, string name)
        {
            System.Collections.Generic.List<Transform> found = new System.Collections.Generic.List<Transform>(4);
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name.Equals(name, System.StringComparison.Ordinal))
                    found.Add(all[i]);
            }

            return found.ToArray();
        }

        private static bool IsUnder(Transform child, Transform ancestor)
        {
            if (child == null || ancestor == null)
                return false;

            Transform cur = child;
            while (cur != null)
            {
                if (cur == ancestor)
                    return true;
                cur = cur.parent;
            }

            return false;
        }

        private static bool IsUnderStockModel(Transform node, Transform stockModel)
        {
            return IsUnder(node, stockModel);
        }

        private static bool IsBodySnapSocketName(string name)
        {
            return name.Equals("RightHand", System.StringComparison.Ordinal) ||
                   name.Equals("LeftHand", System.StringComparison.Ordinal) ||
                   name.Equals("RightUpLeg", System.StringComparison.Ordinal) ||
                   name.Equals("Spine2", System.StringComparison.Ordinal);
        }
    }
}
