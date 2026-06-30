using System;
using System.Collections.Generic;
using Project.Building;
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
        public const float DefaultPiWalletBalance = 100f;
        public const float PiToAcSwapRate = 1f;
        public const float InjuryRecoveryDuration = 60f;

        public static PioneerRosterManager Instance { get; private set; }

        [SerializeField] private float aetherCredits;
        [SerializeField] private float piWalletBalance;
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
        public float PiWalletBalance => piWalletBalance;
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
            record.injuryRecoveryRemaining = InjuryRecoveryDuration;
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

                record.injuryRecoveryRemaining = Mathf.Max(0f, record.injuryRecoveryRemaining - deltaTime);
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
            if (piWalletBalance <= 0.01f)
                piWalletBalance = DefaultPiWalletBalance;

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
            starterPioneerSelected = false;
            aetherCredits = StarterPioneerCatalog.StarterAcGrant;
            EnsureWalletBootstrapped();
            ImportWalletPioneersToSkilledRoster();
            PushCurrencyToUi();
            NotifyRosterChanged();
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

        public bool TryAddRescuedEcho(SkilledPioneerRecord record, out string message)
        {
            if (record != null)
            {
                record.Kind = PioneerKind.RescuedEcho;
                if (record.Disposition == EchoDisposition.HostileUntilSynced)
                    record.Disposition = EchoDisposition.Synced;
            }

            bool added = TryAddSkilledPioneer(record, out message);
            if (added && record != null)
                AppendEchoChronicle(EchoChronicleEntry.CreateSuccess(record));
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

        public void SetPiWalletBalance(float balance)
        {
            piWalletBalance = Mathf.Max(0f, balance);
            PushCurrencyToUi();
        }

        public bool TrySwapPiForAetherCredits(int piAmount, out string message)
        {
            message = string.Empty;
            if (piAmount <= 0)
            {
                message = "Enter a Pi amount greater than zero.";
                return false;
            }

            if (piWalletBalance + 0.01f < piAmount)
            {
                message = "Not enough Pi in wallet.";
                return false;
            }

            int acGain = Mathf.RoundToInt(piAmount * PiToAcSwapRate);
            piWalletBalance -= piAmount;
            aetherCredits += acGain;
            PushCurrencyToUi();
            NotifyRosterChanged();
            message = $"Swapped {piAmount} Pi → {acGain} AC.";
            return true;
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
            piWalletBalance = savedPiWalletBalance > 0.01f
                ? Mathf.Max(0f, savedPiWalletBalance)
                : DefaultPiWalletBalance;
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
                behavior = source.behavior != null ? source.behavior.Clone() : null
            };
        }

        public void ApplyLegacyPiBalanceMigration(float legacyPiBalance)
        {
            if (legacyPiBalance <= 0f)
                return;

            aetherCredits = Mathf.Max(aetherCredits, legacyPiBalance);
            PushCurrencyToUi();
        }

        private void PushCurrencyToUi()
        {
            UIManager ui = FindAnyObjectByType<UIManager>();
            if (ui != null)
            {
                ui.SetAetherCredits(aetherCredits);
                ui.SetPiWalletBalance(piWalletBalance);
            }

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
