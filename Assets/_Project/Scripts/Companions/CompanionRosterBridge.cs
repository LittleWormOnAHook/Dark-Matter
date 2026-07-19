using System;
using System.Collections.Generic;
using Project.Core;
using Project.Map;
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
        private readonly List<PioneerCompanionAgent> vehicleHiddenCompanions = new List<PioneerCompanionAgent>(PioneerRosterManager.ExpeditionTrioSize);
        private PioneerRosterManager roster;
        private Transform playerTransform;
        private bool companionsHiddenForVehicle;

        public event Action ActiveCompanionsChanged;

        public IReadOnlyList<PioneerCompanionAgent> ActiveCompanions => activeCompanions;

        /// <summary>
        /// Hides active expedition companions while the player is aboard a vehicle.
        /// </summary>
        public void SetCompanionsHiddenForVehicle(bool hidden)
        {
            if (hidden == companionsHiddenForVehicle)
                return;

            companionsHiddenForVehicle = hidden;

            if (hidden)
            {
                vehicleHiddenCompanions.Clear();
                for (int i = 0; i < activeCompanions.Count; i++)
                {
                    PioneerCompanionAgent agent = activeCompanions[i];
                    if (agent == null || !agent.gameObject.activeSelf)
                        continue;

                    vehicleHiddenCompanions.Add(agent);
                    MapMarker mapMarker = agent.GetComponent<MapMarker>();
                    if (mapMarker != null)
                        mapMarker.SetKeepRegisteredWhenDisabled(true);
                    agent.gameObject.SetActive(false);
                }

                return;
            }

            for (int i = 0; i < vehicleHiddenCompanions.Count; i++)
            {
                PioneerCompanionAgent agent = vehicleHiddenCompanions[i];
                if (agent != null)
                {
                    MapMarker mapMarker = agent.GetComponent<MapMarker>();
                    if (mapMarker != null)
                        mapMarker.SetKeepRegisteredWhenDisabled(false);
                    agent.gameObject.SetActive(true);
                }
            }

            vehicleHiddenCompanions.Clear();
        }

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

            NotifyActiveCompanionsChanged();
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

            NotifyActiveCompanionsChanged();
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
            PioneerCompanionAgent preferred = PioneerCompanionDefaults.LoadDefaultAgentPrefab();
            if (preferred == null)
                return;

            if (companionPrefab == null)
            {
                companionPrefab = preferred;
                return;
            }

            if (PioneerCompanionDefaults.IsInvectorPrefab(preferred) &&
                !PioneerCompanionDefaults.IsInvectorPrefab(companionPrefab))
            {
                companionPrefab = preferred;
            }
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
            NotifyActiveCompanionsChanged();
        }

        private void NotifyActiveCompanionsChanged()
        {
            // Single choke point for every spawn/despawn/loadout-refresh — keeps the shared group
            // buff snapshot (hazard mitigation, combat synergy) in sync with whichever companions
            // are actually in the field right now.
            CompanionGroupBuffService.Recompute(activeCompanions);
            ActiveCompanionsChanged?.Invoke();
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
