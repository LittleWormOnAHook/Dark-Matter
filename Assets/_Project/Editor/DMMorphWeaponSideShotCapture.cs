using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Project.Prototypes.Weapons;

namespace Project.EditorTools.Audit
{
    /// <summary>
    /// Edit-mode weapon-only side shots via PreviewRenderUtility (isolated, no scene sky).
    /// No Play Mode, no scene save. Temp objects destroyed after capture.
    /// </summary>
    public static class DMMorphWeaponSideShotCapture
    {
        const string BladePrefab = "Assets/_Project/Prefabs/Weapons/Prototypes/MorphBlade_Held.prefab";
        const string OutDir = "Assets/_Project/Art/Debug/MorphShots";
        const int Size = 1024;

        [MenuItem("Dark Matter/Audit/Morph Weapon Side Shots (Edit Mode)")]
        public static void RunFromMenu()
        {
            string report = CaptureAll();
            Debug.Log("[DMMorphSideShots] " + report);
            EditorUtility.DisplayDialog("Morph Side Shots", report, "OK");
        }

        public static string CaptureAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return "FAIL: exit Play Mode first (Edit-mode capture only)";

            Directory.CreateDirectory(Path.Combine(Application.dataPath, "_Project/Art/Debug/MorphShots"));
            var log = new StringBuilder();
            int ok = 0;
            ok += CapturePrefab(BladePrefab, "Blade", new[] { "Metal", "Light", "Plasma", "Ion", "Laser", "Cryo" }, log);
            AssetDatabase.Refresh();
            log.Insert(0, "DONE sideShots=" + ok + "\n");
            return log.ToString();
        }

        static int CapturePrefab(string prefabPath, string prefix, string[] expectedNames, StringBuilder log)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                log.AppendLine("FAIL missing prefab " + prefabPath);
                return 0;
            }

            PreviewRenderUtility preview = null;
            GameObject root = null;
            int captured = 0;

            try
            {
                preview = new PreviewRenderUtility(true);
                preview.cameraFieldOfView = 30f;
                preview.camera.nearClipPlane = 0.01f;
                preview.camera.farClipPlane = 50f;
                preview.camera.orthographic = true;
                preview.camera.clearFlags = CameraClearFlags.SolidColor;
                preview.camera.backgroundColor = new Color(0.05f, 0.055f, 0.07f, 1f);

                // Preview lights
                preview.lights[0].intensity = 1.4f;
                preview.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0f);
                preview.lights[0].color = new Color(1f, 0.98f, 0.95f);
                if (preview.lights.Length > 1)
                {
                    preview.lights[1].intensity = 0.7f;
                    preview.lights[1].transform.rotation = Quaternion.Euler(20f, -50f, 0f);
                    preview.lights[1].color = new Color(0.65f, 0.75f, 0.95f);
                }
                preview.ambientColor = new Color(0.18f, 0.19f, 0.22f);

                root = Object.Instantiate(prefab);
                root.name = "TMP_MorphSide_" + prefix;
                root.hideFlags = HideFlags.HideAndDontSave;
                root.transform.position = Vector3.zero;
                root.transform.rotation = Quaternion.identity;
                preview.AddSingleGO(root);

                var morph = root.GetComponent<DMWeaponMorphController>()
                    ?? root.GetComponentInChildren<DMWeaponMorphController>(true);
                if (morph == null)
                {
                    log.AppendLine("FAIL no morph on " + prefabPath);
                    return 0;
                }

                morph.ApplyFormAtIndexImmediate(0);

                int count = morph.FormCount > 0 ? morph.FormCount : expectedNames.Length;
                for (int i = 0; i < count; i++)
                {
                    string label = i < expectedNames.Length ? expectedNames[i] : morph.FormDisplayName(i);
                    label = label.Replace(" ", "");
                    morph.ApplyFormAtIndexImmediate(i);

                    ApplyPreviewMaterials(root);
                    Bounds b = ComputeBounds(root);
                    FrameSide(preview.camera, b);

                    string fileName = "Side_" + prefix + "_" + label + ".png";
                    string abs = Path.Combine(Application.dataPath, "_Project/Art/Debug/MorphShots", fileName);
                    string assetPath = OutDir + "/" + fileName;

                    if (RenderPreview(preview, abs))
                    {
                        captured++;
                        log.AppendLine("ok " + assetPath);
                    }
                    else log.AppendLine("FAIL render " + assetPath);
                }
            }
            catch (System.Exception e)
            {
                log.AppendLine("FAIL ex: " + e.Message);
                Debug.LogError("[DMMorphSideShots] " + e);
            }
            finally
            {
                if (root != null) Object.DestroyImmediate(root);
                if (preview != null) preview.Cleanup();
            }
            return captured;
        }

        static void ApplyPreviewMaterials(GameObject root)
        {
            Shader unlit = Shader.Find("HDRP/Unlit");
            if (unlit == null) return;
            int idx = 0;
            float[] shades = { 1.0f, 0.82f, 1.18f, 0.9f, 1.1f, 0.75f, 1.25f };
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                r.SetPropertyBlock(null);
                var src = r.sharedMaterial;
                if (src == null || src.name.StartsWith("DMPreview_")) { idx++; continue; }
                Color c = src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor") : (src.HasProperty("_UnlitColor") ? src.GetColor("_UnlitColor") : Color.gray);
                float mx = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                if (mx > 1f) c = new Color(c.r / mx, c.g / mx, c.b / mx, c.a);
                float lum = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
                float boost = lum < 0.55f ? 0.6f / Mathf.Max(lum, 0.05f) : 1f;
                float s = shades[idx % shades.Length] * boost;
                var m = new Material(unlit) { hideFlags = HideFlags.HideAndDontSave, name = "DMPreview_" + src.name };
                if (m.HasProperty("_EmissiveColor")) m.SetColor("_EmissiveColor", Color.black);
                m.SetColor("_UnlitColor", new Color(Mathf.Clamp01(c.r * s), Mathf.Clamp01(c.g * s), Mathf.Clamp01(c.b * s), 1f));
                r.sharedMaterial = m;
                idx++;
            }
        }

        static Bounds ComputeBounds(GameObject root)
        {
            Bounds b = new Bounds(root.transform.position, Vector3.one * 0.2f);
            bool any = false;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy || r.bounds.size.sqrMagnitude < 1e-8f) continue;
                if (!any) { b = r.bounds; any = true; }
                else b.Encapsulate(r.bounds);
            }
            return b;
        }

        static void FrameSide(Camera cam, Bounds b)
        {
            Vector3 center = b.center;
            Vector3 e = b.extents;
            Vector3 view = Vector3.right; // side view along X (thin axis)
            Vector3 up = e.y >= e.z ? Vector3.forward : Vector3.up; // put longest axis horizontal
            float lenH = Mathf.Max(e.y, e.z);
            float lenV = Mathf.Min(e.y, e.z);
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(lenH * 1.15f, lenV * 1.6f, 0.05f);
            float dist = Mathf.Max(e.magnitude * 3f, 1f);
            cam.transform.position = center + view * dist;
            cam.transform.rotation = Quaternion.LookRotation(-view, up);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.055f, 0.07f, 1f);
        }

        static bool RenderPreview(PreviewRenderUtility preview, string absolutePath)
        {
            try
            {
                var rect = new Rect(0, 0, Size, Size);
                preview.BeginPreview(rect, GUIStyle.none);
                preview.camera.clearFlags = CameraClearFlags.SolidColor;
                preview.camera.backgroundColor = new Color(0.05f, 0.055f, 0.07f, 1f);
                preview.Render(true);
                Texture tex = preview.EndPreview();
                if (tex == null) return false;

                RenderTexture rt = tex as RenderTexture;
                Texture2D readable;
                if (rt != null)
                {
                    RenderTexture prev = RenderTexture.active;
                    RenderTexture.active = rt;
                    readable = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false, false);
                    readable.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    readable.Apply();
                    RenderTexture.active = prev;
                }
                else
                {
                    readable = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false, false);
                    RenderTexture tmp = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
                    Graphics.Blit(tex, tmp);
                    RenderTexture prev = RenderTexture.active;
                    RenderTexture.active = tmp;
                    readable.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                    readable.Apply();
                    RenderTexture.active = prev;
                    RenderTexture.ReleaseTemporary(tmp);
                }

                // Force any remaining near-white sky fringe only at edges? skip — PreviewRenderUtility should be dark.
                // Ensure fully opaque dark where alpha is low
                Color dark = new Color(0.05f, 0.055f, 0.07f, 1f);
                var pixels = readable.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (pixels[i].a < 8)
                        pixels[i] = new Color32(13, 14, 18, 255);
                    else if (pixels[i].a < 250)
                    {
                        float a = pixels[i].a / 255f;
                        pixels[i] = new Color32(
                            (byte)(pixels[i].r * a + 13 * (1 - a)),
                            (byte)(pixels[i].g * a + 14 * (1 - a)),
                            (byte)(pixels[i].b * a + 18 * (1 - a)),
                            255);
                    }
                }
                readable.SetPixels32(pixels);
                readable.Apply();

                File.WriteAllBytes(absolutePath, readable.EncodeToPNG());
                Object.DestroyImmediate(readable);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError("[DMMorphSideShots] render ex: " + e);
                return false;
            }
        }
    }
}




