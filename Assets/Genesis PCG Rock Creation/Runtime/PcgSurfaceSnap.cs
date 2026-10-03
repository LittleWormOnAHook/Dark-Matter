using System;
using System.Collections.Generic;
using UnityEngine;

namespace GenesisPCG.RockCreation
{
    [Serializable]
    public sealed class PcgSurfaceSnapSettings
    {
        [Tooltip("Snap to the ground under the object on create, paste/Ctrl+D and Shift+drag duplicate.")]
        public bool snapToSurface = true;
        [Tooltip("Tilt the object's up axis toward the surface normal.")]
        public bool alignToNormal = true;
        [Range(0f, 1f), Tooltip("0 = stay upright (world up), 1 = fully follow the surface normal.")]
        public float alignStrength = 1f;
        [Range(0f, 90f), Tooltip("Never tilt further than this from world up, even on steep slopes.")]
        public float maxAlignAngle = 45f;
        [Tooltip("Layers that count as ground. The object's own colliders are always ignored.")]
        public LayerMask surfaceLayerMask = ~0;
        [Tooltip("Also exclude the object's own layer from the ground mask. Leave off if rocks and terrain share the Default layer.")]
        public bool excludeOwnLayer;
        [Tooltip("Allow snapping on top of other proc rocks / DM PCG Creators. Off = only terrain and regular scene geometry.")]
        public bool snapOntoOtherProcRocks;
        [Min(0f), Tooltip("Pivot (bottom center) ends up this many meters below the hit point.")]
        public float sinkDepth = 0.05f;
        [Min(1f), Tooltip("Rays start this far above the object.")]
        public float rayStartHeight = 50f;

        [Header("Conform base to surface")]
        [Tooltip("After snapping, bend the bottom of the generated mesh so it hugs uneven ground (re-applied from the undeformed mesh on every move/snap).")]
        public bool conformBaseToSurface = true;
        [Min(0f), Tooltip("Height of the band above the rock's lowest point that conforms. Fraction of the rock's height, or meters if Conform Height In Meters is on.")]
        public float conformHeight = 0.2f;
        public bool conformHeightInMeters;
        [Min(0.01f), Tooltip("Weight = (1 - heightInBand / conformHeight) ^ falloff. Higher = deformation stays closer to the ground line.")]
        public float conformFalloff = 1.5f;
        [Min(0f), Tooltip("No vertex is moved further than this (meters).")]
        public float maxConformDistance = 1.5f;
        [Min(0f), Tooltip("Conformed vertices end up this far below the surface so no gaps show.")]
        public float extraSink = 0.05f;
        [Tooltip("Off (default): only vertices floating above the ground are pulled down; buried vertices are already hidden and keep the rock's shape. On: buried vertices are also lifted toward the surface.")]
        public bool conformLiftBuried;
        [Tooltip("Downward-facing (underside) vertices inside the band are pulled all the way to the ground regardless of falloff, so no daylight shows under lips and overhangs.")]
        public bool conformUndersideFully = true;

        [Header("Ground contact guarantee")]
        [Tooltip("Independent of the conform band (also when Conform Height is 0): open bottom rims of pieces and downward-facing " +
                 "undersides floating up to Contact Reach above the ground are pulled to Contact Depth below it, so no daylight shows " +
                 "between a rock and the terrain after bake, move, rotate, paste or library placement.")]
        public bool guaranteeContact = true;
        [Min(0f), Tooltip("Rims / undersides floating at most this far above the ground (m) are pulled down. Higher gaps (nooks, passages, overhangs) are kept.")]
        public float contactReach = 0.35f;
        [Min(0.02f), Tooltip("Pulled vertices end this far below the ground (m). The bake's flush cut then trims them to its own cut line.")]
        public float contactDepth = 0.04f;

        [Header("Vertex masks (rock shader)")]
        [Min(0.01f), Tooltip("Vertex colour R (terrain blend) fades from 1 at the ground contact to 0 this many meters above the surface.")]
        public float bottomBlendHeight = 0.5f;

        public static readonly PcgSurfaceSnapSettings Default = new PcgSurfaceSnapSettings();
    }

    /// <summary>Edit/runtime helper that drops an object onto the surface below it and aligns it to the normal.</summary>
    public static class PcgSurfaceSnap
    {
        private static readonly RaycastHit[] Hits = new RaycastHit[64];

        public struct Result
        {
            public bool hit;
            public Vector3 point;
            public Vector3 normal;
            public Collider collider; // null when the terrain heightmap fallback was used
        }

        /// <summary>Finds the ground under <paramref name="probe"/>, ignoring every collider under any of <paramref name="ignoreRoots"/>.</summary>
        public static Result FindSurface(Vector3 probe, PcgSurfaceSnapSettings s, int ownLayer, IList<Transform> ignoreRoots)
        {
            s ??= PcgSurfaceSnapSettings.Default;
            int mask = s.surfaceLayerMask;
            if (s.excludeOwnLayer && ownLayer >= 0)
                mask &= ~(1 << ownLayer);

            Physics.SyncTransforms(); // edit-time moves are not pushed to physics automatically
            if (RaycastFiltered(new Ray(probe + Vector3.up * s.rayStartHeight, Vector3.down), s.rayStartHeight * 2f + 1000f, mask, s, ignoreRoots, false, out Result best))
                return best;

            // Fallback: terrain heightmap (terrain without collider, or physics not synced yet).
            foreach (Terrain t in Terrain.activeTerrains)
            {
                if (t == null || t.terrainData == null)
                    continue;
                if (((1 << t.gameObject.layer) & mask) == 0)
                    continue;
                Vector3 tp = t.GetPosition();
                Vector3 size = t.terrainData.size;
                float u = (probe.x - tp.x) / size.x;
                float v = (probe.z - tp.z) / size.z;
                if (u < 0f || u > 1f || v < 0f || v > 1f)
                    continue;
                float y = t.SampleHeight(probe) + tp.y;
                Vector3 nrm = t.terrainData.GetInterpolatedNormal(u, v);
                return new Result { hit = true, point = new Vector3(probe.x, y, probe.z), normal = nrm.normalized };
            }
            return best;
        }

        /// <summary>Ray (e.g. from the mouse) against the ground, with the same filtering as <see cref="FindSurface"/>.</summary>
        public static bool RaycastSurface(Ray ray, PcgSurfaceSnapSettings s, int ownLayer, IList<Transform> ignoreRoots, out Result result)
        {
            s ??= PcgSurfaceSnapSettings.Default;
            int mask = s.surfaceLayerMask;
            if (s.excludeOwnLayer && ownLayer >= 0)
                mask &= ~(1 << ownLayer);
            Physics.SyncTransforms();
            return RaycastFiltered(ray, 100000f, mask, s, ignoreRoots, false, out result);
        }

        private static bool RaycastFiltered(Ray ray, float dist, int mask, PcgSurfaceSnapSettings s, IList<Transform> ignoreRoots, bool skipTerrain, out Result best)
        {
            int n = Physics.RaycastNonAlloc(ray, Hits, dist, mask, QueryTriggerInteraction.Ignore);
            best = new Result();
            float bestDist = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                RaycastHit h = Hits[i];
                if (h.collider == null || h.distance >= bestDist)
                    continue;
                if (IsIgnored(h.collider.transform, ignoreRoots))
                    continue;
                if (skipTerrain && h.collider is TerrainCollider)
                    continue;
                if (!s.snapOntoOtherProcRocks && h.collider.GetComponentInParent<IPcgSeeded>() != null)
                    continue;
                bestDist = h.distance;
                best = new Result { hit = true, point = h.point, normal = h.normal, collider = h.collider };
            }
            return best.hit;
        }

        /// <summary>
        /// Rotation whose up axis follows the (clamped, blended) surface normal while keeping the current yaw.
        /// </summary>
        public static Quaternion AlignedRotation(Quaternion current, Vector3 normal, PcgSurfaceSnapSettings s)
        {
            s ??= PcgSurfaceSnapSettings.Default;
            Vector3 fwd = current * Vector3.forward;
            Vector3 flat = Vector3.ProjectOnPlane(fwd, Vector3.up);
            if (flat.sqrMagnitude < 1e-6f)
                flat = Vector3.ProjectOnPlane(current * Vector3.right, Vector3.up);
            if (flat.sqrMagnitude < 1e-6f)
                flat = Vector3.forward;
            Quaternion yaw = Quaternion.LookRotation(flat.normalized, Vector3.up);

            if (!s.alignToNormal || normal.sqrMagnitude < 1e-6f)
                return yaw;

            Vector3 up = Vector3.Slerp(Vector3.up, normal.normalized, Mathf.Clamp01(s.alignStrength));
            float ang = Vector3.Angle(Vector3.up, up);
            if (ang > s.maxAlignAngle && ang > 1e-4f)
                up = Vector3.Slerp(Vector3.up, up, s.maxAlignAngle / ang);
            return Quaternion.FromToRotation(Vector3.up, up) * yaw;
        }

        /// <summary>Snaps <paramref name="t"/> (pivot = bottom center) onto the surface below it. Caller handles Undo.</summary>
        public static bool Snap(Transform t, PcgSurfaceSnapSettings s, IList<Transform> ignoreRoots = null)
        {
            if (t == null)
                return false;
            s ??= PcgSurfaceSnapSettings.Default;
            if (!s.snapToSurface)
                return false;

            var ignore = new List<Transform> { t };
            if (ignoreRoots != null)
                ignore.AddRange(ignoreRoots);

            Result r = FindSurface(t.position, s, t.gameObject.layer, ignore);
            if (!r.hit)
                return false;

            Quaternion rot = AlignedRotation(t.rotation, r.normal, s);
            Vector3 up = rot * Vector3.up;
            var so = t.GetComponent<IPcgSinkOffset>();
            float extra = so != null ? so.UserSinkOffset : 0f;
            t.SetPositionAndRotation(r.point - up * (s.sinkDepth + extra), rot);
            return true;
        }

        /// <summary>
        /// Highest ground below <paramref name="world"/> within <paramref name="below"/> meters (starting <paramref name="above"/> above it):
        /// Terrain.SampleHeight for terrains, filtered Physics raycast for every other collider. Call Physics.SyncTransforms first.
        /// </summary>
        public static bool SampleGroundY(Vector3 world, float above, float below, int mask, PcgSurfaceSnapSettings s, IList<Transform> ignoreRoots, out float groundY)
        {
            groundY = float.MinValue;
            float top = world.y + above, bottom = world.y - below;
            foreach (Terrain t in Terrain.activeTerrains)
            {
                if (t == null || t.terrainData == null || ((1 << t.gameObject.layer) & mask) == 0)
                    continue;
                Vector3 tp = t.GetPosition();
                Vector3 size = t.terrainData.size;
                if (world.x < tp.x || world.x > tp.x + size.x || world.z < tp.z || world.z > tp.z + size.z)
                    continue;
                float y = t.SampleHeight(world) + tp.y;
                if (y <= top && y >= bottom && y > groundY)
                    groundY = y;
            }
            if (RaycastFiltered(new Ray(new Vector3(world.x, top, world.z), Vector3.down), above + below, mask, s, ignoreRoots, true, out Result r)
                && r.point.y > groundY)
                groundY = r.point.y;
            return groundY > float.MinValue;
        }

        public struct ConformStats
        {
            public int bandVerts, movedVerts, sampledVerts, contactVerts;
            public float maxMove;
        }

        /// <summary>
        /// Placement pass, run after every seeded build and every move/snap. Starts from the undeformed arrays (never stacks):
        /// 1) optional conform: bends the bottom band onto the ground under <paramref name="t"/>;
        /// 2) writes vertex-colour masks for the rock shader: R = bottom terrain-blend (1 at ground contact, 0 at
        ///    bottomBlendHeight above it), G = top exposure (world normal . up remapped 0..1), B = piece size / VertexSizeRange
        ///    (1 without <paramref name="pieceSize"/>), A = sqrt(height above ground / VertexHeightRange).
        /// </summary>
        /// <summary>Vertex colour A = sqrt(height above ground / this), so the blend shader can use heights beyond the conform band.</summary>
        public const float VertexHeightRange = 4f;
        /// <summary>Vertex colour B = piece size (m) / this (geometric mean of the piece's two largest extents).</summary>
        public const float VertexSizeRange = 8f;
        /// <summary>Ground contact: vertices above a pulled rim / underside follow the pull over this many x the pull (min ContactBandMin), so the stretch is spread out (no vertical texture curtain).</summary>
        public const float ContactBandFactor = 3f;
        public const float ContactBandMin = 0.3f;
        /// <summary>XZ radius (m) around a pulled vertex within which vertices above it follow the pull.</summary>
        public const float ContactCarryRadius = 0.5f;

        public static ConformStats ConformMesh(Mesh mesh, Vector3[] srcVerts, Vector3[] srcNormals, Vector4[] srcTangents,
            Transform t, PcgSurfaceSnapSettings s, IList<Transform> ignoreRoots, float[] pieceSize = null)
        {
            var stats = new ConformStats();
            if (mesh == null || srcVerts == null || srcVerts.Length == 0 || t == null)
                return stats;
            s ??= PcgSurfaceSnapSettings.Default;

            int n = srcVerts.Length;
            bool hasN = srcNormals != null && srcNormals.Length == n;
            bool hasT = srcTangents != null && srcTangents.Length == n;

            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < n; i++) { minY = Mathf.Min(minY, srcVerts[i].y); maxY = Mathf.Max(maxY, srcVerts[i].y); }
            bool conform = s.conformBaseToSurface && s.maxConformDistance > 0f;
            float band = conform ? (s.conformHeightInMeters ? s.conformHeight : s.conformHeight * (maxY - minY)) : 0f;
            float scaleY = Mathf.Max(1e-4f, Mathf.Abs(t.lossyScale.y));
            float blend = Mathf.Max(1e-4f, s.bottomBlendHeight);
            // Local height below which a vertex needs a ground sample (conform band and/or bottom-blend mask).
            float sampleBand = Mathf.Max(band, (blend + 0.5f) / scaleY);

            int mask = s.surfaceLayerMask;
            if (s.excludeOwnLayer)
                mask &= ~(1 << t.gameObject.layer);
            var ignore = new List<Transform> { t };
            if (ignoreRoots != null) ignore.AddRange(ignoreRoots);
            Physics.SyncTransforms();

            Matrix4x4 l2w = t.localToWorldMatrix, w2l = t.worldToLocalMatrix;
            Matrix4x4 nrmM = l2w.inverse.transpose;
            float reach = s.maxConformDistance + 2f;
            float above = sampleBand * scaleY + reach, below = reach + blend;

            var verts = (Vector3[])srcVerts.Clone();
            var weights = new float[n];
            var ground = new float[n];
            var hasGround = new bool[n];
            for (int i = 0; i < n; i++)
            {
                float h = srcVerts[i].y - minY;
                if (h >= sampleBand) continue;
                Vector3 wp = l2w.MultiplyPoint3x4(srcVerts[i]);
                if (!SampleGroundY(wp, above, below, mask, s, ignore, out float gy)) continue;
                stats.sampledVerts++;
                ground[i] = gy;
                hasGround[i] = true;
                if (!conform || h >= band) continue;
                stats.bandVerts++;
                float w = Mathf.Pow(1f - h / band, s.conformFalloff);
                float gap = gy - s.extraSink - wp.y; // > 0: buried, < 0: floating
                if (gap > 0f && !s.conformLiftBuried) continue;
                if (gap < 0f && s.conformUndersideFully && hasN)
                {
                    float down = -nrmM.MultiplyVector(srcNormals[i]).normalized.y;
                    // Faded linearly with height so pulled undersides stay continuous with their (less pulled) neighbours.
                    w = Mathf.Max(w, Mathf.Clamp01(down * 4f) * (1f - h / band));
                }
                if (w <= 1e-4f) continue;
                float delta = Mathf.Clamp(gap * w, -s.maxConformDistance, s.maxConformDistance);
                if (Mathf.Abs(delta) < 1e-5f) continue;
                verts[i] = w2l.MultiplyPoint3x4(wp + Vector3.up * delta);
                weights[i] = w;
                stats.movedVerts++;
                stats.maxMove = Mathf.Max(stats.maxMove, Mathf.Abs(delta));
            }

            // Ground contact guarantee (independent of the band): open rims and undersides that still float near the ground.
            // Pulling ONLY those vertices stretched the single row of triangles above them into a vertical curtain (UVs
            // unchanged, so the texture smeared straight down like a skirt). The pull is now carried up a soft band: every
            // vertex above a pulled vertex (within ContactCarryRadius in XZ) follows with a linear falloff over
            // ContactBandFactor x the pull, so the extra length is spread over the band (~1.3x) instead of one row.
            if (s.guaranteeContact && s.contactReach > 0f)
            {
                bool[] rim = OpenRimVertices(mesh, srcVerts);
                float reachC = s.contactReach;
                float depth = Mathf.Max(0.02f, s.contactDepth);
                var pullIdx = new List<int>();
                var pullAmt = new List<float>();
                var pullPos = new List<Vector3>();
                for (int i = 0; i < n; i++)
                {
                    bool under = false;
                    if (!rim[i])
                    {
                        if (!hasN) continue;
                        under = nrmM.MultiplyVector(srcNormals[i]).normalized.y < -0.35f;
                        if (!under) continue;
                    }
                    Vector3 wp = l2w.MultiplyPoint3x4(verts[i]);
                    float gy;
                    if (hasGround[i]) gy = ground[i];
                    else
                    {
                        if (!SampleGroundY(wp, reachC + 0.5f, reach, mask, s, ignore, out gy)) continue;
                        ground[i] = gy;
                        hasGround[i] = true;
                    }
                    float hAbove = wp.y - gy;
                    if (hAbove <= -depth || hAbove > reachC) continue; // already buried enough / a real opening (nook, passage, lip)
                    pullIdx.Add(i);
                    pullAmt.Add(wp.y - (gy - depth));
                    pullPos.Add(wp);
                }
                if (pullIdx.Count > 0)
                {
                    const float cell = ContactCarryRadius;
                    var grid = new Dictionary<long, List<int>>();
                    float topY = float.MinValue;
                    for (int k = 0; k < pullIdx.Count; k++)
                    {
                        Vector3 pk = pullPos[k];
                        long key = ((long)Mathf.FloorToInt(pk.x / cell) << 32) ^ (uint)Mathf.FloorToInt(pk.z / cell);
                        if (!grid.TryGetValue(key, out List<int> lst)) grid[key] = lst = new List<int>();
                        lst.Add(k);
                        topY = Mathf.Max(topY, pk.y + Mathf.Max(ContactBandMin, pullAmt[k] * ContactBandFactor));
                    }
                    for (int j = 0; j < n; j++)
                    {
                        Vector3 pj = l2w.MultiplyPoint3x4(verts[j]);
                        if (pj.y > topY) continue;
                        int cx = Mathf.FloorToInt(pj.x / cell), cz = Mathf.FloorToInt(pj.z / cell);
                        float best = 0f, bestW = 0f;
                        for (int dx = -1; dx <= 1; dx++)
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                long key = ((long)(cx + dx) << 32) ^ (uint)(cz + dz);
                                if (!grid.TryGetValue(key, out List<int> lst)) continue;
                                foreach (int k in lst)
                                {
                                    Vector3 pk = pullPos[k];
                                    float dy = pj.y - pk.y;
                                    if (dy < -1e-4f) continue; // below the pulled vertex: buried side, keep
                                    float a = pullAmt[k];
                                    float cBand = Mathf.Max(ContactBandMin, a * ContactBandFactor);
                                    if (dy >= cBand) continue;
                                    float ex = pj.x - pk.x, ez = pj.z - pk.z;
                                    float dxz2 = ex * ex + ez * ez;
                                    if (dxz2 >= cell * cell) continue;
                                    float wr = 1f - Mathf.Sqrt(dxz2) / cell;
                                    wr = wr * wr * (3f - 2f * wr);
                                    float w = (1f - Mathf.Max(0f, dy) / cBand) * wr;
                                    float c = a * w;
                                    if (c > best) { best = c; bestW = w; }
                                }
                            }
                        if (best < 1e-5f) continue;
                        verts[j] = w2l.MultiplyPoint3x4(pj + Vector3.down * best);
                        if (weights[j] <= 0f) stats.movedVerts++;
                        weights[j] = Mathf.Max(weights[j], bestW);
                        stats.maxMove = Mathf.Max(stats.maxMove, best);
                    }
                    stats.contactVerts = pullIdx.Count;
                }
            }

            mesh.vertices = verts;
            Vector3[] normals = srcNormals;
            if (stats.movedVerts > 0 && hasN)
            {
                mesh.RecalculateNormals();
                Vector3[] rn = mesh.normals;
                for (int i = 0; i < n; i++)
                    rn[i] = weights[i] > 0f ? Vector3.Slerp(srcNormals[i], rn[i], weights[i]).normalized : srcNormals[i];
                mesh.normals = rn;
                normals = rn;
                if (hasT)
                {
                    mesh.RecalculateTangents();
                    Vector4[] rt = mesh.tangents;
                    for (int i = 0; i < n; i++)
                        if (weights[i] <= 0f) rt[i] = srcTangents[i];
                    mesh.tangents = rt;
                }
            }
            else
            {
                if (hasN) mesh.normals = srcNormals;
                if (hasT) mesh.tangents = srcTangents;
            }

            // Vertex-colour masks.
            // R needs the real height above ground for EVERY vertex near the ground, not just the band above the global minimum
            // (sunk / tilted pieces and slopes put ground contacts far above minY). Vertices without a band sample get a short
            // ground probe (2 m above .. blend + 0.35 m below), cached per 0.25 m world cell; none found = above the blend band.
            var colors = new Color32[n];
            var probe = new Dictionary<long, float>();
            const float pc = 0.25f;
            for (int i = 0; i < n; i++)
            {
                Vector3 wp = l2w.MultiplyPoint3x4(verts[i]);
                float heightAbove;
                if (hasGround[i]) heightAbove = wp.y - ground[i];
                else
                {
                    int cx = Mathf.FloorToInt(wp.x / pc), cy = Mathf.FloorToInt(wp.y / pc), cz = Mathf.FloorToInt(wp.z / pc);
                    long key = ((long)(cx & 0x1FFFFF) << 42) | ((long)(cy & 0x1FFFFF) << 21) | (long)(cz & 0x1FFFFF);
                    if (!probe.TryGetValue(key, out float gy))
                    {
                        var cp = new Vector3((cx + 0.5f) * pc, (cy + 0.5f) * pc, (cz + 0.5f) * pc);
                        gy = SampleGroundY(cp, 2f, Mathf.Max(blend, VertexHeightRange) + 0.35f + pc, mask, s, ignore, out float g2) ? g2 : float.NaN;
                        probe[key] = gy;
                    }
                    heightAbove = float.IsNaN(gy) ? Mathf.Max(blend, VertexHeightRange) * 2f : wp.y - gy;
                }
                float r = 1f - Mathf.Clamp01(heightAbove / blend);
                float g = 0.5f;
                if (hasN)
                {
                    Vector3 wn = nrmM.MultiplyVector(normals[i]).normalized;
                    g = Mathf.Clamp01(wn.y * 0.5f + 0.5f);
                }
                float bsz = pieceSize != null && i < pieceSize.Length ? Mathf.Clamp01(pieceSize[i] / VertexSizeRange) : 1f;
                float ah = Mathf.Sqrt(Mathf.Clamp01(heightAbove / VertexHeightRange));
                colors[i] = new Color32((byte)Mathf.RoundToInt(r * 255f), (byte)Mathf.RoundToInt(g * 255f),
                    (byte)Mathf.RoundToInt(bsz * 255f), (byte)Mathf.RoundToInt(ah * 255f));
            }
            mesh.colors32 = colors;
            mesh.RecalculateBounds();
            return stats;
        }

        // Open-rim flags per build (keyed by the undeformed vertex array, which is new for every seeded build).
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Vector3[], bool[]> s_rims =
            new System.Runtime.CompilerServices.ConditionalWeakTable<Vector3[], bool[]>();

        /// <summary>
        /// Vertices on an open (boundary) edge: an edge used by exactly one triangle once vertices at the same position are
        /// welded (UV / normal seams are not openings). These are the open bottoms of pack pieces and earlier cut lines.
        /// </summary>
        public static bool[] OpenRimVertices(Mesh mesh, Vector3[] v)
        {
            if (s_rims.TryGetValue(v, out bool[] cached) && cached.Length == v.Length) return cached;
            int n = v.Length;
            var rim = new bool[n];
            var weldOf = new int[n];
            var map = new Dictionary<Vector3Int, int>(n);
            for (int i = 0; i < n; i++)
            {
                var k = new Vector3Int(Mathf.RoundToInt(v[i].x * 2000f), Mathf.RoundToInt(v[i].y * 2000f), Mathf.RoundToInt(v[i].z * 2000f));
                if (!map.TryGetValue(k, out int w)) { w = i; map[k] = i; }
                weldOf[i] = w;
            }
            var edges = new Dictionary<long, int>();
            var tri = new List<int>();
            for (int sm = 0; sm < mesh.subMeshCount; sm++)
            {
                if (mesh.GetTopology(sm) != MeshTopology.Triangles) continue;
                mesh.GetTriangles(tri, sm);
                for (int i = 0; i + 2 < tri.Count; i += 3)
                    for (int e = 0; e < 3; e++)
                    {
                        int a = tri[i + e], b = tri[i + (e + 1) % 3];
                        if (a >= n || b >= n) continue;
                        a = weldOf[a]; b = weldOf[b];
                        if (a == b) continue;
                        long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                        edges[key] = edges.TryGetValue(key, out int c) ? c + 1 : 1;
                    }
            }
            var rimWeld = new bool[n];
            foreach (KeyValuePair<long, int> kv in edges)
                if (kv.Value == 1) { rimWeld[(int)(kv.Key >> 32)] = true; rimWeld[(int)(kv.Key & 0xffffffffL)] = true; }
            for (int i = 0; i < n; i++) rim[i] = rimWeld[weldOf[i]];
            s_rims.Remove(v);
            s_rims.Add(v, rim);
            return rim;
        }

        private static bool IsIgnored(Transform hit, IList<Transform> roots)
        {
            if (roots == null)
                return false;
            for (int i = 0; i < roots.Count; i++)
                if (roots[i] != null && (hit == roots[i] || hit.IsChildOf(roots[i])))
                    return true;
            return false;
        }
    }
}
