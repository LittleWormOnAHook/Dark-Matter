using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Combat
{
    public enum DMVolumetricDecalMode { Projector, Plane, PlaneBeyondDistance }

    /// <summary>One body type's KriptoFX volumetric fluid pool: random pick per hit, left / right lists picked by hit direction.</summary>
    [Serializable]
    public class DMVolumetricBloodSet
    {
        [Tooltip("Used for straight-on hits, and as the fallback when the side list is empty.")]
        public List<GameObject> any = new List<GameObject>();
        [Tooltip("Played when the weapon comes from the enemy's left (swing travelling toward the enemy's right).")]
        public List<GameObject> left = new List<GameObject>();
        [Tooltip("Played when the weapon comes from the enemy's right.")]
        public List<GameObject> right = new List<GameObject>();

        public bool IsEmpty => (any == null || any.Count == 0) && (left == null || left.Count == 0) && (right == null || right.Count == 0);

        /// <summary>side: -1 left, +1 right, 0 straight. Random pick inside the chosen list.</summary>
        public GameObject Pick(int side)
        {
            List<GameObject> l = side < 0 ? left : side > 0 ? right : null;
            GameObject g = RandomOf(l);
            if (g != null) return g;
            g = RandomOf(any);
            if (g != null) return g;
            return RandomOf(side >= 0 ? left : right) ?? RandomOf(side >= 0 ? right : left);
        }

        private static GameObject RandomOf(List<GameObject> l)
        {
            if (l == null || l.Count == 0) return null;
            for (int i = 0; i < 4; i++) { GameObject g = l[UnityEngine.Random.Range(0, l.Count)]; if (g != null) return g; }
            foreach (GameObject g in l) if (g != null) return g;
            return null;
        }
    }

    /// <summary>
    /// Variants live in Assets/_Project/Prefabs/Combat/VFX/BloodFX.
    /// Spawns pooled KriptoFX Volumetric Blood for enemy hits (flesh = vendor originals, Android = _Android green variants,
    /// Robot / machines = _Mech yellow variants). Lists, colours, side mapping, budget and distance live on DM_EnemyHitMarkProfile.
    /// Instances are deactivated and reused, never destroyed.
    /// </summary>
    public static class DMVolumetricBlood
    {
        private struct Live { public GameObject go; public GameObject prefab; public float until; }
        private static readonly Dictionary<GameObject, Stack<GameObject>> Pool = new Dictionary<GameObject, Stack<GameObject>>();
        private static readonly List<Live> Active = new List<Live>();
        private static Transform _root;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Pool.Clear(); Active.Clear(); _root = null; _prewarmQueue = null; _prewarmShown = null; }

        private static Queue<GameObject> _prewarmQueue;
        private static GameObject _prewarmShown, _prewarmShownPrefab;

        /// <summary>2026-10-13 hitch fix: one prefab per frame, shown one frame far below the world so assets + shaders load before the first hit.</summary>
        public static void PrewarmStep()
        {
            if (_prewarmShown != null)
            {
                _prewarmShown.SetActive(false);
                if (!Pool.TryGetValue(_prewarmShownPrefab, out var st)) Pool[_prewarmShownPrefab] = st = new Stack<GameObject>();
                st.Push(_prewarmShown);
                _prewarmShown = null;
            }
            DM_EnemyHitMarkProfile p = DM_EnemyHitMarkProfile.Live;
            if (p == null || !p.useVolumetricBlood || !p.prewarmVolumetricBlood) return;
            if (_prewarmQueue == null)
            {
                _prewarmQueue = new Queue<GameObject>();
                var seen = new HashSet<GameObject>();
                foreach (DMVolumetricBloodSet s in new[] { p.fleshVolumetric, p.androidVolumetric, p.mechVolumetric } /* Plane_ prefabs: vendor BFX_ShaderProperies needs a DecalProjector they lack (NRE), not prewarmed */)
                    if (s != null)
                        foreach (List<GameObject> l in new[] { s.any, s.left, s.right })
                            if (l != null) foreach (GameObject g in l) if (g != null && seen.Add(g)) _prewarmQueue.Enqueue(g);
            }
            if (_prewarmQueue.Count == 0) return;
            GameObject prefab = _prewarmQueue.Dequeue();
            if (Pool.TryGetValue(prefab, out var have) && have.Count > 0) return;
            GameObject go = Get(prefab);
            go.transform.position = new Vector3(0f, -5000f, 0f);
            go.SetActive(true);
            _prewarmShown = go; _prewarmShownPrefab = prefab;
        }

        public static DMVolumetricBloodSet SetFor(DM_EnemyHitMarkProfile p, DMEnemyBodyType body, Vector3 point)
        {
            if (p == null) return null;
            bool plane = p.volumetricDecalMode == DMVolumetricDecalMode.Plane
                || (p.volumetricDecalMode == DMVolumetricDecalMode.PlaneBeyondDistance && !DMEnemyHitFx.IsWithinFxDistance(point, p.volumetricPlaneLodDistance));
            if (plane)
            {
                DMVolumetricBloodSet ps = body == DMEnemyBodyType.Android ? p.androidPlaneDecal : body == DMEnemyBodyType.Robot ? p.mechPlaneDecal : p.fleshPlaneDecal;
                if (ps != null && !ps.IsEmpty) return ps;
            }
            switch (body)
            {
                case DMEnemyBodyType.Android: return p.androidVolumetric;
                case DMEnemyBodyType.Robot: return p.mechVolumetric;
                default: return p.fleshVolumetric;
            }
        }

        /// <summary>Side of the hit from the swing travel relative to the enemy: -1 left, +1 right, 0 straight.</summary>
        public static int ResolveSide(DM_EnemyHitMarkProfile p, Transform enemy, Vector3 travel)
        {
            if (enemy == null || travel.sqrMagnitude < 1e-4f) return 0;
            Vector3 t = travel; t.y = 0f; if (t.sqrMagnitude < 1e-4f) return 0;
            float s = Vector3.Dot(t.normalized, enemy.right);
            int side = s > p.volumetricSideThreshold ? -1 : s < -p.volumetricSideThreshold ? 1 : 0; // travelling toward +right = came from the left
            return p.volumetricSwapSides ? -side : side;
        }

        private static readonly RaycastHit[] GroundHits = new RaycastHit[16];
        private static readonly MaterialPropertyBlock FluidMpb = new MaterialPropertyBlock();
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static Light _sun;
        private static float _lastSpawnTime = -1f;
        private static Vector3 _lastSpawnPoint;

        /// <summary>Ground height under the hit: nearest non-trigger, non-character collider (terrain included).</summary>
        private static bool TryGround(DM_EnemyHitMarkProfile p, Vector3 point, Transform enemy, out float y)
        {
            y = 0f;
            int n = Physics.RaycastNonAlloc(point + Vector3.up * 0.2f, Vector3.down, GroundHits, p.volumetricGroundRayDistance, p.volumetricGroundMask, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                Collider c = GroundHits[i].collider;
                if (c == null || c is CharacterController) continue;
                if (enemy != null && c.transform.IsChildOf(enemy.root)) continue;
                if (!(c is TerrainCollider) && c.GetComponentInParent<Animator>() != null) continue; // characters / ragdolls
                if (GroundHits[i].distance < best) { best = GroundHits[i].distance; y = GroundHits[i].point.y; }
            }
            return best < float.MaxValue;
        }

        private static float FluidScale(DM_EnemyHitMarkProfile p)
        {
            if (!p.volumetricFluidExposureComp) return 1f;
            if (_sun == null || !_sun.isActiveAndEnabled)
            {
                _sun = RenderSettings.sun;
                if ((_sun == null || !_sun.isActiveAndEnabled) && Time.unscaledTime >= _nextSunSearch && (_nextSunSearch = Time.unscaledTime + 5f) > 0f)
                    foreach (Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
                        if (l.type == LightType.Directional && l.isActiveAndEnabled) { _sun = l; break; }
            }
            float lux = _sun != null ? _sun.intensity : 0f;
            return Mathf.Max(1f, lux / Mathf.PI * p.volumetricFluidBrightness);
        }

        private static void ApplyFluidColor(DM_EnemyHitMarkProfile p, GameObject go, DMEnemyBodyType body)
        {
            float s = FluidScale(p) * (body == DMEnemyBodyType.Android ? p.fluidGlowAndroid : body == DMEnemyBodyType.Robot ? p.fluidGlowMech : p.fluidGlowFlesh);
            foreach (MeshRenderer r in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                Material m = r.sharedMaterial;
                if (m == null || !m.HasProperty(ColorId) || !m.shader.name.EndsWith("BFX_Blood")) continue;
                r.GetPropertyBlock(FluidMpb);
                Color fc = m.GetColor(ColorId);
                float mx = Mathf.Max(fc.r, Mathf.Max(fc.g, fc.b));
                float cap = mx > p.fluidMaxChannel && mx > 0f ? p.fluidMaxChannel / mx : 1f; // hue kept, HDR copies brought to the natural range
                FluidMpb.SetColor(ColorId, new Color(fc.r * s * cap, fc.g * s * cap, fc.b * s * cap, fc.a));
                r.SetPropertyBlock(FluidMpb);
            }
        }

        private static readonly Dictionary<Transform, float> LastByTarget = new Dictionary<Transform, float>();

        public static bool TrySpawn(DM_EnemyHitMarkProfile p, DMEnemyBodyType body, Transform enemy, Vector3 point, Vector3 normal, Vector3 travel, bool playerHit = false)
        {
            if (p == null || !p.useVolumetricBlood) return false;
            Transform victimRoot = enemy;
            if (playerHit && enemy != null)
                victimRoot = enemy.root;
            DMVolumetricBloodSet set = SetFor(p, body, point);
            if (set == null || set.IsEmpty) return false;
            if (!DMEnemyHitFx.IsWithinFxDistance(point, p.volumetricMaxDistance)) return true; // 2026-10-12: out of range = handled (no legacy fallback while KriptoFX is on)
            if (Time.time - _lastSpawnTime < 0.06f && (point - _lastSpawnPoint).sqrMagnitude < 0.25f)
                return !playerHit; // player: allow second path if first was a no-op duplicate guard
            if (enemy != null)
            {
                Transform id = enemy;
                if (LastByTarget.TryGetValue(id, out float lt) && Time.time - lt < 0.05f)
                    return !playerHit;
                LastByTarget[id] = Time.time;
            }
            Sweep();
            if (Active.Count >= Mathf.Max(1, p.volumetricBudget)) RecycleOldest(); // 2026-10-12: was return false -> legacy splatter once 6 sprays were alive
            int side = ResolveSide(p, enemy, travel);
            bool forcedSide = false;
            if (side == 0 && UnityEngine.Random.value < p.volumetricStraightUsesSideChance) { side = UnityEngine.Random.value < 0.5f ? -1 : 1; forcedSide = true; } // 2026-10-13: side variants never played
            GameObject prefab = set.Pick(side);
            if (prefab == null) return false;

            // 2026-10-12: spray follows the swing (travel), demo rule atan2(normal)+180 == atan2(-normal) is the fallback.
            Vector3 dir = travel; dir.y = 0f;
            if (!p.volumetricFollowSwing || dir.sqrMagnitude < 1e-4f) { dir = -normal; dir.y = 0f; }
            if (dir.sqrMagnitude < 1e-4f) dir = enemy != null ? enemy.forward : Vector3.forward;
            float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            GameObject go = Get(prefab);
            go.transform.SetPositionAndRotation(point, Quaternion.Euler(0f, angle + p.volumetricYawOffset, 0f));
            go.transform.localScale = Vector3.one * (Mathf.Clamp(p.volumetricAirborneScale, 0.2f, 1.5f) * NextScaleMul); NextScaleMul = 1f; // 2026-10-13 (f): smaller airborne splatter (HDRP decal size is scale-invariant)

            var settings = go.GetComponent<BFX_BloodSettings>();
            if (settings != null)
            {
                settings.LightIntensityMultiplier = p.volumetricLightIntensity;
                float gy = 0f;
                bool allowGroundSnap = p.volumetricGroundSnap && (!playerHit || p.volumetricPlayerFluidGroundSnap);
                bool grounded = allowGroundSnap && TryGround(p, point, enemy, out gy);
                settings.AutomaticGroundHeightDetection = playerHit ? !p.volumetricPlayerFluidGroundSnap : !grounded;
                if (grounded) settings.GroundHeight = gy;
            }
            go.SetActive(true);
            JitterDecals(p, go, victimRoot);
            ApplyFluidColor(p, go, body);
            _lastSpawnTime = Time.time; _lastSpawnPoint = point;
            Active.Add(new Live { go = go, prefab = prefab, until = Time.time + Mathf.Max(1f, p.volumetricLifetime) });
            if (p.logVolumetricSide)
                Debug.Log("[BloodSide] target=" + (enemy != null ? enemy.name : "?") + " side=" + (side < 0 ? "Left" : side > 0 ? "Right" : "Straight") + (forcedSide ? " (random)" : "") + " prefab=" + prefab.name);
            if (DM_CombatCoreProfile.Live != null && DM_CombatCoreProfile.Live.logInterruptsInPlay)
                Debug.Log("[HitMarkMat] vol target=" + (enemy != null ? enemy.name : "?") + " body=" + body + " prefab=" + prefab.name + " side=" + side);
            return true;
        }

        private static void RecycleOldest()
        {
            int oldest = -1; float t = float.MaxValue;
            for (int i = 0; i < Active.Count; i++) if (Active[i].until < t) { t = Active[i].until; oldest = i; }
            if (oldest < 0) return;
            Live l = Active[oldest];
            Active.RemoveAt(oldest);
            if (l.go == null) return;
            l.go.SetActive(false);
            if (!Pool.TryGetValue(l.prefab, out var st)) Pool[l.prefab] = st = new Stack<GameObject>();
            st.Push(l.go);
        }

        private static void JitterDecals(DM_EnemyHitMarkProfile p, GameObject go, Transform victimRoot)
        {
            float lo = Mathf.Min(p.volumetricDecalScaleMin, p.volumetricDecalScaleMax), hi = Mathf.Max(p.volumetricDecalScaleMin, p.volumetricDecalScaleMax);
            float toward = Mathf.Clamp01(p.volumetricGroundTowardTarget);
            foreach (BFX.BFX_DecalSettings d in go.GetComponentsInChildren<BFX.BFX_DecalSettings>(true))
            {
                DMDecalJitter j = d.GetComponent<DMDecalJitter>();
                if (j == null) j = d.gameObject.AddComponent<DMDecalJitter>();
                j.Apply(p.volumetricDecalRandomRotation, UnityEngine.Random.Range(lo, hi) * Mathf.Max(0.01f, p.volumetricDecalSizeMultiplier), p.volumetricDecalDepth, p.volumetricDecalGroundOffset);
                if (toward > 0.001f && victimRoot != null)
                    j.SetGroundToward(victimRoot, toward);
            }
        }

        /// <summary>One-shot size multiplier for the next spray (heavy hits); reset after use.</summary>
        public static float NextScaleMul = 1f;

        private static float _nextSunSearch; // 2026-10-13: no-sun scenes ran FindObjectsByType<Light> on every hit

        private static GameObject Get(GameObject prefab)
        {
            if (_root == null) { var r = new GameObject("DM_VolumetricBloodPool"); UnityEngine.Object.DontDestroyOnLoad(r); _root = r.transform; }
            if (Pool.TryGetValue(prefab, out var st))
                while (st.Count > 0) { GameObject g = st.Pop(); if (g != null) return g; }
            DMHitchMonitor.Mark("blood instantiate " + prefab.name);
            GameObject inst = UnityEngine.Object.Instantiate(prefab, _root);
            inst.SetActive(false);
            return inst;
        }

        private static void Sweep()
        {
            float now = Time.time;
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                Live l = Active[i];
                if (l.go == null) { Active.RemoveAt(i); continue; }
                if (now < l.until) continue;
                l.go.SetActive(false);
                if (!Pool.TryGetValue(l.prefab, out var st)) Pool[l.prefab] = st = new Stack<GameObject>();
                st.Push(l.go);
                Active.RemoveAt(i);
            }
        }
    }
}
