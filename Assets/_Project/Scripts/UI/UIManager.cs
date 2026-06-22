using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Project.Core;
using Project.Player;
using Project.Quests;
using Project.Survival;

namespace Project.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Survival Stats UI")]
        public Slider healthSlider;
        public Slider hungerSlider;
        public Slider thirstSlider;
        public Slider energySlider;

        public TextMeshProUGUI healthText;
        public TextMeshProUGUI hungerText;
        public TextMeshProUGUI thirstText;
        public TextMeshProUGUI energyText;

        [Header("Pi Network")]
        public TextMeshProUGUI piBalanceText;

        [Header("Interaction Prompt")]
        public TextMeshProUGUI interactionPrompt;

        [Header("Popups")]
        public GameObject piRewardPopupPrefab;
        public Transform popupParent;

        [Header("Combat UI")]
        public GameObject floatingDamagePrefab;
        public Transform combatPopupParent;

        [Header("Layout")]
        [Tooltip("When disabled, Pi balance and interaction prompt rects are not repositioned at Start.")]
        [SerializeField] private bool applyRuntimeHudLayout = true;

        private SurvivalStats survivalStats;
        private Camera worldCamera;
        private float piBalance = 0f;

        private void Awake()
        {
            BindSurvivalStats();
            EnsureSurvivalPanelBinder();
            EnsureMapUi();
            EnsureToolBarUi();
            EnsureShiftHudBootstrap();
            EnsurePickupProximityDotUi();
            EnsurePickupAimReticleUi();
            EnsureJournalPanelUi();
            EnsureQuestManager();
            EnsureCraftingUi();
        }

        private void EnsureJournalPanelUi()
        {
            if (GetComponent<JournalPanelUI>() == null)
                gameObject.AddComponent<JournalPanelUI>();
        }

        private void EnsureQuestManager()
        {
            QuestManager.EnsureExists();
        }

        private void EnsureCraftingUi()
        {
            if (GetComponent<CraftingUI>() == null)
                gameObject.AddComponent<CraftingUI>();
        }

        private void EnsurePickupProximityDotUi()
        {
            if (GetComponent<PickupProximityDotUI>() == null)
                gameObject.AddComponent<PickupProximityDotUI>();
        }

        private void EnsurePickupAimReticleUi()
        {
            if (GetComponent<PickupAimReticleUI>() == null)
                gameObject.AddComponent<PickupAimReticleUI>();
        }

        private void EnsureShiftHudBootstrap()
        {
            if (GetComponent<ShiftHudBootstrap>() == null)
                gameObject.AddComponent<ShiftHudBootstrap>();
        }

        private void EnsureMapUi()
        {
            if (GetComponent<MapUI>() == null)
                gameObject.AddComponent<MapUI>();
        }

        private void EnsureToolBarUi()
        {
            if (GetComponent<ToolBarUI>() == null)
                gameObject.AddComponent<ToolBarUI>();
        }

        private void Start()
        {
            BindSurvivalStats();
            UpdateSurvivalUI();
            if (piBalanceText != null) piBalanceText.text = "Pi: 0";
            if (interactionPrompt != null) interactionPrompt.gameObject.SetActive(false);
            ConfigureInteractionPromptPosition();
            ConfigurePiBalancePosition();
            EnsureGameplayUiHelpers();
            worldCamera = Camera.main;
            EnsureCombatUiReady();
        }

        private void EnsureGameplayUiHelpers()
        {
            PickupToastUI.EnsureExists(transform);
            QuestGiverDialogUI.EnsureExists(transform);
            ActiveQuestHudUI.EnsureExists(transform);
            UiFrontLayer.Get(transform);
        }

        private void ConfigureInteractionPromptPosition()
        {
            EnsureInteractionPrompt();
            ApplyInteractionPromptLayout();
        }

        private void EnsureInteractionPrompt()
        {
            if (interactionPrompt != null)
                return;

            Transform existing = transform.Find("InteractionPrompt");
            if (existing != null)
            {
                interactionPrompt = existing.GetComponent<TextMeshProUGUI>();
                if (interactionPrompt != null)
                    return;
            }

            GameObject promptObject = new GameObject("InteractionPrompt", typeof(RectTransform));
            promptObject.transform.SetParent(transform, false);
            interactionPrompt = promptObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(interactionPrompt);
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.ApplyFont(interactionPrompt, semiBold: true);
            interactionPrompt.raycastTarget = false;
            interactionPrompt.gameObject.SetActive(false);
        }

        private void ApplyInteractionPromptLayout()
        {
            if (interactionPrompt == null)
                return;

            RectTransform promptRect = interactionPrompt.rectTransform;
            promptRect.anchorMin = new Vector2(0.5f, 0.5f);
            promptRect.anchorMax = new Vector2(0.5f, 0.5f);
            promptRect.pivot = new Vector2(0.5f, 0.5f);
            promptRect.anchoredPosition = new Vector2(0f, 96f);
            promptRect.sizeDelta = new Vector2(760f, 56f);

            interactionPrompt.fontSize = 24f;
            interactionPrompt.alignment = TextAlignmentOptions.Center;
            interactionPrompt.textWrappingMode = TextWrappingModes.NoWrap;
            interactionPrompt.overflowMode = TextOverflowModes.Overflow;
            interactionPrompt.color = ShiftUiTheme.Current != null
                ? ShiftUiTheme.Current.primaryColor
                : new Color(0.82f, 0.92f, 1f, 0.98f);
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

        private void EnsureCombatUiReady()
        {
            if (combatPopupParent == null)
                combatPopupParent = popupParent != null ? popupParent : transform;

            if (floatingDamagePrefab == null)
            {
                floatingDamagePrefab = Resources.Load<GameObject>("Combat/FloatingDamageNumber");
#if UNITY_EDITOR
                if (floatingDamagePrefab == null)
                {
                    floatingDamagePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                        "Assets/_Project/Prefabs/UI/FloatingDamageNumber.prefab");
                }
#endif
            }
        }

        public void ShowFloatingDamage(float damage, Vector3 worldPosition)
        {
            WorldFloatingDamageNumber.Spawn(damage, worldPosition);
        }

        private Transform GetCombatPopupParent()
        {
            if (combatPopupParent != null)
                return combatPopupParent;

            if (popupParent != null)
                return popupParent;

            Canvas canvas = GetComponent<Canvas>();
            return canvas != null ? canvas.transform : transform;
        }

        private void BindSurvivalStats()
        {
            SurvivalStats found = null;

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player != null)
                found = player.GetComponent<SurvivalStats>();

            if (found == null)
                found = FindAnyObjectByType<SurvivalStats>();

            if (found == null)
                return;

            if (survivalStats != null)
                survivalStats.OnStatsChanged -= UpdateSurvivalUI;

            survivalStats = found;
            survivalStats.OnStatsChanged -= UpdateSurvivalUI;
            survivalStats.OnStatsChanged += UpdateSurvivalUI;
        }

        private void EnsureSurvivalPanelBinder()
        {
            if (healthSlider == null)
                return;

            Transform current = healthSlider.transform;
            while (current != null && current.name != "SurvivalStatsPanel")
                current = current.parent;

            if (current == null)
                return;

            if (current.GetComponent<SurvivalStatsPanelBinder>() == null)
                current.gameObject.AddComponent<SurvivalStatsPanelBinder>();

            if (current.GetComponent<CondensedSurvivalStatsHud>() == null)
                current.gameObject.AddComponent<CondensedSurvivalStatsHud>();
        }

        public void SyncSurvivalBars()
        {
            BindSurvivalStats();
            UpdateSurvivalUI();
        }

        private void UpdateSurvivalUI()
        {
            if (survivalStats == null) return;

            SetSliderValue(healthSlider, survivalStats.CurrentHealth / survivalStats.maxHealth);
            SetSliderValue(hungerSlider, survivalStats.CurrentHunger / survivalStats.maxHunger);
            SetSliderValue(thirstSlider, survivalStats.CurrentThirst / survivalStats.maxThirst);
            SetSliderValue(energySlider, survivalStats.CurrentEnergy / survivalStats.maxEnergy);

            if (healthText != null)
                healthText.text = FormatStatValue(survivalStats.CurrentHealth, "Health");
            if (hungerText != null)
                hungerText.text = FormatStatValue(survivalStats.CurrentHunger, "Hunger");
            if (thirstText != null)
                thirstText.text = FormatStatValue(survivalStats.CurrentThirst, "Thirst");
            if (energyText != null)
                energyText.text = FormatStatValue(survivalStats.CurrentEnergy, "Energy");
        }

        private static string FormatStatValue(float value, string statName)
        {
            if (CondensedSurvivalStatsHud.IsActive)
                return Mathf.CeilToInt(value).ToString();

            return $"{statName}: {Mathf.Ceil(value)}";
        }

        private static void SetSliderValue(Slider slider, float normalizedValue)
        {
            if (slider == null) return;

            float clamped = Mathf.Clamp01(normalizedValue);
            slider.SetValueWithoutNotify(clamped);

            if (CondensedSurvivalStatsHud.IsActive)
            {
                CondensedSurvivalStatsHud.ApplyBarFill(slider, clamped);
                return;
            }

            Transform fillTransform = slider.transform.Find("RingFill");
            if (fillTransform != null && fillTransform.TryGetComponent<Image>(out Image fillImage))
            {
                bool useHorizontalFill = CondensedSurvivalStatsHud.IsActive
                    || (fillImage.type == Image.Type.Filled
                        && fillImage.fillMethod == Image.FillMethod.Horizontal);

                if (useHorizontalFill)
                {
                    fillImage.fillAmount = clamped;
                    return;
                }
            }

            CircularProgressBar progress = slider.GetComponent<CircularProgressBar>();
            if (progress != null)
                progress.UpdateRadialFill(clamped);
        }

        public float GetPiBalance() => piBalance;

        public void SetPiBalance(float balance)
        {
            piBalance = Mathf.Max(0f, balance);
            if (piBalanceText != null)
                piBalanceText.text = $"Pi: {piBalance}";
        }

        public void ShowPiReward(int amount, string source = "Gathering")
        {
            piBalance += amount;

            if (piRewardPopupPrefab != null && popupParent != null)
            {
                GameObject popup = Instantiate(piRewardPopupPrefab, popupParent);
                TextMeshProUGUI txt = popup.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                    txt.text = $"+{amount} Pi\n{source}";

                // If the popup has its own animation handler, let it manage its lifetime.
                // Otherwise, fall back to the basic fade-and-destroy routine.
                if (popup.GetComponent<PiRewardPopup>() == null)
                {
                    StartCoroutine(FadeAndDestroyPopup(popup));
                }
            }

            if (piBalanceText != null)
                piBalanceText.text = $"Pi: {piBalance}";
        }

        public void ShowInteractionPrompt(string message)
        {
            EnsureInteractionPrompt();
            if (interactionPrompt == null)
                return;

            ApplyInteractionPromptLayout();
            interactionPrompt.text = message;
            interactionPrompt.gameObject.SetActive(true);
            interactionPrompt.transform.SetAsLastSibling();
        }

        public void ShowPetFetchMessage(string itemName)
        {
            ShowInteractionPrompt($"Your fox found: {itemName}!");
            CancelInvoke(nameof(HideInteractionPrompt));
            Invoke(nameof(HideInteractionPrompt), 2.5f);
        }

        public void HideInteractionPrompt()
        {
            if (interactionPrompt != null)
                interactionPrompt.gameObject.SetActive(false);
        }

        private System.Collections.IEnumerator FadeAndDestroyPopup(GameObject popup)
        {
            yield return new WaitForSeconds(2.5f);
            Destroy(popup);
        }

        public void RestartScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void RespawnPlayer()
        {
            Time.timeScale = 1f;

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player != null)
            {
                PlayerDeathHandler deathHandler = player.GetComponent<PlayerDeathHandler>();
                if (deathHandler != null)
                {
                    deathHandler.Respawn();
                    return;
                }
            }

            RestartScene();
        }

        public void RefreshSurvivalDisplay()
        {
            if (survivalStats != null)
                survivalStats.OnStatsChanged -= UpdateSurvivalUI;

            survivalStats = null;
            BindSurvivalStats();
            UpdateSurvivalUI();
        }

        public void HideDeathPopup()
        {
            Transform deathPanel = transform.Find("DeathPopupPanel");
            if (deathPanel != null)
                deathPanel.gameObject.SetActive(false);

            PlayerController playerController = FindAnyObjectByType<PlayerController>();
            if (playerController != null)
            {
                playerController.SetInventoryOpen(false);
                GameplayAudioUtility.EnsureListenerOnCamera(playerController.GameplayCamera);
            }
        }

        public void ShowDeathPopup()
        {
            Transform existing = transform.Find("DeathPopupPanel");
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                WireDeathPopupButtons(existing);
                ConfigurePopupCursor();
                return;
            }

            // Create Death Popup Panel
            GameObject deathPanel = new GameObject("DeathPopupPanel", typeof(RectTransform));
            deathPanel.transform.SetParent(this.transform, false);
            
            RectTransform panelRt = deathPanel.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.sizeDelta = Vector2.zero;

            Image bgImage = deathPanel.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.72f);
            bgImage.raycastTarget = true;

            ShiftUiTheme theme = ShiftUiTheme.Current;

            // Inner content panel
            GameObject contentPanel = new GameObject("ContentPanel", typeof(RectTransform));
            contentPanel.transform.SetParent(deathPanel.transform, false);
            RectTransform contentRt = contentPanel.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.5f, 0.5f);
            contentRt.anchorMax = new Vector2(0.5f, 0.5f);
            contentRt.pivot = new Vector2(0.5f, 0.5f);
            contentRt.sizeDelta = new Vector2(520f, 320f);
            contentRt.anchoredPosition = Vector2.zero;

            Image contentBg = contentPanel.AddComponent<Image>();
            if (theme != null)
                theme.ApplyPanelImage(contentBg, large: true);
            else
            {
                MenuUiBuilder.ApplyUiSprite(contentBg);
                contentBg.color = new Color(0.08f, 0.08f, 0.1f, 0.95f);
            }

            // Create title text "GAME OVER"
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform));
            titleObj.transform.SetParent(contentPanel.transform, false);
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            if (theme != null)
                theme.ApplyFont(titleText, bold: true);
            else
                TmpUiHelper.ApplyDefaultFont(titleText);
            titleText.text = "GAME OVER";
            titleText.fontSize = 64f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = theme != null ? theme.negativeColor : new Color(0.9f, 0.1f, 0.1f, 1f);
            titleText.alignment = TextAlignmentOptions.Center;
            
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 0.7f);
            titleRt.anchorMax = new Vector2(0.5f, 0.7f);
            titleRt.sizeDelta = new Vector2(600f, 100f);
            titleRt.anchoredPosition = Vector2.zero;

            // Create retry button
            GameObject retryObj = CreateStyledButton(contentPanel.transform, "RetryButton", "RETRY", new Vector2(0f, 20f));
            Button retryBtn = retryObj.GetComponent<Button>();
            retryBtn.onClick.AddListener(RespawnPlayer);

            // Create exit button
            GameObject exitObj = CreateStyledButton(contentPanel.transform, "ExitButton", "END GAME", new Vector2(0f, -60f));
            Button exitBtn = exitObj.GetComponent<Button>();
            exitBtn.onClick.AddListener(QuitGame);

            ConfigurePopupCursor();
        }

        private void WireDeathPopupButtons(Transform deathPanel)
        {
            Button retryBtn = deathPanel.Find("RetryButton")?.GetComponent<Button>();
            if (retryBtn != null)
            {
                retryBtn.onClick.RemoveAllListeners();
                retryBtn.onClick.AddListener(RespawnPlayer);
            }

            Button exitBtn = deathPanel.Find("ExitButton")?.GetComponent<Button>();
            if (exitBtn != null)
            {
                exitBtn.onClick.RemoveAllListeners();
                exitBtn.onClick.AddListener(QuitGame);
            }
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ConfigurePopupCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            PlayerController pc = FindAnyObjectByType<PlayerController>();
            if (pc != null) pc.SetInventoryOpen(true);

            CameraController cam = FindAnyObjectByType<CameraController>();
            if (cam != null) cam.SetInventoryOpen(true);
        }

        private GameObject CreateStyledButton(Transform parent, string name, string labelText, Vector2 pos)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform));
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.4f);
            rt.anchorMax = new Vector2(0.5f, 0.4f);
            rt.sizeDelta = new Vector2(220f, 50f);
            rt.anchoredPosition = pos;

            Image img = btnObj.AddComponent<Image>();
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.ApplyPanelImage(img, large: false, alphaMultiplier: 0.98f);
            else
                img.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            Button btn = btnObj.AddComponent<Button>();
            
            ColorBlock cb = btn.colors;
            Color normal = theme != null ? theme.backgroundColor : new Color(0.2f, 0.2f, 0.2f, 1f);
            cb.normalColor = normal;
            cb.highlightedColor = theme != null
                ? new Color(theme.primaryColor.r, theme.primaryColor.g, theme.primaryColor.b, 0.35f)
                : new Color(0.35f, 0.35f, 0.35f, 1f);
            cb.pressedColor = theme != null
                ? new Color(theme.primaryColor.r * 0.5f, theme.primaryColor.g * 0.5f, theme.primaryColor.b * 0.5f, 0.85f)
                : new Color(0.15f, 0.15f, 0.15f, 1f);
            btn.colors = cb;

            // Add text child
            GameObject txtObj = new GameObject("Text", typeof(RectTransform));
            txtObj.transform.SetParent(btnObj.transform, false);

            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            if (theme != null)
                theme.ApplyFont(tmp, semiBold: true);
            else
                TmpUiHelper.ApplyDefaultFont(tmp);
            tmp.text = labelText;
            tmp.fontSize = 20f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = theme != null ? theme.primaryColor : Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            return btnObj;
        }

        private void OnDestroy()
        {
            if (survivalStats != null)
                survivalStats.OnStatsChanged -= UpdateSurvivalUI;
        }
    }
}