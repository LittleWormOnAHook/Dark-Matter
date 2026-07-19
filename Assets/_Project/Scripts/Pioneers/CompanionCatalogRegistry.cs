using UnityEngine;

namespace Project.Pioneers
{
    /// <summary>
    /// Lightweight index pointing at NamedPioneerDefinition data assets (kept under
    /// Assets/_Project/Data/Companions) so they can be discovered at runtime via Resources.Load
    /// without requiring the data assets themselves to live inside a Resources folder — mirrors
    /// Project.Data.ItemRegistry's pattern for items.
    /// </summary>
    [CreateAssetMenu(fileName = "CompanionCatalogRegistry", menuName = "Survival Pioneer/Companions/Companion Catalog Registry")]
    public class CompanionCatalogRegistry : ScriptableObject
    {
        [SerializeField] private NamedPioneerDefinition[] companions = System.Array.Empty<NamedPioneerDefinition>();

        public NamedPioneerDefinition[] Companions => companions;
    }
}
