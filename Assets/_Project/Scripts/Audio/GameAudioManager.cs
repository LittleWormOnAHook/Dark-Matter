using Project.Core;
using UnityEngine;

namespace Project.Audio
{
    public class GameAudioManager : MonoBehaviour
    {
        public static GameAudioManager Instance { get; private set; }

        [SerializeField] private GameAudioProfile profile;
        [SerializeField] private int sfxPoolSize = 10;
        [SerializeField] private bool playMusicOnGameStart = true;

        private AudioSource musicSource;
        private AudioSource uiSource;
        private AudioSource[] sfxPool;
        private int sfxPoolIndex;
        private int lastMusicTrackIndex = -1;

        public GameAudioProfile Profile => profile;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (profile == null)
                profile = Resources.Load<GameAudioProfile>("GameAudioProfile");

            BuildSources();
            RefreshVolumes();
        }

        private void BuildSources()
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;

            uiSource = gameObject.AddComponent<AudioSource>();
            uiSource.playOnAwake = false;
            uiSource.spatialBlend = 0f;
            uiSource.loop = false;

            sfxPool = new AudioSource[Mathf.Max(1, sfxPoolSize)];
            for (int i = 0; i < sfxPool.Length; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = profile != null ? profile.sfxSpatialBlend : 1f;
                source.minDistance = profile != null ? profile.sfxMinDistance : 1f;
                source.maxDistance = profile != null ? profile.sfxMaxDistance : 22f;
                sfxPool[i] = source;
            }
        }

        public void RefreshVolumes()
        {
            if (musicSource != null && profile != null)
                musicSource.volume = GameSettings.MusicVolume * profile.musicVolume;
        }

        public void StartGameplayMusic()
        {
            if (!playMusicOnGameStart || profile == null || profile.musicTracks == null || profile.musicTracks.Length == 0)
                return;

            AudioClip track = PickMusicTrack();
            if (track == null)
                return;

            musicSource.clip = track;
            musicSource.loop = profile.loopCurrentTrack;
            RefreshVolumes();
            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource != null)
                musicSource.Stop();
        }

        public void PlayFootstep(Vector3 position, string surfaceTag, bool isRunning)
        {
            if (profile == null)
                return;

            FootstepSurfaceSet set = profile.GetFootstepsForSurface(surfaceTag);
            AudioClip[] clips = isRunning ? set.runClips : set.walkClips;
            if (clips == null || clips.Length == 0)
                clips = isRunning ? profile.defaultFootsteps.runClips : profile.defaultFootsteps.walkClips;

            PlayClip3D(PickClip(clips), position, set.volume * profile.combatVolume, Random.Range(0.92f, 1.08f));
        }

        public void PlayLanding(Vector3 position, string surfaceTag, float impactSpeed)
        {
            if (profile == null || impactSpeed < profile.minLandingSpeed)
                return;

            AudioClip[] clips = profile.GetLandingClipsForSurface(surfaceTag);
            AudioClip clip = PickClip(clips);
            if (clip == null)
                return;

            float speedRange = Mathf.Max(0.01f, profile.hardLandingSpeed - profile.minLandingSpeed);
            float impactT = Mathf.Clamp01((impactSpeed - profile.minLandingSpeed) / speedRange);
            float volume = Mathf.Lerp(0.55f, 1f, impactT) * profile.landingVolume;
            float pitch = Mathf.Lerp(1.05f, 0.88f, impactT);

            PlayClip3D(clip, position, volume, Random.Range(pitch * 0.97f, pitch * 1.03f));
        }

        public void PlayWeaponSwing(Vector3 position)
        {
            PlayCombatClip(PickClip(profile?.weaponSwingClips), position, 0.55f);
        }

        public void PlayWeaponHit(Vector3 position, bool isCritical)
        {
            AudioClip[] clips = isCritical ? profile?.weaponCriticalHitClips : profile?.weaponHitClips;
            PlayCombatClip(PickClip(clips), position, isCritical ? 1f : 0.85f);
        }

        public void PlayResourceHit(Vector3 position)
        {
            PlayCombatClip(PickClip(profile?.resourceHitClips), position, 0.75f);
        }

        public void PlayButtonClick()
        {
            PlayUiClip(PickClip(profile?.buttonClickClips), profile != null ? profile.uiVolume : 0.85f);
        }

        public void PlayInventoryItemClick()
        {
            PlayUiClip(PickClip(profile?.inventoryItemClickClips), profile != null ? profile.uiVolume * 0.9f : 0.75f);
        }

        public void PlayItemUse()
        {
            PlayUiClip(PickClip(profile?.itemUseClips), profile != null ? profile.uiVolume : 0.85f);
        }

        public void PlayItemEquip()
        {
            PlayUiClip(PickClip(profile?.itemEquipClips), profile != null ? profile.uiVolume : 0.85f);
        }

        public void PlayItemUnequip()
        {
            PlayUiClip(PickClip(profile?.itemUnequipClips), profile != null ? profile.uiVolume * 0.9f : 0.75f);
        }

        public void PlayItemSplit()
        {
            PlayUiClip(PickClip(profile?.itemSplitClips), profile != null ? profile.uiVolume * 0.85f : 0.7f);
        }

        public void PlayItemDrop()
        {
            PlayUiClip(PickClip(profile?.itemDropClips), profile != null ? profile.uiVolume : 0.85f);
        }

        public void PlayItemPickup()
        {
            PlayUiClip(PickClip(profile?.itemPickupClips), profile != null ? profile.uiVolume * 0.95f : 0.8f);
        }

        private void PlayUiClip(AudioClip clip, float volumeScale)
        {
            if (clip == null || uiSource == null)
                return;

            uiSource.clip = clip;
            uiSource.volume = GameSettings.SfxVolume * volumeScale;
            uiSource.pitch = Random.Range(0.97f, 1.03f);
            uiSource.Play();
        }

        public void PlayAmbientOneShot(AmbientZoneLayer layer, Vector3 position)
        {
            if (layer == null || layer.clips == null || layer.clips.Length == 0)
                return;

            AudioClip clip = PickClip(layer.clips);
            if (clip == null)
                return;

            float pitch = Random.Range(layer.pitchMin, layer.pitchMax);
            PlayClip3D(clip, position, layer.volume, pitch, layer.spatialBlend);
        }

        private void PlayCombatClip(AudioClip clip, Vector3 position, float volumeScale)
        {
            if (clip == null || profile == null)
                return;

            PlayClip3D(clip, position, volumeScale * profile.combatVolume, Random.Range(0.94f, 1.06f));
        }

        private void PlayClip3D(AudioClip clip, Vector3 position, float volumeScale, float pitch, float? spatialBlendOverride = null)
        {
            if (clip == null)
                return;

            AudioSource source = GetNextSfxSource();
            source.transform.position = position;
            source.clip = clip;
            source.pitch = pitch;
            source.spatialBlend = spatialBlendOverride ?? (profile != null ? profile.sfxSpatialBlend : 1f);
            source.minDistance = profile != null ? profile.sfxMinDistance : 1f;
            source.maxDistance = profile != null ? profile.sfxMaxDistance : 22f;
            source.volume = GameSettings.SfxVolume * volumeScale;
            source.Play();
        }

        private AudioSource GetNextSfxSource()
        {
            if (sfxPool == null || sfxPool.Length == 0)
                return musicSource;

            AudioSource source = sfxPool[sfxPoolIndex];
            sfxPoolIndex = (sfxPoolIndex + 1) % sfxPool.Length;
            return source;
        }

        private AudioClip PickMusicTrack()
        {
            if (profile?.musicTracks == null || profile.musicTracks.Length == 0)
                return null;

            if (profile.musicTracks.Length == 1)
                return profile.musicTracks[0];

            if (!profile.shuffleMusic)
            {
                lastMusicTrackIndex = (lastMusicTrackIndex + 1) % profile.musicTracks.Length;
                return profile.musicTracks[lastMusicTrackIndex];
            }

            int index = Random.Range(0, profile.musicTracks.Length);
            if (profile.musicTracks.Length > 1)
            {
                int safety = 0;
                while (index == lastMusicTrackIndex && safety++ < 8)
                    index = Random.Range(0, profile.musicTracks.Length);
            }

            lastMusicTrackIndex = index;
            return profile.musicTracks[index];
        }

        private static AudioClip PickClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
                return null;

            return clips[Random.Range(0, clips.Length)];
        }

        public static void EnsureExists()
        {
            if (Instance != null)
                return;

            GameAudioManager existing = FindAnyObjectByType<GameAudioManager>();
            if (existing != null)
            {
                Instance = existing;
                return;
            }

            GameObject bootstrap = new GameObject("GameAudioManager");
            bootstrap.AddComponent<GameAudioManager>();
        }
    }
}
