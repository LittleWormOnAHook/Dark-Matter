#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using Project.AI;
using Project.Data;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Project.EditorTools.Audit
{
    public static partial class DMEnemyRuntimeAudit
    {
        private static GameObject _obstacleAuditRoot;
        private static Vector3 ObstacleAuditCenter()
        {
            // Keep isolated box cases out of the authored obstacle course. Floor is
            // below this volume; any elevated collider rejects a candidate arena.
            for (int i = 0; i < 24; i++)
            {
                Vector3 candidate = Ground(_origin + new Vector3(30 + (i % 6) * 20, 0, 30 + (i / 6) * 20));
                if (!Physics.CheckBox(candidate + Vector3.up * 1.6f, new Vector3(9, 1.2f, 9),
                        Quaternion.identity, EnemyChaseWallSlide.ChaseObstructionMask, QueryTriggerInteraction.Ignore))
                    return candidate;
            }
            throw new InvalidOperationException("No clear 18m obstacle-audit arena found; refusing contaminated fixture results.");
        }
        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            Vector3 delta = a - b; delta.y = 0f; return delta.magnitude;
        }
        private static readonly System.Reflection.FieldInfo WallContactField =
            typeof(EnemyAiController).GetField("RecentWallBufferContact", BF);
        private static readonly System.Reflection.FieldInfo WallSideField =
            typeof(EnemyAiController).GetField("ChaseWallFollowSideSign", BF);

        [MenuItem("Tools/Dark Matter Genesis/Combat/Enemy Obstacle Jitter Audit (Play)")]
        private static void LaunchObstacleAudit()
        {
            Launch("{\"filter\":\"HumanoidEnemy_Invector\",\"obstaclesOnly\":true,\"weaponMatrix\":false,\"scenarios\":false,\"mode\":\"full\",\"force\":true}");
        }

        private static void CleanupObstacleAudit()
        {
            if (_obstacleAuditRoot != null) Object.Destroy(_obstacleAuditRoot);
            _obstacleAuditRoot = null;
        }

        // Runtime geometry only; the existing audit owns boot bypass, player guard,
        // enemy spawn/top-up, report writing, and exiting Play without saving the scene.
        private static BoxCollider ObstacleBox(Vector3 center)
        {
            _obstacleAuditRoot = new GameObject("AUDIT_ObstacleFixtures");
            var box = new GameObject("AUDIT_BoxCollider");
            box.transform.SetParent(_obstacleAuditRoot.transform);
            box.transform.position = center + Vector3.up * 1.5f;
            int mask = EnemyChaseWallSlide.ChaseObstructionMask;
            int layer = 0;
            while (layer < 31 && (mask & (1 << layer)) == 0) layer++;
            box.layer = layer;
            var collider = box.AddComponent<BoxCollider>();
            collider.size = new Vector3(4f, 3f, 4f);
            Physics.SyncTransforms();
            return collider;
        }

        private static void ObstaclePlace(Live L, Vector3 position, Vector3 destination)
        {
            position = Ground(position);
            L.go.transform.position = position;
            Vector3 forward = destination - position; forward.y = 0f;
            if (forward.sqrMagnitude > 0.001f) L.go.transform.rotation = Quaternion.LookRotation(forward);
            var body = L.go.GetComponent<Rigidbody>();
            if (body != null) { body.position = position; body.rotation = L.go.transform.rotation; }
            Physics.SyncTransforms();
        }

        private sealed class ObstacleMeasure
        {
            public int samples, reversals, yawReversals, sideFlips, penetrations;
            public string stallTrace = "";
            public string yawTrace = "";
            public float path, maxStall, stall, stationaryAnimation, maxStationaryAnimation, maxTerritoryExcess;
            private Vector3 previous, lastMove;
            private float lastTime, previousYaw, lastYawDelta;
            private int previousSide;
            public void Begin(Live L)
            {
                previous = L.go.transform.position; previousYaw = L.go.transform.eulerAngles.y;
                lastTime = Time.time; lastMove = Vector3.zero;
            }
            public void Sample(Live L, BoxCollider box, bool expectsProgress, Vector3 home, float radius)
            {
                float dt = Time.time - lastTime;
                if (dt < 0.1f || L.go == null) return;
                lastTime = Time.time;
                Vector3 position = L.go.transform.position;
                Vector3 delta = position - previous; delta.y = 0f;
                bool contact = WallContactField != null && (bool)WallContactField.GetValue(L.ai);
                int side = WallSideField != null ? (int)WallSideField.GetValue(L.ai) : 0;
                if (contact && side != 0 && previousSide != 0 && side != previousSide) sideFlips++;
                if (contact && delta.magnitude > 0.005f && lastMove.magnitude > 0.005f &&
                    Vector3.Dot(delta.normalized, lastMove.normalized) < -0.5f) reversals++;
                float yawDelta = Mathf.DeltaAngle(previousYaw, L.go.transform.eulerAngles.y);
                if (contact && Mathf.Abs(yawDelta) > 3f && Mathf.Abs(lastYawDelta) > 3f &&
                    Mathf.Sign(yawDelta) != Mathf.Sign(lastYawDelta))
                {
                    yawReversals++;
                    if (yawReversals <= 12)
                        yawTrace += $"t={Time.time:F2} yawDelta={yawDelta:F1} move={delta:F3} pos={position:F2}; ";
                }
                if (Mathf.Abs(yawDelta) > 3f) lastYawDelta = yawDelta;
                if (delta.magnitude > 0.005f) lastMove = delta;
                if (side != 0) previousSide = side;
                float speed = delta.magnitude / dt;
                stall = expectsProgress && speed < 0.05f ? stall + dt : 0f;
                maxStall = Mathf.Max(maxStall, stall);
                if (stall > 1f && stallTrace.Length == 0)
                {
                    var fields = new[] { "moveTarget", "auditDesiredStep", "auditResolvedStep", "auditRootAfterStep", "auditStepFrame",
                        "chaseWallLastContactNormal", "chaseWallBlockedFrames", "ChaseWallFollowFailStreak" };
                    stallTrace = $"state={AiState(L.ai)} position={position:F3} frame={Time.frameCount}";
                    foreach (string field in fields)
                        stallTrace += $"; {field}={typeof(EnemyAiController).GetField(field, BF)?.GetValue(L.ai)}";
                    var nearby = Physics.OverlapSphere(position + Vector3.up, 3f, EnemyChaseWallSlide.ChaseObstructionMask, QueryTriggerInteraction.Ignore);
                    foreach (var collider in nearby)
                        if (!collider.transform.IsChildOf(L.go.transform))
                            stallTrace += $"; collider={collider.name} bounds={collider.bounds}";
                }
                float magnitude = L.anim != null ? L.anim.GetFloat("InputMagnitude") : 0f;
                stationaryAnimation = expectsProgress && speed < 0.05f && magnitude > 0.2f
                    ? stationaryAnimation + dt : 0f;
                maxStationaryAnimation = Mathf.Max(maxStationaryAnimation, stationaryAnimation);
                if (radius > 0f)
                    maxTerritoryExcess = Mathf.Max(maxTerritoryExcess, HorizontalDistance(position, home) - radius);
                if (box != null)
                {
                    var cap = L.go.GetComponent<CapsuleCollider>();
                    if (cap != null && Physics.ComputePenetration(cap, cap.transform.position, cap.transform.rotation,
                        box, box.transform.position, box.transform.rotation, out Vector3 n, out float depth) &&
                        Mathf.Abs(n.y) < 0.65f && depth > 0.03f) penetrations++;
                }
                path += delta.magnitude; previous = position;
                previousYaw = L.go.transform.eulerAngles.y; samples++;
            }
            public string Describe() =>
                $"samples={samples}, path={path:F2}m, movementReversals={reversals}, yawReversals={yawReversals}, " +
                $"sideFlips={sideFlips}, maxStall={maxStall:F2}s, stationaryWalk={maxStationaryAnimation:F2}s, " +
                $"penetrationSamples={penetrations}, territoryExcess={maxTerritoryExcess:F2}m; firstStall=[{stallTrace}]; yawTrace=[{yawTrace}]";
        }

        private static IEnumerator ScnObstacles(GameObject prefab, DMAuditEnemyResult R)
        {
            // Each face from both sides exercises both winding choices at box corners.
            for (int face = 0; face < 5; face++)
            for (int side = -1; side <= 1; side += 2)
            {
                string id = $"wall.face{face}.side{side}";
                SetStatus("obstacle audit: " + id);
                Live L = SpawnMatrixEnemy(prefab, id);
                yield return SpawnReady(L);
                Vector3 center = face < 4 ? ObstacleAuditCenter() : Ground(_origin + new Vector3(0, 0, 22));
                Quaternion rotation = Quaternion.Euler(0, face * 90, 0);
                Vector3 start = center + rotation * new Vector3(4.5f, 0, 0);
                Vector3 destination = center + rotation * new Vector3(-5, 0, side * 0.8f);
                ObstaclePlace(L, start, destination);
                BoxCollider box = ObstacleBox(center);
                PlacePlayer(Ground(center + Vector3.right * 70), center);
                L.ai.BeginPatrolRouteNow(new[] { destination, start }, EnemyPatrolMode.PingPong);
                var measure = new ObstacleMeasure(); measure.Begin(L);
                float until = Time.time + 18f;
                bool arrived = false;
                while (Time.time < until)
                {
                    measure.Sample(L, box, true, center, 0);
                    if (HorizontalDistance(L.go.transform.position, destination) < 0.9f) { arrived = true; break; }
                    yield return null;
                }
                bool pass = arrived && measure.reversals <= 2 && measure.yawReversals <= 3 &&
                    measure.maxStall < 2.5f && measure.penetrations == 0 && measure.maxStationaryAnimation < 1f;
                R.Add("Obstacle", id, "Box approach and corner escape " + id, pass ? "PASS" : "FAIL",
                    $"arrived={arrived}; " + measure.Describe(),
                    pass ? "" : "Inspect measured reversal, stall, facing, overlap and animation counts; actual Play Mode patrol, no injected root movement.");
                Cleanup(); yield return new WaitGame(0.25f);
            }

            // Ordinary wander stays under the live brain/state machine; no validation route.
            SetStatus("obstacle audit: native wandering / territory");
            Live wander = SpawnMatrixEnemy(prefab, "wall-wander");
            yield return SpawnReady(wander);
            Vector3 home = ObstacleAuditCenter();
            ObstaclePlace(wander, home + Vector3.right * 3.2f, home);
            BoxCollider wanderBox = ObstacleBox(home);
            PlacePlayer(Ground(home + Vector3.right * 70), home);
            typeof(EnemyAiController).GetField("homePosition", BF).SetValue(wander.ai, home);
            typeof(EnemyAiController).GetField("wanderRadius", BF).SetValue(wander.ai, 8f);
            typeof(EnemyAiController).GetField("movementMode", BF).SetValue(wander.ai, EnemyMovementMode.Wander);
            // Fresh spawn has no harness route. Set a deterministic first wander target across the box.
            var state = typeof(EnemyAiController).GetField("state", BF);
            typeof(EnemyAiController).GetMethod("EnterState", BF).Invoke(wander.ai,
                new[] { Enum.Parse(state.FieldType, "Wander") });
            typeof(EnemyAiController).GetField("moveTarget", BF).SetValue(wander.ai, home + Vector3.left * 5f);
            var wm = new ObstacleMeasure(); wm.Begin(wander);
            float end = Time.time + 20f;
            while (Time.time < end)
            {
                Vector3 target = (Vector3)typeof(EnemyAiController).GetField("moveTarget", BF).GetValue(wander.ai);
                wm.Sample(wander, wanderBox, HorizontalDistance(wander.go.transform.position, target) > 0.9f, home, 8f);
                yield return null;
            }
            bool wanderPass = wm.reversals <= 2 && wm.maxStall < 2.5f && wm.penetrations == 0 && wm.maxTerritoryExcess < 0.6f;
            R.Add("Obstacle", "wall.wander", "Native wander around box within territory", wanderPass ? "PASS" : "FAIL",
                wm.Describe(), wanderPass ? "" : "Native wander stalled, reversed, penetrated a wall or exceeded territory.");
            Cleanup(); yield return new WaitGame(0.25f);

            SetStatus("obstacle audit: combat pursuit around box");
            Live combat = SpawnMatrixEnemy(prefab, "wall-combat");
            yield return SpawnReady(combat);
            Vector3 combatCenter = ObstacleAuditCenter();
            ObstaclePlace(combat, combatCenter + Vector3.right * 4.5f, combatCenter - Vector3.right * 5f);
            BoxCollider combatBox = ObstacleBox(combatCenter);
            PlacePlayer(Ground(combatCenter - Vector3.right * 5f), combat.go.transform.position);
            combat.combat.SetTarget(_player.transform);
            var stateField = typeof(EnemyAiController).GetField("state", BF);
            typeof(EnemyAiController).GetMethod("EnterState", BF).Invoke(combat.ai,
                new[] { Enum.Parse(stateField.FieldType, "Chase") });
            var cm = new ObstacleMeasure(); cm.Begin(combat);
            bool reached = false; end = Time.time + 25f;
            while (Time.time < end)
            {
                string current = AiState(combat.ai);
                cm.Sample(combat, combatBox, current == "Chase", combatCenter, 0);
                if (PlayerEnemyDist(combat) < 3f) reached = true;
                TopUpPlayer();
                yield return null;
            }
            bool combatPass = reached && cm.reversals <= 2 && cm.maxStall < 2.5f && cm.penetrations == 0;
            R.Add("Obstacle", "wall.combat", "Live combat chase routes around blocking box", combatPass ? "PASS" : "FAIL",
                $"reachedAttackRange={reached}; states={string.Join("/", combat.phaseStates)}; " + cm.Describe(),
                combatPass ? "" : "Combat pursuit failed to clear obstruction or produced movement instability.");
            R.Add("Coverage", "wall.remaining", "Additional coverage still required", "WARN",
                "This run covers eight box approaches, ping-pong first leg, native territory wander and combat pursuit.",
                "Multiple enemies, complete repeated four-corner circuits, ping-pong return leg, temporary blockers and in-range combat strafe require additional cases; do not infer coverage.");
        }
    }
}
#endif

