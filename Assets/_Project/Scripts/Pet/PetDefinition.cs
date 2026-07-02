using UnityEngine;

namespace Project.Pet
{
    [CreateAssetMenu(menuName = "Survival Pioneer/Pet Definition", fileName = "PetDefinition")]
    public class PetDefinition : ScriptableObject
    {
        public string petId = "fox_cub";
        public string displayName = "Fox Cub";
        [TextArea] public string description = "A loyal companion that gathers nearby items.";
        public Sprite inventoryIcon;
        public GameObject worldPrefab;

        [Header("Taming")]
        public string requiredItemId;
        public float tameDurationSeconds = 8f;
        public float progressPerInteract = 0.2f;
    }
}
