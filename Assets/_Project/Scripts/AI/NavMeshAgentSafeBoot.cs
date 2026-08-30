using UnityEngine;
using UnityEngine.AI;

namespace Project.AI
{
    /// <summary>
    /// Defers NavMeshAgent enable until baked mesh exists nearby (Gaia streaming / partial bakes).
    /// Prevents "Failed to create agent because there is no valid NavMesh" console spam.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-200)]
    public sealed class NavMeshAgentSafeBoot : MonoBehaviour
    {
        [SerializeField] private float sampleRadius = 12f;
        [SerializeField] private float retrySeconds = 30f;

        private NavMeshAgent _agent;
        private float _deadline;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AttachToExistingAgents()
        {
            if (!Application.isPlaying)
                return;

            NavMeshAgent[] agents = FindObjectsByType<NavMeshAgent>(FindObjectsInactive.Include);

            for (int i = 0; i < agents.Length; i++)
            {
                NavMeshAgent agent = agents[i];
                if (agent == null || agent.GetComponent<NavMeshAgentSafeBoot>() != null)
                    continue;

                agent.gameObject.AddComponent<NavMeshAgentSafeBoot>();
            }
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null)
            {
                enabled = false;
                return;
            }

            _deadline = Time.unscaledTime + retrySeconds;
            _agent.enabled = false;
        }

        private void OnEnable()
        {
            if (_agent == null)
                _agent = GetComponent<NavMeshAgent>();

            if (_agent != null && _agent.enabled)
                _agent.enabled = false;
        }

        private void Update()
        {
            if (_agent == null)
            {
                enabled = false;
                return;
            }

            if (_agent.enabled && _agent.isOnNavMesh)
            {
                enabled = false;
                return;
            }

            if (Time.unscaledTime > _deadline)
            {
                enabled = false;
                return;
            }

            if (Time.frameCount % 3 != 0)
                return;

            if (!TryEnableOnNavMesh())
                return;

            enabled = false;
        }

        private bool TryEnableOnNavMesh()
        {
            if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
                return false;

            _agent.enabled = true;
            if (!_agent.isOnNavMesh)
                _agent.Warp(hit.position);

            return _agent.isOnNavMesh;
        }

        /// <summary>Call after spawn to ensure safe boot is attached before Unity enables the agent.</summary>
        public static void PrepareAgent(GameObject instance, float sampleRadius = 12f)
        {
            if (instance == null)
                return;

            NavMeshAgent agent = instance.GetComponent<NavMeshAgent>();
            if (agent == null)
                return;

            agent.enabled = false;
            NavMeshAgentSafeBoot boot = instance.GetComponent<NavMeshAgentSafeBoot>();
            if (boot == null)
                boot = instance.AddComponent<NavMeshAgentSafeBoot>();

            boot.sampleRadius = sampleRadius;
        }
    }
}
