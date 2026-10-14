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
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace Project.EditorTools.Audit
{
    public static partial class DMEnemyRuntimeAudit
    {
        // =============================================================== weapon matrix
        // Every weapon ItemData in the project is equipped on the singular template as a spawn override (the same call
        // EnemySpawnConfigurator / the spawner uses) and made to attack the standing player, so "weapon X deals no damage"
        // is found independent of which enemy normally carries it. Then a dual-slot override case is checked.
        private const string MatrixPrefab = "Assets/_Project/Prefabs/Combat/Enemies/HumanoidEnemy_Invector.prefab";

        private static List<ItemData> AllWeaponItems()
        {
            var melee = new List<ItemData>();
            var ranged = new List<ItemData>();
            foreach (string guid in AssetDatabase.FindAssets("t:ItemData"))
            {
                var it = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));
                if (it == null) continue;
                // Player-only mining tool — never an enemy weapon matrix case (2026-10-14 c).
                if (it.isMiningTool) continue;
                if (it.itemType == ItemType.MeleeWeapon) melee.Add(it);
                else if (it.IsRangedWeapon) ranged.Add(it);
            }

            melee.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            ranged.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            melee.AddRange(ranged);
            return melee;
        }

        private static IEnumerator RunWeaponMatrix()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MatrixPrefab);
            if (prefab == null)
            {
                _run.environment.Add("Weapon matrix skipped: template prefab missing at " + MatrixPrefab);
                yield break;
            }

            List<ItemData> items = AllWeaponItems();
            _run.environment.Add($"Weapon matrix: {items.Count} weapon items equipped one at a time (spawn override) on {prefab.name}; plus one dual-slot override case");
            int n = 0;
            Live shared = null; // ONE template enemy for the whole matrix: weapons are swapped in place (same call the spawner uses)
            string tplExtra = "weapon|" + _tplStamp;
            foreach (ItemData item in items)
            {
                n++;
                bool isMelee = item.itemType == ItemType.MeleeWeapon;
                string ukey = "weapon|" + item.name;
                string uhash = UnitHash(AssetDatabase.GetAssetPath(item), tplExtra, false);
                if (TryReuse(ukey, uhash, null, out DMAuditEnemyResult reused)) { _run.weapons.Add(reused); SetStatus($"weapon {n}/{items.Count}: {item.name} (cached)"); continue; }
                var R = new DMAuditEnemyResult
                {
                    name = item.name,
                    prefabPath = AssetDatabase.GetAssetPath(item),
                    definition = prefab.name,
                    melee = isMelee ? item.name : "-",
                    ranged = isMelee ? "-" : item.name,
                    note = isMelee ? "melee" : "ranged"
                };
                _run.weapons.Add(R);
                _cur = R;
                SetStatus($"weapon {n}/{items.Count}: {item.name}");
                AuditTiming.Begin(ukey);
                float started = Time.realtimeSinceStartup;
                if (shared == null || shared.go == null || shared.health == null || shared.health.IsDead)
                {
                    Cleanup();
                    shared = SpawnMatrixEnemy(prefab, "matrix");
                    yield return SpawnReady(shared);
                }
                else
                {
                    // park the player far away so the enemy goes idle, then reuse the same enemy
                    TopUpPlayer();
                    PlacePlayer(Ground(_origin + new Vector3(-60f, 0f, 0f)), shared.go.transform.position);
                    _live = shared;
                    shared.ResetRunWide();
                    shared.BeginPhase();
                }

                yield return RunWeapon(prefab, item, isMelee, R, shared);
                R.seconds = Time.realtimeSinceStartup - started;
                FinalizeConsole(R);
                AuditTiming.End();
                RecordUnit(ukey, uhash, R);
                _cur = null;
            }

            Cleanup();
            yield return new WaitGame(0.2f);
            {
            string okey = "weapon|OVERRIDE both slots";
            string ohash = UnitHash(MatrixPrefab, "override|" + _tplStamp, false);
            if (TryReuse(okey, ohash, null, out DMAuditEnemyResult oreused)) { _run.weapons.Add(oreused); yield break; }
            var O = new DMAuditEnemyResult { name = "OVERRIDE both slots", prefabPath = MatrixPrefab, definition = prefab.name, note = "dual-slot spawn override" };
            _run.weapons.Add(O);
            _cur = O;
            SetStatus("weapon override case");
            float st2 = Time.realtimeSinceStartup;
            yield return RunOverrideCase(prefab, items, O);
            O.seconds = Time.realtimeSinceStartup - st2;
            FinalizeConsole(O);
            Cleanup();
            RecordUnit(okey, ohash, O);
            _cur = null;
            }
        }

        private static Live SpawnMatrixEnemy(GameObject prefab, string tag)
        {
            TopUpPlayer();
            Vector3 spawn = Ground(_origin + new Vector3(0f, 0f, 22f));
            PlacePlayer(Ground(_origin + new Vector3(-60f, 0f, 0f)), spawn);
            GameObject go = Object.Instantiate(prefab, spawn, Quaternion.LookRotation(new Vector3(1f, 0f, 0f)));
            go.name = "AUDIT_W_" + tag;
            var L = new Live
            {
                go = go,
                ai = go.GetComponent<EnemyAiController>(),
                health = go.GetComponent<EnemyHealth>(),
                combat = go.GetComponent<EnemyCombat>(),
                bridge = go.GetComponent<EnemyInvectorLoadoutBridge>(),
                anim = go.GetComponentInChildren<Animator>(),
                mm = go.GetComponent<vMeleeManager>(),
                marks = go.GetComponent<DMEnemyHitMarks>(),
                trackPlayer = true
            };
            _live = L;
            L.BeginPhase();
            return L;
        }

        private static IEnumerator RunWeapon(GameObject prefab, ItemData item, bool isMelee, DMAuditEnemyResult R, Live L)
        {
            GameObject go = L.go;
            L.trackPlayer = true;
            if (L.bridge == null || L.combat == null)
            {
                R.Add("Weapons", "w.equip", "Equips on the template", "FAIL", "template has no loadout bridge/combat", "");
                yield break;
            }

            EnemyDefinition def = L.bridge.ResolveDefinition();
            // Spawner-style override, then pin it so the AI cannot swap away during the measurement.
            if (isMelee) L.bridge.ConfigureSpawnLoadout(item, null, true, false);
            else L.bridge.ConfigureSpawnLoadout(null, item, false, true);
            L.bridge.TrySwitchTo(item, EnemyWeaponRequester.Brain, 60f);
            yield return WaitMin(0.4f, () => L.bridge.ActiveItem == item, 1.2f);

            bool equipped = L.bridge.ActiveItem == item;
            string vis = DescribeVisibility(L);
            float idmg = isMelee ? item.meleeDamage : item.rangedDamage;
            string dataIssue = idmg <= 0f ? $"ItemData {(isMelee ? "meleeDamage" : "rangedDamage")} = {idmg:0.#}" : "";
            R.Add("Weapons", "w.equip", "Equips on the template (spawn override)",
                equipped ? "PASS" : "FAIL", vis + $"; item dmg {idmg:0.#}",
                equipped ? dataIssue : "ConfigureSpawnLoadout + TrySwitchTo did not make this the active weapon: the ItemData has no equip prefab / the template has no matching Drawn_ slot for it" + (dataIssue.Length > 0 ? "; " + dataIssue : ""));

            var cb = go.GetComponent<EnemyInvectorCombatBridge>();
            string staticInfo = "";
            bool tagOk = true, layerOk = true;
            if (isMelee) staticInfo = DescribeMeleeSetup(L, item, out tagOk, out layerOk);
            else staticInfo = DescribeRangedSetup(L, item, def, cb);

            Vector3 f = go.transform.forward; f.y = 0f; f = f.sqrMagnitude < 0.01f ? Vector3.right : f.normalized;
            float engage = cb != null ? cb.RangedEngageRange : 12f;
            float dist = isMelee ? 1.5f : Mathf.Clamp(engage * 0.6f, 5f, 11f);
            PlacePlayer(Ground(go.transform.position + f * dist), go.transform.position);
            yield return Settle(L);
            L.meleeHitEvents = 0;
            UnityEngine.Events.UnityAction<vHitInfo> listener = hi => L.meleeHitEvents++;
            if (L.mm != null && isMelee) L.mm.onDamageHit.AddListener(listener);
            if (isMelee) AudioProbe.Begin();
            L.BeginPhase();
            yield return WaitSwingsOrDamage(L, isMelee, isMelee ? 14f : 16f, isMelee ? 14f : 10f);
            if (isMelee) yield return new WaitGame(0.4f);
            if (L.mm != null && isMelee) L.mm.onDamageHit.RemoveListener(listener);
            if (isMelee) { AddEnemyMeleeAudioRows(R, item.name, L); AudioProbe.End(); }
            // melee: a real weapon hit must have been registered (passive 1-5 hp survival chip is not damage from the weapon)
            bool dealt = isMelee ? (L.meleeHitEvents > 0 && L.PhaseDamage > 0f) : L.PhaseDamage > 0f;
            string wMissNote;
            string wStat = MissAwareStatus(L, isMelee, dealt, idmg <= 0f ? "WARN" : "PASS", out wMissNote);
            string cause = wMissNote;
            if (!dealt && L.PhaseDamage > 0f)
                cause += $"only {L.PhaseDamage:0.0} hp lost with 0 weapon hit events (passive survival chip, not the weapon) | ";
            if (!dealt)
            {
                cause += isMelee ? DiagnoseMelee(L, item, def, tagOk, layerOk) : DiagnoseRanged(L, item, def, cb);
                if (!equipped) cause = "weapon never became the active weapon (see equip row) | " + cause;
                if (dataIssue.Length > 0) cause = dataIssue + " | " + cause;
            }

            string active = L.bridge.ActiveItem != null ? L.bridge.ActiveItem.name : "none";
            string measured = isMelee
                ? $"player hp lost {L.PhaseDamage:0.0} (item dmg {item.meleeDamage:0.#}), attacks {L.attackStarts}, hitbox-active frames {L.hitboxFrames}, hit events {L.meleeHitEvents}, sub-1hp chip {L.chipInv:0.0}, first hit {FmtT(L.firstDamageT)}, active item {active}; {staticInfo}"
                : $"player hp lost {L.PhaseDamage:0.0} (item dmg {item.rangedDamage:0.#}), shots/projectiles {L.projectileSpawns}, attack-state frames {L.attackStateFrames}, attacks {L.attackStarts}, first hit {FmtT(L.firstDamageT)}, range {dist:0}m (engage {engage:0}), active item {active}; {staticInfo}";
            R.Add("Weapons", "w.damage", (isMelee ? "Melee" : "Ranged") + $" '{item.name}' damages the player", wStat, measured,
                dealt && idmg <= 0f ? $"damage was dealt although the item's damage is {idmg:0.#}: it comes from a fallback (definition/ammo/projectile defaults), not from this item" : cause);

            AddMissRow(R, "w.miss", L, isMelee, (isMelee ? "Melee" : "Ranged") + $" '{item.name}'", def);
            string clips = string.Join(", ", L.clipSeen.Keys.Where(k => k.IndexOf("ttack", StringComparison.OrdinalIgnoreCase) >= 0 || k.IndexOf("hoot", StringComparison.OrdinalIgnoreCase) >= 0 || k.IndexOf("raw", StringComparison.OrdinalIgnoreCase) >= 0 || k.IndexOf("olster", StringComparison.OrdinalIgnoreCase) >= 0).Take(6));
            R.Add("Timing", "w.timing", "Attack timing", "INFO",
                $"first attack {FmtT(L.firstAttackT)} (at {L.firstAttackDist:0.0}m), first damage {FmtT(L.firstDamageT)}, attacks {L.attackStarts}, attack-state frames {L.attackStateFrames}, clips seen: {(clips.Length > 0 ? clips : "none matched")}", "");

            if (L.maxDrawn != 1 || L.strayFrames > 0)
                R.Add("Weapons", "vis", "Exactly one weapon drawn", "FAIL", $"max drawn {L.maxDrawn}: {string.Join(", ", L.extraDrawn)}; stray renderers {string.Join(", ", L.strayRenderers.Take(3))}", "second weapon visual left active, or the equipped weapon has no drawn visual");
            else
                R.Add("Weapons", "vis", "Exactly one weapon drawn", "PASS", $"max drawn {L.maxDrawn}, max holstered {L.maxHolster}", "");
        }

        private static IEnumerator RunOverrideCase(GameObject prefab, List<ItemData> items, DMAuditEnemyResult R)
        {
            ItemData axe = items.FirstOrDefault(i => i.name == "Wood Axe") ?? items.FirstOrDefault(i => i.itemType == ItemType.MeleeWeapon);
            ItemData rifle = items.FirstOrDefault(i => i.name == "survival_rifle") ?? items.FirstOrDefault(i => i.IsRangedWeapon && i.rangedDamage > 0f);
            Live L = SpawnMatrixEnemy(prefab, "override");
            yield return SpawnReady(L);
            if (L.bridge == null || axe == null || rifle == null)
            {
                R.Add("Weapons", "ovr", "Override case setup", "FAIL", "bridge/items missing", "");
                yield break;
            }

            EnemyDefinition def = L.bridge.ResolveDefinition();
            R.melee = axe.name;
            R.ranged = rifle.name;
            L.bridge.ConfigureSpawnLoadout(axe, rifle, false, true);
            yield return new WaitGame(1.2f);
            string v1 = DescribeVisibility(L);
            bool resolvedOk = L.bridge.ResolvedMelee == axe && L.bridge.ResolvedRanged == rifle;
            R.Add("Weapons", "ovr.resolve", $"Override '{axe.name}' + '{rifle.name}' replaces the definition ({(def != null ? def.meleeWeaponItem + "/" + def.rangedWeaponItem : "no def")})",
                resolvedOk ? "PASS" : "FAIL", $"resolved melee={L.bridge.ResolvedMelee}, ranged={L.bridge.ResolvedRanged}, active={L.bridge.ActiveItem}; {v1}",
                resolvedOk ? "" : "ConfigureSpawnLoadout did not replace the resolved items");

            bool starts = L.bridge.ActiveItem == rifle;
            R.Add("Weapons", "ovr.start", "Starts with the ranged override drawn (prefer-ranged on, start-melee off)", starts ? "PASS" : "FAIL", $"active={L.bridge.ActiveItem}", starts ? "" : "start/prefer flags from the spawn override not applied");

            L.bridge.TrySwitchTo(axe, EnemyWeaponRequester.Brain, 5f);
            yield return new WaitGame(1.0f);
            bool sw = L.bridge.ActiveItem == axe;
            string v2 = DescribeVisibility(L);
            R.Add("Weapons", "ovr.switch", "Switch to the melee override via the single switch call", sw ? "PASS" : "FAIL", $"active={L.bridge.ActiveItem}; {v2}", sw ? "" : "TrySwitchTo refused the resolved weapon");

            // an item equal to the definition must not become an override
            if (def != null && def.meleeWeaponItem != null)
            {
                L.bridge.ConfigureSpawnLoadout(def.meleeWeaponItem, def.rangedWeaponItem, true, def.preferRangedWeapon);
                yield return new WaitGame(0.8f);
                bool back = L.bridge.ResolvedMelee == def.meleeWeaponItem && L.bridge.ResolvedRanged == def.rangedWeaponItem;
                R.Add("Weapons", "ovr.default", "Spawn config equal to the definition falls back to the definition", back ? "PASS" : "FAIL",
                    $"resolved melee={L.bridge.ResolvedMelee}, ranged={L.bridge.ResolvedRanged}", back ? "" : "equal-to-definition items kept as overrides (would be flagged as mismatches by the Fix check)");
            }

            if (L.maxDrawn > 1 || L.strayFrames > 0)
                R.Add("Weapons", "vis", "Exactly one weapon drawn through the whole case", "FAIL", $"max drawn {L.maxDrawn}: {string.Join(", ", L.extraDrawn)}", "second weapon visual left active");
            else
                R.Add("Weapons", "vis", "Exactly one weapon drawn through the whole case", "PASS", $"max drawn {L.maxDrawn}, max holstered {L.maxHolster}", "");
        }
    }
}
#endif
