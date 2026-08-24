using UnityEngine;

namespace Project.Map
{
    /// <summary>
    /// Scene reference for the authored world-map art plane (hierarchy "MAP art").
    /// Supplies the biome map texture used by minimap/full map UI.
    /// </summary>
    [DisallowMultipleComponent]
    public class WorldMapArtReference : MonoBehaviour
    {
        private const float UnityPlaneMeshSpanMeters = 10f;

        [SerializeField] private MeshRenderer mapRenderer;
        [SerializeField] private bool invertMapVertical;

        public bool InvertMapVertical => invertMapVertical;

        public static WorldMapArtReference FindInScene()
        {
            WorldMapArtReference[] references = FindObjectsByType<WorldMapArtReference>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            return references.Length > 0 ? references[0] : null;
        }

        public bool TryGetMapTexture(out Texture2D texture)
        {
            texture = null;
            EnsureRenderer();

            if (mapRenderer == null)
                return false;

            Material material = mapRenderer.sharedMaterial;
            if (material == null)
                return false;

            texture = material.GetTexture("_BaseColorMap") as Texture2D;
            if (texture == null)
                texture = material.mainTexture as Texture2D;

            return texture != null;
        }

        /// <summary>World-space XZ bounds of the art plane mesh (for alignment checks).</summary>
        public Bounds GetArtWorldBounds()
        {
            EnsureRenderer();
            Vector3 scale = transform.lossyScale;
            Vector3 size = new Vector3(
                Mathf.Abs(scale.x) * UnityPlaneMeshSpanMeters,
                1f,
                Mathf.Abs(scale.z) * UnityPlaneMeshSpanMeters);
            return new Bounds(transform.position, size);
        }

        private void EnsureRenderer()
        {
            if (mapRenderer == null)
                mapRenderer = GetComponent<MeshRenderer>();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureRenderer();
        }
#endif
    }
}
