using Project.AI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public static class GongoAnimationSetup
    {
        private const string MixamoRoot = "Assets/Animations/Mixamo Animations";
        private const string ControllerPath = ProjectAssetPaths.AnimationsEnemies + "/GongoController.controller";
        private const string PrefabPath = ProjectAssetPaths.PrefabsCombat + "/Gongo.prefab";

        private static readonly (string StateName, string ModelPath, string ClipName)[] ClipAssignments =
        {
            ("Idle01", $"{MixamoRoot}/Bow and Arrow/Movement/Crouch Idle 01.fbx", "mixamo.com"),
            ("Walk", $"{MixamoRoot}/Bow and Arrow/Movement/Crouch Walk Forward (1).fbx", "mixamo.com"),
            ("Run", $"{MixamoRoot}/Bow and Arrow/Movement/Standing Run Forward (1).fbx", "mixamo.com"),
            ("Attack01", $"{MixamoRoot}/Close Combat/Punches/Cross Punch.fbx", "mixamo.com"),
            ("Attack02", $"{MixamoRoot}/Close Combat/Punches/Punching (2).fbx", "mixamo.com"),
            ("Attack03", $"{MixamoRoot}/Close Combat/Punches/Combo Punch.fbx", "mixamo.com"),
            ("Hit01", $"{MixamoRoot}/Hit Reaction/Agony.fbx", "mixamo.com"),
            ("Death", $"{MixamoRoot}/Deaths/Standing Death Forward 01.fbx", "mixamo.com"),
        };

        [MenuItem(SurvivalPioneerEditorMenus.CombatAnimations + "Rebuild Gongo Controller", false, 30)]
        public static void RebuildGongoController()
        {
            EnemyDefinition definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            definition.prefabFileName = "Gongo";
            definition.animatorControllerFileName = "GongoController";
            definition.idleClips = new[] { LoadClip(0) };
            definition.walkClips = new[] { LoadClip(1) };
            definition.runClips = new[] { LoadClip(2) };
            definition.attackClips = new[] { LoadClip(3), LoadClip(4), LoadClip(5) };
            definition.hitClips = new[] { LoadClip(6) };
            definition.deathClips = new[] { LoadClip(7) };

            if (!EnemyAnimationBuilder.HasClipAssignments(definition))
            {
                Object.DestroyImmediate(definition);
                Debug.LogError("GongoAnimationSetup: failed to load Mixamo clips. Check paths under Assets/Animations/Mixamo Animations.");
                return;
            }

            if (!EnemyAnimationSetupUtility.ApplyAnimationToPrefabAsset(PrefabPath, definition))
            {
                Object.DestroyImmediate(definition);
                Debug.LogError($"GongoAnimationSetup: failed to apply animation setup to {PrefabPath}");
                return;
            }

            Object.DestroyImmediate(definition);
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            Debug.Log("Rebuilt GongoController with Mixamo clips and updated Gongo.prefab animator wiring.");
            if (controller != null)
                EditorGUIUtility.PingObject(controller);
        }

        private static AnimationClip LoadClip(int assignmentIndex)
        {
            (string stateName, string modelPath, string clipName) = ClipAssignments[assignmentIndex];
            AnimationClip clip = EnemyAnimationClipUtility.LoadEmbeddedAnimationClip(modelPath, clipName);
            if (clip == null)
                Debug.LogError($"GongoAnimationSetup: missing clip '{clipName}' for state '{stateName}' at {modelPath}.");

            return clip;
        }
    }
}
