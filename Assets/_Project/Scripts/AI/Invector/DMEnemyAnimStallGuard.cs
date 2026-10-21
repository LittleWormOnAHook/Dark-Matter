using Project.Combat;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// 2026-10-20o failsafe for the rare "knocked down -> snaps upright frozen on one combat-stance frame -> glides around
    /// the player" bug. Counts time while this alive, non-ragdolled, not-down enemy has an animator that is stalled:
    /// speed below enemyAnimStallMinSpeed outside a melee hit-stop hold, or a base-layer pose frozen (same state and
    /// normalized time) while the root moves, or the knockdown layer still weighted with no knockdown running.
    /// After enemyAnimStallSeconds it forces recovery: speed 1, knockdown layer weight 0, ResetState trigger (locomotion).
    /// Added by DMEnemyKnockdown.Awake. Settings: DM_CombatCoreProfile (Knockdown section).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMEnemyAnimStallGuard : MonoBehaviour
    {
        private static readonly int ResetStateHash = Animator.StringToHash("ResetState");
        private Animator _anim;
        private EnemyHealth _health;
        private EnemyInvectorRagdollBridge _bridge;
        private DMEnemyKnockdown _kd;
        private int _kdLayer = -2;
        private bool _hasReset;
        private float _stall, _lastN;
        private int _lastHash;
        private Vector3 _lastPos;

        private void Awake()
        {
            _health = GetComponent<EnemyHealth>();
            _bridge = GetComponent<EnemyInvectorRagdollBridge>();
            _kd = GetComponent<DMEnemyKnockdown>();
            _anim = GetComponent<Animator>();
            if (_anim == null) _anim = GetComponentInChildren<Animator>(true);
            _lastPos = transform.position;
        }

        private void Update()
        {
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (p == null || !p.enemyAnimStallFailsafe || _anim == null || !_anim.isActiveAndEnabled || Time.deltaTime <= 0f)
            { _stall = 0f; return; }

            if (_kdLayer == -2)
            {
                _kdLayer = _anim.runtimeAnimatorController != null ? _anim.GetLayerIndex(DMEnemyKnockdown.LayerName) : -1;
                foreach (AnimatorControllerParameter prm in _anim.parameters)
                    if (prm.nameHash == ResetStateHash && prm.type == AnimatorControllerParameterType.Trigger) _hasReset = true;
            }

            Vector3 pos = transform.position;
            Vector3 d = pos - _lastPos; d.y = 0f;
            float planarSpeed = d.magnitude / Time.deltaTime;
            _lastPos = pos;

            bool exempt = (_health != null && _health.IsDead) || DMEnemyKnockdown.IsDown(gameObject) ||
                          (_kd != null && _kd.IsActive) ||
                          (_bridge != null && (_bridge.HasActiveRagdoll || _bridge.IsCorpseRagdolled || _bridge.IsHitStaggerActive));
            DMMeleeHitStopHold hold = _anim.GetComponent<DMMeleeHitStopHold>();
            if (hold != null && hold.IsHolding) exempt = true;
            if (exempt) { _stall = 0f; _lastHash = 0; return; }

            AnimatorStateInfo st = _anim.GetCurrentAnimatorStateInfo(0);
            bool frozenPose = _lastHash == st.shortNameHash && Mathf.Abs(st.normalizedTime - _lastN) < 1e-5f && planarSpeed > 0.4f;
            _lastHash = st.shortNameHash; _lastN = st.normalizedTime;
            bool slow = _anim.speed < p.enemyAnimStallMinSpeed;
            bool kdStuck = _kdLayer >= 0 && _anim.GetLayerWeight(_kdLayer) > 0.01f;

            if (slow || frozenPose || kdStuck) _stall += Time.deltaTime;
            else _stall = 0f;
            if (_stall < p.enemyAnimStallSeconds) return;

            if (p.enemyAnimStallLog)
                Debug.LogWarning($"[AnimStall] {name} recovered: speed={_anim.speed:0.00} frozenPose={frozenPose} kdLayer={kdStuck} state={st.shortNameHash} moving={planarSpeed:0.0}m/s", this);
            _anim.speed = 1f;
            if (_kdLayer >= 0) _anim.SetLayerWeight(_kdLayer, 0f);
            _anim.SetFloat(DMEnemyKnockdown.SpeedParam, 1f);
            if (_hasReset) _anim.SetTrigger(ResetStateHash);
            _stall = 0f;
        }
    }
}
