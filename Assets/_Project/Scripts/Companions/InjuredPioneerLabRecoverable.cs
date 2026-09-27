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

        /// <summary>
        /// Cheap interactability check for proximity dots (no string allocation).
        /// </summary>
        public bool CanShowInteractionHint()
        {
            PioneerRosterManager roster = PioneerRosterManager.Instance;
            SkilledPioneerRecord record = roster != null ? roster.FindSkilledById(pioneerRecordId) : null;
            return record != null && record.WorkState == PioneerWorkState.Injured;
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
}
