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
            if (acBalanceText == null)
                return;

            acBalanceText.gameObject.SetActive(visible);
            if (visible)
            {
                ConfigureAcBalancePosition();
                RefreshCurrencyHud();
            }
        }

        private void ConfigureAcBalancePosition()
        {
            if (!applyRuntimeHudLayout || acBalanceText == null)
                return;

            RectTransform acRect = acBalanceText.rectTransform;
            acRect.anchorMin = new Vector2(1f, 1f);
            acRect.anchorMax = new Vector2(1f, 1f);
            acRect.pivot = new Vector2(1f, 1f);
            acRect.anchoredPosition = new Vector2(
                -HudLayoutMetrics.RightHudInset,
                -HudLayoutMetrics.TopHudInset);

            acBalanceText.fontSize = Mathf.Max(12f, acBalanceText.fontSize * 0.5f);
            acBalanceText.alignment = TextAlignmentOptions.TopRight;
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

            ShowCurrencyPopup($"+{amount} AC Credits", source);
        }

        public void ShowLevelUpPopup(int newLevel, int levelsGained = 1)
        {
            DMILevelUpPopupUI.Show(newLevel, levelsGained);
        }

        [System.Obsolete("Use ShowAcReward.")]
        public void ShowPiReward(int amount, string source = "Gathering")
        {
            ShowAcReward(amount, source);
        }

        private void RefreshCurrencyHud()
        {
            if (acBalanceText == null || !acBalanceText.gameObject.activeSelf)
                return;

            acBalanceText.text = $"AC Credits: {Mathf.RoundToInt(aetherCredits)}";
        }

        private void ShowCurrencyPopup(string amountLine, string source)
        {
            if (DMUiToolkitAcReward.TryShow(amountLine, source))
                return;

            if (acRewardPopupPrefab != null && popupParent != null)
            {
                GameObject popup = Instantiate(acRewardPopupPrefab, popupParent);
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

                if (popup.GetComponent<AcRewardPopup>() == null)
                    StartCoroutine(FadeAndDestroyPopup(popup));
            }
            else
            {
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
