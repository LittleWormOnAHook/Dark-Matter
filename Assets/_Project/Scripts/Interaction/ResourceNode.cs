using UnityEngine;
using Project.Data;
using Project.UI;

namespace Project.Interaction
{
    public class ResourceNode : MonoBehaviour, IDamageable
    {
        public ItemData resourceItem;
        public int amountPerGather = 1;
        public int maxHits = 3;

        private int currentHits = 0;
        private int miningPassIndex;
        private float miningPassProgress;
        private float miningProgressRetainUntil = -1f;

        public ItemData ResourceItem => resourceItem;
        public int MiningPassIndex => miningPassIndex;
        public float MiningPassProgress01 => Mathf.Clamp01(miningPassProgress);
        public float OverallMiningProgress01(int passesRequired)
        {
            int passes = Mathf.Max(1, passesRequired);
            return Mathf.Clamp01((miningPassIndex + Mathf.Clamp01(miningPassProgress)) / passes);
        }

        public bool IsFullyMined(int passesRequired) =>
            miningPassIndex >= Mathf.Max(1, passesRequired);

        private void Update()
        {
            if (miningProgressRetainUntil > 0f && Time.time > miningProgressRetainUntil)
            {
                // Keep completed passes; only decay in-progress fraction after timeout.
                miningPassProgress = 0f;
                miningProgressRetainUntil = -1f;
            }
        }

        public void Gather(ResourceGatherer gatherer)
        {
            Gather(gatherer, 1);
        }

        public void Gather(ResourceGatherer gatherer, int hitStrength)
        {
            currentHits++;
            currentHits += Mathf.Max(0, hitStrength - 1);
            if (currentHits >= maxHits && gatherer != null && resourceItem != null)
            {
                if (gatherer.TryGather(resourceItem, amountPerGather))
                    FinishGatherAndDestroy();
            }
        }

        public void TakeDamage(float damage, GameObject source, bool isCritical = false)
        {
            currentHits += Mathf.Max(1, Mathf.RoundToInt(damage));
            if (currentHits >= maxHits)
            {
                ResourceGatherer gatherer = source != null
                    ? source.GetComponentInParent<ResourceGatherer>()
                    : null;
                if (gatherer == null)
                    gatherer = FindAnyObjectByType<ResourceGatherer>();

                if (gatherer != null && resourceItem != null)
                    gatherer.TryGather(resourceItem, amountPerGather);

                FinishGatherAndDestroy();
            }
        }

        /// <summary>
        /// Advances mining progress while Fire is held. Returns true when a pass completes this frame.
        /// On the final pass completion, grants inventory items and destroys the node.
        /// </summary>
        public bool TickMining(
            ResourceGatherer gatherer,
            float deltaTime,
            float passDuration,
            int passesRequired,
            int dropMin,
            int dropMax,
            float progressRetainSeconds,
            out bool finishedNode,
            out int grantedAmount)
        {
            finishedNode = false;
            grantedAmount = 0;

            if (gatherer == null || resourceItem == null)
                return false;

            int passes = Mathf.Max(1, passesRequired);
            float duration = Mathf.Max(0.05f, passDuration);
            miningProgressRetainUntil = Time.time + Mathf.Max(0.1f, progressRetainSeconds);

            if (miningPassIndex >= passes)
            {
                finishedNode = true;
                return false;
            }

            miningPassProgress += deltaTime / duration;
            if (miningPassProgress < 1f)
                return false;

            miningPassProgress = 0f;
            miningPassIndex++;

            if (miningPassIndex < passes)
                return true;

            int min = Mathf.Max(1, Mathf.Min(dropMin, dropMax));
            int max = Mathf.Max(min, Mathf.Max(dropMin, dropMax));
            grantedAmount = Random.Range(min, max + 1);
            if (gatherer.TryGather(resourceItem, grantedAmount))
            {
                finishedNode = true;
                FinishGatherAndDestroy();
            }
            else
            {
                // Inventory full — roll back final pass so the player can try again.
                miningPassIndex = Mathf.Max(0, passes - 1);
                miningPassProgress = 0.99f;
                grantedAmount = 0;
            }

            return true;
        }

        public void NotifyMiningInterrupted(float progressRetainSeconds)
        {
            miningProgressRetainUntil = Time.time + Mathf.Max(0.1f, progressRetainSeconds);
        }

        private void FinishGatherAndDestroy()
        {
            ItemPickup pickup = GetComponent<ItemPickup>();
            if (pickup != null)
                PickupProximityDotUI.Unregister(pickup);

            Destroy(gameObject);
        }
    }
}
