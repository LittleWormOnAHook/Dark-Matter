#if UNITY_EDITOR
using Invector.vCharacterController;
using Invector.vShooter;
using Project.Player.Invector;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Invector
{
    public static class PlayerInvectorRuntimeSetupEditor
    {
        public static void EnsurePioneerHeadTrack(GameObject root)
        {
            if (root == null)
                return;

            vHeadTrack existing = root.GetComponent<vHeadTrack>();
            if (existing == null)
                return;

            if (existing is PioneerHeadTrack)
                return;

            PioneerHeadTrack pioneerHeadTrack = root.AddComponent<PioneerHeadTrack>();
            EditorUtility.CopySerialized(existing, pioneerHeadTrack);
            Object.DestroyImmediate(existing, true);
        }

        public static void WireRuntimeReferences(GameObject root)
        {
            if (root == null)
                return;

            EnsurePioneerHeadTrack(root);
            RefreshHeadTrackBonesEditor(root);

            PioneerShooterMeleeInput shooterInput = root.GetComponent<PioneerShooterMeleeInput>();
            vHeadTrack headTrack = root.GetComponent<vHeadTrack>();
            PlayerInvectorRuntimeSetup.EnsureThirdPersonCameraRigidbody(root);
            Camera gameplayCamera = PlayerInvectorRuntimeSetup.ResolveGameplayCamera(root, shooterInput);
            if (headTrack != null && gameplayCamera != null)
                headTrack.cameraMain = gameplayCamera;

            vShooterManager shooterManager = root.GetComponent<vShooterManager>();
            PioneerInvectorMeshyAimSnapUtility.ApplyShooterManagerSettings(root, shooterManager);
            if (shooterManager != null)
                EditorUtility.SetDirty(shooterManager);

            EditorUtility.SetDirty(root);
        }

        private static void RefreshHeadTrackBonesEditor(GameObject root)
        {
            vHeadTrack headTrack = root.GetComponent<vHeadTrack>();
            Animator animator = root.GetComponent<Animator>();
            if (headTrack == null || animator == null || !animator.isHuman)
                return;

            Transform headBone = animator.GetBoneTransform(HumanBodyBones.Head);
            if (headBone == null)
                return;

            SerializedObject serializedHeadTrack = new SerializedObject(headTrack);
            serializedHeadTrack.FindProperty("head").objectReferenceValue = headBone;
            SerializedProperty spineProperty = serializedHeadTrack.FindProperty("spine");
            spineProperty.ClearArray();
            serializedHeadTrack.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
