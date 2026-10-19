#if UNITY_EDITOR
using FORGE3D;
using Project.Combat.Turrets;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Combat
{
    [CustomEditor(typeof(F3DTurret))]
    public sealed class F3DTurretDmInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DMTurretRuntime runtime = ((F3DTurret)target).GetComponent<DMTurretRuntime>();
            if (runtime == null)
            {
                DrawDefaultInspector();
                return;
            }

            EditorGUILayout.HelpBox(
                "This turret is driven by DM Turret Runtime on the same GameObject. "
                + "Edit Tracking Authority on that component (or use Capture / Apply buttons there).",
                MessageType.Info);
            if (GUILayout.Button("Select DM Turret Runtime"))
                Selection.activeObject = runtime;

            EditorGUI.BeginDisabledGroup(true);
            DrawDefaultInspector();
            EditorGUI.EndDisabledGroup();
        }
    }

    [CustomEditor(typeof(DMTurretBrain))]
    public sealed class DMTurretBrainDmInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DMTurretRuntime runtime = ((DMTurretBrain)target).GetComponent<DMTurretRuntime>();
            EditorGUILayout.HelpBox(
                runtime != null
                    ? "Auto brain reads control mode and combat tuning from DM Turret Runtime only."
                    : "Add DM Turret Runtime on this root.",
                MessageType.Info);
            if (runtime != null && GUILayout.Button("Select DM Turret Runtime"))
                Selection.activeObject = runtime;
        }
    }

    [CustomEditor(typeof(DMTurretHealth))]
    public sealed class DMTurretHealthDmInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var health = (DMTurretHealth)target;
            DMTurretRuntime runtime = health.GetComponent<DMTurretRuntime>();
            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField("Current HP", health.CurrentHealth);
            EditorGUILayout.FloatField("Max HP", health.MaxHealth);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.HelpBox("Max health is tuned on DM Turret Runtime → Health & Interact / Instance Authority.", MessageType.None);
            if (runtime != null && GUILayout.Button("Select DM Turret Runtime"))
                Selection.activeObject = runtime;
        }
    }

    [CustomEditor(typeof(F3DFXController))]
    public sealed class F3DFXControllerDmInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            F3DFXController fx = (F3DFXController)target;
            DMTurretRuntime runtime = fx.GetComponentInParent<DMTurretRuntime>();
            if (runtime == null)
            {
                DrawDefaultInspector();
                return;
            }

            EditorGUILayout.HelpBox(
                "Prefab FX refs stay here. DefaultFXType and Vulcan fire rate are pushed from DM Turret Runtime (Combat & Ammo / FX / Vendor).",
                MessageType.Info);
            if (GUILayout.Button("Select DM Turret Runtime"))
                Selection.activeObject = runtime;

            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(DMTurretInteract))]
    public sealed class DMTurretInteractDmInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var interact = (DMTurretInteract)target;
            DMTurretRuntime runtime = interact.Runtime;
            EditorGUILayout.HelpBox("Interact range and prompts are driven by DM Turret Runtime.", MessageType.None);
            SerializedProperty collider = serializedObject.FindProperty("interactCollider");
            if (collider != null)
                EditorGUILayout.PropertyField(collider);
            if (runtime != null && GUILayout.Button("Select DM Turret Runtime"))
                Selection.activeObject = runtime;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
