using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Gaia
{
    /// <summary>
    /// Play-mode crossfade for TLM impostor meshes. Unity Terrain cannot alpha-fade,
    /// so we keep the matching impostor overlapping the live tile and fade that instead.
    /// </summary>
    public static class TerrainImpostorFader
    {
        public const float Duration = 0.65f;

        static Runner runner;
        static readonly Dictionary<int, FadeState> states = new Dictionary<int, FadeState>();

        enum Kind { In, Out }

        class FadeState
        {
            public int token;
            public Kind kind;
            public bool finished;
            public bool visible = true;
        }

        public static bool IsFullyVisible(Scene scene)
        {
            if (!scene.IsValid())
                return false;
            int id = scene.handle;
            if (!states.TryGetValue(id, out FadeState state))
                return false;
            return state.kind == Kind.In && state.finished;
        }

        public static bool FadeOutThen(Scene scene, Action onDone)
        {
            if (!Application.isPlaying || !scene.IsValid() || !scene.isLoaded)
                return false;

            Renderer[] renderers = CollectRenderers(scene);
            if (renderers.Length == 0)
                return false;

            PrepareLodCrossFade(scene);
            EnsureRunner();
            int id = scene.handle;
            FadeState state = GetState(id);
            state.token++;
            state.kind = Kind.Out;
            state.finished = false;
            int token = state.token;
            runner.StartCoroutine(Fade(id, token, renderers, 1f, 0f, () =>
            {
                if (!states.TryGetValue(id, out FadeState s) || s.token != token)
                    return;
                s.finished = true;
                s.visible = false;
                onDone?.Invoke();
            }));
            return true;
        }

        public static void FadeIn(Scene scene)
        {
            if (!Application.isPlaying || !scene.IsValid() || !scene.isLoaded)
                return;

            Renderer[] renderers = CollectRenderers(scene);
            if (renderers.Length == 0)
                return;

            int id = scene.handle;
            FadeState existing;
            if (states.TryGetValue(id, out existing) && existing.kind == Kind.In && !existing.finished)
                return;
            if (existing != null && existing.kind == Kind.In && existing.finished)
                return;

            PrepareLodCrossFade(scene);
            EnsureRunner();
            FadeState state = GetState(id);
            state.token++;
            state.kind = Kind.In;
            state.finished = false;
            state.visible = false;
            int token = state.token;
            SnapAlpha(renderers, 0f);
            runner.StartCoroutine(Fade(id, token, renderers, 0f, 1f, () =>
            {
                if (!states.TryGetValue(id, out FadeState s) || s.token != token)
                    return;
                s.finished = true;
                s.visible = true;
                RestoreOpaque(renderers);
            }));
        }

        static FadeState GetState(int id)
        {
            if (!states.TryGetValue(id, out FadeState state))
            {
                state = new FadeState();
                states[id] = state;
            }
            return state;
        }

        static void EnsureRunner()
        {
            if (runner != null)
                return;
            GameObject go = new GameObject("TerrainImpostorFader");
            UnityEngine.Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<Runner>();
        }

        static Renderer[] CollectRenderers(Scene scene)
        {
            List<Renderer> list = new List<Renderer>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] == null)
                    continue;
                roots[i].GetComponentsInChildren(true, list);
            }
            return list.ToArray();
        }

        static void PrepareLodCrossFade(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                LODGroup[] groups = roots[i].GetComponentsInChildren<LODGroup>(true);
                for (int g = 0; g < groups.Length; g++)
                {
                    groups[g].fadeMode = LODFadeMode.CrossFade;
                    groups[g].animateCrossFading = true;
                }
            }
            LODGroup.crossFadeAnimationDuration = Duration;
        }

        static IEnumerator Fade(int id, int token, Renderer[] renderers, float from, float to, Action done)
        {
            float t = 0f;
            while (t < Duration)
            {
                if (!states.TryGetValue(id, out FadeState s) || s.token != token)
                    yield break;
                t += Time.unscaledDeltaTime;
                SetAlpha(renderers, Mathf.Lerp(from, to, Mathf.Clamp01(t / Duration)));
                yield return null;
            }
            SetAlpha(renderers, to);
            done?.Invoke();
        }

        static void SnapAlpha(Renderer[] renderers, float alpha)
        {
            SetAlpha(renderers, alpha);
        }

        static void SetAlpha(Renderer[] renderers, float alpha)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null)
                    continue;
                Material[] mats = r.materials;
                for (int m = 0; m < mats.Length; m++)
                    ApplyTransparentAlpha(mats[m], alpha);
            }
        }

        static void RestoreOpaque(Renderer[] renderers)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null)
                    continue;
                Material[] mats = r.materials;
                for (int m = 0; m < mats.Length; m++)
                    ApplyOpaque(mats[m]);
            }
        }

        static void ApplyTransparentAlpha(Material mat, float alpha)
        {
            if (mat == null)
                return;

            mat.SetFloat("_SurfaceType", 1f);
            mat.SetFloat("_BlendMode", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_BLENDMODE_ALPHA");
            mat.DisableKeyword("_BLENDMODE_ADD");
            mat.DisableKeyword("_BLENDMODE_PRE_MULTIPLY");

            if (mat.HasProperty("_BaseColor"))
            {
                Color c = mat.GetColor("_BaseColor");
                c.a = alpha;
                mat.SetColor("_BaseColor", c);
            }
            if (mat.HasProperty("_UnlitColor"))
            {
                Color c = mat.GetColor("_UnlitColor");
                c.a = alpha;
                mat.SetColor("_UnlitColor", c);
            }
        }

        static void ApplyOpaque(Material mat)
        {
            if (mat == null)
                return;

            mat.SetFloat("_SurfaceType", 0f);
            mat.SetOverrideTag("RenderType", "HDLitShader");
            mat.SetFloat("_SrcBlend", (float)BlendMode.One);
            mat.SetFloat("_DstBlend", (float)BlendMode.Zero);
            mat.SetFloat("_ZWrite", 1f);
            mat.renderQueue = (int)RenderQueue.Geometry;
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_BLENDMODE_ALPHA");
            if (mat.HasProperty("_BaseColor"))
            {
                Color c = mat.GetColor("_BaseColor");
                c.a = 1f;
                mat.SetColor("_BaseColor", c);
            }
        }

        class Runner : MonoBehaviour { }
    }
}