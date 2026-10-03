using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Project.SurfaceCarve
{
    /// <summary>
    /// Play Mode carve brush: click carves once, hold + drag carves continuously.
    /// Uses the screen centre when the cursor is locked (first/third person aim).
    /// </summary>
    [AddComponentMenu("Dark Matter/Environment/Carve Runtime Tool")]
    public class DMCarveRuntimeTool : MonoBehaviour
    {
        public bool toolEnabled = true;
        public Camera viewCamera;
        public DMSurfaceDamageSettings brush = DMSurfaceDamageSettings.Make(DMCarveStyle.Fracture, 0.35f, 0.45f, 1f, 0, 0.35f);
        public LayerMask layers = ~0;
        [Min(1f)] public float maxDistance = 200f;
        public bool spawnDebris = true;

        private bool stroking;
        private Vector3 lastStamp;
        private int seed = 1;

        private void Update()
        {
            if (!toolEnabled || brush == null)
                return;

            Camera cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null)
                return;

            bool down, held;
            Vector2 mouse;
#if ENABLE_INPUT_SYSTEM
            Mouse m = Mouse.current;
            if (m == null)
                return;
            down = m.leftButton.wasPressedThisFrame;
            held = m.leftButton.isPressed;
            mouse = m.position.ReadValue();
#else
            down = Input.GetMouseButtonDown(0);
            held = Input.GetMouseButton(0);
            mouse = Input.mousePosition;
#endif
            if (!held)
            {
                stroking = false;
                return;
            }

            Ray ray = Cursor.lockState == CursorLockMode.Locked
                ? cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                : cam.ScreenPointToRay(mouse);
            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, layers, QueryTriggerInteraction.Ignore))
                return;

            DMCarvable carvable = DMCarvable.ResolveForImpact(hit.collider.gameObject, DMCarveSettings.Active);
            if (carvable == null)
                return;

            float spacing = Mathf.Max(0.01f, brush.radius * DMCarveSettings.Active.dragStampSpacing);
            if (down || !stroking)
            {
                stroking = true;
            }
            else if ((hit.point - lastStamp).sqrMagnitude < spacing * spacing)
            {
                return;
            }

            lastStamp = hit.point;
            seed = seed * 1103515245 + 12345;
            carvable.CarveAtSurface(hit.point, hit.normal, brush, seed & 0x7fffffff, spawnDebris);
        }
    }
}
