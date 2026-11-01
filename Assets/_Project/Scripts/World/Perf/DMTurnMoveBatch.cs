using System.Collections.Generic;
using UnityEngine;
using creepycat.scifikitvol3;

namespace Project.World.Perf
{
    /// <summary>
    /// 2026-10-31c: one Update for all Creepy Cat Turn_Move components (vendor script untouched). In Play the manager
    /// disables each enabled Turn_Move (so its own Update stops) and runs the identical Rotate/Translate math itself.
    /// If anything re-enables a Turn_Move it is handed back (no double motion). Off switch: Studio > Perf
    /// ("Batch Turn_Move", PlayerPrefs dm.turnMoveBatch). Rescans every 2 s for spawned objects.
    /// </summary>
    [DefaultExecutionOrder(0)]
    public sealed class DMTurnMoveBatch : MonoBehaviour
    {
        public const string PrefKey = "dm.turnMoveBatch";
        public static bool Enabled => PlayerPrefs.GetInt(PrefKey, 1) == 1;
        public static int Count => instance != null ? instance.items.Count : 0;
        private static DMTurnMoveBatch instance;
        private readonly List<Turn_Move> items = new List<Turn_Move>();
        private readonly HashSet<Turn_Move> owned = new HashSet<Turn_Move>();
        private float nextScan;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { instance = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!Enabled || instance != null) return;
            var go = new GameObject("DMTurnMoveBatch (auto)");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<DMTurnMoveBatch>();
        }

        private void Scan()
        {
            foreach (Turn_Move t in FindObjectsByType<Turn_Move>())
                if (t != null && t.enabled && !owned.Contains(t)) { t.enabled = false; owned.Add(t); items.Add(t); }
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextScan) { nextScan = Time.unscaledTime + 2f; Scan(); }
            float dt = Time.deltaTime;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                Turn_Move t = items[i];
                if (t == null) { items.RemoveAt(i); continue; }
                if (t.enabled) { owned.Remove(t); items.RemoveAt(i); continue; } // re-enabled by someone: it runs itself
                if (!t.gameObject.activeInHierarchy) continue;
                Transform tr = t.transform;
                Space s = t.World ? Space.World : Space.Self;
                tr.Rotate(t.TurnX * dt, t.TurnY * dt, t.TurnZ * dt, s);
                tr.Translate(t.MoveX * dt, t.MoveY * dt, t.MoveZ * dt, s);
            }
        }

        private void OnDestroy()
        {
            foreach (Turn_Move t in items) if (t != null) t.enabled = true;
            items.Clear(); owned.Clear();
            if (instance == this) instance = null;
        }
    }
}