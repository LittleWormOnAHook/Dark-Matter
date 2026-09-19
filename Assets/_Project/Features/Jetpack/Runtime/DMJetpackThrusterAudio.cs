using Project.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Features.Jetpack
{
    /// <summary>
    /// Layered loop audio on the jetpack engine mounts, driven by <see cref="DMJetpackController.CurrentThrustVisual"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(455)]
    public sealed class DMJetpackThrusterAudio : MonoBehaviour
    {
        public const string Layer1Resource = "Audio/Thruster";
        public const string Layer2Resource = "Audio/Thruster 1";

        [SerializeField] private DMJetpackController jetpack;
        [SerializeField] private DMJetpackProfile profile;
        [SerializeField] private Transform thrusterAnchor;
        [SerializeField] private AudioSource layer1Source;
        [SerializeField] private AudioSource layer2Source;
        [SerializeField] private AudioSource igniteSource;

        private float _smoothed;
        private float _velocity;
        private bool _wasThrusting;
        private bool _releaseFading;
        private float _releaseFrom;
        private float _releaseElapsed;
        private const float IgniteSeconds = 0.14f;
        private const float DefaultReleaseSeconds = 1f;
        private const float ReleaseStopThreshold = 0.001f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOnPlayer()
        {
            if (!Application.isPlaying)
                return;

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player == null)
                player = GameObject.Find("Player_v7 Variant");
            if (player == null)
                player = GameObject.Find("Player_v7");
            if (player == null || player.GetComponent<DMJetpackThrusterAudio>() != null)
                return;

            player.AddComponent<DMJetpackThrusterAudio>();
        }

        private void Awake()
        {
            if (jetpack == null)
                jetpack = GetComponent<DMJetpackController>();
            if (profile == null && jetpack != null)
                profile = jetpack.Profile;

            ResolveThrusterAnchor();
            layer1Source = EnsureSource(layer1Source, "ThrusterLayer1", ResolveEngineMount(0), loop: true);
            layer2Source = EnsureSource(layer2Source, "ThrusterLayer2", ResolveEngineMount(1), loop: true);
            igniteSource = EnsureSource(igniteSource, "ThrusterIgnite", ResolveEngineMount(0), loop: false);
        }

        private void OnDisable()
        {
            HardStop();
        }

        private void Update()
        {
            if (jetpack == null || !GameSession.HasStarted || Time.timeScale <= 0f)
            {
                HardStop();
                return;
            }

            bool thrusting = IsThrusterEngaged();
            if (thrusting && !_wasThrusting)
            {
                _releaseFading = false;
                PlayIgnitePop();
            }

            if (!thrusting && _wasThrusting)
            {
                Stop(igniteSource);
                BeginReleaseFade();
            }

            _wasThrusting = thrusting;

            if (thrusting)
            {
                _releaseFading = false;
                float target = Mathf.Clamp01(jetpack.CurrentThrustVisual);
                float smooth = profile != null ? profile.thrusterAudioSmooth : 0.12f;
                _smoothed = Mathf.SmoothDamp(_smoothed, target, ref _velocity, smooth);
            }
            else if (_releaseFading)
            {
                float duration = Mathf.Max(0.05f, profile != null
                    ? profile.thrusterAudioReleaseSeconds
                    : DefaultReleaseSeconds);
                _releaseElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(_releaseElapsed / duration);
                _smoothed = _releaseFrom * (1f - t);
                _velocity = 0f;
                if (t >= 1f || _smoothed <= ReleaseStopThreshold)
                {
                    HardStop();
                    return;
                }
            }
            else
            {
                HardStop();
                return;
            }

            float master = GameSettings.MasterVolume * GameSettings.SfxVolume;
            AudioClip clip1 = ResolveLayer1();
            AudioClip clip2 = ResolveLayer2();
            TickLayer(layer1Source, clip1, Layer1Volume() * master, Layer1Pitch(), _smoothed, _releaseFading);
            TickLayer(layer2Source, clip2, Layer2Volume() * master, Layer2Pitch(), Layer2Mix(), _releaseFading);
        }

        private void PlayIgnitePop()
        {
            AudioClip clip = ResolveLayer2() ?? ResolveLayer1();
            if (clip == null || igniteSource == null)
                return;

            float master = GameSettings.MasterVolume * GameSettings.SfxVolume;
            igniteSource.clip = clip;
            igniteSource.loop = false;
            igniteSource.volume = (profile != null ? profile.thrusterLayer2Volume : 0.4f) * master * 0.65f;
            igniteSource.pitch = 1f;
            igniteSource.Play();
            igniteSource.SetScheduledEndTime(AudioSettings.dspTime + IgniteSeconds);
        }

        private void HardStop()
        {
            Stop(layer1Source);
            Stop(layer2Source);
            Stop(igniteSource);
            _smoothed = 0f;
            _velocity = 0f;
            _wasThrusting = false;
            _releaseFading = false;
            _releaseFrom = 0f;
            _releaseElapsed = 0f;
        }

        private bool IsThrusterEngaged()
        {
            if (!jetpack.IsBoostingNow)
                return false;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.isPressed)
                return true;

            Gamepad pad = Gamepad.current;
            return pad != null && pad.buttonSouth.isPressed;
        }

        private void BeginReleaseFade()
        {
            _releaseFading = true;
            _releaseElapsed = 0f;
            _releaseFrom = Mathf.Max(_smoothed, 0.18f);
            _smoothed = _releaseFrom;
            _velocity = 0f;
        }

        private void ResolveThrusterAnchor()
        {
            if (thrusterAnchor != null)
                return;

            Transform spine = transform.Find("Spine2") ?? transform.Find("Spine02");
            if (spine != null)
                thrusterAnchor = spine.Find("DM_Jetpack");

            if (thrusterAnchor == null)
            {
                DMJetpackThrusterVfx vfx = GetComponentInChildren<DMJetpackThrusterVfx>(true);
                if (vfx != null)
                    thrusterAnchor = vfx.transform;
            }
        }

        private Transform ResolveEngineMount(int index)
        {
            if (thrusterAnchor == null)
                ResolveThrusterAnchor();

            if (thrusterAnchor == null)
                return null;

            Transform left = thrusterAnchor.Find("Engine_L");
            Transform right = thrusterAnchor.Find("Engine_R");
            if (index == 0)
                return left != null ? left : thrusterAnchor;
            return right != null ? right : thrusterAnchor;
        }

        private float Layer1Volume() => profile != null ? profile.thrusterLayer1Volume : 0.55f;

        private Vector2 Layer1Pitch() => profile != null ? profile.thrusterLayer1Pitch : new Vector2(0.92f, 1.04f);

        private float Layer2Volume() => profile != null ? profile.thrusterLayer2Volume : 0.4f;

        private Vector2 Layer2Pitch() => profile != null ? profile.thrusterLayer2Pitch : new Vector2(0.98f, 1.12f);

        private float Layer2Mix()
        {
            float start = profile != null ? profile.thrusterLayer2Start : 0.25f;
            if (_smoothed <= start)
                return 0f;
            return Mathf.InverseLerp(start, 1f, _smoothed);
        }

        private AudioClip ResolveLayer1()
        {
            if (profile != null && profile.thrusterLayer1 != null)
                return profile.thrusterLayer1;
            return Resources.Load<AudioClip>(Layer1Resource);
        }

        private AudioClip ResolveLayer2()
        {
            if (profile != null && profile.thrusterLayer2 != null)
                return profile.thrusterLayer2;
            return Resources.Load<AudioClip>(Layer2Resource);
        }

        private void TickLayer(
            AudioSource source,
            AudioClip clip,
            float volume,
            Vector2 pitch,
            float mix,
            bool releasing)
        {
            if (!GameplayAudioUtility.CanPlaySpatialSource(source) || clip == null || mix <= ReleaseStopThreshold)
            {
                Stop(source);
                return;
            }

            if (source.clip != clip)
                source.clip = clip;

            float flutter = Mathf.Sin(Time.unscaledTime * 9.3f) * 0.018f * mix;
            source.volume = releasing
                ? volume * mix
                : volume * Mathf.Lerp(0.18f, 1f, mix);
            source.pitch = Mathf.Lerp(pitch.x, pitch.y, mix) + flutter;

            if (!source.isPlaying)
                source.Play();
        }

        private static void Stop(AudioSource source)
        {
            if (source != null && source.isPlaying)
                source.Stop();
        }

        private AudioSource EnsureSource(AudioSource existing, string childName, Transform anchor, bool loop)
        {
            if (existing != null)
            {
                ConfigureSource(existing, loop);
                return existing;
            }

            Transform parent = anchor != null ? anchor : transform;
            Transform child = parent.Find(childName);
            AudioSource source = child != null ? child.GetComponent<AudioSource>() : null;
            if (source == null)
            {
                GameObject go = new GameObject(childName);
                go.transform.SetParent(parent, false);
                source = go.AddComponent<AudioSource>();
            }

            ConfigureSource(source, loop);
            return source;
        }

        private static void ConfigureSource(AudioSource source, bool loop)
        {
            source.playOnAwake = false;
            source.loop = loop;
            source.dopplerLevel = 0.15f;
            GameplayAudioUtility.ConfigureWorldSpatialSource(source, 2f, 28f);
        }
    }
}
