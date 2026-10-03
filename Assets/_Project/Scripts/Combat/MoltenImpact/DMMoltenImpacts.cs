using System.Collections.Generic;
using Project.Data;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Runtime door for molten ammo impacts. Called once from DMCombatFx.PlayWorldImpact.
    /// Pooled, capped by <see cref="DMMoltenImpactMap.maxActive"/>; Play Mode only.
    /// </summary>
    public static class DMMoltenImpacts
    {
        private static readonly Stack<DMMoltenImpact> Free = new Stack<DMMoltenImpact>();
        private static readonly List<DMMoltenImpact> Active = new List<DMMoltenImpact>();
        private static Transform root;
        private static Material spotMaterial;
        private static Material dripMaterial;
        private static Mesh quadMesh;

        /// <summary>Live count (tests / debug).</summary>
        public static int ActiveCount
        {
            get
            {
                Prune();
                return Active.Count;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Domain reload is off in this project: drop references to last session's scene objects.
            Free.Clear();
            Active.Clear();
            root = null;
        }

        public static bool Play(ItemData ammoItem, DMAmmoFxProfile ammoProfile, Vector3 point, Vector3 normal, GameObject receiver)
        {
            if (!Application.isPlaying || receiver == null)
                return false;

            DMMoltenImpactMap map = DMMoltenImpactMap.Active;
            if (map == null || !map.enableMoltenImpacts)
                return false;

            float heat = 1f;
            ItemData ammo = ammoProfile != null ? ammoProfile : ammoItem;
            if (ammo != null)
                heat = map.HeatFor(ammo.ammoType);
            if (heat <= 0f)
                return false;

            DMMoltenImpactProfile profile = map.Resolve(receiver);
            if (profile == null || (!profile.glow && !profile.HasDrips))
                return false;

            if (!EnsureShared(map))
                return false;

            DMMoltenImpact fx = Acquire(map);
            if (fx == null)
                return false;

            Vector3 n = normal.sqrMagnitude > 1e-8f ? normal.normalized : Vector3.up;
            fx.Begin(profile, heat, point, n, receiver.transform, map.dripCollisionMask);
            Active.Add(fx);
            return true;
        }

        internal static void Forget(DMMoltenImpact fx)
        {
            Active.Remove(fx);
        }

        internal static void Return(DMMoltenImpact fx)
        {
            Active.Remove(fx);
            if (fx == null)
                return;
            if (root != null)
                fx.transform.SetParent(root, false);
            fx.gameObject.SetActive(false);
            Free.Push(fx);
        }

        private static DMMoltenImpact Acquire(DMMoltenImpactMap map)
        {
            Prune();
            int cap = Mathf.Max(1, map.maxActive);
            while (Active.Count >= cap)
            {
                DMMoltenImpact oldest = Active[0];
                Active.RemoveAt(0);
                if (oldest != null)
                    oldest.StopNow();
            }

            while (Free.Count > 0)
            {
                DMMoltenImpact f = Free.Pop();
                if (f != null)
                    return f;
            }

            if (root == null)
            {
                var go = new GameObject("[MoltenImpacts]");
                Object.DontDestroyOnLoad(go);
                root = go.transform;
            }

            return DMMoltenImpact.Create(root, quadMesh, spotMaterial, dripMaterial);
        }

        private static void Prune()
        {
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                if (Active[i] == null)
                    Active.RemoveAt(i);
            }
        }

        private static bool EnsureShared(DMMoltenImpactMap map)
        {
            if (spotMaterial != null && dripMaterial != null && quadMesh != null)
                return true;

            Shader shader = map.glowShader != null ? map.glowShader : Shader.Find("Project/MoltenGlow");
            if (shader == null)
                return false;

            if (spotMaterial == null)
                spotMaterial = new Material(shader) { name = "MoltenGlowSpot", hideFlags = HideFlags.HideAndDontSave };
            if (dripMaterial == null)
            {
                dripMaterial = new Material(shader) { name = "MoltenGlowDrip", hideFlags = HideFlags.HideAndDontSave };
                dripMaterial.SetFloat("_Noise", 0f);
                dripMaterial.SetFloat("_Softness", 0.9f);
            }

            if (quadMesh == null)
            {
                quadMesh = new Mesh { name = "MoltenGlowQuad", hideFlags = HideFlags.HideAndDontSave };
                quadMesh.SetVertices(new List<Vector3> { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f) });
                quadMesh.SetUVs(0, new List<Vector2> { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) });
                quadMesh.SetNormals(new List<Vector3> { Vector3.back, Vector3.back, Vector3.back, Vector3.back });
                quadMesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
                quadMesh.RecalculateBounds();
            }

            return true;
        }
    }
}
