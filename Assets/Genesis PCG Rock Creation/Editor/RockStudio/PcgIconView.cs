#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace GenesisPCG.RockCreation.Editor
{
    /// <summary>
    /// 512x512 orbit preview / icon camera (PreviewRenderUtility, HDRP-safe lighting). Left-drag orbits, wheel zooms.
    /// Renders only when something changed, into ONE persistent render texture; everything (preview scene, camera,
    /// RT, subject) is released on detach, on <see cref="Suspended"/> and before an assembly reload.
    /// </summary>
    public sealed class PcgIconView : VisualElement
    {
        public const int IconSize = 512;
        public const float DefaultYaw = 35f, DefaultPitch = 22f, DefaultZoom = 1f;

        /// <summary>Light multiplier for HDRP preview lights (same as the Rock Palette thumbnails).</summary>
        public static float LightScale => DmRockPaletteWindow.ThumbLightScale;

        public float Yaw { get; private set; } = DefaultYaw;
        public float Pitch { get; private set; } = DefaultPitch;
        public float Zoom { get; private set; } = DefaultZoom;
        public Color Background = new Color(0.125f, 0.114f, 0.098f, 1f); // #201d19
        public event Action ViewChanged;

        private readonly Image m_image;
        private readonly Label m_hint, m_info, m_empty;
        private PreviewRenderUtility m_pru;
        private RenderTexture m_rt;
        private GameObject m_subject;
        private Func<GameObject> m_build;
        private Bounds m_bounds;
        private bool m_needsBuild, m_dirty, m_dragging, m_suspended;
        private Vector2 m_last;
        private double m_retryAt;
        private int m_retries;

        public PcgIconView()
        {
            AddToClassList("pcg-iconview");
            style.width = IconSize;
            style.height = IconSize;
            style.minWidth = IconSize;
            style.minHeight = IconSize;
            style.flexShrink = 0;
            focusable = true;

            m_image = new Image { scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            m_image.AddToClassList("pcg-iconview__image");
            m_image.style.position = Position.Absolute;
            m_image.style.left = 0; m_image.style.top = 0; m_image.style.right = 0; m_image.style.bottom = 0;
            Add(m_image);

            m_empty = new Label("Drop prefabs / meshes or pick a preset to preview") { pickingMode = PickingMode.Ignore };
            m_empty.AddToClassList("pcg-iconview__empty");
            Add(m_empty);
            m_hint = new Label("LMB drag: orbit   ·   wheel: zoom") { pickingMode = PickingMode.Ignore };
            m_hint.AddToClassList("pcg-iconview__hint");
            Add(m_hint);
            m_info = new Label { pickingMode = PickingMode.Ignore };
            m_info.AddToClassList("pcg-iconview__info");
            Add(m_info);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCaptureOutEvent>(_ => m_dragging = false);
            RegisterCallback<WheelEvent>(OnWheel);
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                AssemblyReloadEvents.beforeAssemblyReload -= Release;
                AssemblyReloadEvents.beforeAssemblyReload += Release;
                m_needsBuild = m_build != null;
                m_dirty = true;
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                AssemblyReloadEvents.beforeAssemblyReload -= Release;
                Release();
            });
            schedule.Execute(Tick).Every(33);
            UpdateInfo();
        }

        /// <summary>True: frees the preview (GPU memory) until set false again (e.g. while the host tab is hidden).</summary>
        public bool Suspended
        {
            get => m_suspended;
            set
            {
                if (m_suspended == value) return;
                m_suspended = value;
                if (value) Release();
                else { m_needsBuild = m_build != null; m_dirty = true; }
            }
        }

        public bool HasSubject => m_subject != null;
        public Bounds SubjectBounds => m_bounds;

        /// <summary>Object to show (created inactive by <paramref name="build"/>; the view activates, owns and destroys it).</summary>
        public void SetSubject(Func<GameObject> build)
        {
            m_build = build;
            m_needsBuild = true;
            m_dirty = true;
            m_retries = 0;
        }

        public void SetView(float yaw, float pitch, float zoom)
        {
            Yaw = Mathf.Repeat(yaw, 360f);
            Pitch = Mathf.Clamp(pitch, -20f, 85f);
            Zoom = Mathf.Clamp(zoom, 0.35f, 4f);
            m_dirty = true;
            UpdateInfo();
            ViewChanged?.Invoke();
        }

        public void ResetView() => SetView(DefaultYaw, DefaultPitch, DefaultZoom);

        // ------------------------------------------------------------------------------------------------
        // Input

        private void OnPointerDown(PointerDownEvent e)
        {
            if (e.button != 0) return;
            m_dragging = true;
            m_last = e.position;
            this.CapturePointer(e.pointerId);
            Focus();
            e.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent e)
        {
            if (!m_dragging || !this.HasPointerCapture(e.pointerId)) return;
            Vector2 p = e.position, d = p - m_last;
            m_last = p;
            SetView(Yaw + d.x * 0.45f, Pitch + d.y * 0.3f, Zoom);
            e.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent e)
        {
            if (e.button != 0) return;
            m_dragging = false;
            if (this.HasPointerCapture(e.pointerId)) this.ReleasePointer(e.pointerId);
            e.StopPropagation();
        }

        private void OnWheel(WheelEvent e)
        {
            SetView(Yaw, Pitch, Zoom * (1f - Mathf.Clamp(e.delta.y, -6f, 6f) * 0.04f));
            e.StopPropagation();
        }

        private void UpdateInfo() => m_info.text = $"yaw {Yaw:0}°  pitch {Pitch:0}°  zoom {Zoom:0.00}";

        // ------------------------------------------------------------------------------------------------
        // Rendering

        private void Tick()
        {
            if (panel == null || m_suspended || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (m_needsBuild) BuildSubject();
            if (!m_dirty || m_subject == null) return;
            if (m_retryAt > 0 && EditorApplication.timeSinceStartup < m_retryAt && !m_dragging) return;
            m_dirty = false;
            m_retryAt = 0;
            bool complete = RenderTo(EnsureRT());
            m_image.image = m_rt;
            m_image.MarkDirtyRepaint();
            if (!complete && m_retries++ < 40)
            {
                // Shaders still compiling asynchronously: render again shortly.
                m_dirty = true;
                m_retryAt = EditorApplication.timeSinceStartup + 0.5;
            }
        }

        private void BuildSubject()
        {
            m_needsBuild = false;
            DestroySubject();
            m_empty.style.display = DisplayStyle.Flex;
            if (m_build == null) { m_image.image = null; return; }
            GameObject go = null;
            try { go = m_build(); }
            catch (Exception e) { Debug.LogException(e); }
            if (go == null) { m_image.image = null; return; }
            EnsurePru();
            go.hideFlags = HideFlags.HideAndDontSave;
            m_pru.AddSingleGO(go);
            go.SetActive(true); // builds (rock combiners rebuild on enable)
            foreach (Collider col in go.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(col); // picture only: keep the preview out of physics
            m_subject = go;
            m_bounds = ComputeBounds(go);
            m_empty.style.display = m_bounds.size.sqrMagnitude > 0f ? DisplayStyle.None : DisplayStyle.Flex;
            m_dirty = true;
            m_retries = 0;
        }

        private static Bounds ComputeBounds(GameObject go)
        {
            bool first = true;
            Bounds b = new Bounds(go.transform.position, Vector3.zero);
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(false))
            {
                if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
                if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
            }
            return b;
        }

        /// <summary>Renders the current subject from the current orbit into <paramref name="target"/>. False = shaders were still compiling.</summary>
        private bool RenderTo(RenderTexture target)
        {
            if (m_subject == null || m_pru == null) return true;
            // Rocks rebuild on enable: refresh the framing if the mesh changed since the build (e.g. first frame).
            m_bounds = ComputeBounds(m_subject);
            Bounds b = m_bounds;
            float radius = Mathf.Max(0.5f, Mathf.Max(b.extents.y, Mathf.Sqrt(b.extents.x * b.extents.x + b.extents.z * b.extents.z)) * 0.9f);
            Camera cam = m_pru.camera;
            cam.fieldOfView = 30f;
            float dist = radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.05f / Zoom;
            cam.nearClipPlane = Mathf.Max(0.02f, dist - radius * 3f);
            cam.farClipPlane = dist + radius * 4f + 10f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Background;
            cam.aspect = 1f;
            Quaternion view = Quaternion.Euler(Pitch, Yaw, 0f);
            cam.transform.SetPositionAndRotation(b.center - view * Vector3.forward * dist, view);
            // Key light follows the orbit (icons from any angle are lit the same way), fill from the opposite side.
            m_pru.lights[0].intensity = 1.3f * LightScale;
            m_pru.lights[0].transform.rotation = Quaternion.Euler(45f, Yaw - 5f, 0f);
            if (m_pru.lights.Length > 1)
            {
                m_pru.lights[1].intensity = 0.6f * LightScale;
                m_pru.lights[1].transform.rotation = Quaternion.Euler(340f, Yaw + 183f, 177f);
            }
            float amb = DmRockPaletteWindow.ThumbAmbient;
            m_pru.ambientColor = new Color(amb, amb, amb * 1.08f);

            bool prevAsync = ShaderUtil.allowAsyncCompilation;
            RenderTexture prevActive = RenderTexture.active;
            try
            {
                foreach (Light l in m_pru.lights) if (l != null) l.enabled = true;
                cam.targetTexture = target;
                ShaderUtil.allowAsyncCompilation = true;
                cam.Render();
                return !ShaderUtil.anythingCompiling;
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = prevAsync;
                cam.targetTexture = null;
                foreach (Light l in m_pru.lights) if (l != null) l.enabled = false;
                RenderTexture.active = prevActive;
            }
        }

        /// <summary>
        /// 512x512 (or <paramref name="size"/>) picture of the subject from the current orbit, as a new readable texture
        /// (caller owns it). Null when there is nothing to show. <paramref name="complete"/> false = shaders still compiling.
        /// </summary>
        public Texture2D Capture(out bool complete, int size = IconSize)
        {
            complete = false;
            if (m_suspended) return null;
            if (m_needsBuild || m_subject == null) BuildSubject();
            if (m_subject == null) return null;
            RenderTexture rt = RenderTexture.GetTemporary(new RenderTextureDescriptor(size, size, RenderTextureFormat.ARGB32, 24) { sRGB = true, msaaSamples = 1 });
            RenderTexture prevActive = RenderTexture.active;
            try
            {
                RenderTo(rt); // first frame can miss lighting history; the second is the keeper
                complete = RenderTo(rt);
                RenderTexture.active = rt;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                tex.ReadPixels(new Rect(0, 0, size, size), 0, 0, false);
                tex.Apply(false);
                return tex;
            }
            finally
            {
                RenderTexture.active = prevActive == rt ? null : prevActive;
                RenderTexture.ReleaseTemporary(rt);
                m_dirty = true;
            }
        }

        private void EnsurePru()
        {
            if (m_pru == null) m_pru = new PreviewRenderUtility();
        }

        private RenderTexture EnsureRT()
        {
            if (m_rt != null && m_rt.IsCreated()) return m_rt;
            if (m_rt == null)
                m_rt = new RenderTexture(new RenderTextureDescriptor(IconSize, IconSize, RenderTextureFormat.ARGB32, 24) { sRGB = true, msaaSamples = 1 })
                {
                    name = "PcgIconView", hideFlags = HideFlags.HideAndDontSave,
                };
            m_rt.Create();
            return m_rt;
        }

        private void DestroySubject()
        {
            if (m_subject != null) Object.DestroyImmediate(m_subject);
            m_subject = null;
        }

        /// <summary>Frees the preview scene, camera, render texture and subject (rebuilt lazily when shown again).</summary>
        public void Release()
        {
            DestroySubject();
            if (m_image != null) m_image.image = null;
            if (m_rt != null)
            {
                if (RenderTexture.active == m_rt) RenderTexture.active = null;
                if (m_pru != null && m_pru.camera != null && m_pru.camera.targetTexture == m_rt) m_pru.camera.targetTexture = null;
                m_rt.Release();
                Object.DestroyImmediate(m_rt);
                m_rt = null;
            }
            if (m_pru != null)
            {
                m_pru.Cleanup();
                m_pru = null;
            }
            m_needsBuild = m_build != null;
            m_dirty = true;
        }
    }
}
#endif
