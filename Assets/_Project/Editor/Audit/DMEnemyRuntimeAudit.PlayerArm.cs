#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Invector;
using Invector.vCharacterController;
using Invector.vMelee;
using Invector.vShooter;
using Project.AI;
using Project.AI.Invector;
using Project.Combat;
using Project.Core;
using Project.Data;
using Project.Inventory;
using Project.Progression;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace Project.EditorTools.Audit
{
    public static partial class DMEnemyRuntimeAudit
    {
        // =============================================================== arm the player
        // Fresh-start (menu bypass) players carry no weapon, so the injected left-click / melee probes need one. The sword is
        // put in a weapon hotbar slot through the normal inventory API and drawn through EquipmentController, in memory
        // only: the audit never saves (no save-file / slot changes, no scene save).
        private const string PlayerSwordName = "weap2_sword";

        private static string ArmPlayerWithSword()
        {
            ItemData sword = null;
            foreach (string g in AssetDatabase.FindAssets(PlayerSwordName + " t:ItemData"))
            {
                var it = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(g));
                if (it != null && it.name == PlayerSwordName) { sword = it; break; }
            }

            return sword == null ? "ItemData '" + PlayerSwordName + "' not found" : ArmPlayerWithItem(sword);
        }

        /// <summary>Puts ANY weapon item (melee or ranged) in a player weapon hotbar slot via the inventory API and draws it. Returns "" on success.</summary>
        private static string ArmPlayerWithItem(ItemData sword)
        {
            try
            {
                EnsureEquipLevel(sword);
                InventorySystem inv = _player.GetComponentInChildren<InventorySystem>(true) ?? Object.FindAnyObjectByType<InventorySystem>();
                EquipmentController eq = _player.GetComponentInChildren<EquipmentController>(true) ?? Object.FindAnyObjectByType<EquipmentController>();
                if (inv == null || eq == null) return "player has no InventorySystem/EquipmentController";

                int weaponSlot = -1;
                for (int i = 0; i < 4; i++)
                    if (eq.GetHotbarItem(eq.GetWeaponHotbarSlot(i)) == sword) { weaponSlot = i; break; }
                if (weaponSlot < 0)
                    for (int i = 0; i < 4; i++)
                        if (eq.GetHotbarItem(eq.GetWeaponHotbarSlot(i)) == null) { weaponSlot = i; break; }
                if (weaponSlot < 0) weaponSlot = 0;

                int hot = inv.HotbarStartIndex + eq.GetWeaponHotbarSlot(weaponSlot);
                if (inv.GetItemAt(hot) != sword)
                {
                    inv.AddItem(sword, 1, false);
                    for (int i = 0; i < inv.inventorySize; i++)
                        if (inv.GetItemAt(i) == sword) { inv.MoveOrMergeSlots(i, hot); break; }
                }

                if (inv.GetItemAt(hot) == sword && sword.defaultAmmoItem != null && sword.IsRangedWeapon)
                    inv.AddItem(sword.defaultAmmoItem, 90, false); // in-memory ammo for ranged player weapons

                if (inv.GetItemAt(hot) != sword) return "could not place the sword in weapon hotbar slot " + weaponSlot + " (CanAcceptItemAt/level gate refused?)";
                if (eq.ActiveWeaponSlot != weaponSlot || !eq.IsWeaponDrawn || eq.EquippedItem != sword)
                {
                    eq.SelectWeaponSlot(weaponSlot);
                    if (!eq.IsWeaponDrawn || eq.EquippedItem != sword) eq.SelectWeaponSlot(weaponSlot);
                }

                SyncHotCrossFocus(eq.GetWeaponHotbarSlot(weaponSlot));
                _guardItem = sword; _guardArmedAt = Time.time;
                return eq.EquippedItem == sword && eq.IsWeaponDrawn ? "" : $"sword in hotbar but not drawn (equipped={eq.EquippedItem}, drawn={eq.IsWeaponDrawn})";
            }
            catch (Exception e) { return "exception: " + e.Message; }
        }

        // Some weapons (2 Hander, Death Axe, weap_two_handed) carry a level gate (LevelUnlockUtility.PassesEquipGate). The audit raises the
        // in-memory level just enough to equip them (DevSetLevel) and puts level + skill points back in RestoreAuditState().
        // ---- weapon guard: records (and repairs) any time the drawn weapon changes without the audit asking for it
        private static ItemData _guardItem;
        private static float _guardArmedAt, _lastTeleport = -99f;
        private static bool _guardPause;
        private static int _equipReverts;
        private static readonly List<string> _revertNotes = new List<string>();

        // The Hot Cross HUD keeps its OWN weapon focus (DMUiToolkitHotCross.WeaponLocalIndex). EquipmentController.SelectWeaponSlot does not update it,
        // and PioneerShooterMeleeInput (RMB / LT aim) and OpticsController re-select that stale slot. Real players change it via Tab / number keys,
        // which call NotifyWeaponLocalIndex, so the harness must do the same or the weapon snaps back to the first one armed at game start.
        private static void SyncHotCrossFocus(int hotbarLocalSlot)
        {
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type t = null;
                    try { t = asm.GetTypes().FirstOrDefault(x => x.Name == "DMUiToolkitHotCross"); } catch { }
                    if (t == null) continue;
                    var m = t.GetMethod("NotifyWeaponLocalIndex", BindingFlags.Public | BindingFlags.Static);
                    if (m != null) m.Invoke(null, new object[] { hotbarLocalSlot });
                    return;
                }
            }
            catch { /* best effort */ }
        }

        private static void WeaponGuardTick()
        {
            if (_guardItem == null || _guardPause || _player == null) return;
            if (Time.time - _guardArmedAt < 1.2f) return;
            var eq = _player.GetComponentInChildren<EquipmentController>(true);
            if (eq == null) return;
            if (eq.EquippedItem == _guardItem && eq.IsWeaponDrawn) return;
            string was = eq.EquippedItem != null ? eq.EquippedItem.name : "none";
            _equipReverts++;
            if (_revertNotes.Count < 20)
                _revertNotes.Add($"wanted '{_guardItem.name}', found '{was}' (drawn {eq.IsWeaponDrawn}, active slot {eq.ActiveWeaponSlot}) {Time.time - _lastTeleport:0.0}s after the last teleport, {Time.time - _guardArmedAt:0.0}s after arming");
            ItemData want = _guardItem;
            ArmPlayerWithItem(want);
        }

        /// <summary>Teleport first, let it settle, THEN equip and verify (equipping before a teleport has been seen to revert).</summary>
        private static IEnumerator EquipAfterTeleport(ItemData item, Vector3 pos, Vector3 lookAt)
        {
            PlacePlayer(pos, lookAt);
            yield return null; yield return null;
            yield return new WaitGame(0.35f);
            string err = ArmPlayerWithItem(item);
            yield return new WaitGame(0.6f);
            var eq = _player.GetComponentInChildren<EquipmentController>(true);
            if (err.Length == 0 && eq != null && (eq.EquippedItem != item || !eq.IsWeaponDrawn)) { ArmPlayerWithItem(item); yield return new WaitGame(0.6f); }
        }

        // Scenario: does the equipped weapon survive a teleport? (investigates the "weapon reverts to the first one armed" report)
        private static IEnumerator ScnEquipTeleport(GameObject prefab, DMAuditEnemyResult R)
        {
            ItemData first = AllWeaponItems().FirstOrDefault(i => i.name == PlayerSwordName);
            ItemData second = AllWeaponItems().FirstOrDefault(i => i.IsRangedWeapon && !i.isMiningTool) ?? AllWeaponItems().FirstOrDefault(i => i.itemType == ItemType.MeleeWeapon && i != first);
            var eq = _player.GetComponentInChildren<EquipmentController>(true);
            if (first == null || second == null || eq == null) { R.Add("Equip", "equip.setup", "Equip/teleport probe setup", "WARN", "weapons or EquipmentController missing", ""); yield break; }
            _guardPause = true;
            ArmPlayerWithItem(first); yield return WaitMin(0.3f, () => eq.EquippedItem == first, 1.0f);
            ArmPlayerWithItem(second); yield return WaitMin(0.3f, () => eq.EquippedItem == second, 1.2f);
            bool armed = eq.EquippedItem == second;
            Vector3 p = Ground(_origin + new Vector3(0f, 0f, -30f));
            PlacePlayer(p, p + Vector3.forward * 5f);
            float tTele = Time.time, changeAt = -1f; string now = eq.EquippedItem != null ? eq.EquippedItem.name : "none";
            while (Time.time - tTele < 4f)
            {
                string cur = eq.EquippedItem != null ? eq.EquippedItem.name : "none";
                if (cur != second.name && changeAt < 0f) { changeAt = Time.time - tTele; now = cur; }
                yield return null;
            }

            bool kept = changeAt < 0f && eq.IsWeaponDrawn;
            R.Add("Equip", "equip.teleport", "Equipped weapon survives a teleport", armed ? (kept ? "PASS" : "FAIL") : "WARN",
                $"armed '{second.name}' after '{first.name}' (armed ok {armed}); after teleport: {(changeAt >= 0f ? "changed to '" + now + "' after " + changeAt.ToString("0.00") + "s" : "unchanged for 4s")}, drawn {eq.IsWeaponDrawn}, active slot {eq.ActiveWeaponSlot}",
                kept ? "" : "equipment re-selects the first weapon after the player is moved: look at EquipmentController / PioneerShooterMeleeInput re-applying hotbar focus (Hot Cross WeaponLocalIndex) on a position jump, a ground/respawn re-sync, or PlayerInvectorRuntimeSetup re-running weapon setup");
            _guardPause = false;
            ArmPlayerWithSword();
            yield return new WaitGame(1.0f);
        }

        private static int _origLevel = -1;
        private static object _origPoints;

        private static void EnsureEquipLevel(ItemData item)
        {
            try
            {
                int need = Mathf.Max(item.requiredLevelToEquip, item.requiredLevelToUse);
                var pm = Object.FindAnyObjectByType<PlayerProgressionManager>();
                if (pm == null || need <= 1 || pm.Level >= need) return;
                if (_origLevel < 0)
                {
                    _origLevel = pm.Level;
                    _origPoints = Mem(pm, "unspentSkillPoints");
                    _run.environment.Add($"Player level raised in memory from {_origLevel} for equip gates (restored at the end; nothing saved)");
                }

                pm.DevSetLevel(need);
            }
            catch { /* gate stays, equip row reports it */ }
        }

        private static void RestoreAuditState()
        {
            try
            {
                var pm = Object.FindAnyObjectByType<PlayerProgressionManager>();
                if (pm != null && _origLevel > 0)
                {
                    pm.DevSetLevel(_origLevel);
                    if (_origPoints != null)
                    {
                        FieldInfo f = typeof(PlayerProgressionManager).GetField("unspentSkillPoints", BFA);
                        if (f != null) f.SetValue(pm, _origPoints);
                    }
                }
            }
            catch { /* best effort */ }

            if (_equipReverts > 0)
                _run.environment.Add($"WEAPON REVERTS: the drawn weapon changed on its own {_equipReverts} time(s) during the run (the harness re-armed it): " + string.Join(" || ", _revertNotes));
            _guardItem = null; _guardPause = false; _equipReverts = 0; _revertNotes.Clear();
            _origLevel = -1; _origPoints = null;
            try { ReleaseAllInput(); } catch { /* ignore */ }
            AudioProbe.End();
            _camFollow = true;
            _zoomNet = 0;
        }

        private static IEnumerator ArmPlayerRoutine()
        {
            string err = "";
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                err = ArmPlayerWithSword();
                yield return new WaitGame(1.5f);
                var pm = _player.GetComponent<vMeleeManager>();
                if (err.Length == 0 && pm != null && pm.rightWeapon != null)
                {
                    _run.environment.Add($"Player armed for the audit: {PlayerSwordName} drawn (vMeleeManager.rightWeapon = {pm.rightWeapon.name}); in-memory only, nothing saved");
                    yield break;
                }

                if (err.Length == 0) err = "sword equipped but vMeleeManager.rightWeapon still null";
            }

            _run.environment.Add("Player could NOT be armed (" + err + "); the injected melee probe will be skipped");
        }
    }
}
#endif
