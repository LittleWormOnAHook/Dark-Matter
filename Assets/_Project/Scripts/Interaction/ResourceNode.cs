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

        private void FinishGatherAndDestroy()
        {
            ItemPickup pickup = GetComponent<ItemPickup>();
            if (pickup != null)
                PickupProximityDotUI.Unregister(pickup);

            Destroy(gameObject);
        }
    }
}
