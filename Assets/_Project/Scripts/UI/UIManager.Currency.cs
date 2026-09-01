using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using Project.Achievements;
using Project.Core;
using Project.Pioneers;
using Project.Player;
using Project.Quests;
using Project.Progression;
using Project.Survival;
using Project.Survival.Exposure;

namespace Project.UI
{
    public partial class UIManager
    {
        public void SetCurrencyHudVisible(bool visible)
        {
            if (piBalanceText == null)
                return;

            piBalanceText.gameObject.SetActive(visible);
            if (visible)
            {
                ConfigurePiBalancePosition();
                RefreshCurrencyHud();
            }
        }

        private void ConfigurePiBalancePosition()
        {
            if (!applyRuntimeHudLayout || piBalanceText == null)
                return;

            RectTransform piRect = piBalanceText.rectTransform;
            piRect.anchorMin = new Vector2(1f, 1f);
            piRect.anchorMax = new Vector2(1f, 1f);
            piRect.pivot = new Vector2(1f, 1f);
            piRect.anchoredPosition = new Vector2(
                -HudLayoutMetrics.RightHudInset,
                -HudLayoutMetrics.TopHudInset);

            piBalanceText.fontSize = Mathf.Max(12f, piBalanceText.fontSize * 0.5f);
            piBalanceText.alignment = TextAlignmentOptions.TopRight;
        }

        public float GetAetherCredits() => aetherCredits;

        public void SetAetherCredits(float balance)
        {
            aetherCredits = Mathf.Max(0f, balance);
            RefreshCurrencyHud();
        }

        public void ShowAcReward(int amount, string source = "Reward")
        {
            if (amount <= 0)
                return;

            PioneerRosterManager roster = PioneerRosterManager.Instance;
            if (roster != null)
            {
                roster.AddAetherCredits(amount, source);
                return;
            }

            aetherCredits += amount;
            ShowAcRewardPopup(amount, source);
            RefreshCurrencyHud();
        }

        public void ShowAcRewardPopup(int amount, string source = "Reward")
        {
            if (amount <= 0)
                return;

            ShowCurrencyPopup($"+{amount} AC", source);
        }

        public void ShowLevelUpPopup(int newLevel, int levelsGained = 1)
        {
            DMILevelUpPopupUI.Show(newLevel, levelsGained);
        }

        public void ShowPiReward(int amount, string source = "Gathering")
        {
            ShowAcReward(amount, source);
        }

        private void RefreshCurrencyHud()
        {
            if (piBalanceText == null || !piBalanceText.gameObject.activeSelf)
                return;

            piBalanceText.text = $"AC: {Mathf.RoundToInt(aetherCredits)}";
        }

        private void ShowCurrencyPopup(string amountLine, string source)
        {
            if (DMUiToolkitPiReward.TryShow(amountLine, source))
                return;

            if (piRewardPopupPrefab != null && popupParent != null)
            {
                GameObject popup = Instantiate(piRewardPopupPrefab, popupParent);
                RectTransform popupRect = popup.transform as RectTransform;
                if (popupRect != null)
                {
                    popupRect.anchorMin = new Vector2(0.5f, 0.5f);
                    popupRect.anchorMax = new Vector2(0.5f, 0.5f);
                    popupRect.pivot = new Vector2(0.5f, 0.5f);
                    popupRect.anchoredPosition = GameplayHudLayout.MessageToastAnchoredPosition;
                }

                TextMeshProUGUI txt = popup.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                    txt.text = $"{amountLine}\n{source}";

                if (popup.GetComponent<PiRewardPopup>() == null)
                    StartCoroutine(FadeAndDestroyPopup(popup));
            }
            else
            {
                // Fallback when reward prefab is not wired — still surface center-screen feedback.
                PickupToastUI.Show(string.IsNullOrWhiteSpace(source) ? amountLine : $"{amountLine}  ({source})");
            }
        }

        private System.Collections.IEnumerator FadeAndDestroyPopup(GameObject popup)
        {
            yield return new WaitForSeconds(2.5f);
            Destroy(popup);
        }

    }
}
