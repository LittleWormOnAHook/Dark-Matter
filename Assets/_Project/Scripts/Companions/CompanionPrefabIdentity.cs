using Project.Core;
using Project.Pioneers;
using UnityEngine;

namespace Project.Companions
{
    /// <summary>
    /// Self-binding identity for a per-companion prefab baked by the Companion Prefab Tool
    /// (Assets/_Project/Editor/Companions/CompanionPrefabGenerator). Lets a specific named companion
    /// — e.g. "Kael-9.prefab" under Resources/Companions — be dropped directly into a scene or
    /// spawned by a generic spawner and bind itself to its NamedPioneerDefinition on Start, without
    /// needing PioneerRosterManager/CompanionRosterBridge to spawn it via the expedition trio flow.
    /// </summary>
    [RequireComponent(typeof(PioneerCompanionAgent))]
    public class CompanionPrefabIdentity : MonoBehaviour
    {
        [SerializeField] private NamedPioneerDefinition definition;
        [SerializeField] private int formationSlot;
        [Tooltip("If false, call BindNow() manually (e.g. from a custom spawner) instead of binding automatically on Start.")]
        [SerializeField] private bool bindOnStart = true;

        public NamedPioneerDefinition Definition => definition;

        public void SetDefinition(NamedPioneerDefinition value)
        {
            definition = value;
        }

        private void Start()
        {
            if (bindOnStart)
                BindNow();
        }

        /// <summary>
        /// Creates a fresh SkilledPioneerRecord from the assigned definition and binds this agent to
        /// it. Owner defaults to whatever PlayerLocator currently resolves as the player.
        /// </summary>
        public void BindNow()
        {
            BindNow(PlayerLocator.FindPlayerObject()?.transform);
        }

        public void BindNow(Transform owner)
        {
            if (definition == null)
            {
                Debug.LogWarning($"[{name}] CompanionPrefabIdentity has no NamedPioneerDefinition assigned.");
                return;
            }

            PioneerCompanionAgent agent = GetComponent<PioneerCompanionAgent>();
            if (agent == null)
                return;

            SkilledPioneerRecord record = SkilledPioneerRecord.CreateFromCatalog(definition, applyLoadoutDefaults: true);
            agent.BindRecord(record, owner, formationSlot);
        }
    }
}
