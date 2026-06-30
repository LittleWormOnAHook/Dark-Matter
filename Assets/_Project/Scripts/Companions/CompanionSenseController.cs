using System.Collections.Generic;
using Project.Echoes;
using Project.Pioneers;
using UnityEngine;

namespace Project.Companions
{
    /// <summary>
    /// Detects nearby echo signals; Infiltrator Scout pioneers extend detection radius.
    /// </summary>
    public class CompanionSenseController : MonoBehaviour
    {
        [SerializeField] private float baseDetectionRadius = 18f;
        [SerializeField] private float infiltratorBonusRadius = 10f;
        [SerializeField] private float scanInterval = 0.75f;

        private SkilledPioneerClass pioneerClass;
        private float nextScanTime;
        private EchoSignalSummary nearestSignal;

        public EchoSignalSummary NearestSignal => nearestSignal;
        public float EffectiveRadius =>
            baseDetectionRadius + (pioneerClass == SkilledPioneerClass.InfiltratorScout ? infiltratorBonusRadius : 0f);

        public void Initialize(SkilledPioneerClass pioneerClassValue)
        {
            pioneerClass = pioneerClassValue;
        }

        private void Update()
        {
            if (Time.time < nextScanTime)
                return;

            nextScanTime = Time.time + scanInterval;
            nearestSignal = default;
            float bestDistance = EffectiveRadius;

            IReadOnlyList<EchoSignalSummary> signals = EchoSignalRegistry.ActiveSignals;
            for (int i = 0; i < signals.Count; i++)
            {
                EchoSignalSummary signal = signals[i];
                float distance = Vector3.Distance(transform.position, signal.WorldPosition);
                if (distance <= EffectiveRadius && distance < bestDistance)
                {
                    bestDistance = distance;
                    nearestSignal = signal;
                }
            }
        }
    }
}
