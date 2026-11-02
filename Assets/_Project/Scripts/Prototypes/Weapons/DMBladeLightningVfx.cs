// 2026-11-01c: Lightning MorphBlade VFX driver. Emits crackle clusters at random points along the blade
// (local Z = blade axis, length from the shape scale the morph controller sets): short arc sprites
// (Hovl HandPaintedLightning2, 3x3 frames), tiny sparks (Creepy Cat Lightning_01, 8x8) and a faint bolt
// flicker (FORGE3D lightning_gun_bolt_005). Tuning: DM_WeaponMorphStudioSettings "Lightning blade VFX".
using UnityEngine;

namespace Project.Prototypes.Weapons
{
    [DisallowMultipleComponent]
    public sealed class DMBladeLightningVfx : MonoBehaviour
    {
        public ParticleSystem arcs;
        public ParticleSystem sparks;
        public ParticleSystem bolt;
        [Tooltip("Blade half-thickness: arcs start this far off the axis (m).")]
        public float bladeRadius = 0.025f;

        static readonly int BoostId = Shader.PropertyToID("_Boost");
        static readonly int TintId = Shader.PropertyToID("_TintColor");
        MaterialPropertyBlock _mpb;
        float _acc, _boltAcc;

        float Length()
        {
            ParticleSystem ps = arcs != null ? arcs : GetComponentInChildren<ParticleSystem>();
            return ps != null ? Mathf.Max(0.05f, ps.shape.scale.z) : 1f;
        }

        void SetLook(ParticleSystem ps, float boost, Color c)
        {
            if (ps == null) return;
            var r = ps.GetComponent<ParticleSystemRenderer>(); if (r == null) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(_mpb); _mpb.SetFloat(BoostId, boost); _mpb.SetColor(TintId, c); r.SetPropertyBlock(_mpb);
        }

        static Vector3 Perp()
        {
            float a = Random.value * Mathf.PI * 2f;
            return new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
        }

        void Update()
        {
            var s = DM_WeaponMorphStudioSettings.Live;
            float inten = s != null ? s.lightningVfxIntensity : 1f;
            float rate = (s != null ? s.lightningArcRate : 18f) * (s != null ? s.vfxIntensity : 1f);
            float size = s != null ? s.lightningArcLength : 0.12f;
            float outSp = s != null ? s.lightningOutwardSpeed : 0.8f;
            int spk = s != null ? s.lightningSparksPerArc : 3;
            float flick = s != null ? s.lightningBoltFlicker : 0.35f;
            Color col = s != null ? s.lightningVfxColor : new Color(0.6f, 0.78f, 1f, 1f);
            SetLook(arcs, 2.5f * inten, col); SetLook(sparks, 3f * inten, col); SetLook(bolt, 1.5f * inten * flick, col);

            float len = Length(), dt = Time.deltaTime;
            // Bursty: clusters arrive randomly instead of at a steady rate.
            _acc += rate * dt * Random.Range(0f, 2f);
            int guard = 0;
            while (_acc >= 1f && guard++ < 16)
            {
                _acc -= 1f;
                float z = Random.Range(-0.5f, 0.5f) * len;
                Vector3 d = Perp();
                Vector3 p = new Vector3(0f, 0f, z) + d * bladeRadius;
                int n = Random.Range(1, 5);
                if (arcs != null)
                    for (int i = 0; i < n; i++)
                    {
                        var e = new ParticleSystem.EmitParams { applyShapeToPosition = false };
                        Vector3 dd = (d + Perp() * 0.5f + Vector3.forward * Random.Range(-0.4f, 0.4f)).normalized;
                        e.position = p + dd * Random.Range(0f, size * 0.5f);
                        e.velocity = dd * outSp * Random.Range(0.3f, 1.2f);
                        e.startSize = size * Random.Range(0.5f, 1.3f);
                        e.rotation = Random.Range(0f, 360f);
                        e.startLifetime = Random.Range(0.05f, 0.13f);
                        arcs.Emit(e, 1);
                    }
                if (sparks != null)
                    for (int i = 0; i < spk; i++)
                    {
                        var e = new ParticleSystem.EmitParams { applyShapeToPosition = false };
                        Vector3 dd = (d + Perp() * 0.8f + Vector3.forward * Random.Range(-0.6f, 0.6f)).normalized;
                        e.position = p; e.velocity = dd * Mathf.Max(0.3f, outSp) * Random.Range(1.5f, 3.5f);
                        e.startSize = Random.Range(0.01f, 0.03f); e.startLifetime = Random.Range(0.08f, 0.22f);
                        sparks.Emit(e, 1);
                    }
            }
            if (_acc > 4f) _acc = 0f;

            // Faint bolt flicker along the blade (random roll around the axis).
            if (bolt != null && flick > 0.001f)
            {
                _boltAcc += dt * 14f * Random.Range(0f, 2f);
                if (_boltAcc >= 1f)
                {
                    _boltAcc = 0f;
                    var e = new ParticleSystem.EmitParams { applyShapeToPosition = false };
                    e.position = Vector3.zero;
                    e.startSize3D = new Vector3(len * 1.05f, Random.Range(0.05f, 0.1f), 1f);
                    e.rotation3D = new Vector3(Random.Range(0f, 360f), 90f, 0f);
                    e.startLifetime = Random.Range(0.04f, 0.09f);
                    bolt.Emit(e, 1);
                }
            }
        }
    }
}