using Project.Building;
using UnityEngine;

namespace Project.AI.Validation
{
    /// <summary>
    /// NavMesh-free movement validation: one enemy patrols a ring plus props discovered by capsule/ray probes.
    /// How to run (Play mode):
    /// 1. Open Dark Matter Genesis v1.6.6 (or current Genesis scene).
    /// 2. Select an enemy with <see cref="EnemyAiController"/> in the hierarchy (or use menu below).
    /// 3. Menu: Tools → Dark Matter Genesis → Combat → Enemy Wall Walk Validation (Play) — manual only; never auto-starts.
    /// 4. Watch Console for [EnemyWalkValidation] samples (lateral flips, wall-clip suspects). Gizmos show the route while active.
    /// 5. Stop Play or use Context Menu → Stop Validation Patrol to restore wander/patrol.
    /// Does not run the full DMEnemyRuntimeAudit suite.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMEnemyCollisionWalkValidation : MonoBehaviour
    {
        [SerializeField] private Transform orbitCenter;
        [SerializeField] private float orbitRadius = 14f;
        [SerializeField] private int ringPointCount = 10;
        [SerializeField] private int staticProbeRays = 20;
        [SerializeField] private float staticProbeRadius = 22f;
        [SerializeField] private float propStandoff = 1.1f;
        [SerializeField] private float sampleInterval = 0.5f;
        [SerializeField] private bool drawGizmos = true;

        private EnemyAiController ai;
        private Vector3[] routePoints;
        private Vector3 lastSamplePosition;
        private Vector3 lastVelocity;
        private bool hasLastSample;
        private float nextSampleTime;
        private int lateralFlipCount;
        private int wallClipSuspectCount;
        private int sampleCount;
        private bool validationPatrolActive;

        public int RouteWaypointCount => routePoints != null ? routePoints.Length : 0;
        public bool IsValidationPatrolActive => validationPatrolActive;

        /// <summary>Play menu / explicit test only — does not change brain profile or other enemies.</summary>
        public void EnablePatrolDriveForManualRun() { }

        private void Awake()
        {
            ai = GetComponent<EnemyAiController>();
        }

        [ContextMenu("Start Validation Patrol")]
        public void StartValidationPatrol()
        {
            EnablePatrolDriveForManualRun();

            if (ai == null)
                ai = GetComponent<EnemyAiController>();
            if (ai == null)
            {
                Debug.LogWarning("[EnemyWalkValidation] Missing EnemyAiController on " + name, this);
                return;
            }

            routePoints = BuildRoutePoints();
            if (routePoints == null || routePoints.Length < 2)
            {
                Debug.LogWarning("[EnemyWalkValidation] Could not build route (need center + hits).", this);
                return;
            }

            ai.BeginValidationPatrolRoute(routePoints, EnemyPatrolMode.Loop);
            validationPatrolActive = true;
            hasLastSample = false;
            lateralFlipCount = 0;
            wallClipSuspectCount = 0;
            sampleCount = 0;
            nextSampleTime = Time.time + sampleInterval;
            Debug.Log(
                $"[EnemyWalkValidation] Started patrol with {routePoints.Length} waypoints on '{name}'. "
                + "Stop Play or Context Menu → Stop Validation Patrol to restore wander.",
                this);
        }

        [ContextMenu("Stop Validation Patrol")]
        public void StopValidationPatrol()
        {
            if (!validationPatrolActive)
                return;

            validationPatrolActive = false;
            routePoints = null;
            if (ai != null)
                ai.EndValidationPatrolRoute();
            Debug.Log("[EnemyWalkValidation] Stopped — restored prior movement mode / patrol.", this);
        }

        private void Update()
        {
            if (!validationPatrolActive || routePoints == null || ai == null || sampleInterval <= 0.05f)
                return;

            if (Time.time < nextSampleTime)
                return;

            nextSampleTime = Time.time + sampleInterval;
            SampleMovement();
        }

        private void OnDestroy()
        {
            if (validationPatrolActive)
                StopValidationPatrol();
        }

        private void OnDisable()
        {
            if (validationPatrolActive)
                StopValidationPatrol();

            if (sampleCount > 0)
            {
                Debug.Log(
                    $"[EnemyWalkValidation] Summary '{name}': samples={sampleCount}, lateralFlips={lateralFlipCount}, wallClipSuspects={wallClipSuspectCount}",
                    this);
            }
        }

        private void SampleMovement()
        {
            Vector3 pos = transform.position;
            sampleCount++;
            if (!hasLastSample)
            {
                lastSamplePosition = pos;
                hasLastSample = true;
                return;
            }

            Vector3 delta = pos - lastSamplePosition;
            delta.y = 0f;
            float dist = delta.magnitude;
            if (dist > 0.02f)
            {
                Vector3 vel = delta / Mathf.Max(sampleInterval, 0.001f);
                if (lastVelocity.sqrMagnitude > 0.01f && vel.sqrMagnitude > 0.01f)
                {
                    float dot = Vector3.Dot(lastVelocity.normalized, vel.normalized);
                    if (dot < -0.35f)
                        lateralFlipCount++;
                }

                lastVelocity = vel;
                if (TryDetectWallClip(lastSamplePosition, delta))
                    wallClipSuspectCount++;
            }

            lastSamplePosition = pos;
            if (sampleCount % 4 == 0)
            {
                Debug.Log(
                    $"[EnemyWalkValidation] '{name}' flips={lateralFlipCount} clipSuspects={wallClipSuspectCount} pos={pos}",
                    this);
            }
        }

        private bool TryDetectWallClip(Vector3 from, Vector3 delta)
        {
            if (delta.sqrMagnitude < 0.0004f)
                return false;

            ResolveCapsule(out float radius, out Vector3 bottom, out Vector3 top);
            int mask = EnemyChaseWallSlide.ChaseObstructionMask;
            Vector3 dir = delta.normalized;
            float distance = delta.magnitude;
            RaycastHit[] hits = new RaycastHit[4];
            int count = Physics.CapsuleCastNonAlloc(
                bottom,
                top,
                radius,
                dir,
                hits,
                distance,
                mask,
                QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider c = hits[i].collider;
                if (c == null || c.transform == transform || c.transform.IsChildOf(transform))
                    continue;
                if (hits[i].distance < nearest)
                    nearest = hits[i].distance;
            }

            if (nearest >= float.MaxValue - 1f)
                return false;

            return nearest < distance * 0.35f;
        }

        private void ResolveCapsule(out float radius, out Vector3 bottom, out Vector3 top)
        {
            DM_EnemyBrainProfile profile = DM_EnemyBrainProfile.Live;
            radius = profile != null ? profile.chaseWallSlideRadius : 0.35f;
            float height = profile != null ? profile.chaseWallSlideHeight : 1.75f;
            CapsuleCollider cap = GetComponentInChildren<CapsuleCollider>();
            if (cap != null && !cap.isTrigger)
            {
                Vector3 scale = cap.transform.lossyScale;
                radius = cap.radius * Mathf.Max(scale.x, scale.z);
                height = cap.height * scale.y;
                Vector3 worldCenter = cap.transform.TransformPoint(cap.center);
                float halfCylinder = Mathf.Max(0f, height * 0.5f - radius);
                bottom = worldCenter - Vector3.up * halfCylinder;
                top = worldCenter + Vector3.up * halfCylinder;
                return;
            }

            Vector3 pos = transform.position;
            float cyl = Mathf.Max(0f, height - radius * 2f);
            bottom = pos + Vector3.up * radius;
            top = bottom + Vector3.up * cyl;
        }

        private Vector3[] BuildRoutePoints()
        {
            Vector3 center = orbitCenter != null ? orbitCenter.position : transform.position;
            if (orbitCenter == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    center = player.transform.position;
            }

            center.y = SampleGroundY(center, center.y);
            int mask = EnemyChaseWallSlide.ChaseObstructionMask;
            var list = new System.Collections.Generic.List<Vector3>(ringPointCount + staticProbeRays);

            for (int i = 0; i < ringPointCount; i++)
            {
                float angle = i / (float)ringPointCount * 360f;
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * orbitRadius;
                Vector3 p = center + offset;
                p.y = SampleGroundY(p, center.y);
                list.Add(p);
            }

            for (int i = 0; i < staticProbeRays; i++)
            {
                float angle = i / (float)staticProbeRays * 360f;
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                if (Physics.Raycast(center + Vector3.up * 0.5f, dir, out RaycastHit hit, staticProbeRadius, mask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider != null && !hit.collider.isTrigger)
                    {
                        Vector3 n = hit.normal;
                        n.y = 0f;
                        if (n.sqrMagnitude > 0.0001f)
                        {
                            Vector3 p = hit.point + n.normalized * propStandoff;
                            p.y = SampleGroundY(p, hit.point.y);
                            list.Add(p);
                        }
                    }
                }
            }

            return list.Count >= 2 ? list.ToArray() : null;
        }

        private static float SampleGroundY(Vector3 world, float fallback)
        {
            if (Physics.Raycast(world + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 12f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return hit.point.y;
            return fallback;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos || !validationPatrolActive || routePoints == null || routePoints.Length == 0)
                return;

            Gizmos.color = new Color(0.2f, 0.85f, 0.95f, 0.85f);
            for (int i = 0; i < routePoints.Length; i++)
            {
                Gizmos.DrawSphere(routePoints[i] + Vector3.up * 0.15f, 0.25f);
                Vector3 next = routePoints[(i + 1) % routePoints.Length];
                Gizmos.DrawLine(routePoints[i] + Vector3.up * 0.15f, next + Vector3.up * 0.15f);
            }
        }
    }
}
