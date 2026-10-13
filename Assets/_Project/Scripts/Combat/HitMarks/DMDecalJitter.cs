using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Project.Combat
{
    /// <summary>2026-10-12: random yaw + scale for a pooled KriptoFX floor decal so landed splats don't stack aligned,
    /// plus a thin projection box hugging the ground (no projecting onto legs / boots). Added at runtime by DMVolumetricBlood;
    /// yaw at spawn, size / depth / pivot once BFX_DecalSettings has placed and sized the projector (ground + 0.05).</summary>
    [DisallowMultipleComponent]
    public sealed class DMDecalJitter : MonoBehaviour
    {
        private const float BfxGroundLift = 0.05f; // BFX_DecalSettings.InitializePosition puts the projector at ground + 0.05

        private Quaternion _baseLocal;
        private bool _hasBase;
        private float _pendingScale = 1f;
        private float _depth = -1f;
        private float _groundOffset = 0.08f;
        private bool _pending;
        private DecalProjector _decal;

        public void Apply(bool randomYaw, float scale, float depth = -1f, float groundOffset = 0.08f)
        {
            if (!_hasBase) { _baseLocal = transform.localRotation; _hasBase = true; _decal = GetComponent<DecalProjector>(); }
            Quaternion baseWorld = (transform.parent != null ? transform.parent.rotation : Quaternion.identity) * _baseLocal;
            transform.rotation = randomYaw ? Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up) * baseWorld : baseWorld;
            _pendingScale = scale;
            _depth = depth;
            _groundOffset = groundOffset;
            _pending = _decal != null;
        }

        private void LateUpdate()
        {
            if (!_pending || _decal == null || !_decal.enabled) return;
            _pending = false;
            Vector3 s = _decal.size;
            float depth = _depth > 0f ? _depth : s.z;
            _decal.size = new Vector3(s.x * _pendingScale, s.y * _pendingScale, depth);
            if (_depth > 0f)
            {
                // box spans [ground + offset - depth, ground + offset] in world Y, whatever the projector's axis
                float groundY = transform.position.y - BfxGroundLift;
                Vector3 center = new Vector3(transform.position.x, groundY + _groundOffset - depth * 0.5f, transform.position.z);
                _decal.pivot = Quaternion.Inverse(transform.rotation) * (center - transform.position);
            }
        }

        private void OnDisable() { _pending = false; }
    }
}
