using System;
using System.Collections.Generic;
using System.Reflection;
using Invector.vCamera;
using UnityEngine;

namespace Project.Player
{
    /// <summary>
    /// Keeps the live Invector third-person camera out of buildings, walls, and terrain,
    /// and never leaves the lens inside the player mesh (AAA close-cam: push out, or hide).
    /// Runtime-added (not on Player_v7). Does not retune zoom assets, climb, dash, or jetpack.
    /// DMCamera 0831-climbtile
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10000)]
    public sealed class DMCameraCollisionOverlay : MonoBehaviour
    {
        private const string BuildStamp = "DMCamera 0831-climbtile";
        private const float SphereRadius = 0.2f;
        private const float ExtraSkin = 0.08f;
        private const float MinFollow = 0.7f;
        private const float ChestRadius = 0.75f;
        private const float ChestHeight = 1.0f;
        private const float HeadRadius = 0.55f;
        private const float HeadHeight = 1.65f;
        private const float HideHeadDistance = 1.15f;
        private const float BoundsPad = 0.05f;
        private const float PullSpeed = 28f;
        private const float ReleaseSpeed = 7f;
        private const float FloorProbe = 3.0f;
        private const int PlayerLayer = 8;
        private const int ClimbableLayer = 23;

        private static readonly RaycastHit[] Hits = new RaycastHit[16];
        private static readonly RaycastHit[] RayHits = new RaycastHit[16];
        private static readonly Collider[] Overlaps = new Collider[24];
        private static PropertyInfo CullingDistanceProperty;

        [SerializeField] private vThirdPersonCamera tpCamera;
        [SerializeField] private Camera eye;
        [SerializeField] private Transform playerRoot;
        [SerializeField] private LayerMask collisionMask;

        private Rigidbody _body;
        private float _smoothDist = -1f;
        private bool _tuned;
        private bool _logged;
        private readonly List<Renderer> _playerRenderers = new List<Renderer>(32);
        private readonly List<bool> _rendererWasOff = new List<bool>(32);
        private int _rendererCacheFrame = -999;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOnLoad()
        {
            if (!Application.isPlaying)
                return;

            EnsureOnCamera(GameObject.Find("Player_v7"));
        }

        public static DMCameraCollisionOverlay EnsureOnCamera(GameObject playerRoot)
        {
            DMCameraCollisionOverlay existing =
                FindAnyObjectByType<DMCameraCollisionOverlay>(FindObjectsInactive.Include);
            if (existing != null)
                return existing;

            vThirdPersonCamera cam = null;
            if (playerRoot != null)
                cam = playerRoot.GetComponentInChildren<vThirdPersonCamera>(true);
            if (cam == null)
                cam = FindAnyObjectByType<vThirdPersonCamera>(FindObjectsInactive.Include);
            if (cam == null)
                return null;

            existing = cam.GetComponent<DMCameraCollisionOverlay>();
            if (existing == null)
                existing = cam.gameObject.AddComponent<DMCameraCollisionOverlay>();
            return existing;
        }

        private void Awake()
        {
            CacheRefs();
            TuneInvector();
        }

        private void Start()
        {
            CacheRefs();
            TuneInvector();
            CachePlayerRenderers(true);
            if (!_logged)
            {
                _logged = true;
                Debug.Log(BuildStamp);
            }
        }

        private void OnDisable()
        {
            RestorePlayerRenderers();
        }

        private void OnDestroy()
        {
            RestorePlayerRenderers();
        }

        private void LateUpdate()
        {
            if (!isActiveAndEnabled)
                return;

            CacheRefs();
            if (tpCamera == null || tpCamera.isFreezed)
                return;

            Transform pivotTf = tpCamera.mainTarget != null
                ? tpCamera.mainTarget
                : playerRoot;
            if (pivotTf == null)
                return;

            if (collisionMask.value == 0)
                collisionMask = BuildMask();

            ClampScrollZoom();

            Vector3 pivot = ResolvePivot(pivotTf);
            Vector3 desired = transform.position;
            Vector3 toCam = desired - pivot;
            float desiredDist = toCam.magnitude;
            if (desiredDist < 0.001f)
                return;

            Vector3 dir = toCam / desiredDist;
            float radius = SphereRadius;
            if (eye != null)
                radius = Mathf.Max(radius, eye.nearClipPlane * 0.55f);

            float skin = ExtraSkin;
            if (eye != null)
                skin += eye.nearClipPlane;

            float envBlocked = desiredDist;
            float closest = ClosestEnvHit(pivot, dir, desiredDist, radius, pivotTf);

            if (closest < float.PositiveInfinity)
            {
                float allowed = closest - skin;
                if (allowed < envBlocked)
                    envBlocked = allowed;
            }

            float wallFloor = 0.05f;
            if (eye != null)
                wallFloor = Mathf.Max(wallFloor, eye.nearClipPlane * 0.25f);
            envBlocked = Mathf.Max(envBlocked, wallFloor);

            Vector3 head = ResolveHead(pivotTf);
            Vector3 chest = ResolveChest(pivotTf);
            float bodyKeep = MinFollow;
            bodyKeep = Mathf.Max(bodyKeep, SphereExitT(pivot, dir, head, HeadRadius));
            bodyKeep = Mathf.Max(bodyKeep, SphereExitT(pivot, dir, chest, ChestRadius));

            bool envHit = closest < float.PositiveInfinity;
            bool wallWins = envBlocked + 0.001f < bodyKeep;
            float targetDist = wallWins
                ? envBlocked
                : Mathf.Max(envBlocked, bodyKeep);

            // Do not pin follow distance below scroll zoom unless an actual env hit.
            // Climbable 23 stays in the keep-out mask and still counts as a hit.
            if (!envHit)
            {
                float zoom = tpCamera.CurrentZoom;
                if (zoom > 0.01f)
                    targetDist = Mathf.Max(targetDist, zoom);
            }

            if (_smoothDist < 0f)
                _smoothDist = Mathf.Max(desiredDist, wallWins ? envBlocked : bodyKeep);

            if (!wallWins && _smoothDist < bodyKeep - 0.01f)
                _smoothDist = bodyKeep;

            float dt = Time.deltaTime;
            if (dt <= 0f)
                dt = 0.02f;

            float speed = targetDist < _smoothDist - 0.01f ? PullSpeed : ReleaseSpeed;
            _smoothDist = Mathf.Lerp(_smoothDist, targetDist, 1f - Mathf.Exp(-speed * dt));
            if (targetDist < _smoothDist)
                _smoothDist = Mathf.Min(_smoothDist, targetDist + 0.02f);
            if (!wallWins && _smoothDist < bodyKeep)
                _smoothDist = bodyKeep;

            Vector3 pos = pivot + dir * _smoothDist;
            if (!wallWins)
                pos = PushOutOfKeepOutSpheres(pos, head, chest, dir);

            pos = KeepAboveFloor(pos, radius, pivotTf);
            pos = KeepAboveTerrainSurface(pos, radius);
            pos = PullOutOfEnvironment(pivot, pos, radius, pivotTf);

            transform.position = pos;
            if (_body != null)
                _body.position = pos;

            Vector3 placed = pos - pivot;
            float placedDist = placed.magnitude;
            if (placedDist > 0.001f)
                _smoothDist = placedDist;

            tpCamera.distance = _smoothDist;
            SetCullingDistance(_smoothDist);

            UpdatePlayerMeshVisibility(pos, head);
        }

        private void ClampScrollZoom()
        {
            if (tpCamera == null || tpCamera.currentState == null)
                return;
            if (IsAimOrScopeState(tpCamera.currentStateName) ||
                IsAimOrScopeState(tpCamera.currentState.Name))
                return;

            if (tpCamera.currentState.minDistance < MinFollow)
                tpCamera.currentState.minDistance = MinFollow;

            if (tpCamera.CurrentZoom < MinFollow)
                tpCamera.SetZoomTarget(MinFollow);
        }

        private static bool IsAimOrScopeState(string stateName)
        {
            if (string.IsNullOrEmpty(stateName))
                return false;
            return stateName.IndexOf("Aim", StringComparison.OrdinalIgnoreCase) >= 0
                   || stateName.IndexOf("Scope", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private Vector3 PushOutOfKeepOutSpheres(Vector3 pos, Vector3 head, Vector3 chest, Vector3 fallbackDir)
        {
            pos = RadialPush(pos, head, HeadRadius, fallbackDir);
            pos = RadialPush(pos, chest, ChestRadius, fallbackDir);
            pos = RadialPush(pos, head, HeadRadius, fallbackDir);
            return pos;
        }

        private static Vector3 RadialPush(Vector3 pos, Vector3 center, float radius, Vector3 fallbackDir)
        {
            Vector3 delta = pos - center;
            float sqr = delta.sqrMagnitude;
            float minSqr = radius * radius;
            if (sqr >= minSqr)
                return pos;

            if (sqr < 1e-8f)
            {
                Vector3 axis = fallbackDir.sqrMagnitude > 1e-8f ? fallbackDir : Vector3.back;
                return center + axis.normalized * radius;
            }

            return center + delta * (radius / Mathf.Sqrt(sqr));
        }

        private static float SphereExitT(Vector3 origin, Vector3 dir, Vector3 center, float radius)
        {
            Vector3 oc = origin - center;
            float c = Vector3.Dot(oc, oc) - radius * radius;
            if (c >= 0f)
                return 0f;

            float b = Vector3.Dot(oc, dir);
            float disc = b * b - c;
            if (disc <= 0f)
                return MinFollow;

            float tExit = -b + Mathf.Sqrt(disc);
            return Mathf.Max(0f, tExit);
        }

        private void CachePlayerRenderers(bool force = false)
        {
            if (!force && Time.frameCount - _rendererCacheFrame < 30 && _playerRenderers.Count > 0)
                return;

            RestorePlayerRenderers();
            _playerRenderers.Clear();
            _rendererWasOff.Clear();
            _rendererCacheFrame = Time.frameCount;

            Transform root = playerRoot;
            if (root == null)
                return;

            Renderer[] found = root.GetComponentsInChildren<Renderer>(true);
            Transform camRoot = transform;
            for (int i = 0; i < found.Length; i++)
            {
                Renderer r = found[i];
                if (!IsHidablePlayerRenderer(r, camRoot))
                    continue;
                _playerRenderers.Add(r);
                _rendererWasOff.Add(r.forceRenderingOff);
            }
        }

        private static bool IsHidablePlayerRenderer(Renderer r, Transform camRoot)
        {
            if (r == null)
                return false;
            if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer)
                return false;
            if (r.GetComponent<Camera>() != null)
                return false;
            if (camRoot != null && (r.transform == camRoot || r.transform.IsChildOf(camRoot)))
                return false;

            int layer = r.gameObject.layer;
            if (layer == 5)
                return false;

            string layerName = LayerMask.LayerToName(layer);
            if (layerName == "UI" || layerName == "UI_3D" || layerName == "HeadTrack")
                return false;

            return true;
        }

        private void UpdatePlayerMeshVisibility(Vector3 camPos, Vector3 head)
        {
            CachePlayerRenderers();

            float headDist = Vector3.Distance(camPos, head);
            bool closeToHead = headDist < HideHeadDistance;

            for (int i = 0; i < _playerRenderers.Count; i++)
            {
                Renderer r = _playerRenderers[i];
                if (r == null)
                    continue;

                bool hide = false;
                if (closeToHead)
                {
                    hide = true;
                }
                else
                {
                    Bounds b = r.bounds;
                    b.Expand(BoundsPad);
                    if (b.Contains(camPos))
                        hide = true;
                }

                bool desiredOff = hide || _rendererWasOff[i];
                if (r.forceRenderingOff != desiredOff)
                    r.forceRenderingOff = desiredOff;
            }
        }

        private void RestorePlayerRenderers()
        {
            for (int i = 0; i < _playerRenderers.Count; i++)
            {
                Renderer r = _playerRenderers[i];
                if (r == null)
                    continue;
                bool original = i < _rendererWasOff.Count && _rendererWasOff[i];
                if (r.forceRenderingOff != original)
                    r.forceRenderingOff = original;
            }

        }

        private void CacheRefs()
        {
            if (tpCamera == null)
                tpCamera = GetComponent<vThirdPersonCamera>() ??
                           GetComponentInParent<vThirdPersonCamera>();
            if (eye == null)
            {
                if (tpCamera != null && tpCamera.targetCamera != null)
                    eye = tpCamera.targetCamera;
                else
                    eye = GetComponentInChildren<Camera>(true);
            }

            if (playerRoot == null)
            {
                if (tpCamera != null && tpCamera.mainTarget != null)
                    playerRoot = tpCamera.mainTarget.root;
                else
                {
                    GameObject player = GameObject.Find("Player_v7");
                    if (player != null)
                        playerRoot = player.transform;
                }
            }

            if (_body == null)
                _body = GetComponent<Rigidbody>();
        }

        private void TuneInvector()
        {
            if (_tuned || tpCamera == null)
                return;

            collisionMask = BuildMask();
            tpCamera.cullingLayer = collisionMask;
            if (tpCamera.clipPlaneMargin < 0.2f)
                tpCamera.clipPlaneMargin = 0.25f;
            if (tpCamera.checkHeightRadius < 0.15f)
                tpCamera.checkHeightRadius = 0.2f;
            _tuned = true;
        }

        private Vector3 ResolvePivot(Transform pivotTf)
        {
            float height = tpCamera.offSetPlayerPivot;
            if (tpCamera.currentState != null)
                height += tpCamera.currentState.height;
            if (height < 0.4f)
                height = 1.55f;
            return pivotTf.position + pivotTf.up * height;
        }

        private static Vector3 ResolveHead(Transform pivotTf)
        {
            return pivotTf.position + pivotTf.up * HeadHeight;
        }

        private static Vector3 ResolveChest(Transform pivotTf)
        {
            return pivotTf.position + pivotTf.up * ChestHeight;
        }

        private float ClosestEnvHit(Vector3 pivot, Vector3 dir, float desiredDist, float radius, Transform pivotTf)
        {
            float closest = float.PositiveInfinity;
            int count = Physics.SphereCastNonAlloc(
                pivot,
                radius,
                dir,
                Hits,
                desiredDist,
                collisionMask,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = Hits[i];
                if (!IsEnvironmentHit(hit, pivotTf))
                    continue;
                if (hit.distance < closest)
                    closest = hit.distance;
            }

            // TerrainCollider often misses spherecasts once the lens is already inside the heightfield.
            int rayCount = Physics.RaycastNonAlloc(
                pivot,
                dir,
                RayHits,
                desiredDist,
                collisionMask,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < rayCount; i++)
            {
                RaycastHit hit = RayHits[i];
                if (!IsEnvironmentHit(hit, pivotTf))
                    continue;
                if (hit.distance < closest)
                    closest = hit.distance;
            }

            return closest;
        }

        private Vector3 KeepAboveTerrainSurface(Vector3 pos, float radius)
        {
            Terrain[] terrains = Terrain.activeTerrains;
            if (terrains == null || terrains.Length == 0)
                return pos;

            float pad = ExtraSkin + radius;
            for (int i = 0; i < terrains.Length; i++)
            {
                Terrain terrain = terrains[i];
                if (terrain == null || !terrain.enabled)
                    continue;
                TerrainData data = terrain.terrainData;
                if (data == null)
                    continue;

                Vector3 origin = terrain.GetPosition();
                Vector3 size = data.size;
                float x = pos.x - origin.x;
                float z = pos.z - origin.z;
                if (x < 0f || z < 0f || x > size.x || z > size.z)
                    continue;

                float surfaceY = terrain.SampleHeight(pos) + origin.y;
                float minY = surfaceY + pad;
                if (pos.y < minY)
                    pos.y = minY;
            }

            return pos;
        }

        private Vector3 PullOutOfEnvironment(Vector3 pivot, Vector3 pos, float radius, Transform pivotTf)
        {
            if (!OverlapsEnvironment(pos, radius, pivotTf))
                return pos;

            Vector3 delta = pos - pivot;
            float dist = delta.magnitude;
            if (dist < 0.02f)
                return pos;

            Vector3 dir = delta / dist;
            float lo = 0.05f;
            float hi = dist;
            for (int i = 0; i < 10; i++)
            {
                float mid = (lo + hi) * 0.5f;
                Vector3 test = pivot + dir * mid;
                if (OverlapsEnvironment(test, radius, pivotTf))
                    hi = mid;
                else
                    lo = mid;
            }

            return pivot + dir * lo;
        }

        private bool OverlapsEnvironment(Vector3 pos, float radius, Transform pivotTf)
        {
            int n = Physics.OverlapSphereNonAlloc(
                pos,
                radius,
                Overlaps,
                collisionMask,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = Overlaps[i];
                if (c == null || c.isTrigger)
                    continue;

                Transform tr = c.transform;
                if (tr == transform || tr.IsChildOf(transform))
                    continue;

                Transform root = playerRoot != null ? playerRoot : (pivotTf != null ? pivotTf.root : null);
                if (root != null && (tr == root || tr.IsChildOf(root)))
                    continue;

                int layer = c.gameObject.layer;
                if (layer == PlayerLayer)
                    continue;
                if (c is TerrainCollider)
                    return true;

                return true;
            }

            return false;
        }

        private Vector3 KeepAboveFloor(Vector3 pos, float radius, Transform pivotTf)
        {
            int count = Physics.SphereCastNonAlloc(
                pos + Vector3.up * 0.55f,
                radius,
                Vector3.down,
                Hits,
                FloorProbe,
                collisionMask,
                QueryTriggerInteraction.Ignore);

            float minY = float.NegativeInfinity;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = Hits[i];
                if (!IsEnvironmentHit(hit, pivotTf))
                    continue;
                if (hit.normal.y < 0.45f)
                    continue;
                float y = hit.point.y + radius + ExtraSkin;
                if (y > minY)
                    minY = y;
            }

            if (minY > float.NegativeInfinity && pos.y < minY)
                pos.y = minY;
            return pos;
        }

        private bool IsEnvironmentHit(RaycastHit hit, Transform pivotTf)
        {
            if (hit.collider == null)
                return false;
            if (hit.collider.isTrigger)
                return false;

            Transform t = hit.collider.transform;
            if (t == transform || t.IsChildOf(transform))
                return false;

            Transform root = playerRoot != null ? playerRoot : (pivotTf != null ? pivotTf.root : null);
            if (root != null && (t == root || t.IsChildOf(root)))
                return false;

            int layer = hit.collider.gameObject.layer;
            if (layer == PlayerLayer)
                return false;
            if (hit.collider is TerrainCollider)
                return true;

            return true;
        }

        private static LayerMask BuildMask()
        {
            int mask = Physics.DefaultRaycastLayers;
            mask &= ~LayerBits(
                "Player",
                "Triggers",
                "UI",
                "Ignore Raycast",
                "PW_VFX",
                "HeadTrack",
                "BodyPart",
                "Enemy",
                "CompanionAI",
                "Animal",
                "Item",
                "TransparentFX",
                "PostProcess");

            int terrain = LayerMask.NameToLayer("Terrain");
            if (terrain >= 0)
                mask |= 1 << terrain;

            mask |= 1 << 0;
            mask |= 1 << ClimbableLayer;
            mask |= LayerBits(
                "Climbable",
                "PW_Object_Small",
                "PW_Object_Medium",
                "PW_Object_Large",
                "Resource",
                "Water",
                "StopMove");
            return mask;
        }

        private static int LayerBits(params string[] names)
        {
            int bits = 0;
            for (int i = 0; i < names.Length; i++)
            {
                int layer = LayerMask.NameToLayer(names[i]);
                if (layer >= 0)
                    bits |= 1 << layer;
            }

            return bits;
        }

        private static void SetCullingDistance(vThirdPersonCamera camera, float distance)
        {
            if (camera == null)
                return;
            if (CullingDistanceProperty == null)
            {
                CullingDistanceProperty = typeof(vThirdPersonCamera).GetProperty(
                    "cullingDistance",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }

            CullingDistanceProperty?.SetValue(camera, distance);
        }

        private void SetCullingDistance(float distance)
        {
            SetCullingDistance(tpCamera, distance);
        }
    }
}
