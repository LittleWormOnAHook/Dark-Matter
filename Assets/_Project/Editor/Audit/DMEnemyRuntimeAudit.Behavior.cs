#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Project.AI;
using Project.AI.Invector;
using Project.Combat;
using Project.Data;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Project.EditorTools.Audit
{
    public static partial class DMEnemyRuntimeAudit
    {
        // =============================================================== runtime-only helpers
        private static bool SetF(object o, string n, float v)
        {
            try
            {
                for (Type t = o.GetType(); t != null; t = t.BaseType)
                {
                    FieldInfo f = t.GetField(n, BFA | BindingFlags.DeclaredOnly);
                    if (f != null) { f.SetValue(o, f.FieldType == typeof(int) ? (object)Mathf.RoundToInt(v) : f.FieldType == typeof(bool) ? (object)(v > 0.5f) : (object)v); return true; }
                    PropertyInfo p = t.GetProperty(n, BFA | BindingFlags.DeclaredOnly);
                    if (p != null && p.CanWrite) { p.SetValue(o, p.PropertyType == typeof(int) ? (object)Mathf.RoundToInt(v) : (object)v); return true; }
                }
            }
            catch { }
            return false;
        }

        private static float GetFlt(object o, string n)
        {
            object v = o != null ? Mem(o, n) : null;
            if (v is float f) return f;
            if (v is int i) return i;
            if (v is bool b) return b ? 1f : 0f;
            return float.NaN;
        }

        private static void MulStructArray(object profile, string arrayName, params KeyValuePair<string, float>[] muls)
        {
            FieldInfo af = profile.GetType().GetField(arrayName, BFA);
            if (af == null || !(af.GetValue(profile) is Array arr)) return;
            for (int i = 0; i < arr.Length; i++)
            {
                object elem = arr.GetValue(i);
                foreach (var m in muls)
                {
                    FieldInfo f = elem.GetType().GetField(m.Key, BFA);
                    if (f != null && f.FieldType == typeof(float)) f.SetValue(elem, (float)f.GetValue(elem) * m.Value);
                }

                arr.SetValue(elem, i);
            }
        }

        /// <summary>Swaps the global brain / engagement profile for an in-memory clone and restores the original on Dispose. Nothing is written to disk.</summary>
        private sealed class ProfileScope : IDisposable
        {
            private readonly List<KeyValuePair<FieldInfo, object>> _orig = new List<KeyValuePair<FieldInfo, object>>();
            private readonly List<Object> _clones = new List<Object>();
            public string Description = "";

            public ProfileScope(Action<object> brain, Action<object> eng)
            {
                Swap("DM_EnemyBrainProfile", brain);
                Swap("DM_EnemyEngagementProfile", eng);
            }

            private void Swap(string typeName, Action<object> mut)
            {
                if (mut == null) return;
                Type t = FindType(typeName);
                if (t == null) return;
                FieldInfo f = t.GetField("live", BindingFlags.Static | BindingFlags.NonPublic);
                PropertyInfo live = t.GetProperty("Live", BindingFlags.Static | BindingFlags.Public);
                if (f == null || live == null) return;
                var cur = live.GetValue(null) as Object;
                if (cur == null) return;
                Object clone = Object.Instantiate(cur);
                clone.hideFlags = HideFlags.HideAndDontSave;
                mut(clone);
                _orig.Add(new KeyValuePair<FieldInfo, object>(f, f.GetValue(null)));
                f.SetValue(null, clone);
                _clones.Add(clone);
            }

            public void Dispose()
            {
                foreach (var kv in _orig) kv.Key.SetValue(null, kv.Value);
                foreach (Object c in _clones) if (c != null) Object.DestroyImmediate(c);
                _orig.Clear(); _clones.Clear();
            }
        }

        private static string DiskStamp()
        {
            string d = Application.dataPath;
            return Md5(FolderStamp(Path.Combine(d, "_Project", "Resources", "Combat"), "*.asset")
                + FolderStamp(Path.Combine(d, "_Project", "Data", "Enemies"), "*.asset")
                + FolderStamp(Path.Combine(d, "_Project", "Prefabs", "Combat", "Enemies"), "*.prefab"));
        }

        private static GameObject FredPrefab()
        {
            List<GameObject> all = EnemyPrefabs();
            return all.FirstOrDefault(p => p.name == "Hybrid Droid") ?? all.FirstOrDefault(p => p.name.IndexOf("fred", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static Component BrainOf(GameObject go)
        {
            Type t = FindType("DMEnemyBrain");
            return t != null ? go.GetComponent(t) : null;
        }

        // =============================================================== observation
        private sealed class Obs
        {
            public float notice = -1f, chase = -1f, firstAttack = -1f, dmg, orbitNet, orbitAbs, yaw, path, minDist, avgFace, loseT = -1f;
            public int attacks, frames;
            public string states = "";
            public readonly Dictionary<string, int> actions = new Dictionary<string, int>();
            public string Actions { get { return actions.Count == 0 ? "-" : string.Join("/", actions.OrderByDescending(x => x.Value).Select(x => x.Key + ":" + x.Value)); } }
            public string Brief()
            {
                return $"notice {FmtT(notice)}, chase {FmtT(chase)}, first attack {FmtT(firstAttack)}, attacks {attacks}, dmg {dmg:0}, min dist {minDist:0.0}m, path {path:0.0}m, yaw {yaw:0}deg, orbit around player net {orbitNet:0}deg / abs {orbitAbs:0}deg, avg facing error {avgFace:0}deg" + (loseT >= 0f ? $", lost target after {loseT:0.0}s" : (loseT == -2f ? ", never lost the target within 14s" : "")) + (actions.Count > 0 ? ", brain " + Actions : "") + ", states " + states;
            }
        }

        private static IEnumerator Observe(Live L, float seconds, Obs o, Component brain)
        {
            L.trackPlayer = true;
            L.BeginPhase();
            Vector3 lastRel = Vector3.zero; bool have = false;
            float t0 = Time.time, faceSum = 0f; int faceN = 0;
            while (Time.time - t0 < seconds && L.go != null)
            {
                Vector3 rel = L.go.transform.position - _player.transform.position; rel.y = 0f;
                if (have && rel.sqrMagnitude > 0.01f && lastRel.sqrMagnitude > 0.01f)
                {
                    float d = Vector3.SignedAngle(lastRel, rel, Vector3.up);
                    o.orbitNet += d; o.orbitAbs += Mathf.Abs(d);
                }

                lastRel = rel; have = true;
                if (brain != null)
                {
                    object a = Mem(brain, "CurrentAction");
                    string k = a != null ? a.ToString() : "?";
                    o.actions[k] = o.actions.TryGetValue(k, out int c) ? c + 1 : 1;
                }

                faceSum += FacingAngle(L.go.transform, _player.transform.position); faceN++;
                yield return null;
            }

            o.notice = L.noticeT; o.chase = L.chaseT; o.firstAttack = L.firstAttackT; o.attacks = L.attackStarts; o.dmg = L.PhaseDamage;
            o.yaw = L.yawAccum; o.path = L.path; o.minDist = L.minDist < 1000f ? L.minDist : -1f; o.states = string.Join("/", L.phaseStates);
            o.frames = faceN; o.avgFace = faceN > 0 ? faceSum / faceN : 0f;
        }

        /// <summary>Spawns an enemy with the player far away, waits for spawn-ready, then seats the player `dist` m in front of it.</summary>
        private static IEnumerator SpawnFor(GameObject prefab, string tag, float dist, Action<Live> done)
        {
            Cleanup();
            Live L = SpawnMatrixEnemy(prefab, tag);
            yield return SpawnReady(L);
            PlacePlayer(Ground(L.go.transform.position + FlatForward(L.go.transform) * dist), L.go.transform.position);
            done(L);
        }

        // =============================================================== multi-enemy core (FRED x3 / x5, toggle tests)
        private sealed class MultiStats
        {
            public int maxSimul, simulFrames, distinct, changes, frames;
            public float minPair = 99f, minToPlayer = 99f, hpLost;
            public int[] attackFrames, chaseFrames; public float[] orbitAbs, orbitNet, yaw;
            public readonly List<string> circ = new List<string>();
            public readonly Dictionary<string, int> actions = new Dictionary<string, int>();
            public float OrbitAbsAvg { get { return orbitAbs == null || orbitAbs.Length == 0 ? 0f : orbitAbs.Average(); } }
            public float OrbitAbsMax { get { return orbitAbs == null || orbitAbs.Length == 0 ? 0f : orbitAbs.Max(); } }
        }

        private static IEnumerator MultiCore(GameObject prefab, int N, float seconds, MultiStats st)
        {
            TopUpPlayer();
            Vector3 center = Ground(_origin + new Vector3(0f, 0f, 22f));
            PlacePlayer(center, center + Vector3.right);
            var gos = new List<GameObject>(); var ais = new List<EnemyAiController>(); var cmb = new List<EnemyCombat>(); var brains = new List<Component>();
            st.yaw = new float[N]; st.orbitAbs = new float[N]; st.orbitNet = new float[N]; st.attackFrames = new int[N]; st.chaseFrames = new int[N];
            var lastFwd = new Vector3[N]; var startPos = new Vector3[N]; var lastRel = new Vector3[N]; var attacked = new bool[N];
            for (int i = 0; i < N; i++)
            {
                float a = i * 2f * Mathf.PI / N;
                Vector3 p = Ground(center + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * 10f);
                GameObject g = Object.Instantiate(prefab, p, Quaternion.LookRotation((center - p).normalized));
                g.name = "AUDIT_M_" + i;
                gos.Add(g); ais.Add(g.GetComponent<EnemyAiController>()); cmb.Add(g.GetComponent<EnemyCombat>()); brains.Add(BrainOf(g));
                lastFwd[i] = FlatForward(g.transform); startPos[i] = g.transform.position; lastRel[i] = p - center;
            }

            float hp0 = PlayerHpInvector();
            int lastAttacker = -1;
            float t0 = Time.time, nextTop = Time.time + 2f;
            while (Time.time - t0 < seconds)
            {
                int simul = 0, who = -1;
                for (int i = 0; i < N; i++)
                {
                    if (gos[i] == null) continue;
                    string s = AiState(ais[i]);
                    if (cmb[i] != null && cmb[i].IsAttacking) { simul++; attacked[i] = true; who = i; }
                    if (s == "Attack") st.attackFrames[i]++;
                    if (s == "Chase" || s == "Hold") st.chaseFrames[i]++;
                    Vector3 fw = FlatForward(gos[i].transform);
                    st.yaw[i] += Vector3.Angle(lastFwd[i], fw); lastFwd[i] = fw;
                    Vector3 rel = gos[i].transform.position - _player.transform.position; rel.y = 0f;
                    if (rel.sqrMagnitude > 0.01f && lastRel[i].sqrMagnitude > 0.01f)
                    {
                        float d = Vector3.SignedAngle(lastRel[i], rel, Vector3.up);
                        st.orbitAbs[i] += Mathf.Abs(d); st.orbitNet[i] += d;
                    }

                    lastRel[i] = rel;
                    st.minToPlayer = Mathf.Min(st.minToPlayer, rel.magnitude);
                    for (int j = i + 1; j < N; j++)
                        if (gos[j] != null) { Vector3 d2 = gos[i].transform.position - gos[j].transform.position; d2.y = 0f; st.minPair = Mathf.Min(st.minPair, d2.magnitude); }
                    if (brains[i] != null)
                    {
                        object act = Mem(brains[i], "CurrentAction");
                        string k = act != null ? act.ToString() : "?";
                        st.actions[k] = st.actions.TryGetValue(k, out int c) ? c + 1 : 1;
                    }
                }

                if (simul >= 1 && who >= 0 && who != lastAttacker) { if (lastAttacker >= 0) st.changes++; lastAttacker = who; }
                st.maxSimul = Mathf.Max(st.maxSimul, simul);
                if (simul > 1) st.simulFrames++;
                st.frames++;
                if (Time.time > nextTop) { TopUpPlayer(); nextTop = Time.time + 2f; }
                yield return null;
            }

            st.distinct = attacked.Count(x => x);
            for (int i = 0; i < N; i++)
            {
                if (gos[i] == null) continue;
                Vector3 net = gos[i].transform.position - startPos[i]; net.y = 0f;
                if (st.yaw[i] > 900f && net.magnitude < 4f && st.attackFrames[i] == 0) st.circ.Add($"#{i} yaw {st.yaw[i]:0}deg net {net.magnitude:0.0}m");
            }

            st.hpLost = hp0 - PlayerHpInvector();
            foreach (GameObject g in gos) if (g != null) Object.Destroy(g);
            yield return null;
        }

        private static string MultiRow(DMAuditEnemyResult R, string id, string label, MultiStats st, int N, bool expectSingleAttacker, bool expectRotation)
        {
            var causes = new List<string>();
            if (st.minPair < 0.6f) causes.Add($"enemies stacked: closest pair {st.minPair:0.00}m (no separation/avoidance between enemies)");
            if (N >= 3 && st.distinct <= 1) causes.Add("only one enemy ever attacked: the others never got the attack token (token never rotates) or never closed in");
            if (expectRotation && st.distinct >= 2 && st.changes == 0) causes.Add("two enemies attacked but the attacker never changed hands (no rotation)");
            if (expectSingleAttacker && st.frames > 0 && st.simulFrames > st.frames * 0.08f) causes.Add($"{st.simulFrames}/{st.frames} frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target)");
            if (st.OrbitAbsMax > 540f) causes.Add($"orbiting the player: max angular travel around the player {st.OrbitAbsMax:0}deg");
            if (st.circ.Count > 0) causes.Add("circling in place: " + string.Join(", ", st.circ));
            if (st.minToPlayer < 0.4f) causes.Add($"enemy overlapped the player ({st.minToPlayer:0.00}m)");
            string st2 = causes.Count == 0 ? "PASS" : "WARN";
            R.Add("Multi-enemy", id, label, st2,
                $"max simultaneous attackers {st.maxSimul} ({st.simulFrames}/{st.frames} frames with >1), distinct attackers {st.distinct}/{N}, attacker hand-offs {st.changes}, attack-state frames [{string.Join(",", st.attackFrames)}], chase/hold frames [{string.Join(",", st.chaseFrames)}], closest pair {st.minPair:0.00}m, closest to player {st.minToPlayer:0.00}m, orbit around player avg {st.OrbitAbsAvg:0}deg max {st.OrbitAbsMax:0}deg (net {string.Join("/", st.orbitNet.Select(x => x.ToString("0")))}), player hp lost {st.hpLost:0}" + (st.actions.Count > 0 ? ", brain actions " + string.Join("/", st.actions.OrderByDescending(x => x.Value).Select(x => x.Key + ":" + x.Value)) : ""),
                string.Join("; ", causes));
            return st2;
        }

        private static IEnumerator ScnMulti(GameObject prefab, DMAuditEnemyResult R)
        {
            GameObject fred = FredPrefab() ?? prefab;
            R.Add("Multi-enemy", "multi.who", "Enemy used", "INFO", fred.name + " (FRED / Hybrid Droid)", "");
            yield return ScrollZoom(-4); // zoom out for the multi-enemy view
            var s3 = new MultiStats();
            yield return MultiCore(fred, 3, 16f, s3);
            MultiRow(R, "multi", "3 FREDs at once for 16s: token rotation, spacing, no stacking/orbiting", s3, 3, true, true);
            var s5 = new MultiStats();
            yield return MultiCore(fred, 5, 18f, s5);
            MultiRow(R, "multi5", "5 FREDs at once for 18s: token rotation, spacing, no stacking/orbiting", s5, 5, true, true);
        }

        // =============================================================== brain: orbit, toggles, archetypes, what it does / does not do
        private static IEnumerator ScnBrain(GameObject prefab, DMAuditEnemyResult R)
        {
            GameObject fred = FredPrefab() ?? prefab;
            string disk0 = DiskStamp();

            // (1) a single FRED must not orbit the player
            Live L = null;
            yield return SpawnFor(fred, "brain1", 7f, l => L = l);
            Component brain = BrainOf(L.go);
            var still = new Obs();
            yield return Observe(L, 10f, still, brain);
            R.Add("Brain", "brain.orbit.still", "Single FRED, player standing still: does not orbit the player", still.orbitAbs > 150f ? "FAIL" : "PASS",
                still.Brief(), still.orbitAbs > 150f ? $"the enemy travelled {still.orbitAbs:0}deg around a stationary player (ring-slot rotation / holder anchors / kiter orbit running)" : "");
            // player circles the enemy (strafing): enemy must keep facing, not orbit on its own
            float r0 = Mathf.Clamp(PlayerEnemyDist(L), 4f, 7f);
            Vector3 c0 = L.go.transform.position;
            var strafe = new Obs();
            float faceSum = 0f; int faceN = 0, faceOk = 0; Vector3 lastRel = Vector3.zero; float eOrbit = 0f;
            Vector3 startRel = _player.transform.position - c0; startRel.y = 0f;
            float a0 = Mathf.Atan2(startRel.x, startRel.z);
            L.BeginPhase();
            yield return MovePlayer(L, t => c0 + new Vector3(Mathf.Sin(a0 + t * 0.45f), 0f, Mathf.Cos(a0 + t * 0.45f)) * r0, 9f, t =>
            {
                float fe = FacingAngle(L.go.transform, _player.transform.position); faceSum += fe; faceN++; if (fe < 40f) faceOk++;
                Vector3 rel = L.go.transform.position - _player.transform.position; rel.y = 0f;
                if (lastRel.sqrMagnitude > 0.01f && rel.sqrMagnitude > 0.01f) eOrbit += Mathf.Abs(Vector3.SignedAngle(lastRel, rel, Vector3.up));
                lastRel = rel;
            });
            float avgFace = faceN > 0 ? faceSum / faceN : 0f;
            R.Add("Brain", "brain.orbit.strafe", "Single FRED, player strafes around it: keeps facing the player, does not orbit on its own", avgFace > 60f ? "WARN" : "PASS",
                $"player circled {r0:0.0}m radius for 9s; enemy avg facing error {avgFace:0}deg, within 40deg {(faceN > 0 ? 100f * faceOk / faceN : 0f):0}% of frames, enemy angular travel around the player {eOrbit:0}deg, states {string.Join("/", L.phaseStates)}, path {L.path:0.0}m",
                avgFace > 60f ? "enemy turns too slowly to follow a strafing player (turnSpeed / engagerTurnRate / holderTurnRate)" : "");
            Cleanup();

            // (2) toggles: enableUtilityBrain, enableEngagementDirector, legacyRingSlotRotation (in-memory profile clones, restored after each)
            var res = new Dictionary<string, MultiStats>();
            string[] names = { "baseline (brain on, director on)", "enableUtilityBrain=false", "enableEngagementDirector=false", "legacyRingSlotRotation=true", "brain off + director off" };
            for (int v = 0; v < names.Length; v++)
            {
                Action<object> bm = null, em = null;
                if (v == 1 || v == 4) bm = p => SetF(p, "enableUtilityBrain", 0f);
                if (v == 2 || v == 4) em = p => SetF(p, "enableEngagementDirector", 0f);
                if (v == 3) em = p => SetF(p, "legacyRingSlotRotation", 1f);
                var ms = new MultiStats();
                using (new ProfileScope(bm, em))
                {
                    bool dirOn = StaticBool("DMEnemyEngagementDirector", "IsEnabled");
                    yield return MultiCore(fred, 3, 12f, ms);
                    res[names[v]] = ms;
                    string st = MultiRow(R, "brain.toggle." + v, "3 FREDs, " + names[v], ms, 3, v == 0 || v == 3, false);
                    R.Add("Brain", "brain.toggle." + v + ".state", "Profile toggle state while this variation ran", "INFO",
                        $"director IsEnabled read inside the scope = {dirOn}; brain actions {(ms.actions.Count == 0 ? "-" : string.Join("/", ms.actions.OrderByDescending(x => x.Value).Select(x => x.Key + ":" + x.Value)))}", "");
                }
            }

            MultiStats b0 = res[names[0]], bOff = res[names[1]], dOff = res[names[2]], leg = res[names[3]];
            int kindsOn = b0.actions.Count(x => x.Key != "None" && x.Key != "?"), kindsOff = bOff.actions.Count(x => x.Key != "None" && x.Key != "?");
            R.Add("Brain", "brain.expect.brainoff", "enableUtilityBrain=false stops the brain acting (no Press/Defend/Retreat/Hold)", kindsOff == 0 ? "PASS" : "WARN",
                $"distinct brain actions: brain on {kindsOn} ({string.Join("/", b0.actions.Select(x => x.Key + ":" + x.Value))}), brain off {kindsOff} ({string.Join("/", bOff.actions.Select(x => x.Key + ":" + x.Value))})",
                kindsOff == 0 ? "" : "the brain still selected actions with enableUtilityBrain=false (a per-type ProfileOverride, or a cached decision)");
            R.Add("Brain", "brain.expect.diroff", "enableEngagementDirector=false: no single-attacker token, enemies fall back to the old behaviour", dOff.maxSimul >= 1 ? "PASS" : "WARN",
                $"with the director on: max simultaneous attackers {b0.maxSimul} ({b0.simulFrames} frames with >1); director off: {dOff.maxSimul} ({dOff.simulFrames} frames with >1), distinct attackers {b0.distinct} vs {dOff.distinct}, hand-offs {b0.changes} vs {dOff.changes}",
                dOff.maxSimul >= 1 ? "" : "nobody attacked with the director off");
            bool legacyEffect = leg.OrbitAbsAvg > b0.OrbitAbsAvg * 1.5f + 3f || leg.OrbitAbsMax > b0.OrbitAbsMax + 10f; // a few degrees of extra orbit over 12s is already the ring rotation showing
            R.Add("Brain", "brain.expect.legacy", "legacyRingSlotRotation=true restores the old ring-slot rotation (more orbiting than the director)", legacyEffect ? "PASS" : "WARN",
                $"orbit around the player avg {b0.OrbitAbsAvg:0}deg (director) vs {leg.OrbitAbsAvg:0}deg (legacy ring), distinct attackers {b0.distinct} vs {leg.distinct}, hand-offs {b0.changes} vs {leg.changes}, max simultaneous attackers {b0.maxSimul} vs {leg.maxSimul}",
                legacyEffect ? "" : "legacyRingSlotRotation made no measurable difference in 12s (the director may still own spacing, or the toggle is read only at spawn)");

            // (3) archetype / personality survey on one FRED each (Configure at runtime; the EnemyDefinition is never edited)
            Type ta = FindType("DMEnemyArchetype"), tp = FindType("DMEnemyPersonality");
            if (ta != null && tp != null)
            {
                Array archs = Enum.GetValues(ta);
                int limit = FullMode ? archs.Length : Mathf.Min(3, archs.Length);
                object none = Enum.ToObject(tp, 0);
                var survey = new List<string>();
                for (int ai = 0; ai < limit; ai++)
                {
                    object arch = archs.GetValue(ai);
                    Live S = null;
                    yield return SpawnFor(fred, "arch" + ai, 6f, l => S = l);
                    Component bs = BrainOf(S.go);
                    if (bs == null) { R.Add("Brain", "brain.arch." + arch, $"Archetype {arch}", "WARN", "no DMEnemyBrain on the enemy", "template/creator did not attach the brain (autoAttachBrain?)"); Cleanup(); continue; }
                    TryConfigure(bs, arch, none, ta, tp);
                    var o = new Obs();
                    yield return Observe(S, 10f, o, bs);
                    survey.Add(arch + ": " + o.Actions + ", attacks " + o.attacks);
                    R.Add("Brain", "brain.arch." + arch, $"Archetype {arch}: brain actions and attack rate (10s, player at 6m)", o.actions.Count > 0 ? "INFO" : "WARN", o.Brief(), o.actions.Count > 0 ? "" : "brain produced no action");
                    Cleanup();
                }

                string[] pers = FullMode ? new[] { "Aggressive", "Cowardly", "Cautious" } : new[] { "Cowardly" };
                foreach (string pn in pers)
                {
                    if (!Enum.IsDefined(tp, pn)) continue;
                    Live S = null;
                    yield return SpawnFor(fred, "pers" + pn, 6f, l => S = l);
                    Component bs = BrainOf(S.go);
                    if (bs == null) { Cleanup(); continue; }
                    TryConfigure(bs, Enum.ToObject(ta, 0), Enum.Parse(tp, pn), ta, tp);
                    var o = new Obs();
                    yield return Observe(S, 10f, o, bs);
                    R.Add("Brain", "brain.pers." + pn, $"Personality {pn} (Duelist): brain actions and attack rate", "INFO", o.Brief(), "");
                    Cleanup();
                }
            }

            // (4) what the brain does and does not do (code facts + the observations above)
            string brainSrc = ReadSource("DMEnemyBrain.cs");
            bool weaponDriven = brainSrc.Contains("TrySwitchTo") || brainSrc.Contains("SwitchWeapon") || brainSrc.Contains("EnemyWeaponRequester");
            R.Add("Brain", "brain.facts.does", "Brain DOES", "INFO",
                "scores Press / Defend / Retreat / Hold every decisionInterval from archetype rows (press, defend, retreat, tokenBias, holderTaunt, holderFeint) x personality multipliers x health condition (Healthy/Injured/SeverelyInjured/Critical); feeds the engagement director a token bias; scales holder idle actions (sidestep / taunt / feint); issues defend requests (block vs dodge) and retreat windows; reacts to intentional melee misses (miss recovery). Observed actions per archetype are in the rows above.", "");
            R.Add("Brain", "brain.facts.doesnot", "Brain does NOT", weaponDriven ? "WARN" : "INFO",
                weaponDriven ? "DMEnemyBrain references weapon switching, which the plan says is not brain-driven yet" : "no weapon choice: DMEnemyBrain contains no weapon-switch call (weapon selection stays with the loadout bridge / combat bridge; brain-driven weapon choice and grenade throwing are deferred). It does not move the enemy itself (movement stays in EnemyAiController / the engagement director), does not pick targets, and does nothing when enableUtilityBrain is off.", "");
            R.Add("Brain", "brain.disk", "Toggle / archetype tests left nothing on disk", disk0 == DiskStamp() ? "PASS" : "FAIL",
                "Resources/Combat profiles, Data/Enemies definitions and enemy prefabs hashed before and after: " + (disk0 == DiskStamp() ? "identical" : "CHANGED"), disk0 == DiskStamp() ? "" : "a runtime override leaked into an asset");
            Cleanup();
        }

        private static bool StaticBool(string typeName, string member)
        {
            try
            {
                Type t = FindType(typeName);
                if (t == null) return false;
                PropertyInfo p = t.GetProperty(member, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (p != null && p.GetValue(null) is bool b1) return b1;
                FieldInfo f = t.GetField(member, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null && f.GetValue(null) is bool b2) return b2;
            }
            catch { }
            return false;
        }

        /// <summary>Calls DMEnemyBrain.Configure at runtime with (archetype, personality...) whatever its exact signature is.</summary>
        private static void TryConfigure(Component brain, object archetype, object personality, Type archType, Type persType)
        {
            try
            {
                foreach (MethodInfo m in brain.GetType().GetMethods(BFA).Where(x => x.Name == "Configure"))
                {
                    ParameterInfo[] ps = m.GetParameters();
                    var args = new object[ps.Length];
                    bool firstPers = true;
                    for (int i = 0; i < ps.Length; i++)
                    {
                        Type pt = ps[i].ParameterType;
                        if (pt == archType) args[i] = archetype;
                        else if (pt == persType) { args[i] = firstPers ? personality : Enum.ToObject(persType, 0); firstPers = false; }
                        else args[i] = pt.IsValueType ? Activator.CreateInstance(pt) : null;
                    }

                    m.Invoke(brain, args);
                    return;
                }
            }
            catch (Exception e) { Debug.LogWarning("DMAudit: brain Configure failed: " + e.Message); }
        }

        private static string ReadSource(string fileName)
        {
            try
            {
                string root = Path.Combine(Application.dataPath, "_Project");
                string f = Directory.GetFiles(root, Path.GetFileName(fileName), SearchOption.AllDirectories).FirstOrDefault();
                return f != null ? File.ReadAllText(f) : "";
            }
            catch { return ""; }
        }

        // =============================================================== behaviour tweaks: over- then under-exaggerate (runtime only)
        private sealed class TweakSpec
        {
            public string group, kind = "inst", comp = "", note = "";
            public string[] fields = new string[0];
            public float hi = 2f, lo = 0.5f, cap = float.MaxValue;
            public bool lose, multi;
            public bool quick;
        }

        private static List<TweakSpec> TweakSpecs()
        {
            return new List<TweakSpec>
            {
                new TweakSpec { group = "Aggression / attack rate", comp = "EnemyCombat", fields = new[] { "attackCooldown", "meleeRecoveryPauseMin", "meleeRecoveryPauseMax" }, hi = 0.25f, lo = 3f, note = "hi = attacks much faster", quick = true },
                new TweakSpec { group = "Standoff distance", comp = "EnemyAiController", fields = new[] { "attackStandoffFraction", "minCombatSeparation" } /* combatRingSlotSpread is a legacy ring field the director ignores; it clamped to 1.1 in both directions, so it is not tweaked */, hi = 1.6f, lo = 0.4f, cap = 1.1f, note = "hi = stands further off", quick = true },
                new TweakSpec { group = "Chase / lose-target timings", comp = "EnemyAiController", fields = new[] { "loseTargetDelay", "maxChaseDuration", "chaseSpeedMultiplier" }, hi = 4f, lo = 0.2f, lose = true, note = "hi = never gives up, lo = gives up at once", quick = true },
                new TweakSpec { group = "Move speeds", comp = "EnemyAiController", fields = new[] { "walkSpeed", "runSpeed" }, hi = 2.2f, lo = 0.35f },
                new TweakSpec { group = "Senses ranges", comp = "EnemySenses", fields = new[] { "visionRange", "hearingRange", "visionFov" }, hi = 2f, lo = 0.3f, cap = 340f },
                new TweakSpec { group = "Reaction chance / stagger", comp = "EnemyInvectorRagdollBridge", fields = new[] { "baseReactionChance", "defaultStaggerSeconds", "knockdownImpulse" }, hi = 4f, lo = 0f, cap = 1f },
                new TweakSpec { group = "Brain archetype weights", kind = "brain", hi = 3f, lo = 0.2f, note = "hi = press x3 / defend x0.3 / retreat 0, lo = press x0.2 / defend x3 / retreat x3", quick = true },
                new TweakSpec { group = "Brain decision rate / randomness", kind = "brain2", hi = 1f, lo = 0f, note = "hi = decision every 0.15s and fully random, lo = every 3s and deterministic" },
                new TweakSpec { group = "Engagement tuning (3 FREDs)", kind = "eng", multi = true, hi = 3f, lo = 0.2f, note = "token hold, ring radius, holder spacing" }
            };
        }

        private static string Flag(Obs n, Obs v, string group, float attackRange)
        {
            var f = new List<string>();
            bool senses = group.StartsWith("Senses");
            if (n.notice >= 0f && v.notice < 0f && !senses) f.Add("never noticed the player (degenerate: no reaction)");
            if (v.notice >= 0f && v.attacks == 0 && n.attacks > 0 && !group.StartsWith("Senses")) f.Add("noticed but never attacked (no attack)");
            if (v.attacks > Mathf.Max(8, n.attacks * 2.6f)) f.Add($"attack spam ({v.attacks} attacks vs {n.attacks} normal)");
            if (v.orbitAbs > 540f || (v.yaw > 900f && v.path < 3f)) f.Add($"circling (orbit {v.orbitAbs:0}deg, yaw {v.yaw:0}deg, path {v.path:0.0}m)");
            if (v.notice >= 0f && v.path < 0.8f && v.minDist > attackRange * 1.6f && v.attacks == 0) f.Add($"freezing (path {v.path:0.0}m, never closer than {v.minDist:0.0}m)");
            return string.Join("; ", f);
        }

        private static IEnumerator ScnTweaks(GameObject prefab, DMAuditEnemyResult R)
        {
            string disk0 = DiskStamp();
            _enemyTopUp = true;
            R.Add("Behavior", "tweak.about", "How the tweaks are applied", "INFO",
                "runtime only: values are written to the live component instances of a freshly spawned enemy (destroyed after each variation) and profile tweaks go through in-memory clones swapped in for the global Resources profile and restored afterwards; a disk hash of the profiles, definitions and enemy prefabs is compared at the end. Hit-mark / effect budgets are not covered here (they are visual budgets, see the hit-mark rows).", "");
            var normal = new Obs();
            Live L = null;
            yield return SpawnFor(prefab, "tw_normal", 9f, l => L = l);
            Component nb = BrainOf(L.go);
            yield return Observe(L, 8f, normal, nb);
            float range = L.combat != null ? GetFlt(L.combat, "attackRange") : float.NaN; if (float.IsNaN(range) || range <= 0.1f) range = 2f;
            R.Add("Behavior", "tweak.normal", "Normal behaviour (reference): player 9m in front, 8s", "INFO", normal.Brief(), "");
            Cleanup();

            foreach (TweakSpec spec in TweakSpecs())
            {
                if (!FullMode && !spec.quick) continue;
                var degenerate = new List<string>();
                foreach (bool high in new[] { true, false })
                {
                    string tag = high ? "high" : "low";
                    var o = new Obs();
                    string changes = "";
                    ProfileScope scope = null;
                    MultiStats ms = null;
                    Live T = null;
                    if (spec.kind == "inst")
                    {
                        yield return SpawnFor(prefab, "tw", 9f, l => T = l);
                        Component c = T.go.GetComponent(FindType(spec.comp)) ?? T.go.GetComponentInChildren(FindType(spec.comp));
                        if (c == null) { R.Add("Behavior", "tweak." + spec.group + "." + tag, spec.group + " " + tag, "WARN", "component " + spec.comp + " not found on the enemy", ""); Cleanup(); continue; }
                        var parts = new List<string>();
                        foreach (string fn in spec.fields)
                        {
                            float cur = GetFlt(c, fn);
                            if (float.IsNaN(cur)) { parts.Add(fn + " (field not found)"); continue; }
                            float nv = cur * (high ? spec.hi : spec.lo);
                            if (nv > spec.cap) nv = spec.cap;
                            SetF(c, fn, nv);
                            parts.Add($"{spec.comp}.{fn} {cur:0.###} -> {nv:0.###}");
                        }

                        changes = string.Join(", ", parts);
                        yield return Observe(T, 8f, o, BrainOf(T.go));
                        if (spec.lose)
                        {
                            // lose-target timing: teleport the player far away and time how long until the enemy leaves combat states
                            Vector3 away = Ground(T.go.transform.position + new Vector3(-70f, 0f, 0f));
                            PlacePlayer(away, T.go.transform.position);
                            float tl = Time.time; o.loseT = -2f;
                            T.BeginPhase();
                            while (Time.time - tl < 14f && T.go != null) { string s = AiState(T.ai); if (IsCalm(s) && Time.time - tl > 0.5f) { o.loseT = Time.time - tl; break; } yield return null; }
                        }
                    }
                    else if (spec.kind == "brain" || spec.kind == "brain2")
                    {
                        Action<object> bm = p =>
                        {
                            if (spec.kind == "brain")
                                MulStructArray(p, "archetypes", new KeyValuePair<string, float>("press", high ? 3f : 0.2f), new KeyValuePair<string, float>("defend", high ? 0.3f : 3f), new KeyValuePair<string, float>("retreat", high ? 0f : 3f));
                            else { SetF(p, "decisionInterval", high ? 0.15f : 3f); SetF(p, "scoreRandomness", high ? 1f : 0f); }
                        };
                        changes = spec.kind == "brain" ? (high ? "archetype rows: press x3, defend x0.3, retreat x0" : "archetype rows: press x0.2, defend x3, retreat x3") : (high ? "decisionInterval 0.15, scoreRandomness 1" : "decisionInterval 3, scoreRandomness 0");
                        scope = new ProfileScope(bm, null);
                        yield return SpawnFor(prefab, "tw", 7f, l => T = l);
                        yield return Observe(T, 8f, o, BrainOf(T.go));
                    }
                    else // engagement: 3 FREDs under a cloned engagement profile
                    {
                        Action<object> em = p =>
                        {
                            SetF(p, "tokenMinHoldSeconds", high ? 7f : 0f); SetF(p, "holdRingRadius", high ? 7.5f : 2.2f);
                            SetF(p, "holdRingRadiusMin", high ? 6f : 1.8f); SetF(p, "holdRingRadiusMax", high ? 9f : 3.2f); SetF(p, "holderMinSpacing", high ? 3.5f : 0.6f);
                        };
                        changes = high ? "tokenMinHoldSeconds 7, holdRingRadius 7.5 (6..9), holderMinSpacing 3.5" : "tokenMinHoldSeconds 0, holdRingRadius 2.2 (1.8..3.2), holderMinSpacing 0.6";
                        scope = new ProfileScope(null, em);
                        ms = new MultiStats();
                        GameObject fred = FredPrefab() ?? prefab;
                        yield return MultiCore(fred, 3, 10f, ms);
                        o.attacks = ms.attackFrames.Sum() > 0 ? ms.distinct : 0; o.notice = 0f; o.orbitAbs = ms.OrbitAbsMax; o.minDist = ms.minToPlayer; o.path = 10f; o.dmg = ms.hpLost;
                    }

                    scope?.Dispose();
                    string flag = spec.kind == "eng"
                        ? string.Join("; ", new[] { ms.minPair < 0.6f ? $"stacking ({ms.minPair:0.00}m)" : "", ms.distinct <= 1 ? "only one enemy ever attacked" : "", ms.OrbitAbsMax > 540f ? $"orbiting ({ms.OrbitAbsMax:0}deg)" : "", ms.circ.Count > 0 ? "circling in place" : "", ms.simulFrames > ms.frames * 0.3f ? "attack spam (many simultaneous attackers)" : "" }.Where(x => x.Length > 0))
                        : Flag(normal, o, spec.group, range);
                    string measured = spec.kind == "eng"
                        ? $"{changes}; max simultaneous {ms.maxSimul} ({ms.simulFrames}/{ms.frames}), distinct attackers {ms.distinct}/3, hand-offs {ms.changes}, closest pair {ms.minPair:0.00}m, orbit max {ms.OrbitAbsMax:0}deg, player hp lost {ms.hpLost:0}"
                        : changes + "; " + o.Brief();
                    string safe = flag.Length == 0 ? (high ? "high value is safe" : "low value is safe") : "";
                    if (flag.Length > 0) degenerate.Add(tag + ": " + flag);
                    R.Add("Behavior", "tweak." + spec.group + "." + tag, $"{spec.group}: {tag} ({spec.note})", flag.Length == 0 ? "PASS" : "WARN", measured,
                        flag.Length == 0 ? "" : "degenerate: " + flag);
                    Cleanup();
                    yield return null;
                }

                R.Add("Behavior", "tweak." + spec.group + ".range", $"{spec.group}: suggested safe range", degenerate.Count == 0 ? "PASS" : "WARN",
                    degenerate.Count == 0 ? $"both x{spec.hi:0.##} and x{spec.lo:0.##} of the normal values stayed healthy: safe range at least x{spec.lo:0.##} .. x{spec.hi:0.##}" : "degenerate: " + string.Join(" | ", degenerate),
                    degenerate.Count == 0 ? "" : "keep this group between the normal value and about halfway to the degenerate end (high: x" + (1f + (spec.hi - 1f) * 0.5f).ToString("0.##") + ", low: x" + (1f - (1f - spec.lo) * 0.5f).ToString("0.##") + " of normal) until a finer sweep is run");
            }

            string disk1 = DiskStamp();
            R.Add("Behavior", "tweak.disk", "Tweaks left nothing on disk (profiles, definitions, enemy prefabs hashed before/after)", disk0 == disk1 ? "PASS" : "FAIL",
                disk0 == disk1 ? "identical" : "CHANGED", disk0 == disk1 ? "" : "a runtime override leaked into an asset: check ProfileScope / serialized writes");
            Cleanup();
        }

        // =============================================================== intentional misses are not defects
        // EnemyCombat rolls an intentional melee miss (profile enemyIntentionalMeleeMissChance, ~10%) when the target is in range, then a miss-recovery
        // step; the whiff check range is x0.88. Blocks / parries / dodges / out-of-range swings are separate causes. A damage test is therefore
        // judged over many swings: FAIL only on zero damage across many swings, or a miss rate far above the configured chance.
        private static float MeleeMissChance() { return Mathf.Clamp01(CombatProfileValue("enemyIntentionalMeleeMissChance", 0.10f)); }

        private static IEnumerator WaitSwingsOrDamage(Live L, bool melee, float baseTimeout, float extraTimeout)
        {
            yield return new WaitUntil(() => L.PhaseDamage > 0f, baseTimeout);
            if (L.PhaseDamage > 0f) yield break;
            int n0 = melee ? L.attackStarts : L.projectileSpawns;
            int enough = melee ? 8 : 12;
            if (n0 > 0 && n0 < enough) // swung/shot but nothing landed yet: give it up to 8 swings / 12 shots before judging
                yield return new WaitUntil(() => L.PhaseDamage > 0f || (melee ? L.attackStarts : L.projectileSpawns) >= enough, extraTimeout);
        }

        /// <summary>PASS/WARN/FAIL for a damage test that may legitimately miss. `note` is prefixed to the cause.</summary>
        private static string MissAwareStatus(Live L, bool melee, bool dealt, string passStatus, out string note)
        {
            note = "";
            if (dealt) return passStatus;
            if (melee && L.attackStarts > 0 && L.attackStarts - L.missAttacks <= 0)
            {
                float p = MeleeMissChance();
                double pn = Math.Pow(p, L.attackStarts);
                if (pn >= 0.01) { note = $"all {L.attackStarts} swing(s) were intentional misses (configured chance {p:P0} per swing, probability of that {pn:P1}): inconclusive, not a defect | "; return "WARN"; }
                note = $"all {L.attackStarts} swings were intentional misses although the configured chance is only {p:P0} (probability {pn:P3}): miss rate far above the configured value | ";
                return "FAIL";
            }

            int n = melee ? L.attackStarts : L.projectileSpawns;
            int judge = melee ? 6 : 10;
            if (n > 0 && n < judge) { note = $"only {n} {(melee ? "swing(s)" : "shot(s)")} before the window ended, none landed: too few to judge (misses are legitimate) | "; return "WARN"; }
            return "FAIL";
        }

        private static string ItemSpread(Live L)
        {
            ItemData it = L.bridge != null ? L.bridge.ActiveItem : null;
            return it != null ? $"{it.name}: projectileSpread {it.projectileSpreadDegrees:0.##} deg, accuracy {it.weaponAccuracy:0}, close-range scale {it.closeRangeSpreadScale:0.##} within {it.closeRangeFullAccuracyDistance:0}m" : "n/a";
        }

        private static int _burstShots, _burstHits;

        /// <summary>Aimed shots at the stationary enemy: at least `minShots`, up to `maxShots` until the first hit (spread/accuracy can legitimately miss).</summary>
        private static IEnumerator AimedBurst(Live L, ItemData weapon, Vector3 pos, float shootInterval, int minShots, int maxShots)
        {
            _burstShots = _burstHits = 0;
            int start = Loaded(weapon);
            for (int i = 0; i < maxShots; i++)
            {
                if (i >= minShots && _burstHits > 0) break;
                if (Loaded(weapon) <= 0) break;
                float hb = EHp(L);
                KeepAt(Ground(pos), L.go.transform.position);
                yield return AimedShot(L, 0.4f, 0.15f, shootInterval + 0.1f);
                SetMouse(0);
                if (EHp(L) < hb - 0.01f || L.health.IsDead) _burstHits++;
                _burstShots = start - Loaded(weapon);
            }
        }

        private static string AccuracyRow(DMAuditEnemyResult R, string idp, string key, ItemData weapon, ItemData ammo, int shots, int hits)
        {
            float eff = float.NaN;
            try { eff = weapon.GetEffectiveAccuracy(ammo); } catch { }
            float rate = shots > 0 ? (float)hits / shots : 0f;
            string st = shots <= 0 ? "INFO" : (hits == 0 ? (shots >= 6 ? "FAIL" : "WARN") : (shots >= 6 && rate < 0.34f ? "WARN" : "PASS"));
            R.Add("Ammo", idp + ".accuracy", $"{key}: hit rate over aimed shots (shots may miss by design)", st,
                $"{hits}/{shots} aimed shots hit ({rate:P0}); weapon accuracy {weapon.weaponAccuracy:0} (effective incl. skill bonus {eff:0}), ammo accuracy {ammo.weaponAccuracy:0}, spread {weapon.projectileSpreadDegrees:0.##} deg (ammo {ammo.projectileSpreadDegrees:0.##}), close-range scale {weapon.closeRangeSpreadScale:0.##} within {weapon.closeRangeFullAccuracyDistance:0}m, hip multiplier {weapon.hipFireSpreadMultiplier:0.##}; aimed at 6m, RMB held, stationary chest target",
                st == "PASS" ? "" : (st == "FAIL" ? $"zero hits across {shots} aimed shots" : (st == "WARN" && hits == 0 ? $"no hit in {shots} shot(s): too few to judge" : "hit rate well below what the accuracy settings suggest")));
            return st;
        }

        private static void AddMissRow(DMAuditEnemyResult R, string id, Live L, bool melee, string who, EnemyDefinition def)
        {
            if (melee)
            {
                float p = MeleeMissChance();
                int n = L.attackStarts, rolled = L.missAttacks;
                float obs = n > 0 ? (float)rolled / n : 0f;
                float sd = n > 0 ? Mathf.Sqrt(Mathf.Max(0.0001f, p * (1f - p)) / n) : 1f;
                bool enough = n >= 10;
                string st = n == 0 ? "INFO" : (!enough ? "INFO" : (obs <= p + 3f * sd + 0.05f ? "PASS" : "WARN"));
                R.Add("Weapons", id, $"{who}: intentional miss rate vs configured chance", st,
                    $"swings {n}, intentional misses rolled {rolled} ({obs:P0}) vs configured {p:P0} (+-{3f * sd:P0} at 3 sigma), weapon hit events {L.meleeHitEvents}, player hp lost {L.PhaseDamage:0.0}; conditions: the roll applies only while the target is in range, miss-recovery step {CombatProfileValue("enemyMeleeMissStepSeconds", 0.55f):0.##}s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach {L.minDist:0.0}m, first attack at {L.firstAttackDist:0.0}m",
                    st == "WARN" ? $"intentional misses {obs:P0} are well above the configured {p:P0}: check ShouldRollIntentionalMeleeMiss / profile value" : (n < 10 ? $"only {n} swing(s) in the window: too few to judge the rate (need ~10)" : ""));
            }
            else
            {
                int shots = L.projectileSpawns, hits = L.dmgEvents;
                float miss = def != null ? Mathf.Clamp01(def.missRate) : 0.25f;
                float expect = 1f - miss;                       // the enemy rolls a miss first; a hit still carries natural spread
                float rate = shots > 0 ? (float)hits / shots : 0f;
                float sd = shots > 0 ? Mathf.Sqrt(Mathf.Max(0.0001f, expect * (1f - expect)) / shots) : 1f;
                string st = shots < 10 ? "INFO" : (hits == 0 ? "FAIL" : (rate >= expect * 0.5f - 3f * sd ? "PASS" : "WARN"));
                R.Add("Weapons", id, $"{who}: hit rate vs configured accuracy", st,
                    $"shots/projectiles {shots}, damage events {hits} ({rate:P0}) vs about {expect:P0} expected from definition missRate {miss:0.##} (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma {3f * sd:P0}); item spread {(ItemSpread(L))}; the test player never blocks/dodges and stands still; closest approach {L.minDist:0.0}m",
                    st == "FAIL" ? $"zero hits across {shots} shots" : (st == "WARN" ? "hit rate well below the configured accuracy" : (shots < 10 ? $"only {shots} shot(s) in the window: too few to judge the rate (need ~10)" : "")));
            }
        }

        // =============================================================== player hitbox rig: enemy ranged shot -> body bone + mark
        private static IEnumerator ScnPlayerRig(GameObject prefab, DMAuditEnemyResult R)
        {
            DMPlayerHitboxRig rig = _player.GetComponent<DMPlayerHitboxRig>();
            if (rig == null || !rig.IsActive)
                yield return new WaitUntil(() => { rig = _player.GetComponent<DMPlayerHitboxRig>(); return rig != null && rig.IsActive; }, 5f);
            int hbLayer = LayerMask.NameToLayer("DMHitbox");
            bool active = rig != null && rig.IsActive && DMPlayerHitboxRig.ActiveRigCount == 1;
            int wrongLayer = 0, total = 0;
            if (rig != null)
                foreach (var hb in rig.Hitboxes) { if (hb == null) continue; total++; if (hb.gameObject.layer != hbLayer) wrongLayer++; }
            R.Add("Player rig", "rig.active", "Player hitbox rig is built and active (per-bone hitboxes on layer DMHitbox)",
                active && total >= 8 && wrongLayer == 0 ? "PASS" : "FAIL",
                $"rig {(rig != null ? "present" : "MISSING")}, active {(rig != null && rig.IsActive)}, active rigs {DMPlayerHitboxRig.ActiveRigCount}, hitboxes {total}, not on the DMHitbox layer {wrongLayer}, body type {(rig != null ? rig.BodyType.ToString() : "n/a")}",
                rig == null ? "DMPlayerHitboxBootstrap did not add the rig to the live player (RuntimeBootstrapEnabled off, or no humanoid Animator)" : (active && total >= 8 && wrongLayer == 0 ? "" : "rig exists but is inactive / incomplete (player dead, no humanoid bones mapped, or hitboxes on the wrong layer)"));
            if (rig == null || !rig.IsActive) yield break;

            ItemData ranged = AllWeaponItems().FirstOrDefault(w => w.IsRangedWeapon && !w.isMiningTool && w.rangedDamage > 0f && w.name == "survival_rifle")
                              ?? AllWeaponItems().FirstOrDefault(w => w.IsRangedWeapon && !w.isMiningTool && w.rangedDamage > 0f);
            if (ranged == null) { R.Add("Player rig", "rig.shot", "Enemy ranged shot resolves to a player body hitbox", "WARN", "no ranged weapon item", ""); yield break; }
            Cleanup();
            Live L = SpawnMatrixEnemy(prefab, "rig");
            yield return SpawnReady(L);
            if (L.bridge == null) { R.Add("Player rig", "rig.shot", "Enemy ranged shot resolves to a player body hitbox", "WARN", "no loadout bridge", ""); yield break; }
            L.bridge.ConfigureSpawnLoadout(null, ranged, false, true);
            L.bridge.TrySwitchTo(ranged, EnemyWeaponRequester.Brain, 60f);
            yield return WaitMin(0.4f, () => L.bridge.ActiveItem == ranged, 1.5f);
            var cb = L.go.GetComponent<EnemyInvectorCombatBridge>();
            float rd = Mathf.Clamp((cb != null ? cb.RangedEngageRange : 12f) * 0.6f, 5f, 11f);
            PlacePlayer(Ground(L.go.transform.position + FlatForward(L.go.transform) * rd), L.go.transform.position);
            TopUpPlayer();
            DMEnemyHitMarks pm = rig.Marks;
            if (pm != null) pm.ClearAll(); // the player keeps marks from earlier shots: start from a clean body so the stamp count is this test's
            int m0 = pm != null ? pm.StampCount : -1;
            L.trackPlayer = true;
            L.BeginPhase();
            float tEnd = Time.time + 18f;
            while (Time.time < tEnd && !(L.PhaseDamage > 0f && pm != null && pm.StampCount > m0))
            {
                if (Time.time > tEnd - 1f) break;
                yield return null;
            }

            yield return new WaitGame(0.3f);
            int m1 = pm != null ? pm.StampCount : -1;
            Animator ha = _player.GetComponentsInChildren<Animator>(true).FirstOrDefault(a => a != null && a.isHuman);
            var bones = new HashSet<Transform>();
            if (ha != null)
                foreach (HumanBodyBones hb in Enum.GetValues(typeof(HumanBodyBones)))
                {
                    if (hb == HumanBodyBones.LastBone) continue;
                    Transform bt = ha.GetBoneTransform(hb);
                    if (bt != null) bones.Add(bt);
                }

            var parents = new List<string>(); int onBone = 0, onRoot = 0, nMarks = 0;
            foreach (Transform t in _player.GetComponentsInChildren<Transform>(true))
            {
                if (t == null || !t.name.StartsWith("DM_HitMark_", StringComparison.Ordinal) || !t.gameObject.activeInHierarchy) continue;
                nMarks++;
                Transform p = t.parent;
                bool bone = false;
                for (Transform q = p; q != null && q != _player.transform; q = q.parent)
                    if (bones.Contains(q) || q.name.StartsWith(DMPlayerHitboxRig.HitboxObjectPrefix, StringComparison.Ordinal)) { bone = true; break; }
                if (bone) onBone++;
                if (p == _player.transform || (p != null && p.GetComponent<CapsuleCollider>() != null && !p.name.StartsWith(DMPlayerHitboxRig.HitboxObjectPrefix, StringComparison.Ordinal))) onRoot++;
                if (parents.Count < 6) parents.Add(p != null ? p.name : "none");
            }

            bool hit = L.PhaseDamage > 0f, stamped = m1 > m0 && onBone > 0 && onRoot == 0;
            R.Add("Player rig", "rig.shot", $"Enemy ranged shot ('{ranged.name}') resolves to a player body hitbox",
                !hit ? "FAIL" : (stamped ? "PASS" : "FAIL"),
                $"player hp lost {L.PhaseDamage:0.0}, shots/projectiles {L.projectileSpawns}, rig marks {m0} -> {m1}, active mark objects {nMarks}: on a bone {onBone}, on the root/capsule {onRoot}, parents [{string.Join(", ", parents)}], range {rd:0}m, states {string.Join("/", L.phaseStates)}",
                !hit ? "enemy ranged shot did no damage to the player: the shot missed or the hitscan still passes through (the rig supersedes the capsule but nothing resolves on the bone hitboxes)"
                     : (stamped ? "" : (m1 <= m0 ? "damage landed but the player rig stamped no mark (CombatHitResolver did not call DMPlayerHitboxRig.TryHandleRangedHit, or the profile has no humanoid mark entry)" : "marks exist but are not parented to a body bone (capsule/root fallback)")));
            // size / placement of the marks that were stamped on the player (small, shallow, on the hit point)
            float maxSize = 0f, maxDepth = 0f; int projectors = 0;
            foreach (Transform t in _player.GetComponentsInChildren<Transform>(true))
            {
                if (t == null || !t.name.StartsWith("DM_HitMark_", StringComparison.Ordinal) || !t.gameObject.activeInHierarchy) continue;
                foreach (Component comp in t.GetComponentsInChildren<Component>(false))
                {
                    if (comp == null || comp.GetType().Name != "DecalProjector") continue;
                    object sz = Mem(comp, "size");
                    if (sz is Vector3 v) { projectors++; maxSize = Mathf.Max(maxSize, Mathf.Max(v.x, v.y)); maxDepth = Mathf.Max(maxDepth, v.z); }
                }
            }

            if (pm != null && projectors > 0)
            {
                bool smallOk = maxSize <= 0.16f && maxDepth <= 0.3f;
                R.Add("Player rig", "rig.marksize", "Player hit marks are small and shallow (not head-sized, not projected through the body)", smallOk ? "PASS" : "FAIL",
                    $"{projectors} projector(s): largest footprint {maxSize:0.000}m, deepest projection {maxDepth:0.000}m (profile playerMarkSizeScale {pm.Profile.playerMarkSizeScale:0.##}, playerMarkProjectionDepth {pm.Profile.playerMarkProjectionDepth:0.###}); a 0.2m enemy mark would cover most of a head",
                    smallOk ? "" : "player marks are larger or deeper than a small bullet decal: check DM_EnemyHitMarkProfile playerMarkSizeScale / playerMarkProjectionDepth");
            }

            // lifetime: kill the shooter, then wait out the player mark lifetime and verify the marks are gone
            Cleanup();
            float life = pm != null ? pm.Profile.playerMarkLifetimeSeconds : 0f;
            if (pm != null && nMarks > 0 && life > 0f)
            {
                yield return new WaitGame(life + 1.0f);
                int left = 0;
                foreach (Transform t in _player.GetComponentsInChildren<Transform>(true))
                    if (t != null && t.name.StartsWith("DM_HitMark_", StringComparison.Ordinal) && t.gameObject.activeInHierarchy) left++;
                R.Add("Player rig", "rig.markexpire", $"Player hit marks are removed automatically after ~{life:0}s (fade, then clear)", left == 0 ? "PASS" : "FAIL",
                    $"{nMarks} mark object(s) active right after the shots, {left} still active {life + 1f:0.0}s later (playerMarkLifetimeSeconds {life:0.#}, fade {pm.Profile.playerMarkFadeSeconds:0.#}s)",
                    left == 0 ? "" : "player marks did not expire: DMEnemyHitMarks.Update player lifetime path not running or the lifetime is 0");
            }
            else R.Add("Player rig", "rig.markexpire", "Player hit marks are removed automatically after the configured lifetime", life <= 0f ? "INFO" : "WARN", $"no marks to expire (marks {nMarks}), lifetime {life:0.#}s", life <= 0f ? "playerMarkLifetimeSeconds is 0 (marks persist until death/revive)" : "no player mark was stamped, so expiry could not be checked");
            if (pm != null) pm.ClearAll(); // never leave audit marks on the player
        }

        // =============================================================== regression of the fixes made in earlier audit passes
        private static void BuildFixRegression()
        {
            if (_run == null || _run.scenarios.Any(s => s.name.StartsWith("Regression of earlier fixes"))) return;
            var R = new DMAuditEnemyResult { name = "Regression of earlier fixes and tweaks", prefabPath = "", definition = "", note = "scenario" };
            string melee = "";
            try { melee = File.ReadAllText(Directory.GetFiles(Path.Combine(Application.dataPath, "Invector-3rdPersonController"), "vMeleeManager.cs", SearchOption.AllDirectories).First()); } catch { }

            Func<string, string, IEnumerable<DMAuditCheck>> find = (scope, contains) =>
                _run.Scoped().Where(kv => scope == null || kv.Key == scope).SelectMany(kv => kv.Value.checks).Where(c => c.label != null && c.label.IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0);
            Func<IEnumerable<DMAuditCheck>, string> verdict = rows =>
            {
                var l = rows.ToList();
                if (l.Count == 0) return "WARN";
                return l.Any(c => c.status == "FAIL") ? "FAIL" : (l.Any(c => c.status == "WARN") ? "WARN" : "PASS");
            };
            Func<IEnumerable<DMAuditCheck>, string> summ = rows => { var l = rows.ToList(); return $"{l.Count(c => c.status == "PASS")} PASS / {l.Count(c => c.status == "FAIL")} FAIL / {l.Count(c => c.status == "WARN")} warn of {l.Count} runtime row(s)"; };

            // 1 swing swish (vMeleeManager.DMPlaySwingSound)
            bool hasSwish = melee.Contains("DMPlaySwingSound(type)");
            var swish = find(null, "swing plays a swish").ToList();
            R.Add("Fix check", "fix.swish", "vMeleeManager swing swish (PlayWeaponSwing on attack-window open)", !hasSwish ? "FAIL" : verdict(swish), $"code present: {hasSwish}; {summ(swish)}", hasSwish ? (swish.Any(c => c.status == "FAIL") ? "swing rows still silent for: " + string.Join(", ", swish.Where(c => c.status == "FAIL").Select(c => c.label).Take(4)) : "") : "the DMPlaySwingSound call is missing from vMeleeManager.SetActiveAttack");
            // 2 player hit sound
            // 2026-10-19 e: player body-hit audio moved to SurvivalStats (confirmed health loss) + the bridge god-mode cue.
            bool hasHit = melee.Contains("MeleeHitAudio.PlayMeleeHit(hitInfo.hitPoint")
                || (System.IO.File.Exists("Assets/_Project/Scripts/Survival/SurvivalStats.cs") && System.IO.File.ReadAllText("Assets/_Project/Scripts/Survival/SurvivalStats.cs").Contains("PlayPlayerBodyHit()"));
            var hit = find(null, "hitting the player plays a hit sound").ToList();
            R.Add("Fix check", "fix.hitsound", "Enemy melee hit on the player plays a hit sound (SurvivalStats confirmed loss / god-mode cue -> GameAudioManager.PlayPlayerBodyHit)", !hasHit ? "FAIL" : verdict(hit), $"code present: {hasHit}; {summ(hit)}", hasHit ? (hit.Any(c => c.status == "FAIL") ? "still silent for: " + string.Join(", ", hit.Where(c => c.status == "FAIL").Select(c => c.label).Take(4)) : "") : "no PlayPlayerBodyHit call in SurvivalStats");
            // 3 Hot Cross focus sync
            var hc = find("scenario", "survives a teleport").ToList();
            R.Add("Fix check", "fix.hotcross", "Hot Cross weapon focus stays in sync with the equipped weapon (no revert to the first weapon)", hc.Count == 0 ? "WARN" : verdict(hc), summ(hc) + $"; weapon reverts recorded this run: {_equipReverts}", hc.Count == 0 ? "no Hot Cross row ran (cached or scenario skipped)" : "");
            // 4 vHitEffects null-safe
            string he = ""; try { he = File.ReadAllText(Directory.GetFiles(Path.Combine(Application.dataPath, "Invector-3rdPersonController"), "vHitEffects.cs", SearchOption.AllDirectories).First()); } catch { }
            int matrixErrors = _run.weapons.Sum(w => w.consoleErrors);
            R.Add("Fix check", "fix.hiteffects", "vHitEffects null-safe AddListener (no NullReferenceException on runtime weapons)", he.Contains("onDamageHit?.AddListener") && matrixErrors == 0 ? "PASS" : "FAIL", $"code present: {he.Contains("onDamageHit?.AddListener")}; console errors across {_run.weapons.Count} weapon-matrix case(s): {matrixErrors}", "");
            // 5 LoadoutBridge override clearing
            string lb = ""; lb = ReadSource("EnemyInvectorLoadoutBridge.cs");
            DMAuditEnemyResult ov = _run.weapons.FirstOrDefault(w => w.name.StartsWith("OVERRIDE"));
            R.Add("Fix check", "fix.loadout", "EnemyInvectorLoadoutBridge: an override equal to the definition's item does not stick", lb.Contains("melee != def.meleeWeaponItem) ? melee : null") && ov != null && ov.Count("FAIL") == 0 ? "PASS" : (ov == null ? "WARN" : "FAIL"),
                $"code present: {lb.Contains("melee != def.meleeWeaponItem) ? melee : null")}; dual-slot override case: {(ov != null ? ov.Count("PASS") + " PASS / " + ov.Count("FAIL") + " FAIL" : "not run")}", "");
            // 6 template tuning values on the enemies
            AppendTuningRows(R);
            // 7 patrol-anchor / circling fix, 8 one weapon drawn (loadout rebind)
            var unaware = _run.enemies.Select(e => e.Get("unaware")).Where(c => c != null).ToList();
            R.Add("Fix check", "fix.patrol", "No circling while unaware (patrol-anchor fix) on every enemy", unaware.Count == 0 ? "WARN" : (unaware.Any(c => c.status == "FAIL") ? "FAIL" : "PASS"), $"{unaware.Count(c => c.status == "PASS")} PASS / {unaware.Count(c => c.status == "FAIL")} FAIL of {unaware.Count} enemies", string.Join(", ", _run.enemies.Where(e => e.Status("unaware") == "FAIL").Select(e => e.name)));
            var vis = _run.enemies.Select(e => e.Get("vis")).Where(c => c != null).ToList();
            R.Add("Fix check", "fix.onedrawn", "Exactly one weapon drawn on every enemy (loadout bridge melee rebind / hide-unresolved)", vis.Count == 0 ? "WARN" : (vis.Any(c => c.status == "FAIL") ? "FAIL" : "PASS"), $"{vis.Count(c => c.status == "PASS")} PASS / {vis.Count(c => c.status == "FAIL")} FAIL of {vis.Count} enemies", string.Join(", ", _run.enemies.Where(e => e.Status("vis") == "FAIL").Select(e => e.name)));
            R.Add("Fix check", "fix.profiles", "Profile adjustments made by earlier audit passes", "INFO", "none: no brain / engagement / hit-mark / ammo profile or definition value was changed by the audit passes", "");
            _run.scenarios.Add(R);
        }

        private static void AppendTuningRows(DMAuditEnemyResult R)
        {
            try
            {
                GameObject tpl = AssetDatabase.LoadAssetAtPath<GameObject>(MatrixPrefab);
                if (tpl == null) { R.Add("Fix check", "fix.tuning", "Template tuning values on the enemies", "WARN", "template prefab missing", ""); return; }
                var fields = new[] { new[] { "EnemyAiController", "attackStandoffFraction" }, new[] { "EnemyAiController", "playerStandoffBonus" }, new[] { "EnemyAiController", "chaseStaminaPauseChance" },
                    new[] { "EnemyInvectorRagdollBridge", "defaultStaggerSeconds" }, new[] { "EnemyInvectorRagdollBridge", "baseReactionChance" }, new[] { "CombatPoise", "maxPoise" } };
                var diffs = new List<string>(); int checkedN = 0;
                foreach (GameObject pf in EnemyPrefabs())
                {
                    if (pf.name == tpl.name) continue;
                    foreach (var fc in fields)
                    {
                        Type t = FindType(fc[0]);
                        Component a = t != null ? tpl.GetComponent(t) : null, b = t != null ? pf.GetComponent(t) : null;
                        if (a == null || b == null) continue;
                        float va = GetFlt(a, fc[1]), vb = GetFlt(b, fc[1]);
                        if (float.IsNaN(va) || float.IsNaN(vb)) continue;
                        checkedN++;
                        if (Mathf.Abs(va - vb) > 0.0001f) diffs.Add($"{pf.name}.{fc[0]}.{fc[1]} {vb:0.###} (template {va:0.###})");
                    }
                }

                R.Add("Fix check", "fix.tuning", "Template tuning values (AI standoff/stamina, ragdoll stagger/reaction, CombatPoise) still match on the enemies", diffs.Count == 0 ? "PASS" : "WARN",
                    $"{checkedN} value(s) compared on {EnemyPrefabs().Count - 1} enemies" + (diffs.Count > 0 ? "; differing: " + string.Join("; ", diffs.Take(12)) : ""),
                    diffs.Count == 0 ? "" : "values that differ were either left on purpose (per-enemy tuning, definition-driven) or drifted since the sync: compare with DM_Enemy_Script_Sync_Plan.md");
            }
            catch (Exception e) { R.Add("Fix check", "fix.tuning", "Template tuning values", "WARN", "check failed: " + e.Message, ""); }
        }
    }
}
#endif
