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
        // =============================================================== helpers
        private static Type FindType(string name)
        {
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = null;
                try { t = a.GetTypes().FirstOrDefault(x => x.Name == name); } catch { /* ignore */ }
                if (t != null)
                    return t;
            }
            return null;
        }

        private static string AiState(EnemyAiController ai)
        {
            if (ai == null) return "?";
            FieldInfo f = typeof(EnemyAiController).GetField("state", BF);
            object v = f != null ? f.GetValue(ai) : null;
            return v != null ? v.ToString() : "?";
        }

        private static readonly int GroundMask = ~((1 << 8) | (1 << 9) | (1 << 10) | (1 << 11) | (1 << 15) | (1 << 2) | (1 << 16) | (1 << 26));

        private static Vector3 Ground(Vector3 p)
        {
            Vector3 o = p + Vector3.up * 60f;
            if (Physics.Raycast(o, Vector3.down, out RaycastHit h, 200f, GroundMask, QueryTriggerInteraction.Ignore))
                return h.point + Vector3.up * 0.08f;
            return p;
        }

        private static void PlacePlayer(Vector3 pos, Vector3 lookAt)
        {
            if (_player == null) return;
            Vector3 flat = lookAt - pos; flat.y = 0f;
            Quaternion rot = flat.sqrMagnitude > 0.001f ? Quaternion.LookRotation(flat.normalized) : _player.transform.rotation;
            Rigidbody rb = _player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = pos; rb.rotation = rot;
                rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
            }
            _player.transform.SetPositionAndRotation(pos, rot);
            Physics.SyncTransforms();
            _lastTeleport = Time.time;
            if (_camFollow) CameraBehind();   // camera stays behind the player, looking where the player faces
        }

        private static float PlayerHpInvector()
        {
            var c = _player != null ? _player.GetComponent<vThirdPersonController>() : null;
            return c != null ? c.currentHealth : float.NaN;
        }

        private static float PlayerHpSurvival()
        {
            var s = _player != null ? _player.GetComponent<Project.Survival.SurvivalStats>() : null;
            return s != null ? s.CurrentHealth : float.NaN;
        }

        private static void TopUpPlayer()
        {
            if (_player == null) return;
            var c = _player.GetComponent<vThirdPersonController>();
            if (c != null) c.ResetHealth(c.maxHealth);
            if (c != null)
            {
                const System.Reflection.BindingFlags bf = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var ft = c.GetType(); var cur = ft.GetField("currentStamina", bf); var mx = ft.GetField("maxStamina", bf);
                if (cur != null && mx != null && cur.FieldType == typeof(float) && mx.FieldType == typeof(float)) cur.SetValue(c, (float)mx.GetValue(c)); // melee swings cost stamina: keep it full so the audit player can always swing
            }
            var s = _player.GetComponent<Project.Survival.SurvivalStats>();
            if (s != null) s.DevSetHealth(s.maxHealth);
            if (_live != null) _live.ResyncHp();
        }

        private static string Fmt(float v) { return float.IsNaN(v) ? "n/a" : v.ToString("0.0", CultureInfo.InvariantCulture); }
        private static string FmtT(float t) { return t < 0f ? "never" : t.ToString("0.0", CultureInfo.InvariantCulture) + "s"; }

        // =============================================================== per-frame sampler
        private sealed class Live
        {
            public GameObject go;
            public EnemyAiController ai;
            public EnemyHealth health;
            public EnemyCombat combat;
            public EnemyInvectorLoadoutBridge bridge;
            public Animator anim;
            public vMeleeManager mm;
            public DMEnemyHitMarks marks;
            public bool trackPlayer;
            public void ResetRunWide() { maxDrawn = maxHolster = strayFrames = rightWeaponStaleFrames = 0; extraDrawn.Clear(); strayRenderers.Clear(); clipSeen.Clear(); meleeHitEvents = 0; }
            public float refilled;                 // hp the harness topped up (EHp = CurrentHealth - refilled)
            public double dtSum, dtMax; public int dtN; // unscaled frame times in the current phase

            // run-wide
            public int maxDrawn, maxHolster, strayFrames, rightWeaponStaleFrames;
            public readonly HashSet<string> extraDrawn = new HashSet<string>();
            public readonly HashSet<string> strayRenderers = new HashSet<string>();
            public readonly HashSet<string> aiStates = new HashSet<string>();
            public readonly Dictionary<string, float> clipSeen = new Dictionary<string, float>();
            public int meleeHitEvents;
            public float chipInv;

            // phase-scoped
            public float t0;
            public float dmgInv, dmgSurv;
            public int missAttacks, dmgEvents; private bool _missLatch;
            public int attackStarts, attackStateFrames, hitboxFrames, unarmedHitboxFrames, projectileSpawns, missFrames, frames;
            public float firstAttackT = -1f, firstDamageT = -1f, noticeT = -1f, chaseT = -1f, investigateT = -1f;
            public float firstAttackDist = -1f;
            public float minDist = float.MaxValue;
            public float yawAccum, path;
            public string weaponAtAttack = "", targetName = "";
            public readonly HashSet<string> phaseStates = new HashSet<string>();

            private float _lastInv = float.NaN, _lastSurv = float.NaN, _lastYaw;
            private Vector3 _lastPos;
            private bool _lastAttacking, _havePos;
            private HashSet<CombatProjectile> _prevProj = new HashSet<CombatProjectile>();
            private float _slotRefresh;
            private readonly List<Transform> _drawn = new List<Transform>();
            private readonly List<Transform> _holster = new List<Transform>();
            private Vector3 _startPos;

            public void ResyncHp() { _lastInv = PlayerHpInvector(); _lastSurv = PlayerHpSurvival(); }

            public void BeginPhase()
            {
                t0 = Time.time;
                dmgInv = dmgSurv = chipInv = 0f;
                attackStarts = attackStateFrames = hitboxFrames = unarmedHitboxFrames = projectileSpawns = missFrames = frames = missAttacks = dmgEvents = 0; _missLatch = false;
                firstAttackT = firstDamageT = noticeT = chaseT = investigateT = -1f;
                firstAttackDist = -1f;
                minDist = float.MaxValue;
                yawAccum = path = 0f;
                weaponAtAttack = ""; targetName = "";
                phaseStates.Clear();
                _havePos = false;
                _lastAttacking = false;
                dtSum = dtMax = 0; dtN = 0;
                _prevProj.Clear();
                if (go != null) { _startPos = go.transform.position; }
                ResyncHp();
            }

            public float NetDisplacement { get { return go != null ? Vector3.Distance(_startPos, go.transform.position) : 0f; } }
            public float PhaseDamage { get { return Mathf.Max(dmgInv, dmgSurv); } }

            public void Sample()
            {
                if (go == null) return;
                frames++;
                float t = Time.time - t0;
                { double dtu = Time.unscaledDeltaTime; dtSum += dtu; dtN++; if (dtu > dtMax) dtMax = dtu; }
                EnemyTopUpTick(this);

                if (trackPlayer && _player != null)
                {
                    float inv = PlayerHpInvector(), surv = PlayerHpSurvival();
                    // A damage event is a single-frame drop >= 1 hp; smaller drops (survival drain / regen noise) are tracked separately.
                    if (!float.IsNaN(_lastInv) && !float.IsNaN(inv) && inv < _lastInv - 0.001f) { float d = _lastInv - inv; if (d >= 1f) { dmgInv += d; dmgEvents++; } else chipInv += d; }
                    if (!float.IsNaN(_lastSurv) && !float.IsNaN(surv) && surv < _lastSurv - 0.001f) { float d = _lastSurv - surv; if (d >= 1f) dmgSurv += d; }
                    _lastInv = inv; _lastSurv = surv;
                    if (PhaseDamage > 0f && firstDamageT < 0f) firstDamageT = t;
                    var c = _player.GetComponent<vThirdPersonController>();
                    var s = _player.GetComponent<Project.Survival.SurvivalStats>();
                    if (c != null && !float.IsNaN(inv) && inv < c.maxHealth * 0.97f) TopUpPlayer(); // continuous top-up (was 0.45)
                    else if (s != null && !float.IsNaN(surv) && surv < s.maxHealth * 0.97f) TopUpPlayer();
                }

                string st = ai != null ? DMEnemyRuntimeAudit.AiState(ai) : "?";
                aiStates.Add(st);
                phaseStates.Add(st);
                bool calm = st == "Idle" || st == "Wander" || st == "Patrol" || st == "ReturnHome";
                if (!calm && noticeT < 0f && st != "?") noticeT = t;
                if (st == "Investigate" && investigateT < 0f) investigateT = t;
                if ((st == "Chase" || st == "Attack" || st == "Hold" || st == "Defensive") && chaseT < 0f) chaseT = t;
                if (st == "Attack") attackStateFrames++;

                float dist = _player != null ? Vector3.Distance(_player.transform.position, go.transform.position) : -1f;
                if (dist >= 0f && dist < minDist) minDist = dist;

                if (combat != null)
                {
                    if (targetName.Length == 0 && combat.CurrentTarget != null) targetName = combat.CurrentTarget.name;
                    bool atk = combat.IsAttacking;
                    if (atk && !_lastAttacking)
                    {
                        attackStarts++;
                        _missLatch = false;
                        if (firstAttackT < 0f)
                        {
                            firstAttackT = t;
                            firstAttackDist = dist;
                            weaponAtAttack = bridge != null && bridge.ActiveItem != null ? bridge.ActiveItem.name : "unarmed";
                        }
                    }
                    _lastAttacking = atk;
                    if (combat.IsIntentionalMissActive) { missFrames++; if (atk && !_missLatch) { _missLatch = true; missAttacks++; } }
                }

                Vector3 pos = go.transform.position;
                float yaw = go.transform.eulerAngles.y;
                if (_havePos)
                {
                    path += Vector3.Distance(new Vector3(pos.x, 0, pos.z), new Vector3(_lastPos.x, 0, _lastPos.z));
                    yawAccum += Mathf.Abs(Mathf.DeltaAngle(_lastYaw, yaw));
                }
                _lastPos = pos; _lastYaw = yaw; _havePos = true;

                if (bridge != null && bridge.ActiveDrawnInstance != null)
                {
                    var w = bridge.ActiveDrawnInstance.GetComponentInChildren<vMeleeWeapon>(true);
                    if (w != null && w.canApplyDamage) hitboxFrames++;
                }
                if (mm != null && mm.Members != null)
                {
                    foreach (var m in mm.Members)
                        if (m != null && m.attackObject != null && m.attackObject.canApplyDamage) { unarmedHitboxFrames++; break; }
                }

                if (Time.frameCount % 3 == 0) // projectiles live for seconds: no need for a scene-wide search every frame
                {
                    var nowSet = new HashSet<CombatProjectile>();
                    foreach (var p in Object.FindObjectsByType<CombatProjectile>())
                    {
                        if (p == null || !p.gameObject.activeInHierarchy) continue;
                                            nowSet.Add(p);
                        if (!_prevProj.Contains(p)) projectileSpawns++;
                    }
                    _prevProj = nowSet;
                }

                if (Time.unscaledTime >= _slotRefresh)
                {
                    _slotRefresh = Time.unscaledTime + 0.1f;
                    SampleVisibility();
                }

                if (anim != null && anim.isActiveAndEnabled && anim.runtimeAnimatorController != null)
                {
                    for (int l = 0; l < anim.layerCount; l++)
                    {
                        foreach (var ci in anim.GetCurrentAnimatorClipInfo(l)) if (ci.clip != null) clipSeen[ci.clip.name] = ci.clip.length;
                        foreach (var ci in anim.GetNextAnimatorClipInfo(l)) if (ci.clip != null) clipSeen[ci.clip.name] = ci.clip.length;
                    }
                }
            }

            private void SampleVisibility()
            {
                _drawn.Clear(); _holster.Clear();
                foreach (Transform tr in go.GetComponentsInChildren<Transform>(false))
                {
                    if (tr.name.StartsWith("Drawn_", StringComparison.Ordinal)) _drawn.Add(tr);
                    else if (tr.name.StartsWith("Holstered_", StringComparison.Ordinal)) _holster.Add(tr);
                }

                if (_drawn.Count > maxDrawn) maxDrawn = _drawn.Count;
                if (_holster.Count > maxHolster) maxHolster = _holster.Count;
                if (_drawn.Count > 1)
                    foreach (Transform d in _drawn) extraDrawn.Add(d.name);

                bool stray = false;
                foreach (Transform d in _drawn)
                {
                    foreach (var r in d.GetComponentsInChildren<Renderer>(false))
                    {
                        if (!r.enabled || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                        bool owned = false;
                        for (Transform a = r.transform; a != null && a != go.transform; a = a.parent)
                            if (bridge != null && bridge.ActiveDrawnInstance != null && a.gameObject == bridge.ActiveDrawnInstance) { owned = true; break; }
                        if (!owned && bridge != null && bridge.ActiveDrawnInstance != null)
                        {
                            stray = true; strayRenderers.Add(d.name + "/" + r.name);
                        }
                    }
                }

                if (stray) strayFrames++;

                if (mm != null && bridge != null && bridge.ActiveItem != null && bridge.ActiveItem.itemType == ItemType.MeleeWeapon && bridge.ActiveDrawnInstance != null)
                {
                    var drawnW = bridge.ActiveDrawnInstance.GetComponentInChildren<vMeleeWeapon>(true);
                    if (drawnW != null && mm.rightWeapon != drawnW) rightWeaponStaleFrames++;
                }
                else if (mm != null && bridge != null && bridge.ActiveItem != null && bridge.ActiveItem.IsRangedWeapon && mm.rightWeapon != null && mm.rightWeapon.gameObject.activeInHierarchy)
                    rightWeaponStaleFrames++;
            }
        }
    }
}
#endif
