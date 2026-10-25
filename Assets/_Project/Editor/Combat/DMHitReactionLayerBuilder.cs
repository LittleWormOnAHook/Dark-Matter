#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using Project.AI.Invector;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Project.EditorTools.Combat
{
    /// <summary>
    /// Slice 2b.2 / 2b.3 / 2b.4: builds (idempotent) and validates the directional hit-reaction / knockdown assets:
    ///   - 20 PROTOFACTOR clips copied to Assets/_Project/Animations/HitReactions as .anim (vendor FBX untouched; PROTOFACTOR is
    ///     gitignored so the copies keep fresh clones working). Loop off (lying hold loops), root XZ / rotation not baked
    ///     (enemies run applyRootMotion off, so the body stays on the root), root Y baked (falls reach the floor).
    ///   - Avatar masks DM_HitReact_Torso (hips/spine/chest/neck/head) and DM_HitReact_Body (+ legs/feet); arms, hands, fingers,
    ///     hand IK always off so every enemy keeps its own weapon grip.
    ///   - Layers appended to the shared Invector@ShooterMelee.controller (after FullBody): DM_HitReact_Add (additive, torso),
    ///     DM_HitReact_Body (override, body), DM_KnockDown (override, no mask). Weight 0 by default, driven from script.
    /// Tools > Dark Matter > Combat > Build Hit Reaction Layers / Validate Hit Reaction Layers.
    /// </summary>
    public static class DMHitReactionLayerBuilder
    {
        public const string ControllerPath = "Assets/Invector-3rdPersonController/Shooter/Animator/Invector@ShooterMelee.controller";
        // 2026-10-19 d: player controller target (DMPlayerHitReaction / DMPlayerKnockdown); built separately so the enemy controller is not churned.
        public const string PlayerControllerPath = "Assets/_Project/Animations/Player/Invector@ShooterMelee_Jetpack.controller";
        public const string ClipFolder = "Assets/_Project/Animations/HitReactions";
        public const string MaskFolder = "Assets/_Project/Animations/Masks";
        public const string TorsoMaskPath = MaskFolder + "/DM_HitReact_Torso.mask";
        public const string BodyMaskPath = MaskFolder + "/DM_HitReact_Body.mask";
        private const string SourceFolder = "Assets/PROTOFACTOR/Ultimate Animation Collection/Animations/1Handed Melee Weapon Animset/FBX Motions/";

        private static readonly string[] Tags = { "F", "B", "L", "R" };
        private static readonly string[] Dirs = { "Front", "Back", "Left", "Right" };

        /// <summary>state name -> source FBX clip name (Humanoid@{name}.fbx)</summary>
        private static List<KeyValuePair<string, string>> ClipMap()
        {
            var map = new List<KeyValuePair<string, string>>();
            for (int i = 0; i < 4; i++)
            {
                map.Add(new KeyValuePair<string, string>("HR_Light_" + Tags[i], "GetHitLight" + Dirs[i] + "1hMelee"));
                map.Add(new KeyValuePair<string, string>("HR_Heavy_" + Tags[i], "GetHitHeavy" + Dirs[i] + "1hMelee"));
                map.Add(new KeyValuePair<string, string>("KD_Fall_" + Tags[i], "Death" + Dirs[i] + "1hMelee"));
                map.Add(new KeyValuePair<string, string>("KD_Down_" + Tags[i], "Dead" + Dirs[i]));
                map.Add(new KeyValuePair<string, string>("KD_GetUp_" + Tags[i], "GetBackUp" + Dirs[i]));
            }
            return map;
        }

        [MenuItem("Tools/Dark Matter Genesis/Combat/Build Hit Reaction Layers")]
        public static void BuildMenu()
        {
            Debug.Log(Build());
        }

        [MenuItem("Tools/Dark Matter Genesis/Combat/Validate Hit Reaction Layers")]
        public static void ValidateMenu()
        {
            string report = Validate(out bool ok);
            if (ok)
                Debug.Log(report);
            else
                Debug.LogError(report);
        }

        [MenuItem("Tools/Dark Matter Genesis/Combat/Build Player Hit Reaction Layers")]
        public static void BuildPlayerMenu()
        {
            Debug.Log(Build(PlayerControllerPath));
        }

        [MenuItem("Tools/Dark Matter Genesis/Combat/Validate Player Hit Reaction Layers")]
        public static void ValidatePlayerMenu()
        {
            string report = Validate(out bool ok, PlayerControllerPath);
            if (ok)
                Debug.Log(report);
            else
                Debug.LogError(report);
        }

        public static string Build(string controllerPath = ControllerPath)
        {
            var sb = new StringBuilder("[HitReactionBuilder] ").Append(System.IO.Path.GetFileName(controllerPath)).Append(' ');
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return sb.Append("refused: exit Play mode first").ToString();

            EnsureFolder(ClipFolder);
            EnsureFolder(MaskFolder);

            var clips = new Dictionary<string, AnimationClip>();
            int copied = 0;
            foreach (KeyValuePair<string, string> kv in ClipMap())
            {
                string dst = ClipFolder + "/DM_" + kv.Key + ".anim";
                AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(dst);
                if (existing != null)
                {
                    clips[kv.Key] = existing;
                    continue;
                }

                AnimationClip src = LoadFbxClip(SourceFolder + "Humanoid@" + kv.Value + ".fbx");
                if (src == null)
                {
                    sb.Append("MISSING source ").Append(kv.Value).Append("; ");
                    continue;
                }

                var copy = new AnimationClip();
                EditorUtility.CopySerialized(src, copy);
                copy.name = "DM_" + kv.Key;
                AnimationClipSettings s = AnimationUtility.GetAnimationClipSettings(copy);
                bool loop = kv.Key.StartsWith("KD_Down_");
                s.loopTime = loop;
                s.loopBlend = false;
                s.loopBlendOrientation = false;  // rotation not baked: body keeps facing the root
                s.keepOriginalOrientation = true;
                s.loopBlendPositionXZ = false;   // XZ not baked: travel is extracted and discarded (applyRootMotion off)
                s.keepOriginalPositionXZ = false;
                s.loopBlendPositionY = true;     // Y baked into the pose: falls reach the floor
                s.keepOriginalPositionY = true;
                s.mirror = false;
                AnimationUtility.SetAnimationClipSettings(copy, s);
                AnimationUtility.SetAnimationEvents(copy, new AnimationEvent[0]);
                AssetDatabase.CreateAsset(copy, dst);
                clips[kv.Key] = copy;
                copied++;
            }
            sb.Append("clips copied=").Append(copied).Append(" total=").Append(clips.Count).Append("; ");

            AvatarMask torso = EnsureMask(TorsoMaskPath, legs: false);
            AvatarMask body = EnsureMask(BodyMaskPath, legs: true);

            AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (ctrl == null)
                return sb.Append("controller missing").ToString();

            EnsureFloatParam(ctrl, DMEnemyHitReactionPlayer.AddSpeedParam);
            EnsureFloatParam(ctrl, DMEnemyHitReactionPlayer.BodySpeedParam);
            EnsureFloatParam(ctrl, DMEnemyKnockdown.SpeedParam);

            RemoveLayer(ctrl, DMEnemyHitReactionPlayer.AddLayerName);
            RemoveLayer(ctrl, DMEnemyHitReactionPlayer.BodyLayerName);
            RemoveLayer(ctrl, DMEnemyKnockdown.LayerName);

            var hr = new List<string>();
            for (int i = 0; i < 4; i++)
            {
                hr.Add("HR_Light_" + Tags[i]);
                hr.Add("HR_Heavy_" + Tags[i]);
            }
            AddLayer(ctrl, DMEnemyHitReactionPlayer.AddLayerName, AnimatorLayerBlendingMode.Additive, torso, hr, clips, DMEnemyHitReactionPlayer.AddSpeedParam);
            AddLayer(ctrl, DMEnemyHitReactionPlayer.BodyLayerName, AnimatorLayerBlendingMode.Override, body, hr, clips, DMEnemyHitReactionPlayer.BodySpeedParam);

            var kd = new List<string>();
            for (int i = 0; i < 4; i++)
            {
                kd.Add("KD_Fall_" + Tags[i]);
                kd.Add("KD_Down_" + Tags[i]);
                kd.Add("KD_GetUp_" + Tags[i]);
            }
            AddLayer(ctrl, DMEnemyKnockdown.LayerName, AnimatorLayerBlendingMode.Override, null, kd, clips, DMEnemyKnockdown.SpeedParam);

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssetIfDirty(ctrl);
            AssetDatabase.SaveAssetIfDirty(torso);
            AssetDatabase.SaveAssetIfDirty(body);
            sb.Append("layers=").Append(ctrl.layers.Length).Append(" (DM layers rebuilt)");
            return sb.ToString();
        }

        public static string Validate(out bool ok, string controllerPath = ControllerPath)
        {
            ok = true;
            var sb = new StringBuilder("[HitReactionValidate] ").Append(System.IO.Path.GetFileName(controllerPath)).Append(' ');
            foreach (KeyValuePair<string, string> kv in ClipMap())
            {
                AnimationClip c = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipFolder + "/DM_" + kv.Key + ".anim");
                if (c == null) { ok = false; sb.Append("missing clip ").Append(kv.Key).Append("; "); continue; }
                if (!c.humanMotion) { ok = false; sb.Append("not humanoid ").Append(kv.Key).Append("; "); }
                AnimationClipSettings s = AnimationUtility.GetAnimationClipSettings(c);
                bool wantLoop = kv.Key.StartsWith("KD_Down_");
                if (s.loopTime != wantLoop) { ok = false; sb.Append("loop wrong ").Append(kv.Key).Append("; "); }
            }

            CheckMask(AssetDatabase.LoadAssetAtPath<AvatarMask>(TorsoMaskPath), false, "Torso", sb, ref ok);
            CheckMask(AssetDatabase.LoadAssetAtPath<AvatarMask>(BodyMaskPath), true, "Body", sb, ref ok);

            AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (ctrl == null) { ok = false; return sb.Append("controller missing").ToString(); }

            AnimatorControllerLayer[] layers = ctrl.layers;
            int add = IndexOf(layers, DMEnemyHitReactionPlayer.AddLayerName);
            int bod = IndexOf(layers, DMEnemyHitReactionPlayer.BodyLayerName);
            int kdl = IndexOf(layers, DMEnemyKnockdown.LayerName);
            int full = IndexOf(layers, "FullBody");
            if (add < 0 || bod < 0 || kdl < 0) { ok = false; sb.Append("DM layers missing; "); }
            else
            {
                if (!(add > full && bod > add && kdl > bod)) { ok = false; sb.Append("layer order wrong; "); }
                if (layers[add].blendingMode != AnimatorLayerBlendingMode.Additive) { ok = false; sb.Append("Add layer not additive; "); }
                if (layers[add].avatarMask == null || layers[bod].avatarMask == null) { ok = false; sb.Append("mask unassigned; "); }
                if (layers[kdl].avatarMask != null) { ok = false; sb.Append("knockdown layer must have no mask; "); }
                sb.Append($"states add={layers[add].stateMachine.states.Length} body={layers[bod].stateMachine.states.Length} kd={layers[kdl].stateMachine.states.Length}; ");
                foreach (int li in new[] { add, bod, kdl })
                {
                    foreach (ChildAnimatorState cs in layers[li].stateMachine.states)
                    {
                        if (cs.state.name != DMEnemyHitReactionPlayer.EmptyState && cs.state.motion == null)
                        {
                            ok = false;
                            sb.Append("no motion ").Append(layers[li].name).Append('/').Append(cs.state.name).Append("; ");
                        }
                    }
                }
            }

            foreach (string prm in new[] { DMEnemyHitReactionPlayer.AddSpeedParam, DMEnemyHitReactionPlayer.BodySpeedParam, DMEnemyKnockdown.SpeedParam })
            {
                bool found = false;
                foreach (AnimatorControllerParameter cp in ctrl.parameters)
                    found |= cp.name == prm && cp.type == AnimatorControllerParameterType.Float;
                if (!found) { ok = false; sb.Append("param missing ").Append(prm).Append("; "); }
            }

            sb.Append($"layers={layers.Length}; ").Append(ok ? "OK" : "FAILED");
            return sb.ToString();
        }

        private static void CheckMask(AvatarMask m, bool legs, string label, StringBuilder sb, ref bool ok)
        {
            if (m == null) { ok = false; sb.Append("mask missing ").Append(label).Append("; "); return; }
            AvatarMaskBodyPart[] off = { AvatarMaskBodyPart.LeftArm, AvatarMaskBodyPart.RightArm, AvatarMaskBodyPart.LeftFingers, AvatarMaskBodyPart.RightFingers, AvatarMaskBodyPart.LeftHandIK, AvatarMaskBodyPart.RightHandIK, AvatarMaskBodyPart.Root };
            foreach (AvatarMaskBodyPart part in off)
            {
                if (m.GetHumanoidBodyPartActive(part)) { ok = false; sb.Append(label).Append(" mask has ").Append(part).Append(" on; "); }
            }
            if (!m.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Body) || !m.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Head)) { ok = false; sb.Append(label).Append(" mask missing body/head; "); }
            if (m.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg) != legs) { ok = false; sb.Append(label).Append(" mask legs wrong; "); }
        }

        private static int IndexOf(AnimatorControllerLayer[] layers, string name)
        {
            for (int i = 0; i < layers.Length; i++)
                if (layers[i].name == name)
                    return i;
            return -1;
        }

        private static AnimationClip LoadFbxClip(string path)
        {
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (o is AnimationClip c && !c.name.StartsWith("__preview"))
                    return c;
            }
            return null;
        }

        private static AvatarMask EnsureMask(string path, bool legs)
        {
            AvatarMask m = AssetDatabase.LoadAssetAtPath<AvatarMask>(path);
            bool create = m == null;
            if (create)
                m = new AvatarMask();

            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                m.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
            m.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            m.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
            if (legs)
            {
                m.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg, true);
                m.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg, true);
                m.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFootIK, true);
                m.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFootIK, true);
            }

            if (create)
                AssetDatabase.CreateAsset(m, path);
            else
                EditorUtility.SetDirty(m);
            return m;
        }

        private static void EnsureFloatParam(AnimatorController ctrl, string name)
        {
            foreach (AnimatorControllerParameter p in ctrl.parameters)
                if (p.name == name)
                    return;
            ctrl.AddParameter(new AnimatorControllerParameter { name = name, type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
        }

        private static void RemoveLayer(AnimatorController ctrl, string name)
        {
            AnimatorControllerLayer[] layers = ctrl.layers;
            for (int i = layers.Length - 1; i >= 0; i--)
            {
                if (layers[i].name == name)
                    ctrl.RemoveLayer(i); // also removes its state machine sub-assets
            }
        }

        private static void AddLayer(AnimatorController ctrl, string name, AnimatorLayerBlendingMode mode, AvatarMask mask,
            List<string> states, Dictionary<string, AnimationClip> clips, string speedParam)
        {
            var sm = new AnimatorStateMachine { name = name, hideFlags = HideFlags.HideInHierarchy };
            AssetDatabase.AddObjectToAsset(sm, ctrl);
            AnimatorState empty = sm.AddState(DMEnemyHitReactionPlayer.EmptyState, new Vector3(250f, 0f, 0f));
            empty.writeDefaultValues = false;
            sm.defaultState = empty;

            for (int i = 0; i < states.Count; i++)
            {
                AnimatorState st = sm.AddState(states[i], new Vector3(250f + 260f * (i % 3 + 1), 70f * (i / 3 + 1), 0f));
                st.motion = clips.TryGetValue(states[i], out AnimationClip c) ? c : null;
                st.writeDefaultValues = false;
                st.speedParameterActive = true;
                st.speedParameter = speedParam;
            }

            ctrl.AddLayer(new AnimatorControllerLayer
            {
                name = name,
                stateMachine = sm,
                avatarMask = mask,
                blendingMode = mode,
                defaultWeight = 0f,
                iKPass = false
            });
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
#endif
