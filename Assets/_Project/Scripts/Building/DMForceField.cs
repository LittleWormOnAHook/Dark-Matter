using System.Collections.Generic;
using Project.Companions.Invector;
using UnityEngine;

namespace Project.Building
{
    /// <summary>
    /// Force field door (0926-force-fields). The solid box stops bullets, enemies and weather; it switches off while the player
    /// or a companion is passing through, then closes again. Crossing the field plays a ripple, a glow pulse and an electric crackle.
    /// Look, sound and behaviour come from Building Studio > Creation Effects > Force fields.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMForceField : MonoBehaviour
    {
        public const string DefaultMaterialResource = "Building/DM_ForceField";

        const float ActorRefreshSeconds = 0.5f;
        const int RippleSlots = 4;

        static readonly List<Collider> Actors = new List<Collider>();
        static float actorsRefreshedAt = -10f;
        static int actorsFrame = -1;
        static AudioClip generatedCrackle;
        static AudioClip generatedHum;
        static Material defaultMaterial;

        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");
        static readonly int OpacityId = Shader.PropertyToID("_Opacity");
        static readonly int EdgeGlowId = Shader.PropertyToID("_EdgeGlow");
        static readonly int EdgeWidthId = Shader.PropertyToID("_EdgeWidth");
        static readonly int PatternScaleId = Shader.PropertyToID("_PatternScale");
        static readonly int ScrollSpeedId = Shader.PropertyToID("_ScrollSpeed");
        static readonly int PulseId = Shader.PropertyToID("_Pulse");
        static readonly int RippleSpeedId = Shader.PropertyToID("_RippleSpeed");
        static readonly int RippleWidthId = Shader.PropertyToID("_RippleWidth");
        static readonly int RippleLifeId = Shader.PropertyToID("_RippleLife");
        static readonly int[] RippleIds =
        {
            Shader.PropertyToID("_Ripple0"),
            Shader.PropertyToID("_Ripple1"),
            Shader.PropertyToID("_Ripple2"),
            Shader.PropertyToID("_Ripple3"),
        };

        BoxCollider solid;
        Renderer body;
        AudioSource passSource;
        AudioSource humSource;
        MaterialPropertyBlock block;
        readonly Dictionary<Collider, float> insideSide = new Dictionary<Collider, float>();
        readonly Dictionary<Collider, float> nextSound = new Dictionary<Collider, float>();
        readonly List<Collider> stale = new List<Collider>();
        readonly Vector4[] ripples = new Vector4[RippleSlots];
        int nextRipple;
        float pulse;
        float closeAt;

        public bool IsOpen => solid != null && !solid.enabled;

        void Awake()
        {
            BoxCollider[] boxes = GetComponents<BoxCollider>();
            for (int i = 0; i < boxes.Length; i++)
            {
                if (!boxes[i].isTrigger)
                {
                    solid = boxes[i];
                    break;
                }
            }

            body = GetComponent<Renderer>();
            block = new MaterialPropertyBlock();
            for (int i = 0; i < RippleSlots; i++)
                ripples[i] = new Vector4(0f, 0f, 0f, -100f);

            passSource = gameObject.AddComponent<AudioSource>();
            SetupSource(passSource);
            ApplyLook();
        }

        void Start()
        {
            ApplyLook();
        }

        void OnEnable()
        {
            DMBasePower.RegisterConsumer(this, true);
        }

        void OnDisable()
        {
            DMBasePower.UnregisterConsumer(this);
            if (solid != null)
            {
                solid.enabled = true;
                solid.isTrigger = false;
            }
            unpowered = false;
            insideSide.Clear();
            if (humSource != null)
                humSource.Stop();
        }

        /// <summary>Puts the force field material back (after build paint, material swaps, or a Studio change).</summary>
        public void ApplyLook()
        {
            if (body == null)
                return;
            Material target = ResolveMaterial(DMBuildingCreationFxProfile.Live);
            if (target != null)
            {
                Material[] current = body.sharedMaterials;
                bool same = current.Length == 1 && current[0] == target;
                if (!same)
                    body.sharedMaterials = new[] { target };
            }

            PushBlock(DMBuildingCreationFxProfile.Live);
        }

        public static Material ResolveMaterial(DMBuildingCreationFxProfile fx)
        {
            if (fx != null && fx.forceFieldMaterial != null)
                return fx.forceFieldMaterial;
            if (defaultMaterial == null)
                defaultMaterial = Resources.Load<Material>(DefaultMaterialResource);
            return defaultMaterial;
        }

        bool unpowered;

        /// <summary>0926-generator: with no powered generator in the base the field shuts off and anyone can walk through.</summary>
        bool UpdatePower()
        {
            bool powered = !DMBuildingGhostProfile.ForceFieldsNeedPower || DMBasePower.IsPowered(transform.position);
            if (powered == !unpowered)
                return powered;
            unpowered = !powered;
            // 0926-ff-visible: an unpowered field stays visible (dim, still) so you can see it was built,
            // and its box becomes a trigger so anyone walks through but it can still be aimed at.
            if (solid != null)
            {
                solid.enabled = true;
                solid.isTrigger = !powered;
            }
            if (body != null)
                body.enabled = true;
            PushBlock(DMBuildingCreationFxProfile.Live);
            insideSide.Clear();
            if (!powered && humSource != null)
                humSource.Stop();
            return powered;
        }

        void Update()
        {
            if (!UpdatePower())
                return;
            DMBuildingCreationFxProfile fx = DMBuildingCreationFxProfile.Live;
            float now = Time.time;
            bool letThrough = fx == null || fx.forceFieldLetFriendliesThrough;
            bool anyInside = false;

            if (solid != null && letThrough)
            {
                RefreshActors(now);
                Vector3 size = solid.size;
                Vector3 half = size * 0.5f;
                int thin = ThinAxis(size);
                float depth = fx != null ? fx.forceFieldSenseDepthMeters : 0.9f;
                Matrix4x4 toLocal = transform.worldToLocalMatrix;

                stale.Clear();
                foreach (KeyValuePair<Collider, float> pair in insideSide)
                    stale.Add(pair.Key);

                for (int i = 0; i < Actors.Count; i++)
                {
                    Collider actor = Actors[i];
                    if (actor == null || !actor.enabled || !actor.gameObject.activeInHierarchy)
                        continue;

                    Bounds bounds = actor.bounds;
                    Vector3 local = toLocal.MultiplyPoint3x4(bounds.center) - solid.center;
                    Vector3 e = bounds.extents;
                    Vector3 limit = half;
                    for (int k = 0; k < 3; k++)
                        limit[k] += Mathf.Abs(toLocal[k, 0]) * e.x + Mathf.Abs(toLocal[k, 1]) * e.y + Mathf.Abs(toLocal[k, 2]) * e.z;
                    limit[thin] += depth;
                    if (Mathf.Abs(local.x) > limit.x || Mathf.Abs(local.y) > limit.y || Mathf.Abs(local.z) > limit.z)
                        continue;

                    anyInside = true;
                    Collider id = actor;
                    stale.Remove(id);
                    float side = Mathf.Sign(local[thin]);
                    if (!insideSide.TryGetValue(id, out float lastSide))
                    {
                        insideSide[id] = side;
                        pulse = Mathf.Max(pulse, 0.35f);
                    }
                    else if (side != lastSide)
                    {
                        insideSide[id] = side;
                        OnCrossed(id, local, half, thin, fx, now);
                    }
                }

                for (int i = 0; i < stale.Count; i++)
                    insideSide.Remove(stale[i]);
            }
            else if (insideSide.Count > 0)
            {
                insideSide.Clear();
            }

            if (solid != null)
            {
                float closeDelay = fx != null ? fx.forceFieldCloseDelaySeconds : 0.35f;
                if (anyInside)
                {
                    solid.enabled = false;
                    closeAt = now + closeDelay;
                }
                else if (!solid.enabled && now >= closeAt)
                {
                    solid.enabled = true;
                }
            }

            float pulseSeconds = fx != null ? fx.forceFieldPulseSeconds : 0.6f;
            pulse = Mathf.MoveTowards(pulse, 0f, Time.deltaTime / Mathf.Max(0.05f, pulseSeconds));
            UpdateHum(fx);
            PushBlock(fx);
        }

        void OnCrossed(Collider id, Vector3 local, Vector3 half, int thin, DMBuildingCreationFxProfile fx, float now)
        {
            Vector3 onPlane = local;
            for (int k = 0; k < 3; k++)
                onPlane[k] = Mathf.Clamp(onPlane[k], -half[k], half[k]);
            onPlane[thin] = 0f;
            Vector3 world = transform.TransformPoint(onPlane + solid.center);
            ripples[nextRipple] = new Vector4(world.x, world.y, world.z, Time.timeSinceLevelLoad);
            nextRipple = (nextRipple + 1) % RippleSlots;
            pulse = 1f;

            float cooldown = fx != null ? fx.forceFieldSoundCooldown : 0.4f;
            if (nextSound.TryGetValue(id, out float allowedAt) && now < allowedAt)
                return;
            nextSound[id] = now + cooldown;

            AudioClip clip = fx != null && fx.forceFieldPassClip != null ? fx.forceFieldPassClip : Crackle();
            float volume = fx != null ? fx.forceFieldPassVolume : 0.35f;
            float jitter = fx != null ? fx.forceFieldPitchJitter : 0.08f;
            if (clip == null || volume <= 0f || passSource == null)
                return;
            passSource.transform.position = transform.position;
            passSource.maxDistance = fx != null ? fx.forceFieldAudioMaxDistance : 14f;
            passSource.pitch = 1f + Random.Range(-jitter, jitter);
            passSource.PlayOneShot(clip, volume);
        }

        void UpdateHum(DMBuildingCreationFxProfile fx)
        {
            float volume = fx != null ? fx.forceFieldIdleHumVolume : 0f;
            if (volume <= 0f)
            {
                if (humSource != null && humSource.isPlaying)
                    humSource.Stop();
                return;
            }

            if (humSource == null)
            {
                humSource = gameObject.AddComponent<AudioSource>();
                SetupSource(humSource);
                humSource.loop = true;
            }

            AudioClip clip = fx.forceFieldIdleHumClip != null ? fx.forceFieldIdleHumClip : Hum();
            if (humSource.clip != clip)
            {
                humSource.clip = clip;
                humSource.Stop();
            }

            humSource.maxDistance = fx.forceFieldAudioMaxDistance;
            humSource.volume = volume * (1f + pulse * 0.5f);
            if (!humSource.isPlaying)
            {
                humSource.time = Random.Range(0f, Mathf.Max(0f, clip.length - 0.05f));
                humSource.Play();
            }
        }

        void PushBlock(DMBuildingCreationFxProfile fx)
        {
            if (body == null)
                return;
            // Private non-serialized state is lost on a play-mode script reload (Renderer survives, the block does not).
            if (block == null)
                block = new MaterialPropertyBlock();
            body.GetPropertyBlock(block);
            if (fx != null)
            {
                block.SetColor(ColorId, fx.forceFieldColor);
                block.SetColor(EdgeColorId, fx.forceFieldEdgeColor);
                float dim = unpowered ? 0.3f : 1f;
                block.SetFloat(OpacityId, fx.forceFieldOpacity * dim);
                block.SetFloat(EdgeGlowId, fx.forceFieldEdgeGlow * dim);
                block.SetFloat(EdgeWidthId, fx.forceFieldEdgeWidth);
                block.SetFloat(PatternScaleId, fx.forceFieldPatternScale);
                block.SetFloat(ScrollSpeedId, unpowered ? 0f : fx.forceFieldScrollSpeed);
                block.SetFloat(RippleSpeedId, fx.forceFieldRippleSpeed);
                block.SetFloat(RippleWidthId, fx.forceFieldRippleWidth);
                block.SetFloat(RippleLifeId, fx.forceFieldPulseSeconds);
                block.SetFloat(PulseId, pulse * fx.forceFieldPulseBrightness);
            }
            else
            {
                block.SetFloat(PulseId, pulse * 1.5f);
            }

            for (int i = 0; i < RippleSlots; i++)
                block.SetVector(RippleIds[i], ripples[i]);
            body.SetPropertyBlock(block);
        }

        static void SetupSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1.5f;
            source.maxDistance = 14f;
            source.dopplerLevel = 0f;
        }

        static int ThinAxis(Vector3 size)
        {
            if (size.x <= size.y && size.x <= size.z)
                return 0;
            return size.y <= size.z ? 1 : 2;
        }

        /// <summary>Player plus every companion, cached for all fields and refreshed twice a second.</summary>
        static void RefreshActors(float now)
        {
            if (actorsFrame == Time.frameCount || now - actorsRefreshedAt < ActorRefreshSeconds)
                return;
            actorsFrame = Time.frameCount;
            actorsRefreshedAt = now;
            Actors.Clear();

            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            for (int i = 0; i < players.Length; i++)
                AddActor(players[i]);

            CompanionInvectorBootstrap[] companions = FindObjectsByType<CompanionInvectorBootstrap>(FindObjectsInactive.Exclude);
            for (int i = 0; i < companions.Length; i++)
                AddActor(companions[i].gameObject);
        }

        static void AddActor(GameObject go)
        {
            if (go == null)
                return;
            Collider best = go.GetComponent<CapsuleCollider>();
            if (best == null || best.isTrigger)
                best = go.GetComponent<CharacterController>();
            if (best == null)
            {
                Collider[] all = go.GetComponentsInChildren<Collider>();
                for (int i = 0; i < all.Length; i++)
                {
                    if (!all[i].isTrigger)
                    {
                        best = all[i];
                        break;
                    }
                }
            }

            if (best != null && !Actors.Contains(best))
                Actors.Add(best);
        }

        /// <summary>About half a second of electric crackle built in code: a buzz, fizzing noise and random spark pops.</summary>
        static AudioClip Crackle()
        {
            if (generatedCrackle != null)
                return generatedCrackle;

            const int rate = 44100;
            int count = Mathf.RoundToInt(rate * 0.5f);
            var data = new float[count];
            var rng = new System.Random(7331);
            float hp = 0f;
            float previous = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                float envelope = Mathf.Clamp01(t / 0.006f) * Mathf.Exp(-t * 7f);
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                hp = 0.85f * (hp + white - previous);
                previous = white;
                float wobble = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 11f * t);
                float buzz = (Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.35f + Mathf.Sin(2f * Mathf.PI * 240f * t) * 0.2f
                              + Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 60f * t)) * 0.08f) * wobble;
                data[i] = (hp * 0.3f + buzz * 0.3f) * envelope;
            }

            for (int s = 0; s < 28; s++)
            {
                int start = rng.Next(0, count - 900);
                float amplitude = (0.35f + (float)rng.NextDouble() * 0.65f) * (1f - start / (float)count);
                float decay = 40f + (float)rng.NextDouble() * 120f;
                for (int j = 0; j < 900; j++)
                    data[start + j] += amplitude * (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-j / decay);
            }

            Normalize(data, 0.9f);
            generatedCrackle = AudioClip.Create("DM_ForceFieldCrackle", count, 1, rate, false);
            generatedCrackle.SetData(data, 0);
            return generatedCrackle;
        }

        /// <summary>A two-second seamless loop of low electrical hum for the optional idle sound.</summary>
        static AudioClip Hum()
        {
            if (generatedHum != null)
                return generatedHum;

            const int rate = 44100;
            int count = rate * 2;
            var data = new float[count];
            var rng = new System.Random(4242);
            float lp = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += (white - lp) * 0.05f;
                float swell = 0.8f + 0.2f * Mathf.Sin(2f * Mathf.PI * 0.5f * t);
                data[i] = (Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.3f
                           + Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.1f + lp * 0.4f) * swell;
            }

            Normalize(data, 0.6f);
            generatedHum = AudioClip.Create("DM_ForceFieldHum", count, 1, rate, false);
            generatedHum.SetData(data, 0);
            return generatedHum;
        }

        static void Normalize(float[] data, float peak)
        {
            float max = 0.0001f;
            for (int i = 0; i < data.Length; i++)
                max = Mathf.Max(max, Mathf.Abs(data[i]));
            float gain = peak / max;
            for (int i = 0; i < data.Length; i++)
                data[i] *= gain;
        }
    }
}
