using Project.Combat;
using Project.Data;
using UnityEngine;

namespace Project.SurfaceCarve
{
    /// <summary>Ammo impact -> surface carve. Called from DMCombatFx.PlayWorldImpact (projectiles + hitscan).</summary>
    public static class DMCarveImpacts
    {
        public static bool TryCarveFromAmmo(ItemData ammoItem, DMAmmoFxProfile profile, Vector3 point, Vector3 normal, GameObject receiver)
            => TryCarveFromAmmo(ammoItem, profile, point, normal, receiver, out _);

        /// <summary>Carve, then report where the surface now is under the hit (crater floor) so hit marks land on it.</summary>
        public static bool TryCarveFromAmmo(ItemData ammoItem, DMAmmoFxProfile profile, Vector3 point, Vector3 normal, GameObject receiver, out Vector3 markPoint)
        {
            markPoint = point;
            if (!Application.isPlaying || receiver == null)
                return false;

            DMCarveSettings settings = DMCarveSettings.Active;
            if (settings == null || !settings.enableAmmoCarving)
                return false;

            DMSurfaceDamageSettings damage;
            if (profile != null)
            {
                if (!profile.TryResolveSurfaceDamage(receiver.tag, out damage))
                    return false;
            }
            else if (ammoItem != null)
            {
                damage = DMSurfaceDamagePresets.For(ammoItem.ammoType, ammoItem.itemName);
            }
            else
            {
                return false;
            }

            if (damage == null || damage.radius <= 0f)
                return false;

            if (!settings.ammoDeformsMeshes)
            {
                // Meshes stay intact: debris only, point unchanged so hit marks stay on the surface.
                if (settings.spawnDebris && settings.debrisWithoutDeform)
                    SpawnDebrisOnly(receiver, point, normal, damage, settings);
                return false;
            }

            DMCarvable carvable = DMCarvable.ResolveForImpact(receiver, settings);
            if (carvable == null)
                return false;

            int seed = Random.Range(1, int.MaxValue);
            if (!carvable.CarveAtSurface(point, normal, damage, seed, settings.spawnDebris, settings.ammoRadiusScale))
                return false;

            markPoint = FindNewSurface(carvable, point, normal, Mathf.Max(0.01f, damage.radius * settings.ammoRadiusScale));
            return true;
        }

        private static void SpawnDebrisOnly(GameObject receiver, Vector3 point, Vector3 normal, DMSurfaceDamageSettings damage, DMCarveSettings settings)
        {
            if (damage.debrisCount <= 0)
                return;

            DMCarvable carvable = receiver.GetComponentInParent<DMCarvable>();
            if (carvable != null ? !carvable.allowDebris : !settings.MatchesAutoCarvable(receiver))
                return;

            UnityEngine.Renderer r = carvable != null ? carvable.GetComponent<UnityEngine.Renderer>() : receiver.GetComponent<UnityEngine.Renderer>();
            Material source = r != null ? r.sharedMaterial : null;
            Material mat = carvable != null ? carvable.ResolveFractureMaterial(source) : DMCarvable.FractureMaterialFor(source);
            float radius = Mathf.Max(0.01f, damage.radius * settings.ammoRadiusScale);
            DMCarveCutter cutter = DMCarvable.MakeSurfaceCutter(point, normal, radius, damage.depth, damage.style, damage.roughness, Random.Range(1, int.MaxValue));
            DMCarveDebris.Spawn(cutter, normal, damage, mat, receiver.layer);
        }

        private static Vector3 FindNewSurface(DMCarvable carvable, Vector3 point, Vector3 normal, float radius)
        {
            Vector3 n = normal.sqrMagnitude > 1e-8f ? normal.normalized : Vector3.up;
            carvable.FlushCollider();
            Collider col = carvable.GetComponent<Collider>();
            if (col != null && col.enabled && col.Raycast(new Ray(point + n * (radius * 0.5f), -n), out RaycastHit hit, radius * 4f))
                return hit.point;
            return point;
        }
    }
}
