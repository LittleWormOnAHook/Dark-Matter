#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Project.AI;
using Project.Combat;
using Project.Data;
using Project.Inventory;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace Project.EditorTools.Audit
{
    public static partial class DMEnemyRuntimeAudit
    {
        // =============================================================== ammo: fill clips, change ammo types
        // Same paths the game uses: InventorySystem (in memory), InventoryItemActions.TryEquipAmmoToActiveRangedWeapon (what the Hot Cross
        // X / ammo cycle calls) and WeaponAmmoState (magazine). Nothing is saved.

        private static List<ItemData> AllAmmoItems()
        {
            var list = new List<ItemData>();
            foreach (string guid in AssetDatabase.FindAssets("t:ItemData"))
            {
                var it = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));
                if (it != null && it.CountsAsAmmo) list.Add(it);
            }

            list.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            return list;
        }

        private static bool EquippedOk(ItemData item) { var eq = PlayerEq(); return eq != null && eq.EquippedItem == item && eq.IsWeaponDrawn; }

        private static InventorySystem PlayerInv() { return _player != null ? (_player.GetComponentInChildren<InventorySystem>(true) ?? Object.FindAnyObjectByType<InventorySystem>()) : null; }
        private static EquipmentController PlayerEq() { return _player != null ? (_player.GetComponentInChildren<EquipmentController>(true) ?? Object.FindAnyObjectByType<EquipmentController>()) : null; }

        private static WeaponAmmoState PlayerAmmoState()
        {
            var eq = PlayerEq();
            var inv = PlayerInv();
            WeaponAmmoState a = eq != null ? eq.GetComponent<WeaponAmmoState>() : null;
            if (a == null && inv != null) a = inv.GetComponent<WeaponAmmoState>();
            return a != null ? a : Object.FindAnyObjectByType<WeaponAmmoState>();
        }

        /// <summary>Hotbar-local index of the weapon slot holding `weapon`, or -1.</summary>
        private static int WeaponHotbarLocal(ItemData weapon)
        {
            var eq = PlayerEq();
            if (eq == null) return -1;
            for (int i = 0; i < 4; i++)
            {
                int local = eq.GetWeaponHotbarSlot(i);
                if (eq.GetHotbarItem(local) == weapon) return local;
            }

            return -1;
        }

        private static int FindInventorySlot(InventorySystem inv, ItemData item)
        {
            for (int i = 0; i < inv.inventorySize; i++)
                if (inv.GetItemAt(i) == item) return i;
            return -1;
        }

        /// <summary>Puts `count` rounds of `ammo` in the player inventory (in memory) and returns its absolute slot, or -1.</summary>
        private static Dictionary<ItemData, int> InvCounts()
        {
            var d = new Dictionary<ItemData, int>();
            var inv = PlayerInv();
            if (inv == null) return d;
            for (int i = 0; i < inv.inventorySize; i++)
            {
                ItemData it = inv.GetItemAt(i);
                if (it != null && !d.ContainsKey(it)) d[it] = inv.CountItem(it);
            }

            return d;
        }

        /// <summary>Removes whatever was added since `before` (loot granted by the audit), in memory only. Returns the number of items removed.</summary>
        private static int InvRemoveGranted(Dictionary<ItemData, int> before)
        {
            var inv = PlayerInv();
            if (inv == null || before == null) return 0;
            int removed = 0;
            var now = new HashSet<ItemData>();
            for (int i = 0; i < inv.inventorySize; i++) { ItemData it = inv.GetItemAt(i); if (it != null) now.Add(it); }
            foreach (ItemData it in now)
            {
                before.TryGetValue(it, out int had);
                int diff = inv.CountItem(it) - had;
                if (diff > 0 && inv.RemoveItem(it, diff)) removed += diff;
            }

            return removed;
        }

        private static int GiveAmmo(ItemData ammo, int count)
        {
            var inv = PlayerInv();
            if (inv == null) return -1;
            int slot = FindInventorySlot(inv, ammo);
            if (slot < 0)
            {
                inv.AddItem(ammo, count, false); slot = FindInventorySlot(inv, ammo);
                if (slot < 0)
                {   // inventory full of other ammo stacks from earlier cases: drop those (in-memory only) and retry
                    for (int i = 0; i < inv.inventorySize; i++)
                    {
                        ItemData it = inv.GetItemAt(i);
                        if (it != null && it != ammo && it.CountsAsAmmo) inv.RemoveItemAt(i, 100000);
                    }

                    inv.AddItem(ammo, count, false); slot = FindInventorySlot(inv, ammo);
                }
            }
            return slot;
        }

        /// <summary>Loads `ammo` into the drawn ranged weapon the way the Hot Cross ammo cycle does. Returns "" on success.</summary>
        private static string LoadAmmoLikeHotCross(ItemData weapon, ItemData ammo, int invSlot)
        {
            int local = WeaponHotbarLocal(weapon);
            if (local < 0) return "weapon is not on a player weapon hotbar slot";
            if (invSlot < 0) return "ammo is not in the inventory";
            Type iat = FindType("InventoryItemActions");
            Object actions = iat != null ? Object.FindAnyObjectByType(iat) : null;
            if (actions != null)
            {
                MethodInfo m = iat.GetMethod("TryEquipAmmoToActiveRangedWeapon", BFA);
                if (m != null)
                {
                    object[] args = m.GetParameters().Length == 2 ? new object[] { invSlot, false } : new object[] { invSlot };
                    try { if (m.Invoke(actions, args) is bool b && b) return ""; }
                    catch (Exception e) { return "TryEquipAmmoToActiveRangedWeapon threw " + (e.InnerException ?? e).Message; }
                }
            }

            var st = PlayerAmmoState();
            if (st == null) return "no WeaponAmmoState on the player";
            return st.TryEquipAmmoToWeaponSlot(local, invSlot) ? "" : "refused (weapon does not accept this ammo type, mining tool, or no room)";
        }

        private static int Loaded(ItemData weapon)
        {
            var st = PlayerAmmoState();
            int local = WeaponHotbarLocal(weapon);
            return st != null && local >= 0 ? st.GetLoadedAmmo(local) : -1;
        }

        private static ItemData LoadedItem(ItemData weapon)
        {
            var st = PlayerAmmoState();
            int local = WeaponHotbarLocal(weapon);
            return st != null && local >= 0 ? st.GetLoadedAmmoItem(local) : null;
        }

        /// <summary>Top up the magazine of `weapon` from the inventory (same call a reload makes). Returns rounds after.</summary>
        private static int FillClip(ItemData weapon)
        {
            var st = PlayerAmmoState();
            int local = WeaponHotbarLocal(weapon);
            if (st == null || local < 0) return -1;
            st.RefillMagazineFromInventory(local, weapon);
            return st.GetLoadedAmmo(local);
        }

        private static bool AmmoHasSpecialFx(ItemData a)
        {
            string n = a.name.ToLowerInvariant();
            if (n.Contains("laser") || n.Contains("ion") || n.Contains("plasma") || n.Contains("explosive")) return true;
            return a is DMAmmoFxProfile p && (p.enemyBlood.ToString() != "Auto" || p.enemyBurnMark.ToString() != "Auto" || p.spawnLaserBurn || p.delivery.ToString() != "Auto");
        }

        private static IEnumerator ScnAmmo(GameObject prefab, DMAuditEnemyResult R)
        {
            List<ItemData> ammos = AllAmmoItems();
            List<ItemData> weapons = AllWeaponItems().Where(w => w.IsRangedWeapon && !w.isMiningTool).ToList();
            R.Add("Ammo", "ammo.inventory", "Ammo items and ranged weapons found", "INFO",
                $"{ammos.Count} ammo item(s): {string.Join(", ", ammos.Select(a => a.name + "(" + a.ammoType + ")"))}; {weapons.Count} ranged weapon(s): {string.Join(", ", weapons.Select(w => w.name))}", "");
            if (PlayerInv() == null || PlayerAmmoState() == null)
            {
                R.Add("Ammo", "ammo.setup", "Player inventory + WeaponAmmoState available", "FAIL", "missing InventorySystem or WeaponAmmoState on the player", "player prefab/bootstrap lacks the ammo components");
                yield break;
            }

            // ONE enemy, ONE player placement; weapons are swapped in place.
            Cleanup();
            Live L = SpawnMatrixEnemy(prefab, "ammo");
            yield return SpawnReady(L);
            L.ai.enabled = false;
            L.trackPlayer = false;
            Vector3 pos = Ground(L.go.transform.position + FlatForward(L.go.transform) * 6f);
            PlacePlayer(pos, L.go.transform.position);
            yield return new WaitGame(0.3f);

            // (1) every ranged weapon with its DEFAULT ammo: the general tests (load, fill, fire, damage, aim, hit marks, empty, reload)
            int combos = 0;
            var defaults = new HashSet<ItemData>();
            foreach (ItemData weapon in weapons)
            {
                List<ItemData> fit = ammos.Where(a => weapon.AcceptsAmmoType(a.ammoType)).ToList();
                if (fit.Count == 0)
                {
                    R.Add("Ammo", "ammo." + weapon.name + ".none", $"'{weapon.name}' has compatible ammo items", "WARN", $"accepts {string.Join("/", weapon.compatibleAmmoTypes ?? new AmmoType[0])} but no ammo ItemData matches", "ammo ItemData missing for this weapon's compatible ammo types");
                    continue;
                }

                ItemData def = weapon.defaultAmmoItem != null && fit.Contains(weapon.defaultAmmoItem) ? weapon.defaultAmmoItem : fit[0];
                defaults.Add(def);
                combos++;
                yield return AmmoCase(L, R, weapon, def, true, pos);
            }

            // (2) ammo-specific tests: each OTHER ammo type once, on the first weapon that takes it (type-specific FX / rules only)
            var specific = ammos.Where(a => !defaults.Contains(a)).ToList();
            if (!FullMode) specific = specific.Where(AmmoHasSpecialFx).Take(4).ToList();
            int specN = 0;
            foreach (ItemData ammo in specific)
            {
                ItemData host = weapons.FirstOrDefault(w => w.AcceptsAmmoType(ammo.ammoType));
                if (host == null) { R.Add("Ammo", "ammo.spec." + ammo.name, $"ammo-specific: {ammo.name}", "WARN", "no ranged weapon accepts this ammo type", "unreachable ammo item"); continue; }
                specN++;
                yield return AmmoCase(L, R, host, ammo, false, pos);
            }

            // (3) the X ammo-cycle (specific to ammo switching), once
            foreach (ItemData weapon in weapons)
            {
                List<ItemData> fit = ammos.Where(a => weapon.AcceptsAmmoType(a.ammoType)).ToList();
                if (fit.Count >= 2) { yield return AmmoCycleCase(prefab, R, weapon, fit); break; }
            }

            R.Add("Ammo", "ammo.combos", "Ammo coverage", "INFO",
                $"{combos} weapon(s) tested with their default ammo (full: load, fill, fire, damage, marks, empty, reload); {specN} ammo-specific case(s) ({(FullMode ? "all non-default ammo types" : "Quick: only ammo types with special FX rules, max 4")}), each once on a host weapon (load, 2 shots, damage, marks, per-ammo FX rules); no weapon x ammo matrix. Mining tool excluded: it runs on Plasma Fuel charge.", "");
        }

        /// <summary>False when the ammo's own rules mean no mark is expected (useHitMarks off, or blood and burn marks both Never).</summary>
        private static bool MarksExpected(ItemData ammo)
        {
            if (!(ammo is DMAmmoFxProfile p)) return true;
            if (!p.useHitMarks) return false;
            return !(p.enemyBlood.ToString() == "Never" && p.enemyBurnMark.ToString() == "Never");
        }

        private static void AmmoFxRow(DMAuditEnemyResult R, string idp, string key, ItemData ammo, int marks0, int marks1, float lost)
        {
            if (!(ammo is DMAmmoFxProfile p)) { R.Add("Ammo", idp + ".fx", $"{key}: per-ammo FX rules", "INFO", "plain ItemData ammo (no DMAmmoFxProfile): default FX", ""); return; }
            bool neverBoth = p.enemyBlood.ToString() == "Never" && p.enemyBurnMark.ToString() == "Never";
            string rules = $"enemyBlood={p.enemyBlood}, enemyBurnMark={p.enemyBurnMark}, spawnLaserBurn={p.spawnLaserBurn}, delivery={p.delivery}, useHitMarks={p.useHitMarks}";
            string st = lost <= 0f ? "INFO" : (marks1 > marks0 || neverBoth || !p.useHitMarks ? "PASS" : "WARN");
            R.Add("Ammo", idp + ".fx", $"{key}: per-ammo FX rules (blood / burn / glow / explosive handling)", st,
                $"{rules}; hit marks {marks0} -> {marks1}, enemy hp lost {lost:0.0}",
                st == "WARN" ? "damage landed but no mark was stamped although this ammo's rules allow blood/burn marks (check DMAmmoFxProfile.surfaces / body-type entry)" : (st == "INFO" ? "no hit to evaluate the rules against" : (marks1 <= marks0 && !p.useHitMarks ? "useHitMarks is off on this ammo: no mark by design" : "")));
        }

        private static IEnumerator AmmoCase(Live L, DMAuditEnemyResult R, ItemData weapon, ItemData ammo, bool full, Vector3 pos)
        {
            string key = weapon.name + " + " + ammo.name;
            string idp = "ammo." + weapon.name + "." + ammo.name;
            EnemyTopUpNow(L);
            string armErr = ArmPlayerWithItem(weapon);
            yield return WaitMin(0.3f, () => EquippedOk(weapon), 1.5f);
            if (!EquippedOk(weapon)) yield return EquipAfterTeleport(weapon, pos, L.go.transform.position);
            string err = EquippedOk(weapon) ? "" : "could not equip: " + armErr;
            if (err.Length > 0) { R.Add("Ammo", idp + ".arm", $"{key}: player equips the weapon", "FAIL", err, "player cannot equip the weapon (level gate, slot refusal)"); yield break; }

            int slot = GiveAmmo(ammo, 90);
            string loadErr = LoadAmmoLikeHotCross(weapon, ammo, slot);
            yield return new WaitGame(0.3f);
            int cap = WeaponAmmoState.GetMagazineCapacity(weapon, ammo);
            int loaded0 = Loaded(weapon);
            bool typeOk = LoadedItem(weapon) == ammo;
            R.Add("Ammo", idp + ".load", $"{key}: ammo type loads (Hot Cross path)", loadErr.Length == 0 && typeOk && loaded0 > 0 ? "PASS" : "FAIL",
                $"loaded {loaded0}/{cap} of '{(LoadedItem(weapon) != null ? LoadedItem(weapon).name : "none")}', ammoType {ammo.ammoType}" + (loadErr.Length > 0 ? ", error: " + loadErr : ""),
                loadErr.Length == 0 && typeOk ? (loaded0 > 0 ? "" : "load returned success but the magazine is empty") : "TryEquipAmmoToActiveRangedWeapon / WeaponAmmoState.TryEquipAmmoToWeaponSlot refused: " + (loadErr.Length > 0 ? loadErr : "loaded item differs from the requested ammo"));
            if (loaded0 <= 0) yield break;

            if (!full)
            {
                // ammo-specific case: 2 aimed shots, then the damage / marks / per-ammo FX rules
                float shootIntervalS = Mathf.Clamp(DMRangedAmmoStats.ResolveShootInterval(weapon, ammo), 0.2f, 2.0f);
                int marksS0 = L.marks != null ? L.marks.StampCount : -1;
                float hS0 = EHp(L);
                int beforeS = Loaded(weapon);
                yield return AimedBurst(L, weapon, pos, shootIntervalS, 2, 6);
                int shotsS = _burstShots, hitsS = _burstHits;

                yield return new WaitGame(0.3f);
                int consumedS = beforeS - Loaded(weapon);
                float lostS = hS0 - EHp(L);
                int marksS1 = L.marks != null ? L.marks.StampCount : -1;
                R.Add("Ammo", idp + ".fire", $"{key}: fires", consumedS >= 1 ? "PASS" : "FAIL", $"{shotsS} trigger pull(s), rounds {beforeS} -> {Loaded(weapon)}", consumedS >= 1 ? "" : "no round consumed with this ammo type loaded");
                R.Add("Ammo", idp + ".damage", $"{key}: damages the enemy", lostS > 0f ? "PASS" : (consumedS >= 6 ? "FAIL" : "WARN"), $"enemy hp lost {lostS:0.0} (ammo type {ammo.ammoType})", lostS > 0f ? "" : (consumedS >= 6 ? "rounds spent but no damage across many shots with this ammo type (damage mapped to 0 or aim)" : "no hit in the few shots fired: spread/accuracy can miss, too few shots to judge"));
                AccuracyRow(R, idp, key, weapon, ammo, shotsS, hitsS);
                R.Add("Ammo", idp + ".marks", $"{key}: hit marks / FX", marksS1 > marksS0 ? "PASS" : (lostS > 0f ? (MarksExpected(ammo) ? "WARN" : "INFO") : "INFO"), $"hit marks stamped {marksS0} -> {marksS1}, body type {(L.marks != null ? L.marks.BodyType.ToString() : "n/a")}", marksS1 > marksS0 ? "" : (lostS > 0f && !MarksExpected(ammo) ? "this ammo type is configured to leave no hit marks (useHitMarks off / blood and burn Never): by design" : lostS > 0f ? "damage landed but no mark was stamped" : "no hit to mark"));
                AmmoFxRow(R, idp, key, ammo, marksS0, marksS1, lostS);
                EnemyTopUpNow(L);
                ReleaseAllInput();
                yield break;
            }

            int afterFill = FillClip(weapon);
            R.Add("Ammo", idp + ".fill", $"{key}: magazine fills to capacity", afterFill >= cap ? "PASS" : "WARN",
                $"magazine {afterFill}/{cap} after top-up (90 rounds were in the inventory)", afterFill >= cap ? "" : "RefillMagazineFromInventory did not reach capacity (reserve exhausted or capacity resolves differently: ammo.magazineSize vs weapon.magazineSize)");

            // fire: aimed shots at 6 m
            float shootInterval = Mathf.Clamp(DMRangedAmmoStats.ResolveShootInterval(weapon, ammo), 0.2f, 2.0f);
            int marks0 = L.marks != null ? L.marks.StampCount : -1;
            float h0 = EHp(L);
            int before = Loaded(weapon);
            yield return AimedBurst(L, weapon, pos, shootInterval, 3, 9);
            int shotsF = _burstShots, hitsF = _burstHits;

            yield return new WaitGame(0.25f);
            int after = Loaded(weapon);
            float lost = h0 - EHp(L);
            if (L.health.IsDead) lost = Mathf.Max(lost, 1f);
            int marks1 = L.marks != null ? L.marks.StampCount : -1;
            int consumed = before - after;
            R.Add("Ammo", idp + ".fire", $"{key}: fires (rounds consumed)", consumed >= 1 ? "PASS" : "FAIL",
                $"{shotsF} trigger pull(s), rounds {before} -> {after}, shot interval {shootInterval:0.00}s",
                consumed >= 1 ? "" : "no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused");
            float idmg = weapon.rangedDamage;
            R.Add("Ammo", idp + ".damage", $"{key}: damages the enemy", lost > 0f ? "PASS" : (consumed >= 6 ? "FAIL" : "WARN"),
                $"enemy hp lost {lost:0.0} (weapon dmg {idmg:0.#}, ammo type {ammo.ammoType}, ammo is {(ammo is DMAmmoFxProfile ? "DMAmmoFxProfile" : "plain ItemData")})",
                lost > 0f ? "" : (consumed >= 6 ? "rounds were spent but the enemy took no damage across many shots: aim, projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox" : (consumed >= 1 ? "no hit in the few shots fired: spread/accuracy can miss, too few shots to judge" : "no shot fired, see fire row")));
            AccuracyRow(R, idp, key, weapon, ammo, shotsF, hitsF);
            R.Add("Ammo", idp + ".marks", $"{key}: hit marks / FX", marks1 > marks0 ? "PASS" : (lost > 0f ? (MarksExpected(ammo) ? "WARN" : "INFO") : "INFO"),
                $"hit marks stamped {marks0} -> {marks1}, body type {(L.marks != null ? L.marks.BodyType.ToString() : "n/a")}",
                marks1 > marks0 ? "" : (lost > 0f && !MarksExpected(ammo) ? "this ammo type is configured to leave no hit marks (useHitMarks off / blood and burn Never): by design" : lost > 0f ? "damage landed but no mark was stamped: this ammo type has no body-type hit-mark entry (DMAmmoFxProfile) or the hit skipped DMEnemyHitMarks.HandleRangedHit" : "no hit to mark"));
            AmmoFxRow(R, idp, key, ammo, marks0, marks1, lost);
            R.Add("Player weapons", "pw." + weapon.name, $"Player uses ranged '{weapon.name}' on the enemy (default ammo)", lost > 0f ? "PASS" : "FAIL", $"enemy hp lost {lost:0.0} (item dmg {weapon.rangedDamage:0.#}), rounds consumed {consumed}", lost > 0f ? "" : "see the ammo damage row");
            EnemyTopUpNow(L);

            // empty + reload (only small magazines, so the drain stays short)
            if (cap > 0 && cap <= 14)
            {
                int guard = 0;
                while (Loaded(weapon) > 0 && guard++ < cap + 3)
                {
                    KeepAt(Ground(pos), L.go.transform.position);
                    yield return AimedShot(L, 0.25f, 0.15f, shootInterval + 0.05f);
                    SetMouse(0);
                    if (L.health.IsDead) break;
                }

                int empty = Loaded(weapon);
                float hDry = EHp(L);
                yield return AimedShot(L, 0.3f, 0.15f, 0.6f);
                SetMouse(0);
                int emptyAfter = Loaded(weapon);
                bool dryOk = empty == 0 && emptyAfter == 0 && EHp(L) >= hDry - 0.01f || L.health.IsDead;
                R.Add("Ammo", idp + ".empty", $"{key}: empty magazine does not fire", empty == 0 ? (dryOk ? "PASS" : "FAIL") : "WARN",
                    $"magazine after drain {empty}, after an extra trigger pull {emptyAfter}, enemy hp change {(hDry - EHp(L)):0.0}",
                    empty != 0 ? "could not empty the magazine in the time budget (enemy died or aim missed)" : (dryOk ? "" : "empty weapon still fired or damaged: TryProcessShotAmmo does not gate the shot"));

                int rBefore = Loaded(weapon);
                yield return TapKey(Key.R, 0.12f);
                float reloadS = Mathf.Clamp(DMRangedAmmoStats.ResolveReloadSeconds(weapon, ammo), 0.5f, 6f);
                yield return WaitMin(0.4f, () => Loaded(weapon) > rBefore, reloadS + 1.0f);
                int rAfter = Loaded(weapon);
                R.Add("Ammo", idp + ".reload", $"{key}: reload refills the magazine (R)", rAfter > rBefore ? "PASS" : "FAIL",
                    $"magazine {rBefore} -> {rAfter} after R (reload time {reloadS:0.0}s)",
                    rAfter > rBefore ? "" : "reload did nothing: Reload action not reaching WeaponModeSwitchController (tap vs hold), no reserve ammo of this type, or PioneerInvectorAmmoBridge.TryRequestReload refused");
            }
            else
            {
                R.Add("Ammo", idp + ".empty", $"{key}: empty + reload", "INFO", $"magazine capacity {cap} too large to drain in an audit run; reload covered by the smaller magazines", "");
            }

            ReleaseAllInput();
        }

        private static IEnumerator AmmoCycleCase(GameObject prefab, DMAuditEnemyResult R, ItemData weapon, List<ItemData> fit)
        {
            string err = ArmPlayerWithItem(weapon);
            yield return WaitMin(0.3f, () => EquippedOk(weapon), 1.0f);
            var inv = PlayerInv();
            var eq = PlayerEq();
            if (err.Length > 0 || inv == null || eq == null) { R.Add("Ammo", "ammo.cycle", "X cycles ammo types (Hot Cross)", "WARN", "could not arm: " + err, ""); yield break; }

            Type gks = FindType("GameplayKeyboardShortcuts");
            int first = 4;
            FieldInfo ff = gks != null ? gks.GetField("HotCrossUtilityFirstLocal", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) : null;
            if (ff != null && ff.GetRawConstantValue() is int fi) first = fi;
            int placed = 0;
            for (int i = 0; i < fit.Count && i < 3; i++)
            {
                int slot = GiveAmmo(fit[i], 60);
                if (slot < 0) continue;
                int target = inv.HotbarStartIndex + first + i;
                if (inv.GetItemAt(target) != fit[i]) inv.MoveOrMergeSlots(slot, target);
                if (inv.GetItemAt(target) == fit[i]) placed++;
            }

            LoadAmmoLikeHotCross(weapon, fit[0], FindInventorySlot(inv, fit[0]));
            yield return new WaitGame(0.4f);
            ItemData a0 = LoadedItem(weapon);
            yield return TapKey(Key.X, 0.1f);
            yield return new WaitGame(0.8f);
            ItemData a1 = LoadedItem(weapon);
            yield return TapKey(Key.X, 0.1f);
            yield return new WaitGame(0.8f);
            ItemData a2 = LoadedItem(weapon);
            bool changed = a1 != a0 && a1 != null;
            R.Add("Ammo", "ammo.cycle", "X (AmmoCycle) switches the loaded ammo type", changed ? "PASS" : (placed < 2 ? "WARN" : "FAIL"),
                $"{placed} ammo stack(s) on the Hot Cross utility slots; loaded {(a0 != null ? a0.name : "none")} -> {(a1 != null ? a1.name : "none")} -> {(a2 != null ? a2.name : "none")}",
                changed ? "" : (placed < 2 ? "fewer than 2 ammo types could be placed on the Hot Cross utility slots, cycle needs 2" : "X tap did not cycle: AmmoCycle owned by GameplayKeyboardShortcuts poll (needs drawn ranged weapon + Hot Cross focus) or the next type does not fit the weapon"));
            ReleaseAllInput();
        }
    }
}
#endif
