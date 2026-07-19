using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Marks a manual world position where <see cref="EnemySpawner"/> can spawn enemies.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemySpawnPoint : MonoBehaviour
    {
        [SerializeField] private float spawnRadius = 0.75f;
        [SerializeField] private bool faceSpawnerForward = true;

        public float SpawnRadius => spawnRadius;

        public Quaternion ResolveRotation(Transform fallbackForward)
        {
            if (faceSpawnerForward && fallbackForward != null)
                return Quaternion.LookRotation(fallbackForward.forward, Vector3.up);

            return transform.rotation;
        }

        public Vector3 ResolvePosition()
        {
            if (spawnRadius <= 0f)
                return transform.position;

            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            return transform.position + new Vector3(offset.x, 0f, offset.y);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.85f, 0.65f, 0.12f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, spawnRadius));
            Gizmos.DrawSphere(transform.position, 0.12f);
            Gizmos.DrawRay(transform.position, transform.forward * 0.75f);
        }
    }
}
