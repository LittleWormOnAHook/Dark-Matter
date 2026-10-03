using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Project.SurfaceCarve
{
    /// <summary>Editable copy of a mesh (positions, normals, tangents, uv0..uv7, colors, triangle submeshes).</summary>
    public sealed class DMCarveMeshData
    {
        public readonly List<Vector3> Positions = new List<Vector3>();
        public readonly List<Vector3> Normals = new List<Vector3>();
        public readonly List<Vector4> Tangents = new List<Vector4>();
        public readonly List<Vector2> Uv0 = new List<Vector2>();
        public readonly List<Vector2> Uv1 = new List<Vector2>();
        public readonly List<Color> Colors = new List<Color>();
        public readonly List<List<int>> Submeshes = new List<List<int>>();

        // UV channels 2..7 (index 0 = uv2), kept at their original dimension. Genesis PCG baked rocks store the
        // texture-array slice / group in UV3: dropping it on carve re-mapped every piece to slice 0 (texture swirl on hit).
        private const int ExtraUvFirst = 2;
        private const int ExtraUvCount = 6;
        private readonly List<Vector4>[] extraUv = new List<Vector4>[ExtraUvCount];
        private readonly int[] extraUvDim = new int[ExtraUvCount]; // 0 = channel absent

        public bool HasNormals;
        public bool HasTangents;
        public bool HasUv0;
        public bool HasUv1;
        public bool HasColors;

        public int VertexCount => Positions.Count;

        public int TriangleCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Submeshes.Count; i++)
                    n += Submeshes[i].Count / 3;
                return n;
            }
        }

        public static bool TryRead(Mesh mesh, out DMCarveMeshData data, out string error)
        {
            data = null;
            error = null;
            if (mesh == null)
            {
                error = "No mesh.";
                return false;
            }

            if (Application.isPlaying && !mesh.isReadable)
            {
                error = "Mesh '" + mesh.name + "' is not Read/Write enabled (enable it on the model import settings).";
                return false;
            }

            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                if (mesh.GetTopology(s) != MeshTopology.Triangles)
                {
                    error = "Mesh '" + mesh.name + "' has a non-triangle submesh.";
                    return false;
                }
            }

            var d = new DMCarveMeshData();
            mesh.GetVertices(d.Positions);
            mesh.GetNormals(d.Normals);
            mesh.GetTangents(d.Tangents);
            mesh.GetUVs(0, d.Uv0);
            mesh.GetUVs(1, d.Uv1);
            mesh.GetColors(d.Colors);
            int vc = d.Positions.Count;
            d.HasNormals = d.Normals.Count == vc;
            d.HasTangents = d.Tangents.Count == vc;
            d.HasUv0 = d.Uv0.Count == vc;
            d.HasUv1 = d.Uv1.Count == vc;
            d.HasColors = d.Colors.Count == vc;
            if (!d.HasNormals) d.Normals.Clear();
            if (!d.HasTangents) d.Tangents.Clear();
            if (!d.HasUv0) d.Uv0.Clear();
            if (!d.HasUv1) d.Uv1.Clear();
            if (!d.HasColors) d.Colors.Clear();

            for (int e = 0; e < ExtraUvCount; e++)
            {
                VertexAttribute attr = VertexAttribute.TexCoord0 + (ExtraUvFirst + e);
                if (!mesh.HasVertexAttribute(attr))
                    continue;
                var list = new List<Vector4>(vc);
                mesh.GetUVs(ExtraUvFirst + e, list);
                if (list.Count != vc)
                    continue;
                d.extraUv[e] = list;
                d.extraUvDim[e] = Mathf.Clamp(mesh.GetVertexAttributeDimension(attr), 2, 4);
            }

            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                var list = new List<int>();
                mesh.GetTriangles(list, s);
                d.Submeshes.Add(list);
            }

            data = d;
            return true;
        }

        public void WriteTo(Mesh mesh)
        {
            mesh.Clear();
            mesh.indexFormat = Positions.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(Positions);
            if (HasNormals) mesh.SetNormals(Normals);
            if (HasTangents) mesh.SetTangents(Tangents);
            if (HasUv0) mesh.SetUVs(0, Uv0);
            if (HasUv1) mesh.SetUVs(1, Uv1);
            if (HasColors) mesh.SetColors(Colors);
            WriteExtraUvs(mesh);
            mesh.subMeshCount = Submeshes.Count;
            for (int s = 0; s < Submeshes.Count; s++)
                mesh.SetTriangles(Submeshes[s], s, false);
            mesh.RecalculateBounds();
        }

        private void WriteExtraUvs(Mesh mesh)
        {
            for (int e = 0; e < ExtraUvCount; e++)
            {
                List<Vector4> src = extraUv[e];
                if (extraUvDim[e] == 0 || src == null || src.Count != Positions.Count)
                    continue;
                int ch = ExtraUvFirst + e;
                if (extraUvDim[e] == 2)
                {
                    var l2 = new List<Vector2>(src.Count);
                    for (int i = 0; i < src.Count; i++) l2.Add(src[i]);
                    mesh.SetUVs(ch, l2);
                }
                else if (extraUvDim[e] == 3)
                {
                    var l3 = new List<Vector3>(src.Count);
                    for (int i = 0; i < src.Count; i++) l3.Add(src[i]);
                    mesh.SetUVs(ch, l3);
                }
                else
                    mesh.SetUVs(ch, src);
            }
        }

        public int AddSubmesh()
        {
            Submeshes.Add(new List<int>());
            return Submeshes.Count - 1;
        }

        /// <summary>New vertex interpolated between a and b.</summary>
        public int AddLerp(int a, int b, float t)
        {
            int idx = Positions.Count;
            Positions.Add(Vector3.LerpUnclamped(Positions[a], Positions[b], t));
            if (HasNormals)
            {
                Vector3 n = Vector3.LerpUnclamped(Normals[a], Normals[b], t);
                Normals.Add(n.sqrMagnitude > 1e-12f ? n.normalized : Normals[a]);
            }

            if (HasTangents)
            {
                Vector4 ta = Tangents[a];
                Vector3 tv = Vector3.LerpUnclamped((Vector3)ta, (Vector3)Tangents[b], t);
                tv = tv.sqrMagnitude > 1e-12f ? tv.normalized : (Vector3)ta;
                Tangents.Add(new Vector4(tv.x, tv.y, tv.z, ta.w));
            }

            if (HasUv0) Uv0.Add(Vector2.LerpUnclamped(Uv0[a], Uv0[b], t));
            if (HasUv1) Uv1.Add(Vector2.LerpUnclamped(Uv1[a], Uv1[b], t));
            if (HasColors) Colors.Add(Color.LerpUnclamped(Colors[a], Colors[b], t));
            for (int e = 0; e < ExtraUvCount; e++)
                if (extraUv[e] != null)
                    extraUv[e].Add(Vector4.LerpUnclamped(extraUv[e][a], extraUv[e][b], t));
            return idx;
        }

        /// <summary>New free vertex (cap faces). Mesh normals/tangents/uvs are always written so the cap shades.</summary>
        public int AddVertex(Vector3 pos, Vector3 normal, Vector4 tangent, Vector2 uv)
        {
            int idx = Positions.Count;
            Positions.Add(pos);
            if (HasNormals) Normals.Add(normal);
            if (HasTangents) Tangents.Add(tangent);
            if (HasUv0) Uv0.Add(uv);
            if (HasUv1) Uv1.Add(Vector2.zero);
            if (HasColors) Colors.Add(Color.white);
            for (int e = 0; e < ExtraUvCount; e++)
                if (extraUv[e] != null)
                    extraUv[e].Add(Vector4.zero);
            return idx;
        }

        /// <summary>Make sure the channels cap faces need exist (normals, tangents, uv0).</summary>
        public void EnsureCapChannels()
        {
            int vc = Positions.Count;
            if (!HasNormals)
            {
                Normals.Clear();
                for (int i = 0; i < vc; i++) Normals.Add(Vector3.up);
                HasNormals = true;
                RecalculateNormalsInPlace();
            }

            if (!HasUv0)
            {
                Uv0.Clear();
                for (int i = 0; i < vc; i++) Uv0.Add(Vector2.zero);
                HasUv0 = true;
            }

            if (!HasTangents)
            {
                Tangents.Clear();
                for (int i = 0; i < vc; i++) Tangents.Add(new Vector4(1f, 0f, 0f, 1f));
                HasTangents = true;
            }
        }

        private void RecalculateNormalsInPlace()
        {
            var acc = new Vector3[Positions.Count];
            for (int s = 0; s < Submeshes.Count; s++)
            {
                List<int> t = Submeshes[s];
                for (int i = 0; i + 2 < t.Count; i += 3)
                {
                    Vector3 n = Vector3.Cross(Positions[t[i + 1]] - Positions[t[i]], Positions[t[i + 2]] - Positions[t[i]]);
                    acc[t[i]] += n;
                    acc[t[i + 1]] += n;
                    acc[t[i + 2]] += n;
                }
            }

            for (int i = 0; i < acc.Length; i++)
                Normals[i] = acc[i].sqrMagnitude > 1e-12f ? acc[i].normalized : Vector3.up;
        }

        /// <summary>Drop vertices no triangle uses. Returns removed count.</summary>
        public int Compact()
        {
            int vc = Positions.Count;
            var remap = new int[vc];
            for (int i = 0; i < vc; i++) remap[i] = -1;
            int next = 0;
            for (int s = 0; s < Submeshes.Count; s++)
            {
                List<int> t = Submeshes[s];
                for (int i = 0; i < t.Count; i++)
                {
                    int v = t[i];
                    if (remap[v] < 0)
                        remap[v] = next++;
                }
            }

            int removed = vc - next;
            if (removed <= 0)
                return 0;

            var order = new int[next];
            for (int i = 0; i < vc; i++)
            {
                if (remap[i] >= 0)
                    order[remap[i]] = i;
            }

            Reorder(Positions, order);
            if (HasNormals) Reorder(Normals, order);
            if (HasTangents) Reorder(Tangents, order);
            if (HasUv0) Reorder(Uv0, order);
            if (HasUv1) Reorder(Uv1, order);
            if (HasColors) Reorder(Colors, order);
            for (int e = 0; e < ExtraUvCount; e++)
                if (extraUv[e] != null) Reorder(extraUv[e], order);
            for (int s = 0; s < Submeshes.Count; s++)
            {
                List<int> t = Submeshes[s];
                for (int i = 0; i < t.Count; i++)
                    t[i] = remap[t[i]];
            }

            return removed;
        }

        public int CountUsedVertices()
        {
            var used = new bool[Positions.Count];
            int n = 0;
            for (int s = 0; s < Submeshes.Count; s++)
            {
                List<int> t = Submeshes[s];
                for (int i = 0; i < t.Count; i++)
                {
                    if (!used[t[i]])
                    {
                        used[t[i]] = true;
                        n++;
                    }
                }
            }

            return n;
        }

        private static void Reorder<T>(List<T> list, int[] order)
        {
            var copy = new List<T>(order.Length);
            for (int i = 0; i < order.Length; i++)
                copy.Add(list[order[i]]);
            list.Clear();
            list.AddRange(copy);
        }
    }
}
