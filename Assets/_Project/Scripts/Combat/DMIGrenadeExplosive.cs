using System.Collections;
using System.Collections.Generic;
using Invector;
using Invector.Throw;
using Project.AI;
using Project.AI.Invector;
using Project.Companions;
using Project.Interaction;
using Project.Player.Invector;
using Project.Progression;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Pioneer AOE damage for thrown grenades. Suppresses Invector <see cref="vExplosive"/>
    /// damage (wrong layers / weapon-bridge zeroing) and applies 15–25 to nearby enemies.
    /// Owns cook fuse + accelerating beep when the player cooks before throw.
    /// </summary>
    [DisallowMultipleComponent]
    public class DMIGrenadeExplosive : MonoBehaviour
    {
        public const float DefaultCookFuseSeconds = 10f;

        /// <summary>
        /// Invector instantiates a new world copy from the in-hand throwable. Cook sets
        /// <see cref="vExplosive.method"/> to remote on the hand mesh; that serialized state
        /// clones onto the flier and land collisions never arm — staged here at onThrow.
        /// </summary>
        private static float s_pendingThrownCookFuseSeconds = -1f;

        private const int ExplosionHitBufferSize = 64;
        private static readonly Collider[] ExplosionHitBuffer = new Collider[ExplosionHitBufferSize];
        private static readonly Collider[] HitboxSplashBuffer = new Collider[ExplosionHitBufferSize];

        [Header("AOE Damage")]
        [SerializeField] private float minDamage = 15f;
        [SerializeField] private float maxDamage = 25f;
        [SerializeField] private float explosionRadius = 4f;
        [SerializeField] private float innerRadiusFullDamage = 2f;
        [Tooltip("Damage multiplier at outer edge (1 at/inside inner radius).")]
        [SerializeField, Range(0f, 1f)] private float edgeDamageFalloff = 0.35f;

        [Header("Hit Filter")]
        [SerializeField] private LayerMask hitLayers;
        [SerializeField] private bool damagePlayer = false;
        [SerializeField] private bool damageCompanions = false;

        [Header("Cook Fuse")]
        [SerializeField] private float cookFuseSeconds = DefaultCookFuseSeconds;
        [SerializeField] private float beepIntervalStart = 0.9f;
        [SerializeField] private float beepIntervalEnd = 0.07f;
        [SerializeField] private float beepPitchStart = 0.85f;
        [SerializeField] private float beepPitchEnd = 1.55f;
        [SerializeField] private float beepVolume = 0.55f;
        [SerializeField] private AudioClip beepClip;

        private vExplosive _explosive;
        private vThrowableObject _throwable;
        private AudioSource _beepSource;
        private bool _applied;
        private bool _cooking;
        private bool _fuseRunning;
        private bool _detonating;
        private bool _detonationDamagesPlayer;
        private bool _cookReleasedFromHand;
        private float _fuseRemaining;
        private float _fuseDuration;
        private float _nextBeepTime;
        private vExplosive.ExplosiveMethod _methodBeforeCook;
        private GameObject _damageSource;
        private Coroutine _fuseRoutine;

        public bool IsCooking => _cooking;
        public bool IsFuseRunning => _fuseRunning;
        public float FuseRemaining => Mathf.Max(0f, _fuseRemaining);
        public float FuseDuration => Mathf.Max(0.01f, _fuseDuration);

        /// <summary>World-space blast radius for UI ring and AOE (includes splash radius skills).</summary>
        public float EffectiveExplosionRadius =>
            explosionRadius * (DM_CombatCoreProfile.Live != null &&
                               DM_CombatCoreProfile.Live.grenadeLandingRingUsesSplashSkills
                ? PlayerSkillAllocator.GetSplashRadiusMultiplier()
                : 1f);

        /// <summary>Next detonation also damages the thrower (cooked-too-long in hand).</summary>
        public void PrepareDetonationDamagesPlayer(bool includePlayer) => _detonationDamagesPlayer = includePlayer;

        /// <summary>Fired after cook detonation resolves — use for throw-mode cleanup only.</summary>
        public event System.Action<DMIGrenadeExplosive> CookFuseExpired;

        private void Awake()
        {
            EnsurePioneerSplashLayersFromProfileOrPrefab();

            _throwable = GetComponent<vThrowableObject>();
            if (_throwable != null)
                _throwable.onThrow.AddListener(SetDamageSource);

            _explosive = GetComponent<vExplosive>();
            if (_explosive == null)
            {
                RefreshFromProfile();
                EnsureBeepSource();
                return;
            }

            // Source vFragmentedGrenade ships vExplosive disabled — collision/timer never arm without this.
            if (!_explosive.enabled)
                _explosive.enabled = true;

            // Invector overlap uses Default|Player|BodyPart — not Enemy — and routes through
            // the player's weapon damage bridge (0 when a gun is drawn). Zero that path.
            if (_explosive.damage != null)
                _explosive.damage.damageValue = 0;

            _methodBeforeCook = _explosive.method;
            WireExplosiveDetonationListener();
            RefreshFromProfile();
            EnsureBeepSource();
        }

        private void OnEnable() => RefreshFromProfile();

        /// <summary>Apply live combat profile (Genesis / Combat Studio Throwables). Called before detonation and while aiming.</summary>
        public void RefreshFromProfile()
        {
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            if (profile == null)
                return;

            minDamage = Mathf.Max(1f, profile.grenadeMinDamage);
            maxDamage = Mathf.Max(minDamage, profile.grenadeMaxDamage);
            explosionRadius = Mathf.Max(0.1f, profile.grenadeExplosionRadius);
            innerRadiusFullDamage = Mathf.Clamp(profile.grenadeInnerRadiusFullDamage, 0f, explosionRadius);
            edgeDamageFalloff = profile.grenadeEdgeDamageFalloff;
            damagePlayer = profile.grenadeSplashDamagesPlayer;
            damageCompanions = profile.grenadeSplashDamagesCompanions;
            cookFuseSeconds = profile.grenadeCookSecondsMax;

            if (profile.grenadePioneerSplashLayerMask.value != 0)
                hitLayers = profile.grenadePioneerSplashLayerMask;
            else
                EnsurePioneerSplashLayersFromProfileOrPrefab();

            if (_explosive != null)
            {
                _explosive.timeToExplode = Mathf.Max(0.1f, profile.grenadeLandFuseSeconds);
                _explosive.explosionForce = profile.grenadeExplosionForce;
                _explosive.upwardsModifier = profile.grenadeExplosionUpwardsModifier;
                _explosive.minExplosionRadius = innerRadiusFullDamage;
                _explosive.maxExplosionRadius = explosionRadius;

                if (profile.grenadeVExplosiveDamageLayerMask.value != 0)
                    _explosive.applyDamageLayer = profile.grenadeVExplosiveDamageLayerMask;
                if (profile.grenadeVExplosiveForceLayerMask.value != 0)
                    _explosive.applyForceLayer = profile.grenadeVExplosiveForceLayerMask;
            }
        }

        /// <summary>
        /// Prefab hitLayers win when set. Profile mask overrides when non-zero. Runtime AddComponent-only fallback includes Enemy + DMHitbox.
        /// </summary>
        private void EnsurePioneerSplashLayersFromProfileOrPrefab()
        {
            if (hitLayers.value != 0)
                return;

            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            if (profile != null && profile.grenadePioneerSplashLayerMask.value != 0)
            {
                hitLayers = profile.grenadePioneerSplashLayerMask;
                return;
            }

            int mask = 0;
            TryAddLayer(ref mask, "Enemy");
            TryAddLayer(ref mask, "BodyPart");
            TryAddLayer(ref mask, "Default");
            TryAddLayer(ref mask, DMEnemyHitQuery.HitboxLayerName);
            hitLayers = mask != 0 ? mask : DMEnemyHitQuery.SharedRangedLayerMask;
        }

        private void OnDestroy()
        {
            if (_throwable != null)
                _throwable.onThrow.RemoveListener(SetDamageSource);

            if (_explosive != null)
                _explosive.onExplode.RemoveListener(OnInvectorExploded);

            StopFuseInternal(resetMethod: false);
        }

        private void Update()
        {
            if (!_fuseRunning || _detonating)
                return;

            UpdateBeep();
        }

        /// <summary>Pass remaining cook fuse from the in-hand instance to the thrown world copy.</summary>
        public static void StageThrownCookFuse(float fuseSecondsRemaining)
        {
            s_pendingThrownCookFuseSeconds = fuseSecondsRemaining > 0f ? fuseSecondsRemaining : -1f;
        }

        /// <summary>
        /// Called from throwable onThrow so aggro / feedback attribute the player, not the grenade.
        /// </summary>
        public void SetDamageSource(Transform sender)
        {
            _damageSource = sender != null ? sender.gameObject : null;
            if (_explosive != null && sender != null)
                _explosive.overrideDamageSender = sender;

            ArmThrownDetonationAfterLaunch();
        }

        /// <summary>
        /// World flight instance: restore land fuse or continue a staged cook on this copy.
        /// </summary>
        private void ArmThrownDetonationAfterLaunch()
        {
            if (_explosive == null || _detonating || _applied)
                return;

            if (s_pendingThrownCookFuseSeconds > 0f)
            {
                float fuse = s_pendingThrownCookFuseSeconds;
                s_pendingThrownCookFuseSeconds = -1f;
                BeginCook(fuse, _damageSource);
                return;
            }

            if (_fuseRunning)
                return;

            _explosive.method = vExplosive.ExplosiveMethod.collisionEnterTimer;
            _methodBeforeCook = _explosive.method;
            RefreshFromProfile();
        }

        /// <summary>
        /// Start the cook fuse (default 10s). Beep rate accelerates as time runs out.
        /// Switches Invector to remote so bounce-timer cannot double-arm the grenade.
        /// </summary>
        public bool BeginCook(float fuseSeconds = -1f, GameObject damageSource = null)
        {
            if (_detonating || _applied)
                return false;

            if (_fuseRunning)
                return true;

            if (damageSource != null)
                _damageSource = damageSource;

            if (_explosive != null)
            {
                _methodBeforeCook = _explosive.method;
                // Prevent collisionEnterTimer from starting a second fuse after throw.
                _explosive.method = vExplosive.ExplosiveMethod.remote;
                if (_damageSource != null)
                    _explosive.overrideDamageSender = _damageSource.transform;
            }

            _fuseDuration = fuseSeconds > 0f ? fuseSeconds : Mathf.Max(0.1f, cookFuseSeconds);
            _fuseRemaining = _fuseDuration;
            _cooking = true;
            _fuseRunning = true;
            _cookReleasedFromHand = false;
            _nextBeepTime = Time.time;
            EnsureBeepSource();
            ConfigureBeepSpatialForCook();
            PlayBeep();

            if (_fuseRoutine != null)
                StopCoroutine(_fuseRoutine);
            _fuseRoutine = StartCoroutine(CookFuseRoutine());
            return true;
        }

        /// <summary>
        /// Abort cook before throw. Restores the uncooked Invector explode method.
        /// </summary>
        public void CancelCook()
        {
            if (_detonating || _applied)
                return;

            if (!_cooking && !_fuseRunning)
                return;

            StopFuseInternal(resetMethod: true);
        }

        /// <summary>
        /// Mark that the cooked grenade has left the hand — fuse + beep keep running in world.
        /// </summary>
        public void NotifyThrown()
        {
            _cooking = _fuseRunning;
            _cookReleasedFromHand = true;
            ConfigureBeepSpatialForCook();

            // Cook uses remote detonation; uncooked throws must restore land fuse on vExplosive.
            if (_explosive != null && !_fuseRunning && !_detonating && !_applied)
                _explosive.method = _methodBeforeCook;
        }

        private IEnumerator CookFuseRoutine()
        {
            while (_fuseRemaining > 0f && !_detonating)
            {
                yield return null;
                _fuseRemaining -= Time.deltaTime;
            }

            _fuseRoutine = null;
            if (!_detonating && _fuseRemaining <= 0f)
                DetonateFromCookFuse();
        }

        private void DetonateFromCookFuse()
        {
            if (_detonating || _applied)
                return;

            _detonating = true;
            _fuseRunning = false;
            _cooking = false;
            StopBeep();

            DMIGrenadeCookController.TryPrepareCookOutDetonation(this);

            if (_explosive != null)
            {
                _explosive.method = vExplosive.ExplosiveMethod.remote;
                _explosive.ActiveExplosion();
            }
            else
            {
                ApplyPioneerAoe();
                Destroy(gameObject, 0.15f);
            }

            CookFuseExpired?.Invoke(this);
        }

        private void StopFuseInternal(bool resetMethod)
        {
            if (_fuseRoutine != null)
            {
                StopCoroutine(_fuseRoutine);
                _fuseRoutine = null;
            }

            _fuseRunning = false;
            _cooking = false;
            _fuseRemaining = 0f;
            StopBeep();

            if (resetMethod && _explosive != null && !_detonating && !_applied)
                _explosive.method = _methodBeforeCook;
        }

        private void UpdateBeep()
        {
            if (Time.time < _nextBeepTime)
                return;

            PlayBeep();
            float t = 1f - Mathf.Clamp01(_fuseRemaining / _fuseDuration);
            float interval = Mathf.Lerp(beepIntervalStart, beepIntervalEnd, t * t);
            _nextBeepTime = Time.time + Mathf.Max(0.04f, interval);
        }

        private void EnsureBeepSource()
        {
            if (_beepSource == null)
            {
                _beepSource = gameObject.GetComponent<AudioSource>();
                if (_beepSource == null)
                    _beepSource = gameObject.AddComponent<AudioSource>();

                _beepSource.playOnAwake = false;
                _beepSource.loop = false;
                _beepSource.minDistance = 1.5f;
                _beepSource.maxDistance = 28f;
                _beepSource.rolloffMode = AudioRolloffMode.Linear;

                if (beepClip == null)
                    beepClip = CreateProceduralBeepClip();
            }

            ConfigureBeepSpatialForCook();
        }

        private void ConfigureBeepSpatialForCook()
        {
            if (_beepSource == null)
                return;

            // In-hand: 2D beep at the listener so cook ticks match post-throw audibility.
            _beepSource.spatialBlend = _cookReleasedFromHand ? 1f : 0f;
        }

        private void PlayBeep()
        {
            EnsureBeepSource();
            if (_beepSource == null || beepClip == null)
                return;

            float t = 1f - Mathf.Clamp01(_fuseRemaining / _fuseDuration);
            _beepSource.pitch = Mathf.Lerp(beepPitchStart, beepPitchEnd, t);
            _beepSource.PlayOneShot(beepClip, beepVolume);
        }

        private void StopBeep()
        {
            if (_beepSource != null && _beepSource.isPlaying)
                _beepSource.Stop();
        }

        private static AudioClip CreateProceduralBeepClip()
        {
            const int sampleRate = 22050;
            const float duration = 0.055f;
            const float frequency = 980f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var clip = AudioClip.Create("DMI_GrenadeCookBeep", sampleCount, 1, sampleRate, false);
            var samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = 1f - (t / duration);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * envelope;
            }

            clip.SetData(samples, 0);
            return clip;
        }

        private void OnInvectorExploded()
        {
            StopFuseInternal(resetMethod: false);
            TryActivateEmbeddedExplosionVfx();
            ApplyPioneerAoe();
        }

        /// <summary>
        /// DM_Frag_Grenade prefab variant can ship a null RemoveParentOfOther target that throws inside vExplosive.Explode().
        /// Replace the serialized UnityEvent at runtime; embedded/profile VFX are activated in <see cref="OnInvectorExploded"/>.
        /// </summary>
        private void WireExplosiveDetonationListener()
        {
            if (_explosive == null)
                return;

            _explosive.onExplode = new UnityEngine.Events.UnityEvent();
            _explosive.onExplode.AddListener(OnInvectorExploded);
        }

        private void TryActivateEmbeddedExplosionVfx()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child == null)
                    continue;

                if (!child.name.Contains("Explosion", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                child.SetParent(null, worldPositionStays: true);
                child.gameObject.SetActive(true);
            }
        }

        public void ApplyPioneerAoe()
        {
            if (_applied)
                return;

            _applied = true;
            RefreshFromProfile();

            if (_damageSource == null && _explosive != null && _explosive.overrideDamageSender != null)
                _damageSource = _explosive.overrideDamageSender.gameObject;

            SpawnProfileExplosionVfxIfAny();

            float rolled = Random.Range(minDamage, maxDamage);
            rolled *= PlayerSkillAllocator.GetSplashDamageMultiplier();
            Vector3 origin = transform.position;
            float radius = Mathf.Max(0.1f, EffectiveExplosionRadius);
            bool cookOutSelfHit = _detonationDamagesPlayer;
            _detonationDamagesPlayer = false;

            if (cookOutSelfHit)
                ApplyCookOutSelfDamage();

            // Cook-out uses flat self damage only; overlap splash still hits enemies.
            bool hurtPlayer = damagePlayer && !cookOutSelfHit;

            TryPlayDetonationCameraShake();

            var damagedDamageables = new HashSet<IDamageable>();

            int hitCount = Physics.OverlapSphereNonAlloc(
                origin,
                radius,
                ExplosionHitBuffer,
                hitLayers,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                Collider col = ExplosionHitBuffer[i];
                ExplosionHitBuffer[i] = null;
                ApplySplashToCollider(col, origin, radius, rolled, hurtPlayer, damagedDamageables);
            }

            if (DMEnemyHitQuery.AnyRigActive)
            {
                int hitboxCount = DMEnemyHitQuery.CollectHitboxesInRadiusNonAlloc(
                    origin,
                    radius,
                    _damageSource,
                    HitboxSplashBuffer);
                for (int i = 0; i < hitboxCount; i++)
                {
                    Collider col = HitboxSplashBuffer[i];
                    HitboxSplashBuffer[i] = null;
                    ApplySplashToCollider(col, origin, radius, rolled, hurtPlayer, damagedDamageables);
                }
            }
        }

        private void ApplySplashToCollider(
            Collider col,
            Vector3 origin,
            float radius,
            float rolledDamage,
            bool hurtPlayer,
            HashSet<IDamageable> damagedDamageables)
        {
            if (col == null)
                return;

            if (CombatHitResolver.IsOwnerCollider(_damageSource, col))
                return;

            if (DMEnemyHitQuery.IsSupersededByHitboxRig(col))
                return;

            float distance = Vector3.Distance(origin, GetClosestPointSafe(col, origin));
            float damage = ScaleDamageByDistance(rolledDamage, distance, radius);
            if (damage <= 0f)
                return;

            IDamageable damageable = DamageableUtility.GetDamageable(col);
            if (damageable == null || !damagedDamageables.Add(damageable))
                return;

            if (damageable is EnemyHealth enemy)
            {
                if (enemy.IsDead)
                    return;

                Vector3 blastOutward = enemy.transform.position - origin;
                if (blastOutward.sqrMagnitude < 0.0001f)
                    blastOutward = Vector3.up;
                enemy.GetComponent<EnemyInvectorRagdollBridge>()?.RememberHitForDeath(
                    col.bounds.center,
                    blastOutward,
                    damage,
                    _damageSource != null ? _damageSource.transform : null);

                enemy.TakeDamage(damage, _damageSource, isCritical: false);
                if (!enemy.IsDead)
                    DMEnemyGrenadeBlastReaction.TryApplyAfterDamage(enemy, col, origin, damage, _damageSource);
                SpawnHitFeedback(col, damage, enemy);
                return;
            }

            if (damageable is CompanionHealth companion)
            {
                if (!damageCompanions || companion.IsDead)
                    return;

                ((IDamageable)companion).TakeDamage(damage, _damageSource, false);
                return;
            }

            if (hurtPlayer && damageable is not EnemyHealth)
                damageable.TakeDamage(damage, _damageSource, false);
        }

        private void SpawnProfileExplosionVfxIfAny()
        {
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            if (profile == null || profile.grenadeExplosionVfxPrefab == null)
                return;

            GameObject fx = Instantiate(profile.grenadeExplosionVfxPrefab, transform.position, Quaternion.identity);
            Destroy(fx, 8f);
        }

        private void TryPlayDetonationCameraShake()
        {
            if (_damageSource == null)
                return;

            if (_damageSource.GetComponentInParent<PioneerShooterMeleeInput>() == null)
                return;

            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            if (profile == null || profile.grenadeCameraShakeAmplitude <= 0f)
                return;

            DMCombatCameraShake.Play(
                profile.grenadeCameraShakeAmplitude,
                profile.grenadeCameraShakeDurationSeconds);
        }

        private void ApplyCookOutSelfDamage()
        {
            if (_damageSource == null)
                return;

            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            float amount = profile != null ? profile.grenadeCookOutSelfDamage : 50f;
            if (amount <= 0f)
                return;

            IDamageable player = _damageSource.GetComponentInParent<IDamageable>();
            if (player != null && player is not EnemyHealth)
                player.TakeDamage(amount, _damageSource, false);
        }

        private float ScaleDamageByDistance(float baseDamage, float distance, float outerRadius)
        {
            if (distance <= innerRadiusFullDamage)
                return baseDamage;

            if (distance >= outerRadius)
                return baseDamage * edgeDamageFalloff;

            float t = 1f - Mathf.InverseLerp(innerRadiusFullDamage, outerRadius, distance);
            float multiplier = Mathf.Lerp(edgeDamageFalloff, 1f, t);
            return baseDamage * multiplier;
        }

        /// <summary>
        /// Collider.ClosestPoint only supports Box/Sphere/Capsule and convex MeshColliders.
        /// Non-convex meshes, terrain, etc. log Errors — fall back to AABB closest point.
        /// </summary>
        private static Vector3 GetClosestPointSafe(Collider collider, Vector3 point)
        {
            if (collider == null)
                return point;

            if (collider is BoxCollider
                || collider is SphereCollider
                || collider is CapsuleCollider
                || (collider is MeshCollider meshCollider && meshCollider.convex))
            {
                return collider.ClosestPoint(point);
            }

            return collider.bounds.ClosestPoint(point);
        }

        private static void SpawnHitFeedback(Collider col, float damage, EnemyHealth enemy = null)
        {
            Vector3 point = col.bounds.center;
            // Body type aware (clipfix): Androids / Robots never get the legacy red blood splatter; they use the hit-mark profile's
            // coolant / spark slots (the same prefabs the melee and bullet hit-mark path uses).
            DMEnemyHitMarks marks = enemy != null ? enemy.GetComponent<DMEnemyHitMarks>() : null;
            if (marks != null && marks.BodyType != DMEnemyBodyType.Humanoid)
            {
                DM_EnemyHitMarkProfile profile = DM_EnemyHitMarkProfile.LiveOrDefault;
                if (profile != null)
                {
                    DMEnemyBodyHitResponse response = profile.GetResponse(marks.BodyType);
                    foreach (DMEnemyHitFxKind kind in new[] { response.splatter, response.sparks })
                    {
                        GameObject prefab = kind == DMEnemyHitFxKind.None || kind == DMEnemyHitFxKind.Blood ? null : profile.ResolveFxPrefab(kind);
                        if (prefab == null)
                            continue;
                        GameObject fx = Object.Instantiate(prefab, point, Quaternion.LookRotation(Vector3.up));
                        Object.Destroy(fx, 4f);
                    }
                }

                return;
            }

            CombatHitVfx.SpawnBloodSplatter(point, Vector3.up, Vector3.down, damage);
        }

        private static void TryAddLayer(ref int mask, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
                mask |= 1 << layer;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.35f, 0.1f, 0.25f);
            Gizmos.DrawSphere(transform.position, explosionRadius);
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.35f);
            Gizmos.DrawSphere(transform.position, innerRadiusFullDamage);
        }
#endif
    }
}
