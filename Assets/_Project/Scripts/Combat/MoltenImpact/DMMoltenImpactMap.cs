using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Which molten impact profile each surface gets. Lives at Resources/Combat/DMMoltenImpactMap.
    /// Rules are checked top to bottom; the first match wins (a matching rule with no profile = no molten effect).
    /// </summary>
    [CreateAssetMenu(menuName = "Dark Matter/Combat/Molten Impact Map", fileName = "DMMoltenImpactMap")]
    public class DMMoltenImpactMap : ScriptableObject
    {
        public const string ResourcesPath = "Combat/DMMoltenImpactMap";
        public const string AssetPath = "Assets/_Project/Resources/Combat/DMMoltenImpactMap.asset";

        [Serializable]
        public class Rule
        {
            [Tooltip("Just a label for the list.")]
            public string label;
            [Tooltip("Unity tag on the hit object (or a parent). Empty = any.")]
            public string tag;
            [Tooltip("Hit object or parent name contains this (case-insensitive). Empty = any.")]
            public string nameContains;
            [Tooltip("Physics material on the hit collider. Empty = any.")]
            public PhysicsMaterial physicsMaterial;
            [Tooltip("Component type name on the hit object or a parent, e.g. DmRockCombiner. Empty = any.")]
            public string componentType;
            [Tooltip("Profile to play. Empty = no molten effect for this surface.")]
            public DMMoltenImpactProfile profile;

            public bool HasCriteria => !string.IsNullOrEmpty(tag) || !string.IsNullOrEmpty(nameContains)
                || physicsMaterial != null || !string.IsNullOrEmpty(componentType);
        }

        [Serializable]
        public struct AmmoHeat
        {
            public AmmoType ammoType;
            [Tooltip("Scales glow brightness and drip count. 0 = this ammo leaves no molten mark.")]
            [Min(0f)] public float heat;
        }

        [Header("Master")]
        [Tooltip("Ammo impacts on world surfaces leave molten marks (Play Mode).")]
        public bool enableMoltenImpacts = true;
        [Tooltip("Most molten impacts alive at once; the oldest is recycled first.")]
        [Range(1, 128)] public int maxActive = 24;
        [Tooltip("Layers molten drips collide with.")]
        public LayerMask dripCollisionMask = Physics.DefaultRaycastLayers;
        [Tooltip("Shader for the glow spot and drips (Project/MoltenGlow). Keep it set so builds include it.")]
        public Shader glowShader;

        [Header("Surfaces")]
        public Rule[] rules = new Rule[0];
        [Tooltip("Profile for surfaces no rule matches. Empty = nothing.")]
        public DMMoltenImpactProfile defaultProfile;

        [Header("Ammo")]
        [Tooltip("Per-ammo heat. Unlisted ammo = 1.")]
        public AmmoHeat[] ammoHeat = new AmmoHeat[0];

        [NonSerialized] private static DMMoltenImpactMap cached;
        [NonSerialized] private static readonly Dictionary<string, Type> TypeCache = new Dictionary<string, Type>();

        public static DMMoltenImpactMap Active
        {
            get
            {
                if (cached == null)
                {
                    cached = Resources.Load<DMMoltenImpactMap>(ResourcesPath);
                    if (cached == null)
                    {
                        cached = CreateDefault();
                        cached.hideFlags = HideFlags.DontSave;
                    }
                }

                return cached;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            // Domain reload is off: re-read the asset each Play session (picks up a map created after the last reload).
            if (cached != null && cached.hideFlags == HideFlags.DontSave)
                cached = null;
        }

        public float HeatFor(AmmoType type)
        {
            if (ammoHeat != null)
            {
                for (int i = 0; i < ammoHeat.Length; i++)
                {
                    if (ammoHeat[i].ammoType == type)
                        return ammoHeat[i].heat;
                }
            }

            return 1f;
        }

        public DMMoltenImpactProfile Resolve(GameObject receiver, Collider hitCollider = null)
        {
            if (receiver == null)
                return null;

            if (rules != null)
            {
                if (hitCollider == null)
                    hitCollider = receiver.GetComponent<Collider>();
                for (int i = 0; i < rules.Length; i++)
                {
                    Rule r = rules[i];
                    if (r != null && r.HasCriteria && Matches(r, receiver, hitCollider))
                        return r.profile;
                }
            }

            return defaultProfile;
        }

        private static bool Matches(Rule r, GameObject receiver, Collider col)
        {
            if (!string.IsNullOrEmpty(r.tag) && !TagInParents(receiver.transform, r.tag))
                return false;
            if (!string.IsNullOrEmpty(r.nameContains) && !NameInParents(receiver.transform, r.nameContains))
                return false;
            if (r.physicsMaterial != null && (col == null || col.sharedMaterial != r.physicsMaterial))
                return false;
            if (!string.IsNullOrEmpty(r.componentType))
            {
                Type t = FindType(r.componentType);
                if (t == null || receiver.GetComponentInParent(t) == null)
                    return false;
            }

            return true;
        }

        private static bool TagInParents(Transform t, string tag)
        {
            // Plain string compare: CompareTag logs for tags missing from the Tag Manager.
            for (; t != null; t = t.parent)
            {
                if (string.Equals(t.tag, tag, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static bool NameInParents(Transform t, string word)
        {
            for (; t != null; t = t.parent)
            {
                if (t.name.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static Type FindType(string name)
        {
            if (TypeCache.TryGetValue(name, out Type t))
                return t;

            t = null;
            foreach (System.Reflection.Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (System.Reflection.ReflectionTypeLoadException e) { types = e.Types; }
                if (types == null)
                    continue;
                for (int i = 0; i < types.Length; i++)
                {
                    Type c = types[i];
                    if (c != null && typeof(Component).IsAssignableFrom(c) && (c.Name == name || c.FullName == name))
                    {
                        t = c;
                        break;
                    }
                }

                if (t != null)
                    break;
            }

            TypeCache[name] = t;
            return t;
        }

        /// <summary>Default map: Metal = bright running drips, rocks = slight glow + small drip, water = nothing.</summary>
        public static DMMoltenImpactMap CreateDefault()
        {
            var m = CreateInstance<DMMoltenImpactMap>();
            m.name = "DMMoltenImpactMap";
            m.glowShader = Shader.Find("Project/MoltenGlow");
            DMMoltenImpactProfile metal = DMMoltenImpactProfile.CreatePreset("Metal");
            DMMoltenImpactProfile rock = DMMoltenImpactProfile.CreatePreset("Rock");
            DMMoltenImpactProfile concrete = DMMoltenImpactProfile.CreatePreset("Concrete");
            DMMoltenImpactProfile glass = DMMoltenImpactProfile.CreatePreset("Glass");
            DMMoltenImpactProfile wood = DMMoltenImpactProfile.CreatePreset("Wood");
            DMMoltenImpactProfile dirt = DMMoltenImpactProfile.CreatePreset("Dirt");
            m.rules = new[]
            {
                new Rule { label = "Water", tag = "Water", profile = null },
                new Rule { label = "Metal", tag = "Metal", profile = metal },
                new Rule { label = "Building", tag = "Building", profile = metal },
                new Rule { label = "Glass", tag = "Glass", profile = glass },
                new Rule { label = "Wood", tag = "Wood", profile = wood },
                new Rule { label = "Concrete", tag = "Concrete", profile = concrete },
                new Rule { label = "Dirt", tag = "Dirt", profile = dirt },
                new Rule { label = "Terrain", tag = "Terrain", profile = dirt },
                new Rule { label = "PCG Rock", componentType = "DmRockCombiner", profile = rock },
                new Rule { label = "Rock (name)", nameContains = "rock", profile = rock },
                new Rule { label = "Boulder (name)", nameContains = "boulder", profile = rock },
                new Rule { label = "Cliff (name)", nameContains = "cliff", profile = rock },
                new Rule { label = "Stone (name)", nameContains = "stone", profile = rock },
            };
            m.defaultProfile = DMMoltenImpactProfile.CreatePreset("Default");
            m.ammoHeat = new[]
            {
                new AmmoHeat { ammoType = AmmoType.Gunpowder, heat = 0.6f },
                new AmmoHeat { ammoType = AmmoType.Plasma, heat = 1.3f },
                new AmmoHeat { ammoType = AmmoType.Ice, heat = 0f },
                new AmmoHeat { ammoType = AmmoType.Electricity, heat = 0.7f },
                new AmmoHeat { ammoType = AmmoType.ResonanceStabilizer, heat = 0f },
                new AmmoHeat { ammoType = AmmoType.Laser, heat = 1f },
                new AmmoHeat { ammoType = AmmoType.Ion, heat = 0.8f },
                new AmmoHeat { ammoType = AmmoType.Fire, heat = 1.2f },
                new AmmoHeat { ammoType = AmmoType.Explosive, heat = 1.4f },
            };
            return m;
        }
    }
}
