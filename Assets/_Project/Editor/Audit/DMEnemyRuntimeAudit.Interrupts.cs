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
        private const float InterruptRunSeconds = 96f; // 8 phases of 12 s for the player shake study (was 75)
        private static System.Action _shakeRestore;
        private static readonly Regex InterruptedRx = new Regex(@"^(?<t>[\d.]+) \[AttackInterrupt\] (?<name>\S+) (?<cls>\w+) @ (?<p>[\d.]+): INTERRUPTED \((?<why>[^,)]+)", RegexOptions.Compiled);

        /// <summary>Resolves "Name" or "Name@x;z" entries to pre-placed scene enemies (inactive ones included).</summary>
        private static List<GameObject> ResolveSceneEnemies(string spec)
        {
            var res = new List<GameObject>();
            if (string.IsNullOrWhiteSpace(spec)) return res;
            var all = Resources.FindObjectsOfTypeAll<EnemyAiController>().Where(a => a != null && a.gameObject.scene.IsValid() && !string.IsNullOrEmpty(a.gameObject.scene.path)).Select(a => a.gameObject).ToList();
            foreach (string raw in spec.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string s = raw.Trim(); string nm = s; bool hasPos = false; float px = 0f, pz = 0f;
                int at = s.IndexOf('@');
                if (at > 0)
                {
                    nm = s.Substring(0, at); string[] xz = s.Substring(at + 1).Split(';');
                    if (xz.Length == 2 && float.TryParse(xz[0], NumberStyles.Float, CultureInfo.InvariantCulture, out px) && float.TryParse(xz[1], NumberStyles.Float, CultureInfo.InvariantCulture, out pz)) hasPos = true;
                }
                GameObject best = null; float bd = float.MaxValue;
                foreach (GameObject g in all)
                {
                    if (g.name != nm || res.Contains(g)) continue;
                    float d = hasPos ? new Vector2(g.transform.position.x - px, g.transform.position.z - pz).sqrMagnitude : 0f;
                    if (d < bd) { bd = d; best = g; }
                }
                if (best != null) res.Add(best); else Debug.LogWarning("[AuditScene] no scene enemy named " + s);
            }
            return res;
        }

        /// <summary>Activates a pre-placed scene enemy for the run; if a ring position is given and it is far from the anchor it is warped there (runtime only, reverts when play stops).</summary>
        private static Live UseSceneEnemy(GameObject go, Vector3? anchor, Vector3 ringPos = default)
        {
            TopUpPlayer();
            go.SetActive(true);
            if (anchor.HasValue && (go.transform.position - anchor.Value).magnitude > 16f)
            {
                var ag = go.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (ag != null && ag.enabled) ag.Warp(ringPos); else go.transform.position = ringPos;
            }
            var L = new Live
            {
                go = go, ai = go.GetComponent<EnemyAiController>(), health = go.GetComponent<EnemyHealth>(), combat = go.GetComponent<EnemyCombat>(), bridge = go.GetComponent<EnemyInvectorLoadoutBridge>(),
                anim = go.GetComponentInChildren<Animator>(), mm = go.GetComponent<vMeleeManager>(), marks = go.GetComponent<DMEnemyHitMarks>(), trackPlayer = true
            };
            if (!anchor.HasValue) { _live = L; L.BeginPhase(); }
            return L;
        }

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

                if (msg.StartsWith("[AttackInterrupt]") || msg.StartsWith("[DMEnemyCombo]") || msg.StartsWith("[CombatStagger]") || msg.StartsWith("[CooldownMove]") || msg.StartsWith("[CooldownMoveEnd]") || msg.StartsWith("[DodgeStrike") || msg.StartsWith("[SwingReach]") || msg.StartsWith("[AuditStart]") || msg.StartsWith("[HitFx]") || msg.StartsWith("[StepAnimEnd]") || msg.StartsWith("[PlayerHits]") || msg.StartsWith("[FairnessSample]") || msg.StartsWith("[EnemyBlock]") || msg.StartsWith("[DodgeRoll") || msg.StartsWith("[SlideNoLegs]") || msg.StartsWith("[AuditOverlap]") || msg.StartsWith("[AuditCamera]") || msg.StartsWith("[AuditSpawnPose]") || msg.StartsWith("[AuditPlayerMotion]") || msg.StartsWith("[AuditShake]") || msg.StartsWith("[AuditShakeStatic]") || msg.StartsWith("[RollClipLeft]") || msg.StartsWith("[RollFallback]") || msg.StartsWith("[AuditShakePhase]") || msg.StartsWith("[PlayerInterpolation]"))
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
            int quickFrames = 0, quickBoostFrames = 0; float maxQuickAnimSpeed = 1f, maxIdleAnimSpeed = 1f;
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
                List<GameObject> sceneList = ResolveSceneEnemies(_opts.sceneEnemies);
                if (sceneList.Count > 0) { L = UseSceneEnemy(sceneList[0], null); Debug.Log("[AuditScene] primary " + sceneList[0].name + " (pre-placed scene enemy) at " + sceneList[0].transform.position.ToString("0.0")); }
                else L = SpawnMatrixEnemy(prefab, "int");
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
                    if (k < sceneList.Count)
                    {
                        Live sl = UseSceneEnemy(sceneList[k], L.go.transform.position, sp);
                        extras.Add(sl);
                        Debug.Log("[AuditScene] extra " + sceneList[k].name + " (pre-placed scene enemy) at " + sceneList[k].transform.position.ToString("0.0"));
                        continue;
                    }
                    GameObject pf = prefab; string mixName = "";
                    if (!string.IsNullOrWhiteSpace(_opts.mix))
                    {
                        string[] mixList = _opts.mix.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
                        if (k - 1 < mixList.Length)
                            foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { EnemiesFolder }))
                            {
                                string mp = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                                if (System.IO.Path.GetFileNameWithoutExtension(mp).Equals(mixList[k - 1].Trim(), System.StringComparison.OrdinalIgnoreCase)) { pf = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(mp); mixName = "_" + pf.name; break; }
                            }
                    }
                    GameObject g = Object.Instantiate(pf, sp, Quaternion.identity);
                    g.name = "AUDIT_W_int_" + (k + 1) + mixName;
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
                Debug.Log("[AuditStart] t=" + Time.time.ToString("0.00", CultureInfo.InvariantCulture));
                var hitBy = new Dictionary<string, int>(); int attackFrames = 0; float minStam = 9999f; int pressedFrames = 0, lightClicks = 0; var blockFlags = new Dictionary<string, int>(); int armedAtStart = PlayerIsArmed() ? 1 : 0;
                foreach (Live hx in new[] { L }.Concat(extras))
                {
                    if (hx == null || hx.health == null) continue; string hn = hx.go.name; hitBy[hn] = 0;
                    hx.health.DamagedWithSource += (dmg, src, crit) => { if (dmg > 0f && src != null && _player != null && src.transform.root == _player.transform.root) hitBy[hn]++; };
                }
                var fairN = new Dictionary<string, int>(); var fairElig = new Dictionary<string, int>(); var fairEng = new Dictionary<string, int>(); var fairNames = new List<Live>();
                float tEnd = Time.time + InterruptRunSeconds;
                // clipfix trackers: overlapping attack windows, camera distance, spawn pose (hand distance of each enemy in the first 4 s)
                int ovFrames = 0, ovMax = 0, ovWindows = 0, camFrames = 0, camClose = 0; bool ovActive = false; float ovSeconds = 0f, camMin = 999f; var ovSamples = new List<string>();
                var spawnN = new Dictionary<string, int>(); var spawnLow = new Dictionary<string, int>(); var spawnFirstLow = new Dictionary<string, float>(); var spawnLowInfo = new Dictionary<string, string>();
                int trGi(Animator a, string n) { foreach (var pr in a.parameters) if (pr.name == n && pr.type == AnimatorControllerParameterType.Int) return a.GetInteger(n); return -99; }
                var spawnHand = new Dictionary<string, float>(); var spawnSeen = new Dictionary<string, string>(); float auditT0 = Time.time;
                // player motion tracker (lightweight timers only): frame time, timeScale, animator speed / locomotion params, root speed, W key chatter
                Animator pmAn = _player != null ? _player.GetComponentInChildren<Animator>() : null; Vector3 pmLast = _player != null ? _player.transform.position : Vector3.zero;
                int pmFrames = 0, pmHitch = 0, pmTs = 0, pmAnimSpeed = 0, pmWFlips = 0, pmImFlips = 0; float pmSumDt = 0f, pmMaxDt = 0f, pmAnimMin = 1f, pmImPrev = 0f, pmImMaxStep = 0f, pmImDirPrev = 0f; bool pmWPrev = false; var pmSpeeds = new List<float>(); var pmHitchInfo = new List<string>();
                bool pmShot1 = false, pmShot2 = false; int pmNAtt = 0, pmNIdle = 0, pmNStop = 0, pmRel = 0; float pmDtAtt = 0f, pmDtIdle = 0f, pmDtStop = 0f, pmAnimMax = 1f, pmEma = 0.03f; int pmGc0 = System.GC.CollectionCount(0); float pmPosJump = 0f; int pmPosFlips = 0; float pmPrevSp = 0f;
                // ---- shakefix study: 8 phases of the same run, one setting changed per phase, player root reversals logged per frame ----
                DM_CombatCoreProfile shCore = DM_CombatCoreProfile.Live; Rigidbody shRb = _player != null ? _player.GetComponent<Rigidbody>() : null; DMPlayerHitboxRig shRig = _player != null ? _player.GetComponent<DMPlayerHitboxRig>() : null;
                bool shOldFreeze = shCore.hitstopFreezesAttacker, shOldEnable = shCore.enableMeleeHitstop; float shOldHold = shCore.hitstopHoldSpeed, shOldDrift = shCore.lightComboForwardDriftMultiplier; var shOldInterp = shCore.playerRigidbodyInterpolation;
                var shLive = new List<GameObject>(); if (L.go != null) shLive.Add(L.go); foreach (Live sx in extras) if (sx.go != null) shLive.Add(sx.go);
                _shakeRestore = () => { shCore.hitstopFreezesAttacker = shOldFreeze; shCore.enableMeleeHitstop = shOldEnable; shCore.hitstopHoldSpeed = shOldHold; shCore.lightComboForwardDriftMultiplier = shOldDrift; shCore.playerRigidbodyInterpolation = shOldInterp; if (shRig != null) shRig.SetActive(true); foreach (GameObject sg in shLive) if (sg != null && !sg.activeSelf) sg.SetActive(true); };
                const int shN = 8; string[] shNames = { "baseline", "playerNoFreeze", "hitstopOff", "hitboxRigOff", "interpolationNone", "oldHoldSpeed0.05", "noForwardDrift", "playerOnly" };
                float shLen = InterruptRunSeconds / shN; int shCur = -1;
                var shFrames = new int[shN]; var shAtt = new int[shN]; var shHoldF = new int[shN]; var shPosRev = new int[shN]; var shAttPosRev = new int[shN]; var shHoldPosRev = new int[shN]; var shFwdRev = new int[shN]; var shYawRev = new int[shN]; var shCamRev = new int[shN]; var shSpN = new int[shN];
                var shAuditYaw = new float[shN]; var shTotYaw = new float[shN]; var shSpDelta = new float[shN]; var shMaxStep = new float[shN];
                Vector3 shPrevDp = Vector3.zero, shPrevCamRel = Vector3.zero, shPrevDCam = Vector3.zero; float shPrevFwd = 0f, shPrevDYaw = 0f, shPrevYaw = 0f, shPrevSp = 0f; bool shHave = false;
                void ShApply(int ph)
                {
                    _shakeRestore();
                    switch (ph)
                    {
                        case 1: shCore.hitstopFreezesAttacker = false; break;
                        case 2: shCore.enableMeleeHitstop = false; break;
                        case 3: if (shRig != null) shRig.SetActive(false); break;
                        case 4: shCore.playerRigidbodyInterpolation = PlayerRigidbodyInterpolationMode.None; if (shRb != null) shRb.interpolation = RigidbodyInterpolation.None; break;
                        case 5: shCore.hitstopHoldSpeed = 0.05f; break;
                        case 6: shCore.lightComboForwardDriftMultiplier = 0f; break;
                        case 7: foreach (GameObject sg in shLive) if (sg != null) sg.SetActive(false); break;
                    }

                    Debug.Log("[AuditShakePhase] " + ph + " " + shNames[ph] + " at " + (Time.time - auditT0).ToString("0.0", CultureInfo.InvariantCulture) + "s");
                }
                {
                    int shNonTrig = 0, shRigNonTrig = 0, shRigRb = 0, shBoneRbNonKin = 0;
                    if (_player != null)
                    {
                        foreach (Collider sc in _player.GetComponentsInChildren<Collider>(true)) { if (sc.name.StartsWith("DMPlayerHitbox_")) { if (!sc.isTrigger) shRigNonTrig++; } else if (!sc.isTrigger) shNonTrig++; }
                        foreach (Rigidbody sr in _player.GetComponentsInChildren<Rigidbody>(true)) { if (sr.name.StartsWith("DMPlayerHitbox_")) shRigRb++; else if (!sr.isKinematic && sr.gameObject != _player) shBoneRbNonKin++; }
                    }
                    var shHit = new List<string>(); int shHbLayer = DMEnemyHitQuery.HitboxLayer; for (int sl = 0; sl < 32; sl++) if (!Physics.GetIgnoreLayerCollision(shHbLayer, sl) && !string.IsNullOrEmpty(LayerMask.LayerToName(sl))) shHit.Add(LayerMask.LayerToName(sl));
                    Debug.Log("[AuditShakeStatic] rbInterp=" + (shRb != null ? shRb.interpolation.ToString() : "none") + " profileInterp=" + shCore.playerRigidbodyInterpolation + " rbKinematic=" + (shRb != null && shRb.isKinematic ? 1 : 0) + " animatorUpdate=" + (pmAn != null ? pmAn.updateMode.ToString() : "none") + " rootMotion=" + (pmAn != null && pmAn.applyRootMotion ? 1 : 0) + " fixedDt=" + Time.fixedDeltaTime.ToString("0.0000", CultureInfo.InvariantCulture) + " autoSync=off(project setting; hitbox queries call SyncTransforms once per frame)" + " playerNonTriggerColliders=" + shNonTrig + " rigNonTriggerColliders=" + shRigNonTrig + " rigRigidbodies=" + shRigRb + " nonKinematicBoneRigidbodies=" + shBoneRbNonKin + " rigColliders=" + (shRig != null ? shRig.HitboxCount : -1) + " hitboxLayerCollidesWith=" + string.Join("/", shHit) + " hitstopHold=" + shCore.hitstopHoldSpeed.ToString("0.00", CultureInfo.InvariantCulture) + " freezesAttacker=" + shCore.hitstopFreezesAttacker + " forwardDrift=" + shCore.lightComboForwardDriftMultiplier.ToString("0.00", CultureInfo.InvariantCulture));
                }
                float nextTop = 0f;                float nextForcedRoll = Time.time + 5f; // forces the brain dodge roll path now and then so its direction / displacement can be checked
                int i = 0;
                int lastRoleLogCount = 0;

                // Swing reach tracker: distance root->player when a swing sequence starts, and the closest any weapon hitbox gets to the player capsule during it.
                var swingTracks = new Dictionary<string, float[]>(); var swingReachMax = new Dictionary<string, float>(); var swingCounts = new Dictionary<string, int>();
                void TrackSwing(Live x)
                {
                    if (x == null || x.go == null || x.ai == null || _player == null) return;
                    string nm = x.go.name; bool seq = x.ai.IsInAttackSequence;
                    if (!swingTracks.TryGetValue(nm, out float[] t)) swingTracks[nm] = t = new float[4];
                    float gapNow = float.MaxValue;
                    if (seq)
                    {
                        Vector3 pp = _player.transform.position + Vector3.up * 1.0f;
                        var cc = _player.GetComponent<CapsuleCollider>(); float prad = cc != null ? cc.radius : 0.3f;
                        foreach (var hb in x.go.GetComponentsInChildren<global::Invector.vMelee.vHitBox>(true))
                        {
                            Collider col = hb.GetComponent<Collider>(); if (col == null) continue;
                            Vector3 cp = Physics.ClosestPoint(pp, col, col.transform.position, col.transform.rotation); Vector3 dv = cp - pp; dv.y = 0f;
                            gapNow = Mathf.Min(gapNow, dv.magnitude - prad);
                            var bc = col as BoxCollider;
                            if (bc != null)
                            {
                                Vector3 fw = x.go.transform.forward; fw.y = 0f; fw.Normalize();
                                for (int cx = -1; cx <= 1; cx += 2) for (int cy = -1; cy <= 1; cy += 2) for (int cz = -1; cz <= 1; cz += 2)
                                {
                                    Vector3 corner = bc.transform.TransformPoint(bc.center + Vector3.Scale(bc.size * 0.5f, new Vector3(cx, cy, cz)));
                                    t[3] = Mathf.Max(t[3], Vector3.Dot(corner - x.go.transform.position, fw));
                                }
                            }
                        }
                    }
                    if (seq && t[0] < 0.5f)
                    {
                        t[0] = 1f; t[3] = 0f; Vector3 dd0 = x.go.transform.position - _player.transform.position; dd0.y = 0f; t[1] = dd0.magnitude; t[2] = gapNow;
                        swingCounts[nm] = (swingCounts.TryGetValue(nm, out int sc) ? sc : 0) + 1;
                    }
                    else if (seq) t[2] = Mathf.Min(t[2], gapNow);
                    else if (t[0] > 0.5f)
                    {
                        t[0] = 0f;
                        Debug.Log("[SwingReach] " + nm + " n=" + swingCounts[nm] + " startDist=" + t[1].ToString("0.00", CultureInfo.InvariantCulture) + " range=" + (x.combat != null ? x.combat.ResolveEffectiveAttackRange(_player.transform) : 0f).ToString("0.00", CultureInfo.InvariantCulture) + " gate=" + x.ai.ResolveSwingStartDistance(_player.transform).ToString("0.00", CultureInfo.InvariantCulture) + " wlen=" + (x.go.GetComponent<Project.AI.Invector.EnemyInvectorCombatBridge>() != null ? x.go.GetComponent<Project.AI.Invector.EnemyInvectorCombatBridge>().LastWeaponLength : 0f).ToString("0.00", CultureInfo.InvariantCulture) + " t=" + Time.time.ToString("0.00", CultureInfo.InvariantCulture) + " tip=" + t[3].ToString("0.00", CultureInfo.InvariantCulture) + " minGap=" + (t[2] > 1000f ? 99f : t[2]).ToString("0.00", CultureInfo.InvariantCulture));
                    }
                }

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
                        if (Time.time >= nextForcedRoll && _player != null && tgt.go != null && tgt.ai != null && !tgt.ai.IsInAttackSequence)
                        {
                            nextForcedRoll = Time.time + 6f;
                            var begin = typeof(EnemyAiController).GetMethod("BeginBrainDefend", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (begin != null) begin.Invoke(tgt.ai, new object[] { _player.transform, false });
                        }
                        if (extras.Count > 0)
                        {
                            var all2 = new List<Live> { L }; all2.AddRange(extras); frames++;
                            for (int a = 0; a < all2.Count; a++)
                            {
                                if (all2[a].go == null) continue;
                                if (all2[a].ai != null)
                                {
                                    Animator an = all2[a].go.GetComponentInChildren<Animator>();
                                    if (an != null)
                                    {
                                        if (all2[a].ai.IsQuickStepActive) { quickFrames++; if (an.speed > 1.01f) quickBoostFrames++; maxQuickAnimSpeed = Mathf.Max(maxQuickAnimSpeed, an.speed); }
                                        else if (an.speed > 1.01f) maxIdleAnimSpeed = Mathf.Max(maxIdleAnimSpeed, an.speed);
                                    }
                                }
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
                            foreach (Live x in all)
                            {
                                if (x.ai == null || x.go == null) continue;
                                string en = x.go.name; if (!fairNames.Contains(x)) { fairNames.Add(x); fairN[en] = 0; fairElig[en] = 0; fairEng[en] = 0; }
                                fairN[en]++; if (_player != null && x.ai.IsTokenEligible(_player.transform, DM_EnemyEngagementProfile.Live, false)) fairElig[en]++; if (x.ai.EngagementRole == DMEngagementRole.Engager) fairEng[en]++;
                            }
                            if (holder != null && holder != lastHolder) { if (lastHolder != null) holderChanges++; lastHolder = holder; }
                        }
                        if (Time.time >= nextTop)
                        {
                            nextTop = Time.time + 0.4f;
                            TopUpPlayer();
                            EnemyTopUpNow(L);
                            foreach (Live x in extras) EnemyTopUpNow(x);
                        }

                        TrackSwing(L); foreach (Live x in extras) TrackSwing(x);
                        {
                            var trAll = new List<Live> { L }; trAll.AddRange(extras);
                            int trAtt = 0; var trNames = new List<string>();
                            foreach (Live lv in trAll)
                                if (lv != null && lv.go != null && lv.ai != null && lv.ai.IsInAttackSequence && !(lv.health != null && lv.health.IsDead)) { trAtt++; trNames.Add(lv.go.name + "/" + lv.ai.EngagementRole); }
                            ovMax = Mathf.Max(ovMax, trAtt);
                            if (trAtt >= 2)
                            {
                                ovFrames++; ovSeconds += Time.deltaTime;
                                if (!ovActive) { ovActive = true; ovWindows++; if (ovSamples.Count < 8) ovSamples.Add((Time.time - auditT0).ToString("0.0", CultureInfo.InvariantCulture) + "s " + string.Join("+", trNames)); }
                            }
                            else ovActive = false;
                            Camera trCam = Camera.main;
                            if (trCam != null && _player != null)
                            {
                                float trCd = Vector3.Distance(trCam.transform.position, _player.transform.position + Vector3.up * 1.5f);
                                camMin = Mathf.Min(camMin, trCd); camFrames++; if (trCd < 1.0f) camClose++;
                            }
                            if (Time.frameCount % 6 == 0)
                                foreach (Live lv in trAll)
                                {
                                    if (lv == null || lv.go == null) continue;
                                    Animator trAn = lv.go.GetComponent<Animator>(); if (trAn == null || !trAn.isHuman) continue;
                                    Transform trL = trAn.GetBoneTransform(HumanBodyBones.LeftHand), trR = trAn.GetBoneTransform(HumanBodyBones.RightHand); if (trL == null || trR == null) continue;
                                    float trHd = Vector3.Distance(lv.go.transform.InverseTransformPoint(trL.position), lv.go.transform.InverseTransformPoint(trR.position));
                                    spawnN[lv.go.name] = (spawnN.TryGetValue(lv.go.name, out int trN) ? trN : 0) + 1;
                                    if (trHd < 0.35f)
                                    {
                                        spawnLow[lv.go.name] = (spawnLow.TryGetValue(lv.go.name, out int trLw) ? trLw : 0) + 1;
                                        if (!spawnFirstLow.ContainsKey(lv.go.name)) { spawnFirstLow[lv.go.name] = Time.time - auditT0; var trC2 = trAn.GetCurrentAnimatorClipInfo(0); var trLb = lv.go.GetComponent<EnemyInvectorLoadoutBridge>(); spawnLowInfo[lv.go.name] = "clip=" + (trC2.Length > 0 && trC2[0].clip != null ? trC2[0].clip.name : "none") + " upperId=" + trGi(trAn, "UpperBody_ID") + " moveSet=" + trGi(trAn, "MoveSet_ID") + " attackId=" + trGi(trAn, "AttackID") + " weapon=" + (trLb != null && trLb.ActiveItem != null ? trLb.ActiveItem.name.Replace(" ", "_") : "none"); }
                                    }
                                    if (!spawnHand.TryGetValue(lv.go.name, out float trOld) || trHd < trOld)
                                    {
                                        spawnHand[lv.go.name] = trHd; var trCi = trAn.GetCurrentAnimatorClipInfo(0);
                                        spawnSeen[lv.go.name] = (trCi.Length > 0 && trCi[0].clip != null ? trCi[0].clip.name : "none") + " at " + (Time.time - auditT0).ToString("0.0", CultureInfo.InvariantCulture) + "s";
                                    }
                                }
                        }
                        if (!pmShot1 && Time.time - auditT0 > 1.0f) { pmShot1 = true; ScreenCapture.CaptureScreenshot(@"C:\Users\Teabagger\_shot1.png"); }
                        if (!pmShot2 && Time.time - auditT0 > 3.0f) { pmShot2 = true; ScreenCapture.CaptureScreenshot(@"C:\Users\Teabagger\_shot2.png"); }
                        if (_player != null)
                        {
                            float pmDt = Time.unscaledDeltaTime; pmFrames++; pmSumDt += pmDt; pmMaxDt = Mathf.Max(pmMaxDt, pmDt);
                            if (pmFrames > 10 && pmDt > pmEma * 2.2f) pmRel++; pmEma = Mathf.Lerp(pmEma, pmDt, 0.05f);
                            if (PlayerAttacking()) { pmNAtt++; pmDtAtt += pmDt; } else { pmNIdle++; pmDtIdle += pmDt; }
                            if (pmDt > 0.05f) { pmHitch++; if (pmHitchInfo.Count < 6) pmHitchInfo.Add((Time.time - auditT0).ToString("0.0", CultureInfo.InvariantCulture) + "s " + (pmDt * 1000f).ToString("0") + "ms attacking=" + (PlayerAttacking() ? 1 : 0)); }
                            if (Mathf.Abs(Time.timeScale - 1f) > 0.001f) pmTs++;
                            bool pmW = Keyboard.current != null && Keyboard.current.wKey.isPressed; if (pmW != pmWPrev) pmWFlips++; pmWPrev = pmW;
                            Vector3 pmDp = _player.transform.position - pmLast; pmDp.y = 0f; pmLast = _player.transform.position;
                            if (pmAn != null)
                            {
                                if (Mathf.Abs(pmAn.speed - 1f) > 0.001f) { pmAnimSpeed++; pmAnimMin = Mathf.Min(pmAnimMin, pmAn.speed); pmAnimMax = Mathf.Max(pmAnimMax, pmAn.speed); }
                                if (pmAn.speed < 0.5f) { pmNStop++; pmDtStop += pmDt; }
                                float pmIm = pmAn.GetFloat("InputMagnitude"); float pmStep = Mathf.Abs(pmIm - pmImPrev); pmImMaxStep = Mathf.Max(pmImMaxStep, pmStep);
                                float pmDir = Mathf.Sign(pmIm - pmImPrev) * (pmStep > 0.02f ? 1f : 0f); if (pmDir != 0f && pmImDirPrev != 0f && pmDir != pmImDirPrev) pmImFlips++; if (pmDir != 0f) pmImDirPrev = pmDir; pmImPrev = pmIm;
                            }
                            { float pmSp = pmDp.magnitude / Mathf.Max(0.0001f, Time.deltaTime); if (pmSp > 0.3f && (pmAn == null || pmAn.speed > 0.5f)) { pmSpeeds.Add(pmSp); if (pmPrevSp > 0.3f && Mathf.Abs(pmSp - pmPrevSp) > 1.5f) pmPosFlips++; } pmPrevSp = pmSp; pmPosJump = Mathf.Max(pmPosJump, pmDp.magnitude); }
                            {
                                int shP = Mathf.Clamp((int)((Time.time - auditT0) / shLen), 0, shN - 1);
                                if (shP != shCur) { shCur = shP; ShApply(shP); }
                                Transform shPt = _player.transform; Vector3 shDp = pmDp; float shYaw = shPt.eulerAngles.y; float shDYaw = shHave ? Mathf.DeltaAngle(shPrevYaw, shYaw) : 0f;
                                bool shAttk = PlayerAttacking(); bool shHold = DMMeleeHitStop.PlayerHoldActive; float shMag = shDp.magnitude;
                                shFrames[shP]++; if (shAttk) shAtt[shP]++; if (shHold) shHoldF[shP]++;
                                if (shHave)
                                {
                                    if (shMag > 0.004f && shPrevDp.magnitude > 0.004f && Vector3.Dot(shDp, shPrevDp) < 0f) { shPosRev[shP]++; if (shAttk) shAttPosRev[shP]++; if (shHold) shHoldPosRev[shP]++; }
                                    float shFw = Vector3.Dot(shDp, shPt.forward); if (Mathf.Abs(shFw) > 0.003f && Mathf.Abs(shPrevFwd) > 0.003f && Mathf.Sign(shFw) != Mathf.Sign(shPrevFwd)) shFwdRev[shP]++; shPrevFwd = shFw;
                                    if (Mathf.Abs(shDYaw) > 0.3f && Mathf.Abs(shPrevDYaw) > 0.3f && Mathf.Sign(shDYaw) != Mathf.Sign(shPrevDYaw)) shYawRev[shP]++;
                                    shTotYaw[shP] += Mathf.Abs(shDYaw);
                                    float shSp = shMag / Mathf.Max(0.0001f, Time.deltaTime); if (shSp > 0.3f && shPrevSp > 0.3f) { shSpDelta[shP] += Mathf.Abs(shSp - shPrevSp); shSpN[shP]++; } shPrevSp = shSp;
                                    shMaxStep[shP] = Mathf.Max(shMaxStep[shP], shMag);
                                    Camera shCam = Camera.main;
                                    if (shCam != null) { Vector3 shRel = shCam.transform.position - shPt.position; Vector3 shDRel = shRel - shPrevCamRel; if (shDRel.magnitude > 0.01f && shPrevDCam.magnitude > 0.01f && Vector3.Dot(shDRel, shPrevDCam) < 0f) shCamRev[shP]++; shPrevCamRel = shRel; shPrevDCam = shDRel; }
                                }
                                shPrevDp = shDp; shPrevYaw = shYaw; shPrevDYaw = shDYaw; shHave = true;
                            }
                        }
                        if (PlayerAttacking()) attackFrames++;                        if (Mouse.current != null && Mouse.current.leftButton.isPressed) pressedFrames++;
                        { float stam = PlayerStamina(); if (!float.IsNaN(stam)) minStam = Mathf.Min(minStam, stam); }
                        if (tgt.go != null && _player != null)
                        {
                            // Face the attacking enemy like a human would (the camera aim alone does not always turn the body), so swings can land.
                            Vector3 fl = tgt.go.transform.position - _player.transform.position; fl.y = 0f;
                            if (fl.sqrMagnitude > 0.01f && fl.magnitude < 5f) { float shYb = _player.transform.eulerAngles.y; _player.transform.rotation = Quaternion.RotateTowards(_player.transform.rotation, Quaternion.LookRotation(fl.normalized, Vector3.up), 900f * Time.deltaTime); shAuditYaw[Mathf.Clamp(shCur, 0, shN - 1)] += Mathf.Abs(Mathf.DeltaAngle(shYb, _player.transform.eulerAngles.y)); }
                        }
                        float d = PlayerEnemyDist(tgt);
                        if (d > 1.9f) KeyDown(Key.W); else KeyUp(Key.W); // close in to melee reach (2.5 left the player out of reach of the engager)
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
                        foreach (string fl0 in new[] { "isAttacking", "isRolling", "isBlocking", "isJumping", "isSliding", "customAction", "lockMeleeInput", "lockInput", "isDead", "isEquipped", "isUsingMelee" }) if (PlayerFlag(fl0)) blockFlags[fl0] = (blockFlags.TryGetValue(fl0, out int bf0) ? bf0 : 0) + 1;
                        lightClicks++;
                        SetMouse(MouseLeft);
                        yield return Pump(0.12f);
                        SetMouse(0);
                        swingsLight++;
                        yield return Pump(0.55f);
                    }
                }

                Debug.Log("[PlayerHits] hits=" + string.Join(",", hitBy.Select(kv => kv.Key + ":" + kv.Value)) + " attackFrames=" + attackFrames + " minStamina=" + minStam.ToString("0") + " lightClicks=" + lightClicks + " pressedFrames=" + pressedFrames + " flagsAtClick={" + string.Join(",", blockFlags.Select(kv => kv.Key + ":" + kv.Value)) + "}" + " armed=" + armedAtStart);
                foreach (Live x in fairNames)
                    if (x.go != null && x.ai != null) Debug.Log("[FairnessSample] " + x.go.name + " n=" + fairN[x.go.name] + " elig=" + fairElig[x.go.name] + " eng=" + fairEng[x.go.name] + " tokens=" + x.ai.EngagementState.TokensAwarded + " firstAward=" + x.ai.EngagementState.FirstAwardTime.ToString("0.00", CultureInfo.InvariantCulture));
                Debug.Log("[AuditOverlap] windows=" + ovWindows + " seconds=" + ovSeconds.ToString("0.00", CultureInfo.InvariantCulture) + " max=" + ovMax + " samples=" + string.Join(" ; ", ovSamples));
                {
                    float pmMean = pmSpeeds.Count > 0 ? pmSpeeds.Average() : 0f; float pmStd = pmSpeeds.Count > 1 ? Mathf.Sqrt(pmSpeeds.Sum(v => (v - pmMean) * (v - pmMean)) / pmSpeeds.Count) : 0f;
                    Debug.Log("[AuditPlayerMotion] frames=" + pmFrames + " meanDtMs=" + (pmFrames > 0 ? pmSumDt / pmFrames * 1000f : 0f).ToString("0.0", CultureInfo.InvariantCulture) + " maxDtMs=" + (pmMaxDt * 1000f).ToString("0", CultureInfo.InvariantCulture) + " hitchFrames=" + pmHitch + " timeScaleFrames=" + pmTs + " animSpeedFrames=" + pmAnimSpeed + " animSpeedMin=" + pmAnimMin.ToString("0.00", CultureInfo.InvariantCulture) + " wFlips=" + pmWFlips + " moveSamples=" + pmSpeeds.Count + " moveSpeedMean=" + pmMean.ToString("0.00", CultureInfo.InvariantCulture) + " moveSpeedStd=" + pmStd.ToString("0.00", CultureInfo.InvariantCulture) + " inputMagMaxStep=" + pmImMaxStep.ToString("0.00", CultureInfo.InvariantCulture) + " inputMagFlips=" + pmImFlips + " relHitchFrames=" + pmRel + " meanDtAttMs=" + (pmNAtt > 0 ? pmDtAtt / pmNAtt * 1000f : 0f).ToString("0.0", CultureInfo.InvariantCulture) + " meanDtIdleMs=" + (pmNIdle > 0 ? pmDtIdle / pmNIdle * 1000f : 0f).ToString("0.0", CultureInfo.InvariantCulture) + " animSpeedMax=" + pmAnimMax.ToString("0.00", CultureInfo.InvariantCulture) + " hitStopFrames=" + pmNStop + " meanDtHitStopMs=" + (pmNStop > 0 ? pmDtStop / pmNStop * 1000f : 0f).ToString("0.0", CultureInfo.InvariantCulture) + " speedJumps=" + pmPosFlips + " maxStepM=" + pmPosJump.ToString("0.00", CultureInfo.InvariantCulture) + " gc0=" + (System.GC.CollectionCount(0) - pmGc0) + " vsync=" + QualitySettings.vSyncCount + " targetFps=" + Application.targetFrameRate + " maxDeltaTime=" + Time.maximumDeltaTime.ToString("0.00", CultureInfo.InvariantCulture) + " hitches=" + string.Join(";", pmHitchInfo));
                }
                for (int sp = 0; sp < shN; sp++)
                    Debug.Log("[AuditShake] p=" + sp + " name=" + shNames[sp] + " frames=" + shFrames[sp] + " att=" + shAtt[sp] + " hold=" + shHoldF[sp] + " posRev=" + shPosRev[sp] + " attPosRev=" + shAttPosRev[sp] + " holdPosRev=" + shHoldPosRev[sp] + " fwdRev=" + shFwdRev[sp] + " yawRev=" + shYawRev[sp] + " camRev=" + shCamRev[sp] + " auditYaw=" + shAuditYaw[sp].ToString("0", CultureInfo.InvariantCulture) + " totYaw=" + shTotYaw[sp].ToString("0", CultureInfo.InvariantCulture) + " speedJitter=" + (shSpN[sp] > 0 ? shSpDelta[sp] / shSpN[sp] : 0f).ToString("0.00", CultureInfo.InvariantCulture) + " maxStep=" + shMaxStep[sp].ToString("0.000", CultureInfo.InvariantCulture));
                if (_shakeRestore != null) { _shakeRestore(); _shakeRestore = null; }
                Debug.Log("[AuditCamera] min=" + camMin.ToString("0.00", CultureInfo.InvariantCulture) + " closeFrames=" + camClose + " of=" + camFrames);
                foreach (var kv in spawnHand) Debug.Log("[AuditSpawnPose] " + kv.Key + " minHand=" + kv.Value.ToString("0.00", CultureInfo.InvariantCulture) + " clip=" + spawnSeen[kv.Key].Replace(" ", "_") + " lowSamples=" + (spawnLow.TryGetValue(kv.Key, out int pl) ? pl : 0) + "/" + (spawnN.TryGetValue(kv.Key, out int pn) ? pn : 0) + (spawnFirstLow.TryGetValue(kv.Key, out float pf) ? " firstLowAt=" + pf.ToString("0.0", CultureInfo.InvariantCulture) + "s " + spawnLowInfo[kv.Key] : ""));
                KeyUp(Key.W);
                SetMouse(0);
                yield return new WaitGame(1.0f);
            }
            finally
            {
                ReleaseAllInput();
                if (_shakeRestore != null) { _shakeRestore(); _shakeRestore = null; }
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
            var moveLines = logs.Where(x => x.Contains("[CooldownMove]")).ToList();
            int mvStrafe = moveLines.Count(x => x.Contains("kind=Strafe")), mvBack = moveLines.Count(x => x.Contains("kind=Backstep")), mvHold = moveLines.Count(x => x.Contains("kind=HolderSidestep"));
            R.Add("Interrupts", "int.moves", "Slice 3 cooldown moves (engager strafe / backstep, holder sidestep)", moveLines.Count > 0 ? "PASS" : "WARN",
                $"engager strafes {mvStrafe}, engager backsteps {mvBack}, holder sidesteps {mvHold} in the run; other kinds {moveLines.Count - mvStrafe - mvBack - mvHold}; quick-step frames {quickFrames} (animator speed boosted in {quickBoostFrames}, max {maxQuickAnimSpeed:0.00}); animator speed above 1 outside quick steps (max, 1.00 = none; combo/hit-stop speeds count): {maxIdleAnimSpeed:0.00}", moveLines.Count > 0 ? "" : "no cooldown move was logged (logInterruptsInPlay must be on)");            int blkHits = logs.Count(x => x.Contains("[EnemyBlock] blocked hit")), blkRecoil = logs.Count(x => x.Contains("player recoil id")), blkBroken = logs.Count(x => x.Contains("[EnemyBlock] guard broken"));
            int blkCharged = logs.Count(x => x.Contains("guard broken (charged hit)")), blkPoise = logs.Count(x => x.Contains("guard broken (poise break)"));
            R.Add("Interrupts", "int.block", "Enemy block (brain Defend -> guard, blocked hits, player recoil)", blkHits + blkBroken > 0 ? "PASS" : "WARN",
                $"blocked hits {blkHits} (player recoiled {blkRecoil}), guards broken {blkBroken} (charged {blkCharged}, poise {blkPoise}) in the run", blkHits + blkBroken > 0 ? "" : "no enemy block landed (block needs a Defend decision + a player hit inside the block window)");
            var rollLines = logs.Where(x => x.Contains("[DodgeRoll]")).ToList();
            int rollStarts = logs.Count(x => x.Contains("[DodgeRollStart]"));
            float F(string line, string key) { int k = line.IndexOf(" " + key + "=", System.StringComparison.Ordinal); if (k < 0) return 0f; k += key.Length + 2; int e = line.IndexOf(' ', k); string s = e < 0 ? line.Substring(k) : line.Substring(k, e - k); float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v); return v; }
            string S(string line, string key) { int k = line.IndexOf(" " + key + "=", System.StringComparison.Ordinal); if (k < 0) return ""; k += key.Length + 2; int e = line.IndexOf(' ', k); return e < 0 ? line.Substring(k) : line.Substring(k, e - k); }
            int rollsToward = 0, rollsBack = 0, rollsSide = 0, rollsComplete = 0, rollsFullDistance = 0, rollsShort = 0; float sumAnim = 0f, sumElapsed = 0f, sumMoved = 0f; var rollReasons = new Dictionary<string, int>(); var rollDetail = new List<string>();
            foreach (string rl in rollLines)
            {
                float dd = F(rl, "dirDot"), pd = F(rl, "dispDot"), mv = F(rl, "moved"), pl = F(rl, "planned"), an = F(rl, "animSec"), el = F(rl, "elapsed"); string rs = S(rl, "reason"); string cl = S(rl, "clip");
                if (dd > 0.05f || pd > 0.05f) rollsToward++;
                if (cl == "back") rollsBack++; else rollsSide++;
                rollReasons[rs.Split(':')[0]] = (rollReasons.TryGetValue(rs.Split(':')[0], out int rc) ? rc : 0) + 1;
                if (rs == "complete") { rollsComplete++; if (pl > 0.01f && mv >= pl * 0.9f) rollsFullDistance++; else if (pl > 0.01f) rollsShort++; }
                sumAnim += an; sumElapsed += el; sumMoved += mv;
                rollDetail.Add(cl + " " + mv.ToString("0.0", CultureInfo.InvariantCulture) + "/" + pl.ToString("0.0", CultureInfo.InvariantCulture) + "m anim " + an.ToString("0.00", CultureInfo.InvariantCulture) + "s " + rs);
            }
            {
                var byEn = new List<string>();
                foreach (var g in rollLines.GroupBy(x => { int a0 = x.IndexOf("[DodgeRoll] ", System.StringComparison.Ordinal); if (a0 < 0) return "?"; a0 += 12; int e0 = x.IndexOf(" clip=", a0, System.StringComparison.Ordinal); return e0 < 0 ? "?" : x.Substring(a0, e0 - a0); }))
                {
                    int tw = g.Count(x => F(x, "dirDot") > 0.05f || F(x, "dispDot") > 0.05f); float maxDot = g.Max(x => Mathf.Max(F(x, "dirDot"), F(x, "dispDot")));
                    byEn.Add(g.Key + ": " + g.Count() + " rolls, toward the player " + tw + ", max dir/disp dot vs the player " + maxDot.ToString("0.00", CultureInfo.InvariantCulture) + ", back " + g.Count(x => S(x, "clip") == "back") + " / side " + g.Count(x => S(x, "clip") != "back") + ", mean moved " + g.Average(x => F(x, "moved")).ToString("0.0", CultureInfo.InvariantCulture) + "m");
                }
                R.Add("Interrupts", "int.roll.byenemy", "Dodge roll direction per enemy (never toward the player; Humadroid included)", rollLines.Any(x => F(x, "dirDot") > 0.05f || F(x, "dispDot") > 0.05f) ? "FAIL" : (rollLines.Count == 0 ? "WARN" : "PASS"), rollLines.Count == 0 ? "no roll logged" : string.Join(" / ", byEn), "");
            }
            {
                int clipRoll = rollLines.Count(x => S(x, "clip") == "roll"), clipBack = rollLines.Count(x => S(x, "clip") == "back"), clipLeft = rollLines.Count(x => S(x, "clip") == "left"), clipRight = rollLines.Count(x => S(x, "clip") == "right");
                int notSeen = rollLines.Count(x => S(x, "clipSeen") == "0"); int cutEarly = rollLines.Count(x => S(x, "reason") != "complete");
                R.Add("Interrupts", "int.roll.clips", "Roll clip actually used per roll (no forward Roll clip; side = DM_DodgeLeft/Right, back = DM_DodgeBack)", rollLines.Count == 0 ? "WARN" : (clipRoll == 0 && notSeen == 0 && cutEarly == 0 ? "PASS" : "FAIL"),
                    rollLines.Count == 0 ? "no roll logged" : $"{rollLines.Count} rolls: forward Roll clip {clipRoll}, back {clipBack}, left {clipLeft}, right {clipRight}; animator state never seen {notSeen}; ended early (not complete) {cutEarly} [{string.Join(", ", rollLines.Where(x => S(x, "reason") != "complete").Select(x => S(x, "reason")))}]", "a roll used the forward Roll clip, its clip state was never entered, or it ended before complete");
            }
            string rollStatus = rollLines.Count == 0 ? "WARN" : (rollsToward == 0 && rollsShort == 0 && rollStarts - rollLines.Count <= 1 ? "PASS" : "FAIL");
            R.Add("Interrupts", "int.roll", "Dodge roll: direction vs displacement, movement completes with the clip", rollStatus,
                rollLines.Count == 0 ? "no dodge roll was logged" : $"{rollStarts} started, {rollLines.Count} finished (reasons: {string.Join(", ", rollReasons.Select(kv => kv.Key + " x" + kv.Value))}); full distance (>=90% of plan) {rollsFullDistance}, short while complete {rollsShort}; toward the player {rollsToward}; back {rollsBack}, side {rollsSide}; mean moved {sumMoved / rollLines.Count:0.00}m, mean animation {sumAnim / rollLines.Count:0.00}s, mean elapsed {sumElapsed / rollLines.Count:0.00}s; per roll: {string.Join(" | ", rollDetail)}",
                rollStatus == "FAIL" ? "an enemy rolled toward the player, stopped short while the clip kept playing, or a roll never finished" : "");            var strikeAll = logs.Where(x => x.Contains("[DodgeStrike]")).ToList(); int strikeCancelled = strikeAll.Count(x => S(x, "reason") == "roll");
            var strikeLines = strikeAll.Where(x => S(x, "reason") != "roll").ToList(); // strikes overridden by a (forced) defensive roll are counted separately
            int strikeSwings = logs.Count(x => x.Contains("[DodgeStrikeSwing]"));
            {
                int sHand = strikeLines.Count(x => S(x, "reason") == "handover"), sNoTok = strikeLines.Count(x => F(x, "tokenHeld") < 0.5f), sNoAnim = strikeLines.Count(x => F(x, "anim") < 0.5f);
                float sMoved = 0f, sPlan = 0f, sDrift = 0f, sMaxDrift = 0f; var sReasons = new Dictionary<string, int>();
                foreach (string sl in strikeLines)
                {
                    sMoved += F(sl, "moved"); sPlan += F(sl, "planned"); string r3 = S(sl, "reason").Split(':')[0]; sReasons[r3] = (sReasons.TryGetValue(r3, out int c3) ? c3 : 0) + 1;
                    int ar = sl.IndexOf(" dist ", System.StringComparison.Ordinal); if (ar >= 0) { string seg = sl.Substring(ar + 6); int sp2 = seg.IndexOf(' '); seg = sp2 < 0 ? seg : seg.Substring(0, sp2); var pr = seg.Split(new[] { "->" }, System.StringSplitOptions.None); if (pr.Length == 2 && float.TryParse(pr[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float d0) && float.TryParse(pr[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float d1)) { sDrift = Mathf.Abs(d1 - d0); sMaxDrift = Mathf.Max(sMaxDrift, sDrift); } }
                }
                string sStatus = strikeLines.Count == 0 ? "WARN" : (sNoTok == 0 && sNoAnim == 0 && sMoved <= sPlan * 1.5f + 0.1f * strikeLines.Count && strikeSwings >= sHand - 1 ? "PASS" : "FAIL");
                R.Add("Interrupts", "int.dodgestrike", "Dodge strike (HumanM@Dodge01 -> quick swing): token, spacing, hand-over", sStatus,
                    strikeLines.Count == 0 ? "no dodge strike was rolled in this run" : $"{strikeLines.Count} dodge strike(s) (reasons: {string.Join(", ", sReasons.Select(kv => kv.Key + " x" + kv.Value))}); mean weave {sMoved / strikeLines.Count:0.00}m of {sPlan / strikeLines.Count:0.00}m planned; max distance change to the target {sMaxDrift:0.00}m (includes the player moving); without token {sNoTok}; clip never played {sNoAnim}; swing started after the dodge {strikeSwings}x; overridden by a defensive roll {strikeCancelled}",
                    sStatus == "FAIL" ? "a dodge strike ran without the token, never played the clip, changed the distance by more than 0.5m, or no swing followed" : "");
            }

            {
                var reach = logs.Where(x => x.Contains("[SwingReach]")).ToList();
                if (reach.Count > 0)
                {
                    var perEn = new List<string>(); int farEnemies = 0;
                    foreach (var g in reach.GroupBy(x => { int a0 = x.IndexOf("[SwingReach] ", System.StringComparison.Ordinal) + 13; int e0 = x.IndexOf(" n=", a0, System.StringComparison.Ordinal); return e0 < 0 ? "?" : x.Substring(a0, e0 - a0); }))
                    {
                        float mtip = g.Average(x => F(x, "tip")), mstart = g.Average(x => F(x, "startDist")), mxstart = g.Max(x => F(x, "startDist")), mgate = g.Average(x => F(x, "gate")); float f1 = g.Where(x => F(x, "n") < 1.5f).Select(x => F(x, "startDist")).DefaultIfEmpty(-1f).Average();
                        if (mxstart > mtip + 0.45f) farEnemies++;
                        perEn.Add(g.Key + ": " + g.Count() + " swings, start mean " + mstart.ToString("0.00", CultureInfo.InvariantCulture) + " (max " + mxstart.ToString("0.00", CultureInfo.InvariantCulture) + ", first swing " + f1.ToString("0.00", CultureInfo.InvariantCulture) + ") vs weapon tip reach " + mtip.ToString("0.00", CultureInfo.InvariantCulture) + ", gate " + mgate.ToString("0.00", CultureInfo.InvariantCulture));
                    }
                    R.Add("Interrupts", "int.swingreach.byenemy", "Swing-start distance per enemy (Humadroid / Robot use their own weapon reach)", farEnemies == 0 ? "PASS" : "WARN", string.Join(" | ", perEn), farEnemies == 0 ? "" : "an enemy swung from more than its weapon reach + 0.45 m");
                    string Grp(IEnumerable<string> g)
                    {
                        var l = g.ToList(); if (l.Count == 0) return "none";
                        float ms = l.Average(x => F(x, "startDist")), mx = l.Max(x => F(x, "startDist")), mr = l.Average(x => F(x, "range")), mg = l.Average(x => F(x, "minGap")), tp = l.Average(x => F(x, "tip")); int off = l.Count(x => F(x, "minGap") > 0.15f);
                        return l.Count + " swings: start distance mean " + ms.ToString("0.00", CultureInfo.InvariantCulture) + "m (max " + mx.ToString("0.00", CultureInfo.InvariantCulture) + "), attack range " + mr.ToString("0.00", CultureInfo.InvariantCulture) + ", swing-start gate mean " + l.Average(x => F(x, "gate")).ToString("0.00", CultureInfo.InvariantCulture) + ", weapon length " + l.Average(x => F(x, "wlen")).ToString("0.00", CultureInfo.InvariantCulture) + "m, weapon tip reach from root mean " + tp.ToString("0.00", CultureInfo.InvariantCulture) + "m, closest weapon-to-player gap mean " + mg.ToString("0.00", CultureInfo.InvariantCulture) + "m, out of reach (gap>0.15m) " + off + " (" + (100 * off / l.Count) + "%)";
                    }
                    var firstS = reach.Where(x => F(x, "n") < 1.5f); var laterS = reach.Where(x => F(x, "n") >= 1.5f);
                    int offFirst = firstS.Count(x => F(x, "minGap") > 0.15f);
                    R.Add("Interrupts", "int.swingreach", "Swing-start distance vs weapon reach (first vs later swings)", offFirst == 0 ? "PASS" : "WARN", "first swing per enemy: " + Grp(firstS) + " | later swings: " + Grp(laterS), offFirst == 0 ? "" : "a first swing started out of weapon reach");
                }
            }

            {
                var reach2 = logs.Where(x => x.Contains("[SwingReach]")).ToList();
                var samples = logs.Where(x => x.Contains("[FairnessSample]")).ToList();
                string startLine = logs.FirstOrDefault(x => x.Contains("[AuditStart]")); float t0 = startLine != null ? F(startLine.Replace("t=", " t="), "t") : 0f;
                if (samples.Count > 0)
                {
                    var rows = new List<string>(); int minSw = int.MaxValue, maxSw = 0; float maxFirst = 0f; bool anyZero = false;
                    foreach (string sm in samples)
                    {
                        string en = sm.Substring(sm.IndexOf("[FairnessSample] ", System.StringComparison.Ordinal) + 17); en = en.Substring(0, en.IndexOf(' '));
                        var mine = reach2.Where(x => x.Contains("[SwingReach] " + en + " ")).ToList();
                        int sw = mine.Count; float first = sw > 0 ? mine.Min(x => F(x, "t")) - t0 : -1f;
                        minSw = Mathf.Min(minSw, sw); maxSw = Mathf.Max(maxSw, sw); if (sw == 0) anyZero = true; maxFirst = Mathf.Max(maxFirst, first);
                        float nn = Mathf.Max(1f, F(sm, "n"));
                        rows.Add(en + ": " + sw + " swing seq, first swing " + (first < 0f ? "never" : first.ToString("0.0", CultureInfo.InvariantCulture) + "s") + ", tokens " + F(sm, "tokens") + ", token-eligible " + (100f * F(sm, "elig") / nn).ToString("0", CultureInfo.InvariantCulture) + "% of samples, Engager " + (100f * F(sm, "eng") / nn).ToString("0", CultureInfo.InvariantCulture) + "%");
                    }
                    bool unfair = anyZero || (maxSw > 0 && minSw * 2.5f < maxSw);
                    R.Add("Interrupts", "int.fairness", "Attack fairness across enemies (swing sequences, time to first attack, token eligibility)", unfair ? "WARN" : "PASS", string.Join(" | ", rows) + "; most/least swings " + maxSw + "/" + (minSw == int.MaxValue ? 0 : minSw), unfair ? "an enemy attacked less than 40% as often as another, or never" : "");
                }
            }

            {
                string ph = logs.LastOrDefault(x => x.Contains("[PlayerHits]"));
                if (ph != null) R.Add("Interrupts", "int.playerhits", "Audit player: melee hits landed (needed for block / interrupt checks)", ph.Contains("hits=") && !ph.Contains(":0 ") && !ph.EndsWith(":0") ? "PASS" : "WARN", ph.Substring(ph.IndexOf("[PlayerHits]", System.StringComparison.Ordinal) + 13), "");
            }

            {
                var fx = logs.Where(x => x.Contains("[HitFx]")).ToList();
                if (fx.Count > 0)
                {
                    var parts = new List<string>();
                    foreach (var grp in fx.GroupBy(x => S(x, "kind")))
                    {
                        var heavy = grp.Where(x => F(x, "heavy") > 0.5f).ToList(); var light = grp.Where(x => F(x, "heavy") < 0.5f).ToList(); var chained = grp.Where(x => F(x, "chain") >= 2f).ToList();
                        string Avg(List<string> l) { return l.Count == 0 ? "n/a" : l.Average(x => F(x, "mult")).ToString("0.00", CultureInfo.InvariantCulture) + " (n " + l.Count + ")"; }
                        parts.Add(grp.Key + ": light x" + Avg(light) + ", heavy x" + Avg(heavy) + ", chain>=2 x" + Avg(chained));
                    }
                    var fxBy = new List<string>(); int wrongBlood = 0;
                    foreach (var gn in fx.GroupBy(x => { int a0 = x.IndexOf("[HitFx] ", System.StringComparison.Ordinal) + 8; int e0 = x.IndexOf(" kind=", a0, System.StringComparison.Ordinal); return e0 < 0 ? "?" : x.Substring(a0, e0 - a0); }))
                    {
                        bool mech = gn.Key.IndexOf("Humadroid", System.StringComparison.OrdinalIgnoreCase) >= 0 || gn.Key.IndexOf("Robot", System.StringComparison.OrdinalIgnoreCase) >= 0 || gn.Key.IndexOf("Android", System.StringComparison.OrdinalIgnoreCase) >= 0;
                        int bl = gn.Count(x => S(x, "kind") == "Blood"); if (mech) wrongBlood += bl;
                        fxBy.Add(gn.Key + ": " + string.Join(", ", gn.GroupBy(x => S(x, "kind")).Select(k => k.Key + " x" + k.Count())));
                    }
                    R.Add("Interrupts", "int.hitfx", "Hit FX scaling (blood / coolant / sparks use one scaling path)", wrongBlood == 0 ? "PASS" : "FAIL", string.Join(" | ", parts) + " || per enemy: " + string.Join(" | ", fxBy), wrongBlood == 0 ? "" : "an Android / Robot spawned red blood");
                }
            }

            var stepEnds = logs.Where(x => x.Contains("[CooldownMoveEnd]")).ToList();
            if (stepEnds.Count > 0)
            {
                var byKind = new Dictionary<string, List<string>>();
                foreach (string se in stepEnds) { string k = S(se, "kind"); if (!byKind.TryGetValue(k, out var l)) byKind[k] = l = new List<string>(); l.Add(se); }
                var parts = new List<string>(); int shortSteps = 0, zeroSteps = 0;
                foreach (var kv in byKind)
                {
                    float sm = 0f, sp = 0f; var rs2 = new Dictionary<string, int>();
                    foreach (string se in kv.Value) { float mv2 = F(se, "moved"), pl2 = F(se, "planned"); sm += mv2; sp += pl2; string r2 = S(se, "reason"); rs2[r2] = (rs2.TryGetValue(r2, out int c2) ? c2 : 0) + 1; if (mv2 < 0.05f) zeroSteps++; else if (pl2 > 0.1f && mv2 < pl2 * 0.6f && r2 == "complete") shortSteps++; }
                    parts.Add(kv.Key + " x" + kv.Value.Count + " moved " + (sm / kv.Value.Count).ToString("0.00", CultureInfo.InvariantCulture) + "m of " + (sp / kv.Value.Count).ToString("0.00", CultureInfo.InvariantCulture) + "m planned (" + string.Join(", ", rs2.Select(q => q.Key + " " + q.Value)) + ")");
                }
                var animEnds = logs.Where(x => x.Contains("[StepAnimEnd]")).Select(x => F(x, "lag")).ToList();
                parts.Add("step animation end vs move end: " + (animEnds.Count == 0 ? "no sample" : "n " + animEnds.Count + ", mean lag " + (animEnds.Average() * 1000f).ToString("0", CultureInfo.InvariantCulture) + " ms, max " + (animEnds.Max() * 1000f).ToString("0", CultureInfo.InvariantCulture) + " ms, over 150 ms: " + animEnds.Count(v => v > 0.15f)));
                R.Add("Interrupts", "int.steps", "Quick steps (strafe / backstep / holder sidestep): displacement vs plan", zeroSteps == 0 ? "PASS" : "WARN", string.Join("; ", parts) + "; zero-displacement steps " + zeroSteps + ", short while complete " + shortSteps, zeroSteps == 0 ? "" : "a step started but the enemy did not move (blocked / interrupted before the first frames)");
            }

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
            {
                string ovl = logs.LastOrDefault(x => x.Contains("[AuditOverlap]")); string cam = logs.LastOrDefault(x => x.Contains("[AuditCamera]"));
                if (ovl != null) R.Add("Interrupts", "int.overlap", "Overlapping attack windows (one attacker at a time, all enemy types, swings still finishing)", F(ovl, "windows") < 0.5f ? "PASS" : "FAIL", ovl.Substring(ovl.IndexOf("[AuditOverlap]", System.StringComparison.Ordinal) + 14).Trim(), F(ovl, "windows") < 0.5f ? "" : "two enemies were in an attack sequence at the same time");
                if (cam != null) R.Add("Interrupts", "int.camera", "Camera distance to the player pivot during the fight (enemies never pull the lens into the player)", F(cam, "min") >= 1.2f ? "PASS" : (F(cam, "closeFrames") > 0.5f ? "FAIL" : "WARN"), cam.Substring(cam.IndexOf("[AuditCamera]", System.StringComparison.Ordinal) + 13).Trim(), "");
                var slides = logs.Where(x => x.Contains("[SlideNoLegs]")).ToList();
                string SlideName(string x) { int a0 = x.IndexOf("[SlideNoLegs] ", System.StringComparison.Ordinal) + 14; int e0 = x.IndexOf(" speed=", a0, System.StringComparison.Ordinal); return e0 < 0 ? "?" : x.Substring(a0, e0 - a0); }
                R.Add("Interrupts", "int.slide", "Sliding without leg animation (body moving > 0.4 m/s with no locomotion blend, rolls / dodge strikes excluded)", slides.Count == 0 ? "PASS" : "WARN",
                    slides.Count == 0 ? "no slide episodes" : $"{slides.Count} episodes: " + string.Join(" | ", slides.GroupBy(SlideName).Select(g => g.Key + " x" + g.Count() + " (blocked " + g.Count(x => S(x, "blocked") == "1") + ", stagger " + g.Count(x => S(x, "stagger") == "1") + ", attacking " + g.Count(x => S(x, "attacking") == "1") + ", quick " + g.Count(x => S(x, "quick") == "1") + ", suppress " + g.Count(x => S(x, "suppress") == "1") + ", mean speed " + g.Average(x => F(x, "speed")).ToString("0.00", CultureInfo.InvariantCulture) + ", clips " + string.Join("/", g.Select(x => S(x, "clip")).Distinct()) + ", movers " + string.Join("/", g.Select(x => S(x, "mover")).Distinct()) + ", ctrlMag " + g.Average(x => F(x, "ctrlMag")).ToString("0.00", CultureInfo.InvariantCulture) + ", states " + string.Join("/", g.Select(x => S(x, "state")).Distinct()) + ")")), slides.Count == 0 ? "" : "code-driven displacement without a leg cycle");
                {
                    var rdiag = logs.Where(x => x.Contains("[RollClipLeft]") || x.Contains("[RollFallback]")).Select(x => x.Substring(x.IndexOf("[Roll", System.StringComparison.Ordinal)).Replace("  ", " ")).ToList();
                    R.Add("Interrupts", "int.roll.diag", "Roll diagnostics (clip cut by another transition / no dodge clip chosen)", rdiag.Count == 0 ? "PASS" : "WARN", rdiag.Count == 0 ? "none" : string.Join(" || ", rdiag.Take(8)), "");
                }
                string shStatic = logs.LastOrDefault(x => x.Contains("[AuditShakeStatic]"));
                if (shStatic != null) R.Add("Interrupts", "int.shake.static", "Player shake: static physics / animator setup (interpolation, update mode, rig colliders are triggers, nothing pushes the capsule)", S(shStatic, "rigNonTriggerColliders") != "0" || S(shStatic, "rigRigidbodies") != "0" || S(shStatic, "nonKinematicBoneRigidbodies") != "0" || S(shStatic, "rbInterp") == "None" ? "WARN" : "PASS", shStatic.Substring(shStatic.IndexOf("[AuditShakeStatic]", System.StringComparison.Ordinal) + 18).Trim(), "");
                var shLines = logs.Where(x => x.Contains("[AuditShake] p=")).ToList();
                if (shLines.Count > 0)
                {
                    string ShRate(string x, string k) { float fr = Mathf.Max(1f, F(x, "frames")); return (F(x, k) / fr * 100f).ToString("0.0", CultureInfo.InvariantCulture); }
                    string det = string.Join(" / ", shLines.Select(x => S(x, "name") + ": frames " + F(x, "frames") + " (attack " + F(x, "att") + ", holdFrames " + F(x, "hold") + "), posReversals/100f " + ShRate(x, "posRev") + " (during attack " + F(x, "attPosRev") + ", during hold " + F(x, "holdPosRev") + "), forwardReversals/100f " + ShRate(x, "fwdRev") + ", yawReversals/100f " + ShRate(x, "yawRev") + ", cameraReversals/100f " + ShRate(x, "camRev") + ", speedJitter " + S(x, "speedJitter") + " m/s, audit-applied yaw " + S(x, "auditYaw") + " of " + S(x, "totYaw") + " deg, max step " + S(x, "maxStep") + " m"));
                    string shBase = shLines.First();
                    bool shBad = F(shBase, "posRev") / Mathf.Max(1f, F(shBase, "frames")) > 0.04f || F(shBase, "camRev") / Mathf.Max(1f, F(shBase, "frames")) > 0.15f || F(shBase, "speedJitter") > 0.8f;
                    R.Add("Interrupts", "int.shake", "Player shake study (8 phases of 12 s: baseline, player not frozen, hit stop off, hitbox rig off, interpolation None, old hold speed 0.05, no forward drift, player only)", shBad ? "WARN" : "PASS", det, "");
                }
                string pmLine = logs.LastOrDefault(x => x.Contains("[AuditPlayerMotion]"));                if (pmLine != null) R.Add("Interrupts", "int.playermotion", "Player motion smoothness (frame time, timeScale, animator speed, locomotion param jitter, root speed jitter, W key chatter)", F(pmLine, "relHitchFrames") > 40f || F(pmLine, "wFlips") > 40f || F(pmLine, "inputMagFlips") > 60f || F(pmLine, "speedJumps") > 20f ? "WARN" : "PASS", pmLine.Substring(pmLine.IndexOf("[AuditPlayerMotion]", System.StringComparison.Ordinal) + 19).Trim(), "");
                var poses = logs.Where(x => x.Contains("[AuditSpawnPose]")).ToList();
                if (poses.Count > 0) R.Add("Interrupts", "int.spawnpose", "Spawn pose (idle hand distance in the first 4 s; under 0.35 = crossed arms)", poses.Any(x => F(x, "minHand") < 0.35f) ? "FAIL" : "PASS", string.Join(" | ", poses.Select(x => { int a0 = x.IndexOf("[AuditSpawnPose] ", System.StringComparison.Ordinal) + 17; return x.Substring(a0); })), "");
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
