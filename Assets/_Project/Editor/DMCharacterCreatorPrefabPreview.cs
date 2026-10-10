#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// IMGUI orbit preview for Character Creator output prefabs (Editor-only).
    /// </summary>
    public static class DMCharacterCreatorPrefabPreview
    {
        const float DefaultHeight = 140f;
        const float DefaultYaw = 25f;
        const float DefaultPitch = 12f;

        static PreviewRenderUtility s_Pru;
        static GameObject s_Instance;
        static string s_PrefabPath;
        static string s_VisualChildName = "Visual";
        static float s_Yaw = DefaultYaw;
        static float s_Pitch = DefaultPitch;
        static int s_DragControlId;
        static bool s_AddedToPru;

        static DMCharacterCreatorPrefabPreview()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Dispose;
            EditorApplication.quitting += Dispose;
        }

        public static void Draw(string outputPrefabPath, string visualChildName, float height = DefaultHeight)
        {
            EditorGUILayout.LabelField("Output Preview", EditorStyles.boldLabel);

            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(height), GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.13f, 1f));

            if (string.IsNullOrEmpty(outputPrefabPath) || !System.IO.File.Exists(outputPrefabPath))
            {
                GUI.Label(rect, "Output prefab not created yet.", EditorStyles.centeredGreyMiniLabel);
                DisposeInstanceOnly();
                s_PrefabPath = null;
                return;
            }

            s_VisualChildName = string.IsNullOrWhiteSpace(visualChildName) ? "Visual" : visualChildName;
            HandleOrbitInput(rect);

            if (s_PrefabPath != outputPrefabPath)
            {
                DisposeInstanceOnly();
                s_PrefabPath = outputPrefabPath;
            }

            EnsureInstance();
            if (s_Instance == null)
            {
                GUI.Label(rect, "Could not load preview.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            EnsurePru();
            RenderPreview(rect);
        }

        static void HandleOrbitInput(Rect rect)
        {
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            Event e = Event.current;
            if (e == null)
                return;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0 && rect.Contains(e.mousePosition))
                    {
                        s_DragControlId = controlId;
                        GUIUtility.hotControl = controlId;
                        e.Use();
                    }

                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId && s_DragControlId == controlId)
                    {
                        s_Yaw += e.delta.x * 0.45f;
                        s_Pitch = Mathf.Clamp(s_Pitch + e.delta.y * 0.3f, -20f, 85f);
                        e.Use();
                    }

                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId)
                    {
                        GUIUtility.hotControl = 0;
                        s_DragControlId = 0;
                        e.Use();
                    }

                    break;
            }
        }

        static void EnsureInstance()
        {
            if (s_Instance != null || string.IsNullOrEmpty(s_PrefabPath))
                return;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(s_PrefabPath);
            if (prefab == null)
                return;

            s_Instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (s_Instance == null)
                return;

            s_Instance.hideFlags = HideFlags.HideAndDontSave;
            s_Instance.SetActive(true);

            Transform visual = s_Instance.transform.Find(s_VisualChildName);
            if (visual != null)
            {
                visual.gameObject.SetActive(true);
                HideWeaponPreviewNodes(s_Instance.transform);
            }
            else
            {
                HideWeaponPreviewNodes(s_Instance.transform);
            }

            foreach (Collider col in s_Instance.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(col);
        }

        static void HideWeaponPreviewNodes(Transform root)
        {
            if (root == null)
                return;

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null || t == root)
                    continue;

                if (DMHumanoidVisualAttachUtility.IsWeaponVisualNode(t))
                    t.gameObject.SetActive(false);
            }
        }

        static void EnsurePru()
        {
            if (s_Pru == null)
                s_Pru = new PreviewRenderUtility();
        }

        static void RenderPreview(Rect rect)
        {
            if (s_Pru == null || s_Instance == null)
                return;

            Bounds bounds = ComputeBounds(s_Instance);
            if (bounds.size.sqrMagnitude < 0.0001f)
            {
                GUI.Label(rect, "No renderers on output prefab.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            float radius = Mathf.Max(0.35f, bounds.extents.magnitude);
            Camera cam = s_Pru.camera;
            cam.fieldOfView = 28f;
            float dist = radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.15f;
            cam.nearClipPlane = Mathf.Max(0.02f, dist - radius * 3f);
            cam.farClipPlane = dist + radius * 4f + 10f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.13f, 1f);
            cam.aspect = rect.width / Mathf.Max(1f, rect.height);

            Quaternion view = Quaternion.Euler(s_Pitch, s_Yaw, 0f);
            cam.transform.SetPositionAndRotation(bounds.center - view * Vector3.forward * dist, view);

            if (s_Pru.lights != null && s_Pru.lights.Length > 0)
            {
                s_Pru.lights[0].intensity = 1.2f;
                s_Pru.lights[0].transform.rotation = Quaternion.Euler(45f, s_Yaw - 5f, 0f);
                s_Pru.lights[0].enabled = true;
                if (s_Pru.lights.Length > 1)
                {
                    s_Pru.lights[1].intensity = 0.55f;
                    s_Pru.lights[1].transform.rotation = Quaternion.Euler(340f, s_Yaw + 183f, 177f);
                    s_Pru.lights[1].enabled = true;
                }
            }

            s_Pru.ambientColor = new Color(0.22f, 0.22f, 0.24f);

            if (!s_AddedToPru)
            {
                s_Pru.AddSingleGO(s_Instance);
                s_AddedToPru = true;
            }

            RenderTexture prev = RenderTexture.active;
            try
            {
                s_Pru.BeginPreview(rect, GUIStyle.none);
                cam.Render();
                Texture tex = s_Pru.EndPreview();
                if (tex != null)
                    GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit, alphaBlend: true);
            }
            finally
            {
                RenderTexture.active = prev;
                if (s_Pru.lights != null)
                {
                    for (int i = 0; i < s_Pru.lights.Length; i++)
                    {
                        if (s_Pru.lights[i] != null)
                            s_Pru.lights[i].enabled = false;
                    }
                }
            }

            GUI.Label(
                new Rect(rect.x + 4f, rect.yMax - 18f, rect.width - 8f, 16f),
                "Drag to orbit",
                EditorStyles.centeredGreyMiniLabel);
        }

        static Bounds ComputeBounds(GameObject go)
        {
            bool first = true;
            Bounds bounds = new Bounds(go.transform.position, Vector3.zero);
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                    continue;

                if (first)
                {
                    bounds = r.bounds;
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            return bounds;
        }

        static void DisposeInstanceOnly()
        {
            s_AddedToPru = false;
            if (s_Instance != null)
            {
                Object.DestroyImmediate(s_Instance);
                s_Instance = null;
            }
        }

        public static void Dispose()
        {
            DisposeInstanceOnly();
            s_PrefabPath = null;
            if (s_Pru != null)
            {
                s_Pru.Cleanup();
                s_Pru = null;
            }
        }
    }
}
#endif
