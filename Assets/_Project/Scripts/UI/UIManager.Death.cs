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
                existing.SetAsLastSibling();
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
            MenuUiBuilder.ApplyUiSprite(contentBg);
            DarkMatterGenesisUiPalette.ApplyPanelShellBackground(contentBg, 0.98f);
            DarkMatterGenesisUiPalette.ApplyFuchsiaTrim(contentPanel);

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
            titleText.color = DarkMatterGenesisUiPalette.WarningText;
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

            deathPanel.transform.SetAsLastSibling();
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
            MenuUiBuilder.ApplyUiSprite(img);
            img.color = DarkMatterGenesisUiPalette.ButtonNormal;
            DarkMatterGenesisUiPalette.ApplyFuchsiaTrim(btnObj);

            Button btn = btnObj.AddComponent<Button>();
            DarkMatterGenesisUiPalette.StylePrimaryButton(btn, img);

            // Add text child
            GameObject txtObj = new GameObject("Text", typeof(RectTransform));
            txtObj.transform.SetParent(btnObj.transform, false);

            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.ApplyFont(tmp, semiBold: true);
            else
                TmpUiHelper.ApplyDefaultFont(tmp);
            tmp.text = labelText;
            tmp.fontSize = 20f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = DarkMatterGenesisUiPalette.BodyText;
            tmp.alignment = TextAlignmentOptions.Center;

            return btnObj;
        }

    }
}
