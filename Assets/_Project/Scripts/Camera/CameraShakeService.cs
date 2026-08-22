using Project.Core;
using UnityEngine;

namespace Project.CameraFx
{
    /// <summary>
    /// Trauma-based camera shake hub for explosions, impacts, and environmental events.
    /// Listeners sample via <see cref="SampleShake"/> and apply offsets in SRP camera callbacks.
    /// </summary>
    [DisallowMultipleComponent]
    public class CameraShakeService : MonoBehaviour
    {
        public static CameraShakeService Instance { get; private set; }

        [Header("Trauma")]
        [SerializeField, Range(0.05f, 4f)] private float traumaDecayPerSecond = 1.35f;
        [SerializeField, Range(0f, 2f)] private float globalIntensity = 1f;

        [Header("Amplitudes (at trauma = 1)")]
        [SerializeField] private float maxPositionOffset = 0.55f;
        [SerializeField] private float maxRotationDegrees = 7.5f;
        [SerializeField] private float shakeFrequency = 22f;
        [SerializeField] private float directionalKickScale = 0.55f;

        private float _trauma;
        private float _sustainedTrauma;
        private Vector3 _directionalKick;
        private float _seed;
        private CameraShakeListener _activeListener;

        public float Trauma => _trauma;
        public float GlobalIntensity => globalIntensity;
        public float MaxPositionOffset => maxPositionOffset;
        public float MaxRotationDegrees => maxRotationDegrees;
        public float ShakeFrequency => shakeFrequency;
        public float Seed => _seed;
        public Vector3 DirectionalKick => _directionalKick;
        public CameraShakeListener ActiveListener => _activeListener;

        public static CameraShakeService EnsureExists()
        {
            if (Instance != null)
                return Instance;

            CameraShakeService found = FindAnyObjectByType<CameraShakeService>();
            if (found != null)
            {
                // Ignore prefab assets / non-scene objects accidentally matched while editing.
                if (found.gameObject.scene.IsValid())
                {
                    Instance = found;
                    found.MakePersistentRoot();
                    return found;
                }
            }

            GameObject go = new GameObject(nameof(CameraShakeService));
            return go.AddComponent<CameraShakeService>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            MakePersistentRoot();
            _seed = Random.Range(0f, 1000f);
        }

        /// <summary>
        /// DontDestroyOnLoad only accepts root objects — unparent if this was added under a scene hierarchy.
        /// </summary>
        private void MakePersistentRoot()
        {
            if (!Application.isPlaying)
                return;

            if (transform.parent != null)
                transform.SetParent(null, true);

            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            EnsureActiveGameplayListener();

            float dt = Time.deltaTime;
            if (dt <= 0f)
            {
                // Still accept sustained trauma while paused so menu/debug probes work with unscaled systems.
                if (_sustainedTrauma > 0.0001f)
                    _trauma = Mathf.Max(_trauma, Mathf.Clamp01(_sustainedTrauma * Mathf.Max(0f, globalIntensity)));
                _sustainedTrauma = 0f;
                return;
            }

            // Sustained sources (continuous rumble) refresh each frame; decay still applies to spikes.
            if (_sustainedTrauma > 0.0001f)
                _trauma = Mathf.Max(_trauma, Mathf.Clamp01(_sustainedTrauma * Mathf.Max(0f, globalIntensity)));

            _sustainedTrauma = 0f;

            if (_trauma <= 0f && _directionalKick.sqrMagnitude < 0.00001f)
                return;

            _trauma = Mathf.Max(0f, _trauma - traumaDecayPerSecond * dt);
            _directionalKick = Vector3.Lerp(_directionalKick, Vector3.zero, 1f - Mathf.Exp(-traumaDecayPerSecond * 1.8f * dt));
        }

        /// <summary>
        /// Rebinds to the live player / main camera when the previous listener was disabled
        /// (optics handoff, duplicate inactive prefab cameras, shelter/vehicle swaps).
        /// </summary>
        private void EnsureActiveGameplayListener()
        {
            if (_activeListener != null && _activeListener.isActiveAndEnabled)
            {
                Camera listenerCamera = _activeListener.GetComponent<Camera>();
                if (listenerCamera != null && listenerCamera.enabled && listenerCamera.gameObject.activeInHierarchy)
                    return;
            }

            Camera cam = PlayerReference.ResolveCamera();
            if (cam == null)
                cam = Camera.main;
            if (cam == null)
                return;

            CameraShakeListener.EnsureOn(cam);
        }

        public void SetActiveListener(CameraShakeListener listener)
        {
            if (_activeListener != null && _activeListener != listener)
                _activeListener.SetActiveForShake(false);

            _activeListener = listener;
            if (_activeListener != null)
                _activeListener.SetActiveForShake(true);
        }

        public void ClearActiveListener(CameraShakeListener listener)
        {
            if (_activeListener == listener)
                _activeListener = null;
        }

        public void AddTrauma(float amount)
        {
            if (amount <= 0f)
                return;

            _trauma = Mathf.Clamp01(_trauma + amount * Mathf.Max(0f, globalIntensity));
        }

        /// <summary>
        /// Hold a trauma floor for this frame (continuous rumble). Call every Update while active.
        /// Multiple callers should pass their desired floor; the max wins for the frame.
        /// </summary>
        public void SustainTrauma(float amount)
        {
            if (amount <= 0f)
                return;

            _sustainedTrauma = Mathf.Max(_sustainedTrauma, Mathf.Clamp01(amount));
        }

        public void Shake(CameraShakeImpulse impulse)
        {
            float trauma = impulse.Trauma;
            if (trauma <= 0f)
                return;

            if (impulse.HasOrigin && impulse.Radius > 0.01f)
            {
                float falloff = EvaluateFalloff(impulse.Origin, impulse.Radius);
                if (falloff <= 0.001f)
                    return;

                trauma *= falloff;
            }

            AddTrauma(trauma);

            if (impulse.HasDirection)
            {
                _directionalKick += impulse.Direction.normalized * (trauma * directionalKickScale);
                if (_directionalKick.sqrMagnitude > 1f)
                    _directionalKick = _directionalKick.normalized;
            }
            else if (impulse.HasOrigin)
            {
                Vector3 samplePoint = ResolveListenerWorldPosition();
                Vector3 away = samplePoint - impulse.Origin;
                if (away.sqrMagnitude > 0.0001f)
                {
                    _directionalKick += away.normalized * (trauma * directionalKickScale * 0.65f);
                    if (_directionalKick.sqrMagnitude > 1f)
                        _directionalKick = _directionalKick.normalized;
                }
            }
        }

        public void ShakeAt(Vector3 worldPosition, float trauma, float radius)
        {
            Shake(CameraShakeImpulse.At(worldPosition, trauma, radius));
        }

        public void ShakeDirectional(Vector3 worldDirection, float trauma)
        {
            Shake(CameraShakeImpulse.Directional(worldDirection, trauma));
        }

        public void Explosion(Vector3 origin, float strength, float radius = 40f)
        {
            ShakeAt(origin, Mathf.Clamp01(strength), radius);
        }

        public void Impact(Vector3 origin, float strength, float radius = 12f)
        {
            ShakeAt(origin, Mathf.Clamp01(strength * 0.85f), radius);
        }

        public void Environmental(float trauma)
        {
            Shake(CameraShakeImpulse.Global(Mathf.Clamp01(trauma)));
        }

        /// <summary>
        /// Current shake strength (trauma squared) for listeners.
        /// </summary>
        public float GetShakeStrength()
        {
            float t = Mathf.Clamp01(_trauma);
            return t * t * Mathf.Max(0f, globalIntensity);
        }

        public void SampleShake(
            out Vector3 positionOffset,
            out Vector3 eulerOffsetDegrees)
        {
            float strength = GetShakeStrength();
            if (strength <= 0.0001f)
            {
                positionOffset = Vector3.zero;
                eulerOffsetDegrees = Vector3.zero;
                return;
            }

            float time = Time.time * shakeFrequency;
            float px = (Mathf.PerlinNoise(_seed, time) * 2f - 1f);
            float py = (Mathf.PerlinNoise(_seed + 17f, time) * 2f - 1f);
            float pz = (Mathf.PerlinNoise(_seed + 31f, time) * 2f - 1f);
            float rx = (Mathf.PerlinNoise(_seed + 47f, time) * 2f - 1f);
            float ry = (Mathf.PerlinNoise(_seed + 61f, time) * 2f - 1f);
            float rz = (Mathf.PerlinNoise(_seed + 79f, time) * 2f - 1f);

            positionOffset = new Vector3(px, py, pz) * (maxPositionOffset * strength);
            positionOffset += _directionalKick * (maxPositionOffset * strength);

            eulerOffsetDegrees = new Vector3(rx, ry, rz) * (maxRotationDegrees * strength);
            if (_directionalKick.sqrMagnitude > 0.0001f)
            {
                // Bias roll/pitch toward impact direction for readable hits.
                Vector3 kick = _directionalKick.normalized;
                eulerOffsetDegrees.z += kick.x * maxRotationDegrees * strength * 0.75f;
                eulerOffsetDegrees.x -= kick.y * maxRotationDegrees * strength * 0.5f;
            }
        }

        private float EvaluateFalloff(Vector3 origin, float radius)
        {
            Vector3 sample = ResolveListenerWorldPosition();
            float distance = Vector3.Distance(sample, origin);
            if (distance >= radius)
                return 0f;

            float t = 1f - (distance / radius);
            return t * t;
        }

        private static Vector3 ResolveListenerWorldPosition()
        {
            if (Instance != null && Instance._activeListener != null)
                return Instance._activeListener.transform.position;

            Camera cam = PlayerReference.ResolveCamera();
            if (cam != null)
                return cam.transform.position;

            Transform player = PlayerReference.ResolveTransform();
            if (player != null)
                return player.position;

            return Vector3.zero;
        }

#if UNITY_EDITOR
        [ContextMenu("Debug/Add Trauma 0.5")]
        private void DebugAddTrauma() => AddTrauma(0.5f);

        [ContextMenu("Debug/Explosion At Self")]
        private void DebugExplosionAtSelf() => Explosion(transform.position, 0.7f, 50f);
#endif
    }
}
