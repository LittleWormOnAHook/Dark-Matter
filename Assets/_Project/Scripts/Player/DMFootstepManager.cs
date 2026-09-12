using Invector;
using Project.Audio;
using Project.Core;
using UnityEngine;

namespace Project.Player
{
    /// <summary>
    /// Applies <see cref="DMFootstepProfile.Live"/> to Invector footsteps and resolves tag / terrain-layer 0-10 overrides.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-540)]
    public sealed class DMFootstepManager : MonoBehaviour
    {
        private vFootStep footStep;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOnPlayer()
        {
            if (!Application.isPlaying)
                return;

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player == null)
                player = GameObject.Find("Player_v7 Variant") ?? GameObject.Find("Player_v7");
            if (player == null || player.GetComponent<DMFootstepManager>() != null)
                return;

            if (player.GetComponent<vFootStep>() == null)
                return;

            player.AddComponent<DMFootstepManager>();
        }

        public static string ResolveTag(FootStepObject step)
        {
            if (step == null || step.ground == null)
                return string.Empty;

            FootstepSurface surface = step.ground.GetComponentInParent<FootstepSurface>();
            if (surface != null && !string.IsNullOrEmpty(surface.SurfaceTag))
                return surface.SurfaceTag;

            return step.ground.tag;
        }

        private static Terrain cachedTerrain;
        private static int cachedMapX = int.MinValue;
        private static int cachedMapZ = int.MinValue;
        private static int cachedLayerIndex = -1;

        public static int SampleTerrainLayerIndex(Terrain terrain, Vector3 worldPos)
        {
            if (terrain == null || terrain.terrainData == null)
                return -1;

            TerrainData data = terrain.terrainData;
            Vector3 origin = terrain.transform.position;
            int mapX = Mathf.Clamp(
                (int)(((worldPos.x - origin.x) / data.size.x) * data.alphamapWidth),
                0,
                Mathf.Max(0, data.alphamapWidth - 1));
            int mapZ = Mathf.Clamp(
                (int)(((worldPos.z - origin.z) / data.size.z) * data.alphamapHeight),
                0,
                Mathf.Max(0, data.alphamapHeight - 1));

            if (terrain == cachedTerrain && mapX == cachedMapX && mapZ == cachedMapZ)
                return cachedLayerIndex;

            // Dominant splat only — blended sand/rock/mud must not spawn one puff per overlapping layer.
            float[,,] splat = data.GetAlphamaps(mapX, mapZ, 1, 1);
            int best = -1;
            float bestWeight = 0f;
            int count = splat.GetUpperBound(2) + 1;
            for (int i = 0; i < count; i++)
            {
                float weight = splat[0, 0, i];
                if (weight > bestWeight)
                {
                    bestWeight = weight;
                    best = i;
                }
            }

            cachedTerrain = terrain;
            cachedMapX = mapX;
            cachedMapZ = mapZ;
            cachedLayerIndex = best;
            return best;
        }

        public static DMFootstepResolved Resolve(FootStepObject step)
        {
            DMFootstepProfile profile = DMFootstepProfile.Live;
            if (profile == null)
            {
                return new DMFootstepResolved(1f, true, true, null, null, null);
            }

            int layerIndex = step != null ? step.terrainLayerIndex : -1;
            return profile.Resolve(ResolveTag(step), layerIndex);
        }

        private void Awake()
        {
            footStep = GetComponent<vFootStep>();
            ApplyGlobals();
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying)
                return;

            ApplyGlobals();
        }

        private void ApplyGlobals()
        {
            DMFootstepProfile profile = DMFootstepProfile.Live;
            if (profile == null)
                return;

            if (footStep == null)
                footStep = GetComponent<vFootStep>();
            if (footStep == null)
                return;

            if (footStep.Volume == profile.volume
                && footStep.SpawnParticle == profile.spawnParticle
                && footStep.SpawnStepMark == profile.spawnStepMark
                && footStep.UseTriggerEnter == profile.useTriggerEnter
                && footStep.debugTextureName == profile.debugTextureName)
            {
                return;
            }

            footStep.Volume = profile.volume;
            footStep.SpawnParticle = profile.spawnParticle;
            footStep.SpawnStepMark = profile.spawnStepMark;
            footStep.UseTriggerEnter = profile.useTriggerEnter;
            footStep.debugTextureName = profile.debugTextureName;
        }
    }
}
