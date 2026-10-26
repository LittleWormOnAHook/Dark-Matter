using Invector.vMelee;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Project.EditorTools.Combat
{
    /// <summary>2026-10-25f: adds FullBody/Attacks JumpAttack1H + JumpAttack2H states (vMeleeAttackControl damageType "JumpAttack") to the player controller. Idempotent.</summary>
    public static class DMJumpAttackAnimatorSetup
    {
        public const string ControllerPath = "Assets/_Project/Animations/Player/Invector@ShooterMelee_Jetpack.controller";
        const string Clip1HFbx = "Assets/Animations/Props Animations/Animations/Medievil.fbx";
        const string Clip1HName = "1HandSmallWeaponJumpAttack";
        const string Clip2HFbx = "Assets/PROTOFACTOR/Ultimate Animation Collection/Animations/2Handed Melee Weapon Animset/FBX Files/Humanoid@JumpAttack2HandMelee.fbx";

        [MenuItem("Tools/Dark Matter Genesis/Combat/Build Jump Attack States")]
        public static string Build()
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (ctrl == null) return "controller missing";
            bool hasParam = false;
            foreach (var p in ctrl.parameters) if (p.name == Project.Combat.DMPlayerJumpAttack.SpeedParam) hasParam = true;
            if (!hasParam) ctrl.AddParameter(new AnimatorControllerParameter { name = Project.Combat.DMPlayerJumpAttack.SpeedParam, type = AnimatorControllerParameterType.Float, defaultFloat = 1f });

            int li = -1;
            for (int i = 0; i < ctrl.layers.Length; i++) if (ctrl.layers[i].name == "FullBody") li = i;
            if (li < 0) return "FullBody missing";
            var root = ctrl.layers[li].stateMachine;
            AnimatorStateMachine attacks = null;
            foreach (var c in root.stateMachines) if (c.stateMachine.name == "Attacks") attacks = c.stateMachine;
            if (attacks == null) return "Attacks missing";

            AnimatorState idle = null;
            foreach (var s in attacks.states) if (s.state.name == "Idle_Empty") idle = s.state;
            vMeleeAttackControl template = FindTemplate(attacks);

            string r = "";
            r += Make(attacks, idle, template, Project.Combat.DMPlayerJumpAttack.State1H, LoadClip(Clip1HFbx, Clip1HName), 0.3f, 0.55f, new Vector3(650, 520, 0));
            r += Make(attacks, idle, template, Project.Combat.DMPlayerJumpAttack.State2H, LoadClip(Clip2HFbx, null), 0.45f, 0.65f, new Vector3(650, 590, 0));
            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            return r;
        }

        static vMeleeAttackControl FindTemplate(AnimatorStateMachine sm)
        {
            foreach (var s in sm.states)
                if (s.state.name == "SwordAttack C")
                    foreach (var b in s.state.behaviours) if (b is vMeleeAttackControl v) return v;
            foreach (var c in sm.stateMachines) { var t = FindTemplate(c.stateMachine); if (t != null) return t; }
            return null;
        }

        static AnimationClip LoadClip(string fbx, string name)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(fbx))
                if (o is AnimationClip c && !c.name.StartsWith("__preview__") && (name == null || c.name == name)) return c;
            return null;
        }

        static string Make(AnimatorStateMachine sm, AnimatorState idle, vMeleeAttackControl tpl, string name, AnimationClip clip, float s0, float s1, Vector3 pos)
        {
            if (clip == null) return name + ": clip missing; ";
            AnimatorState st = null;
            foreach (var s in sm.states) if (s.state.name == name) st = s.state;
            if (st == null) st = sm.AddState(name, pos);
            st.motion = clip;
            st.writeDefaultValues = true;
            st.speedParameter = Project.Combat.DMPlayerJumpAttack.SpeedParam;
            st.speedParameterActive = true;
            st.tag = "Attack";
            vMeleeAttackControl ac = null;
            foreach (var b in st.behaviours) if (b is vMeleeAttackControl v) ac = v;
            if (ac == null) ac = st.AddStateMachineBehaviour<vMeleeAttackControl>();
            if (tpl != null) EditorUtility.CopySerialized(tpl, ac);
            ac.startDamage = s0; ac.endDamage = s1;
            ac.damageType = Project.Combat.DMPlayerJumpAttack.DamageTypeTag;
            ac.resetAttackTrigger = true;
            ac.meleeAttackType = vAttackType.MeleeWeapon; ac.recoilID = 2; ac.damageMultiplier = 2;
            EditorUtility.SetDirty(ac);
            foreach (var t in st.transitions) st.RemoveTransition(t);
            var ex = st.AddExitTransition(); ex.hasExitTime = true; ex.exitTime = 0.82f; ex.duration = 0.4f; // 25l smooth return to locomotion
            if (idle != null) { var ti = st.AddTransition(idle); ti.hasExitTime = true; ti.exitTime = 0.9f; ti.duration = 0.3f; }
            return name + ": ok (" + clip.name + "); ";
        }
    }
}
