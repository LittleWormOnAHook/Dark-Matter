using System.Collections;
using System.Collections.Generic;
using Invector;
using Invector.Throw;
using Project.AI;
using Project.Companions;
using Project.Interaction;
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

        /// <summary>Fired when a cook fuse reaches zero (in-hand or after throw).</summary>
        public event System.Action<DMIGrenadeExplosive> CookFuseExpired;

        private void Awake()
        {
            if (hitLayers.value == 0)
            {
                int mask = 0;
                TryAddLayer(ref mask, "Enemy");
                TryAddLayer(ref mask, "BodyPart");
                TryAddLayer(ref mask, "Default");
                hitLayers = mask;
            }

            _throwable = GetComponent<vThrowableObject>();
            if (_throwable != null)
                _throwable.onThrow.AddListener(SetDamageSource);

            _explosive = GetComponent<vExplosive>();
            if (_explosive == null)
                return;

            // Invector overlap uses Default|Player|BodyPart — not Enemy — and routes through
            // the player's weapon damage bridge (0 when a gun is drawn). Zero that path.
            if (_explosive.damage != null)
                _explosive.damage.damageValue = 0;

            _explosive.onExplode.AddListener(OnInvectorExploded);
            EnsureBeepSource();
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

        /// <summary>
        /// Called from throwable onThrow so aggro / feedback attribute the player, not the grenade.
        /// </summary>
        public void SetDamageSource(Transform sender)
        {
            _damageSource = sender != null ? sender.gameObject : null;
            if (_explosive != null && sender != null)
                _explosive.overrideDamageSender = sender;
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
            _nextBeepTime = Time.time;
            EnsureBeepSource();
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

            CookFuseExpired?.Invoke(this);

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
            if (_beepSource != null)
                return;

            _beepSource = gameObject.GetComponent<AudioSource>();
            if (_beepSource == null)
                _beepSource = gameObject.AddComponent<AudioSource>();

            _beepSource.playOnAwake = false;
            _beepSource.loop = false;
            _beepSource.spatialBlend = 1f;
            _beepSource.minDistance = 1.5f;
            _beepSource.maxDistance = 28f;
            _beepSource.rolloffMode = AudioRolloffMode.Linear;

            if (beepClip == null)
                beepClip = CreateProceduralBeepClip();
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
            ApplyPioneerAoe();
        }

        public void ApplyPioneerAoe()
        {
            if (_applied)
                return;

            _applied = true;

            if (_damageSource == null && _explosive != null && _explosive.overrideDamageSender != null)
                _damageSource = _explosive.overrideDamageSender.gameObject;

            float rolled = Random.Range(minDamage, maxDamage);
            Vector3 origin = transform.position;
            float radius = Mathf.Max(0.1f, explosionRadius);

            Collider[] hits = Physics.OverlapSphere(origin, radius, hitLayers, QueryTriggerInteraction.Ignore);
            var damagedEnemies = new HashSet<EnemyHealth>();
            var damagedCompanions = new HashSet<CompanionHealth>();

            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i];
                if (col == null)
                    continue;

                float distance = Vector3.Distance(origin, GetClosestPointSafe(col, origin));
                float damage = ScaleDamageByDistance(rolled, distance);
                if (damage <= 0f)
                    continue;

                EnemyHealth enemy = col.GetComponentInParent<EnemyHealth>();
                if (enemy != null)
                {
                    if (damagedEnemies.Add(enemy) && !enemy.IsDead)
                    {
                        enemy.TakeDamage(damage, _damageSource, isCritical: false);
                        SpawnHitFeedback(col, damage);
                    }

                    continue;
                }

                if (damageCompanions)
                {
                    CompanionHealth companion = col.GetComponentInParent<CompanionHealth>();
                    if (companion != null && damagedCompanions.Add(companion) && !companion.IsDead)
                    {
                        ((IDamageable)companion).TakeDamage(damage, _damageSource, false);
                        continue;
                    }
                }

                if (damagePlayer)
                {
                    // Optional self/friendly splash — off by default for expedition grenades.
                    IDamageable damageable = DamageableUtility.GetDamageable(col);
                    if (damageable != null && damageable is not EnemyHealth)
                        damageable.TakeDamage(damage, _damageSource, false);
                }
            }
        }

        private float ScaleDamageByDistance(float baseDamage, float distance)
        {
            if (distance <= innerRadiusFullDamage)
                return baseDamage;

            if (distance >= explosionRadius)
                return baseDamage * edgeDamageFalloff;

            float t = 1f - Mathf.InverseLerp(innerRadiusFullDamage, explosionRadius, distance);
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

        private static void SpawnHitFeedback(Collider col, float damage)
        {
            Vector3 point = col.bounds.center;
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
