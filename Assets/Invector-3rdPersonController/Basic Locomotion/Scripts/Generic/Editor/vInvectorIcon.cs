using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
namespace Invector
{
    [InitializeOnLoad]
    public class vInvectorIcon
    {
        static Texture2D texturePanel;
        static List<int> markedObjects;
        static vInvectorIcon()
        {
#if UNITY_6000_0_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += ThirdPersonControllerIcon;
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += ThirPersonCameraIcon;
#else
#pragma warning disable CS0618
            EditorApplication.hierarchyWindowItemOnGUI += ThirdPersonControllerIcon;
            EditorApplication.hierarchyWindowItemOnGUI += ThirPersonCameraIcon;
#pragma warning restore CS0618
#endif
        }

#if UNITY_6000_0_OR_NEWER
        static void ThirPersonCameraIcon(EntityId entityId, Rect selectionRect)
        {
            GameObject go = EditorUtility.EntityIdToObject(entityId) as GameObject;
            if (go == null) return;

            var tpCamera = go.GetComponent<vCamera.vThirdPersonCamera>();
            if (tpCamera != null) DrawIcon("tp_camera", selectionRect);
        }

        static void ThirdPersonControllerIcon(EntityId entityId, Rect selectionRect)
        {
            GameObject go = EditorUtility.EntityIdToObject(entityId) as GameObject;
            if (go == null) return;

            var controller = go.GetComponent<Invector.vCharacterController.vThirdPersonController>();
            if (controller != null) DrawIcon("controllerIcon", selectionRect);
        }
#else
        static void ThirPersonCameraIcon(int instanceId, Rect selectionRect)
        {
#pragma warning disable CS0618
            GameObject go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
#pragma warning restore CS0618
            if (go == null) return;

            var tpCamera = go.GetComponent<vCamera.vThirdPersonCamera>();
            if (tpCamera != null) DrawIcon("tp_camera", selectionRect);
        }

        static void ThirdPersonControllerIcon(int instanceId, Rect selectionRect)
        {
#pragma warning disable CS0618
            GameObject go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
#pragma warning restore CS0618
            if (go == null) return;

            var controller = go.GetComponent<Invector.vCharacterController.vThirdPersonController>();
            if (controller != null) DrawIcon("controllerIcon", selectionRect);
        }
#endif


        private static void DrawIcon(string texName, Rect rect)
        {
            // Character controller badge is owned by Dark Matter's DMI hierarchy overlay
            // (Project.EditorTools.DmiCharacterHierarchyIconUtility) — skip the yellow T-pose.
            if (texName == "controllerIcon")
                return;

            Texture2D tex = GetTex(texName);
            if (tex == null)
                return;

            Rect r = new Rect(rect.x + rect.width - 16f, rect.y, 16f, 16f);
            GUI.DrawTexture(r, tex);
        }

        private static Texture2D GetTex(string name)
        {
            return (Texture2D)Resources.Load(name);
        }
    }
}