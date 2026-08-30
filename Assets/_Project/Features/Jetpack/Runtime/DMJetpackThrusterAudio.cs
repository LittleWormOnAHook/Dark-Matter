using UnityEngine;

namespace Project.Features.Jetpack
{
    [DisallowMultipleComponent]
    public sealed class DMJetpackThrusterAudio : MonoBehaviour
    {
        public const string Layer1Resource = "Audio/Thruster";
        public const string Layer2Resource = "Audio/Thruster 1";

        [SerializeField] private DMJetpackController jetpack;
        [SerializeField] private DMJetpackProfile profile;
        [SerializeField] private AudioSource layer1Source;
        [SerializeField] private AudioSource layer2Source;

        private float _smoothed;
        private float _velocity;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOnPlayer()
        {
            if (!Application.isPlaying)
                return;

            GameObject player = GameObject.Find("Player_v7");
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

            layer1Source = EnsureSource(layer1Source, "ThrusterLayer1");
            layer2Source = EnsureSource(layer2Source, "ThrusterLayer2");
        }

        private void OnDisable()
        {
            Stop(layer1Source);
            Stop(layer2Source);
        }

        private void Update()
        {
            float target = 0f;
            if (jetpack != null && Time.timeScale > 0f)
                target = Mathf.Clamp01(jetpack.CurrentThrustVisual);

            float smooth = profile != null ? profile.thrusterAudioSmooth : 0.12f;
            _smoothed = Mathf.SmoothDamp(_smoothed, target, ref _velocity, smooth);

            AudioClip clip1 = ResolveLayer1();
            AudioClip clip2 = ResolveLayer2();
            TickLayer(layer1Source, clip1, Layer1Volume(), Layer1Pitch(), _smoothed);
            TickLayer(layer2Source, clip2, Layer2Volume(), Layer2Pitch(), Layer2Mix());
        }

        private float Layer1Volume()
        {
            return profile != null ? profile.thrusterLayer1Volume : 0.55f;
        }

        private Vector2 Layer1Pitch()
        {
            return profile != null ? profile.thrusterLayer1Pitch : new Vector2(0.92f, 1.04f);
        }

        private float Layer2Volume()
        {
            return profile != null ? profile.thrusterLayer2Volume : 0.4f;
        }

        private Vector2 Layer2Pitch()
        {
            return profile != null ? profile.thrusterLayer2Pitch : new Vector2(0.98f, 1.12f);
        }

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

        private void TickLayer(AudioSource source, AudioClip clip, float volume, Vector2 pitch, float mix)
        {
            if (source == null || clip == null || mix <= 0.01f)
            {
                Stop(source);
                return;
            }

            if (source.clip != clip)
                source.clip = clip;

            float flutter = Mathf.Sin(Time.unscaledTime * 9.3f) * 0.018f * mix;
            source.volume = volume * Mathf.Lerp(0.18f, 1f, mix);
            source.pitch = Mathf.Lerp(pitch.x, pitch.y, mix) + flutter;

            if (!source.isPlaying)
                source.Play();
        }

        private static void Stop(AudioSource source)
        {
            if (source != null && source.isPlaying)
                source.Stop();
        }

        private AudioSource EnsureSource(AudioSource existing, string childName)
        {
            if (existing != null)
                return existing;

            Transform child = transform.Find(childName);
            AudioSource source = child != null ? child.GetComponent<AudioSource>() : null;
            if (source == null)
            {
                GameObject go = new GameObject(childName);
                go.transform.SetParent(transform, false);
                source = go.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 1f;
            source.minDistance = 2f;
            source.maxDistance = 28f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.dopplerLevel = 0.15f;
            return source;
        }
    }
}
