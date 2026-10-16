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
using UnityEngine.Events;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace Project.EditorTools.Audit
{
    public static partial class DMEnemyRuntimeAudit
    {
        // =============================================================== scenarios (template enemy)
        // Player-driven scenarios: block / parry (player and enemy side), scripted movement (retreat, push, flank, lose target),
        // multi-enemy, player weapon matrix. Everything goes through the same paths a user has: injected Mouse input, the
        // inventory/equipment API, the enemy's own combat bridge.
        private static ushort _mouseMask;

        private static void SetMouse(ushort mask)
        {
            Mouse m = Mouse.current ?? InputSystem.AddDevice<Mouse>();
            _mouseMask = mask;
            if (m != null) Inject(m, new MouseState { buttons = mask });
        }

        private const ushort MouseLeft = 1;
        private const ushort MouseRight = 2;

        /// <summary>Clicks that actually started a player swing (isAttacking seen during or right after the click). Separates 'the weapon failed' from 'the click never reached the game'.</summary>
        private static int _swingsSeen;

        private static IEnumerator ClickLeft(float hold, float gap)
        {
            SetMouse((ushort)(_mouseMask | MouseLeft));
            float tc = Time.time; bool saw = false;
            while (Time.time - tc < hold) { saw |= PlayerAttacking(); yield return null; }
            SetMouse((ushort)(_mouseMask & ~MouseLeft));
            tc = Time.time;
            while (!saw && Time.time - tc < 0.25f) { saw |= PlayerAttacking(); yield return null; }
            if (saw) _swingsSeen++;
            yield return WaitMin(Mathf.Min(0.35f, gap), () => !PlayerAttacking(), gap); // next click as soon as the swing is over
        }

        private static bool PlayerBlocking() { return PlayerFlag("isBlocking"); }   // isBlocking is a PROPERTY on vMeleeCombatInput (a field lookup always read false)

        private static float PlayerStamina() { return PlayerNumber("currentStamina"); }

        private static float CombatProfileValue(string field, float fallback)
        {
            try
            {
                Type pt = FindType("DM_CombatCoreProfile");
                if (pt == null) return fallback;
                foreach (string g in AssetDatabase.FindAssets("t:" + pt.Name))
                {
                    Object o = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(g), pt);
                    FieldInfo f = pt.GetField(field);
                    if (o != null && f != null) return Convert.ToSingle(f.GetValue(o), CultureInfo.InvariantCulture);
                }
            }
            catch { /* fall through */ }
            return fallback;
        }

        private static Vector3 FlatForward(Transform t)
        {
            Vector3 f = t.forward; f.y = 0f;
            return f.sqrMagnitude < 0.01f ? Vector3.right : f.normalized;
        }

        private static float FacingAngle(Transform enemy, Vector3 target)
        {
            Vector3 to = target - enemy.position; to.y = 0f;
            if (to.sqrMagnitude < 0.0001f) return 0f;
            return Vector3.Angle(FlatForward(enemy), to.normalized);
        }

        private static float PlayerEnemyDist(Live L)
        {
            Vector3 d = _player.transform.position - L.go.transform.position; d.y = 0f;
            return d.magnitude;
        }

        /// <summary>Moves the player along a per-frame path (position function of elapsed seconds), always facing the enemy.</summary>
        private static IEnumerator MovePlayer(Live L, Func<float, Vector3> pos, float seconds, Action<float> perFrame = null)
        {
            float t0 = Time.time;
            while (true)
            {
                float t = Time.time - t0;
                if (t >= seconds || L.go == null) break;
                PlacePlayer(Ground(pos(t)), L.go.transform.position);
                perFrame?.Invoke(t);
                yield return null;
            }
        }

        private static bool PlayerIsArmed()
        {
            var pm = _player != null ? _player.GetComponent<vMeleeManager>() : null;
            return pm != null && pm.rightWeapon != null;
        }

        private static IEnumerator RunScenarios()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MatrixPrefab);
            if (prefab == null) yield break;
            yield return Scn("Camera: faces forward after teleports, zoom in/out", prefab, ScnCamera);
            yield return Scn("Equip: weapon survives a teleport", prefab, ScnEquipTeleport);
            yield return Scn("Audio: swing / hit sound wiring (static)", prefab, ScnAudioFacts);
            yield return Scn("Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch", prefab, ScnControls);
            yield return Scn("Block + parry: player defends against enemy melee", prefab, ScnPlayerBlockParry);
            yield return Scn("Block: enemy defends against player swings", prefab, ScnEnemyBlock);
            yield return Scn("Movement: retreat, push, flank, backstab, lose target", prefab, ScnMovement);
            yield return Scn("Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return", prefab, ScnPlay);
            yield return Scn("Multi-enemy: FRED x3 and x5", prefab, ScnMulti);
            yield return Scn("Brain: orbit, toggles, archetypes", prefab, ScnBrain);
            yield return Scn("Player weapons: each melee item vs enemy (swapped in place)", prefab, ScnPlayerWeapons);
            yield return Scn("Ammo: default ammo per ranged weapon + ammo-specific FX", prefab, ScnAmmo);
            yield return Scn("Player rig: enemy ranged shot hits a body bone + mark", prefab, ScnPlayerRig);
            yield return Scn("Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits", prefab, ScnAim);
            yield return Scn("Behavior tweaks: over / under-exaggerated enemy values (runtime only)", prefab, ScnTweaks);
        }

        private static IEnumerator ScnAudioFacts(GameObject prefab, DMAuditEnemyResult R)
        {
            AudioStaticFacts(R);
            yield break;
        }

        private static bool ScenarioUsesEnemies(string name) { return name.StartsWith("Multi") || name.StartsWith("Brain") || name.StartsWith("Behavior") || name.StartsWith("Movement") || name.StartsWith("Play"); }

        private static IEnumerator Scn(string name, GameObject prefab, Func<GameObject, DMAuditEnemyResult, IEnumerator> body)
        {
            string ukey = "scenario|" + name;
            string extra = "scn|" + name + "|" + _tplStamp;
            if (ScenarioUsesEnemies(name)) extra += "|" + string.Join("", EnemyPrefabs().Select(p => AssetStamp(AssetDatabase.GetAssetPath(p))));
            string uhash = UnitHash(MatrixPrefab, extra + (name.StartsWith("Ammo") ? "|" + _opts.mode : ""), true);
            string alt = name.StartsWith("Ammo") ? UnitHash(MatrixPrefab, extra + "|full", true) : null; // a Full run satisfies a Quick request
            if (TryReuse(ukey, uhash, alt, out DMAuditEnemyResult reused)) { _run.scenarios.Add(reused); SetStatus("scenario: " + name + " (cached)"); yield break; }
            var R = new DMAuditEnemyResult { name = name, prefabPath = MatrixPrefab, definition = prefab.name, note = "scenario" };
            _run.scenarios.Add(R);
            _cur = R;
            SetStatus("scenario: " + name);
            AuditTiming.Begin(ukey);
            float started = Time.realtimeSinceStartup;
            yield return body(prefab, R);
            _observeDeath = false; _enemyTopUp = true;
            R.seconds = Time.realtimeSinceStartup - started;
            ReleaseAllInput();
            AudioProbe.End();
            _camFollow = true;
            if (_zoomNet != 0) { int back = -_zoomNet; _zoomNet = 0; yield return ScrollZoom(back); _zoomNet = 0; }
            FinalizeConsole(R);
            Cleanup();
            TopUpPlayer();
            yield return new WaitGame(0.2f);
            AuditTiming.End();
            RecordUnit(ukey, uhash, R);
            _cur = null;
        }

        // ------------------------------------------------------------ block / parry (player)
        private static IEnumerator ScnPlayerBlockParry(GameObject prefab, DMAuditEnemyResult R)
        {
            yield return ScrollZoom(2); // zoom in to watch the hit
            Live L = SpawnMatrixEnemy(prefab, "block");
            yield return SpawnReady(L);
            if (L.bridge == null || L.mm == null || L.combat == null) { R.Add("Block", "blk.setup", "Setup", "FAIL", "template missing bridge/vMeleeManager/combat", ""); yield break; }
            ItemData melee = L.bridge.ResolvedMelee;
            L.bridge.TrySwitchTo(melee, EnemyWeaponRequester.Brain, 300f);
            var ragdoll = L.go.GetComponent<EnemyInvectorRagdollBridge>();
            float blockStagger = CombatProfileValue("blockStaggerSeconds", 0.4f);
            float parryStagger = CombatProfileValue("parryStaggerSeconds", 5f);
            float parryWindow = CombatProfileValue("parryWindowSeconds", 0.2f);

            int hits = 0, blocked = 0;
            float lastHitT = -1f;
            UnityAction<vHitInfo> lis = hi => { hits++; lastHitT = Time.time; if (hi != null && hi.targetIsBlocking) blocked++; };
            L.mm.onDamageHit.AddListener(lis);

            Vector3 f = FlatForward(L.go.transform);
            PlacePlayer(Ground(L.go.transform.position + f * 1.5f), L.go.transform.position);
            yield return Settle(L);

            // A: unblocked baseline
            hits = blocked = 0;
            L.BeginPhase();
            int aB = L.attackStarts;
            yield return new WaitUntil(() => L.attackStarts > aB || hits > 0, 10f);
            float tAtkB = Time.time;
            yield return new WaitUntil(() => hits > 0, 14f);
            float baseWindup = hits > 0 && lastHitT >= tAtkB ? lastHitT - tAtkB : -1f; // attack start -> hit landing, used to time the parry taps
            float dmgBase = L.PhaseDamage;
            R.Add("Block", "blk.base", "Unblocked baseline (same enemy, same distance)", hits > 0 ? "INFO" : "WARN", $"hits {hits}, player hp lost {dmgBase:0.0}", hits > 0 ? "" : "enemy never hit the player, block comparison not possible");
            yield return Settle(L);
            yield return WaitStaggerEnd(ragdoll, 8f);
            TopUpPlayer();
            PlacePlayer(Ground(L.go.transform.position + FlatForward(L.go.transform) * 1.5f), L.go.transform.position);

            // B: hold block (RMB, the same input a player uses)
            float tPress = Time.time;
            SetMouse(MouseRight);
            yield return new WaitUntil(() => PlayerBlocking(), 1.5f);
            float raise = Time.time - tPress;
            bool isBlk = PlayerBlocking();
            yield return new WaitGame(0.3f);
            hits = blocked = 0;
            L.BeginPhase();
            float t0 = Time.time, stagStart = -1f, stagLongest = 0f;
            while (Time.time - t0 < 14f && hits < 2)
            {
                bool sg = ragdoll != null && ragdoll.IsHitStaggerActive;
                if (sg) { if (stagStart < 0f) stagStart = Time.time; stagLongest = Mathf.Max(stagLongest, Time.time - stagStart); } else stagStart = -1f;
                yield return null;
            }

            float dmgBlk = L.PhaseDamage;
            string cause = "";
            string st = "PASS";
            if (!isBlk) { st = "FAIL"; cause = "player isBlocking never became true after holding RMB: injected right-click not received (Game view focus), stamina 0, or player in customAction/attacking"; }
            else if (hits == 0) { st = "WARN"; cause = "no enemy hit reached the player in 14s while blocking (enemy staggered/out of reach), inconclusive"; }
            else if (blocked < hits) { st = "FAIL"; cause = $"{hits - blocked} hit(s) registered with targetIsBlocking=false: attacker outside meleeBlockDefenseHalfAngle, or block state dropped (stamina {Fmt(PlayerStamina())})"; }
            else if (dmgBlk >= 1f) { st = "FAIL"; cause = "blocked hits still reduced player health: TryAbsorbBlockedMelee/ReduceDamage not applied or another damage path (survival stats) bypasses the block"; }
            R.Add("Block", "blk.hold", "Holding block negates enemy melee damage", st,
                $"BLOCK = hold right mouse. isBlocking {isBlk} (raised in {raise:0.00}s), hits {hits}, blocked {blocked}, player hp lost {dmgBlk:0.0} (unblocked {dmgBase:0.0}), stamina {Fmt(PlayerStamina())}", cause);
            R.Add("Block", "blk.stagger", "Enemy guard-break stagger after a blocked hit", stagLongest > 0f ? "INFO" : "WARN",
                $"longest enemy stagger {stagLongest:0.00}s (profile blockStaggerSeconds {blockStagger:0.0#}s)",
                stagLongest > 0f ? "" : "no enemy hit-stagger observed after the block (DMEnemyGuardBreakStagger skipped: no EnemyInvectorRagdollBridge/attacker, or profile blockStaggerSeconds 0)");
            SetMouse(0);
            yield return WaitStaggerEnd(ragdoll, 8f);

            // C: parry = TAP right mouse (0.08s) timed against the incoming hit. The tap time is relative to the PREDICTED HIT (attack start + the windup measured in the baseline),
            // so offsets -0.60..-0.02 mean "tap that long before the blow lands"; +0.10 is a tap after it landed.
            if (baseWindup < 0.15f) baseWindup = 0.6f; // baseline unusable: assume a typical windup
            float[] offs = { -0.60f, -0.40f, -0.25f, -0.15f, -0.08f, -0.02f, 0.10f };
            int inWinTried = 0, inWinParried = 0, outWinParried = 0, counterStag = 0;
            var rows = new List<string>();
            foreach (float off0 in offs)
            {
                TopUpPlayer();
                yield return WaitStaggerEnd(ragdoll, 8f);
                yield return Settle(L);
                PlacePlayer(Ground(L.go.transform.position + FlatForward(L.go.transform) * 1.5f), L.go.transform.position);
                hits = blocked = 0; lastHitT = -1f;
                L.BeginPhase();
                int a0 = L.attackStarts;
                yield return new WaitUntil(() => L.attackStarts > a0 || hits > 0, 10f);
                float ta = Time.time;
                float pressAt = Mathf.Max(0f, baseWindup + off0);
                while (Time.time - ta < pressAt && hits == 0) yield return null;
                float press = Time.time;
                SetMouse(MouseRight);                       // TAP: down ...
                yield return new WaitGame(0.08f);
                SetMouse(0);                                // ... and up
                float hold = Time.time;
                stagStart = -1f; stagLongest = 0f;
                while (Time.time - hold < 3.5f)
                {
                    bool sg = ragdoll != null && ragdoll.IsHitStaggerActive;
                    if (sg) { if (stagStart < 0f) stagStart = Time.time; stagLongest = Mathf.Max(stagLongest, Time.time - stagStart); }
                    else if (stagStart >= 0f && hits > 0) break;
                    yield return null;
                }

                SetMouse(0);
                string kind;
                float lead = lastHitT >= 0f ? lastHitT - press : float.NaN; // seconds between the tap and the blow actually landing
                if (hits == 0) kind = "no hit (whiff or enemy staggered)";
                else if (blocked == 0) kind = "HIT (tap too early/late, not blocking when it landed)";
                else if (stagLongest >= (blockStagger + parryStagger) * 0.5f) kind = "PARRY";
                else kind = "BLOCK";
                bool inWin = !float.IsNaN(lead) && lead >= 0f && lead <= parryWindow;
                if (inWin) inWinTried++;
                if (kind == "PARRY") { if (inWin || (!float.IsNaN(lead) && lead <= parryWindow + 0.05f)) { inWinParried++; if (stagLongest > 0f) counterStag++; } else outWinParried++; }
                rows.Add($"tap {off0:+0.00;-0.00}s vs predicted hit (windup {baseWindup:0.00}s): blow landed {(float.IsNaN(lead) ? "n/a" : lead.ToString("0.00") + "s")} after the tap -> {kind}, enemy stagger {stagLongest:0.00}s, hp lost {L.PhaseDamage:0.0}");
            }

            L.mm.onDamageHit.RemoveListener(lis);
            R.Add("Parry", "parry.sweep", "Parry = tap right mouse, timed before the incoming hit (several timings)", "INFO", string.Join(" || ", rows), $"input: PARRY = TAP right mouse (0.08s), BLOCK = HOLD right mouse; profile: parryWindowSeconds {parryWindow:0.##}, blockStagger {blockStagger:0.0#}s, parryStagger {parryStagger:0.0#}s");
            string pst = inWinParried > 0 && outWinParried == 0 ? "PASS" : "WARN";
            R.Add("Parry", "parry.window", "A tap inside the parry window parries (and an early/late tap does not)", pst,
                inWinParried > 0 ? $"{inWinParried} parry(ies) from taps landing inside the {parryWindow:0.##}s window (of {inWinTried} such taps), {outWinParried} outside it" : $"no parry observed ({inWinTried} taps landed inside the window; see sweep rows)",
                inWinParried > 0 ? (outWinParried > 0 ? "also saw a parry outside the window" : "") : "no sampled tap produced a parry: enemy attack timing varies between swings (windup from the baseline swing may not match), or the parry window/stagger in the profile differs from what the rows show");
            R.Add("Parry", "parry.counter", "A successful parry staggers the attacker (counter opening)", counterStag > 0 ? "PASS" : (inWinParried > 0 ? "WARN" : "INFO"),
                counterStag > 0 ? $"{counterStag} parry(ies) left the enemy staggered (longest values in the sweep rows)" : "no parry happened, so the counter stagger could not be observed",
                counterStag > 0 ? "" : (inWinParried > 0 ? "parry registered but no enemy hit-stagger was observed (EnemyInvectorRagdollBridge.IsHitStaggerActive)" : ""));
        }
        private static IEnumerator WaitStaggerEnd(EnemyInvectorRagdollBridge r, float max)
        {
            if (r == null) yield break;
            float t0 = Time.time;
            while (r.IsHitStaggerActive && Time.time - t0 < max) yield return null;
            yield return new WaitGame(0.4f);
        }

        // ------------------------------------------------------------ block (enemy)
        private static IEnumerator ScnEnemyBlock(GameObject prefab, DMAuditEnemyResult R)
        {
            yield return ScrollZoom(2); // zoom in to watch the hit
            if (!PlayerIsArmed()) ArmPlayerWithSword();
            yield return new WaitGame(1.2f);
            if (!PlayerIsArmed()) { R.Add("Block", "eblk", "Enemy blocks player swings", "SKIP", "player could not be armed", ""); yield break; }
            Live L = SpawnMatrixEnemy(prefab, "eblock");
            yield return SpawnReady(L);
            var cb = L.go.GetComponent<EnemyInvectorCombatBridge>();
            var pm = _player.GetComponent<vMeleeManager>();
            if (cb == null || pm == null) { R.Add("Block", "eblk", "Enemy blocks player swings", "SKIP", "no EnemyInvectorCombatBridge / player vMeleeManager", ""); yield break; }
            L.ai.enabled = false;
            L.trackPlayer = false;
            int hits = 0, blocked = 0;
            UnityAction<vHitInfo> lis = hi => { hits++; if (hi != null && hi.targetIsBlocking) blocked++; };
            pm.onDamageHit.AddListener(lis);
            Vector3 front = L.go.transform.position + FlatForward(L.go.transform) * 1.4f;

            PlacePlayer(Ground(front), L.go.transform.position);
            yield return new WaitGame(0.5f);
            float hp0 = EHp(L);
            for (int i = 0; i < 4; i++) { KeepAt(Ground(front), L.go.transform.position); yield return ClickLeft(0.15f, 1.1f); }
            float lostOpen = hp0 - EHp(L); int hitsOpen = hits;

            hits = blocked = 0;
            bool began = cb.TryBeginBlock(40f, out float dur);
            yield return new WaitGame(0.5f);
            bool animBlock = L.anim != null && L.anim.GetBool(vAnimatorParameters.IsBlocking);
            string clips = L.anim != null ? string.Join(", ", Enumerable.Range(0, L.anim.layerCount).SelectMany(li => L.anim.GetCurrentAnimatorClipInfo(li).Select(c => c.clip != null ? c.clip.name : "?")).Distinct().Take(6)) : "";
            float hp1 = EHp(L);
            for (int i = 0; i < 4; i++) { KeepAt(Ground(front), L.go.transform.position); yield return ClickLeft(0.15f, 1.1f); }
            float lostBlk = hp1 - EHp(L); int hitsBlk = hits, blockedBlk = blocked;
            cb.EndBlock();
            pm.onDamageHit.RemoveListener(lis);

            string st, cause = "";
            if (hitsOpen == 0) { st = "WARN"; cause = "player swings registered no hits on the open enemy, so the comparison is inconclusive (input focus, reach, or the player weapon)"; }
            else if (!began || !animBlock) { st = "FAIL"; cause = "TryBeginBlock did not put the enemy animator in IsBlocking"; }
            else if (blockedBlk == 0 && hitsBlk > 0) { st = "FAIL"; cause = "player hits landed with targetIsBlocking=false while the enemy animator was blocking: the enemy's vMeleeManager/receiver does not read the block (defense angle, isBlocking not mirrored to the fighter)"; }
            else if (lostBlk >= lostOpen * 0.9f && lostOpen > 0f) { st = "FAIL"; cause = "blocking did not reduce the damage the enemy takes (no defense rate applied on the enemy receiver)"; }
            else st = "PASS";
            R.Add("Block", "eblk", "Enemy block reduces player melee damage", st,
                $"open: {hitsOpen} hits, enemy hp lost {lostOpen:0.0}; blocking (animator IsBlocking {animBlock}, duration {dur:0.0}s): {hitsBlk} hits ({blockedBlk} flagged blocked), enemy hp lost {lostBlk:0.0}; block clips: {clips}", cause);
            R.Add("Parry", "eparry", "Enemy parry", "INFO", "not implemented: EnemyInvectorCombatBridge only has TryBeginBlock/EndBlock (no parry window/riposte API)", "");
        }

        // ------------------------------------------------------------ movement
        private static IEnumerator ScnMovement(GameObject prefab, DMAuditEnemyResult R)
        {
            Live L = SpawnMatrixEnemy(prefab, "move");
            yield return SpawnReady(L);
            if (L.ai == null) yield break;
            Transform et = L.go.transform;
            Vector3 f0 = FlatForward(et);
            PlacePlayer(Ground(et.position + f0 * 8f), et.position);
            L.BeginPhase();
            yield return new WaitUntil(() => L.noticeT >= 0f, 10f);
            yield return new WaitGame(1f);

            // retreat
            float startD = PlayerEnemyDist(L);
            Vector3 fromPos = _player.transform.position;
            Vector3 away = (fromPos - et.position); away.y = 0f; away = away.normalized;
            float minD = 99f; int a0 = L.attackStarts;
            L.BeginPhase();
            yield return MovePlayer(L, t => fromPos + away * (3f * t), 3f, t => minD = Mathf.Min(minD, PlayerEnemyDist(L)));
            float endD = PlayerEnemyDist(L);
            bool follows = endD < startD + 9f - 3f; // player moved 9m; enemy closed at least 3m of it
            R.Add("Movement", "mv.retreat", "Player retreats 9m at 3 m/s: enemy follows", follows ? "PASS" : "WARN",
                $"distance {startD:0.0}m -> {endD:0.0}m (min {minD:0.0}m), states {string.Join("/", L.phaseStates)}, attacks {L.attackStarts - a0}",
                follows ? "" : "enemy did not keep up with a retreating player (chase speed, lose-target, or standoff hold)");

            // push toward the enemy
            yield return new WaitGame(1.0f);
            Vector3 dirIn = (et.position - _player.transform.position); dirIn.y = 0f; dirIn = dirIn.normalized;
            Vector3 start = et.position - dirIn * 6f;
            PlacePlayer(Ground(start), et.position);
            yield return new WaitGame(0.8f);
            minD = 99f; float maxFace = 0f; int attackFrames = 0;
            L.BeginPhase();
            Vector3 pStart = _player.transform.position;
            yield return MovePlayer(L, t => { Vector3 dir = (et.position - pStart); dir.y = 0f; return pStart + dir.normalized * Mathf.Min(2f * t, Mathf.Max(0f, dir.magnitude - 0.5f)); }, 3.2f,
                t => { minD = Mathf.Min(minD, PlayerEnemyDist(L)); maxFace = Mathf.Max(maxFace, FacingAngle(et, _player.transform.position)); if (AiState(L.ai) == "Attack") attackFrames++; });
            string pushSt = minD < 0.45f ? "WARN" : "PASS";
            R.Add("Movement", "mv.push", "Player pushes into the enemy: spacing and facing", pushSt,
                $"min distance {minD:0.00}m, max facing error {maxFace:0}deg, attack-state frames {attackFrames}, states {string.Join("/", L.phaseStates)}",
                minD < 0.45f ? "enemy lets the player walk into its body (no personal-space retreat / standoff distance below capsule radius)" : "");

            // flank: circle to behind the enemy
            yield return new WaitGame(1.0f);
            PlacePlayer(Ground(et.position + FlatForward(et) * 2.5f), et.position);
            yield return new WaitGame(0.8f);
            float ang0 = Mathf.Atan2(_player.transform.position.x - et.position.x, _player.transform.position.z - et.position.z);
            float turnT = -1f, tBehind = -1f; int atkBehind0 = -1; float maxFaceFlank = 0f;
            L.BeginPhase();
            float hpStart = PlayerHpInvector();
            yield return MovePlayer(L, t =>
            {
                float a = ang0 + Mathf.Min(t / 1.5f, 1f) * Mathf.PI; // 180 degrees in 1.5s, then hold behind
                return et.position + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * 2.5f;
            }, 7f, t =>
            {
                float rel = Vector3.SignedAngle(FlatForward(et), _player.transform.position - et.position, Vector3.up);
                bool behind = Mathf.Abs(rel) > 140f;
                if (behind && tBehind < 0f) { tBehind = t; atkBehind0 = L.attackStarts; }
                float face = FacingAngle(et, _player.transform.position);
                if (tBehind >= 0f) { maxFaceFlank = Mathf.Max(maxFaceFlank, face); if (turnT < 0f && face < 30f) turnT = t - tBehind; }
            });
            int atkBehind = atkBehind0 >= 0 ? L.attackStarts - atkBehind0 : 0;
            string fst = tBehind < 0f ? "WARN" : (turnT < 0f ? "FAIL" : (turnT > 2.5f ? "WARN" : "PASS"));
            R.Add("Movement", "mv.flank", "Player circles behind the enemy: enemy turns to face", fst,
                $"reached behind at {FmtT(tBehind)}, enemy faced player (<30deg) after {FmtT(turnT)}, max facing error {maxFaceFlank:0}deg, attacks while behind {atkBehind}, player hp lost {Fmt(hpStart - PlayerHpInvector())}, states {string.Join("/", L.phaseStates)}",
                tBehind < 0f ? "player never got 140deg behind the moving enemy (enemy rotated with the circle): that itself shows it tracks the player" :
                turnT < 0f ? "enemy never turned to face a player standing behind it within 5s (turn-in-place disabled or target lost)" :
                turnT > 2.5f ? "slow turn: > 2.5s to face a flanking player (turn speed / only turns when attacking)" : "");

            // backstab / hit from behind (player real swings)
            L.ai.enabled = false;
            if (!PlayerIsArmed()) ArmPlayerWithSword();
            yield return new WaitGame(1.0f);
            if (PlayerIsArmed() && L.health != null)
            {
                var pm = _player.GetComponent<vMeleeManager>();
                int hits = 0;
                UnityAction<vHitInfo> lis = hi => hits++;
                pm.onDamageHit.AddListener(lis);
                Vector3 front = et.position + FlatForward(et) * 1.4f;
                Vector3 back = et.position - FlatForward(et) * 1.4f;
                float h0 = EHp(L); hits = 0;
                for (int i = 0; i < 3; i++) { KeepAt(Ground(front), et.position); yield return ClickLeft(0.15f, 1.1f); }
                float lostFront = h0 - EHp(L); int hf = hits;
                float h1 = EHp(L); hits = 0;
                for (int i = 0; i < 3; i++) { KeepAt(Ground(back), et.position); yield return ClickLeft(0.15f, 1.1f); }
                float lostBack = h1 - EHp(L); int hb = hits;
                pm.onDamageHit.RemoveListener(lis);
                string bst = hf == 0 || hb == 0 ? "WARN" : "INFO";
                R.Add("Movement", "mv.backstab", "Hit from behind vs from the front (player melee, AI off)", bst,
                    $"front: {hf} hits, {lostFront:0.0} hp; behind: {hb} hits, {lostBack:0.0} hp; ratio {(lostFront > 0f ? (lostBack / lostFront).ToString("0.00") : "n/a")}",
                    bst == "WARN" ? "swings did not land from one side (reach/facing/input), comparison inconclusive" : "no backstab bonus implemented if ratio is 1.0");
            }
            else R.Add("Movement", "mv.backstab", "Hit from behind", "SKIP", "player not armed", "");
            L.ai.enabled = true;

            // lose target
            PlacePlayer(Ground(et.position + FlatForward(et) * 7f), et.position);
            L.BeginPhase();
            yield return new WaitUntil(() => AiState(L.ai) == "Chase" || AiState(L.ai) == "Attack", 8f);
            bool aware = AiState(L.ai) == "Chase" || AiState(L.ai) == "Attack";
            float tGo = Time.time;
            PlacePlayer(Ground(et.position + new Vector3(70f, 0f, 0f)), et.position);
            float tLost = -1f, tInvest = -1f;
            while (Time.time - tGo < 35f)
            {
                string s = AiState(L.ai);
                if (tInvest < 0f && (s == "Investigate" || s == "Search")) tInvest = Time.time - tGo;
                if (s == "Wander" || s == "Patrol" || s == "ReturnHome" || s == "Idle") { tLost = Time.time - tGo; break; }
                yield return null;
            }

            R.Add("Movement", "mv.lose", "Player disappears 70m away: enemy loses the target", !aware ? "WARN" : (tLost < 0f ? "WARN" : "PASS"),
                $"was aware {aware}, investigate/search after {FmtT(tInvest)}, back to calm state after {FmtT(tLost)}, states {string.Join("/", L.phaseStates)}",
                !aware ? "enemy was not aware at the start, inconclusive" : (tLost < 0f ? "enemy still chasing/searching 35s after the player vanished (lose-target / memory time too long)" : ""));
        }

        // ------------------------------------------------------------ player weapons (melee: swapped in place on ONE enemy)
        // Ranged weapons are covered by the Ammo scenario (default ammo): one enemy, no duplicated shooting.
        private static IEnumerator ScnPlayerWeapons(GameObject prefab, DMAuditEnemyResult R)
        {
            var pm = _player.GetComponent<vMeleeManager>();
            Cleanup();
            Live L = SpawnMatrixEnemy(prefab, "pw");
            yield return SpawnReady(L);
            L.ai.enabled = false;
            L.trackPlayer = false;
            Vector3 pos = Ground(L.go.transform.position + FlatForward(L.go.transform) * 1.4f);
            PlacePlayer(pos, L.go.transform.position);
            yield return new WaitGame(0.3f);
            foreach (ItemData item in AllWeaponItems())
            {
                if (item.isMiningTool) { R.Add("Player weapons", "pw." + item.name, $"Player uses '{item.name}'", "INFO", "mining tool: Plasma Fuel scanner/laser, not a damage weapon (damage 0 by design)", ""); continue; }
                bool melee = item.itemType == ItemType.MeleeWeapon;
                if (!melee) continue; // ranged weapons: see the Ammo scenario (default ammo)
                string err = ArmPlayerWithItem(item);
                yield return WaitMin(0.3f, () => EquippedOk(item), 1.5f);
                if (!EquippedOk(item)) yield return EquipAfterTeleport(item, pos, L.go.transform.position);
                if (err.Length > 0 && !EquippedOk(item)) { R.Add("Player weapons", "pw." + item.name, $"Player uses '{item.name}'", "FAIL", err, "player cannot equip this item through the inventory/equipment path"); continue; }
                EnemyTopUpNow(L);
                int hits = 0;
                var hitTimes = new List<float>();
                UnityAction<vHitInfo> lis = hi => { hits++; hitTimes.Add(Time.time); };
                if (pm != null) pm.onDamageHit.AddListener(lis);
                AudioProbe.Begin();
                float h0 = EHp(L);
                int swing0 = _swingsSeen;
                for (int i = 0; i < 4; i++)
                {
                    KeepAt(Ground(pos), L.go.transform.position);
                    yield return ClickLeft(0.2f, 1.1f);
                    SetMouse(0);
                }

                if (pm != null) pm.onDamageHit.RemoveListener(lis);
                int hitAudio = hitTimes.Count(ht => AudioProbe.Window(ht - 0.05f, ht + 0.5f).Count > 0);
                int swung = _swingsSeen - swing0; // swings that really started
                // ambient loops (wind howling etc.) start AudioSources too: they are not swing sounds
                int swingAudio = AudioProbe.Starts.Count(a => !hitTimes.Any(ht => a.t >= ht - 0.05f && a.t <= ht + 0.5f) && (a.clip == null || (a.clip.IndexOf("wind", StringComparison.OrdinalIgnoreCase) < 0 && a.clip.IndexOf("ambient", StringComparison.OrdinalIgnoreCase) < 0)));
                R.Add("Audio", "audio.pswing." + item.name, $"Player '{item.name}' swing plays a swish", swung == 0 ? "WARN" : (swingAudio > 0 ? "PASS" : "FAIL"), $"{swung}/4 clicks started a swing; {swingAudio} non-ambient audio start(s) outside the hit windows: {AudioProbe.Describe(AudioProbe.Starts.Take(3).ToList())}", swung == 0 ? "no swing started, so the swing sound could not be judged (click input not received)" : (swingAudio > 0 ? "" : "no swing sound: GameAudioManager.PlayWeaponSwing has no callers"));
                if (hitTimes.Count > 0)
                    R.Add("Audio", "audio.phit." + item.name, $"Player '{item.name}' hitting the enemy plays a hit sound", hitAudio > 0 ? "PASS" : "FAIL", $"{hitAudio}/{hitTimes.Count} hit(s) had audio within 0.5s", hitAudio > 0 ? "" : "PioneerInvectorDamageReceiver -> MeleeHitAudio.PlayMeleeHit produced no AudioSource (profile meleeHitClips empty or GameAudioManager missing)");
                AudioProbe.End();
                float lost = h0 - EHp(L);
                float idmg = item.meleeDamage;
                string cause = "";
                string st = lost > 0f ? "PASS" : (swung == 0 ? "WARN" : "FAIL");
                if (lost <= 0f && swung == 0) cause = "no swing started: the left-click never reached the player (harness/input, not the weapon); the weapon could not be judged";
                else if (lost <= 0f) cause = $"{swung}/4 clicks swung, {hits} weapon hit event(s) on the enemy: " + (hits == 0 ? "weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received" : "hits registered but no damage applied (damage mapped to 0 or resolver rejected)");
                else if (idmg <= 0f) { st = "WARN"; cause = "damage dealt although item damage is 0 (fallback damage)"; }
                R.Add("Player weapons", "pw." + item.name, $"Player uses melee '{item.name}' on the enemy", st,
                    $"enemy hp lost {lost:0.0} (item dmg {idmg:0.#}), hits {hits}, player right weapon {(pm != null && pm.rightWeapon != null ? pm.rightWeapon.name : "none")}", cause);
            }
        }
    }
}
#endif
