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
