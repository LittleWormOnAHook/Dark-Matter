using Project.Companions;
using Project.Pioneers;
using UnityEngine;

namespace Project.Echoes
{
    /// <summary>
    /// Optional seed for a baked Echo prefab (Resources/Echoes, built by the Companion Prefab Tool).
    /// If a NamedPioneerDefinition is assigned, EchoWorldEntity initializes itself from it on Awake —
    /// already-synced and immediately rescuable, since a named/authored companion isn't meant to be a
    /// random hostile encounter. Leave the definition empty to get a procedurally generated Echo
    /// instead (EchoGenerator.GenerateSignal), useful for generic "found in the world" placements
    /// that should still roll a random class/disposition.
    /// </summary>
    [RequireComponent(typeof(EchoWorldEntity))]
    public class EchoDefinitionSeed : MonoBehaviour
    {
        [SerializeField] private NamedPioneerDefinition definition;
        [SerializeField] private EchoDisposition proceduralDisposition = EchoDisposition.Neutral;

        public NamedPioneerDefinition Definition => definition;

        public void SetDefinition(NamedPioneerDefinition value)
        {
            definition = value;
        }

        private void Awake()
        {
            EchoWorldEntity entity = GetComponent<EchoWorldEntity>();
            if (entity == null || entity.SignalRecord != null)
                return;

            SkilledPioneerRecord record = definition != null
                ? SkilledPioneerRecord.CreateFromCatalog(definition, applyLoadoutDefaults: false)
                : EchoGenerator.GenerateSignal(proceduralDisposition);

            entity.Initialize(record);
            ConfigureWorldAmbient(pioneerDefinition: definition);
        }

        private void ConfigureWorldAmbient(NamedPioneerDefinition pioneerDefinition)
        {
            if (pioneerDefinition == null)
                return;

            CompanionWorldAmbientBehavior ambient = GetComponent<CompanionWorldAmbientBehavior>();
            if (ambient != null)
                ambient.Configure(pioneerDefinition);
        }
    }
}
