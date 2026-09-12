using System;
using UnityEngine;

namespace Project.Player
{
    /// <summary>
    /// Live footstep / footprint / dust tunables, including Unity tags and terrain splat layers 0-10.
    /// Play-mode Genesis Studio edits persist via DMProfilePlayModeSaver.
    /// </summary>
    [CreateAssetMenu(menuName = "Dark Matter/Player/Footstep Profile", fileName = "DM_FootstepProfile")]
    public sealed class DMFootstepProfile : ScriptableObject
    {
        public const string ResourcesPath = "Player/DM_FootstepProfile";
        public const int TerrainLayerSlotCount = 11;

        private static readonly string[] DefaultTerrainLayerLabels =
        {
            "0 Sand Fine",
            "1 Sand Coarse",
            "2 Rock Ground",
            "3 Scattered Stones",
            "4 Rock Path",
            "5 Cliffs",
            "6 Mud",
            "7 Snow",
            "8 Terrain Layer",
            "9 Terrain Layer",
            "10 Terrain Layer"
        };

        private static readonly string[] DefaultTags =
        {
            "Terrain",
            "Untagged",
            "Climbable",
            "Default"
        };

        private static DMFootstepProfile live;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLiveCache()
        {
            live = null;
        }

        public static DMFootstepProfile Live
        {
            get
            {
                if (live == null)
                    live = Resources.Load<DMFootstepProfile>(ResourcesPath);
                if (live != null)
                    live.EnsureSlots();
                return live;
            }
        }

        [Header("Triggers")]
        [Tooltip("Default Invector footstep volume when no tag or terrain layer overrides.")]
        [Range(0f, 1f)]
        public float volume = 1f;

        [Tooltip("Default: spawn dust VFX on each step.")]
        public bool spawnParticle = true;

        [Tooltip("Default: spawn the footprint quad on each step.")]
        public bool spawnStepMark = true;

        [Tooltip("Fire effects from foot trigger enter. Off = animation events only.")]
        public bool useTriggerEnter = true;

        [Tooltip("Ignore a second dust/mark/audio fire on the same foot inside this window (trigger + anim event, or blended clips).")]
        [Min(0.05f)]
        public float minSecondsBetweenFootVfx = 0.18f;

        [Tooltip("Log the resolved tag / terrain layer index on each step.")]
        public bool debugTextureName;

        [Header("Walkable layers")]
        [Tooltip("Allow footprints and dust on Default.")]
        public bool includeDefaultLayer = true;

        [Tooltip("Allow footprints and dust on Climbable (live Gaia tile).")]
        public bool includeClimbableLayer = true;

        [Tooltip("Any extra physics layers that should accept footprints and dust.")]
        public LayerMask extraWalkableLayers;

        [Header("Footprint mark")]
        public GameObject stepMarkPrefab;

        [Tooltip("Seconds before a spawned footprint is destroyed.")]
        [Min(0.1f)]
        public float stepMarkLifetime = 3f;

        [Header("Dust VFX")]
        public GameObject dustPrefab;

        public Material dustMaterial;

        [Tooltip("Unscaled seconds a dust puff stays alive.")]
        [Min(0.05f)]
        public float dustLifetimeSeconds = 1f;

        [Tooltip("Max live dust puffs. Oldest is recycled first.")]
        [Min(1)]
        public int maxLiveDust = 16;

        [Header("Unity tags")]
        [Tooltip("Used when the step hits a mesh (or a terrain with no enabled splat slot). First matching tag wins.")]
        public DMFootstepTagEntry[] tagEntries;

        [Header("Terrain texture layers (0-10)")]
        [Tooltip("Splat slots on Gaia tiles. Dominant layer index picks the row. Disabled rows fall through to tag / defaults.")]
        public DMFootstepTerrainLayerEntry[] terrainTextureLayers;

        public void EnsureSlots()
        {
            if (terrainTextureLayers == null || terrainTextureLayers.Length != TerrainLayerSlotCount)
            {
                var next = new DMFootstepTerrainLayerEntry[TerrainLayerSlotCount];
                for (int i = 0; i < TerrainLayerSlotCount; i++)
                {
                    if (terrainTextureLayers != null &&
                        i < terrainTextureLayers.Length &&
                        terrainTextureLayers[i] != null)
                    {
                        next[i] = terrainTextureLayers[i];
                    }
                    else
                    {
                        next[i] = CreateDefaultTerrainLayer(i);
                    }

                    next[i].layerIndex = i;
                    if (string.IsNullOrEmpty(next[i].label))
                        next[i].label = DefaultTerrainLayerLabels[i];
                }

                terrainTextureLayers = next;
            }

            if (tagEntries == null || tagEntries.Length == 0)
                tagEntries = CreateDefaultTags();
        }

        public int ResolveWalkableMask(LayerMask authored)
        {
            int mask = authored.value;
            if (includeDefaultLayer)
                mask |= 1;

            if (includeClimbableLayer)
            {
                int climbable = LayerMask.NameToLayer("Climbable");
                if (climbable >= 0)
                    mask |= 1 << climbable;
            }

            mask |= extraWalkableLayers.value;
            return mask == 0 ? 1 : mask;
        }

        public DMFootstepResolved Resolve(string tag, int terrainLayerIndex)
        {
            EnsureSlots();

            if (terrainLayerIndex >= 0 &&
                terrainLayerIndex < TerrainLayerSlotCount &&
                terrainTextureLayers != null &&
                terrainLayerIndex < terrainTextureLayers.Length)
            {
                DMFootstepTerrainLayerEntry layer = terrainTextureLayers[terrainLayerIndex];
                if (layer != null && layer.enabled)
                    return BuildResolved(layer.volume, layer.spawnParticle, layer.spawnStepMark, layer.stepMarkPrefab, layer.dustPrefab, layer.dustMaterial);
            }

            if (!string.IsNullOrEmpty(tag) && tagEntries != null)
            {
                for (int i = 0; i < tagEntries.Length; i++)
                {
                    DMFootstepTagEntry entry = tagEntries[i];
                    if (entry == null || !entry.enabled || string.IsNullOrEmpty(entry.tag))
                        continue;

                    if (string.Equals(entry.tag, tag, StringComparison.OrdinalIgnoreCase))
                        return BuildResolved(entry.volume, entry.spawnParticle, entry.spawnStepMark, entry.stepMarkPrefab, entry.dustPrefab, entry.dustMaterial);
                }
            }

            return BuildResolved(volume, spawnParticle, spawnStepMark, null, null, null);
        }

        private DMFootstepResolved BuildResolved(
            float resolvedVolume,
            bool resolvedSpawnParticle,
            bool resolvedSpawnMark,
            GameObject markOverride,
            GameObject dustOverride,
            Material dustMatOverride)
        {
            return new DMFootstepResolved(
                resolvedVolume,
                resolvedSpawnParticle,
                resolvedSpawnMark,
                markOverride != null ? markOverride : stepMarkPrefab,
                dustOverride != null ? dustOverride : dustPrefab,
                dustMatOverride != null ? dustMatOverride : dustMaterial);
        }

        private static DMFootstepTerrainLayerEntry CreateDefaultTerrainLayer(int index)
        {
            return new DMFootstepTerrainLayerEntry
            {
                layerIndex = index,
                label = DefaultTerrainLayerLabels[index],
                enabled = index < 8,
                volume = 1f,
                spawnParticle = true,
                spawnStepMark = true
            };
        }

        private static DMFootstepTagEntry[] CreateDefaultTags()
        {
            var entries = new DMFootstepTagEntry[DefaultTags.Length];
            for (int i = 0; i < DefaultTags.Length; i++)
            {
                entries[i] = new DMFootstepTagEntry
                {
                    tag = DefaultTags[i],
                    enabled = true,
                    volume = 1f,
                    spawnParticle = true,
                    spawnStepMark = true
                };
            }

            return entries;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureSlots();
        }
#endif
    }

    [Serializable]
    public sealed class DMFootstepTagEntry
    {
        [Tooltip("Unity tag on the collider / terrain (Terrain, Untagged, Climbable, Default).")]
        public string tag = "Untagged";

        public bool enabled = true;

        [Range(0f, 1f)]
        public float volume = 1f;

        public bool spawnParticle = true;
        public bool spawnStepMark = true;
        public GameObject stepMarkPrefab;
        public GameObject dustPrefab;
        public Material dustMaterial;
    }

    [Serializable]
    public sealed class DMFootstepTerrainLayerEntry
    {
        [Tooltip("Splat index on the terrain (0-10).")]
        [Range(0, 10)]
        public int layerIndex;

        [Tooltip("Authoring label. Live tiles: 0-1 sand, 2-5 rock/cliff, 6 mud, 7 snow.")]
        public string label = "Terrain Layer";

        [Tooltip("Off = this splat falls through to tag / default settings.")]
        public bool enabled = true;

        [Range(0f, 1f)]
        public float volume = 1f;

        public bool spawnParticle = true;
        public bool spawnStepMark = true;
        public GameObject stepMarkPrefab;
        public GameObject dustPrefab;
        public Material dustMaterial;
    }

    public readonly struct DMFootstepResolved
    {
        public readonly float volume;
        public readonly bool spawnParticle;
        public readonly bool spawnStepMark;
        public readonly GameObject stepMarkPrefab;
        public readonly GameObject dustPrefab;
        public readonly Material dustMaterial;

        public DMFootstepResolved(
            float volume,
            bool spawnParticle,
            bool spawnStepMark,
            GameObject stepMarkPrefab,
            GameObject dustPrefab,
            Material dustMaterial)
        {
            this.volume = volume;
            this.spawnParticle = spawnParticle;
            this.spawnStepMark = spawnStepMark;
            this.stepMarkPrefab = stepMarkPrefab;
            this.dustPrefab = dustPrefab;
            this.dustMaterial = dustMaterial;
        }
    }
}
