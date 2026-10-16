using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Project.Audio
{
    [CreateAssetMenu(menuName = "Project/Audio/Game Audio Profile", fileName = "GameAudioProfile")]
    public class GameAudioProfile : ScriptableObject
    {
        [Header("Music")]
        public AudioClip[] musicTracks;
        [Range(0f, 1f)] public float musicVolume = 0.45f;
        public bool shuffleMusic = true;
        public bool loopCurrentTrack = true;

        [Header("Loading")]
        public AudioClip loadingAmbience;
        [Range(0f, 1f)] public float loadingAmbienceVolume = 0.55f;

        public const int TerrainLayerSlotCount = 11;

        [Header("Footsteps")]
        [Tooltip("Fallback when a tag or terrain layer 0-10 has no clips of its own.")]
        public FootstepSurfaceSet defaultFootsteps = FootstepSurfaceSet.CreateDefault();
        [Tooltip("Unity tag matches (Terrain, Climbable, Default). Empty clip arrays fall back to Default.")]
        public FootstepSurfaceSet[] surfaceFootsteps;
        [Tooltip("Terrain splat slots 0-10. Disabled or empty slots fall back to tag, then Default.")]
        public FootstepTerrainLayerSet[] terrainLayerFootsteps;

        [Header("Landing")]
        public AudioClip[] defaultLandingClips;
        [Min(0.5f)] public float minLandingSpeed = 2.5f;
        [Min(0.5f)] public float hardLandingSpeed = 9f;
        [Range(0f, 1f)] public float landingVolume = 0.9f;

        [Header("Combat — Melee")]
        [FormerlySerializedAs("weaponSwingClips")]
        public AudioClip[] meleeSwingClips;
        [FormerlySerializedAs("weaponHitClips")]
        [InspectorName("Weapon / guard clash clips")]
        public AudioClip[] meleeHitClips;
        [FormerlySerializedAs("weaponCriticalHitClips")]
        [InspectorName("Critical / parry clash clips")]
        public AudioClip[] meleeCriticalHitClips;

        [Header("Combat — Body impacts (not weapon clashes)")]
        [Tooltip("Confirmed melee damage to a body. Empty means silent, never falls back to metallic clash clips.")]
        public AudioClip[] meleeBodyHitClips;
        public AudioClip[] meleeBodyCriticalHitClips;
        [Range(0f, 1f)] public float meleeBodyHitVolume = 0.95f;
        [Tooltip("Existing Melee Hit / Critical Hit libraries above are now weapon/guard clashes only.")]
        [Range(0f, 1f)] public float meleeClashVolume = 0.9f;
        [Header("Combat — Player receives damage")]
        public AudioClip[] playerBodyHitClips;
        public AudioClip[] playerPainClips;
        [Range(0f, 1f)] public float playerBodyHitVolume = 1f;
        [Range(0f, 1f)] public float playerPainVolume = 0.65f;
        [Min(0f)] public float playerPainMinInterval = 0.8f;

        [Header("Combat — Center circular hit direction")]
        public bool playerHitIndicatorEnabled = true;
        [Range(25f, 750f)] public float playerHitIndicatorRadius = 500f;
        [Range(1f, 16f)] public float playerHitIndicatorThickness = 5f;
        [Range(15f, 100f)] public float playerHitIndicatorArc = 55f;
        [Range(0.15f, 2f)] public float playerHitIndicatorSeconds = 0.65f;
        public Color playerHitIndicatorColor = Project.UI.DarkMatterGenesisUiPalette.RichFuchsia;

        [Header("Combat — Ranged")]
        public AudioClip[] rangedHitClips;
        public AudioClip[] rangedCriticalHitClips;
        public AudioClip[] punchSwingClips;
        public AudioClip[] punchHitClips;
        public AudioClip[] punchCriticalHitClips;
        public AudioClip[] resourceHitClips;
        [Tooltip("Short ding played three times (rising pitch) when poise breaks / stagger triggers.")]
        public AudioClip[] poiseStaggerDingClips;
        [Range(0.04f, 0.2f)] public float poiseStaggerDingSpacing = 0.075f;
        [Range(0f, 1f)] public float poiseStaggerDingVolume = 0.92f;
        [Range(0f, 1f)] public float combatVolume = 1f;

        [Header("UI")]
        public AudioClip[] buttonClickClips;
        public AudioClip[] uiDenyClips;
        public AudioClip[] inventoryItemClickClips;
        public AudioClip[] itemUseClips;
        public AudioClip[] itemEquipClips;
        public AudioClip[] itemUnequipClips;
        public AudioClip[] itemSplitClips;
        public AudioClip[] itemDropClips;
        public AudioClip[] itemPickupClips;
        public AudioClip[] achievementUnlockClips;
        public AudioClip[] levelUpClips;

        [Header("Loot Chests")]
        [Tooltip("World loot chest lid opening (3D at the chest). Empty = silent.")]
        public AudioClip[] lootChestOpenClips;
        [Tooltip("World loot chest lid closing. Empty = silent.")]
        public AudioClip[] lootChestCloseClips;
        [Tooltip("Loot chest dissolve start. Empty = silent.")]
        public AudioClip[] lootChestDissolveClips;
        [Range(0f, 1f)] public float lootChestVolume = 0.8f;
        [Range(0f, 1f)] public float uiVolume = 0.85f;

        [Header("3D Playback")]
        [Range(0f, 1f)] public float sfxSpatialBlend = 1f;
        public float sfxMinDistance = 1f;
        public float sfxMaxDistance = 22f;

        [NonSerialized] private FootstepSurfaceSet overlayScratch;

        [HideInInspector] public CombatAudioFolderPool[] combatFolderPools;

        public AudioClip[] ResolveCombatClips(string clipField, AudioClip[] fallback)
        {
            if (combatFolderPools != null)
                foreach (CombatAudioFolderPool pool in combatFolderPools)
                    if (pool != null && pool.clipField == clipField && !string.IsNullOrEmpty(pool.folderPath) && pool.clips != null)
                        foreach (AudioClip clip in pool.clips)
                            if (clip != null) return pool.clips;
            return fallback;
        }

        public FootstepSurfaceSet GetFootstepsForSurface(string surfaceTag)
        {
            return GetFootsteps(surfaceTag, -1);
        }

        public FootstepSurfaceSet GetFootsteps(string surfaceTag, int terrainLayerIndex)
        {
            EnsureTerrainLayerSlots();

            FootstepSurfaceSet chosen = null;
            if (TryGetTerrainLayer(terrainLayerIndex, out FootstepTerrainLayerSet layer))
                chosen = layer.ToSurfaceSet();
            else
                chosen = FindTagSet(surfaceTag);

            return OverlayOnDefault(chosen);
        }

        public AudioClip[] GetLandingClipsForSurface(string surfaceTag)
        {
            return GetLandingClips(surfaceTag, -1);
        }

        public AudioClip[] GetLandingClips(string surfaceTag, int terrainLayerIndex)
        {
            FootstepSurfaceSet set = GetFootsteps(surfaceTag, terrainLayerIndex);
            if (HasClips(set?.landingClips))
                return set.landingClips;

            if (HasClips(defaultLandingClips))
                return defaultLandingClips;

            return defaultFootsteps?.walkClips;
        }

        public void EnsureTerrainLayerSlots()
        {
            if (terrainLayerFootsteps != null && terrainLayerFootsteps.Length == TerrainLayerSlotCount)
                return;

            var next = new FootstepTerrainLayerSet[TerrainLayerSlotCount];
            for (int i = 0; i < TerrainLayerSlotCount; i++)
            {
                if (terrainLayerFootsteps != null &&
                    i < terrainLayerFootsteps.Length &&
                    terrainLayerFootsteps[i] != null)
                {
                    next[i] = terrainLayerFootsteps[i];
                }
                else
                {
                    next[i] = FootstepTerrainLayerSet.Create(i);
                }

                next[i].layerIndex = i;
                if (string.IsNullOrEmpty(next[i].label))
                    next[i].label = FootstepTerrainLayerSet.DefaultLabel(i);
            }

            terrainLayerFootsteps = next;
        }

        private bool TryGetTerrainLayer(int terrainLayerIndex, out FootstepTerrainLayerSet layer)
        {
            layer = null;
            if (terrainLayerIndex < 0 ||
                terrainLayerFootsteps == null ||
                terrainLayerIndex >= terrainLayerFootsteps.Length)
            {
                return false;
            }

            layer = terrainLayerFootsteps[terrainLayerIndex];
            return layer != null && layer.enabled && layer.HasOwnClips();
        }

        private FootstepSurfaceSet FindTagSet(string surfaceTag)
        {
            if (!string.IsNullOrEmpty(surfaceTag) && surfaceFootsteps != null)
            {
                for (int i = 0; i < surfaceFootsteps.Length; i++)
                {
                    FootstepSurfaceSet set = surfaceFootsteps[i];
                    if (set != null && string.Equals(set.surfaceTag, surfaceTag, StringComparison.OrdinalIgnoreCase))
                        return set;
                }
            }

            return defaultFootsteps;
        }

        private FootstepSurfaceSet OverlayOnDefault(FootstepSurfaceSet chosen)
        {
            FootstepSurfaceSet fallback = defaultFootsteps ?? FootstepSurfaceSet.CreateDefault();
            if (chosen == null || chosen == fallback)
                return fallback;

            if (overlayScratch == null)
                overlayScratch = new FootstepSurfaceSet();

            overlayScratch.surfaceTag = string.IsNullOrEmpty(chosen.surfaceTag) ? fallback.surfaceTag : chosen.surfaceTag;
            overlayScratch.walkClips = HasClips(chosen.walkClips) ? chosen.walkClips : fallback.walkClips;
            overlayScratch.runClips = HasClips(chosen.runClips) ? chosen.runClips : fallback.runClips;
            overlayScratch.landingClips = HasClips(chosen.landingClips) ? chosen.landingClips : fallback.landingClips;
            overlayScratch.walkStepDistance = chosen.walkStepDistance > 0.05f ? chosen.walkStepDistance : fallback.walkStepDistance;
            overlayScratch.runStepDistance = chosen.runStepDistance > 0.05f ? chosen.runStepDistance : fallback.runStepDistance;
            overlayScratch.volume = chosen.volume > 0f ? chosen.volume : fallback.volume;
            return overlayScratch;
        }

        private static bool HasClips(AudioClip[] clips)
        {
            return clips != null && clips.Length > 0;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureTerrainLayerSlots();
        }
#endif
    }

    [Serializable]
    public class CombatAudioFolderPool
    {
        public string clipField;
        public string folderPath;
        // Serialized direct references: folders need not be under Resources and
        // playback never depends on editor AssetDatabase or filesystem scans.
        public AudioClip[] clips;
    }

    [Serializable]
    public class FootstepSurfaceSet
    {
        public string surfaceTag = "Default";
        public AudioClip[] walkClips;
        public AudioClip[] runClips;
        public AudioClip[] landingClips;
        [Min(0.05f)] public float walkStepDistance = 2.1f;
        [Min(0.05f)] public float runStepDistance = 2.8f;
        [Range(0f, 1f)] public float volume = 0.85f;

        public static FootstepSurfaceSet CreateDefault()
        {
            return new FootstepSurfaceSet
            {
                surfaceTag = "Default",
                walkStepDistance = 2.1f,
                runStepDistance = 2.8f,
                volume = 0.85f
            };
        }
    }

    [Serializable]
    public class FootstepTerrainLayerSet
    {
        [Range(0, 10)]
        public int layerIndex;

        [Tooltip("Authoring label. Live tiles: 0-1 sand, 2-5 rock/cliff, 6 mud, 7 snow.")]
        public string label = "Terrain Layer";

        [Tooltip("Off or empty clips = use tag, then Default.")]
        public bool enabled = true;

        public AudioClip[] walkClips;
        public AudioClip[] runClips;
        public AudioClip[] landingClips;
        [Min(0.05f)] public float walkStepDistance = 2.1f;
        [Min(0.05f)] public float runStepDistance = 2.8f;
        [Range(0f, 1f)] public float volume = 0.85f;

        public bool HasOwnClips()
        {
            return (walkClips != null && walkClips.Length > 0) ||
                   (runClips != null && runClips.Length > 0) ||
                   (landingClips != null && landingClips.Length > 0);
        }

        public FootstepSurfaceSet ToSurfaceSet()
        {
            return new FootstepSurfaceSet
            {
                surfaceTag = label,
                walkClips = walkClips,
                runClips = runClips,
                landingClips = landingClips,
                walkStepDistance = walkStepDistance,
                runStepDistance = runStepDistance,
                volume = volume
            };
        }

        public static FootstepTerrainLayerSet Create(int index)
        {
            return new FootstepTerrainLayerSet
            {
                layerIndex = index,
                label = DefaultLabel(index),
                enabled = true,
                walkStepDistance = 2.1f,
                runStepDistance = 2.8f,
                volume = 0.85f
            };
        }

        public static string DefaultLabel(int index)
        {
            return index switch
            {
                0 => "0 Sand Fine",
                1 => "1 Sand Coarse",
                2 => "2 Rock Ground",
                3 => "3 Scattered Stones",
                4 => "4 Rock Path",
                5 => "5 Cliffs",
                6 => "6 Mud",
                7 => "7 Snow",
                _ => index + " Terrain Layer"
            };
        }
    }

    [Serializable]
    public class AmbientZoneLayer
    {
        public string layerName = "Ambient Layer";
        public AudioClip[] clips;
        [Min(0.5f)] public float minInterval = 4f;
        [Min(0.5f)] public float maxInterval = 12f;
        [Range(0f, 1f)] public float volume = 0.65f;
        [Range(0f, 1f)] public float spatialBlend = 1f;
        [Range(0.8f, 1.2f)] public float pitchMin = 0.95f;
        [Range(0.8f, 1.2f)] public float pitchMax = 1.05f;
        public bool playAtRandomPointInZone = true;

        public static AmbientZoneLayer CreateBirdsLayer()
        {
            return new AmbientZoneLayer
            {
                layerName = "Birds",
                minInterval = 6f,
                maxInterval = 18f,
                volume = 0.55f,
                spatialBlend = 1f
            };
        }

        public static AmbientZoneLayer CreateInsectsLayer()
        {
            return new AmbientZoneLayer
            {
                layerName = "Insects",
                minInterval = 2f,
                maxInterval = 7f,
                volume = 0.45f,
                spatialBlend = 1f
            };
        }

        public static AmbientZoneLayer CreateTreesLayer()
        {
            return new AmbientZoneLayer
            {
                layerName = "Creaking Trees",
                minInterval = 8f,
                maxInterval = 24f,
                volume = 0.6f,
                spatialBlend = 1f
            };
        }
    }
}
