#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Project.EditorTools.Audit
{
    /// <summary>
    /// Combat fix 2026-10-12, step 1: metrics that match what the clip shows.
    /// Camera: gap between the camera near plane and the player's body colliders (capsule + hitbox rig), not distance to the pivot.
    /// Overlap: body capsule penetration (enemy-enemy and enemy-player), separate from overlapping attack windows.
    /// Spawn pose: animator off / not initialised while the enemy is alive and not ragdolled.
    /// Yaw injection and the 8-phase shake study are off by default so the run is a normal fight.
    /// </summary>
    public static partial class DMEnemyRuntimeAudit
    {
        /// <summary>When true the audit rotates the player toward the attacker each frame (adds its own shake). Off for the combined audit.</summary>
        public static bool AuditYawInjection = false;
        /// <summary>When true the interrupt run cycles the 8 shake-study phases (changes hit stop / interpolation / rig mid-run). Off = baseline only.</summary>
        public static bool AuditShakePhases = false;
        /// <summary>Seconds at the end of the interrupt run with DMEnemyBlock.ForceBlockTest on (0 = off).</summary>
        public static float AuditForceBlockSeconds = 15f;

        private static readonly List<Collider> _camBody = new List<Collider>();
        private static GameObject _camBodyOwner;

        /// <summary>Smallest distance from the camera near-plane sphere to any enabled player body collider; negative or 0 = lens inside the body.</summary>
        private static float CameraSurfaceGap(Camera cam, GameObject player)
        {
            if (cam == null || player == null) return 99f;
            if (_camBodyOwner != player)
            {
                _camBody.Clear(); _camBodyOwner = player;
                foreach (Collider c in player.GetComponentsInChildren<Collider>(true))
                    if ((!c.isTrigger || c.name.StartsWith("DMPlayerHitbox_")) && (c is CapsuleCollider || c is BoxCollider || c is SphereCollider || (c is MeshCollider mc && mc.convex))) _camBody.Add(c); // body only: interaction / sensor triggers are not the mesh
            }
            float n = cam.nearClipPlane, hh = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * n, hw = hh * cam.aspect;
            float r = Mathf.Sqrt(n * n + hh * hh + hw * hw);
            Vector3 p = cam.transform.position; float best = 99f;
            foreach (Collider c in _camBody)
            {
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy) continue;
                Vector3 cp = Physics.ClosestPoint(p, c, c.transform.position, c.transform.rotation);
                best = Mathf.Min(best, Vector3.Distance(cp, p) - r);
            }
            return best;
        }

        /// <summary>Horizontal body radius (CharacterController / CapsuleCollider on the root, else 0.35).</summary>
        private static float BodyRadius(GameObject go)
        {
            if (go == null) return 0.35f;
            var cc = go.GetComponent<CharacterController>(); if (cc != null) return cc.radius * Mathf.Max(go.transform.lossyScale.x, go.transform.lossyScale.z);
            var cap = go.GetComponent<CapsuleCollider>(); if (cap != null) return cap.radius * Mathf.Max(go.transform.lossyScale.x, go.transform.lossyScale.z);
            return 0.35f;
        }

        /// <summary>True when an alive, non-ragdolled enemy shows a frozen / bind pose because its animator is off or not initialised.</summary>
        private static bool AnimatorNotReady(GameObject go, Project.AI.EnemyHealth health)
        {
            if (go == null || !go.activeInHierarchy || (health != null && health.IsDead)) return false;
            var rb = go.GetComponent<Project.AI.Invector.EnemyInvectorRagdollBridge>();
            if (rb != null && (rb.HasActiveRagdoll || rb.IsCorpseRagdolled)) return false;
            Animator an = go.GetComponentInChildren<Animator>();
            return an != null && (!an.isActiveAndEnabled || !an.isInitialized);
        }

        /// <summary>Cut depth as a fraction of the player capsule radius from the closest weapon gap (negative gap = penetration).</summary>
        private static float CutFraction(float minGap)
        {
            if (minGap > 1000f) return 0f;
            float pr = 0.3f; if (_player != null) { var c = _player.GetComponent<CapsuleCollider>(); if (c != null) pr = c.radius; }
            return Mathf.Clamp01(-minGap / Mathf.Max(0.05f, pr));
        }
    }
}
#endif
