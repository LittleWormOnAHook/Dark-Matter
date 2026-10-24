using System.Collections.Generic;
using Project.AI;
using Project.Core;
using Project.Data;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// 2026-10-21c/d: Lightning Gun bolt. The ammo's beam prefab (copy of the FORGE3D Lightning Gun Beam, HDRP materials) is driven as a
    /// travelling bolt: the head leaves the live muzzle at boltSpeed toward the contact point (tail trails at most boltMaxLength),
    /// on arrival the hit is applied through DMCombatRangedResolver.ApplyHitOutcome, the bolt holds for beamDurationSeconds
    /// (optional damage ticks), then the tail retracts into the target over boltTailRetractTime. Chain lightning links do the same,
    /// each starting when the previous head arrives. Never the owner/player, companions, or the same enemy twice. No FORGE3D logic.
    /// </summary>
    [DefaultExecutionOrder(600)]
    public sealed class DMLightningBeamSession : MonoBehaviour
    {
        private sealed class Bolt
        {
            public GameObject Go;
            public LineRenderer[] Lines;
            public bool FromMuzzle;
            public Collider FromCol, ToCol;  // chain links
            public MonoBehaviour Target;
            public float Damage;
            public float Traveled, ArriveTime = -1f, Arrived;
            public bool HasHit, Done, StreamTail; // 2026-10-21i: StreamTail = visual-only stream segment
            public int ChainIndex;
            public RaycastHit Hit;
            public Vector3 Start, End, Dir;
        }

        private GameObject owner, prefab;
        private Transform muzzle;
        private ItemData weapon, ammo;
        private DMAmmoFxProfile p;
        private Vector3 localDir;
        private float damage, range, nextTick;
        private readonly List<Bolt> bolts = new List<Bolt>();
        private readonly HashSet<MonoBehaviour> used = new HashSet<MonoBehaviour>();

        public static void Fire(GameObject owner, Transform muzzle, ItemData weapon, ItemData ammo, DMAmmoFxProfile p,
            Vector3 direction, float damage, float range)
        {
            var go = new GameObject("DMLightningBeamSession");
            go.AddComponent<DMLightningBeamSession>().Begin(owner, muzzle, weapon, ammo, p, direction, damage, range);
        }

        private void Begin(GameObject o, Transform m, ItemData w, ItemData a, DMAmmoFxProfile prof, Vector3 dir, float dmg, float rng)
        {
            owner = o; muzzle = m; weapon = w; ammo = a; p = prof; damage = dmg; range = Mathf.Max(1f, rng);
            localDir = owner != null ? owner.transform.InverseTransformDirection(dir.normalized) : dir.normalized;
            prefab = DMCombatFx.ResolveBeam(a, w);
            Log("fire: session prefab " + (prefab != null ? prefab.name : "NULL") + " speed " + p.boltSpeed + " maxLen " + p.boltMaxLength + " range " + range.ToString("0"));
            // 2026-10-21i: true detached projectile. Snapshot muzzle + direction and raycast ONCE at fire; no muzzle/aim follow afterwards.
            Vector3 start = muzzle.position, d0 = dir.sqrMagnitude > 1e-6f ? dir.normalized : muzzle.forward;
            bool hasHit = DMCombatRangedResolver.TryAimRaycast(start, d0, range, owner, out RaycastHit hit, RangedFireSolver.MuzzleRayStartSkin, 0f, ammo);
            Vector3 end = hasHit ? hit.point : start + d0 * range;
            int segs = Mathf.Clamp(p.streamSegments, 1, 4);
            float spd = Mathf.Max(1f, p.boltSpeed);
            for (int s = 0; s < segs; s++)
            {
                var b = NewBolt(start);
                b.FromMuzzle = true; b.Damage = damage; b.StreamTail = s > 0;
                b.Start = start; b.End = end; b.Dir = d0; b.HasHit = hasHit; b.Hit = hit;
                b.Traveled = -s * Mathf.Max(0.01f, p.streamSegmentGap) * spd; // later segments launch after a gap
                bolts.Add(b);
            }
        }

        private Bolt NewBolt(Vector3 at)
        {
            var b = new Bolt();
            if (prefab != null)
            {
                b.Go = PoolManager.Spawn(prefab, at, Quaternion.identity);
                if (b.Go != null)
                {
                    // 2026-10-21h: a pooled instance must never keep a muzzle-follow (it would pin the line to the gun every frame).
                    foreach (var f in b.Go.GetComponentsInChildren<HitscanBeamMuzzleFollow>(true)) f.enabled = false;
                    b.Lines = b.Go.GetComponentsInChildren<LineRenderer>(true);
                    for (int i = 0; i < b.Lines.Length; i++)
                    {
                        var l = b.Lines[i];
                        float curveMax = 0.01f;
                        foreach (var k in l.widthCurve.keys) curveMax = Mathf.Max(curveMax, k.value);
                        l.widthMultiplier = Mathf.Max(0.001f, p.boltWidth) / curveMax;
                        l.enabled = false;
                    }
                }
            }
            return b;
        }

        private Vector3 CurrentDir()
        {
            Vector3 d = owner != null ? owner.transform.TransformDirection(localDir) : localDir;
            return d.sqrMagnitude > 1e-6f ? d.normalized : (muzzle != null ? muzzle.forward : Vector3.forward);
        }

        // 2026-10-21e: chain endpoints = enemy chest (humanoid bone), not collider bounds (disabled/ragdoll hitbox colliders report empty bounds).
        private Vector3 Center(Collider c)
        {
            if (c == null) return Vector3.zero;
            MonoBehaviour e = EnemyOf(c);
            if (e != null) return ChestOf(e);
            return c.enabled && c.gameObject.activeInHierarchy ? c.bounds.center : c.transform.position;
        }

        // 2026-10-24l: center mass for chain endpoints (enemies AND TrainingDummy). Humanoid chest/spine bone, else enabled
        // non-trigger body collider bounds center, else renderer bounds center, else pivot + 1.2 m; plus Studio height offset.
        private Vector3 ChestOf(MonoBehaviour e)
        {
            if (e == null) return Vector3.zero;
            if (p == null || !p.chainUseCenterMass) return LegacyChest(e);
            return CenterMassOf(e) + Vector3.up * p.chainCenterMassHeightOffset;
        }

        private static Vector3 CenterMassOf(MonoBehaviour e)
        {
            var anim = e.GetComponentInChildren<Animator>();
            if (anim != null && anim.isHuman)
            {
                Transform t = anim.GetBoneTransform(HumanBodyBones.Chest);
                if (t == null) t = anim.GetBoneTransform(HumanBodyBones.Spine);
                if (t == null) t = anim.GetBoneTransform(HumanBodyBones.UpperChest);
                if (t != null) return t.position;
            }
            Bounds bb = default; bool has = false;
            foreach (var c in e.GetComponentsInChildren<Collider>(false))
            {
                if (c == null || !c.enabled || c.isTrigger) continue;
                Bounds cb = c.bounds; if (cb.size.sqrMagnitude < 1e-6f) continue;
                if (!has) { bb = cb; has = true; } else bb.Encapsulate(cb);
            }
            if (has) return bb.center;
            foreach (var r in e.GetComponentsInChildren<Renderer>(false))
            {
                if (r == null || !r.enabled || r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
                if (!has) { bb = r.bounds; has = true; } else bb.Encapsulate(r.bounds);
            }
            if (has) return bb.center;
            return e.transform.position + Vector3.up * 1.2f;
        }

        private static Vector3 LegacyChest(MonoBehaviour e)
        {
            var anim = e.GetComponentInChildren<Animator>();
            if (anim != null && anim.isHuman)
            {
                Transform t = anim.GetBoneTransform(HumanBodyBones.UpperChest);
                if (t == null) t = anim.GetBoneTransform(HumanBodyBones.Chest);
                if (t == null) t = anim.GetBoneTransform(HumanBodyBones.Spine);
                if (t != null) return t.position;
            }
            return e.transform.position + Vector3.up * 1.2f;
        }

        // 2026-10-21e: hit colliders are often DMHitbox colliders outside the EnemyHealth hierarchy; resolve via the damageable.
        private static MonoBehaviour EnemyOf(Collider c)
        {
            if (c == null) return null;
            // 2026-10-24k: chain targets = EnemyHealth OR TrainingDummy (test dummies were never chain targets -> "enemy NONE").
            var d = Project.Interaction.DamageableUtility.GetDamageable(c);
            if (d is EnemyHealth || d is TrainingDummy) return (MonoBehaviour)d;
            MonoBehaviour e = c.GetComponentInParent<EnemyHealth>();
            if (e != null) return e;
            return c.GetComponentInParent<TrainingDummy>();
        }

        private static bool IsDeadT(MonoBehaviour m)
        {
            if (m is EnemyHealth eh) return eh.IsDead;
            if (m is TrainingDummy td) return td.IsDead;
            return m == null;
        }

        private void Log(string msg)
        {
            if (p != null && p.chainDebugLog) Debug.Log("[Chain] " + msg);
        }

        private bool Excluded(Collider c)
        {
            if (c == null) return true;
            Transform root = c.transform.root;
            if (owner != null && root == owner.transform.root) return true;
            if (root.CompareTag("Player")) return true;
            return c.GetComponentInParent<Project.Companions.CompanionHealth>() != null;
        }

        private void LateUpdate()
        {
            if (p == null) { Destroy(gameObject); return; }
            float dt = Time.deltaTime, now = Time.time;
            float speed = Mathf.Max(1f, p.boltSpeed), maxLen = Mathf.Max(0.1f, p.boltMaxLength);
            float hold = Mathf.Max(0f, p.beamDurationSeconds), retract = Mathf.Max(0.01f, p.boltTailRetractTime);
            bool tick = p.beamDamagePerTick && p.beamTickDamageScale > 0f && now >= nextTick;
            if (tick) nextTick = now + 1f / Mathf.Max(1f, p.beamDamageTicksPerSecond);
            bool anyAlive = false;

            for (int i = 0; i < bolts.Count; i++)
            {
                Bolt b = bolts[i];
                if (b.Done) continue;
                anyAlive = true;

                // Endpoints (live): main bolt = muzzle -> aim contact; chain link = previous enemy -> next enemy.
                if (b.FromMuzzle)
                {
                    // 2026-10-21i: fixed path from the fire snapshot (Start/End/Dir/Hit set in Begin).
                }
                else
                {
                    if (b.FromCol != null) b.Start = Center(b.FromCol);
                    if (b.ToCol != null) b.End = Center(b.ToCol);
                    Vector3 d = b.End - b.Start;
                    b.Dir = d.sqrMagnitude > 1e-6f ? d.normalized : Vector3.forward;
                }

                float dist = Vector3.Distance(b.Start, b.End);
                float head, tail;
                if (b.ArriveTime < 0f)
                {
                    b.Traveled += (b.FromMuzzle ? speed : Mathf.Max(1f, speed * p.chainSpeedScale)) * dt;
                    head = Mathf.Clamp(b.Traveled, 0f, dist);
                    tail = Mathf.Max(0f, head - maxLen);
                    if (b.Traveled >= dist)
                    {
                        b.ArriveTime = now;
                        b.Arrived = tail;
                        OnArrive(b);
                    }
                }
                else
                {
                    // 2026-10-21h: on arrival the tail retracts into the contact point right away (bolt detaches from the barrel);
                    // only a short stub stays at the contact (electric ball) for beamDurationSeconds while ticks apply.
                    head = dist;
                    float t = now - b.ArriveTime;
                    float holdTail = Mathf.Min(b.Arrived, dist);
                    float k = Mathf.Clamp01(t / retract);
                    tail = Mathf.Lerp(holdTail, Mathf.Max(0f, dist - 0.05f), k);
                    // 2026-10-21j: chain links (FromMuzzle false) are full bolts too: they hold at the enemy (ball) like the lead hit segment. 21i wrongly culled them after the 0.12 s retract.
                    bool keep = !b.StreamTail && (b.HasHit || !b.FromMuzzle);
                    if (t >= (keep ? Mathf.Max(hold, retract) : retract)) { if (!b.FromMuzzle) Log("link " + b.ChainIndex + " finished after " + t.ToString("0.00") + " s (len " + dist.ToString("0.0") + ")"); Finish(b); continue; }
                    if (tick && t <= hold) Tick(b);
                }

                Vector3 a = b.Start + b.Dir * tail, h = b.Start + b.Dir * head;
                bool show = head - tail > 0.02f;
                if (b.Go != null)
                {
                    b.Go.transform.position = a;
                    for (int j = 0; j < b.Lines.Length; j++)
                    {
                        var l = b.Lines[j]; if (l == null) continue;
                        l.useWorldSpace = true; l.positionCount = 2; l.SetPosition(0, a); l.SetPosition(1, h); l.enabled = show;
                    }
                }
            }
            if (!anyAlive) Destroy(gameObject);
        }

        private void OnArrive(Bolt b)
        {
            if (b.FromMuzzle)
            {
                if (b.StreamTail || !b.HasHit) return; // 2026-10-21i: one hit (one mark) per shot, from the lead segment only
                DMCombatRangedResolver.ApplyHitOutcome(b.Hit, b.Dir, b.Damage, false, owner, ammo, weapon);
                MonoBehaviour first = EnemyOf(b.Hit.collider);
                if (first == null || !p.chainEnabled || !p.chainSkillUnlocked) Log("hit " + b.Hit.collider.name + " -> candidates 0 -> skipped: " + (first == null ? "not an enemy/dummy" : !p.chainEnabled ? "chainEnabled off" : "chainSkillUnlocked off"));
                if (first != null && p.chainEnabled && p.chainSkillUnlocked)
                {
                    used.Add(first);
                    if (p.chainMode == DMAmmoFxProfile.ChainMode.FanOutFromFirst) FanOut(first, b.Hit.collider, b.Damage);
                    else Chain(first, b.Hit.collider, b.Damage, 0);
                }
            }
            else
            {
                HitLink(b, b.Damage);
                if (b.Target != null && p.chainMode == DMAmmoFxProfile.ChainMode.SequentialNearest) Chain(b.Target, b.ToCol, b.Damage, b.ChainIndex);
            }
        }

        // 2026-10-24l: fan-out. All live targets within chainRange of the FIRST target's center mass, nearest first,
        // capped by chainMaxTargets; rank k (1-based) gets base * falloff^k; arc k starts after k * chainJumpDelay.
        private void FanOut(MonoBehaviour cur, Collider curCol, float dmg)
        {
            int max = Mathf.Max(0, p.chainMaxTargets);
            if (max == 0) { Log("hit " + cur.name + " -> candidates - -> skipped: chainMaxTargets 0"); return; }
            Vector3 from = ChestOf(cur);
            var all = new List<MonoBehaviour>(Object.FindObjectsByType<EnemyHealth>());
            all.AddRange(Object.FindObjectsByType<TrainingDummy>());
            var picks = new List<KeyValuePair<float, MonoBehaviour>>();
            int n = 0, blockedLos = 0;
            for (int i = 0; i < all.Count; i++)
            {
                MonoBehaviour e = all[i];
                if (e == null || !e.isActiveAndEnabled || IsDeadT(e) || used.Contains(e) || e == cur) continue;
                if (owner != null && e.transform.root == owner.transform.root) continue;
                Vector3 to = ChestOf(e);
                float d = Vector3.Distance(from, to);
                if (d > p.chainRange) continue;
                n++;
                if (p.chainRequiresLineOfSight && !Clear(from, to, cur, e)) { blockedLos++; Log("hit " + cur.name + " -> candidate " + e.name + " -> skipped: line of sight (" + d.ToString("0.0") + " m)"); continue; }
                picks.Add(new KeyValuePair<float, MonoBehaviour>(d, e));
            }
            picks.Sort((x, y) => x.Key.CompareTo(y.Key));
            if (picks.Count == 0) { Log("hit " + cur.name + " -> candidates " + n + " -> skipped: " + (n == 0 ? "no live target within " + p.chainRange + " m" : blockedLos + " blocked by line of sight")); return; }
            float speed = Mathf.Max(1f, p.boltSpeed * p.chainSpeedScale);
            for (int k = 0; k < picks.Count; k++)
            {
                MonoBehaviour e = picks[k].Value;
                if (k >= max) { Log("hit " + cur.name + " -> candidate " + e.name + " -> skipped: chainMaxTargets " + max + " reached"); continue; }
                Collider c = MainCollider(e);
                if (c == null) { Log("hit " + cur.name + " -> candidate " + e.name + " -> skipped: no collider"); continue; }
                used.Add(e);
                Bolt link = NewBolt(from);
                link.FromCol = curCol; link.ToCol = c; link.Target = e;
                link.Damage = dmg * Mathf.Pow(p.chainDamageFalloff, k + 1); link.ChainIndex = k + 1;
                link.Start = from; link.End = ChestOf(e);
                link.Traveled = -Mathf.Max(0f, p.chainJumpDelay) * (k + 1) * speed;
                StyleLink(link);
                bolts.Add(link);
                Log("hit " + cur.name + " -> candidates " + n + " -> jumped to " + e.name + " (fan " + (k + 1) + "/" + Mathf.Min(max, picks.Count) + ") dist=" + picks[k].Key.ToString("0.0") + " dmg=" + link.Damage.ToString("0.#"));
            }
        }

        private void Chain(MonoBehaviour cur, Collider curCol, float dmg, int index)
        {
            if (index >= Mathf.Max(0, p.chainMaxTargets)) { Log("hit " + cur.name + " -> candidates - -> skipped: chainMaxTargets " + p.chainMaxTargets + " reached"); return; }
            Vector3 from = curCol != null ? Center(curCol) : ChestOf(cur);
            // 2026-10-24i: candidates come from the live EnemyHealth set, not OverlapSphere(QueryTriggerInteraction.Ignore):
            // enemy hit volumes are mostly triggers / ragdoll colliders (disabled while alive), so the overlap often found no
            // enemy collider at all -> "no valid enemy" -> no bounce.
            MonoBehaviour best = null; Collider bestCol = null; float bestD = float.MaxValue; int n = 0, blockedLos = 0;
            var all = new List<MonoBehaviour>(Object.FindObjectsByType<EnemyHealth>());
            all.AddRange(Object.FindObjectsByType<TrainingDummy>());
            for (int i = 0; i < all.Count; i++)
            {
                MonoBehaviour e = all[i];
                if (e == null || !e.isActiveAndEnabled || IsDeadT(e) || used.Contains(e) || e == cur) continue;
                if (owner != null && e.transform.root == owner.transform.root) continue;
                Vector3 to = ChestOf(e);
                float d = Vector3.Distance(from, to);
                if (d > p.chainRange) continue;
                n++;
                if (d >= bestD) continue;
                if (p.chainRequiresLineOfSight && !Clear(from, to, cur, e)) { blockedLos++; continue; }
                Collider c = MainCollider(e);
                if (c == null) continue;
                best = e; bestCol = c; bestD = d;
            }
            if (best == null) { Log("hit " + cur.name + " -> candidates " + n + " -> skipped: " + (n == 0 ? "no live target within " + p.chainRange + " m" : blockedLos + " blocked by line of sight / no collider") + " (jump " + (index + 1) + ")"); return; }
            used.Add(best);
            Bolt link = NewBolt(from);
            link.FromCol = curCol; link.ToCol = bestCol; link.Target = best;
            link.Damage = dmg * p.chainDamageFalloff; link.ChainIndex = index + 1;
            link.Start = from; link.End = ChestOf(best);
            link.Traveled = -Mathf.Max(0f, p.chainJumpDelay) * Mathf.Max(1f, p.boltSpeed * p.chainSpeedScale);
            StyleLink(link);
            bolts.Add(link);
            Log("hit " + cur.name + " -> candidates " + n + " -> jumped to " + best.name + " (jump " + link.ChainIndex + "/" + p.chainMaxTargets + ") dist=" + bestD.ToString("0.0") + " dmg=" + link.Damage.ToString("0.#")
                + " bolt=" + (link.Go != null ? link.Go.name + " active=" + link.Go.activeInHierarchy + " lines=" + (link.Lines != null ? link.Lines.Length : 0) : "NULL (prefab " + (prefab != null ? prefab.name : "null") + ")"));
        }

        // 2026-10-24i: only world geometry blocks a jump. The old single Linecast stopped at the first collider (enemy weapon,
        // another enemy, player/companion capsule, props with rigidbodies) and rejected the jump.
        private static readonly RaycastHit[] LosHits = new RaycastHit[32];
        private bool Clear(Vector3 a, Vector3 b, MonoBehaviour from, MonoBehaviour to)
        {
            Vector3 d = b - a; float len = d.magnitude;
            if (len < 1e-3f) return true;
            int n = Physics.RaycastNonAlloc(a, d / len, LosHits, len, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = LosHits[i].collider; LosHits[i] = default;
                if (c == null) continue;
                if (EnemyOf(c) != null || Excluded(c) || c.attachedRigidbody != null || c is CharacterController) continue;
                return false;
            }
            return true;
        }

        private static Collider MainCollider(MonoBehaviour e)
        {
            Collider fallback = null;
            foreach (var c in e.GetComponentsInChildren<Collider>(false))
            {
                if (c == null || !c.enabled) continue;
                if (!c.isTrigger) return c;
                if (fallback == null) fallback = c;
            }
            return fallback;
        }

        private void StyleLink(Bolt b)
        {
            if (b.Lines == null) return;
            for (int i = 0; i < b.Lines.Length; i++)
            {
                var l = b.Lines[i]; if (l == null) continue;
                l.widthMultiplier *= Mathf.Max(0.01f, p.chainWidthScale);
                if (p.chainColor != Color.white) { l.startColor *= p.chainColor; l.endColor *= p.chainColor; }
            }
        }

        private void HitLink(Bolt b, float dmg)
        {
            if (b.ToCol == null || b.Target == null || IsDeadT(b.Target)) return;
            Vector3 to = Center(b.ToCol);
            DMCombatRangedResolver.ApplyHitOutcome(b.ToCol, to, -b.Dir, b.Dir, dmg, false, owner, ammo, weapon);
        }

        private void Tick(Bolt b)
        {
            // 2026-10-21i: ticks are damage only at the snapshot contact / linked enemy - no re-aim, no marks, no impact FX.
            float s = p.beamTickDamageScale;
            if (b.StreamTail) return;
            Collider c = b.FromMuzzle ? (b.HasHit ? b.Hit.collider : null) : b.ToCol;
            if (c == null) return;
            MonoBehaviour e = EnemyOf(c);
            if (e == null || IsDeadT(e)) return;
            Vector3 at = b.FromMuzzle ? b.Hit.point : Center(c);
            if (e is TrainingDummy) TrainingDummy.NextHitKind = TrainingDummy.HitKind.BeamTick; // 2026-10-24m
            CombatHitResolver.ApplyDirectHit(c, at, b.Dir, b.Damage * s, false, owner, ammo, rangedHitMarks: false);
            TrainingDummy.NextHitKind = TrainingDummy.HitKind.Auto;
        }

        private void Finish(Bolt b)
        {
            b.Done = true;
            if (b.Go != null) { PoolManager.Release(b.Go); b.Go = null; }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < bolts.Count; i++)
                if (bolts[i].Go != null) { PoolManager.Release(bolts[i].Go); bolts[i].Go = null; }
        }
    }
}
