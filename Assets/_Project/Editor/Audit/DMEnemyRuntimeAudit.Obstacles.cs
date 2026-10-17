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

        [MenuItem("Tools/Dark Matter Genesis/Combat/Enemy Thin Wall Endpoint Audit (Play)")]
        private static void LaunchThinWallAudit()
        {
            Launch("{\"filter\":\"HumanoidEnemy_Invector\",\"obstaclesOnly\":true,\"thinWallsOnly\":true,\"weaponMatrix\":false,\"scenarios\":false,\"mode\":\"full\",\"force\":true}");
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
            collider.size = _opts.thinWallsOnly ? new Vector3(0.2f, 3f, 6f) : new Vector3(4f, 3f, 4f);
            Physics.SyncTransforms();
            return collider;
        }

        private static void ObstaclePlace(Live L, Vector3 position, Vector3 destination)
        {
            position = Ground(position);
            L.go.transform.position = position;
            // Teleport the scenario context too: retaining the prefab's original
            // home/last-seen position tests a distant leash, not this obstacle.
            typeof(EnemyAiController).GetField("homePosition", BF).SetValue(L.ai, position);
            typeof(EnemyAiController).GetField("lastKnownPlayerPosition", BF).SetValue(L.ai, destination);
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
            public float maxYawRate;
            public int routeRecoveries;
            private Vector3 previous, lastMove;
            private float lastTime, previousYaw, lastYawDelta;
            private int previousSide;
            public void Begin(Live L)
            {
                previous = L.go.transform.position; previousYaw = L.go.transform.eulerAngles.y;
                lastTime = Time.time; lastMove = Vector3.zero;
            }
            public void Sample(Live L, Collider box, bool expectsProgress, Vector3 home, float radius)
            {
                float dt = Time.time - lastTime;
                if (dt < 0.1f || L.go == null) return;
                lastTime = Time.time;
                Vector3 position = L.go.transform.position;
                Vector3 delta = position - previous; delta.y = 0f;
                bool contact = WallContactField != null && (bool)WallContactField.GetValue(L.ai);
                int side = WallSideField != null ? (int)WallSideField.GetValue(L.ai) : 0;
                routeRecoveries = (int)(typeof(EnemyAiController).GetField("WallRouteRecoveryCount", BF)?.GetValue(L.ai) ?? 0);
                if (contact && side != 0 && previousSide != 0 && side != previousSide) sideFlips++;
                if (contact && delta.magnitude > 0.005f && lastMove.magnitude > 0.005f &&
                    Vector3.Dot(delta.normalized, lastMove.normalized) < -0.5f) reversals++;
                float yawDelta = Mathf.DeltaAngle(previousYaw, L.go.transform.eulerAngles.y);
                maxYawRate = Mathf.Max(maxYawRate, Mathf.Abs(yawDelta) / dt);
                if (contact && Mathf.Abs(yawDelta) > 3f && Mathf.Abs(lastYawDelta) > 3f &&
                    Mathf.Sign(yawDelta) != Mathf.Sign(lastYawDelta))
                {
                    yawReversals++;
                    if (yawReversals <= 12)
                        yawTrace += $"t={Time.time:F2} yawDelta={yawDelta:F1} move={delta:F3} pos={position:F2} normal={typeof(EnemyAiController).GetField("chaseWallLastContactNormal", BF)?.GetValue(L.ai)}; ";
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
                $"sideFlips={sideFlips}, routeRecoveries={routeRecoveries}, maxYawRate={maxYawRate:F1}deg/s, maxStall={maxStall:F2}s, stationaryWalk={maxStationaryAnimation:F2}s, " +
                $"penetrationSamples={penetrations}, territoryExcess={maxTerritoryExcess:F2}m; firstStall=[{stallTrace}]; yawTrace=[{yawTrace}]";
        }

        [MenuItem("Tools/Dark Matter Genesis/Combat/Enemy Rock Mesh Audit (Play)")]
        private static void LaunchRockMeshAudit()
        {
            Launch("{\"filter\":\"HumanoidEnemy_Invector\",\"obstaclesOnly\":true,\"rockMeshesOnly\":true,\"weaponMatrix\":false,\"scenarios\":false,\"mode\":\"full\",\"force\":true}");
        }

        private static IEnumerator ScnRockMeshes(GameObject prefab, DMAuditEnemyResult R)
        {
            var rocks = new List<MeshCollider>();
            foreach (var collider in Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Exclude))
                if (collider.enabled && collider.sharedMesh != null && !collider.isTrigger &&
                    collider.name.IndexOf("rock", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    (EnemyChaseWallSlide.ChaseObstructionMask & (1 << collider.gameObject.layer)) != 0)
                    rocks.Add(collider);
            rocks.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            R.Add("Rock", "rock.setup", "Active scene rock MeshColliders", rocks.Count > 0 ? "PASS" : "FAIL",
                $"{rocks.Count} rock colliders; existing scene geometry retained, not replaced with convex proxies", "");
            if (rocks.Count == 0) yield break;
            bool combatTested = false;
            for (int caseIndex = 0; caseIndex < 4; caseIndex++)
            {
                var rock = rocks[(caseIndex / 2) * (rocks.Count / 2)];
                Vector3 direction = caseIndex % 2 == 0 ? Vector3.right : Vector3.forward;
                Vector3 center = rock.bounds.center;
                float extent = Mathf.Abs(direction.x) * rock.bounds.extents.x + Mathf.Abs(direction.z) * rock.bounds.extents.z;
                Vector3 start = Vector3.zero, destination = Vector3.zero;
                bool clear = false;
                for (int attempt = 0; attempt < 4; attempt++)
                {
                    float arm = extent + 2f + attempt * 2f;
                    start = Ground(center + direction * arm);
                    destination = Ground(center - direction * arm);
                    bool validHeight = Mathf.Abs(start.y - _origin.y) < 2.5f && Mathf.Abs(destination.y - _origin.y) < 2.5f;
                    bool startClear = !Physics.CheckCapsule(start + Vector3.up * 0.4f, start + Vector3.up * 1.4f,
                        0.35f, EnemyChaseWallSlide.ChaseObstructionMask, QueryTriggerInteraction.Ignore);
                    bool endClear = !Physics.CheckCapsule(destination + Vector3.up * 0.4f, destination + Vector3.up * 1.4f,
                        0.35f, EnemyChaseWallSlide.ChaseObstructionMask, QueryTriggerInteraction.Ignore);
                    if (validHeight && startClear && endClear) { clear = true; break; }
                }
                if (!clear)
                {
                    R.Add("Rock", $"rock.{caseIndex}", "Scene rock approach " + rock.name, "WARN",
                        "No clear ground-level endpoints found in the existing cluster", "Skipped instead of teleporting into/on top of a rock.");
                    continue;
                }
                SetStatus($"rock audit: {caseIndex + 1}/4 {rock.name}");
                Live L = SpawnMatrixEnemy(prefab, "rock-" + caseIndex);
                yield return SpawnReady(L);
                ObstaclePlace(L, start, destination);
                PlacePlayer(Ground(_origin + Vector3.right * 100f), center);
                L.ai.BeginPatrolRouteNow(new[] { destination, start }, EnemyPatrolMode.PingPong);
                var measure = new ObstacleMeasure(); measure.Begin(L);
                bool arrived = false;
                float end = Time.time + 25f;
                while (Time.time < end)
                {
                    measure.Sample(L, rock, true, center, 0);
                    if (HorizontalDistance(L.go.transform.position, destination) < 0.9f) { arrived = true; break; }
                    yield return null;
                }
                bool pass = arrived && measure.reversals <= 2 && measure.maxStall < 2.5f && measure.penetrations == 0;
                R.Add("Rock", $"rock.{caseIndex}", "Scene rock approach " + rock.name, pass ? "PASS" : "FAIL",
                    $"arrived={arrived}; mesh={rock.sharedMesh.name}, convex={rock.convex}; " + measure.Describe(),
                    pass ? "" : "Real MeshCollider/cluster route failed, stalled, repeatedly reversed or penetrated the sampled rock.");
                Cleanup(); yield return new WaitGame(0.25f);
                // Use the same verified endpoints for a real three-enemy pursuit.
                // Existing rocks, brain, spacing and attack authority remain live.
                if (!combatTested)
                {
                    combatTested = true;
                    yield return ScnRockCombat(prefab, R, rock, start, destination);
                }
            }
        }

        private static IEnumerator ScnRockCombat(GameObject prefab, DMAuditEnemyResult R,
            MeshCollider rock, Vector3 start, Vector3 destination)
        {
            SetStatus("rock audit: three-enemy combat pursuit");
            var group = new List<Live>();
            var measures = new List<ObstacleMeasure>();
            var reached = new bool[3];
            var attacked = new bool[3];
            Vector3 lateral = Vector3.Cross(Vector3.up, (destination - start).normalized);
            for (int i = 0; i < 3; i++)
            {
                Vector3 spawn = Ground(start + lateral * ((i - 1) * 1.5f));
                if (Physics.CheckCapsule(spawn + Vector3.up * 0.4f, spawn + Vector3.up * 1.4f,
                    0.35f, EnemyChaseWallSlide.ChaseObstructionMask, QueryTriggerInteraction.Ignore))
                {
                    R.Add("RockCombat", "rock.combat.setup", "Three clear spawn positions", "WARN",
                        "A combat spawn was obstructed; group scenario skipped.", "No rock or collider was moved.");
                    foreach (var member in group) if (member.go != null) Object.Destroy(member.go);
                    Cleanup(); yield break;
                }
                Live memberLive = SpawnMatrixEnemy(prefab, "rock-combat-" + i);
                group.Add(memberLive);
                yield return SpawnReady(memberLive);
                ObstaclePlace(memberLive, spawn, destination);
                var measure = new ObstacleMeasure(); measure.Begin(memberLive); measures.Add(measure);
            }
            PlacePlayer(destination, start);
            var stateField = typeof(EnemyAiController).GetField("state", BF);
            foreach (var member in group)
            {
                member.combat.SetTarget(_player.transform);
                typeof(EnemyAiController).GetMethod("EnterState", BF).Invoke(member.ai,
                    new[] { Enum.Parse(stateField.FieldType, "Chase") });
            }
            float until = Time.time + 35f;
            while (Time.time < until)
            {
                for (int i = 0; i < group.Count; i++)
                {
                    var member = group[i];
                    string current = AiState(member.ai);
                    measures[i].Sample(member, rock, current == "Chase", destination, 0f);
                    if (PlayerEnemyDist(member) < 4f) reached[i] = true;
                    if (current == "Attack") attacked[i] = true;
                }
                TopUpPlayer();
                yield return null;
            }
            for (int i = 0; i < group.Count; i++)
            {
                var measure = measures[i];
                bool pass = reached[i] && measure.maxStall < 2.5f && measure.penetrations == 0;
                R.Add("RockCombat", "rock.combat." + i, "Live group combat around scene rock", pass ? "PASS" : "FAIL",
                    $"reachedEngagementRange={reached[i]}, enteredAttack={attacked[i]}; " + measure.Describe(),
                    "Stalls measured only during Chase; Hold/Attack pauses are intentional. Penetration checks cover the sampled rock only.");
            }
            foreach (var member in group) if (member.go != null) Object.Destroy(member.go);
            Cleanup(); yield return new WaitGame(0.25f);
        }

        private static IEnumerator ScnObstacles(GameObject prefab, DMAuditEnemyResult R)
        {
            // Deterministic route-detector contract, without moving the root or
            // overriding steering. Normal approach cases below remain unseeded.
            Live probe = SpawnMatrixEnemy(prefab, "route-detector");
            yield return SpawnReady(probe);
            var aiType = typeof(EnemyAiController);
            Vector3 probeGoal = probe.go.transform.position + Vector3.right * 10f;
            Action<string, object> set = (name, value) => aiType.GetField(name, BF).SetValue(probe.ai, value);
            var track = aiType.GetMethod("TrackWallRouteProgress", BF);
            Func<bool> requested = () => (bool)aiType.GetField("WallRouteRecoveryRequested", BF).GetValue(probe.ai);
            set("wallRouteGoal", probeGoal); set("wallRoutePrevious", probe.go.transform.position);
            set("wallRouteLastSample", Time.time); set("wallRouteBestDistance", 0f);
            set("wallRouteNoProgress", 30f); set("wallRoutePath", 30f); set("ChaseWallFollowSideSign", 1);
            track.Invoke(probe.ai, new object[] { probeGoal });
            bool detectsLoop = requested();
            track.Invoke(probe.ai, new object[] { probeGoal + Vector3.forward * 2f });
            bool resetsForNewGoal = !requested();
            aiType.GetMethod("NoteWallRouteRecovery", BF).Invoke(probe.ai, null);
            set("wallRouteNoProgress", 30f); set("wallRoutePath", 30f); set("wallRouteBestDistance", 0f);
            track.Invoke(probe.ai, new object[] { probeGoal + Vector3.forward * 2f });
            bool cooldownBlocksRepeat = !requested();
            R.Add("Recovery", "wall.recovery.detector", "No-progress detector, goal change and cooldown",
                detectsLoop && resetsForNewGoal && cooldownBlocksRepeat ? "PASS" : "FAIL",
                $"seeded stale history detected={detectsLoop}, changed goal reset={resetsForNewGoal}, cooldown prevented repeat={cooldownBlocksRepeat}",
                "Detector contract only: seeded history, no injected root movement. Natural recovery counts reported separately.");
            Cleanup(); yield return new WaitGame(0.25f);
            // Each face from both sides exercises both winding choices at box corners.
            for (int face = 0; face < (_opts.thinWallsOnly ? 4 : 5); face++)
            for (int side = -1; side <= 1; side += 2)
            {
                string id = $"{(_opts.thinWallsOnly ? "thin" : "wall")}.face{face}.side{side}";
                SetStatus("obstacle audit: " + id);
                Live L = SpawnMatrixEnemy(prefab, id);
                yield return SpawnReady(L);
                Vector3 center = face < 4 ? ObstacleAuditCenter() : Ground(_origin + new Vector3(0, 0, 22));
                Quaternion rotation = Quaternion.Euler(0, face * 90, 0);
                Vector3 start = center + rotation * (_opts.thinWallsOnly
                    ? new Vector3(2.5f, 0, side * (face % 2 == 0 ? 2.4f : -3.5f))
                    : new Vector3(4.5f, 0, 0));
                Vector3 destination = center + rotation * (_opts.thinWallsOnly
                    ? new Vector3(-3f, 0, side * 2.4f) : new Vector3(-5, 0, side * 0.8f));
                ObstaclePlace(L, start, destination);
                BoxCollider box = ObstacleBox(center);
                if (_opts.thinWallsOnly) { box.transform.rotation = rotation; Physics.SyncTransforms(); }
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

            Cleanup(); yield return new WaitGame(0.25f);

            // Reproduce the live failure: the player pressures a melee enemy backward until its
            // movement capsule overlaps static geometry while the attack state owns locomotion.
            SetStatus("obstacle audit: melee pressure wall recovery");
            Live pinned = SpawnMatrixEnemy(prefab, "wall-melee-pressure");
            yield return SpawnReady(pinned);
            Vector3 pinCenter = ObstacleAuditCenter();
            BoxCollider pinBox = ObstacleBox(pinCenter);
            CapsuleCollider pinCapsule = pinned.go.GetComponent<CapsuleCollider>();
            float capsuleExtent = pinCapsule != null ? pinCapsule.bounds.extents.x : 0.35f;
            Vector3 pinPosition = pinCenter + Vector3.right * (pinBox.bounds.extents.x + capsuleExtent - 0.18f);
            Vector3 pressurePlayer = Ground(pinPosition + Vector3.right * 1.15f);
            ObstaclePlace(pinned, pinPosition, pressurePlayer);
            PlacePlayer(pressurePlayer, pinPosition);
            pinned.combat.SetTarget(_player.transform);
            typeof(EnemyAiController).GetMethod("EnterState", BF).Invoke(pinned.ai,
                new[] { Enum.Parse(stateField.FieldType, "Attack") });
            pinned.combat.TryAttack();
            Physics.SyncTransforms();

            float initialDepth = 0f;
            bool beganPenetrating = pinCapsule != null && Physics.ComputePenetration(
                pinCapsule, pinCapsule.transform.position, pinCapsule.transform.rotation,
                pinBox, pinBox.transform.position, pinBox.transform.rotation,
                out _, out initialDepth) && initialDepth > 0.03f;
            bool attackObserved = pinned.combat.IsAttacking;
            bool cleared = false;
            float maxDepth = beganPenetrating ? initialDepth : 0f;
            end = Time.time + 2f;
            while (Time.time < end)
            {
                TopUpPlayer();
                attackObserved |= pinned.combat.IsAttacking;
                Physics.SyncTransforms();
                float depth = 0f;
                bool penetrating = pinCapsule != null && Physics.ComputePenetration(
                    pinCapsule, pinCapsule.transform.position, pinCapsule.transform.rotation,
                    pinBox, pinBox.transform.position, pinBox.transform.rotation,
                    out _, out depth) && depth > 0.03f;
                if (penetrating) maxDepth = Mathf.Max(maxDepth, depth);
                else if (beganPenetrating) { cleared = true; break; }
                yield return null;
            }
            bool pressurePass = beganPenetrating && cleared && attackObserved;
            R.Add("Obstacle", "wall.combat-pressure", "Melee pressure extraction during Attack", pressurePass ? "PASS" : "FAIL",
                $"seededPenetration={beganPenetrating}, initialDepth={initialDepth:F3}m, maxDepth={maxDepth:F3}m, " +
                $"attackObserved={attackObserved}, clearedWithin2s={cleared}, finalState={AiState(pinned.ai)}",
                pressurePass ? "" : "Enemy must escape a static box overlap while melee attack owns normal locomotion.");

            Cleanup(); yield return new WaitGame(0.25f);

            // Kinematic rigidbody on the blocker matches building props; static-only extract must not skip it.
            SetStatus("obstacle audit: kinematic wall recovery");
            Live kinPinned = SpawnMatrixEnemy(prefab, "wall-kinematic-pressure");
            yield return SpawnReady(kinPinned);
            Vector3 kinCenter = ObstacleAuditCenter() + Vector3.forward * 6f;
            BoxCollider kinBox = ObstacleBox(kinCenter);
            var kinBody = kinBox.gameObject.AddComponent<Rigidbody>();
            kinBody.isKinematic = true;
            kinBody.useGravity = false;
            CapsuleCollider kinCapsule = kinPinned.go.GetComponent<CapsuleCollider>();
            float kinExtent = kinCapsule != null ? kinCapsule.bounds.extents.x : 0.35f;
            Vector3 kinPos = kinCenter + Vector3.right * (kinBox.bounds.extents.x + kinExtent - 0.16f);
            Vector3 kinPlayer = Ground(kinPos + Vector3.right * 1.1f);
            ObstaclePlace(kinPinned, kinPos, kinPlayer);
            PlacePlayer(kinPlayer, kinPos);
            kinPinned.combat.SetTarget(_player.transform);
            typeof(EnemyAiController).GetMethod("EnterState", BF).Invoke(kinPinned.ai,
                new[] { Enum.Parse(stateField.FieldType, "Attack") });
            kinPinned.combat.TryAttack();
            Physics.SyncTransforms();

            float kinInitial = 0f;
            bool kinBegan = kinCapsule != null && Physics.ComputePenetration(
                kinCapsule, kinCapsule.transform.position, kinCapsule.transform.rotation,
                kinBox, kinBox.transform.position, kinBox.transform.rotation,
                out _, out kinInitial) && kinInitial > 0.03f;
            bool kinCleared = false;
            float kinMax = kinBegan ? kinInitial : 0f;
            end = Time.time + 2f;
            while (Time.time < end)
            {
                TopUpPlayer();
                Vector3 push = kinPos - _player.transform.position;
                push.y = 0f;
                if (push.sqrMagnitude > 0.01f)
                    _player.transform.position += push.normalized * (Time.deltaTime * 1.35f);
                Physics.SyncTransforms();
                float depth = 0f;
                bool penetrating = kinCapsule != null && Physics.ComputePenetration(
                    kinCapsule, kinCapsule.transform.position, kinCapsule.transform.rotation,
                    kinBox, kinBox.transform.position, kinBox.transform.rotation,
                    out _, out depth) && depth > 0.03f;
                if (penetrating) kinMax = Mathf.Max(kinMax, depth);
                else if (kinBegan) { kinCleared = true; break; }
                yield return null;
            }
            bool kinPass = kinBegan && kinCleared;
            R.Add("Obstacle", "wall.combat-pressure-kinematic", "Extract vs kinematic static box during Attack", kinPass ? "PASS" : "FAIL",
                $"seededPenetration={kinBegan}, initialDepth={kinInitial:F3}m, maxDepth={kinMax:F3}m, clearedWithin2s={kinCleared}",
                kinPass ? "" : "Static extract ignored kinematic blockers (attachedRigidbody filter).");

            R.Add("Coverage", "wall.remaining", "Additional coverage still required", "WARN",
                (_opts.thinWallsOnly ? "Thin wall: 0.2m thickness, both endpoints, perpendicular and oblique approaches, four rotations. " : "Box approaches. ") + "Ping-pong first leg, native territory wander and combat pursuit.",
                "Multiple enemies, complete repeated four-corner circuits, ping-pong return leg and temporary blockers require additional cases; do not infer coverage.");
        }
    }
}
#endif

