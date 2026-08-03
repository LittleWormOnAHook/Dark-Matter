using Project.Core;
using Project.Creatures;
using Project.Interaction;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Project.Environment
{
    /// <summary>
    /// Living flesh blob hazard: idles at a slow anim speed until the player enters range,
    /// then agitates. Explodes only if the player stays inside the radius for the full fuse.
    /// Animation is driven with Playables (no AnimatorController) to avoid Editor graph crashes.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public class DMIFleshBlobTrigger : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private AnimationClip idleClip;
        [SerializeField] private float idleAnimSpeed = 0.1f;
        [SerializeField] private float triggeredAnimSpeed = 1f;

        [Header("Proximity Trigger")]
        [Tooltip("Player must stand within this radius to agitate the blob.")]
        [SerializeField] private float triggerRadius = 2f;
        [Tooltip("Continuous time inside the radius required before explosion.")]
        [SerializeField] private float fuseDuration = 3f;

        [Header("Explosion / Spawn")]
        [SerializeField] private GameObject emberSkitterPrefab;
        [SerializeField] private int spawnCount = 2;
        [SerializeField] private float spawnRadius = 1.25f;
        [SerializeField] private GameObject explosionVfxPrefab;
        [SerializeField] private bool destroyOnExplode = true;

        private bool inRange;
        private bool exploded;
        private float fuseElapsed;
        private PlayableGraph playableGraph;
        private AnimationClipPlayable clipPlayable;
        private bool playablesReady;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>(true);

            // Avoid Mecanim controller + Animator window graph path (known Editor crash vector).
            if (animator != null)
                animator.runtimeAnimatorController = null;
        }

        private void OnEnable()
        {
            BuildPlayables();
            ApplyAnimSpeed(idleAnimSpeed);
        }

        private void OnDisable()
        {
            DestroyPlayables();
        }

        private void OnDestroy()
        {
            DestroyPlayables();
        }

        private void Update()
        {
            if (exploded)
                return;

            if (!GameSession.HasStarted)
                return;

            if (!PlayerInteractionUtility.TryGetPlayerPosition(out Vector3 playerPosition))
                return;

            bool nowInRange = IsWithinTrigger(playerPosition);
            if (nowInRange != inRange)
            {
                inRange = nowInRange;
                fuseElapsed = 0f;
                ApplyAnimSpeed(inRange ? triggeredAnimSpeed : idleAnimSpeed);
            }

            if (!inRange)
                return;

            fuseElapsed += Time.deltaTime;
            if (fuseElapsed >= fuseDuration)
                Explode();
        }

        private void BuildPlayables()
        {
            DestroyPlayables();

            if (animator == null || idleClip == null)
                return;

            playableGraph = PlayableGraph.Create("DMIFleshBlob");
            playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            AnimationPlayableOutput output = AnimationPlayableOutput.Create(playableGraph, "Animation", animator);
            clipPlayable = AnimationClipPlayable.Create(playableGraph, idleClip);
            clipPlayable.SetApplyFootIK(false);
            clipPlayable.SetSpeed(Mathf.Max(0.01f, idleAnimSpeed));
            output.SetSourcePlayable(clipPlayable);

            playableGraph.Play();
            playablesReady = true;
        }

        private void DestroyPlayables()
        {
            if (playableGraph.IsValid())
                playableGraph.Destroy();

            playablesReady = false;
        }

        private bool IsWithinTrigger(Vector3 playerPosition)
        {
            Vector3 delta = playerPosition - transform.position;
            delta.y = 0f;
            return delta.sqrMagnitude <= triggerRadius * triggerRadius;
        }

        private void ApplyAnimSpeed(float speed)
        {
            float safe = Mathf.Max(0.01f, speed);
            if (playablesReady && clipPlayable.IsValid())
                clipPlayable.SetSpeed(safe);
        }

        private void Explode()
        {
            if (exploded)
                return;

            exploded = true;
            DestroyPlayables();

            if (explosionVfxPrefab != null)
                Instantiate(explosionVfxPrefab, transform.position, Quaternion.identity);

            SpawnSkitters();

            if (destroyOnExplode)
                Destroy(gameObject);
            else
                gameObject.SetActive(false);
        }

        private void SpawnSkitters()
        {
            if (emberSkitterPrefab == null || spawnCount <= 0)
                return;

            for (int i = 0; i < spawnCount; i++)
                DMICreatureSpawnUtility.SpawnAround(emberSkitterPrefab, transform.position, spawnRadius);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.95f, 0.35f, 0.15f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, triggerRadius);
        }

        private void Reset()
        {
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>(true);
        }
#endif
    }
}
