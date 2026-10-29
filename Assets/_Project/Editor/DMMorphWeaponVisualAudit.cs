#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Project.Core;
using Project.Data;
using Project.Inventory;
using Project.Prototypes.Weapons;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace Project.EditorTools.Audit
{
    /// <summary>
    /// SMALL visual/functional morph audit: bypass menu, keep terrain, equip Morph Blade,
    /// cycle every form, capture game-view screenshots. No combat / no enemies / no firing.
    /// </summary>
    public static class DMMorphWeaponVisualAudit
    {
        const string KeyPending = "DMMorphVisualAudit.Pending";
        const string ShotFolder = "Assets/_Project/Art/Debug/MorphShots";
        static bool _running;
        static IEnumerator _co;
        static GameObject _player;
        static readonly List<string> Log = new List<string>();
        static readonly List<string> ShotPaths = new List<string>();

        [InitializeOnLoadMethod]
        static void Boot()
        {
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorApplication.playModeStateChanged += OnPlayMode;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            if (SessionState.GetBool(KeyPending, false) && EditorApplication.isPlaying && !_running)
                EditorApplication.delayCall += TryStart;
        }

        [MenuItem(DarkMatterGenesisEditorMenus.Combat + "Audit Morph Weapon Visuals (Play)", false, DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Menu_Band)]
        public static void RunFromMenu()
        {
            SessionState.SetBool(KeyPending, true);
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorApplication.playModeStateChanged += OnPlayMode;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            if (EditorApplication.isPlaying)
                EditorApplication.isPlaying = false;
            else
                EditorApplication.isPlaying = true;
        }

        static void OnPlayMode(PlayModeStateChange s)
        {
            if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(KeyPending, false))
                EditorApplication.delayCall += TryStart;
            if (s == PlayModeStateChange.EnteredEditMode)
            {
                _running = false;
                _co = null;
                SessionState.SetBool(KeyPending, false);
            }
        }

        static void TryStart()
        {
            if (_running || !EditorApplication.isPlaying || !SessionState.GetBool(KeyPending, false))
                return;
            _running = true;
            _stack.Clear();
            Log.Clear();
            ShotPaths.Clear();
            Directory.CreateDirectory(ShotFolder);
            Debug.Log("[DMMorphAudit] TryStart - coroutine armed");
            _co = Main();
        }

        static readonly Stack<IEnumerator> _stack = new Stack<IEnumerator>();

        static void Tick()
        {
            if (!_running) return;
            if (_co != null && _stack.Count == 0)
            {
                _stack.Push(_co);
                _co = null;
            }
            if (_stack.Count == 0) return;
            try
            {
                var top = _stack.Peek();
                if (!top.MoveNext())
                {
                    _stack.Pop();
                    if (_stack.Count == 0)
                    {
                        _running = false;
                        SessionState.SetBool(KeyPending, false);
                        Debug.Log("[DMMorphAudit] DONE shots=" + ShotPaths.Count + "\n" + string.Join("\n", Log));
                        if (EditorApplication.isPlaying)
                            EditorApplication.isPlaying = false;
                    }
                    return;
                }
                if (top.Current is IEnumerator nested)
                    _stack.Push(nested);
            }
            catch (Exception e)
            {
                Log.Add("FATAL: " + e);
                Debug.LogError("[DMMorphAudit] " + e);
                _stack.Clear();
                _co = null;
                _running = false;
                SessionState.SetBool(KeyPending, false);
                if (EditorApplication.isPlaying)
                    EditorApplication.isPlaying = false;
            }
        }

        static IEnumerator Main()
        {
            Log.Add("start keepTerrain=true noCombat=true");
            yield return Bypass();
            _player = GameObject.FindGameObjectWithTag("Player");
            if (_player == null)
            {
                Log.Add("FAIL no player");
                yield break;
            }

            // Keep Gaia terrain ON (do not disable Terrain components).
            int terrains = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Exclude).Length;
            Log.Add("terrains active=" + terrains);

            ItemData blade = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/Melee/MorphBlade.asset");
            if (blade == null)
            {
                Log.Add("FAIL missing MorphBlade ItemData");
                yield break;
            }

            string armBlade = Arm(blade);
            Log.Add("arm blade: " + (string.IsNullOrEmpty(armBlade) ? "ok" : armBlade));
            yield return Wait(1.2f);
            yield return CaptureForms(blade, "Blade", new[] { "Metal", "Light", "Plasma", "Ion", "Laser", "Cryo" });

            Log.Add("shots:");
            foreach (var p in ShotPaths) Log.Add("  " + p);
            File.WriteAllText(Path.Combine(ShotFolder, "audit_log.txt"), string.Join("\n", Log));
            AssetDatabase.Refresh();
        }

        static IEnumerator CaptureForms(ItemData item, string prefix, string[] expectedLabels)
        {
            DMWeaponMorphController morph = FindMorph(item);
            if (morph == null)
            {
                Log.Add("FAIL no DMWeaponMorphController for " + item.itemName);
                yield break;
            }

            // Start at form 0
            if (morph.FormIndex != 0)
                morph.BeginMorph(0);
            yield return WaitMorph(morph);

            for (int i = 0; i < expectedLabels.Length; i++)
            {
                if (morph.FormIndex != i)
                    morph.BeginMorph(i);
                yield return WaitMorph(morph);

                var profile = morph.ActiveProfile;
                string label = expectedLabels[i];
                string actual = profile != null ? profile.displayName : "?";
                bool linedUp = CheckVisual(morph, out string visNote);
                Log.Add($"{prefix}/{label}: formIndex={morph.FormIndex} display={actual} visual={(linedUp ? "ok" : visNote)}");

                // Also exercise TryCycleForm path once after first form (Y equivalent)
                if (i == 0)
                {
                    bool cycled = morph.TryCycleForm();
                    yield return WaitMorph(morph);
                    Log.Add($"{prefix} TryCycleForm after 0 -> index={morph.FormIndex} ok={cycled}");
                    morph.BeginMorph(0);
                    yield return WaitMorph(morph);
                }

                string file = $"{prefix}_{label}.png";
                string path = Path.Combine(ShotFolder, file).Replace('\\', '/');
                ScreenCapture.CaptureScreenshot(path);
                // Wait a couple frames for disk write
                yield return null;
                yield return null;
                yield return Wait(0.35f);
                ShotPaths.Add(path);
                Log.Add("screenshot " + path);

                if (i < expectedLabels.Length - 1)
                {
                    bool ok = morph.TryCycleForm();
                    Log.Add($"{prefix} cycle {i}->{i + 1} ok={ok}");
                    yield return WaitMorph(morph);
                }
            }
        }

        static bool CheckVisual(DMWeaponMorphController morph, out string note)
        {
            note = "";
            if (morph == null) { note = "null morph"; return false; }
            var profile = morph.ActiveProfile;
            if (profile == null) { note = "no profile"; return false; }

            int activeParts = 0;
            foreach (Transform t in morph.GetComponentsInChildren<Transform>(true))
            {
                if (t == morph.transform) continue;
                if (!t.gameObject.activeInHierarchy) continue;
                if (t.name.StartsWith("Part_", StringComparison.Ordinal) ||
                    t.name.StartsWith("Blade_", StringComparison.Ordinal) ||
                    t.name == "Hilt" || t.name == "Handle")
                    activeParts++;
            }
            if (activeParts < 1) { note = "no active parts"; return false; }

            if (morph.Kind == DMWeaponMorphKind.MeleeBlade)
            {
                string want = "Blade_" + profile.bladeType;
                Transform blade = null;
                foreach (Transform t in morph.GetComponentsInChildren<Transform>(true))
                    if (t.name == want) { blade = t; break; }
                if (blade == null || !blade.gameObject.activeInHierarchy)
                {
                    note = "blade child " + want + " inactive";
                    return false;
                }
            }
            return true;
        }

        static DMWeaponMorphController FindMorph(ItemData item)
        {
            var all = Object.FindObjectsByType<DMWeaponMorphController>(FindObjectsInactive.Include);
            bool wantRanged = item != null && (item.IsRangedWeapon || item.itemType == ItemType.RangedWeapon);
            bool wantMelee = item != null && item.itemType == ItemType.MeleeWeapon;
            DMWeaponMorphController boundVisual = null, boundAny = null, kindVisual = null, kindAny = null;
            var diag = new System.Text.StringBuilder("FindMorph candidates:");
            foreach (var m in all)
            {
                if (m == null) continue;
                bool isVisual = m.name.IndexOf("PioneerVisual_", StringComparison.Ordinal) >= 0
                    || (m.transform.parent != null && m.transform.root.name.IndexOf("Player", StringComparison.OrdinalIgnoreCase) >= 0);
                bool bound = m.BoundItem == item;
                bool kindOk = (wantRanged && m.Kind == DMWeaponMorphKind.Ranged)
                    || (wantMelee && m.Kind == DMWeaponMorphKind.MeleeBlade)
                    || (!wantRanged && !wantMelee);
                diag.Append(" [").Append(m.name).Append(" kind=").Append(m.Kind)
                    .Append(" bound=").Append(m.BoundItem != null ? m.BoundItem.name : "null")
                    .Append(" vis=").Append(isVisual).Append(" kindOk=").Append(kindOk).Append("]");
                if (!kindOk) continue;
                if (bound && isVisual) boundVisual = m;
                else if (bound) boundAny = m;
                else if (isVisual && kindVisual == null) kindVisual = m;
                else if (kindAny == null) kindAny = m;
            }
            var pick = boundVisual ?? boundAny ?? kindVisual ?? kindAny;
            Log.Add(diag.ToString());
            Log.Add("FindMorph pick=" + (pick != null ? pick.name + " kind=" + pick.Kind + " bound=" + (pick.BoundItem != null ? pick.BoundItem.name : "null") : "null"));
            return pick;
        }

        static string Arm(ItemData item)
        {
            try
            {
                var inv = _player.GetComponentInChildren<InventorySystem>(true) ?? Object.FindAnyObjectByType<InventorySystem>();
                var eq = _player.GetComponentInChildren<EquipmentController>(true) ?? Object.FindAnyObjectByType<EquipmentController>();
                if (inv == null || eq == null) return "no inv/eq";

                int weaponSlot = -1;
                for (int i = 0; i < 4; i++)
                    if (eq.GetHotbarItem(eq.GetWeaponHotbarSlot(i)) == item) { weaponSlot = i; break; }
                if (weaponSlot < 0)
                    for (int i = 0; i < 4; i++)
                        if (eq.GetHotbarItem(eq.GetWeaponHotbarSlot(i)) == null) { weaponSlot = i; break; }
                if (weaponSlot < 0) weaponSlot = 0;

                int hot = inv.HotbarStartIndex + eq.GetWeaponHotbarSlot(weaponSlot);
                if (inv.GetItemAt(hot) != item)
                {
                    inv.AddItem(item, 1, false);
                    for (int i = 0; i < inv.inventorySize; i++)
                        if (inv.GetItemAt(i) == item) { inv.MoveOrMergeSlots(i, hot); break; }
                }
                if (inv.GetItemAt(hot) == item && item.defaultAmmoItem != null && item.IsRangedWeapon)
                    inv.AddItem(item.defaultAmmoItem, 30, false);

                eq.SelectWeaponSlot(weaponSlot);
                if (!eq.IsWeaponDrawn || eq.EquippedItem != item)
                    eq.SelectWeaponSlot(weaponSlot);

                return eq.EquippedItem == item && eq.IsWeaponDrawn
                    ? ""
                    : $"not drawn (eq={eq.EquippedItem}, drawn={eq.IsWeaponDrawn})";
            }
            catch (Exception e) { return e.Message; }
        }

        static IEnumerator Bypass()
        {
            float until = Time.realtimeSinceStartup + 40f;
            while (LoaderBlocking() && Time.realtimeSinceStartup < until)
                yield return null;
            for (int attempt = 1; attempt <= 4; attempt++)
            {
                ApplyBypass();
                float t2 = Time.realtimeSinceStartup + 1.5f;
                while (Time.realtimeSinceStartup < t2) yield return null;
                string problems = VerifyGameplay();
                Log.Add($"bypass attempt {attempt}: {(problems.Length == 0 ? "clean" : problems)}");
                if (problems.Length == 0) yield break;
            }
        }

        static void ApplyBypass()
        {
            try
            {
                Type mmType = FindType("MainMenuController");
                Object menu = mmType != null ? Object.FindAnyObjectByType(mmType, FindObjectsInactive.Include) : null;
                if (menu != null)
                {
                    mmType.GetMethod("HideMenuChrome", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(menu, null);
                    var pause = mmType.GetProperty("IsPauseOverlayActive", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (pause != null && (bool)pause.GetValue(null))
                        mmType.GetMethod("InvokeResumeFromPause", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(menu, null);
                }
                GameSession.MarkStarted();
                if (menu != null)
                {
                    mmType.GetMethod("SetGameWorldPaused", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(menu, new object[] { false });
                    mmType.GetMethod("ReleaseGameplayInputCapture", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(menu, null);
                    mmType.GetMethod("RefreshGameplayCamera", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(null, null);
                    mmType.GetMethod("RestoreGameplayUi", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(menu, null);
                }
                FindType("LoadingOverlayController")?.GetMethod("ReleaseOpaqueCover", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(null, null);
                FindType("MainCanvasFlow")?.GetMethod("Refresh", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(null, null);
                foreach (var d in Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>(FindObjectsInactive.Exclude))
                {
                    if (d == null || (d.name != "UITK_Loading" && d.name != "UITK_MainMenu") || d.rootVisualElement == null) continue;
                    foreach (var c in d.rootVisualElement.Children())
                        if (c.resolvedStyle.display != UnityEngine.UIElements.DisplayStyle.None)
                            c.style.display = UnityEngine.UIElements.DisplayStyle.None;
                }
                Time.timeScale = 1f;
            }
            catch (Exception e) { Log.Add("bypass ex: " + e.Message); }
        }

        static string VerifyGameplay()
        {
            var p = new List<string>();
            if (!GameSession.HasStarted) p.Add("GameSession not started");
            if (Time.timeScale <= 0.01f) p.Add("timeScale 0");
            if (LoaderBlocking()) p.Add("loader blocking");
            Type mt = FindType("DMUiToolkitMainMenu");
            var vis = mt?.GetProperty("IsVisible", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (vis != null && (bool)vis.GetValue(null)) p.Add("main menu visible");
            if (GameObject.FindGameObjectWithTag("Player") == null) p.Add("no player");
            return string.Join(", ", p);
        }

        static bool LoaderBlocking()
        {
            Type lo = FindType("LoadingOverlayController");
            var p = lo != null ? lo.GetProperty("IsBlockingMenu", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) : null;
            return p != null && (bool)p.GetValue(null);
        }

        static Type FindType(string name)
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var t in a.GetTypes())
                        if (t.Name == name) return t;
                }
                catch { }
            }
            return null;
        }

        static IEnumerator Wait(float s)
        {
            float t = Time.realtimeSinceStartup + s;
            while (Time.realtimeSinceStartup < t) yield return null;
        }

        static IEnumerator WaitMorph(DMWeaponMorphController morph)
        {
            float t = Time.realtimeSinceStartup + 1.2f;
            while (morph != null && morph.IsMorphing && Time.realtimeSinceStartup < t)
                yield return null;
            yield return Wait(0.15f);
        }
    }
}
#endif