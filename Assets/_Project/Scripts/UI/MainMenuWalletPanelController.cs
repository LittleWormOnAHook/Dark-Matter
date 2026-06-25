using System;
using System.Collections.Generic;
using Project.Core;
using Project.Pioneers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Main-menu Wallet: balances + Pi→AC swap, marketplace listings, and owned pioneer roster.
    /// </summary>
    public class MainMenuWalletPanelController : MonoBehaviour
    {
        private enum WalletTab
        {
            Balances,
            Marketplace,
            PioneersOwned
        }

        private const float MenuScale = 1f;

        private TextMeshProUGUI piBalanceLabel;
        private TextMeshProUGUI acBalanceLabel;
        private GameObject walletOverlayRoot;
        private GameObject balancesTabRoot;
        private GameObject marketplaceTabRoot;
        private GameObject pioneersTabRoot;
        private Button balancesTabButton;
        private Button marketplaceTabButton;
        private Button pioneersTabButton;
        private TextMeshProUGUI overlayPiLabel;
        private TextMeshProUGUI overlayAcLabel;
        private TextMeshProUGUI swapStatusLabel;
        private TMP_InputField swapAmountInput;
        private Transform marketplaceListParent;
        private Transform pioneersListParent;
        private PioneerRosterManager roster;
        private WalletTab activeTab = WalletTab.Balances;

        public bool IsSwapPanelOpen => walletOverlayRoot != null && walletOverlayRoot.activeSelf;
        public bool IsWalletPanelOpen => IsSwapPanelOpen;

        private void OnEnable()
        {
            roster = PioneerRosterManager.EnsureExists();
            if (roster != null)
            {
                roster.OnCurrencyChanged += HandleRosterDataChanged;
                roster.OnRosterChanged += HandleRosterDataChanged;
            }
        }

        private void OnDisable()
        {
            if (roster != null)
            {
                roster.OnCurrencyChanged -= HandleRosterDataChanged;
                roster.OnRosterChanged -= HandleRosterDataChanged;
            }
        }

        public void Build(Transform menuPanelTransform)
        {
            if (menuPanelTransform == null || piBalanceLabel != null)
                return;

            Transform canvasRoot = menuPanelTransform.parent;

            GameObject walletBlock = new GameObject("WalletBlock", typeof(RectTransform), typeof(VerticalLayoutGroup));
            walletBlock.transform.SetParent(menuPanelTransform, false);
            VerticalLayoutGroup walletLayout = walletBlock.GetComponent<VerticalLayoutGroup>();
            walletLayout.spacing = Mathf.RoundToInt(6f * MenuScale);
            walletLayout.childAlignment = TextAnchor.UpperCenter;
            walletLayout.childControlWidth = true;
            walletLayout.childForceExpandWidth = true;
            walletLayout.childForceExpandHeight = false;

            TextMeshProUGUI walletTitle = MenuUiBuilder.CreateTitle(walletBlock.transform, "Wallet", 16f * MenuScale);
            walletTitle.color = new Color(0.72f, 0.8f, 0.92f, 1f);

            piBalanceLabel = MenuUiBuilder.CreateTitle(walletBlock.transform, "Pi Wallet: 0", 15f * MenuScale);
            piBalanceLabel.alignment = TextAlignmentOptions.Center;
            piBalanceLabel.color = new Color(0.82f, 0.86f, 0.92f, 1f);

            acBalanceLabel = MenuUiBuilder.CreateTitle(walletBlock.transform, "Aether Credits: 0", 15f * MenuScale);
            acBalanceLabel.alignment = TextAlignmentOptions.Center;
            acBalanceLabel.color = new Color(0.82f, 0.86f, 0.92f, 1f);

            Button openWalletButton = MenuUiBuilder.CreateButton(
                walletBlock.transform,
                "Wallet",
                new Vector2(220f * MenuScale, 44f * MenuScale),
                18f * MenuScale);
            openWalletButton.onClick.AddListener(OpenWalletPanel);

            BuildWalletOverlay(canvasRoot);
            walletOverlayRoot.SetActive(false);
        }

        public void Refresh()
        {
            roster = PioneerRosterManager.EnsureExists();
            roster?.EnsureWalletBootstrapped();

            float piWallet = roster != null ? roster.PiWalletBalance : 0f;
            float ac = roster != null ? roster.AetherCredits : 0f;

            if (piBalanceLabel != null)
                piBalanceLabel.text = $"Pi Wallet: {Mathf.RoundToInt(piWallet)}";

            if (acBalanceLabel != null)
                acBalanceLabel.text = $"Aether Credits: {Mathf.RoundToInt(ac)}";

            if (overlayPiLabel != null)
                overlayPiLabel.text = $"Pi Wallet: {Mathf.RoundToInt(piWallet)}";

            if (overlayAcLabel != null)
                overlayAcLabel.text = $"Aether Credits: {Mathf.RoundToInt(ac)}";

            RefreshActiveTab();
        }

        public void CloseSwapPanel() => CloseWalletPanel();

        public void CloseWalletPanel()
        {
            if (walletOverlayRoot != null)
                walletOverlayRoot.SetActive(false);
        }

        private void HandleRosterDataChanged()
        {
            if (walletOverlayRoot != null && walletOverlayRoot.activeSelf)
                Refresh();
        }

        private void OpenWalletPanel()
        {
            if (walletOverlayRoot == null)
                return;

            Refresh();
            walletOverlayRoot.SetActive(true);
            walletOverlayRoot.transform.SetAsLastSibling();
            ShowTab(WalletTab.Balances);
        }

        private void BuildWalletOverlay(Transform canvasRoot)
        {
            walletOverlayRoot = MenuUiBuilder.CreateFullScreenPanel(
                canvasRoot,
                "WalletSwapPanel",
                new Color(0f, 0f, 0f, 0.92f),
                blockRaycasts: true);

            GameObject window = new GameObject("WalletWindow", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            window.transform.SetParent(walletOverlayRoot.transform, false);
            Image windowImage = window.GetComponent<Image>();
            windowImage.color = new Color(0.08f, 0.09f, 0.12f, 0.98f);

            RectTransform windowRect = window.GetComponent<RectTransform>();
            windowRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.sizeDelta = new Vector2(640f * MenuScale, 520f * MenuScale);

            VerticalLayoutGroup windowLayout = window.GetComponent<VerticalLayoutGroup>();
            windowLayout.padding = new RectOffset(24, 24, 20, 20);
            windowLayout.spacing = 12;
            windowLayout.childAlignment = TextAnchor.UpperCenter;
            windowLayout.childControlWidth = true;
            windowLayout.childForceExpandWidth = true;
            windowLayout.childForceExpandHeight = false;

            MenuUiBuilder.CreateTitle(window.transform, "Wallet", 28f * MenuScale);

            GameObject balanceRow = new GameObject("BalanceRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            balanceRow.transform.SetParent(window.transform, false);
            HorizontalLayoutGroup balanceLayout = balanceRow.GetComponent<HorizontalLayoutGroup>();
            balanceLayout.spacing = 24f;
            balanceLayout.childAlignment = TextAnchor.MiddleCenter;
            balanceLayout.childControlWidth = true;
            balanceLayout.childForceExpandWidth = true;

            overlayPiLabel = MenuUiBuilder.CreateTitle(balanceRow.transform, "Pi Wallet: 0", 16f * MenuScale);
            overlayPiLabel.alignment = TextAlignmentOptions.Center;
            overlayAcLabel = MenuUiBuilder.CreateTitle(balanceRow.transform, "Aether Credits: 0", 16f * MenuScale);
            overlayAcLabel.alignment = TextAlignmentOptions.Center;

            GameObject tabRow = new GameObject("TabRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabRow.transform.SetParent(window.transform, false);
            HorizontalLayoutGroup tabLayout = tabRow.GetComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 8f;
            tabLayout.childAlignment = TextAnchor.MiddleCenter;
            tabLayout.childControlWidth = false;

            balancesTabButton = MenuUiBuilder.CreateButton(
                tabRow.transform,
                "Balances",
                new Vector2(140f * MenuScale, 36f * MenuScale),
                16f * MenuScale);
            marketplaceTabButton = MenuUiBuilder.CreateButton(
                tabRow.transform,
                "Marketplace",
                new Vector2(160f * MenuScale, 36f * MenuScale),
                16f * MenuScale);
            pioneersTabButton = MenuUiBuilder.CreateButton(
                tabRow.transform,
                "Pioneers Owned",
                new Vector2(180f * MenuScale, 36f * MenuScale),
                16f * MenuScale);
            balancesTabButton.onClick.AddListener(() => ShowTab(WalletTab.Balances));
            marketplaceTabButton.onClick.AddListener(() => ShowTab(WalletTab.Marketplace));
            pioneersTabButton.onClick.AddListener(() => ShowTab(WalletTab.PioneersOwned));

            GameObject tabHost = new GameObject("TabHost", typeof(RectTransform));
            tabHost.transform.SetParent(window.transform, false);
            LayoutElement tabHostLayout = tabHost.AddComponent<LayoutElement>();
            tabHostLayout.minHeight = 320f;
            tabHostLayout.flexibleHeight = 1f;
            Stretch(tabHost.GetComponent<RectTransform>());

            balancesTabRoot = BuildBalancesTab(tabHost.transform);
            marketplaceTabRoot = BuildMarketplaceTab(tabHost.transform);
            pioneersTabRoot = BuildPioneersOwnedTab(tabHost.transform);

            Button closeButton = MenuUiBuilder.CreateButton(
                window.transform,
                "Close",
                new Vector2(160f * MenuScale, 44f * MenuScale),
                18f * MenuScale);
            closeButton.onClick.AddListener(CloseWalletPanel);
        }

        private GameObject BuildBalancesTab(Transform parent)
        {
            GameObject root = new GameObject("BalancesTab", typeof(RectTransform), typeof(VerticalLayoutGroup));
            root.transform.SetParent(parent, false);
            Stretch(root.GetComponent<RectTransform>());

            VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            TextMeshProUGUI body = MenuUiBuilder.CreateTitle(
                root.transform,
                "Pi Wallet connects to your external Pi Network wallet.\n" +
                "Aether Credits (AC) are in-game survival currency.\n" +
                "One-way mock swap: Pi → AC at 1:1 (prototype).",
                14f * MenuScale);
            body.alignment = TextAlignmentOptions.TopLeft;
            body.color = new Color(0.78f, 0.82f, 0.88f, 1f);

            GameObject inputRow = new GameObject("SwapInputRow", typeof(RectTransform));
            inputRow.transform.SetParent(root.transform, false);
            LayoutElement inputRowLayout = inputRow.AddComponent<LayoutElement>();
            inputRowLayout.minHeight = 40f;

            GameObject inputObject = new GameObject("SwapAmountInput", typeof(RectTransform), typeof(Image));
            inputObject.transform.SetParent(inputRow.transform, false);
            RectTransform inputRect = inputObject.GetComponent<RectTransform>();
            Stretch(inputRect);
            Image inputBg = inputObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(inputBg);
            inputBg.color = new Color(0.12f, 0.14f, 0.18f, 1f);

            swapAmountInput = inputObject.AddComponent<TMP_InputField>();
            swapAmountInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            swapAmountInput.text = "10";

            GameObject textArea = new GameObject("Text Area", typeof(RectTransform));
            textArea.transform.SetParent(inputObject.transform, false);
            RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
            Stretch(textAreaRect);
            textAreaRect.offsetMin = new Vector2(10f, 6f);
            textAreaRect.offsetMax = new Vector2(-10f, -6f);

            TextMeshProUGUI inputText = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            inputText.transform.SetParent(textArea.transform, false);
            Stretch(inputText.rectTransform);
            TmpUiHelper.ApplyDefaultFont(inputText);
            inputText.fontSize = 16f;
            inputText.color = Color.white;
            swapAmountInput.textComponent = inputText;

            TextMeshProUGUI placeholder = new GameObject("Placeholder", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            placeholder.transform.SetParent(textArea.transform, false);
            Stretch(placeholder.rectTransform);
            TmpUiHelper.ApplyDefaultFont(placeholder);
            placeholder.text = "Pi amount to swap";
            placeholder.fontSize = 16f;
            placeholder.color = new Color(0.55f, 0.6f, 0.66f, 0.9f);
            swapAmountInput.placeholder = placeholder;

            Button swapButton = MenuUiBuilder.CreateButton(
                root.transform,
                "Swap Pi → AC",
                new Vector2(220f * MenuScale, 44f * MenuScale),
                16f * MenuScale);
            swapButton.onClick.AddListener(HandleSwap);

            swapStatusLabel = MenuUiBuilder.CreateTitle(root.transform, string.Empty, 13f * MenuScale);
            swapStatusLabel.alignment = TextAlignmentOptions.TopLeft;
            swapStatusLabel.color = new Color(0.85f, 0.68f, 0.18f, 1f);

            root.SetActive(false);
            return root;
        }

        private GameObject BuildMarketplaceTab(Transform parent)
        {
            GameObject root = new GameObject("MarketplaceTab", typeof(RectTransform), typeof(VerticalLayoutGroup));
            root.transform.SetParent(parent, false);
            Stretch(root.GetComponent<RectTransform>());

            VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            TextMeshProUGUI intro = MenuUiBuilder.CreateTitle(
                root.transform,
                "Pioneer Survivor Exchange — mock Pi listings (prototype).",
                14f * MenuScale);
            intro.alignment = TextAlignmentOptions.TopLeft;
            intro.color = new Color(0.78f, 0.82f, 0.88f, 1f);

            marketplaceListParent = CreateScrollContent(root.transform);
            root.SetActive(false);
            return root;
        }

        private GameObject BuildPioneersOwnedTab(Transform parent)
        {
            GameObject root = new GameObject("PioneersOwnedTab", typeof(RectTransform), typeof(VerticalLayoutGroup));
            root.transform.SetParent(parent, false);
            Stretch(root.GetComponent<RectTransform>());

            VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            TextMeshProUGUI intro = MenuUiBuilder.CreateTitle(
                root.transform,
                "Account wallet roster — pioneers you own (NFT prototype).",
                14f * MenuScale);
            intro.alignment = TextAlignmentOptions.TopLeft;
            intro.color = new Color(0.78f, 0.82f, 0.88f, 1f);

            pioneersListParent = CreateScrollContent(root.transform);
            root.SetActive(false);
            return root;
        }

        private static Transform CreateScrollContent(Transform parent)
        {
            GameObject scrollHost = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollHost.transform.SetParent(parent, false);
            LayoutElement scrollLayout = scrollHost.AddComponent<LayoutElement>();
            scrollLayout.flexibleHeight = 1f;
            scrollLayout.minHeight = 260f;
            scrollHost.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 0.92f);

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewport.transform.SetParent(scrollHost.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            Stretch(viewportRect);
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 8f;
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scrollRect = scrollHost.GetComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            return content.transform;
        }

        private void ShowTab(WalletTab tab)
        {
            activeTab = tab;
            if (balancesTabRoot != null)
                balancesTabRoot.SetActive(tab == WalletTab.Balances);
            if (marketplaceTabRoot != null)
                marketplaceTabRoot.SetActive(tab == WalletTab.Marketplace);
            if (pioneersTabRoot != null)
                pioneersTabRoot.SetActive(tab == WalletTab.PioneersOwned);

            RefreshActiveTab();
        }

        private void RefreshActiveTab()
        {
            switch (activeTab)
            {
                case WalletTab.Marketplace:
                    RefreshMarketplaceList();
                    break;
                case WalletTab.PioneersOwned:
                    RefreshOwnedPioneersList();
                    break;
            }
        }

        private void RefreshMarketplaceList()
        {
            if (marketplaceListParent == null)
                return;

            ClearChildren(marketplaceListParent);
            roster = PioneerRosterManager.EnsureExists();
            if (roster == null)
                return;

            IReadOnlyList<WalletMarketplaceOffer> listings = WalletMarketplaceCatalog.Listings;
            for (int i = 0; i < listings.Count; i++)
            {
                WalletMarketplaceOffer offer = listings[i];
                bool owned = roster.OwnsMarketplaceListing(offer.offerId);
                CreateMarketplaceCard(marketplaceListParent, offer, owned);
            }
        }

        private void RefreshOwnedPioneersList()
        {
            if (pioneersListParent == null)
                return;

            ClearChildren(pioneersListParent);
            roster = PioneerRosterManager.EnsureExists();
            if (roster == null)
                return;

            IReadOnlyList<SkilledPioneerRecord> owned = roster.WalletOwnedPioneers;
            if (owned.Count == 0)
            {
                CreateInfoCard(pioneersListParent, "No pioneers in wallet yet. Browse the Marketplace tab.");
                return;
            }

            for (int i = 0; i < owned.Count; i++)
                CreateOwnedPioneerCard(pioneersListParent, owned[i]);
        }

        private void CreateMarketplaceCard(Transform parent, WalletMarketplaceOffer offer, bool owned)
        {
            GameObject card = CreatePioneerCardShell(parent, 96f);
            Transform row = card.transform.Find("Row");
            Transform textColumn = row.Find("TextColumn");

            TextMeshProUGUI nameLabel = MenuUiBuilder.CreateTitle(textColumn, offer.displayName, 16f * MenuScale);
            nameLabel.alignment = TextAlignmentOptions.TopLeft;

            TextMeshProUGUI detailLabel = MenuUiBuilder.CreateTitle(
                textColumn,
                $"{SkilledPioneerClassUtility.ToDisplayName(offer.pioneerClass)}  ·  Lv {offer.level}\n" +
                FormatStats(offer.radiationResistance, offer.expeditionEfficiency, offer.combatSynergy) + "\n" +
                offer.abilitySummary + "\n" +
                offer.listingNote,
                12f * MenuScale);
            detailLabel.alignment = TextAlignmentOptions.TopLeft;
            detailLabel.color = new Color(0.78f, 0.82f, 0.88f, 1f);

            GameObject actionHost = new GameObject("Action", typeof(RectTransform));
            actionHost.transform.SetParent(row, false);
            LayoutElement actionLayout = actionHost.AddComponent<LayoutElement>();
            actionLayout.minWidth = 120f;
            actionLayout.preferredWidth = 120f;

            if (owned)
            {
                TextMeshProUGUI ownedLabel = MenuUiBuilder.CreateTitle(actionHost.transform, "Owned", 14f * MenuScale);
                ownedLabel.alignment = TextAlignmentOptions.Center;
                ownedLabel.color = new Color(0.55f, 0.82f, 0.55f, 1f);
            }
            else
            {
                Button buyButton = MenuUiBuilder.CreateButton(
                    actionHost.transform,
                    $"{offer.piListPrice} Pi",
                    new Vector2(110f * MenuScale, 40f * MenuScale),
                    14f * MenuScale);
                string offerId = offer.offerId;
                buyButton.onClick.AddListener(() => HandleMarketplacePurchase(offerId));
            }
        }

        private void CreateOwnedPioneerCard(Transform parent, SkilledPioneerRecord record)
        {
            GameObject card = CreatePioneerCardShell(parent, 88f);
            Transform textColumn = card.transform.Find("Row/TextColumn");

            TextMeshProUGUI nameLabel = MenuUiBuilder.CreateTitle(textColumn, record.displayName, 16f * MenuScale);
            nameLabel.alignment = TextAlignmentOptions.TopLeft;

            TextMeshProUGUI detailLabel = MenuUiBuilder.CreateTitle(
                textColumn,
                $"{SkilledPioneerClassUtility.ToDisplayName(record.pioneerClass)}  ·  Lv {record.level}\n" +
                FormatStats(record.radiationResistance, record.expeditionEfficiency, record.combatSynergy) + "\n" +
                (string.IsNullOrEmpty(record.backstory) ? "Wallet pioneer." : record.backstory),
                12f * MenuScale);
            detailLabel.alignment = TextAlignmentOptions.TopLeft;
            detailLabel.color = new Color(0.78f, 0.82f, 0.88f, 1f);
        }

        private static void CreateInfoCard(Transform parent, string message)
        {
            GameObject card = new GameObject("InfoCard", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            card.transform.SetParent(parent, false);
            card.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 0.95f);
            card.GetComponent<LayoutElement>().minHeight = 64f;

            TextMeshProUGUI label = MenuUiBuilder.CreateTitle(card.transform, message, 13f);
            label.alignment = TextAlignmentOptions.TopLeft;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 8f);
            labelRect.offsetMax = new Vector2(-10f, -8f);
        }

        private GameObject CreatePioneerCardShell(Transform parent, float minHeight)
        {
            GameObject card = new GameObject("PioneerCard", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            card.transform.SetParent(parent, false);
            card.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 0.95f);
            card.GetComponent<LayoutElement>().minHeight = minHeight;

            HorizontalLayoutGroup cardLayout = card.GetComponent<HorizontalLayoutGroup>();
            cardLayout.padding = new RectOffset(10, 10, 8, 8);
            cardLayout.spacing = 10f;
            cardLayout.childAlignment = TextAnchor.MiddleLeft;
            cardLayout.childControlWidth = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandHeight = true;

            GameObject row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(card.transform, false);
            HorizontalLayoutGroup rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 10f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandHeight = true;
            LayoutElement rowElement = row.AddComponent<LayoutElement>();
            rowElement.flexibleWidth = 1f;

            GameObject iconHost = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            iconHost.transform.SetParent(row.transform, false);
            LayoutElement iconLayout = iconHost.GetComponent<LayoutElement>();
            iconLayout.minWidth = 64f;
            iconLayout.preferredWidth = 64f;
            iconLayout.minHeight = 64f;
            iconLayout.preferredHeight = 64f;
            Image iconImage = iconHost.GetComponent<Image>();
            Sprite skull = PioneerWalletAssets.SkullIcon256;
            if (skull != null)
            {
                iconImage.sprite = skull;
                iconImage.preserveAspect = true;
                iconImage.color = Color.white;
            }
            else
            {
                iconImage.color = new Color(0.35f, 0.38f, 0.42f, 1f);
            }

            GameObject textColumn = new GameObject("TextColumn", typeof(RectTransform), typeof(VerticalLayoutGroup));
            textColumn.transform.SetParent(row.transform, false);
            VerticalLayoutGroup textLayout = textColumn.GetComponent<VerticalLayoutGroup>();
            textLayout.spacing = 4f;
            textLayout.childAlignment = TextAnchor.UpperLeft;
            textLayout.childControlWidth = true;
            textLayout.childForceExpandWidth = true;
            textLayout.childForceExpandHeight = false;
            LayoutElement textColumnLayout = textColumn.AddComponent<LayoutElement>();
            textColumnLayout.flexibleWidth = 1f;

            return card;
        }

        private void HandleSwap()
        {
            if (swapStatusLabel == null)
                return;

            roster = PioneerRosterManager.EnsureExists();
            if (roster == null)
            {
                swapStatusLabel.text = "Roster manager unavailable.";
                return;
            }

            if (!int.TryParse(swapAmountInput != null ? swapAmountInput.text : string.Empty, out int amount) || amount <= 0)
            {
                swapStatusLabel.text = "Enter a valid Pi amount to swap.";
                return;
            }

            if (roster.TrySwapPiForAetherCredits(amount, out string message))
                swapStatusLabel.color = new Color(0.55f, 0.82f, 0.55f, 1f);
            else
                swapStatusLabel.color = new Color(0.92f, 0.45f, 0.4f, 1f);

            swapStatusLabel.text = message;
            Refresh();
        }

        private void HandleMarketplacePurchase(string offerId)
        {
            roster = PioneerRosterManager.EnsureExists();
            if (roster == null)
                return;

            roster.TryPurchaseMarketplaceListing(offerId, out string message);
            if (swapStatusLabel != null && activeTab == WalletTab.Balances)
                swapStatusLabel.text = message;

            Refresh();
        }

        private static string FormatStats(float rad, float exp, float syn)
        {
            return $"Rad {rad:P0}  Exp {exp:P0}  Syn {syn:P0}";
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
