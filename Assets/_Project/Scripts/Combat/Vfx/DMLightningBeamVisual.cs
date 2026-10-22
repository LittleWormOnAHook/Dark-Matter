using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// 2026-10-21c: FORGE3D F3DLightning look (frame animation, UV scroll, jagged oscillation, muzzle/impact placement) without its
    /// raycast, hit force/damage bridge or F3DTime timers, so it runs under PoolManager and the hitscan beam path.
    /// Endpoints come from the LineRenderer (ApplyBeamLine / HitscanBeamMuzzleFollow write 2 world points; this runs after them).
    /// Settings copied from the vendor F3DLightning.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(700)]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class DMLightningBeamVisual : MonoBehaviour
    {
        public Texture[] beamFrames;
        public float frameStep = 0.035f;
        public bool randomizeFrames;
        public int points = 3;
        public float beamScale = 1f;
        public bool animateUV = true;
        public float uvTime = -3f;
        public bool oscillate = true;
        public float amplitude = 0.3f;
        public float oscillateTime = 0.05f;
        public Transform rayImpact;
        public Transform rayMuzzle;

        private LineRenderer lr;
        private Material mat;
        private Vector3 a, b;
        private bool has;
        private float frameT, oscT, uvOff;
        private int frame;
        private Vector2[] noise = new Vector2[0];

        private void Awake()
        {
            lr = GetComponent<LineRenderer>();
            uvOff = Random.Range(0f, 5f);
        }

        private void OnEnable()
        {
            has = false; frameT = 0f; oscT = 0f; frame = 0;
        }

        private void LateUpdate()
        {
            if (lr == null) return;
            if (lr.positionCount == 2) { a = lr.GetPosition(0); b = lr.GetPosition(1); has = true; }
            if (!has) return;
            if (mat == null) mat = lr.material;

            float dt = Time.deltaTime;
            float len = Vector3.Distance(a, b);
            Vector3 dir = len > 1e-4f ? (b - a) / len : transform.forward;

            if (beamFrames != null && beamFrames.Length > 1 && frameStep > 0f)
            {
                frameT -= dt;
                if (frameT <= 0f)
                {
                    frameT = frameStep;
                    frame = randomizeFrames ? Random.Range(0, beamFrames.Length) : (frame + 1) % beamFrames.Length;
                    if (beamFrames[frame] != null) mat.mainTexture = beamFrames[frame];
                }
            }
            else if (!animateUV && beamFrames != null && beamFrames.Length > 0 && beamFrames[0] != null)
                mat.mainTexture = beamFrames[0];

            // 2026-10-21d: main texture slot (HDRP/Unlit = _UnlitColorMap); was hardcoded _MainTex.
            mat.mainTextureScale = new Vector2(Mathf.Max(0.05f, len * (beamScale / 10f)), 1f);
            if (animateUV) mat.mainTextureOffset = new Vector2(Time.time * uvTime + uvOff, 0f);

            int count = oscillate && points > 0 ? Mathf.Clamp((int)((len / 10f) * points), 2, 256) : 2;
            oscT -= dt;
            if (noise.Length != count || oscT <= 0f)
            {
                oscT = Mathf.Max(0.01f, oscillateTime);
                if (noise.Length != count) noise = new Vector2[count];
                for (int i = 0; i < count; i++)
                    noise[i] = (i == 0 || i == count - 1) ? Vector2.zero : new Vector2(Random.Range(-amplitude, amplitude), Random.Range(-amplitude, amplitude));
            }

            Vector3 right = Vector3.Cross(dir, Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.95f ? Vector3.forward : Vector3.up).normalized;
            Vector3 up = Vector3.Cross(right, dir);
            lr.useWorldSpace = true;
            lr.positionCount = count;
            for (int i = 0; i < count; i++)
            {
                float t = count > 1 ? i / (float)(count - 1) : 0f;
                lr.SetPosition(i, a + dir * (len * t) + right * noise[i].x + up * noise[i].y);
            }

            if (rayImpact != null) rayImpact.position = b - dir * 0.5f;
            if (rayMuzzle != null) rayMuzzle.position = a + dir * 0.1f;
        }
    }
}