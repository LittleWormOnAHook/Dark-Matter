using System;
using UnityEngine;

namespace Project.SurfaceCarve
{
    [Serializable]
    public class DMCarveMaterialRow
    {
        [Tooltip("Rock (or other) material on the carved object.")]
        public Material source;
        [Tooltip("Material for the inside of the cut. Empty = auto (tinted copy of the source).")]
        public Material fracture;
        [Tooltip("Material for the dark jagged rim band. Empty = auto (dark copy of the source).")]
        public Material rim;
    }

    /// <summary>
    /// Global surface carve tunables (Genesis Studio > Combat > Surface Damage).
    /// Lives at Resources/Carve/DMCarveSettings.
    /// </summary>
    [CreateAssetMenu(menuName = "Dark Matter/Combat/Surface Carve Settings", fileName = "DMCarveSettings")]
    public class DMCarveSettings : ScriptableObject
    {
        public const string ResourcesPath = "Carve/DMCarveSettings";
        public const string AssetPath = "Assets/_Project/Resources/Carve/DMCarveSettings.asset";

        [Header("Ammo Carving (Play Mode)")]
        [Tooltip("Master switch: ammo impacts carve DMCarvable objects using each ammo's Surface Damage.")]
        public bool enableAmmoCarving = true;
        [Tooltip("Global multiplier on every ammo carve radius.")]
        [Range(0.1f, 3f)] public float ammoRadiusScale = 1f;
        [Tooltip("Hit objects with one of these Unity tags get a DMCarvable added on first hit.")]
        public string[] autoCarvableTags = new string[0];
        [Tooltip("Hit objects whose name contains one of these words (case-insensitive) get a DMCarvable added on first hit. Clear the list to only carve objects marked carvable.")]
        public string[] autoCarvableNameContains = { "rock", "boulder", "cliff", "stone", "outcrop" };
        [Tooltip("2026-10-20x: carving a statically batched object detaches it at runtime (unbatched readable copy of its original mesh). Scene static flags are not changed.")]
        public bool carveUnbatchesStatic = true;
        [Tooltip("Stop carving an object once it reaches this many triangles.")]
        public int maxTrianglesPerObject = 300000;
        [Tooltip("Ammo impacts dig craters into meshes. Off = meshes stay intact (no runtime mesh copy, no collider re-cook); shots leave molten marks (Resources/Combat/DMMoltenImpactMap) and, on carvable rocks, debris only. The Carve Tool is not affected.")]
        public bool ammoDeformsMeshes = false;
        [Tooltip("When ammo does not deform meshes: still throw debris chunks from carvable rocks.")]
        public bool debrisWithoutDeform = true;

        [Header("Cutter Detail")]
        [Tooltip("Icosphere subdivisions for normal cutters (3 = 1280 triangles).")]
        [Range(1, 4)] public int cutterSubdivisions = 3;
        [Tooltip("Subdivisions for small cutters (bullets).")]
        [Range(1, 4)] public int smallCutterSubdivisions = 2;
        [Tooltip("Cutters at or below this radius (m) use the small subdivision level.")]
        public float smallCutterRadius = 0.2f;
        [Tooltip("Surface triangles under the cutter are split until their longest edge is below radius x this, so even big flat faces get a clean cut line.")]
        [Range(0.1f, 1f)] public float surfaceSplitEdge = 0.3f;

        [Header("Cut Face Look")]
        [Tooltip("Tint on the auto fracture material (a copy of the rock's own material).")]
        public Color fractureTint = new Color(0.62f, 0.58f, 0.55f, 1f);
        [Tooltip("Tint on the auto rim material - the dark outline where the cut meets the surface.")]
        public Color rimTint = new Color(0.16f, 0.145f, 0.135f, 1f);
        [Tooltip("Width of the dark rim band, in cutter-radius units.")]
        [Range(0f, 0.6f)] public float rimWidth = 0.14f;
        [Tooltip("How ragged the rim band is. 0 = even band, 1 = very jagged.")]
        [Range(0f, 1f)] public float rimJitter = 0.65f;
        [Tooltip("World meters per texture tile on cut faces (box-projected UVs).")]
        [Min(0.05f)] public float cutUvTileMeters = 1.5f;
        [Tooltip("Explicit cut materials per source material. Unlisted materials get auto copies.")]
        public DMCarveMaterialRow[] materialOverrides = new DMCarveMaterialRow[0];

        [Header("Debris (Play Mode)")]
        public bool spawnDebris = true;
        [Min(0.1f)] public float debrisLifetime = 6f;
        [Min(0f)] public float debrisImpulse = 2.5f;
        [Min(0)] public int maxLiveDebris = 40;
        [Tooltip("Global multiplier on debris chunk size, on top of each recipe's Debris Scale.")]
        [Range(0.1f, 10f)] public float debrisSizeScale = 3f;

        [Header("Impact VFX (2026-10-20v)")]
        [Tooltip("Explosive impact ground ring / glow circle (horizontal billboard ring in the explosion VFX). Off = hidden at spawn (assets untouched).")]
        public bool explosionRing = false;
        [Tooltip("Impact VFX particles using non-HDRP shaders (render pink) get runtime HDRP materials (one cached copy per source material; assets untouched).")]
        public bool impactVfxHdrpFix = true;
        [Tooltip("Debris-like impact particles (mesh particles, or names with debris/chunk/rock/dirt/stone/dots) become HDRP Lit tinted with the hit surface color (Debris Look settings).")]
        public bool impactDebrisSurfaceColor = true;

        public enum DebrisColorSource { SurfaceSample, MaterialAverage, FractureMaterial }
        [Header("Debris Look (2026-10-20u, MaterialPropertyBlock - no material/prefab changes)")]
        [Tooltip("Tint debris chunks from the surface that was hit, with a rough overlay.")]
        public bool debrisLookEnabled = true;
        [Tooltip("SurfaceSample: base map at the hit UV when readable / terrain layer mix at the hit point, else material average. MaterialAverage: averaged material color. FractureMaterial: keep the cut material color (overlay still applies).")]
        public DebrisColorSource debrisColorSource = DebrisColorSource.SurfaceSample;
        [Tooltip("0 = fracture material color, 1 = fully the sampled surface color.")]
        [Range(0f, 1f)] public float debrisTintStrength = 0.85f;
        [Tooltip("Rough grayscale overlay (base map). Empty = generated tileable noise.")]
        public Texture2D debrisOverlayTexture;
        [Range(0f, 1f)] public float debrisOverlayStrength = 0.55f;
        [Tooltip("Overlay tiles per UV unit (debris UVs are box-projected at Cut UV Tile Meters).")]
        [Range(0.1f, 16f)] public float debrisOverlayTiling = 2f;
        [Range(0f, 3f)] public float debrisBrightness = 1f;
        [Range(0f, 2f)] public float debrisSaturation = 0.9f;
        [Tooltip("Random per-chunk variance (value / slight hue).")]
        [Range(0f, 0.5f)] public float debrisColorVariance = 0.08f;

        [Header("Tools")]
        [Tooltip("Drag spacing between stamps, in cutter-radius units.")]
        [Range(0.05f, 2f)] public float dragStampSpacing = 0.35f;
        [Tooltip("Seconds between MeshCollider rebuilds on an object being carved in Play Mode.")]
        [Range(0f, 1f)] public float runtimeColliderRefresh = 0.12f;
        [Tooltip("Play Mode: a carved object whose MeshCollider uses its own (simplified) mesh keeps that collider instead of switching to the full-detail carved render mesh. Colliders that already share the render mesh still update.")]
        public bool keepDedicatedColliderInPlay = true;

        [NonSerialized] private static DMCarveSettings cached;

        public static DMCarveSettings Active
        {
            get
            {
                if (cached == null)
                {
                    cached = Resources.Load<DMCarveSettings>(ResourcesPath);
                    if (cached == null)
                    {
                        cached = CreateInstance<DMCarveSettings>();
                        cached.hideFlags = HideFlags.DontSave;
                    }
                }

                return cached;
            }
        }

        public bool TryGetMaterialRow(Material source, out DMCarveMaterialRow row)
        {
            row = null;
            if (source == null || materialOverrides == null)
                return false;
            for (int i = 0; i < materialOverrides.Length; i++)
            {
                DMCarveMaterialRow r = materialOverrides[i];
                if (r != null && r.source == source)
                {
                    row = r;
                    return true;
                }
            }

            return false;
        }

        public bool MatchesAutoCarvable(GameObject go)
        {
            if (go == null)
                return false;

            if (autoCarvableTags != null && autoCarvableTags.Length > 0)
            {
                // Plain string compare: CompareTag logs "Tag: X is not defined" for tags missing from the Tag Manager.
                // 2026-10-20v: case-insensitive, and the tag may sit on any parent (collider child of a tagged prop).
                for (Transform tr = go.transform; tr != null; tr = tr.parent)
                {
                    string goTag = tr.gameObject.tag;
                    for (int i = 0; i < autoCarvableTags.Length; i++)
                    {
                        string t = autoCarvableTags[i];
                        if (!string.IsNullOrEmpty(t) && string.Equals(goTag, t.Trim(), StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
            }

            if (autoCarvableNameContains != null)
            {
                string n = go.name;
                for (int i = 0; i < autoCarvableNameContains.Length; i++)
                {
                    string w = autoCarvableNameContains[i];
                    if (!string.IsNullOrEmpty(w) && n.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }

            return false;
        }
    }
}
