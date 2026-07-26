using System.Collections;
using Project.AI;
using Project.Interaction;
using Project.Player;
using Project.UI;
using UnityEngine;

namespace Project.Events
{
    /// <summary>
    /// Dark Matter loot collection trigger for animated world caches.
    /// Plays lid-open presentation, then opens loot via <see cref="DmEvents"/>.
    /// Uses project inventory only — no third-party item lists.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class DMItemCollection : MonoBehaviour, IWorldUsable
    {
        public const string DefaultOpenAnimationName = "Cache-Lid-Open";
        public const string CacheLidChildName = "Cache Lid";

        [Header("Identity")]
        [SerializeField] private string promptText = "Press E to open";
        [SerializeField] private float interactRange = 3f;

        [Header("Chest Presentation")]
        [Tooltip("Cache root with the Animation component (defaults to parent).")]
        [SerializeField] private Animation chestAnimation;
        [SerializeField] private string openAnimationName = DefaultOpenAnimationName;
        [SerializeField] private GameObject openParticle;
        [SerializeField] private float openLootDialogDelay = 1.0f;

        [Header("Loot Source")]
        [Tooltip("Loot table + grant logic. Auto-resolved from parents if empty.")]
        [SerializeField] private DmEvents dmEvents;

        [Header("Lifecycle")]
        [SerializeField] private bool disableTriggerAfterOpen = true;
        [SerializeField] private bool oneTimeOpen = true;

        private bool opened;
        private bool opening;
        private Collider triggerCollider;
        private bool initialized;

        public bool HasOpened => opened;
        public DmEvents Events => dmEvents != null ? dmEvents : (dmEvents = GetComponentInParent<DmEvents>());

        private void Awake()
        {
            EnsureTrigger();
            ResolveReferences();
            initialized = true;
        }

        private void OnEnable()
        {
            if (initialized && CanInteract())
                WorldUseController.Register(this);
        }

        private void OnDisable()
        {
            WorldUseController.Unregister(this);
        }

        private void Start()
        {
            ResolveReferences();
            if (CanInteract())
                WorldUseController.Register(this);
        }

        private void ResolveReferences()
        {
            if (dmEvents == null)
                dmEvents = GetComponentInParent<DmEvents>();

            if (chestAnimation == null)
            {
                Transform root = dmEvents != null ? dmEvents.transform : transform.parent;
                if (root != null)
                    chestAnimation = root.GetComponent<Animation>();
                if (chestAnimation == null)
                    chestAnimation = GetComponentInParent<Animation>();
            }

            if (openParticle == null && chestAnimation != null)
            {
                Transform particle = chestAnimation.transform.Find("Particle System");
                if (particle != null)
                    openParticle = particle.gameObject;
            }

            if (openParticle != null)
                openParticle.SetActive(false);
        }

        private void EnsureTrigger()
        {
            triggerCollider = GetComponent<Collider>();
            if (triggerCollider == null)
            {
                BoxCollider box = gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = Vector3.one * 1.5f;
                triggerCollider = box;
            }
            else
            {
                triggerCollider.isTrigger = true;
            }
        }

        public bool CanInteract()
        {
            if (!initialized || opening)
                return false;

            if (oneTimeOpen && opened)
                return false;

            DmEvents events = Events;
            return events != null && events.HasRemainingLoot;
        }

        public bool IsWithinInteractRange(Vector3 playerPosition)
        {
            return CanInteract() && Vector3.Distance(playerPosition, transform.position) <= interactRange;
        }

        public float GetUsePriority(WorldUseContext context)
        {
            if (!IsWithinInteractRange(context.PlayerPosition))
                return -1f;

            float distance = Vector3.Distance(context.PlayerPosition, transform.position);
            // Slightly above DmEvents so the collection trigger owns the open sequence.
            return 97f - distance;
        }

        public bool TryUse(WorldUseContext context)
        {
            if (!IsWithinInteractRange(context.PlayerPosition))
                return false;

            if (EnemyLootDialogUI.IsDialogOpen)
                return false;

            StartCoroutine(OpenSequence(context.PlayerTransform));
            return true;
        }

        public string GetInteractionPromptMessage()
        {
            DmEvents events = Events;
            string label = events != null ? events.CacheDisplayName : "Cache";
            return $"{promptText} — {label}";
        }

        private IEnumerator OpenSequence(Transform playerTransform)
        {
            opening = true;
            WorldUseController.Unregister(this);

            TryPlayPlayerLootAnimation(playerTransform);
            PlayOpenPresentation();

            if (openLootDialogDelay > 0f)
                yield return new WaitForSeconds(openLootDialogDelay);

            DmEvents events = Events;
            if (events != null && events.HasRemainingLoot)
                events.OpenLootDialogFromCollection();

            opened = true;
            opening = false;

            if (disableTriggerAfterOpen && triggerCollider != null)
                triggerCollider.enabled = false;

            if (!CanInteract())
                WorldUseController.Unregister(this);
        }

        private static void TryPlayPlayerLootAnimation(Transform playerTransform)
        {
            if (playerTransform == null)
                return;

            PlayerLootAnimationController lootAnimation =
                playerTransform.GetComponentInChildren<PlayerLootAnimationController>();
            if (lootAnimation == null)
            {
                Animator animator = playerTransform.GetComponentInChildren<Animator>(true);
                if (animator == null)
                    return;

                lootAnimation = animator.gameObject.AddComponent<PlayerLootAnimationController>();
            }

            lootAnimation.BeginLoot();
        }

        private void PlayOpenPresentation()
        {
            if (openParticle != null)
                openParticle.SetActive(true);

            if (chestAnimation == null || string.IsNullOrEmpty(openAnimationName))
                return;

            // Legacy Animation clips bind by child path ("Cache Lid"). Prefer Play over CrossFade.
            string clipName = ResolveOpenAnimationName();
            if (!string.IsNullOrEmpty(clipName))
            {
                chestAnimation.Stop();
                chestAnimation.Play(clipName);
            }
            else if (chestAnimation.clip != null)
            {
                chestAnimation.Stop();
                chestAnimation.Play();
            }
        }

        private string ResolveOpenAnimationName()
        {
            if (chestAnimation == null)
                return null;

            if (!string.IsNullOrEmpty(openAnimationName) &&
                (chestAnimation.GetClip(openAnimationName) != null || chestAnimation[openAnimationName] != null))
                return openAnimationName;

            if (chestAnimation.GetClip(DefaultOpenAnimationName) != null ||
                chestAnimation[DefaultOpenAnimationName] != null)
                return DefaultOpenAnimationName;

            return null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            interactRange = Mathf.Max(0.5f, interactRange);
            openLootDialogDelay = Mathf.Max(0f, openLootDialogDelay);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.83f, 0.63f, 0.09f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
#endif
    }
}
