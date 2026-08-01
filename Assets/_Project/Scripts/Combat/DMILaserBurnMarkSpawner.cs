using System.Collections;
using Project.Core;
using Project.Data;
using Project.Interaction;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Spawns pooled multi-layer laser burn marks for mining beams and pulse laser ammo.
    /// Mining stamps intentionally overlap into one continuous scorch trail.
    /// Marks stamped on harvestable resources are parented + tracked so deplete cleans them up.
    /// </summary>
    public static class DMILaserBurnMarkSpawner
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/Combat/VFX/Laser_Burn_Mark.prefab";

        // Dense overlap vs scorch diameter ~0.95–1.05 (root scale 1). Slight gaps only.
        private const float MiningStampMinDistance = 0.018f;
        private const float MiningStampCooldown = 0.012f;
        private const float PoolReleaseDelay = 5.5f;

        // Ping-pong twist around surface normal (degrees). Consecutive stamps alternate orientation.
        private const float PingPongAngleDegrees = 32f;
        private const float TwistJitterDegrees = 6f;
        private const float ScaleJitter = 0.08f;
        private const float IntensityJitter = 0.12f;

        private static GameObject _prefab;
        private static Vector3 _lastMiningStamp;
        private static float _nextMiningStampTime;
        private static int _miningStampIndex;
        private static CoroutineRunner _runner;

        private sealed class CoroutineRunner : MonoBehaviour
        {
        }

        public static bool ShouldSpawnForLaserAmmo(ItemData ammoItem, ItemData weapon)
        {
            if (weapon != null && weapon.isMiningTool)
                return true;

            ItemData ammo = ammoItem;
            if (ammo == null && weapon != null)
                ammo = weapon.defaultAmmoItem;

            if (ammo == null)
                return false;

            if (ammo.isHitscanBeam || ammo.isContinuousLaser)
                return true;

            return ammo.ammoType == AmmoType.Laser;
        }

        public static void Spawn(Vector3 point, Vector3 normal)
        {
            SpawnInternal(point, normal, NextPingPongTwist(), NextScaleMul(), NextIntensityMul(), attachTo: null);
        }

        /// <summary>
        /// Continuous mining beam: stamp a burn when the impact moves or cooldown elapses.
        /// When <paramref name="attachTo"/> is a harvestable resource (or a ResourceNode is hit),
        /// the mark is parented and registered so node deplete clears orphans.
        /// </summary>
        public static void TryStampMining(Vector3 point, Vector3 beamDirection, Transform attachTo = null)
        {
            if (Time.time < _nextMiningStampTime)
                return;

            if ((_lastMiningStamp - point).sqrMagnitude < MiningStampMinDistance * MiningStampMinDistance
                && _nextMiningStampTime > 0f)
                return;

            Vector3 normal = beamDirection.sqrMagnitude > 0.0001f
                ? -beamDirection.normalized
                : Vector3.up;

            Transform resolvedAttach = attachTo;

            // Prefer a real surface normal if available.
            if (Physics.Raycast(
                    point - beamDirection.normalized * 0.05f,
                    beamDirection.normalized,
                    out RaycastHit hit,
                    0.2f,
                    ~0,
                    QueryTriggerInteraction.Ignore))
            {
                point = hit.point;
                normal = hit.normal;

                if (resolvedAttach == null && hit.collider != null)
                {
                    ResourceNode node = hit.collider.GetComponentInParent<ResourceNode>();
                    if (node != null)
                        resolvedAttach = node.transform;
                }
            }

            SpawnInternal(point, normal, NextPingPongTwist(), NextScaleMul(), NextIntensityMul(), resolvedAttach);
            _lastMiningStamp = point;
            _nextMiningStampTime = Time.time + MiningStampCooldown;
            _miningStampIndex++;
        }

        public static void ResetMiningStampState()
        {
            _lastMiningStamp = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            _nextMiningStampTime = 0f;
            _miningStampIndex = 0;
        }

        private static void SpawnInternal(
            Vector3 point,
            Vector3 normal,
            float twistDegrees,
            float scaleMul,
            float intensityMul,
            Transform attachTo)
        {
            GameObject prefab = ResolvePrefab();
            if (prefab == null)
                return;

            Vector3 n = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;
            Vector3 tangent = Vector3.Cross(n, Vector3.up);
            if (tangent.sqrMagnitude < 0.001f)
                tangent = Vector3.Cross(n, Vector3.right);
            tangent.Normalize();
            Quaternion rotation = Quaternion.AngleAxis(twistDegrees, n) * Quaternion.LookRotation(-n, tangent);
            Vector3 spawnPos = point + n * 0.014f;

            // Parent to harvestable resources only — terrain/world burns stay under the pool root.
            Transform parent = null;
            DMILaserBurnMarkHost host = null;
            if (attachTo != null && attachTo.GetComponentInParent<ResourceNode>() != null)
            {
                parent = attachTo;
                host = DMILaserBurnMarkHost.GetOrCreate(attachTo);
            }

            GameObject instance = PoolManager.Spawn(prefab, spawnPos, rotation, parent);
            if (instance == null)
                return;

            // Keep world pose after parenting so surface offset / normal orientation stay correct
            // even if the resource root is rotated or slightly scaled.
            instance.transform.SetPositionAndRotation(spawnPos, rotation);

            DMILaserBurnMark mark = instance.GetComponent<DMILaserBurnMark>();
            if (mark != null)
            {
                mark.Play(point, n, twistDegrees, scaleMul, intensityMul);
                if (host != null)
                    host.Register(mark);

                ScheduleLeaseRelease(mark, PoolReleaseDelay);
            }
            else
            {
                PoolManager.ReleaseDelayed(instance, PoolReleaseDelay);
            }
        }

        private static void ScheduleLeaseRelease(DMILaserBurnMark mark, float delay)
        {
            if (mark == null)
                return;

            EnsureRunner();
            int lease = mark.LeaseId;
            _runner.StartCoroutine(ReleaseAfterDelayIfLease(mark, lease, delay));
        }

        private static IEnumerator ReleaseAfterDelayIfLease(DMILaserBurnMark mark, int lease, float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            if (mark == null)
                yield break;

            if (mark.LeaseId != lease)
                yield break;

            DMILaserBurnMarkHost host = mark.GetComponentInParent<DMILaserBurnMarkHost>();
            if (host != null)
                host.Unregister(mark);

            PoolManager.Release(mark.gameObject);
        }

        private static void EnsureRunner()
        {
            if (_runner != null)
                return;

            GameObject runnerObject = new GameObject("DMILaserBurnMarkSpawnerRunner");
            Object.DontDestroyOnLoad(runnerObject);
            _runner = runnerObject.AddComponent<CoroutineRunner>();
        }

        /// <summary>
        /// Smooth ping-pong: … −A → 0 → +A → 0 → −A … so consecutive stamps feel organic, not random.
        /// </summary>
        private static float NextPingPongTwist()
        {
            float oscillating = Mathf.PingPong(_miningStampIndex * (PingPongAngleDegrees * 0.5f), PingPongAngleDegrees * 2f)
                                - PingPongAngleDegrees;
            float jitter = Random.Range(-TwistJitterDegrees, TwistJitterDegrees);
            return oscillating + jitter;
        }

        private static float NextScaleMul()
        {
            return 1f + Random.Range(-ScaleJitter, ScaleJitter);
        }

        private static float NextIntensityMul()
        {
            return 1f + Random.Range(-IntensityJitter, IntensityJitter);
        }

        private static GameObject ResolvePrefab()
        {
            if (_prefab != null)
                return _prefab;

#if UNITY_EDITOR
            _prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
#endif
            if (_prefab == null)
                _prefab = Resources.Load<GameObject>("Combat/VFX/Laser_Burn_Mark");

            return _prefab;
        }

#if UNITY_EDITOR
        public static void SetPrefabForEditor(GameObject prefab)
        {
            _prefab = prefab;
        }
#endif
    }
}
