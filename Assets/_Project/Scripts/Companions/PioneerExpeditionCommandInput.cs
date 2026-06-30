using Project.Core;
using Project.Pioneers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Project.Companions
{
    /// <summary>
    /// Ground hold and party-wide follow shortcuts for expedition companions.
    /// </summary>
    public class PioneerExpeditionCommandInput : MonoBehaviour
    {
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField] private float maxRayDistance = 180f;

        private CompanionRosterBridge rosterBridge;

        private void Start()
        {
            rosterBridge = FindAnyObjectByType<CompanionRosterBridge>();
        }

        private void Update()
        {
            if (!GameSession.HasStarted || Keyboard.current == null)
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (Keyboard.current.hKey.wasPressedThisFrame)
                TryHoldAllAtCursor();

            if (Keyboard.current.gKey.wasPressedThisFrame)
                rosterBridge?.SetAllFollow();
        }

        public bool TryHoldAllAtCursor()
        {
            if (rosterBridge == null)
                rosterBridge = FindAnyObjectByType<CompanionRosterBridge>();

            if (rosterBridge == null || !RaycastGround(out Vector3 point, out float facingYaw))
                return false;

            rosterBridge.SetAllHold(point, facingYaw);
            return true;
        }

        public bool TryHoldAllAtWorldPoint(Vector3 worldPoint, float facingYaw)
        {
            if (rosterBridge == null)
                rosterBridge = FindAnyObjectByType<CompanionRosterBridge>();

            if (rosterBridge == null)
                return false;

            rosterBridge.SetAllHold(worldPoint, facingYaw);
            return true;
        }

        private bool RaycastGround(out Vector3 point, out float facingYaw)
        {
            return TryRaycastGround(out point, out facingYaw, maxRayDistance, groundLayers);
        }

        public static bool TryRaycastGround(
            out Vector3 point,
            out float facingYaw,
            float maxDistance = 180f,
            int layerMask = ~0)
        {
            point = Vector3.zero;
            facingYaw = 0f;

            Camera cam = Camera.main;
            if (cam == null)
                return false;

            Vector2 screen = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            Ray ray = cam.ScreenPointToRay(screen);
            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, layerMask, QueryTriggerInteraction.Ignore))
                return false;

            point = hit.point;
            facingYaw = cam.transform.eulerAngles.y;
            return true;
        }

        public static Vector3 ResolveHoldPointNearPlayer(float forwardDistance = 3.5f)
        {
            GameObject player = PlayerLocator.FindPlayerObject();
            if (player == null)
                return Vector3.zero;

            Vector3 origin = player.transform.position;
            Vector3 forward = player.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;

            Vector3 target = origin + forward.normalized * forwardDistance;
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
                target.y = terrain.SampleHeight(target) + terrain.transform.position.y;

            return target;
        }
    }
}
