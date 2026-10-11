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
        private static IEnumerator Settle(Live L)
        {
            // let any in-flight attack finish so its late hit is not credited to the next phase
            yield return new WaitUntil(() => L.combat == null || !L.combat.IsAttacking, 3f);
            yield return new WaitGame(0.3f);
        }

        // =============================================================== one enemy
        private static IEnumerator RunEnemy(GameObject prefab)
        {
            TopUpPlayer();
            Vector3 spawn = Ground(_origin + new Vector3(0f, 0f, 22f));
            PlacePlayer(Ground(_origin + new Vector3(-60f, 0f, 0f)), spawn);
            var swSpawn = System.Diagnostics.Stopwatch.StartNew();
            GameObject go = Object.Instantiate(prefab, spawn, Quaternion.LookRotation(new Vector3(1f, 0f, 0f)));
            swSpawn.Stop();
            int compCount = go.GetComponentsInChildren<Component>(true).Length;
            go.name = "AUDIT_" + prefab.name;
            var L = new Live
            {
                go = go,
                ai = go.GetComponent<EnemyAiController>(),
                health = go.GetComponent<EnemyHealth>(),
                combat = go.GetComponent<EnemyCombat>(),
                bridge = go.GetComponent<EnemyInvectorLoadoutBridge>(),
                anim = go.GetComponentInChildren<Animator>(),
                mm = go.GetComponent<vMeleeManager>(),
                marks = go.GetComponent<DMEnemyHitMarks>()
            };
            _live = L;
            L.BeginPhase();
            Ph("spawn");
            yield return SpawnReady(L);

            var R = _cur;
            if (L.health == null || L.ai == null || L.combat == null)
            {
                R.Add("Config", "cfg", "Core components", "FAIL", "EnemyHealth/EnemyAiController/EnemyCombat missing", "prefab is not a complete enemy");
                yield break;
            }

            Ph("config");
            // ---- config
            ItemData melee = L.bridge != null ? L.bridge.ResolvedMelee : null;
            ItemData ranged = L.bridge != null ? L.bridge.ResolvedRanged : null;
            EnemyDefinition def = L.bridge != null ? L.bridge.ResolveDefinition() : null;
            R.definition = def != null ? def.name : "(none)";
            R.melee = melee != null ? melee.name : "-";
            R.ranged = ranged != null ? ranged.name : "-";
            string cfgIssues = "";
            if (def == null) cfgIssues += "no EnemyDefinition linked; ";
            if (L.bridge == null) cfgIssues += "no EnemyInvectorLoadoutBridge; ";
            if (melee == null && ranged == null) cfgIssues += "no resolved weapon (unarmed); ";
            if (melee != null && melee.meleeDamage <= 0f) cfgIssues += $"melee item '{melee.name}' meleeDamage={melee.meleeDamage}; ";
            if (ranged != null && ranged.rangedDamage <= 0f) cfgIssues += $"ranged item '{ranged.name}' rangedDamage={ranged.rangedDamage}; ";
            if (def != null && def.attackDamage <= 0f) cfgIssues += $"definition attackDamage={def.attackDamage}; ";
            if (L.mm == null) cfgIssues += "no vMeleeManager; ";
            R.Add("Config", "cfg", "Definition / weapon data",
                cfgIssues.Length == 0 ? "PASS" : (melee == null && ranged == null ? "WARN" : "FAIL"),
                $"def={R.definition}, melee={R.melee} (dmg {(melee != null ? melee.meleeDamage.ToString("0.#") : "-")}), ranged={R.ranged} (dmg {(ranged != null ? ranged.rangedDamage.ToString("0.#") : "-")}), preferRanged={(L.bridge != null ? L.bridge.ResolvedPreferRanged.ToString() : "?")}, def.attackDamage={(def != null ? def.attackDamage.ToString("0.#") : "-")}, hp={L.health.MaxHealth:0}",
                cfgIssues);

            SilentChecks(L, R);
            string meleeStatic = DescribeMeleeSetup(L, melee, out bool tagOk, out bool layerOk);
            string visSpawn = DescribeVisibility(L);

            // ---- unaware behaviour (player far away)
            float senseMax = def != null ? Mathf.Max(def.visionRange, def.hearingRange) : 20f;
            float far = Mathf.Max(45f, senseMax * 2.5f);
            Vector3 farPos = Ground(go.transform.position + new Vector3(-far, 0f, 0f));
            PlacePlayer(farPos, go.transform.position);
            L.trackPlayer = true;
            Ph("unaware 9s (+perf)");
            L.BeginPhase();
            int gc0 = GC.CollectionCount(0);
            long mem0 = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
            yield return new WaitGame(9f);
            {
                // performance folded into this pass (was a separate per-enemy scenario)
                int gc1 = GC.CollectionCount(0);
                long mem1 = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
                float avgMs = L.dtN > 0 ? (float)(L.dtSum / L.dtN * 1000.0) : 0f;
                var pc = new List<string>();
                if (swSpawn.ElapsedMilliseconds > 250) pc.Add($"spawn took {swSpawn.ElapsedMilliseconds}ms (Awake/Start work, component count {compCount})");
                if (avgMs > 50f) pc.Add($"average editor frame {avgMs:0.0}ms with one enemy active");
                if (gc1 - gc0 >= 3) pc.Add($"{gc1 - gc0} GC collections in 9s (per-frame allocations in enemy AI/animation code)");
                R.Add("Performance", "perf", "Spawn cost / frame time / GC (editor, 9s unaware, player far away)", pc.Count == 0 ? "PASS" : "WARN",
                    $"spawn {swSpawn.ElapsedMilliseconds}ms, {compCount} components, frame avg {avgMs:0.0}ms max {L.dtMax * 1000.0:0.0}ms ({L.dtN} frames), GC collections {gc1 - gc0}, managed delta {(mem1 - mem0) / 1024}KB",
                    string.Join("; ", pc));
            }
            float yaw = L.yawAccum, path = L.path, net = L.NetDisplacement;
            bool noticedFar = L.phaseStates.Any(s => s == "Chase" || s == "Attack" || s == "Hold" || s == "Defensive");
            bool circling = yaw > 420f && net < 3.0f;
            string uState = string.Join("/", L.phaseStates);
            R.Add("AI", "unaware", "No circling while unaware (player " + far.ToString("0") + "m away, 9s)",
                noticedFar ? "WARN" : (circling ? "FAIL" : "PASS"),
                $"yaw turned {yaw:0}deg, path {path:0.0}m, net {net:0.0}m, states {uState}",
                noticedFar ? "enemy aggro'd with the player far outside its senses (shared aggro / threat ledger?)" :
                circling ? "turns in place with no destination (patrol points that move with the enemy, or engage/orbit logic running without a target)" : "");

            Ph("notice/chase/attack");
            // ---- notice / chase / attack timings (natural AI)
            float vis = def != null ? def.visionRange : 16f;
            float nd = Mathf.Clamp(vis * 0.6f, 5f, 10f);
            Vector3 fwd = go.transform.forward; fwd.y = 0f; fwd = fwd.sqrMagnitude < 0.01f ? Vector3.right : fwd.normalized;
            PlacePlayer(Ground(go.transform.position + fwd * nd), go.transform.position);
            L.BeginPhase();
            yield return new WaitUntil(() => L.PhaseDamage > 0f && L.firstAttackT >= 0f, 22f);
            string naturalWeapon = L.weaponAtAttack;
            bool noticed = L.noticeT >= 0f;
            R.Add("AI", "notice", "Notice / chase / attack timing (player " + nd.ToString("0") + "m in front)",
                !noticed ? "FAIL" : (L.firstAttackT < 0f ? "WARN" : "PASS"),
                $"notice {FmtT(L.noticeT)}, chase {FmtT(L.chaseT)}, first attack {FmtT(L.firstAttackT)} (at {L.firstAttackDist:0.0}m, weapon {naturalWeapon}), first player damage {FmtT(L.firstDamageT)}, attacks {L.attackStarts}, states {string.Join("/", L.phaseStates)}",
                !noticed ? "never left calm states with the player in front, inside vision range (EnemySenses LOS/FOV, vision range or aggro rules)" :
                L.firstAttackT < 0f ? "noticed but never attacked within 22s (chase blocked, standoff/hold token, attack range, or cooldown)" : "");
            R.Add("Weapons", "natural.damage", "Damage to player during natural AI behaviour",
                "INFO", L.PhaseDamage > 0f ? $"{L.PhaseDamage:0.0} hp in {FmtT(L.firstDamageT)} using {naturalWeapon}" : "no damage in 22s of natural behaviour", "");

            Ph("melee test");
            // ---- forced MELEE test
            if (melee != null && L.bridge != null)
            {
                TopUpPlayer();
                L.bridge.TrySwitchTo(melee, EnemyWeaponRequester.Brain, 60f);
                yield return WaitMin(0.4f, () => L.mm != null && L.mm.rightWeapon != null, 1.2f);
                string visMelee = DescribeVisibility(L);
                Vector3 ef = go.transform.forward; ef.y = 0f; ef = ef.sqrMagnitude < 0.01f ? Vector3.right : ef.normalized;
                PlacePlayer(Ground(go.transform.position + ef * 1.5f), go.transform.position);
                yield return Settle(L);
                L.meleeHitEvents = 0;
                UnityEngine.Events.UnityAction<vHitInfo> listener = hi => L.meleeHitEvents++;
                if (L.mm != null) L.mm.onDamageHit.AddListener(listener);
                L.BeginPhase();
                yield return WaitSwingsOrDamage(L, true, 14f, 14f);
                if (L.mm != null) L.mm.onDamageHit.RemoveListener(listener);
                bool dealt = L.meleeHitEvents > 0 && L.PhaseDamage > 0f;
                string missNote;
                string mstat = MissAwareStatus(L, true, dealt, "PASS", out missNote);
                string cause = dealt ? "" : missNote + (L.PhaseDamage > 0f ? $"only {L.PhaseDamage:0.0} hp lost with 0 weapon hit events (passive chip, not the weapon) | " : "") + DiagnoseMelee(L, melee, def, tagOk, layerOk);
                R.Add("Weapons", "melee.damage", $"Melee '{melee.name}' damages the player",
                    mstat,
                    $"player hp lost {L.PhaseDamage:0.0} (item dmg {melee.meleeDamage:0.#}), attacks {L.attackStarts}, hitbox-active frames {L.hitboxFrames} (unarmed {L.unarmedHitboxFrames}), hit events {L.meleeHitEvents}, sub-1hp chip {L.chipInv:0.0}, first hit {FmtT(L.firstDamageT)}, min distance {L.minDist:0.00}m, target '{L.targetName}', states {string.Join("/", L.phaseStates)}; {meleeStatic}",
                    cause);
                AddMissRow(R, "melee.miss", L, true, $"Melee '{melee.name}'", def);
                R.Add("Weapons", "melee.vis", "Melee drawn state", "INFO", visMelee, "");
            }
            else R.Add("Weapons", "melee.damage", "Melee weapon damages the player", "SKIP", "no resolved melee weapon", "");

            // ---- forced RANGED test
            if (ranged != null && L.bridge != null)
            {
                TopUpPlayer();
                Ph("ranged test");
                L.bridge.TrySwitchTo(ranged, EnemyWeaponRequester.Brain, 60f);
                yield return new WaitGame(0.8f);
                string visRanged = DescribeVisibility(L);
                var cb = go.GetComponent<EnemyInvectorCombatBridge>();
                float engage = cb != null ? cb.RangedEngageRange : 12f;
                float rd = Mathf.Clamp(engage * 0.6f, 5f, 11f);
                Vector3 ef = go.transform.forward; ef.y = 0f; ef = ef.sqrMagnitude < 0.01f ? Vector3.right : ef.normalized;
                PlacePlayer(Ground(go.transform.position + ef * rd), go.transform.position);
                yield return Settle(L);
                L.BeginPhase();
                yield return WaitSwingsOrDamage(L, false, 16f, 10f);
                bool dealt = L.PhaseDamage > 0f;
                string rstat = DescribeRangedSetup(L, ranged, def, cb);
                string rmissNote;
                string rstatus = MissAwareStatus(L, false, dealt, "PASS", out rmissNote);
                string cause = dealt ? "" : rmissNote + DiagnoseRanged(L, ranged, def, cb);
                R.Add("Weapons", "ranged.damage", $"Ranged '{ranged.name}' damages the player",
                    rstatus,
                    $"player hp lost {L.PhaseDamage:0.0} (item dmg {ranged.rangedDamage:0.#}), shots/projectiles {L.projectileSpawns}, attack-state frames {L.attackStateFrames}, attacks {L.attackStarts}, first hit {FmtT(L.firstDamageT)}, range {rd:0}m (engage {engage:0}), target '{L.targetName}', states {string.Join("/", L.phaseStates)}; {rstat}",
                    cause);
                AddMissRow(R, "ranged.miss", L, false, $"Ranged '{ranged.name}'", def);
                R.Add("Weapons", "ranged.vis", "Ranged drawn state", "INFO", visRanged, "");
            }
            else R.Add("Weapons", "ranged.damage", "Ranged weapon damages the player", "SKIP", "no resolved ranged weapon", "");

            // ---- weapon visibility verdict (whole run)
            {
                var sbv = new List<string>();
                if (L.maxDrawn > 1) sbv.Add($"{L.maxDrawn} Drawn_ slots active at once ({string.Join(", ", L.extraDrawn)})");
                if (L.strayFrames > 0) sbv.Add("stray enabled renderers under drawn slots: " + string.Join(", ", L.strayRenderers.Take(4)));
                if (L.rightWeaponStaleFrames > 5) sbv.Add($"vMeleeManager.rightWeapon not the drawn weapon for {L.rightWeaponStaleFrames} samples");
                if (L.maxHolster > 1) sbv.Add($"{L.maxHolster} Holstered_ slots active at once");
                bool armed = melee != null || ranged != null;
                if (armed && L.maxDrawn == 0) sbv.Add("weapon resolved but no Drawn_ slot ever active");
                R.Add("Weapons", "vis", "Exactly one weapon drawn (resolved), other holstered",
                    sbv.Count == 0 ? "PASS" : "FAIL",
                    $"spawn: {visSpawn}; max drawn {L.maxDrawn}, max holstered {L.maxHolster}",
                    string.Join("; ", sbv));
            }

            Ph("player->enemy");
            // ---- player -> enemy damage + hit marks
            L.trackPlayer = false;
            L.ai.enabled = false;
            Vector3 hp = Ground(go.transform.position + (go.transform.forward.sqrMagnitude < 0.01f ? Vector3.right : go.transform.forward) * 1.4f);
            PlacePlayer(hp, go.transform.position);
            yield return new WaitGame(0.3f);
            float hp0 = EHp(L);
            var recv = go.GetComponentsInChildren<PioneerInvectorDamageReceiver>(true);
            var vrecv = go.GetComponentsInChildren<vIDamageReceiver>(true);
            int marks0 = L.marks != null ? L.marks.StampCount : -1;
            string direct;
            string directStatus;
            string directCause = "";
            if (recv.Length == 0 && vrecv.Length == 0)
            {
                direct = "no damage receiver components on the enemy";
                directStatus = "FAIL";
                directCause = "no PioneerInvectorDamageReceiver / vDamageReceiver on the enemy or its hit rig; player melee/bullets cannot hurt it";
            }
            else
            {
                var d = new vDamage(25) { sender = _player.transform };
                Collider hc = recv.Length > 0 ? recv[0].GetComponent<Collider>() : null;
                d.hitPosition = hc != null ? hc.bounds.center : go.transform.position + Vector3.up;
                if (recv.Length > 0) recv[0].TakeDamage(d); else vrecv[0].TakeDamage(d);
                yield return WaitMin(0.15f, () => EHp(L) < hp0, 0.5f);
                float hp1 = EHp(L);
                float delta = hp0 - hp1;
                if (delta > 0f) { direct = $"receiver hit: enemy hp {hp0:0.0} -> {hp1:0.0} (-{delta:0.0}); {recv.Length} receiver(s)"; directStatus = "PASS"; }
                else
                {
                    L.health.TakeDamage(5f, _player, false);
                    yield return new WaitGame(0.2f);
                    float hp2 = EHp(L);
                    direct = $"receiver hit did 0; EnemyHealth.TakeDamage direct: {hp1:0.0} -> {hp2:0.0}; {recv.Length} receiver(s)";
                    directStatus = "FAIL";
                    directCause = hp2 < hp1
                        ? "damage receiver chain returns 0 (PioneerInvectorDamageResolver resolves the sender's weapon damage to 0 - player has no damaging weapon equipped, or hit dedupe rejected) though EnemyHealth itself works"
                        : "EnemyHealth ignores damage (invulnerable/dead flag) as well";
                }
            }

            R.Add("Player->enemy", "player.dmg", "Player damage hurts the enemy (receiver path)", directStatus, direct, directCause);

            var swingMgr = _player != null ? _player.GetComponent<vMeleeManager>() : null;
            if (_opts.realSwing && swingMgr != null && swingMgr.rightWeapon == null) { ArmPlayerWithSword(); yield return WaitMin(0.5f, () => swingMgr.rightWeapon != null, 1.5f); }
            if (_opts.realSwing && (swingMgr == null || swingMgr.rightWeapon == null))
            {
                R.Add("Player->enemy", "player.swing", "Player melee swing via injected Mouse input", "SKIP",
                    "player could not be armed (rightWeapon=none) - see the Player armed line in the environment section",
                    "not applicable: the receiver path above already proves player damage reaches the enemy; equip a melee weapon on the player to enable this probe");
            }
            else if (_opts.realSwing)
            {
                float sh0 = EHp(L);
                var mouse = Mouse.current ?? InputSystem.AddDevice<Mouse>();
                bool injected = mouse != null;
                var pc = _player.GetComponent<vMeleeManager>();
                string wState = $"player rightWeapon={(pc != null && pc.rightWeapon != null ? pc.rightWeapon.name : "none")}";
                for (int a = 0; a < 3 && injected; a++)
                {
                    Inject(mouse, new MouseState { buttons = 1 });
                    yield return new WaitGame(0.15f);
                    Inject(mouse, new MouseState { buttons = 0 });
                    yield return WaitMin(0.35f, () => !PlayerAttacking(), 1.1f);
                }

                float sh1 = EHp(L);
                R.Add("Player->enemy", "player.swing", "Player melee swing via injected Mouse input (best effort)",
                    sh1 < sh0 ? "PASS" : "WARN",
                    $"3 left-click attacks at 1.4m: enemy hp {sh0:0.0} -> {sh1:0.0}; {wState}",
                    sh1 < sh0 ? "" : "inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes");
            }

            if (L.marks == null)
                R.Add("Hit marks", "hitmarks", "Hit marks fire on damage", "FAIL", "no DMEnemyHitMarks component on the enemy", "template/creator did not add DMEnemyHitMarks; hits only add one at runtime");
            else
            {
                int marks1 = L.marks.StampCount;
                string mk = $"marks stamped {marks0} -> {marks1}";
                R.Add("Hit marks", "hitmarks", "Hit marks fire on damage", marks1 > marks0 ? "PASS" : "WARN", mk + $", body type {L.marks.BodyType}",
                    marks1 > marks0 ? "" : "no mark after receiver hits (marks need the hitbox collider + body-type profile; check DMEnemyHitboxRig layer 26 and the hit-mark profile)");
            }

            Ph("death/loot");
            // ---- death / loot / cleanup
            _observeDeath = true; // no top-ups while the death is observed
            L.ai.enabled = true;
            var lootable = go.GetComponent<EnemyLootable>();
            bool lootExpected = lootable != null && lootable.HasRemainingLoot;
            Type bagType = FindType("EnemyLootBag");
            int bags0 = bagType != null ? Object.FindObjectsByType(bagType).Length : 0;
            float tKill = Time.time;
            L.health.TakeDamage(L.health.MaxHealth * 5f, _player, false);
            yield return new WaitGame(0.25f);
            bool dead = L.health.IsDead;
            float tDead = Time.time - tKill;
            GameObject dropped = L.bridge != null ? L.bridge.LastDroppedWeapon : null;
            float destroyDelay = def != null ? def.destroyDelay : 3f;
            bool respawns = def != null && def.respawnTime > 0f;
            float bagT = -1f, goneT = -1f;
            float limit = Time.time + destroyDelay + 14f;
            while (Time.time < limit)
            {
                if (bagType != null && bagT < 0f && Object.FindObjectsByType(bagType).Length > bags0) bagT = Time.time - tKill;
                if (bagT >= 0f) break; // loot spawned: check it now, never wait for the corpse / bag to despawn
                if (go == null || !go.activeInHierarchy) { goneT = Time.time - tKill; if (!lootExpected) break; }
                yield return new WaitGame(0.25f);
            }

            // loot: use the real loot path (EnemyLootBag.TryLootAll -> DMLootGrant), then clean up immediately
            string lootNote = "";
            if (bagT >= 0f)
            {
                Vector3 corpsePos = go != null ? go.transform.position : _player.transform.position;
                EnemyLootBag lootBag = Object.FindObjectsByType<EnemyLootBag>().Where(b => b != null && b.gameObject.activeInHierarchy).OrderBy(b => Vector3.Distance(b.transform.position, corpsePos)).FirstOrDefault();
                if (lootBag != null)
                {
                    int lootN = lootBag.Entries.Count;
                    bool bagHasLoot = lootBag.HasRemainingLoot;
                    var invBefore = InvCounts();
                    string lootRes = lootBag.TryLootAll().ToString();
                    int lootRemoved = InvRemoveGranted(invBefore); // in-memory only: keep the audit inventory from filling up with loot
                    bool lootOk = lootN > 0 && (lootRes == "Granted" || lootRes == "Partial");
                    bool minZero = def != null && def.randomLootCountMin <= 0;
                    lootNote = $"loot bag held {lootN} entr{(lootN == 1 ? "y" : "ies")}, TryLootAll -> {lootRes}, left {lootBag.Entries.Count}" + (lootRemoved > 0 ? $", {lootRemoved} granted item(s) removed again from the audit inventory" : "") + (def != null ? $"; definition random loot count {def.randomLootCountMin}-{def.randomLootCountMax}" : "");
                    R.Add("Death", "loot", "Loot dropped and can be looted (real loot path), then cleaned up at once",
                        lootOk ? "PASS" : (lootN == 0 && minZero && !bagHasLoot ? "INFO" : (lootN > 0 && bagHasLoot && lootRes == "InventoryFull" ? "WARN" : "FAIL")), lootNote + (bagHasLoot ? "" : ", HasRemainingLoot was false"),
                        lootOk ? "" : (lootN == 0 && minZero && !bagHasLoot ? "the random loot roll gave 0 entries and the definition allows 0 (randomLootCountMin 0): an empty bag is a valid drop chance, not a failure" : lootRes == "InventoryFull" ? "the audit player's inventory was full: loot path reached, cannot confirm the grant" : "bag spawned but nothing could be looted (empty entries / TryLootAll refused)"));
                    Object.Destroy(lootBag.gameObject);
                }
            }

            if (go != null) { bool wasGone = !go.activeInHierarchy; if (wasGone && goneT < 0f) goneT = Time.time - tKill; }

            bool goneNow = go == null || !go.activeInHierarchy;
            if (bagT >= 0f) goneNow = true; // corpse is held by the loot bag by design; the audit removes both right after the check
            var dv = new List<string>();
            var dc = new List<string>();
            string dstat = "PASS";
            if (!dead) { dstat = "FAIL"; dc.Add("EnemyHealth.IsDead not set after lethal damage"); }
            if (lootExpected && bagT < 0f) { dstat = "FAIL"; dc.Add("loot expected (EnemyLootable has loot) but no EnemyLootBag spawned"); }
            if (bagT >= 0f) dv.Add("corpse is held until the loot bag is looted/dissolves (by design)");
            else if (!goneNow && !respawns) { dstat = "FAIL"; dc.Add($"no loot bag and corpse not destroyed/disabled after {destroyDelay + 14f:0}s (destroyDelay {destroyDelay:0.#}s)"); }
            R.Add("Death", "death", "Death, loot bag, cleanup", dstat,
                $"dead {(dead ? "yes" : "NO")} in {tDead:0.00}s, loot bag {(bagT >= 0 ? bagT.ToString("0.0") + "s" : (lootExpected ? "MISSING" : "none"))}, corpse gone {(goneT >= 0 ? goneT.ToString("0.0") + "s" : (bagT >= 0f ? "held by the loot bag (removed by the audit)" : (respawns ? "respawns" : "not within window")))}, dropped weapon {(dropped != null ? dropped.name : "none")}; {string.Join(", ", dv)}",
                string.Join("; ", dc));

            if (go != null) Object.Destroy(go); // never wait for the corpse to despawn
            if (bagType != null)
                foreach (Object b in Object.FindObjectsByType(bagType))
                    if (b is Component bc && bc != null) Object.Destroy(bc.gameObject);
            if (dropped != null) Object.Destroy(dropped);

            RecordClips(L, R);
        }
    }
}
#endif
