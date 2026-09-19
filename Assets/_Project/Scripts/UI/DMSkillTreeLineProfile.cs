using UnityEngine;

namespace Project.UI
{
    /// <summary>
    /// Skill tree link visuals (core strip + gold glow). Assign sprites here or drop PNGs under
    /// Resources/UI/Skills/Tree/ (see <see cref="DMSkillTreeLineArt"/> file names).
    /// </summary>
    [CreateAssetMenu(menuName = "Dark Matter/UI/Skill Tree Line Profile", fileName = "DMSkillTreeLineProfile")]
    public sealed class DMSkillTreeLineProfile : ScriptableObject
    {
        public const string ResourcesPath = "UI/Skills/Tree/DMSkillTreeLineProfile";

        private static DMSkillTreeLineProfile live;

        public static DMSkillTreeLineProfile Live
        {
            get
            {
                if (live == null)
                    live = Resources.Load<DMSkillTreeLineProfile>(ResourcesPath);
                return live;
            }
        }

        [Header("Sprites (optional overrides)")]
        [Tooltip("Default horizontal line strip. Resources/UI/Skills/Tree/skill_path_core")]
        public Sprite pathCore;

        [Tooltip("Gold glow strip behind the core. Resources/UI/Skills/Tree/skill_path_glow")]
        public Sprite pathGlow;

        [Tooltip("Optional per-state cores; tint still applies if set.")]
        public Sprite pathLocked;
        public Sprite pathReady;
        public Sprite pathOwned;

        [Header("Thickness (pixels)")]
        public float coreHeight = 5f;
        public float glowHeight = 13f;

        [Header("Gold glow strength")]
        [Range(0f, 1f)]
        public float glowAlphaLocked = 0.1f;

        [Range(0f, 1f)]
        public float glowAlphaReady = 0.42f;

        [Range(0f, 1f)]
        public float glowAlphaOwned = 0.78f;

        [Header("Core tints (when using shared core sprite)")]
        public bool usePaletteTints = true;
    }

    /// <summary>
    /// Loads skill-tree connection sprites from <see cref="DMSkillTreeLineProfile"/> or
    /// Resources/UI/Skills/Tree/*. Procedural strips used until art is added.
    /// </summary>
    internal static class DMSkillTreeLineArt
    {
        public const string ResourceFolder = "UI/Skills/Tree/";

        private static Sprite proceduralCore;
        private static Sprite proceduralGlow;

        public static Sprite ResolveCore(DMSkillTreeLineProfile profile, PathState state)
        {
            if (profile != null)
            {
                Sprite overrideSprite = state switch
                {
                    PathState.Owned => profile.pathOwned,
                    PathState.Ready => profile.pathReady,
                    PathState.Locked => profile.pathLocked,
                    _ => null
                };
                if (overrideSprite != null)
                    return overrideSprite;
                if (profile.pathCore != null)
                    return profile.pathCore;
            }

            Sprite fromResources = state switch
            {
                PathState.Owned => LoadSprite("skill_path_owned"),
                PathState.Ready => LoadSprite("skill_path_ready"),
                PathState.Locked => LoadSprite("skill_path_locked"),
                _ => null
            };
            if (fromResources != null)
                return fromResources;

            fromResources = LoadSprite("skill_path_core");
            if (fromResources != null)
                return fromResources;

            return ProceduralCore;
        }

        public static Sprite ResolveGlow(DMSkillTreeLineProfile profile)
        {
            if (profile != null && profile.pathGlow != null)
                return profile.pathGlow;

            Sprite fromResources = LoadSprite("skill_path_glow");
            if (fromResources != null)
                return fromResources;

            return ProceduralGlow;
        }

        public static float CoreHeight(DMSkillTreeLineProfile profile) =>
            profile != null && profile.coreHeight > 0.5f ? profile.coreHeight : 5f;

        public static float GlowHeight(DMSkillTreeLineProfile profile) =>
            profile != null && profile.glowHeight > 0.5f ? profile.glowHeight : 13f;

        public static float GlowAlpha(DMSkillTreeLineProfile profile, PathState state)
        {
            if (profile == null)
            {
                return state switch
                {
                    PathState.Owned => 0.78f,
                    PathState.Ready => 0.42f,
                    _ => 0.1f
                };
            }

            return state switch
            {
                PathState.Owned => profile.glowAlphaOwned,
                PathState.Ready => profile.glowAlphaReady,
                _ => profile.glowAlphaLocked
            };
        }

        private static Sprite LoadSprite(string fileNameWithoutExtension)
        {
            return Resources.Load<Sprite>(ResourceFolder + fileNameWithoutExtension);
        }

        private static Sprite ProceduralCore
        {
            get
            {
                if (proceduralCore == null)
                    proceduralCore = CreateHorizontalStrip("DmSkillPathCore", 256, 16, filled: true);
                return proceduralCore;
            }
        }

        private static Sprite ProceduralGlow
        {
            get
            {
                if (proceduralGlow == null)
                    proceduralGlow = CreateHorizontalStrip("DmSkillPathGlow", 256, 32, filled: false, soft: true);
                return proceduralGlow;
            }
        }

        private static Sprite CreateHorizontalStrip(string name, int width, int height, bool filled, bool soft = false)
        {
            width = Mathf.Clamp(width, 32, 512);
            height = Mathf.Clamp(height, 8, 64);
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float midY = height * 0.5f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = x / (width - 1f);
                    float edgeFade = Mathf.Min(Mathf.Min(nx, 1f - nx) * 8f, 1f);
                    float dy = Mathf.Abs(y + 0.5f - midY) / midY;
                    float alpha;
                    if (soft)
                        alpha = Mathf.Clamp01(1f - dy);
                    else
                        alpha = dy < 0.85f ? 1f : Mathf.Clamp01((1f - dy) * 6f);

                    alpha *= edgeFade;
                    if (!filled && !soft)
                        alpha = Mathf.Clamp01(alpha - 0.15f);

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        internal enum PathState
        {
            Locked,
            Ready,
            Owned
        }
    }
}
