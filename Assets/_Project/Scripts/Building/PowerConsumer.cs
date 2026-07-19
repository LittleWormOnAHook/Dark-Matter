using UnityEngine;

namespace Project.Building
{
    /// <summary>
    /// Lightweight power-gated device — toggles a Light (and/or emissive renderer) on/off based on its
    /// building's PowerGenerator. Event-driven (no polling): subscribes once and reacts to
    /// PowerGenerator.PowerStateChanged. Attach to any child of a building that already has a
    /// PowerGenerator on its root (or an ancestor); the generator is auto-resolved via
    /// GetComponentInParent when not explicitly assigned.
    /// </summary>
    [DisallowMultipleComponent]
    public class PowerConsumer : MonoBehaviour
    {
        [Tooltip("Resolved automatically via GetComponentInParent<PowerGenerator>() when left blank.")]
        [SerializeField] private PowerGenerator generator;

        [Tooltip("Resolved automatically via GetComponent<Light>() when left blank.")]
        [SerializeField] private Light targetLight;

        [SerializeField] private bool startPowered = true;

        public bool IsPowered { get; private set; }

        private void Awake()
        {
            if (generator == null)
                generator = GetComponentInParent<PowerGenerator>();

            if (targetLight == null)
                targetLight = GetComponent<Light>();

            IsPowered = startPowered;
        }

        private void OnEnable()
        {
            if (generator != null)
            {
                generator.PowerStateChanged += HandlePowerStateChanged;
                ApplyPowerState(generator.HasPower);
            }
            else
            {
                ApplyPowerState(startPowered);
            }
        }

        private void OnDisable()
        {
            if (generator != null)
                generator.PowerStateChanged -= HandlePowerStateChanged;
        }

        private void HandlePowerStateChanged(bool powered)
        {
            ApplyPowerState(powered);
        }

        private void ApplyPowerState(bool powered)
        {
            IsPowered = powered;
            if (targetLight != null)
                targetLight.enabled = powered;
        }
    }
}
