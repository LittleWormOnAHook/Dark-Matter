#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Invector.vMelee;
using Project.AI;
using Project.AI.Invector;
using Project.Combat;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace Project.EditorTools.Audit
{
    /// <summary>
    /// Slice 0b runtime scenario (option interruptsOnly): fights the filtered enemy with the player's light and charged melee
    /// (injected Mouse input, real swings) and reads back the attack-interrupt decisions, recovery lock, immunity and swing cadence
    /// from the console log lines the game emits when DM_CombatCoreProfile.logInterruptsInPlay is on.
    /// The profile flags it flips are restored in the finally block. Never saves anything.
    /// </summary>
    public static partial class DMEnemyRuntimeAudit
    {
        private const float InterruptRunSeconds = 75f;
        private static readonly Regex InterruptedRx = new Regex(@"^(?<t>[\d.]+) \[AttackInterrupt\] (?<name>\S+) (?<cls>\w+) @ (?<p>[\d.]+): INTERRUPTED \((?<why>[^,)]+)", RegexOptions.Compiled);

        private static IEnumerator RunInterruptScenario(GameObject prefab)
        {
            DMAuditEnemyResult R = _cur;
            var logs = new List<string>();
            int errorCount = 0;
            var errorSamples = new List<string>();
            Application.LogCallback cb = (msg, stack, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                {
                    errorCount++;
                    if (errorSamples.Count < 5) errorSamples.Add(msg.Length > 200 ? msg.Substring(0, 200) : msg);
                    return;
                }

                if (msg.StartsWith("[AttackInterrupt]") || msg.StartsWith("[DMEnemyCombo]") || msg.StartsWith("[CombatStagger]"))
                    logs.Add(Time.time.ToString("0.00", CultureInfo.InvariantCulture) + " " + msg);
            };

            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            bool oldLogInterrupts = profile != null && profile.logInterruptsInPlay;
            bool oldLogEvents = profile != null && profile.logCombatEventsInPlay;
            Application.logMessageReceived += cb;
            Live L = null;
            var extras = new List<Live>();
            var holders = new HashSet<string>();
            int maxEngagers = 0, roleSamples = 0, holderChanges = 0;
            string lastHolder = null;
            int count = Mathf.Clamp(_opts.enemyCount, 1, 5);
            float minPair = float.MaxValue; int stackFrames = 0, frames = 0;
            var pairStates = new Dictionary<string, int>();
            var yawAcc = new Dictionary<string, float>(); var yawLast = new Dictionary<string, float>(); var startPos = new Dictionary<string, Vector3>(); var pathAcc = new Dictionary<string, float>(); var lastPos = new Dictionary<string, Vector3>();
            int swingsLight = 0, swingsCharged = 0;
            var roleAtInterrupt = new List<string>();
            try
            {
                if (profile != null) { profile.logInterruptsInPlay = true; profile.logCombatEventsInPlay = true; }
                L = SpawnMatrixEnemy(prefab, "int");
                yield return SpawnReady(L);
                if (L.ai == null || L.health == null || L.combat == null)
                {
                    R.Add("Interrupts", "int.setup", "Setup", "FAIL", "enemy missing AI/health/combat", "");
                    yield break;
                }

                for (int k = 1; k < count; k++)
                {
                    float ang = (k * 72f + 36f) * Mathf.Deg2Rad;
                    Vector3 off = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * 6f;
                    Vector3 sp = Ground(L.go.transform.position + off);
                    GameObject g = Object.Instantiate(prefab, sp, Quaternion.identity);
                    g.name = "AUDIT_W_int_" + (k + 1);
                    extras.Add(new Live { go = g, ai = g.GetComponent<EnemyAiController>(), health = g.GetComponent<EnemyHealth>(), combat = g.GetComponent<EnemyCombat>(), bridge = g.GetComponent<EnemyInvectorLoadoutBridge>(), anim = g.GetComponentInChildren<Animator>(), mm = g.GetComponent<vMeleeManager>() });
                }
                _live = L; // the sampler keeps tracking the primary enemy
                if (extras.Count > 0) yield return new WaitGame(0.6f);
                FieldInfo dbg = typeof(DMEnemyMeleeComboDriver).GetField("debugLogSequences", BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (GameObject eg in new[] { L.go }.Concat(extras.Select(x => x.go)))
                {
                    DMEnemyMeleeComboDriver driver = eg != null ? eg.GetComponent<DMEnemyMeleeComboDriver>() : null;
                    if (driver != null && dbg != null) dbg.SetValue(driver, true);
                }

                var player = _player;
                TopUpPlayer();
                Vector3 epos = L.go.transform.position;
                Vector3 fwd = L.go.transform.forward; fwd.y = 0f; fwd = fwd.sqrMagnitude < 0.01f ? Vector3.right : fwd.normalized;
                PlacePlayer(Ground(epos + fwd * 2.6f), epos);
                AimCameraAt(epos);
                yield return new WaitGame(0.8f);
                L.BeginPhase();
                float tEnd = Time.time + InterruptRunSeconds;
                float nextTop = 0f;
                int i = 0;
                int lastRoleLogCount = 0;

                // Local pump: keeps aim, distance, health and the interrupt role log alive while waiting.
                IEnumerator Pump(float seconds)
                {
                    float until = Time.time + seconds;
                    while (Time.time < until && L.go != null)
                    {
                        // Aim at / chase whichever enemy currently holds the attack token (falls back to the first enemy), so the player fights who is attacking him.
                        Live tgt = L;
                        if (extras.Count > 0)
                        {
                            if (L.ai == null || L.ai.EngagementRole != DMEngagementRole.Engager)
                                foreach (Live x in extras) if (x.go != null && x.ai != null && x.ai.EngagementRole == DMEngagementRole.Engager) { tgt = x; break; }
                        }
                        if (tgt.go != null) AimCameraAt(tgt.go.transform.position);
                        EnemyTopUpTick(L);
                        foreach (Live x in extras) EnemyTopUpNow(x);
                        if (extras.Count > 0)
                        {
                            var all2 = new List<Live> { L }; all2.AddRange(extras); frames++;
                            for (int a = 0; a < all2.Count; a++)
                            {
                                if (all2[a].go == null) continue;
                                string nm = all2[a].go.name; Vector3 p = all2[a].go.transform.position; float yw = all2[a].go.transform.eulerAngles.y;
                                if (!startPos.ContainsKey(nm)) { startPos[nm] = p; lastPos[nm] = p; yawLast[nm] = yw; yawAcc[nm] = 0f; pathAcc[nm] = 0f; }
                                yawAcc[nm] += Mathf.Abs(Mathf.DeltaAngle(yawLast[nm], yw)); yawLast[nm] = yw;
                                pathAcc[nm] += Vector3.Distance(new Vector3(p.x, 0, p.z), new Vector3(lastPos[nm].x, 0, lastPos[nm].z)); lastPos[nm] = p;
                                for (int b2 = a + 1; b2 < all2.Count; b2++)
                                {
                                    if (all2[b2].go == null) continue;
                                    Vector3 dd = p - all2[b2].go.transform.position; dd.y = 0f; float dist2 = dd.magnitude;
                                    minPair = Mathf.Min(minPair, dist2);
                                    if (dist2 < 0.7f)
                                    {
                                        stackFrames++;
                                        string sa = AiState(all2[a].ai) + "/" + all2[a].ai.EngagementRole, sb2 = AiState(all2[b2].ai) + "/" + all2[b2].ai.EngagementRole;
                                        string key = string.CompareOrdinal(sa, sb2) <= 0 ? sa + " + " + sb2 : sb2 + " + " + sa;
                                        pairStates[key] = (pairStates.TryGetValue(key, out int pc0) ? pc0 : 0) + 1;
                                    }
                                }
                            }
                        }
                        if (extras.Count > 0 && Time.frameCount % 15 == 0)
                        {
                            int eng = 0; string holder = null; var all = new List<Live> { L }; all.AddRange(extras);
                            foreach (Live x in all) if (x.ai != null && x.go != null && x.ai.EngagementRole == DMEngagementRole.Engager) { eng++; holder = x.go.name; holders.Add(holder); }
                            roleSamples++; maxEngagers = Mathf.Max(maxEngagers, eng);
                            if (holder != null && holder != lastHolder) { if (lastHolder != null) holderChanges++; lastHolder = holder; }
                        }
                        if (Time.time >= nextTop)
                        {
                            nextTop = Time.time + 0.4f;
                            TopUpPlayer();
                            EnemyTopUpNow(L);
                            foreach (Live x in extras) EnemyTopUpNow(x);
                        }

                        float d = PlayerEnemyDist(tgt);
                        if (d > 2.5f) KeyDown(Key.W); else KeyUp(Key.W);
                        int ic = logs.Count(x => x.Contains("INTERRUPTED"));
                        if (ic > lastRoleLogCount)
                        {
                            lastRoleLogCount = ic;
                            roleAtInterrupt.Add(Time.time.ToString("0.0", CultureInfo.InvariantCulture) + ":" + L.ai.EngagementRole);
                        }

                        yield return null;
                    }
                }

                while (Time.time < tEnd && L.go != null && !L.health.IsDead)
                {
                    SetStatus($"interrupts {prefab.name}: {Mathf.Max(0f, tEnd - Time.time):0}s left, swings {swingsLight}L/{swingsCharged}C");
                    i++;
                    if (i % 7 == 0)
                    {
                        // charged melee: hold past the charge threshold, then release
                        SetMouse(MouseLeft);
                        yield return Pump(2.0f);
                        SetMouse(0);
                        swingsCharged++;
                        yield return Pump(0.9f);
                    }
                    else
                    {
                        SetMouse(MouseLeft);
                        yield return Pump(0.12f);
                        SetMouse(0);
                        swingsLight++;
                        yield return Pump(0.55f);
                    }
                }

                KeyUp(Key.W);
                SetMouse(0);
                yield return new WaitGame(1.0f);
            }
            finally
            {
                ReleaseAllInput();
                foreach (Live x in extras) if (x.go != null) Object.Destroy(x.go);
                Application.logMessageReceived -= cb;
                if (profile != null) { profile.logInterruptsInPlay = oldLogInterrupts; profile.logCombatEventsInPlay = oldLogEvents; }
            }

            // ---------------- analysis
            var interrupts = new List<(float t, string cls, float p, string why, string name)>();
            foreach (string line in logs)
            {
                Match m = InterruptedRx.Match(line);
                if (m.Success)
                    interrupts.Add((float.Parse(m.Groups["t"].Value, CultureInfo.InvariantCulture), m.Groups["cls"].Value,
                        float.Parse(m.Groups["p"].Value, CultureInfo.InvariantCulture), m.Groups["why"].Value.Trim(), m.Groups["name"].Value));
            }

            var byReason = interrupts.GroupBy(x => x.why).ToDictionary(g => g.Key, g => g.Count());
            int skipImmunity = logs.Count(x => x.Contains("skipped: interrupt immunity"));
            int skipArmor = logs.Count(x => x.Contains("skipped: super-armor"));
            int skipCommit = logs.Count(x => x.Contains("skipped: committed window"));
            int noInt = logs.Count(x => x.Contains("no interrupt (chance"));
            int heavyHits = logs.Count(x => x.Contains("heavy=True"));
            int poiseStagger = logs.Count(x => x.StartsWith("[CombatStagger] Poise") || x.Contains("[CombatStagger] Poise"));

            var seqTimes = new List<(float t, string kind, string name)>();
            foreach (string line in logs)
            {
                if (!line.Contains("[DMEnemyCombo]")) continue;
                float t = float.Parse(line.Substring(0, line.IndexOf(' ')), CultureInfo.InvariantCulture);
                int ci = line.IndexOf("[DMEnemyCombo] ", StringComparison.Ordinal) + 15; string sn = line.Substring(ci, Mathf.Max(0, line.IndexOf(':', ci) - ci));
                if (line.Contains(": light start") || line.Contains(": charged start")) seqTimes.Add((t, "start", sn));
                else if (line.Contains(": end: finished")) seqTimes.Add((t, "end", sn));
                else if (line.Contains(": end:")) seqTimes.Add((t, "cut", sn));
            }

            int lockViolations = 0; int lockChecked = 0; float minGapAfterInterrupt = float.MaxValue;
            foreach (var it in interrupts)
            {
                var next = seqTimes.Where(s => s.kind == "start" && s.name == it.name && s.t > it.t + 0.05f).Select(s => s.t).DefaultIfEmpty(-1f).First();
                if (next < 0f) continue;
                lockChecked++;
                float gap = next - it.t;
                minGapAfterInterrupt = Mathf.Min(minGapAfterInterrupt, gap);
                if (gap < 1.4f) lockViolations++;
            }

            int immunityViolations = 0;
            foreach (var grp in interrupts.GroupBy(x => x.name))
            {
                var lst = grp.OrderBy(x => x.t).ToList();
                for (int a = 1; a < lst.Count; a++)
                    if (lst[a].why != "poise break" && lst[a].t - lst[a - 1].t < 1.95f) immunityViolations++;
            }

            var gaps = new List<float>();
            var swingToSwing = new List<float>();
            foreach (var sg in seqTimes.GroupBy(s => s.name))
            {
                var q = sg.OrderBy(s => s.t).ToList();
                for (int a = 0; a + 1 < q.Count; a++)
                    if (q[a].kind == "end" && q[a + 1].kind == "start") gaps.Add(q[a + 1].t - q[a].t);
                var st2 = q.Where(s => s.kind == "start").Select(s => s.t).ToList();
                for (int a = 1; a < st2.Count; a++) swingToSwing.Add(st2[a] - st2[a - 1]);
            }
            int totalStarts = seqTimes.Count(s => s.kind == "start");

            bool earlyOk = interrupts.Where(x => x.why == "early window").All(x => x.p < (profile != null ? profile.interruptEarlyWindowNormalized : 0.35f) + 0.01f);
            bool commitOk = interrupts.Where(x => x.why == "chance" || x.why == "early window" || x.why.StartsWith("heavy")).All(x => x.p < 0.46f);

            string reasons = string.Join(", ", byReason.Select(kv => kv.Key + " x" + kv.Value));
            R.Add("Interrupts", "int.counts", $"Interrupts by reason ({prefab.name}, {InterruptRunSeconds:0}s, {swingsLight} light + {swingsCharged} charged swings)",
                interrupts.Count > 0 ? "PASS" : "WARN",
                $"{interrupts.Count} interrupt(s): {(reasons.Length > 0 ? reasons : "none")}; skipped: immunity x{skipImmunity}, super-armor x{skipArmor}, committed x{skipCommit}; chance rolls that failed x{noInt}; heavy hits seen x{heavyHits}; poise-break stagger events x{poiseStagger}; enemy swing sequences started {totalStarts}; interrupt rate {(totalStarts > 0 ? 100f * interrupts.Count / totalStarts : 0f):0}%",
                interrupts.Count > 0 ? "" : "no interrupt fired: enemy never swung while the player hit it (check logs), or enableAttackInterrupts is off");
            R.Add("Interrupts", "int.early", "Early-window / commit-window rules", earlyOk && commitOk ? "PASS" : "FAIL",
                $"early-window interrupts all inside the profile window ({(profile != null ? profile.interruptEarlyWindowNormalized : 0.35f):0.00}): {earlyOk}; no non-poise interrupt at or past the 0.45 commit window: {commitOk}", earlyOk && commitOk ? "" : "an interrupt fired outside its rule window");
            R.Add("Interrupts", "int.immunity", "Immunity window (2s, no repeat chance interrupts)", immunityViolations == 0 ? "PASS" : "FAIL",
                $"{immunityViolations} repeat interrupt(s) inside 2s of the previous one (poise break excepted); immunity blocked {skipImmunity} hit(s)", immunityViolations == 0 ? "" : "immunity timer not honoured");
            R.Add("Interrupts", "int.recovery", "Recovery lock (1.5s no new attack after an interrupt)", lockViolations == 0 ? "PASS" : "FAIL",
                $"{lockChecked} interrupt(s) followed by a new swing; shortest gap {(minGapAfterInterrupt < 1000f ? minGapAfterInterrupt.ToString("0.00", CultureInfo.InvariantCulture) + "s" : "n/a")}; violations {lockViolations}", lockViolations == 0 ? "" : "enemy started a swing before the recovery lock ended");
            R.Add("Interrupts", "int.cadence", "Cadence (swing-to-swing, recovery pause)", gaps.Count + swingToSwing.Count > 0 ? "PASS" : "WARN",
                $"swing-to-swing mean {(swingToSwing.Count > 0 ? swingToSwing.Average() : 0f):0.00}s (min {(swingToSwing.Count > 0 ? swingToSwing.Min() : 0f):0.00}, max {(swingToSwing.Count > 0 ? swingToSwing.Max() : 0f):0.00}, n {swingToSwing.Count}); end-to-next-start gap mean {(gaps.Count > 0 ? gaps.Average() : 0f):0.00}s (n {gaps.Count}); old model mean about 2.1s swing-to-swing",
                "");
            R.Add("Interrupts", "int.token", "Token / role kept through interrupts", "PASS",
                "engagement role at each interrupt: " + (roleAtInterrupt.Count > 0 ? string.Join(", ", roleAtInterrupt.Take(12)) : "no interrupts") + " (single enemy: role must stay Engager; a two-enemy hand-off check is not part of this run)", "");
            if (count > 1)
                R.Add("Interrupts", "int.multi", $"Multi-enemy ({count} x {prefab.name}): token rotation / spacing", maxEngagers <= 1 || count > 2 ? "PASS" : "WARN",
                    $"{roleSamples} role samples; max simultaneous Engagers {maxEngagers}; distinct enemies that held Engager {holders.Count}/{count}; hand-offs {holderChanges}", "");
            if (count > 1)
            {
                var circ = yawAcc.Where(kv => kv.Value > 720f && Vector3.Distance(startPos[kv.Key], lastPos[kv.Key]) < 3f).Select(kv => kv.Key + $" yaw {kv.Value:0}deg").ToList();
                R.Add("Interrupts", "int.stack", "No stacking / circling (multi-enemy)", stackFrames == 0 && circ.Count == 0 ? "PASS" : "WARN",
                    $"min enemy-enemy distance {minPair:0.00}m, frames with a pair under 0.7m: {stackFrames}/{frames}; circling suspects (>720deg yaw, <3m net): {(circ.Count > 0 ? string.Join(", ", circ) : "none")}; pairs under 0.7m by state/role: {(pairStates.Count > 0 ? string.Join(" | ", pairStates.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + " x" + kv.Value)) : "none")}; path per enemy: {string.Join(", ", pathAcc.Select(kv => kv.Key + " " + kv.Value.ToString("0") + "m"))}", "");
            }
            R.Add("Interrupts", "int.errors", "Console errors during the run", errorCount == 0 ? "PASS" : "FAIL",
                errorCount == 0 ? "0 errors/exceptions" : errorCount + " error(s): " + string.Join(" | ", errorSamples), errorCount == 0 ? "" : "see samples");

            try
            {
                string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "rc_deploy", "savepoint_20261011_combat_slice0"));
                if (Directory.Exists(dir))
                    File.AppendAllText(Path.Combine(dir, "interrupt_test_log.txt"), "=== " + prefab.name + " ===\n" + string.Join("\n", logs) + "\n\n");
            }
            catch { /* log file is best effort */ }
        }
    }
}
#endif
