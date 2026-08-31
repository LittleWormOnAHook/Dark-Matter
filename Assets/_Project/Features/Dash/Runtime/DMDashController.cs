using System.Collections.Generic;
using Invector.vCharacterController;
using Project.Player;
using Project.Progression;
using Project.Vehicles;
using Project.Features.Climb;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Features.Dash
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-30)]
    public sealed class DMDashController : MonoBehaviour
    {
        public const string ResourcesPath = "Dash/DMDashProfile";

        [SerializeField] private DMDashProfile profile;
        [SerializeField] private vThirdPersonMotor motor;
        [SerializeField] private Animator animator;
        [SerializeField] private CharacterController character;
        [SerializeField] private Transform cameraPivot;

        private readonly Dictionary<Renderer, Material[]> _originalMats = new Dictionary<Renderer, Material[]>();
        private readonly List<Renderer> _bodyRenderers = new List<Renderer>(8);

        private Material _runtimeHolo;
        private ParticleSystem[] _streakSystems = System.Array.Empty<ParticleSystem>();
        private ParticleSystem[] _smokeSystems = System.Array.Empty<ParticleSystem>();
        private Transform _streakRoot;

        private bool _dashing;
        private Vector3 _dashDir;
        private float _dashStartedAt;
        private float _dashEndsAt;
        private float _readyAt;
        private float _savedAnimSpeed = 1f;
        private bool _heldLockMovement;
        private bool _heldLockAnimMovement;

        private Key _lastTap = Key.None;
        private float _lastTapAt = -10f;

        public bool IsDashing => _dashing;
        public Vector3 DashDirection => _dashDir;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOnPlayer()
        {
            if (!Application.isPlaying)
                return;

            GameObject player = GameObject.Find("Player_v7");
            if (player == null)
                return;
            if (player.GetComponent<DMDashController>() == null)
                player.AddComponent<DMDashController>();
            DMHangLegOverlay.Bind(player);
        }

        private void Awake()
        {
            if (profile == null)
                profile = Resources.Load<DMDashProfile>(ResourcesPath);
            if (motor == null)
                motor = GetComponent<vThirdPersonMotor>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            if (character == null)
                character = GetComponent<CharacterController>();
            if (cameraPivot == null && Camera.main != null)
                cameraPivot = Camera.main.transform;

            CacheBodyRenderers();
            BuildVfx();
            DMHangLegOverlay.Bind(gameObject);
        }

        private void OnDisable()
        {
            EndDash(restore: true);
        }

        private void OnDestroy()
        {
            if (_runtimeHolo != null)
                Destroy(_runtimeHolo);
        }

        private void Update()
        {
            if (_dashing)
            {
                TickDash();
                return;
            }

            if (!CanDash())
                return;

            TryDoubleTap(Key.W, Vector3.forward);
            TryDoubleTap(Key.S, Vector3.back);
            TryDoubleTap(Key.A, Vector3.left);
            TryDoubleTap(Key.D, Vector3.right);
        }


        private static float SkillMul(SkillModifierType type)
        {
            return 1f + PlayerSkillAllocator.GetTotalBonusPercent(type) / 100f;
        }

        private float ScaledDashDistance()
        {
            float meters = profile.distance > 0.1f
                ? profile.distance
                : profile.speed * Mathf.Max(0.05f, profile.duration);
            return meters * SkillMul(SkillModifierType.DashDistancePercent);
        }

        private float ScaledDashSpeed()
        {
            float baseSpeed = profile.speed > 0.1f
                ? profile.speed
                : profile.distance / Mathf.Max(0.05f, profile.duration);
            return Mathf.Max(0.1f, baseSpeed * SkillMul(SkillModifierType.DashSpeedPercent));
        }

        private float ScaledDashDuration(float speed)
        {
            float meters = ScaledDashDistance();
            if (speed > 0.1f)
                return Mathf.Max(0.05f, meters / speed);
            return Mathf.Max(0.05f, profile.duration);
        }

        private bool AllowsAirDash()
        {
            if (profile != null && profile.allowAirDash)
                return true;
            return PlayerSkillAllocator.GetTotalBonusPercent(SkillModifierType.DashAirUnlock) > 0f;
        }
        private bool CanDash()
        {
            if (!isActiveAndEnabled)
                return false;
            if (profile == null || Time.unscaledTime < _readyAt)
                return false;
            if (Time.timeScale <= 0f)
                return false;
            if (PlayerVehicleState.IsMounted)
                return false;

            var landing = GetComponent<DMLandingDirector>();
            if (landing != null && landing.IsLandingLocked)
                return false;

            var climb = GetComponent<DMClimbController>();
            if (climb != null && climb.IsClimbing)
                return false;

            if (!AllowsAirDash() && motor != null && !motor.isGrounded)
                return false;

            return true;
        }

        private void TryDoubleTap(Key key, Vector3 localDir)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard[key].wasPressedThisFrame)
                return;

            if (_lastTap == key && Time.unscaledTime - _lastTapAt <= profile.doubleTapWindow)
            {
                _lastTap = Key.None;
                StartDash(WorldDir(localDir));
                return;
            }

            _lastTap = key;
            _lastTapAt = Time.unscaledTime;
        }

        private Vector3 WorldDir(Vector3 local)
        {
            Transform pivot = cameraPivot != null
                ? cameraPivot
                : (Camera.main != null ? Camera.main.transform : transform);

            Vector3 forward = pivot.forward;
            Vector3 right = pivot.right;
            forward.y = 0f;
            right.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = transform.forward;
            if (right.sqrMagnitude < 0.001f)
                right = transform.right;
            forward.Normalize();
            right.Normalize();

            Vector3 world = forward * local.z + right * local.x;
            world.y = 0f;
            if (world.sqrMagnitude < 0.001f)
                world = transform.forward;
            return world.normalized;
        }

        private void StartDash(Vector3 dir)
        {
            _dashing = true;
            _dashDir = dir;
            _dashStartedAt = Time.unscaledTime;
            _dashEndsAt = _dashStartedAt + ScaledDashDuration(ScaledDashSpeed());
            _readyAt = _dashEndsAt + Mathf.Max(0f, profile.cooldown);

            if (motor != null)
            {
                _heldLockMovement = motor.lockMovement;
                _heldLockAnimMovement = motor.lockAnimMovement;
                motor.lockMovement = true;
                motor.lockAnimMovement = true;
                motor.input = Vector3.zero;
                motor.inputMagnitude = 0f;
            }

            if (animator != null)
            {
                _savedAnimSpeed = animator.speed;
                animator.speed = Mathf.Clamp01(profile.animationSpeed);
            }

            ApplyHologram(true);
            PlayVfx();
        }

        private void TickDash()
        {
            float dt = Time.deltaTime;
            float speed = ScaledDashSpeed();
            Vector3 delta = _dashDir * speed * dt;

            if (character != null && character.enabled)
                character.Move(delta);
            else
                transform.position += delta;

            if (Time.unscaledTime >= _dashEndsAt)
                EndDash(restore: true);
        }

        private void EndDash(bool restore)
        {
            if (!_dashing)
            {
                ApplyHologram(false);
                return;
            }

            _dashing = false;
            StopVfx();
            ApplyHologram(false);

            if (animator != null)
                animator.speed = _savedAnimSpeed;

            if (restore && motor != null)
            {
                motor.lockMovement = _heldLockMovement;
                motor.lockAnimMovement = _heldLockAnimMovement;
            }
        }

        private void CacheBodyRenderers()
        {
            _bodyRenderers.Clear();
            Renderer[] all = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Renderer r = all[i];
                if (r == null || r is ParticleSystemRenderer)
                    continue;
                if (r.GetComponent<ParticleSystem>() != null)
                    continue;
                _bodyRenderers.Add(r);
            }
        }

        private void ApplyHologram(bool on)
        {
            if (on)
            {
                Material holo = GetHologramMaterial();
                _originalMats.Clear();
                for (int i = 0; i < _bodyRenderers.Count; i++)
                {
                    Renderer r = _bodyRenderers[i];
                    if (r == null)
                        continue;
                    _originalMats[r] = r.sharedMaterials;
                    Material[] swap = new Material[r.sharedMaterials.Length];
                    for (int m = 0; m < swap.Length; m++)
                        swap[m] = holo;
                    r.materials = swap;
                }
                return;
            }

            foreach (var kv in _originalMats)
            {
                if (kv.Key != null)
                    kv.Key.sharedMaterials = kv.Value;
            }
            _originalMats.Clear();
        }

        private Material GetHologramMaterial()
        {
            if (profile != null && profile.hologramMaterial != null)
                return profile.hologramMaterial;

            if (_runtimeHolo == null)
            {
                Shader shader = Shader.Find("HDRP/Unlit");
                if (shader == null)
                    shader = Shader.Find("Unlit/Color");
                _runtimeHolo = new Material(shader);
                _runtimeHolo.name = "DMDashHologram";
            }

            Color c = profile != null ? profile.hologramColor : new Color(0.25f, 0.85f, 1f, 0.42f);
            float emit = profile != null ? profile.hologramEmission : 4f;
            if (_runtimeHolo.HasProperty("_UnlitColor"))
                _runtimeHolo.SetColor("_UnlitColor", c);
            if (_runtimeHolo.HasProperty("_Color"))
                _runtimeHolo.SetColor("_Color", c);
            if (_runtimeHolo.HasProperty("_EmissiveColor"))
                _runtimeHolo.SetColor("_EmissiveColor", c * emit);
            if (_runtimeHolo.HasProperty("_EmissionColor"))
            {
                _runtimeHolo.EnableKeyword("_EMISSION");
                _runtimeHolo.SetColor("_EmissionColor", c * emit);
            }

            return _runtimeHolo;
        }

        private void BuildVfx()
        {
            _streakSystems = SpawnVfx(profile != null ? profile.streakPrefab : null, profile != null ? profile.streakMaterial : null, "DashStreaks", true);
            _smokeSystems = SpawnVfx(profile != null ? profile.smokePrefab : null, profile != null ? profile.smokeMaterial : null, "DashSmoke", false);
        }

        private ParticleSystem[] SpawnVfx(GameObject prefab, Material mat, string fallbackName, bool streaks)
        {
            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab, transform);
                go.name = prefab.name;
                go.transform.localPosition = Vector3.up * 0.9f;
                go.transform.localRotation = Quaternion.identity;
            }
            else
            {
                ParticleSystem created = CreateParticles(fallbackName, streaks);
                go = created.gameObject;
            }

            ApplyVfxMaterial(go, mat);
            if (streaks)
                _streakRoot = go.transform;
            return go.GetComponentsInChildren<ParticleSystem>(true);
        }

        private static void ApplyVfxMaterial(GameObject root, Material mat)
        {
            if (root == null || mat == null)
                return;

            ParticleSystemRenderer[] rends = root.GetComponentsInChildren<ParticleSystemRenderer>(true);
            for (int i = 0; i < rends.Length; i++)
            {
                if (rends[i] != null)
                    rends[i].enableGPUInstancing = false;
                    if (mat != null)
                        rends[i].sharedMaterial = mat;
            }
        }

        private ParticleSystem CreateParticles(string name, bool streaks)
        {
            Transform existing = transform.Find(name);
            ParticleSystem ps = existing != null ? existing.GetComponent<ParticleSystem>() : null;
            if (ps == null)
            {
                GameObject go = new GameObject(name);
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.up * 0.9f;
                ps = go.AddComponent<ParticleSystem>();
            }

            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;

            var emission = ps.emission;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = profile != null ? profile.streakRadius : 0.55f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = streaks
                ? ParticleSystemRenderMode.Stretch
                : ParticleSystemRenderMode.Billboard;
            if (streaks)
                renderer.lengthScale = profile != null ? profile.streakStretch : 3.5f;

            return ps;
        }

        private void PlayVfx()
        {
            AlignStreaksToDash();
            PlaySystems(_streakSystems, true);
            PlaySystems(_smokeSystems, false);
        }

        private void AlignStreaksToDash()
        {
            if (_streakRoot == null)
                return;

            Vector3 dir = _dashDir;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
                dir = transform.forward;
            _streakRoot.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        private void PlaySystems(ParticleSystem[] systems, bool streaks)
        {
            if (systems == null || profile == null)
                return;

            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem ps = systems[i];
                if (ps == null)
                    continue;

                var main = ps.main;
                main.playOnAwake = false;
                if (streaks)
                {
                    main.startColor = profile.streakColor;
                    main.startLifetime = profile.streakLifetime;
                    main.startSize = profile.streakSize;
                }
                else
                {
                    main.startColor = profile.smokeColor;
                    main.startLifetime = profile.smokeLifetime;
                    main.startSize = profile.smokeSize;
                }

                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(true);
            }
        }

        private void StopVfx()
        {
            StopSystems(_streakSystems);
            StopSystems(_smokeSystems);
        }

        private static void StopSystems(ParticleSystem[] systems)
        {
            if (systems == null)
                return;
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] != null)
                    systems[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
}
