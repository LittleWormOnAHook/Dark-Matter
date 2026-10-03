using System;
using UnityEngine;

namespace GenesisPCG.RockCreation
{
    /// <summary>
    /// Shared settings for DmRockCombiner. The size / scale / tilt / flatten fields mirror the original noise-rock recipe
    /// (same names, ranges and defaults, initialised from DM_ProcRecipe_IoBasaltRock) so combined rocks
    /// come out the same size as the noise rocks. Noise, strata and UV-projection fields were dropped
    /// because real kit meshes keep their own shape and UVs.
    /// </summary>
    [CreateAssetMenu(fileName = "DM_RockCombineRecipe", menuName = "Genesis PCG Rock Creation/Rock Combine Recipe")]
    public sealed class DmRockCombineRecipe : ScriptableObject
    {
        public static event Action<DmRockCombineRecipe> Changed;

        [Header("Source")]
        [Tooltip("Default kit for DM PCG Creators that do not set their own.")]
        public DmRockKit kit;
        [Tooltip("Adds a MeshCollider using the combined mesh (convex off).")]
        public bool addMeshCollider = true;

        [Header("Size (meters, full extents)")]
        public Vector3 sizeMin = new Vector3(1.6f, 1.1f, 1.6f);
        public Vector3 sizeMax = new Vector3(3.6f, 2.6f, 3.2f);
        [Range(0f, 1f), Tooltip("0 = each axis rolls its own size (stretched shapes), 1 = all axes share one roll.")]
        public float uniformScaleBias = 0.35f;
        [Range(0f, 45f), Tooltip("Random tilt of the whole rock so shapes don't all line up (yaw is always random).")]
        public float tiltJitter = 12f;
        [Range(1f, 3f), Tooltip("Real meshes distort when squashed. Limits how far one axis may be stretched relative to the others when fitting the rolled size.")]
        public float maxAxisStretch = 1.35f;

        [Header("Base")]
        [Range(0f, 0.9f), Tooltip("How much of the underside gets flattened so the rock sits on the ground.")]
        public float bottomFlatten = 0.22f;

        [Header("Surface snap (create, paste, Shift+drag)")]
        public PcgSurfaceSnapSettings snap = new PcgSurfaceSnapSettings();

        [Header("Bake (atlas, single mesh, LODs)")]
        public DmRockBakeSettings bake = new DmRockBakeSettings();

        [NonSerialized] public int Version;

        private void OnValidate()
        {
            sizeMax = Vector3.Max(sizeMax, sizeMin);
            Version++;
            Changed?.Invoke(this);
        }
    }
}
