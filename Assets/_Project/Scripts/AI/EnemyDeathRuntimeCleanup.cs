using Project.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.AI
{
    /// <summary>
    /// Sweeps orphaned enemy death / loot artifacts left unparented (or as empty dead clones)
    /// after body dissolve and loot-bag dissolve. Ensures zero FRED(Clone) / dissolve / weapon leftovers.
    /// </summary>
    public static class EnemyDeathRuntimeCleanup
    {
        private static int s_lastDeferredSweepFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterQuitting()
        {
            Application.quitting -= HandleQuitting;
            Application.quitting += HandleQuitting;
            s_lastDeferredSweepFrame = -1;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void SweepAfterSceneLoad()
        {
            if (!Application.isPlaying)
                return;

            SweepOrphans(destroyImmediately: false);
        }

        private static void HandleQuitting()
        {
            SweepOrphans(destroyImmediately: false);
        }

        /// <summary>Public entry for tooling / post-death / post-loot sweeps.</summary>
        public static void SweepOrphans(bool destroyImmediately)
        {
            // Death / loot / respawn often call this several times in one frame. One deferred pass
            // is enough; immediate (Play Mode exit) always runs.
            if (!destroyImmediately)
            {
                int frame = Time.frameCount;
                if (frame == s_lastDeferredSweepFrame)
                    return;
                s_lastDeferredSweepFrame = frame;
            }

            SweepDeadEnemyShells(destroyImmediately);
            SweepUnparentedDeathArtifacts(destroyImmediately);
            // Do not sweep EnemyLootBag here: empty bags wait a post-loot dissolve delay and
            // destroy themselves. Sweeping them early would delete bags mid-timer.
            SweepDetachedVolumetricSmoke(destroyImmediately);
        }

        /// <summary>
        /// Destroys a specific dead enemy root (and sweeps scene-wide orphans).
        /// Used when the loot bag finishes so FRED(Clone) does not linger for post-loot delay.
        /// </summary>
        public static void DestroyDeadEnemyAndOrphans(GameObject enemyRoot, bool destroyImmediately = false)
        {
            if (enemyRoot != null)
            {
                if (destroyImmediately)
                    Object.DestroyImmediate(enemyRoot);
                else
                    Object.Destroy(enemyRoot);
            }

            SweepOrphans(destroyImmediately);
        }

        private static void SweepDeadEnemyShells(bool destroyImmediately)
        {
            EnemyHealth[] enemies = Object.FindObjectsByType<EnemyHealth>(FindObjectsInactive.Include);
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyHealth health = enemies[i];
                if (health == null || !health.IsDead)
                    continue;

                EnemyLootable lootable = health.GetComponent<EnemyLootable>();
                if (lootable != null && lootable.IsLootPending)
                    continue;

                EnemyDeathSequence sequence = health.GetComponent<EnemyDeathSequence>();
                // Active sequence (pre-loot dissolve / bag lifetime) - leave the shell.
                // Explicit DestroyDeadEnemyAndOrphans handles the moment loot finishes.
                if (sequence != null && !sequence.IsComplete)
                    continue;

                DestroyOrphan(health.gameObject, destroyImmediately);
            }
        }

        /// <summary>
        /// Name-based death leftovers are always scene roots (parent == null). Walk loaded-scene
        /// roots once instead of three FindObjectsByType Transform full hierarchy scans.
        /// </summary>
        private static void SweepUnparentedDeathArtifacts(bool destroyImmediately)
        {
            int sceneCount = SceneManager.sceneCount;
            for (int s = 0; s < sceneCount; s++)
            {
                Scene scene = SceneManager.GetSceneAt(s);
                if (!scene.IsValid() || !scene.isLoaded)
                    continue;

                GameObject[] roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                {
                    GameObject go = roots[i];
                    if (go == null)
                        continue;

                    if (!IsUnparentedDeathArtifactName(go.name))
                        continue;

                    DestroyOrphan(go, destroyImmediately);
                }
            }
        }

        private static bool IsUnparentedDeathArtifactName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            if (name == "EnemyDissolveLiftAnchor")
                return true;

            if (name.StartsWith("Drawn_", System.StringComparison.Ordinal) ||
                name.StartsWith("Holstered_", System.StringComparison.Ordinal))
                return true;

            if (name.EndsWith("_Dissolve", System.StringComparison.Ordinal) ||
                name.EndsWith("_Smoke", System.StringComparison.Ordinal) ||
                name.EndsWith("_DissolveBake", System.StringComparison.Ordinal) ||
                name.EndsWith("_SmokeBake", System.StringComparison.Ordinal))
                return true;

            return name == "BagVisual"
                || name == "BagTie"
                || name == "SmokeCore"
                || name == "SmokeWisps"
                || name == "SmokeHaze"
                || name == "SmokePrefabFX";
        }

        private static void SweepOrphanLootBags(bool destroyImmediately)
        {
            EnemyLootBag[] bags = Object.FindObjectsByType<EnemyLootBag>(FindObjectsInactive.Include);
            for (int i = 0; i < bags.Length; i++)
            {
                EnemyLootBag bag = bags[i];
                if (bag == null)
                    continue;

                if (bag.HasRemainingLoot)
                    continue;

                DestroyOrphan(bag.gameObject, destroyImmediately);
            }
        }

        private static void SweepDetachedVolumetricSmoke(bool destroyImmediately)
        {
            VolumetricSmokeEmitter[] emitters = Object.FindObjectsByType<VolumetricSmokeEmitter>(FindObjectsInactive.Include);
            for (int i = 0; i < emitters.Length; i++)
            {
                VolumetricSmokeEmitter emitter = emitters[i];
                if (emitter == null)
                    continue;

                Transform t = emitter.transform;
                if (t.parent != null)
                    continue;

                DestroyOrphan(emitter.gameObject, destroyImmediately);
            }
        }

        private static void DestroyOrphan(GameObject go, bool destroyImmediately)
        {
            if (go == null)
                return;

            if (destroyImmediately)
                Object.DestroyImmediate(go);
            else
                Object.Destroy(go);
        }
    }
}