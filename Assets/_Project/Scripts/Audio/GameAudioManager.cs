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
        private AudioSource loadingSource;
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
            SyncWorldAudioGate();
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
            // Main menu / loader keep AudioListener.pause on; clicks must still be heard.
            uiSource.ignoreListenerPause = true;

            loadingSource = gameObject.AddComponent<AudioSource>();
            loadingSource.playOnAwake = false;
            loadingSource.spatialBlend = 0f;
            loadingSource.loop = true;
            // Boot overlay runs while Time.timeScale is 0 and the listener is paused, so this bed
            // must ignore both pause scaling and AudioListener.pause.
            loadingSource.ignoreListenerPause = true;

            sfxPool = new AudioSource[Mathf.Max(1, sfxPoolSize)];
            for (int i = 0; i < sfxPool.Length; i++)
            {
                // Each pool entry needs its own transform so concurrent 3D clips can sit at
                // different world positions. Sources on this manager GO all shared one transform
                // and teleported the whole audio root — sounding camera/listener-relative.
                GameObject child = new GameObject($"SfxPool_{i}");
                child.transform.SetParent(transform, false);
                AudioSource source = child.AddComponent<AudioSource>();
                source.playOnAwake = false;
                float minDist = profile != null ? profile.sfxMinDistance : 1f;
                float maxDist = profile != null ? profile.sfxMaxDistance : 22f;
                float blend = profile != null ? profile.sfxSpatialBlend : 1f;
                GameplayAudioUtility.ConfigureWorldSpatialSource(source, minDist, maxDist);
                source.spatialBlend = blend;
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

        /// <summary>Loading Genesis ambience bed. Owned here so it never fights menu/gameplay music.</summary>
        public void StartLoadingAmbience()
        {
            if (loadingSource == null || profile == null || profile.loadingAmbience == null)
                return;

            loadingSource.clip = profile.loadingAmbience;
            loadingSource.volume = GameSettings.MusicVolume * profile.loadingAmbienceVolume;
            loadingSource.Play();
        }

        public void SetLoadingAmbienceFade(float normalized)
        {
            if (loadingSource == null || profile == null)
                return;

            loadingSource.volume = GameSettings.MusicVolume * profile.loadingAmbienceVolume * Mathf.Clamp01(normalized);
        }

        public void StopLoadingAmbience()
        {
            if (loadingSource != null)
                loadingSource.Stop();
        }

        /// <summary>
        /// Mute every AudioSource that does not ignore listener pause.
        /// Keeps Loading Genesis ambience + UI clicks while silencing Invector footsteps,
        /// weapon reloads, and other world SFX that still fire under the menu/loader.
        /// </summary>
        public static void SyncWorldAudioGate()
        {
            // Domain reload (script recompile) must never touch AudioListener — native AV in Unity.dll.
            // SubsystemRegistration / edit-mode InitializeOnLoad paths must stay away from this API.
            if (!Application.isPlaying)
                return;

            // Pre-expedition (boot loader, main menu, starter select, expedition loader).
            AudioListener.pause = !GameSession.HasStarted;
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

        public void PlayPunchHit(Vector3 position, bool isCritical)
        {
            AudioClip[] clips = isCritical ? profile?.punchCriticalHitClips : profile?.punchHitClips;
            if (clips == null || clips.Length == 0)
            {
                PlayWeaponHit(position, isCritical);
                return;
            }

            PlayCombatClip(PickClip(clips), position, isCritical ? 1f : 0.9f);
        }

        public void PlayPunchSwing(Vector3 position)
        {
            AudioClip[] clips = profile?.punchSwingClips;
            if (clips == null || clips.Length == 0)
            {
                PlayWeaponSwing(position);
                return;
            }

            PlayCombatClip(PickClip(clips), position, 0.58f);
        }

        public void PlayResourceHit(Vector3 position)
        {
            PlayCombatClip(PickClip(profile?.resourceHitClips), position, 0.75f);
        }

        public void PlayButtonClick()
        {
            PlayUiClip(PickClip(profile?.buttonClickClips), profile != null ? profile.uiVolume : 0.85f);
        }

        /// <summary>
        /// Soft UI tick for hover / focus changes (journal tabs, etc.). Reuses buttonClickClips (keyPress).
        /// </summary>
        public void PlayUiHoverTick()
        {
            PlayUiClip(PickClip(profile?.buttonClickClips), profile != null ? profile.uiVolume * 0.55f : 0.45f);
        }

        public void PlayInventoryItemClick()
        {
            AudioClip[] clips = profile?.inventoryItemClickClips;
            if (clips == null || clips.Length == 0)
                clips = profile?.buttonClickClips;
            PlayUiClip(PickClip(clips), profile != null ? profile.uiVolume * 0.9f : 0.75f);
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

        public void PlayAchievementUnlock()
        {
            PlayUiClip(PickClip(profile?.achievementUnlockClips), profile != null ? profile.uiVolume * 1.05f : 0.9f);
        }

        /// <summary>Level-up chime. Prefers levelUpClips, then achievementUnlockClips.</summary>
        public void PlayLevelUp()
        {
            AudioClip[] clips = profile?.levelUpClips;
            if (clips == null || clips.Length == 0)
                clips = profile?.achievementUnlockClips;

            PlayUiClip(PickClip(clips), profile != null ? profile.uiVolume * 1.1f : 0.95f);
        }

        private void PlayUiClip(AudioClip clip, float volumeScale)
        {
            if (clip == null || uiSource == null)
                return;

            uiSource.pitch = Random.Range(0.97f, 1.03f);
            uiSource.PlayOneShot(clip, GameSettings.SfxVolume * volumeScale);
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

        /// <summary>Pooled 3D one-shot for weapon fire / world SFX (avoids PlayClipAtPoint allocs).</summary>
        public static void PlayWorldSfx(AudioClip clip, Vector3 position, float volumeScale = 1f)
        {
            if (clip == null)
                return;

            if (Instance != null)
            {
                Instance.PlayClip3D(clip, position, volumeScale, Random.Range(0.97f, 1.03f));
                return;
            }

            AudioSource.PlayClipAtPoint(clip, position, Mathf.Clamp01(volumeScale));
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
            if (source == null)
            {
                // Pool missing — fire-and-forget one-shot so we never steal the music source.
                float volume = GameSettings.SfxVolume * volumeScale;
                AudioSource.PlayClipAtPoint(clip, position, Mathf.Clamp01(volume));
                return;
            }

            // Move only the pool child — never the GameAudioManager root (music/UI live here).
            source.transform.SetParent(transform, true);
            source.transform.position = position;
            source.clip = clip;
            source.pitch = pitch;
            float blend = spatialBlendOverride ?? (profile != null ? profile.sfxSpatialBlend : 1f);
            float minDist = profile != null ? profile.sfxMinDistance : 1f;
            float maxDist = profile != null ? profile.sfxMaxDistance : 22f;
            GameplayAudioUtility.ConfigureWorldSpatialSource(source, minDist, maxDist);
            source.spatialBlend = blend;
            source.volume = GameSettings.SfxVolume * volumeScale;
            source.Play();
        }

        private AudioSource GetNextSfxSource()
        {
            if (sfxPool == null || sfxPool.Length == 0)
                return null;

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
