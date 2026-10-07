using System.Collections;
using Project.AI;
using Project.Audio;
using Project.Interaction;
using Project.Player;
using Project.UI;
using UnityEngine;

namespace Project.Events
{
    /// <summary>Per-chest behaviour (loot plan 7.1 / 7.5).</summary>
    public enum DMLootChestMode
    {
        /// <summary>Re-openable while items remain; leftovers reset after the reloot window (timers: phase 4).</summary>
        WorldReloot = 0,

        /// <summary>One visit: closing the window locks the chest unless a protected entry or full-inventory hold keeps it open.</summary>
        SingleLoot = 1,

        /// <summary>Like World Reloot but never expires or dissolves; an emptied chest stays with its lid closed.</summary>
        Story = 2,

        /// <summary>Player storage (handled by DMStorageCrate). On a collection it behaves like Story.</summary>
        Storage = 3
    }

    /// <summary>Chest state machine (loot plan 7.1). Dissolving / Gone are driven from phase 4.</summary>
    public enum DMLootChestPhase
    {
        Closed = 0,
        Opening = 1,
        Open = 2,
        Closing = 3,
        Dissolving = 4,
        Gone = 5
    }

    /// <summary>
    /// Dark Matter loot chest trigger for animated world caches.
    /// Closed -> Opening (lid on unscaled time, <see cref="DMChestLid"/>) -> Open (UITK loot window via <see cref="DmEvents"/>)
    /// -> Closing (lid closes when the window closes) -> Closed. Mode rules decide whether it can be opened again.
    /// Timers, holds and the dissolve (loot plan 11) tick in <see cref="DMLootChestRuntime"/>; Gone hides the chest
    /// root (SetActive(false), never Destroy) so a load / New Game can bring it back (D1).
    /// Uses project inventory only — no third-party item lists.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class DMItemCollection : MonoBehaviour, IWorldUsable
    {
        public const string DefaultOpenAnimationName = "Cache-Lid-Open";
        public const string CacheLidChildName = "Cache Lid";

        [Header("Identity")]
        [SerializeField] private DMLootChestMode mode = DMLootChestMode.WorldReloot;
        [Tooltip("Stable save id for this chest (loot plan 7.1 / save v24). Leave empty until the save phase assigns ids.")]
        [SerializeField] private string chestId = string.Empty;
        [SerializeField] private string promptText = "Press E to open";
        [SerializeField] private float interactRange = 3f;

        [Header("Chest Presentation")]
        [Tooltip("Cache root with the Animation component (defaults to parent).")]
        [SerializeField] private Animation chestAnimation;
        [SerializeField] private string openAnimationName = DefaultOpenAnimationName;
        [SerializeField] private GameObject openParticle;
        [Tooltip("Fallback only: DM_LootChestProfile.openLootWindowDelay wins when the profile exists.")]
        [SerializeField] private float openLootDialogDelay = 1.0f;

        [Header("Loot Source")]
        [Tooltip("Loot table + grant logic. Auto-resolved from parents if empty.")]
        [SerializeField] private DmEvents dmEvents;

        private DMLootChestPhase phase = DMLootChestPhase.Closed;
        private bool opened;
        private bool singleLootLocked;
        private bool fullInventoryHold;
        private bool exitedWithLeftovers;
        private Collider triggerCollider;
        private DMChestLid lid;
        private DmEvents subscribedEvents;
        private Coroutine openRoutine;
        private bool initialized;

        // Loot plan 11 timers (scaled game time, ticked by DMLootChestRuntime only while not open).
        private bool relootTimerStarted;
        private float relootRemaining;
        private float holdRemaining;
        private float goneCountdown = -1f;
        private DMChestDissolve dissolve;
        private int promptCacheKey = int.MinValue;
        private string promptCache;

        public DMLootChestMode Mode => mode;
        public string ChestId => chestId;
        public DMLootChestPhase Phase => phase;
        public bool HasOpened => opened;
        /// <summary>True once the player closed the window with items left (World Reloot timer start, phase 4).</summary>
        public bool HasExitedWithLeftovers => exitedWithLeftovers;
        /// <summary>Protected entry or full-inventory hold keeps the chest re-openable and blocks dissolve (D2 / D4).</summary>
        public bool IsHeld => holdRemaining > 0f || IsProtected;
        /// <summary>A quest / unique entry is still inside (D2): no timer, no dissolve.</summary>
        public bool IsProtected => Events != null && Events.HasRemainingLoot && Events.HasProtectedEntries;
        public bool IsFullInventoryHoldActive => holdRemaining > 0f;
        public float FullInventoryHoldRemaining => Mathf.Max(0f, holdRemaining);
        public bool RelootTimerStarted => relootTimerStarted;
        public float RelootRemaining => Mathf.Max(0f, relootRemaining);
        public bool IsGone => phase == DMLootChestPhase.Gone;
        public bool NeverExpires => mode == DMLootChestMode.Story || mode == DMLootChestMode.Storage;
        public DmEvents Events => dmEvents != null ? dmEvents : (dmEvents = GetComponentInParent<DmEvents>());

        private int appliedGeneration;

        private void Awake()
        {
            EnsureTrigger();
            ResolveReferences();
            initialized = true;
            DMLootChestRuntime.Register(this);
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeEvents();
            if (initialized && CanInteract())
                WorldUseController.Register(this);
        }

        private void OnDisable()
        {
            WorldUseController.Unregister(this);
            UnsubscribeEvents();
            if (openRoutine != null)
            {
                StopCoroutine(openRoutine);
                openRoutine = null;
            }

            if (phase == DMLootChestPhase.Opening || phase == DMLootChestPhase.Closing)
            {
                phase = DMLootChestPhase.Closed;
                lid?.SnapClosed();
            }

            // Timers keep running while a tile is disabled (9.5); only a destroyed chest leaves the runtime.
        }

        private void OnDestroy()
        {
            DMLootChestRuntime.Unregister(this);
            dissolve?.Restore();
        }

        /// <summary>Save v24 snapshot (9.1). Null without a chestId (unsaved chest) or for Story-less runtime spawns.</summary>
        public LootChestSave CaptureSave()
        {
            if (string.IsNullOrWhiteSpace(chestId))
                return null;

            DmEvents events = Events;
            // Dissolving, or doomed by a running countdown: the loss is locked in, save as Gone (9.1).
            bool gone = phase == DMLootChestPhase.Gone || phase == DMLootChestPhase.Dissolving || goneCountdown >= 0f;
            return new LootChestSave
            {
                chestId = chestId,
                phase = gone ? (int)DMLootChestPhase.Gone : (int)DMLootChestPhase.Closed,
                opened = opened,
                exitedWithLeftovers = exitedWithLeftovers,
                singleLootLocked = singleLootLocked,
                timerStarted = !gone && relootTimerStarted,
                remainingSeconds = gone ? 0f : Mathf.Max(0f, relootRemaining),
                holdRemainingSeconds = gone ? 0f : Mathf.Max(0f, holdRemaining),
                entries = gone || events == null ? new LootChestEntrySave[0] : events.BuildEntrySave()
            };
        }

        /// <summary>
        /// Load / New Game (9.2 / 9.3): null = fresh authored chest. Brings a dissolved / hidden chest back,
        /// or hides a saved Gone chest instantly (no dissolve).
        /// </summary>
        public void ApplyRegistryState(LootChestSave save, int generation)
        {
            appliedGeneration = generation;
            DmEvents events = Events;
            if (events != null && EnemyLootDialogUI.IsShowing(events))
                EnemyLootDialogUI.CloseAnyOpenLoot();

            if (openRoutine != null)
            {
                StopCoroutine(openRoutine);
                openRoutine = null;
            }

            dissolve?.Restore();
            goneCountdown = -1f;
            fullInventoryHold = false;
            if (openParticle != null)
                openParticle.SetActive(false);
            lid?.SnapClosed();

            if (save == null)
            {
                opened = false;
                exitedWithLeftovers = false;
                singleLootLocked = false;
                relootTimerStarted = false;
                relootRemaining = 0f;
                holdRemaining = 0f;
                events?.ResetToAuthoredLoot();
            }
            else
            {
                opened = save.opened;
                exitedWithLeftovers = save.exitedWithLeftovers;
                singleLootLocked = save.singleLootLocked;
                relootTimerStarted = save.timerStarted;
                relootRemaining = Mathf.Max(0f, save.remainingSeconds);
                holdRemaining = Mathf.Max(0f, save.holdRemainingSeconds);
                fullInventoryHold = holdRemaining > 0f;
                events?.ApplySavedEntries(save.entries);
            }

            promptCacheKey = int.MinValue;
            Transform root = ResolveChestRoot();
            if (save != null && save.phase == (int)DMLootChestPhase.Gone)
            {
                phase = DMLootChestPhase.Gone;
                relootTimerStarted = false;
                holdRemaining = 0f;
                WorldUseController.Unregister(this);
                DMLootChestRuntime.Untrack(this);
                events?.SetVisibleToScanner(false);
                if (root != null)
                    root.gameObject.SetActive(false);
                return;
            }

            phase = DMLootChestPhase.Closed;
            if (root != null && !root.gameObject.activeSelf)
                root.gameObject.SetActive(true);
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            if (triggerCollider != null)
                triggerCollider.enabled = true;

            if (relootTimerStarted || holdRemaining > 0f)
                DMLootChestRuntime.Track(this);
            if (isActiveAndEnabled)
                RefreshRegistration();
        }

        private void Start()
        {
            ResolveReferences();
            SubscribeEvents();
            // Streamed tile woke after a load / New Game: take the registry state now (9.1 / 9.2).
            if (appliedGeneration != DMLootChestRuntime.Generation)
                ApplyRegistryState(DMLootChestRuntime.GetSaved(chestId), DMLootChestRuntime.Generation);
            RefreshRegistration();
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

            if (openParticle != null && phase == DMLootChestPhase.Closed)
                openParticle.SetActive(false);

            if (lid == null)
                lid = DMChestLid.EnsureForLegacyClip(chestAnimation, ResolveOpenAnimationName());

            // The collection owns the emptied look: Story keeps it (7.5), the others dissolve (phase 4).
            if (dmEvents != null)
                dmEvents.SetKeepVisualWhenEmpty(true);
        }

        private void SubscribeEvents()
        {
            DmEvents events = Events;
            if (events == subscribedEvents)
                return;

            UnsubscribeEvents();
            if (events == null)
                return;

            events.LootWindowClosed += HandleLootWindowClosed;
            subscribedEvents = events;
        }

        private void UnsubscribeEvents()
        {
            if (subscribedEvents == null)
                return;

            subscribedEvents.LootWindowClosed -= HandleLootWindowClosed;
            subscribedEvents = null;
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
                triggerCollider.enabled = true;
            }
        }

        public bool CanInteract()
        {
            if (!initialized || phase != DMLootChestPhase.Closed || goneCountdown >= 0f)
                return false;

            if (mode == DMLootChestMode.SingleLoot && singleLootLocked)
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
            // Above the loot bag (94) and storage crate (90) so E opens the chest the prompt names (7.6).
            return 97f - distance;
        }

        public bool TryUse(WorldUseContext context)
        {
            if (!IsWithinInteractRange(context.PlayerPosition))
                return false;

            if (EnemyLootDialogUI.IsDialogOpen)
                return false;

            openRoutine = StartCoroutine(OpenSequence(context.PlayerTransform));
            return true;
        }

        public string GetInteractionPromptMessage()
        {
            // Rebuilt only when the shown second / state changes (no per-frame string work).
            int key = BuildPromptKey();
            if (key == promptCacheKey && promptCache != null)
                return promptCache;

            DmEvents events = Events;
            string label = events != null ? events.CacheDisplayName : "Cache";
            string type = ResolveTypeLabel();
            promptCacheKey = key;
            promptCache = string.IsNullOrEmpty(type)
                ? $"{promptText} — {label}"
                : $"{promptText} — {label} ({type})";
            return promptCache;
        }

        private int BuildPromptKey()
        {
            int state = (int)mode * 4 + (holdRemaining > 0f ? 1 : 0) + (relootTimerStarted ? 2 : 0);
            int seconds = holdRemaining > 0f
                ? Mathf.CeilToInt(holdRemaining / 60f)
                : relootTimerStarted ? Mathf.CeilToInt(relootRemaining) : 0;
            return state * 100000 + seconds;
        }

        /// <summary>Chest type and time left shown in the prompt (7.6).</summary>
        public string ResolveTypeLabel()
        {
            if (holdRemaining > 0f)
                return $"Held {Mathf.CeilToInt(holdRemaining / 60f)} min";

            switch (mode)
            {
                case DMLootChestMode.SingleLoot:
                    return "One visit";
                case DMLootChestMode.Story:
                    return "Story";
                case DMLootChestMode.Storage:
                    return "Storage";
                default:
                    if (!relootTimerStarted || IsProtected)
                        return string.Empty;
                    int total = Mathf.CeilToInt(Mathf.Max(0f, relootRemaining));
                    return $"{total / 60}:{total % 60:00} left";
            }
        }

        private IEnumerator OpenSequence(Transform playerTransform)
        {
            phase = DMLootChestPhase.Opening;
            WorldUseController.Unregister(this);

            TryPlayPlayerLootAnimation(playerTransform);
            if (openParticle != null)
                openParticle.SetActive(true);
            lid?.Open();
            GameAudioManager.Instance?.PlayLootChestOpen(transform.position);

            // Timer option FirstOpen (6.1): the reloot window starts on the first open (it still only ticks while closed).
            DMLootChestProfile profile = DMLootChestProfile.Live;
            if (mode == DMLootChestMode.WorldReloot && !relootTimerStarted && !IsProtected
                && profile != null && profile.timerStart == DMLootChestTimerStart.FirstOpen)
                StartRelootTimer();

            // Unscaled: the lid and the delay keep running even if another overlay paused time (7.4).
            float delay = ResolveOpenDelay();
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            openRoutine = null;
            DmEvents events = Events;
            bool shown = events != null && events.HasRemainingLoot && events.OpenLootDialogFromCollection();
            opened |= shown;
            if (shown)
            {
                phase = DMLootChestPhase.Open;
                yield break;
            }

            // Window did not open (another modal, emptied meanwhile): close the lid again.
            BeginClose();
        }

        private void HandleLootWindowClosed(DmEvents events)
        {
            if (phase != DMLootChestPhase.Open && phase != DMLootChestPhase.Opening)
                return;

            bool leftovers = events != null && events.HasRemainingLoot;
            bool protectedLeft = leftovers && events.HasProtectedEntries;
            fullInventoryHold = leftovers && events.FullInventoryHoldRequested;
            if (leftovers)
                exitedWithLeftovers = true;

            ApplyExitRules(leftovers, protectedLeft);

            if (openRoutine != null)
            {
                StopCoroutine(openRoutine);
                openRoutine = null;
            }

            BeginClose();
        }

        /// <summary>Loot plan 7.5 / 11 / D2 / D4 / D19: what a window exit starts.</summary>
        private void ApplyExitRules(bool leftovers, bool protectedLeft)
        {
            if (NeverExpires)
            {
                holdRemaining = 0f;
                return;
            }

            DMLootChestProfile profile = DMLootChestProfile.Live;
            float holdSeconds = profile != null ? profile.fullInventoryHoldSeconds : 1800f;
            float dissolveSeconds = ResolveDissolveSeconds();

            // D4 / D19: a refused loot starts the hold; any later exit with leftovers restarts it at the full length.
            if (!leftovers)
                holdRemaining = 0f;
            else if (fullInventoryHold || holdRemaining > 0f)
                holdRemaining = holdSeconds;
            fullInventoryHold = holdRemaining > 0f;

            if (mode == DMLootChestMode.SingleLoot)
            {
                bool held = leftovers && (protectedLeft || holdRemaining > 0f);
                singleLootLocked = !held;
                if (!held)
                    StartGoneCountdown(profile != null ? profile.postExitDestroySeconds : 5f, dissolveSeconds);
            }
            else if (!leftovers)
            {
                // WorldReloot emptied: dissolves emptiedDissolveDelay after the window closes.
                float delay = profile != null ? profile.emptiedDissolveDelay : 2f;
                StartGoneCountdown(delay + dissolveSeconds, dissolveSeconds);
            }
            else if (!relootTimerStarted && !protectedLeft && holdRemaining <= 0f)
            {
                // 11: the 120 s window starts at the first exit with leftovers and is never extended.
                StartRelootTimer();
            }

            if (holdRemaining > 0f || goneCountdown >= 0f || relootTimerStarted)
                DMLootChestRuntime.Track(this);
        }

        private void StartRelootTimer()
        {
            DMLootChestProfile profile = DMLootChestProfile.Live;
            relootTimerStarted = true;
            relootRemaining = profile != null ? profile.relootWindowSeconds : 120f;
            DMLootChestRuntime.Track(this);
        }

        private float goneDissolveSeconds;

        private void StartGoneCountdown(float seconds, float dissolveSeconds)
        {
            if (goneCountdown >= 0f)
                return;

            goneCountdown = Mathf.Max(0.01f, seconds);
            goneDissolveSeconds = Mathf.Clamp(dissolveSeconds, 0.01f, goneCountdown);
            WorldUseController.Unregister(this);
            DMLootChestRuntime.Track(this);
        }

        private static float ResolveDissolveSeconds()
        {
            DMLootChestProfile profile = DMLootChestProfile.Live;
            return profile != null ? Mathf.Max(0.01f, profile.EffectiveDissolveSeconds) : 1.6f;
        }

        /// <summary>
        /// Called by <see cref="DMLootChestRuntime"/> with scaled delta time. Returns false when nothing is left to tick.
        /// Never ticks while the window is up (Opening / Open), per loot plan 9.5 / 11.
        /// </summary>
        internal bool TickRuntime(float deltaTime)
        {
            if (phase == DMLootChestPhase.Gone || NeverExpires || this == null)
                return false;

            if (phase == DMLootChestPhase.Opening || phase == DMLootChestPhase.Open)
                return true;

            DmEvents events = Events;
            bool leftovers = events != null && events.HasRemainingLoot;

            // Full-inventory hold (D4 / D19): pauses every other timer; ends emptied or after the full time.
            if (holdRemaining > 0f)
            {
                if (!leftovers)
                {
                    holdRemaining = 0f;
                }
                else
                {
                    holdRemaining -= deltaTime;
                    if (holdRemaining > 0f)
                        return true;
                    holdRemaining = 0f;
                    OnHoldEnded();
                }
            }

            if (goneCountdown >= 0f)
            {
                goneCountdown -= deltaTime;
                if (phase != DMLootChestPhase.Dissolving && goneCountdown <= goneDissolveSeconds)
                    BeginDissolve(Mathf.Max(0.01f, goneCountdown));
                if (phase == DMLootChestPhase.Dissolving)
                    dissolve?.Tick(deltaTime);
                if (goneCountdown > 0f)
                    return true;

                GoGone();
                return false;
            }

            // Protected entries (D2): the reloot window neither ticks nor expires.
            if (relootTimerStarted && leftovers && !IsProtected)
            {
                relootRemaining -= deltaTime;
                if (relootRemaining <= 0f)
                {
                    relootRemaining = 0f;
                    float dissolveSeconds = ResolveDissolveSeconds();
                    StartGoneCountdown(dissolveSeconds, dissolveSeconds);
                }

                return true;
            }

            return relootTimerStarted && leftovers;
        }

        private void OnHoldEnded()
        {
            fullInventoryHold = false;
            DMLootChestProfile profile = DMLootChestProfile.Live;
            if (IsProtected)
                return;

            if (mode == DMLootChestMode.SingleLoot)
            {
                // D19: a Single Loot chest dissolves 5 s after the hold ends.
                singleLootLocked = true;
                StartGoneCountdown(profile != null ? profile.postExitDestroySeconds : 5f, ResolveDissolveSeconds());
                return;
            }

            // D19: World Reloot continues its remaining window (starting it if it never started).
            if (!relootTimerStarted)
                StartRelootTimer();
        }

        private void BeginDissolve(float seconds)
        {
            phase = DMLootChestPhase.Dissolving;
            WorldUseController.Unregister(this);
            if (openParticle != null)
                openParticle.SetActive(false);
            Events?.SetVisibleToScanner(false);

            if (dissolve == null)
                dissolve = new DMChestDissolve(ResolveChestRoot());
            dissolve.Begin(seconds);
            GameAudioManager.Instance?.PlayLootChestDissolve(transform.position);
        }

        /// <summary>D1: gone for good this session. Hidden, not destroyed, so phase 5 can restore it on load / New Game.</summary>
        private void GoGone()
        {
            phase = DMLootChestPhase.Gone;
            goneCountdown = -1f;
            relootTimerStarted = false;
            holdRemaining = 0f;
            WorldUseController.Unregister(this);
            DMLootChestRuntime.Untrack(this);
            Events?.SetVisibleToScanner(false);
            if (openParticle != null)
                openParticle.SetActive(false);

            Transform root = ResolveChestRoot();
            dissolve?.Restore();
            if (root != null)
                root.gameObject.SetActive(false);
        }

        private Transform ResolveChestRoot()
        {
            DmEvents events = Events;
            if (events != null)
                return events.transform;
            return chestAnimation != null ? chestAnimation.transform : transform;
        }

        private void BeginClose()
        {
            phase = DMLootChestPhase.Closing;
            if (openParticle != null)
                openParticle.SetActive(false);
            GameAudioManager.Instance?.PlayLootChestClose(transform.position);

            if (lid != null && isActiveAndEnabled)
            {
                lid.Close(FinishClose);
                return;
            }

            FinishClose();
        }

        private void FinishClose()
        {
            if (phase != DMLootChestPhase.Closing)
                return;

            phase = DMLootChestPhase.Closed;
            RefreshRegistration();
        }

        private void RefreshRegistration()
        {
            if (CanInteract())
                WorldUseController.Register(this);
            else
                WorldUseController.Unregister(this);
        }

        private float ResolveOpenDelay()
        {
            DMLootChestProfile profile = DMLootChestProfile.Live;
            float delay = profile != null ? profile.openLootWindowDelay : openLootDialogDelay;
            return Mathf.Max(0f, delay);
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
            if (chestId != null)
                chestId = chestId.Trim();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.83f, 0.63f, 0.09f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
#endif
    }
}
