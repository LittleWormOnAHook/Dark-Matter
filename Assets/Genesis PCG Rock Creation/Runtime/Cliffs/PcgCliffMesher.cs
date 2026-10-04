using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GenesisPCG.RockCreation
{
    /// <summary>Combines placed kit pieces into one mesh per chunk, one submesh per source material (pack textures kept).</summary>
    public static class PcgCliffMesher
    {
        public static Mesh Combine(DmRockKit kit, List<PcgCliffPart> parts, Matrix4x4 post, string name, out Material[] materials)
        {
            var mats = new List<Material>();
            var tris = new List<List<int>>();
            var v = new List<Vector3>();
            var nr = new List<Vector3>();
            var tg = new List<Vector4>();
            var uv = new List<Vector2>();
            foreach (PcgCliffPart part in parts)
            {
                if (part.piece < 0 || part.piece >= kit.pieces.Count) continue;
                DmRockPieceInfo info = kit.pieces[part.piece];
                Mesh src = DmRockPieceAnalyzer.PieceMesh(info?.prefab, out Material[] pm);
                if (src == null) continue;
                PcgMeshOps.Data d = PcgMeshOps.Get(src);
                if (d == null || d.v == null || d.tris == null) continue;
                Matrix4x4 M = post * part.matrix;
                Matrix4x4 N = M.inverse.transpose;
                int cnt = d.v.Length, start = v.Count;
                bool hasN = d.n != null && d.n.Length == cnt, hasT = d.t != null && d.t.Length == cnt, hasUv = d.uv != null && d.uv.Length == cnt;
                for (int i = 0; i < cnt; i++)
                {
                    v.Add(M.MultiplyPoint3x4(d.v[i]));
                    nr.Add(hasN ? N.MultiplyVector(d.n[i]).normalized : Vector3.up);
                    if (hasT)
                    {
                        Vector4 t = d.t[i];
                        Vector3 td = M.MultiplyVector(new Vector3(t.x, t.y, t.z)).normalized;
                        tg.Add(new Vector4(td.x, td.y, td.z, t.w < 0f ? -1f : 1f));
                    }
                    else tg.Add(new Vector4(1f, 0f, 0f, 1f));
                    uv.Add(hasUv ? d.uv[i] : Vector2.zero);
                }
                for (int sm = 0; sm < d.tris.Length; sm++)
                {
                    int[] src3 = d.tris[sm];
                    if (src3 == null || src3.Length == 0) continue;
                    Material m = pm != null && pm.Length > 0 ? pm[Mathf.Min(sm, pm.Length - 1)] : null;
                    int idx = mats.IndexOf(m);
                    if (idx < 0) { idx = mats.Count; mats.Add(m); tris.Add(new List<int>()); }
                    List<int> dst = tris[idx];
                    for (int k = 0; k < src3.Length; k++) dst.Add(src3[k] + start);
                }
            }
            materials = mats.ToArray();
            if (v.Count == 0) return null;
            var mesh = new Mesh { name = name, indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(v);
            mesh.SetNormals(nr);
            mesh.SetTangents(tg);
            mesh.SetUVs(0, uv);
            mesh.subMeshCount = tris.Count;
            for (int i = 0; i < tris.Count; i++) mesh.SetTriangles(tris[i], i, false);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
