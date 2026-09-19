using UnityEngine;

namespace Project.World.TerrainSplat
{
    /// <summary>
    /// Optional per-terrain hook: applies splat render profile when enabled (Play or editor with ExecuteAlways).
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    [ExecuteAlways]
    public sealed class DMTerrainSplatRenderBootstrap : MonoBehaviour
    {
        [SerializeField] private Terrain terrain;
        [SerializeField] private DMTerrainSplatRenderProfile profileOverride;

        private void OnEnable()
        {
            if (terrain == null)
                terrain = GetComponent<Terrain>();

            DMTerrainSplatRenderProfile profile = profileOverride != null
                ? profileOverride
                : DMTerrainSplatRenderProfile.Live;

            if (profile == null)
                return;

            if (Application.isPlaying && !profile.autoApplyOnPlay)
                return;

            DMTerrainSplatRenderApplier.ApplyToTerrain(terrain, profile);
        }
    }
}
