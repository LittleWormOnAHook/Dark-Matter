using System;
using System.Collections.Generic;
using Project.Building;
using Project.Companions.Abilities;
using Project.Core;
using Project.Managers;
using Project.UI;
using UnityEngine;

namespace Project.Pioneers
{
    public class PioneerRosterManager : MonoBehaviour
    {
        public const int MaxTotalPioneers = 25;
        public const int MaxSkilledPioneers = 13;
        public const int MaxWorkerPioneers = 13;
        public const int ExpeditionTrioSize = 3;
        public const float InjuryRecoveryDuration = 60f;

        public static PioneerRosterManager Instance { get; private set; }

        [SerializeField] private float aetherCredits;
        [SerializeField] private int workerCount;
        [SerializeField] private bool starterPioneerSelected;
        [SerializeField] private bool walletBootstrapped;
        [SerializeField] private int colonistInjuredCount;
        [SerializeField] private int colonistShelteredCount;
        [SerializeField] private int colonistAssignedCount;

        private readonly List<SkilledPioneerRecord> skilledPioneers = new List<SkilledPioneerRecord>();
        private readonly List<SkilledPioneerRecord> walletOwnedPioneers = new List<SkilledPioneerRecord>();
        private readonly List<string> expeditionTrioIds = new List<string>(ExpeditionTrioSize);
        private readonly List<EchoChronicleEntry> echoChronicle = new List<EchoChronicleEntry>();

        public event Action OnRosterChanged;
        public event Action OnCurrencyChanged;
        public event Action OnTrioChanged;
        public event Action OnEchoChronicleChanged;

        public float AetherCredits => aetherCredits;
        public int WorkerCount => workerCount;
        public bool StarterPioneerSelected => starterPioneerSelected;
        public IReadOnlyList<SkilledPioneerRecord> SkilledPioneers => skilledPioneers;
        public IReadOnlyList<SkilledPioneerRecord> WalletOwnedPioneers => walletOwnedPioneers;
        public IReadOnlyList<string> ExpeditionTrioIds => expeditionTrioIds;
        public IReadOnlyList<EchoChronicleEntry> EchoChronicle => echoChronicle;

        public static PioneerRosterManager EnsureExists()
        {
            if (Instance != null)
                return Instance;

            PioneerRosterManager found = FindAnyObjectByType<PioneerRosterManager>();
            if (found != null)
                return found;

            SimpleGameManager gameManager = FindAnyObjectByType<SimpleGameManager>();
            if (gameManager != null)
                return gameManager.GetComponent<PioneerRosterManager>()
                    ?? gameManager.gameObject.AddComponent<PioneerRosterManager>();

            UIManager uiManager = FindAnyObjectByType<UIManager>();
            if (uiManager != null)
                return uiManager.GetComponent<PioneerRosterManager>()
                    ?? uiManager.gameObject.AddComponent<PioneerRosterManager>();

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player != null)
                return player.GetComponent<PioneerRosterManager>()
                    ?? player.AddComponent<PioneerRosterManager>();

            return null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            EnsureWalletBootstrapped();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (!GameSession.HasStarted)
                return;

            TickInjuryRecovery(Time.deltaTime);
        }

        public float GetInjuryRecoveryRemaining(SkilledPioneerRecord record)
        {
            if (record == null || record.WorkState != PioneerWorkState.Injured)
                return 0f;

            return Mathf.Max(0f, record.injuryRecoveryRemaining);
        }

        public bool TryMarkSkilledInjured(string pioneerId, out SkilledPioneerRecord record)
        {
            record = FindSkilledById(pioneerId);
            if (record == null)
                return false;

            EnsureTrioSlotsSized();
            for (int i = 0; i < ExpeditionTrioSize; i++)
            {
                if (expeditionTrioIds[i] == pioneerId)
                    expeditionTrioIds[i] = string.Empty;
            }

            record.isInExpeditionTrio = false;
            record.WorkState = PioneerWorkState.Injured;
            float recoveryDuration = InjuryRecoveryDuration;
            if (MedTechCompanionAbilityController.ExpeditionHasInjuryStabilize())
                recoveryDuration *= MedTechCompanionAbilityController.InjuryRecoveryMultiplier;

            record.injuryRecoveryRemaining = recoveryDuration;
            NotifyRosterChanged();
            NotifyTrioChanged();
            return true;
        }

        public List<SkilledPioneerRecord> GetInjuredSkilledPioneers()
        {
            List<SkilledPioneerRecord> injured = new List<SkilledPioneerRecord>();
            for (int i = 0; i < skilledPioneers.Count; i++)
            {
                SkilledPioneerRecord record = skilledPioneers[i];
                if (record != null && record.WorkState == PioneerWorkState.Injured)
                    injured.Add(record);
            }

            return injured;
        }

        public bool TryRecoverSkilledFromLab(string pioneerId, out string message)
        {
            message = string.Empty;
            SkilledPioneerRecord record = FindSkilledById(pioneerId);
            if (record == null)
            {
                message = "Pioneer not found.";
                return false;
            }

            if (record.WorkState != PioneerWorkState.Injured)
            {
                message = $"{record.displayName} is not injured.";
                return false;
            }

            if (record.injuryRecoveryRemaining > 0.5f)
            {
                message = $"{record.displayName} is still recovering ({Mathf.CeilToInt(record.injuryRecoveryRemaining)}s).";
                return false;
            }

            record.WorkState = PioneerWorkState.Idle;
            record.injuryRecoveryRemaining = 0f;
            NotifyRosterChanged();
            NotifyTrioChanged();
            message = $"{record.displayName} rejoined your expedition.";
            return true;
        }

        private void TickInjuryRecovery(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            bool changed = false;
            for (int i = 0; i < skilledPioneers.Count; i++)
            {
                SkilledPioneerRecord record = skilledPioneers[i];
                if (record == null || record.WorkState != PioneerWorkState.Injured)
                    continue;

                if (record.injuryRecoveryRemaining <= 0f)
                    continue;

                float recoveryDelta = deltaTime;
                if (MedTechCompanionAbilityController.RosterHasInjuryStabilize(this))
                    recoveryDelta *= MedTechCompanionAbilityController.BaseInjuryRecoverySpeedBonus;

                record.injuryRecoveryRemaining = Mathf.Max(0f, record.injuryRecoveryRemaining - recoveryDelta);
                changed = true;
            }

            if (changed)
                NotifyRosterChanged();
        }

        public void EnsureWalletBootstrapped()
        {
            if (walletBootstrapped)
                return;

            walletBootstrapped = true;

            if (walletOwnedPioneers.Count == 0)
            {
                walletOwnedPioneers.Add(WalletMarketplaceCatalog.CreateMockOwned(
                    "Signal Ghost Mira-1",
                    SkilledPioneerClass.InfiltratorScout,
                    level: 2));
            }

            PushCurrencyToUi();
            NotifyRosterChanged();
        }

        public void PrepareNewGameSession()
        {
            skilledPioneers.Clear();
            expeditionTrioIds.Clear();
            echoChronicle.Clear();
            workerCount = 3;
            colonistInjuredCount = 0;
            colonistShelteredCount = 0;
            colonistAssignedCount = 0;
            starterPioneerSelected = true;
            aetherCredits = StarterPioneerCatalog.StarterAcGrant;
            EnsureWalletBootstrapped();
            GrantAllCatalogPioneersToSkilledRoster();
            ImportWalletPioneersToSkilledRoster();
            EnsureDefaultTrioIfNeeded();
            PushCurrencyToUi();
            NotifyRosterChanged();
        }

        private void GrantAllCatalogPioneersToSkilledRoster()
        {
            // Only Echo and Expedition-origin companions are present from the start. Support Ship
            // companions join later via GrantSupportShipCompanion (a story/quest trigger), and Other-
            // origin unique characters (aliens/AI bots/hybrids) are met and recruited directly out in
            // the world via UniqueRecruitEntity — neither should be handed to the player for free.
            IReadOnlyList<NamedPioneerDefinition> definitions = NamedPioneerCatalog.GetAllDefinitions();
            for (int i = 0; i < definitions.Count; i++)
            {
                NamedPioneerDefinition definition = definitions[i];
                if (definition == null
                    || definition.origin == CompanionOrigin.SupportShip
                    || definition.origin == CompanionOrigin.Other)
                    continue;

                TryGrantCatalogPioneer(definition);
            }
        }

        /// <summary>
        /// Grants a single Support Ship-origin companion to the roster — call this from a quest/story
        /// trigger when that delivery event happens (e.g. a supply ship arriving mid-campaign).
        /// Returns false if the pioneerId isn't found, isn't Support Ship-origin, or is already on
        /// the roster.
        /// </summary>
        public bool GrantSupportShipCompanion(string pioneerId)
        {
            NamedPioneerDefinition definition = NamedPioneerCatalog.FindById(pioneerId);
            if (definition == null || definition.origin != CompanionOrigin.SupportShip)
                return false;

            bool granted = TryGrantCatalogPioneer(definition);
            if (granted)
                NotifyRosterChanged();

            return granted;
        }

        private bool TryGrantCatalogPioneer(NamedPioneerDefinition definition)
        {
            if (definition == null)
                return false;

            if (HasSkilledPioneerById(definition.ResolvedId) || HasSkilledPioneerByName(definition.displayName))
                return false;

            if (skilledPioneers.Count >= MaxSkilledPioneers)
                return false;

            SkilledPioneerRecord record = SkilledPioneerRecord.CreateFromCatalog(definition, applyLoadoutDefaults: false);
            if (record == null)
                return false;

            skilledPioneers.Add(record);
            return true;
        }

        private bool HasSkilledPioneerById(string pioneerId)
        {
            return FindSkilledById(pioneerId) != null;
        }

        public bool CanJoinTrio(SkilledPioneerRecord record)
        {
            if (record == null)
                return false;

            if (record.Kind == PioneerKind.ColonistWorker)
                return false;

            if (record.WorkState == PioneerWorkState.Injured)
                return false;

            return record.Kind == PioneerKind.NamedCatalog || record.Kind == PioneerKind.RescuedEcho;
        }

        public SkilledPioneerRecord FindSkilledById(string pioneerId)
        {
            if (string.IsNullOrWhiteSpace(pioneerId))
                return null;

            for (int i = 0; i < skilledPioneers.Count; i++)
            {
                if (skilledPioneers[i].id == pioneerId)
                    return skilledPioneers[i];
            }

            return null;
        }

        public IReadOnlyList<SkilledPioneerRecord> GetExpeditionTrioRecords()
        {
            EnsureTrioSlotsSized();
            List<SkilledPioneerRecord> trio = new List<SkilledPioneerRecord>(ExpeditionTrioSize);
            for (int i = 0; i < ExpeditionTrioSize; i++)
                trio.Add(GetExpeditionTrioRecordAtSlot(i));

            return trio;
        }

        public string GetExpeditionTrioIdAtSlot(int slotIndex)
        {
            EnsureTrioSlotsSized();
            if (slotIndex < 0 || slotIndex >= ExpeditionTrioSize)
                return string.Empty;

            return expeditionTrioIds[slotIndex] ?? string.Empty;
        }

        public SkilledPioneerRecord GetExpeditionTrioRecordAtSlot(int slotIndex)
        {
            string id = GetExpeditionTrioIdAtSlot(slotIndex);
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return FindSkilledById(id);
        }

        public int GetActiveExpeditionTrioCount()
        {
            EnsureTrioSlotsSized();
            int count = 0;
            for (int i = 0; i < ExpeditionTrioSize; i++)
            {
                if (!string.IsNullOrWhiteSpace(expeditionTrioIds[i]))
                    count++;
            }

            return count;
        }

        /// <summary>Raises level on active expedition trio members only (not benched/camp pioneers).</summary>
        public void IncrementExpeditionTrioLevels(int levelsGained)
        {
            if (levelsGained <= 0)
                return;

            bool changed = false;
            for (int i = 0; i < ExpeditionTrioSize; i++)
            {
                SkilledPioneerRecord record = GetExpeditionTrioRecordAtSlot(i);
                if (record == null)
                    continue;

                record.level += levelsGained;
                changed = true;
            }

            if (changed)
                NotifyRosterChanged();
        }

        public bool TrySetExpeditionTrio(IReadOnlyList<string> skilledIds, out string error)
        {
            error = string.Empty;
            if (skilledIds == null || skilledIds.Count != ExpeditionTrioSize)
            {
                error = $"Provide {ExpeditionTrioSize} trio slot entries (empty slots allowed).";
                return false;
            }

            HashSet<string> unique = new HashSet<string>();
            for (int i = 0; i < skilledIds.Count; i++)
            {
                string id = skilledIds[i];
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                if (!unique.Add(id))
                {
                    error = "Each trio slot must be a unique pioneer.";
                    return false;
                }

                SkilledPioneerRecord record = FindSkilledById(id);
                if (record == null)
                {
                    error = "One or more selected pioneers are not on the skilled roster.";
                    return false;
                }

                if (!CanJoinTrio(record))
                {
                    error = $"{record.displayName} cannot join the expedition trio.";
                    return false;
                }
            }

            ClearTrioFlags();
            expeditionTrioIds.Clear();
            for (int i = 0; i < skilledIds.Count; i++)
            {
                string id = skilledIds[i] ?? string.Empty;
                expeditionTrioIds.Add(id);
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                SkilledPioneerRecord record = FindSkilledById(id);
                if (record != null)
                    record.isInExpeditionTrio = true;

                // Joining the expedition trio means leaving whatever building job they were
                // stationed at — can't work a facility and be out in the field at the same time.
                BuildingOperationRegistry.UnassignPioneerFromAllBuildings(id);
            }

            NotifyTrioChanged();
            NotifyRosterChanged();
            return true;
        }

        public bool TrySetPioneerLoadout(
            string pioneerId,
            string weaponItemId,
            string toolItemId,
            string[] assignedSkillIds,
            out string error)
        {
            error = string.Empty;
            SkilledPioneerRecord record = FindSkilledById(pioneerId);
            if (record == null)
            {
                error = "Pioneer not found.";
                return false;
            }

            record.weaponItemId = weaponItemId ?? string.Empty;
            record.toolItemId = toolItemId ?? string.Empty;
            record.assignedSkillIds = assignedSkillIds ?? System.Array.Empty<string>();
            PioneerLoadoutDefaults.EnsureDefaults(record);
            NotifyRosterChanged();
            return true;
        }

        public bool TryAssignTrioSlot(int slotIndex, string skilledId, out string error)
        {
            error = string.Empty;
            if (slotIndex < 0 || slotIndex >= ExpeditionTrioSize)
            {
                error = "Invalid trio slot.";
                return false;
            }

            while (expeditionTrioIds.Count < ExpeditionTrioSize)
                expeditionTrioIds.Add(string.Empty);

            List<string> next = new List<string>(ExpeditionTrioSize);
            for (int i = 0; i < ExpeditionTrioSize; i++)
                next.Add(i < expeditionTrioIds.Count ? expeditionTrioIds[i] : string.Empty);

            next[slotIndex] = skilledId ?? string.Empty;
            return TrySetExpeditionTrio(next, out error);
        }

        public ColonistAggregateState GetColonistState()
        {
            return new ColonistAggregateState
            {
                workerCount = workerCount,
                injuredCount = colonistInjuredCount,
                shelteredCount = colonistShelteredCount,
                assignedToFacilityCount = colonistAssignedCount
            };
        }

        public void SetColonistAggregate(ColonistAggregateState state)
        {
            if (state == null)
                return;

            workerCount = Mathf.Clamp(state.workerCount, 0, MaxWorkerPioneers);
            colonistInjuredCount = Mathf.Clamp(state.injuredCount, 0, workerCount);
            colonistShelteredCount = Mathf.Clamp(state.shelteredCount, 0, workerCount);
            colonistAssignedCount = Mathf.Clamp(state.assignedToFacilityCount, 0, workerCount);
            NotifyRosterChanged();
        }

        public void AppendEchoChronicle(EchoChronicleEntry entry)
        {
            if (entry == null)
                return;

            echoChronicle.Insert(0, entry);
            OnEchoChronicleChanged?.Invoke();
            NotifyRosterChanged();
        }

        public void AppendSimulationChronicle(string incidentId, float severity01, string debugReason = "")
        {
            AppendEchoChronicle(
                EchoChronicleEntry.CreateSimulationIncident(incidentId, severity01, debugReason));
        }

        public bool TryAddSkilledPioneer(SkilledPioneerRecord record, out string message)
        {
            message = string.Empty;
            if (record == null)
            {
                message = "Invalid pioneer record.";
                return false;
            }

            if (HasSkilledPioneerByName(record.displayName))
            {
                message = "This pioneer is already on your base roster.";
                return false;
            }

            if (skilledPioneers.Count >= MaxSkilledPioneers)
            {
                message = "Skilled pioneer roster is full.";
                return false;
            }

            skilledPioneers.Add(CloneRecord(record));
            PioneerLoadoutDefaults.EnsureDefaults(skilledPioneers[skilledPioneers.Count - 1]);
            EnsureDefaultTrioIfNeeded();
            NotifyRosterChanged();
            return true;
        }

        /// <summary>
        /// Adds a pioneer from a world conversation or echo rescue. Fills the first open expedition
        /// trio slot when available; otherwise they remain on the camp roster (Pioneers tab).
        /// </summary>
        public bool TryRecruitFromWorld(
            SkilledPioneerRecord record,
            out string message,
            out bool joinedExpeditionTrio)
        {
            joinedExpeditionTrio = false;
            message = string.Empty;

            if (record == null)
            {
                message = "Invalid pioneer record.";
                return false;
            }

            string recruitName = record.displayName;
            if (!TryAddSkilledPioneer(record, out message))
                return false;

            SkilledPioneerRecord rosterRecord = FindSkilledByName(recruitName);
            if (rosterRecord == null)
                rosterRecord = skilledPioneers[skilledPioneers.Count - 1];

            int emptySlot = FindFirstEmptyTrioSlotIndex();
            if (emptySlot >= 0 && CanJoinTrio(rosterRecord))
            {
                if (TryAssignTrioSlot(emptySlot, rosterRecord.id, out _))
                {
                    joinedExpeditionTrio = true;
                    message = $"{rosterRecord.displayName} joined your expedition trio.";
                    return true;
                }
            }

            message = $"{rosterRecord.displayName} joined the camp roster. Assign them in the Pioneers tab.";
            return true;
        }

        private int FindFirstEmptyTrioSlotIndex()
        {
            EnsureTrioSlotsSized();
            for (int i = 0; i < ExpeditionTrioSize; i++)
            {
                if (string.IsNullOrWhiteSpace(expeditionTrioIds[i]))
                    return i;
            }

            return -1;
        }

        private SkilledPioneerRecord FindSkilledByName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return null;

            for (int i = 0; i < skilledPioneers.Count; i++)
            {
                if (skilledPioneers[i].displayName == displayName)
                    return skilledPioneers[i];
            }

            return null;
        }

        public bool TryAddRescuedEcho(SkilledPioneerRecord record, out string message)
        {
            if (record != null)
            {
                record.Kind = PioneerKind.RescuedEcho;
                if (record.Disposition == EchoDisposition.HostileUntilSynced)
                    record.Disposition = EchoDisposition.Synced;
            }

            bool added = TryRecruitFromWorld(record, out message, out _);
            if (added && record != null)
            {
                SkilledPioneerRecord rosterRecord = FindSkilledById(record.id) ?? FindSkilledByName(record.displayName);
                AppendEchoChronicle(EchoChronicleEntry.CreateSuccess(rosterRecord ?? record));
            }

            return added;
        }

        public void ImportWalletPioneersToSkilledRoster()
        {
            for (int i = 0; i < walletOwnedPioneers.Count; i++)
            {
                SkilledPioneerRecord walletRecord = walletOwnedPioneers[i];
                if (walletRecord == null || IsWalletPrototypeRecord(walletRecord))
                    continue;

                if (HasSkilledPioneerByName(walletRecord.displayName))
                    continue;

                if (skilledPioneers.Count >= MaxSkilledPioneers)
                    break;

                skilledPioneers.Add(CloneRecord(walletRecord));
            }

            EnsureDefaultTrioIfNeeded();
            NotifyRosterChanged();
        }

        public bool TryPurchaseStarterOffer(StarterPioneerOffer offer, out string message)
        {
            message = string.Empty;
            if (offer == null)
            {
                message = "Invalid pioneer offer.";
                return false;
            }

            if (starterPioneerSelected)
            {
                message = "Starter pioneer already selected.";
                return false;
            }

            if (skilledPioneers.Count >= MaxSkilledPioneers)
            {
                message = "Skilled pioneer roster is full.";
                return false;
            }

            if (aetherCredits + 0.01f < offer.acCost)
            {
                message = "Not enough Aether Credits.";
                return false;
            }

            aetherCredits -= offer.acCost;
            skilledPioneers.Add(SkilledPioneerRecord.CreateFromStarter(offer));
            starterPioneerSelected = true;
            EnsureDefaultTrioIfNeeded();
            PushCurrencyToUi();
            NotifyRosterChanged();
            message = $"Recruited {offer.displayName}.";
            return true;
        }

        public bool TrySpendAetherCredits(float amount)
        {
            if (amount <= 0f)
                return true;

            if (aetherCredits + 0.01f < amount)
                return false;

            aetherCredits -= amount;
            PushCurrencyToUi();
            return true;
        }

        public void AddAetherCredits(float amount, string source = "Reward")
        {
            if (amount <= 0f)
                return;

            aetherCredits += amount;
            PushCurrencyToUi();

            UIManager ui = FindAnyObjectByType<UIManager>();
            ui?.ShowAcRewardPopup(Mathf.RoundToInt(amount), source);
        }

        public bool TryPurchaseMarketplaceListing(string offerId, out string message)
        {
            message = string.Empty;
            WalletMarketplaceOffer offer = WalletMarketplaceCatalog.Find(offerId);
            if (offer == null)
            {
                message = "Listing not found.";
                return false;
            }

            if (OwnsMarketplaceListing(offerId))
            {
                message = "You already own this pioneer.";
                return false;
            }

            if (aetherCredits + 0.01f < offer.acListPrice)
            {
                message = $"Need {offer.acListPrice} AC (balance: {Mathf.FloorToInt(aetherCredits)}).";
                return false;
            }

            SkilledPioneerRecord record = WalletMarketplaceCatalog.CreateOwnedFromListing(offer);
            if (record == null)
            {
                message = "Could not create pioneer record.";
                return false;
            }

            aetherCredits -= offer.acListPrice;
            walletOwnedPioneers.Add(record);

            if (!TryAddSkilledPioneer(record, out string rosterMessage))
            {
                message = $"Purchased {offer.displayName} for {offer.acListPrice} AC (wallet only: {rosterMessage}).";
                PushCurrencyToUi();
                NotifyRosterChanged();
                return true;
            }

            PushCurrencyToUi();
            NotifyRosterChanged();
            message = $"Purchased {offer.displayName} for {offer.acListPrice} AC. Added to base roster.";
            return true;
        }

        public bool OwnsMarketplaceListing(string offerId)
        {
            WalletMarketplaceOffer offer = WalletMarketplaceCatalog.Find(offerId);
            if (offer == null)
                return false;

            for (int i = 0; i < walletOwnedPioneers.Count; i++)
            {
                if (walletOwnedPioneers[i].displayName == offer.displayName)
                    return true;
            }

            return false;
        }

        public void SetWorkerCount(int count)
        {
            workerCount = Mathf.Clamp(count, 0, MaxWorkerPioneers);
            NotifyRosterChanged();
        }

        public int GetTotalPioneerCount()
        {
            return workerCount + skilledPioneers.Count;
        }

        public string[] BuildExpeditionTrioSave()
        {
            return expeditionTrioIds.ToArray();
        }

        public EchoChronicleEntry[] BuildEchoChronicleSave()
        {
            return echoChronicle.ToArray();
        }

        public ColonistAggregateSaveRecord BuildColonistAggregateSave()
        {
            return ColonistAggregateSaveRecord.FromRuntime(GetColonistState());
        }

        public SkilledPioneerSaveRecord[] BuildSaveRecords()
        {
            SkilledPioneerSaveRecord[] records = new SkilledPioneerSaveRecord[skilledPioneers.Count];
            for (int i = 0; i < skilledPioneers.Count; i++)
                records[i] = SkilledPioneerSaveRecord.FromRuntime(skilledPioneers[i]);
            return records;
        }

        public void ApplySave(
            float savedAetherCredits,
            float savedPiWalletBalance,
            int savedWorkerCount,
            bool savedStarterSelected,
            SkilledPioneerSaveRecord[] savedSkilled)
        {
            ApplySaveV11(
                savedAetherCredits,
                savedPiWalletBalance,
                savedWorkerCount,
                savedStarterSelected,
                savedSkilled,
                null,
                null,
                null);
        }

        public void ApplySaveV11(
            float savedAetherCredits,
            float savedPiWalletBalance,
            int savedWorkerCount,
            bool savedStarterSelected,
            SkilledPioneerSaveRecord[] savedSkilled,
            string[] savedTrioIds,
            ColonistAggregateSaveRecord savedColonistAggregate,
            EchoChronicleEntry[] savedChronicle)
        {
            aetherCredits = Mathf.Max(0f, savedAetherCredits);
            ApplyLegacyCurrencyMigration(savedPiWalletBalance);
            workerCount = Mathf.Clamp(savedWorkerCount, 0, MaxWorkerPioneers);
            starterPioneerSelected = savedStarterSelected;
            skilledPioneers.Clear();
            expeditionTrioIds.Clear();
            echoChronicle.Clear();

            if (savedSkilled != null)
            {
                for (int i = 0; i < savedSkilled.Length; i++)
                {
                    if (savedSkilled[i] == null)
                        continue;

                    skilledPioneers.Add(savedSkilled[i].ToRuntime());
                }
            }

            if (savedColonistAggregate != null)
            {
                ColonistAggregateState aggregate = savedColonistAggregate.ToRuntime();
                workerCount = aggregate.workerCount;
                colonistInjuredCount = aggregate.injuredCount;
                colonistShelteredCount = aggregate.shelteredCount;
                colonistAssignedCount = aggregate.assignedToFacilityCount;
            }
            else
            {
                colonistInjuredCount = 0;
                colonistShelteredCount = 0;
                colonistAssignedCount = 0;
            }

            if (savedChronicle != null)
                echoChronicle.AddRange(savedChronicle);

            if (savedTrioIds != null && savedTrioIds.Length == ExpeditionTrioSize)
                TrySetExpeditionTrio(savedTrioIds, out _);
            else
                EnsureDefaultTrioIfNeeded();

            EnsureWalletBootstrapped();
            PushCurrencyToUi();
            NotifyRosterChanged();
        }

        public void SyncColonistAssignedCount(int assignedCount)
        {
            colonistAssignedCount = Mathf.Clamp(assignedCount, 0, workerCount);
            NotifyRosterChanged();
        }

        private void EnsureDefaultTrioIfNeeded()
        {
            while (expeditionTrioIds.Count < ExpeditionTrioSize)
                expeditionTrioIds.Add(string.Empty);
        }

        private void EnsureTrioSlotsSized()
        {
            EnsureDefaultTrioIfNeeded();
        }

        public void EnsureDefaultTrioIfNeededPublic()
        {
            EnsureDefaultTrioIfNeeded();
        }

        private void ClearTrioFlags()
        {
            for (int i = 0; i < skilledPioneers.Count; i++)
                skilledPioneers[i].isInExpeditionTrio = false;
        }

        private bool HasSkilledPioneerByName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return false;

            for (int i = 0; i < skilledPioneers.Count; i++)
            {
                if (skilledPioneers[i].displayName == displayName)
                    return true;
            }

            return false;
        }

        private static bool IsWalletPrototypeRecord(SkilledPioneerRecord record)
        {
            return record != null
                && !string.IsNullOrEmpty(record.backstory)
                && record.backstory.Contains("Mock wallet roster pioneer", StringComparison.Ordinal);
        }

        private static SkilledPioneerRecord CloneRecord(SkilledPioneerRecord source)
        {
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
                weaponItemId = source.weaponItemId,
                toolItemId = source.toolItemId,
                assignedSkillIds = source.assignedSkillIds != null
                    ? (string[])source.assignedSkillIds.Clone()
                    : null,
                isInExpeditionTrio = source.isInExpeditionTrio,
                workState = source.workState,
                injuryRecoveryRemaining = source.injuryRecoveryRemaining,
                followMode = source.followMode,
                worldIdleJob = source.worldIdleJob,
                behavior = source.behavior != null ? source.behavior.Clone() : null
            };
        }

        public void ApplyLegacyAcBalanceMigration(float legacyBalance)
        {
            ApplyLegacyCurrencyMigration(legacyBalance);
        }

        [System.Obsolete("Use ApplyLegacyAcBalanceMigration.")]
        public void ApplyLegacyPiBalanceMigration(float legacyPiBalance)
        {
            ApplyLegacyAcBalanceMigration(legacyPiBalance);
        }

        private void ApplyLegacyCurrencyMigration(float legacyAmount)
        {
            if (legacyAmount <= 0.01f)
                return;

            aetherCredits += legacyAmount;
            PushCurrencyToUi();
        }

        private void PushCurrencyToUi()
        {
            UIManager ui = FindAnyObjectByType<UIManager>();
            ui?.SetAetherCredits(aetherCredits);
            OnCurrencyChanged?.Invoke();
        }

        private void NotifyRosterChanged()
        {
            OnRosterChanged?.Invoke();
        }

        private void NotifyTrioChanged()
        {
            OnTrioChanged?.Invoke();
        }
    }
}
