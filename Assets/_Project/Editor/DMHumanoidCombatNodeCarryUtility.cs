#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Player visual swap: before the old Visual is destroyed, lifts weapon / holster / handler / hit-volume
    /// nodes out of it and remembers their parent bone by HumanBodyBones identity (not by name). After the new
    /// Visual + avatar are bound, puts each node back under the matching new bone with its original local pose,
    /// and re-points serialized references (managers, handlers, bridges) that targeted old Visual bones.
    /// </summary>
    public static class DMHumanoidCombatNodeCarryUtility
    {
        const string HolderName = "__DMCombatNodeCarry";

        internal sealed class NodeRecord
        {
            public Transform Node;
            public bool HasBone;
            public HumanBodyBones Bone;
            public List<string> PathFromBone = new List<string>();
            public Vector3 LocalPos;
            public Quaternion LocalRot;
            public Vector3 LocalScale;
            public Vector3 AnchorPos;
            public Quaternion AnchorRot;
            // T-pose, player-root frame: node relative to its anchor bone.
            public Vector3 OffsetPos;
            public Quaternion OffsetRot = Quaternion.identity;
            public float OldLimb;
            public Transform FinalParent;
            public Vector3 FinalLocalPos;
            public Quaternion FinalLocalRot;
            public readonly List<(Transform t, Transform parent, Vector3 p, Quaternion r, Vector3 s)> Inner =
                new List<(Transform, Transform, Vector3, Quaternion, Vector3)>();
        }

        internal sealed class RefRecord
        {
            public Object Owner;
            public string PropertyPath;
            public bool HasBone;
            public HumanBodyBones Bone;
            public List<string> PathFromBone = new List<string>();
            public System.Type TargetType;
            public Component Stash;
        }

        public sealed class CarryState
        {
            internal Transform Holder;
            internal Transform Stash;
            internal readonly Dictionary<Component, Component> Stashed = new Dictionary<Component, Component>();
            internal readonly List<NodeRecord> Nodes = new List<NodeRecord>();
            internal readonly List<RefRecord> Refs = new List<RefRecord>();
            internal GameObject Root;
            public int NodeCount => Nodes.Count;
            public int RefCount => Refs.Count;
        }

        static readonly string[] SocketNames =
        {
            "RightHand", "LeftHand", "RightUpLeg", "Spine2", "WeaponHitbox",
            "hitBox", "leftFoot_trigger", "rightFoot_trigger"
        };

        public static CarryState Capture(GameObject root, string visualChildName)
        {
            if (root == null)
                return null;
            Transform oldVisual = root.transform.Find(string.IsNullOrEmpty(visualChildName) ? "Visual" : visualChildName);
            if (oldVisual == null)
                return null;

            Animator animator = root.GetComponent<Animator>();
            var boneOf = new Dictionary<Transform, HumanBodyBones>();
            if (animator != null && animator.avatar != null && animator.avatar.isHuman)
            {
                if (animator.avatar.isValid)
                {
                    animator.Rebind();
                    animator.Update(0f);
                }

                for (int i = 0; i < (int)HumanBodyBones.LastBone; i++)
                {
                    Transform t = animator.GetBoneTransform((HumanBodyBones)i);
                    if (t != null && t.IsChildOf(oldVisual) && !boneOf.ContainsKey(t))
                        boneOf[t] = (HumanBodyBones)i;
                }
            }

            // Measure in the template avatar's T-pose so offsets are rig-independent.
            if (animator != null)
                PoseToSkeleton(oldVisual, animator.avatar);
            var oldHuman = new HashSet<Transform>(boneOf.Keys);

            var skinBones = new HashSet<Transform>();
            foreach (SkinnedMeshRenderer smr in oldVisual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr == null || smr.bones == null)
                    continue;
                foreach (Transform b in smr.bones)
                    if (b != null)
                        skinBones.Add(b);
            }

            var state = new CarryState { Root = root };
            Transform[] all = oldVisual.GetComponentsInChildren<Transform>(true);
            var picked = new List<Transform>();
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null || t == oldVisual || boneOf.ContainsKey(t) || skinBones.Contains(t))
                    continue;
                if (!IsCarryNode(t))
                    continue;
                bool underPicked = false;
                for (int p = 0; p < picked.Count; p++)
                {
                    if (t.IsChildOf(picked[p]))
                    {
                        underPicked = true;
                        break;
                    }
                }

                if (!underPicked)
                    picked.Add(t);
            }

            if (picked.Count == 0)
                return state;

            // In a variant (e.g. Player_v7 Combat Variant) most holders are owned by the base prefab and Unity
            // refuses to reparent them. Unpack just enough outer prefab layers to make them movable; nested
            // weapon prefabs stay linked.
            int unpacked = UnpackUntilMovable(picked);
            if (unpacked > 0)
                Debug.Log($"[DMCombatNodeCarry] Unpacked {unpacked} outer prefab layer(s) so base-owned holders can move.", root);

            var holder = new GameObject(HolderName).transform;
            holder.SetParent(root.transform, false);
            state.Holder = holder;

            for (int i = 0; i < picked.Count; i++)
            {
                Transform node = picked[i];
                var rec = new NodeRecord
                {
                    Node = node,
                    LocalPos = node.localPosition,
                    LocalRot = node.localRotation,
                    LocalScale = node.localScale
                };
                Transform anchor = DescribeParent(node.parent, oldVisual, boneOf, rec.PathFromBone, out rec.HasBone, out rec.Bone);
                Transform frame = anchor != null ? anchor : oldVisual;
                rec.AnchorPos = frame.InverseTransformPoint(node.position);
                rec.AnchorRot = Quaternion.Inverse(frame.rotation) * node.rotation;
                rec.OffsetPos = root.transform.InverseTransformPoint(node.position) -
                                root.transform.InverseTransformPoint(frame.position);
                Quaternion invRoot = Quaternion.Inverse(root.transform.rotation);
                // Player-frame orientation at T-pose. Bone axes differ between rigs, so the node keeps this
                // orientation relative to the character (not to the bone's local axes) and then follows the bone.
                rec.OffsetRot = invRoot * node.rotation;
                rec.OldLimb = LimbLength(frame, oldHuman, root.transform);
                foreach (Transform d in node.GetComponentsInChildren<Transform>(true))
                    if (d != node)
                        rec.Inner.Add((d, d.parent, d.localPosition, d.localRotation, d.localScale));
                node.SetParent(holder, true);
                state.Nodes.Add(rec);
            }

            CaptureReferences(root, oldVisual, boneOf, state);
            Debug.Log($"[DMCombatNodeCarry] Lifted {state.Nodes.Count} weapon/holster/handler nodes and {state.Refs.Count} bone references out of '{oldVisual.name}'.", root);
            return state;
        }

        public static void Restore(GameObject root, CarryState state, string visualChildName)
        {
            if (root == null || state == null)
                return;

            Animator animator = root.GetComponent<Animator>();
            if (animator != null && animator.avatar != null && animator.avatar.isValid)
            {
                animator.Rebind();
                animator.Update(0f);
            }

            Transform newVisual = root.transform.Find(string.IsNullOrEmpty(visualChildName) ? "Visual" : visualChildName);
            var newHuman = new HashSet<Transform>();
            if (animator != null && animator.avatar != null && animator.avatar.isHuman)
            {
                if (newVisual != null)
                    PoseToSkeleton(newVisual, animator.avatar);
                for (int b = 0; b < (int)HumanBodyBones.LastBone; b++)
                {
                    Transform t = animator.GetBoneTransform((HumanBodyBones)b);
                    if (t != null)
                        newHuman.Add(t);
                }
            }

            int exact = 0, anchored = 0, fallback = 0;
            for (int i = 0; i < state.Nodes.Count; i++)
            {
                NodeRecord rec = state.Nodes[i];
                if (rec.Node == null)
                    continue;

                Transform anchor = rec.HasBone && animator != null ? animator.GetBoneTransform(rec.Bone) : null;
                if (anchor == null && !rec.HasBone)
                    anchor = newVisual;
                Transform parent = Follow(anchor, rec.PathFromBone);
                if (anchor != null)
                {
                    bool viaPath = parent != null && (rec.PathFromBone.Count == 0 || parent != anchor);
                    Transform seat = viaPath ? parent : anchor;
                    // Same pose relative to the anchor bone in the player frame (both rigs in T-pose); position
                    // scaled by the limb-length ratio. Never a raw local copy: bone axes differ between rigs.
                    Transform rt = root.transform;
                    float newLimb = LimbLength(anchor, newHuman, rt);
                    float ratio = rec.OldLimb > 1e-4f && newLimb > 1e-4f ? Mathf.Clamp(newLimb / rec.OldLimb, 0.5f, 2f) : 1f;
                    Vector3 rootPos = rt.InverseTransformPoint(anchor.position) + rec.OffsetPos * ratio;
                    rec.Node.SetParent(seat, true);
                    rec.Node.position = rt.TransformPoint(rootPos);
                    rec.Node.rotation = rt.rotation * rec.OffsetRot;
                    rec.Node.localScale = rec.LocalScale;
                    rec.FinalParent = seat;
                    rec.FinalLocalPos = rec.Node.localPosition;
                    rec.FinalLocalRot = rec.Node.localRotation;
                    if (viaPath) exact++; else anchored++;
                }
                else
                {
                    rec.Node.SetParent(newVisual != null ? newVisual : root.transform, true);
                    fallback++;
                }
            }

            int rewired = 0, missing = 0;
            for (int i = 0; i < state.Refs.Count; i++)
            {
                RefRecord r = state.Refs[i];
                if (r.Owner == null)
                    continue;
                Transform anchor = r.HasBone && animator != null ? animator.GetBoneTransform(r.Bone) : (r.HasBone ? null : newVisual);
                Transform target = Follow(anchor, r.PathFromBone) ?? anchor;
                Object value = null;
                if (target != null)
                {
                    if (r.TargetType == typeof(GameObject))
                        value = target.gameObject;
                    else if (r.TargetType == typeof(Transform))
                        value = target;
                    else if (r.TargetType != null)
                    {
                        value = target.GetComponent(r.TargetType);
                        // Component lived on the old bone itself (e.g. a melee attack object on the forearm):
                        // paste the stashed copy onto the matching new bone.
                        if (value == null && r.Stash != null &&
                            UnityEditorInternal.ComponentUtility.CopyComponent(r.Stash) &&
                            UnityEditorInternal.ComponentUtility.PasteComponentAsNew(target.gameObject))
                            value = target.GetComponent(r.TargetType);
                    }
                }

                var so = new SerializedObject(r.Owner);
                SerializedProperty prop = so.FindProperty(r.PropertyPath);
                if (prop == null || prop.propertyType != SerializedPropertyType.ObjectReference)
                    continue;
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
                if (value != null)
                    rewired++;
                else
                    missing++;
            }

            if (state.Stash != null)
                Object.DestroyImmediate(state.Stash.gameObject);

            if (state.Holder != null)
            {
                if (state.Holder.childCount == 0)
                    Object.DestroyImmediate(state.Holder.gameObject);
                else
                    Debug.LogWarning($"[DMCombatNodeCarry] {state.Holder.childCount} nodes could not be placed; left under {HolderName}.", root);
            }

            LastByRoot[root] = state;
            Debug.Log($"[DMCombatNodeCarry] Restored nodes (T-pose relative): {exact} exact, {anchored} by humanoid anchor, {fallback} fallback. References rewired: {rewired}, unresolved: {missing}.", root);
        }

        static readonly Dictionary<GameObject, CarryState> LastByRoot = new Dictionary<GameObject, CarryState>();

        /// <summary>
        /// Finalize (AfterRebind placement bake, RepairWeaponSlotVisuals) re-applies offsets tuned for the template
        /// rig. Call after it to put carried nodes (and their inner slots) back to the carried pose.
        /// </summary>
        public static int ReapplyCarriedPoses(GameObject root)
        {
            if (root == null || !LastByRoot.TryGetValue(root, out CarryState state))
                return 0;
            LastByRoot.Remove(root);
            int n = 0;
            foreach (NodeRecord rec in state.Nodes)
            {
                if (rec.Node == null || rec.FinalParent == null)
                    continue;
                if (rec.Node.parent == rec.FinalParent)
                {
                    rec.Node.localPosition = rec.FinalLocalPos;
                    rec.Node.localRotation = rec.FinalLocalRot;
                    rec.Node.localScale = rec.LocalScale;
                    n++;
                }

                foreach (var d in rec.Inner)
                {
                    if (d.t == null || d.t.parent != d.parent)
                        continue;
                    d.t.localPosition = d.p;
                    d.t.localRotation = d.r;
                    d.t.localScale = d.s;
                    n++;
                }
            }

            Debug.Log($"[DMCombatNodeCarry] Re-applied carried poses on {n} transform(s) after finalize.", root);
            return n;
        }

        /// <summary>Poses the bones under <paramref name="visual"/> to the avatar's T-pose skeleton (by name).</summary>
        public static int PoseToSkeleton(Transform visual, Avatar avatar)
        {
            if (visual == null || avatar == null || !avatar.isHuman)
                return 0;
            var map = new Dictionary<string, Transform>();
            foreach (Transform t in visual.GetComponentsInChildren<Transform>(true))
                if (t != visual && !map.ContainsKey(t.name))
                    map[t.name] = t;
            int n = 0;
            SkeletonBone[] sk = avatar.humanDescription.skeleton;
            for (int i = 1; i < sk.Length; i++)
            {
                if (!map.TryGetValue(sk[i].name, out Transform t))
                    continue;
                t.localPosition = sk[i].position;
                t.localRotation = sk[i].rotation;
                n++;
            }

            return n;
        }

        static float LimbLength(Transform bone, HashSet<Transform> human, Transform root)
        {
            if (bone == null)
                return 0f;
            for (Transform p = bone.parent; p != null && p != root; p = p.parent)
                if (human.Contains(p))
                    return Vector3.Distance(root.InverseTransformPoint(bone.position), root.InverseTransformPoint(p.position));
            return Mathf.Abs(root.InverseTransformPoint(bone.position).y);
        }

        static int UnpackUntilMovable(List<Transform> nodes)
        {
            int count = 0;
            for (int guard = 0; guard < 16; guard++)
            {
                GameObject blocker = null;
                for (int i = 0; i < nodes.Count; i++)
                {
                    Transform n = nodes[i];
                    if (n == null || !PrefabUtility.IsPartOfPrefabInstance(n.gameObject))
                        continue;
                    GameObject outer = PrefabUtility.GetOutermostPrefabInstanceRoot(n.gameObject);
                    if (outer != null && outer != n.gameObject)
                    {
                        blocker = outer;
                        break;
                    }
                }

                if (blocker == null)
                    break;
                PrefabUtility.UnpackPrefabInstance(blocker, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                count++;
            }

            return count;
        }

        static Component StashComponent(GameObject root, Component src, CarryState state)
        {
            if (state.Stashed.TryGetValue(src, out Component existing))
                return existing;
            if (state.Stash == null)
            {
                state.Stash = new GameObject(HolderName + "_Components").transform;
                state.Stash.SetParent(root.transform, false);
                state.Stash.gameObject.SetActive(false);
            }

            Component copy = null;
            var go = new GameObject(src.GetType().Name);
            go.transform.SetParent(state.Stash, false);
            if (UnityEditorInternal.ComponentUtility.CopyComponent(src) &&
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(go))
                copy = go.GetComponent(src.GetType());
            state.Stashed[src] = copy;
            return copy;
        }

        static bool IsCarryNode(Transform t)
        {
            string n = t.name;
            for (int i = 0; i < SocketNames.Length; i++)
                if (n.Equals(SocketNames[i], System.StringComparison.OrdinalIgnoreCase))
                    return true;
            return DMHumanoidVisualAttachUtility.IsWeaponVisualNode(t);
        }

        static Transform DescribeParent(
            Transform parent,
            Transform oldVisual,
            Dictionary<Transform, HumanBodyBones> boneOf,
            List<string> path,
            out bool hasBone,
            out HumanBodyBones bone)
        {
            hasBone = false;
            bone = HumanBodyBones.Hips;
            path.Clear();
            Transform cur = parent;
            while (cur != null && cur != oldVisual)
            {
                if (boneOf.TryGetValue(cur, out bone))
                {
                    hasBone = true;
                    path.Reverse();
                    return cur;
                }

                path.Add(cur.name);
                cur = cur.parent;
            }

            path.Reverse();
            return oldVisual;
        }

        static Transform Follow(Transform anchor, List<string> path)
        {
            if (anchor == null)
                return null;
            Transform cur = anchor;
            for (int i = 0; i < path.Count; i++)
            {
                Transform next = DMHumanoidBoneRenameUtility.FindNamedUnder(cur, path[i]);
                if (next == null)
                    return null;
                cur = next;
            }

            return cur;
        }

        static void CaptureReferences(
            GameObject root,
            Transform oldVisual,
            Dictionary<Transform, HumanBodyBones> boneOf,
            CarryState state)
        {
            Component[] comps = root.GetComponentsInChildren<Component>(true);
            for (int c = 0; c < comps.Length; c++)
            {
                Component comp = comps[c];
                if (comp == null || comp is Transform || comp.transform.IsChildOf(oldVisual))
                    continue;

                var so = new SerializedObject(comp);
                SerializedProperty it = so.GetIterator();
                while (it.Next(true))
                {
                    if (it.propertyType != SerializedPropertyType.ObjectReference)
                        continue;
                    Object v = it.objectReferenceValue;
                    Transform t = v is GameObject g ? g.transform : (v as Component)?.transform;
                    if (t == null || !t.IsChildOf(oldVisual))
                        continue;

                    var r = new RefRecord
                    {
                        Owner = comp,
                        PropertyPath = it.propertyPath,
                        TargetType = v.GetType()
                    };
                    if (v is Component srcComp && !(v is Transform))
                        r.Stash = StashComponent(root, srcComp, state);

                    if (boneOf.TryGetValue(t, out HumanBodyBones b))
                    {
                        r.HasBone = true;
                        r.Bone = b;
                    }
                    else if (t != oldVisual)
                    {
                        DescribeParent(t.parent, oldVisual, boneOf, r.PathFromBone, out r.HasBone, out r.Bone);
                        r.PathFromBone.Add(t.name);
                    }

                    state.Refs.Add(r);
                }
            }
        }
    }
}
#endif
