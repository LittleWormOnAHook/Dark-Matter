using System.Collections.Generic;
using Project.Core;
using Project.Pioneers;
using UnityEngine;

namespace Project.Companions
{
    /// <summary>
    /// Spawns and despawns expedition trio companions when the roster trio changes.
    /// </summary>
    public class CompanionRosterBridge : MonoBehaviour
    {
        [SerializeField] private PioneerCompanionAgent companionPrefab;
        [SerializeField] private Transform companionRoot;

        private readonly List<PioneerCompanionAgent> activeCompanions = new List<PioneerCompanionAgent>(PioneerRosterManager.ExpeditionTrioSize);
        private PioneerRosterManager roster;
        private Transform playerTransform;

        public IReadOnlyList<PioneerCompanionAgent> ActiveCompanions => activeCompanions;

        private void Awake()
        {
            EnsureDefaultPrefab();
        }

        private void OnEnable()
        {
            roster = PioneerRosterManager.EnsureExists();
            roster.OnTrioChanged += HandleTrioChanged;
            roster.OnRosterChanged += HandleRosterChanged;
        }

        private void OnDisable()
        {
            if (roster != null)
            {
                roster.OnTrioChanged -= HandleTrioChanged;
                roster.OnRosterChanged -= HandleRosterChanged;
            }

            ClearCompanions();
        }

        private void Start()
        {
            ResolvePlayer();
            RefreshCompanions();
        }

        public void SetDefaultPrefab(PioneerCompanionAgent prefab)
        {
            if (prefab != null)
                companionPrefab = prefab;
        }

        public void RefreshCompanions()
        {
            EnsureDefaultPrefab();
            ResolvePlayer();
            ClearCompanions();

            if (roster == null)
                roster = PioneerRosterManager.EnsureExists();

            if (playerTransform == null || companionPrefab == null)
                return;

            IReadOnlyList<SkilledPioneerRecord> trio = roster.GetExpeditionTrioRecords();
            for (int slot = 0; slot < PioneerRosterManager.ExpeditionTrioSize; slot++)
            {
                SkilledPioneerRecord record = slot < trio.Count ? trio[slot] : null;
                if (record == null || record.WorkState == PioneerWorkState.Injured)
                    continue;

                PioneerCompanionAgent agent = SpawnCompanion(record, slot);
                if (agent != null)
                    activeCompanions.Add(agent);
            }
        }

        private void HandleTrioChanged()
        {
            RefreshCompanions();
        }

        private void HandleRosterChanged()
        {
            if (roster == null)
                return;

            for (int i = 0; i < activeCompanions.Count; i++)
            {
                PioneerCompanionAgent agent = activeCompanions[i];
                if (agent == null)
                    continue;

                SkilledPioneerRecord record = roster.FindSkilledById(agent.PioneerRecordId);
                if (record != null)
                    agent.RefreshLoadout(record);
            }
        }

        private PioneerCompanionAgent SpawnCompanion(SkilledPioneerRecord record, int slotIndex)
        {
            PioneerCompanionAgent agent = Instantiate(companionPrefab, GetSpawnRoot());
            agent.transform.position = ComputeSpawnPosition(slotIndex);
            agent.BindRecord(record, playerTransform, slotIndex);
            return agent;
        }

        private void EnsureDefaultPrefab()
        {
            if (companionPrefab == null)
                companionPrefab = PioneerCompanionDefaults.LoadDefaultAgentPrefab();
        }

        private Transform GetSpawnRoot()
        {
            if (companionRoot != null)
                return companionRoot;

            return transform;
        }

        private Vector3 ComputeSpawnPosition(int slotIndex)
        {
            if (playerTransform == null)
                return transform.position;

            return CompanionFollowController.GetFormationPosition(playerTransform, slotIndex);
        }

        private void ClearCompanions()
        {
            for (int i = activeCompanions.Count - 1; i >= 0; i--)
            {
                PioneerCompanionAgent agent = activeCompanions[i];
                if (agent != null)
                    Destroy(agent.gameObject);
            }

            activeCompanions.Clear();
        }

        private void ResolvePlayer()
        {
            if (playerTransform != null)
                return;

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player != null)
                playerTransform = player.transform;
        }

        public void SetAllFollow()
        {
            PioneerRosterManager rosterManager = roster ?? PioneerRosterManager.EnsureExists();

            for (int i = 0; i < activeCompanions.Count; i++)
            {
                PioneerCompanionAgent agent = activeCompanions[i];
                if (agent == null)
                    continue;

                agent.ReleaseHold();
                agent.SetFollowMode(PioneerFollowMode.FollowPlayer);
                PersistFollowMode(rosterManager, agent.PioneerRecordId, PioneerFollowMode.FollowPlayer);
            }
        }

        public void SetAllHold(Vector3 worldPoint, float facingYaw)
        {
            PioneerRosterManager rosterManager = roster ?? PioneerRosterManager.EnsureExists();

            for (int i = 0; i < activeCompanions.Count; i++)
            {
                PioneerCompanionAgent agent = activeCompanions[i];
                if (agent == null)
                    continue;

                agent.SetHold(worldPoint, facingYaw);
                PersistFollowMode(rosterManager, agent.PioneerRecordId, agent.FollowMode);
            }
        }

        public void SetCompanionFollowMode(string pioneerRecordId, PioneerFollowMode mode)
        {
            PioneerCompanionAgent agent = FindAgent(pioneerRecordId);
            if (agent == null)
                return;

            agent.SetFollowMode(mode);
            agent.ReleaseHold();
            PioneerRosterManager rosterManager = roster ?? PioneerRosterManager.EnsureExists();
            PersistFollowMode(rosterManager, pioneerRecordId, mode);
        }

        public void SetCompanionHold(string pioneerRecordId, Vector3 worldPoint, float facingYaw)
        {
            PioneerCompanionAgent agent = FindAgent(pioneerRecordId);
            if (agent == null)
                return;

            agent.SetHold(worldPoint, facingYaw);
        }

        private PioneerCompanionAgent FindAgent(string pioneerRecordId)
        {
            if (string.IsNullOrEmpty(pioneerRecordId))
                return null;

            for (int i = 0; i < activeCompanions.Count; i++)
            {
                PioneerCompanionAgent agent = activeCompanions[i];
                if (agent != null && agent.PioneerRecordId == pioneerRecordId)
                    return agent;
            }

            return null;
        }

        private static void PersistFollowMode(
            PioneerRosterManager rosterManager,
            string pioneerRecordId,
            PioneerFollowMode mode)
        {
            if (rosterManager == null || string.IsNullOrEmpty(pioneerRecordId))
                return;

            SkilledPioneerRecord record = rosterManager.FindSkilledById(pioneerRecordId);
            if (record == null)
                return;

            record.followMode = (int)mode;
        }
    }
}
