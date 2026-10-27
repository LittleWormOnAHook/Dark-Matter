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
        [Tooltip("2026-10-24af: Play Mode carves also cut the collider exactly like the visible mesh (overrides Keep Dedicated Collider; box / sphere / capsule colliders on the carved object are swapped for a MeshCollider of the carved mesh). Player, enemies and projectiles pass through carved holes. Costs a collider re-cook per carve.")]
        public bool colliderFollowsCarvedMesh = true;
        [Tooltip("2026-10-24af: Carve Lab experiment ammo leaves no hit mark / laser burn / molten spot (crater + glow only).")]
        public bool carveAmmoSkipsHitMarks = true;

        [Header("Hot Cut Glow (2026-10-24af)")]
        [Tooltip("Cut faces glow like hot metal / rock right after a Play Mode carve, then cool (MaterialPropertyBlock _EmissiveColor; materials untouched).")]
        public bool carveGlowEnabled = true;
        [Tooltip("2026-10-24ah: glow on carved cut faces and falling chunks. OFF = no emission / property block on carved meshes or chunks (molten drips still glow).")]
        public bool carveSurfaceGlowEnabled = false;
        [ColorUsage(false, true)] public Color carveGlowColor = new Color(1f, 0.38f, 0.08f, 1f);
        [Tooltip("Emission strength at curve value 1 (HDRP units; wide range for exposure).")]
        [Range(0f, 5000f)] public float carveGlowIntensity = 60f;
        [Tooltip("Seconds from carve to fully cooled.")]
        [Range(0.1f, 60f)] public float carveGlowCoolSeconds = 4f;
        [Tooltip("Glow over normalised cool time (0..1). 1 = full hot.")]
        public AnimationCurve carveGlowCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.25f, 0.6f), new Keyframe(1f, 0f));

        [Header("Molten Drips (2026-10-24af)")]
        public bool carveDripsEnabled = true;
        [Tooltip("Drips per second per hot carved object.")]
        [Range(0f, 200f)] public float carveDripRate = 8f;
        [Tooltip("Drip size (m).")]
        [Range(0.002f, 0.5f)] public float carveDripSize = 0.025f;
        [Tooltip("Seconds a drip lives (glow follows the glow curve over this time).")]
        [Range(0.1f, 30f)] public float carveDripLifetime = 2.5f;
        [Tooltip("Fraction of the cool time during which the rim keeps dripping.")]
        [Range(0f, 1f)] public float carveDripWindow = 0.6f;
        [Tooltip("Global cap on live drips (oldest recycled).")]
        [Range(0, 1000)] public int carveDripMaxLive = 80;
        [Range(0f, 3f)] public float carveDripGravity = 1f;
        [Range(0f, 5000f)] public float carveDripIntensity = 2.5f; // 24ag: Anthony default 2.5
        [Tooltip("Colour of a fully cooled drip.")]
        public Color carveDripCooledColor = new Color(0.08f, 0.07f, 0.065f, 1f);

        [Header("Through-Holes (2026-10-24ag)")]
        [Tooltip("When a carve hit goes through a wall / prop thinner than Max Thickness, cut one clean straight tunnel (closed inner wall, no ragged rim) instead of a noisy crater.")]
        public bool throughHoleEnabled = true;
        [Tooltip("Max object thickness (m) along the hit normal that gets a clean through-hole.")]
        [Range(0.02f, 10f)] public float throughHoleMaxThickness = 0.8f;
        [Tooltip("Through-hole radius = carve radius x this.")]
        [Range(0.2f, 4f)] public float throughHoleRadiusScale = 1f;
        [Tooltip("Dark rim band width on through-holes (cutter-radius units). 0 = none.")]
        [Range(0f, 0.3f)] public float throughHoleRimWidth = 0.02f;
        [Tooltip("Tunnel wall smoothness (icosphere subdivisions of the tunnel cutter).")]
        [Range(2, 5)] public int throughHoleSubdivisions = 4;

        [Header("Falling Chunks (2026-10-24ag)")]
        [Tooltip("A carve that fully separates a piece turns it into its own falling object (convex collider + Rigidbody), with the hot glow.")]
        public bool chunkSplitEnabled = true;
        [Tooltip("Chunk density (kg/m3 of bounds volume x 0.5).")]
        [Range(10f, 10000f)] public float chunkDensity = 2400f;
        [Range(0.1f, 2000f)] public float chunkMaxMass = 400f;
        [Tooltip("Separated pieces smaller than this (m, largest side) are deleted instead of dropped (no slivers).")]
        [Range(0.001f, 1f)] public float chunkMinSize = 0.04f;
        [Tooltip("Only pieces within this many cutter radii of the cut are split off (keeps separate parts of a model attached).")]
        [Range(1f, 10f)] public float chunkSearchRadii = 3f;
        [Tooltip("Seconds before a chunk despawns.")]
        [Range(1f, 600f)] public float chunkLifetime = 20f;
        [Tooltip("Max live chunks (oldest despawns).")]
        [Range(0, 200)] public int chunkMaxLive = 24;
        [Tooltip("Log one line per carve that splits off / deletes pieces.")]
        public bool logChunkSplit = false;

        /// <summary>0 Subtle, 1 Default, 2 Extreme.</summary>
        public void ApplyCarveChunkPreset(int preset)
        {
            if (preset <= 0) { throughHoleMaxThickness = 0.4f; chunkMinSize = 0.08f; chunkLifetime = 8f; chunkMaxLive = 8; chunkDensity = 2400f; }
            else if (preset == 1) { throughHoleMaxThickness = 0.8f; chunkMinSize = 0.04f; chunkLifetime = 20f; chunkMaxLive = 24; chunkDensity = 2400f; }
            else { throughHoleMaxThickness = 3f; chunkMinSize = 0.015f; chunkLifetime = 90f; chunkMaxLive = 100; chunkDensity = 2400f; }
        }

        /// <summary>0 Subtle, 1 Default, 2 Extreme.</summary>
        public void ApplyCarveHeatPreset(int preset)
        {
            if (preset <= 0) { carveGlowIntensity = 20f; carveGlowCoolSeconds = 2f; carveDripRate = 3f; carveDripSize = 0.015f; carveDripLifetime = 1.5f; carveDripMaxLive = 30; carveDripIntensity = 1f; }
            else if (preset == 1) { carveGlowIntensity = 60f; carveGlowCoolSeconds = 4f; carveDripRate = 8f; carveDripSize = 0.025f; carveDripLifetime = 2.5f; carveDripMaxLive = 80; carveDripIntensity = 2.5f; }
            else { carveGlowIntensity = 400f; carveGlowCoolSeconds = 10f; carveDripRate = 40f; carveDripSize = 0.05f; carveDripLifetime = 5f; carveDripMaxLive = 300; carveDripIntensity = 25f; }
        }

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
