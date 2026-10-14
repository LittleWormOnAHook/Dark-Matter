using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// Per-enemy ranged shot budget (DM_EnemyBrainProfile default, EnemyDefinition override).
    /// Gates <see cref="EnemyInvectorCombatBridge"/> shots; 0 max = unlimited (legacy infinite).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMEnemyRangedAmmoPool : MonoBehaviour
    {
        private int _maxShots;
        private int _remaining;
        private bool _unlimited;
        private float _reloadSeconds;
        private float _reloadReadyTime;
        private bool _reloadPending;

        public bool IsUnlimited => _unlimited;
        public int Remaining => _unlimited ? int.MaxValue : _remaining;
        public int MaxShots => _maxShots;
        public bool IsReloading => _reloadPending && Time.time < _reloadReadyTime;

        public bool CanFire => _unlimited || (_remaining > 0 && !IsReloading);

        public static int ResolveMaxShots(EnemyDefinition definition, DM_EnemyBrainProfile brain)
        {
            if (definition != null && definition.rangedAmmoPool > 0)
                return definition.rangedAmmoPool;

            DM_EnemyBrainProfile p = brain ?? DM_EnemyBrainProfile.Live;
            return p != null ? Mathf.Max(0, p.enemyRangedAmmoPoolDefault) : 1;
        }

        public static float ResolveReloadSeconds(DM_EnemyBrainProfile brain)
        {
            DM_EnemyBrainProfile p = brain ?? DM_EnemyBrainProfile.Live;
            return p != null ? Mathf.Max(0f, p.enemyRangedAmmoReloadSeconds) : 2.5f;
        }

        public void ApplyFromSources(EnemyDefinition definition, DM_EnemyBrainProfile brain)
        {
            DMEnemyBrain perBrain = GetComponent<DMEnemyBrain>();
            DM_EnemyBrainProfile resolvedBrain = perBrain != null ? perBrain.Profile : brain;
            _maxShots = ResolveMaxShots(definition, resolvedBrain);
            _unlimited = _maxShots <= 0;
            _reloadSeconds = ResolveReloadSeconds(resolvedBrain);
            Refill();
        }

        public void Refill()
        {
            _reloadPending = false;
            _reloadReadyTime = 0f;
            _remaining = _unlimited ? 0 : _maxShots;
        }

        public void ConsumeShot()
        {
            if (_unlimited)
                return;

            _remaining = Mathf.Max(0, _remaining - 1);
            if (_remaining <= 0 && _reloadSeconds > 0f)
            {
                _reloadPending = true;
                _reloadReadyTime = Time.time + _reloadSeconds;
            }
        }

        private void Update()
        {
            if (!_reloadPending || _unlimited)
                return;

            if (Time.time >= _reloadReadyTime)
            {
                Refill();
            }
        }
    }
}
