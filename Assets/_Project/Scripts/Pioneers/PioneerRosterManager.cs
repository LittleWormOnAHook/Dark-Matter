using System;
using System.Collections.Generic;
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

        public static PioneerRosterManager Instance { get; private set; }

        [SerializeField] private float aetherCredits;
        [SerializeField] private float piWalletBalance;
        [SerializeField] private int workerCount;
        [SerializeField] private bool starterPioneerSelected;
        [SerializeField] private bool walletBootstrapped;

        private readonly List<SkilledPioneerRecord> skilledPioneers = new List<SkilledPioneerRecord>();
        private readonly List<SkilledPioneerRecord> walletOwnedPioneers = new List<SkilledPioneerRecord>();

        public event Action OnRosterChanged;
        public event Action OnCurrencyChanged;

        public float AetherCredits => aetherCredits;
        public float PiWalletBalance => piWalletBalance;
        public int WorkerCount => workerCount;
        public bool StarterPioneerSelected => starterPioneerSelected;
        public IReadOnlyList<SkilledPioneerRecord> SkilledPioneers => skilledPioneers;
        public IReadOnlyList<SkilledPioneerRecord> WalletOwnedPioneers => walletOwnedPioneers;

        public static PioneerRosterManager EnsureExists()
        {
            if (Instance != null)
                return Instance;

            SimpleGameManager gameManager = FindAnyObjectByType<SimpleGameManager>();
            if (gameManager != null)
            {
                Instance = gameManager.GetComponent<PioneerRosterManager>();
                if (Instance == null)
                    Instance = gameManager.gameObject.AddComponent<PioneerRosterManager>();
                return Instance;
            }

            GameObject host = new GameObject("PioneerRosterManager");
            DontDestroyOnLoad(host);
            Instance = host.AddComponent<PioneerRosterManager>();
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
            EnsureWalletBootstrapped();
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
            workerCount = 0;
            starterPioneerSelected = false;
            aetherCredits = StarterPioneerCatalog.StarterAcGrant;
            EnsureWalletBootstrapped();
            PushCurrencyToUi();
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

            for (int i = 0; i < walletOwnedPioneers.Count; i++)
            {
                if (walletOwnedPioneers[i].displayName == offer.displayName)
                {
                    message = "You already own this pioneer.";
                    return false;
                }
            }

            if (piWalletBalance + 0.01f < offer.piListPrice)
            {
                message = $"Need {offer.piListPrice} Pi (wallet: {Mathf.FloorToInt(piWalletBalance)}).";
                return false;
            }

            piWalletBalance -= offer.piListPrice;
            walletOwnedPioneers.Add(WalletMarketplaceCatalog.CreateOwnedFromListing(offer));
            PushCurrencyToUi();
            NotifyRosterChanged();
            message = $"Purchased {offer.displayName} for {offer.piListPrice} Pi.";
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
            aetherCredits = Mathf.Max(0f, savedAetherCredits);
            piWalletBalance = savedPiWalletBalance > 0.01f
                ? Mathf.Max(0f, savedPiWalletBalance)
                : DefaultPiWalletBalance;
            workerCount = Mathf.Clamp(savedWorkerCount, 0, MaxWorkerPioneers);
            starterPioneerSelected = savedStarterSelected;
            skilledPioneers.Clear();

            if (savedSkilled != null)
            {
                for (int i = 0; i < savedSkilled.Length; i++)
                {
                    if (savedSkilled[i] == null)
                        continue;

                    skilledPioneers.Add(savedSkilled[i].ToRuntime());
                }
            }

            EnsureWalletBootstrapped();

            PushCurrencyToUi();
            NotifyRosterChanged();
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
    }
}
