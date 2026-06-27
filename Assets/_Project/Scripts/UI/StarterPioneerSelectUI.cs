using System;
using Project.Core;
using Project.Pioneers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Full-screen new-game flow: pick one starter Skilled Pioneer using 5000 AC grant.
    /// Shown after Main Menu → New Game, before the welcome start popup.
    /// </summary>
    public class StarterPioneerSelectUI : MonoBehaviour
    {
        private GameObject overlayRoot;
        private GameObject panelRoot;
        private TextMeshProUGUI creditsLabel;
        private TextMeshProUGUI statusLabel;
        private Action onCompleted;

        public static StarterPioneerSelectUI EnsureExists()
        {
            StarterPioneerSelectUI existing = FindAnyObjectByType<StarterPioneerSelectUI>(FindObjectsInactive.Include);
            if (existing != null)
                return existing;

            Canvas canvas = MainMenuController.ResolveMainCanvas();
            if (canvas == null)
                return null;

            return canvas.gameObject.AddComponent<StarterPioneerSelectUI>();
        }

        public void Show(Action completedCallback)
        {
            onCompleted = completedCallback;
            EnsureBuilt();
            GameSession.SetPhase(GamePhase.StarterPioneerSelect);

            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            roster?.PrepareNewGameSession();

            overlayRoot.SetActive(true);
            panelRoot.SetActive(true);
            overlayRoot.transform.SetAsLastSibling();
            panelRoot.transform.SetAsLastSibling();
            RefreshCreditsLabel();
            statusLabel.text = "Choose one specialist to lead your first expedition trio.";
        }

        public void Hide()
        {
            if (overlayRoot != null)
                overlayRoot.SetActive(false);
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void EnsureBuilt()
        {
            if (panelRoot != null)
                return;

            Transform canvasRoot = transform;
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
                canvasRoot = canvas.transform;

            overlayRoot = MenuUiBuilder.CreateFullScreenPanel(
                canvasRoot,
                "StarterPioneerOverlay",
                new Color(0f, 0f, 0f, 0.55f),
                blockRaycasts: true);

            panelRoot = MenuUiBuilder.CreateFullscreenShell(
                canvasRoot,
                "Recruit Starter Specialist",
                out RectTransform contentArea,
                out Button headerCloseButton);
            panelRoot.name = "StarterPioneerPanel";
            headerCloseButton.gameObject.SetActive(false);

            VerticalLayoutGroup layout = contentArea.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 24, 24);
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            creditsLabel = MenuUiBuilder.CreateTitle(contentArea, "Aether Credits: 5000", 20f);
            creditsLabel.color = new Color(0.78f, 0.86f, 0.95f, 1f);
            statusLabel = MenuUiBuilder.CreateTitle(contentArea, string.Empty, 16f);
            statusLabel.color = new Color(0.72f, 0.78f, 0.86f, 1f);

            GameObject cardRow = new GameObject("OfferRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            cardRow.transform.SetParent(contentArea, false);
            HorizontalLayoutGroup cardLayout = cardRow.GetComponent<HorizontalLayoutGroup>();
            cardLayout.spacing = 12f;
            cardLayout.childAlignment = TextAnchor.UpperCenter;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = true;
            LayoutElement cardRowLayout = cardRow.AddComponent<LayoutElement>();
            cardRowLayout.minHeight = 360f;
            cardRowLayout.flexibleHeight = 1f;

            for (int i = 0; i < StarterPioneerCatalog.Offers.Count; i++)
                CreateOfferCard(cardRow.transform, StarterPioneerCatalog.Offers[i]);

            overlayRoot.SetActive(false);
            panelRoot.SetActive(false);
        }

        private void CreateOfferCard(Transform parent, StarterPioneerOffer offer)
        {
            GameObject card = new GameObject(offer.offerId, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            card.transform.SetParent(parent, false);

            Image cardImage = card.GetComponent<Image>();
            cardImage.color = new Color(0.1f, 0.12f, 0.16f, 0.95f);

            VerticalLayoutGroup cardLayout = card.GetComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(12, 12, 12, 12);
            cardLayout.spacing = 8f;
            cardLayout.childAlignment = TextAnchor.UpperLeft;
            cardLayout.childControlWidth = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            LayoutElement cardLayoutElement = card.AddComponent<LayoutElement>();
            cardLayoutElement.flexibleWidth = 1f;
            cardLayoutElement.flexibleHeight = 1f;

            TextMeshProUGUI nameLabel = MenuUiBuilder.CreateTitle(card.transform, offer.displayName, 18f);
            nameLabel.alignment = TextAlignmentOptions.TopLeft;
            TextMeshProUGUI classLabel = MenuUiBuilder.CreateTitle(
                card.transform,
                SkilledPioneerClassUtility.ToDisplayName(offer.pioneerClass),
                15f);
            classLabel.color = new Color(0.72f, 0.8f, 0.92f, 1f);
            classLabel.alignment = TextAlignmentOptions.TopLeft;

            TextMeshProUGUI statsLabel = MenuUiBuilder.CreateTitle(
                card.transform,
                $"Rad {offer.radiationResistance:P0}  Exp {offer.expeditionEfficiency:P0}  Syn {offer.combatSynergy:P0}",
                13f);
            statsLabel.alignment = TextAlignmentOptions.TopLeft;

            TextMeshProUGUI abilityLabel = MenuUiBuilder.CreateTitle(card.transform, offer.abilitySummary, 13f);
            abilityLabel.alignment = TextAlignmentOptions.TopLeft;

            TextMeshProUGUI storyLabel = MenuUiBuilder.CreateTitle(card.transform, offer.backstory, 12f);
            storyLabel.color = new Color(0.68f, 0.72f, 0.78f, 1f);
            storyLabel.alignment = TextAlignmentOptions.TopLeft;

            Button recruitButton = MenuUiBuilder.CreateButton(
                card.transform,
                $"Recruit ({offer.acCost} AC)",
                new Vector2(220f, 42f),
                16f);
            recruitButton.onClick.AddListener(() => HandleRecruitClicked(offer));
        }

        private void HandleRecruitClicked(StarterPioneerOffer offer)
        {
            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            if (roster == null)
            {
                statusLabel.text = "Roster system unavailable.";
                return;
            }

            if (!roster.TryPurchaseStarterOffer(offer, out string message))
            {
                statusLabel.text = message;
                return;
            }

            statusLabel.text = message;
            Hide();
            onCompleted?.Invoke();
            onCompleted = null;
        }

        private void RefreshCreditsLabel()
        {
            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            float credits = roster != null ? roster.AetherCredits : StarterPioneerCatalog.StarterAcGrant;
            creditsLabel.text = $"Aether Credits: {Mathf.RoundToInt(credits)}";
        }
    }
}
