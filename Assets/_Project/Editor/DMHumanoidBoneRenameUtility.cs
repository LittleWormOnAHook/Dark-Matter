#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Renames an instantiated Visual armature to the template Humanoid/VBOT bone names
    /// (instance only — never the source FBX on disk) and rebuilds a Humanoid Avatar.
    /// </summary>
    public static class DMHumanoidBoneRenameUtility
    {
        public const string StockBonePrefix = "_Stock_";
        public const string TempRenamePrefix = "__DMRename_";

        public struct RenameEntry
        {
            public HumanBodyBones Bone;
            public string From;
            public string To;
        }

        public static string ResolveAvatarAssetPath(string prefabPath)
        {
            if (string.IsNullOrEmpty(prefabPath))
                return null;

            string dir = Path.GetDirectoryName(prefabPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(dir))
                return null;

            string fileName = Path.GetFileNameWithoutExtension(prefabPath);
            return dir + "/" + fileName + "_HumanoidAvatar.asset";
        }

        public static Dictionary<HumanBodyBones, string> SnapshotTemplateBoneNames(
            GameObject outputRoot,
            DMHumanoidVisualTarget target,
            GameObject templateOverride)
        {
            Dictionary<HumanBodyBones, string> fromStock = SnapshotNamesUnderStock(outputRoot);
            if (LooksLikeTemplateNames(fromStock))
                return fromStock;

            Avatar templateAvatar = ResolveTemplateAvatar(target, templateOverride, outputRoot);
            Dictionary<HumanBodyBones, string> fromAvatar = SnapshotNamesFromAvatar(templateAvatar);
            if (LooksLikeTemplateNames(fromAvatar))
                return fromAvatar;

            if (fromStock.Count > 0)
                return fromStock;
            return fromAvatar;
        }

        public static Dictionary<HumanBodyBones, Transform> SnapshotHumanoidBones(Animator animator)
        {
            var map = new Dictionary<HumanBodyBones, Transform>();
            if (animator == null)
                return map;

            if (animator.avatar != null && animator.avatar.isValid)
            {
                animator.Rebind();
                animator.Update(0f);
            }

            int last = (int)HumanBodyBones.LastBone;
            for (int i = 0; i < last; i++)
            {
                var bone = (HumanBodyBones)i;
                Transform t = animator.GetBoneTransform(bone);
                if (t != null)
                    map[bone] = t;
            }

            return map;
        }

        public static Dictionary<HumanBodyBones, Transform> SnapshotHumanoidBonesUnder(
            GameObject visual,
            Avatar sourceAvatar)
        {
            var map = new Dictionary<HumanBodyBones, Transform>();
            if (visual == null)
                return map;

            Animator animator = visual.GetComponent<Animator>();
            if (animator == null)
                animator = visual.GetComponentInChildren<Animator>(true);
            if (animator == null)
                animator = visual.AddComponent<Animator>();

            if (sourceAvatar != null)
                animator.avatar = sourceAvatar;

            Dictionary<HumanBodyBones, Transform> raw = SnapshotHumanoidBones(animator);
            foreach (KeyValuePair<HumanBodyBones, Transform> pair in raw)
            {
                if (pair.Value == null)
                    continue;
                if (pair.Value == visual.transform || pair.Value.IsChildOf(visual.transform))
                    map[pair.Key] = pair.Value;
            }

            return map;
        }

        public static int PrefixStockTemplateBones(GameObject root, Dictionary<HumanBodyBones, string> templateNames)
        {
            if (root == null || templateNames == null || templateNames.Count == 0)
                return 0;

            var targets = new HashSet<string>(StringComparer.Ordinal);
            foreach (string name in templateNames.Values)
            {
                if (!string.IsNullOrEmpty(name))
                    targets.Add(name);
            }

            int renamed = 0;
            PrefixStockUnder(root.transform.Find("3D Model"), targets, ref renamed);
            PrefixStockUnder(root.transform.Find("Humanoid"), targets, ref renamed);
            return renamed;
        }

        public static int RenameVisualBonesToTemplate(
            Dictionary<HumanBodyBones, Transform> sourceBones,
            Dictionary<HumanBodyBones, string> templateNames,
            List<RenameEntry> renamed)
        {
            if (sourceBones == null || templateNames == null)
                return 0;

            var planned = new List<(Transform Transform, HumanBodyBones Bone, string From, string To)>(sourceBones.Count);
            var claimed = new HashSet<string>(StringComparer.Ordinal);

            foreach (KeyValuePair<HumanBodyBones, Transform> pair in sourceBones)
            {
                if (pair.Value == null)
                    continue;
                if (!templateNames.TryGetValue(pair.Key, out string target) || string.IsNullOrEmpty(target))
                    continue;
                if (pair.Value.name.Equals(target, StringComparison.Ordinal))
                    continue;

                string unique = target;
                if (claimed.Contains(unique))
                    unique = target + "_" + pair.Key;
                claimed.Add(unique);
                planned.Add((pair.Value, pair.Key, pair.Value.name, unique));
            }

            planned.Sort((a, b) => Depth(b.Transform).CompareTo(Depth(a.Transform)));

            for (int i = 0; i < planned.Count; i++)
                planned[i].Transform.name = TempRenamePrefix + (int)planned[i].Bone;

            int count = 0;
            for (int i = 0; i < planned.Count; i++)
            {
                planned[i].Transform.name = planned[i].To;
                renamed?.Add(new RenameEntry
                {
                    Bone = planned[i].Bone,
                    From = planned[i].From,
                    To = planned[i].To
                });
                count++;
            }

            return count;
        }

        public static Avatar RebuildHumanoidAvatar(
            GameObject visual,
            Avatar sourceAvatar,
            Dictionary<HumanBodyBones, string> templateNames)
        {
            if (visual == null)
                return null;

            HumanDescription desc;
            if (sourceAvatar != null && sourceAvatar.isHuman)
                desc = sourceAvatar.humanDescription;
            else
                return null;

            HumanBone[] humans = desc.human;
            // old (source FBX) bone name -> new (template) bone name, so the source avatar's authored
            // T-pose skeleton entries can be re-keyed after the Visual bones were renamed.
            var renameMap = new Dictionary<string, string>(StringComparer.Ordinal);
            if (humans != null && templateNames != null)
            {
                for (int i = 0; i < humans.Length; i++)
                {
                    HumanBone bone = humans[i];
                    if (!TryMapHumanName(bone.humanName, out HumanBodyBones bodyBone))
                        continue;
                    if (!templateNames.TryGetValue(bodyBone, out string newName) || string.IsNullOrEmpty(newName))
                        continue;
                    if (!string.IsNullOrEmpty(bone.boneName))
                        renameMap[bone.boneName] = newName;
                    bone.boneName = newName;
                    humans[i] = bone;
                }

                desc.human = humans;
            }

            // ROOT CAUSE OF "CROSSED ARMS": the FBX hierarchy is stored in its authored (often A-) pose while the
            // imported Avatar stores a corrected T-pose in humanDescription.skeleton. Rebuilding the avatar from the
            // live transforms baked the A-pose in as the T-pose reference, so every humanoid clip pulled the arms in.
            // Keep the live hierarchy (names/parents) but take pose data from the source avatar's T-pose skeleton.
            desc.skeleton = BuildSkeleton(visual.transform, sourceAvatar.humanDescription.skeleton, renameMap);

            Avatar built = AvatarBuilder.BuildHumanAvatar(visual, desc);
            if (built == null || !built.isValid)
            {
                if (built != null)
                    UnityEngine.Object.DestroyImmediate(built);
                Debug.LogWarning(
                    "[DMHumanoidBoneRename] AvatarBuilder failed after bone rename. " +
                    "Visual keeps renamed bones; Animator may need a manual Humanoid remap.",
                    visual);
                return null;
            }

            built.name = visual.name + "_HumanoidAvatar";
            return built;
        }

        public static Avatar SaveAvatarAsset(Avatar avatar, string assetPath)
        {
            if (avatar == null || string.IsNullOrEmpty(assetPath))
                return avatar;

            string dir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(dir))
                CraftingEditorUtility.EnsureFolder(dir);

            Avatar existing = AssetDatabase.LoadAssetAtPath<Avatar>(assetPath);
            if (existing != null)
            {
                // Replace the avatar file's contents but keep its .meta, so every prefab / scene reference
                // (GUID + fileID 9000000) keeps resolving.
                string tempPath = (string.IsNullOrEmpty(dir) ? "Assets" : dir) + "/__dm_tmp_avatar.asset";
                AssetDatabase.CreateAsset(avatar, tempPath);
                AssetDatabase.SaveAssets();
                File.Copy(tempPath, assetPath, true);
                AssetDatabase.DeleteAsset(tempPath);
                // The copied main object is still named after the temp file; rename it so the importer does not warn.
                string yaml = File.ReadAllText(assetPath);
                string fixedYaml = yaml.Replace("m_Name: __dm_tmp_avatar", "m_Name: " + Path.GetFileNameWithoutExtension(assetPath));
                if (!ReferenceEquals(yaml, fixedYaml) && yaml != fixedYaml)
                    File.WriteAllText(assetPath, fixedYaml);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                Avatar kept = AssetDatabase.LoadAssetAtPath<Avatar>(assetPath);
                return kept != null ? kept : existing;
            }

            AssetDatabase.CreateAsset(avatar, assetPath);
            AssetDatabase.ImportAsset(assetPath);
            Avatar saved = AssetDatabase.LoadAssetAtPath<Avatar>(assetPath);
            return saved != null ? saved : avatar;
        }

        public static Transform FindNamedUnder(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name))
                return null;

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name.Equals(name, StringComparison.Ordinal))
                    return all[i];
            }

            return null;
        }

        public static void RemountStockHitVolumesToVisual(
            GameObject root,
            string visualChildName,
            Dictionary<HumanBodyBones, string> templateNames)
        {
            if (root == null || templateNames == null)
                return;

            Transform stock = root.transform.Find("3D Model");
            string childName = string.IsNullOrWhiteSpace(visualChildName) ? "Visual" : visualChildName;
            Transform visual = root.transform.Find(childName);
            if (stock == null || visual == null)
                return;

            foreach (KeyValuePair<HumanBodyBones, string> pair in templateNames)
            {
                if (string.IsNullOrEmpty(pair.Value))
                    continue;

                Transform visualBone = FindNamedUnder(visual, pair.Value);
                if (visualBone == null)
                    continue;

                Transform stockBone = FindNamedUnder(stock, StockBonePrefix + pair.Value) ??
                                      FindNamedUnder(stock, pair.Value);
                if (stockBone == null)
                    continue;

                Transform[] children = new Transform[stockBone.childCount];
                for (int i = 0; i < stockBone.childCount; i++)
                    children[i] = stockBone.GetChild(i);

                for (int i = 0; i < children.Length; i++)
                {
                    Transform child = children[i];
                    if (child == null || !IsRemountableHitVolume(child))
                        continue;
                    if (child.IsChildOf(visualBone) || child.parent == visualBone)
                        continue;
                    child.SetParent(visualBone, true);
                }
            }
        }

        public static void LogRenameSummary(GameObject root, List<RenameEntry> renamed)
        {
            if (root == null || renamed == null || renamed.Count == 0)
                return;

            string hips = "none";
            for (int i = 0; i < renamed.Count; i++)
            {
                if (renamed[i].Bone == HumanBodyBones.Hips)
                {
                    hips = renamed[i].From + " → " + renamed[i].To;
                    break;
                }
            }

            Debug.Log(
                $"[DMHumanoidBoneRename] Renamed {renamed.Count} Visual bones to template names " +
                $"(e.g. {hips}).",
                root);
        }

        static Dictionary<HumanBodyBones, string> SnapshotNamesUnderStock(GameObject outputRoot)
        {
            var map = new Dictionary<HumanBodyBones, string>();
            if (outputRoot == null)
                return map;

            Animator animator = outputRoot.GetComponent<Animator>();
            Avatar avatar = animator != null ? animator.avatar : null;
            Dictionary<HumanBodyBones, string> fromAvatar = SnapshotNamesFromAvatar(avatar);

            Transform stock = outputRoot.transform.Find("3D Model");
            if (stock == null)
                return fromAvatar;

            if (fromAvatar.Count > 0)
            {
                foreach (KeyValuePair<HumanBodyBones, string> pair in fromAvatar)
                {
                    if (string.IsNullOrEmpty(pair.Value))
                        continue;
                    Transform live = FindNamedUnder(stock, pair.Value) ??
                                     FindNamedUnder(stock, StockBonePrefix + pair.Value);
                    if (live == null)
                        continue;
                    string name = live.name;
                    if (name.StartsWith(StockBonePrefix, StringComparison.Ordinal))
                        name = name.Substring(StockBonePrefix.Length);
                    map[pair.Key] = name;
                }

                if (LooksLikeTemplateNames(map))
                    return map;
            }

            return map.Count > 0 ? map : fromAvatar;
        }

        public static Dictionary<HumanBodyBones, string> SnapshotNamesFromAvatar(Avatar avatar)
        {
            var map = new Dictionary<HumanBodyBones, string>();
            if (avatar == null || !avatar.isHuman)
                return map;

            HumanBone[] humans = avatar.humanDescription.human;
            if (humans == null)
                return map;

            for (int i = 0; i < humans.Length; i++)
            {
                if (!TryMapHumanName(humans[i].humanName, out HumanBodyBones bone))
                    continue;
                if (string.IsNullOrEmpty(humans[i].boneName))
                    continue;
                map[bone] = humans[i].boneName;
            }

            return map;
        }

        static Avatar ResolveTemplateAvatar(
            DMHumanoidVisualTarget target,
            GameObject templateOverride,
            GameObject outputRoot)
        {
            if (templateOverride != null)
            {
                Avatar fromOverride = AvatarFromGameObject(templateOverride);
                if (fromOverride != null)
                    return fromOverride;
            }

            if (outputRoot != null)
            {
                Animator rootAnimator = outputRoot.GetComponent<Animator>();
                if (rootAnimator != null &&
                    rootAnimator.avatar != null &&
                    LooksLikeTemplateNames(SnapshotNamesFromAvatar(rootAnimator.avatar)))
                    return rootAnimator.avatar;
            }

            string path = target == DMHumanoidVisualTarget.Player
                ? ProjectAssetPaths.PlayerInvectorPrefab
                : ProjectAssetPaths.HumanoidEnemyPrefab;
            GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return AvatarFromGameObject(template);
        }

        static Avatar AvatarFromGameObject(GameObject go)
        {
            if (go == null)
                return null;
            Animator animator = go.GetComponent<Animator>();
            if (animator == null)
                animator = go.GetComponentInChildren<Animator>(true);
            return animator != null ? animator.avatar : null;
        }

        static bool LooksLikeTemplateNames(Dictionary<HumanBodyBones, string> names)
        {
            if (names == null || names.Count == 0)
                return false;

            foreach (string name in names.Values)
            {
                if (string.IsNullOrEmpty(name))
                    continue;
                if (name.StartsWith("ORG-", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("mixamorig", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("DEF-", StringComparison.OrdinalIgnoreCase))
                    return false;
                if (name.StartsWith("VBOT_", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("VBOT_:", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return true;
        }

        static void PrefixStockUnder(Transform searchRoot, HashSet<string> targets, ref int renamed)
        {
            if (searchRoot == null || targets == null || targets.Count == 0)
                return;

            Transform[] nodes = searchRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < nodes.Length; i++)
            {
                Transform node = nodes[i];
                if (node == null || node == searchRoot)
                    continue;
                if (DMHumanoidVisualAttachUtility.IsWeaponVisualNode(node))
                    continue;

                string name = node.name;
                if (name.StartsWith(StockBonePrefix, StringComparison.Ordinal))
                    continue;
                if (!targets.Contains(name))
                    continue;

                node.name = StockBonePrefix + name;
                renamed++;
            }
        }

        static SkeletonBone[] BuildSkeleton(
            Transform visual,
            SkeletonBone[] sourceTPose,
            Dictionary<string, string> renameMap)
        {
            Dictionary<string, SkeletonBone> tpose = null;
            if (sourceTPose != null && sourceTPose.Length > 0)
            {
                tpose = new Dictionary<string, SkeletonBone>(sourceTPose.Length, StringComparer.Ordinal);
                for (int i = 0; i < sourceTPose.Length; i++)
                {
                    string key = sourceTPose[i].name;
                    if (renameMap != null && renameMap.TryGetValue(key, out string renamed))
                        key = renamed;
                    if (!tpose.ContainsKey(key))
                        tpose[key] = sourceTPose[i];
                }
            }

            Transform[] transforms = visual.GetComponentsInChildren<Transform>(true);
            var bones = new SkeletonBone[transforms.Length];
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform t = transforms[i];
                var bone = new SkeletonBone
                {
                    name = t.name,
                    position = t.localPosition,
                    rotation = t.localRotation,
                    scale = t.localScale
                };

                // Index 0 is the Visual root (renamed/placed by the creator): keep the live pose.
                if (i > 0 && tpose != null && tpose.TryGetValue(t.name, out SkeletonBone src))
                {
                    bone.position = src.position;
                    bone.rotation = src.rotation;
                    bone.scale = src.scale;
                }

                bones[i] = bone;
            }

            return bones;
        }

        static bool TryMapHumanName(string humanName, out HumanBodyBones bone)
        {
            bone = HumanBodyBones.LastBone;
            if (string.IsNullOrEmpty(humanName))
                return false;

            int limit = Mathf.Min(HumanTrait.BoneCount, (int)HumanBodyBones.LastBone);
            for (int i = 0; i < limit; i++)
            {
                if (HumanTrait.BoneName[i].Equals(humanName, StringComparison.OrdinalIgnoreCase))
                {
                    bone = (HumanBodyBones)i;
                    return true;
                }
            }

            return false;
        }

        static bool IsRemountableHitVolume(Transform node)
        {
            if (node == null || node.GetComponent<SkinnedMeshRenderer>() != null)
                return false;
            if (DMHumanoidVisualAttachUtility.IsWeaponVisualNode(node))
                return false;

            string name = node.name;
            if (name.Equals("hitBox", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("leftFoot_trigger", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("rightFoot_trigger", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith("_trigger", StringComparison.OrdinalIgnoreCase))
                return true;

            return node.GetComponent<CapsuleCollider>() != null ||
                   node.GetComponent<SphereCollider>() != null ||
                   node.GetComponent<BoxCollider>() != null;
        }

        static int Depth(Transform t)
        {
            int depth = 0;
            while (t != null)
            {
                depth++;
                t = t.parent;
            }

            return depth;
        }
    }
}
#endif
