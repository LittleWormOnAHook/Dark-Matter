using Invector.vShooter;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Drives a hitscan laser visual glued to a live muzzle.
    /// Supports pooled beam prefabs OR the weapon-mounted muzzle/Laser/laserSight stack
    /// (Sci-Fi Pistol / Survival Rifle / Mining Tool). Visual only — damage is applied at fire time.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(650)]
    public class HitscanBeamMuzzleFollow : MonoBehaviour
    {
        private const string DefaultHitSparksPath = "Assets/_Project/Prefabs/Combat/VFX/SparksLong.prefab";

        private Transform muzzle;
        private Transform laserRoot;
        private Transform laserSightSprite;
        private vLaserSight boundLaserSight;
        private bool restoreLaserSight;
        private float weaponRange = 45f;
        private float disableAt = -1f;
        private LayerMask hitMask = ~0;
        private LineRenderer[] lineRenderers;
        private Camera aimCamera;
        private GameObject hitSparksPrefab;
        private GameObject hitSparksInstance;
        private ParticleSystem hitSparksParticles;
        private bool hitSparksAuthored;
        private bool weaponStackMode;

        public void Configure(Transform muzzleTransform, float range, LayerMask? optionalHitMask = null)
        {
            weaponStackMode = false;
            laserRoot = null;
            laserSightSprite = null;
            boundLaserSight = null;
            restoreLaserSight = false;
            disableAt = -1f;
            muzzle = muzzleTransform;
            weaponRange = Mathf.Max(1f, range);
            hitMask = optionalHitMask ?? ~0;
            lineRenderers = GetComponentsInChildren<LineRenderer>(true);
            aimCamera = Camera.main;
            EnableLines(true);
            SyncBeam();
        }

        /// <summary>
        /// Pulse the authored muzzle/Laser/laserSight stack on the drawn weapon for a short duration.
        /// </summary>
        public void ConfigureWeaponLaserPulse(
            Transform laserTransform,
            Transform muzzleTransform,
            float range,
            float durationSeconds,
            GameObject optionalHitSparksPrefab = null,
            LayerMask? optionalHitMask = null)
        {
            weaponStackMode = true;
            laserRoot = laserTransform;
            muzzle = muzzleTransform != null ? muzzleTransform : laserTransform;
            weaponRange = Mathf.Max(1f, range);
            hitMask = optionalHitMask ?? ~0;
            disableAt = Time.time + Mathf.Max(0.05f, durationSeconds);
            aimCamera = Camera.main;
            hitSparksPrefab = optionalHitSparksPrefab;
            EnsureHitSparksPrefab();

            lineRenderers = laserTransform != null
                ? laserTransform.GetComponentsInChildren<LineRenderer>(true)
                : null;

            boundLaserSight = laserTransform != null ? laserTransform.GetComponent<vLaserSight>() : null;
            if (boundLaserSight != null)
            {
                restoreLaserSight = boundLaserSight.enabled;
                boundLaserSight.enabled = false;
            }

            laserSightSprite = null;
            if (laserTransform != null)
            {
                Transform sight = laserTransform.Find("laserSight");
                if (sight == null)
                {
                    Transform[] children = laserTransform.GetComponentsInChildren<Transform>(true);
                    for (int i = 0; i < children.Length; i++)
                    {
                        if (children[i] != null &&
                            children[i].name.Equals("laserSight", System.StringComparison.OrdinalIgnoreCase))
                        {
                            sight = children[i];
                            break;
                        }
                    }
                }

                laserSightSprite = sight;
            }

            if (laserSightSprite == null && boundLaserSight != null && boundLaserSight.aimSprite != null)
                laserSightSprite = boundLaserSight.aimSprite.transform;

            EnableLines(true);
            if (laserSightSprite != null)
                laserSightSprite.gameObject.SetActive(true);

            SyncBeam();
        }

        private void LateUpdate()
        {
            if (weaponStackMode && disableAt > 0f && Time.time >= disableAt)
            {
                ShutdownWeaponPulse();
                return;
            }

            SyncBeam();
        }

        private void OnDisable()
        {
            if (weaponStackMode)
                ShutdownWeaponPulse();

            muzzle = null;
            laserRoot = null;
            laserSightSprite = null;
            lineRenderers = null;
            aimCamera = null;
            StopHitSparks();
        }

        private void OnDestroy()
        {
            if (hitSparksInstance != null && !hitSparksAuthored)
                Destroy(hitSparksInstance);
        }

        private void ShutdownWeaponPulse()
        {
            EnableLines(false);

            if (laserSightSprite != null)
                laserSightSprite.gameObject.SetActive(false);

            if (boundLaserSight != null)
                boundLaserSight.enabled = restoreLaserSight;

            StopHitSparks();

            weaponStackMode = false;
            disableAt = -1f;
            muzzle = null;
            laserRoot = null;
            laserSightSprite = null;
            boundLaserSight = null;
            lineRenderers = null;
            enabled = false;
        }

        private void EnableLines(bool enabled)
        {
            if (lineRenderers == null)
                return;

            for (int i = 0; i < lineRenderers.Length; i++)
            {
                LineRenderer line = lineRenderers[i];
                if (line == null)
                    continue;

                line.useWorldSpace = true;
                line.positionCount = 2;
                line.enabled = enabled;
            }
        }

        private void SyncBeam()
        {
            if (muzzle == null && laserRoot == null)
                return;

            Transform originTransform = laserRoot != null ? laserRoot : muzzle;
            if (originTransform == null)
                return;

            Vector3 origin = originTransform.position;
            Vector3 direction = ResolveAimDirection(origin);
            Vector3 endPoint = origin + direction * weaponRange;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, weaponRange, hitMask, QueryTriggerInteraction.Ignore))
                endPoint = hit.point;

            if (lineRenderers != null)
            {
                for (int i = 0; i < lineRenderers.Length; i++)
                {
                    LineRenderer line = lineRenderers[i];
                    if (line == null)
                        continue;

                    line.useWorldSpace = true;
                    line.positionCount = 2;
                    line.SetPosition(0, origin);
                    line.SetPosition(1, endPoint);
                    line.enabled = true;
                }
            }

            if (laserSightSprite != null)
            {
                laserSightSprite.gameObject.SetActive(true);
                laserSightSprite.position = endPoint;
            }

            if (weaponStackMode)
                UpdateHitSparks(endPoint, endPoint - origin);

            // Only relocate pooled beam prefab hosts — never move the weapon Laser transform.
            if (!weaponStackMode)
            {
                transform.position = origin;
                if (direction.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }

        private Vector3 ResolveAimDirection(Vector3 origin)
        {
            if (aimCamera == null)
                aimCamera = Camera.main;

            if (aimCamera != null)
            {
                return RangedFireSolver.ResolveMuzzleToReticleDirection(
                    aimCamera,
                    origin,
                    weaponRange,
                    out _,
                    hitMask);
            }

            if (originTransformForward(out Vector3 fwd))
                return fwd;

            return transform.forward;
        }

        private bool originTransformForward(out Vector3 forward)
        {
            Transform t = laserRoot != null ? laserRoot : muzzle;
            if (t != null && t.forward.sqrMagnitude > 0.0001f)
            {
                forward = t.forward.normalized;
                return true;
            }

            forward = default;
            return false;
        }

        private void EnsureHitSparksPrefab()
        {
            if (hitSparksPrefab != null)
                return;

#if UNITY_EDITOR
            hitSparksPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(DefaultHitSparksPath);
#endif
        }

        private void UpdateHitSparks(Vector3 endPoint, Vector3 beamDelta)
        {
            EnsureHitSparksPrefab();
            if (hitSparksInstance == null && hitSparksPrefab != null)
            {
                hitSparksAuthored = false;
                hitSparksInstance = Instantiate(hitSparksPrefab);
                hitSparksInstance.name = "HitscanHitSparks_SparksLong";
                hitSparksInstance.transform.SetParent(null, true);
                hitSparksInstance.transform.localScale = Vector3.one;

                hitSparksParticles = hitSparksInstance.GetComponent<ParticleSystem>();
                if (hitSparksParticles == null)
                    hitSparksParticles = hitSparksInstance.GetComponentInChildren<ParticleSystem>(true);
            }

            if (hitSparksInstance == null)
                return;

            hitSparksInstance.SetActive(true);
            hitSparksInstance.transform.SetParent(null, true);
            hitSparksInstance.transform.position = endPoint;
            hitSparksInstance.transform.localScale = Vector3.one;
            if (beamDelta.sqrMagnitude > 0.0001f)
                hitSparksInstance.transform.rotation = Quaternion.LookRotation(beamDelta.normalized, Vector3.up);

            if (hitSparksParticles != null && !hitSparksParticles.isPlaying)
                hitSparksParticles.Play(true);
        }

        private void StopHitSparks()
        {
            if (hitSparksParticles != null)
                hitSparksParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (hitSparksInstance != null)
                hitSparksInstance.SetActive(false);
        }

        /// <summary>
        /// Finds muzzle/Laser (with LineRenderer) under a drawn weapon / muzzle transform.
        /// Prefers a muzzle that owns a Laser child.
        /// </summary>
        public static bool TryFindWeaponLaserStack(Transform searchRoot, out Transform laser, out Transform muzzle)
        {
            laser = null;
            muzzle = null;
            if (searchRoot == null)
                return false;

            // Walk up a bit so we can search the full drawn weapon instance.
            Transform root = searchRoot;
            for (int i = 0; i < 8 && root.parent != null; i++)
            {
                string n = root.name;
                if (n.StartsWith("Drawn_", System.StringComparison.OrdinalIgnoreCase) ||
                    n.IndexOf("Handler", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    break;
                root = root.parent;
            }

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            Transform laserWithLine = null;
            Transform muzzleWithLaser = null;

            for (int i = 0; i < children.Length; i++)
            {
                Transform t = children[i];
                if (t == null)
                    continue;

                if (t.name.Equals("Laser", System.StringComparison.OrdinalIgnoreCase) &&
                    t.GetComponent<LineRenderer>() != null)
                {
                    laserWithLine = t;
                    if (t.parent != null &&
                        (t.parent.name.Equals("muzzle", System.StringComparison.OrdinalIgnoreCase) ||
                         t.parent.name.Equals("Muzzle", System.StringComparison.OrdinalIgnoreCase)))
                    {
                        muzzleWithLaser = t.parent;
                        break;
                    }
                }
            }

            if (laserWithLine == null)
                return false;

            laser = laserWithLine;
            muzzle = muzzleWithLaser != null ? muzzleWithLaser : laserWithLine;
            return true;
        }
    }
}
