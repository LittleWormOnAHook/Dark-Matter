#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Project.AI;
using Project.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace Project.EditorTools.Audit
{
    public static partial class DMEnemyRuntimeAudit
    {
        // =============================================================== scripted gameplay (real keyboard input)
        // Looks like real play: the camera is aimed at the enemy and W / A / S / D / Space / Shift do the moving, so approach, kite, strafe,
        // re-engage, jump away, jetpack escape and lose-target all go through the player's own controller.
        private static bool IsCalm(string st) { return st == "Idle" || st == "Wander" || st == "Patrol" || st == "ReturnHome" || st == "?"; }
        private static bool IsHostile(string st) { return st == "Chase" || st == "Attack" || st == "Hold" || st == "Defensive"; }

        private static float _driveT;

        /// <summary>Holds keys while keeping the camera on the enemy until `until` is true or `max` seconds pass.</summary>
        private static IEnumerator Drive(Live L, Key[] keys, float max, Func<bool> until, Action each = null)
        {
            KeyDown(keys);
            float t0 = Time.time;
            while (Time.time - t0 < max && L.go != null)
            {
                AimCameraAt(L.go.transform.position);
                each?.Invoke();
                if (until != null && until()) break;
                yield return null;
            }

            _driveT = Time.time - t0;
            KeyUp(keys);
            yield return null;
        }

        private static IEnumerator ScnPlay(GameObject prefab, DMAuditEnemyResult R)
        {
            _camFollow = false;
            string arm = ArmPlayerWithSword();
            yield return new WaitGame(1.0f);
            Live L = SpawnMatrixEnemy(prefab, "play");
            yield return new WaitGame(1.0f);
            if (L.ai == null || L.go == null) { R.Add("Play", "play.setup", "Setup", "FAIL", "template missing AI", ""); _camFollow = true; yield break; }
            Vector3 home = L.go.transform.position;
            Vector3 startPos = Ground(home + new Vector3(-30f, 0f, 0f));
            PlacePlayer(startPos, home);
            AimCameraAt(home);
            yield return new WaitGame(0.8f);
            L.BeginPhase();
            Func<float> dist = () => PlayerEnemyDist(L);
            Func<string> st = () => AiState(L.ai);

            // 1 approach (W)
            float noticeDist = -1f, noticeAt = -1f;
            float tA = Time.time;
            yield return Drive(L, new[] { Key.W }, 14f, () => dist() < 9f, () =>
            {
                if (noticeAt < 0f && !IsCalm(st())) { noticeAt = Time.time - tA; noticeDist = dist(); }
            });
            float dAfterApproach = dist();
            R.Add("Play", "play.approach", "Approach with W: enemy notices", noticeAt >= 0f ? "PASS" : "WARN",
                $"walked {30f - dAfterApproach:0.0}m in {_driveT:0.0}s to {dAfterApproach:0.0}m; enemy noticed after {FmtT(noticeAt)} at {(noticeDist >= 0f ? noticeDist.ToString("0.0", CultureInfo.InvariantCulture) + "m" : "n/a")}; state {st()}",
                noticeAt >= 0f ? "" : "enemy never left a calm state while the player walked up to 9m in front of it (vision range/FOV/LOS or notice delay)");

            // 2 retreat / kite (S): does the enemy pursue?
            L.BeginPhase();
            Vector3 e0 = L.go.transform.position;
            bool hostileSeen = false;
            yield return Drive(L, new[] { Key.S }, 4f, null, () => { if (IsHostile(st())) hostileSeen = true; });
            float movedE = Vector3.Distance(e0, L.go.transform.position);
            R.Add("Play", "play.kite", "Run away with S: enemy pursues", hostileSeen && movedE > 2f ? "PASS" : "WARN",
                $"player backed away for {_driveT:0.0}s (gap now {dist():0.0}m), enemy moved {movedE:0.0}m, hostile state seen {hostileSeen}, states {string.Join("/", L.phaseStates)}",
                hostileSeen && movedE > 2f ? "" : "enemy did not chase a retreating player: chase blocked (standoff/hold token, ranged keep-distance) or target lost immediately");

            // 3 strafe (D then A)
            float angSum = 0f; int angN = 0; float angMax = 0f;
            Action sampleAng = () => { float a = FacingAngle(L.go.transform, _player.transform.position); angSum += a; angN++; if (a > angMax) angMax = a; };
            yield return Drive(L, new[] { Key.D }, 2f, null, sampleAng);
            yield return Drive(L, new[] { Key.A }, 2f, null, sampleAng);
            float angAvg = angN > 0 ? angSum / angN : 0f;
            R.Add("Play", "play.strafe", "Strafe around the enemy: it turns to face the player", angAvg < 70f ? "PASS" : "WARN",
                $"enemy facing error avg {angAvg:0}deg, max {angMax:0}deg over {angN} frames while strafing D then A, state {st()}",
                angAvg < 70f ? "" : "enemy did not keep facing a strafing player: turn speed too slow, turn-in-place gated, or enemy in a state that ignores the target");

            // 4 retreat then re-engage
            L.BeginPhase();
            int atk0 = L.attackStarts;
            yield return Drive(L, new[] { Key.S }, 2f, null);
            yield return Drive(L, new[] { Key.W }, 8f, () => dist() < 2.6f);
            yield return new WaitGame(3.5f);
            AimCameraAt(L.go.transform.position);
            R.Add("Play", "play.reengage", "Retreat then re-engage: enemy fights back", L.attackStarts > atk0 || L.PhaseDamage > 0f ? "PASS" : "WARN",
                $"re-engaged to {dist():0.0}m, enemy attacks {L.attackStarts - atk0}, player hp lost {L.PhaseDamage:0.0}, state {st()}",
                L.attackStarts > atk0 || L.PhaseDamage > 0f ? "" : "enemy never attacked a player standing next to it: attack cooldown/token starvation or stagger lock");

            // 5 jump away
            float gap0 = dist();
            float y0 = _player.transform.position.y, ymax = y0;
            yield return Drive(L, new[] { Key.Space, Key.S }, 1.1f, null, () => ymax = Mathf.Max(ymax, _player.transform.position.y));
            yield return Drive(L, new[] { Key.S }, 1.5f, null);
            R.Add("Play", "play.jump", "Jump away: player clears distance", dist() > gap0 + 1.5f && ymax - y0 > 0.25f ? "PASS" : "WARN",
                $"jump height {ymax - y0:0.00}m, gap {gap0:0.0}m -> {dist():0.0}m, enemy state {st()}",
                ymax - y0 <= 0.25f ? "jump did not leave the ground (Jump input / grounded)" : "player could not gain distance (enemy kept pace or player blocked)");

            // 6 jetpack escape
            float gapJ0 = dist();
            float yj0 = _player.transform.position.y, yjmax = yj0;
            yield return Drive(L, new[] { Key.Space, Key.S }, 3.2f, null, () => yjmax = Mathf.Max(yjmax, _player.transform.position.y));
            yield return new WaitGame(1.0f);
            R.Add("Play", "play.jetpack", "Jetpack escape (hold Space + S)", yjmax - yj0 > 2.2f ? "PASS" : "WARN",
                $"peak height gain {yjmax - yj0:0.0}m, gap {gapJ0:0.0}m -> {dist():0.0}m, enemy state {st()}",
                yjmax - yj0 > 2.2f ? "" : "no sustained lift: jetpack locked/no fuel or DMJetpackInputBridge not polling Space; the enemy-reaction part still ran as a jump");

            // 7 lose target + return home
            float tLose0 = Time.time; float loseAt = -1f; float loseDist = -1f;
            yield return Drive(L, new[] { Key.S, Key.LeftShift }, 30f, () => loseAt >= 0f, () =>
            {
                if (loseAt < 0f && IsCalm(st()) && !IsHostile(st()) && Time.time - tLose0 > 1.5f && st() != "Investigate" && st() != "Search") { loseAt = Time.time - tLose0; loseDist = dist(); }
            });
            R.Add("Play", "play.lose", "Sprint away: enemy loses the target", loseAt >= 0f ? "PASS" : "WARN",
                $"lost after {FmtT(loseAt)} at {(loseDist >= 0f ? loseDist.ToString("0.0", CultureInfo.InvariantCulture) : "n/a")}m (sprinting), state {st()}, states {string.Join("/", L.aiStates)}",
                loseAt >= 0f ? "" : "enemy kept hostile for 30s of sprinting away: lose-target distance/time too long (senses) or leash not applied");
            float tRet0 = Time.time; float retAt = -1f;
            float homeNow() { Vector3 d = L.go.transform.position - home; d.y = 0f; return d.magnitude; }
            yield return new WaitUntil(() => { if (retAt < 0f && (st() == "Idle" || st() == "Patrol" || st() == "Wander") && homeNow() < 3.5f) retAt = Time.time - tRet0; return retAt >= 0f; }, 30f);
            R.Add("Play", "play.return", "Enemy returns to its post after losing the player", retAt >= 0f ? "PASS" : "WARN",
                $"back home after {FmtT(retAt)}, now {homeNow():0.0}m from spawn, state {st()}",
                retAt >= 0f ? "" : "enemy did not walk back to its spawn within 30s: ReturnHome blocked, leash undefined, or it stays in Search/Investigate");

            ReleaseAllInput();
            _camFollow = true;
        }
    }
}
#endif
