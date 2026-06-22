using System;
using UnityEngine;

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

        [Header("Footsteps")]
        public FootstepSurfaceSet defaultFootsteps = FootstepSurfaceSet.CreateDefault();
        public FootstepSurfaceSet[] surfaceFootsteps;

        [Header("Landing")]
        public AudioClip[] defaultLandingClips;
        [Min(0.5f)] public float minLandingSpeed = 2.5f;
        [Min(0.5f)] public float hardLandingSpeed = 9f;
        [Range(0f, 1f)] public float landingVolume = 0.9f;

        [Header("Combat")]
        public AudioClip[] weaponSwingClips;
        public AudioClip[] weaponHitClips;
        public AudioClip[] weaponCriticalHitClips;
        public AudioClip[] resourceHitClips;
        [Range(0f, 1f)] public float combatVolume = 1f;

        [Header("UI")]
        public AudioClip[] buttonClickClips;
        public AudioClip[] inventoryItemClickClips;
        public AudioClip[] itemUseClips;
        public AudioClip[] itemEquipClips;
        public AudioClip[] itemUnequipClips;
        public AudioClip[] itemSplitClips;
        public AudioClip[] itemDropClips;
        public AudioClip[] itemPickupClips;
        [Range(0f, 1f)] public float uiVolume = 0.85f;

        [Header("3D Playback")]
        [Range(0f, 1f)] public float sfxSpatialBlend = 1f;
        public float sfxMinDistance = 1f;
        public float sfxMaxDistance = 22f;

        public FootstepSurfaceSet GetFootstepsForSurface(string surfaceTag)
        {
            if (string.IsNullOrEmpty(surfaceTag))
                return defaultFootsteps;

            if (surfaceFootsteps != null)
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

        public AudioClip[] GetLandingClipsForSurface(string surfaceTag)
        {
            if (!string.IsNullOrEmpty(surfaceTag) && surfaceFootsteps != null)
            {
                for (int i = 0; i < surfaceFootsteps.Length; i++)
                {
                    FootstepSurfaceSet set = surfaceFootsteps[i];
                    if (set != null &&
                        string.Equals(set.surfaceTag, surfaceTag, StringComparison.OrdinalIgnoreCase) &&
                        set.landingClips != null &&
                        set.landingClips.Length > 0)
                    {
                        return set.landingClips;
                    }
                }
            }

            if (defaultLandingClips != null && defaultLandingClips.Length > 0)
                return defaultLandingClips;

            return defaultFootsteps?.walkClips;
        }
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
