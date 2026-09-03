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

        [Header("Falloff Colliders")]
        [Tooltip("Outer volume. Effect is weakest here. Sphere, box, capsule, or any collider.")]
        [SerializeField] private Collider outerCollider;
        [Tooltip("Inner volume. Effect is strongest here and inside. Sphere, box, capsule, or any collider.")]
        [SerializeField] private Collider innerCollider;
        [SerializeField, Range(0f, 1f)] private float outerIntensity = 0.12f;
        [SerializeField] private Color outerGizmoColor = new Color(0.79f, 0.18f, 0.48f, 0.22f);
        [SerializeField] private Color innerGizmoColor = new Color(0.42f, 0.07f, 0.24f, 0.50f);

        [Header("Zone Particles")]
        [Tooltip("Particle system for this zone. Starts when the player enters the outer volume and stops when they leave. Alpha fades from the inner collider (full) to the outer rim.")]
        [SerializeField] private ParticleSystem zoneParticles;
        [Tooltip("Optional prefab spawned as a child when Zone Particles is empty.")]
        [SerializeField] private GameObject zoneParticlePrefab;
        [Tooltip("When on, each particle fades with the same inner-to-outer curve as the hazard.")]
        [SerializeField] private bool fadeParticlesFromCenter = true;
        [Tooltip("At the outer rim the prefab emission rate is used as-is (e.g. 2000). Toward the inner collider that rate is multiplied exponentially by this.")]
        [SerializeField, Min(1f)] private float particleCenterEmissionMultiplier = 4f;
        [Tooltip("Fit the ParticleSystem shape to the outer collider so the field fills the zone. World simulation lets the player walk through it.")]
        [SerializeField] private bool matchParticleShapeToZone = false;

        [Header("Screen Overlay")]
        [Tooltip("When off, this zone does not show a screen-edge vignette.")]
        [SerializeField] private bool overlayEnabled = true;
        [Tooltip("Overlay alpha at the outer rim. Overrides the UITK config.")]
        [SerializeField, Range(0f, 1f)] private float overlayAlphaMin = 0.1f;
        [Tooltip("Overlay alpha at the inner / center. Overrides the UITK config.")]
        [SerializeField, Range(0f, 1f)] private float overlayAlphaMax = 0.6f;
        [Tooltip("Optional screen-edge texture for this zone. Empty uses the default for the zone kind.")]
        [SerializeField] private Texture2D overlayTexture;

        private readonly HashSet<ExposureReceiver> occupants = new HashSet<ExposureReceiver>();
        private Collider zoneCollider;
        private AudioSource ambientSource;
        private GameObject spawnedVfx;
        private GameObject spawnedZoneParticles;
        private ParticleSystem[] drivenParticleSystems;
        private ParticleSystem.Particle[] particleBuffer;
        private readonly List<Vector4> particleCustomData = new List<Vector4>(128);
        private bool particleCacheDirty = true;
        private readonly Dictionary<ParticleSystem, Vector2> authoredEmissionMul = new Dictionary<ParticleSystem, Vector2>();
        private float pulsePhaseTimer;
        private bool pulseActive = true;
        private bool _playerInside;
        private CameraShakeEmitter _runtimeShakeEmitter;
        private Coroutine _ambientFadeRoutine;

        public ExposureZoneProfile Profile => profile;
        public Collider OuterCollider => ResolveOuter();
        public Collider InnerCollider => ResolveInner();
        public bool OverlayEnabled => overlayEnabled;
        public float OverlayAlphaMin => overlayAlphaMin;
        public float OverlayAlphaMax => overlayAlphaMax;
        public Texture2D OverlayTexture => overlayTexture;

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
            CacheFalloffColliders();
            zoneCollider = ResolveOuter();
            ForceVolumeCollidersPassThrough();
            EnsureAmbientSource();
            EnsureZoneParticles();
            StopDrivenParticles(clear: true);
        }

        private void Start()
        {
            TryCatchUpPlayerInside();
        }

        private void OnEnable()
        {
            ForceVolumeCollidersPassThrough();
            if (!Application.isPlaying)
                return;

            EnsureZoneParticles();
            if (_playerInside)
                PlayDrivenParticles();
            else
                StopDrivenParticles(clear: true);
        }

        /// <summary>
        /// Hazard volumes must never physically block the player. Invector ground / snap / step
        /// casts use Default and QueriesHitTriggers is on, so a Default-layer trigger still acts like a wall.
        /// Keep them as triggers on the Triggers layer so occupancy works and movement ignores them.
        /// </summary>
        private void ForceVolumeCollidersPassThrough()
        {
            int triggerLayer = LayerMask.NameToLayer("Triggers");
            ApplyPassThrough(zoneCollider, triggerLayer);
            ApplyPassThrough(innerCollider, triggerLayer);
            ApplyPassThrough(outerCollider, triggerLayer);

            Collider[] cols = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
                ApplyPassThrough(cols[i], triggerLayer);
        }

        private static void ApplyPassThrough(Collider col, int triggerLayer)
        {
            if (col == null)
                return;

            col.isTrigger = true;
            if (triggerLayer >= 0 && col.gameObject.layer != triggerLayer)
                col.gameObject.layer = triggerLayer;
        }

        private void OnValidate()
        {
            CacheFalloffColliders();
            ForceVolumeCollidersPassThrough();
            ApplyZoneParticleShape();

            ApplyDefaultGizmoColorsIfNeeded();

            if (profile != null && !string.IsNullOrWhiteSpace(profile.displayName))
                gameObject.name = profile.displayName;
        }

        private void OnDisable()
        {
            StopAmbientFadeRoutine();
            if (ambientSource != null && ambientSource.isPlaying)
                ambientSource.Stop();
            StopDrivenParticles(clear: true);
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;

            if (profile != null && profile.pulse != null && profile.pulse.enabled)
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
            UpdateZoneParticles();
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

            if (!occupants.Contains(receiver))
                return;

            // Two triggers on one volume (inner + outer) both fire exit. Stay registered
            // until the receiver leaves the outer collider.
            if (StillOverlapsOuter(other))
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

            float spatial = EvaluateSpatialIntensity(receiver.transform.position);
            return profile.BuildSample(CurrentPulseMultiplier, spatial);
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
            PlayDrivenParticles();
        }

        private void HandlePlayerLeftAudioAndShake()
        {
            FadeOutAmbientLoopAndStop();
            if (cameraShakeEmitter != null)
                cameraShakeEmitter.StopContinuous();
            if (_runtimeShakeEmitter != null && _runtimeShakeEmitter != cameraShakeEmitter)
                _runtimeShakeEmitter.StopContinuous();
            StopDrivenParticles(clear: false);
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
            Collider outer = ResolveOuter();
            if (outer == null)
                return 20f;

            Vector3 extents = outer.bounds.extents;
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
            particleCacheDirty = true;
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
            particleCacheDirty = true;
        }


        public ParticleSystem ZoneParticles => zoneParticles;

        private void ApplyZoneParticleShape()
        {
            if (!matchParticleShapeToZone)
                return;

            RefreshDrivenParticleCache();
            ParticleSystem root = zoneParticles;
            if (root == null && drivenParticleSystems != null && drivenParticleSystems.Length > 0)
                root = drivenParticleSystems[0];
            if (root == null)
                return;

            ApplyColliderShapeToParticleSystem(root, ResolveOuter());
        }

        private static void ApplyColliderShapeToParticleSystem(ParticleSystem ps, Collider col)
        {
            if (ps == null || col == null)
                return;

            ParticleSystem.MainModule main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radiusThickness = 1f;
            shape.rotation = Vector3.zero;

            Vector3 worldCenter;
            float worldRadius;
            if (col is SphereCollider sphere)
            {
                worldCenter = sphere.transform.TransformPoint(sphere.center);
                worldRadius = sphere.radius * MaxAbsScale(sphere.transform.lossyScale);
            }
            else
            {
                worldCenter = col.bounds.center;
                Vector3 e = col.bounds.extents;
                worldRadius = Mathf.Max(e.x, Mathf.Max(e.y, e.z));
            }

            shape.position = ps.transform.InverseTransformPoint(worldCenter);
            shape.radius = WorldLengthToLocal(ps.transform, worldRadius);
        }

        private static Vector3 AbsVec(Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        private static float MaxAbsScale(Vector3 lossy)
        {
            Vector3 a = AbsVec(lossy);
            return Mathf.Max(a.x, Mathf.Max(a.y, a.z));
        }

        private static float WorldLengthToLocal(Transform xf, float worldLength)
        {
            float s = MaxAbsScale(xf.lossyScale);
            return worldLength / Mathf.Max(s, 0.0001f);
        }

        private static Vector3 WorldSizeToLocal(Transform xf, Vector3 worldSize)
        {
            Vector3 s = AbsVec(xf.lossyScale);
            return new Vector3(
                worldSize.x / Mathf.Max(s.x, 0.0001f),
                worldSize.y / Mathf.Max(s.y, 0.0001f),
                worldSize.z / Mathf.Max(s.z, 0.0001f));
        }

        private void EnsureZoneParticles()
        {
            InstantiateAssignedParticleAssets();

            if (zoneParticles == null)
            {
                ParticleSystem child = GetComponentInChildren<ParticleSystem>(true);
                if (child != null && child.gameObject.scene.IsValid())
                    zoneParticles = child;
            }

            if (zoneParticles == null && zoneParticlePrefab != null && spawnedZoneParticles == null)
            {
                spawnedZoneParticles = Instantiate(zoneParticlePrefab, transform);
                spawnedZoneParticles.transform.localPosition = Vector3.zero;
                zoneParticles = spawnedZoneParticles.GetComponentInChildren<ParticleSystem>(true);
                ForceVolumeCollidersPassThrough();
            }

            particleCacheDirty = true;
            RefreshDrivenParticleCache();
            if (_playerInside)
                PlayDrivenParticles();
            else
                StopDrivenParticles(clear: true);
        }

        private void InstantiateAssignedParticleAssets()
        {
            if (zoneParticles != null && !zoneParticles.gameObject.scene.IsValid())
            {
                ParticleSystem source = zoneParticles;
                spawnedZoneParticles = Instantiate(source.gameObject, transform);
                spawnedZoneParticles.transform.localPosition = Vector3.zero;
                spawnedZoneParticles.transform.localRotation = Quaternion.identity;
                zoneParticles = spawnedZoneParticles.GetComponentInChildren<ParticleSystem>(true);
                ForceVolumeCollidersPassThrough();
            }

            if (zoneParticlePrefab != null && spawnedZoneParticles == null &&
                (zoneParticles == null || !zoneParticles.gameObject.scene.IsValid()))
            {
                spawnedZoneParticles = Instantiate(zoneParticlePrefab, transform);
                spawnedZoneParticles.transform.localPosition = Vector3.zero;
                if (zoneParticles == null || !zoneParticles.gameObject.scene.IsValid())
                    zoneParticles = spawnedZoneParticles.GetComponentInChildren<ParticleSystem>(true);
                ForceVolumeCollidersPassThrough();
            }
        }

        private void TryCatchUpPlayerInside()
        {
            if (_playerInside)
            {
                PlayDrivenParticles();
                return;
            }

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player == null)
                return;

            if (!ContainsPoint(ResolveOuter(), player.transform.position))
                return;

            ExposureReceiver receiver = player.GetComponent<ExposureReceiver>();
            if (receiver == null)
                receiver = player.GetComponentInChildren<ExposureReceiver>();
            if (receiver == null || !ShouldAffect(receiver))
                return;

            occupants.Add(receiver);
            receiver.RegisterZone(this);
            _playerInside = true;
            HandlePlayerEnteredAudioAndShake();
        }

        private void RefreshDrivenParticleCache()
        {
            if (!particleCacheDirty && drivenParticleSystems != null)
                return;

            List<ParticleSystem> found = new List<ParticleSystem>(8);
            CollectParticleSystems(zoneParticles, found);
            if (spawnedZoneParticles != null)
            {
                ParticleSystem[] spawned = spawnedZoneParticles.GetComponentsInChildren<ParticleSystem>(true);
                for (int i = 0; i < spawned.Length; i++)
                    AddParticleSystem(found, spawned[i]);
            }

            ParticleSystem[] children = GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < children.Length; i++)
                AddParticleSystem(found, children[i]);

            if (spawnedVfx != null)
            {
                ParticleSystem[] kids = spawnedVfx.GetComponentsInChildren<ParticleSystem>(true);
                for (int i = 0; i < kids.Length; i++)
                    AddParticleSystem(found, kids[i]);
            }

            drivenParticleSystems = found.ToArray();
            particleCacheDirty = false;
        }

        private static void CollectParticleSystems(ParticleSystem root, List<ParticleSystem> found)
        {
            if (root == null)
                return;

            AddParticleSystem(found, root);
            ParticleSystem[] kids = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < kids.Length; i++)
                AddParticleSystem(found, kids[i]);
        }

        private static void AddParticleSystem(List<ParticleSystem> found, ParticleSystem ps)
        {
            if (ps == null || found.Contains(ps))
                return;
            if (!ps.gameObject.scene.IsValid())
                return;
            found.Add(ps);
        }

        private void PlayDrivenParticles()
        {
            particleCacheDirty = true;
            RefreshDrivenParticleCache();
            ApplyZoneParticleShape();
            if (drivenParticleSystems == null)
                return;

            for (int i = 0; i < drivenParticleSystems.Length; i++)
            {
                ParticleSystem ps = drivenParticleSystems[i];
                if (ps == null)
                    continue;
                var main = ps.main;
                main.playOnAwake = false;
                CacheAuthoredEmission(ps);
                RestoreAuthoredEmission(ps);
                if (!ps.gameObject.activeInHierarchy)
                    ps.gameObject.SetActive(true);
                if (!ps.isPlaying)
                    ps.Play(true);
            }
        }

        private void StopDrivenParticles(bool clear = true)
        {
            RefreshDrivenParticleCache();
            if (drivenParticleSystems == null)
                return;

            ParticleSystemStopBehavior behavior = clear
                ? ParticleSystemStopBehavior.StopEmittingAndClear
                : ParticleSystemStopBehavior.StopEmitting;

            for (int i = 0; i < drivenParticleSystems.Length; i++)
            {
                ParticleSystem ps = drivenParticleSystems[i];
                if (ps == null)
                    continue;
                var main = ps.main;
                main.playOnAwake = false;
                RestoreAuthoredEmission(ps);
                if (ps.isPlaying || clear)
                    ps.Stop(true, behavior);
            }
        }

        private void UpdateZoneParticles()
        {
            if (!_playerInside)
                return;

            RefreshDrivenParticleCache();
            if (drivenParticleSystems == null)
                return;

            GameObject player = PlayerLocator.FindPlayerObject();
            float spatial = player != null
                ? EvaluateSpatialIntensity(player.transform.position)
                : 1f;

            for (int i = 0; i < drivenParticleSystems.Length; i++)
            {
                if (fadeParticlesFromCenter)
                    FadeParticleSystemFromCenter(drivenParticleSystems[i], spatial);
                else
                    RestoreAuthoredEmission(drivenParticleSystems[i]);
            }
        }

        private void CacheAuthoredEmission(ParticleSystem ps)
        {
            if (ps == null || authoredEmissionMul.ContainsKey(ps))
                return;

            ParticleSystem.EmissionModule emission = ps.emission;
            authoredEmissionMul[ps] = new Vector2(
                emission.rateOverTimeMultiplier,
                emission.rateOverDistanceMultiplier);
        }

        private void RestoreAuthoredEmission(ParticleSystem ps)
        {
            if (ps == null)
                return;

            CacheAuthoredEmission(ps);
            if (!authoredEmissionMul.TryGetValue(ps, out Vector2 authored))
                return;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTimeMultiplier = authored.x;
            emission.rateOverDistanceMultiplier = authored.y;
        }

        private void FadeParticleSystemFromCenter(ParticleSystem ps, float spatial)
        {
            if (ps == null)
                return;

            CacheAuthoredEmission(ps);
            if (!authoredEmissionMul.TryGetValue(ps, out Vector2 authored))
                return;

            float fade01 = Mathf.Clamp01(Mathf.InverseLerp(outerIntensity, 1f, spatial));
            float boost = Mathf.Pow(Mathf.Max(1f, particleCenterEmissionMultiplier), fade01);

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTimeMultiplier = authored.x * boost;
            emission.rateOverDistanceMultiplier = authored.y * boost;
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

        private void CacheFalloffColliders()
        {
            Collider[] cols = GetComponents<Collider>();
            if (cols == null || cols.Length == 0)
                return;

            if (outerCollider == null || innerCollider == null)
            {
                Collider largest = null;
                Collider smallest = null;
                float largestVol = -1f;
                float smallestVol = float.MaxValue;
                for (int i = 0; i < cols.Length; i++)
                {
                    Collider col = cols[i];
                    if (col == null)
                        continue;

                    Vector3 e = col.bounds.extents;
                    float vol = Mathf.Max(0.0001f, e.x * e.y * e.z);
                    if (vol > largestVol)
                    {
                        largestVol = vol;
                        largest = col;
                    }

                    if (vol < smallestVol)
                    {
                        smallestVol = vol;
                        smallest = col;
                    }
                }

                if (outerCollider == null)
                    outerCollider = largest;
                if (innerCollider == null)
                    innerCollider = smallest != largest ? smallest : largest;
            }

            zoneCollider = outerCollider != null ? outerCollider : GetComponent<Collider>();
        }

        private Collider ResolveOuter()
        {
            if (outerCollider != null)
                return outerCollider;
            CacheFalloffColliders();
            return outerCollider != null ? outerCollider : GetComponent<Collider>();
        }

        private Collider ResolveInner()
        {
            if (innerCollider != null)
                return innerCollider;
            CacheFalloffColliders();
            return innerCollider;
        }

        private bool StillOverlapsOuter(Collider other)
        {
            Collider outer = ResolveOuter();
            if (outer == null || other == null)
                return false;

            return ContainsPoint(outer, other.bounds.center) || ContainsPoint(outer, other.transform.position);
        }

        public float EvaluateSpatialIntensity(Vector3 worldPoint)
        {
            Collider outer = ResolveOuter();
            if (outer == null)
                return 1f;

            if (!ContainsPoint(outer, worldPoint))
                return 0f;

            Collider inner = ResolveInner();
            if (inner == null || inner == outer)
                return 1f;

            if (ContainsPoint(inner, worldPoint))
                return 1f;

            Vector3 origin = inner.bounds.center;
            Vector3 toPoint = worldPoint - origin;
            float dist = toPoint.magnitude;
            if (dist < 0.0001f)
                return 1f;

            Vector3 dir = toPoint / dist;
            float innerR = DistanceToSurfaceAlongRay(inner, origin, dir);
            float outerR = DistanceToSurfaceAlongRay(outer, origin, dir);
            if (outerR <= innerR + 0.001f)
                return 1f;

            float t = Mathf.InverseLerp(outerR, innerR, dist);
            return Mathf.Lerp(outerIntensity, 1f, t);
        }

        private static bool ContainsPoint(Collider col, Vector3 worldPoint)
        {
            if (col == null)
                return false;

            Vector3 closest = col.ClosestPoint(worldPoint);
            return (closest - worldPoint).sqrMagnitude < 0.0001f;
        }

        private static float DistanceToSurfaceAlongRay(Collider col, Vector3 origin, Vector3 dir)
        {
            float hi = Mathf.Max(col.bounds.extents.magnitude * 4f, 1f);
            float lo = 0f;
            for (int i = 0; i < 18; i++)
            {
                float mid = (lo + hi) * 0.5f;
                if (ContainsPoint(col, origin + dir * mid))
                    lo = mid;
                else
                    hi = mid;
            }

            return hi;
        }

        private void ApplyDefaultGizmoColorsIfNeeded()
        {
            Color baseColor = profile != null ? profile.gizmoColor : new Color(0.79f, 0.18f, 0.48f, 0.45f);
            if (outerGizmoColor.a <= 0.001f)
                outerGizmoColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.22f);
            if (innerGizmoColor.a <= 0.001f)
            {
                innerGizmoColor = new Color(
                    baseColor.r * 0.55f,
                    baseColor.g * 0.55f,
                    baseColor.b * 0.55f,
                    0.50f);
            }
        }

        private void OnDrawGizmos()
        {
            DrawFalloffGizmos(selected: false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawFalloffGizmos(selected: true);
        }

        private void DrawFalloffGizmos(bool selected)
        {
            CacheFalloffColliders();
            ApplyDefaultGizmoColorsIfNeeded();

            Collider outer = ResolveOuter();
            Collider inner = ResolveInner();
            if (outer == null)
                return;

            Color outerFill = outerGizmoColor;
            Color innerFill = innerGizmoColor;
            if (!selected)
            {
                outerFill.a *= 0.45f;
                innerFill.a *= 0.45f;
            }

            DrawColliderGizmo(outer, outerFill, new Color(outerFill.r, outerFill.g, outerFill.b, Mathf.Clamp01(outerFill.a + 0.45f)), drawSolid: false);
            if (inner != null && inner != outer)
                DrawColliderGizmo(inner, innerFill, new Color(innerFill.r, innerFill.g, innerFill.b, Mathf.Clamp01(innerFill.a + 0.35f)), drawSolid: true);
        }

        private static void DrawColliderGizmo(Collider col, Color fill, Color wire, bool drawSolid)
        {
            if (col == null)
                return;

            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = col.transform.localToWorldMatrix;

            SphereCollider sphere = col as SphereCollider;
            BoxCollider box = col as BoxCollider;
            CapsuleCollider capsule = col as CapsuleCollider;
            if (sphere != null)
            {
                if (drawSolid)
                {
                    Gizmos.color = fill;
                    Gizmos.DrawSphere(sphere.center, sphere.radius);
                }

                Gizmos.color = wire;
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
            else if (box != null)
            {
                if (drawSolid)
                {
                    Gizmos.color = fill;
                    Gizmos.DrawCube(box.center, box.size);
                }

                Gizmos.color = wire;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (capsule != null)
            {
                DrawCapsuleGizmo(capsule, fill, wire, drawSolid);
            }
            else
            {
                Gizmos.matrix = Matrix4x4.identity;
                if (drawSolid)
                {
                    Gizmos.color = fill;
                    Gizmos.DrawCube(col.bounds.center, col.bounds.size);
                }

                Gizmos.color = wire;
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }

            Gizmos.matrix = previous;
        }

        private static void DrawCapsuleGizmo(CapsuleCollider capsule, Color fill, Color wire, bool drawSolid)
        {
            float radius = capsule.radius;
            float height = Mathf.Max(capsule.height, radius * 2f);
            Vector3 center = capsule.center;
            Vector3 axis = Vector3.up;
            if (capsule.direction == 0)
                axis = Vector3.right;
            else if (capsule.direction == 2)
                axis = Vector3.forward;

            Vector3 offset = axis * Mathf.Max(0f, height * 0.5f - radius);
            Vector3 a = center + offset;
            Vector3 b = center - offset;
            if (drawSolid)
            {
                Gizmos.color = fill;
                Gizmos.DrawSphere(a, radius);
                Gizmos.DrawSphere(b, radius);
                Gizmos.DrawCube((a + b) * 0.5f, axis * Vector3.Distance(a, b) + Vector3.one * radius);
            }

            Gizmos.color = wire;
            Gizmos.DrawWireSphere(a, radius);
            Gizmos.DrawWireSphere(b, radius);
        }
    }
}
