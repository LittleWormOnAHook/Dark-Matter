using UnityEngine;

namespace Project.Combat
{
    /// <summary>2026-10-20g: scripts that correct the player's position call Note() so the snap probe can name them.</summary>
    public static class DMPlayerPositionCorrections
    {
        public static string LastWriter;
        public static int LastFrame = -100;
        public static void Note(string who)
        {
            LastWriter = who;
            LastFrame = Time.frameCount;
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (p != null && p.playerSnapProbeLog && p.playerSnapProbeLogEveryCorrection)
                Debug.Log("[PosCorrection] " + who + " t=" + Time.time.ToString("0.00"));
        }
    }

    /// <summary>
    /// 2026-10-20h: detects the moving player snapping BACKWARD vs. where his velocity says he should be.
    /// RB = physics body short of expected (fixed step); XFORM = rendered root short of expected (per frame, catches
    /// interpolation / SyncTransforms snaps); VISUAL = hips jumped back vs. the capsule (animation loop / root offset).
    /// A snap counts when actual travel along velocity falls short of expected by more than the threshold (default 2 cm).
    /// Self-attaches to the player (own poll). Rate-limited per kind. Toggle: DM_CombatCoreProfile.playerSnapProbeLog.
    /// </summary>
    [DefaultExecutionOrder(32000)]
    [DisallowMultipleComponent]
    public sealed class DMPlayerSnapProbe : MonoBehaviour
    {
        private const float Threshold = 0.02f;
        private const float MinSpeed = 0.5f;
        private const float RateLimit = 0.25f;
        private const float VisualThreshold = 0.005f; // 2026-10-20i: 0.5 cm for hips / model-child pops

        private Rigidbody _rb;
        private Animator _anim;
        private Transform _hips;
        private bool _hasRb, _hasX, _hasHips;
        private Vector3 _prevPos, _prevVel, _prevX, _prevHipsLocal;
        private Transform _model; private Vector3 _prevModelLocal; private bool _hasModel; private float _lastModel = -1f, _logModel = -9f;
        private float _lastRb = -1f, _lastX = -1f, _lastVis = -1f, _logRb = -9f, _logX = -9f, _logVis = -9f;

        public static void Ensure(GameObject player)
        {
            if (player != null && player.GetComponent<DMPlayerSnapProbe>() == null)
            {
                player.AddComponent<DMPlayerSnapProbe>();
                Debug.Log("[SnapProbe] attached to " + player.name + " (threshold " + Threshold * 100f + " cm)");
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var go = new GameObject("DMPlayerSnapProbeBoot") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            go.AddComponent<Boot_>();
        }

        private sealed class Boot_ : MonoBehaviour
        {
            private float _next;
            private void Update()
            {
                if (Time.unscaledTime < _next) return;
                _next = Time.unscaledTime + 1f;
                GameObject p = Project.Core.PlayerLocator.FindPlayerObject();
                if (p != null && p.activeInHierarchy && p.GetComponent<Rigidbody>() != null)
                    Ensure(p);
            }
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _anim = GetComponentInChildren<Animator>();
        }

        private static bool On { get { var p = DM_CombatCoreProfile.Live; return p == null || p.playerSnapProbeLog; } }

        private static string Writer()
        {
            return Time.frameCount - DMPlayerPositionCorrections.LastFrame <= 2 ? DMPlayerPositionCorrections.LastWriter : "unknown (no DM writer noted; suspect Physics.SyncTransforms / Invector / animation)";
        }

        private static bool Short(Vector3 moved, Vector3 vel, float dt, out float shortBy, out float speed)
        {
            vel.y = 0f; moved.y = 0f;
            speed = vel.magnitude;
            shortBy = 0f;
            if (speed < MinSpeed || dt <= 0f) return false;
            float along = Vector3.Dot(moved, vel / speed);
            shortBy = speed * dt - along;
            return shortBy > Threshold;
        }

        private void Log(string kind, float shortBy, float speed, ref float last, ref float lastLog, string extra)
        {
            float now = Time.time;
            float gap = last < 0f ? -1f : now - last;
            last = now;
            if (now - lastLog < RateLimit) return;
            lastLog = now;
            Debug.LogWarning("[SnapProbe] " + kind + " back " + shortBy.ToString("0.000") + " m @ " + speed.ToString("0.0")
                + " m/s t=" + now.ToString("0.00") + " gap=" + gap.ToString("0.00") + "s " + extra);
        }

        private void FixedUpdate()
        {
            if (!On || _rb == null) { _hasRb = false; return; }
            Vector3 pos = _rb.position;
            if (_hasRb && Short(pos - _prevPos, _prevVel, Time.fixedDeltaTime, out float sb, out float sp))
                Log("RB", sb, sp, ref _lastRb, ref _logRb, "writer=" + Writer() + " interp=" + _rb.interpolation);
            _prevPos = pos; _prevVel = _rb.linearVelocity; _hasRb = true;
        }

        private void LateUpdate()
        {
            if (!On || _rb == null) { _hasX = _hasHips = false; return; }
            Vector3 x = transform.position;
            Vector3 vel = _rb.linearVelocity;
            // Rendered root: with interpolation it should advance ~velocity*deltaTime every frame.
            if (_hasX && Time.deltaTime > 0f && Short(x - _prevX, vel, Time.deltaTime, out float sb, out float sp)
                && sb > Threshold + sp * Time.deltaTime * 0.6f) // allow uneven frame pacing; still catches a true pull-back
                Log("XFORM", sb, sp, ref _lastX, ref _logX, "writer=" + Writer() + " interp=" + _rb.interpolation);
            _prevX = x; _hasX = true;

            // 2026-10-20i: MODEL = the visual child (Animator object or "Visual") moved against travel in its parent.
            if (_model == null)
            {
                Transform v = transform.Find("Visual");
                _model = v != null ? v : (_anim != null && _anim.transform != transform ? _anim.transform : null);
            }
            if (_model != null)
            {
                Vector3 ml = _model.localPosition;
                Vector3 vl = transform.InverseTransformDirection(vel); vl.y = 0f;
                if (_hasModel && vl.magnitude > MinSpeed)
                {
                    Vector3 dm = ml - _prevModelLocal; dm.y = 0f;
                    float a = Vector3.Dot(dm, vl.normalized);
                    if (a < -VisualThreshold || dm.magnitude > 0.01f)
                        Log("MODEL", -a, vl.magnitude, ref _lastModel, ref _logModel, "child=" + _model.name + " localPos=" + ml.ToString("F3") + " d=" + dm.ToString("F3") + " writer=" + Writer());
                }
                _prevModelLocal = ml; _hasModel = true;
            }

            if (_anim == null) return;
            if (_hips == null && _anim.isHuman) _hips = _anim.GetBoneTransform(HumanBodyBones.Hips);
            if (_hips == null) return;
            Vector3 local = transform.InverseTransformPoint(_hips.position);
            Vector3 vLocal = transform.InverseTransformDirection(vel); vLocal.y = 0f;
            if (_hasHips && vLocal.magnitude > MinSpeed)
            {
                Vector3 d = local - _prevHipsLocal; d.y = 0f;
                float along = Vector3.Dot(d, vLocal.normalized);
                if (along < -VisualThreshold)
                {
                    string clip = "?";
                    var info = _anim.GetCurrentAnimatorClipInfo(0);
                    if (info != null && info.Length > 0 && info[0].clip != null)
                        clip = info[0].clip.name + " n=" + _anim.GetCurrentAnimatorStateInfo(0).normalizedTime.ToString("0.00");
                    Log("VISUAL", -along, vLocal.magnitude, ref _lastVis, ref _logVis, "(hips vs capsule) clip=" + clip + " rootMotion=" + _anim.applyRootMotion);
                }
            }
            _prevHipsLocal = local; _hasHips = true;
        }
    }
}
