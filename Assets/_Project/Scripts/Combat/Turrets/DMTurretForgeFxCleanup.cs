using FORGE3D;
using UnityEngine;

namespace Project.Combat.Turrets
{
    /// <summary>Stops FORGE3D timers, despawned beams, loop audio, and stamped projectiles owned by a turret.</summary>
    public static class DMTurretForgeFxCleanup
    {
        public static void StopAll(DMTurretRuntime owner, F3DFXController fxController)
        {
            StopSustained(owner, fxController);
            if (owner != null)
                DespawnOwnedShots(owner);
        }

        /// <summary>Beams/flames/lightning + loop audio only — safe while idle (no projectile despawn).</summary>
        public static void StopSustained(DMTurretRuntime owner, F3DFXController fxController)
        {
            if (fxController != null && fxController.isActiveAndEnabled)
                fxController.Stop();

            if (owner == null)
                return;

            Transform[] sockets = fxController != null ? fxController.TurretSocket : null;
            DespawnContinuousUnderSockets(sockets);
            StopForgeLoopAudio(owner.transform, sockets);
        }

        private const float PooledAudioCleanupRadius = 96f;
        private const float PooledLightScanIntervalSeconds = 2f;
        private static float nextPooledLightScanTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            nextPooledLightScanTime = 0f;
            processedLights.Clear();
            lastPoolSignature = -1;
            lastPoolId = 0;
        }

        /// <summary>
        /// FORGE3D example muzzles/beams/flames ship shadow-casting point lights (6 HDRP shadow requests
        /// each). Strips shadows on the GeneratedPool's scene instances (never the prefab assets), throttled.
        /// </summary>
        // 2026-10-20j: was a full pool GetComponentsInChildren every 2 s (frame hitch -> 2 s player jerk). Now: each light
        // once (cached by id), rescan only when the pool's hierarchy signature changes; no per-call allocs.
        private static readonly System.Collections.Generic.HashSet<int> processedLights = new System.Collections.Generic.HashSet<int>();
        private static readonly System.Collections.Generic.List<Light> lightBuffer = new System.Collections.Generic.List<Light>(256);
        private static int lastPoolSignature = -1;
        private static int lastPoolId;

        private static int PoolSignature(Transform root)
        {
            int sig = root.childCount;
            for (int i = 0; i < root.childCount; i++)
                sig = sig * 31 + root.GetChild(i).childCount;
            return sig;
        }

        public static void SuppressPooledLightShadows()
        {
            DM_CombatCoreProfile cp = DM_CombatCoreProfile.Live;
            if (cp == null || !cp.turretPooledLightRescanLegacy)
            {
                if (!TryGetPool(out F3DPool p) || p == null)
                    return;
                Transform root = p.transform;
                int id = root.GetHashCode();
                int sig = PoolSignature(root);
                if (id == lastPoolId && sig == lastPoolSignature)
                    return; // cheap early-out: nothing new spawned into the pool
                lastPoolId = id;
                lastPoolSignature = sig;
                lightBuffer.Clear();
                root.GetComponentsInChildren(true, lightBuffer);
                for (int i = 0; i < lightBuffer.Count; i++)
                {
                    Light l = lightBuffer[i];
                    if (l == null || !processedLights.Add(l.GetHashCode()))
                        continue;
                    if (l.shadows != LightShadows.None && l.gameObject.scene.IsValid())
                        l.shadows = LightShadows.None;
                }
                lightBuffer.Clear();
                return;
            }

            float now = Time.unscaledTime;
            if (now < nextPooledLightScanTime)
                return;
            nextPooledLightScanTime = now + PooledLightScanIntervalSeconds;

            if (!TryGetPool(out F3DPool pool))
                return;

            TracerVfxCache.SuppressLightShadows(pool.GetComponentsInChildren<Light>(true));
        }

        private static void DespawnContinuousUnderSockets(Transform[] sockets)
        {
            if (sockets == null || sockets.Length == 0)
                return;
            if (!TryGetPool(out F3DPool pool))
                return;

            for (int s = 0; s < sockets.Length; s++)
            {
                Transform socket = sockets[s];
                if (socket == null)
                    continue;

                for (int i = socket.childCount - 1; i >= 0; i--)
                {
                    Transform child = socket.GetChild(i);
                    if (child == null)
                        continue;
                    if (child.GetComponent<F3DBeam>() == null
                        && child.GetComponent<F3DLightning>() == null
                        && child.GetComponent<F3DFlameThrower>() == null)
                        continue;

                    pool.Despawn(child);
                }
            }
        }

        private static void StopForgeLoopAudio(Transform ownerRoot, Transform[] sockets)
        {
            if (ownerRoot == null || !TryGetPool(out F3DPool pool))
                return;

            bool stoppedAny = StopAndDespawnLoopAudioOnHierarchy(ownerRoot, ownerRoot, pool);

            // LightningGunLoop parents loop audio to loopParent.parent (often above the turret root).
            Transform parent = ownerRoot.parent;
            if (parent != null)
                stoppedAny |= StopAndDespawnLoopAudioOnHierarchy(parent, ownerRoot, pool);

            // Scene-wide scan is expensive — only when local hierarchy had loop audio or sockets still have beams.
            if (stoppedAny || HasContinuousFxUnderSockets(sockets))
                DespawnDetachedLoopAudioNear(ownerRoot.position, ownerRoot, pool);
        }

        private static bool HasContinuousFxUnderSockets(Transform[] sockets)
        {
            if (sockets == null || sockets.Length == 0)
                return false;

            for (int s = 0; s < sockets.Length; s++)
            {
                Transform socket = sockets[s];
                if (socket == null)
                    continue;

                for (int i = 0; i < socket.childCount; i++)
                {
                    Transform child = socket.GetChild(i);
                    if (child == null)
                        continue;
                    if (child.GetComponent<F3DBeam>() != null
                        || child.GetComponent<F3DLightning>() != null
                        || child.GetComponent<F3DFlameThrower>() != null)
                        return true;
                }
            }

            return false;
        }

        private static bool StopAndDespawnLoopAudioOnHierarchy(Transform searchRoot, Transform ownerRoot, F3DPool pool)
        {
            bool stoppedAny = false;
            AudioSource[] sources = searchRoot.GetComponentsInChildren<AudioSource>(true);
            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = sources[i];
                if (source == null || !source.loop)
                    continue;

                if (!IsUnderOwnerOrForgePooled(source.transform, ownerRoot))
                    continue;

                source.Stop();
                pool.Despawn(source.transform);
                stoppedAny = true;
            }

            return stoppedAny;
        }

        private static void DespawnDetachedLoopAudioNear(Vector3 origin, Transform ownerRoot, F3DPool pool)
        {
            AudioSource[] sources = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude);
            float radiusSqr = PooledAudioCleanupRadius * PooledAudioCleanupRadius;
            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = sources[i];
                if (source == null || !source.loop)
                    continue;

                Transform t = source.transform;
                if (t.IsChildOf(ownerRoot))
                    continue;

                if ((t.position - origin).sqrMagnitude > radiusSqr)
                    continue;

                if (!IsLikelyForgePooledAudio(t))
                    continue;

                source.Stop();
                pool.Despawn(t);
            }
        }

        private static bool IsUnderOwnerOrForgePooled(Transform t, Transform ownerRoot)
        {
            if (t == null)
                return false;
            if (t.IsChildOf(ownerRoot))
                return true;
            return IsLikelyForgePooledAudio(t);
        }

        private static bool IsLikelyForgePooledAudio(Transform t)
        {
            if (t == null)
                return false;

            string name = t.name;
            return name.StartsWith("AudioSource")
                || name.IndexOf("Audio", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void DespawnOwnedShots(DMTurretRuntime owner)
        {
            if (!TryGetPool(out F3DPool pool))
                return;

            F3DProjectile[] projectiles = Object.FindObjectsByType<F3DProjectile>(FindObjectsInactive.Exclude);
            for (int i = 0; i < projectiles.Length; i++)
            {
                F3DProjectile projectile = projectiles[i];
                if (projectile == null)
                    continue;
                DMTurretRuntime shotOwner = DMTurretWeaponContext.Resolve(projectile);
                if (shotOwner != owner)
                    continue;
                pool.Despawn(projectile.transform);
            }

            F3DMissile[] missiles = Object.FindObjectsByType<F3DMissile>(FindObjectsInactive.Exclude);
            for (int i = 0; i < missiles.Length; i++)
            {
                F3DMissile missile = missiles[i];
                if (missile == null)
                    continue;
                DMTurretRuntime shotOwner = DMTurretWeaponContext.Resolve(missile);
                if (shotOwner != owner)
                    continue;
                pool.Despawn(missile.transform);
            }
        }

        private static bool TryGetPool(out F3DPool pool)
        {
            pool = null;
            if (F3DPoolManager.Pools == null || !F3DPoolManager.Pools.ContainsKey("GeneratedPool"))
                return false;
            pool = F3DPoolManager.Pools["GeneratedPool"];
            return pool != null;
        }
    }
}
