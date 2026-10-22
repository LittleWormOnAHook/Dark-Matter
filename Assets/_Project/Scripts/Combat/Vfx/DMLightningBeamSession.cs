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
            public EnemyHealth Target;
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
        private readonly HashSet<EnemyHealth> used = new HashSet<EnemyHealth>();
        private static readonly Collider[] Overlap = new Collider[128];

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
        private static Vector3 Center(Collider c)
        {
            if (c == null) return Vector3.zero;
            EnemyHealth e = EnemyOf(c);
            if (e != null) return ChestOf(e);
            return c.enabled && c.gameObject.activeInHierarchy ? c.bounds.center : c.transform.position;
        }

        private static Vector3 ChestOf(EnemyHealth e)
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
        private static EnemyHealth EnemyOf(Collider c)
        {
            if (c == null) return null;
            EnemyHealth e = Project.Interaction.DamageableUtility.GetDamageable(c) as EnemyHealth;
            return e != null ? e : c.GetComponentInParent<EnemyHealth>();
        }

        private void Log(string msg)
        {
            if (p != null && p.chainDebugLog) Debug.Log("[ChainLightning] " + msg);
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
                    b.Traveled += speed * dt;
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
                EnemyHealth first = EnemyOf(b.Hit.collider);
                Log("first hit " + b.Hit.collider.name + " -> enemy " + (first != null ? first.name : "NONE (no chain)") + " chain=" + (p.chainEnabled && p.chainSkillUnlocked));
                if (first != null && p.chainEnabled && p.chainSkillUnlocked)
                {
                    used.Add(first);
                    Chain(first, b.Hit.collider, b.Damage, 0);
                }
            }
            else
            {
                HitLink(b, b.Damage);
                if (b.Target != null) Chain(b.Target, b.ToCol, b.Damage, b.ChainIndex);
            }
        }

        private void Chain(EnemyHealth cur, Collider curCol, float dmg, int index)
        {
            if (index >= Mathf.Max(0, p.chainMaxTargets)) return;
            Vector3 from = Center(curCol);
            int n = Physics.OverlapSphereNonAlloc(from, p.chainRange, Overlap, ~0, QueryTriggerInteraction.Ignore);
            EnemyHealth best = null; Collider bestCol = null; float bestD = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                Collider c = Overlap[i]; Overlap[i] = null;
                if (Excluded(c)) continue;
                EnemyHealth e = EnemyOf(c);
                if (e == null || e.IsDead || used.Contains(e)) continue;
                float d = Vector3.Distance(from, Center(c));
                if (d >= bestD || d > p.chainRange) continue;
                if (p.chainRequiresLineOfSight && !Clear(from, Center(c), cur, e)) continue;
                best = e; bestCol = c; bestD = d;
            }
            if (best == null) { Log("jump " + (index + 1) + ": no valid enemy within " + p.chainRange + " m of " + cur.name + " (" + n + " colliders)"); return; }
            used.Add(best);
            Bolt link = NewBolt(from);
            link.FromCol = curCol; link.ToCol = bestCol; link.Target = best;
            link.Damage = dmg * p.chainDamageFalloff; link.ChainIndex = index + 1;
            link.Start = from; link.End = Center(bestCol);
            bolts.Add(link);
            Log("jump " + link.ChainIndex + ": " + cur.name + " -> " + best.name + " dist=" + bestD.ToString("0.0") + " dmg=" + link.Damage.ToString("0.#")
                + " bolt=" + (link.Go != null ? link.Go.name + " active=" + link.Go.activeInHierarchy + " lines=" + (link.Lines != null ? link.Lines.Length : 0) : "NULL (prefab " + (prefab != null ? prefab.name : "null") + ")"));
        }

        private static bool Clear(Vector3 a, Vector3 b, EnemyHealth from, EnemyHealth to)
        {
            if (!Physics.Linecast(a, b, out RaycastHit h, ~0, QueryTriggerInteraction.Ignore)) return true;
            EnemyHealth e = EnemyOf(h.collider);
            return e == to || e == from;
        }

        private void HitLink(Bolt b, float dmg)
        {
            if (b.ToCol == null || b.Target == null || b.Target.IsDead) return;
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
            EnemyHealth e = EnemyOf(c);
            if (e == null || e.IsDead) return;
            Vector3 at = b.FromMuzzle ? b.Hit.point : Center(c);
            CombatHitResolver.ApplyDirectHit(c, at, b.Dir, b.Damage * s, false, owner, ammo, rangedHitMarks: false);
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
