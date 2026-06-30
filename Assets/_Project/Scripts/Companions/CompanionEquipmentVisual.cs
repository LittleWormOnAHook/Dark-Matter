using Project.Data;
using Project.Interaction;
using UnityEngine;

namespace Project.Companions
{
    /// <summary>
    /// Draws a pioneer's weapon in hand during combat; sheathes on the back otherwise.
    /// </summary>
    public class CompanionEquipmentVisual : MonoBehaviour
    {
        [SerializeField] private string defaultSocketName = "RightHand";
        [SerializeField] private string defaultSheatheSocketName = "Spine";

        private GameObject handInstance;
        private GameObject sheathedInstance;
        private ItemData equippedWeapon;
        private string equippedWeaponId = string.Empty;
        private bool isDrawn;

        public ItemData EquippedWeapon => equippedWeapon;

        public void ApplyWeapon(string weaponItemId, bool drawn = false)
        {
            equippedWeaponId = weaponItemId ?? string.Empty;
            isDrawn = drawn;
            RefreshVisual();
        }

        public void SetDrawn(bool drawn)
        {
            if (isDrawn == drawn)
                return;

            isDrawn = drawn;
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            ClearVisuals();

            equippedWeapon = ItemRegistry.Resolve(equippedWeaponId);
            if (equippedWeapon == null || equippedWeapon.heldPrefab == null)
                return;

            if (isDrawn)
                handInstance = SpawnVisual(equippedWeapon, isHand: true);
            else
                sheathedInstance = SpawnVisual(equippedWeapon, isHand: false);
        }

        private GameObject SpawnVisual(ItemData item, bool isHand)
        {
            Transform modelRoot = ResolveModelRoot();
            if (modelRoot == null)
                return null;

            string socketName = isHand
                ? (string.IsNullOrWhiteSpace(item.equipSocketName) ? defaultSocketName : item.equipSocketName)
                : (string.IsNullOrWhiteSpace(item.sheatheSocketName) ? defaultSheatheSocketName : item.sheatheSocketName);

            Transform socket = FindDeepChild(modelRoot, socketName);
            if (socket == null)
            {
                Debug.LogWarning($"CompanionEquipmentVisual: socket '{socketName}' not found on {name}.");
                return null;
            }

            GameObject instance = Instantiate(item.heldPrefab, socket);
            StripForHeld(instance);

            Transform t = instance.transform;
            if (isHand)
            {
                Vector3 scale = item.heldLocalScale == Vector3.zero ? Vector3.one : item.heldLocalScale;
                t.localPosition = item.heldLocalPosition;
                t.localRotation = item.useHeldLocalRotation
                    ? item.heldLocalRotation
                    : Quaternion.Euler(item.heldLocalEuler);
                t.localScale = scale;
            }
            else
            {
                Vector3 scale = item.sheathedLocalScale == Vector3.zero ? Vector3.one : item.sheathedLocalScale;
                t.localPosition = item.sheathedLocalPosition;
                t.localRotation = item.useSheathedLocalRotation
                    ? item.sheathedLocalRotation
                    : Quaternion.Euler(item.sheathedLocalEuler);
                t.localScale = scale;
            }

            return instance;
        }

        private Transform ResolveModelRoot()
        {
            Transform model = transform.Find("ProjectUnityCharacter");
            return model != null ? model : transform;
        }

        private void ClearVisuals()
        {
            if (handInstance != null)
            {
                Destroy(handInstance);
                handInstance = null;
            }

            if (sheathedInstance != null)
            {
                Destroy(sheathedInstance);
                sheathedInstance = null;
            }

            if (!isDrawn)
                equippedWeapon = ItemRegistry.Resolve(equippedWeaponId);
        }

        private static Transform FindDeepChild(Transform parent, string childName)
        {
            if (parent == null)
                return null;

            if (parent.name == childName)
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindDeepChild(parent.GetChild(i), childName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static void StripForHeld(GameObject instance)
        {
            if (instance.GetComponent<EquippedVisualMarker>() == null)
                instance.AddComponent<EquippedVisualMarker>();

            foreach (ItemPickup pickup in instance.GetComponentsInChildren<ItemPickup>(true))
                Destroy(pickup);

            foreach (ResourceNode node in instance.GetComponentsInChildren<ResourceNode>(true))
                Destroy(node);

            foreach (WeaponHitbox hitbox in instance.GetComponentsInChildren<WeaponHitbox>(true))
                Destroy(hitbox);

            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;

            foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.detectCollisions = false;
            }
        }
    }
}
