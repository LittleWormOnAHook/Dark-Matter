using System.Collections;
using System.Collections.Generic;
using Project.Core;
using Project.Data;
using Project.Inventory;
using Project.Quests;
using Project.UI;
using UnityEngine;

namespace Project.Audio.Voice
{
    /// <summary>
    /// 2026-11-03o. Fires a voice line when the player gains an item. Listens to the live
    /// InventorySystem.OnInventoryChanged and diffs per-item counts against a baseline, so no
    /// inventory code had to change. Optional filter: one ItemData asset or an item id string
    /// (StableItemId, asset name or display name); both empty = any pickup.
    /// Gains are ignored (baseline only) while the game is not in Playing phase, while time is
    /// frozen (save load / boot) and, by default, while a menu holds the gameplay input
    /// (crafting, vendor, storage crate), so loads and crate withdrawals do not count as pickups.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Dark Matter/Voice/Voice Item Pickup Trigger")]
    public sealed class DMVoiceItemPickupTrigger : MonoBehaviour
    {
        [Header("Line")]
        public DMVoiceLineRef line;
        public string stageTag = "";
        [Tooltip("Play only once per save. Ignored for barks.")]
        public bool playOnce = true;
        public DMVoicePriority priority = DMVoicePriority.Story;
        public DMVoiceOutputMode outputMode = DMVoiceOutputMode.Narration;
        [Tooltip("World mode only: speaker transform. Defaults to the player.")]
        public Transform worldTarget;
        [Range(0f, 2f)] public float volumeScale = 1f;

        [Header("Item filter (both empty = any item)")]
        public ItemData item;
        [Tooltip("Matches StableItemId, asset name or itemName (case-insensitive).")]
        public string itemId = "";
        [Tooltip("Minimum amount gained in one change to fire.")]
        [Min(1)] public int minAmount = 1;
        [Tooltip("Treat gains while a menu holds gameplay input (crafting, vendor, storage) as not-a-pickup.")]
        public bool ignoreWhileMenuOpen = true;
        [Tooltip("Gains in the first seconds after the player inventory is found (new game starter kit) only set the baseline.")]
        [Min(0f)] public float ignoreSecondsAfterBind = 1f;

        [Header("Delay")]
        [Min(0f)] public float delaySeconds;
        [Tooltip("Extra random delay added on top of delaySeconds, picked in [min, max] each time. Max 0 = off.")]
        [Min(0f)] public float randomDelayMin;
        [Min(0f)] public float randomDelayMax;

        [Header("Quest condition (optional)")]
        public string requiredQuestId = "";
        public QuestStatus requiredStatus = QuestStatus.Active;
        public string forbiddenQuestId = "";
        public QuestStatus forbiddenStatus = QuestStatus.Completed;
        public int requiredObjectiveIndex = -1;
        public int requiredObjectiveCount = 1;

        /// <summary>Raised after a matching gain is detected (before the delay).</summary>
        public event System.Action<ItemData, int> OnMatchingPickup;

        private readonly Dictionary<ItemData, int> baseline = new Dictionary<ItemData, int>();
        private readonly Dictionary<ItemData, int> scratch = new Dictionary<ItemData, int>();
        private InventorySystem inventory;
        private bool fired;
        private Coroutine pendingRoutine;
        private float nextBindAttempt;
        private float ignoreUntilUnscaled;
        private bool dirty;

        private void OnEnable() => TryBind();

        private void OnDisable()
        {
            Unbind();
            if (pendingRoutine != null)
            {
                StopCoroutine(pendingRoutine);
                pendingRoutine = null;
            }
        }

        private void Update()
        {
            // The player (and its inventory) can spawn or be replaced after this object enables.
            if (inventory != null && PlayerLocator.IsLive(inventory))
                return;

            if (Time.unscaledTime < nextBindAttempt)
                return;

            nextBindAttempt = Time.unscaledTime + 0.5f;
            Unbind();
            TryBind();
        }

        private void TryBind()
        {
            InventorySystem live = PlayerLocator.FindLiveInventory();
            if (live == null)
                return;

            inventory = live;
            inventory.OnInventoryChanged += HandleInventoryChanged;
            Snapshot(baseline);
            dirty = false;
            ignoreUntilUnscaled = Time.unscaledTime + ignoreSecondsAfterBind;
        }

        private void Unbind()
        {
            if (inventory != null)
                inventory.OnInventoryChanged -= HandleInventoryChanged;
            inventory = null;
            baseline.Clear();
        }

        // Deferred to LateUpdate: a save load restores the inventory before the voice play-once keys,
        // so the "save applied this frame" flag is only reliable once the whole load call has returned.
        private void HandleInventoryChanged() => dirty = true;

        private void LateUpdate()
        {
            if (!dirty)
                return;

            dirty = false;
            EvaluateChange();
        }

        private void EvaluateChange()
        {
            if (inventory == null)
                return;

            Snapshot(scratch);
            bool countGains = GameSession.HasStarted
                && Time.timeScale > 0f
                && Time.unscaledTime >= ignoreUntilUnscaled
                && !DMVoiceManager.SaveAppliedRecently()
                && !(ignoreWhileMenuOpen && GameplayKeyboardShortcuts.IsGameplayInputLockedByUi());

            ItemData gainedItem = null;
            int gainedAmount = 0;
            if (countGains)
            {
                foreach (KeyValuePair<ItemData, int> pair in scratch)
                {
                    baseline.TryGetValue(pair.Key, out int before);
                    int gain = pair.Value - before;
                    if (gain < minAmount || !Matches(pair.Key))
                        continue;

                    gainedItem = pair.Key;
                    gainedAmount = gain;
                    break;
                }
            }

            baseline.Clear();
            foreach (KeyValuePair<ItemData, int> pair in scratch)
                baseline[pair.Key] = pair.Value;

            if (gainedItem != null)
                OnPickup(gainedItem, gainedAmount);
        }

        private void Snapshot(Dictionary<ItemData, int> into)
        {
            into.Clear();
            if (inventory == null || inventory.slots == null)
                return;

            for (int i = 0; i < inventory.slots.Count; i++)
            {
                InventorySystem.InventorySlot slot = inventory.slots[i];
                if (slot == null || slot.IsEmpty)
                    continue;

                into.TryGetValue(slot.item, out int count);
                into[slot.item] = count + slot.amount;
            }
        }

        /// <summary>True when the given item passes this trigger's filter.</summary>
        public bool Matches(ItemData candidate)
        {
            if (candidate == null)
                return false;

            if (item != null && candidate != item)
                return false;

            if (string.IsNullOrWhiteSpace(itemId))
                return true;

            string id = itemId.Trim();
            return string.Equals(candidate.StableItemId, id, System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(candidate.name, id, System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(candidate.itemName, id, System.StringComparison.OrdinalIgnoreCase);
        }

        private void OnPickup(ItemData picked, int amount)
        {
            if ((fired && playOnce) || pendingRoutine != null)
                return;

            if (!PassesGate())
                return;

            OnMatchingPickup?.Invoke(picked, amount);

            float delay = DMVoiceTriggerUtility.ResolveDelay(delaySeconds, randomDelayMin, randomDelayMax);
            if (delay > 0.01f)
                pendingRoutine = StartCoroutine(FireAfterDelay(delay));
            else
                Fire();
        }

        private IEnumerator FireAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            pendingRoutine = null;
            if (fired && playOnce)
                yield break;
            Fire();
        }

        private void Fire()
        {
            Transform target = worldTarget;
            if (target == null && inventory != null)
                target = inventory.transform;

            DMVoicePlayOptions options = DMVoiceTriggerUtility.BuildOptions(priority, outputMode, target, volumeScale);
            if (DMVoiceTriggerUtility.Fire(line, stageTag, playOnce, options))
                fired = true;
        }

        private bool PassesGate() =>
            DMVoiceTriggerUtility.PassesQuestGate(
                requiredQuestId, requiredStatus, forbiddenQuestId, forbiddenStatus,
                requiredObjectiveIndex, requiredObjectiveCount);
    }
}
