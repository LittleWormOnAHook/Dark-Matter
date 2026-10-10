using Project.Data;
using UnityEngine;

namespace Project.Combat.Experiment.Carve
{
    /// <summary>
    /// Lab-only ammo profile for surface carve experiments (MT / mesh-hit roadmap).
    /// Isolated flags — does not change behavior of standard <see cref="DMAmmoFxProfile"/> assets.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Dark Matter/Combat/Experiment/Carve Experiment Ammo Profile",
        fileName = "DMAmmoFxProfile_CarveExperiment")]
    public sealed class DMCarveExperimentAmmoProfile : DMAmmoFxProfile
    {
        [Header("Experiment (Carve Lab)")]
        [Tooltip("Projectile sphere casts and hitscan aim rays use all physics layers except Player (layer 8).")]
        public bool useAllPhysicsLayers = true;

        [Tooltip("When global DMCarveSettings.ammoDeformsMeshes is off, this ammo still deforms carvable meshes.")]
        public bool forceMeshDeformation = true;

        [Tooltip("Allow DMCarveImpacts on enemy/creature receivers (carve only — no enemy blood decals).")]
        public bool allowCarveOnEnemyReceivers = true;

        [Tooltip("Allow world impact carve/mark path on characters that normally skip PlayWorldImpact.")]
        public bool bypassWorldImpactCharacterBlocks = true;
    }
}
