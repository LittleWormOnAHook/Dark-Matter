using UnityEngine;

namespace Project.Combat.Turrets
{
    /// <summary>
    /// Player seat camera parented to the turret swivel (follows yaw/pitch). Disabled until Enter turret.
    /// </summary>
    public static class DMTurretPlayerCameraSlot
    {
        public const string CameraObjectName = "DMTurretPlayerCamera";
        public const string LegacyCameraObjectName = "Turret Camera";

        public static Camera Ensure(GameObject root)
        {
            if (root == null)
                return null;

            DMTurretProfile profile = DMTurretProfile.Live;
            Transform swivel = FindChild(root.transform, "SOCKET_SWIVEL", "Swivel") ?? root.transform;

            Transform slot = swivel.Find(CameraObjectName);
            if (slot == null)
            {
                for (int i = 0; i < swivel.childCount; i++)
                {
                    Transform child = swivel.GetChild(i);
                    if (child.name == LegacyCameraObjectName)
                    {
                        child.name = CameraObjectName;
                        slot = child;
                        break;
                    }
                }
            }

            if (slot == null)
            {
                GameObject go = new GameObject(CameraObjectName);
                slot = go.transform;
                slot.SetParent(swivel, false);
            }

            ApplyProfileLocalPose(slot, profile);

            Camera cam = slot.GetComponent<Camera>();
            if (cam == null)
                cam = slot.gameObject.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.fieldOfView = profile != null ? profile.PlayerCameraFov : 60f;
            cam.enabled = false;

            AudioListener listener = slot.GetComponent<AudioListener>();
            if (listener == null)
                listener = slot.gameObject.AddComponent<AudioListener>();
            listener.enabled = false;

            RemoveLegacyCamerasOnSwivel(swivel, slot);

            return cam;
        }

        static void RemoveLegacyCamerasOnSwivel(Transform swivel, Transform keep)
        {
            for (int i = swivel.childCount - 1; i >= 0; i--)
            {
                Transform child = swivel.GetChild(i);
                if (child == keep)
                    continue;
                if (child.name != LegacyCameraObjectName)
                    continue;
                if (child.GetComponent<Camera>() == null)
                    continue;
                Object.Destroy(child.gameObject);
            }
        }

        public static void ApplyProfileLocalPose(Transform slot, DMTurretProfile profile)
        {
            if (slot == null)
                return;

            if (profile != null)
            {
                slot.localPosition = profile.PlayerCameraLocalPosition;
                slot.localRotation = Quaternion.Euler(profile.PlayerCameraLocalEuler);
            }
            else
            {
                slot.localPosition = new Vector3(0f, 2.22f, -5.38f);
                slot.localRotation = Quaternion.identity;
            }
        }

        public static bool IsUnderSwivel(Transform cameraTransform, Transform root)
        {
            if (cameraTransform == null || root == null)
                return false;
            Transform swivel = FindChild(root, "SOCKET_SWIVEL", "Swivel");
            if (swivel == null)
                return false;
            return cameraTransform == swivel || cameraTransform.IsChildOf(swivel);
        }

        static Transform FindChild(Transform root, string tokenA, string tokenB)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            Transform fallback = null;
            for (int i = 0; i < all.Length; i++)
            {
                string n = all[i].name;
                if (n.IndexOf(tokenA, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return all[i];
                if (fallback == null && n.IndexOf(tokenB, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    fallback = all[i];
            }

            return fallback;
        }
    }
}
