using System.Collections.Generic;
using Invector;
using Invector.vCharacterController;
using Project.Core;
using Project.Player;
using Project.Progression;
using Project.Survival;
using Project.Vehicles;
using Project.Features.Climb;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Features.Dash
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(600)]
    public sealed class DMDashController : MonoBehaviour
    {
        private const int PlayerLayer = 8;
        private const int ClimbableLayer = 23;

        [SerializeField] private vThirdPersonMotor motor;
        [SerializeField] private Animator animator;
        [SerializeField] private Rigidbody body;
        [SerializeField] private CapsuleCollider capsule;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private LayerMask collisionMask = ~0;

        private readonly Dictionary<Renderer, Material[]> _originalMats = new Dictionary<Renderer, Material[]>();
        private readonly List<Renderer> _bodyRenderers = new List<Renderer>(8);

        private Material _runtimeHolo;
        private ParticleSystem[] _streakSystems = System.Array.Empty<ParticleSystem>();
        private ParticleSystem[] _smokeSystems = System.Array.Empty<ParticleSystem>();
        private Transform _streakRoot;

        private DMClimbController _climb;
        private SurvivalStats _survival;

        private bool _dashing;
        private Vector3 _dashDir;
        private float _dashStartedAt;
        private float _dashEndsAt;
        private float _dashMaxDistance;
        private float _dashTraveled;
        private float _readyAt;
        private float _savedAnimSpeed = 1f;
        private bool _savedAnimatorEnabled = true;
        private bool _savedBodyKinematic;
        private bool _heldLockMovement;
        private bool _heldLockAnimMovement;
        private bool _heldDisableCheckGround;

        private Key _lastTap = Key.None;
        private float _lastTapAt = -10f;

        private vFootStep[] _footSteps;
        private vFootStepTrigger[] _footStepTriggers;
        private bool _footstepsSuppressed;

        public bool IsDashing => _dashing;
        public Vector3 DashDirection => _dashDir;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOnPlayer()
        {
            if (!Application.isPlaying)
                return;

            GameObject player = ResolvePlayerObject();
            if (player == null)
                return;

            if (player.GetComponent<DMDashController>() == null)
                player.AddComponent<DMDashController>();
        }

        private static GameObject ResolvePlayerObject()
        {
            GameObject player = PlayerLocator.FindPlayerObject();
            if (player != null)
                return player;

            player = GameObject.Find("Player_v7");
            if (player != null)
                return player;

            return GameObject.Find("Player_v7 Variant");
        }

        private void Awake()
        {
            if (motor == null)
                motor = GetComponent<vThirdPersonMotor>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            if (body == null)
                body = GetComponent<Rigidbody>();
            if (capsule == null)
                capsule = GetComponent<CapsuleCollider>();
            if (cameraPivot == null && Camera.main != null)
                cameraPivot = Camera.main.transform;

            if (_survival == null)
                _survival = GetComponent<SurvivalStats>();
            if (_climb == null)
                _climb = GetComponent<DMClimbController>();

            if (collisionMask.value == 0)
                collisionMask = BuildCollisionMask();

            CacheBodyRenderers();
            BuildVfx();
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

        private void FixedUpdate()
        {
            if (!_dashing)
                return;

            TickDash();
        }

        private void Update()
        {
            if (GameplayWorldSimulation.IsFrozen)
                return;

            if (_dashing)
                return;

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

        private DM_ClimbDashProfile LiveProfile => DM_ClimbDashProfile.Live;

        private float DoubleTapWindow =>
            LiveProfile != null ? LiveProfile.dashDoubleTapWindow : 0.28f;

        private float BaseDashDistance =>
            LiveProfile != null ? LiveProfile.dashDistance : 4.5f;

        private float BaseDashSpeed =>
            LiveProfile != null ? LiveProfile.dashSpeed : 14f;

        private float BaseDashDuration =>
            LiveProfile != null ? LiveProfile.dashDuration : 0.18f;

        private float DashCooldown =>
            ScaledCooldown(LiveProfile != null ? LiveProfile.dashCooldown : 0.55f);

        private float ScaledCooldown(float baseCooldown)
        {
            float reduction = PlayerSkillAllocator.GetTotalBonusPercent(SkillModifierType.DashCooldownPercent);
            return baseCooldown * Mathf.Max(0.1f, 1f - reduction / 100f);
        }

        private float DashStaminaCost =>
            LiveProfile != null ? LiveProfile.dashStaminaCost : 22f;

        private float DashStaminaTickExtraPercent()
        {
            float baseTick = LiveProfile != null
                ? LiveProfile.dashStaminaTickExtraPercent
                : 0.20f;
            float reduction = PlayerSkillAllocator.GetTotalBonusPercent(SkillModifierType.DashStaminaTickReductionPercent);
            return baseTick * Mathf.Max(0f, 1f - reduction / 100f);
        }

        private float DashCollisionSkin =>
            LiveProfile != null ? LiveProfile.dashCollisionSkin : 0.08f;

        private bool DashDetachesFromClimb => LiveProfile == null || LiveProfile.dashDetachesFromClimb;

        private bool AllowsClimbDash()
        {
            if (LiveProfile != null && LiveProfile.dashAllowedWhileClimbing)
                return true;
            return PlayerSkillAllocator.GetTotalBonusPercent(SkillModifierType.DashClimbUnlock) > 0f;
        }

        private float ScaledDashDistance()
        {
            float meters = BaseDashDistance > 0.1f
                ? BaseDashDistance
                : BaseDashSpeed * Mathf.Max(0.05f, BaseDashDuration);
            return meters * SkillMul(SkillModifierType.DashDistancePercent);
        }

        private float ScaledDashSpeed()
        {
            float baseSpeed = BaseDashSpeed > 0.1f
                ? BaseDashSpeed
                : BaseDashDistance / Mathf.Max(0.05f, BaseDashDuration);
            return Mathf.Max(0.1f, baseSpeed * SkillMul(SkillModifierType.DashSpeedPercent));
        }

        private float ScaledDashDuration(float speed)
        {
            float meters = ScaledDashDistance();
            if (speed > 0.1f)
                return Mathf.Max(0.05f, meters / speed);
            return Mathf.Max(0.05f, BaseDashDuration);
        }

        private bool AllowsAirDash()
        {
            if (LiveProfile != null && LiveProfile.dashAllowAirDash)
                return true;
            return PlayerSkillAllocator.GetTotalBonusPercent(SkillModifierType.DashAirUnlock) > 0f;
        }

        private bool CanDash()
        {
            if (!isActiveAndEnabled)
                return false;
            if (LiveProfile == null || Time.unscaledTime < _readyAt)
                return false;
            if (Time.timeScale <= 0f)
                return false;
            if (PlayerVehicleState.IsMounted)
                return false;

            var landing = GetComponent<DMLandingDirector>();
            if (landing != null && landing.IsLandingLocked)
                return false;

            if (_climb == null)
                _climb = GetComponent<DMClimbController>();

            if (_climb != null && _climb.IsClimbing && !AllowsClimbDash())
                return false;

            if (!AllowsAirDash() && motor != null && !motor.isGrounded)
            {
                if (_climb == null || !_climb.IsClimbing)
                    return false;
            }

            if (_survival == null)
                _survival = GetComponent<SurvivalStats>();
            float cost = DashStaminaCost;
            if (_survival != null && cost > 0f && !_survival.HasStamina(cost))
                return false;

            return true;
        }

        private void TryDoubleTap(Key key, Vector3 localDir)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard[key].wasPressedThisFrame)
                return;

            if (_lastTap == key && Time.unscaledTime - _lastTapAt <= DoubleTapWindow)
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
            Transform pivot = motor != null && motor.rotateTarget != null
                ? motor.rotateTarget
                : cameraPivot != null
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
            if (_climb == null)
                _climb = GetComponent<DMClimbController>();
            if (_climb != null && _climb.IsClimbing && DashDetachesFromClimb)
                _climb.CancelClimb();

            if (_survival == null)
                _survival = GetComponent<SurvivalStats>();
            float cost = DashStaminaCost;
            if (_survival != null && cost > 0f && !_survival.TryConsumeStamina(cost))
                return;

            _survival?.SetStaminaActivity(SurvivalStats.StaminaActivity.DashRecovery, false);
            _survival?.SetStaminaActivity(SurvivalStats.StaminaActivity.Dash, true);

            float speed = ScaledDashSpeed();
            _dashing = true;
            _dashDir = dir;
            _dashStartedAt = Time.unscaledTime;
            _dashEndsAt = _dashStartedAt + ScaledDashDuration(speed);
            _dashMaxDistance = ScaledDashDistance();
            _dashTraveled = 0f;
            _readyAt = _dashEndsAt + Mathf.Max(0f, DashCooldown);

            if (motor != null)
            {
                _heldLockMovement = motor.lockMovement;
                _heldLockAnimMovement = motor.lockAnimMovement;
                _heldDisableCheckGround = motor.disableCheckGround;
                motor.lockMovement = true;
                motor.lockAnimMovement = false;
                motor.disableCheckGround = true;
                motor.input = Vector3.zero;
                motor.inputMagnitude = 0f;
            }

            if (body != null)
            {
                _savedBodyKinematic = body.isKinematic;
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }

                body.isKinematic = true;
            }

            if (animator != null)
            {
                _savedAnimSpeed = animator.speed;
                _savedAnimatorEnabled = animator.enabled;
                animator.speed = 0f;
                animator.enabled = false;
            }

            ApplyHologram(true);
            PlayVfx();
            SetFootstepsSuppressed(true);

            if (TryGetGroundedDashPosition(transform.position, out Vector3 startGrounded))
                ApplyDashPosition(startGrounded);
        }

        private void TickDash()
        {
            float dt = Time.fixedDeltaTime;
            if (dt <= 0f)
                return;

            TickDashStamina(dt);

            float speed = ScaledDashSpeed();
            float step = speed * dt;
            float remaining = _dashMaxDistance - _dashTraveled;
            step = Mathf.Min(step, remaining);

            if (step <= 0.0001f || !TryMoveDash(_dashDir * step, out float moved))
            {
                EndDash(restore: true);
                return;
            }

            _dashTraveled += moved;

            if (_dashTraveled >= _dashMaxDistance - 0.01f || Time.unscaledTime >= _dashEndsAt)
                EndDash(restore: true);
        }

        private void TickDashStamina(float dt)
        {
            if (_survival == null || dt <= 0f)
                return;

            float extraFraction = Mathf.Max(0f, DashStaminaTickExtraPercent());
            if (extraFraction <= 0f)
                return;

            float dashDuration = Mathf.Max(0.05f, _dashEndsAt - _dashStartedAt);
            float totalExtra = DashStaminaCost * extraFraction;
            _survival.SpendStamina(totalExtra / dashDuration * dt);
        }

        private bool TryMoveDash(Vector3 delta, out float movedDistance)
        {
            movedDistance = 0f;
            float distance = delta.magnitude;
            if (distance < 0.0001f)
                return true;

            Vector3 dir = delta / distance;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
                return false;
            dir.Normalize();

            float skin = DashCollisionSkin;

            if (TryGetCapsule(out _, out _, out float radius))
            {
                float probeHeight = Mathf.Max(0.65f, radius * 2.1f);
                Vector3 probeOrigin = transform.position + Vector3.up * probeHeight;
                if (Physics.SphereCast(
                        probeOrigin,
                        radius * 0.82f,
                        dir,
                        out RaycastHit hit,
                        distance + skin,
                        collisionMask,
                        QueryTriggerInteraction.Ignore)
                    && IsBlockingWallHit(hit))
                {
                    distance = Mathf.Max(0f, hit.distance - skin);
                    if (distance < 0.001f)
                        return false;
                }
            }

            Vector3 target = transform.position + dir * distance;
            if (TryGetGroundedDashPosition(target, out Vector3 grounded))
                target = grounded;

            ApplyDashPosition(target);

            movedDistance = distance;
            return true;
        }

        private void ApplyDashPosition(Vector3 target)
        {
            if (body != null && body.isKinematic)
            {
                body.MovePosition(target);
                transform.position = target;
                return;
            }

            transform.position = target;
            if (body != null)
                body.position = target;
        }

        private bool IsBlockingWallHit(RaycastHit hit)
        {
            if (!IsBlockingCollider(hit.collider))
                return false;

            // Foot-level casts treat the floor ahead as a wall; only block steep surfaces.
            return hit.normal.y < 0.55f;
        }

        private bool IsBlockingCollider(Collider collider)
        {
            if (collider == null || collider.isTrigger)
                return false;

            Transform hit = collider.transform;
            if (hit == transform || hit.IsChildOf(transform))
                return false;

            int layer = collider.gameObject.layer;
            if (layer == PlayerLayer || layer == ClimbableLayer)
                return false;

            return true;
        }

        private bool TryGetGroundedDashPosition(Vector3 planarTarget, out Vector3 grounded)
        {
            grounded = planarTarget;
            const float probeUp = 0.55f;
            const float probeDown = 3.5f;
            const float groundSkin = 0.02f;
            Vector3 origin = planarTarget + Vector3.up * probeUp;
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, probeUp + probeDown, collisionMask, QueryTriggerInteraction.Ignore))
                return false;

            if (hit.collider == null || hit.collider.isTrigger)
                return false;

            Transform hitTransform = hit.collider.transform;
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
                return false;

            float bottomOffset = capsule != null
                ? capsule.center.y - capsule.height * 0.5f
                : 0f;
            grounded.y = hit.point.y - bottomOffset + groundSkin;
            return true;
        }

        private void SetFootstepsSuppressed(bool suppress)
        {
            if (_footSteps == null || _footSteps.Length == 0)
                _footSteps = GetComponentsInChildren<vFootStep>(true);

            if (_footStepTriggers == null || _footStepTriggers.Length == 0)
                _footStepTriggers = GetComponentsInChildren<vFootStepTrigger>(true);

            if (suppress == _footstepsSuppressed
                && _footSteps.Length > 0
                && _footStepTriggers.Length > 0)
                return;

            for (int i = 0; i < _footSteps.Length; i++)
            {
                if (_footSteps[i] != null)
                    _footSteps[i].enabled = !suppress;
            }

            for (int i = 0; i < _footStepTriggers.Length; i++)
            {
                vFootStepTrigger trigger = _footStepTriggers[i];
                if (trigger != null)
                    trigger.gameObject.SetActive(!suppress);
            }

            _footstepsSuppressed = suppress;
        }

        private bool TryGetCapsule(out Vector3 bottom, out Vector3 top, out float radius)
        {
            bottom = top = Vector3.zero;
            radius = 0.26f;

            CapsuleCollider col = capsule != null ? capsule : GetComponent<CapsuleCollider>();
            if (col == null)
                return false;

            radius = Mathf.Max(0.05f, col.radius * 0.95f);
            Vector3 worldCenter = col.transform.TransformPoint(col.center);
            float half = Mathf.Max(radius, col.height * 0.5f - radius);
            bottom = worldCenter - Vector3.up * half;
            top = worldCenter + Vector3.up * half;
            return true;
        }

        private static LayerMask BuildCollisionMask()
        {
            int mask = Physics.DefaultRaycastLayers;
            mask &= ~(1 << PlayerLayer);
            return mask;
        }

        private void EndDash(bool restore)
        {
            if (!_dashing)
            {
                ApplyHologram(false);
                return;
            }

            _dashing = false;
            if (_survival != null)
            {
                _survival.SetStaminaActivity(SurvivalStats.StaminaActivity.Dash, false);
                float recoverySeconds = LiveProfile != null ? LiveProfile.dashStaminaRecoverySeconds : 2.5f;
                _survival.BeginDashStaminaRecovery(recoverySeconds);
            }

            StopVfx();
            ApplyHologram(false);
            SetFootstepsSuppressed(false);

            if (animator != null)
            {
                animator.enabled = true;
                float speed = _savedAnimSpeed > 0.01f ? _savedAnimSpeed : 1f;
                animator.speed = speed;
            }

            if (body != null)
            {
                body.isKinematic = false;
                body.useGravity = true;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.WakeUp();
            }

            DMLandingDirector landing = GetComponent<DMLandingDirector>();
            landing?.OnDashEnded();

            if (restore && motor != null && (landing == null || !landing.IsLandingLocked))
                motor.disableCheckGround = _heldDisableCheckGround;
            else if (motor != null)
                motor.disableCheckGround = false;
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
            if (LiveProfile != null && LiveProfile.dashHologramMaterial != null)
                return LiveProfile.dashHologramMaterial;

            if (_runtimeHolo == null)
            {
                Shader shader = Shader.Find("HDRP/Unlit");
                if (shader == null)
                    shader = Shader.Find("Unlit/Color");
                _runtimeHolo = new Material(shader);
                _runtimeHolo.name = "DMDashHologram";
            }

            Color c = LiveProfile != null ? LiveProfile.dashHologramColor : new Color(0.25f, 0.85f, 1f, 0.42f);
            float emit = LiveProfile != null ? LiveProfile.dashHologramEmission : 4f;
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
            _streakSystems = SpawnVfx(
                LiveProfile != null ? LiveProfile.dashStreakPrefab : null,
                LiveProfile != null ? LiveProfile.dashStreakMaterial : null,
                "DashStreaks",
                true);
            _smokeSystems = SpawnVfx(
                LiveProfile != null ? LiveProfile.dashSmokePrefab : null,
                LiveProfile != null ? LiveProfile.dashSmokeMaterial : null,
                "DashSmoke",
                false);
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
                if (rends[i] == null)
                    continue;
                rends[i].enableGPUInstancing = false;
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
            shape.radius = LiveProfile != null ? LiveProfile.dashStreakRadius : 0.55f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = streaks
                ? ParticleSystemRenderMode.Stretch
                : ParticleSystemRenderMode.Billboard;
            if (streaks)
                renderer.lengthScale = LiveProfile != null ? LiveProfile.dashStreakStretch : 3.5f;

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
            if (systems == null || LiveProfile == null)
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
                    main.startColor = LiveProfile.dashStreakColor;
                    main.startLifetime = LiveProfile.dashStreakLifetime;
                    main.startSize = LiveProfile.dashStreakSize;
                }
                else
                {
                    main.startColor = LiveProfile.dashSmokeColor;
                    main.startLifetime = LiveProfile.dashSmokeLifetime;
                    main.startSize = LiveProfile.dashSmokeSize;
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
