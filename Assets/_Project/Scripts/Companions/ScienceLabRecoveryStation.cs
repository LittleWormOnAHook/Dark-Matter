using System.Collections.Generic;
using Project.Core;
using Project.Interaction;
using Project.Map;
using Project.Pioneers;
using Project.UI;
using UnityEngine;

namespace Project.Companions
{
    /// <summary>
    /// Spawns injured pioneer recovery interactables at the Science Lab.
    /// </summary>
    public class ScienceLabRecoveryStation : MonoBehaviour
    {
        public static ScienceLabRecoveryStation Instance { get; private set; }

        [SerializeField] private Transform recoveryAnchor;
        [SerializeField] private Vector3 fallbackLabPosition = new Vector3(-32.72f, 0.06f, 9.89f);
        [SerializeField] private Vector3 slotSpacing = new Vector3(1.6f, 0f, 0f);
        [SerializeField] private float proxyHeight = 0.9f;

        private readonly Dictionary<string, InjuredPioneerLabRecoverable> proxies = new Dictionary<string, InjuredPioneerLabRecoverable>();

        public static ScienceLabRecoveryStation EnsureExists()
        {
            if (Instance != null)
                return Instance;

            ScienceLabRecoveryStation existing = Object.FindAnyObjectByType<ScienceLabRecoveryStation>();
            if (existing != null)
            {
                Instance = existing;
                return existing;
            }

            GameObject host = new GameObject("ScienceLabRecoveryStation");
            DontDestroyOnLoad(host);
            Instance = host.AddComponent<ScienceLabRecoveryStation>();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            ResolveRecoveryAnchor();
            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            if (roster != null)
                roster.OnRosterChanged += RefreshInjuredProxies;

            RefreshInjuredProxies();
        }

        private void OnDestroy()
        {
            if (PioneerRosterManager.Instance != null)
                PioneerRosterManager.Instance.OnRosterChanged -= RefreshInjuredProxies;

            if (Instance == this)
                Instance = null;
        }

        public void RefreshInjuredProxies()
        {
            PioneerRosterManager roster = PioneerRosterManager.Instance;
            if (roster == null)
                return;

            ResolveRecoveryAnchor();
            Vector3 anchor = recoveryAnchor != null ? recoveryAnchor.position : fallbackLabPosition;

            HashSet<string> activeInjured = new HashSet<string>();
            int slot = 0;

            for (int i = 0; i < roster.SkilledPioneers.Count; i++)
            {
                SkilledPioneerRecord record = roster.SkilledPioneers[i];
                if (record == null || record.WorkState != PioneerWorkState.Injured)
                    continue;

                activeInjured.Add(record.id);
                InjuredPioneerLabRecoverable proxy = GetOrCreateProxy(record.id);
                proxy.Configure(record.id, record.displayName);

                Vector3 offset = slotSpacing * slot;
                proxy.transform.position = anchor + offset + Vector3.up * proxyHeight;
                proxy.gameObject.SetActive(true);
                slot++;
            }

            List<string> stale = new List<string>();
            foreach (KeyValuePair<string, InjuredPioneerLabRecoverable> pair in proxies)
            {
                if (!activeInjured.Contains(pair.Key))
                    stale.Add(pair.Key);
            }

            for (int i = 0; i < stale.Count; i++)
            {
                if (proxies.TryGetValue(stale[i], out InjuredPioneerLabRecoverable proxy) && proxy != null)
                    Destroy(proxy.gameObject);

                proxies.Remove(stale[i]);
            }
        }

        private InjuredPioneerLabRecoverable GetOrCreateProxy(string pioneerId)
        {
            if (proxies.TryGetValue(pioneerId, out InjuredPioneerLabRecoverable existing) && existing != null)
                return existing;

            GameObject root = new GameObject($"InjuredPioneer_{pioneerId}");
            root.transform.SetParent(transform, false);

            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.isTrigger = true;
            collider.radius = 0.55f;
            collider.height = 1.6f;
            collider.center = new Vector3(0f, 0.8f, 0f);

            InjuredPioneerLabRecoverable recoverable = root.AddComponent<InjuredPioneerLabRecoverable>();
            proxies[pioneerId] = recoverable;
            return recoverable;
        }

        private void ResolveRecoveryAnchor()
        {
            if (recoveryAnchor != null)
                return;

            MapMarker[] markers = Object.FindObjectsByType<MapMarker>(FindObjectsInactive.Exclude);
            for (int i = 0; i < markers.Length; i++)
            {
                MapMarker marker = markers[i];
                if (marker == null || string.IsNullOrWhiteSpace(marker.Label))
                    continue;

                if (!marker.Label.Contains("Science", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                GameObject anchorObject = new GameObject("ScienceLabRecoveryAnchor");
                anchorObject.transform.SetParent(marker.transform, false);
                anchorObject.transform.localPosition = new Vector3(3.1f, 0f, 6.8f);
                recoveryAnchor = anchorObject.transform;
                return;
            }

            GameObject science = GameObject.Find("Science");
            if (science != null)
            {
                GameObject anchorObject = new GameObject("ScienceLabRecoveryAnchor");
                anchorObject.transform.SetParent(science.transform, false);
                anchorObject.transform.localPosition = new Vector3(3.1f, 0f, 6.8f);
                recoveryAnchor = anchorObject.transform;
            }
        }
    }
}
