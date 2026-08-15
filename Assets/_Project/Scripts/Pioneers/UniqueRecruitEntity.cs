using System;
using Project.Companions;
using Project.Core;
using Project.Interaction;
using Project.UI;
using UnityEngine;

namespace Project.Pioneers
{
    /// <summary>
    /// World interactable for a CompanionOrigin.Other unique character (alien / AI bot / hybrid /
    /// unclassified) — the non-Echo counterpart to Project.Echoes.EchoWorldEntity. No sync/rescue
    /// minigame: talk to them via the existing QuestGiverDialogUI popup, then ask them to join the
    /// colony directly. Can be seeded from a specific NamedPioneerDefinition (baked by the Companion
    /// Prefab Tool into Resources/Recruits) or left empty to procedurally roll a brand new character
    /// via UniqueCompanionGenerator the first time it's encountered — no network calls, so every
    /// platform gets a unique roster without any external dependency.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class UniqueRecruitEntity : MonoBehaviour, IWorldUsable
    {
        private const float UsePriorityBase = 110f;

        [Header("Identity")]
        [SerializeField] private NamedPioneerDefinition definition;
        [SerializeField] private float interactRange = 4f;

        private SkilledPioneerRecord record;
        private bool recruited;

        public string EntityId => record != null ? record.id : string.Empty;
        public SkilledPioneerRecord Record => record;
        public bool IsInteractable => !recruited;
        public float InteractRange => interactRange;
        public NamedPioneerDefinition Definition => definition;

        public void SetDefinition(NamedPioneerDefinition value)
        {
            definition = value;
        }

        private void Awake()
        {
            Collider interactCollider = GetComponent<Collider>();
            if (interactCollider != null)
                interactCollider.isTrigger = true;
        }

        private void OnEnable()
        {
            WorldUseController.Register(this);
            EnsureRecord();
            ConfigureWorldAmbient();
        }

        private void ConfigureWorldAmbient()
        {
            if (definition == null)
                return;

            CompanionWorldAmbientBehavior ambient = GetComponent<CompanionWorldAmbientBehavior>();
            if (ambient != null)
                ambient.Configure(definition);
        }

        private void OnDisable()
        {
            WorldUseController.Unregister(this);
        }

        /// <summary>Explicitly seed this entity with a record (e.g. from a save game or a scripted
        /// story trigger) instead of letting it resolve one from its definition/generator.</summary>
        public void Initialize(SkilledPioneerRecord seedRecord)
        {
            record = seedRecord;
        }

        private void EnsureRecord()
        {
            if (record != null)
                return;

            record = definition != null
                ? SkilledPioneerRecord.CreateFromCatalog(definition, applyLoadoutDefaults: false)
                : BuildFromGenerated(UniqueCompanionGenerator.Generate());
        }

        private static SkilledPioneerRecord BuildFromGenerated(UniqueCompanionGenerator.GeneratedCompanion generated)
        {
            return new SkilledPioneerRecord
            {
                id = Guid.NewGuid().ToString("N"),
                displayName = generated.displayName,
                pioneerClass = generated.pioneerClass,
                level = 1,
                radiationResistance = generated.radiationResistance,
                expeditionEfficiency = generated.expeditionEfficiency,
                combatSynergy = generated.combatSynergy,
                backstory = generated.backstory,
                Kind = PioneerKind.NamedCatalog,
                Disposition = EchoDisposition.Synced,
                saturation = 0.2f,
                traitIds = generated.traitIds,
                passiveAbilityIds = generated.passiveAbilityIds,
                learnedSkills = generated.learnedSkills,
                buffs = generated.buff != null
                    ? new[] { generated.buff }
                    : Array.Empty<CompanionBuffModifier>(),
                weaponItemId = generated.preferredWeaponItemId,
                toolItemId = generated.preferredToolItemId,
                WorkState = PioneerWorkState.Idle
            };
        }

        public float GetUsePriority(WorldUseContext context)
        {
            if (!GameSession.HasStarted || recruited || record == null)
                return -1f;

            float distance = Vector3.Distance(context.PlayerPosition, transform.position);
            if (distance > Mathf.Max(interactRange, context.UseRange))
                return -1f;

            return UsePriorityBase + (interactRange - distance);
        }

        public bool TryUse(WorldUseContext context)
        {
            if (!GameSession.HasStarted || recruited || record == null)
                return false;

            string message = definition != null && !string.IsNullOrWhiteSpace(definition.recruitmentPitch)
                ? definition.recruitmentPitch
                : (!string.IsNullOrWhiteSpace(record.backstory) ? record.backstory : "...");

            QuestGiverDialogUI.Show(record.displayName, message, TryRecruit, "Ask to Join the Colony", npcAnchor: transform);
            return true;
        }

        private void TryRecruit()
        {
            if (recruited || record == null)
                return;

            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            if (!roster.TryRecruitFromWorld(record, out string message, out _))
            {
                PickupToastUI.Show(string.IsNullOrWhiteSpace(message) ? "They can't join right now." : message);
                return;
            }

            recruited = true;
            PickupToastUI.Show(string.IsNullOrWhiteSpace(message)
                ? $"{record.displayName} has joined the colony."
                : message);
            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 0.85f, 0.65f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
