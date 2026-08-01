using System.Collections.Generic;
using Project.Creatures;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Creatures
{
    /// <summary>
    /// Transfers skin weights from Malbers Wolf AC proxy mesh onto a creature visual mesh,
    /// then binds the result to CG/Pelvis bones so AC Animator drives the creature look.
    /// </summary>
    public static class DMICreatureAutoReskin
    {
        public struct ReskinSettings
        {
            public float maxAvgDistance;
            public bool useBarycentricTransfer;

            public static ReskinSettings Default => new ReskinSettings
            {
                maxAvgDistance = 0.28f,
                // Nearest-vertex is fast enough for Meshy meshes; barycentric is O(heavy).
                useBarycentricTransfer = false
            };
        }

        public struct Result
        {
            public Mesh mesh;
            public string meshPath;
            public float avgNearestDistance;
            public float maxNearestDistance;
            public int vertexCount;
            public bool passedQualityGate;
            public string message;
        }

        /// <summary>
        /// Aligns visual to AC Mesh bounds, copies/blends Wolf bone weights onto visual verts,
        /// bakes mesh asset, assigns AC Mesh SMR, destroys temporary visual.
        /// </summary>
        public static Result ReskinVisualToAcTemplate(
            GameObject acRoot,
            GameObject visualSource,
            Material projectMaterial,
            string outputMeshPath,
            ReskinSettings settings)
        {
            var result = new Result { message = "failed" };

            if (acRoot == null || visualSource == null)
            {
                result.message = "AC root or visual source missing.";
                return result;
            }

            Transform meshTransform = FindChildRecursive(acRoot.transform, "Mesh");
            SkinnedMeshRenderer donorSmr = meshTransform != null
                ? meshTransform.GetComponent<SkinnedMeshRenderer>()
                : null;
            Transform pelvis = FindChildRecursive(acRoot.transform, "Pelvis");

            if (donorSmr == null || donorSmr.sharedMesh == null || pelvis == null)
            {
                result.message = "AC template missing Mesh SkinnedMeshRenderer or Pelvis.";
                return result;
            }

            // Capture donor data while Wolf proxy is still active/visible for accurate bounds.
            Mesh donorMesh = donorSmr.sharedMesh;
            Transform[] donorBones = donorSmr.bones;
            if (donorBones == null || donorBones.Length == 0)
            {
                result.message = "Donor Wolf mesh has no bones.";
                return result;
            }

            Bounds templateBounds = CaptureRendererBounds(donorSmr);

            GameObject visualInstance = PrefabUtility.InstantiatePrefab(visualSource) as GameObject;
            if (visualInstance == null)
                visualInstance = Object.Instantiate(visualSource);

            visualInstance.name = "__AutoReskinTemp";
            visualInstance.transform.SetParent(acRoot.transform, false);

            Animator[] strayAnimators = visualInstance.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < strayAnimators.Length; i++)
                Object.DestroyImmediate(strayAnimators[i]);

            SkinnedMeshRenderer doneeSmr = FindPrimarySkinnedMesh(visualInstance);
            if (doneeSmr == null || doneeSmr.sharedMesh == null)
            {
                Object.DestroyImmediate(visualInstance);
                result.message = "Visual source has no SkinnedMeshRenderer/mesh.";
                return result;
            }

            AlignVisualToTemplateBounds(visualInstance, doneeSmr, templateBounds);

            Mesh doneeMesh = doneeSmr.sharedMesh;
            Vector3[] doneeVerts = doneeMesh.vertices;
            Vector3[] doneeNormals = doneeMesh.normals;
            Vector2[] doneeUv = doneeMesh.uv;
            int[] doneeTris = doneeMesh.triangles;
            int vertCount = doneeVerts.Length;

            // Donor verts in world space.
            Vector3[] donorLocalVerts = donorMesh.vertices;
            int[] donorTris = donorMesh.triangles;
            BoneWeight[] donorWeights = donorMesh.boneWeights;
            if (donorWeights == null || donorWeights.Length != donorLocalVerts.Length)
            {
                Object.DestroyImmediate(visualInstance);
                result.message = "Donor mesh missing boneWeights.";
                return result;
            }

            Matrix4x4 donorLocalToWorld = donorSmr.transform.localToWorldMatrix;
            var donorWorldVerts = new Vector3[donorLocalVerts.Length];
            for (int i = 0; i < donorLocalVerts.Length; i++)
                donorWorldVerts[i] = donorLocalToWorld.MultiplyPoint3x4(donorLocalVerts[i]);

            // Spatial hash for nearest donor vertex.
            float cellSize = Mathf.Max(0.08f, templateBounds.size.magnitude * 0.04f);
            var hash = BuildSpatialHash(donorWorldVerts, cellSize);

            Matrix4x4 doneeLocalToWorld = doneeSmr.transform.localToWorldMatrix;
            Matrix4x4 meshWorldToLocal = meshTransform.worldToLocalMatrix;

            var newVerts = new Vector3[vertCount];
            var newNormals = new Vector3[vertCount];
            var newWeights = new BoneWeight[vertCount];
            float distSum = 0f;
            float distMax = 0f;

            for (int i = 0; i < vertCount; i++)
            {
                Vector3 worldPos = doneeLocalToWorld.MultiplyPoint3x4(doneeVerts[i]);
                newVerts[i] = meshWorldToLocal.MultiplyPoint3x4(worldPos);

                if (doneeNormals != null && doneeNormals.Length == vertCount)
                {
                    Vector3 worldN = doneeLocalToWorld.MultiplyVector(doneeNormals[i]).normalized;
                    newNormals[i] = meshWorldToLocal.MultiplyVector(worldN).normalized;
                }
                else
                {
                    newNormals[i] = Vector3.up;
                }

                BoneWeight weight;
                float nearestDist;
                if (settings.useBarycentricTransfer)
                {
                    weight = SampleDonorWeightBarycentric(
                        worldPos, donorWorldVerts, donorTris, donorWeights, hash, cellSize, out nearestDist);
                }
                else
                {
                    int nearest = FindNearestVertex(worldPos, donorWorldVerts, hash, cellSize, out nearestDist);
                    weight = donorWeights[nearest];
                }

                newWeights[i] = NormalizeWeight(weight);
                distSum += nearestDist;
                if (nearestDist > distMax)
                    distMax = nearestDist;
            }

            float avgDist = vertCount > 0 ? distSum / vertCount : 0f;
            bool passed = avgDist <= settings.maxAvgDistance;

            // Reset Mesh slot to identity under AC root for clean bindposes.
            meshTransform.localPosition = Vector3.zero;
            meshTransform.localRotation = Quaternion.identity;
            meshTransform.localScale = Vector3.one;
            meshTransform.gameObject.SetActive(true);

            Matrix4x4[] bindPoses = new Matrix4x4[donorBones.Length];
            Matrix4x4 meshLocalToWorld = meshTransform.localToWorldMatrix;
            for (int i = 0; i < donorBones.Length; i++)
            {
                Transform bone = donorBones[i] != null ? donorBones[i] : pelvis;
                bindPoses[i] = bone.worldToLocalMatrix * meshLocalToWorld;
            }

            Mesh baked = new Mesh
            {
                name = System.IO.Path.GetFileNameWithoutExtension(outputMeshPath)
            };
            baked.vertices = newVerts;
            baked.normals = newNormals;
            if (doneeUv != null && doneeUv.Length == vertCount)
                baked.uv = doneeUv;
            baked.triangles = doneeTris;
            baked.boneWeights = newWeights;
            baked.bindposes = bindPoses;
            baked.RecalculateBounds();
            // Weight transfer often inverts winding relative to AC bind space → Lit/Unlit
            // backface cull shows shadow-only. Flip once, then rebuild normals.
            ReverseTriangleWinding(baked);
            baked.RecalculateNormals();
            baked.RecalculateTangents();

            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.MeshesCreatures);
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(outputMeshPath);
            if (existing != null)
                AssetDatabase.DeleteAsset(outputMeshPath);

            AssetDatabase.CreateAsset(baked, outputMeshPath);
            Mesh loaded = AssetDatabase.LoadAssetAtPath<Mesh>(outputMeshPath);

            Material[] mats;
            if (projectMaterial != null)
            {
                int matCount = doneeSmr.sharedMaterials != null && doneeSmr.sharedMaterials.Length > 0
                    ? doneeSmr.sharedMaterials.Length
                    : 1;
                mats = new Material[matCount];
                for (int m = 0; m < mats.Length; m++)
                    mats[m] = projectMaterial;
            }
            else
            {
                mats = doneeSmr.sharedMaterials;
            }

            donorSmr.sharedMesh = loaded;
            donorSmr.bones = donorBones;
            donorSmr.rootBone = donorSmr.rootBone != null ? donorSmr.rootBone : pelvis;
            donorSmr.sharedMaterials = mats;
            donorSmr.enabled = true;

            Object.DestroyImmediate(visualInstance);

            // Remove sockpuppet retargeter if present — AC Mesh owns the look now.
            DMICreatureBoneRetargeter retargeter = acRoot.GetComponent<DMICreatureBoneRetargeter>();
            if (retargeter != null)
                Object.DestroyImmediate(retargeter);

            result.mesh = loaded;
            result.meshPath = outputMeshPath;
            result.avgNearestDistance = avgDist;
            result.maxNearestDistance = distMax;
            result.vertexCount = vertCount;
            result.passedQualityGate = passed;
            result.message = passed
                ? $"Reskin OK — verts={vertCount}, avgDist={avgDist:F3}, maxDist={distMax:F3}"
                : $"Reskin soft-fail — avgDist={avgDist:F3} > {settings.maxAvgDistance:F3} (mesh still applied)";

            Debug.Log($"[DMICreatureAutoReskin] {result.message} → {outputMeshPath}", acRoot);
            return result;
        }

        private static BoneWeight SampleDonorWeightBarycentric(
            Vector3 worldPos,
            Vector3[] donorWorldVerts,
            int[] donorTris,
            BoneWeight[] donorWeights,
            Dictionary<Vector3Int, List<int>> hash,
            float cellSize,
            out float nearestDist)
        {
            int nearest = FindNearestVertex(worldPos, donorWorldVerts, hash, cellSize, out nearestDist);
            if (donorTris == null || donorTris.Length < 3)
                return donorWeights[nearest];

            // Search triangles that include the nearest vertex (or nearby verts).
            float bestDist = float.MaxValue;
            BoneWeight best = donorWeights[nearest];
            var candidateVerts = new HashSet<int> { nearest };
            Vector3Int cell = WorldToCell(worldPos, cellSize);
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            for (int dz = -1; dz <= 1; dz++)
            {
                if (!hash.TryGetValue(new Vector3Int(cell.x + dx, cell.y + dy, cell.z + dz), out List<int> list))
                    continue;
                for (int i = 0; i < list.Count; i++)
                    candidateVerts.Add(list[i]);
            }

            for (int t = 0; t < donorTris.Length; t += 3)
            {
                int i0 = donorTris[t];
                int i1 = donorTris[t + 1];
                int i2 = donorTris[t + 2];
                if (!candidateVerts.Contains(i0) && !candidateVerts.Contains(i1) && !candidateVerts.Contains(i2))
                    continue;

                Vector3 a = donorWorldVerts[i0];
                Vector3 b = donorWorldVerts[i1];
                Vector3 c = donorWorldVerts[i2];
                ClosestPointOnTriangle(worldPos, a, b, c, out Vector3 closest, out Vector3 bary);
                float d = Vector3.Distance(worldPos, closest);
                if (d >= bestDist)
                    continue;

                bestDist = d;
                best = BlendWeights(donorWeights[i0], donorWeights[i1], donorWeights[i2], bary);
            }

            nearestDist = bestDist < float.MaxValue ? bestDist : nearestDist;
            return best;
        }

        private static BoneWeight BlendWeights(BoneWeight a, BoneWeight b, BoneWeight c, Vector3 bary)
        {
            // Gather up to 12 influences then collapse to top 4.
            var map = new Dictionary<int, float>(12);
            Accumulate(map, a, bary.x);
            Accumulate(map, b, bary.y);
            Accumulate(map, c, bary.z);

            var list = new List<KeyValuePair<int, float>>(map);
            list.Sort((x, y) => y.Value.CompareTo(x.Value));

            BoneWeight result = default;
            float w0 = list.Count > 0 ? list[0].Value : 0f;
            float w1 = list.Count > 1 ? list[1].Value : 0f;
            float w2 = list.Count > 2 ? list[2].Value : 0f;
            float w3 = list.Count > 3 ? list[3].Value : 0f;
            float sum = w0 + w1 + w2 + w3;
            if (sum <= 0.0001f)
                return a;

            result.boneIndex0 = list.Count > 0 ? list[0].Key : 0;
            result.boneIndex1 = list.Count > 1 ? list[1].Key : 0;
            result.boneIndex2 = list.Count > 2 ? list[2].Key : 0;
            result.boneIndex3 = list.Count > 3 ? list[3].Key : 0;
            result.weight0 = w0 / sum;
            result.weight1 = w1 / sum;
            result.weight2 = w2 / sum;
            result.weight3 = w3 / sum;
            return result;
        }

        private static void Accumulate(Dictionary<int, float> map, BoneWeight bw, float scale)
        {
            if (scale <= 0f)
                return;
            Add(map, bw.boneIndex0, bw.weight0 * scale);
            Add(map, bw.boneIndex1, bw.weight1 * scale);
            Add(map, bw.boneIndex2, bw.weight2 * scale);
            Add(map, bw.boneIndex3, bw.weight3 * scale);
        }

        private static void Add(Dictionary<int, float> map, int index, float w)
        {
            if (w <= 0f)
                return;
            if (map.TryGetValue(index, out float existing))
                map[index] = existing + w;
            else
                map[index] = w;
        }

        private static BoneWeight NormalizeWeight(BoneWeight w)
        {
            float sum = w.weight0 + w.weight1 + w.weight2 + w.weight3;
            if (sum <= 0.0001f)
            {
                w.weight0 = 1f;
                w.weight1 = w.weight2 = w.weight3 = 0f;
                return w;
            }

            w.weight0 /= sum;
            w.weight1 /= sum;
            w.weight2 /= sum;
            w.weight3 /= sum;
            return w;
        }

        private static Dictionary<Vector3Int, List<int>> BuildSpatialHash(Vector3[] points, float cellSize)
        {
            var hash = new Dictionary<Vector3Int, List<int>>(points.Length);
            for (int i = 0; i < points.Length; i++)
            {
                Vector3Int cell = WorldToCell(points[i], cellSize);
                if (!hash.TryGetValue(cell, out List<int> list))
                {
                    list = new List<int>(8);
                    hash[cell] = list;
                }

                list.Add(i);
            }

            return hash;
        }

        private static int FindNearestVertex(
            Vector3 worldPos,
            Vector3[] donorWorldVerts,
            Dictionary<Vector3Int, List<int>> hash,
            float cellSize,
            out float nearestDist)
        {
            Vector3Int cell = WorldToCell(worldPos, cellSize);
            nearestDist = float.MaxValue;
            int best = 0;

            for (int ring = 0; ring <= 3; ring++)
            {
                bool found = false;
                for (int dx = -ring; dx <= ring; dx++)
                for (int dy = -ring; dy <= ring; dy++)
                for (int dz = -ring; dz <= ring; dz++)
                {
                    if (ring > 0 && Mathf.Abs(dx) != ring && Mathf.Abs(dy) != ring && Mathf.Abs(dz) != ring)
                        continue;

                    if (!hash.TryGetValue(new Vector3Int(cell.x + dx, cell.y + dy, cell.z + dz), out List<int> list))
                        continue;

                    for (int i = 0; i < list.Count; i++)
                    {
                        int idx = list[i];
                        float d = (donorWorldVerts[idx] - worldPos).sqrMagnitude;
                        if (d < nearestDist)
                        {
                            nearestDist = d;
                            best = idx;
                            found = true;
                        }
                    }
                }

                if (found && ring >= 1)
                    break;
            }

            nearestDist = Mathf.Sqrt(nearestDist);
            return best;
        }

        private static Vector3Int WorldToCell(Vector3 p, float cellSize)
        {
            return new Vector3Int(
                Mathf.FloorToInt(p.x / cellSize),
                Mathf.FloorToInt(p.y / cellSize),
                Mathf.FloorToInt(p.z / cellSize));
        }

        private static void ClosestPointOnTriangle(
            Vector3 p, Vector3 a, Vector3 b, Vector3 c,
            out Vector3 closest, out Vector3 bary)
        {
            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 ap = p - a;

            float d1 = Vector3.Dot(ab, ap);
            float d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0f && d2 <= 0f)
            {
                closest = a;
                bary = new Vector3(1f, 0f, 0f);
                return;
            }

            Vector3 bp = p - b;
            float d3 = Vector3.Dot(ab, bp);
            float d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0f && d4 <= d3)
            {
                closest = b;
                bary = new Vector3(0f, 1f, 0f);
                return;
            }

            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0f && d1 >= 0f && d3 <= 0f)
            {
                float v = d1 / (d1 - d3);
                closest = a + v * ab;
                bary = new Vector3(1f - v, v, 0f);
                return;
            }

            Vector3 cp = p - c;
            float d5 = Vector3.Dot(ab, cp);
            float d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0f && d5 <= d6)
            {
                closest = c;
                bary = new Vector3(0f, 0f, 1f);
                return;
            }

            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0f && d2 >= 0f && d6 <= 0f)
            {
                float w = d2 / (d2 - d6);
                closest = a + w * ac;
                bary = new Vector3(1f - w, 0f, w);
                return;
            }

            float va = d3 * d6 - d5 * d4;
            if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
            {
                float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                closest = b + w * (c - b);
                bary = new Vector3(0f, 1f - w, w);
                return;
            }

            float denom = 1f / (va + vb + vc);
            float vOut = vb * denom;
            float wOut = vc * denom;
            closest = a + ab * vOut + ac * wOut;
            bary = new Vector3(1f - vOut - wOut, vOut, wOut);
        }

        private static Bounds CaptureRendererBounds(SkinnedMeshRenderer smr)
        {
            return smr.bounds;
        }

        private static void ReverseTriangleWinding(Mesh mesh)
        {
            if (mesh == null)
                return;

            int[] tris = mesh.triangles;
            for (int i = 0; i + 2 < tris.Length; i += 3)
            {
                int tmp = tris[i];
                tris[i] = tris[i + 2];
                tris[i + 2] = tmp;
            }

            mesh.triangles = tris;
        }

        private static void AlignVisualToTemplateBounds(
            GameObject visualInstance,
            SkinnedMeshRenderer sourceSmr,
            Bounds templateBounds)
        {
            Bounds visualBounds = sourceSmr.bounds;
            float templateHeight = Mathf.Max(templateBounds.size.y, 0.01f);
            float visualHeight = Mathf.Max(visualBounds.size.y, 0.01f);
            float scale = templateHeight / visualHeight;
            visualInstance.transform.localScale = Vector3.one * scale;

            visualBounds = sourceSmr.bounds;
            Vector3 delta = templateBounds.center - visualBounds.center;
            visualInstance.transform.position += delta;
        }

        private static SkinnedMeshRenderer FindPrimarySkinnedMesh(GameObject visualRoot)
        {
            SkinnedMeshRenderer[] renderers = visualRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers == null || renderers.Length == 0)
                return null;

            SkinnedMeshRenderer best = renderers[0];
            int bestBones = best.bones != null ? best.bones.Length : 0;
            for (int i = 1; i < renderers.Length; i++)
            {
                int count = renderers[i].bones != null ? renderers[i].bones.Length : 0;
                if (count > bestBones)
                {
                    best = renderers[i];
                    bestBones = count;
                }
            }

            return best;
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent == null)
                return null;
            if (parent.name == childName)
                return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindChildRecursive(parent.GetChild(i), childName);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
