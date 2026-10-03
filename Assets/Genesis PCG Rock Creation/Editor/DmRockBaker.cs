#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GenesisPCG.RockCreation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace GenesisPCG.RockCreation.Editor
{
    /// <summary>
    /// Bakes a combiner's built (seeded + conformed + masked) mesh into a saved asset:
    /// hidden-geometry cull -> one atlas sub-mesh (UVs remapped into the shared kit atlas) -> LODs -> collider.
    /// Input is whatever the combiner built (GeneratedMesh + SubmeshSources), so it does not depend on the placement logic.
    /// One .asset per rock (keyed by the rock's placement stamp), overwritten in place on every rebake.
    /// </summary>
    public static class DmRockBaker
    {
        public const string BakedRoot = "Assets/Genesis PCG Rock Creation/Generated/Baked";
        private const float UvTolerance = 0.01f;

        /// <summary>True while a bake runs (the scheduler ignores the bake's own preview rebuild).</summary>
        public static bool Busy { get; private set; }

        public sealed class Report
        {
            public string rock, assetPath, atlasKey, lodBackend, note;
            public bool atlasCreated, overwritten, isArray;
            public string sizeNote;
            public int sourceTris, groundCulled, groundClipped, groundCutVerts, interiorCulled, fragmentCulled, wrappedSubmeshes, closedParts, openParts;
            public int[] lodVerts = new int[0], lodTris = new int[0];
            public float[] lodQuality = new float[0];
            public double ms, atlasMs, cullMs, lodMs, saveMs;
            public override string ToString() =>
                $"{rock}: {assetPath} | tris src {sourceTris} -ground {groundCulled} (clipped {groundClipped}, +{groundCutVerts} cut verts) -interior {interiorCulled} -fragments {fragmentCulled} | " +
                string.Join(" ", lodTris.Select((t, i) => $"LOD{i} {lodVerts[i]}v/{t}t")) +
                $" | {(isArray ? "arrays" : "atlas")} {atlasKey}{(atlasCreated ? " (new)" : "")}{(string.IsNullOrEmpty(sizeNote) ? "" : " [" + sizeNote + "]")} | wrapped {wrappedSubmeshes} | closed parts {closedParts}/{closedParts + openParts}" +
                $" | {ms:F0} ms (atlas {atlasMs:F0}, cull {cullMs:F0}, lod {lodMs:F0}, save {saveMs:F0}){(string.IsNullOrEmpty(note) ? "" : " | " + note)}";
        }

        public static Report LastReport { get; private set; }

        // ------------------------------------------------------------------------------------------------

        public static bool Bake(DmRockCombiner c, bool force, out Report report)
        {
            report = new Report { rock = c != null ? c.name : "null" };
            if (c == null || EditorUtility.IsPersistent(c) || !c.isActiveAndEnabled)
                return false;
            // Terrain match on and no blend (or a copy still pointing at the original's per-rock blend): give the rock its own
            // blend first, so the content stamp / settings hash below already include it.
            Material shownFrom = c.BlendMaterial;
            if (DmRockBlendMaterials.NeedsOwnBlend(c))
            {
                DmRockBlendMaterials.EnsureOwnBlend(c);
                force = true;
            }
            // Values edited on the materials the rock showed (its bake variants: this bake regenerates them) stay the rock's.
            if (DmRockBlendMaterials.KeepShownEdits(c, c.IsBaked ? c.BakeData.materials : c.LastBakedMaterials, shownFrom, null))
                force = true;
            string staleWhy = c.IsBakeCurrent ? StaleReason(c) : null;
            if (!force && c.IsBakeCurrent && staleWhy == null)
            {
                report.assetPath = AssetDatabase.GetAssetPath(c.BakeData.mesh);
                report.note = "already current";
                return true;
            }
            if (staleWhy != null) report.note = "out of date: " + staleWhy;

            var sw = Stopwatch.StartNew();
            Busy = true;
            try
            {
                string oldPath = c.IsBaked && c.BakeData.mesh != null ? AssetDatabase.GetAssetPath(c.BakeData.mesh) : null;

                // 1) Fresh live build for the current pose (a layout assembled at another pose sits on the wrong
                //    ground; the build is deterministic from the seed, so rebuilding is always safe).
                string stamp = ContentStamp(c);
                c.Rebuild();
                Mesh src = c.GeneratedMesh;
                if (src == null || src.vertexCount == 0 || c.SubmeshSources.Count != src.subMeshCount)
                {
                    report.note = "nothing built";
                    return false;
                }
                DmRockBakeSettings bs = c.BakeSettings;

                // 2) Shared atlas (whole kit material set, so every rock of the kit shares it).
                var tA = sw.Elapsed.TotalMilliseconds;
                DmRockAtlas atlas = EnsureAtlas(c, out bool created);
                report.atlasCreated = created;
                report.atlasKey = atlas != null ? atlas.key : "-";
                report.isArray = atlas != null && atlas.isArray;
                report.sizeNote = atlas != null ? atlas.sizeNote : null;
                report.atlasMs = sw.Elapsed.TotalMilliseconds - tA;

                // 3) Hidden-geometry cull + merge into one atlas sub-mesh.
                var tC = sw.Elapsed.TotalMilliseconds;
                Mesh lod0 = BuildMergedMesh(c, src, atlas, bs, report, out Material[] materials);
                if (bs.lod0Quality < 0.999f && lod0 != null && lod0.vertexCount > 0)
                {
                    // Cheaper LOD0 (and so the whole chain): simplify the merged mesh before LODs are made from it.
                    Mesh full = lod0;
                    lod0 = DmRockLodSimplifier.Simplify(full, Mathf.Clamp(bs.lod0Quality, 0.1f, 1f), report.groundClipped > 0);
                    lod0.name = full.name;
                    UnityEngine.Object.DestroyImmediate(full);
                }
                materials = DmRockBlendMaterials.Apply(c, materials, out string blendNote);
                if (blendNote != null) report.note = string.IsNullOrEmpty(report.note) ? blendNote : report.note + "; " + blendNote;
                report.cullMs = sw.Elapsed.TotalMilliseconds - tC;

                // 4) LODs.
                var tL = sw.Elapsed.TotalMilliseconds;
                var lodMeshes = new List<Mesh>();
                float[] heights = bs.lodTransitions != null && bs.lodTransitions.Length > 0 ? bs.lodTransitions : new[] { 0.6f, 0.45f, 0.25f, 0.13f };
                Mesh collider;
                if (bs.generateLods && heights.Length > 1)
                {
                    float[] q = DmRockLodSimplifier.Qualities(heights.Length);
                    // flush-cut rocks: keep open edges (the rim on the terrain) in place in every LOD
                    bool keepRim = report.groundClipped > 0;
                    report.lodQuality = q;
                    for (int i = 1; i < heights.Length; i++)
                    {
                        Mesh m = DmRockLodSimplifier.Simplify(lod0, q[i], keepRim);
                        m.name = "LOD" + i;
                        lodMeshes.Add(m);
                    }
                    report.lodBackend = DmRockLodSimplifier.Backend;
                    collider = lodMeshes[Mathf.Min(1, lodMeshes.Count - 1)]; // LOD2
                }
                else
                {
                    heights = new[] { heights.Length > 0 ? heights[heights.Length - 1] : 0.01f };
                    collider = DmRockLodSimplifier.Simplify(lod0, 0.4225f, report.groundClipped > 0);
                    collider.name = "Collider";
                    report.lodBackend = "none (" + DmRockLodSimplifier.Backend + " for the collider)";
                }
                report.lodMs = sw.Elapsed.TotalMilliseconds - tL;

                // 5) Save into the rock's own asset (overwrite in place).
                var tS = sw.Elapsed.TotalMilliseconds;
                string path = AssetPathFor(c);
                var subs = new List<Mesh>(lodMeshes);
                if (!lodMeshes.Contains(collider)) subs.Add(collider);
                lod0.name = Path.GetFileNameWithoutExtension(path);
                // SaveAsset may copy into the existing sub-assets and destroy these temporaries: keep only names.
                string[] lodNames = lodMeshes.Select(m => m.name).ToArray();
                string colliderName = collider.name;
                // One asset-pipeline refresh per save (each refresh runs every project postprocessor, ~1.5 s here).
                bool overwritten;
                AssetDatabase.StartAssetEditing();
                try { SaveAsset(path, ref lod0, subs, out overwritten); }
                finally { AssetDatabase.StopAssetEditing(); }
                report.overwritten = overwritten;
                report.assetPath = path;
                Mesh[] savedLods = lodNames.Select(n => subs.First(x => x.name == n)).ToArray();
                Mesh savedCollider = subs.First(x => x.name == colliderName);

                c.ApplyBake(lod0, savedLods, heights, savedCollider, materials, atlas, bs.crossFade, stamp);
                DmRockBlendMaterials.NoteBaked(c);
                report.saveMs = sw.Elapsed.TotalMilliseconds - tS;

                if (!string.IsNullOrEmpty(oldPath) && oldPath != path)
                    DeleteIfUnreferenced(oldPath, null);

                int n = 1 + savedLods.Length;
                report.lodVerts = new int[n];
                report.lodTris = new int[n];
                report.lodVerts[0] = lod0.vertexCount; report.lodTris[0] = TriCount(lod0);
                for (int i = 0; i < savedLods.Length; i++) { report.lodVerts[i + 1] = savedLods[i].vertexCount; report.lodTris[i + 1] = TriCount(savedLods[i]); }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e, c);
                report.note = "failed: " + e.Message;
                return false;
            }
            finally
            {
                Busy = false;
                report.ms = sw.Elapsed.TotalMilliseconds;
                LastReport = report;
            }
        }

        private static List<Material> AtlasMaterials(DmRockCombiner c)
        {
            var matSet = new List<Material>(KitMaterials(c.Kit));
            foreach (DmRockCombiner.SubmeshSource s in c.SubmeshSources)
                if (s.sourceMaterial != null) matSet.Add(s.sourceMaterial);
            return matSet;
        }

        /// <summary>The shared atlas this rock bakes into, built on first use (must not run inside StartAssetEditing).</summary>
        public static DmRockAtlas EnsureAtlas(DmRockCombiner c, out bool created)
        {
            created = false;
            // Material override: one material on the meshes' own UVs (no atlas, never world projection).
            if (c.MaterialOverride != null) return null;
            DmRockBakeSettings bs = c.BakeSettings;
            if (bs.textureMode == DmRockTextureMode.TextureArray)
                return DmRockArrayBuilder.GetOrCreate(AtlasMaterials(c), bs.arraySliceSize, bs.arrayHalfResMask, out created);
            return DmRockAtlasBuilder.GetOrCreate(AtlasMaterials(c), (int)bs.atlasSize, bs.atlasPadding, c.MaterialOverride, out created);
        }

        /// <summary>True when the rock's shared atlas already exists (baking then only writes the rock's own .asset).</summary>
        public static bool AtlasReady(DmRockCombiner c)
        {
            if (c == null || (c.IsBakeCurrent && StaleReason(c) == null) || c.MaterialOverride != null) return true; // Bake() will not touch an atlas
            if (c.GeneratedMesh == null || c.SubmeshSources.Count == 0)
            {
                // Needs the live build to know its materials (Bake() would rebuild anyway). Busy keeps the
                // resulting un-bake from re-queuing the rock.
                Busy = true;
                try { c.Rebuild(); }
                finally { Busy = false; }
                if (c.GeneratedMesh == null || c.SubmeshSources.Count == 0) return true;
            }
            DmRockBakeSettings bs = c.BakeSettings;
            var mats = AtlasMaterials(c).Where(m => m != null).Distinct().ToList();
            if (mats.Count == 0) return true;
            return Find(bs, MakeKey(bs, mats)) != null;
        }

        public static int TriCount(Mesh m)
        {
            long n = 0;
            for (int s = 0; s < m.subMeshCount; s++) n += m.GetIndexCount(s);
            return (int)(n / 3);
        }

        // ------------------------------------------------------------------------------------------------
        // Content staleness: kit / atlas / style / recipe / layout-version changes mark bakes out of date (they keep
        // showing their saved mesh; Bake All and Bake skip only bakes that are current in both settings and content).

        private static readonly Dictionary<EntityId, KeyValuePair<int, string>> s_objHash = new Dictionary<EntityId, KeyValuePair<int, string>>();

        /// <summary>Stable content hash of an asset (serialized fields; references by GUID + local id). Cached per dirty count.</summary>
        public static string ContentHash(Object o)
        {
            if (o == null) return "0";
            EntityId id = o.GetEntityId();
            int dirty = EditorUtility.GetDirtyCount(o);
            if (s_objHash.TryGetValue(id, out KeyValuePair<int, string> kv) && kv.Key == dirty) return kv.Value;
            var sb = new System.Text.StringBuilder(4096);
            using (var so = new SerializedObject(o))
            {
                SerializedProperty it = so.GetIterator();
                bool enter = true;
                while (it.Next(enter))
                {
                    enter = true;
                    if (it.depth == 0 && (it.name == "m_Script" || it.name == "m_Name")) { enter = false; continue; }
                    switch (it.propertyType)
                    {
                        case SerializedPropertyType.Integer:
                        case SerializedPropertyType.ArraySize:
                        case SerializedPropertyType.LayerMask:
                        case SerializedPropertyType.Enum: sb.Append(it.propertyPath).Append('=').Append(it.longValue).Append(';'); break;
                        case SerializedPropertyType.Boolean: sb.Append(it.propertyPath).Append('=').Append(it.boolValue ? '1' : '0').Append(';'); break;
                        case SerializedPropertyType.Float: sb.Append(it.propertyPath).Append('=').Append(it.doubleValue.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append(';'); break;
                        case SerializedPropertyType.String: sb.Append(it.propertyPath).Append('=').Append(it.stringValue).Append(';'); break;
                        case SerializedPropertyType.ObjectReference:
                            Object r = it.objectReferenceValue;
                            string rid = r != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(r, out string g, out long l) ? g + ":" + l : (r != null ? r.name : "0");
                            sb.Append(it.propertyPath).Append('=').Append(rid).Append(';'); break;
                    }
                }
            }
            string h = Hash128.Compute(sb.ToString()).ToString().Substring(0, 12);
            s_objHash[id] = new KeyValuePair<int, string>(dirty, h);
            return h;
        }

        private static string RefKey(Object o) =>
            o == null ? "0" : (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out string g, out long l) ? g.Substring(0, 12) : o.name);

        /// <summary>Key of the atlas the kit's material set maps to now ("-" with a material override).</summary>
        public static string KitAtlasKey(DmRockCombiner c)
        {
            if (c.MaterialOverride != null) return "-";
            DmRockBakeSettings bs = c.BakeSettings;
            var mats = KitMaterials(c.Kit).Where(m => m != null).Distinct().ToList();
            return mats.Count == 0 ? "-" : MakeKey(bs, mats);
        }

        private static string MakeKey(DmRockBakeSettings bs, List<Material> mats) =>
            bs.textureMode == DmRockTextureMode.TextureArray
                ? DmRockArrayBuilder.MakeKey(mats, bs.arraySliceSize, bs.arrayHalfResMask, null)
                : DmRockAtlasBuilder.MakeKey(mats, (int)bs.atlasSize, Mathf.Max(8, bs.atlasPadding), null);

        private static DmRockAtlas Find(DmRockBakeSettings bs, string key) =>
            bs.textureMode == DmRockTextureMode.TextureArray ? DmRockArrayBuilder.Find(key) : DmRockAtlasBuilder.Find(key);

        /// <summary>"L&lt;layout&gt;|kit:..|atlas:..|style:..|recipe:.." for the rock's current inputs.</summary>
        public static string ContentStamp(DmRockCombiner c)
        {
            return "L" + DmRockCombiner.LayoutVersion + "|kit:" + ContentHash(c.Kit) + "|atlas:" + KitAtlasKey(c) +
                   "|style:" + ContentHash(c.Style) + "|recipe:" + ContentHash(c.Recipe) + "|preset:" + ContentHash(c.Preset) +
                   "|mat:" + RefKey(c.MaterialOverride) + (c.BlendMaterial != null ? "|blend:" + ContentHash(c.BlendMaterial) : "");
        }

        /// <summary>Null when the bake matches the current kit / atlas / style / layout; otherwise what changed.</summary>
        public static string StaleReason(DmRockCombiner c)
        {
            if (c == null || !c.IsBaked || c.BakeData == null) return null;
            DmRockCombiner.BakeState b = c.BakeData;
            if (b.atlas == null && c.MaterialOverride == null) return "atlas missing";
            string now = ContentStamp(c), was = b.contentStamp;
            if (string.IsNullOrEmpty(was)) return "baked before content tracking (older layout / atlas)";
            if (was == now) return null;
            string[] a = was.Split('|'), n = now.Split('|');
            var diff = new List<string>();
            for (int i = 0; i < Mathf.Max(a.Length, n.Length); i++)
            {
                string x = i < a.Length ? a[i] : "", y = i < n.Length ? n[i] : "";
                if (x != y) { int k = y.IndexOf(':'); diff.Add(k > 0 ? y.Substring(0, k) : (i == 0 ? "layout version" : y)); }
            }
            return string.Join(", ", diff) + " changed";
        }

        [MenuItem("Tools/Genesis PCG Rock Creation/Report Out-of-Date Bakes in Scene")]
        public static void ReportStaleMenu()
        {
            var sb = new System.Text.StringBuilder();
            int n = 0, baked = 0;
            foreach (DmRockCombiner c in AllCombiners())
            {
                if (!c.IsBaked) continue;
                baked++;
                string why = c.IsBakeCurrent ? StaleReason(c) : "settings / pose edited";
                if (why == null) continue;
                n++;
                sb.Append("\n  ").Append(c.name).Append(": ").Append(why);
            }
            Debug.Log($"Genesis PCG Rock Creation: {n} of {baked} baked DM PCG Creator(s) out of date (Bake All rebakes them).{sb}");
        }

        /// <summary>Every material reachable from the kit (base + add-on candidates).</summary>
        public static IEnumerable<Material> KitMaterials(DmRockKit kit)
        {
            if (kit == null) yield break;
            var seen = new HashSet<Material>();
            IEnumerable<GameObject> all = (kit.baseCandidates ?? new List<GameObject>()).Concat(kit.addOnCandidates ?? new List<GameObject>());
            foreach (GameObject go in all)
            {
                if (go == null) continue;
                foreach (MeshRenderer r in go.GetComponentsInChildren<MeshRenderer>(true))
                    foreach (Material m in r.sharedMaterials)
                        if (m != null && seen.Add(m))
                            yield return m;
            }
        }

        // ------------------------------------------------------------------------------------------------
        // Merge + cull

        private sealed class Part
        {
            public int id;
            public readonly List<int> instances = new List<int>();
            public Occluder occluder;
        }

        private static Mesh BuildMergedMesh(DmRockCombiner c, Mesh src, DmRockAtlas atlas, DmRockBakeSettings bs, Report report, out Material[] materials)
        {
            Vector3[] v = src.vertices;
            Vector3[] n = src.normals;
            Color32[] col = src.colors32;
            var uv = new List<Vector2>(); src.GetUVs(0, uv);
            bool hasN = n != null && n.Length == v.Length, hasC = col != null && col.Length == v.Length, hasUv = uv.Count == v.Length;
            IReadOnlyList<DmRockCombiner.SubmeshSource> sources = c.SubmeshSources;
            int subCount = src.subMeshCount;
            var tris = new int[subCount][];
            for (int s = 0; s < subCount; s++) { tris[s] = src.GetTriangles(s); report.sourceTris += tris[s].Length / 3; }

            // Parts (one per source mesh instance; a multi-sub-mesh source is one part).
            var partOf = new int[subCount];
            var parts = new List<Part>();
            for (int s = 0; s < subCount; s++)
            {
                DmRockCombiner.SubmeshSource ss = sources[s];
                bool cont = s > 0 && ss.sourceSubmesh > 0 && sources[s - 1].sourceMesh == ss.sourceMesh && sources[s - 1].pieceIndex == ss.pieceIndex
                            && sources[s - 1].sourceSubmesh == ss.sourceSubmesh - 1;
                if (!cont) parts.Add(new Part { id = parts.Count });
                parts[parts.Count - 1].instances.Add(s);
                partOf[s] = parts.Count - 1;
            }

            var keep = new bool[subCount][];
            for (int s = 0; s < subCount; s++) { keep[s] = new bool[tris[s].Length / 3]; for (int i = 0; i < keep[s].Length; i++) keep[s][i] = true; }

            // a) Below ground, legacy: drop whole triangles whose three vertices are buried (jagged bottom edge).
            Transform rt = c.transform;
            PcgSurfaceSnapSettings rsnap = c.SnapSettings;
            int gmask = rsnap.surfaceLayerMask;
            if (rsnap.excludeOwnLayer) gmask &= ~(1 << c.gameObject.layer);
            var gignore = new List<Transform> { rt };
            float gheight = Mathf.Max(0.5f, src.bounds.size.y * Mathf.Abs(rt.lossyScale.y)) + 1f;
            bool flush = bs.cullBelowGround && bs.groundCut == DmRockGroundCut.FlushClip;
            if (bs.cullBelowGround && bs.groundCut == DmRockGroundCut.CullWholeTriangles)
            {
                Transform t = c.transform;
                PcgSurfaceSnapSettings snap = c.SnapSettings;
                int mask = snap.surfaceLayerMask;
                if (snap.excludeOwnLayer) mask &= ~(1 << c.gameObject.layer);
                var ignore = new List<Transform> { t };
                Physics.SyncTransforms();
                Matrix4x4 l2w = t.localToWorldMatrix;
                float height = Mathf.Max(0.5f, src.bounds.size.y * Mathf.Abs(t.lossyScale.y)) + 1f;
                var buried = new sbyte[v.Length]; // 0 unknown, 1 buried, -1 not
                for (int s = 0; s < subCount; s++)
                {
                    int[] tr = tris[s];
                    for (int i = 0, k = 0; i + 2 < tr.Length; i += 3, k++)
                    {
                        if (!Buried(tr[i]) || !Buried(tr[i + 1]) || !Buried(tr[i + 2])) continue;
                        keep[s][k] = false;
                        report.groundCulled++;
                    }
                }

                bool Buried(int vi)
                {
                    if (buried[vi] == 0)
                    {
                        Vector3 wp = l2w.MultiplyPoint3x4(v[vi]);
                        bool b = PcgSurfaceSnap.SampleGroundY(wp, height, 0.01f, mask, snap, ignore, out float gy) && gy - wp.y > bs.cullBelowGroundMargin;
                        buried[vi] = (sbyte)(b ? 1 : -1);
                    }
                    return buried[vi] > 0;
                }
            }

            // b) Inside other (closed) parts of the same rock.
            if (bs.cullInterior && parts.Count > 1)
            {
                foreach (Part p in parts)
                {
                    var pt = new List<int>();
                    foreach (int s in p.instances) pt.AddRange(tris[s]);
                    p.occluder = Occluder.Build(v, pt);
                    if (p.occluder != null) report.closedParts++; else report.openParts++;
                }
                float inset = bs.interiorInset;
                var occluders = parts.Where(p => p.occluder != null).ToList();
                if (occluders.Count > 0)
                {
                    foreach (Part a in parts)
                    {
                        var others = occluders.Where(o => o != a).Take(64).ToList();
                        if (others.Count == 0) continue;
                        var vmask = new Dictionary<int, ulong>();
                        foreach (int s in a.instances)
                        {
                            int[] tr = tris[s];
                            for (int i = 0, k = 0; i + 2 < tr.Length; i += 3, k++)
                            {
                                ulong m = Mask(tr[i]);
                                if (m == 0) continue;
                                m &= Mask(tr[i + 1]);
                                if (m == 0) continue;
                                m &= Mask(tr[i + 2]);
                                if (m == 0) continue;
                                Vector3 p0 = v[tr[i]], p1 = v[tr[i + 1]], p2 = v[tr[i + 2]];
                                Vector3 cen = (p0 + p1 + p2) / 3f;
                                Vector3 fn = Vector3.Cross(p1 - p0, p2 - p0).normalized;
                                ulong cm = PointMask(cen + fn * inset);
                                if ((m & cm) == 0) continue;
                                if (keep[s][k]) { keep[s][k] = false; report.interiorCulled++; }
                            }
                        }

                        ulong Mask(int vi)
                        {
                            if (vmask.TryGetValue(vi, out ulong m)) return m;
                            Vector3 p = v[vi] + (hasN ? n[vi] * inset : Vector3.zero);
                            m = PointMask(p);
                            vmask[vi] = m;
                            return m;
                        }

                        ulong PointMask(Vector3 p)
                        {
                            ulong m = 0;
                            for (int o = 0; o < others.Count; o++)
                                if (others[o].occluder.Inside(p)) m |= 1UL << o;
                            return m;
                        }
                    }
                }
            }

            // a2) Flush ground cut: clip every triangle against the ground surface (+ offset) so the rock ends exactly at
            // the terrain instead of on a jagged triangle line. New vertices on the cut interpolate position / normal / UV /
            // colour and get "on the ground" colours (R 1, A 0 = height above ground 0); vertices within 3 mm of the cut
            // are treated as on it. Triangles fully below are dropped.
            if (flush)
            {
                Physics.SyncTransforms();
                ClipToGround(c, ref v, ref n, ref col, uv, hasN, hasC, hasUv, tris, keep, bs, gheight, gmask, gignore, report);
            }

            // b2) Cull remnants: culling a piece's hidden middle (inside a neighbour / below ground) can leave a tiny visible
            // remnant cut off from the rest of the piece (reads as a chip floating next to the occluding piece). Per part with
            // culled triangles: drop kept components (welded by position) under 4% of the part's largest kept component's
            // vertices AND under 20% of its size.
            if (bs.cullInterior || (bs.cullBelowGround && bs.groundCut != DmRockGroundCut.Off))
            {
                var par = new int[v.Length];
                for (int i = 0; i < par.Length; i++) par[i] = i;
                int Find(int x) { while (par[x] != x) { par[x] = par[par[x]]; x = par[x]; } return x; }
                var weld = new Dictionary<(int, Vector3Int), int>();
                foreach (Part p in parts)
                    foreach (int s in p.instances)
                    {
                        int[] tr = tris[s];
                        for (int i = 0, k = 0; i + 2 < tr.Length; i += 3, k++)
                        {
                            if (!keep[s][k]) continue;
                            for (int j = 0; j < 3; j++)
                            {
                                int vi = tr[i + j];
                                var key = (p.id, Vector3Int.RoundToInt(v[vi] * 1000f));
                                if (weld.TryGetValue(key, out int w)) par[Find(vi)] = Find(w); else weld[key] = vi;
                            }
                            int a0 = Find(tr[i]);
                            par[Find(tr[i + 1])] = a0;
                            par[Find(tr[i + 2])] = a0;
                        }
                    }
                foreach (Part p in parts)
                {
                    bool culled = false;
                    foreach (int s in p.instances) { foreach (bool kk in keep[s]) if (!kk) { culled = true; break; } if (culled) break; }
                    if (!culled) continue;
                    var cnt = new Dictionary<int, int>(); var bnd = new Dictionary<int, Bounds>(); var seen = new HashSet<int>();
                    foreach (int s in p.instances)
                    {
                        int[] tr = tris[s];
                        for (int i = 0, k = 0; i + 2 < tr.Length; i += 3, k++)
                        {
                            if (!keep[s][k]) continue;
                            for (int j = 0; j < 3; j++)
                            {
                                int vi = tr[i + j];
                                if (!seen.Add(vi)) continue;
                                int r = Find(vi);
                                cnt[r] = cnt.TryGetValue(r, out int q) ? q + 1 : 1;
                                if (bnd.TryGetValue(r, out Bounds bb)) { bb.Encapsulate(v[vi]); bnd[r] = bb; } else bnd[r] = new Bounds(v[vi], Vector3.zero);
                            }
                        }
                    }
                    if (cnt.Count < 2) continue;
                    int big = -1, bigN = 0;
                    foreach (var kv in cnt) if (kv.Value > bigN) { bigN = kv.Value; big = kv.Key; }
                    float bigSize = bnd[big].size.magnitude;
                    var drop = new HashSet<int>();
                    foreach (var kv in cnt)
                        if (kv.Key != big && kv.Value < bigN * 0.04f && bnd[kv.Key].size.magnitude < bigSize * 0.2f) drop.Add(kv.Key);
                    if (drop.Count == 0) continue;
                    foreach (int s in p.instances)
                    {
                        int[] tr = tris[s];
                        for (int i = 0, k = 0; i + 2 < tr.Length; i += 3, k++)
                            if (keep[s][k] && drop.Contains(Find(tr[i]))) { keep[s][k] = false; report.fragmentCulled++; }
                    }
                }
            }

            // c) Merge: sub-mesh 0 = atlas (UVs remapped), then one sub-mesh per fallback material (wrapped UVs / not in atlas).
            Material atlasMat = atlas != null ? atlas.material : null;
            var groups = new List<(Material mat, List<int> idx)>();
            var groupOf = new Dictionary<Material, int>();
            if (atlasMat != null) { groups.Add((atlasMat, new List<int>())); }
            var newIndex = new Dictionary<int, int>();
            var outV = new List<Vector3>(); var outN = new List<Vector3>(); var outC = new List<Color32>(); var outUv = new List<Vector2>(); var outUv3 = new List<Vector2>();
            Material overrideMat = c.MaterialOverride;
            int nullGroup = -1;
            for (int s = 0; s < subCount; s++)
            {
                DmRockCombiner.SubmeshSource ss = sources[s];
                int[] tr = tris[s];
                bool inAtlas = atlas != null && ss.sourceMaterial != null && atlas.TryGetRect(ss.sourceMaterial, out _);
                Rect rect = default;
                bool arr = inAtlas && atlas.isArray;
                Vector2 st = Vector2.one, so = Vector2.zero;
                var slice = new Vector2(0f, 0f);
                if (arr)
                {
                    // Texture array: keep the material's own UVs (native tiling), bake its tiling / offset into UV0 and
                    // put the slice (x) and resolution group (y, 1 = the 4K array) into UV3.
                    atlas.TryGetSlice(ss.sourceMaterial, out int sg, out int sl);
                    slice = new Vector2(sl, sg);
                    string tp = ss.sourceMaterial.HasProperty("_BaseColorMap") ? "_BaseColorMap" : (ss.sourceMaterial.HasProperty("_MainTex") ? "_MainTex" : null);
                    if (tp != null) { st = ss.sourceMaterial.GetTextureScale(tp); so = ss.sourceMaterial.GetTextureOffset(tp); }
                }
                else if (inAtlas) atlas.TryGetRect(ss.sourceMaterial, out rect);
                if (inAtlas && !arr && hasUv && Wrapped(tr, uv))
                {
                    inAtlas = false;
                    report.wrappedSubmeshes++;
                }
                int g;
                if (inAtlas) g = 0;
                else
                {
                    Material fm = overrideMat != null ? overrideMat : ss.sourceMaterial;
                    if (fm == null) fm = atlasMat;
                    if (fm == null)
                    {
                        if (nullGroup < 0) { nullGroup = groups.Count; groups.Add((null, new List<int>())); }
                        g = nullGroup;
                    }
                    else if (!groupOf.TryGetValue(fm, out g)) { g = groups.Count; groups.Add((fm, new List<int>())); groupOf[fm] = g; }
                }
                List<int> idx = groups[g].idx;
                for (int i = 0, k = 0; i + 2 < tr.Length; i += 3, k++)
                {
                    if (!keep[s][k]) continue;
                    for (int j = 0; j < 3; j++)
                    {
                        int vi = tr[i + j];
                        if (!newIndex.TryGetValue(vi, out int ni))
                        {
                            ni = outV.Count;
                            newIndex[vi] = ni;
                            outV.Add(v[vi]);
                            if (hasN) outN.Add(n[vi]);
                            if (hasC) outC.Add(col[vi]);
                            if (hasUv)
                            {
                                Vector2 u = uv[vi];
                                if (arr) u = Vector2.Scale(u, st) + so;
                                else if (inAtlas) u = new Vector2(rect.x + Mathf.Clamp01(u.x) * rect.width, rect.y + Mathf.Clamp01(u.y) * rect.height);
                                outUv.Add(u);
                            }
                            outUv3.Add(inAtlas ? slice : Vector2.zero);
                        }
                        idx.Add(ni);
                    }
                }
            }
            if (atlasMat != null && groups[0].idx.Count == 0 && groups.Count > 1) groups.RemoveAt(0);

            var mesh = new Mesh { indexFormat = outV.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(outV);
            if (hasN) mesh.SetNormals(outN);
            if (hasUv) mesh.SetUVs(0, outUv);
            if (hasC) mesh.SetColors(outC);
            if (atlas != null && atlas.isArray) mesh.SetUVs(3, outUv3);
            mesh.subMeshCount = groups.Count;
            for (int g = 0; g < groups.Count; g++) mesh.SetTriangles(groups[g].idx, g, false);
            if (!hasN) mesh.RecalculateNormals();
            if (hasUv) mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            materials = groups.Select(x => x.mat).ToArray();
            return mesh;
        }

        /// <summary>
        /// Flush ground cut (Sutherland-Hodgman per triangle against "above ground + offset"). d = y - (groundY + offset) per
        /// vertex in world space (terrain height or the highest snap-mask collider under it, so multiple / sloped terrains and
        /// meshes work); the crossing on each edge is refined by regula falsi against the real ground (3 re-samples) and
        /// snapped onto it, and cached per edge (keyed by endpoint positions) so neighbouring triangles share the cut vertex.
        /// </summary>
        private static void ClipToGround(DmRockCombiner c, ref Vector3[] vRef, ref Vector3[] nRef, ref Color32[] colRef, List<Vector2> uv,
            bool hasN, bool hasC, bool hasUv, int[][] tris, bool[][] keep, DmRockBakeSettings bs, float height, int mask, List<Transform> ignore, Report report)
        {
            Vector3[] v = vRef, n = nRef;
            Color32[] col = colRef;
            const float snapEps = 0.003f;
            Transform t = c.transform;
            Matrix4x4 l2w = t.localToWorldMatrix, w2l = t.worldToLocalMatrix;
            PcgSurfaceSnapSettings snap = c.SnapSettings;
            float off = bs.groundCutOffset;
            var vL = new List<Vector3>(v);
            var nL = hasN ? new List<Vector3>(n) : null;
            var cL = hasC ? new List<Color32>(col) : null;
            var d = new float[v.Length];
            var known = new bool[v.Length];
            var onCut = new HashSet<int>();
            var edgeCache = new Dictionary<(Vector3, Vector3), int>();

            float Ground(Vector3 wp)
            {
                // Exact sample (no grid caching: the cut must follow the terrain heightmap, not a 25 cm approximation).
                return PcgSurfaceSnap.SampleGroundY(wp, height, 3f, mask, snap, ignore, out float gy) ? gy : float.NaN;
            }

            float D(int vi)
            {
                if (known[vi]) return d[vi];
                known[vi] = true;
                Vector3 wp = l2w.MultiplyPoint3x4(v[vi]);
                float gy = Ground(wp);
                float dv = float.IsNaN(gy) ? 1000f : wp.y - (gy + off); // no ground under it: above
                if (Mathf.Abs(dv) < snapEps) { dv = 0f; onCut.Add(vi); }
                d[vi] = dv;
                return dv;
            }

            bool Less(Vector3 a, Vector3 b) => a.x != b.x ? a.x < b.x : (a.y != b.y ? a.y < b.y : a.z < b.z);

            int Cut(int ia, int ib)
            {
                // deterministic orientation (by position) so seam duplicates get identical cut points
                if (Less(v[ib], v[ia])) { int tmp = ia; ia = ib; ib = tmp; }
                var key = (v[ia], v[ib]);
                if (edgeCache.TryGetValue(key, out int ni)) return CopyAttrsIfSeam(ni, ia, ib);
                float da = d[ia], db = d[ib];
                Vector3 pa = l2w.MultiplyPoint3x4(v[ia]), pb = l2w.MultiplyPoint3x4(v[ib]);
                float t0 = 0f, t1 = 1f, d0 = da, d1 = db, tt = da / (da - db);
                Vector3 wp = Vector3.Lerp(pa, pb, tt);
                float gy = float.NaN;
                for (int it = 0; it < 3; it++)
                {
                    wp = Vector3.Lerp(pa, pb, tt);
                    float g = Ground(wp);
                    if (float.IsNaN(g)) break;
                    gy = g;
                    float dt = wp.y - (g + off);
                    if (Mathf.Abs(dt) < 0.001f) break;
                    if ((dt > 0f) == (d0 > 0f)) { t0 = tt; d0 = dt; } else { t1 = tt; d1 = dt; }
                    if (Mathf.Abs(d0 - d1) < 1e-6f) break;
                    tt = t0 + (t1 - t0) * (d0 / (d0 - d1));
                    tt = Mathf.Clamp(tt, Mathf.Min(t0, t1), Mathf.Max(t0, t1));
                }
                wp = Vector3.Lerp(pa, pb, tt);
                if (!float.IsNaN(gy)) { float g = Ground(wp); if (!float.IsNaN(g)) wp.y = g + off; }
                ni = vL.Count;
                vL.Add(w2l.MultiplyPoint3x4(wp));
                if (hasN) nL.Add(Vector3.Normalize(Vector3.Lerp(n[ia], n[ib], tt)));
                if (hasUv) uv.Add(Vector2.Lerp(uv[ia], uv[ib], tt));
                if (hasC)
                {
                    Color32 cc = Color32.Lerp(col[ia], col[ib], tt);
                    cc.r = 255; cc.a = 0;
                    cL.Add(cc);
                }
                edgeCache[key] = ni;
                s_cutSrc[ni] = (ia, ib, tt);
                report.groundCutVerts++;
                return ni;
            }

            // A cached cut vertex reused by a triangle whose edge has the same positions but other vertex indices (UV / normal
            // seam): same position, that triangle's own attributes.
            int CopyAttrsIfSeam(int ni, int ia, int ib)
            {
                (int sa, int sb, float tt) = s_cutSrc[ni];
                if (sa == ia && sb == ib) return ni;
                bool sameAttr = (!hasUv || (uv[sa] == uv[ia] && uv[sb] == uv[ib])) && (!hasN || (n[sa] == n[ia] && n[sb] == n[ib]));
                if (sameAttr) return ni;
                var seamKey = (ni, ia, ib);
                if (s_seam.TryGetValue(seamKey, out int sj)) return sj;
                int nj = vL.Count;
                vL.Add(vL[ni]);
                if (hasN) nL.Add(Vector3.Normalize(Vector3.Lerp(n[ia], n[ib], tt)));
                if (hasUv) uv.Add(Vector2.Lerp(uv[ia], uv[ib], tt));
                if (hasC) cL.Add(cL[ni]);
                s_seam[seamKey] = nj;
                report.groundCutVerts++;
                return nj;
            }

            s_cutSrc.Clear(); s_seam.Clear();
            var poly = new List<int>(6);
            for (int s = 0; s < tris.Length; s++)
            {
                int[] tr = tris[s];
                List<int> extra = null;
                for (int i = 0, k = 0; i + 2 < tr.Length; i += 3, k++)
                {
                    if (!keep[s][k]) continue;
                    int a = tr[i], b = tr[i + 1], e = tr[i + 2];
                    float da = D(a), db = D(b), de = D(e);
                    if (da >= 0f && db >= 0f && de >= 0f && (da > 0f || db > 0f || de > 0f)) continue; // fully above (or touching)
                    keep[s][k] = false;
                    if (da <= 0f && db <= 0f && de <= 0f) { report.groundCulled++; continue; } // fully below / on the cut
                    report.groundClipped++;
                    poly.Clear();
                    int[] tv = { a, b, e };
                    for (int j = 0; j < 3; j++)
                    {
                        int p0 = tv[j], p1 = tv[(j + 1) % 3];
                        float d0 = d[p0], d1 = d[p1];
                        if (d0 >= 0f) poly.Add(p0);
                        if ((d0 > 0f && d1 < 0f) || (d0 < 0f && d1 > 0f)) poly.Add(Cut(p0, p1));
                    }
                    if (poly.Count < 3) continue;
                    if (extra == null) extra = new List<int>();
                    for (int j = 1; j + 1 < poly.Count; j++)
                    {
                        Vector3 q0 = vL[poly[0]], q1 = vL[poly[j]], q2 = vL[poly[j + 1]];
                        if (Vector3.Cross(q1 - q0, q2 - q0).sqrMagnitude < 1e-14f) continue; // degenerate sliver
                        extra.Add(poly[0]); extra.Add(poly[j]); extra.Add(poly[j + 1]);
                    }
                }
                if (extra == null || extra.Count == 0) continue;
                var ntr = new int[tr.Length + extra.Count];
                tr.CopyTo(ntr, 0);
                extra.CopyTo(ntr, tr.Length);
                tris[s] = ntr;
                var nk = new bool[ntr.Length / 3];
                keep[s].CopyTo(nk, 0);
                for (int q = keep[s].Length; q < nk.Length; q++) nk[q] = true;
                keep[s] = nk;
            }
            // vertices sitting on the cut (within 3 mm): mark as on the ground too
            if (hasC)
                foreach (int vi in onCut) { Color32 cc = cL[vi]; cc.r = 255; cc.a = 0; cL[vi] = cc; }
            vRef = vL.ToArray();
            if (hasN) nRef = nL.ToArray();
            if (hasC) colRef = cL.ToArray();
            s_cutSrc.Clear(); s_seam.Clear();
        }

        private static readonly Dictionary<int, (int, int, float)> s_cutSrc = new Dictionary<int, (int, int, float)>();
        private static readonly Dictionary<(int, int, int), int> s_seam = new Dictionary<(int, int, int), int>();

        private static bool Wrapped(int[] tr, List<Vector2> uv)
        {
            foreach (int i in tr)
            {
                Vector2 u = uv[i];
                if (u.x < -UvTolerance || u.y < -UvTolerance || u.x > 1f + UvTolerance || u.y > 1f + UvTolerance) return true;
            }
            return false;
        }

        /// <summary>Closed triangle set with a vertical-ray parity test (XZ grid accelerated, up and down rays must agree).</summary>
        private sealed class Occluder
        {
            private Vector3[] a, b, c;
            private Bounds bounds;
            private float cell;
            private Vector3 origin;
            private readonly Dictionary<long, List<int>> grid = new Dictionary<long, List<int>>();

            public static Occluder Build(Vector3[] v, List<int> tris)
            {
                if (tris.Count < 12) return null;
                // Closed check on welded positions: every edge must have exactly two triangles.
                var weld = new Dictionary<Vector3Int, int>();
                Bounds bb = new Bounds(v[tris[0]], Vector3.zero);
                foreach (int i in tris) bb.Encapsulate(v[i]);
                float q = Mathf.Max(1e-5f, bb.size.magnitude * 1e-5f);
                int Id(int vi)
                {
                    Vector3 p = v[vi] / q;
                    var key = new Vector3Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), Mathf.RoundToInt(p.z));
                    if (!weld.TryGetValue(key, out int id)) { id = weld.Count; weld[key] = id; }
                    return id;
                }
                var edges = new Dictionary<long, int>();
                for (int i = 0; i + 2 < tris.Count; i += 3)
                {
                    int i0 = Id(tris[i]), i1 = Id(tris[i + 1]), i2 = Id(tris[i + 2]);
                    AddEdge(i0, i1); AddEdge(i1, i2); AddEdge(i2, i0);
                }
                void AddEdge(int x, int y)
                {
                    if (x == y) return;
                    long k = x < y ? ((long)x << 32) | (uint)y : ((long)y << 32) | (uint)x;
                    edges.TryGetValue(k, out int cnt);
                    edges[k] = cnt + 1;
                }
                int bad = edges.Values.Count(cnt => cnt != 2);
                if (bad > Mathf.Max(2, edges.Count / 200)) return null; // not closed: never used as an occluder

                var o = new Occluder();
                int tc = tris.Count / 3;
                o.a = new Vector3[tc]; o.b = new Vector3[tc]; o.c = new Vector3[tc];
                o.bounds = bb;
                o.origin = bb.min;
                o.cell = Mathf.Max(1e-3f, Mathf.Max(bb.size.x, bb.size.z) / 48f);
                for (int t = 0; t < tc; t++)
                {
                    Vector3 p0 = v[tris[t * 3]], p1 = v[tris[t * 3 + 1]], p2 = v[tris[t * 3 + 2]];
                    o.a[t] = p0; o.b[t] = p1; o.c[t] = p2;
                    int x0 = o.Cx(Mathf.Min(p0.x, Mathf.Min(p1.x, p2.x))), x1 = o.Cx(Mathf.Max(p0.x, Mathf.Max(p1.x, p2.x)));
                    int z0 = o.Cz(Mathf.Min(p0.z, Mathf.Min(p1.z, p2.z))), z1 = o.Cz(Mathf.Max(p0.z, Mathf.Max(p1.z, p2.z)));
                    for (int x = x0; x <= x1; x++)
                        for (int z = z0; z <= z1; z++)
                        {
                            long key = ((long)x << 32) | (uint)z;
                            if (!o.grid.TryGetValue(key, out List<int> l)) o.grid[key] = l = new List<int>();
                            l.Add(t);
                        }
                }
                return o;
            }

            private int Cx(float x) => Mathf.FloorToInt((x - origin.x) / cell);
            private int Cz(float z) => Mathf.FloorToInt((z - origin.z) / cell);

            public bool Inside(Vector3 p)
            {
                if (!bounds.Contains(p)) return false;
                long key = ((long)Cx(p.x) << 32) | (uint)Cz(p.z);
                if (!grid.TryGetValue(key, out List<int> l)) return false;
                int up = 0, down = 0;
                foreach (int t in l)
                {
                    if (!VerticalHit(a[t], b[t], c[t], p.x, p.z, out float y)) continue;
                    if (y > p.y) up++; else if (y < p.y) down++;
                }
                return (up & 1) == 1 && (down & 1) == 1;
            }

            private static bool VerticalHit(Vector3 p0, Vector3 p1, Vector3 p2, float x, float z, out float y)
            {
                y = 0f;
                float d = (p1.z - p2.z) * (p0.x - p2.x) + (p2.x - p1.x) * (p0.z - p2.z);
                if (Mathf.Abs(d) < 1e-12f) return false;
                float w0 = ((p1.z - p2.z) * (x - p2.x) + (p2.x - p1.x) * (z - p2.z)) / d;
                float w1 = ((p2.z - p0.z) * (x - p2.x) + (p0.x - p2.x) * (z - p2.z)) / d;
                float w2 = 1f - w0 - w1;
                if (w0 <= 0f || w1 <= 0f || w2 <= 0f) return false;
                y = w0 * p0.y + w1 * p1.y + w2 * p2.y;
                return true;
            }
        }

        // ------------------------------------------------------------------------------------------------
        // Assets

        public static string SceneFolder(Scene scene)
        {
            string n = scene.IsValid() && !string.IsNullOrEmpty(scene.name) ? scene.name : "Untitled";
            return BakedRoot + "/" + Safe(n);
        }

        /// <summary>One asset per rock: Baked/&lt;Scene&gt;/&lt;object&gt;_&lt;placement stamp&gt;.asset (stable across reseeds and moves).</summary>
        public static string AssetPathFor(DmRockCombiner c)
        {
            string folder = SceneFolder(c.gameObject.scene);
            string path = $"{folder}/{Safe(c.name)}_{c.PlacementStamp:X16}.asset";
            // Another live rock already owns this file (should not happen: copies get their own stamp).
            foreach (DmRockCombiner o in AllCombiners())
                if (o != c && o.IsBaked && o.BakeData.mesh != null && AssetDatabase.GetAssetPath(o.BakeData.mesh) == path)
                    return $"{folder}/{Safe(c.name)}_{c.PlacementStamp:X16}_{(uint)c.GetEntityId().GetHashCode():X8}.asset";
            return path;
        }

        private static string Safe(string s)
        {
            foreach (char ch in Path.GetInvalidFileNameChars()) s = s.Replace(ch, '_');
            return s.Replace(' ', '_').Replace('(', '_').Replace(')', '_');
        }

        /// <summary>Writes <paramref name="main"/> + <paramref name="subs"/> into one .asset; reuses the existing objects in place.</summary>
        private static void SaveAsset(string path, ref Mesh main, List<Mesh> subs, out bool overwritten)
        {
            DmRockAtlasBuilder.EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            var existing = AssetDatabase.LoadMainAssetAtPath(path) as Mesh;
            overwritten = existing != null;
            if (existing == null)
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) != null) AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(main, path);
                foreach (Mesh m in subs) AssetDatabase.AddObjectToAsset(m, main);
                AssetDatabase.SaveAssetIfDirty(main);
                return;
            }

            CopyInto(main, existing);
            existing.name = main.name;
            Object.DestroyImmediate(main);
            main = existing;
            var old = AssetDatabase.LoadAllAssetRepresentationsAtPath(path).OfType<Mesh>().ToList();
            for (int i = 0; i < subs.Count; i++)
            {
                Mesh o = old.FirstOrDefault(x => x.name == subs[i].name);
                if (o != null)
                {
                    CopyInto(subs[i], o);
                    Object.DestroyImmediate(subs[i]);
                    subs[i] = o;
                    old.Remove(o);
                    EditorUtility.SetDirty(o);
                }
                else
                {
                    AssetDatabase.AddObjectToAsset(subs[i], existing);
                }
            }
            foreach (Mesh o in old)
            {
                AssetDatabase.RemoveObjectFromAsset(o);
                Object.DestroyImmediate(o, true);
            }
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssetIfDirty(existing);
        }

        private static void CopyInto(Mesh src, Mesh dst)
        {
            dst.Clear();
            dst.indexFormat = src.indexFormat;
            dst.SetVertices(src.vertices);
            if (src.normals.Length == src.vertexCount) dst.SetNormals(src.normals);
            if (src.tangents.Length == src.vertexCount) dst.SetTangents(src.tangents);
            Color32[] col = src.colors32;
            if (col != null && col.Length == src.vertexCount) dst.SetColors(col);
            var uv = new List<Vector2>(); src.GetUVs(0, uv);
            if (uv.Count == src.vertexCount) dst.SetUVs(0, uv);
            var uv3 = new List<Vector2>(); src.GetUVs(3, uv3); // texture-array slice / group
            if (uv3.Count == src.vertexCount) dst.SetUVs(3, uv3);
            dst.subMeshCount = src.subMeshCount;
            for (int s = 0; s < src.subMeshCount; s++) dst.SetTriangles(src.GetTriangles(s), s, false);
            dst.bounds = src.bounds;
        }

        public static IEnumerable<DmRockCombiner> AllCombiners()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (!s.isLoaded) continue;
                foreach (GameObject root in s.GetRootGameObjects())
                    foreach (DmRockCombiner c in root.GetComponentsInChildren<DmRockCombiner>(true))
                        yield return c;
            }
        }

        /// <summary>Asset paths under Baked/ that loaded scenes still use (bake state, mesh filters, colliders).</summary>
        public static HashSet<string> ReferencedBakedPaths()
        {
            var set = new HashSet<string>();
            void Add(Object o)
            {
                if (o == null) return;
                string p = AssetDatabase.GetAssetPath(o);
                if (!string.IsNullOrEmpty(p) && p.StartsWith(BakedRoot + "/", StringComparison.Ordinal)) set.Add(p);
            }
            foreach (DmRockCombiner c in AllCombiners())
                if (c.IsBaked) Add(c.BakeData.mesh);
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (!s.isLoaded) continue;
                foreach (GameObject root in s.GetRootGameObjects())
                {
                    foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true)) Add(mf.sharedMesh);
                    foreach (MeshCollider mc in root.GetComponentsInChildren<MeshCollider>(true)) Add(mc.sharedMesh);
                }
            }
            return set;
        }

        public static bool DeleteIfUnreferenced(string path, ICollection<string> keep)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith(BakedRoot + "/", StringComparison.Ordinal)) return false;
            if (keep != null && keep.Contains(path)) return false;
            if (ReferencedBakedPaths().Contains(path)) return false;
            return AssetDatabase.DeleteAsset(path);
        }

        /// <summary>Deletes baked assets in the folders of the loaded scenes that nothing in those scenes uses.</summary>
        public static int CleanUnused(ICollection<string> keep)
        {
            HashSet<string> used = ReferencedBakedPaths();
            int removed = 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (!s.isLoaded) continue;
                string folder = SceneFolder(s);
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { folder }))
                {
                    string p = AssetDatabase.GUIDToAssetPath(guid);
                    if (!p.EndsWith(".asset", StringComparison.Ordinal) || used.Contains(p) || (keep != null && keep.Contains(p))) continue;
                    if (AssetDatabase.DeleteAsset(p)) removed++;
                }
            }
            return removed;
        }

        // ------------------------------------------------------------------------------------------------
        // Menus

        [MenuItem("Tools/Genesis PCG Rock Creation/Bake All DM PCG Creators in Scene")]
        public static void BakeAllMenu()
        {
            int ok = 0, fail = 0;
            var list = AllCombiners().Where(c => c.isActiveAndEnabled).ToList();
            bool editing = false; // saves are batched (16 per asset refresh); new atlases are built outside a batch
            try
            {
                for (int i = 0; i < list.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("Bake DM PCG Creators", list[i].name, (float)i / Mathf.Max(1, list.Count));
                    if (!AtlasReady(list[i]))
                    {
                        if (editing) { AssetDatabase.StopAssetEditing(); editing = false; }
                        EnsureAtlas(list[i], out _);
                    }
                    if (!editing) { AssetDatabase.StartAssetEditing(); editing = true; }
                    if (Bake(list[i], false, out _)) ok++; else fail++;
                    if ((i + 1) % 16 == 0) { AssetDatabase.StopAssetEditing(); editing = false; }
                }
            }
            finally
            {
                if (editing) AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }
            Debug.Log($"Genesis PCG Rock Creation: baked {ok} DM PCG Creator(s){(fail > 0 ? $", {fail} failed" : "")}.");
        }

        [MenuItem("Tools/Genesis PCG Rock Creation/Clean Unused Baked Meshes")]
        public static void CleanMenu()
        {
            int n = CleanUnused(null);
            Debug.Log($"Genesis PCG Rock Creation: removed {n} unused baked mesh asset(s) (folders of the open scenes only).");
        }
    }
}
#endif
