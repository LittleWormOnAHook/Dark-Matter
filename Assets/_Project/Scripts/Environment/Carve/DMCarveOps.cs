using System.Collections.Generic;
using UnityEngine;

namespace Project.SurfaceCarve
{
    public struct DMCarveCapTarget
    {
        public int FractureSubmesh;
        public int RimSubmesh;
    }

    /// <summary>Maps the submesh that was hit to the cap submeshes (inside of the cut, dark rim).</summary>
    public delegate DMCarveCapTarget DMCarveCapResolver(int hitSubmesh);

    public sealed class DMCarveParams
    {
        public int CutterSubdivisions = 3;
        public float SplitEdge = 0.3f;
        public float RimWidth = 0.14f;
        public float RimJitter = 0.65f;
        public float UvTileMeters = 1.5f;
        public int MaxTriangles = 300000;

        public static DMCarveParams FromSettings(DMCarveSettings s, float radius)
        {
            return new DMCarveParams
            {
                CutterSubdivisions = radius <= s.smallCutterRadius ? s.smallCutterSubdivisions : s.cutterSubdivisions,
                SplitEdge = s.surfaceSplitEdge,
                RimWidth = s.rimWidth,
                RimJitter = s.rimJitter,
                UvTileMeters = s.cutUvTileMeters,
                MaxTriangles = s.maxTrianglesPerObject
            };
        }
    }

    public struct DMCarveResult
    {
        public bool Changed;
        public int HitSubmesh;
        public int RemovedTriangles;
        public int CapTriangles;
    }

    /// <summary>
    /// Mesh carve: split surface triangles under the cutter, clip them against the cutter
    /// volume (clean cut line, shared crossing vertices), then add the part of the cutter
    /// surface that lies inside the solid as an inward-facing cap with a jagged dark rim band.
    /// </summary>
    public static class DMCarveOps
    {
        private static readonly List<Vector3> CutterVerts = new List<Vector3>();
        private static readonly List<Vector3> CutterNormals = new List<Vector3>();
        private static readonly List<int> CutterTris = new List<int>();

        public static DMCarveResult Carve(DMCarveMeshData d, Matrix4x4 localToWorld, DMCarveCutter cut, DMCarveParams p, DMCarveCapResolver resolver)
        {
            var result = new DMCarveResult { HitSubmesh = -1 };
            if (d == null || cut == null || d.VertexCount == 0)
                return result;

            Matrix4x4 worldToLocal = localToWorld.inverse;
            Matrix4x4 normalToLocal = localToWorld.transpose;
            float radius = cut.Radius;
            float maxR = cut.MaxRadius;

            var world = new List<Vector3>(d.VertexCount + 256);
            for (int i = 0; i < d.VertexCount; i++)
                world.Add(localToWorld.MultiplyPoint3x4(d.Positions[i]));

            var cutBounds = new Bounds(cut.Center, Vector3.one * (2f * maxR));
            Bounds splitBounds = cutBounds;
            splitBounds.Expand(radius * 0.1f);

            // 1. Split big surface triangles under the cutter so small cutters still cut big faces.
            float target = Mathf.Max(radius * p.SplitEdge, 0.002f);
            float target2 = target * target;
            int budget = Mathf.Max(0, p.MaxTriangles - d.TriangleCount);
            var mids = new Dictionary<long, int>();
            var stack = new Stack<Tri>();
            for (int s = 0; s < d.Submeshes.Count; s++)
            {
                List<int> src = d.Submeshes[s];
                var dst = new List<int>(src.Count + 64);
                for (int i = 0; i + 2 < src.Count; i += 3)
                {
                    int a = src[i], b = src[i + 1], c = src[i + 2];
                    if (!TriOverlaps(world[a], world[b], world[c], splitBounds))
                    {
                        dst.Add(a); dst.Add(b); dst.Add(c);
                        continue;
                    }

                    stack.Push(new Tri(a, b, c, 0));
                    while (stack.Count > 0)
                    {
                        Tri t = stack.Pop();
                        float e = Mathf.Max(
                            (world[t.A] - world[t.B]).sqrMagnitude,
                            Mathf.Max((world[t.B] - world[t.C]).sqrMagnitude, (world[t.C] - world[t.A]).sqrMagnitude));
                        if (e > target2 && t.Depth < 8 && budget > 0
                            && TriOverlaps(world[t.A], world[t.B], world[t.C], splitBounds))
                        {
                            int ab = Mid(d, world, mids, t.A, t.B);
                            int bc = Mid(d, world, mids, t.B, t.C);
                            int ca = Mid(d, world, mids, t.C, t.A);
                            int nd = t.Depth + 1;
                            stack.Push(new Tri(t.A, ab, ca, nd));
                            stack.Push(new Tri(t.B, bc, ab, nd));
                            stack.Push(new Tri(t.C, ca, bc, nd));
                            stack.Push(new Tri(ab, bc, ca, nd));
                            budget -= 3;
                        }
                        else
                        {
                            dst.Add(t.A); dst.Add(t.B); dst.Add(t.C);
                        }
                    }
                }

                src.Clear();
                src.AddRange(dst);
            }

            // 2. Solid surface near the cut (before clipping) for the inside/outside test of the cap.
            Bounds candBounds = cutBounds;
            candBounds.Expand(maxR);
            var grid = new DMCarveTriGrid(candBounds, radius * 0.35f);
            for (int s = 0; s < d.Submeshes.Count; s++)
            {
                List<int> t = d.Submeshes[s];
                for (int i = 0; i + 2 < t.Count; i += 3)
                {
                    Vector3 a = world[t[i]], b = world[t[i + 1]], c = world[t[i + 2]];
                    if (TriOverlaps(a, b, c, candBounds))
                        grid.Add(a, b, c, s);
                }
            }

            grid.Build();

            // 3. Cutter sign per vertex (negative = inside the cutter).
            var sd = new List<float>(world.Count + 256);
            for (int i = 0; i < world.Count; i++)
                sd.Add(cutBounds.Contains(world[i]) ? cut.Evaluate(world[i]) : 1f);

            // 4. Clip surface triangles.
            var crossings = new Dictionary<long, int>();
            int removed = 0;
            for (int s = 0; s < d.Submeshes.Count; s++)
            {
                List<int> src = d.Submeshes[s];
                var dst = new List<int>(src.Count + 64);
                for (int i = 0; i + 2 < src.Count; i += 3)
                {
                    int a = src[i], b = src[i + 1], c = src[i + 2];
                    bool ia = sd[a] < 0f, ib = sd[b] < 0f, ic = sd[c] < 0f;
                    int n = (ia ? 1 : 0) + (ib ? 1 : 0) + (ic ? 1 : 0);
                    if (n == 0)
                    {
                        dst.Add(a); dst.Add(b); dst.Add(c);
                        continue;
                    }

                    removed++;
                    if (n == 3)
                        continue;

                    if (n == 1)
                    {
                        if (ia) KeepOneInside(d, world, sd, crossings, cut, dst, a, b, c);
                        else if (ib) KeepOneInside(d, world, sd, crossings, cut, dst, b, c, a);
                        else KeepOneInside(d, world, sd, crossings, cut, dst, c, a, b);
                    }
                    else
                    {
                        if (!ia) KeepTwoInside(d, world, sd, crossings, cut, dst, a, b, c);
                        else if (!ib) KeepTwoInside(d, world, sd, crossings, cut, dst, b, c, a);
                        else KeepTwoInside(d, world, sd, crossings, cut, dst, c, a, b);
                    }
                }

                src.Clear();
                src.AddRange(dst);
            }

            result.RemovedTriangles = removed;
            if (removed == 0)
                return result;

            result.Changed = true;
            if (grid.Count == 0)
                return result;

            // 5. Cap: cutter surface inside the solid, facing into the cut.
            grid.SignedDistance(cut.Center, out int hitSub);
            result.HitSubmesh = hitSub;
            DMCarveCapTarget capTarget = resolver != null
                ? resolver(Mathf.Max(0, hitSub))
                : new DMCarveCapTarget { FractureSubmesh = Mathf.Max(0, hitSub), RimSubmesh = Mathf.Max(0, hitSub) };
            if (capTarget.FractureSubmesh < 0 || capTarget.FractureSubmesh >= d.Submeshes.Count
                || capTarget.RimSubmesh < 0 || capTarget.RimSubmesh >= d.Submeshes.Count)
                return result;

            d.EnsureCapChannels();
            cut.BuildMesh(p.CutterSubdivisions, 1.006f, CutterVerts, CutterNormals, CutterTris);
            float eps = radius * 0.01f;
            var rsd = new float[CutterVerts.Count];
            for (int i = 0; i < rsd.Length; i++)
                rsd[i] = grid.SignedDistance(CutterVerts[i], out _) - eps;

            var cap = new CapBuilder
            {
                Data = d,
                WorldToLocal = worldToLocal,
                NormalToLocal = normalToLocal,
                Grid = grid,
                Eps = eps,
                Radius = radius,
                RimWidth = p.RimWidth * radius,
                RimJitter = p.RimJitter,
                UvTile = Mathf.Max(0.05f, p.UvTileMeters),
                Fracture = d.Submeshes[capTarget.FractureSubmesh],
                Rim = d.Submeshes[capTarget.RimSubmesh],
                Rsd = rsd
            };
            cap.Init(CutterVerts.Count);

            for (int i = 0; i + 2 < CutterTris.Count; i += 3)
            {
                int a = CutterTris[i], b = CutterTris[i + 1], c = CutterTris[i + 2];
                bool ia = rsd[a] < 0f, ib = rsd[b] < 0f, ic = rsd[c] < 0f;
                int n = (ia ? 1 : 0) + (ib ? 1 : 0) + (ic ? 1 : 0);
                if (n == 0)
                    continue;
                if (n == 3)
                {
                    cap.Emit(cap.Corner(a), cap.Corner(b), cap.Corner(c));
                    continue;
                }

                if (n == 1)
                {
                    if (ia) cap.OneInside(a, b, c);
                    else if (ib) cap.OneInside(b, c, a);
                    else cap.OneInside(c, a, b);
                }
                else
                {
                    if (!ia) cap.TwoInside(a, b, c);
                    else if (!ib) cap.TwoInside(b, c, a);
                    else cap.TwoInside(c, a, b);
                }
            }

            result.CapTriangles = cap.Emitted;
            return result;
        }

        private readonly struct Tri
        {
            public readonly int A, B, C, Depth;

            public Tri(int a, int b, int c, int depth)
            {
                A = a; B = b; C = c; Depth = depth;
            }
        }

        private static long Key(int a, int b)
        {
            return a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
        }

        private static int Mid(DMCarveMeshData d, List<Vector3> world, Dictionary<long, int> mids, int a, int b)
        {
            long k = Key(a, b);
            if (mids.TryGetValue(k, out int idx))
                return idx;
            idx = d.AddLerp(a, b, 0.5f);
            world.Add((world[a] + world[b]) * 0.5f);
            mids[k] = idx;
            return idx;
        }

        private static bool TriOverlaps(Vector3 a, Vector3 b, Vector3 c, Bounds bounds)
        {
            Vector3 min = Vector3.Min(a, Vector3.Min(b, c));
            Vector3 max = Vector3.Max(a, Vector3.Max(b, c));
            Vector3 bmin = bounds.min, bmax = bounds.max;
            return min.x <= bmax.x && max.x >= bmin.x
                && min.y <= bmax.y && max.y >= bmin.y
                && min.z <= bmax.z && max.z >= bmin.z;
        }

        /// <summary>Crossing vertex on edge outside->inside of the cutter, shared by both triangles on the edge.</summary>
        private static int Crossing(DMCarveMeshData d, List<Vector3> world, List<float> sd, Dictionary<long, int> cache, DMCarveCutter cut, int outside, int inside)
        {
            long k = Key(outside, inside);
            if (cache.TryGetValue(k, out int idx))
                return idx;

            Vector3 wo = world[outside], wi = world[inside];
            float lo = 0f, hi = 1f;
            for (int it = 0; it < 8; it++)
            {
                float m = (lo + hi) * 0.5f;
                if (cut.Evaluate(Vector3.LerpUnclamped(wo, wi, m)) < 0f) hi = m;
                else lo = m;
            }

            float t = (lo + hi) * 0.5f;
            idx = d.AddLerp(outside, inside, t);
            world.Add(Vector3.LerpUnclamped(wo, wi, t));
            sd.Add(0f);
            cache[k] = idx;
            return idx;
        }

        // Triangle (i, o1, o2) with i inside the cutter: keep quad p1, o1, o2, p2.
        private static void KeepOneInside(DMCarveMeshData d, List<Vector3> world, List<float> sd, Dictionary<long, int> cache, DMCarveCutter cut, List<int> dst, int i, int o1, int o2)
        {
            int p1 = Crossing(d, world, sd, cache, cut, o1, i);
            int p2 = Crossing(d, world, sd, cache, cut, o2, i);
            dst.Add(p1); dst.Add(o1); dst.Add(o2);
            dst.Add(p1); dst.Add(o2); dst.Add(p2);
        }

        // Triangle (o, i1, i2) with o outside the cutter: keep triangle o, p1, p2.
        private static void KeepTwoInside(DMCarveMeshData d, List<Vector3> world, List<float> sd, Dictionary<long, int> cache, DMCarveCutter cut, List<int> dst, int o, int i1, int i2)
        {
            int p1 = Crossing(d, world, sd, cache, cut, o, i1);
            int p2 = Crossing(d, world, sd, cache, cut, o, i2);
            dst.Add(o); dst.Add(p1); dst.Add(p2);
        }

        private sealed class CapBuilder
        {
            public DMCarveMeshData Data;
            public Matrix4x4 WorldToLocal;
            public Matrix4x4 NormalToLocal;
            public DMCarveTriGrid Grid;
            public float Eps;
            public float Radius;
            public float RimWidth;
            public float RimJitter;
            public float UvTile;
            public List<int> Fracture;
            public List<int> Rim;
            public float[] Rsd;
            public int Emitted;

            private int[] corner;
            private Dictionary<long, CapVert> crossings;

            public struct CapVert
            {
                public int Index;
                public Vector3 World;
                public float Rsd;
            }

            private CapVert[] cornerInfo;

            public void Init(int cutterVertexCount)
            {
                corner = new int[cutterVertexCount];
                cornerInfo = new CapVert[cutterVertexCount];
                for (int i = 0; i < corner.Length; i++)
                    corner[i] = -1;
                crossings = new Dictionary<long, CapVert>();
            }

            public CapVert Corner(int ci)
            {
                if (corner[ci] < 0)
                {
                    corner[ci] = 1;
                    cornerInfo[ci] = new CapVert
                    {
                        Index = AddCapVertex(CutterVerts[ci], CutterNormals[ci]),
                        World = CutterVerts[ci],
                        Rsd = Rsd[ci]
                    };
                }

                return cornerInfo[ci];
            }

            private CapVert Cross(int outsideCi, int insideCi)
            {
                long k = Key(outsideCi, insideCi);
                if (crossings.TryGetValue(k, out CapVert v))
                    return v;

                Vector3 wo = CutterVerts[outsideCi], wi = CutterVerts[insideCi];
                float lo = 0f, hi = 1f;
                for (int it = 0; it < 6; it++)
                {
                    float m = (lo + hi) * 0.5f;
                    if (Grid.SignedDistance(Vector3.LerpUnclamped(wo, wi, m), out _) - Eps < 0f) hi = m;
                    else lo = m;
                }

                float t = (lo + hi) * 0.5f;
                Vector3 w = Vector3.LerpUnclamped(wo, wi, t);
                Vector3 n = Vector3.LerpUnclamped(CutterNormals[outsideCi], CutterNormals[insideCi], t).normalized;
                v = new CapVert { Index = AddCapVertex(w, n), World = w, Rsd = 0f };
                crossings[k] = v;
                return v;
            }

            // Cutter triangle (i, o1, o2), i inside the solid: keep i, p1, p2.
            public void OneInside(int i, int o1, int o2)
            {
                CapVert vi = Corner(i);
                CapVert p1 = Cross(o1, i);
                CapVert p2 = Cross(o2, i);
                Emit(vi, p1, p2);
            }

            // Cutter triangle (o, i1, i2), o outside the solid: keep p1, i1, i2, p2.
            public void TwoInside(int o, int i1, int i2)
            {
                CapVert p1 = Cross(o, i1);
                CapVert v1 = Corner(i1);
                CapVert v2 = Corner(i2);
                CapVert p2 = Cross(o, i2);
                Emit(p1, v1, v2);
                Emit(p1, v2, p2);
            }

            /// <summary>Emit with reversed winding so the cap faces into the cut.</summary>
            public void Emit(CapVert a, CapVert b, CapVert c)
            {
                bool rim = IsRim(a) || IsRim(b) || IsRim(c);
                List<int> dst = rim ? Rim : Fracture;
                dst.Add(a.Index);
                dst.Add(c.Index);
                dst.Add(b.Index);
                Emitted++;
            }

            private bool IsRim(CapVert v)
            {
                if (RimWidth <= 0f)
                    return false;
                float jitter = DMCarveNoise.Value01(v.World * (7f / Mathf.Max(Radius, 0.01f)));
                float w = RimWidth * (1f - RimJitter * 0.5f + RimJitter * jitter);
                return Mathf.Abs(v.Rsd + Eps) < w;
            }

            private int AddCapVertex(Vector3 world, Vector3 outwardNormal)
            {
                Vector3 nWorld = -outwardNormal;
                Vector3 local = WorldToLocal.MultiplyPoint3x4(world);
                Vector3 nLocal = NormalToLocal.MultiplyVector(nWorld);
                nLocal = nLocal.sqrMagnitude > 1e-12f ? nLocal.normalized : Vector3.up;

                Vector3 ax = new Vector3(Mathf.Abs(nWorld.x), Mathf.Abs(nWorld.y), Mathf.Abs(nWorld.z));
                Vector2 uv;
                Vector3 tWorld;
                if (ax.x >= ax.y && ax.x >= ax.z)
                {
                    uv = new Vector2(world.z, world.y);
                    tWorld = Vector3.forward;
                }
                else if (ax.y >= ax.z)
                {
                    uv = new Vector2(world.x, world.z);
                    tWorld = Vector3.right;
                }
                else
                {
                    uv = new Vector2(world.x, world.y);
                    tWorld = Vector3.right;
                }

                uv /= UvTile;
                Vector3 tLocal = WorldToLocal.MultiplyVector(tWorld);
                tLocal = Vector3.ProjectOnPlane(tLocal, nLocal);
                if (tLocal.sqrMagnitude < 1e-10f)
                    tLocal = Vector3.ProjectOnPlane(Vector3.up, nLocal);
                tLocal = tLocal.sqrMagnitude > 1e-12f ? tLocal.normalized : Vector3.right;
                return Data.AddVertex(local, nLocal, new Vector4(tLocal.x, tLocal.y, tLocal.z, 1f), uv);
            }
        }
    }

    /// <summary>Uniform grid of world triangles for nearest-surface signed distance queries.</summary>
    public sealed class DMCarveTriGrid
    {
        private readonly List<Vector3> a = new List<Vector3>();
        private readonly List<Vector3> b = new List<Vector3>();
        private readonly List<Vector3> c = new List<Vector3>();
        private readonly List<Vector3> n = new List<Vector3>();
        private readonly List<int> sub = new List<int>();
        private readonly Vector3 min;
        private readonly float cell;
        private readonly int nx, ny, nz;
        private List<int>[] cells;
        private int[] stamp;
        private int stampId;

        public int Count => a.Count;

        public DMCarveTriGrid(Bounds bounds, float cellSize)
        {
            Vector3 size = bounds.size;
            float largest = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
            cell = Mathf.Max(cellSize, largest / 24f, 1e-3f);
            min = bounds.min;
            nx = Mathf.Clamp(Mathf.CeilToInt(size.x / cell), 1, 24);
            ny = Mathf.Clamp(Mathf.CeilToInt(size.y / cell), 1, 24);
            nz = Mathf.Clamp(Mathf.CeilToInt(size.z / cell), 1, 24);
        }

        public void Add(Vector3 va, Vector3 vb, Vector3 vc, int submesh)
        {
            Vector3 normal = Vector3.Cross(vb - va, vc - va);
            if (normal.sqrMagnitude < 1e-20f)
                return;
            a.Add(va);
            b.Add(vb);
            c.Add(vc);
            n.Add(normal.normalized);
            sub.Add(submesh);
        }

        public void Build()
        {
            cells = new List<int>[nx * ny * nz];
            stamp = new int[a.Count];
            for (int t = 0; t < a.Count; t++)
            {
                Vector3 lo = Vector3.Min(a[t], Vector3.Min(b[t], c[t]));
                Vector3 hi = Vector3.Max(a[t], Vector3.Max(b[t], c[t]));
                int x0 = CellX(lo.x), x1 = CellX(hi.x);
                int y0 = CellY(lo.y), y1 = CellY(hi.y);
                int z0 = CellZ(lo.z), z1 = CellZ(hi.z);
                for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                for (int z = z0; z <= z1; z++)
                {
                    int id = (x * ny + y) * nz + z;
                    (cells[id] ?? (cells[id] = new List<int>(8))).Add(t);
                }
            }
        }

        private int CellX(float v) { return Mathf.Clamp(Mathf.FloorToInt((v - min.x) / cell), 0, nx - 1); }
        private int CellY(float v) { return Mathf.Clamp(Mathf.FloorToInt((v - min.y) / cell), 0, ny - 1); }
        private int CellZ(float v) { return Mathf.Clamp(Mathf.FloorToInt((v - min.z) / cell), 0, nz - 1); }

        /// <summary>Signed distance to the nearest stored triangle: negative inside the solid.</summary>
        public float SignedDistance(Vector3 p, out int submesh)
        {
            submesh = -1;
            if (a.Count == 0 || cells == null)
                return float.MaxValue;

            int cx = CellX(p.x), cy = CellY(p.y), cz = CellZ(p.z);
            float best = float.MaxValue;
            int bestTri = -1;
            Vector3 bestPoint = p;
            Vector3 nsum = Vector3.zero;
            stampId++;
            if (stampId == int.MaxValue)
            {
                stampId = 1;
                System.Array.Clear(stamp, 0, stamp.Length);
            }

            int maxRing = Mathf.Max(nx, Mathf.Max(ny, nz));
            for (int r = 0; r <= maxRing; r++)
            {
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if (x < 0 || x >= nx) continue;
                    bool xEdge = x == cx - r || x == cx + r;
                    for (int y = cy - r; y <= cy + r; y++)
                    {
                        if (y < 0 || y >= ny) continue;
                        bool yEdge = y == cy - r || y == cy + r;
                        int zStep = (xEdge || yEdge || r == 0) ? 1 : Mathf.Max(1, 2 * r);
                        for (int z = cz - r; z <= cz + r; z += zStep)
                        {
                            if (z < 0 || z >= nz) continue;
                            List<int> list = cells[(x * ny + y) * nz + z];
                            if (list == null) continue;
                            for (int li = 0; li < list.Count; li++)
                            {
                                int t = list[li];
                                if (stamp[t] == stampId) continue;
                                stamp[t] = stampId;
                                Vector3 q = ClosestPointOnTriangle(p, a[t], b[t], c[t]);
                                float d2 = (p - q).sqrMagnitude;
                                float tie = 1e-12f + best * 1e-5f;
                                if (d2 < best - tie)
                                {
                                    best = d2;
                                    bestTri = t;
                                    bestPoint = q;
                                    nsum = n[t];
                                }
                                else if (d2 <= best + tie)
                                {
                                    nsum += n[t];
                                }
                            }
                        }
                    }
                }

                if (bestTri >= 0)
                {
                    float reach = r * cell;
                    if (best <= reach * reach)
                        break;
                }
            }

            if (bestTri < 0)
                return float.MaxValue;

            submesh = sub[bestTri];
            float dist = Mathf.Sqrt(best);
            return Vector3.Dot(p - bestPoint, nsum) < 0f ? -dist : dist;
        }

        public static Vector3 ClosestPointOnTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ab = b - a, ac = c - a, ap = p - a;
            float d1 = Vector3.Dot(ab, ap), d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0f && d2 <= 0f) return a;
            Vector3 bp = p - b;
            float d3 = Vector3.Dot(ab, bp), d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0f && d4 <= d3) return b;
            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0f && d1 >= 0f && d3 <= 0f)
                return a + ab * (d1 / (d1 - d3));
            Vector3 cp = p - c;
            float d5 = Vector3.Dot(ab, cp), d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0f && d5 <= d6) return c;
            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0f && d2 >= 0f && d6 <= 0f)
                return a + ac * (d2 / (d2 - d6));
            float va = d3 * d6 - d5 * d4;
            if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
                return b + (c - b) * ((d4 - d3) / ((d4 - d3) + (d5 - d6)));
            float denom = 1f / (va + vb + vc);
            return a + ab * (vb * denom) + ac * (vc * denom);
        }
    }
}
