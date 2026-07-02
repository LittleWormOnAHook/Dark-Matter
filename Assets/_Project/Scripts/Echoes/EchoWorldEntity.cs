using System;
using System.Collections.Generic;
using Project.AI;
using Project.Core;
using Project.Interaction;
using Project.Pioneers;
using Project.UI;
using UnityEngine;

namespace Project.Echoes
{
    /// <summary>
    /// World interactable neural echo signal with sync/rescue flow.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EchoWorldEntity : MonoBehaviour, IWorldUsable
    {
        private const float UsePriorityBase = 120f;

        [Header("Signal")]
        [SerializeField] private SkilledPioneerRecord signalRecord;
        [SerializeField] private string coreId = "Aether-9";
        [SerializeField] private float interactRange = 4f;

        [Header("Hostile Echo")]
        [SerializeField] private float hostileSyncBaseChance = 0.45f;
        [SerializeField] private GameObject hostileGuardianPrefab;

        private bool localSynced;
        private bool rescued;
        private GameObject hostileGuardian;

        public string EntityId => signalRecord != null ? signalRecord.id : string.Empty;
        public SkilledPioneerRecord SignalRecord => signalRecord;
        public float InteractRange => interactRange;
        public bool IsInteractable => !rescued && signalRecord != null;

        private void Awake()
        {
            Collider interactCollider = GetComponent<Collider>();
            if (interactCollider != null)
                interactCollider.isTrigger = true;
        }

        private void OnEnable()
        {
            WorldUseController.Register(this);
            RegisterSignalSummary();
            EnsureHostileGuardianIfNeeded();
        }

        private void OnDisable()
        {
            WorldUseController.Unregister(this);
            EchoSignalRegistry.Unregister(EntityId);
            DestroyHostileGuardian();
        }

        public void Initialize(SkilledPioneerRecord record, string signalCoreId = "Aether-9")
        {
            signalRecord = record;
            coreId = signalCoreId ?? "Aether-9";
            localSynced = record != null && record.Disposition != EchoDisposition.HostileUntilSynced;
            RegisterSignalSummary();
            EnsureHostileGuardianIfNeeded();
        }

        public float GetUsePriority(WorldUseContext context)
        {
            if (!GameSession.HasStarted || rescued || signalRecord == null)
                return -1f;

            float distance = Vector3.Distance(context.PlayerPosition, transform.position);
            if (distance > Mathf.Max(interactRange, context.UseRange))
                return -1f;

            return UsePriorityBase + (interactRange - distance);
        }

        public bool TryUse(WorldUseContext context)
        {
            if (!GameSession.HasStarted || rescued || signalRecord == null)
                return false;

            if (NeedsSync())
                return TrySync();

            return TryRescue();
        }

        private bool NeedsSync()
        {
            return signalRecord.Disposition == EchoDisposition.HostileUntilSynced && !localSynced;
        }

        private bool TrySync()
        {
            float chance = hostileSyncBaseChance + (1f - signalRecord.saturation) * 0.35f;
            if (HasInfiltratorInTrio())
                chance += 0.15f;

            if (UnityEngine.Random.value > chance)
            {
                HandleSyncFailure();
                return true;
            }

            localSynced = true;
            signalRecord.Disposition = EchoDisposition.Synced;
            DestroyHostileGuardian();
            RegisterSignalSummary();
            PickupToastUI.Show("Neural echo stabilized — rescue available.");
            return true;
        }

        private bool TryRescue()
        {
            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            SkilledPioneerRecord rescueCopy = CloneRecord(signalRecord);

            if (!roster.TryAddRescuedEcho(rescueCopy, out string message))
            {
                PickupToastUI.Show(string.IsNullOrWhiteSpace(message) ? "Rescue failed." : message);
                roster.AppendEchoChronicle(EchoChronicleEntry.CreateFailure(signalRecord.displayName, coreId));
                return false;
            }

            rescued = true;
            EchoSignalRegistry.Unregister(EntityId);

            string classLine = $"{SkilledPioneerClassUtility.ToDisplayName(rescueCopy.pioneerClass)} — {PioneerTraitUtility.GetDispositionLabel(rescueCopy.Disposition)}";
            string abilitySummary = PioneerTraitUtility.FormatTraitList(rescueCopy.traitIds);

            EchoRescueRevealUI.Show(rescueCopy.displayName, classLine, abilitySummary, () =>
            {
                Destroy(gameObject);
            });

            gameObject.SetActive(false);
            return true;
        }

        private void HandleSyncFailure()
        {
            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            roster.AppendEchoChronicle(EchoChronicleEntry.CreateFailure(signalRecord.displayName, coreId));
            PickupToastUI.Show("Sync failed — imprint lost.");
            EchoSignalRegistry.Unregister(EntityId);
            rescued = true;
            Destroy(gameObject);
        }

        private void RegisterSignalSummary()
        {
            if (signalRecord == null || rescued)
                return;

            EchoSignalRegistry.Register(new EchoSignalSummary
            {
                EntityId = EntityId,
                DisplayName = signalRecord.displayName,
                ClassLabel = SkilledPioneerClassUtility.ToDisplayName(signalRecord.pioneerClass),
                Disposition = signalRecord.Disposition,
                SignalStrength = 1f - signalRecord.saturation,
                WorldPosition = transform.position
            });
        }

        private void EnsureHostileGuardianIfNeeded()
        {
            if (!NeedsSync() || hostileGuardian != null)
                return;

            if (hostileGuardianPrefab != null)
            {
                hostileGuardian = Instantiate(hostileGuardianPrefab, transform.position, transform.rotation, transform);
            }
            else
            {
                hostileGuardian = CreateRuntimeHostileGuardian();
            }
        }

        private GameObject CreateRuntimeHostileGuardian()
        {
            GameObject guardian = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            guardian.name = "HostileEchoGuardian";
            guardian.transform.SetParent(transform, false);
            guardian.transform.localPosition = Vector3.up * 0.5f;
            guardian.transform.localScale = new Vector3(0.7f, 1.1f, 0.7f);

            Collider col = guardian.GetComponent<Collider>();
            if (col != null)
                Destroy(col);

            guardian.AddComponent<EnemySenses>();
            guardian.AddComponent<EnemyHealth>();
            guardian.AddComponent<EnemyCombat>();
            guardian.AddComponent<EnemyAiController>();
            return guardian;
        }

        private void DestroyHostileGuardian()
        {
            if (hostileGuardian == null)
                return;

            Destroy(hostileGuardian);
            hostileGuardian = null;
        }

        private static bool HasInfiltratorInTrio()
        {
            PioneerRosterManager roster = PioneerRosterManager.Instance;
            if (roster == null)
                return false;

            IReadOnlyList<SkilledPioneerRecord> trio = roster.GetExpeditionTrioRecords();
            for (int i = 0; i < trio.Count; i++)
            {
                if (trio[i]?.pioneerClass == SkilledPioneerClass.InfiltratorScout)
                    return true;
            }

            return false;
        }

        private static SkilledPioneerRecord CloneRecord(SkilledPioneerRecord source)
        {
            if (source == null)
                return null;

            return new SkilledPioneerRecord
            {
                id = string.IsNullOrEmpty(source.id) ? Guid.NewGuid().ToString("N") : source.id,
                displayName = source.displayName,
                pioneerClass = source.pioneerClass,
                level = source.level,
                radiationResistance = source.radiationResistance,
                expeditionEfficiency = source.expeditionEfficiency,
                combatSynergy = source.combatSynergy,
                backstory = source.backstory,
                isStarterPick = source.isStarterPick,
                kind = source.kind,
                disposition = source.disposition,
                saturation = source.saturation,
                traitIds = source.traitIds,
                passiveAbilityIds = source.passiveAbilityIds,
                learnedSkills = source.learnedSkills,
                isInExpeditionTrio = false,
                workState = source.workState
            };
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.75f, 0.18f, 0.48f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
