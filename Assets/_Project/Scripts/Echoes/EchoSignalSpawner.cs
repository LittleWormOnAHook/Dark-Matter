using Project.Core;
using Project.Pioneers;
using UnityEngine;

namespace Project.Echoes
{
    /// <summary>
    /// Spawns test neural echo signals and registers them with EchoSignalRegistry.
    /// </summary>
    public class EchoSignalSpawner : MonoBehaviour
    {
        [SerializeField] private EchoWorldEntity echoEntityPrefab;
        [SerializeField] private float spawnDistance = 4f;
        [SerializeField] private EchoDisposition testDisposition = EchoDisposition.Neutral;

        public EchoWorldEntity SpawnTestSignalNearPlayer()
        {
            GameObject player = PlayerLocator.FindPlayerObject();
            Vector3 origin = player != null ? player.transform.position : transform.position;
            Vector3 forward = player != null ? player.transform.forward : Vector3.forward;
            Vector3 spawnPoint = origin + forward * spawnDistance;
            spawnPoint.y = origin.y;

            SkilledPioneerRecord record = EchoGenerator.GenerateSignal(testDisposition);
            return SpawnSignal(record, spawnPoint);
        }

        public EchoWorldEntity SpawnSignal(SkilledPioneerRecord record, Vector3 worldPosition)
        {
            if (record == null)
                return null;

            EchoWorldEntity entity;
            if (echoEntityPrefab != null)
            {
                entity = Instantiate(echoEntityPrefab, worldPosition, Quaternion.identity, transform);
            }
            else
            {
                entity = CreateRuntimeEchoEntity(worldPosition);
            }

            entity.Initialize(record);
            return entity;
        }

        private EchoWorldEntity CreateRuntimeEchoEntity(Vector3 worldPosition)
        {
            GameObject host = new GameObject("EchoSignal");
            host.transform.SetParent(transform, false);
            host.transform.position = worldPosition;

            SphereCollider collider = host.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 1.2f;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "SignalVisual";
            visual.transform.SetParent(host.transform, false);
            visual.transform.localScale = Vector3.one * 0.8f;
            Collider visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null)
                Destroy(visualCollider);

            return host.AddComponent<EchoWorldEntity>();
        }
    }
}
