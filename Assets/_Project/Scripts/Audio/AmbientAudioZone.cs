using System.Collections;
using System.Collections.Generic;
using Project.Core;
using UnityEngine;

namespace Project.Audio
{
    [RequireComponent(typeof(Collider))]
    public class AmbientAudioZone : MonoBehaviour
    {
        [SerializeField] private string zoneName = "Forest Ambience";
        [SerializeField] private AmbientZoneLayer[] layers;
        [SerializeField] private bool playOnlyDuringGameplay = true;

        private readonly HashSet<Transform> occupants = new HashSet<Transform>();
        private readonly List<Coroutine> layerCoroutines = new List<Coroutine>();

        private Collider zoneCollider;

        private void Awake()
        {
            zoneCollider = GetComponent<Collider>();
            zoneCollider.isTrigger = true;
        }

        private void OnValidate()
        {
            if (!string.IsNullOrWhiteSpace(zoneName))
                gameObject.name = zoneName;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other))
                return;

            occupants.Add(other.transform);
            if (occupants.Count == 1)
                StartZoneAudio();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other))
                return;

            occupants.Remove(other.transform);
            if (occupants.Count == 0)
                StopZoneAudio();
        }

        public void SetLayers(AmbientZoneLayer[] newLayers)
        {
            layers = newLayers;
        }

        public Vector3 GetRandomPointInZone()
        {
            if (zoneCollider == null)
                return transform.position;

            Bounds bounds = zoneCollider.bounds;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Vector3 candidate = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    Random.Range(bounds.min.y, bounds.max.y),
                    Random.Range(bounds.min.z, bounds.max.z));

                if (zoneCollider.ClosestPoint(candidate) == candidate)
                    return candidate;
            }

            return bounds.center;
        }

        private void StartZoneAudio()
        {
            StopZoneAudio();
            if (layers == null)
                return;

            for (int i = 0; i < layers.Length; i++)
            {
                AmbientZoneLayer layer = layers[i];
                if (layer == null || layer.clips == null || layer.clips.Length == 0)
                    continue;

                layerCoroutines.Add(StartCoroutine(PlayLayerLoop(layer)));
            }
        }

        private void StopZoneAudio()
        {
            for (int i = 0; i < layerCoroutines.Count; i++)
            {
                if (layerCoroutines[i] != null)
                    StopCoroutine(layerCoroutines[i]);
            }

            layerCoroutines.Clear();
        }

        private IEnumerator PlayLayerLoop(AmbientZoneLayer layer)
        {
            yield return new WaitForSeconds(Random.Range(0.5f, layer.minInterval));

            while (occupants.Count > 0)
            {
                if (!playOnlyDuringGameplay || GameSession.HasStarted)
                {
                    Vector3 point = layer.playAtRandomPointInZone ? GetRandomPointInZone() : transform.position;
                    GameAudioManager.Instance?.PlayAmbientOneShot(layer, point);
                }

                float wait = Random.Range(layer.minInterval, layer.maxInterval);
                yield return new WaitForSeconds(wait);
            }
        }

        private static bool IsPlayer(Collider other)
        {
            return other.CompareTag("Player") || other.GetComponentInParent<ECM2.Character>() != null;
        }

        private void OnDrawGizmosSelected()
        {
            Collider col = zoneCollider != null ? zoneCollider : GetComponent<Collider>();
            if (col == null)
                return;

            Gizmos.color = new Color(0.35f, 0.85f, 0.45f, 0.35f);
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            Gizmos.color = new Color(0.35f, 0.85f, 0.45f, 0.9f);
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}
