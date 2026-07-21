using System.Collections.Generic;
using Project.Companions;
using Project.Core;
using UnityEngine;

namespace Project.Survival.Exposure
{
    [RequireComponent(typeof(Collider))]
    public class ExposureZoneVolume : MonoBehaviour
    {
        [SerializeField] private ExposureZoneProfile profile;
        [SerializeField] private bool affectPlayer = true;
        [SerializeField] private bool affectCompanions = true;
        [SerializeField] private bool playAmbientLoop;
        [SerializeField] private float ambientVolume = 0.35f;

        private readonly HashSet<ExposureReceiver> occupants = new HashSet<ExposureReceiver>();
        private Collider zoneCollider;
        private AudioSource ambientSource;
        private GameObject spawnedVfx;
        private float pulsePhaseTimer;
        private bool pulseActive = true;

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

        private void Update()
        {
            if (!Application.isPlaying || profile == null || profile.pulse == null || !profile.pulse.enabled)
                return;

            pulsePhaseTimer -= Time.deltaTime;
            if (pulsePhaseTimer > 0f)
                return;

            pulseActive = !pulseActive;
            float baseDuration = pulseActive
                ? profile.pulse.activeDurationSeconds
                : profile.pulse.inactiveDurationSeconds;
            float jitter = profile.pulse.timingJitter;
            float variance = baseDuration * Random.Range(-jitter, jitter);
            pulsePhaseTimer = Mathf.Max(0.25f, baseDuration + variance);
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
            StartAmbientLoop();
        }

        private void HandleLastOccupantLeft()
        {
            DestroyAmbientVfx();
            StopAmbientLoop();
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

            ambientSource = GetComponent<AudioSource>();
            if (ambientSource == null)
                ambientSource = gameObject.AddComponent<AudioSource>();

            ambientSource.loop = true;
            ambientSource.playOnAwake = false;
            ambientSource.spatialBlend = 1f;
            ambientSource.volume = ambientVolume;
        }

        private void StartAmbientLoop()
        {
            if (!playAmbientLoop || profile == null || profile.ambientLoopClip == null)
                return;

            EnsureAmbientSource();
            ambientSource.clip = profile.ambientLoopClip;
            if (!ambientSource.isPlaying)
                ambientSource.Play();
        }

        private void StopAmbientLoop()
        {
            if (ambientSource != null && ambientSource.isPlaying)
                ambientSource.Stop();
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
