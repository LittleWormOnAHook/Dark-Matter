using System;
using System.Reflection;
using Invector.vCamera;
using UnityEngine;

namespace Project.Player
{
    /// <summary>
        /// Keeps the live Invector third-person camera out of buildings, walls, and terrain.
        /// Wall/tent collision pulls the lens in; tight spaces can go closer than normal min follow.
        /// Player mesh hides briefly when the lens must sit inside the body shell, then restores.
        /// Runtime-added (not on Player_v7). Does not retune zoom assets, climb, dash, or jetpack.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10000)]
    public sealed class DMCameraCollisionOverlay : MonoBehaviour
    {
        private const string BuildStamp = "DMCamera 0913-profile";
        private const float DefaultSphereRadius = 0.2f;
        private const float DefaultExtraSkin = 0.08f;
        private const float DefaultMinFollow = 2.15f;
        private const float DefaultClimbMinFollow = 2.55f;
        private const float DefaultMantleMinFollow = 2.9f;
        private const float DefaultPullSpeed = 16f;
        private const float DefaultReleaseSpeed = 5.5f;
        private const float DefaultCollisionHysteresis = 0.12f;
        private const float DefaultClimbPullSpeed = 5.5f;
        private const float DefaultClimbReleaseSpeed = 2.8f;
        private const float DefaultMantlePullSpeed = 3.0f;
        private const float DefaultMantleReleaseSpeed = 2.2f;
        private const float DefaultClimbNearRadius = 4.25f;
        private const float DefaultFloorProbe = 3.0f;
        private const float DefaultTightSpaceMinFollow = 0.4f;
        private const float DefaultPlayerMeshHideDistance = 0.85f;
        private const float DefaultPlayerMeshShowHysteresis = 0.18f;
        private const float DefaultTightSpaceReleaseSpeed = 7.5f;
        private const float DefaultInteriorMaxFollow = 1.35f;
        private const float DefaultInteriorFirstPersonDistance = 0.35f;
        private const float DefaultInteriorEnclosureProbeDistance = 2.75f;
        private const int InteriorEnclosureMinHits = 4;

        private static readonly Vector3[] EnclosureProbeOffsets =
        {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right,
            new Vector3(0.707f, 0f, 0.707f),
            new Vector3(-0.707f, 0f, 0.707f),
            new Vector3(0.707f, 0f, -0.707f),
            new Vector3(-0.707f, 0f, -0.707f)
        };

        private DMCameraProfile _profile;

        private DMCameraProfile Profile
        {
            get
            {
                if (_profile == null)
                    _profile = DMCameraProfile.LoadOrNull();
                return _profile;
            }
        }

        private float SphereRadius => Profile != null ? Profile.sphereRadius : DefaultSphereRadius;
        private float ExtraSkin => Profile != null ? Profile.extraSkin : DefaultExtraSkin;
        private float MinFollow => Profile != null ? Profile.minFollow : DefaultMinFollow;
        private float ClimbMinFollow => Profile != null ? Profile.climbMinFollow : DefaultClimbMinFollow;
        private float MantleMinFollow => Profile != null ? Profile.mantleMinFollow : DefaultMantleMinFollow;
        private float PullSpeed => Profile != null ? Profile.pullSpeed : DefaultPullSpeed;
        private float ReleaseSpeed => Profile != null ? Profile.releaseSpeed : DefaultReleaseSpeed;
        private float CollisionHysteresis => Profile != null ? Profile.collisionHysteresis : DefaultCollisionHysteresis;
        private float ClimbPullSpeed => Profile != null ? Profile.climbPullSpeed : DefaultClimbPullSpeed;
        private float ClimbReleaseSpeed => Profile != null ? Profile.climbReleaseSpeed : DefaultClimbReleaseSpeed;
        private float MantlePullSpeed => Profile != null ? Profile.mantlePullSpeed : DefaultMantlePullSpeed;
        private float MantleReleaseSpeed => Profile != null ? Profile.mantleReleaseSpeed : DefaultMantleReleaseSpeed;
        private float ClimbNearRadius => Profile != null ? Profile.climbNearRadius : DefaultClimbNearRadius;
        private float FloorProbe => Profile != null ? Profile.floorProbe : DefaultFloorProbe;
        private float TightSpaceMinFollow => Profile != null ? Profile.tightSpaceMinFollow : DefaultTightSpaceMinFollow;
        private float PlayerMeshHideDistance =>
            Profile != null ? Profile.playerMeshHideDistance : DefaultPlayerMeshHideDistance;
        private float PlayerMeshShowHysteresis =>
            Profile != null ? Profile.playerMeshShowHysteresis : DefaultPlayerMeshShowHysteresis;
        private float TightSpaceReleaseSpeed =>
            Profile != null ? Profile.tightSpaceReleaseSpeed : DefaultTightSpaceReleaseSpeed;
        private float InteriorMaxFollow => Profile != null ? Profile.interiorMaxFollow : DefaultInteriorMaxFollow;
        private float InteriorFirstPersonDistance =>
            Profile != null ? Profile.interiorFirstPersonDistance : DefaultInteriorFirstPersonDistance;
        private float InteriorEnclosureProbeDistance =>
            Profile != null ? Profile.interiorEnclosureProbeDistance : DefaultInteriorEnclosureProbeDistance;
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
        private bool _nearClimbCached;
        private float _nearClimbNextCheck;
        private Vector3 _nearClimbSamplePivot;
        private bool _inTightSpace;
        private bool _pivotEnclosed;
        private bool _playerMeshHidden;
        private Renderer[] _playerRenderers;
        private float _lastComfortDist = -1f;
        private float _restoreComfortDist = -1f;
        private float _savedExteriorZoom = -1f;
        private bool _wasPivotEnclosedLastFrame;
        private bool _pivotInsideShell;
        private bool _treatClimbableAsWall;

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

        private void OnDisable()
        {
            SetPlayerMeshVisible(true);
        }

        private void LateUpdate()
        {
            if (!isActiveAndEnabled)
                return;

            CacheRefs();
            ApplyLensTuning();
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

            Vector3 pivot = ResolvePivot(pivotTf);
            bool mantling = _climb != null && _climb.IsMantling;
            bool climbing = _climb != null && _climb.IsClimbing;
            bool nearClimb = climbing || IsNearClimbableCached(pivot);
            _pivotInsideShell = !mantling && !nearClimb && IsPivotInsideEnvironmentShell(pivot, pivotTf);
            _pivotEnclosed = !mantling && !nearClimb
                && (_pivotInsideShell || IsPivotRayEnclosed(pivot, pivotTf));
            _treatClimbableAsWall = _pivotInsideShell || _pivotEnclosed;
            if (_pivotEnclosed && !_wasPivotEnclosedLastFrame)
                SaveExteriorZoomBeforeEnclosure();
            if (!_pivotEnclosed && _wasPivotEnclosedLastFrame)
                BeginExteriorZoomRestore();
            ClampScrollZoom(mantling, nearClimb, _pivotEnclosed);

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

            float minFollow = mantling ? MantleMinFollow : (nearClimb ? ClimbMinFollow : MinFollow);

            float wantDist;
            if (_pivotEnclosed)
                wantDist = Mathf.Min(desiredDist, InteriorMaxFollow);
            else
                wantDist = Mathf.Max(desiredDist, minFollow);

            float zoom = ResolveExteriorFollowZoom(minFollow);
            if (!_pivotEnclosed && zoom > minFollow)
                wantDist = Mathf.Max(wantDist, zoom);
            else if (_pivotEnclosed)
                wantDist = Mathf.Min(wantDist, InteriorMaxFollow);

            float closest = ClosestEnvHit(pivot, dir, wantDist, radius, pivotTf);
            float targetDist = wantDist;
            bool tightWall = false;
            _inTightSpace = false;
            if (closest < float.PositiveInfinity)
            {
                float allowed = closest - skin;
                bool bypassNormalMinCap = !mantling && !nearClimb && (_pivotEnclosed || allowed < minFollow);
                if (!bypassNormalMinCap && allowed >= minFollow)
                {
                    targetDist = allowed;
                }
                else if (!mantling && !nearClimb)
                {
                    _inTightSpace = true;
                    tightWall = true;
                    targetDist = Mathf.Max(TightSpaceMinFollow, allowed);
                }
                else
                {
                    targetDist = minFollow;
                    tightWall = true;
                }
            }

            if (_pivotEnclosed)
            {
                _inTightSpace = true;
                tightWall = true;
                float interiorMax = FindMaxInteriorCameraDistance(pivot, dir, wantDist, radius, pivotTf);
                targetDist = Mathf.Min(targetDist, interiorMax);
                if (targetDist <= InteriorFirstPersonDistance + 0.08f)
                {
                    targetDist = InteriorFirstPersonDistance;
                    SetPlayerMeshVisible(false);
                }
            }

            float activeMinFollow = ResolveActiveMinFollow(mantling, nearClimb, _inTightSpace, _pivotEnclosed);

            if (_smoothDist < 0f)
            {
                if (_pivotEnclosed || _inTightSpace)
                {
                    _smoothDist = Mathf.Clamp(
                        desiredDist,
                        TightSpaceMinFollow,
                        InteriorMaxFollow);
                }
                else
                {
                    _smoothDist = Mathf.Max(desiredDist, minFollow);
                }
            }

            float dt = Time.deltaTime;
            if (dt <= 0f)
                dt = 0.02f;

            // Ignore sphere-cast flicker inside the hysteresis band (AAA camera collision).
            if (!_pivotEnclosed
                && !mantling && !nearClimb
                && targetDist < _smoothDist - 0.01f
                && targetDist > _smoothDist - CollisionHysteresis)
            {
                targetDist = _smoothDist;
            }

            // Climb/mantle: soft pull only. Normal: finite pull-in, no per-frame snap
            // (the old targetDist+0.02 clamp made walls and pickup stems jitter).
            float pull = mantling ? MantlePullSpeed : (nearClimb ? ClimbPullSpeed : PullSpeed);
            float release = mantling ? MantleReleaseSpeed : (nearClimb ? ClimbReleaseSpeed : ReleaseSpeed);
            if (targetDist < _smoothDist - 0.01f)
            {
                if (mantling || nearClimb)
                {
                    float chase = mantling ? 0.12f : 0.22f;
                    float maxStep = Mathf.Max(0.02f, (_smoothDist - targetDist) * chase * Mathf.Max(dt, 0.01f) * 60f);
                    _smoothDist = Mathf.Max(targetDist, _smoothDist - maxStep);
                }
                else
                {
                    _smoothDist = Mathf.MoveTowards(_smoothDist, targetDist, pull * dt);
                }
            }
            else
            {
                _smoothDist = Mathf.Lerp(_smoothDist, targetDist, 1f - Mathf.Exp(-release * dt));
            }
            if (_smoothDist < activeMinFollow)
                _smoothDist = activeMinFollow;

            if (!_inTightSpace && !_playerMeshHidden && _smoothDist >= minFollow - 0.05f)
                _lastComfortDist = _smoothDist;
            else if (_inTightSpace && _restoreComfortDist < 0f && _lastComfortDist >= minFollow)
                _restoreComfortDist = _lastComfortDist;

            if (!_pivotEnclosed
                && !_inTightSpace
                && _restoreComfortDist > 0f
                && _smoothDist < _restoreComfortDist - 0.05f)
            {
                _smoothDist = Mathf.MoveTowards(_smoothDist, _restoreComfortDist, TightSpaceReleaseSpeed * dt);
            }

            if (!_inTightSpace && !_pivotEnclosed)
                TryFinishExteriorZoomRestore();

            _wasPivotEnclosedLastFrame = _pivotEnclosed;

            UpdatePlayerMeshVisibility(activeMinFollow, _pivotEnclosed);

            Vector3 pos = pivot + dir * _smoothDist;
            pos = KeepAboveFloor(pos, radius, pivotTf);
            pos = KeepAboveTerrainSurface(pos, radius);
            if (mantling)
            {
                // During mantle ignore aggressive depenetrate that yanks into wall/player.
                pos = EnforceMinFollow(pivot, pos, dir, activeMinFollow);
            }
            else if (tightWall)
                pos = SlideClearOfEnvironment(pivot, pos, dir, radius, pivotTf, activeMinFollow);
            else
            {
                pos = PullOutOfEnvironment(pivot, pos, radius, pivotTf, activeMinFollow);
                if (!nearClimb)
                    pos = DepenetrateFromEnvironment(pos, radius, pivotTf);
                else
                    pos = EnforceMinFollow(pivot, pos, dir, activeMinFollow);
            }

            if (_pivotInsideShell || _pivotEnclosed || _inTightSpace)
                pos = ClampCameraAlongPivotRay(pivot, pos, radius, pivotTf, activeMinFollow);
            pos = EnforceMinFollow(pivot, pos, dir, activeMinFollow);
            pos = KeepAboveTerrainSurface(pos, radius);

            Vector3 placed = pos - pivot;
            float placedDist = placed.magnitude;
            float zoomNow = tpCamera.CurrentZoom > 0.01f ? tpCamera.CurrentZoom : tpCamera.distance;
            bool pulledBelowZoom = placedDist < zoomNow - 0.08f;
            bool needsInvectorSync = _pivotEnclosed
                || _inTightSpace
                || pulledBelowZoom
                || _restoreComfortDist > 0f;

            if (!needsInvectorSync)
            {
                _smoothDist = zoomNow;
                _wasPivotEnclosedLastFrame = _pivotEnclosed;
                if (!_pivotEnclosed && !_inTightSpace)
                    TryFinishExteriorZoomRestore();
                return;
            }

            transform.position = pos;
            if (_body != null)
                _body.position = pos;

            if (placedDist > 0.001f)
                _smoothDist = Mathf.Max(placedDist, activeMinFollow);

            tpCamera.distance = _smoothDist;
            if (pulledBelowZoom)
                SetCullingDistance(_smoothDist);
        }

        private float ResolveActiveMinFollow(bool mantling, bool nearClimb, bool tightSpace, bool pivotEnclosed)
        {
            if (mantling)
                return MantleMinFollow;
            if (nearClimb)
                return ClimbMinFollow;
            if (pivotEnclosed || tightSpace)
                return TightSpaceMinFollow;
            return MinFollow;
        }

        private void UpdatePlayerMeshVisibility(float activeMinFollow, bool pivotEnclosed)
        {
            if (_playerRenderers == null || _playerRenderers.Length == 0)
                return;

            float hideDist = pivotEnclosed
                ? Mathf.Max(PlayerMeshHideDistance, InteriorFirstPersonDistance + 0.05f)
                : PlayerMeshHideDistance;
            if (!_playerMeshHidden && (_smoothDist < hideDist || pivotEnclosed && _smoothDist <= InteriorFirstPersonDistance + 0.1f))
            {
                if (_restoreComfortDist < 0f)
                {
                    _restoreComfortDist = _lastComfortDist >= MinFollow
                        ? _lastComfortDist
                        : MinFollow;
                }

                SetPlayerMeshVisible(false);
                return;
            }

            if (_playerMeshHidden
                && _smoothDist > hideDist + PlayerMeshShowHysteresis
                && _smoothDist >= activeMinFollow + 0.02f)
            {
                SetPlayerMeshVisible(true);
            }
        }

        private void SetPlayerMeshVisible(bool visible)
        {
            if (_playerRenderers == null)
                return;

            if (_playerMeshHidden == !visible)
                return;

            _playerMeshHidden = !visible;
            for (int i = 0; i < _playerRenderers.Length; i++)
            {
                Renderer renderer = _playerRenderers[i];
                if (renderer != null)
                    renderer.enabled = visible;
            }
        }

        private void CachePlayerRenderers()
        {
            if (playerRoot == null)
                return;

            if (_playerRenderers != null && _playerRenderers.Length > 0)
                return;

            Renderer[] all = playerRoot.GetComponentsInChildren<Renderer>(true);
            if (all == null || all.Length == 0)
                return;

            int count = 0;
            for (int i = 0; i < all.Length; i++)
            {
                Renderer r = all[i];
                if (r == null)
                    continue;
                if (r.transform.IsChildOf(transform))
                    continue;
                count++;
            }

            if (count == 0)
                return;

            _playerRenderers = new Renderer[count];
            int write = 0;
            for (int i = 0; i < all.Length; i++)
            {
                Renderer r = all[i];
                if (r == null || r.transform.IsChildOf(transform))
                    continue;
                _playerRenderers[write++] = r;
            }
        }

        private void ClampScrollZoom(bool mantling, bool nearClimb, bool pivotEnclosed)
        {
            if (tpCamera == null || tpCamera.currentState == null)
                return;
            if (IsAimOrScopeState(tpCamera.currentStateName) ||
                IsAimOrScopeState(tpCamera.currentState.Name))
                return;

            if (pivotEnclosed)
            {
                if (tpCamera.CurrentZoom > InteriorMaxFollow + 0.02f)
                    tpCamera.SetZoomTarget(InteriorMaxFollow);
                if (tpCamera.CurrentZoom < TightSpaceMinFollow)
                    tpCamera.SetZoomTarget(TightSpaceMinFollow);
                return;
            }

            if (_restoreComfortDist >= MinFollow - 0.05f)
            {
                float live = tpCamera.CurrentZoom > 0.01f ? tpCamera.CurrentZoom : tpCamera.distance;
                if (live < _restoreComfortDist - 0.08f)
                    tpCamera.SetZoomTarget(_restoreComfortDist);
                return;
            }

            float floor = mantling ? MantleMinFollow : (nearClimb ? ClimbMinFollow : MinFollow);
            if (tpCamera.CurrentZoom < 1f)
                tpCamera.SetZoomTarget(floor);
        }

        private void SaveExteriorZoomBeforeEnclosure()
        {
            if (tpCamera == null)
                return;

            float zoom = tpCamera.CurrentZoom > 0.01f ? tpCamera.CurrentZoom : tpCamera.distance;
            if (zoom >= MinFollow - 0.05f)
                _savedExteriorZoom = zoom;
            else if (_lastComfortDist >= MinFollow - 0.05f)
                _savedExteriorZoom = _lastComfortDist;
        }

        private void BeginExteriorZoomRestore()
        {
            if (tpCamera == null)
                return;

            float restore = _savedExteriorZoom;
            if (restore < MinFollow - 0.05f)
            {
                if (_lastComfortDist >= MinFollow - 0.05f)
                    restore = _lastComfortDist;
                else
                    restore = MinFollow;
            }

            _restoreComfortDist = restore;
            tpCamera.SetZoomTarget(restore);
        }

        private float ResolveExteriorFollowZoom(float minFollow)
        {
            if (tpCamera == null)
                return minFollow;

            float zoom = tpCamera.CurrentZoom > 0.01f ? tpCamera.CurrentZoom : tpCamera.distance;
            if (!_pivotEnclosed && _restoreComfortDist >= minFollow - 0.05f)
                return Mathf.Max(zoom, _restoreComfortDist);
            return zoom;
        }

        private void TryFinishExteriorZoomRestore()
        {
            if (_restoreComfortDist < MinFollow - 0.05f)
                return;

            bool distOk = _smoothDist >= _restoreComfortDist - 0.1f;
            float live = tpCamera != null
                ? (tpCamera.CurrentZoom > 0.01f ? tpCamera.CurrentZoom : tpCamera.distance)
                : 0f;
            bool zoomOk = live >= _restoreComfortDist - 0.12f;
            if (!distOk || !zoomOk)
                return;

            _restoreComfortDist = -1f;
            _savedExteriorZoom = -1f;
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
            float minFollow = DefaultMinFollow)
        {
            pos = DepenetrateFromEnvironment(pos, radius, pivotTf);
            pos = EnforceMinFollow(pivot, pos, fallbackDir, minFollow);
            pos = DepenetrateFromEnvironment(pos, radius, pivotTf);
            return EnforceMinFollow(pivot, pos, fallbackDir, minFollow);
        }

        private Vector3 EnforceMinFollow(Vector3 pivot, Vector3 pos, Vector3 fallbackDir, float minFollow = DefaultMinFollow)
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
            if (layer == PlayerLayer)
                return false;
            if (layer == ClimbableLayer && !_treatClimbableAsWall)
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

        private void ApplyLensTuning()
        {
            DMCameraProfile profile = Profile;
            if (profile == null || eye == null)
                return;

            float near = Mathf.Clamp(profile.nearClipPlane, 0.01f, 1f);
            if (!Mathf.Approximately(eye.nearClipPlane, near))
                eye.nearClipPlane = near;
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

            CachePlayerRenderers();
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

        private bool IsPivotRayEnclosed(Vector3 pivot, Transform pivotTf)
        {
            int hits = 0;
            float probe = InteriorEnclosureProbeDistance;
            for (int i = 0; i < EnclosureProbeOffsets.Length; i++)
            {
                Vector3 dir = EnclosureProbeOffsets[i];
                if (Physics.Raycast(
                        pivot,
                        dir,
                        out RaycastHit hit,
                        probe,
                        collisionMask,
                        QueryTriggerInteraction.Ignore)
                    && IsEnvironmentHit(hit, pivotTf))
                {
                    hits++;
                }
            }

            return hits >= InteriorEnclosureMinHits;
        }

        /// <summary>
        /// Forward rays from a pivot already inside geometry miss that shell (Unity raycast rule).
        /// Overlap + ClosestPoint detects box/convex rooms without requiring static colliders.
        /// </summary>
        private bool IsPivotInsideEnvironmentShell(Vector3 pivot, Transform pivotTf)
        {
            int n = Physics.OverlapSphereNonAlloc(
                pivot,
                0.3f,
                Overlaps,
                collisionMask,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = Overlaps[i];
                if (!IsShellVolumeCollider(c, pivotTf))
                    continue;
                if (ColliderContainsPoint(c, pivot))
                    return true;
            }

            return false;
        }

        /// <summary>Detect tent / room volumes; climbable layer counts here only.</summary>
        private bool IsShellVolumeCollider(Collider c, Transform pivotTf)
        {
            if (c == null || c.isTrigger)
                return false;

            Transform tr = c.transform;
            if (tr == transform || tr.IsChildOf(transform))
                return false;

            Transform root = pivotTf != null ? pivotTf.root : null;
            if (root != null && (tr == root || tr.IsChildOf(root)))
                return false;

            if (c.gameObject.layer == PlayerLayer)
                return false;

            return c is BoxCollider || c is SphereCollider || c is CapsuleCollider;
        }

        private static bool ColliderContainsPoint(Collider c, Vector3 point)
        {
            if (c == null)
                return false;

            Vector3 closest = c.ClosestPoint(point);
            return (closest - point).sqrMagnitude < 1e-4f;
        }

        private Vector3 ClampCameraAlongPivotRay(
            Vector3 pivot,
            Vector3 pos,
            float radius,
            Transform pivotTf,
            float minFollow)
        {
            Vector3 offset = pos - pivot;
            float dist = offset.magnitude;
            if (dist < 0.001f)
                return pos;

            Vector3 dir = offset / dist;
            float skin = ExtraSkin + (eye != null ? eye.nearClipPlane : 0f);
            float wallDist = ClosestEnvHit(pivot, dir, dist + radius + skin, radius, pivotTf);
            if (wallDist >= float.PositiveInfinity)
                return pos;

            float allowed = wallDist - skin;
            allowed = Mathf.Max(minFollow, allowed);
            if (dist <= allowed + 0.02f)
                return pos;

            return pivot + dir * allowed;
        }

        private float FindMaxInteriorCameraDistance(
            Vector3 pivot,
            Vector3 dir,
            float maxSearch,
            float radius,
            Transform pivotTf)
        {
            float lo = TightSpaceMinFollow;
            float hi = Mathf.Max(lo + 0.05f, maxSearch);
            for (int i = 0; i < 12; i++)
            {
                float mid = (lo + hi) * 0.5f;
                if (IsValidInteriorCameraDistance(pivot, dir, mid, radius, pivotTf))
                    lo = mid;
                else
                    hi = mid;
            }

            return lo;
        }

        private bool IsValidInteriorCameraDistance(
            Vector3 pivot,
            Vector3 dir,
            float dist,
            float radius,
            Transform pivotTf)
        {
            if (dist < TightSpaceMinFollow - 0.001f)
                return false;

            Vector3 cam = pivot + dir * dist;
            if (OverlapsEnvironment(cam, radius, pivotTf))
                return false;

            if (Physics.Linecast(
                    pivot,
                    cam,
                    out RaycastHit lineHit,
                    collisionMask,
                    QueryTriggerInteraction.Ignore)
                && IsEnvironmentHit(lineHit, pivotTf)
                && lineHit.distance < dist - 0.06f)
            {
                return false;
            }

            float wall = ClosestEnvHit(pivot, dir, dist + 0.05f, radius, pivotTf);
            return wall >= dist - 0.08f;
        }

        private float ClosestEnvHit(Vector3 pivot, Vector3 dir, float desiredDist, float radius, Transform pivotTf)
        {
            float closest = float.PositiveInfinity;

            if (Physics.Linecast(
                    pivot,
                    pivot + dir * desiredDist,
                    out RaycastHit line,
                    collisionMask,
                    QueryTriggerInteraction.Ignore)
                && IsEnvironmentHit(line, pivotTf)
                && line.distance < closest)
            {
                closest = line.distance;
            }

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

            if (_pivotInsideShell || _pivotEnclosed)
            {
                float reverse = ClosestShellHitFromExterior(pivot, dir, desiredDist, radius, pivotTf);
                if (reverse < closest)
                    closest = reverse;
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

        /// <summary>
        /// When the pivot sits inside a shell, cast from the far end back toward the pivot to find the inner wall.
        /// </summary>
        private float ClosestShellHitFromExterior(
            Vector3 pivot,
            Vector3 dir,
            float searchDist,
            float radius,
            Transform pivotTf)
        {
            if (searchDist < 0.05f)
                return float.PositiveInfinity;

            float closest = float.PositiveInfinity;
            float skin = ExtraSkin + (eye != null ? eye.nearClipPlane : 0f);
            float[] probeDepths =
            {
                searchDist,
                searchDist + radius,
                searchDist + skin + radius
            };

            for (int p = 0; p < probeDepths.Length; p++)
            {
                float depth = probeDepths[p];
                Vector3 probeOrigin = pivot + dir * depth;
                float castLen = depth + skin + radius;
                if (!Physics.Raycast(
                        probeOrigin,
                        -dir,
                        out RaycastHit back,
                        castLen,
                        collisionMask,
                        QueryTriggerInteraction.Ignore)
                    || !IsEnvironmentHit(back, pivotTf))
                {
                    continue;
                }

                float fromPivot = depth - back.distance;
                if (fromPivot > 0.02f && fromPivot < closest)
                    closest = fromPivot;
            }

            return closest;
        }

        private Vector3 PullOutOfEnvironment(Vector3 pivot, Vector3 pos, float radius, Transform pivotTf, float minFollow = DefaultMinFollow)
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
                if (layer == PlayerLayer)
                    continue;
                if (layer == ClimbableLayer && !_treatClimbableAsWall)
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
            if (layer == PlayerLayer)
                return false;
            if (layer == ClimbableLayer && !_treatClimbableAsWall)
                return false;
            if (hit.collider is TerrainCollider)
                return true;

            return true;
        }

        private bool IsNearClimbableCached(Vector3 pivot)
        {
            if (_climb != null && _climb.IsClimbing)
                return true;

            float now = Time.time;
            if (now < _nearClimbNextCheck && (pivot - _nearClimbSamplePivot).sqrMagnitude < 0.36f)
                return _nearClimbCached;

            _nearClimbNextCheck = now + 0.15f;
            _nearClimbSamplePivot = pivot;
            _nearClimbCached = NearClimbableGeometry(pivot, ClimbNearRadius);
            return _nearClimbCached;
        }

        private static bool NearClimbableGeometry(Vector3 pivot, float nearRadius)
        {
            int mask = 1 << ClimbableLayer;
            if (Physics.CheckSphere(pivot, nearRadius, mask, QueryTriggerInteraction.Ignore))
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
