using Project.Core;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// Slice 2 / B1-B3: short procedural slide-back for a hit enemy (backstep, heavy slide, stumble steps).
    /// Moves the transform only (same kinematic movement the AI uses), no NavMesh: a forward raycast stops it at walls and
    /// other bodies, and a ground sample under the next position stops it at a ledge. Added on demand, never on a prefab.
    /// All distances / times come from DM_CombatCoreProfile "Hit reactions (Slice 2)".
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMEnemyHitSlide : MonoBehaviour
    {
        private Vector3 _dir;
        private float _total;
        private float _moved;
        private float _start;
        private float _duration = 0.3f;
        private int _pulses = 1;
        private float _maxDrop = 0.5f;
        private float _yOffset;
        private float _lastGroundY;
        private bool _hasGround;
        private EnemyHealth _health;
        private EnemyInvectorRagdollBridge _bridge;

        public static void Begin(GameObject enemy, Vector3 direction, float meters, float seconds, int pulses, float maxLedgeDrop, float maxTotalMeters)
        {
            if (enemy == null || meters <= 0.001f || seconds <= 0.01f)
                return;

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return;

            EnemyAiController rollAi = enemy.GetComponent<EnemyAiController>();
            if (rollAi != null && rollAi.DodgeRollBlocksHitReactions)
                return; // a dodge roll is never pushed around by a hit slide

            DMEnemyHitSlide slide = enemy.GetComponent<DMEnemyHitSlide>();
            if (slide == null)
                slide = enemy.AddComponent<DMEnemyHitSlide>();

            slide.Restart(direction.normalized, meters, seconds, Mathf.Max(1, pulses), maxLedgeDrop, maxTotalMeters);

            // Slice 3: the leg animation exaggerates quick steps (stepAnimSpeedScale), hit backsteps included.
            EnemyAiController ai = enemy.GetComponent<EnemyAiController>();
            if (ai != null)
                ai.NoteQuickStep(seconds);
        }

        private void Restart(Vector3 direction, float meters, float seconds, int pulses, float maxLedgeDrop, float maxTotalMeters)
        {
            if (_health == null)
                _health = GetComponent<EnemyHealth>();
            if (_bridge == null)
                _bridge = GetComponent<EnemyInvectorRagdollBridge>();

            // A slide already running: keep what is left and add the new distance, capped so stacked hits cannot launch the enemy.
            float remaining = enabled ? Mathf.Max(0f, _total - _moved) : 0f;
            _total = Mathf.Min(remaining + meters, Mathf.Max(0.1f, maxTotalMeters));
            _moved = 0f;
            _dir = direction;
            _start = Time.time;
            _duration = seconds;
            _pulses = pulses;
            _maxDrop = Mathf.Max(0.05f, maxLedgeDrop);

            Vector3 pos = transform.position;
            _hasGround = EnemyGroundUtility.TryGetGroundY(pos, out float groundY);
            if (_hasGround)
            {
                _yOffset = pos.y - groundY;
                _lastGroundY = groundY;
            }

            enabled = true;
        }

        private void OnDisable()
        {
            _total = 0f;
            _moved = 0f;
        }

        private void Update()
        {
            if (_total <= 0.0005f || _moved >= _total - 0.0005f)
            {
                enabled = false;
                return;
            }

            if (GameplayWorldSimulation.IsFrozen)
                return;

            if ((_health != null && _health.IsDead) || (_bridge != null && (_bridge.HasActiveRagdoll || _bridge.IsKnockdownActive)))
            {
                enabled = false;
                return;
            }

            float t = Mathf.Clamp01((Time.time - _start) / _duration);
            float want = _total * Ease(t, _pulses);
            float delta = want - _moved;
            if (delta <= 0.0001f)
            {
                if (t >= 1f)
                    enabled = false;
                return;
            }

            Vector3 pos = transform.position;
            Vector3 requestedStep = _dir * delta;
            Vector3 safeStep = EnemyChaseWallSlide.ClampStepToGeometry(transform, requestedStep);
            bool geometryLimited = (safeStep - requestedStep).sqrMagnitude > 0.000004f;
            if (safeStep.sqrMagnitude < 0.000001f)
            {
                enabled = false;
                return;
            }

            Vector3 next = pos + safeStep;
            if (_hasGround)
            {
                if (!EnemyGroundUtility.TryGetGroundY(next, out float groundY) || groundY < _lastGroundY - _maxDrop)
                {
                    enabled = false;
                    return;
                }

                next.y = groundY + _yOffset;
                _lastGroundY = groundY;
            }

            transform.position = next;
            EnemyChaseWallSlide.ApplyStaticOverlapExtract(transform, DM_EnemyBrainProfile.Live, 3);
            { EnemyAiController slideAi = GetComponent<EnemyAiController>(); if (slideAi != null) slideAi.NoteMover("HitSlide", safeStep.magnitude); }
            _moved = geometryLimited
                ? Mathf.Min(_total, _moved + Mathf.Max(0f, Vector3.Dot(safeStep, _dir)))
                : want;
            if (geometryLimited || t >= 1f)
                enabled = false;
        }

        private static float Ease(float t, int pulses)
        {
            if (t >= 1f)
                return 1f;

            if (pulses <= 1)
            {
                float inv = 1f - t;
                return 1f - inv * inv * inv;
            }

            float p = t * pulses;
            int k = Mathf.Min(pulses - 1, Mathf.FloorToInt(p));
            float f = p - k;
            float smooth = f * f * (3f - 2f * f);
            return (k + smooth) / pulses;
        }
    }
}
