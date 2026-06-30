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
    /// Press E at the Science Lab to reassign injured pioneers after recovery.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class InjuredPioneerLabRecoverable : MonoBehaviour, IWorldUsable
    {
        private const float UsePriorityBase = 94f;

        [SerializeField] private string pioneerRecordId;
        [SerializeField] private string displayName = "Pioneer";
        [SerializeField] private float interactRange = 3.5f;

        public string PioneerRecordId => pioneerRecordId;
        public float InteractRange => interactRange;

        public void Configure(string recordId, string pioneerDisplayName)
        {
            pioneerRecordId = recordId;
            displayName = string.IsNullOrWhiteSpace(pioneerDisplayName) ? "Pioneer" : pioneerDisplayName;
        }

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;
        }

        private void OnEnable()
        {
            WorldUseController.Register(this);
        }

        private void OnDisable()
        {
            WorldUseController.Unregister(this);
        }

        public string GetPromptText()
        {
            PioneerRosterManager roster = PioneerRosterManager.Instance;
            SkilledPioneerRecord record = roster != null ? roster.FindSkilledById(pioneerRecordId) : null;
            if (record == null || record.WorkState != PioneerWorkState.Injured)
                return null;

            float remaining = roster.GetInjuryRecoveryRemaining(record);
            if (remaining > 0.5f)
                return $"{displayName} recovering ({Mathf.CeilToInt(remaining)}s)";

            return $"Press E to Reassign {displayName}";
        }

        public float GetUsePriority(WorldUseContext context)
        {
            if (!CanRecover(context, out float distance, out Vector3 aimPoint))
                return -1f;

            float aimRadius = distance <= interactRange * 0.75f ? 4.5f : 2.4f;
            float score = WorldUseController.ScorePickupAim(context.ViewRay, aimPoint, distance, context.UseRange, aimRadius);
            if (score < 0f)
            {
                if (distance > interactRange * 0.9f)
                    return -1f;

                score = Mathf.Max(0f, (interactRange - distance) * 55f);
            }

            return UsePriorityBase + score * 0.01f;
        }

        public bool TryUse(WorldUseContext context)
        {
            if (!CanRecover(context, out _, out _))
                return false;

            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            if (roster == null)
                return false;

            if (!roster.TryRecoverSkilledFromLab(pioneerRecordId, out string message))
            {
                if (!string.IsNullOrEmpty(message))
                    PickupToastUI.Show(message);
                return false;
            }

            CompanionRosterBridge bridge = Object.FindAnyObjectByType<CompanionRosterBridge>();
            bridge?.RefreshCompanions();

            ScienceLabRecoveryStation station = ScienceLabRecoveryStation.Instance;
            station?.RefreshInjuredProxies();

            PickupToastUI.Show(message);
            return true;
        }

        private bool CanRecover(WorldUseContext context, out float distance, out Vector3 aimPoint)
        {
            distance = 0f;
            aimPoint = transform.position + Vector3.up * 0.9f;

            if (!GameSession.HasStarted || string.IsNullOrWhiteSpace(pioneerRecordId) || !isActiveAndEnabled)
                return false;

            PioneerRosterManager roster = PioneerRosterManager.Instance;
            SkilledPioneerRecord record = roster != null ? roster.FindSkilledById(pioneerRecordId) : null;
            if (record == null || record.WorkState != PioneerWorkState.Injured)
                return false;

            if (roster.GetInjuryRecoveryRemaining(record) > 0.5f)
                return false;

            distance = Vector3.Distance(context.PlayerPosition, transform.position);
            if (distance > Mathf.Max(interactRange, context.UseRange))
                return false;

            Collider col = GetComponent<Collider>();
            if (col != null)
                aimPoint = col.bounds.center;

            return true;
        }

        public static InjuredPioneerLabRecoverable FindForPrompt(WorldUseContext context)
        {
            InjuredPioneerLabRecoverable[] recoverables = Object.FindObjectsByType<InjuredPioneerLabRecoverable>(FindObjectsInactive.Exclude);
            InjuredPioneerLabRecoverable best = null;
            float bestScore = float.MinValue;

            for (int i = 0; i < recoverables.Length; i++)
            {
                InjuredPioneerLabRecoverable recoverable = recoverables[i];
                if (recoverable == null || !recoverable.isActiveAndEnabled)
                    continue;

                string prompt = recoverable.GetPromptText();
                if (string.IsNullOrWhiteSpace(prompt))
                    continue;

                float distance = Vector3.Distance(context.PlayerPosition, recoverable.transform.position);
                if (distance > recoverable.interactRange)
                    continue;

                float score = recoverable.GetUsePriority(context);
                if (score < 0f)
                    score = recoverable.interactRange - distance;

                if (score <= bestScore)
                    continue;

                best = recoverable;
                bestScore = score;
            }

            return best;
        }
    }

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
