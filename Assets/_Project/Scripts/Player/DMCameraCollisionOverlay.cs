using System;
using System.Reflection;
using Invector.vCamera;
using UnityEngine;

namespace Project.Player
{
    /// <summary>
        /// Keeps the live Invector third-person camera out of buildings, walls, and terrain.
        /// Close collision holds ~2m and slides along the wall — never into the player, never hides the mesh.
        /// Runtime-added (not on Player_v7). Does not retune zoom assets, climb, dash, or jetpack.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10000)]
    public sealed class DMCameraCollisionOverlay : MonoBehaviour
    {
        private const string BuildStamp = "DMCamera 0905-climbfix3";
        private const float SphereRadius = 0.2f;
        private const float ExtraSkin = 0.08f;
        private const float MinFollow = 2.15f;
        private const float ClimbMinFollow = 2.55f;
        private const float MantleMinFollow = 2.9f;
        private const float PullSpeed = 28f;
        private const float ReleaseSpeed = 7f;
        private const float ClimbPullSpeed = 5.5f;
        private const float ClimbReleaseSpeed = 2.8f;
        private const float MantlePullSpeed = 3.0f;
        private const float MantleReleaseSpeed = 2.2f;
        private const float ClimbNearRadius = 4.25f;
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
        private Project.Features.Climb.DMClimbController _climb;
        private float _smoothDist = -1f;
        private bool _tuned;
        private bool _logged;

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
            if (!_logged)
            {
                _logged = true;
                Debug.Log(BuildStamp);
            }
        }

        private void LateUpdate()
        {
            if (!isActiveAndEnabled)
                return;

            CacheRefs();
            // Binoculars disable vThirdPersonCamera (eye pose). Skip third-person push while frozen/disabled
            // or the lens gets yanked from the eye out to CurrentZoom (often high above terrain).
            if (tpCamera == null || tpCamera.isFreezed || !tpCamera.enabled)
            {
                _smoothDist = -1f;
                return;
            }

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

            bool mantling = _climb != null && _climb.IsMantling;
            bool climbing = _climb != null && _climb.IsClimbing;
            bool nearClimb = climbing || NearClimbableGeometry(pivot);
            float minFollow = mantling ? MantleMinFollow : (nearClimb ? ClimbMinFollow : MinFollow);

            float wantDist = Mathf.Max(desiredDist, minFollow);
            float zoom = tpCamera.CurrentZoom;
            if (zoom > minFollow)
                wantDist = Mathf.Max(wantDist, zoom);

            float closest = ClosestEnvHit(pivot, dir, wantDist, radius, pivotTf);
            float targetDist = wantDist;
            bool tightWall = false;
            if (closest < float.PositiveInfinity)
            {
                float allowed = closest - skin;
                if (allowed >= minFollow)
                    targetDist = allowed;
                else
                {
                    targetDist = minFollow;
                    tightWall = true;
                }
            }

            if (_smoothDist < 0f)
                _smoothDist = Mathf.Max(desiredDist, minFollow);

            float dt = Time.deltaTime;
            if (dt <= 0f)
                dt = 0.02f;

            // Climb/mantle: soft pull only — hard snap was the mantle wall/player slam.
            float pull = mantling ? MantlePullSpeed : (nearClimb ? ClimbPullSpeed : PullSpeed);
            float release = mantling ? MantleReleaseSpeed : (nearClimb ? ClimbReleaseSpeed : ReleaseSpeed);
            float speed = targetDist < _smoothDist - 0.01f ? pull : release;
            _smoothDist = Mathf.Lerp(_smoothDist, targetDist, 1f - Mathf.Exp(-speed * dt));
            if (targetDist < _smoothDist)
            {
                if (mantling || nearClimb)
                {
                    float chase = mantling ? 0.12f : 0.22f;
                    float maxStep = Mathf.Max(0.02f, (_smoothDist - targetDist) * chase * Mathf.Max(dt, 0.01f) * 60f);
                    _smoothDist = Mathf.Max(targetDist, _smoothDist - maxStep);
                }
                else
                {
                    _smoothDist = Mathf.Min(_smoothDist, targetDist + 0.02f);
                }
            }
            if (_smoothDist < minFollow)
                _smoothDist = minFollow;

            Vector3 pos = pivot + dir * _smoothDist;
            pos = KeepAboveFloor(pos, radius, pivotTf);
            pos = KeepAboveTerrainSurface(pos, radius);
            if (mantling)
            {
                // During mantle ignore aggressive depenetrate that yanks into wall/player.
                pos = EnforceMinFollow(pivot, pos, dir, minFollow);
            }
            else if (tightWall)
                pos = SlideClearOfEnvironment(pivot, pos, dir, radius, pivotTf, minFollow);
            else
            {
                pos = PullOutOfEnvironment(pivot, pos, radius, pivotTf, minFollow);
                if (!nearClimb)
                    pos = DepenetrateFromEnvironment(pos, radius, pivotTf);
                else
                    pos = EnforceMinFollow(pivot, pos, dir, minFollow);
            }

            pos = EnforceMinFollow(pivot, pos, dir, minFollow);
            pos = KeepAboveTerrainSurface(pos, radius);

            transform.position = pos;
            if (_body != null)
                _body.position = pos;

            Vector3 placed = pos - pivot;
            float placedDist = placed.magnitude;
            if (placedDist > 0.001f)
                _smoothDist = Mathf.Max(placedDist, minFollow);

            tpCamera.distance = _smoothDist;
            SetCullingDistance(Mathf.Max(_smoothDist, minFollow));
        }

        private void ClampScrollZoom()
        {
            if (tpCamera == null || tpCamera.currentState == null)
                return;
            if (IsAimOrScopeState(tpCamera.currentStateName) ||
                IsAimOrScopeState(tpCamera.currentState.Name))
                return;

            float floor = (_climb != null && _climb.IsMantling) ? MantleMinFollow
                : ((_climb != null && _climb.IsClimbing) || NearClimbableGeometry(
                    tpCamera.mainTarget != null ? tpCamera.mainTarget.position : transform.position)
                    ? ClimbMinFollow : MinFollow);
            if (tpCamera.CurrentZoom < 1f)
                tpCamera.SetZoomTarget(floor);
        }

        private static bool IsAimOrScopeState(string stateName)
        {
            if (string.IsNullOrEmpty(stateName))
                return false;
            return stateName.IndexOf("Aim", StringComparison.OrdinalIgnoreCase) >= 0
                   || stateName.IndexOf("Scope", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private Vector3 SlideClearOfEnvironment(
            Vector3 pivot,
            Vector3 pos,
            Vector3 fallbackDir,
            float radius,
            Transform pivotTf,
            float minFollow = MinFollow)
        {
            pos = DepenetrateFromEnvironment(pos, radius, pivotTf);
            pos = EnforceMinFollow(pivot, pos, fallbackDir, minFollow);
            pos = DepenetrateFromEnvironment(pos, radius, pivotTf);
            return EnforceMinFollow(pivot, pos, fallbackDir, minFollow);
        }

        private Vector3 EnforceMinFollow(Vector3 pivot, Vector3 pos, Vector3 fallbackDir, float minFollow = MinFollow)
        {
            Vector3 offset = pos - pivot;
            float dist = offset.magnitude;
            if (dist >= minFollow)
                return pos;

            Vector3 dir = dist > 0.001f
                ? offset / dist
                : (fallbackDir.sqrMagnitude > 0.001f ? fallbackDir.normalized : Vector3.back);
            return pivot + dir * minFollow;
        }

        private Vector3 DepenetrateFromEnvironment(Vector3 pos, float radius, Transform pivotTf)
        {
            float pad = radius + ExtraSkin;
            for (int iter = 0; iter < 6; iter++)
            {
                int n = Physics.OverlapSphereNonAlloc(
                    pos,
                    pad,
                    Overlaps,
                    collisionMask,
                    QueryTriggerInteraction.Ignore);
                Vector3 push = Vector3.zero;
                int hits = 0;
                for (int i = 0; i < n; i++)
                {
                    Collider c = Overlaps[i];
                    if (!IsEnvironmentCollider(c, pivotTf) || !SupportsClosestPoint(c))
                        continue;

                    Vector3 closest = c.ClosestPoint(pos);
                    Vector3 away = pos - closest;
                    float mag = away.magnitude;
                    if (mag < 0.0001f)
                    {
                        away = pos - pivotTf.position;
                        mag = away.magnitude;
                        if (mag < 0.0001f)
                            away = Vector3.up;
                        else
                            away /= mag;
                        mag = 0f;
                    }
                    else
                    {
                        away /= mag;
                    }

                    float penetrate = pad - mag;
                    if (penetrate > 0f)
                    {
                        push += away * penetrate;
                        hits++;
                    }
                }

                if (hits == 0)
                    break;
                pos += push / hits;
            }

            return pos;
        }

        private bool IsEnvironmentCollider(Collider c, Transform pivotTf)
        {
            if (c == null || c.isTrigger)
                return false;

            Transform tr = c.transform;
            if (tr == transform || tr.IsChildOf(transform))
                return false;

            Transform root = playerRoot != null ? playerRoot : (pivotTf != null ? pivotTf.root : null);
            if (root != null && (tr == root || tr.IsChildOf(root)))
                return false;

            int layer = c.gameObject.layer;
            if (layer == PlayerLayer || layer == ClimbableLayer)
                return false;
            if (c is TerrainCollider)
                return false;

            return true;
        }

        /// <summary>
        /// Physics.ClosestPoint only works on box/sphere/capsule and convex mesh.
        /// Terrain and concave cliff meshes spam warnings and shove the camera off the wall.
        /// </summary>
        private static bool SupportsClosestPoint(Collider c)
        {
            if (c == null)
                return false;
            if (c is BoxCollider || c is SphereCollider || c is CapsuleCollider)
                return true;
            MeshCollider mesh = c as MeshCollider;
            return mesh != null && mesh.convex;
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

            if (_climb == null && playerRoot != null)
                _climb = playerRoot.GetComponent<Project.Features.Climb.DMClimbController>()
                    ?? playerRoot.GetComponentInChildren<Project.Features.Climb.DMClimbController>(true);
            if (_climb == null)
                _climb = FindAnyObjectByType<Project.Features.Climb.DMClimbController>(FindObjectsInactive.Include);
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

        private Vector3 PullOutOfEnvironment(Vector3 pivot, Vector3 pos, float radius, Transform pivotTf, float minFollow = MinFollow)
        {
            if (!OverlapsEnvironment(pos, radius, pivotTf))
                return pos;

            Vector3 delta = pos - pivot;
            float dist = delta.magnitude;
            if (dist < 0.02f)
                return pos;

            Vector3 dir = delta / dist;
            float lo = minFollow;
            float hi = dist;
            if (hi < lo)
                return EnforceMinFollow(pivot, pos, dir, minFollow);
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
                if (layer == PlayerLayer || layer == ClimbableLayer)
                    continue;
                if (c is TerrainCollider)
                    continue;
                MeshCollider mesh = c as MeshCollider;
                if (mesh != null && !mesh.convex)
                    continue;

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
                if (hit.collider.gameObject.layer == ClimbableLayer)
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
            if (layer == PlayerLayer || layer == ClimbableLayer)
                return false;
            if (hit.collider is TerrainCollider)
                return true;

            return true;
        }

        private static bool NearClimbableGeometry(Vector3 pivot)
        {
            int mask = 1 << ClimbableLayer;
            if (Physics.CheckSphere(pivot, ClimbNearRadius, mask, QueryTriggerInteraction.Ignore))
                return true;
            // Also soft-damp when the lens itself is skimming a climbable lip/ledge.
            return Physics.CheckSphere(pivot, 1.25f, mask, QueryTriggerInteraction.Collide);
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
