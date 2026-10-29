#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Invector.vCharacterController;
using Project.AI;
using Project.Combat;
using Project.Player.Invector;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace Project.EditorTools.Audit
{
    /// <summary>
    /// 2026-10-25s: targeted audit for Combat Slice 4 (stick target switch + camera follow), Phase 4 Combat Director
    /// (slots, flank, morale) and Slice 5 E3 (momentum + spin AoE). Launch with {"slicesOnly":true,"useActiveScene":true,"force":true}.
    /// Drives real KBM input (Input System devices), the player, spawned enemies (SwordMaster / Humadroid / Robot) and the camera.
    /// Scene is never saved; Play exits at the end.
    /// </summary>
    public static partial class DMEnemyRuntimeAudit
    {
        private const string PfDir = "Assets/_Project/Prefabs/Combat/Enemies/";
        private static readonly List<GameObject> _slGos = new List<GameObject>();

        private static GameObject SlPrefab(string n) { return AssetDatabase.LoadAssetAtPath<GameObject>(PfDir + n + ".prefab"); }
        private static string F1(float v) { return v.ToString("0.0", CultureInfo.InvariantCulture); }

        private static GameObject SlSpawn(GameObject prefab, Vector3 pos, Vector3 look, string tag)
        {
            Vector3 d = look - pos; d.y = 0f;
            GameObject g = Object.Instantiate(prefab, Ground(pos), d.sqrMagnitude > 0.01f ? Quaternion.LookRotation(d.normalized) : Quaternion.identity);
            g.name = "AUDIT_S_" + tag;
            _slGos.Add(g);
            return g;
        }

        /// <summary>2026-10-25s: terrain stays ON; wait until a collider (terrain) is under every point.</summary>
        private static IEnumerator SlWaitTerrain(DMAuditEnemyResult R, string G, params Vector3[] pts)
        {
            float t0 = Time.realtimeSinceStartup; int ok = 0;
            while (Time.realtimeSinceStartup - t0 < 25f)
            {
                ok = 0;
                foreach (var p in pts)
                    if (Physics.Raycast(p + Vector3.up * 60f, Vector3.down, out RaycastHit h, 200f, GroundMask, QueryTriggerInteraction.Ignore)) ok++;
                if (ok == pts.Length) break;
                yield return new WaitReal(0.25f);
            }
            float waited = Time.realtimeSinceStartup - t0;
            int terr = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Exclude).Length;
            R.Add(G, "terrain", "Terrain loaded under player + enemies", ok == pts.Length && terr > 0 ? "PASS" : "FAIL", $"ground {ok}/{pts.Length}, active terrains {terr}, waited {waited:0.0}s");
            yield return new WaitGame(0.3f);
        }

        private static void SlClear()
        {
            foreach (var g in _slGos) if (g != null) Object.Destroy(g);
            _slGos.Clear();
        }

        private static float YawTo(Transform from, Vector3 to)
        {
            Vector3 d = to - from.position; d.y = 0f;
            return d.sqrMagnitude < 0.0001f ? 0f : Mathf.Abs(Vector3.SignedAngle(Vector3.ProjectOnPlane(from.forward, Vector3.up), d, Vector3.up));
        }

        private static bool ComboActive()
        {
            var mi = _player != null ? _player.GetComponent<PioneerShooterMeleeInput>() : null;
            return mi != null && mi.IsMeleeComboActive;
        }

        private static IEnumerator SlicesMain()
        {
            GameObject sword = SlPrefab("SwordMaster"), huma = SlPrefab("Humadroid"), robot = SlPrefab("Robot");
            if (sword == null || huma == null || robot == null) { Finish("aborted", "slice audit prefabs missing"); yield break; }
            if (_opts != null && _opts.sliceFilter == "humaMelee") { yield return Scn("Humadroid melee lock: damage + spin", huma, ScnHumaMelee); SlClear(); yield break; }
            yield return Scn("Slice4 stick target switch + camera follow", sword, ScnSlice4);
            SlClear();
            yield return Scn("Phase4 director slots + flank + morale", sword, ScnPhase4);
            SlClear();
            yield return Scn("Slice5 momentum + spin AoE (SwordMaster)", sword, ScnSlice5);
            SlClear();
            yield return Scn("Slice5 momentum + spin AoE (Humadroid)", huma, ScnSlice5);
            SlClear();
            yield return Scn("Slice5 momentum + spin AoE (Robot)", robot, ScnSlice5);
            SlClear();
            // 2026-10-26a
            yield return Scn("Slice5 spin input (hold Attack / charged release)", sword, ScnSpinInput);
            SlClear();
            yield return Scn("Slice6 finisher (SwordMaster)", sword, ScnFinisher);
            SlClear();
            yield return Scn("Slice6 finisher (Humadroid)", huma, ScnFinisher);
            SlClear();
            yield return Scn("Slice6 taunts + group camera pull-back", sword, ScnTauntCamera);
            SlClear();
        }

        // ------------------------------------------------------------ Slice 4
        private static IEnumerator ScnSlice4(GameObject prefab, DMAuditEnemyResult R)
        {
            const string G = "Slice4";
            TopUpPlayer();
            Vector3 c = Ground(_origin + new Vector3(0f, 0f, 22f));
            PlacePlayer(c, c + Vector3.forward);
            CameraBehind();
            yield return SlWaitTerrain(R, G, c, c + Vector3.forward * 2.2f, c + Vector3.right * 2.4f);
            c = Ground(c);
            GameObject a = SlSpawn(prefab, c + Vector3.forward * 2.2f, c, "s4A");
            GameObject b = SlSpawn(prefab, c + Vector3.right * 2.4f + Vector3.forward * 0.3f, c, "s4B");
            yield return new WaitGame(1.2f);
            PlacePlayer(c, a.transform.position);
            CameraBehind();
            DMMeleeAttackFacing.ResetStats();
            var facing = _player.GetComponent<DMMeleeAttackFacing>();

            // 1. swing at A (camera forward) -> acquire A
            yield return ClickLeft(0.08f, 0.25f);
            facing = facing != null ? facing : _player.GetComponent<DMMeleeAttackFacing>();
            string first = facing != null && facing.Target != null ? facing.Target.name : "none";
            R.Add(G, "s4.acquire", "Combo lock acquires the enemy in front", DMMeleeAttackFacing.StatAcquires > 0 && first.EndsWith("s4A") ? "PASS" : "FAIL", "target=" + first + " acquires=" + DMMeleeAttackFacing.StatAcquires);

            // 2. steer the stick right (D) while swinging -> switch to B, snap + camera follow
            float camYaw0 = TpCam() != null ? TpCam().transform.eulerAngles.y : 0f;
            KeyDown(Key.D);
            float t0 = Time.time;
            while (Time.time - t0 < 1.6f && DMMeleeAttackFacing.StatSwitches == 0)
            {
                yield return ClickLeft(0.08f, 0.15f);
            }
            KeyUp(Key.D);
            float switchT = DMMeleeAttackFacing.StatLastSwitchTime;
            string second = facing != null && facing.Target != null ? facing.Target.name : "none";
            R.Add(G, "s4.switch", "Stick steer (D) switches target mid-combo", DMMeleeAttackFacing.StatSwitches > 0 && second.EndsWith("s4B") ? "PASS" : "FAIL", "switches=" + DMMeleeAttackFacing.StatSwitches + " target=" + second);
            yield return ClickLeft(0.08f, 0.2f);
            yield return new WaitGame(0.35f);
            float err = b != null ? YawTo(_player.transform, b.transform.position) : 999f;
            R.Add(G, "s4.snap", "Body snaps to the new target (<= 30 deg error)", err <= 30f ? "PASS" : "WARN", "yawErr=" + F1(err) + " maxFrameDelta=" + F1(DMMeleeAttackFacing.StatMaxFrameYawDelta) + " snapFrames>45=" + DMMeleeAttackFacing.StatSnapFrames);
            float camYaw1 = TpCam() != null ? TpCam().transform.eulerAngles.y : 0f;
            var p = DM_CombatCoreProfile.Live;
            bool camOk = !p.meleeSwitchCameraFollow || DMMeleeAttackFacing.StatCameraFollowFrames > 0 || (b != null && TpCam() != null && YawTo(TpCam().transform, b.transform.position) <= p.meleeSwitchCameraKeepInViewDeg + 5f);
            R.Add(G, "s4.camera", "Camera keeps the new target in view after the switch", camOk ? "PASS" : "FAIL", "followFrames=" + DMMeleeAttackFacing.StatCameraFollowFrames + " camYawDelta=" + F1(Mathf.DeltaAngle(camYaw0, camYaw1)));
            R.Add(G, "s4.nosnapjerk", "No single-frame yaw jump > 75 deg (snap is capped per fixed step; editor fps varies)", DMMeleeAttackFacing.StatMaxFrameYawDelta <= 75f ? "PASS" : "WARN", F1(DMMeleeAttackFacing.StatMaxFrameYawDelta));

            // 3. toggle off -> no switch
            bool was = p.meleeTargetSwitchEnabled;
            p.meleeTargetSwitchEnabled = false;
            yield return WaitMin(0.3f, () => !ComboActive(), 2.5f);
            yield return new WaitGame(1.0f);
            PlacePlayer(c, a != null ? a.transform.position : c + Vector3.forward);
            CameraBehind();
            int sw0 = DMMeleeAttackFacing.StatSwitches;
            yield return ClickLeft(0.08f, 0.2f);
            KeyDown(Key.D);
            t0 = Time.time;
            while (Time.time - t0 < 1.0f) yield return ClickLeft(0.08f, 0.15f);
            KeyUp(Key.D);
            p.meleeTargetSwitchEnabled = was;
            R.Add(G, "s4.toggle", "meleeTargetSwitchEnabled=false keeps the first target", DMMeleeAttackFacing.StatSwitches == sw0 ? "PASS" : "FAIL", "switches during off=" + (DMMeleeAttackFacing.StatSwitches - sw0));
        }

        // ------------------------------------------------------------ Phase 4 director
        private static IEnumerator ScnPhase4(GameObject prefab, DMAuditEnemyResult R)
        {
            const string G = "Phase4";
            TopUpPlayer();
            var d = DM_CombatDirectorProfile.Live;
            Vector3 c = Ground(_origin + new Vector3(0f, 0f, 22f));
            PlacePlayer(c, c + Vector3.forward);
            CameraBehind();
            var tp = new Vector3[6]; tp[0] = c;
            for (int i = 0; i < 5; i++) { float an = (i * 72f - 36f) * Mathf.Deg2Rad; tp[i + 1] = c + new Vector3(Mathf.Sin(an), 0f, Mathf.Cos(an)) * 8f; }
            yield return SlWaitTerrain(R, G, tp);
            c = Ground(c);
            DMEnemyEngagementDirector.ResetPhase4Stats();
            GameObject[] pf = { prefab, prefab, SlPrefab("Humadroid"), SlPrefab("Robot"), prefab };
            var hs = new List<EnemyHealth>();
            for (int i = 0; i < pf.Length; i++)
            {
                float ang = (i * 72f - 36f) * Mathf.Deg2Rad; // front arc spread so flank has to move someone
                Vector3 pos = c + new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang)) * 8f;
                var g = SlSpawn(pf[i], pos, c, "p4_" + i);
                hs.Add(g.GetComponent<EnemyHealth>());
            }
            int maxSlots = 0, maxFlank = 0, members = 0; float bestFlankAngle = 0f;
            float t0 = Time.time, nextTop = Time.time + 2f;
            while (Time.time - t0 < 14f)
            {
                if (Time.time > nextTop) { TopUpPlayer(); nextTop = Time.time + 2f; }
                PlacePlayerKeepFacing(c);
                if (DMEnemyEngagementDirector.TryGetGroupDebug(_player.transform, out float mor, out bool br, out int fl, out int mem, out int slots))
                {
                    maxSlots = Mathf.Max(maxSlots, slots); maxFlank = Mathf.Max(maxFlank, fl); members = Mathf.Max(members, mem);
                }
                foreach (var h in hs)
                {
                    if (h == null) continue;
                    var ai = h.GetComponent<EnemyAiController>();
                    if (DMEnemyEngagementDirector.IsFlanker(ai) && Time.time - ai.EngagementState.FlankSince > 1.5f)
                        bestFlankAngle = Mathf.Max(bestFlankAngle, YawTo(_player.transform, h.transform.position));
                }
                yield return null;
            }
            R.Add(G, "p4.members", "All 5 enemies join the player ring", members >= 5 ? "PASS" : "WARN", "members=" + members);
            R.Add(G, "p4.slots", "Attack slots scale with group (>=2, <= max)", maxSlots >= 2 && maxSlots <= Mathf.Max(1, d.maxSimultaneousAttackers) + d.extraAttackersMax ? "PASS" : "WARN", "maxTotalSlots=" + maxSlots);
            R.Add(G, "p4.flank", "Flank role assigned (>=1) with 5 enemies", DMEnemyEngagementDirector.StatFlankAssigns > 0 ? "PASS" : "FAIL", "assigns=" + DMEnemyEngagementDirector.StatFlankAssigns + " releases=" + DMEnemyEngagementDirector.StatFlankReleases + " maxConcurrent=" + maxFlank);
            R.Add(G, "p4.flankpos", "Flanker reaches the side/back (>= 70 deg off player facing)", bestFlankAngle >= 70f ? "PASS" : "WARN", "bestAngle=" + F1(bestFlankAngle));

            // morale: leader first, then gruesome kills until 3 dead
            var leader = DMEnemyEngagementDirector.GetRingLeader(_player.transform);
            if (leader != null) { var lh = leader.GetComponent<EnemyHealth>(); hs.Remove(lh); hs.Insert(0, lh); }
            int killed = 0;
            foreach (var h in hs)
            {
                if (h == null || h.IsDead || killed >= 3) continue;
                DamageInfo info = DamageInfo.FromHit(99999f, true, _player, h.transform.position + Vector3.up);
                CombatDamageApplicator.ApplyToDamageable(h, h.gameObject, in info);
                killed++;
                yield return new WaitGame(0.6f);
            }
            yield return new WaitGame(1.5f);
            bool got = DMEnemyEngagementDirector.TryGetGroupDebug(_player.transform, out float morale, out bool broken, out int f2, out int m2, out int s2);
            R.Add(G, "p4.morale", "Kills lower group morale (leader + crit kills)", (DMEnemyEngagementDirector.StatMoraleDrops >= 3 || (DMEnemyEngagementDirector.StatMoraleDrops >= 2 && DMEnemyEngagementDirector.StatMoraleBreaks > 0)) ? "PASS" : "FAIL", "drops=" + DMEnemyEngagementDirector.StatMoraleDrops + " morale=" + F1(morale) + " ring=" + got);
            R.Add(G, "p4.break", "Morale breaks below threshold -> 1 attacker, outer ring", DMEnemyEngagementDirector.StatMoraleBreaks > 0 && (!got || !broken || s2 <= 1) ? "PASS" : "FAIL", "breaks=" + DMEnemyEngagementDirector.StatMoraleBreaks + " broken=" + broken + " slots=" + s2 + " flankers=" + f2);
        }

        private static void PlacePlayerKeepFacing(Vector3 c)
        {
            Vector3 d = _player.transform.position - c; d.y = 0f;
            if (d.sqrMagnitude > 2.25f) PlacePlayer(c, c + Vector3.forward);
        }

        // ------------------------------------------------------------ Slice 5 momentum + spin
        private static IEnumerator ScnSlice5(GameObject prefab, DMAuditEnemyResult R)
        {
            string G = "Slice5 " + prefab.name;
            var mp = DM_MomentumProfile.Live;
            TopUpPlayer();
            Vector3 c = Ground(_origin + new Vector3(0f, 0f, 22f));
            PlacePlayer(c, c + Vector3.forward);
            CameraBehind();
            yield return SlWaitTerrain(R, G, c, c + Vector3.forward * 1.9f, c - Vector3.forward * 1.9f);
            c = Ground(c);
            PlacePlayer(c, c + Vector3.forward);
            DMPlayerMomentum.Reset();
            DMMomentumSpin.ResetStats();

            // reject with empty meter
            bool rej = !DMMomentumSpin.TrySpin(_player) && DMMomentumSpin.LastReject == "momentum";
            R.Add(G, "s5.reject", "Spin rejected with empty meter", rej || mp.spinCost <= 0f ? "PASS" : "FAIL", "reject=" + DMMomentumSpin.LastReject);

            GameObject a = SlSpawn(prefab, c + Vector3.forward * 1.9f, c, "s5A");
            yield return new WaitGame(1.0f);
            PlacePlayer(c, a.transform.position);
            CameraBehind();
            var ehA = a != null ? a.GetComponentInChildren<EnemyHealth>() : null; float hp0 = ehA != null ? ehA.CurrentHealth : -1f;
            DiagReset();
            for (int i = 0; i < 6; i++) { if (a != null) { Vector3 d = a.transform.position - c; d.y = 0f; if (d.magnitude > 2.2f) a.transform.position = Ground(c + (d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward) * 1.7f); PlacePlayer(c, a.transform.position); } var it = ClickLeft(0.08f, 0.3f); while (true) { DiagSample(ehA); if (!it.MoveNext()) break; yield return it.Current; } }
            R.Add(G, "s5.hitdiag", "Melee contact diagnostics (weapon trigger vs enemy hit volumes)", "INFO", DiagRig(a) + " | " + DiagString());
            yield return new WaitGame(0.3f);
            float v = DMPlayerMomentum.Value;
            R.Add(G, "s5.gain", "Melee hits build momentum", DMPlayerMomentum.StatGains > 0 && v > 0f ? "PASS" : "FAIL", "gains=" + DMPlayerMomentum.StatGains + " value=" + F1(v) + " total=" + F1(DMPlayerMomentum.StatTotalGained) + " swingsSeen=" + _swingsSeen + " enemyHp=" + F1(hp0) + "->" + F1(ehA != null ? ehA.CurrentHealth : -1f));
            yield return WaitMin(0.3f, () => !ComboActive(), 2.5f);
            yield return new WaitGame(0.4f);
            v = DMPlayerMomentum.Value;
            yield return new WaitGame(mp.decayDelaySeconds + 1.2f);
            float v2 = DMPlayerMomentum.Value;
            R.Add(G, "s5.decay", "Meter decays after the delay", v <= 0f || v2 < v ? "PASS" : "FAIL", F1(v) + " -> " + F1(v2));

            // 2026-10-26a: no melee damage -> contact probe (runtime instance only; prefab untouched)
            if (ehA != null && a != null && !ehA.IsDead && Mathf.Abs(ehA.CurrentHealth - hp0) < 0.01f)
            {
                float hpA = ehA.CurrentHealth; DiagReset();
                for (int i = 0; i < 4; i++) { if (a == null) break; a.transform.position = Ground(c + Vector3.forward * 1.2f); PlacePlayer(c, a.transform.position); var it = ClickLeft(0.08f, 0.3f); while (true) { DiagSample(ehA); if (!it.MoveNext()) break; yield return it.Current; } }
                string ra = "close 1.2m: dmg=" + F1(hpA - ehA.CurrentHealth) + " " + DiagString();
                yield return WaitMin(0.3f, () => !ComboActive(), 2.5f);
                if (a != null) foreach (var hbx in a.GetComponentsInChildren<DMEnemyHitbox>(true))
                {
                    if (hbx.Shape is CapsuleCollider cap) cap.radius *= 1.6f;
                    else if (hbx.Shape is SphereCollider sph) sph.radius *= 1.6f;
                    else if (hbx.Shape is BoxCollider bx) bx.size *= 1.6f;
                }
                float hpB = ehA.CurrentHealth; DiagReset();
                for (int i = 0; i < 4; i++) { if (a == null) break; a.transform.position = Ground(c + Vector3.forward * 1.7f); PlacePlayer(c, a.transform.position); var it = ClickLeft(0.08f, 0.3f); while (true) { DiagSample(ehA); if (!it.MoveNext()) break; yield return it.Current; } }
                R.Add(G, "s5.hitprobe", "No-damage contact probe (info)", "INFO", ra + " || hitbox radius x1.6 @1.7m: dmg=" + F1(hpB - ehA.CurrentHealth) + " " + DiagString());
            }

            // spin: enemy in front + enemy behind
            yield return WaitMin(0.2f, () => !ComboActive(), 2.5f);
            TopUpPlayer();
            GameObject b = SlSpawn(prefab, c - Vector3.forward * 1.9f, c, "s5B");
            if (a != null) { a.transform.position = Ground(c + Vector3.forward * 1.9f); }
            yield return new WaitGame(0.6f);
            PlacePlayer(c, c + Vector3.forward);
            var ha = a != null ? a.GetComponent<EnemyHealth>() : null; var hb = b.GetComponent<EnemyHealth>();
            float yaw0 = _player.transform.eulerAngles.y;
            var cc = _player.GetComponent<vThirdPersonController>();
            bool lock0 = cc != null && cc.lockRotation;
            DMPlayerMomentum.DevFill();
            bool started = DMMomentumSpin.TrySpin(_player);
            float meterAfter = DMPlayerMomentum.Value;
            float total = mp.spinWindupSeconds + mp.spinSeconds + mp.spinRecoverySeconds;
            float maxStep = 0f, last = yaw0, tS = Time.time;
            float endYaw = float.NaN;
            while (Time.time - tS < total + 0.3f)
            {
                if (float.IsNaN(endYaw) && Time.time - tS > 0.05f && !DMMomentumSpin.AnySpinning) endYaw = _player.transform.eulerAngles.y;
                float y = _player.transform.eulerAngles.y;
                maxStep = Mathf.Max(maxStep, Mathf.Abs(Mathf.DeltaAngle(last, y))); last = y;
                yield return null;
            }
            R.Add(G, "s5.spin", "Spin starts with a full meter and spends it", started && meterAfter < DMPlayerMomentum.Max ? "PASS" : "FAIL", "started=" + started + " reject=" + DMMomentumSpin.LastReject + " meterAfter=" + F1(meterAfter));
            R.Add(G, "s5.sweep", "Full 360 sweep, returns to start facing", DMMomentumSpin.StatLastSpinDegrees >= 355f && Mathf.Abs(Mathf.DeltaAngle(yaw0, float.IsNaN(endYaw) ? _player.transform.eulerAngles.y : endYaw)) < 8f ? "PASS" : "FAIL", "deg=" + F1(DMMomentumSpin.StatLastSpinDegrees) + " endErr(at spin end)=" + F1(Mathf.DeltaAngle(yaw0, float.IsNaN(endYaw) ? _player.transform.eulerAngles.y : endYaw)) + " maxFrameStep=" + F1(maxStep));
            R.Add(G, "s5.hits", "Spin hits front AND back enemy once each", DMMomentumSpin.StatSpinHits == 2 ? "PASS" : (DMMomentumSpin.StatSpinHits > 0 ? "WARN" : "FAIL"), "hits=" + DMMomentumSpin.StatSpinHits);
            R.Add(G, "s5.lock", "Invector rotation lock restored after the spin", cc != null && cc.lockRotation == lock0 && !DMMomentumSpin.AnySpinning ? "PASS" : "FAIL", "lock=" + (cc != null && cc.lockRotation) + " spinning=" + DMMomentumSpin.AnySpinning);
        }
        // ------------------------------------------------------------ 2026-10-26b Humadroid melee lock
        private static IEnumerator ScnHumaMelee(GameObject prefab, DMAuditEnemyResult R)
        {
            const string G = "HumaMelee";
            var mlp = DM_MeleeLockProfile.Live;
            TopUpPlayer();
            Vector3 c = Ground(_origin + new Vector3(0f, 0f, 22f));
            PlacePlayer(c, c + Vector3.forward);
            CameraBehind();
            yield return SlWaitTerrain(R, G, c, c + Vector3.forward * 6f, c - Vector3.forward * 1.9f);
            c = Ground(c);
            PlacePlayer(c, c + Vector3.forward);
            DMPlayerMomentum.Reset(); DMMomentumSpin.ResetStats();
            var mi = _player.GetComponent<PioneerShooterMeleeInput>();
            GameObject a = SlSpawn(prefab, c + Vector3.forward * 6f, c, "hmA");
            var ai = a.GetComponent<EnemyAiController>();
            var eh = a.GetComponentInChildren<EnemyHealth>();
            float hp0 = eh != null ? eh.CurrentHealth : -1f;
            // approach phase: player stands with sword drawn, enemy must close (no teleport)
            float t0 = Time.time, minD = 99f, lockedFrames = 0, frames = 0, nextTop = Time.time + 2f;
            while (Time.time - t0 < 6f && a != null)
            {
                if (Time.time > nextTop) { TopUpPlayer(); nextTop = Time.time + 2f; }
                PlacePlayer(c, a.transform.position);
                float d = Flat(a.transform.position - _player.transform.position);
                minD = Mathf.Min(minD, d); frames++; if (ai != null && ai.MeleeLockActive) lockedFrames++;
                yield return null;
            }
            R.Add(G, "hm.drawn", "Player has a melee weapon drawn", mi != null && mi.IsDrawnMeleeActive ? "PASS" : "FAIL", "drawn=" + (mi != null && mi.IsDrawnMeleeActive));
            R.Add(G, "hm.lock", "Melee lock engages vs melee player", lockedFrames > frames * 0.5f ? "PASS" : "FAIL", "locked=" + lockedFrames + "/" + frames + " hold=" + F1(mlp.holdDistance));
            R.Add(G, "hm.close", "Humadroid closes to melee range on its own (<= 1.6 m)", minD <= 1.6f ? "PASS" : "FAIL", "minDist=" + F1(minD));
            // spin first (enemy in place after its own approach)
            if (a != null && eh != null && !eh.IsDead)
            {
                float hpS0 = eh.CurrentHealth; int h0 = DMMomentumSpin.StatSpinHits;
                PlacePlayer(c, a.transform.position);
                DMPlayerMomentum.DevFill();
                bool st0 = DMMomentumSpin.TrySpin(_player);
                var mp0 = DM_MomentumProfile.Live;
                yield return new WaitGame(mp0.spinWindupSeconds + mp0.spinSeconds + mp0.spinRecoverySeconds + 0.3f);
                R.Add(G, "hm.spin", "Momentum spin hits + damages the Humadroid", st0 && DMMomentumSpin.StatSpinHits > h0 && (eh == null || eh.CurrentHealth < hpS0) ? "PASS" : "FAIL", "started=" + st0 + " reject=" + DMMomentumSpin.LastReject + " spinHits=" + (DMMomentumSpin.StatSpinHits - h0) + " hp " + F1(hpS0) + "->" + F1(eh != null ? eh.CurrentHealth : 0f) + " dist=" + F1(a != null ? Flat(a.transform.position - _player.transform.position) : -1f));
                DMPlayerMomentum.Reset(); hp0 = eh != null ? eh.CurrentHealth : hp0;
            }
            // swing phase: enemy NOT teleported; record distance during swings
            int swings = 10; float sumD = 0f, maxD = 0f; int samples = 0; int sw0 = _swingsSeen; int pHits = 0;
            System.Action<DamageInfo, GameObject> onHit = (inf, tg) => { if (tg != null && _player != null && (tg == _player || tg.transform.IsChildOf(_player.transform))) pHits++; };
            CombatEvents.HitApplied += onHit;
            DiagReset();
            for (int i = 0; i < swings && a != null && eh != null && !eh.IsDead; i++)
            {
                TopUpPlayer();
                yield return WaitMin(0.05f, () => !ComboActive(), 1.2f);
                PlacePlayer(c, a.transform.position);
                var it = ClickLeft(0.08f, 0.3f);
                while (true)
                {
                    if (a != null) { float d = Flat(a.transform.position - _player.transform.position); sumD += d; maxD = Mathf.Max(maxD, d); samples++; }
                    DiagSample(eh);
                    if (!it.MoveNext()) break; yield return it.Current;
                }
            }
            yield return WaitMin(0.3f, () => !ComboActive(), 2.5f);
            CombatEvents.HitApplied -= onHit;
            float dmg = eh != null ? hp0 - eh.CurrentHealth : hp0;
            R.Add(G, "hm.damage", "Player melee swings damage the Humadroid", dmg > 0f && DMPlayerMomentum.StatGains >= 3 ? "PASS" : (dmg > 0f ? "WARN" : "FAIL"), "hp " + F1(hp0) + "->" + F1(eh != null ? eh.CurrentHealth : 0f) + " hits(gains)=" + DMPlayerMomentum.StatGains + "/" + swings + " avgDist=" + F1(samples > 0 ? sumD / samples : 0f) + " maxDist=" + F1(maxD) + " swingsSeen=" + (_swingsSeen - sw0) + " playerHitBy=" + pHits);
            R.Add(G, "hm.diag", "Contact diagnostics (info)", "INFO", DiagString());
            // spin vs Humadroid (in place, not teleported)
            if (a != null && eh != null && !eh.IsDead)
            {
                float hpS = eh.CurrentHealth; int hits0 = DMMomentumSpin.StatSpinHits;
                PlacePlayer(c, a.transform.position);
                DMPlayerMomentum.DevFill();
                bool started = DMMomentumSpin.TrySpin(_player);
                var mp = DM_MomentumProfile.Live;
                yield return new WaitGame(mp.spinWindupSeconds + mp.spinSeconds + mp.spinRecoverySeconds + 0.3f);
                R.Add(G, "hm.spin2", "Second spin after swings (info)", started && DMMomentumSpin.StatSpinHits > hits0 && (eh == null || eh.CurrentHealth < hpS) ? "PASS" : "FAIL", "started=" + started + " reject=" + DMMomentumSpin.LastReject + " spinHits=" + (DMMomentumSpin.StatSpinHits - hits0) + " hp " + F1(hpS) + "->" + F1(eh != null ? eh.CurrentHealth : 0f) + " dist=" + F1(a != null ? Flat(a.transform.position - _player.transform.position) : -1f));
            }
            else R.Add(G, "hm.spin2", "Second spin after swings (info)", "INFO", "Humadroid already killed by swings");
        }

        // ------------------------------------------------------------ 2026-10-26a melee contact diagnostics
        private static int _dFrames, _dCanApply, _dOverlapHitbox, _dOverlapAny, _dTriggers; // 28k: removed unused _dDamaged (CS0414)
        private static float _dMinDist = 99f;
        private static readonly HashSet<string> _dLayers = new HashSet<string>();
        private static readonly Collider[] _dBuf = new Collider[48];

        private static void DiagReset() { _dFrames = _dCanApply = _dOverlapHitbox = _dOverlapAny = _dTriggers = 0; _dMinDist = 99f; _dLayers.Clear(); }

        private static void DiagSample(EnemyHealth eh)
        {
            if (_player == null || eh == null) return;
            _dFrames++;
            var mm = _player.GetComponentInChildren<global::Invector.vMelee.vMeleeManager>();
            var w = mm != null ? mm.rightWeapon : null;
            if (w == null || !w.canApplyDamage || w.hitBoxes == null) return;
            _dCanApply++;
            foreach (var hb in w.hitBoxes)
            {
                if (hb == null || hb.trigger == null || !hb.trigger.enabled) continue;
                _dTriggers++;
                Bounds b = hb.trigger.bounds;
                _dMinDist = Mathf.Min(_dMinDist, Vector3.Distance(b.ClosestPoint(eh.transform.position + Vector3.up * 1f), eh.transform.position + Vector3.up * 1f));
                int n = Physics.OverlapBoxNonAlloc(b.center, b.extents, _dBuf, Quaternion.identity, DMEnemyHitQuery.HitboxMask, QueryTriggerInteraction.Collide);
                for (int i = 0; i < n; i++) if (_dBuf[i] != null && _dBuf[i].GetComponentInParent<EnemyHealth>() == eh) { _dOverlapHitbox++; break; }
                n = Physics.OverlapBoxNonAlloc(b.center, b.extents, _dBuf, Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
                for (int i = 0; i < n; i++)
                    if (_dBuf[i] != null && _dBuf[i].GetComponentInParent<EnemyHealth>() == eh) { _dOverlapAny++; _dLayers.Add(LayerMask.LayerToName(_dBuf[i].gameObject.layer) + ":" + _dBuf[i].name); }
            }
        }

        private static string DiagString()
        {
            return "frames=" + _dFrames + " dmgWindowFrames=" + _dCanApply + " weaponTriggers=" + _dTriggers + " overlapDMHitbox=" + _dOverlapHitbox + " overlapAnyEnemyCollider=" + _dOverlapAny + " minDist(weapon->chest)=" + F1(_dMinDist) + " touched=[" + string.Join(",", _dLayers) + "]";
        }

        private static string DiagRig(GameObject e)
        {
            if (e == null) return "enemy gone";
            var rig = e.GetComponent<DMEnemyHitboxRig>();
            var boxes = e.GetComponentsInChildren<DMEnemyHitbox>(true);
            int live = 0; float lo = 99f, hi = -99f; Vector3 root = e.transform.position;
            foreach (var hb in boxes)
            {
                if (hb == null || hb.Shape == null) continue;
                if (hb.IsLive) live++;
                Bounds b = hb.Shape.bounds; lo = Mathf.Min(lo, b.min.y - root.y); hi = Mathf.Max(hi, b.max.y - root.y);
            }
            var cap = e.GetComponent<CapsuleCollider>();
            var anim = e.GetComponent<Animator>();
            Renderer[] rs = e.GetComponentsInChildren<SkinnedMeshRenderer>();
            float rlo = 99f, rhi = -99f; foreach (var r in rs) if (r.enabled) { rlo = Mathf.Min(rlo, r.bounds.min.y - root.y); rhi = Mathf.Max(rhi, r.bounds.max.y - root.y); }
            return "rig=" + (rig != null ? (rig.isActiveAndEnabled ? "on" : "off") : "none") + " hitboxes=" + boxes.Length + " live=" + live + " hitY=" + F1(lo) + ".." + F1(hi)
                + " meshY=" + F1(rlo) + ".." + F1(rhi) + " rootCap=" + (cap != null ? ("h" + F1(cap.height) + " r" + F1(cap.radius) + " trig=" + cap.isTrigger + " L=" + LayerMask.LayerToName(cap.gameObject.layer)) : "none")
                + " human=" + (anim != null && anim.isHuman) + " scale=" + F1(e.transform.lossyScale.y);
        }

        // ------------------------------------------------------------ 2026-10-26a Slice 5 spin input
        private static IEnumerator ScnSpinInput(GameObject prefab, DMAuditEnemyResult R)
        {
            const string G = "SpinInput";
            var mp = DM_MomentumProfile.Live;
            bool holdWas = mp.spinOnHoldAttack, autoWas = mp.spinAutoOnChargedRelease;
            TopUpPlayer();
            Vector3 c = Ground(_origin + new Vector3(0f, 0f, 22f));
            PlacePlayer(c, c + Vector3.forward);
            CameraBehind();
            yield return SlWaitTerrain(R, G, c, c + Vector3.forward * 2f);
            c = Ground(c);
            PlacePlayer(c, c + Vector3.forward);
            DMMomentumSpin.ResetStats();
            // tap with a full meter must NOT spin
            DMPlayerMomentum.DevFill();
            yield return ClickLeft(0.08f, 0.5f);
            R.Add(G, "si.tap", "Tap Attack with a full meter = normal swing, no spin", DMMomentumSpin.StatSpins == 0 ? "PASS" : "FAIL", "spins=" + DMMomentumSpin.StatSpins);
            yield return WaitMin(0.3f, () => !ComboActive(), 2.5f);
            // hold Attack -> spin
            DMMomentumSpin.ResetStats();
            DMPlayerMomentum.DevFill();
            yield return ClickLeft(mp.spinHoldSeconds + 0.35f, 0.2f);
            yield return WaitMin(0.2f, () => !DMMomentumSpin.AnySpinning, 2.5f);
            R.Add(G, "si.hold", "Hold Attack (LMB) with a full meter spins", DMMomentumSpin.StatHoldSpins == 1 ? "PASS" : "FAIL", "holdSpins=" + DMMomentumSpin.StatHoldSpins + " spins=" + DMMomentumSpin.StatSpins + " reject=" + DMMomentumSpin.LastReject + " holdSec=" + F1(mp.spinHoldSeconds));
            yield return WaitMin(0.4f, () => !ComboActive(), 2.5f);
            // hold with an empty meter -> normal charge, no spin
            DMMomentumSpin.ResetStats();
            DMPlayerMomentum.Reset();
            yield return ClickLeft(mp.spinHoldSeconds + 0.35f, 0.2f);
            yield return WaitMin(0.6f, () => !ComboActive(), 3f);
            R.Add(G, "si.empty", "Hold with an empty meter = charged swing, no spin", DMMomentumSpin.StatSpins == 0 ? "PASS" : "FAIL", "spins=" + DMMomentumSpin.StatSpins);
            // charged release (hold-spin off) -> spin
            mp.spinOnHoldAttack = false;
            DMMomentumSpin.ResetStats();
            DMPlayerMomentum.DevFill();
            yield return ClickLeft(1.1f, 0.2f);
            yield return WaitMin(0.2f, () => !DMMomentumSpin.AnySpinning, 2.5f);
            mp.spinOnHoldAttack = holdWas;
            R.Add(G, "si.charged", "Charged release with a full meter spins (auto on charged release ON)", autoWas && DMMomentumSpin.StatChargedSpins == 1 ? "PASS" : "FAIL", "chargedSpins=" + DMMomentumSpin.StatChargedSpins + " auto=" + autoWas + " reject=" + DMMomentumSpin.LastReject);
            var cc = _player.GetComponent<vThirdPersonController>();
            R.Add(G, "si.lock", "Rotation lock restored, no stuck charge", cc != null && !cc.lockRotation && !DMMomentumSpin.AnySpinning ? "PASS" : "WARN", "lockRot=" + (cc != null && cc.lockRotation));
        }

        // ------------------------------------------------------------ 2026-10-26a Slice 6 finisher
        private static IEnumerator ScnFinisher(GameObject prefab, DMAuditEnemyResult R)
        {
            string G = "Finisher " + prefab.name;
            var fp = DM_FinisherTauntProfile.Live;
            TopUpPlayer();
            Vector3 c = Ground(_origin + new Vector3(0f, 0f, 22f));
            PlacePlayer(c, c + Vector3.forward);
            CameraBehind();
            yield return SlWaitTerrain(R, G, c, c + Vector3.forward * 1.8f);
            c = Ground(c);
            PlacePlayer(c, c + Vector3.forward);
            DMSlice6Runtime.ResetStats();
            GameObject a = SlSpawn(prefab, c + Vector3.forward * 1.8f, c, "f6A");
            yield return new WaitGame(1.0f);
            var h = a.GetComponent<EnemyHealth>();
            PlacePlayer(c, a.transform.position);
            CameraBehind();
            string probe = DMSlice6Runtime.ProbeFinisher(_player, out _);
            R.Add(G, "f6.healthy", "No finisher on a healthy, unstaggered enemy", probe != null ? "PASS" : "FAIL", "probe=" + (probe ?? "offered"));
            // bring to low HP (not lethal)
            float hpTarget = h.MaxHealth * Mathf.Max(0.02f, fp.finisherLowHpFraction * 0.5f);
            DamageInfo info = DamageInfo.FromHit(Mathf.Max(0f, h.CurrentHealth - hpTarget), false, _player, a.transform.position + Vector3.up);
            info.PoiseDamage = 0f;
            CombatDamageApplicator.ApplyToDamageable(h, h.gameObject, in info);
            yield return new WaitGame(0.6f);
            if (a != null) { a.transform.position = Ground(c + Vector3.forward * 1.6f); PlacePlayer(c, a.transform.position); }
            yield return new WaitGame(0.1f);
            probe = DMSlice6Runtime.ProbeFinisher(_player, out EnemyHealth pt);
            R.Add(G, "f6.offer", "Finisher offered on a low-HP enemy in front", probe == null && pt == h ? "PASS" : "FAIL", "probe=" + (probe ?? "offered") + " hp=" + F1(h.CurrentHealth) + "/" + F1(h.MaxHealth));
            var cc = _player.GetComponent<vThirdPersonController>();
            bool lockM0 = cc.lockMovement, lockR0 = cc.lockRotation;
            yield return ClickLeft(0.08f, 0.1f);
            bool started = DMSlice6Runtime.StatFinishers > 0;
            float maxPair = 0f, minPair = 99f, t0 = Time.time;
            bool lockedDuring = false;
            while (Time.time - t0 < fp.finisherMaxSeconds + 1.5f && DMSlice6Runtime.FinisherActive)
            {
                if (a != null && h != null && !h.IsDead) { float dd = Flat(a.transform.position - _player.transform.position); maxPair = Mathf.Max(maxPair, dd); if (Time.time - t0 > fp.finisherAlignSeconds + 0.1f) minPair = Mathf.Min(minPair, dd); }
                lockedDuring |= cc.lockMovement;
                yield return null;
            }
            float dur = Time.time - t0;
            R.Add(G, "f6.start", "Attack press starts the finisher", started ? "PASS" : "FAIL", "finishers=" + DMSlice6Runtime.StatFinishers + " reject=" + DMSlice6Runtime.LastFinisherReject);
            R.Add(G, "f6.kill", "Finisher kills the enemy", h == null || h.IsDead ? "PASS" : "FAIL", "kills=" + DMSlice6Runtime.StatFinisherKills + " dead=" + (h == null || h.IsDead));
            R.Add(G, "f6.align", "Enemy aligned at pair distance during the finisher", minPair < 99f && Mathf.Abs(minPair - fp.finisherPairDistance) < 0.35f ? "PASS" : "WARN", "pairDist=" + F1(minPair) + " target=" + F1(fp.finisherPairDistance));
            R.Add(G, "f6.lock", "Player locked during, restored after", lockedDuring && cc.lockMovement == lockM0 && cc.lockRotation == lockR0 && !DMSlice6Runtime.FinisherActive ? "PASS" : "FAIL", "lockedDuring=" + lockedDuring + " after=" + cc.lockMovement + "/" + cc.lockRotation + " dur=" + F1(dur));
        }


        // ------------------------------------------------------------ 2026-10-26a Slice 6 taunts + camera pull-back
        private static IEnumerator ScnTauntCamera(GameObject prefab, DMAuditEnemyResult R)
        {
            const string G = "Slice6 taunt+camera";
            var fp = DM_FinisherTauntProfile.Live;
            float chanceWas = fp.tauntChancePerSecond, groupWas = fp.tauntGroupCooldown;
            TopUpPlayer();
            Vector3 c = Ground(_origin + new Vector3(0f, 0f, 22f));
            PlacePlayer(c, c + Vector3.forward);
            CameraBehind();
            var pts = new List<Vector3> { c };
            for (int i = 0; i < 5; i++) pts.Add(c + Quaternion.Euler(0f, -60f + i * 30f, 0f) * Vector3.forward * 6f);
            yield return SlWaitTerrain(R, G, pts.ToArray());
            c = Ground(c);
            PlacePlayer(c, c + Vector3.forward);
            var cam = TpCam();
            float zoom0 = cam != null ? cam.CurrentZoom : 0f;
            DMSlice6Runtime.ResetStats();
            var hs = new List<EnemyHealth>();
            for (int i = 0; i < 5; i++) { var g = SlSpawn(prefab, pts[i + 1], c, "t6_" + i); hs.Add(g.GetComponent<EnemyHealth>()); }
            fp.tauntChancePerSecond = 1f; fp.tauntGroupCooldown = 0.5f;
            float maxPull = 0f, maxStep = 0f, lastZoom = 0f, t0 = Time.time;
            int tauntsBad = 0, maxConc = 0;
            while (Time.time - t0 < 14f)
            {
                TopUpPlayer();
                maxPull = Mathf.Max(maxPull, DMSlice6Runtime.CameraPullback);
                maxConc = Mathf.Max(maxConc, DMSlice6Runtime.ActiveTaunts);
                foreach (var h in hs)
                {
                    if (h == null) continue;
                    var ai = h.GetComponent<EnemyAiController>();
                    if (ai != null && DMSlice6Runtime.IsTaunting(ai) && ai.EngagementState.AttackSlot && ai.EngagementState.Role == DMEngagementRole.Engager) tauntsBad++;
                }
                { float pb = DMSlice6Runtime.CameraPullback; if (Time.deltaTime > 0f) maxStep = Mathf.Max(maxStep, Mathf.Abs(pb - lastZoom) / Time.deltaTime); lastZoom = pb; }
                yield return null;
            }
            fp.tauntChancePerSecond = chanceWas; fp.tauntGroupCooldown = groupWas;
            R.Add(G, "t6.taunt", "Holders taunt during the Director wait/ring", DMSlice6Runtime.StatTaunts > 0 ? "PASS" : "FAIL", "taunts=" + DMSlice6Runtime.StatTaunts + " cancels=" + DMSlice6Runtime.StatTauntCancels + " maxConcurrent=" + maxConc);
            R.Add(G, "t6.limit", "Taunt concurrency within limit", maxConc <= fp.tauntMaxConcurrent ? "PASS" : "FAIL", "maxConcurrent=" + maxConc + " limit=" + fp.tauntMaxConcurrent);
            R.Add(G, "t6.noattack", "No enemy taunts while holding an attack slot", tauntsBad == 0 ? "PASS" : "WARN", "frames=" + tauntsBad);
            R.Add(G, "c6.pull", "Camera pulls back with a large group", maxPull >= 0.3f * fp.pullbackMaxExtraDistance ? "PASS" : "FAIL", "maxPull=" + F1(maxPull) + "m of " + F1(fp.pullbackMaxExtraDistance) + " zoom0=" + F1(zoom0));
            R.Add(G, "c6.smooth", "Camera pull-back speed stays smooth (< 4 m/s)", maxStep < 4f ? "PASS" : "WARN", "maxSpeed m/s=" + maxStep.ToString("0.000", CultureInfo.InvariantCulture));
            // kill the group -> return
            foreach (var h in hs) if (h != null && !h.IsDead) { DamageInfo info = DamageInfo.FromHit(99999f, false, _player, h.transform.position + Vector3.up); CombatDamageApplicator.ApplyToDamageable(h, h.gameObject, in info); }
            float t1 = Time.time;
            while (Time.time - t1 < fp.pullbackReturnDelay + fp.pullbackReturnSeconds * 3f + 1f && DMSlice6Runtime.CameraPullback > 0.02f) yield return null;
            float zoomEnd = cam != null ? cam.CurrentZoom : 0f;
            R.Add(G, "c6.return", "Camera returns to the player's zoom after the fight", DMSlice6Runtime.CameraPullback <= 0.02f && Mathf.Abs(zoomEnd - zoom0) < 0.15f ? "PASS" : "FAIL", "pull=" + F1(DMSlice6Runtime.CameraPullback) + " zoom " + F1(zoom0) + "->" + F1(zoomEnd) + " in " + F1(Time.time - t1) + "s");
        }
    }
}
#endif
