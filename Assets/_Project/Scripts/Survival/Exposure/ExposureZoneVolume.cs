using System.Collections;
using System.Collections.Generic;
using Project.CameraFx;
using Project.Companions;
using Project.Core;
using UnityEngine;

namespace Project.Survival.Exposure
{
    [RequireComponent(typeof(Collider))]
    public class ExposureZoneVolume : MonoBehaviour
    {
        private const float DefaultAmbientFadeSeconds = 0.75f;

        [SerializeField] private ExposureZoneProfile profile;
        [SerializeField] private bool affectPlayer = true;
        [SerializeField] private bool affectCompanions = true;
        [SerializeField] private bool playAmbientLoop;
        [SerializeField] private float ambientVolume = 0.35f;
        [SerializeField, Min(0.05f)] private float ambientFadeSeconds = DefaultAmbientFadeSeconds;

        [Header("Camera Shake")]
        [SerializeField] private bool enableCameraShake;
        [Tooltip("Optional dedicated emitter. When empty, a Continuous emitter is created at runtime from the fields below.")]
        [SerializeField] private CameraShakeEmitter cameraShakeEmitter;
        [SerializeField] private Transform shakeProximityAnchor;
        [SerializeField, Range(0f, 1f)] private float shakeTraumaAtCenter = 0.28f;
        [SerializeField] private float shakeRadius = 0f;
        [SerializeField] private CameraShakePattern shakePattern = CameraShakePattern.Continuous;
        [SerializeField] private float shakePulseIntervalSeconds = 1.4f;
        [SerializeField] private AudioClip shakeClip;
        [SerializeField, Range(0f, 1f)] private float shakeVolume = 0.55f;
        [SerializeField] private bool loopShakeAudio = true;

        private readonly HashSet<ExposureReceiver> occupants = new HashSet<ExposureReceiver>();
        private Collider zoneCollider;
        private AudioSource ambientSource;
        private GameObject spawnedVfx;
        private float pulsePhaseTimer;
        private bool pulseActive = true;
        private bool _playerInside;
        private CameraShakeEmitter _runtimeShakeEmitter;
        private Coroutine _ambientFadeRoutine;

        public ExposureZoneProfile Profile => profile;

        public float CurrentPulseMultiplier
        {
            get
            {
                if (profile == null || profile.pulse == null || !profile.pulse.enabled)
                    return 1f;

                return pulseActive ? profile.pulse.activeIntensityMultiplier : 1f;
            }
        }

        private void Awake()
        {
            zoneCollider = GetComponent<Collider>();
            zoneCollider.isTrigger = true;
            EnsureAmbientSource();
        }

        private void OnValidate()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;

            if (profile != null && !string.IsNullOrWhiteSpace(profile.displayName))
                gameObject.name = profile.displayName;
        }

        private void OnDisable()
        {
            StopAmbientFadeRoutine();
            if (ambientSource != null && ambientSource.isPlaying)
                ambientSource.Stop();
        }

        private void Update()
        {
            if (!Application.isPlaying || profile == null)
                return;

            if (profile.pulse != null && profile.pulse.enabled)
            {
                pulsePhaseTimer -= Time.deltaTime;
                if (pulsePhaseTimer <= 0f)
                {
                    pulseActive = !pulseActive;
                    float baseDuration = pulseActive
                        ? profile.pulse.activeDurationSeconds
                        : profile.pulse.inactiveDurationSeconds;
                    float jitter = profile.pulse.timingJitter;
                    float variance = baseDuration * Random.Range(-jitter, jitter);
                    pulsePhaseTimer = Mathf.Max(0.25f, baseDuration + variance);
                }
            }

            UpdateCameraShake();
            UpdateAmbientProximityVolume();
        }

        private void OnTriggerEnter(Collider other)
        {
            ExposureReceiver receiver = ResolveReceiver(other);
            if (receiver == null)
                return;

            if (!ShouldAffect(receiver))
                return;

            occupants.Add(receiver);
            receiver.RegisterZone(this);

            if (receiver.GetComponent<ExposureController>() != null)
            {
                bool wasInside = _playerInside;
                _playerInside = true;
                if (!wasInside)
                    HandlePlayerEnteredAudioAndShake();
            }

            if (occupants.Count == 1)
                HandleFirstOccupantEntered();
        }

        private void OnTriggerExit(Collider other)
        {
            ExposureReceiver receiver = ResolveReceiver(other);
            if (receiver == null)
                return;

            if (!occupants.Remove(receiver))
                return;

            receiver.UnregisterZone(this);

            bool wasPlayer = receiver.GetComponent<ExposureController>() != null;
            if (wasPlayer)
            {
                _playerInside = StillHasPlayerOccupant();
                // Zone audio / rumble are player-facing — fade out as soon as the player leaves,
                // even if companions remain inside the volume.
                if (!_playerInside)
                    HandlePlayerLeftAudioAndShake();
            }

            if (occupants.Count == 0)
                HandleLastOccupantLeft();
        }

        public ExposureSample GetSampleForReceiver(ExposureReceiver receiver)
        {
            if (profile == null || receiver == null || !occupants.Contains(receiver))
                return default;

            return profile.BuildSample(CurrentPulseMultiplier);
        }

        private bool ShouldAffect(ExposureReceiver receiver)
        {
            if (receiver == null)
                return false;

            if (receiver.GetComponent<ExposureController>() != null)
                return affectPlayer;

            if (receiver.GetComponent<CompanionExposureResponder>() != null)
                return affectCompanions;

            return affectPlayer;
        }

        private static ExposureReceiver ResolveReceiver(Collider other)
        {
            if (other == null)
                return null;

            ExposureController controller = other.GetComponentInParent<ExposureController>();
            if (controller != null)
                return controller;

            ExposureReceiver receiver = other.GetComponentInParent<ExposureReceiver>();
            if (receiver != null)
                return receiver;

            PioneerCompanionAgent companion = other.GetComponentInParent<PioneerCompanionAgent>();
            if (companion != null)
            {
                CompanionExposureResponder responder = companion.GetComponent<CompanionExposureResponder>();
                if (responder == null)
                    responder = companion.gameObject.AddComponent<CompanionExposureResponder>();

                return responder;
            }

            return null;
        }

        private void HandleFirstOccupantEntered()
        {
            SpawnAmbientVfx();
        }

        private void HandlePlayerEnteredAudioAndShake()
        {
            StartAmbientLoopFaded();
            CameraShakeEmitter emitter = EnsureCameraShakeEmitter();
            emitter?.Play();
        }

        private void HandlePlayerLeftAudioAndShake()
        {
            FadeOutAmbientLoopAndStop();
            if (cameraShakeEmitter != null)
                cameraShakeEmitter.StopContinuous();
            if (_runtimeShakeEmitter != null && _runtimeShakeEmitter != cameraShakeEmitter)
                _runtimeShakeEmitter.StopContinuous();
        }

        private void HandleLastOccupantLeft()
        {
            DestroyAmbientVfx();
            // Safety: ensure audio/shake are stopped if player exit was missed.
            HandlePlayerLeftAudioAndShake();
            _playerInside = false;
        }

        private void UpdateCameraShake()
        {
            if (!enableCameraShake || !_playerInside)
                return;

            CameraShakeEmitter emitter = EnsureCameraShakeEmitter();
            if (emitter == null)
                return;

            // Emitter handles Continuous/Pulse; ensure it stays running while the player is inside.
            if (!emitter.IsRunning)
                emitter.Play();
        }

        /// <summary>
        /// While occupied, keep ambient volume in sync with proximity to zone center
        /// (same falloff idea as shake), without fighting an active fade coroutine.
        /// </summary>
        private void UpdateAmbientProximityVolume()
        {
            if (!playAmbientLoop || ambientSource == null || !ambientSource.isPlaying)
                return;

            if (_ambientFadeRoutine != null || occupants.Count == 0)
                return;

            ambientSource.volume = EvaluateAmbientTargetVolume();
        }

        private float EvaluateAmbientTargetVolume()
        {
            float proximity = 1f;
            CameraShakeEmitter emitter = cameraShakeEmitter != null ? cameraShakeEmitter : _runtimeShakeEmitter;
            if (emitter != null)
                proximity = emitter.EvaluateProximityIntensity();
            else
                proximity = EvaluateSimpleProximity();

            float scale = ambientVolume * Mathf.Clamp01(proximity);
            scale *= GameSettings.SfxVolume * GameSettings.MasterVolume;
            return Mathf.Clamp01(scale);
        }

        private float EvaluateSimpleProximity()
        {
            float radius = EstimateZoneRadius();
            if (radius <= 0.01f)
                return 1f;

            Vector3 sample = ResolveListenerSamplePoint();
            float distance = Vector3.Distance(sample, transform.position);
            if (distance >= radius)
                return 0f;

            float t = 1f - (distance / radius);
            return t * t;
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

        private CameraShakeEmitter EnsureCameraShakeEmitter()
        {
            if (!enableCameraShake)
                return null;

            // Scene often wires a disabled CameraShake_Emitter prefab instance — that cannot
            // Play / StartCoroutine and produces console errors. Prefer an active zone-local emitter.
            if (cameraShakeEmitter != null && IsUsableEmitter(cameraShakeEmitter))
            {
                ApplyShakeSettings(cameraShakeEmitter);
                return cameraShakeEmitter;
            }

            if (_runtimeShakeEmitter != null && IsUsableEmitter(_runtimeShakeEmitter))
            {
                ApplyShakeSettings(_runtimeShakeEmitter);
                return _runtimeShakeEmitter;
            }

            _runtimeShakeEmitter = GetComponent<CameraShakeEmitter>();
            if (_runtimeShakeEmitter == null || !IsUsableEmitter(_runtimeShakeEmitter))
            {
                // Dedicated child keeps shake SFX at zone world position and avoids fighting AmbientAudio.
                Transform existing = transform.Find("CameraShakeAudio");
                GameObject host = existing != null ? existing.gameObject : null;
                if (host == null)
                {
                    host = new GameObject("CameraShakeAudio");
                    host.transform.SetParent(transform, false);
                    host.transform.localPosition = Vector3.zero;
                }

                _runtimeShakeEmitter = host.GetComponent<CameraShakeEmitter>();
                if (_runtimeShakeEmitter == null)
                    _runtimeShakeEmitter = host.AddComponent<CameraShakeEmitter>();
            }

            ApplyShakeSettings(_runtimeShakeEmitter);
            return _runtimeShakeEmitter;
        }

        private static bool IsUsableEmitter(CameraShakeEmitter emitter)
        {
            return emitter != null &&
                   emitter.isActiveAndEnabled &&
                   emitter.gameObject.activeInHierarchy;
        }

        private void ApplyShakeSettings(CameraShakeEmitter emitter)
        {
            if (emitter == null)
                return;

            float radius = shakeRadius > 0.01f
                ? shakeRadius
                : EstimateZoneRadius();

            emitter.Configure(
                shakeTraumaAtCenter,
                radius,
                CameraShakeEmitterMode.Manual,
                shakePattern);

            emitter.SetProximityAnchor(shakeProximityAnchor != null ? shakeProximityAnchor : transform);
            emitter.SetProximityFalloff(true, 2f);
            emitter.SetPulseInterval(shakePulseIntervalSeconds);
            emitter.SetLoopAudioWhileContinuous(loopShakeAudio && shakeClip != null);
            emitter.SetAudioFadeSeconds(ambientFadeSeconds);
            if (shakeClip != null)
                emitter.SetClip(shakeClip, shakeVolume);
        }

        private float EstimateZoneRadius()
        {
            if (zoneCollider == null)
                zoneCollider = GetComponent<Collider>();

            if (zoneCollider == null)
                return 20f;

            Vector3 extents = zoneCollider.bounds.extents;
            return Mathf.Max(extents.x, extents.y, extents.z);
        }

        private bool StillHasPlayerOccupant()
        {
            foreach (ExposureReceiver occupant in occupants)
            {
                if (occupant != null && occupant.GetComponent<ExposureController>() != null)
                    return true;
            }

            return false;
        }

        private void SpawnAmbientVfx()
        {
            if (profile == null || profile.ambientVfxPrefab == null || spawnedVfx != null)
                return;

            spawnedVfx = Instantiate(profile.ambientVfxPrefab, transform);
            spawnedVfx.transform.localPosition = Vector3.zero;
        }

        private void DestroyAmbientVfx()
        {
            if (spawnedVfx == null)
                return;

            if (Application.isPlaying)
                Destroy(spawnedVfx);
            else
                DestroyImmediate(spawnedVfx);

            spawnedVfx = null;
        }

        private void EnsureAmbientSource()
        {
            if (!playAmbientLoop)
                return;

            // Dedicated child source so zone ambient never fights CameraShakeEmitter's AudioSource.
            if (ambientSource == null)
            {
                Transform existing = transform.Find("AmbientAudio");
                if (existing != null)
                    ambientSource = existing.GetComponent<AudioSource>();

                if (ambientSource == null)
                {
                    GameObject go = new GameObject("AmbientAudio");
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = Vector3.zero;
                    ambientSource = go.AddComponent<AudioSource>();
                }
            }

            // World-space at zone / proximity anchor — not under camera.
            ambientSource.transform.SetParent(transform, false);
            if (shakeProximityAnchor != null)
                ambientSource.transform.position = shakeProximityAnchor.position;
            else
                ambientSource.transform.localPosition = Vector3.zero;

            ambientSource.loop = true;
            ambientSource.playOnAwake = false;
            float radius = EstimateZoneRadius();
            GameplayAudioUtility.ConfigureWorldSpatialSource(
                ambientSource,
                minDistance: Mathf.Max(2f, radius * 0.15f),
                maxDistance: Mathf.Max(radius * 2.5f, 30f));
            ambientSource.volume = 0f;
        }

        private void StartAmbientLoopFaded()
        {
            if (!playAmbientLoop || profile == null || profile.ambientLoopClip == null)
                return;

            EnsureAmbientSource();
            if (ambientSource == null)
                return;

            StopAmbientFadeRoutine();

            ambientSource.clip = profile.ambientLoopClip;
            ambientSource.loop = true;
            if (!GameplayAudioUtility.CanPlaySpatialSource(ambientSource))
                return;

            float target = EvaluateAmbientTargetVolume();
            float from = ambientSource.isPlaying ? ambientSource.volume : 0f;
            if (!ambientSource.isPlaying)
            {
                ambientSource.volume = 0f;
                ambientSource.Play();
            }

            _ambientFadeRoutine = StartCoroutine(FadeAmbientVolume(from, target, ambientFadeSeconds, stopWhenDone: false));
        }

        private void FadeOutAmbientLoopAndStop()
        {
            if (ambientSource == null || (!ambientSource.isPlaying && ambientSource.volume <= 0.001f))
            {
                StopAmbientImmediate();
                return;
            }

            StopAmbientFadeRoutine();
            float from = ambientSource.volume;
            _ambientFadeRoutine = StartCoroutine(FadeAmbientVolume(from, 0f, ambientFadeSeconds, stopWhenDone: true));
        }

        private IEnumerator FadeAmbientVolume(float from, float to, float duration, bool stopWhenDone)
        {
            if (ambientSource == null)
                yield break;

            duration = Mathf.Max(0.05f, duration);
            float elapsed = 0f;
            ambientSource.volume = from;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                ambientSource.volume = Mathf.Lerp(from, to, t);
                yield return null;
            }

            ambientSource.volume = to;
            _ambientFadeRoutine = null;

            if (stopWhenDone)
                StopAmbientImmediate();
        }

        private void StopAmbientFadeRoutine()
        {
            if (_ambientFadeRoutine == null)
                return;

            StopCoroutine(_ambientFadeRoutine);
            _ambientFadeRoutine = null;
        }

        private void StopAmbientImmediate()
        {
            if (ambientSource == null)
                return;

            ambientSource.Stop();
            ambientSource.volume = 0f;
        }

        private void OnDrawGizmosSelected()
        {
            Collider col = zoneCollider != null ? zoneCollider : GetComponent<Collider>();
            if (col == null)
                return;

            Color color = profile != null ? profile.gizmoColor : new Color(0.79f, 0.18f, 0.48f, 0.35f);
            Gizmos.color = color;
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            Gizmos.color = new Color(color.r, color.g, color.b, 0.95f);
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}
