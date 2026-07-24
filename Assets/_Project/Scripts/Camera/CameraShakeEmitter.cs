using Project.Core;
using Project.Player;
using System.Collections;
using UnityEngine;

namespace Project.CameraFx
{
    /// <summary>
    /// Drop on explosion / environmental prefabs to fire distance-aware camera trauma
    /// and optional SFX. Supports one-shot, continuous rumble, and pulse patterns.
    /// Shake trauma shares the same volume scale as audio (emitter volume × SFX settings × proximity).
    /// Continuous loop audio fades in on start and fades out on stop.
    /// </summary>
    [DisallowMultipleComponent]
    public class CameraShakeEmitter : MonoBehaviour
    {
        private const float DefaultAudioFadeSeconds = 0.65f;

        [Header("Activation")]
        [SerializeField] private CameraShakeEmitterMode mode = CameraShakeEmitterMode.Manual;
        [SerializeField] private CameraShakePattern pattern = CameraShakePattern.OneShot;

        [Header("Shake")]
        [SerializeField, Range(0f, 1f)] private float trauma = 0.55f;
        [SerializeField] private float radius = 40f;
        [SerializeField] private float cooldownSeconds = 0.35f;
        [SerializeField] private float pulseIntervalSeconds = 1.25f;
        [SerializeField, Range(0f, 1f)] private float pulseTraumaScale = 1f;
        [SerializeField] private LayerMask triggerLayers = ~0;
        [SerializeField] private bool requirePlayerTag = true;

        [Header("Proximity (closer = stronger)")]
        [Tooltip("Optional authored epicenter. Defaults to this transform, else trigger collider bounds center.")]
        [SerializeField] private Transform proximityAnchor;
        [SerializeField] private bool useProximityFalloff = true;
        [SerializeField, Range(0.1f, 4f)] private float proximityFalloffPower = 2f;

        [Header("Audio")]
        [SerializeField] private AudioClip playClip;
        [SerializeField] private AudioSource audioSource;
        [SerializeField, Range(0f, 1f)] private float volume = 0.85f;
        [SerializeField] private bool scaleBySfxSetting = true;
        [SerializeField] private bool loopAudioWhileContinuous;
        [SerializeField, Min(0.05f)] private float audioFadeSeconds = DefaultAudioFadeSeconds;

        private float _nextPlayTime;
        private float _nextPulseTime;
        private int _insideTriggerCount;
        private bool _running;
        private Collider _cachedCollider;
        private Coroutine _audioFadeRoutine;
        private float _lastSharedVolumeScale = 1f;

        public CameraShakePattern Pattern => pattern;
        public float Trauma => trauma;
        public float Radius => radius;
        public bool IsRunning => _running;
        public float LastSharedVolumeScale => _lastSharedVolumeScale;

        private void Awake()
        {
            _cachedCollider = GetComponent<Collider>();
            EnsureAudioSource();
        }

        private void OnEnable()
        {
            if (mode == CameraShakeEmitterMode.OnEnable)
                Play();
        }

        private void OnDisable()
        {
            StopContinuousImmediate();
            _insideTriggerCount = 0;
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;

            bool shouldRun = _running ||
                             (mode == CameraShakeEmitterMode.WhileInsideTrigger && _insideTriggerCount > 0);

            if (!shouldRun)
                return;

            float proximity = EvaluateProximityIntensity();
            float sharedScale = EvaluateSharedVolumeScale(proximity);
            _lastSharedVolumeScale = sharedScale;

            if (pattern == CameraShakePattern.Continuous)
            {
                // Shake tracks the same scale used for audible loop volume.
                if (sharedScale > 0.001f)
                    CameraShake.Sustain(trauma * sharedScale);

                if (loopAudioWhileContinuous)
                    ApplyLoopVolume(sharedScale);
                return;
            }

            if (pattern == CameraShakePattern.Pulse && Time.time >= _nextPulseTime)
            {
                _nextPulseTime = Time.time + Mathf.Max(0.05f, pulseIntervalSeconds);
                FireBurst(trauma * pulseTraumaScale, proximity, playAudio: true);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsValidTriggerOccupant(other))
                return;

            _insideTriggerCount++;

            if (mode == CameraShakeEmitterMode.OnTriggerEnter)
                Play();
            else if (mode == CameraShakeEmitterMode.WhileInsideTrigger)
                BeginPatternRuntime();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsValidTriggerOccupant(other))
                return;

            _insideTriggerCount = Mathf.Max(0, _insideTriggerCount - 1);
            if (mode == CameraShakeEmitterMode.WhileInsideTrigger && _insideTriggerCount == 0)
                StopContinuous();
        }

        /// <summary>Fire / start according to pattern (one-shot burst, or begin continuous/pulse).</summary>
        public void Play()
        {
            if (pattern == CameraShakePattern.OneShot)
            {
                if (Time.time < _nextPlayTime)
                    return;

                _nextPlayTime = Time.time + Mathf.Max(0f, cooldownSeconds);
                float proximity = EvaluateProximityIntensity();
                FireBurst(trauma, proximity, playAudio: true);
                return;
            }

            BeginPatternRuntime();
        }

        public void PlayAt(Vector3 worldPosition)
        {
            if (Time.time < _nextPlayTime && pattern == CameraShakePattern.OneShot)
                return;

            _nextPlayTime = Time.time + Mathf.Max(0f, cooldownSeconds);
            float proximity = EvaluateProximityIntensityAt(worldPosition);
            float sharedScale = EvaluateSharedVolumeScale(proximity);
            _lastSharedVolumeScale = sharedScale;
            // Radius 0 skips service falloff — proximity already applied via sharedScale.
            CameraShake.ShakeAt(worldPosition, trauma * sharedScale, 0f);
            PlayAudio(sharedScale);
        }

        public void StartContinuous()
        {
            pattern = CameraShakePattern.Continuous;
            BeginPatternRuntime();
        }

        /// <summary>Stops continuous/pulse shake and fades out looping audio.</summary>
        public void StopContinuous()
        {
            _running = false;
            FadeOutLoopAudioAndStop();
        }

        /// <summary>Hard-stops continuous runtime (no fade). Used on disable/destroy.</summary>
        public void StopContinuousImmediate()
        {
            _running = false;
            StopFadeRoutine();
            StopLoopAudioImmediate();
        }

        public void Configure(
            float traumaAmount,
            float falloffRadius,
            CameraShakeEmitterMode playMode,
            CameraShakePattern shakePattern)
        {
            trauma = Mathf.Clamp01(traumaAmount);
            radius = Mathf.Max(0f, falloffRadius);
            mode = playMode;
            pattern = shakePattern;
        }

        public void SetPulseInterval(float seconds)
        {
            pulseIntervalSeconds = Mathf.Max(0.05f, seconds);
        }

        public void SetProximityFalloff(bool enabled, float power = 2f)
        {
            useProximityFalloff = enabled;
            proximityFalloffPower = Mathf.Clamp(power, 0.1f, 4f);
        }

        public void SetClip(AudioClip clip, float volumeScale = -1f)
        {
            playClip = clip;
            if (volumeScale >= 0f)
                volume = Mathf.Clamp01(volumeScale);
        }

        public void SetLoopAudioWhileContinuous(bool enabled)
        {
            loopAudioWhileContinuous = enabled;
        }

        public void SetAudioFadeSeconds(float seconds)
        {
            audioFadeSeconds = Mathf.Max(0.05f, seconds);
        }

        public void SetProximityAnchor(Transform anchor)
        {
            proximityAnchor = anchor;
        }

        /// <summary>
        /// Shared 0–1 scale for PlayOneShot / loop volume and trauma:
        /// emitter volume × (optional SFX×Master) × proximity falloff.
        /// </summary>
        public float EvaluateSharedVolumeScale(float proximityIntensity)
        {
            float scale = volume * Mathf.Clamp01(proximityIntensity);
            if (scaleBySfxSetting)
                scale *= GameSettings.SfxVolume * GameSettings.MasterVolume;
            return Mathf.Clamp01(scale);
        }

        /// <summary>0 at/beyond radius, 1 at the proximity anchor (or emitter origin).</summary>
        public float EvaluateProximityIntensity()
        {
            return EvaluateProximityIntensityAt(ResolveListenerSamplePoint());
        }

        public float EvaluateProximityIntensityAt(Vector3 samplePoint)
        {
            if (!useProximityFalloff || radius <= 0.01f)
                return 1f;

            Vector3 origin = ResolveProximityOrigin();
            float distance = Vector3.Distance(samplePoint, origin);
            if (distance >= radius)
                return 0f;

            float t = 1f - (distance / radius);
            return Mathf.Pow(Mathf.Clamp01(t), proximityFalloffPower);
        }

        private void BeginPatternRuntime()
        {
            _running = true;
            if (pattern == CameraShakePattern.Pulse)
                _nextPulseTime = Time.time;

            float proximity = EvaluateProximityIntensity();
            float sharedScale = EvaluateSharedVolumeScale(proximity);
            _lastSharedVolumeScale = sharedScale;

            if (loopAudioWhileContinuous && pattern == CameraShakePattern.Continuous)
                StartLoopAudioFaded(sharedScale);
            else if (pattern == CameraShakePattern.Pulse)
                PlayAudio(sharedScale);
        }

        private void FireBurst(float traumaAmount, float proximity, bool playAudio)
        {
            float sharedScale = EvaluateSharedVolumeScale(proximity);
            _lastSharedVolumeScale = sharedScale;

            float shakeAmount = traumaAmount * sharedScale;
            if (shakeAmount <= 0.001f)
            {
                if (playAudio && sharedScale > 0.001f)
                    PlayAudio(sharedScale);
                return;
            }

            // Radius 0 skips service distance falloff — sharedScale already includes proximity.
            CameraShake.ShakeAt(ResolveProximityOrigin(), shakeAmount, 0f);
            if (playAudio)
                PlayAudio(sharedScale);
        }

        private Vector3 ResolveProximityOrigin()
        {
            if (proximityAnchor != null)
                return proximityAnchor.position;

            if (_cachedCollider == null)
                _cachedCollider = GetComponent<Collider>();

            if (_cachedCollider != null)
                return _cachedCollider.bounds.center;

            return transform.position;
        }

        private static Vector3 ResolveListenerSamplePoint()
        {
            Camera cam = PlayerReference.ResolveCamera();
            if (cam != null)
                return cam.transform.position;

            Transform player = PlayerReference.ResolveTransform();
            if (player != null)
                return player.position;

            return Vector3.zero;
        }

        private bool IsValidTriggerOccupant(Collider other)
        {
            if (other == null)
                return false;

            if (mode != CameraShakeEmitterMode.OnTriggerEnter &&
                mode != CameraShakeEmitterMode.WhileInsideTrigger)
                return false;

            if (((1 << other.gameObject.layer) & triggerLayers) == 0)
                return false;

            if (!requirePlayerTag)
                return true;

            return other.CompareTag("Player") ||
                   other.GetComponentInParent<PlayerController>() != null;
        }

        private void PlayAudio(float sharedVolumeScale)
        {
            if (playClip == null)
                return;

            EnsureAudioSource();
            if (!GameplayAudioUtility.CanPlaySpatialSource(audioSource))
                return;

            // OneShot volume is a multiplier; Unity still applies 3D distance/pan from this transform.
            audioSource.PlayOneShot(playClip, Mathf.Clamp01(sharedVolumeScale));
        }

        private void StartLoopAudioFaded(float targetVolume)
        {
            if (playClip == null)
                return;

            // Inactive emitters cannot StartCoroutine / Play — caller should use an active zone-local source.
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
                return;

            EnsureAudioSource();
            if (!GameplayAudioUtility.CanPlaySpatialSource(audioSource))
                return;

            StopFadeRoutine();

            audioSource.clip = playClip;
            audioSource.loop = true;
            float startVolume = audioSource.isPlaying ? audioSource.volume : 0f;
            if (!audioSource.isPlaying)
            {
                audioSource.volume = 0f;
                audioSource.Play();
            }

            _audioFadeRoutine = StartCoroutine(FadeAudioVolume(startVolume, Mathf.Clamp01(targetVolume), audioFadeSeconds, stopWhenDone: false));
        }

        private void ApplyLoopVolume(float targetVolume)
        {
            if (audioSource == null || !audioSource.loop || !audioSource.isPlaying)
                return;

            // Don't fight an active fade-in/out coroutine.
            if (_audioFadeRoutine != null)
                return;

            audioSource.volume = Mathf.Clamp01(targetVolume);
        }

        private void FadeOutLoopAudioAndStop()
        {
            if (audioSource == null || !audioSource.loop || (!audioSource.isPlaying && audioSource.volume <= 0.001f))
            {
                StopLoopAudioImmediate();
                return;
            }

            StopFadeRoutine();
            float from = audioSource.volume;
            _audioFadeRoutine = StartCoroutine(FadeAudioVolume(from, 0f, audioFadeSeconds, stopWhenDone: true));
        }

        private IEnumerator FadeAudioVolume(float from, float to, float duration, bool stopWhenDone)
        {
            if (audioSource == null)
                yield break;

            duration = Mathf.Max(0.05f, duration);
            float elapsed = 0f;
            audioSource.volume = from;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                audioSource.volume = Mathf.Lerp(from, to, t);
                yield return null;
            }

            audioSource.volume = to;
            _audioFadeRoutine = null;

            if (stopWhenDone)
                StopLoopAudioImmediate();
        }

        private void StopFadeRoutine()
        {
            if (_audioFadeRoutine == null)
                return;

            StopCoroutine(_audioFadeRoutine);
            _audioFadeRoutine = null;
        }

        private void StopLoopAudioImmediate()
        {
            if (audioSource == null)
                return;

            if (audioSource.loop || audioSource.isPlaying)
            {
                audioSource.Stop();
                audioSource.loop = false;
            }

            audioSource.volume = 0f;
        }

        private void EnsureAudioSource()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            // Keep SFX on this emitter's world transform — never steal parenting under the camera.
            audioSource.playOnAwake = false;
            if (!audioSource.loop)
                audioSource.loop = false;

            float minDist = Mathf.Max(1f, audioSource.minDistance > 0.01f ? audioSource.minDistance : 4f);
            float maxDist = Mathf.Max(radius, 25f, audioSource.maxDistance, minDist + 0.1f);
            GameplayAudioUtility.ConfigureWorldSpatialSource(audioSource, minDist, maxDist);
        }
    }
}
