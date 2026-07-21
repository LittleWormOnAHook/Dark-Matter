using Project.Echoes;
using Project.Pioneers;
using UnityEngine;

namespace Project.Companions
{
    /// <summary>
    /// Drives pre-recruitment world Echo/recruit ambient life (PingPong patrol or Idle stand).
    /// Does not follow the player until they join the colony via conversation/rescue.
    /// </summary>
    [DisallowMultipleComponent]
    public class CompanionWorldAmbientBehavior : MonoBehaviour
    {
        [SerializeField] private CompanionFollowBehaviorMode defaultMode = CompanionFollowBehaviorMode.PingPong;
        [SerializeField] private float pingPongPatrolRadius = 4f;
        [SerializeField] private NamedPioneerDefinition definition;

        private CompanionFollowController followController;
        private bool initialized;

        public NamedPioneerDefinition Definition
        {
            get => definition;
            set => definition = value;
        }

        private void Awake()
        {
            followController = GetComponent<CompanionFollowController>();
            if (followController == null)
                followController = gameObject.AddComponent<CompanionFollowController>();
        }

        private void Start()
        {
            TryInitializeAmbient();
        }

        public void Configure(NamedPioneerDefinition pioneerDefinition)
        {
            definition = pioneerDefinition;
            initialized = false;
            TryInitializeAmbient();
        }

        private void TryInitializeAmbient()
        {
            if (initialized || followController == null)
                return;

            CompanionFollowBehaviorMode mode = ResolveAmbientMode();
            PioneerWorldIdleJob idleJob = ResolveWorldIdleJob();
            string seed = ResolveSeedId();

            followController.InitializeWorldAmbient(
                transform.position,
                mode,
                seed,
                pingPongPatrolRadius,
                idleJob);

            initialized = true;
        }

        private CompanionFollowBehaviorMode ResolveAmbientMode()
        {
            if (definition?.behavior != null)
                return definition.behavior.worldAmbientMode;

            return defaultMode;
        }

        private PioneerWorldIdleJob ResolveWorldIdleJob()
        {
            if (definition?.behavior != null)
                return definition.behavior.worldIdleJob;

            return PioneerWorldIdleJob.None;
        }

        private string ResolveSeedId()
        {
            if (definition != null && !string.IsNullOrWhiteSpace(definition.ResolvedId))
                return definition.ResolvedId;

            UniqueRecruitEntity recruit = GetComponent<UniqueRecruitEntity>();
            if (recruit?.Record != null && !string.IsNullOrWhiteSpace(recruit.Record.id))
                return recruit.Record.id;

            EchoWorldEntity echo = GetComponent<EchoWorldEntity>();
            if (echo?.SignalRecord != null && !string.IsNullOrWhiteSpace(echo.SignalRecord.id))
                return echo.SignalRecord.id;

            return name;
        }
    }
}
