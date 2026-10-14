using System.Collections.Generic;
using Project.AI;
using Project.Core;
using Project.Survival;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Combat
{
    /// <summary>
    /// 2026-10-13 (i): typed "godmode" cheat (toggle) + respawn fallback. God mode = DMDevCommandState.GodMode: player unkillable
    /// (existing dev path), enemies held at 1 HP, infinite ammo. God mode OFF: dead enemies / the player respawn after a delay.
    /// All settings in DM_CombatCoreProfile "God mode cheat + respawn fallback". Shows a small toast on toggle.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class DMGodModeCheat : MonoBehaviour
    {
        private static DMGodModeCheat s_instance;
        private readonly System.Text.StringBuilder _typed = new System.Text.StringBuilder();
        private Keyboard _hooked;
        private string _toast;
        private float _toastUntil;
        private float _nextScan;
        private readonly Dictionary<EnemyHealth, float> _deadSince = new Dictionary<EnemyHealth, float>();
        private float _playerDeadSince = -1f;
        private GUIStyle _style;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (s_instance != null) return;
            var go = new GameObject("DM_GodModeCheat");
            DontDestroyOnLoad(go);
            s_instance = go.AddComponent<DMGodModeCheat>();
        }

        /// <summary>Set god mode from code (audit harness); shows the toast when it changes.</summary>
        public static void SetGodMode(bool on, bool toast = true)
        {
            bool was = DMDevCommandState.GodMode;
            DMDevCommandState.GodMode = on;
            if (toast && was != on && s_instance != null)
                s_instance.ShowToast(on ? "GOD MODE ON" : "GOD MODE OFF");
        }

        private void OnDisable() { Unhook(); }

        private void Hook()
        {
            Keyboard k = Keyboard.current;
            if (k == _hooked) return;
            Unhook();
            _hooked = k;
            if (_hooked != null) _hooked.onTextInput += OnText;
        }

        private void Unhook()
        {
            if (_hooked != null) _hooked.onTextInput -= OnText;
            _hooked = null;
        }

        private void OnText(char c)
        {
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (p != null && !p.godModeCheatEnabled) return;
            string code = p != null && !string.IsNullOrEmpty(p.godModeCheatCode) ? p.godModeCheatCode.ToLowerInvariant() : "godmode";
            _typed.Append(char.ToLowerInvariant(c));
            if (_typed.Length > code.Length) _typed.Remove(0, _typed.Length - code.Length);
            if (_typed.ToString() == code)
            {
                _typed.Length = 0;
                SetGodMode(!DMDevCommandState.GodMode);
            }
        }

        private void ShowToast(string text)
        {
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            _toast = text;
            _toastUntil = Time.unscaledTime + (p != null ? p.godModeToastSeconds : 2f);
        }

        private void Update()
        {
            Hook();
            if (Time.unscaledTime < _nextScan) return;
            _nextScan = Time.unscaledTime + 0.5f;
            if (DMDevCommandState.GodMode) { _deadSince.Clear(); _playerDeadSince = -1f; return; }
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (p == null) return;

            if (p.respawnEnemiesAfterDeath)
            {
                foreach (EnemyHealth h in FindObjectsByType<EnemyHealth>(FindObjectsInactive.Include))
                {
                    if (h == null) continue;
                    if (!h.IsDead || h.ShouldRespawn) { _deadSince.Remove(h); continue; } // own respawn timer handles it
                    if (!_deadSince.TryGetValue(h, out float t0)) { _deadSince[h] = Time.time; continue; }
                    if (Time.time - t0 < p.enemyRespawnDelay) continue;
                    _deadSince.Remove(h);
                    if (!h.gameObject.activeSelf) h.gameObject.SetActive(true);
                    h.ForceRespawn();
                }
            }

            if (p.respawnPlayerAfterDeath)
            {
                GameObject pl = PlayerLocator.FindPlayerObject();
                SurvivalStats s = pl != null ? pl.GetComponent<SurvivalStats>() : null;
                if (s == null || !s.IsDead) { _playerDeadSince = -1f; return; }
                if (_playerDeadSince < 0f) { _playerDeadSince = Time.unscaledTime; return; }
                if (Time.unscaledTime - _playerDeadSince < p.playerRespawnDelay) return;
                _playerDeadSince = -1f;
                Project.UI.UIManager ui = FindAnyObjectByType<Project.UI.UIManager>();
                if (ui != null) ui.RespawnPlayer();
                else pl.GetComponent<Project.Player.PlayerDeathHandler>()?.Respawn();
            }
        }

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(_toast) || Time.unscaledTime > _toastUntil) return;
            if (_style == null)
                _style = new GUIStyle(GUI.skin.box) { fontSize = 18, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            const float w = 240f, h = 40f;
            GUI.Box(new Rect((Screen.width - w) * 0.5f, Screen.height * 0.18f, w, h), _toast, _style);
        }
    }
}
