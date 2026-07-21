using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Snaps humanoid enemies back to terrain if Invector physics or death presentation drift them upward.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyTerrainRescue : MonoBehaviour
    {
        [SerializeField] private float maxAboveGround = 6f;
        [SerializeField] private float groundOffset = 0.05f;
        [SerializeField] private float checkInterval = 0.35f;

        private EnemyHealth _health;
        private Rigidbody _body;
        private float _nextCheckTime;

        private void Awake()
        {
            _health = GetComponent<EnemyHealth>();
            _body = GetComponent<Rigidbody>();
        }

        private void LateUpdate()
        {
            if (Time.time < _nextCheckTime)
                return;

            _nextCheckTime = Time.time + checkInterval;

            if (_health != null && _health.IsDead)
                return;

            if (!EnemyGroundUtility.IsAbnormallyHigh(transform.position, maxAboveGround))
                return;

            Vector3 snapped = EnemyGroundUtility.SnapPositionToGround(transform.position, groundOffset);
            transform.position = snapped;

            if (_body != null && !_body.isKinematic)
            {
                _body.linearVelocity = Vector3.zero;
                _body.angularVelocity = Vector3.zero;
            }
        }
    }
}
