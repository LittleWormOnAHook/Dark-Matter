using System.Collections.Generic;
using UnityEngine;

namespace Project.Building
{
    /// <summary>
    /// 0926-generator: built pieces with lights switch them off while their base has no power.
    /// 0927-power-lights: also dims glowing (emissive) materials, gates glow-only pieces (signs, posters), applies the
    /// unpowered look the moment the piece is built or restored from a save (no lit first frames), and re-applies it
    /// every check so nothing that re-enables lights or restores materials (build fx, move, finish change) can leave
    /// a piece lit. Every gated piece is a power consumer (generator load). The component may sit on the prefab: it
    /// stays idle on build previews and only acts on a placed piece (DMBuildingGhost.Built).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMBasePoweredLights : MonoBehaviour
    {
        const float CheckSeconds = 0.5f;
        const float EmissiveThreshold = 0.01f;
        /// <summary>Emission kept while unpowered (0 = fully off).</summary>
        const float UnpoweredEmissionScale = 0f;

        static readonly int EmissiveColorId = Shader.PropertyToID("_EmissiveColor");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        /// <summary>Source material to its unpowered copy (a non-glowing material maps to itself).</summary>
        static readonly Dictionary<Material, Material> darkBySource = new Dictionary<Material, Material>();
        static readonly Dictionary<Material, Material> sourceByDark = new Dictionary<Material, Material>();
        static readonly List<Material> scratch = new List<Material>(4);

        Light[] lights;
        bool[] authoredOn;
        Renderer[] renderers;
        DMBuildingGhost ghost;
        bool registered;
        float nextCheck;
        int state = -1;

        /// <summary>1 powered, 0 unpowered, -1 not evaluated yet (or not a placed piece).</summary>
        public int State => state;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            foreach (Material dark in sourceByDark.Keys)
            {
                if (dark == null)
                    continue;
                if (Application.isPlaying)
                    Destroy(dark);
                else
                    DestroyImmediate(dark);
            }

            darkBySource.Clear();
            sourceByDark.Clear();
        }

        /// <summary>
        /// Placement and save load: gates a built piece that has Light components, or (when allowed) glowing materials.
        /// Returns the gate, or null when the piece has nothing to power.
        /// </summary>
        public static DMBasePoweredLights Ensure(GameObject root, bool allowGlowOnly)
        {
            if (root == null)
                return null;
            DMBasePoweredLights gate = root.GetComponentInChildren<DMBasePoweredLights>(true);
            if (gate == null)
            {
                bool wants = root.GetComponentInChildren<Light>(true) != null || (allowGlowOnly && HasGlow(root));
                if (!wants)
                    return null;
                gate = root.AddComponent<DMBasePoweredLights>();
            }

            if (gate.isActiveAndEnabled)
                gate.RefreshNow();
            return gate;
        }

        /// <summary>True when any renderer under root has an emissive material.</summary>
        public static bool HasGlow(GameObject root)
        {
            if (root == null)
                return false;
            Renderer[] all = root.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < all.Length; r++)
            {
                if (all[r] == null || all[r] is ParticleSystemRenderer)
                    continue;
                all[r].GetSharedMaterials(scratch);
                for (int m = 0; m < scratch.Count; m++)
                {
                    if (IsEmissive(scratch[m]))
                        return true;
                }
            }

            return false;
        }

        public static bool IsEmissive(Material material)
        {
            if (material == null || sourceByDark.ContainsKey(material))
                return false;
            if (material.HasProperty(EmissiveColorId))
                return material.GetColor(EmissiveColorId).maxColorComponent > EmissiveThreshold;
            if (material.HasProperty(EmissionColorId) && material.IsKeywordEnabled("_EMISSION"))
                return material.GetColor(EmissionColorId).maxColorComponent > EmissiveThreshold;
            return false;
        }

        void Awake()
        {
            lights = GetComponentsInChildren<Light>(true);
            authoredOn = new bool[lights.Length];
            for (int i = 0; i < lights.Length; i++)
                authoredOn[i] = lights[i] != null && lights[i].enabled;
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        void OnEnable()
        {
            RefreshNow();
        }

        void OnDisable()
        {
            if (registered)
                DMBasePower.UnregisterConsumer(this);
            registered = false;
        }

        void Update()
        {
            if (Time.unscaledTime >= nextCheck)
                RefreshNow();
        }

        /// <summary>Evaluates power now and applies the lit or dark look.</summary>
        public void RefreshNow()
        {
            nextCheck = Time.unscaledTime + CheckSeconds * Random.Range(0.85f, 1.15f);
            if (ghost == null)
                ghost = GetComponentInParent<DMBuildingGhost>(true);
            if (ghost == null || !ghost.Built)
            {
                // Build preview or not placed yet: keep the authored look and draw nothing.
                state = -1;
                return;
            }

            // The generator's own lights are not a load on itself.
            if (!registered && GetComponentInParent<DMBaseGenerator>(true) == null)
            {
                DMBasePower.RegisterConsumer(this, false);
                registered = true;
            }

            bool powered = !DMBuildingGhostProfile.LightsNeedPower || DMBasePower.IsPowered(transform.position);
            state = powered ? 1 : 0;
            ApplyLights(powered);

            // The build fx swaps every material for a while and puts the originals back; the next check catches up.
            DMBuildingCreationFx fx = ghost.GetComponent<DMBuildingCreationFx>();
            if (fx == null || !fx.enabled)
                ApplyGlow(powered);
        }

        void ApplyLights(bool powered)
        {
            if (lights == null)
                return;
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                if (light == null)
                    continue;
                bool want = powered && authoredOn[i];
                if (light.enabled != want)
                    light.enabled = want;
            }
        }

        void ApplyGlow(bool powered)
        {
            if (renderers == null)
                return;
            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer target = renderers[r];
                if (target == null || target is ParticleSystemRenderer)
                    continue;
                target.GetSharedMaterials(scratch);
                bool changed = false;
                for (int m = 0; m < scratch.Count; m++)
                {
                    Material current = scratch[m];
                    if (current == null)
                        continue;
                    Material next = powered ? SourceOf(current) : DarkOf(current);
                    if (next == current)
                        continue;
                    scratch[m] = next;
                    changed = true;
                }

                if (changed)
                    target.sharedMaterials = scratch.ToArray();
            }
        }

        static Material SourceOf(Material material)
        {
            return sourceByDark.TryGetValue(material, out Material source) && source != null ? source : material;
        }

        static Material DarkOf(Material material)
        {
            if (sourceByDark.ContainsKey(material))
                return material;
            if (darkBySource.TryGetValue(material, out Material dark) && dark != null)
                return dark;
            if (!IsEmissive(material))
            {
                darkBySource[material] = material;
                return material;
            }

            dark = new Material(material)
            {
                name = material.name + " (Unpowered)",
                hideFlags = HideFlags.DontSave,
            };
            if (dark.HasProperty(EmissiveColorId))
                dark.SetColor(EmissiveColorId, Scaled(dark.GetColor(EmissiveColorId)));
            if (dark.HasProperty(EmissionColorId))
                dark.SetColor(EmissionColorId, Scaled(dark.GetColor(EmissionColorId)));
            darkBySource[material] = dark;
            sourceByDark[dark] = material;
            return dark;
        }

        static Color Scaled(Color c)
        {
            return new Color(c.r * UnpoweredEmissionScale, c.g * UnpoweredEmissionScale, c.b * UnpoweredEmissionScale, c.a);
        }
    }
}
