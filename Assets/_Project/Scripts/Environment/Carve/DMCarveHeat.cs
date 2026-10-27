using System.Collections.Generic;
using UnityEngine;

namespace Project.SurfaceCarve
{
    /// <summary>
    /// 2026-10-24af: hot cut faces. After a Play Mode carve the object's cut-face submeshes glow (MaterialPropertyBlock
    /// _EmissiveColor, driven by DMCarveSettings.carveGlowCurve over carveGlowCoolSeconds) and molten drips fall from the
    /// cut rim. Materials / prefabs are never modified. Tunables: Genesis Studio > Combat > Surface Damage.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class DMCarveHeat : MonoBehaviour
    {
        private static readonly int EmissiveColorId = Shader.PropertyToID("_EmissiveColor");
        private static readonly List<int> Submeshes = new List<int>();

        private struct Edge { public Vector3 point; public Vector3 normal; public float radius; public float start; }
        private readonly List<Edge> edges = new List<Edge>();
        private MaterialPropertyBlock mpb;
        private MeshRenderer mr;
        private DMCarvable carvable;
        private float heatStart = -999f;
        private float dripAccumulator;

        public static void Ignite(DMCarvable carvable, Vector3 point, Vector3 normal, float radius)
        {
            if (carvable == null || !Application.isPlaying)
                return;
            DMCarveSettings s = DMCarveSettings.Active;
            if (!(s.carveGlowEnabled && s.carveSurfaceGlowEnabled) && !s.carveDripsEnabled)
                return;
            DMCarveHeat h = carvable.GetComponent<DMCarveHeat>();
            if (h == null) h = carvable.gameObject.AddComponent<DMCarveHeat>();
            h.carvable = carvable;
            h.heatStart = Time.time;
            h.edges.Add(new Edge { point = point, normal = normal.sqrMagnitude > 1e-8f ? normal.normalized : Vector3.up, radius = radius, start = Time.time });
            if (h.edges.Count > 16) h.edges.RemoveAt(0);
            h.enabled = true;
        }

        private void Update()
        {
            DMCarveSettings s = DMCarveSettings.Active;
            float cool = Mathf.Max(0.05f, s.carveGlowCoolSeconds);
            float t = (Time.time - heatStart) / cool;
            if (mr == null) mr = GetComponent<MeshRenderer>();

            float k = t >= 1f ? 0f : Mathf.Max(0f, s.carveGlowCurve != null && s.carveGlowCurve.length > 0 ? s.carveGlowCurve.Evaluate(Mathf.Clamp01(t)) : 1f - t);
            bool surfaceGlow = s.carveGlowEnabled && s.carveSurfaceGlowEnabled;
            if (surfaceGlow || glowApplied) ApplyGlow(surfaceGlow ? k : 0f, s); // 24ah: off = never touch carved mesh blocks

            if (s.carveDripsEnabled)
                TickDrips(s, cool);

            if (t >= 1f)
            {
                edges.Clear();
                if (glowApplied) ApplyGlow(0f, s);
                enabled = false;
            }
        }

        private bool glowApplied;

        private void ApplyGlow(float k, DMCarveSettings s)
        {
            glowApplied = k > 0f;
            if (mr == null || carvable == null)
                return;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            carvable.GetFractureSubmeshes(Submeshes);
            Color c = s.carveGlowColor * (s.carveGlowIntensity * k);
            c.a = 1f;
            for (int i = 0; i < Submeshes.Count; i++)
            {
                int sub = Submeshes[i];
                if (sub < 0 || sub >= mr.sharedMaterials.Length) continue;
                Material m = mr.sharedMaterials[sub];
                if (m == null || !m.HasProperty(EmissiveColorId)) continue;
                mr.GetPropertyBlock(mpb, sub);
                if (k <= 0f) mpb.Clear(); // cooled: drop the override so SRP batching returns
                else mpb.SetColor(EmissiveColorId, c);
                mr.SetPropertyBlock(k <= 0f ? null : mpb, sub);
            }
        }

        private void TickDrips(DMCarveSettings s, float cool)
        {
            float window = cool * Mathf.Clamp01(s.carveDripWindow);
            dripAccumulator += Time.deltaTime * s.carveDripRate;
            while (dripAccumulator >= 1f)
            {
                dripAccumulator -= 1f;
                int live = 0;
                for (int i = 0; i < edges.Count; i++) if (Time.time - edges[i].start < window) live++;
                if (live == 0) { dripAccumulator = 0f; return; }
                int pick = Random.Range(0, edges.Count);
                for (int n = 0; n < edges.Count && Time.time - edges[pick].start >= window; n++) pick = (pick + 1) % edges.Count;
                Edge e = edges[pick];
                Vector3 tangent = Vector3.Cross(e.normal, Mathf.Abs(e.normal.y) > 0.95f ? Vector3.forward : Vector3.up).normalized;
                Vector3 bit = Vector3.Cross(e.normal, tangent);
                float a = Random.value * Mathf.PI * 2f;
                Vector3 rim = e.point + (tangent * Mathf.Cos(a) + bit * Mathf.Sin(a)) * e.radius + e.normal * 0.01f;
                DMCarveDrips.Spawn(rim, e.normal, s);
            }
        }

        private void OnDisable()
        {
            if (mr != null && carvable != null) ApplyGlow(0f, DMCarveSettings.Active);
        }
    }

    /// <summary>2026-10-24af: pooled molten drips (glow, fall, land, cool). Global cap DMCarveSettings.carveDripMaxLive.</summary>
    public static class DMCarveDrips
    {
        private sealed class Drip { public GameObject go; public Renderer r; public Vector3 vel; public float born, life, size; public bool landed; }
        private static readonly List<Drip> Live = new List<Drip>();
        private static readonly Stack<Drip> Free = new Stack<Drip>();
        private static readonly int UnlitColorId = Shader.PropertyToID("_UnlitColor");
        private static readonly int EmissiveColorId = Shader.PropertyToID("_EmissiveColor");
        private static Transform root;
        private static Material material;
        private static Mesh mesh;
        private static MaterialPropertyBlock mpb;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Live.Clear(); Free.Clear(); root = null; }

        private sealed class Runner : MonoBehaviour { private void Update() => Tick(); }

        public static void Spawn(Vector3 point, Vector3 normal, DMCarveSettings s)
        {
            if (!Application.isPlaying || s.carveDripMaxLive <= 0)
                return;
            if (!Ensure()) return;
            Drip d;
            if (Live.Count >= s.carveDripMaxLive)
            {
                d = Live[0]; Live.RemoveAt(0); // recycle oldest
            }
            else if (Free.Count > 0) d = Free.Pop();
            else
            {
                var go = new GameObject("CarveDrip");
                go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = material;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                d = new Drip { go = go, r = r };
            }
            d.size = Mathf.Max(0.002f, s.carveDripSize * Random.Range(0.7f, 1.3f));
            d.life = Mathf.Max(0.1f, s.carveDripLifetime * Random.Range(0.8f, 1.2f));
            d.born = Time.time;
            d.landed = false;
            d.vel = normal * Random.Range(0.05f, 0.35f);
            d.go.transform.SetPositionAndRotation(point, Quaternion.identity);
            d.go.transform.localScale = new Vector3(d.size, d.size * 1.6f, d.size);
            d.go.SetActive(true);
            Live.Add(d);
        }

        private static bool Ensure()
        {
            if (root != null && material != null) return true;
            if (material == null)
            {
                Shader sh = Shader.Find("HDRP/Unlit");
                if (sh == null) return false;
                material = new Material(sh) { name = "DM_CarveDrip_Runtime", hideFlags = HideFlags.DontSave };
                material.enableInstancing = true;
            }
            if (mesh == null)
            {
                GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mesh = tmp.GetComponent<MeshFilter>().sharedMesh;
                Object.Destroy(tmp);
            }
            if (root == null)
            {
                var r = new GameObject("DM_CarveDripPool");
                Object.DontDestroyOnLoad(r);
                r.AddComponent<Runner>();
                root = r.transform;
            }
            if (mpb == null) mpb = new MaterialPropertyBlock();
            return true;
        }

        private static void Tick()
        {
            DMCarveSettings s = DMCarveSettings.Active;
            float dt = Time.deltaTime;
            for (int i = Live.Count - 1; i >= 0; i--)
            {
                Drip d = Live[i];
                if (d.go == null) { Live.RemoveAt(i); continue; }
                float age = (Time.time - d.born) / d.life;
                if (age >= 1f) { d.go.SetActive(false); Live.RemoveAt(i); Free.Push(d); continue; }
                Transform tr = d.go.transform;
                if (!d.landed)
                {
                    d.vel += Physics.gravity * (s.carveDripGravity * dt);
                    Vector3 step = d.vel * dt;
                    if (Physics.Raycast(tr.position, step.normalized, out RaycastHit hit, step.magnitude + d.size * 0.5f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        d.landed = true;
                        tr.position = hit.point + hit.normal * (d.size * 0.15f);
                        tr.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                        tr.localScale = new Vector3(d.size * 1.8f, d.size * 0.25f, d.size * 1.8f); // splat
                    }
                    else tr.position += step;
                }
                float k = s.carveGlowCurve != null && s.carveGlowCurve.length > 0 ? Mathf.Max(0f, s.carveGlowCurve.Evaluate(age)) : 1f - age;
                Color hot = s.carveGlowColor * (s.carveDripIntensity * k);
                Color c = Color.Lerp(s.carveDripCooledColor, hot, Mathf.Clamp01(k));
                c.a = 1f;
                mpb.SetColor(UnlitColorId, c);
                mpb.SetColor(EmissiveColorId, c);
                d.r.SetPropertyBlock(mpb);
            }
        }
    }
}
