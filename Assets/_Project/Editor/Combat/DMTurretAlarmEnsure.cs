using FORGE3D;
using Project.Combat.Turrets;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Combat
{
    /// <summary>Creates the optional swivel Alarm sphere and wires <see cref="DMTurretAlarmLight"/>.</summary>
    public static class DMTurretAlarmEnsure
    {
        public const string AlarmMaterialPath = "Assets/_Project/Prefabs/Combat/Turrets/Alarm.mat";

        static readonly Vector3 DefaultLocalPosition = new Vector3(0f, 1.306f, -0.668f);
        static readonly Vector3 DefaultLocalScale = new Vector3(0.28583f, 0.28583f, 0.28583f);

        /// <returns>True when a new Alarm child was created.</returns>
        public static bool EnsureAlarmSphere(GameObject root)
        {
            if (root == null)
                return false;

            if (DMTurretAlarmResolve.TryFindAlarmRenderer(root.transform) != null)
                return false;

            Transform swivel = ResolveSwivelTransform(root);
            if (swivel == null)
            {
                Debug.LogWarning("[DM Turret Alarm] No SOCKET_SWIVEL / SWIVEL on " + root.name + " — skipped Alarm.");
                return false;
            }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(AlarmMaterialPath);
            if (mat == null)
                Debug.LogWarning("[DM Turret Alarm] Missing material at " + AlarmMaterialPath);

            GameObject alarmGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            alarmGo.name = "Alarm";
            Object.DestroyImmediate(alarmGo.GetComponent<Collider>());
            alarmGo.transform.SetParent(swivel, false);
            alarmGo.transform.localPosition = DefaultLocalPosition;
            alarmGo.transform.localRotation = Quaternion.identity;
            alarmGo.transform.localScale = DefaultLocalScale;

            MeshRenderer renderer = alarmGo.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                if (mat != null)
                    renderer.sharedMaterial = mat;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            return true;
        }

        public static int EnsureAlarmOnRoots(System.Collections.Generic.IEnumerable<GameObject> roots)
        {
            int changed = 0;
            foreach (GameObject root in roots)
            {
                if (root == null || root.GetComponent<DMTurretRuntime>() == null)
                    continue;

                bool created = EnsureAlarmSphere(root);
                DMTurretPrefabBakeUtility.WireAlarmLight(root);
                if (created)
                    changed++;
                else if (root.GetComponent<DMTurretAlarmLight>() == null)
                    changed++;
            }

            return changed;
        }

        public static int EnsureAllCombatTurretPrefabs()
        {
            if (!AssetDatabase.IsValidFolder(DMTurretPrefabBakeUtility.CombatTurretFolder))
                return 0;

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { DMTurretPrefabBakeUtility.CombatTurretFolder });
            int changed = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null || prefab.GetComponent<DMTurretRuntime>() == null)
                    continue;

                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    EnsureAlarmSphere(contents);
                    DMTurretPrefabBakeUtility.WireAlarmLight(contents);
                    if (DMTurretAlarmResolve.TryFindAlarmRenderer(contents.transform) != null)
                    {
                        PrefabUtility.SaveAsPrefabAsset(contents, path);
                        changed++;
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            AssetDatabase.SaveAssets();
            return changed;
        }

        static Transform ResolveSwivelTransform(GameObject root)
        {
            F3DTurret forge = root.GetComponent<F3DTurret>();
            if (forge != null && forge.Swivel != null)
                return forge.Swivel.transform;

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            Transform fallback = null;
            for (int i = 0; i < all.Length; i++)
            {
                string n = all[i].name;
                if (n.IndexOf("SOCKET_SWIVEL", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return all[i];
                if (fallback == null && n.Equals("SWIVEL", System.StringComparison.OrdinalIgnoreCase))
                    fallback = all[i];
            }

            return fallback;
        }
    }
}
