using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Project.Features.Climb;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Features.Locomotion.EditorTools
{
    /// <summary>
    /// 2026-10-20w: edit-mode body-weight test harness. Instantiates the player prefab into a preview scene (never saved,
    /// no NavMesh, no Play), drives DMPlayerBodyWeight with synthetic kinematics at a fixed dt, records per-bone offset
    /// curves and checks them against biomechanics targets. Also a chaos pass (Extreme + randomized values, dt jitter).
    /// Output: Temp/BodyWeightHarness/report.txt + per-scenario CSV curves. Menu: Tools/Dark Matter/Body Weight Harness.
    /// </summary>
    public static class DMBodyWeightHarness
    {
        public const float Dt = 1f / 60f;
        private const string PrefabName = "Player_v7 Combat Variant";
        public static string OutDir => Path.Combine(Directory.GetCurrentDirectory(), "Temp", "BodyWeightHarness");

        public sealed class Ctx
        {
            public float Speed, Side, Vy, YawRate;
            public bool Grounded = true, Attacking, Aiming, Exert, HitStop;
            public Vector3 Omega, Input;
            public DMPlayerBodyWeight Bw;
            public Transform Root;
        }

        public sealed class Rec
        {
            public readonly List<float> T = new List<float>(), Yaw = new List<float>(), Speed = new List<float>();
            public readonly List<Vector3[]> B = new List<Vector3[]>();
            public readonly List<Vector3> Hips = new List<Vector3>(), LFoot = new List<Vector3>(), RFoot = new List<Vector3>(), Root = new List<Vector3>();
            public readonly List<Vector2> Plant = new List<Vector2>();
            public readonly List<float> PlantErr = new List<float>(), Exert = new List<float>(), PlantOver = new List<float>();
            public bool NaN; public float MaxAbs, MaxHip, MaxJump; public string JumpAt = "";
            public float Pitch(int f) => B[f][1].x + B[f][2].x;          // trunk pitch (spine + chest)
            public float TrunkYaw(int f) => B[f][1].y + B[f][2].y;
            public float Mag(int f, int i) => B[f][i].magnitude;
            public int Index(float t) { int i = T.FindIndex(x => x >= t - 1e-4f); return i < 0 ? T.Count - 1 : i; }
        }

        private sealed class Rig
        {
            public Scene Scene; public GameObject Go; public DMPlayerBodyWeight Bw; public Animator Anim;
            public Transform LFoot, RFoot;
            public readonly List<(Transform t, Vector3 p, Quaternion r)> Snap = new List<(Transform, Vector3, Quaternion)>();
        }

        private static Rig s_rig;

        private static Rig MakeRig()
        {
            string path = null;
            foreach (string g in AssetDatabase.FindAssets(PrefabName + " t:Prefab"))
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileNameWithoutExtension(p) == PrefabName) { path = p; break; }
            }
            if (path == null) throw new Exception("player prefab not found: " + PrefabName);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var r = new Rig { Scene = EditorSceneManager.NewPreviewScene() };
            r.Go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, r.Scene);
            PrefabUtility.UnpackPrefabInstance(r.Go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction); // instance only; asset untouched
            r.Go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            r.Anim = r.Go.GetComponentInChildren<Animator>();
            r.LFoot = r.Anim != null ? r.Anim.GetBoneTransform(HumanBodyBones.LeftFoot) : null;
            r.RFoot = r.Anim != null ? r.Anim.GetBoneTransform(HumanBodyBones.RightFoot) : null;
            foreach (var t in r.Go.GetComponentsInChildren<Transform>(true)) r.Snap.Add((t, t.localPosition, t.localRotation));
            return r;
        }

        private static DMPlayerBodyWeight Fresh(Rig r)
        {
            if (r.Bw != null) UnityEngine.Object.DestroyImmediate(r.Bw);
            foreach (var s in r.Snap) if (s.t != null) { s.t.localPosition = s.p; s.t.localRotation = s.r; }
            r.Go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            r.Bw = r.Go.GetComponent<DMPlayerBodyWeight>();
            if (r.Bw == null) r.Bw = r.Go.AddComponent<DMPlayerBodyWeight>();
            typeof(DMPlayerBodyWeight).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(r.Bw, null);
            r.Bw.SimActive = true;
            return r.Bw;
        }

        private static void Dispose()
        {
            DMPlayerBodyWeight.ProfileOverride = null;
            if (s_rig != null) { if (s_rig.Go != null) UnityEngine.Object.DestroyImmediate(s_rig.Go); EditorSceneManager.ClosePreviewScene(s_rig.Scene); s_rig = null; }
        }

        /// <summary>Run one scenario. step(t, ctx) sets inputs / fires events each frame.</summary>
        public static Rec Run(DM_ClimbDashProfile p, float seconds, Action<float, Ctx> step, Func<float, float> dtFn = null)
        {
            DMPlayerBodyWeight.ProfileOverride = p;
            var bw = Fresh(s_rig);
            var c = new Ctx { Bw = bw, Root = s_rig.Go.transform };
            var rec = new Rec();
            Vector3[] prev = null, prevL = new Vector3[DMPlayerBodyWeight.ProbeBoneCount];
            float t = 0f;
            while (t < seconds)
            {
                float dt = dtFn != null ? dtFn(t) : Dt;
                step(t, c);
                Transform root = c.Root;
                root.rotation = Quaternion.Euler(0f, c.YawRate * dt, 0f) * root.rotation;
                Vector3 vel = root.forward * c.Speed + root.right * c.Side + Vector3.up * c.Vy;
                root.position += vel * dt;
                bw.Sim = new DMPlayerBodyWeight.SimFrame
                {
                    Vel = vel, Grounded = c.Grounded, Attacking = c.Attacking, Aiming = c.Aiming, Exerting = c.Exert, HitStop = c.HitStop,
                    HandOmegaDeg = c.Omega, Input = c.Input.sqrMagnitude > 0f ? c.Input : (Mathf.Abs(c.Speed) > 0.1f ? new Vector3(0f, 0f, Mathf.Sign(c.Speed)) : Vector3.zero)
                };
                bw.SimStep(dt);
                t += dt;
                var b = new Vector3[DMPlayerBodyWeight.ProbeBoneCount];
                for (int i = 0; i < b.Length; i++)
                {
                    b[i] = bw.ProbeBone(i);
                    if (float.IsNaN(b[i].x) || float.IsNaN(b[i].y) || float.IsNaN(b[i].z)) rec.NaN = true;
                    else if (i < 13) rec.MaxAbs = Mathf.Max(rec.MaxAbs, b[i].magnitude);
                    Vector3 bl = bw.ProbeBoneLocal(i); float jl = Quaternion.Angle(Quaternion.AngleAxis(prevL[i].magnitude, prevL[i].sqrMagnitude > 0f ? prevL[i].normalized : Vector3.up), Quaternion.AngleAxis(bl.magnitude, bl.sqrMagnitude > 0f ? bl.normalized : Vector3.up));
                    if (prev != null && i < 13 && dt <= 0.05f && jl * Dt / dt > rec.MaxJump) { rec.MaxJump = jl * Dt / dt; rec.JumpAt = $"bone{i}@{t:0.00}s dt={dt * 1000f:0}ms"; } // own-frame rotation change, per 60 fps frame
                    prevL[i] = bl;
                }
                prev = b;
                Vector3 h = bw.ProbeHips;
                if (float.IsNaN(h.x)) rec.NaN = true; else rec.MaxHip = Mathf.Max(rec.MaxHip, h.magnitude);
                rec.T.Add(t); rec.B.Add(b); rec.Hips.Add(h); rec.Yaw.Add(root.eulerAngles.y); rec.Speed.Add(new Vector2(vel.x, vel.z).magnitude);
                rec.Root.Add(root.position);
                rec.LFoot.Add(s_rig.LFoot != null ? s_rig.LFoot.position : Vector3.zero);
                rec.RFoot.Add(s_rig.RFoot != null ? s_rig.RFoot.position : Vector3.zero);
                rec.Plant.Add(bw.ProbePlantWeight); rec.PlantErr.Add(bw.ProbePlantErr); rec.PlantOver.Add(bw.ProbePlantOver); rec.Exert.Add(bw.ProbeExert);
            }
            return rec;
        }

        // ------------------------------------------------------------ metrics
        private static float Peak(Rec r, Func<int, float> f, float t0, float t1, out float at)
        {
            float best = 0f; at = t0;
            for (int i = r.Index(t0); i < r.T.Count && r.T[i] <= t1; i++) { float v = f(i); if (Mathf.Abs(v) > Mathf.Abs(best)) { best = v; at = r.T[i]; } }
            return best;
        }

        /// <summary>Time after t0 until |f| stays below frac*|peak| (relative to the final value).</summary>
        private static float Settle(Rec r, Func<int, float> f, float t0, float peak, float frac = 0.1f, float t1 = 1e9f)
        {
            int e = Mathf.Min(r.T.Count - 1, r.Index(t1)); if (t1 >= 1e8f) e = r.T.Count - 1;
            float fin = f(e), lim = Mathf.Max(Mathf.Abs(peak - fin) * frac, 0.02f), last = t0;
            for (int i = r.Index(t0); i <= e; i++) if (Mathf.Abs(f(i) - fin) > lim) last = r.T[i];
            return last - t0;
        }

        /// <summary>Overshoots = sign flips of (f - final) after the peak with amplitude above 5% of the peak.</summary>
        private static int Overshoots(Rec r, Func<int, float> f, float tPeak, float peak)
        {
            float fin = f(r.T.Count - 1), thr = Mathf.Abs(peak - fin) * 0.05f; int n = 0, sign = Math.Sign(peak - fin); float ext = 0f;
            for (int i = r.Index(tPeak); i < r.T.Count; i++)
            {
                float v = f(i) - fin;
                if (Math.Sign(v) == -sign && Mathf.Abs(v) > thr) { if (ext == 0f) n++; ext = v; sign = -sign; }
                else if (Math.Sign(v) == sign) ext = 0f;
            }
            return n;
        }

        private static float Residual(Rec r, float t0, int maxBone = 13)
        {
            float m = 0f;
            for (int i = r.Index(t0); i < r.T.Count; i++) for (int b = 0; b < maxBone; b++) m = Mathf.Max(m, r.B[i][b].magnitude);
            return m;
        }

        private static float Cross50(Rec r, Func<int, float> f, float from, float to, float t0)
        {
            float mid = (from + to) * 0.5f;
            for (int i = r.Index(t0) + 1; i < r.T.Count; i++)
            {
                float a = f(i - 1) - mid, b = f(i) - mid;
                if (Math.Sign(a) != Math.Sign(b)) return Mathf.Lerp(r.T[i - 1], r.T[i], Mathf.Abs(a) / Mathf.Max(1e-6f, Mathf.Abs(a) + Mathf.Abs(b)));
            }
            return float.NaN;
        }

        private static void Csv(string file, Rec r)
        {
            var sb = new StringBuilder("t,speed,yaw,pelvisY,spineX,spineY,chestX,chestY,headX,headY,lUA,lLA,lHand,hipsY,hipsZ,plantL,plantR,exert\n");
            for (int i = 0; i < r.T.Count; i++)
            {
                var b = r.B[i];
                sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:0.000},{1:0.00},{2:0.0},{3:0.00},{4:0.00},{5:0.00},{6:0.00},{7:0.00},{8:0.00},{9:0.00},{10:0.00},{11:0.00},{12:0.00},{13:0.0000},{14:0.0000},{15:0.00},{16:0.00},{17:0.00}\n",
                    r.T[i], r.Speed[i], r.Yaw[i], b[0].y, b[1].x, b[1].y, b[2].x, b[2].y, b[4].x, b[4].y, b[5].magnitude, b[6].magnitude, b[7].magnitude, r.Hips[i].y, r.Hips[i].z, r.Plant[i].x, r.Plant[i].y, r.Exert[i]);
            }
            File.WriteAllText(Path.Combine(OutDir, file), sb.ToString());
        }

        // ------------------------------------------------------------ scenarios
        public sealed class Metrics : Dictionary<string, float> { }

        private static float Ramp(float t, float t0, float t1, float a, float b) => Mathf.Lerp(a, b, Mathf.InverseLerp(t0, t1, t));

        public static Metrics Evaluate(DM_ClimbDashProfile src, string tag, StringBuilder log, bool csv)
        {
            var p = UnityEngine.Object.Instantiate(src);
            p.bodyWeightEnabled = true; p.bodyWeightIntensity = Mathf.Max(0f, p.bodyWeightIntensity); p.bodyWeightDebugExaggerate = 1f;
            bool breath = p.bodyWeightBreathEnabled;
            p.bodyWeightBreathEnabled = false; // isolate: breathing is a deliberate periodic signal, tested on its own
            var m = new Metrics();
            bool anyNaN = false; float maxJump = 0f, maxAbs = 0f;
            string jumpAt = "";
            void Track(Rec r, string name = "?") { anyNaN |= r.NaN; if (r.MaxJump > maxJump) { maxJump = r.MaxJump; jumpAt = name + "/" + r.JumpAt; } maxAbs = Mathf.Max(maxAbs, r.MaxAbs); }
            try
            {
                // 1 start (0 -> 5.5 m/s in 0.35 s)
                var r = Run(p, 3f, (t, c) => { c.Speed = Ramp(t, 0.3f, 1.0f, 0f, 5.5f); }); Track(r, "start");
                m["start.lean"] = Mathf.Abs(Peak(r, r.Pitch, 0.3f, 1.5f, out _));
                m["start.armUA"] = Peak(r, i => r.Mag(i, 5), 0.3f, 1.5f, out _); m["start.armLA"] = Peak(r, i => r.Mag(i, 6), 0.3f, 1.5f, out _); m["start.armH"] = Peak(r, i => r.Mag(i, 7), 0.3f, 1.5f, out _);
                m["start.residual"] = Residual(r, 2.5f);
                if (csv) Csv(tag + "_start.csv", r);

                // 2 stop (5.5 -> 0 in 0.3 s), settle
                r = Run(p, 3.5f, (t, c) => { c.Speed = t < 1f ? 5.5f : Ramp(t, 1f, 1.5f, 5.5f, 0f); }); Track(r, "stop");
                float pk = Peak(r, r.Pitch, 1.0f, 2.2f, out float tp);
                m["stop.carry"] = Mathf.Abs(pk); m["stop.settle"] = Settle(r, r.Pitch, 1.5f, pk); m["stop.overshoots"] = Overshoots(r, r.Pitch, tp, pk);
                m["stop.residual"] = Residual(r, 3.0f);
                if (csv) Csv(tag + "_stop.csv", r);

                // 3 sharp turn: 180 deg in 0.35 s at 4 m/s; pelvis -> chest lag at 50% of the turn
                r = Run(p, 2.5f, (t, c) => { c.Speed = 4f; c.YawRate = t >= 0.5f && t < 0.85f ? 180f / 0.35f : 0f; }); Track(r, "turn");
                Func<int, float> uw = i => r.Yaw[i] > 270f ? r.Yaw[i] - 360f : r.Yaw[i];
                float tPel = Cross50(r, i => uw(i) + r.B[i][0].y, 0f, 180f, 0.4f);
                float tChe = Cross50(r, i => uw(i) + r.B[i][0].y + r.B[i][1].y + r.B[i][2].y, 0f, 180f, 0.4f);
                m["turn.lagMs"] = (tChe - tPel) * 1000f;
                m["turn.pelvisLead"] = Mathf.Abs(Peak(r, i => r.B[i][0].y, 0.5f, 1.2f, out _));
                m["turn.residual"] = Residual(r, 2.2f);
                if (csv) Csv(tag + "_turn.csv", r);

                // 4 zig-zag 1.2 Hz for 3 s, then straight
                r = Run(p, 5.5f, (t, c) => { c.Speed = 4.5f; c.YawRate = t < 3f ? 260f * Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 1.2f * t)) : 0f; }); Track(r, "zigzag");
                m["zig.peak"] = Residual(r, 0f); m["zig.residual"] = Residual(r, 4.8f);
                if (csv) Csv(tag + "_zigzag.csv", r);

                // 5 drop landings at 3 and 8 m/s
                float Drop(float vy, out float rec)
                {
                    var rr = Run(p, 2f, (t, c) => { c.Grounded = t >= 0.5f; c.Vy = t < 0.5f ? -vy : 0f; }); Track(rr, "land");
                    float pkk = Peak(rr, i => rr.Hips[i].y, 0.5f, 1.2f, out _);
                    rec = Settle(rr, i => rr.Hips[i].y, 0.5f, pkk);
                    if (csv && vy > 5f) Csv(tag + "_landing.csv", rr);
                    return Mathf.Abs(pkk);
                }
                float d3 = Drop(3f, out float r3), d8 = Drop(8f, out float r8);
                m["land.drop3cm"] = d3 * 100f; m["land.drop8cm"] = d8 * 100f; m["land.ratio"] = d3 > 1e-5f ? d8 / d3 : 0f; m["land.recover"] = Mathf.Max(r3, r8);

                // 6 melee combo: 3 swings, hit-stop at contact
                r = Run(p, 3.2f, (t, c) =>
                {
                    float u = t % 0.6f; int n = (int)(t / 0.6f);
                    c.Attacking = n < 3 && u < 0.4f;
                    float w = !c.Attacking ? 0f : u < 0.1f ? u / 0.1f : u < 0.25f ? 1f : Mathf.Max(0f, 1f - (u - 0.25f) / 0.1f);
                    c.HitStop = c.Attacking && u >= 0.2f && u < 0.27f;
                    c.Omega = c.HitStop ? Vector3.zero : new Vector3(150f, (n % 2 == 0 ? 700f : -700f), 0f) * w;
                }); Track(r, "melee");
                float ms = Peak(r, r.TrunkYaw, 0f, 1.8f, out _);
                m["melee.torso"] = Mathf.Abs(ms); m["melee.settle"] = Settle(r, r.TrunkYaw, 1.6f, ms); m["melee.residual"] = Residual(r, 3.0f);
                if (csv) Csv(tag + "_melee.csv", r);

                // 7 ranged burst: 6 shots @10 Hz, aiming
                r = Run(p, 2.5f, (t, c) => { c.Aiming = true; if (t >= 0.2f && t < 0.75f && Mathf.Repeat(t - 0.2f, 0.1f) < Dt * 0.99f) c.Bw.SimShot(); }); Track(r, "burst");
                m["burst.spine"] = Mathf.Abs(Peak(r, i => r.B[i][1].x, 0f, 1.2f, out _));
                m["burst.chestNet"] = Mathf.Abs(Peak(r, r.Pitch, 0f, 1.2f, out _));
                m["burst.residual"] = Residual(r, 2.0f);
                if (csv) Csv(tag + "_burst.csv", r);

                // 8 draw / holster
                r = Run(p, 2.8f, (t, c) => { if (Mathf.Abs(t - 0.2f) < Dt * 0.5f) c.Bw.SimDraw(1f); if (Mathf.Abs(t - 1.2f) < Dt * 0.5f) c.Bw.SimDraw(0.6f); }); Track(r, "draw");
                m["draw.peak"] = Residual(r, 0f); m["draw.residual"] = Residual(r, 2.4f);

                // 9 taking hits: front 20, right 20, back 60
                r = Run(p, 5f, (t, c) =>
                {
                    Transform rt = c.Root;
                    if (Mathf.Abs(t - 0.2f) < Dt * 0.5f) c.Bw.ApplyHit(-rt.forward, 20f, 0f);
                    if (Mathf.Abs(t - 1.7f) < Dt * 0.5f) c.Bw.ApplyHit(-rt.right, 20f, 0f);
                    if (Mathf.Abs(t - 3.2f) < Dt * 0.5f) c.Bw.ApplyHit(rt.forward, 60f, 10f);
                }); Track(r, "hits");
                float hf = Peak(r, r.Pitch, 0.2f, 1.0f, out _), hr = Peak(r, i => r.B[i][1].z + r.B[i][2].z, 1.7f, 2.5f, out _), hb = Peak(r, r.Pitch, 3.2f, 4.0f, out _);
                m["hit.front"] = hf; m["hit.side"] = hr; m["hit.back60"] = hb;
                m["hit.head"] = Mathf.Abs(Peak(r, i => r.B[i][3].x + r.B[i][4].x, 0.2f, 1.0f, out _));
                m["hit.settle"] = Settle(r, r.Pitch, 0.2f, hf, 0.1f, 1.65f); m["hit.residual"] = Residual(r, 4.6f);
                m["hit.dirOk"] = (hf < 0f && hb > 0f && hr > 0f) ? 1f : 0f; // front hit -> bend back; from the left (pushed right) -> top tilts right(-roll... see log)
                if (csv) Csv(tag + "_hits.csv", r);

                // 10 foot planting: stop, idle, slow creep
                r = Run(p, 4f, (t, c) => { c.Speed = t < 1f ? 5f : t < 1.3f ? Ramp(t, 1f, 1.3f, 5f, 0f) : t < 2.5f ? 0f : 0.6f; c.YawRate = t > 3.2f ? 90f : 0f; }); Track(r, "plant");
                float err = 0f, pop = 0f, pl = 0f, over = 0f;
                for (int i = 1; i < r.T.Count; i++)
                {
                    err = Mathf.Max(err, r.PlantErr[i]); over = Mathf.Max(over, r.PlantOver[i]); pl = Mathf.Max(pl, Mathf.Max(r.Plant[i].x, r.Plant[i].y));
                    Vector3 dl = (r.LFoot[i] - r.LFoot[i - 1]) - (r.Root[i] - r.Root[i - 1]), dr = (r.RFoot[i] - r.RFoot[i - 1]) - (r.Root[i] - r.Root[i - 1]);
                    pop = Mathf.Max(pop, Mathf.Max(dl.magnitude, dr.magnitude));
                }
                m["plant.maxW"] = pl; m["plant.ikErrCm"] = err * 100f; m["plant.popCm"] = pop * 100f; m["plant.overreachCm"] = over * 100f;

                // 11 breathing: rest 6 s, exert 6 s, recover 20 s (breathing on)
                p.bodyWeightBreathEnabled = breath;
                r = Run(p, 40f, (t, c) => { c.Exert = t >= 6f && t < 12f; c.Speed = c.Exert ? 6f : 0f; p.bodyWeightFootPlantEnabled = false; }); Track(r);
                float Rate(float a, float b)
                {
                    int n = 0; float prev = 0f; bool has = false; float mean = 0f; int cnt = 0;
                    for (int i = r.Index(a); i < r.T.Count && r.T[i] <= b; i++) { mean += r.B[i][2].x; cnt++; }
                    mean /= Mathf.Max(1, cnt);
                    for (int i = r.Index(a); i < r.T.Count && r.T[i] <= b; i++) { float v = r.B[i][2].x - mean; if (has && prev < 0f && v >= 0f) n++; prev = v; has = true; }
                    return n / (b - a);
                }
                float Depth(float a, float b) { float lo = 1e9f, hi = -1e9f; for (int i = r.Index(a); i < r.T.Count && r.T[i] <= b; i++) { lo = Mathf.Min(lo, r.Pitch(i)); hi = Mathf.Max(hi, r.Pitch(i)); } return hi - lo; }
                float FHz(float t) => p.bodyWeightBreathRate + p.bodyWeightBreathExertRate * r.Exert[r.Index(t)];
                m["breath.restHz"] = FHz(5f); m["breath.exertHz"] = FHz(12f); m["breath.lateHz"] = FHz(30f); m["breath.countHz"] = Rate(20f, 40f);
                m["breath.restDeg"] = Depth(1f, 6f); m["breath.exertDeg"] = Depth(11f, 13f);
                if (csv) Csv(tag + "_breath.csv", r);

                m["all.nan"] = anyNaN ? 1f : 0f; m["all.maxJump"] = maxJump; m["all.maxAbs"] = maxAbs;
                log?.AppendLine($"  [{tag}] max frame jump {maxJump:0.0} at {jumpAt}");
            }
            finally { UnityEngine.Object.DestroyImmediate(p); }
            return m;
        }

        // ------------------------------------------------------------ targets
        private static string Check(Metrics m, bool robustOnly = false)
        {
            var bad = new List<string>();
            if (robustOnly)
            {
                if (m["all.nan"] > 0f) bad.Add("NaN");
                if (m["all.maxJump"] > 35f) bad.Add($"frame jump {m["all.maxJump"]:0}deg");
                if (m["all.maxAbs"] > 120f) bad.Add($"bone {m["all.maxAbs"]:0}deg");
                if (m["hit.dirOk"] < 1f) bad.Add("hit direction");
                if (m["plant.popCm"] > 3f) bad.Add($"foot pop {m["plant.popCm"]:0.0}cm");
                return bad.Count == 0 ? "PASS (robustness; look targets not applied)" : "FAIL: " + string.Join("; ", bad);
            }
            void Range(string k, float lo, float hi) { if (m.TryGetValue(k, out float v) && (float.IsNaN(v) || v < lo || v > hi)) bad.Add($"{k}={v:0.###} (want {lo}-{hi})"); }
            Range("start.lean", 5f, 12f); Range("all.maxJump", 0f, 35f); Range("stop.settle", 0.3f, 0.6f); Range("stop.overshoots", 1f, 2f); Range("turn.lagMs", 50f, 120f);
            Range("land.ratio", 1.5f, 99f); Range("land.recover", 0.3f, 0.5f);
            if (!(m["start.armUA"] < m["start.armLA"] && m["start.armLA"] < m["start.armH"])) bad.Add($"arm trail not proximal<distal ({m["start.armUA"]:0.0}/{m["start.armLA"]:0.0}/{m["start.armH"]:0.0})");
            foreach (var k in new[] { "start.residual", "stop.residual", "turn.residual", "zig.residual", "melee.residual", "burst.residual", "draw.residual", "hit.residual" }) Range(k, 0f, 0.25f);
            Range("all.nan", 0f, 0f); Range("hit.dirOk", 1f, 1f); Range("plant.ikErrCm", 0f, 1f); Range("plant.popCm", 0f, 3f);
            if (m["breath.exertHz"] <= m["breath.restHz"] * 1.3f) bad.Add($"breath rate does not rise ({m["breath.restHz"]:0.00}->{m["breath.exertHz"]:0.00} Hz)");
            if (m["breath.lateHz"] > m["breath.restHz"] * 1.35f + 0.05f) bad.Add($"breath does not decay ({m["breath.lateHz"]:0.00} Hz late)");
            return bad.Count == 0 ? "PASS" : "FAIL: " + string.Join("; ", bad);
        }

        private static string Fmt(Metrics m)
        {
            var sb = new StringBuilder();
            foreach (var kv in m) sb.Append(kv.Key).Append('=').Append(kv.Value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)).Append(' ');
            return sb.ToString();
        }

        // ------------------------------------------------------------ entry points
        [MenuItem("Tools/Dark Matter/Body Weight Harness/Run Presets")]
        private static void MenuRun() => Debug.Log(RunPresets(true));

        [MenuItem("Tools/Dark Matter/Body Weight Harness/Chaos Pass")]
        private static void MenuChaos() => Debug.Log(RunChaos(40, 1234));

        /// <summary>Evaluate Subtle / Visible / Extreme (and optionally the live profile). Returns a short summary; full text in report.txt.</summary>
        public static string RunPresets(bool csv, DM_ClimbDashProfile live = null)
        {
            if (EditorApplication.isPlaying) return "skipped: in Play";
            Directory.CreateDirectory(OutDir);
            var log = new StringBuilder(); var sum = new StringBuilder();
            try
            {
                s_rig = MakeRig();
                foreach (var preset in new[] { DM_ClimbDashProfile.BodyWeightPreset.Subtle, DM_ClimbDashProfile.BodyWeightPreset.Visible, DM_ClimbDashProfile.BodyWeightPreset.Extreme })
                {
                    var p = ScriptableObject.CreateInstance<DM_ClimbDashProfile>();
                    p.ApplyBodyWeightPreset(preset);
                    var m = Evaluate(p, preset.ToString(), log, csv && preset == DM_ClimbDashProfile.BodyWeightPreset.Visible);
                    UnityEngine.Object.DestroyImmediate(p);
                    string chk = Check(m, preset == DM_ClimbDashProfile.BodyWeightPreset.Extreme);
                    log.AppendLine($"[{preset}] {chk}\n  {Fmt(m)}");
                    sum.AppendLine($"[{preset}] {chk}");
                }
                if (live != null)
                {
                    var m = Evaluate(live, "Live", log, false);
                    log.AppendLine($"[Live:{live.name}] {Check(m)}\n  {Fmt(m)}");
                    sum.AppendLine($"[Live] {Check(m)}");
                }
            }
            catch (Exception e) { sum.AppendLine("EXCEPTION " + e.GetType().Name + ": " + e.Message); log.AppendLine(e.ToString()); }
            finally { Dispose(); }
            File.WriteAllText(Path.Combine(OutDir, "report.txt"), log.ToString());
            return sum.ToString();
        }

        /// <summary>Tuning helper: preset + "field=value;field=value" overrides -> metrics + target check (one line).</summary>
        public static string Try(DM_ClimbDashProfile.BodyWeightPreset preset, string assigns, bool csv = false)
        {
            if (EditorApplication.isPlaying) return "skipped: in Play";
            Directory.CreateDirectory(OutDir);
            var p = ScriptableObject.CreateInstance<DM_ClimbDashProfile>();
            try
            {
                s_rig = MakeRig();
                p.ApplyBodyWeightPreset(preset);
                foreach (string a in (assigns ?? "").Split(';'))
                {
                    int eq = a.IndexOf('='); if (eq <= 0) continue;
                    var f = typeof(DM_ClimbDashProfile).GetField(a.Substring(0, eq).Trim());
                    if (f != null) f.SetValue(p, float.Parse(a.Substring(eq + 1), System.Globalization.CultureInfo.InvariantCulture));
                }
                var log = new StringBuilder();
                var m = Evaluate(p, "try", log, csv);
                return Check(m) + "\n" + Fmt(m) + "\n" + log;
            }
            catch (Exception e) { return e.ToString(); }
            finally { UnityEngine.Object.DestroyImmediate(p); Dispose(); }
        }

        /// <summary>Chaos: Extreme + randomized values (full Range), random inputs, dt jitter, extreme hits. Flags NaN, explosions, jitter, non-decaying motion.</summary>
        public static string RunChaos(int runs, int seed)
        {
            if (EditorApplication.isPlaying) return "skipped: in Play";
            Directory.CreateDirectory(OutDir);
            var rnd = new System.Random(seed);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            var log = new StringBuilder(); int fails = 0;
            var fields = new List<FieldInfo>();
            foreach (var f in typeof(DM_ClimbDashProfile).GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (f.Name.StartsWith("bodyWeight", StringComparison.Ordinal)) fields.Add(f);
            try
            {
                s_rig = MakeRig();
                for (int run = 0; run < runs; run++)
                {
                    var p = ScriptableObject.CreateInstance<DM_ClimbDashProfile>();
                    p.ApplyBodyWeightPreset(DM_ClimbDashProfile.BodyWeightPreset.Extreme);
                    var changed = new StringBuilder();
                    if (run > 0)
                        foreach (var f in fields)
                        {
                            var ra = (RangeAttribute)Attribute.GetCustomAttribute(f, typeof(RangeAttribute));
                            if (f.FieldType == typeof(float) && ra != null && rnd.NextDouble() < 0.6)
                            {
                                double u = rnd.NextDouble(); float v = u < 0.2 ? ra.min : u > 0.8 ? ra.max : R(ra.min, ra.max); // edges often
                                f.SetValue(p, v); changed.Append(f.Name.Substring(10)).Append('=').Append(v.ToString("0.###")).Append(' ');
                            }
                            else if (f.FieldType == typeof(bool) && f.Name != "bodyWeightEnabled" && rnd.NextDouble() < 0.15) f.SetValue(p, !(bool)f.GetValue(p));
                        }
                    p.bodyWeightEnabled = true; p.bodyWeightDebugExaggerate = run % 5 == 4 ? 10f : 1f;
                    // random input script, then 5 s idle (breathing off for the decay check)
                    float next = 0f, spd = 0f, yaw = 0f, vy = 0f; bool gr = true, atk = false, aim = false, ex = false;
                    var rr = Run(p, 13f, (t, c) =>
                    {
                        if (t < 8f && t >= next)
                        {
                            next = t + R(0.05f, 0.6f);
                            spd = R(-3f, 10f); yaw = rnd.NextDouble() < 0.4 ? R(-900f, 900f) : 0f; gr = rnd.NextDouble() > 0.2; vy = gr ? 0f : R(-20f, 5f);
                            atk = rnd.NextDouble() < 0.3; aim = rnd.NextDouble() < 0.3; ex = rnd.NextDouble() < 0.4;
                            if (rnd.NextDouble() < 0.3) c.Bw.ApplyHit(new Vector3(R(-1f, 1f), R(-1f, 1f), R(-1f, 1f)), rnd.NextDouble() < 0.2 ? 5000f : R(0f, 200f), R(0f, 100f));
                            if (rnd.NextDouble() < 0.2) for (int k = 0; k < rnd.Next(1, 8); k++) c.Bw.SimShot();
                            if (rnd.NextDouble() < 0.1) c.Bw.SimDraw(R(0f, 5f));
                        }
                        if (t >= 8f) { spd = 0f; yaw = 0f; gr = true; vy = 0f; atk = aim = ex = false; p.bodyWeightBreathEnabled = false; }
                        c.Speed = spd; c.YawRate = yaw; c.Grounded = gr; c.Vy = vy; c.Attacking = atk; c.Aiming = aim; c.Exert = ex;
                        c.HitStop = atk && rnd.NextDouble() < 0.1;
                        c.Omega = atk ? new Vector3(R(-2000f, 2000f), R(-2000f, 2000f), R(-500f, 500f)) : Vector3.zero;
                    }, t => t < 8f ? (rnd.NextDouble() < 0.1 ? R(0.0005f, 0.12f) : R(1f / 240f, 1f / 30f)) : Dt);
                    float resA = 0f, resB = 0f;
                    for (int i = rr.Index(11f); i < rr.T.Count; i++) for (int b = 0; b < 13; b++) { float v = rr.B[i][b].magnitude; if (rr.T[i] < 12f) resA = Mathf.Max(resA, v); else resB = Mathf.Max(resB, v); }
                    float hipRes = 0f, hipA = 0f; for (int i = rr.Index(11f); i < rr.T.Count; i++) { if (rr.T[i] < 12f) hipA = Mathf.Max(hipA, rr.Hips[i].magnitude); else hipRes = Mathf.Max(hipRes, rr.Hips[i].magnitude); }
                    var issues = new List<string>();
                    if (rr.NaN) issues.Add("NaN");
                    if (rr.MaxAbs > 120f) issues.Add($"explode {rr.MaxAbs:0}deg");
                    if (rr.MaxHip > 0.6f) issues.Add($"hips {rr.MaxHip:0.00}m");
                    if (rr.MaxJump > 60f) issues.Add($"jump {rr.MaxJump:0}deg/frame");
                    if (resB > 0.5f && resB > resA * 0.9f) issues.Add($"no decay {resA:0.00}->{resB:0.00}deg");
                    if (hipRes > 0.01f && hipRes > hipA * 0.9f) issues.Add($"hip drift {hipRes * 100f:0.0}cm");
                    string line = $"run {run} {(issues.Count == 0 ? "ok" : "ISSUE " + string.Join(", ", issues))} maxAbs={rr.MaxAbs:0.0} jump={rr.MaxJump:0.0} ({rr.JumpAt}) res={resB:0.00}";
                    if (issues.Count > 0) { fails++; line += "\n   values: " + changed; }
                    log.AppendLine(line);
                    UnityEngine.Object.DestroyImmediate(p);
                }
            }
            catch (Exception e) { log.AppendLine(e.ToString()); fails++; }
            finally { Dispose(); }
            File.WriteAllText(Path.Combine(OutDir, "chaos.txt"), log.ToString());
            return $"chaos runs={runs} issues={fails} (Temp/BodyWeightHarness/chaos.txt)";
        }
    }
}
