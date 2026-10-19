using Project.Combat;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>Lightweight hitch logger: frame time + GC count deltas + last marked event. DM_CombatCoreProfile "Hitch monitor".</summary>
    public sealed class DMHitchMonitor : MonoBehaviour
    {
        private static DMHitchMonitor _inst;
        private static string _lastEvent = "-";
        private static float _lastEventTime;
        private int _gc0, _gc1, _gc2;

        public static void Mark(string what)
        {
            _lastEvent = what;
            _lastEventTime = Time.realtimeSinceStartup;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (_inst != null) return;
            var go = new GameObject("DM_HitchMonitor") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            _inst = go.AddComponent<DMHitchMonitor>();
        }

        private void Update()
        {
            if ((Time.frameCount & 3) == 0)
                DMVolumetricBlood.PrewarmStep();
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (p == null || !p.logHitchesInPlay) return;
            int g0 = System.GC.CollectionCount(0), g1 = System.GC.CollectionCount(1), g2 = System.GC.CollectionCount(2);
            float ms = Time.unscaledDeltaTime * 1000f;
            if (ms >= p.hitchThresholdMs && _gc0 > 0)
                Debug.Log("[Hitch] " + ms.ToString("0") + " ms  gc0+" + (g0 - _gc0) + " gc1+" + (g1 - _gc1) + " gc2+" + (g2 - _gc2)
                    + "  last=" + _lastEvent + " (" + ((Time.realtimeSinceStartup - _lastEventTime) * 1000f).ToString("0") + " ms ago)");
            _gc0 = g0; _gc1 = g1; _gc2 = g2;
        }
    }
}
