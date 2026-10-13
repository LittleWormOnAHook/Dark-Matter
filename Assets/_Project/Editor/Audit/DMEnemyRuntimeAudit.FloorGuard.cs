using System.Collections.Generic;
using Project.AI;
using UnityEngine;

namespace Project.EditorTools.Audit
{
    /// <summary>
    /// 2026-10-13: the Gaia terrain must stay off during audits (enemies ended up under it and the player swung at nothing).
    /// EnforceTestTerrain disables every Terrain + TerrainCollider (re-checked every second, Gaia Runtime can re-enable it).
    /// GuardFloor teleports any enemy that sinks below the "Combat Terrain" test floor back on top and logs [AuditFloorGuard];
    /// the run is flagged in environment so misses after a rescue can be discounted.
    /// </summary>
    public static partial class DMEnemyRuntimeAudit
    {
        private const string TestFloorName = "Combat Terrain";
        private static Collider _testFloor;
        private static float _nextTerrainCheck, _nextFloorCheck;
        private static int _floorRescues, _terrainReenables;

        private static void EnforceTestTerrain(bool first)
        {
            if (first) { _floorRescues = 0; _terrainReenables = 0; _testFloor = null; _auditTarget = null; _auditTargetSwitches = 0; _auditTargetFirst = -1f; _auditAimYaw = float.NaN; }
            int off = 0;
            foreach (Terrain t in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include))
            {
                if (t == null || t.name == TestFloorName) continue;
                TerrainCollider tc = t.GetComponent<TerrainCollider>();
                if (t.gameObject.activeSelf || t.enabled || (tc != null && tc.enabled))
                {
                    if (tc != null) tc.enabled = false;
                    t.enabled = false;
                    t.gameObject.SetActive(false);
                    off++;
                }
            }
            if (off > 0)
            {
                Physics.SyncTransforms();
                if (!first) _terrainReenables++;
                Debug.Log("[AuditTerrain] disabled " + off + " Gaia terrain(s)" + (first ? " before spawns" : " (re-enabled at runtime, forced off again)"));
            }
            if (_testFloor == null)
            {
                GameObject f = GameObject.Find(TestFloorName);
                _testFloor = f != null ? f.GetComponentInChildren<Collider>() : null;
            }
        }

        private static void GuardFloor()
        {
            if (Time.realtimeSinceStartup >= _nextTerrainCheck)
            {
                _nextTerrainCheck = Time.realtimeSinceStartup + 1f;
                EnforceTestTerrain(false);
            }
            if (_testFloor == null || Time.realtimeSinceStartup < _nextFloorCheck) return;
            _nextFloorCheck = Time.realtimeSinceStartup + 0.25f;
            foreach (EnemyAiController ai in Object.FindObjectsByType<EnemyAiController>(FindObjectsInactive.Exclude))
            {
                Vector3 p = ai.transform.position;
                Ray down = new Ray(new Vector3(p.x, _testFloor.bounds.max.y + 50f, p.z), Vector3.down);
                if (!_testFloor.Raycast(down, out RaycastHit hit, 500f)) continue; // off the test floor: not our concern
                if (p.y >= hit.point.y - 0.75f) continue;
                Vector3 to = hit.point + Vector3.up * 0.1f;
                Rigidbody rb = ai.GetComponent<Rigidbody>();
                if (rb != null) { rb.position = to; if (!rb.isKinematic) rb.linearVelocity = Vector3.zero; }
                UnityEngine.AI.NavMeshAgent ag = ai.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (ag != null && ag.isOnNavMesh) ag.Warp(to); else ai.transform.position = to;
                _floorRescues++;
                string msg = "[AuditFloorGuard] " + ai.name + " was " + (hit.point.y - p.y).ToString("0.00") + " m below the test floor at t=" + Time.time.ToString("0.0") + ", teleported back (misses after this are unreliable)";
                Debug.LogWarning(msg);
                if (_run != null && _floorRescues <= 10) _run.environment.Add(msg);
            }
        }
    }
}
