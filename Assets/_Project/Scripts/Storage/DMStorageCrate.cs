using System.Collections.Generic;
using Project.Core;
using Project.Interaction;
using Project.Player;
using Project.UI;
using UnityEngine;

namespace Project.Storage
{
    public class DMStorageCrate : MonoBehaviour, IWorldUsable
    {
        public const float AimRadius = 1.25f;
        public const string DefaultOpenAnimationName = "Cache-Lid-Open";

        [SerializeField] private string crateId = "camp_storage_01";
        [SerializeField] private int slotCount = 20;
        [SerializeField] private float interactRange = 3.5f;
        [SerializeField] private string promptText = "Press E — Storage";

        [Header("Presentation")]
        [SerializeField] private Animation chestAnimation;
        [SerializeField] private string openAnimationName = DefaultOpenAnimationName;
        [SerializeField] private GameObject openParticle;
        [SerializeField] private Collider interactCollider;

        public string CrateId => string.IsNullOrWhiteSpace(crateId) ? "camp_storage_01" : crateId;
        public int SlotCount => Mathf.Max(1, slotCount);
        public string DisplayName => "Storage";
        public Collider InteractCollider => interactCollider;

        private static readonly List<DMStorageCrate> active = new List<DMStorageCrate>(4);

        public static IReadOnlyList<DMStorageCrate> Active => active;

        private void Awake()
        {
            ResolvePresentation();
        }

        private void OnEnable()
        {
            ResolvePresentation();
            if (!active.Contains(this))
                active.Add(this);
            WorldUseController.Register(this);
            DMStorageCrateRuntime.GetOrCreate(CrateId, SlotCount);
        }

        private void OnDisable()
        {
            active.Remove(this);
            WorldUseController.Unregister(this);
        }

        public float GetUsePriority(WorldUseContext context)
        {
            if (!GameSession.HasStarted || !IsWithinInteractRange(context.PlayerPosition))
                return -1f;
            if (!WorldUseController.IsAimedAtStorageCrate(context, this, interactCollider))
                return -1f;

            float distance = PlayerInteractionUtility.DistanceToInteractable(
                context.PlayerPosition,
                interactCollider,
                transform.position);
            return 90f - distance;
        }

        public bool TryUse(WorldUseContext context)
        {
            if (!GameSession.HasStarted || !IsWithinInteractRange(context.PlayerPosition))
                return false;
            if (!DMUiToolkitCrate.TryShow(this))
                return false;

            PlayOpenPresentation();
            TryPlayPlayerLootAnimation(context.PlayerTransform);
            return true;
        }

        public void NotifyClosed()
        {
            PlayClosePresentation();
        }

        public bool IsWithinInteractRange(Vector3 playerPosition)
        {
            return PlayerInteractionUtility.DistanceToInteractable(
                playerPosition,
                interactCollider,
                transform.position) <= interactRange;
        }

        public string GetInteractionPromptMessage()
        {
            return string.IsNullOrWhiteSpace(promptText) ? "Press E — Storage" : promptText;
        }

        public void ResolvePresentation()
        {
            if (chestAnimation == null)
                chestAnimation = GetComponent<Animation>();
            if (chestAnimation == null)
                chestAnimation = GetComponentInChildren<Animation>(true);

            if (openParticle == null)
            {
                Transform particle = transform.Find("Particle System");
                if (particle != null)
                    openParticle = particle.gameObject;
            }

            if (interactCollider == null)
            {
                Transform collection = transform.Find("collection");
                if (collection != null)
                    interactCollider = collection.GetComponent<Collider>();
            }

            if (interactCollider == null)
            {
                Collider[] colliders = GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] != null && colliders[i].isTrigger)
                    {
                        interactCollider = colliders[i];
                        break;
                    }
                }
            }

            if (interactCollider == null)
                interactCollider = GetComponent<Collider>();
        }

        private void PlayOpenPresentation()
        {
            if (openParticle != null)
                openParticle.SetActive(true);

            PlayLid(1f, 0f);
        }

        private void PlayClosePresentation()
        {
            if (openParticle != null)
                openParticle.SetActive(false);

            string clipName = ResolveOpenAnimationName();
            if (chestAnimation == null || string.IsNullOrEmpty(clipName))
                return;

            AnimationState state = chestAnimation[clipName];
            float startTime = state != null ? state.length : 1f;
            PlayLid(-1f, startTime);
        }

        private void PlayLid(float speed, float time)
        {
            string clipName = ResolveOpenAnimationName();
            if (chestAnimation == null || string.IsNullOrEmpty(clipName))
                return;

            AnimationState state = chestAnimation[clipName];
            if (state != null)
            {
                state.speed = speed;
                state.time = time;
            }

            chestAnimation.Stop();
            chestAnimation.Play(clipName);
        }

        private string ResolveOpenAnimationName()
        {
            if (chestAnimation == null)
                return null;

            if (!string.IsNullOrEmpty(openAnimationName)
                && (chestAnimation.GetClip(openAnimationName) != null || chestAnimation[openAnimationName] != null))
                return openAnimationName;

            if (chestAnimation.GetClip(DefaultOpenAnimationName) != null
                || chestAnimation[DefaultOpenAnimationName] != null)
                return DefaultOpenAnimationName;

            return chestAnimation.clip != null ? chestAnimation.clip.name : null;
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

#if UNITY_EDITOR
        private void OnValidate()
        {
            interactRange = Mathf.Max(0.5f, interactRange);
            slotCount = Mathf.Max(1, slotCount);
        }
#endif
    }
}
